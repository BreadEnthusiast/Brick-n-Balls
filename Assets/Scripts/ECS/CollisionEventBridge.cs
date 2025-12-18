using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BrickNBalls.ECS
{
    /// <summary>
    /// Bridge component that allows ECS systems to communicate collision events to the OOP layer.
    /// This is a singleton MonoBehaviour that holds a thread-safe queue of collision events.
    /// </summary>
    public sealed class CollisionEventBridge : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the CollisionEventBridge.
        /// </summary>
        public static CollisionEventBridge Instance { get; private set; }

        /// <summary>
        /// Thread-safe queue for brick collision events.
        /// ECS systems write to this queue, OOP code reads from it.
        /// </summary>
        public NativeQueue<Entity> BrickCollisionQueue { get; private set; }

        /// <summary>
        /// Thread-safe queue for ball lost events (out-of-bounds).
        /// ECS systems write to this queue, OOP code reads from it.
        /// </summary>
        public NativeQueue<byte> BallLostQueue { get; private set; }

        /// <summary>
        /// Whether the collision queue is ready for use.
        /// </summary>
        public bool IsReady => BrickCollisionQueue.IsCreated && BallLostQueue.IsCreated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CollisionEventBridge: Duplicate instance detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BrickCollisionQueue = new NativeQueue<Entity>(Allocator.Persistent);
            BallLostQueue = new NativeQueue<byte>(Allocator.Persistent);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (BrickCollisionQueue.IsCreated)
            {
                BrickCollisionQueue.Dispose();
            }

            if (BallLostQueue.IsCreated)
            {
                BallLostQueue.Dispose();
            }
        }

        /// <summary>
        /// Enqueues a brick collision event. Called by ECS systems.
        /// </summary>
        /// <param name="brickEntity">The brick entity that was hit.</param>
        public void EnqueueCollision(Entity brickEntity)
        {
            if (BrickCollisionQueue.IsCreated)
            {
                BrickCollisionQueue.Enqueue(brickEntity);
            }
        }

        /// <summary>
        /// Tries to dequeue a brick collision event. Called by OOP code.
        /// </summary>
        /// <param name="brickEntity">The dequeued brick entity, if any.</param>
        /// <returns>True if an event was dequeued, false if the queue was empty.</returns>
        public bool TryDequeueCollision(out Entity brickEntity)
        {
            if (BrickCollisionQueue.IsCreated && BrickCollisionQueue.TryDequeue(out brickEntity))
            {
                return true;
            }

            brickEntity = Entity.Null;
            return false;
        }

        /// <summary>
        /// Enqueues a ball lost event. Called by ECS systems.
        /// </summary>
        public void EnqueueBallLost()
        {
            if (BallLostQueue.IsCreated)
            {
                BallLostQueue.Enqueue(1);
            }
        }

        /// <summary>
        /// Tries to dequeue a ball lost event. Called by OOP code.
        /// </summary>
        /// <returns>True if an event was dequeued; otherwise false.</returns>
        public bool TryDequeueBallLost()
        {
            if (!BallLostQueue.IsCreated)
            {
                return false;
            }

            bool dequeued = BallLostQueue.TryDequeue(out _);
            return dequeued;
        }

        /// <summary>
        /// Clears all pending collision events.
        /// </summary>
        public void ClearQueue()
        {
            if (BrickCollisionQueue.IsCreated)
            {
                while (BrickCollisionQueue.TryDequeue(out _))
                {
                    // Drain the queue.
                }
            }

            if (BallLostQueue.IsCreated)
            {
                while (BallLostQueue.TryDequeue(out _))
                {
                    // Drain the queue.
                }
            }
        }
    }
}
