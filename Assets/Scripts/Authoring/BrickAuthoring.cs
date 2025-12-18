using Unity.Entities;
using UnityEngine;

namespace BrickNBalls.Authoring
{
    [DisallowMultipleComponent]
    public sealed class BrickAuthoring : MonoBehaviour
    {
        [Tooltip("If true, a random health between MinHealth and MaxHealth is assigned on bake.")]
        public bool RandomizeHealth = true;

        [Tooltip("Minimum health when randomizing.")]
        public int MinHealth = 1;

        [Tooltip("Maximum health when randomizing.")]
        public int MaxHealth = 3;

        [Tooltip("Fixed health when RandomizeHealth is false.")]
        public int FixedHealth = 1;

        public sealed class BrickBaker : Baker<BrickAuthoring>
        {
            public override void Bake(BrickAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                int health = authoring.FixedHealth;
                if (authoring.RandomizeHealth)
                {
                    int min = Mathf.Max(1, authoring.MinHealth);
                    int max = Mathf.Max(min, authoring.MaxHealth);
                    health = Random.Range(min, max + 1);
                }
                else
                {
                    health = Mathf.Max(1, authoring.FixedHealth);
                }

                AddComponent<BrickNBalls.ECS.BrickTag>(entity);
                AddComponent(entity, new BrickNBalls.ECS.BrickHealth
                {
                    Value = health
                });
            }
        }
    }
}
