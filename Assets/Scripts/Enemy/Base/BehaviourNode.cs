using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Loppy.Enemy.Base
{
    public enum BehaviourState
    {
        NONE = 0,
        PENDING,
        SUCCESS,
        FAILURE
    }

    public class BehaviourNode
    {
        protected BehaviourState state = BehaviourState.NONE;

        public BehaviourNode parent = null;
        protected List<BehaviourNode> children = new List<BehaviourNode>();
        protected Agent agent;

        public BehaviourNode(Agent agent)
        {
            this.agent = agent;
        }

        public BehaviourNode(Agent agent, List<BehaviourNode> children)
        {
            this.agent = agent;
            foreach (BehaviourNode child in children) attachChild(child);
        }

        public virtual BehaviourState tick() => BehaviourState.FAILURE;

        private void attachChild(BehaviourNode child)
        {
            child.parent = this;
            children.Add(child);
        }
        /*
        private void attachToParent(BehaviourNode parent)
        {
            this.parent = parent;
            parent.children.Add(this);
        }
        */
    }
}
