using Simulation.Core;
using UnityEngine;

namespace Simulation.Interactive
{
    /// <summary>
    /// Sphere which consumes Boids.
    /// </summary>
    public class BoidEater : MonoBehaviour
    {
        /// <summary>Radius within which the Boids are going to be consumed.</summary>
        [field: SerializeField]
        public float Radius { get; private set; }

        /// <summary>Number of eaten Boids.</summary>
        public int Score { get; private set; }

        /// <summary>
        /// Adds eater to all simulations.
        /// </summary>
        private void Start()
        {
            foreach (var simulation in SimulationManager.Instance.GetSimulations())
                simulation.AddEater(this);
        }

        /// <summary>
        /// Adds eater to all simulations.
        /// </summary>
        private void OnEnable()
        {
            foreach (var simulation in SimulationManager.Instance.GetSimulations())
                simulation.AddEater(this);
        }

        /// <summary>
        /// Removes eater from all simulations.
        /// </summary>
        private void OnDisable()
        {
            if (SimulationManager.Instance == null) return; // check if simulation manager wasn't already destroyed

            foreach (var simulation in SimulationManager.Instance.GetSimulations())
                simulation.RemoveEater(this);
        }

        /// <summary>
        /// Adds a value to the score.
        /// </summary>
        /// <param name="value">Number of eaten Boids.</param>
        public void AddScore(int value)
        {
            Score += value;
        }
    }
}