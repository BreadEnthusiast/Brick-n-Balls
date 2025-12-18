using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrickNBalls.Launcher
{
    [DisallowMultipleComponent]
    public sealed class LauncherController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Horizontal movement speed of the launcher in world units per second.")]
        public float MovementSpeed = 10.0f;

        [Tooltip("Minimum X world position for the launcher.")]
        public float MinX = -7.5f;

        [Tooltip("Maximum X world position for the launcher.")]
        public float MaxX = 7.5f;

        [Header("Input")]
        [Tooltip("Input action used to move the launcher (expected to be a 1D or 2D axis).")]
        public InputActionReference MoveAction;

        [Tooltip("Input action used to trigger the ball launch.")]
        public InputActionReference LaunchAction;

        [Header("Ball Launch")]
        [Tooltip("Launch speed for the ball (world units per second).")]
        public float LaunchSpeed = 6.0f;

        [Tooltip("Camera used to convert mouse position to world space. Defaults to Camera.main.")]
        public Camera TargetCamera;

        private World _ecsWorld;
        private Entity _launcherEntity;
        private Vector3 _aimDirection = Vector3.up;
        private bool _hasWarnedMissingShotLimitManager;
        private bool _hasWarnedMissingLauncherSingleton;

        private void OnEnable()
        {
            if (MoveAction != null)
            {
                MoveAction.action.Enable();
            }

            if (LaunchAction != null)
            {
                LaunchAction.action.performed += OnLaunchPerformed;
                LaunchAction.action.Enable();
            }

            // Get reference to the default ECS world.
            _ecsWorld = World.DefaultGameObjectInjectionWorld;

            // Resolve the launcher singleton entity created by LauncherAuthoring.
            // If it doesn't exist (common when authoring isn't baked), create a minimal singleton as a fallback.
            EnsureLauncherEntityExists();
        }

        private void EnsureLauncherEntityExists()
        {
            if (_ecsWorld == null || !_ecsWorld.IsCreated)
            {
                return;
            }

            EntityManager entityManager = _ecsWorld.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(BrickNBalls.ECS.LauncherPosition));

            try
            {
                if (query.CalculateEntityCount() > 0)
                {
                    _launcherEntity = query.GetSingletonEntity();
                }
                else
                {
                    // Fallback: create the required singleton entity.
                    _launcherEntity = entityManager.CreateEntity();

                    entityManager.AddComponentData(_launcherEntity, new BrickNBalls.ECS.LauncherPosition
                    {
                        Value = (Unity.Mathematics.float3)transform.position
                    });

                    entityManager.AddComponentData(_launcherEntity, new BrickNBalls.ECS.BallInitialVelocity
                    {
                        Value = (Unity.Mathematics.float3)(Vector3.up * LaunchSpeed)
                    });

                    entityManager.AddComponentData(_launcherEntity, new BrickNBalls.ECS.LauncherLaunchState
                    {
                        IsReadyToLaunch = true
                    });

                    if (!_hasWarnedMissingLauncherSingleton)
                    {
                        _hasWarnedMissingLauncherSingleton = true;
                        Debug.LogWarning("LauncherController: Launcher singleton was missing. Created a fallback launcher entity at runtime. Prefer baking LauncherAuthoring in a SubScene.", this);
                    }
                }
            }
            finally
            {
                query.Dispose();
            }
        }

        private void OnDisable()
        {
            if (MoveAction != null)
            {
                MoveAction.action.Disable();
            }

            if (LaunchAction != null)
            {
                LaunchAction.action.performed -= OnLaunchPerformed;
                LaunchAction.action.Disable();
            }
        }

        private void Update()
        {
            float moveAxis = 0.0f;

            if (MoveAction != null)
            {
                // Support both Vector2 and float bindings.
                if (MoveAction.action.expectedControlType == "Vector2")
                {
                    Vector2 value = MoveAction.action.ReadValue<Vector2>();
                    moveAxis = value.x;
                }
                else
                {
                    moveAxis = MoveAction.action.ReadValue<float>();
                }
            }

            Vector3 position = transform.position;
            position.x += moveAxis * MovementSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, MinX, MaxX);
            transform.position = position;

            // Update launcher position in ECS.
            UpdateLauncherPositionInECS();

            // Update launcher rotation to face aim direction.
            UpdateAimRotation();
        }

        private void UpdateLauncherPositionInECS()
        {
            if (_ecsWorld == null || !_ecsWorld.IsCreated || _launcherEntity == Entity.Null)
            {
                return;
            }

            EntityManager entityManager = _ecsWorld.EntityManager;
            
            if (entityManager.Exists(_launcherEntity))
            {
                entityManager.SetComponentData(_launcherEntity, new BrickNBalls.ECS.LauncherPosition
                {
                    Value = (Unity.Mathematics.float3)transform.position
                });
            }
            else
            {
                // Entity was destroyed; in manual-wiring mode we do not recreate it.
                Debug.LogError("LauncherController: Launcher singleton entity no longer exists. Check your SubScene/baking setup.", this);
                _launcherEntity = Entity.Null;
            }
        }

        private void OnLaunchPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (_ecsWorld == null || !_ecsWorld.IsCreated)
            {
                Debug.LogWarning("LauncherController: ECS world is not available. Ball launch request failed.");
                return;
            }

            EntityManager entityManager = _ecsWorld.EntityManager;

            // Ensure launcher entity exists.
            if (_launcherEntity == Entity.Null || !entityManager.Exists(_launcherEntity))
            {
                EnsureLauncherEntityExists();
            }

            if (_launcherEntity == Entity.Null)
            {
                Debug.LogError("LauncherController: Launcher singleton entity is missing; cannot launch.", this);
                return;
            }

            // Only one ball can be in flight at a time.
            if (!entityManager.HasComponent<BrickNBalls.ECS.LauncherLaunchState>(_launcherEntity))
            {
                Debug.LogError("LauncherController: LauncherLaunchState component is missing on launcher entity. Ensure LauncherAuthoring adds it.", this);
                return;
            }

            var launchState = entityManager.GetComponentData<BrickNBalls.ECS.LauncherLaunchState>(_launcherEntity);
            if (!launchState.IsReadyToLaunch)
            {
                return;
            }

            // Enforce shot limit (OOP game logic).
            var shotLimitManager = BrickNBalls.GameLogic.ShotLimitManager.Instance;
            if (shotLimitManager == null)
            {
                if (!_hasWarnedMissingShotLimitManager)
                {
                    _hasWarnedMissingShotLimitManager = true;
                    Debug.LogWarning("LauncherController: ShotLimitManager.Instance is null. Shot limit and game over will not work.");
                }
            }
            else if (!shotLimitManager.TryConsumeShot())
            {
                Debug.Log($"LauncherController: Launch blocked by shot limit. ShotsUsed={shotLimitManager.ShotsUsed}, MaxShots={shotLimitManager.MaxShots}");
                return;
            }

            // Mark launcher as not ready until the ball is recycled.
            launchState.IsReadyToLaunch = false;
            entityManager.SetComponentData(_launcherEntity, launchState);

            Debug.Log("LauncherController: Requesting ball spawn and launch via ECS");

            // Compute launch velocity based on mouse position.
            Vector3 launchVelocity = ComputeLaunchVelocity();

            // Update launcher BallInitialVelocity so the spawn system uses this direction.
            if (entityManager.HasComponent<BrickNBalls.ECS.BallInitialVelocity>(_launcherEntity))
            {
                entityManager.SetComponentData(_launcherEntity, new BrickNBalls.ECS.BallInitialVelocity
                {
                    Value = (Unity.Mathematics.float3)launchVelocity
                });
            }

            Debug.Log($"LauncherController: Using launcher entity: {_launcherEntity}");

            // Add spawn request to move ball to launcher position and launch it.
            // BallSpawnSystem handles both spawning (moving to launcher) and launching.
            if (!entityManager.HasComponent<BrickNBalls.ECS.BallSpawnRequest>(_launcherEntity))
            {
                entityManager.AddComponent<BrickNBalls.ECS.BallSpawnRequest>(_launcherEntity);
                Debug.Log("LauncherController: Added BallSpawnRequest (spawn and launch will be handled by BallSpawnSystem)");
            }
            else
            {
                Debug.LogWarning("LauncherController: BallSpawnRequest already exists on launcher entity");
            }
        }

        private void UpdateAimRotation()
        {
            Vector3 dir = ComputeAimDirection();
            _aimDirection = dir;

            // Orient the launcher so its up axis points along the aim direction (2D).
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
            }
        }

        private Vector3 ComputeLaunchVelocity()
        {
            Camera cam = TargetCamera != null ? TargetCamera : Camera.main;

            if (cam == null || Mouse.current == null)
            {
                return Vector3.up * LaunchSpeed;
            }

            Vector2 mouseScreen = Mouse.current.position.ReadValue();

            // Project mouse to the plane of the launcher (z of launcher).
            float planeZ = transform.position.z;
            float depth = Mathf.Abs(cam.transform.position.z - planeZ);
            Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, depth));

            Vector3 dir = mouseWorld - transform.position;
            dir.z = 0.0f;

            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.up;
            }

            return dir.normalized * LaunchSpeed;
        }

        private Vector3 ComputeAimDirection()
        {
            Camera cam = TargetCamera != null ? TargetCamera : Camera.main;

            if (cam == null || Mouse.current == null)
            {
                return Vector3.up;
            }

            Vector2 mouseScreen = Mouse.current.position.ReadValue();

            float planeZ = transform.position.z;
            float depth = Mathf.Abs(cam.transform.position.z - planeZ);
            Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, depth));

            Vector3 dir = mouseWorld - transform.position;
            dir.z = 0.0f;

            if (dir.sqrMagnitude < 0.0001f)
            {
                return Vector3.up;
            }

            return dir.normalized;
        }
    }
}


