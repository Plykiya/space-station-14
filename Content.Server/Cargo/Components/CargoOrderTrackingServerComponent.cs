using Content.Server.Cargo.Systems;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoOrderTrackingServerSystem))]
public sealed partial class CargoOrderTrackingServerComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this server.
    /// </summary>
    [DataField]
    public Dictionary<string, CargoOrderTrackerData> CargoOrders = new();
}
