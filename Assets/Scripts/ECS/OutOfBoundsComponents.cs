using Unity.Entities;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Tag component for an entity that represents the bottom out-of-bounds trigger.
    /// The associated physics collider should be configured as a Trigger.
    /// </summary>
    public struct OutOfBoundsTriggerTag : IComponentData
    {
    }
}
