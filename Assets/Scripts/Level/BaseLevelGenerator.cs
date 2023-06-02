using GluonGui.Dialog;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.Common.Connection.AskCredentialsToUser;
using static PlasticGui.LaunchDiffParameters;

namespace Loppy.Level
{
    [Serializable]
    public class RoomPatternRule
    {
        public string pattern;
        public RoomPatternNode result;
    }

    [Serializable]
    public class RoomPatternNode
    {
        public string pattern;
        public bool terminal;
        public List<RoomPatternNode> children;

        public RoomPatternNode(string pattern, bool terminal, List<RoomPatternNode> children)
        {
            this.pattern = pattern;
            this.terminal = terminal;
            this.children = children;
        }

        public RoomPatternNode(RoomPatternNode other)
        {
            this.pattern = other.pattern;
            this.terminal = other.terminal;
            this.children = new List<RoomPatternNode>();

            // Recursively initialize new children nodes
            foreach (RoomPatternNode child in other.children)
            {
                this.children.Add(new RoomPatternNode(child));
            }
        }

        public string printTree(string currentString)
        {
            currentString += " [" + pattern;
            foreach (RoomPatternNode child in children)
            {
                currentString += child.printTree("");
            }
            currentString += "] ";
            return currentString;
        }
    }

    [Serializable]
    public class RoomDataNode
    {
        public string type;
        public List<RoomDataNode> children;
        public RoomData roomData;
        public Vector2 roomCenter;
        public GameObject gameObject = null;

        public RoomDataNode(string type, List<RoomDataNode> children, RoomData roomData, Vector2 roomCenter, GameObject gameObject)
        {
            this.type = type;
            this.children = children;
            this.roomData = roomData;
            this.roomCenter = roomCenter;
            this.gameObject = gameObject;
        }

        public RoomDataNode(RoomDataNode other)
        {
            this.type = other.type;
            this.children = new List<RoomDataNode>();
            this.roomData = other.roomData;
            this.roomCenter = other.roomCenter;
            this.gameObject = other.gameObject;

            // Recursively initialize new children nodes
            foreach (RoomDataNode child in other.children)
            {
                this.children.Add(new RoomDataNode(child));
            }
        }

        public string printTree(string currentString)
        {
            currentString += " [" + type;
            foreach (RoomDataNode child in children)
            {
                currentString += child.printTree("");
            }
            currentString += "] ";
            return currentString;
        }
    }

    public class BaseLevelGenerator : MonoBehaviour
    {
        #region Inspector members

        //public LevelGenerationData levelGenerationData;
        public List<GameObject> roomPrefabs;
        public List<RoomPatternRule> rulesList;

        #endregion

        public int randomSeed;

        private Dictionary<string, Dictionary<string, List<RoomData>>> roomsDictionary;
        private Dictionary<string, List<RoomPatternNode>> rulesDictionary;

        private RoomDataNode roomTree;

        private void Start()
        {
            // Initialize storage
            roomsDictionary = new Dictionary<string, Dictionary<string, List<RoomData>>>();
            rulesDictionary = new Dictionary<string, List<RoomPatternNode>>();

            // Fetch and initialize rooms and rules
            StartCoroutine(initializeRooms());
            StartCoroutine(initializeRules());
        }

        // DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                StopAllCoroutines();
                deleteGeneratedRooms(roomTree);

                // Generate test level
                StartCoroutine(generateLevel("test"));
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
                if (!roomsDictionary.ContainsKey(prefabData.region)) roomsDictionary[prefabData.region] = new Dictionary<string, List<RoomData>>();
                if (!roomsDictionary[prefabData.region].ContainsKey(prefabData.type)) roomsDictionary[prefabData.region][prefabData.type] = new List<RoomData>();

                // Add to dictionary
                roomsDictionary[prefabData.region][prefabData.type].Add(prefabData);
            }

