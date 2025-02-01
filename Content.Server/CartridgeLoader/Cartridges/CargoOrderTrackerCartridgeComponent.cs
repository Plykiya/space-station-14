using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class CargoOrderTrackerCartridgeComponent : Component
{
    public List<int> CargoOrders = new();
}
