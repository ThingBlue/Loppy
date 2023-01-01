// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

namespace Loppy
{
    public enum PlayerForce
    {
        // Added directly to the players movement speed, to be controlled by the standard deceleration
        BURST = 0,

        // An external velocity that decays over time, applied additively to the rigidbody's velocity
        DECAY
    }

    public enum PlayerState
    {
        NONE = 0,
        IDLE,
        RUN,
        AIRBORNE,
        WALL
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        #region Inspector members

        public SpriteRenderer sprite;

        public Rigidbody2D rigidbody;
        public CapsuleCollider2D standingCollider;
        public PlayerPhysicsStats playerPhysicsStats;

        private CapsuleCollider2D activeCollider;

        #endregion

        #region Input variables

        private bool hasControl = true;

        private Vector2 playerInput;
        private Vector2 lastPlayerInput;
        private Vector2 playerInputDown; // Only true on the first frame of key down
        private bool jumpKey = false;
        private bool jumpKeyUp = true;

        public int maxAirJumps = 1;
        private int airJumpsRemaining;

        private bool jumpToConsume;
        private bool bufferedJumpUsable;
        private bool endedJumpEarly;
        private bool coyoteUsable;
        private bool wallJumpCoyoteUsable;

        private int jumpBufferFramesCounter = 0;
        private int coyoteFramesCounter = 0;
        private int wallJumpCoyoteFramesCounter = 0;

        #endregion

        #region Physics variables

        public Vector2 velocity;
        private Vector2 externalVelocity;
        private float currentWallJumpMoveMultiplier = 1f;

        #endregion

        #region Collision variables

        public bool onGround;
        private Vector2 groundNormal;
        private Vector2 ceilingNormal;

        public bool onWall;
        private int wallDirection;
        private Vector2 wallNormal;

        public bool onLedge;
        private bool climbingLedge;
        private Vector2 ledgeCornerPosition;

        private bool detectTriggers;

        #endregion

        #region State machine

        public PlayerState playerState;
        public int facingDirection = 1;

        #endregion

        #region Event actions

        public event Action<bool, float> onGroundChanged;
        public event Action<bool, float> onWallChanged;
        public event Action<bool> ledgeClimbChanged;
        public event Action jumped;
        public event Action wallJumped;
        public event Action airJumped;

        #endregion

        public void applyVelocity(Vector2 vel, PlayerForce forceType)
        {
            if (forceType == PlayerForce.BURST) velocity += vel;
            else externalVelocity += vel;
        }

        public void setVelocity(Vector2 vel, PlayerForce velocityType)
        {
            if (velocityType == PlayerForce.BURST) velocity = vel;
            else externalVelocity = vel;
        }

        public void toggleControl(bool control) { hasControl = control; }

        private void Awake()
        {
            // Initialize members
            rigidbody = GetComponent<Rigidbody2D>();
            playerInput = Vector2.zero;
            detectTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesStartInColliders = false;
            activeCollider = standingCollider;
            playerState = PlayerState.IDLE;
        }

        private void Update()
        {
            handleInput();
        }

        private void FixedUpdate()
        {
            // Increment frame counters
            jumpBufferFramesCounter++;
            coyoteFramesCounter++;
            wallJumpCoyoteFramesCounter++;

            handlePhysics();

            handleCollisions();

            handleJump();

            move();

            handleStateMachine();

            // Reset input
            playerInputDown = Vector2.zero;
        }

        #region Input

        private void handleInput()
        {
            playerInput = Vector2.zero;

            // Horizontal input
            if (InputManager.instance.getKey("left")) playerInput.x -= 1;
            if (InputManager.instance.getKey("right")) playerInput.x += 1;
            if (InputManager.instance.getKeyDown("left")) playerInputDown.x -= 1;
            if (InputManager.instance.getKeyDown("right")) playerInputDown.x += 1;
            // Set last horizontal input
            if (playerInput.x != 0) lastPlayerInput.x = playerInput.x;

            // Vertical input
            if (InputManager.instance.getKey("up")) playerInput.y += 1;
            if (InputManager.instance.getKey("down")) playerInput.y -= 1;
            if (InputManager.instance.getKeyDown("up")) playerInputDown.y += 1;
            if (InputManager.instance.getKeyDown("down")) playerInputDown.y -= 1;
            // Set last vertical input
            if (playerInput.y != 0) lastPlayerInput.y = playerInput.y;

            // Jump
            jumpKey = InputManager.instance.getKey("jump");
            if (InputManager.instance.getKeyDown("jump"))
            {
                jumpToConsume = true;
                jumpBufferFramesCounter = 0;
            }

            // Set player's facing direction to last horizontal input
            facingDirection = lastPlayerInput.x >= 0 ? 1 : -1;
        }

