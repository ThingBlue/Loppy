using Loppy.GameCore;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Player
{
    [Serializable]
    public class PlayerHairData
    {
        public PlayerState playerState;

        public Vector3 front1LocalPositionLeft;
        public Vector3 front2LocalPositionLeft;
        public Vector3 back1LocalPositionLeft;
        public Vector3 back2LocalPositionLeft;
        public float front1LocalRotationLeft;
        public float front2LocalRotationLeft;
        public float back1LocalRotationLeft;
        public float back2LocalRotationLeft;

        public Vector3 front1LocalPositionRight;
        public Vector3 front2LocalPositionRight;
        public Vector3 back1LocalPositionRight;
        public Vector3 back2LocalPositionRight;
        public float front1LocalRotationRight;
        public float front2LocalRotationRight;
        public float back1LocalRotationRight;
        public float back2LocalRotationRight;
    }

    public class PlayerAnimationController : MonoBehaviour
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

        public bool faceRight = false;
        private float targetRotation;
        private float rotationVelocity = 0;

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
            handleHair();
        }

        private void handleHair()
        {
            for (int i = 0; i < playerAnimationData.playerHairData.Count; i++)
            {
                if (playerController.playerState == playerAnimationData.playerHairData[i].playerState)
                {
                    if (!faceRight)
                    {
                        // Set positions
                        hairFront1TargetOrigin.localPosition = playerAnimationData.playerHairData[i].front1LocalPositionLeft;
                        hairFront2TargetOrigin.localPosition = playerAnimationData.playerHairData[i].front2LocalPositionLeft;
                        hairBack1TargetOrigin.localPosition = playerAnimationData.playerHairData[i].back1LocalPositionLeft;
                        hairBack2TargetOrigin.localPosition = playerAnimationData.playerHairData[i].back2LocalPositionLeft;

                        // Set rotations
                        hairFront1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].front1LocalRotationLeft - 90);
                        hairFront2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].front2LocalRotationLeft - 90);
                        hairBack1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].back1LocalRotationLeft - 90);
                        hairBack2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].back2LocalRotationLeft - 90);
                    }
                    else
                    {
                        // Set positions
                        hairFront1TargetOrigin.localPosition = playerAnimationData.playerHairData[i].front1LocalPositionRight;
                        hairFront2TargetOrigin.localPosition = playerAnimationData.playerHairData[i].front2LocalPositionRight;
                        hairBack1TargetOrigin.localPosition = playerAnimationData.playerHairData[i].back1LocalPositionRight;
                        hairBack2TargetOrigin.localPosition = playerAnimationData.playerHairData[i].back2LocalPositionRight;

                        // Set rotations
                        hairFront1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].front1LocalRotationRight - 90);
                        hairFront2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].front2LocalRotationRight - 90);
                        hairBack1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].back1LocalRotationRight - 90);
                        hairBack2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[i].back2LocalRotationRight - 90);
                    }
                }
            }
        }
    }
}
