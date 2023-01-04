using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    [CreateAssetMenu]
    public class PlayerUnlocks : ScriptableObject
    {
        // Wall
        public bool wallClimbUnlocked = false;

        // Jump
        public int airJumps = 0;

        // Dash
        public bool dashUnlocked = false;
        public bool directionalDashUnlocked = false;
        public int airDashes = 1;

        // Glide
        public bool glideUnlocked = false;
    }
}
