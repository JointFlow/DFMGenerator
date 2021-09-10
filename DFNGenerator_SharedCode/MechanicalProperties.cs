using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for strain relaxation case
    /// </summary>
    public enum StrainRelaxationCase { NoStrainRelaxation, UniformStrainRelaxation, FractureOnlyStrainRelaxation };
    /// <summary>
    /// Enumerator for whether the Subcritical fracture propagation index is less than, equal to or greater than 2. This will affect the equations used in the calculations.
    /// </summary>
    public enum bType { LessThan2, Equals2, GreaterThan2 };

    /// <summary>
    /// Contains data representing the mechanical properties of a single gridblock
    /// </summary>
    class MechanicalProperties
    {
        // References to external objects
        /// <summary>
        /// Reference to parent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;

        // Cache SqrtPi locally so we don't need to keep recalculating it
        private double SqrtPi = Math.Sqrt(Math.PI);

        // Elastic properties of intact rock
        // NB the compliance tensor for intact rock is an isotropic tensor, dependent only on the Young's Modulus and Poisson's ratio
        // It will only change when those parameters change
        // The code could be rewritten so that the intact rock compliance tensor is get only, and is regenerated from the Young's Modulus and Poisson's ratio every time it is called
        // However this would mean recalculating the same tensor in every timestep
        /// <summary>
        /// Young's modulus of intact rock (Pa)
        /// </summary>
        public double E_r { get; private set; }
        /// <summary>
        /// Poisson's ratio of intact rock
        /// </summary>
        public double Nu_r { get; private set; }
        /// <summary>
        /// Biot's coefficient
        /// </summary>
        public double Biot { get; private set; }
        /// <summary>
        /// Compliance tensor for intact rock
        /// </summary>
        public Tensor4_2Sx2S S_r { get; private set; }
        /// <summary>
        /// Set the elastic constants and recreate the compliance tensor for intact rock
        /// </summary>
        /// <param name="E_r_in">Young's Modulus for intact rock (Pa)</param>
        /// <param name="Nu_r_in">Poisson's ratio for intact rock</param>
        /// <param name="Biot_in">Biot's coefficient for intact rock</param>
        public void setElasticProperties(double E_r_in, double Nu_r_in, double Biot_in)
        {
            // Set the elastic constants
            // Young's modulus of intact rock
            E_r = E_r_in;
            // Poisson's ratio of intact rock
            Nu_r = Nu_r_in;
            // Biot's coefficient
            Biot = Biot_in;

            // Recreate the elastic compliance tensor
            S_r = Tensor4_2Sx2S.IsotropicComplianceTensor(E_r_in, Nu_r_in);
        }

        // Elastoplastic properties
        /// <summary>
        /// Crack surface energy (J/m2)
        /// </summary>
        public double Gc { get; set; }
        /// <summary>
        /// Sliding friction coefficient on fracture plane
        /// </summary>
        public double MuFr { get; set; }
        /// <summary>
        /// Critical stress intensity factor (fracture toughness) 
        /// </summary>
        /// <returns></returns>
        public double Kc { get { return Math.Sqrt((Gc * E_r) / (1 - Math.Pow(Nu_r, 2))); } }

        // Viscoelastic properties
        /// <summary>
        /// Strain relaxation time constant for intact rock (s); if <= 0 then no strain relaxation in bulk rock
        /// </summary>
        public double tr { get; set; }
        /// <summary>
        /// Strain relaxation constant for fracture tips (s); if <= 0 then no strain relaxation around fractures
        /// </summary>
        public double tf { get; set; }
        /// <summary>
        /// Returns strain relaxation case: uniform strain relaxation if tr is positive, fracture only strain relaxation if tr is 0, tf is positive, no relaxation if tr and tf both 0
        /// </summary>
        /// <returns></returns>
        public StrainRelaxationCase GetStrainRelaxationCase() { if (tr > 0) return StrainRelaxationCase.UniformStrainRelaxation; else if (tf > 0) return StrainRelaxationCase.FractureOnlyStrainRelaxation; else return StrainRelaxationCase.NoStrainRelaxation; }

        // Brittle properties
        /// <summary>
        /// Critical fracture propagation rate (m/s)
        /// </summary>
        public double CapA { get; set; }
        /// <summary>
        /// Subcritical fracture propagation index
        /// </summary>
        public double b_factor { get; set; }
        /// <summary>
        /// Returns enumerator for whether b is less than, equal to or greater than 2
        /// </summary>
        /// <returns></returns>
        public bType GetbType() { if (b_factor < 2) return bType.LessThan2; else if (b_factor == 2) return bType.Equals2; else return bType.GreaterThan2; }
        /// <summary>
        /// Alternative form of subcritical fracture propagation index; NB set to return 1 if b=2
        /// </summary>
        /// <returns></returns>
        public double beta { get { return (b_factor == 2 ? 1 : 2 / (2 - b_factor)); } }
        /// <summary>
        /// Microfracture propagation constant
        /// </summary>
        /// <returns></returns>
        public double alpha_uF { get { return CapA * Math.Pow((2 / (SqrtPi * Kc)), b_factor); } }
        /// <summary>
        /// Macrofracture propagation constant
        /// </summary>
        /// <param name="h"> Layer point_t (m) </param>
        /// <returns></returns>
        public double alpha_MF { get { return CapA * Math.Pow(Math.Sqrt(2 * gbc.ThicknessAtDeformation) / (SqrtPi * Kc), b_factor); } }

        // Fracture aperture control data - for dynamic and Barton-Bandis aperture, which are independent of dip set
        // NB fracture aperture control data for uniform and size-dependent aperture are dependent on dip set, so are contained in the FractureDipSet object
        /// <summary>
        /// Multiplier for dynamic aperture
        /// </summary>
        public double DynamicApertureMultiplier { get; set; }
        /// <summary>
        /// Joint Roughness Coefficient - used to calculate aperture by Barton-Bandis formula
        /// </summary>
        public double JRC { get; set; }
        /// <summary>
        /// Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock - used to calculate aperture by Barton-Bandis formula
        /// </summary>
        public double UCS_ratio { get; set; }
        /// <summary>
        /// Initial normal strength on fracture (Pa) - used to calculate aperture by Barton-Bandis formula
        /// </summary>
        public double InitialNormalStress { get; set; }
        /// <summary>
        /// Stiffness normal to the fracture, at initial normal stress (Pa/m) - used to calculate aperture by Barton-Bandis formula
        /// </summary>
        public double FractureNormalStiffness { get; set; }
        /// <summary>
        /// Maximum fracture closure (m) - used to calculate aperture by Barton-Bandis formula
        /// </summary>
        public double MaximumClosure { get; set; }

        // Reset and data input functions
        /// <summary>
        /// Function to set strain relaxation time constants, automatically converting from input to SI units
        /// </summary>
        /// <param name="tr_in">Strain relaxation time constant for intact rock (s): set to zero for no uniform strain relaxation</param>
        /// <param name="tf_in">Strain relaxation time constant for fracture tips (s): set to zero for no fracture strain relaxation</param>
        /// <param name="timeUnits_in">Units for time constants</param>
        public void setStrainRelaxationTimeConstants(double tr_in, double tf_in, TimeUnits timeUnits_in)
        {
            // Adjust strain relaxation time constants if not in SI units
            double timeUnits_Modifier = 1;
            switch (timeUnits_in)
            {
                case TimeUnits.second:
                    // In SI units - no change
                    break;
                case TimeUnits.year:
                    timeUnits_Modifier = 365.25d * 24d * 3600d; // Convert from yr to s
                    break;
                case TimeUnits.ma:
                    timeUnits_Modifier = 1000000d * 365.25d * 24d * 3600d; // Convert from ma to s
                    break;
                default:
                    break;
            }
            // Strain relaxation time constant for intact rock
            tr = tr_in * timeUnits_Modifier;
            /// Strain relaxation constant for fracture tips
            tf = tf_in * timeUnits_Modifier;
        }
        /// <summary>
        /// Function to set mechanical properties
        /// </summary>
        /// <param name="E_r_in">Young's Modulus for intact rock (Pa)</param>
        /// <param name="Nu_r_in">Poisson's ratio for intact rock</param>
        /// <param name="Biot_in">Biot's coefficient for intact rock</param>
        /// <param name="Gc_in">Crack surface energy (J/m2)</param>
        /// <param name="MuFr_in">Friction coefficient on fractures</param>
        /// <param name="tr_in">Strain relaxation time constant for intact rock (s): set to zero for no uniform strain relaxation</param>
        /// <param name="tf_in">Strain relaxation time constant for fracture tips (s): set to zero for no fracture strain relaxation</param>
        /// <param name="A_in">Critical fracture propagation rate (m/s)</param>
        /// <param name="b_in">Subcritical fracture propagation index</param>
        /// <param name="timeUnits_in">Units for strain relaxation time constants</param>
        public void setMechanicalProperties(double E_r_in, double Nu_r_in, double Biot_in, double Gc_in, double MuFr_in, double tr_in, double tf_in, double A_in, double b_in, TimeUnits timeUnits_in)
        {
            // Elastic properties
            setElasticProperties(E_r_in, Nu_r_in, Biot_in);

            // Elastoplastic properties
            // Crack surface energy
            Gc = Gc_in;
            // Sliding friction coefficient on fracture plane
            MuFr = MuFr_in;

            // Viscoelastic properties
            setStrainRelaxationTimeConstants(tr_in, tf_in, timeUnits_in);

            // Brittle properties
            // Critical fracture propagation rate
            CapA = A_in;
            // Subcritical fracture propagation index
            b_factor = b_in;
        }
        /// <summary>
        /// Function to set fracture aperture control data for dynamic and Barton-Bandis aperture (which are independent of dip set)
        /// </summary>
        /// <param name="DynamicApertureMultiplier_in">Multiplier for dynamic aperture</param>
        /// <param name="JRC_in">Joint Roughness Coefficient</param>
        /// <param name="UCS_ratio_in">Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock</param>
        /// <param name="UCS_in">Unconfined Compressive Strength of intact rock</param>
        /// <param name="InitialNormalStress_in">Initial normal strength on fracture</param>
        /// <param name="FractureNormalStiffness_in">Stiffness normal to the fracture, at initial normal stress</param>
        /// <param name="MaximumClosure_in">Maximum fracture closure (m)</param>
        public void setFractureApertureControlData(double DynamicApertureMultiplier_in, double JRC_in, double UCS_ratio_in, double InitialNormalStress_in, double FractureNormalStiffness_in, double MaximumClosure_in)
        {
            // Multiplier for dynamic aperture
            DynamicApertureMultiplier = DynamicApertureMultiplier_in;
            // Joint Roughness Coefficient
            JRC = JRC_in;
            // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock
            UCS_ratio = UCS_ratio_in;
            // Initial normal strength on fracture
            InitialNormalStress = InitialNormalStress_in;
            // Stiffness normal to the fracture, at initial normal stress
            FractureNormalStiffness = FractureNormalStiffness_in;
            // Maximum fracture closure (m)
            MaximumClosure = MaximumClosure_in;
        }

        // Constructors
        /// <summary>
        /// Default Constructor: set default values
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        public MechanicalProperties(GridblockConfiguration gbc_in) : this(gbc_in, 1E+10, 0.25, 1, 1000, 0.5, 0, 0, 2000, 3, TimeUnits.second)
        {
            // Defaults:

            // Elastic properties
            // Young's modulus of intact rock: default 10GPa
            // Poisson's ratio of intact rock: default 0.25
            // Biot's coefficient: default 1

            // Elastoplastic properties
            // Crack surface energy: default 1000J/m2
            // Sliding friction coefficient on fracture plane: default 0.5

            // Viscoelastic properties
            // Strain relaxation time constant for intact rock: default 0 (no strain relaxation)
            /// Strain relaxation constant for fracture tips: default 0 (no strain relaxation)

            // Brittle properties
            // Critical fracture propagation rate: default 2000m/s
            // Subcritical fracture propagation index: default 3 (subcritical propagation)
        }
        /// <summary>
        /// Constructor: input intact rock properties values but not fracture aperture control data
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        /// <param name="E_r_in">Young's Modulus for intact rock (Pa)</param>
        /// <param name="Nu_r_in">Poisson's ratio for intact rock</param>
        /// <param name="Biot_in">Biot's coefficient for intact rock</param>
        /// <param name="Gc_in">Crack surface energy (J/m2)</param>
        /// <param name="MuFr_in">Friction coefficient on fractures</param>
        /// <param name="tr_in">Strain relaxation time constant for intact rock (s): set to zero for no uniform strain relaxation</param>
        /// <param name="tf_in">Strain relaxation time constant for fracture tips (s): set to zero for no fracture strain relaxation</param>
        /// <param name="A_in">Critical fracture propagation rate (m/s)</param>
        /// <param name="b_in">Subcritical fracture propagation index</param>
        /// <param name="timeUnits_in">Units for strain relaxation time constants</param>
        public MechanicalProperties(GridblockConfiguration gbc_in, double E_r_in, double Nu_r_in, double Biot_in, double Gc_in, double MuFr_in, double tr_in, double tf_in, double A_in, double b_in, TimeUnits timeUnits_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set mechanical properties
            setMechanicalProperties(E_r_in, Nu_r_in, Biot_in, Gc_in, MuFr_in, tr_in, tf_in, A_in, b_in, timeUnits_in);

            // Set fracture aperture control data to default values
            // Multiplier for dynamic aperture: 1
            // Joint Roughness Coefficient: 10
            // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock: 2
            // Initial normal strength on fracture: 0.2MPa
            // Stiffness normal to the fracture, at initial normal stress: 2.5MPa/mm
            // Maximum fracture closure: 0.5mm
            setFractureApertureControlData(1, 10, 2, 2E+5, 2.5E+9, 0.0005);
        }
        /// <summary>
        /// Constructor: input intact rock properties values and fracture aperture control data
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        /// <param name="E_r_in">Young's Modulus for intact rock (Pa)</param>
        /// <param name="Nu_r_in">Poisson's ratio for intact rock</param>
        /// <param name="Biot_in">Biot's coefficient for intact rock</param>
        /// <param name="Gc_in">Crack surface energy (J/m2)</param>
        /// <param name="MuFr_in">Friction coefficient on fractures</param>
        /// <param name="tr_in">Strain relaxation time constant for intact rock (s): set to zero for no uniform strain relaxation</param>
        /// <param name="tf_in">Strain relaxation time constant for fracture tips (s): set to zero for no fracture strain relaxation</param>
        /// <param name="A_in">Critical fracture propagation rate (m/s)</param>
        /// <param name="b_in">Subcritical fracture propagation index</param>
        /// <param name="timeUnits_in">Units for strain relaxation time constants</param>
        /// <param name="DynamicApertureMultiplier_in">Multiplier for dynamic aperture</param>
        /// <param name="JRC_in">Joint Roughness Coefficient</param>
        /// <param name="UCS_ratio_in">Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock</param>
        /// <param name="UCS_in">Unconfined Compressive Strength of intact rock</param>
        /// <param name="InitialNormalStress_in">Initial normal strength on fracture</param>
        /// <param name="FractureNormalStiffness_in">Stiffness normal to the fracture, at initial normal stress</param>
        /// <param name="MaximumClosure_in">Maximum fracture closure (m)</param>
        public MechanicalProperties(GridblockConfiguration gbc_in, double E_r_in, double Nu_r_in, double Biot_in, double Gc_in, double MuFr_in, double tr_in, double tf_in, double A_in, double b_in, TimeUnits timeUnits_in, double DynamicApertureMultiplier_in, double JRC_in, double UCS_ratio_in, double InitialNormalStress_in, double FractureNormalStiffness_in, double MaximumClosure_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set mechanical properties
            setMechanicalProperties(E_r_in, Nu_r_in, Biot_in, Gc_in, MuFr_in, tr_in, tf_in, A_in, b_in, timeUnits_in);

            // Set fracture aperture control data
            setFractureApertureControlData(DynamicApertureMultiplier_in, JRC_in, UCS_ratio_in, InitialNormalStress_in, FractureNormalStiffness_in, MaximumClosure_in);
        }
    }
}
