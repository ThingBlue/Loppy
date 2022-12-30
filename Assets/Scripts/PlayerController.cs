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

        private bool jumpToConsume;
        private bool bufferedJumpUsable;
        private bool endedJumpEarly;
        private bool coyoteUsable;
        private int airJumpsRemaining;

        private int jumpBufferFramesCounter = 0;
        private int coyoteFramesCounter = 0;

        #endregion

        #region Physics variables

        private Vector2 velocity;
        private Vector2 externalVelocity;

        #endregion

        #region Collision variables

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[2];
        private readonly RaycastHit2D[] ceilingHits = new RaycastHit2D[2];
        private int groundHitCount;
        private int ceilingHitCount;

        private bool grounded;

        private Vector2 groundNormal;

        private bool detectTriggers;

        #endregion

        #region State machine

        PlayerState playerState;

        #endregion

        #region Event actions

        public event Action<bool, float> groundedChanged;
        public event Action<bool> jumped;
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

            // Horizontal movement
            if (InputManager.instance.getKey("left")) playerInput.x -= 1;
            if (InputManager.instance.getKey("right")) playerInput.x += 1;

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
            #region Horizontal

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
                velocity.x = Mathf.MoveTowards(velocity.x, playerInput.x * playerPhysicsStats.maxRunSpeed, /*currentWallJumpMoveMultiplier * */playerPhysicsStats.acceleration * Time.fixedDeltaTime);
            }

            #endregion

            #region Vertical

            float airborneAcceleration = playerPhysicsStats.fallAcceleration;

            // Check if player ended jump early
            if (endedJumpEarly && velocity.y > 0) airborneAcceleration *= playerPhysicsStats.jumpEndEarlyGravityModifier;

            // Accelerate towards maxFallSpeed using airborneAcceleration
            velocity.y = Mathf.MoveTowards(velocity.y, -playerPhysicsStats.maxFallSpeed, airborneAcceleration * Time.fixedDeltaTime);

            #endregion
        }

        #endregion

        #region Collisions

        private void handleCollisions()
        {
            Physics2D.queriesHitTriggers = false;

            // Ground and Ceiling
            groundHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.down, groundHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);
            ceilingHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.up, ceilingHits, playerPhysicsStats.raycastDistance, ~playerPhysicsStats.playerLayer);

            //groundNormal = getGroundNormal();

            // Ground collision detected
            if (!grounded && groundHitCount > 0)
            // && Math.Abs(groundNormal.x) <= Math.Abs(groundNormal.y)
            {
                grounded = true;
                resetJump();

                // Invoke groundedChanged event action
                groundedChanged?.Invoke(true, Mathf.Abs(velocity.y));
            }
            // On ground
            else if (grounded && groundHitCount > 0)
            {
                // Give the player a constant downwards velocity so that they stick to the ground on slopes
                velocity.y = playerPhysicsStats.groundingForce;

                // Handle slopes
                groundNormal = getGroundNormal();

                if (groundNormal != Vector2.zero)
                {
                    if (!Mathf.Approximately(groundNormal.y, 1f))
                    {
                        // Change y velocity to match ground slope
                        float groundSlope = -groundNormal.x / groundNormal.y;
                        velocity.y = velocity.x * groundSlope;
                        if (velocity.x != 0) velocity.y += playerPhysicsStats.groundingForce;
                    }
                }
            }
            // Leave ground
            else if (grounded && groundHitCount == 0)
            {
                grounded = false;

                // Start coyote frames counter
                coyoteFramesCounter = 0;

                // Invoke groundedChanged event action
                groundedChanged?.Invoke(false, 0);
            }

            // Ceiling collision detected
            if (ceilingHitCount > 0)
            {
                // Prevent sticking to ceiling if we did an air jump after receiving external velocity w/ PlayerForce.Decay
                externalVelocity.y = Mathf.Min(0f, externalVelocity.y);
                velocity.y = Mathf.Min(0, velocity.y);
            }
        }

        private Vector2 getGroundNormal()
        {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(rigidbody.position, Vector2.down, playerPhysicsStats.raycastDistance * 2, ~playerPhysicsStats.playerLayer);
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
            bool canAirJump = airJumpsRemaining > 0;

            if (!endedJumpEarly && !grounded && !jumpKey && rigidbody.velocity.y > 0) endedJumpEarly = true; // Early end detection

            if (!jumpToConsume && !hasBufferedJump) return;

            if (grounded || canUseCoyote) normalJump();
            else if (jumpToConsume && canAirJump) airJump();

            jumpToConsume = false; // Always consume the flag
        }

        private void normalJump()
        {
            endedJumpEarly = false;
            bufferedJumpUsable = false;
            coyoteUsable = false;
            velocity.y = playerPhysicsStats.jumpStrength;
            jumped?.Invoke(false);
        }

        private void airJump()
        {
            endedJumpEarly = false;
            airJumpsRemaining--;
            velocity.y = playerPhysicsStats.jumpStrength;
            externalVelocity.y = 0; // Air jump cancels out vertical external forces
            airJumped?.Invoke();
        }

        private void resetJump()
        {
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