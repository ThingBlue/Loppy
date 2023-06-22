using Loppy.Enemy.Base;
using Loppy.Enemy.Tasks;
using Loppy.Enemy.Conditions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Controllers
{
    public class TestController : BehaviourController
    {
        protected override BehaviourNode initializeTree(Agent agent)
        {
            BehaviourNode root = new BehaviourSelector(agent, new List<BehaviourNode>
            {
                new BehaviourParallel(agent, new List<BehaviourNode>
                {
                    new HasTargetCondition(agent),
                    new FollowTargetTask(agent)
                }),
                new WaitTask(agent)
            });
            return root;
        }
    }
}
