using Codice.CM.Common.Merge;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Loppy.Player
{
    public class PlayerHair : MonoBehaviour
    {
        #region Variables

        public LineRenderer lineRenderer;
        public int length;
        public Vector3[] segmentPositions;
        private Vector3[] segmentVelocities;

        public Transform targetOrigin;
        public float targetDistance = 0.1f;

        public float smoothSpeed = 0.002f;
        public float angleLerpSpeed = 0.05f;

        public bool enableWiggle = true;
        public Transform wiggleOrigin;
        public float wiggleSpeed = 10;
        public float wiggleMagnitude = 20;

        public bool enableRigidity = true;
        public float angleRigidityMultiplier = 0.2f;
        public float smoothRigidityMultiplier = 0.2f;

        #endregion

        private void Start()
        {
            // Initialize line node positions
            lineRenderer.positionCount = length;
            segmentPositions = new Vector3[length];
            segmentVelocities = new Vector3[length];
        }

        private void Update()
        {
            // Handle wiggle
            if (enableWiggle) wiggleOrigin.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * wiggleSpeed) * wiggleMagnitude);

            // Update position of head node
            segmentPositions[0] = targetOrigin.position;

            // Recursively update positions of subsequent nodes
            for (int i = 1; i < segmentPositions.Length; i++)
            {
                // Get direction from last segment
                Vector3 direction = (segmentPositions[i] - segmentPositions[i - 1]).normalized;

                // Lerp direction towards origin direction
                Vector3 targetDirection = Vector3.Lerp(
                    direction,
                    enableWiggle ? wiggleOrigin.right : targetOrigin.right,
                    enableRigidity ? angleLerpSpeed / (i * angleRigidityMultiplier) : angleLerpSpeed);
                
                // Smooth damp position towards position of previous node
                Vector3 targetPosition = segmentPositions[i - 1] + targetDirection.normalized * targetDistance;
                segmentPositions[i] = Vector3.SmoothDamp(
                    segmentPositions[i],
                    targetPosition,
                    ref segmentVelocities[i],
                    enableRigidity ? smoothSpeed * (i * smoothRigidityMultiplier) : smoothSpeed);
            }

            // Apply new positions
            lineRenderer.SetPositions(segmentPositions);
        }
    }
}
