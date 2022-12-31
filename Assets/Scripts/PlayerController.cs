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
        JUMP
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        #region Inspector members

        public Rigidbody2D rigidbody;
        public CapsuleCollider2D standingCollider;
        public PlayerPhysicsStats playerPhysicsStats;

        private CapsuleCollider2D activeCollider;

        #endregion

        #region Input variables

        private Vector2 playerInput;
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

        private Vector2 velocity;
        private Vector2 externalVelocity;
        private float currentWallJumpMoveMultiplier = 1f;

        #endregion

        #region Collision variables

        private bool grounded;
        private Vector2 groundNormal;
        private Vector2 ceilingNormal;

        private bool onWall;
        private int wallDirection;
        private Vector2 wallNormal;

        private bool detectTriggers;

        #endregion

        #region State machine

        PlayerState playerState;

        #endregion

        #region Event actions

        public event Action<bool, float> groundedChanged;
        public event Action<bool, float> onWallChanged;
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
        }

        #region Input

        private void handleInput()
        {
            playerInput = Vector2.zero;

            // Horizontal input
            if (InputManager.instance.getKey("left")) playerInput.x -= 1;
            if (InputManager.instance.getKey("right")) playerInput.x += 1;

            // Vertical input
            if (InputManager.instance.getKey("up")) playerInput.y += 1;
            if (InputManager.instance.getKey("down")) playerInput.y -= 1;

            // Jump
            jumpKey = InputManager.instance.getKey("jump");
            if (InputManager.instance.getKeyDown("jump"))
            {
                jumpToConsume = true;
                jumpBufferFramesCounter = 0;
            }
        }

        #endregion

        #region Physics

        private void handlePhysics()
        {
            #region Vertical physics

            // Wall
            if (onWall)
            {
                if (playerInput.y > 0) velocity.y = playerPhysicsStats.wallClimbSpeed;
                else if (playerInput.y < 0) velocity.y = -playerPhysicsStats.maxWallFallSpeed;
                //else if (GrabbingLedge) velocity.y = Mathf.MoveTowards(velocity.y, 0, playerPhysicsStats.ledgeGrabDeceleration * Time.fixedDeltaTime);
                else velocity.y = Mathf.MoveTowards(Mathf.Min(velocity.y, 0), -playerPhysicsStats.maxWallFallSpeed, playerPhysicsStats.wallFallAcceleration * Time.fixedDeltaTime);
                //else velocity.y = 0;
            }
            // Airborne
            else if (!grounded)
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

            // Deceleration
            if (playerInput.x == 0)
            {
                var deceleration = grounded ? playerPhysicsStats.groundDeceleration : playerPhysicsStats.airDeceleration;

                // Decelerate towards 0
                velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            // Regular Horizontal Movement
            else
            {
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

            #region Vertical collisions

            // Raycast to check for vertical collisions
            groundHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.down, groundHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);
            ceilingHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.up, ceilingHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);

            groundNormal = getRaycastNormal(Vector2.down);
            ceilingNormal = getRaycastNormal(Vector2.up);

            // Enter ground
            if (!grounded && groundHitCount > 0 && Math.Abs(groundNormal.y) > Math.Abs(groundNormal.x))
            // && Math.Abs(groundNormal.x) <= Math.Abs(groundNormal.y)
            {
                grounded = true;
                resetJump();

                // Invoke groundedChanged event action
                groundedChanged?.Invoke(true, Mathf.Abs(velocity.y));
            }
            // Leave ground
            else if (grounded && (groundHitCount == 0 || Math.Abs(groundNormal.y) < Math.Abs(groundNormal.x)))
            {
                grounded = false;

                // Start coyote frames counter
                coyoteFramesCounter = 0;

                // Invoke groundedChanged event action
                groundedChanged?.Invoke(false, 0);
            }
            // On ground
            else if (grounded && groundHitCount > 0)
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
            }

            #endregion

            #region Horizontal collisions
               
            // Raycast to check for horizontal collisions
            wallHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, new(playerInput.x, 0), wallHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);
            wallNormal = getRaycastNormal(new(playerInput.x, 0));

            // Enter wall
            if (!onWall && wallHitCount > 0 && !grounded && ceilingHitCount == 0 && velocity.y < 0)
            {
                onWall = true;
                wallDirection = (int)playerInput.x;
                velocity = Vector2.zero;
                resetJump();

                // Invoke onWallChanged event action
                onWallChanged?.Invoke(true, Mathf.Abs(velocity.x));
            }
            // On wall
            /*
            else if (onWall && wallHitCount > 0 && !grounded)
            {
                // Handle slopes
                if (wallNormal != Vector2.zero) // Make sure wall normal exists
                {
                    if (!Mathf.Approximately(Math.Abs(wallNormal.x), 1f))
                    {
                        // Change x velocity to match wall slope
                        float wallSlope = -wallNormal.x / wallNormal.y;
                        velocity.x = velocity.y * wallSlope;
                    }
                }
            }
            */
            // Leave wall
            else if (onWall && (wallHitCount == 0 || grounded))
            {
                onWall = false;
                wallDirection = 0;

                // Start wall jump coyote frames counter
                wallJumpCoyoteFramesCounter = 0;

                // Invoke onWallChanged event action
                onWallChanged?.Invoke(false, 0);
            }

            #endregion
        }

        private Vector2 getRaycastNormal(Vector2 castDirection)
        {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(rigidbody.position, castDirection, playerPhysicsStats.raycastDistance * 2, ~playerPhysicsStats.playerLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            if (!hit.collider) return Vector2.zero;

            return hit.normal; // Defaults to Vector2.zero if nothing was hit
        }

        #endregion

        #region Jumping

        private void handleJump()
        {
            bool hasBufferedJump = bufferedJumpUsable && jumpBufferFramesCounter < playerPhysicsStats.jumpBufferFrames;
            bool canUseCoyote = coyoteUsable && !grounded && coyoteFramesCounter < playerPhysicsStats.coyoteFrames;
            bool canWallJump = onWall || (wallJumpCoyoteUsable && wallJumpCoyoteFramesCounter < playerPhysicsStats.wallJumpCoyoteFrames);
            bool canAirJump = airJumpsRemaining > 0;

            if (!endedJumpEarly && !grounded && !jumpKey && rigidbody.velocity.y > 0) endedJumpEarly = true; // Early end detection

            // Check for jump input
            if (!jumpToConsume && !hasBufferedJump) return;

            if (canWallJump) wallJump();
            else if (grounded || canUseCoyote) normalJump();
            else if (jumpToConsume && canAirJump) airJump();

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
            wallDirection = 0;

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
            endedJumpEarly = false;

            // Reset number of air jumps
            airJumpsRemaining = maxAirJumps;
        }

        #endregion

        private void move()
        {
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
                case PlayerState.JUMP:
                    jumpState();
                    break;
                default: break;
            }
        }

        private void idleState()
        {
            // Switch states
            if (!grounded)
            {
                playerState = PlayerState.JUMP;
                return;
            }
            if (playerInput.x != 0)
            {
                playerState = PlayerState.RUN;
                return;
            }
        }

        private void runState()
        {
            // Switch states
            if (!grounded)
            {
                playerState = PlayerState.JUMP;
                return;
            }
            if (velocity.x == 0)
            {
                playerState = PlayerState.IDLE;
                return;
            }
        }

        private void jumpState()
        {
            // Switch states
            if (grounded && velocity.x == 0)
            {
                playerState = PlayerState.IDLE;
                return;
            }
            if (grounded)
            {
                playerState = PlayerState.RUN;
                return;
            }
        }

        #endregion
    }
}