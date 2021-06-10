using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] private float speed;

    // L’attribut Range permet de restreindre les valeurs possibles entre 2 valeurs.
    // Ici rotationSmoothing sera toujours comprise entre 0 et 1
    [Range(0, 1), SerializeField] private float rotationSmoothing;

    // La gravité doit être augmentée quand on est sur une pente, en l’occurence on se déplace de pull strength sur Y
    [SerializeField] private float pullStrength;
    [SerializeField] private int jumpForce;

    private Controls controls;

    // Cette variable contient la position du stick directionnel
    private Vector2 moveInputDirection;
    private bool wasGroundedLastFrame;
    private bool isJumpPressed;
    private bool isAiming;

    private CharacterController controller;
    private Camera mainCam;
    private Animator animator;

    private Vector3 positionDelta;

    private void OnEnable()
    {
        controls = new Controls();
        controls.Enable();
        controls.Main.Move.performed += OnMovePerformed;
        controls.Main.Move.canceled += OnMoveCanceled;
        controls.Main.Jump.performed += OnJumpPerformed;
        controls.Main.Jump.canceled += OnJumpCanceled;
        controls.Main.Aim.performed += OnAimPerformed;
        controls.Main.Aim.canceled += OnAimCanceled;
    }

    private void OnAimCanceled(InputAction.CallbackContext obj)
    {
        isAiming = false;
        animator.SetBool("IsAiming", false);
    }

    private void OnAimPerformed(InputAction.CallbackContext obj)
    {
        isAiming = true;
        animator.SetBool("IsAiming", true);
    }

    private void OnMoveCanceled(InputAction.CallbackContext obj)
    {
        moveInputDirection = Vector2.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext obj)
    {
        if (controller.isGrounded)
            isJumpPressed = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext obj)
    {
        isJumpPressed = false;
    }

    private void OnMovePerformed(InputAction.CallbackContext obj)
    {
        moveInputDirection = obj.ReadValue<Vector2>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCam = Camera.main;
    }

    // Update is called once per frame
    private void Update()
    {
        var oldPosition = transform.position;
        var moveDir = Vector3.zero;
        /*
        D’après Pythagore, la magnitude d’un vecteur se calcule de la manière suivante :
        racine carrée de (x au carré + y au carré)
        Ainsi la magnitude au carré se calcule :
        x au carré + y au carré
        On utilise donc la magnitude au carré pour éviter de faire calculer la racine carrée à chaque frame
        */
        if (moveInputDirection.sqrMagnitude >= 0.1f || isAiming)
        {
            var targetRotation = GetTargetRotation();
            transform.rotation = targetRotation;
            moveDir = GetLateralMovement(targetRotation);
        }

        moveDir += GetGravity();
        moveDir += GetJumpForce();
        wasGroundedLastFrame = controller.isGrounded;
        controller.Move(moveDir * Time.deltaTime);
        animator.SetFloat("ForwardSpeed", moveInputDirection.y);
        animator.SetFloat("LateralSpeed", moveInputDirection.x);
        positionDelta = transform.position - oldPosition;
    }

    /// <summary>
    /// Remplacer "void" par Quaternion nous oblige à utiliser le mot clef "return" afin de renvoyer une valeur
    /// Cette valeur peut ensuite être enregistrée dans une variable dans une autre fonction
    /// </summary>
    /// <returns></returns>
    private Quaternion GetTargetRotation()
    {
        // On récupère l’angle en degrés sur l’axe y de la camera pas virtuelle
        var mainCamAngle = mainCam.transform.eulerAngles.y;
        // Atan2(x,y) permet d’obtenir l’angle entre Vector2.up et le vecteur donné
        // À l’inverse, Atan2(y,x) permet d’obtenir l’angle entre Vector2.right et le vecteur donné
        var stickAngleRad = Mathf.Atan2(moveInputDirection.x, moveInputDirection.y);
        // Atan2 renvoie une valeur en radians, on la convertit en degrés
        var stickAngleDeg = stickAngleRad * Mathf.Rad2Deg;
        // L’angle en degré souhaité pour l’avatar est l’angle du stick + l’angle de la caméra
        var targetAngle = stickAngleDeg + mainCamAngle;
        // On transforme un angle sur l’axe y en rotation
        var targetRotation = Quaternion.Euler(0, targetAngle, 0);
        // On renvoit une valeur smoothée entre la rotation actuelle et la rotation calculée
        return Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing);
    }

    private Vector3 GetLateralMovement(Quaternion rotation)
    {
        if (moveInputDirection.sqrMagnitude == 0)
            return Vector3.zero;
        // En multipliant un quaternion par une direction, on peut orienter un axe dans la direction voulue
        // Ici on modifie Vector3.forward (soit un vector3(0,0,1)) pour l’orienter selon la rotation de l’avatar
        var directionToMove = rotation * Vector3.forward;
        return directionToMove * speed;
    }

    private Vector3 GetGravity()
    {
        if (controller.isGrounded && !isJumpPressed)
            return new Vector3(0, -pullStrength, 0);
        if (wasGroundedLastFrame && controller.velocity.y < 0)
            return Vector3.zero;
        return new Vector3(0, controller.velocity.y, 0) + Physics.gravity * Time.deltaTime;
    }

    private Vector3 GetJumpForce()
    {
        if (controller.isGrounded && isJumpPressed)
        {
            // La force verticale nécessaire pour atteindre une hauteur donnée est :
            // racine carrée de (hauteurVoulue * -2 * gravité)
            var verticalForce = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            isJumpPressed = false;
            return new Vector3(0, verticalForce, 0);
        }

        return Vector3.zero;
    }
}
