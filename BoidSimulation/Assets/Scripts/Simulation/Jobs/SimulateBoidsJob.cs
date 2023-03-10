using Simulation.Core;
using Simulation.Interactive;
using Simulation.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Simulation.Jobs
{
    /// <summary>
    /// Simulates Boids based on the principles of separation, alignment and cohesion.
    /// </summary>
    [BurstCompile]
    public struct SimulateBoidsJob : IJobParallelFor
    {
        /// <summary>Strength with which Boids try to avoid each other.</summary>
        public float SeparationStrength;

        /// <summary>Strength with which Boids try to align their directions.</summary>
        public float AlignmentStrength;

        /// <summary>Strength with which Boids try to steer toward local group center.</summary>
        public float CohesionStrength;

        /// <summary>Radius within which a Boid can see other Boids.</summary>
        public float VisionRange;

        /// <summary>Dimensions of the simulation the Boids are in.</summary>
        public Vector3Int SimulationDimensions;

        /// <summary>Number of chunks for each axis of the simulation.</summary>
        public Vector3Int ChunkCount;

        /// <summary>Dimensions of each chunk.</summary>
        public Vector3Int ChunkDimensions;

        /// <summary>Maximum velocity of a boid in m/s^2.</summary>
        public float MaxAcceleration;

        /// <summary>Maximum velocity of a boid in m/s.</summary>
        public float MaxVelocity;

        /// <summary>The interval in seconds from the last frame to the current one.</summary>
        public float TimeDelta;

        /// <summary>Read-only array of Boids to be simulated. Boids have to be sorted based on their chunks.</summary>
        [ReadOnly] public NativeArray<Boid> BoidsCurrent;

        /// <summary>Write-only array for storing simulated Boids.</summary>
        [WriteOnly] public NativeArray<Boid> BoidsNext;

        /// <summary>Read-only array containing indexes of the first Boid in each chunk.</summary>
        [ReadOnly] public NativeArray<int> ChunkStartIndexes;

        /// <summary>Read-only array containing indexes of the last Boid in each chunk.</summary>
        [ReadOnly] public NativeArray<int> ChunkEndIndexes;

        /// <summary>Read-only array of vectors indicating the direction and strength of collision avoidance.</summary>
        [ReadOnly] public NativeArray<Vector3> AvoidanceVectors;

        /// <summary>Read-only array of vectors indicating the direction and strength of Boid attraction.</summary>
        [ReadOnly] public NativeArray<Vector3> AttractionVectors;

        /// <summary>Read-only array of Boid eaters which can consume Boids in the simulation.</summary>
        [ReadOnly] public NativeArray<BoidEaterData> EaterData;

        /// <summary>Write-only array for storing indexes of eaters which have consumed a Boid.</summary>
        [WriteOnly] public NativeArray<int> EatenBy;

        /// <summary>
        /// Simulates a Boid based on the principles of separation, alignment and cohesion.
        /// </summary>
        /// <param name="index">The index of the Boid in the array.</param>
        public void Execute(int index)
        {
            if (TryEatBoid(index)) return; // if the Boid was eaten don't simulate it

            var boid = BoidsCurrent[index];

            var outOfBoundsAcceleration = GetOutOfBoundsAccelerationDirection(boid.Position);

            // if the boid was out of bounds accelerate back, otherwise accelerate in the simulated direction
            var accelerationDirection = outOfBoundsAcceleration == Vector3.zero
                ? CalculateAccelerationDirection(index, boid)
                : outOfBoundsAcceleration;

            // spherically interpolate between current direction and attraction direction based on attraction strength
            var attractionVector = AttractionVectors[index];
            accelerationDirection =
                Vector3.Slerp(accelerationDirection, attractionVector.normalized, attractionVector.magnitude);

            // spherically interpolate between the current direction and avoidance direction based on avoidance strength
            var avoidanceVector = AvoidanceVectors[index];

            // avoidance strength should be proportional to the velocity of the Boid to prevent oversteering
            var avoidanceStrength = avoidanceVector.magnitude * boid.Velocity.magnitude / MaxVelocity;

            // spherically interpolate between the current direction and avoidance direction based on avoidance strength
            accelerationDirection = Vector3.Slerp(accelerationDirection, avoidanceVector.normalized, avoidanceStrength);

            var acceleration = accelerationDirection * MaxAcceleration;

            // calculate new velocity and clamp it
            var newVelocity = boid.Velocity + acceleration * TimeDelta;
            newVelocity = Vector3.ClampMagnitude(newVelocity, MaxVelocity);

            // calculate new position based on new velocity
            var newPosition = boid.Position + newVelocity * TimeDelta;

            // save the calculated position and velocity
            BoidsNext[index] = new Boid { Position = newPosition, Velocity = newVelocity };
        }

        /// <summary>
        /// Checks if the Boid was eaten and if it was, transports it to the farthest bottom edge of the simulation.
        /// </summary>
        /// <param name="index">The index of the Boid in the array.</param>
        /// <returns>True if the Boid was eaten and false if it wasn't.</returns>
        private bool TryEatBoid(int index)
        {
            var boid = BoidsCurrent[index];

            for (var i = 0; i < EaterData.Length; i++)
            {
                var eater = EaterData[i];

                // skip if the Boid didn't get close enough to get eaten
                if (!(Vector3.Distance(eater.Position, boid.Position) <= eater.Radius)) continue;

                // move the boid to the farthest bottom edge of the simulation and randomize it's velocity
                BoidsNext[index] = new Boid
                {
                    Position = GetFarthestBottomEdge(boid.Position),
                    Velocity = Random.CreateFromIndex((uint)index).NextFloat3Direction()
                };
                EatenBy[index] = i; // save by which eater ate the Boid
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds bottom edge of the simulation which is farthest from a Boid.
        /// </summary>
        /// <param name="position">Position of the Boid.</param>
        /// <returns></returns>
        private Vector3 GetFarthestBottomEdge(Vector3 position)
        {
            var maxDistance = 0f;
            Vector3 farthestEdge = default;

            // iterate over all bottom edges
            for (var i = 0; i <= 1; i++)
            for (var j = 0; j <= 1; j++)
            {
                var edgePosition = new Vector3(i * SimulationDimensions.x, 0, j * SimulationDimensions.z);
                var distance = Vector3.Distance(position, edgePosition);
                if (!(distance > maxDistance)) continue; // continue if this isn't the farthest edge
                maxDistance = distance;
                farthestEdge = edgePosition;
            }

            return farthestEdge;
        }

        /// <summary>
        /// Calculates direction in which the Boid needs to accelerate to return to the simulation area.
        /// </summary>
        /// <param name="position">Position of the Boid.</param>
        /// <returns>Direction towards the simulation area for out of bounds Boids, otherwise a zero vector.</returns>
        private Vector3 GetOutOfBoundsAccelerationDirection(Vector3 position)
        {
            if (position.x > SimulationDimensions.x)
                return -Vector3.right;
            if (position.x < 0)
                return Vector3.right;
            if (position.y > SimulationDimensions.y)
                return -Vector3.up;
            if (position.y < 0)
                return Vector3.up;
            if (position.z > SimulationDimensions.z)
                return -Vector3.forward;
            if (position.z < 0)
                return Vector3.forward;

            return Vector3.zero;
        }

        /// <summary>
        /// Calculates acceleration direction based on the principles of separation, alignment and cohesion.
        /// </summary>
        /// <param name="index">Index of the Boid in the array.</param>
        /// <param name="boid">Boid which should be simulated.</param>
        /// <returns>Direction vector for Boid's acceleration with magnitude proportional to acceleration.</returns>
        private Vector3 CalculateAccelerationDirection(int index, Boid boid)
        {
            var nearbyChunkIds = GetNearbyChunkIds(boid.Position); // get chunks in vision range

            // initialize variables
            var boidsInRange = 0;
            var separationVector = Vector3.zero; // combined direction pointing away from Boids in vision range
            var averagePosition = Vector3.zero; // average position of Boids in vision range
            var averageVelocity = Vector3.zero; // average velocity of Boids in vision range

            // iterate over every chunk which contains Boids in vision range of the Boid
            foreach (var chunkId in nearbyChunkIds)
            {
                var chunkStart = ChunkStartIndexes[chunkId]; // get the index of the first Boid in chunk
                if (chunkStart == -1) continue; // skip the chunk if there are no Boids in it
                var chunkEnd = ChunkEndIndexes[chunkId]; // get the index of the last Boid in chunk

                // iterate over every Boid in chunk
                for (var i = chunkStart; i <= chunkEnd; i++)
                {
                    if (index == i) continue; // don't interact with itself

                    var otherBoid = BoidsCurrent[i];
                    var distance = Vector3.Distance(boid.Position, otherBoid.Position);
                    if (distance > VisionRange) continue; // skip this Boid if it's not in vision range

                    boidsInRange++;

                    // add to the separation vector the vector pointing away from the nearby boid with magnitude
                    // inversely proportional to the distance between them
                    separationVector += (VisionRange - distance) / VisionRange *
                                        (boid.Position - otherBoid.Position).normalized;
                    averagePosition += otherBoid.Position;
                    averageVelocity += otherBoid.Velocity;
                }
            }

            // if there are no Boids in range then don't change direction
            if (boidsInRange == 0)
                return boid.Velocity.normalized;

            separationVector /= boidsInRange; // calculate the average separation vector
            averagePosition /= boidsInRange; // calculate the average nearby Boid position
            averageVelocity /= boidsInRange; // calculate the average nearby Boid velocity

            // calculate the cohesion vector as the direction toward the average Boid position with normalized magnitude
            var cohesionVector = (averagePosition - boid.Position) / VisionRange;

            // calculate te alignment as the average normalized velocity of nearby Boids 
            var alignmentVector = averageVelocity / MaxVelocity;

            // calculate the weighted average of separation, alignment and cohesion vectors
            return (separationVector * SeparationStrength + cohesionVector * CohesionStrength +
                    alignmentVector * AlignmentStrength) / (SeparationStrength + CohesionStrength + AlignmentStrength);
        }

        /// <summary>
        /// Finds IDs of chunks which are within vision range of a Boid.
        /// </summary>
        /// <param name="position">Position of the Boid.</param>
        /// <returns>Hash set containing the chunk IDs.</returns>
        private NativeHashSet<int> GetNearbyChunkIds(Vector3 position)
        {
            // allocate hash set with Temp allocator since it will be used within the job
            var neighbourChunks = new NativeHashSet<int>(BoidSimulation.MaxNearbyChunkCount, Allocator.Temp);

            // find chunks which are within a cube encapsulating the vision radius of the Boid
            for (var x = -VisionRange; x <= VisionRange; x += VisionRange)
            for (var y = -VisionRange; y <= VisionRange; y += VisionRange)
            for (var z = -VisionRange; z <= VisionRange; z += VisionRange)
            {
                // when determining chunk Ids the position is clamped so there is no need to check if it's out of bounds
                var edgePosition = position + new Vector3(x, y, x);
                neighbourChunks.Add(BoidHelpers.DetermineChunkId(edgePosition, ChunkCount, ChunkDimensions));
            }

            return neighbourChunks;
        }
    }
}