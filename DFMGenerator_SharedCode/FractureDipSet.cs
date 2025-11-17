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
    /// Enumerator to distinguish different fracture types within a fracture set
    /// </summary>
    public enum FractureType { Microfractures, LayerBoundFractures, AllFractures }
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
        /// Variable for sine of fracture dip
        /// </summary>
        private double sindip;
        /// <summary>
        /// Variable for cosine of fracture dip
        /// </summary>
        private double cosdip;
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

                // Set the sine and cosine of fracture dip
                sindip = VectorXYZ.Sin_trim(Dip);
                cosdip = VectorXYZ.Cos_trim(Dip);

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
        /// <summary>
        /// For a biazimuthally conjugate fracture set, will return the normal vector for a fracture dipping in the J- direction (i.e. the NormalVector rotated 180deg around the Z axis); for a non-biazimuthally conjugate fracture set, will return the NormalVector
        /// </summary>
        public VectorXYZ ConjugateNormalVector { get { return BiazimuthalConjugate ? new VectorXYZ(-normalVector.Component(VectorComponents.X), -normalVector.Component(VectorComponents.Y), normalVector.Component(VectorComponents.Z)) : NormalVector; } }
        /// <summary>
        /// For a biazimuthally conjugate fracture set, will return the dip vector for a fracture dipping in the J- direction (i.e. the DipVector rotated 180deg around the Z axis); for a non-biazimuthally conjugate fracture set, will return the DipVector
        /// </summary>
        public VectorXYZ ConjugateDipVector { get { return BiazimuthalConjugate ? new VectorXYZ(-dipVector.Component(VectorComponents.X), -dipVector.Component(VectorComponents.Y), dipVector.Component(VectorComponents.Z)) : DipVector; } }

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
        public double[] uF_radii { get { return MicroFractures.radii; } }
        /// <summary>
        /// Index half-lengths for cumulative population distribution function arrays
        /// </summary>
        public double[] MF_halflengths { get { return IPlus_halfMacroFractures.halflengths; } }

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
        /// <summary>
        /// Flag to deactivate the fracture set at the start of the next timestep
        /// </summary>
        private bool DeactivateNextTimestep;

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
        /// Return the volumetric density of all half-macrofractures from other fracture sets that terminate against half-macrofractures from this dipset, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTerminatingFractureDensity() { return CurrentFractureData.TerminatingFractureDensity_M; }
        /// <summary>
        /// Return the mean number of half-macrofractures from other fracture sets that terminate against a half-macrofracture from this dipset, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTerminatingFracturesPerMF() { return CurrentFractureData.TerminatingFracturesPerMF_M; }
        ///// <summary>
        ///// Return the P31 value for all microfractures, static and dynamic, during the current timestep
        ///// </summary>
        ///// <returns></returns>
        //public double getTotaluFP31() { return CurrentFractureData.Total_uFP31_M; }
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
        ///// <summary>
        ///// Return the P34 value for all microfractures, static and dynamic, during the current timestep
        ///// </summary>
        ///// <returns></returns>
        //public double getTotaluFP34() { return CurrentFractureData.Total_uFP34_M; }
        /// <summary>
        /// Return the P35 value for all microfractures, static and dynamic, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotaluFP35() { return CurrentFractureData.Total_uFP35_M; }
        /// <summary>
        /// Return the piecewise population distribution function (not cumulative) for total microfracture volumetric density, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double[] getDuFP30_distribution() { return CurrentFractureData.DuFP30_distribution_M; }
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
        /// <summary>
        /// Return the weighted average azimuthal stress shadow width throughout the growth of the fracture network
        /// </summary>
        /// <returns>The average of the azimuthal stress shadow width during each timestep, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_AzimuthalStressShadowWidth() { return PreviousFractureData.getWeightedAverage_AzimuthalStressShadowWidth(); }
        /// <summary>
        /// Return the weighted average shear stress shadow width throughout the growth of the fracture network
        /// </summary>
        /// <returns>The average of the shear stress shadow width during each timestep, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_ShearStressShadowWidth() { return PreviousFractureData.getWeightedAverage_ShearStressShadowWidth(); }
        /// <summary>
        /// Return the weighted average stress shadow width throughout the growth of the fracture network
        /// </summary>
        /// <returns>The average of the stress shadow width during each timestep, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_StressShadowWidth() { return PreviousFractureData.getWeightedAverage_StressShadowWidth(); }

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
        /// Return the volumetric density of all half-macrofractures from other fracture sets that terminate against half-macrofractures from this dipset, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTerminatingFractureDensity(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTerminatingFractureDensity_M(Timestep_M); }
        /// <summary>
        /// Return the mean number of half-macrofractures from other fracture sets that terminate against a half-macrofracture from this dipset, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTerminatingFracturesPerMF(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTerminatingFracturesPerMF_M(Timestep_M); }
        ///// <summary>
        ///// Return the P31 value for all microfractures, static and dynamic, at the end of a specified previous timestep
        ///// </summary>
        ///// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        ///// <returns></returns>
        //public double getTotaluFP31(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_uFP31_M(Timestep_M); }
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
        ///// <summary>
        ///// Return the P34 value for all microfractures, static and dynamic, at the end of a specified previous timestep
        ///// </summary>
        ///// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        ///// <returns></returns>
        //public double getTotaluFP34(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_uFP34_M(Timestep_M); }
        /// <summary>
        /// Return the P35 value for all microfractures, static and dynamic, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotaluFP35(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_uFP35_M(Timestep_M); }
        /// <summary>
        /// Return the piecewise population distribution function (not cumulative) for total microfracture volumetric density, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double[] getDuFP30_distribution(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getDuFP30_distribution_M(Timestep_M); }
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
        /// Return the total porosity of all microfractures that were present at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotaluFPorosity(int Timestep_M)
        {
            return getTotaluFPorosity(gbc.PropControl.FractureApertureControl, Timestep_M);
        }
        /// <summary>
        /// Return the total porosity of all half-macrofractures that were present at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalMFPorosity(int Timestep_M)
        {
            return getTotalMFPorosity(gbc.PropControl.FractureApertureControl, Timestep_M);
        }
        /// <summary>
        /// Return the total porosity of all microfractures that were present at the end of a specified previous timestep, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotaluFPorosity(FractureApertureType ApertureControl, int Timestep_M)
        {
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = PreviousFractureData.getTotal_uFP32_M(Timestep_M) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = PreviousFractureData.getTotal_uFP33_M(Timestep_M) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = PreviousFractureData.getTotal_uFP33_M(Timestep_M) * gbc.MechProps.DynamicApertureMultiplier * (4 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = PreviousFractureData.getTotal_uFP32_M(Timestep_M) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Return the total porosity of all half-macrofractures that were present at the end of a specified previous timestep, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalMFPorosity(FractureApertureType ApertureControl, int Timestep_M)
        {
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = PreviousFractureData.getTotal_MFP32_M(Timestep_M) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * PreviousFractureData.getTotal_MFP32_M(Timestep_M) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * PreviousFractureData.getTotal_MFP32_M(Timestep_M) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = PreviousFractureData.getTotal_MFP32_M(Timestep_M) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Return the weighted average azimuthal stress shadow width up to a specified timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns>The average of the azimuthal stress shadow width during each timestep up to and including Timestep_M, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_AzimuthalStressShadowWidth(int Timestep_M) { return PreviousFractureData.getWeightedAverage_AzimuthalStressShadowWidth(Timestep_M); }
        /// <summary>
        /// Return the weighted average shear stress shadow width up to a specified timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns>The average of the shear stress shadow width during each timestep up to and including Timestep_M, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_ShearStressShadowWidth(int Timestep_M) { return PreviousFractureData.getWeightedAverage_ShearStressShadowWidth(Timestep_M); }
        /// <summary>
        /// Return the weighted average stress shadow width up to a specified timestep M
        /// </summary>
        /// <param name="Timestep_M">Timestep M</param>
        /// <returns>The average of the stress shadow width during each timestep up to and including Timestep_M, weighted by the increase MFP32 during that timestep</returns>
        public double getWeightedAverage_StressShadowWidth(int Timestep_M) { return PreviousFractureData.getWeightedAverage_StressShadowWidth(Timestep_M); }
        /// <summary>
        /// Get the time at which the fracture set becomes deactivated
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if the fracture set was never active: if true, will return Nan; if false, will return 0</param>
        /// <returns>Deactivation time of fracture set; will return zero or NaN if the fracture set was never active</returns>
        public double getFinalActiveTime(bool ReturnNanForUndefined)
        {
            // Get time units and unit conversion modifier for output time data if not in SI units
            double timeUnits_Modifier = gbc.PropControl.getTimeUnitsModifier();

            return PreviousFractureData.getFinalActiveTime(ReturnNanForUndefined) / timeUnits_Modifier;
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
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = 2 * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (8 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = (usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (4d / 3d) * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (16 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = (usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = gbc.ThicknessAtDeformation * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = gbc.MechProps.DynamicApertureMultiplier * (2 * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = (usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (Math.PI / 4) * gbc.ThicknessAtDeformation * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = gbc.MechProps.DynamicApertureMultiplier * (Math.PI * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = (usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Maximum aperture of a microfracture of a specified radius at the end of a previous timestep
        /// </summary>
        /// <param name="radius">Microfracture radius (m)</param>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumMicrofractureAperture(double radius, int timestep)
        {
            double output;

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
                    double tensile_sigmaNeff = -PreviousFractureData.getFinalNormalStress(timestep);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (8 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = PreviousFractureData.getFinalNormalStress(timestep);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

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
                    double tensile_sigmaNeff = -PreviousFractureData.getFinalNormalStress(timestep);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (16 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = PreviousFractureData.getFinalNormalStress(timestep);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Maximum half-macrofracture aperture at the end of a previous timestep
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumMacrofractureAperture(int timestep)
        {
            double output;

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
                    double tensile_sigmaNeff = -PreviousFractureData.getFinalNormalStress(timestep);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = gbc.MechProps.DynamicApertureMultiplier * (2 * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = PreviousFractureData.getFinalNormalStress(timestep);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Mean half-macrofracture aperture at the end of a previous timestep
        /// </summary>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanMacrofractureAperture(int timestep)
        {
            double output;

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
                    double tensile_sigmaNeff = -PreviousFractureData.getFinalNormalStress(timestep);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = gbc.MechProps.DynamicApertureMultiplier * (Math.PI * gbc.ThicknessAtDeformation * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (2 * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = PreviousFractureData.getFinalNormalStress(timestep);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Fracture aperture based on Barton-Bandis model for a specified effective normal stress
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
        // Fracture compressibility is dependent on aperture control data
        /// <summary>
        /// Get the macrofracture compressibility, based on the aperture control data
        /// </summary>
        /// <returns>Elastic compressibility for Dynamic aperture, inverse of specified fracture normal stiffness for Barton-Bandis aperture, NaN for other apertures</returns>
        public double getMacrofractureCompressibility()
        {
            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    // Fracture compressibility not defined for Uniform aperture
                    return double.NaN;
                case FractureApertureType.SizeDependent:
                    // Fracture compressibility not defined for Size Dependent aperture
                    return double.NaN;
                case FractureApertureType.Dynamic:
                    // Calculate compressibility based on elastic closure
                    double geometricFactor = 2;
                    double elasticMod = (1 - Math.Pow(gbc.MechProps.Nu_r, 2)) / gbc.MechProps.E_r;
                    double sizeFactor = gbc.CurrentThickness;
                    return geometricFactor * elasticMod * sizeFactor;
                case FractureApertureType.BartonBandis:
                    // Use the inverse of the specified fracture normal stiffness
                    return 1 / gbc.MechProps.FractureNormalStiffness;
                default:
                    // Return NaN
                    return double.NaN;
            }
        }
        /// <summary>
        /// Get the microfracture compressibility, based on the aperture control data
        /// </summary>
        /// <returns>Elastic compressibility for Dynamic aperture, inverse of specified fracture normal stiffness for Barton-Bandis aperture, NaN for other apertures</returns>
        public double getMicrofractureCompressibility(double radius)
        {
            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    // Fracture compressibility not defined for Uniform aperture
                    return double.NaN;
                case FractureApertureType.SizeDependent:
                    // Fracture compressibility not defined for Size Dependent aperture
                    return double.NaN;
                case FractureApertureType.Dynamic:
                    // Calculate compressibility based on elastic closure
                    double geometricFactor = 8 / Math.PI;
                    double elasticMod = (1 - Math.Pow(gbc.MechProps.Nu_r, 2)) / gbc.MechProps.E_r;
                    double sizeFactor = radius;
                    return geometricFactor * elasticMod * sizeFactor;
                case FractureApertureType.BartonBandis:
                    // Use the inverse of the specified fracture normal stiffness
                    return 1 / gbc.MechProps.FractureNormalStiffness;
                default:
                    // Return NaN
                    return double.NaN;
            }
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
        /// Total P31 value for active microfractures
        /// </summary>
        /// <returns></returns>
        //public double a_uFP31_total() { return MicroFractures.a_P31_total; }
        /// <summary>
        /// Total P31 value for static microfractures
        /// </summary>
        /// <returns></returns>
        //public double s_uFP31_total() { return MicroFractures.s_P31_total; }
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
        /// Total P34 value for active microfractures
        /// </summary>
        /// <returns></returns>
        //public double a_uFP34_total() { return MicroFractures.a_P34_total; }
        /// <summary>
        /// Total P34 value for static microfractures
        /// </summary>
        /// <returns></returns>
        //public double s_uFP34_total() { return MicroFractures.s_P34_total; }
        /// <summary>
        /// Total P35 value for active microfractures
        /// </summary>
        /// <returns></returns>
        public double a_uFP35_total() { return MicroFractures.a_P35_total; }
        /// <summary>
        /// Total P35 value for static microfractures
        /// </summary>
        /// <returns></returns>
        public double s_uFP35_total() { return MicroFractures.s_P35_total; }
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
        /// Volumetric density distribution (not cumulative) for active microfractures 
        /// </summary>
        /// <param name="index">Index for piecewise distribution function arrays</param>
        /// <returns></returns>
        public double a_uFDP30(int index) { return MicroFractures.a_DP30[index]; }
        /// <summary>
        /// Volumetric density distribution (not cumulative) for static microfractures 
        /// </summary>
        /// <param name="index">Index for piecewise distribution function arrays</param>
        /// <returns></returns>
        public double s_uFDP30(int index) { return MicroFractures.s_DP30[index]; }
        /// <summary>
        /// Volumetric density distribution function (not cumulative) for all microfractures 
        /// </summary>
        /// <returns></returns>
        public double[] T_uFDP30()
        {
            int no_rbins = uF_radii.Length;
            double[] output = new double[no_rbins];
            for (int r_bin = 0; r_bin < no_rbins; r_bin++)
                output[r_bin] = MicroFractures.a_DP30[r_bin] + MicroFractures.s_DP30[r_bin];
            return output;
        }
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
        /// Vector for the direction of shear displacement on the fracture surface
        /// NB The displacement vector will not be a unit length vector
        /// This is necessary to get the correct results when calculating frictional traction and compliance for strike-slip fractures
        /// </summary>
        private VectorXYZ shearDisplacementVector;
        /// <summary>
        /// Pitch of the shear stress vector on the surface of the fractures relative to fracture strike (radians, positive downwards; will return NaN if shear stress is zero)
        /// </summary>
        public double ShearStressPitch { get; private set; }
        /// <summary>
        /// Pitch of the shear displacement vector on the surface of the fractures relative to fracture strike (radians, positive downwards; will return NaN if shear displacement is zero)
        /// </summary>
        public double DisplacementPitch { get; private set; }
        /// <summary>
        /// Unit vector for the direction of shear stress on the fracture surface
        /// </summary>
        public VectorXYZ ShearStressVector { get { return new VectorXYZ(shearStressVector); } }
        /// <summary>
        /// Vector for the direction of shear displacement on the fracture surface
        /// NB The displacement vector will not be a unit length vector
        /// This is necessary to get the correct results when calculating frictional traction and compliance for strike-slip fractures
        /// </summary>
        public VectorXYZ ShearDisplacementVector { get { return new VectorXYZ(shearDisplacementVector); } }
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
        /// Driving stress vector at the start of the timestep; this is equivalent to and has a magnitude equal to the scalar quantity U
        /// </summary>
        private VectorXYZ DrivingStressVector;
        /// <summary>
        /// Driving stress vector at the start of the timestep, adjusted for macrofracture displacement orientation
        /// </summary>
        private VectorXYZ MFDisplacementAdjustedDrivingStressVector;
        /// <summary>
        /// Mean displacement vector at the start of the timestep for a microfracture of unit radius
        /// </summary>
        public VectorXYZ uFDisplacementVector { get { return -(16 / (3 * Math.PI * gbc.MechProps.PlainStrainEffectiveE_r)) * DrivingStressVector; } }
        /// <summary>
        /// Mean displacement vector at the start of the timestep for a half-macrofracture
        /// </summary>
        public VectorXYZ MFDisplacementVector { get { return -((Math.PI * gbc.ThicknessAtDeformation) / (2 * gbc.MechProps.PlainStrainEffectiveE_r)) * MFDisplacementAdjustedDrivingStressVector; } }
        /// <summary>
        /// Recalculate the shear displacement pitch and vector, the fracture mode and the compliance tensor base for the given effective stress tensor
        /// </summary>
        public void RecalculateElasticResponse(Tensor2S CurrentStress)
        {
            // Get stress vector acting on the fracture plane
            VectorXYZ stressOnFracture = CurrentStress * normalVector;

            // Calculate the magnitude of normal stress on the fracture plane, and the shear stresses acting on the fracture plane in the downdip and along-strike directions respectively
            double normalStressMagnitude = normalVector & stressOnFracture;
            double dipShearStressMagnitude = dipVector & stressOnFracture;
            double strikeShearStressMagnitude = fs.StrikeVector & stressOnFracture;

            // Recalculate the shear displacement pitch and vector, and flag if it has changed
            double shearStressMagnitude;
            bool stressVectorChanged = RecalculateStressDisplacementVectors(dipShearStressMagnitude, strikeShearStressMagnitude, out shearStressMagnitude);

            // Determine the new fracture mode, and flag if it has changed. This will be:
            // - Mode 1 if the normal stress on the fracture is tensile or zero (i.e. the fracture is dilatant)
            // - Mode 2 if the normal stress on the fracture is compressive (i.e. the fracture is shear only) and the magnitude of the dip-slip shear stress is greater than the magnitude of the strike-slip shear stress
            // - Mode 3 if the normal stress on the fracture is compressive (i.e. the fracture is shear only) and the magnitude of the dip-slip shear stress is less than the magnitude of the strike-slip shear stress
            // Note that a Mode 1 fracture may still have an element of shear displacement, if it is inclined or oblique to the principal stress orientations
            FractureMode newMode;
            if (normalStressMagnitude <= 0)
                newMode = FractureMode.Mode1;
            else if (Math.Abs(dipShearStressMagnitude) >= Math.Abs(strikeShearStressMagnitude))
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
            else if ((float)shearStressMagnitude - (float)(gbc.MechProps.MuFr * normalStressMagnitude) >= -PreviousFractureData.MaxDrivingStressRoundingError)
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
            ShearStressMagnitude = (DipShearStressMagnitude * VectorXYZ.Sin_trim(newShearStressPitch)) + (StrikeShearStressMagnitude * (VectorXYZ.Cos_trim(newShearStressPitch)));
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
                shearDisplacementVector = new VectorXYZ(0, 0, 0);
                DisplacementPitch = double.NaN;
            }
            else
            {
                double nu_r = gbc.MechProps.Nu_r;
                shearStressVector = (VectorXYZ.Sin_trim(newShearStressPitch) * dipVector) + (VectorXYZ.Cos_trim(newShearStressPitch) * fs.StrikeVector);
                DisplacementPitch = Math.Atan2(DipShearStressMagnitude, (StrikeShearStressMagnitude / (1 - nu_r)));
                // NB The displacement vector will not be a unit length vector
                // This is necessary to get the correct results when calculating frictional traction and compliance for strike-slip fractures
                shearDisplacementVector = (VectorXYZ.Sin_trim(newShearStressPitch) * dipVector) + ((VectorXYZ.Cos_trim(newShearStressPitch) / (1 - nu_r)) * fs.StrikeVector);
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
                                Maa = sindip;
                                Mas = 0;
                                Mss = sindip / 2;
                            }
                            else
                            {
                                double oneMinus2Nur = 1 - (2 * gbc.MechProps.Nu_r);
                                Mff = Math.Pow(oneMinusNur, 2) / oneMinus2Nur;
                                Mfw = 0;
                                Mww = oneMinusNur / 2;
                                Mfs = 0;
                                Mss = 1 / 2;
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
                            Tensor4_2Sx2S MF_frictional_traction = (normalVector ^ shearDisplacementVector) ^ normal_OP_mu_normal;
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
                                sinpitch = VectorXYZ.Sin_trim(ShearStressPitch);
                                cospitch = VectorXYZ.Cos_trim(ShearStressPitch);
                            }
                            if (BiazimuthalConjugate)
                            {
                                Maa = cosdip * (sindip * cosdip - (mufr * sinpitch * Math.Pow(sindip, 2)));
                                Mas = -(mufr / oneMinusNur / 2) * cospitch * Math.Pow(sindip, 2);
                                Mss = sindip / 2;
                            }
                            else
                            {
                                double oneMinus2Nur = 1 - (2 * gbc.MechProps.Nu_r);
                                Mff = 0;
                                Mfw = -(Math.Pow(oneMinusNur, 2) / (2 * oneMinus2Nur)) * mufr * sinpitch;
                                Mww = oneMinusNur / 2;
                                Mfs = -(oneMinusNur / (2 * oneMinus2Nur)) * mufr * cospitch;
                                Mss = 1 / 2;
                            }
                        }
                        break;
                    default:
                        {
                            // The compliance tensor bases will contain only zero values
                            uF_ComplianceTensorBase = new Tensor4_2Sx2S();
                            MF_ComplianceTensorBase = new Tensor4_2Sx2S();

                            // Set all mode factors to zero
                            if (BiazimuthalConjugate)
                            {
                                Maa = 0;
                                Mas = 0;
                                Mss = 0;
                            }
                            else
                            {
                                Mff = 0;
                                Mfw = 0;
                                Mww = 0;
                                Mfs = 0;
                                Mss = 0;
                            }
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
                // Set all mode factors to zero
                if (BiazimuthalConjugate)
                {
                    Maa = 0;
                    Mas = 0;
                    Mss = 0;
                }
                else
                {
                    Mff = 0;
                    Mfw = 0;
                    Mww = 0;
                    Mfs = 0;
                    Mss = 0;
                }
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
                if (sindip > 0)
                {
                    uF_fractureDensityMultiplier /= sindip;
                    MF_fractureDensityMultiplier /= sindip;
                }
                return elasticityMultiplier * ((uF_fractureDensityMultiplier * uF_ComplianceTensorBase) + (MF_fractureDensityMultiplier * MF_ComplianceTensorBase));
            }
        }
        // We may want to use the present day stress to calculate fracture aperture and reactivation risk, rather than the stress at the time of fracture development
        // In this case we will use a supplied effective stress tensor to calculate the present day stress on the fracture
        /// <summary>
        /// Present day effective normal stress acting on the fracture
        /// </summary>
        private double PresentDaySigmaNeff { get; set; }
        /// <summary>
        /// Present day shear stress acting on the fracture
        /// </summary>
        private double PresentDayTau { get; set; }
        /// <summary>
        /// Present day fracture reactivition potential
        /// This will return the driving stress if that is positive; however it will return a negative value representing the cohesionless distance to failure if the fracture is closed and not critically stressed
        /// </summary>
        public double PresentDayReactivationPotential { get { return Math.Max(PresentDayDilatancyPotential, PresentDaySlipPotential); } }
        /// <summary>
        /// Present day fracture dilatancy potential
        /// This will return the inverse of the normal effective stress on the fracture, representing the dilatant driving stress if positive, and the cohesionless distance to dilational failure if it is negative
        /// </summary>
        public double PresentDayDilatancyPotential
        {
            get
            {
                // If the fractures are dilatant (i.e. the normal stress on them is tensile), the dilatancy protential is positive and equal to the total stress (normal and shear) on the fracture
                if (PresentDaySigmaNeff <= 0)
                    return Math.Sqrt(Math.Pow(PresentDaySigmaNeff, 2) + Math.Pow(PresentDayTau, 2));
                // If the fractures are closed, the dilatancy potential will be the inverse of the (compressive) normal effective stress on the fracture
                else
                    return -PresentDaySigmaNeff;
            }
        }
        /// <summary>
        /// Present day fracture slip potential
        /// This represents the shear driving stress if that is positive, and the cohesionless distance to shear failure if it is negative
        /// </summary>
        public double PresentDaySlipPotential
        {
            get
            {
                // If the fractures are dilatant (i.e. the normal stress on them is tensile), we do not need to take into account friction
                // Therefore the slip potential is equal to the maximum shear stress
                if (PresentDaySigmaNeff <= 0)
                    return PresentDayTau;
                // If the fractures are closed, the shear driving stress and slip potential will equal the shear stress on the fractures minus the frictional traction
                return PresentDayTau - (gbc.MechProps.MuFr * PresentDaySigmaNeff);
            }
        }
        /// <summary>
        /// Present day fracture slip tendency
        /// This represents the minimum frictional coefficient required to prevent slip on a cohesionless fracture, i.e. shear stress / normal stress
        /// If the fractures are dilatant (i.e. the normal stress on them is tensile), the slip tendency is undefined and will return NaN 
        /// </summary>
        public double PresentDaySlipTendency
        {
            get
            {
                if (PresentDaySigmaNeff > 0)
                    return PresentDayTau / PresentDaySigmaNeff;
                else
                    return double.NaN;
            }
        }
        /// <summary>
        /// This represents the fracture mode closest to failure, or with the highest driving stress if the fracture is critical
        /// </summary>
        public FractureMode MostLikelyReactivationMode { get; private set; }
        /// <summary>
        /// Present day driving stress acting on the fracture
        /// </summary>
        public double PresentDayDrivingStress { get { return (PresentDayReactivationPotential > 0 ? PresentDayReactivationPotential : 0); } }
        /// <summary>
        /// Flag specifying whether to use present day stress or stress at the time of fracture development to calculate fracture aperture and reactivation risk
        /// </summary>
        private bool usePresentDayStress;
        /// <summary>
        /// Call this function to use the present day stress to calculate fracture aperture and reactivation risk, rather than the stress at the time of fracture development  
        /// </summary>
        /// <param name="PresentDayEffectiveStress">Tensor2S object to specify the present day effective stress; if null, the stress at the time of fracture development will be used to calculate fracture aperture and reactivation risk</param>
        public void UsePresentDayStress(Tensor2S PresentDayEffectiveStress)
        {
            if (PresentDayEffectiveStress is null) // Set Present Day normal and driving stress to NaN
            {
                usePresentDayStress = false;
                PresentDaySigmaNeff = double.NaN;
                PresentDayTau = double.NaN;
            }
            else // Set Present Day normal and driving stress based on the supplied effective stress tensor
            {
                usePresentDayStress = true;

                // Get stress vector acting on the fracture plane
                VectorXYZ stressOnFracture = PresentDayEffectiveStress * normalVector;

                // Calculate the magnitude of normal stress on the fracture plane, and the shear stresses acting on the fracture plane in the along-strike and downdip directions respectively
                // The maximum present day shear stress Tau can be calculated by taking the root of the squares of the orthogonal strike and downdip shear stress components
                PresentDaySigmaNeff = normalVector & stressOnFracture;
                double presentDayTauDip = dipVector & stressOnFracture;
                double presentDayTauStrike = fs.StrikeVector & stressOnFracture;
                PresentDayTau = Math.Sqrt(Math.Pow(presentDayTauDip, 2) + Math.Pow(presentDayTauStrike, 2));

                // Calculate the most likely reactivation mode
                if (PresentDaySigmaNeff <= 0)
                    MostLikelyReactivationMode = FractureMode.Mode1;
                else
                    MostLikelyReactivationMode = (presentDayTauDip >= presentDayTauStrike ? FractureMode.Mode2 : FractureMode.Mode3);
            }
        }

        // Fracture porosity values
        /// <summary>
        /// Total porosity of all microfractures
        /// </summary>
        /// <returns></returns>
        public double Total_uF_Porosity()
        {
            return Total_uF_Porosity(gbc.PropControl.FractureApertureControl);
        }
        /// <summary>
        /// Cumulative porosity of all microfractures with radius greater than the specified radius array index
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double Cumulative_uF_Porosity(int index)
        {
            return Cumulative_uF_Porosity(index, gbc.PropControl.FractureApertureControl);
        }
        /// <summary>
        /// Total porosity of all half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double Total_MF_Porosity()
        {
            return Total_MF_Porosity(gbc.PropControl.FractureApertureControl);
        }
        /// <summary>
        /// Cumulative porosity of all half-macrofractures with half-length greater than the specified half-length array index
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double Cumulative_MF_Porosity(int index)
        {
            return Cumulative_MF_Porosity(index, gbc.PropControl.FractureApertureControl);
        }
        /// <summary>
        /// Total porosity of the entire fracture dip set
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Total_Fracture_Porosity()
        {
            return Total_uF_Porosity() + Total_MF_Porosity();
        }
        /// <summary>
        /// Total porosity of all microfractures, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Total_uF_Porosity(FractureApertureType ApertureControl)
        {
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_uFP32_total() + s_uFP32_total()) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_uFP33_total() + s_uFP33_total()) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = (a_uFP33_total() + s_uFP33_total()) * gbc.MechProps.DynamicApertureMultiplier * (4 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = (a_uFP32_total() + s_uFP32_total()) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_uFP32(index) + s_uFP32(index)) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_uFP33(index) + s_uFP33(index)) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = (a_uFP33(index) + s_uFP33(index)) * gbc.MechProps.DynamicApertureMultiplier * (4 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = (a_uFP32(index) + s_uFP32(index)) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_MFP32_total() + s_MFP32_total()) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_MFP33_total() + s_MFP33_total()) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = (a_MFP33_total() + s_MFP33_total()) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = (a_MFP32_total() + s_MFP32_total()) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = (a_MFP32(index) + s_MFP32(index)) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (a_MFP33(index) + s_MFP33(index)) * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = (a_MFP33(index) + s_MFP33(index)) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = (a_MFP32(index) + s_MFP32(index)) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
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

        // Fracture permeability tensors
        // NB These functions return the uncorrected fracture peremabilities, based on based on the Oda (1986) model, which assumes fractures of infinite size and connectivity
        // Corrections for fracture length and connectivity must take into account other fracture sets and the host rock, so are made at the gridblock level
        /// <summary>
        /// Get the uncorrected permeability tensor for all current microfractures in this dipset
        /// This is based on the Oda (1986) model and assumes fractures of infinite size and connectivity
        /// </summary>
        /// <returns>Tensor2S object representing the uncorrected microfracture permeability</returns>
        public Tensor2S Total_uF_Permeability()
        {
            return Total_uF_Permeability(-1);
        }
        /// <summary>
        /// Get the uncorrected permeability tensor for all microfractures in this dipset, at the end of a specified previous timestep
        /// This is based on the Oda (1986) model and assumes fractures of infinite size and connectivity
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the uncorrected microfracture permeability</returns>
        public Tensor2S Total_uF_Permeability(int Timestep_M)
        {
            bool useCurrentDensityData = (Timestep_M < 0);
            bool useCurrentApertureData = useCurrentDensityData || usePresentDayStress;

            double geometryMultiplier = 1d / 12d;
            double apertureMultiplier;
            double densityMultiplier;
            switch (gbc.PropControl.FractureApertureControl)
            {
                // In the Uniform and Barton Bandis fracture aperture scenarios, aperture is uniform across the fracture
                // The permeability will therefore be proportional to the cube of the mean aperture
                case FractureApertureType.Uniform:
                case FractureApertureType.BartonBandis:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMeanMicrofractureAperture(1) : getMeanMicrofractureAperture(1, Timestep_M), 3);
                    densityMultiplier = useCurrentDensityData ? a_uFP32_total() + s_uFP32_total() : getTotaluFP32(Timestep_M);
                    break;
                // In the Size Dependent and Dynamic fracture aperture scenarios, aperture follows an elliptical profile
                // The aperture multiplier is calculated by integrating the cube of the local aperture across every fracture
                // This is therefore proportional to the 5th power of the fracture radii, so we must use the P35 value
                // The P35 value also incorporates the fracture area, so will be included in the density multiplier
                case FractureApertureType.SizeDependent:
                case FractureApertureType.Dynamic:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMaximumMicrofractureAperture(1) : getMaximumMicrofractureAperture(1, Timestep_M), 3);
                    densityMultiplier = (useCurrentDensityData ? a_uFP35_total() + s_uFP35_total() : getTotaluFP35(Timestep_M)) / 8;
                    break;
                // Aperture is not defined
                default:
                    apertureMultiplier = 0;
                    densityMultiplier = 0;
                    break;
            }
            densityMultiplier /= sindip;

            Tensor2S permTensor = Tensor2S.BiaxialTensor(normalVector, geometryMultiplier * apertureMultiplier * densityMultiplier);
            // If the fracture set is biazimuthally conjugate, the YZ and ZX components of the permeability tensor should be 0
            if (BiazimuthalConjugate)
            {
                permTensor.Component(Tensor2SComponents.YZ, 0);
                permTensor.Component(Tensor2SComponents.ZX, 0);
            }
            return permTensor;
        }
        /// <summary>
        /// Calculate the ratio of mean permeability of the fracture-controlled fault block to host rock permeability for a flat square fracture of uniform aperture
        /// </summary>
        /// <param name="fracPermRatio">Ratio of fracture permeability (aperture^3 / 12) to host rock permeability times fracture-controlled flow block width</param>
        /// <returns></returns>
        private double SquareFracturePermeabilityMultiplier(double fracPermRatio)
        {
            return fracPermRatio + 1;
        }
        /// <summary>
        /// Calculate the ratio of mean permeability of the fracture-controlled fault block to host rock permeability for a flat circular fracture of uniform aperture
        /// </summary>
        /// <param name="fracPermRatio">Ratio of fracture permeability (aperture^3 / 12) to host rock permeability times fracture-controlled flow block width</param>
        /// <returns></returns>
        private double DiscFracturePermeabilityMultiplier(double fracPermRatio)
        {
            return ((2d / 3d) * fracPermRatio) + 1;
        }
        /// <summary>
        /// Calculate the ratio of mean permeability of the fracture-controlled fault block to host rock permeability for a spheroidal fracture with maximum aperture in the centre
        /// </summary>
        /// <param name="fracPermRatio">Ratio of maximum fracture permeability (maximum aperture^3 / 12) to host rock permeability times fracture-controlled flow block width</param>
        /// <returns></returns>
        private double SpheroidalFracturePermeabilityMultiplier(double fracPermRatio)
        {
            const double powerCoefficient = 0.95;
            const double constantFactor = 1 / (powerCoefficient * powerCoefficient);
            return powerCoefficient * Math.Sqrt(fracPermRatio * constantFactor);
        }
        /// <summary>
        /// Get the corrected permeability tensor for all current microfractures in this dipset
        /// This assumes microfractures are unconnected and takes into account microfracture size distribution
        /// </summary>
        /// <returns>Tensor2S object representing the uncorrected microfracture permeability</returns>
        public Tensor2S Total_uF_Permeability_Corrected()
        {
            return Total_uF_Permeability_Corrected(-1);
        }
        /// <summary>
        /// Get the corrected permeability tensor for all microfractures in this dipset, at the end of a specified previous timestep
        /// This assumes microfractures are unconnected and takes into account microfracture size distribution
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the uncorrected microfracture permeability</returns>
        public Tensor2S Total_uF_Permeability_Corrected(int Timestep_M)
        {
            bool useCurrentDensityData = (Timestep_M < 0);
            bool useCurrentApertureData = useCurrentDensityData || usePresentDayStress;

            // Create and populate a local array for the cumulative uFP30 density distribution function
            double[] DuFP30 = useCurrentDensityData ? T_uFDP30() : getDuFP30_distribution(Timestep_M);
            int no_rbins = Math.Min(uF_radii.Length, DuFP30.Length);
            double rmin = gbc.PropControl.minImplicitMicrofractureRadius;
            int min_bin = no_rbins - 1;
            double dipset_uFP32 = useCurrentDensityData ? a_uFP32_total() + s_uFP32_total() : getTotaluFP32(Timestep_M);
            double set_uFP32 = useCurrentDensityData ? fs.combined_T_uFP32_total() : fs.combined_T_uFP32_total(Timestep_M);
            while ((min_bin > 0) && (uF_radii[min_bin] > rmin))
                min_bin--;

            // Get the fracture multipliers
            // For now this calculation assumes square fractures of uniform (although potentially size-dependent) aperture
            double geometryMultiplier = 1d / 12d;
            // Get the fracture aperture type and permeability
            // If the fracture aperture is uniform and independent of fracture size, the fracture permeability will be a constant kf
            // Otherwise we will get the permeability in the centre of a fracture of unit radius kf'
            FractureApertureType apertureType = gbc.PropControl.FractureApertureControl;
            double apertureMultiplier;
            if ((apertureType == FractureApertureType.Uniform) || (apertureType == FractureApertureType.BartonBandis))
                apertureMultiplier = Math.Pow(useCurrentApertureData ? getMeanMicrofractureAperture(1) : getMeanMicrofractureAperture(1, Timestep_M), 3);
            else
                apertureMultiplier = Math.Pow(useCurrentApertureData ? getMaximumMicrofractureAperture(1) : getMaximumMicrofractureAperture(1, Timestep_M), 3);
            double k_fmax = geometryMultiplier * apertureMultiplier;
            // Host rock permeability
            // For now we will assume host rock permeability is isotropic and ignore host rock kv
            double k_h = gbc.MechProps.HostRock_kh;
            // Flow pipe geometry
            // The flow pipe width multiplier is the ratio of the width of the fracture-controlled flow block (perpendicular to both the fracture and the flow direction) to the fracture diameter
            // This represents the width of the zone where fluid can transfer between the fracture and the host rock
            double flowPipeWidthMultiplier = 1;
            // The flow pipe length is the mean distance until the fluid will enter another fracture-controlled flow block
            // NB this is calculated from the uFP32 value for the entire fracture set, not just this dipset
            // This allows fluid to switch to the pipe of any other microfracture in the fracture set, not just microfractures in this dipset, when it reaches the end of the current pipe
            double flowPipeLength = 1 / (flowPipeWidthMultiplier * set_uFP32);

            // Calculate total permeability (including microfractures and host rock)
            // We assume flow through the pipes is in parallel - i.e. fluid can switch between different pipes at the end of each pipe (including pipes from other dipsets in this fracture set)
            // We therefore take an arithmetic average of the permeability of each pipe, weighted by the cross-sectional area of the pipes (i.e. the duFP32 for the microfracture size bin)
            // Loop through all the size bins and calculate permeability parallel to the fracture
            double cum_perm = 0;
            for (int r_bin = min_bin; r_bin < no_rbins; r_bin++)
            {
                // Get geometric information for this size bin
                double radius = uF_radii[r_bin];
                if (radius < rmin)
                    radius = rmin;
                double fracDiameter = 2 * radius;
                double DP32_bin = DuFP30[r_bin] * Math.PI * radius * radius;

                // Get the fracture permeability multiplier for this size bin
                // This represents the ratio of mean permeability of the fracture-controlled fault block to host rock permeability
                double fracPermeabilityMultiplier;
                if ((apertureType == FractureApertureType.Uniform) || (apertureType == FractureApertureType.BartonBandis))
                {
                    double fracPermRatio = k_fmax / (flowPipeWidthMultiplier * fracDiameter * k_h);
                    fracPermeabilityMultiplier = DiscFracturePermeabilityMultiplier(fracPermRatio);
                }
                else
                {
                    double fracPermRatio = (k_fmax * fracDiameter * fracDiameter) / (flowPipeWidthMultiplier * k_h);
                    fracPermeabilityMultiplier = SpheroidalFracturePermeabilityMultiplier(fracPermRatio);
                }

                // If the fracture diameter is shorter than the flow pipe length, calculate permeability of series flow through the fracture and the unfractured pipe
                double cum_perm_increment;
                if (fracDiameter < flowPipeLength)
                {
                    double fractureControlledFlowBlockResistance = fracDiameter / (fracPermeabilityMultiplier * k_h);
                    double unfracturedPipeResistance = (flowPipeLength - fracDiameter) / k_h;
                    double kmean_FlowPipe = flowPipeLength / (fractureControlledFlowBlockResistance + unfracturedPipeResistance);
                    cum_perm_increment = DP32_bin * kmean_FlowPipe;
                }
                // Otherwise calculate the permeability of parallel flow through the pipe and adjacent overlapping fractures
                else
                {
                    double noFracsInPipe = fracDiameter / flowPipeLength;
                    double kmean_FlowPipe = (noFracsInPipe * fracPermeabilityMultiplier * k_h) - ((noFracsInPipe - 1) * k_h);
                    cum_perm_increment = DP32_bin * kmean_FlowPipe;
                }
                if (!double.IsNaN(cum_perm_increment))
                {
                    cum_perm += cum_perm_increment;
                }
            }
            // Calculate the average permeability
            // NB this is calculated from the uFP32 value for the entire fracture set, not just this dipset
            // This allows fluid to switch to the pipe of any other microfracture in the fracture set, not just microfractures in this dipset, when it reaches the end of the current pipe
            double k_tot = cum_perm / set_uFP32;
            // We must subtract the host rock permeability, since this is included in the calculation
            k_tot -= (k_h * (dipset_uFP32 / set_uFP32));
            if (k_tot < 0)
                k_tot = 0;

            // Create a biaxial permeability tensor for the fracture set
            Tensor2S permTensor = Tensor2S.BiaxialTensor(normalVector, k_tot);
            // If the fracture set is biazimuthally conjugate, the YZ and ZX components of the permeability tensor should be 0
            if (BiazimuthalConjugate)
            {
                permTensor.Component(Tensor2SComponents.YZ, 0);
                permTensor.Component(Tensor2SComponents.ZX, 0);
            }
            return permTensor;
        }
        /// <summary>
        /// Get the uncorrected permeability tensor for all current half-macrofractures in this dipset
        /// This is based on the Oda (1986) model and assumes fractures of infinite size and connectivity
        /// </summary>
        /// <returns>Tensor2S object representing the uncorrected macrofracture permeability</returns>
        public Tensor2S Total_MF_Permeability()
        {
            return Total_MF_Permeability(-1);
        }
        /// <summary>
        /// Get the uncorrected permeability tensor for all half-macrofractures in this dipset, at the end of a specified previous timestep
        /// This is based on the Oda (1986) model and assumes fractures of infinite size and connectivity
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the uncorrected macrofracture permeability</returns>
        public Tensor2S Total_MF_Permeability(int Timestep_M)
        {
            bool useCurrentDensityData = (Timestep_M < 0);
            bool useCurrentApertureData = useCurrentDensityData || usePresentDayStress;

            double geometryMultiplier = 1d / 12d;
            double apertureMultiplier;
            switch (gbc.PropControl.FractureApertureControl)
            {
                // In the Uniform and Barton Bandis fracture aperture scenarios, aperture is uniform across the fracture
                // The permeability will therefore be proportional to the cube of the mean aperture
                case FractureApertureType.Uniform:
                case FractureApertureType.BartonBandis:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMeanMacrofractureAperture() : getMeanMacrofractureAperture(Timestep_M), 3);
                    break;
                // In the Size Dependent and Dynamic fracture aperture scenarios, aperture follows an elliptical profile
                // The aperture multiplier must therefore be calculated by integrating the cube of the local aperture across the fracture
                case FractureApertureType.SizeDependent:
                case FractureApertureType.Dynamic:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMaximumMacrofractureAperture() : getMaximumMacrofractureAperture(Timestep_M), 3) * (3 * Math.PI / 16);
                    break;
                // Aperture is not defined
                default:
                    apertureMultiplier = 0;
                    break;
            }
            double densityMultiplier = useCurrentDensityData ? a_MFP32_total() + s_MFP32_total() : getTotalMFP32(Timestep_M);
            densityMultiplier /= sindip;

            Tensor2S permTensor = Tensor2S.BiaxialTensor(normalVector, geometryMultiplier * apertureMultiplier * densityMultiplier);
            // If the fracture set is biazimuthally conjugate, the YZ and ZX components of the permeability tensor should be 0
            if (BiazimuthalConjugate)
            {
                permTensor.Component(Tensor2SComponents.YZ, 0);
                permTensor.Component(Tensor2SComponents.ZX, 0);
            }
            return permTensor;
        }
        /// <summary>
        /// Get the corrected permeability tensor for all current half-macrofractures in this dipset, taking into account the mean length and connectivity of macrofracture segments
        /// This takes into account half-macrofracture connectivity and size distribution
        /// </summary>
        /// <returns>Tensor2S object representing the uncorrected macrofracture permeability</returns>
        public Tensor2S Total_MF_Permeability_Corrected()
        {
            return Total_MF_Permeability_Corrected(-1);
        }
        /// <summary>
        /// Get the corrected permeability tensor for all half-macrofractures in this dipset, taking into account the mean length and connectivity of macrofracture segments, at the end of a specified previous timestep
        /// This takes into account half-macrofracture connectivity and size distribution
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the uncorrected macrofracture permeability</returns>
        public Tensor2S Total_MF_Permeability_Corrected(int Timestep_M)
        {
            bool useCurrentDensityData = (Timestep_M < 0);
            bool useCurrentApertureData = useCurrentDensityData || usePresentDayStress;
            // If this flag is true, we will calculate the permeability of the fracture network assuming the gridblock size is infinite
            // In this case the chains of fractures through which fluid flows are unlimited in length; flow will be restricted to the chain until it reaches a branch point
            // If this flag is false, we will calculate the permeability of the fracture network within a single gridblock
            // In this case the chains of fractures through which fluid flows cannot be longer than the gridblock
            // This will tend to give a higher permebility as fluid is less likely to be forced through unconnected nodes (I and soft-linked R nodes) where it relies on matrix permeability
            bool extendToInfiniteSize = true;

            // Calculate the fracture density, aperture and geometric multipliers
            // These are independent of the fracture orientation and connectivity
            double geometryMultiplier = 1d / 12d;
            double apertureMultiplier;
            double relayApertureMultiplier;
            FractureDipSet relayDipSet = gbc.getClosestFractureSet(fs, fs.Strike + (Math.PI / 2)).FractureDipSets[0];
            switch (gbc.PropControl.FractureApertureControl)
            {
                // In the Uniform and Barton Bandis fracture aperture scenarios, aperture is uniform across the fracture
                // The permeability will therefore be proportional to the cube of the mean aperture
                case FractureApertureType.Uniform:
                case FractureApertureType.BartonBandis:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMeanMacrofractureAperture() : getMeanMacrofractureAperture(Timestep_M), 3);
                    relayApertureMultiplier = Math.Pow(useCurrentApertureData ? relayDipSet.getMeanMacrofractureAperture() : relayDipSet.getMeanMacrofractureAperture(Timestep_M), 3);
                    break;
                // In the Size Dependent and Dynamic fracture aperture scenarios, aperture follows an elliptical profile
                // The aperture multiplier must therefore be calculated by integrating the cube of the local aperture across the fracture
                case FractureApertureType.SizeDependent:
                case FractureApertureType.Dynamic:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMaximumMacrofractureAperture() : getMaximumMacrofractureAperture(Timestep_M), 3) * (3 * Math.PI / 16);
                    relayApertureMultiplier = Math.Pow(useCurrentApertureData ? relayDipSet.getMaximumMacrofractureAperture() : relayDipSet.getMaximumMacrofractureAperture(Timestep_M), 3) * (3 * Math.PI / 16);
                    break;
                // Aperture is not defined
                default:
                    apertureMultiplier = 0;
                    relayApertureMultiplier = 1;
                    break;
            }
            double dipset_MFP32 = useCurrentDensityData ? a_MFP32_total() + s_MFP32_total() : getTotalMFP32(Timestep_M);
            double set_MFP32 = useCurrentDensityData ? fs.combined_T_MFP32_total() : fs.combined_T_MFP32_total(Timestep_M);
            double densityMultiplier = dipset_MFP32 / sindip;
            double kf_singlefrac = geometryMultiplier * apertureMultiplier;
            double kf_dipset = kf_singlefrac * densityMultiplier;

            // If the P32 fracture density is zero or the fracture aperture is zero, the permeability tensor will be zero so we can return a null tensor immediately
            if (kf_dipset == 0)
                return new Tensor2S();

            // Calculate the geometric and connectivity indices
            double sinStrike = Math.Abs(VectorXYZ.Sin_trim(fs.Strike));
            double cosStrike = Math.Abs(VectorXYZ.Cos_trim(fs.Strike));
            double kf_kh = kf_singlefrac / gbc.MechProps.HostRock_kh;
            double kf_kr = apertureMultiplier / relayApertureMultiplier;
            double unconnectedTipRatio, hardLinkedRelayTipRatio, softLinkedRelayTipRatio, connectedTipRatio, connectedSideRatio;
            double meanLength, meanRelayOffset, meanNonRelayOffset;
            if (useCurrentDensityData)
            {
                double terminatingFracturesPerMF = getTerminatingFracturesPerMF();
                double TotalNodes = 1 + terminatingFracturesPerMF;

                unconnectedTipRatio = UnconnectedTipRatio(false) / TotalNodes;
                if (gbc.LinkFracturesInStressShadow)
                {
                    softLinkedRelayTipRatio = 0;
                    hardLinkedRelayTipRatio = RelayTipRatio(false) / TotalNodes;
                }
                else
                {
                    softLinkedRelayTipRatio = RelayTipRatio(false) / TotalNodes;
                    hardLinkedRelayTipRatio = 0;
                }
                connectedTipRatio = IntersectingTipRatio(false) / TotalNodes;
                connectedSideRatio = terminatingFracturesPerMF / TotalNodes;
                meanLength = Mean_MF_HalfLength() / (0.5 + terminatingFracturesPerMF);
                meanRelayOffset = getWeightedAverage_StressShadowWidth() / 2;
                // NB the non-relay offset is based on the MFP32 value for the entire fracture set, not just this dipset
                // This allows fluid to switch to any other macrofracture in the fracture set, not just macrofractures in this dipset, when it reaches an I or Y node
                meanNonRelayOffset = 1 / set_MFP32;
            }
            else
            {
                double terminatingFractureDensity = getTerminatingFractureDensity(Timestep_M);
                double terminatingFracturesPerMF = getTerminatingFracturesPerMF();
                double INodes = getActiveMFP30(Timestep_M);
                double RNodes = getStaticRelayMFP30(Timestep_M);
                double YNodes = getStaticIntersectMFP30(Timestep_M);
                double TotalNodes = INodes + RNodes + YNodes + terminatingFractureDensity;
                unconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : 1);
                if (gbc.LinkFracturesInStressShadow)
                {
                    softLinkedRelayTipRatio = 0;
                    hardLinkedRelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : 0);
                }
                else
                {
                    softLinkedRelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : 0);
                    hardLinkedRelayTipRatio = 0;
                }
                connectedTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : 0);
                connectedSideRatio = (TotalNodes > 0 ? terminatingFractureDensity / TotalNodes : 0);
                double MFP30_Thickness = TotalNodes * gbc.ThicknessAtDeformation;
                meanLength = (MFP30_Thickness > 0 ? (dipset_MFP32 / MFP30_Thickness) / (0.5 + terminatingFracturesPerMF) : 0);
                meanRelayOffset = getWeightedAverage_StressShadowWidth(Timestep_M) / 2;
                // NB the non-relay offset is based on the MFP32 value for the entire fracture set, not just this dipset
                // This allows fluid to switch to any other macrofracture in the fracture set, not just macrofractures in this dipset, when it reaches an I or Y node
                meanNonRelayOffset = 1 / set_MFP32;
            }

            // Calculate the length multipliers for different fracture node types
            double LRN_xx = meanLength * sinStrike * sinStrike;
            double LRN_yy = meanLength * cosStrike * cosStrike;
            double LRN_xy = meanLength * sinStrike * cosStrike;
            double LIT_xx = LRN_xx + Math.Abs(meanNonRelayOffset * sinStrike * cosStrike);
            double LIT_yy = LRN_yy + Math.Abs(meanNonRelayOffset * sinStrike * cosStrike);
            double LIT_xy = LRN_xy - (Math.Sign(sinStrike * cosStrike) * meanNonRelayOffset);

            // Calculate the resistivity multipliers for different fracture node types
            double RI;
            if (meanNonRelayOffset >= kf_kh)
                RI = double.PositiveInfinity;
            else if ((meanNonRelayOffset / Math.Sqrt(1 - (meanNonRelayOffset / kf_kh))) < meanLength)
                RI = meanLength + (2 * kf_kh * Math.Sqrt(1 - (meanNonRelayOffset / kf_kh)));
            else
                RI = kf_kh * ((meanNonRelayOffset / meanLength) + (meanLength / meanNonRelayOffset));
            double RRs;
            if (meanRelayOffset >= kf_kh)
                RRs = double.PositiveInfinity;
            else if ((meanRelayOffset / Math.Sqrt(1 - (meanRelayOffset / kf_kh))) < meanLength)
                RRs = meanLength + (2 * kf_kh * Math.Sqrt(1 - (meanRelayOffset / kf_kh)));
            else
                RRs = kf_kh * ((meanRelayOffset / meanLength) + (meanLength / meanRelayOffset));
            double RRh = meanLength + (meanRelayOffset > 0 ? meanRelayOffset * kf_kr : 0);
            double RT = meanLength + (meanNonRelayOffset > 0 ? meanNonRelayOffset * kf_kr : 0);
            double RN = meanLength;

            // Since all relay nodes are either all soft-linked or all hard-linked, we can simplify the calculation by considering only one type
            double relayTipRatio, RR;
            if (gbc.LinkFracturesInStressShadow)
            {
                relayTipRatio = hardLinkedRelayTipRatio;
                RR = RRh;
            }
            else
            {
                relayTipRatio = softLinkedRelayTipRatio;
                RR = RRs;
            }

            // Calculate the overall permeability multipliers for the horizontal tensor components
            double kxx_multiplier, kyy_multiplier, kxy_multiplier;
            const double P_cutoff = 0.99;

            // If there are no junction nodes, then we must calculate the permeability multipliers for indefinite series flow through I and R nodes
            if ((unconnectedTipRatio + relayTipRatio ) > P_cutoff)
            {
                double RRI = (unconnectedTipRatio > 0 ? unconnectedTipRatio * RI : 0) + (relayTipRatio > 0 ? relayTipRatio * RR : 0);
                kxx_multiplier = ((unconnectedTipRatio * LIT_xx) + (relayTipRatio * LRN_xx)) / RRI;
                kyy_multiplier = ((unconnectedTipRatio * LIT_yy) + (relayTipRatio * LRN_yy)) / RRI;
                kxy_multiplier = ((unconnectedTipRatio * LIT_xy) + (relayTipRatio * LRN_xy)) / RRI;
            }
            // If there are junction nodes, then we must calculate the weighted mean of the permeability multipliers for series flow through every possible chain of nodes ending in a junction node
            // In practice we can ignore the longer chains where the probability is very low
            else
            {
                // Set the maximum chain length
                int maxChainLength = 25;
                if (!extendToInfiniteSize)
                {
                    int maxChainLengthFromSize = (int)(gbc.MaxGridblockLength / meanLength);
                    if (maxChainLength > maxChainLengthFromSize) maxChainLength = maxChainLengthFromSize;
                }

                // We will start with the shortest chains and build upwards
                // When the cumulative probability of all chains so far exceeds the cutoff we will stop calculating, thus ignoring longer chains
                // This is necessary as otherwise chains could extend to infinite length
                double P_cumulative = 0;

                // Set the initial permeability multipliers to 0
                kxx_multiplier = 0;
                kyy_multiplier = 0;
                kxy_multiplier = 0;

                // We will loop through chains of nodes of increasing length
                // Each chain will consist of a series of 0 or more I or R nodes, followed by a single T or H node
                // The probability of each chain will therefore be given by a binomial distribution for the I and R nodes
                // We must therefore define and update a row of Pascal's triangle
                int[] PTriangle = new int[0] { };
                // NB noNodes is the number of I and R nodes in the chain; it does not include the final T or H node
                int noNodes = 0;
                do
                {
                    // Update Pascal's triangle for the new chain length
                    int[] PTriangle_nextRow = new int[noNodes + 1];
                    PTriangle_nextRow[0] = 1;
                    for (int m = 1; m < noNodes; m++)
                        PTriangle_nextRow[m] = PTriangle[m - 1] + PTriangle[m];
                    PTriangle_nextRow[noNodes] = 1;
                    PTriangle = PTriangle_nextRow;

                    // Loop through the possible number of R nodes in the chain
                    for (int noRnodes = 0; noRnodes <= noNodes; noRnodes++)
                    {
                        // The number of I nodes is the number of I and R nodes minus the number of R nodes
                        int noInodes = noNodes - noRnodes;

                        // Calculate the probability of the specified number of I and R nodes appearing in a chain (in any order)
                        // This includes the probability of the chain ending in either a T or H node
                        double P_IRchain = (double)PTriangle[noRnodes] * Math.Pow(unconnectedTipRatio, noInodes) * Math.Pow(relayTipRatio, noRnodes);
                        double P_chain = P_IRchain * (connectedTipRatio + connectedSideRatio);
                        P_cumulative += P_chain;

                        // Calculate the kxx, kyy and kxy multipliers for the current chain
                        double R_IR = (noInodes > 0 ? (double)noInodes * RI : 0) + (noRnodes > 0 ? (double)noRnodes * RR : 0);
                        double LIR_xx = (noInodes > 0 ? (double)noInodes * LIT_xx : 0) + (noRnodes > 0 ? (double)noRnodes * LRN_xx : 0);
                        double LIR_yy = (noInodes > 0 ? (double)noInodes * LIT_yy : 0) + (noRnodes > 0 ? (double)noRnodes * LRN_yy : 0);
                        double LIR_xy = (noInodes > 0 ? (double)noInodes * LIT_xy : 0) + (noRnodes > 0 ? (double)noRnodes * LRN_xy : 0);
                        double kIRT_xx = (LIR_xx + LIT_xx) / (R_IR + RT);
                        double kIRT_yy = (LIR_yy + LIT_yy) / (R_IR + RT);
                        double kIRT_xy = (LIR_xy + LIT_xy) / (R_IR + RT);
                        double kIRH_xx = (LIR_xx + LRN_xx) / (R_IR + RN);
                        double kIRH_yy = (LIR_yy + LRN_yy) / (R_IR + RN);
                        double kIRH_xy = (LIR_xy + LRN_xy) / (R_IR + RN);

                        // Increment the overall permeability multipliers
                        kxx_multiplier += (P_IRchain * ((connectedTipRatio * kIRT_xx) + (connectedSideRatio * kIRH_xx)));
                        kyy_multiplier += (P_IRchain * ((connectedTipRatio * kIRT_yy) + (connectedSideRatio * kIRH_yy)));
                        kxy_multiplier += (P_IRchain * ((connectedTipRatio * kIRT_xy) + (connectedSideRatio * kIRH_xy)));
                    }

                    // Increment the number of nodes
                    noNodes++;
                }
                while ((P_cumulative < P_cutoff) && (noNodes < maxChainLength));

                // Adjust the overall permeability multipliers to take account of the longer chains that were excluded from the calculation
                // We can do this by dividing by the cumulative probability of all the chains we have included
                kxx_multiplier /= P_cumulative;
                kyy_multiplier /= P_cumulative;
                kxy_multiplier /= P_cumulative;
            }

            // Calculate the overall permeability multipliers for the vertical tensor components
            double kzz_multiplier = sindip * sindip;
            // We will set the vertical shear components kyz and kzx to 0
            double kyz_multiplier = 0;
            double kzx_multiplier = 0;

            // We can now create a fracture permeability tensor using the tensor component multipliers
            // Unlike the Oda (1986) tensor, this calculation excludes the component of flow parallel to the fracture azimuth due to inclination of the fractures
            // This is valid as we can assume that fractures will not directly intersect other fractures in the same set, even if they are inclined
            double kxx = kxx_multiplier * kf_dipset;
            double kyy = kyy_multiplier * kf_dipset;
            double kzz = kzz_multiplier * kf_dipset;
            double kxy = kxy_multiplier * kf_dipset;
            double kyz = kyz_multiplier * kf_dipset;
            double kzx = kzx_multiplier * kf_dipset;
            Tensor2S permTensor = new Tensor2S(kxx, kyy, kzz, kxy, kyz, kzx);

            return permTensor;
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
        private double efffsd_e2d { get; set; }
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
                effd = 0;
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
        /// Fracture Mode Factor: azimuthal strain => azimuthal displacement
        /// </summary>
        private double Maa_eaa2d_eh2d { get { return Math.Max(BiazimuthalConjugate ? fs.eaa2d_eh2d * Maa : ((eff2d_e2d * Mff) + (efffwd_e2d * Mfw) + (efw2d_e2d * Mww)) / sindip, 0); } }
        /// <summary>
        /// Fracture Mode Factor: strike-parallel shear strain => azimuthal displacement
        /// </summary>
        private double Mas_eaaasd_eh2d { get { return Math.Max(BiazimuthalConjugate ? fs.eaaasd_eh2d * Mas : (efffsd_e2d * Mfs) / sindip, 0); } }
        /// <summary>
        /// Fracture Mode Factor: strike-parallel shear strain => strike-slip displacement
        /// </summary>
        private double Mss_eas2d_eh2d { get { return Math.Max(BiazimuthalConjugate ? fs.eas2d_eh2d * Mss : (efs2d_e2d * Mss) / sindip, 0); } }
        /// <summary>
        /// Fracture Mode Factor: maximum horizontal strain => horizontal displacement
        /// </summary>
        private double Mhh_eh2d { get { return Maa_eaa2d_eh2d + Mas_eaaasd_eh2d + Mss_eas2d_eh2d; } }
        // Geometric variables for calculating mode factors
        /// <summary>
        /// Geometric factor: azimuthal strain => azimuthal displacement
        /// </summary>
        private double Maa { get; set; }
        /// <summary>
        /// Geometric factor: azimuthal strain => along-strike displacement
        /// </summary>
        private double Mas { get; set; }
        /// <summary>
        /// Geometric factor: along-strike strain => along-strike displacement
        /// </summary>
        private double Mss { get; set; }
        /// <summary>
        /// Geometric factor: normal strain => normal displacement
        /// </summary>
        private double Mff { get; set; }
        /// <summary>
        /// Geometric factor: normal strain => down-dip displacement
        /// </summary>
        private double Mfw { get; set; }
        /// <summary>
        /// Geometric factor: normal strain => along-strike displacement
        /// </summary>
        private double Mfs { get; set; }
        /// <summary>
        /// Geometric factor: down-dip strain => down-dip displacement
        /// </summary>
        private double Mww { get; set; }

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
            double T_MFP30_thickness = (a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total()) * gbc.ThicknessAtDeformation;
            return (T_MFP30_thickness > 0 ? (a_MFP32_total() + s_MFP32_total()) / T_MFP30_thickness : 0);
        }

        // Connectivity indices
        /// <summary>
        /// Proportion of unconnected macrofracture tips - i.e. active macrofracture tips
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 1</param>
        /// <returns>Ratio of a_MFP30_total to T_MFP30_total</returns>
        public double UnconnectedTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 1;
            double T_MFP30 = a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total();
            return (T_MFP30 > 0 ? a_MFP30_total() / T_MFP30 : undefinedReturn);
        }
        /// <summary>
        /// Proportion of macrofracture tips connected to relay zones - i.e. static macrofracture tips deactivated due to stress shadow interaction
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sII_MFP30_total to T_MFP30_total</returns>
        public double RelayTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double T_MFP30 = a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total();
            return (T_MFP30 > 0 ? sII_MFP30_total() / T_MFP30 : undefinedReturn);
        }
        /// <summary>
        /// Proportion of intersecting macrofracture tips - i.e. static macrofracture tips deactivated due to intersection with orthogonal or oblique fractures
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sIJ_MFP30_total to T_MFP30_total</returns>
        public double IntersectingTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double T_MFP30 = a_MFP30_total() + sII_MFP30_total() + sIJ_MFP30_total();
            return (T_MFP30 > 0 ? sIJ_MFP30_total() / T_MFP30 : undefinedReturn);
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
        /// <param name="no_r_bins">Number of microfracture radius bins used to integrate uFP32 and uFP33 numerically</param>
        public void reset_uF_radii_array(int no_r_bins)
        {
            // Create a local radius array
            List<double> radii = new List<double>();

            // Cache useful variables locally
            double max_uF_radius = gbc.MaximumMicrofractureRadius;

            // Add new values to the array for all bin sizes
            // The first value is zero - this equates to the total microfracture density properties
            radii.Add(0);
            // Add a value for all intermediate bin sizes
            for (int r_bin = 1; r_bin < no_r_bins; r_bin++)
            {
                double rb_maxRad = ((double)r_bin / (double)no_r_bins) * max_uF_radius;
                radii.Add(rb_maxRad);
            }
            // Add a final value for the maximum size
            radii.Add(max_uF_radius);

            // Pass the array to the Microfractures object
            MicroFractures.ResetPopulationDistributionData(radii.ToArray());
        }
        /// <summary>
        /// Populate the macrofracture halflength index array based on the current halflengths of macrofractures that nucleated at timestep boundaries
        /// </summary>
        /// <param name="cullValue">Only calculate a value for every nth timestep; set to zero to use all timesteps</param>
        public void reset_MF_halflength_array(int cullValue)
        {
            // Create a local halflength array
            List<double> halflengths = new List<double>();

            // If cullValue is negative or zero set it to 1 (use all timesteps)
            if (cullValue <= 0) cullValue = 1;

            // Get current timestep number
            int CurrentTimestep = PreviousFractureData.NoTimesteps;

            // The first value is zero - this equates to the total halfmacrofracture density properties
            halflengths.Add(0);

            // Fill the rest of the array by looping backwards through every nth timestep
            // NB the current timestep would give a halflength zero, which we have already added
            for (int tsM = CurrentTimestep - cullValue; tsM >= 0; tsM -= cullValue)
            {
                // Calculate the current half length of a half-macrofracture that nucleated at the end of timestep M
                double halfLength = PreviousFractureData.getCumulativeHalfLength(CurrentTimestep, tsM);

                // Add this value to the array
                halflengths.Add(halfLength);
            }

            // Pass the array to the Macrofractures objects
            IPlus_halfMacroFractures.ResetPopulationDistributionData(halflengths.ToArray());
            IMinus_halfMacroFractures.ResetPopulationDistributionData(halflengths.ToArray());
        }
        /// <summary>
        /// Populate the macrofracture halflength index array geometrically, up to a defined maximum halflength
        /// </summary>
        /// <param name="NoHalflengths">Number of halflength index points (not including 0)</param>
        /// <param name="MaxHalflengths">Maximum halflength</param>
        public void reset_MF_halflength_array(int NoHalflengths, double MaxHalflength)
        {
            // Create a local halflength array
            List<double> halflengths = new List<double>();

            // Add the required number of intermediate index points on a logarithmic scale
            double logMaxLength = Math.Log(MaxHalflength + 1d);

            for (int halflength_no = 0; halflength_no < NoHalflengths; halflength_no++)
            {
                if (fs.FractureDistribution == StressDistribution.EvenlyDistributedStress)
                {
                    double logNewValue = ((double)(NoHalflengths - halflength_no) / (double)NoHalflengths) * logMaxLength;
                    halflengths.Add(MaxHalflength + 1d - Math.Exp(logNewValue));
                }
                else
                {
                    double logNewValue = ((double)halflength_no / (double)NoHalflengths) * logMaxLength;
                    halflengths.Add(Math.Exp(logNewValue) - 1);
                }
            }

            // Add the final index point
            halflengths.Add(MaxHalflength);

            // Pass the array to the Macrofractures objects
            IPlus_halfMacroFractures.ResetPopulationDistributionData(halflengths.ToArray());
            IMinus_halfMacroFractures.ResetPopulationDistributionData(halflengths.ToArray());
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

            // If the flag is set to deactivate the fracture set at the start of the next timestep, do this now
            if (DeactivateNextTimestep)
                CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
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
        /// Set the volumetric density of all half-macrofractures from other fracture sets that terminate against half-macrofractures from this dipset, at the end of timestep M
        /// </summary>
        /// <param name="terminatingFractureDensity_in">Volumetric density of all half-macrofractures from other fracture sets that terminate against half-macrofractures from this dipset</param>
        public void setTerminatingFractureDensity(double terminatingFractureDensity_in)
        {
            CurrentFractureData.TerminatingFractureDensity_M = terminatingFractureDensity_in;
        }
        /// <summary>
        /// Set the microfracture density indices uFP32, uFP33 and uFP35 in the CurrentFractureData object
        /// </summary>
        public void setMicrofractureDensityData()
        {
            double uFP32 = a_uFP32_total() + s_uFP32_total();
            double uFP33 = a_uFP33_total() + s_uFP33_total();
            double uFP35 = a_uFP35_total() + s_uFP35_total();
            CurrentFractureData.SetMicrofractureDensityData(uFP32, uFP33, uFP35);
        }
        /// <summary>
        /// Set the microfracture density distribution array in the CurrentFractureData object
        /// </summary>
        public void setMicrofractureDistributionData()
        {
            CurrentFractureData.SetMicrofractureDistributionData(MicroFractures.a_DP30, MicroFractures.s_DP30);
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
            bool stressShadowWidthChanged;
            switch (fs.FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows, so stress shadow width can never change
                    stressShadowWidthChanged = false;
                    break;
                case StressDistribution.StressShadow:
                case StressDistribution.DuctileBoundary:
                default:
                    // Check if either the total or the azimuth stress shadow width calculated from the mode factors is different to that in the FractureCalculationData object
                    stressShadowWidthChanged = (Mean_MF_StressShadowWidth != CurrentFractureData.Mean_StressShadowWidth_M) || (Mean_Azimuthal_MF_StressShadowWidth != CurrentFractureData.Mean_AzimuthalStressShadowWidth_M);
                    break;
            }
            // If the stress shadow widths have changed, revert any residual active sets to growing, since the deactivation probabilities may have significantly reduced
            // If the deactivation probabilities have not significantly reduced, the fracture dipsets will revert to Residual Active when the calculateTotalMacrofracturePopulation() function is called
            if (stressShadowWidthChanged)
                if (getEvolutionStage() == FractureEvolutionStage.ResidualActivity)
                    CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Growing);
            return stressShadowWidthChanged;
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

            // First we will calculate the time taken for the driving stress to reach zero (from either a positive or negative value) based on the rate of change of driving stress
            // This will act as an upper bound to the timestep length
            // If the driving stress is less than zero it will represent the time until the fracture set becomes active
            // If the driving stress is greater than zero it will represent the time until the fracture set stops propagating
            // This can only be calculated if the initial driving stress U is not zero and the rate of change of driving stress V is in the opposite direction to U
            // In fact we compare the initial driving stress to the maximum rounding error, not zero, to determine if this is the case.
            // This is because calculating the time required for driving stress to reach zero, multiplying it by horizontal strain rate to increment horizontal strain, and using new horizontal strain to calculate driving stress does not always give a driving stress = 0 (as it should).
            // Sometimes due to rounding errors in the calculation, it generates a slightly negative driving stress. This can cause the calculation to get stuck in a loop, with no strain increments and no fracture growth, until the maximum number of timesteps is reached.
            if ((Math.Abs(U) > PreviousFractureData.MaxDrivingStressRoundingError) && (Math.Sign(U) != Math.Sign(V)) && ((float)Math.Abs(V) > 0f))
            {
                // Calculating the time taken for the driving stress to reach zero for a fracture with shear displacement is more complicated than simply dividing the negative initial driving stress -U by the rate of change of driving stress V
                // As we have noted previously, the rate of change of driving stress may itself change through time, as the shear displacement vector and in situ stress change
                // We must therefore use a quadratic expression comprising the three orthogonal components of the stress acting on the fault: normal, shear in the direction of strike, and shear in the downdip direction
                // The rates of change of these components do not change through time

                // Calculate the multiplier for the normal stress component
                double mufr_squared = Math.Pow(gbc.MechProps.MuFr, 2);

                // Calculate the three quadratic terms
                double a_term = Math.Pow(taustrike_var, 2) + Math.Pow(taudip_var, 2) - (mufr_squared * Math.Pow(sneff_var, 2));
                double b_term = 2 * ((taustrike_cst * taustrike_var) + (taudip_cst * taudip_var) - (mufr_squared * sneff_cst * sneff_var));
                double c_term = Math.Pow(taustrike_cst, 2) + Math.Pow(taudip_cst, 2) - (mufr_squared * Math.Pow(sneff_cst, 2));
                double rootterm = Math.Sqrt(Math.Pow(b_term, 2) - (4 * a_term * c_term));

                // Take the lowest positive root as the optimal timestep duration
                // If U is negative and V is positive (i.e. the initial driving stress is negative but increasing) then at least one root should be positive
                // However in case neither are, or the quadratic does not have real roots (i.e. rootterm is NaN) then we can approximate the optimal timestep duration by dividing the negative initial driving stress -U by the rate of change of driving stress V
                double negativeroot = (-b_term - rootterm) / (2 * a_term);
                double positiveroot = (-b_term + rootterm) / (2 * a_term);
                double timeToSdZero;
                if ((negativeroot > PreviousFractureData.MaxDrivingStressRoundingError) && (negativeroot < positiveroot))
                    timeToSdZero = negativeroot;
                else if (positiveroot > PreviousFractureData.MaxDrivingStressRoundingError)
                    timeToSdZero = positiveroot;
                else
                    timeToSdZero = -U / V;
                if (timeToSdZero < optdur)
                    optdur = timeToSdZero;
            }

            // Next, we will calculate the time taken for the normal stress on the fracture to reach zero (from either a positive or negative value)
            // This will act as an upper bound to the timestep length if it is less than the time taken for the driving stress to reach zero
            // This represents the point at which the fracture will switch mode, from Mode 1 to Mode 2 or 3, or vice versa
            // This can only happen if it is moving in the right direction, and is not already zero
            if ((Math.Sign(sneff_cst) == -Math.Sign(sneff_var)) && ((float)sneff_cst != 0f) && ((float)sneff_var != 0f))
            {
                double timeToSneffZero = -(sneff_cst / sneff_var);
                if (timeToSneffZero < optdur)
                    optdur = timeToSneffZero;
            }

            // Finally, we will calculate a maximum timestep duration based on the rate of fracture growth, and the time taken for the fracture set to grow by the specified limit dMFP33
            // This is only required if the fracture set is growing
            // Otherwise we will return the upper bound to the duration (the time until fracture driving stress or normal stress reaches zero), or if this is not calculated, the default value infinity (no optimal duration calculated)
            // If the initial driving stress is negative, there will be no fracture growth in this timestep so we cannot calculate an optimal duration
            if (U < -PreviousFractureData.MaxDrivingStressRoundingError)
            {
                // Update the maximum driving stress rounding error
                PreviousFractureData.UpdateMaxDrivingStressRoundingError(U);

                // Since the initial driving stress is negative or zero throughout this timestep, we can set U and V to zero
                U = 0;
                V = 0;
            }
            // If the initial driving stress is zero and not increasing (i.e. U=0, V<=0), there will be no fracture growth in this timestep so we cannot calculate an optimal duration
            else if ((U < PreviousFractureData.MaxDrivingStressRoundingError) && ((float)V <= 0f))
            {
                // If U is not quite zero due to rounding error, we must round it up to zero
                // We can also set V to zero
                U = 0;
                V = 0;
            }
            // If we are not allowing reverse fractures and the new displacement vector gives a reverse sense of displacement, there will be no fracture growth in this timestep so we cannot calculate an optimal duration
            else if (!IncludeReverseFractures && DisplacementSense == FractureDisplacementSense.Reverse)
            {
                // Since the driving stress for allowed dilatant, normal or strike-slip fractures is zero or negative, we can set U and V to zero
                U = 0;
                V = 0;
            }
            // If the fracture set has been deactivated, there will be no fracture growth in this timestep so we cannot calculate an optimal duration
            // Driving stress may still be positive however
            else if (CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated)
            {
                // No calculation required
            }
            // If the initial driving stress is positive or zero, there will be no fracture growth in this timestep so the optimal timestep duration will be the estimated minimum time taken for the fracture set to grow by the specified limit dMFP33
            // This can be calculated from the appropriate equations
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
                // In these cases we cannot calculate an optimal duration
                // Otherwise we will set the optimal duration as the time taken to reach the specified d_MFP33
                if (((float)ts_ahalfMF_uF_ratio > 0f) && ((float)(V_factor + U_factor1) > 0f))
                {
                    double UV_factor = alpha_uF_b_factor * Math.Pow(V_factor + U_factor2, 1 / (b + 1));
                    double timeTodMFP33;

                    // If U>>V then the exact equation for optimal duration may give zero because (V_factor + U_factor2) ^ (1 / (b + 1)) is indistinguishable from U due to rounding
                    if ((float)UV_factor > (float)U)
                    {
                        // Use the formula for increasing stress to calculate optimal duration
                        timeTodMFP33 = (UV_factor - U) / V;
                    }
                    else // In this case we can approximate V=0 and use the constant driving stress formula
                    {
                        // Use the formula for constant stress to calculate optimal duration, and set V to zero
                        timeTodMFP33 = d_MFP33_CumhGammaM_betac1_factor2 / (U_factor1 / beta);
                        V = 0;
                    }

                    if (timeTodMFP33 < optdur)
                        optdur = timeTodMFP33;
                }
            }

            // Set the constant and variable components of driving stress in the current Fracture Calculation Data object
            CurrentFractureData.U_M = U;
            CurrentFractureData.V_M = V;

            // Recalculate the fracture driving stress vectors
            if (U <= 0)
            {
                DrivingStressVector = new VectorXYZ(0, 0, 0);
                MFDisplacementAdjustedDrivingStressVector = new VectorXYZ(0, 0, 0);
            }
            else if (sneff_cst <= 0)
            {
                double oneMinusNur = 1 - gbc.MechProps.Nu_r;
                DrivingStressVector = SigmaF_Const;
                MFDisplacementAdjustedDrivingStressVector = (sneff_cst * normalVector) + (taudip_cst * dipVector) + ((taustrike_cst / oneMinusNur) * strikeVector);
            }
            else
            {
                double oneMinusNur = 1 - gbc.MechProps.Nu_r;
                double MuFr = gbc.MechProps.MuFr;
                double sinpitch = VectorXYZ.Sin_trim(ShearStressPitch);
                double cospitch = VectorXYZ.Cos_trim(ShearStressPitch);

                DrivingStressVector = ((taudip_cst - (sinpitch * MuFr * sneff_cst)) * dipVector) + ((taustrike_cst - (cospitch * MuFr * sneff_cst)) * strikeVector);
                MFDisplacementAdjustedDrivingStressVector = ((taudip_cst - (sinpitch * MuFr * sneff_cst)) * dipVector) + (((taustrike_cst - (cospitch * MuFr * sneff_cst)) / oneMinusNur) * strikeVector);
            }

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
            double a_MFP30_Thickness = a_MFP30_total() * gbc.ThicknessAtDeformation;
            double currentMeanActiveHalfMacrofractureLength = (a_MFP30_Thickness > 0 ? a_MFP32_total() / a_MFP30_Thickness : 0);
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
                    double intersectionAngleSin = Math.Abs(VectorXYZ.Sin_trim(fs.Strike - other_fs.Strike));
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
            //double CapA = gbc.MechProps.CapA;
            //double Kc = gbc.MechProps.Kc;
            //double SqrtPi = Math.Sqrt(Math.PI);
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
            //double beta_betac2_factor = (bis2 ? 1 / c_coefficient : -2 / (4 - (2 * b) - (2 * c_coefficient)));
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

            // Create a flag for reverting to a previous FractureEvolutionStage - this is required to prevent infinite looping between the Growing and ResidualActive stages
            //bool revertFractureEvolutionStage = false;

            switch (CurrentFractureData.EvolutionStage)
            {
                case FractureEvolutionStage.NotActivated:
                    // If the fracture set is not activated, there will be no growth so we can leave the fracture density values and increments set at zero
                    break;
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
                        if (tsK_a_MFP30_residual > tsK_a_MFP30_value) //((tsK_a_MFP30_residual > tsK_a_MFP30_value) && !revertFractureEvolutionStage) // (as long as we have not already reverted to the Growing stage within this timestep)
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
                            // Maximum macrofracture propagation distance in timestep K, ignoring interaction
                            double MaxMFLength_K = PreviousFractureData.getHalfLength(tsK);
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
                            // If gamma_InvBeta_K is not positive then the fracture is not propagating
                            // In either case we can reset the fracture evolution stage to Deactivated and go to the FractureEvolutionStage.Deactivated case
                            if (double.IsInfinity(inst_F_K) || !(gamma_InvBeta_K > 0))
                            {
                                // Set the fracture evolutionary stage to Deactivated
                                CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);

                                // Go to the FractureEvolutionStage.Deactivated case - there will be no change to the cumulative fracture population data
                                goto case FractureEvolutionStage.Deactivated;
                            }

                            // Calculate mean residual fracture length and duration
                            // In the evenly distributed stress scenario, the fractures in the other sets are randomly distributed, and we can use a quick version of the formula that assumes a constant instantaneous deactivation probability
                            // If there are stress shadows, the fractures in the other sets have a semi-regular distribution
                            // In this case we must use an approximate formula that takes the weighted mean stress shadow width of all dip sets - this should give a good approximation if most fractures are in one dip set
                            bool useQuickFormula = (gbc.PropControl.StressDistributionCase == StressDistribution.EvenlyDistributedStress);
                            double ts_MeanMFLength = fs.Calculate_MeanPropagationDistance(this, tsK, new List<double>() { 0 }, useQuickFormula)[0];
                            // The mean duration of residual active macrofractures is the mean length divided by the propagation rate
                            double ts_MeanMFDuration = ts_MeanMFLength / MeanMFPropagationRate_K;

                            /* If the mean residual length is greater than the maximum propagation distance in this timestep (e.g. if the instantaneous probabilities of macrofracture deactivation have reduced due to changes in stress shadow widths) then
                            // we will revert the fracture evolution stage to Growing and go to the FractureEvolutionStage.Growing
                            if (ts_MeanMFLength > MaxMFLength_K)
                            {
                                // Set the fracture evolutionary stage to Deactivated
                                CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Growing);

                                // Set the flag for reverting to a previous FractureEvolutionStage to true
                                revertFractureEvolutionStage = true;

                                // Reset the macrofracture density values and increments
                                tsK_a_MFP30_value = 0;
                                tsK_s_MFP30_increment = 0;
                                tsK_a_MFP32_value = 0;
                                tsK_s_MFP32_increment = 0;
                                tsK_a_MFP30_residual = 0;

                                // Go to the FractureEvolutionStage.Growing case - there will be no change to the cumulative fracture population data
                                goto case FractureEvolutionStage.Growing;
                            }*/

                            // Calculate useful components
                            double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsK);
                            double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                            double ts_CumhGammaM_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betacminus1_factor));
                            double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsK - 1);
                            double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                            double ts_CumhGammaMminus1_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betacminus1_factor));
                            double ts_StressShadowDecreaseFactor = ((MeanMFPropagationRate_K > 0) && (dChiMP_dChiTot_K > 0) ? Math.Exp(-2 * CapB * Math.Abs(betac_factor) * gamma_InvBeta_K * ts_CumhGammaMminus1_betacminus1_factor * h * dChi_dMFP32_K * tsK_Duration * ts_MeanMFLength / dChiMP_dChiTot_K) : 0);

                            // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                            if (double.IsInfinity(ts_CumhGammaM))
                                break;

                            // Calculate a_MFP30 value for Timestep K
                            tsK_a_MFP30_value = (MeanMFPropagationRate_K > 0 ? Math.Abs(betac_factor) * gamma_InvBeta_K * theta_dashed_Kminus1 * ts_CumhGammaM_betacminus1_factor * ts_StressShadowDecreaseFactor * ts_MeanMFDuration : 0);

                            // Calculate s_MFP30 increments for Timestep K
                            // First we must determine whether fracture growth is limited by decrease in nucleation rate or stress shadow growth
                            // We will calculate the increments for both end members and take the smallest
                            double tsK_s_MFP30_increment_NucleationLimited = theta_dashed_Kminus1 * (ts_CumhGammaM_betac_factor - ts_CumhGammaMminus1_betac_factor);
                            // If there are no stress shadows (s_MFP30_increment_StressShadowLimited_denominator is zero) set the stress shadow limited s_MFP30 increment to be higher than the nucleation rate limited increment, so it will not be used 
                            double s_MFP30_increment_StressShadowLimited_denominator = ts_MeanMFLength * h * dChi_dMFP32_K;
                            double tsK_s_MFP30_increment_StressShadowLimited = ((float)s_MFP30_increment_StressShadowLimited_denominator != 0f ? (1 / (2 * CapB)) * ((theta_dashed_Kminus1 * dChiMP_dChiTot_K) / s_MFP30_increment_StressShadowLimited_denominator) * (1 - ts_StressShadowDecreaseFactor) : tsK_s_MFP30_increment_NucleationLimited + 1);
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

                // Increment the static macrofracture P30 values in proportion to the ratio of new to available stress shadow volume
                double fractureGrowthReductionFactor = maxAvailableStressShadowVolume / newStressShadowVolume;
                tsK_sII_MFP30_increment *= fractureGrowthReductionFactor;
                tsK_sIJ_MFP30_increment *= fractureGrowthReductionFactor;
                IPlus_halfMacroFractures.sII_P30_total += tsK_sII_MFP30_increment;
                IPlus_halfMacroFractures.sIJ_P30_total += tsK_sIJ_MFP30_increment;
                IMinus_halfMacroFractures.sII_P30_total += tsK_sII_MFP30_increment;
                IMinus_halfMacroFractures.sIJ_P30_total += tsK_sIJ_MFP30_increment;

                // Set static macrofracture P32 values so that total stress shadow volume will be 1
                double s_MFP32_required = maxAvailableStressShadowVolume / MeanW;
                IPlus_halfMacroFractures.s_P32_total = s_MFP32_required / 2;
                IMinus_halfMacroFractures.s_P32_total = s_MFP32_required / 2;

                // Set the flag to set the fracture evolutionary stage to Deactivated at the start of the next timestep
                // Then reduce the mean half-macrofracture propagation rate and microfracture propagation rate coefficient for this timestep proportionally
                // This will reduce growth in the populations of implicit microfractures, and of explicit fractures in the DFN, in proportion with the implicit macrofracture growth
                // This will also update the last item in the PreviousFractureData list, as it points to the same object
                DeactivateNextTimestep = true;
                CurrentFractureData.ReduceFractureGrowth(fractureGrowthReductionFactor);
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
        public void calculateTotalMicrofracturePopulation()
        {
            // Cache constants locally
            //double h = gbc.ThicknessAtDeformation;
            double b = gbc.MechProps.b_factor;
            double beta = gbc.MechProps.beta;
            bType b_type = gbc.MechProps.GetbType();
            bool bis2 = (b_type == bType.Equals2);
            int tsN = PreviousFractureData.NoTimesteps;
            int no_r_bins = uF_radii.Length - 1;
            double rmin_cutoff = gbc.PropControl.minImplicitMicrofractureRadius;
            double max_uF_radius = gbc.MaximumMicrofractureRadius;

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
            // h1c_factor is (h/2)^(1-c) when c!=1, ln(h/2) when c=1
            //double h1c_factor = (c_coefficient == 1 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 1 - c_coefficient));
            // h2c_factor is (h/2)^(2-c) when c!=2, ln(h/2) when c=2
            double h2c_factor = (c_coefficient == 2 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 2 - c_coefficient));
            // h3c_factor is (h/2)^(3-c) when c!=3, ln(h/2) when c=3
            double h3c_factor = (c_coefficient == 3 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 3 - c_coefficient));
            // h4c_factor is (h/2)^(4-c) when c!=4, ln(h/2) when c=4
            //double h4c_factor = (c_coefficient == 4 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 4 - c_coefficient));
            // h5c_factor is (h/2)^(5-c) when c!=5, ln(h/2) when c=5
            double h5c_factor = (c_coefficient == 5 ? Math.Log(max_uF_radius) : Math.Pow(max_uF_radius, 5 - c_coefficient));
            // Calculate multipliers for the P32 and P33 values
            //double uFP31_multiplier = Math.PI / 2;
            double uFP32_multiplier = Math.PI;
            double uFP33_multiplier = (4d / 3d) * Math.PI;
            //double uFP34_multiplier = (8d / 4d) * Math.PI;
            double uFP35_multiplier = (16d / 5d) * Math.PI;

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
            double ts_suF_deactivation_multiplier = mean_qiI_N * ts_theta_Nminus1;

            // Create local variables to calculate summation
            double a_uFP30_value = 0;
            double s_uFP30_increment = 0;
            //double a_uFP31_value = 0;
            //double s_uFP31_increment = 0;
            double a_uFP32_value = 0;
            double s_uFP32_increment = 0;
            double a_uFP33_value = 0;
            double s_uFP33_increment = 0;
            //double a_uFP34_value = 0;
            //double s_uFP34_increment = 0;
            double a_uFP35_value = 0;
            double s_uFP35_increment = 0;

            // Also set up local arrays to store the microfracture density distribution values for each index value as they are calculated, and set the initial values to zero
            double[] a_DuFP30_values = new double[no_r_bins + 1];
            double[] s_DuFP30_increments = new double[no_r_bins + 1];
            for (int r_bin = 0; r_bin < no_r_bins + 1; r_bin++)
            {
                // Add zero values to the local microfracture population data arrays
                a_DuFP30_values[r_bin] = 0;
                s_DuFP30_increments[r_bin] = 0;
            }

            // If the rmin cutoff is greater than the maximum microfracture radius, all terms will be zero
            if (rmin_cutoff < max_uF_radius)
            {
                // Equations are different for b<2, b=2 and b>2
                switch (b_type)
                {
                    case bType.LessThan2:
                        {
                            // a_uFP30 and s_uFP30 terms can be calculated directly
                            // However we cannot calculate a_uFP30 or s_uFP30 if the rmin cutoff is zero, as they will be infinite 
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
                                    a_uFP30_value = CapB * ts_theta_N * (ts_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);

                                    // Calculate s_uFP30 increment for rmin_cutoff
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP30_increment = (CapB / betac1_factor) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (ts_CumhGammaN_betac1_factor - ts_CumhGammaNminus1_betac1_factor - ts_rminCumGammaN_betac1_factor + ts_rminCumGammaNminus1_betac1_factor);
                                    else
                                        s_uFP30_increment = CapB * (ts_theta_Nminus1 - ts_theta_N) * (ts_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);
                                }
                            }

                            // a_uFP32_value, s_uFP32_increment, a_uFP33_value and s_uFP33_increment terms must be calculated numerically by iterating through a range of r-values
                            // The static uF increments calculated in this iteration represent growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                            for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                            {
                                // Get range of radii sizes in the current r-bin
                                double rb_minRad = uF_radii[r_bin];
                                double rb_maxRad = uF_radii[r_bin + 1];

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
                                    // Calculate a_DuFP30 value for this bin
                                    double a_DuFP30_rbin = CapB * ts_theta_N * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);
                                    a_DuFP30_values[r_bin] = a_DuFP30_rbin;

                                    // Calculate s_DuFP30 increment for this bin
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    double s_DuFP30_increment_rbin;
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_DuFP30_increment_rbin = (CapB / betac1_factor) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (rb_rmaxCumGammaN_betac1_factor - rb_rmaxCumGammaNminus1_betac1_factor - rb_rminCumGammaN_betac1_factor + rb_rminCumGammaNminus1_betac1_factor);
                                    else
                                        s_DuFP30_increment_rbin = CapB * (ts_theta_Nminus1 - ts_theta_N) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);
                                    s_DuFP30_increments[r_bin] = s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP31_value component
                                    //a_uFP31_value += uFP31_multiplier * rb_minRad * a_DuFP30_rbin;

                                    // Calculate term for s_uFP31_increment component
                                    //s_uFP31_increment += uFP31_multiplier * rb_minRad * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP32_value component
                                    a_uFP32_value += uFP32_multiplier * Math.Pow(rb_minRad, 2) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP32_increment component
                                    s_uFP32_increment += uFP32_multiplier * Math.Pow(rb_minRad, 2) * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP33_value component
                                    a_uFP33_value += uFP33_multiplier * Math.Pow(rb_minRad, 3) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP33_increment component
                                    s_uFP33_increment += uFP33_multiplier * Math.Pow(rb_minRad, 3) * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP34_value component
                                    //a_uFP34_value += uFP34_multiplier * Math.Pow(rb_minRad, 4) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP34_increment component
                                    //s_uFP34_increment += uFP34_multiplier * Math.Pow(rb_minRad, 4) * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP35_value component
                                    a_uFP35_value += uFP35_multiplier * Math.Pow(rb_minRad, 5) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP35_increment component
                                    s_uFP35_increment += uFP35_multiplier * Math.Pow(rb_minRad, 5) * s_DuFP30_increment_rbin;
                                }
                            }
                        }
                        break;
                    case bType.Equals2:
                        {
                            // Calculate local helper variables
                            double ts_CumGammaN_betac_factor = Math.Exp(betac_factor * ts_CumGammaN);
                            double ts_CumGammaNminus1_betac_factor = Math.Exp(betac_factor * ts_CumGammaNminus1);

                            // Calculate terms analytically for whole fracture population
                            // When b=2 the extrapolated initial minimum radius is always greater than 0
                            {
                                // We cannot calculate the a_uFP30 and s_uFP30 terms if the rmin cutoff is zero, as they will be infinite 
                                if (rmin_cutoff > 0)
                                {
                                    // Calculate useful components
                                    double rb_minRad_betac_factor = Math.Pow(rmin_cutoff, betac_factor);
                                    double rb_rminCumGammaN_betac_factor = rb_minRad_betac_factor * ts_CumGammaN_betac_factor;
                                    double rb_rminCumGammaNminus1_betac_factor = rb_minRad_betac_factor * ts_CumGammaNminus1_betac_factor;

                                    // Calculate a_uFP30 value for rmin_cutoff
                                    a_uFP30_value = CapB * ts_theta_N * (rb_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);

                                    // Calculate s_uFP30 increment for rmin_cutoff
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP30_increment = (CapB / c_coefficient) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (rb_rminCumGammaN_betac_factor - rb_rminCumGammaNminus1_betac_factor - ts_CumhGammaN_betac1_factor + ts_CumhGammaNminus1_betac1_factor);
                                    else
                                        s_uFP30_increment = CapB * (ts_theta_Nminus1 - ts_theta_N) * (rb_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);
                                }

                                // a_uFP32_value, s_uFP32_increment, a_uFP33_value and s_uFP33_increment terms can be calculated analytically
                                // However we will still need to cycle through the r_bins to calculate the components of the a_DuFP30 and s_DuFP30 arrays
                                for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                                {
                                    // Get range of radii sizes in the current r-bin
                                    double rb_minRad = uF_radii[r_bin];
                                    double rb_maxRad = uF_radii[r_bin + 1];

                                    // If the maximum bin size is less than the minimum cutoff, go straight on to the next bin
                                    if (rb_maxRad < rmin_cutoff) continue;
                                    // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                                    if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;
                                    // If the minimum bin size is zero, go straight on to the next bin
                                    if (rb_minRad <= 0) continue;

                                    // Calculate useful components
                                    double rb_minRad_betac_factor = Math.Pow(rb_minRad, betac_factor);
                                    double rb_rminCumGammaN_betac_factor = rb_minRad_betac_factor * ts_CumGammaN_betac_factor;
                                    double rb_rminCumGammaNminus1_betac_factor = rb_minRad_betac_factor * ts_CumGammaNminus1_betac_factor;
                                    double rb_maxRad_betac_factor = Math.Pow(rb_maxRad, betac_factor);
                                    double rb_rmaxCumGammaN_betac_factor = rb_maxRad_betac_factor * ts_CumGammaN_betac_factor;
                                    double rb_rmaxCumGammaNminus1_betac_factor = rb_maxRad_betac_factor * ts_CumGammaNminus1_betac_factor;

                                    // Calculate a_DuFP30 value for this bin
                                    double a_DuFP30_rbin = CapB * ts_theta_N * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);
                                    a_DuFP30_values[r_bin] = a_DuFP30_rbin;

                                    // Calculate s_DuFP30 increment for this bin
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    double s_DuFP30_increment_rbin;
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_DuFP30_increment_rbin = (CapB / c_coefficient) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (rb_rminCumGammaN_betac_factor - rb_rminCumGammaNminus1_betac_factor - rb_rmaxCumGammaN_betac_factor + rb_rmaxCumGammaNminus1_betac_factor);
                                    else
                                        s_DuFP30_increment_rbin = CapB * (ts_theta_Nminus1 - ts_theta_N) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);
                                    s_DuFP30_increments[r_bin] = s_DuFP30_increment_rbin;
                                }

                                // We cannot calculate the a_uFP31 and s_uFP31 terms if the rmin cutoff is zero and c>=1, as they will be infinite
                                /*if ((rmin_cutoff > 0) || (c_coefficient < 1))
                                {
                                    // Calculate useful components
                                    double b2_uFP31_factor = (c_coefficient == 1 ? 1 : (1 - c_coefficient));
                                    double rb_minRad_1c_factor = (c_coefficient == 1 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 1 - c_coefficient));

                                    // Calculate term for a_uFP31_value
                                    a_uFP31_value = CapB * uFP31_multiplier * ts_theta_N * (c_coefficient / b2_uFP31_factor) * ts_CumGammaN_betac_factor * (h1c_factor - rb_minRad_1c_factor);

                                    // Calculate term for s_uFP31_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    s_uFP31_increment = CapB * uFP31_multiplier * (ts_suF_deactivation_multiplier / b2_uFP31_factor) * (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h1c_factor - rb_minRad_1c_factor);
                                }*/

                                // We cannot calculate the a_uFP32 and s_uFP32 terms if the rmin cutoff is zero and c>=2, as they will be infinite
                                if ((rmin_cutoff > 0) || (c_coefficient < 2))
                                {
                                    // Calculate useful components
                                    double b2_uFP32_factor = (c_coefficient == 2 ? 1 : (2 - c_coefficient));
                                    double rb_minRad_2c_factor = (c_coefficient == 2 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 2 - c_coefficient));

                                    // Calculate term for a_uFP32_value
                                    a_uFP32_value = CapB * uFP32_multiplier * ts_theta_N * (c_coefficient / b2_uFP32_factor) * ts_CumGammaN_betac_factor * (h2c_factor - rb_minRad_2c_factor);

                                    // Calculate term for s_uFP32_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP32_increment = (CapB / b2_uFP32_factor) * uFP32_multiplier * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h2c_factor - rb_minRad_2c_factor);
                                    else
                                        s_uFP32_increment = CapB * uFP32_multiplier * (c_coefficient / b2_uFP32_factor) * (ts_theta_Nminus1 - ts_theta_N) * ts_CumGammaN_betac_factor * (h2c_factor - rb_minRad_2c_factor);
                                    //s_uFP32_increment = CapB * uFP32_multiplier * (ts_suF_deactivation_multiplier / b2_uFP32_factor) * (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h2c_factor - rb_minRad_2c_factor);
                                }

                                // We cannot calculate the a_uFP33 and s_uFP33 terms if the rmin cutoff is zero and c>=3, as they will be infinite
                                if ((rmin_cutoff > 0) || (c_coefficient < 3))
                                {
                                    // Calculate useful components
                                    double b2_uFP33_factor = (c_coefficient == 3 ? 1 : (3 - c_coefficient));
                                    double rb_minRad_3c_factor = (c_coefficient == 3 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 3 - c_coefficient));

                                    // Calculate term for a_uFP33_value
                                    a_uFP33_value = CapB * uFP33_multiplier * ts_theta_N * (c_coefficient / b2_uFP33_factor) * ts_CumGammaN_betac_factor * (h3c_factor - rb_minRad_3c_factor);

                                    // Calculate term for s_uFP33_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP33_increment = (CapB / b2_uFP33_factor) * uFP33_multiplier * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h3c_factor - rb_minRad_3c_factor);
                                    else
                                        s_uFP33_increment = CapB * uFP33_multiplier * (c_coefficient / b2_uFP33_factor) * (ts_theta_Nminus1 - ts_theta_N) * ts_CumGammaN_betac_factor * (h3c_factor - rb_minRad_3c_factor);
                                    //s_uFP33_increment = CapB * uFP33_multiplier * (ts_suF_deactivation_multiplier / b2_uFP33_factor) * (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h3c_factor - rb_minRad_3c_factor);
                                }

                                // We cannot calculate the a_uFP34 and s_uFP34 terms if the rmin cutoff is zero and c>=4, as they will be infinite
                                /*if ((rmin_cutoff > 0) || (c_coefficient < 4))
                                {
                                    // Calculate useful components
                                    double b2_uFP34_factor = (c_coefficient == 4 ? 1 : (4 - c_coefficient));
                                    double rb_minRad_4c_factor = (c_coefficient == 4 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 4 - c_coefficient));

                                    // Calculate term for a_uFP34_value
                                    a_uFP34_value = CapB * uFP34_multiplier * ts_theta_N * (c_coefficient / b2_uFP34_factor) * ts_CumGammaN_betac_factor * (h4c_factor - rb_minRad_4c_factor);

                                    // Calculate term for s_uFP34_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP34_increment = (CapB / b2_uFP34_factor) * uFP34_multiplier * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h4c_factor - rb_minRad_4c_factor);
                                    else
                                        s_uFP34_increment = CapB * uFP34_multiplier * (c_coefficient / b2_uFP34_factor) * (ts_theta_Nminus1 - ts_theta_N) * ts_CumGammaN_betac_factor * (h4c_factor - rb_minRad_4c_factor);
                                    //s_uFP34_increment = CapB * uFP34_multiplier * (ts_suF_deactivation_multiplier / b2_uFP34_factor) * (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h4c_factor - rb_minRad_4c_factor);
                                }*/

                                // We cannot calculate the a_uFP35 and s_uFP35 terms if the rmin cutoff is zero and c>=5, as they will be infinite
                                if ((rmin_cutoff > 0) || (c_coefficient < 5))
                                {
                                    // Calculate useful components
                                    double b2_uFP35_factor = (c_coefficient == 5 ? 1 : (5 - c_coefficient));
                                    double rb_minRad_5c_factor = (c_coefficient == 5 ? Math.Log(rmin_cutoff) : Math.Pow(rmin_cutoff, 5 - c_coefficient));

                                    // Calculate term for a_uFP35_value
                                    a_uFP35_value = CapB * uFP35_multiplier * ts_theta_N * (c_coefficient / b2_uFP35_factor) * ts_CumGammaN_betac_factor * (h5c_factor - rb_minRad_5c_factor);

                                    // Calculate term for s_uFP35_increment
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP35_increment = (CapB / b2_uFP35_factor) * uFP35_multiplier * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h5c_factor - rb_minRad_5c_factor);
                                    else
                                        s_uFP35_increment = CapB * uFP35_multiplier * (c_coefficient / b2_uFP35_factor) * (ts_theta_Nminus1 - ts_theta_N) * ts_CumGammaN_betac_factor * (h5c_factor - rb_minRad_5c_factor);
                                    //s_uFP35_increment = CapB * uFP35_multiplier * (ts_suF_deactivation_multiplier / b2_uFP35_factor) * (ts_CumGammaN_betac_factor - ts_CumGammaNminus1_betac_factor) * (h5c_factor - rb_minRad_5c_factor);
                                }
                            }
                        }
                        break;
                    case bType.GreaterThan2:
                        {
                            // a_uFP30 and s_uFP30 terms can be calculated directly
                            // However we cannot calculate a_uFP30 or s_uFP30 if the rmin cutoff is zero, as they will be infinite 
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
                                    a_uFP30_value = CapB * ts_theta_N * (ts_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);

                                    // Calculate s_uFP30 increment for rmin_cutoff
                                    // This increment represents growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_uFP30_increment = (CapB / betac1_factor) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (ts_rminCumGammaN_betac1_factor - ts_rminCumGammaNminus1_betac1_factor - ts_CumhGammaN_betac1_factor + ts_CumhGammaNminus1_betac1_factor);
                                    else
                                        s_uFP30_increment = CapB * (ts_theta_Nminus1 - ts_theta_N) * (ts_rminCumGammaN_betac_factor - ts_CumhGammaN_betac_factor);
                                }
                            }

                            // a_uFP32_value, s_uFP32_increment, a_uFP33_value and s_uFP33_increment terms must be calculated numerically by iterating through a range of r-values
                            // The static uF increments calculated in this iteration represent growth deactivation microfractures; additional terms for transition deactivation microfractures will be added later
                            for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                            {
                                // Get range of radii sizes in the current r-bin
                                double rb_minRad = uF_radii[r_bin];
                                double rb_maxRad = uF_radii[r_bin + 1];

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
                                    // Calculate a_DuFP30 value for this bin
                                    double a_DuFP30_rbin = CapB * ts_theta_N * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);
                                    a_DuFP30_values[r_bin] = a_DuFP30_rbin;

                                    // Calculate s_DuFP30 increment for this bin
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    double s_DuFP30_increment_rbin;
                                    if (ts_gamma_InvBeta_N > 0)
                                        s_DuFP30_increment_rbin = (CapB / betac1_factor) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_N) *
                                            (rb_rminCumGammaN_betac1_factor - rb_rminCumGammaNminus1_betac1_factor - rb_rmaxCumGammaN_betac1_factor + rb_rmaxCumGammaNminus1_betac1_factor);
                                    else
                                        s_DuFP30_increment_rbin = CapB * (ts_theta_Nminus1 - ts_theta_N) * (rb_rminCumGammaN_betac_factor - rb_rmaxCumGammaN_betac_factor);
                                    s_DuFP30_increments[r_bin] = s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP31_value component
                                    //a_uFP31_value += uFP31_multiplier * rb_minRad * a_DuFP30_rbin;

                                    // Calculate term for s_uFP31_increment component
                                    //s_uFP31_increment += uFP31_multiplier * rb_minRad * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP32_value component
                                    a_uFP32_value += uFP32_multiplier * Math.Pow(rb_minRad, 2) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP32_increment component
                                    s_uFP32_increment += uFP32_multiplier * Math.Pow(rb_minRad, 2) * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP33_value component
                                    a_uFP33_value += uFP33_multiplier * Math.Pow(rb_minRad, 3) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP33_increment component
                                    s_uFP33_increment += uFP33_multiplier * Math.Pow(rb_minRad, 3) * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP34_value component
                                    //a_uFP34_value += uFP34_multiplier * Math.Pow(rb_minRad, 4) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP34_increment component
                                    //s_uFP34_increment += uFP34_multiplier * Math.Pow(rb_minRad, 4) * s_DuFP30_increment_rbin;

                                    // Calculate term for a_uFP35_value component
                                    a_uFP35_value += uFP35_multiplier * Math.Pow(rb_minRad, 5) * a_DuFP30_rbin;

                                    // Calculate term for s_uFP35_increment component
                                    s_uFP35_increment += uFP35_multiplier * Math.Pow(rb_minRad, 5) * s_DuFP30_increment_rbin;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }

                // Add additional terms to s_uFP30_increment, s_uFP32_increment and s_uFP33_increment to account for transition deactivation microfractures
                double transition_deactivation_term = 0;
                if (ts_CumhGammaN < double.PositiveInfinity)
                    transition_deactivation_term = (ts_theta_Nminus1 - ts_theta_dashed_Nminus1) * (ts_CumhGammaN_betac_factor - ts_CumhGammaNminus1_betac_factor);
                s_uFP30_increment += CapB * transition_deactivation_term;
                //s_uFP31_increment += CapB * uFP31_multiplier * max_uF_radius * transition_deactivation_term;
                s_uFP32_increment += CapB * uFP32_multiplier * Math.Pow(max_uF_radius, 2) * transition_deactivation_term;
                s_uFP33_increment += CapB * uFP33_multiplier * Math.Pow(max_uF_radius, 3) * transition_deactivation_term;
                //s_uFP34_increment += CapB * uFP34_multiplier * Math.Pow(max_uF_radius, 4) * transition_deactivation_term;
                s_uFP35_increment += CapB * uFP35_multiplier * Math.Pow(max_uF_radius, 5) * transition_deactivation_term;

                // Add final components to a_DuFP30 and s_DuFP30 arrays representing transition deactivation microfractures
                a_DuFP30_values[no_r_bins] = 0;
                s_DuFP30_increments[no_r_bins] = CapB * transition_deactivation_term;

                // Number or area of static microfractures cannot decrease 
                // Therefore the static microfracture increments s_uFP30, s_uFP32 and s_uFP33 can never be negative; if they are set them to zero
                if (s_uFP30_increment < 0)
                    s_uFP30_increment = 0;
                //if (s_uFP31_increment < 0)
                //    s_uFP31_increment = 0;
                if (s_uFP32_increment < 0)
                    s_uFP32_increment = 0;
                if (s_uFP33_increment < 0)
                    s_uFP33_increment = 0;
                //if (s_uFP34_increment < 0)
                //    s_uFP34_increment = 0;
                if (s_uFP35_increment < 0)
                    s_uFP35_increment = 0;

            } // End if the rmin cutoff is greater than the maximum microfracture radius

            // Update values for the total microfracture population data for this fracture dip set
            MicroFractures.a_P30_total = a_uFP30_value;
            MicroFractures.s_P30_total += s_uFP30_increment;
            //MicroFractures.a_P31_total = a_uFP31_value;
            //MicroFractures.s_P31_total += s_uFP31_increment;
            MicroFractures.a_P32_total = a_uFP32_value;
            MicroFractures.s_P32_total += s_uFP32_increment;
            MicroFractures.a_P33_total = a_uFP33_value;
            MicroFractures.s_P33_total += s_uFP33_increment;
            //MicroFractures.a_P34_total = a_uFP34_value;
            //MicroFractures.s_P34_total += s_uFP34_increment;
            MicroFractures.a_P35_total = a_uFP35_value;
            MicroFractures.s_P35_total += s_uFP35_increment;

            // Update values for the microfracture density distribution data for this fracture dip set
            for (int r_bin = 0; r_bin < no_r_bins + 1; r_bin++)
            {
                MicroFractures.a_DP30[r_bin] = a_DuFP30_values[r_bin];
                MicroFractures.s_DP30[r_bin] += s_DuFP30_increments[r_bin];
            }
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
#if DBLOG
            MF_halflengths[0] = 0;
            string fileName = string.Format("logFile_X{0}_Y{1}_Strike{2}_Dip{3}.txt", gbc.SWtop.X, gbc.SWtop.Y, (int)(fs.Strike * 180 / Math.PI), (int)(Dip * 180 / Math.PI));
            String namecomb = gbc.PropControl.FolderPath + fileName;
            StreamWriter logFile = new StreamWriter(namecomb);
            logFile.WriteLine("NewSet");
