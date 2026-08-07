// Switch this flag off to use hardcoded values for all parameters
// This should be done for debugging only
// The flag should be set to generate release versions of the standalone code
//#define READINPUTFROMFILE
// Set this flag to run test models with hardcoded values for unconfined fractures
#define TESTUCF
// Set this flag to output detailed information on input parameters and properties for each gridblock
// Use for debugging only; will significantly increase runtime 
//#define DEBUG_FRACS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DFMGenerator_SharedCode;

namespace DFMGenerator_Standalone
{
    // Enumerators used throughout code

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
                input_file.WriteLine("% Grid size");
                input_file.WriteLine("NoCols 3");
                input_file.WriteLine("NoRows 3");
                input_file.WriteLine("NoLayers 1");
                input_file.WriteLine("% Gridblock size; all lengths in metres");
                input_file.WriteLine("Width_EW 50");
                input_file.WriteLine("Length_NS 50");
                input_file.WriteLine("LayerThickness 1");
                input_file.WriteLine("% Model location");
                input_file.WriteLine("% Use the origin offset to set the absolute XY coordinates of the SW corner of the bottom left gridblock");
                input_file.WriteLine("OriginXOffset 0");
                input_file.WriteLine("OriginYOffset 0");
                input_file.WriteLine("% Current depth of burial of the top surface in metres, positive downwards");
                input_file.WriteLine("Depth 2000");
                input_file.WriteLine("% Time units used in input load rates, time limits and strain relaxation time constants");
                input_file.WriteLine("% Set time units to ma, year or second");
                input_file.WriteLine("ModelTimeUnits ma");
                input_file.WriteLine("% Deformation load");
                input_file.WriteLine("% Minimum strain orientatation (i.e. direction of maximum extension) in radians, clockwise from North");
                input_file.WriteLine("EhminAzi 0");
                input_file.WriteLine("% Set VariableStrainOrientation false to have the same minimum strain orientatation in all cells");
                input_file.WriteLine("% Set VariableStrainOrientation true to have laterally variable strain orientation controlled by EhminAzi and EhminCurvature");
                input_file.WriteLine("% EhminCurvature is the difference in strain orientation between adjacent blocks in radians");
                input_file.WriteLine("VariableStrainOrientation false");
                input_file.WriteLine("EhminCurvature 0.1963495");
                input_file.WriteLine("% Strain rates; units determined by ModelTimeUnits (i.e. strain/s, strain/year or strain/ma)");
                input_file.WriteLine("% Set to negative value for extensional strain - this is necessary in at least one direction to generate fractures");
                input_file.WriteLine("% With no strain relaxation, strain rate will control rate of horizontal stress increase");
                input_file.WriteLine("% With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant horizontal stress");
                input_file.WriteLine("% EhminRate is most tensile (i.e. most negative) horizontal strain rate");
                input_file.WriteLine("EhminRate -0.01");
                input_file.WriteLine("% Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern");
                input_file.WriteLine("EhmaxRate 0");
                input_file.WriteLine("% Set VariableStrainMagnitude to add random variation to the input strain rates");
                input_file.WriteLine("% Strain rates for each gridblock will vary randomly from 0 to 2x specified values");
                input_file.WriteLine("VariableStrainMagnitude false");
                input_file.WriteLine("% Fluid pressure, thermal and uplift loads");
                input_file.WriteLine("% Rate of increase of fluid overpressure (Pa/ModelTimeUnit)");
                input_file.WriteLine("AppliedOverpressureRate 0");
                input_file.WriteLine("% Rate of in situ temperature change (not including cooling due to uplift) (degK/ModelTimeUnit)");
                input_file.WriteLine("AppliedTemperatureChange 0");
                input_file.WriteLine("% Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/ModelTimeUnit)");
                input_file.WriteLine("AppliedUpliftRate 0");
                input_file.WriteLine("% Proportion of vertical stress due to fluid pressure and thermal loads accommodated by stress arching: set to 0 for no stress arching (dsigma_v = 0) or 1 for complete stress arching (dsigma_v = dsigma_h)");
                input_file.WriteLine("StressArchingFactor 0");
                input_file.WriteLine("% Duration of the deformation episode; set to -1 to continue until fracture saturation is reached");
                input_file.WriteLine("% Units are determined by ModelTimeUnits setting");
                input_file.WriteLine("DeformationEpisodeDuration -1");
                input_file.WriteLine("% To add additional deformation episodes, list multiple values after the deformation load keywords");
                input_file.WriteLine("% For example, to model a 1ma episode of NE-oriented extensional strain, followed by uplift and erosion of 2000m over 20ma, ");
                input_file.WriteLine("% followed by an episode of overpressure and cooling (e.g. due to injection of cold fluid for 10 years), use the following");
                input_file.WriteLine("%   EhminAzi 0 0.7853 0");
                input_file.WriteLine("%   EhminRate -0.01 0 0");
                input_file.WriteLine("%   EhmaxRate 0 0 0");
                input_file.WriteLine("%   AppliedOverpressureRate 0 0 1E+12");
                input_file.WriteLine("%   AppliedTemperatureChange 0 0 -5E+6");
                input_file.WriteLine("%   AppliedUpliftRate 0 100 0");
                input_file.WriteLine("%   StressArchingFactor 0 0 1");
                input_file.WriteLine("%   DeformationEpisodeDuration 1 20 1E-5");
                input_file.WriteLine();

                input_file.WriteLine("% Mechanical properties");
                input_file.WriteLine("% Young's Modulus in Pa");
                input_file.WriteLine("YoungsMod 1E+10");
                input_file.WriteLine("% Set VariableYoungsMod true to have laterally variable Young's Modulus");
                input_file.WriteLine("VariableYoungsMod false");
                input_file.WriteLine("PoissonsRatio 0.25");
                input_file.WriteLine("Porosity 0.2");
                input_file.WriteLine("BiotCoefficient 1");
                input_file.WriteLine("% Thermal expansion coefficient typically 3E-5/degK for sandstone, 4E-5/degK for shale (Miller 1995)");
                input_file.WriteLine("ThermalExpansionCoefficient 4E-5");
                input_file.WriteLine("% Crack surface energy in J/m2");
                input_file.WriteLine("CrackSurfaceEnergy 1000");
                input_file.WriteLine("% Set VariableCSE true to have laterally variable crack surface energy");
                input_file.WriteLine("VariableCSE false");
                input_file.WriteLine("FrictionCoefficient 0.5");
                input_file.WriteLine("% Set VariableFriction true to have laterally variable friction coefficient");
                input_file.WriteLine("VariableFriction false");
                input_file.WriteLine("% Strain relaxation time constants");
                input_file.WriteLine("% Units are determined by ModelTimeUnits setting");
                input_file.WriteLine("% Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate");
                input_file.WriteLine("RockStrainRelaxation 0");
                input_file.WriteLine("% Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only");
                input_file.WriteLine("FractureRelaxation 0");
                input_file.WriteLine("% Initial microfracture distribution function");
                input_file.WriteLine("% Set to PowerLaw, Exponential, or LogNormal");
                input_file.WriteLine("% NB This is currently only used for unconfined fractures; for layer-bound fractures it is assumed to be power law");
                input_file.WriteLine("InitialMicrofractureDistributionFunction PowerLaw");
                input_file.WriteLine("% Density of initial microfractures");
                input_file.WriteLine("InitialMicrofractureDensity 0.001");
                input_file.WriteLine("% Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures");
                input_file.WriteLine("% This will be used as the S parameter (standard deviation) for the log-normal distribution");
                input_file.WriteLine("InitialMicrofractureSizeDistribution 3");
                input_file.WriteLine("% Median initial microfracture radius - this is only used for the log-normal distribution");
                input_file.WriteLine("% Set to -1 to use layer thickness / 20");
                input_file.WriteLine("InitialMicrofractureMedianRadius -1");
                input_file.WriteLine("% Subcritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation");
                input_file.WriteLine("SubcriticalPropIndex 10");
                input_file.WriteLine("% Critical fracture propagation rate in m/s");
                input_file.WriteLine("CriticalPropagationRate 2000");
                input_file.WriteLine("% Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity");
                input_file.WriteLine("% NB Permeability must be given in m^2; 1mD = 9.869233E-16m^2");
                input_file.WriteLine("HostRock_kh 0");
                input_file.WriteLine("HostRock_kv 0");
                input_file.WriteLine();

                input_file.WriteLine("% Cleavages: Cleavage planes reduce the crack surface energy and/or friction coefficient on fractures parallel to the specified cleavage orientation");
                input_file.WriteLine("% Multiple cleavage planes can be defined; for each one, the crack surface energy and/or friction coefficient on the fracture set with the closest orientation (within the specified MaxConsistencyAngle) will be modified accordingly");
                input_file.WriteLine("% If there is no fracture set within the specified MaxConsistencyAngle, the cleavage plane will have no effect");
                input_file.WriteLine("% To specify a cleavage plane, use the keyword Cleavage then specify the cleavage azimuth and dip (radians), and the crack surface energy (J/m2) and sliding friction coefficient parallel to the cleavage plane, in order");
                input_file.WriteLine("% E.g. to specify a North-dipping inclined cleavage plane with crack surface energy 500J/m2 and sliding friction coefficient 0.4, use:");
                input_file.WriteLine("%Cleavage 0 1.05 500 0.4");
                input_file.WriteLine();

                input_file.WriteLine("% Stress state");
                input_file.WriteLine("% Stress distribution scenario - use to turn on or off stress shadow effect");
                input_file.WriteLine("% Options are EvenlyDistributedStress or StressShadow");
                input_file.WriteLine("% Do not use DuctileBoundary as this is not yet implemented");
                input_file.WriteLine("StressDistributionScenario StressShadow");
                input_file.WriteLine("% Depth at the start of deformation (in metres, positive downwards) - this will control stress state");
                input_file.WriteLine("% If DepthAtDeformation is specified, this will be used to calculate the initial vertical stress");
                input_file.WriteLine("% If DepthAtDeformation is <=0 or NaN, the depth at the start of deformation will be set to the current depth plus total specified uplift");
                input_file.WriteLine("DepthAtDeformation -1");
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
                input_file.WriteLine("% Flag to output the layer-bound fracture centrepoints and the unconfined fracture rays as polylines");
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
                input_file.WriteLine("% Number of fracture or ray length values to calculate for each of the implicit fracture population distribution functions");
                input_file.WriteLine("No_l_indexPoints 20");
                input_file.WriteLine("% MaxHMinLength and MaxHMaxLength control the range of fracture or ray lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively");
                input_file.WriteLine("% Set these values to the approximate maximum length of fractures generated (in metres), or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length");
                input_file.WriteLine("MaxHMinLength 0");
                input_file.WriteLine("MaxHMaxLength 0");
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
                input_file.WriteLine("% Flag to use present day effective stress tensor, instead of stress at the time of deformation, to calculate fracture aperture and permeability");
                input_file.WriteLine("UsePresentDayStress false");
                input_file.WriteLine("% Present Terzaghi effective day stress tensor components (Pa)");
                input_file.WriteLine("% These will override the stress at the time of deformation when calculating fracture aperture and permeability");
                input_file.WriteLine("% Any undefined components will be set to 0");
                input_file.WriteLine("PresentDayEffectiveStress_XX 15000000");
                input_file.WriteLine("PresentDayEffectiveStress_YY 15000000");
                input_file.WriteLine("PresentDayEffectiveStress_ZZ 25000000");
                input_file.WriteLine("PresentDayEffectiveStress_XY 0");
                input_file.WriteLine("PresentDayEffectiveStress_YZ 0");
                input_file.WriteLine("PresentDayEffectiveStress_ZX 0");
                input_file.WriteLine();

                input_file.WriteLine("% Calculation control parameters");
                input_file.WriteLine("% Number of layer-bound fracture sets");
                input_file.WriteLine("% Set to 1 to generate a single fracture set, perpendicular to ehmin");
                input_file.WriteLine("% Set to 2 to generate two orthogonal fracture sets, perpendicular to the minimum and maximum horizontal strain directions; this is typical of a single stage of tectonic deformation in intact rock");
                input_file.WriteLine("% Set to 6 to model polygonal or strike-slip fractures, or multiple deformation episodes where there are pre-existing fractures oblique to the principal horizontal stresses");
                input_file.WriteLine("NoLayerBoundFractureSets 2");
                input_file.WriteLine("% Layer-bound fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal");
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
                input_file.WriteLine("% Minimum required clear zone volume in which layer-bound fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated");
                input_file.WriteLine("MinimumMFClearZoneVolume 0.01");
                input_file.WriteLine("% Use the deformation episode duration (set in the deformation load inputs) or the maximum timestep limit to stop the calculation before fractures have finished growing");
                input_file.WriteLine("MaxTimesteps 1000");
                input_file.WriteLine("% DFN geometry controls");
                input_file.WriteLine("% Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated");
                input_file.WriteLine("GenerateExplicitDFN true");
                input_file.WriteLine("% Flag to calculate implicit data for unconfined fractures; if set to false no grid properties will be generated, only an explicit DFN; does not affect layer-bound fractures");
                input_file.WriteLine("CalculateImplicitUCFData true");
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
                input_file.WriteLine("SearchAdjacentGridblocks Automatic");
                input_file.WriteLine("% Minimum radius for microfractures to be included in explicit DFN (in metres)");
                input_file.WriteLine("% Set this to 0 to exclude microfractures from DFN; set to between 0 and half layer thickness to include larger microfractures in the DFN");
                input_file.WriteLine("MinExplicitMicrofractureRadius 0");
                input_file.WriteLine("% Number of cornerpoints defining the microfracture polygons in the explicit DFN");
                input_file.WriteLine("% Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints");
                input_file.WriteLine("Number_uF_Points 8");
                input_file.WriteLine("% Maximum number of fracture segments that can be generated per gridblock");
                input_file.WriteLine("% Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason");
                input_file.WriteLine("MaxNoFractureSegments 1000");
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
                input_file.WriteLine();

                input_file.WriteLine("% Add property and geometry overrides for individual gridblocks here");
                input_file.WriteLine("% Overrides for individual gridblocks should be nested between a Gridblock Col Row statement and an End Gridblock statement");
                input_file.WriteLine("% E.g. to override the properties in the gridblock in column 1 row 2 layer 1, use:");
                input_file.WriteLine("% Gridblock 1 2 1");
                input_file.WriteLine("%   PropertyA ValueA");
                input_file.WriteLine("%   PropertyB ValueB");
                input_file.WriteLine("%   CornerpointA Xcoord Ycoord Zcoord");
                input_file.WriteLine("%   CornerpointB Xcoord Ycoord Zcoord");
                input_file.WriteLine("% End Gridblock");
                input_file.WriteLine("% Properties that can be overridden are EhminAzi, EhminRate, EhmaxRate, AppliedOverpressureRate, AppliedTemperatureChange, AppliedUpliftRate, DepthAtDeformation, ");
                input_file.WriteLine("% YoungsMod, PoissonsRatio, Porosity, BiotCoefficient, GeothermalGradient, FrictionCoefficient, CrackSurfaceEnergy,");
                input_file.WriteLine("% SubcriticalPropIndex, RockStrainRelaxation, FractureRelaxation, InitialMicrofractureDensity, InitialMicrofractureSizeDistribution, InitialMicrofractureMedianRadius");
                input_file.WriteLine("% HostRock_kh, HostRock_kv");
                input_file.WriteLine("% PresentDayEffectiveStress_XX, PresentDayEffectiveStress_YY, PresentDayEffectiveStress_ZZ, PresentDayEffectiveStress_XY, PresentDayEffectiveStress_YZ, PresentDayEffectiveStress_ZX");
                input_file.WriteLine("% Additional deformation episodes can be overwritten by listing multiple values after the deformation load keywords");
                input_file.WriteLine("% Cornerpoints that can be overridden are SETopCorner, SEBottomCorner, NETopCorner, NEBottomCorner,");
                input_file.WriteLine("% NWTopCorner, NWBottomCorner, SWTopCorner, SWBottomCorner");
                input_file.WriteLine("% Z coordinates should be specified positive downwards");
                input_file.WriteLine("% NB the cornerpoints of adjacent gridblocks will automatically be adjusted when a gridblock cornerpoint is overridden,");
                input_file.WriteLine("% but there is no sanity check to ensure the resulting grid geometry is consistent (e.g. checking for negative gridblock volumes, etc.); this must be done beforehand");
                input_file.WriteLine();

