// Set this flag to output detailed information on the calculation of the cumulative macrofracture density distribution
// Use for debugging only; will significantly increase runtime
//#define DBLOG
// Set this flag to activate a set of functions useful for debugging
// By default these are not compiled
//#define DBFUNCTIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for fracture mode: Mode1 = dilatant, Mode2 = dip-slip shear, Mode3 = strike-slip shear
    /// </summary>
    public enum FractureMode { Mode1, Mode2, Mode3 }
    /// <summary>
    /// Enumerator for predominant sense of fracture displacement: dilatant, normal, right- or left-lateral strike-slip, or reverse
    /// </summary>
    public enum FractureDisplacementSense { Dilatant, Normal, RightLateral, Reverse, LeftLateral }
    /// <summary>
    /// Enumerator for initial microfracture distribution - at present only Power Law is implemented
    /// </summary>
    public enum InitialFractureDistribution { PowerLaw, Exponential }
    /// <summary>
    /// Enumerator for method used to determine fracture aperture - used in porosity and permeability calculation
    /// </summary>
    public enum FractureApertureType { Uniform, SizeDependent, Dynamic, BartonBandis }

    /// <summary>
    /// Representation of a single fracture dipset in a single gridblock
    /// </summary>
    class FractureDipSet : IComparable<FractureDipSet>
    {
        // References to external objects
        /// <summary>
        /// Reference to grandparent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;
        /// <summary>
        /// Reference to parent FractureSet object
        /// </summary>
        private Gridblock_FractureSet fs;

        // Control and implementation functions
        /// <summary>
        /// Compare FractureDipSet objects based on stress shadow width
        /// NB Comparison result is inverted so that the fracture dipsets will be sorted in reverse order of stress shadow width (largest to smallest)
        /// </summary>
        /// <param name="that">FractureDipSet object to compare with</param>
        /// <returns>Negative if this FractureDipSet has the greatest stress shadow width, positive if that FractureDipSet has the greatest stress shadow width, zero if they have the same stress shadow width</returns>
        public int CompareTo(FractureDipSet that)
        {
            return -this.Mean_MF_StressShadowWidth.CompareTo(that.Mean_MF_StressShadowWidth);
        }

        // Fracture mode and dip
        /// <summary>
        /// Fracture mode
        /// </summary>
        public FractureMode Mode { get; private set; }
        /// <summary>
        /// Flag for a biazimuthal conjugate dipset: if true, the dipset contains equal numbers of fractures dipping in opposite directions; if false, the dipset contains only fractures dipping in the specified azimuth direction
        /// </summary>
        public bool BiazimuthalConjugate { get; private set; }
        /// <summary>
        /// Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow
        /// </summary>
        public bool IncludeReverseFractures { get; set; }
        /// <summary>
        /// Variable for fracture dip
        /// </summary>
        private double dip;
        /// <summary>
        /// Unit vector on the fracture plane in direction of maximum dip (in the case of bimodal conjugate dipsets, dipping in J+ direction)
        /// </summary>
        private VectorXYZ dipVector;
        /// <summary>
        /// Variable for normal vector to the fracture
        /// </summary>
        private VectorXYZ normalVector;
        /// <summary>
        /// Fracture dip
        /// </summary>
        public double Dip
        {
            get
            {
                return dip;
            }
            // The set method will set both the fracture dip and the normal and azimuth vectors
            private set
            {
                // Set the fracture dip
                dip = value;

                // Create a new normal vector
                normalVector = VectorXYZ.GetNormalToPlane(fs.Strike + (Math.PI / 2), value);

                // Create a new azimuth vector
                dipVector = VectorXYZ.GetLineVector(fs.Strike + (Math.PI / 2), value);
            }
        }
        /// <summary>
        /// Normal vector to the fracture (in the case of bimodal conjugate dipsets, normal to fracture dipping in J+ direction)
        /// </summary>
        public VectorXYZ NormalVector { get { return new VectorXYZ(normalVector); } }
        /// <summary>
        /// Unit vector on the fracture plane in direction of maximum dip (in the case of bimodal conjugate dipsets, dipping in J+ direction)
        /// </summary>
        public VectorXYZ DipVector { get { return new VectorXYZ(dipVector); } }

        // Fracture distribution control data
        /// <summary>
        /// Initial microfracture distribution - at present only Power Law is implemented
        /// </summary>
        public InitialFractureDistribution InitialDistribution { get; set; }
        /// <summary>
        /// Initial microfracture density coefficient B (/m3)
        /// </summary>
        public double CapB { get; set; }
        /// <summary>
        /// Initial microfracture distribution coefficient c
        /// </summary>
        public double c_coefficient { get; set; }
        /// <summary>
        /// Index radii for microfracture cumulative population distribution function arrays
        /// </summary>
        public List<double> uF_radii;
        /// <summary>
        /// Index half-lengths for cumulative population distribution function arrays
        /// </summary>
        public List<double> MF_halflengths;

        // Current fracture data
        /// <summary>
        /// Object containing cumulative population data for microfractures
        /// </summary>
        private MicrofractureData MicroFractures;
        /// <summary>
        /// Object containing cumulative population data for macrofractures propagating in the IPlus direction
        /// </summary>
        private MacrofractureData IPlus_halfMacroFractures;
        /// <summary>
        /// Object containing cumulative population data for macrofractures propagating in the IMinus direction
        /// </summary>
        private MacrofractureData IMinus_halfMacroFractures;
        /// <summary>
        /// Object containing dynamic propagation data for the current timestep; updated as each timestep is calculated
        /// </summary>
        private FractureCalculationData CurrentFractureData;

        // Fracture data for previous timesteps
        /// <summary>
        /// Object containing a list of FractureCalculationData objects with dynamic propagation data for the previous timesteps
        /// </summary>
        private FCD_List PreviousFractureData;

        // Functions to return data for the current timestep from the CurrentFractureData object
        /// <summary>
        /// Flag for the current stage of evolution of the fracture dip set
        /// </summary>
        /// <returns></returns>
        public FractureEvolutionStage getEvolutionStage() { return CurrentFractureData.EvolutionStage; }
        /// <summary>
        /// Effective normal stress on the fracture at the end of the current timestep (Pa)
        /// </summary>
        /// <returns></returns>
        public double getFinalEffectiveNormalStress() { return CurrentFractureData.SigmaNeff_Final_M; }
        /// <summary>
        /// Driving stress at the start of the current timestep U (Pa) 
        /// </summary>
        /// <returns></returns>
        public double getConstantDrivingStressU() { return CurrentFractureData.U_M; }
        /// <summary>
        /// Rate of increase of driving stress during the current timestep V (Pa/s)
        /// </summary>
        /// <returns></returns>
        public double getVariableDrivingStressV() { return CurrentFractureData.V_M; }
        /// <summary>
        /// Weighted mean driving stress during the current timestep (Pa)
        /// </summary>
        /// <returns></returns>
        public double getMeanDrivingStressSigmaD() { return CurrentFractureData.Mean_SigmaD_M; }
        /// <summary>
        /// Driving stress at the end of the current timestep (Pa)
        /// </summary>
        /// <returns></returns>
        public double getFinalDrivingStressSigmaD() { return CurrentFractureData.Final_SigmaD_M; }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps up to and including the current timestep (m^(1+b/2))
        /// </summary>
        /// <returns></returns>
        public double getCumGamma() { return CurrentFractureData.Cum_Gamma_M; }
        /// <summary>
        /// Mean half-macrofracture propagation rate during the current timestep (m/s)
        /// </summary>
        /// <returns></returns>
        public double getMeanMFPropagationRate() { return CurrentFractureData.Mean_MF_PropagationRate_M; }
        /// <summary>
        /// Distance that an active half-macrofracture will propagate during the current timestep (m)
        /// </summary>
        /// <returns></returns>
        public double getMFPropagationDistance() { return CurrentFractureData.halfLength_M; }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getInverseStressShadowVolume() { return CurrentFractureData.theta_M; }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getClearZoneVolume() { return CurrentFractureData.theta_dashed_M; }
        /// <summary>
        /// Inverse stress shadow volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the stress shadow of any fracture set, at end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getInverseStressShadowVolumeAllFS() { return CurrentFractureData.theta_allFS_M; }
        /// <summary>
        /// Clear zone volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the exclusion zone of any fracture set, at end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getClearZoneVolumeAllFS() { return CurrentFractureData.theta_dashed_allFS_M; }
        /// <summary>
        /// Coefficient for this segment of the fracture spacing distribution curve, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getAA() { return CurrentFractureData.AA_M; }
        /// <summary>
        /// Exponent for this segment of the fracture spacing distribution curve, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getBB() { return CurrentFractureData.BB_M; }
        /// <summary>
        /// Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr), during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getCCStep() { return CurrentFractureData.CCstep_M; }
        /// <summary>
        /// Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32, during the current timestep
        /// </summary>
        public double getdChi_dMFP32_M() { return CurrentFractureData.dChi_dMFP32_M; }
        /// <summary>
        /// Rate of change of exclusion zone volume relative to stress shadow volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / (1 - theta), during the current timestep
        /// </summary>
        public double getdChi_dPsi_M() { return CurrentFractureData.dChi_dPsi_M; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by stress shadow interaction during the current timestep, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <returns></returns>
        public double getMeanFII() { return CurrentFractureData.Mean_FII_M; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by intersecting another fracture set during the current timestep, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <returns></returns>
        public double getMeanFIJ() { return CurrentFractureData.Mean_FIJ_M; }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation during the current timestep, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <returns></returns>
        public double getMeanF() { return CurrentFractureData.Mean_F_M; }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by stress shadow interaction during the current timestep, as a proportion of current fracture population (/s)
        /// </summary>
        /// <returns></returns>
        public double getInstantaneousFII() { return CurrentFractureData.Instantaneous_FII_M; }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during the current timestep, as a proportion of current fracture population (/s)
        /// </summary>
        /// <returns></returns>
        public double getInstantaneousFIJ() { return CurrentFractureData.Instantaneous_FIJ_M; }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation during the current timestep, as a proportion of current fracture population (/s)
        /// </summary>
        /// <returns></returns>
        public double getInstantaneousF() { return CurrentFractureData.Instantaneous_F_M; }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getPhiII() { return CurrentFractureData.Phi_II_M; }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getPhiIJ() { return CurrentFractureData.Phi_IJ_M; }
        /// <summary>
        /// Probability that a half-macrofracture active in this gridblock at the start of the current timestep is still active at the end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getPhi() { return CurrentFractureData.Phi_M; }
        /// <summary>
        /// Return the maximum number of half-macrofractures that will have nucleated in this gridblock, if there is no fracture deactivation
        /// </summary>
        /// <returns></returns>
        public double getMaximumNucleatedHalfMacrofractures()
        {
            // Cache constants locally
            double beta = gbc.MechProps.beta;
            bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);

            // Calculate local helper variables
            double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
            double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(PreviousFractureData.NoTimesteps); ;
            double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));

            // Return maximum number of half macrofractures
            return 2 * CapB * ts_CumhGammaM_betac_factor;
        }
        /// <summary>
        /// Return the volumetric density of all active half-macrofractures, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getActiveMFP30() { return CurrentFractureData.a_MFP30_M; }
        /// <summary>
        /// Return the volumetric density of all static half-macrofractures terminated due to stress shadow interaction, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getStaticRelayMFP30() { return CurrentFractureData.sII_MFP30_M; }
        /// <summary>
        /// Return the volumetric density of all static half-macrofractures terminated due to intersection, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getStaticIntersectMFP30() { return CurrentFractureData.sIJ_MFP30_M; }
        /// <summary>
        /// Return the volumetric density of all half-macrofractures, static and dynamic, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotalMFP30() { return CurrentFractureData.Total_MFP30_M; }
        /// <summary>
        /// Return the mean linear density of all microfractures, static and dynamic, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotaluFP32() { return CurrentFractureData.Total_uFP32_M; }
        /// <summary>
        /// Return the total linear density of all half-macrofractures, static and dynamic, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotalMFP32() { return CurrentFractureData.Total_MFP32_M; }
        /// <summary>
        /// Return the volumetric ratio of all microfractures, static and dynamic, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotaluFP33() { return CurrentFractureData.Total_uFP33_M; }
        /// <summary>
        /// Return the volumetric ratio of all half-macrofractures, static and dynamic, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotalMFP33() { return (Math.PI / 4) * gbc.ThicknessAtDeformation * CurrentFractureData.Total_MFP32_M; }
        /// <summary>
        /// Return the azimuthal component of the mean macrofracture stress shadow width during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getMeanAzimuthalStressShadowWidth() { return CurrentFractureData.Mean_AzimuthalStressShadowWidth_M; }
        /// <summary>
        /// Return the strike-slip shear component of the mean macrofracture stress shadow width during the current timestep
        /// </summary>
        /// <returns></returns>
        /// <summary>
        public double getMeanShearStressShadowWidth() { return CurrentFractureData.Mean_ShearStressShadowWidth_M; }
        /// Return the mean macrofracture stress shadow width during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getMeanStressShadowWidth() { return CurrentFractureData.Mean_StressShadowWidth_M; }

        // Functions to return data for previous timesteps from the PreviousFractureData list
        /// <summary>
        /// Flag for the stage of evolution of the fracture dip set at a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public FractureEvolutionStage getEvolutionStage(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getEvolutionStage(Timestep_M); }
        /// <summary>
        /// Effective normal stress on the fracture at the end of a specified previous timestep (Pa)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getFinalEffectiveNormalStress(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFinalNormalStress(Timestep_M); }
        /// <summary>
        /// Driving stress at the start of a specified previous timestep U (Pa) 
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getConstantDrivingStressU(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getConstantDrivingStressU(Timestep_M); }
        /// <summary>
        /// Rate of increase of driving stress during a specified previous timestep V (Pa/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getVariableDrivingStressV(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getVariableDrivingStressV(Timestep_M); }
        /// <summary>
        /// Weighted mean driving stress during a specified previous timestep (Pa)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanDrivingStressSigmaD(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMeanDrivingStressSigmaD(Timestep_M); }
        /// <summary>
        /// Driving stress at the end of a specified previous timestep (Pa)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getFinalDrivingStressSigmaD(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFinalDrivingStressSigmaD(Timestep_M); }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including M (m^(1+b/2))
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getCumGamma(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCum_Gamma_M(Timestep_M); }
        /// <summary>
        /// For b!=2, (h/2)^(1/beta) + Cum_Gamma_Mminus1; for b=2, ln(h/2) + Cum_Gamma_Mminus1 (m^(1+b/2))
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getCumhGamma(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCum_hGamma_M(Timestep_M); }
        /// <summary>
        /// Mean half-macrofracture propagation rate during a specified previous timestep (m/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanMFPropagationRate(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMeanMFPropagationRate(Timestep_M); }
        /// <summary>
        /// Distance that an active half-macrofracture will propagate during a specified previous timestep (m)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMFPropagationDistance(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getHalfLength(Timestep_M); }
        /// <summary>
        /// Distance that an active half-macrofracture will propagate between the end of timestep M and the end of timestep N (m)
        /// </summary>
        /// <param name="Timestep_N">Index number of the end timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <param name="Timestep_M">Index number of the start timestep</param>
        /// <returns></returns>
        public double getCumulativeMFPropagationDistance(int Timestep_N, int Timestep_M) { if (Timestep_N < 0) Timestep_N = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeHalfLength(Timestep_N, Timestep_M); }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getInverseStressShadowVolume(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeTheta(Timestep_M); }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getClearZoneVolume(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeThetaDashed(Timestep_M); }
        /// <summary>
        /// Inverse stress shadow volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the stress shadow of any fracture set, at end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getInverseStressShadowVolumeAllFS(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeTheta_AllFS_M(Timestep_M); }
        /// <summary>
        /// Clear zone volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the exclusion zone of any fracture set, at end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getClearZoneVolumeAllFS(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeThetaDashed_AllFS_M(Timestep_M); }
        /// <summary>
        /// Coefficient for this segment of the fracture spacing distribution curve, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getAA(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getAA_M(Timestep_M); }
        /// <summary>
        /// Exponent for this segment of the fracture spacing distribution curve, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getBB(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getBB_M(Timestep_M); }
        /// <summary>
        /// Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr), at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getCCStep(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCCStep_M(Timestep_M); }
        /// <summary>
        /// Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getdChi_dMFP32_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getdChi_dMFP32_M(Timestep_M); }
        /// <summary>
        /// Rate of change of exclusion zone volume relative to stress shadow volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / (1 - theta), at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getdChi_dPsi_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getdChi_dPsi_M(Timestep_M); }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by stress shadow interaction during a specified previous timestep, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanFII(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMeanFII(Timestep_M); }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation by intersecting another fracture set during a specified previous timestep, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanFIJ(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMeanFIJ(Timestep_M); }
        /// <summary>
        /// Mean probability of half-macrofracture deactivation during a specified previous timestep, as a proportion of initial fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanF(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMeanF(Timestep_M); }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by stress shadow interaction during a specified previous timestep, as a proportion of current fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getInstantaneousFII(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getInstantaneousFII(Timestep_M); }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation by intersecting another fracture set during a specified previous timestep, as a proportion of current fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getInstantaneousFIJ(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getInstantaneousFIJ(Timestep_M); }
        /// <summary>
        /// Instantaneous probability of half-macrofracture deactivation during a specified previous timestep, as a proportion of current fracture population (/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getInstantaneousF(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getInstantaneousF(Timestep_M); }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to stress shadow interaction during a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getPhiII(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getPhiII(Timestep_M); }
        /// <summary>
        /// Probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getPhiIJ(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getPhiIJ(Timestep_M); }
        /// <summary>
        /// Probability that a half-macrofracture active in this gridblock at the start of a specified timestep is still active at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getPhi(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getPhi(Timestep_M); }
        /// <summary>
        /// Probability that a half-macrofracture active in this gridblock at the end of timestep M is still active at the end of timestep N
        /// </summary>
        /// <param name="Timestep_N">Index number of the end timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <param name="Timestep_M">Index number of the start timestep</param>
        /// <returns></returns>
        public double getCumulativePhi(int Timestep_N, int Timestep_M) { if (Timestep_N < 0) Timestep_N = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativePhi(Timestep_N, Timestep_M); }
        /// <summary>
        /// Return the maximum number of half-macrofractures that will have nucleated in this gridblock at the end of a specified previous timestep, if there is no fracture deactivation
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMaximumNucleatedHalfMacrofractures(int Timestep_M)
        {
            // If the specified timestep is negative, set it to the current explicit timestep
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;

            // Cache constants locally
            double beta = gbc.MechProps.beta;
            bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);

            // Calculate local helper variables
            double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
            double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(Timestep_M); ;
            double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));

            // Return maximum number of half macrofractures
            return 2 * CapB * ts_CumhGammaM_betac_factor;
        }
        /// <summary>
        /// Return the volumetric density of all active half-macrofractures, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getActiveMFP30(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.geta_MFP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all static half-macrofractures terminated due to stress shadow interaction, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getStaticRelayMFP30(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getsII_MFP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all static half-macrofractures terminated due to intersection, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getStaticIntersectMFP30(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getsIJ_MFP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all half-macrofractures, static and dynamic, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalMFP30(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_MFP30_M(Timestep_M); }
        /// <summary>
        /// Return the mean linear density of all microfractures, static and dynamic, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotaluFP32(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_uFP32_M(Timestep_M); }
        /// <summary>
        /// Return the total linear density of all half-macrofractures, static and dynamic, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalMFP32(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_MFP32_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric ratio of all microfractures, static and dynamic, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotaluFP33(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_uFP33_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric ratio of all half-macrofractures, static and dynamic, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalMFP33(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return (Math.PI / 4) * gbc.ThicknessAtDeformation * PreviousFractureData.getTotal_MFP32_M(Timestep_M); }
        /// <summary>
        /// Return the azimuthal component of the mean macrofracture stress shadow width at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanAzimuthalStressShadowWidth(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMean_AzimuthalStressShadowWidth_M(Timestep_M); }
        /// <summary>
        /// Return the strike-slip shear component of the mean macrofracture stress shadow width at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanShearStressShadowWidth(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMean_ShearStressShadowWidth_M(Timestep_M); }
        /// <summary>
        /// Return the mean macrofracture stress shadow width at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanStressShadowWidth(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMean_StressShadowWidth_M(Timestep_M); }
        /// <summary>
        /// Return the total macrofracture stress shadow volume at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotaluFPorosity(FractureApertureType ApertureControl, bool UseCurrentStress, int Timestep_M)
        {
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;
            double output = 0;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = PreviousFractureData.getTotal_uFP32_M(Timestep_M) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = PreviousFractureData.getTotal_uFP33_M(Timestep_M) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(UseCurrentStress ? CurrentFractureData.SigmaNeff_Final_M : PreviousFractureData.getFinalNormalStress(Timestep_M));
                    tensile_sigmaNeff = Math.Max(tensile_sigmaNeff, 0);
                    output = PreviousFractureData.getTotal_uFP33_M(Timestep_M) * gbc.MechProps.DynamicApertureMultiplier * (4 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = (UseCurrentStress ? CurrentFractureData.SigmaNeff_Final_M : PreviousFractureData.getFinalNormalStress(Timestep_M));
                    compressive_sigmaNeff = Math.Max(compressive_sigmaNeff, 0);
                    output = PreviousFractureData.getTotal_uFP32_M(Timestep_M) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Return the total porosity of all half-macrofractures that were present at the end of a specified previous timestep
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <param name="UseCurrentStress">If true, use the stress in the current explicit timestep to calculate the fracture aperture; if false, use the stress in the specified timestep to calculate the fracture aperture</param>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalMFPorosity(FractureApertureType ApertureControl, bool UseCurrentStress, int Timestep_M)
        {
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;
            double output = 0;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = PreviousFractureData.getTotal_MFP32_M(Timestep_M) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * PreviousFractureData.getTotal_MFP32_M(Timestep_M) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(UseCurrentStress ? CurrentFractureData.SigmaNeff_Final_M : PreviousFractureData.getFinalNormalStress(Timestep_M));
                    tensile_sigmaNeff = Math.Max(tensile_sigmaNeff, 0);
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * PreviousFractureData.getTotal_MFP32_M(Timestep_M) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = (UseCurrentStress ? CurrentFractureData.SigmaNeff_Final_M : PreviousFractureData.getFinalNormalStress(Timestep_M));
                    compressive_sigmaNeff = Math.Max(compressive_sigmaNeff, 0);
                    output = PreviousFractureData.getTotal_MFP32_M(Timestep_M) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Get the time at which the fracture set becomes deactivated
        /// </summary>
        /// <returns>Deactivation time of fracture set; will return zero if the fracture set was never active</returns>
        public double getFinalActiveTime()
        {
            // Get time units and unit conversion modifier for output time data if not in SI units
            double timeUnits_Modifier = gbc.PropControl.getTimeUnitsModifier();

            // Loop through the timesteps in reverse order
            for (int TimestepNo = PreviousFractureData.NoTimesteps; TimestepNo > 0; TimestepNo--)
            {
                // Check if the dipset is active (or residual active); if so return the end time of the current timestep
                FractureEvolutionStage CurrentStage = getEvolutionStage(TimestepNo);
                if ((CurrentStage == FractureEvolutionStage.Growing) || (CurrentStage == FractureEvolutionStage.ResidualActivity))
                    return PreviousFractureData.getEndTime(TimestepNo) / timeUnits_Modifier;
            }

            // If the fracture set was never active, return 0
            return 0;
        }

        // Fracture aperture control data - for uniform and size-dependent aperture, which are dependent on dip set
        // NB fracture aperture control data for dynamic and Barton-Bandis aperture are independent of dip set, so are contained in the MechanicalProperties object for the gridblock
        /// <summary>
        /// Fixed aperture for fractures in the uniform aperture case (m)
        /// </summary>
        public double UniformAperture { get; set; }
        /// <summary>
        /// Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier
        /// </summary>
        public double SizeDependentApertureMultiplier { get; set; }
        /// <summary>
        /// Current maximum aperture of a microfracture of a specified radius
        /// </summary>
        /// <param name="radius">Microfracture radius (m)</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumMicrofractureAperture(double radius)
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = 2 * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (8 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current mean aperture of a microfracture of a specified radius
        /// </summary>
        /// <param name="radius">Microfracture radius (m)</param>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanMicrofractureAperture(double radius)
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (4 / 3) * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (16 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current maximum half-macrofracture aperture
        /// </summary>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumMacrofractureAperture()
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = gbc.ThicknessAtDeformation * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = gbc.MechProps.DynamicApertureMultiplier * (2 * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current mean half-macrofracture aperture
        /// </summary>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanMacrofractureAperture()
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = gbc.MechProps.DynamicApertureMultiplier * (Math.PI * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current maximum aperture of a microfracture of a specified radius
        /// </summary>
        /// <param name="radius">Microfracture radius (m)</param>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumMicrofractureAperture(double radius, int timestep)
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    // Mean aperture 
                    output = (4 / Math.PI) * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (8 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Mean aperture of a microfracture of a specified radius at the end of a previous timestep
        /// </summary>
        /// <param name="radius">Microfracture radius (m)</param>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanMicrofractureAperture(double radius, int timestep)
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    // Mean aperture 
                    output = radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (16 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current maximum half-macrofracture aperture
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumMacrofractureAperture(int timestep)
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    // Mean aperture 
                    output = gbc.ThicknessAtDeformation * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = gbc.MechProps.DynamicApertureMultiplier * (2 * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current mean half-macrofracture aperture
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanMacrofractureAperture(int timestep)
        {
            double output = 0;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    // Mean aperture 
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = gbc.MechProps.DynamicApertureMultiplier * (Math.PI * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(PreviousFractureData.getFinalNormalStress(timestep), 0);
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Fracture aperture based on Barton-Bandis model at current stress state
        /// </summary>
        /// <param name="SigmaNeff">Effective normal stress on the fracture (Pa)</param>
        /// <returns></returns>
        public double BartonBandisAperture(double SigmaNeff)
        {
            // Cache variables locally and convert into units required by Barton-Bandis formulae
            // Convert effective normal stress on the fracture to MPa
            SigmaNeff = SigmaNeff / 1E+6;
            // Joint Roughness Coefficient
            double JRC = gbc.MechProps.JRC;
            // Ratio of unconfined compressive strength of unfractured rock to fractured rock
            double UCS_ratio = gbc.MechProps.UCS_ratio;
            // Initial normal stress on fracture (MPa)
            double InitialNormalStress = gbc.MechProps.InitialNormalStress / 1E+6;
            // Stiffness normal to the fracture, at initial normal stress (MPa/mm)
            double FractureNormalStiffness = gbc.MechProps.FractureNormalStiffness / 1E+9;
            // Maximum fracture closure (mm)
            double MaximumClosure = gbc.MechProps.MaximumClosure * 1E+3;

            // First calculate initial fracture aperture (i.e. the aperture at InitialNormalStress) in mm
            double a0 = (JRC / 5) * ((0.2 * UCS_ratio) - 0.1);
            // Then calculate stress-dependent fracture closure in mm
            // This is dependent on the fracture mode
            double delta_a = 0;
            if (Mode == FractureMode.Mode1)
            {
                // If effective normal stress on the fracture is tensile, set it to zero
                SigmaNeff = Math.Max(SigmaNeff, 0);
                delta_a = SigmaNeff / (FractureNormalStiffness + (SigmaNeff / MaximumClosure));
            }
            else
            {
                // If effective normal stress on the fracture is less than the initial normal stress, set it to the initial normal stress
                SigmaNeff = Math.Max(SigmaNeff, InitialNormalStress);
                delta_a = (Math.Log10(SigmaNeff) - Math.Log10(InitialNormalStress)) * Math.Log(10) * (InitialNormalStress / FractureNormalStiffness);
            }
            // Subtract the fracture closure from the initial aperture, and convert to metres
            double aperture = (a0 - delta_a) / 1E+3;
            // If the calculated aperture is negative, set it to zero
            if (aperture < 0) aperture = 0;

            return aperture;
        }

        // Macrofracture growth rate data
        /// <summary>
        /// Rate of change of total volumetric density of active half-macrofractures 
        /// </summary>
        public double da_MFP30 { get; private set; }
        /// <summary>
        /// Rate of change of total volumetric density of static half-macrofractures terminated due to stress shadow interaction
        /// </summary>
        public double dsII_MFP30 { get; private set; }
        /// <summary>
        /// Rate of change of total volumetric density of static half-macrofractures terminated due to intersection with other fracture sets
        /// </summary>
        public double dsIJ_MFP30 { get; private set; }
        /// <summary>
        /// Rate of change of total linear density of active half-macrofractures
        /// </summary>
        public double da_MFP32 { get; private set; }
        /// <summary>
        /// Rate of change of total linear density of static half-macrofractures
        /// </summary>
        public double ds_MFP32 { get; private set; }
        /// <summary>
        /// Rate of change of total volumetric ratio of active half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double da_MFP33() { return (Math.PI / 4) * gbc.ThicknessAtDeformation * da_MFP32; }
        /// <summary>
        /// Rate of change of total volumetric ratio of static half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double ds_MFP33() { return (Math.PI / 4) * gbc.ThicknessAtDeformation * ds_MFP32; }

        // Functions to get total population data for all fractures in the dipset
        /// <summary>
        /// Total volumetric density of active microfractures
        /// </summary>
        /// <returns></returns>
        public double a_uFP30_total() { return MicroFractures.a_P30_total; }
        /// <summary>
        /// Total volumetric density of static microfractures
        /// </summary>
        /// <returns></returns>
        public double s_uFP30_total() { return MicroFractures.s_P30_total; }
        /// <summary>
        /// Total linear density of active microfractures
        /// </summary>
        /// <returns></returns>
        public double a_uFP32_total() { return MicroFractures.a_P32_total; }
        /// <summary>
        /// Total linear density of static microfractures
        /// </summary>
        /// <returns></returns>
        public double s_uFP32_total() { return MicroFractures.s_P32_total; }
        /// <summary>
        /// Total volumetric ratio of active microfractures
        /// </summary>
        /// <returns></returns>
        public double a_uFP33_total() { return MicroFractures.a_P33_total; }
        /// <summary>
        /// Total volumetric ratio of static microfractures
        /// </summary>
        /// <returns></returns>
        public double s_uFP33_total() { return MicroFractures.s_P33_total; }
        /// <summary>
        /// Total volumetric density of active half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double a_MFP30_total() { return IPlus_halfMacroFractures.a_P30_total + IMinus_halfMacroFractures.a_P30_total; }
        /// <summary>
        /// Total volumetric density of active half-macrofractures propagating in a specified direction
        /// </summary>
        /// <param name="propdir">Direction of propagation</param>
        /// <returns></returns>
        public double a_MFP30_total(PropagationDirection propdir) { return (propdir == PropagationDirection.IPlus ? IPlus_halfMacroFractures.a_P30_total : IMinus_halfMacroFractures.a_P30_total); }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to stress shadow interaction
        /// </summary>
        /// <returns></returns>
        public double sII_MFP30_total() { return IPlus_halfMacroFractures.sII_P30_total + IMinus_halfMacroFractures.sII_P30_total; }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to stress shadow interaction propagating in a specified direction
        /// </summary>
        /// <param name="propdir">Direction of propagation</param>
        /// <returns></returns>
        public double sII_MFP30_total(PropagationDirection propdir) { return (propdir == PropagationDirection.IPlus ? IPlus_halfMacroFractures.sII_P30_total : IMinus_halfMacroFractures.sII_P30_total); }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to intersection with other fracture sets
        /// </summary>
        /// <returns></returns>
        public double sIJ_MFP30_total() { return IPlus_halfMacroFractures.sIJ_P30_total + IMinus_halfMacroFractures.sIJ_P30_total; }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to intersection with other fracture sets propagating in a specified direction
        /// </summary>
        /// <param name="propdir">Direction of propagation</param>
        /// <returns></returns>
        public double sIJ_MFP30_total(PropagationDirection propdir) { return (propdir == PropagationDirection.IPlus ? IPlus_halfMacroFractures.sIJ_P30_total : IMinus_halfMacroFractures.sIJ_P30_total); }
        /// <summary>
        /// Total linear density of active half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double a_MFP32_total() { return IPlus_halfMacroFractures.a_P32_total + IMinus_halfMacroFractures.a_P32_total; }
        /// <summary>
        /// Total linear density of active half-macrofractures propagating in a specified direction
        /// </summary>
        /// <param name="propdir">Direction of propagation</param>
        /// <returns></returns>
        public double a_MFP32_total(PropagationDirection propdir) { return (propdir == PropagationDirection.IPlus ? IPlus_halfMacroFractures.a_P32_total : IMinus_halfMacroFractures.a_P32_total); }
        /// <summary>
        /// Total linear density of static half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double s_MFP32_total() { return IPlus_halfMacroFractures.s_P32_total + IMinus_halfMacroFractures.s_P32_total; }
        /// <summary>
        /// Total linear density of static half-macrofractures propagating in a specified direction
        /// </summary>
        /// <param name="propdir">Direction of propagation</param>
        /// <returns></returns>
        public double s_MFP32_total(PropagationDirection propdir) { return (propdir == PropagationDirection.IPlus ? IPlus_halfMacroFractures.s_P32_total : IMinus_halfMacroFractures.s_P32_total); }
        /// <summary>
        /// Total volumetric ratio of active half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double a_MFP33_total() { return (Math.PI / 4) * gbc.ThicknessAtDeformation * a_MFP32_total(); }
        /// <summary>
        /// Total volumetric ratio of active half-macrofractures propagating in a specified direction
        /// </summary>
        /// <param name="propdir"></param>
        /// <returns></returns>
        public double a_MFP33_total(PropagationDirection propdir) { return (Math.PI / 4) * gbc.ThicknessAtDeformation * a_MFP32_total(propdir); }
        /// <summary>
        /// Total volumetric ratio of static half-macrofractures
        /// </summary>
        /// <returns>Direction of propagation</returns>
        public double s_MFP33_total() { return (Math.PI / 4) * gbc.ThicknessAtDeformation * s_MFP32_total(); }
        /// <summary>
        /// Total volumetric ratio of static half-macrofractures propagating in a specified direction
        /// </summary>
        /// <param name="propdir"></param>
        /// <returns></returns>
        public double s_MFP33_total(PropagationDirection propdir) { return (Math.PI / 4) * gbc.ThicknessAtDeformation * s_MFP32_total(propdir); }

        // Functions to get cumulative population data for all fractures in the dipset
        /// <summary>
        /// Cumulative volumetric density distribution for active microfractures 
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_uFP30(int index) { return MicroFractures.a_P30[index]; }
        /// <summary>
        /// Cumulative volumetric density distribution for static microfractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double s_uFP30(int index) { return MicroFractures.s_P30[index]; }
        /// <summary>
        /// Cumulative linear density distribution for active microfractures 
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_uFP32(int index) { return MicroFractures.a_P32[index]; }
        /// <summary>
        /// Cumulative linear density distribution for static microfractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double s_uFP32(int index) { return MicroFractures.s_P32[index]; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for active microfractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_uFP33(int index) { return MicroFractures.a_P33[index]; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static microfractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double s_uFP33(int index) { return MicroFractures.s_P33[index]; }
        /// <summary>
        /// Cumulative volumetric density distribution for active half-macrofractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_MFP30(int index) { return IPlus_halfMacroFractures.a_P30[index] + IMinus_halfMacroFractures.a_P30[index]; }
        /// <summary>
        /// Cumulative volumetric density distribution for static half-macrofractures terminated due to stress shadow interaction
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double sII_MFP30(int index) { return IPlus_halfMacroFractures.sII_P30[index] + IMinus_halfMacroFractures.sII_P30[index]; }
        /// <summary>
        /// Cumulative volumetric density distribution for static half-macrofractures terminated due to intersection with other fracture sets
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double sIJ_MFP30(int index) { return IPlus_halfMacroFractures.sIJ_P30[index] + IMinus_halfMacroFractures.sIJ_P30[index]; }
        /// <summary>
        /// Cumulative linear density distribution for active half-macrofractures 
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_MFP32(int index) { return IPlus_halfMacroFractures.a_P32[index] + IMinus_halfMacroFractures.a_P32[index]; }
        /// <summary>
        /// Cumulative linear density distribution for static half-macrofractures 
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double s_MFP32(int index) { return IPlus_halfMacroFractures.s_P32[index] + IMinus_halfMacroFractures.s_P32[index]; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for active half-macrofractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_MFP33(int index) { return (Math.PI / 4) * gbc.ThicknessAtDeformation * a_MFP32(index); }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static half-macrofractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double s_MFP33(int index) { return (Math.PI / 4) * gbc.ThicknessAtDeformation * s_MFP32(index); }

        // Dynamic and geomechanical data
        /// <summary>
        /// Unit vector for the direction of shear stress on the fracture surface
        /// </summary>
        private VectorXYZ shearStressVector;
        /// <summary>
        /// Unit vector for the direction of shear displacement on the fracture surface
        /// </summary>
        private VectorXYZ displacementVector;
        /// <summary>
        /// Pitch of the shear stress vector on the surface of the fractures relative to fracture strike (radians, positive downwards; will return NaN if shear stress is zero)
        /// </summary>
        public double ShearStressPitch{ get; private set; }
        /// <summary>
        /// Pitch of the shear displacement vector on the surface of the fractures relative to fracture strike (radians, positive downwards; will return NaN if shear displacement is zero)
        /// </summary>
        public double DisplacementPitch { get; private set; }
        /// <summary>
        /// Unit vector for the direction of shear stress on the fracture surface
        /// </summary>
        public VectorXYZ ShearStressVector { get { return new VectorXYZ(shearStressVector); } }
        /// <summary>
        /// Unit vector for the direction of shear displacement on the fracture surface
        /// </summary>
        public VectorXYZ DisplacementVector { get { return new VectorXYZ(displacementVector); } }
        /// <summary>
        /// Predominant sense of fracture displacement
        /// </summary>
        public FractureDisplacementSense DisplacementSense
        {
            get
            {
                if (double.IsNaN(DisplacementPitch))
                    return FractureDisplacementSense.Dilatant;
                else if (DisplacementPitch > (0.75 * Math.PI))
                    return FractureDisplacementSense.LeftLateral;
                else if (DisplacementPitch >= (0.25 * Math.PI))
                    return FractureDisplacementSense.Reverse;
                else if (DisplacementPitch > -(0.25 * Math.PI))
                    return FractureDisplacementSense.RightLateral;
                else if (DisplacementPitch >= (-0.75 * Math.PI))
                    return FractureDisplacementSense.Normal;
                else
                    return FractureDisplacementSense.LeftLateral;
            }
        }
        /// <summary>
        /// Recalculate the shear displacement pitch and vector, the fracture mode and the compliance tensor base for the given effective stress tensor
        /// </summary>
        public void RecalculateElasticResponse(Tensor2S CurrentStress)
        {
            // Get stress vector acting on the fracture plane
            VectorXYZ stressOnFracture = CurrentStress * normalVector;

            // Calculate the magnitude of normal stress on the fracture plane, and the shear stresses acting on the fracture plane in the along-strike and downdip directions respectively
            double normalStressMagnitude = normalVector & stressOnFracture;
            double strikeShearStressMagnitude = fs.StrikeVector & stressOnFracture;
            double dipShearStessMagnitude = dipVector & stressOnFracture;

            // Recalculate the shear displacement pitch and vector, and flag if it has changed
            double shearStressMagnitude;
            bool stressVectorChanged = RecalculateStressDisplacementVectors(dipShearStessMagnitude, strikeShearStressMagnitude, out shearStressMagnitude);

            // Determine the new fracture mode, and flag if it has changed. This will be:
            // - Mode 1 if the normal stress on the fracture is tensile or zero (i.e. the fracture is dilatant)
            // - Mode 2 if the normal stress on the fracture is compressive (i.e. the fracture is shear only) and the magnitude of the dip-slip shear stress is greater than the magnitude of the strike-slip shear stress
            // - Mode 3 if the normal stress on the fracture is compressive (i.e. the fracture is shear only) and the magnitude of the dip-slip shear stress is less than the magnitude of the strike-slip shear stress
            // Note that a Mode 1 fracture may still have an element of shear displacement, if it is inclined or oblique to the principal stress orientations
            FractureMode newMode;
            if (normalStressMagnitude <= 0)
                newMode = FractureMode.Mode1;
            else if (Math.Abs(dipShearStessMagnitude) >= Math.Abs(strikeShearStressMagnitude))
                newMode = FractureMode.Mode2;
            else
                newMode = FractureMode.Mode3;
            bool modeChanged = (newMode != Mode);
            Mode = newMode;

            // Check if the fractures can accommodate elastic strain, and flag if this has changed
            // Fractures can only accumulate displacement, and hence accommodate elastic strain, if the driving stress is positive
            // This will be the case if either the normal stress on the fractures is negative (Mode 1 displacement) or the shear stress exceeds the frictional traction (Mode 2 displacement)
            // NB If the driving stress is zero (within rounding error) we will count it as positive - since it is likely to increase over the current timestep
            bool previous_sigmad_positive = (CurrentFractureData.Mean_SigmaD_M > 0);
            bool sigmad_positive = false;
            if (normalStressMagnitude <= PreviousFractureData.MaxDrivingStressRoundingError)
                sigmad_positive = true;
            else if (shearStressMagnitude - (gbc.MechProps.MuFr * normalStressMagnitude) >= -PreviousFractureData.MaxDrivingStressRoundingError)
                sigmad_positive = true;
            bool sigmad_changed = (sigmad_positive != previous_sigmad_positive);

            // Recalculate the compliance tensor base
            // This is only necessary if either:
            // - the fracture mode has changed (from dilatant to shear or vice versa)
            // - the driving stress has changed from positive to negative, or vice versa, so fractures can now / can no longer accommodate elastic strain
            // - the shear displacement vector has changed, for Mode 2 fractures (the compliance tensor is independent of the shear displacement vector for Mode 1 fractures so this does not apply for these)
            if (modeChanged || sigmad_changed || (stressVectorChanged && Mode != FractureMode.Mode1))
                RecalculateComplianceTensorBase(sigmad_positive);
        }
        /// <summary>
        /// Recalculate the shear stress and displacement pitch and vectors if they have changed
        /// </summary>
        /// <param name="DipShearStressMagnitude">Magnitude of the shear stress acting on the fracture in the downdip direction</param>
        /// <param name="StrikeShearStressMagnitude">Magnitude of the shear stress acting on the fracture in the strike direction</param>
        /// <param name="ShearStressMagnitude">Reference parameter for the magnitude of the total shear stress acting on the fracture</param>
        /// <returns>True if the shear displacement vector has changed, false if it has not</returns>
        private bool RecalculateStressDisplacementVectors(double DipShearStressMagnitude, double StrikeShearStressMagnitude, out double ShearStressMagnitude)
        {
            // Calculate pitch of shear stress on the fracture
            // NB there will be two possible shear stress pitches in opposite directions, one with a negative shear stress magnitude and one with a positive shear stress magnitude
            // We will use the one with a positive shear stress magnitude
            // If the shear stress magnitude is zero then the shear displacement pitch will be set to NaN
            double newShearStressPitch = Math.Atan2(DipShearStressMagnitude, StrikeShearStressMagnitude);
            ShearStressMagnitude = (DipShearStressMagnitude * Math.Sin(newShearStressPitch)) + (StrikeShearStressMagnitude * (Math.Cos(newShearStressPitch)));
            if (ShearStressMagnitude == 0)
                newShearStressPitch = double.NaN;

            // If the shear displacement pitch has not changed we do not need to recalculate the shear displacement vector, and can simply return false
            if ((newShearStressPitch == ShearStressPitch) || (double.IsNaN(newShearStressPitch) && double.IsNaN(ShearStressPitch)))
                return false;

            // Update the shear stress and displacement pitch and vectors
            // The shear stress vector is parallel to fracture, in the direction of the shear stress pitch
            // The shear displacement vector is parallel to fracture, in the direction of the displacement pitch
            // The shear stress and displacement vectors will generally be different due to the different elastic responses of dip-slip (plane-) and strike-slip (antiplane-) strain
            // If the shear stress magnitude is zero, will be set to (0,0,0)
            ShearStressPitch = newShearStressPitch;
            if (double.IsNaN(newShearStressPitch))
            {
                shearStressVector = new VectorXYZ(0, 0, 0);
                displacementVector = new VectorXYZ(0, 0, 0);
                DisplacementPitch = double.NaN;
            }
            else
            {
                double nu_r = gbc.MechProps.Nu_r;
                shearStressVector = (Math.Sin(newShearStressPitch) * dipVector) + (Math.Cos(newShearStressPitch) * fs.StrikeVector);
                DisplacementPitch = Math.Atan2(DipShearStressMagnitude, (StrikeShearStressMagnitude / (1 - nu_r)));
                // NB The displacement vector will not be a unit length vector
                // This is necessary to get the correct results when calculating frictional traction and compliance for strike-slip fractures
                displacementVector = (Math.Sin(newShearStressPitch) * dipVector) + ((Math.Cos(newShearStressPitch) / (1 - nu_r)) * fs.StrikeVector);
            }

            // The shear displacement vector has changed so return true
            return true;
        }
        /// <summary>
        /// Recalculate the compliance tensor base, based on the current driving stress, fracture orientation, mode and displacement vector
        /// </summary>
        /// <param name="sigmad_positive">True is the current fracture driving stress is positive, otherwise false</param>
        private void RecalculateComplianceTensorBase(bool sigmad_positive)
        {
            // If the driving stress is positive, the components of the compliance tensor base will be dependent on the current fracture mode, orientation and displacement vector
            if (sigmad_positive)
            {
                switch (Mode)
                {
                    case FractureMode.Mode1:
                        {
                            // For Mode 1 fractures, the microfracture compliance tensor base is most easily generated using the fourth order outer vector product operator on the normal vector
                            // This returns a fourth order tensor C such that Cijkl=(AiBkDjl+AiBlDjk+AjBkDil+AjBlDik)/4, where D is the Kronecker delta
                            uF_ComplianceTensorBase = normalVector | normalVector;
                            uF_ComplianceTensorBase.DoubleShearColumnComponents();
                            // The compliance tensor base for macrofractures is most easily generated using a combination of outer vector product and outer tensor product operators on the normal and shear displacement vectors
                            double oneMinusNur = 1 - gbc.MechProps.Nu_r;
                            VectorXYZ strikeVector = fs.StrikeVector;
                            Tensor2S normal_OP_normal = normalVector ^ normalVector;
                            Tensor2S normal_OP_dip = normalVector ^ dipVector;
                            Tensor2S normal_OP_strike = normalVector ^ strikeVector;
                            MF_ComplianceTensorBase = (normal_OP_normal ^ normal_OP_normal) + (normal_OP_dip ^ normal_OP_dip) + ((normal_OP_strike ^ normal_OP_strike) / oneMinusNur);
                            MF_ComplianceTensorBase.DoubleShearColumnComponents();

                            // Recalculate fracture mode factors
                            if (BiazimuthalConjugate)
                            {
                                double sindip = Math.Sin(Dip);
                                double Maa = sindip;
                                double Mas = 0;
                                double Mss = sindip / 2;
                                Maa_eaa2d_eh2d = fs.eaa2d_eh2d * Maa;
                                Mas_eaaasd_eh2d = fs.eaaasd_eh2d * Mas;
                                Mss_eas2d_eh2d = fs.eas2d_eh2d * Mss;
                            }
                            else
                            {
                                double sindip = Math.Sin(Dip);
                                double oneMinus2Nur = 1 - (2 * gbc.MechProps.Nu_r);
                                double Mff = Math.Pow(oneMinusNur, 2) / oneMinus2Nur;
                                double Mfw = 0;
                                double Mww = oneMinusNur / 2;
                                double Mfs = 0;
                                double Mss = 1 / 2;
                                Maa_eaa2d_eh2d = ((eff2d_e2d * Mff) + (efffwd_e2d * Mfw) + (efw2d_e2d * Mww)) / sindip;
                                Mas_eaaasd_eh2d = (efffsd_e2d * Mfs) / sindip;
                                Mss_eas2d_eh2d = (efs2d_e2d * Mss) / sindip;
                            }

                            // Remove rounding errors
                            {
                                double Mtot = Maa_eaa2d_eh2d + Mas_eaaasd_eh2d + Mss_eas2d_eh2d;
                                if ((float)(Mtot + Maa_eaa2d_eh2d) == (float)Mtot) Maa_eaa2d_eh2d = 0;
                                if ((float)(Mtot + Mas_eaaasd_eh2d) == (float)Mtot) Mas_eaaasd_eh2d = 0;
                                if ((float)(Mtot + Mss_eas2d_eh2d) == (float)Mtot) Mss_eas2d_eh2d = 0;
                            }
                        }
                        break;
                    case FractureMode.Mode2:
                    case FractureMode.Mode3:
                        {
                            // For Mode 2 fractures, the compliance tensor base for both microfractures and macrofractures is most easily generated using a combination of outer vector product and outer tensor product operators on the normal and shear displacement vectors
                            double mufr = gbc.MechProps.MuFr;
                            double oneMinusNur = 1 - gbc.MechProps.Nu_r;
                            VectorXYZ strikeVector = fs.StrikeVector;
                            Tensor2S normal_OP_mu_normal = normalVector ^ (mufr * normalVector);
                            Tensor2S normal_OP_dip = normalVector ^ dipVector;
                            Tensor2S normal_OP_strike = normalVector ^ strikeVector;
                            Tensor4_2Sx2S normal_OP_dip_OP_normal_OP_dip = normal_OP_dip ^ normal_OP_dip;
                            Tensor4_2Sx2S normal_OP_strike_OP_normal_OP_strike = normal_OP_strike ^ normal_OP_strike;
                            Tensor4_2Sx2S uF_frictional_traction = (normalVector ^ shearStressVector) ^ normal_OP_mu_normal;
                            Tensor4_2Sx2S MF_frictional_traction = (normalVector ^ displacementVector) ^ normal_OP_mu_normal;
                            uF_ComplianceTensorBase = normal_OP_dip_OP_normal_OP_dip + normal_OP_strike_OP_normal_OP_strike - uF_frictional_traction;
                            uF_ComplianceTensorBase.DoubleShearColumnComponents();
                            MF_ComplianceTensorBase = normal_OP_dip_OP_normal_OP_dip + (normal_OP_strike_OP_normal_OP_strike / oneMinusNur) - MF_frictional_traction;
                            MF_ComplianceTensorBase.DoubleShearColumnComponents();

                            // Recalculate fracture mode factors
                            double sinpitch, cospitch;
                            if (double.IsNaN(ShearStressPitch))
                            {
                                sinpitch = 0;
                                cospitch = 0;
                            }
                            else
                            {
                                sinpitch = Math.Sin(ShearStressPitch);
                                cospitch = Math.Cos(ShearStressPitch);
                            }
                            if (BiazimuthalConjugate)
                            {
                                double sindip = Math.Sin(Dip);
                                double cosdip = Math.Cos(Dip);
                                double sincosdip = sindip * cosdip;
                                double Maa = cosdip * (sincosdip - (mufr * sinpitch * Math.Pow(sindip, 2)));
                                double Mas = -(mufr / oneMinusNur / 2) * cospitch * Math.Pow(sindip, 2);
                                double Mss = sindip / 2;
                                Maa_eaa2d_eh2d = fs.eaa2d_eh2d * Maa;
                                Mas_eaaasd_eh2d = fs.eaaasd_eh2d * Mas;
                                Mss_eas2d_eh2d = fs.eas2d_eh2d * Mss;
                            }
                            else
                            {
                                double sindip = Math.Sin(Dip);
                                double oneMinus2Nur = 1 - (2 * gbc.MechProps.Nu_r);
                                double Mff = 0;
                                double Mfw = -(Math.Pow(oneMinusNur, 2) / (2 * oneMinus2Nur)) * mufr * sinpitch;
                                double Mww = oneMinusNur / 2;
                                double Mfs = -(oneMinusNur / (2 * oneMinus2Nur)) * mufr * cospitch;
                                double Mss = 1 / 2;
                                Maa_eaa2d_eh2d = ((eff2d_e2d * Mff) + (efffwd_e2d * Mfw) + (efw2d_e2d * Mww)) / sindip;
                                Mas_eaaasd_eh2d = (efffsd_e2d * Mfs) / sindip;
                                Mss_eas2d_eh2d = (efs2d_e2d * Mss) / sindip;
                            }
                            // Remove rounding errors
                            {
                                double Mtot = Maa_eaa2d_eh2d + Mas_eaaasd_eh2d + Mss_eas2d_eh2d;
                                if ((float)(Mtot + Maa_eaa2d_eh2d) == (float)Mtot) Maa_eaa2d_eh2d = 0;
                                if ((float)(Mtot + Mas_eaaasd_eh2d) == (float)Mtot) Mas_eaaasd_eh2d = 0;
                                if ((float)(Mtot + Mss_eas2d_eh2d) == (float)Mtot) Mss_eas2d_eh2d = 0;
                            }
                        }
                        break;
                    default:
                        {
                            // The compliance tensor bases will contain only zero values
                            uF_ComplianceTensorBase = new Tensor4_2Sx2S();
                            MF_ComplianceTensorBase = new Tensor4_2Sx2S();

                            // Set all mode factors to zero
                            Maa_eaa2d_eh2d = 0;
                            Mas_eaaasd_eh2d = 0;
                            Mss_eas2d_eh2d = 0;
                        }
                        break;
                }

                // If this is a biazimuthal conjugate fracture set, we now need to remove the excess YZ,YZ and ZX,ZX components of the tensor
                if (BiazimuthalConjugate)
                {
                    foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    {
                        if ((ij != Tensor2SComponents.YZ) && (ij != Tensor2SComponents.ZX))
                        {
                            uF_ComplianceTensorBase.Component(ij, Tensor2SComponents.YZ, 0);
                            uF_ComplianceTensorBase.Component(Tensor2SComponents.YZ, ij, 0);
                            MF_ComplianceTensorBase.Component(ij, Tensor2SComponents.YZ, 0);
                            MF_ComplianceTensorBase.Component(Tensor2SComponents.YZ, ij, 0);

                            uF_ComplianceTensorBase.Component(ij, Tensor2SComponents.ZX, 0);
                            uF_ComplianceTensorBase.Component(Tensor2SComponents.ZX, ij, 0);
                            MF_ComplianceTensorBase.Component(ij, Tensor2SComponents.ZX, 0);
                            MF_ComplianceTensorBase.Component(Tensor2SComponents.ZX, ij, 0);
                        }
                    }
                }
            }
            // If the driving stress is negative, no displacement can occur on the fractures so the compliance tensor and mode factors will be zero
            else
            {
                // In this case the compliance tensor bases will contain only zero values
                uF_ComplianceTensorBase = new Tensor4_2Sx2S();
                MF_ComplianceTensorBase = new Tensor4_2Sx2S();

                // Set all mode factors to zero
                Maa_eaa2d_eh2d = 0;
                Mas_eaaasd_eh2d = 0;
                Mss_eas2d_eh2d = 0;
            }
        }
        /// <summary>
        /// Base for the microfracture compliance tensor, constructed from a combination of the fracture normal vector and the shear displacement vector
        /// </summary>
        private Tensor4_2Sx2S uF_ComplianceTensorBase;
        /// <summary>
        /// Base for the macrofracture compliance tensor, constructed from a combination of the fracture normal vector and the shear displacement vector
        /// </summary>
        private Tensor4_2Sx2S MF_ComplianceTensorBase;
        /// <summary>
        /// Compliance tensor for this fracture dipset
        /// </summary>
        public Tensor4_2Sx2S S_dipset
        {
            get
            {
                double elasticityMultiplier = (1 - Math.Pow(gbc.MechProps.Nu_r, 2)) / gbc.MechProps.E_r;
                double uF_fractureDensityMultiplier = (4 / Math.PI) * (a_uFP33_total() + s_uFP33_total());
                double MF_fractureDensityMultiplier = (Math.PI) / 2 * gbc.ThicknessAtDeformation * (a_MFP32_total() + s_MFP32_total());
                double sindip = Math.Sin(Dip);
                if (sindip > 0)
                {
                    uF_fractureDensityMultiplier /= sindip;
                    MF_fractureDensityMultiplier /= sindip;
                }
                return elasticityMultiplier * ((uF_fractureDensityMultiplier * uF_ComplianceTensorBase) + (MF_fractureDensityMultiplier * MF_ComplianceTensorBase));
            }
        }

        // Displacement functions
        /// <summary>
        /// Current ratio of maximum microfracture displacement to radius: aperture for Mode 1 microfractures, shear offset for Mode 2 microfractures
        /// </summary>
        /// <returns></returns>
        public double Max_uF_Displacement_r()
        {
            return (8 * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
        }
        /// <summary>
        /// Current ratio of mean microfracture displacement to radius: aperture for Mode 1 microfractures, shear offset for Mode 2 microfractures
        /// </summary>
        /// <returns></returns>
        public double Mean_uF_Displacement_r()
        {
            return (16 * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
        }
        /// <summary>
        /// Current maximum half-macrofracture displacement: aperture for Mode 1 half-macrofractures, shear offset for Mode 2 half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double Max_MF_Displacement()
        {
            return (2 * gbc.ThicknessAtDeformation * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
        }
        /// <summary>
        /// Current mean half-macrofracture displacement: aperture for Mode 1 half-macrofractures, shear offset for Mode 2 half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double Mean_MF_Displacement()
        {
            return (Math.PI * gbc.ThicknessAtDeformation * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
        }
        /// <summary>
        /// Ratio of maximum microfracture displacement to radius at the end of a previous timestep: aperture for Mode 1 microfractures, shear offset for Mode 2 microfractures
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns></returns>
        public double Max_uF_Displacement_r(int timestep)
        {
            return (8 * PreviousFractureData.getFinalDrivingStressSigmaD(timestep) * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
        }
        /// <summary>
        /// Ratio of mean microfracture displacement to radius at the end of a previous timestep: aperture for Mode 1 microfractures, shear offset for Mode 2 microfractures
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns></returns>
        public double Mean_uF_Displacement_r(int timestep)
        {
            return (16 * PreviousFractureData.getFinalDrivingStressSigmaD(timestep) * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
        }
        /// <summary>
        /// Maximum half-macrofracture displacement at the end of a previous timestep: aperture for Mode 1 half-macrofractures, shear offset for Mode 2 half-macrofractures
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns></returns>
        public double Max_MF_Displacement(int timestep)
        {
            return (2 * gbc.ThicknessAtDeformation * PreviousFractureData.getFinalDrivingStressSigmaD(timestep) * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
        }
        /// <summary>
        /// Mean half-macrofracture displacement at the end of a previous timestep: aperture for Mode 1 half-macrofractures, shear offset for Mode 2 half-macrofractures
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns></returns>
        public double Mean_MF_Displacement(int timestep)
        {
            return (Math.PI * gbc.ThicknessAtDeformation * PreviousFractureData.getFinalDrivingStressSigmaD(timestep) * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
        }

        // Porosity & volumetric heave functions
        /// <summary>
        /// Total dynamic fracture porosity for Mode 1 microfractures or volumetric heave for Mode 2 microfractures
        /// </summary>
        /// <returns></returns>
        public double Total_uF_DynamicPorosityHeave()
        {
            double output = (4 * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
            double P33_total = a_uFP33_total() + s_uFP33_total();

            switch (Mode)
            {
                case FractureMode.Mode1:
                    output *= P33_total;
                    break;
                case FractureMode.Mode2:
                case FractureMode.Mode3:
                    output *= P33_total * Math.Cos(Dip);
                    if (!double.IsNaN(ShearStressPitch))
                        output *= Math.Abs(Math.Sin(ShearStressPitch));
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Cumulative dynamic fracture porosity for Mode 1 microfractures or volumetric heave for Mode 2 microfractures, for all microfractures with radius greater than the specified radius array index
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>      
        public double Cumulative_uF_DynamicPorosityHeave(int index)
        {
            double output = (4 * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
            double P33 = a_uFP33(index) + s_uFP33(index);

            switch (Mode)
            {
                case FractureMode.Mode1:
                    output *= P33;
                    break;
                case FractureMode.Mode2:
                case FractureMode.Mode3:
                    output *= P33 * Math.Cos(Dip);
                    if (!double.IsNaN(ShearStressPitch))
                        output *= Math.Abs(Math.Sin(ShearStressPitch));
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Total dynamic fracture porosity for Mode 1 half-macrofractures or volumetric heave for Mode 2 half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double Total_MF_DynamicPorosityHeave()
        {
            double output = (Math.PI * gbc.ThicknessAtDeformation * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
            double P32_total = a_MFP32_total() + s_MFP32_total();

            switch (Mode)
            {
                case FractureMode.Mode1:
                    output *= P32_total;
                    break;
                case FractureMode.Mode2:
                case FractureMode.Mode3:
                    output *= P32_total * Math.Cos(Dip);
                    if (!double.IsNaN(DisplacementPitch))
                        output *= Math.Abs(Math.Sin(DisplacementPitch));
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Cumulative dynamic fracture porosity for Mode 1 half-macrofractures or volumetric heave for Mode 2 half-macrofractures, for all half-macrofractures with half-length greater than the specified half-length array index
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>      
        public double Cumulative_MF_DynamicPorosityHeave(int index)
        {
            double output = (Math.PI * gbc.ThicknessAtDeformation * CurrentFractureData.Final_SigmaD_M * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
            double P32 = a_MFP32(index) + s_MFP32(index);

            switch (Mode)
            {
                case FractureMode.Mode1:
                    output *= P32;
                    break;
                case FractureMode.Mode2:
                case FractureMode.Mode3:
                    output *= P32 * Math.Cos(Dip);
                    if (!double.IsNaN(DisplacementPitch))
                        output *= Math.Abs(Math.Sin(DisplacementPitch));
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Total porosity of all microfractures, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Total_uF_Porosity(FractureApertureType ApertureControl)
        {
            double output = 0;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_uFP32_total() + s_uFP32_total()) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_uFP33_total() + s_uFP33_total()) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_uFP33_total() + s_uFP33_total()) * gbc.MechProps.DynamicApertureMultiplier * (4 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_uFP32_total() + s_uFP32_total()) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Cumulative porosity of all microfractures with radius greater than the specified radius array index, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Cumulative_uF_Porosity(int index, FractureApertureType ApertureControl)
        {
            double output = 0;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_uFP32(index) + s_uFP32(index)) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_uFP33(index) + s_uFP33(index)) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_uFP33(index) + s_uFP33(index)) * gbc.MechProps.DynamicApertureMultiplier * (4 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_uFP32(index) + s_uFP32(index)) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Total porosity of all half-macrofractures, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Total_MF_Porosity(FractureApertureType ApertureControl)
        {
            double output = 0;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_MFP32_total() + s_MFP32_total()) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_MFP33_total() + s_MFP33_total()) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_MFP33_total() + s_MFP33_total()) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_MFP32_total() + s_MFP32_total()) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Cumulative porosity of all half-macrofractures with half-length greater than the specified half-length array index, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Cumulative_MF_Porosity(int index, FractureApertureType ApertureControl)
        {
            double output = 0;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_MFP32(index) + s_MFP32(index)) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_MFP33(index) + s_MFP33(index)) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = Math.Max(-CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_MFP33(index) + s_MFP33(index)) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = Math.Max(CurrentFractureData.SigmaNeff_Final_M, 0);
                    output = (a_MFP32(index) + s_MFP32(index)) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Total porosity of the entire fracture dip set, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Total_Fracture_Porosity(FractureApertureType ApertureControl)
        {
            return Total_uF_Porosity(ApertureControl) + Total_MF_Porosity(ApertureControl);
        }

        // Applied strain components
        // NB for biazimuthal conjugate fracture sets, these are defined in terms of fracture azimuth and strike and are thus contained within the fracture set
        /// <summary>
        /// Ratio of incremental normal strain to total incremental normal strain on the fracture, given by eff^2 / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double eff2d_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental normal strain x downdip shear strain to total incremental normal strain on the fracture, given by eff*efw / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efffwd_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental downdip shear strain to total incremental normal strain on the fracture, given by efw^2 / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efw2d_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental normal strain x alongstrike shear strain to total incremental normal strain on the fracture, given by eff*efs / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efffsd_e2d{ get; set; }
        /// <summary>
        /// Ratio of incremental alongstrike shear strain to total incremental normal strain on the fracture, given by efs^2 / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efs2d_e2d { get { return 1 - eff2d_e2d - efw2d_e2d; } } 
        /// <summary>
        /// Recalculate the applied strain components acting on the fractures, for a specified strain or strain rate tensor
        /// </summary>
        /// <param name="AppliedStrainTensor">Current strain or strain rate tensor</param>
        public void RecalculateStrainRatios(Tensor2S AppliedStrainTensor)
        {
            VectorXYZ strikeVector = fs.StrikeVector;
            VectorXYZ normalStrainOnFracture = AppliedStrainTensor * normalVector;
            VectorXYZ downDipStrainOnFracture = AppliedStrainTensor * dipVector;
            VectorXYZ alongStrikeStrainOnFracture = AppliedStrainTensor * strikeVector;
            double effd = normalVector & normalStrainOnFracture;
            double efwd = dipVector & normalStrainOnFracture;
            double ewwd = dipVector & downDipStrainOnFracture;
            double efsd = strikeVector & normalStrainOnFracture;
            double essd = strikeVector & alongStrikeStrainOnFracture;

            // Set the strain ratios to zero if they are small - this will avoid rounding errors
            double emax = effd + efwd + ewwd + efsd + essd;
            if ((float)(emax + effd) == (float)emax)
                effd= 0;
            if ((float)(emax + efwd) == (float)emax)
                efwd = 0;
            if ((float)(emax + ewwd) == (float)emax)
                ewwd = 0;
            if ((float)(emax + efsd) == (float)emax)
                efsd = 0;
            if ((float)(emax + essd) == (float)emax)
                essd = 0;
            double eff_squared = Math.Pow(effd, 2);
            double efw_squared = Math.Pow(efwd, 2);
            double efs_squared = Math.Pow(efsd, 2);
            double e_squared = eff_squared + efw_squared + efs_squared;
            eff2d_e2d = (e_squared > 0 ? eff_squared / e_squared : 1);
            efw2d_e2d = (e_squared > 0 ? efw_squared / e_squared : 0);
            efffwd_e2d = (e_squared > 0 ? (effd * efwd) / e_squared : 0);
            efffsd_e2d = (e_squared > 0 ? (effd * efsd) / e_squared : 0);
        }

        // Fracture mode factors - these form the basis for the stress shadow width
        // They represent the ratio of far-field displacement (i.e. applied strain) to displacement on a fracture, normalised to remove the effects of fracture size and geometry
        // For convenience, these are combined with the respective strain components when they are calculated, so they need only be multiplied by geometric factors to determine stress shadow widths
        /// <summary>
        /// Fracture Mode Factor Maa: azimuthal strain => azimuthal displacement
        /// </summary>
        private double Maa_eaa2d_eh2d { get; set; }
        /// <summary>
        /// Fracture Mode Factor Mas: strike-parallel shear strain => azimuthal displacement
        /// </summary>
        private double Mas_eaaasd_eh2d { get; set; }
        /// <summary>
        /// Fracture Mode Factor Mss: strike-parallel shear strain => strike-slip displacement
        /// </summary>
        private double Mss_eas2d_eh2d { get; set; }
        /// <summary>
        /// Fracture Mode Factor Mhh: maximum horizontal strain => horizontal displacement
        /// </summary>
        private double Mhh_eh2d { get { return Maa_eaa2d_eh2d + Mas_eaaasd_eh2d + Mss_eas2d_eh2d; } }

        // Stress shadow width
        /// <summary>
        /// Ratio of maximum stress shadow width to microfracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Max_uF_StressShadowWidth_r
        {
            get { return Mhh_eh2d * (8 / Math.PI); }
        }
        /// <summary>
        /// Ratio of mean stress shadow width to microfracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_uF_StressShadowWidth_r
        {
            get { return Mhh_eh2d * (16 / (3 * Math.PI)); }
        }
        /// <summary>
        /// Maximum half-macrofracture stress shadow width - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Max_MF_StressShadowWidth
        {
            get { return Mhh_eh2d * 2 * gbc.ThicknessAtDeformation; }
        }
        /// <summary>
        /// Mean half-macrofracture stress shadow width - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_MF_StressShadowWidth
        {
            get { return Mhh_eh2d * (Math.PI / 2) * gbc.ThicknessAtDeformation; }
        }
        /// <summary>
        /// Azimuthal component of mean half-macrofracture stress shadow width - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_Azimuthal_MF_StressShadowWidth
        {
            get { return Maa_eaa2d_eh2d * (Math.PI / 2) * gbc.ThicknessAtDeformation; }
        }
        /// <summary>
        /// Strike-slip shear component of mean half-macrofracture stress shadow width - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_Shear_MF_StressShadowWidth
        {
            get { return (Mas_eaaasd_eh2d + Mss_eas2d_eh2d) * (Math.PI / 2) * gbc.ThicknessAtDeformation; }
        }

        // Stress shadow volume functions
        /// <summary>
        /// Total half-macrofracture stress shadow volume - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Total_MF_StressShadowVolume()
        {
            // NB this calculation assumes that stress shadow widths do not change in time, so there is no overlap of stress shadows
            // Unlike the FractureSet.combined_MF_StressShadowVolume() function, this function does not check for and cap stress shadow volumes greater than 1
            // It also returns a value regardless of the FractureDistribution case
            double P32_total = a_MFP32_total() + s_MFP32_total();
            return Mean_MF_StressShadowWidth * P32_total;
        }
        /// <summary>
        /// Cumulative stress shadow volume for a given index in the cumulative distribution function arrays - return data regardless of the FractureDistribution case
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double Cumulative_MF_StressShadowVolume(int index)
        {
            // NB this calculation assumes that stress shadow widths do not change in time, so there is no overlap of stress shadows
            // Unlike the FractureSet.combined_MF_StressShadowVolume() function, this function does not check for and cap stress shadow volumes greater than 1
            // It also returns a value regardless of the FractureDistribution case
            double P32 = a_MFP32(index) + s_MFP32(index);
            return Mean_MF_StressShadowWidth * P32;
        }

        // Length functions
        /// <summary>
        /// Mean macrofracture half-length
        /// </summary>
        /// <returns></returns>
        public double Mean_MF_HalfLength()
        {
            return (a_MFP32_total() + s_MFP32_total()) / ((a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total()) * gbc.ThicknessAtDeformation);
        }

        // Connectivity indices
        /// <summary>
        /// Proportion of unconnected macrofracture tips - i.e. active macrofracture tips
        /// </summary>
        /// <returns>Ratio of a_MFP30_total to T_MFP30_total</returns>
        public double UnconnectedTipRatio()
        {
            double T_MFP30 = a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total();
            return (T_MFP30 > 0 ? a_MFP30_total() / T_MFP30 : 1);
        }
        /// <summary>
        /// Proportion of macrofracture tips connected to relay zones - i.e. static macrofracture tips deactivated due to stress shadow interaction
        /// </summary>
        /// <returns>Ratio of sII_MFP30_total to T_MFP30_total</returns>
        public double RelayTipRatio()
        {
            double T_MFP30 = a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total();
            return (T_MFP30 > 0 ? sII_MFP30_total() / T_MFP30 : 0);
        }
        /// <summary>
        /// Proportion of connected macrofracture tips - i.e. static macrofracture tips deactivated due to intersection with orthogonal or oblique fractures
        /// </summary>
        /// <returns>Ratio of sIJ_MFP30_total to T_MFP30_total</returns>
        public double ConnectedTipRatio()
        {
            double T_MFP30 = a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total();
            return (T_MFP30 > 0 ? sIJ_MFP30_total() / T_MFP30 : 0);
        }

        // Weighted macrofracture tip density; used to calculate probability of stress shadow interaction
        /// <summary>
        /// Weighted macrofracture tip density; used to calculate probability of stress shadow interaction for an inward propagating fracture tip
        /// </summary>
        /// <param name="mp_fds">Reference to dip set of inward propagating fracture</param>
        /// <param name="mp_PropDir">Propagation direction of inward propagating fracture tip</param>
        /// <param name="ZetaII_in">Weighting factor for sIIP30 to account for stress shadow blocking by other stress shadows (for default use 0.25)</param>
        /// <param name="ZetaIJ_in">Weighting factor for sIJP30 to account for stress shadow blocking by intersecting fractures (for default use 0)</param>
        /// <returns>Sum of (combined propagation rate * aP30) + (zetaII * inward propagation rate * sIIP30) + (zetaIJ * inward propagation rate * sIJP30) for all fractures in this dip set propagating in the opposite direction to the inward propagating fracture</returns>
        public double Xi(FractureDipSet mp_fds, PropagationDirection mp_PropDir, double ZetaII_in, double ZetaIJ_in)
        {
            // Get propagation direction of terminating fractures
            PropagationDirection mt_PropDir = (mp_PropDir == PropagationDirection.IPlus ? PropagationDirection.IMinus : PropagationDirection.IPlus);

            // Get propagation rates for the inward propagating and terminating half-macrofractures
            double mp_PropagationRate = mp_fds.CurrentFractureData.Mean_MF_PropagationRate_M;
            double mt_PropagationRate = CurrentFractureData.Mean_MF_PropagationRate_M;

            // Calculate and return weighted macrofracture tip density
            return ((mp_PropagationRate + mt_PropagationRate) * a_MFP30_total(mt_PropDir)) + (ZetaII_in * mp_PropagationRate * sII_MFP30_total(mt_PropDir)) + (ZetaIJ_in * mp_PropagationRate * sIJ_MFP30_total(mt_PropDir));
        }
        /// <summary>
        /// Weighted macrofracture tip density; used to calculate probability of stress shadow interaction for an inward propagating fracture tip
        /// </summary>
        /// <param name="mp_fds">Reference to FractureDipSet object for the inward propagating fracture</param>
        /// <param name="mp_PropDir">Propagation direction of inward propagating fracture tip</param>
        /// <returns>Sum of (combined propagation rate * aP30) + (0.25 * inward propagation rate * sIIP30) for all fractures in this dip set propagating in the opposite direction to the inward propagating fracture</returns>
        public double Xi(FractureDipSet mp_fds, PropagationDirection mp_PropDir)
        {
            // Use 0.25 as default value for zetaII (all stress shadows have equal width) and 0 as default value for zetaIJ (intersection fracture length >> stress shadow width)
            return Xi(mp_fds, mp_PropDir, 0.25, 0);
        }

        // Functions to convert between weighted time (proportional to half-macrofracture propagation distance) and real time
        /// <summary>
        /// Convert from half-macrofracture propagation length since start of timestep to real time; for constant driving stress these are proportional but for variable driving stress the ts_PropLength can be as a proxy for stress-weighted time (LTime)
        /// </summary>
        /// <param name="ts_PropLength">Half-macrofracture propagation length since start of timestep LTime (m)</param>
        /// <param name="timestep">Timestep index</param>
        /// <returns>Real time (s)</returns>
        public double ConvertLengthToTime(double length, int timestep)
        {
            double time;

            // Check specified timestep is within range
            if ((timestep >= 0) && (timestep <= PreviousFractureData.NoTimesteps))
            {
                // Cache constants locally
                double h = gbc.ThicknessAtDeformation;
                double b = gbc.MechProps.b_factor;
                double CapA = gbc.MechProps.CapA;
                double Kc = gbc.MechProps.Kc;
                double SqrtPi = Math.Sqrt(Math.PI);
                double hPiKc_factor = Math.Sqrt(2 * h) / (SqrtPi * Kc);

                // Set start time to timestep
                time = PreviousFractureData.getStartTime(timestep);

                // Get U, V
                double tsU = PreviousFractureData.getConstantDrivingStressU(timestep);
                double tsV = PreviousFractureData.getVariableDrivingStressV(timestep);

                if (tsV == 0) // If V is zero (i.e. UniformStrainRelaxation and FractureOnlyStrainRelaxation strain relaxation cases) the driving stress is constant, given by U
                {
                    if (tsU > 0)
                        time += length / (CapA * Math.Pow(hPiKc_factor * tsU, b));
                }
                else // If V is not zero (i.e. NoStrainRelaxation strain relaxation case) the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    double U_factor = Math.Pow(hPiKc_factor * tsU, b) * tsU;
                    double V_factor = ((b + 1) * tsV * length) / CapA;
                    double UV_factor = Math.Pow(hPiKc_factor, -b / (b + 1)) * Math.Pow(V_factor + U_factor, 1 / (b + 1));
                    double UV_factor_minus_U = UV_factor - tsU;
                    // UV_factor should always be greater than U, but due to rounding errors, sometimes (UV_factor - tsU) returns a small negative number when it should return 0
                    // This is incorrect and can cause problems later, so to prevent this we will ensure that UV_factor_minus_U is never less than 0
                    if (UV_factor_minus_U < 0) UV_factor_minus_U = 0;
                    time += (UV_factor_minus_U / tsV);
                }
            }
            // If the specified timestep is out of range, set return value to NaN
            else
            {
                time = double.NaN;
            }

            return time;
        }
        /// <summary>
        /// Convert from real time to half-macrofracture propagation length since start of timestep; for constant driving stress these are proportional but for variable driving stress the ts_PropLength can be as a proxy for stress-weighted time (LTime)
        /// </summary>
        /// <param name="time">Real time (s)</param>
        /// <param name="timestep">Timestep index</param>
        /// <returns>Half-macrofracture propagation length since start of timestep LTime (m)</returns>
        public double ConvertTimeToLength(double time, int timestep)
        {
            double length;

            // Check specified timestep is within range
            if ((timestep >= 0) && (timestep <= PreviousFractureData.NoTimesteps))
            {
                // Cache constants locally
                double h = gbc.ThicknessAtDeformation;
                double b = gbc.MechProps.b_factor;
                double CapA = gbc.MechProps.CapA;
                double Kc = gbc.MechProps.Kc;
                double SqrtPi = Math.Sqrt(Math.PI);
                double hPiKc_factor = Math.Sqrt(2 * h) / (SqrtPi * Kc);

                // Subtract start time of timestep
                time -= PreviousFractureData.getStartTime(timestep);

                // Get U, V
                double tsU = PreviousFractureData.getConstantDrivingStressU(timestep);
                double tsV = PreviousFractureData.getVariableDrivingStressV(timestep);

                if (tsV == 0) // If V is zero (i.e. UniformStrainRelaxation and FractureOnlyStrainRelaxation strain relaxation cases) the driving stress is constant, given by U
                {
                    if (tsU >= 0)
                        length = time * (CapA * Math.Pow(hPiKc_factor * tsU, b));
                    // If U < 0 then the driving stress is negative; therefore the half-macrofracture propagation length is 0
                    else
                        length = 0;
                }
                else // If V is not zero (i.e. NoStrainRelaxation strain relaxation case) the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    double UV_factor1 = tsU + (tsV * time);
                    if (UV_factor1 >= 0) 
                    {
                        double UV_factor2 = Math.Pow(hPiKc_factor * UV_factor1, b) * UV_factor1;
                        double U_factor2 = Math.Pow(hPiKc_factor * tsU, b) * tsU;
                        length = (CapA / ((b + 1) * tsV)) * (UV_factor2 - U_factor2);
                    }
                    // If UV_factor1 < 0 then the driving stress is negative; therefore the half-macrofracture propagation length is 0
                    else
                    {
                        length = 0;
                    }
                }
            }
            // If the specified timestep is out of range, set return value to NaN
            else
            {
                length = double.NaN;
            }

            return length;
        }

        // Dynamic functions
        // Currently these are not required except for debugging purposes
#if DBFUNCTIONS
        /// <summary>
        /// Initial driving stress acting on the fractures, before application of strain
        /// </summary>
        /// <returns></returns>
        public double getInitialSigmaD()
        {
            double output = 0;

            // Get the magnitudes of the normal and shear stresses acting on the fractures
            Tensor2S Sigma_Const = gbc.StressStrain.Sigma_eff;
            double sneff = NormalVector & (Sigma_Const * NormalVector);
            double tau = DisplacementVector & (Sigma_Const * NormalVector);
            double mu = gbc.MechProps.MuFr;

            switch (Mode)
            {
                case FractureMode.Mode1:
                    if (sneff < 0) output = -sneff;
                    break;
                case FractureMode.Mode2:
                case FractureMode.Mode3:
                    if (tau > 0) output = tau - (mu * Math.Max(sneff, 0));
                    break;
                default:
                    break;
            }

            return output;
        }
        /// <summary>
        /// Return the initial radius (at t=0) of a microfracture that reaches r=h/2 and nucleates a macrofracture at the end of a specified specified timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double getInitialMicrofractureRadius(int Timestep_M)
        {
            switch (gbc.MechProps.GetbType())
            {
                case bType.LessThan2:
                    return Math.Pow(getCumhGamma(Timestep_M), gbc.MechProps.beta);
                case bType.Equals2:
                    return Math.Exp(getCumhGamma(Timestep_M));
                case bType.GreaterThan2:
                    return Math.Pow(getCumhGamma(Timestep_M), gbc.MechProps.beta);
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Return the maximum possible volumetric macrofracture density MFP30 (i.e. assuming no fracture deactivation) at the end of a specified timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double getMaximumMacrofracturePopulation(int Timestep_M)
        {
            return CapB * Math.Pow(getInitialMicrofractureRadius(Timestep_M), -c_coefficient);
        }
        /// <summary>
        /// Return the maximum possible macrofracture nucleation rate (i.e. assuming no fracture deactivation) for a specified timestep; for actual macrofracture deactivation rate, this must be multiplied by the clear zone volume theta_dashed
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double getMaximumMacrofractureNucleationRate(int Timestep_M)
        {
            switch (gbc.MechProps.GetbType())
            {
                case bType.LessThan2:
                    return gbc.MechProps.beta * c_coefficient * PreviousFractureData.getuFPropagationRateFactor(Timestep_M) * CapB * Math.Pow(getCumhGamma(Timestep_M), -((gbc.MechProps.beta * c_coefficient) + 1));
                case bType.Equals2:
                    return -c_coefficient * PreviousFractureData.getuFGrowthFactor(Timestep_M) * CapB * Math.Exp(getCumhGamma(Timestep_M) * -c_coefficient);
                case bType.GreaterThan2:
                    return -gbc.MechProps.beta * c_coefficient * PreviousFractureData.getuFPropagationRateFactor(Timestep_M) * CapB * Math.Pow(getCumhGamma(Timestep_M), -((gbc.MechProps.beta * c_coefficient) + 1));
                default:
                    return 0;
            }

        }
#endif

        // Functions to populate the cumulative population distribution function index arrays
        /// <summary>
        /// Populate the microfracture radius index array based on the microfracture calculation bin sizes
        /// </summary>
        /// <param name="ClearExistingValues">Clear all existing values from the array first (if false, will retain all existing values in the array even if they are duplicates)</param>
        /// <param name="no_r_bins">Number of microfracture radius bins used to integrate uFP32 and uFP33 numerically</param>
        public void reset_uF_radii_array(bool ClearExistingValues, int no_r_bins)
        {
            // Clear existing values if this flag is specified
            if (ClearExistingValues)
                uF_radii.Clear();

            // Cache useful variables locally
            double max_uF_radius = gbc.MaximumMicrofractureRadius;

            // Add new values to the array for all bin sizes
            // We will not add a value for zero as this is given by the total microfracture density properties
            // Add a value for all intermediate bin sizes
            for (int r_bin = 1; r_bin < no_r_bins; r_bin++)
            {
                double rb_maxRad = ((double)r_bin / (double)no_r_bins) * max_uF_radius;
                uF_radii.Add(rb_maxRad);
            }
            // Add a final value for the maximum size
            uF_radii.Add(max_uF_radius);
        }
        /// <summary>
        /// Populate the macrofracture halflength index array based on the current halflengths of macrofractures that nucleated at timestep boundaries
        /// </summary>
        /// <param name="ClearExistingValues">Clear all existing values from the array first (if false, will retain all existing values in the array even if they are duplicates)</param>
        /// <param name="cullValue">Only calculate a value for every nth timestep; set to zero to use all timesteps</param>
        public void reset_MF_halflength_array(bool ClearExistingValues, int cullValue)
        {
            // Clear existing values if this flag is specified
            if (ClearExistingValues)
                MF_halflengths.Clear();

            // If cullValue is negative or zero set it to 1 (use all timesteps)
            if (cullValue <= 0) cullValue = 1;

            // Get current timestep number
            int CurrentTimestep = PreviousFractureData.NoTimesteps;

            // Loop backwards through every nth timestep
            // We will not start with the current timestep as this will give a halflength zero, and we will not add a value for zero as this is given by the total halfmacrofracture density properties
            for (int tsM = CurrentTimestep - cullValue; tsM >= 0; tsM -= cullValue)
            {
                // Calculate the current half length of a half-macrofracture that nucleated at the end of timestep M
                double halfLength = PreviousFractureData.getCumulativeHalfLength(CurrentTimestep, tsM);

                // Add this value to the array
                MF_halflengths.Add(halfLength);
            }
        }

        // Functions to calculate fracture population data
        /// <summary>
        /// Create a new FractureCalculationData object for the current timestep, populate it with data from the end of the previous timestep, and add it to the list of previous timestep data
        /// </summary>
        public void setTimestepData()
        {
            // Create a new FractureCalculationData object for the current timestep
            CurrentFractureData = CurrentFractureData.GetNextTimestepData();

            // Now we can add the CurrentFractureData object to the list of previous FractureCalculationData objects in the PreviousFractureData object
            PreviousFractureData.AddTimestep(CurrentFractureData, true);
        }
        /// <summary>
        /// Set the macrofracture density indices aMFP30, sIIMFP30, sIJMFP30 and MFP32 in the CurrentFractureData object
        /// </summary>
        public void setMacrofractureDensityData()
        {
            double MFP32 = a_MFP32_total() + s_MFP32_total();
            CurrentFractureData.SetMacrofractureDensityData(a_MFP30_total(), sII_MFP30_total(), sIJ_MFP30_total(), MFP32);
        }
        /// <summary>
        /// Set the microfracture density indices uFP32 and uFP33 in the CurrentFractureData object
        /// </summary>
        public void setMicrofractureDensityData()
        {
            double uFP32 = a_uFP32_total() + s_uFP32_total();
            double uFP33 = a_uFP33_total() + s_uFP33_total();
            CurrentFractureData.SetMicrofractureDensityData(uFP32, uFP33);
        }
        /// <summary>
        /// Update the mean total and azimuthal stress shadow widths in the CurrentFractureData object
        /// NB we cannot do this as we calculate the new macrofracture density data for the timestep, because we need to keep the previous values until all macrofracture sets have been calculated
        /// </summary>
        public void setStressShadowWidth()
        {
            // Get the new mean total and azimuthal stress shadow width values
            // The mean shear stress shadow width can be calculated from the total and azimuthal widths so does not need to be set here
            double MeanW = 0;
            double MeanAW = 0;
            switch (fs.FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows - mean stress shadow width is 0
                    break;
                case StressDistribution.StressShadow:
                    // Constant stress shadow width
                    MeanW = Mean_MF_StressShadowWidth;
                    MeanAW = Mean_Azimuthal_MF_StressShadowWidth;
                    break;
                case StressDistribution.DuctileBoundary:
                    // Not yet implemented
                    break;
                default:
                    break;
            }

            // Set the new stress shadow widths and return true if the stress shadow widths have changed, false otherwise
            CurrentFractureData.SetStressShadowWidth(MeanAW, MeanW);
        }
        /// <summary>
        /// Check whether either the mean total and azimuthal stress shadow widths have changed, compared to the values in the CurrentFractureData object
        /// </summary>
        /// <returns>True if the stress shadow widths have changed, false if they have not changed</returns>
        public bool checkStressShadowWidthChange()
        {
            switch (fs.FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows, so stress shadow width can never change
                    return false;
                case StressDistribution.StressShadow:
                case StressDistribution.DuctileBoundary:
                default:
                    // Check if either the total or the azimuth stress shadow width calculated from the mode factors is different to that in the FractureCalculationData object
                    return (Mean_MF_StressShadowWidth != CurrentFractureData.Mean_StressShadowWidth_M) || (Mean_Azimuthal_MF_StressShadowWidth != CurrentFractureData.Mean_AzimuthalStressShadowWidth_M);
            }
        }
        /// <summary>
        /// Calculate the constant and variable components of the driving stress (U and V) for the upcoming timestep, and estimate the optimal timestep duration based on a specified maximum increase in fracture density (MFP33)
        /// NB this function should be run before the CurrentFractureData object is updated to the new timestep, so it still contains dynamic data from the previous timestep 
        /// </summary>
        /// <param name="Sigma_Const">Tensor for initial in situ effective stress (Pa)</param>
        /// <param name="Sigma_Var">Tensor for rate of change of effective stress (Pa/s)</param>
        /// <param name="d_MFP33">Maximum allowed increase in MFP33</param>
        /// <returns>Maximum allowable timestep duration (s)</returns>
        public double getOptimalDuration(Tensor2S Sigma_Const, Tensor2S Sigma_Var, double d_MFP33)
        {
            // If it is not possible to calculate a value for the optimal timestep duration, return infinity
            // This will always be greater than any actual calculated optimal duration
            double optdur = double.PositiveInfinity;

            // Set the ratio for comparing initial and rate of change of stress values; if the initial value is less than the rate of change times the comparison ratio, we can round the initial value down to zero
            const double stress_comparator = 0.01;

            // Get the magnitudes of the initial values and the rate of change of the normal and shear stresses acting on the fractures
            // NB sneff, taustrike and taudip represent three orthogonal components of the stress acting on the fault: normal, shear in the direction of strike, and shear in the downdip direction
            // These can be calculated by taking the dot product of the (initial or rate of change of) stress vector on the fracture and the normal, strike or downdip vector of the fracture
            VectorXYZ strikeVector = fs.StrikeVector;
            VectorXYZ SigmaF_Const = Sigma_Const * normalVector;
            VectorXYZ SigmaF_Var = Sigma_Var * normalVector;
            double sneff_cst = normalVector & SigmaF_Const;
            double sneff_var = normalVector & SigmaF_Var;
            // If sneff_cst << sneff_var or sneff_cst is less than the maximum driving stress error then we can assume this is rounding error and set snd_cst to 0; otherwise we will get stuck in a loop
            if ((Math.Abs(sneff_cst) < Math.Abs(sneff_var * stress_comparator)) || (Math.Abs(sneff_cst) <= PreviousFractureData.MaxDrivingStressRoundingError))
                sneff_cst = 0;
            double taudip_cst = dipVector & SigmaF_Const;
            double taudip_var = dipVector & SigmaF_Var;
            // If taudip_cst << taudip_var or taudip_cst is less than the maximum driving stress error then we can assume this is rounding error and set taudip_cst to 0
            if ((Math.Abs(taudip_cst) < (taudip_var * stress_comparator)) || (Math.Abs(taudip_cst) <= PreviousFractureData.MaxDrivingStressRoundingError))
                taudip_cst = 0;
            double taustrike_cst = strikeVector & SigmaF_Const;
            double taustrike_var = strikeVector & SigmaF_Var;
            // If taustrike_cst << taustrike_var or taustrike_cst is less than the maximum driving stress error then we can assume this is rounding error and set taustrike_cst to 0
            if ((Math.Abs(taustrike_cst) < (taustrike_var * stress_comparator)) || (Math.Abs(taustrike_cst) <= PreviousFractureData.MaxDrivingStressRoundingError))
                taustrike_cst = 0;

            // Check whether the normal stress on the fractures is tensile (i.e. the fractures are dilatant) or compressive (the fractures are closed)
            // NB If the sneff_cst is zero but sneff_var is negative, the normal stress on the fractures will be tensile during most of the timestep so we flag the fractures as dilatant
            bool dilatant = (sneff_cst < 0) || ((sneff_cst == 0) && (sneff_var < 0));

            // Calculate initial estimates for U and V
            // U is the initial driving stress at the start of the timestep
            // V is the rate of change of driving stress (in SI units, Pa/s) at the start of the timestep
            // NB V may not be constant, as the shear displacement vector may change through time as the in situ stress changes
            double U = 0;
            double V = 0;
            // If the fractures are dilatant (i.e. the initial normal stress on them is tensile), we do not need to take into account friction
            if (dilatant)
            {
                // For vertical dilatant (Mode 1) fractures, the driving stress will equal the tensile normal stress on the fractures
                // For inclined dilatant fractures, the driving stress will equal the root of the square of the normal and shear stress components acting on the fractures
                U = Math.Sqrt(Math.Pow(sneff_cst, 2) + Math.Pow(taudip_cst, 2) + Math.Pow(taustrike_cst, 2));
                // If U is zero, V can also be calculated by taking the square of the normal and shear stress components acting on the fractures
                // If U is not zero, we will have to calculate V by differentiating the expression for driving stress in terms of its three components sneff, taudip and taustrike
                // NB the rate of change of driving stress may not be constant, since the shear displacement vector may change through time as the in situ stress changes
                // Here we set V to the rate of change of driving stress at the start of the timestep
                if ((float)U == 0f)
                    V = Math.Sqrt(Math.Pow(sneff_var, 2) + Math.Pow(taudip_var, 2) + Math.Pow(taustrike_var, 2));
                else
                    V = ((sneff_cst * sneff_var) + (taudip_cst * taudip_var) + (taustrike_cst * taustrike_var)) / U;
            }
            // If initial normal stress on fracture is compressive, we must take into account friction
            else
            {
                // For inclined closed (Mode 2) fractures, the driving stress will equal the shear stress on the fractures minus the frictional traction
                // We must therefore start by calculating the magnitude of the shear stress in the direction of shear displacement (i.e. the maximum shear stress, tau)
                // The initial shear stress (tau_cst) can be calculated by taking the root of the squares of the orthogonal strike and downdip shear stress components
                double tau_cst = Math.Sqrt(Math.Pow(taudip_cst, 2) + Math.Pow(taustrike_cst, 2));
                // If tau_cst is zero, the variable component of the maximum shear stress (tau_var) can also be calculated by taking the root of the squares of the orthogonal strike and downdip shear stress components
                // If tau_cst is not zero, we will have to calculate tau_var by differentiating the expression for maximum shear stress in terms of its two components taudip and taustrike
                // NB the rate of change of shear stress may not be constant, since the shear displacement vector may change through time as the in situ stress changes
                // Here we set tau_var to the rate of change of shear stress at the start of the timestep
                double tau_var = Math.Sqrt(Math.Pow(taudip_var, 2) + Math.Pow(taustrike_var, 2));
                if ((float)tau_cst > (float)tau_var)
                    tau_var = ((taudip_cst * taudip_var) + (taustrike_cst * taustrike_var)) / tau_cst;

                // We also need to know the coefficient of friction on the fractures
                double MuFr = gbc.MechProps.MuFr;

                // We can now calculate U and V as the shear stress on the fractures minus the frictional traction
                // NB the rate of change of driving stress may not be constant, since tau_var may change through time
                // Here we set V to the rate of change of driving stress at the start of the timestep
                U = tau_cst - (MuFr * sneff_cst);
                V = tau_var - (MuFr * sneff_var);
            }

            // Set the constant and variable components of normal stress on the fracture in the current Fracture Calculation Data object
            CurrentFractureData.SigmaNeff_Const_M = sneff_cst;
            CurrentFractureData.SigmaNeff_Var_M = sneff_var;

            // Now we can calculate the optimal timestep duration for this fracture set

            // If the fracture set has been deactivated, do not calculate an optimal duration
            // Instead we return the default value infinity (no optimal duration calculated)
            if (CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated)
            {
                // No calculation required
            }
            // If the initial driving stress is negative, the optimal timestep duration should be the time until it becomes positive
            // In fact we compare the initial driving stress to the maximum rounding error, not zero, to determine if this is the case.
            // This is because calculating the time required for driving stress to reach zero, multiplying it by horizontal strain rate to increment horizontal strain, and using new horizontal strain to calculate driving stress does not always give a driving stress = 0 (as it should).
            // Sometimes due to rounding errors in the calculation, it generates a slightly negative driving stress. This can cause the calculation to get stuck in a loop, with no strain increments and no fracture growth, until the maximum number of timesteps is reached.
            else if (U < -PreviousFractureData.MaxDrivingStressRoundingError)
            {
                // First we can calculate the time taken for the driving stress to reach zero based on the rate of change of driving stress
                // Note that we must be dealing with Mode 2 shear fractures at this point, because if the fractures were dilatant then U must already be positive or zero
                // Therefore this represents the time until Mode 2 shear displacement will occur   
                // If the driving stress is static or decreasing then it will never reach zero, so we cannot calculate an optimal duration in this way
                if (V > 0)
                {
                    // However calculating the time taken for the driving stress to reach zero for a Mode 2 fracture is more complicated than simply dividing the negative initial driving stress -U by the rate of change of driving stress V
                    // As we have noted previously, the rate of change of driving stress may itself change through time, as the shear displacement vector and in situ stress change
                    // We must therefore use a quadratic expression comprising the three orthogonal components of the stress acting on the fault: normal, shear in the direction of strike, and shear in the downdip direction
                    // The rates of change of these components do not change through time

                    // Calculate the multiplier for the normal stress component
                    double mufr_squared = Math.Pow(gbc.MechProps.MuFr, 2);

                    // Calculate the three quadratic terms
                    double a = Math.Pow(taustrike_var, 2) + Math.Pow(taudip_var, 2) - (mufr_squared * Math.Pow(sneff_var, 2));
                    double b = 2 * ((taustrike_cst * taustrike_var) + (taudip_cst * taudip_var) - (mufr_squared * sneff_cst * sneff_var));
                    double c = Math.Pow(taustrike_cst, 2) + Math.Pow(taudip_cst, 2) - (mufr_squared * Math.Pow(sneff_cst, 2));
                    double rootterm = Math.Sqrt(Math.Pow(b, 2) - (4 * a * c));

                    // Take the lowest positive root as the optimal timestep duration
                    // If U is negative and V is positive (i.e. the initial driving stress is negative but increasing) then at least one root should be positive
                    // However in case neither are, or the quadratic does not have real roots (i.e. rootterm is NaN) then we can approximate the optimal timestep duration by dividing the negative initial driving stress -U by the rate of change of driving stress V
                    double negativeroot = (-b - rootterm) / (2 * a);
                    double positiveroot = (-b + rootterm) / (2 * a);
                    if ((negativeroot > PreviousFractureData.MaxDrivingStressRoundingError) && (negativeroot < positiveroot))
                        optdur = negativeroot;
                    else if (positiveroot > PreviousFractureData.MaxDrivingStressRoundingError)
                        optdur = positiveroot;
                    else
                        optdur = -U / V;
                }

                // The driving stress will also become positive if the normal stress on the fracture becomes tensile, so the fracture becomes dilatant 
                // If this happens before the driving stress for Mode 2 shear displacement reaches zero, we will set this as the optimal timestep duration 
                if ((sneff_var < 0) && (-(sneff_cst / sneff_var) < optdur))
                    optdur = -(sneff_cst / sneff_var);

                // NB if both V < 0 and sneff_var > 0 then we cannot calculate an optimal duration and will return the default value infinity (no optimal duration calculated)

                // Update the maximum driving stress rounding error
                PreviousFractureData.UpdateMaxDrivingStressRoundingError(U);

                // Since the initial driving stress is less than zero, there will be no fracture growth in this timestep
                // We will therefore now set U and V to zero
                U = 0;
                V = 0;
            }
            // If we are not allowing reverse fractures and the new displacement vector gives a reverse sense of displacement, there will be no fracture growth in this timestep
            // We can therefore set U and V to zero
            // In this case we cannot calculate an optimal duration and will return the default value infinity (no optimal duration calculated)
            else if (!IncludeReverseFractures && DisplacementSense == FractureDisplacementSense.Reverse)
            {
                U = 0;
                V = 0;
            }
            // If the initial driving stress is positive or zero, the optimal timestep duration will be the estimated minimum time taken for the fracture set to grow by a specified amount
            // This can be calculated from the appropriate equations
            // NB If the initial driving stress U is zero and the rate of change of driving stress V is negative, the fracture set will not grow, so we cannot calculate an optimal duration and will return the default value -1
            else
            {
                // If U is less than zero due to rounding error, we must round it up to zero
                if (U < 0) U = 0;

                // Cache constants locally
                double h = gbc.ThicknessAtDeformation;
                double half_h = h / 2;
                double b = gbc.MechProps.b_factor;
                double beta = gbc.MechProps.beta;
                bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);
                double CapA = gbc.MechProps.CapA;
                double Kc = gbc.MechProps.Kc;
                double SqrtPi = Math.Sqrt(Math.PI);
                double sqrtpi_Kc_factor = 2 / (SqrtPi * Kc);
                double alpha_uF_b_factor = Math.Pow(CapA, -1 / (b + 1)) * Math.Pow(sqrtpi_Kc_factor, -b / (b + 1));
                // hb1_factor is (h/2)^(b/2), = h/2 if b=2
                // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
                double hb1_factor = (bis2 ? half_h : Math.Pow(half_h, b / 2)); 
                // initial_uF_factor is a component related to the maximum microfracture radius rmax, included in Cum_hGamma to represent the initial population of seed macrofractures: ln(rmax) for b=2; rmax^(1/beta) for b!=2
                double initial_uF_factor = gbc.Initial_uF_factor;

                // Calculate local helper variables
                // betac_factor is -beta*c if b<>2, -c if b=2
                double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
                // betac1_factor is (1 - (beta c)) if b!=2, -c if b=2
                double betac1_factor = (bis2 ? -c_coefficient : 1 - ((2 * c_coefficient) / (2 - b)));
                // beta_betac1_factor is -beta / (1 - (beta c)) if b!=2, 1 / c if b=2
                double beta_betac1_factor = (bis2 ? 1 / c_coefficient : -2 / (2 - b - (2 * c_coefficient)));
                // Cache the cumulative Gamma factor from the previous timestep locally
                double ts_CumhGamma_Nminus1 = CurrentFractureData.Cum_Gamma_Mminus1 + initial_uF_factor;
                // Calculate helper variables related to the cumulative Gamma factor
                double ts_CumhGammaNminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGamma_Nminus1) : Math.Pow(ts_CumhGamma_Nminus1, betac_factor));
                double ts_CumhGammaNminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGamma_Nminus1) : Math.Pow(ts_CumhGamma_Nminus1, betac1_factor));

                // Calculate ratio of active half macrofractures to maximum potential half macrofractures at the end of the previous timestep - this will depend on the stage of fracture evolution
                double ts_ahalfMF_uF_ratio;
                switch (CurrentFractureData.EvolutionStage)
                {
                    case FractureEvolutionStage.NotActivated:
                        // If the fracture set has not yet been activated, all initial fractures will be active so the ratio of active to total half-macrofractures will be 1
                        ts_ahalfMF_uF_ratio = 1;
                        // Activate the fracture set
                        CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Growing);
                        break;
                    case FractureEvolutionStage.Growing:
                        // If the fracture set is growing, calculate the ratio based on populations of active and total half macrofractures at the end of the previous timestep
                        ts_ahalfMF_uF_ratio = a_MFP30_total() / (2 * CapB * ts_CumhGammaNminus1_betac_factor);
                        break;
                    case FractureEvolutionStage.ResidualActivity:
                        // If there is only residual fracture activity, calculate the ratio based on populations of active and total half macrofractures at the end of the previous timestep
                        // NB we cannot use the formula for the proportion of active fractures in residual fracture populations, as we do not yet know the microfracture propagation rate coefficient gamma_InvBeta_M or the instantaneous probability of macrofracture deactivation Instantaneous_F_M for this timestep
                        //ts_ahalfMF_uF_ratio = (betac_factor * CurrentFractureData.gamma_InvBeta_M) / (CurrentFractureData.Instantaneous_F_M * ts_CumhGamma_Nminus1);
                        ts_ahalfMF_uF_ratio = a_MFP30_total() / (2 * CapB * ts_CumhGammaNminus1_betac_factor);
                        break;
                    default:
                        // As a default, set the ratio of active to total half-macrofractures to 1
                        ts_ahalfMF_uF_ratio = 1;
                        break;
                }

                // Calculate helper variables related to the maximum allowable increase in MFP33
                double d_MFP33_factor = (2 * d_MFP33) / (beta_betac1_factor * ts_ahalfMF_uF_ratio * CapB * Math.PI * Math.Pow(h, 2) * hb1_factor);
                double d_MFP33_CumhGammaM_betac1_factor1 = (bis2 ? Math.Log(d_MFP33_factor + ts_CumhGammaNminus1_betac1_factor) / -c_coefficient : Math.Pow(d_MFP33_factor + ts_CumhGammaNminus1_betac1_factor, 1 / betac1_factor));
                double d_MFP33_CumhGammaM_betac1_factor2 = (ts_CumhGamma_Nminus1 - d_MFP33_CumhGammaM_betac1_factor1);
                double V_factor = beta * (b + 1) * V * d_MFP33_CumhGammaM_betac1_factor2;
                double U_factor1 = CapA * Math.Pow(sqrtpi_Kc_factor * U, b);
                double U_factor2 = U * U_factor1;

                // If the ratio of active to maximum potential half macrofractures is close to zero, the specified d_MFP33 will never be reached
                // If (V_factor + U_factor1) <= 0 then driving stress is decreasing and will never be sufficient to reach the specified d_MFP33
                // In these cases we cannot calculate an optimal duration, so will deactivate the fracture set and return the default value infinity (no optimal duration calculated)
                if (((float)ts_ahalfMF_uF_ratio > 0f) && ((float)(V_factor + U_factor1) > 0f))
                {
                    double UV_factor = alpha_uF_b_factor * Math.Pow(V_factor + U_factor2, 1 / (b + 1));

                    // If U>>V then the exact equation for optimal duration may give zero because (V_factor + U_factor2) ^ (1 / (b + 1)) is indistinguishable from U due to rounding
                    if (UV_factor > U)
                    {
                        // Use the formula for increasing stress to calculate optimal duration
                        optdur = (UV_factor - U) / V;
                    }
                    else // In this case we can approximate V=0 and use the constant driving stress formula
                    {
                        // Use the formula for constant stress to calculate optimal duration, and set V to zero
                        optdur = d_MFP33_CumhGammaM_betac1_factor2 / (U_factor1 / beta);
                        V = 0;
                    }
                }
                else
                {
                    CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
                }

                // Finally, if the normal stress on the fracture will reach zero (from either a positive or negative value) before the calculated optimal duration, we will set this as the optimal timestep duration
                // This can only happen if it is moving in the right direction
                // NB we will also set this to be the optimal timestep duration if we have not yet calculated an optimal duration (i.e. if optdur = -1)
                if ((Math.Sign(sneff_cst) == -Math.Sign(sneff_var)) && (sneff_var != 0))
                {
                    double timeToSneffZero = -(sneff_cst / sneff_var);
                    if (timeToSneffZero < optdur)
                        optdur = timeToSneffZero;
                }
            }

            // Set the constant and variable components of driving stress in the current Fracture Calculation Data object
            CurrentFractureData.U_M = U;
            CurrentFractureData.V_M = V;

            // Return the calculated maximum duration
            return optdur;
        }
        /// <summary>
        /// Set the duration, driving stress and propagation rate data for the current timestep; this can only be done after U and V are set using the getOptimalDuration function
        /// </summary>
        /// <param name="CurrentTime_in">Time at start of timestep (s)</param>
        /// <param name="TimestepDuration_in">Timestep duration (s)</param>
        public void setTimestepPropagationData(double CurrentTime_in, double TimestepDuration_in)
        {
            // Get a reference to the mechanical property data object for the gridblock
            MechanicalProperties MechProps = gbc.MechProps;

            // Set the timestep start time
            CurrentFractureData.M_StartTime = CurrentTime_in;

            // Cache required mechanical properties locally
            double h = gbc.ThicknessAtDeformation;
            double half_h = h / 2;
            double CapA = MechProps.CapA;
            double b = MechProps.b_factor;
            double beta = MechProps.beta;
            bType b_type = MechProps.GetbType();
            bool bis2 = (b_type == bType.Equals2);
            double Kc = MechProps.Kc;
            double SqrtPi = Math.Sqrt(Math.PI);
            //double sqrtpi_Kc_factor = 2 / (SqrtPi * Kc);
            double sqrtpi_Kc_h_factor = Math.Sqrt(2 * h) / (SqrtPi * Kc);
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb1_factor = (bis2 ? half_h : Math.Pow(half_h, b / 2));
            // Flag to show that the fracture set has not been deactivated
            bool FracturesActive = !(CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated);

            // Set the flag for whether subcritical fracture propagation index b is less than, equal to or greater than 2
            CurrentFractureData.M_bType = b_type;

            // Cache constant and variable components of driving stress for this timestep locally
            double U_M = CurrentFractureData.U_M;
            double V_M = CurrentFractureData.V_M;

            // Calculate weighted mean driving stress (Pa), mean half-macrofracture propagation rate (m/s) and microfracture propagation rate coefficient (gamma ^ 1/beta) during timestep M
            // These will have default values of zero if there is no fracture propagation in this timestep
            // Weighted mean driving stress during timestep M (Pa)
            double mean_SigmaD_M = 0;
            // Mean half-macrofracture propagation rate during timestep M (m/s)
            double mean_MF_PropRate = 0;
            // Microfracture propagation rate coefficient (gamma ^ 1/beta) - a helper function related to microfracture propagation rate for timestep M 
            // (alpha_uF / |B|) * SigmaD^b (m^(1+b/2)/s) for b!=2; alpha_uF * SigmaD^2 (m^2/s) for b=2
            // This is the same for both constant and variable driving stress, but is calculated differently to optimise accuracy
            double uF_PropRate_Coefficient = 0;
            if ((float)U_M >= 0f) // If the initial driving stress is less than zero, the mean driving stress for the timestep will be zero
            {
                // Calculate the final driving stress for the timestep
                double final_SigmaD_M = U_M + (TimestepDuration_in * V_M);

                if ((float)final_SigmaD_M == (float)U_M) // If the final driving stress is equal to the initial driving stress then assume the driving stress is constant; NB we check this rather than checking for V_M equals zero, as this will also pick up very short timesteps, where the change in driving stress during the timestep is negligible even though V_M > 0
                {
                    // To calculate alpha_MF * SigmaDb_M (for macrofracture propagation rate) and alpha_uF * SigmaDb_M (for gamma_InvBeta_M), we divide mean_SigmaD_M by Kc before raising it to b, to avoid excessively large numbers
                    mean_SigmaD_M = U_M;
                    // We will only calculate the mean half-macrofracture propagation rate and microfracture propagation rate coefficient if the fractures are active
                    if (FracturesActive)
                    {
                        mean_MF_PropRate = CapA * Math.Pow(sqrtpi_Kc_h_factor * mean_SigmaD_M, b);
                        if (hb1_factor > 0)
                            uF_PropRate_Coefficient = (bis2 ? mean_MF_PropRate / hb1_factor : mean_MF_PropRate / (hb1_factor * Math.Abs(beta)));
                    }
                }
                else // If final driving stress is equal to the initial driving stress then the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    // We will combine the U and V power terms with Kc to avoid getting extreme values when b is high
                    double UV_U_term = (final_SigmaD_M < 0 ? 0 : (final_SigmaD_M * Math.Pow(sqrtpi_Kc_h_factor * final_SigmaD_M, b)) - (U_M * Math.Pow(sqrtpi_Kc_h_factor * U_M, b)));

                    mean_SigmaD_M = Math.Pow(UV_U_term / ((b + 1) * V_M * TimestepDuration_in), 1 / b) / sqrtpi_Kc_h_factor;
                    // We will only calculate the mean half-macrofracture propagation rate and microfracture propagation rate coefficient if the fractures are active
                    if (FracturesActive)
                    {
                        mean_MF_PropRate = CapA * (UV_U_term / ((b + 1) * V_M * TimestepDuration_in));
                        if (hb1_factor > 0)
                            uF_PropRate_Coefficient = (bis2 ? mean_MF_PropRate / hb1_factor : mean_MF_PropRate / (hb1_factor * Math.Abs(beta)));
                    }
                }
            }

            // Set the timestep duration mean driving stress, mean macrofracture propagation rate and microfracture propagation rate coefficient (= gamma ^ 1/beta)
            CurrentFractureData.SetDynamicData(TimestepDuration_in, mean_SigmaD_M, uF_PropRate_Coefficient, mean_MF_PropRate);
        }
        /// <summary>
        /// Set the deactivation rate parameters for macrofractures; NB normally these can only be calculated after macrofracture propagation rates have been calculated for all fracture dip sets
        /// </summary>
        public void setMacrofractureDeactivationRate()
        {
            // Calculate the probability that an active half-macrofracture will not be deactivated due to stress shadow interaction during this timestep
            double PhiII_M = fs.Calculate_PhiII_ByTime(this, PropagationDirection.IPlus, CurrentFractureData.M_Duration);

            // Calculate the probability that an active half-macrofracture in this gridblock will not be deactivated due to intersecting another fracture during this timestep
            double propagationDistance = CurrentFractureData.halfLength_M;
            // Since probability of macrofracture intersection is length-dependent, we must take into account the existing mean length of active half-macrofractures
            double a_MFP30 = a_MFP30_total();
            double currentMeanActiveHalfMacrofractureLength = (a_MFP30 > 0 ? a_MFP32_total() / (gbc.ThicknessAtDeformation * a_MFP30_total()) : 0);
            double PhiIJ_l0 = fs.Calculate_PhiIJ_ByDistance(this, currentMeanActiveHalfMacrofractureLength);
            double PhiIJ_l0plusDl = fs.Calculate_PhiIJ_ByDistance(this, currentMeanActiveHalfMacrofractureLength + propagationDistance);
            double PhiIJ_M = (PhiIJ_l0 > 0 ? PhiIJ_l0plusDl / PhiIJ_l0 : 0);
            if (PhiIJ_M > 1) PhiIJ_M = 1;

            // Calculate the instantaneous probability of deactivation due to intersecting another macrofracture, for a macrofracture tip in a random location, as a proportion of current half-macrofracture population (/s)
            // Since macrofracture intersection is a semi-regular process when there are stress shadows, this cannot always be calculated from Phi_IJ so is stored separately
            // This will be the sum of the instantaneous probabilities of half-macrofracture deactivation by intersecting every other fracture set
            // The instantaneous probability of deactivation of a macrofracture tip in a random location, by intersecting a specific fracture set, can be calculated from the mean linear density of that fracture set
            // Note that this is an approximation, as it does not take into account the existing length of the fractures (i.e. it assumes they have length 0 at the start of the current timestep)
            // Nor does it take into account restriction on the nucleation position of half-macrofractures
            // However this should not be a major problem as the instantaneous deactivation probabilities are mostly used during the residual activity stage when the fractures are short
            double inst_FIJ = 0;
            foreach (Gridblock_FractureSet other_fs in gbc.FractureSets)
                if (other_fs != fs)
                {
                    double intersectionAngleSin = Math.Abs(Math.Sin(fs.Strike - other_fs.Strike));
                    double setP32 = 0;
                    foreach (FractureDipSet fds in other_fs.FractureDipSets)
                        setP32 += fds.getTotalMFP32();
                    inst_FIJ += (setP32 * intersectionAngleSin);
                }
            inst_FIJ *= CurrentFractureData.Mean_MF_PropagationRate_M;

            // Update the FractureCalculationData object with the calculate probabilities - all other related probabilities will be calculated automatically
            // If either PhiII_M or PhiIJ_M are zero, the fracture evolution stage will also automatically be set to Deactivated
            CurrentFractureData.SetMacrofractureDeactivationRates(PhiII_M, PhiIJ_M, inst_FIJ);
        }
        /// <summary>
        /// Update the values describing the cumulative macrofracture spacing distribution function
        /// </summary>
        /// <param name="AA_in">Macrofracture spacing distribution coefficient</param>
        /// <param name="BB_in">Macrofracture spacing distribution exponent</param>
        /// <param name="CCstep_in">Step change in macrofracture spacing distribution offset between this and the next dipset (CCr+1 - CCr)</param>
        public void setMacrofractureSpacingData(double AA_in, double BB_in, double CCStep_in)
        {
            CurrentFractureData.SetMacrofractureSpacingData(AA_in, BB_in, CCStep_in);
        }
        /// <summary>
        /// Update the values describing the inverse stress shadow and clear zone volumes for this fracture set
        /// </summary>
        /// <param name="theta_in">Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M</param>
        /// <param name="theta_dashed_in">Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of timestep M</param>
        /// <param name="dChi_dMFP32_M_in">Rate of increase of exclusion zone volume when adding new macrofractures from this dipset, i.e. the gradient of (1 - theta_dashed) / Total_MFP32</param>
        public void setMacrofractureExclusionZoneData(double theta_in, double theta_dashed_in, double dChi_dMFP32_M_in)
        {
            CurrentFractureData.SetMacrofractureExclusionZoneData(theta_in, theta_dashed_in, dChi_dMFP32_M_in);
        }
        /// <summary>
        /// Update the values describing the inverse stress shadow and clear zone volumes for all other fracture sets
        /// </summary>
        /// <param name="psi_allFS_in">Total stress shadow volume for all fracture sets (including this one) as seen by this dipset, i.e. cumulative probability that an initial microfracture from this fracture lies in the stress shadow of another fracture set relative to this dipset, at end of timestep M</param>
        /// <param name="chi_allFS_in">Total exclusion zone volume for all fracture sets (including this one) as seen by this dipset, i.e. cumulative probability that an initial microfracture from this fracture set lies in the exclusion zone of another fracture set relative to this dipset, at end of timestep M</param>
        public void setOtherFSExclusionZoneData(double psi_allFS_in, double chi_allFS_in)
        {
            CurrentFractureData.SetOtherFSExclusionZoneData(psi_allFS_in, chi_allFS_in);
        }
        /// <summary>
        /// Calculate the increment in total active and static half-macrofracture density (a_MFP30, sII_MFP30, sIJ_MFP30, a_MFP32 and s_MFP32) during the current timestep, in response to the applied driving stress and taking into account fracture deactivation
        /// </summary>
        /// <param name="CurrentTimestep"></param>
        public void calculateTotalMacrofracturePopulation()
        {
            // Cache constants locally
            double h = gbc.ThicknessAtDeformation;
            double max_uF_radius = gbc.MaximumMicrofractureRadius;
            double b = gbc.MechProps.b_factor;
            double beta = gbc.MechProps.beta;
            bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);
            double CapA = gbc.MechProps.CapA;
            double Kc = gbc.MechProps.Kc;
            double SqrtPi = Math.Sqrt(Math.PI);
            int tsN = PreviousFractureData.NoTimesteps;
            double MeanW = 0;
            switch (fs.FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows - mean stress shadow width is 0
                    break;
                case StressDistribution.StressShadow:
                    // Constant stress shadow width
                    MeanW = Mean_MF_StressShadowWidth;
                    break;
                case StressDistribution.DuctileBoundary:
                    // Not yet implemented
                    break;
                default:
                    break;
            }

            // Calculate local helper variables
            // betac_factor is -beta*c if b<>2, -c if b=2
            double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
            // betacminus1_factor is -(beta*c) - 1 if b<>2, -c if b=2
            double betacminus1_factor = (bis2 ? -c_coefficient : -((2 * c_coefficient) / (2 - b)) - 1);
            // betac1_factor is (1 - (beta c)) if b!=2, -c if b=2
            double betac1_factor = (bis2 ? -c_coefficient : 1 - ((2 * c_coefficient) / (2 - b)));
            // betac2_factor is (2 - (beta c)) if b!=2, -c if b=2
            double betac2_factor = (bis2 ? -c_coefficient : 2 - ((2 * c_coefficient) / (2 - b)));
            // beta_betac1_factor is -beta / (1 - (beta c)) if b!=2, 1 / c if b=2
            double beta_betac1_factor = (bis2 ? 1 / c_coefficient : -2 / (2 - b - (2 * c_coefficient)));
            // beta_betac2_factor is -beta / (2 - (beta c)) if b!=2, 1 / c if b=2
            double beta_betac2_factor = (bis2 ? 1 / c_coefficient : -2 / (4 - (2 * b) - (2 * c_coefficient)));
            // beta2_betac1betac2_factor is beta^2 / (1 - (beta c)(2 - (beta c)) if b!=2, 1 / c^2 if b=2
            double beta2_betac1betac2_factor = (bis2 ? 1 / Math.Pow(c_coefficient, 2) : Math.Pow(beta, 2) / (betac1_factor * betac2_factor));
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb1_factor = (bis2 ? h / 2 : Math.Pow(h / 2, b / 2));
            // hb2_factor is (h/2)^b, = (h/2)^2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb2_factor = (bis2 ? Math.Pow(h / 2, 2) : Math.Pow(h / 2, b));

            // Create local variables to calculate summation
            double tsK_a_MFP30_value = 0;
            double tsK_s_MFP30_increment = 0;
            double tsK_sII_MFP30_increment = 0;
            double tsK_sIJ_MFP30_increment = 0;
            double tsK_a_MFP32_value = 0;
            double tsK_s_MFP32_increment = 0;
            double tsK_a_MFP30_residual = 0;

            switch (CurrentFractureData.EvolutionStage)
            {
                case FractureEvolutionStage.NotActivated:
                    // No fracture growth in this timestep; however we will fall through to the "Growing" stage to maintain any initial zero-length macrofractures
                case FractureEvolutionStage.Growing:
                    // Use equations for growing fracture sets to update cumulative fracture population data
                    {
                        // Set the timestep K to the current timestep N
                        int tsK = tsN;
                        {
                            // Cache data for timestep K locally
                            // Timestep K duration
                            double tsK_Duration = PreviousFractureData.getDuration(tsK);
                            // Mean driving stress
                            double mean_SigmaD_K = PreviousFractureData.getMeanDrivingStressSigmaD(tsK);
                            // Mean macrofracture propagation rate (=alpha_MF * current driving stress to the power of b)
                            double MeanMFPropagationRate_K = PreviousFractureData.getMeanMFPropagationRate(tsK);
                            // Current mean probabilities of macrofracture deactivation
                            double mean_F_K = PreviousFractureData.getMeanF(tsK);
                            double mean_FII_K = PreviousFractureData.getMeanFII(tsK);
                            double mean_FIJ_K = PreviousFractureData.getMeanFIJ(tsK);

                            // Declare iterator for looping through the previous timesteps
                            int tsM;

                            // Calculate terms for timestep M=0
                            for (tsM = 0; tsM < 1; tsM++)
                            {
                                // Get index for the K,M element of the half-ts_PropLength and cumulative half-macrofracture deactivation probability arrays
                                int K_M_index = tsK;

                                // Cache useful variables locally
                                double ts_halfLength_K_M = PreviousFractureData.getCumulativeHalfLength(tsK, tsM);
                                double ts_IPlus_Phi_K_M = PreviousFractureData.getCumulativePhi(tsK, tsM);

                                // Calculate terms for a_MFP32_values
                                double tsM_a_MFP32_increment = ts_IPlus_Phi_K_M * h * Math.Pow(max_uF_radius, -c_coefficient) * ts_halfLength_K_M;
                                tsK_a_MFP32_value += tsM_a_MFP32_increment;
                            }

                            // Loop through all timesteps M between timestep 1 and timestep K-1 and calculate increments
                            for (; tsM < tsK; tsM++)
                            {
                                // Cache useful variables locally
                                double ts_halfLength_K_M = PreviousFractureData.getCumulativeHalfLength(tsK, tsM);
                                double ts_halfLength_K_Mminus1 = PreviousFractureData.getCumulativeHalfLength(tsK, tsM - 1);
                                double ts_halfLength_Kminus1_M = PreviousFractureData.getCumulativeHalfLength(tsK - 1, tsM);
                                //double ts_halfLength_Kminus1_Mminus1 = PreviousFractureData.getCumulativeHalfLength(tsK - 1, tsM - 1);
                                double ts_theta_dashed_Mminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsM - 1);
                                double ts_theta_dashed_M = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsM);
                                double ts_Phi_K_M = PreviousFractureData.getCumulativePhi(tsK, tsM);
                                double ts_Phi_K_Mminus1 = PreviousFractureData.getCumulativePhi(tsK, tsM - 1);
                                double ts_Phi_Kminus1_M = PreviousFractureData.getCumulativePhi(tsK - 1, tsM);
                                double ts_Phi_Kminus1_Mminus1 = PreviousFractureData.getCumulativePhi(tsK - 1, tsM - 1);

                                // Calculate useful components
                                double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsM);
                                double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                                double ts_CumhGammaM_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac1_factor));
                                //double ts_CumhGammaM_betac2_factor = (bis2 ? Math.Exp(betac2_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac2_factor));
                                double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsM - 1);
                                double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                                double ts_CumhGammaMminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac1_factor));
                                //double ts_CumhGammaMminus1_betac2_factor = (bis2 ? Math.Exp(betac2_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac2_factor));
                                double ts_PhiTheta_K_Mminus1 = (ts_Phi_K_Mminus1 * ts_theta_dashed_Mminus1);
                                double ts_PhiTheta_Kminus1_Mminus1 = (ts_Phi_Kminus1_Mminus1 * ts_theta_dashed_Mminus1);
                                double ts_dPhiTheta_K_dM = ((ts_Phi_K_Mminus1 * ts_theta_dashed_Mminus1) - (ts_Phi_K_M * ts_theta_dashed_M));
                                double ts_dPhiTheta_Kminus1_dM = ((ts_Phi_Kminus1_Mminus1 * ts_theta_dashed_Mminus1) - (ts_Phi_Kminus1_M * ts_theta_dashed_M));

                                // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                                if (double.IsInfinity(ts_CumhGammaM))
                                    continue;

                                // Calculate a_MFP30 increments valid if Timestep M < K
                                double tsM_a_MFP30_increment = ts_dPhiTheta_K_dM * ts_CumhGammaM_betac_factor;
                                tsK_a_MFP30_value += tsM_a_MFP30_increment;

                                // Calculate s_MFP30 increments valid if Timestep M < K
                                double tsM_s_MFP30_increment = 0;
                                if ((float)MeanMFPropagationRate_K > 0f) // If the propagation rate is zero there will be no fracture deactivation
                                {
                                    tsM_s_MFP30_increment = ts_dPhiTheta_Kminus1_dM * ts_CumhGammaM_betac_factor;
                                }
                                tsK_s_MFP30_increment += tsM_s_MFP30_increment;

                                // Calculate a_MFP32 increments valid if Timestep M < K
                                double tsM_a_MFP32_increment = ts_PhiTheta_K_Mminus1 * h * ((beta_betac1_factor * hb1_factor * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor))
                                    + ((ts_CumhGammaM_betac_factor * ts_halfLength_K_M) - (ts_CumhGammaMminus1_betac_factor * ts_halfLength_K_Mminus1)));
                                tsK_a_MFP32_value += tsM_a_MFP32_increment;

                                // Calculate s_MFP32 increments valid if Timestep M < K
                                double tsM_s_MFP32_factor0 = 0;
                                double tsM_s_MFP32_factor1 = 0;
                                if ((float)MeanMFPropagationRate_K > 0f) // If the propagation rate is zero there will be no fracture deactivation
                                {
                                    tsM_s_MFP32_factor0 = (ts_dPhiTheta_Kminus1_dM * (h / 2) * ts_CumhGammaM_betac_factor) / MeanMFPropagationRate_K;
                                    tsM_s_MFP32_factor1 = (ts_PhiTheta_Kminus1_Mminus1 * beta_betac1_factor * h * hb1_factor * tsK_Duration);
                                }
                                double tsM_s_MFP32_increment = (tsM_s_MFP32_factor0 * (Math.Pow(ts_halfLength_K_M, 2) - Math.Pow(ts_halfLength_Kminus1_M, 2)))
                                    + (tsM_s_MFP32_factor1 * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor));
                                tsK_s_MFP32_increment += tsM_s_MFP32_increment;

                            } // End loop through all timesteps M between timestep 1 and timestep K-1

                            // Apply general multipliers to increment and value terms up until current timestep
                            tsK_s_MFP30_increment *= tsK_Duration;

                            // Final timestep M=K=N
                            for (; tsM == tsK; tsM++)
                            {
                                // Cache useful variables locally
                                double ts_halfLength_K_M = PreviousFractureData.getCumulativeHalfLength(tsK, tsM);
                                double ts_halfLength_K_Mminus1 = PreviousFractureData.getCumulativeHalfLength(tsK, tsM - 1);
                                double ts_theta_dashed_Mminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsM - 1);
                                double ts_Phi_K_Mminus1 = PreviousFractureData.getCumulativePhi(tsK, tsM - 1);
                                // For use in calculating residual fracture population
                                double ts_gamma_InvBeta = PreviousFractureData.getuFPropagationRateFactor(tsK);
                                double inst_F_K = PreviousFractureData.getInstantaneousF(tsK);
                                double ts_equilibriation_factor = 1 - Math.Exp(-inst_F_K * CurrentFractureData.M_Duration);

                                // Calculate useful components
                                double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsM);
                                double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                                double ts_CumhGammaM_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac1_factor));
                                double ts_CumhGammaM_betac2_factor = (bis2 ? Math.Exp(betac2_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac2_factor));
                                double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsM - 1);
                                double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                                double ts_CumhGammaMminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac1_factor));
                                double ts_CumhGammaMminus1_betac2_factor = (bis2 ? Math.Exp(betac2_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac2_factor));
                                double ts_PhiTheta_K_Mminus1 = (ts_Phi_K_Mminus1 * ts_theta_dashed_Mminus1);
                                // For use in calculating residual fracture population
                                double ts_CumhGammaM_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betacminus1_factor));

                                // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                                if (double.IsInfinity(ts_CumhGammaM))
                                    continue;

                                // Calculate a_MFP30 increments valid if Timestep M = K
                                double tsM_a_MFP30_increment = ts_PhiTheta_K_Mminus1 * ts_CumhGammaM_betac_factor;
                                tsK_a_MFP30_value += tsM_a_MFP30_increment;

                                // Calculate s_MFP30 increments valid if Timestep M = K
                                double tsM_s_MFP30_factor1 = 0;
                                if ((float)MeanMFPropagationRate_K > 0f) // If the propagation rate is zero there will be no fracture deactivation
                                {
                                    tsM_s_MFP30_factor1 = (ts_theta_dashed_Mminus1 * beta_betac1_factor * hb1_factor) / MeanMFPropagationRate_K;
                                }
                                double tsM_s_MFP30_increment = tsM_s_MFP30_factor1 * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor);
                                tsK_s_MFP30_increment += tsM_s_MFP30_increment;

                                // Calculate a_MFP32 increments valid if Timestep M = K
                                double tsM_a_MFP32_increment = ts_PhiTheta_K_Mminus1 * h * ((beta_betac1_factor * hb1_factor * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor))
                                    + ((ts_CumhGammaM_betac_factor * ts_halfLength_K_M) - (ts_CumhGammaMminus1_betac_factor * ts_halfLength_K_Mminus1)));
                                tsK_a_MFP32_value += tsM_a_MFP32_increment;

                                // Calculate s_MFP32 increments valid if Timestep M = K
                                double tsM_s_MFP32_factor1 = 0;
                                double tsM_s_MFP32_factor2 = 0;
                                if ((float)MeanMFPropagationRate_K > 0f) // If the propagation rate is zero there will be no fracture deactivation
                                {
                                    tsM_s_MFP32_factor1 = (ts_theta_dashed_Mminus1 * beta_betac1_factor * h * hb1_factor * tsK_Duration);
                                    tsM_s_MFP32_factor2 = (ts_theta_dashed_Mminus1 * beta2_betac1betac2_factor * hb2_factor * h) / MeanMFPropagationRate_K;
                                }
                                double tsM_s_MFP32_increment = (tsM_s_MFP32_factor2 * (ts_CumhGammaM_betac2_factor - ts_CumhGammaMminus1_betac2_factor))
                                    - (tsM_s_MFP32_factor1 * ts_CumhGammaMminus1_betac1_factor);
                                tsK_s_MFP32_increment += tsM_s_MFP32_increment;

                                // Also calculate a_MFP30 for a residual active macrofracture population - we will compare this with the a_MFP30 calculated for a growing fracture population
                                // We include the timestep equilibriation factor to exclude early timesteps where the projected residual fracture population is high, but the equilbrium has not yet been reached so the actual population is much lower
                                tsK_a_MFP30_residual = (inst_F_K > 0 ? ts_equilibriation_factor * ((Math.Abs(betac_factor) * ts_gamma_InvBeta) / inst_F_K) * ts_theta_dashed_Mminus1 * ts_CumhGammaM_betacminus1_factor : 0);

                            } // End final timestep M=K=N

                            // Apply deactivation probability multipliers to s_MFP30 and s_MFP32 increments
                            tsK_sII_MFP30_increment = tsK_s_MFP30_increment * mean_FII_K;
                            tsK_sIJ_MFP30_increment = tsK_s_MFP30_increment * mean_FIJ_K;
                            tsK_s_MFP32_increment *= mean_F_K;

                        } // End calculate the static data increments and active data values for timestep K

                        // Compare a_MFP30 for a residual active macrofracture population with a_MFP30 calculated for a growing fracture population
                        // If the residual active macrofracture population is greater, we will reset the fracture evolution stage to ResidualActivity and update cumulative fracture population data using equations for residual fracture sets
                        if (tsK_a_MFP30_residual > tsK_a_MFP30_value)
                        {
                            // Set the fracture evolutionary stage to ResidualActivity
                            CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.ResidualActivity);

                            // Go to the FractureEvolutionStage.ResidualActivity case to update cumulative fracture population data using equations for residual fracture sets
                            goto case FractureEvolutionStage.ResidualActivity;
                        }

                    } // End case FractureEvolutionStage.Growing
                    break;
                case FractureEvolutionStage.ResidualActivity:
                    // Use equations for residual fracture sets to update cumulative fracture population data
                    {
                        // Set the timestep K to the current timestep N
                        int tsK = tsN;
                        {
                            // Cache data for timestep K locally
                            // Timestep K duration
                            double tsK_Duration = PreviousFractureData.getDuration(tsK);
                            // Mean macrofracture propagation rate (=alpha_MF * current driving stress to the power of b)
                            double MeanMFPropagationRate_K = PreviousFractureData.getMeanMFPropagationRate(tsK);
                            // Clear zone volume
                            double theta_dashed_Kminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsK - 1);
                            // Microfracture propagation rate coefficient (gamma ^ 1/beta) 
                            double gamma_InvBeta_K = PreviousFractureData.getuFPropagationRateFactor(tsK);
                            // Rate of growth of exclusion zone of this dipset relative to MFP32 of this dipset
                            double dChi_dMFP32_K = getdChi_dMFP32_M(tsK);
                            // Rate of growth of exclusion zone of this dipset relative to rate of total exclusion zone growth
                            double dChiMP_dChiTot_K = fs.get_dChiMP_dChiTot(this, tsK);

                            // Current instantaneous probabilities of macrofracture deactivation
                            double inst_F_K = PreviousFractureData.getInstantaneousF(tsK);
                            double inst_FII_K = PreviousFractureData.getInstantaneousFII(tsK);
                            double inst_FIJ_K = PreviousFractureData.getInstantaneousFIJ(tsK);

                            // If the instantaneous macrofracture deactivation probability is infinite, all macrofractures will be deactivated instantly
                            // In this case we can reset the fracture evolution stage to Deactivated and go to the FractureEvolutionStage.Deactivated case
                            if (double.IsInfinity(inst_F_K))
                            {
                                // Set the fracture evolutionary stage to Deactivated
                                CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);

                                // Go to the FractureEvolutionStage.Deactivated case - there will be no change to the cumulative fracture population data
                                goto case FractureEvolutionStage.Deactivated;
                            }

                            // Calculate mean residual fracture length
                            // In the evenly distributed stress scenario, the fractures in the other sets are randomly distributed, and we can use a quick version of the formula that assumes a constant instantaneous deactivation probability
                            // If there are stress shadows, the fractures in the other sets have a semi-regular distribution
                            // In this case we must use an approximate formula that takes the weighted mean stress shadow width of all dip sets - this should give a good approximation if most fractures are in one dip set
                            bool useQuickFormula = (gbc.PropControl.StressDistributionCase == StressDistribution.EvenlyDistributedStress);
                            double ts_MeanMFLength = fs.Calculate_MeanPropagationDistance(this, tsK, new List<double>() { 0 }, useQuickFormula)[0];

                            // Calculate useful components
                            double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsK);
                            double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                            double ts_CumhGammaM_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betacminus1_factor));
                            double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsK - 1);
                            double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                            double ts_CumhGammaMminus1_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betacminus1_factor));
                            double ts_StressShadowDecreaseFactor_denominator = inst_F_K * dChiMP_dChiTot_K;
                            double ts_StressShadowDecreaseFactor = (ts_StressShadowDecreaseFactor_denominator > 0 ? Math.Exp(-2 * CapB * Math.Abs(betac_factor) * gamma_InvBeta_K * ts_CumhGammaMminus1_betacminus1_factor * MeanMFPropagationRate_K * h * dChi_dMFP32_K * tsK_Duration / ts_StressShadowDecreaseFactor_denominator) : 0);

                            // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                            if (double.IsInfinity(ts_CumhGammaM))
                                break;

                            // Calculate a_MFP30 value for Timestep K
                            // If the fracture is not propagating then both gamma_InvBeta_K and inst_F_K will be zero, giving a NaN when calculating the residual a_MFP30 value
                            // In this case we will set tsK_a_MFP30_value to 0
                            tsK_a_MFP30_value = (inst_F_K > 0 ? ((Math.Abs(betac_factor) * gamma_InvBeta_K) / inst_F_K) * theta_dashed_Kminus1 * ts_CumhGammaM_betacminus1_factor * ts_StressShadowDecreaseFactor : 0);

                            // Calculate s_MFP30 increments for Timestep K
                            // First we must determine whether fracture growth is limited by decrease in nucleation rate or stress shadow growth
                            // We will calculate the increments for both end members and take the smallest
                            double tsK_s_MFP30_increment_NucleationLimited = theta_dashed_Kminus1 * (ts_CumhGammaM_betac_factor - ts_CumhGammaMminus1_betac_factor);
                            // If there are no stress shadows (s_MFP30_increment_StressShadowLimited_denominator is zero) set the stress shadow limited s_MFP30 increment to be higher than the nucleation rate limited increment, so it will not be used 
                            double s_MFP30_increment_StressShadowLimited_denominator = MeanMFPropagationRate_K * h * dChi_dMFP32_K;
                            double tsK_s_MFP30_increment_StressShadowLimited = (s_MFP30_increment_StressShadowLimited_denominator == 0 ? tsK_s_MFP30_increment_NucleationLimited + 1 : (1 / (2 * CapB)) * ((theta_dashed_Kminus1 * inst_F_K * dChiMP_dChiTot_K) / s_MFP30_increment_StressShadowLimited_denominator) * (1 - ts_StressShadowDecreaseFactor));
                            tsK_s_MFP30_increment = Math.Min(tsK_s_MFP30_increment_NucleationLimited, tsK_s_MFP30_increment_StressShadowLimited);
                            // NB If the fracture is not propagating then inst_FII_K, inst_FIJ_K and inst_F_K will all be zero, giving a NaN when calculating the sII_MFP30 and sIJ_MFP30 increments
                            // In this case we will set both tsK_sII_MFP30_increment and tsK_sIJ_MFP30_increment to 0
                            if (inst_F_K > 0)
                            {
                                tsK_sII_MFP30_increment = tsK_s_MFP30_increment * (inst_FII_K / inst_F_K);
                                tsK_sIJ_MFP30_increment = tsK_s_MFP30_increment * (inst_FIJ_K / inst_F_K);
                            }
                            else
                            {
                                tsK_sII_MFP30_increment = 0;
                                tsK_sIJ_MFP30_increment = 0;
                            }

                            // Calculate a_MFP32 value for Timestep K
                            tsK_a_MFP32_value = tsK_a_MFP30_value * h * ts_MeanMFLength;

                            // Calculate s_MFP32 increment for Timestep K
                            tsK_s_MFP32_increment = tsK_s_MFP30_increment * h * ts_MeanMFLength;

                        } // End calculate the static data increments and active data values for timestep K

                    } // End case FractureEvolutionStage.ResidualActivity
                    break;
                case FractureEvolutionStage.Deactivated:
                    // Active fracture fracture populations are zero and there is no increment to static fracture populations, so we do not need to do anything
                    break;
                default:
                    break;
            }

            // Apply general multipliers to all values
            tsK_a_MFP30_value *= CapB;
            tsK_sII_MFP30_increment *= CapB;
            tsK_sIJ_MFP30_increment *= CapB;
            tsK_a_MFP32_value *= CapB;
            tsK_s_MFP32_increment *= CapB;

            // Check if these increments leave us with a stress shadow volume greater than 1
            // Calculate the maximum volume available to accommodate stress shadows from this dipset (i.e. the volume not currently occupied by stress shadows from other dipsets)
            double maxAvailableStressShadowVolume = 1 - (fs.combined_MF_StressShadowVolume() - Total_MF_StressShadowVolume());
            // Calculate the new stress shadow volume, including the increments (this will be zero if there are no stress shadows)
            double newStressShadowVolume = ((2 * tsK_a_MFP32_value) + (IPlus_halfMacroFractures.s_P32_total + tsK_s_MFP32_increment) + (IMinus_halfMacroFractures.s_P32_total + tsK_s_MFP32_increment)) * MeanW;
            if (newStressShadowVolume > maxAvailableStressShadowVolume) // If the increments do leave us with a stress shadow volume greater than 1 then roll them back and deactivate the fracture set
            {
                // Set active populations to zero
                IPlus_halfMacroFractures.a_P30_total = 0;
                IPlus_halfMacroFractures.a_P32_total = 0;
                IMinus_halfMacroFractures.a_P30_total = 0;
                IMinus_halfMacroFractures.a_P32_total = 0;

                // Set static macrofracture P32 values so that total stress shadow volume will be 1
                double s_MFP32_required = maxAvailableStressShadowVolume / MeanW;
                IPlus_halfMacroFractures.s_P32_total = s_MFP32_required / 2;
                IMinus_halfMacroFractures.s_P32_total = s_MFP32_required / 2;

                // Set the fracture evolutionary stage to Deactivated
                // This will also set the propagation-related variables for microfractures and macrofractures to zero for this timestep
                // This will prevent any growth in the populations of implicit microfractures (or macrofractures in future timesteps), and also prevent nucleation and growth of explicit fractures in the DFN
                // This will also update the last item in the PreviousFractureData list, as it points to the same object
                CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
            }
            else // Otherwise update the fracture dip set data with calculated values and increments
            {
                // Number and area of active half-macrofractures cannot be less than zero
                // Therefore the active half-macrofracture values a_MFP30 and a_MFP32 can never be negative; if they are set them to zero
                if (tsK_a_MFP30_value < 0)
                    tsK_a_MFP30_value = 0;
                if (tsK_a_MFP32_value < 0)
                    tsK_a_MFP32_value = 0;

                // Number and area of static half-macrofractures cannot decrease 
                // Therefore the static half-macrofracture increments s_MFP30 and s_MFP32 can never be negative; if they are set them to zero
                if (tsK_sII_MFP30_increment < 0)
                    tsK_sII_MFP30_increment = 0;
                if (tsK_sIJ_MFP30_increment < 0)
                    tsK_sIJ_MFP30_increment = 0;
                if (tsK_s_MFP32_increment < 0)
                    tsK_s_MFP32_increment = 0;

                // Update the values for the rate of growth of half-macrofractures
                da_MFP30 = ((2 * tsK_a_MFP30_value) - (IPlus_halfMacroFractures.a_P30_total + IMinus_halfMacroFractures.a_P30_total));
                dsII_MFP30 = (2 * tsK_sII_MFP30_increment);
                dsIJ_MFP30 = (2 * tsK_sIJ_MFP30_increment);
                da_MFP32 = ((2 * tsK_a_MFP32_value) - (IPlus_halfMacroFractures.a_P32_total + IMinus_halfMacroFractures.a_P32_total));
                ds_MFP32 = (2 * tsK_s_MFP32_increment);

                // Total half-macrofracture area cannot decrease 
                // Therefore if the rate of growth of active MFP32 is negative, then the increase in static MFP32 must be greater than the decrease in active MFP32
                if (ds_MFP32 < -da_MFP32)
                {
                    ds_MFP32 = -da_MFP32;
                    tsK_s_MFP32_increment = ds_MFP32 / 2;
                }

                // Update values for total half-macrofracture population data for this fracture dip set
                IPlus_halfMacroFractures.a_P30_total = tsK_a_MFP30_value;
                IPlus_halfMacroFractures.sII_P30_total += tsK_sII_MFP30_increment;
                IPlus_halfMacroFractures.sIJ_P30_total += tsK_sIJ_MFP30_increment;
                IPlus_halfMacroFractures.a_P32_total = tsK_a_MFP32_value;
                IPlus_halfMacroFractures.s_P32_total += tsK_s_MFP32_increment;
                IMinus_halfMacroFractures.a_P30_total = tsK_a_MFP30_value;
                IMinus_halfMacroFractures.sII_P30_total += tsK_sII_MFP30_increment;
                IMinus_halfMacroFractures.sIJ_P30_total += tsK_sIJ_MFP30_increment;
                IMinus_halfMacroFractures.a_P32_total = tsK_a_MFP32_value;
                IMinus_halfMacroFractures.s_P32_total += tsK_s_MFP32_increment;
            }
        }
        /// <summary>
        /// Calculate the increment in total active and static microfracture density (a_uFP30, s_uFP30, a_uFP32, s_uFP32, a_uFP33 and s_uFP33) during the current timestep, in response to the applied driving stress and taking into account fracture deactivation
        /// </summary>
        /// <param name="no_r_bins">Number of microfracture radius bins used to integrate uFP32 and uFP33 numerically</param>
        public void calculateTotalMicrofracturePopulation(int no_r_bins)
        {
            // Cache constants locally
            //double h = gbc.ThicknessAtDeformation;
            double max_uF_radius = gbc.MaximumMicrofractureRadius;
            double b = gbc.MechProps.b_factor;
            double beta = gbc.MechProps.beta;
            bType b_type = gbc.MechProps.GetbType();
            bool bis2 = (b_type == bType.Equals2);
            int tsN = PreviousFractureData.NoTimesteps;
            double rmin_cutoff = gbc.PropControl.minImplicitMicrofractureRadius;

            // Calculate local helper variables
            // betac factor is -beta*c if b<>2, -c if b=2
            double betac_factor = (b == 2 ? -c_coefficient : -(beta * c_coefficient));
            // betac1 factor is (1 - (beta c)) if b!=2, -c if b=2
            double betac1_factor = (b == 2 ? -c_coefficient : 1 - ((2 * c_coefficient) / (2 - b)));
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            //double hb1_factor = (b_type == bType.Equals2 ? half_h : Math.Pow(half_h, b / 2));
            // hb2_factor is (h/2)^b, = (h/2)^2 if b=2
            //double hb2_factor = (bis2 ? Math.Pow(half_h, 2) : Math.Pow(half_h, b));
            // hc_factor is (h/2)^-c
            //double hc_factor = Math.Pow(half_h, -c_coefficient);
            // h2c_factor is (h/2)^(2-c) when c!=2, ln(h/2) when c=2
            double h2c_factor = (c_coefficient == 2 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 2 - c_coefficient));
            // h3c_factor is (h/2)^(3-c) when c!=3, ln(h/2) when c=3
            double h3c_factor = (c_coefficient == 3 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 3 - c_coefficient));
            // Calculate multipliers for the P32 and P33 values
            double uFP32_multiplier = CapB * Math.PI;
            double uFP33_multiplier = CapB * (4 / 3) * Math.PI;
            double b2_uFP32_factor = (c_coefficient == 2 ? 1 : (2 - c_coefficient));
            double b2_uFP33_factor = (c_coefficient == 3 ? 1 : (3 - c_coefficient));

            // Cache current timestep duration
            double tsN_Duration = PreviousFractureData.getDuration(tsN);

            // Cache current mean probability of microfracture deactivation
            double mean_qiI_N = PreviousFractureData.getMean_qiI(tsN);

            // Cache useful variables locally
            double ts_gamma_InvBeta_N = PreviousFractureData.getuFPropagationRateFactor(tsN);
            double ts_CumGammaN = PreviousFractureData.getCum_Gamma_M(tsN);
            double ts_CumGammaNminus1 = PreviousFractureData.getCum_Gamma_M(tsN - 1);
            double ts_theta_N = PreviousFractureData.getCumulativeTheta_AllFS_M(tsN);
            double ts_theta_Nminus1 = PreviousFractureData.getCumulativeTheta_AllFS_M(tsN - 1);
            // These variables are used for the terms representing transition deactivation microfractures
            double ts_CumhGammaN = PreviousFractureData.getCum_hGamma_M(tsN);
            double ts_CumhGammaNminus1 = PreviousFractureData.getCum_hGamma_M(tsN - 1);
            double ts_CumhGammaN_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaN) : Math.Pow(ts_CumhGammaN, betac_factor));
            double ts_CumhGammaNminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaNminus1) : Math.Pow(ts_CumhGammaNminus1, betac_factor));
            double ts_CumhGammaN_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaN) : Math.Pow(ts_CumhGammaN, betac1_factor));
            double ts_CumhGammaNminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaNminus1) : Math.Pow(ts_CumhGammaNminus1, betac1_factor));
            double ts_theta_dashed_Nminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsN - 1);
            // If driving stress is zero (i.e. ts_gamma_InvBeta_M is zero) there will be no fracture propagation and hence no fracture deactivation
            double ts_suF_deactivation_multiplier = (ts_gamma_InvBeta_N > 0 ? (mean_qiI_N * ts_theta_Nminus1) / ts_gamma_InvBeta_N : 0);

            // Create local variables to calculate summation
            double a_uFP30_value = 0;
            double s_uFP30_increment = 0;
            double a_uFP32_value = 0;
            double s_uFP32_increment = 0;
            double a_uFP33_value = 0;
            double s_uFP33_increment = 0;

            // If the rmin cutoff is greater than the maximum microfracture radius, all terms will be zero
            if (rmin_cutoff < max_uF_radius)
            {
                // Equations are different for b<2, b=2 and b>2
                switch (b_type)
                {
                    case bType.LessThan2:
                        {
                            // a_uF_P_30 and s_uF_P_30 terms can be calculated directly
                            // However we cannot calculate a_uF_P_30 or s_uF_P_30 if the rmin cutoff is zero, as they will be infinite 
                            if (rmin_cutoff > 0)
                            {
                                // Calculate useful components
                                double ts_rminCumGammaN_factor = Math.Pow(rmin_cutoff, 1 / beta) + ts_CumGammaN;
                                double ts_rminCumGammaNminus1_factor = Math.Pow(rmin_cutoff, 1 / beta) + ts_CumGammaNminus1;
                                double ts_rminCumGammaN_betac_factor = Math.Pow(ts_rminCumGammaN_factor, betac_factor);
                                double ts_rminCumGammaN_betac1_factor = Math.Pow(ts_rminCumGammaN_factor, betac1_factor);
                                double ts_rminCumGammaNminus1_betac1_factor = Math.Pow(ts_rminCumGammaNminus1_factor, betac1_factor);

                                // Do not calculate a value if the extrapolated initial minimum radius is zero or less
                                if (ts_rminCumGammaN_factor > 0)
                                {
                                    // Calculate a_uFP30 value for rmin_cutoff
                                    a_uFP30_value = (ts_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);

                                    // Calculate s_uFP30 increment for rmin_cutoff
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    s_uFP30_increment +=
                                        (ts_CumhGammaN_betac1_factor - ts_CumhGammaNminus1_betac1_factor - ts_rminCumGammaN_betac1_factor + ts_rminCumGammaNminus1_betac1_factor);
                                }
                            }

                            // a_uFP32_value, s_uFP32_increment, a_uFP33_value and s_uFP33_increment terms must be calculated numerically by iterating through a range of r-values
                            // The s_uFP32 and s_uFP33 increments calculated in this iteration represent growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                            double rb_minRad = 0;
                            double rb_maxRad = 0;
                            for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                            {
                                // Calculate range of radii sizes in the current r-bin
                                rb_minRad = rb_maxRad;
                                rb_maxRad = ((double)(r_bin + 1) / (double)no_r_bins) * max_uF_radius;

                                // If the maximum bin size is less than the minimum cutoff, go straight on to the next bin
                                if (rb_maxRad < rmin_cutoff) continue;
                                // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                                if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;
                                // If the minimum bin size is zero, go straight on to the next bin
                                if (rb_minRad <= 0) continue;

                                // Calculate useful components
                                double rb_rminCumGammaN_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaN;
                                double rb_rmaxCumGammaN_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaN;
                                double rb_rminCumGammaNminus1_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaNminus1;
                                double rb_rmaxCumGammaNminus1_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaNminus1;
                                double rb_rminCumGammaN_betac_factor = Math.Pow(rb_rminCumGammaN_factor, betac_factor);
                                double rb_rmaxCumGammaN_betac_factor = Math.Pow(rb_rmaxCumGammaN_factor, betac_factor);
                                double rb_rminCumGammaN_betac1_factor = Math.Pow(rb_rminCumGammaN_factor, betac1_factor);
                                double rb_rmaxCumGammaN_betac1_factor = Math.Pow(rb_rmaxCumGammaN_factor, betac1_factor);
                                double rb_rminCumGammaNminus1_betac1_factor = Math.Pow(rb_rminCumGammaNminus1_factor, betac1_factor);
                                double rb_rmaxCumGammaNminus1_betac1_factor = Math.Pow(rb_rmaxCumGammaNminus1_factor, betac1_factor);

                                // Do not calculate a value if the extrapolated initial minimum radius is zero or less
                                if (rb_rminCumGammaN_factor > 0)
                                {
                                    // Calculate term for a_uFP32_value component
                                    a_uFP32_value += Math.Pow(rb_minRad, 2) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);

                                    // Calculate term for s_uFP32_increment component
                                    s_uFP32_increment += Math.Pow(rb_minRad, 2) *
                                        (rb_rminCumGammaNminus1_betac1_factor - rb_rminCumGammaN_betac1_factor - rb_rmaxCumGammaNminus1_betac1_factor + rb_rmaxCumGammaN_betac1_factor);

                                    // Calculate term for a_uFP33_value component
                                    a_uFP33_value += Math.Pow(rb_minRad, 3) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);

                                    // Calculate term for s_uFP33_increment component
                                    s_uFP33_increment += Math.Pow(rb_minRad, 3) *
                                        (rb_rminCumGammaNminus1_betac1_factor - rb_rminCumGammaN_betac1_factor - rb_rmaxCumGammaNminus1_betac1_factor + rb_rmaxCumGammaN_betac1_factor);
                                }
                            }

                            // Apply multipliers for microfracture deactivation rates for this timestep
                            a_uFP30_value *= ts_theta_N;
                            a_uFP32_value *= ts_theta_N;
                            a_uFP33_value *= ts_theta_N;
                            s_uFP30_increment *= (ts_suF_deactivation_multiplier / betac1_factor);
                            s_uFP32_increment *= (ts_suF_deactivation_multiplier / betac1_factor);
                            s_uFP33_increment *= (ts_suF_deactivation_multiplier / betac1_factor);
                        }
                        break;
                    case bType.Equals2:
                        {
                            // Calculate local helper variables
                            double ts_CumGammaN_betac_factor = Math.Exp(betac_factor * ts_CumGammaN);
                            double ts_CumGammaMminus1_betac_factor = Math.Exp(betac_factor * ts_CumGammaNminus1);

                            // Calculate terms analytically for whole fracture population
                            // When b=2 the extrapolated initial minimum radius is always greater than 0
                            {
                                // We cannot calculate the a_uFP30 and s_uFP30 terms if the rmin cutoff is zero, as they will be infinite 
                                if (rmin_cutoff > 0)
                                {
                                    // Calculate useful components
                                    double rb_minRad_betac_factor = Math.Pow(rmin_cutoff, betac_factor);
                                    double rb_rminCumGammaM_betac_factor = rb_minRad_betac_factor * ts_CumGammaN_betac_factor;
                                    double rb_rminCumGammaMminus1_betac_factor = rb_minRad_betac_factor * ts_CumGammaMminus1_betac_factor;

                                    // Calculate a_uFP30 value for rmin_cutoff
                                    a_uFP30_value = (rb_rminCumGammaM_betac_factor - ts_CumhGammaN_betac_factor);

                                    // Calculate s_uFP30 increment for rmin_cutoff
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    s_uFP30_increment +=
                                        (rb_rminCumGammaM_betac_factor - rb_rminCumGammaMminus1_betac_factor - ts_CumhGammaN_betac1_factor + ts_CumhGammaNminus1_betac1_factor);
                                }

                                // We cannot calculate the a_uFP32 and s_uFP32 terms if the rmin cutoff is zero and c>=2, as they will be infinite
                                if ((rmin_cutoff > 0) || (c_coefficient < 2))
                                {
                                    // Calculate useful components
                                    double rb_minRad_2c_factor = (c_coefficient == 2 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 2 - c_coefficient));

                                    // Calculate term for a_uFP32_value
                                    a_uFP32_value = ts_CumGammaN_betac_factor * (h2c_factor - rb_minRad_2c_factor);

                                    // Calculate term for s_uFP32_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    s_uFP32_increment = (ts_CumGammaN_betac_factor - ts_CumGammaMminus1_betac_factor) * (h2c_factor - rb_minRad_2c_factor);
                                }

                                // We cannot calculate the a_uFP33 and s_uFP33 terms if the rmin cutoff is zero and c>=3, as they will be infinite
                                if ((rmin_cutoff > 0) || (c_coefficient < 3))
                                {
                                    // Calculate useful components
                                    double rb_minRad_3c_factor = (c_coefficient == 3 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 3 - c_coefficient));

                                    // Calculate term for a_uFP33_value
                                    a_uFP33_value = ts_CumGammaN_betac_factor * (h3c_factor - rb_minRad_3c_factor);

                                    // Calculate term for s_uFP33_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    s_uFP33_increment = (ts_CumGammaN_betac_factor - ts_CumGammaMminus1_betac_factor) * (h3c_factor - rb_minRad_3c_factor);
                                }
                            }

                            // Apply multipliers for microfracture deactivation rates for this timestep
                            a_uFP30_value *= ts_theta_N;
                            a_uFP32_value *= ts_theta_N * (c_coefficient / b2_uFP32_factor);
                            a_uFP33_value *= ts_theta_N * (c_coefficient / b2_uFP33_factor);
                            s_uFP30_increment *= (ts_suF_deactivation_multiplier / c_coefficient);
                            s_uFP32_increment *= (ts_suF_deactivation_multiplier / b2_uFP32_factor);
                            s_uFP33_increment *= (ts_suF_deactivation_multiplier / b2_uFP33_factor);
                        }
                        break;
                    case bType.GreaterThan2:
                        {
                            // a_uF_P_30 and s_uF_P_30 terms can be calculated directly
                            // However we cannot calculate a_uF_P_30 or s_uF_P_30 if the rmin cutoff is zero, as they will be infinite 
                            if (rmin_cutoff > 0)
                            {
                                // Calculate useful components
                                double ts_rminCumGammaN_factor = Math.Pow(rmin_cutoff, 1 / beta) + ts_CumGammaN;
                                double ts_rminCumGammaNminus1_factor = Math.Pow(rmin_cutoff, 1 / beta) + ts_CumGammaNminus1;
                                double ts_rminCumGammaN_betac_factor = Math.Pow(ts_rminCumGammaN_factor, betac_factor);
                                double ts_rminCumGammaN_betac1_factor = Math.Pow(ts_rminCumGammaN_factor, betac1_factor);
                                double ts_rminCumGammaNminus1_betac1_factor = Math.Pow(ts_rminCumGammaNminus1_factor, betac1_factor);

                                // When b>2 the extrapolated initial minimum radius is always greater than 0
                                // However if b is very large, 1/beta will be large and negative, so ts_rminCumGammaN_factor and ts_rminCumGammaNminus1_factor may tend to infinity; in this case no uFP30 values will be calculated
                                if (ts_rminCumGammaN_factor < double.PositiveInfinity)
                                {
                                    // Calculate a_uFP30 value for rmin_cutoff
                                    a_uFP30_value = (ts_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);

                                    // Calculate s_uFP30 increment for rmin_cutoff
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    s_uFP30_increment +=
                                        (ts_rminCumGammaN_betac1_factor - ts_rminCumGammaNminus1_betac1_factor - ts_CumhGammaN_betac1_factor + ts_CumhGammaNminus1_betac1_factor);
                                }
                            }

                            // a_uFP32_value, s_uFP32_increment, a_uFP33_value and s_uFP33_increment terms must be calculated numerically by iterating through a range of r-values
                            // The s_uFP32 and s_uFP33 increments calculated in this iteration represent growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                            double rb_minRad = 0;
                            double rb_maxRad = 0;
                            for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                            {
                                // Calculate range of radii sizes in the current r-bin
                                rb_minRad = rb_maxRad;
                                rb_maxRad = ((double)(r_bin + 1) / (double)no_r_bins) * max_uF_radius;

                                // If the maximum bin size is less than the minimum cutoff, go straight on to the next bin
                                if (rb_maxRad < rmin_cutoff) continue;
                                // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                                if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;
                                // If the minimum bin size is zero, go straight on to the next bin
                                if (rb_minRad <= 0) continue;

                                // Calculate useful components
                                double rb_rminCumGammaN_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaN;
                                double rb_rmaxCumGammaN_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaN;
                                double rb_rminCumGammaNminus1_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaNminus1;
                                double rb_rmaxCumGammaNminus1_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaNminus1;
                                double rb_rminCumGammaN_betac_factor = Math.Pow(rb_rminCumGammaN_factor, betac_factor);
                                double rb_rmaxCumGammaN_betac_factor = Math.Pow(rb_rmaxCumGammaN_factor, betac_factor);
                                double rb_rminCumGammaN_betac1_factor = Math.Pow(rb_rminCumGammaN_factor, betac1_factor);
                                double rb_rmaxCumGammaN_betac1_factor = Math.Pow(rb_rmaxCumGammaN_factor, betac1_factor);
                                double rb_rminCumGammaNminus1_betac1_factor = Math.Pow(rb_rminCumGammaNminus1_factor, betac1_factor);
                                double rb_rmaxCumGammaNminus1_betac1_factor = Math.Pow(rb_rmaxCumGammaNminus1_factor, betac1_factor);

                                // When b>2 the extrapolated initial minimum radius is always greater than 0
                                // However if b is very large, 1/beta will be large and negative, so rb_rminCumGammaN_factor and rb_rmaxCumGammaN_factor may tend to infinity; in this case no uFP32 or uFP33 values will be calculated
                                if (rb_rminCumGammaN_factor < double.PositiveInfinity)
                                {
                                    // Calculate term for a_uFP32_value component
                                    a_uFP32_value += Math.Pow(rb_minRad, 2) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);

                                    // Calculate term for s_uFP32_increment component
                                    s_uFP32_increment += Math.Pow(rb_minRad, 2) *
                                        (rb_rminCumGammaN_betac1_factor - rb_rminCumGammaNminus1_betac1_factor - rb_rmaxCumGammaN_betac1_factor + rb_rmaxCumGammaNminus1_betac1_factor);

                                    // Calculate term for a_uFP33_value component
                                    a_uFP33_value += Math.Pow(rb_minRad, 3) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);

                                    // Calculate term for s_uFP33_increment component
                                    s_uFP33_increment += Math.Pow(rb_minRad, 3) *
                                        (rb_rminCumGammaN_betac1_factor - rb_rminCumGammaNminus1_betac1_factor - rb_rmaxCumGammaN_betac1_factor + rb_rmaxCumGammaNminus1_betac1_factor);
                                }
                            }

                            // Apply multipliers for microfracture deactivation rates for this timestep
                            a_uFP30_value *= ts_theta_N;
                            a_uFP32_value *= ts_theta_N;
                            a_uFP33_value *= ts_theta_N;
                            s_uFP30_increment *= (ts_suF_deactivation_multiplier / betac1_factor);
                            s_uFP32_increment *= (ts_suF_deactivation_multiplier / betac1_factor);
                            s_uFP33_increment *= (ts_suF_deactivation_multiplier / betac1_factor);
                        }
                        break;
                    default:
                        break;
                }

                // Add additional terms to s_uFP30_increment, s_uFP32_increment and s_uFP33_increment to account for transition deactivation microfractures
                double transition_deactivation_term = 0;
                if (ts_CumhGammaN < double.PositiveInfinity)
                    transition_deactivation_term = (ts_theta_Nminus1 - ts_theta_dashed_Nminus1) * (ts_CumhGammaN_betac_factor - ts_CumhGammaNminus1_betac_factor);
                s_uFP30_increment += transition_deactivation_term;
                s_uFP32_increment += (transition_deactivation_term * Math.Pow(max_uF_radius, 2));
                s_uFP33_increment += (transition_deactivation_term * Math.Pow(max_uF_radius, 3));

                // Area of static microfractures cannot decrease 
                // Therefore the static microfracture increments s_uFP30, s_uFP32 and s_uFP33 can never be negative; if they are set them to zero
                if (s_uFP30_increment < 0)
                    s_uFP30_increment = 0;
                if (s_uFP32_increment < 0)
                    s_uFP32_increment = 0;
                if (s_uFP33_increment < 0)
                    s_uFP33_increment = 0;

                // Apply general multipliers to increment and value terms
                a_uFP30_value *= CapB;
                a_uFP32_value *= uFP32_multiplier;
                a_uFP33_value *= uFP33_multiplier;
                s_uFP30_increment *= CapB;
                s_uFP32_increment *= uFP32_multiplier;
                s_uFP33_increment *= uFP33_multiplier;

            } // End if the rmin cutoff is greater than the maximum microfracture radius

            // Update values for total linear half-macrofracture population data for this fracture dip set
            MicroFractures.a_P30_total = a_uFP30_value;
            MicroFractures.s_P30_total += s_uFP30_increment;
            MicroFractures.a_P32_total = a_uFP32_value;
            MicroFractures.s_P32_total += s_uFP32_increment;
            MicroFractures.a_P33_total = a_uFP33_value;
            MicroFractures.s_P33_total += s_uFP33_increment;
        }
        /// <summary>
        /// Calculate the cumulative active and static half-macrofracture density distribution functions a_MFP30(l), sII_MFP30(l), sIJ_MFP30(l), a_MFP32(l) and s_MFP32(l) at the current time, for a specified list of lengths l
        /// </summary>
        public void calculateCumulativeMacrofracturePopulationArrays()
        {
            // Cache constants locally
            double h = gbc.ThicknessAtDeformation;
            double max_uF_radius = gbc.MaximumMicrofractureRadius;
            double b = gbc.MechProps.b_factor;
            double beta = gbc.MechProps.beta;
            bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);
            double CapA = gbc.MechProps.CapA;
            double Kc = gbc.MechProps.Kc;
            double SqrtPi = Math.Sqrt(Math.PI);
            int tsN = PreviousFractureData.NoTimesteps;

            // Calculate local helper variables
            // betac_factor is -beta*c if b<>2, -c if b=2
            double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
            // betacminus1_factor is 1-beta*c if b<>2, -c if b=2
            double betacminus1_factor = (bis2 ? -c_coefficient : -((2 * c_coefficient) / (2 - b)) - 1);
            // betac1_factor is (1 - (beta c)) if b!=2, -c if b=2
            double betac1_factor = (bis2 ? -c_coefficient : 1 - ((2 * c_coefficient) / (2 - b)));
            // betac2_factor is (2 - (beta c)) if b!=2, -c if b=2
            double betac2_factor = (bis2 ? -c_coefficient : 2 - ((2 * c_coefficient) / (2 - b)));
            // beta_betac1_factor is -beta / (1 - (beta c)) if b!=2, 1 / c if b=2
            double beta_betac1_factor = (bis2 ? 1 / c_coefficient : -2 / (2 - b - (2 * c_coefficient)));
            // beta_betac2_factor is -beta / (2 - (beta c)) if b!=2, 1 / c if b=2
            double beta_betac2_factor = (bis2 ? 1 / c_coefficient : -2 / (4 - (2 * b) - (2 * c_coefficient)));
            // beta2_betac1betac2_factor is beta^2 / (1 - (beta c)(2 - (beta c)) if b!=2, 1 / c^2 if b=2
            double beta2_betac1betac2_factor = (bis2 ? 1 / Math.Pow(c_coefficient, 2) : Math.Pow(beta, 2) / (betac1_factor * betac2_factor));
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb1_factor = (bis2 ? (h / 2) : Math.Pow(h / 2, b / 2));
            // hb2_factor is (h/2)^b, = (h/2)^2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb2_factor = (bis2 ? Math.Pow((h / 2), 2) : Math.Pow(h / 2, b));

            // Resort index array
            MF_halflengths.Sort();
