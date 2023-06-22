using Loppy.Enemy.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Tasks
{
    public class FollowTargetTask : BehaviourNode
    {
        public FollowTargetTask(Agent agent) : base(agent) { }

        public override BehaviourState tick()
        {
            // Check that we have a target
            if (agent.targetTransform == null)
            {
                state = BehaviourState.FAILURE;
                return state;
            }

            if (Mathf.Abs(agent.transform.position.x - agent.targetTransform.position.x) < 1)
            {
                state = BehaviourState.SUCCESS;
                return state;
            }
            else
            {
                Vector3 newPosition = agent.transform.position;
                newPosition.x = Mathf.MoveTowards(newPosition.x, agent.targetTransform.position.x, agent.moveSpeed * Time.fixedDeltaTime);
                agent.transform.position = newPosition;
                state = BehaviourState.PENDING;
                return state;
            }
        }
    }
}
