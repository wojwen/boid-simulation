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

    /// <summary>
    /// Initializes Boids and variables.
    /// </summary>
    private void Start()
    {
        _chunkDimensions = new Vector3Int(simulationDimensions.x / chunkCount.x, simulationDimensions.y / chunkCount.y,
            simulationDimensions.z / chunkCount.z);
        InitializeBoids();
    }

    /// <summary>
    /// Runs the simulation. Steps of the simulation:
    /// <list>
    /// <item>1. Generate TRS matrices.</item>
    /// <item>2. Draw all boids.</item>
    /// </list>
    /// </summary>
    private void Update()
    {
        // schedule generating TRS matrices
        new GenerateTRSMatricesJob
        {
            SimulationOrigin = transform.position,
            Boids = _boids,
            TRSMatrices = _trsMatrices
        }.Schedule(boidCount, JobBachSize).Complete();
        _trsMatrices.CopyTo(_trsMatricesHelperArray); // copy the TRS matrices to the helper array

        // draw the Boids in the simulation using the TRS matrices
        Graphics.DrawMeshInstanced(boidMesh, 0, boidMaterial, _trsMatricesHelperArray, boidCount);
    }

    /// <summary>
    /// Disposes of all allocated NativeArrays.
    /// </summary>
    public void Dispose()
    {
        _boids.Dispose();
        _trsMatrices.Dispose();
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
        return x + y * chunkCount.y + z * chunkCount.z * chunkCount.z;
    }
}