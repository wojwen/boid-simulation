using TMPro;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Displays current FPS.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        /// <summary>Text where the FPS should be displayed.</summary>
        [SerializeField] private TextMeshProUGUI fpsText;

        /// <summary>Refresh interval of he FPS text.</summary>
        [SerializeField] private float refreshInterval;

        /// <summary>The time of next refresh.</summary>
        private float _timer;

        /// <summary>
        /// Updates FPS timer.
        /// </summary>
        private void Update()
        {
            if (!(Time.unscaledTime > _timer)) return;

            var fps = (int)(1f / Time.unscaledDeltaTime);
            fpsText.text = fps + " FPS";
            _timer = Time.unscaledTime + refreshInterval;
        }
    }
}