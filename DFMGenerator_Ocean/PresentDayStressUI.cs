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
    public partial class PresentDayStressUI : Form
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
        /// Reference to the DFM Generator dialog box that called this dialog
        /// </summary>
        private DFMGeneratorUI callingDialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentDayStressUI"/> class 
        /// </summary>
        /// <param name="args">the arguments</param>
        /// <param name="callingDialog">Reference to the dialog box that called this</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        internal PresentDayStressUI(DFMGeneratorWorkstep.Arguments args, DFMGeneratorUI callingDialog, WorkflowContext context)
        {
            InitializeComponent();

            this.args = args;
            this.context = context;
            this.callingDialog = callingDialog;
            updateUIFromArgs();

            this.btn_PDS_OK.Image = PetrelImages.OK;
            this.btn_PDS_Cancel.Image = PetrelImages.Cancel;

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
            UpdateComboBox(args.Argument_PresentDayStressInput, comboBox_PDS_StressStateDefinition);
            UpdatePropertyPresentationBox(args.Argument_EhminAzi_PresentDay, presentationBox_PDS_EhminAzi);
            UpdatePropertyPresentationBox(args.Argument_Ehmin_PresentDay, presentationBox_PDS_Ehmin);
            UpdatePropertyPresentationBox(args.Argument_Ehmax_PresentDay, presentationBox_PDS_Ehmax);
            UpdatePropertyPresentationBox(args.Argument_AppliedOverpressure_PresentDay, presentationBox_PDS_OP);
            UpdatePropertyPresentationBox(args.Argument_YoungsMod_PresentDay, presentationBox_PDS_YoungsMod);
            UpdatePropertyPresentationBox(args.Argument_PoissonsRatio_PresentDay, presentationBox_PDS_PoissonsRatio);
            UpdatePropertyPresentationBox(args.Argument_BiotCoefficient_PresentDay, presentationBox_PDS_BiotCoefficient);
            UpdateTextBox(args.Argument_EhminAzi_PresentDay_default, unitTextBox_PDS_EhminAzi_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth, label_PDS_EhminAzi_Units);
            UpdateTextBox(args.Argument_Ehmin_PresentDay_default, unitTextBox_PDS_Ehmin_default, PetrelProject.WellKnownTemplates.GeomechanicGroup.Strain);
            UpdateTextBox(args.Argument_Ehmax_PresentDay_default, unitTextBox_PDS_Ehmax_default, PetrelProject.WellKnownTemplates.GeomechanicGroup.Strain);
            UpdateTextBox(args.Argument_AppliedOverpressure_PresentDay_default, unitTextBox_PDS_OP_default, PetrelProject.WellKnownTemplates.PetrophysicalGroup.Pressure, label_PDS_OP_Units);
            UpdateTextBox(args.Argument_YoungsMod_PresentDay_default, unitTextBox_PDS_YoungsMod_default, PetrelProject.WellKnownTemplates.GeomechanicGroup.YoungsModulus, label_PDS_YoungsMod_Units);
            UpdateTextBox(args.Argument_PoissonsRatio_PresentDay_default, unitTextBox_PDS_PoissonsRatio_default, PetrelProject.WellKnownTemplates.GeophysicalGroup.PoissonRatio);
            UpdateTextBox(args.Argument_BiotCoefficient_PresentDay_default, unitTextBox_PDS_BiotCoefficient_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdateTextBox(args.Argument_InitialStressRelaxation_PresentDay, unitTextBox_PDS_StressRelaxation, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
            UpdatePropertyPresentationBox(args.Argument_Sxx_PresentDay, presentationBox_PDS_StressXX);
            UpdatePropertyPresentationBox(args.Argument_Syy_PresentDay, presentationBox_PDS_StressYY);
            UpdatePropertyPresentationBox(args.Argument_Szz_PresentDay, presentationBox_PDS_StressZZ);
            UpdatePropertyPresentationBox(args.Argument_Sxy_PresentDay, presentationBox_PDS_StressXY);
            UpdatePropertyPresentationBox(args.Argument_Syz_PresentDay, presentationBox_PDS_StressYZ);
            UpdatePropertyPresentationBox(args.Argument_Szx_PresentDay, presentationBox_PDS_StressZX);
            UpdatePropertyPresentationBox(args.Argument_FluidPressure_PresentDay, presentationBox_PDS_FP);
        }

        private void updateArgsFromUI()
        {
            // Write data to the argument package
            args.Argument_PresentDayStressInput = comboBox_PDS_StressStateDefinition.SelectedIndex;
            args.Argument_EhminAzi_PresentDay = presentationBox_PDS_EhminAzi.Tag as Property;
            args.Argument_Ehmin_PresentDay = presentationBox_PDS_Ehmin.Tag as Property;
            args.Argument_Ehmax_PresentDay = presentationBox_PDS_Ehmax.Tag as Property;
            args.Argument_AppliedOverpressure_PresentDay = presentationBox_PDS_OP.Tag as Property;
            args.Argument_YoungsMod_PresentDay = presentationBox_PDS_YoungsMod.Tag as Property;
            args.Argument_PoissonsRatio_PresentDay = presentationBox_PDS_PoissonsRatio.Tag as Property;
            args.Argument_BiotCoefficient_PresentDay = presentationBox_PDS_BiotCoefficient.Tag as Property;
            args.Argument_EhminAzi_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_EhminAzi_default);
            args.Argument_Ehmin_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_Ehmin_default);
            args.Argument_Ehmax_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_Ehmax_default);
            args.Argument_AppliedOverpressure_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_OP_default);
            args.Argument_YoungsMod_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_YoungsMod_default);
            args.Argument_PoissonsRatio_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_PoissonsRatio_default);
            args.Argument_BiotCoefficient_PresentDay_default = GetDoubleFromTextBox(unitTextBox_PDS_BiotCoefficient_default);
            args.Argument_InitialStressRelaxation_PresentDay = GetDoubleFromTextBox(unitTextBox_PDS_StressRelaxation);
            args.Argument_Sxx_PresentDay = presentationBox_PDS_StressXX.Tag as Property;
            args.Argument_Syy_PresentDay = presentationBox_PDS_StressYY.Tag as Property;
            args.Argument_Szz_PresentDay = presentationBox_PDS_StressZZ.Tag as Property;
            args.Argument_Sxy_PresentDay = presentationBox_PDS_StressXY.Tag as Property;
            args.Argument_Syz_PresentDay = presentationBox_PDS_StressYZ.Tag as Property;
            args.Argument_Szx_PresentDay = presentationBox_PDS_StressZX.Tag as Property;
            args.Argument_FluidPressure_PresentDay = presentationBox_PDS_FP.Tag as Property;

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
        #endregion

        #region EventHandlers

        private void btn_PDS_OK_Click(object sender, EventArgs e)
        {
            updateArgsFromUI();
            this.FindForm().Close();
        }

        private void btn_PDS_Cancel_Click(object sender, EventArgs e)
        {
            this.FindForm().Close();
        }

        private void switchTab(bool enableStrainTab, bool enableStressTab, bool enableFP)
        {
            // Strain tab
            label_PDS_EhminAzi.Enabled = enableStrainTab;
            dropTarget_PDS_EhminAzi.Enabled = enableStrainTab;
            presentationBox_PDS_EhminAzi.Enabled = enableStrainTab;
            label_PDS_EhminAzi_default.Enabled = enableStrainTab;
            unitTextBox_PDS_EhminAzi_default.Enabled = enableStrainTab;
            label_PDS_EhminAzi_Units.Enabled = enableStrainTab;

            label_PDS_Ehmin.Enabled = enableStrainTab;
            dropTarget_PDS_Ehmin.Enabled = enableStrainTab;
            presentationBox_PDS_Ehmin.Enabled = enableStrainTab;
            label_PDS_Ehmin_default.Enabled = enableStrainTab;
            unitTextBox_PDS_Ehmin_default.Enabled = enableStrainTab;

            label_PDS_Ehmax.Enabled = enableStrainTab;
            dropTarget_PDS_Ehmax.Enabled = enableStrainTab;
            presentationBox_PDS_Ehmax.Enabled = enableStrainTab;
            label_PDS_Ehmax_default.Enabled = enableStrainTab;
            unitTextBox_PDS_Ehmax_default.Enabled = enableStrainTab;

            label_PDS_OP.Enabled = enableStrainTab;
            dropTarget_PDS_OP.Enabled = enableStrainTab;
            presentationBox_PDS_OP.Enabled = enableStrainTab;
            label_PDS_OP_default.Enabled = enableStrainTab;
            unitTextBox_PDS_OP_default.Enabled = enableStrainTab;
            label_PDS_OP_Units.Enabled = enableStrainTab;

            label_PDS_YoungsMod.Enabled = enableStrainTab;
            dropTarget_PDS_YoungsMod.Enabled = enableStrainTab;
            presentationBox_PDS_YoungsMod.Enabled = enableStrainTab;
            label_PDS_YoungsMod_default.Enabled = enableStrainTab;
            unitTextBox_PDS_YoungsMod_default.Enabled = enableStrainTab;
            label_PDS_YoungsMod_Units.Enabled = enableStrainTab;

            label_PDS_PoissonsRatio.Enabled = enableStrainTab;
            dropTarget_PDS_PoissonsRatio.Enabled = enableStrainTab;
            presentationBox_PDS_PoissonsRatio.Enabled = enableStrainTab;
            label_PDS_PoissonsRatio_default.Enabled = enableStrainTab;
            unitTextBox_PDS_PoissonsRatio_default.Enabled = enableStrainTab;

            label_PDS_BiotCoefficient.Enabled = enableStrainTab;
            dropTarget_PDS_BiotCoefficient.Enabled = enableStrainTab;
            presentationBox_PDS_BiotCoefficient.Enabled = enableStrainTab;
            label_PDS_BiotCoefficient_default.Enabled = enableStrainTab;
            unitTextBox_PDS_BiotCoefficient_default.Enabled = enableStrainTab;

            label_PDS_StressRelaxation.Enabled = enableStrainTab;
            unitTextBox_PDS_StressRelaxation.Enabled = enableStrainTab;

            // Stress tab
            label_PDS_StressXX.Enabled = enableStressTab;
            dropTarget_PDS_StressXX.Enabled = enableStressTab;
            presentationBox_PDS_StressXX.Enabled = enableStressTab;

            label_PDS_StressYY.Enabled = enableStressTab;
            dropTarget_PDS_StressYY.Enabled = enableStressTab;
            presentationBox_PDS_StressYY.Enabled = enableStressTab;

            label_PDS_StressZZ.Enabled = enableStressTab;
            dropTarget_PDS_StressZZ.Enabled = enableStressTab;
            presentationBox_PDS_StressZZ.Enabled = enableStressTab;

            label_PDS_StressXY.Enabled = enableStressTab;
            dropTarget_PDS_StressXY.Enabled = enableStressTab;
            presentationBox_PDS_StressXY.Enabled = enableStressTab;

            label_PDS_StressYZ.Enabled = enableStressTab;
            dropTarget_PDS_StressYZ.Enabled = enableStressTab;
            presentationBox_PDS_StressYZ.Enabled = enableStressTab;

            label_PDS_StressZX.Enabled = enableStressTab;
            dropTarget_PDS_StressZX.Enabled = enableStressTab;
            presentationBox_PDS_StressZX.Enabled = enableStressTab;

            label_PDS_FP.Enabled = enableFP;
            dropTarget_PDS_FP.Enabled = enableFP;
            presentationBox_PDS_FP.Enabled = enableFP;
        }

        private void comboBox_PDS_DefinedFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox_PDS_StressStateDefinition.SelectedIndex)
            {
                case 0: // From lithostatic stress and horizontal strain
                    tabControl1.SelectedPage = tabStrain;
                    switchTab(true, false, false);
                    break;
                case 1: // From absolute (total) stress and fluid pressure
                    tabControl1.SelectedPage = tabStress;
                    switchTab(false, true, true);
                    break;
                case 2: // From Terzaghi effective stress
                    tabControl1.SelectedPage = tabStress;
                    switchTab(false, true, false);
                    break;
                default: // Enable everything
                    switchTab(true, true, true);
                    break;
            }
        }

        private void dropTarget_PDS_EhminAzi_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_EhminAzi);
        }

        private void dropTarget_PDS_Ehmin_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_Ehmin);
        }

        private void dropTarget_PDS_Ehmax_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_Ehmax);
        }

        private void dropTarget_PDS_OP_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_OP);
        }

        private void dropTarget_PDS_YoungsMod_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_YoungsMod);
        }

        private void dropTarget_PDS_PoissonsRatio_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_PoissonsRatio);
        }

        private void dropTarget_PDS_BiotCoefficient_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_BiotCoefficient);
        }

        private void dropTarget_PDS_StressXX_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_StressXX);
        }

        private void dropTarget_PDS_StressYY_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_StressYY);
        }

        private void dropTarget_PDS_StressZZ_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_StressZZ);
        }

        private void dropTarget_PDS_StressXY_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_StressXY);
        }

        private void dropTarget_PDS_StressYZ_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_StressYZ);
        }

        private void dropTarget_PDS_StressZX_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_StressZX);
        }

        private void dropTarget_PDS_FP_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_PDS_FP);
        }

        private void presentationBox_PDS_EhminAzi_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_EhminAzi);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_Ehmin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_Ehmin);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_Ehmax_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_Ehmax);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_OP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_OP);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_YoungsMod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_YoungsMod);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_PoissonsRatio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_PoissonsRatio);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_BiotCoefficient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_BiotCoefficient);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_StressXX_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_StressXX);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_StressYY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_StressYY);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_StressZZ_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_StressZZ);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_StressXY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_StressXY);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_StressYZ_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_StressYZ);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_StressZX_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_StressZX);
                e.Handled = true;
            }
        }

        private void presentationBox_PDS_FP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_PDS_FP);
                e.Handled = true;
            }
        }

        private void PresentDayStressUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Remove the event handlers when the form is closed
            context.ArgumentPackageChanged -= context_ArgumentPackageChanged;

            // Remove the Deformation Episode UI from the list in the calling dialog
            callingDialog.RemovePresentDayStressUI();
        }

        #endregion
    }
}
