using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// The GloveBoxComponent/System
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GloveBoxComponent : Component
{
    /// <summary>
    /// The pair of gloves to spawn that will be receiving new visuals, clothing comp, and fibers.
    /// </summary>
    [DataField]
    public EntProtoId GloveSpawn = "GloveBoxThievingGloves";
}
