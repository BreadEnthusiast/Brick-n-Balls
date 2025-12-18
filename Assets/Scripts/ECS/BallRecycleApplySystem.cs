using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Applies ball recycle requests outside the physics step.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct BallRecycleApplySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LauncherPosition>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity launcherEntity = SystemAPI.GetSingletonEntity<LauncherPosition>();
            float3 launcherPosition = SystemAPI.GetComponent<LauncherPosition>(launcherEntity).Value;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool recycledAnyBall = false;

            foreach (var (transform, velocity, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>>()
                .WithAll<BallTag, BallRecycleRequest>()
                .WithEntityAccess())
            {
                // Stop the ball.
                velocity.ValueRW.Linear = float3.zero;
                velocity.ValueRW.Angular = float3.zero;

                // Move ball back to launcher.
                transform.ValueRW.Position = launcherPosition;

                // Clear request.
                ecb.RemoveComponent<BallRecycleRequest>(entity);
                recycledAnyBall = true;
            }

            // Allow the launcher to fire again if we recycled at least one ball this frame.
            if (recycledAnyBall && state.EntityManager.HasComponent<LauncherLaunchState>(launcherEntity))
            {
                var launchState = state.EntityManager.GetComponentData<LauncherLaunchState>(launcherEntity);
                launchState.IsReadyToLaunch = true;
                state.EntityManager.SetComponentData(launcherEntity, launchState);
            }

            // Notify OOP layer that the ball was lost this frame.
            if (recycledAnyBall && CollisionEventBridge.Instance != null && CollisionEventBridge.Instance.IsReady)
            {
                CollisionEventBridge.Instance.EnqueueBallLost();
            }
        }
    }
}
