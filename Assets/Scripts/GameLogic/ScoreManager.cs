using System;
using UnityEngine;

namespace BrickNBalls.GameLogic
{
    /// <summary>
    /// Singleton manager responsible for tracking and managing the game score.
    /// This is part of the OOP game logic layer, separate from ECS physics.
    /// </summary>
    public sealed class ScoreManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the ScoreManager.
        /// </summary>
        public static ScoreManager Instance { get; private set; }

        /// <summary>
        /// Event raised when the score changes. Provides the new score value.
        /// </summary>
        public event Action<int> ScoreChanged;

        /// <summary>
        /// The current score.
        /// </summary>
        public int Score
        {
            get => _score;
            private set
            {
                if (_score != value)
                {
                    _score = value;
                    ScoreChanged?.Invoke(_score);
                }
            }
        }

        private int _score;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("ScoreManager: Duplicate instance detected. Destroying this instance.");
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
        /// Adds points to the current score.
        /// </summary>
        /// <param name="points">The number of points to add.</param>
        public void AddScore(int points)
        {
            if (points < 0)
            {
                Debug.LogWarning($"ScoreManager: Attempted to add negative points ({points}). Use ResetScore to clear.");
                return;
            }

            Score += points;
        }

        /// <summary>
        /// Resets the score to zero.
        /// </summary>
        public void ResetScore()
        {
            Score = 0;
        }

        /// <summary>
        /// Sets the score to a specific value.
        /// </summary>
        /// <param name="value">The new score value.</param>
        public void SetScore(int value)
        {
            Score = Mathf.Max(0, value);
        }
    }
}
