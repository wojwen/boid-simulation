using UnityEngine;

namespace Simulation.Core
{
    /// <summary>
    /// Struct representing a Boid.
    /// </summary>
    public struct Boid
    {
        /// <summary>Position of the Boid relative to the origin of the <see cref="BoidSimulation"/> it's in.</summary>
        public Vector3 Position;

        /// <summary>Velocity of the Boid in m/s. Boids are always orientated in the direction they are going.</summary>
        public Vector3 Velocity;
    }
}