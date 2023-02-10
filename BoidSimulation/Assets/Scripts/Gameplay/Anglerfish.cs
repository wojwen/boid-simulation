using Simulation.Interactive;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Component representing an anglerfish which has to attract and eat fish to survive.
    /// </summary>
    public class Anglerfish : MonoBehaviour
    {
        /// <summary>The identifier for the emission color property in the shader.</summary>
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>Attractor representing the anglerfish light.</summary>
        [SerializeField] private BoidAttractor lightAttractor;

        /// <summary>Attractor representing the anglerfish mouth.</summary>
        [SerializeField] private BoidAttractor mouthAttractor;

        /// <summary>Renderer of the anglerfish light.</summary>
        [SerializeField] private Renderer lightRenderer;

        /// <summary>Point light of the anglerfish light.</summary>
        [SerializeField] private Light pointLight;

        /// <summary>Color of the anglerfish light.</summary>
        [SerializeField] private Color lightColor;

        /// <summary>Mouth of the anglerfish which can consume other fish.</summary>
        [SerializeField] private BoidEater anglerfishMouth;

        /// <summary>Rigidbody of the anglerfish.</summary>
        [SerializeField] private Rigidbody anglerfishRigidbody;

        /// <summary>Audio source for playing sounds when a fish is eaten.</summary>
        [SerializeField] private AudioSource audioSource;

        /// <summary>Sound played every time a fish is eaten.</summary>
        [SerializeField] private AudioClip eatSound;

        /// <summary>Maximum health of the anglerfish. This is also the starting amount.</summary>
        [SerializeField] private float maxHealth;

        /// <summary>Multiplier for the attraction strength of anglerfish light.</summary>
        [SerializeField] private float lightAttractionStrengthMultiplier;

        /// <summary>Multiplier for the attraction strength of anglerfish mouth.</summary>
        [SerializeField] private float mouthAttractionStrengthMultiplier;

        /// <summary>Multiplier for the intensity of anglerfish light.</summary>
        [SerializeField] private float lightIntensityMultiplier;

        /// <summary>Starting health decrease speed, per second. Increases to the max value over time.</summary>
        [SerializeField] private float initialHealthDecreaseSpeed;

        /// <summary>Maximum health decrease speed, per second.</summary>
        [SerializeField] private float maxHealthDecreaseSpeed;

        /// <summary>Time after which the maximum health decrease speed is archived.</summary>
        [SerializeField] private float maxHealthDecreaseSpeedTime;

        /// <summary>Amount of health gained by eating a fish.</summary>
        [SerializeField] private float fishHealthBonus;

        /// <summary>Minimum velocity at which the light attracts fish.</summary>
        [SerializeField] private float minimumAttractVelocity;

        /// <summary>Current health. When it falls to 0 the game is over.</summary>
        private float _health;

        /// <summary>MaterialPropertyBlock for changing emission color of the sphere.</summary>
        private MaterialPropertyBlock _materialPropertyBlock;

        /// <summary>Real time since startup when the anglerfish was created.</summary>
        private float _startTime;

        /// <summary>Current health decrease speed.</summary>
        private float HealthDecreaseSpeed => Mathf.Lerp(initialHealthDecreaseSpeed, maxHealthDecreaseSpeed,
            (Time.realtimeSinceStartup - _startTime) / maxHealthDecreaseSpeedTime);

        /// <summary>Current health percentage, between 0 and 1.</summary>
        public float HealthPercentage => _health / maxHealth;

        /// <summary>Current score.</summary>
        public int Score => anglerfishMouth.Score;


        /// <summary>
        /// Initializes variables and sets light color.
        /// </summary>
        private void Start()
        {
            _startTime = Time.realtimeSinceStartup;
            _health = maxHealth;
            _materialPropertyBlock = new MaterialPropertyBlock();
            pointLight.color = lightColor;
        }

        /// <summary>
        /// Changes light intensity and controls health.
        /// </summary>
        private void Update()
        {
            ChangeLightIntensity();
            DecreaseHealth();
            CheckIfAlive();
        }

        /// <summary>
        /// Subscribes to score increased event.
        /// </summary>
        private void OnEnable()
        {
            anglerfishMouth.ScoreIncreased += ScoreIncreasedHandler;
        }

        /// <summary>
        /// Unsubscribes from score increased event.
        /// </summary>
        private void OnDisable()
        {
            anglerfishMouth.ScoreIncreased -= ScoreIncreasedHandler;
        }

        /// <summary>
        /// Changes light and emission intensity and attraction force based on the velocity.
        /// </summary>
        private void ChangeLightIntensity()
        {
            // calculate base intensity and set attractor intensity
            var velocity = anglerfishRigidbody.velocity.magnitude;
            float intensity;
            if (velocity < minimumAttractVelocity)
            {
                intensity = 1f - velocity / minimumAttractVelocity;
                lightAttractor.SetStrength(intensity * lightAttractionStrengthMultiplier);
                mouthAttractor.SetStrength(0f);
            }
            else
            {
                intensity = minimumAttractVelocity - velocity;
                mouthAttractor.SetStrength(intensity * mouthAttractionStrengthMultiplier);
                lightAttractor.SetStrength(0f);
            }

            // set intensity for renderer and light
            var lightIntensity = Mathf.Clamp01(intensity) * lightIntensityMultiplier;
            _materialPropertyBlock.SetColor(EmissionColor, lightColor * lightIntensity);
            lightRenderer.SetPropertyBlock(_materialPropertyBlock);
            pointLight.intensity = lightIntensity;
        }

        /// <summary>
        /// Decreases health over time.
        /// </summary>
        private void DecreaseHealth()
        {
            _health -= HealthDecreaseSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Checks if the fish is alive and if not ends the game.
        /// </summary>
        private void CheckIfAlive()
        {
            if (_health <= 0)
            {
                ApplicationManager.Instance.GameEnded(anglerfishMouth.Score);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Increases health based on the score.
        /// </summary>
        /// <param name="increase">Amount the score increased.</param>
        private void ScoreIncreasedHandler(int increase)
        {
            _health = Mathf.Min(maxHealth, _health + fishHealthBonus * increase);
            audioSource.PlayOneShot(eatSound);
        }
    }
}