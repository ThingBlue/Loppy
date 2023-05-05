using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.GameCore
{
    public enum GameState
    {
        NONE = 0,
        MAIN_MENU,
        LOADING,
        GAME,
        PAUSED
    }

    public class GameManager : MonoBehaviour
    {
        // Singleton
        public static GameManager instance;

        public static GameState gameState = GameState.NONE;
        public GameSettings gameSettings;

        private float timeScaleBeforePause = 0;

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);

            // TEMP TEMP TEMP TEMP TEMP TEMP TEMP TEMP TEMP TEMP
            gameState = GameState.GAME;
        }

        private void Start()
        {
            // Subscribe to events
            EventManager.instance.pauseEvent.AddListener(onPause);
            EventManager.instance.unpauseEvent.AddListener(onUnpause);

            // Apply settings at the start of the game
            applyGraphicsSettings();
            applyAudioSettings();
            applyControlsSettings();
        }

        private void Update()
        {
            // Pause
            if (gameState == GameState.GAME && InputManager.instance.getKeyDown("pause")) EventManager.instance.pauseEvent.Invoke();
            // Unpause
            else if (gameState == GameState.PAUSED && InputManager.instance.getKeyDown("pause")) EventManager.instance.unpauseEvent.Invoke();
        }

        #region Apply settings

        public void applyGraphicsSettings()
        {
            Screen.SetResolution(gameSettings.resolution.x, gameSettings.resolution.y, gameSettings.fullScreenMode, gameSettings.refreshRate);
            Time.fixedDeltaTime = (1f / gameSettings.targetFrameRate);
            Screen.brightness = gameSettings.brightness;
        }

        public void applyAudioSettings()
        {
            
        }

        public void applyControlsSettings()
        {
            for (int i = 0; i < gameSettings.keyBinds.Count; i++)
            {
                foreach (KeyCode keyCode in gameSettings.keyBinds[i].keys)
                {
                    InputManager.instance.addKeyToMap(gameSettings.keyBinds[i].name, keyCode);
                }
            }
        }

        #endregion

        #region Apply defaults

        public void applyGraphicsDefaults()
        {
            gameSettings.resolution = new(1920, 1080);
            gameSettings.refreshRate = 60;
            gameSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            gameSettings.targetFrameRate = 60;
            gameSettings.brightness = 100;

            applyGraphicsSettings();
        }

        public void applyAudioDefaults()
        {
            gameSettings.masterVolume = 50;
            gameSettings.musicVolume = 50;
            gameSettings.soundVolume = 50;

            applyAudioSettings();
        }

        public void applyControlsDefaults()
        {
            gameSettings.keyBinds.Clear();
            InputManager.instance.clearKeyMap();

            // Game controls
            gameSettings.keyBinds.Add(new KeyBind("up", new List<KeyCode> { KeyCode.W, KeyCode.UpArrow }));
            gameSettings.keyBinds.Add(new KeyBind("down", new List<KeyCode> { KeyCode.S, KeyCode.DownArrow }));
            gameSettings.keyBinds.Add(new KeyBind("left", new List<KeyCode> { KeyCode.A, KeyCode.LeftArrow }));
            gameSettings.keyBinds.Add(new KeyBind("right", new List<KeyCode> { KeyCode.D, KeyCode.RightArrow }));
            gameSettings.keyBinds.Add(new KeyBind("jump", new List<KeyCode> { KeyCode.Space }));
            gameSettings.keyBinds.Add(new KeyBind("dash", new List<KeyCode> { KeyCode.LeftShift }));
            gameSettings.keyBinds.Add(new KeyBind("glide", new List<KeyCode> { KeyCode.LeftControl }));
            gameSettings.keyBinds.Add(new KeyBind("grapple", new List<KeyCode> { KeyCode.Mouse1 }));
            gameSettings.keyBinds.Add(new KeyBind("alternateGrapple", new List<KeyCode> { KeyCode.E }));

            // Menu controls
            gameSettings.keyBinds.Add(new KeyBind("pause", new List<KeyCode> { KeyCode.Escape }));

            applyControlsSettings();
        }

        #endregion

        #region Event system callbacks

        private void onPause()
        {
            gameState = GameState.PAUSED;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0;
        }

        private void onUnpause()
        {
            gameState = GameState.GAME;
            Time.timeScale = timeScaleBeforePause;
        }

        #endregion

    }
}
