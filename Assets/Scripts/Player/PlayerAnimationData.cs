using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    [CreateAssetMenu]
    public class PlayerAnimationData : ScriptableObject
    {
        [Header("GRAPPLE")]
        [Tooltip("Offset of the grapple line renderer from the player")]
        public float grappleLineRendererOffset = 0.5f;

        [Tooltip("Offset of the grapple line renderer from the player")]
        public float alternateGrappleLineRendererOffset = 0.5f;

        [Header("HAIR")]
        public Vector3 hairFront1TargetOriginLocalPosition = new Vector3(0.05f, 0.35f, 0);
        public Vector3 hairFront2TargetOriginLocalPosition = new Vector3(-0.29f, 0.43f, 0);
        public Vector3 hairBack1TargetOriginLocalPosition = new Vector3(0.07f, 0.295f, 0);
        public Vector3 hairBack2TargetOriginLocalPosition = new Vector3(-0.1f, 0.28f, 0);
        public float hairFront1TargetOriginLocalRotation = 5;
        public float hairFront2TargetOriginLocalRotation = -5;
        public float hairBack1TargetOriginLocalRotation = 3;
        public float hairBack2TargetOriginLocalRotation = -3;
    }
}
