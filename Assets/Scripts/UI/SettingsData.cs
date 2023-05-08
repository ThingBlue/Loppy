using Loppy.GameCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.UI
{
    [CreateAssetMenu]
    public class SettingsData : ScriptableObject
    {
        [Header("GRAPHICS")]
        public Vector2Int resolution = new(1920, 1080);
        public int refreshRate = 60;
        public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
        public float targetFrameRate = 60;
        public float brightness = 100;

        [Header("AUDIO")]
        public float masterVolume = 50;
        public float musicVolume = 50;
        public float soundVolume = 50;

        [Header("CONTROLS")]
        public List<KeyBind> keyBinds;

        /*
        [Header("CONTROLS")]
        public List<KeyCode> upKeyBinds = new List<KeyCode> { KeyCode.W, KeyCode.UpArrow };
        public List<KeyCode> downKeyBinds = new List<KeyCode> { KeyCode.S, KeyCode.DownArrow };
        public List<KeyCode> leftKeyBinds = new List<KeyCode> { KeyCode.A, KeyCode.LeftArrow };
        public List<KeyCode> rightKeyBinds = new List<KeyCode> { KeyCode.D, KeyCode.RightArrow };
        public List<KeyCode> jumpKeyBinds = new List<KeyCode> { KeyCode.Space };
        public List<KeyCode> dashKeyBinds = new List<KeyCode> { KeyCode.LeftShift };
        public List<KeyCode> glideKeyBinds = new List<KeyCode> { KeyCode.LeftControl };
        public List<KeyCode> grappleKeyBinds = new List<KeyCode> { KeyCode.Mouse1 };
        public List<KeyCode> alternateGrappleKeyBinds = new List<KeyCode> { KeyCode.E };

        public List<KeyCode> pauseKeyBinds = new List<KeyCode> { KeyCode.Escape };

        public List<KeyCode> advanceDialogueKeyBinds = new List<KeyCode> { KeyCode.Mouse0 };
        public List<KeyCode> startDialogueKeyBinds = new List<KeyCode> { KeyCode.L };
        */
    }
}
