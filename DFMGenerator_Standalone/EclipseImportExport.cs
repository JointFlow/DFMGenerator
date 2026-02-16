using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DFMGenerator_SharedCode;

namespace DFMGenerator_Standalone
{
    /// <summary>
    /// Class to read input data to and write output data from a FractureGrid object, in Eclipse-compatible ASCII file formats 
    /// </summary>
    class EclipseImportExport
    {
        // References to external objects
        /// <summary>
        /// Reference to the grid object to read to or write from
        /// </summary>
        private FractureGrid gd { get; set; }

        // Helper functions and properties
        /// <summary>
        /// Null value for the output files
        /// </summary>
        private double NullValue { get; set; }

        // Functions to write output data
        /// <summary>
        /// Write grid geometry and implicit output data to a GRDECL format file or files
        /// Data will be written for each specified intermediate stage as well as the final stage
        /// </summary>
        /// <param name="ModelName">Name of the model to output - will be included in the filenames</param>
        /// <param name="progressReporter">Reference to progress reporter implementing the IProgressReporterWrapper interface</param>
        /// <param name="WritePropertiesToSeparateFiles">Flag to write properties to separate GRDECL files - if true separate files will be created for each group of implicit properties</param>
        /// <param name="OutputFractureSetData">Flag to output implicit data for each fracture set and dipset</param>
        /// <param name="OutputFractureConnectivityAnisotropy">Flag to output fracture connectivity and anisotropy data, for each fracture set (if selected) and for the fracture network as a whole</param>
        /// <param name="OutputFractureReactivationPotential">Flag to output reactivation potential and slip tendency for each fracture set</param>
        /// <param name="OutputFracturePorosity">Flag to output P32 density and porosity for the fracture network as a whole</param>
        /// <param name="OutputFracturePermeabilityTensor">Flag to output the fracture permeability tensor</param>
        /// <param name="OutputBulkRockElasticTensors">Flag to output the bulk rock stiffness and compressibility tensors, including the effects of fractures (final stage only)</param>
        /// <param name="PopulateEmptyGridblocks">Flag to write implicit fracture data for gridblocks with no fractures; if false, null values will be written to implicit fracture properties in gridblocks with no fractures</param>
        public void WriteGRDECL(string ModelName, IProgressReporterWrapper progressReporter, bool WritePropertiesToSeparateFiles, bool OutputFractureSetData, bool OutputFractureConnectivityAnisotropy, bool OutputFractureReactivationPotential, bool OutputFracturePorosity, bool OutputFracturePermeabilityTensor, bool OutputBulkRockElasticTensors, bool PopulateEmptyGridblocks)
        {
            // Get the number of rows, columns and gridblocks
            int NoRows = gd.NoRows();
            int NoCols = gd.NoCols();
            int NoActiveGridblocks = gd.NoGridblocks();
            // Get a representative gridblock, and use it to count the number of fracture sets and dipsets
            int NoFractureSets, NoDipSets;
            GridblockConfiguration representative_gbc = gd.GetRepresentativeGridblock(out NoFractureSets, out NoDipSets);
            if (representative_gbc is null)
            {
                progressReporter.OutputMessage("There are no gridblocks containing any data");
                progressReporter.OutputMessage("No output files will be written");
                return;
            }
            List<string> DipSetLabels = new List<string>();
            if (NoFractureSets > 0)
                DipSetLabels = representative_gbc.FractureSets[0].DipSetLabels();

            // Get control data from DFNControl object
            // Folder to write output files in
            string filepath = gd.DFNControl.FolderPath;
            // Number of intermediate stages to output and flag to control their separation
            int NoIntermediateOutputs = gd.DFNControl.NumberOfIntermediateOutputs;
            if (NoIntermediateOutputs < 0) NoIntermediateOutputs = 0;
            IntermediateOutputInterval IntermediateOutputIntervalControl = gd.DFNControl.SeparateIntermediateOutputsBy;
            // Algorithms used to calculate fracture aperture and permeability
            FractureApertureType FractureApertureControl = representative_gbc.PropControl.FractureApertureControl;
            PermeabilityCalculationAlgorithm PermeabilityAlgorithm = representative_gbc.PropControl.PermeabilityAlgorithm;
            // Calculate permeability tensor for layer-bound fractures only
            FractureType FractureTypesInPermeabilityTensor = FractureType.LayerBoundFractures;
            // Flag to link fracture segments terminating due to stress shadow interaction
            bool LinkStressShadows = gd.DFNControl.LinkFracturesInStressShadow;
            // Get the time units and time units modifier for the output labels
            TimeUnits timeUnits = gd.DFNControl.timeUnits;
            string ProjectTimeUnits;
            switch (timeUnits)
            {
                case TimeUnits.second:
                    ProjectTimeUnits = "seconds";
                    break;
                case TimeUnits.year:
                    ProjectTimeUnits = "years";
                    break;
                case TimeUnits.ma:
                    ProjectTimeUnits = "ma";
                    break;
                default:
                    ProjectTimeUnits = "seconds";
                    break;
            }
            double timeUnits_Modifier = gd.DFNControl.getTimeUnitsModifier();

            // If the calculation has already been cancelled, do not write any output data
            if (!progressReporter.abortCalculation())
            {
                // Write implicit fracture property data to an Eclipse GRDECL file or files
                progressReporter.OutputMessage("Write implicit data to GRDECL file(s)");

                // Set the output file extension
                string fileExtension = ".GRDECL";

                // Calculate the number of stages, the number of fracture sets and the total number of calculation elements
                int NoStages = NoIntermediateOutputs + 1;
                int NoCalculationElementsCompleted = 0;
                int NoElements = NoActiveGridblocks * ((OutputFractureSetData ? (NoFractureSets * NoDipSets) : 0) + (OutputFractureConnectivityAnisotropy ? (OutputFractureSetData ? (NoFractureSets * NoDipSets) : 0) + 1 : 0) + (OutputFractureReactivationPotential ? (NoFractureSets * NoDipSets) : 0) + (OutputFracturePorosity ? 1 : 0) + (OutputFracturePermeabilityTensor ? 1 : 0));
                NoElements *= NoStages;
                // Bulk rock elastic tensors are only output for the final stage
                if (OutputBulkRockElasticTensors)
                    NoElements += NoActiveGridblocks;
                progressReporter.SetNumberOfElements(NoElements);

                // Get a list of end times for each gridblock - this is used to calculate the end times for the intermediate stages
                List<double> timestepEndtimes = gd.GetTimestepEndtimeList();
                int NoGridblockTimesteps = timestepEndtimes.Count;
                double endTime = (NoGridblockTimesteps > 0 ? timestepEndtimes[NoGridblockTimesteps - 1] : 0);

                // Loop through each stage in the fracture growth
                for (int stageNumber = 1; stageNumber <= NoStages; stageNumber++)
                {
                    // Set a flag to determine whether this is the final stage - if so we can read the output data directly from the final state of the gridblock objects
                    bool finalStage = (stageNumber == NoStages);

                    // Get the endtime for the current stage
                    double stageEndTime = 0;
                    string stageNameOverride = null;
                    switch (IntermediateOutputIntervalControl)
                    {
                        case IntermediateOutputInterval.SpecifiedTime:
                            double nextListValue = gd.DFNControl.GetIntermediateOutputTime(stageNumber - 1); // List of intermediate outputs is zero-based
                            stageEndTime = !double.IsNaN(nextListValue) ? nextListValue : endTime; // If the next list value is NaN (i.e. we have reached the end of the list), used the end time instead
                            break;
                        case IntermediateOutputInterval.EqualTime:
                            stageEndTime = (stageNumber * endTime) / NoStages;
                            break;
                        case IntermediateOutputInterval.EqualArea:
                            int gridblockTimestepNo = ((stageNumber * NoGridblockTimesteps) / NoStages);
                            if (gridblockTimestepNo < 1)
                                gridblockTimestepNo = 1;
                            if (gridblockTimestepNo > NoGridblockTimesteps)
                                gridblockTimestepNo = NoGridblockTimesteps;
                            stageEndTime = (NoGridblockTimesteps > 0) ? timestepEndtimes[gridblockTimestepNo - 1] : 0;
                            break;
                        default:
                            break;
                    }

                    // Create a stage-specific label and description for the output
                    string outputStageLabel;
                    if ((stageNameOverride is null) || (stageNameOverride.Length == 0))
                        outputStageLabel = (stageNumber == NoStages) ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, (stageEndTime / timeUnits_Modifier).ToString("G3"), ProjectTimeUnits);
                    else
                        outputStageLabel = "_" + stageNameOverride;
                    string outputStageParams = string.Format("Model name: {0}\n", ModelName);
                    outputStageParams += (stageNumber == NoStages) ? "Final stage" : string.Format("Stage {0}", stageNumber);
                    outputStageParams += (stageNameOverride is null) ? "\n" : string.Format(": {0}\n", stageNameOverride);
                    outputStageParams += string.Format("Time {0}{1}\n", (stageEndTime / timeUnits_Modifier), ProjectTimeUnits);
                    outputStageParams += "\n";
                    string metadataSource = "";
                    string outputDataSource = "DFM_Generator";

                    // Create the principal output file - this will contain the header information, grid geometry
                    // It will also contain the property information if WritePropertiesToSeparateFiles is false
                    // Also create a list of all output files - this will make it easier to keep track of them and ensure that all of them are closed at the end
                    string fileNameBase = ModelName + outputStageLabel;
                    string outputFileName = filepath + fileNameBase + fileExtension;
                    List<StreamWriter> outputFiles = new List<StreamWriter>();
                    StreamWriter outputFile = new StreamWriter(outputFileName);
                    outputFiles.Add(outputFile);

                    try
                    {
                        // Write header
                        string headerInfo = "";
                        headerInfo += string.Format("-- Generated [\n");
                        headerInfo += string.Format("--Format      : Eclipse keywords(grid geometry and properties)(ASCII)\n");
                        headerInfo += string.Format("-- Exported by: {0}\n", FractureGrid.VersionNumber);
                        headerInfo += string.Format("-- User name: {0}\n", "Unknown");
                        headerInfo += string.Format("-- Date: {0}\n", System.DateTime.Now);
                        headerInfo += string.Format("-- Project: {0}\n", ModelName);
                        headerInfo += string.Format("-- Grid: \n");
                        headerInfo += string.Format("-- Generated ]\n\n");
                        outputFile.Write(headerInfo);

                        // Grid and unit metadata
                        string gridMetadata = "";
                        gridMetadata += string.Format("PINCH\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  /\n\n");
                        gridMetadata += string.Format("NOECHO\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  /\n\n");
                        gridMetadata += string.Format("MAPUNITS\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  METRES /\n\n");
                        PointXYZ gridOrigin = gd.GetGridOrigin();
                        gridMetadata += string.Format("MAPAXES\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  {0} {1} {2} {3} {4} {5}/\n\n", gridOrigin.X, gridOrigin.Y + 1000, gridOrigin.X, gridOrigin.Y, gridOrigin.X + 1000, gridOrigin.Y);
                        gridMetadata += string.Format("GRIDUNIT\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  METRES /\n\n");
                        gridMetadata += string.Format("DIMENS\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  {0} {1} {2}/\n\n", gd.NoCols(), gd.NoRows(), 1);
                        gridMetadata += string.Format("SPECGRID\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  {0} {1} {2} {3} {4}/\n\n", gd.NoCols(), gd.NoRows(), 1, 1, "F");
                        gridMetadata += string.Format("COORDSYS\t-- Generated: {0}\n", metadataSource);
                        gridMetadata += string.Format("  {0} {1}/\n\n", 1, 5);
                        outputFile.Write(gridMetadata);

                        // Grid geometry
                        // Define pillars, z coordinates and active cells
                        string pillars = string.Format("COORD\t-- Generated : {0}\n", metadataSource);
                        string zcoords = string.Format("ZCORN\t-- Generated : {0}\n", metadataSource);
                        string topzcoords = "";
                        string bottomzcoords = "";
                        string actnum = string.Format("ACTNUM\t-- Generated : {0}\n  ", metadataSource);
                        PointXYZ TopPillar = new PointXYZ(0, 0, 0);
                        PointXYZ BottomPillar = new PointXYZ(0, 0, 0);
                        for (int RowNo = NoRows - 1; RowNo >= 0; RowNo--)
                        {
                            string row_topzcoords = "";
                            string row_bottomzcoords = "";

                            // Add the columns on the northern face of the gridblock
                            for (int ColNo = 0; ColNo < NoCols; ColNo++)
                            {
                                GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);
                                // Add the pillar and cornerpoints at the NW corner of the gridblock
                                {
                                    if (gbc is null)
                                    {
                                        actnum += "0 ";
                                    }
                                    else
                                    {
                                        TopPillar = gbc.NWtop;
                                        BottomPillar = gbc.NWbottom;
                                        actnum += "1 ";
                                    }
                                    pillars += string.Format("  {0} {1} {2} {3} {4} {5}\n", TopPillar.X, TopPillar.Y, TopPillar.Depth, BottomPillar.X, BottomPillar.Y, BottomPillar.Depth);
                                    if (ColNo == 0)
                                    {
                                        row_topzcoords += string.Format("  {0}", TopPillar.Depth);
                                        row_bottomzcoords += string.Format("  {0}", BottomPillar.Depth);
                                    }
                                    else
                                    {
                                        row_topzcoords += string.Format(" {0} {0}", TopPillar.Depth);
                                        row_bottomzcoords += string.Format(" {0} {0}", BottomPillar.Depth);
                                    }
                                }
                                // If this is the last column, add the pillar and cornerpoints at the NE corner of the gridblock
                                if (ColNo == (NoCols - 1))
                                {
                                    if (gbc is null)
                                    {
                                        actnum += "\n  ";
                                    }
                                    else
                                    {
                                        TopPillar = gbc.NEtop;
                                        BottomPillar = gbc.NEbottom;
                                        actnum += "\n  ";
                                    }
                                    pillars += string.Format("  {0} {1} {2} {3} {4} {5}\n", TopPillar.X, TopPillar.Y, TopPillar.Depth, BottomPillar.X, BottomPillar.Y, BottomPillar.Depth);
                                    row_topzcoords += string.Format(" {0}\n", TopPillar.Depth);
                                    row_bottomzcoords += string.Format(" {0}\n", BottomPillar.Depth);
                                }
                            }

                            // Add the z coordinates of the top and bottom of the cells to the z coordinate strings
                            // For all except the first and last rows, we need to double the top and bottom coordinates, first for the southern faces of the northern gridblocks, and then for the northern faces of the southern gridblocks
                            if (RowNo > 0)
                            {
                                row_topzcoords += row_topzcoords;
                                row_bottomzcoords += row_bottomzcoords;
                            }

                            // If this is the last row, add the columns on the southern face of the gridblock
                            if (RowNo == 0)
                                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                {
                                    GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);
                                    // Add the pillar and cornerpoints at the SW corner of the gridblock
                                    {
                                        if (gbc is null)
                                        {
                                            //actnum += "0 ";
                                        }
                                        else
                                        {
                                            TopPillar = gbc.SWtop;
                                            BottomPillar = gbc.SWbottom;
                                            //actnum += "1 ";
                                        }
                                        pillars += string.Format("  {0} {1} {2} {3} {4} {5}\n", TopPillar.X, TopPillar.Y, TopPillar.Depth, BottomPillar.X, BottomPillar.Y, BottomPillar.Depth);
                                        if (ColNo == 0)
                                        {
                                            row_topzcoords += string.Format("  {0}", TopPillar.Depth);
                                            row_bottomzcoords += string.Format("  {0}", BottomPillar.Depth);
                                        }
                                        else
                                        {
                                            row_topzcoords += string.Format(" {0} {0}", TopPillar.Depth);
                                            row_bottomzcoords += string.Format(" {0} {0}", BottomPillar.Depth);
                                        }
                                    }
                                    // If this is the last column, add the pillar and cornerpoints at the SE corner of the gridblock
                                    if (ColNo == (NoCols - 1))
                                    {
                                        if (gbc is null)
                                        {
                                            //actnum += "\n";
                                        }
                                        else
                                        {
                                            TopPillar = gbc.SEtop;
                                            BottomPillar = gbc.SEbottom;
                                            //actnum += "\n  ";
                                        }
                                        pillars += string.Format("  {0} {1} {2} {3} {4} {5}\n", TopPillar.X, TopPillar.Y, TopPillar.Depth, BottomPillar.X, BottomPillar.Y, BottomPillar.Depth);
                                        row_topzcoords += string.Format(" {0}\n", TopPillar.Depth);
                                        row_bottomzcoords += string.Format(" {0}\n", BottomPillar.Depth);
                                    }
                                }

                            // Add the z coordinates for the row to the appropriate strings
                            topzcoords += row_topzcoords;
                            bottomzcoords += row_bottomzcoords;
                        }
                        pillars += "  /\n\n";
                        zcoords += topzcoords + bottomzcoords + "  /\n\n";
                        actnum += "/\n\n";
                        outputFile.Write(pillars);
                        outputFile.Write(zcoords);
                        outputFile.Write(actnum);

                        // Write property data
                        // Loop through each fracture set
                        if (OutputFractureSetData)
                            for (int FractureSetNo = 0; FractureSetNo < NoFractureSets; FractureSetNo++)
                            {
                                // Set a name for the fracture set
                                string FractureSetName = GridblockConfiguration.getFractureSetName(FractureSetNo, NoFractureSets);

                                for (int DipSetNo = 0; DipSetNo < NoDipSets; DipSetNo++)
                                {
                                    // Create a label for the fracture dip set
                                    string dipsetLabel = (DipSetNo < DipSetLabels.Count ? DipSetLabels[DipSetNo] : "");
                                    string FDS_Label = string.Format("{0}_{1}", FractureSetName, dipsetLabel);

                                    // If required, create a new file for the property output
                                    StreamWriter fs_outputFile;
                                    if (WritePropertiesToSeparateFiles)
                                    {
                                        string fs_outputFileName = filepath + fileNameBase + "_" + FDS_Label + fileExtension;
                                        fs_outputFile = new StreamWriter(fs_outputFileName);
                                        outputFiles.Add(fs_outputFile);
                                        fs_outputFile.Write(headerInfo);
                                        fs_outputFile.WriteLine(string.Format("-- Implicit fracture data for fracture set {0}", FDS_Label));
                                        fs_outputFile.WriteLine(string.Format("-- Generated : {0}", outputDataSource));
                                        fs_outputFile.WriteLine();
                                    }
                                    else
                                    {
                                        fs_outputFile = outputFile;
                                    }

                                    // Write fracture density and length data
                                    {
                                        // Create output strings for each property
                                        string MF_P30_tot = FDS_Label + string.Format("_Layer_bound_fracture_P30\t-- Generated : {0}\n  ", outputDataSource);
                                        string MF_P32_tot = FDS_Label + string.Format("_Layer_bound_fracture_P32\t-- Generated : {0}\n  ", outputDataSource);
                                        string uF_P32_tot = FDS_Label + string.Format("_Microfracture_P32\t-- Generated : {0}\n  ", outputDataSource);
                                        string MF_MeanLength = FDS_Label + string.Format("_Mean_fracture_length\t-- Generated : {0}\n  ", outputDataSource);

                                        // Loop through all rows and columns in the grid
                                        // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                        // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                                        // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                                        for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                            for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressReporter.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                                // Get data from GridblockConfiguration object
                                                // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                                // In this case we will set all data to null value
                                                double cell_MF_P30_tot, cell_MF_P32_tot, cell_uF_P32_tot, cell_MF_MeanLength;
                                                if (gbc is null)
                                                {
                                                    cell_MF_P30_tot = NullValue;
                                                    cell_MF_P32_tot = NullValue;
                                                    cell_uF_P32_tot = NullValue;
                                                    cell_MF_MeanLength = NullValue;
                                                }
                                                else if ((FractureSetNo >= gbc.NoFractureSets) || (DipSetNo >= gbc.FractureSets[FractureSetNo].FractureDipSets.Count))
                                                {
                                                    cell_MF_P30_tot = NullValue;
                                                    cell_MF_P32_tot = NullValue;
                                                    cell_uF_P32_tot = NullValue;
                                                    cell_MF_MeanLength = NullValue;
                                                }
                                                else
                                                {
                                                    FractureDipSet fds = gbc.FractureSets[FractureSetNo].FractureDipSets[DipSetNo];

                                                    if (finalStage)
                                                    {
                                                        cell_MF_P30_tot = (fds.a_MFP30_total() + fds.sII_MFP30_total() + fds.sIJ_MFP30_total()) / 2;
                                                        cell_MF_P32_tot = fds.a_MFP32_total() + fds.s_MFP32_total();
                                                        cell_uF_P32_tot = fds.a_uFP32_total() + fds.s_uFP32_total();
                                                        cell_MF_MeanLength = fds.Mean_MF_HalfLength() * 2;
                                                    }
                                                    else
                                                    {
                                                        int TSNo = gbc.getTimestepIndex(stageEndTime);
                                                        cell_MF_P30_tot = fds.getTotalMFP30(TSNo);
                                                        cell_MF_P32_tot = fds.getTotalMFP32(TSNo);
                                                        cell_uF_P32_tot = fds.getTotaluFP32(TSNo);
                                                        double MFP30_Thickness = cell_MF_P30_tot * gbc.ThicknessAtDeformation;
                                                        cell_MF_MeanLength = (MFP30_Thickness > 0 ? 2 * (cell_MF_P32_tot / MFP30_Thickness) : 0);
                                                    }
                                                    if (!PopulateEmptyGridblocks && (cell_MF_P32_tot <= 0))
                                                    {
                                                        cell_MF_P30_tot = NullValue;
                                                        cell_MF_P32_tot = NullValue;
                                                        cell_MF_MeanLength = NullValue;
                                                    }
                                                }

                                                // Update the output string for each property
                                                MF_P30_tot += string.Format("{0} ", cell_MF_P30_tot);
                                                MF_P32_tot += string.Format("{0} ", cell_MF_P32_tot);
                                                uF_P32_tot += string.Format("{0} ", cell_uF_P32_tot);
                                                MF_MeanLength += string.Format("{0} ", cell_MF_MeanLength);

                                                // If this is the final column in the row, add a line return to each of the output strings
                                                // This will ensure each row of data starts on a new line
                                                if (ColNo == (NoCols - 1))
                                                {
                                                    MF_P30_tot += "\n  ";
                                                    MF_P32_tot += "\n  ";
                                                    uF_P32_tot += "\n  ";
                                                    MF_MeanLength += "\n  ";
                                                }

                                                // Update progress bar
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the grid

                                        // Add the end to each output string then write them all to the output file
                                        MF_P30_tot += "/\n\n";
                                        MF_P32_tot += "/\n\n";
                                        uF_P32_tot += "/\n\n";
                                        MF_MeanLength += "/\n\n";
                                        fs_outputFile.Write(MF_P30_tot);
                                        fs_outputFile.Write(MF_P32_tot);
                                        fs_outputFile.Write(uF_P32_tot);
                                        fs_outputFile.Write(MF_MeanLength);

                                    } // End write fracture density and length data

                                    // If required, write fracture connectivity data
                                    if (OutputFractureConnectivityAnisotropy)
                                    {
                                        // Create output strings for each property
                                        string MF_UnconnectedTipRatio = FDS_Label + string.Format("_Unconnected_fracture_tip_ratio\t-- Generated : {0}\n  ", outputDataSource);
                                        string MF_RelayTipRatio = FDS_Label + string.Format("_Relay_zone_fracture_tip_ratio\t-- Generated : {0}\n  ", outputDataSource);
                                        string MF_IntersectingTipRatio = FDS_Label + string.Format("_Intersecting_fracture_tip_ratio\t-- Generated : {0}\n  ", outputDataSource);
                                        string ConnectionsPerMF = FDS_Label + string.Format("_Connections_per_fracture\t-- Generated : {0}\n  ", outputDataSource);
                                        string EndDeformationTime = FDS_Label + string.Format("_Time_of_end_macrofracture_growth\t-- Generated : {0}\n  ", outputDataSource);

                                        // Loop through all rows and columns in the grid
                                        // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                        // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                                        // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                                        for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                            for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressReporter.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                                // Get data from GridblockConfiguration object
                                                // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                                // In this case we will set all data to null value
                                                double UnconnectedTipRatio, RelayTipRatio, IntersectingTipRatio, NodesPerMF, EndTime;
                                                if (gbc is null)
                                                {
                                                    UnconnectedTipRatio = NullValue;
                                                    RelayTipRatio = NullValue;
                                                    IntersectingTipRatio = NullValue;
                                                    NodesPerMF = NullValue;
                                                    EndTime = NullValue;
                                                }
                                                else if ((FractureSetNo >= gbc.NoFractureSets) || (DipSetNo >= gbc.FractureSets[FractureSetNo].FractureDipSets.Count))
                                                {
                                                    UnconnectedTipRatio = NullValue;
                                                    RelayTipRatio = NullValue;
                                                    IntersectingTipRatio = NullValue;
                                                    NodesPerMF = NullValue;
                                                    EndTime = NullValue;
                                                }
                                                else
                                                {
                                                    FractureDipSet fds = gbc.FractureSets[FractureSetNo].FractureDipSets[DipSetNo];

                                                    if (finalStage)
                                                    {
                                                        UnconnectedTipRatio = fds.UnconnectedTipRatio(!PopulateEmptyGridblocks);
                                                        RelayTipRatio = fds.RelayTipRatio(!PopulateEmptyGridblocks);
                                                        IntersectingTipRatio = fds.IntersectingTipRatio(!PopulateEmptyGridblocks);
                                                        NodesPerMF = gbc.ConnectionsPerMacrofracture(FractureSetNo, DipSetNo, !PopulateEmptyGridblocks);
                                                        EndTime = fds.getFinalActiveTime(!PopulateEmptyGridblocks);
                                                        if (double.IsNaN(UnconnectedTipRatio))
                                                            UnconnectedTipRatio = NullValue;
                                                        if (double.IsNaN(RelayTipRatio))
                                                            RelayTipRatio = NullValue;
                                                        if (double.IsNaN(IntersectingTipRatio))
                                                            IntersectingTipRatio = NullValue;
                                                        if (double.IsNaN(NodesPerMF))
                                                            NodesPerMF = NullValue;
                                                        if (double.IsNaN(EndTime))
                                                            EndTime = NullValue;
                                                    }
                                                    else
                                                    {
                                                        int TSNo = gbc.getTimestepIndex(stageEndTime);
                                                        double INodes = fds.getActiveMFP30(TSNo);
                                                        double RNodes = fds.getStaticRelayMFP30(TSNo);
                                                        double YNodes = fds.getStaticIntersectMFP30(TSNo);
                                                        double TotalNodes = INodes + RNodes + YNodes;
                                                        double NoConnections = (LinkStressShadows ? RNodes : 0) + YNodes + fds.getTerminatingFractureDensity(TSNo);
                                                        UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : (PopulateEmptyGridblocks ? 1 : NullValue));
                                                        RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : (PopulateEmptyGridblocks ? 0 : NullValue));
                                                        IntersectingTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : (PopulateEmptyGridblocks ? 0 : NullValue));
                                                        NodesPerMF = (TotalNodes > 0 ? NoConnections / TotalNodes : (PopulateEmptyGridblocks ? 0 : NullValue));
                                                        EndTime = stageEndTime;
                                                    }
                                                }

                                                // Update the output string for each property
                                                MF_UnconnectedTipRatio += string.Format("{0} ", UnconnectedTipRatio);
                                                MF_RelayTipRatio += string.Format("{0} ", RelayTipRatio);
                                                MF_IntersectingTipRatio += string.Format("{0} ", IntersectingTipRatio);
                                                ConnectionsPerMF += string.Format("{0} ", NodesPerMF);
                                                EndDeformationTime += string.Format("{0} ", EndTime);

                                                // If this is the final column in the row, add a line return to each of the output strings
                                                // This will ensure each row of data starts on a new line
                                                if (ColNo == (NoCols - 1))
                                                {
                                                    MF_UnconnectedTipRatio += "\n  ";
                                                    MF_RelayTipRatio += "\n  ";
                                                    MF_IntersectingTipRatio += "\n  ";
                                                    ConnectionsPerMF += "\n  ";
                                                    EndDeformationTime += "\n  ";
                                                }

                                                // Update progress bar
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the grid

                                        // Add the end to each output string then write them all to the output file
                                        MF_UnconnectedTipRatio += "/\n\n";
                                        MF_RelayTipRatio += "/\n\n";
                                        MF_IntersectingTipRatio += "/\n\n";
                                        ConnectionsPerMF += "/\n\n";
                                        EndDeformationTime += "/\n\n";
                                        fs_outputFile.Write(MF_UnconnectedTipRatio);
                                        fs_outputFile.Write(MF_RelayTipRatio);
                                        fs_outputFile.Write(MF_IntersectingTipRatio);
                                        fs_outputFile.Write(ConnectionsPerMF);
                                        fs_outputFile.Write(EndDeformationTime);

                                    } // End write fracture connectivity data

