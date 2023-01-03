using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Loppy
{
    public class UIManager : MonoBehaviour
    {
        // Singleton
        public static UIManager instance;

        #region Inspector members

        public GameObject pauseMenuCanvas;

        public GameObject pauseMenuPanel;
        public Button continueButton;
        public Button settingsButton;
        public Button exitToMenuButton;

        public GameObject settingsMenuPanel;
        public Button backButton;

        #endregion

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Disable menus
            pauseMenuCanvas.SetActive(false);
        }

        #region Pause menu

        public void togglePause(bool pause)
        {
            if (pause)
            {
                pauseMenuCanvas.SetActive(true);
                pauseMenuPanel.SetActive(true);
                settingsMenuPanel.SetActive(false);
            }
            else
            {
                pauseMenuCanvas.SetActive(false);
            }
        }

        public void onContinueButtonPressed() { GameManager.instance.togglePause(false); }

        public void onSettingsButtonPressed()
        {
            pauseMenuPanel.SetActive(false);
            settingsMenuPanel.SetActive(true);
        }

        public void onExitToMenuButtonPressed()
        {

        }

        #endregion

        #region Settings menu

        public void onBackButtonPressed()
        {
            settingsMenuPanel.SetActive(false);
            pauseMenuPanel.SetActive(true);
        }

        #endregion
    }
}
