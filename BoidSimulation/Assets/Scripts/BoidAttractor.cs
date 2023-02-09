using UnityEngine;

/// <summary>
/// Point which attracts or repels Boids within a radius.
/// </summary>
public class BoidAttractor : MonoBehaviour
{
    /// <summary>Strength of attraction. Negative values repel Boids.</summary>
    [field: SerializeField]
    public float Strength { get; private set; }

    /// <summary>Radius of attraction.</summary>
    [field: SerializeField]
    public float Radius { get; private set; }

    /// <summary>
    /// Add attractor to all simulations.
    /// </summary>
    private void OnEnable()
    {
        foreach (var simulation in SimulationManager.Instance.GetSimulations())
            simulation.AddAttractor(this);
    }

    /// <summary>
    /// Remove attractor from all simulations.
    /// </summary>
    private void OnDisable()
    {
        if (SimulationManager.Instance == null) return; // check if simulation manager wasn't already destroyed

        foreach (var simulation in SimulationManager.Instance.GetSimulations())
            simulation.RemoveAttractor(this);
    }
}