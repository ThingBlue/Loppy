using Loppy.GameCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.UI
{
    public class SettingsManager : MonoBehaviour
    {
        // Singleton
        public static SettingsManager instance;

        #region Inspector members

        public SettingsData settingsData;

        #endregion

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Apply settings at the start of the game
            applyGraphicsSettings();
            applyAudioSettings();
            applyControlsSettings();
        }

        #region Apply settings

        public void applyGraphicsSettings()
        {
            Screen.SetResolution(settingsData.resolution.x, settingsData.resolution.y, settingsData.fullScreenMode, settingsData.refreshRate);
            Time.fixedDeltaTime = (1f / settingsData.targetFrameRate);
            Screen.brightness = settingsData.brightness;
        }

        public void applyAudioSettings()
        {

        }

        public void applyControlsSettings()
        {
            // Clear current key binds
            InputManager.instance.clearKeyMap();

            // Add every key bind in list to input manger
            for (int i = 0; i < settingsData.keyBinds.Count; i++)
            {
                InputManager.instance.setKeyListInMap(settingsData.keyBinds[i].name, settingsData.keyBinds[i].keys);
            }
        }

        #endregion

        #region Apply defaults

        public void applyGraphicsDefaults()
        {
            settingsData.resolution = new(1920, 1080);
            settingsData.refreshRate = 60;
            settingsData.fullScreenMode = FullScreenMode.FullScreenWindow;
            settingsData.targetFrameRate = 60;
            settingsData.brightness = 100;

            applyGraphicsSettings();
        }

        public void applyAudioDefaults()
        {
            settingsData.masterVolume = 50;
            settingsData.musicVolume = 50;
            settingsData.soundVolume = 50;

            applyAudioSettings();
        }

        public void applyControlsDefaults()
        {
            settingsData.keyBinds.Clear();
            InputManager.instance.clearKeyMap();

            // Game controls
            settingsData.keyBinds.Add(new KeyBind("up", new List<KeyCode> { KeyCode.W, KeyCode.UpArrow }));
            settingsData.keyBinds.Add(new KeyBind("down", new List<KeyCode> { KeyCode.S, KeyCode.DownArrow }));
            settingsData.keyBinds.Add(new KeyBind("left", new List<KeyCode> { KeyCode.A, KeyCode.LeftArrow }));
            settingsData.keyBinds.Add(new KeyBind("right", new List<KeyCode> { KeyCode.D, KeyCode.RightArrow }));
            settingsData.keyBinds.Add(new KeyBind("jump", new List<KeyCode> { KeyCode.Space }));
            settingsData.keyBinds.Add(new KeyBind("dash", new List<KeyCode> { KeyCode.LeftShift }));
            settingsData.keyBinds.Add(new KeyBind("glide", new List<KeyCode> { KeyCode.LeftControl }));
            settingsData.keyBinds.Add(new KeyBind("grapple", new List<KeyCode> { KeyCode.Mouse1 }));
            settingsData.keyBinds.Add(new KeyBind("alternateGrapple", new List<KeyCode> { KeyCode.E }));

            // Menu controls
            settingsData.keyBinds.Add(new KeyBind("pause", new List<KeyCode> { KeyCode.Escape }));

            // DEBUG
            settingsData.keyBinds.Add(new KeyBind("advanceDialogue", new List<KeyCode> { KeyCode.Mouse0 }));
            settingsData.keyBinds.Add(new KeyBind("startDialogue", new List<KeyCode> { KeyCode.L }));

            applyControlsSettings();
        }

        #endregion
    }
}