#endif
            int noHalflengths = MF_halflengths.Length;

            // Also set up local arrays to store the cumulative macrofracture population values for each index value as they are calculated, and set the initial values to zero
            double[] tsN_a_MFP30_values = new double[noHalflengths];
            double[] tsN_sII_MFP30_values = new double[noHalflengths];
            double[] tsN_sIJ_MFP30_values = new double[noHalflengths];
            double[] tsN_a_MFP32_values = new double[noHalflengths];
            double[] tsN_s_MFP32_values = new double[noHalflengths];
            for (int halflength_no = 0; halflength_no < noHalflengths; halflength_no++)
            {
                // Add zero values to the local cumulative macrofracture population data arrays
                tsN_a_MFP30_values[halflength_no] = 0;
                tsN_sII_MFP30_values[halflength_no] = 0;
                tsN_sIJ_MFP30_values[halflength_no] = 0;
                tsN_a_MFP32_values[halflength_no] = 0;
                tsN_s_MFP32_values[halflength_no] = 0;
            }

            // Loop through the previous timesteps K to calculate the static data increments
            // For the current timestep N also calculate the active data values
            for (int tsK = 1; tsK <= tsN; tsK++)
            {
                // Set flag to determine if this is the last timestep
                bool is_tsN = (tsK == tsN);

                // Also set up local arrays to store the cumulative macrofracture population values calculated for this timestep K, and set the initial values to zero
                double[] tsK_a_MFP30_values = new double[noHalflengths];
                double[] tsK_s_MFP30_increments = new double[noHalflengths];
                double[] tsK_sII_MFP30_increments = new double[noHalflengths];
                double[] tsK_sIJ_MFP30_increments = new double[noHalflengths];
                double[] tsK_a_MFP32_values = new double[noHalflengths];
                double[] tsK_s_MFP32_increments = new double[noHalflengths];
                for (int halflength_no = 0; halflength_no < noHalflengths; halflength_no++)
                {
                    // Add zero values to the local cumulative macrofracture population data arrays
                    tsK_a_MFP30_values[halflength_no] = 0;
                    tsK_s_MFP30_increments[halflength_no] = 0;
                    tsK_sII_MFP30_increments[halflength_no] = 0;
                    tsK_sIJ_MFP30_increments[halflength_no] = 0;
                    tsK_a_MFP32_values[halflength_no] = 0;
                    tsK_s_MFP32_increments[halflength_no] = 0;
                }

                // Set up and populate a local array of J-timestep values for each index half-length
                int[] tsJ = new int[noHalflengths];
                int currentJTimestep = tsK;
                for (int halflength_no = 0; halflength_no < noHalflengths; halflength_no++)
                {
                    // Calculate the J timesteps for each index value: this is the timestep in which a currently active fracture of the specified half-length must have nucleated
                    while ((currentJTimestep > 0) && (MF_halflengths[halflength_no] >= PreviousFractureData.getCumulativeHalfLength(tsK, currentJTimestep)))
                        currentJTimestep--;
                    tsJ[halflength_no] = currentJTimestep;
                }

                switch (PreviousFractureData.getEvolutionStage(tsK))
                {
                    case FractureEvolutionStage.NotActivated:
                        // If the fracture set is not activated, there will be no growth so we can leave the fracture density values and increments set at zero
                        break;
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

                                    for (int indexPoint = 0; indexPoint < noHalflengths; indexPoint++)
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
                                for (int indexPoint = 0; indexPoint < noHalflengths; indexPoint++)
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
                            for (int indexPoint = 0; indexPoint < noHalflengths; indexPoint++)
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
                            // Convert the macrofracture halflength array to a list
                            List<double> MF_halflength_list = new List<double>(MF_halflengths);

                            // Current instantaneous probabilities of macrofracture deactivation
                            double inst_F_K = PreviousFractureData.getInstantaneousF(tsK);
                            double inst_FII_K = PreviousFractureData.getInstantaneousFII(tsK);
                            double inst_FIJ_K = PreviousFractureData.getInstantaneousFIJ(tsK);

                            // Get a list of the probabilities that fractures will reach the index lengths
                            List<double> fractureLengthProbabilityMultipliers = fs.Calculate_Phi_ByDistance(this, tsK, MF_halflength_list);

                            // Get a list of the normalised cumulative lengths of fractures longer than the index lengths
                            // In the evenly distributed stress scenario, the fractures in the other sets are randomly distributed, and we can use a quick version of the formula that assumes a constant instantaneous deactivation probability
                            // If there are stress shadows, the fractures in the other sets have a semi-regular distribution
                            // In this case we must use an approximate formula that takes the weighted mean stress shadow width of all dip sets - this should give a good approximation if most fractures are in one dip set
                            bool useQuickFormula = (gbc.PropControl.StressDistributionCase == StressDistribution.EvenlyDistributedStress);
                            List<double> fractureLengthGrowthMultipliers = fs.Calculate_MeanPropagationDistance(this, tsK, MF_halflength_list, useQuickFormula);
                            // Also get the mean length and duration of zero length macrofractures for calculating the base values
                            double ts_MeanMFLength = fs.Calculate_MeanPropagationDistance(this, tsK, new List<double>() { 0 }, useQuickFormula)[0];
                            // The mean duration of residual active macrofractures is the mean length divided by the propagation rate
                            double ts_MeanMFDuration = ts_MeanMFLength / MeanMFPropagationRate_K;

                            // Calculate useful components
                            double ts_CumhGammaM = PreviousFractureData.getCum_hGamma_M(tsK);
                            double ts_CumhGammaM_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betac_factor));
                            double ts_CumhGammaM_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaM) : Math.Pow(ts_CumhGammaM, betacminus1_factor));
                            double ts_CumhGammaMminus1 = PreviousFractureData.getCum_hGamma_M(tsK - 1);
                            double ts_CumhGammaMminus1_betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));
                            double ts_CumhGammaMminus1_betacminus1_factor = (bis2 ? Math.Exp(betacminus1_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betacminus1_factor));
                            double ts_StressShadowDecreaseFactor = (dChiMP_dChiTot_K > 0 ? Math.Exp(-2 * CapB * Math.Abs(betac_factor) * gamma_InvBeta_K * ts_CumhGammaMminus1_betacminus1_factor * h * dChi_dMFP32_K * tsK_Duration * ts_MeanMFLength / dChiMP_dChiTot_K) : 0);

                            // If b is very large, (h/2)^(1/beta) will tend to infinity, so ts_CumhGammaM, ts_CumhGammaMminus1 and values derived from them will also be infinite; in this case no increments will be calculated
                            if (double.IsInfinity(ts_CumhGammaM))
                                break;

                            // Calculate the active MFP30 value for all fractures of any length
                            // The cumulative MFP30 and MFP32 datapoints can be calculated by multiplying these base values with probability multipliers calculated for the fracture lengths
                            // If the fracture is not propagating then both gamma_InvBeta_K and inst_F_K will be zero, giving a NaN when calculating the residual a_MFP30 value
                            // In this case we will set tsK_a_MFP30_value to 0
                            double tsK_a_MFP30_value_base = (MeanMFPropagationRate_K > 0 ? Math.Abs(betac_factor) * gamma_InvBeta_K * ts_MeanMFDuration * theta_dashed_Kminus1 * ts_CumhGammaM_betacminus1_factor * ts_StressShadowDecreaseFactor : 0);

                            // Calculate the static MFP30 increments for all fractures of any length
                            // The cumulative MFP30 and MFP32 datapoints can be calculated by multiplying these base values with probability multipliers calculated for the fracture lengths
                            // First we must determine whether fracture growth is limited by decrease in nucleation rate or stress shadow growth
                            // We will calculate the increments for both end members and take the smallest
                            double tsK_s_MFP30_increment_NucleationLimited = theta_dashed_Kminus1 * (ts_CumhGammaM_betac_factor - ts_CumhGammaMminus1_betac_factor);
                            // If there are no stress shadows (s_MFP30_increment_StressShadowLimited_denominator is zero) set the stress shadow limited s_MFP30 increment to be higher than the nucleation rate limited increment, so it will not be used 
                            double s_MFP30_increment_StressShadowLimited_denominator = (ts_MeanMFLength * h * dChi_dMFP32_K);
                            double tsK_s_MFP30_increment_StressShadowLimited = ((float)s_MFP30_increment_StressShadowLimited_denominator != 0f ? (1 / (2 * CapB)) * ((theta_dashed_Kminus1 * dChiMP_dChiTot_K) / s_MFP30_increment_StressShadowLimited_denominator) * (1 - ts_StressShadowDecreaseFactor) : tsK_s_MFP30_increment_NucleationLimited + 1);
                            double tsK_s_MFP30_increment_base = Math.Min(tsK_s_MFP30_increment_NucleationLimited, tsK_s_MFP30_increment_StressShadowLimited);
