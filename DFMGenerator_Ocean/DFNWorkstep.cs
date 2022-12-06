//#define DEBUG_FRACS

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.IO;

using Slb.Ocean.Petrel.Basics;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using Slb.Ocean.Basics;
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel.DomainObject;
using System.Collections.Generic;
using Slb.Ocean.Petrel.UI.Controls;
using Slb.Ocean.Petrel.DomainObject.Shapes;
using Slb.Ocean.Units;

using DFNGenerator_SharedCode;

namespace DFNGenerator_Ocean
{
    /// <summary>
    /// This class contains all the methods and subclasses of the DFNWorkstep.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    class DFNWorkstep : Workstep<DFNWorkstep.Arguments>, IExecutorSource, IAppearance, IDescriptionSource
    {
        #region Overridden Workstep methods

        /// <summary>
        /// Creates an empty Argument instance
        /// </summary>
        /// <returns>New Argument instance.</returns>
        protected override DFNWorkstep.Arguments CreateArgumentPackageCore(IDataSourceManager dataSourceManager)
        {
            return new Arguments(dataSourceManager);
        }
        /// <summary>
        /// Copies the Arguments instance.
        /// </summary>
        /// <param name="fromArgumentPackage">the source Arguments instance</param>
        /// <param name="toArgumentPackage">the target Arguments instance</param>
        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
        }

        /// <summary>
        /// Gets the unique identifier for this Workstep.
        /// </summary>
        protected override string UniqueIdCore
        {
            get
            {
                return "b0fc50e1-447a-4683-a426-45c55eec6d9c";
            }
        }
        #endregion

        #region IExecutorSource Members and Executor class

        /// <summary>
        /// Creates the Executor instance for this workstep. This class will do the work of the Workstep.
        /// </summary>
        /// <param name="argumentPackage">the argumentpackage to pass to the Executor</param>
        /// <param name="workflowRuntimeContext">the context to pass to the Executor</param>
        /// <returns>The Executor instance.</returns>
        public Slb.Ocean.Petrel.Workflow.Executor GetExecutor(object argumentPackage, WorkflowRuntimeContext workflowRuntimeContext)
        {
            return new Executor(argumentPackage as Arguments, workflowRuntimeContext);
        }

        public class Executor : Slb.Ocean.Petrel.Workflow.Executor
        {
            Arguments arguments;
            WorkflowRuntimeContext context;

            public Executor(Arguments arguments, WorkflowRuntimeContext context)
            {
                this.arguments = arguments;
                this.context = context;
            }

