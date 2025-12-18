using BrickNBalls.GameLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickNBalls.SceneManagement
{
    /// <summary>
    /// Controls the Game Over popup (show/hide + final score). Hosted in UIScene.
    /// The visual layout is authored in the editor; this script only binds behavior.
    /// </summary>
    public sealed class GameOverPopupController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameObject _popupRoot;

        [SerializeField]
        private TMP_Text _finalScoreText;

        [SerializeField]
        private Button _goBackToMenuButton;

        [Header("Formatting")]
        [SerializeField]
        private string _finalScorePrefix = "Final Score: ";

        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugLogging;

        private bool _isSubscribed;

        private void Awake()
        {
            if (_popupRoot == null)
            {
                Debug.LogError("GameOverPopupController: Popup root reference is not assigned.", this);
            }

            if (_finalScoreText == null)
            {
                Debug.LogError("GameOverPopupController: Final score text reference is not assigned.", this);
            }

            if (_goBackToMenuButton == null)
            {
                Debug.LogError("GameOverPopupController: Go Back button reference is not assigned.", this);
            }
        }

        private void OnEnable()
        {
            if (_goBackToMenuButton != null)
            {
                _goBackToMenuButton.onClick.AddListener(OnGoBackClicked);
            }

            SetVisible(false);
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_goBackToMenuButton != null)
            {
                _goBackToMenuButton.onClick.RemoveListener(OnGoBackClicked);
            }

            Unsubscribe();
        }

        public void SetVisible(bool isVisible)
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(isVisible);
            }
        }

        private void OnGameOver()
        {
            LogDebug("OnGameOver received.");
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;

            if (_finalScoreText != null)
            {
                _finalScoreText.text = $"{_finalScorePrefix}{score}";
            }
            else
            {
                LogDebug("FinalScoreText reference is null.");
            }

            SetVisible(true);

            // Hide HUD when game over is shown.
            if (FindAnyObjectByType<HudController>() is { } hud)
            {
                hud.SetVisible(false);
            }
        }

        private void OnGoBackClicked()
        {
            if (FindAnyObjectByType<MainMenuController>() is { } menu)
            {
                menu.ReturnToMenuFromGameOver();
            }
        }

        private void TrySubscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.GameOver -= OnGameOver;
            GameManager.Instance.GameOver += OnGameOver;

            _isSubscribed = true;
            LogDebug("Subscribed to GameManager.GameOver.");
        }

        private void Unsubscribe()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver -= OnGameOver;
            }

            _isSubscribed = false;
        }

        private void Update()
        {
            if (!_isSubscribed)
            {
                TrySubscribe();
            }
        }

        private void LogDebug(string message)
        {
            if (!_enableDebugLogging)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GameOverPopupController] {message}", this);
#endif
        }
    }
}
