using Patterns;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Gameplay
{
    /// <summary>
    /// Manages modes and UI of the application.
    /// </summary>
    public class ApplicationManager : Singleton<ApplicationManager>
    {
        /// <summary>UI used in the simulation mode.</summary>
        [SerializeField] private GameObject simulationUI;

        /// <summary>UI used in gameplay mode.</summary>
        [SerializeField] private GameObject gameplayUI;

        /// <summary>UI with instructions for the gameplay mode.</summary>
        [SerializeField] private GameObject instructionsUI;

        /// <summary>UI displayed when the player loses in gameplay mode.</summary>
        [SerializeField] private GameObject gameOverUI;

        /// <summary>Text which displays the score during gameplay.</summary>
        [SerializeField] private TextMeshProUGUI scoreText;

        /// <summary>Text which displays the score when the player loses.</summary>
        [SerializeField] private TextMeshProUGUI gameOverScoreText;

        /// <summary>Slider used for displaying health.</summary>
        [SerializeField] private Slider healthSlider;

        /// <summary>Player controller used in the simulation mode.</summary>
        [SerializeField] private GameObject simulationPlayerController;

        /// <summary>Player controller used in the gameplay mode.</summary>
        [SerializeField] private GameObject gameplayPlayerController;

        /// <summary>Start position for player controllers.</summary>
        [SerializeField] private Transform startPosition;

        /// <summary>Anglerfish controlled in the gameplay mode.</summary>
        private Anglerfish _anglerfish;

        /// <summary>Flag showing whether the application is currently in gameplay mode.</summary>
        private bool _gameplayMode;

        /// <summary>Currently existing player controller.</summary>
        private GameObject _playerController;

        /// <summary>
        /// Starts application in simulation mode.
        /// </summary>
        private void Start()
        {
            ChangeModeToSimulation();
        }

        /// <summary>
        /// Updates UI if in gameplay mode.
        /// </summary>
        private void Update()
        {
            if (_gameplayMode && _anglerfish != null)
            {
                healthSlider.value = _anglerfish.HealthPercentage;
                scoreText.text = "Score: " + _anglerfish.Score;
            }
        }

        /// <summary>
        /// Switches mode.
        /// </summary>
        public void SwitchMode()
        {
            if (_gameplayMode)
                ChangeModeToSimulation();
            else
                ChangeModeToGameplay(true);
        }

        /// <summary>
        /// Changes mode to simulation.
        /// </summary>
        public void ChangeModeToSimulation()
        {
            CursorHelper.ShowCursor();

            // create simulation player controller
            if (_playerController != null) Destroy(_playerController);
            _playerController = Instantiate(simulationPlayerController, startPosition.position, Quaternion.identity);

            gameplayUI.SetActive(false);
            gameOverUI.SetActive(false);
            simulationUI.SetActive(true);

            _gameplayMode = false;
        }

        /// <summary>
        /// Changes mode to gameplay.
        /// </summary>
        /// <param name="showInstructions">Whether instructions should be shown before entering the mode.</param>
        public void ChangeModeToGameplay(bool showInstructions)
        {
            if (showInstructions)
            {
                CursorHelper.ShowCursor();

                simulationUI.SetActive(false);
                gameOverUI.SetActive(false);
                gameplayUI.SetActive(false);
                instructionsUI.SetActive(true);

                _gameplayMode = false;
            }
            else
            {
                CursorHelper.HideCursor();

                // create gameplay player controller
                if (_playerController != null) Destroy(_playerController);
                _playerController = Instantiate(gameplayPlayerController, startPosition.position, Quaternion.identity);
                _anglerfish = _playerController.GetComponent<Anglerfish>();

                simulationUI.SetActive(false);
                gameOverUI.SetActive(false);
                instructionsUI.SetActive(false);
                gameplayUI.SetActive(true);

                _gameplayMode = true;
            }
        }

        /// <summary>
        /// Displays game over screen.
        /// </summary>
        public void GameEnded(int score)
        {
            CursorHelper.ShowCursor();

            gameplayUI.SetActive(false);
            gameOverUI.SetActive(true);
            gameOverScoreText.text = "Score: " + score;
        }

        /// <summary>
        /// Closes the application.
        /// </summary>
        public static void CloseApplication()
        {
            Application.Quit();
        }
    }
}