                input_file.WriteLine("% Overrides for individual properties can be done by specifying Include files, using the statement:");
                input_file.WriteLine("% Include Filename");
                input_file.WriteLine("% The Include file should follow the format");
                input_file.WriteLine("% #PropertyA");
                input_file.WriteLine("% Gridblock_1_1_1_value Gridblock_2_1_1_value Gridblock_3_1_1_value");
                input_file.WriteLine("% Gridblock_1_2_1_value Gridblock_2_2_1_value Gridblock_3_2_1_value");
                input_file.WriteLine("% Gridblock_1_3_1_value Gridblock_2_3_1_value Gridblock_3_3_1_value");
                input_file.WriteLine("% Gridblock_1_1_2_value Gridblock_2_1_2_value Gridblock_3_1_2_value");
                input_file.WriteLine("% Gridblock_1_2_2_value Gridblock_2_2_2_value Gridblock_3_2_2_value");
                input_file.WriteLine("% Gridblock_1_3_2_value Gridblock_2_3_2_value Gridblock_3_3_2_value");
                input_file.WriteLine("% #PropertyB");
                input_file.WriteLine("% Gridblock_1_1_1_value Gridblock_2_1_1_value Gridblock_3_1_1_value");
                input_file.WriteLine("% Gridblock_1_2_1_value Gridblock_2_2_1_value Gridblock_3_2_1_value");
                input_file.WriteLine("% Gridblock_1_3_1_value Gridblock_2_3_1_value Gridblock_3_3_1_value");
                input_file.WriteLine("% NA NA NA");
                input_file.WriteLine("% NA NA NA");
                input_file.WriteLine("% NA NA NA");
                input_file.WriteLine("% i.e. in the geometric sequence");
                input_file.WriteLine("%      *---*---*---*");
                input_file.WriteLine("%     /| |/| |/| |/|");
                input_file.WriteLine("%    *-|-*-|-*-|-* |");
                input_file.WriteLine("%   /| |/| |/| |/| |");
                input_file.WriteLine("%  *---*---*---* | |");
                input_file.WriteLine("%  | | 10| 11| 12| |");
                input_file.WriteLine("%  | | | | | | | | |");
                input_file.WriteLine("%  | |7*-|8*-|9*-|-*");
                input_file.WriteLine("%  | |/| |/| |/| |/|");
                input_file.WriteLine("%  | *-|-*-|-*-|-* |");
                input_file.WriteLine("%  |/| |/| |/| |/| |");
                input_file.WriteLine("%  *---*---*---* | |");
                input_file.WriteLine("%  | | |4| |5| |6| |");
                input_file.WriteLine("%  | | | | | | | | |");
                input_file.WriteLine("%  | |1*-|2*-|3*-|-*");
                input_file.WriteLine("%  | |/| |/| |/| |/");
                input_file.WriteLine("%  | *-|-*-|-*-|-*");
                input_file.WriteLine("%  |/  |/  |/  |/");
                input_file.WriteLine("%  *---*---*---*");
                input_file.WriteLine("% Include files can include values for multiple properties, in separate blocks; any property in the list above can be overridden");
                input_file.WriteLine("% Each new property must start on a new line, but within each property block, the values can be separated by either spaces or line returns; the layout of the data is not significant");
                input_file.WriteLine("% However the values must be given in order shown above, looping first through rows and then through columns");
                input_file.WriteLine("% Use NA instead of a specifying a value to revert to the default value for a specific gridblock (as has been done for PropertyB in the gridblocks in layer 2 in the example above)");
                input_file.WriteLine("% To override additional deformation episodes, add an index number in square brackets after the deformation load property name, but before the values, e.g.:");
                input_file.WriteLine("% #AppliedUpliftRate [2] Gridblock_1_1_1_value Gridblock_2_1_1_value Gridblock_3_1_1_value etc");
                input_file.WriteLine("% ");
                input_file.WriteLine("% Overrides for geometry can be also done using Include files");
                input_file.WriteLine("% However in this case the Include file format is slightly different:");
                input_file.WriteLine("% #Geometry");
                input_file.WriteLine("% Gridblock_1_1_1_SWBottomCornerpoint_X Gridblock_1_1_1_SWBottomCornerpoint_Y Gridblock_1_1_1_SWBottomCornerpoint_Z Gridblock_1_1_1_SEBottomCornerpoint_X Gridblock_1_1_1_SEBottomCornerpoint_Y Gridblock_1_1_1_SEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_1_1_SEBottomCornerpoint_X Gridblock_2_1_1_SEBottomCornerpoint_Y Gridblock_2_1_1_SEBottomCornerpoint_Z Gridblock_3_1_1_SEBottomCornerpoint_X Gridblock_3_1_1_SEBottomCornerpoint_Y Gridblock_3_1_1_SEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_1_1_NWBottomCornerpoint_X Gridblock_1_1_1_NWBottomCornerpoint_Y Gridblock_1_1_1_NWBottomCornerpoint_Z Gridblock_1_1_1_NEBottomCornerpoint_X Gridblock_1_1_1_NEBottomCornerpoint_Y Gridblock_1_1_1_NEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_1_1_NEBottomCornerpoint_X Gridblock_2_1_1_NEBottomCornerpoint_Y Gridblock_2_1_1_NEBottomCornerpoint_Z Gridblock_3_1_1_NEBottomCornerpoint_X Gridblock_3_1_1_NEBottomCornerpoint_Y Gridblock_3_1_1_NEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_2_1_NWBottomCornerpoint_X Gridblock_1_2_1_NWBottomCornerpoint_Y Gridblock_1_2_1_NWBottomCornerpoint_Z Gridblock_1_2_1_NEBottomCornerpoint_X Gridblock_1_2_1_NEBottomCornerpoint_Y Gridblock_1_2_1_NEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_2_1_NEBottomCornerpoint_X Gridblock_2_2_1_NEBottomCornerpoint_Y Gridblock_2_2_1_NEBottomCornerpoint_Z Gridblock_3_2_1_NEBottomCornerpoint_X Gridblock_3_2_1_NEBottomCornerpoint_Y Gridblock_3_2_1_NEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_3_1_NWBottomCornerpoint_X Gridblock_1_3_1_NWBottomCornerpoint_Y Gridblock_1_3_1_NWBottomCornerpoint_Z Gridblock_1_3_1_NEBottomCornerpoint_X Gridblock_1_3_1_NEBottomCornerpoint_Y Gridblock_1_3_1_NEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_3_1_NEBottomCornerpoint_X Gridblock_2_3_1_NEBottomCornerpoint_Y Gridblock_2_3_1_NEBottomCornerpoint_Z Gridblock_3_3_1_NEBottomCornerpoint_X Gridblock_3_3_1_NEBottomCornerpoint_Y Gridblock_3_3_1_NEBottomCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_1_1_SWTopCornerpoint_X Gridblock_1_1_1_SWTopCornerpoint_Y Gridblock_1_1_1_SWTopCornerpoint_Z Gridblock_1_1_1_SETopCornerpoint_X Gridblock_1_1_1_SETopCornerpoint_Y Gridblock_1_1_1_SETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_1_1_SETopCornerpoint_X Gridblock_2_1_1_SETopCornerpoint_Y Gridblock_2_1_1_SETopCornerpoint_Z Gridblock_3_1_1_SETopCornerpoint_X Gridblock_3_1_1_SETopCornerpoint_Y Gridblock_3_1_1_SETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_1_1_NWTopCornerpoint_X Gridblock_1_1_1_NWTopCornerpoint_Y Gridblock_1_1_1_NWTopCornerpoint_Z Gridblock_1_1_1_NETopCornerpoint_X Gridblock_1_1_1_NETopCornerpoint_Y Gridblock_1_1_1_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_1_1_NETopCornerpoint_X Gridblock_2_1_1_NETopCornerpoint_Y Gridblock_2_1_1_NETopCornerpoint_Z Gridblock_3_1_1_NETopCornerpoint_X Gridblock_3_1_1_NETopCornerpoint_Y Gridblock_3_1_1_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_2_1_NWTopCornerpoint_X Gridblock_1_2_1_NWTopCornerpoint_Y Gridblock_1_2_1_NWTopCornerpoint_Z Gridblock_1_2_1_NETopCornerpoint_X Gridblock_1_2_1_NETopCornerpoint_Y Gridblock_1_2_1_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_2_1_NETopCornerpoint_X Gridblock_2_2_1_NETopCornerpoint_Y Gridblock_2_2_1_NETopCornerpoint_Z Gridblock_3_2_1_NETopCornerpoint_X Gridblock_3_2_1_NETopCornerpoint_Y Gridblock_3_2_1_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_3_1_NWTopCornerpoint_X Gridblock_1_3_1_NWTopCornerpoint_Y Gridblock_1_3_1_NWTopCornerpoint_Z Gridblock_1_3_1_NETopCornerpoint_X Gridblock_1_3_1_NETopCornerpoint_Y Gridblock_1_3_1_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_3_1_NETopCornerpoint_X Gridblock_2_3_1_NETopCornerpoint_Y Gridblock_2_3_1_NETopCornerpoint_Z Gridblock_3_3_1_NETopCornerpoint_X Gridblock_3_3_1_NETopCornerpoint_Y Gridblock_3_3_1_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_1_2_SWTopCornerpoint_X Gridblock_1_1_2_SWTopCornerpoint_Y Gridblock_1_1_2_SWTopCornerpoint_Z Gridblock_1_1_2_SETopCornerpoint_X Gridblock_1_1_2_SETopCornerpoint_Y Gridblock_1_1_2_SETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_1_2_SETopCornerpoint_X Gridblock_2_1_2_SETopCornerpoint_Y Gridblock_2_1_2_SETopCornerpoint_Z Gridblock_3_1_2_SETopCornerpoint_X Gridblock_3_1_2_SETopCornerpoint_Y Gridblock_3_1_2_SETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_1_2_NWTopCornerpoint_X Gridblock_1_1_2_NWTopCornerpoint_Y Gridblock_1_1_2_NWTopCornerpoint_Z Gridblock_1_1_2_NETopCornerpoint_X Gridblock_1_1_2_NETopCornerpoint_Y Gridblock_1_1_2_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_1_2_NETopCornerpoint_X Gridblock_2_1_2_NETopCornerpoint_Y Gridblock_2_1_2_NETopCornerpoint_Z Gridblock_3_1_2_NETopCornerpoint_X Gridblock_3_1_2_NETopCornerpoint_Y Gridblock_3_1_2_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_2_2_NWTopCornerpoint_X Gridblock_1_2_2_NWTopCornerpoint_Y Gridblock_1_2_2_NWTopCornerpoint_Z Gridblock_1_2_2_NETopCornerpoint_X Gridblock_1_2_2_NETopCornerpoint_Y Gridblock_1_2_2_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_2_2_NETopCornerpoint_X Gridblock_2_2_2_NETopCornerpoint_Y Gridblock_2_2_2_NETopCornerpoint_Z Gridblock_3_2_2_NETopCornerpoint_X Gridblock_3_2_2_NETopCornerpoint_Y Gridblock_3_2_2_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_1_3_2_NWTopCornerpoint_X Gridblock_1_3_2_NWTopCornerpoint_Y Gridblock_1_3_2_NWTopCornerpoint_Z Gridblock_1_3_2_NETopCornerpoint_X Gridblock_1_3_2_NETopCornerpoint_Y Gridblock_1_3_2_NETopCornerpoint_Z");
                input_file.WriteLine("% Gridblock_2_3_2_NETopCornerpoint_X Gridblock_2_3_2_NETopCornerpoint_Y Gridblock_2_3_2_NETopCornerpoint_Z Gridblock_3_3_2_NETopCornerpoint_X Gridblock_3_3_2_NETopCornerpoint_Y Gridblock_3_3_2_NETopCornerpoint_Z");
                input_file.WriteLine("% i.e. in the geometric sequence");
                input_file.WriteLine("%     33--34--35--36");
                input_file.WriteLine("%     /| |/| |/| |/|");
                input_file.WriteLine("%   29-|30-|31-|32 |");
                input_file.WriteLine("%   /| |/| |/| |/| |");
                input_file.WriteLine("% 25--26--27--28 | |");
                input_file.WriteLine("%  | | | | | | | | |");
                input_file.WriteLine("%  | | | | | | | | |");
                input_file.WriteLine("%  | |21-|22-|23-|24");
                input_file.WriteLine("%  | |/| |/| |/| |/|");
                input_file.WriteLine("%  |17-|18-|19-|20 |");
                input_file.WriteLine("%  |/| |/| |/| |/| |");
                input_file.WriteLine("% 13--14--15--16 | |");
                input_file.WriteLine("%  | | | | | | | | |");
                input_file.WriteLine("%  | | | | | | | | |");
                input_file.WriteLine("%  | | 9-|10-|11-|12");
                input_file.WriteLine("%  | |/| |/| |/| |/");
                input_file.WriteLine("%  | 5-|-6-|-7-|-8");
                input_file.WriteLine("%  |/  |/  |/  |/");
                input_file.WriteLine("%  1---2---3---4");
                input_file.WriteLine("% The values can be separated by either spaces or line returns; the layout of the data is not significant");
                input_file.WriteLine("% However the values must be given in order shown above, i.e. looping through each cornerpoint in the grid, first in order of row, then in order of column, then finally in order of layer moving upwards");
                input_file.WriteLine("% NB there is no sanity check to ensure the resulting grid geometry is consistent (e.g. checking for negative gridblock volumes, etc.); this must be done beforehand");

                input_file.Close();

