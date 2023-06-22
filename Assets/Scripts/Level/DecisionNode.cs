using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    public class DecisionNode
    {
        public Dictionary<int, DecisionNode> children;
        public bool complete;

        public DecisionNode()
        {
            this.children = new Dictionary<int, DecisionNode>();
            this.complete = false;
        }

        public string printTree(string output)
        {
            if (children.Count == 0) return "";
            output += "[";
            foreach (KeyValuePair<int, DecisionNode> child in children)
            {
                output += child.Key;
                output += child.Value.printTree("");
            }
            output += "]";
            return output;
        }
    }
}
