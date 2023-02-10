using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// Component responsible for handling the camera and movement.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        /// <summary>Rigidbody component attached to the player object.</summary>
        [SerializeField] private Rigidbody playerRigidbody;

        /// <summary>Transform component of the player's camera.</summary>
        [SerializeField] private Transform playerCamera;

        /// <summary>InputActionProperty for reading player rotation input.</summary>
        [SerializeField] private InputActionProperty lookAction;

        /// <summary>InputActionProperty for reading player movement input.</summary>
        [SerializeField] private InputActionProperty moveAction;

        /// <summary>Force applied to the player for movement.</summary>
        [SerializeField] private float moveForce;

        /// <summary>Sensitivity of the look input.</summary>
        [SerializeField] private Vector2 sensitivity;

        /// <summary>Maximum rotation angle for the vertical rotation of the player camera.</summary>
        [SerializeField] private float verticalClamp;

        /// <summary>Accumulated vertical rotation delta.</summary>
        private float _verticalRotationDelta;

        /// <summary>
        /// Handles look and movement input.
        /// </summary>
        private void FixedUpdate()
        {
            HandleLookInput();
            HandleMovementInput();
        }

        /// <summary>
        /// Enables the look and movement actions.
        /// </summary>
        private void OnEnable()
        {
            lookAction.action.Enable();
            moveAction.action.Enable();
        }

        /// <summary>
        /// Disables the look and movement actions.
        /// </summary>
        private void OnDestroy()
        {
            lookAction.action.Disable();
            moveAction.action.Disable();
        }

        /// <summary>
        /// Handles the look input by rotating the player's rigidbody and camera.
        /// </summary>
        private void HandleLookInput()
        {
            // rotate player
            var lookInput = lookAction.action.ReadValue<Vector2>();
            var rotationDelta = new Vector2(lookInput.x * sensitivity.x, lookInput.y * sensitivity.y) *
                                Time.fixedDeltaTime;
            var horizontalRotationDelta = Quaternion.AngleAxis(rotationDelta.x, Vector3.up);
            playerRigidbody.MoveRotation(playerRigidbody.rotation * horizontalRotationDelta);

            // rotate camera
            _verticalRotationDelta -= rotationDelta.y;
            _verticalRotationDelta = Mathf.Clamp(_verticalRotationDelta, -verticalClamp, verticalClamp);
            var targetRotation = transform.eulerAngles;
            targetRotation.x = _verticalRotationDelta;
            playerCamera.eulerAngles = targetRotation;
        }

        /// <summary>
        /// Handles the movement input by applying a force to the player's rigidbody in the direction of the camera.
        /// </summary>
        private void HandleMovementInput()
        {
            var movementInput = moveAction.action.ReadValue<Vector3>();
            var positionDelta = movementInput * Time.fixedDeltaTime;
            playerRigidbody.AddForce(playerCamera.TransformDirection(positionDelta * moveForce));
        }
    }
}