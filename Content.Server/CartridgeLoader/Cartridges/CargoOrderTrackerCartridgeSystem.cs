using Content.Server.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class CargoOrderTrackerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoOrderTrackerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<CargoOrderTrackerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(Entity<CargoOrderTrackerCartridgeComponent> entity, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(entity, args.Loader);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(Entity<CargoOrderTrackerCartridgeComponent> entity, ref CartridgeMessageEvent args)
    {
        if (args is not CargoOrderTrackerUiMessageEvent message)
            return;

        switch (message.Action)
        {
            case CargoOrderTrackerUiAction.Add:
                entity.Comp.CargoOrders.Add(message.OrderID);
                _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                    $"{ToPrettyString(args.Actor)} added an order to PDA: '{message.OrderID}' contained on: {ToPrettyString(entity.Owner)}");
                break;
            case CargoOrderTrackerUiAction.Remove:
                entity.Comp.CargoOrders.Remove(message.OrderID);
                _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                    $"{ToPrettyString(args.Actor)} removed a note from PDA: '{message.OrderID}' was contained on: {ToPrettyString(entity.Owner)}");
                break;
        }

        UpdateUiState(entity, GetEntity(args.LoaderUid));
    }

    private void UpdateUiState(Entity<CargoOrderTrackerCartridgeComponent> entity, EntityUid loaderUid)
    {
        // var state = new CargoOrderTrackerUiState(entity.Comp.CargoOrders);
        // _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
