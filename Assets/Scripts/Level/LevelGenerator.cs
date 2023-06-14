using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Graphs;
using UnityEngine;

namespace Loppy.Level
{
    public class LevelGenerator : MonoBehaviour
    {
        public static LevelGenerator instance;

        #region Inspector members

        public string editorDataPath;
        public uint maxPatternParseCoroutines;
        public uint maxRoomParseCoroutines;
        //public uint maxParseQueueCount;
        public List<GameObject> roomPrefabs;

        #endregion

        public int randomSeed;

        public int nextId = 0;

        private Dictionary<string, List<RoomPrefabData>> roomDictionary;
        private Dictionary<string, List<List<RoomNode>>> patternDictionary;

        private Queue<List<RoomNode>> patternParseQueue;
        private uint runningPatternParseCoroutines = 0;

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

            //Debug.Log("runningPatternCoroutines: " + runningPatternParseCoroutines + ", patternQueue: " + patternParseQueue.Count + ", runningRoomCoroutines: " + runningRoomParseCoroutines + ", roomQueue: " + roomParseQueue.Count);

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
                List<RoomNode> patternRoomNodes = convertToRoomGraph(newPatternData.data);
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
            destroyGeneratedRooms(roomGraph);

            // Initialize room graph
            roomGraph = pickRandomPatternResult(level);

            // Parse patterns
            StartCoroutine(startPatternParse());

            // Generate rooms
            StartCoroutine(startRoomParse());

            yield break;
        }

        #region Pattern parsing

        private IEnumerator startPatternParse()
        {
            patternParseQueue.Clear();
            roomParseQueue.Clear();
            patternParseQueue.Enqueue(roomGraph);
            while (patternParseQueue.Count > 0 || runningPatternParseCoroutines > 0)
            {
                List<RoomNode> cloneToParse = patternParseQueue.Dequeue();
                runningPatternParseCoroutines++;
                StartCoroutine(parsePatterns(cloneToParse));

                if (patternParseQueue.Count == 0 ||
                    runningPatternParseCoroutines >= maxPatternParseCoroutines ||
                    roomParseQueue.Count >= maxRoomParseCoroutines)
                {
                    yield return new WaitUntil(() => (patternParseQueue.Count > 0 &&
                                                     runningPatternParseCoroutines < maxPatternParseCoroutines &&
                                                     roomParseQueue.Count < maxRoomParseCoroutines) ||
                                                     runningPatternParseCoroutines == 0);
                }
            }

            foreach (List<RoomNode> patternParseResultClone in roomParseQueue)
            {
                string patternDebugString = "Clone: ";
                foreach (RoomNode node in patternParseResultClone)
                {
                    patternDebugString += " [" + node.type + "]";
                }
                Debug.Log(patternDebugString);
            }
            Debug.Log("Total patterns: " + roomParseQueue.Count);
        }

