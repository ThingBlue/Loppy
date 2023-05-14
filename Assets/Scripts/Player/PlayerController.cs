// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

using Loppy.GameCore;
using System;
using System.Collections;
using UnityEngine;

namespace Loppy.Player
{
    public enum PlayerForce
    {
        BURST = 0, // Added directly to the players movement speed, to be controlled by the standard deceleration
        DECAY // An external velocity that decays over time, applied additively to the rigidbody's velocity
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
        public Transform cameraFocalPoint;

        public GameObject grappleRangeCircle;
        public LineRenderer grappleArrowLineRenderer;

        public GameObject alternateGrappleRangeCircle;
        public LineRenderer alternateGrappleArrowLineRenderer;

        public Rigidbody2D rigidbody;
        public CapsuleCollider2D standingCollider;

        public PlayerPhysicsData playerPhysicsData;
        public PlayerUnlocks playerUnlocks;
        public PlayerAnimationData playerAnimationData;

        #endregion

        #region Input variables

        private bool hasControl = true;

        private Vector2 playerInput = Vector2.zero;
        private Vector2 lastPlayerInput = new(1, 0);
        private Vector2 playerInputDown = Vector2.zero; // Only true on the first frame of key down

        private bool jumpKey = false;
        private bool jumpToConsume = false;
        private bool dashToConsume = false;
        private bool glideKey = false;
        private bool grappleKey = false;
        private bool grappleToConsume = false;
        private bool alternateGrappleKey = false;
        private bool alternateGrappleToConsume = false;

        #endregion

        #region Physics variables

        public Vector2 velocity = Vector2.zero;
        private Vector2 externalVelocity = Vector2.zero;

        // Jump
        private bool canEndJumpEarly = false;
        private bool endedJumpEarly = false;

        private bool jumpBufferUsable = false;
        private bool jumpCoyoteUsable = false;
        private float jumpBufferTimer = 0;
        private float jumpCoyoteTimer = 0;

        private bool wallJumpCoyoteUsable = false;
        private float wallJumpCoyoteTimer = 0;
        private float wallJumpControlLossMultiplier = 1;
        private float wallJumpControlLossTimer = 0;

        private int airJumpsRemaining = 0;

        // Dash
        private bool dashing = false;
        private bool canDash = false;
        private bool dashBufferUsable = false;
        private bool dashCoyoteUsable = false;

        private Vector2 dashVelocity = Vector2.zero;

        private float dashTimer = 0;
        private float dashCooldownTimer = 0;
        private float dashBufferTimer = 0;
        private float dashCoyoteTimer = 0;

        // Dash jump
        private float dashJumpControlLossMultiplier = 1;
        private float dashJumpControlLossTimer = 0;

        // Glide
        private bool gliding = false;

        // Grapple
        private bool grappleAiming = false;
        private bool grappling = false;
        private bool canGrapple = false;
        private bool grappleBufferUsable = false;

        private Vector2 grappleAimDirection = Vector2.zero;

        private Collider2D grappleAimTargetCollider = null;
        private Vector3 grappleAimTargetPosition = Vector2.zero;
        private bool grappleAimHitEnemy = false;

        private Collider2D grappleTargetCollider = null;
        private Vector3 grappleTargetPosition = Vector2.zero;
        private bool grappleHitEnemy = false;

        private float grappleBufferTimer = 0;
        private float grappleControlLossMultiplier = 1;
        private float grappleControlLossTimer = 0;

        // Alternate grapple
        private bool alternateGrappleAiming = false;
        private bool alternateGrappling = false;
        private bool canAlternateGrapple = false;
        private bool alternateGrappleBufferUsable = false;

        private Vector2 alternateGrappleAimDirection = Vector2.zero;

        private Collider2D alternateGrappleAimTargetCollider = null;
        private Vector3 alternateGrappleAimTargetPosition = Vector2.zero;
        private bool alternateGrappleAimHitEnemy = false;

        private Collider2D alternateGrappleTargetCollider = null;
        private Vector3 alternateGrappleTargetPosition = Vector2.zero;
        private bool alternateGrappleHitEnemy = false;

        private float alternateGrappleFreezeTimer = 0;

        private float alternateGrappleBufferTimer = 0;
        private float alternateGrappleControlLossMultiplier = 1;
        private float alternateGrappleControlLossTimer = 0;

        #endregion

        #region Collision variables

        private CapsuleCollider2D activeCollider;

        public bool onGround = false;
        private bool leavingGround = false;
        public Vector2 groundNormal = Vector2.zero;
        private Vector2 ceilingNormal = Vector2.zero;

        public bool onWall = false;
        private int wallDirection = 0;
        public Vector2 wallNormal = Vector2.zero;

        public bool onLedge = false;
        private bool climbingLedge = false;
        private Vector2 ledgeCornerPosition = Vector2.zero;
        private float ledgeClimbTimer = 0;

        private bool detectTriggers = false;

        private GameObject currentPlatform = null;
        private bool collidingWithPlatform = false;
        private bool fallingThroughPlatform = false;

        #endregion

        #region State machine

        public PlayerState playerState = PlayerState.NONE;
        public int facingDirection = 1;

        #endregion

        #region Event actions

        public event Action<bool, float> onGrounded; // Velocity upon hitting ground
        public event Action<bool, float> onWallCling;
        public event Action<bool> onLedgeClimb;
        public event Action onJump;
        public event Action onWallJump;
        public event Action onAirJump;
        public event Action onDashJump;
        public event Action<bool> onDash;
        public event Action<bool> onGlide;
        public event Action<bool> onGrappleAim;
        public event Action<bool> onGrapple;

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

