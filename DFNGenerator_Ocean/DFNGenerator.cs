// Set this flag to output detailed information on input parameters and properties for each gridblock
// Use for debugging only; will significantly increase runtime
//#define DEBUG_FRACS

// Set this flag to enable managed persistence of the dialog box input data
//#define MANAGED_PERSISTENCE

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using Slb.Ocean.Petrel.Basics;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using Slb.Ocean.Petrel.Data;
using Slb.Ocean.Petrel.Data.Persistence;
using Slb.Ocean.Basics;
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Shapes;
using Slb.Ocean.Units;

using DFNGenerator_SharedCode;

namespace DFNGenerator_Ocean
{
    /// <summary>
    /// This class contains all the methods and subclasses of the DFNGenerator.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    class DFNGenerator : Workstep<DFNGenerator.Arguments>, IExecutorSource, IAppearance, IDescriptionSource
    {
        #region Overridden Workstep methods

        /// <summary>
        /// Creates an empty Argument instance
        /// </summary>
        /// <returns>New Argument instance.</returns>

        protected override DFNGenerator.Arguments CreateArgumentPackageCore(IDataSourceManager dataSourceManager)
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
                //return "32231407-0f99-4178-a00b-8de946429384";
                // Change workstep UniqueID to a namespace-based ID
                return "JointFlow.DFNGenerator_Code.DFNGenerator_Ocean.DFNGenerator";
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
                try
                {
                    // Get handle to grid and check a valid grid has been supplied
                    object GridObject = arguments.Argument_Grid;

                    if (GridObject == null)
                    {
                        PetrelLogger.WarnBox("Please provide valid grid object");
                        return;
                    }
                    Grid PetrelGrid = arguments.Argument_Grid;

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

                    // Set hardcoded default values for all parameters

                    // Main properties
                    // Model name
                    string ModelName = arguments.Argument_ModelName;
                    if (ModelName.Length == 0) ModelName = "New DFN";
                    // Grid size
                    // NB the number of pillars is one more than the number of cells in any direction, and the cells are zero indexed, so we must subtract 2 to get the index number of the last cell in the grid
                    int maxI = PetrelGrid.NumPillarsIJ.I - 2;
                    int maxJ = PetrelGrid.NumPillarsIJ.J - 2;
                    int maxK = PetrelGrid.NumCellsIJK.K - 2;
                    // Counter to get number of active gridblocks (after upscaling)
                    int NoActiveGridblocks = 0;
                    // Get all uniform or default values from dialog box arguments
                    // Number of rows and columns to include
                    int NoPetrelGridRows = arguments.Argument_NoRowsJ;
                    if (NoPetrelGridRows < 1) NoPetrelGridRows = maxJ;
                    int NoPetrelGridCols = arguments.Argument_NoColsI;
                    if (NoPetrelGridCols < 1) NoPetrelGridCols = maxI;
                    // Index of start cell (lower left hand corner) - set to zero index
                    int PetrelGrid_StartCellI = arguments.Argument_StartColI - 1;
                    if (PetrelGrid_StartCellI < 0) PetrelGrid_StartCellI = 0;
                    if (PetrelGrid_StartCellI > maxI) PetrelGrid_StartCellI = maxI;
                    int PetrelGrid_StartCellJ = arguments.Argument_StartRowJ - 1;
                    if (PetrelGrid_StartCellJ < 0) PetrelGrid_StartCellJ = 0;
                    // Required because Petrel's internal row indexing is opposite to the external row indexing
                    PetrelGrid_StartCellJ = maxJ - PetrelGrid_StartCellJ - (NoPetrelGridRows - 1);
                    if (PetrelGrid_StartCellJ < 0) PetrelGrid_StartCellJ = 0;
                    if (PetrelGrid_StartCellJ > maxJ) PetrelGrid_StartCellJ = maxJ;
                    if (NoPetrelGridCols > (maxI - PetrelGrid_StartCellI + 1)) NoPetrelGridCols = (maxI - PetrelGrid_StartCellI + 1);
                    if (NoPetrelGridRows > (maxJ - PetrelGrid_StartCellJ + 1)) NoPetrelGridRows = (maxJ - PetrelGrid_StartCellJ + 1);
#if DEBUG_FRACS
                    PetrelLogger.InfoOutputWindow("");
                    PetrelLogger.InfoOutputWindow(string.Format("StartCellI {0}, maxI {1}, NoPetrelGridCols {2}", PetrelGrid_StartCellI, maxI, NoPetrelGridCols));
                    PetrelLogger.InfoOutputWindow(string.Format("StartCellJ {0}, maxJ {1}, NoPetrelGridRows {2}", PetrelGrid_StartCellJ, maxJ, NoPetrelGridRows));
                    double minX = double.NegativeInfinity;
                    double minY = double.NegativeInfinity;
                    double minZ = double.NegativeInfinity;
                    double maxX = double.PositiveInfinity;
                    double maxY = double.PositiveInfinity;
                    double maxZ = double.PositiveInfinity;
#endif
                    // Index of top and bottom rows - set to zero index
                    int PetrelGrid_TopCellK = arguments.Argument_TopLayerK - 1;
                    if (PetrelGrid_TopCellK < 0) PetrelGrid_TopCellK = 0;
                    if (PetrelGrid_TopCellK > maxK) PetrelGrid_TopCellK = maxK;
                    int PetrelGrid_BaseCellK = arguments.Argument_BottomLayerK - 1;
                    if (PetrelGrid_BaseCellK < PetrelGrid_TopCellK) PetrelGrid_BaseCellK = PetrelGrid_TopCellK;
                    if (PetrelGrid_BaseCellK > maxK) PetrelGrid_BaseCellK = maxK;
                    // Strain orientatation
                    double EhminAzi = 0;
                    if (!double.IsNaN(arguments.Argument_EhminAzi_default))
                        EhminAzi = arguments.Argument_EhminAzi_default; // Can also be set from grid property; NB value should be returned in radians as conversion should be carried out by the Petrel unit conversion functionality 
                    Property EhminAzi_grid = arguments.Argument_EhminAzi;
                    bool UseGridFor_EhminAzi = (EhminAzi_grid != null);
                    // Strain rates
                    // Set to negative value for extensional strain
                    // Ehmin is most tensile (i.e. most negative) horizontal strain rate
                    double EhminRate_GeologicalTimeUnits = 0;
                    if (!double.IsNaN(arguments.Argument_EhminRate_default))
                        EhminRate_GeologicalTimeUnits = arguments.Argument_EhminRate_default; // Can also be set from grid property
                    Property EhminRate_grid = arguments.Argument_EhminRate;
                    bool UseGridFor_EhminRate = (EhminRate_grid != null);
                    // Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern
                    double EhmaxRate_GeologicalTimeUnits = 0;
                    if (!double.IsNaN(arguments.Argument_EhmaxRate_default))
                        EhmaxRate_GeologicalTimeUnits = arguments.Argument_EhmaxRate_default; // Can also be set from grid property
                    Property EhmaxRate_grid = arguments.Argument_EhmaxRate;
                    bool UseGridFor_EhmaxRate = (EhmaxRate_grid != null);
                    // Time units set to seconds - unit conversion from geological time units is carried out automatically by the Petrel unit conversion functionality
                    // NB the calculation code uses seconds internally in any case, so this will not lead to any loss in accuracy
                    TimeUnits ModelTimeUnits = TimeUnits.second;

                    // Mechanical properties
                    double YoungsMod = 1E+10;
                    if (!double.IsNaN(arguments.Argument_YoungsMod_default))
                        YoungsMod = arguments.Argument_YoungsMod_default; // Can also be set from grid property
                    Property YoungsMod_grid = arguments.Argument_YoungsMod;
                    bool UseGridFor_YoungsMod = (YoungsMod_grid != null);
                    double PoissonsRatio = 0.25;
                    if (!double.IsNaN(arguments.Argument_PoissonsRatio_default))
                        PoissonsRatio = arguments.Argument_PoissonsRatio_default; // Can also be set from grid property
                    Property PoissonsRatio_grid = arguments.Argument_PoissonsRatio;
                    bool UseGridFor_PoissonsRatio = (PoissonsRatio_grid != null);
                    double BiotCoefficient = 1;
                    if (!double.IsNaN(arguments.Argument_BiotCoefficient_default))
                        BiotCoefficient = arguments.Argument_BiotCoefficient_default; // Can also be set from grid property
                    Property BiotCoefficient_grid = arguments.Argument_BiotCoefficient;
                    bool UseGridFor_BiotCoefficient = (BiotCoefficient_grid != null);
                    double FrictionCoefficient = 0.5;
                    if (!double.IsNaN(arguments.Argument_FrictionCoefficient_default))
                        FrictionCoefficient = arguments.Argument_FrictionCoefficient_default; // Can also be set from grid property
                    Property FrictionCoefficient_grid = arguments.Argument_FrictionCoefficient;
                    bool UseGridFor_FrictionCoefficient = (FrictionCoefficient_grid != null);
                    double CrackSurfaceEnergy = 1000;
                    if (!double.IsNaN(arguments.Argument_CrackSurfaceEnergy_default))
                        CrackSurfaceEnergy = arguments.Argument_CrackSurfaceEnergy_default; // Can also be set from grid property
                    Property CrackSurfaceEnergy_grid = arguments.Argument_CrackSurfaceEnergy;
                    bool UseGridFor_CrackSurfaceEnergy = (CrackSurfaceEnergy_grid != null);
                    // Strain relaxation data
                    // Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate
                    double RockStrainRelaxation = 0;
                    if (!double.IsNaN(arguments.Argument_RockStrainRelaxation_default))
                        RockStrainRelaxation = arguments.Argument_RockStrainRelaxation_default; // Can also be set from grid property
                    Property RockStrainRelaxation_grid = arguments.Argument_RockStrainRelaxation;
                    bool UseGridFor_RockStrainRelaxation = (RockStrainRelaxation_grid != null);
                    // Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only
                    double FractureRelaxation = 0;
                    if (!double.IsNaN(arguments.Argument_FractureStrainRelaxation_default))
                        FractureRelaxation = arguments.Argument_FractureStrainRelaxation_default; // Can also be set from grid property
                    Property FractureRelaxation_grid = arguments.Argument_FractureStrainRelaxation;
                    bool UseGridFor_FractureRelaxation = (FractureRelaxation_grid != null);
                    // Density of initial microfractures
                    double InitialMicrofractureDensity = 0;
                    if (!double.IsNaN(arguments.Argument_InitialMicrofractureDensity_default))
                        InitialMicrofractureDensity = arguments.Argument_InitialMicrofractureDensity_default; // Can also be set from grid property
                    Property InitialMicrofractureDensity_grid = arguments.Argument_InitialMicrofractureDensity;
                    bool UseGridFor_InitialMicrofractureDensity = (InitialMicrofractureDensity_grid != null);
                    // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
                    double InitialMicrofractureSizeDistribution = 2;
                    if (!double.IsNaN(arguments.Argument_InitialMicrofractureSizeDistribution_default))
                        InitialMicrofractureSizeDistribution = arguments.Argument_InitialMicrofractureSizeDistribution_default; // Can also be set from grid property
                    Property InitialMicrofractureSizeDistribution_grid = arguments.Argument_InitialMicrofractureSizeDistribution;
                    bool UseGridFor_InitialMicrofractureSizeDistribution = (InitialMicrofractureSizeDistribution_grid != null);
                    // Subritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
                    double SubcriticalPropIndex = 10;
                    if (!double.IsNaN(arguments.Argument_SubcriticalPropagationIndex_default))
                        SubcriticalPropIndex = arguments.Argument_SubcriticalPropagationIndex_default; // Can also be set from grid property
                    Property SubcriticalPropIndex_grid = arguments.Argument_SubcriticalPropagationIndex;
                    bool UseGridFor_SubcriticalPropIndex = (SubcriticalPropIndex_grid != null);
                    double CriticalPropagationRate = 2000;
                    if (!double.IsNaN(arguments.Argument_CriticalPropagationRate))
                        CriticalPropagationRate = arguments.Argument_CriticalPropagationRate;
                    // Flags for whether to average mechanical properties properties across the Petrel grid cells, or take the value from the top middle cell
                    bool AverageMechanicalPropertyData = arguments.Argument_AverageMechanicalPropertyData;

                    // Stress state
                    // Stress distribution scenario - use to turn on or off stress shadow effect
                    // Do not use DuctileBoundary as this is not yet implemented
                    StressDistribution StressDistributionScenario = (StressDistribution)arguments.Argument_StressDistribution;
                    // If depth at the time of deformation is specified, use this to calculate effective vertical stress instead of the current depth
                    // If DepthAtFracture is <=0 or NaN, OverwriteDepth will be set to false and DepthAtFracture will not be used
                    double DepthAtDeformation = arguments.Argument_DepthAtDeformation;
                    bool OverwriteDepth = (DepthAtDeformation > 0);
                    // Mean density of overlying sediments and fluid (kg/m3)
                    double MeanOverlyingSedimentDensity = 2250;
                    if (!double.IsNaN(arguments.Argument_MeanOverlyingSedimentDensity))
                        MeanOverlyingSedimentDensity = arguments.Argument_MeanOverlyingSedimentDensity;
                    double FluidDensity = 1000;
                    if (!double.IsNaN(arguments.Argument_FluidDensity))
                        FluidDensity = arguments.Argument_FluidDensity;
                    // Fluid overpressure (Pa)
                    double InitialOverpressure = 0;
                    if (!double.IsNaN(arguments.Argument_InitialOverpressure))
                        InitialOverpressure = arguments.Argument_InitialOverpressure;
                    // InitialStressRelaxation controls the initial horizontal stress, prior to the application of horizontal strain
                    // Set InitialStressRelaxation to 1 to have initial horizontal stress = vertical stress (viscoelastic equilibrium)
                    // Set InitialStressRelaxation to 0 to have initial horizontal stress = v/(1-v) * vertical stress (elastic equilibrium)
                    // Set InitialStressRelaxation to -1 for initial horizontal stress = Mohr-Coulomb failure stress (critical stress state)
                    double InitialStressRelaxation = -1;
                    if (!double.IsNaN(arguments.Argument_InitialStressRelaxation))
                        InitialStressRelaxation = arguments.Argument_InitialStressRelaxation;
                    // Flags for whether to average stress and strain data across the Petrel grid cells, or take the value from the top middle cell
                    bool AverageStressStrainData = arguments.Argument_AverageStressStrainData;

                    // Outputs
                    // Output to file
                    // These must be set to true for stand-alone version or no output will be generated
#if DEBUG_FRACS
                    bool WriteImplicitDataFiles = true;
                    bool WriteDFNFiles = true;
#else
                    bool WriteImplicitDataFiles = arguments.Argument_WriteImplicitDataFiles;
                    bool WriteDFNFiles = arguments.Argument_WriteDFNFiles;
#endif
                    bool WriteToProjectFolder = arguments.Argument_WriteToProjectFolder;
                    DFNFileType OutputDFNFileType = (DFNFileType)arguments.Argument_DFNFileType;
                    // Output DFN at intermediate stages of fracture growth
                    int NoIntermediateOutputs = 0;
                    if (arguments.Argument_NoIntermediateOutputs > 0)
                        NoIntermediateOutputs = arguments.Argument_NoIntermediateOutputs;
                    bool OutputAtEqualTimeIntervals = arguments.Argument_OutputAtEqualTimeIntervals;
                    // Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints
                    bool OutputCentrepoints = arguments.Argument_OutputCentrepoints;
                    // Flag to output the bulk rock compliance and stiffness tensors
                    bool OutputBulkRockElasticTensors = arguments.Argument_CalculateBulkRockElasticTensors;
                    // Fracture connectivity and anisotropy index control parameters
                    // Flag to calculate and output fracture connectivity and anisotropy indices
                    bool CalculateFractureConnectivityAnisotropy = arguments.Argument_CalculateFractureConnectivityAnisotropy;
                    // Flag to calculate and output fracture porosity
                    bool CalculateFracturePorosity = arguments.Argument_CalculateFracturePorosity;

                    // Fracture aperture control parameters
                    // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
                    FractureApertureType FractureApertureControl = (FractureApertureType)arguments.Argument_FractureApertureControl;
                    // Fracture aperture control parameters: Uniform fracture aperture
                    // Fixed aperture for Mode 1 fractures striking perpendicular to hmin in the uniform aperture case (m)
                    double Mode1HMin_UniformAperture = 0;
                    if (!double.IsNaN(arguments.Argument_HMin_UniformAperture))
                        Mode1HMin_UniformAperture = arguments.Argument_HMin_UniformAperture;
                    // Fixed aperture for Mode 2 fractures striking perpendicular to hmin in the uniform aperture case (m)
                    double Mode2HMin_UniformAperture = 0;
                    if (!double.IsNaN(arguments.Argument_HMin_UniformAperture))
                        Mode2HMin_UniformAperture = arguments.Argument_HMin_UniformAperture;
                    // Fixed aperture for Mode 1 fractures striking perpendicular to hmax in the uniform aperture case (m)
                    double Mode1HMax_UniformAperture = 0;
                    if (!double.IsNaN(arguments.Argument_HMax_UniformAperture))
                        Mode1HMax_UniformAperture = arguments.Argument_HMax_UniformAperture;
                    // Fixed aperture for Mode 2 fractures striking perpendicular to hmax in the uniform aperture case (m)
                    double Mode2HMax_UniformAperture = 0;
                    if (!double.IsNaN(arguments.Argument_HMax_UniformAperture))
                        Mode2HMax_UniformAperture = arguments.Argument_HMax_UniformAperture;
                    // Fracture aperture control parameters: SizeDependent fracture aperture
                    // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                    double Mode1HMin_SizeDependentApertureMultiplier = 0;
                    if (!double.IsNaN(arguments.Argument_HMin_SizeDependentApertureMultiplier))
                        Mode1HMin_SizeDependentApertureMultiplier = arguments.Argument_HMin_SizeDependentApertureMultiplier;
                    // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                    double Mode2HMin_SizeDependentApertureMultiplier = 0;
                    if (!double.IsNaN(arguments.Argument_HMin_SizeDependentApertureMultiplier))
                        Mode2HMin_SizeDependentApertureMultiplier = arguments.Argument_HMin_SizeDependentApertureMultiplier;
                    // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                    double Mode1HMax_SizeDependentApertureMultiplier = 0;
                    if (!double.IsNaN(arguments.Argument_HMax_SizeDependentApertureMultiplier))
                        Mode1HMax_SizeDependentApertureMultiplier = arguments.Argument_HMax_SizeDependentApertureMultiplier;
                    // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                    double Mode2HMax_SizeDependentApertureMultiplier = 0;
                    if (!double.IsNaN(arguments.Argument_HMax_SizeDependentApertureMultiplier))
                        Mode2HMax_SizeDependentApertureMultiplier = arguments.Argument_HMax_SizeDependentApertureMultiplier;
                    // Fracture aperture control parameters: Dynamic fracture aperture
                    // Multiplier for dynamic aperture
                    double DynamicApertureMultiplier = 0;
                    if (!double.IsNaN(arguments.Argument_DynamicApertureMultiplier))
                        DynamicApertureMultiplier = arguments.Argument_DynamicApertureMultiplier;
                    // Fracture aperture control parameters: Barton-Bandis model for fracture aperture
                    // Joint Roughness Coefficient
                    double JRC = 10;
                    if (!double.IsNaN(arguments.Argument_JRC))
                        JRC = arguments.Argument_JRC;
                    // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock
                    double UCSRatio = 2;
                    if (!double.IsNaN(arguments.Argument_UCSratio))
                        UCSRatio = arguments.Argument_UCSratio;
                    // Initial normal stress on fracture
                    double InitialNormalStress = 200000;
                    if (!double.IsNaN(arguments.Argument_InitialNormalStress))
                        InitialNormalStress = arguments.Argument_InitialNormalStress;
                    // Stiffness normal to the fracture, at initial normal stress
                    double FractureNormalStiffness = 2.5E+9;
                    if (!double.IsNaN(arguments.Argument_FractureNormalStiffness))
                        FractureNormalStiffness = arguments.Argument_FractureNormalStiffness;
                    // Maximum fracture closure (m)
                    double MaximumClosure = 0.0005;
                    if (!double.IsNaN(arguments.Argument_MaximumClosure))
                        MaximumClosure = arguments.Argument_MaximumClosure;

                    // Calculation control parameters
                    // Number of fracture sets
                    // Set to 1 to generate a single fracture set, perpendicular to ehmin
                    // Set to 2 to generate two orthogonal fracture sets, perpendicular to ehmin and ehmax; this is typical of a single stage of deformation in intact rock
                    // Set to 6 or more to generate oblique fractures; this is typical of multiple stages of deformation with fracture reactivation, or transtensional strain
                    // NB if the Include oblique fractures checkbox on the main tab is unchecked, this will override NoFractureSets and set the number of fracture sets to 2
                    int NoFractureSets = 2;
                    if (arguments.Argument_IncludeObliqueFracs && (arguments.Argument_NoFractureSets >= 0))
                        NoFractureSets = arguments.Argument_NoFractureSets;
                    // Fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal
                    bool Mode1Only = false;
                    bool Mode2Only = false;
                    if (arguments.Argument_FractureMode == 1)
                        Mode1Only = true;
                    else if (arguments.Argument_FractureMode == 2)
                        Mode2Only = true;
                    // Flag to check microfractures against stress shadows of all macrofractures, regardless of set
                    // If None, microfractures will only be deactivated if they lie in the stress shadow zone of parallel macrofractures
                    // If All, microfractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular macrofractures, depending on the strain tensor
                    // If Automatic, microfractures in the stress shadow zone of oblique or perpendicular macrofractures will be deactivated only if there are more than two fracture sets
                    // NB if the Include oblique fractures checkbox on the main tab is unchecked, this will override CheckAlluFStressShadows and set the flag to None
                    AutomaticFlag CheckAlluFStressShadows = AutomaticFlag.None;
                    if (arguments.Argument_IncludeObliqueFracs && arguments.Argument_CheckAlluFStressShadows)
                        CheckAlluFStressShadows = AutomaticFlag.All;
                    // Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes
                    double AnisotropyCutoff = arguments.Argument_AnisotropyCutoff;
                    // Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow
                    bool AllowReverseFractures = arguments.Argument_AllowReverseFractures;
                    // Horizontal upscaling factor - used to amalgamate multiple Petrel grid cells into one fracture gridblock
                    int HorizontalUpscalingFactor = 1;
                    if (arguments.Argument_HorizontalUpscalingFactor > 0)
                        HorizontalUpscalingFactor = arguments.Argument_HorizontalUpscalingFactor;
                    // Maximum duration for individual timesteps; set to -1 for no maximum timestep duration
                    double MaxTimestepDuration = -1;
                    if (!double.IsNaN(arguments.Argument_MaxTSDuration))
                        MaxTimestepDuration = arguments.Argument_MaxTSDuration;
                    // Maximum increase in MFP33 allowed in each timestep - controls the optimal timestep duration
                    // Increase this to run calculation faster, with fewer but longer timesteps
                    double MaxTimestepMFP33Increase = 0.002;
                    if (!double.IsNaN(arguments.Argument_Max_TS_MFP33_increase))
                        MaxTimestepMFP33Increase = arguments.Argument_Max_TS_MFP33_increase;
                    // Minimum radius for microfractures to be included in implicit fracture density and porosity calculations
                    // If this is set to 0 (i.e. include all microfractures) then it will not be possible to calculate volumetric microfracture density as this will be infinite
                    // If this is set to -1 the maximum radius of the smallest bin will be used (i.e. exclude the smallest bin from the microfracture population)
                    double MinImplicitMicrofractureRadius = -1;
                    if (!double.IsNaN(arguments.Argument_MinimumImplicitMicrofractureRadius))
                        MinImplicitMicrofractureRadius = arguments.Argument_MinimumImplicitMicrofractureRadius;
                    // Number of bins used in numerical integration of uFP32
                    // This controls accuracy of numerical calculation of microfracture populations - increase this to increase accuracy of the numerical integration at expense of runtime 
                    int No_r_bins = 10;
                    if (arguments.Argument_No_r_bins > 0)
                        No_r_bins = arguments.Argument_No_r_bins;
                    // Implicit fracture population distribution functions are not calculated in the Petrel implementation of DFN Generator
                    bool CalculatePopulationDistribution = false;
                    // Number of macrofracture length values to calculate for each of the implicit fracture population distribution functions
                    int No_l_indexPoints = 20;
                    // MaxHMinLength and MaxHMaxLength control the range of fracture lengths in the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
                    // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known (0 will default to maximum potential length - but may be much greater than actual maximum length)
                    double MaxHMinLength = 0;
                    double MaxHMaxLength = 0;
                    // Minimum macrofracture length cutoff is not yet implemented - keep this at 0
                    double MinMacrofractureLength = 0;
                    // Calculation termination controls
                    // The calculation is set to stop automatically when fractures stop growing
                    // This can be defined in one of three ways:
                    //      - When the total volumetric ratio of active (propagating) half-macrofractures (a_MFP33) drops below a specified proportion of the peak historic value
                    //      - When the total volumetric density of active (propagating) half-macrofractures (a_MFP30) drops below a specified proportion of the total (propagating and non-propagating) volumetric density (MFP30)
                    //      - When the total clear zone volume (the volume in which fractures can nucleate without falling within or overlapping a stress shadow) drops below a specified proportion of the total volume
                    // Increase these cutoffs to reduce the sensitivity and stop the calculation earlier
                    // Use this to prevent a long calculation tail - i.e. late timesteps where fractures have stopped growing so they have no impact on fracture populations, just increase runtime
                    // To stop calculation while fractures are still growing reduce the DeformationStageDuration_in or maxTimesteps_in limits
                    // Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control
                    double Current_HistoricMFP33TerminationRatio = -1;
                    if (!double.IsNaN(arguments.Argument_Historic_MFP33_TerminationRatio))
                        Current_HistoricMFP33TerminationRatio = arguments.Argument_Historic_MFP33_TerminationRatio;
                    // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
                    double Active_TotalMFP30TerminationRatio = -1;
                    if (!double.IsNaN(arguments.Argument_Active_MFP30_TerminationRatio))
                        Active_TotalMFP30TerminationRatio = arguments.Argument_Active_MFP30_TerminationRatio;
                    // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
                    double MinimumClearZoneVolume = 0;
                    if (!double.IsNaN(arguments.Argument_Minimum_ClearZone_Volume))
                        MinimumClearZoneVolume = arguments.Argument_Minimum_ClearZone_Volume;
                    // Use the deformation stage duration and maximum timestep limits to stop the calculation before fractures have finished growing
                    // Set DeformationStageDuration to -1 to continue until fracture saturation is reached
                    double DeformationStageDuration = -1;
                    if (!double.IsNaN(arguments.Argument_DeformationDuration))
                        DeformationStageDuration = arguments.Argument_DeformationDuration;
                    int MaxTimesteps = 1;
                    if (arguments.Argument_MaxNoTimesteps > 0)
                        MaxTimesteps = arguments.Argument_MaxNoTimesteps;
                    // DFN geometry controls
                    // Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated
                    bool GenerateExplicitDFN = arguments.Argument_GenerateExplicitDFN;
                    // Set false to allow fractures to propagate outside of the outer grid boundary
                    bool CropAtBoundary = arguments.Argument_CropAtGridBoundary;
                    // Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment
                    bool LinkStressShadows = arguments.Argument_LinkParallelFractures;
                    // Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
                    // Set to Pi/4 rad (45 degrees) by default; NB value should be returned in radians as conversion should be carried out by the Petrel unit conversion functionality
                    double MaxConsistencyAngle = Math.PI / 4;
                    if (!double.IsNaN(arguments.Argument_MaxConsistencyAngle))
                        MaxConsistencyAngle = arguments.Argument_MaxConsistencyAngle;
                    // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value
                    // Set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
                    double MinimumLayerThickness = 0;
                    if (!double.IsNaN(arguments.Argument_MinimumLayerThickness))
                        MinimumLayerThickness = arguments.Argument_MinimumLayerThickness;
                    // Flag to create triangular instead of quadrilateral macrofracture segments; will increase the total number of segments but generation algorithm may run faster
                    // If set to true, microfractures will comprise a series of coplanar triangles with vertices at the centre, rather than a single polygon
                    bool CreateTriangularFractureSegments = arguments.Argument_CreateTriangularFractureSegments;
                    // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small
                    // Set to 0 to disable probabilistic fracture nucleation
                    // Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)
                    double ProbabilisticFractureNucleationLimit = -1;
                    if (!double.IsNaN(arguments.Argument_ProbabilisticFractureNucleationLimit))
                        ProbabilisticFractureNucleationLimit = arguments.Argument_ProbabilisticFractureNucleationLimit;
                    // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
                    // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
                    bool PropagateFracturesInNucleationOrder = arguments.Argument_PropagateFracturesInNucleationOrder;
                    // Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
                    AutomaticFlag SearchAdjacentGridblocks = (AutomaticFlag)arguments.Argument_SearchAdjacentGridblocks;
                    // Minimum radius for microfractures to be included in explicit DFN
                    // Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN
                    double MinExplicitMicrofractureRadius = 0;
                    if (!double.IsNaN(arguments.Argument_MinimumExplicitMicrofractureRadius))
                        MinExplicitMicrofractureRadius = arguments.Argument_MinimumExplicitMicrofractureRadius;
                    // Number of cornerpoints defining the microfracture polygons in the explicit DFN
                    // Minimum is 3 - it is not possible to generate a polygon with less than 3 points
                    int Number_uF_Points = 3;
                    if (arguments.Argument_NoMicrofractureCornerpoints > 3)
                        Number_uF_Points = arguments.Argument_NoMicrofractureCornerpoints;

                    // Flag to assign discrete fractures to sets based on azimuth
                    // Currently set to false as set assignment adds a high overhead in computational cost; this could be allowed as an option in future
                    bool assignOrientationSets = true;// false;

                    // Create Petrel unit converters to convert geological time and other properties from SI to project units, and strings for the unit labels
                    // Geological time units
                    Template GeologicalTimeTemplate = PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale;
                    IUnitConverter toGeologicalTimeUnits = PetrelUnitSystem.GetConverterToUI(GeologicalTimeTemplate);
                    string ProjectTimeUnits = PetrelUnitSystem.GetDisplayUnit(GeologicalTimeTemplate).Symbol;
                    // Strain rate units
                    IUnitConverter toProjectStrainRateUnits = PetrelUnitSystem.GetConverterFromUI(GeologicalTimeTemplate);
                    string ProjectStrainRateUnits = "/" + PetrelUnitSystem.GetDisplayUnit(GeologicalTimeTemplate).Symbol;
                    // Azimuth
                    Template AzimuthTemplate = PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth;
                    IUnitConverter toProjectAzimuthUnits = PetrelUnitSystem.GetConverterToUI(AzimuthTemplate);
                    string AzimuthUnits = PetrelUnitSystem.GetDisplayUnit(AzimuthTemplate).Symbol;
                    // Young's Modulus
                    Template YoungsModTemplate = PetrelProject.WellKnownTemplates.GeomechanicGroup.YoungsModulus;
                    IUnitConverter toProjectYoungsModUnits = PetrelUnitSystem.GetConverterToUI(YoungsModTemplate);
                    string YoungsModUnits = PetrelUnitSystem.GetDisplayUnit(YoungsModTemplate).Symbol;
                    // Crack Surface Energy
                    Template CrackSurfaceEnergyTemplate = PetrelProject.WellKnownTemplates.PetrophysicalGroup.SurfaceTension;
                    IUnitConverter toProjectCrackSurfaceEnergyUnits = PetrelUnitSystem.GetConverterToUI(CrackSurfaceEnergyTemplate);
                    string CrackSurfaceEnergyUnits = PetrelUnitSystem.GetDisplayUnit(CrackSurfaceEnergyTemplate).Symbol;
                    // Propagation rate
                    Template PropagationRateTemplate = PetrelProject.WellKnownTemplates.GeophysicalGroup.Velocity;
                    IUnitConverter toProjectPropagationRateUnits = PetrelUnitSystem.GetConverterToUI(PropagationRateTemplate);
                    string PropagationRateUnits = PetrelUnitSystem.GetDisplayUnit(PropagationRateTemplate).Symbol;
                    // Depth
                    Template DepthTemplate = PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth;
                    IUnitConverter toProjectDepthUnits = PetrelUnitSystem.GetConverterToUI(DepthTemplate);
                    string DepthUnits = PetrelUnitSystem.GetDisplayUnit(DepthTemplate).Symbol;
                    // Rock density
                    Template RockDensityTemplate = PetrelProject.WellKnownTemplates.GeophysicalGroup.RockDensity;
                    IUnitConverter toProjectRockDensityUnits = PetrelUnitSystem.GetConverterToUI(RockDensityTemplate);
                    string RockDensityUnits = PetrelUnitSystem.GetDisplayUnit(RockDensityTemplate).Symbol;
                    // Fluid density
                    Template FluidDensityTemplate = PetrelProject.WellKnownTemplates.GeophysicalGroup.LiquidDensity;
                    IUnitConverter toProjectFluidDensityUnits = PetrelUnitSystem.GetConverterToUI(FluidDensityTemplate);
                    string FluidDensityUnits = PetrelUnitSystem.GetDisplayUnit(FluidDensityTemplate).Symbol;
                    // Fluid pressure
                    Template PressureTemplate = PetrelProject.WellKnownTemplates.PetrophysicalGroup.Pressure;
                    IUnitConverter toProjectPressureUnits = PetrelUnitSystem.GetConverterToUI(PressureTemplate);
                    string PressureUnits = PetrelUnitSystem.GetDisplayUnit(PressureTemplate).Symbol;
                    // Fracture aperture
                    Template FractureApertureTemplate = PetrelProject.WellKnownTemplates.FracturePropertyGroup.FractureAperture;
                    IUnitConverter toProjectFractureApertureUnits = PetrelUnitSystem.GetConverterToUI(FractureApertureTemplate);
                    string FractureApertureUnits = PetrelUnitSystem.GetDisplayUnit(FractureApertureTemplate).Symbol;
                    // Stress
                    Template StressTemplate = PetrelProject.WellKnownTemplates.GeomechanicGroup.StressEffective;
                    IUnitConverter toProjectStressUnits = PetrelUnitSystem.GetConverterToUI(StressTemplate);
                    string StressUnits = PetrelUnitSystem.GetDisplayUnit(StressTemplate).Symbol;
                    // Fracture stiffness
                    Template FractureStiffnessTemplate = PetrelProject.WellKnownTemplates.LogTypesGroup.PressureGradient;
                    IUnitConverter toProjectFractureStiffnessUnits = PetrelUnitSystem.GetConverterToUI(FractureStiffnessTemplate);
                    string FractureStiffnessUnits = PetrelUnitSystem.GetDisplayUnit(FractureStiffnessTemplate).Symbol;
                    // Layer thickness
                    Template LayerThicknessTemplate = PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth;
                    IUnitConverter toProjectLayerThicknessUnits = PetrelUnitSystem.GetConverterToUI(LayerThicknessTemplate);
                    string LayerThicknessUnits = PetrelUnitSystem.GetDisplayUnit(LayerThicknessTemplate).Symbol;
                    // Fracture radius
                    Template FractureRadiusTemplate = PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth;
                    IUnitConverter toProjectFractureRadiusUnits = PetrelUnitSystem.GetConverterToUI(FractureRadiusTemplate);
                    string FractureRadiusUnits = PetrelUnitSystem.GetDisplayUnit(FractureRadiusTemplate).Symbol;
                    // Get length unit conversion factor for calculating the initial microfracture density coefficient A
                    // Note that the conversion factor for A itself will need to be calculated separately for each gridblock since it is dependent on the initial microfracture size distribution coefficient c
                    // For the parameters written to the comments section, we can determine units based on the default value of c
                    double toSIUnits_Length = PetrelUnitSystem.ConvertFromUI(FractureRadiusTemplate, 1);
                    bool lengthUnitMetres = (toSIUnits_Length == 1);
                    string AUnit = "";
                    if (InitialMicrofractureSizeDistribution < 3)
                        AUnit = string.Format("fracs/{0}^{1}", FractureRadiusUnits, 3 - InitialMicrofractureSizeDistribution);
                    else if (InitialMicrofractureSizeDistribution > 3)
                        AUnit = string.Format("frac.{0}^{1}", FractureRadiusUnits, InitialMicrofractureSizeDistribution - 3);
                    else
                        AUnit = "fracs";

                    // NB if certain properties are supplied with a General template, we will carry out unit conversion as if they were supplied in project units
                    // Therefore we need to create Petrel unit converters from project to SI units, and flags to indicate if this conversion is required
                    Template GeneralTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.General;
                    // Geological time units
                    IUnitConverter toSITimeUnits = PetrelUnitSystem.GetConverterFromUI(GeologicalTimeTemplate);
                    bool convertFromGeneral_RockStrainRelaxation = (UseGridFor_RockStrainRelaxation ? RockStrainRelaxation_grid.Template.Equals(GeneralTemplate) : false);
                    bool convertFromGeneral_FractureRelaxation = (UseGridFor_FractureRelaxation ? FractureRelaxation_grid.Template.Equals(GeneralTemplate) : false);
                    // Azimuth
                    IUnitConverter toSIAzimuthUnits = PetrelUnitSystem.GetConverterFromUI(AzimuthTemplate);
                    bool convertFromGeneral_EhminAzi = (UseGridFor_EhminAzi ? EhminAzi_grid.Template.Equals(GeneralTemplate) : false);
                    // Young's Modulus
                    IUnitConverter toSIYoungsModUnits = PetrelUnitSystem.GetConverterFromUI(YoungsModTemplate);
                    bool convertFromGeneral_YoungsMod = (UseGridFor_YoungsMod ? YoungsMod_grid.Template.Equals(GeneralTemplate) : false);
                    // Crack surface energy
                    IUnitConverter toSICrackSurfaceEnergyUnits = PetrelUnitSystem.GetConverterFromUI(CrackSurfaceEnergyTemplate);
                    bool convertFromGeneral_CrackSurfaceEnergy = (UseGridFor_CrackSurfaceEnergy ? CrackSurfaceEnergy_grid.Template.Equals(GeneralTemplate) : false);
                    // Strain rates must be converted from geological time units to SI time units manually, as there is no Petrel template for strain rate
                    IUnitConverter toSIUnits_StrainRate = PetrelUnitSystem.GetConverterToUI(GeologicalTimeTemplate);
                    double EhminRate_SIUnits = toSIUnits_StrainRate.Convert(EhminRate_GeologicalTimeUnits);
                    double EhmaxRate_SIUnits = toSIUnits_StrainRate.Convert(EhmaxRate_GeologicalTimeUnits);
                    // If the friction grid property is supplied as a friction angle, this will be converted to a friction coefficient
                    bool convertFromFrictionAngle_FrictionCoefficient = (UseGridFor_FrictionCoefficient ? FrictionCoefficient_grid.Template.Equals(PetrelProject.WellKnownTemplates.GeomechanicGroup.FrictionAngle) : false);

                    // Get path for output files
                    string folderPath = "";
                    if (WriteToProjectFolder)
                    {
                        IProjectInfo pi = PetrelProject.GetProjectInfo(DataManager.DataSourceManager);
                        folderPath = pi.ProjectStorageDirectory.Parent.FullName;
                    }
                    else
                    {
                        var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
                        if (homeDrive != null)
                        {
                            var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
                            if (homePath != null)
                            {
                                string fullHomePath = homeDrive + Path.DirectorySeparatorChar + homePath;
                                folderPath = Path.Combine(fullHomePath, "DFNFolder");
                                folderPath = folderPath + @"\";
                                // If the output folder does not exist, create it
                                if (!Directory.Exists(folderPath))
                                    Directory.CreateDirectory(folderPath);
                            }
                        }
                    }
                    if (WriteImplicitDataFiles || WriteDFNFiles)
                    {
                        // Check that the folder for the output files exists; if not then do not write them
                        if (!Directory.Exists(folderPath))
                        {
                            PetrelLogger.InfoOutputWindow("Cannot find project folder to write output files to; no output files will be written");
                            WriteImplicitDataFiles = false;
                            WriteDFNFiles = false;
                        }
                        else
                        {
                            PetrelLogger.InfoOutputWindow("Output files will be written to " + folderPath);
                        }
                    }

                    // Write input parameters to comments section
                    // Create string to write input parameters to
                    string generalInputParams = string.Format("Generated by {0}\n\nInput parameters:\n", FractureGrid.VersionNumber);
                    string implicitInputParams = "";
                    string explicitInputParams = "";

                    // Start and end cells, grid size and layers
                    generalInputParams += string.Format("Grid {0}\n", PetrelGrid.Name);
                    generalInputParams += string.Format("Columns {0}-{1}, rows {2}-{3}\n", PetrelGrid_StartCellI + 1, PetrelGrid_StartCellI + NoPetrelGridCols, maxJ - PetrelGrid_StartCellJ - NoPetrelGridRows + 2, maxJ - PetrelGrid_StartCellJ + 1);
                    generalInputParams += string.Format("Layers {0}-{1}\n", PetrelGrid_TopCellK + 1, PetrelGrid_BaseCellK + 1);

                    // Strain orientation and rate
                    if (EhminAzi_grid != null)
                        generalInputParams += string.Format("Minimum strain orientation: {0}, default {1}{2}\n", EhminAzi_grid.Name, toProjectAzimuthUnits.Convert(arguments.Argument_EhminAzi_default), AzimuthUnits);
                    else
                        generalInputParams += string.Format("Minimum strain orientation: {0}{1}\n", toProjectAzimuthUnits.Convert(arguments.Argument_EhminAzi_default), AzimuthUnits);
                    if (UseGridFor_EhminRate)
                        generalInputParams += string.Format("Minimum strain rate: {0}, default {1}{2}\n", EhminRate_grid.Name, EhminRate_GeologicalTimeUnits, ProjectStrainRateUnits);
                    else
                        generalInputParams += string.Format("Minimum strain rate: {0}{1}\n", EhminRate_GeologicalTimeUnits, ProjectStrainRateUnits);
                    if (UseGridFor_EhmaxRate)
                        generalInputParams += string.Format("Maximum strain rate: {0}, default {1}{2}\n", EhmaxRate_grid.Name, EhmaxRate_GeologicalTimeUnits, ProjectStrainRateUnits);
                    else
                        generalInputParams += string.Format("Maximum strain rate: {0}{1}\n", EhmaxRate_GeologicalTimeUnits, ProjectStrainRateUnits);
                    if (AverageStressStrainData)
                        generalInputParams += "Strain input parameters averaged across all cells\n";
                    else
                        generalInputParams += "Strain data taken from top middle cell in each stack\n";

                    // Intermediate outputs
                    if (NoIntermediateOutputs > 0)
                    {
                        generalInputParams += string.Format("Output {0} intermediate DFNs ", NoIntermediateOutputs);
                        if (OutputAtEqualTimeIntervals)
                            generalInputParams += "at equal time intervals\n";
                        else
                            generalInputParams += "at approximately equal stages of fracture growth\n";
                    }

                    // Mechanical properties
                    if (UseGridFor_YoungsMod)
                        generalInputParams += string.Format("Young's Modulus: {0}, default {1}{2}\n", YoungsMod_grid.Name, toProjectYoungsModUnits.Convert(YoungsMod), YoungsModUnits);
                    else
                        generalInputParams += string.Format("Young's Modulus: {0}{1}\n", toProjectYoungsModUnits.Convert(YoungsMod), YoungsModUnits);
                    if (UseGridFor_PoissonsRatio)
                        generalInputParams += string.Format("Poisson's ratio: {0}, default {1}\n", PoissonsRatio_grid.Name, PoissonsRatio);
                    else
                        generalInputParams += string.Format("Poisson's ratio: {0}\n", PoissonsRatio);
                    if (UseGridFor_BiotCoefficient)
                        generalInputParams += string.Format("Biot's coefficient: {0}, default {1}\n", BiotCoefficient_grid.Name, BiotCoefficient);
                    else
                        generalInputParams += string.Format("Biot's coefficient: {0}\n", BiotCoefficient);
                    if (UseGridFor_FrictionCoefficient)
                        generalInputParams += string.Format("Friction coefficient: {0}, default {1}\n", FrictionCoefficient_grid.Name, FrictionCoefficient);
                    else
                        generalInputParams += string.Format("Friction coefficient: {0}\n", FrictionCoefficient);
                    if (UseGridFor_CrackSurfaceEnergy)
                        generalInputParams += string.Format("Crack surface energy: {0}, default {1}{2}\n", CrackSurfaceEnergy_grid.Name, toProjectCrackSurfaceEnergyUnits.Convert(CrackSurfaceEnergy), CrackSurfaceEnergyUnits);
                    else
                        generalInputParams += string.Format("Crack surface energy: {0}{1}\n", toProjectCrackSurfaceEnergyUnits.Convert(CrackSurfaceEnergy), CrackSurfaceEnergyUnits);

                    // Strain relaxation
                    if (RockStrainRelaxation > 0)
                    {
                        if (UseGridFor_RockStrainRelaxation)
                            generalInputParams += string.Format("Strain relaxation time constant for rock matrix: {0}, default {1}{2}\n", RockStrainRelaxation_grid.Name, RockStrainRelaxation, ProjectTimeUnits);
                        else
                            generalInputParams += string.Format("Strain relaxation time constant for rock matrix: {0}{1}\n", RockStrainRelaxation, ProjectTimeUnits);
                    }
                    else if (FractureRelaxation > 0)
                    {
                        if (UseGridFor_FractureRelaxation)
                            generalInputParams += string.Format("Strain relaxation time constant for fractures: {0}, default {1}{2}\n", FractureRelaxation_grid.Name, FractureRelaxation, ProjectTimeUnits);
                        else
                            generalInputParams += string.Format("Strain relaxation time constant for fractures: {0}{1}\n", FractureRelaxation, ProjectTimeUnits);
                    }
                    else
                        generalInputParams += "No strain relaxation applied\n";
                    // Initial microfracture density
                    if (UseGridFor_InitialMicrofractureDensity)
                        generalInputParams += string.Format("Initial fracture density: {0}, default {1}{2}; ", InitialMicrofractureDensity_grid.Name, InitialMicrofractureDensity, AUnit);
                    else
                        generalInputParams += string.Format("Initial fracture density: {0}{1}; ", InitialMicrofractureDensity, AUnit);
                    if (UseGridFor_InitialMicrofractureSizeDistribution)
                        generalInputParams += string.Format("exponent {0}, default {1}\n ", InitialMicrofractureSizeDistribution_grid.Name, InitialMicrofractureSizeDistribution);
                    else
                        generalInputParams += string.Format("exponent {0}\n", InitialMicrofractureSizeDistribution);

                    if (UseGridFor_SubcriticalPropIndex)
                        generalInputParams += string.Format("Subcritical propagation index: {0}, default {1}\n", SubcriticalPropIndex_grid.Name, SubcriticalPropIndex);
                    else
                        generalInputParams += string.Format("Subcritical propagation index: {0}\n", SubcriticalPropIndex);
                    generalInputParams += string.Format("Critical propagation rate: {0}{1}\n", toProjectPropagationRateUnits.Convert(CriticalPropagationRate), PropagationRateUnits);
                    if (AverageMechanicalPropertyData)
                        generalInputParams += "Mechanical property input parameters averaged across all cells\n";
                    else
                        generalInputParams += "Mechanical property data taken from top middle cell in each stack\n";

                    // Stress state
                    generalInputParams += string.Format("Stress distribution: {0}\n", StressDistributionScenario);
                    if (OverwriteDepth)
                        generalInputParams += string.Format("Depth of deformation: {0}{1}\n", toProjectDepthUnits.Convert(DepthAtDeformation), DepthUnits);
                    else
                        generalInputParams += "Depth of deformation: current depth\n";
                    generalInputParams += string.Format("Mean overlying sediment density: {0}{1}\n", toProjectRockDensityUnits.Convert(MeanOverlyingSedimentDensity), RockDensityUnits);
                    generalInputParams += string.Format("Fluid density: {0}{1}\n", toProjectFluidDensityUnits.Convert(FluidDensity), FluidDensityUnits);
                    generalInputParams += string.Format("Initial fluid overpressure: {0}{1}\n", toProjectPressureUnits.Convert(InitialOverpressure), PressureUnits);
                    generalInputParams += string.Format("Initial stress relaxation factor: {0}\n", InitialStressRelaxation);

                    // Fracture aperture
                    if (CalculateFracturePorosity)
                    {
                        switch (FractureApertureControl)
                        {
                            case FractureApertureType.Uniform:
                                generalInputParams += string.Format("Uniform fracture aperture: HMin Mode 1 {0}{4}; HMin Mode 2 {1}{4}; HMax Mode 1 {2}{4}; HMax Mode 2 {3}{4}\n", toProjectFractureApertureUnits.Convert(Mode1HMin_UniformAperture), toProjectFractureApertureUnits.Convert(Mode2HMin_UniformAperture), toProjectFractureApertureUnits.Convert(Mode1HMax_UniformAperture), toProjectFractureApertureUnits.Convert(Mode2HMax_UniformAperture), FractureApertureUnits);
                                break;
                            case FractureApertureType.SizeDependent:
                                generalInputParams += string.Format("Size dependent aperture: HMin Mode 1 x{0}; HMin Mode 2 x{1}; HMax Mode 1 x{2}; HMax Mode 2 x{3}\n", Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier);
                                break;
                            case FractureApertureType.Dynamic:
                                generalInputParams += string.Format("Dynamic aperture: x{0}\n", DynamicApertureMultiplier);
                                break;
                            case FractureApertureType.BartonBandis:
                                generalInputParams += string.Format("Barton-Bandis aperture: JRC {0}; UCS ratio {1}; Initial normal stress {2}{5}; Initial stiffness {3}{6} Max closure {4}{7}\n", JRC, UCSRatio, toProjectStressUnits.Convert(InitialNormalStress), toProjectFractureStiffnessUnits.Convert(FractureNormalStiffness), toProjectFractureApertureUnits.Convert(MaximumClosure), StressUnits, FractureStiffnessUnits, FractureApertureUnits);
                                break;
                            default:
                                break;
                        }
                    }

                    // Calculation control parameters
                    // Fracture mode
                    if (Mode1Only)
                        generalInputParams += "Mode 1 fractures only\n";
                    else if (Mode2Only)
                        generalInputParams += "Mode 2 fractures only\n";
                    else
                        generalInputParams += "Optimal fracture mode\n";
                    if (CheckAlluFStressShadows == AutomaticFlag.All)
                        generalInputParams += "Include stress shadow interaction between different fracture sets\n";
                    generalInputParams += string.Format("Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes: {0}\n", AnisotropyCutoff);
                    if (AllowReverseFractures)
                        generalInputParams += "Include reverse fractures\n";
                    else
                        generalInputParams += "Do not include reverse fractures\n";
                    if (HorizontalUpscalingFactor > 1)
                        generalInputParams += string.Format("Horizontal upscaling factor: {0}\n", HorizontalUpscalingFactor);
                    if (MaxTimestepDuration > 0)
                        generalInputParams += string.Format("Maximum duration for individual timesteps: {0}{1}\n", MaxTimestepDuration, ProjectTimeUnits);
                    generalInputParams += string.Format("Maximum MFP33 increase per timestep (controls accuracy of calculation): {0}\n", MaxTimestepMFP33Increase);
                    generalInputParams += string.Format("Minimum microfracture radius for implicit population data: {0}{1}\n", toProjectFractureRadiusUnits.Convert(MinImplicitMicrofractureRadius), FractureRadiusUnits);
                    generalInputParams += string.Format("Number of radius bins for numerical calculation of microfracture P32: {0}\n", No_r_bins);
                    // Calculation termination controls
                    generalInputParams += string.Format("Maximum duration of deformation: {0}{1}\n", DeformationStageDuration, ProjectTimeUnits);
                    generalInputParams += string.Format("Calculation termination control: Max timesteps {0}; Min clear zone volume {1}", MaxTimesteps, MinimumClearZoneVolume);
                    if (Current_HistoricMFP33TerminationRatio > 0)
                        generalInputParams += string.Format("; Current:Peak active MFP33 ratio {0}", Current_HistoricMFP33TerminationRatio);
                    if (Active_TotalMFP30TerminationRatio > 0)
                        generalInputParams += string.Format("; Current active:total MFP30 ratio {0}", Active_TotalMFP30TerminationRatio);
                    generalInputParams += "\n";

                    // DFN geometry controls
                    if (CropAtBoundary)
                        explicitInputParams += "Crop fractures at boundary to specified subgrid\n";
                    else
                        explicitInputParams += "Fractures can propagate out of specified subgrid\n";
                    if (LinkStressShadows)
                        explicitInputParams += "Link fractures across relay zones\n";
                    else
                        explicitInputParams += "Do not link fractures across relay zones\n";
                    explicitInputParams += string.Format("Maximum bend across cell boundaries (Max Consistency Angle): {0}{1}\n", toProjectAzimuthUnits.Convert(arguments.Argument_MaxConsistencyAngle), AzimuthUnits);
                    explicitInputParams += string.Format("Minimum layer thickness cutoff: {0}{1}\n", toProjectLayerThicknessUnits.Convert(MinimumLayerThickness), LayerThicknessUnits);
                    if (CreateTriangularFractureSegments)
                        explicitInputParams += "Fractures represented by triangular segments\n";
                    if (ProbabilisticFractureNucleationLimit > 0)
                        explicitInputParams += string.Format("Use probabilistic fracture nucleation if fewer than {0} fractures nucleate per timestep\n", ProbabilisticFractureNucleationLimit);
                    if (PropagateFracturesInNucleationOrder)
                        explicitInputParams += "Fractures propagate in order of nucleation\n";
                    switch (SearchAdjacentGridblocks)
                    {
                        case AutomaticFlag.None:
                            explicitInputParams += "Search neighbouring cells for stress shadow interaction: None\n";
                            break;
                        case AutomaticFlag.All:
                            explicitInputParams += "Search neighbouring cells for stress shadow interaction: All\n";
                            break;
                        case AutomaticFlag.Automatic:
                            explicitInputParams += "Search neighbouring cells for stress shadow interaction: Automatic\n";
                            break;
                        default:
                            break;
                    }
                    if (MinExplicitMicrofractureRadius > 0)
                    {
                        explicitInputParams += string.Format("Minimum microfracture radius for explicit DFN: {0}{1}\n", toProjectFractureRadiusUnits.Convert(MinExplicitMicrofractureRadius), FractureRadiusUnits);
                        explicitInputParams += string.Format("Microfractures represented as {0} point polygons\n", Number_uF_Points);
                    }
                    else
                    {
                        explicitInputParams += "Explicit DFN includes macrofractures only\n";
                    }

                    // Define a time unit modifier to convert between input units and SI units, when writing output
                    // Not needed as this can be done using Petrel unit conversion
                    /*double timeUnits_Modifier = 1;
                    switch (ModelTimeUnits)
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
                    }*/

                    // Get the index numbers of the fracture sets perpendicular to hmin and hmax
                    // Note that if the number of fracture sets is odd, there will be no set directly perpendicular to hmax, so we will take the index number of the closest set
                    int hmin_index = 0;
                    int hmax_index = NoFractureSets / 2;

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
#if DEBUG_FRACS
                                PetrelLogger.InfoOutputWindow("");
                                PetrelLogger.InfoOutputWindow(string.Format("ColNo {0}, RowNo {1}", FractureGrid_ColNo, FractureGrid_RowNo));
#endif

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
                                double local_Depth = 0;
                                double local_LayerThickness = 0;

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
                                PointXYZ FractureGrid_SWtop = new PointXYZ(SW_top_corner.X, SW_top_corner.Y, -SW_top_corner.Z);
                                PointXYZ FractureGrid_SWbottom = new PointXYZ(SW_bottom_corner.X, SW_bottom_corner.Y, -SW_bottom_corner.Z);
                                // Update mean depth and thickness variables
                                local_Depth -= SW_top_corner.Z;
                                local_LayerThickness += (SW_top_corner.Z - SW_bottom_corner.Z);

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
                                PointXYZ FractureGrid_NWtop = new PointXYZ(NW_top_corner.X, NW_top_corner.Y, -NW_top_corner.Z);
                                PointXYZ FractureGrid_NWbottom = new PointXYZ(NW_bottom_corner.X, NW_bottom_corner.Y, -NW_bottom_corner.Z);
                                // Update mean depth and thickness variables
                                local_Depth -= NW_top_corner.Z;
                                local_LayerThickness += (NW_top_corner.Z - NW_bottom_corner.Z);

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
                                PointXYZ FractureGrid_NEtop = new PointXYZ(NE_top_corner.X, NE_top_corner.Y, -NE_top_corner.Z);
                                PointXYZ FractureGrid_NEbottom = new PointXYZ(NE_bottom_corner.X, NE_bottom_corner.Y, -NE_bottom_corner.Z);
                                // Update mean depth and thickness variables
                                local_Depth -= NE_top_corner.Z;
                                local_LayerThickness += (NE_top_corner.Z - NE_bottom_corner.Z);

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
                                PointXYZ FractureGrid_SEtop = new PointXYZ(SE_top_corner.X, SE_top_corner.Y, -SE_top_corner.Z);
                                PointXYZ FractureGrid_SEbottom = new PointXYZ(SE_bottom_corner.X, SE_bottom_corner.Y, -SE_bottom_corner.Z);
                                // Update mean depth and thickness variables
                                local_Depth -= SE_top_corner.Z;
                                local_LayerThickness += (SE_top_corner.Z - SE_bottom_corner.Z);

                                // Calculate the mean depth and layer thickness
                                local_Depth = (OverwriteDepth ? DepthAtDeformation : local_Depth / 4);
                                local_LayerThickness = local_LayerThickness / 4;

                                // If either the mean depth or the layer thickness are undefined, then one or more of the corners lies outside the grid
                                // In this case we will abort this gridblock and move onto the next
                                if (double.IsNaN(local_Depth) || double.IsNaN(local_LayerThickness))
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
                                double local_Epsilon_hmin_azimuth_in = EhminAzi;
                                double local_Epsilon_hmin_rate_in = EhminRate_SIUnits;
                                double local_Epsilon_hmax_rate_in = EhmaxRate_SIUnits;

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
                                                if (UseGridFor_EhminAzi)
                                                {
                                                    double cell_ehmin_orient = (double)EhminAzi_grid[cellRef];
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    if (convertFromGeneral_EhminAzi)
                                                        cell_ehmin_orient = toSIAzimuthUnits.Convert(cell_ehmin_orient);
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
                                                if (UseGridFor_EhminRate)
                                                {
                                                    // Unit conversion for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
                                                    double cell_ehmin_rate = toSIUnits_StrainRate.Convert((double)EhminRate_grid[cellRef]);
                                                    if (!double.IsNaN(cell_ehmin_rate))
                                                    {
                                                        ehmin_rate_total += cell_ehmin_rate;
                                                        ehmin_rate_novalues++;
                                                    }
                                                }

                                                // Update ehmax rate total if defined
                                                if (UseGridFor_EhmaxRate)
                                                {
                                                    // Unit conversion for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
                                                    double cell_ehmax_rate = toSIUnits_StrainRate.Convert((double)EhmaxRate_grid[cellRef]);
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
                                    if (UseGridFor_EhminAzi)
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                        {
                                            cellRef.K = PetrelGrid_DataCellK;
                                            double cell_ehmin_orient = (double)EhminAzi_grid[cellRef];
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_EhminAzi)
                                                cell_ehmin_orient = toSIAzimuthUnits.Convert(cell_ehmin_orient);
                                            if (!double.IsNaN(cell_ehmin_orient))
                                            {
                                                local_Epsilon_hmin_azimuth_in = cell_ehmin_orient;
                                                break;
                                            }
                                        }
                                    }

                                    // Update ehmin rate total if defined
                                    if (UseGridFor_EhminRate)
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                        {
                                            cellRef.K = PetrelGrid_DataCellK;
                                            // Unit conversion for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
                                            double cell_ehmin_rate = toSIUnits_StrainRate.Convert((double)EhminRate_grid[cellRef]);
                                            if (!double.IsNaN(cell_ehmin_rate))
                                            {
                                                local_Epsilon_hmin_rate_in = cell_ehmin_rate;
                                                break;
                                            }
                                        }
                                    }

                                    // Update ehmax rate total if defined
                                    if (UseGridFor_EhmaxRate)
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                        {
                                            cellRef.K = PetrelGrid_DataCellK;
                                            // Unit conversion for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
                                            double cell_ehmax_rate = toSIUnits_StrainRate.Convert((double)EhmaxRate_grid[cellRef]);
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
                                double local_InitialMicrofractureDensity = InitialMicrofractureDensity;
                                double local_InitialMicrofractureSizeDistribution = InitialMicrofractureSizeDistribution;
                                double local_SubcriticalPropIndex = SubcriticalPropIndex;
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
                                    double InitialMicrofractureDensity_total = 0;
                                    int InitialMicrofractureDensity_novalues = 0;
                                    double InitialMicrofractureSizeDistribution_total = 0;
                                    int InitialMicrofractureSizeDistribution_novalues = 0;
                                    double SubcriticalPropIndex_total = 0;
                                    int SubcriticalPropIndex_novalues = 0;
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
                                                if (UseGridFor_InitialMicrofractureDensity)
                                                {
                                                    double cell_CapB = (double)InitialMicrofractureDensity_grid[cellRef];
                                                    if (!double.IsNaN(cell_CapB))
                                                    {
                                                        InitialMicrofractureDensity_total += cell_CapB;
                                                        InitialMicrofractureDensity_novalues++;
                                                    }
                                                }

                                                // Update initial microfracture size distribution total if defined
                                                if (UseGridFor_InitialMicrofractureSizeDistribution)
                                                {
                                                    double cell_smallc = (double)InitialMicrofractureSizeDistribution_grid[cellRef];
                                                    if (!double.IsNaN(cell_smallc))
                                                    {
                                                        InitialMicrofractureSizeDistribution_total += cell_smallc;
                                                        InitialMicrofractureSizeDistribution_novalues++;
                                                    }
                                                }

                                                // Update subcritical propagation index total if defined
                                                if (UseGridFor_SubcriticalPropIndex)
                                                {
                                                    double cell_smallb = (double)SubcriticalPropIndex_grid[cellRef];
                                                    if (!double.IsNaN(cell_smallb))
                                                    {
                                                        SubcriticalPropIndex_total += cell_smallb;
                                                        SubcriticalPropIndex_novalues++;
                                                    }
                                                }

                                                // Update Young's Modulus total if defined
                                                if (UseGridFor_YoungsMod)
                                                {
                                                    double cell_YoungsMod = (double)YoungsMod_grid[cellRef];
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    if (convertFromGeneral_YoungsMod)
                                                        cell_YoungsMod = toSIYoungsModUnits.Convert(cell_YoungsMod);
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
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    if (convertFromGeneral_CrackSurfaceEnergy)
                                                        cell_CrackSurfaceEnergy = toSICrackSurfaceEnergyUnits.Convert(cell_CrackSurfaceEnergy);
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
                                                    // If the property has a FrictionAngle template, convert this to a friction coefficient
                                                    if (convertFromFrictionAngle_FrictionCoefficient)
                                                        cell_FrictionCoeff = Math.Tan(cell_FrictionCoeff);
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
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    if (convertFromGeneral_RockStrainRelaxation)
                                                        cell_RockStrainRelaxation = toSITimeUnits.Convert(cell_RockStrainRelaxation);
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
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    if (convertFromGeneral_FractureRelaxation)
                                                        cell_FractureRelaxation = toSITimeUnits.Convert(cell_FractureRelaxation);
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
                                    if (InitialMicrofractureDensity_novalues > 0)
                                        local_InitialMicrofractureDensity = InitialMicrofractureDensity_total / (double)InitialMicrofractureDensity_novalues;
                                    if (InitialMicrofractureSizeDistribution_novalues > 0)
                                        local_InitialMicrofractureSizeDistribution = InitialMicrofractureSizeDistribution_total / (double)InitialMicrofractureSizeDistribution_novalues;
                                    if (SubcriticalPropIndex_novalues > 0)
                                        local_SubcriticalPropIndex = SubcriticalPropIndex_total / (double)SubcriticalPropIndex_novalues;
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
                                    if (UseGridFor_InitialMicrofractureDensity)
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                        {
                                            cellRef.K = PetrelGrid_DataCellK;
                                            double cell_InitialMicrofractureDensity = (double)InitialMicrofractureDensity_grid[cellRef];
                                            if (!double.IsNaN(cell_InitialMicrofractureDensity))
                                            {
                                                local_InitialMicrofractureDensity = cell_InitialMicrofractureDensity;
                                                break;
                                            }
                                        }
                                    }

                                    // Update initial microfracture size distribution total if defined
                                    if (UseGridFor_InitialMicrofractureSizeDistribution)
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                        {
                                            cellRef.K = PetrelGrid_DataCellK;
                                            double cell_InitialMicrofractureSizeDistribution = (double)InitialMicrofractureSizeDistribution_grid[cellRef];
                                            if (!double.IsNaN(cell_InitialMicrofractureSizeDistribution))
                                            {
                                                local_InitialMicrofractureSizeDistribution = cell_InitialMicrofractureSizeDistribution;
                                                break;
                                            }
                                        }
                                    }

                                    // Update subcritical propagation index total if defined
                                    if (UseGridFor_SubcriticalPropIndex)
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                        {
                                            cellRef.K = PetrelGrid_DataCellK;
                                            double cell_SubcriticalPropIndex = (double)SubcriticalPropIndex_grid[cellRef];
                                            if (!double.IsNaN(cell_SubcriticalPropIndex))
                                            {
                                                local_SubcriticalPropIndex = cell_SubcriticalPropIndex;
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
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_YoungsMod)
                                                cell_YoungsMod = toSIYoungsModUnits.Convert(cell_YoungsMod);
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
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_CrackSurfaceEnergy)
                                                cell_CrackSurfaceEnergy = toSICrackSurfaceEnergyUnits.Convert(cell_CrackSurfaceEnergy);
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
                                            // If the property has a FrictionAngle template, convert this to a friction coefficient
                                            if (convertFromFrictionAngle_FrictionCoefficient)
                                                cell_FrictionCoeff = Math.Tan(cell_FrictionCoeff);
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
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_RockStrainRelaxation)
                                                cell_RockStrainRelaxation = toSITimeUnits.Convert(cell_RockStrainRelaxation);
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
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_FractureRelaxation)
                                                cell_FractureRelaxation = toSITimeUnits.Convert(cell_FractureRelaxation);
                                            if (!double.IsNaN(cell_FractureRelaxation))
                                            {
                                                local_FractureRelaxation = cell_FractureRelaxation;
                                                break;
                                            }
                                        }
                                    }
                                }
                                // Until that point, all data will be kept in project units

                                // InitialMicrofractureDensity A is stored in project units rather than SI units, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
                                // Therefore unit conversion for InitialMicrofractureDensity A must be carried out now
                                // Unit conversion must be done on a cell by cell basis, since the values of InitialMicrofractureSizeDistribution may vary between cells
                                if (!lengthUnitMetres)
                                {
                                    double toSIUnits_InitialMicrofractureDensity = Math.Pow(toSIUnits_Length, local_InitialMicrofractureSizeDistribution - 3);
                                    local_InitialMicrofractureDensity *= toSIUnits_InitialMicrofractureDensity;
                                }

                                // Check the elastic properties for physically unrealistic values, and if so warn the user
                                // NB The code will actually generate a result with any input values except Young's Modulus = 0, Poisson's ratio = -1 or Poisson's ratio = 1
                                // and these values will automatically be corrected by the MechanicalProperties object
                                if (local_YoungsMod <= 0)
                                {
                                    PetrelLogger.InfoOutputWindow(string.Format("Invalid value for Young's Modulus ({0}Pa) in cell {1},{2}. This will create errors in the calculation.", local_YoungsMod, PetrelGrid_FirstCellI + 1, maxJ - PetrelGrid_FirstCellJ + 1));
                                }
                                if ((local_PoissonsRatio < 0) || (local_PoissonsRatio > 0.5))
                                {
                                    PetrelLogger.InfoOutputWindow(string.Format("Invalid value for Poisson's ratio ({0}) in cell {1},{2}. This will create errors in the calculation.", local_PoissonsRatio, PetrelGrid_FirstCellI + 1, maxJ - PetrelGrid_FirstCellJ + 1));
                                }
                                // End get the mechanical properties from the grid as required

                                // Create a new gridblock object containing the required number of fracture sets
                                GridblockConfiguration gc = new GridblockConfiguration(local_LayerThickness, local_Depth, NoFractureSets);

                                // Set the gridblock cornerpoints
                                gc.setGridblockCorners(FractureGrid_SWtop, FractureGrid_SWbottom, FractureGrid_NWtop, FractureGrid_NWbottom, FractureGrid_NEtop, FractureGrid_NEbottom, FractureGrid_SEtop, FractureGrid_SEbottom);

                                // Set the mechanical properties for the gridblock
                                gc.MechProps.setMechanicalProperties(local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_SubcriticalPropIndex, ModelTimeUnits);

                                // Set the fracture aperture control properties
                                gc.MechProps.setFractureApertureControlData(DynamicApertureMultiplier, JRC, UCSRatio, InitialNormalStress, FractureNormalStiffness, MaximumClosure);

                                // Set the initial stress and strain
                                // If the initial stress relaxation value is negative, set it to the required value for a critical initial stress state
                                double local_InitialStressRelaxation = InitialStressRelaxation;
                                if (InitialStressRelaxation < 0)
                                {
                                    double friction_angle = Math.Atan(local_FrictionCoefficient);
                                    double sin_friction_angle = Math.Sin(friction_angle);
                                    double sh0d_svd = (1 - sin_friction_angle) / (1 + sin_friction_angle);
                                    local_InitialStressRelaxation = (((1 - local_PoissonsRatio) * sh0d_svd) - local_PoissonsRatio) / (1 - (2 * local_PoissonsRatio));
                                }
                                double lithostatic_stress = (MeanOverlyingSedimentDensity * 9.81 * local_Depth);
                                double fluid_pressure = (FluidDensity * 9.81 * local_Depth) + InitialOverpressure;
                                gc.StressStrain.SetInitialStressStrainState(lithostatic_stress, fluid_pressure, local_InitialStressRelaxation);

                                // Calculate the minimum microfracture radius from the layer thickness, if required
                                double local_minImplicitMicrofractureRadius = (MinImplicitMicrofractureRadius < 0 ? local_LayerThickness / (double)No_r_bins : MinImplicitMicrofractureRadius);

                                // Determine whether to check for stress shadows from other fracture sets
                                bool local_checkAlluFStressShadows;
                                switch (CheckAlluFStressShadows)
                                {
                                    case AutomaticFlag.None:
                                        local_checkAlluFStressShadows = false;
                                        break;
                                    case AutomaticFlag.All:
                                        local_checkAlluFStressShadows = true;
                                        break;
                                    case AutomaticFlag.Automatic:
                                        local_checkAlluFStressShadows = (NoFractureSets > 2);
                                        break;
                                    default:
                                        local_checkAlluFStressShadows = false;
                                        break;
                                }

                                // Set the propagation control data for the gridblock
                                gc.PropControl.setPropagationControl(CalculatePopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio,
                                    MinimumClearZoneVolume, DeformationStageDuration, MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, local_Epsilon_hmin_azimuth_in, local_Epsilon_hmin_rate_in, local_Epsilon_hmax_rate_in, ModelTimeUnits, CalculateFracturePorosity, FractureApertureControl);

                                // Set folder path for output files
                                gc.PropControl.FolderPath = folderPath;

                                // Create the fracture sets
                                if (Mode1Only)
                                    gc.resetFractures(local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures);
                                else if (Mode2Only)
                                    gc.resetFractures(local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures);
                                else
                                    gc.resetFractures(local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, AllowReverseFractures);

                                // Set the fracture aperture control data for fracture porosity calculation
                                for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
                                {
                                    double Mode1_UniformAperture_in, Mode2_UniformAperture_in, Mode1_SizeDependentApertureMultiplier_in, Mode2_SizeDependentApertureMultiplier_in;
                                    if (fs_index == 0)
                                    {
                                        Mode1_UniformAperture_in = Mode1HMin_UniformAperture;
                                        Mode2_UniformAperture_in = Mode2HMin_UniformAperture;
                                        Mode1_SizeDependentApertureMultiplier_in = Mode1HMin_SizeDependentApertureMultiplier;
                                        Mode2_SizeDependentApertureMultiplier_in = Mode2HMin_SizeDependentApertureMultiplier;
                                    }
                                    else if ((fs_index == (NoFractureSets / 2)) && ((NoFractureSets % 2) == 0))
                                    {
                                        Mode1_UniformAperture_in = Mode1HMax_UniformAperture;
                                        Mode2_UniformAperture_in = Mode2HMax_UniformAperture;
                                        Mode1_SizeDependentApertureMultiplier_in = Mode1HMax_SizeDependentApertureMultiplier;
                                        Mode2_SizeDependentApertureMultiplier_in = Mode2HMax_SizeDependentApertureMultiplier;
                                    }
                                    else
                                    {
                                        double relativeAngle = Math.PI * ((double)fs_index / (double)NoFractureSets);
                                        double HMinComponent = Math.Pow(Math.Cos(relativeAngle), 2);
                                        double HMaxComponent = Math.Pow(Math.Sin(relativeAngle), 2);
                                        Mode1_UniformAperture_in = (Mode1HMin_UniformAperture * HMinComponent) + (Mode1HMax_UniformAperture * HMaxComponent);
                                        Mode2_UniformAperture_in = (Mode2HMin_UniformAperture * HMinComponent) + (Mode2HMax_UniformAperture * HMaxComponent);
                                        Mode1_SizeDependentApertureMultiplier_in = (Mode1HMin_SizeDependentApertureMultiplier * HMinComponent) + (Mode1HMax_SizeDependentApertureMultiplier * HMaxComponent);
                                        Mode2_SizeDependentApertureMultiplier_in = (Mode2HMin_SizeDependentApertureMultiplier * HMinComponent) + (Mode2HMax_SizeDependentApertureMultiplier * HMaxComponent);
                                    }
                                    gc.FractureSets[fs_index].SetFractureApertureControlData(Mode1_UniformAperture_in, Mode2_UniformAperture_in, Mode1_SizeDependentApertureMultiplier_in, Mode2_SizeDependentApertureMultiplier_in);
                                }

                                // Add to grid
                                ModelGrid.AddGridblock(gc, FractureGrid_RowNo, FractureGrid_ColNo, !faultToWest, !faultToSouth, true, true);

