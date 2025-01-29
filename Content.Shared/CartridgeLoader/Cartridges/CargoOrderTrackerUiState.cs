using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CargoOrderTrackerUiState : BoundUserInterfaceState
{
    public Dictionary<string, CargoOrderTrackerData> CargoOrders = new();
}

[Serializable]
public sealed class CargoOrderTrackerData
{
    public CargoOrderTrackerData(string trackedEntity, string location, TimeSpan lastUpdated)
    {
        TrackedEntity = trackedEntity;
        Location = location;
        LastUpdated = lastUpdated;
    }

    public string TrackedEntity;
    public string Location;
    public TimeSpan LastUpdated;
}

public static class CargoOrderTrackerConstants
{
    public const string CargoOrderTrackerData = "cargo-order-tracker-data";
}