                Console.WriteLine("\nDFMGenerator configuration file did not exist. An empty file has been created. Please enter the required values, SAVE it and press ENTER! ");
                Console.ReadKey();
            }

            // Get path for output files
            string folderPath = "";
            if (args.Length > 0)
            {
                folderPath = inputfile_name.Replace(".txt", "") + "_output" + @"\";
                // If the output folder does not exist, create it
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }
#else
            // Get path for output files
            string fullHomePath = "";
            string folderPath = "";

            try
            {
                var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
                if (homeDrive != null)
                {
                    var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
                    if (homePath != null)
                    {
                        fullHomePath = homeDrive + Path.DirectorySeparatorChar + homePath;
                        folderPath = Path.Combine(fullHomePath, "DFMFolder");
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown: " + e.Message);
                return;
            }
#endif

            // Set hardcoded default values for all parameters

            // Main properties
#if TESTUCF
            // Grid size
            int NoCols = 1;// 3;
            int NoRows = 1;// 3;
            int NoLayers = 1;// 3;
            // Gridblock size; all lengths in metres
            double Width_EW = 10;// 1000;
            double Length_NS = 10;// 1000;
            double LayerThickness = 10;// 1000;
            // Model location 
            // Use the origin offset to set the absolute XY coordinates of the SW corner of the bottom left gridblock
            double OriginXOffset = 0;
            double OriginYOffset = 0;
            // Current depth of burial of the top surface in metres, positive downwards
            double Depth = 2000;// 1000;
#else
            // Grid size
            int NoCols = 3;
            int NoRows = 3;
            int NoLayers = 1;
            // Gridblock size; all lengths in metres
            double Width_EW = 50;
            double Length_NS = 50;
            double LayerThickness = 1;
            // Model location 
            // Use the origin offset to set the absolute XY coordinates of the SW corner of the bottom left gridblock
            double OriginXOffset = 0;
            double OriginYOffset = 0;
            // Current depth of burial of the top surface in metres, positive downwards
            double Depth = 2000;
#endif
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
            double EhminAzi = 0;
            // Set VariableStrainOrientation false to have N-S minimum strain orientatation in all cells
            // Set VariableStrainOrientation true to have laterally variable strain orientation controlled by EhminAzi and EhminCurvature
            bool VariableStrainOrientation = false;
            double EhminCurvature = Math.PI / 16;
            if (VariableStrainOrientation)
                EhminAzi = Math.PI / 4;
            // Strain rates
            // Set to negative value for extensional strain - this is necessary in at least one direction to generate fractures
            // With no strain relaxation, strain rate will control rate of horizontal stress increase
            // With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant horizontal stress
            // Ehmin is most tensile (i.e. most negative) horizontal strain rate
            double EhminRate = 0;
            // Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern
            double EhmaxRate = 0;
            // Set VariableStrainMagnitude to add random variation to the input strain rates
            // Strain rates for each gridblock will vary randomly from 0 to 2x specified values
            bool VariableStrainMagnitude = false;
            double VariableStrainSmoothingFactor = 0;
            // Set TestComplexGeometry true to generate a geometry with non-orthogonal gridblock boundaries and convergent stress orientations - use this to check if fracture generation algorithms can cope with complex geometry
            // TestComplexGeometry will override the normal strain orientation data, so VariableStrainOrientation must be set to false; however the variable strain magnitude should be set to true
            bool TestComplexGeometry = false;
            if (TestComplexGeometry)
            {
                VariableStrainOrientation = false;
                VariableStrainMagnitude = true;
            }
            // Fluid pressure, thermal and uplift loads
            // Rate of increase of fluid overpressure (Pa/ModelTimeUnit)
            double AppliedOverpressureRate = 0;
            // Rate of in situ temperature change (not including cooling due to uplift) (degK/ModelTimeUnit)
            double AppliedTemperatureChange = 0;
            // Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/ModelTimeUnit)
            double AppliedUpliftRate = 0;
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
            List<double> EhminAzi_list = new List<double>();
            List<double> EhminRate_list = new List<double>();
            List<double> EhmaxRate_list = new List<double>();
            List<double> AppliedOverpressureRate_list = new List<double>();
            List<double> AppliedTemperatureChange_list = new List<double>();
            List<double> AppliedUpliftRate_list = new List<double>();
            List<double> StressArchingFactor_list = new List<double>();
            List<double> DeformationEpisodeDuration_list = new List<double>();
            List<Tensor2S> AbsoluteStressRate_list = new List<Tensor2S>();
            List<double> InitialFluidPressure_list = new List<double>();
            List<Tensor2S> InitialAbsoluteStress_list = new List<Tensor2S>();
#if !READINPUTFROMFILE
            /*// Add a deformation episode with default values
            EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(AppliedOverpressureRate);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            ModelTimeUnits = TimeUnits.ma;
            DeformationEpisodeDuration_list.Add(DeformationEpisodeDuration);*/
            /*// Add a deformation episode with uniaxial extension of -0.001/ma over 1ma
            EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(-0.001);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(AppliedOverpressureRate);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            DeformationEpisodeDuration_list.Add(1);
            AbsoluteStressRate_list.Add(AbsoluteStressRate);
            InitialFluidPressure_list.Add(InitialFluidPressure);
            InitialAbsoluteStress_list.Add(InitialAbsoluteStress);*/
            /*// Add an uplift episode, with uplift of 1800m over 18ma
            EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(AppliedOverpressureRate);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(100);
            StressArchingFactor_list.Add(StressArchingFactor);
            DeformationEpisodeDuration_list.Add(18);
            AbsoluteStressRate_list.Add(AbsoluteStressRate);
            InitialFluidPressure_list.Add(InitialFluidPressure);
            InitialAbsoluteStress_list.Add(InitialAbsoluteStress);*/
            /*// Add an overpressure and cooling episode (e.g. injection of cold fluid) for 10 years, with stress arching
            EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(1E+12);
            AppliedTemperatureChange_list.Add(-5E+6);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(1);
            DeformationEpisodeDuration_list.Add(1E-5);
            AbsoluteStressRate_list.Add(AbsoluteStressRate);
            InitialFluidPressure_list.Add(InitialFluidPressure);
            InitialAbsoluteStress_list.Add(InitialAbsoluteStress);*/
            /*// Add a deformation episode with a defined stress load
            EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(0.05);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            ModelTimeUnits = TimeUnits.second;
            DeformationEpisodeDuration_list.Add(31622400); // One year in seconds
            AbsoluteStressRate_list.Add(new Tensor2S(0.02, -0.02, -0.3, 0.02, -0.04, 0.04));
            InitialFluidPressure_list.Add(15000000);
            InitialAbsoluteStress_list.Add(new Tensor2S(40000000, 40000000, 60000000, -500000, 1000000, -1000000));
            BiazimuthalConjugate = false;*/
#if TESTUCF
            /*EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(0);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            ModelTimeUnits = TimeUnits.ma;
            DeformationEpisodeDuration_list.Add(10);
            AbsoluteStressRate_list.Add(new Tensor2S(-1333333.333, -1333333.333, 0, 0, 0, 0));
            InitialFluidPressure_list.Add(19620000);
            InitialAbsoluteStress_list.Add(new Tensor2S(35970000, 35970000, 44145000, 0, 0, 0));
            BiazimuthalConjugate = false;*/
            //EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(0);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            ModelTimeUnits = TimeUnits.ma;
            DeformationEpisodeDuration_list.Add(1);
            //AbsoluteStressRate_list.Add(new Tensor2S(-1333333.333, -1333333.333, 0, 0, 0, 0));
            AbsoluteStressRate_list.Add(new Tensor2S(0, 0, 0, 0, 0, 0));
            //InitialFluidPressure_list.Add(19620000);
            InitialFluidPressure_list.Add(0);
            //InitialAbsoluteStress_list.Add(new Tensor2S(35970000, 35970000, 44145000, 0, 0, 0));
            InitialAbsoluteStress_list.Add(new Tensor2S(15406000, 15694000, 42114000, -542000, 1254000, -277000));
            BiazimuthalConjugate = false;
            //EhminAzi_list.Add(EhminAzi);
            EhminRate_list.Add(EhminRate);
            EhmaxRate_list.Add(EhmaxRate);
            AppliedOverpressureRate_list.Add(0);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            ModelTimeUnits = TimeUnits.ma;
            DeformationEpisodeDuration_list.Add(5);
            //AbsoluteStressRate_list.Add(new Tensor2S(-1333333.333, -1333333.333, 0, 0, 0, 0));
            AbsoluteStressRate_list.Add(new Tensor2S(-1358400, -1057000, 199600, -680800, 31600, -123000));
            //InitialFluidPressure_list.Add(19620000);
            //InitialFluidPressure_list.Add(0);
            //InitialAbsoluteStress_list.Add(new Tensor2S(35970000, 35970000, 44145000, 0, 0, 0));
            //InitialAbsoluteStress_list.Add(new Tensor2S(16304000, 16778000, 43412000, -408000, 2154000, -459000));
            BiazimuthalConjugate = false;
#else
            // Add a deformation episode with uniaxial extension of -0.001/ma over 1ma
            EhminAzi_list.Add(Math.PI/4);
            EhminRate_list.Add(-0.01);
            EhmaxRate_list.Add(-0.01);
            AppliedOverpressureRate_list.Add(AppliedOverpressureRate);
            AppliedTemperatureChange_list.Add(AppliedTemperatureChange);
            AppliedUpliftRate_list.Add(AppliedUpliftRate);
            StressArchingFactor_list.Add(StressArchingFactor);
            DeformationEpisodeDuration_list.Add(-1);
            AbsoluteStressRate_list.Add(AbsoluteStressRate);
            InitialFluidPressure_list.Add(InitialFluidPressure);
            InitialAbsoluteStress_list.Add(InitialAbsoluteStress);
#endif
#endif

            // Mechanical properties
            double YoungsMod = 2.5E+10;// 1E+10;
            // Set VariableYoungsMod true to have laterally variable Young's Modulus
            bool VariableYoungsMod = false;
            double VariableYoungsModSmoothingFactor = 2;
            double PoissonsRatio = 0.25;
            double Porosity = 0.2;
            double BiotCoefficient = 1;
            // Thermal expansion coefficient typically 3E-5/degK for sandstone, 4E-5/degK for shale (Miller 1995)
            double ThermalExpansionCoefficient = 4E-5;
            double CrackSurfaceEnergy = 1000;
            // Set VariableCSE true to have laterally variable crack surface energy
            bool VariableCSE = false;
            double FrictionCoefficient = 0.5;
            // Set VariableFriction true to have laterally variable friction coefficient
            bool VariableFriction = false;
            // Strain relaxation data
            // Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate
            double RockStrainRelaxation = 0;
            // Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only
            double FractureRelaxation = 0;
#if TESTUCF
            /*// Initial microfracture distribution function: Exponential
            // NB This is currently only used for unconfined fractures; for layer-bound fractures it is assumed to be power law
            InitialFractureDistribution InitialMicrofractureDistributionFunction = InitialFractureDistribution.Exponential;
            // Density of initial microfractures
            double InitialMicrofractureDensity = 2;
            // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
            // This will be used as the S parameter (standard deviation) for the log-normal distribution function
            double InitialMicrofractureSizeDistribution = 15;
            // Median initial microfracture size - this is only used for the log-normal distribution function
            // Set to -1 to use layer thickness / 20
            double InitialMicrofractureMedianRadius = 1;*/
            // Initial microfracture distribution function: Log-normal
            // NB This is currently only used for unconfined fractures; for layer-bound fractures it is assumed to be power law
            InitialFractureDistribution InitialMicrofractureDistributionFunction = InitialFractureDistribution.LogNormal;
            // Density of initial microfractures
            double InitialMicrofractureDensity = 2;
            // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
            // This will be used as the S parameter (standard deviation) for the log-normal distribution function
            double InitialMicrofractureSizeDistribution = 0.6;
            // Median initial microfracture radius - this is only used for the log-normal distribution function
            // Set to -1 to use layer thickness / 20
            double InitialMicrofractureMedianRadius = 0.05;
#else
            // Initial microfracture distribution function: Power Law
            InitialFractureDistribution InitialMicrofractureDistributionFunction = InitialFractureDistribution.PowerLaw;
            // Density of initial microfractures
            double InitialMicrofractureDensity = 0.001;
            // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
            // This will be used as the S parameter (standard deviation) for the log-normal distribution function
            double InitialMicrofractureSizeDistribution = 3;
            // Median initial microfracture size - this is only used for the log-normal distribution function
            // Set to -1 to use layer thickness / 20
            double InitialMicrofractureMedianRadius = 1;
#endif
            // Subcritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
            double SubcriticalPropIndex = 10;
            double CriticalPropagationRate = 2000;
            // Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity
            double HostRock_kh = 9.869233e-16;// 1mD in m2
            double HostRock_kv = 9.869233e-16;// 1mD in m2

            // Cleavages: Cleavage planes reduce the crack surface energy and/or friction coefficient on fractures parallel to the specified cleavage orientation
            // Multiple cleavage planes can be defined; for each one, the crack surface energy and/or friction coefficient on the fracture set with the closest orientation (within the specified MaxConsistencyAngle) will be modified accordingly
            // If there is no fracture set within the specified MaxConsistencyAngle, the cleavage plane will have no effect
            // Create a list of cleavage planes
            List<Cleavage> Cleavages = new List<Cleavage>();
            // Add a North-dipping inclined cleavage plane with Crack Surface Energy 500J/m2 and Friction Coefficient 0.4
            //Cleavages.Add(new Cleavage(500, 0.4, 4.71, 1.01));// Math.PI / 2));

            // Stress state
            // Stress distribution scenario - use to turn on or off stress shadow effect
            // Do not use DuctileBoundary as this is not yet implemented
            StressDistribution StressDistributionScenario = StressDistribution.StressShadow; // StressDistribution.EvenlyDistributedStress;//
            // Depth at the start of deformation (in metres, positive downwards) - this will control stress state
            // If DepthAtDeformation is specified, this will be used to calculate vertical stress
            // If DepthAtDeformation is <=0 or NaN, the depth at the start of deformation will be set to the current depth plus total specified uplift
            double DepthAtDeformation = -1;
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
            // Flag to output the layer-bound fracture centrepoints and the unconfined fracture rays as polylines
            bool OutputCentrepoints = true;// false;
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
            // Number of fracture or ray length values to calculate for each of the implicit fracture population distribution functions
            int No_l_indexPoints = 20;
            // MaxHMinLength and MaxHMaxLength control the range of fracture or ray lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
            // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length
            double MaxHMinLength = 0;
            double MaxHMaxLength = 0;
            // Depth of horizontal section
            // Set this to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file
            double DepthOfHorizontalSection = 2500;// double.NaN;

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
            // Present day Terzaghi effective stress tensor - define this to override stress at the time of deformation when calculating fracture aperture and permeability
            double PresentDayEffectiveStress_XX = 0;
            double PresentDayEffectiveStress_YY = 0;
            double PresentDayEffectiveStress_ZZ = 0;
            double PresentDayEffectiveStress_XY = 0;
            double PresentDayEffectiveStress_YZ = 0;
            double PresentDayEffectiveStress_ZX = 0;

            // Calculation control parameters
            // Number of layer-bound fracture sets
            // Set to 1 to generate a single fracture set, perpendicular to ehmin
            // Set to 2 to generate two orthogonal fracture sets, perpendicular to ehmin and ehmax; this is typical of a single stage of deformation in intact rock
            // Set to 6 or more to generate oblique fractures; this is typical of multiple stages of deformation with fracture reactivation, or transtensional strain
#if TESTUCF
            int NoLayerBoundFractureSets = 0;
#else
            int NoLayerBoundFractureSets = 2;
#endif
            // Layer-bound fracture mode: set these to force only Mode 1 (dilatant) or only Mode 2 (shear) fractures; otherwise model will include both, depending on which is energetically optimal
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
            // Minimum required clear zone volume in which macrofractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
            double MinimumMFClearZoneVolume = 0.01;
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
            // Maximum number of fracture patches that can be generated per gridblock
            // Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason
#if TESTUCF
            int MaxNoFracturePatches = 1000;
#else
            int MaxNoFracturePatches = 100000;
#endif
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

            // Parameters for controlling unconfined fractures
            // Use NoUnconfinedFractureStrikeSets and NoUnconfinedFractureDipSets to create unconfined fracture sets, which can propagate and interact vertically as well as horizontally
            // These are useful for modelling fractures in thick geobodies such as igneous plutons
            // NB Unconfined fracture sets are not subdivided into dipsets; unconfined fractures with the same strike but different dips are counted as different sets
            // The total number of unconfined fracture sets generated will therefore be given by NoUnconfinedFractureStrikeSets * NoUnconfinedFractureDipSets
#if TESTUCF
            int NoUnconfinedFractureStrikeSets = 6;
            int NoUnconfinedFractureDipSets = 3;
#else
            int NoUnconfinedFractureStrikeSets = 0;
            int NoUnconfinedFractureDipSets = 0;
#endif
            // Number of rays comprising each unconfined fracture
            int NoRaysPerUnconfinedFracture = 16;
            // Minimum radius for unconfined fractures; this will be the length of the rays at nucleation
            // If set to -1, will use 0.01 * layer thickness
            double MinUnconfinedFractureRadius = 0.5;// -1;
            // Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length
            // If set to -1, will use 0.5 * layer thickness
            double MaxUnconfinedFractureRadius = 50;// -1;
            // Maximum allowed effective radius for unconfined fractures; will limit fracture stress shadow and propagation rate but not fracture growth
            // If set to -1, there will be no limit on effective fracture radius
            double MaxEffectiveUnconfinedFractureRadius = -1;
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
            double MinimumUCFClearZoneVolume = 0.1;
            // Minimum allowed mean static unconfined fracture ray length; if the mean static ray length drops below this value, the fracture set will be deactivated; set to 0 for no limit and -1 to use the minimum UCF radius
            double MinimumStaticUCRLength = -1;
            // Maximum increase in UCFP33 allowed in each timestep - controls the optimal timestep duration
            // Increase this to run calculation faster, with fewer but longer timesteps
            double MaxTimestepUCFP33Increase = 0.01;
            // Maximum proportional increase in the unconfined fracture ray length in each timestep (controls speed and accuracy of calculation)
            // Set to -1 for no limit 
            double MaxTimestepRadiusIncrease = 0.2;
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
                        case "NoCols":
                            NoCols = Convert.ToInt32(line_split[1]);
                            break;
                        case "NoRows":
                            NoRows = Convert.ToInt32(line_split[1]);
                            break;
                        case "NoLayers":
                            NoLayers = Convert.ToInt32(line_split[1]);
                            break;
                        // Gridblock size
                        case "Width_EW":
                        case "width_EW": // For backwards compatibility
                            Width_EW = Convert.ToDouble(line_split[1]);
                            break;
                        case "Length_NS":
                        case "length_NS": // For backwards compatibility
                            Length_NS = Convert.ToDouble(line_split[1]);
                            break;
                        case "LayerThickness":
                            LayerThickness = Convert.ToDouble(line_split[1]);
                            break;
                        // Model location 
                        // Use the origin offset to set the absolute XY coordinates of the SW corner of the bottom left gridblock
                        case "OriginXOffset":
                            OriginXOffset = Convert.ToDouble(line_split[1]);
                            break;
                        case "OriginYOffset":
                            OriginYOffset = Convert.ToDouble(line_split[1]);
                            break;
                        case "Depth":
                            Depth = Convert.ToDouble(line_split[1]);
                            break;
                        // Time units used in input load rates, time limits and strain relaxation time constants
                        // These will be converted to SI units (s) by the gridblock objects
                        case "ModelTimeUnits":
                        case "timeUnits_in": // For backwards compatibility
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
                        case "EhminAzi":
                        case "Epsilon_hmin_azimuth_in": // For backwards compatibility
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    EhminAzi_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;
                        // Set VariableStrainOrientation false to have N-S minimum strain orientatation in all cells
                        // Set VariableStrainOrientation true to have laterally variable strain orientation controlled by EhminAzi and EhminCurvature
                        case "VariableStrainOrientation":
                            VariableStrainOrientation = (line_split[1] == "true");
                            break;
                        case "EhminCurvature":
                        case "Epsilon_hmin_curvature_in": // For backwards compatibility
                            EhminCurvature = Convert.ToDouble(line_split[1]);
                            break;
                        // Strain rates
                        // Set to negative value for extensional strain - this is necessary in at least one direction to generate fractures
                        // With no strain relaxation, strain rate will control rate of horizontal stress increase
                        // With strain relaxation, ratio of strain rate to strain relaxation time constants will control magnitude of constant horizontal stress
                        // Ehmin is most tensile (i.e. most negative) horizontal strain rate
                        case "EhminRate":
                        case "Epsilon_hmin_dashed_in": // For backwards compatibility
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    EhminRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;
                        // Set EhmaxRate to 0 for uniaxial strain; set to between 0 and EhminRate for anisotropic fracture pattern; set to EhminRate for isotropic fracture pattern
                        case "EhmaxRate":
                        case "Epsilon_hmax_dashed_in": // For backwards compatibility
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    EhmaxRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;
                        // Set VariableStrainMagnitude to add random variation to the input strain rates
                        // Strain rates for each gridblock will vary randomly from 0 to 2x specified values
                        case "VariableStrainMagnitude":
                            VariableStrainMagnitude = (line_split[1] == "true");
                            break;
                        // TestComplexGeometry is only used for testing and cannot be set in an input file
                        // Fluid pressure, thermal and uplift loads
                        // Rate of increase of fluid overpressure (Pa/ModelTImeUnit)
                        case "AppliedOverpressureRate":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    AppliedOverpressureRate_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;
                        // Rate of in situ temperature change (not including cooling due to uplift) (degK/ModelTImeUnit)
                        case "AppliedTemperatureChange":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    AppliedTemperatureChange_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;
                        // Rate of uplift and erosion; will generate decrease in lithostatic stress, fluid pressure and temperature (m/ModelTImeUnit)
                        case "AppliedUpliftRate":
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    AppliedUpliftRate_list.Add(Convert.ToDouble(line_split[valueNo]));
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
                        case "DeformationStageDuration": // For backwards compatibility
                        case "DeformationStageDuration_in": // For backwards compatibility
                            {
                                int noValues = line_split.GetLength(0);
                                for (int valueNo = 1; valueNo < noValues; valueNo++)
                                    DeformationEpisodeDuration_list.Add(Convert.ToDouble(line_split[valueNo]));
                            }
                            break;

                        // Mechanical properties
                        case "YoungsMod":
                            YoungsMod = Convert.ToDouble(line_split[1]);
                            break;
                        // Set VariableYoungsMod true to have laterally variable Young's Modulus
                        case "VariableYoungsMod":
                            VariableYoungsMod = (line_split[1] == "true");
                            break;
                        case "PoissonsRatio":
                            PoissonsRatio = Convert.ToDouble(line_split[1]);
                            break;
                        case "Porosity":
                            Porosity = Convert.ToDouble(line_split[1]);
                            break;
                        case "BiotCoefficient":
                            BiotCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        case "ThermalExpansionCoefficient":
                            ThermalExpansionCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        case "CrackSurfaceEnergy":
                            CrackSurfaceEnergy = Convert.ToDouble(line_split[1]);
                            break;
                        // Set VariableCSE true to have laterally variable crack surface energy
                        case "VariableCSE":
                            VariableCSE = (line_split[1] == "true");
                            break;
                        case "FrictionCoefficient":
                            FrictionCoefficient = Convert.ToDouble(line_split[1]);
                            break;
                        // Set VariableFriction true to have laterally variable friction coefficient
                        case "VariableFriction":
                            VariableFriction = (line_split[1] == "true");
                            break;
                        // Strain relaxation data
                        // Set RockStrainRelaxation to 0 for no strain relaxation and steadily increasing horizontal stress; set it to >0 for constant horizontal stress determined by ratio of strain rate and relaxation rate
                        case "RockStrainRelaxation":
                            RockStrainRelaxation = Convert.ToDouble(line_split[1]);
                            break;
                        // Set FractureRelaxation to >0 and RockStrainRelaxation to 0 to apply strain relaxation to the fractures only
                        case "FractureRelaxation":
                            FractureRelaxation = Convert.ToDouble(line_split[1]);
                            break;
                        // Initial microfracture distribution function
                        // NB This is currently only used for unconfined fractures; for layer-bound fractures it is assumed to be power law
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
                        case "InitialMicrofractureDensity":
                        case "B": // For backwards compatibility
                            InitialMicrofractureDensity = Convert.ToDouble(line_split[1]);
                            break;
                        // Size distribution of initial microfractures - increase for larger ratio of small:large initial microfractures
                        // This will be used as the S parameter (standard deviation) for the log-normal distribution
                        case "InitialMicrofractureSizeDistribution":
                        case "c": // For backwards compatibility
                            InitialMicrofractureSizeDistribution = Convert.ToDouble(line_split[1]);
                            break;
                        // Median initial microfracture radius - this is only used for the log-normal distribution
                        // Set to -1 to use layer thickness / 20
                        case "InitialMicrofractureMedianRadius":
                            InitialMicrofractureMedianRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Subcritical fracture propagation index; <5 for slow subcritical propagation, 5-15 for intermediate, >15 for rapid critical propagation
                        case "SubcriticalPropIndex":
                        case "b": // For backwards compatibility
                            SubcriticalPropIndex = Convert.ToDouble(line_split[1]);
                            break;
                        case "CriticalPropagationRate":
                            CriticalPropagationRate = Convert.ToDouble(line_split[1]);
                            break;
                        // Host rock permeability is used to calculate fracture permeability correcting for fracture size and connectivity
                        case "HostRock_kh":
                            HostRock_kh = Convert.ToDouble(line_split[1]);
                            break;
                        case "HostRock_kv":
                            HostRock_kv = Convert.ToDouble(line_split[1]);
                            break;

                        // Cleavages: Cleavage planes reduce the crack surface energy and/or friction coefficient on fractures parallel to the specified cleavage orientation");
                        // Multiple cleavage planes can be defined; for each one, the crack surface energy and/or friction coefficient on the fracture set with the closest orientation (within the specified MaxConsistencyAngle) will be modified accordingly");
                        // If there is no fracture set within the specified MaxConsistencyAngle, the cleavage plane will have no effect");
                        case "Cleavage":
                            {
                                int noValues = line_split.GetLength(0);
                                // We can only add a cleavage if at least three values are specified (azimuth, dip, crack surface energy); optionally friction coefficient can also be defined
                                if (noValues > 3)
                                {
                                    // The azimuth must be converted to a strike
                                    double strike = Convert.ToDouble(line_split[1]) - (Math.PI / 2);
                                    double dip = Convert.ToDouble(line_split[2]);
                                    double CSE = Convert.ToDouble(line_split[3]);
                                    // If only 3 values are specified, set the friction coefficint override to NaN
                                    double MuFr = (noValues > 4) ? Convert.ToDouble(line_split[4]) : double.NaN;

                                    // If both strike and dip are valid numbers, create a new cleavage plane and add it to the list
                                    if (!double.IsNaN(strike) && !double.IsNaN(dip))
                                    {
                                        Cleavage newCleavage = new Cleavage(CSE, MuFr, strike, dip);
                                        Cleavages.Add(newCleavage);
                                    }
                                }
                            }
                            break;

                        // Stress state
                        // Stress distribution scenario - use to turn on or off stress shadow effect
                        // Do not use DuctileBoundary as this is not yet implemented
                        case "StressDistributionScenario":
                        case "StressDistribution_in": // For backwards compatibility
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
                        case "DepthAtDeformation":
                        case "DepthAtFracture": // For backwards compatibility
                            DepthAtDeformation = Convert.ToDouble(line_split[1]);
                            break;
                        // Mean density of overlying sediments and fluid (kg/m3)
                        case "MeanOverlyingSedimentDensity":
                        case "mean_overlying_sediment_density": // For backwards compatibility
                            MeanOverlyingSedimentDensity = Convert.ToDouble(line_split[1]);
                            break;
                        case "FluidDensity":
                        case "fluid_density": // For backwards compatibility
                            FluidDensity = Convert.ToDouble(line_split[1]);
                            break;
                        // Fluid overpressure (Pa)
                        case "InitialOverpressure":
                        case "fluid_overpressure": // For backwards compatibility
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
                        case "LogCalculation": // For backwards compatibility
                            WriteImplicitDataFiles = true;
                            break;
                        case "WriteDFNFiles":
                            WriteDFNFiles = true;
                            break;
                        // Output file type for explicit DFN data: ASCII or FAB (NB FAB files can be loaded directly into Petrel)
                        case "OutputDFNFileType":
                        case "OutputFileType": // For backwards compatibility
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
                        case "noIntermediateOutputs": // For backwards compatibility
                            NoIntermediateOutputs = Convert.ToInt32(line_split[1]);
                            break;
                        // Flag to control interval between output of intermediate stage DFMs; they can either be output at specified times, at equal intervals of time, or at approximately regular intervals of total fracture area
                        case "IntermediateOutputIntervalControl":
                        case "OutputAtEqualTimeIntervals": // For backwards compatibility
                        case "separateIntermediateOutputsByTime": // For backwards compatibility
                            {
                                if (line_split[1] == "EqualArea")
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualArea;
                                else if (line_split[1] == "EqualTime")
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualTime;
                                else if (line_split[1] == "SpecifiedTime")
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.SpecifiedTime;
                                else if (line_split[1] == "true") // For backwards compatibility
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualTime;
                                else
                                    IntermediateOutputIntervalControl = IntermediateOutputInterval.EqualArea;
                            }
                            break;
                        // Flag to output the layer-bound fracture centrepoints and the unconfined fracture rays as polylines
                        case "OutputCentrepoints":
                        case "outputCentrepoints": // For backwards compatibility
                            OutputCentrepoints = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output the bulk rock compliance and stiffness tensors
                        case "OutputBulkRockElasticTensors":
                        case "OutputComplianceTensor": // For backwards compatibility
                            OutputBulkRockElasticTensors = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture porosity
                        case "OutputFracturePorosity":
                        case "CalculateFracturePorosity": // For backwards compatibility
                        case "CalculateFracturePorosity_in": // For backwards compatibility
                            OutputFracturePorosity = (line_split[1] == "true");
                            break;
                        // Flag to calculate and output fracture permeability tensors
                        case "OutputFracturePermeabilityTensor":
                        case "CalculateFracturePermeabilityTensor": // For backwards compatibility
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
                        case "CalculatePopulationDistribution": // For backwards compatibility
                        case "CalculatePopulationDistribution_in": // For backwards compatibility
                            OutputPopulationDistribution = (line_split[1] == "true");
                            break;
                        // Number of macrofracture length values to calculate for each of the implicit fracture population distribution functions
                        case "No_l_indexPoints":
                        case "no_l_indexPoints_in": // For backwards compatibility
                            No_l_indexPoints = Convert.ToInt32(line_split[1]);
                            break;
                        // MaxHMinLength and MaxHMaxLength control the range of macrofracture lengths to calculate for the implicit fracture population distribution functions for fractures striking perpendicular to hmin and hmax respectively
                        // Set these values to the approximate maximum length of fractures generated, or 0 if this is not known; 0 will default to maximum potential length - but this may be much greater than actual maximum length
                        case "MaxHMinLength":
                        case "maxHMinLength": // For backwards compatibility
                            MaxHMinLength = Convert.ToDouble(line_split[1]);
                            break;
                        case "MaxHMaxLength":
                        case "maxHMaxLength": // For backwards compatibility
                            MaxHMaxLength = Convert.ToDouble(line_split[1]);
                            break;
                        // Depth of horizontal section
                        // Set this to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file
                        case "DepthOfHorizontalSection":
                            DepthOfHorizontalSection = Convert.ToDouble(line_split[1]);
                            break;

                        // Fracture aperture control parameters
                        // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
                        case "FractureApertureControl":
                        case "FractureApertureControl_in": // For backwards compatibility
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
                        case "Mode1HMin_UniformAperture_in": // For backwards compatibility
                            Mode1HMin_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fixed aperture for Mode 2 fractures striking perpendicular to hmin in the uniform aperture case (m)
                        case "Mode2HMin_UniformAperture":
                        case "Mode2HMin_UniformAperture_in": // For backwards compatibility
                            Mode2HMin_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fixed aperture for Mode 1 fractures striking perpendicular to hmax in the uniform aperture case (m)
                        case "Mode1HMax_UniformAperture":
                        case "Mode1HMax_UniformAperture_in": // For backwards compatibility
                            Mode1HMax_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fixed aperture for Mode 2 fractures striking perpendicular to hmax in the uniform aperture case (m)
                        case "Mode2HMax_UniformAperture":
                        case "Mode2HMax_UniformAperture_in": // For backwards compatibility
                            Mode2HMax_UniformAperture = Convert.ToDouble(line_split[1]);
                            break;
                        // Fracture aperture control parameters: SizeDependent fracture aperture
                        // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode1HMin_SizeDependentApertureMultiplier":
                        case "Mode1HMin_SizeDependentApertureMultiplier_in": // For backwards compatibility
                            Mode1HMin_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmin - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode2HMin_SizeDependentApertureMultiplier":
                        case "Mode2HMin_SizeDependentApertureMultiplier_in": // For backwards compatibility
                            Mode2HMin_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode1HMax_SizeDependentApertureMultiplier":
                        case "Mode1HMax_SizeDependentApertureMultiplier_in": // For backwards compatibility
                            Mode1HMax_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to hmax - layer-bound fracture aperture is given by layer thickness times this multiplier
                        case "Mode2HMax_SizeDependentApertureMultiplier":
                        case "Mode2HMax_SizeDependentApertureMultiplier_in": // For backwards compatibility
                            Mode2HMax_SizeDependentApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Fracture aperture control parameters: Dynamic fracture aperture
                        // Multiplier for dynamic aperture
                        case "DynamicApertureMultiplier":
                        case "DynamicApertureMultiplier_in": // For backwards compatibility
                            DynamicApertureMultiplier = Convert.ToDouble(line_split[1]);
                            break;
                        // Fracture aperture control parameters: Barton-Bandis model for fracture aperture
                        // Joint Roughness Coefficient
                        case "JRC":
                        case "JRC_in": // For backwards compatibility
                            JRC = Convert.ToDouble(line_split[1]);
                            break;
                        // Compressive strength ratio; ratio of unconfined compressive strength of unfractured rock to fractured rock
                        case "UCSRatio":
                        case "UCS_ratio_in": // For backwards compatibility
                            UCSRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Initial normal stress on fracture (Pa)
                        case "InitialNormalStress":
                        case "InitialNormalStress_in": // For backwards compatibility
                            InitialNormalStress = Convert.ToDouble(line_split[1]);
                            break;
                        // Stiffness normal to the fracture, at initial normal stress (Pa/m)
                        case "FractureNormalStiffness":
                        case "FractureNormalStiffness_in": // For backwards compatibility
                            FractureNormalStiffness = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum fracture closure (m)
                        case "MaximumClosure":
                        case "MaximumClosure_in": // For backwards compatibility
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
                        case "PresentDayEffectiveStress_XX":
                            PresentDayEffectiveStress_XX = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayEffectiveStress_YY":
                            PresentDayEffectiveStress_YY = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayEffectiveStress_ZZ":
                            PresentDayEffectiveStress_ZZ = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayEffectiveStress_XY":
                            PresentDayEffectiveStress_XY = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayEffectiveStress_YZ":
                            PresentDayEffectiveStress_YZ = Convert.ToDouble(line_split[1]);
                            break;
                        case "PresentDayEffectiveStress_ZX":
                            PresentDayEffectiveStress_ZX = Convert.ToDouble(line_split[1]);
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
                        case "maxTimestepDuration_in": // For backwards compatibility
                            MaxTimestepDuration = Convert.ToDouble(line_split[1]);
                            break;
                        // Maximum increase in MFP33 allowed in each timestep - controls the optimal timestep duration
                        // Increase this to run calculation faster, with fewer but longer timesteps
                        case "MaxTimestepMFP33Increase":
                        case "max_TS_MFP33_increase_in": // For backwards compatibility
                            MaxTimestepMFP33Increase = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum radius for microfractures to be included in implicit fracture density and porosity calculations
                        // If this is set to 0 (i.e. include all microfractures) then it will not be possible to calculate volumetric microfracture density as this will be infinite
                        // If this is set to -1 the maximum radius of the smallest bin will be used (i.e. exclude the smallest bin from the microfracture population)
                        case "MinImplicitMicrofractureRadius":
                        case "minMicrofractureRadius_in": // For backwards compatibility
                            MinImplicitMicrofractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Number of bins used in numerical integration of uFP32
                        // This controls accuracy of numerical calculation of microfracture populations - increase this to increase accuracy of the numerical integration at expense of runtime 
                        case "No_r_bins":
                        case "no_r_bins_in": // For backwards compatibility
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
                        case "d_historic_MFP33_termination_ratio_in": // For backwards compatibility
                            Current_HistoricMFP33TerminationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; set to negative value to switch off this control
                        case "Active_TotalMFP30TerminationRatio":
                        case "active_total_MFP30_termination_ratio_in": // For backwards compatibility
                            Active_TotalMFP30TerminationRatio = Convert.ToDouble(line_split[1]);
                            break;
                        // Minimum required clear zone volume in which macrofractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
                        case "MinimumMFClearZoneVolume":
                        case "MinimumClearZoneVolume": // For backwards compatibility
                        case "minimum_ClearZone_Volume_in": // For backwards compatibility
                            MinimumMFClearZoneVolume = Convert.ToDouble(line_split[1]);
                            break;
                        // Use the deformation episode duration (set in the deformation load inputs) or the maximum timestep limit to stop the calculation before fractures have finished growing
                        case "MaxTimesteps":
                        case "maxTimesteps_in": // For backwards compatibility
                            MaxTimesteps = Convert.ToInt32(line_split[1]);
                            break;
                        // DFN geometry controls
                        // Flag to generate explicit DFN; if set to false only implicit fracture population functions will be generated
                        case "GenerateExplicitDFN":
                            GenerateExplicitDFN = (line_split[1] == "true");
                            break;
                        // Set false to allow fractures to propagate outside of the outer grid boundary
                        case "CropAtBoundary":
                        case "cropAtBoundary": // For backwards compatibility
                            CropAtBoundary = (line_split[1] == "true");
                            break;
                        // Set true to link fractures that terminate due to stress shadow interaction into one long fracture, via a relay segment
                        case "LinkStressShadows":
                        case "linkStressShadows": // For backwards compatibility
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
                        // Maximum number of fracture segments that can be generated per gridblock
                        // Set this to prevent the program from hanging if excessive numbers of fractures are generated for any reason
                        case "MaxNoFractureSegments":
                            MaxNoFractureSegments = Convert.ToInt32(line_split[1]);
                            break;
                        // Allow fracture nucleation to be controlled probabilistically, if the number of fractures nucleating per timestep is less than the specified value - this will allow fractures to nucleate when gridblocks are small
                        // Set to 0 to disable probabilistic fracture nucleation
                        // Set to -1 for automatic (probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active; if SearchNeighbouringGridblocks is set to automatic, this will be determined independently for each gridblock based on the gridblock geometry)
                        case "ProbabilisticFractureNucleationLimit":
                        case "probabilisticFractureNucleationLimit": // For backwards compatibility
                            ProbabilisticFractureNucleationLimit = Convert.ToDouble(line_split[1]);
                            break;
                        // Flag to control the order in which fractures are propagated within each timestep: if true, fractures will be propagated in order of nucleation time regardless of fracture set; if false they will be propagated in order of fracture set
                        // Propagating in strict order of nucleation time removes bias in fracture lengths between sets, but will add a small overhead to calculation time
                        case "PropagateFracturesInNucleationOrder":
                        case "propagateFracturesInNucleationOrder": // For backwards compatibility
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
                        case "MinDFNMicrofractureRadius": // For backwards compatibility
                            MinExplicitMicrofractureRadius = Convert.ToDouble(line_split[1]);
                            break;
                        // Number of cornerpoints defining the microfracture polygons in the explicit DFN
                        // Set to zero to output microfractures as just a centrepoint and radius; set to 3 or greater to output microfractures as polygons defined by a list of cornerpoints
                        case "Number_uF_Points":
                        case "number_uF_Points": // For backwards compatibility
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

            // Find the number of deformation episodes
            int noDeformationEpisodes = EhminAzi_list.Count;
            if (noDeformationEpisodes < EhminRate_list.Count)
                noDeformationEpisodes = EhminRate_list.Count;
            if (noDeformationEpisodes < EhmaxRate_list.Count)
                noDeformationEpisodes = EhmaxRate_list.Count;
            if (noDeformationEpisodes < AppliedOverpressureRate_list.Count)
                noDeformationEpisodes = AppliedOverpressureRate_list.Count;
            if (noDeformationEpisodes < AppliedTemperatureChange_list.Count)
                noDeformationEpisodes = AppliedTemperatureChange_list.Count;
            if (noDeformationEpisodes < AppliedUpliftRate_list.Count)
                noDeformationEpisodes = AppliedUpliftRate_list.Count;
            if (noDeformationEpisodes < StressArchingFactor_list.Count)
                noDeformationEpisodes = StressArchingFactor_list.Count;
            if (noDeformationEpisodes < DeformationEpisodeDuration_list.Count)
                noDeformationEpisodes = DeformationEpisodeDuration_list.Count;

            // Create arrays for variable deformation load parameters and populate them
            // For these parameters, the arrays representing the gridblocks will be nested within lists representing the deformation episodes
            List<double[,,]> EhminAzi_array = new List<double[,,]>();
            List<double[,,]> EhminRate_array = new List<double[,,]>();
            List<double[,,]> EhmaxRate_array = new List<double[,,]>();
            List<double[,,]> AppliedOverpressureRate_array = new List<double[,,]>();
            List<double[,,]> AppliedTemperatureChange_array = new List<double[,,]>();
            List<double[,,]> AppliedUpliftRate_array = new List<double[,,]>();
            List<double[,,]> StressArchingFactor_array = new List<double[,,]>();
            List<double[,,]> DeformationEpisodeDuration_array = new List<double[,,]>();
            List<Tensor2S[,,]> AbsoluteStressRate_array = new List<Tensor2S[,,]>();
            List<double[,,]> InitialFluidPressure_array = new List<double[,,]>();
            List<Tensor2S[,,]> InitialAbsoluteStress_array = new List<Tensor2S[,,]>();
            // Loop through each deformation episode
            for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
            {
                // Get the global values for this deformation episode for each parameter
                double nextEhminAzi = (deformationEpisodeNo < EhminAzi_list.Count ? EhminAzi_list[deformationEpisodeNo] : EhminAzi);
                double nextEhminRate = (deformationEpisodeNo < EhminRate_list.Count ? EhminRate_list[deformationEpisodeNo] : EhminRate);
                double nextEhmaxRate = (deformationEpisodeNo < EhmaxRate_list.Count ? EhmaxRate_list[deformationEpisodeNo] : EhmaxRate);
                double nextAppliedOverpressureRate = (deformationEpisodeNo < AppliedOverpressureRate_list.Count ? AppliedOverpressureRate_list[deformationEpisodeNo] : AppliedOverpressureRate);
                double nextAppliedTemperatureChange = (deformationEpisodeNo < AppliedTemperatureChange_list.Count ? AppliedTemperatureChange_list[deformationEpisodeNo] : AppliedTemperatureChange);
                double nextAppliedUpliftRate = (deformationEpisodeNo < AppliedUpliftRate_list.Count ? AppliedUpliftRate_list[deformationEpisodeNo] : AppliedUpliftRate);
                double nextStressArchingFactor = (deformationEpisodeNo < StressArchingFactor_list.Count ? StressArchingFactor_list[deformationEpisodeNo] : StressArchingFactor);
                double nextDeformationEpisodeDuration = (deformationEpisodeNo < DeformationEpisodeDuration_list.Count ? DeformationEpisodeDuration_list[deformationEpisodeNo] : DeformationEpisodeDuration);
                Tensor2S nextAbsoluteStressRate = (deformationEpisodeNo < AbsoluteStressRate_list.Count ? AbsoluteStressRate_list[deformationEpisodeNo] : AbsoluteStressRate);
                double nextInitialFluidPressure = (deformationEpisodeNo < InitialFluidPressure_list.Count ? InitialFluidPressure_list[deformationEpisodeNo] : InitialFluidPressure);
                Tensor2S nextInitialAbsoluteStess = (deformationEpisodeNo < InitialAbsoluteStress_list.Count ? InitialAbsoluteStress_list[deformationEpisodeNo] : InitialAbsoluteStress);

                // Create a new array for this deformation episode for each parameter
                double[,,] nextEhminAzi_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextEhminRate_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextEhmaxRate_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextAppliedOverpressureRate_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextAppliedTemperatureChange_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextAppliedUpliftRate_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextStressArchingFactor_array = new double[NoCols, NoRows, NoLayers];
                double[,,] nextDeformationEpisodeDuration_array = new double[NoCols, NoRows, NoLayers];
                Tensor2S[,,] nextAbsoluteStressRate_array = new Tensor2S[NoCols, NoRows, NoLayers];
                double[,,] nextInitialFluidPressure_array = new double[NoCols, NoRows, NoLayers];
                Tensor2S[,,] nextInitialAbsoluteStess_array = new Tensor2S[NoCols, NoRows, NoLayers];

                // Populate the new arrays with default values
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                    for (int RowNo = 0; RowNo < NoRows; RowNo++)
                        for (int LayerNo = 0; LayerNo < NoLayers; LayerNo++)
                        {
                            if (TestComplexGeometry)
                            {
                                double local_EhminAzi = 0;
                                if ((ColNo + RowNo) > 4)
                                    local_EhminAzi += Math.PI / 2;
                                nextEhminAzi_array[ColNo, RowNo, LayerNo] = ((ColNo + RowNo) % 2 == 1 ? local_EhminAzi - EhminCurvature : local_EhminAzi + EhminCurvature);

                            }
                            else if (VariableStrainOrientation)
                            {
                                double local_EhminAzi = nextEhminAzi + ((double)(ColNo - RowNo) * (EhminCurvature));
                                nextEhminAzi_array[ColNo, RowNo, LayerNo] = local_EhminAzi;
                            }
                            else
                            {
                                nextEhminAzi_array[ColNo, RowNo, LayerNo] = nextEhminAzi;
                            }
                            if (VariableStrainMagnitude)
                            {
                                //double local_EhminRate = nextEhminRate * (1 + ((double)(ColNo + RowNo) / 10));
                                //double local_EhmaxRate = nextEhmaxRate * (1 + ((double)(ColNo + RowNo) / 10));
                                double strainMultiplier = RandomNumberGenerator.NextDouble() * 2;
                                double local_EhminRate = nextEhminRate * strainMultiplier;
                                double local_EhmaxRate = nextEhmaxRate * strainMultiplier;
                                if (VariableStrainSmoothingFactor > 0)
                                {
                                    double neighbourEhminRate, neighbourEhmaxRate;
                                    if ((ColNo > 0) && (RowNo > 0))
                                    {
                                        neighbourEhminRate = (nextEhminRate_array[ColNo - 1, RowNo - 1, LayerNo] + nextEhminRate_array[ColNo, RowNo - 1, LayerNo] + nextEhminRate_array[ColNo - 1, RowNo, LayerNo]) / 3;
                                        neighbourEhmaxRate = (nextEhmaxRate_array[ColNo - 1, RowNo - 1, LayerNo] + nextEhmaxRate_array[ColNo, RowNo - 1, LayerNo] + nextEhmaxRate_array[ColNo - 1, RowNo, LayerNo]) / 3;
                                    }
                                    else if (ColNo > 0)
                                    {
                                        neighbourEhminRate = nextEhminRate_array[ColNo - 1, RowNo, LayerNo];
                                        neighbourEhmaxRate = nextEhmaxRate_array[ColNo - 1, RowNo, LayerNo];
                                    }
                                    else if (RowNo > 0)
                                    {
                                        neighbourEhminRate = nextEhminRate_array[ColNo, RowNo - 1, LayerNo];
                                        neighbourEhmaxRate = nextEhmaxRate_array[ColNo, RowNo - 1, LayerNo];
                                    }
                                    else
                                    {
                                        neighbourEhminRate = nextEhminRate;
                                        neighbourEhmaxRate = nextEhmaxRate;
                                    }
                                    nextEhminRate_array[ColNo, RowNo, LayerNo] = ((neighbourEhminRate * VariableStrainSmoothingFactor) + local_EhminRate) / (VariableStrainSmoothingFactor + 1);
                                    nextEhmaxRate_array[ColNo, RowNo, LayerNo] = ((neighbourEhmaxRate * VariableStrainSmoothingFactor) + local_EhmaxRate) / (VariableStrainSmoothingFactor + 1);
                                }
                                else
                                {
                                    nextEhminRate_array[ColNo, RowNo, LayerNo] = local_EhminRate;
                                    nextEhmaxRate_array[ColNo, RowNo, LayerNo] = local_EhmaxRate;
                                }
                            }
                            else
                            {
                                nextEhminRate_array[ColNo, RowNo, LayerNo] = nextEhminRate;
                                nextEhmaxRate_array[ColNo, RowNo, LayerNo] = nextEhmaxRate;
                            }
                            nextAppliedOverpressureRate_array[ColNo, RowNo, LayerNo] = nextAppliedOverpressureRate;
                            nextAppliedTemperatureChange_array[ColNo, RowNo, LayerNo] = nextAppliedTemperatureChange;
                            nextAppliedUpliftRate_array[ColNo, RowNo, LayerNo] = nextAppliedUpliftRate;
                            nextStressArchingFactor_array[ColNo, RowNo, LayerNo] = nextStressArchingFactor;
                            nextDeformationEpisodeDuration_array[ColNo, RowNo, LayerNo] = nextDeformationEpisodeDuration;
                            nextAbsoluteStressRate_array[ColNo, RowNo, LayerNo] = nextAbsoluteStressRate;
                            nextInitialFluidPressure_array[ColNo, RowNo, LayerNo] = nextInitialFluidPressure;
                            nextInitialAbsoluteStess_array[ColNo, RowNo, LayerNo] = nextInitialAbsoluteStess;
                            if (VariableStrainOrientation)
                            {
                                double local_EhminAzi = nextEhminAzi + ((double)(ColNo - RowNo + LayerNo) * (EhminCurvature));
                                Tensor2S local_AbsoluteStressRate = new Tensor2S(-1300000 * Math.Pow(Math.Cos(local_EhminAzi), 2), -1300000 * Math.Pow(Math.Sin(local_EhminAzi), 2), 0, 1300000 * Math.Sin(local_EhminAzi) * Math.Cos(local_EhminAzi), 0, 0);
                                nextAbsoluteStressRate_array[ColNo, RowNo, LayerNo] = local_AbsoluteStressRate;
                            }
                        }

                // Add the new arrays to the appropriate list
                EhminAzi_array.Add(nextEhminAzi_array);
                EhminRate_array.Add(nextEhminRate_array);
                EhmaxRate_array.Add(nextEhmaxRate_array);
                AppliedOverpressureRate_array.Add(nextAppliedOverpressureRate_array);
                AppliedTemperatureChange_array.Add(nextAppliedTemperatureChange_array);
                AppliedUpliftRate_array.Add(nextAppliedUpliftRate_array);
                StressArchingFactor_array.Add(nextStressArchingFactor_array);
                DeformationEpisodeDuration_array.Add(nextDeformationEpisodeDuration_array);
                AbsoluteStressRate_array.Add(nextAbsoluteStressRate_array);
                InitialFluidPressure_array.Add(nextInitialFluidPressure_array);
                InitialAbsoluteStress_array.Add(nextInitialAbsoluteStess_array);
            }

            // Create arrays for variable mechanical property parameters, depth at start of deformation and present day effective stress, and populate them with default values
            double[,,] YoungsMod_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PoissonsRatio_array = new double[NoCols, NoRows, NoLayers];
            double[,,] Porosity_array = new double[NoCols, NoRows, NoLayers];
            double[,,] BiotCoefficient_array = new double[NoCols, NoRows, NoLayers];
            double[,,] ThermalExpansionCoefficient_array = new double[NoCols, NoRows, NoLayers];
            double[,,] FrictionCoefficient_array = new double[NoCols, NoRows, NoLayers];
            double[,,] CrackSurfaceEnergy_array = new double[NoCols, NoRows, NoLayers];
            double[,,] SubcriticalPropIndex_array = new double[NoCols, NoRows, NoLayers];
            double[,,] RockStrainRelaxation_array = new double[NoCols, NoRows, NoLayers];
            double[,,] FractureRelaxation_array = new double[NoCols, NoRows, NoLayers];
            double[,,] InitialMicrofractureDensity_array = new double[NoCols, NoRows, NoLayers];
            double[,,] InitialMicrofractureSizeDistribution_array = new double[NoCols, NoRows, NoLayers];
            double[,,] InitialMicrofractureMedianRadius_array = new double[NoCols, NoRows, NoLayers];
            double[,,] HostRock_kh_array = new double[NoCols, NoRows, NoLayers];
            double[,,] HostRock_kv_array = new double[NoCols, NoRows, NoLayers];
            double[,,] DepthAtDeformation_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PresentDayEffectiveStress_XX_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PresentDayEffectiveStress_YY_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PresentDayEffectiveStress_ZZ_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PresentDayEffectiveStress_XY_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PresentDayEffectiveStress_YZ_array = new double[NoCols, NoRows, NoLayers];
            double[,,] PresentDayEffectiveStress_ZX_array = new double[NoCols, NoRows, NoLayers];
            List<Cleavage>[,,] Cleavages_array = new List<Cleavage>[NoCols, NoRows, NoLayers];
            for (int ColNo = 0; ColNo < NoCols; ColNo++)
                for (int RowNo = 0; RowNo < NoRows; RowNo++)
                    for (int LayerNo = 0; LayerNo < NoLayers; LayerNo++)
                    {
                        // Set local values for mechanical properties and add them to the appropriate arrays
                        if (VariableYoungsMod)
                        {
                            //YoungsMod_array[ColNo, RowNo] = YoungsMod * (1 + (((double)(NoRows - RowNo) / (double)NoRows) * 0.05));
                            double youngsModMultiplier = RandomNumberGenerator.NextDouble() * 2;
                            double local_YoungsMod = YoungsMod * youngsModMultiplier;
                            if (VariableYoungsModSmoothingFactor >= 0)
                            {
                                double neighbourYoungsMod;
                                if ((RowNo > 0) && (ColNo > 0))
                                {
                                    neighbourYoungsMod = (YoungsMod_array[ColNo - 1, RowNo - 1, LayerNo] + YoungsMod_array[ColNo - 1, RowNo, LayerNo] + YoungsMod_array[ColNo, RowNo - 1, LayerNo]) / 3;
                                }
                                else if (RowNo > 0)
                                {
                                    neighbourYoungsMod = YoungsMod_array[ColNo, RowNo - 1, LayerNo];
                                }
                                else if (ColNo > 0)
                                {
                                    neighbourYoungsMod = YoungsMod_array[ColNo - 1, RowNo, LayerNo];
                                }
                                else
                                {
                                    neighbourYoungsMod = YoungsMod;
                                }
                                YoungsMod_array[ColNo, RowNo, LayerNo] = ((neighbourYoungsMod * VariableYoungsModSmoothingFactor) + local_YoungsMod) / (VariableYoungsModSmoothingFactor + 1);
                                //Console.WriteLine(string.Format("Cell {0},{1},{2}: ehmin rate {3}, ehmax rate {4}, Youngs Mod {5}", ColNo, RowNo, LayerNo, EhminRate_array[0][ColNo, RowNo, LayerNo], EhmaxRate_array[0][ColNo, RowNo, LayerNo], YoungsMod_array[ColNo, RowNo, LayerNo]));
                            }
                            else
                            {
                                YoungsMod_array[ColNo, RowNo, LayerNo] = YoungsMod * (1 + (((double)(RowNo) / (double)NoRows) * 1));
                            }
                        }
                        else
                        {
                            YoungsMod_array[ColNo, RowNo, LayerNo] = YoungsMod;
                        }
                        PoissonsRatio_array[ColNo, RowNo, LayerNo] = PoissonsRatio;
                        Porosity_array[ColNo, RowNo, LayerNo] = Porosity;
                        BiotCoefficient_array[ColNo, RowNo, LayerNo] = BiotCoefficient;
                        ThermalExpansionCoefficient_array[ColNo, RowNo, LayerNo] = ThermalExpansionCoefficient;
                        if (VariableCSE)
                            CrackSurfaceEnergy_array[ColNo, RowNo, LayerNo] = CrackSurfaceEnergy * (1 + (((double)(RowNo) / (double)NoRows) * 1));
                        else
                            CrackSurfaceEnergy_array[ColNo, RowNo, LayerNo] = CrackSurfaceEnergy;
                        if (VariableFriction)
                            FrictionCoefficient_array[ColNo, RowNo, LayerNo] = FrictionCoefficient * (1 + (((double)(RowNo) / (double)NoRows) * 1));
                        else
                            FrictionCoefficient_array[ColNo, RowNo, LayerNo] = FrictionCoefficient;
                        SubcriticalPropIndex_array[ColNo, RowNo, LayerNo] = SubcriticalPropIndex;
                        RockStrainRelaxation_array[ColNo, RowNo, LayerNo] = RockStrainRelaxation;
                        FractureRelaxation_array[ColNo, RowNo, LayerNo] = FractureRelaxation;
                        InitialMicrofractureDensity_array[ColNo, RowNo, LayerNo] = InitialMicrofractureDensity;
                        InitialMicrofractureSizeDistribution_array[ColNo, RowNo, LayerNo] = InitialMicrofractureSizeDistribution;
                        InitialMicrofractureMedianRadius_array[ColNo, RowNo, LayerNo] = InitialMicrofractureMedianRadius;
                        HostRock_kh_array[ColNo, RowNo, LayerNo] = HostRock_kh;
                        HostRock_kv_array[ColNo, RowNo, LayerNo] = HostRock_kv;
                        DepthAtDeformation_array[ColNo, RowNo, LayerNo] = DepthAtDeformation;
                        PresentDayEffectiveStress_XX_array[ColNo, RowNo, LayerNo] = PresentDayEffectiveStress_XX;
                        PresentDayEffectiveStress_YY_array[ColNo, RowNo, LayerNo] = PresentDayEffectiveStress_YY;
                        PresentDayEffectiveStress_ZZ_array[ColNo, RowNo, LayerNo] = PresentDayEffectiveStress_ZZ;
                        PresentDayEffectiveStress_XY_array[ColNo, RowNo, LayerNo] = PresentDayEffectiveStress_XY;
                        PresentDayEffectiveStress_YZ_array[ColNo, RowNo, LayerNo] = PresentDayEffectiveStress_YZ;
                        PresentDayEffectiveStress_ZX_array[ColNo, RowNo, LayerNo] = PresentDayEffectiveStress_ZX;
                        Cleavages_array[ColNo, RowNo, LayerNo] = new List<Cleavage>();
                        foreach (Cleavage cleavage in Cleavages)
                            Cleavages_array[ColNo, RowNo, LayerNo].Add(new Cleavage(cleavage));
                    }

            // Create arrays for the gridblock cornerpoints and populate them
            // Note that the cornerpoint with coordinates [ColNo, RowNo, LayerNo] will refer to the SW bottom cornerpoint of the gridblock with coordinates [ColNo, RowNo, LayerNo]
            PointXYZ[,,] CornerPoints = new PointXYZ[NoCols + 1, NoRows + 1, NoLayers + 1];
            for (int LayerNo = 0; LayerNo <= NoLayers; LayerNo++)
            {
                double z_coord = -(Depth + ((double)(NoLayers - LayerNo) * LayerThickness));

                for (int RowNo = 0; RowNo <= NoRows; RowNo++)
                {
                    double y_coord = ((double)RowNo * Length_NS) + OriginYOffset;

                    for (int ColNo = 0; ColNo <= NoCols; ColNo++)
                    {
                        double x_coord = ((double)ColNo * Width_EW) + OriginXOffset;

                        PointXYZ Cornerpoint;
                        if (TestComplexGeometry)
                        {
                            if ((LayerNo % 2) == 1)
                                Cornerpoint = new PointXYZ(x_coord - ((RowNo % 2) == 1 ? 2 : -2), y_coord - ((ColNo % 2) == 1 ? 2 : -2), z_coord - (LayerThickness * ((ColNo % 2) == 1 ? 0.1 : -0.1)));
                            else
                                Cornerpoint = new PointXYZ(x_coord, y_coord, z_coord - (LayerThickness * ((RowNo % 2) == 1 ? 0.1 : -0.1)));
                        }
                        else
                        {
                            Cornerpoint = new PointXYZ(x_coord, y_coord, z_coord);
                        }
                        CornerPoints[ColNo, RowNo, LayerNo] = Cornerpoint;
                    }
                }
            }

#if READINPUTFROMFILE
            // Read include files and set property and geometry overrides
            foreach (string includefile_name in IncludeFiles)
            {
                // Check if the file actually exists
                if (!File.Exists(includefile_name))
                {
                    Console.WriteLine(string.Format("Warning! Could not find include file {0}", includefile_name));
                    continue;
                }
                else
                {
                    Console.WriteLine(string.Format("Reading include file {0}", includefile_name));

                    // Read the file data and sort it into properties
                    List<List<string>> Properties = new List<List<string>>();
                    List<string> PropertyData = new List<string>();
                    string[] includefile_lines = File.ReadAllLines(includefile_name);
                    foreach (string line in includefile_lines)
                    {
                        // Ignore comment lines
                        if (line.StartsWith("%"))
                            continue;

                        // Split the line into items
                        string[] line_split = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string item in line_split)
                        {
                            // Check if the item starts with a hash - this indicates a new property
                            if (item.StartsWith("#"))
                            {
                                // Create a new property list, add it to the Properties list, and add the property name to the start of it
                                PropertyData = new List<string>();
                                Properties.Add(PropertyData);
                                PropertyData.Add(item.TrimStart('#'));
                            }
                            // Otherwise just add the item to the current PropertyData list
                            else
                            {
                                PropertyData.Add(item);
                            }
                        }
                    }

                    // Loop through each of the properties
                    foreach (List<string> PropertyOverrideData in Properties)
                    {
                        if (PropertyOverrideData.Count < 2)
                            continue;

                        // Get the name of the property and remove it from the list
                        string propertyName = PropertyOverrideData[0];
                        PropertyOverrideData.RemoveAt(0);

                        // Geometry data must be processed differently
                        if (propertyName == "Geometry")
                        {
                            Console.WriteLine(string.Format("Reading values for grid geometry"));

                            // Get the number of data items, and give a warning if this does not match the number of cornerpoints
                            int noDataItems = PropertyOverrideData.Count / 3;
                            int noCornerPoints = (NoCols + 1) * (NoRows + 1) * (NoLayers + 1);
                            if (noDataItems > noCornerPoints)
                            {
                                Console.WriteLine(string.Format("Note: the number of specified cornerpoint locations is greater than the number of cornerpoints; excess values will be ignored"));
                                noDataItems = noCornerPoints;
                            }
                            else if (noDataItems < noCornerPoints)
                                Console.WriteLine(string.Format("Note: the number of specified cornerpoint locations is less than the number of cornerpoints; excess cornerpoints will be set to default locations"));

                            // Loop through all the items in the data list
                            // NB each item consists of 3 values: CornerpointX, CornerpointY, CornerpointZ
                            for (int itemNo = 0; itemNo < noDataItems; itemNo++)
                            {
                                // Get the row, column and layer index for this item
                                int noPointsPerLayer = (NoCols + 1) * (NoRows + 1);
                                int layerNo = itemNo / noPointsPerLayer;
                                int rowNo = (itemNo % noPointsPerLayer) / (NoRows + 1);
                                int colNo = (itemNo % noPointsPerLayer) % (NoRows + 1);

                                // Write the data item to the appropriate array
                                // Catch any exceptions due to invalid data formats
                                try
                                {
                                    // Get the data items
                                    double CornerPointX = Convert.ToDouble(PropertyOverrideData[(itemNo * 3)]);
                                    double CornerPointY = Convert.ToDouble(PropertyOverrideData[(itemNo * 3) + 1]);
                                    double CornerPointZ = Convert.ToDouble(PropertyOverrideData[(itemNo * 3) + 2]);

                                    CornerPoints[rowNo, colNo, layerNo] = new PointXYZ(CornerPointX, CornerPointY, CornerPointZ);
                                }
                                catch (System.FormatException)
                                {
                                    Console.WriteLine(string.Format("Warning! Could not read data for cornerpoint {0},{1},{2}", colNo, rowNo, layerNo));
                                    Console.WriteLine("Data will be ignored");
                                }
                            } // Next item in the data list  
                        }
                        // Process property array data
                        else
                        {
                            // Check if the first item is a deformation episode index; if so get it and remove it from the list
                            // Otherwise default to the first deformation episode
                            int deformationEpisodeIndex = 0;
                            if (PropertyOverrideData[0].StartsWith("["))
                            {
                                string deformationEpisodeIndexString = PropertyOverrideData[0].TrimStart('[').TrimEnd(']');

                                try
                                {
                                    // Convert the deformation episode number to a zero-based index
                                    deformationEpisodeIndex = Convert.ToInt32(deformationEpisodeIndexString) - 1;
                                    if (deformationEpisodeIndex < 0)
                                        deformationEpisodeIndex = 0;
                                }
                                catch (System.FormatException)
                                {
                                    Console.WriteLine(string.Format("Warning! Could not read deformation episode number for property {0}, episode {1}", propertyName, deformationEpisodeIndexString));
                                    Console.WriteLine("Data will be written to the first deformation episode");
                                }

                                // Remove the deformation episode index from the list
                                PropertyOverrideData.RemoveAt(0);
                            }

                            // Get a handle to the property data array for the specified property
                            // If the property name is not recognised, abort
                            double[,,] propertyArray;
                            bool deformationEpisodeSpecified = false;
                            switch (propertyName)
                            {
                                case "EhminAzi":
                                case "Epsilon_hmin_azimuth_in": // For backwards compatibility
                                    try
                                    {
                                        propertyArray = EhminAzi_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "EhminRate":
                                case "Epsilon_hmin_dashed_in": // For backwards compatibility
                                    try
                                    {
                                        propertyArray = EhminRate_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "EhmaxRate":
                                case "Epsilon_hmax_dashed_in": // For backwards compatibility
                                    try
                                    {
                                        propertyArray = EhmaxRate_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "AppliedOverpressureRate":
                                    try
                                    {
                                        propertyArray = AppliedOverpressureRate_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "AppliedTemperatureChange":
                                    try
                                    {
                                        propertyArray = AppliedTemperatureChange_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "AppliedUpliftRate":
                                    try
                                    {
                                        propertyArray = AppliedUpliftRate_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "StressArchingFactor":
                                    try
                                    {
                                        propertyArray = StressArchingFactor_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;
                                case "DeformationEpisodeDuration":
                                case "DeformationStageDuration": // For backwards compatibility
                                case "DeformationStageDuration_in": // For backwards compatibility
                                    try
                                    {
                                        propertyArray = DeformationEpisodeDuration_array[deformationEpisodeIndex];
                                        deformationEpisodeSpecified = true;
                                    }
                                    catch (System.IndexOutOfRangeException)
                                    {
                                        Console.WriteLine(string.Format("Warning! Deformation episode {0} is not defined for property {1}", deformationEpisodeIndex + 1, propertyName));
                                        Console.WriteLine("Data will be ignored");
                                        continue;
                                    }
                                    break;

                                case "YoungsMod":
                                    propertyArray = YoungsMod_array;
                                    break;
                                case "PoissonsRatio":
                                    propertyArray = PoissonsRatio_array;
                                    break;
                                case "Porosity":
                                    propertyArray = Porosity_array;
                                    break;
                                case "BiotCoefficient":
                                    propertyArray = BiotCoefficient_array;
                                    break;
                                case "ThermalExpansionCoefficient":
                                    propertyArray = ThermalExpansionCoefficient_array;
                                    break;
                                case "CrackSurfaceEnergy":
                                    propertyArray = CrackSurfaceEnergy_array;
                                    break;
                                case "FrictionCoefficient":
                                    propertyArray = FrictionCoefficient_array;
                                    break;
                                case "RockStrainRelaxation":
                                    propertyArray = RockStrainRelaxation_array;
                                    break;
                                case "FractureRelaxation":
                                    propertyArray = FractureRelaxation_array;
                                    break;
                                case "InitialMicrofractureDensity":
                                case "B": // For backwards compatibility
                                    propertyArray = InitialMicrofractureDensity_array;
                                    break;
                                case "InitialMicrofractureSizeDistribution":
                                case "c": // For backwards compatibility
                                    propertyArray = InitialMicrofractureSizeDistribution_array;
                                    break;
                                case "InitialMicrofractureMedianRadius":
                                    propertyArray = InitialMicrofractureMedianRadius_array;
                                    break;
                                case "SubcriticalPropIndex":
                                case "b": // For backwards compatibility
                                    propertyArray = SubcriticalPropIndex_array;
                                    break;
                                case "HostRock_kh":
                                    propertyArray = HostRock_kh_array;
                                    break;
                                case "HostRock_kv":
                                    propertyArray = HostRock_kv_array;
                                    break;
                                case "DepthAtDeformation":
                                case "DepthAtFracture": // For backwards compatibility
                                    propertyArray = DepthAtDeformation_array;
                                    break;
                                case "PresentDayEffectiveStress_XX":
                                    propertyArray = PresentDayEffectiveStress_XX_array;
                                    break;
                                case "PresentDayEffectiveStress_YY":
                                    propertyArray = PresentDayEffectiveStress_YY_array;
                                    break;
                                case "PresentDayEffectiveStress_ZZ":
                                    propertyArray = PresentDayEffectiveStress_ZZ_array;
                                    break;
                                case "PresentDayEffectiveStress_XY":
                                    propertyArray = PresentDayEffectiveStress_XY_array;
                                    break;
                                case "PresentDayEffectiveStress_YZ":
                                    propertyArray = PresentDayEffectiveStress_YZ_array;
                                    break;
                                case "PresentDayEffectiveStress_ZX":
                                    propertyArray = PresentDayEffectiveStress_ZX_array;
                                    break;

                                default:
                                    Console.WriteLine(string.Format("Warning! Property name {0} is not recognised", propertyName));
                                    continue;
                            }

                            if (deformationEpisodeSpecified)
                                Console.WriteLine(string.Format("Reading values for property {0}, deformation episode {1}", propertyName, deformationEpisodeIndex + 1));
                            else
                                Console.WriteLine(string.Format("Reading values for property {0}", propertyName));

                            // Get the number of data items, and give a warning if this does not match the number of gridblocks
                            int noDataItems = PropertyOverrideData.Count;
                            int noGridblocks = NoCols * NoRows * NoLayers;
                            if (noDataItems > noGridblocks)
                            {
                                Console.WriteLine(string.Format("Warning! The number of specified {0} values is greater than the number of gridblocks; excess values will be ignored", propertyName));
                                noDataItems = noGridblocks;
                            }
                            else if (noDataItems < noGridblocks)
                            {
                                Console.WriteLine(string.Format("Warning! The number of specified {0} values is less than the number of gridblocks; excess gridblocks will be set to default values", propertyName));
                            }

                            // Loop through all the items in the data list
                            for (int itemNo = 0; itemNo < noDataItems; itemNo++)
                            {
                                // Get the data item, and check to see if it is set to the null value
                                string dataItem = PropertyOverrideData[itemNo];
                                if (dataItem == "NA")
                                    continue;

                                // Get the row, column and layer index for this item
                                int noPointsPerLayer = NoCols * NoRows;
                                int layerNo = itemNo / noPointsPerLayer;
                                int rowNo = (itemNo % noPointsPerLayer) / NoRows;
                                int colNo = (itemNo % noPointsPerLayer) % NoRows;

                                // Write the data item to the appropriate array
                                // Catch any exceptions due to invalid data formats
                                try
                                {
                                    propertyArray[rowNo, colNo, layerNo] = Convert.ToDouble(dataItem);
                                }
                                catch (System.FormatException)
                                {
                                    Console.WriteLine(string.Format("Warning! {0} is an invalid format for {1}", dataItem, propertyName));
                                    Console.WriteLine("Data will be ignored");
                                }
                            } // Next item in the data list
                        } // End process property array 
                    } // Next property array
                } // End check if file exists
            } // Next include file

            // Set overrides for individual gridblocks
            foreach (List<string> NextGBoverride in GBoverrides)
            {
                // Check if there is any data; if not move on to the next gridblock override
                if (NextGBoverride.Count < 1)
                    continue;

                // Set the row and column numbers for the gridblock to override
                int ColNo = -1;
                int RowNo = -1;
                int LayerNo = -1;
                // Catch any exceptions due to invalid data formats
                try
                {
                    string[] firstline_split = NextGBoverride[0].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (firstline_split.Length < 4)
                    {
                        Console.WriteLine("Warning! Invalid format for gridblock coordinates");
                        Console.WriteLine("Data will be ignored");
                        continue;
                    }
                    else
                    {
                        // In the input data, the first coordinate is the column number (E-W), the second coordinate is the row number (N-S) and the third coordinate is the layer number (from bottom to top)
                        // NB The order of the column and row coordinates is reversed in the Grid object interface
                        // Also in the input data the coordinates a 1-based but in the Grid object interface they are 0-based
                        ColNo = Convert.ToInt32(firstline_split[1]);
                        RowNo = Convert.ToInt32(firstline_split[2]);
                        LayerNo = Convert.ToInt32(firstline_split[3]);
                    }
                    if ((ColNo < 1) || (ColNo > NoCols) || (RowNo < 1) || (RowNo > NoRows) || (LayerNo < 1) || (LayerNo > NoLayers))
                    {
                        Console.WriteLine(string.Format("Warning! Gridblock coordinate {0},{1},{2} is out of range", ColNo, RowNo, LayerNo));
                        Console.WriteLine("Data will be ignored");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Overrides for gridblock {0},{1},{2}", ColNo, RowNo, LayerNo));
                        // Convert to zero-based coordinates
                        ColNo--;
                        RowNo--;
                        LayerNo--;
                    }
                }
                catch (System.FormatException)
                {
                    Console.WriteLine("Warning! Invalid format for gridblock coordinates");
                    Console.WriteLine("Data will be ignored");
                    continue;
                }

                foreach (string line in NextGBoverride)
                {
                    // First find any gridblock override blocks
                    // Ignore comment lines
                    if (line.StartsWith("%"))
                        continue;

                    // Split the line into components and echo to console
                    // All lines should have at least 2 components; if not we can ignore them
                    string[] line_split = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (line_split.Length < 2)
                        continue;

                    // Check if this is an include statement and if so ignore
                    if (line_split[0] == "Include")
                        continue;

                    // Now we can process specified values
                    // Catch any exceptions due to invalid data formats
                    try
                    {
                        // Check for start and end block markers
                        if (line_split[0] == "Gridblock")
                            continue;
                        if (line_split[0] == "End")
                            break;

                        Console.WriteLine(line_split[0] + " " + line_split[1]);

                        switch (line_split[0])
                        {
                            // Strain orientatation
                            case "EhminAzi":
                            case "Epsilon_hmin_azimuth_in": // For backwards compatibility
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > EhminAzi_array.Count)
                                        noValues = EhminAzi_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        EhminAzi_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "EhminRate":
                            case "Epsilon_hmin_dashed_in": // For backwards compatibility
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > EhminRate_array.Count)
                                        noValues = EhminRate_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        EhminRate_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "EhmaxRate":
                            case "Epsilon_hmax_dashed_in": // For backwards compatibility
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > EhmaxRate_array.Count)
                                        noValues = EhmaxRate_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        EhmaxRate_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "AppliedOverpressureRate":
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > AppliedOverpressureRate_array.Count)
                                        noValues = AppliedOverpressureRate_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        AppliedOverpressureRate_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "AppliedTemperatureChange":
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > AppliedTemperatureChange_array.Count)
                                        noValues = AppliedTemperatureChange_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        AppliedTemperatureChange_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "AppliedUpliftRate":
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > AppliedUpliftRate_array.Count)
                                        noValues = AppliedUpliftRate_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        AppliedUpliftRate_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "StressArchingFactor":
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > StressArchingFactor_array.Count)
                                        noValues = StressArchingFactor_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        StressArchingFactor_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;
                            case "DeformationEpisodeDuration":
                            case "DeformationStageDuration": // For backwards compatibility
                            case "DeformationStageDuration_in": // For backwards compatibility
                                {
                                    int noValues = line_split.GetLength(0);
                                    if (noValues > DeformationEpisodeDuration_array.Count)
                                        noValues = DeformationEpisodeDuration_array.Count;
                                    for (int valueNo = 1; valueNo < noValues; valueNo++)
                                        DeformationEpisodeDuration_array[valueNo - 1][ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[valueNo]);
                                }
                                break;

                            case "YoungsMod":
                                YoungsMod_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PoissonsRatio":
                                PoissonsRatio_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "Porosity":
                                Porosity_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "BiotCoefficient":
                                BiotCoefficient_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "ThermalExpansionCoefficient":
                                ThermalExpansionCoefficient_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "CrackSurfaceEnergy":
                                CrackSurfaceEnergy_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "FrictionCoefficient":
                                FrictionCoefficient_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "RockStrainRelaxation":
                                RockStrainRelaxation_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "FractureRelaxation":
                                FractureRelaxation_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "InitialMicrofractureDensity":
                            case "B": // For backwards compatibility
                                InitialMicrofractureDensity_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "InitialMicrofractureSizeDistribution":
                            case "c": // For backwards compatibility
                                InitialMicrofractureSizeDistribution_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "InitialMicrofractureMedianRadius":
                                InitialMicrofractureMedianRadius_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "SubcriticalPropIndex":
                            case "b": // For backwards compatibility
                                SubcriticalPropIndex_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "HostRock_kh":
                                HostRock_kh_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "HostRock_kv":
                                HostRock_kv_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "DepthAtDeformation":
                            case "DepthAtFracture": // For backwards compatibility
                                DepthAtDeformation_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PresentDayEffectiveStress_XX":
                                PresentDayEffectiveStress_XX_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PresentDayEffectiveStress_YY":
                                PresentDayEffectiveStress_YY_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PresentDayEffectiveStress_ZZ":
                                PresentDayEffectiveStress_ZZ_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PresentDayEffectiveStress_XY":
                                PresentDayEffectiveStress_XY_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PresentDayEffectiveStress_YZ":
                                PresentDayEffectiveStress_YZ_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;
                            case "PresentDayEffectiveStress_ZX":
                                PresentDayEffectiveStress_ZX_array[ColNo, RowNo, LayerNo] = Convert.ToDouble(line_split[1]);
                                break;

                            case "SWTopCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo, RowNo, LayerNo + 1] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "SWBottomCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo, RowNo, LayerNo] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "NWTopCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo + 1, RowNo, LayerNo + 1] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "NWBottomCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo + 1, RowNo, LayerNo] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "NETopCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo + 1, RowNo + 1, LayerNo + 1] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "NEBottomCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo + 1, RowNo + 1, LayerNo] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "SETopCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo, RowNo + 1, LayerNo + 1] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;
                            case "SEBottomCorner":
                                {
                                    if (line_split.Length < 4)
                                        Console.WriteLine(string.Format("Warning! Need to specify 3 coordinates for {0}", line_split[0]));
                                    else
                                        CornerPoints[ColNo, RowNo + 1, LayerNo] = new PointXYZ(Convert.ToDouble(line_split[1]), Convert.ToDouble(line_split[2]), Convert.ToDouble(line_split[3]));
                                }
                                break;

                            default:
                                Console.WriteLine(string.Format("Warning! Property name {0} is not recognised", line_split[0]));
                                continue;
                        }
                    }
                    catch (System.FormatException)
                    {
                        Console.WriteLine(string.Format("Warning! {0} is an invalid format for {1}", line_split[1], line_split[0]));
                        Console.WriteLine("Data will be ignored");
                    }
                }
            }