#if DEBUG_FRACS
                                foreach (PointXYZ point in new PointXYZ[] { FractureGrid_SWtop, FractureGrid_NWtop, FractureGrid_NEtop, FractureGrid_SEtop, FractureGrid_SWbottom, FractureGrid_NWbottom, FractureGrid_NEbottom, FractureGrid_SEbottom })
                                {
                                    if (minX > point.X) minX = point.X;
                                    if (minY > point.Y) minY = point.Y;
                                    if (minZ > point.Z) minZ = point.Z;
                                    if (maxX < point.X) maxX = point.X;
                                    if (maxY < point.Y) maxY = point.Y;
                                    if (maxZ < point.Z) maxZ = point.Z;
                                }

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
                                PetrelLogger.InfoOutputWindow(string.Format("LayerThickness = {0}; Depth = {1};", local_LayerThickness, local_Depth));
                                PetrelLogger.InfoOutputWindow(string.Format("local_Epsilon_hmin_azimuth_in = {0}; Epsilon_hmin_rate_in = {1}; Epsilon_hmax_rate_in = {2};", local_Epsilon_hmin_azimuth_in, local_Epsilon_hmin_rate_in, local_Epsilon_hmax_rate_in));
                                PetrelLogger.InfoOutputWindow(string.Format("sv' {0}", gc.StressStrain.Sigma_v_eff));
                                PetrelLogger.InfoOutputWindow(string.Format("Young's Mod: {0}, Poisson's ratio: {1}, Biot coefficient {2}, Crack surface energy:{3}, Friction coefficient:{4}", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient));
                                PetrelLogger.InfoOutputWindow(string.Format("gc = new GridblockConfiguration({0}, {1}, {2});", local_LayerThickness, local_Depth, NoFractureSets));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.MechProps.setMechanicalProperties({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, TimeUnits.{9});", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_SubcriticalPropIndex, ModelTimeUnits));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.MechProps.setFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5});", DynamicApertureMultiplier, JRC, UCSRatio, InitialNormalStress, FractureNormalStiffness, MaximumClosure));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.StressStrain.setStressStrainState({0}, {1}, {2});", lithostatic_stress, fluid_pressure, InitialStressRelaxation));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.setPropagationControl({0}, {1}, {2}, {3}, {4}, {5}, StressDistribution.{6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, TimeUnits.{22}, {23}, {24});",
                                    CalculatePopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio,
                                    MinimumClearZoneVolume, DeformationStageDuration, MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, local_Epsilon_hmin_azimuth_in, local_Epsilon_hmin_rate_in, local_Epsilon_hmax_rate_in, ModelTimeUnits, CalculateFracturePorosity, FractureApertureControl));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.resetFractures({0}, {1}, {2});", local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, (Mode1Only ? "Mode1" : (Mode2Only ? "Mode2" : "NoModeSpecified")), AllowReverseFractures));
                                PetrelLogger.InfoOutputWindow(string.Format("ModelGrid.AddGridblock(gc, {0}, {1}, {2}, {3}, {4}, {5});", FractureGrid_RowNo, FractureGrid_ColNo, !faultToWest, !faultToSouth, true, true));
