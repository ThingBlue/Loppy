using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loppy.Level
{
    [Serializable]
    public class RoomDataNode
    {
        public string type;
        public List<RoomDataNode> children;
    }

    public enum EntranceDirection
    {
        NONE = 0,
        LEFT,
        RIGHT,
        TOP,
        BOTTOM
    }

    [Serializable]
    public class RoomData
    {
        public string name;
        public GameObject roomPrefab;
        public Vector2 size;
        public Vector2 entrance;
        public EntranceDirection entranceDirection;
        public int exitCount;
        public List<Vector2> exits;
        public List<EntranceDirection> exitDirections;
    }

    [Serializable]
    public class RoomType
    {
        public string type;
        public List<RoomData> rooms;
    }

    [Serializable]
    public class RoomRegion
    {
        public string region;
        public List<RoomType> types;
    }

    public class LevelGenerator : MonoBehaviour
    {
        #region Inspector members

        public List<RoomRegion> roomRegions;
        public LevelGraphs levelGraphs;

        #endregion

        public int randomSeed;

        private List<KeyValuePair<RoomData, Vector2>> generatedRooms;
        private List<GameObject> generatedRoomGameObjects;

        private void Start()
        {
            // Initialize generated room lists
            generatedRooms = new List<KeyValuePair<RoomData, Vector2>>();
            generatedRoomGameObjects = new List<GameObject>();

            // Generate test level
            if (!generateLevel(levelGraphs.testLevel, "test"))
            {
                Debug.Log("Failed to generate level");
            }
        }

        // Repeatedly attempts to generate the level
        // Returns true if level generation successful
        // Returns false if level generation fails 10 times
        public bool generateLevel(RoomDataNode levelData, string region)
        {
            int failureCount = 0;

            // Find region index
            int regionIndex = 0;
            for (int i = 0; i < roomRegions.Count; i++)
            {
                if (roomRegions[i].region == region)
                {
                    regionIndex = i;
                    break;
                }
            }

            // Try to generate level
            while (failureCount < 10)
            {
                if (generateRoom(levelData, Vector2.zero, EntranceDirection.LEFT, regionIndex)) return true;

                deleteGeneratedRooms();
                failureCount++;
            }
            return false;
        }

        // Recursively generates rooms using the level data node given
        private bool generateRoom(RoomDataNode node, Vector2 entrancePosition, EntranceDirection entranceDirection, int regionIndex)
        {
            if (entranceDirection == EntranceDirection.NONE) return false;

            UnityEngine.Random.InitState(randomSeed);

            // Find type index
            int typeIndex = 0;
            for (int i = 0; i < roomRegions[regionIndex].types.Count; i++)
            {
                if (roomRegions[regionIndex].types[i].type == node.type)
                {
                    typeIndex = i;
                    break;
                }
            }

            // Randomly generate room
            List<int> usedIndices = new List<int>();
            while (usedIndices.Count < roomRegions[regionIndex].types[typeIndex].rooms.Count)
            {
                // Generate new random index
                int randomIndex = UnityEngine.Random.Range(0, roomRegions[regionIndex].types[typeIndex].rooms.Count);

                // Check if index has already been used
                if (usedIndices.Contains(randomIndex)) continue;

                // Index has not been used yet
                // Add it to used indices
                usedIndices.Add(randomIndex);

                // Check if room fits
                RoomData roomData = roomRegions[regionIndex].types[typeIndex].rooms[randomIndex];
                // Check if entrance direction matches exit direction of last room
                if (roomData.entranceDirection != entranceDirection) continue;
                // Check if room overlaps any previous rooms
                Vector2 roomCenter = entrancePosition - roomData.entrance;
                bool overlap = false;
                foreach (KeyValuePair<RoomData, Vector2> existingRoom in generatedRooms)
                {
                    if (Mathf.Abs(roomCenter.x - existingRoom.Value.x) * 2 < (roomData.size.x + existingRoom.Key.size.x) &&
                        Mathf.Abs(roomCenter.y - existingRoom.Value.y) * 2 < (roomData.size.y + existingRoom.Key.size.y))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap) continue;

                // Room fits, generate it
                GameObject newRoomGameObject = GameObject.Instantiate(roomRegions[regionIndex].types[typeIndex].rooms[randomIndex].roomPrefab, roomCenter, Quaternion.identity);
                generatedRooms.Add(new KeyValuePair<RoomData, Vector2>(roomData, roomCenter));
                generatedRoomGameObjects.Add(newRoomGameObject);

                // Recurse
                for (int i = 0; i < node.children.Count; i++)
                {
                    // Calculate position of exit for current room
                    Vector2 exitPosition = roomCenter + roomData.exits[i];

                    // Calculate entrance direction of next room
                    EntranceDirection nextEntranceDirection = EntranceDirection.NONE;
                    if (roomData.exitDirections[i] == EntranceDirection.LEFT) nextEntranceDirection = EntranceDirection.RIGHT;
                    if (roomData.exitDirections[i] == EntranceDirection.RIGHT) nextEntranceDirection = EntranceDirection.LEFT;
                    if (roomData.exitDirections[i] == EntranceDirection.TOP) nextEntranceDirection = EntranceDirection.BOTTOM;
                    if (roomData.exitDirections[i] == EntranceDirection.BOTTOM) nextEntranceDirection = EntranceDirection.TOP;

                    // Return false if any children fail
                    if (!generateRoom(node.children[i], exitPosition, nextEntranceDirection, regionIndex)) return false;
                }

                // All recursions successful, return true
                return true;
            }

            // No suitable rooms found
            return false;
        }

        private void deleteGeneratedRooms()
        {
            // Delete all generated room game objects
            foreach (GameObject roomGameObject in generatedRoomGameObjects) Destroy(roomGameObject);

            // Clear lists
            generatedRooms.Clear();
            generatedRoomGameObjects.Clear();
        }
    }
}
