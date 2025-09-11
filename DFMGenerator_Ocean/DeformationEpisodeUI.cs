using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using Slb.Ocean.Petrel.UI.Controls;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Simulation;

namespace DFMGenerator_Ocean
{
    public partial class DeformationEpisodeUI : Form
    {
        /// <summary>
        /// The argument package instance being edited by the UI.
        /// </summary>
        private DFMGeneratorWorkstep.Arguments args;
        /// <summary>
        /// Contains the actual underlaying context.
        /// </summary>
        private WorkflowContext context;
        /// <summary>
        /// Index number of the deformation episode to edit
        /// </summary>
        private int deformationEpisodeIndex;
        /// <summary>
        /// Reference to the DFM Generator dialog box that called this dialog
        /// </summary>
        private DFMGeneratorUI callingDialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeformationEpisodeUI"/> class.
        /// </summary>
        /// <param name="args">the arguments</param>
        /// <param name="callingDialog">Reference to the dialog box that called this</param>
        /// <param name="deformationEpisodeIndex">Index number of the deformation episode to edit</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        internal DeformationEpisodeUI(DFMGeneratorWorkstep.Arguments args, DFMGeneratorUI callingDialog,  int deformationEpisodeIndex, WorkflowContext context)
        {
            InitializeComponent();

            this.args = args;
            this.deformationEpisodeIndex = deformationEpisodeIndex;
            this.context = context;
            this.callingDialog = callingDialog;
            updateUIFromArgs();

            this.btn_DE_OK.Image = PetrelImages.OK;
            this.btn_DE_Cancel.Image = PetrelImages.Cancel;

            context.ArgumentPackageChanged += new EventHandler<WorkflowContext.ArgumentPackageChangedEventArgs>(context_ArgumentPackageChanged);
            callingDialog.DeformationEpisodeRemoved += new EventHandler<RemoveDeformationEpisodeEventArgs>(dfmGeneratorUI_DeformationEpisodeRemoved);
        }

        #region UI_update

        void context_ArgumentPackageChanged(object sender, WorkflowContext.ArgumentPackageChangedEventArgs e)
        {
            if (sender != this)
                updateUIFromArgs();
        }

        void dfmGeneratorUI_DeformationEpisodeRemoved(object sender, RemoveDeformationEpisodeEventArgs e)
        {
            int episodeRemoved = e.RemovedEpisodeIndex;
            // If this deformation episode has been removed, close this form immediately without saving changes
            if (episodeRemoved == deformationEpisodeIndex)
                this.FindForm().Close();
            // If the deformation episode removed has a lower index than this one, modify the index of this episode accordingly
            else if (episodeRemoved < deformationEpisodeIndex)
                deformationEpisodeIndex--;
        }