            // Initialize grapple line renderer
            grappleArrowLineRenderer.positionCount = 2;
            grappleArrowLineRenderer.startWidth = 0.2f;
            grappleArrowLineRenderer.endWidth = 0.2f;

            alternateGrappleArrowLineRenderer.positionCount = 2;
            alternateGrappleArrowLineRenderer.startWidth = 0.2f;
            alternateGrappleArrowLineRenderer.endWidth = 0.2f;
        }

        private void Start()
        {
            // Disable grapple indicators
            grappleRangeCircle.SetActive(false);
            grappleArrowLineRenderer.enabled = false;
            alternateGrappleRangeCircle.SetActive(false);
            alternateGrappleArrowLineRenderer.enabled = false;
        }

        private void Update()
        {
            handleInput();
        }

        private void FixedUpdate()
        {
            // Increment timers
            jumpBufferTimer += Time.fixedDeltaTime;
            jumpCoyoteTimer += Time.fixedDeltaTime;
            wallJumpCoyoteTimer += Time.fixedDeltaTime;
            wallJumpControlLossTimer += Time.fixedDeltaTime;
            dashTimer += Time.fixedDeltaTime;
            dashCooldownTimer += Time.fixedDeltaTime;
            dashBufferTimer += Time.fixedDeltaTime;
            dashCoyoteTimer += Time.fixedDeltaTime;
            dashJumpControlLossTimer += Time.fixedDeltaTime;
            ledgeClimbTimer += Time.fixedDeltaTime;
            grappleBufferTimer += Time.fixedDeltaTime;
            grappleControlLossTimer += Time.fixedDeltaTime;

            alternateGrappleFreezeTimer += Time.fixedDeltaTime;
            alternateGrappleBufferTimer += Time.fixedDeltaTime;
            alternateGrappleControlLossTimer += Time.fixedDeltaTime;

            handlePhysics();
            handleCollisions();

            // Check if player has control
            if (hasControl)
            {
                // Handle movement machanics
                handleJump();
                handleDash();
                handleGlide();
                handleGrapple();
                handleAlternateGrapple();
            }

            move();

            handleStateMachine();

            // Reset input
            playerInputDown = Vector2.zero;
        }

        #region Input

        private void handleInput()
        {
            // Reset inputs at start of frame
            playerInput = Vector2.zero;

            // Check if game is paused
            if (GameManager.gameState == GameState.PAUSED) return;

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

            // Grapple
            grappleKey = InputManager.instance.getKey("grapple");
            if (InputManager.instance.getKeyDown("grapple"))
            {
                grappleToConsume = true;
                grappleBufferTimer = 0;
            }

            alternateGrappleKey = InputManager.instance.getKey("alternateGrapple");
            if (InputManager.instance.getKeyDown("alternateGrapple"))
            {
                alternateGrappleToConsume = true;
                alternateGrappleBufferTimer = 0;
            }

            // Set player's facing direction to last horizontal input
            facingDirection = lastPlayerInput.x >= 0 ? 1 : -1;
        }

        #endregion

        #region Physics

