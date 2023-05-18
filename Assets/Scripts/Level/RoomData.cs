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
    public class RoomData : MonoBehaviour
    {
        public string region;
        public string type;
        public string name;
        public GameObject roomPrefab;
        public Vector2 size;
        public Vector2 entrance;
        public EntranceDirection entranceDirection;
        public int exitCount;
        public List<Vector2> exits;
        public List<EntranceDirection> exitDirections;
    }
}
