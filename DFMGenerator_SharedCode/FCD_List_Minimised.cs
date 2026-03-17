using System;
using System.Collections.Generic;
using System.Text;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Series of FractureCalculationData_Minimised objects containing cached data for explicit DFN Generation or data output from all previous timesteps
    /// </summary>
    class FCD_List_Minimised
    {
        // Data
        /// <summary>
        /// List of FractureCalculationData_Minimised objects, one per timestep
        /// </summary>
        private List<FractureCalculationData_Minimised> dataList;
        /// <summary>
        /// Maximum rounding error that can be generated when calculating timestep duration from driving stress, horizontal strain increment from timestep duration, and driving stress from horizontal strain. Used to determine if initial driving stress is really negative
        /// Can also be used to check if normal stress on the fracture is really negative
        /// </summary>
        public double MaxDrivingStressRoundingError { get; private set; }

        // Functions to get data
        /// <summary>
        /// Return the number of active timesteps (i.e. not including the initial timestep 0)
        /// </summary>
        public int NoTimesteps { get { return dataList.Count - 1; } }
        /// <summary>
        /// Time at start of timestep M (s) 
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getStartTime(int Timestep_M) { return dataList[Timestep_M].M_StartTime; }
        /// <summary>
        /// Duration of timestep M (s) 
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getDuration(int Timestep_M) { return dataList[Timestep_M].M_Duration; }
        /// <summary>
        /// Time at end of timestep M (s) 
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getEndTime(int Timestep_M) { return dataList[Timestep_M].M_EndTime; }
        /// <summary>
        /// Flag for the current stage of evolution of the fracture set
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public FractureEvolutionStage getEvolutionStage(int Timestep_M) { return dataList[Timestep_M].EvolutionStage; }
        /// <summary>
        /// Constant component of effective normal stress on the fracture for timestep M (Pa)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getConstantNormalStress(int Timestep_M) { return dataList[Timestep_M].SigmaNeff_Const_M; }
        /// <summary>
        /// Variable component of effective normal stress on the fracture for timestep M (Pa)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getVariableNormalStress(int Timestep_M) { return dataList[Timestep_M].SigmaNeff_Var_M; }
        /// <summary>
        /// Effective normal stress on the fracture at the end of timestep M (Pa)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getFinalNormalStress(int Timestep_M) { return dataList[Timestep_M].SigmaNeff_Final_M; }
        /// <summary>
        /// Constant component of driving stress for timestep M (Pa) 
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getConstantDrivingStressU(int Timestep_M) { return dataList[Timestep_M].U_M; }
        /// <summary>
        /// Variable component of driving stress for timestep M (Pa/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getVariableDrivingStressV(int Timestep_M) { return dataList[Timestep_M].V_M; }
        /// <summary>
        /// Weighted mean driving stress during timestep M (Pa)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMeanDrivingStressSigmaD(int Timestep_M) { return dataList[Timestep_M].Mean_SigmaD_M; }
        /// <summary>
        /// Mean driving stress at the end of timestep M (Pa)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getFinalDrivingStressSigmaD(int Timestep_M) { return dataList[Timestep_M].Final_SigmaD_M; }
        /// <summary>
        /// Factor related to fracture propagation rate for timestep M: (A / |beta|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b!=2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getFPropagationRateFactor(int Timestep_M) { return dataList[Timestep_M].gamma_InvBeta_M; }
        /// <summary>
        /// Factor related to fracture growth in this gridblock during timestep M: -inv_gamma_factor * M_duration (b less than or equal to 2) or +inv_gamma_factor * M_duration (b greater than 2)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getFGrowthFactor(int Timestep_M) { return dataList[Timestep_M].gamma_Duration_M; }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCum_Gamma_M(int Timestep_M) { return dataList[Timestep_M].Cum_Gamma_M; }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration from the end of timestep M to the end of timestep N
        /// </summary>
        /// <param name="Timestep_N">Timestep N</param>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getFGrowthFactor(int Timestep_N, int Timestep_M) { return (Timestep_N < Timestep_M ? 0 : (dataList[Timestep_N].Cum_Gamma_M - dataList[Timestep_M].Cum_Gamma_M)); }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeTheta(int Timestep_M) { return dataList[Timestep_M].theta_M; }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeThetaDashed(int Timestep_M) { return dataList[Timestep_M].theta_dashed_M; }
        /// <summary>
        /// Inverse stress shadow volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the stress shadow of any fracture set, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeTheta_AllFS_M(int Timestep_M) { return dataList[Timestep_M].theta_allFS_M; }
        /// <summary>
        /// Clear zone volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the exclusion zone of any fracture set, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeThetaDashed_AllFS_M(int Timestep_M) { return dataList[Timestep_M].theta_dashed_allFS_M; }
        /// <summary>
        /// Volumetric density of all fully active rays, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double geta_RP30_M(int Timestep_M) { return dataList[Timestep_M].a_RP30_M; }
        /// <summary>
        /// Volumetric density of all restricted rays, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getr_RP30_M(int Timestep_M) { return dataList[Timestep_M].r_RP30_M; }
        /// <summary>
        /// Volumetric density of all static rays terminated due to stress shadow interaction, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getsII_RP30_M(int Timestep_M) { return dataList[Timestep_M].sII_RP30_M; }
        /// <summary>
        /// Volumetric density of all static rays terminated due to intersection, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getsIJ_RP30_M(int Timestep_M) { return dataList[Timestep_M].sIJ_RP30_M; }
        /// <summary>
        /// Volumetric density of all static rays terminated due to exceeding the maximum radius, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getsRMax_RP30_M(int Timestep_M) { return dataList[Timestep_M].sRMax_RP30_M; }
        /// <summary>
        /// Volumetric density of all rays, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_RP30_M(int Timestep_M) { return dataList[Timestep_M].Total_RP30_M; }
        /// <summary>
        /// Volumetric density of all unconfined fractures from other fracture sets that terminate against fractures from this set, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTerminatingFractureDensity_M(int Timestep_M) { return dataList[Timestep_M].TerminatingFractureDensity_M; }
        /*/// <summary>
        /// Volumetric density of all rays from other fracture sets that terminate against rays from this dipset, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTerminatingRayDensity_M(int Timestep_M) { return dataList[Timestep_M].TerminatingRayDensity_M; }
        /// <summary>
        /// Mean number of rays from other fracture sets that terminate against a fracture from this dipset, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTerminatingRaysPerR_M(int Timestep_M) { return dataList[Timestep_M].TerminatingRaysPerR_M; }
        /// <summary>
        /// P31 value for all rays, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_RP31_M(int Timestep_M) { return dataList[Timestep_M].Total_RP31_M; }*/
        /// <summary>
        /// Mean linear density of all rays, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_RP32_M(int Timestep_M) { return dataList[Timestep_M].Total_RP32_M; }
        /// <summary>
        /// Maximum volumetric ratio of all rays, not accounting for overlap, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_RP33_M(int Timestep_M) { return dataList[Timestep_M].Total_RP33_M; }
        /*/// <summary>
        /// P35 value for all rays, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_RP35_M(int Timestep_M) { return dataList[Timestep_M].Total_RP35_M; }
        /// <summary>
        /// Piecewise population distribution function (not cumulative) for total ray volumetric density, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double[] getDRP30_distribution_M(int Timestep_M) { return dataList[Timestep_M].DRP30_distribution_M; }*/
        /// <summary>
        /// Increment of displacement on the fracture resulting from an increment in the applied strain, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public VectorXYZ getIncrementalDisplacement_M(int Timestep_M) { return dataList[Timestep_M].IncrementalDisplacement_M; }
        /// <summary>
        /// Increment of applied strain acting on the fracture, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public VectorXYZ getIncrementalStrainOnFracture_M(int Timestep_M) { return dataList[Timestep_M].IncrementalStrainOnFracture_M; }
        /// <summary>
        /// Ratio of the maximum fracture stress shadow width to effective fracture radius, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getStressShadowWidthRatio_M(int Timestep_M) { return dataList[Timestep_M].StressShadowWidthRatio_M; }
        /*/// <summary>
        /// Get the weighted average stress shadow width throughout the growth of the fracture network
        /// </summary>
        /// <returns>The average of the stress shadow width during each timestep, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_StressShadowWidth()
        {
            return getWeightedAverage_StressShadowWidth(NoTimesteps);
        }
        /// <summary>
        /// Get the weighted average stress shadow width up to a specified timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns>The average of the stress shadow width during each timestep up to and including Timestep_M, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_StressShadowWidth(int Timestep_M)
        {
            double W_P32 = 0;
            double lastP32 = 0;

            for (int tsNo = 1; tsNo <= Timestep_M; tsNo++)
            {
                FractureCalculationData_Minimised nextFCD = dataList[tsNo];
                double nextP32 = nextFCD.Total_RP32_M;
                W_P32 += (nextFCD.StressShadowWidthRatio_M * (nextP32 - lastP32));
                lastP32 = nextP32;
            }

            // If there are no fractures it will not be possible to calculate a weighted average; in that case return the calculated stress shadow width for Timestep M
            return lastP32 > 0 ? W_P32 / lastP32 : dataList[Timestep_M].StressShadowWidthRatio_M;
        }*/
        /// <summary>
        /// Get the time at which the fracture set becomes deactivated
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if the fracture set was never active: if true, will return Nan; if false, will return 0</param>
        /// <returns>Deactivation time of fracture set; will return zero or NaN if the fracture set was never active</returns>
        public double getFinalActiveTime(bool ReturnNanForUndefined)
        {
            // Loop through the timesteps in reverse order
            for (int TimestepNo = NoTimesteps; TimestepNo > 0; TimestepNo--)
            {
                // Check if the dipset is active (or residual active); if so return the end time of the current timestep
                FractureEvolutionStage CurrentStage = getEvolutionStage(TimestepNo);
                if ((CurrentStage == FractureEvolutionStage.Growing) || (CurrentStage == FractureEvolutionStage.ResidualActivity))
                    return getEndTime(TimestepNo);
            }

            // If the fracture set was never active, return 0 or NaN as appropriate
            if (ReturnNanForUndefined)
                return double.NaN;
            else
                return 0;
        }

        // Functions to add data
        /// <summary>
        /// Add a new timestep by passing in a FractureCalculationData_Minimised object
        /// </summary>
        /// <param name="fcd_in">Reference to a FractureCalculationData_Minimised object containing data for the new timestep</param>
        /// <param name="AddReference">Set true to add a reference to the input FractureCalculationData_Minimised object to the list; set false to add a copy of the input FractureCalculationData_Minimised object to the list</param>
        public void AddTimestep(FractureCalculationData_Minimised fcd_in, bool AddReference)
        {
            // Add the new FractureCalculationData_Minimised object to the list
            if (AddReference) // If AddReference is true, we will add a reference to the input FractureCalculationData_Minimised object; any later changes to this object will automatically update the list as well
                dataList.Add(fcd_in);
            else // If AddReference is false, we will add a copy of the input FractureCalculationData_Minimised object, not the original
                dataList.Add(new FractureCalculationData_Minimised(fcd_in));
        }
        /// <summary>
        /// Replace the FractureCalculationData_Minimised object for the most recent timestep with a new one - useful when rolling back calculations
        /// </summary>
        /// <param name="fcd_in">Reference to a FractureCalculationData_Minimised object containing new data for the timestep</param>
        /// <param name="ReplaceWithReference">Set true to replace the last item in the list with a reference to the input FractureCalculationData_Minimised object; set false to replace the last item in the list with a copy of the input FractureCalculationData_Minimised object</param>
        public void ReplaceLastTimestep(FractureCalculationData_Minimised fcd_in, bool ReplaceWithReference)
        {
            // Relace the last FractureCalculationData_Minimised object in the list with the new one
            int lastTimestep = NoTimesteps;

            if (ReplaceWithReference) // If ReplaceWithReference is true, we will replace the last item in the list with a reference to the input FractureCalculationData_Minimised object; any later changes to this object will automatically update the list as well
                dataList[lastTimestep] = fcd_in;
            else // If ReplaceWithReference is false, we will replace the last item in the list with a copy of the input FractureCalculationData_Minimised object, not the original
                dataList[lastTimestep] = new FractureCalculationData_Minimised(fcd_in);
        }
        /// <summary>
        /// Update the maximum driving stress rounding error: set to the numerical precision factor (assumed to be 1E-12) times the driving stress specified, if this is greater than the previous maximum error
        /// </summary>
        /// <param name="drivingStress_in">Current driving stress</param>
        public void UpdateMaxDrivingStressRoundingError(double drivingStress_in)
        {
            const double precision = 1E-12;
            double newMaxError = Math.Abs(drivingStress_in) * precision;
            if (MaxDrivingStressRoundingError < newMaxError) MaxDrivingStressRoundingError = newMaxError;
        }

        // Constructors
        /// <summary>
        /// Default constructor: create a new FractureCalculationData_Minimised object with default values for timestep 0
        /// </summary>
        public FCD_List_Minimised() : this(null, true)
        {
            // Pass a null reference to a FractureCalculationData_Minimised to the copy constructor, so it will create a new default FractureCalculationData_Minimised object for timestep 0
        }
        /// <summary>
        /// Copy constructor: use data from a supplied FractureCalculationData_Minimised object for timestep 0
        /// </summary>
        /// <param name="fcd_in">Reference to FractureCalculationData_Minimised object containing initial data for timestep 0</param>
        /// <param name="AddReference">Set true to add a reference to the input FractureCalculationData object to the list; set false to add a copy of the input FractureCalculationData_Minimised object to the list</param>
        public FCD_List_Minimised(FractureCalculationData_Minimised fcd_in, bool AddReference)
        {
            // Set the initial maximum driving stress rounding error to 0
            MaxDrivingStressRoundingError = 0;

            // Create a new list of FractureCalculationData_Minimised objects
            dataList = new List<FractureCalculationData_Minimised>();

            // Add a new FractureCalculationData_Minimised object for timestep 0, based on input FractureCalculationData_Minimised object if supplied
            if (fcd_in == null)
                dataList.Add(new FractureCalculationData_Minimised());
            else if (AddReference)
                dataList.Add(fcd_in);
            else
                dataList.Add(new FractureCalculationData_Minimised(fcd_in));
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing FCD_List object
        /// </summary>
        /// <param name="PreviousFractureData">Reference to an FCD_List object containing data</param>
        public FCD_List_Minimised(FCD_List_Minimised PreviousFractureData)
        {
            // Set the rounding error in the maximum driving stress
            MaxDrivingStressRoundingError = PreviousFractureData.MaxDrivingStressRoundingError;

            // Create a new list of FractureCalculationData_Minimised objects and populate it with data from the list in the input FCD_List object
            // NB although this will create a new list object, it will only copy references to the FractureCalculationData_Minimised objects in that list, not generate new FractureCalculationData_Minimised objects
            dataList = new List<FractureCalculationData_Minimised>(PreviousFractureData.dataList);
        }
    }
}
