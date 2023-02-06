using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Contains data representing the stress and strain state of a single gridblock, including tensors for the in situ stress, elastic and inelastic strain
    /// </summary>
    class StressStrainState
    {
        // References to external objects
        /// <summary>
        /// Reference to parent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;

        // Lithostatic stress state and pore fluid pressure controls
        // These define the initial stress state, prior to deformation 
        /// <summary>
        /// Gravitational constant (m/s2)
        /// </summary>
        public const double Gravity = 9.81;
        /// <summary>
        /// Mean bulk density of overlying rock, including pore fluid (kg/m3)
        /// </summary>
        public double MeanOverlyingBulkRockDensity { get; private set; }
        /// <summary>
        /// Pore fluid density (kg/m3)
        /// </summary>
        public double FluidDensity { get; private set; }
        /// <summary>
        /// Lithostatic stress: component of vertical stress due to the weight of the overlying rock column, including pore fluid (Pa)
        /// Normal this is equal to the vertical component of absolute stress SigmaZZ, although they may diverge due to stress arching
        /// </summary>
        public double LithostaticStress { get { return gbc.DepthAtDeformation * MeanOverlyingBulkRockDensity * Gravity; } }
        /// <summary>
        /// Hydrostatic fluid pressure: component of pore fluid pressure due to the weight of the overlying fluid column (Pa)
        /// </summary>
        private double HydrostaticFluidPressure { get { return gbc.DepthAtDeformation * FluidDensity * Gravity; } }
        /// <summary>
        /// Fluid overpressure: component of additional pore fluid pressure above hydrostatic gradient (Pa)
        /// </summary>
        private double FluidOverpressure { get; set; }
        /// <summary>
        /// Pore fluid pressure (Pa)
        /// </summary>
        public double P_f { get { return HydrostaticFluidPressure + FluidOverpressure; } }
        /// <summary>
        /// Terzaghi effective lithostatic stress (Pa)
        /// </summary>
        /// <returns></returns>
        public double LithostaticStress_eff_Terzaghi { get { return LithostaticStress - P_f; } }
        /// <summary>
        /// Biot effective lithostatic stress (Pa)
        /// </summary>
        /// <returns></returns>
        public double LithostaticStress_eff_Biot { get { return LithostaticStress - (gbc.MechProps.Biot * P_f); } }

        // Rates of change of lithostatic stress state and pore fluid pressure  controls
        /// <summary>
        /// Rate of uplift and erosion; must be specified in SI units (m/s)
        /// This will control both lithostatic stress and hydrostatic fluid pressure
        /// </summary>
        public double UpliftRate { private get; set; }
        /// <summary>
        /// Rate of change of fluid overpressure; must be specified in SI units (Pa/s)
        /// </summary>
        public double FluidOverpressureRate { private get; set; }
        /// <summary>
        /// Rate of change of lithostatic stress, due to uplift and erosion/subsidence and burial (Pa/s)
        /// Normal this is equal to the vertical component of the rate of change of absolute stress SigmaZZ_dashed, although they may diverge due to stress arching
        /// </summary>
        public double LithostaticStress_dashed { get { return -UpliftRate * MeanOverlyingBulkRockDensity * Gravity; } }
        /// <summary>
        /// Rate of change of fluid pressure (Pa/s)
        /// </summary>
        public double P_f_dashed { get { return (-UpliftRate * FluidDensity * Gravity) + FluidOverpressureRate; } }
        /// <summary>
        /// Rate of change of Terzaghi effective lithostatic stress (Pa/s)
        /// </summary>
        /// <returns></returns>
        public double LithostaticStress_eff_Terzaghi_dashed { get { return LithostaticStress_dashed - P_f_dashed; } }
        /// <summary>
        /// Rate of change of Biot effective lithostatic stress (Pa/s)
        /// </summary>
        /// <returns></returns>
        public double LithostaticStress_eff_Biot_dashed { get { return LithostaticStress_dashed - (gbc.MechProps.Biot * P_f_dashed); } }

        // Initial stress and strain state
        /// <summary>
        /// Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress
        /// </summary>
        private double initialstressrelaxation;
        /// <summary>
        /// Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress
        /// </summary>
        public double InitialStressRelaxation { get { return initialstressrelaxation; } private set { if (value > 1) value = 1; if (value < 0) value = 0; initialstressrelaxation = value; } }

        // Geothermal controls
        /// <summary>
        /// Geothermal gradient (degK/m)
        /// </summary>
        public double GeothermalGradient { get; set; }

        // Effective stress state
        /// <summary>
        /// Current in situ Terzaghi effective stress tensor
        /// </summary>
        private Tensor2S sigma_eff;
        /// <summary>
        /// Rate of change of in situ Terzaghi effective stress tensor in model time units
        /// </summary>
        private Tensor2S sigma_eff_dashed;
        /// <summary>
        /// Current in situ Terzaghi effective stress tensor
        /// </summary>
        public Tensor2S Sigma_eff { get { return sigma_eff; } private set { sigma_eff = new Tensor2S(value); } }
        /// <summary>
        /// Rate of change of in situ Terzaghi effective stress tensor; must be specified in SI units (Pa/s)
        /// </summary>
        public Tensor2S Sigma_eff_dashed { get { return sigma_eff_dashed; } private set { sigma_eff_dashed = new Tensor2S(value); } }
        /// <summary>
        /// Current in situ absolute stress tensor
        /// </summary>
        public Tensor2S Sigma { get { return Sigma_eff + new Tensor2S(P_f, P_f, P_f, 0, 0, 0); } }
        /// <summary>
        /// Rate of change of in situ absolute stress tensor
        /// </summary>
        public Tensor2S Sigma_dashed { get { return Sigma_eff_dashed + new Tensor2S(P_f_dashed, P_f_dashed, P_f_dashed, 0, 0, 0); } }
        /// <summary>
        /// Current in situ Biot effective stress tensor
        /// </summary>
        public Tensor2S Sigma_eff_Biot { get { double BiotCorrection = (1 - gbc.MechProps.Biot) * P_f; return Sigma_eff + new Tensor2S(BiotCorrection, BiotCorrection, BiotCorrection, 0, 0, 0); } }
        /// <summary>
        /// Rate of change of in situ Biot effective stress tensor
        /// </summary>
        public Tensor2S Sigma_eff_dashed_Biot { get { double BiotCorrection = (1 - gbc.MechProps.Biot) * P_f_dashed; return Sigma_eff_dashed + new Tensor2S(BiotCorrection, BiotCorrection, BiotCorrection, 0, 0, 0); } }

        // Cumulative strain data
        /// <summary>
        /// Cumulative inelastic (relaxed) strain in host rock
        /// </summary>
        public Tensor2S rel_Epsilon_r { get { return tot_Epsilon - rel_Epsilon_f - el_Epsilon_noncompactional; } }
        /// <summary>
        /// Cumulative inelastic (relaxed) strain on all fractures
        /// </summary>
        public Tensor2S rel_Epsilon_f { get; set; }
        /// <summary>
        /// Total cumulative strain tensor, including noncompactional elastic and inelastic (relaxed) strain; does not include initial compactional strain
        /// </summary>
        public Tensor2S tot_Epsilon { get; set; }

        // Elastic strain data
        /// <summary>
        /// Current bulk rock elastic strain tensor, including compactional strain
        /// </summary>
        private Tensor2S el_epsilon;
        /// <summary>
        /// Rate of change of internal elastic strain tensor (includes external loads and strain relaxation)
        /// </summary>
        private Tensor2S el_epsilon_dashed;
        /// <summary>
        /// Current bulk rock compactional strain tensor; this is the implied strain to compensate for prior compaction of the rockmass, and compaction of the grains due to fluid pressure or temperature change; not subject to strain relaxation
        /// </summary>
        private Tensor2S el_epsilon_compactional;
        /// <summary>
        /// Rate of change of bulk rock compactional strain tensor; this is the implied strain to compensate for prior compaction of the rockmass, and compaction of the grains due to fluid pressure or temperature change
        /// </summary>
        private Tensor2S el_epsilon_compactional_dashed;
        /// <summary>
        /// Current bulk rock elastic strain tensor, including compactional strain
        /// </summary>
        public Tensor2S el_Epsilon { get { return el_epsilon; } private set { el_epsilon = new Tensor2S(value); } }
        /// <summary>
        /// Rate of change of internal elastic strain tensor (includes applied external strain and strain relaxation); must be specified in SI units (/s)
        /// </summary>
        public Tensor2S el_Epsilon_dashed { get { return el_epsilon_dashed; } set { el_epsilon_dashed = new Tensor2S(value); } }
        /// <summary>
        /// Current bulk rock compactional strain tensor; this is the implied strain to compensate for prior compaction of the rockmass, and compaction of the grains due to fluid pressure or temperature change; not subject to strain relaxation
        /// </summary>
        public Tensor2S el_Epsilon_compactional { get { return new Tensor2S(el_epsilon_compactional); } private set { el_epsilon_compactional = new Tensor2S(value); } }
        /// <summary>
        /// Rate of change of bulk rock compactional strain tensor; this is the implied strain to compensate for prior compaction of the rockmass, and compaction of the grains due to fluid pressure or temperature change
        /// </summary>
        public Tensor2S el_Epsilon_compactional_dashed { get { return new Tensor2S(el_epsilon_compactional_dashed); } set { el_epsilon_compactional_dashed = new Tensor2S(value); } }
        /// <summary>
        /// Current bulk rock elastic strain, excluding compactional strain; this is the elastic strain that is subject to strain relaxation
        /// </summary>
        public Tensor2S el_Epsilon_noncompactional { get { return el_Epsilon - el_epsilon_compactional; } }
        /// <summary>
        /// Rate of change of bulk rock elastic strain, excluding compactional strain
        /// </summary>
        public Tensor2S el_Epsilon_noncompactional_dashed { get { return el_Epsilon_dashed - el_epsilon_compactional_dashed; } }
        // Elastic strain partitioning
        // NB these calculations are not very efficient, and it may be quicker to calculate the elastic strain partitioning directly from the stress tensor, using the appropriate compliance tensor
        /// <summary>
        /// Current elastic strain locally in host rock
        /// </summary>
        public Tensor2S el_Epsilon_r
        {
            get
            {
                switch (gbc.PropControl.StressDistributionCase)
                {
                    case StressDistribution.EvenlyDistributedStress:
                        // In the evenly distributed stress scenario, the local elastic strain in the host rock is uniform,
                        // and proportional to the ratio of the compliance tensor for intact host rock and the bulk rock compliance tensor
                        return (gbc.MechProps.S_r / gbc.S_beff) * el_Epsilon;
                    case StressDistribution.StressShadow:
                        // In the stress shadow scenario, the local elastic strain in the host rock (outside the stress shadows) is equal to the total elastic strain
                        // NB within the stress shadows, the local elastic strain in the host rock is zero
                        return el_Epsilon;
                    case StressDistribution.DuctileBoundary:
                        // Not yet implemented - return total elastic strain
                        return el_Epsilon;
                    default:
                        // Return total elastic strain
                        return el_Epsilon;
                }
            }
        }
        /// <summary>
        /// Current elastic strain averaged across host rock
        /// </summary>
        public Tensor2S el_Epsilon_rmean
        {
            get
            {
                switch (gbc.PropControl.StressDistributionCase)
                {
                    case StressDistribution.EvenlyDistributedStress:
                        // In the evenly distributed stress scenario, the mean elastic strain in the host rock is uniform, and equal to the local elastic strain
                        // and proportional to the ratio of the compliance tensor for intact host rock and the bulk rock compliance tensor
                        return el_Epsilon_r;
                    case StressDistribution.StressShadow:
                        // In the stress shadow scenario, the mean elastic strain in the host rock is given by the total elastic strain minus the strain on the fractures
                        // NB within the stress shadows, the local elastic strain in the host rock is zero
                        return el_Epsilon - el_Epsilon_f;
                    case StressDistribution.DuctileBoundary:
                        // Not yet implemented - return total elastic strain minus the strain on the fractures
                        return el_Epsilon - el_Epsilon_f;
                    default:
                        // Return total elastic strain minus the strain on the fractures
                        return el_Epsilon - el_Epsilon_f;
                }
            }
        }
        /// <summary>
        /// Current elastic strain on all fractures
        /// </summary>
        public Tensor2S el_Epsilon_f
        {
            get
            {
                // Regardless of the stress distribution scenario, the elastic strain on the fractures is proportional to the ratio of the fracture compliance tensor to the bulk rock compliance tensor
                return (gbc.S_F / gbc.S_beff) * el_Epsilon;
            }
        }

        // Functions to update and recalculate the stress and strain tensors
        // The first set of functions are used to add a specified load to the effective stress and strain tensors
        // This generally cause misalignment of the effective stress and strain tensors (they will no longer be consistent according to Hooke's Law)
        // The second set of functions can then be used to realign the effective stress and strain tensors so they are consistent according to Hooke's Law
        // NB this cannot be done automatically as it requires data on the bulk rock stiffness (either elastic moduli or compliance tensor) which may change through time
        // Functions to update the Terzaghi effective stress and elastic strain tensors to take account of applied deformation
        /// <summary>
        /// Update the effective stress and bulk rock elastic strain tensors, and fluid pressure, based on the respective rate of change tensors and the timestep duration
        /// </summary>
        /// <param name="TimestepDuration">Timestep duration in model time units</param>
        public void UpdateStressStrainState(double TimestepDuration)
        {
            // Change in effective stress tensor is given by the rate of change of effective stress tensor multiplied by the timestep duration
            sigma_eff += (TimestepDuration * sigma_eff_dashed);
            // Change in bulk rock elastic strain tensor is given by the rate of change of elastic strain tensor multiplied by the timestep duration
            el_epsilon += (TimestepDuration * el_epsilon_dashed);
            // Change in bulk rock compactional elastic strain tensor is given by the rate of change of compactional elastic strain tensor multiplied by the timestep duration
            el_epsilon_compactional += (TimestepDuration * el_epsilon_compactional_dashed);
            // Change in fluid overpressure is given by the rate of change of fluid overpressure multiplied by the timestep duration
            FluidOverpressure += (TimestepDuration * FluidOverpressureRate);
            // NB Uplift must be applied within the gridblock object
        }
        /// <summary>
        /// Adjust the stress and strain tensors to a specified stress and strain state
        /// </summary>
        /// <param name="StressStrainState_in">DeformationEpisodeStressStrainInitialiser object containing the required absolute vertical stress, pore fluid pressure and elastic strain tensor (only XX, YY and XY components will be used)</param>
        public void SetStressStrainState(DeformationEpisodeStressInitialiser StressStrainState_in)
        {
            // Reset the fluid pressure, if this has been supplied
            // NB This must be done first as it will be used to calculate the effective vertical stress
            if (StressStrainState_in.SetInitialFluidPressure)
                FluidOverpressure = StressStrainState_in.FluidPressure - HydrostaticFluidPressure;

            // Reset the vertical effective stress, if this has been supplied
            if (StressStrainState_in.SetInitialAbsoluteVerticalStress)
                sigma_eff.Component(Tensor2SComponents.ZZ, StressStrainState_in.AbsoluteVerticalStress - P_f);

            // Reset the horizontal components of the elastic strain tensor, if these have been supplied
            if (StressStrainState_in.SetInitialStressTensor)
            {
                Tensor2S HorizontalStrain_in = StressStrainState_in.AbsoluteStress;
                el_epsilon.Component(Tensor2SComponents.XX, HorizontalStrain_in.Component(Tensor2SComponents.XX));
                el_epsilon.Component(Tensor2SComponents.YY, HorizontalStrain_in.Component(Tensor2SComponents.YY));
                el_epsilon.Component(Tensor2SComponents.XY, HorizontalStrain_in.Component(Tensor2SComponents.XY));
            }
        }
        // Functions to realign the Terzaghi effective stress and elastic strain tensors
        /// <summary>
        /// Recalculate the effective stress and rate of change of effective stress tensors from the elastic strain and strain rate tensors and the effective vertical stress, using the supplied bulk rock compliance tensor
        /// </summary>
        /// <param name="ComplianceTensor">Bulk rock compliance tensor (may be anisotropic)</param>
        public void RecalculateEffectiveStressState(Tensor4_2Sx2S ComplianceTensor)
        {
            // If there is stress arching, vertical stress will no longer be equal to the lithostatic overburden so must be taken from the ZZ component of the effective stress tensor
            // The effective stress tensor calculated from the stress rate tensor and timestep duration, using the UpdateStressStrainState(double TimestepDuration) function
            //sigma_eff.Component(Tensor2SComponents.ZZ, Sigma_v_eff_Terzaghi);

            // Use the Tensor4_2Sx2S.PartialInversion function to recalculate the tensors for current effective stress and rate of change of effective stress based on the elastic strain and strain rate tensors
            // This function will keep the vertical effective stress constant, but recalculate all other components of the stress tensors
            ComplianceTensor.PartialInversion(ref el_epsilon, ref sigma_eff);
            ComplianceTensor.PartialInversion(ref el_epsilon_dashed, ref sigma_eff_dashed);
        }
        /// <summary>
        /// Recalculate the effective stress and rate of change of effective stress tensors from the elastic strain and strain rate tensors and the effective vertical stress, assuming an isotropic bulk rock compliance tensor
        /// </summary>
        /// <param name="E_r">Bulk rock Young's modulus (isotropic)</param>
        /// <param name="Nu_r">Bulk rock Poisson's ratio (isotropic)</param>
        public void RecalculateEffectiveStressState(double E_r, double Nu_r)
        {
            // Calculate helper variables
            double Nur_OneMinusNur = Nu_r / (1 - Nu_r);
            double Er_OneMinusNur2 = E_r / (1 - Math.Pow(Nu_r, 2));
            double Er_OnePlusNur = E_r / (1 + Nu_r);

            // Cache the elastic strain tensor components locally
            double e_xx = el_epsilon.Component(Tensor2SComponents.XX);
            double e_yy = el_epsilon.Component(Tensor2SComponents.YY);
            double e_xy = el_epsilon.Component(Tensor2SComponents.XY);
            double e_yz = el_epsilon.Component(Tensor2SComponents.YZ);
            double e_zx = el_epsilon.Component(Tensor2SComponents.ZX);

            // Calculate current effective stress tensor components
            // If there is stress arching, vertical stress will no longer be equal to the lithostatic overburden so must be taken from the ZZ component of the effective stress tensor
            // The effective stress tensor calculated from the stress rate tensor and timestep duration, using the UpdateStressStrainState(double TimestepDuration) function
            //double sveff = Sigma_v_eff;
            double sveff = sigma_eff.Component(Tensor2SComponents.ZZ);
            double sveff_factor = sveff * Nur_OneMinusNur;
            double sigma_xx = (Er_OneMinusNur2 * (e_xx + (Nu_r * e_yy))) + sveff_factor;
            double sigma_yy = (Er_OneMinusNur2 * (e_yy + (Nu_r * e_xx))) + sveff_factor;
            sigma_eff.Component(Tensor2SComponents.XX, sigma_xx);
            sigma_eff.Component(Tensor2SComponents.YY, sigma_yy);
            sigma_eff.Component(Tensor2SComponents.XY, Er_OnePlusNur * e_xy);
            sigma_eff.Component(Tensor2SComponents.YZ, Er_OnePlusNur * e_yz);
            sigma_eff.Component(Tensor2SComponents.ZX, Er_OnePlusNur * e_zx);

            // Calculate zz component of elastic strain tensor
            double e_zz = (sveff - (Nu_r * sigma_xx) - (Nu_r * sigma_yy)) / E_r;
            el_epsilon.Component(Tensor2SComponents.ZZ, e_zz);

            // Cache the elastic strain rate tensor components locally
            double ed_xx = el_epsilon_dashed.Component(Tensor2SComponents.XX);
            double ed_yy = el_epsilon_dashed.Component(Tensor2SComponents.YY);
            double ed_xy = el_epsilon_dashed.Component(Tensor2SComponents.XY);
            double ed_yz = el_epsilon_dashed.Component(Tensor2SComponents.YZ);
            double ed_zx = el_epsilon_dashed.Component(Tensor2SComponents.ZX);

            // Calculate current effective stress tensor components
            // If there is stress arching, vertical stress may not be equal to the lithostatic overburden so must be taken from the ZZ component of the stress rate tensor
            //double sveffd = Sigma_v_eff_dashed;
            double sveffd = sigma_eff_dashed.Component(Tensor2SComponents.ZZ);
            double sveffd_factor = sveffd * Nur_OneMinusNur;
            double sigmad_xx = (Er_OneMinusNur2 * (ed_xx + (Nu_r * ed_yy))) + sveffd_factor;
            double sigmad_yy = (Er_OneMinusNur2 * (ed_yy + (Nu_r * ed_xx))) + sveffd_factor;
            sigma_eff_dashed.Component(Tensor2SComponents.XX, sigmad_xx);
            sigma_eff_dashed.Component(Tensor2SComponents.YY, sigmad_yy);
            sigma_eff_dashed.Component(Tensor2SComponents.XY, Er_OnePlusNur * ed_xy);
            sigma_eff_dashed.Component(Tensor2SComponents.YZ, Er_OnePlusNur * ed_yz);
            sigma_eff_dashed.Component(Tensor2SComponents.ZX, Er_OnePlusNur * ed_zx);

            // Calculate zz component of elastic strain rate tensor
            double ed_zz = (sveffd - (Nu_r * sigmad_xx) - (Nu_r * sigmad_yy)) / E_r;
            el_epsilon_dashed.Component(Tensor2SComponents.ZZ, ed_zz);
        }

        // Reset and data input functions
        /// <summary>
        /// Reset the elastic strain tensor to the initial compactional strain, and reset the strain rate tensor
        /// </summary>
        private void Reset_Elastic_Strain()
        {
            // Cache elastic constants for intact rock
            double E_r = gbc.MechProps.E_r;
            double Nu_r = gbc.MechProps.Nu_r;

            // Recalculate initial compactional strain
            double horizontalCompactionalStrain = InitialStressRelaxation * (1 - (2 * Nu_r)) * (LithostaticStress_eff_Terzaghi / E_r);
            double verticalCompactionalStrain = -InitialStressRelaxation * ((2 * Nu_r * (1 - (2 * Nu_r))) / (1 - Nu_r)) * (LithostaticStress_eff_Terzaghi / E_r);
            el_epsilon_compactional = new Tensor2S(horizontalCompactionalStrain, horizontalCompactionalStrain, verticalCompactionalStrain, 0, 0, 0);

            // Reset the bulk rock elastic strain to the initial compactional strain plus the lithostatic strain
            double lithostaticStrain = ((1 + Nu_r) * (1 - (2 * Nu_r)) / (1 - Nu_r)) * (LithostaticStress_eff_Terzaghi / E_r);
            el_epsilon = new Tensor2S(0, 0, lithostaticStrain, 0, 0, 0) + el_Epsilon_compactional;

            // Reset the rate of change of elastic strain and compactional strain tensors to zero
            el_epsilon_dashed = new Tensor2S();
            el_epsilon_compactional_dashed = new Tensor2S();
        }
        /// <summary>
        /// Reset the stress tensor to the initial lithostatic stress state with no applied horizontal strain, and reset the rate of change of stress tensor
        /// </summary>
        private void Reset_Stress()
        {
            // Reset the stress tensor to the initial stress with no applied horizontal strain (but including initial stress relaxation)
            double Nu_r = gbc.MechProps.Nu_r;
            double Sigma_h0_eff = (((InitialStressRelaxation * (1 - Nu_r)) + ((1 - InitialStressRelaxation) * Nu_r)) / (1 - Nu_r)) * LithostaticStress_eff_Terzaghi;
            sigma_eff = new Tensor2S(Sigma_h0_eff, Sigma_h0_eff, LithostaticStress_eff_Terzaghi, 0, 0, 0);

            // Reset the rate of change of stress tensor
            sigma_eff_dashed = new Tensor2S();

            // Reset the rate of change of fluid pressure
            FluidOverpressureRate = 0;

            // Reset the uplift rate
            UpliftRate = 0;
        }
        /// <summary>
        /// Reset the total cumulative strain tensors to zero, reset the elastic strain and stress tensors to initial compactional state, and reset the strain and stress rate tensors to zero
        /// </summary>
        public void ResetStressStrainState()
        {
            // Reset the total cumulative strain and cumulative inelastic fracture strain tensors
            tot_Epsilon = new Tensor2S();
            rel_Epsilon_f = new Tensor2S();

            // Reset the elastic strain tensor to initial compactional strain and reset the strain rate tensor
            Reset_Elastic_Strain();

            // Reset the stress tensor to the initial stress with no applied horizontal strain, and reset the rate of change of stress tensor
            Reset_Stress();
        }
        /// <summary>
        /// Set lithostatic stress, fluid pressure and proportion of initial compaction-induced differential stress relaxation, and reset stress and strain tensors to initial conditions
        /// </summary>
        /// <param name="MeanOverlyingSedimentDensity_in">Mean bulk density of overlying rock (kg/m3)</param>
        /// <param name="FluidDensity_in">Pore fluid density (kg/m3)</param>
        /// <param name="FluidOverpressure_in">Fluid overpressure (i.e. pore pressure above hydrostatic gradient) (Pa)</param>
        /// <param name="InitialStressRelaxation_in">Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress</param>
        public void SetInitialStressStrainState(double MeanOverlyingSedimentDensity_in, double FluidDensity_in, double FluidOverpressure_in, double InitialStressRelaxation_in)
        {
            // The stress state is calculated automatically on the basis of mean overlying bulk rock density, fluid density, overpressure and depth of burial
            // Absolute vertical (lithostatic) stress is controlled by mean overlying sediment density
            MeanOverlyingBulkRockDensity = MeanOverlyingSedimentDensity_in;
            // Fluid pressure is controlled by fluid density and overpressure
            FluidDensity = FluidDensity_in;
            FluidOverpressure = FluidOverpressure_in;
            // Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress
            InitialStressRelaxation = InitialStressRelaxation_in;

            // Reset the total cumulative strain tensors to zero, reset the elastic strain and stress tensors to initial compactional state, and reset the strain and stress rate tensors to zero
            ResetStressStrainState();
        }

        // Constructors
        /// <summary>
        /// Default Constructor: calculate default lithostatic stress and fluid pressure for gridblock point_z
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        public StressStrainState(GridblockConfiguration gbc_in)
            : this(gbc_in, 2250, 1000, 0, 0, 0.03)
        {
            // Defaults:

            // Absolute vertical (lithostatic) stress: default overlying rock bulk density 2250kg/m3
            // Fluid pressure: default fluid density 1000kg/m3
            // Fluid pressure: default fluid overpressure 0Pa
            // Proportion of initial compaction-induced differential stress relaxation: default 0 (elastic equilibrium)
            // Geothermal gradient: default 0.03degK/m
        }
        /// <summary>
        /// Constructor: input mean overlying bulk rock and fluid density, fluid overpressure, proportion of initial compaction-induced differential stress relaxation and geothermal gradient
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        /// <param name="MeanOverlyingSedimentDensity_in">Mean bulk density of overlying rock (kg/m3)</param>
        /// <param name="FluidDensity_in">Pore fluid density (kg/m3)</param>
        /// <param name="FluidOverpressure_in">Fluid overpressure (i.e. pore pressure above hydrostatic gradient) (Pa)</param>
        /// <param name="InitialStressRelaxation_in">Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress</param>
        /// <param name="GeothermalGradient_in">Geothermal gradient (degK/m)</param>
        public StressStrainState(GridblockConfiguration gbc_in, double MeanOverlyingSedimentDensity_in, double FluidDensity_in, double FluidOverpressure_in, double InitialStressRelaxation_in, double GeothermalGradient_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set initial stress and strain state
            SetInitialStressStrainState(MeanOverlyingSedimentDensity_in, FluidDensity_in, FluidOverpressure_in, InitialStressRelaxation_in);

            // Set geothermal gradient
            GeothermalGradient = GeothermalGradient_in;
        }
    }
}
