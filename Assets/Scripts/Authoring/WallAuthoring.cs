using Unity.Entities;
using UnityEngine;

namespace BrickNBalls.Authoring
{
    [DisallowMultipleComponent]
    public sealed class WallAuthoring : MonoBehaviour
    {
        public sealed class WallBaker : Baker<WallAuthoring>
        {
            public override void Bake(WallAuthoring authoring)
            {
                _ = GetEntity(TransformUsageFlags.None);
            }
        }
    }
}


    