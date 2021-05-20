using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] private float speed;
    [Range(0, 1), SerializeField] private float rotationSmoothing;
    [SerializeField] private float pullStrength;
    [SerializeField] private int jumpForce;

    private Controls controls;
    private Vector2 moveInputDirection;
    private CharacterController controller;
    private Camera mainCam;
    private bool wasGroundedLastFrame;
    private bool isJumpPressed;
    private Vector3 velocity;

    private void OnEnable()
    {
        controls = new Controls();
        controls.Enable();
        controls.Main.Move.performed += OnMovePerformed;
        controls.Main.Move.canceled += OnMoveCanceled;
        controls.Main.Jump.performed += OnJumpPerformed;
    }

    private void OnMoveCanceled(InputAction.CallbackContext obj)
    {
        moveInputDirection = Vector2.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext obj)
    {
        isJumpPressed = true;
    }

    private void OnMovePerformed(InputAction.CallbackContext obj)
    {
        moveInputDirection = obj.ReadValue<Vector2>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCam = Camera.main;
    }

    // Update is called once per frame
    private void Update()
    {
        var moveDir = Vector3.zero;
        if (moveInputDirection.sqrMagnitude >= 0.1f)
        {
            var targetRotation = GetTargetRotation();
            transform.rotation = targetRotation;
            moveDir = GetLateralMovement(targetRotation);
        }

        moveDir += GetGravity();
        moveDir += GetJumpForce();
        controller.Move(moveDir * Time.deltaTime);
        velocity = moveDir;
    }

    private Vector3 GetLateralMovement(Quaternion rotation)
    {
        var directionToMove = rotation * Vector3.forward;
        return directionToMove * speed;
    }

    private Vector3 GetGravity()
    {
        if (controller.isGrounded)
            return new Vector3(0, -pullStrength, 0);
        if (wasGroundedLastFrame)
            return Vector3.zero;
        return new Vector3(0, velocity.y, 0) + Physics.gravity * Time.deltaTime;
    }

    private Vector3 GetJumpForce()
    {
        if (controller.isGrounded && isJumpPressed)
        {
            var verticalForce = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            isJumpPressed = false;
            return new Vector3(0, verticalForce, 0);
        }

        return Vector3.zero;
    }

    private Quaternion GetTargetRotation()
    {
        var mainCamAngle = mainCam.transform.eulerAngles.y;
        var targetAngleRad = Mathf.Atan2(moveInputDirection.x, moveInputDirection.y);
        var targetAngleDegrees = targetAngleRad * Mathf.Rad2Deg + mainCamAngle;
        var targetRotation = Quaternion.Euler(0, targetAngleDegrees, 0);
        return Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing);
    }

    private void LateUpdate()
    {
        wasGroundedLastFrame = controller.isGrounded;
    }
}
