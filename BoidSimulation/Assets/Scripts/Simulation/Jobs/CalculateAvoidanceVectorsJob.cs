using Simulation.Core;
using Simulation.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Simulation.Jobs
{
    /// <summary>
    /// Calculates avoidance vectors for each Boid based on raycast results, in parallel.
    /// </summary>
    [BurstCompile]
    public struct CalculateAvoidanceVectorsJob : IJobParallelFor
    {
        /// <summary>Number of evenly distributed collision avoidance rays cast for each Boid.</summary>
        public int BoidRaycastCount;

        /// <summary>Maximum distance at which collisions were detected by a ray.</summary>
        public float RaycastDistance;

        /// <summary>Angle from Boid forward direction at which collision avoidance rays were cast.</summary>
        public float RaycastAngle;

        /// <summary>Maximum impact collision avoidance can have on Boid acceleration, between 0 and 1.</summary>
        public float MaxAvoidanceStrength;

        /// <summary>Read-only array of Boids for which avoidance vectors need to be calculated.</summary>
        [ReadOnly] public NativeArray<Boid> Boids;

        /// <summary>Read-only array containing raycast results.</summary>
        [ReadOnly] public NativeArray<RaycastHit> RaycastResults;

        /// <summary>Write-only array of vectors for storing the direction and strength of Boid collision avoidance.
        /// Strength is indicated by the magnitude of each vector.</summary>
        [WriteOnly] public NativeArray<Vector3> AvoidanceVectors;

        /// <summary>
        /// Calculates avoidance vector for a Boid based on all raycasts performed for it.
        /// </summary>
        /// <param name="index">The index of the Boid in the array.</param>
        public void Execute(int index)
        {
            // Boids are always orientated in the direction they are going
            var boidForward = Boids[index].Velocity.normalized;
            var avoidanceVector = Vector3.zero;
            var minDistance = RaycastDistance;
            for (var i = 0; i < BoidRaycastCount; i++)
            {
                var raycastResult = RaycastResults[index * BoidRaycastCount + i];
                if (raycastResult.colliderInstanceID == 0) continue; // skip raycasts which didn't hit anything

                // calculate the direction at which the ray was cast
                var raycastDirection =
                    BoidHelpers.CalculateRaycastDirection(boidForward, i, BoidRaycastCount, RaycastAngle);

                // find minimum obstacle distance 
                if (raycastResult.distance < minDistance)
                    minDistance = raycastResult.distance;

                // avoidance should happen in the opposite direction than the obstacle
                avoidanceVector -= raycastDirection;
            }

            // avoidance strength is inversely proportional to the distance to obstacle
            var avoidanceStrength =
                Mathf.Clamp((RaycastDistance - minDistance) / RaycastDistance, 0f, MaxAvoidanceStrength);
            AvoidanceVectors[index] = avoidanceVector.normalized * avoidanceStrength;
        }
    }
}