                                    // If required, write fracture reactivity data
                                    if (OutputFractureReactivationPotential)
                                    {
                                        // Create output strings for each property
                                        string FDS_ReactivationPotential = FDS_Label + string.Format("_Reactivation_Potential\t-- Generated : {0}\n  ", outputDataSource);
                                        string FDS_SlipTendency = FDS_Label + string.Format("_Slip_Tendency\t-- Generated : {0}\n  ", outputDataSource);

                                        // Loop through all rows and columns in the grid
                                        // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                                        // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                                        // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                                        for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                            for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                            {
                                                // Check if calculation has been aborted
                                                if (progressReporter.abortCalculation())
                                                {
                                                    // Clean up any resources or data
                                                    break;
                                                }

                                                // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                                GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                                // Get data from GridblockConfiguration object
                                                // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                                // In this case we will set all data to null value
                                                double ReactivationPotential, SlipTendency;
                                                if (gbc is null)
                                                {
                                                    ReactivationPotential = NullValue;
                                                    SlipTendency = NullValue;
                                                }
                                                else if ((FractureSetNo >= gbc.NoFractureSets) || (DipSetNo >= gbc.FractureSets[FractureSetNo].FractureDipSets.Count))
                                                {
                                                    ReactivationPotential = NullValue;
                                                    SlipTendency = NullValue;
                                                }
                                                else
                                                {
                                                    FractureDipSet fds = gbc.FractureSets[FractureSetNo].FractureDipSets[DipSetNo];

                                                    ReactivationPotential = fds.PresentDayReactivationPotential;
                                                    SlipTendency = fds.PresentDaySlipTendency;
                                                }

                                                // Update the output string for each property
                                                FDS_ReactivationPotential += string.Format("{0} ", ReactivationPotential);
                                                FDS_SlipTendency += string.Format("{0} ", SlipTendency);

                                                // If this is the final column in the row, add a line return to each of the output strings
                                                // This will ensure each row of data starts on a new line
                                                if (ColNo == (NoCols - 1))
                                                {
                                                    FDS_ReactivationPotential += "\n  ";
                                                    FDS_SlipTendency += "\n  ";
                                                }

                                                // Update progress bar
                                                progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                            } // End loop through all columns and rows in the grid

