using Loppy.GameCore;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;

namespace Loppy.UI
{
    public class SettingsManager : MonoBehaviour
    {
        // Singleton
        public static SettingsManager instance;

        #region Inspector members

        public SettingsData settingsData;
        public SettingsData defaultSettingsData;

        #endregion

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Subscribe to event callbacks
            EventManager.instance.settingsMenuOpenedEvent.AddListener(onSettingsOpened);

            // Get persistent path
            settingsData.settingsFilePath = Path.Combine(Application.persistentDataPath, "settings.json");

            // Check if settings json file exists
            if (!File.Exists(settingsData.settingsFilePath))
            {
                Debug.Log("Settings file does not exist");
                applyGraphicsDefaults();
                applyAudioDefaults();
                applyControlsDefaults();
                saveSettingsJson();
            }
            else
            {
                // Apply settings at the start of the game
                readSettingsJson();
                applyGraphicsSettings();
                applyAudioSettings();
                applyControlsSettings();
            }
        }

        #region Json

        private void saveSettingsJson()
        {
            // Write to file
            string jsonString = JsonUtility.ToJson(settingsData);
            File.WriteAllText(settingsData.settingsFilePath, jsonString);
        }


        private void readSettingsJson()
        {
            // Overwrite settings data with values stored in json
            string jsonString = File.ReadAllText(settingsData.settingsFilePath);
            JsonUtility.FromJsonOverwrite(jsonString, settingsData);
        }

        #endregion

        #region Apply settings

        public void applyGraphicsSettings()
        {
            // Apply to settings data
            Screen.SetResolution(settingsData.resolution.x, settingsData.resolution.y, settingsData.fullScreenMode, settingsData.refreshRate);
            Time.fixedDeltaTime = (1f / settingsData.targetFrameRate);
            Screen.brightness = settingsData.brightness;

            // Save to json file
            saveSettingsJson();
        }

        public void applyAudioSettings()
        {
            // Apply to settings data

            // Save to json file
            saveSettingsJson();
        }

        public void applyControlsSettings()
        {
            // Clear current key binds
            InputManager.instance.clearKeyMap();

            // Add every key bind in list to input manger
            foreach (KeyBind keyBind in settingsData.keyBinds)
            {
                InputManager.instance.setKeyListInMap(keyBind.name, keyBind.keys);
            }

            // Save to json file
            saveSettingsJson();
        }

        #endregion

        #region Apply defaults

        public void applyGraphicsDefaults()
        {
            /*
            settingsData.resolution = new(1920, 1080);
            settingsData.refreshRate = 60;
            settingsData.fullScreenMode = FullScreenMode.FullScreenWindow;
            settingsData.targetFrameRate = 60;
            settingsData.brightness = 100;
            */

            settingsData.resolution = defaultSettingsData.resolution;
            settingsData.refreshRate = defaultSettingsData.refreshRate;
            settingsData.fullScreenMode = defaultSettingsData.fullScreenMode;
            settingsData.targetFrameRate = defaultSettingsData.targetFrameRate;
            settingsData.brightness = defaultSettingsData.brightness;

            applyGraphicsSettings();
        }

        public void applyAudioDefaults()
        {
            /*
            settingsData.masterVolume = 50;
            settingsData.musicVolume = 50;
            settingsData.soundVolume = 50;
            */

            settingsData.masterVolume = defaultSettingsData.masterVolume;
            settingsData.musicVolume = defaultSettingsData.musicVolume;
            settingsData.soundVolume = defaultSettingsData.soundVolume;

            applyAudioSettings();
        }

        public void applyControlsDefaults()
        {
            /*
            // Game controls
            settingsData.defaultKeyBinds.Clear();
            settingsData.defaultKeyBinds.Add(new KeyBind("up", new List<KeyCode> { KeyCode.W, KeyCode.UpArrow }));
            settingsData.defaultKeyBinds.Add(new KeyBind("down", new List<KeyCode> { KeyCode.S, KeyCode.DownArrow }));
            settingsData.defaultKeyBinds.Add(new KeyBind("left", new List<KeyCode> { KeyCode.A, KeyCode.LeftArrow }));
            settingsData.defaultKeyBinds.Add(new KeyBind("right", new List<KeyCode> { KeyCode.D, KeyCode.RightArrow }));
            settingsData.defaultKeyBinds.Add(new KeyBind("jump", new List<KeyCode> { KeyCode.Space }));
            settingsData.defaultKeyBinds.Add(new KeyBind("dash", new List<KeyCode> { KeyCode.LeftShift }));
            settingsData.defaultKeyBinds.Add(new KeyBind("glide", new List<KeyCode> { KeyCode.LeftControl }));
            settingsData.defaultKeyBinds.Add(new KeyBind("grapple", new List<KeyCode> { KeyCode.Mouse1 }));
            settingsData.defaultKeyBinds.Add(new KeyBind("alternateGrapple", new List<KeyCode> { KeyCode.E }));

            // Menu controls
            settingsData.defaultKeyBinds.Add(new KeyBind("pause", new List<KeyCode> { KeyCode.Escape }));

            // DEBUG
            settingsData.defaultKeyBinds.Add(new KeyBind("advanceDialogue", new List<KeyCode> { KeyCode.Mouse0 }));
            settingsData.defaultKeyBinds.Add(new KeyBind("startDialogue", new List<KeyCode> { KeyCode.L }));
            */

            // Set default keys
            settingsData.keyBinds = defaultSettingsData.keyBinds;

            applyControlsSettings();
        }

        #endregion

        #region Event system callbacks

        private void onSettingsOpened()
        {
            readSettingsJson();

            // Set display values
            UIManager.instance.setGraphicsTabDisplayValues();
            //UIManager.instance.setAudioTabDisplayValues();
            //UIManager.instance.setControlsTabDisplayValues();
        }

        #endregion
    }
}
