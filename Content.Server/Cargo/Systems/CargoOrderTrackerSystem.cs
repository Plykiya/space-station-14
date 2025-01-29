using Content.Server.Cargo.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork;
using Content.Shared.Pinpointer;
using Robust.Shared.Timing;

namespace Content.Server.Cargo.Systems;

public sealed class CargoOrderTrackerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedNavMapSystem _navMapSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CargoOrderTrackerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CargoOrderTrackerComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.UpdateTime = _gameTiming.CurTime + entity.Comp.UpdateInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entityQuery = EntityQueryEnumerator<CargoOrderTrackerComponent, DeviceNetworkComponent>();

        while (entityQuery.MoveNext(out var uid, out var cargoOrderTrackerComp, out var deviceNetworkComponent))
        {
            if (_gameTiming.CurTime < cargoOrderTrackerComp.UpdateTime)
                continue;

            cargoOrderTrackerComp.Location = _navMapSystem.GetNearestBeaconString((uid, Transform(uid)));
            cargoOrderTrackerComp.UpdateTime += cargoOrderTrackerComp.UpdateInterval;

            var payload = new NetworkPayload
            {
                [CargoOrderTrackerConstants.CargoOrderTrackerData] = new CargoOrderTrackerData(uid.ToString(), cargoOrderTrackerComp.Location, _gameTiming.CurTime)
            };

            _deviceNetworkSystem.QueuePacket(uid, null, payload, deviceNetworkComponent.TransmitFrequency);
        }
    }
}