        private IEnumerator parsePatterns(List<RoomNode> graph)
        {
            //yield return new WaitForSeconds(0.1f);

            // Find first pattern node reachable from the start node
            RoomNode patternNode = null;
            Queue<RoomNode> nodesToVisit = new Queue<RoomNode>();
            nodesToVisit.Enqueue(findRoomByType("startNode", graph));
            while (nodesToVisit.Count > 0)
            {
                RoomNode node = nodesToVisit.Dequeue();
                node.visited = true;

                // Enqueue connected and unvisited children nodes
                foreach (RoomNode connectedNode in node.connectedNodes)
                {
                    if (connectedNode == null) Debug.LogError("Null node connected to: " + node.type);
                    if (!connectedNode.visited)
                    {
                        connectedNode.parentNode = node;
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
                foreach (RoomNode node in graph) node.visited = node.type == "startNode";
                roomParseQueue.Enqueue(graph);
                runningPatternParseCoroutines--;
                yield break;
            }

            // Only parse graph when more is needed by the room parser
            if (roomParseQueue.Count >= maxRoomParseCoroutines) yield return new WaitUntil(() => roomParseQueue.Count < maxRoomParseCoroutines);

            // Create a clone of the graph for every choice of pattern result
            for (int i = 0; i < patternDictionary[patternNode.type].Count; i++)
            {
                // Create clone
                List<RoomNode> clone = cloneGraph(graph);

                // Replace pattern with new pattern
                replacePattern(patternNode, i, clone);

                // Push clone to queue
                patternParseQueue.Enqueue(clone);
            }

            // Don't need current graph anymore, clean up
            foreach (RoomNode node in graph) node.Dispose();
            graph.Clear();

            runningPatternParseCoroutines--;
            yield break;
        }

        private void replacePattern(RoomNode patternNode, int resultIndex, List<RoomNode> graph)
        {
            // Replace pattern with new pattern
            List<RoomNode> patternResult = new List<RoomNode>();
            foreach (RoomNode patternResultNode in patternDictionary[patternNode.type][resultIndex]) patternResult.Add(new RoomNode(patternResultNode));
            //assignNewIds(patternResult);

            // Find start and end nodes
            RoomNode startNode = findRoomByType("startNode", patternResult);
            RoomNode endNode = findRoomByType("endNode", patternResult);

            // Remove nonterminal node and replace it with its result in parse graph
            patternResult.Remove(startNode);
            if (endNode != null) patternResult.Remove(endNode);
            graph.AddRange(patternResult);
            graph.Remove(patternNode);

            // Find first, last, next, and previous nodes
            RoomNode firstNode = startNode.connectedNodes[0];
            RoomNode previousNode = patternNode.parentNode;
            RoomNode lastNode = endNode != null ? endNode.connectedNodes[0] : null;
            RoomNode nextNode = null;
            if (endNode != null)
            {
                foreach (RoomNode node in graph)
                {
                    if (node.parentNode == patternNode)
                    {
                        nextNode = node;
                        break;
                    }
                }
            }

            // Connect first node to previous node
            previousNode.connectedNodes.Add(firstNode);
            firstNode.connectedNodes.Add(previousNode);
            previousNode.connectedNodes.Remove(patternNode);
            // Remove start node from first node
            firstNode.connectedNodes.Remove(startNode);

            // Connect last node to next node if it exists
            if (endNode != null)
            {
                // Link with last node
                lastNode.connectedNodes.Add(nextNode);
                nextNode.connectedNodes.Add(lastNode);
                nextNode.connectedNodes.Remove(patternNode);
                // Remove end node from last node
                lastNode.connectedNodes.Remove(endNode);

                // Clean up
                endNode.Dispose();
            }

            // Clean up
            startNode.Dispose();
            patternResult.Clear();
        }

        #endregion

        #region Room graph parsing

        private IEnumerator startRoomParse()
        {
            // Wait until pattern parse yields enough results to begin new coroutine
            if (roomParseQueue.Count == 0) yield return new WaitUntil(() => roomParseQueue.Count > 0);

            Debug.Log("Started room parsing");

            // Start parse
            while (!success)
            {
                List<RoomNode> cloneToParse = roomParseQueue.Dequeue();
                RoomNode startNode = findRoomByType("startNode", cloneToParse);
                startNode.visited = true;
                RoomNode firstNode = startNode.connectedNodes[0];
                firstNode.parentExit = new RoomEntrance(Vector2.zero, EntranceDirection.NONE);
                runningRoomParseCoroutines++;
                StartCoroutine(parseRoom(firstNode, cloneToParse, (result) => roomParseSuccessCallback(cloneToParse, result)));

                if (roomParseQueue.Count == 0 ||
                    runningRoomParseCoroutines >= maxRoomParseCoroutines)
                {
                    yield return new WaitUntil(() => (roomParseQueue.Count > 0 && runningRoomParseCoroutines < maxRoomParseCoroutines) ||
                                                     runningRoomParseCoroutines == 0);
                }
            }
        }

        private IEnumerator parseRoom(RoomNode node, List<RoomNode> graph, Action<bool> result)
        {
            //yield return new WaitForSeconds(0.1f);

            if (node.type == "endNode")
            {
                result(true);
                yield break;
            }

            List<RoomPrefabData> validRooms = new List<RoomPrefabData>(roomDictionary[node.type]);
            // Randomly generate room
            while (validRooms.Count > 0)
            {
                // Get new random room prefab data
                RoomPrefabData randomRoom = popRandom(validRooms);
                node.roomPrefabData = randomRoom;

                // Get all entrances matching the exit direction of last room
                List<RoomEntrance> validEntrances = new List<RoomEntrance>();
                EntranceDirection entranceDirection = getOppositeEntranceDirection(node.parentExit.direction);
                // No specified direction, can use any entrance
                if (entranceDirection == EntranceDirection.NONE) validEntrances = new List<RoomEntrance>(randomRoom.entrances);
                // Only find entrances of specified direction
                else
                {
                    for (int i = 0; i < randomRoom.entrances.Count; i++)
                    {
                        if (randomRoom.entrances[i].direction == entranceDirection) validEntrances.Add(randomRoom.entrances[i]);
                    }
                }
                // Verify that there exists at least 1 valid entrance
                if (validEntrances.Count == 0) { node.roomPrefabData = null; continue; }

                // Randomly pick a valid entrance to position current room
                while (validEntrances.Count > 0)
                {
                    // Get entrance and room positioning data
                    RoomEntrance randomEntrance = popRandom(validEntrances);
                    node.roomCenter = node.parentExit.position - randomEntrance.position;

                    // Get open entrances (Remove used entrance and keep others)
                    node.openExits = new List<RoomEntrance>(randomRoom.entrances);
                    node.openExits.Remove(randomEntrance);

                    // Validate room
                    if (!validateRoom(node, graph)) continue;

                    // Room fits, mark it as visited
                    node.visited = true;

                    // Recurse
                    bool childrenResult = true;
                    for (int i = 0; i < node.connectedNodes.Count; i++)
                    {
                        RoomNode connectedRoom = node.connectedNodes[i];

                        // Check that the room is not already generated
                        //     (FOR GENERATION OF LOOPS)
                        if (connectedRoom.visited) continue;

                        // Randomly pick an open exit
                        bool childResult = false;
                        List<RoomEntrance> validExits = new List<RoomEntrance>(node.openExits);
                        while (validExits.Count > 0)
                        {
                            // Get random exit
                            RoomEntrance randomExit = popRandom(validExits);

                            // Remove this exit from list of open exits of current node
                            node.openExits.Remove(randomExit);

                            // Calculate world position of exit for current room
                            connectedRoom.parentExit = new RoomEntrance(node.roomCenter + randomExit.position, randomExit.direction);

                            // Try to generate room at this exit
                            yield return parseRoom(connectedRoom, graph, (result) => childResult = result);
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
                    resetRoom(node, graph);
                }

                // No valid entrances, try next room
                node.roomPrefabData = null;
                node.openExits = new List<RoomEntrance>();
            }

            // No suitable rooms found
            //Debug.Log("No suitable rooms found on " + node.type);
            result(false);
            yield break;
        }

        private void roomParseSuccessCallback(List<RoomNode> parseResult, bool result)
        {
            runningRoomParseCoroutines--;
            if (!result) return;

            // Stop generation
            StopAllCoroutines();

            // Create room game objects
            roomGraph = parseResult;
            createRoomGameObjects();

            // Clean up
            patternParseQueue.Clear();
            roomParseQueue.Clear();
            runningPatternParseCoroutines = 0;
            runningRoomParseCoroutines = 0;

            success = true;
            Debug.Log("Level successfully generated");
        }

        private void createRoomGameObjects()
        {
            foreach (RoomNode node in roomGraph)
            {
                if (node.type == "startNode" || node.type == "endNode") continue;

                if (node.roomPrefabData == null) Debug.Log("NO ROOM PREFAB DATA");
                if (node.roomPrefabData.roomPrefab == null) Debug.Log("NO ROOM PREFAB OBJECT");
                // Room fits, generate it
                GameObject newRoomGameObject = Instantiate(node.roomPrefabData.roomPrefab, node.roomCenter, Quaternion.identity);
                node.roomGameObject = newRoomGameObject;
            }
        }

        #endregion

        #region Helpers

        /*
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
        */

        /*
        private RoomNode findRoomById(int id, List<RoomNode> graph)
        {
            foreach (RoomNode node in graph)
            {
                if (node.id == id) return node;
            }
            return null;
        }
        */

        private RoomNode findRoomByType(string type, List<RoomNode> graph)
        {
            foreach (RoomNode node in graph)
            {
                if (node.type == type) return node;
            }
            return null;
        }

        private DataNode findRoomByType(string type, List<DataNode> graph)
        {
            foreach (DataNode node in graph)
            {
                if (node.type == type) return node;
            }
            return null;
        }

        /*
        private void removeById(int id, List<RoomNode> graph)
        {
            List<RoomNode> nodesToRemove = new List<RoomNode>();
            foreach (RoomNode node in graph)
            {
                if (node.id == id) nodesToRemove.Add(node);
            }
            foreach (RoomNode nodeToRemove in nodesToRemove) graph.Remove(nodeToRemove);
            nodesToRemove.Clear();
        }
        */

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

        private List<RoomNode> convertToRoomGraph(List<DataNode> dataGraph)
        {
            List<RoomNode> newRoomGraph = new List<RoomNode>();

            // Clear visited flags
            foreach (DataNode node in dataGraph) node.visited = false;

            // Initialize queue
            // KeyValuePair: original node in data node graph, parent node in room node graph
            Queue<KeyValuePair<DataNode, RoomNode>> convertQueue = new Queue<KeyValuePair<DataNode, RoomNode>>();
            convertQueue.Enqueue(new KeyValuePair<DataNode, RoomNode>(findRoomByType("startNode", dataGraph), null));

            // Clone each node
            while (convertQueue.Count > 0)
            {
                // Create new node
                KeyValuePair<DataNode, RoomNode> nextNode = convertQueue.Dequeue();
                RoomNode newNode = new RoomNode(nextNode.Key.type, nextNode.Key.entranceCount, new List<RoomNode>(), nextNode.Key.terminal);
                newRoomGraph.Add(newNode);

                // Create connection to and from parent
                if (nextNode.Value != null)
                {
                    nextNode.Value.connectedNodes.Add(newNode);
                    newNode.connectedNodes.Add(nextNode.Value);
                }

                // Find connected nodes to recurse
                nextNode.Key.visited = true;
                foreach (DataNode node in dataGraph)
                {
                    if (nextNode.Key.connections.Contains(node.id) && !node.visited) convertQueue.Enqueue(new KeyValuePair<DataNode, RoomNode>(node, newNode));
                }
            }


            /*
            // Start converting from start node
            DataNode startNode = findRoomByType("startNode", dataGraph);
            convertToRoomNode(startNode, dataGraph, newRoomGraph);
            */

            return newRoomGraph;
        }

        private RoomNode convertToRoomNode(DataNode dataNode, List<DataNode> dataGraph, List<RoomNode> newRoomGraph)
        {
            // Create new room node based on current data node
            dataNode.visited = true;
            RoomNode newRoomNode = new RoomNode(dataNode.type, dataNode.entranceCount, new List<RoomNode>(), dataNode.terminal);
            newRoomGraph.Add(newRoomNode);

            // Recurse over each connected child node
            foreach (DataNode otherNode in dataGraph)
            {
                if (dataNode.connections.Contains(otherNode.id) && !otherNode.visited)
                {
                    RoomNode otherRoomNode = convertToRoomNode(otherNode, dataGraph, newRoomGraph);
                    
                    // Add connection between the two nodes
                    newRoomNode.connectedNodes.Add(otherRoomNode);
                    otherRoomNode.connectedNodes.Add(newRoomNode);
                }
            }

            return newRoomNode;
        }

        private List<RoomNode> cloneGraph(List<RoomNode> graph)
        {
            // Clear visited flag
            foreach (RoomNode node in graph) node.visited = false;

            // Initialize queue
            List<RoomNode> clone = new List<RoomNode>();
            // KeyValuePair: original node in graph, parent node in clone
            Queue<KeyValuePair<RoomNode, RoomNode>> cloneQueue = new Queue<KeyValuePair<RoomNode, RoomNode>>();
            cloneQueue.Enqueue(new KeyValuePair<RoomNode, RoomNode>(findRoomByType("startNode", graph), null));

            // Clone each node
            while (cloneQueue.Count > 0)
            {
                // Create new node
                KeyValuePair<RoomNode, RoomNode> nextNode = cloneQueue.Dequeue();
                RoomNode newNode = new RoomNode(nextNode.Key.type, nextNode.Key.entranceCount, new List<RoomNode>(), nextNode.Key.terminal);
                clone.Add(newNode);

                // Create connection to and from parent
                if (nextNode.Value != null)
                {
                    nextNode.Value.connectedNodes.Add(newNode);
                    newNode.connectedNodes.Add(nextNode.Value);
                }

                // Find connected nodes to recurse
                FOR SOME REASON CONNECTED NODE IS DESYNCED FROM THE ACTUAL NODE
                nextNode.Key.visited = true;
                foreach (RoomNode node in nextNode.Key.connectedNodes)
                {
                    if (!node.visited) cloneQueue.Enqueue(new KeyValuePair<RoomNode, RoomNode>(node, newNode));
                }
            }

            return clone;
        }

        private T popRandom<T>(List<T> possibilities)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibilities.Count);
            T result = possibilities[randomIndex];
            possibilities.RemoveAt(randomIndex);
            return result;
        }

        private EntranceDirection getOppositeEntranceDirection(EntranceDirection direction)
        {
            switch (direction)
            {
                case EntranceDirection.LEFT:
                    return EntranceDirection.RIGHT;
                case EntranceDirection.RIGHT:
                    return EntranceDirection.LEFT;
                case EntranceDirection.TOP:
                    return EntranceDirection.BOTTOM;
                case EntranceDirection.BOTTOM:
                    return EntranceDirection.TOP;
                default:
                    return EntranceDirection.NONE;
            }
        }

        #region Room validation

        private bool validateRoom(RoomNode node, List<RoomNode> graph)
        {
            // Check if room overlaps any existing rooms
            if (checkRoomOverlap(node, graph)) return false;

            // Check if any open exits are blocked by an already existing room
            if (checkExitsBlocked(node, graph)) return false;

            // Check if new room will block an open exit of any other room
            if (checkBlockingOtherExits(node, graph)) return false;

            // Check if room is connected to every generated connected room that it should be connected to
            //     (FOR GENERATION OF LOOPS)
            if (!checkConnected(node, graph)) return false;
            return true;
        }

        // Returns true if given room (roomData and roomCenter) overlaps room stored in node or any of its children
        private bool checkRoomOverlap(RoomNode node, List<RoomNode> graph)
        {
            foreach (RoomNode otherRoom in graph)
            {
                // Check if other node has no instantiated room
                if (!otherRoom.visited || !otherRoom.roomPrefabData) continue;

                // Check for overlap with current room node
                if (Mathf.Abs(node.roomCenter.x - otherRoom.roomCenter.x) * 2 < (node.roomPrefabData.size.x + otherRoom.roomPrefabData.size.x) &&
                    Mathf.Abs(node.roomCenter.y - otherRoom.roomCenter.y) * 2 < (node.roomPrefabData.size.y + otherRoom.roomPrefabData.size.y))
                {
                    return true;
                }
            }
            return false;
        }

        // Returns true if any exit is blocked
        private bool checkExitsBlocked(RoomNode node, List<RoomNode> graph)
        {
            List<RoomEntrance> exits = node.openExits;
            foreach (RoomEntrance exit in exits)
            {
                Vector2 checkPosition = exit.position + node.roomCenter;
                switch (exit.direction)
                {
                    case EntranceDirection.LEFT:
                        checkPosition.x -= 0.1f;
                        break;
                    case EntranceDirection.RIGHT:
                        checkPosition.x += 0.1f;
                        break;
                    case EntranceDirection.TOP:
                        checkPosition.y += 0.1f;
                        break;
                    case EntranceDirection.BOTTOM:
                        checkPosition.y -= 0.1f;
                        break;
                    default:
                        // Undefined exit error
                        Debug.Log("Checking exit with undefined direction");
                        return true;
                }

                foreach (RoomNode otherNode in graph)
                {
                    // Check if other node has no instantiated room
                    if (!otherNode.visited || !otherNode.roomPrefabData) continue;

                    // Check if the room should be connected anyways
                    if (otherNode.connectedNodes.Contains(node)) continue;

                    float otherRoomLeft = otherNode.roomCenter.x - otherNode.roomPrefabData.size.x / 2;
                    float otherRoomRight = otherNode.roomCenter.x + otherNode.roomPrefabData.size.x / 2;
                    float otherRoomTop = otherNode.roomCenter.y + otherNode.roomPrefabData.size.y / 2;
                    float otherRoomBottom = otherNode.roomCenter.y - otherNode.roomPrefabData.size.y / 2;

                    // Check for overlap with current room node
                    if (otherRoomLeft <= checkPosition.x && checkPosition.x <= otherRoomRight &&
                        otherRoomBottom <= checkPosition.y && checkPosition.y <= otherRoomTop)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Returns true if current room blocks open exits of any other room
        private bool checkBlockingOtherExits(RoomNode node, List<RoomNode> graph)
        {
            float roomLeft = node.roomCenter.x - node.roomPrefabData.size.x / 2;
            float roomRight = node.roomCenter.x + node.roomPrefabData.size.x / 2;
            float roomTop = node.roomCenter.y + node.roomPrefabData.size.y / 2;
            float roomBottom = node.roomCenter.y - node.roomPrefabData.size.y / 2;

            foreach (RoomNode otherNode in graph)
            {
                // Check if other node has no instantiated room
                if (!otherNode.visited || !otherNode.roomPrefabData) continue;

                // Check if other room should be connected to current room anyways
                if (otherNode.connectedNodes.Contains(node)) continue;

                //if (otherRoom.type == "basicTestJunction") Debug.Log("JUNCTION WITH " + otherRoom.openExits.Count + " OPEN EXITS");

                foreach (RoomEntrance exit in otherNode.openExits)
                {
                    Vector2 checkPosition = exit.position + otherNode.roomCenter;
                    switch (exit.direction)
                    {
                        case EntranceDirection.LEFT:
                            checkPosition.x -= 0.1f;
                            break;
                        case EntranceDirection.RIGHT:
                            checkPosition.x += 0.1f;
                            break;
                        case EntranceDirection.TOP:
                            checkPosition.y += 0.1f;
                            break;
                        case EntranceDirection.BOTTOM:
                            checkPosition.y -= 0.1f;
                            break;
                        default:
                            // Undefined exit error
                            Debug.Log("Checking exit with undefined direction");
                            return true;
                    }

                    // Check for overlap with current room node
                    if (roomLeft <= checkPosition.x && checkPosition.x <= roomRight &&
                        roomBottom <= checkPosition.y && checkPosition.y <= roomTop)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool checkConnected(RoomNode node, List<RoomNode> graph)
        {
            if (!node.roomPrefabData) return false;

            foreach (RoomNode connectedNode in node.connectedNodes)
            {
                if (!connectedNode.visited || !connectedNode.roomPrefabData) continue;

                // Check if any entrances match
                bool isConnected = false;
                foreach (RoomEntrance entrance in node.roomPrefabData.entrances)
                {
                    foreach (RoomEntrance connectedEntrance in connectedNode.roomPrefabData.entrances)
                    {
                        if (entrance.position + node.roomCenter == connectedEntrance.position + connectedNode.roomCenter)
                        {
                            isConnected = true;
                            break;
                        }
                    }
                    if (isConnected) break;
                }
                if (!isConnected) return false;
            }

            // All rooms connected
            return true;
        }

        #endregion

        private void resetRoom(RoomNode node, List<RoomNode> graph)
        {
            node.visited = false;
            node.openExits.Clear();

            // Recursively destroy children nodes
            foreach (RoomNode connectedNode in node.connectedNodes)
            {
                if (connectedNode != node.parentNode) resetRoom(connectedNode, graph);
            }
        }

        private void destroyGeneratedRooms(List<RoomNode> graph)
        {
            foreach (RoomNode node in graph)
            {
                if (node.roomGameObject != null) Destroy(node.roomGameObject);
            }
        }

        #endregion
    }
}
