using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Generates TRS matrices for Boids in parallel. TRS matrices can be used for instanced rendering.
/// </summary>
[BurstCompile]
public struct GenerateTRSMatricesJob : IJobParallelFor
{
    /// <summary>Origin of the simulation the Boids are in.</summary>
    public Vector3 SimulationOrigin;

    /// <summary>Read-only array of Boids to be processed.</summary>
    [ReadOnly] public NativeArray<Boid> Boids;

    /// <summary>Write-only array for storing the calculated TRS matrices.</summary>
    [WriteOnly] public NativeArray<Matrix4x4> TRSMatrices;

    /// <summary>
    /// Executes the calculation of TRS matrices for each Boid.
    /// </summary>
    /// <param name="index">The index of the Boid in the Boids array.</param>
    public void Execute(int index)
    {
        TRSMatrices[index] = GenerateTRSMatrix(Boids[index]);
    }

    /// <summary>
    /// Generates a TRS matrix for a specific Boid, which is faster than the built-in Unity function
    /// <see cref="Matrix4x4.TRS"/> since all Boids have the default scale.
    /// </summary>
    /// <param name="boid">Boid for which TRS matrix should be calculated.</param>
    private Matrix4x4 GenerateTRSMatrix(Boid boid)
    {
        var position = SimulationOrigin + boid.Position; // calculate position in world space
        var rotation = Quaternion.LookRotation(boid.Velocity, Vector3.up);
        return new Matrix4x4
        {
            m00 = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z),
            m10 = (rotation.x * rotation.y + rotation.z * rotation.w) * 2.0f,
            m20 = (rotation.x * rotation.z - rotation.y * rotation.w) * 2.0f,
            m30 = 0.0f,
            m01 = (rotation.x * rotation.y - rotation.z * rotation.w) * 2.0f,
            m11 = 1.0f - 2.0f * (rotation.x * rotation.x + rotation.z * rotation.z),
            m21 = (rotation.y * rotation.z + rotation.x * rotation.w) * 2.0f,
            m31 = 0.0f,
            m02 = (rotation.x * rotation.z + rotation.y * rotation.w) * 2.0f,
            m12 = (rotation.y * rotation.z - rotation.x * rotation.w) * 2.0f,
            m22 = 1.0f - 2.0f * (rotation.x * rotation.x + rotation.y * rotation.y),
            m32 = 0.0f,
            m03 = position.x,
            m13 = position.y,
            m23 = position.z,
            m33 = 1.0f
        };
    }
}