#if DBLOG
            MF_halflengths[0] = 0;
            string fileName = string.Format("logFile_X{0}_Y{1}_Strike{2}_Dip{3}.txt", gbc.SWtop.X, gbc.SWtop.Y, (int)(fs.Strike * 180 / Math.PI), (int)(Dip * 180 / Math.PI));
            String namecomb = gbc.PropControl.FolderPath + fileName;
            StreamWriter logFile = new StreamWriter(namecomb);
            logFile.WriteLine("NewSet");
#endif
            int noIndexPoints = MF_halflengths.Count();

            // Also set up local arrays to store the cumulative macrofracture population values for each index value as they are calculated
            List<double> tsN_a_MFP30_values = new List<double>();
            List<double> tsN_sII_MFP30_values = new List<double>();
            List<double> tsN_sIJ_MFP30_values = new List<double>();
            List<double> tsN_a_MFP32_values = new List<double>();
            List<double> tsN_s_MFP32_values = new List<double>();

            // Initialise and populate local arrays
            for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
            {
                // Add zero values to the local cumulative macrofracture population data arrays
                tsN_a_MFP30_values.Add(0d);
                tsN_sII_MFP30_values.Add(0d);
                tsN_sIJ_MFP30_values.Add(0d);
                tsN_a_MFP32_values.Add(0d);
                tsN_s_MFP32_values.Add(0d);
            }

            // Loop through the previous timesteps K to calculate the static data increments
            // For the current timestep N also calculate the active data values
            for (int tsK = 1; tsK <= tsN; tsK++)
            {
                // Set flag to determine if this is the last timestep
                bool is_tsN = (tsK == tsN);

                // Set up local array of J-timestep values for each index half-length
                List<int> tsJ = new List<int>();

                // Set up arrays for the increments or values calculated for this timestep K
                List<double> tsK_a_MFP30_values = new List<double>();
                List<double> tsK_s_MFP30_increments = new List<double>();
                List<double> tsK_sII_MFP30_increments = new List<double>();
                List<double> tsK_sIJ_MFP30_increments = new List<double>();
                List<double> tsK_a_MFP32_values = new List<double>();
                List<double> tsK_s_MFP32_increments = new List<double>();

                // Initialise and populate local arrays
                int currentJTimestep = tsK;
                for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                {
                    // Calculate the J timesteps for each index value: this is the timestep in which a currently active fracture of the specified half-length must have nucleated
                    while ((currentJTimestep > 0) && (MF_halflengths[indexPoint] >= PreviousFractureData.getCumulativeHalfLength(tsK, currentJTimestep)))
                        currentJTimestep--;
                    tsJ.Add(currentJTimestep);

                    // Add zero values to the local cumulative macrofracture population data arrays
                    tsK_a_MFP30_values.Add(0d);
                    tsK_s_MFP30_increments.Add(0d);
                    tsK_sII_MFP30_increments.Add(0d);
                    tsK_sIJ_MFP30_increments.Add(0d);
                    tsK_a_MFP32_values.Add(0d);
                    tsK_s_MFP32_increments.Add(0d);
                }

                switch (PreviousFractureData.getEvolutionStage(tsK))
                {
                    case FractureEvolutionStage.NotActivated:
                    // No fracture growth in this timestep; however we will fall through to the "Growing" stage to maintain any initial zero-length macrofractures
                    case FractureEvolutionStage.Growing:
                        // Use equations for growing fracture sets to update cumulative fracture population arrays
                        {
                            // Cache data for timestep K locally
                            // Timestep K duration
                            double tsK_Duration = PreviousFractureData.getDuration(tsK);
                            // Mean driving stress
                            double mean_SigmaD_K = PreviousFractureData.getMeanDrivingStressSigmaD(tsK);
                            // Mean macrofracture propagation rate (=alpha_MF * current driving stress to the power of b)
                            double MeanMFPropagationRate_K = PreviousFractureData.getMeanMFPropagationRate(tsK);
                            // Current mean probabilities of macrofracture deactivation
                            double mean_F_K = PreviousFractureData.getMeanF(tsK);
                            double mean_FII_K = PreviousFractureData.getMeanFII(tsK);
                            double mean_FIJ_K = PreviousFractureData.getMeanFIJ(tsK);

                            // Declare iterator for looping through the previous timesteps
                            int tsM;

                            // Calculate terms for timestep M=0
                            for (tsM = 0; tsM < 1; tsM++)
                            {
                                // We must still calculate values for a_MFP30 and a_MFP32 even if timestep K!=N, in order to check the that the increments in s_MFP32 values do not exceed the decreases in a_MFP32 values
                                //if (is_tsN)
                                {
                                    // Cache useful variables locally
                                    double ts_halfLength_K_M = PreviousFractureData.getCumulativeHalfLength(tsK, tsM);
                                    double ts_Phi_K_M = PreviousFractureData.getCumulativePhi(tsK, tsM);

                                    // Calculate terms for a_MFP32_values
                                    double tsM_a_MFP32_M_increment = ts_Phi_K_M * h * Math.Pow(max_uF_radius, -c_coefficient) * ts_halfLength_K_M;

                                    for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                                    {
                                        // Add M=0 values to a_MFP32 value arrays
                                        if (MF_halflengths[indexPoint] <= ts_halfLength_K_M)
                                            tsK_a_MFP32_values[indexPoint] = tsM_a_MFP32_M_increment;
                                    }
                                }
                            }

                            // Loop through all timesteps M between timestep 1 and timestep K and calculate increments
                            for (; tsM <= tsK; tsM++)
                            {
                                // Cache useful variables locally
                                double ts_halfLength_K_M = PreviousFractureData.getCumulativeHalfLength(tsK, tsM);
                                double ts_halfLength_K_Mminus1 = PreviousFractureData.getCumulativeHalfLength(tsK, tsM - 1);
                                double ts_halfLength_Kminus1_M = PreviousFractureData.getCumulativeHalfLength(tsK - 1, tsM);
                                double ts_halfLength_Kminus1_Mminus1 = PreviousFractureData.getCumulativeHalfLength(tsK - 1, tsM - 1);
                                double ts_theta_dashed_Mminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsM - 1);
                                double ts_theta_dashed_M = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsM);
                                double ts_Phi_K_M = PreviousFractureData.getCumulativePhi(tsK, tsM);
                                double ts_Phi_K_Mminus1 = PreviousFractureData.getCumulativePhi(tsK, tsM - 1);
                                double ts_Phi_Kminus1_M = PreviousFractureData.getCumulativePhi(tsK - 1, tsM);
                                double ts_Phi_Kminus1_Mminus1 = PreviousFractureData.getCumulativePhi(tsK - 1, tsM - 1);

                                // Calculate useful components
                                double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsM);
                                double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                                double ts_CumhGammaM_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac1_factor));
                                double ts_CumhGammaM_betac2_factor = (bis2 ? Math.Exp(betac2_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac2_factor));
                                double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsM - 1);
                                double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                                double ts_CumhGammaMminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac1_factor));
                                double ts_CumhGammaMminus1_betac2_factor = (bis2 ? Math.Exp(betac2_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac2_factor));
                                double ts_PhiTheta_K_Mminus1 = (ts_Phi_K_Mminus1 * ts_theta_dashed_Mminus1);
                                double ts_PhiTheta_Kminus1_Mminus1 = (ts_Phi_Kminus1_Mminus1 * ts_theta_dashed_Mminus1);
                                double ts_dPhiTheta_K_dM = ((ts_Phi_K_Mminus1 * ts_theta_dashed_Mminus1) - (ts_Phi_K_M * ts_theta_dashed_M));
                                double ts_dPhiTheta_Kminus1_dM = ((ts_Phi_Kminus1_Mminus1 * ts_theta_dashed_Mminus1) - (ts_Phi_Kminus1_M * ts_theta_dashed_M));