        /// <summary>
        /// Updates the data displayed on the UI.
        /// </summary>
        private void updateUIFromArgs()
        {
            this.Text = args.DeformationEpisode(deformationEpisodeIndex);

            // Static load tab
            UpdateTextBox(args.DeformationEpisodeDuration(deformationEpisodeIndex), unitTextBox_DE_DeformationDuration);
            UpdateComboBox(args.DeformationEpisodeTimeUnits(deformationEpisodeIndex), comboBox_DE_TimeUnits);
            UpdatePropertyPresentationBox(args.EhminAzi(deformationEpisodeIndex), presentationBox_DE_EhminAzi);
            // Time unit conversion for the load properties EhminRate, EhmaxRate, AppliedOverpressureRate, AppliedTemperatureChange and AppliedUpliftRate are carried out when the grid is populated in ExecuteSimple(), as there are no inbuilt Petrel units for these rates
            // Therefore these properties are stored in geological time units (typically Ma), not SI units (/s)
            // Unit labelling must be handled manually
            UpdatePropertyPresentationBox(args.EhminRate(deformationEpisodeIndex), presentationBox_DE_EhminRate);
            UpdatePropertyPresentationBox(args.EhmaxRate(deformationEpisodeIndex), presentationBox_DE_EhmaxRate);
            UpdatePropertyPresentationBox(args.AppliedOverpressureRate(deformationEpisodeIndex), presentationBox_DE_OPRate);
            UpdatePropertyPresentationBox(args.AppliedTemperatureChange(deformationEpisodeIndex), presentationBox_DE_TempChange);
            UpdatePropertyPresentationBox(args.AppliedUpliftRate(deformationEpisodeIndex), presentationBox_DE_UpliftRate);
            UpdateTextBox(args.EhminAzi_default(deformationEpisodeIndex), unitTextBox_DE_EhminAzi_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth, label_DE_EhminAzi_Units);
            UpdateTextBox(args.EhminRate_default(deformationEpisodeIndex), unitTextBox_DE_EhminRate_default);
            UpdateTextBox(args.EhmaxRate_default(deformationEpisodeIndex), unitTextBox_DE_EhmaxRate_default);
            UpdateTextBox(args.AppliedOverpressureRate_default(deformationEpisodeIndex), unitTextBox_DE_OPRate_default, PetrelProject.WellKnownTemplates.PetrophysicalGroup.Pressure); // Units contain a time component; label will be set by SetLoadRateUnits()
            UpdateTextBox(args.AppliedTemperatureChange_default(deformationEpisodeIndex), unitTextBox_DE_TempChange_default, PetrelProject.WellKnownTemplates.GeophysicalGroup.AbsoluteTemperature); // Units contain a time component; label will be set by SetLoadRateUnits()
            UpdateTextBox(args.AppliedUpliftRate_default(deformationEpisodeIndex), unitTextBox_DE_UpliftRate_default, PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth); // Units contain a time component; label will be set by SetLoadRateUnits()
            UpdateTextBox(args.StressArchingFactor(deformationEpisodeIndex), unitTextBox_DE_StressArchingFactor);
            SetLoadRateUnits();

            // Dynamic load tab
            UpdateCasePresentationBox(args.SimulationCase(deformationEpisodeIndex), presentationBox_DE_SimCase);
            UpdateComboBoxExcludingOption1(args.GetStressStateDefinition(deformationEpisodeIndex), comboBox_DE_StressStateDefinition);
            // NB either standard grid properties or simulation case results can be input for the dynamic load stress and fluid pressure
            if (args.AbsoluteStressXXProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.AbsoluteStressXXProperty(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressXX);
            else
                UpdateGridResultPresentationBox(args.AbsoluteStressXXTimeSeries(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressXX);
            if (args.AbsoluteStressYYProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.AbsoluteStressYYProperty(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressYY);
            else
                UpdateGridResultPresentationBox(args.AbsoluteStressYYTimeSeries(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressYY);
            if (args.AbsoluteStressXYProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.AbsoluteStressXYProperty(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressXY);
            else
                UpdateGridResultPresentationBox(args.AbsoluteStressXYTimeSeries(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressXY);
            if (args.AbsoluteStressZXProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.AbsoluteStressZXProperty(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressZX);
            else
                UpdateGridResultPresentationBox(args.AbsoluteStressZXTimeSeries(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressZX);
            if (args.AbsoluteStressYZProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.AbsoluteStressYZProperty(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressYZ);
            else
                UpdateGridResultPresentationBox(args.AbsoluteStressYZTimeSeries(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressYZ);
            if (args.AbsoluteStressZZProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.AbsoluteStressZZProperty(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressZZ);
            else
                UpdateGridResultPresentationBox(args.AbsoluteStressZZTimeSeries(deformationEpisodeIndex), presentationBox_DE_AbsoluteStressZZ);
            if (args.FluidPressureProperty(deformationEpisodeIndex) != null)
                UpdatePropertyPresentationBox(args.FluidPressureProperty(deformationEpisodeIndex), presentationBox_DE_FP);
            else
                UpdateGridResultPresentationBox(args.FluidPressureTimeSeries(deformationEpisodeIndex), presentationBox_DE_FP);
        }

        private void updateArgsFromUI()
        {
            // Write data to the argument package
            // Static load tab
            args.DeformationEpisodeDuration(GetDoubleFromTextBox(unitTextBox_DE_DeformationDuration), deformationEpisodeIndex);
            args.DeformationEpisodeTimeUnits(comboBox_DE_TimeUnits.SelectedIndex, deformationEpisodeIndex);
            args.EhminAzi(presentationBox_DE_EhminAzi.Tag as Property, deformationEpisodeIndex);
            args.EhminRate(presentationBox_DE_EhminRate.Tag as Property, deformationEpisodeIndex);
            args.EhmaxRate(presentationBox_DE_EhmaxRate.Tag as Property, deformationEpisodeIndex);
            args.AppliedOverpressureRate(presentationBox_DE_OPRate.Tag as Property, deformationEpisodeIndex);
            args.AppliedTemperatureChange(presentationBox_DE_TempChange.Tag as Property, deformationEpisodeIndex);
            args.AppliedUpliftRate(presentationBox_DE_UpliftRate.Tag as Property, deformationEpisodeIndex);
            args.EhminAzi_default(GetDoubleFromTextBox(unitTextBox_DE_EhminAzi_default), deformationEpisodeIndex);
            args.EhminRate_default(GetDoubleFromTextBox(unitTextBox_DE_EhminRate_default), deformationEpisodeIndex);
            args.EhmaxRate_default(GetDoubleFromTextBox(unitTextBox_DE_EhmaxRate_default), deformationEpisodeIndex);
            args.AppliedOverpressureRate_default(GetDoubleFromTextBox(unitTextBox_DE_OPRate_default), deformationEpisodeIndex);
            args.AppliedTemperatureChange_default(GetDoubleFromTextBox(unitTextBox_DE_TempChange_default), deformationEpisodeIndex);
            args.AppliedUpliftRate_default(GetDoubleFromTextBox(unitTextBox_DE_UpliftRate_default), deformationEpisodeIndex);
            args.StressArchingFactor(GetDoubleFromTextBox(unitTextBox_DE_StressArchingFactor), deformationEpisodeIndex);

            // Dynamic load tab
            args.SimulationCase(presentationBox_DE_SimCase.Tag as Case, deformationEpisodeIndex);
            args.SetStressStateDefinition(comboBox_DE_StressStateDefinition.SelectedIndex + 1, deformationEpisodeIndex);
            // NB either standard grid properties or simulation case results can be input for the standard elastic and plastic parameters
            try
            {
                Property stressXX = presentationBox_DE_AbsoluteStressXX.Tag as Property;
                args.AbsoluteStressXXProperty(stressXX, deformationEpisodeIndex);
                if (stressXX != Property.NullObject)
                    args.AbsoluteStressXXTimeSeries(null, deformationEpisodeIndex);
                else
                    args.AbsoluteStressXXTimeSeries(presentationBox_DE_AbsoluteStressXX.Tag as GridResult, deformationEpisodeIndex);
                Property stressYY = presentationBox_DE_AbsoluteStressYY.Tag as Property;
                args.AbsoluteStressYYProperty(stressYY, deformationEpisodeIndex);
                if (stressYY != Property.NullObject)
                    args.AbsoluteStressYYTimeSeries(null, deformationEpisodeIndex);
                else
                    args.AbsoluteStressYYTimeSeries(presentationBox_DE_AbsoluteStressYY.Tag as GridResult, deformationEpisodeIndex);
                Property stressXY = presentationBox_DE_AbsoluteStressXY.Tag as Property;
                args.AbsoluteStressXYProperty(stressXY, deformationEpisodeIndex);
                if (stressXY != Property.NullObject)
                    args.AbsoluteStressXYTimeSeries(null, deformationEpisodeIndex);
                else
                    args.AbsoluteStressXYTimeSeries(presentationBox_DE_AbsoluteStressXY.Tag as GridResult, deformationEpisodeIndex);
                Property stressZX = presentationBox_DE_AbsoluteStressZX.Tag as Property;
                args.AbsoluteStressZXProperty(stressZX, deformationEpisodeIndex);
                if (stressZX != Property.NullObject)
                    args.AbsoluteStressZXTimeSeries(null, deformationEpisodeIndex);
                else
                    args.AbsoluteStressZXTimeSeries(presentationBox_DE_AbsoluteStressZX.Tag as GridResult, deformationEpisodeIndex);
                Property stressYZ = presentationBox_DE_AbsoluteStressYZ.Tag as Property;
                args.AbsoluteStressYZProperty(stressYZ, deformationEpisodeIndex);
                if (stressYZ != Property.NullObject)
                    args.AbsoluteStressYZTimeSeries(null, deformationEpisodeIndex);
                else
                    args.AbsoluteStressYZTimeSeries(presentationBox_DE_AbsoluteStressYZ.Tag as GridResult, deformationEpisodeIndex);
                Property stressZZ = presentationBox_DE_AbsoluteStressZZ.Tag as Property;
                args.AbsoluteStressZZProperty(stressZZ, deformationEpisodeIndex);
                if (stressZZ != Property.NullObject)
                    args.AbsoluteStressZZTimeSeries(null, deformationEpisodeIndex);
                else
                    args.AbsoluteStressZZTimeSeries(presentationBox_DE_AbsoluteStressZZ.Tag as GridResult, deformationEpisodeIndex);
                Property FP = presentationBox_DE_FP.Tag as Property;
                args.FluidPressureProperty(FP, deformationEpisodeIndex);
                if (FP != Property.NullObject)
                    args.FluidPressureTimeSeries(null, deformationEpisodeIndex);
                else
                    args.FluidPressureTimeSeries(presentationBox_DE_FP.Tag as GridResult, deformationEpisodeIndex);
            }
            catch (Exception e)
            {
                PetrelLogger.InfoOutputWindow(e.Message);
                PetrelLogger.InfoOutputWindow(e.StackTrace);
            }

            // Update the deformation episode name
            args.GenerateDeformationEpisodeName(deformationEpisodeIndex, true);

            // tell fwk to update LineUI:
            context.OnArgumentPackageChanged(this, new WorkflowContext.ArgumentPackageChangedEventArgs());
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
        private void UpdateGridResultPresentationBox(GridResult gres, PresentationBox pBox)
        {
            if (gres != GridResult.NullObject)
            {
                INameInfoFactory gresNIF = CoreSystem.GetService<INameInfoFactory>(gres);
                if (gresNIF != null)
                {
                    NameInfo gresName = gresNIF.GetNameInfo(gres);
                    pBox.Text = gresName.Name;
                }
                else
                {
                    pBox.Text = gres.Name;
                }
                IImageInfoFactory gresImgIF = CoreSystem.GetService<IImageInfoFactory>(gres);
                if (gresImgIF != null)
                {
                    ImageInfo gresImage = gresImgIF.GetImageInfo(gres);
                    pBox.Image = gresImage.GetDisplayImage(new ImageInfoContext());
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
            pBox.Tag = gres;
        }
        private void UpdateCasePresentationBox(Case cs, PresentationBox pBox)
        {
            if (cs != Case.NullObject)
            {
                INameInfoFactory csNIF = CoreSystem.GetService<INameInfoFactory>(cs);
                if (csNIF != null)
                {
                    NameInfo csName = csNIF.GetNameInfo(cs);
                    pBox.Text = csName.Name;
                }
                else
                {
                    pBox.Text = cs.Name;
                }
                IImageInfoFactory csImgIF = CoreSystem.GetService<IImageInfoFactory>(cs);
                if (csImgIF != null)
                {
                    ImageInfo csImage = csImgIF.GetImageInfo(cs);
                    pBox.Image = csImage.GetDisplayImage(new ImageInfoContext());
                }
                else
                {
                    pBox.Image = PetrelImages.Case;
                }
            }
            else
            {
                pBox.Text = "";
                pBox.Image = null;
            }
            pBox.Tag = cs;
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
        private void UpdateComboBox(int index, Slb.Ocean.Petrel.UI.Controls.ComboBox cBox)
        {
            if (index < 0) index = 0;
            if (index >= cBox.Items.Count) index = cBox.Items.Count - 1;
            cBox.SelectedIndex = index;
        }
        private void UpdateComboBoxExcludingOption1(int index, Slb.Ocean.Petrel.UI.Controls.ComboBox cBox)
        {
            if (index < 1) index = 1;
            if (index > cBox.Items.Count) index = cBox.Items.Count;
            cBox.SelectedIndex = index - 1;
        }
        private double GetDoubleFromTextBox(Slb.Ocean.Petrel.UI.Controls.UnitTextBox tBox)
        {
            try
            {
                // If the text box is blank, return NaN
                if (tBox.TextLength == 0)
                    return double.NaN;
                else
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
                // Convert.ToDouble will throw an exception if the text box is blank, so there is no need to check this
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
        private bool GetBoolFromComboBox(Slb.Ocean.Petrel.UI.Controls.ComboBox cBox)
        {
            if (cBox.SelectedIndex == 0)
                return false;
            else
                return true;
        }

        private void SetLoadRateUnits()
        {
            string rateUnitText = string.Format("/{0}", (DFMGenerator_SharedCode.TimeUnits)comboBox_DE_TimeUnits.SelectedIndex);
            label_DE_EhminRate_Units.Text = rateUnitText;
            label_DE_EhmaxRate_Units.Text = rateUnitText;
            label_DE_OPRate_Units.Text = PetrelUnitSystem.GetDisplayUnit(PetrelProject.WellKnownTemplates.PetrophysicalGroup.Pressure).Symbol + rateUnitText;
            label_DE_TempChange_Units.Text = PetrelUnitSystem.GetDisplayUnit(PetrelProject.WellKnownTemplates.GeophysicalGroup.AbsoluteTemperature).Symbol+ rateUnitText;
            label_DE_UpliftRate_Units.Text = PetrelUnitSystem.GetDisplayUnit(PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth).Symbol+rateUnitText;
        }
        #endregion

        #region EventHandlers
        private void btn_DE_OK_Click(object sender, EventArgs e)
        {
            updateArgsFromUI();
            this.FindForm().Close();
        }

        private void btn_DE_Cancel_Click(object sender, EventArgs e)
        {
            this.FindForm().Close();
        }

        private void dropTarget_DE_EhminAzi_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_DE_EhminAzi);
        }

        private void dropTarget_DE_EhminRate_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_DE_EhminRate);
        }

        private void dropTarget_DE_EhmaxRate_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_DE_EhmaxRate);
        }

        private void dropTarget_DE_OPRate_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_DE_OPRate);
        }

        private void dropTarget_DE_TempChange_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_DE_TempChange);
        }

        private void dropTarget_DE_UpliftRate_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_DE_UpliftRate);
        }

        private void dropTarget_DE_SimCase_DragDrop(object sender, DragEventArgs e)
        {
            Case droppedCase = e.Data.GetData(typeof(object)) as Case;
            UpdateCasePresentationBox(droppedCase, presentationBox_DE_SimCase);
        }

        private void dropTarget_DE_AbsoluteStressXX_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_AbsoluteStressXX);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_AbsoluteStressXX);
        }

        private void dropTarget_DE_AbsoluteStressYY_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_AbsoluteStressYY);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_AbsoluteStressYY);
        }