            public override void ExecuteSimple()
            {
                // Get handle to grid and check a valid grid has been supplied
                object GridObject = arguments.Grid;

                if (GridObject == null)
                {
                    PetrelLogger.WarnBox("Please provide valid grid object");
                    return;
                }
                Grid PetrelGrid = arguments.Grid;

                // Get reference to parent object of grid
                IParentSourceFactory psFactory = CoreSystem.GetService<IParentSourceFactory>(GridObject);
                if (psFactory == null)
                {
                    PetrelLogger.InfoOutputWindow("The object does not have any parent or not known by Ocean.");
                    return;
                }
                IParentSource parentSource = psFactory.GetParentSource(GridObject);
                object parent = parentSource.Parent;

                // Find name of parent
                INameInfoFactory nFactory = CoreSystem.GetService<INameInfoFactory>(parent);
                if (nFactory == null)
                {
                    PetrelLogger.InfoOutputWindow("Cannot find name info factory for parent");
                    return;
                }

                // Get path for output files
                string fullHomePath = "";
                string folderPath = "";

                var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
                if (homeDrive != null)
                {
                    var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
                    if (homePath != null)
                    {
                        fullHomePath = homeDrive + Path.DirectorySeparatorChar + homePath;
                        folderPath = Path.Combine(fullHomePath, "DFNFolder");
                        folderPath = folderPath + @"\";
                        // If the output folder does not exist, create it
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                    }
                    else
                    {
                        throw new Exception("Environment variable error, there is no 'HOMEPATH'");
                    }
                }
                else
                {
                    throw new Exception("Environment variable error, there is no 'HOMEDRIVE'");
                }

                // Get model name
                string ModelName = arguments.Argument_ModelName;
                if (ModelName.Length == 0) ModelName = "New DFN";

                // Get the grid size
                int maxI = PetrelGrid.NumPillarsIJ.I - 1;
                int maxJ = PetrelGrid.NumPillarsIJ.J - 1;
                int maxK = PetrelGrid.NumCellsIJK.K - 1;

                // Counter to get number of active gridblocks (after upscaling)
                int NoActiveGridblocks = 0;

                // Get all uniform or default values from dialog box arguments
                // Number of rows and columns to include
                int NoPetrelGridRows = arguments.Argument_NoRowsJ;
                int NoPetrelGridCols = arguments.Argument_NoColsI;

                // Index of start cell (lower left hand corner) - set to zero index
                int PetrelGrid_StartCellI = arguments.Argument_I_start_cell - 1;
                if (PetrelGrid_StartCellI < 0) PetrelGrid_StartCellI = 0;
                int PetrelGrid_StartCellJ = arguments.Argument_J_start_cell - 1;
                if (PetrelGrid_StartCellJ < 0) PetrelGrid_StartCellJ = 0;
                if (NoPetrelGridRows > (maxJ - PetrelGrid_StartCellJ)) NoPetrelGridRows = (maxJ - PetrelGrid_StartCellJ);
                if (NoPetrelGridCols > (maxI - PetrelGrid_StartCellI)) NoPetrelGridCols = (maxI - PetrelGrid_StartCellI);

                // Index of top and bottom rows - set to zero index
                int PetrelGrid_TopCellK = arguments.Argument_K_top_layer - 1;
                if (PetrelGrid_TopCellK < 0) PetrelGrid_TopCellK = 0;
                if (PetrelGrid_TopCellK > maxK) PetrelGrid_TopCellK = maxK;
                int PetrelGrid_BaseCellK = arguments.Argument_K_base_layer - 1;
                if (PetrelGrid_BaseCellK < 0) PetrelGrid_BaseCellK = 0;
                if (PetrelGrid_BaseCellK > maxK) PetrelGrid_BaseCellK = maxK;

                // Set VariableStrainOrientation false to have N-S minimum strain orientatation in all cells
                // Set VariableStrainOrientation true to have laterally variable strain orientation controlled by Epsilon_hmin_azimuth_in and Epsilon_hmin_curvature_in
                double Epsilon_hmin_azimuth_in = arguments.Argument_Default_ehmin_orient * (Math.PI / 180); // Can also be set from grid property
                Property ehmin_orient_grid = arguments.Argument_ehmin_orient;
                bool UseGridFor_ehmin_orient = (ehmin_orient_grid != null);

                // Properties of individual gridblocks
                // If depth at time of fracture is specified, use this to overwrite calculated current depth
                double DepthAtFracture = arguments.Argument_DepthAtTimeOfFracture;
                bool OverwriteDepth = (DepthAtFracture > 0);
                // Density of initial microfractures
                double CapB = arguments.Argument_Default_InitialMicrofractureDensity; // Can also be set from grid property
                Property CapB_grid = arguments.Argument_InitialMicrofractureDensity;
                bool UseGridFor_CapB = (CapB_grid != null);
                // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
                double smallc = arguments.Argument_Default_InitialMicrofractureSizeDistribution; // Can also be set from grid property
                Property smallc_grid = arguments.Argument_InitialMicrofractureSizeDistribution;
                bool UseGridFor_smallc = (smallc_grid != null);

                // Mechanical properties
                // Subritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
                double smallb = arguments.Argument_Default_SubcriticalPropIndex; // Can also be set from grid property
                Property smallb_grid = arguments.Argument_SubcriticalPropIndex;
                bool UseGridFor_smallb = (smallb_grid != null);
                double YoungsMod = arguments.Argument_Default_YoungsMod; // Can also be set from grid property
                Property YoungsMod_grid = arguments.Argument_YoungsMod;
                bool UseGridFor_YoungsMod = (YoungsMod_grid != null);
                double PoissonsRatio = arguments.Argument_Default_PoissonsRatio; // Can also be set from grid property
                Property PoissonsRatio_grid = arguments.Argument_PoissonsRatio;
                bool UseGridFor_PoissonsRatio = (PoissonsRatio_grid != null);
                double BiotCoefficient = arguments.Argument_Default_BiotCoefficient; // Can also be set from grid property
                Property BiotCoefficient_grid = arguments.Argument_BiotCoefficient;
                bool UseGridFor_BiotCoefficient = (BiotCoefficient_grid != null);
                double CrackSurfaceEnergy = arguments.Argument_Default_CrackSurfaceEnergy; // Can also be set from grid property
                Property CrackSurfaceEnergy_grid = arguments.Argument_CrackSurfaceEnergy;
                bool UseGridFor_CrackSurfaceEnergy = (CrackSurfaceEnergy_grid != null);
                double FrictionCoefficient = arguments.Argument_Default_FrictionCoefficient; // Can also be set from grid property
                Property FrictionCoefficient_grid = arguments.Argument_FrictionCoefficient;
                bool UseGridFor_FrictionCoefficient = (FrictionCoefficient_grid != null);
                double CriticalPropagationRate = arguments.Argument_CriticalPropagationRate;

                // Strain relaxation data
                // Set this to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate
                double RockStrainRelaxation = arguments.Argument_Default_RockStrainRelaxation; // Can also be set from grid property
                Property RockStrainRelaxation_grid = arguments.Argument_RockStrainRelaxation;
                bool UseGridFor_RockStrainRelaxation = (RockStrainRelaxation_grid != null);
                // Set this to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only
                // Typical value 1E-6?
                double FractureRelaxation = arguments.Argument_Default_FractureRelaxation; // Can also be set from grid property
                Property FractureRelaxation_grid = arguments.Argument_FractureRelaxation;
                bool UseGridFor_FractureRelaxation = (FractureRelaxation_grid != null);
                // Set this to 1 to have initial horizontal stress = vertical stress (viscoelastic equilibrium), set it to 0 to have initial horizontal stress = v/(1-v) * vertical stress (elastic equilibrium)
                // Set it to -1 for initial horizontal stress = critical stress
                double InitialStressRelaxation = arguments.Argument_InitialStressRelaxation;

                // Fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal
                bool Mode1Only = false;
                bool Mode2Only = false;
                if (arguments.Argument_FractureMode == 1)
                    Mode1Only = true;
                else if (arguments.Argument_FractureMode == 2)
                    Mode2Only = true;

                // Stress distribution - use to turn on or off stress shadow effect
                // At present this is set by a boolean flag in the UI, to switch between StressShadow and EvenlyDistributedStress cases
                // When DuctileBoundary case is implemented we will need to convert this to a 3-way selection
                StressDistribution StressDistribution_in = (arguments.Argument_StressDistribution ? StressDistribution.StressShadow : StressDistribution.EvenlyDistributedStress);

                // Stress input data
                // Fluid overpressure (Pa)
                double fluid_overpressure = arguments.Argument_InitialOverpressure;
                // Mean density of overlying sediments and fluid (kg/m3)
                double mean_overlying_sediment_density = arguments.Argument_MeanOverlyingSedimentDensity;
                double fluid_density = arguments.Argument_FluidDensity;
                // Set to negative value for extensional environment
                // With no strain relaxation, strain rate will control rate of horizontal stress increase
                // With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant of horizontal stress
                double Epsilon_hmin_rate_in = arguments.Argument_Default_ehmin_rate; // Can also be set from grid property
                Property ehmin_rate_grid = arguments.Argument_ehmin_rate;
                bool UseGridFor_ehmin_rate = (ehmin_rate_grid != null);
                // Set this to 0 for single fracture set; set to between 0 and Epsilon_hmin_dashed_in for anisotropic fracture pattern; set =Epsilon_hmin_dashed_in for isotropic fracture pattern
                double Epsilon_hmax_rate_in = arguments.Argument_Default_ehmax_rate; // Can also be set from grid property
                Property ehmax_rate_grid = arguments.Argument_ehmax_rate;
                bool UseGridFor_ehmax_rate = (ehmax_rate_grid != null);
                TimeUnits timeUnits_in = TimeUnits.ma;

                // Calculation and output control data
                // Increase this to run calculation faster, with fewer but longer timesteps
                double max_TS_MFP33_increase_in = arguments.Argument_Max_TS_MFP33_increase;
                // Calculation is set to stop automatically when fractures stop growing
                // This can be defined in one of three ways:
                //      - When the total volumetric ratio of active (propagating) half-macrofractures (a_MFP33) drops below a specified proportion of the peak historic value
                //      - When the total volumetric density of active (propagating) half-macrofractures (a_MFP30) drops below a specified proportion of the total (propagating and non-propagating) volumetric density (MFP30)
                //      - When the total clear zone volume (the volume in which fractures can nucleate without falling within or overlapping a stress shadow) drops below a specified proportion of the total volume
                // Increase these cutoffs to reduce the sensitivity and stop the calculation earlier
                // Use this to prevent a long calculation tail - i.e. late timesteps where fractures have stopped growing so they have no impact on fracture populations, just increase runtime
                // To stop calculation while fractures are still growing reduce the DeformationStageDuration_in or maxTimesteps_in limits
                // Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control
                double d_historic_MFP33_termination_ratio_in = arguments.Argument_Historic_MFP33_TerminationRatio;
                // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
                double active_total_MFP30_termination_ratio_in = arguments.Argument_Active_MFP30_TerminationRatio;
                // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
                double minimum_ClearZone_Volume_in = arguments.Argument_Minimum_ClearZone_Volume;
                // Use these to stop the calculation before fractures have finished growing
                // Set DeformationStageDuration to -1 to stop automatically when fracture saturation is reached
                double DeformationStageDuration_in = arguments.Argument_DeformationDuration;
                int maxTimesteps_in = arguments.Argument_MaxNoTimesteps;
                // Maximum duration for individual timesteps; set to -1 for no maximum timestep duration
                double maxTimestepDuration_in = arguments.Argument_MaxTSDuration;
                // Set this to calculate implicit fracture population distribution functions - can also set the number of datapoints to calculate
                bool CalculatePopulationDistribution_in = false;
                int no_l_indexPoints_in = 20;
                // maxLength controls the range of fracture lengths in the implicit fracture population distribution functions
                // Set this value to the approximate maximum length of fractures generated, or 0 if this is not known (0 will default to maximum potential length - but may be much greater than actual maximum length)
                double maxHMinLength = 0;
                double maxHMaxLength = 0;
                Dictionary<FractureOrientation, double> maxLength = new Dictionary<FractureOrientation, double>();
                maxLength.Add(FractureOrientation.HMin, maxHMinLength);
                maxLength.Add(FractureOrientation.HMax, maxHMaxLength);
                // This controls accuracy of numerical calculation of microfracture populations - increase this to increase accuracy of the numerical integration at expense of runtime 
                int no_r_bins_in = arguments.Argument_no_r_bins;
                // Minimum radius for microfractures to be included in implicit fracture density and porosity calculations
                // By default set to exclude only the smallest bin
                // If this is set to zero (i.e. include all microfractures) then it will not be possible to calculate volumetric microfracture density as this will be infinite
                double minMicrofractureRadius_in = arguments.Argument_MinEffectiveMicrofractureRadius;
                // These must be set to true for stand-alone version or no output will be generated
#if DEBUG_FRACS
                bool logCalculation_in = true;
                bool writeDFNFiles_in = true;
#else
                bool logCalculation_in = arguments.Argument_LogCalculation;
                bool writeDFNFiles_in = arguments.Argument_WriteDFNFiles;
#endif
                DFNFileType DFN_file_type = (arguments.Argument_DFNFileType == 2 ? DFNFileType.FAB : DFNFileType.ASCII);
                // Horizontal upscaling factor - used to amalgamate multiple Petrel grid cells into one fracture gridblock
                int HorizontalUpscalingFactor = arguments.Argument_HorizontalUpscalingFactor;
                if (HorizontalUpscalingFactor < 1) HorizontalUpscalingFactor = 1;
                // Flags for whether to average grid properties across the Petrel grid cells, or take the value from the top middle cell
                bool AverageStressStrainData = arguments.Argument_AverageStressStrainData;

                // Fracture connectivity and anisotropy index control parameters
                // Flag to calculate and output fracture connectivity and anisotropy indices
                bool CalculateFractureConnectivityAnisotropy_in = arguments.Argument_CalculateFractureConnectivityAnisotropy;

                // Fracture porosity calculation control data
                // Flag to calculate and output fracture porosity
                bool CalculateFracturePorosity_in = arguments.Argument_CalculateFracturePorosity;
                // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
                // At present we calculate all aperture types so this has no effect
                FractureApertureType FractureApertureControl_in;
                switch (arguments.Argument_FractureApertureControl)
                {
                    case 1:
                        FractureApertureControl_in = FractureApertureType.Uniform;
                        break;
                    case 2:
                        FractureApertureControl_in = FractureApertureType.SizeDependent;
                        break;
                    case 3:
                        FractureApertureControl_in = FractureApertureType.Dynamic;
                        break;
                    case 4:
                        FractureApertureControl_in = FractureApertureType.BartonBandis;
                        break;
                    default:
                        FractureApertureControl_in = FractureApertureType.BartonBandis;
                        break;
                }
                // Fracture aperture control parameters: Uniform fracture aperture
                // Fixed aperture for Mode 1 fractures striking perpendicular to hmin in the uniform aperture case (m)
                double Mode1HMin_UniformAperture_in = arguments.Argument_HMin_UniformAperture;
                // Fixed aperture for Mode 2 fractures striking perpendicular to hmin in the uniform aperture case (m)
                double Mode2HMin_UniformAperture_in = arguments.Argument_HMin_UniformAperture;
                // Fixed aperture for Mode 1 fractures striking perpendicular to hmax in the uniform aperture case (m)
                double Mode1HMax_UniformAperture_in = arguments.Argument_HMax_UniformAperture;
                // Fixed aperture for Mode 2 fractures striking perpendicular to hmax in the uniform aperture case (m)
                double Mode2HMax_UniformAperture_in = arguments.Argument_HMax_UniformAperture;
                // Fracture aperture control parameters: SizeDependent fracture aperture
                // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                double Mode1HMin_SizeDependentApertureMultiplier_in = arguments.Argument_HMin_SizeDependentApertureMultiplier;
                // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                double Mode2HMin_SizeDependentApertureMultiplier_in = arguments.Argument_HMin_SizeDependentApertureMultiplier;
                // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                double Mode1HMax_SizeDependentApertureMultiplier_in = arguments.Argument_HMin_SizeDependentApertureMultiplier;
                // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                double Mode2HMax_SizeDependentApertureMultiplier_in = arguments.Argument_HMin_SizeDependentApertureMultiplier;
                // Fracture aperture control parameters: Dynamic fracture aperture
                // Multiplier for dynamic aperture
                double DynamicApertureMultiplier_in = arguments.Argument_DynamicApertureMultiplier;
                // Fracture aperture control parameters: Barton-Bandis model for fracture aperture
                // Joint Roughness Coefficient
                double JRC_in = arguments.Argument_JRC;
                // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock
                double UCS_ratio_in = arguments.Argument_UCSratio;
                // Initial normal strength on fracture
                double InitialNormalStress_in = arguments.Argument_InitialNormalStress;
                // Stiffness normal to the fracture, at initial normal stress
                double FractureNormalStiffness_in = arguments.Argument_FractureNormalStiffness;
                // Maximum fracture closure (m)
                double MaximumClosure_in = arguments.Argument_MaximumClosure;

                // DFN control data
                // Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated
                bool GenerateExplicitDFN = arguments.Argument_GenerateExplicitDFN;
                // Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include microfractures above minimum radius in DFN
                double MinMicrofractureRadius = arguments.Argument_MinimumMicrofractureRadius;
                // Not yet implemented - keep this at 0
                double MinMacrofractureLength = 0;
                // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
                double MinimumLayerThickness = arguments.Argument_MinimumLayerThickness;
                // Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints
                int number_uF_Points = arguments.Argument_NoMicrofractureCornerpoints;
                // Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
                // Set to Pi/4 rad (45 degrees) by default
                double MaxConsistencyAngle = arguments.Argument_MaxConsistencyAngle * (Math.PI / 180);
                // Set false to allow fractures to propagate outside of the outer grid boundary
                bool cropAtBoundary = arguments.Argument_CropAtGridBoudary;
                // Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment
                bool linkStressShadows = arguments.Argument_LinkParallelFractures;
                // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small
                // Set to 0 to disable probabilistic fracture nucleation
                // Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
                double probabilisticFractureNucleationLimit = arguments.Argument_ProbabilisticFractureNucleationLimit;
                // Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
                GridblockSearchControl SearchAdjacentGridblocks;
                switch (arguments.Argument_SearchAdjacentGridblocks)
                {
                    case 0:
                        SearchAdjacentGridblocks = GridblockSearchControl.None;
                        break;
                    case 1:
                        SearchAdjacentGridblocks = GridblockSearchControl.All;
                        break;
                    case 2:
                        SearchAdjacentGridblocks = GridblockSearchControl.Automatic;
                        break;
                    default:
                        SearchAdjacentGridblocks = GridblockSearchControl.Automatic;
                        break;
                }
                // Output DFN at intermediate stages of fracture growth
                int NoIntermediateOutputs = arguments.Argument_NoIntermediateOutputs;
                bool OutputIntermediatesByTime = arguments.Argument_OutputIntermediatesByTime;
                // Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints
                bool outputCentrepoints = arguments.Argument_OutputCentrepoints;
                // Flag to create triangular instead of quadrilateral macrofracture segments; will increase the total number of segments but generation algorithm may run faster
                // If set to true, microfractures will comprise a series of coplanar triangles with vertices at the centre, rather than a single polygon
                bool createTriangularFractureSegments = arguments.Argument_CreateTriangularFractureSegments;
                // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
                // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
                bool propagateFracturesInNucleationOrder = arguments.Argument_PropagateFracturesInNucleationOrder;

                // Write input parameters to comments section
                // Create string to write input parameters to
                string generalInputParams = string.Format("Generated by {0}\n\nInput parameters:\n", FractureGrid.VersionNumber);
                string implicitInputParams = "";
                string explicitInputParams = "";

                // Start and end cells, grid size and layers
                generalInputParams += string.Format("Grid {0}\n", PetrelGrid.Name);
                generalInputParams += string.Format("Rows {0}-{1}, columns {2}-{3}\n", PetrelGrid_StartCellJ + 1, PetrelGrid_StartCellI + 1 + NoPetrelGridRows, PetrelGrid_StartCellI + 1, PetrelGrid_StartCellI + 1 + NoPetrelGridCols);
                generalInputParams += string.Format("Layers {0}-{1}\n", PetrelGrid_TopCellK + 1, PetrelGrid_BaseCellK + 1);

                // Strain orientation and rate
                if (ehmin_orient_grid != null)
                    generalInputParams += string.Format("Minimum strain orientation: {0}, default {1}deg\n", ehmin_orient_grid.Name, arguments.Argument_Default_ehmin_orient);
                else
                    generalInputParams += string.Format("Minimum strain orientation: {0}deg\n", arguments.Argument_Default_ehmin_orient);
                if (UseGridFor_ehmin_rate)
                    generalInputParams += string.Format("Minimum strain orientation: {0}, default {1}/{2}\n", ehmin_rate_grid.Name, Epsilon_hmin_rate_in, timeUnits_in);
                else
                    generalInputParams += string.Format("Minimum strain orientation: {0}/{1}\n", Epsilon_hmin_rate_in, timeUnits_in);
                if (UseGridFor_ehmax_rate)
                    generalInputParams += string.Format("Maximum strain orientation: {0}, default {1}/{2}\n", ehmax_rate_grid.Name, Epsilon_hmax_rate_in, timeUnits_in);
                else
                    generalInputParams += string.Format("Maximum strain orientation: {0}/{1}\n", Epsilon_hmax_rate_in, timeUnits_in);
                if (AverageStressStrainData)
                    generalInputParams += "Strain input parameters averaged across all cells\n";
                else
                    generalInputParams += "Strain data taken from top middle cell in each stack\n";

                // Effective stress at time of fracturing
                if (OverwriteDepth)
                    generalInputParams += string.Format("Depth of fracturing: {0}m\n", DepthAtFracture);
                else
                    generalInputParams += "Depth of fracturing: current depth\n";
                generalInputParams += string.Format("Initial fluid overpressure: {0}Pa\n", fluid_overpressure);
                generalInputParams += string.Format("Mean overlying sediment density: {0}kg/m3\n", mean_overlying_sediment_density);
                generalInputParams += string.Format("Fluid density: {0}kg/m3\n", fluid_density);

                // Initial fracture density
                if (UseGridFor_CapB)
                    generalInputParams += string.Format("Initial fracture density: {0}, default {1}; ", CapB_grid.Name, CapB);
                else
                    generalInputParams += string.Format("Initial fracture density: {0}; ", CapB);
                if (UseGridFor_smallc)
                    generalInputParams += string.Format("exponent {0}, default {1}\n ", smallc_grid.Name, smallc);
                else
                    generalInputParams += string.Format("exponent {0}\n", smallc);

                // Mechanical properties
                if (UseGridFor_smallb)
                    generalInputParams += string.Format("Subcritical propagation index: {0}, default {1}\n", smallb_grid.Name, smallb);
                else
                    generalInputParams += string.Format("Subcritical propagation index: {0}\n", smallb);
                if (UseGridFor_YoungsMod)
                    generalInputParams += string.Format("Young's Modulus: {0}, default {1}Pa\n", YoungsMod_grid.Name, YoungsMod);
                else
                    generalInputParams += string.Format("Young's Modulus: {0}Pa\n", YoungsMod);
                if (UseGridFor_PoissonsRatio)
                    generalInputParams += string.Format("Poisson's ratio: {0}, default {1}\n", PoissonsRatio_grid.Name, PoissonsRatio);
                else
                    generalInputParams += string.Format("Poisson's ratio: {0}\n", PoissonsRatio);
                if (UseGridFor_BiotCoefficient)
                    generalInputParams += string.Format("Biot's coefficient: {0}, default {1}\n", BiotCoefficient_grid.Name, BiotCoefficient);
                else
                    generalInputParams += string.Format("Biot's coefficient: {0}\n", BiotCoefficient);
                if (UseGridFor_CrackSurfaceEnergy)
                    generalInputParams += string.Format("Crack surface energy: {0}, default {1}J/m2\n", CrackSurfaceEnergy_grid.Name, CrackSurfaceEnergy);
                else
                    generalInputParams += string.Format("Crack surface energy: {0}J/m2\n", CrackSurfaceEnergy);
                if (UseGridFor_FrictionCoefficient)
                    generalInputParams += string.Format("Friction coefficient: {0}, default {1}\n", FrictionCoefficient_grid.Name, FrictionCoefficient);
                else
                    generalInputParams += string.Format("Friction coefficient: {0}\n", FrictionCoefficient);
                generalInputParams += string.Format("Critical propagation rate: {0}m/s\n", CriticalPropagationRate);
                bool AverageMechanicalPropertyData = arguments.Argument_AverageMechanicalPropertyData;
                if (AverageMechanicalPropertyData)
                    generalInputParams += "Mechanical property input parameters averaged across all cells\n";
                else
                    generalInputParams += "Mechanical property data taken from top middle cell in each stack\n";

                // Stress and strain relaxation
                if (RockStrainRelaxation > 0)
                {
                    if (UseGridFor_RockStrainRelaxation)
                        generalInputParams += string.Format("Strain relaxation time constant for rock matrix: {0}, default {1}{2}\n", RockStrainRelaxation_grid.Name, RockStrainRelaxation, timeUnits_in);
                    else
                        generalInputParams += string.Format("Strain relaxation time constant for rock matrix: {0}{1}\n", RockStrainRelaxation, timeUnits_in);
                }
                else if (FractureRelaxation > 0)
                {
                    if (UseGridFor_FractureRelaxation)
                        generalInputParams += string.Format("Strain relaxation time constant for fractures: {0}, default {1}{2}\n", FractureRelaxation_grid.Name, FractureRelaxation, timeUnits_in);
                    else
                        generalInputParams += string.Format("Strain relaxation time constant for fractures: {0}{1}\n", FractureRelaxation, timeUnits_in);
                }
                else
                    generalInputParams += "No strain relaxation applied\n";
                generalInputParams += string.Format("Initial stress relaxation factor: {0}\n", InitialStressRelaxation);

                // Fracture mode and distribution
                if (Mode1Only)
                    generalInputParams += "Mode 1 fractures only\n";
                else if (Mode2Only)
                    generalInputParams += "Mode 2 fractures only\n";
                else
                    generalInputParams += "Optimal fracture mode\n";
                generalInputParams += string.Format("Stress distribution: {0}\n", StressDistribution_in);

                // Fracture porosity
                if (CalculateFracturePorosity_in)
                {
                    switch (FractureApertureControl_in)
                    {
                        case FractureApertureType.Uniform:
                            generalInputParams += string.Format("Uniform fracture aperture: HMin Mode 1 {0}m; HMin Mode 2 {1}m; HMax Mode 1 {2}m; HMax Mode 2 {3}m\n", Mode1HMin_UniformAperture_in, Mode2HMin_UniformAperture_in, Mode1HMax_UniformAperture_in, Mode2HMax_UniformAperture_in);
                            break;
                        case FractureApertureType.SizeDependent:
                            generalInputParams += string.Format("Size dependent aperture: HMin Mode 1 x{0}; HMin Mode 2 x{1}; HMax Mode 1 x{2}; HMax Mode 2 x{3}\n", Mode1HMin_SizeDependentApertureMultiplier_in, Mode2HMin_SizeDependentApertureMultiplier_in, Mode1HMax_SizeDependentApertureMultiplier_in, Mode2HMax_SizeDependentApertureMultiplier_in);
                            break;
                        case FractureApertureType.Dynamic:
                            generalInputParams += string.Format("Dynamic aperture: x{0}\n", DynamicApertureMultiplier_in);
                            break;
                        case FractureApertureType.BartonBandis:
                            generalInputParams += string.Format("Barton-Bandis aperture: JRC {0}; UCS ratio {1}; Initial normal stress {2}Pa; Initial stiffness {3}Pa; Max closure {4}m\n", JRC_in, UCS_ratio_in, InitialNormalStress_in, FractureNormalStiffness_in, MaximumClosure_in);
                            break;
                        default:
                            break;
                    }
                }

                // Control parameters
                generalInputParams += string.Format("Maximum duration of fracturing: {0}{1}\n", DeformationStageDuration_in, timeUnits_in);
                if (maxTimestepDuration_in > 0)
                    generalInputParams += string.Format("Maximum duration for individual timesteps: {0}{1}\n", maxTimestepDuration_in, timeUnits_in);
                generalInputParams += string.Format("Maximum MFP33 increase per timestep (controls accuracy of calculation): {0}\n",max_TS_MFP33_increase_in);
                generalInputParams += string.Format("Calculation termination control: Max timesteps {0}; Min clear zone volume {1}", maxTimesteps_in, minimum_ClearZone_Volume_in);
                if (d_historic_MFP33_termination_ratio_in > 0)
                    generalInputParams += string.Format("; Current:Peak active MFP33 ratio {0}", d_historic_MFP33_termination_ratio_in);
                if (active_total_MFP30_termination_ratio_in > 0)
                    generalInputParams += string.Format("; Current active:total MFP30 ratio {0}", active_total_MFP30_termination_ratio_in);
                generalInputParams += "\n";
                if (HorizontalUpscalingFactor > 1)
                    generalInputParams += string.Format("Horizontal upscaling factor: {0}\n", HorizontalUpscalingFactor);
                generalInputParams += string.Format("Number of radius bins for numerical calculation of microfracture P32: {0}\n", no_r_bins_in);
                implicitInputParams += string.Format("Minimum microfracture radius for implicit population data: {0}m\n", minMicrofractureRadius_in);

                // DFN control parameters
                if (NoIntermediateOutputs > 0)
                {
                    explicitInputParams += string.Format("Output {0} intermediate DFNs ", NoIntermediateOutputs);
                    if (OutputIntermediatesByTime)
                        explicitInputParams += "at equal time intervals\n";
                    else
                        explicitInputParams += "at approximately equal stages of fracture growth\n";
                    }
                if (minMicrofractureRadius_in > 0)
                {
                    explicitInputParams += string.Format("Minimum microfracture radius for explicit DFN: {0}m\n", minMicrofractureRadius_in);
                    explicitInputParams += string.Format("Microfractures represented as {0} point polygons\n", number_uF_Points);
                }
                else
                {
                    explicitInputParams += "Explicit DFN includes macrofractures only\n";
                }
                if (linkStressShadows)
                    explicitInputParams += "Link fractures across relay zones\n";
                else
                    explicitInputParams += "Do not link fractures across relay zones\n";
                if (cropAtBoundary)
                    explicitInputParams += "Crop fractures at boundary to specified subgrid\n";
                else
                    explicitInputParams += "Fractures can propagate out of specified subgrid\n";
                explicitInputParams += string.Format("Minimum layer thickness cutoff: {0}m\n", MinimumLayerThickness);
                explicitInputParams += string.Format("Maximum bend across cell boundaries (Max Consistency Angle): {0}deg\n", arguments.Argument_MaxConsistencyAngle);
                if (probabilisticFractureNucleationLimit > 0)
                    explicitInputParams += string.Format("Use probabilistic fracture nucleation if fewer than {0} fractures nucleate per timestep\n", probabilisticFractureNucleationLimit);
                switch (SearchAdjacentGridblocks)
                {
                    case GridblockSearchControl.None:
                        explicitInputParams += "Search neighbouring cells for stress shadow interaction: None\n";
                        break;
                    case GridblockSearchControl.All:
                        explicitInputParams += "Search neighbouring cells for stress shadow interaction: All\n";
                        break;
                    case GridblockSearchControl.Automatic:
                        explicitInputParams += "Search neighbouring cells for stress shadow interaction: Automatic\n";
                        break;
                    default:
                        break;
                }
                if (propagateFracturesInNucleationOrder)
                    explicitInputParams += "Fractures propagate in order of nucleation\n";

                // Define a time unit modifier to convert between input units and SI units, when writing output
                double timeUnits_Modifier = 1;
                switch (timeUnits_in)
                {
                    case TimeUnits.second:
                        // In SI units - no change
                        break;
                    case TimeUnits.year:
                        timeUnits_Modifier = 1 / (365.25d * 24d * 3600d); // Convert from yr to s
                        break;
                    case TimeUnits.ma:
                        timeUnits_Modifier = 1 / (1000000d * 365.25d * 24d * 3600d); // Convert from ma to s
                        break;
                    default:
                        break;
                }

                // Progress Bar
                using (IProgress progressBar = PetrelLogger.NewProgress(0, 100, ProgressType.Cancelable, System.Windows.Forms.Cursors.WaitCursor))
                {
                    PetrelProgressReporter progressBarWrapper = new PetrelProgressReporter(progressBar);

                    // Create fracture grid
                    PetrelLogger.InfoOutputWindow("Start building grid");
                    progressBar.SetProgressText("Building grid");
                    int NoFractureGridRows = NoPetrelGridRows / HorizontalUpscalingFactor;
                    int NoFractureGridCols = NoPetrelGridCols / HorizontalUpscalingFactor;
                    FractureGrid ModelGrid = new FractureGrid(NoFractureGridRows, NoFractureGridCols);

                    // Populate fracture grid
                    // Loop through all columns and rows in the Fracture Grid
                    // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                    progressBarWrapper.SetNumberOfElements(NoFractureGridCols * NoFractureGridRows);
                    for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                    {
                        for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                        {
                            // Check if calculation has been aborted
                            if (progressBarWrapper.abortCalculation())
                            {
                                // Clean up any resources or data
                                break;
                            }

                            // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                            int PetrelGrid_FirstCellI = PetrelGrid_StartCellI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                            int PetrelGrid_FirstCellJ = PetrelGrid_StartCellJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                            int PetrelGrid_LastCellI = PetrelGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                            int PetrelGrid_LastCellJ = PetrelGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);

                            // Initialise variables for mean depth and thickness
                            double Depth = 0;
                            double LayerThickness = 0;

                            // Find SW cornerpoints; if the top or bottom cells in the SW corner are undefined, use the highest and lowest defined cells
                            Index3 SW_top = new Index3(PetrelGrid_FirstCellI, PetrelGrid_FirstCellJ, PetrelGrid_TopCellK);
                            Point3 SW_top_corner = PetrelGrid.GetPointAtCell(SW_top, Corner.SouthWest, TopOrBase.Top);
                            // If the top cell is not defined, find the uppermost cell that is
                            if (Point3.IsNull(SW_top_corner))
                            {
                                // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK + 1; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    SW_top.K = PetrelGrid_DataCellK;
                                    SW_top_corner = PetrelGrid.GetPointAtCell(SW_top, Corner.SouthWest, TopOrBase.Top);
                                    if (!Point3.IsNull(SW_top_corner))
                                        break;
                                }
                            }
                            Index3 SW_base = new Index3(PetrelGrid_FirstCellI, PetrelGrid_FirstCellJ, PetrelGrid_BaseCellK);
                            Point3 SW_bottom_corner = PetrelGrid.GetPointAtCell(SW_base, Corner.SouthWest, TopOrBase.Base);
                            // If the bottom cell is not defined, find the lowermost cell that is
                            if (Point3.IsNull(SW_bottom_corner))
                            {
                                // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_BaseCellK - 1; PetrelGrid_DataCellK >= PetrelGrid_TopCellK; PetrelGrid_DataCellK--)
                                {
                                    SW_base.K = PetrelGrid_DataCellK;
                                    SW_bottom_corner = PetrelGrid.GetPointAtCell(SW_base, Corner.SouthWest, TopOrBase.Base);
                                    if (!Point3.IsNull(SW_bottom_corner))
                                        break;
                                }
                            }
                            // Create FractureGrid point corresponding to the SW Petrel gridblock corners
                            PointXYZ FractureGrid_SWtop = new PointXYZ(SW_top_corner.X, SW_top_corner.Y, SW_top_corner.Z);
                            PointXYZ FractureGrid_SWbottom = new PointXYZ(SW_bottom_corner.X, SW_bottom_corner.Y, SW_bottom_corner.Z);
                            // Update mean depth and thickness variables
                            Depth -= SW_top_corner.Z;
                            LayerThickness += (SW_top_corner.Z - SW_bottom_corner.Z);

                            // Find NW cornerpoints; if the top or bottom cells in the NW corner are undefined, use the highest and lowest defined cells
                            Index3 NW_top = new Index3(PetrelGrid_FirstCellI, PetrelGrid_LastCellJ, PetrelGrid_TopCellK);
                            Point3 NW_top_corner = PetrelGrid.GetPointAtCell(NW_top, Corner.NorthWest, TopOrBase.Top);
                            // If the top cell is not defined, find the uppermost cell that is
                            if (Point3.IsNull(NW_top_corner))
                            {
                                // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK + 1; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    NW_top.K = PetrelGrid_DataCellK;
                                    NW_top_corner = PetrelGrid.GetPointAtCell(NW_top, Corner.NorthWest, TopOrBase.Top);
                                    if (!Point3.IsNull(NW_top_corner))
                                        break;
                                }
                            }
                            Index3 NW_base = new Index3(PetrelGrid_FirstCellI, PetrelGrid_LastCellJ, PetrelGrid_BaseCellK);
                            Point3 NW_bottom_corner = PetrelGrid.GetPointAtCell(NW_base, Corner.NorthWest, TopOrBase.Base);
                            // If the bottom cell is not defined, find the lowermost cell that is
                            if (Point3.IsNull(NW_bottom_corner))
                            {
                                // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_BaseCellK - 1; PetrelGrid_DataCellK >= PetrelGrid_TopCellK; PetrelGrid_DataCellK--)
                                {
                                    NW_base.K = PetrelGrid_DataCellK;
                                    NW_bottom_corner = PetrelGrid.GetPointAtCell(NW_base, Corner.NorthWest, TopOrBase.Base);
                                    if (!Point3.IsNull(NW_bottom_corner))
                                        break;
                                }
                            }
                            // Create FractureGrid point corresponding to the NW Petrel gridblock corners
                            PointXYZ FractureGrid_NWtop = new PointXYZ(NW_top_corner.X, NW_top_corner.Y, NW_top_corner.Z);
                            PointXYZ FractureGrid_NWbottom = new PointXYZ(NW_bottom_corner.X, NW_bottom_corner.Y, NW_bottom_corner.Z);
                            // Update mean depth and thickness variables
                            Depth -= NW_top_corner.Z;
                            LayerThickness += (NW_top_corner.Z - NW_bottom_corner.Z);

                            // Find NE cornerpoints; if the top or bottom cells in the NE corner are undefined, use the highest and lowest defined cells
                            Index3 NE_top = new Index3(PetrelGrid_LastCellI, PetrelGrid_LastCellJ, PetrelGrid_TopCellK);
                            Point3 NE_top_corner = PetrelGrid.GetPointAtCell(NE_top, Corner.NorthEast, TopOrBase.Top);
                            // If the top cell is not defined, find the uppermost cell that is
                            if (Point3.IsNull(NE_top_corner))
                            {
                                // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK + 1; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    NE_top.K = PetrelGrid_DataCellK;
                                    NE_top_corner = PetrelGrid.GetPointAtCell(NE_top, Corner.NorthEast, TopOrBase.Top);
                                    if (!Point3.IsNull(NE_top_corner))
                                        break;
                                }
                            }
                            Index3 NE_base = new Index3(PetrelGrid_LastCellI, PetrelGrid_LastCellJ, PetrelGrid_BaseCellK);
                            Point3 NE_bottom_corner = PetrelGrid.GetPointAtCell(NE_base, Corner.NorthEast, TopOrBase.Base);
                            // If the bottom cell is not defined, find the lowermost cell that is
                            if (Point3.IsNull(NE_bottom_corner))
                            {
                                // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_BaseCellK - 1; PetrelGrid_DataCellK >= PetrelGrid_TopCellK; PetrelGrid_DataCellK--)
                                {
                                    NE_base.K = PetrelGrid_DataCellK;
                                    NE_bottom_corner = PetrelGrid.GetPointAtCell(NE_base, Corner.NorthEast, TopOrBase.Base);
                                    if (!Point3.IsNull(NE_bottom_corner))
                                        break;
                                }
                            }
                            // Create FractureGrid point corresponding to the NE Petrel gridblock corners
                            PointXYZ FractureGrid_NEtop = new PointXYZ(NE_top_corner.X, NE_top_corner.Y, NE_top_corner.Z);
                            PointXYZ FractureGrid_NEbottom = new PointXYZ(NE_bottom_corner.X, NE_bottom_corner.Y, NE_bottom_corner.Z);
                            // Update mean depth and thickness variables
                            Depth -= NE_top_corner.Z;
                            LayerThickness += (NE_top_corner.Z - NE_bottom_corner.Z);

                            // Find SE cornerpoints; if the top or bottom cells in the SE corner are undefined, use the highest and lowest defined cells
                            Index3 SE_top = new Index3(PetrelGrid_LastCellI, PetrelGrid_FirstCellJ, PetrelGrid_TopCellK);
                            Point3 SE_top_corner = PetrelGrid.GetPointAtCell(SE_top, Corner.SouthEast, TopOrBase.Top);
                            // If the top cell is not defined, find the uppermost cell that is
                            if (Point3.IsNull(SE_top_corner))
                            {
                                // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK + 1; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    SE_top.K = PetrelGrid_DataCellK;
                                    SE_top_corner = PetrelGrid.GetPointAtCell(SE_top, Corner.SouthEast, TopOrBase.Top);
                                    if (!Point3.IsNull(SE_top_corner))
                                        break;
                                }
                            }
                            Index3 SE_base = new Index3(PetrelGrid_LastCellI, PetrelGrid_FirstCellJ, PetrelGrid_BaseCellK);
                            Point3 SE_bottom_corner = PetrelGrid.GetPointAtCell(SE_base, Corner.SouthEast, TopOrBase.Base);
                            // If the bottom cell is not defined, find the lowermost cell that is
                            if (Point3.IsNull(SE_bottom_corner))
                            {
                                // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_BaseCellK - 1; PetrelGrid_DataCellK >= PetrelGrid_TopCellK; PetrelGrid_DataCellK--)
                                {
                                    SE_base.K = PetrelGrid_DataCellK;
                                    SE_bottom_corner = PetrelGrid.GetPointAtCell(SE_base, Corner.SouthEast, TopOrBase.Base);
                                    if (!Point3.IsNull(SE_bottom_corner))
                                        break;
                                }
                            }
                            // Create FractureGrid point corresponding to the SE Petrel gridblock corners
                            PointXYZ FractureGrid_SEtop = new PointXYZ(SE_top_corner.X, SE_top_corner.Y, SE_top_corner.Z);
                            PointXYZ FractureGrid_SEbottom = new PointXYZ(SE_bottom_corner.X, SE_bottom_corner.Y, SE_bottom_corner.Z);
                            // Update mean depth and thickness variables
                            Depth -= SE_top_corner.Z;
                            LayerThickness += (SE_top_corner.Z - SE_bottom_corner.Z);

