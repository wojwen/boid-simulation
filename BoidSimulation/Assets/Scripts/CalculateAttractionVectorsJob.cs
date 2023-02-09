using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Calculates attraction vectors for each Boid based on all attractors in simulation, in parallel.
/// </summary>
[BurstCompile]
public struct CalculateAttractionVectorsJob : IJobParallelFor
{
    /// <summary>Read-only array of Boids for which attraction vectors need to be calculated.</summary>
    [ReadOnly] public NativeArray<Boid> Boids;

    /// <summary>Read-only array containing data about attractors in the simulation.</summary>
    [ReadOnly] public NativeArray<BoidAttractorData> AttractorData;

    /// <summary>Write-only array of vectors for storing the direction and strength of Boid attraction. Strength is
    /// indicated by the magnitude of each vector.</summary>
    [WriteOnly] public NativeArray<Vector3> AttractionVectors;

    /// <summary>
    /// Calculates attraction vector for a Boid based on all attractors in the simulation.
    /// </summary>
    /// <param name="index">The index of the Boid in the array.</param>
    public void Execute(int index)
    {
        var boidPosition = Boids[index].Position;
        var attractionVector = Vector3.zero;

        foreach (var attractor in AttractorData)
        {
            var distance = Vector3.Distance(boidPosition, attractor.Position);
            if (distance >= attractor.Radius) continue; // skip attractors which are too far away

            // magnitude is inversely proportional to the distance to the attractor and proportional to it's strength
            var magnitude = (attractor.Radius - distance) / attractor.Radius * attractor.Strength;
            attractionVector += (attractor.Position - boidPosition).normalized * magnitude;
        }

        AttractionVectors[index] = attractionVector;
    }
}