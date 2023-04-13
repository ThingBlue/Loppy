using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    public class PlayerAnimation : MonoBehaviour
    {
        #region Inspector members

        public PlayerController playerController;
        public PlayerAnimationData playerAnimationData;

        // Player sprite components
        public SpriteRenderer headSpriteRenderer;
        public SpriteRenderer bodySpriteRenderer;
        public SpriteRenderer overcoatSpriteRenderer;

        // Hair origins
        public Transform hairFront1TargetOrigin;
        public Transform hairFront2TargetOrigin;
        public Transform hairBack1TargetOrigin;
        public Transform hairBack2TargetOrigin;

        #endregion

        #region Variables

        public bool faceRight = false;

        #endregion

        private void Update()
        {
            // Get face right from player controller
            if (playerController.velocity.x > 0) faceRight = true;
            else if (playerController.velocity.x < 0) faceRight = false;

            // Make player face the correct direction
            headSpriteRenderer.flipX = faceRight;
            bodySpriteRenderer.flipX = faceRight;
            overcoatSpriteRenderer.flipX = faceRight;

            // Handle hair
            Vector3 hairFront1TargetOriginLocalPosition = playerAnimationData.hairFront1TargetOriginLocalPosition;
            Vector3 hairFront2TargetOriginLocalPosition = playerAnimationData.hairFront2TargetOriginLocalPosition;
            Vector3 hairBack1TargetOriginLocalPosition = playerAnimationData.hairBack1TargetOriginLocalPosition;
            Vector3 hairBack2TargetOriginLocalPosition = playerAnimationData.hairBack2TargetOriginLocalPosition;

            if (faceRight)
            {
                hairFront1TargetOriginLocalPosition.x = -playerAnimationData.hairFront1TargetOriginLocalPosition.x;
                hairFront2TargetOriginLocalPosition.x = -playerAnimationData.hairFront2TargetOriginLocalPosition.x;
                hairBack1TargetOriginLocalPosition.x = -playerAnimationData.hairBack1TargetOriginLocalPosition.x;
                hairBack2TargetOriginLocalPosition.x = -playerAnimationData.hairBack2TargetOriginLocalPosition.x;
            }
            else
            {
                hairFront1TargetOriginLocalPosition.x = playerAnimationData.hairFront1TargetOriginLocalPosition.x;
                hairFront2TargetOriginLocalPosition.x = playerAnimationData.hairFront2TargetOriginLocalPosition.x;
                hairBack1TargetOriginLocalPosition.x = playerAnimationData.hairBack1TargetOriginLocalPosition.x;
                hairBack2TargetOriginLocalPosition.x = playerAnimationData.hairBack2TargetOriginLocalPosition.x;
            }

            hairFront1TargetOrigin.localPosition = hairFront1TargetOriginLocalPosition;
            hairFront2TargetOrigin.localPosition = hairFront2TargetOriginLocalPosition;
            hairBack1TargetOrigin.localPosition = hairBack1TargetOriginLocalPosition;
            hairBack2TargetOrigin.localPosition = hairBack2TargetOriginLocalPosition;
        }
    }
}