#endif

                                // Update gridblock counter
                                NoActiveGridblocks++;

                                //Update status bar
                                progressBarWrapper.UpdateProgress(NoActiveGridblocks);

                            } // End loop through all rows in the Fracture Grid
                        } // End loop through all columns in the Fracture Grid

                        // Set the DFN generation data
                        DFNGenerationControl dfn_control = new DFNGenerationControl(GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, -1, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, OutputAtEqualTimeIntervals, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchAdjacentGridblocks, PropagateFracturesInNucleationOrder, ModelTimeUnits);
#if DEBUG_FRACS
                        PetrelLogger.InfoOutputWindow("");
                        PetrelLogger.InfoOutputWindow(string.Format("DFNGenerationControl dfn_control = new DFNGenerationControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, DFNFileType.{12}, {13}, {14}, {15}, {16}, TimeUnits.{17});", GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, -1, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, OutputAtEqualTimeIntervals, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchAdjacentGridblocks, PropagateFracturesInNucleationOrder, ModelTimeUnits));
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
                            if (ModelGrid.DFNThicknessCutoffActivated)
                                PetrelLogger.InfoOutputWindow("The explicit DFN was not generated in one or more cells due to the layer thickness cutoff. To prevent this, reduce the cutoff value on the Control Parameters tab.");
                        }

                        // If the calculation has already been cancelled, do not write any output data
                        if (!progressBarWrapper.abortCalculation())
                        {
                            PetrelLogger.InfoOutputWindow("Start writing output");

                            // Write implicit fracture property data to Petrel grid
                            PetrelLogger.InfoOutputWindow("Write implicit data");
                            progressBar.SetProgressText("Write implicit data");

                            // Get templates for the implicit data
                            // Where possible, use standard Petrel templates
                            Template P30Template = DFNModule2.GetP30Template();
                            Template P32Template = DFNModule2.GetP32Template();
                            Template LengthTemplate = PetrelProject.WellKnownTemplates.GeometricalGroup.Distance;
                            Template DeformationTimeTemplate = PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale;
                            Template ConnectivityTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.Fraction;
                            Template AnisotropyTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.General;
                            Template PorosityTemplate = PetrelProject.WellKnownTemplates.PetrophysicalGroup.Porosity;
                            Template StiffnessTensorComponentTemplate = PetrelProject.WellKnownTemplates.GeophysicalGroup.ModulusCompressional;
                            Template ComplianceTensorComponentTemplate = PetrelProject.WellKnownTemplates.GeophysicalGroup.CompressibilityRock;

                            // Create a transaction to write the property data to the Petrel grid
                            using (ITransaction transactionWritePropertyData = DataManager.NewTransaction())
                            {
                                // Get handle to parent object and lock database
                                PropertyCollection root = PetrelGrid.PropertyCollection;
                                transactionWritePropertyData.Lock(root);

                                // Calculate the number of stages, the number of fracture sets and the total number of calculation elements
                                int NoStages = NoIntermediateOutputs + 1;
                                int NoSets = NoFractureSets;
                                int NoDipSets = ((Mode1Only || Mode2Only) ? 1 : 2);
                                int NoCalculationElementsCompleted = 0;
                                int NoElements = NoActiveGridblocks * ((NoSets * NoDipSets) + (CalculateFractureConnectivityAnisotropy ? (NoSets * NoDipSets) + 1 : 0) + (CalculateFracturePorosity ? 1 : 0));
                                NoElements *= NoStages;
                                // Bulk rock elastic tensors are only output for the final stage
                                if (OutputBulkRockElasticTensors)
                                    NoElements += NoActiveGridblocks;
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
                                    if (OutputAtEqualTimeIntervals)
                                    {
                                        stageEndTime = (stageNumber * endTime) / NoStages;
                                    }
                                    else
                                    {
                                        int gridblockTimestepNo = ((stageNumber * NoGridblockTimesteps) / NoStages);
                                        if (gridblockTimestepNo < 1)
                                            gridblockTimestepNo = 1;
                                        if (gridblockTimestepNo > NoGridblockTimesteps)
                                            gridblockTimestepNo = NoGridblockTimesteps;
                                        stageEndTime = (NoGridblockTimesteps > 0 ? timestepEndtimes[gridblockTimestepNo - 1] : 0);
                                    }

                                    // Create a stage-specific label for the output file
                                    string outputLabel = (stageNumber == NoStages ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, toGeologicalTimeUnits.Convert(stageEndTime), ProjectTimeUnits));

#if DEBUG_FRACS
                                    PetrelLogger.InfoOutputWindow("");
                                    PetrelLogger.InfoOutputWindow("Stage" + outputLabel);
#endif

                                    // Create a property folder for all sets
                                    PropertyCollection FracData = root.CreatePropertyCollection(ModelName + outputLabel);

                                    // Write the input parameters for the model run to the property folder comments string
                                    FracData.Comments = generalInputParams + implicitInputParams;

                                    // Loop through each fracture set
                                    for (int FractureSetNo = 0; FractureSetNo < NoFractureSets; FractureSetNo++)
                                    {
                                        // Set a name for the fracture set
                                        string FractureSetName;
                                        if (NoFractureSets == 1)
                                            FractureSetName = "HMin";
                                        else if (FractureSetNo < hmax_index)
                                        {
                                            FractureSetName = "HMin";
                                            if (FractureSetNo > hmin_index)
                                                FractureSetName += string.Format("+{0}deg", (FractureSetNo - hmin_index) * (180 / NoFractureSets));
                                        }
                                        else
                                        {
                                            FractureSetName = "HMax";
                                            if (FractureSetNo > hmax_index)
                                                FractureSetName += string.Format("+{0}deg", (FractureSetNo - hmin_index) * (180 / NoFractureSets));
                                        }

                                        for (int DipSetNo = 0; DipSetNo < NoDipSets; DipSetNo++)
                                        {
                                            // Create a subfolder for the fracture dip set
                                            string dipsetLabel = (Mode2Only || (DipSetNo > 0)) ? "Inclined" : "Vertical";
                                            string CollectionName = string.Format("{0}_{1}_FracSetData", FractureSetName, dipsetLabel);
                                            PropertyCollection FracSetData = FracData.CreatePropertyCollection(CollectionName);

                                            // Create properties and set templates for each property
                                            Property MF_P30_tot = FracSetData.CreateProperty(P30Template);
                                            MF_P30_tot.Name = "Layer_bound_fracture_P30";
                                            Property MF_P32_tot = FracSetData.CreateProperty(P32Template);
                                            MF_P32_tot.Name = "Layer_bound_fracture_P32";
                                            Property uF_P32_tot = FracSetData.CreateProperty(P32Template);
                                            uF_P32_tot.Name = "Microfracture_P32";
                                            Property MF_MeanLength = FracSetData.CreateProperty(LengthTemplate);
                                            MF_MeanLength.Name = "Mean_fracture_length";

                                            // Add creation event to each property
                                            IHistoryInfoEditor MF_P30_totHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_P30_tot);
                                            IHistoryInfoEditor MF_P32_totHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_P32_tot);
                                            IHistoryInfoEditor uF_P32_totHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(uF_P32_tot);
                                            IHistoryInfoEditor MF_MeanLengthHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_MeanLength);
                                            MF_P30_totHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                            MF_P32_totHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                            uF_P32_totHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                            MF_MeanLengthHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));

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
                                                    FractureDipSet fds = fractureGridCell.FractureSets[FractureSetNo].FractureDipSets[DipSetNo];
                                                    if (finalStage)
                                                    {
                                                        cell_MF_P30_tot = (fds.a_MFP30_total() + fds.sII_MFP30_total() + fds.sIJ_MFP30_total()) / 2;
                                                        cell_MF_P32_tot = fds.a_MFP32_total() + fds.s_MFP32_total();
                                                        cell_uF_P32_tot = fds.a_uFP32_total() + fds.s_uFP32_total();
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
                                                    try
                                                    {
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
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string errorMessage = string.Format("Exception thrown when writing density data for fracture set {0} dipset {1} to column {2}, {3}:", FractureSetNo, DipSetNo, FractureGrid_RowNo, FractureGrid_ColNo);
                                                        errorMessage = errorMessage + string.Format(" cell_MF_P30_tot {0}", (float)cell_MF_P30_tot);
                                                        errorMessage = errorMessage + string.Format(" cell_MF_P32_tot {0}", (float)cell_MF_P32_tot);
                                                        errorMessage = errorMessage + string.Format(" cell_uF_P32_tot {0}", (float)cell_uF_P32_tot);
                                                        errorMessage = errorMessage + string.Format(" cell_MF_MeanLength {0}", (float)cell_MF_MeanLength);
                                                        PetrelLogger.InfoOutputWindow(errorMessage);
                                                        PetrelLogger.InfoOutputWindow(e.Message);
                                                        PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                    }

                                                    // Update progress bar
                                                    progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                                } // End loop through all columns and rows in the Fracture Grid

                                            // If required, write fracture connectivity data to Petrel grid
                                            if (CalculateFractureConnectivityAnisotropy)
                                            {
                                                // Create properties and set templates for each property
                                                Property MF_UnconnectedTipRatio = FracSetData.CreateProperty(ConnectivityTemplate);
                                                MF_UnconnectedTipRatio.Name = "Unconnected_fracture_tip_ratio";
                                                Property MF_RelayTipRatio = FracSetData.CreateProperty(ConnectivityTemplate);
                                                MF_RelayTipRatio.Name = "Relay_zone_fracture_tip_ratio";
                                                Property MF_ConnectedTipRatio = FracSetData.CreateProperty(ConnectivityTemplate);
                                                MF_ConnectedTipRatio.Name = "Connected_fracture_tip_ratio";
                                                Property EndDeformationTime = FracSetData.CreateProperty(DeformationTimeTemplate);
                                                EndDeformationTime.Name = "Time_of_end_macrofracture_growth";

                                                // Add creation event to each property
                                                IHistoryInfoEditor MF_UnconnectedTipRatioHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_UnconnectedTipRatio);
                                                IHistoryInfoEditor MF_RelayTipRatioHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_RelayTipRatio);
                                                IHistoryInfoEditor MF_ConnectedTipRatioHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_ConnectedTipRatio);
                                                IHistoryInfoEditor EndDeformationTimeHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(EndDeformationTime);
                                                MF_UnconnectedTipRatioHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                                MF_RelayTipRatioHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                                MF_ConnectedTipRatioHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                                EndDeformationTimeHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));

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
                                                        FractureDipSet fds = fractureGridCell.FractureSets[FractureSetNo].FractureDipSets[DipSetNo];
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
                                                        try
                                                        {
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
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            string errorMessage = string.Format("Exception thrown when writing anisotropy data for fracture set {0} dipset {1} to column {2}, {3}:", FractureSetNo, DipSetNo, FractureGrid_RowNo, FractureGrid_ColNo);
                                                            errorMessage = errorMessage + string.Format(" UnconnectedTipRatio {0}", (float)UnconnectedTipRatio);
                                                            errorMessage = errorMessage + string.Format(" RelayTipRatio {0}", (float)RelayTipRatio);
                                                            errorMessage = errorMessage + string.Format(" ConnectedTipRatio {0}", (float)ConnectedTipRatio);
                                                            errorMessage = errorMessage + string.Format(" EndTime {0}", (float)EndTime);
                                                            PetrelLogger.InfoOutputWindow(errorMessage);
                                                            PetrelLogger.InfoOutputWindow(e.Message);
                                                            PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                        }

                                                        // Update progress bar
                                                        progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                                    } // End loop through all columns and rows in the Fracture Grid
                                            } // End write fracture connectivity data to Petrel grid

                                        } // End loop through fracture dip sets
                                    } // End loop through fracture sets

                                    // Write fracture anisotropy data to Petrel grid
                                    if (CalculateFractureConnectivityAnisotropy)
                                    {
                                        // Create a subfolder for the fracture anisotropy data
                                        string CollectionName = "Fracture_Anisotropy";
                                        PropertyCollection FracAnisotropyData = FracData.CreatePropertyCollection(CollectionName);

                                        // Create properties and set templates for each property
                                        Property P32_Anisotropy = FracAnisotropyData.CreateProperty(AnisotropyTemplate);
                                        P32_Anisotropy.Name = "P32_anisotropy";
                                        Property P33_Anisotropy = FracAnisotropyData.CreateProperty(AnisotropyTemplate);
                                        if (CalculateFracturePorosity)
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

                                        // Add creation event to each property
                                        IHistoryInfoEditor P32_AnisotropyHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(P32_Anisotropy);
                                        IHistoryInfoEditor P33_AnisotropyHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(P33_Anisotropy);
                                        IHistoryInfoEditor MF_UnconnectedTipRatioHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_UnconnectedTipRatio);
                                        IHistoryInfoEditor MF_RelayTipRatioHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_RelayTipRatio);
                                        IHistoryInfoEditor MF_ConnectedTipRatioHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_ConnectedTipRatio);
                                        IHistoryInfoEditor EndDeformationTimeHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(EndDeformationTime);
                                        P32_AnisotropyHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        P33_AnisotropyHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        MF_UnconnectedTipRatioHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        MF_RelayTipRatioHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        MF_ConnectedTipRatioHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        EndDeformationTimeHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));

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

                                                // Calculate fracture anisotropy and connectivity for the entire fracture network
                                                double P32_anisotropy, P33_anisotropy;
                                                double UnconnectedTipRatio, RelayTipRatio, ConnectedTipRatio, EndTime;
                                                if (finalStage)
                                                {
                                                    // Calculate fracture anisotropy data using the functions in the GridblockConfiguration object
                                                    P32_anisotropy = fractureGridCell.P32AnisotropyIndex(true);
                                                    if (CalculateFracturePorosity)
                                                        P33_anisotropy = fractureGridCell.FracturePorosityAnisotropyIndex(FractureApertureControl);
                                                    else
                                                        P33_anisotropy = fractureGridCell.P33AnisotropyIndex(true);

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

                                                    // Calculate fracture anisotropy data using the data cached in the FCDList
                                                    double HMin_P32 = 0;
                                                    double HMax_P32 = 0;
                                                    double HMin_P33 = 0;
                                                    double HMax_P33 = 0;
                                                    // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
                                                    if (NoFractureSets < 2)
                                                    {
                                                        HMin_P32 = 1;
                                                        HMin_P33 = 1;
                                                    }
                                                    else
                                                    {
                                                        foreach (FractureDipSet fds in fractureGridCell.FractureSets[hmin_index].FractureDipSets)
                                                        {
                                                            HMin_P32 += fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo);
                                                            if (CalculateFracturePorosity)
                                                            {
                                                                HMin_P33 += fds.getTotaluFPorosity(FractureApertureControl, false, TSNo);
                                                                HMin_P33 += fds.getTotalMFPorosity(FractureApertureControl, false, TSNo);
                                                            }
                                                            else
                                                            {
                                                                HMin_P33 += fds.getTotaluFP33(TSNo);
                                                                HMin_P33 += fds.getTotalMFP33(TSNo);
                                                            }
                                                        }
                                                        foreach (FractureDipSet fds in fractureGridCell.FractureSets[hmax_index].FractureDipSets)
                                                        {
                                                            HMax_P32 += fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo);
                                                            if (CalculateFracturePorosity)
                                                            {
                                                                HMax_P33 += fds.getTotaluFPorosity(FractureApertureControl, false, TSNo);
                                                                HMax_P33 += fds.getTotalMFPorosity(FractureApertureControl, false, TSNo);
                                                            }
                                                            else
                                                            {
                                                                HMax_P33 += fds.getTotaluFP33(TSNo);
                                                                HMax_P33 += fds.getTotalMFP33(TSNo);
                                                            }
                                                        }
                                                    }
                                                    double Combined_P32 = HMin_P32 + HMax_P32;
                                                    P32_anisotropy = (Combined_P32 > 0 ? (HMin_P32 - HMax_P32) / Combined_P32 : 0);
                                                    double Combined_P33 = HMin_P33 + HMax_P33;
                                                    P33_anisotropy = (Combined_P33 > 0 ? (HMin_P33 - HMax_P33) / Combined_P33 : 0);

                                                    // Calculate fracture connectivity data using the data cached in the FCDList
                                                    double INodes = 0;
                                                    double RNodes = 0;
                                                    double YNodes = 0;
                                                    foreach (Gridblock_FractureSet fs in fractureGridCell.FractureSets)
                                                        foreach (FractureDipSet fds in fs.FractureDipSets)
                                                        {
                                                            INodes += fds.getActiveMFP30(TSNo);
                                                            RNodes += fds.getStaticRelayMFP30(TSNo);
                                                            YNodes += fds.getStaticIntersectMFP30(TSNo);
                                                        }
                                                    double TotalNodes = INodes + RNodes + YNodes;
                                                    UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : 0);
                                                    RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : 0);
                                                    ConnectedTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : 0);

                                                    // Get the time at the end of this intermediate stage
                                                    EndTime = stageEndTime;
                                                }

