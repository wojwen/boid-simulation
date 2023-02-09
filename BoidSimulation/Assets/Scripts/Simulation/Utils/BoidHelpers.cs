using UnityEngine;

namespace Simulation.Utils
{
    /// <summary>
    /// Class containing helper methods for performing operations on Boids.
    /// </summary>
    public static class BoidHelpers
    {
        /// <summary>
        /// Calculate a direction at which n-th evenly distributed collision avoidance ray should be cast for a Boid.
        /// </summary>
        /// <param name="boidForward">Forward direction of the Boid.</param>
        /// <param name="raycastId">Index of the ray which direction will be calculated.</param>
        /// <param name="boidRaycastCount">Number of rays cast for each Boid.</param>
        /// <param name="raycastAngle">Angle from Boid forward direction at which the ray should be cast.</param>
        public static Vector3 CalculateRaycastDirection(Vector3 boidForward, int raycastId, float boidRaycastCount,
            float raycastAngle)
        {
            var centralAngle = 360f / boidRaycastCount; // calculate angle which evenly distributes raycasts
            var raycastDirection = GenerateVectorAtAngle(boidForward, raycastAngle);

            // rotate raycast direction around Boid forward direction
            return Quaternion.AngleAxis(centralAngle * raycastId, boidForward) * raycastDirection;
        }

        /// <summary>
        /// Generates a vector at an angle from a direction.
        /// </summary>
        /// <param name="direction">Direction from which the new vector should be calculated.</param>
        /// <param name="angle">Angle between the direction and the new vector.</param>
        private static Vector3 GenerateVectorAtAngle(Vector3 direction, float angle)
        {
            // find an axis perpendicular to direction around which the direction is going to be rotated
            var perpendicularAxis = Vector3.Cross(direction, Vector3.right);
            if (perpendicularAxis.sqrMagnitude < Mathf.Epsilon) // if vectors were aligned use another axis
                perpendicularAxis = Vector3.Cross(direction, Vector3.up);

            return Quaternion.AngleAxis(angle, perpendicularAxis) * direction; // rotate direction by an angle
        }

        /// <summary>
        /// Determines the chunk ID for a given position in the simulation.
        /// </summary>
        /// <param name="position">Position in the simulation.</param>
        /// <param name="chunkCount">Number of chunks for each axis of the simulation.</param>
        /// <param name="chunkDimensions">Dimensions of each chunk.</param>
        public static int DetermineChunkId(Vector3 position, Vector3Int chunkCount, Vector3Int chunkDimensions)
        {
            // the position can be cast to int since all chunks have integer dimensions
            var x = Mathf.Clamp((int)position.x / chunkDimensions.x, 0, chunkCount.x - 1);
            var y = Mathf.Clamp((int)position.y / chunkDimensions.y, 0, chunkCount.y - 1);
            var z = Mathf.Clamp((int)position.z / chunkDimensions.z, 0, chunkCount.z - 1);

            return x + y * chunkCount.x + z * chunkCount.x * chunkCount.y;
        }
    }
}