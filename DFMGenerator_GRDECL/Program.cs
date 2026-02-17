// Switch this flag off to use hardcoded values for all parameters
// This should be done for debugging only
// The flag should be set to generate release versions of the standalone code
//#define READINPUTFROMFILE
// Set these flags to output detailed information on input parameters and properties for each gridblock
// Use for debugging only; will significantly increase runtime
#define DEBUG_FRAC_INPUT
#define DEBUG_FRAC_OUTPUT

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
                inputfile_name = "DFMGenerator_configuration.txt";
            if (!File.Exists(inputfile_name))
            {
                StreamWriter input_file = new StreamWriter(inputfile_name);

                input_file.WriteLine(string.Format("% Model {0}\n", inputfile_name));
                input_file.WriteLine("% Replace defaults with required values");
                input_file.WriteLine("% Use % for comment lines");
                input_file.WriteLine();

                input_file.WriteLine("% Main properties");
                input_file.WriteLine("% Geometry and grid property data");
                input_file.WriteLine("% This should be a GRDECL format file");
                input_file.WriteLine("GridFileName Gridfile.GRDECL");
                input_file.WriteLine("% Set time units to ma, year or second");
                input_file.WriteLine("ModelTimeUnits ma");
                input_file.WriteLine("% Deformation load");
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
                input_file.WriteLine("% Density of initial microfractures");
                input_file.WriteLine("DefaultInitialMicrofractureDensity 0.001");
                input_file.WriteLine("%InitialMicrofractureDensityProperty PROPERTYNAME");
                input_file.WriteLine("% Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures");
                input_file.WriteLine("DefaultInitialMicrofractureSizeDistribution 3");
                input_file.WriteLine("%InitialMicrofractureSizeDistributionProperty PROPERTYNAME");
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
                input_file.WriteLine();

                input_file.WriteLine("% Outputs");
                input_file.WriteLine("% Output to file");
                input_file.WriteLine("% These must be set to true for stand-alone version or no output will be generated");
                input_file.WriteLine("WriteImplicitDataFiles true");
                input_file.WriteLine("WriteDFNFiles true");
                input_file.WriteLine("% Output file type for explicit DFN data:");
                input_file.WriteLine("%      - ASCII (Can be loaded into the data analysis spreadsheets supplied with DFM Generator)");
                input_file.WriteLine("%      - FAB (FAB files can be loaded directly into Petrel)");
                input_file.WriteLine("OutputDFNFileType ASCII");
                input_file.WriteLine("% Flag to write implicit fracture data to a GRDECL file");
                input_file.WriteLine("% This can be written to a single file including the grid geometry, or to separate files for the geometry and different property groups");
                input_file.WriteLine("WriteGRDECLFiles false");
                input_file.WriteLine("WriteSeparateGRDECLPropertyFiles true");
                input_file.WriteLine("% Output DFM at intermediate stages of fracture growth");
                input_file.WriteLine("NoIntermediateOutputs 0");
                input_file.WriteLine("% Flag to control interval between output of intermediate stage DFMs:");
                input_file.WriteLine("%      - EqualArea (output at at approximately equal increments of total fracture area)");
                input_file.WriteLine("%      - EqualTime (output at equal intervals of time)");
                input_file.WriteLine("%      - SpecifiedTime (output at the end of each specified deformation episode)");
                input_file.WriteLine("IntermediateOutputIntervalControl EqualArea");
                input_file.WriteLine("% Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints");
                input_file.WriteLine("OutputCentrepoints false");
                input_file.WriteLine("% Flag to calculate and output the bulk rock compliance and stiffness tensors");
                input_file.WriteLine("OutputBulkRockElasticTensors false");
                input_file.WriteLine("% Flag to calculate and output fracture porosity");
                input_file.WriteLine("OutputFracturePorosity true");
                input_file.WriteLine("% Flag to calculate and output fracture permeability tensors");
                input_file.WriteLine("OutputFracturePermeabilityTensor false");
                input_file.WriteLine("% Algorithm to use for calculating fracture permeability");
                input_file.WriteLine("%      - Oda1986 (The Oda 1986 model assumes fractures of infinite size and connectivity)");
                input_file.WriteLine("%      - OdaCorrected1987 (This algorithm adds a correction factor to account for fracture connectivity)");
                input_file.WriteLine("%      - SizeConnectivityCorrected (This takes into account flow between fractures along relay segments, fractures from other sets, or through the host rock; for the latter, host rock permeability must be specified)");
                input_file.WriteLine("PermeabilityAlgorithm Oda1986");
                input_file.WriteLine("% Flag to calculate and output implicit fracture population distribution functions");
                input_file.WriteLine("OutputPopulationDistribution true");
                input_file.WriteLine("% Number of macrofracture length values to calculate for each of the implicit fracture population distribution functions");
                input_file.WriteLine("No_l_indexPoints 20");
                input_file.WriteLine("% MaxHMinLength and MaxHMaxLength control the range of macrofracture lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively");
                input_file.WriteLine("% Set these values to the approximate maximum length of macrofractures generated (in metres), or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length");
                input_file.WriteLine("MaxHMinLength 0");
                input_file.WriteLine("MaxHMaxLength 0");
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

                input_file.WriteLine("% Present day effective stress parameters");
                input_file.WriteLine("% Flag to use present day effective stress tensor, instead of stress at the time of deformation, to calculate fracture aperture and permeability");
                input_file.WriteLine("UsePresentDayStress false");
                input_file.WriteLine("% Present Terzaghi effective day stress tensor components (Pa)");
                input_file.WriteLine("% These will override the stress at the time of deformation when calculating fracture aperture and permeability");
                input_file.WriteLine("% Any undefined components will be set to 0");
                input_file.WriteLine("DefaultPresentDayEffectiveStress_XX 15000000");
                input_file.WriteLine("DefaultPresentDayEffectiveStress_YY 15000000");
                input_file.WriteLine("DefaultPresentDayEffectiveStress_ZZ 25000000");
                input_file.WriteLine("DefaultPresentDayEffectiveStress_XY 0");
                input_file.WriteLine("DefaultPresentDayEffectiveStress_YZ 0");
                input_file.WriteLine("DefaultPresentDayEffectiveStress_ZX 0");
                input_file.WriteLine("%PresentDayEffectiveStress_XXProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayEffectiveStress_YYProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayEffectiveStress_ZZProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayEffectiveStress_XYProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayEffectiveStress_YZProperty PROPERTYNAME");
                input_file.WriteLine("%PresentDayEffectiveStress_ZXProperty PROPERTYNAME");
                input_file.WriteLine();

                input_file.WriteLine("% Calculation control parameters");
                input_file.WriteLine("% Number of fracture sets");
                input_file.WriteLine("% Set to 1 to generate a single fracture set, perpendicular to ehmin");
                input_file.WriteLine("% Set to 2 to generate two orthogonal fracture sets, perpendicular to the minimum and maximum horizontal strain directions; this is typical of a single stage of tectonic deformation in intact rock");
                input_file.WriteLine("% Set to 6 to model polygonal or strike-slip fractures, or multiple deformation episodes where there are pre-existing fractures oblique to the principal horizontal stresses");
                input_file.WriteLine("NoFractureSets 2");
                input_file.WriteLine("% Fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal");
                input_file.WriteLine("Mode1Only false");
                input_file.WriteLine("Mode2Only false");
                input_file.WriteLine("% Position of fracture nucleation within the layer; set to 0 to force all fractures to nucleate at the base of the layer and 1 to force all fractures to nucleate at the top of the layer; set to -1 to nucleate fractures at random locations within the layer");
                input_file.WriteLine("FractureNucleationPosition -1");
                input_file.WriteLine("% Flag to check microfractures against stress shadows of all macrofractures, regardless of set: can be set to None, All or Automatic");
                input_file.WriteLine("% Flag to control whether to search adjacent gridblocks for stress shadow interaction: can be set to All, None or Automatic; if set to Automatic, this will be determined independently for each gridblock based on the gridblock geometry");
                input_file.WriteLine("% If None, microfractures will only be deactivated if they lie in the stress shadow zone of parallel macrofractures");
                input_file.WriteLine("% If All, microfractures will also be deactivated if they lie in the stress shadow zone of oblique or perpendicular macrofractures, depending on the strain tensor");
                input_file.WriteLine("% If Automatic, microfractures in the stress shadow zone of oblique or perpendicular macrofractures will be deactivated only if there are more than two fracture sets");
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
                input_file.WriteLine("% Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small");
                input_file.WriteLine("% Set to 0 to disable probabilistic fracture nucleation");
                input_file.WriteLine("% Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)");
                input_file.WriteLine("ProbabilisticFractureNucleationLimit -1");
                input_file.WriteLine("% Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set");
                input_file.WriteLine("% Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time");
                input_file.WriteLine("PropagateFracturesInNucleationOrder true");
                input_file.WriteLine("% Flag to control whether to search adjacent gridblocks for stress shadow interaction: can be set to All, None or Automatic; if set to Automatic, this will be determined independently for each gridblock based on the gridblock geometry");
                input_file.WriteLine("SearchNeighbouringGridblocks Automatic");
                input_file.WriteLine("% Minimum radius for microfractures to be included in explicit DFN (in metres)");
                input_file.WriteLine("% Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN");
                input_file.WriteLine("MinExplicitMicrofractureRadius 0");
                input_file.WriteLine("% Number of cornerpoints defining the microfracture polygons in the explicit DFN");
                input_file.WriteLine("% Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints");
                input_file.WriteLine("Number_uF_Points 8");
                input_file.WriteLine();

                input_file.Close();

                Console.WriteLine("\nDFMGenerator configuration file did not exist. An empty file has been created. Please enter the required values, SAVE it and press ENTER! ");
                Console.ReadKey();
            }

            // Input files are assumed to be in the current folder
            string inputFolderPath = "";
            // Get path for output files
            string outputFolderPath = "";
            if (args.Length > 0)
            {
                outputFolderPath = inputfile_name.Replace(".txt", "") + "_output" + @"\";
                // If the output folder does not exist, create it
                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);
            }
