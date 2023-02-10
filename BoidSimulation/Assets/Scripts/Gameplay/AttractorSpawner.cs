using System.Collections;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Component responsible for spawning positive and negative attractors at random positions.
    /// </summary>
    public class AttractorSpawner : MonoBehaviour
    {
        /// <summary>The area within which the attractors are randomly spawned.</summary>
        [SerializeField] private Vector2 spawnArea;

        /// <summary>The time interval between two successive attractor spawns.</summary>
        [SerializeField] private float spawnInterval;

        /// <summary>The GameObject for the positive attractor.</summary>
        [SerializeField] private GameObject positiveAttractor;

        /// <summary>The GameObject for the negative attractor.</summary>
        [SerializeField] private GameObject negativeAttractor;

        /// <summary>
        /// Starts spawning attractors.
        /// </summary>
        private void Start()
        {
            StartCoroutine(SpawnAttractors());
        }

        /// <summary>
        /// Coroutine that continuously spawns either a positive or negative attractor at random positions.
        /// </summary>
        private IEnumerator SpawnAttractors()
        {
            while (true)
            {
                var toSpawn = Random.Range(0, 2) == 1 ? positiveAttractor : negativeAttractor;
                var spawnPosition = new Vector3(Random.Range(0f, spawnArea.x), transform.position.y,
                    Random.Range(0f, spawnArea.y));

                Instantiate(toSpawn, spawnPosition, Quaternion.identity);

                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }
}