#if DBLOG
                                if (is_tsN) logFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}", tsM, ts_theta_dashed_Mminus1, ts_theta_dashed_M, ts_Phi_Kminus1_Mminus1, ts_Phi_Kminus1_M, ts_Phi_K_Mminus1, ts_Phi_K_M, ts_PhiTheta_K_Mminus1, ts_PhiTheta_Kminus1_Mminus1, ts_dPhiTheta_K_dM, ts_dPhiTheta_Kminus1_dM));
#endif

                                // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                                if (double.IsInfinity(ts_CumhGammaM))
                                    continue;

                                // Calculate standard terms for s_MFP30_increments
                                double tsM_s_MFP30_factor0 = 0;
                                double tsM_s_MFP30_factor1 = 0;
                                if ((float)MeanMFPropagationRate_K > 0f) // If the propagation rate is zero there will be no fracture deactivation
                                {
                                    tsM_s_MFP30_factor0 = ts_dPhiTheta_Kminus1_dM * ts_CumhGammaM_betac_factor;
                                    tsM_s_MFP30_factor1 = (ts_PhiTheta_Kminus1_Mminus1 * beta_betac1_factor * hb1_factor) / MeanMFPropagationRate_K;
                                }

                                // Calculate standard terms for s_MFP32_increments
                                double tsM_s_MFP32_factor0 = 0;
                                double tsM_s_MFP32_factor1 = 0;
                                double tsM_s_MFP32_factor2 = 0;
                                if ((float)MeanMFPropagationRate_K > 0f) // If the propagation rate is zero there will be no fracture deactivation
                                {
                                    tsM_s_MFP32_factor0 = (ts_dPhiTheta_Kminus1_dM * (h / 2) * ts_CumhGammaM_betac_factor) / MeanMFPropagationRate_K;
                                    tsM_s_MFP32_factor1 = (ts_PhiTheta_Kminus1_Mminus1 * beta_betac1_factor * h * hb1_factor * tsK_Duration);
                                    tsM_s_MFP32_factor2 = (ts_PhiTheta_Kminus1_Mminus1 * beta2_betac1betac2_factor * hb2_factor * h) / MeanMFPropagationRate_K;
                                }

                                // Loop through all index points and populate the cumulative macrofracture population data arrays
                                for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                                {
                                    double length = MF_halflengths[indexPoint];

                                    // Calculate increments for IPlus half-macrofractures
                                    {
                                        // Calculate values for a_MFP30 and a_MFP32 for fractures nucleating in timestep M if timestep K=N
                                        // We must still calculate values for a_MFP30 and a_MFP32 even if timestep K!=N, in order to check the that the increments in s_MFP32 values do not exceed the decreases in a_MFP32 values
                                        //if (is_tsN)
                                        {
                                            if (tsM < tsJ[indexPoint]) // If timestep M is before the timestep in which macrofractures of half-length l nucleated then use the standard terms for a_MFP30_values if timestep K=N
                                            {
                                                // a_MFP30_M_increment valid if Timestep M < J
                                                double tsM_a_MFP30_M_increment = ts_dPhiTheta_K_dM * ts_CumhGammaM_betac_factor;
                                                tsK_a_MFP30_values[indexPoint] += tsM_a_MFP30_M_increment;

                                                // a_MFP32_M_increment valid if Timestep M < J
                                                double tsM_a_MFP32_M_increment = ts_PhiTheta_K_Mminus1 * h * ((beta_betac1_factor * hb1_factor * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor))
                                                    + ((ts_CumhGammaM_betac_factor * ts_halfLength_K_M) - (ts_CumhGammaMminus1_betac_factor * ts_halfLength_K_Mminus1)));
                                                tsK_a_MFP32_values[indexPoint] += tsM_a_MFP32_M_increment;
                                            }
                                            else if (tsM == tsJ[indexPoint]) // If timestep M is the timestep in which macrofractures of half-ts_PropLength l nucleated then calculate special terms with a cutoff at l
                                            {
                                                // Calculate CumhGammaJ factors with a cutoff at l
                                                double J_factor = (ts_halfLength_K_M - length) / (hb1_factor * beta);
                                                double CumhGammaJ = ts_CumhGammaM - J_factor;
                                                double ts_CumhGammaJ_betac_factor = (bis2 ? Math.Exp(betac_factor * CumhGammaJ) : Math.Pow(CumhGammaJ, betac_factor));
                                                double ts_CumhGammaJ_betac1_factor = (bis2 ? Math.Exp(betac1_factor * CumhGammaJ) : Math.Pow(CumhGammaJ, betac1_factor));

                                                // a_MFP30_J_increment valid if Timestep M = J
                                                double tsJ_a_MFP30_J_increment = ts_PhiTheta_K_Mminus1 * ts_CumhGammaJ_betac_factor;
                                                // a_MFP32_J_increment valid if Timestep M = J
                                                double tsJ_a_MFP32_J_increment = ts_PhiTheta_K_Mminus1 * h * ((beta_betac1_factor * hb1_factor * (ts_CumhGammaJ_betac1_factor - ts_CumhGammaMminus1_betac1_factor))
                                                    + ((ts_CumhGammaJ_betac_factor * length) - (ts_CumhGammaMminus1_betac_factor * ts_halfLength_K_Mminus1)));

                                                tsK_a_MFP30_values[indexPoint] += tsJ_a_MFP30_J_increment;
                                                tsK_a_MFP32_values[indexPoint] += tsJ_a_MFP32_J_increment;
                                            }
                                        }

                                        // Calculate values for s_MFP30 and s_MFP32 for fractures nucleating in timestep M, deactivating in timestep N
                                        if (mean_SigmaD_K > 0) // If driving stress is zero there will be no fracture propagation and hence no fracture deactivation
                                        {
                                            if ((length <= ts_halfLength_Kminus1_M) && (length < ts_halfLength_K_M)) // Type 1 term
                                            {
                                                // Add standard increment to s_MFP30 valid for Type 1 timesteps
                                                double tsM_s_MFP30_increment = tsM_s_MFP30_factor0 * tsK_Duration;
                                                tsK_s_MFP30_increments[indexPoint] += tsM_s_MFP30_increment;

                                                // Add standard increment to s_MFP32 valid for Type 1 timesteps
                                                double tsM_s_MFP32_increment = (tsM_s_MFP32_factor0 * (Math.Pow(ts_halfLength_K_M, 2) - Math.Pow(ts_halfLength_Kminus1_M, 2)))
                                                    + (tsM_s_MFP32_factor1 * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor));
                                                tsK_s_MFP32_increments[indexPoint] += tsM_s_MFP32_increment;
                                            }
                                            else if ((length <= ts_halfLength_Kminus1_Mminus1) && (length < ts_halfLength_K_M)) // Type 2 term
                                            {
                                                // Calculate J factors with a cutoff at l
                                                double t_factor = ((ts_halfLength_K_M - length) / MeanMFPropagationRate_K);
                                                double Jminus1_factor = (ts_halfLength_Kminus1_Mminus1 - length) / (hb1_factor * beta);
                                                double CumhGammaJminus1 = ts_CumhGammaMminus1 - Jminus1_factor;
                                                double ts_CumhGammaJminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * CumhGammaJminus1) : Math.Pow(CumhGammaJminus1, betac1_factor));
                                                double ts_CumhGammaJminus1_betac2_factor = (bis2 ? Math.Exp(betac2_factor * CumhGammaJminus1) : Math.Pow(CumhGammaJminus1, betac2_factor));

                                                // Add increment to s_MFP30
                                                double tsM_s_MFP30_increment = tsM_s_MFP30_factor0 * t_factor;
                                                tsM_s_MFP30_increment += tsM_s_MFP30_factor1 * (ts_CumhGammaM_betac1_factor - ts_CumhGammaJminus1_betac1_factor);
                                                tsK_s_MFP30_increments[indexPoint] += tsM_s_MFP30_increment;

                                                // Add increment to s_MFP32
                                                double tsM_s_MFP32_increment = tsM_s_MFP32_factor0 * (Math.Pow(ts_halfLength_K_M, 2) - Math.Pow(length, 2));
                                                tsM_s_MFP32_increment -= tsM_s_MFP32_factor1 * ts_CumhGammaMminus1_betac1_factor;
                                                tsM_s_MFP32_increment += tsM_s_MFP32_factor2 * (ts_CumhGammaM_betac2_factor + (ts_CumhGammaM_betac1_factor * (ts_halfLength_K_M / (beta_betac2_factor * hb1_factor)))
                                                    - ts_CumhGammaJminus1_betac2_factor - (ts_CumhGammaJminus1_betac1_factor * (length / (beta_betac2_factor * hb1_factor))));
                                                tsK_s_MFP32_increments[indexPoint] += tsM_s_MFP32_increment;
                                            }
                                            else if (length < ts_halfLength_K_M) // Type 3 term
                                            {
                                                // Calculate J factors with a cutoff at l
                                                double t_factor = ((ts_halfLength_K_M - length) / MeanMFPropagationRate_K);

                                                // Add increment to s_MFP30
                                                double tsM_s_MFP30_increment = tsM_s_MFP30_factor0 * t_factor;
                                                tsM_s_MFP30_increment += tsM_s_MFP30_factor1 * (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor);
                                                tsK_s_MFP30_increments[indexPoint] += tsM_s_MFP30_increment;

                                                // Add increment to s_MFP32
                                                double tsM_s_MFP32_increment = tsM_s_MFP32_factor0 * (Math.Pow(ts_halfLength_K_M, 2) - Math.Pow(length, 2));
                                                tsM_s_MFP32_increment += tsM_s_MFP32_factor2 * (ts_CumhGammaM_betac2_factor + (ts_CumhGammaM_betac1_factor * (ts_halfLength_K_M / (beta_betac2_factor * hb1_factor)))
                                                    - ts_CumhGammaMminus1_betac2_factor - (ts_CumhGammaMminus1_betac1_factor * (ts_halfLength_K_Mminus1 / (beta_betac2_factor * hb1_factor))));
                                                tsK_s_MFP32_increments[indexPoint] += tsM_s_MFP32_increment;
                                            }
                                            else if (length <= ts_halfLength_Kminus1_Mminus1) // Type 5 term
                                            {
                                                // Calculate J factors with a cutoff at l
                                                double J_factor = (ts_halfLength_K_M - length) / (hb1_factor * beta);
                                                double CumhGammaJ = ts_CumhGammaM - J_factor;
                                                double ts_CumhGammaJ_betac1_factor = (bis2 ? Math.Exp(betac1_factor * CumhGammaJ) : Math.Pow(CumhGammaJ, betac1_factor));
                                                double ts_CumhGammaJ_betac2_factor = (bis2 ? Math.Exp(betac2_factor * CumhGammaJ) : Math.Pow(CumhGammaJ, betac2_factor));
                                                double Jminus1_factor = (ts_halfLength_Kminus1_Mminus1 - length) / (hb1_factor * beta);
                                                double CumhGammaJminus1 = ts_CumhGammaMminus1 - Jminus1_factor;
                                                double ts_CumhGammaJminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * CumhGammaJminus1) : Math.Pow(CumhGammaJminus1, betac1_factor));
                                                double ts_CumhGammaJminus1_betac2_factor = (bis2 ? Math.Exp(betac2_factor * CumhGammaJminus1) : Math.Pow(CumhGammaJminus1, betac2_factor));

                                                // Add increment to s_MFP30
                                                double tsM_s_MFP30_increment = tsM_s_MFP30_factor1 * (ts_CumhGammaJ_betac1_factor - ts_CumhGammaJminus1_betac1_factor);
                                                tsK_s_MFP30_increments[indexPoint] += tsM_s_MFP30_increment;

                                                // Add increment to s_MFP32
                                                double tsM_s_MFP32_increment = -tsM_s_MFP32_factor1 * ts_CumhGammaMminus1_betac1_factor;
                                                tsM_s_MFP32_increment += tsM_s_MFP32_factor2 * (ts_CumhGammaJ_betac2_factor + (ts_CumhGammaJ_betac1_factor * (length / (beta_betac2_factor * hb1_factor)))
                                                    - ts_CumhGammaJminus1_betac2_factor - (ts_CumhGammaJminus1_betac1_factor * (length / (beta_betac2_factor * hb1_factor))));
                                                tsK_s_MFP32_increments[indexPoint] += tsM_s_MFP32_increment;
                                            }
                                            else if (length <= ts_halfLength_K_Mminus1) // Type 4 term
                                            {
                                                // Calculate J factors with a cutoff at l
                                                double J_factor = (ts_halfLength_K_M - length) / (hb1_factor * beta);
                                                double CumhGammaJ = ts_CumhGammaM - J_factor;
                                                double ts_CumhGammaJ_betac1_factor = (bis2 ? Math.Exp(betac1_factor * CumhGammaJ) : Math.Pow(CumhGammaJ, betac1_factor));
                                                double ts_CumhGammaJ_betac2_factor = (bis2 ? Math.Exp(betac2_factor * CumhGammaJ) : Math.Pow(CumhGammaJ, betac2_factor));

                                                // Add increment to s_MFP30
                                                double tsM_s_MFP30_increment = tsM_s_MFP30_factor1 * (ts_CumhGammaJ_betac1_factor - ts_CumhGammaMminus1_betac1_factor);
                                                tsK_s_MFP30_increments[indexPoint] += tsM_s_MFP30_increment;

                                                // Add increment to s_MFP32
                                                double tsM_s_MFP32_increment = tsM_s_MFP32_factor2 * (ts_CumhGammaJ_betac2_factor + (ts_CumhGammaJ_betac1_factor * (length / (beta_betac2_factor * hb1_factor)))
                                                    - ts_CumhGammaMminus1_betac2_factor - (ts_CumhGammaMminus1_betac1_factor * (ts_halfLength_K_Mminus1 / (beta_betac2_factor * hb1_factor))));
                                                tsK_s_MFP32_increments[indexPoint] += tsM_s_MFP32_increment;
                                            }
                                            else // Type 6 term
                                            {
                                                // tsM_s_MFP30 increment is zero
                                                // tsM_s_MFP32 increment is zero
                                            }

                                        } //end if (mean_SigmaDb_K > 0)

                                    } // End calculate increments for IPlus half-macrofractures
