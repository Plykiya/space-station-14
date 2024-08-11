using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class SpawnItemsOnUseSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly EntityTableSystem _entityTable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnItemsOnUseComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<SpawnItemsOnUseComponent, PriceCalculationEvent>(CalculatePrice, before: new[] { typeof(PricingSystem) });
        }

        private void CalculatePrice(Entity<SpawnItemsOnUseComponent> entity, ref PriceCalculationEvent args)
        {
            var ungrouped = CollectOrGroups(entity.Comp.Items, out var orGroups);

            foreach (var entry in ungrouped)
            {
                var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                // Calculate the average price of the possible spawned items
                args.Price += _pricing.GetPrice(protUid) * entry.SpawnProbability * entry.GetAmount(getAverage: true);

                EntityManager.DeleteEntity(protUid);
            }

            foreach (var group in orGroups)
            {
                foreach (var entry in group.Entries)
                {
                    var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                    // Calculate the average price of the possible spawned items
                    args.Price += _pricing.GetPrice(protUid) *
                                  (entry.SpawnProbability / group.CumulativeProbability) *
                                  entry.GetAmount(getAverage: true);

                    EntityManager.DeleteEntity(protUid);
                }
            }

            args.Handled = true;
        }

        private void OnUseInHand(Entity<SpawnItemsOnUseComponent> entity, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            // If starting with zero or fewer uses, this component is a no-op
            if (entity.Comp.Uses <= 0)
                return;

            var coords = Transform(args.User).Coordinates;
            var spawnEntities = _entityTable.GetSpawns(entity.Comp.Table);
            EntityUid? entityToPlaceInHands = null;

            foreach (var proto in spawnEntities)
            {
                entityToPlaceInHands = Spawn(proto, coords);
                _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User)} used {ToPrettyString(entity)} which spawned {ToPrettyString(entityToPlaceInHands.Value)}");
            }

            if (entity.Comp.Sound != null)
            {
                // The entity is often deleted, so play the sound at its position rather than parenting
                var coordinates = Transform(entity).Coordinates;
                _audio.PlayPvs(entity.Comp.Sound, coordinates);
            }

            entity.Comp.Uses--;

            // Delete entity only if component was successfully used
            if (entity.Comp.Uses <= 0)
            {
                args.Handled = true;
                EntityManager.DeleteEntity(entity);
            }

            if (entityToPlaceInHands != null)
            {
                _hands.PickupOrDrop(args.User, entityToPlaceInHands.Value);
            }
        }
    }
}
