using Loppy.GameCore;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loppy.UI
{
    public class UIManager : MonoBehaviour
    {
        // Singleton
        public static UIManager instance;

        #region Inspector members

        public SettingsData settingsData;

        // Pause menu
        public GameObject pauseMenuPanel;
        public Button continueButton;
        public Button settingsButton;
        public Button exitToMenuButton;

        // Settings menu
        public GameObject settingsMenuPanel;
        public Button backButton;
        // Graphics tab
        public GameObject graphicsTabPanel;
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown refreshRateDropdown;
        public TMP_Dropdown fullScreenDropdown;
        public Slider brightnessSlider;
        public TMP_InputField brightnessInputField;
        public Button applyChangesGraphicsButton;
        public Button applyDefaultsGraphicsButton;
        // Audio tab
        public GameObject audioTabPanel;
        public Button applyChangesAudioButton;
        public Button applyDefaultsAudioButton;
        // Controls tab
        public GameObject controlsTabPanel;
        public Button applyChangesControlsButton;
        public Button applyDefaultsControlsButton;

        // Debug menu
        public GameObject debugMenuPanel;

        #endregion

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Subscribe to events
            EventManager.instance.pauseEvent.AddListener(onPause);
            EventManager.instance.unpauseEvent.AddListener(onUnpause);

            // Disable menus
            pauseMenuPanel.SetActive(false);
            settingsMenuPanel.SetActive(false);
        }

        #region Pause menu

        public void onContinueButtonPressed() { EventManager.instance.unpauseEvent.Invoke(); }

        public void onSettingsButtonPressed()
        {
            pauseMenuPanel.SetActive(false);
            settingsMenuPanel.SetActive(true);

            // Start up on graphics tab
            graphicsTabPanel.SetActive(true);
            audioTabPanel.SetActive(false);
            controlsTabPanel.SetActive(false);

            // Set display values for graphics tab
            setGraphicsTabDisplayValues();
        }

        public void onExitToMenuButtonPressed()
        {

        }

        public void onDebugButtonPressed()
        {
            pauseMenuPanel.SetActive(false);
            debugMenuPanel.SetActive(true);

            EventManager.instance.debugMenuOpened.Invoke();
        }

        public void onExitGameButtonPressed()
        {
            Application.Quit();
        }

        #endregion

        #region Settings menu

        #region Graphics tab

        public void setGraphicsTabDisplayValues()
        {
            // Resolution dropdown
            if (settingsData.resolution == new Vector2Int(2560, 1440)) resolutionDropdown.value = 0;
            if (settingsData.resolution == new Vector2Int(1920, 1080)) resolutionDropdown.value = 1;
            if (settingsData.resolution == new Vector2Int(1600, 900)) resolutionDropdown.value = 2;
            if (settingsData.resolution == new Vector2Int(1366, 768)) resolutionDropdown.value = 3;
            if (settingsData.resolution == new Vector2Int(1360, 768)) resolutionDropdown.value = 4;
            if (settingsData.resolution == new Vector2Int(1280, 720)) resolutionDropdown.value = 5;
            if (settingsData.resolution == new Vector2Int(1176, 664)) resolutionDropdown.value = 6;

            // Refresh rate dropdown
            switch (settingsData.refreshRate)
            {
                case 144:
                    refreshRateDropdown.value = 0;
                    break;
                case 120:
                    refreshRateDropdown.value = 1;
                    break;
                case 100:
                    refreshRateDropdown.value = 2;
                    break;
                case 60:
                    refreshRateDropdown.value = 3;
                    break;
                case 50:
                    refreshRateDropdown.value = 4;
                    break;
                default:
                    break;
            }

            // Full screen dropdown
            switch (settingsData.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                    fullScreenDropdown.value = 0;
                    break;
                case FullScreenMode.FullScreenWindow:
                    fullScreenDropdown.value = 1;
                    break;
                case FullScreenMode.Windowed:
                    fullScreenDropdown.value = 2;
                    break;
                default:
                    break;
            }

            // Brightness slider
            brightnessSlider.value = settingsData.brightness / 200f;

            // Brightness input field
            brightnessInputField.text = Mathf.Round(settingsData.brightness).ToString();
        }

        public void onGraphicsTabButtonPressed()
        {
            // Activate only current tab
            graphicsTabPanel.SetActive(true);
            audioTabPanel.SetActive(false);
            controlsTabPanel.SetActive(false);

            // Set display values
            setGraphicsTabDisplayValues();
        }

        public void onBrightnessSliderValueChanged(Slider change)
        {
            // Set input field value
            brightnessInputField.text = Mathf.Round(change.value * 200).ToString();
        }

        public void onBrightnessInputFieldEndEdit(TMP_InputField change)
        {
            // Set slider value
            brightnessSlider.value = int.Parse(change.text) / 200f;
        }

        public void onApplyChangesGraphicsButtonPressed()
        {
            // Resolution
            switch (resolutionDropdown.value)
            {
                case 0:
                    settingsData.resolution = new(2560, 1440);
                    break;
                case 1:
                    settingsData.resolution = new(1920, 1080);
                    break;
                case 2:
                    settingsData.resolution = new(1600, 900);
                    break;
                case 3:
                    settingsData.resolution = new(1366, 768);
                    break;
                case 4:
                    settingsData.resolution = new(1360, 768);
                    break;
                case 5:
                    settingsData.resolution = new(1280, 720);
                    break;
                case 6:
                    settingsData.resolution = new(1176, 664);
                    break;
                default:
                    break;
            }

            // Refresh rate
            switch (refreshRateDropdown.value)
            {
                case 0:
                    settingsData.refreshRate = 144;
                    break;
                case 1:
                    settingsData.refreshRate = 120;
                    break;
                case 2:
                    settingsData.refreshRate = 100;
                    break;
                case 3:
                    settingsData.refreshRate = 60;
                    break;
                case 4:
                    settingsData.refreshRate = 50;
                    break;
                default:
                    break;
            }

            // Full screen
            switch (fullScreenDropdown.value)
            {
                case 0:
                    settingsData.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    break;
                case 1:
                    settingsData.fullScreenMode = FullScreenMode.FullScreenWindow;
                    break;
                case 2:
                    settingsData.fullScreenMode = FullScreenMode.Windowed;
                    break;
                default:
                    break;
            }

            // Brightness
            settingsData.brightness = brightnessSlider.value * 200;

            // Apply changes
            SettingsManager.instance.applyGraphicsSettings();
        }

        public void onApplyDefaultsGraphicsButtonPressed()
        {
            SettingsManager.instance.applyGraphicsDefaults();
            setGraphicsTabDisplayValues();
        }

        #endregion

        #region Audio tab

        public void onAudioTabButtonPressed()
        {
            // Activate only current tab
            graphicsTabPanel.SetActive(false);
            audioTabPanel.SetActive(true);
            controlsTabPanel.SetActive(false);
        }

        public void onApplyChangesAudioButtonPressed()
        {

        }

        public void onApplyDefaultsAudioButtonPressed() { SettingsManager.instance.applyAudioDefaults(); }

        #endregion

        #region Controls tab

        public void onControlsTabButtonPressed()
        {
            // Activate only current tab
            graphicsTabPanel.SetActive(false);
            audioTabPanel.SetActive(false);
            controlsTabPanel.SetActive(true);
        }

        public void onApplyChangesControlsButtonPressed()
        {

        }

        public void onApplyDefaultsControlsButtonPressed() { SettingsManager.instance.applyControlsDefaults(); }

        public void onBackButtonPressed()
        {
            settingsMenuPanel.SetActive(false);
            pauseMenuPanel.SetActive(true);
        }

        #endregion

        #endregion

        #region Event system callbacks

        private void onPause()
        {
            // Show pause menu
            pauseMenuPanel.SetActive(true);
        }

        private void onUnpause()
        {
            // Hide menus
            pauseMenuPanel.SetActive(false);
            settingsMenuPanel.SetActive(false);
            debugMenuPanel.SetActive(false);
        }

        #endregion
    }
}