#if DBLOG
                                    if (is_tsN) logFile.WriteLine(string.Format("{0}\t", tsN_a_MFP32_values[indexPoint]));
#endif
                                } // End loop through all index points

                            } // End loop through all timesteps M

                            // Multiply s_MFP30 and s_MFP32 increments by fracture deactivation probabilities
                            for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                            {
                                tsK_sII_MFP30_increments[indexPoint] = tsK_s_MFP30_increments[indexPoint] * mean_FII_K;
                                tsK_sIJ_MFP30_increments[indexPoint] = tsK_s_MFP30_increments[indexPoint] * mean_FIJ_K;
                                tsK_s_MFP32_increments[indexPoint] *= mean_F_K;
                            }
#if DBLOG
                            string setData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", mean_FII_K, mean_FIJ_K, (tsK_s_MFP30_increments[0]) * CapB, (tsK_s_MFP30_increments[noIndexPoints - 1]) * CapB, (tsK_s_MFP32_increments[0]) * CapB * h * (Math.PI / 4), (tsK_s_MFP32_increments[noIndexPoints - 1]) * CapB * h * (Math.PI / 4));
                            //string setData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t", tsK, PreviousFractureData.getCum_hGamma_M(tsK), PreviousFractureData.getuFGrowthFactor(tsK), PreviousFractureData.getCumulativeHalfLength(tsK), PreviousFractureData.getHalfLength(tsK));
                            logFile.WriteLine(setData);
