using Codice.Client.Common.TreeGrouper;
using JetBrains.Annotations;
using log4net.Core;
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

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

        public int nextId = 0;

        private Dictionary<string, Dictionary<string, List<RoomData>>> roomDictionary;
        private Dictionary<string, List<List<NodeData>>> patternDictionary;

        private List<NodeData> parseGraph;
        private List<NodeData> roomGraph;
        private List<GameObject> generatedRooms;

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
            parseGraph = new List<NodeData>();
            roomGraph = new List<NodeData>();
            generatedRooms = new List<GameObject>();

            StartCoroutine(loadPatterns());
        }

        // DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                StopAllCoroutines();

                // Generate test level
                StartCoroutine(generateLevel("basicTestLevel"));
            }
        }

        private void assignNewIds(List<NodeData> pattern)
        {
            foreach (NodeData nodeToReassign in pattern)
            {
                // Assign new id to current node
                int oldId = nodeToReassign.id;
                nodeToReassign.id = nextId;
                nextId++;

                // Update connections to use the same new id
                foreach (NodeData node in pattern)
                {
                    if (node.connections.Contains(oldId))
                    {
                        node.connections.Remove(oldId);
                        node.connections.Add(nodeToReassign.id);
                    }
                }
            }
        }

        private void readAllPatternJsonInDirectory(string path)
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

                // Update data with unique ids
                assignNewIds(newPatternData.data);

                // Add to pattern dictionary
                patternDictionary[newPatternData.name].Add(newPatternData.data);
            }
        }

        // Read all json files and add them into pattern dictionary
        private IEnumerator loadPatterns()
        {
            // Clear patterns list
            patternDictionary.Clear();

            // Load json files
            // Region folders
            string[] regionDirectories = Directory.GetDirectories(editorDataPath);
            foreach (string regionDirectory in regionDirectories)
            {
                // Read level pattern file
                readAllPatternJsonInDirectory(regionDirectory);

                // Type folders
                string[] typeDirectories = Directory.GetDirectories(regionDirectory);
                foreach (string typeDirectory in typeDirectories)
                {
                    // Read all files
                    readAllPatternJsonInDirectory(typeDirectory);
                }
            }

            
            // Debug output
            foreach (KeyValuePair<string, List<List<NodeData>>> entry in patternDictionary)
            {
                string debugOutput = entry.Key;
                foreach (List<NodeData> dataList in entry.Value)
                {
                    foreach (NodeData data in dataList)
                    {
                        debugOutput += " [" + data.name + " " + data.id + "]";
                    }
                }
                Debug.Log(debugOutput);
            }
            

            yield break;
        }

        private IEnumerator generateLevel(string level)
        {
            int failureCount = 0;

            // Assert that the level exists as a key in patterns
            if (!patternDictionary.ContainsKey(level))
            {
                Debug.LogError("Could not find specified level");
                yield break;
            }

            while (failureCount < 10)
            {
                // Reset parse graph and generated rooms
                roomGraph = new List<NodeData>();
                generatedRooms = new List<GameObject>();

                // Attempt to parse pattern
                if (!parseLevel(level))
                {
                    Debug.LogError("Could not parse pattern");
                    failureCount++;
                    continue;
                }

                /*
                // Attempt to generate level from parsed pattern
                bool generationResult = false;
                yield return generateRoom(, Vector2.zero, EntranceDirection.LEFT, level, , (result) => generationResult = result);

                // Success
                if (generationResult)
                {
                    Debug.Log("Level successfully generated, attempts: " + (failureCount + 1));
                    yield break;
                }

                // Failure
                failureCount++;
                */

                yield break;
            }

            // Failure
            Debug.LogError("Failed to generate level");
            yield break;
        }

        private bool parseLevel(string level)
        {
            // Get random level pattern
            parseGraph = pickRandomPatternResult(level);

            // Find start node and add to graph
            foreach (NodeData nodeData in parseGraph)
            {
                if (nodeData.type == "startNode")
                {
                    roomGraph.Add(nodeData);
                    parseGraph.Remove(nodeData);
                    break;
                }
            }

            while (parseGraph.Count > 0)
            {
                if (!addConnectedNodes()) return false;
                Debug.Log("Current pattern graph count: " + parseGraph.Count);
            }
            return true;
        }

        // Finds all nodes in parse graph that have a connection already in room graph,
        //     then adds them to room graph as well
        private bool addConnectedNodes()
        {
            foreach (NodeData node in parseGraph)
            {
                foreach (int connection in node.connections)
                {
                    NodeData connectedNode = findNodeWithId(connection, roomGraph);
                    if (connectedNode == null) continue;

                    Debug.Log("Found connected node: " + node.type + " " + connectedNode.type);

                    if (node.terminal)
                    {
                        // Add directly to parse graph
                        roomGraph.Add(node);
                        parseGraph.Remove(node);

                        Debug.Log("Added node " + node.type);
                    }
                    else
                    {
                        // Get random pattern result
                        List<NodeData> patternResult = pickRandomPatternResult(node.type);

                        // Find start and end nodes
                        NodeData startNode = null;
                        NodeData endNode = null;
                        foreach (NodeData patternNode in patternResult)
                        {
                            if (patternNode.type == "startNode") startNode = patternNode;
                            if (patternNode.type == "endNode") endNode = patternNode;

                            // Both nodes have been found if they exist
                            if (startNode != null && (endNode != null || node.entranceCount == 1)) break;
                        }
                        if (startNode == null)
                        {
                            Debug.LogError("Failed to find startNode");
                            return false;
                        }
                        if (endNode == null && node.entranceCount > 1)
                        {
                            Debug.LogError("Failed to find endNode");
                            return false;
                        }
                        if (startNode.connections.Count != 1)
                        {
                            Debug.LogError("startNode has more than 1 connection");
                            return false;
                        }
                        if (endNode != null && endNode.connections.Count != 1)
                        {
                            Debug.LogError("endNode has more than 1 connection");
                            return false;
                        }

                        foreach (int startConnection in startNode.connections)
                        {
                            Debug.Log("StartConnection: " + startConnection);
                        }

                        // Reroute connections
                        // Route first node
                        NodeData firstNode = findNodeWithId(startNode.connections[0], parseGraph);
                        if (firstNode == null)
                        {
                            Debug.LogError("Failed to find first node");
                            return false;
                        }
                        connectedNode.connections.Add(firstNode.id);
                        firstNode.connections.Add(connectedNode.id);
                        connectedNode.connections.Remove(node.id);
                        node.connections.Remove(connectedNode.id);

                        // Route last node if it exists
                        if (endNode != null)
                        {
                            // Node should now only have 1 connection
                            if (node.connections.Count != 1)
                            {
                                Debug.LogError("Node has more than 1 connection remaining after removing startNode");
                                return false;
                            }

                            // Find next node
                            NodeData nextNode = findNodeWithId(node.connections[0], parseGraph);
                            if (nextNode == null)
                            {
                                Debug.LogError("Failed to find next node");
                                return false;
                            }

                            // Link with last node
                            NodeData lastNode = findNodeWithId(endNode.connections[0], parseGraph);
                            if (lastNode == null)
                            {
                                Debug.LogError("Failed to find last node");
                                return false;
                            }
                            lastNode.connections.Add(nextNode.id);
                            nextNode.connections.Add(lastNode.id);
                            node.connections.Remove(nextNode.id);
                            nextNode.connections.Remove(node.id);
                        }

                        // Remove nonterminal node and replace it with its result
                        roomGraph.AddRange(patternResult);
                        parseGraph.Remove(node);

                        // Debug output
                        string debugOutput = "Replaced node: " + node.type + " with:";
                        foreach (NodeData debugNodeData in patternResult)
                        {
                            debugOutput += " [" + debugNodeData.type + "]";
                        }
                        Debug.Log(debugOutput);
                    }

                    // Iterate again
                    return true;
                }
            }

            // No connections found, at least one node does not have a valid connection
            Debug.LogError("Failed to parse graph, at least one node has an invalid connection");
            return false;
        }

        // Returns a NEW list containing a random dictionary entry with given pattern as its key
        private List<NodeData> pickRandomPatternResult(string pattern)
        {
            // Check if dictionary contains pattern
            if (!patternDictionary.ContainsKey(pattern))
            {
                Debug.LogError("Rule key " + pattern + " not found");
                return null;
            }

            // Randomly pick pattern
            int randomIndex = UnityEngine.Random.Range(0, patternDictionary[pattern].Count);
            return new List<NodeData>(patternDictionary[pattern][randomIndex]);
        }

        private NodeData findNodeWithId(int id, List<NodeData> nodeDataList)
        {
            foreach (NodeData nodeData in nodeDataList)
            {
                if (nodeData.id == id) return nodeData;
            }
            return null;
        }

    }
}
