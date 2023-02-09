using System;
using System.Collections.Generic;
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

    /// <summary>Number of evenly distributed collision avoidance rays cast for each Boid.</summary>
    [SerializeField] private int boidRaycastCount;

    /// <summary>Maximum distance at which collisions are going to be detected by a ray.</summary>
    [SerializeField] private float raycastDistance;

    /// <summary>Angle from Boid forward direction at which collision avoidance rays are going to be cast.</summary>
    [SerializeField] private float raycastAngle;

    /// <summary>Layer mask for ignoring layers when casting rays.</summary>
    [SerializeField] private LayerMask raycastLayerMask;

    /// <summary>Maximum impact collision avoidance can have on Boid acceleration, between 0 and 1.</summary>
    [SerializeField] [Range(0, 1)] private float maxAvoidanceStrength;

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

    /// <summary>Raycast commands used for collision avoidance.</summary>
    private NativeArray<RaycastCommand> _raycastCommands;

    /// <summary>Results of collision avoidance raycasts.</summary>
    private NativeArray<RaycastHit> _raycastResults;

    /// <summary>Vectors indicating the direction and strength of collision avoidance for each Boid.</summary>
    private NativeArray<Vector3> _avoidanceVectors;

    /// <summary>Total number of collision avoidance rays cast each frame.</summary>
    private int _totalRaycastCount;

    private HashSet<BoidAttractor> _attractors;

    /// <summary>
    /// Initializes Boids and chunks.
    /// </summary>
    private void Awake()
    {
        SimulationManager.Instance.RegisterSimulation(this);

        InitializeBoids();
        InitializeChunks();
        InitializeCollisionAvoidance();
        InitializeAttractors();

        _sortBoidsByChunksJob = _boidsCurrent.SortJob(new BoidChunkComparer(_chunkDimensions, chunkCount)).Schedule();
    }

    private void OnDestroy()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.UnregisterSimulation(this);
    }

    /// <summary>
    /// Runs the simulation. Steps of the simulation:
    /// <list>
    /// <item>1. Initialize chunk start/end arrays.</item>
    /// <item>2. Finding chunk start/end indexes.</item>
    /// <item>3. Prepare raycast commands for collision avoidance.</item>
    /// <item>4. Execute raycast commands.</item>
    /// <item>5. Calculate collision avoidance vectors based on raycast results.</item>
    /// <item>6. Simulate Boids.</item>
    /// <item>7. Generate TRS matrices.</item>
    /// <item>8. Draw all boids.</item>
    /// <item>9. Swap current and next Boid arrays.</item>
    /// <item>10. Sort Boids based on chunks.</item>
    /// </list>
    /// Steps 1-2 and 3-5 are performed in parallel.
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

        // prepare raycast commands for collision avoidance
        var prepareRaycastCommandsJob = new PrepareRaycastCommands
        {
            SimulationOrigin = transform.position,
            BoidRaycastCount = boidRaycastCount,
            RaycastDistance = raycastDistance,
            RaycastAngle = raycastAngle,
            LayerMask = raycastLayerMask,
            Boids = _boidsCurrent,
            RaycastCommands = _raycastCommands
        }.Schedule(_totalRaycastCount, JobBachSize, _sortBoidsByChunksJob);

        // execute raycast commands
        var raycastJob =
            RaycastCommand.ScheduleBatch(_raycastCommands, _raycastResults, JobBachSize, prepareRaycastCommandsJob);

        // calculate collision avoidance vectors based on raycast results
        var calculateAvoidanceVectorsJob = new CalculateAvoidanceVectorsJob
        {
            BoidRaycastCount = boidRaycastCount,
            RaycastDistance = raycastDistance,
            RaycastAngle = raycastAngle,
            MaxAvoidanceStrength = maxAvoidanceStrength,
            Boids = _boidsCurrent,
            RaycastResults = _raycastResults,
            AvoidanceVectors = _avoidanceVectors
        }.Schedule(boidCount, JobBachSize, raycastJob);

        var attractionVectors = new NativeArray<Vector3>(boidCount, Allocator.TempJob);
        var attractorData = ConvertAttractorsToNativeArray();
        var calculateAttractionVectorsJob = new CalculateAttractionVectorsJob
        {
            Boids = _boidsCurrent,
            AttractorData = attractorData,
            AttractionVectors = attractionVectors
        }.Schedule(boidCount, JobBachSize, _sortBoidsByChunksJob);

        var simulateBoidsDependencies = JobHandle.CombineDependencies(findChunkBoundsJob, calculateAvoidanceVectorsJob,
            calculateAttractionVectorsJob);

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
            ChunkEndIndexes = _chunkEndIndexes,
            AvoidanceVectors = _avoidanceVectors,
            AttractionVectors = attractionVectors
        }.Schedule(boidCount, JobBachSize, simulateBoidsDependencies);

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

        attractionVectors.Dispose();
        attractorData.Dispose();

        // sort Boids in the new state based on their chunks
        _sortBoidsByChunksJob = _boidsCurrent.SortJob(new BoidChunkComparer(_chunkDimensions, chunkCount)).Schedule();
    }

    public void AddAttractor(BoidAttractor boidAttractor)
    {
        _attractors.Add(boidAttractor);
    }

    public void RemoveAttractor(BoidAttractor boidAttractor)
    {
        if (_attractors.Contains(boidAttractor))
            _attractors.Remove(boidAttractor);
    }

    private NativeArray<BoidAttractorData> ConvertAttractorsToNativeArray()
    {
        var attractorData = new NativeArray<BoidAttractorData>(_attractors.Count, Allocator.TempJob);
        var index = 0;
        foreach (var attractor in _attractors)
        {
            attractorData[index] = new BoidAttractorData
            {
                Position = attractor.transform.position - transform.position,
                Strength = attractor.Strength,
                Radius = attractor.Radius
            };
            index++;
        }

        return attractorData;
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
        _raycastCommands.Dispose();
        _raycastResults.Dispose();
        _avoidanceVectors.Dispose();
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
    /// Initializes arrays and variables used for collision avoidance.
    /// </summary>
    private void InitializeCollisionAvoidance()
    {
        _totalRaycastCount = boidCount * boidRaycastCount;
        _raycastCommands = new NativeArray<RaycastCommand>(_totalRaycastCount, Allocator.Persistent);
        _raycastResults = new NativeArray<RaycastHit>(_totalRaycastCount, Allocator.Persistent);
        _avoidanceVectors = new NativeArray<Vector3>(boidCount, Allocator.Persistent);
    }

    private void InitializeAttractors()
    {
        _attractors = new HashSet<BoidAttractor>();
    }
}