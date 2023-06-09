using Codice.Client.BaseCommands;
using Codice.Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

namespace Loppy.Level
{
    // Data for each pattern node imported from the editor
    [Serializable]
    public class DataNode
    {
        public int id;
        public string name;
        public string region;
        public string type;
        public bool terminal;
        public int entranceCount;
        public List<int> connections;
        public Vector2 editorPosition;

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
    public class RoomNode
    {
        public string type;
        public int entranceCount;
        public List<RoomNode> connectedNodes;

        // Imported from DataNode
        public int id;
        public List<int> connections;

        // Members that will be assigned after the best room to generate is determined
        public bool generated = false;
        public RoomPrefabData roomPrefabData = null;
        public Vector2 roomCenter = Vector2.zero;
        public GameObject gameObject = null;

        // Temporary list of unused entrances to help with room generation
        public List<RoomEntrance> openExits;

        public RoomNode(string type, int entranceCount, List<RoomNode> connectedNodes, int id, List<int> connections, bool generated, RoomPrefabData roomData, Vector2 roomCenter, GameObject gameObject, List<RoomEntrance> openEntrances)
        {
            this.type = type;
            this.entranceCount = entranceCount;
            this.connectedNodes = new List<RoomNode>(connectedNodes);

            this.id = id;
            this.connections = new List<int>(connections);

            this.generated = generated;
            this.roomPrefabData = roomData;
            this.roomCenter = roomCenter;
            this.gameObject = gameObject;

            this.openExits = new List<RoomEntrance>(openEntrances);
        }

        public RoomNode(RoomNode other)
        {
            this.type = other.type;
            this.entranceCount = other.entranceCount;
            this.connectedNodes = new List<RoomNode>(other.connectedNodes);

            this.id = other.id;
            this.connections = new List<int>(other.connections);

            this.generated = other.generated;
            this.roomPrefabData = other.roomPrefabData;
            this.roomCenter = other.roomCenter;
            this.gameObject = other.gameObject;

            this.openExits = new List<RoomEntrance>(other.openExits);
        }
    }

    public class LevelGenerator : MonoBehaviour
    {
        public static LevelGenerator instance;

        #region Inspector members

        public string editorDataPath;
        public List<GameObject> roomPrefabs;

        #endregion

        public int randomSeed;

        public int nextId = 0;

        private Dictionary<string, Dictionary<string, List<RoomPrefabData>>> roomDictionary;
        private Dictionary<string, List<List<DataNode>>> patternDictionary;

        private List<DataNode> parseInput;
        private List<DataNode> parseStack;

        private List<RoomNode> roomGraph;

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Initialize storage
            roomDictionary = new Dictionary<string, Dictionary<string, List<RoomPrefabData>>>();
            patternDictionary = new Dictionary<string, List<List<DataNode>>>();
            parseInput = new List<DataNode>();
            parseStack = new List<DataNode>();
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
        }

        private IEnumerator initializeRooms()
        {
            foreach (GameObject roomPrefab in roomPrefabs)
            {
                RoomPrefabData prefabData = roomPrefab.GetComponent<RoomPrefabData>();
                prefabData.name = roomPrefab.name;
                prefabData.roomPrefab = roomPrefab;

                // Create storage structures if they do not exist
                if (!roomDictionary.ContainsKey(prefabData.region)) roomDictionary[prefabData.region] = new Dictionary<string, List<RoomPrefabData>>();
                if (!roomDictionary[prefabData.region].ContainsKey(prefabData.type)) roomDictionary[prefabData.region][prefabData.type] = new List<RoomPrefabData>();

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
                if (!patternDictionary.ContainsKey(newPatternData.name)) patternDictionary[newPatternData.name] = new List<List<DataNode>>();

                // Update data with unique ids
                assignNewIds(newPatternData.data);

                // Add to pattern dictionary
                patternDictionary[newPatternData.name].Add(newPatternData.data);
            }
        }

