using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Finds the indexes of the first and last Boid for each chunk, in parallel. Boids have to be sorted based on chunks.
/// </summary>
[BurstCompile]
public struct FindChunkBoundsJob : IJobParallelFor
{
    /// <summary>Number of boids in the simulation.</summary>
    public int BoidCount;

    /// <summary>Dimensions of each chunk.</summary>
    public Vector3Int ChunkDimensions;

    /// <summary>Number of chunks for each axis of the simulation.</summary>
    public Vector3Int ChunkCount;

    /// <summary>Read-only array of Boids to be processed. Boids have to be sorted based on their chunks.</summary>
    [ReadOnly] public NativeArray<Boid> Boids;

    /// <summary>Write-only array for storing indexes of the first Boid in each chunk.</summary>
    [NativeDisableParallelForRestriction] [WriteOnly]
    public NativeArray<int> ChunkStartIndexes;

    /// <summary>Write-only array for storing indexes of the last Boid in each chunk.</summary>
    [NativeDisableParallelForRestriction] [WriteOnly]
    public NativeArray<int> ChunkEndIndexes;

    /// <summary>
    /// Checks if a Boid is on a boundary of two chunks and if it is saves it's position.
    /// </summary>
    /// <param name="index">The index of the Boid in the Boids array.</param>
    public void Execute(int index)
    {
        // if it's the first Boid save its index
        if (index == 0)
        {
            var firstChunkId = BoidHelpers.DetermineChunkId(Boids[index].Position, ChunkCount, ChunkDimensions);
            ChunkStartIndexes[firstChunkId] = index;
            return;
        }

        // if it's the last Boid save its index
        if (index == BoidCount - 1)
        {
            var lastChunkId = BoidHelpers.DetermineChunkId(Boids[index].Position, ChunkCount, ChunkDimensions);
            ChunkEndIndexes[lastChunkId] = index;
            return;
        }

        // check if the Boid is on a boundary of chunks
        var currentChunk = BoidHelpers.DetermineChunkId(Boids[index].Position, ChunkCount, ChunkDimensions);
        var nextChunk = BoidHelpers.DetermineChunkId(Boids[index + 1].Position, ChunkCount, ChunkDimensions);

        if (currentChunk == nextChunk) return;

        ChunkEndIndexes[currentChunk] = index;
        ChunkStartIndexes[nextChunk] = index + 1;
    }
}