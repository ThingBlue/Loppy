using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    [Serializable]
    public class RoomDataNode
    {
        public string type;
        public List<RoomDataNode> children;
    }

    [Serializable]
    public class RoomData
    {
        public string name;
        public GameObject roomPrefab;
        public Vector2 size;
        public Vector2 entrance;
        public int exitCount;
        public List<Vector2> exits;
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
        public LevelGenerationData levelGenerationData;

        #endregion

        public int randomSeed;

        private void Start()
        {
            if (!generateLevel(levelGenerationData.testLevel, "test"))
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
                if (generateRoom(levelData, Vector2.zero, regionIndex)) return true;

                failureCount++;
            }
            return false;
        }

        // Recursively generates rooms using the level data node given
        private bool generateRoom(RoomDataNode node, Vector2 entrancePosition, int regionIndex)
        {
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

                // Room fits, generate it
                // Find position to generate room
                Vector2 roomCenter = entrancePosition - roomData.entrance;
                GameObject.Instantiate(roomRegions[regionIndex].types[typeIndex].rooms[randomIndex].roomPrefab, roomCenter, Quaternion.identity);

                // Recurse
                for (int i = 0; i < node.children.Count; i++)
                {
                    // Calculate position of exit for current room
                    Vector2 exitPosition = roomCenter + roomData.exits[i];

                    // Return false if any children fail
                    if (!generateRoom(node.children[i], exitPosition, regionIndex)) return false;
                }

                // All recursions successful, return true
                return true;
            }
            return false; // Should never execute
        }
    }
}
