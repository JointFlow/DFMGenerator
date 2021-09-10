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
        /// Flag to control interval between output of intermediate stage DFNs; if true, they will be output at equal intervals of time, if false they will be output at approximately regular intervals of total fracture area 
        /// </summary>
        public bool SeparateIntermediateOutputsByTime { get; set; }
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
        /// <param name="SeparateIntermediateOutputsByTime_in">Flag to control interval between output of intermediate stage DFNs; if true, they will be output at equal intervals of time, if false they will be output at approximately regular intervals of total fracture area</param>
        /// <param name="WriteDFNFiles_in">Flag to write DFN data to file</param>
        /// <param name="OutputFileType_in">Flag for microfracture and macrofracture output file type</param>
        /// <param name="outputCentrepoints_in">Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints</param>
        /// <param name="probabilisticFractureNucleationLimit_in">Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small, but at the expense of missing stress shadow interactions; set to zero to disable probabilistic fracture nucleation</param>
        /// <param name="SearchNeighbouringGridlocks_in">Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry</param>
        /// <param name="propagateFracturesInNucleationOrder_in">Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set</param>
        /// <param name="timeUnits_in">Time units for output data</param>
        public void setDFNGenerationControl(bool GenerateExplicitDFN_in, double MicrofractureDFNMinimumSizeIndex_in, double MacrofractureDFNMinimumSizeIndex_in, int maxNoFractures_in, double MinimumLayerThickness_in, double MaxConsistencyAngle_in, bool CropToGrid_in, bool LinkFracturesInStressShadow_in, int NumberOfuFPoints_in, int NumberOfIntermediateOutputs_in, bool SeparateIntermediateOutputsByTime_in, bool WriteDFNFiles_in, DFNFileType OutputFileType_in, bool outputCentrepoints_in, double probabilisticFractureNucleationLimit_in, AutomaticFlag SearchNeighbouringGridlocks_in, bool propagateFracturesInNucleationOrder_in, TimeUnits timeUnits_in)
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
            // Flag to control interval between output of intermediate stage DFNs; if true, they will be output at equal intervals of time, if false they will be output at approximately regular intervals of total fracture area 
            SeparateIntermediateOutputsByTime = SeparateIntermediateOutputsByTime_in;
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
                    : this(true, 0d, 0d, -1, 1, Math.PI / 4, false, false, 4, 0, false, true, DFNFileType.ASCII, false, 0, AutomaticFlag.Automatic, true, TimeUnits.second)
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
            // Flag to control interval between output of intermediate stage DFNs: false (output at approximately regular intervals of total fracture area)
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
        /// <param name="SeparateIntermediateOutputsByTime_in">Flag to control interval between output of intermediate stage DFNs; if true, they will be output at equal intervals of time, if false they will be output at approximately regular intervals of total fracture area</param>
        /// <param name="WriteDFNFiles_in">Flag to write DFN data to file</param>
        /// <param name="OutputFileType_in">Flag for microfracture and macrofracture output file type</param>
        /// <param name="outputCentrepoints_in">Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints</param>
        /// <param name="probabilisticFractureNucleationLimit_in">Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small, but at the expense of missing stress shadow interactions; set to zero to disable probabilistic fracture nucleation</param>
        /// <param name="SearchAdjacentGridlocks_in">Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry</param>
        /// <param name="propagateFracturesInNucleationOrder_in">Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set</param>
        /// <param name="timeUnits_in">Time units for output data</param>
        public DFNGenerationControl(bool GenerateExplicitDFN_in, double MicrofractureDFNMinimumSizeIndex_in, double MacrofractureDFNMinimumSizeIndex_in, int maxNoFractures_in, double MinimumLayerThickness_in, double MaxConsistencyAngle_in, bool CropToGrid_in, bool LinkFracturesInStressShadow_in, int NumberOfuFPoints_in, int NumberOfIntermediateOutputs_in, bool SeparateIntermediateOutputsByTime_in, bool WriteDFNFiles_in, DFNFileType OutputFileType_in, bool outputCentrepoints_in, double probabilisticFractureNucleationLimit_in, AutomaticFlag SearchAdjacentGridlocks_in, bool propagateFracturesInNucleationOrder_in, TimeUnits timeUnits_in)
        {
            // Set folder path for output files to current folder
            FolderPath = "";
            // Set all other data
            setDFNGenerationControl(GenerateExplicitDFN_in, MicrofractureDFNMinimumSizeIndex_in, MacrofractureDFNMinimumSizeIndex_in, maxNoFractures_in, MinimumLayerThickness_in, MaxConsistencyAngle_in, CropToGrid_in, LinkFracturesInStressShadow_in, NumberOfuFPoints_in, NumberOfIntermediateOutputs_in, SeparateIntermediateOutputsByTime_in, WriteDFNFiles_in, OutputFileType_in, outputCentrepoints_in, probabilisticFractureNucleationLimit_in, SearchAdjacentGridlocks_in, propagateFracturesInNucleationOrder_in, timeUnits_in);
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
        /// Flag to output the bulk rock compliance tensor
        /// </summary>
        public bool OutputComplianceTensor { get; set; }
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

        // Deformation stage data
        /// <summary>
        /// Deformation Stage duration (s): set automatically if termination criteria is set to NoFractureGrowth
        /// </summary>
        public double DeformationStageDuration { get; set; }

        // External strain load
        /// <summary>
        /// Azimuth of minimum applied horizontal strain (radians)
        /// </summary>
        private double applied_epsilon_hmin_azimuth;
        /// <summary>
        /// Minimum applied horizontal strain rate (/s)
        /// </summary>
        private double applied_Epsilon_hmin_dashed;
        /// <summary>
        /// Maximum applied horizontal strain rate (/s)
        /// </summary>
        private double applied_Epsilon_hmax_dashed;
        /// <summary>
        /// Applied strain rate tensor
        /// </summary>
        private Tensor2S applied_Epsilon_dashed;
        /// <summary>
        /// Azimuth of minimum applied horizontal strain (radians)
        /// </summary>
        public double Applied_Epsilon_hmin_azimuth { get { return applied_epsilon_hmin_azimuth; } }
        /// <summary>
        /// Minimum applied horizontal strain rate (/s)
        /// </summary>
        public double Applied_Epsilon_hmin_dashed { get { return applied_Epsilon_hmin_dashed; } }
        /// <summary>
        /// Maximum applied horizontal strain rate (/s)
        /// </summary>
        public double Applied_Epsilon_hmax_dashed { get { return applied_Epsilon_hmax_dashed; } }
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
        public void SetExternalAppliedStrain(double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, double Applied_Epsilon_hmin_azimuth_in)
        {
            // Set the minimum and maximum applied horizontal strain rates
            applied_Epsilon_hmin_dashed = Applied_Epsilon_hmin_dashed_in;
            applied_Epsilon_hmax_dashed = Applied_Epsilon_hmax_dashed_in;

            // Set the azimuth of minimum applied horizontal strain
            while (Applied_Epsilon_hmin_azimuth_in >= (Math.PI))
                Applied_Epsilon_hmin_azimuth_in -= (Math.PI);
            while (Applied_Epsilon_hmin_azimuth_in < 0)
                Applied_Epsilon_hmin_azimuth_in += (Math.PI);
            applied_epsilon_hmin_azimuth = Applied_Epsilon_hmin_azimuth_in;

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

        /// <summary>
        /// Rate of fluid pressure increase (Pa/s); not currently implemented
        /// </summary>
        public double P_f_dashed { get; set; }
        /// <summary>
        /// Rate of uplift (m/s); not currently implemented
        /// </summary>
        public double z_dashed { get; set; }

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
        /// Set all calculation control data, including applied strain rate; set length distribution independently for each fracture set
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
        /// <param name="DeformationStageDuration_in">Total duration of deformation episode</param>
        /// <param name="maxTimesteps_in">Maximum number of timesteps allowed; calculation will abort when this is reached regardless of time or fracture growth (controls calculation termination)</param>
        /// <param name="maxTimestepDuration_in">Maximum duration for individual timesteps; set to -1 for no maximum timestep duration</param>
        /// <param name="no_r_bins_in">Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically (controls accuracy of microfracture calculation)</param>
        /// <param name="minImplicitMicrofractureRadius_in">Minimum radius for microfractures to be included in fracture density and porosity calculations; if set to zero (no limit) it will not be possible to calculate volumetric microfracture density</param>
        /// <param name="checkAlluFStressShadows_in">Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set</param>
        /// <param name="anisotropyCutoff_in">Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes</param>
        /// <param name="WriteImplicitDataFiles_in">Write to log while running calculation (use for debugging)</param>
        /// <param name="Epsilon_hmin_azimuth_in">Azimuth of minimum horizontal strain (radians)</param>
        /// <param name="Epsilon_hmin_dashed_in">Minimum horizontal strain rate (/s)</param>
        /// <param name="Epsilon_hmax_dashed_in">Maximum horizontal strain rate (/s)</param>
        /// <param name="timeUnits_in">Time units for deformation rates</param>
        /// <param name="CalculateFracturePorosity_in">Flag to calculate and output fracture porosity</param>
        /// <param name="FractureApertureControl_in">Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation</param>
        public void setPropagationControl(bool CalculatePopulationDistribution_in, int no_l_indexPoints_in, double max_HMin_l_indexPoint_Length_in, double max_HMax_l_indexPoint_Length_in, bool CalculateRelaxedStrainPartitioning_in, bool OutputComplianceTensor_in, StressDistribution StressDistribution_in, double max_TS_MFP33_increase_in, double historic_a_MFP33_termination_ratio_in, double active_total_MFP30_termination_ratio_in, double minimum_ClearZone_Volume_in, double DeformationStageDuration_in, int maxTimesteps_in, double maxTimestepDuration_in, int no_r_bins_in, double minImplicitMicrofractureRadius_in, bool checkAlluFStressShadows_in, double anisotropyCutoff_in, bool WriteImplicitDataFiles_in, double Epsilon_hmin_azimuth_in, double Epsilon_hmin_dashed_in, double Epsilon_hmax_dashed_in, TimeUnits timeUnits_in, bool CalculateFracturePorosity_in, FractureApertureType FractureApertureControl_in)
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
            // Flag to output the bulk rock compliance tensor
            OutputComplianceTensor = OutputComplianceTensor_in;
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
            // Deformation Stage duration
            DeformationStageDuration = DeformationStageDuration_in * timeUnits_Modifier;
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

            // Set the external strain load
            SetExternalAppliedStrain((Epsilon_hmin_dashed_in / timeUnits_Modifier), (Epsilon_hmax_dashed_in / timeUnits_Modifier), Epsilon_hmin_azimuth_in);
            // Rate of fluid pressure increase: 0Pa/s
            P_f_dashed = 0 / timeUnits_Modifier;
            // Rate of uplift: 0m/s
            z_dashed = 0 / timeUnits_Modifier;

            // Flag to calculate and output fracture porosity
            CalculateFracturePorosity = CalculateFracturePorosity_in;
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
            FractureApertureControl = FractureApertureControl_in;
        }

        // Constructors
        /// <summary>
        /// Default Constructor: set default values
        /// </summary>
        public PropagationControl() : this(0, 0, 0, TimeUnits.second)
        {
            // Defaults:

            // Azimuth of minimum horizontal strain: 0rad
            // Minimum horizontal strain rate: 0/s
            // Maximum horizontal strain rate: 0/s
            // Rate of fluid pressure increase: 0Pa/s
            // Rate of uplift: 0m/s
        }
        /// <summary>
        /// Constructor: Use defaults for all calculation control data, input strain rate only
        /// </summary>
        /// <param name="Applied_Epsilon_hmin_azimuth_in">Azimuth of minimum applied horizontal strain (radians)</param>
        /// <param name="Applied_Epsilon_hmin_dashed_in">Minimum applied horizontal strain rate (in specified time units)</param>
        /// <param name="Applied_Epsilon_hmax_dashed_in">Maximum applied horizontal strain rate (in specified time units)</param>
        /// <param name="timeUnits_in">Time units for deformation rates</param>
        public PropagationControl(double Applied_Epsilon_hmin_azimuth_in, double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, TimeUnits timeUnits_in)
            : this(true, 20, 0, 0, false, false, StressDistribution.StressShadow, 0.01, 0.01, 0.01, 0.001, 1e+10, 1000, -1, 10, 0, false, 0.5, false, Applied_Epsilon_hmin_azimuth_in, Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, timeUnits_in, false, FractureApertureType.Uniform)
        {
            // Defaults:

            // Calculate full fracture cumulative population distribution functions
            // Approximate number of index points in the full macrofracture cumulative population distribution array: 20
            // Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMin: 0 (set lengths automatically)
            // Length of largest index point in the full macrofracture cumulative population distribution array for fractures striking perpendicular to HMax: 0 (set lengths automatically)
            // Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures: false (only calculate overall total cumulative strain tensor)
            // Flag to output the bulk rock compliance tensor: false
            // Stress distribution case: Stress shadow
            // Maximum allowable increase in MFP33 value in each timestep: 0.01 (1%)
            // Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive: 0.01 (1%)
            // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio: 0.01 (1%)
            // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume): 0.001 (0.1%)
            // Total duration of deformation episode: Set to arbitrarily high value (1E+10) so calculation will run until all fracture sets are inactive
            // Maximum number of timesteps allowed: 1000
            // Maximum duration for individual timesteps: -1 (no maximum timestep duration)
            // Number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically: 10
            // Minimum radius for microfractures to be included in fracture density and porosity calculations: 0 (no limit); NB it will not be possible to calculate volumetric microfracture density as this will be infinite
            // Flag to check microfractures against stress shadows of all macrofractures, regardless of set: false
            // Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes: 0.5
            // Write implicit fracture data to file while running calculation: false
            // Flag to calculate and output fracture porosity: false
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation: Uniform
        }
        /// <summary>
        /// Constructor: Set all calculation control data, including applied strain rate; set index points for cumulative fracture size distribution arrays independently for each fracture set
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
        /// <param name="DeformationStageDuration_in">Total duration of deformation episode</param>
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
        public PropagationControl(bool CalculatePopulationDistribution_in, int no_l_indexPoints_in, double max_HMin_l_indexPoint_Length_in, double max_HMax_l_indexPoint_Length_in, bool CalculateRelaxedStrainPartitioning_in, bool OutputComplianceTensor_in, StressDistribution StressDistribution_in, double max_TS_MFP33_increase_in, double historic_a_MFP33_termination_ratio_in, double active_total_MFP30_termination_ratio_in, double minimum_ClearZone_Volume_in, double DeformationStageDuration_in, int maxTimesteps_in, double maxTimestepDuration_in, int no_r_bins_in, double minMicrofractureRadius_in, bool checkAlluFStressShadows_in, double anisotropyCutoff_in, bool WriteImplicitDataFiles_in, double Applied_Epsilon_hmin_azimuth_in, double Applied_Epsilon_hmin_dashed_in, double Applied_Epsilon_hmax_dashed_in, TimeUnits timeUnits_in, bool CalculateFracturePorosity_in, FractureApertureType FractureApertureControl_in)
        {
            // Set folder path for output files to current folder
            FolderPath = "";
            // Set all other data
            setPropagationControl(CalculatePopulationDistribution_in, no_l_indexPoints_in, max_HMin_l_indexPoint_Length_in, max_HMax_l_indexPoint_Length_in, CalculateRelaxedStrainPartitioning_in, OutputComplianceTensor_in, StressDistribution_in, max_TS_MFP33_increase_in, historic_a_MFP33_termination_ratio_in, active_total_MFP30_termination_ratio_in, minimum_ClearZone_Volume_in, DeformationStageDuration_in, maxTimesteps_in, maxTimestepDuration_in, no_r_bins_in, minMicrofractureRadius_in, checkAlluFStressShadows_in, anisotropyCutoff_in, WriteImplicitDataFiles_in, Applied_Epsilon_hmin_azimuth_in, Applied_Epsilon_hmin_dashed_in, Applied_Epsilon_hmax_dashed_in, timeUnits_in, CalculateFracturePorosity_in, FractureApertureControl_in);
        }
    }
}
