using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BrickNBalls.Authoring
{
    /// <summary>
    /// Authoring component for the launcher. Creates a singleton entity that tracks
    /// the launcher's position for ball spawning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LauncherAuthoring : MonoBehaviour
    {
        public sealed class LauncherBaker : Baker<LauncherAuthoring>
        {
            public override void Bake(LauncherAuthoring authoring)
            {
                // Create a singleton entity to track launcher position.
                Entity entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);

                AddComponent<BrickNBalls.ECS.LauncherPosition>(entity, new BrickNBalls.ECS.LauncherPosition
                {
                    Value = (float3)authoring.transform.position
                });

                // Provide a default initial velocity; LauncherController overwrites this at launch time.
                AddComponent(entity, new BrickNBalls.ECS.BallInitialVelocity
                {
                    Value = new float3(0.0f, 6.0f, 0.0f)
                });

                // Launcher can fire at the start of the game.
                AddComponent(entity, new BrickNBalls.ECS.LauncherLaunchState
                {
                    IsReadyToLaunch = true
                });
            }
        }
    }
}

