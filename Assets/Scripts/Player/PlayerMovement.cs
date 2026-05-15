using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //Declare variables
    private CharacterController controller;
    public float sidewaySpeed = 10f;
    public float forwardSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    //Ground variables
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;

    //Vector variable
    private float velocity;
    public LayerMask Ground;

    private bool canJump = true;
    public bool DidJumpThisFrame { get; private set; }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        EnsureFootGroundCheck();
    }

    /// <summary>
    /// Scene had groundCheck wired to the Ground platform — that made isGrounded always true.
    /// </summary>
    private void EnsureFootGroundCheck()
    {
        if (groundCheck != null && groundCheck.IsChildOf(transform))
            return;

        var existing = transform.Find("GroundCheck");
        if (existing != null)
        {
            groundCheck = existing;
            return;
        }

        var feet = new GameObject("GroundCheck");
        feet.transform.SetParent(transform, false);
        float footY = controller != null
            ? controller.center.y - controller.height * 0.5f + 0.05f
            : -0.95f;
        feet.transform.localPosition = new Vector3(0f, footY, 0f);
        groundCheck = feet.transform;
    }

    private void UpdateGrounded()
    {
        isGrounded = controller.isGrounded;

        if (groundCheck != null && groundCheck.IsChildOf(transform))
        {
            isGrounded |= Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);
        }
    }

    void Update()
    {
        DidJumpThisFrame = false;

        if (GameManager.GameStarted)
            UICursor.LockForGameplay();

        groundMask = Ground;
        UpdateGrounded();

        if (controller.isGrounded && velocity < 0f)
        {
            velocity = -2f;
            canJump = true;
        }

        float horizontal = Input.GetAxis("Horizontal");
        Vector3 move = new Vector3(horizontal * sidewaySpeed, 0, forwardSpeed);

        if (Input.GetButtonDown("Jump") && canJump && isGrounded)
        {
            velocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            canJump = false;
            DidJumpThisFrame = true;
        }

        velocity += gravity * Time.deltaTime;
        move.y = velocity;
        controller.Move(move * Time.deltaTime);
    }
}