#endif

            // Progress reporter object - write progress updates to Console
            ConsoleProgressReporter progReporter = new ConsoleProgressReporter(10, true);

            // Create a grid and populate it with identical gridblocks
            FractureGrid ModelGrid = new FractureGrid(NoCols, NoRows, NoLayers);
            for (int LayerNo = 0; LayerNo < NoLayers; LayerNo++)
            {
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                {
                    for (int RowNo = 0; RowNo < NoRows; RowNo++)
                    {
                        // Get the cornerpoints for the new gridblock
                        PointXYZ SWtop, NWtop, NEtop, SEtop;
                        PointXYZ SWbottom, NWbottom, NEbottom, SEbottom;
                        SWtop = CornerPoints[ColNo, RowNo, LayerNo + 1];
                        NWtop = CornerPoints[ColNo, RowNo + 1, LayerNo + 1];
                        NEtop = CornerPoints[ColNo + 1, RowNo + 1, LayerNo + 1];
                        SEtop = CornerPoints[ColNo + 1, RowNo, LayerNo + 1];
                        SWbottom = CornerPoints[ColNo, RowNo, LayerNo];
                        NWbottom = CornerPoints[ColNo, RowNo + 1, LayerNo];
                        NEbottom = CornerPoints[ColNo + 1, RowNo + 1, LayerNo];
                        SEbottom = CornerPoints[ColNo + 1, RowNo, LayerNo];

                        // Calculate the mean depth of top surface and mean layer thickness at start of deformation - assume that these are equal to the current mean depth minus total specified uplift, and current layer thickness, respectively, unless the depth at the start of deformation has been specified
                        double local_LayerThickness = ((SWtop.Z - SWbottom.Z) + (NWtop.Z - NWbottom.Z) + (NEtop.Z - NEbottom.Z) + (SEtop.Z - SEbottom.Z)) / 4;
                        double local_Current_Depth = (SWtop.Depth + NWtop.Depth + NEtop.Depth + SEtop.Depth) / 4;
                        double local_DepthAtDeformation = DepthAtDeformation_array[ColNo, RowNo, LayerNo];
                        double local_Depth;
                        if (local_DepthAtDeformation > 0)
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
                                double local_AppliedUpliftRate = (deformationEpisodeNo < AppliedUpliftRate_array.Count ? AppliedUpliftRate_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : AppliedUpliftRate);
                                double local_DeformationEpisodeDuration = (deformationEpisodeNo < DeformationEpisodeDuration_array.Count ? DeformationEpisodeDuration_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : DeformationEpisodeDuration);
                                if (local_DeformationEpisodeDuration > 0)
                                    local_Depth += (local_AppliedUpliftRate * local_DeformationEpisodeDuration);
                            }
                        }

                        // Create a new gridblock object with the required layer thickness and depth
                        GridblockConfiguration gc = new GridblockConfiguration(local_LayerThickness, local_Depth);

                        // Set the gridblock cornerpoints
                        gc.setGridblockCorners(SWtop, SWbottom, NWtop, NWbottom, NEtop, NEbottom, SEtop, SEbottom);

                        // Get the mechanical properties for the gridblock
                        double local_YoungsMod = YoungsMod_array[ColNo, RowNo, LayerNo];
                        double local_PoissonsRatio = PoissonsRatio_array[ColNo, RowNo, LayerNo];
                        double local_Porosity = Porosity_array[ColNo, RowNo, LayerNo];
                        double local_BiotCoefficient = BiotCoefficient_array[ColNo, RowNo, LayerNo];
                        double local_ThermalExpansionCoefficient = ThermalExpansionCoefficient_array[ColNo, RowNo, LayerNo];
                        double local_CrackSurfaceEnergy = CrackSurfaceEnergy_array[ColNo, RowNo, LayerNo];
                        double local_FrictionCoefficient = FrictionCoefficient_array[ColNo, RowNo, LayerNo];
                        double local_RockStrainRelaxation = RockStrainRelaxation_array[ColNo, RowNo, LayerNo];
                        double local_FractureRelaxation = FractureRelaxation_array[ColNo, RowNo, LayerNo];
                        double local_SubcriticalPropIndex = SubcriticalPropIndex_array[ColNo, RowNo, LayerNo];
                        double local_HostRock_kh = HostRock_kh_array[ColNo, RowNo, LayerNo];
                        double local_HostRock_kv = HostRock_kv_array[ColNo, RowNo, LayerNo];

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

                        // Unless the fractures are forced to be planar, the default fracture azimuth for the gridblock will be defined based on the minimum horizontal strain azimuth for the first deformation episode
                        // If the minimum horizontal strain azimuth is not specified for the first deformation episode, it will be set to the default minimum horizontal strain azimuth
                        double local_DefaultFractureAzimuth = PlanarUnconfinedFractures ? 0 : (EhminAzi_array.Count > 0 ? EhminAzi_array[0][ColNo, RowNo, LayerNo] : EhminAzi);

                        // Set the propagation control data for the gridblock
                        gc.PropControl.setPropagationControl(OutputPopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio, MinimumMFClearZoneVolume,
                             MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, FractureNucleationPosition, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, ModelTimeUnits, OutputFracturePorosity, FractureApertureControl, OutputFracturePermeabilityTensor, PermeabilityAlgorithm, local_DefaultFractureAzimuth, PlanarUnconfinedFractures);
                        gc.PropControl.setUnconfinedFractureControl(Current_HistoricUCFP32TerminationRatio, Active_TotalUCRP30TerminationRatio, MinimumUCFClearZoneVolume, MinimumStaticUCRLength, MaxTimestepUCFP33Increase, MaxTimestepRadiusIncrease, Max_R_DeactivationCheck_interval, Min_R_ActivationProbability, ProportionalUCRIncrementToApply, Min_R_StaticDatapointSizeRatio, CullTSFrequency, CalculateImplicitUCFData, local_checkAllUCFStressShadows, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio);

                        // Set folder path for output files
                        gc.PropControl.FolderPath = folderPath;

#if DEBUG_FRACS
                        Console.WriteLine(string.Format("Cell {0} {1} {2} ", ColNo, RowNo, LayerNo));
                        Console.WriteLine(string.Format("SWtop {0} {1} {2}", SWtop.X, SWtop.Y, SWtop.Z));
                        Console.WriteLine(string.Format("NWtop {0} {1} {2}", NWtop.X, NWtop.Y, NWtop.Z));
                        Console.WriteLine(string.Format("NEtop {0} {1} {2}", NEtop.X, NEtop.Y, NEtop.Z));
                        Console.WriteLine(string.Format("SEtop {0} {1} {2}", SEtop.X, SEtop.Y, SEtop.Z));
                        Console.WriteLine(string.Format("SWbottom {0} {1} {2}", SWbottom.X, SWbottom.Y, SWbottom.Z));
                        Console.WriteLine(string.Format("NWbottom {0} {1} {2}", NWbottom.X, NWbottom.Y, NWbottom.Z));
                        Console.WriteLine(string.Format("NEbottom {0} {1} {2}", NEbottom.X, NEbottom.Y, NEbottom.Z));
                        Console.WriteLine(string.Format("SEbottom {0} {1} {2}", SEbottom.X, SEbottom.Y, SEbottom.Z));
                        Console.WriteLine(string.Format("LayerThickness = {0}; Depth = {1};", local_LayerThickness, local_Depth));
                        Console.WriteLine(string.Format("sv' {0}", gc.StressStrain.LithostaticStress_eff_Terzaghi));
                        Console.WriteLine(string.Format("Young's Mod: {0}, Poisson's ratio: {1}, Biot coefficient {2}, Crack surface energy:{3}, Friction coefficient:{4}", local_YoungsMod, local_PoissonsRatio, local_BiotCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient));
                        Console.WriteLine(string.Format("gc = new GridblockConfiguration({0}, {1}, {2});", local_LayerThickness, local_Depth, NoLayerBoundFractureSets));
                        Console.WriteLine(string.Format("gc.MechProps.setMechanicalProperties({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, TimeUnits.{11});", local_YoungsMod, local_PoissonsRatio, local_Porosity, local_BiotCoefficient, local_ThermalExpansionCoefficient, local_CrackSurfaceEnergy, local_FrictionCoefficient, local_RockStrainRelaxation, local_FractureRelaxation, CriticalPropagationRate, local_SubcriticalPropIndex, ModelTimeUnits));
                        Console.WriteLine(string.Format("gc.MechProps.setFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5});", DynamicApertureMultiplier, JRC, UCSRatio, InitialNormalStress, FractureNormalStiffness, MaximumClosure));
                        Console.WriteLine(string.Format("gc.MechProps.setHostRockPermeability({0}, {1});", local_HostRock_kh, local_HostRock_kv));
                        Console.WriteLine(string.Format("gc.StressStrain.setStressStrainState({0}, {1}, {2}, {3});", MeanOverlyingSedimentDensity, FluidDensity, InitialOverpressure, local_InitialStressRelaxation));
                        Console.WriteLine(string.Format("gc.StressStrain.GeothermalGradient = {0};", GeothermalGradient));
                        Console.WriteLine(string.Format("gc.PropControl.setPropagationControl({0}, {1}, {2}, {3}, {4}, {5}, StressDistribution.{6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, TimeUnits.{19}, {20}, FractureApertureType.{21}, {22}, PermeabilityAlgorithm.{23}, {24}, {25}); ",
                            OutputPopulationDistribution, No_l_indexPoints, MaxHMinLength, MaxHMaxLength, false, OutputBulkRockElasticTensors, StressDistributionScenario, MaxTimestepMFP33Increase, Current_HistoricMFP33TerminationRatio, Active_TotalMFP30TerminationRatio, MinimumMFClearZoneVolume,
                             MaxTimesteps, MaxTimestepDuration, No_r_bins, local_minImplicitMicrofractureRadius, FractureNucleationPosition, local_checkAlluFStressShadows, AnisotropyCutoff, WriteImplicitDataFiles, ModelTimeUnits, OutputFracturePorosity, FractureApertureControl, OutputFracturePermeabilityTensor, PermeabilityAlgorithm, local_DefaultFractureAzimuth, PlanarUnconfinedFractures));
                        Console.WriteLine(string.Format("gc.PropControl.setUnconfinedFractureControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14});", Current_HistoricUCFP32TerminationRatio, Active_TotalUCRP30TerminationRatio, MinimumUCFClearZoneVolume, MinimumStaticUCRLength, MaxTimestepUCFP33Increase, MaxTimestepRadiusIncrease, Max_R_DeactivationCheck_interval, Min_R_ActivationProbability, ProportionalUCRIncrementToApply, Min_R_StaticDatapointSizeRatio, CullTSFrequency, CalculateImplicitUCFData, local_checkAllUCFStressShadows, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio));
#endif

                        // Add the deformation load data 
                        for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
                        {
                            // Get the deformation load properties for this deformation episode in this gridblock
                            double local_EhminAzi = (deformationEpisodeNo < EhminAzi_array.Count ? EhminAzi_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : EhminAzi);
                            double local_EhminRate = (deformationEpisodeNo < EhminRate_array.Count ? EhminRate_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : EhminRate);
                            double local_EhmaxRate = (deformationEpisodeNo < EhmaxRate_array.Count ? EhmaxRate_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : EhmaxRate);
                            double local_AppliedOverpressureRate = (deformationEpisodeNo < AppliedOverpressureRate_array.Count ? AppliedOverpressureRate_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : AppliedOverpressureRate);
                            double local_AppliedTemperatureChange = (deformationEpisodeNo < AppliedTemperatureChange_array.Count ? AppliedTemperatureChange_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : AppliedTemperatureChange);
                            double local_AppliedUpliftRate = (deformationEpisodeNo < AppliedUpliftRate_array.Count ? AppliedUpliftRate_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : AppliedUpliftRate);
                            double local_StressArchingFactor = (deformationEpisodeNo < StressArchingFactor_array.Count ? StressArchingFactor_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : StressArchingFactor);
                            double local_DeformationEpisodeDuration = (deformationEpisodeNo < DeformationEpisodeDuration_array.Count ? DeformationEpisodeDuration_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : DeformationEpisodeDuration);
                            Tensor2S local_AbsoluteStressRate = (deformationEpisodeNo < AbsoluteStressRate_array.Count ? AbsoluteStressRate_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : AbsoluteStressRate);
                            double local_InitialFluidPressure = (deformationEpisodeNo < InitialFluidPressure_array.Count ? InitialFluidPressure_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : InitialFluidPressure);
                            Tensor2S local_InitialAbsoluteStress = (deformationEpisodeNo < InitialAbsoluteStress_array.Count ? InitialAbsoluteStress_array[deformationEpisodeNo][ColNo, RowNo, LayerNo] : InitialAbsoluteStress);

                            // Add the deformation episode to the deformation episode list in the PropControl object
                            if (local_AbsoluteStressRate is null)
                            {
                                gc.PropControl.AddDeformationEpisode_StrainLoad(local_EhminRate, local_EhmaxRate, local_EhminAzi, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor, local_DeformationEpisodeDuration);
#if DEBUG_FRACS
                                Console.WriteLine(string.Format("gc.PropControl.AddDeformationEpisode({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", local_EhminRate, local_EhmaxRate, local_EhminAzi, local_AppliedOverpressureRate, local_AppliedTemperatureChange, local_AppliedUpliftRate, local_StressArchingFactor, local_DeformationEpisodeDuration));
#endif
                            }
                            else
                            {
                                gc.PropControl.AddDeformationEpisode_AbsoluteStressLoad(local_AbsoluteStressRate, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialAbsoluteStress, local_InitialFluidPressure);
#if DEBUG_FRACS
                            string local_AbsoluteStressRate_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_AbsoluteStressRate.Component(Tensor2SComponents.XX), local_AbsoluteStressRate.Component(Tensor2SComponents.YY), local_AbsoluteStressRate.Component(Tensor2SComponents.ZZ), local_AbsoluteStressRate.Component(Tensor2SComponents.XY), local_AbsoluteStressRate.Component(Tensor2SComponents.YZ), local_AbsoluteStressRate.Component(Tensor2SComponents.ZX));
                            string local_InitialAbsoluteStress_info = string.Format("Tensor2S({0}, {1}, {2}, {3}, {4}, {5})", local_InitialAbsoluteStress.Component(Tensor2SComponents.XX), local_InitialAbsoluteStress.Component(Tensor2SComponents.YY), local_InitialAbsoluteStress.Component(Tensor2SComponents.ZZ), local_InitialAbsoluteStress.Component(Tensor2SComponents.XY), local_InitialAbsoluteStress.Component(Tensor2SComponents.YZ), local_InitialAbsoluteStress.Component(Tensor2SComponents.ZX));
                            Console.WriteLine(string.Format("gc.PropControl.AddDeformationEpisode({0}, {1}, {2}, {3}, {4});", local_AbsoluteStressRate_info, local_AppliedOverpressureRate, local_DeformationEpisodeDuration, local_InitialAbsoluteStress_info, local_InitialFluidPressure));
#endif
                            }
                        }

                        // Create the fracture sets
                        double local_InitialMicrofractureDensity = InitialMicrofractureDensity_array[ColNo, RowNo, LayerNo];
                        double local_InitialMicrofractureSizeDistribution = InitialMicrofractureSizeDistribution_array[ColNo, RowNo, LayerNo];
                        double local_InitialMicrofractureMedianRadius = InitialMicrofractureMedianRadius_array[ColNo, RowNo, LayerNo];
                        if (!(local_InitialMicrofractureMedianRadius > 0))
                            local_InitialMicrofractureMedianRadius = local_LayerThickness / 20;
                        if (Mode1Only)
                            gc.resetLayerBoundFractures(NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures);
                        else if (Mode2Only)
                            gc.resetLayerBoundFractures(NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures);
                        else
                            gc.resetLayerBoundFractures(NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, BiazimuthalConjugate, AllowReverseFractures);
                        if (NoUnconfinedFractureStrikeSets > 0)
                            gc.resetUnconfinedFractures(NoUnconfinedFractureStrikeSets, NoUnconfinedFractureDipSets, NoRaysPerUnconfinedFracture, local_minUnconfinedFractureRadius, local_maxUnconfinedFractureRadius, local_maxEffectiveUnconfinedFractureRadius, InitialMicrofractureDistributionFunction, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, local_InitialMicrofractureMedianRadius);

