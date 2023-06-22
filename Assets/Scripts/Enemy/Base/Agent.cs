using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Enemy.Base
{
    public class Agent : MonoBehaviour
    {
        #region Inspector members

        public float moveSpeed;

        public Transform targetTransform;
        public float alertRange;
        public float abandonRange;

        public LayerMask playerLayer;

        #endregion

        private void FixedUpdate()
        {
            // Handle target
            // Already have target
            if (targetTransform)
            {
                // Check if target has left range
                if ((transform.position - targetTransform.position).magnitude > abandonRange)
                {
                    targetTransform = null;
                }
            }
            else
            {
                // Find target
                Collider2D[] targetColliders = Physics2D.OverlapCircleAll(transform.position, alertRange, playerLayer);
                if (targetColliders.Length > 0)
                {
                    // TODO: RAYCAST CHECK FOR BLOCKERS
                    targetTransform = targetColliders[0].transform;
                }
            }
        }
    }
}
