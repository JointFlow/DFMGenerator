using System;
using System.Drawing;
using System.Windows.Forms;

using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;

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
            UpdatePropertyPresentationBox(args.Argument_EhminRate, presentationBox_EhminRate);
            UpdatePropertyPresentationBox(args.Argument_EhmaxRate, presentationBox_EhmaxRate);
            UpdateTextBox(args.Argument_EhminAzi_default, textBox_EhminAzi_default);
            UpdateTextBox(args.Argument_EhminRate_default, textBox_EhminRate_default);
            UpdateTextBox(args.Argument_EhmaxRate_default, textBox_EhmaxRate_default);
            UpdateCheckBox(args.Argument_GenerateExplicitDFN, checkBox_GenerateExplicitDFN);
            UpdateNumericBox(args.Argument_NoIntermediateOutputs, numericUpDown_NoIntermediateOutputs);

            // Mechanical properties
            UpdatePropertyPresentationBox(args.Argument_YoungsMod, presentationBox_YoungsMod);
            UpdateTextBox(args.Argument_YoungsMod_default, textBox_YoungsMod_default);
            UpdatePropertyPresentationBox(args.Argument_PoissonsRatio, presentationBox_PoissonsRatio);
            UpdateTextBox(args.Argument_PoissonsRatio_default, textBox_PoissonsRatio_default);
            UpdatePropertyPresentationBox(args.Argument_BiotCoefficient, presentationBox_BiotCoefficient);
            UpdateTextBox(args.Argument_BiotCoefficient_default, textBox_BiotCoefficient_default);
            UpdatePropertyPresentationBox(args.Argument_FrictionCoefficient, presentationBox_FrictionCoefficient);
            UpdateTextBox(args.Argument_FrictionCoefficient_default, textBox_FrictionCoefficient_default);
            UpdatePropertyPresentationBox(args.Argument_CrackSurfaceEnergy, presentationBox_CrackSurfaceEnergy);
            UpdateTextBox(args.Argument_CrackSurfaceEnergy_default, textBox_CrackSurfaceEnergy_default);
            UpdatePropertyPresentationBox(args.Argument_RockStrainRelaxation, presentationBox_RockStrainRelaxation);
            UpdateTextBox(args.Argument_RockStrainRelaxation_default, textBox_RockStrainRelaxation_default);
            UpdatePropertyPresentationBox(args.Argument_FractureStrainRelaxation, presentationBox_FractureStrainRelaxation);
            UpdateTextBox(args.Argument_FractureStrainRelaxation_default, textBox_FractureStrainRelaxation_default);
            UpdatePropertyPresentationBox(args.Argument_InitialMicrofractureDensity, presentationBox_InitialMicrofractureDensity);
            UpdateTextBox(args.Argument_InitialMicrofractureDensity_default, textBox_InitialMicrofractureDensity_default);
            UpdatePropertyPresentationBox(args.Argument_InitialMicrofractureSizeDistribution, presentationBox_InitialMicrofractureSizeDistribution);
            UpdateTextBox(args.Argument_InitialMicrofractureSizeDistribution_default, textBox_InitialMicrofractureSizeDistribution_default);
            UpdatePropertyPresentationBox(args.Argument_SubcriticalPropagationIndex, presentationBox_SubcriticalPropagationIndex);
            UpdateTextBox(args.Argument_SubcriticalPropagationIndex_default, textBox_SubcriticalPropagationIndex_default);
            UpdateTextBox(args.Argument_CriticalPropagationRate, textBox_CriticalPropagationRate);
            UpdateCheckBox(args.Argument_AverageMechanicalPropertyData, checkBox_AverageMechanicalPropertyData);

            // Stress state
            UpdateComboBox(args.Argument_StressDistribution, comboBox_StressDistribution);
            UpdateTextBox(args.Argument_DepthAtDeformation,textBox_DepthAtDeformation);
            UpdateTextBox(args.Argument_MeanOverlyingSedimentDensity,textBox_MeanOverlyingSedimentDensity);
            UpdateTextBox(args.Argument_FluidDensity,textBox_FluidDensity);
            UpdateTextBox(args.Argument_InitialOverpressure,textBox_InitialOverpressure);
            UpdateTextBox(args.Argument_InitialStressRelaxation,textBox_InitialStressRelaxation);
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
            // Fracture porosity control parameters
            UpdateCheckBox(args.Argument_CalculateFracturePorosity, checkBox_CalculateFracturePorosity);
            UpdateComboBox(args.Argument_FractureApertureControl, comboBox_FractureApertureControl);
            UpdateTextBox(args.Argument_HMin_UniformAperture, textBox_HMin_UniformAperture);
            UpdateTextBox(args.Argument_HMax_UniformAperture, textBox_HMax_UniformAperture);
            UpdateTextBox(args.Argument_HMin_SizeDependentApertureMultiplier, textBox_HMin_SizeDependentApertureMultiplier);
            UpdateTextBox(args.Argument_HMax_SizeDependentApertureMultiplier, textBox_HMax_SizeDependentApertureMultiplier);
            UpdateTextBox(args.Argument_DynamicApertureMultiplier, textBox_DynamicApertureMultiplier);
            UpdateTextBox(args.Argument_JRC, textBox_JRC);
            UpdateTextBox(args.Argument_UCSratio, textBox_UCSratio);
            UpdateTextBox(args.Argument_InitialNormalStress, textBox_InitialNormalStress);
            UpdateTextBox(args.Argument_FractureNormalStiffness, textBox_FractureNormalStiffness);
            UpdateTextBox(args.Argument_MaximumClosure, textBox_MaximumClosure);

            // Calculation control parameters
            UpdateNumericBox(args.Argument_NoFractureSets, numericUpDown_NoFractureSets);
            UpdateComboBox(args.Argument_FractureMode, comboBox_FractureMode);
            UpdateCheckBox(args.Argument_CheckAlluFStressShadows, checkBox_CheckAlluFStressShadows);
            UpdateTextBox(args.Argument_AnisotropyCutoff, textBox_AnisotropyCutoff);
            UpdateCheckBox(args.Argument_AllowReverseFractures, checkBox_AllowReverseFractures);
            UpdateNumericBox(args.Argument_HorizontalUpscalingFactor, numericUpDown_HorizontalUpscalingFactor);
            UpdateTextBox(args.Argument_MaxTSDuration, textBox_MaxTSDuration);
            UpdateTextBox(args.Argument_Max_TS_MFP33_increase, textBox_Max_TS_MFP33_increase);
            UpdateTextBox(args.Argument_MinimumImplicitMicrofractureRadius, textBox_MinimumImplicitMicrofractureRadius);
            UpdateTextBox(args.Argument_No_r_bins, textBox_No_r_bins);
            // Calculation termination controls
            UpdateTextBox(args.Argument_DeformationDuration, textBox_DeformationDuration);
            UpdateTextBox(args.Argument_MaxNoTimesteps, textBox_MaxNoTimesteps);
            UpdateTextBox(args.Argument_Historic_MFP33_TerminationRatio, textBox_Historic_MFP33_TerminationRatio);
            UpdateTextBox(args.Argument_Active_MFP30_TerminationRatio, textBox_Active_MFP30_TerminationRatio);
            UpdateTextBox(args.Argument_Minimum_ClearZone_Volume, textBox_Minimum_ClearZone_Volume);
            // DFN geometry controls
            UpdateCheckBox(args.Argument_CropAtGridBoundary, checkBox_CropAtGridBoundary);
            UpdateCheckBox(args.Argument_LinkParallelFractures, checkBox_LinkParallelFractures);
            UpdateTextBox(args.Argument_MaxConsistencyAngle, textBox_MaxConsistencyAngle);
            UpdateTextBox(args.Argument_MinimumLayerThickness, textBox_MinimumLayerThickness);
            UpdateCheckBox(args.Argument_CreateTriangularFractureSegments, checkBox_CreateTriangularFractureSegments);
            UpdateTextBox(args.Argument_ProbabilisticFractureNucleationLimit, textBox_ProbabilisticFractureNucleationLimit);
            UpdateCheckBox(args.Argument_PropagateFracturesInNucleationOrder, checkBox_PropagateFracturesInNucleationOrder);
            UpdateComboBox(args.Argument_SearchAdjacentGridblocks, comboBox_SearchAdjacentGridblocks);
            UpdateTextBox(args.Argument_MinimumExplicitMicrofractureRadius, textBox_MinimumExplicitMicrofractureRadius);
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
            args.Argument_EhminAzi_default = GetDoubleFromTextBox(textBox_EhminAzi_default);
            args.Argument_EhminRate_default = GetDoubleFromTextBox(textBox_EhminRate_default);
            args.Argument_EhmaxRate_default = GetDoubleFromTextBox(textBox_EhmaxRate_default);
            args.Argument_GenerateExplicitDFN = checkBox_GenerateExplicitDFN.Checked;
            args.Argument_NoIntermediateOutputs = GetIntFromNumericBox(numericUpDown_NoIntermediateOutputs);

            // Mechanical properties
            args.Argument_YoungsMod = presentationBox_YoungsMod.Tag as Property;
            args.Argument_YoungsMod_default = GetDoubleFromTextBox(textBox_YoungsMod_default);
            args.Argument_PoissonsRatio = presentationBox_PoissonsRatio.Tag as Property;
            args.Argument_PoissonsRatio_default = GetDoubleFromTextBox(textBox_PoissonsRatio_default);
            args.Argument_BiotCoefficient = presentationBox_BiotCoefficient.Tag as Property;
            args.Argument_BiotCoefficient_default = GetDoubleFromTextBox(textBox_BiotCoefficient_default);
            args.Argument_FrictionCoefficient = presentationBox_FrictionCoefficient.Tag as Property;
            args.Argument_FrictionCoefficient_default = GetDoubleFromTextBox(textBox_FrictionCoefficient_default);
            args.Argument_CrackSurfaceEnergy = presentationBox_CrackSurfaceEnergy.Tag as Property;
            args.Argument_CrackSurfaceEnergy_default = GetDoubleFromTextBox(textBox_CrackSurfaceEnergy_default);
            args.Argument_RockStrainRelaxation = presentationBox_RockStrainRelaxation.Tag as Property;
            args.Argument_RockStrainRelaxation_default = GetDoubleFromTextBox(textBox_RockStrainRelaxation_default);
            args.Argument_FractureStrainRelaxation = presentationBox_FractureStrainRelaxation.Tag as Property;
            args.Argument_FractureStrainRelaxation_default = GetDoubleFromTextBox(textBox_FractureStrainRelaxation_default);
            args.Argument_InitialMicrofractureDensity = presentationBox_InitialMicrofractureDensity.Tag as Property;
            args.Argument_InitialMicrofractureDensity_default = GetDoubleFromTextBox(textBox_InitialMicrofractureDensity_default);
            args.Argument_InitialMicrofractureSizeDistribution = presentationBox_InitialMicrofractureSizeDistribution.Tag as Property;
            args.Argument_InitialMicrofractureSizeDistribution_default = GetDoubleFromTextBox(textBox_InitialMicrofractureSizeDistribution_default);
            args.Argument_SubcriticalPropagationIndex = presentationBox_SubcriticalPropagationIndex.Tag as Property;
            args.Argument_SubcriticalPropagationIndex_default = GetDoubleFromTextBox(textBox_SubcriticalPropagationIndex_default);
            args.Argument_CriticalPropagationRate = GetDoubleFromTextBox(textBox_CriticalPropagationRate);
            args.Argument_AverageMechanicalPropertyData = checkBox_AverageMechanicalPropertyData.Checked;

            // Stress state
            args.Argument_StressDistribution = comboBox_StressDistribution.SelectedIndex;
            args.Argument_DepthAtDeformation = GetDoubleFromTextBox(textBox_DepthAtDeformation);
            args.Argument_MeanOverlyingSedimentDensity = GetDoubleFromTextBox(textBox_MeanOverlyingSedimentDensity);
            args.Argument_FluidDensity = GetDoubleFromTextBox(textBox_FluidDensity);
            args.Argument_InitialOverpressure = GetDoubleFromTextBox(textBox_InitialOverpressure);
            args.Argument_InitialStressRelaxation = GetDoubleFromTextBox(textBox_InitialStressRelaxation);
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
            // Fracture porosity control parameters
            args.Argument_CalculateFracturePorosity = checkBox_CalculateFracturePorosity.Checked;
            args.Argument_FractureApertureControl = comboBox_FractureApertureControl.SelectedIndex;
            args.Argument_HMin_UniformAperture = GetDoubleFromTextBox(textBox_HMin_UniformAperture);
            args.Argument_HMax_UniformAperture = GetDoubleFromTextBox(textBox_HMax_UniformAperture);
            args.Argument_HMin_SizeDependentApertureMultiplier = GetDoubleFromTextBox(textBox_HMin_SizeDependentApertureMultiplier);
            args.Argument_HMax_SizeDependentApertureMultiplier = GetDoubleFromTextBox(textBox_HMax_SizeDependentApertureMultiplier);
            args.Argument_DynamicApertureMultiplier = GetDoubleFromTextBox(textBox_DynamicApertureMultiplier);
            args.Argument_JRC = GetDoubleFromTextBox(textBox_JRC);
            args.Argument_UCSratio = GetDoubleFromTextBox(textBox_UCSratio);
            args.Argument_InitialNormalStress = GetDoubleFromTextBox(textBox_InitialNormalStress);
            args.Argument_FractureNormalStiffness = GetDoubleFromTextBox(textBox_FractureNormalStiffness);
            args.Argument_MaximumClosure = GetDoubleFromTextBox(textBox_MaximumClosure);

            // Calculation control parameters
            args.Argument_NoFractureSets = GetIntFromNumericBox(numericUpDown_NoFractureSets);
            args.Argument_FractureMode = comboBox_FractureMode.SelectedIndex;
            args.Argument_CheckAlluFStressShadows = checkBox_CheckAlluFStressShadows.Checked;
            args.Argument_AnisotropyCutoff = GetDoubleFromTextBox(textBox_AnisotropyCutoff);
            args.Argument_AllowReverseFractures = checkBox_AllowReverseFractures.Checked;
            args.Argument_HorizontalUpscalingFactor = GetIntFromNumericBox(numericUpDown_HorizontalUpscalingFactor);
            args.Argument_MaxTSDuration = GetDoubleFromTextBox(textBox_MaxTSDuration);
            args.Argument_Max_TS_MFP33_increase = GetDoubleFromTextBox(textBox_Max_TS_MFP33_increase);
            args.Argument_MinimumImplicitMicrofractureRadius = GetDoubleFromTextBox(textBox_MinimumImplicitMicrofractureRadius);
            args.Argument_No_r_bins = GetIntFromTextBox(textBox_No_r_bins);
            // Calculation termination controls
            args.Argument_DeformationDuration = GetDoubleFromTextBox(textBox_DeformationDuration);
            args.Argument_MaxNoTimesteps = GetIntFromTextBox(textBox_MaxNoTimesteps);
            args.Argument_Historic_MFP33_TerminationRatio = GetDoubleFromTextBox(textBox_Historic_MFP33_TerminationRatio);
            args.Argument_Active_MFP30_TerminationRatio = GetDoubleFromTextBox(textBox_Active_MFP30_TerminationRatio);
            args.Argument_Minimum_ClearZone_Volume = GetDoubleFromTextBox(textBox_Minimum_ClearZone_Volume);
            // DFN geometry controls
            args.Argument_CropAtGridBoundary = checkBox_CropAtGridBoundary.Checked;
            args.Argument_LinkParallelFractures = checkBox_LinkParallelFractures.Checked;
            args.Argument_MaxConsistencyAngle = GetDoubleFromTextBox(textBox_MaxConsistencyAngle);
            args.Argument_MinimumLayerThickness = GetDoubleFromTextBox(textBox_MinimumLayerThickness);
            args.Argument_CreateTriangularFractureSegments = checkBox_CreateTriangularFractureSegments.Checked;
            args.Argument_ProbabilisticFractureNucleationLimit = GetDoubleFromTextBox(textBox_ProbabilisticFractureNucleationLimit);
            args.Argument_PropagateFracturesInNucleationOrder = checkBox_PropagateFracturesInNucleationOrder.Checked;
            args.Argument_SearchAdjacentGridblocks = comboBox_SearchAdjacentGridblocks.SelectedIndex;
            args.Argument_MinimumExplicitMicrofractureRadius = GetDoubleFromTextBox(textBox_MinimumExplicitMicrofractureRadius);
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
                    presentationBox_Grid.Text = gr.Name;
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

        private void UpdatePropertyPresentationBox(Property gprop, Slb.Ocean.Petrel.UI.Controls.PresentationBox pBox)
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
                    pBox.Text = gprop.Name;
                IImageInfoFactory propImgIF = CoreSystem.GetService<IImageInfoFactory>(gprop);
                if (propImgIF != null)
                {
                    ImageInfo propImage = propImgIF.GetImageInfo(gprop);
                    pBox.Image = propImage.GetDisplayImage(new ImageInfoContext());
                }
                else
                    pBox.Image = PetrelImages.Property;
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
        #endregion

        private void numericUpDown_NoFractureSets_ValueChanged(object sender, EventArgs e)
        {
            // When the number of fracture sets is changed, adjust the flag to check microfractures against stress shadows of all macrofractures regardless of set accordingly
            if (numericUpDown_NoFractureSets.Value > 2)
                checkBox_CheckAlluFStressShadows.Checked = true;
            else
                checkBox_CheckAlluFStressShadows.Checked = false;
        }
    }
}