#if DEBUG_FRACS
                                                PetrelLogger.InfoOutputWindow("");
                                                PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                                // Loop through all the Petrel cells in the gridblock
                                                try
                                                {
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
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing anisotropy data to column {0}, {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                    errorMessage = errorMessage + string.Format(" P32_anisotropy {0}", (float)P32_anisotropy);
                                                    errorMessage = errorMessage + string.Format(" P33_anisotropy {0}", (float)P33_anisotropy);
                                                    errorMessage = errorMessage + string.Format(" UnconnectedTipRatio {0}", (float)UnconnectedTipRatio);
                                                    errorMessage = errorMessage + string.Format(" RelayTipRatio {0}", (float)RelayTipRatio);
                                                    errorMessage = errorMessage + string.Format(" ConnectedTipRatio {0}", (float)ConnectedTipRatio);
                                                    errorMessage = errorMessage + string.Format(" EndTime {0}", (float)EndTime);
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }

                                                // Update progress bar
                                                progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the Fracture Grid

                                    } // End write fracture anisotropy data

                                    // Write fracture porosity data to Petrel grid
                                    if (CalculateFracturePorosity)
                                    {
                                        // Create a subfolder for the fracture porosity data
                                        string CollectionName = "Fracture_Porosity";
                                        PropertyCollection FracPorosityData = FracData.CreatePropertyCollection(CollectionName);

                                        // Create properties and set templates for microfracture and macrofracture porosity and combined P32 values
                                        Property uF_P32combined = FracPorosityData.CreateProperty(P32Template);
                                        Property MF_P32combined = FracPorosityData.CreateProperty(P32Template);
                                        Property uF_Porosity = FracPorosityData.CreateProperty(PorosityTemplate);
                                        Property MF_Porosity = FracPorosityData.CreateProperty(PorosityTemplate);

                                        // Set property template names
                                        uF_P32combined.Name = "Microfracture_combined_P32";
                                        MF_P32combined.Name = "Layer_bound_fracture_combined_P32";
                                        switch (FractureApertureControl)
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

                                        // Add creation event to each property
                                        IHistoryInfoEditor uF_P32combinedHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(uF_P32combined);
                                        IHistoryInfoEditor MF_P32combinedHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_P32combined);
                                        IHistoryInfoEditor uF_PorosityHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(uF_Porosity);
                                        IHistoryInfoEditor MF_PorosityHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(MF_Porosity);
                                        uF_P32combinedHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        MF_P32combinedHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        uF_PorosityHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                        MF_PorosityHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));

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
                                                    foreach (Gridblock_FractureSet fs in fractureGridCell.FractureSets)
                                                    {
                                                        uF_P32_value += fs.combined_T_uFP32_total();
                                                        MF_P32_value += fs.combined_T_MFP32_total();
                                                        uF_Porosity_value += fs.combined_uF_Porosity(FractureApertureControl);
                                                        MF_Porosity_value += fs.combined_MF_Porosity(FractureApertureControl);
                                                    }
                                                }
                                                else
                                                {
                                                    foreach (Gridblock_FractureSet fs in fractureGridCell.FractureSets)
                                                        foreach (FractureDipSet fds in fs.FractureDipSets)
                                                        {
                                                            int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                            uF_P32_value += fds.getTotaluFP32(TSNo);
                                                            MF_P32_value += fds.getTotalMFP32(TSNo);
                                                            // We will calculate the porosity based on fracture aperture during the respective stage, rather than the current aperture
                                                            uF_Porosity_value += fds.getTotaluFPorosity(FractureApertureControl, false, TSNo);
                                                            MF_Porosity_value += fds.getTotalMFPorosity(FractureApertureControl, false, TSNo);
                                                        }
                                                }

