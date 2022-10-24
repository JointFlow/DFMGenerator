using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Series of FractureCalculationData objects containing cached data from all previous timesteps
    /// </summary>
    class FCD_List
    {
        // Data
        /// <summary>
        /// Component related to layer thickness to include in Cum_hGamma: for b!=2, (h/2)^(1/beta); for b=2, ln(h/2)
        /// </summary>
        private double h_factor { get; set; }
        /// <summary>
        /// List of FractureCalculationData objects, one per timestep
        /// </summary>
        private List<FractureCalculationData> dataList;
        /// <summary>
        /// Maximum rounding error that can be generated when calculating timestep duration from driving stress, horizontal strain increment from timestep duration, and driving stress from horizontal strain. Used to determine if initial driving stress is really negative
        /// // Can also be used to check if normal stress on the fracture is really negative
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
        /// Flag for the current stage of evolution of the fracture dip set
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
        /// Factor related to microfracture propagation rate for timestep M: (A / |beta|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b!=2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getuFPropagationRateFactor(int Timestep_M) { return dataList[Timestep_M].gamma_InvBeta_M; }
        /// <summary>
        /// Factor related to microfracture growth during timestep M: -inv_gamma_factor * M_duration (b less than or equal to 2) or +inv_gamma_factor * M_duration (b greater than 2) in this gridblock for timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getuFGrowthFactor(int Timestep_M) { return dataList[Timestep_M].gamma_Duration_M; }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCum_Gamma_M(int Timestep_M) { return dataList[Timestep_M].Cum_Gamma_M; }
        /// <summary>
        /// For b!=2, (h/2)^(1/beta) + Cum_Gamma_Mminus1; for b=2, ln(h/2) + Cum_Gamma_Mminus1 (m^(1+b/2))
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCum_hGamma_M(int Timestep_M) { return (h_factor + dataList[Timestep_M].Cum_Gamma_M); }
        /// <summary>
        /// Mean half-macrofracture propagation rate during timestep M (m/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMeanMFPropagationRate(int Timestep_M) { return dataList[Timestep_M].Mean_MF_PropagationRate_M; }
        /// <summary>
        /// Distance that an active half-macrofracture in this gridblock will propagate during timestep M (m)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getHalfLength(int Timestep_M) { return dataList[Timestep_M].halfLength_M; }
        /// <summary>
        /// Total half-length at the end of timestep M of a half-macrofracture that nucleated in this gridblock at time 0 (m)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeHalfLength(int Timestep_M) { return dataList[Timestep_M].Cum_HalfLength_M; }
        /// <summary>
        /// Total half-length at the end of timestep N of a half-macrofracture that nucleated in this gridblock at the end of timestep M (m)
        /// </summary>
        /// <param name="Timestep_N">Timestep N</param>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeHalfLength(int Timestep_N, int Timestep_M) { return (Timestep_N < Timestep_M ? 0 : (dataList[Timestep_N].Cum_HalfLength_M - dataList[Timestep_M].Cum_HalfLength_M)); }
        /// <summary>
        /// Mean probability of microfracture deactivation by falling into a macrofracture stress shadow in this gridblock, during timestep M (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMean_qiI(int Timestep_M) { return dataList[Timestep_M].Mean_qiI_M; }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativeTheta(int Timestep_M) { return dataList[Timestep_M].theta_M; }
        /// <summary>
        /// Mean probability of a microfracture in this gridblock falling into a macrofracture exclusion zone, during timestep M (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMean_qiI_dashed(int Timestep_M) { return dataList[Timestep_M].Mean_qiI_dashed_M; }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
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
        /// Macrofracture spacing distribution coefficient
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getAA_M(int Timestep_M) { return dataList[Timestep_M].AA_M; }
        /// <summary>
        /// Macrofracture spacing distribution exponent
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getBB_M(int Timestep_M) { return dataList[Timestep_M].BB_M; }
        /// <summary>
        /// Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCCStep_M(int Timestep_M) { return dataList[Timestep_M].CCstep_M; }
        /// <summary>
        /// Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getdChi_dMFP32_M(int Timestep_M) { return dataList[Timestep_M].dChi_dMFP32_M; }
        /// <summary>
        /// Rate of change of exclusion zone volume relative to stress shadow volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / (1 - theta): will drop from 2 (when Psi = 0) to 1 (as Chi approaches 1)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getdChi_dPsi_M(int Timestep_M) { return dataList[Timestep_M].dChi_dPsi_M; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by stress shadow interaction during timestep M, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMeanFII(int Timestep_M) { return dataList[Timestep_M].Mean_FII_M; }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by stress shadow interaction during timestep M, as a proportion of current fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getInstantaneousFII(int Timestep_M) { return dataList[Timestep_M].Instantaneous_FII_M; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by intersecting another fracture set during timestep M, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMeanFIJ(int Timestep_M) { return dataList[Timestep_M].Mean_FIJ_M; }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during timestep M, as a proportion of current fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getInstantaneousFIJ(int Timestep_M) { return dataList[Timestep_M].Instantaneous_FIJ_M; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation during timestep M, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMeanF(int Timestep_M) { return dataList[Timestep_M].Mean_F_M; }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation during timestep M, as a proportion of current fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getInstantaneousF(int Timestep_M) { return dataList[Timestep_M].Instantaneous_F_M; }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getPhiII(int Timestep_M) { return dataList[Timestep_M].Phi_II_M; }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getPhiIJ(int Timestep_M) { return dataList[Timestep_M].Phi_IJ_M; }
        /// <summary>
        /// Probability that a half-macrofracture active in this gridblock at the start of timestep M is still active at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getPhi(int Timestep_M) { return dataList[Timestep_M].Phi_M; }
        /// <summary>
        /// Cumulative probability that a half-macrofracture nucleated in this gridblock at time 0 is still active at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativePhi(int Timestep_M) { return dataList[Timestep_M].Cum_Phi_M; }
        /// <summary>
        /// Cumulative probability that a half-macrofracture nucleated in this gridblock at the end of timestep M is still active at the start of timestep N
        /// </summary>
        /// <param name="Timestep_N">Timestep N</param>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getCumulativePhi(int Timestep_N, int Timestep_M) { double cum_phi_m = dataList[Timestep_M].Cum_Phi_M; return (Timestep_N < Timestep_M ? 1 : (cum_phi_m == 0 ? 0 : dataList[Timestep_N].Cum_Phi_M / cum_phi_m)); }
        /// <summary>
        /// Volumetric density of all active half-macrofractures, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double geta_MFP30_M(int Timestep_M) { return dataList[Timestep_M].a_MFP30_M; }
        /// <summary>
        /// Volumetric density of all static half-macrofractures terminated due to stress shadow interaction, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getsII_MFP30_M(int Timestep_M) { return dataList[Timestep_M].sII_MFP30_M; }
        /// <summary>
        /// Volumetric density of all static half-macrofractures terminated due to intersection, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getsIJ_MFP30_M(int Timestep_M) { return dataList[Timestep_M].sIJ_MFP30_M; }
        /// <summary>
        /// Volumetric density of all half-macrofractures, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getTotal_MFP30_M(int Timestep_M) { return dataList[Timestep_M].Total_MFP30_M; }
        /// <summary>
        /// Mean linear density of all microfractures, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M"></param>
        /// <returns></returns>
        public double getTotal_uFP32_M(int Timestep_M) { return dataList[Timestep_M].Total_uFP32_M; }
        /// <summary>
        /// Mean linear density of all half-macrofractures, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_MFP32_M(int Timestep_M) { return dataList[Timestep_M].Total_MFP32_M; }
        /// <summary>
        /// Volumetric ratio of all microfractures, static and dynamic, at the end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getTotal_uFP33_M(int Timestep_M) { return dataList[Timestep_M].Total_uFP33_M; }
        /// <summary>
        /// Azimuthal component of mean macrofracture stress shadow width
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMean_AzimuthalStressShadowWidth_M(int Timestep_M) { return dataList[Timestep_M].Mean_AzimuthalStressShadowWidth_M; }
        /// <summary>
        /// Strike-slip shear component of mean macrofracture stress shadow width
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMean_ShearStressShadowWidth_M(int Timestep_M) { return dataList[Timestep_M].Mean_ShearStressShadowWidth_M; }
        /// <summary>
        /// Mean macrofracture stress shadow width
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns></returns>
        public double getMean_StressShadowWidth_M(int Timestep_M) { return dataList[Timestep_M].Mean_StressShadowWidth_M; }

        // Functions to add data
        /// <summary>
        /// Add a new timestep by passing in a FractureCalculationData object
        /// </summary>
        /// <param name="fcd_in">Reference to a FractureCalculationData object containing data for the new timestep</param>
        /// <param name="AddReference">Set true to add a reference to the input FractureCalculationData object to the list; set false to add a copy of the input FractureCalculationData object to the list</param>
        public void AddTimestep(FractureCalculationData fcd_in, bool AddReference)
        {
            // Add the new FractureCalculationData object to the list
            if (AddReference) // If AddReference is true, we will add a reference to the input FractureCalculationData object; any later changes to this object will automatically update the list as well
                dataList.Add(fcd_in);
            else // If AddReference is false, we will add a copy of the input FractureCalculationData object, not the original
                dataList.Add(new FractureCalculationData(fcd_in));
        }
        /// <summary>
        /// Replace the FractureCalculationData object for the most recent timestep with a new one - useful when rolling back calculations
        /// </summary>
        /// <param name="fcd_in">Reference to a FractureCalculationData object containing new data for the timestep</param>
        /// <param name="ReplaceWithReference">Set true to replace the last item in the list with a reference to the input FractureCalculationData object; set false to replace the last item in the list with a copy of the input FractureCalculationData object</param>
        public void ReplaceLastTimestep(FractureCalculationData fcd_in, bool ReplaceWithReference)
        {
            // Relace the last FractureCalculationData object in the list with the new one; NB we must make sure to add a copy of the input FractureCalculationData object, not the original
            int lastTimestep = NoTimesteps;

            if (ReplaceWithReference) // If ReplaceWithReference is true, we will replace the last item in the list with a reference to the input FractureCalculationData object; any later changes to this object will automatically update the list as well
                dataList[lastTimestep] = fcd_in;
            else // If ReplaceWithReference is false, we will replace the last item in the list with a copy of the input FractureCalculationData object, not the original
                dataList[lastTimestep] = new FractureCalculationData(fcd_in);
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
        /// Default constructor: create a new FractureCalculationData object with default values for timestep 0
        /// </summary>
        /// <param name="initial_h_component">Component related to layer thickness to include in Cum_hGamma: ln(h/2) for b=2; (h/2)^(1/beta) for b!=2</param>
        public FCD_List(double initial_h_component) : this (initial_h_component, null, true)
        {
            // Pass a null reference to a FractureCalculationData to the copy constructor, so it will create a new default FractureCalculationData object for timestep 0
        }
        /// <summary>
        /// Copy constructor: use data from a supplied FractureCalculationData object for timestep 0
        /// </summary>
        /// <param name="initial_h_component">Component related to layer thickness to include in Cum_hGamma: ln(h/2) for b=2; (h/2)^(1/beta) for b!=2</param>
        /// <param name="fcd_in">Reference to FractureCalculationData object containing initial data for timestep 0</param>
        /// <param name="AddReference">Set true to add a reference to the input FractureCalculationData object to the list; set false to add a copy of the input FractureCalculationData object to the list</param>
        public FCD_List(double initial_h_component, FractureCalculationData fcd_in, bool AddReference)
        {
            // Set initial h factor
            h_factor = initial_h_component;

            // Set the initial maximum driving stress rounding error to 0
            MaxDrivingStressRoundingError = 0;

            // Create a new list of FractureCalculationData objects
            dataList = new List<FractureCalculationData>();

            // Add a new FractureCalculationData object for timestep 0, based on input FractureCalculationData object if supplied
            if (fcd_in == null)
                dataList.Add(new FractureCalculationData());
            else if (AddReference)
                dataList.Add(fcd_in);
            else
                dataList.Add(new FractureCalculationData(fcd_in));
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing FCD_List object
        /// </summary>
        /// <param name="PreviousFractureData">Reference to an FCD_List object containing data</param>
        public FCD_List(FCD_List PreviousFractureData)
        {
            // Set initial h factor
            h_factor = PreviousFractureData.h_factor;

            // Set the rounding error in the maximum driving stress
            MaxDrivingStressRoundingError = PreviousFractureData.MaxDrivingStressRoundingError;

            // Create a new list of FractureCalculationData objects and populate it with data from the list in the input FCD_List object
            // NB although this will create a new list object, it will only copy references to the FractureCalculationData objects in that list, not generate new FractureCalculationData objects
            dataList = new List<FractureCalculationData>(PreviousFractureData.dataList);
        }
    }
}
