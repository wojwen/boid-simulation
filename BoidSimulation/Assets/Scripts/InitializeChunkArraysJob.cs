using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Initializes arrays for storing chunk start and end indexes, in parallel.
/// </summary>
[BurstCompile]
public struct InitializeChunkArraysJob : IJobParallelFor
{
    /// <summary>Write-only array for storing indexes of the first Boid in each chunk.</summary>
    [WriteOnly] public NativeArray<int> ChunkStartIndexes;

    /// <summary>Write-only array for storing indexes of the last Boid in each chunk.</summary>
    [WriteOnly] public NativeArray<int> ChunkEndIndexes;

    /// <summary>
    /// Sets start and end indexes for a chunk to -1, which represents no Boids in chunk.
    /// </summary>
    /// <param name="index">Index of the chunk.</param>
    public void Execute(int index)
    {
        ChunkStartIndexes[index] = -1;
        ChunkEndIndexes[index] = -1;
    }
}