using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Base
{
    public abstract class BehaviourController : MonoBehaviour
    {
        public BehaviourNode tree = null;
        public Agent agent;

        protected void Start()
        {
            tree = initializeTree(agent);
        }

        private void Update()
        {
            if (tree != null) tree.tick();
        }

        protected abstract BehaviourNode initializeTree(Agent agent);
    }
}
