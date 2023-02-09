using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Simulation.Jobs
{
    /// <summary>
    /// Counts how many Boids each eater consumed.
    /// </summary>
    [BurstCompile]
    public struct CountEaterScoresJob : IJob
    {
        /// <summary>Read-only array containing Indexes of eaters which have consumed a Boid.</summary>
        [ReadOnly] public NativeArray<int> EatenBy;

        /// <summary>Accumulated count of Boids consumed by each Boid eater.</summary>
        public NativeArray<int> Scores;

        /// <summary>
        /// Counts boids consumed by each Boid eater.
        /// </summary>
        public void Execute()
        {
            foreach (var eaterIndex in EatenBy)
            {
                if (eaterIndex != -1) // -1 represents a Boid that hasn't been eaten
                    Scores[eaterIndex]++;
            }
        }
    }
}