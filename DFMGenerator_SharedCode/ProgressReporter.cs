using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Interface for an object to be used by the calculation algorithms to update progress - can be a wrapper for a Progress Bar object
    /// </summary>
    interface IProgressReporterWrapper
    {
        /// <summary>
        /// Set the total number of calculation elements to be calculated
        /// </summary>
        /// <param name="noTotalCalculationElements">Total number of calculation elements - can be number of gridblocks, number of timesteps, or total number of gridblock timesteps</param>
        void SetNumberOfElements(int noTotalCalculationElements);
        /// <summary>
        /// Output an update on the progress of the calculation, using whatever method is specified by the implementing class
        /// </summary>
        /// <param name="noCalculationElementsCompleted">Number of calculation elements completed so far</param>
        void UpdateProgress(int noCalculationElementsCompleted);
        /// <summary>
        /// Output a message (e.g. an error message), using whatever method is specified by the implementing class
        /// </summary>
        /// <param name="message">Message to output</param>
        void OutputMessage(string message);
        /// <summary>
        /// Flag to abort calculation; return true if calculation should be aborted
        /// </summary>
        /// <returns>True if calculation should be aborted</returns>
        bool abortCalculation();
    }
    /// <summary>
    /// Basic form of ProgressReporter object; implements the IProgressReporterWrapper interface but does not report any progress
    /// </summary>
    class DefaultProgressReporter : IProgressReporterWrapper
    {
        // Internal data
        // None

        // Functions defined by IProgressReporterWrapper interface
        /// <summary>
        /// Set the total number of calculation elements to be calculated
        /// </summary>
        /// <param name="noTotalCalculationElements">Total number of calculation elements - can be number of gridblocks, number of timesteps, or total number of gridblock timesteps</param>
        public void SetNumberOfElements(int noTotalCalculationElements)
        {
            // Do nothing
        }
        /// <summary>
        /// Write an update on the progress of the calculation, if the next update point has been reached
        /// </summary>
        /// <param name="noCalculationElementsCompleted">Number of calculation elements completed so far</param>
        public void UpdateProgress(int noCalculationElementsCompleted)
        {
            // Do nothing
        }
        /// <summary>
        /// Output a message (e.g. an error message)
        /// </summary>
        /// <param name="message">Message to output</param>
        public void OutputMessage(string message)
        {
            // Do nothing
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
        /// Constructor
        /// </summary>
        public DefaultProgressReporter()
        {
            // Do nothing
        }
    }
}