#if DEBUG_FRACS
                                                PetrelLogger.InfoOutputWindow("");
                                                PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                                // Loop through all the Petrel cells in the gridblock
                                                try
                                                {
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
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing porosity data to column {0}, {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                    errorMessage = errorMessage + string.Format(" uF_P32_value {0}", (float)uF_P32_value);
                                                    errorMessage = errorMessage + string.Format(" MF_P32_value {0}", (float)MF_P32_value);
                                                    errorMessage = errorMessage + string.Format(" uF_Porosity_value {0}", (float)uF_Porosity_value);
                                                    errorMessage = errorMessage + string.Format(" MF_Porosity_value {0}", (float)MF_Porosity_value);
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }

                                                // Update progress bar
                                                progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the Fracture Grid

                                    } // End write fracture porosity data

                                    // Write stiffness and compliance tensor data to Petrel grid
                                    if (OutputBulkRockElasticTensors && finalStage)
                                    {

                                        // Create subfolders for the bulk rock compliance and stiffness tensor components
                                        string ComplianceTensorCollectionName = "Bulk rock compliance tensor";
                                        PropertyCollection ComplianceTensorData = FracData.CreatePropertyCollection(ComplianceTensorCollectionName);
                                        string StiffnessTensorCollectionName = "Bulk rock stiffness tensor";
                                        PropertyCollection StiffnessTensorData = FracData.CreatePropertyCollection(StiffnessTensorCollectionName);

                                        // Create properties and set templates for each component of both tensors
                                        Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, Property>> ComplianceTensorProperties = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, Property>>();
                                        Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, Property>> StiffnessTensorProperties = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, Property>>();
                                        Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                                        foreach (Tensor2SComponents ij in tensorComponents)
                                        {
                                            ComplianceTensorProperties[ij] = new Dictionary<Tensor2SComponents, Property>();
                                            StiffnessTensorProperties[ij] = new Dictionary<Tensor2SComponents, Property>();
                                            foreach (Tensor2SComponents kl in tensorComponents)
                                            {
                                                Property ComplianceTensor_ijkl = ComplianceTensorData.CreateProperty(ComplianceTensorComponentTemplate);
                                                ComplianceTensor_ijkl.Name = string.Format("S_{0}{1}", ij, kl);
                                                IHistoryInfoEditor ComplianceTensor_ijklHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(ComplianceTensor_ijkl);
                                                ComplianceTensor_ijklHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                                ComplianceTensorProperties[ij][kl] = ComplianceTensor_ijkl;

                                                Property StiffnessTensor_ijkl = StiffnessTensorData.CreateProperty(StiffnessTensorComponentTemplate);
                                                StiffnessTensor_ijkl.Name = string.Format("C_{0}{1}", ij, kl);
                                                IHistoryInfoEditor StiffnessTensor_ijklHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(StiffnessTensor_ijkl);
                                                StiffnessTensor_ijklHistoryInfoEditor.AddHistoryEntry(new HistoryEntry("Create dynamic implicit fracture model", "", PetrelSystem.VersionInfo.ToString()));
                                                StiffnessTensorProperties[ij][kl] = StiffnessTensor_ijkl;
                                            }
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

                                                // Get the compliance and stiffness tensors for this gridblock
                                                Tensor4_2Sx2S gridblockComplianceTensor = fractureGridCell.S_b;
                                                Tensor4_2Sx2S gridblockStiffnessTensor = gridblockComplianceTensor.Inverse();

#if DEBUG_FRACS
                                                PetrelLogger.InfoOutputWindow("");
                                                PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock {0}, {1}", FractureGrid_RowNo, FractureGrid_ColNo));
#endif

                                                // Loop through all the Petrel cells in the gridblock
                                                // We need to define the last ij and kl components outside the loop so we can identify the tensor component if an exception is thrown
                                                Tensor2SComponents lastij = Tensor2SComponents.XX;
                                                Tensor2SComponents lastkl = Tensor2SComponents.XX;
                                                try
                                                {
                                                    for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                                        for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                            for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                            {
#if DEBUG_FRACS
                                                                PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid cell {0}, {1}, {2}", PetrelGrid_I, PetrelGrid_J, PetrelGrid_K));
#endif

                                                                // Get index for cell in Petrel grid
                                                                Index3 index_cell = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                                // Write tensor component data to Petrel grid
                                                                foreach (Tensor2SComponents ij in tensorComponents)
                                                                {
                                                                    lastij = ij;
                                                                    foreach (Tensor2SComponents kl in tensorComponents)
                                                                    {
                                                                        lastkl = kl;
                                                                        ComplianceTensorProperties[ij][kl][index_cell] = (float)gridblockComplianceTensor.Component(ij, kl);
                                                                        StiffnessTensorProperties[ij][kl][index_cell] = (float)gridblockStiffnessTensor.Component(ij, kl);
                                                                    }
                                                                }
                                                            } // End loop through all the Petrel cells in the gridblock
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing bulk rock elastic tensor components to column {0}, {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                    errorMessage = errorMessage + string.Format(" S_{0}{1} {2}", lastij, lastkl, (float)gridblockComplianceTensor.Component(lastij, lastkl));
                                                    errorMessage = errorMessage + string.Format(" C_{0}{1} {2}", lastij, lastkl, (float)gridblockStiffnessTensor.Component(lastij, lastkl));
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }

                                                // Update progress bar
                                                progressBarWrapper.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the Fracture Grid

                                    } // End write stiffness and compliance tensor data

                                } // End loop through each stage in the fracture growth

                                // Commit the changes to the Petrel database
                                transactionWritePropertyData.Commit();
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
                                if (OutputCentrepoints) numberOfElements += totalNoFractures;
                                progressBarWrapper.SetNumberOfElements(numberOfElements);
                                int noFracturesGenerated = 0;

                                // Loop through each stage in the fracture growth
                                int stageNumber = 1;
                                int NoStages = ModelGrid.DFNGrowthStages.Count;
                                foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                                {
                                    // Create a stage-specific label for the output file
                                    string outputLabel = (stageNumber == NoStages ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, toGeologicalTimeUnits.Convert(DFN.CurrentTime), ProjectTimeUnits));

                                    // Create a new fracture network object
                                    FractureNetwork fractureNetwork;
                                    using (ITransaction transactionCreateFractureNetwork = DataManager.NewTransaction())
                                    {
                                        // Get handle to parent object and lock database
                                        ModelCollection GridColl = PetrelGrid.ModelCollection;
                                        transactionCreateFractureNetwork.Lock(GridColl);

                                        // Create a new fracture network object
                                        fractureNetwork = GridColl.CreateFractureNetwork(ModelName + outputLabel, Domain.ELEVATION_DEPTH);

                                        // Add creation event to the DFN object history
                                        HistoryEntry dfnCreationEvent = new HistoryEntry("Create dynamic DFN", "", PetrelSystem.VersionInfo.ToString());
                                        IHistoryInfoEditor dfnHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(fractureNetwork);
                                        dfnHistoryInfoEditor.AddHistoryEntry(dfnCreationEvent);

                                        // Write the input parameters for the model run to the fracture network object comments string
                                        fractureNetwork.Comments = generalInputParams + explicitInputParams;

                                        // Commit the changes to the Petrel database
                                        transactionCreateFractureNetwork.Commit();
                                    }

                                    // Write the model time of the intermediate DFN to the Petrel log window
                                    PetrelLogger.InfoOutputWindow(string.Format("DFN realisation {0} at time {1} {2}", stageNumber, toGeologicalTimeUnits.Convert(DFN.CurrentTime), ProjectTimeUnits));

                                    // Create the fracture objects
                                    using (ITransaction transactionCreateFractures = DataManager.NewTransaction())
                                    {
                                        // Lock the database
                                        transactionCreateFractures.Lock(fractureNetwork);

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
                                            int uF_set = uF.SetIndex;

                                            // Get a list of cornerpoints around the circumference of the fracture, as PointXYZ objects
                                            List<PointXYZ> CornerPoints = uF.GetFractureCornerpointsInXYZ(Number_uF_Points);

                                            if (CreateTriangularFractureSegments)
                                            {
                                                try
                                                {
                                                    // Get a reference to the centre of the fracture as a PointXYZ object
                                                    PointXYZ CP1 = uF.CentrePoint;

                                                    // Loop through each cornerpoint creating a new triangular element
                                                    for (int cornerPointNo = 0; cornerPointNo < Number_uF_Points; cornerPointNo++)
                                                    {
                                                        // Create a collection of Petrel Point3 objects
                                                        List<Point3> Petrel_CornerPoints = new List<Point3>();

                                                        // Add the centrepoint, the current cornerpoint and the next cornerpoint to the Petrel point collection
                                                        PointXYZ CP2 = CornerPoints[cornerPointNo];
                                                        PointXYZ CP3 = (cornerPointNo < Number_uF_Points - 1 ? CornerPoints[cornerPointNo + 1] : CornerPoints[0]);
                                                        Petrel_CornerPoints.Add(new Point3(CP1.X, CP1.Y, CP1.Depth));
                                                        Petrel_CornerPoints.Add(new Point3(CP2.X, CP2.Y, CP2.Depth));
                                                        Petrel_CornerPoints.Add(new Point3(CP3.X, CP3.Y, CP3.Depth));

                                                        // Create a new fracture patch object
                                                        FracturePatch Petrel_FractureSegment = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints);

                                                        // Assign it to the correct set, if required
                                                        if (assignOrientationSets)
                                                            Petrel_FractureSegment.FractureSetValue = uF_set;
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing microfracture:");
                                                    foreach (PointXYZ CornerPoint in CornerPoints)
                                                        errorMessage = errorMessage + string.Format(" ({0},{1},{2})", CornerPoint.X, CornerPoint.Y, CornerPoint.Depth);
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }

                                            }
                                            else
                                            {
                                                try
                                                {
                                                    // Create a collection of Petrel Point3 objects
                                                    List<Point3> Petrel_CornerPoints = new List<Point3>();

                                                    // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the Petrel point collection
                                                    foreach (PointXYZ CornerPoint in CornerPoints)
                                                        Petrel_CornerPoints.Add(new Point3(CornerPoint.X, CornerPoint.Y, CornerPoint.Depth));

                                                    // Create a new fracture patch object
                                                    FracturePatch Petrel_FractureSegment = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints);

                                                    // Assign it to the correct set, if required
                                                    if (assignOrientationSets)
                                                        Petrel_FractureSegment.FractureSetValue = uF_set;
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing microfracture:");
                                                    foreach (PointXYZ CornerPoint in CornerPoints)
                                                        errorMessage = errorMessage + string.Format(" ({0},{1},{2})", CornerPoint.X, CornerPoint.Y, CornerPoint.Depth);
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }
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
                                            int MF_set = MF.SetIndex;

                                            // Loop through each macrofracture segment
                                            foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                            {
                                                int noSegments = MF.SegmentCornerPoints[dir].Count;
                                                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                                                {
                                                    // Get a reference to the cornerpoint list for the segment
                                                    List<PointXYZ> segment = MF.SegmentCornerPoints[dir][segmentNo];

                                                    if (CreateTriangularFractureSegments)
                                                    {
                                                        try
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
#if DEBUG_FRACS
                                                            foreach (PointXYZ point in new PointXYZ[] { CP1, CP2, CP3, CP4 })
                                                            {
                                                                if (point.X < minX) point.X = minX;
                                                                if (point.Y < minY) point.Y = minY;
                                                                if (point.Z < minZ) point.Z = minZ;
                                                                if (point.X > maxX) point.X = maxX;
                                                                if (point.Y > maxY) point.Y = maxY;
                                                                if (point.Z > maxZ) point.Z = maxZ;
                                                            }
#endif
                                                            Petrel_CornerPoints1.Add(new Point3(CP1.X, CP1.Y, CP1.Depth));
                                                            Petrel_CornerPoints1.Add(new Point3(CP2.X, CP2.Y, CP2.Depth));
                                                            Petrel_CornerPoints1.Add(new Point3(CP3.X, CP3.Y, CP3.Depth));
                                                            Petrel_CornerPoints2.Add(new Point3(CP3.X, CP3.Y, CP3.Depth));
                                                            Petrel_CornerPoints2.Add(new Point3(CP4.X, CP4.Y, CP4.Depth));
                                                            Petrel_CornerPoints2.Add(new Point3(CP1.X, CP1.Y, CP1.Depth));

                                                            // Create two new fracture patch objects
                                                            FracturePatch Petrel_FractureSegment1 = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints1);
                                                            FracturePatch Petrel_FractureSegment2 = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints2);

                                                            // Assign them to the correct set, if required
                                                            if (assignOrientationSets)
                                                            {
                                                                Petrel_FractureSegment1.FractureSetValue = MF_set;
                                                                Petrel_FractureSegment2.FractureSetValue = MF_set;
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            string errorMessage = string.Format("Exception thrown when writing macrofracture segment:");
                                                            foreach (PointXYZ CornerPoint in segment)
                                                                errorMessage = errorMessage + string.Format(" ({0},{1},{2})", CornerPoint.X, CornerPoint.Y, CornerPoint.Depth);
                                                            PetrelLogger.InfoOutputWindow(errorMessage);
                                                            PetrelLogger.InfoOutputWindow(e.Message);
                                                            PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            // Create a collection of Petrel Point3 objects
                                                            List<Point3> Petrel_CornerPoints = new List<Point3>();

                                                            // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the Petrel point collection
                                                            foreach (PointXYZ CornerPoint in segment)
                                                            {
#if DEBUG_FRACS
                                                                {
                                                                    if (CornerPoint.X < minX) CornerPoint.X = minX;
                                                                    if (CornerPoint.Y < minY) CornerPoint.Y = minY;
                                                                    if (CornerPoint.Z < minZ) CornerPoint.Z = minZ;
                                                                    if (CornerPoint.X > maxX) CornerPoint.X = maxX;
                                                                    if (CornerPoint.Y > maxY) CornerPoint.Y = maxY;
                                                                    if (CornerPoint.Z > maxZ) CornerPoint.Z = maxZ;
                                                                }
#endif
                                                                Petrel_CornerPoints.Add(new Point3(CornerPoint.X, CornerPoint.Y, CornerPoint.Depth));
                                                            }

                                                            // Create a new fracture patch object
                                                            FracturePatch Petrel_FractureSegment = fractureNetwork.CreateFracturePatch(Petrel_CornerPoints);

                                                            // Assign it to the correct set, if required
                                                            if (assignOrientationSets)
                                                                Petrel_FractureSegment.FractureSetValue = (MF_set + 1); // Gridblock fracture sets are zero referenced, Petrel fracture sets are not
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            string errorMessage = string.Format("Exception thrown when writing macrofracture segment:");
                                                            foreach (PointXYZ CornerPoint in segment)
                                                                errorMessage = errorMessage + string.Format(" ({0},{1},{2})", CornerPoint.X, CornerPoint.Y, CornerPoint.Depth);
                                                            PetrelLogger.InfoOutputWindow(errorMessage);
                                                            PetrelLogger.InfoOutputWindow(e.Message);
                                                            PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                        }
                                                    }
                                                }

                                            }

                                            // Update progress bar
                                            progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                        }

                                        // Commit the changes to the Petrel database
                                        transactionCreateFractures.Commit();
                                    }

                                    // Assign the fracture properties
                                    using (ITransaction transactionAssignFractureProperties = DataManager.NewTransaction())
                                    {

                                        // Get handle to the appropriate fracture properties
                                        // At present we will only write fracture aperture data - this could be expanded to include fracture permeability, connectivity and nucleation time
                                        FracturePatchProperty fractureAperture = FracturePatchProperty.NullObject;
                                        FracturePatchPropertyType aperture = WellKnownFracturePatchPropertyTypes.Aperture;
                                        if (fractureNetwork.HasWellKnownProperty(aperture))
                                        {
                                            fractureAperture = fractureNetwork.GetWellKnownProperty(aperture);
                                            // Lock the database
                                            transactionAssignFractureProperties.Lock(fractureAperture);
                                        }
                                        else
                                        {
                                            // Lock the database
                                            transactionAssignFractureProperties.Lock(fractureNetwork);
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

                                            if (CreateTriangularFractureSegments)
                                            {
                                                // Get a reference to the centre of the fracture as a PointXYZ object
                                                //PointXYZ CP1 = uF.CentrePoint;

                                                // Loop through each cornerpoint creating a new triangular element
                                                for (int cornerPointNo = 0; cornerPointNo < Number_uF_Points; cornerPointNo++)
                                                {
                                                    // Assign the appropriate fracture properties to the new patch object
                                                    try
                                                    {
                                                        if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = uF_Aperture;
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string errorMessage = string.Format("Exception thrown when writing properties to microfracture patch {0}:", PatchNo);
                                                        errorMessage = errorMessage + string.Format(" Aperture {0}", uF_Aperture);
                                                        PetrelLogger.InfoOutputWindow(errorMessage);
                                                        PetrelLogger.InfoOutputWindow(e.Message);
                                                        PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                    }
                                                    PatchNo++;
                                                }

                                            }
                                            else
                                            {
                                                // Assign the appropriate fracture properties to the new patch object
                                                try
                                                {
                                                    if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = uF_Aperture;
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing properties to microfracture patch {0}:", PatchNo);
                                                    errorMessage = errorMessage + string.Format(" Aperture {0}", uF_Aperture);
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }
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

                                                    if (CreateTriangularFractureSegments)
                                                    {
                                                        // Assign the appropriate fracture properties to the two new patch objects
                                                        try
                                                        {
                                                            if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = MF_Aperture;
                                                            if (PatchNo + 1 < NoPatches) fractureAperture[PatchNo + 1].Value = MF_Aperture;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            string errorMessage = string.Format("Exception thrown when writing properties to macrofracture patch {0}:", PatchNo);
                                                            errorMessage = errorMessage + string.Format(" Aperture {0}", MF_Aperture);
                                                            PetrelLogger.InfoOutputWindow(errorMessage);
                                                            PetrelLogger.InfoOutputWindow(e.Message);
                                                            PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                        }
                                                        PatchNo += 2;
                                                    }
                                                    else
                                                    {
                                                        // Assign the appropriate fracture properties to the new patch object
                                                        try
                                                        {
                                                            if (PatchNo < NoPatches) fractureAperture[PatchNo].Value = MF_Aperture;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            string errorMessage = string.Format("Exception thrown when writing properties to macrofracture patch {0}:", PatchNo);
                                                            errorMessage = errorMessage + string.Format(" Aperture {0}", MF_Aperture);
                                                            PetrelLogger.InfoOutputWindow(errorMessage);
                                                            PetrelLogger.InfoOutputWindow(e.Message);
                                                            PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                        }
                                                        PatchNo++;
                                                    }
                                                }

                                            }

                                            // Update progress bar
                                            progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                        }

                                        // Commit the changes to the Petrel database
                                        transactionAssignFractureProperties.Commit();
                                    }

                                    // Update the stage number
                                    stageNumber++;
                                }

                                // If required, create fracture centrelines as polylines
                                if (OutputCentrepoints)
                                {
                                    // Create a new collection for the polyline output
                                    Collection CentrelineCollection = Collection.NullObject;
                                    using (ITransaction transactionCreateCentrelineCollection = DataManager.NewTransaction())
                                    {
                                        // Get handle to project and lock database
                                        Project project = PetrelProject.PrimaryProject;
                                        transactionCreateCentrelineCollection.Lock(project);

                                        // Create a new collection for the polyline set
                                        CentrelineCollection = project.CreateCollection(ModelName + "_Centrelines");

                                        // Write the input parameters for the model run to the collection comments string
                                        CentrelineCollection.Comments = generalInputParams + explicitInputParams;

                                        // Commit the changes to the Petrel database
                                        transactionCreateCentrelineCollection.Commit();
                                    }

                                    // Loop through each stage in the fracture growth
                                    stageNumber = 1;
                                    foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                                    {
                                        // Create a stage-specific label for the output file
                                        string outputLabel = (stageNumber == NoStages ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, toGeologicalTimeUnits.Convert(DFN.CurrentTime), ProjectTimeUnits));

                                        using (ITransaction transactionCreateCentrelines = DataManager.NewTransaction())
                                        {
                                            // Lock database
                                            transactionCreateCentrelines.Lock(CentrelineCollection);

                                            // Create a new polyline set and set to the depth domain
                                            PolylineSet Centrelines = CentrelineCollection.CreatePolylineSet("Fracture_Centrelines" + outputLabel);
                                            Centrelines.Domain = Domain.ELEVATION_DEPTH;

                                            // Add creation event to the polyline set object history
                                            HistoryEntry centrelinesCreationEvent = new HistoryEntry("Create dynamic DFN", "", PetrelSystem.VersionInfo.ToString());
                                            IHistoryInfoEditor centrelinesHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(Centrelines);
                                            centrelinesHistoryInfoEditor.AddHistoryEntry(centrelinesCreationEvent);

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
                                                try
                                                {
                                                    List<Point3> newpline = new List<Point3>();
                                                    foreach (PointXYZ grid_Point in grid_Centreline)
                                                        newpline.Add(new Point3(grid_Point.X, grid_Point.Y, grid_Point.Depth));
                                                    pline_list.Add(new Polyline3(newpline));
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing centreline:");
                                                    foreach (PointXYZ grid_Point in grid_Centreline)
                                                        errorMessage = errorMessage + string.Format(" ({0},{1},{2})", grid_Point.X, grid_Point.Y, grid_Point.Depth);
                                                    PetrelLogger.InfoOutputWindow(errorMessage);
                                                    PetrelLogger.InfoOutputWindow(e.Message);
                                                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                                                }

                                                // Update progress bar
                                                progressBarWrapper.UpdateProgress(++noFracturesGenerated);
                                            }

                                            // Add the polyline list to the polyline set
                                            Centrelines.Polylines = pline_list;

                                            // Commit the changes to the Petrel database
                                            transactionCreateCentrelines.Commit();
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
                                        try
                                        {
                                            // Create a collection of Petrel Point3 objects
                                            List<Point3> Petrel_CornerPoints = new List<Point3>();

                                            // Convert each cornerpoint from a PointXYZ to a Point3 object and add it to the Petrel point collection
                                            foreach (PointXYZ CornerPoint in Segment)
                                                Petrel_CornerPoints.Add(new Point3(CornerPoint.X, CornerPoint.Y, CornerPoint.Depth));

                                            // Add the cornerpoints to the set of points for comparison with the corners of the fracture patches
                                            CornersAsPoints.AddRange(Petrel_CornerPoints);
                                        }
                                        catch (Exception e)
                                        {
                                            string errorMessage = string.Format("Exception thrown when writing fracture cornerpoints:");
                                            foreach (PointXYZ CornerPoint in Segment)
                                                errorMessage = errorMessage + string.Format(" ({0},{1},{2})", CornerPoint.X, CornerPoint.Y, CornerPoint.Depth);
                                            PetrelLogger.InfoOutputWindow(errorMessage);
                                            PetrelLogger.InfoOutputWindow(e.Message);
                                            PetrelLogger.InfoOutputWindow(e.StackTrace);
                                        }
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
                catch (Exception e)
                {
                    PetrelLogger.InfoOutputWindow("Calculation failed: exception thrown");
                    PetrelLogger.InfoOutputWindow(e.Message);
                    PetrelLogger.InfoOutputWindow(e.StackTrace);
                }
            }
        }

        #endregion

        /// <summary>
        /// ArgumentPackage class for DFNGenerator.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