        private void dropTarget_DE_AbsoluteStressXY_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_AbsoluteStressXY);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_AbsoluteStressXY);
        }

        private void dropTarget_DE_AbsoluteStressZX_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_AbsoluteStressZX);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_AbsoluteStressZX);
        }

        private void dropTarget_DE_AbsoluteStressYZ_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_AbsoluteStressYZ);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_AbsoluteStressYZ);
        }

        private void dropTarget_DE_AbsoluteStressZZ_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_AbsoluteStressZZ);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_AbsoluteStressZZ);
        }

        private void dropTarget_DE_FP_DragDrop(object sender, DragEventArgs e)
        {
            object droppedObject = e.Data.GetData(typeof(object));
            if ((droppedObject != null) && (droppedObject.GetType() == typeof(GridResult)))
                UpdateGridResultPresentationBox(droppedObject as GridResult, presentationBox_DE_FP);
            else
                UpdatePropertyPresentationBox(droppedObject as Property, presentationBox_DE_FP);
        }

        private void presentationBox_DE_EhminAzi_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_DE_EhminAzi);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_EhminRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_DE_EhminRate);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_EhmaxRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_DE_EhmaxRate);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_OPRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_DE_OPRate);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_TempChange_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_DE_TempChange);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_UpliftRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_DE_UpliftRate);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_SimCase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateCasePresentationBox(Case.NullObject, presentationBox_DE_SimCase);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_AbsoluteStressXX_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_AbsoluteStressXX);
                e.Handled = true;
            }
        }

        private void presentationBox__DE_AbsoluteStressYY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_AbsoluteStressYY);
                e.Handled = true;
            }
        }

        private void presentationBox__DE_AbsoluteStressXY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_AbsoluteStressXY);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_AbsoluteStressZX_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_AbsoluteStressZX);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_AbsoluteStressYZ_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_AbsoluteStressYZ);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_AbsoluteStressZZ_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_AbsoluteStressZZ);
                e.Handled = true;
            }
        }

        private void presentationBox_DE_FP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateGridResultPresentationBox(GridResult.NullObject, presentationBox_DE_FP);
                e.Handled = true;
            }
        }

        private void comboBox_DE_TimeUnits_SelectedIndexChanged(object sender, EventArgs e)
        {
            // The load rate units will have changed; update the display
            SetLoadRateUnits();
        }

        private void DeformationEpisodeUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Remove the event handlers when the form is closed
            context.ArgumentPackageChanged -= context_ArgumentPackageChanged;
            callingDialog.DeformationEpisodeRemoved -= dfmGeneratorUI_DeformationEpisodeRemoved;

            // Remove the Deformation Episode UI from the list in the calling dialog
            callingDialog.RemoveDeformationEpisodeUI(this);
        }
        #endregion
    }
}
