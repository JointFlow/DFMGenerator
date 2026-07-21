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
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using Slb.Ocean.Petrel.UI.Controls;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Shapes;
using Slb.Ocean.Petrel.DomainObject.Simulation;

namespace DFMGenerator_Ocean
{
    public partial class ExtractFractureTraces : Form
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
        /// Initializes a new instance of the <see cref="ExtractFractureTraces"/> class 
        /// </summary>
        /// <param name="args">the arguments</param>
        /// <param name="callingDialog">Reference to the dialog box that called this</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        internal ExtractFractureTraces(DFMGeneratorWorkstep.Arguments args, DFMGeneratorUI callingDialog, WorkflowContext context)
        {
            InitializeComponent();

            this.args = args;
            this.context = context;
            this.callingDialog = callingDialog;
            updateUIFromArgs();

            this.btn_EFT_OK.Image = PetrelImages.OK;
            this.btn_EFT_Cancel.Image = PetrelImages.Cancel;

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
            UpdateDFNPresentationBox(args.Argument_EFT_DFNToExtract, presentationBox_EFT_DFNToExtract);
            UpdateTextBox(args.Argument_EFT_Depth, unitTextBox_EFT_Depth, PetrelProject.WellKnownTemplates.GeometricalGroup.MeasuredDepth, label_EFT_Depth_Units);
        }

        private void updateArgsFromUI()
        {
            // Write data to the argument package
            args.Argument_EFT_DFNToExtract = presentationBox_EFT_DFNToExtract.Tag as FractureNetwork;
            args.Argument_EFT_Depth = GetDoubleFromTextBox(unitTextBox_EFT_Depth);

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

        private void UpdateDFNPresentationBox(FractureNetwork fnet, PresentationBox pBox)
        {
            if (fnet != FractureNetwork.NullObject)
            {
                INameInfoFactory fnetNIF = CoreSystem.GetService<INameInfoFactory>(fnet);
                if (fnetNIF != null)
                {
                    NameInfo fnetName = fnetNIF.GetNameInfo(fnet);
                    pBox.Text = fnetName.Name;
                }
                else
                {
                    pBox.Text = fnet.Name;
                }
                IImageInfoFactory fnetImgIF = CoreSystem.GetService<IImageInfoFactory>(fnet);
                if (fnetImgIF != null)
                {
                    ImageInfo fnetImage = fnetImgIF.GetImageInfo(fnet);
                    pBox.Image = fnetImage.GetDisplayImage(new ImageInfoContext());
                }
                else
                {
                    pBox.Image = PetrelImages.FaultPatches;
                }
            }
            else
            {
                pBox.Text = "";
                pBox.Image = null;
            }
            pBox.Tag = fnet;
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

        private void btn_EFT_OK_Click(object sender, EventArgs e)
        {
            updateArgsFromUI();
            ExtractFractureTraces_Implementation();
        }

        private void btn_EFT_Cancel_Click(object sender, EventArgs e)
        {
            this.FindForm().Close();
        }

        private void dropTarget_EFT_DFNToExtract_DragDrop(object sender, DragEventArgs e)
        {
            FractureNetwork droppedDFN = e.Data.GetData(typeof(object)) as FractureNetwork;
            UpdateDFNPresentationBox(droppedDFN, presentationBox_EFT_DFNToExtract);
        }

        private void presentationBox_EFT_DFNToExtract_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                UpdateDFNPresentationBox(FractureNetwork.NullObject, presentationBox_EFT_DFNToExtract);
                e.Handled = true;
            }
        }

        private void ExtractFractureTraces_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Remove the event handlers when the form is closed
            context.ArgumentPackageChanged -= context_ArgumentPackageChanged;

