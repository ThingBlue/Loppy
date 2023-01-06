using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loppy
{
    public class DebugCanvasManager : MonoBehaviour
    {
        // Singleton
        public static DebugCanvasManager instance;

        public PlayerController player;
        public PlayerUnlocks playerUnlocks;

        public TMP_Text gameStateText;
        public TMP_Text playerStateText;

        public GameObject pauseMenuPanel;
        public GameObject debugMenuPanel;
        public Toggle wallClimbToggle;
        public Toggle grappleToggle;
        public Toggle dashToggle;
        public Toggle directionalDashToggle;
        public Toggle glideToggle;
        public TMP_InputField airJumpsInputField;
        public TMP_InputField airDashesInputField;
        public TMP_InputField grappleDistanceInputField;

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Disable menu
            debugMenuPanel.SetActive(false);
        }

        private void Update()
        {
            // Set value for state text
            gameStateText.text = $"Game State: {GameManager.gameState}";
            playerStateText.text = $"Player State: {player.playerState}";
        }

        public void onDebugMenuOpened()
        {
            // Set value for toggles
            wallClimbToggle.isOn = playerUnlocks.wallClimbUnlocked;
            grappleToggle.isOn = playerUnlocks.grappleUnlocked;
            dashToggle.isOn = playerUnlocks.dashUnlocked;
            directionalDashToggle.isOn = playerUnlocks.directionalDashUnlocked;
            glideToggle.isOn = playerUnlocks.glideUnlocked;

            // Set value for input fields
            airJumpsInputField.text = playerUnlocks.airJumps.ToString();
            airDashesInputField.text = playerUnlocks.airDashes.ToString();
            grappleDistanceInputField.text = playerUnlocks.grappleDistance.ToString();
        }

        public void onWallClingToggleChanged(Toggle change)
        {
            playerUnlocks.wallClimbUnlocked = change.isOn;
        }

        public void onAirJumpsInputFieldEndEdit(TMP_InputField change)
        {
            playerUnlocks.airJumps = int.Parse(change.text);
        }

        public void onDashToggleChanged(Toggle change)
        {
            playerUnlocks.dashUnlocked = change.isOn;
        }

        public void onDirectionalDashToggleChanged(Toggle change)
        {
            playerUnlocks.directionalDashUnlocked = change.isOn;
        }

        public void onAirDashesInputFieldEndEdit(TMP_InputField change)
        {
            playerUnlocks.airDashes = int.Parse(change.text);
        }

        public void onGlideToggleChanged(Toggle change)
        {
            playerUnlocks.glideUnlocked = change.isOn;
        }

        public void onGrappleToggleChanged(Toggle change)
        {
            playerUnlocks.grappleUnlocked = change.isOn;
        }

        public void onGrappleDistanceInputFieldEndEdit(TMP_InputField change)
        {
            playerUnlocks.grappleDistance = int.Parse(change.text);
        }

        public void onBackButtonPressed()
        {
            debugMenuPanel.SetActive(false);
            pauseMenuPanel.SetActive(true);
        }
    }
}
