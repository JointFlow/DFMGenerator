using System;
using System.Drawing;
using System.Windows.Forms;

using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using Slb.Ocean.Petrel.UI.Controls;
using Slb.Ocean.Petrel.DomainObject;

namespace DFNGenerator_Ocean
{
    /// <summary>
    /// This class is the user interface which forms the focus for the capabilities offered by the process.  
    /// This often includes UI to set up arguments and interactively run a batch part expressed as a workstep.
    /// </summary>
    partial class DFNGeneratorUI : UserControl
    {
        private DFNGenerator workstep;
        /// <summary>
        /// The argument package instance being edited by the UI.
        /// </summary>
        private DFNGenerator.Arguments args;
        /// <summary>
        /// Contains the actual underlaying context.
        /// </summary>
        private WorkflowContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DFNGeneratorUI"/> class.
        /// </summary>
        /// <param name="workstep">the workstep instance</param>
        /// <param name="args">the arguments</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        public DFNGeneratorUI(DFNGenerator workstep, DFNGenerator.Arguments args, WorkflowContext context)
        {
            InitializeComponent();

            this.workstep = workstep;
            this.args = args;
            this.context = context;

            this.btnOK.Image = PetrelImages.OK;
            this.btnCancel.Image = PetrelImages.Cancel;
            this.btnApply.Image = PetrelImages.Apply;

            context.ArgumentPackageChanged += new EventHandler<WorkflowContext.ArgumentPackageChangedEventArgs>(context_ArgumentPackageChanged);

        }

        #region UI_update

        void context_ArgumentPackageChanged(object sender, WorkflowContext.ArgumentPackageChangedEventArgs e)
        {
            if (sender != this)
                updateUIFromArgs();
        }

