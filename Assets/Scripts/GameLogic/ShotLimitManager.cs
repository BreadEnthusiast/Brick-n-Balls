using System;
using UnityEngine;

namespace BrickNBalls.GameLogic
{
    /// <summary>
    /// Tracks how many shots the player has taken and enforces a maximum.
    /// Provides a hook for end-game behavior when the last shot is spent and the ball is lost.
    /// </summary>
    public sealed class ShotLimitManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the ShotLimitManager.
        /// </summary>
        public static ShotLimitManager Instance { get; private set; }

        /// <summary>
        /// Raised when the last available shot has been used and the ball is lost (hits out-of-bounds).
        /// Use this to show an end-game popup.
        /// </summary>
        public event Action GameOver;

        /// <summary>
        /// Raised when the shot count changes.
        /// Parameters: (shotsUsed, maxShots).
        /// </summary>
        public event Action<int, int> ShotsChanged;

        [Header("Debug")]
        [Tooltip("If true, logs key shot-limit events to help diagnose issues.")]
        [SerializeField]
        private bool _enableDebugLogging;

        [Header("Shot Limit")]
        [Tooltip("Maximum number of shots allowed for the run.")]
        [SerializeField]
        private int _maxShots = 5;

        /// <summary>
        /// Maximum number of shots allowed.
        /// </summary>
        public int MaxShots
        {
            get => _maxShots;
            set
            {
                _maxShots = Mathf.Max(0, value);
                ShotsChanged?.Invoke(ShotsUsed, _maxShots);
            }
        }

        /// <summary>
        /// Number of shots already used.
        /// </summary>
        public int ShotsUsed { get; private set; }

        /// <summary>
        /// True if another launch is allowed based on the shot limit.
        /// </summary>
        public bool CanLaunch => ShotsUsed < _maxShots;

        private bool _gameOverRaised;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("ShotLimitManager: Duplicate instance detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Tries to consume a shot. Call this when the player launches the ball.
        /// </summary>
        /// <returns>True if a shot was consumed and the launch is allowed; otherwise false.</returns>
        public bool TryConsumeShot()
        {
            if (!CanLaunch)
            {
                LogDebug($"TryConsumeShot denied. ShotsUsed={ShotsUsed}, MaxShots={_maxShots}");
                return false;
            }

            ShotsUsed++;
            ShotsChanged?.Invoke(ShotsUsed, _maxShots);
            LogDebug($"TryConsumeShot ok. ShotsUsed={ShotsUsed}, MaxShots={_maxShots}");
            return true;
        }

        /// <summary>
        /// Notifies that the active ball has been lost (out-of-bounds). If this was the last shot,
        /// the GameOver event is raised.
        /// </summary>
        public void NotifyBallLost()
        {
            if (_gameOverRaised)
            {
                LogDebug("NotifyBallLost ignored (already game over).");
                return;
            }

            LogDebug($"NotifyBallLost. ShotsUsed={ShotsUsed}, MaxShots={_maxShots}");

            if (ShotsUsed >= _maxShots)
            {
                _gameOverRaised = true;
                LogDebug("NotifyBallLost: raising GameOver.");
                GameOver?.Invoke();
            }
        }

        /// <summary>
        /// Resets the shot counter for a new run.
        /// </summary>
        public void ResetShots()
        {
            ShotsUsed = 0;
            _gameOverRaised = false;
            ShotsChanged?.Invoke(ShotsUsed, _maxShots);
            LogDebug("ResetShots.");
        }

        private void LogDebug(string message)
        {
            if (!_enableDebugLogging)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShotLimitManager] {message}", this);
#endif
        }
    }
}
