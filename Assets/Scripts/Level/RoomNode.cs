using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    public enum DecisionType
    {
        NONE = 0,
        PATTERN,
        EXIT,
        ROOM,
        ENTRANCE
    }

    // Data for each pattern node imported from the editor
    [Serializable]
    public class DataNode
    {
        public int id;
        public string name;
        public string region;
        public string type;
        public bool terminal;
        public int entranceCount;
        public List<int> connections;
        public Vector2 editorPosition;

        public DataNode(int id, string name, string region, string type, bool terminal, int entranceCount, List<int> connections, Vector2 editorPosition)
        {
            this.id = id;
            this.name = name;
            this.region = region;
            this.type = type;
            this.terminal = terminal;
            this.entranceCount = entranceCount;
            this.connections = new List<int>(connections);
            this.editorPosition = editorPosition;
        }

        public DataNode(DataNode other)
        {
            this.id = other.id;
            this.name = other.name;
            this.region = other.region;
            this.type = other.type;
            this.terminal = other.terminal;
            this.entranceCount = other.entranceCount;
            this.connections = new List<int>(other.connections);
            this.editorPosition = other.editorPosition;
        }
    }

    // Data for the pattern dictionary
    [Serializable]
    public class PatternData
    {
        public string name;
        public List<DataNode> data;
    }

    // Data for each node in the final room graph,
    //     including the generated gameObject
    [Serializable]
    public class RoomNode : IEquatable<RoomNode>
    {
        public string type;
        public int entranceCount;
        public List<RoomNode> connectedRooms;

        // Imported from DataNode
        public int id;
        public List<int> connections;
        public bool terminal;

        // Members that will be assigned after the best room to generate is determined
        public bool generated = false;
        public RoomPrefabData roomPrefabData = null;
        public Vector2 roomCenter = Vector2.zero;
        public GameObject roomGameObject = null;

        // Decision tree
        public DecisionType decisionType = DecisionType.NONE;
        public int parentId;
        public RoomNode parent = null;
        public RoomEntrance parentExit = null;
        public RoomEntrance entrance = null;

        // Temporary list of unused entrances to help with room generation
        public List<RoomEntrance> openExits;

        public RoomNode(string type, int entranceCount, int id, List<int> connections, bool terminal)
        {
            this.type = type;
            this.entranceCount = entranceCount;
            this.connectedRooms = new List<RoomNode>();

            this.id = id;
            this.connections = new List<int>(connections);
            this.terminal = terminal;

            this.generated = false;
            this.roomPrefabData = null;
            this.roomCenter = Vector2.zero;
            this.roomGameObject = null;

            this.decisionType = DecisionType.NONE;
            this.parentId = 0;
            this.parentExit = null;
            this.entrance = null;

            this.openExits = new List<RoomEntrance>();
        }

        public RoomNode(RoomNode other)
        {
            this.type = other.type;
            this.entranceCount = other.entranceCount;
            this.connectedRooms = new List<RoomNode>();

            this.id = other.id;
            this.connections = new List<int>(other.connections);
            this.terminal = other.terminal;

            this.generated = other.generated;
            this.roomPrefabData = other.roomPrefabData;
            this.roomCenter = other.roomCenter;
            this.roomGameObject = null;

            this.decisionType = other.decisionType;
            this.parentId = other.parentId;
            this.parent = other.parent;
            this.parentExit = other.parentExit == null ? null : new RoomEntrance(other.parentExit);
            this.entrance = other.entrance == null ? null : new RoomEntrance(other.entrance);

            this.openExits = new List<RoomEntrance>(other.openExits);
        }
        public bool Equals(RoomNode other)
        {
            return this.id == other.id;
        }
    }
}
