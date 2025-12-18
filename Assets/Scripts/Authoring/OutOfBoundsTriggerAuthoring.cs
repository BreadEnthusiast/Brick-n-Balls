using Unity.Entities;
using UnityEngine;

namespace BrickNBalls.Authoring
{
    /// <summary>
    /// Authoring component for the bottom out-of-bounds trigger.
    /// Attach this to a GameObject that has a Unity Physics trigger collider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutOfBoundsTriggerAuthoring : MonoBehaviour
    {
        public sealed class OutOfBoundsTriggerBaker : Baker<OutOfBoundsTriggerAuthoring>
        {
            public override void Bake(OutOfBoundsTriggerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BrickNBalls.ECS.OutOfBoundsTriggerTag>(entity);
            }
        }
    }
}