        private void handlePhysics()
        {
            // Increase control loss multipliers
            wallJumpControlLossMultiplier = Mathf.Clamp(wallJumpControlLossTimer / playerPhysicsData.wallJumpControlLossTime, 0f, 1f);
            dashJumpControlLossMultiplier = Mathf.Clamp(dashJumpControlLossTimer / playerPhysicsData.dashJumpControlLossTime, 0f, 1f);
            grappleControlLossMultiplier = Mathf.Clamp(grappleControlLossTimer / playerPhysicsData.grappleControlLossTime, 0f, 1f);
            alternateGrappleControlLossMultiplier = Mathf.Clamp(alternateGrappleControlLossTimer / playerPhysicsData.alternateGrappleControlLossTime, 0f, 1f);

            bool noControlLoss = (wallJumpControlLossMultiplier == 1 && dashJumpControlLossMultiplier == 1 && grappleControlLossMultiplier == 1 && alternateGrappleControlLossMultiplier == 1);

            if (dashing) return;
            if (grappling) return;
            if (alternateGrappling) return;
                
            #region Vertical physics

            // Climbing ledge
            if (climbingLedge)
            {
                // Reset y velocity
                velocity.y = 0;
            }
            // Wall
            else if (onWall)
            {
                // Climb wall
                if (playerInput.y > 0 && !onLedge) velocity.y = playerPhysicsData.wallClimbSpeed;
                // Fast fall on wall
                else if (playerInput.y < 0) velocity.y = -playerPhysicsData.fastWallFallSpeed;
                // Decelerate rapidly when grabbing ledge
                else if (onLedge) velocity.y = Mathf.MoveTowards(velocity.y, 0, playerPhysicsData.ledgeGrabDeceleration * Time.fixedDeltaTime);
                // Slow fall on wall
                else if (velocity.y < -playerPhysicsData.maxWallFallSpeed) velocity.y = -playerPhysicsData.maxWallFallSpeed;
                else velocity.y = Mathf.MoveTowards(Mathf.Min(velocity.y, 0), -playerPhysicsData.maxWallFallSpeed, playerPhysicsData.wallFallAcceleration * Time.fixedDeltaTime);
                //else velocity.y = 0;
            }
            // Gliding (And downwards velocity)
            else if (gliding && velocity.y < 0)
            {
                // Cap fall speed
                velocity.y = Mathf.Max(velocity.y, -playerPhysicsData.glideFallSpeed);

                // Accelerate towards glideFallSpeed using playerPhysicsStats.glideFallAcceleration
                velocity.y = Mathf.MoveTowards(velocity.y, -playerPhysicsData.glideFallSpeed, playerPhysicsData.glideFallAcceleration * Time.fixedDeltaTime);
            }
            // Airborne
            else if (!onGround)
            {
                float airborneAcceleration = playerPhysicsData.fallAcceleration;

                // Check if player ended jump early
                if (endedJumpEarly && velocity.y > 0) airborneAcceleration *= playerPhysicsData.jumpEndEarlyGravityModifier;

                // Accelerate towards maxFallSpeed using airborneAcceleration
                velocity.y = Mathf.MoveTowards(velocity.y, -playerPhysicsData.maxFallSpeed, airborneAcceleration * Time.fixedDeltaTime);
            }

            #endregion

            #region Horizontal physics

            // Player input is in the opposite direction of current velocity
            if (playerInput.x != 0 && velocity.x != 0 && Mathf.Sign(playerInput.x) != Mathf.Sign(velocity.x) && noControlLoss)
            {
                // Instantly reset velocity
                velocity.x = 0;
            }
            // Deceleration
            else if (playerInput.x == 0 && noControlLoss)
            {
                var deceleration = onGround ? playerPhysicsData.groundDeceleration : playerPhysicsData.airDeceleration;

                // Decelerate towards 0
                velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            // Regular Horizontal Movement
            else
            {
                // Accelerate towards max speed
                // Take into account control loss multipliers
                velocity.x = Mathf.MoveTowards(velocity.x, playerInput.x * playerPhysicsData.maxRunSpeed, wallJumpControlLossMultiplier * dashJumpControlLossMultiplier * grappleControlLossMultiplier * alternateGrappleControlLossMultiplier * playerPhysicsData.acceleration * Time.fixedDeltaTime);

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
            Physics2D.queriesHitTriggers = false;
            groundHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.down, groundHits, playerPhysicsData.raycastDistance, playerPhysicsData.terrainLayer);
            ceilingHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, Vector2.up, ceilingHits, playerPhysicsData.raycastDistance, playerPhysicsData.terrainLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            // Get normals
            groundNormal = getRaycastNormal(Vector2.down);
            ceilingNormal = getRaycastNormal(Vector2.up);
            float groundAngle = Vector2.Angle(groundNormal, Vector2.up);

            // Enter ground
            if (!onGround && groundHitCount > 0 &&               // Ground detected
                groundAngle <= playerPhysicsData.maxWalkAngle && // Walkable angle
                !fallingThroughPlatform)                         // Player is not trying to fall through platform
            {
                onGround = true;
                leavingGround = false;
                fallingThroughPlatform = false;
                resetJump();
                resetDash();
                resetGrapple();
                resetAlternateGrapple();

                // Invoke event action
                onGrounded?.Invoke(true, Mathf.Abs(velocity.y));

                // Reset y velocity
                if (groundNormal != Vector2.zero) // Make sure ground normal exists
                {
                    // Handle slopes
                    if (!Mathf.Approximately(Math.Abs(groundNormal.y), 1f))
                    {
                        // Change y velocity to match ground slope
                        float groundSlope = -groundNormal.x / groundNormal.y;
                        velocity.y = velocity.x * groundSlope;
                    }
                    // No slope
                    else
                    {
                        velocity.y = 0;
                    }
                }
            }
            // Leave ground
            else if (onGround && (groundHitCount == 0 || groundAngle > playerPhysicsData.maxWalkAngle || fallingThroughPlatform))
            {
                // Check if player is leaving ground intentionally
                // Using a movement ability or falling through platform
                if (leavingGround || fallingThroughPlatform)
                {
                    // Leave ground
                    onGround = false;

                    // Start coyote timer
                    jumpCoyoteTimer = 0;
                    dashCoyoteTimer = 0;

                    // Invoke event action
                    onGrounded?.Invoke(false, 0);
                }
                // Check if player walked off platform
                else if (!leavingGround)
                {
                    // Do raycast downwards
                    Physics2D.queriesHitTriggers = false;
                    var hit = Physics2D.Raycast(activeCollider.bounds.center, Vector2.down, playerPhysicsData.groundCheckRaycastDistance, playerPhysicsData.terrainLayer);
                    Physics2D.queriesHitTriggers = detectTriggers;

                    Vector2 hitNormal = getRaycastNormal(Vector2.down);
                    float groundCheckAngle = Vector2.Angle(hitNormal, Vector2.up);

                    // There is still a platform, don't leave ground
                    if (hitNormal != Vector2.zero && groundCheckAngle <= playerPhysicsData.maxWalkAngle)
                    {
                        // Move player down to ground
                        //transform.position = new Vector2(transform.position.x, transform.position.y - hit.distance + (activeCollider.size.y / 2));

                        // Change y velocity to match ground slope
                        float groundSlope = -groundNormal.x / groundNormal.y;
                        velocity.y = velocity.x * groundSlope + playerPhysicsData.groundingForce; // Add some negative grounding force
                    }
                    // No hits, leave ground
                    else
                    {
                        velocity.y = 0;

                        // Leave ground
                        onGround = false;

                        // Start coyote timer
                        jumpCoyoteTimer = 0;
                        dashCoyoteTimer = 0;

                        // Invoke event action
                        onGrounded?.Invoke(false, 0);
                    }
                }
            }
            // On ground
            else if (onGround && groundHitCount > 0 && groundAngle <= playerPhysicsData.maxWalkAngle)
            {
                if (!fallingThroughPlatform)
                {
                    // Handle slopes
                    if (groundNormal != Vector2.zero) // Make sure ground normal exists
                    {
                        // Change y velocity to match ground slope
                        float groundSlope = -groundNormal.x / groundNormal.y;
                        velocity.y = velocity.x * groundSlope;

                        /*
                        if (!Mathf.Approximately(Math.Abs(groundNormal.y), 1f))
                        {
                            // Change y velocity to match ground slope
                            float groundSlope = -groundNormal.x / groundNormal.y;
                            velocity.y = velocity.x * groundSlope;

                            // Give the player a constant velocity so that they stick to sloped ground
                            //if (velocity.x != 0) velocity.y += playerPhysicsData.groundingForce;
                        }
                        */
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

            // Handle platforms
            if (playerInput.y < 0 && currentPlatform != null && !fallingThroughPlatform)
            {
                StartCoroutine(disableCollision());
                fallingThroughPlatform = true;
            }

            #endregion

            #region Horizontal collisions

            // Raycast to check for horizontal collisions
            Physics2D.queriesHitTriggers = false;
            wallHitCount = Physics2D.CapsuleCastNonAlloc(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, new(lastPlayerInput.x, 0), wallHits, playerPhysicsData.raycastDistance, playerPhysicsData.terrainLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            // Get normal
            wallNormal = getRaycastNormal(new(lastPlayerInput.x, 0));
            float wallAngle = Mathf.Min(Vector2.Angle(wallNormal, Vector2.left), Vector2.Angle(wallNormal, Vector2.right));

            // Enter wall
            // Conditions to enter wall:
            //    Player is actively inputting direction of the wall
            //    Wall is of climbable angle
            //    Not colliding with ground or ceiling
            //    Not currently moving upwards
            if (playerUnlocks.wallClimbUnlocked && !onWall && wallHitCount > 0 && playerInput.x != 0 && wallAngle <= playerPhysicsData.maxClimbAngle && !onGround && !ceilingCollision && velocity.y < 0)
            {
                onWall = true;
                wallDirection = (int)Mathf.Sign(lastPlayerInput.x);
                velocity = Vector2.zero;
                resetJump();
                resetDash();
                resetGrapple();
                resetAlternateGrapple();

                // Invoke event action
                onWallCling?.Invoke(true, Mathf.Abs(velocity.x));
            }
            // Leave wall
            else if (onWall && (wallHitCount == 0 || wallAngle > playerPhysicsData.maxClimbAngle || onGround))
            {
                onWall = false;
                onLedge = false;
                climbingLedge = false;

                // Start wall jump coyote timer
                wallJumpCoyoteTimer = 0;
                dashCoyoteTimer = 0;

                // Invoke event action
                onWallCling?.Invoke(false, 0);
            }
            // On wall
            else if (onWall && wallHitCount > 0 && wallAngle <= playerPhysicsData.maxClimbAngle && !onGround)
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
                        Vector2 targetPosition = ledgeCornerPosition - Vector2.Scale(playerPhysicsData.ledgeGrabPoint, new(wallDirection, 1f));
                        rigidbody.position = Vector2.MoveTowards(rigidbody.position, targetPosition, playerPhysicsData.ledgeGrabDeceleration * Time.fixedDeltaTime);
                    }

                    // Detect ledge climb input and check to see if final position is clear
                    Vector2 resultantPosition = ledgeCornerPosition + Vector2.Scale(playerPhysicsData.standUpOffset, new(wallDirection, 1f));
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
            var hit = Physics2D.CapsuleCast(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, castDirection, playerPhysicsData.normalRaycastDistance, playerPhysicsData.terrainLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            if (!hit.collider) return Vector2.zero;

            return hit.normal; // Defaults to Vector2.zero if nothing was hit
        }

        private bool getLedgeCorner(out Vector2 cornerPos)
        {
            // Reset corner position
            cornerPos = Vector2.zero;

            // Check if player is on wall
            if (!onWall) return false;

            Physics2D.queriesHitTriggers = false;

            // Can grab ledge if a raycast from the top does not hit any walls
            RaycastHit2D topHit = Physics2D.Raycast(activeCollider.bounds.center + new Vector3(0, activeCollider.size.y / 2), wallDirection * Vector2.right, (wallDirection * activeCollider.size.x / 2) + playerPhysicsData.ledgeRaycastDistance, playerPhysicsData.terrainLayer);
            
            // Get x position of corner
            RaycastHit2D wallHit = Physics2D.CapsuleCast(activeCollider.bounds.center, activeCollider.size, activeCollider.direction, 0, wallDirection * Vector2.right, playerPhysicsData.ledgeRaycastDistance, playerPhysicsData.terrainLayer);
            // Get y position of corner
            RaycastHit2D cornerHit = Physics2D.Raycast(activeCollider.bounds.center + new Vector3(wallDirection * playerPhysicsData.ledgeGrabPoint.x * 2, activeCollider.size.y / 2), Vector2.down, activeCollider.size.y, playerPhysicsData.terrainLayer);
            
            Physics2D.queriesHitTriggers = detectTriggers;

            // Check if no corner was found
            if (topHit.collider || !wallHit.collider || !cornerHit.collider) return false;

            cornerPos = new(wallHit.point.x, cornerHit.point.y);
            return true;
        }

        private IEnumerator climbLedge()
        {
            // Invoke event action at start
            onLedgeClimb?.Invoke(true);

            // Take away player control
            hasControl = false;
            rigidbody.velocity = Vector2.zero;

            // Reset ledge and wall flags
            climbingLedge = true;
            ledgeClimbTimer = 0;

            // Get startup and resultant positions
            Vector2 startupPosition = ledgeCornerPosition - Vector2.Scale(playerPhysicsData.ledgeGrabPoint, new(wallDirection, 1f));
            Vector2 resultantPosition = ledgeCornerPosition + Vector2.Scale(playerPhysicsData.standUpOffset, new(wallDirection, 1f));

            // Set startup position
            transform.position = startupPosition;
            cameraFocalPoint.position = transform.position;

            // Wait for ledge climb animation to finish
            while (ledgeClimbTimer < playerPhysicsData.ledgeClimbDuration)
            {
                // Gradually move camera towards resultant position
                cameraFocalPoint.position = startupPosition + ((resultantPosition - startupPosition) * Mathf.Clamp(ledgeClimbTimer / playerPhysicsData.ledgeClimbDuration, 0f, 1f));
                yield return new WaitForFixedUpdate();
            }

            // Set final position
            transform.position = resultantPosition;
            cameraFocalPoint.position = transform.position;

            // Reset ledge and wall flags
            climbingLedge = false;
            onLedge = false;
            onWall = false;
            wallJumpCoyoteUsable = false;

            // Return control to player
            hasControl = true;
            velocity.x = 0;

            // Invoke event action at end
            onLedgeClimb?.Invoke(false);
        }

        private bool checkPositionClear(Vector2 position)
        {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.OverlapCapsule(position + activeCollider.offset, activeCollider.size - new Vector2(0.1f, 0.1f), activeCollider.direction, 0, playerPhysicsData.terrainLayer);
            Physics2D.queriesHitTriggers = detectTriggers;

            return !hit;
        }

        private IEnumerator disableCollision()
        {
            // Ignore collision until player can fall through platform
            Physics2D.IgnoreCollision(activeCollider, currentPlatform.GetComponent<BoxCollider2D>());
            yield return new WaitForSeconds(playerPhysicsData.platformCollisionIgnoreTime);

            // Stop ignoring collision
            Physics2D.IgnoreCollision(activeCollider, currentPlatform.GetComponent<BoxCollider2D>(), false);

            // Reset current platform
            if (!collidingWithPlatform)
            {
                currentPlatform = null;
            }

            // Reset flags
            fallingThroughPlatform = false;
        }

        #endregion

        #region Jump

        private void handleJump()
        {
            bool canUseJumpBuffer = jumpBufferUsable && jumpBufferTimer < playerPhysicsData.jumpBufferTime;
            bool canUseCoyote = jumpCoyoteUsable && jumpCoyoteTimer < playerPhysicsData.coyoteTime;
            bool canUseWallJumpCoyote = wallJumpCoyoteUsable && wallJumpCoyoteTimer < playerPhysicsData.wallJumpCoyoteTime;

            // Detect early jump end
            if (!endedJumpEarly && !onGround && !onWall && !jumpKey && velocity.y > 0 && canEndJumpEarly)
            {
                endedJumpEarly = true;
                canEndJumpEarly = false;
            }

            // Check for jump input
            if (!jumpToConsume && !canUseJumpBuffer) return;

            if (dashing && (onGround || airJumpsRemaining > 0)) dashJump();
            else if ((onWall || canUseWallJumpCoyote) && !climbingLedge) wallJump();
            else if (onGround || canUseCoyote) normalJump();
            else if (airJumpsRemaining > 0) airJump();

            jumpToConsume = false; // Always consume the flag
        }

        private void normalJump()
        {
            // Reset jump flags
            leavingGround = true;
            endedJumpEarly = false;
            canEndJumpEarly = true;
            jumpBufferUsable = false;
            jumpCoyoteUsable = false;

            // Apply jump velocity
            velocity.y = playerPhysicsData.jumpStrength;

            // Invoke event action
            onJump?.Invoke();
        }

        protected void wallJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            canEndJumpEarly = true;
            jumpBufferUsable = false;
            wallJumpCoyoteUsable = false;

            // Apply wall jump velocity
            velocity = Vector2.Scale(playerPhysicsData.wallJumpStrength, new(-wallDirection, 1));
            wallJumpControlLossMultiplier = 0;
            wallJumpControlLossTimer = 0;

            // Reset onWall status
            onWall = false;

            // Invoke event action
            onWallJump?.Invoke();
        }

        private void airJump()
        {
            // End other movement abilities
            endGlide();
            endGrapple();
            endAlternateGrapple();

            // Reset jump flags
            endedJumpEarly = false;
            canEndJumpEarly = true;
            airJumpsRemaining--;

            // Apply air jump velocity
            velocity.y = playerPhysicsData.jumpStrength;
            externalVelocity.y = 0; // Air jump cancels out vertical external forces

            // Invoke event action
            onAirJump?.Invoke();
        }

        private void dashJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            canEndJumpEarly = true;
            jumpBufferUsable = false;
            jumpCoyoteUsable = false;
            if (!onGround && !onWall) airJumpsRemaining--;
            if (onGround) leavingGround = true;

            // Apply dash jump velocity
            velocity = Vector2.Scale(playerPhysicsData.dashJumpStrength, new(Mathf.Sign(velocity.x), 1));
            dashJumpControlLossMultiplier = 0;
            dashJumpControlLossTimer = 0;

            // Reset dashing status
            dashing = false;
            dashCooldownTimer = 0;

            // Invoke event actions
            onDash?.Invoke(false);
            onDashJump?.Invoke();
        }

        private void resetJump()
        {
            // Reset jump flags
            endedJumpEarly = false;
            canEndJumpEarly = false;
            jumpBufferUsable = true;
            if (onGround) jumpCoyoteUsable = true;
            if (onWall && !onGround) wallJumpCoyoteUsable = true;

            // Reset number of air jumps
            airJumpsRemaining = playerUnlocks.airJumps;
        }

        #endregion

        #region Dash

        private void handleDash()
        {
            bool canUseDashBuffer = dashBufferUsable && dashBufferTimer < playerPhysicsData.dashBufferTime;
            bool canUseDashCoyote = dashCoyoteUsable && dashCoyoteTimer < playerPhysicsData.dashCoyoteTime;

            // Check for conditions to initiate dash:
            //    Not currently dashing
            //    Player dash input detected or buffered
            //    Can dash or use dash coyote
            //    Dash cooldown elapsed
            if (playerUnlocks.dashUnlocked && !dashing && (dashToConsume || canUseDashBuffer) && (canDash || canUseDashCoyote) && dashCooldownTimer > playerPhysicsData.dashCooldownTime)
            {
                // End other movement abilities
                endGlide();
                endGrapple();
                endAlternateGrapple();

                // Reset grounded flags
                onGround = false;
                leavingGround = true;

                // Set dash velocity
                if (onWall) dashVelocity = playerPhysicsData.dashVelocity * new Vector2(-wallDirection, 0);
                else dashVelocity = playerPhysicsData.dashVelocity * new Vector2(lastPlayerInput.x, 0);

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

                // Invoke event action
                onDash?.Invoke(true);
            }

            // Handle the dash itself
            if (dashing)
            {
                // Maintain dash velocity
                velocity = dashVelocity;

                // Check if dash time has been reached
                if (dashTimer >= playerPhysicsData.dashTime)
                {
                    endDash();

                    // Start dash cooldown timer
                    dashCooldownTimer = 0;

                    // Set player velocity at end of dash
                    velocity.x *= playerPhysicsData.dashEndHorizontalMultiplier;
                    velocity.y = Mathf.Min(0, velocity.y);
                }
            }

            // Reset dash to consume flag regardless
            dashToConsume = false;
        }

        private void endDash()
        {
            if (dashing)
            {
                // Reset dashing flag
                dashing = false;

                // Invoke event action
                onDash?.Invoke(false);
            }
        }

        private void resetDash()
        {
            // Reset dash
            canDash = true;
            if (onGround) dashBufferUsable = true; // Don't allow dash buffer on wall
            dashCoyoteUsable = true;
        }

        #endregion

        #region Glide

        private void handleGlide()
        {
            // Check for conditions to initate glide
            if (playerUnlocks.glideUnlocked && !gliding && glideKey && !onGround && !onWall && !dashing)
            {
                // Set gliding flag
                gliding = true;

                // Invoke glidingChanged event action
                onGlide?.Invoke(true);
            }

            // Check for conditions to stop glide
            if (gliding && (!glideKey || onGround || onWall || dashing))
            {
                endGlide();
            }
        }

        private void endGlide()
        {
            if (gliding)
            {
                // Reset gliding flag
                gliding = false;

                // Invoke glidingChanged event action
                onGlide?.Invoke(false);
            }
        }

        #endregion

        #region Grapple

        private void handleGrapple()
        {
            bool canUseGrappleBuffer = grappleBufferUsable && grappleBufferTimer < playerPhysicsData.grappleBufferTime;

            // Check for conditions to initiate grapple aim
            if (playerUnlocks.grappleUnlocked && !grappleAiming && (grappleToConsume || canUseGrappleBuffer) && canGrapple)
            {
                // Invoke event action
                onGrappleAim?.Invoke(true);

                // Start grapple aim
                grappleAiming = true;

                // Activate grapple indicators
                grappleRangeCircle.SetActive(true);
                grappleArrowLineRenderer.enabled = true;
            }

            // Handle grapple aim
            if (grappleAiming)
            {
                // Get grapple direction
                grappleAimDirection = InputManager.instance.getMousePositionInWorld() - activeCollider.bounds.center;
                grappleAimDirection = grappleAimDirection.normalized;

                // Search for grapple target
                Physics2D.queriesHitTriggers = true;
                Physics2D.queriesStartInColliders = true;
                RaycastHit2D enemyHit = Physics2D.Raycast(activeCollider.bounds.center, grappleAimDirection, playerUnlocks.grappleDistance, playerPhysicsData.enemyLayer);
                RaycastHit2D terrainHit = Physics2D.Raycast(activeCollider.bounds.center, grappleAimDirection, playerUnlocks.grappleDistance, playerPhysicsData.terrainLayer);
                Physics2D.queriesHitTriggers = detectTriggers;
                Physics2D.queriesStartInColliders = false;
                // Enemy hit detected
                if (enemyHit.collider)
                {
                    grappleAimHitEnemy = true;
                    grappleAimTargetCollider = enemyHit.collider;
                }
                // Terrain hit detected
                else if (terrainHit.collider)
                {
                    grappleAimHitEnemy = false;
                    grappleAimTargetCollider = terrainHit.collider;
                    grappleAimTargetPosition = terrainHit.point;
                }
                // No hits
                else
                {
                    grappleAimTargetCollider = null;
                }

                // Draw indicators
                grappleRangeCircle.transform.localScale = new Vector2(playerUnlocks.grappleDistance * 2, playerUnlocks.grappleDistance * 2);
                grappleArrowLineRenderer.SetPosition(0, new Vector2(activeCollider.bounds.center.x, activeCollider.bounds.center.y) + (grappleAimDirection * playerAnimationData.grappleLineRendererOffset));
                grappleArrowLineRenderer.SetPosition(1, new Vector2(activeCollider.bounds.center.x, activeCollider.bounds.center.y) + (grappleAimDirection * playerUnlocks.grappleDistance));
            }

            // Check for conditions to end grapple aim and start grapple
            if (grappleAiming && !grappleKey)
            {
                // End grapple freeze
                grappleAiming = false;

                // End other movement abilities
                endDash();
                endGlide();

                // Make sure jump flags are set to false
                endedJumpEarly = false;
                canEndJumpEarly = false;

                // Set coyote flags to false
                jumpCoyoteUsable = false;
                wallJumpCoyoteUsable = false;
                dashCoyoteUsable = false;

                // Leave ground
                leavingGround = true;

                // Deactivate indicators
                grappleRangeCircle.SetActive(false);
                grappleArrowLineRenderer.enabled = false;

                // Trigger event action
                onGrappleAim?.Invoke(false);

                // Check if grapple target found
                if (grappleAimTargetCollider != null)
                {
                    // Move grapple target position slightly closer to player
                    if (!grappleAimHitEnemy) grappleAimTargetPosition -= (grappleAimTargetPosition - activeCollider.bounds.center).normalized * playerPhysicsData.grappleTargetOffset;

                    // Set grapple target
                    grappleHitEnemy = grappleAimHitEnemy;
                    grappleTargetCollider = grappleAimTargetCollider;
                    grappleTargetPosition = grappleAimTargetPosition;

                    // Set grappling flag
                    grappling = true;

                    // Set startup velocity
                    velocity = grappleAimDirection * playerPhysicsData.grappleVelocity;

                    // Trigger event action
                    onGrapple?.Invoke(true);
                }
            }

            // Handle grapple
            if (grappling)
            {
                // Target is an enemy, get current enemy position
                if (grappleHitEnemy) grappleTargetPosition = grappleTargetCollider.ClosestPoint(activeCollider.bounds.center);

                // Check if we have reached target position
                if (activeCollider.OverlapPoint(grappleTargetPosition))
                {
                    endGrapple();

                    // Start grapple control loss
                    grappleControlLossTimer = 0;
                    grappleControlLossMultiplier = 0;
                }
                // Move towards target
                else
                {
                    velocity = (grappleTargetPosition - activeCollider.bounds.center).normalized * playerPhysicsData.grappleVelocity;
                }
            }

            // Consume grapple flag
            grappleToConsume = false;
        }

        private void endGrapple()
        {
            if (grappling)
            {
                // Stop grappling
                grappling = false;

                // Trigger event action
                onGrapple?.Invoke(false);
            }
        }

        private void resetGrapple()
        {
            // Reset grapple
            canGrapple = true;
            grappleBufferUsable = true;
        }

        #endregion

        #region Alternate grapple

        private void handleAlternateGrapple()
        {
            bool canUseAlternateGrappleBuffer = alternateGrappleBufferUsable && alternateGrappleBufferTimer < playerPhysicsData.alternateGrappleBufferTime;

            // Check for conditions to initiate grapple aim
            if (playerUnlocks.grappleUnlocked && !alternateGrappleAiming && (alternateGrappleToConsume || canUseAlternateGrappleBuffer) && canAlternateGrapple)
            {
                StartCoroutine(alternateGrappleFreeze());
            }

            // Handle grapple
            if (alternateGrappling)
            {
                // Target is an enemy, get current enemy position
                if (alternateGrappleHitEnemy) alternateGrappleTargetPosition = alternateGrappleTargetCollider.ClosestPoint(activeCollider.bounds.center);

                // Check if we have reached target position
                if (activeCollider.OverlapPoint(alternateGrappleTargetPosition))
                {
                    endAlternateGrapple();

                    // Start grapple control loss
                    alternateGrappleControlLossTimer = 0;
                    alternateGrappleControlLossMultiplier = 0;
                }
                // Move towards target
                else
                {
                    velocity = (alternateGrappleTargetPosition - activeCollider.bounds.center).normalized * playerPhysicsData.alternateGrappleVelocity;
                }
            }

            // Consume grapple flag
            alternateGrappleToConsume = false;
        }

        private IEnumerator alternateGrappleFreeze()
        {
            // Invoke event action
            onGrappleAim?.Invoke(true);

            // Start grapple aim
            alternateGrappleAiming = true;
            alternateGrappleFreezeTimer = 0;

            // Timer to keep track of time scale lerping
            float timeScaleLerpTimer = 0;

            // Activate grapple indicators
            alternateGrappleRangeCircle.SetActive(true);
            alternateGrappleArrowLineRenderer.enabled = true;

            // Loop until grapple end
            while (alternateGrappleKey && alternateGrappleFreezeTimer< playerPhysicsData.alternateGrappleFreezeTime)
            {
                // Get grapple direction
                alternateGrappleAimDirection = InputManager.instance.getMousePositionInWorld() - activeCollider.bounds.center;
                alternateGrappleAimDirection = alternateGrappleAimDirection.normalized;

                // Search for grapple target
                Physics2D.queriesHitTriggers = true;
                Physics2D.queriesStartInColliders = true;
                RaycastHit2D enemyHit = Physics2D.Raycast(activeCollider.bounds.center, alternateGrappleAimDirection, playerUnlocks.grappleDistance, playerPhysicsData.enemyLayer);
                RaycastHit2D terrainHit = Physics2D.Raycast(activeCollider.bounds.center, alternateGrappleAimDirection, playerUnlocks.grappleDistance, playerPhysicsData.terrainLayer);
                Physics2D.queriesHitTriggers = detectTriggers;
                Physics2D.queriesStartInColliders = false;
                // Enemy hit detected
                if (enemyHit.collider)
                {
                    alternateGrappleAimHitEnemy = true;
                    alternateGrappleAimTargetCollider = enemyHit.collider;
                }
                // Terrain hit detected
                else if (terrainHit.collider)
                {
                    alternateGrappleAimHitEnemy = false;
                    alternateGrappleAimTargetCollider = terrainHit.collider;
                    alternateGrappleAimTargetPosition = terrainHit.point;
                }
                // No hits
                else
                {
                    alternateGrappleAimTargetCollider = null;
                }

                // Draw indicators
                alternateGrappleRangeCircle.transform.localScale = new Vector2(playerUnlocks.grappleDistance * 2, playerUnlocks.grappleDistance * 2);
                alternateGrappleArrowLineRenderer.SetPosition(0, new Vector2(activeCollider.bounds.center.x, activeCollider.bounds.center.y) + (alternateGrappleAimDirection * playerAnimationData.alternateGrappleLineRendererOffset));
                alternateGrappleArrowLineRenderer.SetPosition(1, new Vector2(activeCollider.bounds.center.x, activeCollider.bounds.center.y) + (alternateGrappleAimDirection * playerUnlocks.grappleDistance));

                // Check for "fixed update"
                if (timeScaleLerpTimer > playerPhysicsData.timeScaleLerpTime)
                {
                    // Slow time
                    Time.timeScale = Mathf.Lerp(Time.timeScale, 0, playerPhysicsData.timeScaleLerpFactor);

                    // Decrement timer
                    timeScaleLerpTimer -= playerPhysicsData.timeScaleLerpTime;
                }

                // Increment timers
                alternateGrappleFreezeTimer += Time.unscaledDeltaTime;
                timeScaleLerpTimer += Time.unscaledDeltaTime;

                yield return new WaitForEndOfFrame();
            }

            // End grapple freeze
            alternateGrappleAiming = false;
            Time.timeScale = 1;

            // End other movement abilities
            endDash();
            endGlide();

            // Make sure jump flags are set to false
            endedJumpEarly = false;
            canEndJumpEarly = false;

            // Set coyote flags to false
            jumpCoyoteUsable = false;
            wallJumpCoyoteUsable = false;
            dashCoyoteUsable = false;

            // Deactivate indicators
            alternateGrappleRangeCircle.SetActive(false);
            alternateGrappleArrowLineRenderer.enabled = false;

            // Trigger event action
            onGrappleAim?.Invoke(false);

            // Check if grapple target found
            if (alternateGrappleAimTargetCollider != null)
            {
                // Move grapple target position slightly closer to player
                if (!alternateGrappleAimHitEnemy) alternateGrappleAimTargetPosition -= (alternateGrappleAimTargetPosition - activeCollider.bounds.center).normalized * playerPhysicsData.alternateGrappleTargetOffset;

                // Set grapple target
                alternateGrappleHitEnemy = alternateGrappleAimHitEnemy;
                alternateGrappleTargetCollider = alternateGrappleAimTargetCollider;
                alternateGrappleTargetPosition = alternateGrappleAimTargetPosition;

                // Set grappling flag
                alternateGrappling = true;

                // Set startup velocity
                velocity = alternateGrappleAimDirection * playerPhysicsData.alternateGrappleVelocity;

                // Trigger event action
                onGrapple?.Invoke(true);
            }
        }

        private void endAlternateGrapple()
        {
            if (alternateGrappling)
            {
                // Stop grappling
                alternateGrappling = false;

                // Trigger event action
                onGrapple?.Invoke(false);
            }
        }

        private void resetAlternateGrapple()
        {
            // Reset grapple
            canAlternateGrapple = true;
            alternateGrappleBufferUsable = true;
        }

        #endregion

        private void move()
        {
            // Check if player has control
            if (!hasControl) return;

            // Apply velocity to rigidbody
            rigidbody.velocity = velocity + externalVelocity;

            // Decay external velocity
            externalVelocity = Vector2.MoveTowards(externalVelocity, Vector2.zero, playerPhysicsData.externalVelocityDecay * Time.fixedDeltaTime);
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
            // Switch states
            if      (dashing)            playerState = PlayerState.DASH;
            else if (!onGround)          playerState = PlayerState.AIRBORNE;
            else if (playerInput.x != 0) playerState = PlayerState.RUN;
        }

        private void runState()
        {
            // Switch states
            if      (dashing)         playerState = PlayerState.DASH;
            else if (!onGround)       playerState = PlayerState.AIRBORNE;
            else if (velocity.x == 0) playerState = PlayerState.IDLE;
        }

        private void airborneState()
        {
            // Switch states
            if      (dashing)                     playerState = PlayerState.DASH;
            else if (gliding)                     playerState = PlayerState.GLIDE;
            else if (onGround && velocity.x == 0) playerState = PlayerState.IDLE;
            else if (onGround)                    playerState = PlayerState.RUN;
            else if (onWall)                      playerState = PlayerState.ON_WALL;
        }

        private void onWallState()
        {
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
            // Switch states
            if      (climbingLedge)        playerState = PlayerState.CLIMB_LEDGE;
            else if (dashing)              playerState = PlayerState.DASH;
            else if (!onLedge && onGround) playerState = PlayerState.IDLE;
            else if (!onLedge && onWall)   playerState = PlayerState.ON_WALL;
            else if (!onLedge)             playerState = PlayerState.AIRBORNE;
        }

        private void climbLedgeState()
        {
            // Switch states
            if (!climbingLedge) playerState = PlayerState.IDLE;
        }

        private void dashState()
        {
            // Switch states
            if      (!dashing && onGround) playerState = PlayerState.RUN;
            else if (!dashing && onWall)   playerState = PlayerState.ON_WALL;
            else if (!dashing)             playerState = PlayerState.AIRBORNE;
        }

        private void glideState()
        {
            // Switch states
            if      (dashing)  playerState = PlayerState.DASH;
            else if (onGround) playerState = PlayerState.IDLE;
            else if (onWall)   playerState = PlayerState.ON_WALL;
            else if (!gliding) playerState = PlayerState.AIRBORNE;
        }

        #endregion

        #region Collision callbacks

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "Platform")
            {
                currentPlatform = collision.gameObject;
                collidingWithPlatform = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.tag == "Platform")
            {
                collidingWithPlatform = false;
            }
        }

        #endregion
    }
}