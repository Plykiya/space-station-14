using Content.Server.Cargo.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Cargo.Components;


[RegisterComponent, AutoGenerateComponentPause, Access(typeof(CargoOrderTrackerSystem))]

public sealed partial class CargoOrderTrackerComponent : Component
{
    /// <summary>
    /// The beacon that the crate is closest to as determined by NavMapSystem.GetNearestBeaconString()
    /// </summary>
    [DataField]
    public string Location = "";

    /// <summary>
    /// When the next update will occur
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan UpdateTime = TimeSpan.Zero;

    /// <summary>
    /// The time (in seconds) between updates to the crate's tracked location
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);
}
