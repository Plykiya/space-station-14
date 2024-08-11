using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Spawns items when used in hand.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnItemsOnUseComponent : Component
    {
        /// <summary>
        /// Table that determines what gets spawned.
        /// </summary>
        [DataField(required: true)]
        public EntityTableSelector Table = default!;

        /// <summary>
        /// Scatter of entity spawn coordinates to make selecting things a bit easier.
        /// </summary>
        [DataField]
        public float Offset = 0.2f;

        /// <summary>
        ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
        /// </summary>
        [DataField]
        public SoundSpecifier? Sound;

        /// <summary>
        ///     How many uses before the item should delete itself.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public int Uses = 1;
    }
}
