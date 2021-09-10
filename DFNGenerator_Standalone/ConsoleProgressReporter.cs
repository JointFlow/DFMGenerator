using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DFNGenerator_SharedCode;

namespace DFNGenerator_Standalone
{
    /// <summary>
    /// Object to be used by the calculation algorithms to update progress, implementing the IProgressReporterWrapper interface; will write progress updates to Console
    /// </summary>
    class ConsoleProgressReporter : IProgressReporterWrapper
    {
        // Internal data
        /// <summary>
        /// Total number of calculation elements - can be number of gridblocks, number of timesteps, or total number of gridblock timesteps
        /// </summary>
        private int TotalCalculationElements { get; set; }
        /// <summary>
        /// Frequency at which to write progress updates to Console - an update will be written when the specified number or percentage of calculation elements are completed
        /// </summary>
        private int UpdateFrequency { get; set; }
        /// <summary>
        /// Next point (number or percentage of calculation elements) at which to write a progress update to Console
        /// </summary>
        private int NextUpdate { get; set; }
        /// <summary>
        /// Flag to control update frequency units; set true to report updates by percentage of total calculation elements completed, set false to report updates by number of calculation elements completed
        /// </summary>
        private bool UpdatePercentage { get; set; }

        // Functions defined by IProgressReporterWrapper interface
        /// <summary>
        /// Set the total number of calculation elements to be calculated
        /// </summary>
        /// <param name="noTotalCalculationElements">Total number of calculation elements - can be number of gridblocks, number of timesteps, or total number of gridblock timesteps</param>
        public void SetNumberOfElements(int noTotalCalculationElements)
        {
            TotalCalculationElements = noTotalCalculationElements;
            NextUpdate = UpdateFrequency;
        }
        /// <summary>
        /// Write an update on the progress of the calculation to Console, if the next update point has been reached
        /// </summary>
        /// <param name="noCalculationElementsCompleted">Number of calculation elements completed so far</param>
        public void UpdateProgress(int noCalculationElementsCompleted)
        {
            if (TotalCalculationElements > 0)
            {
                if (UpdatePercentage)
                {
                    int percentageComplete = (int)(100d * ((double)noCalculationElementsCompleted / (double)TotalCalculationElements));

                    while (percentageComplete >= NextUpdate)
                    {
                        Console.WriteLine(string.Format("{0}% complete", NextUpdate));
                        NextUpdate += UpdateFrequency;
                    }
                }
                else
                {
                    while (noCalculationElementsCompleted >= NextUpdate)
                    {
                        Console.WriteLine(string.Format("Completed {0} of {1} calculation elements", NextUpdate, TotalCalculationElements));
                        NextUpdate += UpdateFrequency;
                    }
                }
            }
        }
        /// <summary>
        /// Flag to abort calculation; always returns false (this implementation of the IProgressReporterWrapper interface does not allow calculation to be aborted)
        /// </summary>
        /// <returns>Always returns false</returns>
        public bool abortCalculation()
        {
            return false;
        }

        // Constructors
        /// <summary>
        /// Constructor specifying update frequency and update frequency units
        /// </summary>
        /// <param name="UpdateFrequency_in">Frequency at which to write progress updates to Console - an update will be written when the specified number or percentage of calculation elements are completed</param>
        /// <param name="UpdatePercentage_in">Flag to control update frequency units; set true to report updates by percentage of total calculation elements completed, set false to report updates by number of calculation elements completed</param>
        public ConsoleProgressReporter(int UpdateFrequency_in, bool UpdatePercentage_in)
        {
            // Set the update frequency and update frequency units
            UpdateFrequency = UpdateFrequency_in;
            UpdatePercentage = UpdatePercentage_in;

            // By default, set the total number of calculations elements to zero - no updates will be issued until this is set
            TotalCalculationElements = 0;
            // Set the next update to the update frequency
            NextUpdate = UpdateFrequency;
        }
    }
}
