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
    }
}
