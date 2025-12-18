using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BrickNBalls.GameLogic
{
    /// <summary>
    /// Manages all bricks in the game from an OOP perspective.
    /// Processes collision events from ECS and handles game logic (HP, destruction, scoring).
    /// </summary>
    public sealed class BrickManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the BrickManager.
        /// </summary>
        public static BrickManager Instance { get; private set; }

        /// <summary>
        /// Event raised when a brick is hit by a ball.
        /// Parameters: (BrickData brick, int remainingHealth)
        /// </summary>
        public event Action<BrickData, int> BrickHit;

        /// <summary>
        /// Event raised when a brick is destroyed.
        /// </summary>
        public event Action<BrickData> BrickDestroyed;

        private int _pointsPerHit = 1;

        /// <summary>
        /// The number of active bricks remaining.
        /// </summary>
        public int ActiveBrickCount => _bricks.Count;

        private readonly Dictionary<Entity, BrickData> _bricks = new();
        private World _ecsWorld;
        private EntityManager _entityManager;
        private bool _isInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("BrickManager: Duplicate instance detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeEcsReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _bricks.Clear();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                InitializeEcsReferences();
            }

            ProcessCollisionEvents();
        }

        private void InitializeEcsReferences()
        {
            _ecsWorld = World.DefaultGameObjectInjectionWorld;

            if (_ecsWorld == null || !_ecsWorld.IsCreated)
            {
                return;
            }

            _entityManager = _ecsWorld.EntityManager;
            _isInitialized = true;

            // Discover existing bricks in the ECS world.
            DiscoverExistingBricks();
        }

        /// <summary>
        /// Scans the ECS world for existing brick entities and registers them.
        /// </summary>
        private void DiscoverExistingBricks()
        {
            if (!_isInitialized)
            {
                return;
            }

            using EntityQuery query = _entityManager.CreateEntityQuery(
                typeof(ECS.BrickTag),
                typeof(ECS.BrickHealth));

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in entities)
            {
                if (!_bricks.ContainsKey(entity))
                {
                    int initialHealth = _entityManager.GetComponentData<ECS.BrickHealth>(entity).Value;
                    RegisterBrick(entity, initialHealth);
                }
            }
        }

        /// <summary>
        /// Registers a brick entity with the manager.
        /// </summary>
        /// <param name="entity">The ECS entity.</param>
        /// <param name="initialHealth">The initial health of the brick.</param>
        /// <returns>The created BrickData, or null if already registered.</returns>
        public BrickData RegisterBrick(Entity entity, int initialHealth)
        {
            if (_bricks.ContainsKey(entity))
            {
                return _bricks[entity];
            }

            var brickData = new BrickData(entity, initialHealth);
            _bricks[entity] = brickData;

            return brickData;
        }

        /// <summary>
        /// Gets the BrickData for an entity, registering it if necessary.
        /// </summary>
        /// <param name="entity">The ECS entity.</param>
        /// <returns>The BrickData, or null if entity doesn't exist.</returns>
        public BrickData GetBrickData(Entity entity)
        {
            if (_bricks.TryGetValue(entity, out BrickData data))
            {
                return data;
            }

            // Try to register if entity exists with required components.
            if (_isInitialized && _entityManager.Exists(entity) &&
                _entityManager.HasComponent<ECS.BrickTag>(entity) &&
                _entityManager.HasComponent<ECS.BrickHealth>(entity))
            {
                int health = _entityManager.GetComponentData<ECS.BrickHealth>(entity).Value;
                return RegisterBrick(entity, health);
            }

            return null;
        }

        /// <summary>
        /// Processes collision events from the ECS collision bridge.
        /// </summary>
        private void ProcessCollisionEvents()
        {
            if (ECS.CollisionEventBridge.Instance == null)
            {
                return;
            }

            while (ECS.CollisionEventBridge.Instance.TryDequeueCollision(out Entity brickEntity))
            {
                HandleBrickCollision(brickEntity);
            }
        }

        /// <summary>
        /// Handles a collision event for a brick.
        /// </summary>
        /// <param name="brickEntity">The brick entity that was hit.</param>
        private void HandleBrickCollision(Entity brickEntity)
        {
            BrickData brick = GetBrickData(brickEntity);

            if (brick == null || brick.IsDestroyed)
            {
                return;
            }

            ScoreManager.Instance?.AddScore(_pointsPerHit);

            bool wasDestroyed = brick.TakeDamage(1);

            BrickHit?.Invoke(brick, brick.Health);

            if (wasDestroyed)
            {
                HandleBrickDestruction(brick);
            }
        }

        /// <summary>
        /// Handles the destruction of a brick.
        /// </summary>
        /// <param name="brick">The brick that was destroyed.</param>
        private void HandleBrickDestruction(BrickData brick)
        {
            // Remove from tracking.
            _bricks.Remove(brick.Entity);

            // Destroy the ECS entity.
            DestroyBrickEntity(brick.Entity);

            // Raise destroyed event.
            BrickDestroyed?.Invoke(brick);
        }

        /// <summary>
        /// Destroys the ECS entity for a brick.
        /// </summary>
        /// <param name="entity">The entity to destroy.</param>
        private void DestroyBrickEntity(Entity entity)
        {
            if (!_isInitialized || !_entityManager.Exists(entity))
            {
                return;
            }

            _entityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// Resets the brick tracking state for a new game.
        /// </summary>
        public void ResetForNewGame()
        {
            _bricks.Clear();
            DiscoverExistingBricks();
        }
    }
}