#if DEBUG_FRACS
                        if (Mode1Only)
                            Console.WriteLine(string.Format("gc.resetLayerBoundFractures({0}, {1}, {2}, FractureMode.{3}, {4});", NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode1, AllowReverseFractures));
                        else if (Mode2Only)
                            Console.WriteLine(string.Format("gc.resetLayerBoundFractures({0}, {1}, {2}, FractureMode.{3}, {4});", NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, FractureMode.Mode2, AllowReverseFractures));
                        else
                            Console.WriteLine(string.Format("gc.resetLayerBoundFractures({0}, {1}, {2}, {3}, {4});", NoLayerBoundFractureSets, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, BiazimuthalConjugate, AllowReverseFractures));
                        if (NoUnconfinedFractureStrikeSets > 0)
                            Console.WriteLine(string.Format("gc.resetUnconfinedFractures({0}, {1}, {2}, {3}, {4}, {5}, InitialFractureDistribution.{6}, {7}, {8}, {9});", NoUnconfinedFractureStrikeSets, NoUnconfinedFractureDipSets, NoRaysPerUnconfinedFracture, local_minUnconfinedFractureRadius, local_maxUnconfinedFractureRadius, local_maxEffectiveUnconfinedFractureRadius, InitialMicrofractureDistributionFunction, local_InitialMicrofractureDensity, local_InitialMicrofractureSizeDistribution, local_InitialMicrofractureMedianRadius));
