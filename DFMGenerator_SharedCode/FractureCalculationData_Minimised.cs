using System;
using System.Collections.Generic;
using System.Text;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Minimised version of the FractureCalculationData cache for data from a previous timestep; contains only the data required for explicit DFN Generation or data output
    /// </summary>
    class FractureCalculationData_Minimised
    {
        /// <summary>
        /// Real time at start of timestep (s)
        /// </summary>
        public double M_StartTime { get; set; }
        /// <summary>
        /// Timestep duration (s)
        /// </summary>
        public double M_Duration { get; private set; }
        /// <summary>
        /// Real time at end of timestep (s)
        /// </summary>
        public double M_EndTime { get { return M_StartTime + M_Duration; } }
        /// <summary>
        /// Constant component of effective normal stress on the fracture for timestep M
        /// </summary>
        public double SigmaNeff_Const_M { get; set; }
        /// <summary>
        /// Variable component of effective normal stress on the fracture for timestep M
        /// </summary>
        public double SigmaNeff_Var_M { get; set; }
        /// <summary>
        /// Effective normal stress on the fracture at the end of timestep M
        /// </summary>
        public double SigmaNeff_Final_M { get { return SigmaNeff_Const_M + (SigmaNeff_Var_M * M_Duration); } }
        /// <summary>
        /// Constant component of driving stress for timestep M
        /// </summary>
        public double U_M { get; set; }
        /// <summary>
        /// Variable component of driving stress for timestep M
        /// </summary>
        public double V_M { get; set; }
        /// <summary>
        /// Weighted mean driving stress during timestep M (Pa)
        /// </summary>
        public double Mean_SigmaD_M { get; private set; }
        /// <summary>
        /// Mean driving stress at the end of timestep M (Pa)
        /// </summary>
        public double Final_SigmaD_M { get { return U_M + (V_M * M_Duration); } }
        /*/// <summary>
        /// Factor related to fracture propagation rate for timestep M: (A / |beta|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b!=2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
        /// </summary>
        public double gamma_InvBeta_M { get; private set; }
        /// <summary>
        /// Factor related to fracture growth during timestep M: -inv_gamma_factor * M_duration (b less than or equal to 2) or +inv_gamma_factor * M_duration (b greater than 2) in this gridblock for timestep M
        /// </summary>
        public double gamma_Duration_M { get { return (M_bType == bType.GreaterThan2 ? gamma_InvBeta_M * M_Duration : -gamma_InvBeta_M * M_Duration); } }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M-1 (m^(1+b/2))
        /// </summary>
        public double Cum_Gamma_Mminus1 { get { return Cum_Gamma_M - gamma_Duration_M; } set { Cum_Gamma_M = value + gamma_Duration_M; } }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
        /// </summary>
        public double Cum_Gamma_M { get; set; }*/
        /*/// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at start of timestep M
        /// </summary>
        public double theta_Mminus1 { get; private set; }*/
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
        /// </summary>
        public double theta_M { get; private set; }
        /*/// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at start of timestep M
        /// </summary>
        public double theta_dashed_Mminus1 { get; private set; }*/
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
        /// </summary>
        public double theta_dashed_M { get; private set; }
        /// <summary>
        /// Volumetric density of all fully active rays, at the end of timestep M
        /// </summary>
        public double a_RP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all restricted rays, at the end of timestep M
        /// </summary>
        public double r_RP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all static rays terminated due to stress shadow interaction, at the end of timestep M
        /// </summary>
        public double sII_RP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all static rays terminated due to intersection, at the end of timestep M
        /// </summary>
        public double sIJ_RP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all rays, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_RP30_M { get { return a_RP30_M + r_RP30_M + sII_RP30_M + sIJ_RP30_M; } }
        /*/// <summary>
        /// Volumetric density of all rays from other fracture sets that terminate against rays from this dipset, at the end of timestep M
        /// </summary>
        public double TerminatingRayDensity_M { get; set; }
        /// <summary>
        /// Mean number of rays from other fracture sets that terminate against a fracture from this dipset, at the end of timestep M
        /// </summary>
        public double TerminatingRaysPerR_M { get { return Total_RP30_M > 0 ? TerminatingRayDensity_M / Total_RP30_M : 0; } }
        /// <summary>
        /// P31 value for all rays, static and dynamic, at the end of timestep M
        /// This represents the sum of the lengths of every ray
        /// It is used to calculate the fracture permeability corrected for fracture size distribution
        /// </summary>
        public double Total_RP31_M { get; private set; }*/
        /// <summary>
        /// Mean linear density of all rays, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_RP32_M { get; private set; }
        /// <summary>
        /// Volumetric ratio of all rays, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_RP33_M { get; private set; }
        /*/// <summary>
        /// P35 value for all rays, static and dynamic, at the end of timestep M
        /// This represents the combined integral of R^3 across the area of every fracture segment, where R is the ray length
        /// It is used to calculate the fracture permeability where fracture aperture is size-dependent, and permeability is proportional to aperture cubed
        /// </summary>
        public double Total_RP35_M { get; private set; }*/
        /// <summary>
        /// Piecewise population distribution function (not cumulative) for total ray volumetric density, at the end of timestep M
        /// NB This list will only be instantiated if required to calculate microfracture permeability
        /// </summary>
        public double[] DRP30_distribution_M { get; private set; }
        /// <summary>
        /// Azimuthal component of mean fracture stress shadow width
        /// </summary>
        public double Mean_AzimuthalStressShadowWidth_M { get; private set; }
        /// <summary>
        /// Strike-slip shear component of mean fracture stress shadow width
        /// </summary>
        public double Mean_ShearStressShadowWidth_M { get { return Mean_StressShadowWidth_M - Mean_AzimuthalStressShadowWidth_M; } }
        /// <summary>
        /// Mean fracture stress shadow width
        /// </summary>
        public double Mean_StressShadowWidth_M { get; private set; }

        // Reset and data input functions
        /// <summary>
        /// Update the timestep duration, mean driving stress, mean macrofracture propagation rate and microfracture propagation rate coefficient (= gamma ^ 1/beta); all related data will be updated automatically
        /// </summary>
        /// <param name="duration_in">New timestep duration (s); set to -1 to keep current value</param>
        /// <param name="meanSigmaD_in">New mean driving stress (Pa); set to -1 to keep current value</param>
        /// <param name="gammaInvBeta_in">Microfracture propagation rate coefficient (= gamma ^ 1/beta)</param>
        /// <param name="meanMFPropRate_in">Mean macrofracture propagation rate</param>
        public void SetDynamicData(double duration_in, double meanSigmaD_in, double gammaInvBeta_in, double meanMFPropRate_in)
        {
            // Before updating the dynamic data for timestep M we must cache the cumulative data at the start of the timestep Cum_Gamma_Mminus1
            //double temp_Cum_Gamma = Cum_Gamma_Mminus1;

            // Set the new timestep duration - if input value <0 keep old data
            if (duration_in >= 0) M_Duration = duration_in;

            // Set the new driving stress - if input value <0 keep old data
            if (meanSigmaD_in >= 0) Mean_SigmaD_M = meanSigmaD_in;

            // Set the mean fracture propagation rate coefficient
            //gamma_InvBeta_M = gammaInvBeta_in;

            // Update the cumulative data for the start of this timestep; the FractureCalculationData object will then automatically calculate the cumulative data for the end of this timestep
            //Cum_Gamma_Mminus1 = temp_Cum_Gamma;
        }
        /// <summary>
        /// Set values for the ray density indices a_RP30, r_RP30, sII_RP30, sIJ_RP30, RP32 and RP33 at the end of the timestep
        /// </summary>
        /// <param name="a_RP30_in">Volumetric density of fully active rays (a_RP30) at end of timestep M</param>
        /// <param name="a_RP30_in">Volumetric density of restricted rays (r_RP30) at end of timestep M</param>
        /// <param name="sII_RP30_in">Volumetric density of static rays terminated due to stress shadow interaction (sII_RP30) at end of timestep M</param>
        /// <param name="sIJ_RP30_in">Volumetric density of static rays terminated due to intersection (sIJ_RP30) at end of timestep M</param>
        /// <param name="Total_RP32_in">Total mean linear density of rays (Total_RP32) at end of timestep M</param>
        /// <param name="Total_RP33_in">Total volumetric ratio of rays (Total_RP33) at end of timestep M</param>
        public void SetMacrofractureDensityData(double a_RP30_in, double r_RP30_in, double sII_RP30_in, double sIJ_RP30_in, double Total_RP32_in, double Total_RP33_in)
        {
            // Set the new values for all ray densities at the end of the timestep
            a_RP30_M = a_RP30_in;
            r_RP30_M = r_RP30_in;
            sII_RP30_M = sII_RP30_in;
            sIJ_RP30_M = sIJ_RP30_in;
            Total_RP32_M = Total_RP32_in;
            Total_RP33_M = Total_RP33_in;
        }
        /*/// <summary>
        /// Set values for the ray density indices a_RP30, r_RP30, sII_RP30, sIJ_RP30, RP31, RP32, RP33 and RP35 at the end of the timestep
        /// </summary>
        /// <param name="a_RP30_in">Volumetric density of fully active rays (a_RP30) at end of timestep M</param>
        /// <param name="a_RP30_in">Volumetric density of restricted rays (r_RP30) at end of timestep M</param>
        /// <param name="sII_RP30_in">Volumetric density of static rays terminated due to stress shadow interaction (sII_RP30) at end of timestep M</param>
        /// <param name="sIJ_RP30_in">Volumetric density of static rays terminated due to intersection (sIJ_RP30) at end of timestep M</param>
        /// <param name="Total_RP31_in">Sum of the lengths of every ray in a unit volume at end of timestep M</param>
        /// <param name="Total_RP32_in">Total mean linear density of rays (Total_RP32) at end of timestep M</param>
        /// <param name="Total_RP33_in">Total volumetric ratio of rays (Total_RP33) at end of timestep M</param>
        /// <param name="Total_RP35_in">Sum of the integral of R^3 across the area of every fracture segment, where R is the ray length, at end of timestep M</param>
        public void SetMacrofractureDensityData(double a_RP30_in, double r_RP30_in, double sII_RP30_in, double sIJ_RP30_in, double Total_RP31_in, double Total_RP32_in, double Total_RP33_in, double Total_RP35_in)
        {
            // Set the new values for all ray densities at the end of the timestep
            a_RP30_M = a_RP30_in;
            r_RP30_M = r_RP30_in;
            sII_RP30_M = sII_RP30_in;
            sIJ_RP30_M = sIJ_RP30_in;
            Total_RP31_M = Total_RP31_in;
            Total_RP32_M = Total_RP32_in;
            Total_RP33_M = Total_RP33_in;
            Total_RP35_M = Total_RP35_in;
        }*/
        /*/// <summary>
        /// Set the piecewise population distribution function (not cumulative) for total fracture volumetric density at the end of the timestep, based on active and static fracture volumetric density arrays
        /// </summary>
        /// <param name="a_DRP30_distribution_in">Array representing the active fracture volumetric density distribution function at the end of timestep M</param>
        /// <param name="s_DRP30_distribution_in">Array representing the static fracture volumetric density distribution function at the end of timestep M</param>
        public void SetMicrofractureDistributionData(double[] a_DRP30_distribution_in, double[] s_DRP30_distribution_in)
        {
            // Get the size of the input arrays
            int no_r_bins = Math.Min(a_DRP30_distribution_in.Length, s_DRP30_distribution_in.Length);

            // Copy the density data from the input active and static fracture density arrays into the total fracture density distribution array
            DRP30_distribution_M = new double[no_r_bins];
            for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                DRP30_distribution_M[r_bin] = a_DRP30_distribution_in[r_bin] + s_DRP30_distribution_in[r_bin];
        }*/
        /// <summary>
        /// Set values for the mean total and azimuthal stress shadow width, at the end of the timestep
        /// </summary>
        /// <param name="Mean_AzimuthalStressShadowWidth_in">Azimuthal component of mean stress shadow width at end of timestep M</param>
        /// <param name="Mean_StressShadowWidth_in">Mean stress shadow width at end of timestep M</param>
        public void SetStressShadowWidth(double Mean_AzimuthalStressShadowWidth_in, double Mean_StressShadowWidth_in)
        {
            // Set the new values for mean total and azimuthal stress shadow width at the end of the timestep
            // The mean shear stress shadow width is calculated from the total and azimuthal widths
            // NB we do not set the inverse stress shadow volume theta here, as this may not be equal to 1 - (MFP32 * W) if the stress shadow width varies through time
            // In this case it will be controlled by the cumulative macrofracture spacing distribution function, so is set in the SetFractureExclusionZoneData function

            // Check if the input stress shadow widths are negative, and if so set them to 0
            if (Mean_AzimuthalStressShadowWidth_in < 0)
                Mean_AzimuthalStressShadowWidth_in = 0;
            if (Mean_StressShadowWidth_in < 0)
                Mean_StressShadowWidth_in = 0;

            // Set the new stress shadow widths
            Mean_AzimuthalStressShadowWidth_M = Mean_AzimuthalStressShadowWidth_in;
            Mean_StressShadowWidth_M = Mean_StressShadowWidth_in;
        }
        /// <summary>
        /// Set the inverse stress shadow and clear zone volume for this fracture set
        /// </summary>
        /// <param name="theta_in">Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M</param>
        /// <param name="theta_dashed_in">Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M</param>
        public void SetFractureExclusionZoneData(double theta_in, double theta_dashed_in)
        {
            // Set the inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            // NB this is set here rather than in the SetMacrofractureDensityData, because the value of psi will be controlled by the cumulative macrofracture spacing distribution function if the stress shadow width varies through time
            if ((theta_in >= 0) && (theta_in <= 1))
                theta_M = theta_in;
            // Set the clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
            if ((theta_dashed_in >= 0) && (theta_dashed_in <= 1))
                theta_dashed_M = theta_dashed_in;
        }
        /*/// <summary>
        /// Reduce the fracture propagation rate coefficient by a specified amount
        /// This will reduce growth in the populations of implicit fractures, and of explicit fractures in the DFN, in the case that the fracture dipset is deactivated within the timestep
        /// NB this will not change the values for the driving stress, U and V
        /// </summary>
        /// <param name="reductionFactor"></param>
        public void ReduceFractureGrowth(double reductionFactor)
        {
            if (reductionFactor > 0)
            {
                gamma_InvBeta_M *= reductionFactor;
            }
        }*/
        /// <summary>
        /// Create a new FractureCalculationData_Minimised object for the next timestep, and populate it based on the data for this timestep (dynamic values will be set to defaults)
        /// </summary>
        /// <returns>A new FractureCalculationData_Minimised object populated with data for the next timestep</returns>
        public FractureCalculationData_Minimised GetNextTimestepData()
        {
            // Create a new FractureCalculationData object as a copy of the current one
            FractureCalculationData_Minimised nextTimestepData = new FractureCalculationData_Minimised(this);

            // Update data
            // Start time (s): set to end date of previous timestep
            nextTimestepData.M_StartTime = M_StartTime + M_Duration;
            // Timestep duration: set to 0
            nextTimestepData.M_Duration = 0;
            // Flag for the current stage of evolution of the fracture dip set: does not change
            // Flag for whether subcritical fracture propagation index b is less than, equal to or greater than 2: does not change
            // Constant component of effective normal stress on the fracture for timestep M
            nextTimestepData.SigmaNeff_Const_M = 0;
            // Variable component of effective normal stress on the fracture for timestep M
            nextTimestepData.SigmaNeff_Var_M = 0;
            // Constant component of driving stress: set to 0
            nextTimestepData.U_M = 0;
            // Variable component of driving stress: set to 0
            nextTimestepData.V_M = 0;
            // Equivalent mean driving stress during timestep M (Pa): set to 0
            nextTimestepData.Mean_SigmaD_M = 0;
            // Helper function related to fracture propagation rate for timestep M: set to 0
            //nextTimestepData.gamma_InvBeta_M = 0;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active at start of timestep M: set to the same value as that at the end of the timestep
            // Mean_qiI_M will therefore be 0
            //nextTimestepData.theta_Mminus1 = theta_M;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone at start of timestep M: set to the same value as that at the end of the timestep
            // Mean_qiI_dashed_M will therefore be 0
            //nextTimestepData.theta_dashed_Mminus1 = theta_dashed_M;
            // Volumetric density of all active rays: does not change
            // Volumetric density of all restricted rays: does not change
            // Volumetric density of all static rays terminated due to stress shadow interaction: does not change
            // Volumetric density of all static rays terminated due to intersection: does not change
            // Mean linear density of all rays, static and dynamic: does not change
            // Volumetric ratio of all rays, static and dynamic: does not change
            // Azimuthal component of mean fracture stress shadow width: does not change
            // Mean fracture stress shadow width: does not change

            return nextTimestepData;
        }

        // Constructors
        /// <summary>
        /// Default constructor: set everything to initial values (before fractures) 
        /// </summary>
        public FractureCalculationData_Minimised()
        {
            // Start time (s)
            M_StartTime = 0;
            // Timestep duration (s)
            M_Duration = 0;
            // Constant component of effective normal stress on the fracture for timestep M
            SigmaNeff_Const_M = 0;
            // Variable component of effective normal stress on the fracture for timestep M
            SigmaNeff_Var_M = 0;
            // Constant component of driving stress
            U_M = 0;
            // Variable component of driving stress
            V_M = 0;
            // Equivalent mean driving stress during timestep M (Pa)
            Mean_SigmaD_M = 0;
            // Helper function related to fracture propagation rate for timestep M: (A / |B|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b<>2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
            //gamma_InvBeta_M = 0;
            // Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
            //Cum_Gamma_M = 0;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at start of timestep M
            //theta_Mminus1 = 1;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            theta_M = 1;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at start of timestep M
            //theta_dashed_Mminus1 = 1;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
            theta_dashed_M = 1;
            // Volumetric density of all fully active rays
            a_RP30_M = 0;
            // Volumetric density of all restricted rays
            r_RP30_M = 0;
            // Volumetric density of all static rays terminated due to stress shadow interaction, at the end of timestep M
            sII_RP30_M = 0;
            // Volumetric density of all static rays terminated due to intersection, at the end of timestep M
            sIJ_RP30_M = 0;
            // Volumetric density of all rays from other fracture sets that terminate against rays from this dipset, at the end of timestep M
            //TerminatingFractureDensity_M = 0;
            // P31 of all rays, static and dynamic, at the end of timestep M
            //Total_RP31_M = 0;
            // Mean linear density of all rays, static and dynamic, at the end of timestep M
            Total_RP32_M = 0;
            // Volumetric ratio of all rays, static and dynamic, at the end of timestep M
            Total_RP33_M = 0;
            // P35 of all rays, static and dynamic, at the end of timestep M
            //Total_RP35_M = 0;
            // Piecewise population distribution function (not cumulative) for total ray volumetric density, at the end of timestep M
            // NB This list will only be instantiated if required to calculate fracture permeability
            //DRP30_distribution_M = null;
            // Azimuthal component of mean fracture stress shadow width
            Mean_AzimuthalStressShadowWidth_M = 0;
            // Mean fracture stress shadow width
            Mean_StressShadowWidth_M = 0;
        }
        /// <summary>
        /// Constructor: specify start time and timestep duration, set everything else to zero
        /// </summary>
        /// <param name="StartTime_in">Real time at start of timestep (s)</param>
        /// <param name="Duration_in">Timestep duration (s)</param>
        public FractureCalculationData_Minimised(double StartTime_in, double Duration_in) : this()
        {
            // Start time (s)
            M_StartTime = StartTime_in;
            // Timestep duration
            M_Duration = Duration_in;
        }
        /// <summary>
        /// Copy constructor: copy all data from the input FractureCalculationData_Minimised object into the new FractureCalculationData_Minimised object
        /// </summary>
        /// <param name="fcd_in">Reference to FractureCalculationData_Minimised object to copy</param>
        public FractureCalculationData_Minimised(FractureCalculationData_Minimised fcd_in)
        {
            // Start time (s)
            M_StartTime = fcd_in.M_StartTime;
            // Timestep duration
            M_Duration = fcd_in.M_Duration;
            // Constant component of effective normal stress on the fracture for timestep M
            SigmaNeff_Const_M = fcd_in.SigmaNeff_Const_M;
            // Variable component of effective normal stress on the fracture for timestep M
            SigmaNeff_Var_M = fcd_in.SigmaNeff_Var_M;
            // Constant component of driving stress
            U_M = fcd_in.U_M;
            // Variable component of driving stress
            V_M = fcd_in.V_M;
            // Equivalent mean driving stress during timestep M (Pa)
            Mean_SigmaD_M = fcd_in.Mean_SigmaD_M;
            // Helper function related to fracture propagation rate for timestep M: (A / |B|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b<>2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
            //gamma_InvBeta_M = fcd_in.gamma_InvBeta_M;
            // Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
            //Cum_Gamma_M = fcd_in.Cum_Gamma_M;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at start of timestep M
            //theta_Mminus1 = fcd_in.theta_Mminus1;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            theta_M = fcd_in.theta_M;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at start of timestep M
            //theta_dashed_Mminus1 = fcd_in.theta_dashed_Mminus1;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a fracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
            theta_dashed_M = fcd_in.theta_dashed_M;
            // Volumetric density of all fully active rays
            a_RP30_M = fcd_in.a_RP30_M;
            // Volumetric density of all restricted rays
            r_RP30_M = fcd_in.r_RP30_M;
            // Volumetric density of all static rays terminated due to stress shadow interaction, at the end of timestep M
            sII_RP30_M = fcd_in.sII_RP30_M;
            // Volumetric density of all static rays terminated due to intersection, at the end of timestep M
            sIJ_RP30_M = fcd_in.sIJ_RP30_M;
            // Volumetric density of all rays from other fracture sets that terminate against rays from this dipset, at the end of timestep M
            //TerminatingFractureDensity_M = fcd_in.TerminatingFractureDensity_M;
            // P31 of all rays, static and dynamic, at the end of timestep M
            //Total_RP31_M = fcd_in.Total_RP31_M;
            // Mean linear density of all rays, static and dynamic, at the end of timestep M
            Total_RP32_M = fcd_in.Total_RP32_M;
            // Volumetric ratio of all rays, static and dynamic, at the end of timestep M
            Total_RP33_M = fcd_in.Total_RP33_M;
            // P35 of all rays, static and dynamic, at the end of timestep M
            //Total_RP35_M = fcd_in.Total_RP35_M;
            // Piecewise population distribution function (not cumulative) for total ray volumetric density, at the end of timestep M
            // NB This list will only be instantiated if required to calculate fracture permeability
            //DRP30_distribution_M = null;
            // Azimuthal component of mean fracture stress shadow width
            Mean_AzimuthalStressShadowWidth_M = fcd_in.Mean_AzimuthalStressShadowWidth_M;
            // Mean fracture stress shadow width
            Mean_StressShadowWidth_M = fcd_in.Mean_StressShadowWidth_M;
        }
    }
}