                            // Calculate the mean depth and layer thickness
                            Depth = (OverwriteDepth ? DepthAtFracture : Depth / 4);
                            LayerThickness = LayerThickness / 4;

                            // If either the mean depth or the layer thickness are undefined, then one or more of the corners lies outside the grid
                            // In this case we will abort this gridblock and move onto the next
                            if (double.IsNaN(Depth) || double.IsNaN(LayerThickness))
                                continue;

                            // Check if the western boundary if faulted
                            // This will be the case if any of the Petrel cells on the southern boundary are faulted
                            bool faultToWest = false;
                            for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                            {
                                Index2 SWpillar = new Index2(PetrelGrid_FirstCellI, PetrelGrid_J);
                                Index2 NWpillar = new Index2(PetrelGrid_FirstCellI, PetrelGrid_J + 1);
                                if (PetrelGrid.IsNodeFaulted(SWpillar) && PetrelGrid.IsNodeFaulted(NWpillar))
                                    faultToWest = true;
                            }

                            // Check if the southern boundary is faulted
                            // This will be the case if any of the Petrel cells on the southern boundary are faulted
                            bool faultToSouth = false;
                            for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                            {
                                Index2 SWpillar = new Index2(PetrelGrid_I, PetrelGrid_FirstCellJ);
                                Index2 SEpillar = new Index2(PetrelGrid_I + 1, PetrelGrid_FirstCellJ);
                                if (PetrelGrid.IsNodeFaulted(SWpillar) && PetrelGrid.IsNodeFaulted(SEpillar))
                                    faultToSouth = true;
                            }

                            // Get the stress/strain data from the grid as required
                            // This will depend on whether we are averaging the stress/strain over all Petrel cells that make up the gridblock, or taking the values from a single cell
                            // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                            double local_Epsilon_hmin_azimuth_in = Epsilon_hmin_azimuth_in;
                            double local_Epsilon_hmin_rate_in = Epsilon_hmin_rate_in;
                            double local_Epsilon_hmax_rate_in = Epsilon_hmax_rate_in;

                            if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
                            {
                                // Create local variables for running total and number of datapoints for each stress/strain state parameter
                                double ehmin_orient_x_total = 0;
                                double ehmin_orient_y_total = 0;
                                int ehmin_orient_novalues = 0;
                                double ehmin_rate_total = 0;
                                int ehmin_rate_novalues = 0;
                                double ehmax_rate_total = 0;
                                int ehmax_rate_novalues = 0;

                                // Loop through all the Petrel cells in the gridblock
                                for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                    for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                        for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                        {
                                            Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                            // Update ehmin orientation total if defined
                                            if (UseGridFor_ehmin_orient)
                                            {
                                                double cell_ehmin_orient = (double)ehmin_orient_grid[cellRef];
                                                if (!double.IsNaN(cell_ehmin_orient))
                                                {
                                                    // Trim the ehmin orientation values so they lie within a semicircular range
                                                    // To try to get a more meaningful average, the range will depend on the previous values
                                                    // If previous values have tended towards an EW orientation (so total x > total y), the range will be between 0 and pi to better average near EW vectors
                                                    if (Math.Abs(ehmin_orient_x_total) > 1.2 * Math.Abs(ehmin_orient_y_total))
                                                    {
                                                        // Trim the ehmin orientation values so they lie between 0 and pi
                                                        while (cell_ehmin_orient < 0)
                                                            cell_ehmin_orient += Math.PI;
                                                        while (cell_ehmin_orient >= Math.PI)
                                                            cell_ehmin_orient -= Math.PI;
                                                    }
                                                    // If previous values have tended towards an NS orientation (so total x < total y), the range will be between -pi/2 and pi/2 to better average near NS vectors
                                                    else if (Math.Abs(ehmin_orient_y_total) > 1.2 * Math.Abs(ehmin_orient_x_total))
                                                    {
                                                        // Trim the ehmin orientation values so they lie between 0 and pi
                                                        while (cell_ehmin_orient < -(Math.PI / 2))
                                                            cell_ehmin_orient += Math.PI;
                                                        while (cell_ehmin_orient >= (Math.PI / 2))
                                                            cell_ehmin_orient -= Math.PI;
                                                    }
                                                    // If previous values have no preferred orientation, or this is the first value (so total x = total y), the range will be between -pi/4 and 3*pi/4 to better average near NS vectors
                                                    else
                                                    {
                                                        // Trim the ehmin orientation values so they lie between -pi/4 and 3*pi/4
                                                        while (cell_ehmin_orient < -(Math.PI / 4))
                                                            cell_ehmin_orient += Math.PI;
                                                        while (cell_ehmin_orient >= (3 * Math.PI / 4))
                                                            cell_ehmin_orient -= Math.PI;
                                                    }

                                                    ehmin_orient_x_total += Math.Sin(cell_ehmin_orient);
                                                    ehmin_orient_y_total += Math.Cos(cell_ehmin_orient);
                                                    ehmin_orient_novalues++;
                                                }
                                            }

                                            // Update ehmin rate total if defined
                                            if (UseGridFor_ehmin_rate)
                                            {
                                                double cell_ehmin_rate = (double)ehmin_rate_grid[cellRef];
                                                if (!double.IsNaN(cell_ehmin_rate))
                                                {
                                                    ehmin_rate_total += cell_ehmin_rate;
                                                    ehmin_rate_novalues++;
                                                }
                                            }

                                            // Update ehmax rate total if defined
                                            if (UseGridFor_ehmax_rate)
                                            {
                                                double cell_ehmax_rate = (double)ehmax_rate_grid[cellRef];
                                                if (!double.IsNaN(cell_ehmax_rate))
                                                {
                                                    ehmax_rate_total += cell_ehmax_rate;
                                                    ehmax_rate_novalues++;
                                                }
                                            }
                                        }

                                // Update the gridblock values with the averages - if there is any data to calculate them from
                                if (ehmin_orient_novalues > 0)
                                    local_Epsilon_hmin_azimuth_in = Math.Atan(ehmin_orient_x_total / ehmin_orient_y_total);
                                if (ehmin_rate_novalues > 0)
                                    local_Epsilon_hmin_rate_in = ehmin_rate_total / (double)ehmin_rate_novalues;
                                if (ehmax_rate_novalues > 0)
                                    local_Epsilon_hmax_rate_in = ehmax_rate_total / (double)ehmax_rate_novalues;
                            }
                            else // We are taking data from a single cell
                            {
                                // If there is no upscaling, we take the data from the uppermost cell that contains valid data
                                int PetrelGrid_DataCellI = PetrelGrid_FirstCellI;
                                int PetrelGrid_DataCellJ = PetrelGrid_FirstCellJ;

                                // If there is upscaling, we take data from the uppermost middle cell that contains valid data
                                if (HorizontalUpscalingFactor > 1)
                                {
                                    PetrelGrid_DataCellI += (HorizontalUpscalingFactor / 2);
                                    PetrelGrid_DataCellJ += (HorizontalUpscalingFactor / 2);
                                }

                                // Create a reference to the cell from which we will read the data
                                Index3 cellRef = new Index3(PetrelGrid_DataCellI, PetrelGrid_DataCellJ, PetrelGrid_TopCellK);

                                // Update ehmin orientation total if defined
                                if (UseGridFor_ehmin_orient)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_ehmin_orient = (double)ehmin_orient_grid[cellRef];
                                        if (!double.IsNaN(cell_ehmin_orient))
                                        {
                                            local_Epsilon_hmin_azimuth_in = cell_ehmin_orient;
                                            break;
                                        }
                                    }
                                }

