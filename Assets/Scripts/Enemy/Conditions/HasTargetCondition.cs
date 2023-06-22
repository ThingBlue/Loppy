using Loppy.Enemy.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Conditions
{
    public class HasTargetCondition : BehaviourNode
    {
        public HasTargetCondition(Agent agent) : base(agent) { }

        public override BehaviourState tick()
        {
            return agent.targetTransform == null ?
                BehaviourState.FAILURE :
                BehaviourState.SUCCESS;
        }
    }
}
