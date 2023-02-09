using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Struct for comparing Boids based on the chunks they are in.
/// </summary>
public readonly struct BoidChunkComparer : IComparer<Boid>
{
    /// <summary>Dimensions of each chunk in the simulation.</summary>
    private readonly Vector3Int _chunkDimensions;

    /// <summary>Number of chunks for each axis of the simulation.</summary>
    private readonly Vector3Int _chunkCount;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="chunkDimensions">Dimensions of each chunk in the simulation.</param>
    /// <param name="chunkCount">Number of chunks for each axis of the simulation.</param>
    public BoidChunkComparer(Vector3Int chunkDimensions, Vector3Int chunkCount)
    {
        _chunkDimensions = chunkDimensions;
        _chunkCount = chunkCount;
    }

    /// <summary>
    /// Compares two Boids based on their chunks.
    /// </summary>
    /// <param name="boid">First Boid to compare.</param>
    /// <param name="otherBoid">Second Boid to compare.</param>
    /// <returns>
    /// Less than 0 if first Boid's chunk is first, more than 0 if second Boid's chunk is first and 0 if both Boids are
    /// in the same chunk.
    /// </returns>
    public int Compare(Boid boid, Boid otherBoid)
    {
        return BoidHelpers.DetermineChunkId(boid.Position, _chunkCount, _chunkDimensions)
            .CompareTo(BoidHelpers.DetermineChunkId(otherBoid.Position, _chunkCount, _chunkDimensions));
    }
}