#endif
                        // NB the fracture aperture control data must be set after the present day stress is defined, as it may be dependent on it

                        // Set the present day effective stress tensor, if required
                        if (UsePresentDayStress)
                        {
                            Tensor2S local_PresentDayEffectiveStress = new Tensor2S(PresentDayEffectiveStress_XX_array[ColNo, RowNo, LayerNo], PresentDayEffectiveStress_YY_array[ColNo, RowNo, LayerNo], PresentDayEffectiveStress_ZZ_array[ColNo, RowNo, LayerNo],
                                PresentDayEffectiveStress_XY_array[ColNo, RowNo, LayerNo], PresentDayEffectiveStress_YZ_array[ColNo, RowNo, LayerNo], PresentDayEffectiveStress_ZX_array[ColNo, RowNo, LayerNo]);
                            gc.SetPresentDayStress(local_PresentDayEffectiveStress);
                            //gc.SetPresentDayStressFromStrain(0, 0, 0, 0, double.NaN, double.NaN, double.NaN, double.NaN);
                        }

                        // Set the fracture aperture control data
                        gc.SetFractureApertureControlData(Mode1HMin_UniformAperture, Mode2HMin_UniformAperture, Mode1HMax_UniformAperture, Mode2HMax_UniformAperture, Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier);
