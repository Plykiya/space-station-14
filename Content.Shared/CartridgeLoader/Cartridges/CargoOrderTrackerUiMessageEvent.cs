using Robust.Shared.Serialization;


namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CargoOrderTrackerUiMessageEvent : CartridgeMessageEvent
{
    public readonly CargoOrderTrackerUiAction Action;
    public readonly int OrderID;

    public CargoOrderTrackerUiMessageEvent(CargoOrderTrackerUiAction action, int orderId)
    {
        Action = action;
        OrderID = orderId;
    }
}

[Serializable, NetSerializable]
public enum CargoOrderTrackerUiAction
{
    Add,
    Remove
}
