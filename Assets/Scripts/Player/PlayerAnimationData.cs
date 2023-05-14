using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Player
{
    [CreateAssetMenu]
    public class PlayerAnimationData : ScriptableObject
    {
        [Header("ROTATION")]
        public float rotationSmoothSpeed = 0.1f;

        [Header("HAIR")]
        public List<PlayerHairData> playerHairData;

        [Header("GRAPPLE")]
        [Tooltip("Offset of the grapple line renderer from the player")]
        public float grappleLineRendererOffset = 0.5f;

        [Tooltip("Offset of the grapple line renderer from the player")]
        public float alternateGrappleLineRendererOffset = 0.5f;
    }
}
