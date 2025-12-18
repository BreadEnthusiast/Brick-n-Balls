using Unity.Entities;
using Unity.Mathematics;

namespace BrickNBalls.ECS
{
    public struct BallTag : IComponentData
    {
    }

    public struct BallInitialVelocity : IComponentData
    {
        public float3 Value;
    }

    /// <summary>
    /// Singleton component that stores the launcher's current position.
    /// Updated by LauncherController, used by BallSpawnSystem to spawn balls.
    /// </summary>
    public struct LauncherPosition : IComponentData
    {
        public float3 Value;
    }

    /// <summary>
    /// Singleton component that tracks whether the launcher is currently allowed to fire.
    /// When false, the ball is considered "in flight" and the launcher should ignore launch input.
    /// When true, the ball has been recycled (e.g. fell out of bounds) and can be launched again.
    /// </summary>
    public struct LauncherLaunchState : IComponentData
    {
        public bool IsReadyToLaunch;
    }

    /// <summary>
    /// Component that signals a ball should be spawned at the launcher position.
    /// Added by LauncherController when launch is requested.
    /// </summary>
    public struct BallSpawnRequest : IComponentData
    {
    }
}


