using System.Collections.Generic;

/// <summary>
/// Manager for all currently running Boid simulations.
/// </summary>
public class SimulationManager : Singleton<SimulationManager>
{
    /// <summary>Hash set containing all currently running Boid simulations.</summary>
    private readonly HashSet<BoidSimulation> _simulations = new();

    /// <summary>
    /// Registers simulation. All simulations should be registered when created. 
    /// </summary>
    /// <param name="simulation">Simulation to register.</param>
    public void RegisterSimulation(BoidSimulation simulation)
    {
        _simulations.Add(simulation);
    }

    /// <summary>
    /// Unregisters simulation. All simulations should be registered when destroyed. 
    /// </summary>
    /// <param name="simulation">Simulation to unregister.</param>
    public void UnregisterSimulation(BoidSimulation simulation)
    {
        _simulations.Remove(simulation);
    }

    /// <summary>
    /// Returns all currently running simulations.
    /// </summary>
    public HashSet<BoidSimulation> GetSimulations()
    {
        return _simulations;
    }
}