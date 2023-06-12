using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace Loppy.Level
{
    public class LevelGenerator : MonoBehaviour
    {
        public static LevelGenerator instance;

        #region Inspector members

        public string editorDataPath;
        public List<GameObject> roomPrefabs;

        #endregion

        public int randomSeed;

        public int nextId = 0;

        private Dictionary<string, List<RoomPrefabData>> roomDictionary;
        private Dictionary<string, List<List<DataNode>>> patternDictionary;

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
            roomDictionary = new Dictionary<string, List<RoomPrefabData>>();
            patternDictionary = new Dictionary<string, List<List<DataNode>>>();
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

        #endregion

        private IEnumerator generateLevel(string level)
        {
            // Assert that the level exists as a key in patterns
            if (!patternDictionary.ContainsKey(level))
            {
                Debug.LogError("Could not find specified level");
                yield break;
            }

            // Reset parse graph and generated rooms
            destroyGeneratedRooms();

            // Initialize room graph
            List<DataNode> dataGraph = new List<DataNode>(pickRandomPatternResult(level));
            roomGraph = new List<RoomNode>(convertToRoomNodes(dataGraph)); // This also marks start node as generated

            // Connect all nodes
            connectAllNodes(roomGraph);

            /*
            foreach (RoomNode room in roomGraph)
            {
                string connectionDebugString = room.type + " ";
                foreach (RoomNode connectedNode in room.connectedRooms)
                {
                    connectionDebugString += " [" + connectedNode.type + "]";
                }
                Debug.Log(connectionDebugString);
            }
            */

            // Start parsing
            List<RoomNode> parseResult = null;
            yield return parseGraph(roomGraph, parseSuccessCallback);
        }

        private void parseSuccessCallback(List<RoomNode> parseResult)
        {
            roomGraph = parseResult;
            createRoomGameObjects();
            Debug.Log("Level successfully generated");
            StopAllCoroutines();
        }

        /*
         * Decisions for patterns:
         *     Which pattern result?
         * 
         * Decisions for rooms:
         *     Which exit of the previous room?
         *     Which room to pick from dictionary?
         *     Which entrance of current room matching the exit of the previous room?
         */

        private IEnumerator parseGraph(List<RoomNode> graph, Action<List<RoomNode>> successCallback)
        {
            yield return new WaitForSeconds(0.2f);

            // Check if graph is complete
            bool complete = true;
            foreach (RoomNode node in graph)
            {
                if (!node.generated && node.type != "startNode" && node.type != "endNode")
                {
                    complete = false;
                    break;
                }
            }
            if (complete)
            {
                successCallback(graph);
                yield break;
            }

            // Find all connected nodes
            List<RoomNode> nextRooms = findAllConnectedRooms(graph);

            string nextRoomsDebugText = "nextRooms: ";
            foreach (RoomNode node in nextRooms)
            {
                nextRoomsDebugText += " [" + node.type + "]";
            }
            Debug.Log(nextRoomsDebugText);

            // We want to perform an operation on each room that we can perform an operation on at this moment
            // Find all possible combinations of decisions
            List<int> maxDecisions = getMaxDecisions(nextRooms);

            // Initialize current decisions list
            List<int> decisions = new List<int>();
            for (int i = 0; i < maxDecisions.Count; i++) decisions.Add(0);

            // Loop through all possibilities to construct decision tree
            while (decisions[decisions.Count - 1] < maxDecisions[maxDecisions.Count - 1])
            {
                // Clone graph and apply decisions
                List<RoomNode> clone = cloneGraph(graph);

                for (int i = 0; i < nextRooms.Count; i++)
                {
                    RoomNode room = findRoomWithId(nextRooms[i].id, clone);

                    // Pattern
                    if (!nextRooms[i].terminal)
                    {
                        // Replace pattern with new pattern
                        List<DataNode> patternNodes = new List<DataNode>();
                        foreach (DataNode patternNode in patternDictionary[room.type][decisions[i]]) patternNodes.Add(patternNode);
                        
                        assignNewIds(patternNodes);
                        List<RoomNode> patternRooms = convertToRoomNodes(patternNodes);

                        // Find start and end nodes
                        RoomNode startNode = findRoomWithType("startNode", patternRooms);
                        RoomNode endNode = findRoomWithType("endNode", patternRooms);

                        // Remove nonterminal node and replace it with its result in parse graph
                        patternRooms.Remove(startNode);
                        if (endNode != null) patternRooms.Remove(endNode);
                        clone.AddRange(patternRooms);
                        removeById(clone, room.id);

                        // Find first node
                        RoomNode firstNode = findRoomWithId(startNode.connections[0], clone);// startNode.connectedRooms[0];
                        // Find previous node
                        RoomNode previousNode = null;
                        foreach (RoomNode connectedRoom in room.connectedRooms)
                        {
                            if (connectedRoom.generated)
                            {
                                previousNode = connectedRoom;
                                break;
                            }
                        }
                        // Connect first node to previous node
                        previousNode.connections.Add(firstNode.id);
                        firstNode.connections.Add(previousNode.id);
                        previousNode.connections.Remove(room.id);

                        // Connect last node to next node if it exists
                        if (endNode != null)
                        {
                            // Find last node
                            RoomNode lastNode = findRoomWithId(endNode.connections[0], clone); //endNode.connectedRooms[0];
                            // Find next node
                            RoomNode nextNode = null;
                            foreach (RoomNode connectedRoom in room.connectedRooms)
                            {
                                if (!connectedRoom.generated)
                                {
                                    nextNode = connectedRoom;
                                    break;
                                }
                            }

                            // Link with last node
                            lastNode.connections.Add(nextNode.id);
                            nextNode.connections.Add(lastNode.id);
                            nextNode.connections.Remove(room.id);
                        }

                        // Reform connections
                        connectAllNodes(clone);

                        Debug.Log("pattern parse: " + room.type);

                        // Clean up
                        patternNodes.Clear();
                        patternRooms.Clear();
                    }
                    // Exit
                    else if (room.decisionType == DecisionType.EXIT)
                    {
                        // Check if another room has already taken the same exit
                        //      If so, we can safely exit
                        if (room.parent.openExits.Count == 1 && decisions[i] == 1) yield break;

                        // Set previous room exit and remove it from the parent's open exits
                        room.parentExit = room.parent.openExits[decisions[i]];
                        room.parent.openExits.Remove(room.parentExit);

                        room.decisionType = DecisionType.ROOM;

                        Debug.Log("exit parse: " + room.parentExit.position + " " + room.parentExit.direction);
                    }
                    // Room
                    else if (room.decisionType == DecisionType.ROOM)
                    {
                        // Set room
                        room.roomPrefabData = roomDictionary[room.type][decisions[i]];

                        room.decisionType = DecisionType.ENTRANCE;

                        Debug.Log("room parse: " + room.roomPrefabData.name);
                    }
                    // Entrance
                    else if (room.decisionType == DecisionType.ENTRANCE)
                    {
                        // Find valid entrances
                        List<RoomEntrance> validEntrances = getValidEntrances(room);

                        // No valid entrances
                        if (validEntrances.Count == 0) yield break;

                        // Set room entrance
                        room.entrance = validEntrances[decisions[i]];

                        // Calculate room center
                        room.roomCenter = room.parent.roomCenter + room.parentExit.position - room.entrance.position;

                        // Get open entrances (Remove used entrance and keep others)
                        room.openExits = new List<RoomEntrance>(room.roomPrefabData.entrances);
                        room.openExits.Remove(room.entrance);

                        Debug.Log("entrance parse: " + room.entrance.position + " " + room.entrance.direction);

                        // Clean up
                        validEntrances.Clear();

                        // Validate room
                        if (!validateRoom(room)) yield break;

                        // Room fits, mark it as generated
                        room.generated = true;
                        room.decisionType = DecisionType.NONE;

                        Debug.Log("ROOM VALID AND GENERATED: " + room.type);
                    }
                }

                // Recurse
                StartCoroutine(parseGraph(clone, successCallback));

                // Increment
                decisions[0]++;
                for (int i = 0; i < decisions.Count - 1; i++)
                {
                    // Reset current decision and increment next decision
                    if (decisions[i] == maxDecisions[i])
                    {
                        decisions[i] = 0;
                        if (i < decisions.Count - 1) decisions[i + 1]++;
                    }
                }
            }

            // Clean up
            graph.Clear();
            nextRooms.Clear();
            maxDecisions.Clear();
            decisions.Clear();
        }
        
        // Returns a list of all nodes that are connected to currently generated nodes
        private List<RoomNode> findAllConnectedRooms(List<RoomNode> graph)
        {
            List<RoomNode> result = new List<RoomNode>();
            foreach (RoomNode room in graph)
            {
                if (room.generated)
                {
                    foreach (RoomNode connectedRoom in room.connectedRooms)
                    {
                        if (!connectedRoom.generated)
                        {
                            connectedRoom.parent = room;
                            connectedRoom.parentId = room.id;
                            result.Add(connectedRoom);
                            if (connectedRoom.decisionType == DecisionType.NONE) connectedRoom.decisionType = DecisionType.EXIT;
                        }
                    }
                }
            }
            return result;
        }

        // Given a list of nodes that we can perform operations on, returns a list representing
        //     the list of all possible combinations of operations

        // Patterns:
        //     Which pattern? This is easy
        // NEED TO CONSIDER FOR ROOMS:
        //     Which exit of previous room?
        //     Which room?
        //     Which entrance of room?

        private List<int> getMaxDecisions(List<RoomNode> nextNodes)
        {
            // Get max number of operations for each room
            List<int> maxDecisions = new List<int>();
            foreach (RoomNode room in nextNodes)
            {
                // Pattern
                if (!room.terminal)
                {
                    maxDecisions.Add(patternDictionary[room.type].Count);
                }
                // Room
                else
                {
                    switch (room.decisionType)
                    {
                        case DecisionType.NONE:
                            break;
                        case DecisionType.PATTERN:
                            break;
                        case DecisionType.EXIT:
                            maxDecisions.Add(room.parent.openExits.Count);
                            break;
                        case DecisionType.ROOM:
                            maxDecisions.Add(roomDictionary[room.type].Count);
                            break;
                        case DecisionType.ENTRANCE:
                            maxDecisions.Add(getValidEntrances(room).Count);
                            break;
                        default:
                            break;
                    }
                }
            }
            return maxDecisions;
        }

        private List<RoomEntrance> getValidEntrances(RoomNode room)
        {
            // Find entrance direction from parent exit direction
            EntranceDirection entranceDirection = EntranceDirection.NONE;
            switch (room.parentExit.direction)
            {
                case EntranceDirection.LEFT:
                    entranceDirection = EntranceDirection.RIGHT;
                    break;
                case EntranceDirection.RIGHT:
                    entranceDirection = EntranceDirection.LEFT;
                    break;
                case EntranceDirection.TOP:
                    entranceDirection = EntranceDirection.BOTTOM;
                    break;
                case EntranceDirection.BOTTOM:
                    entranceDirection = EntranceDirection.TOP;
                    break;
                default:
                    entranceDirection = EntranceDirection.NONE;
                    break;
            }

            // Get all entrances matching the exit direction of last room
            List<RoomEntrance> validEntrances = new List<RoomEntrance>();
            for (int i = 0; i < room.roomPrefabData.entrances.Count; i++)
            {
                if (room.roomPrefabData.entrances[i].direction == entranceDirection) validEntrances.Add(room.roomPrefabData.entrances[i]);
            }
            return validEntrances;
        }

        private List<RoomNode> cloneGraph(List<RoomNode> graph)
        {
            List<RoomNode> clone = new List<RoomNode>();
            foreach (RoomNode node in graph)
            {
                clone.Add(new RoomNode(node));
            }
            connectAllNodes(clone);
            return clone;
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
                if (node.roomGameObject != null) Destroy(node.roomGameObject);
            }
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

        private RoomNode findRoomWithId(int id, List<RoomNode> rooms)
        {
            foreach (RoomNode nodeData in rooms)
            {
                if (nodeData.id == id) return nodeData;
            }
            return null;
        }

        private RoomNode findRoomWithType(string type, List<RoomNode> rooms)
        {
            foreach (RoomNode room in rooms)
            {
                if (room.type == type) return room;
            }
            return null;
        }

        private List<RoomNode> convertToRoomNodes(List<DataNode> dataGraph)
        {
            List<RoomNode> newRoomGraph = new List<RoomNode>();
            // Convert all DataNodes to RoomNodes
            foreach (DataNode dataNode in dataGraph)
            {
                RoomNode newRoomNode = new RoomNode(dataNode.type, dataNode.entranceCount, dataNode.id, dataNode.connections, dataNode.terminal);

                // Mark start node as generated
                if (newRoomNode.type == "startNode")
                {
                    newRoomNode.generated = true;
                    newRoomNode.openExits.Add(new RoomEntrance(Vector2.zero, EntranceDirection.LEFT));
                }
                newRoomGraph.Add(newRoomNode);
            }
            return newRoomGraph;
        }

        private void connectAllNodes(List<RoomNode> graph)
        {
            // Remove existing connections
            foreach (RoomNode room in graph) room.connectedRooms.Clear();

            // Create new connections
            foreach (RoomNode room in graph)
            {
                foreach (RoomNode otherRoom in graph)
                {
                    if (otherRoom.connections.Contains(room.id) && !otherRoom.connectedRooms.Contains(room))
                    {
                        otherRoom.connectedRooms.Add(room);
                        room.connectedRooms.Add(otherRoom);
                    }

                    // Preserve parenthood
                    if (otherRoom.id == room.parentId) room.parent = otherRoom;
                }
            }
        }

        private void removeById(List<RoomNode> rooms, int id)
        {
            List<RoomNode> roomsToRemove = new List<RoomNode>();
            foreach (RoomNode room in rooms)
            {
                if (room.id == id) roomsToRemove.Add(room);
            }
            foreach (RoomNode roomToRemove in roomsToRemove) rooms.Remove(roomToRemove);
        }

        private T popRandom<T>(List<T> possibilities)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibilities.Count);
            T result = possibilities[randomIndex];
            possibilities.RemoveAt(randomIndex);
            return result;
        }

        private bool validateRoom(RoomNode room)
        {
            // Check if room overlaps any existing rooms
            if (checkRoomOverlap(room)) return false;

            // Check if any open exits are blocked by an already existing room
            //if (checkExitsBlocked(room)) return false;

            // Check if new room will block an open exit of any other room
            //if (checkBlockingOtherExits(room)) return false;

            // Check if room is connected to every generated connected room that it should be connected to
            //     (FOR GENERATION OF LOOPS)
            //if (!checkConnected(room)) return false;
            return true;
        }

        // Returns true if given room (roomData and roomCenter) overlaps room stored in node or any of its children
        private bool checkRoomOverlap(RoomNode room)
        {
            // Check if current node has no instantiated room
            if (room.roomPrefabData == null) return false;

            foreach (RoomNode otherRoom in roomGraph)
            {
                // Check if other node has no instantiated room
                if (!otherRoom.generated || otherRoom.roomPrefabData == null) continue;

                // Check for overlap with current room node
                if (Mathf.Abs(room.roomCenter.x - otherRoom.roomCenter.x) * 2 < (room.roomPrefabData.size.x + otherRoom.roomPrefabData.size.x) &&
                    Mathf.Abs(room.roomCenter.y - otherRoom.roomCenter.y) * 2 < (room.roomPrefabData.size.y + otherRoom.roomPrefabData.size.y))
                {
                    return true;
                }
            }
            return false;
        }

        // Returns true if any exit is blocked
        private bool checkExitsBlocked(RoomNode room)
        {
            List<RoomEntrance> exits = room.openExits;
            foreach (RoomEntrance exit in exits)
            {
                Vector2 checkPosition = exit.position + room.roomCenter;
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

                foreach (RoomNode otherRoom in roomGraph)
                {
                    // Check if other node has no instantiated room
                    if (!otherRoom.generated || otherRoom.roomPrefabData == null) continue;

                    // Check if the room should be connected anyways
                    if (otherRoom.connectedRooms.Contains(room)) continue;

                    float otherRoomLeft = otherRoom.roomCenter.x - otherRoom.roomPrefabData.size.x / 2;
                    float otherRoomRight = otherRoom.roomCenter.x + otherRoom.roomPrefabData.size.x / 2;
                    float otherRoomTop = otherRoom.roomCenter.y + otherRoom.roomPrefabData.size.y / 2;
                    float otherRoomBottom = otherRoom.roomCenter.y - otherRoom.roomPrefabData.size.y / 2;

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
        private bool checkBlockingOtherExits(RoomNode room)
        {
            float roomLeft = room.roomCenter.x - room.roomPrefabData.size.x / 2;
            float roomRight = room.roomCenter.x + room.roomPrefabData.size.x / 2;
            float roomTop = room.roomCenter.y + room.roomPrefabData.size.y / 2;
            float roomBottom = room.roomCenter.y - room.roomPrefabData.size.y / 2;

            foreach (RoomNode otherRoom in roomGraph)
            {
                // Check if other node has no instantiated room
                if (!otherRoom.generated || otherRoom.roomPrefabData == null) continue;

                // Check if other room should be connected to current room anyways
                if (otherRoom.connectedRooms.Contains(room)) continue;

                //if (otherRoom.type == "basicTestJunction") Debug.Log("JUNCTION WITH " + otherRoom.openExits.Count + " OPEN EXITS");

                foreach (RoomEntrance exit in otherRoom.openExits)
                {
                    Vector2 checkPosition = exit.position + otherRoom.roomCenter;
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

        private bool checkConnected(RoomNode room)
        {
            if (room.roomPrefabData == null) return false;

            foreach (RoomNode connectedRoom in room.connectedRooms)
            {
                if (!connectedRoom.generated || connectedRoom.roomPrefabData == null) continue;

                // Check if any entrances match
                bool isConnected = false;
                foreach (RoomEntrance entrance in room.roomPrefabData.entrances)
                {
                    foreach (RoomEntrance connectedEntrance in connectedRoom.roomPrefabData.entrances)
                    {
                        if (entrance.position + room.roomCenter == connectedEntrance.position + connectedRoom.roomCenter)
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

    }
}
