using System.Collections;
using BrickNBalls.GameLogic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrickNBalls.SceneManagement
{
    /// <summary>
    /// Main menu controller hosted in UIScene.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Tooltip("Name of the gameplay scene to load.")]
        [SerializeField]
        private string _gameSceneName = "GameScene";

        [Header("UI References")]
        [SerializeField]
        private GameObject _mainMenuPanel;

        [SerializeField]
        private Button _startGameButton;

        [SerializeField]
        private HudController _hudController;

        [SerializeField]
        private GameOverPopupController _gameOverPopupController;

        [SerializeField]
        private GameObject _gameOverPanel;

        private bool _isStarting;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            _startGameButton.onClick.AddListener(OnStartClicked);

            ShowMenu(true);
            _hudController?.SetVisible(false);
            _gameOverPopupController?.SetVisible(false);

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveListener(OnStartClicked);
            }
        }

        private bool ValidateReferences()
        {
            if (string.IsNullOrWhiteSpace(_gameSceneName))
            {
                Debug.LogError("MainMenuController: Game scene name is not set.", this);
                return false;
            }

            if (_mainMenuPanel == null)
            {
                Debug.LogError("MainMenuController: Main menu panel reference is not assigned.", this);
                return false;
            }

            if (_startGameButton == null)
            {
                Debug.LogError("MainMenuController: Start Game button reference is not assigned.", this);
                return false;
            }

            if (_hudController == null)
            {
                Debug.LogError("MainMenuController: HUD controller reference is not assigned.", this);
                return false;
            }

            if (_gameOverPopupController == null)
            {
                Debug.LogError("MainMenuController: Game Over popup controller reference is not assigned.", this);
                return false;
            }

            return true;
        }

        private void OnStartClicked()
        {
            if (_isStarting)
            {
                return;
            }

            _isStarting = true;
            StartCoroutine(StartGameFromScratch());
        }

        private IEnumerator StartGameFromScratch()
        {
            // If the gameplay scene is already loaded, unload it first.
            Scene loadedGameScene = SceneManager.GetSceneByName(_gameSceneName);
            if (loadedGameScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(loadedGameScene);
                while (unloadOp != null && !unloadOp.isDone)
                {
                    yield return null;
                }
            }

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Additive);
            while (loadOp != null && !loadOp.isDone)
            {
                yield return null;
            }

            Scene gameScene = SceneManager.GetSceneByName(_gameSceneName);
            if (gameScene.isLoaded)
            {
                SceneManager.SetActiveScene(gameScene);
            }

            // Reset game state.
            GameManager.Instance?.ResetGame();

            ShowMenu(false);
            _hudController?.SetVisible(true);
            _gameOverPopupController?.SetVisible(false);
            _isStarting = false;
        }

        private void ShowMenu(bool isVisible)
        {
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(isVisible);
            }

            if (_startGameButton != null)
            {
                _startGameButton.interactable = isVisible;
            }
        }

        /// <summary>
        /// Returns to the main menu (future: called from game over popup).
        /// </summary>
        public void ReturnToMenu()
        {
            ShowMenu(true);
            _hudController?.SetVisible(false);

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Called by the Game Over popup button. Unloads the gameplay scene and shows the menu again.
        /// </summary>
        public void ReturnToMenuFromGameOver()
        {
            if (_isStarting)
            {
                return;
            }

            StartCoroutine(ReturnToMenuCoroutine());
        }

        private IEnumerator ReturnToMenuCoroutine()
        {
            Scene loadedGameScene = SceneManager.GetSceneByName(_gameSceneName);
            if (loadedGameScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(loadedGameScene);
                while (unloadOp != null && !unloadOp.isDone)
                {
                    yield return null;
                }
            }

            GameManager.Instance?.ResetGame();
            ReturnToMenu();
        }
    }
}