#if MANAGED_PERSISTENCE
        [Archivable]
        public class Arguments : DescribedArgumentsByReflection, IIdentifiable, IDisposable
#else
        public class Arguments : DescribedArgumentsByReflection
#endif
        {
            public Arguments()
                : this(DataManager.DataSourceManager)
            {
            }

            public Arguments(IDataSourceManager dataSourceManager)
            {
#if MANAGED_PERSISTENCE
                // Create argument package in correct project; for RPT scenarios
                dataSource = DFNGeneratorDataSourceFactory.Get(dataSourceManager);
                if (dataSource != null)
                {
                    arguments_Droid = dataSource.GenerateDroid();
                    dataSource.AddItem(arguments_Droid, this);
                }
#endif
            }

#if MANAGED_PERSISTENCE
            // Keep data source to dispose of transient argument packages
            [ArchivableContextInject]
            private StructuredArchiveDataSource dataSource;

            // Main settings
            [Archived(IsOptional = true)]
            private string argument_ModelName = "New DFN";
            [Archived(IsOptional = true)]
            private Droid argument_Grid;
            [Archived(IsOptional = true)]
            private int argument_StartColI = 1;
            [Archived(IsOptional = true)]
            private int argument_StartRowJ = 1;
            [Archived(IsOptional = true)]
            private int argument_NoColsI = 1;
            [Archived(IsOptional = true)]
            private int argument_NoRowsJ = 1;
            [Archived(IsOptional = true)]
            private int argument_TopLayerK = 1;
            [Archived(IsOptional = true)]
            private int argument_BottomLayerK = 1;
            [Archived(IsOptional = true)]
            private double argument_EhminAzi_default = 0;
            [Archived(IsOptional = true)]
            private Droid argument_EhminAzi;
            // Unit conversion and labelling for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
            // Therefore strain rate units EhminRate and EhmaxRate are stored in geological time units, not SI units
            [Archived(IsOptional = true)]
            private double argument_EhminRate_default = -0.01;
            [Archived(IsOptional = true)]
            private Droid argument_EhminRate;
            [Archived(IsOptional = true)]
            private double argument_EhmaxRate_default = 0;
            [Archived(IsOptional = true)]
            private Droid argument_EhmaxRate;
            [Archived(IsOptional = true)]
            private bool argument_GenerateExplicitDFN = true;
            [Archived(IsOptional = true)]
            private int argument_NoIntermediateOutputs = 0;
            [Archived(IsOptional = true)]
            private bool argument_IncludeObliqueFracs = false;

            // Mechanical properties
            [Archived(IsOptional = true)]
            private double argument_YoungsMod_default = 10000000000;
            [Archived(IsOptional = true)]
            private Droid argument_YoungsMod;
            [Archived(IsOptional = true)]
            private double argument_PoissonsRatio_default = 0.25;
            [Archived(IsOptional = true)]
            private Droid argument_PoissonsRatio;
            [Archived(IsOptional = true)]
            private double argument_BiotCoefficient_default = 1;
            [Archived(IsOptional = true)]
            private Droid argument_BiotCoefficient;
            [Archived(IsOptional = true)]
            private double argument_FrictionCoefficient_default = 0.5;
            [Archived(IsOptional = true)]
            private Droid argument_FrictionCoefficient;
            [Archived(IsOptional = true)]
            private double argument_CrackSurfaceEnergy_default = 1000;
            [Archived(IsOptional = true)]
            private Droid argument_CrackSurfaceEnergy;
            // NB the rock strain relaxation and fracture relaxation time constants must be supplied in SI units (seconds), not geological time units
            [Archived(IsOptional = true)]
            private double argument_RockStrainRelaxation_default = 0;
            [Archived(IsOptional = true)]
            private Droid argument_RockStrainRelaxation;
            [Archived(IsOptional = true)]
            private double argument_FractureStrainRelaxation_default = 0;
            [Archived(IsOptional = true)]
            private Droid argument_FractureStrainRelaxation;
            // InitialMicrofractureDensity A is stored in project units rather than SI units, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
            // However it is initialised with a default value equivalent to 0.001fracs/m, since this is calibrated empirically
            // We therefore set up a private variable (in project units) and a private initialising variable (in SI units); the project unit variable will only be set when first called
            [Archived(IsOptional = true)]
            private double argument_InitialMicrofractureDensity_SI = 0.001;
            [Archived(IsOptional = true)]
            private double argument_InitialMicrofractureDensity_default = double.NaN;
            [Archived(IsOptional = true)]
            private Droid argument_InitialMicrofractureDensity;
            [Archived(IsOptional = true)]
            private double argument_InitialMicrofractureSizeDistribution_default = 2;
            [Archived(IsOptional = true)]
            private Droid argument_InitialMicrofractureSizeDistribution;
            [Archived(IsOptional = true)]
            private double argument_SubcriticalPropagationIndex_default = 10;
            [Archived(IsOptional = true)]
            private Droid argument_SubcriticalPropagationIndex;
            [Archived(IsOptional = true)]
            private double argument_CriticalPropagationRate = 2000;
            [Archived(IsOptional = true)]
            private bool argument_AverageMechanicalPropertyData = true;

            // Stress state
            [Archived(IsOptional = true)]
            private int argument_StressDistribution = 1;
            [Archived(IsOptional = true)]
            private double argument_DepthAtDeformation = double.NaN;
            [Archived(IsOptional = true)]
            private double argument_MeanOverlyingSedimentDensity = 2250;
            [Archived(IsOptional = true)]
            private double argument_FluidDensity = 1000;
            [Archived(IsOptional = true)]
            private double argument_InitialOverpressure = 0;
            [Archived(IsOptional = true)]
            private double argument_InitialStressRelaxation = 1;
            [Archived(IsOptional = true)]
            private bool argument_AverageStressStrainData = false;

            // Outputs
            [Archived(IsOptional = true)]
            private bool argument_WriteImplicitDataFiles = false;
            [Archived(IsOptional = true)]
            private bool argument_WriteDFNFiles = false;
            [Archived(IsOptional = true)]
            private bool argument_WriteToProjectFolder = true;
            [Archived(IsOptional = true)]
            private int argument_DFNFileType = 1;
            [Archived(IsOptional = true)]
            private bool argument_OutputAtEqualTimeIntervals = false;
            [Archived(IsOptional = true)]
            private bool argument_OutputCentrepoints = false;
            // Fracture connectivity and anisotropy index control parameters
            [Archived(IsOptional = true)]
            private bool argument_CalculateFractureConnectivityAnisotropy = true;
            [Archived(IsOptional = true)]
            private bool argument_CalculateFracturePorosity = true;
            [Archived(IsOptional = true)]
            private bool argument_CalculateBulkRockElasticTensors = false;

            // Fracture aperture control parameters
            [Archived(IsOptional = true)]
            private int argument_FractureApertureControl = 0;
            [Archived(IsOptional = true)]
            private double argument_HMin_UniformAperture = 0.0005;
            [Archived(IsOptional = true)]
            private double argument_HMax_UniformAperture = 0.0005;
            [Archived(IsOptional = true)]
            private double argument_HMin_SizeDependentApertureMultiplier = 0.00001;
            [Archived(IsOptional = true)]
            private double argument_HMax_SizeDependentApertureMultiplier = 0.00001;
            [Archived(IsOptional = true)]
            private double argument_DynamicApertureMultiplier = 1;
            [Archived(IsOptional = true)]
            private double argument_JRC = 10;
            [Archived(IsOptional = true)]
            private double argument_UCSratio = 2;
            [Archived(IsOptional = true)]
            private double argument_InitialNormalStress = 200000;
            [Archived(IsOptional = true)]
            private double argument_FractureNormalStiffness = 2.5E+9;
            [Archived(IsOptional = true)]
            private double argument_MaximumClosure = 0.0005;

            // Calculation control parameters
            // Set argument_NoFractureSets to 6 by default; however this value will only apply if argument_IncludeObliqueFracs is true;
            // otherwise argument_NoFractureSets will be overriden and the number of fracture sets will be set to 2
            [Archived(IsOptional = true)]
            private int argument_NoFractureSets = 6;
            [Archived(IsOptional = true)]
            private int argument_FractureMode = 0;
            // Set argument_CheckAlluFStressShadows to true by default; however this value will only apply if argument_IncludeObliqueFracs is true;
            // otherwise argument_CheckAlluFStressShadows will be overriden and the CheckAlluFStressShadows flag will be set to None
            [Archived(IsOptional = true)]
            private bool argument_CheckAlluFStressShadows = true;
            [Archived(IsOptional = true)]
            private double argument_AnisotropyCutoff = 1;
            [Archived(IsOptional = true)]
            private bool argument_AllowReverseFractures = false;
            [Archived(IsOptional = true)]
            private int argument_HorizontalUpscalingFactor = 1;
            // NB the maximum timestep duration must be supplied in SI units (seconds), not geological time units
            [Archived(IsOptional = true)]
            private double argument_MaxTSDuration = double.NaN;
            [Archived(IsOptional = true)]
            private double argument_Max_TS_MFP33_increase = 0.002;
            [Archived(IsOptional = true)]
            private double argument_MinimumImplicitMicrofractureRadius = 0.05;
            [Archived(IsOptional = true)]
            private int argument_No_r_bins = 10;
            // Calculation termination controls
            // NB the deformation duration must be supplied in SI units (seconds), not geological time units
            [Archived(IsOptional = true)]
            private double argument_DeformationDuration = double.NaN;
            [Archived(IsOptional = true)]
            private int argument_MaxNoTimesteps = 1000;
            [Archived(IsOptional = true)]
            private double argument_Historic_MFP33_TerminationRatio = double.NaN;
            [Archived(IsOptional = true)]
            private double argument_Active_MFP30_TerminationRatio = double.NaN;
            [Archived(IsOptional = true)]
            private double argument_Minimum_ClearZone_Volume = 0.01;
            // DFN geometry controls
            [Archived(IsOptional = true)]
            private bool argument_CropAtGridBoundary = true;
            [Archived(IsOptional = true)]
            private bool argument_LinkParallelFractures = true;
            [Archived(IsOptional = true)]
            private double argument_MaxConsistencyAngle = Math.PI / 4;
            [Archived(IsOptional = true)]
            private double argument_MinimumLayerThickness = 1;
            [Archived(IsOptional = true)]
            private bool argument_CreateTriangularFractureSegments = false;
            [Archived(IsOptional = true)]
            private double argument_ProbabilisticFractureNucleationLimit = double.NaN;
            [Archived(IsOptional = true)]
            private bool argument_PropagateFracturesInNucleationOrder = true;
            [Archived(IsOptional = true)]
            private int argument_SearchAdjacentGridblocks = 2;
            [Archived(IsOptional = true)]
            private double argument_MinimumExplicitMicrofractureRadius = double.NaN;
            [Archived(IsOptional = true)]
            private int argument_NoMicrofractureCornerpoints = 8;
#else
            // Main settings
            private string argument_ModelName = "New DFN";
            private Droid argument_Grid;
            private int argument_StartColI = 1;
            private int argument_StartRowJ = 1;
            private int argument_NoColsI = 1;
            private int argument_NoRowsJ = 1;
            private int argument_TopLayerK = 1;
            private int argument_BottomLayerK = 1;
            private double argument_EhminAzi_default = 0;
            private Droid argument_EhminAzi;
            // Unit conversion and labelling for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
            // Therefore strain rate units EhminRate and EhmaxRate are stored in geological time units, not SI units
            private double argument_EhminRate_default = -0.01;
            private Droid argument_EhminRate;
            private double argument_EhmaxRate_default = 0;
            private Droid argument_EhmaxRate;
            private bool argument_GenerateExplicitDFN = true;
            private int argument_NoIntermediateOutputs = 0;
            private bool argument_IncludeObliqueFracs = false;

            // Mechanical properties
            private double argument_YoungsMod_default = 10000000000;
            private Droid argument_YoungsMod;
            private double argument_PoissonsRatio_default = 0.25;
            private Droid argument_PoissonsRatio;
            private double argument_BiotCoefficient_default = 1;
            private Droid argument_BiotCoefficient;
            private double argument_FrictionCoefficient_default = 0.5;
            private Droid argument_FrictionCoefficient;
            private double argument_CrackSurfaceEnergy_default = 1000;
            private Droid argument_CrackSurfaceEnergy;
            // NB the rock strain relaxation and fracture relaxation time constants must be supplied in SI units (seconds), not geological time units
            private double argument_RockStrainRelaxation_default = 0;
            private Droid argument_RockStrainRelaxation;
            private double argument_FractureStrainRelaxation_default = 0;
            private Droid argument_FractureStrainRelaxation;
            // InitialMicrofractureDensity A is stored in project units rather than SI units, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
            // However it is initialised with a default value equivalent to 0.001fracs/m, since this is calibrated empirically
            // We therefore set up a private variable (in project units) and a private initialising variable (in SI units); the project unit variable will only be set when first called
            private double argument_InitialMicrofractureDensity_SI = 0.001;
            private double argument_InitialMicrofractureDensity_default = double.NaN;
            private Droid argument_InitialMicrofractureDensity;
            private double argument_InitialMicrofractureSizeDistribution_default = 2;
            private Droid argument_InitialMicrofractureSizeDistribution;
            private double argument_SubcriticalPropagationIndex_default = 10;
            private Droid argument_SubcriticalPropagationIndex;
            private double argument_CriticalPropagationRate = 2000;
            private bool argument_AverageMechanicalPropertyData = true;

            // Stress state
            private int argument_StressDistribution = 1;
            private double argument_DepthAtDeformation = double.NaN;
            private double argument_MeanOverlyingSedimentDensity = 2250;
            private double argument_FluidDensity = 1000;
            private double argument_InitialOverpressure = 0;
            private double argument_InitialStressRelaxation = 1;
            private bool argument_AverageStressStrainData = false;

            // Outputs
            private bool argument_WriteImplicitDataFiles = false;
            private bool argument_WriteDFNFiles = false;
            private bool argument_WriteToProjectFolder = true;
            private int argument_DFNFileType = 1;
            private bool argument_OutputAtEqualTimeIntervals = false;
            private bool argument_OutputCentrepoints = false;
            // Fracture connectivity and anisotropy index control parameters
            private bool argument_CalculateFractureConnectivityAnisotropy = true;
            private bool argument_CalculateFracturePorosity = true;
            private bool argument_CalculateBulkRockElasticTensors = false;

            // Fracture aperture control parameters
            private int argument_FractureApertureControl = 0;
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

            // Calculation control parameters
            // Set argument_NoFractureSets to 6 by default; however this value will only apply if argument_IncludeObliqueFracs is true;
            // otherwise argument_NoFractureSets will be overriden and the number of fracture sets will be set to 2
            private int argument_NoFractureSets = 6;
            private int argument_FractureMode = 0;
            // Set argument_CheckAlluFStressShadows to true by default; however this value will only apply if argument_IncludeObliqueFracs is true;
            // otherwise argument_CheckAlluFStressShadows will be overriden and the CheckAlluFStressShadows flag will be set to None
            private bool argument_CheckAlluFStressShadows = true;
            private double argument_AnisotropyCutoff = 1;
            private bool argument_AllowReverseFractures = false;
            private int argument_HorizontalUpscalingFactor = 1;
            // NB the maximum timestep duration must be supplied in SI units (seconds), not geological time units
            private double argument_MaxTSDuration = double.NaN;
            private double argument_Max_TS_MFP33_increase = 0.002;
            private double argument_MinimumImplicitMicrofractureRadius = 0.05;
            private int argument_No_r_bins = 10;
            // Calculation termination controls
            // NB the deformation duration must be supplied in SI units (seconds), not geological time units
            private double argument_DeformationDuration = double.NaN;
            private int argument_MaxNoTimesteps = 1000;
            private double argument_Historic_MFP33_TerminationRatio = double.NaN;
            private double argument_Active_MFP30_TerminationRatio = double.NaN;
            private double argument_Minimum_ClearZone_Volume = 0.01;
            // DFN geometry controls
            private bool argument_CropAtGridBoundary = true;
            private bool argument_LinkParallelFractures = true;
            private double argument_MaxConsistencyAngle = Math.PI / 4;
            private double argument_MinimumLayerThickness = 1;
            private bool argument_CreateTriangularFractureSegments = false;
            private double argument_ProbabilisticFractureNucleationLimit = double.NaN;
            private bool argument_PropagateFracturesInNucleationOrder = true;
            private int argument_SearchAdjacentGridblocks = 2;
            private double argument_MinimumExplicitMicrofractureRadius = double.NaN;
            private int argument_NoMicrofractureCornerpoints = 8;
#endif

            // Main settings
            [Description("Model name", "Model name")]
            public string Argument_ModelName
            {
                internal get { return this.argument_ModelName; }
                set { this.argument_ModelName = value; }
            }

            [Description("Grid to use for DFN model", "Grid to use for DFN model")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid Argument_Grid
            {
                internal get { return DataManager.Resolve(this.argument_Grid) as Grid; }
                set { this.argument_Grid = (value == null ? null : value.Droid); }
            }

            [Description("Index (I) of first column to model", "Index (I) of first column to model")]
            public int Argument_StartColI
            {
                internal get { return this.argument_StartColI; }
                set { this.argument_StartColI = value; }
            }

            [Description("Index (J) of first row to model", "Index (J) of first row to model")]
            public int Argument_StartRowJ
            {
                internal get { return this.argument_StartRowJ; }
                set { this.argument_StartRowJ = value; }
            }

            [Description("Number of columns to model", "Number of columns to model")]
            public int Argument_NoColsI
            {
                internal get { return this.argument_NoColsI; }
                set { this.argument_NoColsI = value; }
            }

            [Description("Number of rows to model", "Number of rows to model")]
            public int Argument_NoRowsJ
            {
                internal get { return this.argument_NoRowsJ; }
                set { this.argument_NoRowsJ = value; }
            }

            [Description("Index (K) of top grid layer in the fractured layer", "Index (K) of top grid layer in the fractured layer")]
            public int Argument_TopLayerK
            {
                internal get { return this.argument_TopLayerK; }
                set { this.argument_TopLayerK = value; }
            }

            [Description("Index (K) of bottom grid layer in the fractured layer", "Index (K) of bottom grid layer in the fractured layer")]
            public int Argument_BottomLayerK
            {
                internal get { return this.argument_BottomLayerK; }
                set { this.argument_BottomLayerK = value; }
            }

            [Description("Default azimuth of minimum (most tensile) horizontal strain (rad)", "Default value for azimuth of minimum (most tensile) horizontal strain (rad)")]
            public double Argument_EhminAzi_default
            {
                internal get { return this.argument_EhminAzi_default; }
                set { this.argument_EhminAzi_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Azimuth of minimum (most tensile) horizontal strain", "Azimuth of minimum (most tensile) horizontal strain")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Property Argument_EhminAzi
            {
                internal get { return DataManager.Resolve(this.argument_EhminAzi) as Property; }
                set { this.argument_EhminAzi = (value == null ? null : value.Droid); }
            }

            [Description("Default minimum horizontal strain rate (/s, tensile strain negative)", "Default value for minimum horizontal strain rate (/s, tensile strain negative)")]
            public double Argument_EhminRate_default
            {
                internal get { return this.argument_EhminRate_default; }
                set { this.argument_EhminRate_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Minimum horizontal strain rate (/s, tensile strain negative)", "Minimum horizontal strain rate (/s, tensile strain negative)")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Property Argument_EhminRate
            {
                internal get { return DataManager.Resolve(this.argument_EhminRate) as Property; }
                set { this.argument_EhminRate = (value == null ? null : value.Droid); }
            }

            [Description("Default maximum horizontal strain rate (/s, tensile strain negative)", "Default value for maximum horizontal strain rate (/s, tensile strain negative)")]
            public double Argument_EhmaxRate_default
            {
                internal get { return this.argument_EhmaxRate_default; }
                set { this.argument_EhmaxRate_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Maximum horizontal strain rate (/s, tensile strain negative)", "Maximum horizontal strain rate (/s, tensile strain negative)")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Property Argument_EhmaxRate
            {
                internal get { return DataManager.Resolve(this.argument_EhmaxRate) as Property; }
                set { this.argument_EhmaxRate = (value == null ? null : value.Droid); }
            }

            [Description("Generate explicit DFN?", "Generate explicit DFN? (if false, will only generate implicit fracture data)")]
            public bool Argument_GenerateExplicitDFN
            {
                internal get { return this.argument_GenerateExplicitDFN; }
                set { this.argument_GenerateExplicitDFN = value; }
            }

            [Description("Number of intermediate outputs", "Number of intermediate outputs; set to output data for intermediate stages of fracture growth")]
            public int Argument_NoIntermediateOutputs
            {
                internal get { return this.argument_NoIntermediateOutputs; }
                set { this.argument_NoIntermediateOutputs = value; }
            }

            [Description("Include oblique fractures?", "Include oblique fractures? (if true, this will override Argument_NoFractureSets to set the number of fracture sets to 2)")]
            public bool Argument_IncludeObliqueFracs
            {
                internal get { return this.argument_IncludeObliqueFracs; }
                set { this.argument_IncludeObliqueFracs = value; }
            }

            // Mechanical properties
            [Description("Default Young's Modulus (Pa)", "Default value for Young's Modulus (Pa)")]
            public double Argument_YoungsMod_default
            {
                internal get { return this.argument_YoungsMod_default; }
                set { this.argument_YoungsMod_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Young's Modulus", "Young's Modulus")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Property Argument_YoungsMod
            {
                internal get { return DataManager.Resolve(this.argument_YoungsMod) as Property; }
                set { this.argument_YoungsMod = (value == null ? null : value.Droid); }
            }

            [Description("Default Poisson's ratio", "Default value for Poisson's ratio")]
            public double Argument_PoissonsRatio_default
            {
                internal get { return this.argument_PoissonsRatio_default; }
                set { this.argument_PoissonsRatio_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Poisson's ratio", "Poisson's ratio")]
            public Property Argument_PoissonsRatio
            {
                internal get { return DataManager.Resolve(this.argument_PoissonsRatio) as Property; }
                set { this.argument_PoissonsRatio = (value == null ? null : value.Droid); }
            }

            [Description("Default Biot's coefficient", "Default value for Biot's coefficient")]
            public double Argument_BiotCoefficient_default
            {
                internal get { return this.argument_BiotCoefficient_default; }
                set { this.argument_BiotCoefficient_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Biot's coefficient", "Biot's coefficient")]
            public Property Argument_BiotCoefficient
            {
                internal get { return DataManager.Resolve(this.argument_BiotCoefficient) as Property; }
                set { this.argument_BiotCoefficient = (value == null ? null : value.Droid); }
            }

            [Description("Default friction coefficient", "Default value for friction coefficient")]
            public double Argument_FrictionCoefficient_default
            {
                internal get { return this.argument_FrictionCoefficient_default; }
                set { this.argument_FrictionCoefficient_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Friction coefficient", "Friction coefficient")]
            public Property Argument_FrictionCoefficient
            {
                internal get { return DataManager.Resolve(this.argument_FrictionCoefficient) as Property; }
                set { this.argument_FrictionCoefficient = (value == null ? null : value.Droid); }
            }

            [Description("Default crack surface energy (J/m2)", "Default value for crack surface energy (J/m2)")]
            public double Argument_CrackSurfaceEnergy_default
            {
                internal get { return this.argument_CrackSurfaceEnergy_default; }
                set { this.argument_CrackSurfaceEnergy_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Crack surface energy", "Crack surface energy")]
            public Property Argument_CrackSurfaceEnergy
            {
                internal get { return DataManager.Resolve(this.argument_CrackSurfaceEnergy) as Property; }
                set { this.argument_CrackSurfaceEnergy = (value == null ? null : value.Droid); }
            }

            [Description("Default rock strain relaxation time constant (s); set to 0 for no rock strain relaxation", "Default rock strain relaxation time constant (s); set to 0 for no rock strain relaxation")]
            public double Argument_RockStrainRelaxation_default
            {
                internal get { return this.argument_RockStrainRelaxation_default; }
                set { this.argument_RockStrainRelaxation_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Rock strain relaxation time constant (s)", "Rock strain relaxation time constant (s)")]
            public Property Argument_RockStrainRelaxation
            {
                internal get { return DataManager.Resolve(this.argument_RockStrainRelaxation) as Property; }
                set { this.argument_RockStrainRelaxation = (value == null ? null : value.Droid); }
            }

            [Description("Default fracture strain relaxation time constant (s); set to 0 for no fracture strain relaxation", "Default fracture strain relaxation time constant (s); set to 0 for no fracture strain relaxation")]
            public double Argument_FractureStrainRelaxation_default
            {
                internal get { return this.argument_FractureStrainRelaxation_default; }
                set { this.argument_FractureStrainRelaxation_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Fracture strain relaxation time constant (s)", "Fracture strain relaxation time constant (s)")]
            public Property Argument_FractureStrainRelaxation
            {
                internal get { return DataManager.Resolve(this.argument_FractureStrainRelaxation) as Property; }
                set { this.argument_FractureStrainRelaxation = (value == null ? null : value.Droid); }
            }

            [Description("Default initial microfracture density; NB must use project units", "Default value for initial microfracture density; NB must use project units")]
            public double Argument_InitialMicrofractureDensity_default
            {
                // InitialMicrofractureDensity A is stored in project units rather than SI units, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
                // However it is initialised with a default value equivalent to 0.001(fracs>1m radius)/m3 (=0.001fracs/m), since this is calibrated empirically
                // We therefore set up a private variable (in project units) and a private initialising variable (in SI units); the project unit variable will only be set when first called
                // NB Length units are taken from the PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth template rather than the PetrelProject.WellKnownTemplates.GeometricalGroup.Distance template,
                // because with a UTM coordinate reference system, the Distance template may be set to metric when the project length units are in ft
                internal get
                {
                    if (double.IsNaN(this.argument_InitialMicrofractureDensity_default))
                    {
                        double toProjectUnits_InitialMicrofractureDensity = Math.Pow(PetrelUnitSystem.ConvertFromUI(PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth, 1), 3 - Argument_InitialMicrofractureSizeDistribution_default);
                        this.argument_InitialMicrofractureDensity_default = this.argument_InitialMicrofractureDensity_SI * toProjectUnits_InitialMicrofractureDensity;
                    }
                    return this.argument_InitialMicrofractureDensity_default;
                }
                set { this.argument_InitialMicrofractureDensity_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Initial microfracture density (project units)", "Initial microfracture density (project units)")]
            public Property Argument_InitialMicrofractureDensity
            {
                internal get { return DataManager.Resolve(this.argument_InitialMicrofractureDensity) as Property; }
                set { this.argument_InitialMicrofractureDensity = (value == null ? null : value.Droid); }
            }

            [Description("Default initial microfracture size distribution coefficient", "Default value for initial microfracture size distribution coefficient")]
            public double Argument_InitialMicrofractureSizeDistribution_default
            {
                internal get { return this.argument_InitialMicrofractureSizeDistribution_default; }
                set { this.argument_InitialMicrofractureSizeDistribution_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Initial microfracture size distribution coefficient", "Initial microfracture size distribution coefficient")]
            public Property Argument_InitialMicrofractureSizeDistribution
            {
                internal get { return DataManager.Resolve(this.argument_InitialMicrofractureSizeDistribution) as Property; }
                set { this.argument_InitialMicrofractureSizeDistribution = (value == null ? null : value.Droid); }
            }

            [Description("Default subcritical fracture propagation index", "Default value for subcritical fracture propagation index")]
            public double Argument_SubcriticalPropagationIndex_default
            {
                internal get { return this.argument_SubcriticalPropagationIndex_default; }
                set { this.argument_SubcriticalPropagationIndex_default = value; }
            }

            [OptionalInWorkflow]
            [Description("Subcritical fracture propagation index", "Subcritical fracture propagation index")]
            public Property Argument_SubcriticalPropagationIndex
            {
                internal get { return DataManager.Resolve(this.argument_SubcriticalPropagationIndex) as Property; }
                set { this.argument_SubcriticalPropagationIndex = (value == null ? null : value.Droid); }
            }

            [Description("Critical fracture propagation rate (m/s)", "Critical fracture propagation rate (m/s)")]
            public double Argument_CriticalPropagationRate
            {
                internal get { return this.argument_CriticalPropagationRate; }
                set { this.argument_CriticalPropagationRate = value; }
            }

            [Description("Average mechanical properties across all cells? (if false, will use value of top middle cell)", "Average mechanical properties across all cells? (if false, will use value of the uppermost middle cell that contains valid data)")]
            public bool Argument_AverageMechanicalPropertyData
            {
                internal get { return this.argument_AverageMechanicalPropertyData; }
                set { this.argument_AverageMechanicalPropertyData = value; }
            }

            // Stress state
            [Description("Stress distribution scenario: 0 = Evenly distributed stress, 1 = Stress shadow", "Stress distribution scenario: 0 = Evenly distributed stress, 1 = Stress shadow")]
            public int Argument_StressDistribution
            {
                internal get { return this.argument_StressDistribution; }
                set { this.argument_StressDistribution = value; }
            }

            [OptionalInWorkflow]
            [Description("Depth at time of deformation (m); leave blank to use current depth", "Depth at time of deformation (m); leave blank to use current depth")]
            public double Argument_DepthAtDeformation
            {
                internal get { return this.argument_DepthAtDeformation; }
                set { this.argument_DepthAtDeformation = value; }
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

            [Description("Initial fluid overpressure (Pa)", "Initial fluid overpressure (Pa)")]
            public double Argument_InitialOverpressure
            {
                internal get { return this.argument_InitialOverpressure; }
                set { this.argument_InitialOverpressure = value; }
            }

            [Description("Initial stress relaxation", "Initial stress relaxation")]
            public double Argument_InitialStressRelaxation
            {
                internal get { return this.argument_InitialStressRelaxation; }
                set { this.argument_InitialStressRelaxation = value; }
            }

            [Description("Average stress/strain state across all cells? (if false, will use value of top middle cell)", "Average stress/strain state across all cells? (if false, will use value of the uppermost middle cell that contains valid data)")]
            public bool Argument_AverageStressStrainData
            {
                internal get { return this.argument_AverageStressStrainData; }
                set { this.argument_AverageStressStrainData = value; }
            }

            // Outputs
            [Description("Write implicit fracture data to file?", "Write implicit fracture data to file? (will generate 1 file per cell stack)")]
            public bool Argument_WriteImplicitDataFiles
            {
                internal get { return this.argument_WriteImplicitDataFiles; }
                set { this.argument_WriteImplicitDataFiles = value; }
            }

            [Description("Write explicit fracture data to file?", "Write explicit fracture data to file?")]
            public bool Argument_WriteDFNFiles
            {
                internal get { return this.argument_WriteDFNFiles; }
                set { this.argument_WriteDFNFiles = value; }
            }

            [Description("Write output files to project folder?", "Write output files to project folder?")]
            public bool Argument_WriteToProjectFolder
            {
                internal get { return this.argument_WriteToProjectFolder; }
                set { this.argument_WriteToProjectFolder = value; }
            }

            [Description("File type for explicit data: 0 = ASCII, 1 = FAB", "File type for explicit data: 0 = ASCII, 1 = FAB")]
            public int Argument_DFNFileType
            {
                internal get { return this.argument_DFNFileType; }
                set { this.argument_DFNFileType = value; }
            }

            [Description("Output intermediate results by time? (if false, will output intermediate results by approximate fracture area)", "Output intermediate results by time? (if false, will output intermediate results by approximate fracture area)")]
            public bool Argument_OutputAtEqualTimeIntervals
            {
                internal get { return this.argument_OutputAtEqualTimeIntervals; }
                set { this.argument_OutputAtEqualTimeIntervals = value; }
            }

            [Description("Output fracture centrelines as polylines?", "Output fracture centrelines as polylines?")]
            public bool Argument_OutputCentrepoints
            {
                internal get { return this.argument_OutputCentrepoints; }
                set { this.argument_OutputCentrepoints = value; }
            }

            // Fracture connectivity and anisotropy index control parameters
            [Description("Calculate fracture connectivity and anisotropy?", "Calculate fracture connectivity and anisotropy?")]
            public bool Argument_CalculateFractureConnectivityAnisotropy
            {
                internal get { return this.argument_CalculateFractureConnectivityAnisotropy; }
                set { this.argument_CalculateFractureConnectivityAnisotropy = value; }
            }

            [Description("Calculate fracture porosity?", "Calculate fracture porosity?")]
            public bool Argument_CalculateFracturePorosity
            {
                internal get { return this.argument_CalculateFracturePorosity; }
                set { this.argument_CalculateFracturePorosity = value; }
            }

            [Description("Calculate bulk rock elastic tensors?", "Calculate bulk rock compliance and stiffness tensors, taking into account the fractures")]
            public bool Argument_CalculateBulkRockElasticTensors
            {
                internal get { return this.argument_CalculateBulkRockElasticTensors; }
                set { this.argument_CalculateBulkRockElasticTensors = value; }
            }

            // Fracture aperture control parameters
            [Description("Method used to determine fracture aperture: 0 = Uniform Aperture, 1 = Size Dependent, 2 = Dynamic Aperture, 3 = Barton-Bandis", "Method used to determine fracture aperture: 0 = Uniform Aperture, 1 = Size Dependent, 2 = Dynamic Aperture, 3 = Barton-Bandis")]
            public int Argument_FractureApertureControl
            {
                internal get { return this.argument_FractureApertureControl; }
                set { this.argument_FractureApertureControl = value; }
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

            [Description("Barton-Bandis Fracture Aperture: Stiffness normal to the fracture, at initial normal stress (Pa/m)", "Barton-Bandis Fracture Aperture: Stiffness normal to the fracture, at initial normal stress (Pa/m)")]
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

            // Calculation control parameters
            [Description("Number of fracture sets", "Number of fracture sets: set to 2 for two fracture sets orthogonal to ehmin and ehmax; set to 6 or more for oblique fractures")]
            public int Argument_NoFractureSets
            {
                internal get { return this.argument_NoFractureSets; }
                set { this.argument_NoFractureSets = value; }
            }

            [Description("Fracture mode", "Fracture mode (if 0, will select energetically optimal mode)")]
            public int Argument_FractureMode
            {
                internal get { return this.argument_FractureMode; }
                set { this.argument_FractureMode = value; }
            }

            [Description("Flag to check microfractures against stress shadows of all macrofractures, regardless of set", "Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set")]
            public bool Argument_CheckAlluFStressShadows
            {
                internal get { return this.argument_CheckAlluFStressShadows; }
                set { this.argument_CheckAlluFStressShadows = value; }
            }

            [Description("Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes", "Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes")]
            public double Argument_AnisotropyCutoff
            {
                internal get { return this.argument_AnisotropyCutoff; }
                set { this.argument_AnisotropyCutoff = value; }
            }

            [Description("Allow reverse fractures", "Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow")]
            public bool Argument_AllowReverseFractures
            {
                internal get { return this.argument_AllowReverseFractures; }
                set { this.argument_AllowReverseFractures = value; }
            }

            [Description("Horizontal upscaling factor", "Horizontal upscaling factor")]
            public int Argument_HorizontalUpscalingFactor
            {
                internal get { return this.argument_HorizontalUpscalingFactor; }
                set { this.argument_HorizontalUpscalingFactor = value; }
            }

            [OptionalInWorkflow]
            [Description("Max timestep duration (s); blank for no limit", "Maximum duration for individual timesteps (s); leave blank for no limit to timestep duration")]
            public double Argument_MaxTSDuration
            {
                internal get { return this.argument_MaxTSDuration; }
                set { this.argument_MaxTSDuration = value; }
            }

            [Description("Max increase in MFP33 per timestep", "Maximum increase in macrofracture volumetric ratio allowed in one timestep")]
            public double Argument_Max_TS_MFP33_increase
            {
                internal get { return this.argument_Max_TS_MFP33_increase; }
                set { this.argument_Max_TS_MFP33_increase = value; }
            }

            [OptionalInWorkflow]
            [Description("Minimum implicit microfracture radius (m)", "Minimum implicit microfracture radius: Cut-off radius for microfractures included in implicit fracture density data (m)")]
            public double Argument_MinimumImplicitMicrofractureRadius
            {
                internal get { return this.argument_MinimumImplicitMicrofractureRadius; }
                set { this.argument_MinimumImplicitMicrofractureRadius = value; }
            }

            [Description("Number of bins used to integrate uFP32", "Number of bins used in numerical integration of uFP32 - increase this to increase accuracy of the microfracture porosity calculation at the expense of runtime")]
            public int Argument_No_r_bins
            {
                internal get { return this.argument_No_r_bins; }
                set { this.argument_No_r_bins = value; }
            }

            // Calculation termination controls
            [OptionalInWorkflow]
            [Description("Max deformation episode duration (s)", "Maximum deformation episode duration (s); if blank, will continue until fracture saturation is reached")]
            public double Argument_DeformationDuration
            {
                internal get { return this.argument_DeformationDuration; }
                set { this.argument_DeformationDuration = value; }
            }

            [Description("Max number of timesteps", "Maximum number of timesteps")]
            public int Argument_MaxNoTimesteps
            {
                internal get { return this.argument_MaxNoTimesteps; }
                set { this.argument_MaxNoTimesteps = value; }
            }

            [OptionalInWorkflow]
            [Description("Min ratio of current to peak active MFP33", "Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive")]
            public double Argument_Historic_MFP33_TerminationRatio
            {
                internal get { return this.argument_Historic_MFP33_TerminationRatio; }
                set { this.argument_Historic_MFP33_TerminationRatio = value; }
            }

            [OptionalInWorkflow]
            [Description("Min ratio of active to total MFP30", "Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive")]
            public double Argument_Active_MFP30_TerminationRatio
            {
                internal get { return this.argument_Active_MFP30_TerminationRatio; }
                set { this.argument_Active_MFP30_TerminationRatio = value; }
            }

            [OptionalInWorkflow]
            [Description("Minimum clear zone volume", "Minimum clear zone volume; if the clear zone volume falls below this value, the fracture set will be considered inactive")]
            public double Argument_Minimum_ClearZone_Volume
            {
                internal get { return this.argument_Minimum_ClearZone_Volume; }
                set { this.argument_Minimum_ClearZone_Volume = value; }
            }

            // DFN geometry controls
            [Description("Crop fractures at outer boundary of selected cells?", "Crop fractures at outer boundary of selected cells?")]
            public bool Argument_CropAtGridBoundary
            {
                internal get { return this.argument_CropAtGridBoundary; }
                set { this.argument_CropAtGridBoundary = value; }
            }

            [Description("Create relay segments?", "Create relay segments to link parallel fractures terminated due to stress shadow interaction?")]
            public bool Argument_LinkParallelFractures
            {
                internal get { return this.argument_LinkParallelFractures; }
                set { this.argument_LinkParallelFractures = value; }
            }

            [Description("Maximum consistency angle (rad)", "Maximum consistency angle (rad); if variation in fracture strike across gridblock boundary is greater than this, the algorithm will search for another fracture set")]
            public double Argument_MaxConsistencyAngle
            {
                internal get { return this.argument_MaxConsistencyAngle; }
                set { this.argument_MaxConsistencyAngle = value; }
            }

            [Description("Layer thickness cutoff (m)", "Layer thickness cutoff (m): explicit DFN will not be calculated for gridblocks thinner than this value; set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers")]
            public double Argument_MinimumLayerThickness
            {
                internal get { return this.argument_MinimumLayerThickness; }
                set { this.argument_MinimumLayerThickness = value; }
            }

            [Description("Create triangular fracture segments?", "Flag to create triangular instead of quadrilateral fracture segments; will increase the total number of segments but generation algorithm may run faster")]
            public bool Argument_CreateTriangularFractureSegments
            {
                internal get { return this.argument_CreateTriangularFractureSegments; }
                set { this.argument_CreateTriangularFractureSegments = value; }
            }

            [OptionalInWorkflow]
            [Description("Probabilistic fracture nucleation limit", "Probabilistic fracture nucleation limit: allows fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small; set to 0 to disable probabilistic fracture nucleation, leave blank for automatic probabilistic fracture nucleation")]
            public double Argument_ProbabilisticFractureNucleationLimit
            {
                internal get { return this.argument_ProbabilisticFractureNucleationLimit; }
                set { this.argument_ProbabilisticFractureNucleationLimit = value; }
            }

            [Description("Propagate fractures in order of nucleation?", "Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set")]
            public bool Argument_PropagateFracturesInNucleationOrder
            {
                internal get { return this.argument_PropagateFracturesInNucleationOrder; }
                set { this.argument_PropagateFracturesInNucleationOrder = value; }
            }

            [Description("Search adjacent gridblocks for stress shadow interaction?", "Flag to control whether to search adjacent gridblocks for stress shadow interaction: 0 = None, 1 = All, 2 = Automatic (determined independently for each gridblock based on the gridblock geometry)")]
            public int Argument_SearchAdjacentGridblocks
            {
                internal get { return this.argument_SearchAdjacentGridblocks; }
                set { this.argument_SearchAdjacentGridblocks = value; }
            }

            [OptionalInWorkflow]
            [Description("Minimum explicit microfracture radius (m); leave blank to generate no explicit microfractures", "Minimum explicit microfracture radius (m); leave blank to generate no explicit microfractures")]
            public double Argument_MinimumExplicitMicrofractureRadius
            {
                internal get { return this.argument_MinimumExplicitMicrofractureRadius; }
                set { this.argument_MinimumExplicitMicrofractureRadius = value; }
            }

            [Description("Number of microfracture cornerpoints", "Number of cornerpoints in explicit microfractures")]
            public int Argument_NoMicrofractureCornerpoints
            {
                internal get { return this.argument_NoMicrofractureCornerpoints; }
                set { this.argument_NoMicrofractureCornerpoints = value; }
            }

            /// <summary>
            /// Reset all arguments to default values
            /// </summary>
            public void ResetDefaults()
            {
                // Main settings
                argument_ModelName = "New DFN";
                argument_Grid = null;
                argument_StartColI = 1;
                argument_StartRowJ = 1;
                argument_NoColsI = 1;
                argument_NoRowsJ = 1;
                argument_TopLayerK = 1;
                argument_BottomLayerK = 1;
                argument_EhminAzi_default = 0;
                argument_EhminAzi = null;
                // Unit conversion and labelling for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
                // Therefore strain rate units EhminRate and EhmaxRate are stored in geological time units, not SI units
                argument_EhminRate_default = -0.01;
                argument_EhminRate = null;
                argument_EhmaxRate_default = 0;
                argument_EhmaxRate = null;
                argument_GenerateExplicitDFN = true;
                argument_NoIntermediateOutputs = 0;
                argument_IncludeObliqueFracs = false;

                // Mechanical properties
                argument_YoungsMod_default = 10000000000;
                argument_YoungsMod = null;
                argument_PoissonsRatio_default = 0.25;
                argument_PoissonsRatio = null;
                argument_BiotCoefficient_default = 1;
                argument_BiotCoefficient = null;
                argument_FrictionCoefficient_default = 0.5;
                argument_FrictionCoefficient = null;
                argument_CrackSurfaceEnergy_default = 1000;
                argument_CrackSurfaceEnergy = null;
                // NB the rock strain relaxation and fracture relaxation time constants must be supplied in SI units (seconds), not geological time units
                argument_RockStrainRelaxation_default = 0;
                argument_RockStrainRelaxation = null;
                argument_FractureStrainRelaxation_default = 0;
                argument_FractureStrainRelaxation = null;
                // InitialMicrofractureDensity A is stored in project units rather than SI units, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
                // However it is initialised with a default value equivalent to 0.001fracs/m, since this is calibrated empirically
                // We therefore set up a private variable (in project units) and a private initialising variable (in SI units); the project unit variable will only be set when first called
                argument_InitialMicrofractureDensity_SI = 0.001;
                argument_InitialMicrofractureDensity_default = double.NaN;
                argument_InitialMicrofractureDensity = null;
                argument_InitialMicrofractureSizeDistribution_default = 2;
                argument_InitialMicrofractureSizeDistribution = null;
                argument_SubcriticalPropagationIndex_default = 10;
                argument_SubcriticalPropagationIndex = null;
                argument_CriticalPropagationRate = 2000;
                argument_AverageMechanicalPropertyData = true;

                // Stress state
                argument_StressDistribution = 1;
                argument_DepthAtDeformation = double.NaN;
                argument_MeanOverlyingSedimentDensity = 2250;
                argument_FluidDensity = 1000;
                argument_InitialOverpressure = 0;
                argument_InitialStressRelaxation = 1;
                argument_AverageStressStrainData = false;

                // Outputs
                argument_WriteImplicitDataFiles = false;
                argument_WriteDFNFiles = false;
                argument_WriteToProjectFolder = true;
                argument_DFNFileType = 1;
                argument_OutputAtEqualTimeIntervals = false;
                argument_OutputCentrepoints = false;
                // Fracture connectivity and anisotropy index control parameters
                argument_CalculateFractureConnectivityAnisotropy = true;
                argument_CalculateFracturePorosity = true;
                argument_CalculateBulkRockElasticTensors = false;

                // Fracture aperture control parameters
                argument_FractureApertureControl = 0;
                argument_HMin_UniformAperture = 0.0005;
                argument_HMax_UniformAperture = 0.0005;
                argument_HMin_SizeDependentApertureMultiplier = 0.00001;
                argument_HMax_SizeDependentApertureMultiplier = 0.00001;
                argument_DynamicApertureMultiplier = 1;
                argument_JRC = 10;
                argument_UCSratio = 2;
                argument_InitialNormalStress = 200000;
                argument_FractureNormalStiffness = 2.5E+9;
                argument_MaximumClosure = 0.0005;

                // Calculation control parameters
                // Set argument_NoFractureSets to 6 by default; however this value will only apply if argument_IncludeObliqueFracs is true;
                // otherwise argument_NoFractureSets will be overriden and the number of fracture sets will be set to 2
                argument_NoFractureSets = 6;
                argument_FractureMode = 0;
                // Set argument_CheckAlluFStressShadows to true by default; however this value will only apply if argument_IncludeObliqueFracs is true;
                // otherwise argument_CheckAlluFStressShadows will be overriden and the CheckAlluFStressShadows flag will be set to None
                argument_CheckAlluFStressShadows = true;
                argument_AnisotropyCutoff = 1;
                argument_AllowReverseFractures = false;
                argument_HorizontalUpscalingFactor = 1;
                // NB the maximum timestep duration must be supplied in SI units (seconds), not geological time units
                argument_MaxTSDuration = double.NaN;
                argument_Max_TS_MFP33_increase = 0.002;
                argument_MinimumImplicitMicrofractureRadius = 0.05;
                argument_No_r_bins = 10;
                // Calculation termination controls
                // NB the deformation duration must be supplied in SI units (seconds), not geological time units
                argument_DeformationDuration = double.NaN;
                argument_MaxNoTimesteps = 1000;
                argument_Historic_MFP33_TerminationRatio = double.NaN;
                argument_Active_MFP30_TerminationRatio = double.NaN;
                argument_Minimum_ClearZone_Volume = 0.01;
                // DFN geometry controls
                argument_CropAtGridBoundary = true;
                argument_LinkParallelFractures = true;
                argument_MaxConsistencyAngle = Math.PI / 4;
                argument_MinimumLayerThickness = 1;
                argument_CreateTriangularFractureSegments = false;
                argument_ProbabilisticFractureNucleationLimit = double.NaN;
                argument_PropagateFracturesInNucleationOrder = true;
                argument_SearchAdjacentGridblocks = 2;
                argument_MinimumExplicitMicrofractureRadius = double.NaN;
                argument_NoMicrofractureCornerpoints = 8;
            }

#if MANAGED_PERSISTENCE
            // IIdentifiable Members
            [Archived]
            private Droid arguments_Droid;
            // Set IgnoreInWorkflow so it will not appear in the default UI
            // Any custom UI would just ignore this
            [IgnoreInWorkflow]
            public Droid Droid { get { return arguments_Droid; } }

            // IDisposable Members
            public void Dispose()
            {
                if (dataSource != null)
                    dataSource.RemoveItem(arguments_Droid);
            }
#endif
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
            //get { return Description.Name; }
            get { return "DFN Generator"; }
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
            //get { return PetrelImages.Modules; }
            get { return PetrelImages.PolylineSet; }
            private set
            {
                // TODO: implement set
                this.RaiseImageChanged();
            }
        }
#endregion

#region IDescriptionSource Members

        /// <summary>
        /// Gets the description of the DFNGenerator
        /// </summary>
        public IDescription Description
        {
            get { return DFNGeneratorDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the DFNGenerator.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class DFNGeneratorDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private static DFNGeneratorDescription instance = new DFNGeneratorDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static DFNGeneratorDescription Instance
            {
                get { return instance; }
            }

#region IDescription Members

            /// <summary>
            /// Gets the name of DFNGenerator
            /// </summary>
            public string Name
            {
                get { return "DFNGenerator"; }
            }
            /// <summary>
            /// Gets the short description of DFNGenerator
            /// </summary>
            public string ShortDescription
            {
                get { return "Petrel UI for DFN Generator module"; }
            }
            /// <summary>
            /// Gets the detailed description of DFNGenerator
            /// </summary>
            public string Description
            {
                get { return "Petrel UI for DFN Generator module"; }
            }

#endregion
        }
#endregion

        public class UIFactory : WorkflowEditorUIFactory
        {
            /// <summary>
            /// This method creates the dialog UI for the given workstep, arguments
            /// and context.
            /// </summary>
            /// <param name="workstep">the workstep instance</param>
            /// <param name="argumentPackage">the arguments to pass to the UI</param>
            /// <param name="context">the underlying context in which the UI is being used</param>
            /// <returns>a Windows.Forms.Control to edit the argument package with</returns>
            protected override System.Windows.Forms.Control CreateDialogUICore(Workstep workstep, object argumentPackage, WorkflowContext context)
            {
                return new DFNGeneratorUI((DFNGenerator)workstep, (Arguments)argumentPackage, context);
            }
        }
    }
}