#if DEBUG_FRACS
                        Console.WriteLine(string.Format("gc.SetFractureApertureControlData({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", Mode1HMin_UniformAperture, Mode2HMin_UniformAperture, Mode1HMax_UniformAperture, Mode2HMax_UniformAperture, Mode1HMin_SizeDependentApertureMultiplier, Mode2HMin_SizeDependentApertureMultiplier, Mode1HMax_SizeDependentApertureMultiplier, Mode2HMax_SizeDependentApertureMultiplier));
#endif

                        // Set the mechanical property overrides for any fracture sets parallel to the defined cleavages
                        // NB Cleavage overrides are currently applied only to unconfined fracture sets, and not to layer-bound fracture sets
                        List<Cleavage> local_Cleavages = Cleavages_array[ColNo, RowNo, LayerNo];
                        gc.SetCleavages(local_Cleavages, MaxConsistencyAngle);
#if DEBUG_FRACS
                        foreach (Cleavage cleavage in local_Cleavages)
                            Console.WriteLine(string.Format("Set cleavage normal to ({0},{1},{2}): Gc {3}, MuFr {4}", cleavage.NormalVector.Component(VectorComponents.X), cleavage.NormalVector.Component(VectorComponents.Y), cleavage.NormalVector.Component(VectorComponents.Z), cleavage.GcOverride, cleavage.MuFrOverride));
