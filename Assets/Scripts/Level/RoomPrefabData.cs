using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    public enum EntranceDirection
    {
        NONE = 0,
        LEFT,
        RIGHT,
        TOP,
        BOTTOM
    }

    [Serializable]
    public class RoomEntrance
    {
        public Vector2 position;
        public EntranceDirection direction;
    }

    // Data attached to each room prefab
    [Serializable]
    public class RoomPrefabData : MonoBehaviour
    {
        public string name;
        public string region;
        public string type;

        public GameObject roomPrefab;
        public Vector2 size;

        public List<RoomEntrance> entrances;
    }
}
