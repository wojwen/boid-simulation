using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// Enables switching modes by pressing a button.
    /// </summary>
    public class ModeSwitcher : MonoBehaviour
    {
        /// <summary>Action which switches modes.</summary>
        [SerializeField] private InputActionProperty switchModeAction;

        /// <summary>
        /// Enables action and registers a callback method.
        /// </summary>
        private void OnEnable()
        {
            switchModeAction.action.Enable();
            switchModeAction.action.performed += _ => ApplicationManager.Instance.SwitchMode();
        }
    }
}