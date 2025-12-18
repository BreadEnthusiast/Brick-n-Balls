using System;
using UnityEngine;

namespace BrickNBalls.GameLogic
{
    /// <summary>
    /// Main game manager that initializes and coordinates all game systems.
    /// Attach this to a GameObject in your main scene to set up the game.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the GameManager.
        /// </summary>
        public static GameManager Instance { get; private set; }

        [Header("Manager References")]
        [Tooltip("Reference to the ScoreManager. If null, will try to find or create one.")]
        [SerializeField]
        private ScoreManager _scoreManager;

        [Tooltip("Reference to the BrickManager. If null, will try to find or create one.")]
        [SerializeField]
        private BrickManager _brickManager;

        [Tooltip("Reference to the CollisionEventBridge. If null, will try to find or create one.")]
        [SerializeField]
        private ECS.CollisionEventBridge _collisionBridge;

        [Tooltip("Reference to the ShotLimitManager. If null, will try to find or create one.")]
        [SerializeField]
        private ShotLimitManager _shotLimitManager;

        [Header("Debug")]
        [Tooltip("If true, logs key game flow events to help diagnose issues.")]
        [SerializeField]
        private bool _enableDebugLogging;

        /// <summary>
        /// Raised when the run is over (last shot spent and the ball is lost).
        /// Hook your future popup UI here.
        /// </summary>
        public event Action GameOver;

        /// <summary>
        /// Whether the game is currently paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GameManager: Duplicate instance detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            Instance = this;
            _shotLimitManager.GameOver -= OnShotLimitGameOver;
            _shotLimitManager.GameOver += OnShotLimitGameOver;

            LogDebug("Awake complete. Subscribed to ShotLimitManager.GameOver.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool ValidateReferences()
        {
            if (_scoreManager == null)
            {
                Debug.LogError("GameManager: ScoreManager reference is not assigned.", this);
                return false;
            }

            if (_brickManager == null)
            {
                Debug.LogError("GameManager: BrickManager reference is not assigned.", this);
                return false;
            }

            if (_collisionBridge == null)
            {
                Debug.LogError("GameManager: CollisionEventBridge reference is not assigned.", this);
                return false;
            }

            if (_shotLimitManager == null)
            {
                Debug.LogError("GameManager: ShotLimitManager reference is not assigned.", this);
                return false;
            }

            return true;
        }

        private void Update()
        {
            if (_collisionBridge == null)
            {
                LogDebug("Update: collision bridge is null.");
                return;
            }

            int drained = 0;
            while (_collisionBridge.TryDequeueBallLost())
            {
                drained++;
                _shotLimitManager?.NotifyBallLost();
            }

            if (drained > 0)
            {
                if (_shotLimitManager == null)
                {
                    Debug.LogWarning("[GameManager] Dequeued BallLost but ShotLimitManager reference is null.");
                }

                LogDebug($"Update: dequeued BallLost x{drained}. ShotsUsed={_shotLimitManager?.ShotsUsed}, MaxShots={_shotLimitManager?.MaxShots}");
            }
        }

        private void OnShotLimitGameOver()
        {
            LogDebug("OnShotLimitGameOver: raising GameManager.GameOver");
            GameOver?.Invoke();
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void PauseGame()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Resumes the game.
        /// </summary>
        public void ResumeGame()
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Resets the game state for a new game.
        /// </summary>
        public void ResetGame()
        {
            _scoreManager?.ResetScore();
            _brickManager?.ResetForNewGame();
            _collisionBridge?.ClearQueue();
            _shotLimitManager?.ResetShots();

            LogDebug("ResetGame called.");
        }

        private void LogDebug(string message)
        {
            if (!_enableDebugLogging)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GameManager] {message}", this);
#endif
        }
    }
}