#endif
                        } // End case FractureEvolutionStage.Growing
                        break;
                    case FractureEvolutionStage.ResidualActivity:
                        // Use equations for residual fracture sets to update cumulative fracture population arrays
                        {
                            // Cache data for timestep K locally
                            // Timestep K duration
                            double tsK_Duration = PreviousFractureData.getDuration(tsK);
                            // Mean macrofracture propagation rate (=alpha_MF * current driving stress to the power of b)
                            double MeanMFPropagationRate_K = PreviousFractureData.getMeanMFPropagationRate(tsK);
                            // Clear zone volume
                            double theta_dashed_Kminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsK - 1);
                            // Microfracture propagation rate coefficient (gamma ^ 1/beta) 
                            double gamma_InvBeta_K = PreviousFractureData.getuFPropagationRateFactor(tsK);
                            // Stress shadow width
                            double Mean_MF_StressShadowWidth_K = PreviousFractureData.getMean_StressShadowWidth_M(tsK);
                            // Rate of growth of exclusion zone relative to stress shadow
                            // NB for consistency we will take the value at the end of the previous timestep, since this is the value used by the with the calculateTotalMacrofracturePopulation() function in the first timestep iteration
                            double dChi_dMFP32_K = getdChi_dMFP32_M(tsK - 1);
                            // Rate of growth of exclusion zone of this dipset relative to rate of total exclusion zone growth
                            // NB for consistency we will take the value at the end of the previous timestep, since this is the value used by the with the calculateTotalMacrofracturePopulation() function in the first timestep iteration
                            double dChiMP_dChiTot_K = fs.get_dChiMP_dChiTot(this, tsK - 1);

                            // Current instantaneous probabilities of macrofracture deactivation
                            double inst_F_K = PreviousFractureData.getInstantaneousF(tsK);
                            double inst_FII_K = PreviousFractureData.getInstantaneousFII(tsK);
                            double inst_FIJ_K = PreviousFractureData.getInstantaneousFIJ(tsK);

                            // Get a list of the probabilities that fractures will reach the index lengths
                            List<double> fractureLengthProbabilityMultipliers = fs.Calculate_Phi_ByDistance(this, tsK, MF_halflengths);

                            // Get a list of the normalised cumulative lengths of fractures longer than the index lengths
                            // In the evenly distributed stress scenario, the fractures in the other sets are randomly distributed, and we can use a quick version of the formula that assumes a constant instantaneous deactivation probability
                            // If there are stress shadows, the fractures in the other sets have a semi-regular distribution
                            // In this case we must use an approximate formula that takes the weighted mean stress shadow width of all dip sets - this should give a good approximation if most fractures are in one dip set
                            bool useQuickFormula = (gbc.PropControl.StressDistributionCase == StressDistribution.EvenlyDistributedStress);
                            List<double> fractureLengthGrowthMultipliers = fs.Calculate_MeanPropagationDistance(this, tsK, MF_halflengths, useQuickFormula);

                            // Calculate useful components
                            double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsK);
                            double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                            double ts_CumhGammaM_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betacminus1_factor));
                            double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsK - 1);
                            double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                            double ts_CumhGammaMminus1_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betacminus1_factor));
                            double ts_StressShadowDecreaseFactor_denominator = inst_F_K * dChiMP_dChiTot_K;
                            double ts_StressShadowDecreaseFactor = (ts_StressShadowDecreaseFactor_denominator > 0 ? Math.Exp(-2 * CapB * Math.Abs(betac_factor) * gamma_InvBeta_K * ts_CumhGammaMminus1_betacminus1_factor * MeanMFPropagationRate_K * h * dChi_dMFP32_K * tsK_Duration / ts_StressShadowDecreaseFactor_denominator) : 0);

                            // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                            if (double.IsInfinity(ts_CumhGammaM))
                                break;

                            // Calculate the active MFP30 value for all fractures of any length
                            // The cumulative MFP30 and MFP32 datapoints can be calculated by multiplying these base values with probability multipliers calculated for the fracture lengths
                            // If the fracture is not propagating then both gamma_InvBeta_K and inst_F_K will be zero, giving a NaN when calculating the residual a_MFP30 value
                            // In this case we will set tsK_a_MFP30_value to 0
                            double tsK_a_MFP30_value_base = (inst_F_K > 0 ? ((Math.Abs(betac_factor) * gamma_InvBeta_K) / inst_F_K) * theta_dashed_Kminus1 * ts_CumhGammaM_betacminus1_factor * ts_StressShadowDecreaseFactor : 0);

                            // Calculate the static MFP30 increments for all fractures of any length
                            // The cumulative MFP30 and MFP32 datapoints can be calculated by multiplying these base values with probability multipliers calculated for the fracture lengths
                            // First we must determine whether fracture growth is limited by decrease in nucleation rate or stress shadow growth
                            // We will calculate the increments for both end members and take the smallest
                            double tsK_s_MFP30_increment_NucleationLimited = theta_dashed_Kminus1 * (ts_CumhGammaM_betac_factor - ts_CumhGammaMminus1_betac_factor);
                            // If there are no stress shadows (s_MFP30_increment_StressShadowLimited_denominator is zero) set the stress shadow limited s_MFP30 increment to be higher than the nucleation rate limited increment, so it will not be used 
                            double s_MFP30_increment_StressShadowLimited_denominator = (MeanMFPropagationRate_K * h * dChi_dMFP32_K);
                            double tsK_s_MFP30_increment_StressShadowLimited = (s_MFP30_increment_StressShadowLimited_denominator == 0 ? tsK_s_MFP30_increment_NucleationLimited + 1 : (1 / (2 * CapB)) * ((theta_dashed_Kminus1 * inst_F_K * dChiMP_dChiTot_K) / s_MFP30_increment_StressShadowLimited_denominator) * (1 - ts_StressShadowDecreaseFactor));
                            double tsK_s_MFP30_increment_base = Math.Min(tsK_s_MFP30_increment_NucleationLimited, tsK_s_MFP30_increment_StressShadowLimited);