                                // Update ehmin rate total if defined
                                if (UseGridFor_ehmin_rate)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_ehmin_rate = (double)ehmin_rate_grid[cellRef];
                                        if (!double.IsNaN(cell_ehmin_rate))
                                        {
                                            local_Epsilon_hmin_rate_in = cell_ehmin_rate;
                                            break;
                                        }
                                    }
                                }

                                // Update ehmax rate total if defined
                                if (UseGridFor_ehmax_rate)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_ehmax_rate = (double)ehmax_rate_grid[cellRef];
                                        if (!double.IsNaN(cell_ehmax_rate))
                                        {
                                            local_Epsilon_hmax_rate_in = cell_ehmax_rate;
                                            break;
                                        }
                                    }
                                }
                            } // End get the stress / strain data from the grid as required

                            // Get the mechanical properties from the grid as required
                            // This will depend on whether we are averaging the mechanical properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                            // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                            double local_CapB = CapB;
                            double local_smallc = smallc;
                            double local_smallb = smallb;
                            double local_YoungsMod = YoungsMod;
                            double local_PoissonsRatio = PoissonsRatio;
                            double local_BiotCoefficient = BiotCoefficient;
                            double local_CrackSurfaceEnergy = CrackSurfaceEnergy;
                            double local_FrictionCoefficient = FrictionCoefficient;
                            double local_RockStrainRelaxation = RockStrainRelaxation;
                            double local_FractureRelaxation = FractureRelaxation;

                            if (AverageMechanicalPropertyData) // We are averaging over all Petrel cells in the gridblock
                            {
                                // Create local variables for running total and number of datapoints for each mechanical property
                                double CapB_total = 0;
                                int CapB_novalues = 0;
                                double smallc_total = 0;
                                int smallc_novalues = 0;
                                double smallb_total = 0;
                                int smallb_novalues = 0;
                                double YoungsMod_total = 0;
                                int YoungsMod_novalues = 0;
                                double PoissonsRatio_total = 0;
                                int PoissonsRatio_novalues = 0;
                                double BiotCoeff_total = 0;
                                int BiotCoeff_novalues = 0;
                                double CrackSurfaceEnergy_total = 0;
                                int CrackSurfaceEnergy_novalues = 0;
                                double FrictionCoeff_total = 0;
                                int FrictionCoeff_novalues = 0;
                                double RockStrainRelaxation_total = 0;
                                int RockStrainRelaxation_novalues = 0;
                                double FractureRelaxation_total = 0;
                                int FractureRelaxation_novalues = 0;

                                // Loop through all the Petrel cells in the gridblock
                                for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                    for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                        for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                        {
                                            Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                            // Update initial microfracture density total if defined
                                            if (UseGridFor_CapB)
                                            {
                                                double cell_CapB = (double)CapB_grid[cellRef];
                                                if (!double.IsNaN(cell_CapB))
                                                {
                                                    CapB_total += cell_CapB;
                                                    CapB_novalues++;
                                                }
                                            }

                                            // Update initial microfracture size distribution total if defined
                                            if (UseGridFor_smallc)
                                            {
                                                double cell_smallc = (double)smallc_grid[cellRef];
                                                if (!double.IsNaN(cell_smallc))
                                                {
                                                    smallc_total += cell_smallc;
                                                    smallc_novalues++;
                                                }
                                            }

                                            // Update subcritical propagation index total if defined
                                            if (UseGridFor_smallb)
                                            {
                                                double cell_smallb = (double)smallb_grid[cellRef];
                                                if (!double.IsNaN(cell_smallb))
                                                {
                                                    smallb_total += cell_smallb;
                                                    smallb_novalues++;
                                                }
                                            }

                                            // Update Young's Modulus total if defined
                                            if (UseGridFor_YoungsMod)
                                            {
                                                double cell_YoungsMod = (double)YoungsMod_grid[cellRef];
                                                if (!double.IsNaN(cell_YoungsMod))
                                                {
                                                    YoungsMod_total += cell_YoungsMod;
                                                    YoungsMod_novalues++;
                                                }
                                            }

                                            // Update Poisson's ratio total if defined
                                            if (UseGridFor_PoissonsRatio)
                                            {
                                                double cell_PoissonsRatio = (double)PoissonsRatio_grid[cellRef];
                                                if (!double.IsNaN(cell_PoissonsRatio))
                                                {
                                                    PoissonsRatio_total += cell_PoissonsRatio;
                                                    PoissonsRatio_novalues++;
                                                }
                                            }

                                            // Update Biot coefficient total if defined
                                            if (UseGridFor_BiotCoefficient)
                                            {
                                                double cell_BiotCoeff = (double)BiotCoefficient_grid[cellRef];
                                                if (!double.IsNaN(cell_BiotCoeff))
                                                {
                                                    BiotCoeff_total += cell_BiotCoeff;
                                                    BiotCoeff_novalues++;
                                                }
                                            }

                                            // Update crack surface energy total if defined
                                            if (UseGridFor_CrackSurfaceEnergy)
                                            {
                                                double cell_CrackSurfaceEnergy = (double)CrackSurfaceEnergy_grid[cellRef];
                                                if (!double.IsNaN(cell_CrackSurfaceEnergy))
                                                {
                                                    CrackSurfaceEnergy_total += cell_CrackSurfaceEnergy;
                                                    CrackSurfaceEnergy_novalues++;
                                                }
                                            }

                                            // Update friction coefficient total if defined
                                            if (UseGridFor_FrictionCoefficient)
                                            {
                                                double cell_FrictionCoeff = (double)FrictionCoefficient_grid[cellRef];
                                                if (!double.IsNaN(cell_FrictionCoeff))
                                                {
                                                    FrictionCoeff_total += cell_FrictionCoeff;
                                                    FrictionCoeff_novalues++;
                                                }
                                            }

                                            // Update rock strain relaxation total if defined
                                            if (UseGridFor_RockStrainRelaxation)
                                            {
                                                double cell_RockStrainRelaxation = (double)RockStrainRelaxation_grid[cellRef];
                                                if (!double.IsNaN(cell_RockStrainRelaxation))
                                                {
                                                    if (cell_RockStrainRelaxation > 0)
                                                    {
                                                        RockStrainRelaxation_total += cell_RockStrainRelaxation;
                                                        RockStrainRelaxation_novalues++;
                                                    }
                                                }
                                            }

                                            // Update fracture strain relaxation total if defined
                                            if (UseGridFor_FractureRelaxation)
                                            {
                                                double cell_FractureRelaxation = (double)FractureRelaxation_grid[cellRef];
                                                if (!double.IsNaN(cell_FractureRelaxation))
                                                {
                                                    if (cell_FractureRelaxation > 0)
                                                    {
                                                        FractureRelaxation_total += cell_FractureRelaxation;
                                                        FractureRelaxation_novalues++;
                                                    }
                                                }
                                            }
                                        }

                                // Update the gridblock values with the averages - if there is any data to calculate them from
                                if (CapB_novalues > 0)
                                    local_CapB = CapB_total / (double)CapB_novalues;
                                if (smallc_novalues > 0)
                                    local_smallc = smallc_total / (double)smallc_novalues;
                                if (smallb_novalues > 0)
                                    local_smallb = smallb_total / (double)smallb_novalues;
                                if (YoungsMod_novalues > 0)
                                    local_YoungsMod = YoungsMod_total / (double)YoungsMod_novalues;
                                if (PoissonsRatio_novalues > 0)
                                    local_PoissonsRatio = PoissonsRatio_total / (double)PoissonsRatio_novalues;
                                if (BiotCoeff_novalues > 0)
                                    local_BiotCoefficient = BiotCoeff_total / (double)BiotCoeff_novalues;
                                if (CrackSurfaceEnergy_novalues > 0)
                                    local_CrackSurfaceEnergy = CrackSurfaceEnergy_total / (double)CrackSurfaceEnergy_novalues;
                                if (FrictionCoeff_novalues > 0)
                                    local_FrictionCoefficient = FrictionCoeff_total / (double)FrictionCoeff_novalues;
                                if (RockStrainRelaxation_novalues > 0)
                                    local_RockStrainRelaxation = RockStrainRelaxation_total / (double)RockStrainRelaxation_novalues;
                                if (FractureRelaxation_novalues > 0)
                                    local_FractureRelaxation = FractureRelaxation_total / (double)FractureRelaxation_novalues;
                            }
                            else // We are taking data from a single cell
                            {
                                // If there is no upscaling, we take the data from the uppermost cell that contains valid data
                                int PetrelGrid_DataCellI = PetrelGrid_FirstCellI;
                                int PetrelGrid_DataCellJ = PetrelGrid_FirstCellJ;

                                // If there is upscaling, we take data from the uppermost middle cell that contains valid data
                                if (HorizontalUpscalingFactor > 1)
                                {
                                    PetrelGrid_DataCellI += (HorizontalUpscalingFactor / 2);
                                    PetrelGrid_DataCellJ += (HorizontalUpscalingFactor / 2);
                                }

                                // Create a reference to the cell from which we will read the data
                                Index3 cellRef = new Index3(PetrelGrid_DataCellI, PetrelGrid_DataCellJ, PetrelGrid_TopCellK);

                                // Update initial microfracture density total if defined
                                if (UseGridFor_CapB)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_CapB = (double)CapB_grid[cellRef];
                                        if (!double.IsNaN(cell_CapB))
                                        {
                                            local_CapB = cell_CapB;
                                            break;
                                        }
                                    }
                                }

                                // Update initial microfracture size distribution total if defined
                                if (UseGridFor_smallc)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_smallc = (double)smallc_grid[cellRef];
                                        if (!double.IsNaN(cell_smallc))
                                        {
                                            local_smallc = cell_smallc;
                                            break;
                                        }
                                    }
                                }

                                // Update subcritical propagation index total if defined
                                if (UseGridFor_smallb)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_smallb = (double)smallb_grid[cellRef];
                                        if (!double.IsNaN(cell_smallb))
                                        {
                                            local_smallb = cell_smallb;
                                            break;
                                        }
                                    }
                                }

                                // Update Young's Modulus total if defined
                                if (UseGridFor_YoungsMod)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_YoungsMod = (double)YoungsMod_grid[cellRef];
                                        if (!double.IsNaN(cell_YoungsMod))
                                        {
                                            local_YoungsMod = cell_YoungsMod;
                                            break;
                                        }
                                    }
                                }

                                // Update Poisson's ratio total if defined
                                if (UseGridFor_PoissonsRatio)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_PoissonsRatio = (double)PoissonsRatio_grid[cellRef];
                                        if (!double.IsNaN(cell_PoissonsRatio))
                                        {
                                            local_PoissonsRatio = cell_PoissonsRatio;
                                            break;
                                        }
                                    }
                                }

                                // Update Biot coefficient total if defined
                                if (UseGridFor_BiotCoefficient)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_BiotCoeff = (double)BiotCoefficient_grid[cellRef];
                                        if (!double.IsNaN(cell_BiotCoeff))
                                        {
                                            local_BiotCoefficient = cell_BiotCoeff;
                                            break;
                                        }
                                    }
                                }

                                // Update crack surface energy total if defined
                                if (UseGridFor_CrackSurfaceEnergy)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_CrackSurfaceEnergy = (double)CrackSurfaceEnergy_grid[cellRef];
                                        if (!double.IsNaN(cell_CrackSurfaceEnergy))
                                        {
                                            local_CrackSurfaceEnergy = cell_CrackSurfaceEnergy;
                                            break;
                                        }
                                    }
                                }

                                // Update friction coefficient total if defined
                                if (UseGridFor_FrictionCoefficient)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_FrictionCoeff = (double)FrictionCoefficient_grid[cellRef];
                                        if (!double.IsNaN(cell_FrictionCoeff))
                                        {
                                            local_FrictionCoefficient = cell_FrictionCoeff;
                                            break;
                                        }
                                    }
                                }

                                // Update rock strain relaxation total if defined
                                if (UseGridFor_RockStrainRelaxation)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_RockStrainRelaxation = (double)RockStrainRelaxation_grid[cellRef];
                                        if (!double.IsNaN(cell_RockStrainRelaxation))
                                        {
                                            local_RockStrainRelaxation = cell_RockStrainRelaxation;
                                            break;
                                        }
                                    }
                                }

                                // Update fracture strain relaxation total if defined
                                if (UseGridFor_FractureRelaxation)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                    {
                                        cellRef.K = PetrelGrid_DataCellK;
                                        double cell_FractureRelaxation = (double)FractureRelaxation_grid[cellRef];
                                        if (!double.IsNaN(cell_FractureRelaxation))
                                        {
                                            local_FractureRelaxation = cell_FractureRelaxation;
                                            break;
                                        }
                                    }
                                }
                            } // End get the mechanical properties from the grid as required

                            // Create a new gridblock object
                            GridblockConfiguration gc = new GridblockConfiguration(LayerThickness, Depth);

                            // Set the cornerpoints
                            gc.setGridblockCorners(FractureGrid_SWtop, FractureGrid_SWbottom, FractureGrid_NWtop, FractureGrid_NWbottom, FractureGrid_NEtop, FractureGrid_NEbottom, FractureGrid_SEtop, FractureGrid_SEbottom);

                            // Set the mechanical properties
                            gc.MechProps.setMechanicalProperties(local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_smallb, timeUnits_in);
                            // Set the fracture aperture control properties
                            gc.MechProps.setFractureApertureControlData(DynamicApertureMultiplier_in, JRC_in, UCS_ratio_in, InitialNormalStress_in, FractureNormalStiffness_in, MaximumClosure_in);

                            // Set the initial stress and strain
                            // If the initial stress relaxation value is negative, set it to the required value for a critical initial stress state
                            double local_InitialStressRelaxation = InitialStressRelaxation;
                            if (InitialStressRelaxation < 0)
                            {
                                double opt_dip = ((Math.PI / 2) + Math.Atan(local_FrictionCoefficient)) / 2;
                                local_InitialStressRelaxation = (((1 - PoissonsRatio) * ((Math.Sin(opt_dip) * Math.Cos(opt_dip)) - (local_FrictionCoefficient * Math.Pow(Math.Cos(opt_dip), 2)))) - PoissonsRatio) / (1 - (2 * PoissonsRatio));
                            }
                            double lithostatic_stress = (mean_overlying_sediment_density * 9.81 * Depth);
                            double fluid_pressure = (fluid_density * 9.81 * Depth) + fluid_overpressure;
                            gc.StressStrain.SetInitialStressStrainState(lithostatic_stress, fluid_pressure, InitialStressRelaxation);

                            // Set the propagation control data
                            gc.PropControl.setPropagationControl(CalculatePopulationDistribution_in, no_l_indexPoints_in, maxLength, false, StressDistribution_in, max_TS_MFP33_increase_in, d_historic_MFP33_termination_ratio_in, active_total_MFP30_termination_ratio_in,
                                minimum_ClearZone_Volume_in, DeformationStageDuration_in, maxTimesteps_in, maxTimestepDuration_in, no_r_bins_in, minMicrofractureRadius_in, logCalculation_in, local_Epsilon_hmin_azimuth_in, local_Epsilon_hmin_rate_in, local_Epsilon_hmax_rate_in, timeUnits_in, CalculateFracturePorosity_in, FractureApertureControl_in);

                            // Set folder path for output files
                            gc.PropControl.FolderPath = folderPath;

                            // Create the fracture sets
                            if (Mode1Only)
                                gc.resetFractures(local_CapB, local_smallc, FractureMode.Mode1);
                            else if (Mode2Only)
                                gc.resetFractures(local_CapB, local_smallc, FractureMode.Mode2);
                            else
                                gc.resetFractures(local_CapB, local_smallc);

                            // Set the fracture aperture control data for fracture porosity calculation
                            gc.FractureSets[FractureOrientation.HMin].SetFractureApertureControlData(Mode1HMin_UniformAperture_in, Mode2HMin_UniformAperture_in, Mode1HMin_SizeDependentApertureMultiplier_in, Mode2HMin_SizeDependentApertureMultiplier_in);
                            gc.FractureSets[FractureOrientation.HMax].SetFractureApertureControlData(Mode1HMax_UniformAperture_in, Mode2HMax_UniformAperture_in, Mode1HMax_SizeDependentApertureMultiplier_in, Mode2HMax_SizeDependentApertureMultiplier_in);

