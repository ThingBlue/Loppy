using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

namespace Loppy.Level
{
    [Serializable]
    public class RoomDataNode
    {
        public string type;
        public List<RoomDataNode> children;
    }

    public class LevelGenerator : MonoBehaviour
    {
        #region Inspector members

        public LevelGenerationData levelGenerationData;
        public List<GameObject> roomPrefabs;

        #endregion

        public int randomSeed;

        private Dictionary<string, Dictionary<string, List<RoomData>>> rooms;
        private List<KeyValuePair<RoomData, Vector2>> generatedRooms;
        private List<GameObject> generatedRoomGameObjects;

        private void Start()
        {
            // Initialize storage
            rooms = new Dictionary<string, Dictionary<string, List<RoomData>>>();
            generatedRooms = new List<KeyValuePair<RoomData, Vector2>>();
            generatedRoomGameObjects = new List<GameObject>();

            // Fetch and initialize rooms
            if (!initializeRooms()) Debug.Log("Could not initialize rooms!");

            // Generate test level
            if (!generateLevel(levelGenerationData.testLevel, "test"))
            {
                Debug.Log("Failed to generate level");
            }
        }

        private bool initializeRooms()
        {
            foreach (GameObject roomPrefab in roomPrefabs)
            {
                RoomData prefabData = roomPrefab.GetComponent<RoomData>();
                prefabData.name = roomPrefab.name;
                prefabData.roomPrefab = roomPrefab;

                // Create storage structures if they do not exist
                if (!rooms.ContainsKey(prefabData.region)) rooms[prefabData.region] = new Dictionary<string, List<RoomData>>();
                if (!rooms[prefabData.region].ContainsKey(prefabData.type)) rooms[prefabData.region][prefabData.type] = new List<RoomData>();

                // Add to dictionary
                rooms[prefabData.region][prefabData.type].Add(prefabData);
            }

            return true;
        }

        // Repeatedly attempts to generate the level
        // Returns true if level generation successful
        // Returns false if level generation fails 10 times
        public bool generateLevel(RoomDataNode levelData, string region)
        {
            int failureCount = 0;

            // Try to generate level
            while (failureCount < 10)
            {
                if (generateRoom(levelData, Vector2.zero, EntranceDirection.LEFT, region)) return true;

                deleteGeneratedRooms();
                failureCount++;
            }
            return false;
        }

        // Recursively generates rooms using the level data node given
        private bool generateRoom(RoomDataNode node, Vector2 entrancePosition, EntranceDirection entranceDirection, string region)
        {
            if (entranceDirection == EntranceDirection.NONE) return false;

            UnityEngine.Random.InitState(randomSeed);

            // Randomly generate room
            List<int> usedIndices = new List<int>();
            while (usedIndices.Count < rooms[region][node.type].Count)
            {
                // Generate new random index
                int randomIndex = UnityEngine.Random.Range(0, rooms[region][node.type].Count);
                // Check if index has already been used
                if (usedIndices.Contains(randomIndex)) continue;

                // Index has not been used yet
                // Add it to used indices
                usedIndices.Add(randomIndex);

                // Get information about room
                RoomData roomData = rooms[region][node.type][randomIndex];
                Vector2 roomCenter = entrancePosition - roomData.entrance;

                // Validate room
                // Check if entrance direction matches exit direction of last room
                if (roomData.entranceDirection != entranceDirection) continue;
                // Check if room overlaps any existing rooms
                if (checkRoomOverlap(roomData, roomCenter)) continue;

                // Room fits, generate it
                GameObject newRoomGameObject = Instantiate(rooms[region][node.type][randomIndex].roomPrefab, roomCenter, Quaternion.identity);
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
                    if (!generateRoom(node.children[i], exitPosition, nextEntranceDirection, region)) return false;
                }

                // All recursions successful, return true
                return true;
            }

            // No suitable rooms found
            return false;
        }

        private bool checkRoomOverlap(RoomData roomData, Vector2 roomCenter)
        {
            // Check if room overlaps any previous rooms
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
            return overlap;
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