#if DBLOG
                            logFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}", tsK, theta_dashed_Kminus1, tsK_s_MFP30_increment_NucleationLimited, tsK_s_MFP30_increment_StressShadowLimited, inst_F_K, ts_StressShadowDecreaseFactor, ts_CumhGammaM, ts_CumhGammaM_betac_factor, ts_CumhGammaM_betacminus1_factor, ts_CumhGammaMminus1, ts_CumhGammaMminus1_betac_factor, ts_CumhGammaMminus1_betacminus1_factor));
                            logFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", tsK, MeanMFPropagationRate_K, Mean_MF_StressShadowWidth_K, dChi_dMFP32_K));
                            string lengthProbabilityMultipliers = string.Format("LengthProbabilityMultipliers Timestep {0}", tsK);
                            string lengthGrowthMultipliers = string.Format("LengthGrowthMultipliers Timestep {0}", tsK);
#endif
                            for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                            {
                                // Get the length for this index point and calculate the proportion of fractures longer than this
                                double length = MF_halflengths[indexPoint];
                                double fractureLengthProbabilityMultiplier = fractureLengthProbabilityMultipliers[indexPoint];
                                double fractureLengthGrowthMultiplier = fractureLengthGrowthMultipliers[indexPoint];
#if DBLOG
                                lengthProbabilityMultipliers += string.Format("\t{0}", fractureLengthProbabilityMultiplier);
                                lengthGrowthMultipliers += string.Format("\t{0}", fractureLengthGrowthMultiplier);
#endif
                                // We must still calculate values for a_MFP30 and a_MFP32 even if timestep K!=N, in order to check the that the increments in s_MFP32 values do not exceed the decreases in a_MFP32 values
                                // Calculate new a_MFP30 value for Timestep K
                                tsK_a_MFP30_values[indexPoint] = tsK_a_MFP30_value_base * fractureLengthProbabilityMultiplier;

                                // Calculate new a_MFP32 value for Timestep K
                                tsK_a_MFP32_values[indexPoint] = tsK_a_MFP30_value_base * h * fractureLengthGrowthMultiplier;

                                // Calculate s_MFP30 increments for Timestep K
                                // NB If the fracture is not propagating then inst_FII_K, inst_FIJ_K and inst_F_K will all be zero, giving a NaN when calculating the sII_MFP30 and sIJ_MFP30 increments
                                // In this case we will set both tsK_sII_MFP30_increment and tsK_sIJ_MFP30_increment to 0
                                if (inst_F_K > 0)
                                {
                                    tsK_sII_MFP30_increments[indexPoint] = tsK_s_MFP30_increment_base * fractureLengthProbabilityMultiplier * (inst_FII_K / inst_F_K);
                                    tsK_sIJ_MFP30_increments[indexPoint] = tsK_s_MFP30_increment_base * fractureLengthProbabilityMultiplier * (inst_FIJ_K / inst_F_K);
                                }
                                else
                                {
                                    tsK_sII_MFP30_increments[indexPoint] = 0;
                                    tsK_sIJ_MFP30_increments[indexPoint] = 0;
                                }

                                // Calculate s_MFP32 increment for Timestep K
                                tsK_s_MFP32_increments[indexPoint] = tsK_s_MFP30_increment_base * h * fractureLengthGrowthMultiplier;
#if DBLOG
                                logFile.WriteLine(lengthProbabilityMultipliers);
                                logFile.WriteLine(lengthGrowthMultipliers);
                                if (is_tsN) logFile.WriteLine(string.Format("{0}\t", tsK_a_MFP32_values[indexPoint]));
#endif
                            }