#if DEBUG_FRACS
                            PetrelLogger.InfoOutputWindow("");
                            PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock Row {0}, Col {1}", FractureGrid_RowNo, FractureGrid_ColNo));
                            PetrelLogger.InfoOutputWindow(string.Format("SWtop = new PointXYZ({0}, {1}, {2});", FractureGrid_SWtop.X, FractureGrid_SWtop.Y, FractureGrid_SWtop.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("NWtop = new PointXYZ({0}, {1}, {2});", FractureGrid_NWtop.X, FractureGrid_NWtop.Y, FractureGrid_NWtop.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("NEtop = new PointXYZ({0}, {1}, {2});", FractureGrid_NEtop.X, FractureGrid_NEtop.Y, FractureGrid_NEtop.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("SEtop = new PointXYZ({0}, {1}, {2});", FractureGrid_SEtop.X, FractureGrid_SEtop.Y, FractureGrid_SEtop.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("SWbottom = new PointXYZ({0}, {1}, {2});", FractureGrid_SWbottom.X, FractureGrid_SWbottom.Y, FractureGrid_SWbottom.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("NWbottom = new PointXYZ({0}, {1}, {2});", FractureGrid_NWbottom.X, FractureGrid_NWbottom.Y, FractureGrid_NWbottom.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("NEbottom = new PointXYZ({0}, {1}, {2});", FractureGrid_NEbottom.X, FractureGrid_NEbottom.Y, FractureGrid_NEbottom.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("SEbottom = new PointXYZ({0}, {1}, {2});", FractureGrid_SEbottom.X, FractureGrid_SEbottom.Y, FractureGrid_SEbottom.Z));
                            PetrelLogger.InfoOutputWindow(string.Format("LayerThickness = {0}; Depth = {1};", LayerThickness, Depth));
                            PetrelLogger.InfoOutputWindow(string.Format("local_Epsilon_hmin_azimuth_in = {0}; Epsilon_hmin_dashed_in = {1}; Epsilon_hmax_dashed_in = {2};", local_Epsilon_hmin_azimuth_in, local_Epsilon_hmin_rate_in, local_Epsilon_hmax_rate_in));
                            PetrelLogger.InfoOutputWindow(string.Format("sv' {0}", gc.StressStrain.Sigma_v_eff()));
                            PetrelLogger.InfoOutputWindow(string.Format("Young's Mod: {0}, Poisson's ratio: {1}, Biot coefficient {2}, Crack surface energy:{3}, Friction coefficient:{4}", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient));
                            PetrelLogger.InfoOutputWindow(string.Format("gc = new GridblockConfiguration({0}, {1});", LayerThickness, Depth, local_CapB, local_smallc, local_smallb, InitialStressRelaxation, Mode1Only));
                            PetrelLogger.InfoOutputWindow(string.Format("gc.MechProps.setMechanicalProperties({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, TimeUnits.{9});", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_smallb, timeUnits_in));
                            PetrelLogger.InfoOutputWindow(string.Format("gc.StressStrain.setStressStrainState({0}, {1}, {2});", lithostatic_stress, fluid_pressure, InitialStressRelaxation));
                            PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.setPropagationControl({0}, {1}, {2}, {3}, StressDistribution.{4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, TimeUnits.{16});",
                                CalculatePopulationDistribution_in, no_l_indexPoints_in, "maxLength", StressDistribution_in, max_TS_MFP33_increase_in, d_historic_MFP33_termination_ratio_in, active_total_MFP30_termination_ratio_in,
                                minimum_ClearZone_Volume_in, DeformationStageDuration_in, maxTimesteps_in, maxTimestepDuration_in, no_r_bins_in, logCalculation_in, local_Epsilon_hmin_azimuth_in, local_Epsilon_hmin_rate_in, local_Epsilon_hmax_rate_in, timeUnits_in));
                            PetrelLogger.InfoOutputWindow(string.Format("gc.resetFractures({0}, {1}, {2});", local_CapB, local_smallc, (Mode1Only ? "Mode1" : (Mode2Only ? "Mode2" : "NoModeSpecified"))));
                            PetrelLogger.InfoOutputWindow(string.Format("ModelGrid.AddGridblock(gc, {0}, {1}, {2}, {3}, {4}, {5});", FractureGrid_RowNo, FractureGrid_ColNo, !faultToWest, !faultToSouth, true, true));
#endif

                            // Add to grid
                            ModelGrid.AddGridblock(gc, FractureGrid_RowNo, FractureGrid_ColNo, !faultToWest, !faultToSouth, true, true);

                            // Update gridblock counter
                            NoActiveGridblocks++;

                            //Update status bar
                            progressBarWrapper.UpdateProgress(NoActiveGridblocks);

                        } // End loop through all rows in the Fracture Grid
                    } // End loop through all columns in the Fracture Grid

                    // Set the DFN generation data
                    DFNGenerationControl dfn_control = new DFNGenerationControl(GenerateExplicitDFN, MinMicrofractureRadius, MinMacrofractureLength, -1, MinimumLayerThickness, MaxConsistencyAngle, cropAtBoundary, linkStressShadows, number_uF_Points, NoIntermediateOutputs, OutputIntermediatesByTime, writeDFNFiles_in, DFN_file_type, outputCentrepoints, probabilisticFractureNucleationLimit, SearchAdjacentGridblocks, propagateFracturesInNucleationOrder, timeUnits_in);
#if DEBUG_FRACS
                    PetrelLogger.InfoOutputWindow("");
                    PetrelLogger.InfoOutputWindow(string.Format("DFNGenerationControl dfn_control = new DFNGenerationControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, DFNFileType.{12}, {13}, {14}, {15}, {16}, TimeUnits.{17});", GenerateExplicitDFN, MinMicrofractureRadius, MinMacrofractureLength, -1, MinimumLayerThickness, MaxConsistencyAngle, cropAtBoundary, linkStressShadows, number_uF_Points, NoIntermediateOutputs, OutputIntermediatesByTime, writeDFNFiles_in, DFN_file_type, outputCentrepoints, probabilisticFractureNucleationLimit, SearchAdjacentGridblocks, propagateFracturesInNucleationOrder, timeUnits_in));
#endif
                    dfn_control.FolderPath = folderPath;
                    ModelGrid.DFNControl = dfn_control;

                    // Calculate implicit fractures for all gridblocks - unless the calculation has already been cancelled
                    if (!progressBarWrapper.abortCalculation())
                    {
                        PetrelLogger.InfoOutputWindow("Start calculating implicit data");
                        progressBar.SetProgressText("Calculating implicit data");
                        ModelGrid.CalculateAllFractureData(progressBarWrapper);
                    }

                    // Calculate explicit DFN - unless the calculation has already been cancelled
                    if (!progressBarWrapper.abortCalculation())
                    {
                        PetrelLogger.InfoOutputWindow("Start generating explicit DFN");
                        progressBar.SetProgressText("Generating explicit DFN");
                        ModelGrid.GenerateDFN(progressBarWrapper);
                    }

                    // If the calculation has already been cancelled, do not write any output data
                    if (!progressBarWrapper.abortCalculation())
                    {
                        PetrelLogger.InfoOutputWindow("Start writing output");

                        // Write implicit fracture property data to Petrel grid
                        PetrelLogger.InfoOutputWindow("Write implicit data");
                        progressBar.SetProgressText("Write implicit data");
                        Template P30Template = PetrelProject.WellKnownTemplates.MiscellaneousGroup.Intensity;
                        Template P32Template = PetrelProject.WellKnownTemplates.MiscellaneousGroup.Intensity;
                        Template LengthTemplate = PetrelProject.WellKnownTemplates.GeometricalGroup.Distance;
                        Template ConnectivityTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.Fraction;
                        Template AnisotropyTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.General;
                        Template PorosityTemplate = PetrelProject.WellKnownTemplates.PetrophysicalGroup.Porosity;
                        Template DeformationTimeTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.General;
                        using (ITransaction t = DataManager.NewTransaction())
                        {
                            // Get handle to parent object and lock database
                            PropertyCollection root = PetrelGrid.PropertyCollection;
                            t.Lock(root);

                            // Calculate the number of stages, the number of fracture sets and the total number of calculation elements
                            int NoStages = NoIntermediateOutputs + 1;
                            int NoSets = Enum.GetValues(typeof(FractureOrientation)).Length;
                            int NoDipSets = (Mode1Only || Mode2Only ? 1 : 2);
                            int NoCalculationElementsCompleted = 0;
                            int NoElements = NoActiveGridblocks * ((NoSets * NoDipSets) + (CalculateFractureConnectivityAnisotropy_in ? (NoSets * NoDipSets) + 1 : 0) + (CalculateFracturePorosity_in ? 1 : 0));
                            NoElements *= NoStages;
                            progressBarWrapper.SetNumberOfElements(NoElements);

                            // Get a list of end times for each gridblock - this is used to calculate the end times for the intermediate stages
                            List<double> timestepEndtimes = ModelGrid.GetTimestepEndtimeList();
                            int NoGridblockTimesteps = timestepEndtimes.Count;
                            double endTime = (NoGridblockTimesteps > 0 ? timestepEndtimes[NoGridblockTimesteps - 1] : 0);

                            // Loop through each stage in the fracture growth
                            for (int stageNumber = 1; stageNumber <= NoStages; stageNumber++)
                            {
                                // Set a flag to determine whether this is the final stage - if so we can read the output data directly from the final state of the gridblock objects
                                bool finalStage = (stageNumber == NoStages);

                                // Get the endtime for the current stage
                                // Run the calculation to the next required intermediate point, or to completion if no intermediates are required
                                double stageEndTime = 0;
                                if (OutputIntermediatesByTime)
                                {
                                    stageEndTime = (stageNumber * endTime) / NoStages;
                                }
                                else
                                {
                                    int gridblockTimestepNo = ((stageNumber * NoGridblockTimesteps) / NoStages) - 1;
                                    if (gridblockTimestepNo < 0)
                                        gridblockTimestepNo = 0;
                                    if (gridblockTimestepNo >= NoGridblockTimesteps)
                                        gridblockTimestepNo = NoGridblockTimesteps - 1;
                                    stageEndTime = timestepEndtimes[gridblockTimestepNo];
                                }

                                // Create a stage-specific label for the output file
                                string outputLabel = (stageNumber == NoStages ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, stageEndTime * timeUnits_Modifier, timeUnits_in));

#if DEBUG_FRACS
                                PetrelLogger.InfoOutputWindow("");
                                PetrelLogger.InfoOutputWindow("Stage" + outputLabel);
#endif

                                // Create a property folder for all sets
                                PropertyCollection FracData = root.CreatePropertyCollection(ModelName + outputLabel);

                                // Write the input parameters for the model run to the property folder comments string
                                FracData.Comments = generalInputParams + implicitInputParams;

                                // Loop through each fracture set
                                foreach (FractureOrientation FractureSetOrient in Enum.GetValues(typeof(FractureOrientation)).Cast<FractureOrientation>())
                                {
                                    for (int DipSetNo = 0; DipSetNo < NoDipSets; DipSetNo++)
                                    {
                                        // Create a subfolder for the fracture dip set
                                        string CollectionName = string.Format("{0}_Mode{1}_FracSetData", FractureSetOrient, (Mode2Only ? 2 : DipSetNo + 1));
                                        PropertyCollection FracSetData = FracData.CreatePropertyCollection(CollectionName);

                                        // Get templates for each property
                                        Property MF_P30_tot = FracSetData.CreateProperty(P30Template);
                                        MF_P30_tot.Name = "Layer_bound_fracture_P30";
                                        Property MF_P32_tot = FracSetData.CreateProperty(P32Template);
                                        MF_P32_tot.Name = "Layer_bound_fracture_P32";
                                        Property uF_P32_tot = FracSetData.CreateProperty(P32Template);
                                        uF_P32_tot.Name = "Microfracture_P32";
                                        Property MF_MeanLength = FracSetData.CreateProperty(LengthTemplate);
                                        MF_MeanLength.Name = "Mean_fracture_length";

                                        // Loop through all columns and rows in the Fracture Grid
                                        // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                        for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                            for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressBarWrapper.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_RowNo, FractureGrid_ColNo);
                                                if (fractureGridCell == null)
                                                    continue;

                                                // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                                                int PetrelGrid_FirstCellI = PetrelGrid_StartCellI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                int PetrelGrid_FirstCellJ = PetrelGrid_StartCellJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                int PetrelGrid_LastCellI = PetrelGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                int PetrelGrid_LastCellJ = PetrelGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);

                                                // Get data from GridblockConfiguration object
                                                double cell_MF_P30_tot, cell_MF_P32_tot, cell_uF_P32_tot, cell_MF_MeanLength;
                                                FractureDipSet fds = fractureGridCell.FractureSets[FractureSetOrient].FractureDipSets[DipSetNo];
                                                if (finalStage)
                                                {
                                                    cell_MF_P30_tot = (fds.a_MFP30_total() + fds.sII_MFP30_total() + fds.sIJ_MFP30_total()) / 2;
                                                    cell_MF_P32_tot = fds.a_MFP32_total() + fds.s_MFP32_total();
                                                    cell_uF_P32_tot = fds.a_uFP32_total() + fractureGridCell.FractureSets[FractureSetOrient].FractureDipSets[DipSetNo].s_uFP32_total();
                                                    cell_MF_MeanLength = fds.Mean_MF_HalfLength() * 2;
                                                }
                                                else
                                                {
                                                    int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                    cell_MF_P30_tot = fds.getTotalMFP30(TSNo);
                                                    cell_MF_P32_tot = fds.getTotalMFP32(TSNo);
                                                    cell_uF_P32_tot = fds.getTotaluFP32(TSNo);
                                                    double MFP30_Thickness = cell_MF_P30_tot * fractureGridCell.ThicknessAtDeformation;
                                                    cell_MF_MeanLength = (MFP30_Thickness > 0 ? 2 * (cell_MF_P32_tot / MFP30_Thickness) : 0);
                                                }

#if DEBUG_FRACS
                                                PetrelLogger.InfoOutputWindow("");
                                                PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                                // Loop through all the Petrel cells in the gridblock
                                                for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                                    for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                        for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                        {
#if DEBUG_FRACS
                                                            PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid cell {0}, {1}, {2}", PetrelGrid_I, PetrelGrid_J, PetrelGrid_K));
#endif

                                                            // Get index for cell in Petrel grid
                                                            Index3 index_cell = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                            // Write data to Petrel grid
                                                            MF_P30_tot[index_cell] = (float)cell_MF_P30_tot;
                                                            MF_P32_tot[index_cell] = (float)cell_MF_P32_tot;
                                                            uF_P32_tot[index_cell] = (float)cell_uF_P32_tot;
                                                            MF_MeanLength[index_cell] = (float)cell_MF_MeanLength;
                                                        } // End loop through all the Petrel cells in the gridblock

                                                // Update progress bar
                                                progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the Fracture Grid

                                        // If required, write fracture connectivity data to Petrel grid
                                        if (CalculateFractureConnectivityAnisotropy_in)
                                        {
                                            // Get templates for each property
                                            Property MF_UnconnectedTipRatio = FracSetData.CreateProperty(ConnectivityTemplate);
                                            MF_UnconnectedTipRatio.Name = "Unconnected_fracture_tip_ratio";
                                            Property MF_RelayTipRatio = FracSetData.CreateProperty(ConnectivityTemplate);
                                            MF_RelayTipRatio.Name = "Relay_zone_fracture_tip_ratio";
                                            Property MF_ConnectedTipRatio = FracSetData.CreateProperty(ConnectivityTemplate);
                                            MF_ConnectedTipRatio.Name = "Connected_fracture_tip_ratio";
                                            Property EndDeformationTime = FracSetData.CreateProperty(DeformationTimeTemplate);
                                            EndDeformationTime.Name = "Time_of_end_macrofracture_growth";

                                            // Loop through all columns and rows in the Fracture Grid
                                            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                            for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                                for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                                {
                                                    // Check if calculation has been aborted
                                                    if (progressBarWrapper.abortCalculation())
                                                    {
                                                        // Clean up any resources or data
                                                        break;
                                                    }

                                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                    GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_RowNo, FractureGrid_ColNo);
                                                    if (fractureGridCell == null)
                                                        continue;

                                                    // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                                                    int PetrelGrid_FirstCellI = PetrelGrid_StartCellI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                    int PetrelGrid_FirstCellJ = PetrelGrid_StartCellJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                    int PetrelGrid_LastCellI = PetrelGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                    int PetrelGrid_LastCellJ = PetrelGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);

                                                    // Get data from GridblockConfiguration object
                                                    FractureDipSet fds = fractureGridCell.FractureSets[FractureSetOrient].FractureDipSets[DipSetNo];
                                                    double UnconnectedTipRatio, RelayTipRatio, ConnectedTipRatio, EndTime;
                                                    if (finalStage)
                                                    {
                                                        UnconnectedTipRatio = fds.UnconnectedTipRatio();
                                                        RelayTipRatio = fds.RelayTipRatio();
                                                        ConnectedTipRatio = fds.ConnectedTipRatio();
                                                        EndTime = fds.getFinalActiveTime();
                                                    }
                                                    else
                                                    {
                                                        int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                        double INodes = fds.getActiveMFP30(TSNo);
                                                        double RNodes = fds.getStaticRelayMFP30(TSNo);
                                                        double YNodes = fds.getStaticIntersectMFP30(TSNo);
                                                        double TotalNodes = INodes + RNodes + YNodes;
                                                        UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : 0);
                                                        RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : 0);
                                                        ConnectedTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : 0);
                                                        EndTime = stageEndTime;
                                                    }

#if DEBUG_FRACS
                                                    PetrelLogger.InfoOutputWindow("");
                                                    PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                                    // Loop through all the Petrel cells in the gridblock
                                                    for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                                        for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                            for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                            {
#if DEBUG_FRACS
                                                                PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid cell {0}, {1}, {2}", PetrelGrid_I, PetrelGrid_J, PetrelGrid_K));
#endif

                                                                // Get index for cell in Petrel grid
                                                                Index3 index_cell = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                                // Write data to Petrel grid
                                                                MF_UnconnectedTipRatio[index_cell] = (float)UnconnectedTipRatio;
                                                                MF_RelayTipRatio[index_cell] = (float)RelayTipRatio;
                                                                MF_ConnectedTipRatio[index_cell] = (float)ConnectedTipRatio;
                                                                EndDeformationTime[index_cell] = (float)EndTime;
                                                            } // End loop through all the Petrel cells in the gridblock

                                                    // Update progress bar
                                                    progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                                } // End loop through all columns and rows in the Fracture Grid
                                        } // End write fracture connectivity data to Petrel grid

                                    } // End loop through fracture dip sets
                                } // End loop through fracture sets

                                // Write fracture anisotropy data to Petrel grid
                                if (CalculateFractureConnectivityAnisotropy_in)
                                {
                                    // Create a subfolder for the fracture anisotropy data
                                    string CollectionName = "Fracture_Anisotropy";
                                    PropertyCollection FracAnisotropyData = FracData.CreatePropertyCollection(CollectionName);

                                    // Get templates for each property
                                    Property P32_Anisotropy = FracAnisotropyData.CreateProperty(AnisotropyTemplate);
                                    P32_Anisotropy.Name = "P32_anisotropy";
                                    Property P33_Anisotropy = FracAnisotropyData.CreateProperty(AnisotropyTemplate);
                                    if (CalculateFracturePorosity_in)
                                        P33_Anisotropy.Name = "FracturePorosity_anisotropy";
                                    else
                                        P33_Anisotropy.Name = "P33_anisotropy";
                                    Property MF_UnconnectedTipRatio = FracAnisotropyData.CreateProperty(ConnectivityTemplate);
                                    MF_UnconnectedTipRatio.Name = "Unconnected_fracture_tip_ratio";
                                    Property MF_RelayTipRatio = FracAnisotropyData.CreateProperty(ConnectivityTemplate);
                                    MF_RelayTipRatio.Name = "Relay_zone_fracture_tip_ratio";
                                    Property MF_ConnectedTipRatio = FracAnisotropyData.CreateProperty(ConnectivityTemplate);
                                    MF_ConnectedTipRatio.Name = "Connected_fracture_tip_ratio";
                                    Property EndDeformationTime = FracAnisotropyData.CreateProperty(DeformationTimeTemplate);
                                    EndDeformationTime.Name = "Time_of_end_macrofracture_growth";

                                    // Loop through all columns and rows in the Fracture Grid
                                    // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                    for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                        for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressBarWrapper.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_RowNo, FractureGrid_ColNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                                            int PetrelGrid_FirstCellI = PetrelGrid_StartCellI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int PetrelGrid_FirstCellJ = PetrelGrid_StartCellJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int PetrelGrid_LastCellI = PetrelGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            int PetrelGrid_LastCellJ = PetrelGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);

                                            // Calculate fracture anisotropy data using the functions in the GridblockConfiguration object
                                            double P32_anisotropy, P33_anisotropy;
                                            double UnconnectedTipRatio, RelayTipRatio, ConnectedTipRatio, EndTime;
                                            if (finalStage)
                                            {
                                                P32_anisotropy = fractureGridCell.P32AnisotropyIndex();
                                                if (CalculateFracturePorosity_in)
                                                    P33_anisotropy = fractureGridCell.FracturePorosityAnisotropyIndex(FractureApertureControl_in);
                                                else
                                                    P33_anisotropy = fractureGridCell.P33AnisotropyIndex();

                                                // Calculate fracture connectivity data using the functions in the GridblockConfiguration object
                                                UnconnectedTipRatio = fractureGridCell.UnconnectedTipRatio();
                                                RelayTipRatio = fractureGridCell.RelayTipRatio();
                                                ConnectedTipRatio = fractureGridCell.ConnectedTipRatio();

                                                // Calculate end deformation time using the function in the GridblockConfiguration object
                                                // This will represent either the end of the deformation episode or the time of macrofracture saturation, whichever is earliest
                                                EndTime = fractureGridCell.getFinalActiveTime();
                                            }
                                            else
                                            {
                                                int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                double HMin_P32 = 0;
                                                double HMax_P32 = 0;
                                                double HMin_P33 = 0;
                                                double HMax_P33 = 0;
                                                double INodes = 0;
                                                double RNodes = 0;
                                                double YNodes = 0;
                                                foreach (FractureDipSet fds in fractureGridCell.FractureSets[FractureOrientation.HMin].FractureDipSets)
                                                {
                                                    HMin_P32 += fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo);
                                                    if (CalculateFracturePorosity_in)
                                                    {
                                                        HMin_P33 += fds.getTotaluFPorosity(FractureApertureControl_in, false, TSNo);
                                                        HMin_P33 += fds.getTotalMFPorosity(FractureApertureControl_in, false, TSNo);
                                                    }
                                                    else
                                                    {
                                                        HMin_P33 += fds.getTotaluFP33(TSNo);
                                                        HMin_P33 += fds.getTotalMFP33(TSNo);
                                                    }
                                                    INodes += fds.getActiveMFP30(TSNo);
                                                    RNodes += fds.getStaticRelayMFP30(TSNo);
                                                    YNodes += fds.getStaticIntersectMFP30(TSNo);
                                                }
                                                foreach (FractureDipSet fds in fractureGridCell.FractureSets[FractureOrientation.HMax].FractureDipSets)
                                                {
                                                    HMax_P32 += fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo);
                                                    if (CalculateFracturePorosity_in)
                                                    {
                                                        HMax_P33 += fds.getTotaluFPorosity(FractureApertureControl_in, false, TSNo);
                                                        HMax_P33 += fds.getTotalMFPorosity(FractureApertureControl_in, false, TSNo);
                                                    }
                                                    else
                                                    {
                                                        HMax_P33 += fds.getTotaluFP33(TSNo);
                                                        HMax_P33 += fds.getTotalMFP33(TSNo);
                                                    }
                                                    INodes += fds.getActiveMFP30(TSNo);
                                                    RNodes += fds.getStaticRelayMFP30(TSNo);
                                                    YNodes += fds.getStaticIntersectMFP30(TSNo);
                                                }

                                                double Combined_P32 = HMin_P32 + HMax_P32;
                                                P32_anisotropy = (Combined_P32 > 0 ? (HMin_P32 - HMax_P32) / Combined_P32 : 0);
                                                double Combined_P33 = HMin_P33 + HMax_P33;
                                                P33_anisotropy = (Combined_P33 > 0 ? (HMin_P33 - HMax_P33) / Combined_P33 : 0);

                                                double TotalNodes = INodes + RNodes + YNodes;
                                                UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : 0);
                                                RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : 0);
                                                ConnectedTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : 0);
                                                EndTime = stageEndTime;
                                            }

