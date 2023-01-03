using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Loppy
{
    public class DebugCanvasController : MonoBehaviour
    {
        public PlayerController player;
        public TMP_Text gameStateText;
        public TMP_Text playerStateText;

        private void Update()
        {
            gameStateText.text = $"Game State: {GameManager.gameState}";
            playerStateText.text = $"Player State: {player.playerState}";
        }
    }
}
