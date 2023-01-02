using Loppy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
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

        private void Update()
        {

        }

        public void applyGraphicsSettings()
        {
            Screen.SetResolution(gameSettings.resolution.x, gameSettings.resolution.y, gameSettings.fullScreenMode, gameSettings.refreshRate);
            Time.fixedDeltaTime = (1f / gameSettings.targetFps);
            Screen.brightness = gameSettings.brightness;
        }

        public void applyAudioSettings()
        {
            
        }

        public void applyControlsSettings()
        {
            foreach (KeyCode keyBind in gameSettings.upKeyBinds) InputManager.instance.addKeyToMap("up", keyBind);
            foreach (KeyCode keyBind in gameSettings.downKeyBinds) InputManager.instance.addKeyToMap("down", keyBind);
            foreach (KeyCode keyBind in gameSettings.leftKeyBinds) InputManager.instance.addKeyToMap("left", keyBind);
            foreach (KeyCode keyBind in gameSettings.rightKeyBinds) InputManager.instance.addKeyToMap("right", keyBind);
            foreach (KeyCode keyBind in gameSettings.jumpKeyBinds) InputManager.instance.addKeyToMap("jump", keyBind);
            foreach (KeyCode keyBind in gameSettings.dashKeyBinds) InputManager.instance.addKeyToMap("dash", keyBind);
            foreach (KeyCode keyBind in gameSettings.glideKeyBinds) InputManager.instance.addKeyToMap("glide", keyBind);
        }
    }
}
