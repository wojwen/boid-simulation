using UnityEngine;
using UnityEngine.InputSystem;

namespace Utils
{
    /// <summary>
    /// This class provides helper methods for controlling the visibility and lock state of the mouse cursor. 
    /// </summary>
    public class CursorHelper : MonoBehaviour
    {
        /// <summary>Action used to toggle the cursor's visibility and lock state.</summary>
        [SerializeField] private InputActionProperty toggleAction;


        /// <summary>
        /// Enables the input action and registers a callback method.
        /// </summary>
        private void OnEnable()
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += _ => ToggleCursor();
        }

        /// <summary>
        /// Toggles the visibility and lock state of the mouse cursor.
        /// </summary>
        private static void ToggleCursor()
        {
            if (Cursor.visible)
                HideCursor();
            else
                ShowCursor();
        }

        /// <summary>
        /// Makes the mouse cursor visible and unlocked.
        /// </summary>
        public static void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Makes the mouse cursor invisible and locked.
        /// </summary>
        public static void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}