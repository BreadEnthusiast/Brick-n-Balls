using Unity.Entities;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Component used as a request to recycle the ball back to the launcher.
    /// Added by the physics trigger job and processed later outside the physics step.
    /// </summary>
    public struct BallRecycleRequest : IComponentData
    {
    }
}
