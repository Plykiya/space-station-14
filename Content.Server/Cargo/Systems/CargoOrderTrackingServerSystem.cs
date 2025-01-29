using Content.Server.Cargo.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.Cargo.Systems;

public sealed class CargoOrderTrackingServerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CargoOrderTrackingServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(Entity<CargoOrderTrackingServerComponent> entity, ref DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        if (payload.TryGetValue(CargoOrderTrackerConstants.CargoOrderTrackerData, out CargoOrderTrackerData? data))
        {
            entity.Comp.CargoOrders[data.TrackedEntity] = data;
        }
    }
}
