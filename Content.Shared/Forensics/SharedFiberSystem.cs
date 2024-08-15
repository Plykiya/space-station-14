namespace Content.Shared.Forensics;

public sealed class SharedFiberSystem : EntitySystem
{
    /// <summary>
    /// Copy the fiber material and color from one entity to another.
    /// Useful if you want to copy the material substance of a clothing item without copying other properties of it.
    /// </summary>
    /// <param name="source">The piece of clothing to copy</param>
    /// <param name="target">The piece of clothing to apply changes to</param>
    public void CopyFibers(Entity<FiberComponent> source, Entity<FiberComponent> target)
    {
        // We only dirty the component if anything actually changed.
        if (target.Comp.FiberMaterial != source.Comp.FiberMaterial || target.Comp.FiberColor != source.Comp.FiberColor)
        {
            target.Comp.FiberMaterial = source.Comp.FiberMaterial;
            target.Comp.FiberColor = source.Comp.FiberColor;
            Dirty(target);
        }
    }
}