#if DEBUG_FRACS
                                            PetrelLogger.InfoOutputWindow("");
                                            PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                            // Loop through all the Petrel cells in the gridblock
                                            for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                                for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                    for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                    {
#if DEBUG_FRACS
                                                        PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid cell {0}, {1}, {2}", PetrelGrid_I, PetrelGrid_J, PetrelGrid_K));
#endif

                                                        // Get index for cell in Petrel grid
                                                        Index3 index_cell = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                        // Write data to Petrel grid
                                                        P32_Anisotropy[index_cell] = (float)P32_anisotropy;
                                                        P33_Anisotropy[index_cell] = (float)P33_anisotropy;
                                                        MF_UnconnectedTipRatio[index_cell] = (float)UnconnectedTipRatio;
                                                        MF_RelayTipRatio[index_cell] = (float)RelayTipRatio;
                                                        MF_ConnectedTipRatio[index_cell] = (float)ConnectedTipRatio;
                                                        EndDeformationTime[index_cell] = (float)EndTime;
                                                    } // End loop through all the Petrel cells in the gridblock

                                            // Update progress bar
                                            progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all columns and rows in the Fracture Grid

                                } // End write fracture anisotropy data


                                // Write fracture porosity data to Petrel grid
                                if (CalculateFracturePorosity_in)
                                {
                                    // Create a subfolder for the fracture porosity data
                                    string CollectionName = "Fracture_Porosity";
                                    PropertyCollection FracPorosityData = FracData.CreatePropertyCollection(CollectionName);

                                    // Create templates for microfracture and macrofracture porosity and combined P32 values
                                    Property uF_P32combined = FracPorosityData.CreateProperty(P32Template);
                                    Property MF_P32combined = FracPorosityData.CreateProperty(P32Template);
                                    Property uF_Porosity = FracPorosityData.CreateProperty(PorosityTemplate);
                                    Property MF_Porosity = FracPorosityData.CreateProperty(PorosityTemplate);

                                    // Set property template names
                                    uF_P32combined.Name = "Microfracture_combined_P32";
                                    MF_P32combined.Name = "Layer_bound_fracture_combined_P32";
                                    switch (FractureApertureControl_in)
                                    {
                                        case FractureApertureType.Uniform:
                                            uF_Porosity.Name = "Microfracture_porosity_UniformAperture";
                                            MF_Porosity.Name = "Layer_bound_fracture_porosity_UniformAperture";
                                            break;
                                        case FractureApertureType.SizeDependent:
                                            uF_Porosity.Name = "Microfracture_porosity_SizeDependentAperture";
                                            MF_Porosity.Name = "Layer_bound_fracture_porosity_SizeDependentAperture";
                                            break;
                                        case FractureApertureType.Dynamic:
                                            uF_Porosity.Name = "Microfracture_porosity_DynamicAperture";
                                            MF_Porosity.Name = "Layer_bound_fracture_porosity_DynamicAperture";
                                            break;
                                        case FractureApertureType.BartonBandis:
                                            uF_Porosity.Name = "Microfracture_porosity_BartonBandisAperture";
                                            MF_Porosity.Name = "Layer_bound_fracture_porosity_BartonBandisAperture";
                                            break;
                                        default:
                                            break;
                                    }

                                    // Loop through all columns and rows in the Fracture Grid
                                    // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                    for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                        for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressBarWrapper.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_RowNo, FractureGrid_ColNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                                            int PetrelGrid_FirstCellI = PetrelGrid_StartCellI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int PetrelGrid_FirstCellJ = PetrelGrid_StartCellJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int PetrelGrid_LastCellI = PetrelGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            int PetrelGrid_LastCellJ = PetrelGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);

                                            // Get combined fracture porosity data from the FractureSet objects in the GridblockConfiguration object and combine them locally
                                            double uF_P32_value = 0;
                                            double MF_P32_value = 0;
                                            double uF_Porosity_value = 0;
                                            double MF_Porosity_value = 0;

                                            if (finalStage)
                                            {
                                                foreach (Gridblock_FractureSet fs in fractureGridCell.FractureSets.Values)
                                                {
                                                    uF_P32_value += fs.combined_T_uFP32_total();
                                                    MF_P32_value += fs.combined_T_MFP32_total();
                                                    uF_Porosity_value += fs.combined_uF_Porosity(FractureApertureControl_in);
                                                    MF_Porosity_value += fs.combined_MF_Porosity(FractureApertureControl_in);
                                                }
                                            }
                                            else
                                            {
                                                foreach (Gridblock_FractureSet fs in fractureGridCell.FractureSets.Values)
                                                    foreach (FractureDipSet fds in fs.FractureDipSets)
                                                    {
                                                        int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                        uF_P32_value += fds.getTotaluFP32(TSNo);
                                                        MF_P32_value += fds.getTotalMFP32(TSNo);
                                                        // We will calculate the porosity based on fracture aperture during the respective stage, rather than the current aperture
                                                        uF_Porosity_value += fds.getTotaluFPorosity(FractureApertureControl_in, false, TSNo);
                                                        MF_Porosity_value += fds.getTotalMFPorosity(FractureApertureControl_in, false, TSNo);
                                                    }
                                            }

#if DEBUG_FRACS
                                            PetrelLogger.InfoOutputWindow("");
                                            PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                            // Loop through all the Petrel cells in the gridblock
                                            for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                                for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                    for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                    {
#if DEBUG_FRACS
                                                        PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid cell {0}, {1}, {2}", PetrelGrid_I, PetrelGrid_J, PetrelGrid_K));
#endif

                                                        // Get index for cell in Petrel grid
                                                        Index3 index_cell = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                        // Write data to Petrel grid
                                                        uF_P32combined[index_cell] = (float)uF_P32_value;
                                                        MF_P32combined[index_cell] = (float)MF_P32_value;
                                                        uF_Porosity[index_cell] = (float)uF_Porosity_value;
                                                        MF_Porosity[index_cell] = (float)MF_Porosity_value;
                                                    } // End loop through all the Petrel cells in the gridblock

                                            // Update progress bar
                                            progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all columns and rows in the Fracture Grid

                                } // End write fracture porosity data

                            } // End loop through each stage in the fracture growth

                            // Commit the changes to the Petrel database
                            t.Commit();
                        }

                        // Write explicit DFN to Petrel project
                        PetrelLogger.InfoOutputWindow("Write explicit DFN");
                        progressBar.SetProgressText("Write explicit DFN");
                        {
                            // Get the total number of fractures to write and update the progress bar
                            int totalNoFractures = 0;
#if DEBUG_FRACS
                            int stage = 1;
#endif
                            foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                            {
                                totalNoFractures += (DFN.GlobalDFNMicrofractures.Count + DFN.GlobalDFNMacrofractures.Count);
#if DEBUG_FRACS
                                PetrelLogger.InfoOutputWindow(string.Format("Stage {0}, {1} microfractures", stage, DFN.GlobalDFNMicrofractures.Count));
                                PetrelLogger.InfoOutputWindow(string.Format("Stage {0}, {1} macrofractures", stage++, DFN.GlobalDFNMacrofractures.Count));
#endif
                            }
#if DEBUG_FRACS
                            PetrelLogger.InfoOutputWindow(string.Format("Total {0} fractures", totalNoFractures));
#endif

                            // Set the number of elements in the progress bar to twice the total number of fractures
                            // We must loop through all the fractures twice - the first time to generate the fracture objects and the second to assign properties to them
                            // Unless we are generating fracture centrelines in which case we will need to loop through a third time
                            int numberOfElements = totalNoFractures * 2;
                            if (outputCentrepoints) numberOfElements += totalNoFractures;
                            progressBarWrapper.SetNumberOfElements(numberOfElements);
                            int noFracturesGenerated = 0;

                            // Loop through each stage in the fracture growth
                            int stageNumber = 1;
                            int NoStages = ModelGrid.DFNGrowthStages.Count;
                            foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                            {
                                // Create a stage-specific label for the output file
                                string outputLabel = (stageNumber == NoStages ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, DFN.CurrentTime * timeUnits_Modifier, timeUnits_in));

                                // Create a new fracture network object
                                FractureNetwork fractureNetwork;
                                using (ITransaction trans = DataManager.NewTransaction())
                                {
                                    // Get handle to parent object and lock database
                                    ModelCollection GridColl = PetrelGrid.ModelCollection;
                                    trans.Lock(GridColl);

                                    // Create a new fracture network object
                                    fractureNetwork = GridColl.CreateFractureNetwork(ModelName + outputLabel, Domain.ELEVATION_DEPTH);

                                    // Write the input parameters for the model run to the fracture network object comments string
                                    fractureNetwork.Comments = generalInputParams + explicitInputParams;

                                    // Commit the changes to the Petrel database
                                    trans.Commit();
                                }

                                // Write the model time of the intermediate DFN to the Petrel log window
                                PetrelLogger.InfoOutputWindow(string.Format("DFN realisation {0} at time {1} {2}", stageNumber, DFN.CurrentTime * timeUnits_Modifier, timeUnits_in));

                                // Create the fracture objects
                                using (ITransaction trans = DataManager.NewTransaction())
                                {
                                    // Lock the database
                                    trans.Lock(fractureNetwork);

                                    // Loop through all the microfractures in the DFN
                                    foreach (MicrofractureXYZ uF in DFN.GlobalDFNMicrofractures)
                                    {
                                        // Check if calculation has been aborted
                                        if (progressBarWrapper.abortCalculation())
                                        {
                                            // Clean up any resources or data
                                            break;
                                        }

                                        // Get the microfracture set
                                        //int uF_set = (uF.Set == FractureOrientation.HMin ? 1 : 2);

                                        // Get a list of cornerpoints around the circumference of the fracture, as PointXYZ objects
                                        List<PointXYZ> CornerPoints = uF.GetFractureCornerpointsInXYZ(number_uF_Points);

                                        if (createTriangularFractureSegments)
                                        {
                                            // Get a reference to the centre of the fracture as a PointXYZ object
                                            PointXYZ CP1 = uF.CentrePoint;

                                            // Loop through each cornerpoint creating a new triangular element
                                            for (int cornerPointNo = 0; cornerPointNo < number_uF_Points; cornerPointNo++)
                                            {
                                                // Create a collection of Petrel Point3 objects
                                                List<Point3> Petrel_CornerPoints = new List<Point3>();

                                                // Add the centrepoint, the current cornerpoint and the next cornerpoint to the Petrel point collection
                                                PointXYZ CP2 = CornerPoints[cornerPointNo];
                                                PointXYZ CP3 = (cornerPointNo < number_uF_Points - 1 ? CornerPoints[cornerPointNo + 1] : CornerPoints[0]);
                                                Petrel_CornerPoints.Add(new Point3(CP1.X, CP1.Y, CP1.Z));
                                                Petrel_CornerPoints.Add(new Point3(CP2.X, CP2.Y, CP2.Z));
                                                Petrel_CornerPoints.Add(new Point3(CP3.X, CP3.Y, CP3.Z));

                                                // Create a new fracture patch object
                                                FracturePatch Petrel_FractureSegment = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints);

                                                // Assign it to the correct set
                                                // Set assignment adds a high overhead in computational cost so for now we will not assign sets; this could be allowed as an option in future
                                                //Petrel_FractureSegment.FractureSetValue = uF_set;
                                            }

                                        }
                                        else
                                        {
                                            // Create a collection of Petrel Point3 objects
                                            List<Point3> Petrel_CornerPoints = new List<Point3>();

                                            // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the Petrel point collection
                                            foreach (PointXYZ CornerPoint in CornerPoints)
                                                Petrel_CornerPoints.Add(new Point3(CornerPoint.X, CornerPoint.Y, CornerPoint.Z));

                                            // Create a new fracture patch object
                                            FracturePatch Petrel_FractureSegment = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints);

                                            // Assign it to the correct set
                                            // Set assignment adds a high overhead in computational cost so for now we will not assign sets; this could be allowed as an option in future
                                            //Petrel_FractureSegment.FractureSetValue = uF_set;
                                        }

                                        // Update progress bar
                                        progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                    }

                                    // Loop through all the macrofractures in the DFN
                                    foreach (MacrofractureXYZ MF in DFN.GlobalDFNMacrofractures)
                                    {
                                        // Check if calculation has been aborted
                                        if (progressBarWrapper.abortCalculation())
                                        {
                                            // Clean up any resources or data
                                            break;
                                        }

                                        // Get the macrofracture set
                                        //int MF_set = (MF.Set == FractureOrientation.HMin ? 1 : 2);

                                        // Loop through each macrofracture segment
                                        foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                        {
                                            int noSegments = MF.SegmentCornerPoints[dir].Count;
                                            for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                                            {
                                                // Get a reference to the cornerpoint list for the segment
                                                List<PointXYZ> segment = MF.SegmentCornerPoints[dir][segmentNo];

                                                if (createTriangularFractureSegments)
                                                {
                                                    // Create two new collections of Petrel Point3 objects
                                                    List<Point3> Petrel_CornerPoints1 = new List<Point3>();
                                                    List<Point3> Petrel_CornerPoints2 = new List<Point3>();

                                                    // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the correct Petrel point collection
                                                    // Since the four cornerpoints in the original list are arranged in order moving around the edge of the fracture segment, we will use the first, second and third points for the first triangular element
                                                    // and the third, fourth and first points for the second triangular element
                                                    PointXYZ CP1 = segment[0];
                                                    PointXYZ CP2 = segment[1];
                                                    PointXYZ CP3 = segment[2];
                                                    PointXYZ CP4 = segment[3];
                                                    Petrel_CornerPoints1.Add(new Point3(CP1.X, CP1.Y, CP1.Z));
                                                    Petrel_CornerPoints1.Add(new Point3(CP2.X, CP2.Y, CP2.Z));
                                                    Petrel_CornerPoints1.Add(new Point3(CP3.X, CP3.Y, CP3.Z));
                                                    Petrel_CornerPoints2.Add(new Point3(CP3.X, CP3.Y, CP3.Z));
                                                    Petrel_CornerPoints2.Add(new Point3(CP4.X, CP4.Y, CP4.Z));
                                                    Petrel_CornerPoints2.Add(new Point3(CP1.X, CP1.Y, CP1.Z));

                                                    // Create two new fracture patch objects
                                                    FracturePatch Petrel_FractureSegment1 = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints1);
                                                    FracturePatch Petrel_FractureSegment2 = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints2);

                                                    // Assign them to the correct set
                                                    // Set assignment adds a high overhead in computational cost so for now we will not assign sets; this could be allowed as an option in future
                                                    //Petrel_FractureSegment1.FractureSetValue = MF_set;
                                                    //Petrel_FractureSegment2.FractureSetValue = MF_set;
                                                }
                                                else
                                                {
                                                    // Create a collection of Petrel Point3 objects
                                                    List<Point3> Petrel_CornerPoints = new List<Point3>();

                                                    // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the Petrel point collection
                                                    foreach (PointXYZ CornerPoint in segment)
                                                        Petrel_CornerPoints.Add(new Point3(CornerPoint.X, CornerPoint.Y, CornerPoint.Z));

                                                    // Create a new fracture patch object
                                                    FracturePatch Petrel_FractureSegment = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints);

                                                    // Assign it to the correct set
                                                    // Set assignment adds a high overhead in computational cost so for now we will not assign sets; this could be allowed as an option in future
                                                    //Petrel_FractureSegment.FractureSetValue = MF_set;
                                                }
                                            }

                                        }

                                        // Update progress bar
                                        progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                    }

                                    // Commit the changes to the Petrel database
                                    trans.Commit();
                                }

                                // Assign the fracture properties
                                using (ITransaction trans = DataManager.NewTransaction())
                                {

                                    // Get handle to the appropriate fracture properties
                                    // At present we will only write fracture aperture data - this could be expanded to include fracture permeability, connectivity and nucleation time
                                    FracturePatchProperty fractureAperture = FracturePatchProperty.NullObject;
                                    FracturePatchPropertyType aperture = WellKnownFracturePatchPropertyTypes.Aperture;
                                    if (fractureNetwork.HasWellKnownProperty(aperture))
                                    {
                                        fractureAperture = fractureNetwork.GetWellKnownProperty(aperture);
                                        // Lock the database
                                        trans.Lock(fractureAperture);
                                    }
                                    else
                                    {
                                        // Lock the database
                                        trans.Lock(fractureNetwork);
                                        fractureAperture = fractureNetwork.CreateWellKnownProperty(aperture);
                                    }

                                    // Create a counter for the fracture patches
                                    int PatchNo = 0;
                                    int NoPatches = fractureNetwork.FracturePatchCount;

                                    // Loop through all the microfractures in the DFN
                                    foreach (MicrofractureXYZ uF in DFN.GlobalDFNMicrofractures)
                                    {
                                        // Check if calculation has been aborted
                                        if (progressBarWrapper.abortCalculation())
                                        {
                                            // Clean up any resources or data
                                            break;
                                        }

                                        // Get the fracture aperture
                                        double uF_Aperture = uF.MeanAperture;

                                        if (createTriangularFractureSegments)
                                        {
                                            // Get a reference to the centre of the fracture as a PointXYZ object
                                            //PointXYZ CP1 = uF.CentrePoint;

                                            // Loop through each cornerpoint creating a new triangular element
                                            for (int cornerPointNo = 0; cornerPointNo < number_uF_Points; cornerPointNo++)
                                            {
                                                // Assign the appropriate fracture properties to the new patch object
                                                if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = uF_Aperture;
                                                PatchNo++;
                                            }

                                        }
                                        else
                                        {
                                            // Assign the appropriate fracture properties to the new patch object
                                            if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = uF_Aperture;
                                            PatchNo++;
                                        }

                                        // Update progress bar
                                        progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                    }

                                    // Loop through all the macrofractures in the DFN
                                    foreach (MacrofractureXYZ MF in DFN.GlobalDFNMacrofractures)
                                    {
                                        // Check if calculation has been aborted
                                        if (progressBarWrapper.abortCalculation())
                                        {
                                            // Clean up any resources or data
                                            break;
                                        }

                                        // Loop through each macrofracture segment
                                        foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                        {
                                            int noSegments = MF.SegmentCornerPoints[dir].Count;
                                            for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                                            {
                                                // Get the aperture of the fracture segment
                                                double MF_Aperture = MF.SegmentMeanAperture[dir][segmentNo];

                                                if (createTriangularFractureSegments)
                                                {
                                                    // Assign the appropriate fracture properties to the two new patch objects
                                                    if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = MF_Aperture;
                                                    PatchNo++;
                                                    if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = MF_Aperture;
                                                    PatchNo++;
                                                }
                                                else
                                                {
                                                    // Assign the appropriate fracture properties to the new patch object
                                                    if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = MF_Aperture;
                                                    PatchNo++;
                                                }
                                            }

                                        }

                                        // Update progress bar
                                        progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                    }

                                    // Commit the changes to the Petrel database
                                    trans.Commit();
                                }

                                // Update the stage number
                                stageNumber++;
                            }

                            // If required, create fracture centrelines as polylines
                            if (outputCentrepoints)
                            {
                                // Create a new collection for the polyline output
                                Collection CentrelineCollection = Collection.NullObject;
                                using (ITransaction trans = DataManager.NewTransaction())
                                {
                                    // Get handle to project and lock database
                                    Project project = PetrelProject.PrimaryProject;
                                    trans.Lock(project);

                                    // Create a new collection for the polyline set
                                    CentrelineCollection = project.CreateCollection(ModelName + "_Centrelines");

                                    // Write the input parameters for the model run to the collection comments string
                                    CentrelineCollection.Comments = generalInputParams + explicitInputParams;

                                    // Commit the changes to the Petrel database
                                    trans.Commit();
                                }

                                // Loop through each stage in the fracture growth
                                stageNumber = 1;
                                foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                                {
                                    // Create a stage-specific label for the output file
                                    string outputLabel = (stageNumber == NoStages ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, DFN.CurrentTime * timeUnits_Modifier, timeUnits_in));

                                    using (ITransaction trans = DataManager.NewTransaction())
                                    {
                                        // Lock database
                                        trans.Lock(CentrelineCollection);

                                        // Create a new polyline set and set to the depth domain
                                        PolylineSet Centrelines = CentrelineCollection.CreatePolylineSet("Fracture_Centrelines" + outputLabel);
                                        Centrelines.Domain = Domain.ELEVATION_DEPTH;

                                        // Create a list of Petrel polylines
                                        List<Polyline3> pline_list = new List<Polyline3>();

                                        // Loop through all the macrofractures in the DFN
                                        foreach (MacrofractureXYZ MF in DFN.GlobalDFNMacrofractures)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressBarWrapper.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get the fracture centreline
                                            List<PointXYZ> grid_Centreline = MF.GetCentrepoints();

                                            // Create a new Petrel polyline from the centreline and add it to the polyline list
                                            List<Point3> newpline = new List<Point3>();
                                            foreach (PointXYZ grid_Point in grid_Centreline)
                                                newpline.Add(new Point3(grid_Point.X, grid_Point.Y, grid_Point.Z));
                                            pline_list.Add(new Polyline3(newpline));

                                            // Update progress bar
                                            progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                        }

                                        // Add the polyline list to the polyline set
                                        Centrelines.Polylines = pline_list;

                                        // Commit the changes to the Petrel database
                                        trans.Commit();
                                    }

                                    // Update the stage number
                                    stageNumber++;
                                }
                            }
                        }
