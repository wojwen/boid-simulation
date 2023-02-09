using UnityEngine;

/// <summary>
/// Data describing a <see cref="BoidAttractor"/> ,which can be passed into a Unity Job. 
/// </summary>
public struct BoidAttractorData
{
    /// <summary>Position of the Boid attractor in simulation space.</summary>
    public Vector3 Position;

    /// <summary>Strength of the Boid attractor. Negative values repel Boids.</summary>
    public float Strength;

    /// <summary>Radius of the Boid attractor.</summary>
    public float Radius;
}