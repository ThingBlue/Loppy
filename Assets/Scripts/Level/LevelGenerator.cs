using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        [SerializeField]
        private uint runningPatternParseCoroutines = 0;
        private DecisionNode decisionTree;

        private Queue<List<RoomNode>> roomParseQueue;
        [SerializeField]
        private uint runningRoomParseCoroutines = 0;

        private List<RoomNode> roomGraph;
        private bool parseSuccess = false;

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
                //StartCoroutine(generateLevel("basicTestLevel"));
                StartCoroutine(generateLevel("largeTestLevel"));
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                List<RoomNode> og = new List<RoomNode>();
                RoomNode a = new RoomNode("startNode", true, new List<RoomNode>());
                RoomNode b = new RoomNode("b", true, new List<RoomNode>());
                RoomNode c = new RoomNode("c", true, new List<RoomNode>());
                a.connectedNodes.Add(b);
                b.connectedNodes.Add(a);
                b.connectedNodes.Add(c);
                c.connectedNodes.Add(b);
                a.parentNode = null;
                b.parentNode = a;
                c.parentNode = b;
                og.Add(a);
                og.Add(b);
                og.Add(c);
                List<RoomNode> cl = cloneGraph(og);
                cl[1].connectedNodes[1].terminal = false;
                Debug.Log("Terminal: " + cl[2].terminal);
            }
            if (Input.GetKeyDown(KeyCode.I))
            {
                List<RoomNode> og = new List<RoomNode>();
                RoomNode a = new RoomNode("startNode", true, new List<RoomNode>());
                RoomNode b = new RoomNode("b", true, new List<RoomNode>());
                RoomNode c = new RoomNode("c", true, new List<RoomNode>());
                a.connectedNodes.Add(b);
                b.connectedNodes.Add(a);
                b.connectedNodes.Add(c);
                c.connectedNodes.Add(b);
                a.parentNode = null;
                b.parentNode = a;
                c.parentNode = b;
                og.Add(a);
                og.Add(b);
                og.Add(c);
                List<RoomNode> cl = cloneGraph(og);
                cl[2].parentNode.terminal = false;
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                List<RoomNode> og = new List<RoomNode>();
                RoomNode a = new RoomNode("startNode", true, new List<RoomNode>());
                RoomNode b = new RoomNode("basicTestCorridor", true, new List<RoomNode>());
                RoomNode c = new RoomNode("basicTestShopPattern", false, new List<RoomNode>());
                a.connectedNodes.Add(b);
                b.connectedNodes.Add(a);
                b.connectedNodes.Add(c);
                c.connectedNodes.Add(b);
                a.parentNode = null;
                b.parentNode = a;
                c.parentNode = b;
                og.Add(a);
                og.Add(b);
                og.Add(c);
                replacePattern(c, 1, og);
                replacePattern(og[2], 0, og);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                /*
                foreach (List<RoomNode> patternParseResultClone in roomParseQueue)
                {
                    string patternDebugString = "Clone: ";
                    foreach (RoomNode node in patternParseResultClone)
                    {
                        patternDebugString += " [" + node.type + "]";
                    }
                    Debug.Log(patternDebugString);
                }
                */
                Debug.Log("Total patterns: " + roomParseQueue.Count);
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
            // Reset flags
            StopAllCoroutines();
            parseSuccess = false;
            runningPatternParseCoroutines = 0;
            runningRoomParseCoroutines = 0;
            patternParseQueue.Clear();
            roomParseQueue.Clear();

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

        private void roomParseSuccessCallback(List<RoomNode> parseResult, bool result)
        {
            runningRoomParseCoroutines--;

            // Check result returned from parsing
            if (!result) return;

            // Check that all nodes have the correct number of connections
            foreach (RoomNode node in parseResult)
            {
                if (node.type == "startNode" || node.type == "endNode") continue;
                if (node.connectedNodes.Count != node.roomPrefabData.entrances.Count)
                {
                    Debug.Log("Generated level does not have correct connections, check level graph in editor");
                    return;
                }
            }

            // Stop generation
            parseSuccess = true;
            StopAllCoroutines();

            // Create room game objects
            roomGraph = parseResult;
            StartCoroutine(createRoomGameObjects());

            // Clean up
            patternParseQueue.Clear();
            roomParseQueue.Clear();
            runningPatternParseCoroutines = 0;
            runningRoomParseCoroutines = 0;
        }

        #region Pattern parsing

        private IEnumerator startPatternParse()
        {
            decisionTree = new DecisionNode();
            patternParseQueue.Clear();
            patternParseQueue.Enqueue(roomGraph);

            for (int i = 0; i < maxPatternParseCoroutines; i++)
            {
                StartCoroutine(parsePatterns(roomGraph));
            }

            yield break;            
        }

        private IEnumerator parsePatterns(List<RoomNode> graph)
        {
            Queue<RoomNode> parseQueue = new Queue<RoomNode>();

            // Keep producing new rooms until room parse finishes
            while (!parseSuccess)
            {
                // Slow down evaluation to reduce load
                yield return new WaitForSeconds(0.1f);

                // Initialize/reset for loop
                List<RoomNode> clone = new List<RoomNode>(cloneGraph(graph));
                foreach (RoomNode node in clone) node.visited = false;

                parseQueue.Clear();
                RoomNode startNode = findRoomByType("startNode", clone);
                parseQueue.Enqueue(startNode);

                DecisionNode decisionNode = decisionTree;

                // Iteratively go through every node
                bool exists = false;
                while (parseQueue.Count > 0)
                {
                    // Get node from queue
                    RoomNode node = parseQueue.Dequeue();
                    node.visited = true;

                    // Find neighbours
                    List<RoomNode> nonterminalNeighbours = new List<RoomNode>();
                    foreach (RoomNode neighbour in node.connectedNodes)
                    {
                        if (neighbour.visited) continue;

                        // Check for terminal node
                        if (neighbour.terminal)
                        {
                            // Enqueue and recurse
                            parseQueue.Enqueue(neighbour);
                            continue;
                        }

                        // Nonterminal node
                        nonterminalNeighbours.Add(neighbour);
                    }

                    // Parse nonterminal neighbours
                    if (nonterminalNeighbours.Count > 0) parseQueue.Enqueue(node);
                    foreach (RoomNode nonterminalNeighbour in nonterminalNeighbours)
                    {
                        // Make random decision and replace pattern
                        int randomIndex = UnityEngine.Random.Range(0, patternDictionary[nonterminalNeighbour.type].Count);
                        RoomNode firstNode = replacePattern(nonterminalNeighbour, randomIndex, clone);

                        // Update decision tree
                        // Current sequence already exists
                        if (decisionNode.children.ContainsKey(randomIndex))
                        {
                            exists = true;
                            decisionNode = decisionNode.children[randomIndex];
                        }
                        // Current sequence does not exist
                        else
                        {
                            decisionNode.children.Add(randomIndex, new DecisionNode());
                            decisionNode = decisionNode.children[randomIndex];
                        }

                        // Clean up
                        nonterminalNeighbour.Dispose();
                    }
                    // Clean up
                    nonterminalNeighbours.Clear();
                }

                // Report this result if it does not already exist
                if (!exists)
                {
                    string patternDebugString = "Clone: ";
                    foreach (RoomNode node in clone)
                    {
                        patternDebugString += " [" + node.type + "]";
                    }
                    Debug.Log(patternDebugString);

                    roomParseQueue.Enqueue(clone);
                }
                else
                {
                    // Clean up
                    foreach (RoomNode node in clone) node.Dispose();
                    clone.Clear();
                }

                // We have enough finished patterns for now
                if (roomParseQueue.Count >= maxRoomParseCoroutines) yield return new WaitUntil(() => roomParseQueue.Count < maxRoomParseCoroutines);
            }
        }


        // Replaces pattern using the resultIndex-th result in patternDictionary
        // Returns the first non-startNode node of the new pattern
        private RoomNode replacePattern(RoomNode patternNode, int resultIndex, List<RoomNode> graph)
        {
            // Replace pattern with new pattern
            List<RoomNode> patternResult = cloneGraph(patternDictionary[patternNode.type][resultIndex]);

            // Find start and end nodes
            RoomNode startNode = findRoomByType("startNode", patternResult);
            RoomNode endNode = findRoomByType("endNode", patternResult);

            // Remove nonterminal node and replace it with its result in parse graph
            patternResult.Remove(startNode);
            if (endNode != null) patternResult.Remove(endNode);
            graph.AddRange(patternResult);
            if (!graph.Remove(patternNode)) Debug.LogError("Failed to remove pattern node from graph");

            // Find first, last, next, and previous nodes
            RoomNode firstNode = startNode.connectedNodes[0];
            RoomNode previousNode = patternNode.parentNode;
            RoomNode lastNode = endNode != null ? endNode.connectedNodes[0] : null;
            RoomNode nextNode = null;

            // Connect first node to previous node
            previousNode.connectedNodes.Add(firstNode);
            firstNode.connectedNodes.Add(previousNode);
            // Remove old connection
            previousNode.connectedNodes.Remove(patternNode);
            // Set new parent
            firstNode.parentNode = previousNode;
            // Remove start node from first node
            firstNode.connectedNodes.Remove(startNode);

            // Connect last node to next node if it exists
            if (endNode != null)
            {
                foreach (RoomNode connectedNode in patternNode.connectedNodes)
                {
                    if (!patternNode.parentNode.Equals(connectedNode))
                    {
                        nextNode = connectedNode;
                        break;
                    }
                }
                if (nextNode == null)
                {
                    Debug.LogWarning("nextNode null");
                }
                foreach (RoomNode connectedNode in patternNode.connectedNodes)
                {
                    if (!patternNode.parentNode.Equals(connectedNode))
                    {
                        nextNode = connectedNode;
                        break;
                    }
                }
                if (lastNode == null) Debug.LogWarning("lastNode null");

                // Link with last node
                lastNode.connectedNodes.Add(nextNode);
                nextNode.connectedNodes.Add(lastNode);
                // Remove old connection
                nextNode.connectedNodes.Remove(patternNode);
                // Set new parent
                nextNode.parentNode = lastNode;
                // Remove end node from last node
                lastNode.connectedNodes.Remove(endNode);

                // Clean up
                endNode.Dispose();
            }

            // Clean up
            startNode.Dispose();
            patternResult.Clear();

            return firstNode;
        }

        #endregion

        #region Room graph parsing

        private IEnumerator startRoomParse()
        {
            // Wait until pattern parse yields enough results to begin new coroutine
            if (roomParseQueue.Count == 0) yield return new WaitUntil(() => roomParseQueue.Count > 0);

            // Start parse
            while (!parseSuccess)
            {
                // Grab next finished map
                List<RoomNode> cloneToParse = roomParseQueue.Dequeue();

                // Reset visited status of all nodes
                foreach (RoomNode node in cloneToParse) node.visited = false;

                // Start parsing through start node
                RoomNode startNode = findRoomByType("startNode", cloneToParse);
                startNode.visited = true;
                RoomNode firstNode = startNode.connectedNodes[0];
                firstNode.parentExit = new RoomEntrance(Vector2.zero, EntranceDirection.NONE);
                runningRoomParseCoroutines++;
                StartCoroutine(parseRoom(firstNode, cloneToParse, (result) => roomParseSuccessCallback(cloneToParse, result)));

                // Wait until we have another finished map to parse
                if (roomParseQueue.Count == 0 ||
                    runningRoomParseCoroutines >= maxRoomParseCoroutines)
                {
                    yield return new WaitUntil(() => (roomParseQueue.Count > 0 && runningRoomParseCoroutines < maxRoomParseCoroutines) ||
                                                     parseSuccess);
                }
            }
        }

        private IEnumerator parseRoom(RoomNode node, List<RoomNode> graph, Action<bool> result)
        {
            // Slow down evaluation to reduce load
            yield return new WaitForSeconds(0.01f);

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

        private IEnumerator createRoomGameObjects()
        {
            foreach (RoomNode node in roomGraph)
            {
                // Slow down evaluation to reduce load
                //yield return new WaitForSeconds(0.1f);

                if (node.type == "startNode" || node.type == "endNode") continue;

                if (node.roomPrefabData == null)
                {
                    Debug.Log("NO ROOM PREFAB DATA");
                }
                if (node.roomPrefabData.roomPrefab == null) Debug.Log("NO ROOM PREFAB OBJECT");
                // Room fits, generate it
                GameObject newRoomGameObject = Instantiate(node.roomPrefabData.roomPrefab, node.roomCenter, Quaternion.identity);
                node.roomGameObject = newRoomGameObject;
            }

            Debug.Log("Level successfully generated");
            yield break;
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

        private DataNode findRoomById(int id, List<DataNode> graph)
        {
            foreach (DataNode node in graph)
            {
                if (node.id == id) return node;
            }
            return null;
        }

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

            List<RoomNode> result = cloneGraph(patternDictionary[pattern][randomIndex]);
            return result;
        }

        private List<RoomNode> convertToRoomGraph(List<DataNode> dataGraph)
        {
            // Find start node and make new one
            DataNode oldStartNode = findRoomByType("startNode", dataGraph);
            RoomNode newStartNode = new RoomNode("startNode", true, new List<RoomNode>());

            // Initialize clone list
            List<RoomNode> newRoomGraph = new List<RoomNode>();
            newRoomGraph.Add(newStartNode);

            // Initialize queue
            Queue<DataNode> convertQueue = new Queue<DataNode>();
            convertQueue.Enqueue(oldStartNode);

            // Initialize map
            Dictionary<DataNode, RoomNode> convertMap = new Dictionary<DataNode, RoomNode>();
            convertMap.Add(oldStartNode, newStartNode);

            // Clone each node
            while (convertQueue.Count > 0)
            {
                // Get node from queue
                DataNode currentNode = convertQueue.Dequeue();

                // Connect it to each neighbour node that has already been cloned
                foreach (int connection in currentNode.connections)
                {
                    DataNode neighbour = findRoomById(connection, dataGraph);

                    if (!convertMap.ContainsKey(neighbour))
                    {
                        // Clone of neighbour node does not exist yet, create it
                        RoomNode neighbourClone = new RoomNode(neighbour.type, neighbour.terminal, new List<RoomNode>());
                        convertMap.Add(neighbour, neighbourClone);
                        newRoomGraph.Add(neighbourClone);

                        // Create connection
                        convertMap[currentNode].connectedNodes.Add(neighbourClone);
                        neighbourClone.parentNode = convertMap[currentNode];

                        // Enqueue
                        convertQueue.Enqueue(neighbour);
                    }
                    else
                    {
                        // Clone of neighbour exists, create connection
                        convertMap[currentNode].connectedNodes.Add(convertMap[neighbour]);
                    }
                }
            }

            return newRoomGraph;
        }

        private List<RoomNode> cloneGraph(List<RoomNode> graph)
        {
            // Find start node and make new one
            RoomNode oldStartNode = findRoomByType("startNode", graph);
            RoomNode newStartNode = new RoomNode("startNode", true, new List<RoomNode>());

            // Initialize clone list
            List<RoomNode> clone = new List<RoomNode>();
            clone.Add(newStartNode);

            // Initialize queue
            Queue<RoomNode> cloneQueue = new Queue<RoomNode>();
            cloneQueue.Enqueue(oldStartNode);

            // Initialize map
            Dictionary<RoomNode, RoomNode> cloneMap = new Dictionary<RoomNode, RoomNode>();
            cloneMap.Add(oldStartNode, newStartNode);

            // Clone each node
            while (cloneQueue.Count > 0)
            {
                // Get node from queue
                RoomNode currentNode = cloneQueue.Dequeue();

                // Connect it to each neighbour node that has already been cloned
                foreach (RoomNode neighbour in currentNode.connectedNodes)
                {
                    if (!cloneMap.ContainsKey(neighbour))
                    {
                        // Clone of neighbour node does not exist yet, create it
                        RoomNode neighbourClone = new RoomNode(neighbour.type, neighbour.terminal, new List<RoomNode>());
                        cloneMap.Add(neighbour, neighbourClone);
                        clone.Add(neighbourClone);

                        // Create connection
                        cloneMap[currentNode].connectedNodes.Add(neighbourClone);
                        neighbourClone.parentNode = cloneMap[currentNode];

                        // Enqueue
                        cloneQueue.Enqueue(neighbour);
                    }
                    else
                    {
                        // Clone of neighbour exists, create connection
                        cloneMap[currentNode].connectedNodes.Add(cloneMap[neighbour]);
                    }
                }
            }
            return clone;
        }

        private List<RoomNode> cloneGraphAndGetDoppelganger(List<RoomNode> graph, RoomNode original, ref RoomNode doppelganger)
        {
            // Find start node and make new one
            RoomNode oldStartNode = findRoomByType("startNode", graph);
            RoomNode newStartNode = new RoomNode("startNode", true, new List<RoomNode>());

            // Initialize clone list
            List<RoomNode> clone = new List<RoomNode>();
            clone.Add(newStartNode);

            // Initialize queue
            Queue<RoomNode> cloneQueue = new Queue<RoomNode>();
            cloneQueue.Enqueue(oldStartNode);

            // Initialize map
            Dictionary<RoomNode, RoomNode> cloneMap = new Dictionary<RoomNode, RoomNode>();
            cloneMap.Add(oldStartNode, newStartNode);

            // Clone each node
            while (cloneQueue.Count > 0)
            {
                // Get node from queue
                RoomNode currentNode = cloneQueue.Dequeue();

                // Connect it to each neighbour node that has already been cloned
                foreach (RoomNode neighbour in currentNode.connectedNodes)
                {
                    if (!cloneMap.ContainsKey(neighbour))
                    {
                        // Clone of neighbour node does not exist yet, create it
                        RoomNode neighbourClone = new RoomNode(neighbour.type, neighbour.terminal, new List<RoomNode>());
                        cloneMap.Add(neighbour, neighbourClone);
                        clone.Add(neighbourClone);

                        // Create connection
                        cloneMap[currentNode].connectedNodes.Add(neighbourClone);
                        neighbourClone.parentNode = cloneMap[currentNode];

                        // Enqueue
                        cloneQueue.Enqueue(neighbour);
                    }
                    else
                    {
                        // Clone of neighbour exists, create connection
                        cloneMap[currentNode].connectedNodes.Add(cloneMap[neighbour]);
                    }
                }
            }
            doppelganger = cloneMap[original];
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
                if (connectedNode != node.parentNode && node.visited) resetRoom(connectedNode, graph);
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