#endif

                        // Add the gridblock to the grid
                        ModelGrid.AddGridblock(gc, ColNo, RowNo, LayerNo);

#if DEBUG_FRACS
                        Console.WriteLine(string.Format("ModelGrid.AddGridblock(gc, {0}, {1}, {2});", ColNo, RowNo, LayerNo));
#endif
                    }
                }
            }
            // Set the DFN generation data
            DFNGenerationControl dfn_control = new DFNGenerationControl(GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, MinUnconfinedFractureRadius, -1, MaxNoFracturePatches, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, IntermediateOutputIntervalControl, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchNeighbouringGridblocks, PropagateFracturesInNucleationOrder, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio, LargeFractureMinimumRadius, ModelTimeUnits);
#if DEBUG_FRACS
            Console.WriteLine(string.Format("DFNGenerationControl dfn_control = new DFNGenerationControl({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, DFNFileType.{14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, TimeUnits.{22});", GenerateExplicitDFN, MinExplicitMicrofractureRadius, MinMacrofractureLength, MinUnconfinedFractureRadius, -1, MaxNoFractureSegments, MinimumLayerThickness, MaxConsistencyAngle, CropAtBoundary, LinkStressShadows, Number_uF_Points, NoIntermediateOutputs, IntermediateOutputIntervalControl, WriteDFNFiles, OutputDFNFileType, OutputCentrepoints, ProbabilisticFractureNucleationLimit, SearchNeighbouringGridblocks, PropagateFracturesInNucleationOrder, MinStressShadowDeactivationRatio, MinIntersectionDeactivationRatio, LargeFractureMinimumRadius, ModelTimeUnits));
#endif

#if DEBUG_FRACS
            for (int ColNo = 0; ColNo < NoCols; ColNo++)
                for (int RowNo = 0; RowNo < NoRows; RowNo++)
                    for (int LayerNo = 0; LayerNo < NoLayers; LayerNo++)
                    {
                        Console.WriteLine("");
                        Console.WriteLine(string.Format("FractureGrid gridblock Col(I) {0}, Row(J) {1}, Layer(K) {2}", ColNo, RowNo, LayerNo));
                        GridblockConfiguration gbc = ModelGrid.GetGridblock(ColNo, RowNo, LayerNo);
                        Console.WriteLine(string.Format("SWtop = new PointXYZ({0}, {1}, {2});", gbc.SWtop.X, gbc.SWtop.Y, gbc.SWtop.Z));
                        Console.WriteLine(string.Format("SWbottom = new PointXYZ({0}, {1}, {2});", gbc.SWbottom.X, gbc.SWbottom.Y, gbc.SWbottom.Z));
                        Console.WriteLine(string.Format("NWtop = new PointXYZ({0}, {1}, {2});", gbc.NWtop.X, gbc.NWtop.Y, gbc.NWtop.Z));
                        Console.WriteLine(string.Format("NWbottom = new PointXYZ({0}, {1}, {2});", gbc.NWbottom.X, gbc.NWbottom.Y, gbc.NWbottom.Z));
                        Console.WriteLine(string.Format("NEtop = new PointXYZ({0}, {1}, {2});", gbc.NEtop.X, gbc.NEtop.Y, gbc.NEtop.Z));
                        Console.WriteLine(string.Format("NEbottom = new PointXYZ({0}, {1}, {2});", gbc.NEbottom.X, gbc.NEbottom.Y, gbc.NEbottom.Z));
                        Console.WriteLine(string.Format("SEtop = new PointXYZ({0}, {1}, {2});", gbc.SEtop.X, gbc.SEtop.Y, gbc.SEtop.Z));
                        Console.WriteLine(string.Format("SEbottom = new PointXYZ({0}, {1}, {2});", gbc.SEbottom.X, gbc.SEbottom.Y, gbc.SEbottom.Z));
                    }
#endif

            // If the intermediate stage DFMs are set to be output at specified times, create a list of deformation episode end times for this purpose and supply it to the DFNGenerationControl object
            // NB This list is based on the global deformation episode end times, and ignores local overrides
            if (IntermediateOutputIntervalControl == IntermediateOutputInterval.SpecifiedTime)
            {
                List<double> DeformationEpisodeEndTimes_list = new List<double>();
                double currentEpisodeEndTime = 0;
                for (int deformationEpisodeNo = 0; deformationEpisodeNo < noDeformationEpisodes; deformationEpisodeNo++)
                {
                    double deformationEpisodeDuration = DeformationEpisodeDuration_list[deformationEpisodeNo];
                    if (DeformationEpisodeDuration_list[deformationEpisodeNo] > 0)
                    {
                        currentEpisodeEndTime += deformationEpisodeDuration;
                        DeformationEpisodeEndTimes_list.Add(currentEpisodeEndTime);
                    }
                }
                dfn_control.IntermediateOutputTimes = DeformationEpisodeEndTimes_list;

#if DEBUG_FRACS
                string setIntermediateTimes = "dfn_control.IntermediateOutputTimes = {";
                foreach (double nextEndTime in DeformationEpisodeEndTimes_list)
                    setIntermediateTimes += string.Format(" {0},", nextEndTime);
                setIntermediateTimes.TrimEnd(',');
                setIntermediateTimes += " }";
                Console.WriteLine(setIntermediateTimes);
#endif
            }

            // Set the output folder path
            dfn_control.FolderPath = folderPath;

            // If required, set the flag to extract the traces of the 3D fractures in the DFN on a horizontal plane at the specified depth, and write the fracture network geometry data to file
            // This requires that we specify the depth of the horizontal section to extract the fracture traces onto
            if (!double.IsNaN(DepthOfHorizontalSection))
                dfn_control.Create2DFractureNetwork(DepthOfHorizontalSection);

            // Add the DFNGenerationControl object to the grid
            ModelGrid.DFNControl = dfn_control;

#if DEBUG_FRACS
            Console.WriteLine(string.Format("dfn_control.FolderPath = {0};", folderPath));
            Console.WriteLine(string.Format("ModelGrid.DFNControl = dfn_control;"));
#endif

            Console.WriteLine("Grid created");

            // Run the calculation for all gridblocks
            ModelGrid.CalculateAllFractureData(progReporter);
            Console.WriteLine("Implicit fracture data calculated");
            ModelGrid.GenerateDFN(progReporter);
            if (dfn_control.GenerateExplicitDFN)
                Console.WriteLine("DFN Generated");
            Console.WriteLine("Calculation completed!");

            // If required, write property data to GRDECL files
            if (WriteGRDECLFiles)
            {
            Console.WriteLine("Writing implicit data to GRDECL files");
            EclipseImportExport Exporter = new EclipseImportExport(ModelGrid);
            Exporter.WriteGRDECLFile("TestExportSeparate", progReporter, WriteSeparateGRDECLPropertyFiles, true, true, true, OutputFracturePorosity, OutputFracturePermeabilityTensor, OutputBulkRockElasticTensors, false);
            Console.WriteLine("Data export completed!");
            }

#if !READINPUTFROMFILE
            // If running from hardcoded data, require a user key press before terminating and closing the console window
            Console.ReadKey();
#endif
        }
    }
}
