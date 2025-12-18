using Unity.Entities;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Tag component that identifies an entity as a brick.
    /// Used by the collision system to detect ball-brick collisions.
    /// </summary>
    public struct BrickTag : IComponentData
    {
    }

    /// <summary>
    /// Component that stores the initial health of a brick.
    /// This value is set during baking and read by BrickManager when registering bricks.
    /// The actual health tracking is done in the OOP layer (BrickData).
    /// </summary>
    public struct BrickHealth : IComponentData
    {
        public int Value;
    }
}
