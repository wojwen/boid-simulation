using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Initializes array for storing indexes of eaters which ate Boids, in parallel.
/// </summary>
[BurstCompile]
public struct InitializeEatenByArrayJob : IJobParallelFor
{
    /// <summary>Write only array for storing indexes of eaters which ate Boids.</summary>
    [WriteOnly] public NativeArray<int> EatenBy;

    /// <summary>
    /// Sets each value in the array to -1, which represents not being eaten.
    /// </summary>
    /// <param name="index">Index of the Boid.</param>
    public void Execute(int index)
    {
        EatenBy[index] = -1;
    }
}