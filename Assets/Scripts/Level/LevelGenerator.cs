using Codice.Client.Common.TreeGrouper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace Loppy.Level
{
    public class LevelGenerator : MonoBehaviour
    {
        public static LevelGenerator instance;

        #region Inspector members

        public string editorDataPath;
        public uint maxConcurrentCoroutines;
        //public uint maxParseQueueCount;
        public List<GameObject> roomPrefabs;

        #endregion

        public int randomSeed;

        public int nextId = 0;

        private Dictionary<string, List<RoomPrefabData>> roomDictionary;
        private Dictionary<string, List<List<RoomNode>>> patternDictionary;

        private Queue<List<RoomNode>> patternParseQueue;
        private uint runningPatternParseCoroutines = 0;
        private List<List<RoomNode>> patternParseResults;

        private Queue<List<RoomNode>> roomParseQueue;
        private uint runningRoomParseCoroutines = 0;

        private List<RoomNode> roomGraph;
        private bool success = false;

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Initialize storage
            roomDictionary = new Dictionary<string, List<RoomPrefabData>>();
            patternDictionary = new Dictionary<string, List<List<RoomNode>>>();
            patternParseQueue = new Queue<List<RoomNode>>();
            patternParseResults = new List<List<RoomNode>>();
            roomParseQueue = new Queue<List<RoomNode>>();
            roomGraph = new List<RoomNode>();

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
            
            //Debug.Log("runningCoroutines: " + runningPatternParseCoroutines + ", queue: " + patternParseQueue.Count);
            //Debug.Log("runningCoroutines: " + runningCoroutines + ", queue: " + parseQueue.Count);

        }

        #region Initialization

        private IEnumerator initializeRooms()
        {
            foreach (GameObject roomPrefab in roomPrefabs)
            {
                RoomPrefabData prefabData = roomPrefab.GetComponent<RoomPrefabData>();
                prefabData.name = roomPrefab.name;
                prefabData.roomPrefab = roomPrefab;

                // Create storage structures if they do not exist
                if (!roomDictionary.ContainsKey(prefabData.type)) roomDictionary[prefabData.type] = new List<RoomPrefabData>();

                // Add to dictionary
                roomDictionary[prefabData.type].Add(prefabData);
            }
            roomPrefabs.Clear();

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

            /*
            // Debug output
            foreach (KeyValuePair<string, List<List<DataNode>>> entry in patternDictionary)
            {
                string debugOutput = entry.Key;
                foreach (List<DataNode> dataList in entry.Value)
                {
                    foreach (DataNode data in dataList)
                    {
                        debugOutput += " [" + data.name + " " + data.id + "]";
                    }
                }
                Debug.Log(debugOutput);
            }
            */

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
                if (!patternDictionary.ContainsKey(newPatternData.name)) patternDictionary[newPatternData.name] = new List<List<RoomNode>>();

                // Add to pattern dictionary
                List<RoomNode> patternRoomNodes = convertToRoomNodes(newPatternData.data);
                patternDictionary[newPatternData.name].Add(patternRoomNodes);
                
                // Clean up
                newPatternData.data.Clear();
            }
        }

        #endregion

        private IEnumerator generateLevel(string level)
        {
            success = false;

            // Assert that the level exists as a key in patterns
            if (!patternDictionary.ContainsKey(level))
            {
                Debug.LogError("Could not find specified level");
                yield break;
            }

            // Reset parse graph and generated rooms
            destroyGeneratedRooms();

            // Initialize room graph
            roomGraph = pickRandomPatternResult(level);

            // Parse patterns
            patternParseQueue.Clear();
            patternParseResults.Clear();
            patternParseQueue.Enqueue(roomGraph);
            while (patternParseQueue.Count > 0 || runningPatternParseCoroutines > 0)
            {
                List<RoomNode> cloneToParse = patternParseQueue.Dequeue();
                runningPatternParseCoroutines++;
                StartCoroutine(parsePatterns(cloneToParse));

                yield return new WaitUntil(() => patternParseQueue.Count > 0 || runningPatternParseCoroutines == 0);
            }

            foreach (List<RoomNode> patternParseResultClone in patternParseResults)
            {
                string patternDebugString = "Clone: ";
                foreach (RoomNode node in patternParseResultClone)
                {
                    patternDebugString += " [" + node.type + "]";
                }
                Debug.Log(patternDebugString);
            }
            Debug.Log("Total patterns: " + patternParseResults.Count);

            /*
            // Enqueue start state
            parseQueue.Clear();
            parseQueue.Enqueue(roomGraph);

            // Start parsing
            while (!success)
            {
                // Wait for enqueue if empty
                if (parseQueue.Count == 0 || runningCoroutines >= maxConcurrentCoroutines)
                {
                    yield return new WaitUntil(() => parseQueue.Count > 0 && runningCoroutines < maxConcurrentCoroutines);
                }

                // Parse next clone
                List<RoomNode> clone = parseQueue.Dequeue();
                runningCoroutines++;
                StartCoroutine(parseGraph(clone, parseSuccessCallback));
            }

            */

            // Clean up
            roomParseQueue.Clear();
            yield break;
        }

        #region Pattern parsing

        private IEnumerator parsePatterns(List<RoomNode> graph)
        {
            // Find first pattern node reachable from the start node
            RoomNode patternNode = null;
            Queue<RoomNode> nodesToVisit = new Queue<RoomNode>();
            nodesToVisit.Enqueue(findRoomByType("startNode", graph));
            //nodesToVisit.Enqueue(findRoomById(startId, graph));
            while (nodesToVisit.Count > 0)
            {
                RoomNode node = nodesToVisit.Dequeue();
                node.visited = true;

                // Enqueue connected and unvisited children nodes
                foreach (int connection in node.connections)
                {
                    RoomNode connectedNode = findRoomById(connection, graph);
                    if (connectedNode == null) Debug.LogError("Null node connected to: " + node.type);
                    if (!connectedNode.visited)
                    {
                        connectedNode.parentId = node.id;
                        nodesToVisit.Enqueue(connectedNode);
                    }
                }

                // Check for pattern node
                if (!node.terminal)
                {
                    patternNode = node;
                    break;
                }
            }
            nodesToVisit.Clear();

            // Check if all nodes are terminal
            if (patternNode == null)
            {
                patternParseResults.Add(graph);
                runningPatternParseCoroutines--;
                yield break;
            }

            // Create a clone of the graph for every choice of pattern result
            for (int i = 0; i < patternDictionary[patternNode.type].Count; i++)
            {
                // Create clone
                List<RoomNode> clone = new List<RoomNode>();
                foreach (RoomNode node in graph)
                {
                    RoomNode newRoomNode = new RoomNode(node);
                    newRoomNode.visited = false;
                    clone.Add(newRoomNode);
                }

                // Replace pattern with new pattern
                List<RoomNode> patternResult = new List<RoomNode>();
                foreach (RoomNode patternResultNode in patternDictionary[patternNode.type][i]) patternResult.Add(new RoomNode(patternResultNode));
                assignNewIds(patternResult);

                // Find start and end nodes
                RoomNode startNode = findRoomByType("startNode", patternResult);
                RoomNode endNode = findRoomByType("endNode", patternResult);

                // Remove nonterminal node and replace it with its result in parse graph
                patternResult.Remove(startNode);
                if (endNode != null) patternResult.Remove(endNode);
                clone.AddRange(patternResult);
                removeById(patternNode.id, clone);

                // Find first, last, next, and previous nodes
                RoomNode firstNode = null;
                RoomNode previousNode = null;
                RoomNode lastNode = null;
                RoomNode nextNode = null;
                foreach (RoomNode node in clone)
                {
                    if (node.id == startNode.connections[0]) firstNode = node;
                    if (node.id == patternNode.parentId) previousNode = node;
                    if (endNode != null)
                    {
                        if (node.id == endNode.connections[0]) lastNode = node;
                        if (node.parentId == patternNode.id) nextNode = node;
                    }
                }

                // Connect first node to previous node
                previousNode.connections.Add(firstNode.id);
                firstNode.connections.Add(previousNode.id);
                previousNode.connections.Remove(patternNode.id);
                // Remove start node from first node
                firstNode.connections.Remove(startNode.id);

                // Connect last node to next node if it exists
                if (endNode != null)
                {
                    // Link with last node
                    lastNode.connections.Add(nextNode.id);
                    nextNode.connections.Add(lastNode.id);
                    nextNode.connections.Remove(patternNode.id);
                    // Remove end node from last node
                    lastNode.connections.Remove(endNode.id);

                    // Clean up
                    endNode.Dispose();
                }

                // Clean up
                startNode.Dispose();
                patternResult.Clear();

                // Push clone to queue
                //runningPatternParseCoroutines++;
                //StartCoroutine(parsePatterns(clone));
                patternParseQueue.Enqueue(clone);
            }

            // Don't need current graph anymore, clean up
            foreach (RoomNode node in graph) node.Dispose();
            graph.Clear();

            runningPatternParseCoroutines--;
            yield break;
        }

        #endregion

        #region Room graph parsing

        private IEnumerator parseGraph(List<RoomNode> graph, Action<List<RoomNode>> successCallback)
        {
            yield break;
        }

        private void parseSuccessCallback(List<RoomNode> parseResult)
        {
            StopAllCoroutines();
            roomGraph = parseResult;
            createRoomGameObjects();
            success = true;
            Debug.Log("Level successfully generated");
        }

        private void createRoomGameObjects()
        {
            foreach (RoomNode room in roomGraph)
            {
                if (room.type == "startNode" || room.type == "endNode") continue;

                if (room.roomPrefabData == null) Debug.Log("NO ROOM PREFAB DATA");
                if (room.roomPrefabData.roomPrefab == null) Debug.Log("NO ROOM PREFAB OBJECT");
                // Room fits, generate it
                GameObject newRoomGameObject = Instantiate(room.roomPrefabData.roomPrefab, room.roomCenter, Quaternion.identity);
                room.roomGameObject = newRoomGameObject;
            }
        }

        #endregion

        #region Helpers

        private void assignNewIds(List<RoomNode> pattern)
        {
            foreach (RoomNode node in pattern)
            {
                // Update connections to use the same new id
                foreach (RoomNode otherNode in pattern)
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

        private RoomNode findRoomById(int id, List<RoomNode> rooms)
        {
            foreach (RoomNode nodeData in rooms)
            {
                if (nodeData.id == id) return nodeData;
            }
            return null;
        }

        private RoomNode findRoomByType(string type, List<RoomNode> rooms)
        {
            foreach (RoomNode room in rooms)
            {
                if (room.type == type) return room;
            }
            return null;
        }

        private void removeById(int id, List<RoomNode> rooms)
        {
            List<RoomNode> roomsToRemove = new List<RoomNode>();
            foreach (RoomNode room in rooms)
            {
                if (room.id == id) roomsToRemove.Add(room);
            }
            foreach (RoomNode roomToRemove in roomsToRemove) rooms.Remove(roomToRemove);
            roomsToRemove.Clear();
        }

        // Returns a NEW list containing a random dictionary entry with given pattern as its key
        private List<RoomNode> pickRandomPatternResult(string pattern)
        {
            // Check if dictionary contains pattern
            if (!patternDictionary.ContainsKey(pattern))
            {
                Debug.LogError("Rule key " + pattern + " not found");
                return null;
            }

            // Randomly pick pattern
            int randomIndex = UnityEngine.Random.Range(0, patternDictionary[pattern].Count);

            List<RoomNode> result = new List<RoomNode>();
            foreach (RoomNode node in patternDictionary[pattern][randomIndex]) result.Add(new RoomNode(node));
            return result;
        }

        private List<RoomNode> convertToRoomNodes(List<DataNode> dataGraph)
        {
            List<RoomNode> newRoomGraph = new List<RoomNode>();
            // Convert all DataNodes to RoomNodes
            foreach (DataNode dataNode in dataGraph)
            {
                RoomNode newRoomNode = new RoomNode(dataNode.type, dataNode.entranceCount, dataNode.id, dataNode.connections, dataNode.terminal);
                newRoomGraph.Add(newRoomNode);
            }
            return newRoomGraph;
        }

        private void destroyGeneratedRooms()
        {
            foreach (RoomNode node in roomGraph)
            {
                if (node.roomGameObject != null) Destroy(node.roomGameObject);
            }
        }

        #endregion
    }
}