#if DBLOG
                            string setData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", inst_FII_K, inst_FIJ_K, (tsK_s_MFP30_increments[0]) * CapB, (tsK_s_MFP30_increments[noIndexPoints - 1]) * CapB, (tsK_s_MFP32_increments[0]) * CapB * h * (Math.PI / 4), (tsK_s_MFP32_increments[noIndexPoints - 1]) * CapB * h * (Math.PI / 4));
                            logFile.WriteLine(setData);
#endif
                        } // End case FractureEvolutionStage.ResidualActivity
                        break;
                    case FractureEvolutionStage.Deactivated:
                        // Active fracture fracture populations are zero and there is no increment to static fracture populations, so we do not need to do anything
                        break;
                    default:
                        break;
                }

                // Update the cumulative macrofracture population arrays
                for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                {
                    // Set new a_MFP30 value for Timestep K
                    tsN_a_MFP30_values[indexPoint] = tsK_a_MFP30_values[indexPoint];

                    // Set new a_MFP32 value for Timestep K
                    // Also calculate increment; we will need this to check against the s_MFP32 increment
                    double tsK_a_MFP32_increment = tsK_a_MFP32_values[indexPoint] - tsN_a_MFP32_values[indexPoint];
                    tsN_a_MFP32_values[indexPoint] = tsK_a_MFP32_values[indexPoint];

                    // Update s_MFP30 values for Timestep K
                    tsN_sII_MFP30_values[indexPoint] += tsK_sII_MFP30_increments[indexPoint];
                    tsN_sIJ_MFP30_values[indexPoint] += tsK_sIJ_MFP30_increments[indexPoint];

                    // Update s_MFP32 values for Timestep K
                    // Total half-macrofracture area cannot decrease 
                    // Therefore if the rate of growth of active MFP32 is negative, then the increase in static MFP32 must be greater than the decrease in active MFP32
                    double tsK_s_MFP32_increment = tsK_s_MFP32_increments[indexPoint];
                    if (tsK_s_MFP32_increment < -tsK_a_MFP32_increment)
                        tsK_s_MFP32_increment = -tsK_a_MFP32_increment;
                    tsN_s_MFP32_values[indexPoint] += tsK_s_MFP32_increment;
                }

            } // End calculate the static data increments and active data values for timestep K
#if DBLOG
            logFile.Close();