#else
            // Get path for input and output files
            string fullHomePath = "";
            string outputFolderPath = "";

            try
            {
                var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
                if (homeDrive != null)
                {
                    var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
                    if (homePath != null)
                    {
                        fullHomePath = homeDrive + Path.DirectorySeparatorChar + homePath;
                        outputFolderPath = Path.Combine(fullHomePath, "DFMFolder");
                        outputFolderPath = outputFolderPath + @"\";
                        // If the output folder does not exist, create it
                        if (!Directory.Exists(outputFolderPath))
                            Directory.CreateDirectory(outputFolderPath);
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown: " + e.Message);
                return;
            }
            string inputFolderPath = outputFolderPath;
#endif

            // Set hardcoded default values for all parameters

            // Main properties
            // Grid geometry will be read from a GRDECL format file, along with grid properties
            // The filename should be specified here, including the extension but excluding the filepath
            // Parameters whose value can vary in different gridblocks are defined in two ways: by a default value and a property name
            // If the property name is specified and can be found in the GRDECL file, the value for each gridblock will be read from the GRDECL file
            // If the specified property in the GRDECL file has a null value for any individual gridblock, 
            // the parameter will be set to the specified default value in that gridblock
            // If no property name is specified (the property name string is empty), or no property with the specified name can be found in the GRDECL file,
            // the parameter will be set to the specified default value in every gridblock
            string GridFileName = "Gridfile.GRDECL";
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
            // Duration of the deformation episode; set to -1 to continue until fracture saturation is reached
            double DeformationEpisodeDuration = -1;
            // Rate of change of absolute stress tenor; set this to define the deformation load in terms of stress rather than strain
            // This will override the strain, fluid overpressure, thermal and uplift loads
            // If null (undefined), the strain strain, fluid overpressure, thermal and uplift loads defined previously will be used to calculate the rate of change of the in situ stress tensor
            Tensor2S AbsoluteStressRate = null;
            // Set initial fluid pressure and absolute stress tensor to specify the in situ effective stress state at the start of the deformation episode
            // If these are NaN or null, the initial effective stress will be determined by the weight of the overburden and initial stress relaxation, or the stress state at the end of the previous timestep
            double InitialFluidPressure = double.NaN;
            Tensor2S InitialAbsoluteStress = null;

            // Global deformation load parameter lists
            // These contain one entry for each deformation episode, in order
            // They will be copied to all gridblocks
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
            List<Tensor2S> AbsoluteStressRate_list = new List<Tensor2S>();
            List<double> InitialFluidPressure_list = new List<double>();
            List<Tensor2S> InitialAbsoluteStress_list = new List<Tensor2S>();
#if !READINPUTFROMFILE
            // Add a deformation episode with default values
            DefaultEhminAzi_list.Add(DefaultEhminAzi);
            EhminAziPropertyName_list.Add(EhminAziPropertyName);
            DefaultEhminRate_list.Add(-0.001);
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
            AbsoluteStressRate_list.Add(AbsoluteStressRate);
            InitialFluidPressure_list.Add(InitialFluidPressure);
            InitialAbsoluteStress_list.Add(InitialAbsoluteStress);
            /*// Add a deformation episode with a defined stress load
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
            ModelTimeUnits = TimeUnits.second;
            DeformationEpisodeDuration_list.Add(31622400); // One year in seconds
            AbsoluteStressRate_list.Add(new Tensor2S(0.02, -0.02, -0.3, 0.02, -0.04, 0.04));
            InitialFluidPressure_list.Add(15000000);
            InitialAbsoluteStress_list.Add(new Tensor2S(40000000, 40000000, 60000000, -500000, 1000000, -1000000));
            BiazimuthalConjugate = false;*/
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
            // Density of initial microfractures
            double DefaultInitialMicrofractureDensity = 0.001;
            string InitialMicrofractureDensityPropertyName = string.Empty;
            // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
            double DefaultInitialMicrofractureSizeDistribution = 3;
            string InitialMicrofractureSizeDistributionPropertyName = string.Empty;
            // Subritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
            double DefaultSubcriticalPropIndex = 10;
            string SubcriticalPropIndexPropertyName = string.Empty;
            double CriticalPropagationRate = 2000;
            // Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity
            double DefaultHostRock_kh = 9.869233e-16;// 1mD in m2
            string HostRock_khPropertyName = string.Empty;
            double DefaultHostRock_kv = 9.869233e-16;// 1mD in m2
            string HostRock_kvPropertyName = string.Empty;

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

            // Outputs
            // Output to file
            // These must be set to true for stand-alone version or no output will be generated
            bool WriteImplicitDataFiles = true;
            bool WriteDFNFiles = true;
            // Output file type for explicit DFN data: ASCII or FAB (NB FAB files can be loaded directly into Petrel)
            DFNFileType OutputDFNFileType = DFNFileType.ASCII;
            // Flag to write implicit fracture data to a GRDECL file
            // This can be written to a single file including the grid geometry, or to separate files for the geometry and different property groups
            bool WriteGRDECLFiles = false;
            bool WriteSeparateGRDECLPropertyFiles = true;
            // Output DFM at intermediate stages of fracture growth
            int NoIntermediateOutputs = 0;
            // Flag to control interval between output of intermediate stage DFMs:
            // - EqualArea (output at at approximately euqal increments of total fracture area)
            // - EqualTime (output at equal intervals of time)
            // - SpecifiedTime (output at the end of each specified deformation episode)
            IntermediateOutputInterval IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualArea;
            // Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints
            bool OutputCentrepoints = false;
            // Flag to calculate and output the bulk rock compliance and stiffness tensors
            bool OutputBulkRockElasticTensors = false;
            // Flag to calculate and output fracture porosity
            bool OutputFracturePorosity = true;
            // Flag to calculate and output fracture permeability tensors
            bool OutputFracturePermeabilityTensor = true;
            // Algorithm to use for calculating fracture permeability
            PermeabilityCalculationAlgorithm PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.SizeConnectivityCorrected;
            // Flag to calculate and output implicit fracture population distribution functions
            bool OutputPopulationDistribution = true;
            // Number of macrofracture length values to calculate for each of the implicit fracture population distribution functions
            int No_l_indexPoints = 20;
            // MaxHMinLength and MaxHMaxLength control the range of macrofracture lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
            // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length
            double MaxHMinLength = 0;
            double MaxHMaxLength = 0;

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
            // Flag to use present day effective stress tensor, instead of stress at the time of deformation, to calculate fracture aperture and permeability
            bool UsePresentDayStress = false;
            // Present Terzaghi effective day stress tensor - define this to override stress at the time of deformation when calculating fracture aperture and permeability
            double DefaultPresentDayEffectiveStress_XX = 0;
            double DefaultPresentDayEffectiveStress_YY = 0;
            double DefaultPresentDayEffectiveStress_ZZ = 0;
            double DefaultPresentDayEffectiveStress_XY = 0;
            double DefaultPresentDayEffectiveStress_YZ = 0;
            double DefaultPresentDayEffectiveStress_ZX = 0;
            string PresentDayEffectiveStress_XXPropertyName = string.Empty;
            string PresentDayEffectiveStress_YYPropertyName = string.Empty;
            string PresentDayEffectiveStress_ZZPropertyName = string.Empty;
            string PresentDayEffectiveStress_XYPropertyName = string.Empty;
            string PresentDayEffectiveStress_YZPropertyName = string.Empty;
            string PresentDayEffectiveStress_ZXPropertyName = string.Empty;

            // Calculation control parameters
            // Number of fracture sets
            // Set to 1 to generate a single fracture set, perpendicular to ehmin
            // Set to 2 to generate two orthogonal fracture sets, perpendicular to ehmin and ehmax; this is typical of a single stage of deformation in intact rock
            // Set to 6 or more to generate oblique fractures; this is typical of multiple stages of deformation with fracture reactivation, or transtensional strain
            int NoFractureSets = 2;
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
            // Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment
            bool LinkStressShadows = false;
            // Maximum variation in fracture propagation azimuth allowed across gridblock boundary; if the orientation of the fracture set varies across the gridblock boundary by more than this, the algorithm will seek a better matching set 
            // Set to Pi/4 rad (45 degrees) by default
            double MaxConsistencyAngle = Math.PI / 4;
            // Layer thickness cutoff: explicit DFN will not be calculated for gridblocks thinner than this value
            // Set this to prevent the generation of excessive numbers of fractures in very thin gridblocks where there is geometric pinch-out of the layers
            double MinimumLayerThickness = 0;
            // Maximum number of new fractures that can be generated per gridblock per timestep: set automatically to 100,000
            // Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason
            int MaximumNewFracturesPerTimestep = 100000;
            // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small
            // Set to 0 to disable probabilistic fracture nucleation
            // Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)
            double ProbabilisticFractureNucleationLimit = -1;
            // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
            // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
            bool PropagateFracturesInNucleationOrder = true;
            // Flag to control whether to search adjacent gridblocks for stress shadow interaction; if set to automatic, this will be determined independently for each gridblock based on the gridblock geometry
            AutomaticFlag SearchNeighbouringGridblocks = AutomaticFlag.Automatic;
            // Minimum radius for microfractures to be included in explicit DFN
            // Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN
            double MinExplicitMicrofractureRadius = 0;
            // Number of cornerpoints defining the microfracture polygons in the explicit DFN
            // Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints
            int Number_uF_Points = 8;
            // Not yet implemented - keep this at 0
            double MinDFNMacrofractureLength = 0;

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
                        // Grid size
                        case "GridFile":
                            GridFileName = line_split[1];
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
                        // Strain orientatation
                        case "DefaultEhminAzi":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    DefaultEhminAzi_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                                    DefaultEhminRate_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                                    DefaultEhmaxRate_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                                    DefaultAppliedOverpressureRate_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                                    DefaultAppliedTemperatureChange_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                                    DefaultAppliedUpliftRate_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                                    StressArchingFactor_list.Add(Convert.ToDouble(line_split[valueNo]));
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

                        // Outputs
                        // Output to file
                        // LogCalculation and WriteDFNFiles must be set to true or no output will be generated
                        case "WriteImplicitDataFiles":
                            WriteImplicitDataFiles = true;
                            break;
                        case "WriteDFNFiles":
                            WriteDFNFiles = true;
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
                        // Flag to write implicit fracture data to a GRDECL file
                        // This can be written to a single file including the grid geometry, or to separate files for the geometry and different property groups
                        case "WriteGRDECLFiles":
                            WriteGRDECLFiles = true;
                            break;
                        case "WriteSeparateGRDECLPropertyFiles":
                            WriteSeparateGRDECLPropertyFiles = true;
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
                        // Flag to output the macrofracture centrepoints as a polyline, in addition to the macrofracture cornerpoints
                        case "OutputCentrepoints":
                            OutputCentrepoints = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output the bulk rock compliance and stiffness tensors
                        case "OutputBulkRockElasticTensors":
                            OutputBulkRockElasticTensors = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture porosity
                        case "OutputFracturePorosity":
                            OutputFracturePorosity = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture permeability tensors
                        case "OutputFracturePermeabilityTensor":
                            OutputFracturePermeabilityTensor = (line_split[1] == "true");
                            break;
                        // Algorithm to use for calculating fracture permeability
                        case "PermeabilityAlgorithm":
                            if (line_split[1] == "Oda1986")
                                PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.Oda1986;
                            else if (line_split[1] == "OdaCorrected1987")
                                PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.OdaCorrected1987;
                            else
                                PermeabilityAlgorithm = PermeabilityCalculationAlgorithm.SizeConnectivityCorrected;
                            break;
                        // Flag to calculate and output implicit fracture population distribution functions
                        case "OutputPopulationDistribution":
                            OutputPopulationDistribution = (line_split[1] == "true");
                            break;
                        // Number of macrofracture length values to calculate for each of the implicit fracture population distribution functions
                        case "No_l_indexPoints":
                            No_l_indexPoints = Convert.ToInt32(line_split[1]);
                            break;
                        // MaxHMinLength and MaxHMaxLength control the range of macrofracture lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
                        // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length
                        case "MaxHMinLength":
                            MaxHMinLength = Convert.ToDouble(line_split[1]);
                            break;
                        case "MaxHMaxLength":
                            MaxHMaxLength = Convert.ToDouble(line_split[1]);
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
                        // Flag to use present day effective stress tensor, instead of stress at the time of deformation, to calculate fracture aperture and permeability
                        case "UsePresentDayStress":
                            UsePresentDayStress = (line_split[1] == "true");
                            break;
                        // Present Terzaghi effective day stress tensor components (Pa)
                        // These will override the stress at the time of deformation when calculating fracture aperture and permeability
                        // Any undefined components will be set to 0
                        case "DefaultPresentDayEffectiveStress_XX":
                            DefaultPresentDayEffectiveStress_XX = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayEffectiveStress_YY":
                            DefaultPresentDayEffectiveStress_YY = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayEffectiveStress_ZZ":
                            DefaultPresentDayEffectiveStress_ZZ = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayEffectiveStress_XY":
                            DefaultPresentDayEffectiveStress_XY = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayEffectiveStress_YZ":
                            DefaultPresentDayEffectiveStress_YZ = Convert.ToDouble(line_split[1]);
                            break;
                        case "DefaultPresentDayEffectiveStress_ZX":
                            DefaultPresentDayEffectiveStress_ZX = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayEffectiveStress_XXProperty":
                            PresentDayEffectiveStress_XXPropertyName = line_split[1];
                            break;
                        case "PresentDayEffectiveStress_YYProperty":
                            PresentDayEffectiveStress_YYPropertyName = line_split[1];
                            break;
                        case "PresentDayEffectiveStress_ZZProperty":
                            PresentDayEffectiveStress_ZZPropertyName = line_split[1];
                            break;
                        case "PresentDayEffectiveStress_XYProperty":
                            PresentDayEffectiveStress_XYPropertyName = line_split[1];
                            break;
                        case "PresentDayEffectiveStress_YZProperty":
                            PresentDayEffectiveStress_YZPropertyName = line_split[1];
                            break;
                        case "PresentDayEffectiveStress_ZXProperty":
                            PresentDayEffectiveStress_ZXPropertyName = line_split[1];
                            break;

                        // Calculation control parameters
                        // Number of fracture sets
                        // Set to 1 to generate a single fracture set, perpendicular to ehmin
                        // Set to 2 to generate two orthogonal fracture sets, perpendicular to ehmin and ehmax; this is typical of a single stage of deformation in intact rock
                        // Set to 6 or more to generate oblique fractures; this is typical of multiple stages of deformation with fracture reactivation, or transtensional strain
                        case "NoFractureSets":
                            NoFractureSets = Convert.ToInt32(line_split[1]);
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
                        case "SearchNeighbouringGridblocks":
                            {
                                if (line_split[1] == "All")
                                    SearchNeighbouringGridblocks = AutomaticFlag.All;
                                else if (line_split[1] == "None")
                                    SearchNeighbouringGridblocks = AutomaticFlag.None;
                                else if (line_split[1] == "Automatic")
                                    SearchNeighbouringGridblocks = AutomaticFlag.Automatic;
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
                            MinDFNMacrofractureLength = 0;
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

            // Create a shadow grid and use the specified GRDECL file to populate it
            // If this operation fails then display an error message and abort the run
            ShadowGrid GRDECLGrid = new ShadowGrid(GridFileName, inputFolderPath, GridFileType.GRDECL);
            if (GRDECLGrid.ErrorStatus != ShadowGridErrorStatus.DataLoadedOK)
            {
                Console.WriteLine(string.Format("Error reading grid and property data from {0}: {1}", inputFolderPath + GridFileName, GRDECLGrid.ErrorStatus));
                Console.WriteLine("Model run will be aborted without generating output");
                return;
            }

            // Create a DFM Generator grid and populate it with default values and property data from the shadow grid

            // Create a console progress reporter object to write progress updates to Console
            ConsoleProgressReporter progressReporter = new ConsoleProgressReporter(10, true);

            // Create fracture grid
            progressReporter.OutputMessage("Start building grid");
            int NoFractureGridRows = NoPetrelGridRows / HorizontalUpscalingFactor;
            int NoFractureGridCols = NoPetrelGridCols / HorizontalUpscalingFactor;
            FractureGrid ModelGrid = new FractureGrid(NoFractureGridRows, NoFractureGridCols);

            // Populate fracture grid
            // Loop through all columns and rows in the Fracture Grid
            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
            progressReporter.SetNumberOfElements(NoFractureGridCols * NoFractureGridRows);
            for (int FractureGrid_ColNo = 0; FractureGrid_ColNo < NoFractureGridCols; FractureGrid_ColNo++)
            {
                for (int FractureGrid_RowNo = 0; FractureGrid_RowNo < NoFractureGridRows; FractureGrid_RowNo++)
                {
#if DEBUG_FRAC_INPUT
                                PetrelLogger.InfoOutputWindow("");
                                PetrelLogger.InfoOutputWindow(string.Format("FractureGrid gridblock Col(I) {0}, Row(J) {1}", FractureGrid_ColNo, FractureGrid_RowNo));
#endif

                    // Check if calculation has been aborted
                    if (progressReporter.abortCalculation())
                    {
                        // Clean up any resources or data
                        break;
                    }

                    // Create indices for the all the Petrel grid cells corresponding to the fracture gridblock 
                    int PetrelGrid_FirstCellI = PetrelGrid_StartCellI + (FractureGrid_ColNo * HorizontalUpscalingFactor);
                    int PetrelGrid_FirstCellJ = PetrelGrid_StartCellJ + (FractureGrid_RowNo * HorizontalUpscalingFactor);
                    int PetrelGrid_LastCellI = PetrelGrid_FirstCellI + (HorizontalUpscalingFactor - 1);
                    int PetrelGrid_LastCellJ = PetrelGrid_FirstCellJ + (HorizontalUpscalingFactor - 1);
#if DEBUG_FRAC_INPUT
                                PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid_FirstCellI {0}, PetrelGrid_FirstCellJ {1}, PetrelGrid_TopCellK {2}", PetrelGrid_FirstCellI, PetrelGrid_FirstCellJ, PetrelGrid_TopCellK));
                                PetrelLogger.InfoOutputWindow(string.Format("PetrelGrid_LastCellI {0}, PetrelGrid_LastCellJ {1}, PetrelGrid_BaseCellK {2}", PetrelGrid_LastCellI, PetrelGrid_LastCellJ, PetrelGrid_BaseCellK));
#endif

                    // Initialise variables for mean depth and thickness
                    // If the top of the grid is above MSL we will also take this into account when calculating depth
                    // However if it is below MSL we will calculate depth from MSL (Z=0)
                    double local_Current_Depth = 0;
                    double local_LayerThickness = 0;
                    double local_Current_SurfaceHeight = 0;

                    // Find SW cornerpoints; if the top or bottom cells in the SW corner are undefined, use the highest and lowest defined cells
                    Index3 SW_topgrid = new Index3(PetrelGrid_FirstCellI, PetrelGrid_FirstCellJ, 0);
                    Point3 SW_topgrid_corner = PetrelGrid.GetPointAtCell(SW_topgrid, Corner.SouthWest, TopOrBase.Top);
                    // If the top cell in the grid is not defined, find the uppermost cell that is
                    if (Point3.IsNull(SW_topgrid_corner))
                    {
                        // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                        for (int PetrelGrid_DataCellK = 1; PetrelGrid_DataCellK <= PetrelGrid_TopCellK; PetrelGrid_DataCellK++)
                        {
                            SW_topgrid.K = PetrelGrid_DataCellK;
                            SW_topgrid_corner = PetrelGrid.GetPointAtCell(SW_topgrid, Corner.SouthWest, TopOrBase.Top);
                            if (!Point3.IsNull(SW_topgrid_corner))
                                break;
                        }
                    }
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
                    // Create FractureGrid points corresponding to the SW Petrel gridblock corners
                    PointXYZ FractureGrid_SWtop = new PointXYZ(SW_top_corner.X, SW_top_corner.Y, SW_top_corner.Z);
                    PointXYZ FractureGrid_SWbottom = new PointXYZ(SW_bottom_corner.X, SW_bottom_corner.Y, SW_bottom_corner.Z);
                    // Update mean depth and thickness variables
                    local_Current_SurfaceHeight += SW_topgrid_corner.Z;
                    local_Current_Depth -= SW_top_corner.Z;
                    local_LayerThickness += (SW_top_corner.Z - SW_bottom_corner.Z);

                    // Find NW cornerpoints; if the top or bottom cells in the NW corner are undefined, use the highest and lowest defined cells
                    Index3 NW_topgrid = new Index3(PetrelGrid_FirstCellI, PetrelGrid_LastCellJ, 0);
                    Point3 NW_topgrid_corner = PetrelGrid.GetPointAtCell(NW_topgrid, Corner.NorthWest, TopOrBase.Top);
                    // If the top cell is not defined, find the uppermost cell that is
                    if (Point3.IsNull(NW_topgrid_corner))
                    {
                        // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                        for (int PetrelGrid_DataCellK = 1; PetrelGrid_DataCellK <= PetrelGrid_TopCellK; PetrelGrid_DataCellK++)
                        {
                            NW_topgrid.K = PetrelGrid_DataCellK;
                            NW_topgrid_corner = PetrelGrid.GetPointAtCell(NW_topgrid, Corner.NorthWest, TopOrBase.Top);
                            if (!Point3.IsNull(NW_topgrid_corner))
                                break;
                        }
                    }
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
                    // Create FractureGrid points corresponding to the NW Petrel gridblock corners
                    PointXYZ FractureGrid_NWtop = new PointXYZ(NW_top_corner.X, NW_top_corner.Y, NW_top_corner.Z);
                    PointXYZ FractureGrid_NWbottom = new PointXYZ(NW_bottom_corner.X, NW_bottom_corner.Y, NW_bottom_corner.Z);
                    // Update mean depth and thickness variables
                    local_Current_SurfaceHeight += NW_topgrid_corner.Z;
                    local_Current_Depth -= NW_top_corner.Z;
                    local_LayerThickness += (NW_top_corner.Z - NW_bottom_corner.Z);

                    // Find NE cornerpoints; if the top or bottom cells in the NE corner are undefined, use the highest and lowest defined cells
                    Index3 NE_topgrid = new Index3(PetrelGrid_LastCellI, PetrelGrid_LastCellJ, 0);
                    Point3 NE_topgrid_corner = PetrelGrid.GetPointAtCell(NE_topgrid, Corner.NorthEast, TopOrBase.Top);
                    // If the top cell is not defined, find the uppermost cell that is
                    if (Point3.IsNull(NE_topgrid_corner))
                    {
                        // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                        for (int PetrelGrid_DataCellK = 1; PetrelGrid_DataCellK <= PetrelGrid_TopCellK; PetrelGrid_DataCellK++)
                        {
                            NE_topgrid.K = PetrelGrid_DataCellK;
                            NE_topgrid_corner = PetrelGrid.GetPointAtCell(NE_topgrid, Corner.NorthEast, TopOrBase.Top);
                            if (!Point3.IsNull(NE_topgrid_corner))
                                break;
                        }
                    }
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
                    // Create FractureGrid points corresponding to the NE Petrel gridblock corners
                    PointXYZ FractureGrid_NEtop = new PointXYZ(NE_top_corner.X, NE_top_corner.Y, NE_top_corner.Z);
                    PointXYZ FractureGrid_NEbottom = new PointXYZ(NE_bottom_corner.X, NE_bottom_corner.Y, NE_bottom_corner.Z);
                    // Update mean depth and thickness variables
                    local_Current_SurfaceHeight += NE_topgrid_corner.Z;
                    local_Current_Depth -= NE_top_corner.Z;
                    local_LayerThickness += (NE_top_corner.Z - NE_bottom_corner.Z);

                    // Find SE cornerpoints; if the top or bottom cells in the SE corner are undefined, use the highest and lowest defined cells
                    Index3 SE_topgrid = new Index3(PetrelGrid_LastCellI, PetrelGrid_FirstCellJ, 0);
                    Point3 SE_topgrid_corner = PetrelGrid.GetPointAtCell(SE_topgrid, Corner.SouthEast, TopOrBase.Top);
                    // If the top cell is not defined, find the uppermost cell that is
                    if (Point3.IsNull(SE_topgrid_corner))
                    {
                        // Loop through all cells in the stack, from the second to top down, until we find one that contains valid data
                        for (int PetrelGrid_DataCellK = 1; PetrelGrid_DataCellK <= PetrelGrid_TopCellK; PetrelGrid_DataCellK++)
                        {
                            SE_topgrid.K = PetrelGrid_DataCellK;
                            SE_topgrid_corner = PetrelGrid.GetPointAtCell(SE_topgrid, Corner.SouthEast, TopOrBase.Top);
                            if (!Point3.IsNull(SE_topgrid_corner))
                                break;
                        }
                    }
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
                    // Create FractureGrid points corresponding to the SE Petrel gridblock corners
                    PointXYZ FractureGrid_SEtop = new PointXYZ(SE_top_corner.X, SE_top_corner.Y, SE_top_corner.Z);
                    PointXYZ FractureGrid_SEbottom = new PointXYZ(SE_bottom_corner.X, SE_bottom_corner.Y, SE_bottom_corner.Z);
                    // Update mean depth and thickness variables
                    local_Current_SurfaceHeight += SE_topgrid_corner.Z;
                    local_Current_Depth -= SE_top_corner.Z;
                    local_LayerThickness += (SE_top_corner.Z - SE_bottom_corner.Z);

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
                    // This will depend on whether we are averaging the mechanical properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                    // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                    double local_InitialMicrofractureDensity = InitialMicrofractureDensity;
                    double local_InitialMicrofractureSizeDistribution = InitialMicrofractureSizeDistribution;
                    double local_SubcriticalPropIndex = SubcriticalPropIndex;
                    double local_YoungsMod = YoungsMod;
                    double local_PoissonsRatio = PoissonsRatio;
                    double local_Porosity = Porosity;
                    double local_BiotCoefficient = BiotCoefficient;
                    double local_ThermalExpansionCoefficient = ThermalExpansionCoefficient;
                    double local_CrackSurfaceEnergy = CrackSurfaceEnergy;
                    double local_FrictionCoefficient = FrictionCoefficient;
                    double local_RockStrainRelaxation = RockStrainRelaxation;
                    double local_FractureRelaxation = FractureRelaxation;
                    double local_HostRock_kh = HostRock_kh;
                    double local_HostRock_kv = HostRock_kv;

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
                        double Porosity_total = 0;
                        int Porosity_novalues = 0;
                        double BiotCoeff_total = 0;
                        int BiotCoeff_novalues = 0;
                        double ThermalExpansionCoefficient_total = 0;
                        int ThermalExpansionCoefficient_novalues = 0;
                        double CrackSurfaceEnergy_total = 0;
                        int CrackSurfaceEnergy_novalues = 0;
                        double FrictionCoeff_total = 0;
                        int FrictionCoeff_novalues = 0;
                        double RockStrainRelaxation_total = 0;
                        int RockStrainRelaxation_novalues = 0;
                        double FractureRelaxation_total = 0;
                        int FractureRelaxation_novalues = 0;
                        double HostRock_kh_total = 0;
                        int HostRock_kh_novalues = 0;
                        double HostRock_kv_total = 0;
                        int HostRock_kv_novalues = 0;

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
                                    if (UseGridPropertyFor_YoungsMod || UseGridFor_YoungsMod)
                                    {
                                        double cell_YoungsMod = UseGridPropertyFor_YoungsMod ? (double)YoungsMod_gridProperty[cellRef] : (double)YoungsMod_grid[cellRef];
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
                                    if (UseGridPropertyFor_PoissonsRatio || UseGridFor_PoissonsRatio)
                                    {
                                        double cell_PoissonsRatio = UseGridPropertyFor_PoissonsRatio ? (double)PoissonsRatio_gridProperty[cellRef] : (double)PoissonsRatio_grid[cellRef];
                                        if (!double.IsNaN(cell_PoissonsRatio))
                                        {
                                            PoissonsRatio_total += cell_PoissonsRatio;
                                            PoissonsRatio_novalues++;
                                        }
                                    }

                                    // Update porosity total if defined
                                    if (UseGridPropertyFor_Porosity || UseGridFor_Porosity)
                                    {
                                        double cell_Porosity = UseGridPropertyFor_Porosity ? (double)Porosity_gridProperty[cellRef] : (double)Porosity_grid[cellRef];
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        if (convertFromGeneral_Porosity)
                                            cell_Porosity = toSIPorosityUnits.Convert(cell_Porosity);
                                        if (!double.IsNaN(cell_Porosity))
                                        {
                                            Porosity_total += cell_Porosity;
                                            Porosity_novalues++;
                                        }
                                    }

                                    // Update Biot coefficient total if defined
                                    if (UseGridPropertyFor_BiotCoefficient || UseGridFor_BiotCoefficient)
                                    {
                                        double cell_BiotCoeff = UseGridPropertyFor_BiotCoefficient ? (double)BiotCoefficient_gridProperty[cellRef] : (double)BiotCoefficient_grid[cellRef];
                                        if (!double.IsNaN(cell_BiotCoeff))
                                        {
                                            BiotCoeff_total += cell_BiotCoeff;
                                            BiotCoeff_novalues++;
                                        }
                                    }

                                    // Update thermal expansion coefficient total if defined
                                    if (UseGridPropertyFor_ThermalExpansionCoefficient || UseGridFor_ThermalExpansionCoefficient)
                                    {
                                        double cell_ThermalExpansionCoefficient = UseGridPropertyFor_ThermalExpansionCoefficient ? (double)ThermalExpansionCoefficient_gridProperty[cellRef] : (double)ThermalExpansionCoefficient_grid[cellRef];
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        if (convertFromGeneral_ThermalExpansionCoefficient)
                                            cell_ThermalExpansionCoefficient = toSIThermalExpansionCoefficientUnits.Convert(cell_ThermalExpansionCoefficient);
                                        if (!double.IsNaN(cell_ThermalExpansionCoefficient))
                                        {
                                            ThermalExpansionCoefficient_total += cell_ThermalExpansionCoefficient;
                                            ThermalExpansionCoefficient_novalues++;
                                        }
                                    }

                                    // Update crack surface energy total if defined
                                    if (UseGridPropertyFor_CrackSurfaceEnergy || UseGridFor_CrackSurfaceEnergy)
                                    {
                                        double cell_CrackSurfaceEnergy = UseGridPropertyFor_CrackSurfaceEnergy ? (double)CrackSurfaceEnergy_gridProperty[cellRef] : (double)CrackSurfaceEnergy_grid[cellRef];
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
                                    if (UseGridPropertyFor_FrictionCoefficient || UseGridFor_FrictionCoefficient)
                                    {
                                        double cell_FrictionCoeff = UseGridPropertyFor_FrictionCoefficient ? (double)FrictionCoefficient_gridProperty[cellRef] : (double)FrictionCoefficient_grid[cellRef];
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

                                    // Update host rock horizontal permeability total if defined
                                    if (UseGridFor_HostRock_kh)
                                    {
                                        double cell_HostRock_kh = (double)HostRock_kh_grid[cellRef];
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        if (convertFromGeneral_HostRock_kh)
                                            cell_HostRock_kh = toSIPermeabilityUnits.Convert(cell_HostRock_kh);
                                        if (!double.IsNaN(cell_HostRock_kh))
                                        {
                                            HostRock_kh_total += cell_HostRock_kh;
                                            HostRock_kh_novalues++;
                                        }
                                    }

                                    // Update host rock vertical permeability total if defined
                                    if (UseGridFor_HostRock_kv)
                                    {
                                        double cell_HostRock_kv = (double)HostRock_kv_grid[cellRef];
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        if (convertFromGeneral_HostRock_kv)
                                            cell_HostRock_kv = toSIPermeabilityUnits.Convert(cell_HostRock_kv);
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
                        if (SubcriticalPropIndex_novalues > 0)
                            local_SubcriticalPropIndex = SubcriticalPropIndex_total / (double)SubcriticalPropIndex_novalues;
                        if (YoungsMod_novalues > 0)
                            local_YoungsMod = YoungsMod_total / (double)YoungsMod_novalues;
                        if (PoissonsRatio_novalues > 0)
                            local_PoissonsRatio = PoissonsRatio_total / (double)PoissonsRatio_novalues;
                        if (Porosity_novalues > 0)
                            local_Porosity = Porosity_total / (double)Porosity_novalues;
                        if (BiotCoeff_novalues > 0)
                            local_BiotCoefficient = BiotCoeff_total / (double)BiotCoeff_novalues;
                        if (ThermalExpansionCoefficient_novalues > 0)
                            local_ThermalExpansionCoefficient = ThermalExpansionCoefficient_total / (double)ThermalExpansionCoefficient_novalues;
                        if (CrackSurfaceEnergy_novalues > 0)
                            local_CrackSurfaceEnergy = CrackSurfaceEnergy_total / (double)CrackSurfaceEnergy_novalues;
                        if (FrictionCoeff_novalues > 0)
                            local_FrictionCoefficient = FrictionCoeff_total / (double)FrictionCoeff_novalues;
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
                        if (UseGridPropertyFor_YoungsMod || UseGridFor_YoungsMod)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_YoungsMod = UseGridPropertyFor_YoungsMod ? (double)YoungsMod_gridProperty[cellRef] : (double)YoungsMod_grid[cellRef];
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
                        if (UseGridPropertyFor_PoissonsRatio || UseGridFor_PoissonsRatio)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_PoissonsRatio = UseGridPropertyFor_PoissonsRatio ? (double)PoissonsRatio_gridProperty[cellRef] : (double)PoissonsRatio_grid[cellRef];
                                if (!double.IsNaN(cell_PoissonsRatio))
                                {
                                    local_PoissonsRatio = cell_PoissonsRatio;
                                    break;
                                }
                            }
                        }

                        // Update porosity total if defined
                        if (UseGridPropertyFor_Porosity || UseGridFor_Porosity)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_Porosity = UseGridPropertyFor_Porosity ? (double)Porosity_gridProperty[cellRef] : (double)Porosity_grid[cellRef];
                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                if (convertFromGeneral_Porosity)
                                    cell_Porosity = toSIPorosityUnits.Convert(cell_Porosity);
                                if (!double.IsNaN(cell_Porosity))
                                {
                                    local_Porosity = cell_Porosity;
                                    break;
                                }
                            }
                        }

                        // Update Biot coefficient total if defined
                        if (UseGridPropertyFor_BiotCoefficient || UseGridFor_BiotCoefficient)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_BiotCoeff = UseGridPropertyFor_BiotCoefficient ? (double)BiotCoefficient_gridProperty[cellRef] : (double)BiotCoefficient_grid[cellRef];
                                if (!double.IsNaN(cell_BiotCoeff))
                                {
                                    local_BiotCoefficient = cell_BiotCoeff;
                                    break;
                                }
                            }
                        }

                        // Update thermal expansion coefficient total if defined
                        if (UseGridPropertyFor_ThermalExpansionCoefficient || UseGridFor_ThermalExpansionCoefficient)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_ThermalExpansionCoefficient = UseGridPropertyFor_ThermalExpansionCoefficient ? (double)ThermalExpansionCoefficient_gridProperty[cellRef] : (double)ThermalExpansionCoefficient_grid[cellRef];
                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                if (convertFromGeneral_ThermalExpansionCoefficient)
                                    cell_ThermalExpansionCoefficient = toSIThermalExpansionCoefficientUnits.Convert(cell_ThermalExpansionCoefficient);
                                if (!double.IsNaN(cell_ThermalExpansionCoefficient))
                                {
                                    local_ThermalExpansionCoefficient = cell_ThermalExpansionCoefficient;
                                    break;
                                }
                            }
                        }

                        // Update crack surface energy total if defined
                        if (UseGridPropertyFor_CrackSurfaceEnergy || UseGridFor_CrackSurfaceEnergy)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_CrackSurfaceEnergy = UseGridPropertyFor_CrackSurfaceEnergy ? (double)CrackSurfaceEnergy_gridProperty[cellRef] : (double)CrackSurfaceEnergy_grid[cellRef];
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
                        if (UseGridPropertyFor_FrictionCoefficient || UseGridFor_FrictionCoefficient)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_FrictionCoeff = UseGridPropertyFor_FrictionCoefficient ? (double)FrictionCoefficient_gridProperty[cellRef] : (double)FrictionCoefficient_grid[cellRef];
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

                        // Update host rock horizontal permeability total if defined
                        if (UseGridFor_HostRock_kh)
                        {
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_HostRock_kh = (double)HostRock_kh_grid[cellRef];
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
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_HostRock_kv = (double)HostRock_kv_grid[cellRef];
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

                    // Get the deformation load data for each deformation episode
                    // Also calculate the total uplift - this will be needed to calculate the depth at the time of deformation
                    List<Tensor2S> local_EhRate_list = new List<Tensor2S>();
                    List<double> local_AppliedOverpressureRate_list = new List<double>();
                    List<double> local_AppliedTemperatureChange_list = new List<double>();
                    List<double> local_AppliedUpliftRate_list = new List<double>();
                    List<double> local_StressArchingFactor_list = new List<double>();
                    List<double> local_DeformationEpisodeDuration_list = new List<double>();
                    List<StressStateDefinition> local_StressStateDefinition_list = new List<StressStateDefinition>();
                    List<double> local_InitialVerticalStress_list = new List<double>();
                    List<Tensor2S> local_StressRateTensor_list = new List<Tensor2S>();
                    List<Tensor2S> local_InitialStressTensor_list = new List<Tensor2S>();
                    List<double> local_InitialFluidPressure_list = new List<double>();

                    // Create variables for the initial and final dynamic load values outside the sub episodes, so the final value for each episode can be used as the initial value for the subsequent episode 
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
                    for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDefinedDeformationEpisodes; deformationEpisodeNo++)
                    {
                        // Get the time converter for this episode
                        // This can vary between episodes as the time units may be different for each deformation episode
                        // NB Times in project units must be divided by the converter to convert to SI units (s)
                        // NB Load rates in project units must be multiplied by the converter to convert to SI units (/s)
                        double TimeUnitConverter = TimeUnitConverter_list[deformationEpisodeNo];

                        // Get the static deformation load data from the grid as required
                        // This will depend on whether we are averaging the stress/strain over all Petrel cells that make up the gridblock, or taking the values from a single cell
                        // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                        double local_EhminAzi = EhminAzi_list[deformationEpisodeNo];
                        double local_EhminRate = EhminRate_GeologicalTimeUnits_list[deformationEpisodeNo] / TimeUnitConverter;
                        double local_EhmaxRate = EhmaxRate_GeologicalTimeUnits_list[deformationEpisodeNo] / TimeUnitConverter;
                        double local_AppliedOverpressureRate = AppliedOverpressureRate_GeologicalTimeUnits_list[deformationEpisodeNo] / TimeUnitConverter;
                        double local_AppliedTemperatureChange = AppliedTemperatureChange_GeologicalTimeUnits_list[deformationEpisodeNo] / TimeUnitConverter;
                        double local_AppliedUpliftRate = AppliedUpliftRate_GeologicalTimeUnits_list[deformationEpisodeNo] / TimeUnitConverter;
                        double local_StressArchingFactor = StressArchingFactor_list[deformationEpisodeNo];
                        double local_DeformationEpisodeDuration = DeformationEpisodeDuration_GeologicalTimeUnits_list[deformationEpisodeNo] * TimeUnitConverter;

                        // Get local handles for the static load properties and flags
                        bool UseGridFor_EhminAzi = UseGridFor_EhminAzi_list[deformationEpisodeNo];
                        Property EhminAzi_grid = EhminAzi_grid_list[deformationEpisodeNo];
                        bool convertFromGeneral_EhminAzi = convertFromGeneral_EhminAzi_list[deformationEpisodeNo];
                        bool UseGridFor_EhminRate = UseGridFor_EhminRate_list[deformationEpisodeNo];
                        Property EhminRate_grid = EhminRate_grid_list[deformationEpisodeNo];
                        bool UseGridFor_EhmaxRate = UseGridFor_EhmaxRate_list[deformationEpisodeNo];
                        Property EhmaxRate_grid = EhmaxRate_grid_list[deformationEpisodeNo];
                        bool UseGridFor_AppliedOverpressureRate = UseGridFor_AppliedOverpressureRate_list[deformationEpisodeNo];
                        Property AppliedOverpressureRate_grid = AppliedOverpressureRate_grid_list[deformationEpisodeNo];
                        bool convertFromGeneral_AppliedOverpressureRate = convertFromGeneral_AppliedOverpressureRate_list[deformationEpisodeNo];
                        bool UseGridFor_AppliedTemperatureChange = UseGridFor_AppliedTemperatureChange_list[deformationEpisodeNo];
                        Property AppliedTemperatureChange_grid = AppliedTemperatureChange_grid_list[deformationEpisodeNo];
                        bool convertFromGeneral_AppliedTemperatureChange = convertFromGeneral_AppliedTemperatureChange_list[deformationEpisodeNo];
                        bool UseGridFor_AppliedUpliftRate = UseGridFor_AppliedUpliftRate_list[deformationEpisodeNo];
                        Property AppliedUpliftRate_grid = AppliedUpliftRate_grid_list[deformationEpisodeNo];
                        bool convertFromGeneral_AppliedUpliftRate = convertFromGeneral_AppliedUpliftRate_list[deformationEpisodeNo];

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
                                            // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                            double cell_ehmin_rate = (double)EhminRate_grid[cellRef] / TimeUnitConverter;
                                            if (!double.IsNaN(cell_ehmin_rate))
                                            {
                                                ehmin_rate_total += cell_ehmin_rate;
                                                ehmin_rate_novalues++;
                                            }
                                        }

                                        // Update ehmax rate total if defined
                                        if (UseGridFor_EhmaxRate)
                                        {
                                            // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                            double cell_ehmax_rate = (double)EhmaxRate_grid[cellRef] / TimeUnitConverter;
                                            if (!double.IsNaN(cell_ehmax_rate))
                                            {
                                                ehmax_rate_total += cell_ehmax_rate;
                                                ehmax_rate_novalues++;
                                            }
                                        }

                                        // Update overpressure rate total if defined
                                        if (UseGridFor_AppliedOverpressureRate)
                                        {
                                            // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                            // However unit conversion for the load quantity itself (in this case, pressure) will be done automatically
                                            double cell_OP_rate = (double)AppliedOverpressureRate_grid[cellRef] / TimeUnitConverter;
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_AppliedOverpressureRate)
                                                cell_OP_rate = toSIPressureUnits.Convert(cell_OP_rate);
                                            if (!double.IsNaN(cell_OP_rate))
                                            {
                                                OP_rate_total += cell_OP_rate;
                                                OP_rate_novalues++;
                                            }
                                        }

                                        // Update temperature change total if defined
                                        if (UseGridFor_AppliedTemperatureChange)
                                        {
                                            // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                            // However unit conversion for the load quantity itself (in this case, temperature) will be done automatically
                                            double cell_temp_rate = (double)AppliedTemperatureChange_grid[cellRef] / TimeUnitConverter;
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_AppliedTemperatureChange)
                                                cell_temp_rate = toSITemperatureUnits.Convert(cell_temp_rate);
                                            if (!double.IsNaN(cell_temp_rate))
                                            {
                                                temp_rate_total += cell_temp_rate;
                                                temp_rate_novalues++;
                                            }
                                        }

                                        // Update uplift rate total if defined
                                        if (UseGridFor_AppliedUpliftRate)
                                        {
                                            // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                            // However unit conversion for the load quantity itself (in this case, uplift) will be done automatically
                                            double cell_uplift_rate = (double)AppliedUpliftRate_grid[cellRef] / TimeUnitConverter;
                                            // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                            if (convertFromGeneral_AppliedUpliftRate)
                                                cell_uplift_rate = toSIDepthUnits.Convert(cell_uplift_rate);
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
                                        local_EhminAzi = cell_ehmin_orient;
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
                                    // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                    double cell_ehmin_rate = (double)EhminRate_grid[cellRef] / TimeUnitConverter;
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
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                    double cell_ehmax_rate = (double)EhmaxRate_grid[cellRef] / TimeUnitConverter;
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
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                    // However unit conversion for the load quantity itself (in this case, pressure) will be done automatically
                                    double cell_OP_rate = (double)AppliedOverpressureRate_grid[cellRef] / TimeUnitConverter;
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    if (convertFromGeneral_AppliedOverpressureRate)
                                        cell_OP_rate = toSIPressureUnits.Convert(cell_OP_rate);
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
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                    // However unit conversion for the load quantity itself (in this case, temperature) will be done automatically
                                    double cell_temp_rate = (double)AppliedTemperatureChange_grid[cellRef] / TimeUnitConverter;
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    if (convertFromGeneral_AppliedTemperatureChange)
                                        cell_temp_rate = toSITemperatureUnits.Convert(cell_temp_rate);
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
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    // Time conversion for the load rate properties must be carried out manually, as there are no inbuilt Petrel units for inverse time
                                    // However unit conversion for the load quantity itself (in this case, uplift) will be done automatically
                                    double cell_uplift_rate = (double)AppliedUpliftRate_grid[cellRef] / TimeUnitConverter;
                                    // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                    if (convertFromGeneral_AppliedUpliftRate)
                                        cell_uplift_rate = toSIDepthUnits.Convert(cell_uplift_rate);
                                    if (!double.IsNaN(cell_uplift_rate))
                                    {
                                        local_AppliedUpliftRate = cell_uplift_rate;
                                        break;
                                    }
                                }
                            }
                        }

                        // If this is the first deformation episode, set the default fracture azimuth for the gridblock
                        if (deformationEpisodeNo == 0)
                            local_DefaultFractureAzimuth = local_EhminAzi;

                        // Get the dynamic deformation load data as grid properties if required
                        // Get local handles for the Property objects and flags defining the load data for this deformation episode
                        bool UsePropertyFor_FluidPressure = UsePropertyFor_FluidPressure_list[deformationEpisodeNo];
                        Property local_FluidPressure_property = FluidPressure_property_list[deformationEpisodeNo];
                        bool UsePropertyFor_Szz = UsePropertyFor_Szz_list[deformationEpisodeNo];
                        Property local_Szz_property = Szz_property_list[deformationEpisodeNo];
                        bool UsePropertyFor_StressTensor = UsePropertyFor_StressTensor_list[deformationEpisodeNo];
                        Property local_Sxx_property = Sxx_property_list[deformationEpisodeNo];
                        Property local_Syy_property = Syy_property_list[deformationEpisodeNo];
                        Property local_Sxy_property = Sxy_property_list[deformationEpisodeNo];
                        bool UsePropertyFor_ShvComponents = UsePropertyFor_ShvComponents_list[deformationEpisodeNo];
                        Property local_Syz_property = Syz_property_list[deformationEpisodeNo];
                        Property local_Szx_property = Szx_property_list[deformationEpisodeNo];

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

                            // Loop through all the Petrel cells in the gridblock
                            for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                    for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                    {
                                        Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                        // Update final absolute vertical stress total if defined
                                        if (UsePropertyFor_Szz)
                                        {
                                            double cell_szz = (double)local_Szz_property[cellRef];
                                            if (!double.IsNaN(cell_szz))
                                            {
                                                szz_total += cell_szz;
                                                szz_novalues++;
                                            }
                                        }

                                        // Update final horizontal stress tensor components total if defined
                                        if (UsePropertyFor_StressTensor)
                                        {
                                            double cell_sxx = (double)local_Sxx_property[cellRef];
                                            if (!double.IsNaN(cell_sxx))
                                            {
                                                sxx_total += cell_sxx;
                                                sxx_novalues++;
                                            }
                                            double cell_syy = (double)local_Syy_property[cellRef];
                                            if (!double.IsNaN(cell_syy))
                                            {
                                                syy_total += cell_syy;
                                                syy_novalues++;
                                            }
                                            double cell_sxy = (double)local_Sxy_property[cellRef];
                                            if (!double.IsNaN(cell_sxy))
                                            {
                                                sxy_total += cell_sxy;
                                                sxy_novalues++;
                                            }
                                        }

                                        // Update final vertical shear stress tensor components total if defined
                                        if (UsePropertyFor_ShvComponents)
                                        {
                                            double cell_szx = (double)local_Szx_property[cellRef];
                                            if (!double.IsNaN(cell_szx))
                                            {
                                                szx_total += cell_szx;
                                                szx_novalues++;
                                            }
                                            double cell_syz = (double)local_Syz_property[cellRef];
                                            if (!double.IsNaN(cell_syz))
                                            {
                                                syz_total += cell_syz;
                                                syz_novalues++;
                                            }
                                        }

                                        // Update final fluid pressure total if defined
                                        if (UsePropertyFor_FluidPressure)
                                        {
                                            double cell_fluidpressure = (double)local_FluidPressure_property[cellRef];
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

                            // Update final absolute vertical stress total if defined
                            if (UsePropertyFor_Szz)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    double cell_szz = (double)local_Szz_property[cellRef];
                                    if (!double.IsNaN(cell_szz))
                                    {
                                        finalSzz = cell_szz;
                                        break;
                                    }
                                }
                            }

                            // Update final horizontal stress totals if defined
                            if (UsePropertyFor_StressTensor)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                // We need valid data for all three horizontal components of the strain tensor
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    double cell_sxx = (double)local_Sxx_property[cellRef];
                                    double cell_syy = (double)local_Syy_property[cellRef];
                                    double cell_sxy = (double)local_Sxy_property[cellRef];
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
                            if (UsePropertyFor_ShvComponents)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                // We need valid data for all three horizontal components of the strain tensor
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    double cell_szx = (double)local_Szx_property[cellRef];
                                    double cell_syz = (double)local_Syz_property[cellRef];
                                    if (!double.IsNaN(cell_szx) && !double.IsNaN(cell_syz))
                                    {
                                        finalSzx = cell_szx;
                                        finalSyz = cell_syz;
                                        break;
                                    }
                                }
                            }

                            // Update final fluid pressure total if defined
                            if (UsePropertyFor_FluidPressure)
                            {
                                // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                {
                                    cellRef.K = PetrelGrid_DataCellK;
                                    double cell_fluidpressure = (double)local_FluidPressure_property[cellRef];
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
                        bool overideStressRate = UsePropertyFor_StressTensor && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalSzz) && !double.IsNaN(finalSxx) && !double.IsNaN(finalSyy) && !double.IsNaN(finalSxy);
                        bool overideShvComponents = UsePropertyFor_ShvComponents && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalSzx) && !double.IsNaN(finalSyz);
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
                        bool overrideFluidPressure = UsePropertyFor_FluidPressure && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalFluidPressure);
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
                        bool overrideStressArchingFactor = UsePropertyFor_Szz && !UsePropertyFor_StressTensor && (local_DeformationEpisodeDuration > 0) && !double.IsNaN(finalSzz);
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
                            local_StressArchingFactor = (dSigmazz_dt - dLithStress_dt) / ((local_BiotCoefficient * local_AppliedOverpressureRate) + dEtherm_dt);
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

                        // Check if the deformation episode is subdivided into sub episodes
                        // If it is not, add the load data for this deformation episode to the deformation episode lists
                        if (!SubEpisodesDefined_list[deformationEpisodeNo])
                        {
                            // Add the strain load data
                            local_EhRate_list.Add(Tensor2S.HorizontalStrainTensor(local_EhminRate, local_EhmaxRate, local_EhminAzi));
                            local_AppliedOverpressureRate_list.Add(local_AppliedOverpressureRate);
                            local_AppliedTemperatureChange_list.Add(local_AppliedTemperatureChange);
                            local_AppliedUpliftRate_list.Add(local_AppliedUpliftRate);
                            local_StressArchingFactor_list.Add(local_StressArchingFactor);
                            local_DeformationEpisodeDuration_list.Add(local_DeformationEpisodeDuration);
                            local_StressStateDefinition_list.Add(StressStateDefinition_list[deformationEpisodeNo]);

                            // Add the stress load tensor - this will be null if not defined
                            local_StressRateTensor_list.Add(local_StressRateTensor);

                            // Add values for the inital data - these will be null if not defined
                            local_InitialStressTensor_list.Add(local_InitialStressTensor);
                            local_InitialFluidPressure_list.Add(local_InitialFluidPressure);
                            local_InitialVerticalStress_list.Add(local_InitialVerticalStress);

#if DEBUG_FRAC_INPUT
                                        if (local_StressRateTensor is null)
                                            PetrelLogger.InfoOutputWindow(string.Format("New deformation episode: Duration {0}, EhminAzi {1}, EhminRate {2}, EhmaxRate {3}, OP rate {4}, Temp change {5}, Uplift rate {6}, Stress arching factor {7});", local_DeformationEpisodeDuration, local_EhminAzi, local_EhminRate, local_EhmaxRate, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor));
                                        else
                                            PetrelLogger.InfoOutputWindow(string.Format("New deformation episode: Duration {0}, Initial stress (Sxx, Syy, Szz, Sxy, Syz, Szx) = ({1}, {2}, {3}, {4}, {5}, {6}), Initial FP {7}, Final stress (Sxx, Syy, Szz, Sxy, Syz, Szx) = ({8}, {9}, {10}, {11}, {12}, {13}), Final FP {14}", local_DeformationEpisodeDuration, initialSxx, initialSyy, initialSzz, initialSxy, initialSyz, initialSzx, initialFluidPressure, finalSxx, finalSyy, finalSzz, finalSxy, finalSyz, finalSzx, finalFluidPressure));
#endif
                        }
                        // If the deformation episode is subdivided into sub episodes, the dynamic load data takes the form of simulation results
                        else
                        {
                            // Get the number and durations of the sub episodes
                            List<double> subEpisodeDuration_list = SubEpisodeDurations_GeologicalTimeUnits_list[deformationEpisodeNo];
                            int noSubEpisodes = subEpisodeDuration_list.Count;

                            // Get lists of GridProperty objects defining the load data for each sub episode
                            List<GridProperty> local_FluidPressure_grid_list = FluidPressure_grid_list[deformationEpisodeNo];
                            bool UseGridFor_FluidPressure = UseGridPropertyTimeSeriesFor_FluidPressure_list[deformationEpisodeNo] && (local_FluidPressure_grid_list.Count >= noSubEpisodes + 1);
                            List<GridProperty> local_Szz_grid_list = Szz_grid_list[deformationEpisodeNo];
                            bool UseGridFor_Szz = UseGridPropertyTimeSeriesFor_Szz_list[deformationEpisodeNo] && (local_Szz_grid_list.Count >= noSubEpisodes + 1);
                            List<GridProperty> local_Sxx_grid_list = Sxx_grid_list[deformationEpisodeNo];
                            List<GridProperty> local_Syy_grid_list = Syy_grid_list[deformationEpisodeNo];
                            List<GridProperty> local_Sxy_grid_list = Sxy_grid_list[deformationEpisodeNo];
                            bool UseGridFor_StressTensor = UseGridPropertyTimeSeriesFor_StressTensor_list[deformationEpisodeNo] && (local_Sxx_grid_list.Count >= noSubEpisodes + 1) && (local_Syy_grid_list.Count >= noSubEpisodes + 1) && (local_Sxy_grid_list.Count >= noSubEpisodes + 1);
                            List<GridProperty> local_Szx_grid_list = Szx_grid_list[deformationEpisodeNo];
                            List<GridProperty> local_Syz_grid_list = Syz_grid_list[deformationEpisodeNo];
                            bool UseGridFor_ShvComponents = UseGridPropertyTimeSeriesFor_ShvComponents_list[deformationEpisodeNo] && (local_Szx_grid_list.Count >= noSubEpisodes + 1) && (local_Syz_grid_list.Count >= noSubEpisodes + 1);

                            // Loop through each sub episode; if the deformation episode is not subdivided, we must still go through the loop once to add the deformation episode data to the lists
                            // NB in the -1 iteration the initial values for the first timestep will be loaded into the final value local variables; these will then be copied into the initial values variables in iteration 0
                            for (int subEpisodeNo = -1; subEpisodeNo < noSubEpisodes; subEpisodeNo++)
                            {
                                // Update the initial load values with the final load values from the previous sub episode, except for iteration -1
                                if (subEpisodeNo >= 0)
                                {
                                    initialSzz = finalSzz;
                                    initialSxx = finalSxx;
                                    initialSyy = finalSyy;
                                    initialSxy = finalSxy;
                                    initialSzx = finalSzx;
                                    initialSyz = finalSyz;
                                    initialFluidPressure = finalFluidPressure;
                                }

                                // Get the final load values from the grid properties
                                // In iteration -1, these will be the initial load values for the first episode
                                {
                                    // Get local handles for the final grid properties
                                    GridProperty local_Szz_grid = (UseGridFor_Szz ? local_Szz_grid_list[subEpisodeNo + 1] : null);
                                    GridProperty local_Sxx_grid = (UseGridFor_StressTensor ? local_Sxx_grid_list[subEpisodeNo + 1] : null);
                                    GridProperty local_Syy_grid = (UseGridFor_StressTensor ? local_Syy_grid_list[subEpisodeNo + 1] : null);
                                    GridProperty local_Sxy_grid = (UseGridFor_StressTensor ? local_Sxy_grid_list[subEpisodeNo + 1] : null);
                                    GridProperty local_Szx_grid = (UseGridFor_ShvComponents ? local_Szx_grid_list[subEpisodeNo + 1] : null);
                                    GridProperty local_Syz_grid = (UseGridFor_ShvComponents ? local_Syz_grid_list[subEpisodeNo + 1] : null);
                                    GridProperty local_FluidPressure_grid = (UseGridFor_FluidPressure ? local_FluidPressure_grid_list[subEpisodeNo + 1] : null);

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

                                        // Loop through all the Petrel cells in the gridblock
                                        for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                            for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                {
                                                    Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                    // Update final absolute vertical stress total if defined
                                                    if (UseGridFor_Szz)
                                                    {
                                                        double cell_szz = (double)local_Szz_grid[cellRef];
                                                        if (!double.IsNaN(cell_szz))
                                                        {
                                                            szz_total += cell_szz;
                                                            szz_novalues++;
                                                        }
                                                    }

                                                    // Update final horizontal stress tensor components total if defined
                                                    if (UseGridFor_StressTensor)
                                                    {
                                                        double cell_sxx = (double)local_Sxx_grid[cellRef];
                                                        if (!double.IsNaN(cell_sxx))
                                                        {
                                                            sxx_total += cell_sxx;
                                                            sxx_novalues++;
                                                        }
                                                        double cell_syy = (double)local_Syy_grid[cellRef];
                                                        if (!double.IsNaN(cell_syy))
                                                        {
                                                            syy_total += cell_syy;
                                                            syy_novalues++;
                                                        }
                                                        double cell_sxy = (double)local_Sxy_grid[cellRef];
                                                        if (!double.IsNaN(cell_sxy))
                                                        {
                                                            sxy_total += cell_sxy;
                                                            sxy_novalues++;
                                                        }
                                                    }

                                                    // Update final vertical shear stress tensor components total if defined
                                                    if (UseGridFor_ShvComponents)
                                                    {
                                                        double cell_szx = (double)local_Szx_grid[cellRef];
                                                        if (!double.IsNaN(cell_szx))
                                                        {
                                                            szx_total += cell_szx;
                                                            szx_novalues++;
                                                        }
                                                        double cell_syz = (double)local_Syz_grid[cellRef];
                                                        if (!double.IsNaN(cell_syz))
                                                        {
                                                            syz_total += cell_syz;
                                                            syz_novalues++;
                                                        }
                                                    }

                                                    // Update final fluid pressure total if defined
                                                    if (UseGridFor_FluidPressure)
                                                    {
                                                        double cell_fluidpressure = (double)local_FluidPressure_grid[cellRef];
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

                                        // Update final absolute vertical stress total if defined
                                        if (UseGridFor_Szz)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_szz = (double)local_Szz_grid[cellRef];
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
                                            // We need valid data for all three horizontal components of the strain tensor
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_sxx = (double)local_Sxx_grid[cellRef];
                                                double cell_syy = (double)local_Syy_grid[cellRef];
                                                double cell_sxy = (double)local_Sxy_grid[cellRef];
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
                                            // We need valid data for all three horizontal components of the strain tensor
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_szx = (double)local_Szx_grid[cellRef];
                                                double cell_syz = (double)local_Syz_grid[cellRef];
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
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_fluidpressure = (double)local_FluidPressure_grid[cellRef];
                                                if (!double.IsNaN(cell_fluidpressure))
                                                {
                                                    finalFluidPressure = cell_fluidpressure;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                } // End get the final load values from the grid properties

                                // If this is the -1 iteration, skip straight on to the next iteration
                                if (subEpisodeNo < 0)
                                    continue;

                                // Get the sub episode duration
                                local_DeformationEpisodeDuration = subEpisodeDuration_list[subEpisodeNo] * TimeUnitConverter;

                                // Get the load rates for the sub episode
                                // Create a tensor for the horizontal strain rate load
                                Tensor2S local_EhRate = Tensor2S.HorizontalStrainTensor(local_EhminRate, local_EhmaxRate, local_EhminAzi);
                                // NB This will be overridden if a stress load is defined
                                bool overideStressRateForSubEpisode = UseGridFor_StressTensor && !double.IsNaN(initialSzz) && !double.IsNaN(finalSzz) && !double.IsNaN(initialSxx) && !double.IsNaN(finalSxx) && !double.IsNaN(initialSyy) && !double.IsNaN(finalSyy) && !double.IsNaN(initialSxy) && !double.IsNaN(finalSxy);
                                bool overideShvComponentsForSubEpisode = UseGridFor_ShvComponents && !double.IsNaN(initialSzx) && !double.IsNaN(finalSzx) && !double.IsNaN(initialSyz) && !double.IsNaN(finalSyz);
                                Tensor2S local_StressRateForSubEpisode = null;
                                if (overideStressRateForSubEpisode)
                                {
                                    double local_szzRate = (finalSzz - initialSzz) / local_DeformationEpisodeDuration;
                                    double local_sxxRate = (finalSxx - initialSxx) / local_DeformationEpisodeDuration;
                                    double local_syyRate = (finalSyy - initialSyy) / local_DeformationEpisodeDuration;
                                    double local_sxyRate = (finalSxy - initialSxy) / local_DeformationEpisodeDuration;
                                    double local_szxRate = 0;
                                    double local_syzRate = 0;
                                    if (overideShvComponentsForSubEpisode)
                                    {
                                        local_szxRate = (finalSzx - initialSzx) / local_DeformationEpisodeDuration;
                                        local_syzRate = (finalSyz - initialSyz) / local_DeformationEpisodeDuration;
                                    }
                                    local_StressRateForSubEpisode = new Tensor2S(local_sxxRate, local_syyRate, local_szzRate, local_sxyRate, local_syzRate, local_szxRate);
                                }
                                bool overrideFluidPressureForSubEpisode = UseGridFor_FluidPressure && !double.IsNaN(initialFluidPressure) && !double.IsNaN(finalFluidPressure);
                                if (overrideFluidPressureForSubEpisode)
                                {
                                    double local_FluidPressureRate = (finalFluidPressure - initialFluidPressure) / local_DeformationEpisodeDuration;
                                    double local_HydrostaticPressureRate = (local_AppliedUpliftRate > 0 ? -local_AppliedUpliftRate * FluidDensity * StressStrainState.Gravity : 0);
                                    local_AppliedOverpressureRate = local_FluidPressureRate - local_HydrostaticPressureRate;
                                }
                                // If the stress tensor is not defined, then changes in the absolute vertical stress within each sub episode will be accounted for through the stress arching factor
                                // NB The absolute vertical stress will also be reset at the start of each sub episode, so will remain synchronised with the specified input load
                                bool overrideStressArchingFactorForSubEpisode = UseGridFor_Szz && !UseGridFor_StressTensor && !double.IsNaN(initialSzz) && !double.IsNaN(finalSzz);
                                if (overrideStressArchingFactorForSubEpisode)
                                {
                                    double dSigmazz_dt = (finalSzz - initialSzz) / local_DeformationEpisodeDuration;
                                    double dLithStress_dt = (local_AppliedUpliftRate > 0 ? -local_AppliedUpliftRate * (MeanOverlyingSedimentDensity - FluidDensity) * StressStrainState.Gravity : 0);
                                    double local_Kb = local_YoungsMod / (2 * (1 + local_PoissonsRatio));
                                    double dEtherm_dt = local_Kb * local_ThermalExpansionCoefficient * local_AppliedTemperatureChange;
                                    local_StressArchingFactor = (dSigmazz_dt - dLithStress_dt) / ((local_BiotCoefficient * local_AppliedOverpressureRate) + dEtherm_dt);
                                    // Trim the result so it lies between 0 and 1 inclusive
                                    if (local_StressArchingFactor < 0)
                                        local_StressArchingFactor = 0;
                                    if (local_StressArchingFactor > 1)
                                        local_StressArchingFactor = 1;
                                }

                                // Add the data for this sub episode to the deformation episode lists
                                local_EhRate_list.Add(local_EhRate);
                                local_AppliedOverpressureRate_list.Add(local_AppliedOverpressureRate);
                                local_AppliedTemperatureChange_list.Add(local_AppliedTemperatureChange);
                                local_AppliedUpliftRate_list.Add(local_AppliedUpliftRate);
                                local_StressArchingFactor_list.Add(local_StressArchingFactor);
                                local_DeformationEpisodeDuration_list.Add(local_DeformationEpisodeDuration);
                                local_StressRateTensor_list.Add(local_StressRateForSubEpisode);
                                local_StressStateDefinition_list.Add(StressStateDefinition_list[deformationEpisodeNo]);

                                // Add data for the inital data lists
                                if (overideStressRateForSubEpisode && overideShvComponentsForSubEpisode)
                                    local_InitialStressTensor_list.Add(new Tensor2S(initialSxx, initialSyy, initialSzz, initialSxy, initialSyz, initialSzx));
                                else if (overideStressRateForSubEpisode)
                                    local_InitialStressTensor_list.Add(new Tensor2S(initialSxx, initialSyy, initialSzz, initialSxy, 0, 0));
                                else
                                    local_InitialStressTensor_list.Add(null);
                                if (overrideFluidPressureForSubEpisode)
                                    local_InitialFluidPressure_list.Add(initialFluidPressure);
                                else
                                    local_InitialFluidPressure_list.Add(double.NaN);
                                if (overrideStressArchingFactorForSubEpisode)
                                    local_InitialVerticalStress_list.Add(initialSzz);
                                else
                                    local_InitialVerticalStress_list.Add(double.NaN);

#if DEBUG_FRAC_INPUT
                                            string strainLoadText = "";
                                            if (overideStressRate && overideShvComponents)
                                                strainLoadText = string.Format("Initial stress (Sxx, Syy, Szz, Sxy, Syz, Szx) = ({0}, {1}, {2}, {3}, {4}, {5}), Final stress (Sxx, Syy, Szz, Sxy, Syz, Szx) = ({6}, {7}, {8}, {9}, {10}, {11})", initialSxx, initialSyy, initialSzz, initialSxy, initialSyz, initialSzx, finalSxx, finalSyy, finalSzz, finalSxy, finalSyz, finalSzx);
                                            else if (overideStressRate)
                                                strainLoadText = string.Format("Initial stress (Sxx, Syy, Sxy, Szz) = ({0}, {1}, {2}, {3}), Final stress (Sxx, Syy, Sxy, Szz) = ({4}, {5}, {6}, {7})", initialSxx, initialSyy, initialSxy, initialSzz, finalSxx, finalSyy, finalSxy, finalSzz);
                                            else
                                                strainLoadText = string.Format("EhminAzi {0}, EhminRate {1}, EhmaxRate {2}", local_EhminAzi, local_EhminRate, local_EhmaxRate);
                                            string fluidPressureLoadText = (overrideFluidPressure ? string.Format("Initial fluid pressure {0}, Final fluid pressure {1}, OP rate {2}", initialFluidPressure, finalFluidPressure, local_AppliedOverpressureRate) : string.Format("OP rate {0}", local_AppliedOverpressureRate));
                                            string safText = (overrideStressArchingFactor ? string.Format("Initial Sv {0}, Final Sv {1}, implied stress arching factor {2}", initialSzz, finalSzz, local_StressArchingFactor) : string.Format("Stress arching factor {0}", local_StressArchingFactor));
                                            PetrelLogger.InfoOutputWindow(string.Format("New deformation sub episode: Duration {0}, {1}, {2}, {3});", local_DeformationEpisodeDuration, strainLoadText, fluidPressureLoadText, safText));
#endif
                            }// End get the deformation load data for each sub episode
                        }// End check if the deformation episode is subdivided into sub episodes
                    }// End get the deformation load data for each deformation episode

                    // Get the depth at the start of deformation from the grid as required
                    // This will depend on whether we are averaging the stress and strain data over all Petrel cells that make up the gridblock, or taking the values from a single cell
                    // First we will create a local variable for the property value in this gridblock; we can then recalculate this without altering the global default value
                    double local_DepthAtDeformation = DepthAtDeformation;
                    if (UseGridFor_DepthAtDeformation)
                    {
                        if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
                        {
                            // Create local variables for running total and number of datapoints
                            double DepthAtDeformation_total = 0;
                            int DepthAtDeformation_novalues = 0;

                            // Loop through all the Petrel cells in the gridblock
                            for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                    for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                    {
                                        Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                        // Update depth at deformation total if defined
                                        double cell_depthatdeformation = (double)DepthAtDeformation_grid[cellRef];
                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                        if (convertFromGeneral_DepthAtDeformation)
                                            cell_depthatdeformation = toSIDepthUnits.Convert(cell_depthatdeformation);
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

                            // Update depth at deformation total if defined
                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                            {
                                cellRef.K = PetrelGrid_DataCellK;
                                double cell_depthatdeformation = (double)DepthAtDeformation_grid[cellRef];
                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                if (convertFromGeneral_DepthAtDeformation)
                                    cell_depthatdeformation = toSIDepthUnits.Convert(cell_depthatdeformation);
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
                        for (int deformationEpisodeNo = 0; deformationEpisodeNo < noTotalDeformationEpisodes; deformationEpisodeNo++)
                        {
                            double local_AppliedUpliftRate = local_AppliedUpliftRate_list[deformationEpisodeNo];
                            double local_DeformationEpisodeDuration = local_DeformationEpisodeDuration_list[deformationEpisodeNo];
                            if (local_DeformationEpisodeDuration > 0)
                                local_Depth += (local_AppliedUpliftRate * local_DeformationEpisodeDuration);
                        }
                    }

                    // Create a new gridblock object containing the required number of fracture sets
                    GridblockConfiguration gc = new GridblockConfiguration(local_LayerThickness, local_Depth, NoFractureSets);

                    // Check if the western boundary if faulted
                    // This will be the case if any of the Petrel cells on the southern boundary are faulted
                    bool faultToWest = false;
                    if (!IgnoreFaults)
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
                    if (!IgnoreFaults)
                        for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                        {
                            Index2 SWpillar = new Index2(PetrelGrid_I, PetrelGrid_FirstCellJ);
                            Index2 SEpillar = new Index2(PetrelGrid_I + 1, PetrelGrid_FirstCellJ);
                            if (PetrelGrid.IsNodeFaulted(SWpillar) && PetrelGrid.IsNodeFaulted(SEpillar))
                                faultToSouth = true;
                        }

#if DEBUG_FRAC_INPUT
                                foreach (PointXYZ point in new PointXYZ[] { FractureGrid_SWtop, FractureGrid_NWtop, FractureGrid_NEtop, FractureGrid_SEtop, FractureGrid_SWbottom, FractureGrid_NWbottom, FractureGrid_NEbottom, FractureGrid_SEbottom })
                                {
                                    if (minX > point.X) minX = point.X;
                                    if (minY > point.Y) minY = point.Y;
                                    if (minZ > point.Z) minZ = point.Z;
                                    if (maxX < point.X) maxX = point.X;
                                    if (maxY < point.Y) maxY = point.Y;
                                    if (maxZ < point.Z) maxZ = point.Z;
                                }

                                PetrelLogger.InfoOutputWindow("Geometry");
                                PetrelLogger.InfoOutputWindow(string.Format("SW top of FractureGrid: ({0}, {1}, {2});", SW_topgrid_corner.X, SW_topgrid_corner.Y, SW_topgrid_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("NW top of FractureGrid: ({0}, {1}, {2});", NW_topgrid_corner.X, NW_topgrid_corner.Y, NW_topgrid_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("NE top of FractureGrid: ({0}, {1}, {2});", NE_topgrid_corner.X, NE_topgrid_corner.Y, NE_topgrid_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("SE top of FractureGrid: ({0}, {1}, {2});", SE_topgrid_corner.X, SE_topgrid_corner.Y, SE_topgrid_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_SWtop = new PointXYZ({0}, {1}, {2});", SW_top_corner.X, SW_top_corner.Y, SW_top_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_SWbottom = new PointXYZ({0}, {1}, {2});", SW_bottom_corner.X, SW_bottom_corner.Y, SW_bottom_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_NWtop = new PointXYZ({0}, {1}, {2});", NW_top_corner.X, NW_top_corner.Y, NW_top_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_NWbottom = new PointXYZ({0}, {1}, {2});", NW_bottom_corner.X, NW_bottom_corner.Y, NW_bottom_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_NEtop = new PointXYZ({0}, {1}, {2});", NE_top_corner.X, NE_top_corner.Y, NE_top_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_NEbottom = new PointXYZ({0}, {1}, {2});", NE_bottom_corner.X, NE_bottom_corner.Y, NE_bottom_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_SEtop = new PointXYZ({0}, {1}, {2});", SE_top_corner.X, SE_top_corner.Y, SE_top_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("PointXYZ FractureGrid_SEbottom = new PointXYZ({0}, {1}, {2});", SE_bottom_corner.X, SE_bottom_corner.Y, SE_bottom_corner.Z));
                                PetrelLogger.InfoOutputWindow(string.Format("LayerThickness = {0}; Depth = {1}; Surface height = {2}", local_LayerThickness, local_Current_Depth, Math.Max(local_Current_SurfaceHeight, 0)));
#endif

                    // Set the gridblock cornerpoints
                    gc.setGridblockCorners(FractureGrid_SWtop, FractureGrid_SWbottom, FractureGrid_NWtop, FractureGrid_NWbottom, FractureGrid_NEtop, FractureGrid_NEbottom, FractureGrid_SEtop, FractureGrid_SEbottom);

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
                    gc.PropControl.setPropagationControl(OutputPopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio,
                        MinimumClearZoneVolume, MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, FractureNucleationPosition, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, ModelTimeUnits, OutputFracturePorosity, FractureApertureControl, OutputFracturePermeabilityTensor, PermeabilityAlgorithm, local_DefaultFractureAzimuth);

                    // Set folder path for output files
                    gc.PropControl.FolderPath = folderPath;

#if DEBUG_FRAC_INPUT
                                PetrelLogger.InfoOutputWindow("Properties");
                                PetrelLogger.InfoOutputWindow(string.Format("sv': {0}", gc.StressStrain.LithostaticStress_eff_Terzaghi));
                                PetrelLogger.InfoOutputWindow(string.Format("Young's Mod: {0}, Poisson's ratio: {1}, Biot coefficient: {2}, Crack surface energy: {3}, Friction coefficient: {4}", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient));
                                PetrelLogger.InfoOutputWindow("Create gridblock");
                                PetrelLogger.InfoOutputWindow(string.Format("gc = new GridblockConfiguration({0}, {1}, {2});", local_LayerThickness, local_Current_Depth, NoFractureSets));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.MechProps.setMechanicalProperties({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, TimeUnits.{11});", local_YoungsMod, local_PoissonsRatio, local_Porosity, local_BiotCoefficient, local_ThermalExpansionCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_SubcriticalPropIndex, ModelTimeUnits));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.MechProps.setFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5});", DynamicApertureMultiplier, JRC, UCSRatio, InitialNormalStress, FractureNormalStiffness, MaximumClosure));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.MechProps.setHostRockPermeability({0}, {1});", local_HostRock_kh, local_HostRock_kv));
                                if (InitialStressRelaxation < 0)
                                    PetrelLogger.InfoOutputWindow(string.Format("gc.StressStrain.SetCriticalInitialStressStrainState({0}, {1}, {2});", MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure));
                                else
                                    PetrelLogger.InfoOutputWindow(string.Format("gc.StressStrain.SetInitialStressStrainState({0}, {1}, {2}, {3});", MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure, local_InitialStressRelaxation));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.StressStrain.GeothermalGradient = {0};", GeothermalGradient));
                                PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.setPropagationControl({0}, {1}, {2}, {3}, {4}, {5}, StressDistribution.{6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, TimeUnits.{19}, {20}, {21}, {22}, {23}, {24});",
                                    CalculatePopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio,
                                    MinimumClearZoneVolume, MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, FractureNucleationPosition, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, ModelTimeUnits, CalculateFracturePorosity, FractureApertureControl, CalculateFracturePermeabilityTensor, PermeabilityAlgorithm, local_DefaultFractureAzimuth));
#endif

                    // Add the deformation load data 
                    // Keep a record of the initial fluid pressure at the start of each timestep in case it is not defined for a stress load
                    double initialFP = gc.StressStrain.P_f;
                    for (int deformationEpisodeNo = 0; deformationEpisodeNo < noTotalDeformationEpisodes; deformationEpisodeNo++)
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
                        StressStateDefinition local_StressStateDefinition = local_StressStateDefinition_list[deformationEpisodeNo];
                        if (local_StressRateTensor is null)
                            local_StressStateDefinition = StressStateDefinition.Strain;

                        // Add the deformation episode to the deformation episode list in the PropControl object, using the function appropriate to the data type
                        switch (local_StressStateDefinition)
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
                                                PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.AddDeformationEpisode_StrainLoad({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_EhRate_info, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor, local_DeformationEpisodeDuration, local_InitialVerticalStress, local_InitialFluidPressure));
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
                                                PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.AddDeformationEpisode_AbsoluteStressLoad({0}, {1}, {2}, {3}, {4});", local_StressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStress_info, initialFP));
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
                                                PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.AddDeformationEpisode_TerzaghiStressLoad({0}, {1}, {2}, {3}, {4});", local_StressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStress_info, initialFP));
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
                                                PetrelLogger.InfoOutputWindow(string.Format("gc.PropControl.AddDeformationEpisode_BiotStressLoad({0}, {1}, {2}, {3}, {4}, {5});", local_StressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialStress_info, initialFP, local_BiotCoefficient));
#endif
                                }
                                break;
                            default:
                                PetrelLogger.InfoOutputWindow(string.Format("No load defined for deformation episode {0}", deformationEpisodeNo));
                                break;
                        }

                        // Update the record of the initial fluid pressure
                        initialFP += (local_AppliedOverpressureRate * ((local_DeformationEpisodeDuration > 0) ? local_DeformationEpisodeDuration : 0));

                    }// End add the deformation load data

                    // Create the fracture sets
                    if (Mode1Only)
                        gc.resetFractures(local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures);
                    else if (Mode2Only)
                        gc.resetFractures(local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures);
                    else
                        gc.resetFractures(local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, BiazimuthalConjugate, AllowReverseFractures);

#if DEBUG_FRAC_INPUT
                                if (Mode1Only)
                                    PetrelLogger.InfoOutputWindow(string.Format("gc.resetFractures({0}, {1}, FractureMode.{2}, {3});", local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures));
                                else if (Mode2Only)
                                    PetrelLogger.InfoOutputWindow(string.Format("gc.resetFractures({0}, {1}, FractureMode.{2}, {3});", local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures));
                                else
                                    PetrelLogger.InfoOutputWindow(string.Format("gc.resetFractures({0}, {1}, {2}, {3});", local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, BiazimuthalConjugate, AllowReverseFractures));
#endif
                    // NB the fracture aperture control data must be set after the present day stress is defined, as it may be dependent on it

                    // If required, define the present day stress
#if DEBUG_FRAC_INPUT
                                PetrelLogger.InfoOutputWindow("");
                                PetrelLogger.InfoOutputWindow(string.Format("Use present day stress? {0}", UsePresentDayStress));
                                PetrelLogger.InfoOutputWindow(string.Format("Define present day stress from {0}", PresentDayStressInput));
#endif
                    if (UsePresentDayStress)
                    {
                        switch (PresentDayStressInput)
                        {
                            case StressStateDefinition.Strain:
                                {
                                    // Get the present day strain and fluid overpressure from the grid as required
                                    // This will depend on whether we are averaging the strain and fluid overpressure properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                                    // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                                    double local_EhminAzi_PresentDay = EhminAzi_PresentDay;
                                    double local_Ehmin_PresentDay = Ehmin_PresentDay;
                                    double local_Ehmax_PresentDay = Ehmax_PresentDay;
                                    double local_AppliedOverpressure_PresentDay = AppliedOverpressure_PresentDay;

                                    if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
                                    {
                                        // Create local variables for running total and number of datapoints for each property
                                        double EhminAzi_PresentDay_total = 0;
                                        int EhminAzi_PresentDay_novalues = 0;
                                        double Ehmin_PresentDay_total = 0;
                                        int Ehmin_PresentDay_novalues = 0;
                                        double Ehmax_PresentDay_total = 0;
                                        int Ehmax_PresentDay_novalues = 0;
                                        double AppliedOverpressure_total = 0;
                                        int AppliedOverpressure_novalues = 0;

                                        // Loop through all the Petrel cells in the gridblock
                                        for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                            for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                {
                                                    Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                    // Update minimum horizontal strain azimuth total if defined
                                                    if (UseGridFor_EhminAzi_PresentDay)
                                                    {
                                                        double cell_EhminAzi_PresentDay = (double)EhminAzi_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_EhminAzi_PresentDay)
                                                            cell_EhminAzi_PresentDay = toSIAzimuthUnits.Convert(cell_EhminAzi_PresentDay);
                                                        if (!double.IsNaN(cell_EhminAzi_PresentDay))
                                                        {
                                                            EhminAzi_PresentDay_total += cell_EhminAzi_PresentDay;
                                                            EhminAzi_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update minimum horizontal strain total if defined
                                                    if (UseGridFor_Ehmin_PresentDay)
                                                    {
                                                        double cell_Ehmin_PresentDay = (double)Ehmin_PresentDay_grid[cellRef];
                                                        if (!double.IsNaN(cell_Ehmin_PresentDay))
                                                        {
                                                            Ehmin_PresentDay_total += cell_Ehmin_PresentDay;
                                                            Ehmin_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update maximum horizontal strain total if defined
                                                    if (UseGridFor_Ehmax_PresentDay)
                                                    {
                                                        double cell_Ehmax_PresentDay = (double)Ehmax_PresentDay_grid[cellRef];
                                                        if (!double.IsNaN(cell_Ehmax_PresentDay))
                                                        {
                                                            Ehmax_PresentDay_total += cell_Ehmax_PresentDay;
                                                            Ehmax_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update fluid overpressure total if defined
                                                    if (UseGridFor_AppliedOverpressure_PresentDay)
                                                    {
                                                        double cell_AppliedOverpressure = (double)AppliedOverpressure_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_AppliedOverpressure_PresentDay)
                                                            cell_AppliedOverpressure = toSIPressureUnits.Convert(cell_AppliedOverpressure);
                                                        if (!double.IsNaN(cell_AppliedOverpressure))
                                                        {
                                                            AppliedOverpressure_total += cell_AppliedOverpressure;
                                                            AppliedOverpressure_novalues++;
                                                        }
                                                    }

                                                }

                                        // Update the gridblock values with the averages - if there is any data to calculate them from
                                        if (EhminAzi_PresentDay_novalues > 0)
                                            local_EhminAzi_PresentDay = EhminAzi_PresentDay_total / (double)EhminAzi_PresentDay_novalues;
                                        if (Ehmin_PresentDay_novalues > 0)
                                            local_Ehmin_PresentDay = Ehmin_PresentDay_total / (double)Ehmin_PresentDay_novalues;
                                        if (Ehmax_PresentDay_novalues > 0)
                                            local_Ehmax_PresentDay = Ehmax_PresentDay_total / (double)Ehmax_PresentDay_novalues;
                                        if (AppliedOverpressure_novalues > 0)
                                            local_AppliedOverpressure_PresentDay = AppliedOverpressure_total / (double)AppliedOverpressure_novalues;
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

                                        // Update minimum horizontal strain azimuth total if defined
                                        if (UseGridFor_EhminAzi_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_EhminAzi_PresentDay = (double)EhminAzi_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_EhminAzi_PresentDay)
                                                    cell_EhminAzi_PresentDay = toSIAzimuthUnits.Convert(cell_EhminAzi_PresentDay);
                                                if (!double.IsNaN(cell_EhminAzi_PresentDay))
                                                {
                                                    local_EhminAzi_PresentDay = cell_EhminAzi_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update minimum horizontal strain total if defined
                                        if (UseGridFor_Ehmin_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Ehmin_PresentDay = (double)Ehmin_PresentDay_grid[cellRef];
                                                if (!double.IsNaN(cell_Ehmin_PresentDay))
                                                {
                                                    local_Ehmin_PresentDay = cell_Ehmin_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update maximum horizontal strain total if defined
                                        if (UseGridFor_Ehmax_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Ehmax_PresentDay = (double)Ehmax_PresentDay_grid[cellRef];
                                                if (!double.IsNaN(cell_Ehmax_PresentDay))
                                                {
                                                    local_Ehmax_PresentDay = cell_Ehmax_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update fluid overpressure total if defined
                                        if (UseGridFor_AppliedOverpressure_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_AppliedOverpressure = (double)AppliedOverpressure_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_AppliedOverpressure_PresentDay)
                                                    cell_AppliedOverpressure = toSIPressureUnits.Convert(cell_AppliedOverpressure);
                                                if (!double.IsNaN(cell_AppliedOverpressure))
                                                {
                                                    local_AppliedOverpressure_PresentDay = cell_AppliedOverpressure;
                                                    break;
                                                }
                                            }
                                        }

                                    }
                                    // End get the present day strain and fluid overpressure from the grid as required

                                    // Get the present day mechanical properties from the grid as required
                                    // This will depend on whether we are averaging the mechanical properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                                    // First we will create local variables for the property values in this gridblock; we can then recalculate these without altering the global default values
                                    double local_YoungsMod_PresentDay = YoungsMod_PresentDay;
                                    double local_PoissonsRatio_PresentDay = PoissonsRatio_PresentDay;
                                    double local_BiotCoefficient_PresentDay = BiotCoefficient_PresentDay;

                                    if (AverageMechanicalPropertyData) // We are averaging over all Petrel cells in the gridblock
                                    {
                                        // Create local variables for running total and number of datapoints for each mechanical property
                                        double YoungsMod_PresentDay_total = 0;
                                        int YoungsMod_PresentDay_novalues = 0;
                                        double PoissonsRatio_PresentDay_total = 0;
                                        int PoissonsRatio_PresentDay_novalues = 0;
                                        double BiotCoeff_total = 0;
                                        int BiotCoeff_novalues = 0;

                                        // Loop through all the Petrel cells in the gridblock
                                        for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                            for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                {
                                                    Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                    // Update Young's Modulus total if defined
                                                    if (UseGridFor_YoungsMod_PresentDay)
                                                    {
                                                        double cell_YoungsMod_PresentDay = (double)YoungsMod_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_YoungsMod_PresentDay)
                                                            cell_YoungsMod_PresentDay = toSIYoungsModUnits.Convert(cell_YoungsMod_PresentDay);
                                                        if (!double.IsNaN(cell_YoungsMod_PresentDay))
                                                        {
                                                            YoungsMod_PresentDay_total += cell_YoungsMod_PresentDay;
                                                            YoungsMod_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update Poisson's ratio total if defined
                                                    if (UseGridFor_PoissonsRatio_PresentDay)
                                                    {
                                                        double cell_PoissonsRatio_PresentDay = (double)PoissonsRatio_PresentDay_grid[cellRef];
                                                        if (!double.IsNaN(cell_PoissonsRatio_PresentDay))
                                                        {
                                                            PoissonsRatio_PresentDay_total += cell_PoissonsRatio_PresentDay;
                                                            PoissonsRatio_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update Biot coefficient total if defined
                                                    if (UseGridFor_BiotCoefficient_PresentDay)
                                                    {
                                                        double cell_BiotCoeff = (double)BiotCoefficient_PresentDay_grid[cellRef];
                                                        if (!double.IsNaN(cell_BiotCoeff))
                                                        {
                                                            BiotCoeff_total += cell_BiotCoeff;
                                                            BiotCoeff_novalues++;
                                                        }
                                                    }

                                                }

                                        // Update the gridblock values with the averages - if there is any data to calculate them from
                                        if (YoungsMod_PresentDay_novalues > 0)
                                            local_YoungsMod_PresentDay = YoungsMod_PresentDay_total / (double)YoungsMod_PresentDay_novalues;
                                        if (PoissonsRatio_PresentDay_novalues > 0)
                                            local_PoissonsRatio_PresentDay = PoissonsRatio_PresentDay_total / (double)PoissonsRatio_PresentDay_novalues;
                                        if (BiotCoeff_novalues > 0)
                                            local_BiotCoefficient_PresentDay = BiotCoeff_total / (double)BiotCoeff_novalues;
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

                                        // Update Young's Modulus total if defined
                                        if (UseGridFor_YoungsMod_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_YoungsMod_PresentDay = (double)YoungsMod_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_YoungsMod_PresentDay)
                                                    cell_YoungsMod_PresentDay = toSIYoungsModUnits.Convert(cell_YoungsMod_PresentDay);
                                                if (!double.IsNaN(cell_YoungsMod_PresentDay))
                                                {
                                                    local_YoungsMod_PresentDay = cell_YoungsMod_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update Poisson's ratio total if defined
                                        if (UseGridFor_PoissonsRatio_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_PoissonsRatio_PresentDay = (double)PoissonsRatio_PresentDay_grid[cellRef];
                                                if (!double.IsNaN(cell_PoissonsRatio_PresentDay))
                                                {
                                                    local_PoissonsRatio_PresentDay = cell_PoissonsRatio_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update Biot coefficient total if defined
                                        if (UseGridFor_BiotCoefficient_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_BiotCoeff = (double)BiotCoefficient_PresentDay_grid[cellRef];
                                                if (!double.IsNaN(cell_BiotCoeff))
                                                {
                                                    local_BiotCoefficient_PresentDay = cell_BiotCoeff;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    // Check the elastic properties for physically unrealistic values, and if so warn the user
                                    // NB The code will actually generate a result with any input values except Young's Modulus = 0, Poisson's ratio = -1 or Poisson's ratio = 1
                                    // and these values will automatically be corrected by the MechanicalProperties object
                                    if (local_YoungsMod_PresentDay <= 0)
                                    {
                                        PetrelLogger.InfoOutputWindow(string.Format("Invalid value for present day Young's Modulus ({0}Pa) in cell {1},{2}. This will create errors in the calculation.", local_YoungsMod_PresentDay, PetrelGrid_FirstCellI + 1, maxJ - PetrelGrid_FirstCellJ + 1));
                                    }
                                    if ((local_PoissonsRatio_PresentDay < 0) || (local_PoissonsRatio_PresentDay > 0.5))
                                    {
                                        PetrelLogger.InfoOutputWindow(string.Format("Invalid value for present day Poisson's ratio ({0}) in cell {1},{2}. This will create errors in the calculation.", local_PoissonsRatio_PresentDay, PetrelGrid_FirstCellI + 1, maxJ - PetrelGrid_FirstCellJ + 1));
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
                                    double local_InitialStressRelaxation_PresentDay = InitialStressRelaxation_PresentDay;
                                    // If it is not defined, use the initial stress relaxation at the time of deformation
                                    if (double.IsNaN(local_InitialStressRelaxation_PresentDay))
                                        local_InitialStressRelaxation_PresentDay = local_InitialStressRelaxation;

                                    // Now we can set the present day stress
                                    gc.SetPresentDayStressFromStrain(local_Ehmin_PresentDay, local_Ehmax_PresentDay, local_EhminAzi_PresentDay, local_AppliedOverpressure_PresentDay, local_YoungsMod_PresentDay, local_PoissonsRatio_PresentDay, local_BiotCoefficient_PresentDay, local_InitialStressRelaxation_PresentDay);
#if DEBUG_FRAC_INPUT
                                                PetrelLogger.InfoOutputWindow("");
                                                PetrelLogger.InfoOutputWindow(string.Format("gc.SetPresentDayStressFromStrain({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_Ehmin_PresentDay, local_Ehmax_PresentDay, local_EhminAzi_PresentDay, local_AppliedOverpressure_PresentDay, local_YoungsMod_PresentDay, local_PoissonsRatio_PresentDay, local_BiotCoefficient_PresentDay, local_InitialStressRelaxation_PresentDay));
                                                PetrelLogger.InfoOutputWindow(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZX)));
#endif
                                }
                                break;
                            case StressStateDefinition.AbsoluteStress:
                            case StressStateDefinition.TerzaghiEffectiveStress:
                            case StressStateDefinition.BiotEffectiveStress:
                                {
                                    // Get the present day absolute stress and fluid pressure from the grid as required
                                    // This will depend on whether we are averaging the stress and fluid pressure properties over all Petrel cells that make up the gridblock, or taking the values from a single cell
                                    // First we will create local variables for the property values in this gridblock
                                    // By default these will be set to zero, since default values are not specified by the user 
                                    double local_Sxx_PresentDay = 0;
                                    double local_Syy_PresentDay = 0;
                                    double local_Szz_PresentDay = 0;
                                    double local_Sxy_PresentDay = 0;
                                    double local_Syz_PresentDay = 0;
                                    double local_Szx_PresentDay = 0;
                                    double local_FluidPressure_PresentDay = 0;

                                    if (AverageStressStrainData) // We are averaging over all Petrel cells in the gridblock
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
                                        for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                            for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                {
                                                    Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                    // Update XX stress component total if defined
                                                    if (UseGridFor_Sxx_PresentDay)
                                                    {
                                                        double cell_Sxx_PresentDay = (double)Sxx_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_Sxx_PresentDay)
                                                            cell_Sxx_PresentDay = toSIStressUnits.Convert(cell_Sxx_PresentDay);
                                                        if (!double.IsNaN(cell_Sxx_PresentDay))
                                                        {
                                                            Sxx_PresentDay_total += cell_Sxx_PresentDay;
                                                            Sxx_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update YY stress component total if defined
                                                    if (UseGridFor_Syy_PresentDay)
                                                    {
                                                        double cell_Syy_PresentDay = (double)Syy_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_Syy_PresentDay)
                                                            cell_Syy_PresentDay = toSIStressUnits.Convert(cell_Syy_PresentDay);
                                                        if (!double.IsNaN(cell_Syy_PresentDay))
                                                        {
                                                            Syy_PresentDay_total += cell_Syy_PresentDay;
                                                            Syy_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update ZZ stress component total if defined
                                                    if (UseGridFor_Szz_PresentDay)
                                                    {
                                                        double cell_Szz_PresentDay = (double)Szz_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_Szz_PresentDay)
                                                            cell_Szz_PresentDay = toSIStressUnits.Convert(cell_Szz_PresentDay);
                                                        if (!double.IsNaN(cell_Szz_PresentDay))
                                                        {
                                                            Szz_PresentDay_total += cell_Szz_PresentDay;
                                                            Szz_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update XY stress component total if defined
                                                    if (UseGridFor_Sxy_PresentDay)
                                                    {
                                                        double cell_Sxy_PresentDay = (double)Sxy_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_Sxy_PresentDay)
                                                            cell_Sxy_PresentDay = toSIStressUnits.Convert(cell_Sxy_PresentDay);
                                                        if (!double.IsNaN(cell_Sxy_PresentDay))
                                                        {
                                                            Sxy_PresentDay_total += cell_Sxy_PresentDay;
                                                            Sxy_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update YZ stress component total if defined
                                                    if (UseGridFor_Syz_PresentDay)
                                                    {
                                                        double cell_Syz_PresentDay = (double)Syz_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_Syz_PresentDay)
                                                            cell_Syz_PresentDay = toSIStressUnits.Convert(cell_Syz_PresentDay);
                                                        if (!double.IsNaN(cell_Syz_PresentDay))
                                                        {
                                                            Syz_PresentDay_total += cell_Syz_PresentDay;
                                                            Syz_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update ZX stress component total if defined
                                                    if (UseGridFor_Szx_PresentDay)
                                                    {
                                                        double cell_Szx_PresentDay = (double)Szx_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_Szx_PresentDay)
                                                            cell_Szx_PresentDay = toSIStressUnits.Convert(cell_Szx_PresentDay);
                                                        if (!double.IsNaN(cell_Szx_PresentDay))
                                                        {
                                                            Szx_PresentDay_total += cell_Szx_PresentDay;
                                                            Szx_PresentDay_novalues++;
                                                        }
                                                    }

                                                    // Update fluid pressure total if defined
                                                    if (UseGridFor_FluidPressure_PresentDay)
                                                    {
                                                        double cell_FluidPressure = (double)FluidPressure_PresentDay_grid[cellRef];
                                                        // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                        if (convertFromGeneral_FluidPressure_PresentDay)
                                                            cell_FluidPressure = toSIPressureUnits.Convert(cell_FluidPressure);
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

                                        // Update XX stress component total if defined
                                        if (UseGridFor_Sxx_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Sxx_PresentDay = (double)Sxx_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_Sxx_PresentDay)
                                                    cell_Sxx_PresentDay = toSIStressUnits.Convert(cell_Sxx_PresentDay);
                                                if (!double.IsNaN(cell_Sxx_PresentDay))
                                                {
                                                    local_Sxx_PresentDay = cell_Sxx_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update YY stress component total if defined
                                        if (UseGridFor_Syy_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Syy_PresentDay = (double)Syy_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_Syy_PresentDay)
                                                    cell_Syy_PresentDay = toSIStressUnits.Convert(cell_Syy_PresentDay);
                                                if (!double.IsNaN(cell_Syy_PresentDay))
                                                {
                                                    local_Syy_PresentDay = cell_Syy_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update ZZ stress component total if defined
                                        if (UseGridFor_Szz_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Szz_PresentDay = (double)Szz_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_Szz_PresentDay)
                                                    cell_Szz_PresentDay = toSIStressUnits.Convert(cell_Szz_PresentDay);
                                                if (!double.IsNaN(cell_Szz_PresentDay))
                                                {
                                                    local_Szz_PresentDay = cell_Szz_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update XY stress component total if defined
                                        if (UseGridFor_Sxy_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Sxy_PresentDay = (double)Sxy_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_Sxy_PresentDay)
                                                    cell_Sxy_PresentDay = toSIStressUnits.Convert(cell_Sxy_PresentDay);
                                                if (!double.IsNaN(cell_Sxy_PresentDay))
                                                {
                                                    local_Sxy_PresentDay = cell_Sxy_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update YZ stress component total if defined
                                        if (UseGridFor_Syz_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Syz_PresentDay = (double)Syz_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_Syz_PresentDay)
                                                    cell_Syz_PresentDay = toSIStressUnits.Convert(cell_Syz_PresentDay);
                                                if (!double.IsNaN(cell_Syz_PresentDay))
                                                {
                                                    local_Syz_PresentDay = cell_Syz_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update ZX stress component total if defined
                                        if (UseGridFor_Szx_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_Szx_PresentDay = (double)Szx_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_Szx_PresentDay)
                                                    cell_Szx_PresentDay = toSIStressUnits.Convert(cell_Szx_PresentDay);
                                                if (!double.IsNaN(cell_Szx_PresentDay))
                                                {
                                                    local_Szx_PresentDay = cell_Szx_PresentDay;
                                                    break;
                                                }
                                            }
                                        }

                                        // Update fluid pressure total if defined
                                        if (UseGridFor_FluidPressure_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_FluidPressure = (double)FluidPressure_PresentDay_grid[cellRef];
                                                // If the property has a General template, carry out unit conversion as if it was supplied in project units
                                                if (convertFromGeneral_FluidPressure_PresentDay)
                                                    cell_FluidPressure = toSIPressureUnits.Convert(cell_FluidPressure);
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
                                    double local_BiotCoefficient_PresentDay = BiotCoefficient_PresentDay;

                                    if (AverageMechanicalPropertyData) // We are averaging over all Petrel cells in the gridblock
                                    {
                                        // Create local variables for running total and number of datapoints for each mechanical property
                                        double BiotCoeff_total = 0;
                                        int BiotCoeff_novalues = 0;

                                        // Loop through all the Petrel cells in the gridblock
                                        for (int PetrelGrid_I = PetrelGrid_FirstCellI; PetrelGrid_I <= PetrelGrid_LastCellI; PetrelGrid_I++)
                                            for (int PetrelGrid_J = PetrelGrid_FirstCellJ; PetrelGrid_J <= PetrelGrid_LastCellJ; PetrelGrid_J++)
                                                for (int PetrelGrid_K = PetrelGrid_TopCellK; PetrelGrid_K <= PetrelGrid_BaseCellK; PetrelGrid_K++)
                                                {
                                                    Index3 cellRef = new Index3(PetrelGrid_I, PetrelGrid_J, PetrelGrid_K);

                                                    // Update Biot coefficient total if defined
                                                    if (UseGridFor_BiotCoefficient_PresentDay)
                                                    {
                                                        double cell_BiotCoeff = (double)BiotCoefficient_PresentDay_grid[cellRef];
                                                        if (!double.IsNaN(cell_BiotCoeff))
                                                        {
                                                            BiotCoeff_total += cell_BiotCoeff;
                                                            BiotCoeff_novalues++;
                                                        }
                                                    }

                                                }

                                        // Update the gridblock values with the averages - if there is any data to calculate them from
                                        if (BiotCoeff_novalues > 0)
                                            local_BiotCoefficient_PresentDay = BiotCoeff_total / (double)BiotCoeff_novalues;
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

                                        // Update Biot coefficient total if defined
                                        if (UseGridFor_BiotCoefficient_PresentDay)
                                        {
                                            // Loop through all cells in the stack, from the top down, until we find one that contains valid data
                                            for (int PetrelGrid_DataCellK = PetrelGrid_TopCellK; PetrelGrid_DataCellK <= PetrelGrid_BaseCellK; PetrelGrid_DataCellK++)
                                            {
                                                cellRef.K = PetrelGrid_DataCellK;
                                                double cell_BiotCoeff = (double)BiotCoefficient_PresentDay_grid[cellRef];
                                                if (!double.IsNaN(cell_BiotCoeff))
                                                {
                                                    local_BiotCoefficient_PresentDay = cell_BiotCoeff;
                                                    break;
                                                }
                                            }
                                        }
                                    } // End get the present day mechanical properties from the grid as required


                                    // Now we can set the present day stress, depending on the stress type selected
                                    if (PresentDayStressInput == StressStateDefinition.AbsoluteStress)
                                    {
                                        gc.SetPresentDayAbsoluteStress(local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay);
#if DEBUG_FRAC_INPUT
                                                    PetrelLogger.InfoOutputWindow("");
                                                    PetrelLogger.InfoOutputWindow(string.Format("gc.SetPresentDayAbsoluteStress({0}, {1}, {2}, {3}, {4}, {5}, {6});", local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay));
                                                    PetrelLogger.InfoOutputWindow(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZZ)));
#endif
                                    }
                                    else if (PresentDayStressInput == StressStateDefinition.TerzaghiEffectiveStress)
                                    {
                                        gc.SetPresentDayTerzaghiStress(local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay);
#if DEBUG_FRAC_INPUT
                                                    PetrelLogger.InfoOutputWindow("");
                                                    PetrelLogger.InfoOutputWindow(string.Format("gc.SetPresentDayTerzaghiStress({0}, {1}, {2}, {3}, {4}, {5});", local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay));
                                                    PetrelLogger.InfoOutputWindow(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZZ)));
#endif
                                    }
                                    else if (PresentDayStressInput == StressStateDefinition.BiotEffectiveStress)
                                    {
                                        gc.SetPresentDayBiotStress(local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay, local_BiotCoefficient_PresentDay);
#if DEBUG_FRAC_INPUT
                                                    PetrelLogger.InfoOutputWindow("");
                                                    PetrelLogger.InfoOutputWindow(string.Format("gc.SetPresentDayBiotStress({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_Sxx_PresentDay, local_Syy_PresentDay, local_Szz_PresentDay, local_Sxy_PresentDay, local_Syz_PresentDay, local_Szx_PresentDay, local_FluidPressure_PresentDay, local_BiotCoefficient_PresentDay));
                                                    PetrelLogger.InfoOutputWindow(string.Format("Present day stress tensor is (XX: {0}, YY: {1}, ZZ: {2}, XY: {3}, YZ: {4}, ZX: {5})", gc.PresentDayStress.Component(Tensor2SComponents.XX), gc.PresentDayStress.Component(Tensor2SComponents.YY), gc.PresentDayStress.Component(Tensor2SComponents.ZZ), gc.PresentDayStress.Component(Tensor2SComponents.XY), gc.PresentDayStress.Component(Tensor2SComponents.YZ), gc.PresentDayStress.Component(Tensor2SComponents.ZZ)));
#endif
                                    }
                                }
                                break;
                            default:
                                {
#if DEBUG_FRAC_INPUT
                                                PetrelLogger.InfoOutputWindow("");
                                                PetrelLogger.InfoOutputWindow(string.Format("Not setting present day stress"));
#endif
                                }
                                break;
                        }
                    }

                    // Set the fracture aperture control data
                    gc.SetFractureApertureControlData(Mode1HMin_UniformAperture, Mode2HMin_UniformAperture, Mode1HMax_UniformAperture, Mode2HMax_UniformAperture, Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier);
#if DEBUG_FRAC_INPUT
                                PetrelLogger.InfoOutputWindow(string.Format("gc.SetFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", Mode1HMin_UniformAperture, Mode2HMin_UniformAperture, Mode1HMax_UniformAperture, Mode2HMax_UniformAperture, Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier));
#endif

                    // Add the gridblock to the grid
                    ModelGrid.AddGridblock(gc, FractureGrid_RowNo, FractureGrid_ColNo, !faultToWest, !faultToSouth, true, true);

#if DEBUG_FRAC_INPUT
                                PetrelLogger.InfoOutputWindow(string.Format("ModelGrid.AddGridblock(gc, {0}, {1}, {2}, {3}, {4}, {5});", FractureGrid_RowNo, FractureGrid_ColNo, !faultToWest, !faultToSouth, true, true));
#endif

                    // Update gridblock counter
                    NoActiveGridblocks++;

                    //Update status bar
                    progressReporter.UpdateProgress(NoActiveGridblocks);

                } // End loop through all rows in the Fracture Grid
            } // End loop through all columns in the Fracture Grid

            // Set the DFN generation data
            DFNGenerationControl dfn_control = new DFNGenerationControl(GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, -1, MaximumNewFracturesPerTimestep, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, IntermediateOutputIntervalControl, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchAdjacentGridblocks, PropagateFracturesInNucleationOrder, ModelTimeUnits);

#if DEBUG_FRAC_INPUT
                        PetrelLogger.InfoOutputWindow("");
                        PetrelLogger.InfoOutputWindow(string.Format("DFNGenerationControl dfn_control = new DFNGenerationControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, DFNFileType.{13}, {14}, {15}, {16}, {18}, TimeUnits.{18});", GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, -1, MaximumNewFracturesPerTimestep, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, IntermediateOutputIntervalControl, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchAdjacentGridblocks, PropagateFracturesInNucleationOrder, ModelTimeUnits));
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
                for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDefinedDeformationEpisodes; deformationEpisodeNo++)
                {
                    bool finalEpisode = (deformationEpisodeNo == (noDefinedDeformationEpisodes - 1));
                    double TimeUnitConverter = TimeUnitConverter_list[deformationEpisodeNo];
                    if (SubEpisodesDefined_list[deformationEpisodeNo])
                    {
                        List<double> subEpisodeDurations_GeologicalTime = SubEpisodeDurations_GeologicalTimeUnits_list[deformationEpisodeNo];
                        List<string> subEpisodeNameOverrides = SubEpisodeNameOverride_list[deformationEpisodeNo];
                        int noSubEpisodes = subEpisodeDurations_GeologicalTime.Count;
                        for (int subEpisodeNo = 0; subEpisodeNo < noSubEpisodes; subEpisodeNo++)
                        {
                            double subEpisodeDuration_GeologicalTimeUnit = subEpisodeDurations_GeologicalTime[subEpisodeNo];
                            if (subEpisodeDuration_GeologicalTimeUnit > 0)
                            {
                                currentEpisodeEndTime += (subEpisodeDuration_GeologicalTimeUnit * TimeUnitConverter);
                                DeformationEpisodeEndTimes_SITimeUnits_list.Add(currentEpisodeEndTime);
                                OutputStageNameOverride.Add(subEpisodeNameOverrides[subEpisodeNo]);
                                NoIntermediateOutputs++;
                            }
                        }
                    }
                    else
                    {
                        double deformationEpisodeDuration_GeologicalTimeUnit = DeformationEpisodeDuration_GeologicalTimeUnits_list[deformationEpisodeNo];
                        // Intermediate deformation episodes will only be added to the list if they have a defined duration
                        // The final deformation episode will be added to the list even if the duration is undefined
                        if ((deformationEpisodeDuration_GeologicalTimeUnit > 0) || finalEpisode)
                        {
                            currentEpisodeEndTime += (deformationEpisodeDuration_GeologicalTimeUnit * TimeUnitConverter);
                            DeformationEpisodeEndTimes_SITimeUnits_list.Add(currentEpisodeEndTime);
                            if (deformationEpisodeNo < DeformationEpisodeName_list.Count)
                                OutputStageNameOverride.Add(DeformationEpisodeName_list[deformationEpisodeNo]);
                            else
                                OutputStageNameOverride.Add(null);
                            NoIntermediateOutputs++;
                        }
                        else
                        {
                            PetrelLogger.InfoOutputWindow(string.Format("Duration is undefined for deformation episode {0}. This may cause errors in calculating the timing of intermediate outputs.", deformationEpisodeNo + 1));
                            PetrelLogger.InfoOutputWindow(string.Format("Duration should be defined for all deformation episodes except the final episode, which can have undefined duration (run to fracture saturation)."));
                        }
                    }
                }
                dfn_control.IntermediateOutputTimes = DeformationEpisodeEndTimes_SITimeUnits_list;

#if DEBUG_FRAC_INPUT
                            string setIntermediateTimes = "dfn_control.IntermediateOutputTimes = {";
                            foreach (double nextEndTime in DeformationEpisodeEndTimes_SITimeUnits_list)
                                setIntermediateTimes += string.Format(" {0},", nextEndTime);
                            setIntermediateTimes = setIntermediateTimes.TrimEnd(',');
                            setIntermediateTimes += " }";
                            PetrelLogger.InfoOutputWindow(setIntermediateTimes);
#endif
            }

            // Set the output folder path
            dfn_control.FolderPath = folderPath;

            // Add the DFNGenerationControl object to the grid
            ModelGrid.DFNControl = dfn_control;

#if DEBUG_FRAC_INPUT
                        PetrelLogger.InfoOutputWindow(string.Format("dfn_control.FolderPath = {0};", folderPath));
                        PetrelLogger.InfoOutputWindow(string.Format("ModelGrid.DFNControl = dfn_control;"));
#endif

            // Calculate implicit fractures for all gridblocks - unless the calculation has already been cancelled
            if (!progressReporter.abortCalculation())
            {
                PetrelLogger.InfoOutputWindow("Start calculating implicit data");
                progressBar.SetProgressText("Calculating implicit data");
                ModelGrid.CalculateAllFractureData(progressReporter);
            }

            // Calculate explicit DFN - unless the calculation has already been cancelled
            if (!progressReporter.abortCalculation())
            {
                PetrelLogger.InfoOutputWindow("Start generating explicit DFN");
                progressBar.SetProgressText("Generating explicit DFN");
                ModelGrid.GenerateDFN(progressReporter);
            }


        }
    }
}
