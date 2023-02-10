using Simulation.Interactive;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Glowing sphere which attracts or scares Boids and changes it's intensity over time.
    /// </summary>
    public class GlowingSphere : MonoBehaviour
    {
        /// <summary>The identifier for the emission color property in the shader.</summary>
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>Curve representing the intensity of the sphere over time.</summary>
        [SerializeField] private AnimationCurve intensityCurve;

        /// <summary>Life span of the sphere in seconds.</summary>
        [SerializeField] private float lifeSpan;

        /// <summary>Renderer component of the sphere.</summary>
        [SerializeField] private Renderer ballRenderer;

        /// <summary>Light component used for lighting around the sphere.</summary>
        [SerializeField] private Light pointLight;

        /// <summary>Boid attractor component used for attracting or scaring Boids.</summary>
        [SerializeField] private BoidAttractor boidAttractor;

        /// <summary>Color of the sphere and its light.</summary>
        [SerializeField] private Color color;

        /// <summary>Multiplier used for light intensity.</summary>
        [SerializeField] private float lightIntensityMultiplier;

        /// <summary>Multiplier used for attractor strength.</summary>
        [SerializeField] private float attractorStrengthMultiplier;

        /// <summary>Multiplier used for emission intensity.</summary>
        [SerializeField] private float emissionIntensityMultiplier;

        /// <summary>MaterialPropertyBlock for changing emission color of the sphere.</summary>
        private MaterialPropertyBlock _materialPropertyBlock;

        /// <summary>Time the sphere was created.</summary>
        private float _timeCreated;

        /// <summary>
        /// Initializes variables and light color.
        /// </summary>
        private void Start()
        {
            _timeCreated = Time.realtimeSinceStartup;
            _materialPropertyBlock = new MaterialPropertyBlock();
            pointLight.color = color;
        }

        /// <summary>
        /// Updates the appearance and attraction strength of the sphere based on its intensity over time. Destroys the
        /// sphere when the intensity becomes 0.
        /// </summary>
        private void Update()
        {
            var intensity = intensityCurve.Evaluate((Time.realtimeSinceStartup - _timeCreated) / lifeSpan);

            if (intensity == 0)
                Destroy(gameObject);

            pointLight.intensity = intensity * lightIntensityMultiplier;

            _materialPropertyBlock.SetColor(EmissionColor, color * intensity * emissionIntensityMultiplier);
            ballRenderer.SetPropertyBlock(_materialPropertyBlock);
            boidAttractor.SetStrength(intensity * attractorStrengthMultiplier);

            transform.localScale = Vector3.one * intensity;
        }
    }
}