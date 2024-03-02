using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Player
{
    [CreateAssetMenu]
    public class PlayerCombatData : ScriptableObject
    {
        [Header("LIGHT ATTACK")]
        [Tooltip("Size of light attack 1 hurtbox")]
        public Vector2 lightAttack1Size;

        [Tooltip("Positional offset of light attack 1 hurtbox")]
        public Vector2 lightAttack1Offset;
    }
}