        /// <summary>
        /// Updates the data displayed on the UI.
        /// </summary>
        private void updateUIFromArgs()
        {
            // Main settings
            UpdateTextBox(args.Argument_ModelName, textBox_ModelName);
            UpdateGridPresentationBox(args.Argument_Grid);
            UpdateTextBox(args.Argument_StartColI, textBox_StartColI);
            UpdateTextBox(args.Argument_NoColsI, textBox_NoColsI);
            UpdateTextBox(args.Argument_StartRowJ, textBox_StartRowJ);
            UpdateTextBox(args.Argument_NoRowsJ, textBox_NoRowsJ);
            UpdateTextBox(args.Argument_TopLayerK, textBox_TopLayerK);
            UpdateTextBox(args.Argument_BottomLayerK, textBox_BottomLayerK);
            UpdatePropertyPresentationBox(args.Argument_EhminAzi, presentationBox_EhminAzi);
            // Unit conversion and labelling for the strain rate properties EhminRate and EhmaxRate must be carried out manually, as there are no inbuilt Petrel units for strain rate 
            // Because geological time units are very different from SI time units, this conversion is carried out when downloading data from the dialog box or when reading the grid property values
            // Until that point, all data will be kept in project units
            UpdatePropertyPresentationBox(args.Argument_EhminRate, presentationBox_EhminRate);
            UpdatePropertyPresentationBox(args.Argument_EhmaxRate, presentationBox_EhmaxRate);
            SetStrainRateUnits();
            UpdateTextBox(args.Argument_EhminAzi_default, unitTextBox_EhminAzi_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth, label_EhminAzi_Units);
            UpdateTextBox(args.Argument_EhminRate_default, unitTextBox_EhminRate_default);
            UpdateTextBox(args.Argument_EhmaxRate_default, unitTextBox_EhmaxRate_default);
            UpdateCheckBox(args.Argument_GenerateExplicitDFN, checkBox_GenerateExplicitDFN);
            UpdateNumericBox(args.Argument_NoIntermediateOutputs, numericUpDown_NoIntermediateOutputs);
            UpdateCheckBox(args.Argument_IncludeObliqueFracs, checkBox_IncludeObliqueFracs);
            // Disable the numeric box for selecting the number of fracture sets and the checkbox for checking all microfracture stress shadowson the Control parameters tab,
            // if the Include oblique fractures checkbox on the Main tab is checked, and enable them if the Include oblique fractures checkbox is unchecked
            EnableNoFractureSets();

            // Mechanical properties
            UpdatePropertyPresentationBox(args.Argument_YoungsMod, presentationBox_YoungsMod);
            UpdateTextBox(args.Argument_YoungsMod_default, unitTextBox_YoungsMod_default, PetrelProject.WellKnownTemplates.GeomechanicGroup.YoungsModulus, label_YoungsMod_Units);
            UpdatePropertyPresentationBox(args.Argument_PoissonsRatio, presentationBox_PoissonsRatio);
            UpdateTextBox(args.Argument_PoissonsRatio_default, unitTextBox_PoissonsRatio_default, PetrelProject.WellKnownTemplates.GeophysicalGroup.PoissonRatio);
            UpdatePropertyPresentationBox(args.Argument_BiotCoefficient, presentationBox_BiotCoefficient);
            UpdateTextBox(args.Argument_BiotCoefficient_default, unitTextBox_BiotCoefficient_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdatePropertyPresentationBox(args.Argument_FrictionCoefficient, presentationBox_FrictionCoefficient);
            UpdateTextBox(args.Argument_FrictionCoefficient_default, unitTextBox_FrictionCoefficient_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdatePropertyPresentationBox(args.Argument_CrackSurfaceEnergy, presentationBox_CrackSurfaceEnergy);
            UpdateTextBox(args.Argument_CrackSurfaceEnergy_default, unitTextBox_CrackSurfaceEnergy_default, PetrelProject.WellKnownTemplates.PetrophysicalGroup.SurfaceTension, label_CrackSurfaceEnergy_Units);
            UpdatePropertyPresentationBox(args.Argument_RockStrainRelaxation, presentationBox_RockStrainRelaxation);
            UpdateTextBox(args.Argument_RockStrainRelaxation_default, unitTextBox_RockStrainRelaxation_default, PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale, label_RockStrainRelaxation_Units);
            UpdatePropertyPresentationBox(args.Argument_FractureStrainRelaxation, presentationBox_FractureStrainRelaxation);
            UpdateTextBox(args.Argument_FractureStrainRelaxation_default, unitTextBox_FractureStrainRelaxation_default, PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale, label_FractureStrainRelaxation_Units);
            // Unit conversion and labelling must be carried out manually for InitialMicrofractureDensity A, since its units will vary depending on the value of InitialMicrofractureSizeDistribution c: [A]=[L]^c-3 
            // Unit conversion will be done on a cell by cell basis, when launching the calculation code, since the values of InitialMicrofractureSizeDistribution may vary between cells
            // Until that point, all data will be kept in project units
            UpdatePropertyPresentationBox(args.Argument_InitialMicrofractureDensity, presentationBox_InitialMicrofractureDensity);
            UpdateTextBox(args.Argument_InitialMicrofractureDensity_default, unitTextBox_InitialMicrofractureDensity_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdatePropertyPresentationBox(args.Argument_InitialMicrofractureSizeDistribution, presentationBox_InitialMicrofractureSizeDistribution);
            UpdateTextBox(args.Argument_InitialMicrofractureSizeDistribution_default, unitTextBox_InitialMicrofractureSizeDistribution_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            // Recalculate and display the InitialMicrofractureDensity units
            SetInitialMicrofractureDensityUnits();
            UpdatePropertyPresentationBox(args.Argument_SubcriticalPropagationIndex, presentationBox_SubcriticalPropagationIndex);
            UpdateTextBox(args.Argument_SubcriticalPropagationIndex_default, unitTextBox_SubcriticalPropagationIndex_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_CriticalPropagationRate, unitTextBox_CriticalPropagationRate, PetrelProject.WellKnownTemplates.GeophysicalGroup.Velocity, label_CriticalPropagationRate_Units);
            UpdateCheckBox(args.Argument_AverageMechanicalPropertyData, checkBox_AverageMechanicalPropertyData);

            // Stress state
            UpdateComboBox(args.Argument_StressDistribution, comboBox_StressDistribution);
            UpdateTextBox(args.Argument_DepthAtDeformation, unitTextBox_DepthAtDeformation, PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth, label_DepthAtDeformation_Units);
            UpdateTextBox(args.Argument_MeanOverlyingSedimentDensity, unitTextBox_MeanOverlyingSedimentDensity, PetrelProject.WellKnownTemplates.GeophysicalGroup.RockDensity, label_MeanOverlyingSedimentDensity_Units);
            UpdateTextBox(args.Argument_FluidDensity, unitTextBox_FluidDensity, PetrelProject.WellKnownTemplates.GeophysicalGroup.LiquidDensity, label_FluidDensity_Units);
            UpdateTextBox(args.Argument_InitialOverpressure, unitTextBox_InitialOverpressure, PetrelProject.WellKnownTemplates.PetrophysicalGroup.Pressure, label_InitialOverpressure_Units);
            UpdateTextBox(args.Argument_InitialStressRelaxation, unitTextBox_InitialStressRelaxation, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateCheckBox(args.Argument_AverageStressStrainData, checkBox_AverageStressStrainData);

            // Outputs
            UpdateCheckBox(args.Argument_WriteImplicitDataFiles, checkBox_LogCalculation);
            UpdateCheckBox(args.Argument_WriteDFNFiles, checkBox_WriteDFNFiles);
            UpdateCheckBox(args.Argument_WriteToProjectFolder, checkBox_WriteToProjectFolder);
            UpdateComboBox(args.Argument_DFNFileType, comboBox_DFNFileType);
            UpdateCheckBox(args.Argument_OutputAtEqualTimeIntervals, checkBox_OutputIntermediatesByTime);
            UpdateCheckBox(args.Argument_OutputCentrepoints, checkBox_OutputCentrepoints);
            // Fracture connectivity and anisotropy index control parameters
            UpdateCheckBox(args.Argument_CalculateFractureConnectivityAnisotropy, checkBox_CalculateFractureConnectivityAnisotropy);
            UpdateCheckBox(args.Argument_CalculateFracturePorosity, checkBox_CalculateFracturePorosity);
            UpdateCheckBox(args.Argument_CalculateBulkRockElasticTensors, checkBox_CalculateBulkRockElasticTensors);

            // Fracture aperture control parameters
            UpdateComboBox(args.Argument_FractureApertureControl, comboBox_FractureApertureControl);
            // Enable the GroupBox for the selected fracture aperture control method, and disable the others 
            EnableFractureApertureControlData();
            UpdateTextBox(args.Argument_HMin_UniformAperture, unitTextBox_HMin_UniformAperture, PetrelProject.WellKnownTemplates.FracturePropertyGroup.FractureAperture, label_HMin_UniformAperture_Units);
            UpdateTextBox(args.Argument_HMax_UniformAperture, unitTextBox_HMax_UniformAperture, PetrelProject.WellKnownTemplates.FracturePropertyGroup.FractureAperture, label_HMax_UniformAperture_Units);
            UpdateTextBox(args.Argument_HMin_SizeDependentApertureMultiplier, unitTextBox_HMin_SizeDependentApertureMultiplier, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_HMax_SizeDependentApertureMultiplier, unitTextBox_HMax_SizeDependentApertureMultiplier, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_DynamicApertureMultiplier, unitTextBox_DynamicApertureMultiplier, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_JRC, unitTextBox_JRC, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_UCSratio, unitTextBox_UCSratio, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_InitialNormalStress, unitTextBox_InitialNormalStress, PetrelProject.WellKnownTemplates.GeomechanicGroup.StressEffective, label_InitialNormalStress_Units);
            UpdateTextBox(args.Argument_FractureNormalStiffness, unitTextBox_FractureNormalStiffness, PetrelProject.WellKnownTemplates.LogTypesGroup.PressureGradient, label_FractureNormalStiffness_Units);
            UpdateTextBox(args.Argument_MaximumClosure, unitTextBox_MaximumClosure, PetrelProject.WellKnownTemplates.FracturePropertyGroup.FractureAperture, label_MaximumClosure_Units);

            // Calculation control parameters
            UpdateNumericBox(args.Argument_NoFractureSets, numericUpDown_NoFractureSets);
            UpdateComboBox(args.Argument_FractureMode, comboBox_FractureMode);
            UpdateCheckBox(args.Argument_CheckAlluFStressShadows, checkBox_CheckAlluFStressShadows);
            UpdateTextBox(args.Argument_AnisotropyCutoff, unitTextBox_AnisotropyCutoff, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateCheckBox(args.Argument_AllowReverseFractures, checkBox_AllowReverseFractures);
            UpdateNumericBox(args.Argument_HorizontalUpscalingFactor, numericUpDown_HorizontalUpscalingFactor);
            UpdateTextBox(args.Argument_MaxTSDuration, unitTextBox_MaxTSDuration, PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale, label_MaxTSDuration_Units);
            UpdateTextBox(args.Argument_Max_TS_MFP33_increase, unitTextBox_Max_TS_MFP33_increase, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            // NB Length units are taken from the PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth template rather than the PetrelProject.WellKnownTemplates.GeometricalGroup.Distance template,
            // because with a UTM coordinate reference system, the Distance template may be set to metric when the project length units are in ft
            UpdateTextBox(args.Argument_MinimumImplicitMicrofractureRadius, unitTextBox_MinimumImplicitMicrofractureRadius, PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth, label_MinimumImplicitMicrofractureRadius_Units);
            UpdateTextBox(args.Argument_No_r_bins, textBox_No_r_bins);
            // Calculation termination controls
            UpdateTextBox(args.Argument_DeformationDuration, unitTextBox_DeformationDuration, PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale, label_DeformationDuration_Units);
            UpdateTextBox(args.Argument_MaxNoTimesteps, textBox_MaxNoTimesteps);
            UpdateTextBox(args.Argument_Historic_MFP33_TerminationRatio, unitTextBox_Historic_MFP33_TerminationRatio, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_Active_MFP30_TerminationRatio, unitTextBox_Active_MFP30_TerminationRatio, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_Minimum_ClearZone_Volume, unitTextBox_Minimum_ClearZone_Volume, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            // DFN geometry controls
            UpdateCheckBox(args.Argument_CropAtGridBoundary, checkBox_CropAtGridBoundary);
            UpdateCheckBox(args.Argument_LinkParallelFractures, checkBox_LinkParallelFractures);
            UpdateTextBox(args.Argument_MaxConsistencyAngle, unitTextBox_MaxConsistencyAngle, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth, label_MaxConsistencyAngle_Units);
            UpdateTextBox(args.Argument_MinimumLayerThickness, unitTextBox_MinimumLayerThickness, PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth, label_MinimumLayerThickness_Units);
            UpdateCheckBox(args.Argument_CreateTriangularFractureSegments, checkBox_CreateTriangularFractureSegments);
            UpdateTextBox(args.Argument_ProbabilisticFractureNucleationLimit, unitTextBox_ProbabilisticFractureNucleationLimit, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateCheckBox(args.Argument_PropagateFracturesInNucleationOrder, checkBox_PropagateFracturesInNucleationOrder);
            UpdateComboBox(args.Argument_SearchAdjacentGridblocks, comboBox_SearchAdjacentGridblocks);
            UpdateTextBox(args.Argument_MinimumExplicitMicrofractureRadius, unitTextBox_MinimumExplicitMicrofractureRadius, PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth, label_MinimumExplicitMicrofractureRadius_Units);
            UpdateNumericBox(args.Argument_NoMicrofractureCornerpoints, numericUpDown_NoMicrofractureCornerpoints);
        }

        private void updateArgsFromUI()
        {
            // Main settings
            args.Argument_ModelName = GetStringFromTextBox(textBox_ModelName);
            args.Argument_Grid = presentationBox_Grid.Tag as Grid;
            args.Argument_StartColI = GetIntFromTextBox(textBox_StartColI);
            args.Argument_NoColsI = GetIntFromTextBox(textBox_NoColsI);
            args.Argument_StartRowJ = GetIntFromTextBox(textBox_StartRowJ);
            args.Argument_NoRowsJ = GetIntFromTextBox(textBox_NoRowsJ);
            args.Argument_TopLayerK = GetIntFromTextBox(textBox_TopLayerK);
            args.Argument_BottomLayerK = GetIntFromTextBox(textBox_BottomLayerK);
            args.Argument_EhminAzi = presentationBox_EhminAzi.Tag as Property;
            args.Argument_EhminRate = presentationBox_EhminRate.Tag as Property;
            args.Argument_EhmaxRate = presentationBox_EhmaxRate.Tag as Property;
            args.Argument_EhminAzi_default = GetDoubleFromTextBox(unitTextBox_EhminAzi_default);
            args.Argument_EhminRate_default = GetDoubleFromTextBox(unitTextBox_EhminRate_default);
            args.Argument_EhmaxRate_default = GetDoubleFromTextBox(unitTextBox_EhmaxRate_default);
            args.Argument_GenerateExplicitDFN = checkBox_GenerateExplicitDFN.Checked;
            args.Argument_NoIntermediateOutputs = GetIntFromNumericBox(numericUpDown_NoIntermediateOutputs);
            args.Argument_IncludeObliqueFracs = checkBox_IncludeObliqueFracs.Checked;

            // Mechanical properties
            args.Argument_YoungsMod = presentationBox_YoungsMod.Tag as Property;
            args.Argument_YoungsMod_default = GetDoubleFromTextBox(unitTextBox_YoungsMod_default);
            args.Argument_PoissonsRatio = presentationBox_PoissonsRatio.Tag as Property;
            args.Argument_PoissonsRatio_default = GetDoubleFromTextBox(unitTextBox_PoissonsRatio_default);
            args.Argument_BiotCoefficient = presentationBox_BiotCoefficient.Tag as Property;
            args.Argument_BiotCoefficient_default = GetDoubleFromTextBox(unitTextBox_BiotCoefficient_default);
            args.Argument_FrictionCoefficient = presentationBox_FrictionCoefficient.Tag as Property;
            args.Argument_FrictionCoefficient_default = GetDoubleFromTextBox(unitTextBox_FrictionCoefficient_default);
            args.Argument_CrackSurfaceEnergy = presentationBox_CrackSurfaceEnergy.Tag as Property;
            args.Argument_CrackSurfaceEnergy_default = GetDoubleFromTextBox(unitTextBox_CrackSurfaceEnergy_default);
            args.Argument_RockStrainRelaxation = presentationBox_RockStrainRelaxation.Tag as Property;
            args.Argument_RockStrainRelaxation_default = GetDoubleFromTextBox(unitTextBox_RockStrainRelaxation_default);
            args.Argument_FractureStrainRelaxation = presentationBox_FractureStrainRelaxation.Tag as Property;
            args.Argument_FractureStrainRelaxation_default = GetDoubleFromTextBox(unitTextBox_FractureStrainRelaxation_default);
            args.Argument_InitialMicrofractureDensity = presentationBox_InitialMicrofractureDensity.Tag as Property;
            args.Argument_InitialMicrofractureDensity_default = GetDoubleFromTextBox(unitTextBox_InitialMicrofractureDensity_default);
            args.Argument_InitialMicrofractureSizeDistribution = presentationBox_InitialMicrofractureSizeDistribution.Tag as Property;
            args.Argument_InitialMicrofractureSizeDistribution_default = GetDoubleFromTextBox(unitTextBox_InitialMicrofractureSizeDistribution_default);
            args.Argument_SubcriticalPropagationIndex = presentationBox_SubcriticalPropagationIndex.Tag as Property;
            args.Argument_SubcriticalPropagationIndex_default = GetDoubleFromTextBox(unitTextBox_SubcriticalPropagationIndex_default);
            args.Argument_CriticalPropagationRate = GetDoubleFromTextBox(unitTextBox_CriticalPropagationRate);
            args.Argument_AverageMechanicalPropertyData = checkBox_AverageMechanicalPropertyData.Checked;

            // Stress state
            args.Argument_StressDistribution = comboBox_StressDistribution.SelectedIndex;
            args.Argument_DepthAtDeformation = GetDoubleFromTextBox(unitTextBox_DepthAtDeformation);
            args.Argument_MeanOverlyingSedimentDensity = GetDoubleFromTextBox(unitTextBox_MeanOverlyingSedimentDensity);
            args.Argument_FluidDensity = GetDoubleFromTextBox(unitTextBox_FluidDensity);
            args.Argument_InitialOverpressure = GetDoubleFromTextBox(unitTextBox_InitialOverpressure);
            args.Argument_InitialStressRelaxation = GetDoubleFromTextBox(unitTextBox_InitialStressRelaxation);
            args.Argument_AverageStressStrainData = checkBox_AverageStressStrainData.Checked;

            // Outputs
            args.Argument_WriteImplicitDataFiles = checkBox_LogCalculation.Checked;
            args.Argument_WriteDFNFiles = checkBox_WriteDFNFiles.Checked;
            args.Argument_WriteToProjectFolder = checkBox_WriteToProjectFolder.Checked;
            args.Argument_DFNFileType = comboBox_DFNFileType.SelectedIndex;
            args.Argument_OutputAtEqualTimeIntervals = checkBox_OutputIntermediatesByTime.Checked;
            args.Argument_OutputCentrepoints = checkBox_OutputCentrepoints.Checked;
            // Fracture connectivity and anisotropy index control parameters
            args.Argument_CalculateFractureConnectivityAnisotropy = checkBox_CalculateFractureConnectivityAnisotropy.Checked;
            args.Argument_CalculateFracturePorosity = checkBox_CalculateFracturePorosity.Checked;
            args.Argument_CalculateBulkRockElasticTensors = checkBox_CalculateBulkRockElasticTensors.Checked;

            // Fracture aperture control parameters
            args.Argument_FractureApertureControl = comboBox_FractureApertureControl.SelectedIndex;
            args.Argument_HMin_UniformAperture = GetDoubleFromTextBox(unitTextBox_HMin_UniformAperture);
            args.Argument_HMax_UniformAperture = GetDoubleFromTextBox(unitTextBox_HMax_UniformAperture);
            args.Argument_HMin_SizeDependentApertureMultiplier = GetDoubleFromTextBox(unitTextBox_HMin_SizeDependentApertureMultiplier);
            args.Argument_HMax_SizeDependentApertureMultiplier = GetDoubleFromTextBox(unitTextBox_HMax_SizeDependentApertureMultiplier);
            args.Argument_DynamicApertureMultiplier = GetDoubleFromTextBox(unitTextBox_DynamicApertureMultiplier);
            args.Argument_JRC = GetDoubleFromTextBox(unitTextBox_JRC);
            args.Argument_UCSratio = GetDoubleFromTextBox(unitTextBox_UCSratio);
            args.Argument_InitialNormalStress = GetDoubleFromTextBox(unitTextBox_InitialNormalStress);
            args.Argument_FractureNormalStiffness = GetDoubleFromTextBox(unitTextBox_FractureNormalStiffness);
            args.Argument_MaximumClosure = GetDoubleFromTextBox(unitTextBox_MaximumClosure);

            // Calculation control parameters
            args.Argument_NoFractureSets = GetIntFromNumericBox(numericUpDown_NoFractureSets);
            args.Argument_FractureMode = comboBox_FractureMode.SelectedIndex;
            args.Argument_CheckAlluFStressShadows = checkBox_CheckAlluFStressShadows.Checked;
            args.Argument_AnisotropyCutoff = GetDoubleFromTextBox(unitTextBox_AnisotropyCutoff);
            args.Argument_AllowReverseFractures = checkBox_AllowReverseFractures.Checked;
            args.Argument_HorizontalUpscalingFactor = GetIntFromNumericBox(numericUpDown_HorizontalUpscalingFactor);
            args.Argument_MaxTSDuration = GetDoubleFromTextBox(unitTextBox_MaxTSDuration);
            args.Argument_Max_TS_MFP33_increase = GetDoubleFromTextBox(unitTextBox_Max_TS_MFP33_increase);
            args.Argument_MinimumImplicitMicrofractureRadius = GetDoubleFromTextBox(unitTextBox_MinimumImplicitMicrofractureRadius);
            args.Argument_No_r_bins = GetIntFromTextBox(textBox_No_r_bins);
            // Calculation termination controls
            args.Argument_DeformationDuration = GetDoubleFromTextBox(unitTextBox_DeformationDuration);
            args.Argument_MaxNoTimesteps = GetIntFromTextBox(textBox_MaxNoTimesteps);
            args.Argument_Historic_MFP33_TerminationRatio = GetDoubleFromTextBox(unitTextBox_Historic_MFP33_TerminationRatio);
            args.Argument_Active_MFP30_TerminationRatio = GetDoubleFromTextBox(unitTextBox_Active_MFP30_TerminationRatio);
            args.Argument_Minimum_ClearZone_Volume = GetDoubleFromTextBox(unitTextBox_Minimum_ClearZone_Volume);
            // DFN geometry controls
            args.Argument_CropAtGridBoundary = checkBox_CropAtGridBoundary.Checked;
            args.Argument_LinkParallelFractures = checkBox_LinkParallelFractures.Checked;
            args.Argument_MaxConsistencyAngle = GetDoubleFromTextBox(unitTextBox_MaxConsistencyAngle);
            args.Argument_MinimumLayerThickness = GetDoubleFromTextBox(unitTextBox_MinimumLayerThickness);
            args.Argument_CreateTriangularFractureSegments = checkBox_CreateTriangularFractureSegments.Checked;
            args.Argument_ProbabilisticFractureNucleationLimit = GetDoubleFromTextBox(unitTextBox_ProbabilisticFractureNucleationLimit);
            args.Argument_PropagateFracturesInNucleationOrder = checkBox_PropagateFracturesInNucleationOrder.Checked;
            args.Argument_SearchAdjacentGridblocks = comboBox_SearchAdjacentGridblocks.SelectedIndex;
            args.Argument_MinimumExplicitMicrofractureRadius = GetDoubleFromTextBox(unitTextBox_MinimumExplicitMicrofractureRadius);
            args.Argument_NoMicrofractureCornerpoints = GetIntFromNumericBox(numericUpDown_NoMicrofractureCornerpoints);

            // tell fwk to update LineUI:
            context.OnArgumentPackageChanged(this, new WorkflowContext.ArgumentPackageChangedEventArgs());
        }

        private void UpdateGridPresentationBox(Grid gr)
        {
            if (gr != Grid.NullObject)
            {
                INameInfoFactory grNIF = CoreSystem.GetService<INameInfoFactory>(gr);
                if (grNIF != null)
                {
                    NameInfo grName = grNIF.GetNameInfo(gr);
                    presentationBox_Grid.Text = grName.Name;
                }
                else
                {
                    presentationBox_Grid.Text = gr.Name;
                }
                IImageInfoFactory grImgIF = CoreSystem.GetService<IImageInfoFactory>(gr);
                if (grImgIF != null)
                {
                    ImageInfo grImage = grImgIF.GetImageInfo(gr);
                    presentationBox_Grid.Image = grImage.TypeImage;
                }
            }
            else
            {
                presentationBox_Grid.Text = "";
                presentationBox_Grid.Image = null;
            }
            presentationBox_Grid.Tag = gr;
        }

        private void UpdatePropertyPresentationBox(Property gprop, PresentationBox pBox)
        {
            if (gprop != Property.NullObject)
            {
                INameInfoFactory propNIF = CoreSystem.GetService<INameInfoFactory>(gprop);
                if (propNIF != null)
                {
                    NameInfo propName = propNIF.GetNameInfo(gprop);
                    pBox.Text = propName.Name;
                }
                else
                {
                    pBox.Text = gprop.Name;
                }
                IImageInfoFactory propImgIF = CoreSystem.GetService<IImageInfoFactory>(gprop);
                if (propImgIF != null)
                {
                    ImageInfo propImage = propImgIF.GetImageInfo(gprop);
                    pBox.Image = propImage.GetDisplayImage(new ImageInfoContext());
                }
                else
                {
                    pBox.Image = PetrelImages.Property;
                }
            }
            else
            {
                pBox.Text = "";
                pBox.Image = null;
            }
            pBox.Tag = gprop;
        }
        private void UpdateTextBox(string text, System.Windows.Forms.TextBox tBox)
        {
            tBox.Text = text;
        }
        private void UpdateTextBox(double number, System.Windows.Forms.TextBox tBox)
        {
            if (double.IsNaN(number))
                tBox.Text = "";
            else
                tBox.Text = Convert.ToString(number);
        }
        private void UpdateTextBox(double number, Slb.Ocean.Petrel.UI.Controls.UnitTextBox tBox, Template propertyTemplate)
        {
            // Set the correct units from the supplied template, and set the numeric format to general
            tBox.Template = propertyTemplate;
            tBox.TextFormat = "G";

            if (double.IsNaN(number))
                tBox.Text = "";
            else
                tBox.Value = number;
        }
        private void UpdateTextBox(double number, Slb.Ocean.Petrel.UI.Controls.UnitTextBox tBox, Template propertyTemplate, System.Windows.Forms.Label unitLabel)
        {
            UpdateTextBox(number, tBox, propertyTemplate);

            // Set the unit label text
            unitLabel.Text = PetrelUnitSystem.GetDisplayUnit(propertyTemplate).Symbol;
        }
        // These overloads, which set the UnitTextBox units from an IUnitMeasurement object rather than a Template object, are not required because standard Petrel Template objects are available for all required properties
        /*private void UpdateTextBox(double number, Slb.Ocean.Petrel.UI.Controls.UnitTextBox tBox, Slb.Ocean.Units.IUnitMeasurement propertyUnitMeasurement)
        {
            // Set the correct units from the supplied template, and set the numeric format to general
            tBox.UnitMeasurement = propertyUnitMeasurement;
            tBox.TextFormat = "G";

            if (double.IsNaN(number))
                tBox.Text = "";
            else
                tBox.Value = number;
        }
        private void UpdateTextBox(double number, Slb.Ocean.Petrel.UI.Controls.UnitTextBox tBox, Slb.Ocean.Units.IUnitMeasurement propertyUnitMeasurement, System.Windows.Forms.Label unitLabel)
        {
            UpdateTextBox(number, tBox, propertyUnitMeasurement);

            // Set the unit label text
            unitLabel.Text = PetrelUnitSystem.GetDisplayUnit(propertyUnitMeasurement).Symbol;
        }*/
        private void UpdateTextBox(int number, System.Windows.Forms.TextBox tBox)
        {
            if (number == -999)
                tBox.Text = "";
            else
                tBox.Text = Convert.ToString(number);
        }
        private void UpdateNumericBox(int number, System.Windows.Forms.NumericUpDown nBox)
        {
            nBox.Value = number;
        }
        private void UpdateCheckBox(bool state, System.Windows.Forms.CheckBox cBox)
        {
            cBox.Checked = state;
        }
        private void UpdateListBox(int index, System.Windows.Forms.ListBox lBox)
        {
            if (index < 0) index = 0;
            if (index >= lBox.Items.Count) index = lBox.Items.Count - 1;
            lBox.SelectedIndex = index;
        }
        private void UpdateComboBox(int index, Slb.Ocean.Petrel.UI.Controls.ComboBox cBox)
        {
            if (index < 0) index = 0;
            if (index >= cBox.Items.Count) index = cBox.Items.Count - 1;
            cBox.SelectedIndex = index;
        }
        private string GetStringFromTextBox(System.Windows.Forms.TextBox tBox)
        {
            return tBox.Text;
        }
        private double GetDoubleFromTextBox(Slb.Ocean.Petrel.UI.Controls.UnitTextBox tBox)
        {
            try
            {
                return tBox.Value;
            }
            catch (FormatException)
            {
                return double.NaN;
            }
            catch (OverflowException)
            {
                return double.NaN;
            }
        }
        private double GetDoubleFromTextBox(System.Windows.Forms.TextBox tBox)
        {
            try
            {
                return Convert.ToDouble(tBox.Text);
            }
            catch (FormatException)
            {
                return double.NaN;
            }
            catch (OverflowException)
            {
                return double.NaN;
            }
        }
        private int GetIntFromTextBox(System.Windows.Forms.TextBox tBox)
        {
            try
            {
                return Convert.ToInt32(tBox.Text);
            }
            catch (FormatException)
            {
                return -999;
            }
            catch (OverflowException)
            {
                return -999;
            }
        }
        private int GetIntFromNumericBox(System.Windows.Forms.NumericUpDown nBox)
        {
            return Convert.ToInt32(nBox.Value);
        }

        private void EnableFractureApertureControlData()
        {
            // Enable the GroupBox for the selected fracture aperture control method, and disable the others 
            switch (comboBox_FractureApertureControl.SelectedIndex)
            {
                case 0: // Uniform aperture

                    groupBox_UniformApertureData.Enabled = true;
                    groupBox_SizeDependentApertureData.Enabled = false;
                    groupBox_DynamicApertureData.Enabled = false;
                    groupBox_BartonBandisApertureData.Enabled = false;
                    return;

                case 1: // Size dependent aperture

                    groupBox_UniformApertureData.Enabled = false;
                    groupBox_SizeDependentApertureData.Enabled = true;
                    groupBox_DynamicApertureData.Enabled = false;
                    groupBox_BartonBandisApertureData.Enabled = false;
                    return;

                case 2: // Dynamic aperture

                    groupBox_UniformApertureData.Enabled = false;
                    groupBox_SizeDependentApertureData.Enabled = false;
                    groupBox_DynamicApertureData.Enabled = true;
                    groupBox_BartonBandisApertureData.Enabled = false;
                    return;

                case 3: // Barton-Bandis aperture

                    groupBox_UniformApertureData.Enabled = false;
                    groupBox_SizeDependentApertureData.Enabled = false;
                    groupBox_DynamicApertureData.Enabled = false;
                    groupBox_BartonBandisApertureData.Enabled = true;
                    return;

                default: // If no option is selected, disable all fracture aperture control GroupBoxeas

                    groupBox_UniformApertureData.Enabled = false;
                    groupBox_SizeDependentApertureData.Enabled = false;
                    groupBox_DynamicApertureData.Enabled = false;
                    groupBox_BartonBandisApertureData.Enabled = false;
                    return;
            }
        }

        private void EnableNoFractureSets()
        {
            // Disable the numeric box for selecting the number of fracture sets and the checkbox for checking all microfracture stress shadowson the Control parameters tab,
            // if the Include oblique fractures checkbox on the Main tab is unchecked, and enable them if the Include oblique fractures checkbox is checked
            if (checkBox_IncludeObliqueFracs.Checked == false)
            { 
                label_NoFractureSets.Enabled = false;
                numericUpDown_NoFractureSets.Enabled = false;
                checkBox_CheckAlluFStressShadows.Enabled = false;
            }
            else
            {
                label_NoFractureSets.Enabled = true;
                numericUpDown_NoFractureSets.Enabled = true;
                checkBox_CheckAlluFStressShadows.Enabled = true;
            }
        }
        private void SetInitialMicrofractureDensityUnits()
        {
            double c = GetDoubleFromTextBox(unitTextBox_InitialMicrofractureSizeDistribution_default);
            // NB Length units are taken from the PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth template rather than the PetrelProject.WellKnownTemplates.GeometricalGroup.Distance template,
            // because with a UTM coordinate reference system, the Distance template may be set to metric when the project length units are in ft
            string lengthUnit = PetrelUnitSystem.GetDisplayUnit(PetrelProject.WellKnownTemplates.SpatialGroup.ThicknessDepth).Symbol;
            string AUnit = "";
            if (c < 3)
                AUnit = string.Format("fracs/{0}^{1}", lengthUnit, 3 - c);
            else if (c > 3)
                AUnit = string.Format("frac.{0}^{1}", lengthUnit, c - 3);
            else
                AUnit = "fracs";
            label_InitialMicrofractureDensity_Units.Text = AUnit;
        }
        private void SetStrainRateUnits()
        {
            string StrainRateUnits = string.Format("/{0}", PetrelUnitSystem.GetDisplayUnit(PetrelProject.WellKnownTemplates.PetroleumGroup.GeologicalTimescale).Symbol);
            label_EhminRate_Units.Text = StrainRateUnits;
            label_EhmaxRate_Units.Text = StrainRateUnits;
        }
        #endregion

        #region EventHandlers

        private void btnApply_Click(object sender, EventArgs e)
        {
            updateArgsFromUI();
            if (context is WorkstepProcessWrapper.Context)
            {
                Executor executor = (workstep as IExecutorSource).GetExecutor(args, null);
                executor.ExecuteSimple();
                this.updateUIFromArgs();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            btnApply_Click(sender, e);
            this.FindForm().Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.FindForm().Close();
        }

        private void dropTarget_Grid_DragDrop(object sender, DragEventArgs e)
        {
            Grid droppedGrid = e.Data.GetData(typeof(object)) as Grid;
            UpdateGridPresentationBox(droppedGrid);
        }

        private void dropTarget_EhminAzi_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_EhminAzi);
        }

        private void dropTarget_EhminRate_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_EhminRate);
        }

        private void dropTarget_EhmaxAzi_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_EhmaxRate);
        }

        private void dropTarget_YoungsMod_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_YoungsMod);
        }

        private void dropTarget_PoissonsRatio_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PoissonsRatio);
        }

