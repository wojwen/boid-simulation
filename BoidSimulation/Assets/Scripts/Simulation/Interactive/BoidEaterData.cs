using UnityEngine;

/// <summary>
/// Data describing a <see cref="BoidEater"/>, which can be passed into a Unity Job. 
/// </summary>
public struct BoidEaterData
{
    /// <summary>Position of the Boid eater in simulation space.</summary>
    public Vector3 Position;

    /// <summary>Radius within which the Boids are going to be consumed.</summary>
    public float Radius;
}