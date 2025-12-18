using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Handles collision detection between balls and bricks.
    /// This system only detects collisions and forwards them to the OOP layer
    /// via the CollisionEventBridge. All game logic (HP, score, destruction)
    /// is handled by BrickManager in the OOP layer.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct BrickCollisionSystem : ISystem
    {
        private ComponentLookup<BrickTag> _brickTagLookup;
        private ComponentLookup<BallTag> _ballTagLookup;

        public void OnCreate(ref SystemState state)
        {
            _brickTagLookup = state.GetComponentLookup<BrickTag>(true);
            _ballTagLookup = state.GetComponentLookup<BallTag>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (CollisionEventBridge.Instance == null || !CollisionEventBridge.Instance.IsReady)
            {
                return;
            }

            _brickTagLookup.Update(ref state);
            _ballTagLookup.Update(ref state);

            NativeQueue<Entity>.ParallelWriter collisionQueueWriter =
                CollisionEventBridge.Instance.BrickCollisionQueue.AsParallelWriter();

            var job = new BrickCollisionJob
            {
                BrickTagLookup = _brickTagLookup,
                BallTagLookup = _ballTagLookup,
                CollisionQueue = collisionQueueWriter
            };

            SimulationSingleton sim = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = job.Schedule(sim, state.Dependency);

            // Ensure the queue is safe for the OOP layer to read this frame.
            state.Dependency.Complete();
        }

        /// <summary>
        /// Job that processes collision events and forwards ball-brick collisions to the OOP layer.
        /// </summary>
        private struct BrickCollisionJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<BrickTag> BrickTagLookup;
            [ReadOnly] public ComponentLookup<BallTag> BallTagLookup;
            public NativeQueue<Entity>.ParallelWriter CollisionQueue;

            public void Execute(CollisionEvent collisionEvent)
            {
                Entity a = collisionEvent.EntityA;
                Entity b = collisionEvent.EntityB;

                bool aIsBrick = BrickTagLookup.HasComponent(a);
                bool bIsBrick = BrickTagLookup.HasComponent(b);
                bool aIsBall = BallTagLookup.HasComponent(a);
                bool bIsBall = BallTagLookup.HasComponent(b);

                // We only care about ball vs brick collisions.
                if (!((aIsBrick && bIsBall) || (bIsBrick && aIsBall)))
                {
                    return;
                }

                Entity brickEntity = aIsBrick ? a : b;

                // Forward the collision to the OOP layer.
                CollisionQueue.Enqueue(brickEntity);
            }
        }
    }
}
