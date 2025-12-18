using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// System that moves the ball to the launcher position when a spawn request is made.
    /// The ball should already exist in the scene (baked from a SubScene).
    /// </summary>
    public partial struct BallSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LauncherPosition>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var launcherQuery = SystemAPI.QueryBuilder()
                .WithAll<LauncherPosition, BallSpawnRequest>()
                .Build();

            if (launcherQuery.IsEmpty)
            {
                return;
            }

            var launcherEntity = launcherQuery.GetSingletonEntity();
            var launcherPosition = SystemAPI.GetComponent<LauncherPosition>(launcherEntity);
            var initialVelocity = SystemAPI.GetComponent<BallInitialVelocity>(launcherEntity);

            state.EntityManager.RemoveComponent<BallSpawnRequest>(launcherEntity);

            foreach (var (transform, velocity, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>>()
                .WithAll<BallTag>()
                .WithEntityAccess())
            {
                transform.ValueRW.Position = launcherPosition.Value;

                velocity.ValueRW.Linear = initialVelocity.Value;
                velocity.ValueRW.Angular = float3.zero;
            }
        }
    }
}

