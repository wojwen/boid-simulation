using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Simulates Boids in a given space.
/// </summary>
public class BoidSimulation : MonoBehaviour, IDisposable
{
    /// <summary>The size of each batch of jobs.</summary>
    private const int JobBachSize = 32;

    /// <summary>Mesh used for rendering the Boids.</summary>
    [SerializeField] private Mesh boidMesh;

    /// <summary>Material used for rendering the Boids. Must support instanced rendering.</summary>
    [SerializeField] private Material boidMaterial;

    /// <summary>Dimensions of the simulation the Boids are in.</summary>
    [SerializeField] private Vector3Int simulationDimensions;

    /// <summary>Number of chunks for each axis of the simulation.</summary>
    [SerializeField] private Vector3Int chunkCount;

    /// <summary>Number of boids in the simulation.</summary>
    [SerializeField] private int boidCount;

    /// <summary>Native array containing the Boids</summary>
    private NativeArray<Boid> _boids;

    /// <summary>Dimensions of each chunk.</summary>
    private Vector3Int _chunkDimensions;

    /// <summary>Native array for storing the calculated TRS matrices.</summary>
    private NativeArray<Matrix4x4> _trsMatrices;

    /// <summary>Array to which TRS matrices need to be copied to so that Boids can be drawn.</summary>
    private Matrix4x4[] _trsMatricesHelperArray;

    /// <summary>Native array for storing indexes of the first Boid in each chunk.</summary>
    private NativeArray<int> _chunkStartIndexes;

    /// <summary>Native array for storing indexes of the last Boid in each chunk.</summary>
    private NativeArray<int> _chunkEndIndexes;

    /// <summary>Total number of chunks.</summary>
    private int _totalChunkCount;

    /// <summary>Handle for a job which sorts Boids based on the chunks they are in.</summary>
    private JobHandle _sortBoidsByChunksJob;

    /// <summary>
    /// Initializes Boids and chunks.
    /// </summary>
    private void Start()
    {
        InitializeBoids();
        InitializeChunks();
    }

    /// <summary>
    /// Runs the simulation. Steps of the simulation:
    /// <list>
    /// <item>1. Initialize chunk start/end arrays.</item>
    /// <item>2. Finding chunk start/end indexes.</item>
    /// <item>3. Generate TRS matrices.</item>
    /// <item>4. Draw all boids.</item>
    /// <item>5. Sort Boids based on chunks.</item>
    /// </list>
    /// </summary>
    private void Update()
    {
        // schedule initializing chunk start/end arrays
        var initializeChunkArraysJob = new InitializeChunkArraysJob
        {
            ChunkStartIndexes = _chunkStartIndexes,
            ChunkEndIndexes = _chunkEndIndexes
        }.Schedule(_totalChunkCount, JobBachSize, _sortBoidsByChunksJob);

        // schedule finding chunk start/end indexes
        var findChunkBoundsJob = new FindChunkBoundsJob
        {
            BoidCount = boidCount,
            ChunkDimensions = _chunkDimensions,
            ChunkCount = chunkCount,
            Boids = _boids,
            ChunkStartIndexes = _chunkStartIndexes,
            ChunkEndIndexes = _chunkEndIndexes
        }.Schedule(boidCount, JobBachSize, initializeChunkArraysJob);

        // schedule generating TRS matrices
        new GenerateTRSMatricesJob
        {
            SimulationOrigin = transform.position,
            Boids = _boids,
            TRSMatrices = _trsMatrices
        }.Schedule(boidCount, JobBachSize, findChunkBoundsJob).Complete();
        _trsMatrices.CopyTo(_trsMatricesHelperArray); // copy the TRS matrices to the helper array

        // draw the Boids in the simulation using the TRS matrices
        Graphics.DrawMeshInstanced(boidMesh, 0, boidMaterial, _trsMatricesHelperArray, boidCount);

        _sortBoidsByChunksJob = _boids.SortJob(new BoidChunkComparer(_chunkDimensions, chunkCount)).Schedule();
    }

    /// <summary>
    /// Disposes of all allocated NativeArrays.
    /// </summary>
    public void Dispose()
    {
        _boids.Dispose();
        _trsMatrices.Dispose();
        _chunkStartIndexes.Dispose();
        _chunkEndIndexes.Dispose();
    }

    /// <summary>
    /// Initializes all Boids at random positions and with random velocities.
    /// </summary>
    private void InitializeBoids()
    {
        _boids = new NativeArray<Boid>(boidCount, Allocator.Persistent);
        _trsMatrices = new NativeArray<Matrix4x4>(boidCount, Allocator.Persistent);
        _trsMatricesHelperArray = new Matrix4x4[boidCount];

        for (var i = 0; i < boidCount; i++)
        {
            var randomPosition = new Vector3
            {
                x = Random.Range(0f, simulationDimensions.x),
                y = Random.Range(0f, simulationDimensions.y),
                z = Random.Range(0f, simulationDimensions.z)
            };

            _boids[i] = new Boid { Position = randomPosition, Velocity = Random.insideUnitSphere };
        }
    }

    /// <summary>
    /// Initializes arrays and variables for storing information about chunks.
    /// </summary>
    private void InitializeChunks()
    {
        _chunkDimensions = new Vector3Int(simulationDimensions.x / chunkCount.x, simulationDimensions.y / chunkCount.y,
            simulationDimensions.z / chunkCount.z);
        _totalChunkCount = chunkCount.x * chunkCount.y * chunkCount.z;

        _chunkStartIndexes = new NativeArray<int>(_totalChunkCount, Allocator.Persistent);
        _chunkEndIndexes = new NativeArray<int>(_totalChunkCount, Allocator.Persistent);
    }

    /// <summary>
    /// Determines the chunk ID for a given position in the simulation.
    /// </summary>
    /// <param name="position">Position in the simulation.</param>
    /// <param name="chunkCount">Number of chunks for each axis of the simulation.</param>
    /// <param name="chunkDimensions">Dimensions of each chunk.</param>
    public static int DetermineChunkId(Vector3 position, Vector3Int chunkCount, Vector3Int chunkDimensions)
    {
        var x = Mathf.Clamp((int)position.x / chunkDimensions.x, 0, chunkCount.x - 1);
        var y = Mathf.Clamp((int)position.y / chunkDimensions.y, 0, chunkCount.y - 1);
        var z = Mathf.Clamp((int)position.z / chunkDimensions.z, 0, chunkCount.z - 1);

        return x + y * chunkCount.x + z * chunkCount.x * chunkCount.y;
    }
}