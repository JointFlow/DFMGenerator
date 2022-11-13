using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Slb.Ocean.Petrel.Basics;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;

using DFMGenerator_SharedCode;

namespace DFMGenerator_Ocean
{
    class PetrelProgressReporter : IProgressReporterWrapper
    {
        // References to external objects
        /// <summary>
        /// Reference to a Petrel progress bar object; the range for this should be set from 0 to 100
        /// </summary>
        private IProgress progBar;

        // Internal data
        /// <summary>
        /// Total number of calculation elements - can be number of gridblocks, number of timesteps, or total number of gridblock timesteps
        /// </summary>
        private int TotalCalculationElements { get; set; }

        // Functions defined by IProgressReporterWrapper interface
        /// <summary>
        /// Set the total number of calculation elements to be calculated
        /// </summary>
        /// <param name="noTotalCalculationElements">Total number of calculation elements - can be number of gridblocks, number of timesteps, or total number of gridblock timesteps</param>
        public void SetNumberOfElements(int noTotalCalculationElements)
        {
            TotalCalculationElements = noTotalCalculationElements;
        }
        /// <summary>
        /// Write an update on the progress of the calculation to Console, if the next update point has been reached
        /// </summary>
        /// <param name="noCalculationElementsCompleted">Number of calculation elements completed so far</param>
        public void UpdateProgress(int noCalculationElementsCompleted)
        {
            if (TotalCalculationElements > 0)
            {
                int percentageComplete = (int)(100d * ((double)noCalculationElementsCompleted / (double)TotalCalculationElements));
                progBar.ProgressStatus = percentageComplete;
            }
        }
        /// <summary>
        /// Flag to abort calculation; returns value of the Petrel progress bar IsCanceled property
        /// </summary>
        /// <returns>True if calculation should be aborted</returns>
        public bool abortCalculation()
        {
            return progBar.IsCanceled;
        }

        // Constructors
        public PetrelProgressReporter(IProgress progBar_in)
        {
            // Set the reference to the Petrel progress bar object
            progBar = progBar_in;

            // By default, set the total number of calculations elements to zero - no updates will be issued until this is set
            TotalCalculationElements = 0;
        }

    }
}
