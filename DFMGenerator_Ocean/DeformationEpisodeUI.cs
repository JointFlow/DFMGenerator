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
        /// Initializes a new instance of the <see cref="DeformationEpisodeUI"/> class.
        /// </summary>
        /// <param name="args">the arguments</param>
        /// <param name="deformationEpisodeIndex">Index number of the deformation episode to edit</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        internal DeformationEpisodeUI(DFMGeneratorWorkstep.Arguments args, int deformationEpisodeIndex, WorkflowContext context)
        {
            InitializeComponent();

            this.args = args;
            this.deformationEpisodeIndex = deformationEpisodeIndex;
            this.context = context;
            updateUIFromArgs();

            this.btn_DE_OK.Image = PetrelImages.OK;
            this.btn_DE_Cancel.Image = PetrelImages.Cancel;

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
            this.Text = args.DeformationEpisode(deformationEpisodeIndex);
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
            UpdateTextBox(args.AppliedOverpressureRate_default(deformationEpisodeIndex), unitTextBox_DE_OPRate_default, PetrelProject.WellKnownTemplates.PetrophysicalGroup.Pressure, label_DE_OPRate_Units);
            UpdateTextBox(args.AppliedTemperatureChange_default(deformationEpisodeIndex), unitTextBox_DE_TempChange_default, PetrelProject.WellKnownTemplates.GeophysicalGroup.AbsoluteTemperature, label_DE_TempChange_Units);
            UpdateTextBox(args.AppliedUpliftRate_default(deformationEpisodeIndex), unitTextBox_DE_UpliftRate_default, PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth, label_DE_UpliftRate_Units);
            UpdateTextBox(args.StressArchingFactor(deformationEpisodeIndex), unitTextBox_DE_StressArchingFactor);
            SetStrainRateUnits();
        }

        private void updateArgsFromUI()
        {
            // Write data to the argument package
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

        private void SetStrainRateUnits()
        {
            string rateUnitText = string.Format("/{0}", (DFMGenerator_SharedCode.TimeUnits)comboBox_DE_TimeUnits.SelectedIndex);
            label_DE_EhminRate_Units.Text += rateUnitText;
            label_DE_EhmaxRate_Units.Text += rateUnitText;
            label_DE_OPRate_Units.Text += rateUnitText;
            label_DE_TempChange_Units.Text += rateUnitText;
            label_DE_UpliftRate_Units.Text += rateUnitText;
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
        #endregion
    }
}