#if DEBUG_FRACS
                        // Write cornerpoints of each fracture in the explicit DFN to Petrel project
                        using (ITransaction trans = DataManager.NewTransaction())
                        {
                            // Lock database
                            Project DFNroot = PetrelProject.PrimaryProject;
                            trans.Lock(DFNroot);

                            // Create a set of points to compare with the corners of the fracture patches, when debugging
                            PointSet FractureCorners = DFNroot.CreatePointSet("Fracture corners");
                            List<Point3> CornersAsPoints = new List<Point3>();

                            // Loop through all the macrofractures in the DFN
                            foreach (MacrofractureXYZ MF in ModelGrid.CurrentDFN.GlobalDFNMacrofractures)
                            {
                                // Get a nested list of cornerpoints for each segment of the fracture, as PointXYZ objects
                                List<List<PointXYZ>> Segments = MF.GetFractureSegmentsInXYZ();

                                // Loop through each segment in the list supplied
                                foreach (List<PointXYZ> Segment in Segments)
                                {
                                    // Create a collection of Petrel Point3 objects
                                    List<Point3> Petrel_CornerPoints = new List<Point3>();

                                    // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the Petrel point collection
                                    foreach (PointXYZ CornerPoint in Segment)
                                        Petrel_CornerPoints.Add(new Point3(CornerPoint.X, CornerPoint.Y, CornerPoint.minusZ));

                                    // Add the cornerpoints to the set of points for comparison with the corners of the fracture patches
                                    CornersAsPoints.AddRange(Petrel_CornerPoints);
                                }
                            }

                            // Add the set of points for comparison with the corners of the fracture patches
                            FractureCorners.Points = new Point3Set(CornersAsPoints);

                            // Commit the changes to the Petrel database
                            trans.Commit();
                        }
