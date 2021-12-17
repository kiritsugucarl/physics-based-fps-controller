using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Assignables")]
    public Transform playerCam;
    public Transform orientation;

    //Others
    private Rigidbody rb;

    //Rotation and Look
    private float xRotation;
    private float sensitivity = 50f; //mouse sensitivity
    private float sensMultiplier = 1f;

    [Header("Movement Attributes")]
    public float moveSpeed;
    public float maxSpeed;
    public bool grounded; //to check if the player is grounded
    public LayerMask whatIsGround; //what is ground duh

    [Header("Additional Physics Attributes")]
    public float counterMovement;
    private float threshold;
    public float maxSlopeAngle;

    //Crouch and Slide
    [Header("Crouch and Slide")]
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce;
    public float slideCounterMovement;

    //Jumping
    [Header("Jumping")]
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce;

    //INPUTS
    float x, y;
    bool jumping, sprinting, crouching;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    //Wall Running
    [Header("Wall Running Properties")]
    public LayerMask whatIsWall;
    public float wallrunForce;
    public float maxWallRunTime;
    public float maxWallSpeed;
    bool isWallRight;
    bool isWallLeft;
    bool isWallRunning;
    public float maxWallRunCameraTilt;
    public float wallRunCameraTilt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // to put rigidbody
    }

    void Start()
    {
        playerScale = transform.localScale; // to store current scale
        Cursor.lockState = CursorLockMode.Locked; // for the cursor be locked inside
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        PlayerInput();
        Look();
        CheckForWall();
        WallRunInput();
    }

    //Find user input
    private void PlayerInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.LeftControl);

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartCrouch();
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            StopCrouch();
        }
    }

    private void StartCrouch()
    {
        transform.localScale = crouchScale; //change scale
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z); //make player little
        if (rb.velocity.magnitude > 0.5f)
        {
            if (grounded)
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = playerScale; //bring back original scale
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        //Additional Gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x;
        float yMag = mag.y;

        //Counterforce sliding and movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then the player can jump
        if (readyToJump && jumping)
        {
            Jump();
        }

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //if sliding down a ramp, add force down so player stays grounded and add speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //speed limiter
        if (x > 0 && xMag > maxSpeed)
        {
            x = 0;
        }
        if (x < 0 && xMag < -maxSpeed)
        {
            x = 0;
        }
        if (y > 0 && yMag > maxSpeed)
        {
            y = 0;
        }
        if (y < 0 && yMag < -maxSpeed)
        {
            y = 0;
        }

        //Some multipliers
        float multiplier = 1f;
        float multiplierV = 1f;

        //Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        //Movement while sliding
        if (grounded && crouching)
        {
            multiplierV = 0f;
        }

        //Apply forces to move the player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            //Go jumping
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
            {
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            }
            else if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
            }

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (isWallRunning)
        {
            readyToJump = false; //u cant jump when wallrunning

            if (isWallLeft && !Input.GetKey(KeyCode.D) || isWallRight && !Input.GetKey(KeyCode.A)) //as long as they stick on the wall do :
            {
                rb.AddForce(Vector2.up * jumpForce * 1.5f);
                rb.AddForce(normalVector * jumpForce * 0.5f);
            }

            if (isWallRight || isWallLeft && Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) //if the player want to move up, hold down the keys
            {
                rb.AddForce(-orientation.up * jumpForce * 1f);
            }
            if (isWallRight && Input.GetKey(KeyCode.A))
            {
                rb.AddForce(-orientation.right * jumpForce * 3.2f);
            }
            if (isWallLeft && Input.GetKey(KeyCode.D))
            {
                rb.AddForce(orientation.right * jumpForce * 3.2f);
            }

            rb.AddForce(orientation.forward * jumpForce * 1f);

            rb.velocity = Vector3.zero;

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true; //make the jumping ready again
    }

    private float desiredX;

    //mouse stuff
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find where the player looks
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we cannot over rotate
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, wallRunCameraTilt);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);

        //cam tilt
        if (Math.Abs(wallRunCameraTilt) < maxWallRunCameraTilt && isWallRunning && isWallRight)
        {
            wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * 2;
        }
        if (Math.Abs(wallRunCameraTilt) < maxWallRunCameraTilt && isWallRunning && isWallLeft)
        {
            wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * 2;
        }

        //put the camera back
        if (wallRunCameraTilt > 0 && !isWallRight && !isWallLeft)
        {
            wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * 2;
        }
        if (wallRunCameraTilt < 0 && !isWallRight && !isWallLeft)
        {
            wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * 2;
        }
    }

    private Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    //Ground Detection
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    //stop being grounded
    private void StopGrounded()
    {
        grounded = false;
    }

    //Checking for the movement during wallrun
    private void WallRunInput()
    {
        if (Input.GetKey(KeyCode.D) && isWallRight)
        {
            StartWallRun();
        }
        if (Input.GetKey(KeyCode.A) && isWallLeft)
        {
            StartWallRun();
        }
    }

    private void StartWallRun()
    {
        rb.useGravity = false;
        isWallRunning = true;

        if (rb.velocity.magnitude <= maxWallSpeed)
        {
            rb.AddForce(orientation.forward * wallrunForce * Time.deltaTime);

            if (isWallRight)
            {
                rb.AddForce(orientation.right * wallrunForce / 5 * Time.deltaTime);
            }
            else
            {
                rb.AddForce(-orientation.right * wallrunForce / 5 * Time.deltaTime);
            }
        }
    }

    private void StopWallRun()
    {
        rb.useGravity = true;
        isWallRunning = false;
    }

    private void CheckForWall()
    {
        isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, whatIsWall);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, whatIsWall);

        if (!isWallLeft && !isWallRight)
        {
            StopWallRun();
        }
    }
}
