using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Loppy.Level
{
    [Serializable]
    public class NodeData
    {
        public int id;
        public string name;
        public string region;
        public string type;
        public bool terminal;
        public int entranceCount;
        public List<int> connections;
        public Vector2 editorPosition;

        public NodeData(int id, string name, string region, string type, bool terminal, int entranceCount, List<int> connections, Vector2 editorPosition)
        {
            this.id = id;
            this.name = name;
            this.region = region;
            this.type = type;
            this.terminal = terminal;
            this.entranceCount = entranceCount;
            this.connections = connections;
            this.editorPosition = editorPosition;
        }

        public NodeData(NodeData other)
        {
            this.id = other.id;
            this.name = other.name;
            this.region = other.region;
            this.type = other.type;
            this.terminal = other.terminal;
            this.entranceCount = other.entranceCount;
            this.connections = other.connections;
            this.editorPosition = other.editorPosition;
        }
    }

    [Serializable]
    public class PatternData
    {
        public string name;
        public List<NodeData> data;
    }

    public class LevelGenerator : MonoBehaviour
    {
        public static LevelGenerator instance;

        #region Inspector members

        public string editorDataPath;

        //public LevelGenerationData levelGenerationData;
        public List<GameObject> roomPrefabs;
        public List<RoomPatternRule> rulesList;

        #endregion

        public int randomSeed;

        private Dictionary<string, Dictionary<string, List<RoomData>>> roomDictionary;
        private Dictionary<string, List<List<NodeData>>> patternDictionary;

        private RoomDataNode roomTree;

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Initialize storage
            roomDictionary = new Dictionary<string, Dictionary<string, List<RoomData>>>();
            patternDictionary = new Dictionary<string, List<List<NodeData>>>();

            loadPatterns();
        }

        public void readJsonFiles(string path)
        {
            string[] files = Directory.GetFiles(path, "*.json");
            foreach (string file in files)
            {
                // Read json
                string jsonString = File.ReadAllText(file);
                PatternData newPatternData = new PatternData();
                newPatternData = JsonUtility.FromJson<PatternData>(jsonString);

                // Make sure the read data exists
                if (newPatternData.data == null)
                {
                    Debug.Log("Failed to load json file: " + file);
                    continue;
                }
                if (newPatternData.data.Count == 0)
                {
                    Debug.Log("Empty json file: " + file);
                    continue;
                }

                // Create list if it does not exist
                if (!patternDictionary.ContainsKey(newPatternData.name)) patternDictionary[newPatternData.name] = new List<List<NodeData>>();

                patternDictionary[newPatternData.name].Add(newPatternData.data);
            }
        }

        // Read all json files and add them into pattern dictionary
        public void loadPatterns()
        {
            // Clear patterns list
            patternDictionary.Clear();

            // Load json files
            // Region folders
            string[] regionDirectories = Directory.GetDirectories(editorDataPath);
            foreach (string regionDirectory in regionDirectories)
            {
                // Read level pattern file
                readJsonFiles(regionDirectory);

                // Type folders
                string[] typeDirectories = Directory.GetDirectories(regionDirectory);
                foreach (string typeDirectory in typeDirectories)
                {
                    // Read all files
                    readJsonFiles(typeDirectory);
                }
            }

            /*
            // Debug output
            foreach (KeyValuePair<string, List<List<NodeData>>> entry in patternDictionary)
            {
                string debugOutput = entry.Key;
                foreach (List<NodeData> dataList in entry.Value)
                {
                    foreach (NodeData data in dataList)
                    {
                        debugOutput += " [" + data.name + " " + data.entranceCount + "]";
                    }
                }
                Debug.Log(debugOutput);
            }
            */
        }
    }
}
