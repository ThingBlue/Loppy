using PlasticPipe.PlasticProtocol.Messages.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Serialization.NodeDeserializers;
using UnityEngine;

namespace Loppy.Level
{
    public class LevelDataNode
    {
        public string value;
        public List<LevelDataNode> children;
    }

    [Serializable]
    public class RoomData
    {
        public string region;
        public string type;
        public string name;
        public GameObject roomPrefab;
        public Vector2 size;
        public Vector2 entrance;
        public List<Vector2> exits;
    }

    [Serializable]
    public class RoomTypes
    {
        public string type;
        public List<RoomData> rooms;
    }

    [Serializable]
    public class RoomRegions
    {
        public string region;
        public List<RoomTypes> types;
    }

    public class LevelGenerator : MonoBehaviour
    {
        public List<RoomData> roomData;
        public List<RoomRegions> roomRegions;

        public void generateLevel(LevelDataNode levelData)
        {
            int failureCount = 0;
            while (!tryGenerateLevel(levelData) && failureCount < 10)
            {
                failureCount++;
            }
        }

        private bool tryGenerateLevel(LevelDataNode levelData)
        {
            for (int i = 0; i < levelData.children.Count; i++)
            {
                if (generateRoom(levelData.children[i]))
                {

                }
                // Could not find a room that suits the space requirements
                else
                {
                    // Failure
                    return false;
                }
            }

            // Success
            return true;
        }

        private bool generateRoom(LevelDataNode levelData)
        {


            return true;
        }
    }
}
