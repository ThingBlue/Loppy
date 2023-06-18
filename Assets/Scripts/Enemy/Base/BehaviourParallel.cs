using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Base
{
    public class BehaviourParallel : BehaviourNode
    {
        public BehaviourParallel(Agent agent) : base(agent) { }
        public BehaviourParallel(Agent agent, List<BehaviourNode> children) : base(agent, children) { }

        // AND gate
        // Returns SUCCESS if all nodes return SUCCESS
        // Returns PENDING if any node returns PENDING
        // Returns FAILURE if any node returns FAILURE
        public override BehaviourState tick()
        {
            foreach (BehaviourNode child in children)
            {
                BehaviourState childState = child.tick();
                switch (childState)
                {
                    case BehaviourState.PENDING:
                        return BehaviourState.PENDING;
                    case BehaviourState.FAILURE:
                        return BehaviourState.FAILURE;
                    case BehaviourState.SUCCESS:
                    case BehaviourState.NONE:
                    default:
                        break;
                }
            }
            return BehaviourState.SUCCESS;
        }
    }
}
