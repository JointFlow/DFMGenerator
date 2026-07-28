// Switch this flag off to use hardcoded values for all parameters
// This should be done for debugging only
// The flag should be set to generate release versions of the standalone code
#define READINPUTFROMFILE
// Set these flags to output detailed information on input parameters and properties for each gridblock
// Use for debugging only; will significantly increase runtime
//#define DEBUG_FRAC_INPUT
//#define DEBUG_FRAC_OUTPUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DFMGenerator_SharedCode;
using DFMGenerator_DataTransfer;

namespace DFMGenerator_GRDECL
{
    class Program
    {
        static void Main(string[] args)
        {
#if READINPUTFROMFILE
            // Find input file; if it does not exist then create a dummy file and abort
            string inputfile_name;
            if (args.Length > 0)
                inputfile_name = args[0];
            else
                inputfile_name = "DFMGenerator_GRDECL_configuration.txt";
            if (!File.Exists(inputfile_name))
            {
                StreamWriter input_file = new StreamWriter(inputfile_name);

                input_file.WriteLine(string.Format("% Model {0}\n", inputfile_name));
                input_file.WriteLine("% Replace defaults with required values");
                input_file.WriteLine("% Use % for comment lines");
                input_file.WriteLine();

                input_file.WriteLine("% Main properties");
                input_file.WriteLine("% Model name, used to name output files; if not specified, will be taken from the input file name");
                input_file.WriteLine("%ModelName NAME");
                input_file.WriteLine("% Geometry and grid property data");
                input_file.WriteLine("% This should be a GRDECL format file");
                input_file.WriteLine("GridFileName Gridfile.GRDECL");
                input_file.WriteLine("% Subset of rows and columns from the GRDECL grid to include in the fracture grid (indexed from 1)");
                input_file.WriteLine("% Following GRDECL format, columns are indexed from west to east but rows are indexed in reverse order, from north to south");
                input_file.WriteLine("% Set to -1 to include all rows and columns");
                input_file.WriteLine("StartColumnI -1");
                input_file.WriteLine("EndColumnI -1");
                input_file.WriteLine("StartRowJ -1");
                input_file.WriteLine("EndRowJ -1");
                input_file.WriteLine("% Subset of layers from the GRDECL grid to include in the fracture grid (indexed from 1)");
                input_file.WriteLine("% Following GRDECL format, layers are indexed from top to bottom");
                input_file.WriteLine("% Set to -1 to include all layers");
                input_file.WriteLine("TopLayerK -1");
                input_file.WriteLine("BottomLayerK -1");
                input_file.WriteLine("% Horizontal upscaling factor - used to amalgamate multiple GRDECL grid cells into one fracture gridblock");
                input_file.WriteLine("HorizontalUpscalingFactor 1");
                input_file.WriteLine("% Vertical upscaling factor - used to amalgamate multiple shadow grid layers into one fracture grid layer");
                input_file.WriteLine("% Set to 0 to amalgamate all selected shadow grid layers into a single fracture grid layer");
                input_file.WriteLine("% Set to 1 to create a fracture grid layer for each selected shadow grid layer");
                input_file.WriteLine("VerticalUpscalingFactor 1");
                input_file.WriteLine("% Time units used in input load rates, time limits and strain relaxation time constants");
                input_file.WriteLine("% Set time units to ma, year or second");
                input_file.WriteLine("ModelTimeUnits ma");
                input_file.WriteLine("% Deformation load");
                input_file.WriteLine("% Each deformation episode can be assigned a name; if this is not specified, a default name will be generated");
                input_file.WriteLine("%DeformationEpisodeName Deformation_Episode_1");
                input_file.WriteLine("% Minimum strain orientatation (i.e. direction of maximum extension) in radians, clockwise from North");
                input_file.WriteLine("DefaultEhminAzi 0");
                input_file.WriteLine("%EhminAziProperty PROPERTYNAME");
                input_file.WriteLine("% Strain rates; units determined by ModelTimeUnits (i.e. strain/s, strain/year or strain/ma)");
                input_file.WriteLine("% Set to negative value for extensional strain - this is necessary in at least one direction to generate fractures");
                input_file.WriteLine("% With no strain relaxation, strain rate will control rate of horizontal stress increase");
                input_file.WriteLine("% With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant horizontal stress");
                input_file.WriteLine("% EhminRate is most tensile (i.e. most negative) horizontal strain rate");
                input_file.WriteLine("DefaultEhminRate -0.01");
                input_file.WriteLine("%EhminRateProperty PROPERTYNAME");
                input_file.WriteLine("% Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern");
                input_file.WriteLine("DefaultEhmaxRate 0");
                input_file.WriteLine("%EhmaxRateProperty PROPERTYNAME");
                input_file.WriteLine("% Fluid pressure, thermal and uplift loads");
                input_file.WriteLine("% Rate of increase of fluid overpressure (Pa/ModelTimeUnit)");
                input_file.WriteLine("DefaultAppliedOverpressureRate 0");
                input_file.WriteLine("%AppliedOverpressureRateProperty PROPERTYNAME");
                input_file.WriteLine("% Rate of in situ temperature change (not including cooling due to uplift) (degK/ModelTimeUnit)");
                input_file.WriteLine("DefaultAppliedTemperatureChange 0");
                input_file.WriteLine("%AppliedTemperatureChangeProperty PROPERTYNAME");
                input_file.WriteLine("% Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/ModelTimeUnit)");
                input_file.WriteLine("DefaultAppliedUpliftRate 0");
                input_file.WriteLine("%AppliedUpliftRateProperty PROPERTYNAME");
                input_file.WriteLine("% Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)");
                input_file.WriteLine("StressArchingFactor 0");
                input_file.WriteLine("% Parameters to specify dynamic deformation loads, defined in terms of stress rather than strain");
                input_file.WriteLine("% If these parameters are specified, they will override the strain, fluid overpressure, thermal and uplift loads");
                input_file.WriteLine("% If only the ZZ stress component is specified, it will be used to calculate the lithostatic stress instead of depth and overburden density but the strain and thermal loads will used to define horizontal stress");
                input_file.WriteLine("% If the XX, YY, ZZ and XY stress components are specified, a horizontally symmetric stress tensor will be generated (YZ and ZX components set to 0) which will override the strain, thermal and uplift loads");
                input_file.WriteLine("% If all 6 stress components are specified, a full stress tensor will be generated which will override the strain, thermal and uplift loads");
                input_file.WriteLine("% If the fluid pressure is specified this will replace the hydrostatic fluid pressure and override the fluid overpressure load");
                input_file.WriteLine("% Dynamic loads should be specified in Pa as either AbsoluteStress, TerzaghiEffectiveStress or BiotEffectiveStress tensors, depending on the StressLoadDefinition flag");
                input_file.WriteLine("%StressLoadDefinition TerzaghiEffectiveStress");
                input_file.WriteLine("%StressXXProperty PROPERTYNAME");
                input_file.WriteLine("%StressYYProperty PROPERTYNAME");
                input_file.WriteLine("%StressZZProperty PROPERTYNAME");
                input_file.WriteLine("%StressXYProperty PROPERTYNAME");
                input_file.WriteLine("%StressYZProperty PROPERTYNAME");
                input_file.WriteLine("%StressZXProperty PROPERTYNAME");
                input_file.WriteLine("%FluidPressureProperty PROPERTYNAME");
                input_file.WriteLine("% Duration of the deformation episode; set to -1 to continue until fracture saturation is reached");
                input_file.WriteLine("% Units are determined by ModelTimeUnits setting");
                input_file.WriteLine("DeformationEpisodeDuration -1");
                input_file.WriteLine("% To add additional deformation episodes, list multiple values after the deformation load keywords");
                input_file.WriteLine("% For example, to model a 1ma episode of NE-oriented extensional strain, followed by uplift and erosion of 2000m over 20ma, ");
                input_file.WriteLine("% followed by an episode of overpressure and cooling (e.g. due to injection of cold fluid for 10 years), use the following");
                input_file.WriteLine("%   DefaultEhminAzi 0 0.7853 0");
                input_file.WriteLine("%   EhminAziProperty PROPERTY1 PROPERTY2 PROPERTY3 PROPERTY4");
                input_file.WriteLine("%   DefaultEhminRate -0.01 0 0");
                input_file.WriteLine("%   EhminRateProperty PROPERTY1 PROPERTY2 PROPERTY3 PROPERTY4");
                input_file.WriteLine("%   DefaultEhmaxRate 0 0 0");
                input_file.WriteLine("%   EhmaxRateProperty PROPERTY1 PROPERTY2 PROPERTY3 PROPERTY4");
                input_file.WriteLine("%   DefaultAppliedOverpressureRate 0 0 1E+12");
                input_file.WriteLine("%   DefaultAppliedTemperatureChange 0 0 -5E+6");
                input_file.WriteLine("%   DefaultAppliedUpliftRate 0 100 0");
                input_file.WriteLine("%   StressArchingFactor 0 0 1");
                input_file.WriteLine("%   DeformationEpisodeDuration 1 20 1E-5");
                input_file.WriteLine();

                input_file.WriteLine("% Mechanical properties");
                input_file.WriteLine("% Young's Modulus in Pa");
                input_file.WriteLine("DefaultYoungsMod 1E+10");
                input_file.WriteLine("%YoungsModProperty PROPERTYNAME");
                input_file.WriteLine("DefaultPoissonsRatio 0.25");
                input_file.WriteLine("%PoissonsRatioProperty PROPERTYNAME");
                input_file.WriteLine("DefaultPorosity 0.2");
                input_file.WriteLine("PorosityProperty PROPERTYNAME");
                input_file.WriteLine("DefaultBiotCoefficient 1");
                input_file.WriteLine("%BiotCoefficientProperty PROPERTYNAME");
                input_file.WriteLine("% Thermal expansion coefficient typically 3E-5/degK for sandstone, 4E-5/degK for shale (Miller 1995)");
                input_file.WriteLine("DefaultThermalExpansionCoefficient 4E-5");
                input_file.WriteLine("%ThermalExpansionCoefficientProperty PROPERTYNAME");
                input_file.WriteLine("% Crack surface energy in J/m2");
                input_file.WriteLine("DefaultCrackSurfaceEnergy 1000");
                input_file.WriteLine("%CrackSurfaceEnergyProperty PROPERTYNAME");
                input_file.WriteLine("DefaultFrictionCoefficient 0.5");
                input_file.WriteLine("%FrictionCoefficientProperty PROPERTYNAME");
                input_file.WriteLine("% Strain relaxation time constants");
                input_file.WriteLine("% Units are determined by ModelTimeUnits setting");
                input_file.WriteLine("% Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate");
                input_file.WriteLine("DefaultRockStrainRelaxation 0");
                input_file.WriteLine("%RockStrainRelaxationProperty PROPERTYNAME");
                input_file.WriteLine("% Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only");
                input_file.WriteLine("DefaultFractureRelaxation 0");
                input_file.WriteLine("%FractureRelaxationProperty PROPERTYNAME");
                input_file.WriteLine("% Initial microfracture distribution function");
                input_file.WriteLine("% For unconfined fractures this can be set to PowerLaw, Exponential or LogNormal");
                input_file.WriteLine("% For layer-bound fractures it is assumed to be power law");
                input_file.WriteLine("InitialMicrofractureDistributionFunction PowerLaw");
                input_file.WriteLine("% Density of initial microfractures");
                input_file.WriteLine("DefaultInitialMicrofractureDensity 0.001");
                input_file.WriteLine("%InitialMicrofractureDensityProperty PROPERTYNAME");
                input_file.WriteLine("% Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures");
                input_file.WriteLine("DefaultInitialMicrofractureSizeDistribution 3");
                input_file.WriteLine("%InitialMicrofractureSizeDistributionProperty PROPERTYNAME");
                input_file.WriteLine("% Median initial microfracture radius - this is only used for the log-normal distribution");
                input_file.WriteLine("% Set to -1 to use layer thickness / 20");
                input_file.WriteLine("DefaultInitialMicrofractureMedianRadius -1");
                input_file.WriteLine("%InitialMicrofractureMedianRadiusPropertyName PROPERTYNAME");
                input_file.WriteLine("% Subritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation");
                input_file.WriteLine("DefaultSubcriticalPropIndex 10");
                input_file.WriteLine("%SubcriticalPropIndexProperty PROPERTYNAME");
                input_file.WriteLine("% Critical fracture propagation rate in m/s");
                input_file.WriteLine("CriticalPropagationRate 2000");
                input_file.WriteLine("% Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity");
                input_file.WriteLine("% NB Permeability must be given in m^2; 1mD = 9.869233E-16m^2");
                input_file.WriteLine("DefaultHostRock_kh 0");
                input_file.WriteLine("%HostRock_kvProperty PROPERTYNAME");
                input_file.WriteLine("DefaultHostRock_kh 0");
                input_file.WriteLine("%HostRock_kvProperty PROPERTYNAME");
                input_file.WriteLine("% Flag for whether to average mechanical properties properties across the GRDECL grid cells, or take the value from the top middle cell");
                input_file.WriteLine("AverageMechanicalPropertyData true");
                input_file.WriteLine();

                input_file.WriteLine("% Cleavages: Cleavage planes reduce the crack surface energy and/or friction coefficient on fractures parallel to the specified cleavage orientation");
                input_file.WriteLine("% Multiple cleavage planes can be defined; for each one, the crack surface energy and/or friction coefficient on the fracture set with the closest orientation (within the specified MaxConsistencyAngle) will be modified accordingly");
                input_file.WriteLine("% If there is no fracture set within the specified MaxConsistencyAngle, the cleavage plane will have no effect");
                input_file.WriteLine("% To specify a cleavage plane, use the keyword Cleavage then specify the default values for the cleavage azimuth and dip (radians), and the crack surface energy (J/m2) and sliding friction coefficient parallel to the cleavage plane, in that order");
                input_file.WriteLine("% Optionally these values can be followed by property names for each of the four parameters, in the same order");
                input_file.WriteLine("% E.g. to specify a North-dipping inclined cleavage plane with crack surface energy 500J/m2 and sliding friction coefficient 0.4, use:");
                input_file.WriteLine("%Cleavage 0 1.05 500 0.4");
                input_file.WriteLine("% Use grid properties to define these parameters as follows:");
                input_file.WriteLine("%Cleavage 0 1.05 500 0.4 AZIMUTH_PROPERTYNAME DIP_PROPERTYNAME CSE_PROPERTYNAME FRICTION_PROPERTYNAME");
                input_file.WriteLine();

                input_file.WriteLine("% Stress state");
                input_file.WriteLine("% Stress distribution scenario - use to turn on or off stress shadow effect");
                input_file.WriteLine("% Options are EvenlyDistributedStress or StressShadow");
                input_file.WriteLine("% Do not use DuctileBoundary as this is not yet implemented");
                input_file.WriteLine("StressDistributionScenario StressShadow");
                input_file.WriteLine("% Depth at the start of deformation (in metres, positive downwards) - this will control stress state");
                input_file.WriteLine("% If DepthAtDeformation is specified, this will be used to calculate the initial vertical stress");
                input_file.WriteLine("% If DepthAtDeformation is <=0 or NaN, the depth at the start of deformation will be set to the current depth plus total specified uplift");
                input_file.WriteLine("DefaultDepthAtDeformation -1");
                input_file.WriteLine("%DepthAtDeformationProperty PROPERTYNAME");
                input_file.WriteLine("% Mean density of overlying sediments and fluid in kg/m3");
                input_file.WriteLine("MeanOverlyingSedimentDensity 2250");
                input_file.WriteLine("FluidDensity 1000");
                input_file.WriteLine("% Fluid overpressure in Pa");
                input_file.WriteLine("InitialOverpressure 0");
                input_file.WriteLine("% Geothermal gradient (degK/m)");
                input_file.WriteLine("GeothermalGradient 0.03");
                input_file.WriteLine("% InitialStressRelaxation controls the initial horizontal stress, prior to the application of horizontal strain");
                input_file.WriteLine("% Set InitialStressRelaxation to 1 to have initial horizontal stress = vertical stress (viscoelastic equilibrium)");
                input_file.WriteLine("% Set InitialStressRelaxation to 0 to have initial horizontal stress = v/(1-v) * vertical stress (elastic equilibrium)");
                input_file.WriteLine("% Set InitialStressRelaxation to -1 for initial horizontal stress = Mohr-Coulomb failure stress (critical stress state)");
                input_file.WriteLine("InitialStressRelaxation 0.5");
                input_file.WriteLine("% Flag for whether to average stress and strain data across the GRDECL grid cells, or take the value from the top middle cell");
                input_file.WriteLine("AverageStressStrainData false");
                input_file.WriteLine();

                input_file.WriteLine("% Outputs");
                input_file.WriteLine("% Output to file from within FractureGrid object");
                input_file.WriteLine("% This will generate one file of implicit data per gridblock, and one DFN file per output stage");
                input_file.WriteLine("% Output files from the FractureGrid object can be useful for debugging or detailed analysis of fracture growth");
                input_file.WriteLine("WriteImplicitDataFiles false");
                input_file.WriteLine("WriteDFNFiles false");
                input_file.WriteLine("% Output file type for explicit DFN data:");
                input_file.WriteLine("%      - ASCII (Can be loaded into the data analysis spreadsheets supplied with DFM Generator)");
                input_file.WriteLine("%      - FAB (FAB files can be loaded directly into Petrel)");
                input_file.WriteLine("OutputDFNFileType ASCII");
                input_file.WriteLine("% Flag to write implicit fracture data to a series of GRDECL files");
                input_file.WriteLine("% A separate GRDECL file will be generated for each output stage");
                input_file.WriteLine("% These files can include the grid geometry and output properties, or only the output properties");
                input_file.WriteLine("WriteGRDECLFiles true");
                input_file.WriteLine("IncludeGridGeometryInGRDECLFiles false");
                input_file.WriteLine("% Flag to write explicit DFN data to a series of FAB files");
                input_file.WriteLine("% A separate FAB file will be generated for each output stage");
                input_file.WriteLine("WriteFABFiles true");
                input_file.WriteLine("% Output DFM at intermediate stages of fracture growth");
                input_file.WriteLine("NoIntermediateOutputs 0");
                input_file.WriteLine("% Flag to control interval between output of intermediate stage DFMs:");
                input_file.WriteLine("%      - EqualArea (output at at approximately equal increments of total fracture area)");
                input_file.WriteLine("%      - EqualTime (output at equal intervals of time)");
                input_file.WriteLine("%      - SpecifiedTime (output at the end of each specified deformation episode)");
                input_file.WriteLine("IntermediateOutputIntervalControl EqualArea");
                input_file.WriteLine("% Flag to output the layer-bound fracture centrepoints and the unconfined fracture rays as polylines");
                input_file.WriteLine("OutputCentrepoints false");
                input_file.WriteLine("% Flag to calculate and output density, mean length and connectivity data (if selected) for individual fracture sets");
                input_file.WriteLine("OutputFractureSets true");
                input_file.WriteLine("% Flag to calculate and output the fracture reactivation potential: this represents the fracture driving stress if positive, and the cohesionless distance to failure if negative");
                input_file.WriteLine("OutputFractureReactivationPotential false");
                input_file.WriteLine("% Flag to calculate and output the bulk rock compliance and stiffness tensors");
                input_file.WriteLine("OutputBulkRockElasticTensors false");
                input_file.WriteLine("% Flag to calculate and output fracture connectivity and anisotropy indices");
                input_file.WriteLine("OutputFractureConnectivityAnisotropy false");
                input_file.WriteLine("% Flag to calculate and output fracture porosity");
                input_file.WriteLine("OutputFracturePorosity true");
                input_file.WriteLine("% Flag to calculate and output fracture permeability tensors");
                input_file.WriteLine("OutputFracturePermeabilityTensor false");
                input_file.WriteLine("% Fracture types included in the fracture permeability tensor:");
                input_file.WriteLine("%      - Microfractures: Microfractures only");
                input_file.WriteLine("%      - LayerBoundFractures: Layer-bound fractures only");
                input_file.WriteLine("%      - UnconfinedFractures: Unconfined fractures only");
                input_file.WriteLine("%      - AllFractures: All fractures");
                input_file.WriteLine("FractureTypesInPermeabilityTensor AllFractures");
                input_file.WriteLine("% Algorithm to use for calculating fracture permeability");
                input_file.WriteLine("%      - Oda1986 (The Oda 1986 model assumes fractures of infinite size and connectivity)");
                input_file.WriteLine("%      - OdaCorrected1987 (This algorithm adds a correction factor to account for fracture connectivity)");
                input_file.WriteLine("%      - SizeConnectivityCorrected (This takes into account flow between fractures along relay segments, fractures from other sets, or through the host rock; for the latter, host rock permeability must be specified)");
                input_file.WriteLine("PermeabilityAlgorithm Oda1986");
                input_file.WriteLine("% Flag to calculate and output implicit fracture population distribution functions from the FractureGrid object (will only be output if WriteImplicitDataFiles is true)");
                input_file.WriteLine("OutputPopulationDistribution true");
                input_file.WriteLine("% Number of fracture or ray length values to calculate for each of the implicit fracture population distribution functions");
                input_file.WriteLine("No_l_indexPoints 20");
                input_file.WriteLine("% MaxHMinLength and MaxHMaxLength control the range of fracture or ray lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively");
                input_file.WriteLine("% Set these values to the approximate maximum length of fractures generated (in metres), or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length");
                input_file.WriteLine("MaxHMinLength 0");
                input_file.WriteLine("MaxHMaxLength 0");
                input_file.WriteLine("% Flag to populate implicit fracture data in gridblocks with no fractures?");
                input_file.WriteLine("% If true, all gridblocks in the specified region of the grid will be populated with implicit fracture data, even if the fracture density is zero; otherwise gridblocks with zero fracture density will not be populated, enabling easier visualisation of the extent of the fracture network");
                input_file.WriteLine("PopulateEmptyGridblocks true");
                input_file.WriteLine("% Depth of horizontal section");
                input_file.WriteLine("% Set this to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file");
                input_file.WriteLine("%DepthOfHorizontalSection 2000.5");
                input_file.WriteLine();

                input_file.WriteLine("% Fracture aperture control parameters");
                input_file.WriteLine("% Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation");
                input_file.WriteLine("% Set to Uniform, SizeDependent, Dynamic, or BartonBandis");
                input_file.WriteLine("FractureApertureControl Uniform");
                input_file.WriteLine("% Fracture aperture control parameters: Uniform fracture aperture");
                input_file.WriteLine("% Fixed aperture for Mode 1 fractures striking perpendicular to hmin in the uniform aperture case (m)");
                input_file.WriteLine("Mode1HMin_UniformAperture 0.0005");
                input_file.WriteLine("% Fixed aperture for Mode 2 fractures striking perpendicular to hmin in the uniform aperture case (m)");
                input_file.WriteLine("Mode2HMin_UniformAperture 0.0005");
                input_file.WriteLine("% Fixed aperture for Mode 1 fractures striking perpendicular to hmax in the uniform aperture case (m)");
                input_file.WriteLine("Mode1HMax_UniformAperture 0.0005");
                input_file.WriteLine("% Fixed aperture for Mode 2 fractures striking perpendicular to hmax in the uniform aperture case (m)");
                input_file.WriteLine("Mode2HMax_UniformAperture 0.0005");
                input_file.WriteLine("% Fracture aperture control parameters: SizeDependent fracture aperture");
                input_file.WriteLine("% Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier");
                input_file.WriteLine("Mode1HMin_SizeDependentApertureMultiplier 1E-5");
                input_file.WriteLine("% Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier");
                input_file.WriteLine("Mode2HMin_SizeDependentApertureMultiplier 1E-5");
                input_file.WriteLine("% Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier");
                input_file.WriteLine("Mode1HMax_SizeDependentApertureMultiplier 1E-5");
                input_file.WriteLine("% Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier");
                input_file.WriteLine("Mode2HMax_SizeDependentApertureMultiplier 1E-5");
                input_file.WriteLine("% Fracture aperture control parameters: Dynamic fracture aperture");
                input_file.WriteLine("% Multiplier for dynamic aperture");
                input_file.WriteLine("DynamicApertureMultiplier 1");
                input_file.WriteLine("% Fracture aperture control parameters: Barton-Bandis model for fracture aperture");
                input_file.WriteLine("% Joint Roughness Coefficient");
                input_file.WriteLine("JRC 10");
                input_file.WriteLine("% Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock");
                input_file.WriteLine("UCSRatio 2");
                input_file.WriteLine("% Initial normal stress on fracture (Pa)");
                input_file.WriteLine("InitialNormalStress 2E+5");
                input_file.WriteLine("% Stiffness normal to the fracture, at initial normal stress (Pa/m)");
                input_file.WriteLine("FractureNormalStiffness 2.5E+9");
                input_file.WriteLine("% Maximum fracture closure (m)");
                input_file.WriteLine("MaximumClosure 0.0005");
                input_file.WriteLine();

                input_file.WriteLine("% Present day effective stress parameters");
                input_file.WriteLine("% Define the present day stress tensor and fluid pressure to override stress at the time of deformation when calculating fracture aperture and permeability");
                input_file.WriteLine("% Present day stress tensor and fluid pressure should be specified in Pa as either AbsoluteStress, TerzaghiEffectiveStress or BiotEffectiveStress tensors, depending on the PresentDayStressDefinition flag");
                input_file.WriteLine("UsePresentDayStress false");
                input_file.WriteLine("PresentDayStressDefinition TerzaghiEffectiveStress");
                input_file.WriteLine("DefaultPresentDayStressXX 15000000");
                input_file.WriteLine("DefaultPresentDayStressYY 15000000");
                input_file.WriteLine("DefaultPresentDayStressZZ 25000000");
                input_file.WriteLine("DefaultPresentDayStressXY 0");
                input_file.WriteLine("DefaultPresentDayStressYZ 0");
                input_file.WriteLine("DefaultPresentDayStressZX 0");
                input_file.WriteLine("DefaultPresentDayFluidPressure 10000000");
                input_file.WriteLine("%PresentDayStressXXProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayStressYYProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayStressZZProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayStressXYProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayStressYZProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayStressZXProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayFluidPressurePropertyName PROPERTYNAME");
                input_file.WriteLine("% It is also possible to overridde the Biot Coefficient when calculating present day effective stress");
                input_file.WriteLine("%DefaultPresentDayBiotCoefficient 1");
                input_file.WriteLine("%PresentDayBiotCoefficientPropertyName PROPERTYNAME");
                input_file.WriteLine();

                input_file.WriteLine("% Calculation control parameters");
                input_file.WriteLine("% Number of layer-bound fracture sets");
                input_file.WriteLine("% Set to 1 to generate a single fracture set, perpendicular to ehmin");
                input_file.WriteLine("% Set to 2 to generate two orthogonal fracture sets, perpendicular to the minimum and maximum horizontal strain directions; this is typical of a single stage of tectonic deformation in intact rock");
                input_file.WriteLine("% Set to 6 to model polygonal or strike-slip fractures, or multiple deformation episodes where there are pre-existing fractures oblique to the principal horizontal stresses");
                input_file.WriteLine("NoLayerBoundFractureSets 2");
                input_file.WriteLine("% Fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal");
                input_file.WriteLine("Mode1Only false");
                input_file.WriteLine("Mode2Only false");
                input_file.WriteLine("% Position of fracture nucleation within the layer; set to 0 to force all fractures to nucleate at the base of the layer and 1 to force all fractures to nucleate at the top of the layer; set to -1 to nucleate fractures at random locations within the layer");
                input_file.WriteLine("FractureNucleationPosition -1");
                input_file.WriteLine("% Flag to check microfractures against stress shadows of all macrofractures, regardless of set: can be set to None, All or Automatic");
                input_file.WriteLine("%      - If None, microfractures will only be deactivated if they lie in the stress shadow zone of parallel macrofractures");
                input_file.WriteLine("%      - If All, microfractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular macrofractures, depending on the strain tensor");
                input_file.WriteLine("%      - If Automatic, microfractures in the stress shadow zone of oblique or perpendicular macrofractures will be deactivated only if there are more than two fracture sets");
                input_file.WriteLine("CheckAlluFStressShadows Automatic");
                input_file.WriteLine("% Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes");
                input_file.WriteLine("AnisotropyCutoff 1");
                input_file.WriteLine("% Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow");
                input_file.WriteLine("AllowReverseFractures false");
                input_file.WriteLine("% Maximum duration for individual timesteps; set to -1 for no maximum timestep duration");
                input_file.WriteLine("MaxTimestepDuration -1");
                input_file.WriteLine("% Maximum increase in MFP33 allowed in each timestep - controls the optimal timestep duration");
                input_file.WriteLine("% Increase this to run calculation faster, with fewer but longer timesteps");
                input_file.WriteLine("MaxTimestepMFP33Increase 0.005");
                input_file.WriteLine("% Minimum radius for microfractures to be included in implicit fracture density and porosity calculations (in metres)");
                input_file.WriteLine("% If this is set to 0 (i.e. include all microfractures) then it will not be possible to calculate volumetric microfracture density as this will be infinite");
                input_file.WriteLine("% If this is set to -1 the maximum radius of the smallest bin will be used (i.e. exclude the smallest bin from the microfracture population)");
                input_file.WriteLine("MinImplicitMicrofractureRadius -1");
                input_file.WriteLine("% Number of bins used in numerical integration of uFP32");
                input_file.WriteLine("% This controls accuracy of numerical calculation of microfracture populations - increase this to increase accuracy of the numerical integration at expense of runtime");
                input_file.WriteLine("No_r_bins 10");
                input_file.WriteLine("% Calculation termination controls");
                input_file.WriteLine("% The calculation is set to stop automatically when fractures stop growing");
                input_file.WriteLine("% This can be defined in one of three ways:");
                input_file.WriteLine("%      - When the total volumetric ratio of active (propagating) half-macrofractures (a_MFP33) drops below a specified proportion of the peak historic value");
                input_file.WriteLine("%      - When the total volumetric density of active (propagating) half-macrofractures (a_MFP30) drops below a specified proportion of the total (propagating and non-propagating) volumetric density (MFP30)");
                input_file.WriteLine("%      - When the total clear zone volume (the volume in which fractures can nucleate without falling within or overlapping a stress shadow) drops below a specified proportion of the total volume");
                input_file.WriteLine("% Increase these cutoffs to reduce the sensitivity and stop the calculation earlier");
                input_file.WriteLine("% Use this to prevent a long calculation tail - i.e. late timesteps where fractures have stopped growing so they have no impact on fracture populations, just increase runtime");
                input_file.WriteLine("% To stop calculation while fractures are still growing reduce the DeformationEpisodeDuration (in the deformation load inputs) or MaxTimesteps limits");
                input_file.WriteLine("% Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control");
                input_file.WriteLine("Current_HistoricMFP33TerminationRatio -1");
                input_file.WriteLine("% Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control");
                input_file.WriteLine("Active_TotalMFP30TerminationRatio -1");
                input_file.WriteLine("% Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated");
                input_file.WriteLine("MinimumClearZoneVolume 0.01");
                input_file.WriteLine("% Use the deformation episode duration (set in the deformation load inputs) or the maximum timestep limit to stop the calculation before fractures have finished growing");
                input_file.WriteLine("MaxTimesteps 1000");
                input_file.WriteLine("% DFN geometry controls");
                input_file.WriteLine("% Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated");
                input_file.WriteLine("GenerateExplicitDFN true");
                input_file.WriteLine("% Set false to allow fractures to propagate outside of the outer grid boundary");
                input_file.WriteLine("CropAtBoundary true");
                input_file.WriteLine("% Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment");
                input_file.WriteLine("LinkStressShadows false");
                input_file.WriteLine("% Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set");
                input_file.WriteLine("% Set to Pi/4 radians (45 degrees) by default");
                input_file.WriteLine("MaxConsistencyAngle 0.78539816");
                input_file.WriteLine("% Layer thickness cutoff (in metres): explicit DFN will not be calculated for gridblocks thinner than this value");
                input_file.WriteLine("% Set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers");
                input_file.WriteLine("MinimumLayerThickness 0");
                input_file.WriteLine("% Maximum number of fracture patches that can be generated per gridblock");
                input_file.WriteLine("% Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason");
                input_file.WriteLine("MaxNoFracturePatches 1000");
                input_file.WriteLine("% Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small");
                input_file.WriteLine("% Set to 0 to disable probabilistic fracture nucleation");
                input_file.WriteLine("% Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)");
                input_file.WriteLine("ProbabilisticFractureNucleationLimit -1");
                input_file.WriteLine("% Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set");
                input_file.WriteLine("% Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time");
                input_file.WriteLine("PropagateFracturesInNucleationOrder true");
                input_file.WriteLine("% Flag to control whether to search adjacent gridblocks for stress shadow interaction: can be set to All, None or Automatic; if set to Automatic, this will be determined independently for each gridblock based on the gridblock geometry");
                input_file.WriteLine("SearchAdjacentGridblocks Automatic");
                input_file.WriteLine("% Minimum radius for microfractures to be included in explicit DFN (in metres)");
                input_file.WriteLine("% Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN");
                input_file.WriteLine("MinExplicitMicrofractureRadius 0");
                input_file.WriteLine("% Number of cornerpoints defining the microfracture polygons in the explicit DFN");
                input_file.WriteLine("% Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints");
                input_file.WriteLine("Number_uF_Points 8");
                input_file.WriteLine();

                input_file.WriteLine("% Parameters for controlling unconfined fractures");
                input_file.WriteLine("% Use NoUnconfinedFractureStrikeSets and NoUnconfinedFractureDipSets to create unconfined fracture sets, which can propagate and interact vertically as well as horizontally");
                input_file.WriteLine("% These are useful for modelling fractures in thick geobodies such as igneous plutons");
                input_file.WriteLine("% NB Unconfined fracture sets are not subdivided into dipsets; unconfined fractures with the same strike but different dips are counted as different sets");
                input_file.WriteLine("% The total number of unconfined fracture sets generates will therefore be given by NoUnconfinedFractureStrikeSets * NoUnconfinedFractureDipSets");
                input_file.WriteLine("% Recommended values fo unconfined fracture modelling: NoUnconfinedFractureStrikeSets 6; NoUnconfinedFractureDipSets 3");
                input_file.WriteLine("NoUnconfinedFractureStrikeSets 0");
                input_file.WriteLine("NoUnconfinedFractureDipSets 0");
                input_file.WriteLine("% Number of rays comprising each unconfined fracture");
                input_file.WriteLine("NoRaysPerUnconfinedFracture 16");
                input_file.WriteLine("% Minimum radius for unconfined fractures; this will be the length of the rays at nucleation");
                input_file.WriteLine("% If set to -1, will use 0.01 * layer thickness");
                input_file.WriteLine("MinUnconfinedFractureRadius -1");
                input_file.WriteLine("% Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length");
                input_file.WriteLine("% If set to -1, will use 0.5 * layer thickness");
                input_file.WriteLine("MaxUnconfinedFractureRadius -1");
                input_file.WriteLine("% Maximum allowed effective radius for unconfined fractures; will limit fracture stress shadow and propagation rate but not fracture growth");
                input_file.WriteLine("% If set to -1, there will be no limit on effective fracture radius");
                input_file.WriteLine("MaxEffectiveUnconfinedFractureRadius -1");
                input_file.WriteLine("% Calculation termination controls");
                input_file.WriteLine("% The calculation is set to stop automatically when fractures stop growing");
                input_file.WriteLine("% This can be defined in one of three ways:");
                input_file.WriteLine("%      - When the total volumetric ratio of active (propagating)  unconfined fractures (a_UCFP33) drops below a specified proportion of the peak historic value");
                input_file.WriteLine("%      - When the total volumetric density of active (propagating) unconfined fracture rays (a_UCRP30) drops below a specified proportion of the total (propagating and non-propagating) volumetric density (UCRP30)");
                input_file.WriteLine("%      - When the total clear zone volume (the volume in which fractures can nucleate without falling within or overlapping a stress shadow) drops below a specified proportion of the total volume");
                input_file.WriteLine("%      - When the mean static unconfined fracture ray length drops below a specified value (this prevents brecciation - large numbers of small UCFs terminating against each other, filling the voids between stress shadows of larger UCFs)");
                input_file.WriteLine("% Increase these cutoffs to reduce the sensitivity and stop the calculation earlier");
                input_file.WriteLine("% Use this to prevent a long calculation tail - i.e. late timesteps where fractures have stopped growing so they have no impact on fracture populations, just increase runtime");
                input_file.WriteLine("% To stop calculation while fractures are still growing reduce the DeformationEpisodeDuration (in the deformation load inputs) or MaxTimesteps limits");
                input_file.WriteLine("% Ratio of current to peak active unconfined fracture mean linear density at which fracture sets are considered inactive; set to negative value to switch off this control");
                input_file.WriteLine("Current_HistoricUCFP32TerminationRatio -1");
                input_file.WriteLine("% Ratio of active to total unconfined fracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control");
                input_file.WriteLine("Active_TotalUCRP30TerminationRatio -1");
                input_file.WriteLine("% Minimum required clear zone volume in which unconfined fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated");
                input_file.WriteLine("MinimumUCFClearZoneVolume 0.2");
                input_file.WriteLine("% Minimum allowed mean static unconfined fracture ray length; if the mean static ray length drops below this value, the fracture set will be deactivated; set to 0 for no limit and -1 to use the minimum UCF radius");
                input_file.WriteLine("% MinimumStaticUCRLength -1");
                input_file.WriteLine("% Maximum increase in UCFP33 allowed in each timestep - controls the optimal timestep duration");
                input_file.WriteLine("% Increase this to run calculation faster, with fewer but longer timesteps");
                input_file.WriteLine("MaxTimestepUCFP33Increase 0.005");
                input_file.WriteLine("% Maximum proportional increase in the unconfined fracture ray length in each timestep (controls speed and accuracy of calculation)");
                input_file.WriteLine("% Set to -1 for no limit");
                input_file.WriteLine("MaxTimestepRadiusIncrease -1");
                input_file.WriteLine("% Maximum proportional increase in the radius of the unconfined fractures before checking for fracture deactivation (controls number of implicit fracture population datapoints generated)");
                input_file.WriteLine("Max_R_DeactivationCheck_interval 0.2");
                input_file.WriteLine("% Minimum activation probability for unconfined fractures; if the activation probability drops below this, the specified proportion of fractures will be deactivated, creating a new implicit fracture population datapoint");
                input_file.WriteLine("Min_R_ActivationProbability 0.8");
                input_file.WriteLine("% The proportion of the ray length increment to apply to active unconfined fracture datapoints before the specified proportion of fractures are deactivated");
                input_file.WriteLine("% Set to -1 to use the mean distance that a fracture propagates before being deactivated");
                input_file.WriteLine("ProportionalIncrementToApply -1");
                input_file.WriteLine("% Minimum proportional size difference for static unconfined fracture datapoints; any datapoints with less than this proportional size difference may be amalgamated into a single point");
                input_file.WriteLine("Min_R_StaticDatapointSizeRatio 0.02");
                input_file.WriteLine("% Frequency (in timesteps) with which static unconfined fracture datapoints are culled");
                input_file.WriteLine("CullTSFrequency 10");
                input_file.WriteLine("% Flag to calculate implicit data for unconfined fractures; if set to false no grid properties will be generated, only an explicit DFN; does not affect layer-bound fractures");
                input_file.WriteLine("CalculateImplicitUCFData true");
                input_file.WriteLine("% Flag to check unconfined fractures against stress shadows of all other unconfined fractures, regardless of set: can be set to None, All or Automatic");
                input_file.WriteLine("%      - If None, unconfined fractures will only be deactivated if they lie in the stress shadow zone of parallel unconfined fractures");
                input_file.WriteLine("%      - If All, unconfined fractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular unconfined fractures, depending on the strain tensor");
                input_file.WriteLine("%      - If Automatic, unconfined fractures in the stress shadow zone of oblique or perpendicular unconfined fractures will be deactivated only if there are more than two fracture sets");
                input_file.WriteLine("CheckAllUCFStressShadows Automatic");
                input_file.WriteLine("% Flag to make unconfined fractures completely planar, even when crossing gridblock boundaries");
                input_file.WriteLine("PlanarUnconfinedFractures false");
                input_file.WriteLine("% Minimum radius for large fractures; fractures larger than this will be considered to influence the entire grid when checking stress shadows");
                input_file.WriteLine("LargeFractureMinimumRadius -1");
                input_file.WriteLine("% Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius");
                input_file.WriteLine("MinStressShadowDeactivationRatio 0.5");
                input_file.WriteLine("% Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius");
                input_file.WriteLine("MinIntersectionDeactivationRatio 0.5");
                input_file.WriteLine("% Flag to filter cells by property; if true, cells with property values outside the specified range will not be included in the model");
                input_file.WriteLine("FilterByProperty false");
                input_file.WriteLine("% Property to filter cells by; cells with property values outside the specified range will not be included in the model");
                input_file.WriteLine("%PropertyToFilter PROPERTYNAME");
                input_file.WriteLine("% Minimum cutoff for the property filter; cells where the spcified property value is lower than this will not be included in the model");
                input_file.WriteLine("%FilterByPropertyMinCutoff 0");
                input_file.WriteLine("% Maximum cutoff for the property filter; cells where the spcified property value is higher than this will not be included in the model");
                input_file.WriteLine("%FilterByPropertyMaxCutoff 1");
                input_file.WriteLine();

                input_file.Close();

                Console.WriteLine("\nDFMGenerator configuration file did not exist. An empty file has been created. Please enter the required values, SAVE it and press ENTER! ");
                Console.ReadKey();
            }

            // Get path for input and output files
            // Input files are assumed to be in the current folder
            string inputFolderPath = "";
            // Get path for output files
            string outputFolderPath = "";
#else
            // Get path for input files
            string inputfile_name = "Hardcoded";
            string outputFolderPath = "";
            string inputFolderPath = "";
#endif

            // Set hardcoded default values for all parameters

            // Main properties
            // Model name, used to name output files; if not specified, will be taken from the input file name
            string ModelName = inputfile_name.Replace(".txt", "");
            // Grid geometry will be read from a GRDECL format file, along with grid properties
            // The filename should be specified here, including the extension but excluding the filepath
            // Parameters whose value can vary in different gridblocks are defined in two ways: by a default value and a property name
            // If the property name is specified and can be found in the GRDECL file, the value for each gridblock will be read from the GRDECL file
            // If the specified property in the GRDECL file has a null value for any individual gridblock, 
            // the parameter will be set to the specified default value in that gridblock
            // If no property name is specified (the property name string is empty), or no property with the specified name can be found in the GRDECL file,
            // the parameter will be set to the specified default value in every gridblock
            string GridFileName = ModelName + ".GRDECL";
            // Subset of rows and columns from the shadow grid to include in the fracture grid (indexed from 1)
            // Following GRDECL format, columns are indexed from west to east but rows are indexed in reverse order, from north to south
            // Set to -1 to include all rows and columns
            int ShadowGrid_StartColI = -1;
            int ShadowGrid_EndColI = -1;
            int ShadowGrid_StartRowJ = -1;
            int ShadowGrid_EndRowJ = -1;
            // Subset of layers from the shadow grid to include in the fracture grid (indexed from 1)
            // Following GRDECL format, layers are indexed from top to bottom
            // Set to -1 to include all layers
            int ShadowGrid_TopLayerK = -1;
            int ShadowGrid_BottomLayerK = -1;
            // Horizontal upscaling factor - used to amalgamate multiple shadow grid cell stacks into one fracture gridblock stack
            int HorizontalUpscalingFactor = 1;
            // Vertical upscaling factor - used to amalgamate multiple shadow grid layers into one fracture grid layer
            // Set to 0 to amalgamate all selected shadow grid layers into a single fracture grid layer
            // Set to 1 to create a fracture grid layer for each selected shadow grid layer
            int VerticalUpscalingFactor = 1;
            // Counter to get number of active gridblocks (after upscaling)
            int NoActiveGridblocks = 0;
            // Time units used in input load rates, time limits and strain relaxation time constants
            // These will be converted to SI units (s) by the gridblock objects
            TimeUnits ModelTimeUnits = TimeUnits.ma;
            // Flag to determine whether to generate biazimuthal conjugate dipsets; these are dipsets that contain equal numbers of fractures dipping in opposite directions
            // By default, biazimuthal conjugate dipsets will be used since this will reduce the required number of dipsets
            // However if any of the deformation episodes use a stress load tensor which includes the vertical shear components (YZ and ZX) then the resulting fracture network will not be symmetrical about strike
            // In this case we must use multiple dipsets contains only fractures dipping in opposite directions
            bool BiazimuthalConjugate = true;
            // Deformation load
            // Multiple deformation episodes can be added
            // Deformation load parameter defaults: the following describe the default values that will be applied to all deformation episodes unless otherwise specified
            // Strain orientatation
            double DefaultEhminAzi = 0;
            string EhminAziPropertyName = string.Empty;
            // Strain rates
            // Set to negative value for extensional strain - this is necessary in at least one direction to generate fractures
            // With no strain relaxation, strain rate will control rate of horizontal stress increase
            // With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant horizontal stress
            // Ehmin is most tensile (i.e. most negative) horizontal strain rate
            double DefaultEhminRate = 0;
            string EhminRatePropertyName = string.Empty;
            // Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern
            double DefaultEhmaxRate = 0;
            string EhmaxRatePropertyName = string.Empty;
            // Fluid pressure, thermal and uplift loads
            // Rate of increase of fluid overpressure (Pa/ModelTimeUnit)
            double DefaultAppliedOverpressureRate = 0;
            string AppliedOverpressureRatePropertyName = string.Empty;
            // Rate of in situ temperature change (not including cooling due to uplift) (degK/ModelTimeUnit)
            double DefaultAppliedTemperatureChange = 0;
            string AppliedTemperatureChangePropertyName = string.Empty;
            // Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/ModelTimeUnit)
            double DefaultAppliedUpliftRate = 0;
            string AppliedUpliftRatePropertyName = string.Empty;
            // Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)
            double StressArchingFactor = 0;
            // Parameters to specify dynamic deformation loads, defined in terms of stress rather than strain
            // If these parameters are specified, they will override the strain, fluid overpressure, thermal and uplift loads
            // If only the ZZ stress component is specified, it will be used to calculate the lithostatic stress instead of depth and overburden density but the strain and thermal loads will used to define horizontal stress
            // If the XX, YY, ZZ and XY stress components are specified, a horizontally symmetric stress tensor will be generated (YZ and ZX components set to 0) which will override the strain, thermal and uplift loads
            // If all 6 stress components are specified, a full stress tensor will be generated which will override the strain, thermal and uplift loads
            // If the fluid pressure is specified this will replace the hydrostatic fluid pressure and override the fluid overpressure load
            // Dynamic loads should be specified in Pa as either AbsoluteStress, TerzaghiEffectiveStress or BiotEffectiveStress tensors, depending on the StressLoadDefinition flag
            StressStateDefinition StressDefinition = StressStateDefinition.AbsoluteStress;
            //double DefaultSxx = double.NaN;
            //double DefaultSyy = double.NaN;
            //double DefaultSzz = double.NaN;
            //double DefaultSxy = double.NaN;
            //double DefaultSyz = double.NaN;
            //double DefaultSzx = double.NaN;
            //double DefaultFluidPressure = double.NaN;
            string SxxPropertyName = string.Empty;
            string SyyPropertyName = string.Empty;
            string SzzPropertyName = string.Empty;
            string SxyPropertyName = string.Empty;
            string SyzPropertyName = string.Empty;
            string SzxPropertyName = string.Empty;
            string FluidPressurePropertyName = string.Empty;
            // Duration of the deformation episode; set to -1 to continue until fracture saturation is reached
            double DeformationEpisodeDuration = -1;

            // Global deformation load parameter lists
            // These contain one entry for each deformation episode, in order
            // They will be copied to all gridblocks
            List<string> DeformationEpisodeName_list = new List<string>();
            List<double> DefaultEhminAzi_list = new List<double>();
            List<string> EhminAziPropertyName_list = new List<string>();
            List<double> DefaultEhminRate_list = new List<double>();
            List<string> EhminRatePropertyName_list = new List<string>();
            List<double> DefaultEhmaxRate_list = new List<double>();
            List<string> EhmaxRatePropertyName_list = new List<string>();
            List<double> DefaultAppliedOverpressureRate_list = new List<double>();
            List<string> AppliedOverpressureRatePropertyName_list = new List<string>();
            List<double> DefaultAppliedTemperatureChange_list = new List<double>();
            List<string> AppliedTemperatureChangePropertyName_list = new List<string>();
            List<double> DefaultAppliedUpliftRate_list = new List<double>();
            List<string> AppliedUpliftRatePropertyName_list = new List<string>();
            List<double> StressArchingFactor_list = new List<double>();
            List<double> DeformationEpisodeDuration_list = new List<double>();
            List<StressStateDefinition> StressDefinition_list = new List<StressStateDefinition>();
            //List<double> DefaultSxx_list = new List<double>();
            //List<double> DefaultSyy_list = new List<double>();
            //List<double> DefaultSzz_list = new List<double>();
            //List<double> DefaultSxy_list = new List<double>();
            //List<double> DefaultSyz_list = new List<double>();
            //List<double> DefaultSzx_list = new List<double>();
            //List<double> DefaultFluidPressure_list = new List<double>();
            List<string> SxxPropertyName_list = new List<string>();
            List<string> SyyPropertyName_list = new List<string>();
            List<string> SzzPropertyName_list = new List<string>();
            List<string> SxyPropertyName_list = new List<string>();
            List<string> SyzPropertyName_list = new List<string>();
            List<string> SzxPropertyName_list = new List<string>();
            List<string> FluidPressurePropertyName_list = new List<string>();
#if !READINPUTFROMFILE
            // Add a deformation episode with default values
            DeformationEpisodeName_list.Add("Deformation_Episode_1");
            DefaultEhminAzi_list.Add(DefaultEhminAzi);
            EhminAziPropertyName_list.Add(EhminAziPropertyName);
            DefaultEhminRate_list.Add(DefaultEhminRate);
            EhminRatePropertyName_list.Add(EhminRatePropertyName);
            DefaultEhmaxRate_list.Add(DefaultEhmaxRate);
            EhmaxRatePropertyName_list.Add(EhmaxRatePropertyName);
            DefaultAppliedOverpressureRate_list.Add(DefaultAppliedOverpressureRate);
            AppliedOverpressureRatePropertyName_list.Add(AppliedOverpressureRatePropertyName);
            DefaultAppliedTemperatureChange_list.Add(DefaultAppliedTemperatureChange);
            AppliedTemperatureChangePropertyName_list.Add(AppliedTemperatureChangePropertyName);
            DefaultAppliedUpliftRate_list.Add(DefaultAppliedUpliftRate);
            AppliedUpliftRatePropertyName_list.Add(AppliedUpliftRatePropertyName);
            StressArchingFactor_list.Add(StressArchingFactor);
            DeformationEpisodeDuration_list.Add(1);
#endif

            // Mechanical properties
            double DefaultYoungsMod = 1E+10;
            string YoungsModPropertyName = string.Empty;
            double DefaultPoissonsRatio = 0.25;
            string PoissonsRatioPropertyName = string.Empty;
            double DefaultPorosity = 0.2;
            string PorosityPropertyName = string.Empty;
            double DefaultBiotCoefficient = 1;
            string BiotCoefficientPropertyName = string.Empty;
            // Thermal expansion coefficient typically 3E-5/degK for sandstone, 4E-5/degK for shale (Miller 1995)
            double DefaultThermalExpansionCoefficient = 4E-5;
            string ThermalExpansionCoefficientPropertyName = string.Empty;
            double DefaultCrackSurfaceEnergy = 1000;
            string CrackSurfaceEnergyPropertyName = string.Empty;
            double DefaultFrictionCoefficient = 0.5;
            string FrictionCoefficientPropertyName = string.Empty;
            // Strain relaxation data
            // Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate
            double DefaultRockStrainRelaxation = 0;
            string RockStrainRelaxationPropertyName = string.Empty;
            // Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only
            double DefaultFractureRelaxation = 0;
            string FractureRelaxationPropertyName = string.Empty;
            // Initial microfracture distribution function
            // For unconfined fractures this can be set to PowerLaw, Exponential or LogNormal
            // For layer-bound fractures it is assumed to be power law
            InitialFractureDistribution InitialMicrofractureDistributionFunction = InitialFractureDistribution.PowerLaw;
            // Density of initial microfractures
            double DefaultInitialMicrofractureDensity = 0.001;
            string InitialMicrofractureDensityPropertyName = string.Empty;
            // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
            double DefaultInitialMicrofractureSizeDistribution = 3;
            string InitialMicrofractureSizeDistributionPropertyName = string.Empty;
            // Median initial microfracture radius - this is only used for the log-normal distribution function
            // If undefined, will be set to layer thickness / 20
            double DefaultInitialMicrofractureMedianRadius = double.NaN;
            string InitialMicrofractureMedianRadiusPropertyName = string.Empty;
            // Subritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
            double DefaultSubcriticalPropIndex = 10;
            string SubcriticalPropIndexPropertyName = string.Empty;
            double CriticalPropagationRate = 2000;
            // Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity
            double DefaultHostRock_kh = 9.869233e-16;// 1mD in m2
            string HostRock_khPropertyName = string.Empty;
            double DefaultHostRock_kv = 9.869233e-16;// 1mD in m2
            string HostRock_kvPropertyName = string.Empty;
            // Flag for whether to average mechanical properties properties across the shadow grid cells, or take the value from the top middle cell
            bool AverageMechanicalPropertyData = true;

            // Cleavages: Cleavage planes reduce the crack surface energy and/or friction coefficient on fractures parallel to the specified cleavage orientation
            // Multiple cleavage planes can be defined; for each one, the crack surface energy and/or friction coefficient on the fracture set with the closest orientation (within the specified MaxConsistencyAngle) will be modified accordingly
            // If there is no fracture set within the specified MaxConsistencyAngle, the cleavage plane will have no effect
            // Any number of cleavage planes can be defined
            // Create arrays for the default values and grid properties related to the cleavages, if defined
            int NoCleavages = 0;
            List<bool> Cleavage_Defined = new List<bool>();
            List<double> Default_Cleavage_Azimuth = new List<double>();
            List<string> Cleavage_Azimuth_PropertyName = new List<string>();
            List<double> Default_Cleavage_Dip = new List<double>();
            List<string> Cleavage_Dip_PropertyName = new List<string>();
            List<double> Default_Cleavage_CrackSurfaceEnergy = new List<double>();
            List<string> Cleavage_CrackSurfaceEnergy_PropertyName = new List<string>();
            List<double> Default_Cleavage_FrictionCoefficient = new List<double>();
            List<string> Cleavage_FrictionCoefficient_PropertyName = new List<string>();

            // Stress state
            // Stress distribution scenario - use to turn on or off stress shadow effect
            // Do not use DuctileBoundary as this is not yet implemented
            StressDistribution StressDistributionScenario = StressDistribution.StressShadow;
            // Depth at the start of deformation (in metres, positive downwards) - this will control stress state
            // If DepthAtDeformation is specified, this will be used to calculate vertical stress
            // If DepthAtDeformation is <=0 or NaN, the depth at the start of deformation will be set to the current depth plus total specified uplift
            double DefaultDepthAtDeformation = -1;
            string DepthAtDeformationPropertyName = string.Empty;
            //bool OverwriteDepth = (DepthAtDeformation > 0);
            // Mean density of overlying sediments and fluid (kg/m3)
            double MeanOverlyingSedimentDensity = 2250;
            double FluidDensity = 1000;
            // Fluid overpressure (Pa)
            double InitialOverpressure = 0;
            // Geothermal gradient (degK/m)
            double GeothermalGradient = 0.03;
            // InitialStressRelaxation controls the initial horizontal stress, prior to the application of horizontal strain
            // Set InitialStressRelaxation to 1 to have initial horizontal stress = vertical stress (viscoelastic equilibrium)
            // Set InitialStressRelaxation to 0 to have initial horizontal stress = v/(1-v) * vertical stress (elastic equilibrium)
            // Set InitialStressRelaxation to -1 for initial horizontal stress = Mohr-Coulomb failure stress (critical stress state)
            double InitialStressRelaxation = 0.5;
            // Flag for whether to average stress and strain data across the shadow grid cells, or take the value from the top middle cell
            bool AverageStressStrainData = false;

            // Outputs
            // Output to file from within FractureGrid object
            // This will generate one file of implicit data per gridblock, and one DFN file per output stage
            // Output files from the FractureGrid object can be useful for debugging or detailed analysis of fracture growth
            bool WriteImplicitDataFiles = false;
            bool WriteDFNFiles = false;
            // Output file type for explicit DFN data: ASCII or FAB (NB FAB files can be loaded directly into Petrel)
            DFNFileType OutputDFNFileType = DFNFileType.ASCII;
            // Flag to write implicit fracture data from the ShadowGrid object to a series of GRDECL files
            // A separate GRDECL file will be generated for each output stage
            // These files can include the grid geometry and output properties, or only the output properties
            bool WriteGRDECLFiles = true;
            bool IncludeGridGeometryInGRDECLFiles = false;
            // Flag to write explicit DFN data from the ShadowGrid object to a series of FAB files
            // A separate FAB file will be generated for each output stage
            bool WriteFABFiles = true;
            // Output DFM at intermediate stages of fracture growth
            int NoIntermediateOutputs = 0;
            // Flag to control interval between output of intermediate stage DFMs:
            // - EqualArea (output at at approximately euqal increments of total fracture area)
            // - EqualTime (output at equal intervals of time)
            // - SpecifiedTime (output at the end of each specified deformation episode)
            IntermediateOutputInterval IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualArea;
            // Flag to output the layer-bound fracture centrepoints and the unconfined fracture rays as polylines
            bool OutputCentrepoints = false;
            // Flag to calculate and output density, mean length and connectivity data (if selected) for individual fracture sets
            bool OutputFractureSets = true;
            // Flag to calculate and output the fracture reactivation potential: this represents the fracture driving stress if positive, and the cohesionless distance to failure if negative
            bool OutputFractureReactivationPotential = false;
            // Flag to calculate and output the bulk rock compliance and stiffness tensors
            bool OutputBulkRockElasticTensors = false;
            // Flag to calculate and output fracture connectivity and anisotropy indices
            bool OutputFractureConnectivityAnisotropy = false;
            // Flag to calculate and output fracture porosity
            bool OutputFracturePorosity = true;
            // Flag to calculate and output fracture permeability tensors
            bool OutputFracturePermeabilityTensor = true;
            // Fracture types included in the fracture permeability tensor: Microfractures only; Layer-bound fractures only; Unconfined fractures only; All fractures
            FractureType FractureTypesInPermeabilityTensor = FractureType.AllFractures;
            // Algorithm to use for calculating fracture permeability
            PermeabilityCalculationAlgorithm PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.SizeConnectivityCorrected;
            // Flag to calculate and output implicit fracture population distribution functions from the FractureGrid object (will only be output if WriteImplicitDataFiles is true)
            bool OutputPopulationDistribution = true;
            // Number of fracture or ray length values to calculate for each of the implicit fracture population distribution functions
            int No_l_indexPoints = 20;
            // MaxHMinLength and MaxHMaxLength control the range of fracture or ray lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
            // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length
            double MaxHMinLength = 0;
            double MaxHMaxLength = 0;
            // Flag to populate implicit fracture data in gridblocks with no fractures?
            // If true, all gridblocks in the specified region of the grid will be populated with implicit fracture data, even if the fracture density is zero; otherwise gridblocks with zero fracture density will not be populated, enabling easier visualisation of the extent of the fracture network
            bool PopulateEmptyGridblocks = true;
            // Depth of horizontal section
            // Set this to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file
            double DepthOfHorizontalSection = double.NaN;

            // Fracture aperture control parameters
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
            FractureApertureType FractureApertureControl = FractureApertureType.Uniform;
            // Fracture aperture control parameters: Uniform fracture aperture
            // Fixed aperture for Mode 1 fractures striking perpendicular to hmin in the uniform aperture case (m)
            double Mode1HMin_UniformAperture = 0.0005;
            // Fixed aperture for Mode 2 fractures striking perpendicular to hmin in the uniform aperture case (m)
            double Mode2HMin_UniformAperture = 0.0005;
            // Fixed aperture for Mode 1 fractures striking perpendicular to hmax in the uniform aperture case (m)
            double Mode1HMax_UniformAperture = 0.0005;
            // Fixed aperture for Mode 2 fractures striking perpendicular to hmax in the uniform aperture case (m)
            double Mode2HMax_UniformAperture = 0.0005;
            // Fracture aperture control parameters: SizeDependent fracture aperture
            // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
            double Mode1HMin_SizeDependentApertureMultiplier = 1E-5;
            // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
            double Mode2HMin_SizeDependentApertureMultiplier = 1E-5;
            // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
            double Mode1HMax_SizeDependentApertureMultiplier = 1E-5;
            // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
            double Mode2HMax_SizeDependentApertureMultiplier = 1E-5;
            // Fracture aperture control parameters: Dynamic fracture aperture
            // Multiplier for dynamic aperture
            double DynamicApertureMultiplier = 1;
            // Fracture aperture control parameters: Barton-Bandis model for fracture aperture
            // Joint Roughness Coefficient
            double JRC = 10;
            // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock
            double UCSRatio = 2;
            // Initial normal stress on fracture (Pa)
            double InitialNormalStress = 2E+5;
            // Stiffness normal to the fracture, at initial normal stress (Pa/m)
            double FractureNormalStiffness = 2.5E+9;
            // Maximum fracture closure (m)
            double MaximumClosure = 0.0005;

            // Present day effective stress parameters
            // Define the present day stress tensor and fluid pressure to override stress at the time of deformation when calculating fracture aperture and permeability
            // Present day stress tensor and fluid pressure should be specified in Pa as either AbsoluteStress, TerzaghiEffectiveStress or BiotEffectiveStress tensors, depending on the PresentDayStressDefinition flag
            bool UsePresentDayStress = false;
            StressStateDefinition PresentDayStressDefinition = StressStateDefinition.TerzaghiEffectiveStress;
            double DefaultPresentDayStress_XX = 0;
            double DefaultPresentDayStress_YY = 0;
            double DefaultPresentDayStress_ZZ = 0;
            double DefaultPresentDayStress_XY = 0;
            double DefaultPresentDayStress_YZ = 0;
            double DefaultPresentDayStress_ZX = 0;
            double DefaultPresentDayFluidPressure = 0;
            string PresentDayStress_XXPropertyName = string.Empty;
            string PresentDayStress_YYPropertyName = string.Empty;
            string PresentDayStress_ZZPropertyName = string.Empty;
            string PresentDayStress_XYPropertyName = string.Empty;
            string PresentDayStress_YZPropertyName = string.Empty;
            string PresentDayStress_ZXPropertyName = string.Empty;
            string PresentDayFluidPressurePropertyName = string.Empty;
            // It is also possible to overridde the Biot Coefficient when calculating present day effective stress
            double DefaultPresentDayBiotCoefficient = double.NaN;
            string PresentDayBiotCoefficientPropertyName = string.Empty;

            // Calculation control parameters
            // Number of layer-bound fracture sets
            // Set to 1 to generate a single fracture set, perpendicular to ehmin
            // Set to 2 to generate two orthogonal fracture sets, perpendicular to ehmin and ehmax; this is typical of a single stage of deformation in intact rock
            // Set to 6 or more to generate oblique fractures; this is typical of multiple stages of deformation with fracture reactivation, or transtensional strain
            int NoLayerBoundFractureSets = 2;
            // Fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal
            bool Mode1Only = false;
            bool Mode2Only = false;
            // Position of fracture nucleation within the layer; set to 0 to force all fractures to nucleate at the base of the layer and 1 to force all fractures to nucleate at the top of the layer; set to -1 to nucleate fractures at random locations within the layer
            double FractureNucleationPosition = -1;
            // Flag to check microfractures against stress shadows of all macrofractures, regardless of set
            // If None, microfractures will only be deactivated if they lie in the stress shadow zone of parallel macrofractures
            // If All, microfractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular macrofractures, depending on the strain tensor
            // If Automatic, microfractures in the stress shadow zone of oblique or perpendicular macrofractures will be deactivated only if there are more than two fracture sets
            AutomaticFlag CheckAlluFStressShadows = AutomaticFlag.Automatic;
            // Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes
            // For now we will set this to 1 (always use isotropic method) as this seems to give more reliable results
            double AnisotropyCutoff = 1;
            // Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow
            bool AllowReverseFractures = false;
            // Maximum duration for individual timesteps; set to -1 for no maximum timestep duration
            double MaxTimestepDuration = -1;
            // Maximum increase in MFP33 allowed in each timestep - controls the optimal timestep duration
            // Increase this to run calculation faster, with fewer but longer timesteps
            double MaxTimestepMFP33Increase = 0.005;
            // Minimum radius for microfractures to be included in implicit fracture density and porosity calculations
            // If this is set to 0 (i.e. include all microfractures) then it will not be possible to calculate volumetric microfracture density as this will be infinite
            // If this is set to -1 the maximum radius of the smallest bin will be used (i.e. exclude the smallest bin from the microfracture population)
            double MinImplicitMicrofractureRadius = -1;
            // Number of bins used in numerical integration of uFP32
            // This controls accuracy of numerical calculation of microfracture populations - increase this to increase accuracy of the numerical integration at expense of runtime 
            int No_r_bins = 10;
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
            // To stop calculation while fractures are still growing reduce the DeformationEpisodeDuration (in the deformation load inputs) or MaxTimesteps limits
            // Ratio of current to peak active macrofracture volumetric ratio at which fracture sets are considered inactive; set to negative value to switch off this control
            double Current_HistoricMFP33TerminationRatio = -1;// 0.01;
            // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
            double Active_TotalMFP30TerminationRatio = -1;// 0.01;
            // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
            double MinimumClearZoneVolume = 0.01;
            // Use the deformation episode duration (set in the deformation load inputs) or the maximum timestep limit to stop the calculation before fractures have finished growing
            int MaxTimesteps = 1000;
            // DFN geometry controls
            // Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated
            bool GenerateExplicitDFN = true;
            // Set false to allow fractures to propagate outside of the outer grid boundary
            bool CropAtBoundary = true;
            // Flag to ignore faults when propagating fractures; if set to true, fractures will be able to propagate across faults
            // By default fractures will terminate if they intersect a fault at a gridblock boundary
            // This can cause problems with stairstep grids contining inclined faults or faults at different depths, as any pillar intersecting a fault will cause fractures to terminate, even if the fault does not cut the layer containing the fractures
            // Therefore this flag can be set to ignore the faults and allow fractures to propagate across all gridblock boundaries
            bool IgnoreFaults = false;
            // Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment
            bool LinkStressShadows = false;
            // Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
            // Set to Pi/4 rad (45 degrees) by default
            double MaxConsistencyAngle = Math.PI / 4;
            // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value
            // Set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
            double MinimumLayerThickness = 0;
            // Maximum number of fracture patches that can be generated per gridblock
            // Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason
            int MaxNoFracturePatches = 1000;
            // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small
            // Set to 0 to disable probabilistic fracture nucleation
            // Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)
            double ProbabilisticFractureNucleationLimit = -1;
            // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
            // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
            bool PropagateFracturesInNucleationOrder = true;
            // Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
            AutomaticFlag SearchAdjacentGridblocks = AutomaticFlag.Automatic;
            // Minimum radius for microfractures to be included in explicit DFN
            // Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN
            double MinExplicitMicrofractureRadius = 0;
            // Number of cornerpoints defining the microfracture polygons in the explicit DFN
            // Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints
            int Number_uF_Points = 8;

            // Parameters for controlling unconfined fractures
            // Use NoUnconfinedFractureStrikeSets and NoUnconfinedFractureDipSets to create unconfined fracture sets, which can propagate and interact vertically as well as horizontally
            // These are useful for modelling fractures in thick geobodies such as igneous plutons
            // NB Unconfined fracture sets are not subdivided into dipsets; unconfined fractures with the same strike but different dips are counted as different sets
            // The total number of unconfined fracture sets generates will therefore be given by NoUnconfinedFractureStrikeSets * NoUnconfinedFractureDipSets
            int NoUnconfinedFractureStrikeSets = 0;
            int NoUnconfinedFractureDipSets = 0;
            int NoUnconfinedFractureSets = 0;
            List<string> UnconfinedFractureSetNames = new List<string>();
            // Number of rays comprising each unconfined fracture
            int NoRaysPerUnconfinedFracture = 16;// 8;
            // Minimum radius for unconfined fractures; this will be the length of the rays at nucleation
            // If set to -1, will use 0.01 * layer thickness
            double MinUnconfinedFractureRadius = 10;// -1;
            // Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length
            // If set to -1, will use 0.5 * layer thickness
            double MaxUnconfinedFractureRadius = 1000;// -1;
            // Maximum allowed effective radius for unconfined fractures; will limit fracture stress shadow and propagation rate but not fracture growth
            // If set to -1, there will be no limit on effective fracture radius
            double MaxEffectiveUnconfinedFractureRadius = MaxUnconfinedFractureRadius;
            // Calculation termination controls
            // The calculation is set to stop automatically when fractures stop growing
            // This can be defined in one of three ways:
            //      - When the total volumetric ratio of active (propagating)  unconfined fractures (a_UCFP33) drops below a specified proportion of the peak historic value
            //      - When the total volumetric density of active (propagating) unconfined fracture rays (a_UCRP30) drops below a specified proportion of the total (propagating and non-propagating) volumetric density (UCRP30)
            //      - When the total clear zone volume (the volume in which fractures can nucleate without falling within or overlapping a stress shadow) drops below a specified proportion of the total volume
            //      - When the mean static unconfined fracture ray length drops below a specified value (this prevents brecciation - large numbers of small UCFs terminating against each other, filling the voids between stress shadows of larger UCFs)
            // Increase these cutoffs to reduce the sensitivity and stop the calculation earlier
            // Use this to prevent a long calculation tail - i.e. late timesteps where fractures have stopped growing so they have no impact on fracture populations, just increase runtime
            // To stop calculation while fractures are still growing reduce the DeformationEpisodeDuration (in the deformation load inputs) or MaxTimesteps limits
            // Ratio of current to peak active unconfined fracture mean linear density at which fracture sets are considered inactive; set to negative value to switch off this control
            double Current_HistoricUCFP32TerminationRatio = -1;// 0.01;
            // Ratio of active to total unconfined fracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
            double Active_TotalUCRP30TerminationRatio = -1;// 0.01;
            // Minimum required clear zone volume in which unconfined fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
            double MinimumUCFClearZoneVolume = 0.2;
            // Minimum allowed mean static unconfined fracture ray length; if the mean static ray length drops below this value, the fracture set will be deactivated; set to 0 for no limit and -1 to use the minimum UCF radius
            double MinimumStaticUCRLength = -1;
            // Maximum increase in UCFP33 allowed in each timestep - controls the optimal timestep duration
            // Increase this to run calculation faster, with fewer but longer timesteps
            double MaxTimestepUCFP33Increase = 0.005;// 0.01;
            // Maximum proportional increase in the unconfined fracture ray length in each timestep (controls speed and accuracy of calculation)
            // Set to -1 for no limit 
            double MaxTimestepRadiusIncrease = 0.2;// double.NaN;// 0.05;//
            // Maximum proportional increase in the radius of the unconfined fractures before checking for fracture deactivation (controls number of implicit fracture population datapoints generated)
            double Max_R_DeactivationCheck_interval = 0.2;
            // Minimum activation probability for unconfined fractures; if the activation probability drops below this, the specified proportion of fractures will be deactivated, creating a new implicit fracture population datapoint
            double Min_R_ActivationProbability = 0.8;
            // The proportion of the ray length increment to apply to active unconfined fracture datapoints before the specified proportion of fractures are deactivated
            // Set to -1 to use the mean distance that a fracture propagates before being deactivated
            double ProportionalUCRIncrementToApply = -1;
            // Minimum proportional size difference for static unconfined fracture datapoints; any datapoints with less than this proportional size difference may be amalgamated into a single point
            double Min_R_StaticDatapointSizeRatio = 0.02;
            // Frequency (in timesteps) with which static unconfined fracture datapoints are culled
            int CullTSFrequency = 10;
            // Flag to calculate implicit data for unconfined fractures; if set to false no grid properties will be generated, only an explicit DFN; does not affect layer-bound fractures
            bool CalculateImplicitUCFData = true;
            // Flag to check unconfined fractures against stress shadows of all other unconfined fractures, regardless of set
            // If None, unconfined fractures will only be deactivated if they lie in the stress shadow zone of parallel unconfined fractures
            // If All, unconfined fractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular unconfined fractures, depending on the strain tensor
            // If Automatic, unconfined fractures in the stress shadow zone of oblique or perpendicular unconfined fractures will be deactivated only if there are more than two fracture sets
            AutomaticFlag CheckAllUCFStressShadows = AutomaticFlag.Automatic;
            // Flag to make unconfined fractures completely planar, even when crossing gridblock boundaries
            bool PlanarUnconfinedFractures = false;
            // Minimum radius for large fractures; fractures larger than this will be considered to influence the entire grid when checking stress shadows
            double LargeFractureMinimumRadius = double.NaN;
            // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius
            double MinStressShadowDeactivationRatio = 0.5;
            // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius
            double MinIntersectionDeactivationRatio = 0.5;
            // Flag to filter cells by property; if true, cells with property values outside the specified range will not be included in the model
            bool FilterByProperty = false;
            // Property to filter cells by; cells with property values outside the specified range will not be included in the model
            string PropertyToFilter = string.Empty;
            // Minimum cutoff for the property filter; cells where the spcified property value is lower than this will not be included in the model
            double FilterByPropertyMinCutoff = double.NaN;
            // Maximum cutoff for the property filter; cells where the spcified property value is higher than this will not be included in the model
            double FilterByPropertyMaxCutoff = double.NaN;

            // Create a random number generator for randomising properties, if required
            Random RandomNumberGenerator = new Random();

#if READINPUTFROMFILE
            // Read default values from input file
            Console.WriteLine("Reading the DFMGenerator configuration file...");
            string[] inputfile_lines = File.ReadAllLines(inputfile_name);
            // NB we will create a list of gridblock overrides and include files - they will be processed later
            List<List<string>> GBoverrides = new List<List<string>>();
            List<string> CurrentGBoverride = new List<string>();
            List<string> IncludeFiles = new List<string>();
            bool GBoverride = false;
            foreach (string line in inputfile_lines)
            {
                // Ignore comment lines
                if (line.StartsWith("%"))
                    continue;

                // Split the line into components and echo to console
                // All lines should have at least 2 components; if not we can ignore them
                string[] line_split = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (line_split.Length < 2)
                    continue;

                // Check if we are in a gridblock override block, and if so add the line to the list of gridblock overrides - we will deal with these later
                if (GBoverride)
                {
                    CurrentGBoverride.Add(line);
                    if (line_split[0] == "End")
                    {
                        GBoverride = false;
                        GBoverrides.Add(CurrentGBoverride);
                    }

                    continue;
                }
                else if (line_split[0] == "Gridblock")
                {
                    GBoverride = true;
                    CurrentGBoverride = new List<string>();
                    CurrentGBoverride.Add(line);
                    continue;
                }
                else
                {
                    string line_echo = line_split[0] + " " + line_split[1];
                    for (int line_component = 2; line_component < line_split.Length; line_component++)
                        line_echo += " " + line_split[line_component];
                    Console.WriteLine(line_echo);
                }

                // Check if this is an include statement and if so add to the list - we will deal with this later
                if (line_split[0] == "Include")
                {
                    IncludeFiles.Add(line_split[1]);
                    continue;
                }

                // Now we can process specified universal or default values
                // Catch any exceptions due to invalid data formats
                try
                {
                    switch (line_split[0])
                    {
                        // Main properties
                        // Model name, used to name output files; if not specified, will be taken from the input file name
                        case "ModelName":
                            ModelName = line_split[1];
                            break;
                        // Grid size
                        case "GridFileName":
                            GridFileName = line_split[1];
                            break;
                        // Subset of rows and columns from the shadow grid to include in the fracture grid (indexed from 1)
                        // Following GRDECL format, columns are indexed from west to east but rows are indexed in reverse order, from north to south
                        // Set to -1 to include all rows and columns
                        case "StartColumnI":
                            ShadowGrid_StartColI = Convert.ToInt32(line_split[1]);
                            break;
                        case "EndColumnI":
                            ShadowGrid_EndColI = Convert.ToInt32(line_split[1]);
                            break;
                        case "StartRowJ":
                            ShadowGrid_StartRowJ = Convert.ToInt32(line_split[1]);
                            break;
                        case "EndRowJ":
                            ShadowGrid_EndRowJ = Convert.ToInt32(line_split[1]);
                            break;
                        // Subset of layers from the shadow grid to include in the fracture grid (indexed from 1)
                        // Following GRDECL format, layers are indexed from top to bottom
                        // Set to -1 to include all layers
                        case "TopLayerK":
                            ShadowGrid_TopLayerK = Convert.ToInt32(line_split[1]);
                            break;
                        case "BottomLayerK":
                            ShadowGrid_BottomLayerK = Convert.ToInt32(line_split[1]);
                            break;
                        // Horizontal upscaling factor - used to amalgamate multiple shadow grid cells into one fracture gridblock
                        case "HorizontalUpscalingFactor":
                            HorizontalUpscalingFactor = Convert.ToInt32(line_split[1]);
                            break;
                        // Vertical upscaling factor - used to amalgamate multiple shadow grid layers into one fracture grid layer
                        // Set to 0 to amalgamate all selected shadow grid layers into a single fracture grid layer
                        // Set to 1 to create a fracture grid layer for each selected shadow grid layer
                        case "VerticalUpscalingFactor":
                            VerticalUpscalingFactor = Convert.ToInt32(line_split[1]);
                            break;
                        // Time units used in input load rates, time limits and strain relaxation time constants
                        // These will be converted to SI units (s) by the gridblock objects
                        case "ModelTimeUnits":
                            {
                                if (line_split[1] == "ma")
                                    ModelTimeUnits = TimeUnits.ma;
                                else if (line_split[1] == "year")
                                    ModelTimeUnits = TimeUnits.year;
                                else if (line_split[1] == "second")
                                    ModelTimeUnits = TimeUnits.second;
                            }
                            break;
                        // Deformation load
                        // Here we will populate the global deformation load parameter lists
                        // These contain one entry for each deformation episode, in order
                        // They will be copied to all gridblocks
                        // Each deformation episode can be assigned a name; if this is not specified, a default name will be generated
                        case "DeformationEpisodeName":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    DeformationEpisodeName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Strain orientatation
                        case "DefaultEhminAzi":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        DefaultEhminAzi_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        DefaultEhminAzi_list.Add(DefaultEhminAzi);
                                    }
                                }
                            }
                            break;
                        case "EhminAziProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    EhminAziPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Strain rates
                        // Set to negative value for extensional strain - this is necessary in at least one direction to generate fractures
                        // With no strain relaxation, strain rate will control rate of horizontal stress increase
                        // With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant horizontal stress
                        // Ehmin is most tensile (i.e. most negative) horizontal strain rate
                        case "DefaultEhminRate":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        DefaultEhminRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        DefaultEhminRate_list.Add(DefaultEhminRate);
                                    }
                                }
                            }
                            break;
                        case "EhminRateProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    EhminRatePropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern
                        case "DefaultEhmaxRate":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        DefaultEhmaxRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        DefaultEhmaxRate_list.Add(DefaultEhmaxRate);
                                    }
                                }
                            }
                            break;
                        case "EhmaxRateProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    EhmaxRatePropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Fluid pressure, thermal and uplift loads
                        // Rate of increase of fluid overpressure (Pa/ModelTImeUnit)
                        case "DefaultAppliedOverpressureRate":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        DefaultAppliedOverpressureRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        DefaultAppliedOverpressureRate_list.Add(DefaultAppliedOverpressureRate);
                                    }
                                }
                            }
                            break;
                        case "AppliedOverpressureRateProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    AppliedOverpressureRatePropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Rate of in situ temperature change (not including cooling due to uplift) (degK/ModelTImeUnit)
                        case "DefaultAppliedTemperatureChange":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        DefaultAppliedTemperatureChange_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        DefaultAppliedTemperatureChange_list.Add(DefaultAppliedTemperatureChange);
                                    }
                                }
                            }
                            break;
                        case "AppliedTemperatureChangeProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    AppliedTemperatureChangePropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/ModelTImeUnit)
                        case "DefaultAppliedUpliftRate":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        DefaultAppliedUpliftRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        DefaultAppliedUpliftRate_list.Add(DefaultAppliedUpliftRate);
                                    }
                                }
                            }
                            break;
                        case "AppliedUpliftRateProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    AppliedUpliftRatePropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)
                        case "StressArchingFactor":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                {
                                    // If the specified value is not a valid number, replace it with the default
                                    // This keeps the list index in sync with the items in the input data 
                                    try
                                    {
                                        StressArchingFactor_list.Add(Convert.ToDouble(line_split[valueNo]));
                                    }
                                    catch
                                    {
                                        StressArchingFactor_list.Add(StressArchingFactor);
                                    }
                                }
                            }
                            break;
                        // Parameters to specify dynamic deformation loads, defined in terms of stress rather than strain
                        // If these parameters are specified, they will override the strain, fluid overpressure, thermal and uplift loads
                        // If only the ZZ stress component is specified, it will be used to calculate the lithostatic stress instead of depth and overburden density but the strain and thermal loads will used to define horizontal stress
                        // If the XX, YY, ZZ and XY stress components are specified, a horizontally symmetric stress tensor will be generated (YZ and ZX components set to 0) which will override the strain, thermal and uplift loads
                        // If all 6 stress components are specified, a full stress tensor will be generated which will override the strain, thermal and uplift loads
                        // If the fluid pressure is specified this will replace the hydrostatic fluid pressure and override the fluid overpressure load
                        // Dynamic loads should be specified in Pa as either AbsoluteStress, TerzaghiEffectiveStress or BiotEffectiveStress tensors, depending on the StressLoadDefinition flag
                        case "StressLoadDefinition":
                            {
                                if (line_split[1] == "AbsoluteStress")
                                    StressDefinition = StressStateDefinition.AbsoluteStress;
                                else if (line_split[1] == "TerzaghiEffectiveStress")
                                    StressDefinition = StressStateDefinition.TerzaghiEffectiveStress;
                                else if (line_split[1] == "BiotEffectiveStress")
                                    StressDefinition = StressStateDefinition.BiotEffectiveStress;
                            }
                            break;
                        case "StressXXProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    SxxPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        case "StressYYProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    SyyPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        case "StressZZProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    SzzPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        case "StressXYProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    SxyPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        case "StressYZProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    SyzPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        case "StressZXProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    SzxPropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        case "FluidPressureProperty":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    FluidPressurePropertyName_list.Add(line_split[valueNo]);
                            }
                            break;
                        // Set DeformationEpisodeDuration to -1 to continue until fracture saturation is reached
                        case "DeformationEpisodeDuration":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    DeformationEpisodeDuration_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;

                        // Mechanical properties
                        case "DefaultYoungsMod":
                            DefaultYoungsMod = Convert.ToDouble(line_split[1]);
                            break;
                        case "YoungsModProperty":
                            YoungsModPropertyName = line_split[1];
                            break;
                        case "DefaultPoissonsRatio":
                            DefaultPoissonsRatio = Convert.ToDouble(line_split[1]);
                            break;
                        case "PoissonsRatioProperty":
                            PoissonsRatioPropertyName = line_split[1];
                            break;
                        case "DefaultPorosity":
                            DefaultPorosity = Convert.ToDouble(line_split[1]);
                            break;
                        case "PorosityProperty":
                            PorosityPropertyName = line_split[1];
                            break;
                        case "DefaultBiotCoefficient":
                            DefaultBiotCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        case "BiotCoefficientProperty":
                            BiotCoefficientPropertyName = line_split[1];
                            break;
                        case "DefaultThermalExpansionCoefficient":
                            DefaultThermalExpansionCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        case "ThermalExpansionCoefficientProperty":
                            ThermalExpansionCoefficientPropertyName = line_split[1];
                            break;
                        case "DefaultCrackSurfaceEnergy":
                            DefaultCrackSurfaceEnergy = Convert.ToDouble(line_split[1]);
                            break;
                        case "CrackSurfaceEnergyProperty":
                            CrackSurfaceEnergyPropertyName = line_split[1];
                            break;
                        case "DefaultFrictionCoefficient":
                            DefaultFrictionCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        case "FrictionCoefficientProperty":
                            FrictionCoefficientPropertyName = line_split[1];
                            break;
                        // Strain relaxation data
                        // Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate
                        case "DefaultRockStrainRelaxation":
                            DefaultRockStrainRelaxation = Convert.ToDouble(line_split[1]);
                            break;
                        case "RockStrainRelaxationProperty":
                            RockStrainRelaxationPropertyName = line_split[1];
                            break;
                        // Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only
                        case "DefaultFractureRelaxation":
                            DefaultFractureRelaxation = Convert.ToDouble(line_split[1]);
                            break;
                        case "FractureRelaxationProperty":
                            FractureRelaxationPropertyName = line_split[1];
                            break;
                        // Initial microfracture distribution function
                        // For unconfined fractures this can be set to PowerLaw, Exponential or LogNormal
                        // For layer-bound fractures it is assumed to be power law
                        case "InitialMicrofractureDistributionFunction":
                            {
                                if (line_split[1] == "PowerLaw")
                                    InitialMicrofractureDistributionFunction = InitialFractureDistribution.PowerLaw;
                                else if (line_split[1] == "Exponential")
                                    InitialMicrofractureDistributionFunction = InitialFractureDistribution.Exponential;
                                else if (line_split[1] == "LogNormal")
                                    InitialMicrofractureDistributionFunction = InitialFractureDistribution.LogNormal;
                            }
                            break;
                        // Density of initial microfractures
                        case "DefaultInitialMicrofractureDensity":
                            DefaultInitialMicrofractureDensity = Convert.ToDouble(line_split[1]);
                            break;
                        case "InitialMicrofractureDensityProperty":
                            InitialMicrofractureDensityPropertyName = line_split[1];
                            break;
                        // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
                        case "DefaultInitialMicrofractureSizeDistribution":
                            DefaultInitialMicrofractureSizeDistribution = Convert.ToDouble(line_split[1]);
                            break;
                        case "InitialMicrofractureSizeDistributionProperty":
                            InitialMicrofractureSizeDistributionPropertyName = line_split[1];
                            break;
                        // Median initial microfracture radius - this is only used for the log-normal distribution function
                        // If undefined, will be set to layer thickness / 20
                        case "DefaultInitialMicrofractureMedianRadius":
                            DefaultInitialMicrofractureMedianRadius = Convert.ToDouble(line_split[1]);
                            break;
                        case "InitialMicrofractureMedianRadiusPropertyName":
                            InitialMicrofractureMedianRadiusPropertyName = line_split[1];
                            break;
                        // Subritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
                        case "DefaultSubcriticalPropIndex":
                            DefaultSubcriticalPropIndex = Convert.ToDouble(line_split[1]);
                            break;
                        case "SubcriticalPropIndexProperty":
                            SubcriticalPropIndexPropertyName = line_split[1];
                            break;
                        case "CriticalPropagationRate":
                            CriticalPropagationRate = Convert.ToDouble(line_split[1]);
                            break;
                        // Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity
                        case "DefaultHostRock_kh":
                            DefaultHostRock_kh = Convert.ToDouble(line_split[1]);
                            break;
                        case "HostRock_khProperty":
                            HostRock_khPropertyName = line_split[1];
                            break;
                        case "DefaultHostRock_kv":
                            DefaultHostRock_kv = Convert.ToDouble(line_split[1]);
                            break;
                        case "HostRock_kvProperty":
                            HostRock_kvPropertyName = line_split[1];
                            break;
                        // Flag for whether to average mechanical properties properties across the shadow grid cells, or take the value from the top middle cell
                        case "AverageMechanicalPropertyData":
                            AverageMechanicalPropertyData = (line_split[1] == "true");
                            break;
  
                        // Cleavages: Cleavage planes reduce the crack surface energy and/or friction coefficient on fractures parallel to the specified cleavage orientation");
                        // Multiple cleavage planes can be defined; for each one, the crack surface energy and/or friction coefficient on the fracture set with the closest orientation (within the specified MaxConsistencyAngle) will be modified accordingly");
                        // If there is no fracture set within the specified MaxConsistencyAngle, the cleavage plane will have no effect");
                        case "Cleavage":
                            {
                                int noValues = line_split.GetLength(0);
                                // We can only add a cleavage if at least three values are specified (azimuth, dip, crack surface energy); optionally friction coefficient can also be defined
                                if (noValues > 4)
                                {
                                    // The azimuth must be converted to a strike
                                    double azimuth = Convert.ToDouble(line_split[1]);
                                    double dip = Convert.ToDouble(line_split[2]);
                                    double CSE = Convert.ToDouble(line_split[3]);
                                    double MuFr = Convert.ToDouble(line_split[4]);

                                    // If both strike and dip are valid numbers, the cleavage plane is valid - add the default values to the appropriate lists
                                    if (!double.IsNaN(azimuth) && !double.IsNaN(dip))
                                    {
                                        Cleavage_Defined.Add(true);
                                        Default_Cleavage_Azimuth.Add(azimuth);
                                        Default_Cleavage_Dip.Add(dip);
                                        Default_Cleavage_CrackSurfaceEnergy.Add(CSE);
                                        Default_Cleavage_FrictionCoefficient.Add(MuFr);

                                        // If property names have also been specified, add these to the appropriate lists
                                        if (noValues > 8)
                                        {
                                            Cleavage_Azimuth_PropertyName.Add(line_split[5]);
                                            Cleavage_Dip_PropertyName.Add(line_split[6]);
                                            Cleavage_CrackSurfaceEnergy_PropertyName.Add(line_split[7]);
                                            Cleavage_FrictionCoefficient_PropertyName.Add(line_split[8]);
                                        }
                                        // Otherwise add empty strings
                                        else
                                        {
                                            Cleavage_Azimuth_PropertyName.Add(string.Empty);
                                            Cleavage_Dip_PropertyName.Add(string.Empty);
                                            Cleavage_CrackSurfaceEnergy_PropertyName.Add(string.Empty);
                                            Cleavage_FrictionCoefficient_PropertyName.Add(string.Empty);
                                        }

                                        // Update the number of cleavage planes
                                        NoCleavages++;
                                    }
                                }
                            }
                            break;

                        // Stress state
                        // Stress distribution scenario - use to turn on or off stress shadow effect
                        // Do not use DuctileBoundary as this is not yet implemented
                        case "StressDistributionScenario":
                            {
                                if (line_split[1] == "EvenlyDistributedStress")
                                    StressDistributionScenario = StressDistribution.EvenlyDistributedStress;
                                else if (line_split[1] == "StressShadow")
                                    StressDistributionScenario = StressDistribution.StressShadow;
                                else if (line_split[1] == "DuctileBoundary")
                                    StressDistributionScenario = StressDistribution.DuctileBoundary;
                            }
                            break;
                        // Depth at the start of deformation (in metres, positive downwards) - this will control stress state
                        // If DepthAtDeformation is specified, this will be used to calculate vertical stress
                        // If DepthAtDeformation is <=0 or NaN, the depth at the start of deformation will be set to the current depth plus total specified uplift
                        case "DefaultDepthAtDeformation":
                            DefaultDepthAtDeformation = Convert.ToDouble(line_split[1]);
                            break;
                        case "DepthAtDeformationProperty":
                            DepthAtDeformationPropertyName = line_split[1];
                            break;
                        // Mean density of overlying sediments and fluid (kg/m3)
                        case "MeanOverlyingSedimentDensity":
                            MeanOverlyingSedimentDensity = Convert.ToDouble(line_split[1]);
                            break;
                        case "FluidDensity":
                            FluidDensity = Convert.ToDouble(line_split[1]);
                            break;
                        // Fluid overpressure (Pa)
                        case "InitialOverpressure":
                            InitialOverpressure = Convert.ToDouble(line_split[1]);
                            break;
                        // Geothermal gradient (degK/m)
                        case "GeothermalGradient":
                            GeothermalGradient = Convert.ToDouble(line_split[1]);
                            break;
                        // InitialStressRelaxation controls the initial horizontal stress, prior to the application of horizontal strain
                        // Set InitialStressRelaxation to 1 to have initial horizontal stress = vertical stress (viscoelastic equilibrium)
                        // Set InitialStressRelaxation to 0 to have initial horizontal stress = v/(1-v) * vertical stress (elastic equilibrium)
                        // Set InitialStressRelaxation to -1 for initial horizontal stress = Mohr-Coulomb failure stress (critical stress state)
                        case "InitialStressRelaxation":
                            InitialStressRelaxation = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag for whether to average stress and strain data across the shadow grid cells, or take the value from the top middle cell
                        case "AverageStressStrainData":
                            AverageStressStrainData = (line_split[1] == "true");
                            break;

                        // Outputs
                        // Output to file from within FractureGrid object
                        // This will generate one file of implicit data per gridblock, and one DFN file per output stage
                        // Output files from the FractureGrid object can be useful for debugging or detailed analysis of fracture growth
                        case "WriteImplicitDataFiles":
                            WriteImplicitDataFiles = (line_split[1] == "true");
                            break;
                        case "WriteDFNFiles":
                            WriteDFNFiles = (line_split[1] == "true");
                            break;
                        // Output file type for explicit DFN data: ASCII or FAB (NB FAB files can be loaded directly into Petrel)
                        case "OutputDFNFileType":
                            {
                                if (line_split[1] == "ASCII")
                                    OutputDFNFileType = DFNFileType.ASCII;
                                else if (line_split[1] == "FAB")
                                    OutputDFNFileType = DFNFileType.FAB;
                            }
                            break;
                        // Flag to write implicit fracture data to a series of GRDECL files
                        // A separate GRDECL file will be generated for each output stage
                        // These files can include the grid geometry and output properties, or only the output properties
                        case "WriteGRDECLFiles":
                            WriteGRDECLFiles = (line_split[1] == "true");
                            break;
                        case "IncludeGridGeometryInGRDECLFiles":
                            IncludeGridGeometryInGRDECLFiles = (line_split[1] == "true");
                            break;
                        // Flag to write explicit DFN data to a series of FAB files
                        // A separate FAB file will be generated for each output stage
                        case "WriteFABFiles":
                            WriteFABFiles = (line_split[1] == "true");
                            break;
                        // Output DFM at intermediate stages of fracture growth
                        case "NoIntermediateOutputs":
                            NoIntermediateOutputs = Convert.ToInt32(line_split[1]);
                            break;
                        // Flag to control interval between output of intermediate stage DFMs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area
                        case "IntermediateOutputIntervalControl":
                            {
                                if (line_split[1] == "EqualArea")
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualArea;
                                else if (line_split[1] == "EqualTime")
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualTime;
                                else if (line_split[1] == "SpecifiedTime")
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.SpecifiedTime;
                                else
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualArea;
                            }
                            break;
                        // Flag to output the layer-bound fracture centrepoints and the unconfined fracture rays as polylines
                        case "OutputCentrepoints":
                            OutputCentrepoints = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output density, mean length and connectivity data (if selected) for individual fracture sets
                        case "OutputFractureSets":
                            OutputFractureSets = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output the fracture reactivation potential: this represents the fracture driving stress if positive, and the cohesionless distance to failure if negative
                        case "OutputFractureReactivationPotential":
                            OutputFractureReactivationPotential = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output the bulk rock compliance and stiffness tensors
                        case "OutputBulkRockElasticTensors":
                            OutputBulkRockElasticTensors = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture connectivity and anisotropy indices
                        case "OutputFractureConnectivityAnisotropy":
                            OutputFractureConnectivityAnisotropy = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture porosity
                        case "OutputFracturePorosity":
                            OutputFracturePorosity = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture permeability tensors
                        case "OutputFracturePermeabilityTensor":
                            OutputFracturePermeabilityTensor = (line_split[1] == "true");
                            break;
                        // Fracture types included in the fracture permeability tensor: Microfractures only; Layer-bound fractures only; Unconfined fractures only; All fractures
                        case "FractureTypesInPermeabilityTensor":
                            if (line_split[1] == "Microfractures")
                                FractureTypesInPermeabilityTensor = FractureType.Microfractures;
                            else if (line_split[1] == "LayerBoundFractures")
                                FractureTypesInPermeabilityTensor = FractureType.LayerBoundFractures;
                            else if (line_split[1] == "AllFractures")
                                FractureTypesInPermeabilityTensor = FractureType.AllFractures;
                            break;
                        // Algorithm to use for calculating fracture permeability
                        case "PermeabilityAlgorithm":
                            if (line_split[1] == "Oda1986")
                                PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.Oda1986;
                            else if (line_split[1] == "OdaCorrected1987")
                                PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.OdaCorrected1987;
                            else if (line_split[1] == "SizeConnectivityCorrected")
                                PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.SizeConnectivityCorrected;
                            break;
                        // Flag to calculate and output implicit fracture population distribution functions from the FractureGrid object (will only be output if WriteImplicitDataFiles is true)
                        case "OutputPopulationDistribution":
                            OutputPopulationDistribution = (line_split[1] == "true");
                            break;
                        // Number of fracture or ray length values to calculate for each of the implicit fracture population distribution functions
                        case "No_l_indexPoints":
                            No_l_indexPoints = Convert.ToInt32(line_split[1]);
                            break;
                        // MaxHMinLength and MaxHMaxLength control the range of fracture or ray lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
                        // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length
                        case "MaxHMinLength":
                            MaxHMinLength = Convert.ToDouble(line_split[1]);
                            break;
                        case "MaxHMaxLength":
                            MaxHMaxLength = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag to populate implicit fracture data in gridblocks with no fractures?
                        // If true, all gridblocks in the specified region of the grid will be populated with implicit fracture data, even if the fracture density is zero; otherwise gridblocks with zero fracture density will not be populated, enabling easier visualisation of the extent of the fracture network
                        case "PopulateEmptyGridblocks":
                            PopulateEmptyGridblocks = (line_split[1] == "true");
                            break;
                        // Depth of horizontal section
                        // Set this to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file
                        case "DepthOfHorizontalSection":
                            DepthOfHorizontalSection = Convert.ToDouble(line_split[1]);
                            break;

                        // Fracture aperture control parameters
                        // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
                        case "FractureApertureControl":
                            {
                                if (line_split[1] == "Uniform")
                                    FractureApertureControl = FractureApertureType.Uniform;
                                else if (line_split[1] == "SizeDependent")
                                    FractureApertureControl = FractureApertureType.SizeDependent;
                                else if (line_split[1] == "Dynamic")
                                    FractureApertureControl = FractureApertureType.Dynamic;
                                else if (line_split[1] == "BartonBandis")
                                    FractureApertureControl = FractureApertureType.BartonBandis;
                            }
                            break;
                        // Fracture aperture control parameters: Uniform fracture aperture
                        // Fixed aperture for Mode 1 fractures striking perpendicular to hmin in the uniform aperture case (m)
                        case "Mode1HMin_UniformAperture":
                            Mode1HMin_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fixed aperture for Mode 2 fractures striking perpendicular to hmin in the uniform aperture case (m)
                        case "Mode2HMin_UniformAperture":
                            Mode2HMin_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fixed aperture for Mode 1 fractures striking perpendicular to hmax in the uniform aperture case (m)
                        case "Mode1HMax_UniformAperture":
                            Mode1HMax_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fixed aperture for Mode 2 fractures striking perpendicular to hmax in the uniform aperture case (m)
                        case "Mode2HMax_UniformAperture":
                            Mode2HMax_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fracture aperture control parameters: SizeDependent fracture aperture
                        // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode1HMin_SizeDependentApertureMultiplier":
                            Mode1HMin_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode2HMin_SizeDependentApertureMultiplier":
                            Mode2HMin_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode1HMax_SizeDependentApertureMultiplier":
                            Mode1HMax_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode2HMax_SizeDependentApertureMultiplier":
                            Mode2HMax_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Fracture aperture control parameters: Dynamic fracture aperture
                        // Multiplier for dynamic aperture
                        case "DynamicApertureMultiplier":
                            DynamicApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Fracture aperture control parameters: Barton-Bandis model for fracture aperture
                        // Joint Roughness Coefficient
                        case "JRC":
                            JRC = Convert.ToDouble(line_split[1]);
                            break;
                        // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock
                        case "UCSRatio":
                            UCSRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Initial normal stress on fracture (Pa)
                        case "InitialNormalStress":
                            InitialNormalStress = Convert.ToDouble(line_split[1]);
                            break;
                        // Stiffness normal to the fracture, at initial normal stress (Pa/m)
                        case "FractureNormalStiffness":
                            FractureNormalStiffness = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum fracture closure (m)
                        case "MaximumClosure":
                            MaximumClosure = Convert.ToDouble(line_split[1]);
                            break;

                        // Present day effective stress parameters
                        // Define the present day stress tensor and fluid pressure to override stress at the time of deformation when calculating fracture aperture and permeability
                        // Present day stress tensor and fluid pressure should be specified in Pa as either AbsoluteStress, TerzaghiEffectiveStress or BiotEffectiveStress tensors, depending on the PresentDayStressDefinition flag
                        case "UsePresentDayStress":
                            UsePresentDayStress = (line_split[1] == "true");
                            break;
                        case "PresentDayStressDefinition":
                            {
                                if (line_split[1] == "AbsoluteStress")
                                    PresentDayStressDefinition = StressStateDefinition.AbsoluteStress;
                                else if (line_split[1] == "TerzaghiEffectiveStress")
                                    PresentDayStressDefinition = StressStateDefinition.TerzaghiEffectiveStress;
                                else if (line_split[1] == "BiotEffectiveStress")
                                    PresentDayStressDefinition = StressStateDefinition.BiotEffectiveStress;
                                else if (line_split[1] == "Strain")
                                    PresentDayStressDefinition = StressStateDefinition.Strain;
                            }
                            break;
                        case "DefaultPresentDayStressXX":
                            DefaultPresentDayStress_XX = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayStressYY":
                            DefaultPresentDayStress_YY = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayStressZZ":
                            DefaultPresentDayStress_ZZ = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayStressXY":
                            DefaultPresentDayStress_XY = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayStressYZ":
                            DefaultPresentDayStress_YZ = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayStressZX":
                            DefaultPresentDayStress_ZX = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayFluidPressure":
                            DefaultPresentDayFluidPressure = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayStressXXProperty":
                            PresentDayStress_XXPropertyName = line_split[1];
                            break;
                        case "PresentDayStressYYProperty":
                            PresentDayStress_YYPropertyName = line_split[1];
                            break;
                        case "PresentDayStressZZProperty":
                            PresentDayStress_ZZPropertyName = line_split[1];
                            break;
                        case "PresentDayStressXYProperty":
                            PresentDayStress_XYPropertyName = line_split[1];
                            break;
                        case "PresentDayStressYZProperty":
                            PresentDayStress_YZPropertyName = line_split[1];
                            break;
                        case "PresentDayStressZXProperty":
                            PresentDayStress_ZXPropertyName = line_split[1];
                            break;
                        case "PresentDayFluidPressureProperty":
                            PresentDayFluidPressurePropertyName = line_split[1];
                            break;
                        // It is also possible to overridde the Biot Coefficient when calculating present day effective stress
                        case "DefaultPresentDayBiotCoefficient":
                            DefaultPresentDayBiotCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayBiotCoefficientProperty":
                            PresentDayBiotCoefficientPropertyName = line_split[1];
                            break;

                        // Calculation control parameters
                        // Number of layer-bound fracture sets
                        // Set to 1 to generate a single fracture set, perpendicular to ehmin
                        // Set to 2 to generate two orthogonal fracture sets, perpendicular to ehmin and ehmax; this is typical of a single stage of deformation in intact rock
                        // Set to 6 or more to generate oblique fractures; this is typical of multiple stages of deformation with fracture reactivation, or transtensional strain
                        case "NoLayerBoundFractureSets":
                        case "NoFractureSets": // For backwards compatibility
                            NoLayerBoundFractureSets = Convert.ToInt32(line_split[1]);
                            break;
                        // Fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal
                        case "Mode1Only":
                            Mode1Only = (line_split[1] == "true");
                            break;
                        case "Mode2Only":
                            Mode2Only = (line_split[1] == "true");
                            break;
                        // Position of fracture nucleation within the layer; set to 0 to force all fractures to nucleate at the base of the layer and 1 to force all fractures to nucleate at the top of the layer; set to -1 to nucleate fractures at random locations within the layer
                        case "FractureNucleationPosition":
                            FractureNucleationPosition = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag to check microfractures against stress shadows of all macrofractures, regardless of set
                        // If None, microfractures will only be deactivated if they lie in the stress shadow zone of parallel macrofractures
                        // If All, microfractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular macrofractures, depending on the strain tensor
                        // If Automatic, microfractures in the stress shadow zone of oblique or perpendicular macrofractures will be deactivated only if there are more than two fracture sets
                        case "CheckAlluFStressShadows":
                            {
                                if (line_split[1] == "All")
                                    CheckAlluFStressShadows = AutomaticFlag.All;
                                else if (line_split[1] == "None")
                                    CheckAlluFStressShadows = AutomaticFlag.None;
                                else if (line_split[1] == "Automatic")
                                    CheckAlluFStressShadows = AutomaticFlag.Automatic;
                            }
                            break;
                        // Cutoff value to use the isotropic method for calculating cross-fracture set stress shadow and exclusion zone volumes
                        case "AnisotropyCutoff":
                            AnisotropyCutoff = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag to allow growth of reverse fractures; if set to false, fracture sets with reverse displacement will be deactivated
                        case "AllowReverseFractures":
                            AllowReverseFractures = (line_split[1] == "true");
                            break;
                        // Maximum duration for individual timesteps; set to -1 for no maximum timestep duration
                        case "MaxTimestepDuration":
                            MaxTimestepDuration = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum increase in MFP33 allowed in each timestep - controls the optimal timestep duration
                        // Increase this to run calculation faster, with fewer but longer timesteps
                        case "MaxTimestepMFP33Increase":
                            MaxTimestepMFP33Increase = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum radius for microfractures to be included in implicit fracture density and porosity calculations
                        // If this is set to 0 (i.e. include all microfractures) then it will not be possible to calculate volumetric microfracture density as this will be infinite
                        // If this is set to -1 the maximum radius of the smallest bin will be used (i.e. exclude the smallest bin from the microfracture population)
                        case "MinImplicitMicrofractureRadius":
                            MinImplicitMicrofractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Number of bins used in numerical integration of uFP32
                        // This controls accuracy of numerical calculation of microfracture populations - increase this to increase accuracy of the numerical integration at expense of runtime 
                        case "No_r_bins":
                            No_r_bins = Convert.ToInt32(line_split[1]);
                            break;
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
                        case "Current_HistoricMFP33TerminationRatio":
                            Current_HistoricMFP33TerminationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
                        case "Active_TotalMFP30TerminationRatio":
                            Active_TotalMFP30TerminationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
                        case "MinimumClearZoneVolume":
                            MinimumClearZoneVolume = Convert.ToDouble(line_split[1]);
                            break;
                        // Use the deformation episode duration (set in the deformation load inputs) or the maximum timestep limit to stop the calculation before fractures have finished growing
                        case "MaxTimesteps":
                            MaxTimesteps = Convert.ToInt32(line_split[1]);
                            break;
                        // DFN geometry controls
                        // Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated
                        case "GenerateExplicitDFN":
                            GenerateExplicitDFN = (line_split[1] == "true");
                            break;
                        // Set false to allow fractures to propagate outside of the outer grid boundary
                        case "CropAtBoundary":
                            CropAtBoundary = (line_split[1] == "true");
                            break;
                        // Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment
                        case "LinkStressShadows":
                            LinkStressShadows = (line_split[1] == "true");
                            break;
                        // Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
                        // Set to Pi/4 rad (45 degrees) by default
                        case "MaxConsistencyAngle":
                            MaxConsistencyAngle = Convert.ToDouble(line_split[1]);
                            break;
                        // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value
                        // Set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
                        case "MinimumLayerThickness":
                            MinimumLayerThickness = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum number of fracture patches that can be generated per gridblock
                        // Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason
                        case "MaxNoFracturePatches":
                        case "MaxNoFractureSegments": // For backwards compatibility
                            MaxNoFracturePatches = Convert.ToInt32(line_split[1]);
                            break;
                        // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small
                        // Set to 0 to disable probabilistic fracture nucleation
                        // Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)
                        case "ProbabilisticFractureNucleationLimit":
                            ProbabilisticFractureNucleationLimit = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
                        // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
                        case "PropagateFracturesInNucleationOrder":
                            PropagateFracturesInNucleationOrder = (line_split[1] == "true");
                            break;
                        // Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
                        case "SearchAdjacentGridblocks":
                            {
                                if (line_split[1] == "All")
                                    SearchAdjacentGridblocks = AutomaticFlag.All;
                                else if (line_split[1] == "None")
                                    SearchAdjacentGridblocks = AutomaticFlag.None;
                                else if (line_split[1] == "Automatic")
                                    SearchAdjacentGridblocks = AutomaticFlag.Automatic;
                            }
                            break;
                        // Minimum radius for microfractures to be included in explicit DFN
                        // Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN
                        case "MinExplicitMicrofractureRadius":
                            MinExplicitMicrofractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Number of cornerpoints defining the microfracture polygons in the explicit DFN
                        // Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints
                        case "Number_uF_Points":
                            Number_uF_Points = Convert.ToInt32(line_split[1]);
                            break;
                        // Not yet implemented - keep this at 0
                        case "MinDFNMacrofractureLength":
                            MinMacrofractureLength = 0;
                            break;

                        // Parameters for controlling unconfined fractures
                        // Use NoUnconfinedFractureStrikeSets and NoUnconfinedFractureDipSets to create unconfined fracture sets, which can propagate and interact vertically as well as horizontally
                        // These are useful for modelling fractures in thick geobodies such as igneous plutons
                        // NB Unconfined fracture sets are not subdivided into dipsets; unconfined fractures with the same strike but different dips are counted as different sets
                        // The total number of unconfined fracture sets generates will therefore be given by NoUnconfinedFractureStrikeSets * NoUnconfinedFractureDipSets
                        case "NoUnconfinedFractureStrikeSets":
                            NoUnconfinedFractureStrikeSets = Convert.ToInt32(line_split[1]);
                            break;
                        case "NoUnconfinedFractureDipSets":
                            NoUnconfinedFractureDipSets = Convert.ToInt32(line_split[1]);
                            break;
                        // Number of rays comprising each unconfined fracture
                        case "NoRaysPerUnconfinedFracture":
                            NoRaysPerUnconfinedFracture = Convert.ToInt32(line_split[1]);
                            break;
                        // Minimum radius for unconfined fractures; this will be the length of the rays at nucleation
                        // If set to -1, will use 0.01 * layer thickness
                        case "MinUnconfinedFractureRadius":
                            MinUnconfinedFractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length
                        // If set to -1, will use 0.5 * layer thickness
                        case "MaxUnconfinedFractureRadius":
                            MaxUnconfinedFractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum allowed effective radius for unconfined fractures; will limit fracture stress shadow and propagation rate but not fracture growth
                        // If set to -1, there will be no limit on effective fracture radius
                        case "MaxEffectiveUnconfinedFractureRadius":
                            MaxEffectiveUnconfinedFractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Calculation termination controls
                        // The calculation is set to stop automatically when fractures stop growing
                        // This can be defined in one of three ways:
                        //      - When the total volumetric ratio of active (propagating)  unconfined fractures (a_UCFP33) drops below a specified proportion of the peak historic value
                        //      - When the total volumetric density of active (propagating) unconfined fracture rays (a_UCRP30) drops below a specified proportion of the total (propagating and non-propagating) volumetric density (UCRP30)
                        //      - When the total clear zone volume (the volume in which fractures can nucleate without falling within or overlapping a stress shadow) drops below a specified proportion of the total volume
                        //      - When the mean static unconfined fracture ray length drops below a specified value (this prevents brecciation - large numbers of small UCFs terminating against each other, filling the voids between stress shadows of larger UCFs)
                        // Increase these cutoffs to reduce the sensitivity and stop the calculation earlier
                        // Use this to prevent a long calculation tail - i.e. late timesteps where fractures have stopped growing so they have no impact on fracture populations, just increase runtime
                        // To stop calculation while fractures are still growing reduce the DeformationEpisodeDuration (in the deformation load inputs) or MaxTimesteps limits
                        // Ratio of current to peak active unconfined fracture mean linear density at which fracture sets are considered inactive; set to negative value to switch off this control
                        case "Current_HistoricUCFP32TerminationRatio":
                            Current_HistoricUCFP32TerminationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Ratio of active to total unconfined fracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
                        case "Active_TotalUCRP30TerminationRatio":
                            Active_TotalUCRP30TerminationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum required clear zone volume in which unconfined fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
                        case "MinimumUCFClearZoneVolume":
                            MinimumUCFClearZoneVolume = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum allowed mean static unconfined fracture ray length; if the mean static ray length drops below this value, the fracture set will be deactivated; set to 0 for no limit and -1 to use the minimum UCF radius
                        case "MinimumStaticUCRLength":
                            MinimumStaticUCRLength = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum increase in UCFP33 allowed in each timestep - controls the optimal timestep duration
                        // Increase this to run calculation faster, with fewer but longer timesteps
                        case "MaxTimestepUCFP33Increase":
                            MaxTimestepUCFP33Increase = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum proportional increase in the unconfined fracture ray length in each timestep (controls speed and accuracy of calculation)
                        // Set to -1 for no limit 
                        case "MaxTimestepRadiusIncrease":
                            MaxTimestepRadiusIncrease = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum proportional increase in the radius of the unconfined fractures before checking for fracture deactivation (controls number of implicit fracture population datapoints generated)
                        case "Max_R_DeactivationCheck_interval":
                            Max_R_DeactivationCheck_interval = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum activation probability for unconfined fractures; if the activation probability drops below this, the specified proportion of fractures will be deactivated, creating a new implicit fracture population datapoint
                        case "Min_R_ActivationProbability":
                            Min_R_ActivationProbability = Convert.ToDouble(line_split[1]);
                            break;
                        // The proportion of the ray length increment to apply to active unconfined fracture datapoints before the specified proportion of fractures are deactivated
                        case "ProportionalIncrementToApply":
                            ProportionalUCRIncrementToApply = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum proportional size difference for static unconfined fracture datapoints; any datapoints with less than this proportional size difference may be amalgamated into a single point
                        case "Min_R_StaticDatapointSizeRatio":
                            Min_R_StaticDatapointSizeRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Frequency (in timesteps) with which static unconfined fracture datapoints are culled
                        case "CullTSFrequency":
                            CullTSFrequency = Convert.ToInt32(line_split[1]);
                            break;
                        // Flag to calculate implicit data for unconfined fractures; if set to false no grid properties will be generated, only an explicit DFN; does not affect layer-bound fractures
                        case "CalculateImplicitUCFData":
                            CalculateImplicitUCFData = (line_split[1] == "true");
                            break;
                        // Flag to check unconfined fractures against stress shadows of all other unconfined fractures, regardless of set
                        // If None, unconfined fractures will only be deactivated if they lie in the stress shadow zone of parallel unconfined fractures
                        // If All, unconfined fractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular unconfined fractures, depending on the strain tensor
                        // If Automatic, unconfined fractures in the stress shadow zone of oblique or perpendicular unconfined fractures will be deactivated only if there are more than two fracture sets
                        case "CheckAllUCFStressShadows":
                            {
                                if (line_split[1] == "All")
                                    CheckAllUCFStressShadows = AutomaticFlag.All;
                                else if (line_split[1] == "None")
                                    CheckAllUCFStressShadows = AutomaticFlag.None;
                                else if (line_split[1] == "Automatic")
                                    CheckAllUCFStressShadows = AutomaticFlag.Automatic;
                            }
                            break;
                        // Flag to make unconfined fractures completely planar, even when crossing gridblock boundaries
                        case "PlanarUnconfinedFractures":
                            PlanarUnconfinedFractures = (line_split[1] == "true");
                            break;
                        // Minimum radius for large fractures; fractures larger than this will be considered to influence the entire grid when checking stress shadows
                        case "LargeFractureMinimumRadius":
                            LargeFractureMinimumRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius
                        case "MinStressShadowDeactivationRatio":
                            MinStressShadowDeactivationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius
                        case "MinIntersectionDeactivationRatio":
                            MinIntersectionDeactivationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag to filter cells by property; if true, cells with property values outside the specified range will not be included in the model
                        case "FilterByProperty":
                            FilterByProperty = (line_split[1] == "true");
                            break;
                        // Property to filter cells by; cells with property values outside the specified range will not be included in the model
                        case "PropertyToFilter":
                            PropertyToFilter = line_split[1];
                            break;
                        // Minimum cutoff for the property filter; cells where the spcified property value is lower than this will not be included in the model
                        case "FilterByPropertyMinCutoff":
                            FilterByPropertyMinCutoff = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum cutoff for the property filter; cells where the spcified property value is higher than this will not be included in the model
                        case "FilterByPropertyMaxCutoff":
                            FilterByPropertyMaxCutoff = Convert.ToDouble(line_split[1]);
                            break;

                        // If this is an include statement, add the file name to the list - we will deal with this later
                        case "Include":
                            IncludeFiles.Add(line_split[1]);
                            break;
                        // If the property name is not recognised, give a warning 
                        default:
                            Console.WriteLine(string.Format("Warning! Property name {0} is not recognised", line_split[0]));
                            break;
                    }
                }
                catch (System.FormatException)
                {
                    Console.WriteLine(string.Format("Warning! {0} is an invalid format for {1}", line_split[1], line_split[0]));
                    Console.WriteLine("Data will be ignored");
                }
            }
#endif
            // Create output folder
            try
            {
                outputFolderPath = (ModelName.Length > 0 ? ModelName : inputfile_name.Replace(".txt", "")) + "_output" + @"\";
                // If the output folder does not exist, create it
                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);
            }
            catch (Exception e)
            {
                if (Directory.Exists(outputFolderPath))
                    Console.WriteLine(string.Format("Could not find folder {0}: 1", outputFolderPath, e.Message));
                else
                    Console.WriteLine(string.Format("Error creating folder {0}: {1}", outputFolderPath, e.Message));
                Console.WriteLine("Model will not be run");
                return;
            }

            // Create a  time unit converter
            double TimeUnitConverter = 1;
            switch (ModelTimeUnits)
            {
                case TimeUnits.second:
                    // In SI units - no change
                    break;
                case TimeUnits.year:
                    // Convert from yr to s
                    TimeUnitConverter = 365.25d * 24d * 3600d;
                    break;
                case TimeUnits.ma:
                    // Convert from ma to s
                    TimeUnitConverter = 1000000d * 365.25d * 24d * 3600d;
                    break;
                default:
                    break;
            }

            // Get the number of fracture dipsets and dipset labels
            int NoDipSets = LayerBoundFractureSet.DefaultDipSets(Mode1Only, Mode2Only, BiazimuthalConjugate, AllowReverseFractures);
            List<string> DipSetLabels = LayerBoundFractureSet.DefaultDipSetLabels(Mode1Only, Mode2Only, BiazimuthalConjugate, AllowReverseFractures);

            // Get the deformation load data for each deformation episode
            // Find the number of deformation episodes
            // NB Deformation episodes can be defined in different ways, and not all load parameters need to be specified
            int noDeformationEpisodes = DeformationEpisodeName_list.Count;
            if (noDeformationEpisodes < DefaultEhminAzi_list.Count)
                noDeformationEpisodes = DefaultEhminAzi_list.Count;
            if (noDeformationEpisodes < DefaultEhminRate_list.Count)
                noDeformationEpisodes = DefaultEhminRate_list.Count;
            if (noDeformationEpisodes < DefaultEhmaxRate_list.Count)
                noDeformationEpisodes = DefaultEhmaxRate_list.Count;
            if (noDeformationEpisodes < DefaultAppliedOverpressureRate_list.Count)
                noDeformationEpisodes = DefaultAppliedOverpressureRate_list.Count;
            if (noDeformationEpisodes < DefaultAppliedTemperatureChange_list.Count)
                noDeformationEpisodes = DefaultAppliedTemperatureChange_list.Count;
            if (noDeformationEpisodes < DefaultAppliedUpliftRate_list.Count)
                noDeformationEpisodes = DefaultAppliedUpliftRate_list.Count;
            if (noDeformationEpisodes < StressArchingFactor_list.Count)
                noDeformationEpisodes = StressArchingFactor_list.Count;
            if (noDeformationEpisodes < DeformationEpisodeDuration_list.Count)
                noDeformationEpisodes = DeformationEpisodeDuration_list.Count;
            if (noDeformationEpisodes < SzzPropertyName_list.Count)
                noDeformationEpisodes = SzzPropertyName_list.Count;

            // Default values for the static deformation load parameters must be defined for each deformation episode
            // If they are not defined we will revert to the defaults
            for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
            {
                if ((DefaultEhminAzi_list.Count <= deformationEpisodeNo) || double.IsNaN(DefaultEhminAzi_list[deformationEpisodeNo]))
                    DefaultEhminAzi_list.Add(DefaultEhminAzi);
                if ((DefaultEhminRate_list.Count <= deformationEpisodeNo) || double.IsNaN(DefaultEhminRate_list[deformationEpisodeNo]))
                    DefaultEhminRate_list.Add(DefaultEhminRate);
                if ((DefaultEhmaxRate_list.Count <= deformationEpisodeNo) || double.IsNaN(DefaultEhmaxRate_list[deformationEpisodeNo]))
                    DefaultEhmaxRate_list.Add(DefaultEhmaxRate);
                if ((DefaultAppliedOverpressureRate_list.Count <= deformationEpisodeNo) || double.IsNaN(DefaultAppliedOverpressureRate_list[deformationEpisodeNo]))
                    DefaultAppliedOverpressureRate_list.Add(DefaultAppliedOverpressureRate);
                if ((DefaultAppliedTemperatureChange_list.Count <= deformationEpisodeNo) || double.IsNaN(DefaultAppliedTemperatureChange_list[deformationEpisodeNo]))
                    DefaultAppliedTemperatureChange_list.Add(DefaultAppliedTemperatureChange);
                if ((DefaultAppliedUpliftRate_list.Count <= deformationEpisodeNo) || double.IsNaN(DefaultAppliedUpliftRate_list[deformationEpisodeNo]))
                    DefaultAppliedUpliftRate_list.Add(DefaultAppliedUpliftRate);
                if ((StressArchingFactor_list.Count <= deformationEpisodeNo) || double.IsNaN(StressArchingFactor_list[deformationEpisodeNo]))
                    StressArchingFactor_list.Add(StressArchingFactor);
                if ((DeformationEpisodeDuration_list.Count <= deformationEpisodeNo) || double.IsNaN(DeformationEpisodeDuration_list[deformationEpisodeNo]))
                    DeformationEpisodeDuration_list.Add(DeformationEpisodeDuration);
                // If the dynamic load stress definition has not been defined for this deformation episode we will use the value for the previous episode
                // If the dynamic load stress definition has not been defined for any deformation episode we will revert to the default
                if (StressDefinition_list.Count <= deformationEpisodeNo)
                    if (deformationEpisodeNo > 0)
                        StressDefinition_list.Add(StressDefinition_list[deformationEpisodeNo - 1]);
                    else
                        StressDefinition_list.Add(StressDefinition);
                // We do not need defaults for the dynamic load components, since if these are inadequately defined by the specified properties
                // (either because the properties are not specified or because they are null in a specific gridblock) we will revert to the static load
            }

            // Create lists of flags to determine which load parameters should be populated from grid properties
            List<bool> UseGridFor_EhminAzi_list = new List<bool>();
            List<bool> UseGridFor_EhminRate_list = new List<bool>();
            List<bool> UseGridFor_EhmaxRate_list = new List<bool>();
            List<bool> UseGridFor_AppliedOverpressureRate_list = new List<bool>();
            List<bool> UseGridFor_AppliedTemperatureChange_list = new List<bool>();
            List<bool> UseGridFor_AppliedUpliftRate_list = new List<bool>();
            List<bool> UseGridFor_Szz_list = new List<bool>();
            List<bool> UseGridFor_StressTensor_list = new List<bool>();
            List<bool> UseGridFor_ShvComponents_list = new List<bool>();
            List<bool> UseGridFor_FluidPressure_list = new List<bool>();
            for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
            {
                UseGridFor_EhminAzi_list.Add((EhminAziPropertyName_list.Count > deformationEpisodeNo) ? (EhminAziPropertyName_list[deformationEpisodeNo].Length > 0) : false);
                UseGridFor_EhminRate_list.Add((EhminRatePropertyName_list.Count > deformationEpisodeNo) ? (EhminRatePropertyName_list[deformationEpisodeNo].Length > 0) : false);
                UseGridFor_EhmaxRate_list.Add((EhmaxRatePropertyName_list.Count > deformationEpisodeNo) ? (EhmaxRatePropertyName_list[deformationEpisodeNo].Length > 0) : false);
                UseGridFor_AppliedOverpressureRate_list.Add((AppliedOverpressureRatePropertyName_list.Count > deformationEpisodeNo) ? (AppliedOverpressureRatePropertyName_list[deformationEpisodeNo].Length > 0) : false);
                UseGridFor_AppliedTemperatureChange_list.Add((AppliedTemperatureChangePropertyName_list.Count > deformationEpisodeNo) ? (AppliedTemperatureChangePropertyName_list[deformationEpisodeNo].Length > 0) : false);
                UseGridFor_AppliedUpliftRate_list.Add((AppliedUpliftRatePropertyName_list.Count > deformationEpisodeNo) ? (AppliedUpliftRatePropertyName_list[deformationEpisodeNo].Length > 0) : false);
                bool UseGridFor_Szz = (SzzPropertyName_list.Count > deformationEpisodeNo) ? (SzzPropertyName_list[deformationEpisodeNo].Length > 0) : false;
                bool UseGridFor_StressTensor = UseGridFor_Szz && (SxxPropertyName_list.Count > deformationEpisodeNo) && (SyyPropertyName_list.Count > deformationEpisodeNo) && (SxyPropertyName_list.Count > deformationEpisodeNo) &&
                    (SxxPropertyName_list[deformationEpisodeNo].Length > 0) && (SyyPropertyName_list[deformationEpisodeNo].Length > 0) && (SxyPropertyName_list[deformationEpisodeNo].Length > 0);
                bool UseGridFor_ShvComponents = UseGridFor_StressTensor && (SyzPropertyName_list.Count > deformationEpisodeNo) && (SzxPropertyName_list.Count > deformationEpisodeNo) &&
                    (SyzPropertyName_list[deformationEpisodeNo].Length > 0) && (SzxPropertyName_list[deformationEpisodeNo].Length > 0);
                UseGridFor_Szz_list.Add(UseGridFor_Szz);
                UseGridFor_StressTensor_list.Add(UseGridFor_StressTensor);
                UseGridFor_ShvComponents_list.Add(UseGridFor_ShvComponents);
                UseGridFor_FluidPressure_list.Add((FluidPressurePropertyName_list.Count > deformationEpisodeNo) ? (FluidPressurePropertyName_list[deformationEpisodeNo].Length > 0) : false);
            }

            // Create flags to determine which mechanical properties should be populated from grid properties
            bool UseGridFor_InitialMicrofractureDensity = InitialMicrofractureDensityPropertyName.Length > 0;
            bool UseGridFor_InitialMicrofractureSizeDistribution = InitialMicrofractureSizeDistributionPropertyName.Length > 0;
            bool UseGridFor_InitialMicrofractureMedianRadius = InitialMicrofractureMedianRadiusPropertyName.Length > 0;
            bool UseGridFor_SubcriticalPropIndex = SubcriticalPropIndexPropertyName.Length > 0;
            bool UseGridFor_YoungsMod = YoungsModPropertyName.Length > 0;
            bool UseGridFor_PoissonsRatio = PoissonsRatioPropertyName.Length > 0;
            bool UseGridFor_Porosity = PorosityPropertyName.Length > 0;
            bool UseGridFor_BiotCoefficient = BiotCoefficientPropertyName.Length > 0;
            bool UseGridFor_ThermalExpansionCoefficient = ThermalExpansionCoefficientPropertyName.Length > 0;
            bool UseGridFor_CrackSurfaceEnergy = CrackSurfaceEnergyPropertyName.Length > 0;
            bool UseGridFor_FrictionCoefficient = FrictionCoefficientPropertyName.Length > 0;
            bool UseGridFor_RockStrainRelaxation = RockStrainRelaxationPropertyName.Length > 0;
            bool UseGridFor_FractureRelaxation = FractureRelaxationPropertyName.Length > 0;
            bool UseGridFor_HostRock_kh = HostRock_khPropertyName.Length > 0;
            bool UseGridFor_HostRock_kv = HostRock_kvPropertyName.Length > 0;
            bool UseGridFor_DepthAtDeformation = DepthAtDeformationPropertyName.Length > 0;

            // Create flags to determine which cleavage properties should be populated from grid properties
            List<bool> UseGridFor_Cleavage_Azimuth = new List<bool>();
            List<bool> UseGridFor_Cleavage_Dip = new List<bool>();
            List<bool> UseGridFor_Cleavage_CrackSurfaceEnergy = new List<bool>();
            List<bool> UseGridFor_Cleavage_FrictionCoefficient = new List<bool>();
            for (int cleavageNo = 0; cleavageNo < NoCleavages; cleavageNo++)
            {
                UseGridFor_Cleavage_Azimuth.Add((Cleavage_Azimuth_PropertyName.Count > cleavageNo) ? (Cleavage_Azimuth_PropertyName[cleavageNo].Length > 0) : false);
                UseGridFor_Cleavage_Dip.Add((Cleavage_Dip_PropertyName.Count > cleavageNo) ? (Cleavage_Dip_PropertyName[cleavageNo].Length > 0) : false);
                UseGridFor_Cleavage_CrackSurfaceEnergy.Add((Cleavage_CrackSurfaceEnergy_PropertyName.Count > cleavageNo) ? (Cleavage_CrackSurfaceEnergy_PropertyName[cleavageNo].Length > 0) : false);
                UseGridFor_Cleavage_FrictionCoefficient.Add((Cleavage_FrictionCoefficient_PropertyName.Count > cleavageNo) ? (Cleavage_FrictionCoefficient_PropertyName[cleavageNo].Length > 0) : false);
            }

            // Create flags to determine which present day stress properties should be populated from grid properties
            bool UseGridFor_PresentDayStress_XX = PresentDayStress_XXPropertyName.Length > 0;
            bool UseGridFor_PresentDayStress_YY = PresentDayStress_YYPropertyName.Length > 0;
            bool UseGridFor_PresentDayStress_ZZ = PresentDayStress_ZZPropertyName.Length > 0;
            bool UseGridFor_PresentDayStress_XY = PresentDayStress_XYPropertyName.Length > 0;
            bool UseGridFor_PresentDayStress_YZ = PresentDayStress_YZPropertyName.Length > 0;
            bool UseGridFor_PresentDayStress_ZX = PresentDayStress_ZXPropertyName.Length > 0;
            bool UseGridFor_PresentDayFluidPressure = PresentDayFluidPressurePropertyName.Length > 0;
            bool UseGridFor_PresentDayBiotCoefficient = PresentDayBiotCoefficientPropertyName.Length > 0;

            // Create a flag to determine whether a grid property shuld be used to filter the grid cells
            bool UseGridFor_PropertyToFilter = PropertyToFilter.Length > 0;

            // Create a shadow grid and use the specified GRDECL file to populate it
            // If this operation fails then display an error message and abort the run
            Console.WriteLine(string.Format("Loading data from {0}", inputFolderPath + GridFileName));
            ShadowGrid SourceDataGrid = new ShadowGrid(GridFileName, inputFolderPath, GridFileType.GRDECL);
            switch (SourceDataGrid.ErrorStatus)
            {
                case ShadowGridErrorStatus.DataLoadedOK:
                    Console.WriteLine(string.Format("Data loaded OK from {0}", inputFolderPath + GridFileName));
                    Console.WriteLine("Model run will continue");
                    break;
                case ShadowGridErrorStatus.NoDataLoaded:
                    Console.WriteLine(string.Format("No grid properties were found in {0}", inputFolderPath + GridFileName));
                    Console.WriteLine("Model run will continue using defaults");
                    break;
                case ShadowGridErrorStatus.ErrorReadingFile:
                    Console.WriteLine(string.Format("Error reading file {0}", inputFolderPath + GridFileName));
                    Console.WriteLine("Model run will be aborted without generating output");
                    return;
                case ShadowGridErrorStatus.ErrorBuildingGrid:
                    Console.WriteLine(string.Format("Error building grid from {0}", inputFolderPath + GridFileName));
                    Console.WriteLine("Model run will be aborted without generating output");
                    return;
                case ShadowGridErrorStatus.ErrorPopulatingProperties:
                    Console.WriteLine(string.Format("Error reading one or more grid properties from {0}", inputFolderPath + GridFileName));
                    Console.WriteLine("Model run will continue but some properties may be replaced by defaults");
                    break;
                default:
                    break;
            }

            // Create a DFM Generator grid and populate it with default values and property data from the shadow grid

            // Create a console progress reporter object to write progress updates to Console
            ConsoleProgressReporter progressReporter = new ConsoleProgressReporter(10, true);

            // Convert shadow grid indices to 0-based and reverse the J index (rows are indexed in reverse order in GRDECL files)
            if (ShadowGrid_StartColI > 0)
                ShadowGrid_StartColI--;
            else
                ShadowGrid_StartColI = 0;
            if (ShadowGrid_EndColI > 0)
                ShadowGrid_EndColI--;
            else
                ShadowGrid_EndColI = SourceDataGrid.NoICols - 1;
            int ShadowGrid_StartRowJ_temp, ShadowGrid_EndRowJ_temp;
            if (ShadowGrid_EndRowJ > 0)
                ShadowGrid_StartRowJ_temp = SourceDataGrid.NoJRows - ShadowGrid_EndRowJ;
            else
                ShadowGrid_StartRowJ_temp = 0;
            if (ShadowGrid_StartRowJ > 0)
                ShadowGrid_EndRowJ_temp = SourceDataGrid.NoJRows - ShadowGrid_StartRowJ;
            else
                ShadowGrid_EndRowJ_temp = SourceDataGrid.NoJRows - 1;
            ShadowGrid_StartRowJ = (ShadowGrid_StartRowJ_temp >= 0) ? ShadowGrid_StartRowJ_temp : 0;
            ShadowGrid_EndRowJ = (ShadowGrid_EndRowJ_temp >= 0) ? ShadowGrid_EndRowJ_temp : 0;
            int NoICols = ShadowGrid_EndColI - ShadowGrid_StartColI + 1;
            int NoJRows = ShadowGrid_EndRowJ - ShadowGrid_StartRowJ + 1;
            // Subset of layers from the shadow grid to include in the fracture grid (indexed from 1)
            // Set to -1 to include all layers
            if (ShadowGrid_TopLayerK > 0)
                ShadowGrid_TopLayerK--;
            else
                ShadowGrid_TopLayerK = 0;
            if (ShadowGrid_BottomLayerK > 0)
                ShadowGrid_BottomLayerK--;
            else
                ShadowGrid_BottomLayerK = SourceDataGrid.NoKLayers - 1;
            int NoKLayers = ShadowGrid_BottomLayerK - ShadowGrid_TopLayerK + 1;
            // If the vertical upscaling factor is 0, set it equal to the number of selected layers in the shadow grid
            // This will amalgamate all selected shadow grid layers into a single fracture grid layer
            if (VerticalUpscalingFactor <= 0)
                VerticalUpscalingFactor = NoKLayers;

#if DEBUG_FRAC_INPUT
            progressReporter.OutputMessage("");
            progressReporter.OutputMessage(string.Format("ShadowGrid_StartColI {0}, ShadowGrid_EndColI {1}, NoICols {2}", ShadowGrid_StartColI, ShadowGrid_EndColI, NoICols));
            progressReporter.OutputMessage(string.Format("ShadowGrid_StartRowJ {0}, ShadowGrid_StartRowJ {1}, NoJRows {2}", ShadowGrid_StartRowJ, ShadowGrid_EndRowJ, NoJRows));
            progressReporter.OutputMessage(string.Format("ShadowGrid_TopLayerK {0}, ShadowGrid_BottomLayerK {1}, NoKLayers {2}", ShadowGrid_TopLayerK, ShadowGrid_BottomLayerK, NoKLayers));
            double minX = double.NegativeInfinity;
            double minY = double.NegativeInfinity;
            double minZ = double.NegativeInfinity;
            double maxX = double.PositiveInfinity;
            double maxY = double.PositiveInfinity;
            double maxZ = double.PositiveInfinity;
#endif

            // Create fracture grid
            progressReporter.OutputMessage("Start building grid");
            int NoFractureGridCols = NoICols / HorizontalUpscalingFactor;
            if ((NoICols % HorizontalUpscalingFactor) > 0)
                NoFractureGridCols++;
            int NoFractureGridRows = NoJRows / HorizontalUpscalingFactor;
            if ((NoJRows % HorizontalUpscalingFactor) > 0)
                NoFractureGridRows++;
            int NoFractureGridLayers = NoKLayers / VerticalUpscalingFactor;
            if ((NoKLayers % VerticalUpscalingFactor) > 0)
                NoFractureGridLayers++;
            FractureGrid ModelGrid = new FractureGrid(NoFractureGridCols, NoFractureGridRows, NoFractureGridLayers);

            // Populate fracture grid
            // Loop through all gridblocks in the Fracture Grid
            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
            progressReporter.SetNumberOfElements(NoFractureGridCols * NoFractureGridRows * NoFractureGridLayers);
            for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                    for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                    {
#if DEBUG_FRAC_INPUT
                        progressReporter.OutputMessage("");
                        progressReporter.OutputMessage(string.Format("FractureGrid gridblock Col(I) {0}, Row(J) {1}, Layer {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                        // Check if calculation has been aborted
                        if (progressReporter.abortCalculation())
                        {
                            // Clean up any resources or data
                            break;
                        }

                        // Create indices for the all the shadow grid cells corresponding to the fracture gridblock 
                        int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                        int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                        int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                        if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                            ShadowGrid_LastCellI = ShadowGrid_EndColI;
                        int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                        if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                            ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                        int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                        int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                        if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                            ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;
                        // The DataCell indices indicate the cells from which to take data when we are taking data from a single cell
                        // If there is no upscaling, we take the data from the uppermost cell that contains valid data
                        int ShadowGrid_DataCellI = ShadowGrid_FirstCellI;
                        int ShadowGrid_DataCellJ = ShadowGrid_FirstCellJ;
                        // If there is upscaling, we take data from the uppermost middle cell that contains valid data
                        if (HorizontalUpscalingFactor > 1)
                        {
                            ShadowGrid_DataCellI += (HorizontalUpscalingFactor / 2);
                            if (ShadowGrid_DataCellI > ShadowGrid_LastCellI)
                                ShadowGrid_DataCellI = ShadowGrid_LastCellI;
                            ShadowGrid_DataCellJ += (HorizontalUpscalingFactor / 2);
                            if (ShadowGrid_DataCellJ > ShadowGrid_LastCellJ)
                                ShadowGrid_DataCellJ = ShadowGrid_LastCellJ;
                        }

#if DEBUG_FRAC_INPUT
                        progressReporter.OutputMessage(string.Format("ShadowGrid_FirstCellI {0}, ShadowGrid_FirstCellJ {1}, ShadowGrid_HighestCellK {2}", ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, ShadowGrid_HighestCellK));
                        progressReporter.OutputMessage(string.Format("ShadowGrid_LastCellI {0}, ShadowGrid_LastCellJ {1}, ShadowGrid_LowestCellK {2}", ShadowGrid_LastCellI, ShadowGrid_LastCellJ, ShadowGrid_LowestCellK));
#endif

                        // If we are filtering by property, get the value of the property to filter by in this gridblock and check whether it lies within the specified range
                        // If not, skip this gridblock and move on to the next
                        if (FilterByProperty)
                        {
                            // Get the value of the property to filter by from the grid as required
                            double local_PropertyToFilter = double.NaN;

                            if (AverageMechanicalPropertyData) // We are averaging over all shadow cells in the gridblock
                            {
                                // Create local variables for running total and number of datapoints for each property
                                double PropertyToFilter_total = 0;
                                int PropertyToFilter_novalues = 0;

                                // Loop through all the shadow grid cells in the gridblock
                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                        {
                                            // Update property to filter by total if defined
                                            if (FilterByProperty)
                                            {
                                                double cell_PropertyToFilter = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PropertyToFilter);

                                                if (!double.IsNaN(cell_PropertyToFilter))
                                                {
                                                    PropertyToFilter_total += cell_PropertyToFilter;
                                                    PropertyToFilter_novalues++;
                                                }
                                            }

                                        }

                                // Update the gridblock values with the averages - if there is any data to calculate them from
                                if (PropertyToFilter_novalues > 0)
                                    local_PropertyToFilter = PropertyToFilter_total / (double)PropertyToFilter_novalues;
                            }
                            else // We are taking data from a single cell
                            {
                                // Update property to filter by value if defined
                                if (FilterByProperty)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_PropertyToFilter = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PropertyToFilter);
                                        if (!double.IsNaN(cell_PropertyToFilter))
                                        {
                                            local_PropertyToFilter = cell_PropertyToFilter;
                                            break;
                                        }
                                    }
                                }
                            }
#if DEBUG_FRAC_INPUT
                            progressReporter.OutputMessage(string.Format("Specified property value {0}; range {1} to {2}", local_PropertyToFilter, FilterByPropertyMinCutoff, FilterByPropertyMaxCutoff));
                            if (double.IsNaN(FilterByPropertyMinCutoff) || (local_PropertyToFilter >= FilterByPropertyMinCutoff))
                                progressReporter.OutputMessage(string.Format("Specified property value {0} is above {1}", local_PropertyToFilter, FilterByPropertyMinCutoff));
                            if (double.IsNaN(FilterByPropertyMaxCutoff) || (local_PropertyToFilter <= FilterByPropertyMaxCutoff))
                                progressReporter.OutputMessage(string.Format("Specified property value {0} is below {1}", local_PropertyToFilter, FilterByPropertyMaxCutoff));
#endif
                            // Check whether the property lies within the spcified range, and if not move on to the next block
                            bool PropertyInRange = (double.IsNaN(FilterByPropertyMinCutoff) || (local_PropertyToFilter >= FilterByPropertyMinCutoff)) &&
                                (double.IsNaN(FilterByPropertyMaxCutoff) || (local_PropertyToFilter <= FilterByPropertyMaxCutoff));
                            if (!PropertyInRange)
                                continue;
                        }

                        // Initialise variables for mean depth and thickness
                        // If the top of the grid is above MSL we will also take this into account when calculating depth
                        // However if it is below MSL we will calculate depth from MSL (Z=0)
                        double local_Current_Depth = 0;
                        double local_LayerThickness = 0;
                        double local_Current_SurfaceHeight = 0;

                        // Find SW cornerpoints; if the top or bottom cells in the SW corner are undefined, use the highest and lowest defined cells
                        PointXYZ FractureGridStack_SWtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, 0, GridblockCornerpoint.SWTop);
                        // If the top cell in the grid is not defined, find the uppermost cell that is
                        if (FractureGridStack_SWtopgrid_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridStack_SWtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.SWTop);
                                if (!(FractureGridStack_SWtopgrid_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_SWtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, ShadowGrid_HighestCellK, GridblockCornerpoint.SWTop);
                        // If the top cell is not defined, find the uppermost cell that is
                        if (FractureGridBlock_SWtop_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK + 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridBlock_SWtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.SWTop);
                                if (!(FractureGridBlock_SWtop_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_SWbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, ShadowGrid_LowestCellK, GridblockCornerpoint.SWBottom);
                        // If the bottom cell is not defined, find the lowermost cell that is
                        if (FractureGridBlock_SWbottom_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_LowestCellK - 1; ShadowGrid_DataCellK >= ShadowGrid_HighestCellK; ShadowGrid_DataCellK--)
                            {
                                FractureGridBlock_SWbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_FirstCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.SWBottom);
                                if (!(FractureGridBlock_SWbottom_corner is null))
                                    break;
                            }
                        }
                        // Update mean depth and thickness variables
                        local_Current_SurfaceHeight += FractureGridStack_SWtopgrid_corner.Z;
                        local_Current_Depth -= FractureGridBlock_SWtop_corner.Z;
                        local_LayerThickness += (FractureGridBlock_SWtop_corner.Z - FractureGridBlock_SWbottom_corner.Z);

                        // Find NW cornerpoints; if the top or bottom cells in the NW corner are undefined, use the highest and lowest defined cells
                        PointXYZ FractureGridStack_NWtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_LastCellJ, 0, GridblockCornerpoint.NWTop);
                        // If the top cell in the grid is not defined, find the uppermost cell that is
                        if (FractureGridStack_NWtopgrid_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridStack_NWtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_LastCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.NWTop);
                                if (!(FractureGridStack_NWtopgrid_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_NWtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_LastCellJ, ShadowGrid_HighestCellK, GridblockCornerpoint.NWTop);
                        // If the top cell is not defined, find the uppermost cell that is
                        if (FractureGridBlock_NWtop_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK + 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridBlock_NWtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_LastCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.NWTop);
                                if (!(FractureGridBlock_NWtop_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_NWbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_LastCellJ, ShadowGrid_LowestCellK, GridblockCornerpoint.NWBottom);
                        // If the bottom cell is not defined, find the lowermost cell that is
                        if (FractureGridBlock_NWbottom_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_LowestCellK - 1; ShadowGrid_DataCellK >= ShadowGrid_HighestCellK; ShadowGrid_DataCellK--)
                            {
                                FractureGridBlock_NWbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_FirstCellI, ShadowGrid_LastCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.NWBottom);
                                if (!(FractureGridBlock_NWbottom_corner is null))
                                    break;
                            }
                        }
                        // Update mean depth and thickness variables
                        local_Current_SurfaceHeight += FractureGridStack_NWtopgrid_corner.Z;
                        local_Current_Depth -= FractureGridBlock_NWtop_corner.Z;
                        local_LayerThickness += (FractureGridBlock_NWtop_corner.Z - FractureGridBlock_NWbottom_corner.Z);

                        // Find NE cornerpoints; if the top or bottom cells in the NE corner are undefined, use the highest and lowest defined cells
                        PointXYZ FractureGridStack_NEtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_LastCellJ, 0, GridblockCornerpoint.NETop);
                        // If the top cell in the grid is not defined, find the uppermost cell that is
                        if (FractureGridStack_NEtopgrid_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridStack_NEtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_LastCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.NETop);
                                if (!(FractureGridStack_NEtopgrid_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_NEtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_LastCellJ, ShadowGrid_HighestCellK, GridblockCornerpoint.NETop);
                        // If the top cell is not defined, find the uppermost cell that is
                        if (FractureGridBlock_NEtop_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK + 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridBlock_NEtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_LastCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.NETop);
                                if (!(FractureGridBlock_NEtop_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_NEbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_LastCellJ, ShadowGrid_LowestCellK, GridblockCornerpoint.NEBottom);
                        // If the bottom cell is not defined, find the lowermost cell that is
                        if (FractureGridBlock_NEbottom_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_LowestCellK - 1; ShadowGrid_DataCellK >= ShadowGrid_HighestCellK; ShadowGrid_DataCellK--)
                            {
                                FractureGridBlock_NEbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_LastCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.NEBottom);
                                if (!(FractureGridBlock_NEbottom_corner is null))
                                    break;
                            }
                        }
                        // Update mean depth and thickness variables
                        local_Current_SurfaceHeight += FractureGridStack_NEtopgrid_corner.Z;
                        local_Current_Depth -= FractureGridBlock_NEtop_corner.Z;
                        local_LayerThickness += (FractureGridBlock_NEtop_corner.Z - FractureGridBlock_NEbottom_corner.Z);

                        // Find SE cornerpoints; if the top or bottom cells in the SE corner are undefined, use the highest and lowest defined cells
                        PointXYZ FractureGridStack_SEtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_FirstCellJ, 0, GridblockCornerpoint.SETop);
                        // If the top cell in the grid is not defined, find the uppermost cell that is
                        if (FractureGridStack_SEtopgrid_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridStack_SEtopgrid_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_FirstCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.SETop);
                                if (!(FractureGridStack_SEtopgrid_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_SEtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_FirstCellJ, ShadowGrid_HighestCellK, GridblockCornerpoint.SETop);
                        // If the top cell is not defined, find the uppermost cell that is
                        if (FractureGridBlock_SEtop_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK + 1; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                            {
                                FractureGridBlock_SEtop_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_FirstCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.SETop);
                                if (!(FractureGridBlock_SEtop_corner is null))
                                    break;
                            }
                        }
                        PointXYZ FractureGridBlock_SEbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_FirstCellJ, ShadowGrid_LowestCellK, GridblockCornerpoint.SEBottom);
                        // If the bottom cell is not defined, find the lowermost cell that is
                        if (FractureGridBlock_SEbottom_corner is null)
                        {
                            // Loop through all cells in the stack, from the second to bottom up, until we find one that contains valid data
                            for (int ShadowGrid_DataCellK = ShadowGrid_LowestCellK - 1; ShadowGrid_DataCellK >= ShadowGrid_HighestCellK; ShadowGrid_DataCellK--)
                            {
                                FractureGridBlock_SEbottom_corner = SourceDataGrid.GetCellCornerpoint(ShadowGrid_LastCellI, ShadowGrid_FirstCellJ, ShadowGrid_DataCellK, GridblockCornerpoint.SEBottom);
                                if (!(FractureGridBlock_SEbottom_corner is null))
                                    break;
                            }
                        }
                        // Update mean depth and thickness variables
                        local_Current_SurfaceHeight += FractureGridStack_SEtopgrid_corner.Z;
                        local_Current_Depth -= FractureGridBlock_SEtop_corner.Z;
                        local_LayerThickness += (FractureGridBlock_SEtop_corner.Z - FractureGridBlock_SEbottom_corner.Z);

                        // Calculate the mean current depth of top surface and layer thickness
                        local_Current_SurfaceHeight /= 4;
                        local_Current_Depth /= 4;
                        local_LayerThickness /= 4;
                        // If the top of the grid is above MSL, add this height to the current depth
                        // NB If the grid does not extend to the current surface height, this adjustment will need to be made manually by defining a depth of deformation property
                        if (local_Current_SurfaceHeight > 0)
                            local_Current_Depth += local_Current_SurfaceHeight;

                        // If either the mean depth or the layer thickness are undefined, then one or more of the corners lies outside the grid
                        // In this case we will abort this gridblock and move onto the next
                        if (double.IsNaN(local_Current_Depth) || double.IsNaN(local_LayerThickness))
                            continue;

                        // Get the mechanical properties from the grid as required
                        // This will depend on whether we are averaging the mechanical properties over all shadow grid cells that make up the gridblock, or taking the values from a single cell
                        // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                        double local_InitialMicrofractureDensity = DefaultInitialMicrofractureDensity;
                        double local_InitialMicrofractureSizeDistribution = DefaultInitialMicrofractureSizeDistribution;
                        double local_InitialMicrofractureMedianRadius = DefaultInitialMicrofractureMedianRadius;
                        double local_SubcriticalPropIndex = DefaultSubcriticalPropIndex;
                        double local_YoungsMod = DefaultYoungsMod;
                        double local_PoissonsRatio = DefaultPoissonsRatio;
                        double local_Porosity = DefaultPorosity;
                        double local_BiotCoefficient = DefaultBiotCoefficient;
                        double local_ThermalExpansionCoefficient = DefaultThermalExpansionCoefficient;
                        double local_CrackSurfaceEnergy = DefaultCrackSurfaceEnergy;
                        double local_FrictionCoefficient = DefaultFrictionCoefficient;
                        double local_RockStrainRelaxation = DefaultRockStrainRelaxation;
                        double local_FractureRelaxation = DefaultFractureRelaxation;
                        double local_HostRock_kh = DefaultHostRock_kh;
                        double local_HostRock_kv = DefaultHostRock_kv;

                        if (AverageMechanicalPropertyData) // We are averaging over all shadow grid cells in the gridblock
                        {
                            // Create local variables for running total and number of datapoints for each mechanical property
                            double InitialMicrofractureDensity_total = 0;
                            int InitialMicrofractureDensity_novalues = 0;
                            double InitialMicrofractureSizeDistribution_total = 0;
                            int InitialMicrofractureSizeDistribution_novalues = 0;
                            double InitialMicrofractureMedianRadius_total = 0;
                            int InitialMicrofractureMedianRadius_novalues = 0;
                            double SubcriticalPropIndex_total = 0;
                            int SubcriticalPropIndex_novalues = 0;
                            double YoungsMod_total = 0;
                            int YoungsMod_novalues = 0;
                            double PoissonsRatio_total = 0;
                            int PoissonsRatio_novalues = 0;
                            double Porosity_total = 0;
                            int Porosity_novalues = 0;
                            double BiotCoefficient_total = 0;
                            int BiotCoefficient_novalues = 0;
                            double ThermalExpansionCoefficient_total = 0;
                            int ThermalExpansionCoefficient_novalues = 0;
                            double CrackSurfaceEnergy_total = 0;
                            int CrackSurfaceEnergy_novalues = 0;
                            double FrictionCoefficient_total = 0;
                            int FrictionCoefficient_novalues = 0;
                            double RockStrainRelaxation_total = 0;
                            int RockStrainRelaxation_novalues = 0;
                            double FractureRelaxation_total = 0;
                            int FractureRelaxation_novalues = 0;
                            double HostRock_kh_total = 0;
                            int HostRock_kh_novalues = 0;
                            double HostRock_kv_total = 0;
                            int HostRock_kv_novalues = 0;

                            // Loop through all the shadow grid cells in the gridblock
                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                    for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                    {
                                        // Update initial microfracture density total if defined
                                        if (UseGridFor_InitialMicrofractureDensity)
                                        {
                                            double cell_InitialMicrofractureDensity = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, InitialMicrofractureDensityPropertyName);
                                            if (!double.IsNaN(cell_InitialMicrofractureDensity))
                                            {
                                                InitialMicrofractureDensity_total += cell_InitialMicrofractureDensity;
                                                InitialMicrofractureDensity_novalues++;
                                            }
                                        }

                                        // Update initial microfracture size distribution total if defined
                                        if (UseGridFor_InitialMicrofractureSizeDistribution)
                                        {
                                            double cell_InitialMicrofractureSizeDistribution = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, InitialMicrofractureSizeDistributionPropertyName);
                                            if (!double.IsNaN(cell_InitialMicrofractureSizeDistribution))
                                            {
                                                InitialMicrofractureSizeDistribution_total += cell_InitialMicrofractureSizeDistribution;
                                                InitialMicrofractureSizeDistribution_novalues++;
                                            }
                                        }

                                        // Update initial microfracture median radius total if defined
                                        if (UseGridFor_InitialMicrofractureMedianRadius)
                                        {
                                            double cell_InitialMicrofractureMedianRadius = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, InitialMicrofractureMedianRadiusPropertyName);
                                            if (!double.IsNaN(cell_InitialMicrofractureMedianRadius))
                                            {
                                                InitialMicrofractureMedianRadius_total += cell_InitialMicrofractureMedianRadius;
                                                InitialMicrofractureMedianRadius_novalues++;
                                            }
                                        }

                                        // Update subcritical propagation index total if defined
                                        if (UseGridFor_SubcriticalPropIndex)
                                        {
                                            double cell_SubcriticalPropIndex = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SubcriticalPropIndexPropertyName);
                                            if (!double.IsNaN(cell_SubcriticalPropIndex))
                                            {
                                                SubcriticalPropIndex_total += cell_SubcriticalPropIndex;
                                                SubcriticalPropIndex_novalues++;
                                            }
                                        }

                                        // Update Young's Modulus total if defined
                                        if (UseGridFor_YoungsMod)
                                        {
                                            double cell_YoungsMod = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, YoungsModPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_YoungsMod)
                                            //    cell_YoungsMod = toSIYoungsModUnits.Convert(cell_YoungsMod);
                                            if (!double.IsNaN(cell_YoungsMod))
                                            {
                                                YoungsMod_total += cell_YoungsMod;
                                                YoungsMod_novalues++;
                                            }
                                        }

                                        // Update Poisson's ratio total if defined
                                        if (UseGridFor_PoissonsRatio)
                                        {
                                            double cell_PoissonsRatio = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PoissonsRatioPropertyName);
                                            if (!double.IsNaN(cell_PoissonsRatio))
                                            {
                                                PoissonsRatio_total += cell_PoissonsRatio;
                                                PoissonsRatio_novalues++;
                                            }
                                        }

                                        // Update porosity total if defined
                                        if (UseGridFor_Porosity)
                                        {
                                            double cell_Porosity = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PorosityPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_Porosity)
                                            //    cell_Porosity = toSIPorosityUnits.Convert(cell_Porosity);
                                            if (!double.IsNaN(cell_Porosity))
                                            {
                                                Porosity_total += cell_Porosity;
                                                Porosity_novalues++;
                                            }
                                        }

                                        // Update Biot coefficient total if defined
                                        if (UseGridFor_BiotCoefficient)
                                        {
                                            double cell_BiotCoefficient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, BiotCoefficientPropertyName);
                                            if (!double.IsNaN(cell_BiotCoefficient))
                                            {
                                                BiotCoefficient_total += cell_BiotCoefficient;
                                                BiotCoefficient_novalues++;
                                            }
                                        }

                                        // Update thermal expansion coefficient total if defined
                                        if (UseGridFor_ThermalExpansionCoefficient)
                                        {
                                            double cell_ThermalExpansionCoefficient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, ThermalExpansionCoefficientPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_ThermalExpansionCoefficient)
                                            //    cell_ThermalExpansionCoefficient = toSIThermalExpansionCoefficientUnits.Convert(cell_ThermalExpansionCoefficient);
                                            if (!double.IsNaN(cell_ThermalExpansionCoefficient))
                                            {
                                                ThermalExpansionCoefficient_total += cell_ThermalExpansionCoefficient;
                                                ThermalExpansionCoefficient_novalues++;
                                            }
                                        }

                                        // Update crack surface energy total if defined
                                        if (UseGridFor_CrackSurfaceEnergy)
                                        {
                                            double cell_CrackSurfaceEnergy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, CrackSurfaceEnergyPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_CrackSurfaceEnergy)
                                            //    cell_CrackSurfaceEnergy = toSICrackSurfaceEnergyUnits.Convert(cell_CrackSurfaceEnergy);
                                            if (!double.IsNaN(cell_CrackSurfaceEnergy))
                                            {
                                                CrackSurfaceEnergy_total += cell_CrackSurfaceEnergy;
                                                CrackSurfaceEnergy_novalues++;
                                            }
                                        }

                                        // Update friction coefficient total if defined
                                        if (UseGridFor_FrictionCoefficient)
                                        {
                                            double cell_FrictionCoefficient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, FrictionCoefficientPropertyName);
                                            // If the property has a FrictionAngle template, convert this to a friction coefficient
                                            //if (convertFromFrictionAngle_FrictionCoefficient)
                                            //    cell_FrictionCoeff = Math.Tan(cell_FrictionCoeff);
                                            if (!double.IsNaN(cell_FrictionCoefficient))
                                            {
                                                FrictionCoefficient_total += cell_FrictionCoefficient;
                                                FrictionCoefficient_novalues++;
                                            }
                                        }

                                        // Update rock strain relaxation total if defined
                                        if (UseGridFor_RockStrainRelaxation)
                                        {
                                            double cell_RockStrainRelaxation = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, RockStrainRelaxationPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_RockStrainRelaxation)
                                            //    cell_RockStrainRelaxation = toSITimeUnits.Convert(cell_RockStrainRelaxation);
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
                                            double cell_FractureRelaxation = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, FractureRelaxationPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_FractureRelaxation)
                                            //    cell_FractureRelaxation = toSITimeUnits.Convert(cell_FractureRelaxation);
                                            if (!double.IsNaN(cell_FractureRelaxation))
                                            {
                                                if (cell_FractureRelaxation > 0)
                                                {
                                                    FractureRelaxation_total += cell_FractureRelaxation;
                                                    FractureRelaxation_novalues++;
                                                }
                                            }
                                        }

                                        // Update host rock horizontal permeability total if defined
                                        if (UseGridFor_HostRock_kh)
                                        {
                                            double cell_HostRock_kh = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, HostRock_khPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_HostRock_kh)
                                            //    cell_HostRock_kh = toSIPermeabilityUnits.Convert(cell_HostRock_kh);
                                            if (!double.IsNaN(cell_HostRock_kh))
                                            {
                                                HostRock_kh_total += cell_HostRock_kh;
                                                HostRock_kh_novalues++;
                                            }
                                        }

                                        // Update host rock vertical permeability total if defined
                                        if (UseGridFor_HostRock_kv)
                                        {
                                            double cell_HostRock_kv = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, HostRock_kvPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_HostRock_kv)
                                            //    cell_HostRock_kv = toSIPermeabilityUnits.Convert(cell_HostRock_kv);
                                            if (!double.IsNaN(cell_HostRock_kv))
                                            {
                                                HostRock_kv_total += cell_HostRock_kv;
                                                HostRock_kv_novalues++;
                                            }
                                        }
                                    }

                            // Update the gridblock values with the averages - if there is any data to calculate them from
                            if (InitialMicrofractureDensity_novalues > 0)
                                local_InitialMicrofractureDensity = InitialMicrofractureDensity_total / (double)InitialMicrofractureDensity_novalues;
                            if (InitialMicrofractureSizeDistribution_novalues > 0)
                                local_InitialMicrofractureSizeDistribution = InitialMicrofractureSizeDistribution_total / (double)InitialMicrofractureSizeDistribution_novalues;
                            if (InitialMicrofractureMedianRadius_novalues > 0)
                                local_InitialMicrofractureMedianRadius = InitialMicrofractureMedianRadius_total / (double)InitialMicrofractureMedianRadius_novalues;
                            if (SubcriticalPropIndex_novalues > 0)
                                local_SubcriticalPropIndex = SubcriticalPropIndex_total / (double)SubcriticalPropIndex_novalues;
                            if (YoungsMod_novalues > 0)
                                local_YoungsMod = YoungsMod_total / (double)YoungsMod_novalues;
                            if (PoissonsRatio_novalues > 0)
                                local_PoissonsRatio = PoissonsRatio_total / (double)PoissonsRatio_novalues;
                            if (Porosity_novalues > 0)
                                local_Porosity = Porosity_total / (double)Porosity_novalues;
                            if (BiotCoefficient_novalues > 0)
                                local_BiotCoefficient = BiotCoefficient_total / (double)BiotCoefficient_novalues;
                            if (ThermalExpansionCoefficient_novalues > 0)
                                local_ThermalExpansionCoefficient = ThermalExpansionCoefficient_total / (double)ThermalExpansionCoefficient_novalues;
                            if (CrackSurfaceEnergy_novalues > 0)
                                local_CrackSurfaceEnergy = CrackSurfaceEnergy_total / (double)CrackSurfaceEnergy_novalues;
                            if (FrictionCoefficient_novalues > 0)
                                local_FrictionCoefficient = FrictionCoefficient_total / (double)FrictionCoefficient_novalues;
                            if (RockStrainRelaxation_novalues > 0)
                                local_RockStrainRelaxation = RockStrainRelaxation_total / (double)RockStrainRelaxation_novalues;
                            if (FractureRelaxation_novalues > 0)
                                local_FractureRelaxation = FractureRelaxation_total / (double)FractureRelaxation_novalues;
                            if (HostRock_kh_novalues > 0)
                                local_HostRock_kh = HostRock_kh_total / (double)HostRock_kh_novalues;
                            if (HostRock_kv_novalues > 0)
                                local_HostRock_kv = HostRock_kv_total / (double)HostRock_kv_novalues;
                        }
                        else // We are taking data from a single cell
                        {
                            // Update initial microfracture density total if defined
                            if (UseGridFor_InitialMicrofractureDensity)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_InitialMicrofractureDensity = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, InitialMicrofractureDensityPropertyName);
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
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_InitialMicrofractureSizeDistribution = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, InitialMicrofractureSizeDistributionPropertyName);
                                    if (!double.IsNaN(cell_InitialMicrofractureSizeDistribution))
                                    {
                                        local_InitialMicrofractureSizeDistribution = cell_InitialMicrofractureSizeDistribution;
                                        break;
                                    }
                                }
                            }

                            // Update initial microfracture median radius total if defined
                            if (UseGridFor_InitialMicrofractureMedianRadius)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_InitialMicrofractureMedianRadius = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, InitialMicrofractureMedianRadiusPropertyName);
                                    if (!double.IsNaN(cell_InitialMicrofractureMedianRadius))
                                    {
                                        local_InitialMicrofractureMedianRadius = cell_InitialMicrofractureMedianRadius;
                                        break;
                                    }
                                }
                            }

                            // Update subcritical propagation index total if defined
                            if (UseGridFor_SubcriticalPropIndex)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_SubcriticalPropIndex = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SubcriticalPropIndexPropertyName);
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
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_YoungsMod = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, YoungsModPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_YoungsMod)
                                    //    cell_YoungsMod = toSIYoungsModUnits.Convert(cell_YoungsMod);
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
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_PoissonsRatio = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PoissonsRatioPropertyName);
                                    if (!double.IsNaN(cell_PoissonsRatio))
                                    {
                                        local_PoissonsRatio = cell_PoissonsRatio;
                                        break;
                                    }
                                }
                            }

                            // Update porosity total if defined
                            if (UseGridFor_Porosity)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_Porosity = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PorosityPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_Porosity)
                                    //    cell_Porosity = toSIPorosityUnits.Convert(cell_Porosity);
                                    if (!double.IsNaN(cell_Porosity))
                                    {
                                        local_Porosity = cell_Porosity;
                                        break;
                                    }
                                }
                            }

                            // Update Biot coefficient total if defined
                            if (UseGridFor_BiotCoefficient)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_BiotCoefficient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, BiotCoefficientPropertyName);
                                    if (!double.IsNaN(cell_BiotCoefficient))
                                    {
                                        local_BiotCoefficient = cell_BiotCoefficient;
                                        break;
                                    }
                                }
                            }

                            // Update thermal expansion coefficient total if defined
                            if (UseGridFor_ThermalExpansionCoefficient)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_ThermalExpansionCoefficient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, ThermalExpansionCoefficientPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_ThermalExpansionCoefficient)
                                    //    cell_ThermalExpansionCoefficient = toSIThermalExpansionCoefficientUnits.Convert(cell_ThermalExpansionCoefficient);
                                    if (!double.IsNaN(cell_ThermalExpansionCoefficient))
                                    {
                                        local_ThermalExpansionCoefficient = cell_ThermalExpansionCoefficient;
                                        break;
                                    }
                                }
                            }

                            // Update crack surface energy total if defined
                            if (UseGridFor_CrackSurfaceEnergy)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_CrackSurfaceEnergy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, CrackSurfaceEnergyPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_CrackSurfaceEnergy)
                                    //    cell_CrackSurfaceEnergy = toSICrackSurfaceEnergyUnits.Convert(cell_CrackSurfaceEnergy);
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
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_FrictionCoefficient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, FrictionCoefficientPropertyName);
                                    // If the property has a FrictionAngle template, convert this to a friction coefficient
                                    //if (convertFromFrictionAngle_FrictionCoefficient)
                                    //    cell_FrictionCoefficient = Math.Tan(cell_FrictionCoefficient);
                                    if (!double.IsNaN(cell_FrictionCoefficient))
                                    {
                                        local_FrictionCoefficient = cell_FrictionCoefficient;
                                        break;
                                    }
                                }
                            }

                            // Update rock strain relaxation total if defined
                            if (UseGridFor_RockStrainRelaxation)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_RockStrainRelaxation = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, RockStrainRelaxationPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_RockStrainRelaxation)
                                    //    cell_RockStrainRelaxation = toSITimeUnits.Convert(cell_RockStrainRelaxation);
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
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_FractureRelaxation = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, FractureRelaxationPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_FractureRelaxation)
                                    //    cell_FractureRelaxation = toSITimeUnits.Convert(cell_FractureRelaxation);
                                    if (!double.IsNaN(cell_FractureRelaxation))
                                    {
                                        local_FractureRelaxation = cell_FractureRelaxation;
                                        break;
                                    }
                                }
                            }

                            // Update host rock horizontal permeability total if defined
                            if (UseGridFor_HostRock_kh)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_HostRock_kh = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, HostRock_khPropertyName);
                                    if (!double.IsNaN(cell_HostRock_kh))
                                    {
                                        local_HostRock_kh = cell_HostRock_kh;
                                        break;
                                    }
                                }
                            }

                            // Update host rock vertical permeability total if defined
                            if (UseGridFor_HostRock_kv)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_HostRock_kv = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, HostRock_kvPropertyName);
                                    if (!double.IsNaN(cell_HostRock_kv))
                                    {
                                        local_HostRock_kv = cell_HostRock_kv;
                                        break;
                                    }
                                }
                            }
                        }

                        // InitialMicrofractureDensity A is stored in project units rather than SI units, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
                        // Therefore unit conversion for InitialMicrofractureDensity A must be carried out now
                        // Unit conversion must be done on a cell by cell basis, since the values of InitialMicrofractureSizeDistribution may vary between cells
                        /*if (!lengthUnitMetres)
                        {
                            double toSIUnits_InitialMicrofractureDensity = Math.Pow(toSIUnits_Length, local_InitialMicrofractureSizeDistribution - 3);
                            local_InitialMicrofractureDensity *= toSIUnits_InitialMicrofractureDensity;
                        }*/

                        // Check the elastic properties for physically unrealistic values, and if so warn the user
                        // NB The code will actually generate a result with any input values except Young's Modulus = 0, Poisson's ratio = -1 or Poisson's ratio = 1
                        // and these values will automatically be corrected by the MechanicalProperties object
                        if (local_YoungsMod <= 0)
                        {
                            progressReporter.OutputMessage(string.Format("Invalid value for Young's Modulus ({0}Pa) in cell {1},{2}. This will create errors in the calculation.", local_YoungsMod, ShadowGrid_FirstCellI + 1, SourceDataGrid.NoJRows - ShadowGrid_FirstCellJ + 1));
                        }
                        if ((local_PoissonsRatio < 0) || (local_PoissonsRatio > 0.5))
                        {
                            progressReporter.OutputMessage(string.Format("Invalid value for Poisson's ratio ({0}) in cell {1},{2}. This will create errors in the calculation.", local_PoissonsRatio, ShadowGrid_FirstCellI + 1, SourceDataGrid.NoJRows - ShadowGrid_FirstCellJ + 1));
                        }
                        // End get the mechanical properties from the grid as required

                        // Create a local list of cleavages and get the grid properties representing the cleavage values if required 
                        List<Cleavage> local_Cleavages = new List<Cleavage>();
                        for (int cleavageNo = 0; cleavageNo < NoCleavages; cleavageNo++)
                        {
                            // Check if the cleavage plane is defined - at a minimum a default azimuth and dip must be specified
                            // If this is the case, create a new cleavage object and add it to the list
                            if (Cleavage_Defined[cleavageNo])
                            {
                                // Get the default values fo the cleavage orientation and properties
                                double cleavage_Azimuth = Default_Cleavage_Azimuth[cleavageNo];
                                double cleavage_Dip = Default_Cleavage_Dip[cleavageNo];
                                double cleavage_CrackSurfaceEnergy = Default_Cleavage_CrackSurfaceEnergy[cleavageNo];
                                double cleavage_FrictionCoefficient = Default_Cleavage_FrictionCoefficient[cleavageNo];

                                // Read the orientation data for the cleavage from the grid
                                if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
                                {
                                    // Create local variables for running total and number of datapoints for each mechanical property
                                    double cleavage_Azimuth_total = 0;
                                    int cleavage_Azimuth_novalues = 0;
                                    double cleavage_Dip_total = 0;
                                    int cleavage_Dip_novalues = 0;

                                    // Loop through all the shadow grid cells in the gridblock
                                    for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                        for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                            for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                            {
                                                // Update the azimuth total if defined
                                                if (UseGridFor_Cleavage_Azimuth[cleavageNo])
                                                {
                                                    double cell_Cleavage_Azimuth = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, Cleavage_Azimuth_PropertyName[cleavageNo]);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Cleavage_Azimuth[cleavageNo])
                                                    //    cell_Cleavage_Azimuth = toSIAzimuthUnits.Convert(cell_Cleavage_Azimuth);
                                                    if (!double.IsNaN(cell_Cleavage_Azimuth))
                                                    {
                                                        cleavage_Azimuth_total += cell_Cleavage_Azimuth;
                                                        cleavage_Azimuth_novalues++;
                                                    }
                                                }

                                                // Update the dip total if defined
                                                if (UseGridFor_Cleavage_Dip[cleavageNo])
                                                {
                                                    double cell_Cleavage_Dip = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, Cleavage_Dip_PropertyName[cleavageNo]);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Cleavage_Dip[cleavageNo])
                                                    //    cell_Cleavage_Dip = toSIDipUnits.Convert(cell_Cleavage_Dip);
                                                    if (!double.IsNaN(cell_Cleavage_Dip))
                                                    {
                                                        cleavage_Dip_total += cell_Cleavage_Dip;
                                                        cleavage_Dip_novalues++;
                                                    }
                                                }
                                            }

                                    // Update the gridblock values with the averages - if there is any data to calculate them from
                                    if (cleavage_Azimuth_novalues > 0)
                                        cleavage_Azimuth = cleavage_Azimuth_total / (double)cleavage_Azimuth_novalues;
                                    if (cleavage_Dip_novalues > 0)
                                        cleavage_Dip = cleavage_Dip_total / (double)cleavage_Dip_novalues;
                                }
                                else // We are taking data from a single cell
                                {
                                    // Update crack surface energy total if defined
                                    if (UseGridFor_Cleavage_Azimuth[cleavageNo])
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                        {
                                            double cell_Cleavage_Azimuth = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, Cleavage_Azimuth_PropertyName[cleavageNo]);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_Cleavage_Azimuth[cleavageNo])
                                            //    cell_Cleavage_Azimuth = toSIAzimuthUnits.Convert(cell_Cleavage_Azimuth);
                                            if (!double.IsNaN(cell_Cleavage_Azimuth))
                                            {
                                                cleavage_Azimuth = cell_Cleavage_Azimuth;
                                                break;
                                            }
                                        }
                                    }

                                    // Update friction coefficient total if defined
                                    if (UseGridFor_Cleavage_Dip[cleavageNo])
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                        {
                                            double cell_Cleavage_Dip = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, Cleavage_Dip_PropertyName[cleavageNo]);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_Cleavage_Dip[cleavageNo])
                                            //    cell_Cleavage_Dip = toSIDipUnits.Convert(cell_Cleavage_Dip);
                                            if (!double.IsNaN(cell_Cleavage_Dip))
                                            {
                                                cleavage_Dip = cell_Cleavage_Dip;
                                                break;
                                            }
                                        }
                                    }
                                }

                                // Read the mechanical property data for the cleavage from the grid
                                if (AverageMechanicalPropertyData) // We are averaging over all Petrel cells in the gridblock
                                {
                                    // Create local variables for running total and number of datapoints for each mechanical property
                                    double cleavage_CrackSurfaceEnergy_total = 0;
                                    int cleavage_CrackSurfaceEnergy_novalues = 0;
                                    double cleavage_FrictionCoeff_total = 0;
                                    int cleavage_FrictionCoeff_novalues = 0;

                                    // Loop through all the shadow grid cells in the gridblock
                                    for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                        for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                            for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                            {
                                                // Update crack surface energy total if defined
                                                if (UseGridFor_Cleavage_CrackSurfaceEnergy[cleavageNo])
                                                {
                                                    double cell_Cleavage_CrackSurfaceEnergy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, Cleavage_CrackSurfaceEnergy_PropertyName[cleavageNo]);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Cleavage_CrackSurfaceEnergy[cleavageNo])
                                                    //    cell_Cleavage_CrackSurfaceEnergy = toSICrackSurfaceEnergyUnits.Convert(cell_Cleavage_CrackSurfaceEnergy);
                                                    if (!double.IsNaN(cell_Cleavage_CrackSurfaceEnergy))
                                                    {
                                                        cleavage_CrackSurfaceEnergy_total += cell_Cleavage_CrackSurfaceEnergy;
                                                        cleavage_CrackSurfaceEnergy_novalues++;
                                                    }
                                                }

                                                // Update friction coefficient total if defined
                                                if (UseGridFor_Cleavage_FrictionCoefficient[cleavageNo])
                                                {
                                                    double cell_Cleavage_FrictionCoeff = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, Cleavage_FrictionCoefficient_PropertyName[cleavageNo]);
                                                    // If the property has a FrictionAngle template, convert this to a friction coefficient
                                                    //if (convertFromFrictionAngle_Cleavage_FrictionCoefficient[cleavageNo])
                                                    //    cell_Cleavage_FrictionCoeff = Math.Tan(cell_Cleavage_FrictionCoeff);
                                                    if (!double.IsNaN(cell_Cleavage_FrictionCoeff))
                                                    {
                                                        cleavage_FrictionCoeff_total += cell_Cleavage_FrictionCoeff;
                                                        cleavage_FrictionCoeff_novalues++;
                                                    }
                                                }
                                            }

                                    // Update the gridblock values with the averages - if there is any data to calculate them from
                                    if (cleavage_CrackSurfaceEnergy_novalues > 0)
                                        cleavage_CrackSurfaceEnergy = cleavage_CrackSurfaceEnergy_total / (double)cleavage_CrackSurfaceEnergy_novalues;
                                    if (cleavage_FrictionCoeff_novalues > 0)
                                        cleavage_FrictionCoefficient = cleavage_FrictionCoeff_total / (double)cleavage_FrictionCoeff_novalues;
                                }
                                else // We are taking data from a single cell
                                {
                                    // Update crack surface energy total if defined
                                    if (UseGridFor_Cleavage_CrackSurfaceEnergy[cleavageNo])
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                        {
                                            double cell_Cleavage_CrackSurfaceEnergy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, Cleavage_CrackSurfaceEnergy_PropertyName[cleavageNo]);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_Cleavage_CrackSurfaceEnergy[cleavageNo])
                                            //    cell_Cleavage_CrackSurfaceEnergy = toSICrackSurfaceEnergyUnits.Convert(cell_Cleavage_CrackSurfaceEnergy);
                                            if (!double.IsNaN(cell_Cleavage_CrackSurfaceEnergy))
                                            {
                                                cleavage_CrackSurfaceEnergy = cell_Cleavage_CrackSurfaceEnergy;
                                                break;
                                            }
                                        }
                                    }

                                    // Update friction coefficient total if defined
                                    if (UseGridFor_Cleavage_FrictionCoefficient[cleavageNo])
                                    {
                                        // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                        for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                        {
                                            double cell_Cleavage_FrictionCoeff = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, Cleavage_FrictionCoefficient_PropertyName[cleavageNo]);
                                            // If the property has a FrictionAngle template, convert this to a friction coefficient
                                            //if (convertFromFrictionAngle_Cleavage_FrictionCoefficient[cleavageNo])
                                            //    cell_Cleavage_FrictionCoeff = Math.Tan(cell_Cleavage_FrictionCoeff);
                                            if (!double.IsNaN(cell_Cleavage_FrictionCoeff))
                                            {
                                                cleavage_FrictionCoefficient = cell_Cleavage_FrictionCoeff;
                                                break;
                                            }
                                        }
                                    }
                                }

                                // Create a Cleavage object for this gridblock and add it to the local list
                                Cleavage newCleavage = new Cleavage(cleavage_CrackSurfaceEnergy, cleavage_FrictionCoefficient, cleavage_Azimuth - (Math.PI / 2), cleavage_Dip);
                                local_Cleavages.Add(newCleavage);
                            }
                        } // End create a local list of cleavages


                        // Also calculate the total uplift - this will be needed to calculate the depth at the time of deformation
                        List<Tensor2S> local_EhRate_list = new List<Tensor2S>();
                        List<double> local_AppliedOverpressureRate_list = new List<double>();
                        List<double> local_AppliedTemperatureChange_list = new List<double>();
                        List<double> local_AppliedUpliftRate_list = new List<double>();
                        List<double> local_StressArchingFactor_list = new List<double>();
                        List<double> local_DeformationEpisodeDuration_list = new List<double>();
                        List<StressStateDefinition> local_StressDefinition_list = new List<StressStateDefinition>();
                        List<double> local_InitialVerticalStress_list = new List<double>();
                        List<Tensor2S> local_StressRateTensor_list = new List<Tensor2S>();
                        List<Tensor2S> local_InitialStressTensor_list = new List<Tensor2S>();
                        List<double> local_InitialFluidPressure_list = new List<double>();

                        // Create local variables for the initial and final dynamic load values
                        // These are created outside the loop through the deformation episodes, so that the final values for each episode can be used as the initial values for the subsequent episode 
                        double initialSzz = double.NaN;
                        double finalSzz = double.NaN;
                        double initialSxx = double.NaN;
                        double finalSxx = double.NaN;
                        double initialSyy = double.NaN;
                        double finalSyy = double.NaN;
                        double initialSxy = double.NaN;
                        double finalSxy = double.NaN;
                        double initialSzx = double.NaN;
                        double finalSzx = double.NaN;
                        double initialSyz = double.NaN;
                        double finalSyz = double.NaN;
                        double initialFluidPressure = double.NaN;
                        double finalFluidPressure = double.NaN;

                        // The default fracture azimuth for the gridblock will be defined based on the minimum horizontal strain azimuth for the first deformation episode
                        // If the minimum horizontal strain azimuth is not specified for the first deformation episode, it will be set to zero
                        double local_DefaultFractureAzimuth = 0;
                        for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
                        {
                            // Get the static deformation load data from the grid as required
                            // This will depend on whether we are averaging the stress/strain over all shadow grid cells that make up the gridblock, or taking the values from a single shadow grid cell
                            // First we will create local variables for the static load property values in this gridblock; we can then recalculate these without altering the global default values
                            double local_EhminAzi = DefaultEhminAzi_list[deformationEpisodeNo];
                            double local_EhminRate = DefaultEhminRate_list[deformationEpisodeNo];
                            double local_EhmaxRate = DefaultEhmaxRate_list[deformationEpisodeNo];
                            double local_AppliedOverpressureRate = DefaultAppliedOverpressureRate_list[deformationEpisodeNo];
                            double local_AppliedTemperatureChange = DefaultAppliedTemperatureChange_list[deformationEpisodeNo];
                            double local_AppliedUpliftRate = DefaultAppliedUpliftRate_list[deformationEpisodeNo];
                            double local_StressArchingFactor = StressArchingFactor_list[deformationEpisodeNo];
                            double local_DeformationEpisodeDuration = DeformationEpisodeDuration_list[deformationEpisodeNo];

                            // Get local handles for the static load properties and flags
                            bool UseGridFor_EhminAzi = UseGridFor_EhminAzi_list[deformationEpisodeNo];
                            string EhminAziProperty = UseGridFor_EhminAzi ? EhminAziPropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_EhminRate = UseGridFor_EhminRate_list[deformationEpisodeNo];
                            string EhminRateProperty = UseGridFor_EhminRate ? EhminRatePropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_EhmaxRate = UseGridFor_EhmaxRate_list[deformationEpisodeNo];
                            string EhmaxRateProperty = UseGridFor_EhmaxRate ? EhmaxRatePropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_AppliedOverpressureRate = UseGridFor_AppliedOverpressureRate_list[deformationEpisodeNo];
                            string AppliedOverpressureRateProperty = UseGridFor_AppliedOverpressureRate ? AppliedOverpressureRatePropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_AppliedTemperatureChange = UseGridFor_AppliedTemperatureChange_list[deformationEpisodeNo];
                            string AppliedTemperatureChangeProperty = UseGridFor_AppliedTemperatureChange ? AppliedTemperatureChangePropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_AppliedUpliftRate = UseGridFor_AppliedUpliftRate_list[deformationEpisodeNo];
                            string AppliedUpliftRateProperty = UseGridFor_AppliedUpliftRate ? AppliedUpliftRatePropertyName_list[deformationEpisodeNo] : string.Empty;

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
                                double OP_rate_total = 0;
                                int OP_rate_novalues = 0;
                                double temp_rate_total = 0;
                                int temp_rate_novalues = 0;
                                double uplift_rate_total = 0;
                                int uplift_rate_novalues = 0;

                                // Loop through all the shadow grid cells in the gridblock
                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                        {
                                            // Update ehmin orientation total if defined
                                            if (UseGridFor_EhminAzi)
                                            {
                                                double cell_ehmin_orient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, EhminAziProperty);
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                //if (convertFromGeneral_EhminAzi)
                                                //    cell_ehmin_orient = toSIAzimuthUnits.Convert(cell_ehmin_orient);
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
                                                double cell_ehmin_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, EhminRateProperty);
                                                if (!double.IsNaN(cell_ehmin_rate))
                                                {
                                                    ehmin_rate_total += cell_ehmin_rate;
                                                    ehmin_rate_novalues++;
                                                }
                                            }

                                            // Update ehmax rate total if defined
                                            if (UseGridFor_EhmaxRate)
                                            {
                                                double cell_ehmax_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, EhmaxRateProperty);
                                                if (!double.IsNaN(cell_ehmax_rate))
                                                {
                                                    ehmax_rate_total += cell_ehmax_rate;
                                                    ehmax_rate_novalues++;
                                                }
                                            }

                                            // Update overpressure rate total if defined
                                            if (UseGridFor_AppliedOverpressureRate)
                                            {
                                                double cell_OP_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, AppliedOverpressureRateProperty);
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                //if (convertFromGeneral_AppliedOverpressureRate)
                                                //    cell_OP_rate = toSIPressureUnits.Convert(cell_OP_rate);
                                                if (!double.IsNaN(cell_OP_rate))
                                                {
                                                    OP_rate_total += cell_OP_rate;
                                                    OP_rate_novalues++;
                                                }
                                            }

                                            // Update temperature change total if defined
                                            if (UseGridFor_AppliedTemperatureChange)
                                            {
                                                double cell_temp_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, AppliedTemperatureChangeProperty);
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                //if (convertFromGeneral_AppliedTemperatureChange)
                                                //    cell_temp_rate = toSITemperatureUnits.Convert(cell_temp_rate);
                                                if (!double.IsNaN(cell_temp_rate))
                                                {
                                                    temp_rate_total += cell_temp_rate;
                                                    temp_rate_novalues++;
                                                }
                                            }

                                            // Update uplift rate total if defined
                                            if (UseGridFor_AppliedUpliftRate)
                                            {
                                                double cell_uplift_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, AppliedUpliftRateProperty);
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                //if (convertFromGeneral_AppliedUpliftRate)
                                                //    cell_uplift_rate = toSIDepthUnits.Convert(cell_uplift_rate);
                                                if (!double.IsNaN(cell_uplift_rate))
                                                {
                                                    uplift_rate_total += cell_uplift_rate;
                                                    uplift_rate_novalues++;
                                                }
                                            }
                                        }

                                // Update the gridblock values with the averages - if there is any data to calculate them from
                                if (ehmin_orient_novalues > 0)
                                    local_EhminAzi = Math.Atan(ehmin_orient_x_total / ehmin_orient_y_total);
                                if (ehmin_rate_novalues > 0)
                                    local_EhminRate = ehmin_rate_total / (double)ehmin_rate_novalues;
                                if (ehmax_rate_novalues > 0)
                                    local_EhmaxRate = ehmax_rate_total / (double)ehmax_rate_novalues;
                                if (OP_rate_novalues > 0)
                                    local_AppliedOverpressureRate = OP_rate_total / (double)OP_rate_novalues;
                                if (temp_rate_novalues > 0)
                                    local_AppliedTemperatureChange = temp_rate_total / (double)temp_rate_novalues;
                                if (uplift_rate_novalues > 0)
                                    local_AppliedUpliftRate = uplift_rate_total / (double)uplift_rate_novalues;
                            }
                            else // We are taking data from a single cell
                            {
                                // Update ehmin orientation total if defined
                                if (UseGridFor_EhminAzi)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_ehmin_orient = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, EhminAziProperty);
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        //if (convertFromGeneral_EhminAzi)
                                        //    cell_ehmin_orient = toSIAzimuthUnits.Convert(cell_ehmin_orient);
                                        if (!double.IsNaN(cell_ehmin_orient))
                                        {
                                            local_EhminAzi = cell_ehmin_orient;
                                            break;
                                        }
                                    }
                                }

                                // Update ehmin rate total if defined
                                if (UseGridFor_EhminRate)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_ehmin_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, EhminRateProperty);
                                        if (!double.IsNaN(cell_ehmin_rate))
                                        {
                                            local_EhminRate = cell_ehmin_rate;
                                            break;
                                        }
                                    }
                                }

                                // Update ehmax rate total if defined
                                if (UseGridFor_EhmaxRate)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_ehmax_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, EhmaxRateProperty);
                                        if (!double.IsNaN(cell_ehmax_rate))
                                        {
                                            local_EhmaxRate = cell_ehmax_rate;
                                            break;
                                        }
                                    }
                                }

                                // Update overpressure rate total if defined
                                if (UseGridFor_AppliedOverpressureRate)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_OP_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, AppliedOverpressureRateProperty);
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        //if (convertFromGeneral_AppliedOverpressureRate)
                                        //    cell_OP_rate = toSIPressureUnits.Convert(cell_OP_rate);
                                        if (!double.IsNaN(cell_OP_rate))
                                        {
                                            local_AppliedOverpressureRate = cell_OP_rate;
                                            break;
                                        }
                                    }
                                }

                                // Update temperature change total if defined
                                if (UseGridFor_AppliedTemperatureChange)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_temp_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, AppliedTemperatureChangeProperty);
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        //if (convertFromGeneral_AppliedTemperatureChange)
                                        //    cell_temp_rate = toSITemperatureUnits.Convert(cell_temp_rate);
                                        if (!double.IsNaN(cell_temp_rate))
                                        {
                                            local_AppliedTemperatureChange = cell_temp_rate;
                                            break;
                                        }
                                    }
                                }

                                // Update uplift rate total if defined
                                if (UseGridFor_AppliedUpliftRate)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_uplift_rate = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, AppliedUpliftRateProperty);
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        //if (convertFromGeneral_AppliedUpliftRate)
                                        //    cell_uplift_rate = toSIDepthUnits.Convert(cell_uplift_rate);
                                        if (!double.IsNaN(cell_uplift_rate))
                                        {
                                            local_AppliedUpliftRate = cell_uplift_rate;
                                            break;
                                        }
                                    }
                                }
                            }

                            // If this is the first deformation episode, and the fractures are not forced to be planar, set the default fracture azimuth for the gridblock
                            if (!PlanarUnconfinedFractures && (deformationEpisodeNo == 0))
                                local_DefaultFractureAzimuth = local_EhminAzi;

                            // Get the dynamic deformation load data as grid properties if required
                            // Get local handles for the Property names and flags defining the load data for this deformation episode
                            bool UseGridFor_FluidPressure = UseGridFor_FluidPressure_list[deformationEpisodeNo];
                            string FluidPressureProperty = UseGridFor_FluidPressure ? FluidPressurePropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_Szz = UseGridFor_Szz_list[deformationEpisodeNo];
                            string SzzProperty = UseGridFor_Szz ? SzzPropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_StressTensor = UseGridFor_StressTensor_list[deformationEpisodeNo];
                            string SxxProperty = UseGridFor_StressTensor ? SxxPropertyName_list[deformationEpisodeNo] : string.Empty;
                            string SyyProperty = UseGridFor_StressTensor ? SyyPropertyName_list[deformationEpisodeNo] : string.Empty;
                            string SxyProperty = UseGridFor_StressTensor ? SxyPropertyName_list[deformationEpisodeNo] : string.Empty;
                            bool UseGridFor_ShvComponents = UseGridFor_ShvComponents_list[deformationEpisodeNo];
                            string SyzProperty = UseGridFor_ShvComponents ? SyzPropertyName_list[deformationEpisodeNo] : string.Empty;
                            string SzxProperty = UseGridFor_ShvComponents ? SzxPropertyName_list[deformationEpisodeNo] : string.Empty;

                            // Update the initial load values with the final load values from the previous deformation episode, if defined
                            // If these are not defined, we will use the final values (i.e. assume constant stress during the deformation episode)
                            initialSzz = finalSzz;
                            initialSxx = finalSxx;
                            initialSyy = finalSyy;
                            initialSxy = finalSxy;
                            initialSzx = finalSzx;
                            initialSyz = finalSyz;
                            initialFluidPressure = finalFluidPressure;

                            // Get the final stress values
                            if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
                            {
                                // Create local variables for running total and number of datapoints for each stress/strain state parameter
                                double szz_total = 0;
                                int szz_novalues = 0;
                                double sxx_total = 0;
                                int sxx_novalues = 0;
                                double syy_total = 0;
                                int syy_novalues = 0;
                                double sxy_total = 0;
                                int sxy_novalues = 0;
                                double szx_total = 0;
                                int szx_novalues = 0;
                                double syz_total = 0;
                                int syz_novalues = 0;
                                double fluidPressure_total = 0;
                                int fluidPressure_novalues = 0;

                                // Loop through all the shadow grid cells in the gridblock
                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                        {
                                            // Update final absolute vertical stress total if defined
                                            if (UseGridFor_Szz)
                                            {
                                                double cell_szz = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SzzProperty);
                                                if (!double.IsNaN(cell_szz))
                                                {
                                                    szz_total += cell_szz;
                                                    szz_novalues++;
                                                }
                                            }

                                            // Update final horizontal stress tensor components total if defined
                                            if (UseGridFor_StressTensor)
                                            {
                                                double cell_sxx = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SxxProperty);
                                                if (!double.IsNaN(cell_sxx))
                                                {
                                                    sxx_total += cell_sxx;
                                                    sxx_novalues++;
                                                }
                                                double cell_syy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SyyProperty);
                                                if (!double.IsNaN(cell_syy))
                                                {
                                                    syy_total += cell_syy;
                                                    syy_novalues++;
                                                }
                                                double cell_sxy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SxyProperty);
                                                if (!double.IsNaN(cell_sxy))
                                                {
                                                    sxy_total += cell_sxy;
                                                    sxy_novalues++;
                                                }
                                            }

                                            // Update final vertical shear stress tensor components total if defined
                                            if (UseGridFor_ShvComponents)
                                            {
                                                double cell_szx = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SzxProperty);
                                                if (!double.IsNaN(cell_szx))
                                                {
                                                    szx_total += cell_szx;
                                                    szx_novalues++;
                                                }
                                                double cell_syz = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SyzProperty);
                                                if (!double.IsNaN(cell_syz))
                                                {
                                                    syz_total += cell_syz;
                                                    syz_novalues++;
                                                }
                                            }

                                            // Update final fluid pressure total if defined
                                            if (UseGridFor_FluidPressure)
                                            {
                                                double cell_fluidpressure = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, FluidPressureProperty);
                                                if (!double.IsNaN(cell_fluidpressure))
                                                {
                                                    fluidPressure_total += cell_fluidpressure;
                                                    fluidPressure_novalues++;
                                                }
                                            }
                                        }

                                // Update the gridblock values with the averages - if there is any data to calculate them from
                                if (szz_novalues > 0)
                                    finalSzz = szz_total / (double)szz_novalues;
                                if (sxx_novalues > 0)
                                    finalSxx = sxx_total / (double)sxx_novalues;
                                if (syy_novalues > 0)
                                    finalSyy = syy_total / (double)syy_novalues;
                                if (sxy_novalues > 0)
                                    finalSxy = sxy_total / (double)sxy_novalues;
                                if (szx_novalues > 0)
                                    finalSzx = szx_total / (double)szx_novalues;
                                if (syz_novalues > 0)
                                    finalSyz = syz_total / (double)syz_novalues;
                                if (fluidPressure_novalues > 0)
                                    finalFluidPressure = fluidPressure_total / (double)fluidPressure_novalues;
                            }
                            else // We are taking data from a single cell
                            {
                                // Update final absolute vertical stress total if defined
                                if (UseGridFor_Szz)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_szz = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SzzProperty);
                                        if (!double.IsNaN(cell_szz))
                                        {
                                            finalSzz = cell_szz;
                                            break;
                                        }
                                    }
                                }

                                // Update final horizontal stress totals if defined
                                if (UseGridFor_StressTensor)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    // We need valid data for all three horizontal components of the stress tensor
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_sxx = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SxxProperty);
                                        double cell_syy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SyyProperty);
                                        double cell_sxy = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SxyProperty);
                                        if (!double.IsNaN(cell_sxx) && !double.IsNaN(cell_syy) && !double.IsNaN(cell_sxy))
                                        {
                                            finalSxx = cell_sxx;
                                            finalSyy = cell_syy;
                                            finalSxy = cell_sxy;
                                            break;
                                        }
                                    }
                                }

                                // Update final vertical shear stress totals if defined
                                if (UseGridFor_ShvComponents)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    // We need valid data for both vertical shear components of the stress tensor
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_szx = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SzxProperty);
                                        double cell_syz = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, SyzProperty);
                                        if (!double.IsNaN(cell_szx) && !double.IsNaN(cell_syz))
                                        {
                                            finalSzx = cell_szx;
                                            finalSyz = cell_syz;
                                            break;
                                        }
                                    }
                                }

                                // Update final fluid pressure total if defined
                                if (UseGridFor_FluidPressure)
                                {
                                    // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                    for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                    {
                                        double cell_fluidpressure = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, FluidPressureProperty);
                                        if (!double.IsNaN(cell_fluidpressure))
                                        {
                                            finalFluidPressure = cell_fluidpressure;
                                            break;
                                        }
                                    }
                                }
                            }

                            // Calculate the dynamic load rates from standard properties
                            // First define null/NaN values for initial vertical stress, fluid pressure and stress tensor, and stress rate tensor
                            double local_InitialVerticalStress = double.NaN;
                            double local_InitialFluidPressure = double.NaN;
                            Tensor2S local_InitialStressTensor = null;
                            Tensor2S local_StressRateTensor = null;
                            // Next determine if there is sufficient data to calculate the stress rate tensor
                            // This can only be done if a timestep duration is defined
                            bool overideStressRate = UseGridFor_StressTensor && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalSzz) && !double.IsNaN(finalSxx) && !double.IsNaN(finalSyy) && !double.IsNaN(finalSxy);
                            bool overideShvComponents = UseGridFor_ShvComponents && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalSzx) && !double.IsNaN(finalSyz);
                            if (overideStressRate)
                            {
                                double local_szzRate = 0;
                                double local_sxxRate = 0;
                                double local_syyRate = 0;
                                double local_sxyRate = 0;
                                double local_szxRate = 0;
                                double local_syzRate = 0;

                                // If initial stress values are not defined, set them equal to the final values - this will give a constant stress during the timestep
                                if (double.IsNaN(initialSzz))
                                    initialSzz = finalSzz;
                                else
                                    local_szzRate = (finalSzz - initialSzz) / local_DeformationEpisodeDuration;
                                if (double.IsNaN(initialSxx))
                                    initialSxx = finalSxx;
                                else
                                    local_sxxRate = (finalSxx - initialSxx) / local_DeformationEpisodeDuration;
                                if (double.IsNaN(initialSyy))
                                    initialSyy = finalSyy;
                                else
                                    local_syyRate = (finalSyy - initialSyy) / local_DeformationEpisodeDuration;
                                if (double.IsNaN(initialSxy))
                                    initialSxy = finalSxy;
                                else
                                    local_sxyRate = (finalSxy - initialSxy) / local_DeformationEpisodeDuration;
                                if (overideShvComponents)
                                {
                                    if (double.IsNaN(initialSzx))
                                        initialSzx = finalSzx;
                                    else
                                        local_szxRate = (finalSzx - initialSzx) / local_DeformationEpisodeDuration;
                                    if (double.IsNaN(initialSyz))
                                        initialSyz = finalSyz;
                                    else
                                        local_syzRate = (finalSyz - initialSyz) / local_DeformationEpisodeDuration;
                                }
                                else
                                {
                                    initialSzx = 0;
                                    initialSyz = 0;
                                    finalSzx = 0;
                                    finalSyz = 0;
                                }
                                local_InitialStressTensor = new Tensor2S(initialSxx, initialSyy, initialSzz, initialSxy, initialSyz, initialSzx);
                                local_StressRateTensor = new Tensor2S(local_sxxRate, local_syyRate, local_szzRate, local_sxyRate, local_syzRate, local_szxRate);
                            }
                            bool overrideFluidPressure = UseGridFor_FluidPressure && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalFluidPressure);
                            if (overrideFluidPressure)
                            {
                                double local_FluidPressureRate = 0;
                                if (double.IsNaN(initialFluidPressure))
                                    initialFluidPressure = finalFluidPressure;
                                else
                                    local_FluidPressureRate = (finalFluidPressure - initialFluidPressure) / local_DeformationEpisodeDuration;
                                double local_HydrostaticPressureRate = (local_AppliedUpliftRate > 0 ? -local_AppliedUpliftRate * FluidDensity * StressStrainState.Gravity : 0);
                                local_InitialFluidPressure = initialFluidPressure;
                                local_AppliedOverpressureRate = local_FluidPressureRate - local_HydrostaticPressureRate;
                            }
                            // If the stress tensor is not defined, then changes in the absolute vertical stress within each deformation episode will be accounted for through the stress arching factor
                            // NB The absolute vertical stress will also be reset at the start of each deformation episode, so will remain synchronised with the specified input load
                            bool overrideStressArchingFactor = UseGridFor_Szz && !UseGridFor_StressTensor && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalSzz);
                            if (overrideStressArchingFactor)
                            {
                                double dSigmazz_dt = 0;
                                if (double.IsNaN(initialSzz))
                                    initialSzz = finalSzz;
                                else
                                    dSigmazz_dt = (finalSzz - initialSzz) / local_DeformationEpisodeDuration;
                                double dLithStress_dt = (local_AppliedUpliftRate > 0 ? -local_AppliedUpliftRate * (MeanOverlyingSedimentDensity - FluidDensity) * StressStrainState.Gravity : 0);
                                double local_Kb = local_YoungsMod / (2 * (1 + local_PoissonsRatio));
                                double dEtherm_dt = local_Kb * local_ThermalExpansionCoefficient * local_AppliedTemperatureChange;
                                local_InitialVerticalStress = initialSzz;
                                double local_OP_Thermal_factor = (local_BiotCoefficient * local_AppliedOverpressureRate) + dEtherm_dt;
                                local_StressArchingFactor = (local_OP_Thermal_factor != 0) ? (dSigmazz_dt - dLithStress_dt) / ((local_BiotCoefficient * local_AppliedOverpressureRate) + dEtherm_dt) : 1;
                                // Trim the result so it lies between 0 and 1 inclusive
                                if (local_StressArchingFactor < 0)
                                    local_StressArchingFactor = 0;
                                if (local_StressArchingFactor > 1)
                                    local_StressArchingFactor = 1;
                            }

                            // If the final stress tensor and fluid pressure values are not defined, reset them to NaN so they will not be picked up by the next deformation episode
                            if (!overideStressRate)
                            {
                                finalSxx = double.NaN;
                                finalSyy = double.NaN;
                                finalSxy = double.NaN;
                                finalSzx = double.NaN;
                                finalSyz = double.NaN;
                                if (!overrideStressArchingFactor)
                                    finalSzz = double.NaN;
                            }
                            if (!overrideFluidPressure)
                            {
                                finalFluidPressure = double.NaN;
                            }

                            // Add the load data for this deformation episode to the deformation episode lists
                            // Add the strain load data
                            local_EhRate_list.Add(Tensor2S.HorizontalStrainTensor(local_EhminRate, local_EhmaxRate, local_EhminAzi));
                            local_AppliedOverpressureRate_list.Add(local_AppliedOverpressureRate);
                            local_AppliedTemperatureChange_list.Add(local_AppliedTemperatureChange);
                            local_AppliedUpliftRate_list.Add(local_AppliedUpliftRate);
                            local_StressArchingFactor_list.Add(local_StressArchingFactor);
                            local_DeformationEpisodeDuration_list.Add(local_DeformationEpisodeDuration);
                            local_StressDefinition_list.Add(StressDefinition_list[deformationEpisodeNo]);

                            // Add the stress load tensor - this will be null if not defined
                            local_StressRateTensor_list.Add(local_StressRateTensor);

                            // Add values for the inital data - these will be null if not defined
                            local_InitialStressTensor_list.Add(local_InitialStressTensor);
                            local_InitialFluidPressure_list.Add(local_InitialFluidPressure);
                            local_InitialVerticalStress_list.Add(local_InitialVerticalStress);

#if DEBUG_FRAC_INPUT
                            if (local_StressRateTensor is null)
                                progressReporter.OutputMessage(string.Format("New deformation episode: Duration {0}, EhminAzi {1}, EhminRate {2}, EhmaxRate {3}, OP rate {4}, Temp change {5}, Uplift rate {6}, Stress arching factor {7});", local_DeformationEpisodeDuration, local_EhminAzi, local_EhminRate, local_EhmaxRate, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor));
                            else
                                progressReporter.OutputMessage(string.Format("New deformation episode: Duration {0}, Initial stress (Sxx, Syy, Szz, Sxy, Syz, Szx) = ({1}, {2}, {3}, {4}, {5}, {6}), Initial FP {7}, Final stress (Sxx, Syy, Szz, Sxy, Syz, Szx) = ({8}, {9}, {10}, {11}, {12}, {13}), Final FP {14}", local_DeformationEpisodeDuration, initialSxx, initialSyy, initialSzz, initialSxy, initialSyz, initialSzx, initialFluidPressure, finalSxx, finalSyy, finalSzz, finalSxy, finalSyz, finalSzx, finalFluidPressure));
#endif
                        } // End get the deformation load data for each deformation episode

                        // Get the depth at the start of deformation from the grid as required
                        // This will depend on whether we are averaging the stress and strain data over all Petrel cells that make up the gridblock, or taking the values from a single cell
                        // First we will create a local variable for the property value in this gridblock; we can then recalculate this without altering the global default value
                        double local_DepthAtDeformation = DefaultDepthAtDeformation;
                        if (UseGridFor_DepthAtDeformation)
                        {
                            if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
                            {
                                // Create local variables for running total and number of datapoints
                                double DepthAtDeformation_total = 0;
                                int DepthAtDeformation_novalues = 0;

                                // Loop through all the shadow grid cells in the gridblock
                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                        {
                                            // Update depth at deformation total if defined
                                            double cell_depthatdeformation = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, DepthAtDeformationPropertyName);
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            //if (convertFromGeneral_DepthAtDeformation)
                                            //    cell_depthatdeformation = toSIDepthUnits.Convert(cell_depthatdeformation);
                                            // If the cell value is undefined, it will not be included in the average; if all cell values are undefined, the default value for depth at deformation will be used
                                            // If the cell value is <=0 it will be included in the average; if the average <=0, the depth at deformation will be set to current burial depth, even if a default value has been specified 
                                            if (!double.IsNaN(cell_depthatdeformation))
                                            {
                                                DepthAtDeformation_total += cell_depthatdeformation;
                                                DepthAtDeformation_novalues++;
                                            }
                                        }

                                // Update the gridblock value with the average - if there is any data to calculate it from
                                if (DepthAtDeformation_novalues > 0)
                                    local_DepthAtDeformation = DepthAtDeformation_total / (double)DepthAtDeformation_novalues;
                            }
                            else // We are taking data from a single cell
                            {
                                // Update depth at deformation total if defined
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                {
                                    double cell_depthatdeformation = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, DepthAtDeformationPropertyName);
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    //if (convertFromGeneral_DepthAtDeformation)
                                    //    cell_depthatdeformation = toSIDepthUnits.Convert(cell_depthatdeformation);
                                    // If all cell values are undefined, the default value for depth at deformation will be used
                                    // If the top cell value is <=0, the depth at deformation will be set to current burial depth, even if a default value has been specified 
                                    if (!double.IsNaN(cell_depthatdeformation))
                                    {
                                        local_DepthAtDeformation = cell_depthatdeformation;
                                        break;
                                    }
                                }
                            }
                        }

                        // Calculate the mean depth of top surface and mean layer thickness at start of deformation - assume that these are equal to the current mean depth minus total specified uplift, and current layer thickness, respectively, unless the depth at the start of deformation has been specified
                        double local_Depth;
                        if (local_DepthAtDeformation >= 0)
                        {
                            // If the initial depth  has been specified, use this 
                            local_Depth = local_DepthAtDeformation;
                        }
                        else
                        {
                            // Otherwise, calculate the current mean depth of the gridblock minus the total specified uplift
                            // NB Uplift will not be counted for deformation episodes with indefinite duration
                            local_Depth = local_Current_Depth;
                            for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
                            {
                                double local_AppliedUpliftRate = local_AppliedUpliftRate_list[deformationEpisodeNo];
                                double local_DeformationEpisodeDuration = local_DeformationEpisodeDuration_list[deformationEpisodeNo];
                                if (local_DeformationEpisodeDuration > 0)
                                    local_Depth += (local_AppliedUpliftRate * local_DeformationEpisodeDuration);
                            }
                        }

                        // Create a new gridblock object containing the required number of fracture sets
                        GridblockConfiguration gc = new GridblockConfiguration(local_LayerThickness, local_Depth);

                        // Check if the western boundary if faulted
                        // This will be the case if any of the shadow grid cells on the southern boundary are faulted
                        bool faultToWest = false;
                        if (!IgnoreFaults)
                            for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                for (int ShadowGrid_K = ShadowGrid_TopLayerK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                    if (SourceDataGrid.FaultedContact(ShadowGrid_FirstCellI, ShadowGrid_J, ShadowGrid_K, GridDirection.W))
                                        faultToWest = true;

                        // Check if the southern boundary is faulted
                        // This will be the case if any of the shadow grid cells on the southern boundary are faulted
                        bool faultToSouth = false;
                        if (!IgnoreFaults)
                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                    if (SourceDataGrid.FaultedContact(ShadowGrid_I, ShadowGrid_FirstCellJ, ShadowGrid_K, GridDirection.S))
                                        faultToSouth = true;

#if DEBUG_FRAC_INPUT
                        foreach (PointXYZ point in new PointXYZ[] { FractureGridBlock_SWtop_corner, FractureGridBlock_NWtop_corner, FractureGridBlock_NEtop_corner, FractureGridBlock_SEtop_corner, FractureGridBlock_SWbottom_corner, FractureGridBlock_NWbottom_corner, FractureGridBlock_NEbottom_corner, FractureGridBlock_SEbottom_corner })
                        {
                            if (minX > point.X) minX = point.X;
                            if (minY > point.Y) minY = point.Y;
                            if (minZ > point.Z) minZ = point.Z;
                            if (maxX < point.X) maxX = point.X;
                            if (maxY < point.Y) maxY = point.Y;
                            if (maxZ < point.Z) maxZ = point.Z;
                        }

                        progressReporter.OutputMessage("Geometry");
                        progressReporter.OutputMessage(string.Format("SW top of FractureGrid: ({0}, {1}, {2});", FractureGridStack_SWtopgrid_corner.X, FractureGridStack_SWtopgrid_corner.Y, FractureGridStack_SWtopgrid_corner.Z));
                        progressReporter.OutputMessage(string.Format("NW top of FractureGrid: ({0}, {1}, {2});", FractureGridStack_NWtopgrid_corner.X, FractureGridStack_NWtopgrid_corner.Y, FractureGridStack_NWtopgrid_corner.Z));
                        progressReporter.OutputMessage(string.Format("NE top of FractureGrid: ({0}, {1}, {2});", FractureGridStack_NEtopgrid_corner.X, FractureGridStack_NEtopgrid_corner.Y, FractureGridStack_NEtopgrid_corner.Z));
                        progressReporter.OutputMessage(string.Format("SE top of FractureGrid: ({0}, {1}, {2});", FractureGridStack_SEtopgrid_corner.X, FractureGridStack_SEtopgrid_corner.Y, FractureGridStack_SEtopgrid_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_SWtop = new PointXYZ({0}, {1}, {2});", FractureGridBlock_SWtop_corner.X, FractureGridBlock_SWtop_corner.Y, FractureGridBlock_SWtop_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_SWbottom = new PointXYZ({0}, {1}, {2});", FractureGridBlock_SWbottom_corner.X, FractureGridBlock_SWbottom_corner.Y, FractureGridBlock_SWbottom_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_NWtop = new PointXYZ({0}, {1}, {2});", FractureGridBlock_NWtop_corner.X, FractureGridBlock_NWtop_corner.Y, FractureGridBlock_NWtop_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_NWbottom = new PointXYZ({0}, {1}, {2});", FractureGridBlock_NWbottom_corner.X, FractureGridBlock_NWbottom_corner.Y, FractureGridBlock_NWbottom_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_NEtop = new PointXYZ({0}, {1}, {2});", FractureGridBlock_NEtop_corner.X, FractureGridBlock_NEtop_corner.Y, FractureGridBlock_NEtop_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_NEbottom = new PointXYZ({0}, {1}, {2});", FractureGridBlock_NEbottom_corner.X, FractureGridBlock_NEbottom_corner.Y, FractureGridBlock_NEbottom_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_SEtop = new PointXYZ({0}, {1}, {2});", FractureGridBlock_SEtop_corner.X, FractureGridBlock_SEtop_corner.Y, FractureGridBlock_SEtop_corner.Z));
                        progressReporter.OutputMessage(string.Format("PointXYZ FractureGrid_SEbottom = new PointXYZ({0}, {1}, {2});", FractureGridBlock_SEbottom_corner.X, FractureGridBlock_SEbottom_corner.Y, FractureGridBlock_SEbottom_corner.Z));
                        progressReporter.OutputMessage(string.Format("LayerThickness = {0}; Depth = {1}; Surface height = {2}", local_LayerThickness, local_Current_Depth, Math.Max(local_Current_SurfaceHeight, 0)));
#endif

                        // Set the gridblock cornerpoints
                        gc.setGridblockCorners(FractureGridBlock_SWtop_corner, FractureGridBlock_SWbottom_corner, FractureGridBlock_NWtop_corner, FractureGridBlock_NWbottom_corner, FractureGridBlock_NEtop_corner, FractureGridBlock_NEbottom_corner, FractureGridBlock_SEtop_corner, FractureGridBlock_SEbottom_corner);

                        // Set the mechanical properties for the gridblock
                        gc.MechProps.setMechanicalProperties(local_YoungsMod, local_PoissonsRatio, local_Porosity, local_BiotCoefficient, local_ThermalExpansionCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_SubcriticalPropIndex, ModelTimeUnits);

                        // Set the fracture aperture control properties
                        gc.MechProps.setFractureApertureControlData(DynamicApertureMultiplier, JRC, UCSRatio, InitialNormalStress, FractureNormalStiffness, MaximumClosure);

                        // Set the host rock permeability
                        gc.MechProps.setHostRockPermeability(local_HostRock_kh, local_HostRock_kv);

                        // Set the initial stress and strain
                        // If the initial stress relaxation value is negative, set it to the required value for a critical initial stress state
                        double local_InitialStressRelaxation = InitialStressRelaxation;
                        if (InitialStressRelaxation < 0)
                            gc.StressStrain.SetCriticalInitialStressStrainState(MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure);
                        else
                            gc.StressStrain.SetInitialStressStrainState(MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure, local_InitialStressRelaxation);

                        // Set the geothermal gradient
                        gc.StressStrain.GeothermalGradient = GeothermalGradient;

                        // Calculate the minimum microfracture radius from the layer thickness, if required
                        double local_minImplicitMicrofractureRadius = MinImplicitMicrofractureRadius;
                        if (MinImplicitMicrofractureRadius < 0)
                        {
                            double maxMicrofractureRadius = local_LayerThickness * (0.5 + (FractureNucleationPosition >= 0 ? Math.Abs(FractureNucleationPosition - 0.5) : 0));
                            local_minImplicitMicrofractureRadius = maxMicrofractureRadius / (double)No_r_bins;
                        }

                        // Calculate the minimum and maximum unconfined fracture radius from the layer thickness, if required
                        double local_minUnconfinedFractureRadius = (MinUnconfinedFractureRadius > 0) ? MinUnconfinedFractureRadius : 0.01 * local_LayerThickness;
                        double local_maxUnconfinedFractureRadius = (MaxUnconfinedFractureRadius > 0) ? MaxUnconfinedFractureRadius : 0.5 * local_LayerThickness;
                        double local_maxEffectiveUnconfinedFractureRadius = (MaxEffectiveUnconfinedFractureRadius > 0) ? MaxEffectiveUnconfinedFractureRadius : local_maxUnconfinedFractureRadius;

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
                                local_checkAlluFStressShadows = (NoLayerBoundFractureSets > 2);
                                break;
                            default:
                                local_checkAlluFStressShadows = false;
                                break;
                        }
                        bool local_checkAllUCFStressShadows;
                        switch (CheckAllUCFStressShadows)
                        {
                            case AutomaticFlag.None:
                                local_checkAllUCFStressShadows = false;
                                break;
                            case AutomaticFlag.All:
                                local_checkAllUCFStressShadows = true;
                                break;
                            case AutomaticFlag.Automatic:
                                local_checkAllUCFStressShadows = (NoUnconfinedFractureStrikeSets > 2);
                                break;
                            default:
                                local_checkAllUCFStressShadows = false;
                                break;
                        }

                        // Set the propagation control data for the gridblock
                        gc.PropControl.setPropagationControl(OutputPopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio,
                            MinimumClearZoneVolume, MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, FractureNucleationPosition, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, ModelTimeUnits, OutputFracturePorosity, FractureApertureControl, OutputFracturePermeabilityTensor, PermeabilityAlgorithm, local_DefaultFractureAzimuth, PlanarUnconfinedFractures);
                        gc.PropControl.setUnconfinedFractureControl(Current_HistoricUCFP32TerminationRatio, Active_TotalUCRP30TerminationRatio, MinimumUCFClearZoneVolume, MinimumStaticUCRLength, MaxTimestepUCFP33Increase, MaxTimestepRadiusIncrease, Max_R_DeactivationCheck_interval, Min_R_ActivationProbability, ProportionalUCRIncrementToApply, Min_R_StaticDatapointSizeRatio, CullTSFrequency, CalculateImplicitUCFData, local_checkAllUCFStressShadows, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio);

                        // Set folder path for output files
                        gc.PropControl.FolderPath = outputFolderPath;

#if DEBUG_FRAC_INPUT
                        progressReporter.OutputMessage("Properties");
                        progressReporter.OutputMessage(string.Format("sv': {0}", gc.StressStrain.LithostaticStress_eff_Terzaghi));
                        progressReporter.OutputMessage(string.Format("Young's Mod: {0}, Poisson's ratio: {1}, Biot coefficient: {2}, Crack surface energy: {3}, Friction coefficient: {4}", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient));
                        progressReporter.OutputMessage("Create gridblock");
                        progressReporter.OutputMessage(string.Format("gc = new GridblockConfiguration({0}, {1}, {2});", local_LayerThickness, local_Current_Depth, NoLayerBoundFractureSets));
                        progressReporter.OutputMessage(string.Format("gc.MechProps.setMechanicalProperties({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, TimeUnits.{11});", local_YoungsMod, local_PoissonsRatio, local_Porosity, local_BiotCoefficient, local_ThermalExpansionCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_SubcriticalPropIndex, ModelTimeUnits));
                        progressReporter.OutputMessage(string.Format("gc.MechProps.setFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5});", DynamicApertureMultiplier, JRC, UCSRatio, InitialNormalStress, FractureNormalStiffness, MaximumClosure));
                        progressReporter.OutputMessage(string.Format("gc.MechProps.setHostRockPermeability({0}, {1});", local_HostRock_kh, local_HostRock_kv));
                        if (InitialStressRelaxation < 0)
                            progressReporter.OutputMessage(string.Format("gc.StressStrain.SetCriticalInitialStressStrainState({0}, {1}, {2});", MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure));
                        else
                            progressReporter.OutputMessage(string.Format("gc.StressStrain.SetInitialStressStrainState({0}, {1}, {2}, {3});", MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure, local_InitialStressRelaxation));
                        progressReporter.OutputMessage(string.Format("gc.StressStrain.GeothermalGradient = {0};", GeothermalGradient));
                        progressReporter.OutputMessage(string.Format("gc.PropControl.setPropagationControl({0}, {1}, {2}, {3}, {4}, {5}, StressDistribution.{6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, TimeUnits.{19}, {20}, {21}, {22}, {23}, {24}, {25});",
                            OutputPopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio,
                            MinimumClearZoneVolume, MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, FractureNucleationPosition, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, ModelTimeUnits, OutputFracturePorosity, FractureApertureControl, OutputFracturePermeabilityTensor, PermeabilityAlgorithm, local_DefaultFractureAzimuth, PlanarUnconfinedFractures));
                        progressReporter.OutputMessage(string.Format("gc.PropControl.setUnconfinedFractureControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14});", Current_HistoricUCFP32TerminationRatio, Active_TotalUCRP30TerminationRatio, MinimumUCFClearZoneVolume, MinimumStaticUCRLength, MaxTimestepUCFP33Increase, MaxTimestepRadiusIncrease, Max_R_DeactivationCheck_interval, Min_R_ActivationProbability, ProportionalUCRIncrementToApply, Min_R_StaticDatapointSizeRatio, CullTSFrequency, CalculateImplicitUCFData, local_checkAllUCFStressShadows, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio));
#endif

                        // Add the deformation load data 
                        // Keep a record of the initial fluid pressure at the start of each timestep in case it is not defined for a stress load
                        double initialFP = gc.StressStrain.P_f;
                        for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
                        {
                            // Get load data for this deformation episode from the deformation episode lists
                            Tensor2S local_EhRate = local_EhRate_list[deformationEpisodeNo];
                            double local_AppliedOverpressureRate = local_AppliedOverpressureRate_list[deformationEpisodeNo];
                            double local_AppliedTemperatureChange = local_AppliedTemperatureChange_list[deformationEpisodeNo];
                            double local_AppliedUpliftRate = local_AppliedUpliftRate_list[deformationEpisodeNo];
                            double local_StressArchingFactor = local_StressArchingFactor_list[deformationEpisodeNo];
                            double local_DeformationEpisodeDuration = local_DeformationEpisodeDuration_list[deformationEpisodeNo];
                            Tensor2S local_StressRateTensor = local_StressRateTensor_list[deformationEpisodeNo];
                            double local_InitialVerticalStress = local_InitialVerticalStress_list[deformationEpisodeNo];
                            Tensor2S local_InitialStressTensor = local_InitialStressTensor_list[deformationEpisodeNo];
                            double local_InitialFluidPressure = local_InitialFluidPressure_list[deformationEpisodeNo];
                            if (local_InitialFluidPressure >= 0)
                                initialFP = local_InitialFluidPressure;

                            // Get the type of data used to define this deformation episode load
                            StressStateDefinition local_StressDefinition = local_StressDefinition_list[deformationEpisodeNo];
                            if (local_StressRateTensor is null)
                                local_StressDefinition = StressStateDefinition.Strain;

                            // Add the deformation episode to the deformation episode list in the PropControl object, using the function appropriate to the data type
                            switch (local_StressDefinition)
                            {
                                case StressStateDefinition.Strain:
                                    {
                                        gc.PropControl.AddDeformationEpisode_StrainLoad(local_EhRate, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor, local_DeformationEpisodeDuration, local_InitialVerticalStress, local_InitialFluidPressure);
#if DEBUG_FRAC_INPUT
                                        string local_EhRate_info;
                                        if (local_EhRate is null)
                                            local_EhRate_info = "null";
                                        else
                                            local_EhRate_info = string.Format("Tensor2S({0}, {1}, 0, {2}, 0, 0)", local_EhRate.Component(Tensor2SComponents.XX), local_EhRate.Component(Tensor2SComponents.YY), local_EhRate.Component(Tensor2SComponents.XY));
                                        progressReporter.OutputMessage(string.Format("gc.PropControl.AddDeformationEpisode_StrainLoad({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_EhRate_info, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor, local_DeformationEpisodeDuration, local_InitialVerticalStress, local_InitialFluidPressure));
#endif
                                    }
                                    break;
                                case StressStateDefinition.AbsoluteStress:
                                    {
                                        gc.PropControl.AddDeformationEpisode_AbsoluteStressLoad(local_StressRateTensor, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStressTensor, initialFP);
#if DEBUG_FRAC_INPUT
                                        string local_StressRate_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_StressRateTensor.Component(Tensor2SComponents.XX), local_StressRateTensor.Component(Tensor2SComponents.YY), local_StressRateTensor.Component(Tensor2SComponents.ZZ), local_StressRateTensor.Component(Tensor2SComponents.XY), local_StressRateTensor.Component(Tensor2SComponents.YZ), local_StressRateTensor.Component(Tensor2SComponents.ZX));
                                        string local_InitialStress_info;
                                        if (local_InitialStressTensor is null)
                                            local_InitialStress_info = "null";
                                        else
                                            local_InitialStress_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_InitialStressTensor.Component(Tensor2SComponents.XX), local_InitialStressTensor.Component(Tensor2SComponents.YY), local_InitialStressTensor.Component(Tensor2SComponents.ZZ), local_InitialStressTensor.Component(Tensor2SComponents.XY), local_InitialStressTensor.Component(Tensor2SComponents.YZ), local_InitialStressTensor.Component(Tensor2SComponents.ZX));
                                        progressReporter.OutputMessage(string.Format("gc.PropControl.AddDeformationEpisode_AbsoluteStressLoad({0}, {1}, {2}, {3}, {4});", local_StressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStress_info, initialFP));
#endif
                                    }
                                    break;
                                case StressStateDefinition.TerzaghiEffectiveStress:
                                    {
                                        gc.PropControl.AddDeformationEpisode_TerzaghiStressLoad(local_StressRateTensor, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStressTensor, initialFP);
#if DEBUG_FRAC_INPUT
                                        string local_StressRate_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_StressRateTensor.Component(Tensor2SComponents.XX), local_StressRateTensor.Component(Tensor2SComponents.YY), local_StressRateTensor.Component(Tensor2SComponents.ZZ), local_StressRateTensor.Component(Tensor2SComponents.XY), local_StressRateTensor.Component(Tensor2SComponents.YZ), local_StressRateTensor.Component(Tensor2SComponents.ZX));
                                        string local_InitialStress_info;
                                        if (local_InitialStressTensor is null)
                                            local_InitialStress_info = "null";
                                        else
                                            local_InitialStress_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_InitialStressTensor.Component(Tensor2SComponents.XX), local_InitialStressTensor.Component(Tensor2SComponents.YY), local_InitialStressTensor.Component(Tensor2SComponents.ZZ), local_InitialStressTensor.Component(Tensor2SComponents.XY), local_InitialStressTensor.Component(Tensor2SComponents.YZ), local_InitialStressTensor.Component(Tensor2SComponents.ZX));
                                        progressReporter.OutputMessage(string.Format("gc.PropControl.AddDeformationEpisode_TerzaghiStressLoad({0}, {1}, {2}, {3}, {4});", local_StressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStress_info, initialFP));
#endif
                                    }
                                    break;
                                case StressStateDefinition.BiotEffectiveStress:
                                    {
                                        gc.PropControl.AddDeformationEpisode_BiotStressLoad(local_StressRateTensor, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStressTensor, initialFP, local_BiotCoefficient);
#if DEBUG_FRAC_INPUT
                                        string local_StressRate_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_StressRateTensor.Component(Tensor2SComponents.XX), local_StressRateTensor.Component(Tensor2SComponents.YY), local_StressRateTensor.Component(Tensor2SComponents.ZZ), local_StressRateTensor.Component(Tensor2SComponents.XY), local_StressRateTensor.Component(Tensor2SComponents.YZ), local_StressRateTensor.Component(Tensor2SComponents.ZX));
                                        string local_InitialStress_info;
                                        if (local_InitialStressTensor is null)
                                            local_InitialStress_info = "null";
                                        else
                                            local_InitialStress_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_InitialStressTensor.Component(Tensor2SComponents.XX), local_InitialStressTensor.Component(Tensor2SComponents.YY), local_InitialStressTensor.Component(Tensor2SComponents.ZZ), local_InitialStressTensor.Component(Tensor2SComponents.XY), local_InitialStressTensor.Component(Tensor2SComponents.YZ), local_InitialStressTensor.Component(Tensor2SComponents.ZX));
                                        progressReporter.OutputMessage(string.Format("gc.PropControl.AddDeformationEpisode_BiotStressLoad({0}, {1}, {2}, {3}, {4}, {5});", local_StressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStress_info, initialFP, local_BiotCoefficient));
#endif
                                    }
                                    break;
                                default:
                                    progressReporter.OutputMessage(string.Format("No load defined for deformation episode {0}", deformationEpisodeNo));
                                    break;
                            }

                            // Update the record of the initial fluid pressure
                            initialFP += (local_AppliedOverpressureRate * ((local_DeformationEpisodeDuration > 0) ? local_DeformationEpisodeDuration : 0));

                        }// End add the deformation load data

                        // Create the fracture sets
                        if (Mode1Only)
                            gc.resetLayerBoundFractures(NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures);
                        else if (Mode2Only)
                            gc.resetLayerBoundFractures(NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures);
                        else
                            gc.resetLayerBoundFractures(NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, BiazimuthalConjugate, AllowReverseFractures);
                        if (NoUnconfinedFractureStrikeSets > 0)
                            gc.resetUnconfinedFractures(NoUnconfinedFractureStrikeSets, NoUnconfinedFractureDipSets, NoRaysPerUnconfinedFracture, local_minUnconfinedFractureRadius, local_maxUnconfinedFractureRadius, local_maxEffectiveUnconfinedFractureRadius, InitialMicrofractureDistributionFunction, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, local_InitialMicrofractureMedianRadius);

                        // Update the number and names of the unconfined fracture sets
                        if (NoUnconfinedFractureSets < gc.NoUnconfinedFractureSets)
                        {
                            NoUnconfinedFractureSets = gc.NoUnconfinedFractureSets;
                            UnconfinedFractureSetNames.Clear();
                            for (int newUFSSetNo = 0; newUFSSetNo < NoUnconfinedFractureSets; newUFSSetNo++)
                                UnconfinedFractureSetNames.Add(gc.getUnconfinedFractureSetName(newUFSSetNo));
                        }

#if DEBUG_FRAC_INPUT
                        if (Mode1Only)
                            progressReporter.OutputMessage(string.Format("gc.resetLayerBoundFractures({0}, {1}, {2}, FractureMode.{3}, {4});", NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures));
                        else if (Mode2Only)
                            progressReporter.OutputMessage(string.Format("gc.resetLayerBoundFractures({0}, {1}, {2}, FractureMode.{3}, {4});", NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures));
                        else
                            progressReporter.OutputMessage(string.Format("gc.resetLayerBoundFractures({0}, {1}, {2}, {3}, {4});", NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, BiazimuthalConjugate, AllowReverseFractures));
                        if (NoUnconfinedFractureStrikeSets > 0)
                            progressReporter.OutputMessage(string.Format("gc.resetUnconfinedFractures({0}, {1}, {2}, {3}, {4}, {5}, InitialFractureDistribution.{6}, {7}, {8}, {9});", NoUnconfinedFractureStrikeSets, NoUnconfinedFractureDipSets, NoRaysPerUnconfinedFracture, local_minUnconfinedFractureRadius, local_maxUnconfinedFractureRadius, local_maxEffectiveUnconfinedFractureRadius, InitialMicrofractureDistributionFunction, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, local_InitialMicrofractureMedianRadius));
#endif
                        // NB the fracture aperture control data must be set after the present day stress is defined, as it may be dependent on it

                        // If required, define the present day stress
#if DEBUG_FRAC_INPUT
                        progressReporter.OutputMessage("");
                        progressReporter.OutputMessage(string.Format("Use present day stress? {0}", UsePresentDayStress));
                        progressReporter.OutputMessage(string.Format("Define present day stress from {0}", PresentDayStressDefinition));
#endif
                        if (UsePresentDayStress)
                        {
                            switch (PresentDayStressDefinition)
                            {
                                case StressStateDefinition.Strain:
                                    {
                                        // For this option, stress will be calculated from present day lithostatic stress assuming no horizontal strin and no overpressure
                                        // This is provided for convenience but is not documented, as users re encouraged to defined the absolute or effective stress tensor
                                        // This will depend on whether we are averaging the strain and fluid overpressure properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                                        // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                                        double local_EhminAzi_PresentDay = 0;
                                        double local_Ehmin_PresentDay = 0;
                                        double local_Ehmax_PresentDay = 0;
                                        double local_AppliedOverpressure_PresentDay = 0;

                                        // Get the present day mechanical properties from the grid as required
                                        // This will depend on whether we are averaging the mechanical properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                                        // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                                        double local_PresentDayYoungsMod = double.NaN;
                                        double local_PresentDayPoissonsRatio = double.NaN;
                                        double local_PresentDayBiotCoefficient = DefaultPresentDayBiotCoefficient;

                                        if (AverageMechanicalPropertyData) // We are averaging over all Petrel cells in the gridblock
                                        {
                                            // Create local variables for running total and number of datapoints for each mechanical property
                                            double BiotCoeff_total = 0;
                                            int BiotCoeff_novalues = 0;

                                            // Loop through all the shadow grid cells in the gridblock
                                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                    for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                    {
                                                        // Update Biot coefficient total if defined
                                                        if (UseGridFor_PresentDayBiotCoefficient)
                                                        {
                                                            double cell_BiotCoeff = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayBiotCoefficientPropertyName);
                                                            if (!double.IsNaN(cell_BiotCoeff))
                                                            {
                                                                BiotCoeff_total += cell_BiotCoeff;
                                                                BiotCoeff_novalues++;
                                                            }
                                                        }

                                                    }

                                            // Update the gridblock values with the averages - if there is any data to calculate them from
                                            if (BiotCoeff_novalues > 0)
                                                local_PresentDayBiotCoefficient = BiotCoeff_total / (double)BiotCoeff_novalues;
                                        }
                                        else // We are taking data from a single cell
                                        {
                                            // Update Biot coefficient total if defined
                                            if (UseGridFor_PresentDayBiotCoefficient)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_BiotCoeff = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayBiotCoefficientPropertyName);
                                                    if (!double.IsNaN(cell_BiotCoeff))
                                                    {
                                                        local_PresentDayBiotCoefficient = cell_BiotCoeff;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        // End get the present day mechanical properties from the grid as required

                                        // If the present day mechanical properties are not defined, use the mechanical properties at the time of deformation
                                        // This is not required as the SetPresentDayStressFromStrain will automatically substitute mechanical properties at the time of deformation if NaNs are supplied
                                        /*if (double.IsNaN(local_YoungsMod_PresentDay))
                                            local_YoungsMod_PresentDay = local_YoungsMod;
                                        if (double.IsNaN(local_PoissonsRatio_PresentDay))
                                            local_PoissonsRatio_PresentDay = local_PoissonsRatio;
                                        if (double.IsNaN(local_BiotCoefficient_PresentDay))
                                            local_BiotCoefficient_PresentDay = local_BiotCoefficient;*/

                                        // Get the present day stress relaxation factor
                                        // This is a uniform constant across the grid
                                        double local_InitialStressRelaxation_PresentDay = double.NaN;
                                        // If it is not defined, use the initial stress relaxation at the time of deformation
                                        if (double.IsNaN(local_InitialStressRelaxation_PresentDay))
                                            local_InitialStressRelaxation_PresentDay = local_InitialStressRelaxation;

                                        // Now we can set the present day stress
                                        gc.SetPresentDayStressFromStrain(local_Ehmin_PresentDay, local_Ehmax_PresentDay, local_EhminAzi_PresentDay, local_AppliedOverpressure_PresentDay, local_PresentDayYoungsMod, local_PresentDayPoissonsRatio, local_PresentDayBiotCoefficient, local_InitialStressRelaxation_PresentDay);
#if DEBUG_FRAC_INPUT
                                        progressReporter.OutputMessage("");
                                        progressReporter.OutputMessage(string.Format("gc.SetPresentDayStressFromStrain({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_Ehmin_PresentDay, local_Ehmax_PresentDay, local_EhminAzi_PresentDay, local_AppliedOverpressure_PresentDay, local_PresentDayYoungsMod, local_PresentDayPoissonsRatio, local_PresentDayBiotCoefficient, local_InitialStressRelaxation_PresentDay));
                                        progressReporter.OutputMessage(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZX)));
#endif
                                    }
                                    break;
                                case StressStateDefinition.AbsoluteStress:
                                case StressStateDefinition.TerzaghiEffectiveStress:
                                case StressStateDefinition.BiotEffectiveStress:
                                    {
                                        // Get the present day absolute stress and fluid pressure from the grid as required
                                        // This will depend on whether we are averaging the stress and fluid pressure properties over all shadow grid cells that make up the gridblock, or taking the values from a single cell
                                        // First we will create local variables for the property values in this gridblock
                                        // By default these will be set to zero, since default values are not specified by the user 
                                        double local_Sxx_PresentDay = DefaultPresentDayStress_XX;
                                        double local_Syy_PresentDay = DefaultPresentDayStress_YY;
                                        double local_Szz_PresentDay = DefaultPresentDayStress_ZZ;
                                        double local_Sxy_PresentDay = DefaultPresentDayStress_XY;
                                        double local_Syz_PresentDay = DefaultPresentDayStress_YZ;
                                        double local_Szx_PresentDay = DefaultPresentDayStress_ZX;
                                        double local_FluidPressure_PresentDay = DefaultPresentDayFluidPressure;

                                        if (AverageStressStrainData) // We are averaging over all shadow grid cells in the gridblock
                                        {
                                            // Create local variables for running total and number of datapoints for each property
                                            double Sxx_PresentDay_total = 0;
                                            int Sxx_PresentDay_novalues = 0;
                                            double Syy_PresentDay_total = 0;
                                            int Syy_PresentDay_novalues = 0;
                                            double Szz_PresentDay_total = 0;
                                            int Szz_PresentDay_novalues = 0;
                                            double Sxy_PresentDay_total = 0;
                                            int Sxy_PresentDay_novalues = 0;
                                            double Syz_PresentDay_total = 0;
                                            int Syz_PresentDay_novalues = 0;
                                            double Szx_PresentDay_total = 0;
                                            int Szx_PresentDay_novalues = 0;
                                            double FluidPressure_total = 0;
                                            int FluidPressure_novalues = 0;

                                            // Loop through all the Petrel cells in the gridblock
                                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                    for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                    {
                                                        // Update XX stress component total if defined
                                                        if (UseGridFor_PresentDayStress_XX)
                                                        {
                                                            double cell_Sxx_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayStress_XXPropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_Sxx_PresentDay)
                                                            //    cell_Sxx_PresentDay = toSIStressUnits.Convert(cell_Sxx_PresentDay);
                                                            if (!double.IsNaN(cell_Sxx_PresentDay))
                                                            {
                                                                Sxx_PresentDay_total += cell_Sxx_PresentDay;
                                                                Sxx_PresentDay_novalues++;
                                                            }
                                                        }

                                                        // Update YY stress component total if defined
                                                        if (UseGridFor_PresentDayStress_YY)
                                                        {
                                                            double cell_Syy_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayStress_YYPropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_Syy_PresentDay)
                                                            //    cell_Syy_PresentDay = toSIStressUnits.Convert(cell_Syy_PresentDay);
                                                            if (!double.IsNaN(cell_Syy_PresentDay))
                                                            {
                                                                Syy_PresentDay_total += cell_Syy_PresentDay;
                                                                Syy_PresentDay_novalues++;
                                                            }
                                                        }

                                                        // Update ZZ stress component total if defined
                                                        if (UseGridFor_PresentDayStress_ZZ)
                                                        {
                                                            double cell_Szz_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayStress_ZZPropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_Szz_PresentDay)
                                                            //    cell_Szz_PresentDay = toSIStressUnits.Convert(cell_Szz_PresentDay);
                                                            if (!double.IsNaN(cell_Szz_PresentDay))
                                                            {
                                                                Szz_PresentDay_total += cell_Szz_PresentDay;
                                                                Szz_PresentDay_novalues++;
                                                            }
                                                        }

                                                        // Update XY stress component total if defined
                                                        if (UseGridFor_PresentDayStress_XY)
                                                        {
                                                            double cell_Sxy_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayStress_XYPropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_Sxy_PresentDay)
                                                            //    cell_Sxy_PresentDay = toSIStressUnits.Convert(cell_Sxy_PresentDay);
                                                            if (!double.IsNaN(cell_Sxy_PresentDay))
                                                            {
                                                                Sxy_PresentDay_total += cell_Sxy_PresentDay;
                                                                Sxy_PresentDay_novalues++;
                                                            }
                                                        }

                                                        // Update YZ stress component total if defined
                                                        if (UseGridFor_PresentDayStress_YZ)
                                                        {
                                                            double cell_Syz_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayStress_YZPropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_Syz_PresentDay)
                                                            //    cell_Syz_PresentDay = toSIStressUnits.Convert(cell_Syz_PresentDay);
                                                            if (!double.IsNaN(cell_Syz_PresentDay))
                                                            {
                                                                Syz_PresentDay_total += cell_Syz_PresentDay;
                                                                Syz_PresentDay_novalues++;
                                                            }
                                                        }

                                                        // Update ZX stress component total if defined
                                                        if (UseGridFor_PresentDayStress_ZX)
                                                        {
                                                            double cell_Szx_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayStress_ZXPropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_Szx_PresentDay)
                                                            //    cell_Szx_PresentDay = toSIStressUnits.Convert(cell_Szx_PresentDay);
                                                            if (!double.IsNaN(cell_Szx_PresentDay))
                                                            {
                                                                Szx_PresentDay_total += cell_Szx_PresentDay;
                                                                Szx_PresentDay_novalues++;
                                                            }
                                                        }

                                                        // Update fluid pressure total if defined
                                                        if (UseGridFor_PresentDayFluidPressure)
                                                        {
                                                            double cell_FluidPressure = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayFluidPressurePropertyName);
                                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                            //if (convertFromGeneral_FluidPressure_PresentDay)
                                                            //    cell_FluidPressure = toSIPressureUnits.Convert(cell_FluidPressure);
                                                            if (!double.IsNaN(cell_FluidPressure))
                                                            {
                                                                FluidPressure_total += cell_FluidPressure;
                                                                FluidPressure_novalues++;
                                                            }
                                                        }

                                                    }

                                            // Update the gridblock values with the averages - if there is any data to calculate them from
                                            if (Sxx_PresentDay_novalues > 0)
                                                local_Sxx_PresentDay = Sxx_PresentDay_total / (double)Sxx_PresentDay_novalues;
                                            if (Syy_PresentDay_novalues > 0)
                                                local_Syy_PresentDay = Syy_PresentDay_total / (double)Syy_PresentDay_novalues;
                                            if (Szz_PresentDay_novalues > 0)
                                                local_Szz_PresentDay = Szz_PresentDay_total / (double)Szz_PresentDay_novalues;
                                            if (Sxy_PresentDay_novalues > 0)
                                                local_Sxy_PresentDay = Sxy_PresentDay_total / (double)Sxy_PresentDay_novalues;
                                            if (Syz_PresentDay_novalues > 0)
                                                local_Syz_PresentDay = Syz_PresentDay_total / (double)Syz_PresentDay_novalues;
                                            if (Szx_PresentDay_novalues > 0)
                                                local_Szx_PresentDay = Szx_PresentDay_total / (double)Szx_PresentDay_novalues;
                                            if (FluidPressure_novalues > 0)
                                                local_FluidPressure_PresentDay = FluidPressure_total / (double)FluidPressure_novalues;
                                        }
                                        else // We are taking data from a single cell
                                        {
                                            // Update XX stress component total if defined
                                            if (UseGridFor_PresentDayStress_XX)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_Sxx_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayStress_XXPropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Sxx_PresentDay)
                                                    //    cell_Sxx_PresentDay = toSIStressUnits.Convert(cell_Sxx_PresentDay);
                                                    if (!double.IsNaN(cell_Sxx_PresentDay))
                                                    {
                                                        local_Sxx_PresentDay = cell_Sxx_PresentDay;
                                                        break;
                                                    }
                                                }
                                            }

                                            // Update YY stress component total if defined
                                            if (UseGridFor_PresentDayStress_YY)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_Syy_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayStress_YYPropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Syy_PresentDay)
                                                    //    cell_Syy_PresentDay = toSIStressUnits.Convert(cell_Syy_PresentDay);
                                                    if (!double.IsNaN(cell_Syy_PresentDay))
                                                    {
                                                        local_Syy_PresentDay = cell_Syy_PresentDay;
                                                        break;
                                                    }
                                                }
                                            }

                                            // Update ZZ stress component total if defined
                                            if (UseGridFor_PresentDayStress_ZZ)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_Szz_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayStress_ZZPropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Szz_PresentDay)
                                                    //    cell_Szz_PresentDay = toSIStressUnits.Convert(cell_Szz_PresentDay);
                                                    if (!double.IsNaN(cell_Szz_PresentDay))
                                                    {
                                                        local_Szz_PresentDay = cell_Szz_PresentDay;
                                                        break;
                                                    }
                                                }
                                            }

                                            // Update XY stress component total if defined
                                            if (UseGridFor_PresentDayStress_XY)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_Sxy_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayStress_XYPropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Sxy_PresentDay)
                                                    //    cell_Sxy_PresentDay = toSIStressUnits.Convert(cell_Sxy_PresentDay);
                                                    if (!double.IsNaN(cell_Sxy_PresentDay))
                                                    {
                                                        local_Sxy_PresentDay = cell_Sxy_PresentDay;
                                                        break;
                                                    }
                                                }
                                            }

                                            // Update YZ stress component total if defined
                                            if (UseGridFor_PresentDayStress_YZ)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_Syz_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayStress_YZPropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Syz_PresentDay)
                                                    //    cell_Syz_PresentDay = toSIStressUnits.Convert(cell_Syz_PresentDay);
                                                    if (!double.IsNaN(cell_Syz_PresentDay))
                                                    {
                                                        local_Syz_PresentDay = cell_Syz_PresentDay;
                                                        break;
                                                    }
                                                }
                                            }

                                            // Update ZX stress component total if defined
                                            if (UseGridFor_PresentDayStress_ZX)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_Szx_PresentDay = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayStress_ZXPropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_Szx_PresentDay)
                                                    //    cell_Szx_PresentDay = toSIStressUnits.Convert(cell_Szx_PresentDay);
                                                    if (!double.IsNaN(cell_Szx_PresentDay))
                                                    {
                                                        local_Szx_PresentDay = cell_Szx_PresentDay;
                                                        break;
                                                    }
                                                }
                                            }

                                            // Update fluid pressure total if defined
                                            if (UseGridFor_PresentDayFluidPressure)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_FluidPressure = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayFluidPressurePropertyName);
                                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                    //if (convertFromGeneral_FluidPressure_PresentDay)
                                                    //    cell_FluidPressure = toSIPressureUnits.Convert(cell_FluidPressure);
                                                    if (!double.IsNaN(cell_FluidPressure))
                                                    {
                                                        local_FluidPressure_PresentDay = cell_FluidPressure;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        // End get the present day absolute stress and fluid pressure from the grid as required

                                        // Get the present day mechanical properties from the grid as required
                                        // Only the Biot coefficient is relevant here (and this only if the Biot effective stress is defined)
                                        // This will depend on whether we are averaging the mechanical properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                                        // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                                        double local_PresentDayBiotCoefficient = DefaultPresentDayBiotCoefficient;

                                        if (AverageMechanicalPropertyData) // We are averaging over all Petrel cells in the gridblock
                                        {
                                            // Create local variables for running total and number of datapoints for each mechanical property
                                            double BiotCoeff_total = 0;
                                            int BiotCoeff_novalues = 0;

                                            // Loop through all the shadow grid cells in the gridblock
                                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                    for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                    {
                                                        // Update Biot coefficient total if defined
                                                        if (UseGridFor_PresentDayBiotCoefficient)
                                                        {
                                                            double cell_BiotCoeff = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PresentDayBiotCoefficientPropertyName);
                                                            if (!double.IsNaN(cell_BiotCoeff))
                                                            {
                                                                BiotCoeff_total += cell_BiotCoeff;
                                                                BiotCoeff_novalues++;
                                                            }
                                                        }

                                                    }

                                            // Update the gridblock values with the averages - if there is any data to calculate them from
                                            if (BiotCoeff_novalues > 0)
                                                local_PresentDayBiotCoefficient = BiotCoeff_total / (double)BiotCoeff_novalues;
                                        }
                                        else // We are taking data from a single cell
                                        {
                                            // Update Biot coefficient total if defined
                                            if (UseGridFor_PresentDayBiotCoefficient)
                                            {
                                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                                for (int ShadowGrid_DataCellK = ShadowGrid_HighestCellK; ShadowGrid_DataCellK <= ShadowGrid_LowestCellK; ShadowGrid_DataCellK++)
                                                {
                                                    double cell_BiotCoeff = SourceDataGrid.GetFloatingPointPropertyValue(ShadowGrid_DataCellI, ShadowGrid_DataCellJ, ShadowGrid_DataCellK, PresentDayBiotCoefficientPropertyName);
                                                    if (!double.IsNaN(cell_BiotCoeff))
                                                    {
                                                        local_PresentDayBiotCoefficient = cell_BiotCoeff;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        // End get the present day mechanical properties from the grid as required

                                        // Now we can set the present day stress, depending on the stress type selected
                                        if (PresentDayStressDefinition == StressStateDefinition.AbsoluteStress)
                                        {
                                            gc.SetPresentDayAbsoluteStress(local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay);
#if DEBUG_FRAC_INPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage(string.Format("gc.SetPresentDayAbsoluteStress({0}, {1}, {2}, {3}, {4}, {5}, {6});", local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay));
                                            progressReporter.OutputMessage(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZZ)));
#endif
                                        }
                                        else if (PresentDayStressDefinition == StressStateDefinition.TerzaghiEffectiveStress)
                                        {
                                            gc.SetPresentDayTerzaghiStress(local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay);
#if DEBUG_FRAC_INPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage(string.Format("gc.SetPresentDayTerzaghiStress({0}, {1}, {2}, {3}, {4}, {5});", local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay));
                                            progressReporter.OutputMessage(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZZ)));
#endif
                                        }
                                        else if (PresentDayStressDefinition == StressStateDefinition.BiotEffectiveStress)
                                        {
                                            gc.SetPresentDayBiotStress(local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay, local_PresentDayBiotCoefficient);
#if DEBUG_FRAC_INPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage(string.Format("gc.SetPresentDayBiotStress({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay, local_PresentDayBiotCoefficient));
                                            progressReporter.OutputMessage(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZZ)));
#endif
                                        }
                                    }
                                    break;
                                default:
                                    {
#if DEBUG_FRAC_INPUT
                                        progressReporter.OutputMessage("");
                                        progressReporter.OutputMessage(string.Format("Not setting present day stress"));
#endif
                                    }
                                    break;
                            }
                        }

                        // Set the fracture aperture control data
                        gc.SetFractureApertureControlData(Mode1HMin_UniformAperture, Mode2HMin_UniformAperture, Mode1HMax_UniformAperture, Mode2HMax_UniformAperture, Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier);
#if DEBUG_FRAC_INPUT
                        progressReporter.OutputMessage(string.Format("gc.SetFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", Mode1HMin_UniformAperture, Mode2HMin_UniformAperture, Mode1HMax_UniformAperture, Mode2HMax_UniformAperture, Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier));
#endif

                        // Set the mechanical property overrides for any fracture sets parallel to the defined cleavages
                        // NB Cleavage overrides are currently applied only to unconfined fracture sets, and not to layer-bound fracture sets
                        gc.SetCleavages(local_Cleavages, MaxConsistencyAngle);
#if DEBUG_FRAC_INPUT
                        foreach (Cleavage cleavage in local_Cleavages)
                            progressReporter.OutputMessage(string.Format("Set cleavage normal to ({0},{1},{2}): Gc {3}, MuFr {4}", cleavage.NormalVector.Component(VectorComponents.X), cleavage.NormalVector.Component(VectorComponents.Y), cleavage.NormalVector.Component(VectorComponents.Z), cleavage.GcOverride, cleavage.MuFrOverride));
#endif

                        // Add the gridblock to the grid
                        ModelGrid.AddGridblock(gc, FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo, !faultToWest, !faultToSouth, true, true, true, true);

#if DEBUG_FRAC_INPUT
                        progressReporter.OutputMessage(string.Format("ModelGrid.AddGridblock(gc, {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8});", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo, !faultToWest, !faultToSouth, true, true, true, true));
#endif

                        // Update gridblock counter
                        NoActiveGridblocks++;

                        //Update status bar
                        progressReporter.UpdateProgress(NoActiveGridblocks);

                    } // End loop through all gridblocks in the Fracture Grid

            // Set the DFN generation data
            DFNGenerationControl dfn_control = new DFNGenerationControl(GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, MinUnconfinedFractureRadius, -1, MaxNoFracturePatches, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, IntermediateOutputIntervalControl, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchAdjacentGridblocks, PropagateFracturesInNucleationOrder, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio, LargeFractureMinimumRadius, ModelTimeUnits);

#if DEBUG_FRAC_INPUT
            progressReporter.OutputMessage("");
            progressReporter.OutputMessage(string.Format("DFNGenerationControl dfn_control = new DFNGenerationControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, DFNFileType.{14}, {15}, {16}, {18}, {18}, {19}, {20}, {21}, TimeUnits.{22});", GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, MinUnconfinedFractureRadius, -1, MaxNoFractureSegments, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, IntermediateOutputIntervalControl, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchAdjacentGridblocks, PropagateFracturesInNucleationOrder, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio, LargeFractureMinimumRadius, ModelTimeUnits));
#endif

            // If the intermediate stage DFMs are set to be output at specified times, create a list of deformation episode end times in SI units for this purpose and supply it to the DFNGenerationControl object
            // NB In this case the DFNGenerationControl will ignore the specified NoIntermediateOutputs value; we recalculate it here only for internal use
            // Also create a local list of output stage name overrides
            List<string> OutputStageNameOverride = new List<string>();
            if (IntermediateOutputIntervalControl == IntermediateOutputInterval.SpecifiedTime)
            {
                List<double> DeformationEpisodeEndTimes_SITimeUnits_list = new List<double>();
                double currentEpisodeEndTime = 0;
                // Since the final deformation episode does not count as an intermediate output, we will start the Intermediate Output counter at -1
                NoIntermediateOutputs = -1;
                for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
                {
                    bool finalEpisode = (deformationEpisodeNo == (noDeformationEpisodes - 1));
                    double deformationEpisodeDuration = DeformationEpisodeDuration_list[deformationEpisodeNo];
                    // Intermediate deformation episodes will only be added to the list if they have a defined duration
                    // The final deformation episode will be added to the list even if the duration is undefined
                    if ((deformationEpisodeDuration > 0) || finalEpisode)
                    {
                        currentEpisodeEndTime += (deformationEpisodeDuration * TimeUnitConverter);
                        DeformationEpisodeEndTimes_SITimeUnits_list.Add(currentEpisodeEndTime);
                        if (deformationEpisodeNo < DeformationEpisodeName_list.Count)
                            OutputStageNameOverride.Add(DeformationEpisodeName_list[deformationEpisodeNo]);
                        else
                            OutputStageNameOverride.Add(null);
                        NoIntermediateOutputs++;
                    }
                    else
                    {
                        progressReporter.OutputMessage(string.Format("Duration is undefined for deformation episode {0}. This may cause errors in calculating the timing of intermediate outputs.", deformationEpisodeNo + 1));
                        progressReporter.OutputMessage(string.Format("Duration should be defined for all deformation episodes except the final episode, which can have undefined duration (run to fracture saturation)."));
                    }
                }
                dfn_control.IntermediateOutputTimes = DeformationEpisodeEndTimes_SITimeUnits_list;

#if DEBUG_FRAC_INPUT
                string setIntermediateTimes = "dfn_control.IntermediateOutputTimes = {";
                foreach (double nextEndTime in DeformationEpisodeEndTimes_SITimeUnits_list)
                    setIntermediateTimes += string.Format(" {0},", nextEndTime);
                setIntermediateTimes = setIntermediateTimes.TrimEnd(',');
                setIntermediateTimes += " }";
                progressReporter.OutputMessage(setIntermediateTimes);
#endif
            }

            // Set the output folder path
            dfn_control.FolderPath = outputFolderPath;

            // If required, set the flag to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file
            // This requires that we specify the depth of the horizontal section to extract the fracture traces onto
            if (!double.IsNaN(DepthOfHorizontalSection))
                dfn_control.Create2DFractureNetwork(DepthOfHorizontalSection);

            // Add the DFNGenerationControl object to the grid
            ModelGrid.DFNControl = dfn_control;

#if DEBUG_FRAC_INPUT
            progressReporter.OutputMessage(string.Format("dfn_control.FolderPath = {0};", outputFolderPath));
            progressReporter.OutputMessage(string.Format("ModelGrid.DFNControl = dfn_control;"));
#endif

            // Calculate implicit fractures for all gridblocks - unless the calculation has already been cancelled
            if (!progressReporter.abortCalculation())
            {
                progressReporter.OutputMessage("Start calculating implicit data");
                ModelGrid.CalculateAllFractureData(progressReporter);
            }

            // Calculate explicit DFN - unless the calculation has already been cancelled
            if (!progressReporter.abortCalculation())
            {
                progressReporter.OutputMessage("Start generating explicit DFN");
                ModelGrid.GenerateDFN(progressReporter);
            }

            // If the calculation has already been cancelled, do not write any output data
            if (!progressReporter.abortCalculation())
            {
                progressReporter.OutputMessage("Start writing output");

                // Write implicit fracture property data to shadow grid
                progressReporter.OutputMessage("Write implicit data");

                {
                    // Calculate the number of stages, the number of fracture sets and the total number of calculation elements
                    int NoStages = NoIntermediateOutputs + 1;
                    int NoCalculationElementsCompleted = 0;
                    int TotalNoSets = (NoLayerBoundFractureSets * NoDipSets) + NoUnconfinedFractureSets;
                    int NoSetTypes = (NoLayerBoundFractureSets > 0 ? 1 : 0) + (NoUnconfinedFractureSets > 0 ? 1 : 0);
                    int NoElements = NoActiveGridblocks * (TotalNoSets + (OutputFractureConnectivityAnisotropy ? TotalNoSets + NoSetTypes + 1 : 0) + (OutputFractureReactivationPotential ? TotalNoSets : 0) + (OutputFracturePorosity ? NoSetTypes : 0) + (OutputFracturePermeabilityTensor ? 1 : 0));
                    NoElements *= NoStages;
                    // Bulk rock elastic tensors are only output for the final stage
                    if (OutputBulkRockElasticTensors)
                        NoElements += NoActiveGridblocks;
                    progressReporter.SetNumberOfElements(NoElements);

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
                        double stageEndTime = 0;
                        string stageNameOverride = null;
                        switch (IntermediateOutputIntervalControl)
                        {
                            case IntermediateOutputInterval.SpecifiedTime:
                                double nextListValue = dfn_control.GetIntermediateOutputTime(stageNumber - 1); // List of intermediate outputs is zero-based
                                stageEndTime = !double.IsNaN(nextListValue) ? nextListValue : endTime; // If the next list value is NaN (i.e. we have reached the end of the list), used the end time instead
                                if (stageNumber <= OutputStageNameOverride.Count) stageNameOverride = OutputStageNameOverride[stageNumber - 1];
                                break;
                            case IntermediateOutputInterval.EqualTime:
                                stageEndTime = (stageNumber * endTime) / NoStages;
                                break;
                            case IntermediateOutputInterval.EqualArea:
                                int gridblockTimestepNo = ((stageNumber * NoGridblockTimesteps) / NoStages);
                                if (gridblockTimestepNo < 1)
                                    gridblockTimestepNo = 1;
                                if (gridblockTimestepNo > NoGridblockTimesteps)
                                    gridblockTimestepNo = NoGridblockTimesteps;
                                stageEndTime = (NoGridblockTimesteps > 0) ? timestepEndtimes[gridblockTimestepNo - 1] : 0;
                                break;
                            default:
                                break;
                        }

                        // Create a stage-specific label and description for the output
                        string outputLabel;
                        if ((stageNameOverride is null) || (stageNameOverride.Length == 0))
                            outputLabel = (stageNumber == NoStages) ? "Final" : string.Format("Stage{0}_Time{1}{2}", stageNumber, (stageEndTime / TimeUnitConverter).ToString("G3"), ModelTimeUnits);
                        else
                            outputLabel = stageNameOverride;
                        string outputStageParams = string.Format("Model name: {0}\n", ModelName);
                        outputStageParams += (stageNumber == NoStages) ? "Final stage" : string.Format("Stage {0}", stageNumber);
                        outputStageParams += (stageNameOverride is null) ? "\n" : string.Format(": {0}\n", stageNameOverride);
                        outputStageParams += string.Format("Time {0}{1}\n", stageEndTime / TimeUnitConverter, ModelTimeUnits);
                        outputStageParams += "\n";

                        // Create a new model stage output property folder in the shadow grid
                        SourceDataGrid.CreateNewStage(outputLabel);

#if DEBUG_FRAC_OUTPUT
                        progressReporter.OutputMessage("");
                        progressReporter.OutputMessage("Stage: " + outputLabel);
#endif

                        // If required, loop through each fracture set to output data
                        if (OutputFractureSets)
                        {
                            for (int LayerBoundFractureSetNo = 0; LayerBoundFractureSetNo < NoLayerBoundFractureSets; LayerBoundFractureSetNo++)
                            {
                                // Set a name for the fracture set
                                string FractureSetName = GridblockConfiguration.getLayerBoundFractureSetName(LayerBoundFractureSetNo, NoLayerBoundFractureSets);

                                for (int DipSetNo = 0; DipSetNo < NoDipSets; DipSetNo++)
                                {
                                    // Create a subfolder for the fracture dip set
                                    string dipsetLabel = (DipSetNo < DipSetLabels.Count ? DipSetLabels[DipSetNo] : "");
                                    string CollectionName = string.Format("{0}_{1}_", FractureSetName, dipsetLabel);

                                    // Write fracture density and length data to shadow grid
                                    {
                                        // Create properties and set templates for each property
                                        string MF_P30_tot = CollectionName + "Layer_bound_fracture_P30";
                                        SourceDataGrid.CreateFloatingPointProperty(MF_P30_tot);
                                        string MF_P32_tot = CollectionName + "Layer_bound_fracture_P32";
                                        SourceDataGrid.CreateFloatingPointProperty(MF_P32_tot);
                                        string uF_P32_tot = CollectionName + "Microfracture_P32";
                                        SourceDataGrid.CreateFloatingPointProperty(uF_P32_tot);
                                        string MF_MeanLength = CollectionName + "Mean_fracture_length";
                                        SourceDataGrid.CreateFloatingPointProperty(MF_MeanLength);

                                        // Loop through all gridblocks in the Fracture Grid
                                        // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                        for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                            for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                                for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                                {
                                                    // Check if calculation has been aborted
                                                    if (progressReporter.abortCalculation())
                                                    {
                                                        // Clean up any resources or data
                                                        break;
                                                    }

                                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                    GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                    if (fractureGridCell == null)
                                                        continue;

                                                    // Check if the fracture set and dipset exist in this gridblock - if so get a reference to the dipset object, otherwise update the progress bar and move on to the next gridblock
                                                    if ((LayerBoundFractureSetNo >= fractureGridCell.NoLayerBoundFractureSets) || (DipSetNo >= fractureGridCell.LayerBoundFractureSets[LayerBoundFractureSetNo].FractureDipSets.Count))
                                                    {
                                                        progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                        continue;
                                                    }
                                                    FractureDipSet fds = fractureGridCell.LayerBoundFractureSets[LayerBoundFractureSetNo].FractureDipSets[DipSetNo];

                                                    // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                                    int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                    int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                    int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                    if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                        ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                                    int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                                    if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                        ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                                    int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                                    int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                                    if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                        ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                                    // Get data from GridblockConfiguration object
                                                    double cell_MF_P30_tot, cell_MF_P32_tot, cell_uF_P32_tot, cell_MF_MeanLength;
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
                                                    bool writeMacrofractureData = PopulateEmptyGridblocks || (cell_MF_P32_tot > 0);

#if DEBUG_FRAC_OUTPUT
                                                    progressReporter.OutputMessage("");
                                                    progressReporter.OutputMessage(string.Format("Base data: Set {0} dipset {1}", LayerBoundFractureSetNo, DipSetNo));
                                                    progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                                    // Loop through all the shadow grid cells in the gridblock
                                                    try
                                                    {
                                                        for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                            for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                                for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                                {
#if DEBUG_FRAC_OUTPUT
                                                                    progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                                    // Write data to shadow grid
                                                                    // If the MFP32 value for the gridblock is 0 and we are not populating empty gridblocks, do not assign the MFP30, MFP32 and Mean MF Length values. This will enable easier visualisation of the fracture distribution.
                                                                    if (writeMacrofractureData)
                                                                    {
                                                                        SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_P30_tot, cell_MF_P30_tot);
                                                                        SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_P32_tot, cell_MF_P32_tot);
                                                                        SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_MeanLength, cell_MF_MeanLength);
                                                                    }
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, uF_P32_tot, cell_uF_P32_tot);
                                                                } // End loop through all the shadow grid cells in the gridblock
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string errorMessage = string.Format("Exception thrown when writing density data for fracture set {0} dipset {1} to row {2}, column {3}:", LayerBoundFractureSetNo, DipSetNo, FractureGrid_RowNo, FractureGrid_ColNo);
                                                        errorMessage = errorMessage + string.Format(" cell_MF_P30_tot {0}", (float)cell_MF_P30_tot);
                                                        errorMessage = errorMessage + string.Format(" cell_MF_P32_tot {0}", (float)cell_MF_P32_tot);
                                                        errorMessage = errorMessage + string.Format(" cell_uF_P32_tot {0}", (float)cell_uF_P32_tot);
                                                        errorMessage = errorMessage + string.Format(" cell_MF_MeanLength {0}", (float)cell_MF_MeanLength);
                                                        progressReporter.OutputMessage(errorMessage);
                                                        progressReporter.OutputMessage(e.Message);
                                                        progressReporter.OutputMessage(e.StackTrace);
                                                    }

                                                    // Update progress reporter
                                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                                } // End loop through all gridblocks in the Fracture Grid
                                    } // End write fracture density and length data to shadow grid

                                    // If required, write fracture connectivity data to shadow grid
                                    if (OutputFractureConnectivityAnisotropy)
                                    {
                                        // Create properties and set templates for each property
                                        string MF_UnconnectedTipRatio = CollectionName + "Unconnected_fracture_tip_ratio";
                                        SourceDataGrid.CreateFloatingPointProperty(MF_UnconnectedTipRatio);
                                        string MF_RelayTipRatio = CollectionName + "Relay_zone_fracture_tip_ratio";
                                        SourceDataGrid.CreateFloatingPointProperty(MF_RelayTipRatio);
                                        string MF_IntersectingTipRatio = CollectionName + "Intersecting_fracture_tip_ratio";
                                        SourceDataGrid.CreateFloatingPointProperty(MF_IntersectingTipRatio);
                                        string ConnectionsPerMF = CollectionName + "Connections_per_fracture";
                                        SourceDataGrid.CreateFloatingPointProperty(ConnectionsPerMF);
                                        string EndDeformationTime = CollectionName + "Time_of_end_macrofracture_growth";
                                        SourceDataGrid.CreateFloatingPointProperty(EndDeformationTime);

                                        // Loop through all gridblocks in the Fracture Grid
                                        // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                        for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                            for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                                for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                                {
                                                    // Check if calculation has been aborted
                                                    if (progressReporter.abortCalculation())
                                                    {
                                                        // Clean up any resources or data
                                                        break;
                                                    }

                                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                    GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                    if (fractureGridCell == null)
                                                        continue;

                                                    // Check if the fracture set and dipset exist in this gridblock - if so get a reference to the dipset object, otherwise update the progress bar and move on to the next gridblock
                                                    if ((LayerBoundFractureSetNo >= fractureGridCell.NoLayerBoundFractureSets) || (DipSetNo >= fractureGridCell.LayerBoundFractureSets[LayerBoundFractureSetNo].FractureDipSets.Count))
                                                    {
                                                        progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                        continue;
                                                    }
                                                    FractureDipSet fds = fractureGridCell.LayerBoundFractureSets[LayerBoundFractureSetNo].FractureDipSets[DipSetNo];

                                                    // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                                    int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                    int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                    int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                    if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                        ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                                    int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                                    if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                        ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                                    int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                                    int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                                    if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                        ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                                    // Get data from GridblockConfiguration object
                                                    double UnconnectedTipRatio, RelayTipRatio, IntersectingTipRatio, NodesPerMF, EndTime;
                                                    if (finalStage)
                                                    {
                                                        UnconnectedTipRatio = fds.UnconnectedTipRatio(!PopulateEmptyGridblocks);
                                                        RelayTipRatio = fds.RelayTipRatio(!PopulateEmptyGridblocks);
                                                        IntersectingTipRatio = fds.IntersectingTipRatio(!PopulateEmptyGridblocks);
                                                        NodesPerMF = fractureGridCell.ConnectionsPerMacrofracture(LayerBoundFractureSetNo, DipSetNo, !PopulateEmptyGridblocks);
                                                        EndTime = fds.getFinalActiveTime(!PopulateEmptyGridblocks);
                                                    }
                                                    else
                                                    {
                                                        int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                        double undefinedValue = PopulateEmptyGridblocks ? 0 : double.NaN;
                                                        double INodes = fds.getActiveMFP30(TSNo);
                                                        double RNodes = fds.getStaticRelayMFP30(TSNo);
                                                        double YNodes = fds.getStaticIntersectMFP30(TSNo);
                                                        double TotalNodes = INodes + RNodes + YNodes;
                                                        double NoConnections = (LinkStressShadows ? RNodes : 0) + YNodes + fds.getTerminatingFractureDensity(TSNo);
                                                        UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : undefinedValue + 1);
                                                        RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : undefinedValue);
                                                        IntersectingTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : undefinedValue);
                                                        NodesPerMF = (TotalNodes > 0 ? NoConnections / TotalNodes : undefinedValue);
                                                        EndTime = stageEndTime;
                                                    }

#if DEBUG_FRAC_OUTPUT
                                                    progressReporter.OutputMessage("");
                                                    progressReporter.OutputMessage(string.Format("Connectivity data: Set {0} dipset {1}", LayerBoundFractureSetNo, DipSetNo));
                                                    progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                                    // Loop through all the shadow grid cells in the gridblock
                                                    try
                                                    {
                                                        for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                            for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                                for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                                {
#if DEBUG_FRAC_OUTPUT
                                                                    progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                                    // Write data to shadow grid
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_UnconnectedTipRatio, UnconnectedTipRatio);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_RelayTipRatio, RelayTipRatio);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_IntersectingTipRatio, IntersectingTipRatio);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, ConnectionsPerMF, NodesPerMF);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, EndDeformationTime, EndTime);
                                                                } // End loop through all the shadow grid cells in the gridblock
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string errorMessage = string.Format("Exception thrown when writing anisotropy data for fracture set {0} dipset {1} to row {2}, column {3}:", LayerBoundFractureSetNo, DipSetNo, FractureGrid_RowNo, FractureGrid_ColNo);
                                                        errorMessage = errorMessage + string.Format(" UnconnectedTipRatio {0}", (float)UnconnectedTipRatio);
                                                        errorMessage = errorMessage + string.Format(" RelayTipRatio {0}", (float)RelayTipRatio);
                                                        errorMessage = errorMessage + string.Format(" IntersectingTipRatio {0}", (float)IntersectingTipRatio);
                                                        errorMessage = errorMessage + string.Format(" ConnectionsPerMF {0}", (float)NodesPerMF);
                                                        errorMessage = errorMessage + string.Format(" EndTime {0}", (float)EndTime);
                                                        progressReporter.OutputMessage(errorMessage);
                                                        progressReporter.OutputMessage(e.Message);
                                                        progressReporter.OutputMessage(e.StackTrace);
                                                    }

                                                    // Update progress reporter
                                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                                } // End loop through all gridblocks in the Fracture Grid
                                    } // End write fracture connectivity data to shadow grid

                                    // If required, write fracture reactivity data to shadow grid
                                    if (OutputFractureReactivationPotential)
                                    {
                                        // Create properties and set templates for each property
                                        string FDS_ReactivationPotential = CollectionName + "Reactivation_Potential";
                                        SourceDataGrid.CreateFloatingPointProperty(FDS_ReactivationPotential);
                                        string FDS_SlipTendency = CollectionName + "Slip_Tendency";
                                        SourceDataGrid.CreateFloatingPointProperty(FDS_SlipTendency);

                                        // Loop through all gridblocks in the Fracture Grid
                                        // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                        for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                            for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                                for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                                {
                                                    // Check if calculation has been aborted
                                                    if (progressReporter.abortCalculation())
                                                    {
                                                        // Clean up any resources or data
                                                        break;
                                                    }

                                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                    GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                    if (fractureGridCell == null)
                                                        continue;

                                                    // Check if the fracture set and dipset exist in this gridblock - if so get a reference to the dipset object, otherwise update the progress bar and move on to the next gridblock
                                                    if ((LayerBoundFractureSetNo >= fractureGridCell.NoLayerBoundFractureSets) || (DipSetNo >= fractureGridCell.LayerBoundFractureSets[LayerBoundFractureSetNo].FractureDipSets.Count))
                                                    {
                                                        progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                        continue;
                                                    }
                                                    FractureDipSet fds = fractureGridCell.LayerBoundFractureSets[LayerBoundFractureSetNo].FractureDipSets[DipSetNo];

                                                    // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                                    int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                    int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                    int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                    if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                        ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                                    int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                                    if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                        ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                                    int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                                    int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                                    if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                        ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                                    // Get data from GridblockConfiguration object
                                                    double ReactivationPotential = fds.PresentDayReactivationPotential;
                                                    double SlipTendency = fds.PresentDaySlipTendency;

#if DEBUG_FRAC_OUTPUT
                                                    progressReporter.OutputMessage("");
                                                    progressReporter.OutputMessage(string.Format("Reactivation potential: Set {0} dipset {1}", LayerBoundFractureSetNo, DipSetNo));
                                                    progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                                    // Loop through all the shadow grid cells in the gridblock
                                                    try
                                                    {
                                                        for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                            for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                                for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                                {
#if DEBUG_FRAC_OUTPUT
                                                                    progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                                    // Write data to shadow grid
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, FDS_ReactivationPotential, ReactivationPotential);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, FDS_SlipTendency, SlipTendency);
                                                                } // End loop through all the shadow grid cells in the gridblock
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string errorMessage = string.Format("Exception thrown when writing anisotropy data for fracture set {0} dipset {1} to row {2}, column {3}:", LayerBoundFractureSetNo, DipSetNo, FractureGrid_RowNo, FractureGrid_ColNo);
                                                        errorMessage = errorMessage + string.Format(" ReactivationPotential {0}", (float)ReactivationPotential);
                                                        errorMessage = errorMessage + string.Format(" SlipTendency {0}", (float)SlipTendency);
                                                        progressReporter.OutputMessage(errorMessage);
                                                        progressReporter.OutputMessage(e.Message);
                                                        progressReporter.OutputMessage(e.StackTrace);
                                                    }

                                                    // Update progress reporter
                                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                                } // End loop through all gridblocks in the Fracture Grid
                                    } // End write fracture reactivity data to shadow grid

                                } // End loop through fracture dip sets
                            } // End loop through layer-bound fracture sets

                            for (int UnconfinedFractureSetNo = 0; UnconfinedFractureSetNo < NoUnconfinedFractureSets; UnconfinedFractureSetNo++)
                            {
                                // Set a name for the fracture set
                                string FractureSetName = string.Format("UnconfinedFractureSet{0}", UnconfinedFractureSetNo);

                                // Create a subfolder for the unconfined fracture set
                                string CollectionName = string.Format("{0}_", FractureSetName);

                                // Write fracture density and length data to shadow grid
                                {
                                    // Create properties and set templates for each property
                                    string UCF_P30_tot = CollectionName + "Unconfined_fracture_P30";
                                    SourceDataGrid.CreateFloatingPointProperty(UCF_P30_tot);
                                    string UCF_P32_tot = CollectionName + "Unconfined_fracture_P32";
                                    SourceDataGrid.CreateFloatingPointProperty(UCF_P32_tot);
                                    string UCF_MeanArea = CollectionName + "Mean_fracture_area";
                                    SourceDataGrid.CreateFloatingPointProperty(UCF_MeanArea);

                                    // Loop through all gridblocks in the Fracture Grid
                                    // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                    for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                        for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                            for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressReporter.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                if (fractureGridCell == null)
                                                    continue;

                                                // Check if the unconfined fracture set exists in this gridblock - if so get a reference to the unconfined fracture set object, otherwise update the progress bar and move on to the next gridblock
                                                if (UnconfinedFractureSetNo >= fractureGridCell.NoUnconfinedFractureSets)
                                                {
                                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                    continue;
                                                }
                                                UnconfinedFractureSet ufs = fractureGridCell.UnconfinedFractureSets[UnconfinedFractureSetNo];

                                                // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                                                int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                    ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                                int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                                if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                    ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                                int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                                int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                                if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                    ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                                // Get data from GridblockConfiguration object
                                                // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
                                                // we must always take data from the FractureCalculationData list
                                                double cell_UCF_P30_tot, cell_UCF_P32_tot, cell_UCF_MeanArea;
                                                if (finalStage)
                                                {
                                                    cell_UCF_P30_tot = ufs.getTotalUCFP30();
                                                    cell_UCF_P32_tot = ufs.getTotalUCFP32();
                                                    cell_UCF_MeanArea = cell_UCF_P32_tot / cell_UCF_P30_tot;
                                                }
                                                else
                                                {
                                                    int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                    cell_UCF_P30_tot = ufs.getTotalUCFP30(TSNo);
                                                    cell_UCF_P32_tot = ufs.getTotalUCFP32(TSNo);
                                                    cell_UCF_MeanArea = cell_UCF_P32_tot / cell_UCF_P30_tot;
                                                }
                                                bool writeUCFData = PopulateEmptyGridblocks || (cell_UCF_P32_tot > 0);

#if DEBUG_FRAC_OUTPUT
                                                progressReporter.OutputMessage("");
                                                progressReporter.OutputMessage(string.Format("Base data: Set {0}", UnconfinedFractureSetNo));
                                                progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                                // Loop through all the shadow grid cells in the gridblock
                                                try
                                                {
                                                    for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                        for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                            for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                            {
#if DEBUG_FRAC_OUTPUT
                                                                progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                                // Write data to shadow grid
                                                                // If the UCFP32 value for the gridblock is 0 and we are not populating empty gridblocks, do not assign the UCFP30, UCFP32 and Mean UCF area values. This will enable easier visualisation of the fracture distribution.
                                                                if (writeUCFData)
                                                                {
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_P30_tot, cell_UCF_P30_tot);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_P32_tot, cell_UCF_P32_tot);
                                                                    SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_MeanArea, cell_UCF_MeanArea);
                                                                }
                                                            } // End loop through all the shadow grid cells in the gridblock
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing density data for unconfined fracture set {0} to column {1}, row {2}, layer {3}:", UnconfinedFractureSetNo, FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                    errorMessage = errorMessage + string.Format(" cell_UCF_P30_tot {0}", (float)cell_UCF_P30_tot);
                                                    errorMessage = errorMessage + string.Format(" cell_UCF_P32_tot {0}", (float)cell_UCF_P32_tot);
                                                    errorMessage = errorMessage + string.Format(" cell_UCF_MeanArea {0}", (float)cell_UCF_MeanArea);
                                                    progressReporter.OutputMessage(errorMessage);
                                                    progressReporter.OutputMessage(e.Message);
                                                    progressReporter.OutputMessage(e.StackTrace);
                                                }

                                                // Update progress reporter
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all gridblocks in the Fracture Grid
                                } // End write fracture density and length data to shadow grid

                                // If required, write fracture connectivity data to Petrel grid
                                if (OutputFractureConnectivityAnisotropy)
                                {
                                    // Create properties and set templates for each property
                                    string UCF_UnconnectedTipRatio = CollectionName + "Unconnected_fracture_tip_ratio";
                                    SourceDataGrid.CreateFloatingPointProperty(UCF_UnconnectedTipRatio);
                                    string UCF_RelayTipRatio = CollectionName + "Relay_zone_fracture_tip_ratio";
                                    SourceDataGrid.CreateFloatingPointProperty(UCF_RelayTipRatio);
                                    string UCF_IntersectingTipRatio = CollectionName + "Intersecting_fracture_tip_ratio";
                                    SourceDataGrid.CreateFloatingPointProperty(UCF_IntersectingTipRatio);
                                    string ConnectionsPerUCF = CollectionName + "Connections_per_fracture";
                                    SourceDataGrid.CreateFloatingPointProperty(ConnectionsPerUCF);
                                    string EndDeformationTime = CollectionName + "Time_of_end_macrofracture_growth";
                                    SourceDataGrid.CreateFloatingPointProperty(EndDeformationTime);

                                    // Loop through all gridblocks in the Fracture Grid
                                    // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                    for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                        for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                            for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressReporter.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                if (fractureGridCell == null)
                                                    continue;

                                                // Check if the unconfined fracture set exists in this gridblock - if so get a reference to the unconfined fracture set object, otherwise update the progress bar and move on to the next gridblock
                                                if (UnconfinedFractureSetNo >= fractureGridCell.NoUnconfinedFractureSets)
                                                {
                                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                    continue;
                                                }
                                                UnconfinedFractureSet ufs = fractureGridCell.UnconfinedFractureSets[UnconfinedFractureSetNo];

                                                // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                                                int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                    ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                                int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                                if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                    ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                                int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                                int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                                if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                    ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                                // Get data from GridblockConfiguration object
                                                // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
                                                // we must always take data from the FractureCalculationData list
                                                double UnconnectedTipRatio, RelayTipRatio, IntersectingTipRatio, NodesPerUCF, EndTime;
                                                if (finalStage)
                                                {
                                                    double undefinedValue = PopulateEmptyGridblocks ? 0 : double.NaN;
                                                    double INodes = ufs.getActive_RP30_M() + ufs.get_RP30_M(RayPropagationStatus.StaticMaxRadius);
                                                    double RNodes = ufs.get_RP30_M(RayPropagationStatus.StaticStressShadow);
                                                    double YNodes = ufs.get_RP30_M(RayPropagationStatus.StaticIntersection);
                                                    double TotalFractures = INodes + RNodes + YNodes;
                                                    double NoConnections = (LinkStressShadows ? RNodes : 0) + YNodes + ufs.getTerminatingFractureDensity();
                                                    UnconnectedTipRatio = (TotalFractures > 0 ? INodes / TotalFractures : undefinedValue + 1);
                                                    RelayTipRatio = (TotalFractures > 0 ? RNodes / TotalFractures : undefinedValue);
                                                    IntersectingTipRatio = (TotalFractures > 0 ? YNodes / TotalFractures : undefinedValue);
                                                    NodesPerUCF = (TotalFractures > 0 ? NoConnections / TotalFractures : undefinedValue);
                                                    EndTime = ufs.getFinalActiveTime(!PopulateEmptyGridblocks);
                                                }
                                                else
                                                {
                                                    int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                    double undefinedValue = PopulateEmptyGridblocks ? 0 : double.NaN;
                                                    double INodes = ufs.getActive_RP30_M(TSNo) + ufs.get_RP30_M(RayPropagationStatus.StaticMaxRadius, TSNo);
                                                    double RNodes = ufs.get_RP30_M(RayPropagationStatus.StaticStressShadow, TSNo);
                                                    double YNodes = ufs.get_RP30_M(RayPropagationStatus.StaticIntersection, TSNo);
                                                    double TotalFractures = INodes + RNodes + YNodes;
                                                    double NoConnections = (LinkStressShadows ? RNodes : 0) + YNodes + ufs.getTerminatingFractureDensity(TSNo);
                                                    UnconnectedTipRatio = (TotalFractures > 0 ? INodes / TotalFractures : undefinedValue + 1);
                                                    RelayTipRatio = (TotalFractures > 0 ? RNodes / TotalFractures : undefinedValue);
                                                    IntersectingTipRatio = (TotalFractures > 0 ? YNodes / TotalFractures : undefinedValue);
                                                    NodesPerUCF = (TotalFractures > 0 ? NoConnections / TotalFractures : undefinedValue);
                                                    EndTime = stageEndTime;
                                                }

#if DEBUG_FRAC_OUTPUT
                                                progressReporter.OutputMessage("");
                                                progressReporter.OutputMessage(string.Format("Connectivity data: Set {0}", UnconfinedFractureSetNo));
                                                progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                                // Loop through all the shadow grid cells in the gridblock
                                                try
                                                {
                                                    for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                        for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                            for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                            {
#if DEBUG_FRAC_OUTPUT
                                                                progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                                // Write data to shadow grid
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_UnconnectedTipRatio, UnconnectedTipRatio);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_RelayTipRatio, RelayTipRatio);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_IntersectingTipRatio, IntersectingTipRatio);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, ConnectionsPerUCF, NodesPerUCF);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, EndDeformationTime, EndTime);
                                                            } // End loop through all the shadow grid cells in the gridblock
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing anisotropy data for unconfined fracture set {0} to column {1}, row {2}, layer {3}:", UnconfinedFractureSetNo, FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                    errorMessage = errorMessage + string.Format(" UnconnectedTipRatio {0}", (float)UnconnectedTipRatio);
                                                    errorMessage = errorMessage + string.Format(" RelayTipRatio {0}", (float)RelayTipRatio);
                                                    errorMessage = errorMessage + string.Format(" IntersectingTipRatio {0}", (float)IntersectingTipRatio);
                                                    errorMessage = errorMessage + string.Format(" ConnectionsPerMF {0}", (float)NodesPerUCF);
                                                    errorMessage = errorMessage + string.Format(" EndTime {0}", (float)EndTime);
                                                    progressReporter.OutputMessage(errorMessage);
                                                    progressReporter.OutputMessage(e.Message);
                                                    progressReporter.OutputMessage(e.StackTrace);
                                                }

                                                // Update progress reporter
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all gridblocks in the Fracture Grid
                                } // End write fracture connectivity data to shadow grid

                                // Write fracture reactivity data to Petrel grid
                                if (OutputFractureReactivationPotential)
                                {
                                    // Create properties and set templates for each property
                                    string UFS_ReactivationPotential = CollectionName + "Reactivation_Potential";
                                    SourceDataGrid.CreateFloatingPointProperty(UFS_ReactivationPotential);
                                    string UFS_SlipTendency = CollectionName + "Slip_Tendency";
                                    SourceDataGrid.CreateFloatingPointProperty(UFS_SlipTendency);

                                    // Loop through all gridblocks in the Fracture Grid
                                    // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                    for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                        for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                            for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressReporter.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                if (fractureGridCell == null)
                                                    continue;

                                                // Check if the unconfined fracture set exists in this gridblock - if so get a reference to the unconfined fracture set object, otherwise update the progress bar and move on to the next gridblock
                                                if (UnconfinedFractureSetNo >= fractureGridCell.NoUnconfinedFractureSets)
                                                {
                                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                    continue;
                                                }
                                                UnconfinedFractureSet ufs = fractureGridCell.UnconfinedFractureSets[UnconfinedFractureSetNo];

                                                // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                                int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                                int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                                int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                                if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                    ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                                int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                                if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                    ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                                int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                                int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                                if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                    ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                                // Get data from GridblockConfiguration object
                                                double ReactivationPotential = ufs.PresentDayReactivationPotential;
                                                double SlipTendency = ufs.PresentDaySlipTendency;

#if DEBUG_FRAC_OUTPUT
                                                progressReporter.OutputMessage("");
                                                progressReporter.OutputMessage(string.Format("Reactivation potential: Unconfined set {0}", UnconfinedFractureSetNo));
                                                progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                                // Loop through all the shadow grid cells in the gridblock
                                                try
                                                {
                                                    for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                        for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                            for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                            {
#if DEBUG_FRAC_OUTPUT
                                                                progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                                // Write data to shadow grid
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UFS_ReactivationPotential, ReactivationPotential);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UFS_SlipTendency, SlipTendency);
                                                            } // End loop through all the shadow grid cells in the gridblock
                                                }
                                                catch (Exception e)
                                                {
                                                    string errorMessage = string.Format("Exception thrown when writing reactivation potential data for unconfined fracture set {0} to column {1}, row {2}, layer {3}:", UnconfinedFractureSetNo, FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                                    errorMessage = errorMessage + string.Format(" ReactivationPotential {0}", (float)ReactivationPotential);
                                                    errorMessage = errorMessage + string.Format(" SlipTendency {0}", (float)SlipTendency);
                                                    progressReporter.OutputMessage(errorMessage);
                                                    progressReporter.OutputMessage(e.Message);
                                                    progressReporter.OutputMessage(e.StackTrace);
                                                }

                                                // Update progress reporter
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all gridblocks in the Fracture Grid
                                } // End write fracture reactivity data to shadow grid

                            } // End loop through unconfined fracture sets

                        } // End write fracture set data

                        // If required, write fracture anisotropy data to shadow grid
                        if (OutputFractureConnectivityAnisotropy)
                        {
                            // First write microfracture and layer-bound fracture anisotropy data, if present
                            if (NoLayerBoundFractureSets > 0)
                            {
                                // Create properties and set templates for each property
                                string CollectionName = "LayerBoundFractures_";
                                string P32_Anisotropy = CollectionName + "P32_anisotropy";
                                SourceDataGrid.CreateFloatingPointProperty(P32_Anisotropy);
                                string P33_Anisotropy = CollectionName + (OutputFracturePorosity ? "FracturePorosity_anisotropy" : "P33_anisotropy");
                                SourceDataGrid.CreateFloatingPointProperty(P33_Anisotropy);
                                string MF_UnconnectedTipRatio = CollectionName + "Unconnected_fracture_tip_ratio";
                                SourceDataGrid.CreateFloatingPointProperty(MF_UnconnectedTipRatio);
                                string MF_RelayTipRatio = CollectionName + "Relay_zone_fracture_tip_ratio";
                                SourceDataGrid.CreateFloatingPointProperty(MF_RelayTipRatio);
                                string MF_IntersectingTipRatio = CollectionName + "Intersecting_fracture_tip_ratio";
                                SourceDataGrid.CreateFloatingPointProperty(MF_IntersectingTipRatio);
                                string ConnectionsPerMF = CollectionName + "Connections_per_fracture";
                                SourceDataGrid.CreateFloatingPointProperty(ConnectionsPerMF);

                                // Loop through all gridblocks in the Fracture Grid
                                // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                    for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressReporter.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                            int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                            int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                            int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                            int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                            if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                            // Calculate fracture anisotropy and connectivity for the entire fracture network
                                            double P32_anisotropy, P33_anisotropy;
                                            double UnconnectedTipRatio, RelayTipRatio, IntersectingTipRatio, NodesPerMF;
                                            if (finalStage)
                                            {
                                                // Calculate fracture anisotropy data using the functions in the GridblockConfiguration object
                                                P32_anisotropy = fractureGridCell.P32AnisotropyIndex(true, !PopulateEmptyGridblocks);
                                                if (OutputFracturePorosity)
                                                    P33_anisotropy = fractureGridCell.FracturePorosityAnisotropyIndex(true, !PopulateEmptyGridblocks);
                                                else
                                                    P33_anisotropy = fractureGridCell.P33AnisotropyIndex(true, !PopulateEmptyGridblocks);

                                                // Calculate fracture connectivity data using the functions in the GridblockConfiguration object
                                                UnconnectedTipRatio = fractureGridCell.UnconnectedMFTipRatio(!PopulateEmptyGridblocks);
                                                RelayTipRatio = fractureGridCell.RelayMFTipRatio(!PopulateEmptyGridblocks);
                                                IntersectingTipRatio = fractureGridCell.IntersectingMFTipRatio(!PopulateEmptyGridblocks);
                                                NodesPerMF = fractureGridCell.ConnectionsPerMacrofracture(!PopulateEmptyGridblocks);
                                            }
                                            else
                                            {
                                                int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                double undefinedValue = PopulateEmptyGridblocks ? 0 : double.NaN;

                                                // Calculate fracture anisotropy data using the data cached in the FCDList
                                                double Min_P32 = 0;
                                                double Max_P32 = 0;
                                                double Min_P33 = 0;
                                                double Max_P33 = 0;
                                                // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
                                                if (NoLayerBoundFractureSets < 2)
                                                {
                                                    Max_P32 = 1;
                                                    Max_P33 = 1;
                                                }
                                                else
                                                {
                                                    foreach (FractureDipSet fds in fractureGridCell.LayerBoundFractureSets[0].FractureDipSets)
                                                    {
                                                        Max_P32 += (fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo));
                                                        if (OutputFracturePorosity)
                                                            Max_P33 += (fds.getTotaluFPorosity(TSNo) + fds.getTotalMFPorosity(TSNo));
                                                        else
                                                            Max_P33 += (fds.getTotaluFP33(TSNo) + fds.getTotalMFP33(TSNo));
                                                    }
                                                    Min_P32 = Max_P32;
                                                    Min_P33 = Max_P33;

                                                    for (int fs_Index = 1; fs_Index < NoLayerBoundFractureSets; fs_Index++)
                                                    {
                                                        double fs_P32 = 0;
                                                        double fs_P33 = 0;
                                                        foreach (FractureDipSet fds in fractureGridCell.LayerBoundFractureSets[fs_Index].FractureDipSets)
                                                        {
                                                            fs_P32 += (fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo));
                                                            if (OutputFracturePorosity)
                                                                fs_P32 += (fds.getTotaluFPorosity(TSNo) + fds.getTotalMFPorosity(TSNo));
                                                            else
                                                                fs_P33 += (fds.getTotaluFP33(TSNo) + fds.getTotalMFP33(TSNo));
                                                        }
                                                        if (fs_P32 > Max_P32)
                                                            Max_P32 = fs_P32;
                                                        if (fs_P32 < Min_P32)
                                                            Min_P32 = fs_P32;
                                                        if (fs_P33 > Max_P33)
                                                            Max_P33 = fs_P33;
                                                        if (fs_P33 < Min_P33)
                                                            Min_P33 = fs_P33;
                                                    }
                                                }
                                                double Combined_P32 = Min_P32 + Max_P32;
                                                P32_anisotropy = (Combined_P32 > 0 ? (Max_P32 - Min_P32) / Combined_P32 : undefinedValue);
                                                double Combined_P33 = Min_P33 + Max_P33;
                                                P33_anisotropy = (Combined_P33 > 0 ? (Max_P33 - Min_P33) / Combined_P33 : undefinedValue);

                                                // Calculate fracture connectivity data using the data cached in the FCDList
                                                double INodes = 0;
                                                double RNodes = 0;
                                                double YNodes = 0;
                                                foreach (LayerBoundFractureSet fs in fractureGridCell.LayerBoundFractureSets)
                                                    foreach (FractureDipSet fds in fs.FractureDipSets)
                                                    {
                                                        INodes += fds.getActiveMFP30(TSNo);
                                                        RNodes += fds.getStaticRelayMFP30(TSNo);
                                                        YNodes += fds.getStaticIntersectMFP30(TSNo);
                                                    }
                                                double TotalNodes = INodes + RNodes + YNodes;
                                                UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : undefinedValue + 1);
                                                RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : undefinedValue);
                                                IntersectingTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : undefinedValue);
                                                double NoConnections = (LinkStressShadows ? RNodes : 0) + (2 * YNodes);
                                                NodesPerMF = (TotalNodes > 0 ? NoConnections / TotalNodes : undefinedValue);
                                            }

#if DEBUG_FRAC_OUTPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage("Connectivity data: all layer-bound sets");
                                            progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                            // Loop through all the shadow grid cells in the gridblock
                                            try
                                            {
                                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                        {
#if DEBUG_FRAC_OUTPUT
                                                            progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                            // Write data to shadow grid
                                                            if (!double.IsNaN(P32_anisotropy))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, P32_Anisotropy, P32_anisotropy);
                                                            if (!double.IsNaN(P33_anisotropy))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, P33_Anisotropy, P33_anisotropy);
                                                            if (!double.IsNaN(UnconnectedTipRatio))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_UnconnectedTipRatio, UnconnectedTipRatio);
                                                            if (!double.IsNaN(RelayTipRatio))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_RelayTipRatio, RelayTipRatio);
                                                            if (!double.IsNaN(IntersectingTipRatio))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_IntersectingTipRatio, IntersectingTipRatio);
                                                            if (!double.IsNaN(NodesPerMF))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, ConnectionsPerMF, NodesPerMF);
                                                        } // End loop through all the shadow grid cells in the gridblock
                                            }
                                            catch (Exception e)
                                            {
                                                string errorMessage = string.Format("Exception thrown when writing anisotropy data to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                errorMessage = errorMessage + string.Format(" P32_anisotropy {0}", (float)P32_anisotropy);
                                                errorMessage = errorMessage + string.Format(" P33_anisotropy {0}", (float)P33_anisotropy);
                                                errorMessage = errorMessage + string.Format(" UnconnectedTipRatio {0}", (float)UnconnectedTipRatio);
                                                errorMessage = errorMessage + string.Format(" RelayTipRatio {0}", (float)RelayTipRatio);
                                                errorMessage = errorMessage + string.Format(" IntersectingTipRatio {0}", (float)IntersectingTipRatio);
                                                errorMessage = errorMessage + string.Format(" ConnectionsPerMF {0}", (float)NodesPerMF);
                                                progressReporter.OutputMessage(errorMessage);
                                                progressReporter.OutputMessage(e.Message);
                                                progressReporter.OutputMessage(e.StackTrace);
                                            }

                                            // Update progress reporter
                                            progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all gridblocks in the Fracture Grid
                            } // End write microfracture and layer-bound fracture anisotropy data

                            // Then write unconfined fracture anisotropy data, if present
                            if (NoUnconfinedFractureSets > 0)
                            {
                                // Create properties and set templates for each property
                                string CollectionName = "UnconfinedFractures_";
                                // Anisotropy indices are not defined for unconfined fractures
                                //string P32_Anisotropy = CollectionName + "P32_anisotropy";
                                //SourceDataGrid.CreateFloatingPointProperty(P32_Anisotropy);
                                //string P33_Anisotropy = CollectionName + (OutputFracturePorosity ? "FracturePorosity_anisotropy" : "P33_anisotropy");
                                //SourceDataGrid.CreateFloatingPointProperty(P33_Anisotropy);
                                string UCF_UnconnectedTipRatio = CollectionName + "Unconnected_fracture_tip_ratio";
                                SourceDataGrid.CreateFloatingPointProperty(UCF_UnconnectedTipRatio);
                                string UCF_RelayTipRatio = CollectionName + "Relay_zone_fracture_tip_ratio";
                                SourceDataGrid.CreateFloatingPointProperty(UCF_RelayTipRatio);
                                string UCF_IntersectingTipRatio = CollectionName + "Intersecting_fracture_tip_ratio";
                                SourceDataGrid.CreateFloatingPointProperty(UCF_IntersectingTipRatio);
                                string ConnectionsPerUCF = CollectionName + "Connections_per_fracture";
                                SourceDataGrid.CreateFloatingPointProperty(ConnectionsPerUCF);

                                // Loop through all gridblocks in the Fracture Grid
                                // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                    for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressReporter.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                            int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                            int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                            int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                            int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                            if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                            // Calculate fracture connectivity for the entire fracture network
                                            // Anisotropy indices are not defined for unconfined fractures
                                            //double P32_anisotropy, P33_anisotropy;
                                            double UnconnectedTipRatio, RelayTipRatio, IntersectingTipRatio, NodesPerUCF;
                                            if (finalStage)
                                            {
                                                /*// Calculate fracture anisotropy data using the functions in the GridblockConfiguration object
                                                P32_anisotropy = fractureGridCell.P32AnisotropyIndex(true, !PopulateEmptyGridblocks);
                                                if (OutputFracturePorosity)
                                                    P33_anisotropy = fractureGridCell.FracturePorosityAnisotropyIndex(true, !PopulateEmptyGridblocks);
                                                else
                                                    P33_anisotropy = fractureGridCell.P33AnisotropyIndex(true, !PopulateEmptyGridblocks);*/

                                                // Calculate fracture connectivity data using the functions in the GridblockConfiguration object
                                                UnconnectedTipRatio = fractureGridCell.UnconnectedUCFTipRatio(!PopulateEmptyGridblocks);
                                                RelayTipRatio = fractureGridCell.RelayUCFTipRatio(!PopulateEmptyGridblocks);
                                                IntersectingTipRatio = fractureGridCell.IntersectingUCFTipRatio(!PopulateEmptyGridblocks);
                                                NodesPerUCF = fractureGridCell.ConnectionsPerUnconfinedFracture(!PopulateEmptyGridblocks);
                                            }
                                            else
                                            {
                                                int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                double undefinedValue = PopulateEmptyGridblocks ? 0 : double.NaN;

                                                /*// Calculate fracture anisotropy data using the data cached in the FCDList
                                                double Min_P32 = 0;
                                                double Max_P32 = 0;
                                                double Min_P33 = 0;
                                                double Max_P33 = 0;
                                                // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
                                                if (NoLayerBoundFractureSets < 2)
                                                {
                                                    Max_P32 = 1;
                                                    Max_P33 = 1;
                                                }
                                                else
                                                {
                                                    foreach (FractureDipSet fds in fractureGridCell.LayerBoundFractureSets[0].FractureDipSets)
                                                    {
                                                        Max_P32 += (fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo));
                                                        if (OutputFracturePorosity)
                                                            Max_P33 += (fds.getTotaluFPorosity(TSNo) + fds.getTotalMFPorosity(TSNo));
                                                        else
                                                            Max_P33 += (fds.getTotaluFP33(TSNo) + fds.getTotalMFP33(TSNo));
                                                    }
                                                    Min_P32 = Max_P32;
                                                    Min_P33 = Max_P33;

                                                    for (int fs_Index = 1; fs_Index < NoLayerBoundFractureSets; fs_Index++)
                                                    {
                                                        double fs_P32 = 0;
                                                        double fs_P33 = 0;
                                                        foreach (FractureDipSet fds in fractureGridCell.LayerBoundFractureSets[fs_Index].FractureDipSets)
                                                        {
                                                            fs_P32 += (fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo));
                                                            if (OutputFracturePorosity)
                                                                fs_P32 += (fds.getTotaluFPorosity(TSNo) + fds.getTotalMFPorosity(TSNo));
                                                            else
                                                                fs_P33 += (fds.getTotaluFP33(TSNo) + fds.getTotalMFP33(TSNo));
                                                        }
                                                        if (fs_P32 > Max_P32)
                                                            Max_P32 = fs_P32;
                                                        if (fs_P32 < Min_P32)
                                                            Min_P32 = fs_P32;
                                                        if (fs_P33 > Max_P33)
                                                            Max_P33 = fs_P33;
                                                        if (fs_P33 < Min_P33)
                                                            Min_P33 = fs_P33;
                                                    }
                                                }
                                                double Combined_P32 = Min_P32 + Max_P32;
                                                P32_anisotropy = (Combined_P32 > 0 ? (Max_P32 - Min_P32) / Combined_P32 : undefinedValue);
                                                double Combined_P33 = Min_P33 + Max_P33;
                                                P33_anisotropy = (Combined_P33 > 0 ? (Max_P33 - Min_P33) / Combined_P33 : undefinedValue);*/

                                                // Calculate fracture connectivity data using the data cached in the FCDList
                                                double INodes = 0;
                                                double RNodes = 0;
                                                double YNodes = 0;
                                                foreach (UnconfinedFractureSet ufs in fractureGridCell.UnconfinedFractureSets)
                                                {
                                                    INodes += ufs.getActive_RP30_M(TSNo) + ufs.get_RP30_M(RayPropagationStatus.StaticMaxRadius, TSNo);
                                                    RNodes += ufs.get_RP30_M(RayPropagationStatus.StaticStressShadow, TSNo);
                                                    YNodes += ufs.get_RP30_M(RayPropagationStatus.StaticIntersection, TSNo);
                                                }
                                                double TotalNodes = INodes + RNodes + YNodes;
                                                UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : undefinedValue + 1);
                                                RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : undefinedValue);
                                                IntersectingTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : undefinedValue);
                                                double NoConnections = (LinkStressShadows ? RNodes : 0) + (2 * YNodes);
                                                NodesPerUCF = (TotalNodes > 0 ? NoConnections / TotalNodes : undefinedValue);
                                            }

#if DEBUG_FRAC_OUTPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage("Connectivity data: all unconfined sets");
                                            progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                            // Loop through all the shadow grid cells in the gridblock
                                            try
                                            {
                                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                        {
#if DEBUG_FRAC_OUTPUT
                                                            progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                            // Write data to shadow grid
                                                            /*if (!double.IsNaN(P32_anisotropy))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, P32_Anisotropy, P32_anisotropy);
                                                            if (!double.IsNaN(P33_anisotropy))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, P33_Anisotropy, P33_anisotropy);*/
                                                            if (!double.IsNaN(UnconnectedTipRatio))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_UnconnectedTipRatio, UnconnectedTipRatio);
                                                            if (!double.IsNaN(RelayTipRatio))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_RelayTipRatio, RelayTipRatio);
                                                            if (!double.IsNaN(IntersectingTipRatio))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_IntersectingTipRatio, IntersectingTipRatio);
                                                            if (!double.IsNaN(NodesPerUCF))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, ConnectionsPerUCF, NodesPerUCF);
                                                        } // End loop through all the shadow grid cells in the gridblock
                                            }
                                            catch (Exception e)
                                            {
                                                string errorMessage = string.Format("Exception thrown when writing anisotropy data to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                //errorMessage = errorMessage + string.Format(" P32_anisotropy {0}", (float)P32_anisotropy);
                                                //errorMessage = errorMessage + string.Format(" P33_anisotropy {0}", (float)P33_anisotropy);
                                                errorMessage = errorMessage + string.Format(" UnconnectedTipRatio {0}", (float)UnconnectedTipRatio);
                                                errorMessage = errorMessage + string.Format(" RelayTipRatio {0}", (float)RelayTipRatio);
                                                errorMessage = errorMessage + string.Format(" IntersectingTipRatio {0}", (float)IntersectingTipRatio);
                                                errorMessage = errorMessage + string.Format(" ConnectionsPerMF {0}", (float)NodesPerUCF);
                                                progressReporter.OutputMessage(errorMessage);
                                                progressReporter.OutputMessage(e.Message);
                                                progressReporter.OutputMessage(e.StackTrace);
                                            }

                                            // Update progress reporter
                                            progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all gridblocks in the Fracture Grid
                            } // End write unconfined fracture anisotropy data

                            // Finally write end time data for all fracture sets
                            {
                                // Create properties and set templates for each property
                                string CollectionName = "AllFractures_";
                                string EndDeformationTime = CollectionName + "Time_of_end_fracture_growth";
                                SourceDataGrid.CreateFloatingPointProperty(EndDeformationTime);

                                // Loop through all gridblocks in the Fracture Grid
                                // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                    for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressReporter.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                            int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                            int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                            int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                            int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                            if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                            // Calculate end time for the entire fracture network
                                            double EndTime;
                                            if (finalStage)
                                            {
                                                // Calculate end deformation time using the function in the GridblockConfiguration object
                                                // This will represent either the end of the deformation episode or the time of fracture saturation, whichever is earliest
                                                EndTime = fractureGridCell.getFinalActiveTime(!PopulateEmptyGridblocks);
                                            }
                                            else
                                            {
                                                // Get the end deformation time or the time at the end of this intermediate stage, if it is earlier
                                                EndTime = fractureGridCell.getFinalActiveTime(!PopulateEmptyGridblocks);
                                                if (EndTime > stageEndTime)
                                                    EndTime = stageEndTime;
                                            }

#if DEBUG_FRAC_OUTPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage("End time data: all sets");
                                            progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                            // Loop through all the shadow grid cells in the gridblock
                                            try
                                            {
                                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                        {
#if DEBUG_FRAC_OUTPUT
                                                            progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                            // Write data to shadow grid
                                                            if (!double.IsNaN(EndTime))
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, EndDeformationTime, EndTime);
                                                        } // End loop through all the shadow grid cells in the gridblock
                                            }
                                            catch (Exception e)
                                            {
                                                string errorMessage = string.Format("Exception thrown when writing end time data to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                errorMessage = errorMessage + string.Format(" EndTime {0}", (float)EndTime);
                                                progressReporter.OutputMessage(errorMessage);
                                                progressReporter.OutputMessage(e.Message);
                                                progressReporter.OutputMessage(e.StackTrace);
                                            }

                                            // Update progress reporter
                                            progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all gridblocks in the Fracture Grid
                            } // End write end time data for all fracture sets

                        } // End write fracture anisotropy data

                        // Write fracture porosity data to shadow grid
                        if (OutputFracturePorosity)
                        {
                            // Create properties and set templates for each property
                            string CollectionName = "AllFractures_";
                            string apertureLabel = "";
                            switch (FractureApertureControl)
                            {
                                case FractureApertureType.Uniform:
                                    apertureLabel = "_UniformAperture";
                                    break;
                                case FractureApertureType.SizeDependent:
                                    apertureLabel = "_SizeDependentAperture";
                                    break;
                                case FractureApertureType.Dynamic:
                                    apertureLabel = "_DynamicAperture";
                                    break;
                                case FractureApertureType.BartonBandis:
                                    apertureLabel = "_BartonBandisAperture";
                                    break;
                                default:
                                    break;
                            }

                            // First write microfracture and layer-bound fracture porosity data, if present
                            if (NoLayerBoundFractureSets > 0)
                            {
                                // Set porosity property names based on aperture type
                                string uF_P32combined = CollectionName + "uFP32";
                                SourceDataGrid.CreateFloatingPointProperty(uF_P32combined);
                                string MF_P32combined = CollectionName + "MFP32";
                                SourceDataGrid.CreateFloatingPointProperty(MF_P32combined);
                                string uF_Porosity = CollectionName + "uF_Porosity" + apertureLabel;
                                SourceDataGrid.CreateFloatingPointProperty(uF_Porosity);
                                string MF_Porosity = CollectionName + "MF_Porosity" + apertureLabel;
                                SourceDataGrid.CreateFloatingPointProperty(MF_Porosity);

                                // Loop through all gridblocks in the Fracture Grid
                                // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                    for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressReporter.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                            int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                            int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                            int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                            int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                            if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                            // Get combined fracture porosity data from the FractureSet objects in the GridblockConfiguration object and combine them locally
                                            double uF_P32_value;
                                            double MF_P32_value;
                                            double uF_Porosity_value;
                                            double MF_Porosity_value;

                                            if (finalStage)
                                            {
                                                uF_P32_value = fractureGridCell.MicrofractureDensity_P32();
                                                MF_P32_value = fractureGridCell.LayerBoundFractureDensity_P32();
                                                uF_Porosity_value = fractureGridCell.MicrofracturePorosity();
                                                MF_Porosity_value = fractureGridCell.LayerBoundFracturePorosity();
                                            }
                                            else
                                            {
                                                int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                uF_P32_value = fractureGridCell.MicrofractureDensity_P32(TSNo);
                                                MF_P32_value = fractureGridCell.LayerBoundFractureDensity_P32(TSNo);
                                                uF_Porosity_value = fractureGridCell.MicrofracturePorosity(TSNo);
                                                MF_Porosity_value = fractureGridCell.LayerBoundFracturePorosity(TSNo);
                                            }
                                            bool writeMacrofractureData = PopulateEmptyGridblocks || (MF_P32_value > 0);

#if DEBUG_FRAC_OUTPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage("Porosity data: all sets");
                                            progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                            // Loop through all the shadow grid cells in the gridblock
                                            try
                                            {
                                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                        {
#if DEBUG_FRAC_OUTPUT
                                                            progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                            // Write data to shadow grid
                                                            SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, uF_P32combined, uF_P32_value);
                                                            SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, uF_Porosity, uF_Porosity_value);
                                                            if (writeMacrofractureData)
                                                            {
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_P32combined, MF_P32_value);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, MF_Porosity, MF_Porosity_value);
                                                            }
                                                        } // End loop through all the shadow grid cells in the gridblock
                                            }
                                            catch (Exception e)
                                            {
                                                string errorMessage = string.Format("Exception thrown when writing porosity data to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                errorMessage = errorMessage + string.Format(" uF_P32_value {0}", (float)uF_P32_value);
                                                errorMessage = errorMessage + string.Format(" MF_P32_value {0}", (float)MF_P32_value);
                                                errorMessage = errorMessage + string.Format(" uF_Porosity_value {0}", (float)uF_Porosity_value);
                                                errorMessage = errorMessage + string.Format(" MF_Porosity_value {0}", (float)MF_Porosity_value);
                                                progressReporter.OutputMessage(errorMessage);
                                                progressReporter.OutputMessage(e.Message);
                                                progressReporter.OutputMessage(e.StackTrace);
                                            }

                                            // Update progress reporter
                                            progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all gridblocks in the Fracture Grid
                            } // End write microfracture and layer-bound fracture porosity data

                            // Then write unconfined fracture porosity data, if present
                            if (NoUnconfinedFractureSets > 0)
                            {
                                // Set porosity property names based on aperture type
                                string UCF_P32combined = CollectionName + "UCFP32";
                                SourceDataGrid.CreateFloatingPointProperty(UCF_P32combined);
                                string UCF_Porosity = CollectionName + "UCF_Porosity" + apertureLabel;
                                SourceDataGrid.CreateFloatingPointProperty(UCF_Porosity);

                                // Loop through all gridblocks in the Fracture Grid
                                // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                                for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                    for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                        for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                        {
                                            // Check if calculation has been aborted
                                            if (progressReporter.abortCalculation())
                                            {
                                                // Clean up any resources or data
                                                break;
                                            }

                                            // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                            GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                            if (fractureGridCell == null)
                                                continue;

                                            // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                            int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                            int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                                ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                            int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                            if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                                ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                            int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                            int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                            if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                                ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                            // Get combined fracture porosity data from the FractureSet objects in the GridblockConfiguration object and combine them locally
                                            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
                                            // we must always take data from the FractureCalculationData list
                                            double UCF_P32_value;
                                            double UCF_Porosity_value;
                                            if (finalStage)
                                            {
                                                UCF_P32_value = fractureGridCell.UnconfinedFractureDensity_P32();
                                                UCF_Porosity_value = fractureGridCell.UnconfinedFracturePorosity();
                                            }
                                            else
                                            {
                                                int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                                UCF_P32_value = fractureGridCell.UnconfinedFractureDensity_P32(TSNo);
                                                UCF_Porosity_value = fractureGridCell.UnconfinedFracturePorosity(TSNo);
                                            }
                                            bool writeUnconfinedFractureData = PopulateEmptyGridblocks || (UCF_P32_value > 0);

#if DEBUG_FRAC_OUTPUT
                                            progressReporter.OutputMessage("");
                                            progressReporter.OutputMessage("Porosity data: all sets");
                                            progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                            // Loop through all the shadow grid cells in the gridblock
                                            try
                                            {
                                                for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                    for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                        for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                        {
#if DEBUG_FRAC_OUTPUT
                                                            progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                            // Write data to shadow grid
                                                            if (writeUnconfinedFractureData)
                                                            {
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_P32combined, UCF_P32_value);
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, UCF_Porosity, UCF_Porosity_value);
                                                            }
                                                        } // End loop through all the shadow grid cells in the gridblock
                                            }
                                            catch (Exception e)
                                            {
                                                string errorMessage = string.Format("Exception thrown when writing porosity data to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                                errorMessage = errorMessage + string.Format(" UCF_P32_value {0}", (float)UCF_P32_value);
                                                errorMessage = errorMessage + string.Format(" UCF_Porosity_value {0}", (float)UCF_Porosity_value);
                                                progressReporter.OutputMessage(errorMessage);
                                                progressReporter.OutputMessage(e.Message);
                                                progressReporter.OutputMessage(e.StackTrace);
                                            }

                                            // Update progress reporter
                                            progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                        } // End loop through all gridblocks in the Fracture Grid
                            } // End write unconfined fracture porosity data

                        } // End write fracture porosity data

                        // Write fracture permeability tensor and sigma factor data to shadow grid
                        if (OutputFracturePermeabilityTensor)
                        {
                            // Get the base property name for the fracture permeability tensor components and sigma factors
                            string FracturePermeabilityTensorCollectionName;
                            string PermeabilityTensorComponentName_base;
                            switch (FractureTypesInPermeabilityTensor)
                            {
                                case FractureType.Microfractures:
                                    FracturePermeabilityTensorCollectionName = "Microfracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_uF_";
                                    break;
                                case FractureType.LayerBoundFractures:
                                    FracturePermeabilityTensorCollectionName = "Macrofracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_MF_";
                                    break;
                                case FractureType.UnconfinedFractures:
                                    FracturePermeabilityTensorCollectionName = "Unconfined fracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_UCF_";
                                    break;
                                case FractureType.AllFractures:
                                    FracturePermeabilityTensorCollectionName = "Fracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_F_";
                                    break;
                                default:
                                    FracturePermeabilityTensorCollectionName = "";
                                    PermeabilityTensorComponentName_base = "";
                                    break;
                            }
                            switch (PermeabilityAlgorithm)
                            {
                                case PermeabilityCalculationAlgorithm.Oda1986:
                                    FracturePermeabilityTensorCollectionName += ": Oda (1986)";
                                    break;
                                case PermeabilityCalculationAlgorithm.OdaCorrected1987:
                                    FracturePermeabilityTensorCollectionName += ": Oda corrected (1987)";
                                    break;
                                case PermeabilityCalculationAlgorithm.SizeConnectivityCorrected:
                                    FracturePermeabilityTensorCollectionName += ": Connectivity and size corrected";
                                    break;
                                default:
                                    break;
                            }

                            // Create properties and set templates for each component of both tensors
                            Dictionary<Tensor2SComponents, string> PermeabilityTensorProperties = new Dictionary<Tensor2SComponents, string>();
                            Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                            foreach (Tensor2SComponents ij in tensorComponents)
                            {
                                string PermeabilityTensor_ij = string.Format("{0}{1}", PermeabilityTensorComponentName_base, ij);
                                PermeabilityTensorProperties[ij] = PermeabilityTensor_ij;
                                SourceDataGrid.CreateFloatingPointProperty(PermeabilityTensor_ij);
                            }
                            // Create property for the sigma factor
                            string SigmaFactorProperty = string.Format("{0}Sigma", PermeabilityTensorComponentName_base);
                            SourceDataGrid.CreateFloatingPointProperty(SigmaFactorProperty);

                            // Loop through all gridblocks in the Fracture Grid
                            // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                            for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                    for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                    {
                                        // Check if calculation has been aborted
                                        if (progressReporter.abortCalculation())
                                        {
                                            // Clean up any resources or data
                                            break;
                                        }

                                        // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                        GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                        if (fractureGridCell == null)
                                            continue;

                                        // If we are not populating empty cells, we are outputting the macrofracture permability tensor, and there are no macrofractures in the gridblock, move on to the next gridblock
                                        if (!PopulateEmptyGridblocks && (FractureTypesInPermeabilityTensor == FractureType.LayerBoundFractures))
                                        {
                                            double MF_P32_value = finalStage ? fractureGridCell.LayerBoundFractureDensity_P32() : fractureGridCell.LayerBoundFractureDensity_P32(fractureGridCell.getTimestepIndex(stageEndTime));
                                            if (!(MF_P32_value > 0))
                                            {
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);
                                                continue;
                                            }
                                        }

                                        // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                        int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                        int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                        int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                        if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                            ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                        int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                        if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                            ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                        int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                        int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                        if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                            ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                        // Get the appropriate permeability tensor for this gridblock
                                        Tensor2S gridblockPermeabilityTensor;
                                        double gridblockSigmaFactor;
                                        if (finalStage)
                                        {
                                            switch (FractureTypesInPermeabilityTensor)
                                            {
                                                case FractureType.Microfractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.MicrofracturePermeability();
                                                    gridblockSigmaFactor = fractureGridCell.MicrofractureSigmaFactor();
                                                    break;
                                                case FractureType.LayerBoundFractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.MacrofracturePermeability();
                                                    gridblockSigmaFactor = fractureGridCell.MacrofractureSigmaFactor();
                                                    break;
                                                case FractureType.UnconfinedFractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.UnconfinedFracturePermeability();
                                                    gridblockSigmaFactor = fractureGridCell.UnconfinedFractureSigmaFactor();
                                                    break;
                                                case FractureType.AllFractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.TotalFracturePermeability();
                                                    gridblockSigmaFactor = fractureGridCell.TotalFractureSigmaFactor();
                                                    break;
                                                default:
                                                    gridblockPermeabilityTensor = new Tensor2S();
                                                    gridblockSigmaFactor = double.NaN;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            int TSNo = fractureGridCell.getTimestepIndex(stageEndTime);
                                            switch (FractureTypesInPermeabilityTensor)
                                            {
                                                case FractureType.Microfractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.MicrofracturePermeability(TSNo);
                                                    gridblockSigmaFactor = fractureGridCell.MicrofractureSigmaFactor(TSNo);
                                                    break;
                                                case FractureType.LayerBoundFractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.MacrofracturePermeability(TSNo);
                                                    gridblockSigmaFactor = fractureGridCell.MacrofractureSigmaFactor(TSNo);
                                                    break;
                                                case FractureType.UnconfinedFractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.UnconfinedFracturePermeability(TSNo);
                                                    gridblockSigmaFactor = fractureGridCell.UnconfinedFractureSigmaFactor(TSNo);
                                                    break;
                                                case FractureType.AllFractures:
                                                    gridblockPermeabilityTensor = fractureGridCell.TotalFracturePermeability(TSNo);
                                                    gridblockSigmaFactor = fractureGridCell.TotalFractureSigmaFactor(TSNo);
                                                    break;
                                                default:
                                                    gridblockPermeabilityTensor = new Tensor2S();
                                                    gridblockSigmaFactor = double.NaN;
                                                    break;
                                            }
                                        }

#if DEBUG_FRAC_OUTPUT
                                        progressReporter.OutputMessage("");
                                        progressReporter.OutputMessage("Permability tensor: all sets");
                                        progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                        // Loop through all the shadow grid cells in the gridblock
                                        // We need to define the last ij and kl components outside the loop so we can identify the tensor component if an exception is thrown
                                        Tensor2SComponents lastij = Tensor2SComponents.XX;
                                        try
                                        {
                                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                    for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                    {
#if DEBUG_FRAC_OUTPUT
                                                        progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                        // Write tensor component data to shadow grid
                                                        foreach (Tensor2SComponents ij in tensorComponents)
                                                        {
                                                            lastij = ij;
                                                            SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, PermeabilityTensorProperties[ij], gridblockPermeabilityTensor.Component(ij));
                                                        }
                                                        SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, SigmaFactorProperty, gridblockSigmaFactor);
                                                    } // End loop through all the shadow grid cells in the gridblock
                                        }
                                        catch (Exception e)
                                        {
                                            string errorMessage = string.Format("Exception thrown when writing fracture permeability tensor components to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                            errorMessage = errorMessage + string.Format(" {0}{1} {2}, ", PermeabilityTensorComponentName_base, lastij, (float)gridblockPermeabilityTensor.Component(lastij));
                                            errorMessage = errorMessage + string.Format(" {0}{1} {2}, ", PermeabilityTensorComponentName_base, "sigma", (float)gridblockSigmaFactor);
                                            progressReporter.OutputMessage(errorMessage);
                                            progressReporter.OutputMessage(e.Message);
                                            progressReporter.OutputMessage(e.StackTrace);
                                        }

                                        // Update progress reporter
                                        progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                    } // End loop through all gridblocks in the Fracture Grid

                        } // End write fracture permeability tensor data

                        // Write stiffness and compliance tensor data to shadow grid
                        if (OutputBulkRockElasticTensors && finalStage)
                        {

                            // Create properties and set templates for each component of both tensors
                            Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>> ComplianceTensorProperties = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>>();
                            Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>> StiffnessTensorProperties = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>>();
                            Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                            foreach (Tensor2SComponents ij in tensorComponents)
                            {
                                ComplianceTensorProperties[ij] = new Dictionary<Tensor2SComponents, string>();
                                StiffnessTensorProperties[ij] = new Dictionary<Tensor2SComponents, string>();
                                foreach (Tensor2SComponents kl in tensorComponents)
                                {
                                    string ComplianceTensor_ijkl = string.Format("S_{0}{1}", ij, kl);
                                    ComplianceTensorProperties[ij][kl] = ComplianceTensor_ijkl;
                                    SourceDataGrid.CreateFloatingPointProperty(ComplianceTensor_ijkl);

                                    string StiffnessTensor_ijkl = string.Format("C_{0}{1}", ij, kl);
                                    StiffnessTensorProperties[ij][kl] = StiffnessTensor_ijkl;
                                    SourceDataGrid.CreateFloatingPointProperty(StiffnessTensor_ijkl);
                                }
                            }

                            // Loop through all gridblocks in the Fracture Grid
                            // ColNo corresponds to the shadow grid I index, RowNo corresponds to the shadow grid J index, and LayerNo corresponds to the shadow grid K index
                            for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
                                for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                                    for (int FractureGrid_LayerNo = 0; FractureGrid_LayerNo < NoFractureGridLayers; FractureGrid_LayerNo++)
                                    {
                                        // Check if calculation has been aborted
                                        if (progressReporter.abortCalculation())
                                        {
                                            // Clean up any resources or data
                                            break;
                                        }

                                        // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                        GridblockConfiguration fractureGridCell = ModelGrid.GetGridblock(FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo);
                                        if (fractureGridCell == null)
                                            continue;

                                        // Create indices for the all the shadow grid grid cells corresponding to the fracture gridblock 
                                        int ShadowGrid_FirstCellI = ShadowGrid_StartColI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                                        int ShadowGrid_FirstCellJ = ShadowGrid_StartRowJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                                        int ShadowGrid_LastCellI = ShadowGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                                        if (ShadowGrid_LastCellI > ShadowGrid_EndColI)
                                            ShadowGrid_LastCellI = ShadowGrid_EndColI;
                                        int ShadowGrid_LastCellJ = ShadowGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
                                        if (ShadowGrid_LastCellJ > ShadowGrid_EndRowJ)
                                            ShadowGrid_LastCellJ = ShadowGrid_EndRowJ;
                                        int ShadowGrid_LowestCellK = ShadowGrid_BottomLayerK - (FractureGrid_LayerNo * VerticalUpscalingFactor);
                                        int ShadowGrid_HighestCellK = ShadowGrid_LowestCellK - (VerticalUpscalingFactor - 1);
                                        if (ShadowGrid_HighestCellK < ShadowGrid_TopLayerK)
                                            ShadowGrid_HighestCellK = ShadowGrid_TopLayerK;

                                        // Get the compliance and stiffness tensors for this gridblock
                                        Tensor4_2Sx2S gridblockComplianceTensor = fractureGridCell.S_b;
                                        Tensor4_2Sx2S gridblockStiffnessTensor = gridblockComplianceTensor.Inverse();

#if DEBUG_FRAC_OUTPUT
                                        progressReporter.OutputMessage("");
                                        progressReporter.OutputMessage("Stiffness and compliance tensors: all sets");
                                        progressReporter.OutputMessage(string.Format("FractureGrid gridblock {0}, {1}, {2}", FractureGrid_ColNo, FractureGrid_RowNo, FractureGrid_LayerNo));
#endif

                                        // Loop through all the shadow grid cells in the gridblock
                                        // We need to define the last ij and kl components outside the loop so we can identify the tensor component if an exception is thrown
                                        Tensor2SComponents lastij = Tensor2SComponents.XX;
                                        Tensor2SComponents lastkl = Tensor2SComponents.XX;
                                        try
                                        {
                                            for (int ShadowGrid_I = ShadowGrid_FirstCellI; ShadowGrid_I <= ShadowGrid_LastCellI; ShadowGrid_I++)
                                                for (int ShadowGrid_J = ShadowGrid_FirstCellJ; ShadowGrid_J <= ShadowGrid_LastCellJ; ShadowGrid_J++)
                                                    for (int ShadowGrid_K = ShadowGrid_HighestCellK; ShadowGrid_K <= ShadowGrid_LowestCellK; ShadowGrid_K++)
                                                    {
#if DEBUG_FRAC_OUTPUT
                                                        progressReporter.OutputMessage(string.Format("ShadowGrid cell {0}, {1}, {2}", ShadowGrid_I, ShadowGrid_J, ShadowGrid_K));
#endif

                                                        // Write tensor component data to shadow grid
                                                        foreach (Tensor2SComponents ij in tensorComponents)
                                                        {
                                                            lastij = ij;
                                                            foreach (Tensor2SComponents kl in tensorComponents)
                                                            {
                                                                lastkl = kl;
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, ComplianceTensorProperties[ij][kl], gridblockComplianceTensor.Component(ij, kl));
                                                                SourceDataGrid.SetFloatingPointProperty(ShadowGrid_I, ShadowGrid_J, ShadowGrid_K, StiffnessTensorProperties[ij][kl], gridblockStiffnessTensor.Component(ij, kl));
                                                            }
                                                        }
                                                    } // End loop through all the shadow grid cells in the gridblock
                                        }
                                        catch (Exception e)
                                        {
                                            string errorMessage = string.Format("Exception thrown when writing bulk rock elastic tensor components to row {0}, column {1}:", FractureGrid_RowNo, FractureGrid_ColNo);
                                            errorMessage = errorMessage + string.Format(" S_{0}{1} {2}", lastij, lastkl, (float)gridblockComplianceTensor.Component(lastij, lastkl));
                                            errorMessage = errorMessage + string.Format(" C_{0}{1} {2}", lastij, lastkl, (float)gridblockStiffnessTensor.Component(lastij, lastkl));
                                            progressReporter.OutputMessage(errorMessage);
                                            progressReporter.OutputMessage(e.Message);
                                            progressReporter.OutputMessage(e.StackTrace);
                                        }

                                        // Update progress reporter
                                        progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                    } // End loop through all gridblocks in the Fracture Grid

                        } // End write stiffness and compliance tensor data

                    } // End loop through each stage in the fracture growth

                    // Write the implicit properties to a series of GRDECL output files, one for each stage in the fracture growth
                    if (WriteGRDECLFiles)
                        for (int stageNumber = 1; stageNumber <= NoStages; stageNumber++)
                        {
                            SourceDataGrid.WriteGRDECLFile(ModelName, outputFolderPath, progressReporter, stageNumber, ShadowGrid_TopLayerK, ShadowGrid_BottomLayerK, IncludeGridGeometryInGRDECLFiles, PopulateEmptyGridblocks);
                        }

                    // Write the explicit DFNs to a series of FAB output files, one for each stage in the fracture growth
                    if (WriteFABFiles)
                        SourceDataGrid.WriteFABFiles(ModelName, outputFolderPath, ModelGrid, progressReporter, (MinExplicitMicrofractureRadius > 0), true, true);
                }
            }

#if !READINPUTFROMFILE
            // If running from hardcoded data, require a user key press before terminating and closing the console window
            Console.WriteLine("Model run complete; press any key to continue");
            Console.ReadKey();
#endif
        }
    }
}
