using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mesh;

namespace Loppy.Level
{
    // Data for each pattern node imported from the editor
    [Serializable]
    public class DataNode : IDisposable
    {
        public int id;
        public string name;
        public string region;
        public string type;
        public bool terminal;
        public int entranceCount;
        public List<int> connections;
        public Vector2 editorPosition;

        public bool visited = false;

        private bool disposed = false;

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposed) return;

            //if (disposing) // TODO: dispose managed state (managed objects).

            connections.Clear();
            name = null;
            region = null;
            type = null;

            disposed = true;
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
    public class RoomNode : IDisposable//, IEquatable<RoomNode>
    {
        public string type;
        public bool terminal;
        public List<RoomNode> connectedNodes;

        // Room data
        public RoomPrefabData roomPrefabData = null;
        public Vector2 roomCenter = Vector2.zero;
        public GameObject roomGameObject = null;

        // Parsing
        public bool visited = false;
        public RoomNode parentNode;
        public RoomEntrance parentExit = null;
        public RoomEntrance entrance = null;
        public List<RoomEntrance> openExits;

        public bool disposed = false;

        public RoomNode(string type, bool terminal, List<RoomNode> connectedNodes)
        {
            this.type = type;
            this.terminal = terminal;
            this.connectedNodes = new List<RoomNode>(connectedNodes);
            
            this.roomPrefabData = null;
            this.roomCenter = Vector2.zero;
            this.roomGameObject = null;

            this.visited = false;
            this.parentNode = null;
            this.parentExit = null;
            this.entrance = null;
            this.openExits = new List<RoomEntrance>();
        }

        public RoomNode(RoomNode other)
        {
            this.type = other.type;
            this.terminal = other.terminal;
            this.connectedNodes = new List<RoomNode>(other.connectedNodes);

            this.roomPrefabData = other.roomPrefabData;
            this.roomCenter = other.roomCenter;
            this.roomGameObject = null;

            this.visited = other.visited;
            this.parentNode = other.parentNode;
            this.parentExit = other.parentExit == null ? null : new RoomEntrance(other.parentExit);
            this.entrance = other.entrance == null ? null : new RoomEntrance(other.entrance);
            this.openExits = new List<RoomEntrance>(other.openExits);
        }

        /*
        public override bool Equals(object obj)
        {
            var other = obj as RoomNode;
            if (other == null) return false;

            return Equals(other);
        }

        public bool Equals(RoomNode other)
        {
            return (type == other.type &&
                    entranceCount == other.entranceCount &&
                    terminal == other.terminal &&
                    visited == other.visited &&
                    roomCenter == other.roomCenter &&
                    disposed == other.disposed);
        }
        */

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposed) return;

            //if (disposing) // TODO: dispose managed state (managed objects).

            type = null;
            connectedNodes.Clear();
            roomPrefabData = null;
            roomGameObject = null;
            parentExit = null;
            entrance = null;
            openExits.Clear();
            openExits = null;

            disposed = true;
        }
    }
}