            yield break;
        }

        private IEnumerator initializeRules()
        {
            foreach (RoomPatternRule rule in rulesList)
            {
                // Create storage structures if they do not exist
                if (!rulesDictionary.ContainsKey(rule.pattern)) rulesDictionary[rule.pattern] = new List<RoomPatternNode>();

                // Add to dictionary
                rulesDictionary[rule.pattern].Add(rule.result);
            }

            yield break;
        }

        // Repeatedly attempts to generate the level
        // Returns true if level generation successful
        // Returns false if level generation fails 10 times
        public IEnumerator generateLevel(string region)
        {
            int failureCount = 0;

            // Assert that region start exists within rules
            if (!rulesDictionary.ContainsKey(region))
            {
                Debug.Log("Could not find specified level");
                yield break;
            }

            while (failureCount < 10)
            {
                // Reset room tree
                deleteGeneratedRooms(roomTree);
                roomTree = null;

                // Attempt to parse room pattern tree
                if (!parsePattern(pickRandomPatternResult(region), null))
                {
                    Debug.Log("Could not parse pattern!");
                    failureCount++;
                    continue;
                }

                Debug.Log(roomTree.printTree(""));

                // Attempt to generate level
                bool generationResult = false;
                yield return generateRoom(roomTree, Vector2.right * failureCount * 5, EntranceDirection.LEFT, region, roomTree, (result) => generationResult = result);

                // Success
                if (generationResult)
                {
                    Debug.Log("Level successfully generated");
                    yield break;
                }

                // Failure
                failureCount++;
            }
            Debug.Log("Failed to generate level");
        }

        
        // Recursively parses room pattern rules using the node given
        private bool parsePattern(RoomPatternNode node, RoomDataNode parent)
        {
            if (roomTree != null) Debug.Log(roomTree.printTree(""));

            RoomDataNode nextParent = null;

            // Add to parse tree if terminal
            if (node.terminal)
            {
                // Create new node
                RoomDataNode newNode = new RoomDataNode(node.pattern, new List<RoomDataNode>(), null, Vector2.zero, null);

                // If parent is null then current node is root node
                if (parent == null) roomTree = newNode;
                // Parent exists, add new node as child of parent
                else parent.children.Add(newNode);

                // Set next parent for recursion
                nextParent = newNode;
            }
            // Nonterminal, parse current node pattern
            else
            {
                if (!parsePattern(pickRandomPatternResult(node.pattern), parent))
                {
                    return false;
                }

                // Set next parent for recursion
                nextParent = getFinalLeafNode(roomTree);
            }

            // Recurse on children
            bool childrenResult = true;
            foreach (RoomPatternNode child in node.children)
            {
                if (!parsePattern(child, nextParent))
                {
                    childrenResult = false;
                    break;
                }
            }
            return childrenResult;
        }

        /*
        // Recursively parses room pattern rules using the node given
        private bool parsePattern(RoomPatternNode node, RoomDataNode parent)
        {
            //if (roomTree != null) Debug.Log(roomTree.printTree(""));

            RoomDataNode nextParent = null;

            // Add to parse tree if terminal
            if (node.terminal)
            {
                // Create new node
                RoomDataNode newNode = new RoomDataNode(node.pattern, null, Vector2.zero, null, new List<RoomDataNode>());

                // If parent is null then current node is root node
                if (parent == null) roomTree = newNode;
                // Parent exists, add new node as child of parent
                else parent.children.Add(newNode);

                // Set next parent for recursion
                nextParent = newNode;
            }
            // Nonterminal, parse current node pattern
            else
            {
                if (!parsePattern(pickRandomPatternResult(node.pattern), parent))
                {
                    return false;
                }

                // Set next parent for recursion
                nextParent = getFinalLeafNode(roomTree);
            }

            // Recurse on children
            bool childrenResult = true;
            foreach (RoomPatternNode child in node.children)
            {
                if (!parsePattern(child, nextParent))
                {
                    childrenResult = false;
                    break;
                }
            }
            return childrenResult;
        }
        */

        private RoomPatternNode pickRandomPatternResult(string pattern)
        {
            // Check if dictionary contains pattern
            if (!rulesDictionary.ContainsKey(pattern))
            {
                Debug.Log("Rule key " + pattern + " not found");
                return null;
            }

            // Randomly pick pattern
            int randomIndex = UnityEngine.Random.Range(0, rulesDictionary[pattern].Count);
            return rulesDictionary[pattern][randomIndex];
        }

        private RoomDataNode getFinalLeafNode(RoomDataNode node)
        {
            //Debug.Log("Node: " + node.type + ", children count: " + node.children.Count + ", max children: " + node.roomData.maxChildren);

            // Leaf
            if (node.children.Count == 0) return node;

            // Not leaf, recurse on final child
            return getFinalLeafNode(node.children[node.children.Count - 1]);
        }

        // Recursively generates rooms using the level data node given
        private IEnumerator generateRoom(RoomDataNode node, Vector2 entrancePosition, EntranceDirection entranceDirection, string region, RoomDataNode root, Action<bool> result)
        {
            // Check for room errors
            if (entranceDirection == EntranceDirection.NONE)
            {
                result(false);
                yield break;
            }

            // Set seed for randomizer
            //UnityEngine.Random.InitState(randomSeed);

            // Randomly generate room
            List<int> usedIndices = new List<int>(); // List of indices for rooms we have already tried (Rooms that don't fit)
            while (usedIndices.Count < roomsDictionary[region][node.type].Count)
            {
                // Generate new random index
                int randomIndex = UnityEngine.Random.Range(0, roomsDictionary[region][node.type].Count);
                // Check if index has already been used
                if (usedIndices.Contains(randomIndex)) continue;

                // Index has not been used yet
                // Add it to used indices
                usedIndices.Add(randomIndex);

                // Get information about room
                RoomData roomData = roomsDictionary[region][node.type][randomIndex];
                Vector2 roomCenter = entrancePosition - roomData.entrance;

                // Validate room
                // Check if entrance direction matches exit direction of last room
                if (roomData.entranceDirection != entranceDirection) continue;
                // Check if room overlaps any existing rooms
                if (checkRoomOverlap(roomData, roomCenter, root)) continue;

                // Room fits, generate it
                GameObject newRoomGameObject = Instantiate(roomsDictionary[region][node.type][randomIndex].roomPrefab, roomCenter, Quaternion.identity);
                node.gameObject = newRoomGameObject;
                node.roomData = roomData;
                node.roomCenter = roomCenter;

                //yield return new WaitForSeconds(0.2f);

                // Recurse
                bool childrenResult = true;
                for (int i = 0; i < node.children.Count; i++)
                {
                    // Calculate position of exit for current room
                    Vector2 exitPosition = roomCenter + roomData.exits[i];

                    // Calculate entrance direction of next room
                    EntranceDirection nextEntranceDirection = EntranceDirection.NONE;
                    if (roomData.exitDirections[i] == EntranceDirection.LEFT) nextEntranceDirection = EntranceDirection.RIGHT;
                    if (roomData.exitDirections[i] == EntranceDirection.RIGHT) nextEntranceDirection = EntranceDirection.LEFT;
                    if (roomData.exitDirections[i] == EntranceDirection.TOP) nextEntranceDirection = EntranceDirection.BOTTOM;
                    if (roomData.exitDirections[i] == EntranceDirection.BOTTOM) nextEntranceDirection = EntranceDirection.TOP;

                    // Return false if any children fail
                    bool childResult = false;
                    yield return generateRoom((RoomDataNode)(node.children[i]), exitPosition, nextEntranceDirection, region, root, (result) => childResult = result);
                    if (!childResult)
                    {
                        childrenResult = false;
                        break;
                    }
                }

                // All recursions successful, return true
                if (childrenResult)
                {
                    result(true);
                    yield break;
                }

                // Try again if any children fail
                deleteGeneratedRooms(node);
                node.gameObject = null;
            }

            // No suitable rooms found
            result(false);
            yield break;
        }

        // Returns true if given room (roomData and roomCenter) overlaps room stored in node or any of its children
        private bool checkRoomOverlap(RoomData roomData, Vector2 roomCenter, RoomDataNode node)
        {
            // Check if current node has no instantiated room
            if (!node.gameObject) return false;

            // Check for overlap with current room node
            if (Mathf.Abs(roomCenter.x - node.roomCenter.x) * 2 < (roomData.size.x + node.roomData.size.x) &&
                Mathf.Abs(roomCenter.y - node.roomCenter.y) * 2 < (roomData.size.y + node.roomData.size.y))
            {
                return true;
            }

            // Recurse
            bool childrenOverlap = false;
            foreach (RoomDataNode child in node.children)
            {
                if (checkRoomOverlap(roomData, roomCenter, child))
                {
                    childrenOverlap = true;
                    break;
                }
            }

            return childrenOverlap;
        }

        // Delete room game object and data for given node and all children
        private void deleteGeneratedRooms(RoomDataNode node)
        {
            if (node == null) return;

            // End recursion if current node has no instantiated room
            if (!node.gameObject) return;

            // Delete room of current node
            if (node.gameObject) Destroy(node.gameObject);
            node.gameObject = null;
            node.roomData = null;
            node.roomCenter = Vector2.zero;

            // Recursively destroy children rooms
            foreach (RoomDataNode child in node.children) deleteGeneratedRooms(child);
        }
    }
}
