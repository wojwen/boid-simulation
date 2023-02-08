using UnityEngine;

/// <summary>
/// Struct representing a Boid.
/// </summary>
public struct Boid
{
    /// <summary>Position of the Boid relative to the origin of the <see cref="BoidSimulation"/> it's in.</summary>
    public Vector3 Position;

    /// <summary>Velocity of the Boid in m/s.</summary>
    public Vector3 Velocity;
}