        private void dropTarget_BiotCoefficient_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_BiotCoefficient);
        }

        private void dropTarget_FrictionCoefficient_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_FrictionCoefficient);
        }

        private void dropTarget_CrackSurfaceEnergy_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_CrackSurfaceEnergy);
        }

        private void dropTarget_RockStrainRelaxation_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_RockStrainRelaxation);
        }

        private void dropTarget_FractureStrainRelaxation_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_FractureStrainRelaxation);
        }

        private void dropTarget_InitialMicrofractureDensity_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_InitialMicrofractureDensity);
        }

        private void dropTarget_InitialMicrofractureSizeDistribution_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_InitialMicrofractureSizeDistribution);
        }

        private void dropTarget_SubcriticalPropagationIndex_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_SubcriticalPropagationIndex);
        }

        private void presentationBox_Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridPresentationBox(Grid.NullObject);
                e.Handled = true;
            }
        }

        private void presentationBox_EhminAzi_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_EhminAzi);
                e.Handled = true;
            }
        }

        private void presentationBox_EhminRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_EhminRate);
                e.Handled = true;
            }
        }

        private void presentationBox_EhmaxRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_EhmaxRate);
                e.Handled = true;
            }
        }

        private void presentationBox_YoungsMod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_YoungsMod);
                e.Handled = true;
            }
        }

        private void presentationBox_PoissonsRatio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PoissonsRatio);
                e.Handled = true;
            }
        }

        private void presentationBox_BiotCoefficient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_BiotCoefficient);
                e.Handled = true;
            }
        }

        private void presentationBox_FrictionCoefficient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_FrictionCoefficient);
                e.Handled = true;
            }
        }

        private void presentationBox_CrackSurfaceEnergy_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_CrackSurfaceEnergy);
                e.Handled = true;
            }
        }

        private void presentationBox_RockStrainRelaxation_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_RockStrainRelaxation);
                e.Handled = true;
            }
        }

        private void presentationBox_FractureStrainRelaxation_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_FractureStrainRelaxation);
                e.Handled = true;
            }
        }

        private void presentationBox_InitialMicrofractureDensity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_InitialMicrofractureDensity);
                e.Handled = true;
            }
        }

        private void presentationBox_InitialMicrofractureSizeDistribution_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_InitialMicrofractureSizeDistribution);
                e.Handled = true;
            }
        }

        private void presentationBox_SubcriticalPropagationIndex_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_SubcriticalPropagationIndex);
                e.Handled = true;
            }
        }

        private void numericUpDown_NoFractureSets_ValueChanged(object sender, EventArgs e)
        {
            // When the number of fracture sets is changed, adjust the flag to check microfractures against stress shadows of all macrofractures regardless of set accordingly
            if (numericUpDown_NoFractureSets.Value > 2)
                checkBox_CheckAlluFStressShadows.Checked = true;
            else
                checkBox_CheckAlluFStressShadows.Checked = false;
        }

        private void comboBox_FractureApertureControl_SelectedValueChanged(object sender, EventArgs e)
        {
            // Enable the GroupBox for the selected fracture aperture control method, and disable the others 
            EnableFractureApertureControlData();
        }

        private void checkBox_IncludeObliqueFracs_CheckedChanged(object sender, EventArgs e)
        {
            // Disable the numeric box for selecting the number of fracture sets and the checkbox for checking all microfracture stress shadows on the Control parameters tab,
            // if the Include oblique fractures checkbox on the Main tab is checked, and enable them if the Include oblique fractures checkbox is unchecked
            EnableNoFractureSets();
        }

        private void unitTextBox_InitialMicrofractureSizeDistribution_default_ValueChanged(object sender, EventArgs e)
        {
            // Recalculate and display the InitialMicrofractureDensity units
            SetInitialMicrofractureDensityUnits();
        }

        private void btnRestoreDefaults_Click(object sender, EventArgs e)
        {
            // Restore all arguments to their default values, then repopulate the dialog
            args.ResetDefaults();
            updateUIFromArgs();
        }
        #endregion
    }
}
