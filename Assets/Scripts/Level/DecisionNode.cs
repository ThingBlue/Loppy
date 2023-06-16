using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    public class DecisionNode
    {
        public Dictionary<int, DecisionNode> children;

        public DecisionNode()
        {
            this.children = new Dictionary<int, DecisionNode>();
        }

        public DecisionNode(Dictionary<int, DecisionNode> children)
        {
            this.children = new Dictionary<int, DecisionNode>(children);
        }
    }
}
