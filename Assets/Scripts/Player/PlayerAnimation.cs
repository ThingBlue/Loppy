using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Player
{
    public class PlayerHairData
    {
        public Vector3 hairFront1TargetOriginLocalPositionLeft;
        public Vector3 hairFront2TargetOriginLocalPositionLeft;
        public Vector3 hairBack1TargetOriginLocalPositionLeft;
        public Vector3 hairBack2TargetOriginLocalPositionLeft;
        public float hairFront1TargetOriginLocalRotationLeft;
        public float hairFront2TargetOriginLocalRotationLeft;
        public float hairBack1TargetOriginLocalRotationLeft;
        public float hairBack2TargetOriginLocalRotationLeft;

        public Vector3 hairFront1TargetOriginLocalPositionRight;
        public Vector3 hairFront2TargetOriginLocalPositionRight;
        public Vector3 hairBack1TargetOriginLocalPositionRight;
        public Vector3 hairBack2TargetOriginLocalPositionRight;
        public float hairFront1TargetOriginLocalRotationRight;
        public float hairFront2TargetOriginLocalRotationRight;
        public float hairBack1TargetOriginLocalRotationRight;
        public float hairBack2TargetOriginLocalRotationRight;
    }

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

        private void Start()
        {
            initializeHairData();
        }

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
            if (!faceRight)
            {
                // Set positions
                hairFront1TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairFront1TargetOriginLocalPositionLeft;
                hairFront2TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairFront2TargetOriginLocalPositionLeft;
                hairBack1TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairBack1TargetOriginLocalPositionLeft;
                hairBack2TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairBack2TargetOriginLocalPositionLeft;

                // Set rotations
                hairFront1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairFront1TargetOriginLocalRotationLeft - 90);
                hairFront2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairFront2TargetOriginLocalRotationLeft - 90);
                hairBack1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairBack1TargetOriginLocalRotationLeft - 90);
                hairBack2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairBack2TargetOriginLocalRotationLeft - 90);
            }
            else
            {
                // Set positions
                hairFront1TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairFront1TargetOriginLocalPositionRight;
                hairFront2TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairFront2TargetOriginLocalPositionRight;
                hairBack1TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairBack1TargetOriginLocalPositionRight;
                hairBack2TargetOrigin.localPosition = playerAnimationData.playerHairData[playerController.playerState].hairBack2TargetOriginLocalPositionRight;

                // Set rotations
                hairFront1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairFront1TargetOriginLocalRotationRight - 90);
                hairFront2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairFront2TargetOriginLocalRotationRight - 90);
                hairBack1TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairBack1TargetOriginLocalRotationRight - 90);
                hairBack2TargetOrigin.localRotation = Quaternion.Euler(0, 0, playerAnimationData.playerHairData[playerController.playerState].hairBack2TargetOriginLocalRotationRight - 90);
            }
        }

        private void initializeHairData()
        {
            // Initialize dictionary
            playerAnimationData.playerHairData = new Dictionary<PlayerState, PlayerHairData>();

            // Idle
            PlayerHairData newHairData = new PlayerHairData();

            newHairData.hairFront1TargetOriginLocalPositionLeft = new Vector3(0.05f, 0.35f, 0);
            newHairData.hairFront2TargetOriginLocalPositionLeft = new Vector3(-0.29f, 0.43f, 0);
            newHairData.hairBack1TargetOriginLocalPositionLeft = new Vector3(0.07f, 0.295f, 0);
            newHairData.hairBack2TargetOriginLocalPositionLeft = new Vector3(-0.1f, 0.28f, 0);
            newHairData.hairFront1TargetOriginLocalRotationLeft = 5;
            newHairData.hairFront2TargetOriginLocalRotationLeft = -5;
            newHairData.hairBack1TargetOriginLocalRotationLeft = 3;
            newHairData.hairBack2TargetOriginLocalRotationLeft = -3;

            newHairData.hairFront1TargetOriginLocalPositionRight = new Vector3(0.29f, 0.43f, 0);
            newHairData.hairFront2TargetOriginLocalPositionRight = new Vector3(-0.05f, 0.35f, 0);
            newHairData.hairBack1TargetOriginLocalPositionRight = new Vector3(0.1f, 0.28f, 0);
            newHairData.hairBack2TargetOriginLocalPositionRight = new Vector3(-0.07f, 0.295f, 0);
            newHairData.hairFront1TargetOriginLocalRotationRight = 5;
            newHairData.hairFront2TargetOriginLocalRotationRight = -5;
            newHairData.hairBack1TargetOriginLocalRotationRight = 3;
            newHairData.hairBack2TargetOriginLocalRotationRight = -3;

            playerAnimationData.playerHairData[PlayerState.IDLE] = newHairData;
        }
    }
}