        #endregion

        #region Physics

        private void handlePhysics()
        {
            #region Vertical physics

            // Wall
            if (onWall)
            {
                // Climb wall
                if (playerInput.y > 0) velocity.y = playerPhysicsStats.wallClimbSpeed;
                // Fast fall on wall
                else if (playerInput.y < 0) velocity.y = -playerPhysicsStats.fastWallFallSpeed;
                // Decelerate rapidly when grabbing ledge
                else if (onLedge) velocity.y = Mathf.MoveTowards(velocity.y, 0, playerPhysicsStats.ledgeGrabDeceleration * Time.fixedDeltaTime);
                // Slow fall on wall
                else if (velocity.y < -playerPhysicsStats.maxWallFallSpeed) velocity.y = -playerPhysicsStats.maxWallFallSpeed;
                else velocity.y = Mathf.MoveTowards(Mathf.Min(velocity.y, 0), -playerPhysicsStats.maxWallFallSpeed, playerPhysicsStats.wallFallAcceleration * Time.fixedDeltaTime);
                //else velocity.y = 0;
            }
            // Airborne
            else if (!onGround)
            {
                float airborneAcceleration = playerPhysicsStats.fallAcceleration;

                // Check if player ended jump early
                if (endedJumpEarly && velocity.y > 0) airborneAcceleration *= playerPhysicsStats.jumpEndEarlyGravityModifier;

                // Accelerate towards maxFallSpeed using airborneAcceleration
                velocity.y = Mathf.MoveTowards(velocity.y, -playerPhysicsStats.maxFallSpeed, airborneAcceleration * Time.fixedDeltaTime);
            }

            #endregion

            #region Horizontal physics

            currentWallJumpMoveMultiplier = Mathf.MoveTowards(currentWallJumpMoveMultiplier, 1f, 1f / playerPhysicsStats.wallJumpInputLossFrames);

            // Give player a burst of speed upon key down
            if (playerInputDown.x > 0) velocity.x = playerPhysicsStats.burstVelocity;
            if (playerInputDown.x < 0) velocity.x = -playerPhysicsStats.burstVelocity;

            // Deceleration
            if (playerInput.x == 0)
            {
                var deceleration = onGround ? playerPhysicsStats.groundDeceleration : playerPhysicsStats.airDeceleration;

                // Decelerate towards 0
                //velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
                velocity.x = 0;
            }
            // Regular Horizontal Movement
            else
            {
                // Reset x velocity when on wall
                if (onWall) velocity.x = 0;

                // Accelerate towards max speed
                velocity.x = Mathf.MoveTowards(velocity.x, playerInput.x * playerPhysicsStats.maxRunSpeed, currentWallJumpMoveMultiplier * playerPhysicsStats.acceleration * Time.fixedDeltaTime);
            }

            #endregion
        }

        #endregion

        #region Collisions

        private void handleCollisions()
        {
            Physics2D.queriesHitTriggers = false;

            RaycastHit2D[] groundHits = new RaycastHit2D[2];
            RaycastHit2D[] ceilingHits = new RaycastHit2D[2];
            RaycastHit2D[] wallHits = new RaycastHit2D[2];
            int groundHitCount;
            int ceilingHitCount;
            int wallHitCount;
            bool ceilingCollision = false;

            #region Vertical collisions

            // Raycast to check for vertical collisions
            groundHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.down, groundHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);
            ceilingHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.up, ceilingHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);

            groundNormal = getRaycastNormal(Vector2.down);
            ceilingNormal = getRaycastNormal(Vector2.up);

