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
            // Get data from the dialog box
            Property ehminAzi = presentationBox_DE_EhminAzi.Tag as Property;
            Property ehminRate = presentationBox_DE_EhminRate.Tag as Property;
            Property ehmaxRate = presentationBox_DE_EhmaxRate.Tag as Property;
            Property OPRate = presentationBox_DE_OPRate.Tag as Property;
            Property tempChange = presentationBox_DE_TempChange.Tag as Property;
            Property upliftRate = presentationBox_DE_UpliftRate.Tag as Property;
            double duration = GetDoubleFromTextBox(unitTextBox_DE_DeformationDuration);
            int timeUnits = comboBox_DE_TimeUnits.SelectedIndex;
            double ehminAzi_default = GetDoubleFromTextBox(unitTextBox_DE_EhminAzi_default);
            double ehminRate_default = GetDoubleFromTextBox(unitTextBox_DE_EhminRate_default);
            double ehmaxRate_default = GetDoubleFromTextBox(unitTextBox_DE_EhmaxRate_default);
            double OPRate_default = GetDoubleFromTextBox(unitTextBox_DE_OPRate_default);
            double tempChange_default = GetDoubleFromTextBox(unitTextBox_DE_TempChange_default);
            double upliftRate_default = GetDoubleFromTextBox(unitTextBox_DE_UpliftRate_default);
            double stressArchingFactor = GetDoubleFromTextBox(unitTextBox_DE_StressArchingFactor);

            // Create a name based on deformation episode duration and specified load
            string deformationEpisodeName = string.Format("Deformation episode {0}: ", deformationEpisodeIndex);
            string timeUnitText = string.Format("{0}", (DFMGenerator_SharedCode.TimeUnits)timeUnits);
            if (duration >= 0)
                deformationEpisodeName += string.Format(" Duration {0}{1}", duration, timeUnitText);
            if (ehminRate!=null)
            {
                deformationEpisodeName += string.Format(" Strain {0}", ehminRate.Name);
                if (ehminAzi != null)
                    deformationEpisodeName += string.Format(" azimuth {0}", ehminAzi.Name);
                else
                    deformationEpisodeName += string.Format(" azimuth {0}deg", Math.Round(ehminAzi_default * (180 / Math.PI)));
            }
            else if (ehminRate_default != 0)
            {
                deformationEpisodeName += string.Format(" Strain {0}/{1}", ehminRate_default, timeUnitText);
                if (ehminAzi != null)
                    deformationEpisodeName += string.Format(" azimuth {0}", ehminAzi.Name);
                else
                    deformationEpisodeName += string.Format(" azimuth {0}deg", Math.Round(ehminAzi_default * (180 / Math.PI)));
            }
            if (OPRate != null)
                deformationEpisodeName += string.Format(" Overpressure {0}", OPRate.Name);
            else if (OPRate_default != 0)
                deformationEpisodeName += string.Format(" Overpressure {0}Pa/{1}", Math.Round(OPRate_default), timeUnitText);
            if (tempChange != null)
                deformationEpisodeName += string.Format(" Temperature {0}", tempChange.Name);
            else if (tempChange_default != 0)
                deformationEpisodeName += string.Format(" Temperature {0}degK/{1}", Math.Round(tempChange_default), timeUnitText);
            if (upliftRate != null)
                deformationEpisodeName += string.Format(" Uplift {0}", upliftRate.Name);
            else if (upliftRate_default != 0)
                deformationEpisodeName += string.Format(" Uplift {0}m/{1}", Math.Round(upliftRate_default), timeUnitText);

            // Write data to the argument package
            args.DeformationEpisode(deformationEpisodeName, deformationEpisodeIndex);
            args.DeformationEpisodeDuration(duration, deformationEpisodeIndex);
            args.DeformationEpisodeTimeUnits(timeUnits, deformationEpisodeIndex);
            args.EhminAzi(ehminAzi, deformationEpisodeIndex);
            args.EhminRate(ehminRate, deformationEpisodeIndex);
            args.EhmaxRate(ehmaxRate, deformationEpisodeIndex);
            args.AppliedOverpressureRate(OPRate, deformationEpisodeIndex);
            args.AppliedTemperatureChange(tempChange, deformationEpisodeIndex);
            args.AppliedUpliftRate(upliftRate, deformationEpisodeIndex);
            args.EhminAzi_default(ehminAzi_default, deformationEpisodeIndex);
            args.EhminRate_default(ehminRate_default, deformationEpisodeIndex);
            args.EhmaxRate_default(ehmaxRate_default, deformationEpisodeIndex);
            args.AppliedOverpressureRate_default(OPRate_default, deformationEpisodeIndex);
            args.AppliedTemperatureChange_default(tempChange_default, deformationEpisodeIndex);
            args.AppliedUpliftRate_default(upliftRate_default, deformationEpisodeIndex);
            args.StressArchingFactor(stressArchingFactor, deformationEpisodeIndex);

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
