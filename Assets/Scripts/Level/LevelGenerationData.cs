using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    [CreateAssetMenu]
    public class LevelGenerationData : ScriptableObject
    {
        [Header("LEVELS")]
        public RoomDataNode testLevel;
    }
}