                                        // Add the end to each output string then write them all to the output file
                                        FDS_ReactivationPotential += "/\n\n";
                                        FDS_SlipTendency += "/\n\n";
                                        fs_outputFile.Write(FDS_ReactivationPotential);
                                        fs_outputFile.Write(FDS_SlipTendency);

                                    } // End write fracture reactivity data to Petrel grid

                                } // End loop through fracture dip sets
                            } // End loop through fracture sets

                        // If required, write fracture anisotropy data for the whole fracture network
                        if (OutputFractureConnectivityAnisotropy)
                        {
                            // If required, create a new file for the property output
                            StreamWriter aniso_outputFile;
                            if (WritePropertiesToSeparateFiles)
                            {
                                string aniso_outputFileName = filepath + fileNameBase + "_AnisotropyConnectivity" + fileExtension;
                                aniso_outputFile = new StreamWriter(aniso_outputFileName);
                                outputFiles.Add(aniso_outputFile);
                                aniso_outputFile.Write(headerInfo);
                                aniso_outputFile.WriteLine(string.Format("-- Implicit fracture data for anisotropy and connectivity of the whole fracture network"));
                                aniso_outputFile.WriteLine(string.Format("-- Generated : {0}", outputDataSource));
                                aniso_outputFile.WriteLine();
                            }
                            else
                            {
                                aniso_outputFile = outputFile;
                            }

                            // Create output strings for each property
                            string P32_Anisotropy = string.Format("P32_anisotropy\t-- Generated : {0}\n  ", outputDataSource);
                            string P33_Anisotropy;
                            if (OutputFracturePorosity)
                                P33_Anisotropy = string.Format("FracturePorosity_anisotropy\t-- Generated : {0}\n  ", outputDataSource);
                            else
                                P33_Anisotropy = string.Format("P33_anisotropy\t-- Generated : {0}\n  ", outputDataSource);
                            string MF_UnconnectedTipRatio = string.Format("Unconnected_fracture_tip_ratio\t-- Generated : {0}\n  ", outputDataSource);
                            string MF_RelayTipRatio = string.Format("Relay_zone_fracture_tip_ratio\t-- Generated : {0}\n  ", outputDataSource);
                            string MF_IntersectingTipRatio = string.Format("Intersecting_fracture_tip_ratio\t-- Generated : {0}\n  ", outputDataSource);
                            string ConnectionsPerMF = string.Format("Connections_per_fracture\t-- Generated : {0}\n  ", outputDataSource);
                            string EndDeformationTime = string.Format("Time_of_end_macrofracture_growth\t-- Generated : {0}\n  ", outputDataSource);

                            // Loop through all rows and columns in the grid
                            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                            // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                            // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                {
                                    // Check if calculation has been aborted
                                    if (progressReporter.abortCalculation())
                                    {
                                        // Clean up any resources or data
                                        break;
                                    }

                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                    GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                    // Get data from GridblockConfiguration object
                                    // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                    // In this case we will set all data to null value
                                    double P32_anisotropy, P33_anisotropy;
                                    double UnconnectedTipRatio, RelayTipRatio, IntersectingTipRatio, NodesPerMF, EndTime;
                                    if (gbc is null)
                                    {
                                        P32_anisotropy = NullValue;
                                        P33_anisotropy = NullValue;
                                        UnconnectedTipRatio = NullValue;
                                        RelayTipRatio = NullValue;
                                        IntersectingTipRatio = NullValue;
                                        NodesPerMF = NullValue;
                                        EndTime = NullValue;
                                    }
                                    else
                                    {
                                        // Calculate fracture anisotropy and connectivity for the entire fracture network
                                        if (finalStage)
                                        {
                                            // Calculate fracture anisotropy data using the functions in the GridblockConfiguration object
                                            P32_anisotropy = gbc.P32AnisotropyIndex(true, !PopulateEmptyGridblocks);
                                            if (OutputFracturePorosity)
                                                P33_anisotropy = gbc.FracturePorosityAnisotropyIndex(true, !PopulateEmptyGridblocks);
                                            else
                                                P33_anisotropy = gbc.P33AnisotropyIndex(true, !PopulateEmptyGridblocks);

                                            // Calculate fracture connectivity data using the functions in the GridblockConfiguration object
                                            UnconnectedTipRatio = gbc.UnconnectedTipRatio(!PopulateEmptyGridblocks);
                                            RelayTipRatio = gbc.RelayTipRatio(!PopulateEmptyGridblocks);
                                            IntersectingTipRatio = gbc.IntersectingTipRatio(!PopulateEmptyGridblocks);
                                            NodesPerMF = gbc.ConnectionsPerMacrofracture(!PopulateEmptyGridblocks);

                                            // Calculate end deformation time using the function in the GridblockConfiguration object
                                            // This will represent either the end of the deformation episode or the time of macrofracture saturation, whichever is earliest
                                            EndTime = gbc.getFinalActiveTime(!PopulateEmptyGridblocks);

                                            // Set any NaNs to the default null value
                                            if (double.IsNaN(P32_anisotropy))
                                                P32_anisotropy = NullValue;
                                            if (double.IsNaN(P33_anisotropy))
                                                P33_anisotropy = NullValue;
                                            if (double.IsNaN(UnconnectedTipRatio))
                                                UnconnectedTipRatio = NullValue;
                                            if (double.IsNaN(RelayTipRatio))
                                                RelayTipRatio = NullValue;
                                            if (double.IsNaN(IntersectingTipRatio))
                                                IntersectingTipRatio = NullValue;
                                            if (double.IsNaN(NodesPerMF))
                                                NodesPerMF = NullValue;
                                            if (double.IsNaN(EndTime))
                                                EndTime = NullValue;
                                        }
                                        else
                                        {
                                            int TSNo = gbc.getTimestepIndex(stageEndTime);
                                            double undefinedValue = (PopulateEmptyGridblocks ? 0 : NullValue);

                                            // Calculate fracture anisotropy data using the data cached in the FCDList
                                            double Min_P32 = 0;
                                            double Max_P32 = 0;
                                            double Min_P33 = 0;
                                            double Max_P33 = 0;
                                            // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
                                            if (NoFractureSets < 2)
                                            {
                                                Max_P32 = 1;
                                                Max_P33 = 1;
                                            }
                                            else
                                            {
                                                foreach (FractureDipSet fds in gbc.FractureSets[0].FractureDipSets)
                                                {
                                                    Max_P32 += (fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo));
                                                    if (OutputFracturePorosity)
                                                        Max_P33 += (fds.getTotaluFPorosity(TSNo) + fds.getTotalMFPorosity(TSNo));
                                                    else
                                                        Max_P33 += (fds.getTotaluFP33(TSNo) + fds.getTotalMFP33(TSNo));
                                                }
                                                Min_P32 = Max_P32;
                                                Min_P33 = Max_P33;

                                                for (int fs_Index = 1; fs_Index < NoFractureSets; fs_Index++)
                                                {
                                                    double fs_P32 = 0;
                                                    double fs_P33 = 0;
                                                    foreach (FractureDipSet fds in gbc.FractureSets[fs_Index].FractureDipSets)
                                                    {
                                                        fs_P32 += (fds.getTotaluFP32(TSNo) + fds.getTotalMFP32(TSNo));
                                                        if (OutputFracturePorosity)
                                                            fs_P32 += (fds.getTotaluFPorosity(TSNo) + fds.getTotalMFPorosity(TSNo));
                                                        else
                                                            fs_P33 += (fds.getTotaluFP33(TSNo) + fds.getTotalMFP33(TSNo));
                                                    }
                                                    if (fs_P32 > Max_P32)
                                                        Max_P32 = fs_P32;
                                                    if (fs_P32 < Min_P32)
                                                        Min_P32 = fs_P32;
                                                    if (fs_P33 > Max_P33)
                                                        Max_P33 = fs_P33;
                                                    if (fs_P33 < Min_P33)
                                                        Min_P33 = fs_P33;
                                                }
                                            }
                                            double Combined_P32 = Min_P32 + Max_P32;
                                            P32_anisotropy = (Combined_P32 > 0 ? (Max_P32 - Min_P32) / Combined_P32 : (PopulateEmptyGridblocks ? 0 : NullValue));
                                            double Combined_P33 = Min_P33 + Max_P33;
                                            P33_anisotropy = (Combined_P33 > 0 ? (Max_P33 - Min_P33) / Combined_P33 : (PopulateEmptyGridblocks ? 0 : NullValue));

                                            // Calculate fracture connectivity data using the data cached in the FCDList
                                            double INodes = 0;
                                            double RNodes = 0;
                                            double YNodes = 0;
                                            foreach (Gridblock_FractureSet fs in gbc.FractureSets)
                                                foreach (FractureDipSet fds in fs.FractureDipSets)
                                                {
                                                    INodes += fds.getActiveMFP30(TSNo);
                                                    RNodes += fds.getStaticRelayMFP30(TSNo);
                                                    YNodes += fds.getStaticIntersectMFP30(TSNo);
                                                }
                                            double TotalNodes = INodes + RNodes + YNodes;
                                            UnconnectedTipRatio = (TotalNodes > 0 ? INodes / TotalNodes : (PopulateEmptyGridblocks ? 1 : NullValue));
                                            RelayTipRatio = (TotalNodes > 0 ? RNodes / TotalNodes : undefinedValue);
                                            IntersectingTipRatio = (TotalNodes > 0 ? YNodes / TotalNodes : (PopulateEmptyGridblocks ? 0 : NullValue));
                                            double NoConnections = (LinkStressShadows ? RNodes : 0) + (2 * YNodes);
                                            NodesPerMF = (TotalNodes > 0 ? NoConnections / TotalNodes : (PopulateEmptyGridblocks ? 0 : NullValue));

                                            // Get the time at the end of this intermediate stage
                                            EndTime = stageEndTime;
                                        }
                                    }

                                    // Update the output string for each property
                                    P32_Anisotropy += string.Format("{0} ", P32_anisotropy);
                                    P33_Anisotropy += string.Format("{0} ", P33_anisotropy);
                                    MF_UnconnectedTipRatio += string.Format("{0} ", UnconnectedTipRatio);
                                    MF_RelayTipRatio += string.Format("{0} ", RelayTipRatio);
                                    MF_IntersectingTipRatio += string.Format("{0} ", IntersectingTipRatio);
                                    ConnectionsPerMF += string.Format("{0} ", NodesPerMF);
                                    EndDeformationTime += string.Format("{0} ", EndTime);

                                    // If this is the final column in the row, add a line return to each of the output strings
                                    // This will ensure each row of data starts on a new line
                                    if (ColNo == (NoCols - 1))
                                    {
                                        P32_Anisotropy += "\n  ";
                                        P33_Anisotropy += "\n  ";
                                        MF_UnconnectedTipRatio += "\n  ";
                                        MF_RelayTipRatio += "\n  ";
                                        MF_IntersectingTipRatio += "\n  ";
                                        ConnectionsPerMF += "\n  ";
                                        EndDeformationTime += "\n  ";
                                    }

                                    // Update progress bar
                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                } // End loop through all columns and rows in the grid

                            // Add the end to each output string then write them all to the output file
                            P32_Anisotropy += "/\n\n";
                            P33_Anisotropy += "/\n\n";
                            MF_UnconnectedTipRatio += "/\n\n";
                            MF_RelayTipRatio += "/\n\n";
                            MF_IntersectingTipRatio += "/\n\n";
                            ConnectionsPerMF += "/\n\n";
                            EndDeformationTime += "/\n\n";
                            aniso_outputFile.Write(P32_Anisotropy);
                            aniso_outputFile.Write(P33_Anisotropy);
                            aniso_outputFile.Write(MF_UnconnectedTipRatio);
                            aniso_outputFile.Write(MF_RelayTipRatio);
                            aniso_outputFile.Write(MF_IntersectingTipRatio);
                            aniso_outputFile.Write(ConnectionsPerMF);
                            aniso_outputFile.Write(EndDeformationTime);

                        } // End write fracture anisotropy data

                        // Write fracture porosity data to Petrel grid
                        if (OutputFracturePorosity)
                        {
                            // If required, create a new file for the property output
                            StreamWriter poro_outputFile;
                            if (WritePropertiesToSeparateFiles)
                            {
                                string poro_outputFileName = filepath + fileNameBase + "_DensityPorosity" + fileExtension;
                                poro_outputFile = new StreamWriter(poro_outputFileName);
                                outputFiles.Add(poro_outputFile);
                                poro_outputFile.Write(headerInfo);
                                poro_outputFile.WriteLine(string.Format("-- Implicit fracture data for P32 density and fracture porosity of the whole fracture network"));
                                poro_outputFile.WriteLine(string.Format("-- Generated : {0}", outputDataSource));
                                poro_outputFile.WriteLine();
                            }
                            else
                            {
                                poro_outputFile = outputFile;
                            }

                            // Create output strings for each property
                            string uF_P32combined = string.Format("Microfracture_combined_P32\t-- Generated : DFM_Generator\n  ", outputDataSource);
                            string MF_P32combined = string.Format("Layer_bound_fracture_combined_P32\t-- Generated : DFM_Generator\n  ", outputDataSource);
                            string uF_Porosity = string.Format("Microfracture_porosity");
                            string MF_Porosity = string.Format("Layer_bound_fracture_porosity");
                            switch (FractureApertureControl)
                            {
                                case FractureApertureType.Uniform:
                                    uF_Porosity += string.Format("_UniformAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    MF_Porosity += string.Format("_UniformAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    break;
                                case FractureApertureType.SizeDependent:
                                    uF_Porosity += string.Format("_SizeDependentAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    MF_Porosity += string.Format("_SizeDependentAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    break;
                                case FractureApertureType.Dynamic:
                                    uF_Porosity += string.Format("_DynamicAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    MF_Porosity += string.Format("_DynamicAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    break;
                                case FractureApertureType.BartonBandis:
                                    uF_Porosity += string.Format("_BartonBandisAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    MF_Porosity += string.Format("_BartonBandisAperture\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    break;
                                default:
                                    uF_Porosity += string.Format("\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    MF_Porosity += string.Format("\t-- Generated : DFM_Generator\n  ", outputDataSource);
                                    break;
                            }

                            // Loop through all rows and columns in the grid
                            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                            // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                            // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                {
                                    // Check if calculation has been aborted
                                    if (progressReporter.abortCalculation())
                                    {
                                        // Clean up any resources or data
                                        break;
                                    }

                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                    GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                    // Get data from GridblockConfiguration object
                                    // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                    // In this case we will set all data to null value
                                    double uF_P32_value, MF_P32_value, uF_Porosity_value, MF_Porosity_value;
                                    if (gbc is null)
                                    {
                                        uF_P32_value = NullValue;
                                        MF_P32_value = NullValue;
                                        uF_Porosity_value = NullValue;
                                        MF_Porosity_value = NullValue;
                                    }
                                    else
                                    {
                                        if (finalStage)
                                        {
                                            uF_P32_value = gbc.MicrofractureDensity_P32();
                                            MF_P32_value = gbc.LayerBoundFractureDensity_P32();
                                            uF_Porosity_value = gbc.MicrofracturePorosity();
                                            MF_Porosity_value = gbc.LayerBoundFracturePorosity();
                                        }
                                        else
                                        {
                                            int TSNo = gbc.getTimestepIndex(stageEndTime);
                                            uF_P32_value = gbc.MicrofractureDensity_P32(TSNo);
                                            MF_P32_value = gbc.LayerBoundFractureDensity_P32(TSNo);
                                            uF_Porosity_value = gbc.MicrofracturePorosity(TSNo);
                                            MF_Porosity_value = gbc.LayerBoundFracturePorosity(TSNo);
                                        }
                                        if (!PopulateEmptyGridblocks && (MF_P32_value <= 0))
                                        {
                                            MF_P32_value = NullValue;
                                            MF_Porosity_value = NullValue;
                                        }
                                    }

                                    // Update the output string for each property
                                    uF_P32combined += string.Format("{0} ", uF_P32_value);
                                    MF_P32combined += string.Format("{0} ", MF_P32_value);
                                    uF_Porosity += string.Format("{0} ", uF_Porosity_value);
                                    MF_Porosity += string.Format("{0} ", MF_Porosity_value);

                                    // If this is the final column in the row, add a line return to each of the output strings
                                    // This will ensure each row of data starts on a new line
                                    if (ColNo == (NoCols - 1))
                                    {
                                        uF_P32combined += "\n  ";
                                        MF_P32combined += "\n  ";
                                        uF_Porosity += "\n  ";
                                        MF_Porosity += "\n  ";
                                    }

                                    // Update progress bar
                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                } // End loop through all columns and rows in the grid

                            // Add the end to each output string then write them all to the output file
                            uF_P32combined += "/\n\n";
                            MF_P32combined += "/\n\n";
                            uF_Porosity += "/\n\n";
                            MF_Porosity += "/\n\n";
                            poro_outputFile.Write(uF_P32combined);
                            poro_outputFile.Write(MF_P32combined);
                            poro_outputFile.Write(uF_Porosity);
                            poro_outputFile.Write(MF_Porosity);

                        } // End write fracture porosity data

                        // Write fracture permeability tensor and sigma factor data
                        if (OutputFracturePermeabilityTensor)
                        {
                            // If required, create a new file for the property output
                            StreamWriter perm_outputFile;
                            if (WritePropertiesToSeparateFiles)
                            {
                                string perm_outputFileName = filepath + fileNameBase + "_FracturePermeability" + fileExtension;
                                perm_outputFile = new StreamWriter(perm_outputFileName);
                                outputFiles.Add(perm_outputFile);
                                perm_outputFile.Write(headerInfo);
                                perm_outputFile.WriteLine(string.Format("-- Fracture permeability tensor the whole fracture network"));
                                perm_outputFile.WriteLine(string.Format("-- Generated : {0}", outputDataSource));
                                perm_outputFile.WriteLine();
                            }
                            else
                            {
                                perm_outputFile = outputFile;
                            }

                            // Get the algorithm and fracture types used to calculate the fracture permeability tensor
                            string FracturePermeabilityTensorCollectionName;
                            string PermeabilityTensorComponentName_base;
                            switch (FractureTypesInPermeabilityTensor)
                            {
                                case FractureType.Microfractures:
                                    FracturePermeabilityTensorCollectionName = "Microfracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_uF_";
                                    break;
                                case FractureType.LayerBoundFractures:
                                    FracturePermeabilityTensorCollectionName = "Macrofracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_MF_";
                                    break;
                                case FractureType.AllFractures:
                                    FracturePermeabilityTensorCollectionName = "Fracture permeability tensor";
                                    PermeabilityTensorComponentName_base = "k_F_";
                                    break;
                                default:
                                    FracturePermeabilityTensorCollectionName = "k_";
                                    PermeabilityTensorComponentName_base = "";
                                    break;
                            }
                            switch (PermeabilityAlgorithm)
                            {
                                case PermeabilityCalculationAlgorithm.Oda1986:
                                    FracturePermeabilityTensorCollectionName += ": Oda (1986)";
                                    break;
                                case PermeabilityCalculationAlgorithm.OdaCorrected1987:
                                    FracturePermeabilityTensorCollectionName += ": Oda corrected (1987)";
                                    break;
                                case PermeabilityCalculationAlgorithm.SizeConnectivityCorrected:
                                    FracturePermeabilityTensorCollectionName += ": Connectivity and size corrected";
                                    break;
                                default:
                                    break;
                            }

                            // Create output strings for each component of the permeability tensor
                            Dictionary<Tensor2SComponents, string> PermeabilityTensorProperties = new Dictionary<Tensor2SComponents, string>();
                            Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                            foreach (Tensor2SComponents ij in tensorComponents)
                            {
                                string PermeabilityTensor_ij = string.Format("{0}{1}\t", PermeabilityTensorComponentName_base, ij);
                                PermeabilityTensor_ij += string.Format("-- {0}\n", FracturePermeabilityTensorCollectionName);
                                PermeabilityTensorProperties[ij] = PermeabilityTensor_ij;
                            }
                            // Create an output string for the sigma factor
                            string SigmaFactorProperty = string.Format("{0}Sigma\t", PermeabilityTensorComponentName_base);
                            SigmaFactorProperty += string.Format("-- {0}\n", FracturePermeabilityTensorCollectionName);

                            // Loop through all rows and columns in the grid
                            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                            // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                            // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                {
                                    // Check if calculation has been aborted
                                    if (progressReporter.abortCalculation())
                                    {
                                        // Clean up any resources or data
                                        break;
                                    }

                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                    GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                    // Get data from GridblockConfiguration object
                                    // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                    // In this case we will set all data to null value
                                    Tensor2S gridblockPermeabilityTensor;
                                    double gridblockSigmaFactor;
                                    if (gbc is null)
                                    {
                                        gridblockPermeabilityTensor = new Tensor2S(NullValue, NullValue, NullValue, NullValue, NullValue, NullValue);
                                        gridblockSigmaFactor = NullValue;
                                    }
                                    else
                                    {
                                        // Get the appropriate permeability tensor for this gridblock
                                        if (finalStage)
                                        {
                                            switch (FractureTypesInPermeabilityTensor)
                                            {
                                                case FractureType.Microfractures:
                                                    gridblockPermeabilityTensor = gbc.MicrofracturePermeability();
                                                    gridblockSigmaFactor = gbc.MicrofractureSigmaFactor();
                                                    break;
                                                case FractureType.LayerBoundFractures:
                                                    gridblockPermeabilityTensor = gbc.MacrofracturePermeability();
                                                    gridblockSigmaFactor = gbc.MacrofractureSigmaFactor();
                                                    break;
                                                case FractureType.AllFractures:
                                                    gridblockPermeabilityTensor = gbc.TotalFracturePermeability();
                                                    gridblockSigmaFactor = gbc.TotalFractureSigmaFactor();
                                                    break;
                                                default:
                                                    gridblockPermeabilityTensor = new Tensor2S(NullValue, NullValue, NullValue, NullValue, NullValue, NullValue);
                                                    gridblockSigmaFactor = NullValue;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            int TSNo = gbc.getTimestepIndex(stageEndTime);
                                            switch (FractureTypesInPermeabilityTensor)
                                            {
                                                case FractureType.Microfractures:
                                                    gridblockPermeabilityTensor = gbc.MicrofracturePermeability(TSNo);
                                                    gridblockSigmaFactor = gbc.MicrofractureSigmaFactor(TSNo);
                                                    break;
                                                case FractureType.LayerBoundFractures:
                                                    gridblockPermeabilityTensor = gbc.MacrofracturePermeability(TSNo);
                                                    gridblockSigmaFactor = gbc.MacrofractureSigmaFactor(TSNo);
                                                    break;
                                                case FractureType.AllFractures:
                                                    gridblockPermeabilityTensor = gbc.TotalFracturePermeability(TSNo);
                                                    gridblockSigmaFactor = gbc.TotalFractureSigmaFactor(TSNo);
                                                    break;
                                                default:
                                                    gridblockPermeabilityTensor = new Tensor2S(NullValue, NullValue, NullValue, NullValue, NullValue, NullValue);
                                                    gridblockSigmaFactor = NullValue;
                                                    break;
                                            }
                                        }
                                        if (!PopulateEmptyGridblocks && (FractureTypesInPermeabilityTensor == FractureType.LayerBoundFractures))
                                        {
                                            double MF_P32_value = finalStage ? gbc.LayerBoundFractureDensity_P32() : gbc.LayerBoundFractureDensity_P32(gbc.getTimestepIndex(stageEndTime));
                                            if (!(MF_P32_value > 0))
                                            {
                                                gridblockPermeabilityTensor = new Tensor2S(NullValue, NullValue, NullValue, NullValue, NullValue, NullValue);
                                                gridblockSigmaFactor = NullValue;
                                            }
                                        }
                                    }

                                    // Update the output string for each property
                                    foreach (Tensor2SComponents ij in tensorComponents)
                                        PermeabilityTensorProperties[ij] += string.Format("{0} ", gridblockPermeabilityTensor.Component(ij));
                                    SigmaFactorProperty += string.Format("{0} ", gridblockSigmaFactor);

                                    // If this is the final column in the row, add a line return to each of the output strings
                                    // This will ensure each row of data starts on a new line
                                    if (ColNo == (NoCols - 1))
                                    {
                                        foreach (Tensor2SComponents ij in tensorComponents)
                                            PermeabilityTensorProperties[ij] += "\n  ";
                                        SigmaFactorProperty += "\n  ";
                                    }

                                    // Update progress bar
                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                } // End loop through all columns and rows in the grid

                            // Add the end to each output string then write them all to the output file
                            foreach (Tensor2SComponents ij in tensorComponents)
                                PermeabilityTensorProperties[ij] += "/\n\n";
                            SigmaFactorProperty += "/\n\n";
                            foreach (Tensor2SComponents ij in tensorComponents)
                                perm_outputFile.Write(PermeabilityTensorProperties[ij]);
                            perm_outputFile.Write(SigmaFactorProperty);

                        } // End write fracture permeability tensor data

                        // Write stiffness and compliance tensor data
                        if (OutputBulkRockElasticTensors && finalStage)
                        {
                            // If required, create a new file for the property output
                            StreamWriter CS_outputFile;
                            if (WritePropertiesToSeparateFiles)
                            {
                                string CS_outputFileName = filepath + fileNameBase + "_FractureStiffnessCompressibility" + fileExtension;
                                CS_outputFile = new StreamWriter(CS_outputFileName);
                                outputFiles.Add(CS_outputFile);
                                CS_outputFile.Write(headerInfo);
                                CS_outputFile.WriteLine(string.Format("-- Bulk rock stiffness and permeability tensors including the fracture network"));
                                CS_outputFile.WriteLine(string.Format("-- Generated : {0}", outputDataSource));
                                CS_outputFile.WriteLine();
                            }
                            else
                            {
                                CS_outputFile = outputFile;
                            }

                            // Create output strings for each component of both tensors
                            Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>> ComplianceTensorProperties = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>>();
                            Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>> StiffnessTensorProperties = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, string>>();
                            Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                            foreach (Tensor2SComponents ij in tensorComponents)
                            {
                                ComplianceTensorProperties[ij] = new Dictionary<Tensor2SComponents, string>();
                                StiffnessTensorProperties[ij] = new Dictionary<Tensor2SComponents, string>();
                                foreach (Tensor2SComponents kl in tensorComponents)
                                {
                                    string ComplianceTensor_ijkl = string.Format("S_{0}{1}", ij, kl);
                                    ComplianceTensor_ijkl += string.Format("\t-- Generated : {0}\n  ", outputDataSource);
                                    ComplianceTensorProperties[ij][kl] = ComplianceTensor_ijkl;

                                    string StiffnessTensor_ijkl = string.Format("C_{0}{1}", ij, kl);
                                    StiffnessTensor_ijkl += string.Format("\t-- Generated : {0}\n  ", outputDataSource);
                                    StiffnessTensorProperties[ij][kl] = StiffnessTensor_ijkl;
                                }
                            }

                            // Loop through all rows and columns in the grid
                            // ColNo corresponds to the Petrel grid I index, RowNo corresponds to the Petrel grid J index, and LayerNo corresponds to the Petrel grid K index
                            // In GRDECL format we must increment ColNo must be incremented first, then RowNo, and finally LayerNo (if there is more than one layer)
                            // Therefore we should place ColNo in the innermost loop, then RowNo, and LayerNo in the outermost loop (if there is more than one layer)
                            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                                {
                                    // Check if calculation has been aborted
                                    if (progressReporter.abortCalculation())
                                    {
                                        // Clean up any resources or data
                                        break;
                                    }

                                    // Get a reference to the gridblock and check if it exists - if not move on to the next one
                                    GridblockConfiguration gbc = gd.GetGridblock(RowNo, ColNo);

                                    // Get data from GridblockConfiguration object
                                    // If the gridblock does not exist or if the fracture sets do not exist we must still write an entry in the data lists
                                    // In this case we will set all data to null value
                                    Tensor4_2Sx2S gridblockComplianceTensor = new Tensor4_2Sx2S();
                                    Tensor4_2Sx2S gridblockStiffnessTensor = new Tensor4_2Sx2S();
                                    if (gbc is null)
                                    {
                                        foreach (Tensor2SComponents ij in tensorComponents)
                                            foreach (Tensor2SComponents kl in tensorComponents)
                                            {
                                                gridblockComplianceTensor.Component(ij, kl, NullValue);
                                                gridblockStiffnessTensor.Component(ij, kl, NullValue);
                                            }
                                    }
                                    else
                                    {
                                        // Get the compliance and stiffness tensors for this gridblock
                                        gridblockComplianceTensor = gbc.S_b;
                                        gridblockStiffnessTensor = gridblockComplianceTensor.Inverse();
                                    }
                                    if (!PopulateEmptyGridblocks && (FractureTypesInPermeabilityTensor == FractureType.LayerBoundFractures))
                                    {
                                        double MF_P32_value = finalStage ? gbc.LayerBoundFractureDensity_P32() : gbc.LayerBoundFractureDensity_P32(gbc.getTimestepIndex(stageEndTime));
                                        if (!(MF_P32_value > 0))
                                        {
                                            foreach (Tensor2SComponents ij in tensorComponents)
                                                foreach (Tensor2SComponents kl in tensorComponents)
                                                {
                                                    gridblockComplianceTensor.Component(ij, kl, NullValue);
                                                    gridblockStiffnessTensor.Component(ij, kl, NullValue);
                                                }
                                        }
                                    }

                                    // Update the output string for each property
                                    foreach (Tensor2SComponents ij in tensorComponents)
                                        foreach (Tensor2SComponents kl in tensorComponents)
                                        {
                                            ComplianceTensorProperties[ij][kl] += string.Format("{0} ", gridblockComplianceTensor.Component(ij, kl));
                                            StiffnessTensorProperties[ij][kl] += string.Format("{0} ", gridblockStiffnessTensor.Component(ij, kl));
                                        }

                                    // If this is the final column in the row, add a line return to each of the output strings
                                    // This will ensure each row of data starts on a new line
                                    if (ColNo == (NoCols - 1))
                                    {
                                        foreach (Tensor2SComponents ij in tensorComponents)
                                            foreach (Tensor2SComponents kl in tensorComponents)
                                            {
                                                ComplianceTensorProperties[ij][kl] += "\n  ";
                                                StiffnessTensorProperties[ij][kl] += "\n  ";
                                            }
                                    }

                                    // Update progress bar
                                    progressReporter.UpdateProgress(++NoCalculationElementsCompleted);

                                } // End loop through all columns and rows in the grid

                            // Add the end to each output string then write them all to the output file
                            foreach (Tensor2SComponents ij in tensorComponents)
                                foreach (Tensor2SComponents kl in tensorComponents)
                                {
                                    ComplianceTensorProperties[ij][kl] += "/\n\n";
                                    StiffnessTensorProperties[ij][kl] += "/\n\n";
                                }
                            foreach (Tensor2SComponents ij in tensorComponents)
                                foreach (Tensor2SComponents kl in tensorComponents)
                                {
                                    CS_outputFile.Write(ComplianceTensorProperties[ij][kl]);
                                    CS_outputFile.Write(StiffnessTensorProperties[ij][kl]);
                                }

                        } // End write stiffness and compliance tensor data

                    }
                    catch (Exception e)
                    {
                        // Write an error message
                        progressReporter.OutputMessage(string.Format("Error occurred while writing output file {0}", fileNameBase));
                        progressReporter.OutputMessage(string.Format("Error message: {0}", e.Message));
                    }
                    finally
                    {
                        // Close all the output files
                        foreach (StreamWriter nextFile in outputFiles)
                            nextFile.Close();
                    }
                }
            }
        }
        /// <summary>
        /// Write explicit DFN data to a FAB format file or files
        /// Data will be written for each specified intermediate stage as well as the final stage
        /// </summary>
        /// <param name="ModelName">Name of the model to output - will be included in the filenames</param>
        /// <param name="progressReporter">Reference to progress reporter implementing the IProgressReporterWrapper interface</param>
        /// <param name="WriteuFData">Write data for microfractures in the DFN</param>
        /// <param name="WriteMFData">Write data for layer-bound macrofractures in the DFN</param>
        public void WriteFAB(string ModelName, IProgressReporterWrapper progressReporter, bool WriteuFData, bool WriteMFData)
        {
            // Get control data from DFNControl object
            // Folder to write output files in
            string filepath = gd.DFNControl.FolderPath;
            // Number of intermediate DFNs to output and flag to control their separation
            int NoIntermediateOutputs = gd.DFNControl.NumberOfIntermediateOutputs;
            if (NoIntermediateOutputs < 0) NoIntermediateOutputs = 0;
            IntermediateOutputInterval IntermediateOutputIntervalControl = gd.DFNControl.SeparateIntermediateOutputsBy;
            // Number of cornerpoints for each microfracture
            int nouFCornerPoints = gd.DFNControl.NumberOfuFPoints;
            // Default values for fracture aperture, permeability and compressibility
            double DefaultFractureAperture = gd.DFNControl.DefaultFractureAperture;
            double DefaultFracturePermeability = gd.DFNControl.DefaultFracturePermeability;
            double DefaultFractureCompressibility = gd.DFNControl.DefaultFractureCompressibility;
            // Get the time units and time units modifier for the output labels
            double currentTime = gd.CurrentDFN.CurrentTime;
            TimeUnits timeUnits = gd.DFNControl.timeUnits;
            string ProjectTimeUnits;
            switch (timeUnits)
            {
                case TimeUnits.second:
                    ProjectTimeUnits = "seconds";
                    break;
                case TimeUnits.year:
                    ProjectTimeUnits = "years";
                    break;
                case TimeUnits.ma:
                    ProjectTimeUnits = "ma";
                    break;
                default:
                    ProjectTimeUnits = "seconds";
                    break;
            }
            double timeUnits_Modifier = gd.DFNControl.getTimeUnitsModifier();

            // If the calculation has already been cancelled, do not write any output data
            if (!progressReporter.abortCalculation())
            {
                // Write explicit fracture property data to an FAB file
                progressReporter.OutputMessage("Write explicit data to FAB file(s)");

                // Set the output file extension
                string fileExtension = ".GRDECL";

                // Get the total number of fractures to write and update the progress bar
                int totalNoFractures = 0;
                foreach (GlobalDFN DFN in gd.DFNGrowthStages)
                {
                    totalNoFractures += (DFN.GlobalDFNMicrofractures.Count + DFN.GlobalDFNMacrofractures.Count);

                }

                // Set the number of elements in the progress bar to twice the total number of fractures
                // We must loop through all the fractures twice - the first time to generate the fracture objects and the second to assign properties to them
                // Unless we are generating fracture centrelines in which case we will need to loop through a third time
                int numberOfElements = totalNoFractures * 2;
                progressReporter.SetNumberOfElements(numberOfElements);
                //int noFracturesGenerated = 0;

                // Loop through each stage in the fracture growth
                int stageNumber = 1;
                int NoStages = gd.DFNGrowthStages.Count;

                // Loop through each stage in the fracture growth
                foreach (GlobalDFN DFN in gd.DFNGrowthStages)
                {
                    // Create a stage-specific label and description for the output
                    string outputStageLabel;
                    string stageNameOverride = null;
                    if ((stageNameOverride is null) || (stageNameOverride.Length == 0))
                        outputStageLabel = (stageNumber == NoStages) ? "_final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, (DFN.CurrentTime / timeUnits_Modifier).ToString("G3"), ProjectTimeUnits);
                    else
                        outputStageLabel = "_" + stageNameOverride;
                    string outputStageParams = string.Format("Model name: {0}\n", ModelName);
                    outputStageParams += (stageNumber == NoStages) ? "Final stage" : string.Format("Stage {0}", stageNumber);
                    outputStageParams += (stageNameOverride is null) ? "\n" : string.Format(": {0}\n", stageNameOverride);
                    outputStageParams += string.Format("Time {0}{1}\n", (DFN.CurrentTime / timeUnits_Modifier), ProjectTimeUnits);
                    outputStageParams += "\n";

                    // Write the model time of the intermediate DFN to the Petrel log window
                    progressReporter.OutputMessage(string.Format("DFN realisation {0} at time {1} {2}", stageNumber, (DFN.CurrentTime / timeUnits_Modifier), ProjectTimeUnits));

                    // Create a list of all output files for this stage - this will make it easier to keep track of them and ensure that all of them are closed at the end
                    List<StreamWriter> outputFiles = new List<StreamWriter>();

                    try
                    {
                        // Write microfracture data to file
                        if (WriteuFData)
                        {
                            // Create output file for microfractures
                            string fileNameBase = ModelName + outputStageLabel + "_Microfractures";
                            string outputFileName = filepath + fileNameBase + fileExtension;
                            StreamWriter uF_outputFile = new StreamWriter(outputFileName);
                            outputFiles.Add(uF_outputFile);

                            {
                                int No_uFracs = DFN.GlobalDFNMicrofractures.Count();
                                int No_Nodes = No_uFracs * nouFCornerPoints;

                                // Write general fracture FAB header data to logfile
                                string FAB_header_1 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}\r\n{4}", "BEGIN FORMAT", "Format = Ascii", "Length_Unit = M", "XAxis = East", "Scale = 8124.44");
                                uF_outputFile.WriteLine(FAB_header_1);
                                string FAB_header5 = string.Format("{0} {1}", "No_Fractures =", No_uFracs);
                                string FAB_header6 = string.Format("No_TessFractures = 0");
                                string FAB_header7 = string.Format("{0} {1}", "No_Nodes = ", No_Nodes);
                                uF_outputFile.WriteLine(FAB_header5);
                                uF_outputFile.WriteLine(FAB_header6);
                                uF_outputFile.WriteLine(FAB_header7);

                                string FAB_header_3 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}\r\n", "No_RockBlocks = 0", "No_NodesRockBlock = 0", "No_Properties = 3", "END FORMAT");
                                uF_outputFile.WriteLine(FAB_header_3);
                                string FAB_header_4 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}", "BEGIN PROPERTIES", "Prop1    =    (Real*4) \"Permeability\"", "Prop2    =    (Real*4) \"Compressibility\"", "Prop3    =    (Real*4) \"Aperture\"");
                                uF_outputFile.WriteLine(FAB_header_4);
                                string FAB_header_5 = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n{3}\r\n\r\n{4}", "END PROPERTIES", "BEGIN SETS", "Set1    =    \"Discrete fractures\"", "END SETS", "BEGIN FRACTURE");
                                uF_outputFile.WriteLine(FAB_header_5);

                                // Loop through each microfracture and write data to logfile
                                for (int uFracNo = 0; uFracNo < No_uFracs; uFracNo++)
                                {
                                    MicrofractureXYZ frac = DFN.GlobalDFNMicrofractures[uFracNo];
                                    double aperture = frac.MeanAperture;
                                    double permeability = Math.Pow(aperture, 2) / 12;
                                    if (double.IsNaN(aperture))
                                    {
                                        aperture = DefaultFractureAperture;
                                        permeability = DefaultFracturePermeability;
                                    }
                                    double compressibility = frac.Compressibility;
                                    if (double.IsNaN(compressibility))
                                        compressibility = DefaultFractureCompressibility;

                                    string data = string.Format("{0} {1} {2} {3} {4} {5}", uFracNo + 1, nouFCornerPoints, 1, permeability, compressibility, aperture);
                                    uF_outputFile.WriteLine(data);

                                    // Get a list of cornerpoints using the GetFractureCornerpointsInXYZ function
                                    List<PointXYZ> cornerPoints = frac.GetFractureCornerpointsInXYZ(nouFCornerPoints);
                                    // Loop through each point and write the coordinates to file
                                    int pointNo = 1;
                                    foreach (PointXYZ cornerPoint in cornerPoints)
                                    {
                                        string pointCoords = string.Format("{0} {1} {2} {3}", pointNo++, cornerPoint.X, cornerPoint.Y, cornerPoint.Z);
                                        uF_outputFile.WriteLine(pointCoords);
                                    }

                                    VectorXYZ fractureNormal = VectorXYZ.GetNormalToPlane(frac.Azimuth, frac.Dip);
                                    string lastLine = string.Format("{0} {1} {2} {3}", 0, fractureNormal.Component(VectorComponents.X), fractureNormal.Component(VectorComponents.Y), fractureNormal.Component(VectorComponents.Z));
                                    uF_outputFile.WriteLine(lastLine);
                                }

                                // Write FAB footer data to logfile
                                string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                uF_outputFile.WriteLine(footer);
                            }

                            // Close microfracture output file
                            uF_outputFile.Close();
                        }

                        // Write macrofracture data to file
                        if (WriteMFData)
                        {
                            // Create file for microfractures
                            string fileNameBase = ModelName + outputStageLabel + "_LayerBoundFractures";
                            string outputFileName = filepath + fileNameBase + fileExtension;
                            StreamWriter MF_outputFile = new StreamWriter(outputFileName);
                            outputFiles.Add(MF_outputFile);

                            {
                                int No_MFracs = DFN.GlobalDFNMacrofractures.Count;
                                int No_Segments = 0;
                                int No_Cornerpoints = 0;

                                // Loop through each macrofracture and count the total number of segments and cornerpoints, excluding zero length segments
                                foreach (MacrofractureXYZ frac in DFN.GlobalDFNMacrofractures)
                                {
                                    foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                    {
                                        int nonZeroLengthSegments = 0;
                                        foreach (bool segmentFlag in frac.ZeroLengthSegments[dir])
                                            if (!segmentFlag)
                                                nonZeroLengthSegments++;
                                        No_Segments += nonZeroLengthSegments;
                                        No_Cornerpoints += nonZeroLengthSegments * 4;
                                    }
                                }

                                // Write general fracture FAB header data to logfile
                                string FAB_header_1 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}\r\n{4}", "BEGIN FORMAT", "Format = Ascii", "Length_Unit = M", "XAxis = East", "Scale = 8124.44");
                                MF_outputFile.WriteLine(FAB_header_1);
                                string FAB_header5 = string.Format("{0} {1}", "No_Fractures =", No_Segments);
                                string FAB_header6 = string.Format("No_TessFractures = 0");
                                // NB In FAB terminology, "Nodes" refer to Cornerpoints
                                string FAB_header7 = string.Format("{0} {1}", "No_Nodes = ", No_Cornerpoints);
                                MF_outputFile.WriteLine(FAB_header5);
                                MF_outputFile.WriteLine(FAB_header6);
                                MF_outputFile.WriteLine(FAB_header7);

                                string FAB_header_3 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}\r\n", "No_RockBlocks = 0", "No_NodesRockBlock = 0", "No_Properties = 3", "END FORMAT");
                                MF_outputFile.WriteLine(FAB_header_3);
                                string FAB_header_4 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}", "BEGIN PROPERTIES", "Prop1    =    (Real*4) \"Permeability\"", "Prop2    =    (Real*4) \"Compressibility\"", "Prop3    =    (Real*4) \"Aperture\"");
                                MF_outputFile.WriteLine(FAB_header_4);
                                string FAB_header_5 = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n{3}\r\n\r\n{4}", "END PROPERTIES", "BEGIN SETS", "Set1    =    \"Discrete fractures\"", "END SETS", "BEGIN FRACTURE");
                                MF_outputFile.WriteLine(FAB_header_5);

                                // Loop through each macrofracture segment and write data to logfile
                                int global_segmentNo = 1;
                                foreach (MacrofractureXYZ MF in DFN.GlobalDFNMacrofractures)
                                {
                                    // Get a list of normal vectors to the fracture segments
                                    Dictionary<PropagationDirection, List<VectorXYZ>> segmentNormalVectors = MF.GetSegmentNormalVectors();

                                    foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                    {
                                        int MF_noSegments = MF.SegmentCornerPoints[dir].Count;
                                        for (int MF_segmentNo = 0; MF_segmentNo < MF_noSegments; MF_segmentNo++)
                                        {
                                            // Check if it is a zero length segment; if so, move on to the next segment
                                            if (MF.ZeroLengthSegments[dir][MF_segmentNo])
                                                continue;

                                            // Get a reference to the cornerpoint list for the segment
                                            List<PointXYZ> segment = MF.SegmentCornerPoints[dir][MF_segmentNo];

                                            int noNodes = segment.Count;

                                            // Set the fracture aperture
                                            double aperture = MF.SegmentMeanAperture[dir][MF_segmentNo];
                                            double permeability = Math.Pow(aperture, 2) / 12;
                                            if (double.IsNaN(aperture))
                                            {
                                                aperture = DefaultFractureAperture;
                                                permeability = DefaultFracturePermeability;
                                            }
                                            double compressibility = MF.SegmentCompressibility[dir][MF_segmentNo];
                                            if (double.IsNaN(compressibility))
                                                compressibility = DefaultFractureCompressibility;

                                            string data = string.Format("{0} {1} {2} {3} {4} {5}", global_segmentNo, noNodes, 1, permeability, compressibility, aperture);
                                            MF_outputFile.WriteLine(data);

                                            int nodeCounter = 1;

                                            foreach (PointXYZ nextPoint in segment)
                                            {
                                                string pointCoords = string.Format("{0} {1} {2} {3}", nodeCounter, nextPoint.X, nextPoint.Y, nextPoint.Z);
                                                MF_outputFile.WriteLine(pointCoords);
                                                nodeCounter++;
                                            }

                                            VectorXYZ segmentNormal = segmentNormalVectors[dir][MF_segmentNo];
                                            string lastLine = string.Format("{0} {1} {2} {3}", 0, segmentNormal.Component(VectorComponents.X), segmentNormal.Component(VectorComponents.Y), segmentNormal.Component(VectorComponents.Z));
                                            MF_outputFile.WriteLine(lastLine);

                                            global_segmentNo++;
                                        }
                                    }
                                }

                                // Write FAB footer data to logfile
                                string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                MF_outputFile.WriteLine(footer);
                            }

                            // Close macrofracture  output file
                            MF_outputFile.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        // Write an error message
                        progressReporter.OutputMessage(string.Format("Error occurred while writing output files for stage {0}", outputStageLabel));
                        progressReporter.OutputMessage(string.Format("Error message: {0}", e.Message));
                    }
                    finally
                    {
                        // Close all the output files
                        foreach (StreamWriter nextFile in outputFiles)
                            nextFile.Close();
                    }
                }
            }
        }

        // Constructors
        /// <summary>
        /// Default constructor - must supply a grid to write from and read to
        /// </summary>
        /// <param name="grid">Reference to a FractureGrid object to read from and write to</param>
        public EclipseImportExport(FractureGrid grid)
        {
            // Set a reference to the grid object
            gd = grid;

            // Set the null value
            NullValue = -999.99;
        }
    }
}
