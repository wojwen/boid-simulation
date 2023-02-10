using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// Component responsible for letting the player spawn positive and negative attractors.
    /// </summary>
    public class PlayerAttractorSpawner : MonoBehaviour
    {
        /// <summary>Point at which the attractors are going to be spawned.</summary>
        [SerializeField] private Transform spawnPoint;

        /// <summary>Input action for spawning a positive attractor.</summary>
        [SerializeField] private InputActionProperty spawnPositiveAttractorAction;

        /// <summary>Input action for spawning a negative attractor.</summary>
        [SerializeField] private InputActionProperty spawnNegativeAttractorAction;

        /// <summary>Positive attractor prefab.</summary>
        [SerializeField] private GameObject positiveAttractor;

        /// <summary>Negative attractor prefab.</summary>
        [SerializeField] private GameObject negativeAttractor;

        /// <summary>Force applied to the attractors after spawning them.</summary>
        [SerializeField] private float pushForce;

        /// <summary>Minimum time interval between spawns.</summary>
        [SerializeField] private float cooldownTime;

        /// <summary>Time since startup of the last spawn.</summary>
        private float _lastSpawnTime;

        /// <summary>
        /// Enables input actions and registers event listeners.
        /// </summary>
        private void OnEnable()
        {
            spawnPositiveAttractorAction.action.Enable();
            spawnNegativeAttractorAction.action.Enable();
            spawnPositiveAttractorAction.action.performed += _ => SpawnAttractor(true);
            spawnNegativeAttractorAction.action.performed += _ => SpawnAttractor(false);
        }

        /// <summary>
        /// Disables input actions.
        /// </summary>
        private void OnDestroy()
        {
            spawnPositiveAttractorAction.action.Disable();
            spawnNegativeAttractorAction.action.Disable();
        }

        /// <summary>
        /// Spawns either a positive or negative attractor and applies a force to it.
        /// </summary>
        /// <param name="positive">Flag showing whether the attractor should be positive or negative.</param>
        private void SpawnAttractor(bool positive)
        {
            if (Time.realtimeSinceStartup - _lastSpawnTime < cooldownTime) return; // check if enough time passed

            var toSpawn = positive ? positiveAttractor : negativeAttractor;
            var attractor = Instantiate(toSpawn, spawnPoint.position, Quaternion.identity);
            attractor.GetComponent<Rigidbody>().AddForce(spawnPoint.forward * pushForce);

            _lastSpawnTime = Time.realtimeSinceStartup; // save spawn time
        }
    }
}