        private void assignNewIds(List<DataNode> pattern)
        {
            foreach (DataNode node in pattern)
            {
                // Update connections to use the same new id
                foreach (DataNode otherNode in pattern)
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

        private void destroyGeneratedRooms()
        {
            foreach (RoomNode node in roomGraph)
            {
                if (node.gameObject != null) Destroy(node.gameObject);
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
                parseInput = new List<DataNode>();
                parseStack = new List<DataNode>();
                destroyGeneratedRooms();
                roomGraph = new List<RoomNode>();

                // Attempt to parse pattern
                if (!parseLevel(level))
                {
                    Debug.LogError("Could not parse pattern");
                    failureCount++;
                    continue;
                }


                
                // Debug output
                string debugOutput = "Parse result:";
                foreach (DataNode data in parseStack)
                {
                    debugOutput += " [" + data.type + " " + data.id + "]";
                }
                Debug.Log(debugOutput);
                




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


                convertToRoomNodes();

                // Attempt to generate level from parsed pattern
                bool generationResult = false;
                yield return generateRoom(findFirstRoomNode(), Vector2.zero, EntranceDirection.LEFT, level, (result) => generationResult = result);
                if (generationResult)
                {
                    Debug.Log("Level successfully generated, attempts: " + (failureCount + 1));
                    yield break;
                }

                // Failure
                failureCount++;

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
            parseInput = pickRandomPatternResult(level);
            assignNewIds(parseInput);

            // Find start node and add to graph
            foreach (DataNode node in parseInput)
            {
                if (node.type == "startNode")
                {
                    parseStack.Add(node);
                    parseInput.Remove(node);
                    break;
                }
            }

            while (parseInput.Count > 0)
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
            foreach (DataNode node in parseInput)
            {
                foreach (int connection in node.connections)
                {
                    DataNode connectedNode = findNodeWithId(connection, parseStack);
                    if (connectedNode == null) continue;

                    if (node.terminal)
                    {
                        // Add directly to parse graph
                        parseStack.Add(node);
                        parseInput.Remove(node);
                    }
                    else
                    {
                        // Get random pattern result as a NEW list of NEW nodes
                        List<DataNode> patternResult = pickRandomPatternResult(node.type);
                        assignNewIds(patternResult);

                        // Find start and end nodes
                        DataNode startNode = findNodeWithType("startNode", patternResult);
                        DataNode endNode = findNodeWithType("endNode", patternResult);
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
                        parseInput.AddRange(patternResult);
                        parseInput.Remove(node);

                        // Find first node
                        DataNode firstNode = findNodeWithId(startNode.connections[0], parseInput);
                        if (firstNode == null) { Debug.LogError("Failed to find first node with id: " + startNode.connections[0]); return false; }

                        // Connect first node to previous node
                        connectedNode.connections.Add(firstNode.id);
                        firstNode.connections.Add(connectedNode.id);
                        connectedNode.connections.Remove(node.id);

                        // Connect last node to next node if it exists
                        if (endNode != null)
                        {
                            // Find next node
                            DataNode nextNode = findFirstConnectedNode(node.id, parseInput);
                            if (nextNode == null) { Debug.LogError("Failed to find next node"); return false; }

                            DataNode lastNode = findNodeWithId(endNode.connections[0], parseInput);
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
        private List<DataNode> pickRandomPatternResult(string pattern)
        {
            // Check if dictionary contains pattern
            if (!patternDictionary.ContainsKey(pattern))
            {
                Debug.LogError("Rule key " + pattern + " not found");
                return null;
            }

            // Set seed for randomizer
            //UnityEngine.Random.InitState(randomSeed);

            // Randomly pick pattern
            int randomIndex = UnityEngine.Random.Range(0, patternDictionary[pattern].Count);

            List<DataNode> result = new List<DataNode>();
            foreach (DataNode node in patternDictionary[pattern][randomIndex]) result.Add(new DataNode(node));
            return result;
        }

        private DataNode findNodeWithId(int id, List<DataNode> nodeDataList)
        {
            foreach (DataNode nodeData in nodeDataList)
            {
                if (nodeData.id == id) return nodeData;
            }
            return null;
        }

        private DataNode findNodeWithType(string type, List<DataNode> nodeDataList)
        {
            foreach (DataNode nodeData in nodeDataList)
            {
                if (nodeData.type == type) return nodeData;
            }
            return null;
        }

        // Returns the first node in list that has the connection id
        private DataNode findFirstConnectedNode(int id, List<DataNode> nodeDataList)
        {
            foreach (DataNode node in nodeDataList)
            {
                if (node.connections.Contains(id)) return node;
            }
            return null;
        }

        #endregion

        #region Generation

        private void convertToRoomNodes()
        {
            // Convert all DataNodes to RoomNodes
            foreach (DataNode dataNode in parseStack)
            {
                RoomNode newRoomNode = new RoomNode(dataNode.type, dataNode.entranceCount, new List<RoomNode>(), dataNode.id, dataNode.connections, false, null, Vector2.zero, null, new List<RoomEntrance>());
                
                // Mark to ignore start and end nodes
                if (newRoomNode.type == "startNode" || newRoomNode.type == "endNode") newRoomNode.generated = true;
                roomGraph.Add(newRoomNode);
            }

            // Connect all nodes
            foreach (RoomNode node in roomGraph)
            {
                foreach (RoomNode otherNode in roomGraph)
                {
                    if (otherNode.connections.Contains(node.id))
                    {
                        otherNode.connections.Remove(node.id);
                        node.connections.Remove(otherNode.id);
                        otherNode.connectedNodes.Add(node);
                        node.connectedNodes.Add(otherNode);
                    }
                }
            }
        }

        private RoomNode findFirstRoomNode()
        {
            // Find start node and return its connection
            foreach (RoomNode node in roomGraph)
            {
                if (node.type == "startNode") return node.connectedNodes[0];
            }
            return null;
        }

        private IEnumerator generateRoom(RoomNode node, Vector2 previousRoomExitPosition, EntranceDirection entranceDirection, string region, Action<bool> result)
        {
            // Check for room errors
            if (entranceDirection == EntranceDirection.NONE)
            {
                Debug.Log("Found room with no entrance direction");
                result(false);
                yield break;
            }

            // Set seed for randomizer
            //UnityEngine.Random.InitState(randomSeed);

            // Randomly generate room
            List<RoomPrefabData> validRooms = new List<RoomPrefabData>(roomDictionary[region][node.type]);
            while (validRooms.Count > 0)
            {
                // Get new random room prefab data
                RoomPrefabData randomRoom = popRandom<RoomPrefabData>(validRooms);

                // Get all entrances matching the exit direction of last room
                List<RoomEntrance> validEntrances = new List<RoomEntrance>();
                for (int i = 0; i < randomRoom.entrances.Count; i++)
                {
                    if (randomRoom.entrances[i].direction == entranceDirection) validEntrances.Add(randomRoom.entrances[i]);
                }
                if (validEntrances.Count == 0) { node.roomPrefabData = null; continue; }

                // Randomly pick a valid entrance to position current room
                while (validEntrances.Count > 0)
                {
                    // Get entrance and room positioning data
                    RoomEntrance randomEntrance = popRandom<RoomEntrance>(validEntrances);
                    Vector2 roomCenter = previousRoomExitPosition - randomEntrance.position;

                    // Validate room
                    // Check if room overlaps any existing rooms
                    if (checkRoomOverlap(randomRoom, roomCenter)) continue;

                    // Room fits, generate it
                    GameObject newRoomGameObject = Instantiate(randomRoom.roomPrefab, roomCenter, Quaternion.identity);
                    node.generated = true;
                    node.gameObject = newRoomGameObject;
                    node.roomPrefabData = randomRoom;
                    node.roomCenter = roomCenter;

                    // Get open entrances (Remove used entrance and keep others)
                    node.openExits = new List<RoomEntrance>(randomRoom.entrances);
                    node.openExits.Remove(randomEntrance);

                    // Recurse
                    bool childrenResult = true;
                    for (int i = 0; i < node.connectedNodes.Count; i++)
                    {
                        // Check that the room is not already generated
                        if (node.connectedNodes[i].generated) continue;

                        // Randomly pick an open exit
                        bool childResult = false;
                        List<RoomEntrance> validExits = new List<RoomEntrance>(node.openExits);
                        while (validExits.Count > 0)
                        {
                            // Get random exit
                            RoomEntrance randomExit = popRandom<RoomEntrance>(validExits);

                            // Remove this exit from list of open exits
                            node.openExits.Remove(randomExit);

                            // Calculate world position of exit for current room
                            Vector2 exitPosition = roomCenter + randomExit.position;

                            // Calculate entrance direction of next room
                            EntranceDirection nextEntranceDirection = EntranceDirection.NONE;
                            switch (randomExit.direction)
                            {
                                case EntranceDirection.LEFT:
                                    nextEntranceDirection = EntranceDirection.RIGHT;
                                    break;
                                case EntranceDirection.RIGHT:
                                    nextEntranceDirection = EntranceDirection.LEFT;
                                    break;
                                case EntranceDirection.TOP:
                                    nextEntranceDirection = EntranceDirection.BOTTOM;
                                    break;
                                case EntranceDirection.BOTTOM:
                                    nextEntranceDirection = EntranceDirection.TOP;
                                    break;
                                default:
                                    nextEntranceDirection = EntranceDirection.NONE;
                                    break;
                            }

                            yield return new WaitForSeconds(0.1f);

                            // Try to generate room at this exit
                            yield return generateRoom(node.connectedNodes[i], exitPosition, nextEntranceDirection, region, (result) => childResult = result);
                            // Generation successful
                            if (childResult) break;
                            // Generation at this exit failed, try next open exit
                            node.openExits.Add(randomExit);
                        }

                        if (childResult) continue;

                        // Return false if any children fail
                        childrenResult = false;
                        break;
                    }

                    // All recursions successful
                    if (childrenResult)
                    {
                        result(true);
                        yield break;
                    }

                    // Generation of connected nodes failed somehow, clean up and try next entrance
                    node.generated = false;
                    Destroy(node.gameObject);
                    node.gameObject = null;
                    node.roomPrefabData = null;
                    node.roomCenter = Vector2.zero;
                    node.openExits.Add(randomEntrance);
                }

                // No valid entrances, try next room
                node.roomPrefabData = null;
                node.openExits = new List<RoomEntrance>();
            }

            // No suitable rooms found
            Debug.Log("No suitable rooms found on " + node.type);
            result(false);
            yield break;
        }

        private T popRandom<T>(List<T> possibilities)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibilities.Count);
            T result = possibilities[randomIndex];
            possibilities.RemoveAt(randomIndex);
            return result;
        }

        // Returns true if given room (roomData and roomCenter) overlaps room stored in node or any of its children
        private bool checkRoomOverlap(RoomPrefabData roomPrefabData, Vector2 roomCenter)
        {
            // Check if current node has no instantiated room
            if (roomPrefabData == null) return false;

            foreach (RoomNode otherNode in roomGraph)
            {
                // Check if other node has no instantiated room
                if (!otherNode.generated || !otherNode.gameObject || !otherNode.roomPrefabData) continue;

                // Check for overlap with current room node
                if (Mathf.Abs(roomCenter.x - otherNode.roomCenter.x) * 2 < (roomPrefabData.size.x + otherNode.roomPrefabData.size.x) &&
                    Mathf.Abs(roomCenter.y - otherNode.roomCenter.y) * 2 < (roomPrefabData.size.y + otherNode.roomPrefabData.size.y))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

    }
}