#if DBLOG
                            logFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}", tsK, theta_dashed_Kminus1, tsK_s_MFP30_increment_NucleationLimited, tsK_s_MFP30_increment_StressShadowLimited, inst_F_K, ts_StressShadowDecreaseFactor, ts_CumhGammaM, ts_CumhGammaM_betac_factor, ts_CumhGammaM_betacminus1_factor, ts_CumhGammaMminus1, ts_CumhGammaMminus1_betac_factor, ts_CumhGammaMminus1_betacminus1_factor));
                            logFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", tsK, MeanMFPropagationRate_K, Mean_MF_StressShadowWidth_K, dChi_dMFP32_K));
                            string lengthProbabilityMultipliers = string.Format("LengthProbabilityMultipliers Timestep {0}", tsK);
                            string lengthGrowthMultipliers = string.Format("LengthGrowthMultipliers Timestep {0}", tsK);
#endif
                            for (int indexPoint = 0; indexPoint < noHalflengths; indexPoint++)
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
                for (int indexPoint = 0; indexPoint < noHalflengths; indexPoint++)
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
            for (int indexPoint = 0; indexPoint < noHalflengths; indexPoint++)
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
            IMinus_halfMacroFractures.a_P30 = (double[])tsN_a_MFP30_values.Clone();
            IMinus_halfMacroFractures.sII_P30 = (double[])tsN_sII_MFP30_values.Clone();
            IMinus_halfMacroFractures.sIJ_P30 = (double[])tsN_sIJ_MFP30_values.Clone();
            IMinus_halfMacroFractures.a_P32 = (double[])tsN_a_MFP32_values.Clone();
            IMinus_halfMacroFractures.s_P32 = (double[])tsN_s_MFP32_values.Clone();
        }
        /// <summary>
        /// Calculate the cumulative active and static microfracture density distribution functions a_uFP30(r), s_uFP30(r), a_uFP32(r), s_uFP32(r), a_uFP33(r) and s_uFP33(r) at the current time, for a specified list of radii r
        /// </summary>
        public void calculateCumulativeMicrofracturePopulationArrays()
        {
            // Cache constants locally
            //double h = gbc.ThicknessAtDeformation;
            double b = gbc.MechProps.b_factor;
            double beta = gbc.MechProps.beta;
            bType b_type = gbc.MechProps.GetbType();
            bool bis2 = (b_type == bType.Equals2);
            int tsN = PreviousFractureData.NoTimesteps;
            int no_r_bins = uF_radii.Length - 1;
            double rmin_cutoff = gbc.PropControl.minImplicitMicrofractureRadius;
            double max_uF_radius = gbc.MaximumMicrofractureRadius;

            // Calculate multipliers for the P32 and P33 values
            double uFP32_multiplier = Math.PI;
            double uFP33_multiplier = (4d / 3d) * Math.PI;

            // Also set up local arrays to store the cumulative microfracture population values for each index value as they are calculated, and set the initial values to zero
            double[] a_uFP30_values = new double[no_r_bins + 1];
            double[] s_uFP30_values = new double[no_r_bins + 1];
            double[] a_uFP32_components = new double[no_r_bins + 1];
            double[] s_uFP32_components = new double[no_r_bins + 1];
            double[] a_uFP33_components = new double[no_r_bins + 1];
            double[] s_uFP33_components = new double[no_r_bins + 1];
            double[] a_uFP32_values = new double[no_r_bins + 1];
            double[] s_uFP32_values = new double[no_r_bins + 1];
            double[] a_uFP33_values = new double[no_r_bins + 1];
            double[] s_uFP33_values = new double[no_r_bins + 1];
            for (int r_bin = 0; r_bin < no_r_bins + 1; r_bin++)
            {
                // Add zero values to the local cumulative microfracture population data arrays
                a_uFP30_values[r_bin] = 0;
                s_uFP30_values[r_bin] = 0;
                a_uFP32_components[r_bin] = 0;
                s_uFP32_components[r_bin] = 0;
                a_uFP33_components[r_bin] = 0;
                s_uFP33_components[r_bin] = 0;
                a_uFP32_values[r_bin] = 0;
                s_uFP32_values[r_bin] = 0;
                a_uFP33_values[r_bin] = 0;
                s_uFP33_values[r_bin] = 0;
            }

            // If the rmin cutoff is greater than the maximum microfracture radius, all terms will be zero
            if (rmin_cutoff < max_uF_radius)
            {
                // If b=2 we can calculate all cumulative density values analytically
                if (bis2)
                {
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
                    double b2_uFP32_factor = (c_coefficient == 2 ? 1 : (2 - c_coefficient));
                    double b2_uFP33_factor = (c_coefficient == 3 ? 1 : (3 - c_coefficient));

                    // Loop through the previous timesteps M to calculate the static data increments
                    // For the current timestep N also calculate the active data values
                    for (int tsM = 1; tsM <= tsN; tsM++)
                    {
                        // Set flag to determine if this is the last timestep
                        bool is_tsN = (tsM == tsN);

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
                        double ts_suF_deactivation_multiplier = mean_qiI_M * ts_theta_Mminus1;

                        // Calculate useful components
                        double ts_CumGammaM_betac_factor = Math.Exp(betac_factor * ts_CumGammaM);
                        double ts_CumGammaMminus1_betac_factor = Math.Exp(betac_factor * ts_CumGammaMminus1);

                        // Loop through all index points / r_bins and calculate the components of microfracture population data arrays
                        for (int r_bin = 0; r_bin < no_r_bins; r_bin++)
                        {
                            // Get minimum radius size in the current r-bin
                            double rb_minRad = uF_radii[r_bin];
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
                                        a_uFP30_values[r_bin] = CapB * ts_theta_M * (rb_rminCumGammaM_betac_factor - ts_CumhGammaM_betac_factor);
                                    }

                                    // Calculate term for s_uFP30 increment for rmin (excluding transition deactivation microfractures)
                                    // NB the static microfracture population can still grow even if the driving stress is zero, as microfractures can be deactivated by macrofractures from other dipsets
                                    if (ts_gamma_InvBeta_M > 0)
                                        s_uFP30_values[r_bin] += (CapB / c_coefficient) * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_M) *
                                            (rb_rminCumGammaM_betac_factor - rb_rminCumGammaMminus1_betac_factor - ts_CumhGammaM_betac1_factor + ts_CumhGammaMminus1_betac1_factor);
                                    else
                                        s_uFP30_values[r_bin] += CapB * (ts_theta_Mminus1 - ts_theta_M) * (rb_rminCumGammaM_betac_factor - ts_CumhGammaM_betac_factor);
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
                                        a_uFP32_values[r_bin] = CapB * uFP32_multiplier * (c_coefficient / b2_uFP32_factor) * ts_theta_M * ts_CumGammaM_betac_factor * (h2c_factor - rb_minRad_2c_factor);
                                    }

                                    // Calculate term for s_uFP32 increment for rmin (excluding transition deactivation microfractures)
                                    if (ts_gamma_InvBeta_M > 0)
                                        s_uFP32_values[r_bin] += (CapB / b2_uFP32_factor) * uFP32_multiplier * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_M) *
                                            (ts_CumGammaM_betac_factor - ts_CumGammaMminus1_betac_factor) * (h2c_factor - rb_minRad_2c_factor);
                                    else
                                        s_uFP32_values[r_bin] += CapB * uFP32_multiplier * (c_coefficient / b2_uFP32_factor) * (ts_theta_Mminus1 - ts_theta_M) * ts_CumGammaM_betac_factor * (h2c_factor - rb_minRad_2c_factor);
                                    //s_uFP32_values[r_bin] += CapB * uFP32_multiplier * (ts_suF_deactivation_multiplier / b2_uFP32_factor) * (ts_CumGammaM_betac_factor - ts_CumGammaMminus1_betac_factor) * (h2c_factor - rb_minRad_2c_factor);
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
                                        a_uFP33_values[r_bin] = CapB * uFP33_multiplier * (c_coefficient / b2_uFP33_factor) * ts_theta_M * ts_CumGammaM_betac_factor * (h3c_factor - rb_minRad_3c_factor);
                                    }

                                    // Calculate term for s_uFP33 increment for rmin (excluding transition deactivation microfractures)
                                    if (ts_gamma_InvBeta_M > 0)
                                        s_uFP33_values[r_bin] += (CapB / b2_uFP33_factor) * uFP33_multiplier * (ts_suF_deactivation_multiplier / ts_gamma_InvBeta_M) *
                                            (ts_CumGammaM_betac_factor - ts_CumGammaMminus1_betac_factor) * (h3c_factor - rb_minRad_3c_factor);
                                    else
                                        s_uFP33_values[r_bin] += CapB * uFP33_multiplier * (c_coefficient / b2_uFP33_factor) * (ts_theta_Mminus1 - ts_theta_M) * ts_CumGammaM_betac_factor * (h3c_factor - rb_minRad_3c_factor);
                                    //s_uFP33_values[r_bin] += CapB * uFP33_multiplier * (ts_suF_deactivation_multiplier / b2_uFP33_factor) * (ts_CumGammaM_betac_factor - ts_CumGammaMminus1_betac_factor) * (h3c_factor - rb_minRad_3c_factor);
                                }
                            }
                        } // End loop through all r_bins

                        // Calculate increment for the volumetric density of transition deactivation microfractures
                        double transition_deactivation_term = 0;
                        if (ts_CumhGammaM < double.PositiveInfinity)
                            transition_deactivation_term = (ts_theta_Mminus1 - ts_theta_dashed_Mminus1) * (ts_CumhGammaM_betac_factor - ts_CumhGammaMminus1_betac_factor);

                        // Add terms for transition deactivation microfractures to static fracture density values
                        for (int r_bin = 0; r_bin < no_r_bins + 1; r_bin++)
                        {
                            s_uFP30_values[r_bin] += CapB * transition_deactivation_term;
                            s_uFP32_values[r_bin] += CapB * uFP32_multiplier * Math.Pow(max_uF_radius, 2) * transition_deactivation_term;
                            s_uFP33_values[r_bin] += CapB * uFP33_multiplier * Math.Pow(max_uF_radius, 3) * transition_deactivation_term;
                        }
                    } // End loop through the previous timesteps M

                } // End if b=2
                // If b!=2 then we must calculate the cumulative density values numerically by summing the components in each r_bin, using the microfracture density distribution arrays
                else
                {
                    // Since the density values are cumulative, we will loop backwards through the bins, incrementing the cumulative densities with the density values calculated for each bin
                    // Create local variables to store the ongoing cumulative density values
                    double cumulative_a_uFP30 = 0;
                    double cumulative_s_uFP30 = 0;
                    double cumulative_a_uFP32 = 0;
                    double cumulative_s_uFP32 = 0;
                    double cumulative_a_uFP33 = 0;
                    double cumulative_s_uFP33 = 0;

                    // Loop through the backwards through the r_bins
                    for (int r_bin = no_r_bins; r_bin >= 0; r_bin--)
                    {
                        // Get minimum radius size in the current r-bin
                        double rb_minRad = uF_radii[r_bin];
                        // If the minimum bin size is less than the minimum cutoff, set it to equal the minimum cutoff
                        if (rb_minRad < rmin_cutoff) rb_minRad = rmin_cutoff;

                        // Increment the cumulative uFP30 values and copy them into the cumulative density arrays
                        cumulative_a_uFP30 += MicroFractures.a_DP30[r_bin];
                        cumulative_s_uFP30 += MicroFractures.s_DP30[r_bin];
                        a_uFP30_values[r_bin] = cumulative_a_uFP30;
                        s_uFP30_values[r_bin] = cumulative_s_uFP30;

                        // Increment the cumulative uFP32 values and copy them into the cumulative density arrays
                        cumulative_a_uFP32 += uFP32_multiplier * Math.Pow(rb_minRad, 2) * MicroFractures.a_DP30[r_bin];
                        cumulative_s_uFP32 += uFP32_multiplier * Math.Pow(rb_minRad, 2) * MicroFractures.s_DP30[r_bin];
                        a_uFP32_values[r_bin] = cumulative_a_uFP32;
                        s_uFP32_values[r_bin] = cumulative_s_uFP32;

                        // Increment the cumulative uFP33 values and copy them into the cumulative density arrays
                        cumulative_a_uFP33 += uFP33_multiplier * Math.Pow(rb_minRad, 3) * MicroFractures.a_DP30[r_bin];
                        cumulative_s_uFP33 += uFP33_multiplier * Math.Pow(rb_minRad, 3) * MicroFractures.s_DP30[r_bin];
                        a_uFP33_values[r_bin] = cumulative_a_uFP33;
                        s_uFP33_values[r_bin] = cumulative_s_uFP33;

                    } // End loop through all r_bins

                } // End if b!=2

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
            // Create new Microfracture and Macrofracture objects; this will override the microfracture radii and macrofracture halflength arrays
            MicroFractures = new MicrofractureData(gbc, this);
            IPlus_halfMacroFractures = new MacrofractureData(gbc, this);
            IMinus_halfMacroFractures = new MacrofractureData(gbc, this);

            // Set initial microfracture densities according to specified density and distribution coefficients
            double max_uF_radius = gbc.MaximumMicrofractureRadius;
            double rmin_cutoff = gbc.PropControl.minImplicitMicrofractureRadius;
            // NB Since we do not know the number of calculation bins for r at this stage, we can only calculate uFP32 and uFP33 if rmin_cutoff > 0
            if ((rmin_cutoff > 0) && (rmin_cutoff < max_uF_radius))
            {
                MicroFractures.a_P30_total = CapB * (Math.Pow(rmin_cutoff, -c_coefficient) - Math.Pow(rmin_cutoff, -c_coefficient));
                //if (c_coefficient == 1)
                //    MicroFractures.a_P31_total = 2 * CapB * c_coefficient * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                //else
                //    MicroFractures.a_P31_total = 2 * CapB * (c_coefficient / (1 - c_coefficient)) * (Math.Pow(max_uF_radius, 1 - c_coefficient) - Math.Pow(rmin_cutoff, 1 - c_coefficient));
                if (c_coefficient == 2)
                    MicroFractures.a_P32_total = Math.PI * CapB * c_coefficient * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                else
                    MicroFractures.a_P32_total = Math.PI * CapB * (c_coefficient / (2 - c_coefficient)) * (Math.Pow(max_uF_radius, 2 - c_coefficient) - Math.Pow(rmin_cutoff, 2 - c_coefficient));
                if (c_coefficient == 3)
                    MicroFractures.a_P33_total = (4d / 3d) * Math.PI * CapB * c_coefficient * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                else
                    MicroFractures.a_P33_total = (4d / 3d) * Math.PI * CapB * (c_coefficient / (3 - c_coefficient)) * (Math.Pow(max_uF_radius, 3 - c_coefficient) - Math.Pow(rmin_cutoff, 3 - c_coefficient));
                //if (c_coefficient == 4)
                //    MicroFractures.a_P34_total = (8d / 4d) * Math.PI * CapB * c_coefficient * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                //else
                //    MicroFractures.a_P34_total = (8d / 4d) * Math.PI * CapB * (c_coefficient / (4 - c_coefficient)) * (Math.Pow(max_uF_radius, 4 - c_coefficient) - Math.Pow(rmin_cutoff, 4 - c_coefficient));
                //if (c_coefficient == 5)
                //    MicroFractures.a_P35_total = (16d / 5d) * Math.PI * CapB * c_coefficient * (Math.Log(max_uF_radius) - Math.Log(rmin_cutoff));
                //else
                //    MicroFractures.a_P35_total = (16d / 5d) * Math.PI * CapB * (c_coefficient / (5 - c_coefficient)) * (Math.Pow(max_uF_radius, 5 - c_coefficient) - Math.Pow(rmin_cutoff, 5 - c_coefficient));
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
            // Set the flag to deactivate the fracture set at the start of the next timestep to false
            DeactivateNextTimestep = false;
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
            shearDisplacementVector = new VectorXYZ(0, 0, 0);

            // Set the initial driving stress vectors to (0,0,0)
            DrivingStressVector = new VectorXYZ(0, 0, 0);
            MFDisplacementAdjustedDrivingStressVector = new VectorXYZ(0, 0, 0);

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
