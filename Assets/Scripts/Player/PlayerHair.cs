using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
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
        private float defaultRotation;

        public float smoothSpeed = 0.002f;
        public float angleLerpSpeed = 0.05f;

        public bool enableWiggle = true;
        public float wiggleSpeed = 10;
        public float wiggleMagnitude = 20;

        #endregion

        private void Start()
        {
            // Initialize line node positions
            lineRenderer.positionCount = length;
            segmentPositions = new Vector3[length];
            segmentVelocities = new Vector3[length];

            // Initialize default rotation
            defaultRotation = targetOrigin.rotation.eulerAngles.z;
        }

        private void Update()
        {
            // Handle wiggle
            if (enableWiggle)
            {
                targetOrigin.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * wiggleSpeed) * wiggleMagnitude + defaultRotation);
            }

            // Update position of head node
            segmentPositions[0] = targetOrigin.position;

            // Recursively update positions of subsequent nodes
            for (int i = 1; i < segmentPositions.Length; i++)
            {
                // Lerp direction towards origin direction
                Vector3 direction = segmentPositions[i] - segmentPositions[i - 1];
                Vector3 targetDirection = Vector3.Lerp(direction, targetOrigin.right, angleLerpSpeed);

                // Smooth damp position towards position of previous node
                Vector3 targetPosition = segmentPositions[i - 1] + targetDirection.normalized * targetDistance;
                segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPosition, ref segmentVelocities[i], smoothSpeed);
            }

            // Apply new positions
            lineRenderer.SetPositions(segmentPositions);
        }
    }
}