#endif
                    }

                }
                // Clean up
                PetrelLogger.InfoOutputWindow("Calculation completed");
                PetrelLogger.InfoOutputWindow("=====================");
            }
        }
        #endregion

        /// <summary>
        /// ArgumentPackage class for DFNWorkstep.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            public Arguments()
                : this(DataManager.DataSourceManager)
            {
            }

            public Arguments(IDataSourceManager dataSourceManager)
            {
            }

            // Default values: Petrel grid
            private Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid grid;
            // Default values: Model name
            private string argument_ModelName = "New DFN";
            // Default values: Geometric control parameters 
            private int argument_NoRowsJ = 3;
            private int argument_NoColsI = 3;
            private int argument_I_start_cell = 50;
            private int argument_J_start_cell = 50;
            private int argument_K_top_layer = 1;
            private int argument_K_base_layer = 10;
            // Default values: Stress state
            private double argument_Default_ehmin_orient = 0;
            private Property argument_ehmin_orient;
            private double argument_Default_ehmin_rate = -0.2;
            private Property argument_ehmin_rate;
            private double argument_Default_ehmax_rate = 0;
            private Property argument_ehmax_rate;
            private bool argument_StressDistribution = true;
            private double argument_DepthAtTimeOfFracture;
            private double argument_InitialOverpressure = 0;
            private double argument_MeanOverlyingSedimentDensity = 2250;
            private double argument_FluidDensity = 1000;
            private double argument_InitialStressRelaxation = 1;
            // Default values: Mechanical properties
            private double argument_Default_YoungsMod = 1E+10;
            private Property argument_YoungsMod;
            private double argument_Default_PoissonsRatio = 0.25;
            private Property argument_PoissonsRatio;
            private double argument_Default_BiotCoefficient = 1;
            private Property argument_BiotCoefficient;
            private double argument_Default_CrackSurfaceEnergy = 1000;
            private Property argument_CrackSurfaceEnergy;
            private double argument_Default_FrictionCoefficient = 0.5;
            private Property argument_FrictionCoefficient;
            private double argument_CriticalPropagationRate = 2000;
            private double argument_Default_SubcriticalPropIndex = 10;
            private Property argument_SubcriticalPropIndex;
            private double argument_Default_RockStrainRelaxation = 0;
            private Property argument_RockStrainRelaxation;
            private double argument_Default_FractureRelaxation = 0;
            private Property argument_FractureRelaxation;
            private double argument_Default_InitialMicrofractureDensity = 0.001;
            private Property argument_InitialMicrofractureDensity;
            private double argument_Default_InitialMicrofractureSizeDistribution = 2;
            private Property argument_InitialMicrofractureSizeDistribution;
            // Default values: Calculation control parameters
            private int argument_FractureMode;
            private int argument_HorizontalUpscalingFactor = 1;
            private bool argument_AverageStressStrainData = false;
            private bool argument_AverageMechanicalPropertyData = true;
            private bool argument_GenerateExplicitDFN = true;
            private bool argument_outputCentrepoints = false;
            private bool argument_CropAtGridBoudary = true;
            private bool argument_LinkParallelFractures = true;
            private double argument_MinimumMicrofractureRadius;
            private int argument_NoMicrofractureCornerpoints = 8;
            private bool argument_CreateTriangularFractureSegments = false;
            private double argument_DeformationDuration = -1;
            private int argument_MaxNoTimesteps = 1000;
            private double argument_MaxTSDuration = -1;
            private int argument_NoIntermediateOutputs = 0;
            private bool argument_OutputIntermediatesByTime = false;
            private double argument_Max_TS_MFP33_increase = 0.002;
            private double argument_Historic_MFP33_TerminationRatio = -1;
            private double argument_Active_MFP30_TerminationRatio = -1;
            private double argument_minimum_ClearZone_Volume = 0.001;
            private double argument_MinimumLayerThickness = 1;
            private double argument_MaxConsistencyAngle = 45;
            private double argument_probabilisticFractureNucleationLimit = -1;
            private int argument_SearchAdjacentGridblocks = 2;
            private bool argument_propagateFracturesInNucleationOrder = true;
            private bool argument_logCalculation = false;
            private bool argument_writeDFNFiles = false;
            private int argument_DFNFileType = 1;
            // Default values: Fracture connectivity and anisotropy index control parameters
            private bool argument_CalculateFractureConnectivityAnisotropy = true;
            // Default values: Fracture porosity control parameters
            private bool argument_CalculateFracturePorosity = true;
            private int argument_FractureApertureControl = 1;
            private double argument_MinEffectiveMicrofractureRadius = 0.05;
            private int argument_no_r_bins = 10;
            private double argument_HMin_UniformAperture = 0.0005;
            private double argument_HMax_UniformAperture = 0.0005;
            private double argument_HMin_SizeDependentApertureMultiplier = 0.00001;
            private double argument_HMax_SizeDependentApertureMultiplier = 0.00001;
            private double argument_DynamicApertureMultiplier = 1;
            private double argument_JRC = 10;
            private double argument_UCSratio = 2;
            private double argument_InitialNormalStress = 200000;
            private double argument_FractureNormalStiffness = 2.5E+9;
            private double argument_MaximumClosure = 0.0005;

            // Model name
            [Description("Model name", "Model name")]
            public string Argument_ModelName
            {
                internal get { return this.argument_ModelName; }
                set { this.argument_ModelName = value; }
            }

            // Geometric control parameters
            [Description("Grid", "Grid from a model")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid Grid
            {
                internal get { return this.grid; }
                set { this.grid = value; }
            }

            [Description("Number of columns (width E-W)", "Number of columns (width E-W)")]
            public int Argument_NoColsI
            {
                internal get { return argument_NoColsI; }
                set { this.argument_NoColsI = value; }
            }

            [Description("Number of rows (length N-S)", "Number of rows (length N-S)")]
            public int Argument_NoRowsJ
            {
                internal get { return this.argument_NoRowsJ; }
                set { this.argument_NoRowsJ = value; }
            }

            [Description("Start column (East to West)", "Start column (East to West)")]
            public int Argument_I_start_cell
            {
                internal get { return argument_I_start_cell; }
                set { this.argument_I_start_cell = value; }
            }

            [Description("Start row (South to North)", "Start row (South to North)")]
            public int Argument_J_start_cell
            {
                internal get { return argument_J_start_cell; }
                set { this.argument_J_start_cell = value; }
            }

            [Description("Top Layer", "Top Layer")]
            public int Argument_K_top_layer
            {
                internal get { return argument_K_top_layer; }
                set { this.argument_K_top_layer = value; }
            }
            [Description("Base Layer", "Base Layer")]
            public int Argument_K_base_layer
            {
                internal get { return argument_K_base_layer; }
                set { this.argument_K_base_layer = value; }
            }

            // Stress state
            [Description("Default minimum strain orientation (deg)", "Default minimum strain orientation (deg)")]
            public double Argument_Default_ehmin_orient
            {
                internal get { return this.argument_Default_ehmin_orient; }
                set { this.argument_Default_ehmin_orient = value; }
            }

            [Description("Minimum strain orientation (deg)", "Minimum strain orientation (deg)")]
            public Property Argument_ehmin_orient
            {
                internal get { return this.argument_ehmin_orient; }
                set { this.argument_ehmin_orient = value; }
            }

            [Description("Default minimum strain rate (/ma); negative for extensional strain", "Default minimum strain rate (/ma); negative for extensional strain")]
            public double Argument_Default_ehmin_rate
            {
                internal get { return this.argument_Default_ehmin_rate; }
                set { this.argument_Default_ehmin_rate = value; }
            }

            [Description("Minimum strain rate (/ma); negative for extensional strain", "Minimum strain rate (/ma); negative for extensional strain")]
            public Property Argument_ehmin_rate
            {
                internal get { return this.argument_ehmin_rate; }
                set { this.argument_ehmin_rate = value; }
            }

            [Description("Default maximum strain rate (/ma); negative for extensional strain", "Default maximum strain rate (/ma); negative for extensional strain")]
            public double Argument_Default_ehmax_rate
            {
                internal get { return this.argument_Default_ehmax_rate; }
                set { this.argument_Default_ehmax_rate = value; }
            }

            [Description("Maximum strain rate (/ma); negative for extensional strain", "Maximum strain rate (/ma); negative for extensional strain")]
            public Property Argument_ehmax_rate
            {
                internal get { return this.argument_ehmax_rate; }
                set { this.argument_ehmax_rate = value; }
            }

            [Description("Allow stress shadows?", "Allow stress shadows?")]
            public bool Argument_StressDistribution
            {
                internal get { return this.argument_StressDistribution; }
                set { this.argument_StressDistribution = value; }
            }

            [Description("Depth at time of fracture (m); leave blank to use current depth", "Depth at time of fracture (m); leave blank to use current depth")]
            public double Argument_DepthAtTimeOfFracture
            {
                internal get { return this.argument_DepthAtTimeOfFracture; }
                set { this.argument_DepthAtTimeOfFracture = value; }
            }

            [Description("Initial fluid overpressure (Pa)", "Initial fluid overpressure (Pa)")]
            public double Argument_InitialOverpressure
            {
                internal get { return this.argument_InitialOverpressure; }
                set { this.argument_InitialOverpressure = value; }
            }

            [Description("Mean density of overlying sediments (kg/m3)", "Mean density of overlying sediments (kg/m3)")]
            public double Argument_MeanOverlyingSedimentDensity
            {
                internal get { return this.argument_MeanOverlyingSedimentDensity; }
                set { this.argument_MeanOverlyingSedimentDensity = value; }
            }

            [Description("Fluid density (kg/m3)", "Fluid density (kg/m3)")]
            public double Argument_FluidDensity
            {
                internal get { return this.argument_FluidDensity; }
                set { this.argument_FluidDensity = value; }
            }

            [Description("Initial stress relaxation", "Initial stress relaxation: set to 0 for elastic equilibrium, 1 for viscoelastic equilibrium, -1 for critical stress state")]
            public double Argument_InitialStressRelaxation
            {
                internal get { return this.argument_InitialStressRelaxation; }
                set { this.argument_InitialStressRelaxation = value; }
            }

            // Mechanical properties
            [Description("Default Young's Modulus (Pa)", "Default Young's Modulus (Pa)")]
            public double Argument_Default_YoungsMod
            {
                internal get { return this.argument_Default_YoungsMod; }
                set { this.argument_Default_YoungsMod = value; }
            }

            [Description("Young's Modulus (Pa)", "Young's Modulus (Pa)")]
            public Property Argument_YoungsMod
            {
                internal get { return this.argument_YoungsMod; }
                set { this.argument_YoungsMod = value; }
            }

            [Description("Default Poisson's ratio", "Default Poisson's ratio")]
            public double Argument_Default_PoissonsRatio
            {
                internal get { return this.argument_Default_PoissonsRatio; }
                set { this.argument_Default_PoissonsRatio = value; }
            }

            [Description("Poisson's ratio", "Poisson's ratio")]
            public Property Argument_PoissonsRatio
            {
                internal get { return this.argument_PoissonsRatio; }
                set { this.argument_PoissonsRatio = value; }
            }

            [Description("Default Biot's coefficient", "Default Biot's coefficient")]
            public double Argument_Default_BiotCoefficient
            {
                internal get { return this.argument_Default_BiotCoefficient; }
                set { this.argument_Default_BiotCoefficient = value; }
            }

            [Description("Biot's coefficient", "Biot's coefficient")]
            public Property Argument_BiotCoefficient
            {
                internal get { return this.argument_BiotCoefficient; }
                set { this.argument_BiotCoefficient = value; }
            }

            [Description("Default crack surface energy (J/m2)", "Default crack surface energy (J/m2)")]
            public double Argument_Default_CrackSurfaceEnergy
            {
                internal get { return this.argument_Default_CrackSurfaceEnergy; }
                set { this.argument_Default_CrackSurfaceEnergy = value; }
            }

            [Description("Crack surface energy (J/m2)", "Crack surface energy (J/m2)")]
            public Property Argument_CrackSurfaceEnergy
            {
                internal get { return this.argument_CrackSurfaceEnergy; }
                set { this.argument_CrackSurfaceEnergy = value; }
            }

            [Description("Default friction coefficient", "Default friction coefficient")]
            public double Argument_Default_FrictionCoefficient
            {
                internal get { return this.argument_Default_FrictionCoefficient; }
                set { this.argument_Default_FrictionCoefficient = value; }
            }

            [Description("Friction coefficient", "Friction coefficient")]
            public Property Argument_FrictionCoefficient
            {
                internal get { return this.argument_FrictionCoefficient; }
                set { this.argument_FrictionCoefficient = value; }
            }

            [Description("Critical fracture propagation rate (m/s)", "Critical fracture propagation rate (m/s)")]
            public double Argument_CriticalPropagationRate
            {
                internal get { return this.argument_CriticalPropagationRate; }
                set { this.argument_CriticalPropagationRate = value; }
            }

            [Description("Default Subcritical frac. prop. index", "Default Subcritical frac. prop. index")]
            public double Argument_Default_SubcriticalPropIndex
            {
                internal get { return this.argument_Default_SubcriticalPropIndex; }
                set { this.argument_Default_SubcriticalPropIndex = value; }
            }

            [Description("Subcritical frac. prop. index", "Subcritical frac. prop. index")]
            public Property Argument_SubcriticalPropIndex
            {
                internal get { return this.argument_SubcriticalPropIndex; }
                set { this.argument_SubcriticalPropIndex = value; }
            }

            [Description("Default rock strain relaxation time constant (ma); set to 0 for no rock strain relaxation", "Default rock strain relaxation time constant (ma); set to 0 for no rock strain relaxation")]
            public double Argument_Default_RockStrainRelaxation
            {
                internal get { return this.argument_Default_RockStrainRelaxation; }
                set { this.argument_Default_RockStrainRelaxation = value; }
            }

            [Description("Rock strain relaxation time constant (ma)", "Rock strain relaxation time constant (ma)")]
            public Property Argument_RockStrainRelaxation
            {
                internal get { return this.argument_RockStrainRelaxation; }
                set { this.argument_RockStrainRelaxation = value; }
            }

            [Description("Default fracture strain relaxation time constant (ma); set to 0 for no fracture strain relaxation", "Default fracture strain relaxation time constant (ma); set to 0 for no fracture strain relaxation")]
            public double Argument_Default_FractureRelaxation
            {
                internal get { return this.argument_Default_FractureRelaxation; }
                set { this.argument_Default_FractureRelaxation = value; }
            }

            [Description("Fracture strain relaxation time constant (ma)", "Fracture strain relaxation time constant (ma)")]
            public Property Argument_FractureRelaxation
            {
                internal get { return this.argument_FractureRelaxation; }
                set { this.argument_FractureRelaxation = value; }
            }

            [Description("Default initial microfracture density (fr/m3)", "Default initial microfracture density (fr/m3)")]
            public double Argument_Default_InitialMicrofractureDensity
            {
                internal get { return this.argument_Default_InitialMicrofractureDensity; }
                set { this.argument_Default_InitialMicrofractureDensity = value; }
            }

            [Description("Initial microfracture density (fr/m3)", "Initial microfracture density (fr/m3)")]
            public Property Argument_InitialMicrofractureDensity
            {
                internal get { return this.argument_InitialMicrofractureDensity; }
                set { this.argument_InitialMicrofractureDensity = value; }
            }

            [Description("Default initial microfracture size distribution coefficient", "Default initial microfracture size distribution coefficient")]
            public double Argument_Default_InitialMicrofractureSizeDistribution
            {
                internal get { return this.argument_Default_InitialMicrofractureSizeDistribution; }
                set { this.argument_Default_InitialMicrofractureSizeDistribution = value; }
            }

            [Description("Initial microfracture size distribution coefficient", "Initial microfracture size distribution coefficient")]
            public Property Argument_InitialMicrofractureSizeDistribution
            {
                internal get { return this.argument_InitialMicrofractureSizeDistribution; }
                set { this.argument_InitialMicrofractureSizeDistribution = value; }
            }

            // Default values: Calculation control parameters
            [Description("Fracture mode (if blank, will select energetically optimal mode)", "Fracture mode (if blank, will select energetically optimal mode)")]
            public int Argument_FractureMode
            {
                internal get { return this.argument_FractureMode; }
                set { this.argument_FractureMode = value; }
            }

            [Description("Horizontal upscaling factor", "Horizontal upscaling factor")]
            public int Argument_HorizontalUpscalingFactor
            {
                internal get { return this.argument_HorizontalUpscalingFactor; }
                set { this.argument_HorizontalUpscalingFactor = value; }
            }

            [Description("Average stress/strain state across all cells? (if false, will use value of top middle cell)", "Average stress/strain state across all cells? (if false, will use value of the uppermost middle cell that contains valid data)")]
            public bool Argument_AverageStressStrainData
            {
                internal get { return this.argument_AverageStressStrainData; }
                set { this.argument_AverageStressStrainData = value; }
            }

            [Description("Average mechanical properties across all cells? (if false, will use value of top middle cell)", "Average mechanical properties across all cells? (if false, will use value of the uppermost middle cell that contains valid data)")]
            public bool Argument_AverageMechanicalPropertyData
            {
                internal get { return this.argument_AverageMechanicalPropertyData; }
                set { this.argument_AverageMechanicalPropertyData = value; }
            }

            [Description("Generate explicit DFN? (if false, will only generate implicit fracture data)", "Generate explicit DFN? (if false, will only generate implicit fracture data)")]
            public bool Argument_GenerateExplicitDFN
            {
                internal get { return this.argument_GenerateExplicitDFN; }
                set { this.argument_GenerateExplicitDFN = value; }
            }

            [Description("Output fractre centrelines as polylines?", "Output fractre centrelines as polylines?")]
            public bool Argument_OutputCentrepoints
            {
                internal get { return this.argument_outputCentrepoints; }
                set { this.argument_outputCentrepoints = value; }
            }

            [Description("Crop fractures at outer boundary of selected cells?", "Crop fractures at outer boundary of selected cells?")]
            public bool Argument_CropAtGridBoudary
            {
                internal get { return this.argument_CropAtGridBoudary; }
                set { this.argument_CropAtGridBoudary = value; }
            }

            [Description("Link parallel fractures within stress shadow range?", "Link parallel fractures within stress shadow range?")]
            public bool Argument_LinkParallelFractures
            {
                internal get { return this.argument_LinkParallelFractures; }
                set { this.argument_LinkParallelFractures = value; }
            }

            [Description("Minimum explicit microfracture radius (m); leave blank to generate no explicit microfractures", "Minimum explicit microfracture radius (m); leave blank to generate no explicit microfractures")]
            public double Argument_MinimumMicrofractureRadius
            {
                internal get { return this.argument_MinimumMicrofractureRadius; }
                set { this.argument_MinimumMicrofractureRadius = value; }
            }

            [Description("Number of cornerpoints in explicit microfractures", "Number of cornerpoints in explicit microfractures")]
            public int Argument_NoMicrofractureCornerpoints
            {
                internal get { return this.argument_NoMicrofractureCornerpoints; }
                set { this.argument_NoMicrofractureCornerpoints = value; }
            }

            [Description("Create triangular fracture segments?", "Flag to create triangular instead of quadrilateral fracture segments; will increase the total number of segments but generation algorithm may run faster")]
            public bool Argument_CreateTriangularFractureSegments
            {
                internal get { return this.argument_CreateTriangularFractureSegments; }
                set { this.argument_CreateTriangularFractureSegments = value; }
            }

            [Description("Maximum deformation episode duration (ma); set to -1 to stop automatically when fracture saturation is reached", "Maximum deformation episode duration (ma); set to -1 to stop automatically when fracture saturation is reached")]
            public double Argument_DeformationDuration
            {
                internal get { return this.argument_DeformationDuration; }
                set { this.argument_DeformationDuration = value; }
            }

            [Description("Maximum number of timesteps", "Maximum number of timesteps")]
            public int Argument_MaxNoTimesteps
            {
                internal get { return this.argument_MaxNoTimesteps; }
                set { this.argument_MaxNoTimesteps = value; }
            }

            [Description("Maximum duration for individual timesteps; set negative for no limit to timestep duration", "Maximum duration for individual timesteps; set negative for no limit to timestep duration")]
            public double Argument_MaxTSDuration
            {
                internal get { return this.argument_MaxTSDuration; }
                set { this.argument_MaxTSDuration = value; }
            }

            [Description("Output DFNs for intermediate stages of fracture growth", "Output DFNs for intermediate stages of fracture growth")]
            public int Argument_NoIntermediateOutputs
            {
                internal get { return this.argument_NoIntermediateOutputs; }
                set { this.argument_NoIntermediateOutputs = value; }
            }

            [Description("True to space intermediate outputs by time, false to space intermediate outputs by approximate fracture area", "True to space intermediate outputs by time, false to space intermediate outputs by approximate fracture area")]
            public bool Argument_OutputIntermediatesByTime
            {
                internal get { return this.argument_OutputIntermediatesByTime; }
                set { this.argument_OutputIntermediatesByTime = value; }
            }

            [Description("Maximum increase in macrofracture volumetric ratio allowed in one timestep", "Maximum increase in macrofracture volumetric ratio allowed in one timestep")]
            public double Argument_Max_TS_MFP33_increase
            {
                internal get { return this.argument_Max_TS_MFP33_increase; }
                set { this.argument_Max_TS_MFP33_increase = value; }
            }

            [Description("Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive", "Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive")]
            public double Argument_Historic_MFP33_TerminationRatio
            {
                internal get { return this.argument_Historic_MFP33_TerminationRatio; }
                set { this.argument_Historic_MFP33_TerminationRatio = value; }
            }

            [Description("Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive", "Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive")]
            public double Argument_Active_MFP30_TerminationRatio
            {
                internal get { return this.argument_Active_MFP30_TerminationRatio; }
                set { this.argument_Active_MFP30_TerminationRatio = value; }
            }

            [Description("Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be considered inactive", "Minimum required clear zone volume for active fractures")]
            public double Argument_Minimum_ClearZone_Volume
            {
                internal get { return this.argument_minimum_ClearZone_Volume; }
                set { this.argument_minimum_ClearZone_Volume = value; }
            }

            [Description("Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers", "Layer thickness cutoff")]
            public double Argument_MinimumLayerThickness
            {
                internal get { return this.argument_MinimumLayerThickness; }
                set { this.argument_MinimumLayerThickness = value; }
            }

            [Description("Maximum variation in fracture propagation azimuth allowed across gridblock boundary", "Maximum variation in fracture propagation azimuth allowed across gridblock boundary")]
            public double Argument_MaxConsistencyAngle
            {
                internal get { return this.argument_MaxConsistencyAngle; }
                set { this.argument_MaxConsistencyAngle = value; }
            }

            [Description("Probabilistic Fracture Nucleation Limit: allows fracture nucleation to be controlled probabilistically; set to 0 to disable probabilistic fracture nucleation, set to -1 for automatic probabilistic fracture nucleation", "Probabilistic Fracture Nucleation Limit: allows fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small; set to 0 to disable probabilistic fracture nucleation, set to -1 for automatic probabilistic fracture nucleation")]
            public double Argument_ProbabilisticFractureNucleationLimit
            {
                internal get { return this.argument_probabilisticFractureNucleationLimit; }
                set { this.argument_probabilisticFractureNucleationLimit = value; }
            }

            [Description("Flag to control whether to search adjacent gridblocks for stress shadow interaction: 0 = None, 1 = All, 3 = Automatic (determined independently for each gridblock based on the gridblock geometry)", "Flag to control whether to search adjacent gridblocks for stress shadow interaction")]
            public int Argument_SearchAdjacentGridblocks
            {
                internal get { return this.argument_SearchAdjacentGridblocks; }
                set { this.argument_SearchAdjacentGridblocks = value; }
            }

            [Description("Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set", "Propagated fractures in order of nucleation time, regardless of fracture set?")]
            public bool Argument_PropagateFracturesInNucleationOrder
            {
                internal get { return this.argument_propagateFracturesInNucleationOrder; }
                set { this.argument_propagateFracturesInNucleationOrder = value; }
            }

            [Description("Write implicit fracture data to file (will generate 1 file per gridblock)", "Write implicit fracture data to file (will generate 1 file per gridblock)")]
            public bool Argument_LogCalculation
            {
                internal get { return this.argument_logCalculation; }
                set { this.argument_logCalculation = value; }
            }

            [Description("Write explicit fracture data to file", "Write explicit fracture data to file")]
            public bool Argument_WriteDFNFiles
            {
                internal get { return this.argument_writeDFNFiles; }
                set { this.argument_writeDFNFiles = value; }
            }

            [Description("File type for explicit data: 1 = ASCII, 2 = FAB", "File type for explicit data: 1 = ASCII, 2 = FAB")]
            public int Argument_DFNFileType
            {
                internal get { return this.argument_DFNFileType; }
                set { this.argument_DFNFileType = value; }
            }

            // Fracture connectivity and anisotropy index control parameters
            [Description("Calculate fracture connectivity and anisotropy", "Calculate fracture connectivity and anisotropy")]
            public bool Argument_CalculateFractureConnectivityAnisotropy
            {
                internal get { return this.argument_CalculateFractureConnectivityAnisotropy; }
                set { this.argument_CalculateFractureConnectivityAnisotropy = value; }
            }

            // Fracture porosity control parameters
            [Description("Calculate fracture porosity", "Calculate fracture porosity")]
            public bool Argument_CalculateFracturePorosity
            {
                internal get { return this.argument_CalculateFracturePorosity; }
                set { this.argument_CalculateFracturePorosity = value; }
            }
            [Description("Method used to determine fracture aperture: 1 = Uniform Aperture, 2 = Size Dependent, 3 = Dynamic Aperture, 4 = Barton-Bandis", "Method used to determine fracture aperture: 1 = Uniform Aperture, 2 = Size Dependent, 3 = Dynamic Aperture, 4 = Barton-Bandis")]
            public int Argument_FractureApertureControl
            {
                internal get { return this.argument_FractureApertureControl; }
                set { this.argument_FractureApertureControl = value; }
            }
            [Description("Minimum effective microfracture radius: Cut-off radius for microfractures contributing to effective porosity (m)", "Minimum effective microfracture radius: Cut-off radius for microfractures contributing to effective porosity (m)")]
            public double Argument_MinEffectiveMicrofractureRadius
            {
                internal get { return this.argument_MinEffectiveMicrofractureRadius; }
                set { this.argument_MinEffectiveMicrofractureRadius = value; }
            }
            [Description("Number of bins used in numerical integration of uFP32", "Number of bins used in numerical integration of uFP32 - increase this to increase accuracy of the microfracture porosity calculation at the expense of runtime")]
            public int Argument_no_r_bins
            {
                internal get { return this.argument_no_r_bins; }
                set { this.argument_no_r_bins = value; }
            }
            [Description("Uniform Fracture Aperture: Aperture for fractures striking perpendicular to Shmin (m)", "Uniform Fracture Aperture: Aperture for fractures striking perpendicular to Shmin (m)")]
            public double Argument_HMin_UniformAperture
            {
                internal get { return this.argument_HMin_UniformAperture; }
                set { this.argument_HMin_UniformAperture = value; }
            }
            [Description("Uniform Fracture Aperture: Aperture for fractures striking perpendicular to Shmax (m)", "Uniform Fracture Aperture: Aperture for fractures striking perpendicular to Shmax (m)")]
            public double Argument_HMax_UniformAperture
            {
                internal get { return this.argument_HMax_UniformAperture; }
                set { this.argument_HMax_UniformAperture = value; }
            }
            [Description("Size-Dependent Fracture Aperture: Aperture multiplier for fractures striking perpendicular to Shmin", "Size-Dependent Fracture Aperture: Aperture multiplier for fractures striking perpendicular to Shmin; layer-bound fracture aperture is given by layer thickness times this multiplier")]
            public double Argument_HMin_SizeDependentApertureMultiplier
            {
                internal get { return this.argument_HMin_SizeDependentApertureMultiplier; }
                set { this.argument_HMin_SizeDependentApertureMultiplier = value; }
            }
            [Description("Size-Dependent Fracture Aperture: Aperture multiplier for fractures striking perpendicular to Shmax", "Size-Dependent Fracture Aperture: Aperture multiplier for fractures striking perpendicular to Shmax; layer-bound fracture aperture is given by layer thickness times this multiplier")]
            public double Argument_HMax_SizeDependentApertureMultiplier
            {
                internal get { return this.argument_HMax_SizeDependentApertureMultiplier; }
                set { this.argument_HMax_SizeDependentApertureMultiplier = value; }
            }
            [Description("Dynamic Fracture Aperture: Multiplier for dynamic aperture", "Dynamic Fracture Aperture: Multiplier for dynamic aperture")]
            public double Argument_DynamicApertureMultiplier
            {
                internal get { return this.argument_DynamicApertureMultiplier; }
                set { this.argument_DynamicApertureMultiplier = value; }
            }
            [Description("Barton-Bandis Fracture Aperture: Joint Roughness Coefficient", "Barton-Bandis Fracture Aperture: Joint Roughness Coefficient")]
            public double Argument_JRC
            {
                internal get { return this.argument_JRC; }
                set { this.argument_JRC = value; }
            }
            [Description("Barton-Bandis Fracture Aperture: Compressive strength ratio", "Barton-Bandis Fracture Aperture: Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock")]
            public double Argument_UCSratio
            {
                internal get { return this.argument_UCSratio; }
                set { this.argument_UCSratio = value; }
            }
            [Description("Barton-Bandis Fracture Aperture: Initial normal stress, at zero fracture closure (Pa)", "Barton-Bandis Fracture Aperture: Initial normal stress, at zero fracture closure (Pa)")]
            public double Argument_InitialNormalStress
            {
                internal get { return this.argument_InitialNormalStress; }
                set { this.argument_InitialNormalStress = value; }
            }
            [Description("Barton-Bandis Fracture Aperture: Stiffness normal to the fracture, at initial normal stress (Pa/mm)", "Barton-Bandis Fracture Aperture: Stiffness normal to the fracture, at initial normal stress (Pa/mm)")]
            public double Argument_FractureNormalStiffness
            {
                internal get { return this.argument_FractureNormalStiffness; }
                set { this.argument_FractureNormalStiffness = value; }
            }
            [Description("Barton-Bandis Fracture Aperture: Maximum fracture closure (m)", "Barton-Bandis Fracture Aperture: Maximum fracture closure (m)")]
            public double Argument_MaximumClosure
            {
                internal get { return this.argument_MaximumClosure; }
                set { this.argument_MaximumClosure = value; }
            }

        }

        #region IAppearance Members
        public event EventHandler<TextChangedEventArgs> TextChanged;
        protected void RaiseTextChanged()
        {
            if (this.TextChanged != null)
                this.TextChanged(this, new TextChangedEventArgs(this));
        }

        public string Text
        {
            get { return Description.Name; }
            private set
            {
                // TODO: implement set
                this.RaiseTextChanged();
            }
        }

        public event EventHandler<ImageChangedEventArgs> ImageChanged;
        protected void RaiseImageChanged()
        {
            if (this.ImageChanged != null)
                this.ImageChanged(this, new ImageChangedEventArgs(this));
        }

        public System.Drawing.Bitmap Image
        {
            get { return PetrelImages.Modules; }
            private set
            {
                // TODO: implement set
                this.RaiseImageChanged();
            }
        }
        #endregion

        #region IDescriptionSource Members

        /// <summary>
        /// Gets the description of the DFNWorkstep
        /// </summary>
        public IDescription Description
        {
            get { return DFNWorkstepDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the DFNWorkstep.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class DFNWorkstepDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private static DFNWorkstepDescription instance = new DFNWorkstepDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static DFNWorkstepDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of DFNWorkstep
            /// </summary>
            public string Name
            {
                get { return "DFN Generator Module"; }
            }
            /// <summary>
            /// Gets the short description of DFNWorkstep
            /// </summary>
            public string ShortDescription
            {
                get { return "Run fracture simulation"; }
            }
            /// <summary>
            /// Gets the detailed description of DFNWorkstep
            /// </summary>
            public string Description
            {
                get { return "Version 1. Please input parametres below."; }
            }

            #endregion
        }
        #endregion
    }
}