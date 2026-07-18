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
    public partial class CleavageUI : Form
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
        /// Initializes a new instance of the <see cref="CleavageUI"/> class 
        /// </summary>
        /// <param name="args">the arguments</param>
        /// <param name="callingDialog">Reference to the dialog box that called this</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        internal CleavageUI(DFMGeneratorWorkstep.Arguments args, DFMGeneratorUI callingDialog, WorkflowContext context)
        {
            InitializeComponent();

            this.args = args;
            this.context = context;
            this.callingDialog = callingDialog;
            updateUIFromArgs();

            this.btn_Cleavage_OK.Image = PetrelImages.OK;
            this.btn_Cleavage_Cancel.Image = PetrelImages.Cancel;

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
            UpdatePropertyPresentationBox(args.Argument_Cleavage1_Azimuth, presentationBox_Cleavage1_Azimuth);
            UpdatePropertyPresentationBox(args.Argument_Cleavage1_Dip, presentationBox_Cleavage1_Dip);
            UpdatePropertyPresentationBox(args.Argument_Cleavage1_CrackSurfaceEnergy, presentationBox_Cleavage1_CrackSurfaceEnergy);
            UpdatePropertyPresentationBox(args.Argument_Cleavage1_FrictionCoefficient, presentationBox_Cleavage1_FrictionCoefficient);
            UpdateTextBox(args.Argument_Cleavage1_Azimuth_default, unitTextBox_Cleavage1_Azimuth_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth, label_Cleavage1_Azimuth_Units);
            UpdateTextBox(args.Argument_Cleavage1_Dip_default, unitTextBox_Cleavage1_Dip_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAngle, label_Cleavage1_Dip_Units);
            UpdateTextBox(args.Argument_Cleavage1_CrackSurfaceEnergy_default, unitTextBox_Cleavage1_CrackSurfaceEnergy_default, PetrelProject.WellKnownTemplates.PetrophysicalGroup.SurfaceTension, label_Cleavage1_CrackSurfaceEnergy_Units);
            UpdateTextBox(args.Argument_Cleavage1_FrictionCoefficient_default, unitTextBox_Cleavage1_FrictionCoefficient_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);

            UpdatePropertyPresentationBox(args.Argument_Cleavage2_Azimuth, presentationBox_Cleavage2_Azimuth);
            UpdatePropertyPresentationBox(args.Argument_Cleavage2_Dip, presentationBox_Cleavage2_Dip);
            UpdatePropertyPresentationBox(args.Argument_Cleavage2_CrackSurfaceEnergy, presentationBox_Cleavage2_CrackSurfaceEnergy);
            UpdatePropertyPresentationBox(args.Argument_Cleavage2_FrictionCoefficient, presentationBox_Cleavage2_FrictionCoefficient);
            UpdateTextBox(args.Argument_Cleavage2_Azimuth_default, unitTextBox_Cleavage2_Azimuth_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAzimuth, label_Cleavage2_Azimuth_Units);
            UpdateTextBox(args.Argument_Cleavage2_Dip_default, unitTextBox_Cleavage2_Dip_default, PetrelProject.WellKnownTemplates.GeometricalGroup.DipAngle, label_Cleavage2_Dip_Units);
            UpdateTextBox(args.Argument_Cleavage2_CrackSurfaceEnergy_default, unitTextBox_Cleavage2_CrackSurfaceEnergy_default, PetrelProject.WellKnownTemplates.PetrophysicalGroup.SurfaceTension, label_Cleavage2_CrackSurfaceEnergy_Units);
            UpdateTextBox(args.Argument_Cleavage2_FrictionCoefficient_default, unitTextBox_Cleavage2_FrictionCoefficient_default, PetrelProject.WellKnownTemplates.MiscellaneousGroup.General);
        }

        private void updateArgsFromUI()
        {
            // Write data to the argument package
            args.Argument_Cleavage1_Azimuth = presentationBox_Cleavage1_Azimuth.Tag as Property;
            args.Argument_Cleavage1_Dip = presentationBox_Cleavage1_Dip.Tag as Property;
            args.Argument_Cleavage1_CrackSurfaceEnergy = presentationBox_Cleavage1_CrackSurfaceEnergy.Tag as Property;
            args.Argument_Cleavage1_FrictionCoefficient = presentationBox_Cleavage1_FrictionCoefficient.Tag as Property;
            args.Argument_Cleavage1_Azimuth_default = GetDoubleFromTextBox(unitTextBox_Cleavage1_Azimuth_default);
            args.Argument_Cleavage1_Dip_default = GetDoubleFromTextBox(unitTextBox_Cleavage1_Dip_default);
            args.Argument_Cleavage1_CrackSurfaceEnergy_default = GetDoubleFromTextBox(unitTextBox_Cleavage1_CrackSurfaceEnergy_default);
            args.Argument_Cleavage1_FrictionCoefficient_default = GetDoubleFromTextBox(unitTextBox_Cleavage1_FrictionCoefficient_default);

            args.Argument_Cleavage2_Azimuth = presentationBox_Cleavage2_Azimuth.Tag as Property;
            args.Argument_Cleavage2_Dip = presentationBox_Cleavage2_Dip.Tag as Property;
            args.Argument_Cleavage2_CrackSurfaceEnergy = presentationBox_Cleavage2_CrackSurfaceEnergy.Tag as Property;
            args.Argument_Cleavage2_FrictionCoefficient = presentationBox_Cleavage2_FrictionCoefficient.Tag as Property;
            args.Argument_Cleavage2_Azimuth_default = GetDoubleFromTextBox(unitTextBox_Cleavage2_Azimuth_default);
            args.Argument_Cleavage2_Dip_default = GetDoubleFromTextBox(unitTextBox_Cleavage2_Dip_default);
            args.Argument_Cleavage2_CrackSurfaceEnergy_default = GetDoubleFromTextBox(unitTextBox_Cleavage2_CrackSurfaceEnergy_default);
            args.Argument_Cleavage2_FrictionCoefficient_default = GetDoubleFromTextBox(unitTextBox_Cleavage2_FrictionCoefficient_default);

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

        private void btn_Cleavage_OK_Click(object sender, EventArgs e)
        {
            updateArgsFromUI();
            this.FindForm().Close();
        }

        private void btn_Cleavage_Cancel_Click(object sender, EventArgs e)
        {
            this.FindForm().Close();
        }

        private void dropTarget_Cleavage1_Azimuth_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage1_Azimuth);
        }

        private void dropTarget_Cleavage1_Dip_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage1_Dip);
        }


        private void dropTarget_Cleavage1_FrictionCoefficient_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage1_FrictionCoefficient);
        }

        private void dropTarget_Cleavage1_CrackSurfaceEnergy_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage1_CrackSurfaceEnergy);
        }

        private void dropTarget_Cleavage2_Azimuth_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage2_Azimuth);
        }

        private void dropTarget_Cleavage2_Dip_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage2_Dip);
        }

        private void dropTarget_Cleavage2_FrictionCoefficient_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage2_FrictionCoefficient);
        }

        private void dropTarget_Cleavage2_CrackSurfaceEnergy_DragDrop(object sender, DragEventArgs e)
        {
            Property droppedProperty = e.Data.GetData(typeof(object)) as Property;
            UpdatePropertyPresentationBox(droppedProperty, presentationBox_Cleavage2_CrackSurfaceEnergy);
        }

        private void presentationBox_Cleavage1_Azimuth_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage1_Azimuth);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage1_Dip_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage1_Dip);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage1_FrictionCoefficient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage1_FrictionCoefficient);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage1_CrackSurfaceEnergy_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage1_CrackSurfaceEnergy);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage2_Azimuth_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage2_Azimuth);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage2_Dip_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage2_Dip);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage2_FrictionCoefficient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage2_FrictionCoefficient);
                e.Handled = true;
            }
        }

        private void presentationBox_Cleavage2_CrackSurfaceEnergy_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdatePropertyPresentationBox(Property.NullObject, presentationBox_Cleavage2_CrackSurfaceEnergy);
                e.Handled = true;
            }
        }

        private void CleavageUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Remove the event handlers when the form is closed
            context.ArgumentPackageChanged -= context_ArgumentPackageChanged;

            // Remove the Deformation Episode UI from the list in the calling dialog
            callingDialog.RemoveCleavageUI();
        }
        #endregion
    }
}
