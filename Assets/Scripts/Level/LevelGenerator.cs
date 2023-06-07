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
            this.connections = new List<int>(connections);
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
            this.connections = new List<int>(other.connections);
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

            StartCoroutine(initializeRooms());
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

        private IEnumerator initializeRooms()
        {
            foreach (GameObject roomPrefab in roomPrefabs)
            {
                RoomData prefabData = roomPrefab.GetComponent<RoomData>();
                prefabData.name = roomPrefab.name;
                prefabData.roomPrefab = roomPrefab;

                // Create storage structures if they do not exist
                if (!roomDictionary.ContainsKey(prefabData.region)) roomDictionary[prefabData.region] = new Dictionary<string, List<RoomData>>();
                if (!roomDictionary[prefabData.region].ContainsKey(prefabData.type)) roomDictionary[prefabData.region][prefabData.type] = new List<RoomData>();

                // Add to dictionary
                roomDictionary[prefabData.region][prefabData.type].Add(prefabData);
            }

            yield break;
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

        private void assignNewIds(List<NodeData> pattern)
        {
            foreach (NodeData node in pattern)
            {
                // Update connections to use the same new id
                foreach (NodeData otherNode in pattern)
                {
                    if (otherNode.connections.Contains(node.id))
                    {
                        otherNode.connections.Remove(node.id);
                        otherNode.connections.Add(nextId);
                    }
                }

                // Assign new id to current node
                node.id = nextId;
                nextId++;
            }
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
                parseGraph = new List<NodeData>();
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
                // Debug output
                string debugOutput = "Parse result:";
                foreach (NodeData data in roomGraph)
                {
                    debugOutput += " [" + data.type + " " + data.id + "]";
                }
                Debug.Log(debugOutput);
                */




                /*
                // Collect all node data
                PatternData newPatternData = new PatternData();
                newPatternData.data = new List<NodeData>(roomGraph);
                newPatternData.name = "roomGraph";
                // Convert to json
                string jsonString = JsonUtility.ToJson(newPatternData);
                // Write to file
                File.WriteAllText("C:\\Users\\ThingBlue\\Documents\\LoppyLevelPatterns\\roomGraph.json", jsonString);
                */





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

        #region Parsing

        private bool parseLevel(string level)
        {
            // Get random level pattern
            parseGraph = pickRandomPatternResult(level);
            assignNewIds(parseGraph);

            // Find start node and add to graph
            foreach (NodeData node in parseGraph)
            {
                if (node.type == "startNode")
                {
                    roomGraph.Add(node);
                    parseGraph.Remove(node);
                    break;
                }
            }

            while (parseGraph.Count > 0)
            {
                if (!addConnectedNodes()) return false;

                /*
                // Debug output
                string debugOutput = "Current parse result:";
                foreach (NodeData data in roomGraph)
                {
                    debugOutput += " [" + data.type + " " + data.id + "]";
                }
                Debug.Log(debugOutput);
                */
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

                    if (node.terminal)
                    {
                        // Add directly to parse graph
                        roomGraph.Add(node);
                        parseGraph.Remove(node);
                    }
                    else
                    {
                        // Get random pattern result as a NEW list of NEW nodes
                        List<NodeData> patternResult = pickRandomPatternResult(node.type);
                        assignNewIds(patternResult);

                        // Find start and end nodes
                        NodeData startNode = findNodeWithType("startNode", patternResult);
                        NodeData endNode = findNodeWithType("endNode", patternResult);
                        if (startNode == null) { Debug.LogError("Failed to find startNode"); return false; }
                        if (endNode == null && node.entranceCount > 1) { Debug.LogError("Failed to find endNode"); return false; }
                        if (startNode.connections.Count != 1) { Debug.LogError("startNode has more than 1 connection"); return false; }
                        if (endNode != null && endNode.connections.Count != 1) { Debug.LogError("endNode has more than 1 connection"); return false; }

                        // Remove nonterminal node and replace it with its result in parse graph
                        if (!patternResult.Remove(startNode)) { Debug.LogError("Failed to remove startNode, startNode id: " + startNode.id); return false; }
                        if (endNode != null)
                        {
                            if (!patternResult.Remove(endNode)) { Debug.LogError("Failed to remove endNode, endNode id: " + endNode.id); return false; }
                        }
                        parseGraph.AddRange(patternResult);
                        parseGraph.Remove(node);

                        // Find first node
                        NodeData firstNode = findNodeWithId(startNode.connections[0], parseGraph);
                        if (firstNode == null) { Debug.LogError("Failed to find first node with id: " + startNode.connections[0]); return false; }

                        // Connect first node to previous node
                        connectedNode.connections.Add(firstNode.id);
                        firstNode.connections.Add(connectedNode.id);
                        connectedNode.connections.Remove(node.id);

                        // Connect last node to next node if it exists
                        if (endNode != null)
                        {
                            // Find next node
                            NodeData nextNode = findFirstConnectedNode(node.id, parseGraph);
                            if (nextNode == null) { Debug.LogError("Failed to find next node"); return false; }

                            NodeData lastNode = findNodeWithId(endNode.connections[0], parseGraph);
                            if (lastNode == null) { Debug.LogError("Failed to find last node"); return false; }

                            // Link with last node
                            lastNode.connections.Add(nextNode.id);
                            nextNode.connections.Add(lastNode.id);
                            nextNode.connections.Remove(node.id);
                        }
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

            List<NodeData> result = new List<NodeData>();
            foreach (NodeData node in patternDictionary[pattern][randomIndex]) result.Add(new NodeData(node));
            return result;
        }

        private NodeData findNodeWithId(int id, List<NodeData> nodeDataList)
        {
            foreach (NodeData nodeData in nodeDataList)
            {
                if (nodeData.id == id) return nodeData;
            }
            return null;
        }

        private NodeData findNodeWithType(string type, List<NodeData> nodeDataList)
        {
            foreach (NodeData nodeData in nodeDataList)
            {
                if (nodeData.type == type) return nodeData;
            }
            return null;
        }

        // Returns the first node in list that has the connection id
        private NodeData findFirstConnectedNode(int id, List<NodeData> nodeDataList)
        {
            foreach (NodeData node in nodeDataList)
            {
                if (node.connections.Contains(id)) return node;
            }
            return null;
        }

        #endregion

        #region Generation

        private bool generateLevel()
        {
            return true;
        }

        private bool generateRoom()
        {
            return true;
        }

        #endregion

    }
}
