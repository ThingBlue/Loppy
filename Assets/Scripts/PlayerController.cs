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
        ON_WALL,
        ON_LEDGE,
        CLIMB_LEDGE,
        DASH,
        GLIDE
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

        private Vector2 playerInput = Vector2.zero;
        private Vector2 lastPlayerInput = Vector2.zero;
        private Vector2 playerInputDown = Vector2.zero; // Only true on the first frame of key down

        private bool jumpKey = false;
        private bool glideKey = false;

        #endregion

        #region Physics variables

        public Vector2 velocity = Vector2.zero;
        private Vector2 externalVelocity = Vector2.zero;

        // Jump
        public int maxAirJumps = 1;
        private int airJumpsRemaining = 0;

        private bool jumpToConsume = false;
        private bool jumpBufferUsable = false;
        private bool endedJumpEarly = false;
        private bool coyoteUsable = false;
        private bool wallJumpCoyoteUsable = false;

        private float jumpBufferTimer = 0;
        private float coyoteTimer = 0;
        private float wallJumpCoyoteTimer = 0;

        private float wallJumpControlLossMultiplier = 1;
        private float wallJumpControlLossTimer = 0;

        // Dash
        private bool dashing = false;
        private bool dashToConsume = false;
        private bool canDash = false;
        private bool dashBufferUsable = false;
        private bool dashCoyoteUsable = false;

        private Vector2 dashVelocity = Vector2.zero;

        private float dashTimer = 0;
        private float dashCooldownTimer = 0;
        private float dashBufferTimer = 0;
        private float dashCoyoteTimer = 0;

        // Glide
        private bool gliding = false;

        #endregion

        #region Collision variables

        public bool onGround = false;
        private Vector2 groundNormal = Vector2.zero;
        private Vector2 ceilingNormal = Vector2.zero;

        public bool onWall = false;
        private int wallDirection = 0;
        private Vector2 wallNormal = Vector2.zero;

        public bool onLedge = false;
        private bool climbingLedge = false;
        private Vector2 ledgeCornerPosition = Vector2.zero;

        private bool detectTriggers = false;

        #endregion

        #region State machine

        public PlayerState playerState = PlayerState.NONE;
        public int facingDirection = 1;

        #endregion

        #region Event actions

        public event Action<bool, float> onGroundChanged; // Velocity upon hitting ground
        public event Action<bool, float> onWallChanged;
        public event Action<bool> ledgeClimbChanged;
        public event Action jumped;
        public event Action wallJumped;
        public event Action airJumped;
        public event Action<bool> dashingChanged;
        public event Action<bool> glidingChanged;

        #endregion

        #region External

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

        #endregion

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
            // Increment timers
            jumpBufferTimer += Time.fixedDeltaTime;
            coyoteTimer += Time.fixedDeltaTime;
            wallJumpCoyoteTimer += Time.fixedDeltaTime;
            wallJumpControlLossTimer += Time.fixedDeltaTime;
            dashTimer += Time.fixedDeltaTime;
            dashCooldownTimer += Time.fixedDeltaTime;
            dashBufferTimer += Time.fixedDeltaTime;
            dashCoyoteTimer += Time.fixedDeltaTime;

            handlePhysics();
            handleCollisions();

            handleJump();
            handleDash();
            handleGlide();

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
                jumpBufferTimer = 0;
            }

            // Dash
            if (InputManager.instance.getKeyDown("dash"))
            {
                dashToConsume = true;
                dashBufferTimer = 0;
            }

            // Glide
            glideKey = InputManager.instance.getKey("glide");

            // Set player's facing direction to last horizontal input
            facingDirection = lastPlayerInput.x >= 0 ? 1 : -1;
        }

        #endregion

        #region Physics

        private void handlePhysics()
        {
            #region Vertical physics

            // Dashing
            if (dashing)
            {

            }
            // Wall
            else if (onWall)
            {
                // Climb wall
                if (playerInput.y > 0 && !onLedge) velocity.y = playerPhysicsStats.wallClimbSpeed;
                // Fast fall on wall
                else if (playerInput.y < 0) velocity.y = -playerPhysicsStats.fastWallFallSpeed;
                // Decelerate rapidly when grabbing ledge
                else if (onLedge) velocity.y = Mathf.MoveTowards(velocity.y, 0, playerPhysicsStats.ledgeGrabDeceleration * Time.fixedDeltaTime);
                // Slow fall on wall
                else if (velocity.y < -playerPhysicsStats.maxWallFallSpeed) velocity.y = -playerPhysicsStats.maxWallFallSpeed;
                else velocity.y = Mathf.MoveTowards(Mathf.Min(velocity.y, 0), -playerPhysicsStats.maxWallFallSpeed, playerPhysicsStats.wallFallAcceleration * Time.fixedDeltaTime);
                //else velocity.y = 0;
            }
            // Gliding (And downwards velocity)
            else if (gliding && velocity.y < 0)
            {
                // Cap fall speed
                velocity.y = Mathf.Max(velocity.y, -playerPhysicsStats.glideFallSpeed);

                // Accelerate towards glideFallSpeed using playerPhysicsStats.glideFallAcceleration
                velocity.y = Mathf.MoveTowards(velocity.y, -playerPhysicsStats.glideFallSpeed, playerPhysicsStats.glideFallAcceleration * Time.fixedDeltaTime);
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

            // Decrease wall jump input loss
            wallJumpControlLossMultiplier = Mathf.Clamp(wallJumpControlLossTimer / playerPhysicsStats.wallJumpInputLossTime, 0f, 1f);

            // Dashing
            if (dashing)
            {

            }
            // Player input is in the opposite direction of current velocity
            else if (playerInput.x != 0 && velocity.x != 0 && Mathf.Sign(playerInput.x) != Mathf.Sign(velocity.x) && wallJumpControlLossMultiplier == 1)
            {
                // Instantly reset velocity
                velocity.x = 0;
            }
            // Deceleration
            else if (playerInput.x == 0 && wallJumpControlLossMultiplier == 1)
            {
                var deceleration = onGround ? playerPhysicsStats.groundDeceleration : playerPhysicsStats.airDeceleration;

                // Decelerate towards 0
                velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            // Regular Horizontal Movement
            else
            {
                // Accelerate towards max speed
                velocity.x = Mathf.MoveTowards(velocity.x, playerInput.x * playerPhysicsStats.maxRunSpeed, wallJumpControlLossMultiplier * playerPhysicsStats.acceleration * Time.fixedDeltaTime);

                // Reset x velocity when on wall
                if (onWall) velocity.x = 0;
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
            {
                onGround = true;
                resetJump();

                // Invoke onGroundChanged event action
                onGroundChanged?.Invoke(true, Mathf.Abs(velocity.y));
            }
            // Leave ground
            else if (onGround && (groundHitCount == 0 || groundAngle > playerPhysicsStats.maxWalkAngle))
            {
                onGround = false;

                // Start coyote timer
                coyoteTimer = 0;
                dashCoyoteTimer = 0;

                // Invoke onGroundChanged event action
                onGroundChanged?.Invoke(false, 0);
            }
            // On ground
            else if (onGround && groundHitCount > 0 && groundAngle <= playerPhysicsStats.maxWalkAngle)
            {
                // Handle slopes
                if (groundNormal != Vector2.zero) // Make sure ground normal exists
                {
                    if (!Mathf.Approximately(Math.Abs(groundNormal.y), 1f))
                    {
                        // Change y velocity to match ground slope
                        float groundSlope = -groundNormal.x / groundNormal.y;
                        velocity.y = velocity.x * groundSlope;

                        // Give the player a constant velocity so that they stick to sloped ground
                        if (velocity.x != 0) velocity.y += playerPhysicsStats.groundingForce;
                    }
                }
            }

            // Enter ceiling
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
            // Conditions to enter wall:
            //    Player is actively inputting direction of the wall
            //    Wall is of climbable angle
            //    Not colliding with ground or ceiling
            //    Not currently moving upwards
            if (!onWall && wallHitCount > 0 && playerInput.x != 0 && wallAngle <= playerPhysicsStats.maxClimbAngle && !onGround && !ceilingCollision && velocity.y < 0)
            {
                onWall = true;
                wallDirection = (int)Mathf.Sign(lastPlayerInput.x);
                velocity = Vector2.zero;
                resetJump();

                // Invoke onWallChanged event action
                onWallChanged?.Invoke(true, Mathf.Abs(velocity.x));
            }
            // Leave wall
            else if (onWall && (wallHitCount == 0 || wallAngle > playerPhysicsStats.maxClimbAngle || onGround))
            {
                onWall = false;
                onLedge = false;
                climbingLedge = false;

                // Start wall jump coyote timer
                wallJumpCoyoteTimer = 0;
                dashCoyoteTimer = 0;

                // Invoke onWallChanged event action
                onWallChanged?.Invoke(false, 0);
            }
            // On wall
            else if (onWall && wallHitCount > 0 && wallAngle <= playerPhysicsStats.maxClimbAngle && !onGround)
            {
                // Handle slopes
                if (wallNormal != Vector2.zero) // Make sure wall normal exists
                {
                    if (!Mathf.Approximately(Math.Abs(wallNormal.x), 1f))
                    {
                        // Change x velocity to match wall slope
                        float inverseWallSlope = -wallNormal.y / wallNormal.x;
                        velocity.x = velocity.y * inverseWallSlope;

                        // Give the player a constant velocity so that they stick to sloped walls
                        //if (velocity.y != 0) velocity.x += playerPhysicsStats.groundingForce * -wallDirection;
                    }
                }

                // Handle ledges
                Vector2 newLedgeCornerPosition = Vector2.zero;
                onLedge = getLedgeCorner(out newLedgeCornerPosition);
                if (onLedge)
                {
                    // Set new ledge corner position
                    ledgeCornerPosition = newLedgeCornerPosition;

                    // Nudge towards better grabbing position
                    if (hasControl)
                    {
                        Vector2 targetPosition = ledgeCornerPosition - Vector2.Scale(playerPhysicsStats.ledgeGrabPoint, new(wallDirection, 1f));
                        rigidbody.position = Vector2.MoveTowards(rigidbody.position, targetPosition, playerPhysicsStats.ledgeGrabDeceleration * Time.fixedDeltaTime);
                    }

                    // Detect ledge climb input and check to see if final position is clear
                    Vector2 resultantPosition = ledgeCornerPosition + Vector2.Scale(playerPhysicsStats.standUpOffset, new(wallDirection, 1f));
                    if (!climbingLedge && playerInput.y > 0 && checkPositionClear(resultantPosition)) StartCoroutine(climbLedge());
                }
            }
            // Not on wall
            else if (!onWall)
            {
                onLedge = false;
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
            if (!onWall) return false;

            // Can grab ledge if a raycast from the top does not hit any walls
            RaycastHit2D topHit = Physics2D.Raycast(activeCollider.bounds.center + new Vector3(0, activeCollider.size.y / 2), wallDirection * Vector2.right, (wallDirection * activeCollider.size.x / 2) + playerPhysicsStats.ledgeRaycastDistance, ~playerPhysicsStats.playerLayer);
            if (topHit.collider) return false;

            // Get x position of corner
            RaycastHit2D wallHit = Physics2D.CapsuleCast(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, wallDirection * Vector2.right, playerPhysicsStats.ledgeRaycastDistance, ~playerPhysicsStats.playerLayer);
            if (!wallHit.collider) return false;
            // Get y position of corner
            RaycastHit2D cornerHit = Physics2D.Raycast(activeCollider.bounds.center + new Vector3(wallDirection * playerPhysicsStats.ledgeGrabPoint.x * 2, activeCollider.size.y / 2), Vector2.down, activeCollider.size.y, ~playerPhysicsStats.playerLayer);
            if (!cornerHit.collider) return false;

            cornerPos = new(wallHit.point.x, cornerHit.point.y);
            return true;
        }

        private IEnumerator climbLedge()
        {
            // Invoke ledgeClimbChanged event action at the start
            ledgeClimbChanged?.Invoke(true);

            // Take away player control
            hasControl = false;
            rigidbody.velocity = Vector2.zero;

            // Reset ledge and wall flags
            climbingLedge = true;
            onLedge = false;
            onWall = false;

            // Set startup position
            transform.position = ledgeCornerPosition - Vector2.Scale(playerPhysicsStats.ledgeGrabPoint, new(wallDirection, 1f));

            // Wait for ledge climb animation to finish
            float animationEndTime = Time.time + playerPhysicsStats.ledgeClimbDuration;
            while (Time.time < animationEndTime) yield return new WaitForFixedUpdate();

            // Set final position
            transform.position = ledgeCornerPosition + Vector2.Scale(playerPhysicsStats.standUpOffset, new(wallDirection, 1f));
            
            // Reset ledge and wall flags
            climbingLedge = false;
            onLedge = false;
            onWall = false;
            wallJumpCoyoteUsable = false;

            // Return control to player
            hasControl = true;
            velocity.x = 0;

            // Invoke ledgeClimbChanged event action at the end
            ledgeClimbChanged?.Invoke(false);
        }

        private bool checkPositionClear(Vector2 position)
        {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.OverlapCapsule(position + activeCollider.offset, activeCollider.size - new Vector2(0.1f, 0.1f), activeCollider.direction, 0, ~playerPhysicsStats.playerLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            return !hit;
        }

        #endregion

        #region Jump

        private void handleJump()
        {
            // Check if player has control
            if (!hasControl) return;

            bool canUseJumpBuffer = jumpBufferUsable && jumpBufferTimer < playerPhysicsStats.jumpBufferTime;
            bool canUseCoyote = coyoteUsable && coyoteTimer < playerPhysicsStats.coyoteTime;
            bool canUseWallJumpCoyote = wallJumpCoyoteUsable && wallJumpCoyoteTimer < playerPhysicsStats.wallJumpCoyoteTime;

            // Detect early jump end
            if (!endedJumpEarly && !onGround && !onWall && !jumpKey && velocity.y > 0) endedJumpEarly = true;

            // Check for jump input
            if (!jumpToConsume && !canUseJumpBuffer) return;

            if ((onWall || canUseWallJumpCoyote) && !climbingLedge) wallJump();
            else if (onGround || canUseCoyote) normalJump();
            else if (airJumpsRemaining > 0) airJump();

            jumpToConsume = false; // Always consume the flag
        }

        private void normalJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            jumpBufferUsable = false;
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
            jumpBufferUsable = false;
            wallJumpCoyoteUsable = false;

            // Apply jump velocity
            velocity = Vector2.Scale(playerPhysicsStats.wallJumpStrength, new(-wallDirection, 1));
            wallJumpControlLossMultiplier = 0;
            wallJumpControlLossTimer = 0;

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
            jumpBufferUsable = true;
            if (onGround) coyoteUsable = true;
            if (onWall && !onGround) wallJumpCoyoteUsable = true;
            endedJumpEarly = false;

            // Reset number of air jumps
            airJumpsRemaining = maxAirJumps;

            // Reset dash
            canDash = true;
            if (onGround) dashBufferUsable = true; // Don't allow dash buffer on wall
            dashCoyoteUsable = true;
        }

        #endregion

        #region Dash

        private void handleDash()
        {
            bool canUseDashBuffer = dashBufferUsable && dashBufferTimer < playerPhysicsStats.dashBufferTime;
            bool canUseDashCoyote = dashCoyoteUsable && dashCoyoteTimer < playerPhysicsStats.dashCoyoteTime;

            // Check for conditions to initiate dash:
            //    Not currently dashing
            //    Player dash input detected or buffered
            //    Can dash or use dash coyote
            //    Dash cooldown elapsed
            if (!dashing && (dashToConsume || canUseDashBuffer) && (canDash || canUseDashCoyote) && dashCooldownTimer > playerPhysicsStats.dashCooldownTime)
            {
                // Set dash velocity
                if (onWall) dashVelocity = playerPhysicsStats.dashVelocity * new Vector2(-wallDirection, 0);
                else dashVelocity = playerPhysicsStats.dashVelocity * new Vector2(lastPlayerInput.x, 0);

                // Set dash flags
                dashing = true;
                if (!onGround && !onWall)
                {
                    if (!canUseDashCoyote)
                    {
                        canDash = false;
                        dashBufferUsable = false;
                    }
                    dashCoyoteUsable = false;
                }

                // Start dash timer
                dashTimer = 0;

                // Remove external velocity
                externalVelocity = Vector2.zero;

                // Invoke dashing changed event action
                dashingChanged?.Invoke(true);
            }

            // Handle the dash itself
            if (dashing)
            {
                // Maintain dash velocity
                velocity = dashVelocity;

                // Check if dash time has been reached
                if (dashTimer >= playerPhysicsStats.dashTime)
                {
                    // Reset dashing flag
                    dashing = false;

                    // Start dash cooldown timer
                    dashCooldownTimer = 0;

                    // Set player velocity at end of dash
                    velocity.x *= playerPhysicsStats.dashEndHorizontalMultiplier;
                    velocity.y = Mathf.Min(0, velocity.y);

                    // Invoke dashing changed event action
                    dashingChanged?.Invoke(false);
                }
            }

            // Reset dash to consume flag regardless
            dashToConsume = false;
        }

        #endregion

        #region Glide

        private void handleGlide()
        {
            // Check for conditions to initate glide
            if (!gliding && glideKey && !onGround && !onWall && !dashing)
            {
                // Set gliding flag
                gliding = true;

                // Invoke glidingChanged event action
                glidingChanged?.Invoke(true);
            }

            // Check for conditions to stop glide
            if (gliding && (!glideKey || onGround || onWall || dashing))
            {
                // Reset gliding flag
                gliding = false;

                // Invoke glidingChanged event action
                glidingChanged?.Invoke(false);
            }
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
            // Call corresponding state function
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
                case PlayerState.ON_WALL:
                    onWallState();
                    break;
                case PlayerState.ON_LEDGE:
                    onLedgeState();
                    break;
                case PlayerState.CLIMB_LEDGE:
                    climbLedgeState();
                    break;
                case PlayerState.DASH:
                    dashState();
                    break;
                case PlayerState.GLIDE:
                    glideState();
                    break;
                default: break;
            }
        }

        private void idleState()
        {
            sprite.color = Color.blue;

            // Switch states
            if      (dashing)            playerState = PlayerState.DASH;
            else if (!onGround)          playerState = PlayerState.AIRBORNE;
            else if (playerInput.x != 0) playerState = PlayerState.RUN;
        }

        private void runState()
        {
            sprite.color = Color.red;

            // Switch states
            if      (dashing)         playerState = PlayerState.DASH;
            else if (!onGround)       playerState = PlayerState.AIRBORNE;
            else if (velocity.x == 0) playerState = PlayerState.IDLE;
        }

        private void airborneState()
        {
            sprite.color = Color.yellow;

            // Switch states
            if      (dashing)                     playerState = PlayerState.DASH;
            else if (gliding)                     playerState = PlayerState.GLIDE;
            else if (onGround && velocity.x == 0) playerState = PlayerState.IDLE;
            else if (onGround)                    playerState = PlayerState.RUN;
            else if (onWall)                      playerState = PlayerState.ON_WALL;
        }

        private void onWallState()
        {
            sprite.color = Color.cyan;

            // Switch states
            if      (climbingLedge)               playerState = PlayerState.CLIMB_LEDGE;
            else if (onLedge)                     playerState = PlayerState.ON_LEDGE;
            else if (dashing)                     playerState = PlayerState.DASH;
            else if (!onWall && !onGround)        playerState = PlayerState.AIRBORNE;
            else if (onGround && velocity.x == 0) playerState = PlayerState.IDLE;
            else if (onGround)                    playerState = PlayerState.RUN;
        }

        private void onLedgeState()
        {
            sprite.color = Color.gray;

            // Switch states
            if      (climbingLedge)        playerState = PlayerState.CLIMB_LEDGE;
            else if (dashing)              playerState = PlayerState.DASH;
            else if (!onLedge && onGround) playerState = PlayerState.IDLE;
            else if (!onLedge && onWall)   playerState = PlayerState.ON_WALL;
            else if (!onLedge)             playerState = PlayerState.AIRBORNE;
        }

        private void climbLedgeState()
        {
            sprite.color = Color.black;

            // Switch states
            if (!climbingLedge) playerState = PlayerState.IDLE;
        }

        private void dashState()
        {
            sprite.color = Color.green;

            // Switch states
            if      (!dashing && onGround) playerState = PlayerState.RUN;
            else if (!dashing && onWall)   playerState = PlayerState.ON_WALL;
            else if (!dashing)             playerState = PlayerState.AIRBORNE;
        }

        private void glideState()
        {
            sprite.color = Color.magenta;

            // Switch states
            if      (dashing)  playerState = PlayerState.DASH;
            else if (onGround) playerState = PlayerState.IDLE;
            else if (onWall)   playerState = PlayerState.ON_WALL;
            else if (!gliding) playerState = PlayerState.AIRBORNE;
        }

        #endregion
    }
}