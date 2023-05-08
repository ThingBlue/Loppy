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
        }

        private void Update()
        {
            // Pause
            if (gameState == GameState.GAME && InputManager.instance.getKeyDown("pause")) EventManager.instance.pauseEvent.Invoke();
            // Unpause
            else if (gameState == GameState.PAUSED && InputManager.instance.getKeyDown("pause")) EventManager.instance.unpauseEvent.Invoke();
        }


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
