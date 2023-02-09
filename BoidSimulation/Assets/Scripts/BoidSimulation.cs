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

    /// <summary>Maximum number of chunks within Boid's vision range.</summary>
    public const int MaxNearbyChunkCount = 27;

    /// <summary>Strength with which Boids try to avoid each other.</summary>
    [SerializeField] private float separationStrength;

    /// <summary>Strength with which Boids try to align their directions.</summary>
    [SerializeField] private float alignmentStrength;

    /// <summary>Strength with which Boids try to steer toward local group center.</summary>
    [SerializeField] private float cohesionStrength;

    /// <summary>Radius within which a Boid can see other Boids.</summary>
    [SerializeField] private float visionRange;

    /// <summary>Maximum velocity of a boid in m/s.</summary>
    [SerializeField] private float maxVelocity;

    /// <summary>Maximum velocity of a boid in m/s^2.</summary>
    [SerializeField] private float maxAcceleration;

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

    /// <summary>Native array containing the Boids at the current state, always swapped with <see cref="_boidsNext"/> at
    /// the end of a simulation step.</summary>
    private NativeArray<Boid> _boidsCurrent;

    /// <summary>Native array for storing the new state of the Boids, always swapped with <see cref="_boidsCurrent"/> at
    /// the end of a simulation step. Can be read from while the other array is being sorted.</summary>
    private NativeArray<Boid> _boidsNext;

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
        _sortBoidsByChunksJob = _boidsCurrent.SortJob(new BoidChunkComparer(_chunkDimensions, chunkCount)).Schedule();
    }

    /// <summary>
    /// Runs the simulation. Steps of the simulation:
    /// <list>
    /// <item>1. Swap current and next Boid arrays.</item>
    /// <item>2. Initialize chunk start/end arrays.</item>
    /// <item>3. Finding chunk start/end indexes.</item>
    /// <item>4. Simulate Boids.</item>
    /// <item>5. Generate TRS matrices.</item>
    /// <item>6. Draw all boids.</item>
    /// <item>7. Sort Boids based on chunks.</item>
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
            Boids = _boidsCurrent,
            ChunkStartIndexes = _chunkStartIndexes,
            ChunkEndIndexes = _chunkEndIndexes
        }.Schedule(boidCount, JobBachSize, initializeChunkArraysJob);

        // simulate Boids
        var simulateBoidsJob = new SimulateBoidsJob
        {
            SeparationStrength = separationStrength,
            AlignmentStrength = alignmentStrength,
            CohesionStrength = cohesionStrength,
            VisionRange = visionRange,
            SimulationDimensions = simulationDimensions,
            ChunkCount = chunkCount,
            ChunkDimensions = _chunkDimensions,
            MaxAcceleration = maxAcceleration,
            MaxVelocity = maxVelocity,
            TimeDelta = Time.deltaTime,
            BoidsCurrent = _boidsCurrent,
            BoidsNext = _boidsNext,
            ChunkStartIndexes = _chunkStartIndexes,
            ChunkEndIndexes = _chunkEndIndexes
        }.Schedule(boidCount, JobBachSize, findChunkBoundsJob);

        // schedule generating TRS matrices
        new GenerateTRSMatricesJob
        {
            SimulationOrigin = transform.position,
            Boids = _boidsNext,
            TRSMatrices = _trsMatrices
        }.Schedule(boidCount, JobBachSize, simulateBoidsJob).Complete();
        _trsMatrices.CopyTo(_trsMatricesHelperArray); // copy the TRS matrices to the helper array

        // draw the Boids in the simulation using the TRS matrices
        Graphics.DrawMeshInstanced(boidMesh, 0, boidMaterial, _trsMatricesHelperArray, boidCount);

        (_boidsCurrent, _boidsNext) = (_boidsNext, _boidsCurrent);

        // sort Boids in the new state based on their chunks
        _sortBoidsByChunksJob = _boidsCurrent.SortJob(new BoidChunkComparer(_chunkDimensions, chunkCount)).Schedule();
    }

    /// <summary>
    /// Disposes of all allocated NativeArrays.
    /// </summary>
    public void Dispose()
    {
        _boidsCurrent.Dispose();
        _boidsNext.Dispose();
        _trsMatrices.Dispose();
        _chunkStartIndexes.Dispose();
        _chunkEndIndexes.Dispose();
    }

    /// <summary>
    /// Initializes all Boids at random positions and with random velocities.
    /// </summary>
    private void InitializeBoids()
    {
        _boidsCurrent = new NativeArray<Boid>(boidCount, Allocator.Persistent);
        _boidsNext = new NativeArray<Boid>(boidCount, Allocator.Persistent);
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

            _boidsCurrent[i] = new Boid { Position = randomPosition, Velocity = Random.insideUnitSphere };
            _boidsNext[i] = new Boid { Position = randomPosition, Velocity = Random.insideUnitSphere };
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
        // the position can be cast to int since all chunks have integer dimensions
        var x = Mathf.Clamp((int)position.x / chunkDimensions.x, 0, chunkCount.x - 1);
        var y = Mathf.Clamp((int)position.y / chunkDimensions.y, 0, chunkCount.y - 1);
        var z = Mathf.Clamp((int)position.z / chunkDimensions.z, 0, chunkCount.z - 1);

        return x + y * chunkCount.x + z * chunkCount.x * chunkCount.y;
    }
}