using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for the stages in the evolution of a fracture dip set
    /// </summary>
    public enum FractureEvolutionStage { NotActivated, Growing, ResidualActivity, Deactivated }

    /// <summary>
    /// Cache for data from a previous timestep (e.g. fracture nucleation or growth rate) that is needed for later calculation or data output
    /// </summary>
    class FractureCalculationData
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
        /// Flag for the current stage of evolution of the fracture dip set
        /// </summary>
        public FractureEvolutionStage EvolutionStage { get; private set; }
        /// <summary>
        /// Flag for whether subcritical fracture propagation index b is less than, equal to or greater than 2; this will affect the calculation of gamma_Duration_M
        /// </summary>
        public bType M_bType { get; set; }
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
        /// <summary>
        /// Factor related to microfracture propagation rate for timestep M: (A / |beta|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b!=2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
        /// </summary>
        public double gamma_InvBeta_M { get; private set; }
        /// <summary>
        /// Factor related to microfracture growth during timestep M: -inv_gamma_factor * M_duration (b less than or equal to 2) or +inv_gamma_factor * M_duration (b greater than 2) in this gridblock for timestep M
        /// </summary>
        public double gamma_Duration_M { get { return (M_bType == bType.GreaterThan2 ? gamma_InvBeta_M * M_Duration : -gamma_InvBeta_M * M_Duration); } }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M-1 (m^(1+b/2))
        /// </summary>
        public double Cum_Gamma_Mminus1 { get { return Cum_Gamma_M - gamma_Duration_M; } set { Cum_Gamma_M = value + gamma_Duration_M; } }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
        /// </summary>
        public double Cum_Gamma_M { get; set; }
        /// <summary>
        /// Mean half-macrofracture propagation rate during timestep M (m/s)
        /// </summary>
        public double Mean_MF_PropagationRate_M { get; private set; }
        /// <summary>
        /// Distance that an active half-macrofracture in this gridblock will propagate during timestep M (m)
        /// </summary>
        public double halfLength_M { get { return Mean_MF_PropagationRate_M * M_Duration; } }
        /// <summary>
        /// Total half-length at the start of timestep M of a half-macrofracture that nucleated in this gridblock at time 0 (m)
        /// </summary>
        public double Cum_HalfLength_Mminus1 { get { return Cum_HalfLength_M - halfLength_M; } set { Cum_HalfLength_M = value + halfLength_M; } }
        /// <summary>
        /// Total half-length at the end of timestep M of a half-macrofracture that nucleated in this gridblock at time 0 (m)
        /// </summary>
        public double Cum_HalfLength_M { get; set; }
        /// <summary>
        /// Mean probability of microfracture deactivation by falling into a macrofracture stress shadow in this gridblock, during timestep M (/s)
        /// </summary>
        public double Mean_qiI_M { get { double OneMinusTheta_ratio = 1 - (theta_allFS_M / theta_allFS_Mminus1); if ((OneMinusTheta_ratio > 0) && (M_Duration > 0)) return OneMinusTheta_ratio / M_Duration; else return 0; } }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at start of timestep M
        /// </summary>
        public double theta_Mminus1 { get; private set; }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
        /// NB this is stored as an independent variable rather than calculated from MFP32 and W, since it may not be equal to 1 - (MFP32 * W) if the stress shadow width varies through time
        /// </summary>
        public double theta_M { get; private set; }
        /// <summary>
        /// Mean probability of a microfracture in this gridblock falling into a macrofracture exclusion zone, during timestep M (/s)
        /// </summary>
        public double Mean_qiI_dashed_M { get { double OneMinusTheta_ratio = 1 - (theta_dashed_allFS_M / theta_dashed_allFS_Mminus1); if ((OneMinusTheta_ratio > 0) && (M_Duration > 0)) return OneMinusTheta_ratio / M_Duration; else return 0; } }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at start of timestep M
        /// </summary>
        public double theta_dashed_Mminus1 { get; private set; }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
        /// </summary>
        public double theta_dashed_M { get; private set; }
        /// <summary>
        /// Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at start of timestep M
        /// </summary>
        private double psi_otherFS_Mminus1 { get; set; }
        /// <summary>
        /// Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at end of timestep M
        /// </summary>
        private double psi_otherFS_M { get; set; }
        /// <summary>
        /// Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at start of timestep M
        /// </summary>
        private double chi_otherFS_Mminus1 { get; set; }
        /// <summary>
        /// Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at end of timestep M
        /// </summary>
        private double chi_otherFS_M { get; set; }
        /// <summary>
        /// Inverse stress shadow volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the stress shadow of any fracture set, at start of timestep M
        /// </summary>
        public double theta_allFS_Mminus1 { get { return Math.Max(theta_Mminus1 - psi_otherFS_Mminus1, 0); } }
        /// <summary>
        /// Inverse stress shadow volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the stress shadow of any fracture set, at end of timestep M
        /// </summary>
        public double theta_allFS_M { get { return Math.Max(theta_M - psi_otherFS_M, 0); } }
        /// <summary>
        /// Clear zone volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the exclusion zone of any fracture set, at start of timestep M
        /// </summary>
        public double theta_dashed_allFS_Mminus1 { get { return Math.Max(theta_dashed_Mminus1 - chi_otherFS_Mminus1, 0); } }
        /// <summary>
        /// Clear zone volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the exclusion zone of any fracture set, at end of timestep M
        /// </summary>
        public double theta_dashed_allFS_M { get { return Math.Max(theta_dashed_M - chi_otherFS_M, 0); } }
        /// <summary>
        /// Macrofracture spacing distribution coefficient
        /// </summary>
        public double AA_M { get; private set; }
        /// <summary>
        /// Macrofracture spacing distribution exponent
        /// </summary>
        public double BB_M { get; private set; }
        /// <summary>
        /// Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)
        /// </summary>
        public double CCstep_M { get; private set; }
        /// <summary>
        /// Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32
        /// </summary>
        public double dChi_dMFP32_M { get; private set; }
        /// <summary>
        /// Rate of change of exclusion zone volume relative to stress shadow volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / (1 - theta): will drop from 2 (when Psi = 0) to 1 (as Chi approaches 1)
        /// </summary>
        public double dChi_dPsi_M { get { return (Mean_StressShadowWidth_M > 0 ? dChi_dMFP32_M / Mean_StressShadowWidth_M : 0); } }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by stress shadow interaction averaged over the whole of timestep M, as a proportion of initial fracture population (/s)
        /// </summary>
        public double Mean_FII_M { get { double Phi_ratio = (Phi_II_M > 0 ? Math.Log(Phi_II_M) / Math.Log(Phi_M) : 1); if (Phi_ratio > 0) return Mean_F_M * Phi_ratio; else return 0; } }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by stress shadow interaction during timestep M, as a proportion of current fracture population (/s)
        /// Since macrofracture stress shadow interaction is a random process, this can be calculated directly from Phi_II
        /// </summary>
        public double Instantaneous_FII_M { get { double logPhiII = -Math.Log(Phi_II_M); if (logPhiII > 0) return logPhiII / M_Duration; else return 0; } }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by intersecting another fracture set averaged over the whole of timestep M, as a proportion of initial fracture population (/s)
        /// </summary>
        public double Mean_FIJ_M { get { double Phi_ratio = (Phi_IJ_M > 0 ? Math.Log(Phi_IJ_M) / Math.Log(Phi_M) : 1); if (Phi_ratio > 0) return Mean_F_M * Phi_ratio; else return 0; } }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during timestep M, as a proportion of current fracture population (/s)
        /// Since macrofracture intersection is a semi-regular process when there are stress shadows, this cannot always be calculated from Phi_IJ so is stored separately
        /// </summary>
        public double Instantaneous_FIJ_M { get; private set; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation averaged over the whole of timestep M, as a proportion of initial fracture population (/s)
        /// </summary>
        public double Mean_F_M { get { double OneMinusPhi = 1 - Phi_M; if ((OneMinusPhi > 0) && (M_Duration > 0)) return (1 - Phi_M) / M_Duration; else return 0; } }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation during timestep M, as a proportion of current fracture population (/s)
        /// </summary>
        public double Instantaneous_F_M { get { return Instantaneous_FII_M + Instantaneous_FIJ_M; } }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during timestep M
        /// </summary>
        public double Phi_II_M { get; private set; }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during timestep M
        /// </summary>
        public double Phi_IJ_M { get; private set; }
        /// <summary>
        /// Probability that a half-macrofracture active in this gridblock at the start of timestep M is still active at the end of timestep M
        /// </summary>
        public double Phi_M { get { return Phi_II_M * Phi_IJ_M; } }
        /// <summary>
        /// Cumulative probability that a half-macrofracture nucleated in this gridblock at time 0 is still active at the start of timestep M
        /// </summary>
        public double Cum_Phi_Mminus1 { get { return Cum_Phi_M / Phi_M; } private set { Cum_Phi_M = value * Phi_M; } }
        /// <summary>
        /// Cumulative probability that a half-macrofracture nucleated in this gridblock at time 0 is still active at the end of timestep M
        /// </summary>
        public double Cum_Phi_M { get; private set; }
        /// <summary>
        /// Volumetric density of all active half-macrofractures, at the end of timestep M
        /// </summary>
        public double a_MFP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all static half-macrofractures terminated due to stress shadow interaction, at the end of timestep M
        /// </summary>
        public double sII_MFP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all static half-macrofractures terminated due to intersection, at the end of timestep M
        /// </summary>
        public double sIJ_MFP30_M { get; private set; }
        /// <summary>
        /// Volumetric density of all half-macrofractures, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_MFP30_M { get { return a_MFP30_M + sII_MFP30_M + sIJ_MFP30_M; } }
        /// <summary>
        /// Mean linear density of all microfractures, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_uFP32_M { get; private set; }
        /// <summary>
        /// Mean linear density of all half-macrofractures, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_MFP32_M { get; private set; }
        /// <summary>
        /// Volumetric ratio of all microfractures, static and dynamic, at the end of timestep M
        /// </summary>
        public double Total_uFP33_M { get; private set; }
        /// <summary>
        /// Azimuthal component of mean macrofracture stress shadow width
        /// </summary>
        public double Mean_AzimuthalStressShadowWidth_M { get; private set; }
        /// <summary>
        /// Strike-slip shear component of mean macrofracture stress shadow width
        /// </summary>
        public double Mean_ShearStressShadowWidth_M { get { return Mean_StressShadowWidth_M - Mean_AzimuthalStressShadowWidth_M; } }
        /// <summary>
        /// Mean macrofracture stress shadow width
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
            // Before updating the dynamic data for timestep M we must cache the cumulative data at the start of the timestep Cum_Gamma_Mminus1 and Cum_HalfLength_Mminus1
            double temp_Cum_Gamma = Cum_Gamma_Mminus1;
            double temp_Cum_Halflength = Cum_HalfLength_Mminus1;

            // Set the new timestep duration - if input value <0 keep old data
            if (duration_in >= 0) M_Duration = duration_in;

            // Set the new driving stress - if input value <0 keep old data
            if (meanSigmaD_in >= 0) Mean_SigmaD_M = meanSigmaD_in;

            // Set the mean macrofracture propagation rate and microfracture propagation rate coefficient
            gamma_InvBeta_M = gammaInvBeta_in;
            Mean_MF_PropagationRate_M = meanMFPropRate_in;

            // Update the cumulative data for the start of this timestep; the FractureCalculationData object will then automatically calculate the cumulative data for the end of this timestep
            Cum_Gamma_Mminus1 = temp_Cum_Gamma;
            Cum_HalfLength_Mminus1 = temp_Cum_Halflength;
        }
        /// <summary>
        /// Update the half-macrofracture deactivation probabilities: specify Phi_II and Phi_IJ for this timestep, and all related probabilities will be updated automatically
        /// </summary>
        /// <param name="Phi_II_in">Probability that an active half-macrofracture will not be deactivated due to stress shadow interaction during this timestep</param>
        /// <param name="Phi_IJ_in">Probability that an active half-macrofracture will not be deactivated due to intersecting another fracture during this timestep</param>
        /// <param name="Instantaneous_FIJ_in">Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during timestep M, as a proportion of current fracture population (/s)</param>
        public void SetMacrofractureDeactivationRates(double Phi_II_in, double Phi_IJ_in, double Instantaneous_FIJ_in)
        {
            // Before updating the macrofracture deactivation rate data for timestep M we must cache the cumulative probability at the start of the timestep Cum_Phi_Mminus1
            double temp_Cum_Phi = Cum_Phi_Mminus1;

            // Set the new deactivation rates
            Phi_II_M = Phi_II_in;
            Phi_IJ_M = Phi_IJ_in;
            Instantaneous_FIJ_M = Instantaneous_FIJ_in;

            if ((float)Phi_M <= 0f)
            {
                // If the probability that a half-macrofracture remains active throughout this timestep is less than the specified minimum, then the fractures should be set to residual active
                SetEvolutionStage(FractureEvolutionStage.ResidualActivity);
            }
            else
            {
                // Update the cumulative probability at the start of this timestep; the FractureCalculationData object will then automatically calculate the cumulative probability for the end of this timestep
                // NB We do not need to do this if Phi_M=0 because the cumulative probability at the start of this timestep will be the same as the cumulative probability at the end of this timestep
                Cum_Phi_Mminus1 = temp_Cum_Phi;
            }
        }
        /// <summary>
        /// Set values for the macrofracture density indices aMFP30, sIIMFP30, sIJMFP30 and MFP32 at the end of the timestep
        /// </summary>
        /// <param name="a_MFP30_in">Volumetric density of active half-macrofractures (aMFP30) at end of timestep M</param>
        /// <param name="sII_MFP30_in">Volumetric density of static half-macrofractures terminated due to stress shadow interaction (sIIMFP30) at end of timestep M</param>
        /// <param name="sIJ_MFP30_in">Volumetric density of static half-macrofractures terminated due to intersection (sIJMFP30) at end of timestep M</param>
        /// <param name="Total_MFP32_in">Total mean linear half-macrofracture density (MFP32) at end of timestep M</param>
        public void SetMacrofractureDensityData(double a_MFP30_in, double sII_MFP30_in, double sIJ_MFP30_in, double Total_MFP32_in)
        {
            // Set the new values for active and static MFP30, total uFP32, total MFP32, and total uFP33 at the end of the timestep
            // These are used in calculating fracture deactivation probabilities and mean propagation distances for intervals within the timestep
            a_MFP30_M = a_MFP30_in;
            sII_MFP30_M = sII_MFP30_in;
            sIJ_MFP30_M = sIJ_MFP30_in;
            Total_MFP32_M = Total_MFP32_in;
        }
        /// <summary>
        /// Set values for the microfracture density indices uFP32 and uFP33 at the end of the timestep
        /// </summary>
        /// <param name="Total_uFP32_in">Total mean linear microfracture density (uFP32) at end of timestep M</param>
        /// <param name="Total_uFP33_in">Total volumetric microfracture ratio (uFP33) at end of timestep M</param>
        public void SetMicrofractureDensityData(double Total_uFP32_in, double Total_uFP33_in)
        {
            // Set the new values for active and static MFP30, total uFP32, total MFP32, and total uFP33 at the end of the timestep
            // These are stored for output at the end of the model run 
            Total_uFP32_M = Total_uFP32_in;
            Total_uFP33_M = Total_uFP33_in;
        }
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
            // In this case it will be controlled by the cumulative macrofracture spacing distribution function, so is set in the SetMacrofractureExclusionZoneData function

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
        /// Set values describing the cumulative macrofracture spacing distribution function
        /// </summary>
        /// <param name="AA_in">Macrofracture spacing distribution coefficient AA; set negative to keep the existing value</param>
        /// <param name="BB_in">Macrofracture spacing distribution exponent BB; set negative to keep the existing value</param>
        /// <param name="CCstep_in">Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)</param>
        public void SetMacrofractureSpacingData(double AA_in, double BB_in, double CCstep_in)
        {
            // Set the macrofracture spacing distribution coefficient
            if (AA_in >= 0)
                AA_M = AA_in;
            // Set the macrofracture spacing distribution exponent
            if (BB_in >= 0)
                BB_M = BB_in;
            // Set the step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)
            CCstep_M = CCstep_in;
        }
        /// <summary>
        /// Set the inverse stress shadow and clear zone volume for this fracture set
        /// </summary>
        /// <param name="theta_in">Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M</param>
        /// <param name="theta_dashed_in">Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M</param>
        /// <param name="dChi_dMFP32_M_in">Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32</param>
        public void SetMacrofractureExclusionZoneData(double theta_in, double theta_dashed_in, double dChi_dMFP32_M_in)
        {
            // Set the inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            // NB this is set here rather than in the SetMacrofractureDensityData, because the value of psi will be controlled by the cumulative macrofracture spacing distribution function if the stress shadow width varies through time
            if ((theta_in >= 0) && (theta_in <= 1))
                theta_M = theta_in;
            // Set the clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
            if ((theta_dashed_in >= 0) && (theta_dashed_in <= 1))
                theta_dashed_M = theta_dashed_in;
            // Set the rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32
            if (dChi_dMFP32_M_in >= 0)
                dChi_dMFP32_M = dChi_dMFP32_M_in;
        }
        /// <summary>
        /// Set the inverse stress shadow and clear zone volume for other fracture sets
        /// </summary>
        /// <param name="psi_allFS_in">Total stress shadow volume for all fracture sets (including this one) as seen by this dipset, i.e. cumulative probability that an initial microfracture from this fracture lies in the stress shadow of another fracture set relative to this dipset, at end of timestep M</param>
        /// <param name="chi_allFS_in">Total exclusion zone volume for all fracture sets (including this one) as seen by this dipset, i.e. cumulative probability that an initial microfracture from this fracture set lies in the exclusion zone of another fracture set relative to this dipset, at end of timestep M</param>
        public void SetOtherFSExclusionZoneData(double psi_allFS_in, double chi_allFS_in)
        {
            psi_otherFS_M = psi_allFS_in - (1 - theta_M);
            if (psi_otherFS_M < 0)
                psi_otherFS_M = 0;
            chi_otherFS_M = chi_allFS_in - (1 - theta_dashed_M);
            if (chi_otherFS_M < 0)
                chi_otherFS_M = 0;
        }
        /// <summary>
        /// Reset the fracture evolutionary stage; this may also reset other data items
        /// </summary>
        /// <param name="evolutionStage_in">New evolutionary stage for the fracture set</param>
        public void SetEvolutionStage(FractureEvolutionStage evolutionStage_in)
        {
            // Set the fracture evolution to the specified stage and perform any other data manipulation required
            switch (evolutionStage_in)
            {
                case FractureEvolutionStage.NotActivated:
                    {
                        EvolutionStage = FractureEvolutionStage.NotActivated;
                    }
                    break;
                case FractureEvolutionStage.Growing:
                    {
                        EvolutionStage = FractureEvolutionStage.Growing;
                    }
                    break;
                case FractureEvolutionStage.ResidualActivity:
                    {
                        EvolutionStage = FractureEvolutionStage.ResidualActivity;
                    }
                    break;
                case FractureEvolutionStage.Deactivated:
                    {
                        EvolutionStage = FractureEvolutionStage.Deactivated;
                        // Set the mean half-macrofracture propagation rate and microfracture propagation rate coefficient to zero for this timestep
                        // This will prevent any growth in the populations of implicit microfractures (or macrofractures in future timesteps), and also prevent nucleation and growth of explicit fractures in the DFN
                        // NB we will keep the values for the driving stress, U and V; this is equivalent to reducing the timestep duration to zero
                        Mean_MF_PropagationRate_M = 0;
                        gamma_InvBeta_M = 0;
                        // Set the cumulative half-macrofracture activation function at the end of the timestep Cum_Phi_M to 0 
                        // We will also set the half-macrofracture activation functions within the timestep (Phi_II_M and Phi_IJ_M) to 1 to avoid getting DIV0 errors when calculating Cum_Phi_Mminus1
                        Phi_II_M = 1;
                        Phi_IJ_M = 1;
                    }
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Create a new FractureCalculationData object for the next timestep, and populate it based on the data for this timestep (dynamic values will be set to defaults)
        /// </summary>
        /// <returns>A new FractureCalculationData object populated with data for the next timestep</returns>
        public FractureCalculationData GetNextTimestepData()
        {
            // Create a new FractureCalculationData object as a copy of the current one
            FractureCalculationData nextTimestepData = new FractureCalculationData(this);

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
            // Helper function related to microfracture propagation rate for timestep M: set to 0
            nextTimestepData.gamma_InvBeta_M = 0;
            // Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
            // Do not change; since gamma_InvBeta_M is zero, the cumulative value of gamma_InvBeta_K * K_duration at the start of the timestep (Cum_Gamma_Mminus1) will be automatically set to the same value
            // Mean half-macrofracture propagation rate during timestep M (m/s): set to 0
            nextTimestepData.Mean_MF_PropagationRate_M = 0;
            // Total half-length at the end of timestep M of a half-macrofracture propagating in the specified direction that nucleated in this gridblock at time 0 (m)
            // Do not change; since Mean_MF_PropagationRate_M is zero, the total half-length at the start of the timestep (Cum_HalfLength_Mminus1) will be automatically set to the same value
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active at start of timestep M: set to the same value as that at the end of the timestep
            // Mean_qiI_M will therefore be 0
            nextTimestepData.theta_Mminus1 = theta_M;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone at start of timestep M: set to the same value as that at the end of the timestep
            // Mean_qiI_dashed_M will therefore be 0
            nextTimestepData.theta_dashed_Mminus1 = theta_dashed_M;
            // Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at start of timestep M: set to the same value as that at the end of the timestep
            nextTimestepData.psi_otherFS_Mminus1 = psi_otherFS_M;
            // Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at start of timestep M: set to the same value as that at the end of the timestep
            nextTimestepData.chi_otherFS_Mminus1 = chi_otherFS_M;
            // Macrofracture spacing distribution coefficient AA: does not change
            // Macrofracture spacing distribution exponent BB: does not change
            // Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr): does not change
            // Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32: does not change
            // Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during timestep M: set to 0
            nextTimestepData.Instantaneous_FIJ_M = 0;
            // Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during timestep M: set to 1
            nextTimestepData.Phi_II_M = 1;
            // Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during timestep M: set to 1
            nextTimestepData.Phi_IJ_M = 1;
            // Cumulative probability that a half-macrofracture nucleated in this gridblock at time 0 and propagating in the specified direction is still active at the end of timestep M
            // Do not change; since Phi_M is 1, the cumulative probability that a half-macrofracture is still active at the start of the timestep (Cum_Phi_Mminus) will be automatically set to the same value
            // Volumetric density of all active half-macrofractures: does not change
            // Volumetric density of all static half-macrofractures terminated due to stress shadow interaction: does not change
            // Volumetric density of all static half-macrofractures terminated due to intersection: does not change
            // Mean linear density of all microfractures, static and dynamic: does not change
            // Mean linear density of all half-macrofractures, static and dynamic: does not change
            // Volumetric ratio of all microfractures, static and dynamic: does not change
            // Azimuthal component of mean macrofracture stress shadow width: does not change
            // Mean macrofracture stress shadow width: does not change

            return nextTimestepData;
        }

        // Constructors
        /// <summary>
        /// Default constructor: set everything to initial values (before fractures) 
        /// </summary>
        public FractureCalculationData()
        {
            // Start time (s)
            M_StartTime = 0;
            // Timestep duration (s)
            M_Duration = 0;
            // Flag for the current stage of evolution of the fracture dip set: set to NotActivated
            EvolutionStage = FractureEvolutionStage.NotActivated;
            // Flag for whether subcritical fracture propagation index b is less than, equal to or greater than 2
            M_bType = bType.GreaterThan2;
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
            // Helper function related to microfracture propagation rate for timestep M: (A / |B|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b<>2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
            gamma_InvBeta_M = 0;
            // Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
            Cum_Gamma_M = 0;
            // Mean half-macrofracture propagation rate during timestep M (m/s)
            Mean_MF_PropagationRate_M = 0;
            // Total half-length at the end of timestep M of a half-macrofracture propagating in the specified direction that nucleated in this gridblock at time 0 (m)
            Cum_HalfLength_M = 0;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at start of timestep M
            theta_Mminus1 = 1;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            theta_M = 1;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at start of timestep M
            theta_dashed_Mminus1 = 1;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
            theta_dashed_M = 1;
            // Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at start of timestep M
            psi_otherFS_Mminus1 = 0;
            // Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at end of timestep M
            psi_otherFS_M = 0;
            // Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at start of timestep M
            chi_otherFS_Mminus1 = 0;
            // Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at end of timestep M
            chi_otherFS_M = 0;
            // Macrofracture spacing distribution coefficient
            AA_M = 0;
            // Macrofracture spacing distribution exponent
            BB_M = 0;
            // Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)
            CCstep_M = 0;
            // Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32
            dChi_dMFP32_M = Mean_StressShadowWidth_M * 2;
            // Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during timestep M, as a proportion of current fracture population (/s)
            // Since macrofracture intersection is a semi-regular process when there are stress shadows, this cannot always be calculated from Phi_IJ so is stored separately
            Instantaneous_FIJ_M = 0;
            // Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during timestep M
            Phi_II_M = 1;
            // Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during timestep M
            Phi_IJ_M = 1;
            // Cumulative probability that a half-macrofracture nucleated in this gridblock at time 0 and propagating in the specified direction is still active at the end of timestep M
            Cum_Phi_M = 1;
            // Volumetric density of all active half-macrofractures
            a_MFP30_M = 0;
            // Volumetric density of all static half-macrofractures terminated due to stress shadow interaction, at the end of timestep M
            sII_MFP30_M = 0;
            // Volumetric density of all static half-macrofractures terminated due to intersection, at the end of timestep M
            sIJ_MFP30_M = 0;
            // Mean linear density of all microfractures, static and dynamic, at the end of timestep M
            Total_uFP32_M = 0;
            // Mean linear density of all half-macrofractures, static and dynamic
            Total_MFP32_M = 0;
            // Volumetric ratio of all microfractures, static and dynamic, at the end of timestep M
            Total_uFP33_M = 0;
            // Azimuthal component of mean macrofracture stress shadow width
            Mean_AzimuthalStressShadowWidth_M = 0;
            // Mean macrofracture stress shadow width
            Mean_StressShadowWidth_M = 0;
        }
        /// <summary>
        /// Constructor: specify start time and timestep duration, set everything else to zero
        /// </summary>
        /// <param name="StartTime_in">Real time at start of timestep (s)</param>
        /// <param name="Duration_in">Timestep duration (s)</param>
        /// <param name="initial_h_component">Component related to layer point_t to include in Cum_hGamma: ln(h/2) for b=2, (h/2)^(1/beta) for b!=2</param>
        public FractureCalculationData(double StartTime_in, double Duration_in) : this()
        {
            // Start time (s)
            M_StartTime = StartTime_in;
            // Timestep duration
            M_Duration = Duration_in;
        }
        /// <summary>
        /// Copy constructor: copy all data from the input FractureCalculationData object into the new FractureCalculationData object
        /// </summary>
        /// <param name="fcd_in">Reference to FractureCalculationData object to copy</param>
        public FractureCalculationData(FractureCalculationData fcd_in)
        {
            // Start time (s)
            M_StartTime = fcd_in.M_StartTime;
            // Timestep duration
            M_Duration = fcd_in.M_Duration;
            // Flag for the current stage of evolution of the fracture dip set
            EvolutionStage = fcd_in.EvolutionStage;
            // Flag for whether subcritical fracture propagation index b is less than, equal to or greater than 2
            M_bType = fcd_in.M_bType;
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
            // Helper function related to microfracture propagation rate for timestep M: (A / |B|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b<>2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
            gamma_InvBeta_M = fcd_in.gamma_InvBeta_M;
            // Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
            Cum_Gamma_M = fcd_in.Cum_Gamma_M;
            // Mean half-macrofracture propagation rate during timestep M (m/s)
            Mean_MF_PropagationRate_M = fcd_in.Mean_MF_PropagationRate_M;
            // Total half-length at the end of timestep M of a half-macrofracture propagating in the specified direction that nucleated in this gridblock at time 0 (m)
            Cum_HalfLength_M = fcd_in.Cum_HalfLength_M;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at start of timestep M
            theta_Mminus1 = fcd_in.theta_Mminus1;
            // Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            theta_M = fcd_in.theta_M;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at start of timestep M
            theta_dashed_Mminus1 = fcd_in.theta_dashed_Mminus1;
            // Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M
            theta_dashed_M = fcd_in.theta_dashed_M;
            // Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at start of timestep M
            psi_otherFS_Mminus1 = fcd_in.psi_otherFS_Mminus1;
            // Total volume of stress shadow for all other fracture sets, excluding overlap with the stress shadow of this dipset, at end of timestep M
            psi_otherFS_M = fcd_in.psi_otherFS_M;
            // Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at start of timestep M
            chi_otherFS_Mminus1 = fcd_in.chi_otherFS_Mminus1;
            // Total exclusion zone volume for all other fracture sets, excluding overlap with the exclusion zone of this dipset, at end of timestep M
            chi_otherFS_M = fcd_in.chi_otherFS_M;
            // Macrofracture spacing distribution coefficient
            AA_M = fcd_in.AA_M;
            // Macrofracture spacing distribution exponent
            BB_M = fcd_in.BB_M;
            // Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)
            CCstep_M = fcd_in.CCstep_M;
            // Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32
            dChi_dMFP32_M = fcd_in.dChi_dMFP32_M;
            // Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during timestep M, as a proportion of current fracture population (/s)
            // Since macrofracture intersection is a semi-regular process when there are stress shadows, this cannot always be calculated from Phi_IJ so is stored separately
            Instantaneous_FIJ_M = fcd_in.Instantaneous_FIJ_M;
            // Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during timestep M
            Phi_II_M = fcd_in.Phi_II_M;
            // Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during timestep M
            Phi_IJ_M = fcd_in.Phi_IJ_M;
            // Cumulative probability that a half-macrofracture nucleated in this gridblock at time 0 and propagating in the specified direction is still active at the end of timestep M
            Cum_Phi_M = fcd_in.Cum_Phi_M;
            // Volumetric density of all active half-macrofractures
            a_MFP30_M = fcd_in.a_MFP30_M;
            // Volumetric density of all static half-macrofractures terminated due to stress shadow interaction, at the end of timestep M
            sII_MFP30_M = fcd_in.sII_MFP30_M;
            // Volumetric density of all static half-macrofractures terminated due to intersection, at the end of timestep M
            sIJ_MFP30_M = fcd_in.sIJ_MFP30_M;
            // Mean linear density of all microfractures, static and dynamic, at the end of timestep M
            Total_uFP32_M = fcd_in.Total_uFP32_M;
            // Mean linear density of all half-macrofractures, static and dynamic, at the end of the timestep
            Total_MFP32_M = fcd_in.Total_MFP32_M;
            // Volumetric ratio of all microfractures, static and dynamic, at the end of timestep M
            Total_uFP33_M = fcd_in.Total_uFP33_M;
            // Azimuthal component of mean macrofracture stress shadow width
            Mean_AzimuthalStressShadowWidth_M = fcd_in.Mean_AzimuthalStressShadowWidth_M;
            // Mean macrofracture stress shadow width
            Mean_StressShadowWidth_M = fcd_in.Mean_StressShadowWidth_M;
        }
    }
}