            float groundAngle = Vector2.Angle(groundNormal, Vector2.up);

            // Enter ground
            if (!onGround && groundHitCount > 0 && groundAngle <= playerPhysicsStats.maxWalkAngle)
            //if (!onGround && groundHitCount > 0 && Math.Abs(groundNormal.y) > Math.Abs(groundNormal.x))
            {
            onGround = true;
            resetJump();

            // Invoke onGroundChanged event action
            onGroundChanged?.Invoke(true, Mathf.Abs(velocity.y));
            }
            // Leave ground
            else if (onGround && (groundHitCount == 0 || groundAngle > playerPhysicsStats.maxWalkAngle))
            //else if (onGround && (groundHitCount == 0 || Math.Abs(groundNormal.y) < Math.Abs(groundNormal.x)))
            {
                onGround = false;

                // Start coyote frames counter
                coyoteFramesCounter = 0;

                // Invoke onGroundChanged event action
                onGroundChanged?.Invoke(false, 0);
            }
            // On ground
            else if (onGround && groundHitCount > 0 && groundAngle <= playerPhysicsStats.maxWalkAngle)
            {
                // Give the player a constant downwards velocity so that they stick to the ground on slopes
                velocity.y = playerPhysicsStats.groundingForce;

                // Handle slopes
                if (groundNormal != Vector2.zero) // Make sure ground normal exists
                {
                    if (!Mathf.Approximately(Math.Abs(groundNormal.y), 1f))
                    {
                        // Change y velocity to match ground slope
                        float groundSlope = -groundNormal.x / groundNormal.y;
                        velocity.y = velocity.x * groundSlope;
                        if (velocity.x != 0) velocity.y += playerPhysicsStats.groundingForce;
                    }
                }
            }

            // Ceiling collision detected
            if (ceilingHitCount > 0 && Math.Abs(ceilingNormal.y) > Math.Abs(ceilingNormal.x))
            {
                // Prevent sticking to ceiling if we did an air jump after receiving external velocity w/ PlayerForce.Decay
                externalVelocity.y = Mathf.Min(0f, externalVelocity.y);
                velocity.y = Mathf.Min(0, velocity.y);

                // Set ceiling collision flag to true
                ceilingCollision = true;
            }

            #endregion

            #region Horizontal collisions
               
