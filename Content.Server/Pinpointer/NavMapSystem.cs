using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.Warps;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Maps;
using Content.Shared.Pinpointer;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Pinpointer;

/// <summary>
/// Handles data to be used for in-grid map displays.
/// </summary>
public sealed partial class NavMapSystem : SharedNavMapSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<NavMapComponent> _navQuery;

    public override void Initialize()
    {
        base.Initialize();

        var categories = Enum.GetNames(typeof(NavMapChunkType)).Length - 1; // -1 due to "Invalid" entry.
        if (Categories != categories)
            throw new Exception($"{nameof(Categories)} must be equal to the number of chunk types");

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _navQuery = GetEntityQuery<NavMapComponent>();

        // Initialization events
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationInit);

        // Grid change events
        SubscribeLocalEvent<GridSplitEvent>(OnNavMapSplit);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);

        SubscribeLocalEvent<AirtightChanged>(OnAirtightChange);

        // Beacon events
        SubscribeLocalEvent<NavMapBeaconComponent, MapInitEvent>(OnNavMapBeaconMapInit);
        SubscribeLocalEvent<NavMapBeaconComponent, AnchorStateChangedEvent>(OnNavMapBeaconAnchor);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, NavMapBeaconConfigureBuiMessage>(OnConfigureMessage);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, MapInitEvent>(OnConfigurableMapInit);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, ExaminedEvent>(OnConfigurableExamined);
    }

    private void OnStationInit(StationGridAddedEvent ev)
    {
        var comp = EnsureComp<NavMapComponent>(ev.GridId);
        RefreshGrid(ev.GridId, comp, Comp<MapGridComponent>(ev.GridId));
    }

    #region: Grid change event handling

    private void OnNavMapSplit(ref GridSplitEvent args)
    {
        if (!_navQuery.TryComp(args.Grid, out var comp))
            return;

        foreach (var grid in args.NewGrids)
        {
            var newComp = EnsureComp<NavMapComponent>(grid);
            RefreshGrid(grid, newComp, _gridQuery.GetComponent(grid));
        }

        RefreshGrid(args.Grid, comp, _gridQuery.GetComponent(args.Grid));
    }

    private NavMapChunk EnsureChunk(NavMapComponent component, Vector2i origin)
    {
        if (!component.Chunks.TryGetValue(origin, out var chunk))
        {
            chunk = new(origin);
            component.Chunks[origin] = chunk;
        }

        return chunk;
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        if (!ev.EmptyChanged || !_navQuery.TryComp(ev.NewTile.GridUid, out var navMap))
            return;

        var tile = ev.NewTile.GridIndices;
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

        var chunk = EnsureChunk(navMap, chunkOrigin);

        // This could be easily replaced in the future to accommodate diagonal tiles
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        ref var tileData = ref chunk.TileData[GetTileIndex(relative)];

        if (ev.NewTile.IsSpace(_tileDefManager))
        {
            tileData = 0;
            if (PruneEmpty((ev.NewTile.GridUid, navMap), chunk))
                return;
        }
        else
        {
            tileData = FloorMask;
        }

        DirtyChunk((ev.NewTile.GridUid, navMap), chunk);
    }

    private void DirtyChunk(Entity<NavMapComponent> entity, NavMapChunk chunk)
    {
        if (chunk.LastUpdate == _gameTiming.CurTick)
            return;

        chunk.LastUpdate = _gameTiming.CurTick;
        Dirty(entity);
    }

    private void OnAirtightChange(ref AirtightChanged args)
    {
        if (args.AirBlockedChanged)
            return;

        var gridUid = args.Position.Grid;

        if (!_navQuery.TryComp(gridUid, out var navMap) ||
            !_gridQuery.TryComp(gridUid, out var mapGrid))
        {
            return;
        }

        var chunkOrigin = SharedMapSystem.GetChunkIndices(args.Position.Tile, ChunkSize);
        var (newValue, chunk) = RefreshTileEntityContents(gridUid, navMap, mapGrid, chunkOrigin, args.Position.Tile, setFloor: false);

        if (newValue == 0 && PruneEmpty((gridUid, navMap), chunk))
            return;

        DirtyChunk((gridUid, navMap), chunk);
    }

    #endregion

    #region: Beacon event handling

    private void OnNavMapBeaconMapInit(EntityUid uid, NavMapBeaconComponent component, MapInitEvent args)
    {
        if (component.DefaultText == null || component.Text != null)
            return;

        component.Text = Loc.GetString(component.DefaultText);
        Dirty(uid, component);

        UpdateNavMapBeaconData(uid, component);
    }

    private void OnNavMapBeaconAnchor(EntityUid uid, NavMapBeaconComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateBeaconEnabledVisuals((uid, component));
        UpdateNavMapBeaconData(uid, component);
    }

    private void OnConfigureMessage(Entity<ConfigurableNavMapBeaconComponent> ent, ref NavMapBeaconConfigureBuiMessage args)
    {
        if (!TryComp<NavMapBeaconComponent>(ent, out var beacon))
            return;

        if (beacon.Text == args.Text &&
            beacon.Color == args.Color &&
            beacon.Enabled == args.Enabled)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} configured NavMapBeacon \'{ToPrettyString(ent):entity}\' with text \'{args.Text}\', color {args.Color.ToHexNoAlpha()}, and {(args.Enabled ? "enabled" : "disabled")} it.");

        if (TryComp<WarpPointComponent>(ent, out var warpPoint))
        {
            warpPoint.Location = args.Text;
        }

        beacon.Text = args.Text;
        beacon.Color = args.Color;
        beacon.Enabled = args.Enabled;

        UpdateBeaconEnabledVisuals((ent, beacon));
        UpdateNavMapBeaconData(ent, beacon);
    }

    private void OnConfigurableMapInit(Entity<ConfigurableNavMapBeaconComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<NavMapBeaconComponent>(ent, out var navMap))
            return;

        // We set this on mapinit just in case the text was edited via VV or something.
        if (TryComp<WarpPointComponent>(ent, out var warpPoint))
            warpPoint.Location = navMap.Text;

        UpdateBeaconEnabledVisuals((ent, navMap));
    }

    private void OnConfigurableExamined(Entity<ConfigurableNavMapBeaconComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<NavMapBeaconComponent>(ent, out var navMap))
            return;

        args.PushMarkup(Loc.GetString("nav-beacon-examine-text",
            ("enabled", navMap.Enabled),
            ("color", navMap.Color.ToHexNoAlpha()),
            ("label", navMap.Text ?? string.Empty)));
    }

    #endregion

    #region: Grid functions

    private void RefreshGrid(EntityUid uid, NavMapComponent component, MapGridComponent mapGrid)
    {
        // Clear stale data
        component.Chunks.Clear();
        component.Beacons.Clear();

        // Refresh beacons
        var query = EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var qUid, out var qNavComp, out var qTransComp))
        {
            if (qTransComp.ParentUid != uid)
                continue;

            UpdateNavMapBeaconData(qUid, qNavComp);
        }

        // Loop over all tiles
        var tileRefs = _mapSystem.GetAllTiles(uid, mapGrid);

        foreach (var tileRef in tileRefs)
        {
            var tile = tileRef.GridIndices;
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

            var chunk = EnsureChunk(component, chunkOrigin);
            chunk.LastUpdate = _gameTiming.CurTick;
            RefreshTileEntityContents(uid, component, mapGrid, chunkOrigin, tile, setFloor: true);
        }

        Dirty(uid, component);
    }

    private (int NewVal, NavMapChunk Chunk) RefreshTileEntityContents(EntityUid uid,
        NavMapComponent component,
        MapGridComponent mapGrid,
        Vector2i chunkOrigin,
        Vector2i tile,
        bool setFloor)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var chunk = EnsureChunk(component, chunkOrigin);
        ref var tileData = ref chunk.TileData[GetTileIndex(relative)];

        // Clear all data except for floor bits
        if (setFloor)
            tileData = FloorMask;
        else
            tileData &= FloorMask;

        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(uid, mapGrid, tile);
        while (enumerator.MoveNext(out var ent))
        {
            if (!_airtightQuery.TryComp(ent, out var airtight))
                continue;

            var category = GetEntityType(ent.Value);
            if (category == NavMapChunkType.Invalid)
                continue;

            var directions = (int)airtight.AirBlockedDirection;
            tileData |= directions << (int) category;
        }

        // Remove walls that intersect with doors (unless they can both physically fit on the same tile)
        // TODO NAVMAP why can this even happen?
        // Is this for blast-doors or something?

        // Shift airlock bits over to the wall bits
        var shiftedAirlockBits = (tileData & AirlockMask) >> ((int) NavMapChunkType.Airlock - (int) NavMapChunkType.Wall);

        // And then mask door bits
        tileData &= ~shiftedAirlockBits;

        return (tileData, chunk);
    }

    private bool PruneEmpty(Entity<NavMapComponent> entity, NavMapChunk chunk)
    {
        foreach (var val in chunk.TileData)
        {
            // TODO NAVMAP SIMD
            if (val != 0)
                return false;
        }

        entity.Comp.Chunks.Remove(chunk.Origin);
        Dirty(entity);
        return true;
    }

    #endregion

    #region: Beacon functions

    private void UpdateNavMapBeaconData(EntityUid uid, NavMapBeaconComponent component, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        if (xform.GridUid == null)
            return;

        if (!_navQuery.TryComp(xform.GridUid, out var navMap))
            return;

        var meta = MetaData(uid);
        var changed = navMap.Beacons.Remove(meta.NetEntity);

        if (TryCreateNavMapBeaconData(uid, component, xform, meta, out var beaconData))
        {
            navMap.Beacons.Add(meta.NetEntity, beaconData.Value);
            changed = true;
        }

        if (changed)
            Dirty(xform.GridUid.Value, navMap);
    }

    private void UpdateBeaconEnabledVisuals(Entity<NavMapBeaconComponent> ent)
    {
        _appearance.SetData(ent, NavMapBeaconVisuals.Enabled, ent.Comp.Enabled && Transform(ent).Anchored);
    }

    /// <summary>
    /// Sets the beacon's Enabled field and refreshes the grid.
    /// </summary>
    public void SetBeaconEnabled(EntityUid uid, bool enabled, NavMapBeaconComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Enabled == enabled)
            return;

        comp.Enabled = enabled;
        UpdateBeaconEnabledVisuals((uid, comp));
    }

    /// <summary>
    /// Toggles the beacon's Enabled field and refreshes the grid.
    /// </summary>
    public void ToggleBeacon(EntityUid uid, NavMapBeaconComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        SetBeaconEnabled(uid, !comp.Enabled, comp);
    }

    #endregion
}
