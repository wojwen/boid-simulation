using Simulation.Core;
using Simulation.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Simulation.Jobs
{
    /// <summary>
    /// Prepares raycast commands for Boid collision avoidance, in parallel.
    /// </summary>
    [BurstCompile]
    public struct PrepareRaycastCommands : IJobParallelFor
    {
        /// <summary>Origin of the simulation the Boids are in.</summary>
        public Vector3 SimulationOrigin;

        /// <summary>Number of evenly distributed collision avoidance rays cast for each Boid.</summary>
        public int BoidRaycastCount;

        /// <summary>Maximum distance at which collisions are going to be detected by a ray.</summary>
        public float RaycastDistance;

        /// <summary>Angle from Boid forward direction at which collision avoidance rays are going to be cast.</summary>
        public float RaycastAngle;

        /// <summary>Layer mask for ignoring layers when casting rays.</summary>
        public LayerMask LayerMask;

        /// <summary>Read-only array of Boids for which raycast commands need to be prepared.</summary>
        [ReadOnly] public NativeArray<Boid> Boids;

        /// <summary>Write-only array for storing prepared raycast commands. Desired number of commands are created for
        /// each Boid and stored in the array next to each other.</summary>
        [WriteOnly] public NativeArray<RaycastCommand> RaycastCommands;

        /// <summary>
        /// Prepares raycast commands for a Boid.
        /// </summary>
        /// <param name="index">The index of the raycast command in the array.</param>
        public void Execute(int index)
        {
            var boid = Boids[index / BoidRaycastCount]; // get the Boid for which the commands is going to be prepared

            // find raycast direction
            var boidForward = boid.Velocity.normalized; // Boids are always facing the direction they are going
            var raycastId = index % BoidRaycastCount; // calculate which raycast for this Boid needs to be prepared 
            var raycastDirection =
                BoidHelpers.CalculateRaycastDirection(boidForward, raycastId, BoidRaycastCount, RaycastAngle);

            // prepare raycast command
            var queryParameters = new QueryParameters(layerMask: LayerMask); // include layer mask in query params 
            RaycastCommands[index] = new RaycastCommand(SimulationOrigin + boid.Position, raycastDirection,
                queryParameters, RaycastDistance);
        }
    }
}