using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlasticGui.LaunchDiffParameters;

namespace Loppy.Enemy.Base
{
    public class BehaviourSelector : BehaviourNode
    {
        public BehaviourSelector(Agent agent) : base(agent) { }
        public BehaviourSelector(Agent agent, List<BehaviourNode> children) : base(agent, children) { }

        // OR gate
        // Returns SUCCESS if any node returns SUCCESS
        // Returns PENDING if any node returns PENDING
        // Returns FAILURE if all nodes return FAILURE
        public override BehaviourState tick()
        {
            foreach (BehaviourNode child in children)
            {
                BehaviourState childState = child.tick();
                switch (childState)
                {
                    case BehaviourState.PENDING:
                        return BehaviourState.PENDING;
                    case BehaviourState.SUCCESS:
                        return BehaviourState.SUCCESS;
                    case BehaviourState.FAILURE:
                    case BehaviourState.NONE:
                    default:
                        break;
                }
            }
            return BehaviourState.FAILURE;
        }
    }
}
