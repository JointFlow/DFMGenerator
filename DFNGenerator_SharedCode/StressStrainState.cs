using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFNGenerator_SharedCode
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
        /// Current bulk rock elastic strain tensor
        /// </summary>
        private Tensor2S el_epsilon;
        /// <summary>
        /// Rate of change of internal elastic strain tensor (includes applied external strain and strain relaxation)
        /// </summary>
        private Tensor2S el_epsilon_dashed;
        /// <summary>
        /// Current bulk rock elastic strain tensor
        /// </summary>
        public Tensor2S el_Epsilon { get { return el_epsilon; } private set { el_epsilon = value; } }
        /// <summary>
        /// Rate of change of internal elastic strain tensor (includes applied external strain and strain relaxation) in model time units
        /// </summary>
        public Tensor2S el_Epsilon_dashed { get { return el_epsilon_dashed; } set { el_epsilon_dashed = value; } }
        /// <summary>
        /// Current bulk rock elastic strain, excluding initial compactional strain; this is the elastic strain that is subject to strain relaxation
        /// </summary>
        public Tensor2S el_Epsilon_noncompactional { get { return el_Epsilon - initialCompactionalStrain; } }
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

        // Initial stress and strain state
        /// <summary>
        /// Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress
        /// </summary>
        private double initialstressrelaxation;
        /// <summary>
        /// Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress
        /// </summary>
        public double InitialStressRelaxation { get { return initialstressrelaxation; } private set { if (value > 1) value = 1; if (value < 0) value = 0; initialstressrelaxation = value; } }
        /// <summary>
        /// Element of implied horizontal strain to compensate for prior compaction of the rockmass; will be zero if initial stress relaxation is zero
        /// </summary>
        public double InitialHorizontalCompactionalStrain { get { return (InitialCompactionalStrain.Component(Tensor2SComponents.XX)); } }
        /// <summary>
        /// Element of implied vertical strain to compensate for prior compaction of the rockmass; will be zero if initial stress relaxation is zero
        /// </summary>
        public double InitialVerticalCompactionalStrain { get { return (InitialCompactionalStrain.Component(Tensor2SComponents.ZZ)); } }
        /// <summary>
        /// Implied horizontal strain to compensate for prior compaction of the rockmass; will be zero if initial stress relaxation is zero
        /// </summary>
        private Tensor2S initialCompactionalStrain;
        /// <summary>
        /// Implied horizontal strain to compensate for prior compaction of the rockmass; will be zero if initial stress relaxation is zero
        /// </summary>
        public Tensor2S InitialCompactionalStrain { get { return new Tensor2S(initialCompactionalStrain); } }
        
        // Stress state invariants
        /// <summary>
        /// Absolute vertical (lithostatic) stress (Pa)
        /// </summary>
        public double Sigma_v { get; private set; }
        /// <summary>
        /// Fluid pressure (Pa)
        /// </summary>
        public double P_f { get; private set; }
        /// <summary>
        /// Effective vertical stress (Pa)
        /// </summary>
        /// <returns></returns>
        public double Sigma_v_eff { get { return Sigma_v - (gbc.MechProps.Biot * P_f); } }

        // Effective stress state
        /// <summary>
        /// Current in situ effective stress tensor
        /// </summary>
        private Tensor2S sigma_eff;
        /// <summary>
        /// Rate of change of in situ effective stress tensor in model time units
        /// </summary>
        private Tensor2S sigma_dashed;
        /// <summary>
        /// Current in situ effective stress tensor
        /// </summary>
        public Tensor2S Sigma_eff { get { return sigma_eff; } private set { sigma_eff = value; } }
        /// <summary>
        /// Rate of change of in situ effective stress tensor
        /// </summary>
        public Tensor2S Sigma_dashed { get { return sigma_dashed; } private set { sigma_dashed = value; } }
        /// <summary>
        /// Recalculate the effective stress and rate of change of effective stress tensors from the elastic strain and strain rate tensors and the effective vertical stress, using the supplied bulk rock compliance tensor
        /// </summary>
        /// <param name="ComplianceTensor">Bulk rock compliance tensor (may be anisotropic)</param>
        public void RecalculateEffectiveStressState(Tensor4_2Sx2S ComplianceTensor)
        {
            // Use the Tensor4_2Sx2S.PartialInversion function to recalculate the tensors for current effective stress and rate of change of effective stress based on the elastic strain and strain rate tensors
            // This function will keep the vertical effective stress constant, but recalculate all other components of the stress tensors
            ComplianceTensor.PartialInversion(ref el_epsilon, ref sigma_eff);
            ComplianceTensor.PartialInversion(ref el_epsilon_dashed, ref sigma_dashed);
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
            double sveff = Sigma_v_eff;
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
            double sveffd = sigma_dashed.Component(Tensor2SComponents.ZZ);
            double sveffd_factor = sveffd * Nur_OneMinusNur;
            double sigmad_xx = (Er_OneMinusNur2 * (ed_xx + (Nu_r * ed_yy))) + sveffd_factor;
            double sigmad_yy = (Er_OneMinusNur2 * (ed_yy + (Nu_r * ed_xx))) + sveffd_factor;
            sigma_dashed.Component(Tensor2SComponents.XX, sigmad_xx);
            sigma_dashed.Component(Tensor2SComponents.YY, sigmad_yy);
            sigma_dashed.Component(Tensor2SComponents.XY, Er_OnePlusNur * ed_xy);
            sigma_dashed.Component(Tensor2SComponents.YZ, Er_OnePlusNur * ed_yz);
            sigma_dashed.Component(Tensor2SComponents.ZX, Er_OnePlusNur * ed_zx);

            // Calculate zz component of elastic strain rate tensor
            double ed_zz = (sveffd - (Nu_r * sigmad_xx) - (Nu_r * sigmad_yy)) / E_r;
            el_epsilon_dashed.Component(Tensor2SComponents.ZZ, ed_zz);
        }

        // Reset and data input functions
        /// <summary>
        /// Update the effective stress and bulk rock elastic strain tensors, based on the respective rate of change tensors and the timestep duration
        /// </summary>
        /// <param name="TimestepDuration">Timestep duration in model time units</param>
        public void UpdateStressStrainState(double TimestepDuration)
        {
            // Change in effective stress tensor is given by the rate of change of effective stress tensor multiplied by the timestep duration
            sigma_eff += (TimestepDuration * sigma_dashed);
            // Change in bulk rock elastic strain tensor is given by the rate of change of elastic strain tensor multiplied by the timestep duration
            el_epsilon += (TimestepDuration * el_epsilon_dashed);
        }
        /// <summary>
        /// Reset the elastic strain tensor to the initial compactional strain, and reset the strain rate tensor
        /// </summary>
        private void Reset_Elastic_Strain()
        {
            // Cache elastic constants for intact rock
            double E_r = gbc.MechProps.E_r;
            double Nu_r = gbc.MechProps.Nu_r;

            // Recalculate initial compactional strain
            double HorizontalCompactionalStrain = InitialStressRelaxation * (1 - (2 * Nu_r)) * (Sigma_v_eff / E_r);
            double VerticalCompactionalStrain = -InitialStressRelaxation * ((2 * Nu_r * (1 - (2 * Nu_r))) / (1 - Nu_r)) * (Sigma_v_eff / E_r);
            initialCompactionalStrain = new Tensor2S(HorizontalCompactionalStrain, HorizontalCompactionalStrain, VerticalCompactionalStrain, 0, 0, 0);

            // Reset the bulk rock elastic strain to the initial compactional strain plus the lithostatic strain
            double LithostaticStrain = ((1 + Nu_r) * (1 - (2 * Nu_r)) / (1 - Nu_r)) * (Sigma_v_eff / E_r);
            el_Epsilon = new Tensor2S(0, 0, LithostaticStrain, 0, 0, 0) + InitialCompactionalStrain;

            // Reset the rate of change of internal elastic strain tensor to zero
            el_Epsilon_dashed = new Tensor2S();
        }
        /// <summary>
        /// Reset the stress tensor to the initial stress with no applied horizontal strain, and reset the rate of change of stress tensor
        /// </summary>
        private void Reset_Stress()
        {


            // Reset the stress tensor to the initial stress with no applied horizontal strain (but including initial stress relaxation)
            double Nu_r = gbc.MechProps.Nu_r;
            double Sigma_h0_eff = (((InitialStressRelaxation * (1 - Nu_r)) + ((1 - InitialStressRelaxation) * Nu_r)) / (1 - Nu_r)) * Sigma_v_eff;
            Sigma_eff = new Tensor2S(Sigma_h0_eff, Sigma_h0_eff, Sigma_v_eff, 0, 0, 0);

            // Reset the rate of change of stress tensor
            Sigma_dashed = new Tensor2S();
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
        /// <param name="Sigma_v_in">Lithostatic stress (i.e. absolute vertical stress) (Pa)</param>
        /// <param name="P_f_in">Fluid pressure (Pa)</param>
        /// <param name="InitialStressRelaxation_in">Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress</param>
        public void SetInitialStressStrainState(double Sigma_v_in, double P_f_in, double InitialStressRelaxation_in)
        {
            // Set stress state invariants
            // Absolute vertical (lithostatic) stress
            Sigma_v = Sigma_v_in;
            // Fluid pressure
            P_f = P_f_in;
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
            : this(gbc_in, (2500 * 9.81 * gbc_in.DepthAtDeformation), (1000 * 9.81 * gbc_in.DepthAtDeformation), 0)
        {
            // Defaults:

            // Absolute vertical (lithostatic) stress: default bulk density 2500kg/m3
            // Fluid pressure: default fluid density 1000kg/m3
            // Proportion of initial compaction-induced differential stress relaxation: 0 (full initial compaction-induced differential stress)
        }
        /// <summary>
        /// Constructor: input lithostatic stress, fluid pressure and proportion of initial compaction-induced differential stress relaxation
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        /// <param name="Sigma_v_in">Lithostatic stress (i.e. absolute vertical stress) (Pa)</param>
        /// <param name="P_f_in">Fluid pressure (Pa)</param>
        /// <param name="InitialStressRelaxation_in">Proportion of initial compaction-induced differential stress relaxation: set to 0 for full initial compaction-induced differential stress, set to 1 for no initial compaction-induced differential stress</param>
        public StressStrainState(GridblockConfiguration gbc_in, double Sigma_v_in, double P_f_in, double InitialStressRelaxation_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set initial stress and strain state
            SetInitialStressStrainState(Sigma_v_in, P_f_in, InitialStressRelaxation_in);
        }
    }
}