#endif

            // Apply general multipliers to all values
            for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
            {
                tsN_a_MFP30_values[indexPoint] *= CapB;
                tsN_sII_MFP30_values[indexPoint] *= CapB;
                tsN_sIJ_MFP30_values[indexPoint] *= CapB;
                tsN_a_MFP32_values[indexPoint] *= CapB;
                tsN_s_MFP32_values[indexPoint] *= CapB;
            }

            // Copy local arrays to half-macrofracture set objects
            IPlus_halfMacroFractures.a_P30 = tsN_a_MFP30_values;
            IPlus_halfMacroFractures.sII_P30 = tsN_sII_MFP30_values;
            IPlus_halfMacroFractures.sIJ_P30 = tsN_sIJ_MFP30_values;
            IPlus_halfMacroFractures.a_P32 = tsN_a_MFP32_values;
            IPlus_halfMacroFractures.s_P32 = tsN_s_MFP32_values;
            // For the IMinus_halfMacroFractures we will create duplicate lists of values rather than references to the same lists as IPlus_halfMacroFractures
            IMinus_halfMacroFractures.a_P30 = new List<double>(tsN_a_MFP30_values);
            IMinus_halfMacroFractures.sII_P30 = new List<double>(tsN_sII_MFP30_values);
            IMinus_halfMacroFractures.sIJ_P30 = new List<double>(tsN_sIJ_MFP30_values);
            IMinus_halfMacroFractures.a_P32 = new List<double>(tsN_a_MFP32_values);
            IMinus_halfMacroFractures.s_P32 = new List<double>(tsN_s_MFP32_values);
        }
        /// <summary>
        /// Calculate the cumulative active and static microfracture density distribution functions a_uFP30(r), s_uFP30(r), a_uFP32(r), s_uFP32(r), a_uFP33(r) and s_uFP33(r) at the current time, for a specified list of radii r
        /// </summary>
        public void calculateCumulativeMicrofracturePopulationArrays()
        {
            // Cache constants locally
            //double h = gbc.ThicknessAtDeformation;
            double max_uF_radius = gbc.MaximumMicrofractureRadius;
            double b = gbc.MechProps.b_factor;
            double beta = gbc.MechProps.beta;
            bType b_type = gbc.MechProps.GetbType();
            bool bis2 = (b_type == bType.Equals2);
            int tsN = PreviousFractureData.NoTimesteps;
            double rmin_cutoff = gbc.PropControl.minImplicitMicrofractureRadius;

            // Calculate local helper variables
            // betac factor is -beta*c if b<>2, -c if b=2
            double betac_factor = (b == 2 ? -c_coefficient : -(beta * c_coefficient));
            // betac1 factor is (1 - (beta c)) if b!=2, -c if b=2
            double betac1_factor = (b == 2 ? -c_coefficient : 1 - ((2 * c_coefficient) / (2 - b)));
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            //double hb1_factor = (b_type == bType.Equals2 ? half_h : Math.Pow(half_h, b / 2));
            // h2c_factor is (h/2)^(2-c) when c!=2, ln(h/2) when c=2
            double h2c_factor = (c_coefficient == 2 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 2 - c_coefficient));
            // h3c_factor is (h/2)^(3-c) when c!=3, ln(h/2) when c=3
            double h3c_factor = (c_coefficient == 3 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 3 - c_coefficient));
            // Calculate multipliers for the P32 and P33 values
            double uFP32_multiplier = CapB * Math.PI;
            double uFP33_multiplier = CapB * (4 / 3) * Math.PI;
            double b2_uFP32_factor = (c_coefficient == 2 ? 1 : (2 - c_coefficient));
            double b2_uFP33_factor = (c_coefficient == 3 ? 1 : (3 - c_coefficient));

            // Resort index array and ensure the maximum value is h/2
            uF_radii.Sort();
            int no_r_bins = uF_radii.Count();
            while (uF_radii[no_r_bins - 1] > max_uF_radius)
            {
                uF_radii.RemoveAt(no_r_bins - 1);
                no_r_bins--;
            }
            if (uF_radii[no_r_bins - 1] < max_uF_radius)
            {
                uF_radii.Add(max_uF_radius);
                no_r_bins++;
            }

            // Also set up local arrays to store the cumulative macrofracture population values for each index value as they are calculated
            List<double> a_uFP30_values = new List<double>();
            List<double> s_uFP30_values = new List<double>();
            List<double> a_uFP32_components = new List<double>();
            List<double> s_uFP32_components = new List<double>();
            List<double> a_uFP33_components = new List<double>();
            List<double> s_uFP33_components = new List<double>();
            List<double> a_uFP32_values = new List<double>();
            List<double> s_uFP32_values = new List<double>();
            List<double> a_uFP33_values = new List<double>();
            List<double> s_uFP33_values = new List<double>();

            // Initialise and populate local arrays, and clear out the arrays in the microfracture set object
            for (int indexPoint = 0; indexPoint < no_r_bins; indexPoint++)
            {
                // Add zero values to the local cumulative macrofracture population data arrays
                a_uFP30_values.Add(0d);
                s_uFP30_values.Add(0d);
                a_uFP32_components.Add(0d);
                s_uFP32_components.Add(0d);
                a_uFP33_components.Add(0d);
                s_uFP33_components.Add(0d);
                a_uFP32_values.Add(0d);
                s_uFP32_values.Add(0d);
                a_uFP33_values.Add(0d);
                s_uFP33_values.Add(0d);
            }

            // Finally set up a local variable to store the cumulative volumetric density for transition deactivation microfractures
            double st_uFP30_component = 0;

            // If the rmin cutoff is greater than the maximum microfracture radius, all terms will be zero
            if (rmin_cutoff < max_uF_radius)
            {
                // Loop through the previous timesteps M to calculate the static data increments
                // For the current timestep N also calculate the active data values
                for (int tsM = 1; tsM <= tsN; tsM++)
                {
                    // Set flag to determine if this is the last timestep
                    bool is_tsN = (tsM == tsN);

                    // Cache timestep M duration
                    double tsM_Duration = PreviousFractureData.getDuration(tsM);

                    // Cache current mean probability of microfracture deactivation
                    double mean_qiI_M = PreviousFractureData.getMean_qiI(tsM);

                    // Cache useful variables locally
                    double ts_gamma_InvBeta_M = PreviousFractureData.getuFPropagationRateFactor(tsM);
                    double ts_CumGammaM = PreviousFractureData.getCum_Gamma_M(tsM);
                    double ts_CumGammaMminus1 = PreviousFractureData.getCum_Gamma_M(tsM - 1);
                    double ts_theta_M = PreviousFractureData.getCumulativeTheta_AllFS_M(tsM);
                    double ts_theta_Mminus1 = PreviousFractureData.getCumulativeTheta_AllFS_M(tsM - 1);
                    // These variables are used for the terms representing transition deactivation microfractures, and also for calculating the upper cutoff for growth deactivation microfractures
                    double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsM);
                    double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                    double ts_CumhGammaM_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac1_factor));
                    double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsM - 1);
                    double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                    double ts_CumhGammaMminus1_betac1_factor = (bis2 ? Math.Exp(betac1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac1_factor));
                    double ts_theta_dashed_Mminus1 = PreviousFractureData.getCumulativeThetaDashed_AllFS_M(tsM - 1);
                    // If driving stress is zero (i.e. ts_gamma_InvBeta_M is zero) there will be no fracture propagation and hence no fracture deactivation
                    double ts_suF_deactivation_multiplier = (ts_gamma_InvBeta_M > 0 ? (mean_qiI_M * ts_theta_Mminus1) / ts_gamma_InvBeta_M : 0);

                    // Equations are different for b<2, b=2 and b>2
                    switch (b_type)
                    {
                        case bType.LessThan2:
                            {
                                // Loop through all index points / r_bins and calculate the components of microfracture population data arrays
                                for (int r_bin = 1; r_bin < no_r_bins; r_bin++)
                                {
                                    // Get range of radii sizes in the current r-bin
                                    double rb_minRad = uF_radii[r_bin - 1];
                                    double rb_maxRad = uF_radii[r_bin];

                                    // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                                    if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;
                                    // If the minimum bin size is zero, go straight on to the next bin
                                    if (rb_minRad <= 0) continue;

                                    // Calculate useful components
                                    double rb_rminCumGammaM_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaM;
                                    double rb_rmaxCumGammaM_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaM;
                                    double rb_rminCumGammaMminus1_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaMminus1;
                                    double rb_rmaxCumGammaMminus1_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaMminus1;
                                    double rb_rminCumGammaM_betac_factor = Math.Pow(rb_rminCumGammaM_factor, betac_factor);
                                    double rb_rmaxCumGammaM_betac_factor = Math.Pow(rb_rmaxCumGammaM_factor, betac_factor);
                                    double rb_rminCumGammaM_betac1_factor = Math.Pow(rb_rminCumGammaM_factor, betac1_factor);
                                    double rb_rmaxCumGammaM_betac1_factor = Math.Pow(rb_rmaxCumGammaM_factor, betac1_factor);
                                    double rb_rminCumGammaMminus1_betac1_factor = Math.Pow(rb_rminCumGammaMminus1_factor, betac1_factor);
                                    double rb_rmaxCumGammaMminus1_betac1_factor = Math.Pow(rb_rmaxCumGammaMminus1_factor, betac1_factor);

                                    // Do not calculate values if the extrapolated initial minimum radius is zero or less
                                    if (rb_rminCumGammaM_factor > 0)
                                    {
                                        // Only calculate a_uFP30 value if timestep M=N
                                        if (is_tsN)
                                        {
                                            // Calculate term for a_uFP30 value for rmin
                                            a_uFP30_values[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - ts_CumhGammaM_betac_factor);
                                        }

                                        // Calculate term for s_uFP30 increment for rmin
                                        s_uFP30_values[r_bin - 1] += ts_suF_deactivation_multiplier *
                                            (ts_CumhGammaM_betac1_factor - ts_CumhGammaMminus1_betac1_factor - rb_rminCumGammaM_betac1_factor + rb_rminCumGammaMminus1_betac1_factor);

                                        // If the maximum bin size is less than the minimum cutoff, do not calculate increment terms but go straight on to the next bin
                                        if (rb_maxRad < rmin_cutoff) continue;

                                        // Only calculate a_uFP32 and a_uFP33 increments if timestep M=N
                                        if (is_tsN)
                                        {
                                            // Calculate term for a_uFP32 value component for rmin to rmax
                                            a_uFP32_components[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - rb_rmaxCumGammaM_betac_factor);

                                            // Calculate term for a_uFP33 value component for rmin to rmax
                                            a_uFP33_components[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - rb_rmaxCumGammaM_betac_factor);
                                        }

                                        // Calculate term for s_uFP32 and s_uFP33 increment component for rmin to rmax
                                        // At this point they will be the same - as we have not yet converted to area or volume
                                        double s_uFP32_component_increment = ts_suF_deactivation_multiplier *
                                            (rb_rminCumGammaMminus1_betac1_factor - rb_rminCumGammaM_betac1_factor - rb_rmaxCumGammaMminus1_betac1_factor + rb_rmaxCumGammaM_betac1_factor);
                                        s_uFP32_components[r_bin - 1] += s_uFP32_component_increment;
                                        s_uFP33_components[r_bin - 1] += s_uFP32_component_increment;
                                    }
                                } // End loop through all r_bins
                            }
                            break;
                        case bType.Equals2:
                            {
                                // Calculate useful components
                                double ts_CumGammaM_betac_factor = Math.Exp(betac_factor * ts_CumGammaM);
                                double ts_CumGammaMminus1_betac_factor = Math.Exp(betac_factor * ts_CumGammaMminus1);

                                // Loop through all index points / r_bins and calculate the components of microfracture population data arrays
                                for (int r_bin = 1; r_bin < no_r_bins; r_bin++)
                                {
                                    // Get minimum radius size in the current r-bin
                                    double rb_minRad = uF_radii[r_bin - 1];

                                    // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                                    if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;

                                    // When b=2 the extrapolated initial minimum radius is always greater than 0
                                    {
                                        // We cannot calculate the a_uFP30 and s_uFP30 terms if rmin is zero, as they will be infinite 
                                        if (rb_minRad > 0)
                                        {
                                            // Calculate useful components
                                            double rb_minRad_betac_factor = Math.Pow(rb_minRad, betac_factor);
                                            double rb_rminCumGammaM_betac_factor = rb_minRad_betac_factor * ts_CumGammaM_betac_factor;
                                            double rb_rminCumGammaMminus1_betac_factor = rb_minRad_betac_factor * ts_CumGammaMminus1_betac_factor;

                                            // Only calculate a_uFP30 value if timestep M=N
                                            if (is_tsN)
                                            {
                                                // Calculate term for a_uFP30 value for rmin
                                                a_uFP30_values[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - ts_CumhGammaM_betac_factor);
                                            }

                                            // Calculate term for s_uFP30 increment for rmin (excluding transition deactivation microfractures)
                                            s_uFP30_values[r_bin - 1] += ts_suF_deactivation_multiplier *
                                                (rb_rminCumGammaM_betac_factor - rb_rminCumGammaMminus1_betac_factor - ts_CumhGammaM_betac1_factor + ts_CumhGammaMminus1_betac1_factor);
                                        }

                                        // We cannot calculate the a_uFP32 and s_uFP32 terms if rmin is zero and c>=2, as they will be infinite
                                        if ((rb_minRad > 0) || (c_coefficient < 2))
                                        {
                                            // Calculate useful components
                                            double rb_minRad_2c_factor = (c_coefficient == 2 ? Math.Log(rb_minRad) : Math.Pow(rb_minRad, 2 - c_coefficient));

                                            // Only calculate a_uFP32 value if timestep M=N
                                            if (is_tsN)
                                            {
                                                // Calculate term for a_uFP32 value for rmin
                                                a_uFP32_values[r_bin - 1] = ts_theta_M * ts_CumGammaM_betac_factor * (h2c_factor - rb_minRad_2c_factor);
                                            }

                                            // Calculate term for s_uFP32 increment for rmin (excluding transition deactivation microfractures)
                                            s_uFP32_values[r_bin - 1] += ts_suF_deactivation_multiplier * (ts_CumGammaM_betac_factor - ts_CumGammaMminus1_betac_factor) * (h2c_factor - rb_minRad_2c_factor);
                                        }

                                        // We cannot calculate the a_uFP33 and s_uFP33 terms if rmin is zero and c>=3, as they will be infinite
                                        if ((rb_minRad > 0) || (c_coefficient < 3))
                                        {
                                            // Calculate useful components
                                            double rb_minRad_3c_factor = (c_coefficient == 3 ? Math.Log(rb_minRad) : Math.Pow(rb_minRad, 3 - c_coefficient));

                                            // Only calculate a_uFP33 value if timestep M=N
                                            if (is_tsN)
                                            {
                                                // Calculate term for a_uFP33 value for rmin
                                                a_uFP33_values[r_bin - 1] = ts_theta_M * ts_CumGammaM_betac_factor * (h3c_factor - rb_minRad_3c_factor);
                                            }

                                            // Calculate term for s_uFP33 increment for rmin (excluding transition deactivation microfractures)
                                            s_uFP33_values[r_bin - 1] += ts_suF_deactivation_multiplier * (ts_CumGammaM_betac_factor - ts_CumGammaMminus1_betac_factor) * (h3c_factor - rb_minRad_3c_factor);
                                        }
                                    }
                                } // End loop through all r_bins
                            }
                            break;
                        case bType.GreaterThan2:
                            {
                                // Loop through all index points / r_bins and calculate the components of microfracture population data arrays
                                for (int r_bin = 1; r_bin < no_r_bins; r_bin++)
                                {
                                    // Get range of radii sizes in the current r-bin
                                    double rb_minRad = uF_radii[r_bin - 1];
                                    double rb_maxRad = uF_radii[r_bin];

                                    // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                                    if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;
                                    // If the minimum bin size is zero, go straight on to the next bin
                                    if (rb_minRad <= 0) continue;

                                    // Calculate useful components
                                    double rb_rminCumGammaM_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaM;
                                    double rb_rmaxCumGammaM_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaM;
                                    double rb_rminCumGammaMminus1_factor = Math.Pow(rb_minRad, 1 / beta) + ts_CumGammaMminus1;
                                    double rb_rmaxCumGammaMminus1_factor = Math.Pow(rb_maxRad, 1 / beta) + ts_CumGammaMminus1;
                                    double rb_rminCumGammaM_betac_factor = Math.Pow(rb_rminCumGammaM_factor, betac_factor);
                                    double rb_rmaxCumGammaM_betac_factor = Math.Pow(rb_rmaxCumGammaM_factor, betac_factor);
                                    double rb_rminCumGammaM_betac1_factor = Math.Pow(rb_rminCumGammaM_factor, betac1_factor);
                                    double rb_rmaxCumGammaM_betac1_factor = Math.Pow(rb_rmaxCumGammaM_factor, betac1_factor);
                                    double rb_rminCumGammaMminus1_betac1_factor = Math.Pow(rb_rminCumGammaMminus1_factor, betac1_factor);
                                    double rb_rmaxCumGammaMminus1_betac1_factor = Math.Pow(rb_rmaxCumGammaMminus1_factor, betac1_factor);

                                    // When b>2 the extrapolated initial minimum radius is always greater than 0
                                    // However if b is very large, 1/beta will be large and negative, so rb_rminCumGammaM_factor and rb_rmaxCumGammaM_factor may tend to infinity; in this case no uFP30, uFP32 or uFP33 values will be calculated
                                    if (rb_rminCumGammaM_factor < double.PositiveInfinity)
                                    {
                                        // Only calculate a_uFP30 value if timestep M=N
                                        if (is_tsN)
                                        {
                                            // Calculate term for a_uFP30 value for rmin
                                            a_uFP30_values[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - ts_CumhGammaM_betac_factor);
                                        }

                                        // Calculate term for s_uFP30 increment for rmin
                                        s_uFP30_values[r_bin - 1] += ts_suF_deactivation_multiplier *
                                            (rb_rminCumGammaM_betac1_factor - rb_rminCumGammaMminus1_betac1_factor - ts_CumhGammaM_betac1_factor + ts_CumhGammaMminus1_betac1_factor);

                                        // If the maximum bin size is less than the minimum cutoff, do not calculate increment terms but go straight on to the next bin
                                        if (rb_maxRad < rmin_cutoff) continue;

                                        // Only calculate a_uFP32 and a_uFP33 increments if timestep M=N
                                        if (is_tsN)
                                        {
                                            // Calculate term for a_uFP32 value component for rmin to rmax
                                            a_uFP32_components[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - rb_rmaxCumGammaM_betac_factor);

                                            // Calculate term for a_uFP33 value component for rmin to rmax
                                            a_uFP33_components[r_bin - 1] = ts_theta_M * (rb_rminCumGammaM_betac_factor - rb_rmaxCumGammaM_betac_factor);
                                        }

                                        // Calculate term for s_uFP32 and s_uFP33 increment component for rmin to rmax
                                        // At this point they will be the same - as we have not yet converted to area or volume
                                        double s_uFP32_component_increment = ts_suF_deactivation_multiplier *
                                            (rb_rminCumGammaM_betac1_factor - rb_rminCumGammaMminus1_betac1_factor - rb_rmaxCumGammaM_betac1_factor + rb_rmaxCumGammaMminus1_betac1_factor);
                                        s_uFP32_components[r_bin - 1] += s_uFP32_component_increment;
                                        s_uFP33_components[r_bin - 1] += s_uFP32_component_increment;
                                    }
                                } // End loop through all r_bins
                            } 
                            break;
                        default:
                            break;
                    } // End switch on b_type

                    // Calculate increment for the volumetric density of transition deactivation microfractures
                    st_uFP30_component += (ts_theta_Mminus1 - ts_theta_dashed_Mminus1) * (ts_CumhGammaM_betac_factor - ts_CumhGammaMminus1_betac_factor);

                } // End timestep M

                // Calculate final values for each component of the cumulative arrays

                if (bis2) // If b=2 then the cumulative uFP32 and uFP33 values have been calculated directly and we need only apply multipliers
                {
                    // Loop through the local value arrays and apply general multipliers
                    // Also add terms for transition deactivation microfractures to static fracture values
                    for (int r_bin = no_r_bins; r_bin > 0; r_bin--)
                    {
                        // Apply multiplier to a_uFP30
                        a_uFP30_values[r_bin - 1] *= CapB;

                        // Apply multipliers and add term for transition deactivation microfractures to s_uFP30
                        s_uFP30_values[r_bin - 1] /= c_coefficient;
                        s_uFP30_values[r_bin - 1] += st_uFP30_component;
                        s_uFP30_values[r_bin - 1] *= CapB;

                        // Apply multipliers to a_uFP32 and a_uFP33
                        a_uFP32_values[r_bin - 1] *= uFP32_multiplier * (c_coefficient / b2_uFP32_factor);
                        a_uFP33_values[r_bin - 1] *= uFP33_multiplier * (c_coefficient / b2_uFP33_factor);

                        // Apply multipliers and add terms for transition deactivation microfractures to s_uFP32 and s_uFP33
                        s_uFP32_values[r_bin - 1] /= b2_uFP32_factor;
                        s_uFP32_values[r_bin - 1] += (st_uFP30_component * Math.Pow(max_uF_radius, 2));
                        s_uFP32_values[r_bin - 1] *= uFP32_multiplier;
                        s_uFP33_values[r_bin - 1] /= b2_uFP33_factor;
                        s_uFP33_values[r_bin - 1] += (st_uFP30_component * Math.Pow(max_uF_radius, 3));
                        s_uFP33_values[r_bin - 1] *= uFP33_multiplier;
                    }
                }
                else // If b!=2 then we must calculate the cumulative uFP32 and uFP33 values numerically by summing the components in each r_bin
                {
                    // The largest value in the array will just contain the residual values
                    int r_bin = no_r_bins;

                    // Set a_uFP30, a_uFP32 and a_uFP33 to zero and set s_uFP30, s_uFP32 and s_uFP33 to the appropriate values for the transition deactivation microfractures
                    a_uFP30_values[r_bin - 1] = 0;
                    a_uFP32_values[r_bin - 1] = 0;
                    a_uFP33_values[r_bin - 1] = 0;
                    s_uFP30_values[r_bin - 1] = CapB * st_uFP30_component;
                    s_uFP32_values[r_bin - 1] = uFP32_multiplier * Math.Pow(max_uF_radius, 2) * st_uFP30_component;
                    s_uFP33_values[r_bin - 1] = uFP33_multiplier * Math.Pow(max_uF_radius, 3) * st_uFP30_component;

                    // Loop through the local value arrays
                    for (r_bin--; r_bin > 0; r_bin--)
                    {
                        // Get range of radii sizes in the current r-bin
                        double rb_minRad = uF_radii[r_bin - 1];
                        double rb_maxRad = uF_radii[r_bin];

                        // uFP30 values have been calculated directly and we need only apply multipliers and terms for transition deactivation microfractures (to s_uFP30) 
                        // Apply multiplier to a_uFP30
                        a_uFP30_values[r_bin - 1] *= CapB;

                        // Apply multipliers and add term for transition deactivation microfractures to s_uFP30
                        s_uFP30_values[r_bin - 1] /= betac1_factor;
                        s_uFP30_values[r_bin - 1] += st_uFP30_component;
                        s_uFP30_values[r_bin - 1] *= CapB;

                        // uFP32 and uFP33 values must be calculated numerically as the sum of components from all the higher component bins  

                        // Copy the previously calculated cumulative density values from the next higher bin
                        a_uFP32_values[r_bin - 1] = a_uFP32_values[r_bin];
                        a_uFP33_values[r_bin - 1] = a_uFP33_values[r_bin];
                        s_uFP32_values[r_bin - 1] = s_uFP32_values[r_bin];
                        s_uFP33_values[r_bin - 1] = s_uFP33_values[r_bin];

                        // Now add the components calculated for this bin
                        a_uFP32_values[r_bin - 1] += uFP32_multiplier * Math.Pow(rb_minRad, 2) * a_uFP32_components[r_bin - 1];
                        a_uFP33_values[r_bin - 1] += uFP33_multiplier * Math.Pow(rb_minRad, 3) * a_uFP33_components[r_bin - 1];
                        s_uFP32_values[r_bin - 1] += (uFP32_multiplier / betac1_factor) * Math.Pow(rb_minRad, 2) * s_uFP32_components[r_bin - 1];
                        s_uFP33_values[r_bin - 1] += (uFP33_multiplier / betac1_factor) * Math.Pow(rb_minRad, 3) * s_uFP33_components[r_bin - 1];

                    } // End loop through all r_bins

                } // End if bis2

            } // End if the rmin cutoff is greater than the maximum microfracture radius

            // Copy local arrays to microfracture set objects
            MicroFractures.a_P30 = a_uFP30_values;
            MicroFractures.s_P30 = s_uFP30_values;
            MicroFractures.a_P32 = a_uFP32_values;
            MicroFractures.s_P32 = s_uFP32_values;
            MicroFractures.a_P33 = a_uFP33_values;
            MicroFractures.s_P33 = s_uFP33_values;
        }
        /// <summary>
        /// Set the current fracture evolution stage to Deactivated
        /// </summary>
        public void deactivateFractures()
        {
            CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
        }
        /// <summary>
        /// Check if the if the extrapolated initial radius of a microfracture with current radius r is zero or less, and if so deactivate the fracture set (will only apply if b is less than 2)
        /// </summary>
        /// <param name="r">Current microfracture radius</param>
        /// <returns>True if initial microfracture radius is zero or less; false if it is greater than zero or b is greater than or equal to 2</returns>
        public bool Check_Initial_uF_Radius(double r, double b)
        {
            if (b < 2)
            {
                double initial_minrb_minRad = Math.Pow(r, (2 - b) / 2) + CurrentFractureData.Cum_Gamma_M;
                if (initial_minrb_minRad <= 0)
                {
                    deactivateFractures();
                    return true;
                }
            }
            return false;
        }

        // Reset and data input functions
        /// <summary>
        /// Reset all fracture data to initial values (no fractures, no previous deformation)
        /// </summary>
        public void resetFractureData()
        {
            // Create empty arrays for microfracture radii and macrofracture halflengths
            uF_radii = new List<double>();
            MF_halflengths = new List<double>();

            // Create new Microfracture and Macrofracture objects; override the microfracture radii and macrofracture halflength arrays
            MicroFractures = new MicrofractureData(gbc, this, uF_radii);
            IPlus_halfMacroFractures = new MacrofractureData(gbc, this, MF_halflengths);
            IMinus_halfMacroFractures = new MacrofractureData(gbc, this, MF_halflengths);

            // Set initial microfracture densities according to specified density and distribution coefficients
            double max_uF_radius = gbc.MaximumMicrofractureRadius;
            double rmin_cutoff = gbc.PropControl.minImplicitMicrofractureRadius;
            // NB Since we do not know the number of calculation bins for r at this stage, we can only calculate uFP32 and uFP33 if rmin_cutoff > 0
            if ((rmin_cutoff > 0) && (rmin_cutoff < max_uF_radius))
            {
                MicroFractures.a_P30_total = CapB * (Math.Pow(rmin_cutoff, -c_coefficient) - Math.Pow(rmin_cutoff, -c_coefficient));
                if (c_coefficient == 2)
                    MicroFractures.a_P32_total = 2 * Math.PI * CapB * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                else
                    MicroFractures.a_P32_total = Math.PI * CapB * (c_coefficient / (2 - c_coefficient)) * (Math.Pow(max_uF_radius, 2 - c_coefficient) - Math.Pow(rmin_cutoff, 2 - c_coefficient));
                if (c_coefficient == 3)
                    MicroFractures.a_P33_total = 4 * Math.PI * CapB * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                else
                    MicroFractures.a_P33_total = (4 / 3) * Math.PI * CapB * (c_coefficient / (3 - c_coefficient)) * (Math.Pow(max_uF_radius, 3 - c_coefficient) - Math.Pow(rmin_cutoff, 3 - c_coefficient));
            }

            // Macrofracture growth rate data - set all growth rates to zero
            da_MFP30 = 0;
            dsII_MFP30 = 0;
            dsIJ_MFP30 = 0;
            da_MFP32 = 0;
            ds_MFP32 = 0;

            // Create new fracture calculation data object, and initialise for timestep 0 (i.e. initial data before the model runs)
            CurrentFractureData = new FractureCalculationData();

            // Create an new list for previous fracture calculation data objects and use the CurrentFractureData object for timestep 0 
            // Initial_uF_factor is a component related to the maximum microfracture radius rmax, included in Cum_hGamma to represent the initial population of seed macrofractures: ln(rmax) for b=2; rmax^(1/beta) for b!=2
            PreviousFractureData = new FCD_List(gbc.Initial_uF_factor, CurrentFractureData, true);
        }

        // Constructors
        /// <summary>
        /// Default constructor: set default values 
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        public FractureDipSet(GridblockConfiguration gbc_in, Gridblock_FractureSet fs_in)
                : this(gbc_in, fs_in, FractureMode.Mode1, true, false, Math.PI / 2, 0.001, 3d)
        {
            // Defaults:

            // Fracture Mode and dip: set to vertical Mode 1 fractures
            // Bimodal conjugate flag: set to true
            // Include reverse fractures: set to false

            // Initial microfracture distribution - set to power law, B=0.001, c=3
        }
        /// <summary>
        /// Constructor: input fracture mode, dip and initial fracture distribution parameters
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="Mode_in">Fracture mode</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, the dipset contains equal numbers of fractures dipping in opposite directions; if false, the dipset contains only fractures dipping in the specified azimuth direction</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        public FractureDipSet(GridblockConfiguration gbc_in, Gridblock_FractureSet fs_in, FractureMode Mode_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in, double Dip_in, double B_in, double c_in)
            : this(gbc_in, fs_in, Mode_in, BiazimuthalConjugate_in, IncludeReverseFractures_in, Dip_in, B_in, c_in, 0.0005, 1E-5)
        {
            // Defaults for fracture aperture control data for uniform and size-dependent aperture:

            // Fixed aperture for fractures in the uniform aperture case: 0.5mm
            // Multiplier for fracture aperture in the size-dependent aperture case: 1E-5 (gives 1mm aperture for 100m high fracture) 
        }
        /// <summary>
        /// Constructor: input fracture mode and dip, initial fracture distribution parameters, and fracture aperture control data for uniform and size-dependent aperture
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="Mode_in">Fracture mode</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, the dipset contains equal numbers of fractures dipping in opposite directions; if false, the dipset contains only fractures dipping in the specified azimuth direction</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="UniformAperture_in">Fixed aperture for fractures in the uniform aperture case (m)</param>
        /// <param name="SizeDependentApertureMultiplier_in">Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public FractureDipSet(GridblockConfiguration gbc_in, Gridblock_FractureSet fs_in, FractureMode Mode_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in, double Dip_in, double B_in, double c_in, double UniformAperture_in, double SizeDependentApertureMultiplier_in)
        {
            // Reference to grandparent GridblockConfiguration object
            gbc = gbc_in;
            // Reference to parent FractureSet object
            fs = fs_in;

            // Set the fracture Mode, biazimuthal conjugate and include reverse fractures flags
            Mode = Mode_in;
            BiazimuthalConjugate = BiazimuthalConjugate_in;
            IncludeReverseFractures = IncludeReverseFractures_in;

            // Set fracture dip
            Dip = Dip_in;

            // Set the applied strain components to default values
            eff2d_e2d = 1;
            efw2d_e2d = 0;
            efffwd_e2d = 0;
            efffsd_e2d = 0;

            // Set the initial shear stress pitch to NaN and the initial shear stress vector to (0,0,0)
            // This represents no shear stress on the fracture
            ShearStressPitch = double.NaN;
            shearStressVector = new VectorXYZ(0, 0, 0);

            // Set the initial shear displacement pitch to NaN and the initial shear displacement vector to (0,0,0)
            // This represents no shear displacement
            DisplacementPitch = double.NaN;
            displacementVector = new VectorXYZ(0, 0, 0);

            // Calculate the initial compliance tensor base; NB we assume initial driving stress is zero
            RecalculateComplianceTensorBase(false);

            // Initial microfracture distribution - set to power law
            InitialDistribution = InitialFractureDistribution.PowerLaw;
            CapB = B_in;
            c_coefficient = c_in;

            // Set fracture aperture control data for uniform and size-dependent aperture
            // Fixed aperture for fractures in the uniform aperture case (m)
            UniformAperture = UniformAperture_in;
            // Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier
            SizeDependentApertureMultiplier = SizeDependentApertureMultiplier_in;

            // Create new arrays for microfracture radii and macrofracture halflengths, Microfracture and Macrofracture objects, and fracture calculation data objects
            resetFractureData();
        }
    }
}
