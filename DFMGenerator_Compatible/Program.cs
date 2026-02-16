// Switch this flag off to use hardcoded values for all parameters
// This should be done for debugging only
// The flag should be set to generate release versions of the standalone code
//#define READINPUTFROMFILE

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

        }
    }
}