            // Raycast to check for horizontal collisions
            wallHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, new(lastPlayerInput.x, 0), wallHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);
            wallNormal = getRaycastNormal(new(lastPlayerInput.x, 0));
            float wallAngle = Mathf.Min(Vector2.Angle(wallNormal, Vector2.left), Vector2.Angle(wallNormal, Vector2.right));

            // Enter wall
            // Make sure we're not colliding with ground or ceiling
            if (!onWall && wallHitCount > 0 && wallAngle <= playerPhysicsStats.maxClimbAngle && !onGround && !ceilingCollision && velocity.y < 0)
            {
                onWall = true;
                wallDirection = (int)lastPlayerInput.x;
                velocity = Vector2.zero;
                resetJump();

                // Invoke onWallChanged event action
                onWallChanged?.Invoke(true, Mathf.Abs(velocity.x));
            }
            // Leave wall
            else if (onWall && (wallHitCount == 0 || wallAngle > playerPhysicsStats.maxClimbAngle || onGround))
            {
                onWall = false;

                // Start wall jump coyote frames counter
                wallJumpCoyoteFramesCounter = 0;

                // Invoke onWallChanged event action
                onWallChanged?.Invoke(false, 0);
            }
            // On wall
            else if (onWall && wallHitCount > 0 && wallAngle <= playerPhysicsStats.maxClimbAngle && !onGround)
            {
                // Give the player a constant velocity so that they stick to sloped walls
                //velocity.x = playerPhysicsStats.groundingForce * -wallDirection;

                // Handle slopes
                if (wallNormal != Vector2.zero) // Make sure wall normal exists
                {
                    if (!Mathf.Approximately(Math.Abs(wallNormal.x), 1f))
                    {
                        // Change x velocity to match wall slope
                        float inverseWallSlope = -wallNormal.y / wallNormal.x;
                        velocity.x = velocity.y * inverseWallSlope;
                        //if (velocity.y != 0) velocity.x += playerPhysicsStats.groundingForce * -wallDirection;
                    }
                }

                // Handle ledges
                onLedge = getLedgeCorner(out ledgeCornerPosition);
                if (onLedge)
                {
                    // Nudge towards better grabbing position
                    if ( /*Input.y != 0 &&*/ playerInput.x == 0 && hasControl)
                    {
                        Vector2 pos = rigidbody.position;
                        Vector2 targetPos = ledgeCornerPosition - Vector2.Scale(playerPhysicsStats.ledgeGrabPoint, new(wallDirection, 1f));
                        rigidbody.position = Vector2.MoveTowards(pos, targetPos, playerPhysicsStats.ledgeGrabDeceleration * Time.fixedDeltaTime);
                    }

                    // Detect ledge climb input
                    if (playerInput.y > 0)
                    {
                        Vector2 finalPos = ledgeCornerPosition + Vector2.Scale(playerPhysicsStats.standUpOffset, new(wallDirection, 1f));
                        //if (!canClimbLedge(finalPos)) return; // TODO: Split this into 2 different ledge climb animations - standing & crawling

                        StartCoroutine(climbLedge());
                    }
                }
            }

            #endregion
        }

        private Vector2 getRaycastNormal(Vector2 castDirection)
        {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.CapsuleCast(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, castDirection, playerPhysicsStats.raycastDistance * 2, ~playerPhysicsStats.playerLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            if (!hit.collider) return Vector2.zero;

            return hit.normal; // Defaults to Vector2.zero if nothing was hit
        }

        private bool getLedgeCorner(out Vector2 cornerPos)
        {
            cornerPos = Vector2.zero;
            var grabHeight = rigidbody.position + playerPhysicsStats.ledgeGrabPoint.y * Vector2.up;

            var hit1 = Physics2D.Raycast(grabHeight + playerPhysicsStats.ledgeRaycastSpacing * Vector2.down, wallDirection * Vector2.right, playerPhysicsStats.ledgeRaycastDistance, ~playerPhysicsStats.playerLayer);
            if (!hit1.collider) return false; // Should hit below the ledge. Mainly used to determine xPos accurately

            var hit2 = Physics2D.Raycast(grabHeight + playerPhysicsStats.ledgeRaycastSpacing * Vector2.up, wallDirection * Vector2.right, playerPhysicsStats.ledgeRaycastDistance, ~playerPhysicsStats.playerLayer);
            if (hit2.collider) return false; // We only are within ledge-grab range when the first hits and second doesn't

            var hit3 = Physics2D.Raycast(grabHeight + new Vector2(wallDirection * 0.5f, playerPhysicsStats.ledgeRaycastSpacing), Vector2.down, playerPhysicsStats.ledgeRaycastDistance, ~playerPhysicsStats.playerLayer);
            if (!hit3.collider) return false; // Gets our yPos of the corner

            cornerPos = new(hit1.point.x, hit3.point.y);
            return true;
        }

        private IEnumerator climbLedge()
        {
            // Invoke ledgeClimbChanged event action at the start
            ledgeClimbChanged?.Invoke(true);

            // Take away player control
            hasControl = false;
            rigidbody.velocity = Vector2.zero;

            // Reset ledge flags
            climbingLedge = true;
            onLedge = false;

            // Set startup position
            transform.position = ledgeCornerPosition - Vector2.Scale(playerPhysicsStats.ledgeGrabPoint, new(wallDirection, 1f));

            // Wait for ledge climb animation to finish
            float animationEndTime = Time.time + playerPhysicsStats.ledgeClimbDuration;
            while (Time.time < animationEndTime) yield return new WaitForFixedUpdate();

            // Set final position
            transform.position = ledgeCornerPosition + Vector2.Scale(playerPhysicsStats.standUpOffset, new(wallDirection, 1f));
            
            // Reset flags
            climbingLedge = false;
            onWall = false;

            // Return control to player
            hasControl = true;
            velocity = Vector2.zero;

            // Invoke ledgeClimbChanged event action at the end
            ledgeClimbChanged?.Invoke(false);
        }

        #endregion

        #region Jumping

        private void handleJump()
        {
            bool hasBufferedJump = bufferedJumpUsable && jumpBufferFramesCounter < playerPhysicsStats.jumpBufferFrames;
            bool canUseCoyote = coyoteUsable && !onGround && coyoteFramesCounter < playerPhysicsStats.coyoteFrames;
            bool canUseWallJumpCoyote = wallJumpCoyoteUsable && !onGround && wallJumpCoyoteFramesCounter < playerPhysicsStats.wallJumpCoyoteFrames;
            bool canAirJump = airJumpsRemaining > 0;

            if (!endedJumpEarly && !onGround && !jumpKey && rigidbody.velocity.y > 0) endedJumpEarly = true; // Early end detection

            // Check for jump input
            if (!jumpToConsume && !hasBufferedJump) return;

            if (onWall || canUseWallJumpCoyote) wallJump();
            else if (onGround || canUseCoyote) normalJump();
            else if (canAirJump) airJump();

            jumpToConsume = false; // Always consume the flag
        }

        private void normalJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            bufferedJumpUsable = false;
            coyoteUsable = false;

            // Apply jump velocity
            velocity.y = playerPhysicsStats.jumpStrength;

            // Invoke jumped event action
            jumped?.Invoke();
        }

        protected void wallJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            bufferedJumpUsable = false;
            wallJumpCoyoteUsable = false;

            // Apply jump velocity
            velocity = Vector2.Scale(playerPhysicsStats.wallJumpStrength, new(-wallDirection, 1));
            currentWallJumpMoveMultiplier = 0;

            // Reset onWall status
            onWall = false;

            // Invoke wall jumped event action
            wallJumped?.Invoke();
        }

        private void airJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            airJumpsRemaining--;

            // Apply jump velocity
            velocity.y = playerPhysicsStats.jumpStrength;
            externalVelocity.y = 0; // Air jump cancels out vertical external forces

            // Invoke air jumped event action
            airJumped?.Invoke();
        }

        private void resetJump()
        {
            // Reset jump flags
            bufferedJumpUsable = true;
            coyoteUsable = true;
            wallJumpCoyoteUsable = true;
            endedJumpEarly = false;

            // Reset number of air jumps
            airJumpsRemaining = maxAirJumps;
        }

        #endregion

        private void move()
        {
            // Check if player has control
            if (!hasControl) return;

            // Apply velocity to rigidbody
            rigidbody.velocity = velocity + externalVelocity;

            // Decay external velocity
            externalVelocity = Vector2.MoveTowards(externalVelocity, Vector2.zero, playerPhysicsStats.externalVelocityDecay * Time.fixedDeltaTime);
        }

        #region State machine

        private void handleStateMachine()
        {
            switch (playerState)
            {
                case PlayerState.NONE: break;
                case PlayerState.IDLE:
                    idleState();
                    break;
                case PlayerState.RUN:
                    runState();
                    break;
                case PlayerState.AIRBORNE:
                    airborneState();
                    break;
                case PlayerState.WALL:
                    wallState();
                    break;
                default: break;
            }
        }

        private void idleState()
        {
            sprite.color = Color.blue;

            // Switch states
            if      (!onGround)          playerState = PlayerState.AIRBORNE;
            else if (playerInput.x != 0) playerState = PlayerState.RUN;
        }

        private void runState()
        {
            sprite.color = Color.red;

            // Switch states
            if      (!onGround)       playerState = PlayerState.AIRBORNE;
            else if (velocity.x == 0) playerState = PlayerState.IDLE;
        }

        private void airborneState()
        {
            sprite.color = Color.yellow;

            // Switch states
            if      (onGround && velocity.x == 0) playerState = PlayerState.IDLE;
            else if (onGround)                    playerState = PlayerState.RUN;
            else if (onWall)                      playerState = PlayerState.WALL;
        }

        private void wallState()
        {
            sprite.color = Color.cyan;

            // Switch states
            if (!onWall && !onGround)        playerState = PlayerState.AIRBORNE;
            else if (onGround && velocity.x == 0) playerState = PlayerState.IDLE;
            else if (onGround)                    playerState = PlayerState.RUN;
        }

        #endregion
    }
}