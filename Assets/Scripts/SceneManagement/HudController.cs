using BrickNBalls.GameLogic;
using TMPro;
using UnityEngine;

namespace BrickNBalls.SceneManagement
{
    /// <summary>
    /// Updates HUD UI elements (score and shots remaining).
    /// Hosted in UIScene.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameObject _hudRoot;

        [SerializeField]
        private TMP_Text _scoreText;

        [SerializeField]
        private TMP_Text _shotsRemainingText;

        [Header("Formatting")]
        [SerializeField]
        private string _scorePrefix = "Score: ";

        [SerializeField]
        private string _shotsPrefix = "Shots: ";

        private bool _isSubscribed;

        private void Awake()
        {
            if (_hudRoot == null)
            {
                Debug.LogError("HudController: HUD root reference is not assigned.", this);
            }

            if (_scoreText == null)
            {
                Debug.LogError("HudController: Score text reference is not assigned.", this);
            }

            if (_shotsRemainingText == null)
            {
                Debug.LogError("HudController: Shots remaining text reference is not assigned.", this);
            }
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetVisible(bool isVisible)
        {
            if (_hudRoot != null)
            {
                _hudRoot.SetActive(isVisible);
            }
        }

        private void Update()
        {
            if (!_isSubscribed)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (ScoreManager.Instance == null || ShotLimitManager.Instance == null)
            {
                return;
            }

            ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
            ScoreManager.Instance.ScoreChanged += OnScoreChanged;

            ShotLimitManager.Instance.ShotsChanged -= OnShotsChanged;
            ShotLimitManager.Instance.ShotsChanged += OnShotsChanged;

            _isSubscribed = true;
            RefreshAll();
        }

        private void Unsubscribe()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
            }

            if (ShotLimitManager.Instance != null)
            {
                ShotLimitManager.Instance.ShotsChanged -= OnShotsChanged;
            }

            _isSubscribed = false;
        }

        private void RefreshAll()
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            OnScoreChanged(score);

            int used = ShotLimitManager.Instance != null ? ShotLimitManager.Instance.ShotsUsed : 0;
            int max = ShotLimitManager.Instance != null ? ShotLimitManager.Instance.MaxShots : 0;
            OnShotsChanged(used, max);
        }

        private void OnScoreChanged(int score)
        {
            if (_scoreText == null)
            {
                return;
            }

            _scoreText.text = $"{_scorePrefix}{score}";
        }

        private void OnShotsChanged(int shotsUsed, int maxShots)
        {
            if (_shotsRemainingText == null)
            {
                return;
            }

            int remaining = Mathf.Max(0, maxShots - shotsUsed);
            _shotsRemainingText.text = $"{_shotsPrefix}{remaining}";
        }
    }
}
