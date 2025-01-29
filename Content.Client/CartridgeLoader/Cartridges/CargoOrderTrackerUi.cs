using Content.Client.UserInterface.Fragments;
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
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CargoOrderTrackerUiState cargoOrderTrackerUiState)
            return;

        _fragment?.UpdateState(cargoOrderTrackerUiState.CargoOrders);
    }
}
