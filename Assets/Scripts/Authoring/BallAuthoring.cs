using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BrickNBalls.Authoring
{
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class BallAuthoring : MonoBehaviour
    {
        [Tooltip("Initial launch direction and speed for the ball in world space.")]
        public Vector3 InitialVelocity = new Vector3(0.0f, 6.0f, 0.0f);

        public sealed class BallBaker : Baker<BallAuthoring>
        {
            public override void Bake(BallAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<BrickNBalls.ECS.BallTag>(entity);
                AddComponent(entity, new BrickNBalls.ECS.BallInitialVelocity
                {
                    Value = (float3)authoring.InitialVelocity
                });
            }
        }
    }
}
