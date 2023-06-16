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

        public RoomEntrance(Vector2 position, EntranceDirection direction)
        {
            this.position = position;
            this.direction = direction;
        }

        public RoomEntrance(RoomEntrance other)
        {
            this.position = other.position;
            this.direction = other.direction;
        }
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

        public RoomPrefabData(RoomPrefabData other)
        {
            this.name = other.name;
            this.region = other.region;
            this.type = other.type;

            this.roomPrefab = other.roomPrefab;
            this.size = other.size;

            this.entrances = new List<RoomEntrance>(other.entrances);
        }
    }
}
