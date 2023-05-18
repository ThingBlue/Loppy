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
        public RoomData roomData;
        public Vector2 roomCenter;
        public GameObject gameObject = null;
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

        private void Start()
        {
            // Initialize storage
            rooms = new Dictionary<string, Dictionary<string, List<RoomData>>>();

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
        public bool generateLevel(RoomDataNode root, string region)
        {
            int failureCount = 0;

            // Try to generate level
            while (failureCount < 10)
            {
                //if (generateRoom(levelData, Vector2.zero, EntranceDirection.LEFT, region)) return true;
                if (generateRoom(root, Vector2.right * failureCount * 5, EntranceDirection.LEFT, region, root)) return true;

                //deleteGeneratedRooms();
                failureCount++;
                //randomSeed++;
            }
            return false;
        }

        // Recursively generates rooms using the level data node given
        private bool generateRoom(RoomDataNode node, Vector2 entrancePosition, EntranceDirection entranceDirection, string region, RoomDataNode root)
        {
            if (entranceDirection == EntranceDirection.NONE) return false;

            //UnityEngine.Random.InitState(randomSeed);

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
                if (checkRoomOverlap(roomData, roomCenter, root)) continue;

                // Room fits, generate it
                GameObject newRoomGameObject = Instantiate(rooms[region][node.type][randomIndex].roomPrefab, roomCenter, Quaternion.identity);
                node.gameObject = newRoomGameObject;
                node.roomData = roomData;
                node.roomCenter = roomCenter;

                // Recurse
                bool childrenSuccess = true;
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
                    if (!generateRoom(node.children[i], exitPosition, nextEntranceDirection, region, root))
                    {
                        childrenSuccess = false;
                        break;
                    }
                }

                // All recursions successful, return true
                if (childrenSuccess) return true;

                // Try again if any children fail
                deleteGeneratedRooms(node);
                node.gameObject = null;
            }

            // No suitable rooms found
            return false;
        }

        private bool checkRoomOverlap(RoomData roomData, Vector2 roomCenter, RoomDataNode node)
        {
            // Check if current node has no instantiated room
            if (!node.gameObject) return false;

            // Check for overlap with current room node
            if (Mathf.Abs(roomCenter.x - node.roomCenter.x) * 2 < (roomData.size.x + node.roomData.size.x) &&
                Mathf.Abs(roomCenter.y - node.roomCenter.y) * 2 < (roomData.size.y + node.roomData.size.y))
            {
                return true;
            }

            // Recurse
            bool childrenOverlap = false;
            foreach (RoomDataNode child in node.children)
            {
                if (checkRoomOverlap(roomData, roomCenter, child))
                {
                    childrenOverlap = true;
                    break;
                }
            }

            return childrenOverlap;
        }

        private void deleteGeneratedRooms(RoomDataNode node)
        {
            // End recursion if current node has no instantiated room
            if (!node.gameObject) return;

            // Delete room of current node
            if (node.gameObject) Destroy(node.gameObject);
            node.gameObject = null;
            node.roomData = null;
            node.roomCenter = Vector2.zero;

            // Recursively destroy children rooms
            foreach (RoomDataNode child in node.children) deleteGeneratedRooms(child);
        }
    }
}
