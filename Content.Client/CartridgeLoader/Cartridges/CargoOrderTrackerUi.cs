using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class CargoOrderTrackerUi : UIFragment
{
    private CargoOrderTrackerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CargoOrderTrackerUiFragment();
        _fragment.OnOrderAdded += orderId =>
            SendCargoOrderTrackerMessage(CargoOrderTrackerUiAction.Add, orderId, userInterface);
        _fragment.OnOrderRemoved += orderId =>
            SendCargoOrderTrackerMessage(CargoOrderTrackerUiAction.Remove, orderId, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CargoOrderTrackerUiState cargoOrderTrackerUiState)
            return;

        _fragment?.UpdateState(cargoOrderTrackerUiState.CargoOrders);
    }

    private void SendCargoOrderTrackerMessage(CargoOrderTrackerUiAction action,
        int orderId,
        BoundUserInterface userInterface)
    {
        var cargoOrderTrackerMessage = new CargoOrderTrackerUiMessageEvent(action, orderId);
        var message = new CartridgeUiMessage(cargoOrderTrackerMessage);
        userInterface.SendMessage(message);
    }
}
