using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Detects when the ball enters the bottom out-of-bounds trigger and enqueues a
    /// <see cref="BallRecycleRequest"/> for the ball. The actual recycle (teleport + velocity reset)
    /// is applied later by <see cref="BallRecycleApplySystem"/>.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct BallRecycleSystem : ISystem
    {
        private ComponentLookup<OutOfBoundsTriggerTag> _outOfBoundsLookup;
        private ComponentLookup<BallTag> _ballTagLookup;
        private ComponentLookup<BallRecycleRequest> _ballRecycleRequestLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;

        public void OnCreate(ref SystemState state)
        {
            _outOfBoundsLookup = state.GetComponentLookup<OutOfBoundsTriggerTag>(true);
            _ballTagLookup = state.GetComponentLookup<BallTag>(true);
            _ballRecycleRequestLookup = state.GetComponentLookup<BallRecycleRequest>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);

            state.RequireForUpdate<LauncherPosition>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _outOfBoundsLookup.Update(ref state);
            _ballTagLookup.Update(ref state);
            _ballRecycleRequestLookup.Update(ref state);
            _localTransformLookup.Update(ref state);

            Entity launcherEntity = SystemAPI.GetSingletonEntity<LauncherPosition>();
            float3 launcherPosition = SystemAPI.GetComponent<LauncherPosition>(launcherEntity).Value;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            var job = new BallRecycleJob
            {
                OutOfBoundsTriggerLookup = _outOfBoundsLookup,
                BallTagLookup = _ballTagLookup,
                BallRecycleRequestLookup = _ballRecycleRequestLookup,
                LocalTransformLookup = _localTransformLookup,
                LauncherY = launcherPosition.y,
                Ecb = ecb,
            };

            SimulationSingleton sim = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = job.Schedule(sim, state.Dependency);
        }

        private struct BallRecycleJob : ITriggerEventsJob
        {
            [ReadOnly] public ComponentLookup<OutOfBoundsTriggerTag> OutOfBoundsTriggerLookup;
            [ReadOnly] public ComponentLookup<BallTag> BallTagLookup;
            [ReadOnly] public ComponentLookup<BallRecycleRequest> BallRecycleRequestLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            public float LauncherY;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(TriggerEvent triggerEvent)
            {
                Entity a = triggerEvent.EntityA;
                Entity b = triggerEvent.EntityB;

                bool aIsOutOfBounds = OutOfBoundsTriggerLookup.HasComponent(a);
                bool bIsOutOfBounds = OutOfBoundsTriggerLookup.HasComponent(b);

                if (!aIsOutOfBounds && !bIsOutOfBounds)
                {
                    return;
                }

                Entity other = aIsOutOfBounds ? b : a;

                if (!BallTagLookup.HasComponent(other))
                {
                    return;
                }

                // Only recycle once the ball is below the launcher to avoid immediate recycle at spawn.
                if (!LocalTransformLookup.HasComponent(other))
                {
                    return;
                }

                float ballY = LocalTransformLookup[other].Position.y;
                const float MinFallDistanceBelowLauncher = 0.10f;

                if (ballY >= LauncherY - MinFallDistanceBelowLauncher)
                {
                    return;
                }

                // Avoid spamming duplicate requests while the ball stays inside the trigger volume.
                if (BallRecycleRequestLookup.HasComponent(other))
                {
                    return;
                }

                // Do not mutate transforms/velocities during the physics step; enqueue a request instead.
                Ecb.AddComponent(0, other, new BallRecycleRequest());
            }
        }
    }
}
