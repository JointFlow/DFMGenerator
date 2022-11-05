using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for microfracture and macrofracture output file types
    /// </summary>
    public enum DFNFileType { ASCII, FAB }
    /// <summary>
    /// Enumerator for stress distribution case
    /// </summary>
    public enum StressDistribution { EvenlyDistributedStress, StressShadow, DuctileBoundary }
    /// <summary>
    /// Enumerator to allow flags to be set to be set to False (=None), True(=All) or Automatic
    /// </summary>
    public enum AutomaticFlag { None, All, Automatic }
    /// <summary>
    /// Flag to control interval between output of intermediate stage DFNs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area
    /// </summary>
    public enum IntermediateOutputInterval { SpecifiedTime, EqualTime, EqualArea }

    /// <summary>
    /// Describes the applied load (applied horizontal strain, fluid pressure change, uplift, thermal load) and duration for a single deformation episode
    /// </summary>
    class DeformationEpisodeControl
    {
        // Name and index number
        /// <summary>
        /// Deformation episode name
        /// </summary>
        private string episodeName;
        /// <summary>
        /// Deformation episode name
        /// </summary>
        public string EpisodeName { get { if (episodeName == null) return string.Format("Deformation episode {0}", EpisodeIndex); else return episodeName; } set { episodeName = value; } }
        /// <summary>
        /// Index number for deformation episode; this will be when the deformation episode is added to the list in a PropagationControl object, otherwise will default to -1
        /// </summary>
        public int EpisodeIndex { get; set; }

        // External strain load
        /// <summary>
        /// Applied strain rate tensor
        /// </summary>
        private Tensor2S applied_Epsilon_dashed;
        /// <summary>
        /// Azimuth of minimum applied horizontal strain (radians)
        /// </summary>
        public double Applied_Epsilon_hmin_azimuth { get; private set; }
        /// <summary>
        /// Minimum applied horizontal strain rate (/s)
        /// </summary>
        public double Applied_Epsilon_hmin_dashed { get; private set; }
        /// <summary>
        /// Maximum applied horizontal strain rate (/s)
        /// </summary>
        public double Applied_Epsilon_hmax_dashed { get; private set; }
        /// <summary>
        /// Applied strain rate tensor
        /// </summary>
        public Tensor2S Applied_Epsilon_dashed { get { return applied_Epsilon_dashed; } }
        /// <summary>
        /// Set the external strain load
        /// </summary>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="TimeUnits_in">Time units of input data</param>
        public void SetExternalAppliedStrain(double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in, TimeUnits TimeUnits_in)
        {
            // Convert input values to SI time units
            // If any of the supplied values are NaNs, use the existing values
            double timeUnits_Modifier = 1;
            switch (TimeUnits_in)
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
            if (double.IsNaN(Applied_Epsilon_hmin_azimuth_in))
                Applied_Epsilon_hmin_dashed_in = Applied_Epsilon_hmin_dashed;
            else
                Applied_Epsilon_hmin_dashed_in /= timeUnits_Modifier;
            if (double.IsNaN(Applied_Epsilon_hmax_dashed_in))
                Applied_Epsilon_hmax_dashed_in = Applied_Epsilon_hmax_dashed;
            else
                Applied_Epsilon_hmax_dashed_in /= timeUnits_Modifier;
            if (double.IsNaN(Applied_Epsilon_hmin_azimuth_in))
                Applied_Epsilon_hmin_azimuth_in = Applied_Epsilon_hmin_azimuth;

            // Set the minimum and maximum applied horizontal strain rates
            Applied_Epsilon_hmin_dashed = Applied_Epsilon_hmin_dashed_in;
            Applied_Epsilon_hmax_dashed = Applied_Epsilon_hmax_dashed_in;

            // Set the azimuth of minimum applied horizontal strain
            while (Applied_Epsilon_hmin_azimuth_in >= (Math.PI))
                Applied_Epsilon_hmin_azimuth_in -= (Math.PI);
            while (Applied_Epsilon_hmin_azimuth_in < 0)
                Applied_Epsilon_hmin_azimuth_in += (Math.PI);
            Applied_Epsilon_hmin_azimuth = Applied_Epsilon_hmin_azimuth_in;

            // Regenerate the applied strain rate tensor
            // Calculate the horizontal components of the applied strain rate tensor; vertical components will be zero as there is no applied vertical strain
            double sinazi = Math.Sin(Applied_Epsilon_hmin_azimuth_in);
            double cosazi = Math.Cos(Applied_Epsilon_hmin_azimuth_in);
            double epsilon_dashed_xx = (Applied_Epsilon_hmin_dashed_in * Math.Pow(sinazi, 2)) + (Applied_Epsilon_hmax_dashed_in * Math.Pow(cosazi, 2));
            double epsilon_dashed_yy = (Applied_Epsilon_hmin_dashed_in * Math.Pow(cosazi, 2)) + (Applied_Epsilon_hmax_dashed_in * Math.Pow(sinazi, 2));
            double epsilon_dashed_xy = (Applied_Epsilon_hmin_dashed_in - Applied_Epsilon_hmax_dashed_in) * sinazi * cosazi;

            // Create a new tensor applied strain rate tensor
            applied_Epsilon_dashed = new Tensor2S(epsilon_dashed_xx, epsilon_dashed_yy, 0, epsilon_dashed_xy, 0, 0);
        }

        // Fluid pressure, thermal and uplift loads
        /// <summary>
        /// Rate of increase of fluid overpressure (Pa/s)
        /// </summary>
        public double AppliedOverpressureRate { get; private set; }
        /// <summary>
        /// Rate of in situ temperature change (not including cooling due to uplift) (degK/s)
        /// </summary>
        public double AppliedTemperatureChange { get; private set; }
        /// <summary>
        /// Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/s)
        /// </summary>
        public double AppliedUpliftRate { get; private set; }
        /// <summary>
        /// Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)
        /// </summary>
        public double StressArchingFactor { get; private set; }
        /// <summary>
        /// Set the fluid pressure, thermal and uplift loads
        /// </summary>
        /// <param name="AppliedOverpressureRate_in">Rate of increase of fluid overpressure (Pa/unit time)</param>
        /// <param name="AppliedTemperatureChange_in">Rate of in situ temperature change (not including cooling due to uplift) (degK/unit time)</param>
        /// <param name="AppliedUpliftRate_in">Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/unit time)</param>
        /// <param name="StressArchingFactor_in">Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)</param>
        /// <param name="TimeUnits_in">Time units of input data</param>
        public void SetFPThermalUpliftLoad(double AppliedOverpressureRate_in, double AppliedTemperatureChange_in, double AppliedUpliftRate_in, double StressArchingFactor_in, TimeUnits TimeUnits_in)
        {
            // Convert to SI time units
            double timeUnits_Modifier = 1;
            switch (TimeUnits_in)
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

            // Set fluid pressure, thermal and uplift loads
            // If any of the supplied values are NaNs, use the existing values
            if (!double.IsNaN(AppliedOverpressureRate_in))
                AppliedOverpressureRate = AppliedOverpressureRate_in / timeUnits_Modifier;
            if (!double.IsNaN(AppliedTemperatureChange))
                AppliedTemperatureChange = AppliedTemperatureChange_in / timeUnits_Modifier;
            if (!double.IsNaN(AppliedUpliftRate))
                AppliedUpliftRate = AppliedUpliftRate_in / timeUnits_Modifier;
            if (!double.IsNaN(StressArchingFactor))
                StressArchingFactor = StressArchingFactor_in;
        }

        // Deformation episode duration and total uplift
        /// <summary>
        /// Deformation episode duration (s): if negative, the deformation episode will terminate automatically when the fractures stop growing
        /// </summary>
        public double DeformationEpisodeDuration { get; private set; }
        /// <summary>
        /// Set the deformation episode duration
        /// </summary>
        /// <param name="DeformationEpisodeDuration_in">Deformation episode duration: if negative, the deformation episode will terminate automatically when the fractures stop growing</param>
        /// <param name="TimeUnits_in">Time units of input data</param>
        public void SetDeformationEpisodeDuration(double DeformationEpisodeDuration_in, TimeUnits TimeUnits_in)
        {
            // Convert to SI time units
            double timeUnits_Modifier = 1;
            switch (TimeUnits_in)
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

            // If negative, the deformation episode will terminate automatically when the fractures stop growing
            if (DeformationEpisodeDuration_in > 0)
                DeformationEpisodeDuration = DeformationEpisodeDuration_in * timeUnits_Modifier;
            else
                DeformationEpisodeDuration = DeformationEpisodeDuration_in;
        }
        /// <summary>
        /// Total uplift during the deformation episode (m); if the duration is set to automatic, will return zero
        /// </summary>
        public double DeformationEpisodeUplift { get { return (DeformationEpisodeDuration > 0 ? AppliedUpliftRate * DeformationEpisodeDuration : 0); } }

        // Constructors
        /// <summary>
        /// Default Constructor: set default values
        /// </summary>
        public DeformationEpisodeControl() : this(0, 0, 0, -1, TimeUnits.second)
        {
            // Defaults:

            // Azimuth of minimum horizontal strain: 0rad
            // Minimum horizontal strain rate: 0/s
            // Maximum horizontal strain rate: 0/s
            // Deformation episode duration: -1 (deformation episode will terminate automatically when the fractures stop growing)
            // Time units: second
        }
        /// <summary>
        /// Constructor: Set external strain load only; no fluid pressure, thermal or uplift load
        /// </summary>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="DeformationEpisodeDuration_in">Deformation episode duration: if negative, the deformation episode will terminate automatically when the fractures stop growing</param>
        /// <param name="TimeUnits_in">Time units of input data</param>
        public DeformationEpisodeControl(double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in, double DeformationEpisodeDuration_in, TimeUnits TimeUnits_in)
            : this(Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, Applied_Epsilon_hmin_azimuth_in, 0, 0, 0, 0, DeformationEpisodeDuration_in, TimeUnits_in)
        {
            // Defaults:

            // Rate of increase of fluid overpressure: default 0Pa/s
            // Rate of in situ temperature change: default 0degK/s
            // Rate of uplift and erosion: default 0m/s
            // Stress arching factor: default 0 (no stress arching)
        }
        /// <summary>
        /// Constructor: Set external strain, fluid pressure, thermal and uplift loads
        /// </summary>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="AppliedOverpressureRate_in">Rate of increase of fluid overpressure (Pa/unit time)</param>
        /// <param name="AppliedTemperatureChange_in">Rate of in situ temperature change (not including cooling due to uplift) (degK/unit time)</param>
        /// <param name="AppliedUpliftRate_in">Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/unit time)</param>
        /// <param name="StressArchingFactor_in">Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)</param>
        /// <param name="DeformationEpisodeDuration_in">Deformation episode duration: if negative, the deformation episode will terminate automatically when the fractures stop growing</param>
        /// <param name="TimeUnits_in">Time units of input data</param>
        public DeformationEpisodeControl(double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in, double AppliedOverpressureRate_in, double AppliedTemperatureChange_in, double AppliedUpliftRate_in, double StressArchingFactor_in, double DeformationEpisodeDuration_in, TimeUnits TimeUnits_in)
            : this(null, Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, Applied_Epsilon_hmin_azimuth_in, AppliedOverpressureRate_in, AppliedTemperatureChange_in, AppliedUpliftRate_in, StressArchingFactor_in, DeformationEpisodeDuration_in, TimeUnits_in)
        {
            // Episode name: default to null - will return "Deformation Episode {EpisodeIndex}"
        }
        /// <summary>
        /// Constructor: Give the episode a distinct name and set the external strain, fluid pressure, thermal and uplift loads
        /// </summary>
        /// <param name="EpisodeName_in">Deformation episode name</param>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="AppliedOverpressureRate_in">Rate of increase of fluid overpressure (Pa/s)</param>
        /// <param name="AppliedTemperatureChange_in">Rate of in situ temperature change (not including cooling due to uplift) (degK/s)</param>
        /// <param name="AppliedUpliftRate_in">Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/s)</param>
        /// <param name="StressArchingFactor_in">Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)</param>
        /// <param name="DeformationEpisodeDuration_in">Deformation episode duration (s): if negative, the deformation episode will terminate automatically when the fractures stop growing</param>
        /// <param name="TimeUnits_in">Time units of input data</param>
        public DeformationEpisodeControl(string EpisodeName_in, double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in, double AppliedOverpressureRate_in, double AppliedTemperatureChange_in, double AppliedUpliftRate_in, double StressArchingFactor_in, double DeformationEpisodeDuration_in, TimeUnits TimeUnits_in)
        {
            // Set the deformation episode name
            EpisodeName = EpisodeName_in;
            // Set the deformation episode index to -1; this will be when the deformation episode is added to the list in a PropagationControl object
            EpisodeIndex = -1;

            // Set the external strain load
            SetExternalAppliedStrain(Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, Applied_Epsilon_hmin_azimuth_in, TimeUnits_in);

            // Set fluid pressure, thermal and uplift loads
            SetFPThermalUpliftLoad(AppliedOverpressureRate_in, AppliedTemperatureChange_in, AppliedUpliftRate_in, StressArchingFactor_in, TimeUnits_in);

            // Set the deformation episode duration
            SetDeformationEpisodeDuration(DeformationEpisodeDuration_in, TimeUnits_in);
        }
        /// <summary>
        /// Copy constructor: Create an exact copy of the DeformationEpisodeControl object supplied 
        /// </summary>
        /// <param name="DeformationEpisodeControl_in">Deformation episode object to copy</param>
        public DeformationEpisodeControl(DeformationEpisodeControl DeformationEpisodeControl_in)
        {
            // Set the deformation episode name
            EpisodeName = DeformationEpisodeControl_in.episodeName;
            // Set the deformation episode index to -1; this will be when the deformation episode is added to the list in a PropagationControl object
            EpisodeIndex = DeformationEpisodeControl_in.EpisodeIndex;

            // Set the external strain load
            Applied_Epsilon_hmin_dashed = DeformationEpisodeControl_in.Applied_Epsilon_hmin_dashed;
            Applied_Epsilon_hmax_dashed = DeformationEpisodeControl_in.Applied_Epsilon_hmax_dashed;
            Applied_Epsilon_hmin_azimuth = DeformationEpisodeControl_in.Applied_Epsilon_hmin_azimuth;

            // Set fluid pressure, thermal and uplift loads
            AppliedOverpressureRate = DeformationEpisodeControl_in.AppliedOverpressureRate;
            AppliedTemperatureChange = DeformationEpisodeControl_in.AppliedTemperatureChange;
            AppliedUpliftRate = DeformationEpisodeControl_in.AppliedUpliftRate;
            StressArchingFactor = DeformationEpisodeControl_in.StressArchingFactor;

            // Set the deformation episode duration
            DeformationEpisodeDuration = DeformationEpisodeControl_in.DeformationEpisodeDuration;
        }
    }

    /// <summary>
    /// Contains parameters used to control calculation of the explicit DFN across the entire grid
    /// </summary>
    class DFNGenerationControl
    {
        // References to external objects

        // DFN control flags and data
        /// <summary>
        /// Flag to generate explicit DFN; if false only implicit fracture population functions will be generated
        /// </summary>
        public bool GenerateExplicitDFN { get { return (MacrofractureDFNMinimumLength >= 0); } }
        /// <summary>
        /// Minimum microfracture radius to be included in DFN: if zero or negative, DFN will contain no microfractures; if positive, MacrofractureDFNMinimumLength will be zero (include all macrofractures)
        /// </summary>
        public double MicrofractureDFNMinimumRadius { get; set; }
        /// <summary>
        /// Minimum macrofracture half-length to be included in DFN: if zero, DFN will include all macrofractures; if negative, no DFN will be generated. Culling is syn DFN-generation; macrofractures will be generated with the specified half-length
        /// </summary>
        public double MacrofractureDFNMinimumLength { get; set; }
        /// <summary>
        /// Maximum number of fractures allowed in the DFN: if negative no limit will be applied. Culling is post DFN-generation; if the limit is exceeded the algorithm will remove the smallest fractures from the DFN until the limit is reached
        /// </summary>
        public int maxNoFractures { get; set; }
        /// <summary>
        /// Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
        /// </summary>
        public double MinimumLayerThickness { get; set; }
        /// <summary>
        /// Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
        /// </summary>
        public double MaxConsistencyAngle { get; set; }
        /// <summary>
        /// Flag to crop fractures at the boundary of the grid: if true fractures will be cropped at grid boundary, if false fractures will propagate indefinitely beyond the grid
        /// </summary>
        public bool CropToGrid { get; set; }
        /// <summary>
        /// Flag to connect parallel fractures that are deactivated because their stress shadows interact; this will allow long composite fractures to form
        /// </summary>
        public bool LinkFracturesInStressShadow { get; set; }
        /// <summary>
        /// Number of points used to define polygon for microfracture objects; if less than 3 they will be defined as circles, by centrepoint and radius
        /// </summary>
        public int NumberOfuFPoints { get; set; }
        /// <summary>
        /// Output the DFN at intermediate stages of fracture growth; set to 0 to only output the final DFN
        /// </summary>
        public int NumberOfIntermediateOutputs { get; set; }
        /// <summary>
        /// List of intermediate output times, if intermediate stage DFNs are to be output by time
        /// </summary>
        public List<double> IntermediateOutputTimes;
        /// <summary>
        /// Flag to control interval between output of intermediate stage DFNs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area
        /// </summary>
        public IntermediateOutputInterval SeparateIntermediateOutputsBy { get; set; }
        /// <summary>
        /// Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small, but at the expense of missing stress shadow interactions; set to zero to disable probabilistic fracture nucleation
        /// </summary>
        public double probabilisticFractureNucleationLimit { get; set; }
        /// <summary>
        /// Flag to control whether to search neighbouring gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
        /// </summary>
        public AutomaticFlag SearchNeighbouringGridblocks { get; set; }
        /// <summary>
        /// Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
        /// Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
        /// </summary>
        public bool propagateFracturesInNucleationOrder { get; set; }
        /// <summary>
        /// Time units - will be used for output data
        /// </summary>
        public TimeUnits timeUnits { get; private set; }
        /// <summary>
        /// Get the conversion multiplier from the specified time units to SI units
        /// </summary>
        /// <returns></returns>
        public double getTimeUnitsModifier()
        {
            switch (timeUnits)
            {
                case TimeUnits.second:
                    // In SI units - no change
                    return 1;
                case TimeUnits.year:
                    // Convert from yr to s
                    return (365.25d * 24d * 3600d);
                case TimeUnits.ma:
                    // Convert from ma to s
                    return (1000000d * 365.25d * 24d * 3600d);
                default:
                    return 1;
            }
        }
        /// <summary>
        /// Flag to write data to logfile
        /// </summary>
        public bool WriteDFNFiles { get; set; }
        /// <summary>
        /// Flag for microfracture and macrofracture output file type
        /// </summary>
        public DFNFileType OutputFileType { get; set; }
        /// <summary>
        /// Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints
        /// </summary>
        public bool outputCentrepoints { get; set; }
        /// <summary>
        /// Path to folder for output files
        /// </summary>
        public string FolderPath { get; set; }

        // Default values for fracture permeability, compressibility and aperture
        // Use these values when the fracture permeability, compressibility and aperture must be written to file but are not explicitly calculated
        /// <summary>
        /// Default fracture permeability: 40000mD
        /// </summary>
        public double DefaultFracturePermeability = 40000;
        /// <summary>
        /// Default fracture compressibility: 3e+038Pa-1
        /// </summary>
        public double DefaultFractureCompressibility = 3e+038;

        // Reset and data input functions
        /// <summary>
        /// Set all DFN control data
        /// </summary>
        /// <param name="GenerateExplicitDFN_in">Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated</param>
        /// <param name="MicrofractureDFNMinimumSizeIndex_in">Index of minimum microfracture radius to be included in DFN: if zero or negative, DFN will contain no microfractures; if positive, MacrofractureDFNMinimumLength will be zero (include all macrofractures)</param>
        /// <param name="MacrofractureDFNMinimumSizeIndex_in">Minimum macrofracture half-length to be included in DFN: if zero, DFN will include all macrofractures; if negative, no DFN will be generated</param>
        /// <param name="maxNoFractures_in">Maximum number of fractures allowed in the DFN. Culling is post DFN-generation; if the limit is exceeded the algorithm will remove the smallest fractures from the DFN until the limit is reached</param>
        /// <param name="MinimumLayerThickness_in">Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers</param>
        /// <param name="MaxConsistencyAngle_in">Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set</param>
        /// <param name="CropToGrid_in">Flag to crop fractures at the boundary of the grid: true to crop fractures at grid boundary, false to propagate fractures indefinitely beyond the grid</param>
        /// <param name="LinkFracturesInStressShadow_in">Flag to connect parallel fractures that are deactivated because their stress shadows interact; this will allow long composite fractures to form</param>
        /// <param name="NumberOfuFPoints_in">Number of points used to define polygon for microfracture objects; if less than 3 they will be defined as circles, by centrepoint and radius</param>
        /// <param name="NumberOfIntermediateOutputs_in">Output the DFN at intermediate stages of fracture growth; set to 0 to only output the final DFN</param>
        /// <param name="SeparateIntermediateOutputsBy_in">Flag to control interval between output of intermediate stage DFNs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area</param>
        /// <param name="WriteDFNFiles_in">Flag to write DFN data to file</param>
        /// <param name="OutputFileType_in">Flag for microfracture and macrofracture output file type</param>
        /// <param name="outputCentrepoints_in">Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints</param>
        /// <param name="probabilisticFractureNucleationLimit_in">Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small, but at the expense of missing stress shadow interactions; set to zero to disable probabilistic fracture nucleation</param>
        /// <param name="SearchNeighbouringGridlocks_in">Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry</param>
        /// <param name="propagateFracturesInNucleationOrder_in">Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set</param>
        /// <param name="timeUnits_in">Time units for output data</param>
        public void setDFNGenerationControl(bool GenerateExplicitDFN_in, double MicrofractureDFNMinimumSizeIndex_in, double MacrofractureDFNMinimumSizeIndex_in, int maxNoFractures_in, double MinimumLayerThickness_in, double MaxConsistencyAngle_in, bool CropToGrid_in, bool LinkFracturesInStressShadow_in, int NumberOfuFPoints_in, int NumberOfIntermediateOutputs_in, IntermediateOutputInterval SeparateIntermediateOutputsBy_in, bool WriteDFNFiles_in, DFNFileType OutputFileType_in, bool outputCentrepoints_in, double probabilisticFractureNucleationLimit_in, AutomaticFlag SearchNeighbouringGridlocks_in, bool propagateFracturesInNucleationOrder_in, TimeUnits timeUnits_in)
        {
            // Minimum microfracture radius to be included in DFN: if zero or negative, DFN will contain no microfractures; if positive, MacrofractureDFNMinimumLength will be zero (include all macrofractures)
            MicrofractureDFNMinimumRadius = MicrofractureDFNMinimumSizeIndex_in;
            // Minimum macrofracture half-length to be included in DFN: if zero, DFN will include all macrofractures; if negative, no DFN will be generated. Culling is syn DFN-generation; macrofractures will be generated with the specified half-length
            // If flag to generate explicit DFN is set to false, set minimum macrofracture half-length to -1
            MacrofractureDFNMinimumLength = (GenerateExplicitDFN_in ? MacrofractureDFNMinimumSizeIndex_in : -1);
            // Maximum number of fractures allowed in the DFN. Culling is post DFN-generation; if the limit is exceeded the algorithm will remove the smallest fractures from the DFN until the limit is reached
            maxNoFractures = maxNoFractures_in;
            // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
            MinimumLayerThickness = MinimumLayerThickness_in;
            // Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
            MaxConsistencyAngle = MaxConsistencyAngle_in;
            // Flag to crop fractures at the boundary of the grid: true to crop fractures at grid boundary, false to propagate fractures indefinitely beyond the grid
            CropToGrid = CropToGrid_in;
            // Flag to connect parallel fractures that are deactivated because their stress shadows interact; this will allow long composite fractures to form
            LinkFracturesInStressShadow = LinkFracturesInStressShadow_in;
            // Number of points used to define polygon for microfracture objects; if less than 3 they will be defined as circles, by centrepoint and radius
            NumberOfuFPoints = NumberOfuFPoints_in;
            // Output the DFN at intermediate stages of fracture growth; set to 0 to only output the final DFN
            NumberOfIntermediateOutputs = NumberOfIntermediateOutputs_in;
            // Flag to control interval between output of intermediate stage DFNs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area
            SeparateIntermediateOutputsBy = SeparateIntermediateOutputsBy_in;
            // Flag to write DFN data to file
            WriteDFNFiles = WriteDFNFiles_in;
            // Flag for microfracture and macrofracture output file type
            OutputFileType = OutputFileType_in;
            // Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints
            outputCentrepoints = outputCentrepoints_in;
            // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small, but at the expense of missing stress shadow interactions; set to zero to disable probabilistic fracture nucleation
            probabilisticFractureNucleationLimit = probabilisticFractureNucleationLimit_in;
            // Flag to control whether to search neighbouring gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
            SearchNeighbouringGridblocks = SearchNeighbouringGridlocks_in;
            // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
            // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
            propagateFracturesInNucleationOrder = propagateFracturesInNucleationOrder_in;
            // Time units - will be used for output data
            timeUnits = timeUnits_in;
        }

        // Constructors
        /// <summary>
        /// Default Constructor: set default values
        /// </summary>
        public DFNGenerationControl()
                    : this(true, 0d, 0d, -1, 1, Math.PI / 4, false, false, 4, 0, IntermediateOutputInterval.EqualArea, true, DFNFileType.ASCII, false, 0, AutomaticFlag.Automatic, true, TimeUnits.second)
        {
            // Defaults:

            // Flag to generate explicit DFN: true (generate explicit DFN)
            // Index of minimum microfracture radius to be included in DFN: 0 (DFN will contain no microfractures)
            // Index of minimum macrofracture half-length to be included in DFN: 0 (DFN will include all macrofractures)
            // Maximum number of fractures allowed in the DFN: -1 (no limit)
            // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than 1m
            // Maximum variation in fracture propagation azimuth allowed across gridblock boundary: Pi/4 rad (45 degrees) 
            // Flag to extend fractures outside grid area: false (crop at grid boundary)
            // Flag to connect parallel fractures that are deactivated because their stress shadows interact: false
            // Number of points used to define polygon for microfracture objects: 4 (quadrilateral)
            // Output the DFN at intermediate stages of fracture growth: 0 (only output the final DFN)
            // Flag to control interval between output of intermediate stage DFNs: EqualArea (output at approximately regular intervals of total fracture area)
            // Flag to write DFN data to file: true
            // Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints: false
            // Flag for microfracture and macrofracture output file type: ASCII
            // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value: 0 (no probabilistic fracture nucleation)
            // Flag to control whether to search adjacent gridblocks for stress shadow interaction: automatic (this will be determined independently for each gridblock based on the gridblock geometry)
            // Flag to control the order in which fractures are propagated within each timestep: true (fractures will be propagated in order of nucleation time regardless of fracture set)
        }
        /// <summary>
        /// Constructor: Set all DFN control data
        /// </summary>
        /// <param name="GenerateExplicitDFN_in">Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated</param>
        /// <param name="MicrofractureDFNMinimumSizeIndex_in">Index of minimum microfracture radius to be included in DFN: if zero or negative, DFN will contain no microfractures; if positive, MacrofractureDFNMinimumLength will be zero (include all macrofractures)</param>
        /// <param name="MacrofractureDFNMinimumSizeIndex_in">Minimum macrofracture half-length to be included in DFN: if zero, DFN will include all macrofractures; if negative, no DFN will be generated</param>
        /// <param name="maxNoFractures_in">Maximum number of fractures allowed in the DFN. Culling is post DFN-generation; if the limit is exceeded the algorithm will remove the smallest fractures from the DFN until the limit is reached</param>
        /// <param name="MinimumLayerThickness_in">Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers</param>
        /// <param name="MaxConsistencyAngle_in">Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set</param>
        /// <param name="CropToGrid_in">Flag to crop fractures at the boundary of the grid: true to crop fractures at grid boundary, false to propagate fractures indefinitely beyond the grid</param>
        /// <param name="LinkFracturesInStressShadow_in">Flag to connect parallel fractures that are deactivated because their stress shadows interact; this will allow long composite fractures to form</param>
        /// <param name="NumberOfuFPoints_in">Number of points used to define polygon for microfracture objects; if less than 3 they will be defined as circles, by centrepoint and radius</param>
        /// <param name="NumberOfIntermediateOutputs_in">Output the DFN at intermediate stages of fracture growth; set to 0 to only output the final DFN</param>
        /// <param name="SeparateIntermediateOutputsBy_in">Flag to control interval between output of intermediate stage DFNs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area</param>
        /// <param name="WriteDFNFiles_in">Flag to write DFN data to file</param>
        /// <param name="OutputFileType_in">Flag for microfracture and macrofracture output file type</param>
        /// <param name="outputCentrepoints_in">Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints</param>
        /// <param name="probabilisticFractureNucleationLimit_in">Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small, but at the expense of missing stress shadow interactions; set to zero to disable probabilistic fracture nucleation</param>
        /// <param name="SearchAdjacentGridlocks_in">Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry</param>
        /// <param name="propagateFracturesInNucleationOrder_in">Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set</param>
        /// <param name="timeUnits_in">Time units for output data</param>
        public DFNGenerationControl(bool GenerateExplicitDFN_in, double MicrofractureDFNMinimumSizeIndex_in, double MacrofractureDFNMinimumSizeIndex_in, int maxNoFractures_in, double MinimumLayerThickness_in, double MaxConsistencyAngle_in, bool CropToGrid_in, bool LinkFracturesInStressShadow_in, int NumberOfuFPoints_in, int NumberOfIntermediateOutputs_in, IntermediateOutputInterval SeparateIntermediateOutputsBy_in, bool WriteDFNFiles_in, DFNFileType OutputFileType_in, bool outputCentrepoints_in, double probabilisticFractureNucleationLimit_in, AutomaticFlag SearchAdjacentGridlocks_in, bool propagateFracturesInNucleationOrder_in, TimeUnits timeUnits_in)
        {
            // Set folder path for output files to current folder
            FolderPath = "";
            // Create a new (empty) list specifying times for intermediate outputs
            IntermediateOutputTimes = new List<double>();
            // Set all other data
            setDFNGenerationControl(GenerateExplicitDFN_in, MicrofractureDFNMinimumSizeIndex_in, MacrofractureDFNMinimumSizeIndex_in, maxNoFractures_in, MinimumLayerThickness_in, MaxConsistencyAngle_in, CropToGrid_in, LinkFracturesInStressShadow_in, NumberOfuFPoints_in, NumberOfIntermediateOutputs_in, SeparateIntermediateOutputsBy_in, WriteDFNFiles_in, OutputFileType_in, outputCentrepoints_in, probabilisticFractureNucleationLimit_in, SearchAdjacentGridlocks_in, propagateFracturesInNucleationOrder_in, timeUnits_in);
        }
    }

    /// <summary>
    /// Contains parameters used to control calculation of the implicit fracture model in a specific gridblock, including applied strain rate tensor
    /// </summary>
    class PropagationControl
    {
        // References to external objects

        // Calculation control flags and data
        /// <summary>
        /// Calculate full fracture cumulative population distribution functions, or only total cumulative properties (i.e. r = 0 and l = 0)
        /// </summary>
        public bool CalculatePopulationDistributionData { get; set; }
        /// <summary>
        /// Approximate number of index points in the full macrofracture cumulative population distribution array; if zero use one per timestep
        /// </summary>
        public int no_l_indexPoints { get; set; }
        /// <summary>
        /// Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin; if zero set lengths automatically
        /// </summary>
        public double max_HMin_l_indexPoint_Length { get; set; }
        /// <summary>
        /// Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax; if zero set lengths automatically
        /// </summary>
        public double max_HMax_l_indexPoint_Length { get; set; }
        /// <summary>
        /// Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor
        /// </summary>
        public bool CalculateRelaxedStrainPartitioning { get; set; }
        /// <summary>
        /// Flag to output the bulk rock compliance and stiffness tensors
        /// </summary>
        public bool OutputBulkRockElasticTensors { get; set; }
        /// <summary>
        /// Stress distribution case; DuctileBoundary not yet implemented
        /// </summary>
        public StressDistribution StressDistributionCase { get; set; }
        /// <summary>
        /// Maximum allowable increase in MFP33 value in each timestep (controls accuracy of calculation)
        /// </summary>
        public double max_TS_MFP33_increase { get; set; }
        /// <summary>
        /// Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio
        /// </summary>
        public double historic_a_MFP33_termination_ratio { get; set; }
        /// <summary>
        /// Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio 
        /// </summary>
        public double active_total_MFP30_termination_ratio { get; set; }
        /// <summary>
        /// Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
        /// </summary>
        public double minimum_ClearZone_Volume { get; set; }
        /// <summary>
        /// Maximum number of timesteps allowed; calculation will abort when this is reached regardless of time or fracture growth (controls calculation termination)
        /// </summary>
        public int maxTimesteps { get; set; }
        /// <summary>
        /// Maximum duration for individual timesteps; set to -1 for no maximum timestep duration
        /// </summary>
        public double maxTimestepDuration { get; set; }
        /// <summary>
        /// Time units - will be used for output data
        /// </summary>
        public TimeUnits timeUnits { get; private set; }
        /// <summary>
        /// Get the conversion multiplier from the specified time units to SI units
        /// </summary>
        /// <returns></returns>
        public double getTimeUnitsModifier()
        {
            switch (timeUnits)
            {
                case TimeUnits.second:
                    // In SI units - no change
                    return 1;
                case TimeUnits.year:
                    // Convert from yr to s
                    return (365.25d * 24d * 3600d);
                case TimeUnits.ma:
                    // Convert from ma to s
                    return (1000000d * 365.25d * 24d * 3600d);
                default:
                    return 1;
            }
        }
        /// <summary>
        /// Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically (controls accuracy of microfracture calculation)
        /// </summary>
        public int no_r_bins { get; set; }
        /// <summary>
        /// Minimum radius for microfractures to be included in fracture density and porosity calculations; if set to zero (no limit) it will not be possible to calculate volumetric microfracture density as this will be infinite
        /// </summary>
        public double minImplicitMicrofractureRadius { get; set; }
        /// <summary>
        /// Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set
        /// </summary>
        public bool checkAlluFStressShadows { get; set; }
        /// <summary>
        /// Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes
        /// If the P32 anisotropy of the fracture network is less than the specified cutoff, the isotropic method (which takes account of overlapping fractures) will be used
        /// Otherwise the anisotropic method (which takes account of the influence of a primary fracture set on the distribution of secondary sets) will be used
        /// </summary>
        public double anisotropyCutoff { get; set; }
        /// <summary>
        /// Write to log while running calculation
        /// </summary>
        public bool WriteImplicitDataFiles { get; set; }
        /// <summary>
        /// Path to folder for output files
        /// </summary>
        public string FolderPath { get; set; }

        // Deformation episodes and applied external loads
        /// <summary>
        /// List of DeformationEpisodeControl objects, describing the external applied load and duration of each deformation episode in turn
        /// </summary>
        private List<DeformationEpisodeControl> deformationEpisodes;
        /// <summary>
        /// List of DeformationEpisodeControl objects, describing the external applied load and duration of each deformation episode in turn
        /// </summary>
        public List<DeformationEpisodeControl> DeformationEpisodes { get { return deformationEpisodes; } set { deformationEpisodes = value; for (int episodeNo = 0; episodeNo < deformationEpisodes.Count; episodeNo++) deformationEpisodes[episodeNo].EpisodeIndex = episodeNo + 1; } }
        /*/// <summary>
        /// Index number of the current deformation episode
        /// </summary>
        private int currentDeformationEpisode;
        /// <summary>
        /// Get the DeformationEpisodeControl object describing the external applied load and duration of the next deformation episode
        /// </summary>
        /// <returns>The DeformationEpisodeControl object describing the next deformation episode, or null if the deformation has ended</returns>
        public DeformationEpisodeControl GetNextDeformationEpisode()
        {
            if (currentDeformationEpisode < deformationEpisodes.Count)
                return deformationEpisodes[currentDeformationEpisode++];
            else
                return null;
        }
        /// <summary>
        /// Reset the current deformation episode index to zero 
        /// </summary>
        public void ResetDeformation()
        {
            currentDeformationEpisode = 0;
        }*/
        /// <summary>
        /// Add a new deformation episode with load only; no fluid pressure, thermal or uplift load
        /// </summary>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="DeformationEpisodeDuration_in">Deformation episode duration (s): if negative, the deformation episode will terminate automatically when the fractures stop growing</param>
        public void AddDeformationEpisode(double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in, double DeformationEpisodeDuration_in)
        {
            DeformationEpisodeControl newDeformationEpisode = new DeformationEpisodeControl(Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, Applied_Epsilon_hmin_azimuth_in, DeformationEpisodeDuration_in, timeUnits);
            newDeformationEpisode.EpisodeIndex = deformationEpisodes.Count;
            deformationEpisodes.Add(newDeformationEpisode);
        }
        /// <summary>
        /// Add a new deformation episode with external strain, fluid pressure, thermal and uplift loads
        /// </summary>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate (/s)</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="AppliedOverpressureRate_in">Rate of increase of fluid overpressure (Pa/s)</param>
        /// <param name="AppliedTemperatureChange_in">Rate of in situ temperature change (not including cooling due to uplift) (degK/s)</param>
        /// <param name="AppliedUpliftRate_in">Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/s)</param>
        /// <param name="StressArchingFactor_in">Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)</param>
        /// <param name="DeformationEpisodeDuration_in">Deformation episode duration (s): if negative, the deformation episode will terminate automatically when the fractures stop growing</param>
        public void AddDeformationEpisode(double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in, double AppliedOverpressureRate_in, double AppliedTemperatureChange_in, double AppliedUpliftRate_in, double StressArchingFactor_in, double DeformationEpisodeDuration_in)
        {
            DeformationEpisodeControl newDeformationEpisode = new DeformationEpisodeControl(Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, Applied_Epsilon_hmin_azimuth_in, AppliedOverpressureRate_in, AppliedTemperatureChange_in, AppliedUpliftRate_in, StressArchingFactor_in, DeformationEpisodeDuration_in, timeUnits);
            newDeformationEpisode.EpisodeIndex = deformationEpisodes.Count;
            deformationEpisodes.Add(newDeformationEpisode);
        }
        /// <summary>
        /// Add new deformation episode objects to the deformation episode list; this will also convert the data to SI time units 
        /// </summary>
        /// <param name="DeformationEpisodes_in">List of Deformation Episode object</param>
        /// <param name="ClearExistingList">Flag to clear existing list: if true, existing deformation objects will be deleted; if false, the new deformation episodes will be appended to the existing list</param>
        public void AddDeformationEpisodes(List<DeformationEpisodeControl> DeformationEpisodes_in, bool ClearExistingList)
        {
            if (ClearExistingList)
                deformationEpisodes.Clear();
            int nextEpisodeIndex = deformationEpisodes.Count + 1;
            foreach (DeformationEpisodeControl nextEpisode in DeformationEpisodes_in)
            {
                nextEpisode.EpisodeIndex = nextEpisodeIndex++;
                deformationEpisodes.Add(new DeformationEpisodeControl(nextEpisode));
            }
        }
        /// <summary>
        /// Total number of deformation episodes
        /// </summary>
        public int TotalNoDeformationEpisodes { get { return deformationEpisodes.Count; } }
        /// <summary>
        /// Total duration of all deformation episodes
        /// </summary>
        public double TotalDeformationDuration
        {
            get
            {
                double output = 0;
                foreach (DeformationEpisodeControl defControl in deformationEpisodes)
                {
                    if (defControl.DeformationEpisodeDuration >= 0)
                        output += defControl.DeformationEpisodeDuration;
                }
                return output;
            }
        }
        /// <summary>
        /// Total uplift during all deformation episodes, excluding episodes whose duration is set to automatic
        /// </summary>
        public double TotalAppliedUplift
        {
            get
            {
                double output = 0;
                foreach (DeformationEpisodeControl defControl in deformationEpisodes)
                    output += defControl.DeformationEpisodeUplift;
                return output;
            }
        }
        /// <summary>
        /// Azimuth of minimum applied horizontal strain for the first deformation episode (radians) 
        /// </summary>
        public double Initial_Applied_Epsilon_hmin_azimuth
        {
            get
            {
                if (TotalNoDeformationEpisodes > 0)
                    return deformationEpisodes[0].Applied_Epsilon_hmin_azimuth;
                else
                    return 0;
            }
        }

        // Fracture porosity control data
        /// <summary>
        /// Flag to calculate and output fracture porosity
        /// </summary>
        public bool CalculateFracturePorosity { get; set; }
        /// <summary>
        /// Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
        /// </summary>
        public FractureApertureType FractureApertureControl { get; set; }

        // Reset and data input functions
        /// <summary>
        /// Set all calculation control data except applied external loads; set length distribution independently for each fracture set
        /// </summary>
        /// <param name="CalculatePopulationDistribution_in">Calculate full fracture cumulative population distribution functions?</param>
        /// <param name="no_l_indexPoints_in">Approximate number of index points in the full macrofracture cumulative population distribution array; if zero use one per timestep</param>
        /// <param name="max_HMin_l_indexPoint_Length_in">Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin; if zero set lengths automatically</param>
        /// <param name="max_HMax_l_indexPoint_Length_in">Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax; if zero set lengths automatically</param>
        /// <param name="CalculateRelaxedStrainPartitioning_in">Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor</param>
        /// <param name="OutputBulkRockElasticTensors_in">Flag to output the bulk rock compliance and stiffness tensors</param>
        /// <param name="StressDistribution_in">Stress distribution case</param>
        /// <param name="max_TS_MFP33_increase_in">Maximum allowable increase in MFP33 value in each timestep (controls accuracy of calculation)</param>
        /// <param name="historic_a_MFP33_termination_ratio_in">Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control</param>
        /// <param name="active_total_MFP30_termination_ratio_in">Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control</param>
        /// <param name="minimum_ClearZone_Volume_in">Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated</param>
        /// <param name="maxTimesteps_in">Maximum number of timesteps allowed; calculation will abort when this is reached regardless of time or fracture growth (controls calculation termination)</param>
        /// <param name="maxTimestepDuration_in">Maximum duration for individual timesteps; set to -1 for no maximum timestep duration</param>
        /// <param name="no_r_bins_in">Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically (controls accuracy of microfracture calculation)</param>
        /// <param name="minImplicitMicrofractureRadius_in">Minimum radius for microfractures to be included in fracture density and porosity calculations; if set to zero (no limit) it will not be possible to calculate volumetric microfracture density</param>
        /// <param name="checkAlluFStressShadows_in">Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set</param>
        /// <param name="anisotropyCutoff_in">Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes</param>
        /// <param name="WriteImplicitDataFiles_in">Write to log while running calculation (use for debugging)</param>
        /// <param name="timeUnits_in">Time units for deformation rates</param>
        /// <param name="CalculateFracturePorosity_in">Flag to calculate and output fracture porosity</param>
        /// <param name="FractureApertureControl_in">Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation</param>
        public void setPropagationControl(bool CalculatePopulationDistribution_in, int no_l_indexPoints_in, double max_HMin_l_indexPoint_Length_in, double max_HMax_l_indexPoint_Length_in, bool CalculateRelaxedStrainPartitioning_in, bool OutputBulkRockElasticTensors_in, StressDistribution StressDistribution_in, double max_TS_MFP33_increase_in, double historic_a_MFP33_termination_ratio_in, double active_total_MFP30_termination_ratio_in, double minimum_ClearZone_Volume_in, int maxTimesteps_in, double maxTimestepDuration_in, int no_r_bins_in, double minImplicitMicrofractureRadius_in, bool checkAlluFStressShadows_in, double anisotropyCutoff_in, bool WriteImplicitDataFiles_in, TimeUnits timeUnits_in, bool CalculateFracturePorosity_in, FractureApertureType FractureApertureControl_in)
        {
            // Set the time units and calculate unit conversion multiplier to adjust input rates if not in SI units
            timeUnits = timeUnits_in;
            double timeUnits_Modifier = getTimeUnitsModifier();
            // Calculate full fracture cumulative population distribution functions, or only total cumulative properties (i.e. r = 0 and l = 0)
            CalculatePopulationDistributionData = CalculatePopulationDistribution_in;
            // Approximate number of index points in the full macrofracture cumulative population distribution array; if zero use one per timestep
            no_l_indexPoints = no_l_indexPoints_in;
            // Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin; if zero set lengths automatically
            max_HMin_l_indexPoint_Length = max_HMin_l_indexPoint_Length_in;
            // Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax; if zero set lengths automatically
            max_HMax_l_indexPoint_Length = max_HMax_l_indexPoint_Length_in;
            // Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor
            CalculateRelaxedStrainPartitioning = CalculateRelaxedStrainPartitioning_in;
            // Flag to output the bulk rock compliance and stiffness tensors
            OutputBulkRockElasticTensors = OutputBulkRockElasticTensors_in;
            // Stress distribution case
            StressDistributionCase = StressDistribution_in;
            // Maximum allowable increase in MFP33 value in each timestep
            max_TS_MFP33_increase = max_TS_MFP33_increase_in;
            // Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio
            historic_a_MFP33_termination_ratio = historic_a_MFP33_termination_ratio_in;
            // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio 
            active_total_MFP30_termination_ratio = active_total_MFP30_termination_ratio_in;
            // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
            minimum_ClearZone_Volume = minimum_ClearZone_Volume_in;
            // Maximum number of timesteps allowed; calculation will abort when this is reached regardless of time or fracture growth
            maxTimesteps = maxTimesteps_in;
            // Maximum timestep duration
            maxTimestepDuration = maxTimestepDuration_in * timeUnits_Modifier;
            // Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically
            no_r_bins = no_r_bins_in;
            // Minimum radius for microfractures to be included in fracture density and porosity calculations; if set to zero (no limit) it will not be possible to calculate volumetric microfracture density as this will be infinite
            minImplicitMicrofractureRadius = minImplicitMicrofractureRadius_in;
            // Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set
            checkAlluFStressShadows = checkAlluFStressShadows_in;
            // Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes
            anisotropyCutoff = anisotropyCutoff_in;
            // Write implicit fracture data to file while running calculation
            WriteImplicitDataFiles = WriteImplicitDataFiles_in;

            // Flag to calculate and output fracture porosity
            CalculateFracturePorosity = CalculateFracturePorosity_in;
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
            FractureApertureControl = FractureApertureControl_in;
        }

        // Constructors
        /// <summary>
        /// Default Constructor: set default values
        /// </summary>
        public PropagationControl() 
            : this(true, 20, 0, 0, false, false, StressDistribution.StressShadow, 0.002, -1, -1, 0.01, 1000, -1, 10, 0, false, 1, false, TimeUnits.second, false, FractureApertureType.Uniform)
        {
            // Defaults:

            // Calculate full fracture cumulative population distribution functions
            // Approximate number of index points in the full macrofracture cumulative population distribution array: 20
            // Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin: 0 (set lengths automatically)
            // Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax: 0 (set lengths automatically)
            // Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures: false (only calculate overall total cumulative strain tensor)
            // Flag to output the bulk rock compliance tensor: false
            // Stress distribution case: Stress shadow
            // Maximum allowable increase in MFP33 value in each timestep: 0.002
            // Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive: -1 (control deactivated)
            // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio: -1 (control deactivated)
            // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume): 0.01 (1%)
            // Maximum number of timesteps allowed: 1000
            // Maximum duration for individual timesteps: -1 (no maximum timestep duration)
            // Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically: 10
            // Minimum radius for microfractures to be included in fracture density and porosity calculations: 0 (no limit); NB it will not be possible to calculate volumetric microfracture density as this will be infinite
            // Flag to check microfractures against stress shadows of all macrofractures, regardless of set: false
            // Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes: 1 (always use isotropic method)
            // Write implicit fracture data to file while running calculation: false
            // Time units: seconds
            // Flag to calculate and output fracture porosity: false
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation: Uniform
        }
        /// <summary>
        /// Constructor: Set all calculation control data ecxcept applied external loads; set index points for cumulative fracture size distribution arrays independently for each fracture set
        /// </summary>
        /// <param name="CalculatePopulationDistribution_in">Calculate full fracture cumulative population distribution functions?</param>
        /// <param name="no_l_indexPoints_in">Approximate number of index points in the full macrofracture cumulative population distribution array; if zero use one per timestep</param>
        /// <param name="max_HMin_l_indexPoint_Length_in">Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin; if zero set lengths automatically</param>
        /// <param name="max_HMax_l_indexPoint_Length_in">Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax; if zero set lengths automatically</param>
        /// <param name="CalculateRelaxedStrainPartitioning_in">Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor</param>
        /// <param name="OutputComplianceTensor_in">Flag to output the bulk rock compliance tensor</param>
        /// <param name="StressDistribution_in">Stress distribution case</param>
        /// <param name="max_TS_MFP33_increase_in">Maximum allowable increase in MFP33 value in each timestep (controls accuracy of calculation)</param>
        /// <param name="historic_a_MFP33_termination_ratio_in">Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control</param>
        /// <param name="active_total_MFP30_termination_ratio_in">Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control</param>
        /// <param name="minimum_ClearZone_Volume_in">Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated</param>
        /// <param name="maxTimesteps_in">Maximum number of timesteps allowed; calculation will abort when this is reached regardless of time or fracture growth (controls calculation termination)</param>
        /// <param name="maxTimestepDuration_in">Maximum duration for individual timesteps; set to -1 for no maximum timestep duration</param>
        /// <param name="no_r_bins_in">Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically (controls accuracy of microfracture calculation)</param>
        /// <param name="minMicrofractureRadius_in">Minimum radius for microfractures to be included in fracture density and porosity calculations; if set to zero (no limit) it will not be possible to calculate volumetric microfracture density as this will be infinite</param>
        /// <param name="checkAlluFStressShadows_in">Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set</param>
        /// <param name="anisotropyCutoff_in">Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes</param>
        /// <param name="WriteImplicitDataFiles_in">Write implicit fracture data to file while running calculation</param>
        /// <param name="timeUnits_in">Time units for deformation rates</param>
        /// <param name="CalculateFracturePorosity_in">Flag to calculate and output fracture porosity</param>
        /// <param name="FractureApertureControl_in">Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation</param>
        public PropagationControl(bool CalculatePopulationDistribution_in, int no_l_indexPoints_in, double max_HMin_l_indexPoint_Length_in, double max_HMax_l_indexPoint_Length_in, bool CalculateRelaxedStrainPartitioning_in, bool OutputComplianceTensor_in, StressDistribution StressDistribution_in, double max_TS_MFP33_increase_in, double historic_a_MFP33_termination_ratio_in, double active_total_MFP30_termination_ratio_in, double minimum_ClearZone_Volume_in, int maxTimesteps_in, double maxTimestepDuration_in, int no_r_bins_in, double minMicrofractureRadius_in, bool checkAlluFStressShadows_in, double anisotropyCutoff_in, bool WriteImplicitDataFiles_in, TimeUnits timeUnits_in, bool CalculateFracturePorosity_in, FractureApertureType FractureApertureControl_in)
        {
            // Set folder path for output files to current folder
            FolderPath = "";
            // Create a new list of deformation episodes, but do not create any deformation episodes
            deformationEpisodes = new List<DeformationEpisodeControl>();
            // Set all other data
            setPropagationControl(CalculatePopulationDistribution_in, no_l_indexPoints_in, max_HMin_l_indexPoint_Length_in, max_HMax_l_indexPoint_Length_in, CalculateRelaxedStrainPartitioning_in, OutputComplianceTensor_in, StressDistribution_in, max_TS_MFP33_increase_in, historic_a_MFP33_termination_ratio_in, active_total_MFP30_termination_ratio_in, minimum_ClearZone_Volume_in, maxTimesteps_in, maxTimestepDuration_in, no_r_bins_in, minMicrofractureRadius_in, checkAlluFStressShadows_in, anisotropyCutoff_in, WriteImplicitDataFiles_in, timeUnits_in, CalculateFracturePorosity_in, FractureApertureControl_in);
        }
        /// <summary>
        /// Constructor: Set all calculation control data, and create a single deformation episode with an applied strain load only; set index points for cumulative fracture size distribution arrays independently for each fracture set
        /// </summary>
        /// <param name="CalculatePopulationDistribution_in">Calculate full fracture cumulative population distribution functions?</param>
        /// <param name="no_l_indexPoints_in">Approximate number of index points in the full macrofracture cumulative population distribution array; if zero use one per timestep</param>
        /// <param name="max_HMin_l_indexPoint_Length_in">Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin; if zero set lengths automatically</param>
        /// <param name="max_HMax_l_indexPoint_Length_in">Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax; if zero set lengths automatically</param>
        /// <param name="CalculateRelaxedStrainPartitioning_in">Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor</param>
        /// <param name="OutputComplianceTensor_in">Flag to output the bulk rock compliance tensor</param>
        /// <param name="StressDistribution_in">Stress distribution case</param>
        /// <param name="max_TS_MFP33_increase_in">Maximum allowable increase in MFP33 value in each timestep (controls accuracy of calculation)</param>
        /// <param name="historic_a_MFP33_termination_ratio_in">Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control</param>
        /// <param name="active_total_MFP30_termination_ratio_in">Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control</param>
        /// <param name="minimum_ClearZone_Volume_in">Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated</param>
        /// <param name="DeformationEpisodeDuration_in">Total duration of deformation episode</param>
        /// <param name="maxTimesteps_in">Maximum number of timesteps allowed; calculation will abort when this is reached regardless of time or fracture growth (controls calculation termination)</param>
        /// <param name="maxTimestepDuration_in">Maximum duration for individual timesteps; set to -1 for no maximum timestep duration</param>
        /// <param name="no_r_bins_in">Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically (controls accuracy of microfracture calculation)</param>
        /// <param name="minMicrofractureRadius_in">Minimum radius for microfractures to be included in fracture density and porosity calculations; if set to zero (no limit) it will not be possible to calculate volumetric microfracture density as this will be infinite</param>
        /// <param name="checkAlluFStressShadows_in">Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set</param>
        /// <param name="anisotropyCutoff_in">Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes</param>
        /// <param name="WriteImplicitDataFiles_in">Write implicit fracture data to file while running calculation</param>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate (in specified time units)</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate (in specified time units)</param>
        /// <param name="timeUnits_in">Time units for deformation rates</param>
        /// <param name="CalculateFracturePorosity_in">Flag to calculate and output fracture porosity</param>
        /// <param name="FractureApertureControl_in">Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation</param>
        public PropagationControl(bool CalculatePopulationDistribution_in, int no_l_indexPoints_in, double max_HMin_l_indexPoint_Length_in, double max_HMax_l_indexPoint_Length_in, bool CalculateRelaxedStrainPartitioning_in, bool OutputComplianceTensor_in, StressDistribution StressDistribution_in, double max_TS_MFP33_increase_in, double historic_a_MFP33_termination_ratio_in, double active_total_MFP30_termination_ratio_in, double minimum_ClearZone_Volume_in, double DeformationEpisodeDuration_in, int maxTimesteps_in, double maxTimestepDuration_in, int no_r_bins_in, double minMicrofractureRadius_in, bool checkAlluFStressShadows_in, double anisotropyCutoff_in, bool WriteImplicitDataFiles_in, double Applied_Epsilon_hmin_azimuth_in, double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, TimeUnits timeUnits_in, bool CalculateFracturePorosity_in, FractureApertureType FractureApertureControl_in)
        {
            // Set folder path for output files to current folder
            FolderPath = "";
            // Create a new list of deformation episodes, but do not create any deformation episodes
            deformationEpisodes = new List<DeformationEpisodeControl>();
            // Set all other data
            setPropagationControl(CalculatePopulationDistribution_in, no_l_indexPoints_in, max_HMin_l_indexPoint_Length_in, max_HMax_l_indexPoint_Length_in, CalculateRelaxedStrainPartitioning_in, OutputComplianceTensor_in, StressDistribution_in, max_TS_MFP33_increase_in, historic_a_MFP33_termination_ratio_in, active_total_MFP30_termination_ratio_in, minimum_ClearZone_Volume_in, maxTimesteps_in, maxTimestepDuration_in, no_r_bins_in, minMicrofractureRadius_in, checkAlluFStressShadows_in, anisotropyCutoff_in, WriteImplicitDataFiles_in, timeUnits_in, CalculateFracturePorosity_in, FractureApertureControl_in);
            // Create the deformation episode
            AddDeformationEpisode(Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, Applied_Epsilon_hmin_azimuth_in, DeformationEpisodeDuration_in);
        }
    }
}
