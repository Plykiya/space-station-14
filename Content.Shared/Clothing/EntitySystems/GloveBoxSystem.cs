using Content.Shared.Clothing.Components;
using Content.Shared.Forensics;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class GloveBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedFiberSystem _fiberSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GloveBoxComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<GloveBoxComponent> entity, ref UseInHandEvent args)
    {

    }
}
