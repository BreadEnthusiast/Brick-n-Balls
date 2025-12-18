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

        private ScoreManager _subscribedScoreManager;
        private ShotLimitManager _subscribedShotLimitManager;

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
            MaintainSubscription();
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
            MaintainSubscription();
        }

        private void MaintainSubscription()
        {
            ScoreManager currentScoreManager = ScoreManager.Instance;
            ShotLimitManager currentShotLimitManager = ShotLimitManager.Instance;

            if (_subscribedScoreManager != currentScoreManager)
            {
                if (_subscribedScoreManager != null)
                {
                    _subscribedScoreManager.ScoreChanged -= OnScoreChanged;
                }

                _subscribedScoreManager = currentScoreManager;
                if (_subscribedScoreManager != null)
                {
                    _subscribedScoreManager.ScoreChanged += OnScoreChanged;
                }
            }

            if (_subscribedShotLimitManager != currentShotLimitManager)
            {
                if (_subscribedShotLimitManager != null)
                {
                    _subscribedShotLimitManager.ShotsChanged -= OnShotsChanged;
                }

                _subscribedShotLimitManager = currentShotLimitManager;
                if (_subscribedShotLimitManager != null)
                {
                    _subscribedShotLimitManager.ShotsChanged += OnShotsChanged;
                }
            }

            if (_subscribedScoreManager != null && _subscribedShotLimitManager != null)
            {
                RefreshAll();
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedScoreManager != null)
            {
                _subscribedScoreManager.ScoreChanged -= OnScoreChanged;
                _subscribedScoreManager = null;
            }

            if (_subscribedShotLimitManager != null)
            {
                _subscribedShotLimitManager.ShotsChanged -= OnShotsChanged;
                _subscribedShotLimitManager = null;
            }
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
