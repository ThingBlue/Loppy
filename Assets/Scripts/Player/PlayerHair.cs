using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    public class PlayerHair : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public int length;
        public Vector3[] segmentPositions;
        private Vector3[] segmentVelocities;

        public Transform targetOrigin;
        public float targetDistance = 0.1f;

        public float smoothSpeed = 0.002f;
        public bool useTrailSpeed = true;
        public float trailSpeed = 2000;

        public bool enableWiggle = true;
        public Transform wiggleOrigin;
        public float wiggleSpeed = 10;
        public float wiggleMagnitude = 20;
        public float wiggleRotation = 0;

        private void Start()
        {
            lineRenderer.positionCount = length;
            segmentPositions = new Vector3[length];
            segmentVelocities = new Vector3[length];
        }

        private void Update()
        {
            // Do wiggle
            if (enableWiggle)
            {
                wiggleOrigin.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * wiggleSpeed) * wiggleMagnitude + wiggleRotation);
            }

            // Update position of head node
            segmentPositions[0] = targetOrigin.position;

            // Recursively update positions of subsequent nodes
            for (int i = 1; i < segmentPositions.Length; i++)
            {
                /*
                Vector3 targetPosition = segmentPositions[i - 1] + (segmentPositions[i] - segmentPositions[i - 1]).normalized * targetDistance;
                segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPosition, ref segmentVelocities[i], smoothSpeed);
                */

                if (useTrailSpeed)
                {
                    segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], segmentPositions[i - 1] + targetOrigin.right * targetDistance, ref segmentVelocities[i], smoothSpeed + i / trailSpeed);
                }
                else
                {
                    segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], segmentPositions[i - 1] + targetOrigin.right * targetDistance, ref segmentVelocities[i], smoothSpeed);
                }
            }

            // Apply new positions
            lineRenderer.SetPositions(segmentPositions);
        }
    }
}