            // Remove the Extract Fracture Traces UI from the list in the calling dialog
            callingDialog.RemoveExtractFractureTraces();
        }
        #endregion

        // Implementation
        private void ExtractFractureTraces_Implementation()
        {
            if ((args.Argument_EFT_DFNToExtract == FractureNetwork.NullObject) || double.IsNaN(args.Argument_EFT_Depth))
                return;

            FractureNetwork DFNToExtract = args.Argument_EFT_DFNToExtract;
            double planeZ = -args.Argument_EFT_Depth;
            string DFNName = DFNToExtract.Name;

            // Create a new collection for the polyline output
            Collection CentrelineCollection = Collection.NullObject;
            using (ITransaction transactionCreateCentrelineCollection = DataManager.NewTransaction())
            {
                // Get handle to project and lock database
                Project project = PetrelProject.PrimaryProject;
                transactionCreateCentrelineCollection.Lock(project);

                // Create a new collection for the polyline set
                CentrelineCollection = project.CreateCollection(DFNName + "_Centrelines");

                // Write the input parameters for the model run to the collection comments string
                string outputStageParams = string.Format("Model name: {0}\n\n", DFNName);

                // Commit the changes to the Petrel database
                transactionCreateCentrelineCollection.Commit();
            }

            // Create a stage-specific label for the output
            string outputLabel = DFNName + "_Centrelines";

            using (ITransaction transactionCreateCentrelines = DataManager.NewTransaction())
            {
                // Lock database
                transactionCreateCentrelines.Lock(CentrelineCollection);

                // Create a new polyline set and set to the depth domain
                PolylineSet Centrelines = CentrelineCollection.CreatePolylineSet("Fracture_Centrelines" + outputLabel);
                Centrelines.Domain = Domain.ELEVATION_DEPTH;

                // Add creation event to the polyline set object history
                HistoryEntry centrelinesCreationEvent = new HistoryEntry("Create dynamic DFN", "", PetrelSystem.VersionInfo.ToString());
                IHistoryInfoEditor centrelinesHistoryInfoEditor = HistoryService.GetHistoryInfoEditor(Centrelines);
                centrelinesHistoryInfoEditor.AddHistoryEntry(centrelinesCreationEvent);

                // Create a list of Petrel polylines
                List<Polyline3> pline_list = new List<Polyline3>();

                // Loop through all the farcture patches in the DFN
                foreach (FracturePatch patch in DFNToExtract.FracturePatches)
                {
                    IIndexedTriangleMesh patchMesh = patch.IndexedTriangleMesh;
                    List<Point3> vertices = new List<Point3>();
                    foreach (Point3 point in patchMesh.Vertices)
                        vertices.Add(point);

                    foreach (IndexedTriangle triangle in patchMesh.Triangles)
                    {
                        if (triangle.Vertex1 >= vertices.Count)
                            continue;
                        if (triangle.Vertex2 >= vertices.Count)
                            continue;
                        if (triangle.Vertex3 >= vertices.Count)
                            continue;
                        Point3[] triangleCorners = new Point3[4];
                        triangleCorners[0] = vertices[triangle.Vertex1];
                        triangleCorners[1] = vertices[triangle.Vertex2];
                        triangleCorners[2] = vertices[triangle.Vertex3];
                        triangleCorners[3] = vertices[triangle.Vertex1];

                        // Find the crossing points
                        List<Point3> crossings = new List<Point3>();
                        for (int edgeNo = 0; edgeNo < 3; edgeNo++)
                        {
                            // Check if the triangle edge intersects the horizontal plane
                            double z1 = triangleCorners[edgeNo].Z;
                            double z2 = triangleCorners[edgeNo + 1].Z;
                            if (((z1 < planeZ) && (z2 < planeZ)) || ((z1 > planeZ) && (z2 > planeZ)))
                                continue;
                            if (z1 == z2)
                                continue;

                            // If so calculate the coordinates of the intersection
                            double intersectionZRatio = (planeZ - z1) / (z2 - z1);
                            double x1 = triangleCorners[edgeNo].X;
                            double x2 = triangleCorners[edgeNo + 1].X;
                            double intersectionX = x1 + (intersectionZRatio * (x2 - x1));
                            double y1 = triangleCorners[edgeNo].Y;
                            double y2 = triangleCorners[edgeNo + 1].Y;
                            double intersectionY = y1 + (intersectionZRatio * (y2 - y1));

                            // Create a new point and add it to the list
                            crossings.Add(new Point3(intersectionX, intersectionY, planeZ));
                        }

                        // If there are two crossing points in the triangle, create a new line segment and add it to the list
                        if (crossings.Count > 1)
                        {
                            try
                            {
                                List<Point3> newpline = new List<Point3>();
                                foreach (Point3 point in crossings)
                                    newpline.Add(point);
                                pline_list.Add(new Polyline3(newpline));
                            }
                            catch (Exception e)
                            {
                                string errorMessage = string.Format("Exception thrown when writing centreline:");
                                foreach (Point3 point in crossings)
                                    errorMessage = errorMessage + string.Format(" ({0},{1},{2})", point.X, point.Y, point.Z);
                                PetrelLogger.InfoOutputWindow(errorMessage);
                                PetrelLogger.InfoOutputWindow(e.Message);
                                PetrelLogger.InfoOutputWindow(e.StackTrace);
                            }
                        }
                    }
                }

                // Add the polyline list to the polyline set
                Centrelines.Polylines = pline_list;

                // Commit the changes to the Petrel database
                transactionCreateCentrelines.Commit();
            }

        }
    }
}
