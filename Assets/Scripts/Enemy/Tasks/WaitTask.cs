using Loppy.Enemy.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Tasks
{
    public class WaitTask : BehaviourNode
    {
        public WaitTask(Agent agent) : base(agent) { }

        public override BehaviourState tick()
        {
            state = BehaviourState.PENDING;
            return state;
        }
    }
}
