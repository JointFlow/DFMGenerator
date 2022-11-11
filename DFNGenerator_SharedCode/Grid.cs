using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Class representing the entire fractured layer, containing a 2D array of gridblocks
    /// </summary>
    class FractureGrid
    {
        /// <summary>
        /// Program name and version number (hard coded)
        /// </summary>
        public static string VersionNumber { get { return "DFNGenerator v2.1.1"; } }

        // Grid data
        /// <summary>
        /// 2D array containing GridblockConfiguration objects for each cell in the grid
        /// </summary>
        private List<List<GridblockConfiguration>> Gridblocks;
        /// <summary>
        /// GlobalDFN object representing the current DFN
        /// </summary>
        public GlobalDFN CurrentDFN;
        /// <summary>
        /// List of GlobalDFN objects representing the DFN at different stages of fracture propagation
        /// </summary>
        public List<GlobalDFN> DFNGrowthStages;
        /// <summary>
        /// Return reference to a specific gridblock in the grid
        /// </summary>
        /// <param name="RowNo">Row number of gridblock to retrieve</param>
        /// <param name="ColNo">Column number of gridblock to retrieve</param>
        /// <returns>Reference to the specified GridblockConfiguration object</returns>
        public GridblockConfiguration GetGridblock(int RowNo, int ColNo)
        {
            return Gridblocks[RowNo][ColNo];
        }

        // Objects containing geomechanical, fracture property and calculation data relating to the grid
        /// <summary>
        /// Control data for generating the DFN 
        /// </summary>
        public DFNGenerationControl DFNControl;
        /// <summary>
        /// Flag to indicate that the explicit DFN was not generated in some gridblocks, due to the layer thickness cutoff
        /// </summary>
        public bool DFNThicknessCutoffActivated { get; private set; }

        // Functions to calculate fracture population data and generate the DFN
        /// <summary>
        /// Calculate fracture data for each cell in the gridblock based on existing GridblockConfiguration.PropagationControl objects, without updating progress
        /// </summary>
        public void CalculateAllFractureData()
        {
            CalculateAllFractureData(null);
        }
        /// <summary>
        /// Calculate fracture data for each cell in the gridblock based on existing GridblockConfiguration.PropagationControl objects 
        /// </summary>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        public void CalculateAllFractureData(IProgressReporterWrapper progressReporter)
        {
            // Create variable to loop through the grid rows and columns
            int NoRows, RowNo, NoCols, ColNo;

            // If the supplied progress reporter is null, create a new DefaultProgressReporter object (this will not actually report any progress)
            // Otherwise calculate the number of gridblocks and set the total number of calculation elements (= number of gridblocks)
            if (progressReporter == null)
            {
                progressReporter = new DefaultProgressReporter();
            }
            else
            {
                // Calculate total number of gridblocks
                int TotalNoGridblocks = 0;
                NoRows = Gridblocks.Count();
                for (RowNo = 0; RowNo < NoRows; RowNo++)
                    TotalNoGridblocks += Gridblocks[RowNo].Count();

                // Set the total number of calculation elements in the progress reporter
                progressReporter.SetNumberOfElements(TotalNoGridblocks);
            }

            // Loop through every gridblock in every row in the grid
            int NoGridblocksCalculated = 0;
            NoRows = Gridblocks.Count();
            for(RowNo = 0; RowNo < NoRows; RowNo++)
            {
                List<GridblockConfiguration> GridRow = Gridblocks[RowNo];
                NoCols = GridRow.Count();
                for(ColNo = 0; ColNo < NoCols; ColNo++)
                {
                    // Check if calculation has been aborted
                    if (progressReporter.abortCalculation())
                    {
                        // Clean up any resources or data
                        break;
                    }

                    // Get a reference to the GridblockConfiguration object
                    GridblockConfiguration Gridblock = GridRow[ColNo];

                    // Check if it is null
                    if (Gridblock != null)
                    {
                        // Run the calculation function for the specified gridblock
                        Gridblock.CalculateFractureData();

                    }

                    // Update progress
                    progressReporter.UpdateProgress(++NoGridblocksCalculated);
                }
            }
        }
        /// <summary>
        /// Generate a global DFN based on based on existing Grid.DFNControl object, without updating progress
        /// </summary>
        public void GenerateDFN()
        {
            GenerateDFN(null);
        }
        /// <summary>
        /// Generate a global DFN based on based on existing Grid.DFNControl object
        /// </summary>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        public void GenerateDFN(IProgressReporterWrapper progressReporter)
        {
            // If flag to generate explicit DFN is set to false, abort calculation
            if (!DFNControl.GenerateExplicitDFN)
                return;

            // Flag to write fracture geometry data to file
            bool writeDFNToFile = DFNControl.WriteDFNFiles;
            // Flag to write centrepoints to file
            bool outputCentrepoints = DFNControl.outputCentrepoints;
            // Number of cornerpoints for microfracture polygons - if less than 3 we will not generate a polygon
            int nouFCornerPoints = DFNControl.NumberOfuFPoints;
            bool generateuFPolygon = (nouFCornerPoints >= 3);
            // Number of intermediate DFNs to output and flag to control their separation
            int noIntermediateDFNs = DFNControl.NumberOfIntermediateOutputs;
            if (noIntermediateDFNs < 0) noIntermediateDFNs = 0;
            IntermediateOutputInterval separateIntermediatesBy = DFNControl.SeparateIntermediateOutputsBy;

            // Calculate unit conversion modifier for output time data if not in SI units
            TimeUnits timeUnits = DFNControl.timeUnits;
            double timeUnits_Modifier = DFNControl.getTimeUnitsModifier();

            // Initialise flags to indicate that the explicit DFN was not generated in some or any gridblocks, due to the layer thickness cutoff
            DFNThicknessCutoffActivated = false;

            // Generate a list of timestep end times for all timesteps in every gridblock in the grid
            int totalNoCalculationElements;
            List<GridblockTimestepControl> timestepList = GetTimestepList(out totalNoCalculationElements);

            // If the supplied progress reporter is null, create a new DefaultProgressReporter object (this will not actually report any progress)
            // Otherwise set the total number of calculation elements in the progress reporter
            if (progressReporter == null)
                progressReporter = new DefaultProgressReporter();
            else
                progressReporter.SetNumberOfElements(totalNoCalculationElements);

            // Create counters for the current calculation element and the next output stage, and a flag for completion of the calculation
            int currentCalculationElement = 0;
            int nextStage = 1;
            bool calculationCompleted = !(totalNoCalculationElements > 0);

            // Find the end time of the last GridblockTimestepControl object and the required separation (in calculation elements and time) between output of intermediate DFNs
            double endTime = (calculationCompleted ? 0 : timestepList[totalNoCalculationElements - 1].EndTimestepTime);

            // Loop through the intermediate DFNs
            while (!calculationCompleted)
            {
                // Run the calculation to the next required intermediate point, or to completion if no intermediates are required
                if (separateIntermediatesBy == IntermediateOutputInterval.SpecifiedTime)
                {
                    double nextIntermediateEndTime = (nextStage < DFNControl.IntermediateOutputTimes.Count ? DFNControl.IntermediateOutputTimes[nextStage] : endTime);
                    calculationCompleted = PropagateLocalDFNs(ref currentCalculationElement, nextIntermediateEndTime, timestepList, progressReporter);
                }
                else if (separateIntermediatesBy == IntermediateOutputInterval.EqualTime)
                {
                    double nextIntermediateEndTime = (nextStage * endTime) / (noIntermediateDFNs + 1);
                    calculationCompleted = PropagateLocalDFNs(ref currentCalculationElement, nextIntermediateEndTime, timestepList, progressReporter);
                }
                else
                {
                    int nextIntermediateCalculationElement = ((nextStage * totalNoCalculationElements) / (noIntermediateDFNs + 1)) - 1;
                    calculationCompleted = PropagateLocalDFNs(ref currentCalculationElement, nextIntermediateCalculationElement, timestepList, progressReporter);
                }

                // If we are at an intermediate stage, make a copy of the current DFN and add it to the list of intermediate DFNs
                // If we are at the end of the calculation, create a reference to the current DFN and add it to the list of intermediate DFNs
                // In that way we will avoid duplicating the final stage of the DFN (and doubling the memory overhead if we are not outputting intermediate stages)
                GlobalDFN latestDFN = (calculationCompleted ? CurrentDFN : new GlobalDFN(CurrentDFN));
                DFNGrowthStages.Add(latestDFN);

                // Remove all zero radius microfractures and zero length macrofractures and any above the specified maximum number from the latest DFN
                // NB Since we have already made a copy of the CurrentDFN object, these fractures can still be activated in subsequent propagation stages
                double minRadius = 0;
                double minLength = 0;
                latestDFN.removeShortestFractures(minRadius, minLength, DFNControl.maxNoFractures);

                // Check if calculation has been aborted
                if (progressReporter.abortCalculation())
                {
                    // Clean up any resources or data
                    break;
                }

                // Write fracture data to file
                if (writeDFNToFile)
                {
                    // Create a stage-specific label for the output file
                    string outputLabel = (calculationCompleted ? "final" : string.Format("Stage{0}_Time{1}", nextStage, CurrentDFN.CurrentTime / timeUnits_Modifier));

                    // Write microfracture data to file
                    {
                        // Create file for microfractures
                        string fileName = "Microfractures_" + outputLabel + ".txt";
                        String namecomb = DFNControl.FolderPath + fileName;
                        StreamWriter uF_outputFile = new StreamWriter(namecomb);

                        switch (DFNControl.OutputFileType)
                        {
                            case DFNFileType.ASCII:
                                {
                                    // Write header data
                                    string FSheader1 = "FracNo\tSet\tCentre X\tCentre Y\tCentre Depth\tRadius\tDip\tAzimuth\tActive\t";
                                    uF_outputFile.WriteLine(FSheader1);

                                    // Loop through each microfracture and write data to logfile
                                    //int No_uFracs = latestDFN.GlobalDFNMicrofractures.Count();
                                    //for (int uFracNo = 0; uFracNo < No_uFracs; uFracNo++)
                                    foreach (MicrofractureXYZ frac in latestDFN.GlobalDFNMicrofractures)
                                    {
                                        string data = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t", frac.MicrofractureID, frac.SetIndex, frac.CentrePoint.X, frac.CentrePoint.Y, frac.CentrePoint.Depth, frac.Radius, frac.Dip, frac.Azimuth, frac.Active);
                                        uF_outputFile.WriteLine(data);

                                        // Generate a polygon if required
                                        if (generateuFPolygon)
                                        {
                                            // Write cornerpoint coordinates to logfile - one row per point
                                            uF_outputFile.WriteLine("Start Points");

                                            // Get a list of cornerpoints using the GetFractureCornerpointsInXYZ function
                                            List<PointXYZ> cornerPoints = frac.GetFractureCornerpointsInXYZ(nouFCornerPoints);
                                            // Loop through each point and write the coordinates to file
                                            foreach (PointXYZ cornerPoint in cornerPoints)
                                            {
                                                string pointCoords = string.Format("{0}\t{1}\t{2}\t", cornerPoint.X, cornerPoint.Y, cornerPoint.Depth);
                                                uF_outputFile.WriteLine(pointCoords);
                                            }

                                            uF_outputFile.WriteLine("End Points");
                                        }
                                    }
                                }
                                break;
                            case DFNFileType.FAB:
                                {
                                    int No_uFracs = latestDFN.GlobalDFNMicrofractures.Count();
                                    int No_Nodes = No_uFracs * nouFCornerPoints;

                                    double permability = DFNControl.DefaultFracturePermeability;
                                    double compressibility = DFNControl.DefaultFractureCompressibility;

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

                                    // Loop through each macrofracture and write data to logfile
                                    for (int uFracNo = 0; uFracNo < No_uFracs; uFracNo++)
                                    {
                                        MicrofractureXYZ frac = latestDFN.GlobalDFNMicrofractures[uFracNo];

                                        string data = string.Format("{0} {1} {2} {3} {4} {5}", uFracNo + 1, nouFCornerPoints, 1, permability, compressibility, frac.MeanAperture);
                                        uF_outputFile.WriteLine(data);

                                        // Get a list of cornerpoints using the GetFractureCornerpointsInXYZ function
                                        List<PointXYZ> cornerPoints = frac.GetFractureCornerpointsInXYZ(nouFCornerPoints);
                                        // Loop through each point and write the coordinates to file
                                        int pointNo = 1;
                                        foreach(PointXYZ cornerPoint in cornerPoints)
                                        {
                                            string pointCoords = string.Format("{0} {1} {2} {3}", pointNo++, cornerPoint.X, cornerPoint.Y, cornerPoint.Z);
                                            uF_outputFile.WriteLine(pointCoords);
                                        }

                                        string lastLine = string.Format("{0} {1} {2} {3}", 0, -0.733665229313, 0.679473616879, 0.00713689448164);
                                        uF_outputFile.WriteLine(lastLine);
                                    }

                                    // Write FAB footer data to logfile
                                    string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                    uF_outputFile.WriteLine(footer);
                                }
                                break;
                            default:
                                break;
                        }

                        // Close microfracture output file
                        uF_outputFile.Close();
                    }

                    // Write macrofracture data to file
                    {
                        // Create output file for macrofractures
                        string fileName = "Macrofractures_" + outputLabel + ".txt";
                        String namecomb = DFNControl.FolderPath + fileName;
                        StreamWriter MF_outputFile = new StreamWriter(namecomb);

                        switch (DFNControl.OutputFileType)
                        {
                            case DFNFileType.ASCII:
                                {
                                    // Write header data
                                    string FSheader1 = string.Format("FracNo\tSet\tIPlusHalfLength\tIMinusHalfLength\tNumber of points\tDip\tIPlusTipType\tIMinusTipType\tIPlusTerminatingFracture\tIMinusTerminatingFracture\tNucleation time ({0})\t", timeUnits);
                                    MF_outputFile.WriteLine(FSheader1);

                                    // Loop through each macrofracture and write data to logfile
                                    foreach (MacrofractureXYZ frac in latestDFN.GlobalDFNMacrofractures)
                                    {
                                        // Generate a list of fracture cornerpoints
                                        List<PointXYZ> CornerPoints = frac.GetCornerpoints();
                                        int NoPoints = CornerPoints.Count();

                                        // Write general fracture data to logfile
                                        string data = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t", frac.MacrofractureID, frac.SetIndex, frac.StrikeHalfLength(PropagationDirection.IPlus), frac.StrikeHalfLength(PropagationDirection.IMinus), NoPoints, frac.Dip, frac.TipTypes(PropagationDirection.IPlus), frac.TipTypes(PropagationDirection.IMinus), frac.TerminatingFracture(PropagationDirection.IPlus), frac.TerminatingFracture(PropagationDirection.IMinus), frac.NucleationTime / timeUnits_Modifier);
                                        MF_outputFile.WriteLine(data);

                                        // Write cornerpoint coordinates to logfile - one row per point
                                        MF_outputFile.WriteLine("Start Points");
                                        // Loop through each point and write the coordinates to file
                                        // NB Z coordinates are output as positive downwards
                                        foreach (PointXYZ nextPoint in CornerPoints)
                                        {
                                            string pointCoords = string.Format("{0}\t{1}\t{2}\t", nextPoint.X, nextPoint.Y, nextPoint.Depth);
                                            MF_outputFile.WriteLine(pointCoords);
                                        }
                                        MF_outputFile.WriteLine("End Points");
                                    }
                                }
                                break;
                            case DFNFileType.FAB:
                                {
                                    int No_MFracs = latestDFN.GlobalDFNMacrofractures.Count;
                                    int No_Segments = 0;
                                    int No_Cornerpoints = 0;

                                    // Loop through each macrofracture and count the total number of segments and cornerpoints
                                    foreach (MacrofractureXYZ frac in latestDFN.GlobalDFNMacrofractures)
                                    { 
                                        foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                        {
                                            No_Segments += frac.SegmentCornerPoints[dir].Count;
                                            No_Cornerpoints += frac.SegmentCornerPoints[dir].Count * 4;
                                        }
                                    }

                                    double permability = DFNControl.DefaultFracturePermeability;
                                    double compressibility = DFNControl.DefaultFractureCompressibility;

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
                                    int segmentNo = 1;
                                    foreach (MacrofractureXYZ frac in latestDFN.GlobalDFNMacrofractures)
                                    {
                                        foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                                        {
                                            int segmentCounter = 0;

                                            foreach (List<PointXYZ> segment in frac.SegmentCornerPoints[dir])
                                            {
                                                int noNodes = segment.Count;

                                                // Set the fracture aperture
                                                double aperture = frac.SegmentMeanAperture[dir][segmentCounter];

                                                string data = string.Format("{0} {1} {2} {3} {4} {5}", segmentNo, noNodes, 1, permability, compressibility, aperture);
                                                MF_outputFile.WriteLine(data);

                                                int nodeCounter = 1;

                                                foreach (PointXYZ nextPoint in segment)
                                                {
                                                    string pointCoords = string.Format("{0} {1} {2} {3}", nodeCounter, nextPoint.X, nextPoint.Y, nextPoint.Z);
                                                    MF_outputFile.WriteLine(pointCoords);
                                                    nodeCounter++;
                                                }

                                                string lastLine = string.Format("{0} {1} {2} {3}", 0, -0.733665229313, 0.679473616879, 0.00713689448164);
                                                MF_outputFile.WriteLine(lastLine);

                                                if (segmentCounter < frac.SegmentMeanAperture[dir].Count - 1) segmentCounter++;
                                                segmentNo++;
                                            }
                                        }
                                    }

                                    // Write FAB footer data to logfile
                                    string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                    MF_outputFile.WriteLine(footer);
                                }
                                break;
                            default:
                                break;
                        }

                        // Close macrofracture  output file
                        MF_outputFile.Close();
                    }

                    // If required, write macrofracture centrepoints to file
                    if (outputCentrepoints)
                    {
                        // Create output file for macrofracture centrepoints
                        string fileName = "MFCentrepoints_" + outputLabel + ".txt";
                        String namecomb = DFNControl.FolderPath + fileName;
                        StreamWriter CP_outputFile = new StreamWriter(namecomb);

                        switch (DFNControl.OutputFileType)
                        {
                            case DFNFileType.ASCII:
                                {
                                    // Write header data
                                    string FSheader1 = string.Format("FracNo\tSet\tIPlusHalfLength\tIMinusHalfLength\tNumber of points\tDip\tIPlusTipType\tIMinusTipType\tIPlusTerminatingFracture\tIMinusTerminatingFracture\tNucleation time ({0})\t", timeUnits);
                                    CP_outputFile.WriteLine(FSheader1);

                                    // Loop through each macrofracture and write data to logfile
                                    foreach (MacrofractureXYZ frac in latestDFN.GlobalDFNMacrofractures)
                                    {
                                        // Generate a list of fracture cornerpoints
                                        List<PointXYZ> CentrePoints = frac.GetCentrepoints();
                                        int NoPoints = CentrePoints.Count();

                                        // Write general fracture data to logfile
                                        string data = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t", frac.MacrofractureID, frac.SetIndex, frac.StrikeHalfLength(PropagationDirection.IPlus), frac.StrikeHalfLength(PropagationDirection.IMinus), NoPoints, frac.Dip, frac.TipTypes(PropagationDirection.IPlus), frac.TipTypes(PropagationDirection.IMinus), frac.TerminatingFracture(PropagationDirection.IPlus), frac.TerminatingFracture(PropagationDirection.IMinus), frac.NucleationTime / timeUnits_Modifier);
                                        CP_outputFile.WriteLine(data);

                                        // Write cornerpoint coordinates to logfile - one row per point
                                        CP_outputFile.WriteLine("Start Points");
                                        // Loop through each point and write the coordinates to file
                                        // NB Z coordinates are output as positive downwards
                                        foreach (PointXYZ nextPoint in CentrePoints)
                                        {
                                            string pointCoords = string.Format("{0}\t{1}\t{2}\t", nextPoint.X, nextPoint.Y, nextPoint.Depth);
                                            CP_outputFile.WriteLine(pointCoords);
                                        }
                                        CP_outputFile.WriteLine("End Points");
                                    }
                                }
                                break;
                            case DFNFileType.FAB:
                                {
                                    // Add code to write output in a format that can be read by Petrel as a polyline
                                }
                                break;
                            default:
                                break;
                        }

                        // Close macrofracture centrepoint output file
                        CP_outputFile.Close();
                    }
                } // End write fracture data to file

                // Update the next output stage counter
                nextStage++;

            } // End loop through the intermediate DFNs
        }
        /// <summary>
        /// Generate a global DFN based on user-specified DFNControl object - copy this into the Grid object first
        /// </summary>
        /// <param name="dfnc_in">DFNControl object containing control data for generating the DFN</param>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        public void GenerateDFN(DFNGenerationControl dfnc_in, IProgressReporterWrapper progressReporter)
        {
            DFNControl = dfnc_in;
            GenerateDFN(progressReporter);

            return;
        }
        /// <summary>
        /// Generate a list of GridblockTimestepControl objects for all timesteps in every gridblock in the grid
        /// </summary>
        /// <param name="totalNoCalculationElements">Reference parameter for the total number of calculation elements (i.e. the total number of timesteps in all gridblocks)</param>
        /// <returns>New list of GridblockTimestepControl objects representing all timesteps in every gridblock in the grid</returns>
        private List<GridblockTimestepControl> GetTimestepList (out int totalNoCalculationElements)
        {
            // Create a new list of GridblockTimestepControl objects and set the total number of calculation elements (i.e. the number of GridblockTimestepControl objects) to 0
            totalNoCalculationElements = 0;
            List<GridblockTimestepControl> timestepList = new List<GridblockTimestepControl>();

            // Loop through every gridblock in every row in the grid
            int NoRows = Gridblocks.Count();
            for (int RowNo = 0; RowNo < NoRows; RowNo++)
            {
                List<GridblockConfiguration> GridRow = Gridblocks[RowNo];
                int NoCols = GridRow.Count();
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                {
                    // Get a reference to the GridblockConfiguration object
                    GridblockConfiguration Gridblock = GridRow[ColNo];

                    // Check if it is null
                    if (Gridblock != null)
                    {
                        // Loop through every timestep and add it to the list
                        int NoTimesteps = Gridblock.TimestepEndTimes.Count() - 1;
                        totalNoCalculationElements += NoTimesteps;
                        for (int TimestepNo = 1; TimestepNo <= NoTimesteps; TimestepNo++)
                        {
                            GridblockTimestepControl nextTimestep = new GridblockTimestepControl(Gridblock, TimestepNo, Gridblock.TimestepEndTimes[TimestepNo]);
                            timestepList.Add(nextTimestep);
                        }
                    }
                }
            }

            // Sort the list
            timestepList.Sort();

            // Return the list of GridblockTimestepControl objects
            return timestepList;
        }
        /// <summary>
        /// Move through the GridblockTimestepControl list for a specified number of times, propagating the local DFNs
        /// </summary>
        /// <param name="currentCalculationElement">Index number of the first GridblockTimestepControl object to calculate; passed as a reference, and will be updated as the calculation is run</param>
        /// <param name="endCalculationElement">Index number of the final GridblockTimestepControl object to calculate</param>
        /// <param name="timestepList">Reference to a list of GridblockTimestepControl objects, each representing a calculation element</param>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        /// <returns>True if the end of the GridblockTimestepControl list has been reached, otherwise false</returns>
        private bool PropagateLocalDFNs(ref int currentCalculationElement, int endCalculationElement, List<GridblockTimestepControl> timestepList, IProgressReporterWrapper progressReporter)
        {
            // If the supplied progress reporter is null, create a new DefaultProgressReporter object (this will not actually report any progress)
            if (progressReporter == null)
                progressReporter = new DefaultProgressReporter();

            // Get mininum layer thickness cutoff
            double minLayerThickness = DFNControl.MinimumLayerThickness;

            // Get the total number of calculation elements in the GridblockTimestepControl list, and adjust the endCalculationElement if it is greater
            int lastCalculationElement = timestepList.Count - 1;
            if (endCalculationElement > lastCalculationElement) endCalculationElement = lastCalculationElement;

            // If the endCalculationElement is set to -1, set it to the last calculation element in the list
            if (endCalculationElement < 0) endCalculationElement = lastCalculationElement;

            // Loop through each gridblock timestep propagating the local DFNs
            double currentTime = -1;
            for (; currentCalculationElement <= endCalculationElement; currentCalculationElement++)
            {
                GridblockTimestepControl nextTimestep = timestepList[currentCalculationElement];

                // Check if calculation has been aborted
                if (progressReporter.abortCalculation())
                {
                    // Clean up any resources or data
                    break;
                }

                // Update the current time
                currentTime = nextTimestep.EndTimestepTime;

                // Run calculation - if the gridblock thickness is greater than the minimum cutoff
                if (nextTimestep.Gridblock.ThicknessAtDeformation > minLayerThickness)
                    nextTimestep.Gridblock.PropagateDFN(CurrentDFN, DFNControl);
                else
                    DFNThicknessCutoffActivated = true;

                // Update progress
                progressReporter.UpdateProgress(currentCalculationElement);
            }

            // If we have run any calculations, regenerate the fractures in the global DFN
            if (currentTime >= 0)
                CurrentDFN.updateDFN(currentTime);

            // Return true if we have reached the end of the GridblockTimestepControl list, otherwise return false
            return (currentCalculationElement > lastCalculationElement);
        }
        /// <summary>
        /// Move through the GridblockTimestepControl list until a specified end time is reached, propagating the local DFNs
        /// </summary>
        /// <param name="currentCalculationElement">Index number of the first GridblockTimestepControl object to calculate; passed as a reference, and will be updated as the calculation is run</param>
        /// <param name="endTime">End time of the final GridblockTimestepControl object to calculate</param>
        /// <param name="timestepList">Reference to a list of GridblockTimestepControl objects, each representing a calculation element</param>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        /// <returns>True if the end of the GridblockTimestepControl list has been reached, otherwise false</returns>
        private bool PropagateLocalDFNs(ref int currentCalculationElement, double endTime, List<GridblockTimestepControl> timestepList, IProgressReporterWrapper progressReporter)
        {
            // If the supplied progress reporter is null, create a new DefaultProgressReporter object (this will not actually report any progress)
            if (progressReporter == null)
                progressReporter = new DefaultProgressReporter();

            // Get mininum layer thickness cutoff
            double minLayerThickness = DFNControl.MinimumLayerThickness;

            // Get the total number of calculation elements in the GridblockTimestepControl list
            int lastCalculationElement = timestepList.Count - 1;

            // Loop through each gridblock timestep propagating the local DFNs
            double currentTime = -1;
            for (; currentCalculationElement <= lastCalculationElement; currentCalculationElement++)
            {
                GridblockTimestepControl nextTimestep = timestepList[currentCalculationElement];

                // Check if calculation has been aborted
                if (progressReporter.abortCalculation())
                {
                    // Clean up any resources or data
                    break;
                }

                // If we have exceeded the specified end time, break out of the loop, otherwise update the current time
                if (nextTimestep.EndTimestepTime > endTime)
                    break;
                else
                    currentTime = nextTimestep.EndTimestepTime;

                // Run calculation - if the gridblock thickness is greater than the minimum cutoff
                if (nextTimestep.Gridblock.ThicknessAtDeformation > minLayerThickness)
                    nextTimestep.Gridblock.PropagateDFN(CurrentDFN, DFNControl);
                else
                    DFNThicknessCutoffActivated = true;

                // Update progress
                progressReporter.UpdateProgress(currentCalculationElement);
            }

            // If we have run any calculations, regenerate the fractures in the global DFN
            if (currentTime >= 0)
                CurrentDFN.updateDFN(endTime);

            // Return true if we have reached the end of the GridblockTimestepControl list, otherwise return false
            return (currentCalculationElement > lastCalculationElement);
        }
        /// <summary>
        /// Generate a list of timestep end times for all timesteps in every gridblock in the grid
        /// </summary>
        /// <returns>New list of doubles representing the end times of all timesteps in every gridblock in the grid, in order</returns>
        public List<double> GetTimestepEndtimeList()
        {
            // Create a new list of doubles for the output
            List<double> timestepList = new List<double>();

            // Loop through every gridblock in every row in the grid
            int NoRows = Gridblocks.Count();
            for (int RowNo = 0; RowNo < NoRows; RowNo++)
            {
                List<GridblockConfiguration> GridRow = Gridblocks[RowNo];
                int NoCols = GridRow.Count();
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                {
                    // Get a reference to the GridblockConfiguration object
                    GridblockConfiguration Gridblock = GridRow[ColNo];

                    // Check if it is null
                    if (Gridblock != null)
                    {
                        // Loop through every timestep and add it to the list
                        int NoTimesteps = Gridblock.TimestepEndTimes.Count() - 1;
                        for (int TimestepNo = 1; TimestepNo <= NoTimesteps; TimestepNo++)
                        {
                            timestepList.Add(Gridblock.TimestepEndTimes[TimestepNo]);
                        }
                    }
                }
            }

            // Sort the list
            timestepList.Sort();

            // Return the list of GridblockTimestepControl objects
            return timestepList;
        }
        /// <summary>
        /// Class used to collate calculation timesteps from all gridblocks and compare them by end time
        /// </summary>
        private class GridblockTimestepControl : IComparable<GridblockTimestepControl>
        {
            /// <summary>
            /// Reference to relevant GridblockConfiguration object
            /// </summary>
            public GridblockConfiguration Gridblock;
            /// <summary>
            /// Timestep number
            /// </summary>
            public int TimestepNo;
            /// <summary>
            /// End time
            /// </summary>
            public double EndTimestepTime;

            // Control and implementation functions
            /// <summary>
            /// Compare GridblockTimestepControl objects based on time
            /// </summary>
            /// <param name="that">GridblockTimestepControl object to compare with</param>
            /// <returns>Positive if this is the latest GridblockTimestepControl object, negative if that is the latest GridblockTimestepControl object, zero if they have the same end time</returns>
            public int CompareTo(GridblockTimestepControl that)
            {
                return this.EndTimestepTime.CompareTo(that.EndTimestepTime);
            }

            // Constructors
            /// <summary>
            /// Constructor: specify GridblockCOnfiguration object, timestep number and end time
            /// </summary>
            /// <param name="Gridblock_in">Reference to GridblockCOnfiguration object</param>
            /// <param name="TimestepNo_in">Timestep number</param>
            /// <param name="EndTimestepTime_in">End time (s)</param>
            public GridblockTimestepControl(GridblockConfiguration Gridblock_in, int TimestepNo_in, double EndTimestepTime_in)
            {
                // Reference to relevant GridblockConfiguration object
                Gridblock = Gridblock_in;
                // Timestep number
                TimestepNo = TimestepNo_in;
                // End time
                EndTimestepTime = EndTimestepTime_in;
            }
        }

        // Reset and data input functions
        /// <summary>
        /// Add a GridblockConfiguration object to a specified cell in the grid and set up references to adjecent cells. The specified cell in the grid must already exist.
        /// </summary>
        /// <param name="gridblock_in">GridblockConfiguration object to add to the grid</param>
        /// <param name="RowNo">Row number of cell to place it in (zero referenced)</param>
        /// <param name="ColNo">Column number of cell to place it in (zero referenced)</param>
        public void AddGridblock(GridblockConfiguration gridblock_in, int RowNo, int ColNo)
        {
            AddGridblock(gridblock_in, RowNo, ColNo, true, true, true, true);
        }
        /// <summary>
        /// Add a GridblockConfiguration object to a specified cell in the grid, and set up references to adjecent cells if required. The specified cell in the grid must already exist.
        /// </summary>
        /// <param name="gridblock_in">GridblockConfiguration object to add to the grid</param>
        /// <param name="RowNo">Row number of cell to place it in (zero referenced)</param>
        /// <param name="ColNo">Column number of cell to place it in (zero referenced)</param>
        /// <param name="connectToWesternNeighbour">Flag to connect to western neighbouring gridblock; if false, the western corners of this gridblock will not match the eastern corners of the neighbouring gridblock</param>
        /// <param name="ConnectToSouthernNeighbour">Flag to connect to southern neighbouring gridblock; if false, the southern corners of this gridblock will not match the northern corners of the neighbouring gridblock</param>
        /// <param name="connectToEasternNeighbour">Flag to connect to eastern neighbouring gridblock; if false, the eastern corners of this gridblock will not match the western corners of the neighbouring gridblock</param>
        /// <param name="ConnectToNorthernNeighbour">Flag to connect to northern neighbouring gridblock; if false, the northern corners of this gridblock will not match the southern corners of the neighbouring gridblock</param>
        public void AddGridblock(GridblockConfiguration gridblock_in, int RowNo, int ColNo, bool connectToWesternNeighbour, bool ConnectToSouthernNeighbour, bool connectToEasternNeighbour, bool ConnectToNorthernNeighbour)
        {
            // Check to see if the row number is within the bounds of the grid
            if (RowNo < 0) RowNo = 0;
            if (RowNo >= Gridblocks.Count()) RowNo = Gridblocks.Count() - 1;

            // Get the row object
            List<GridblockConfiguration> GridRow = Gridblocks[RowNo];

            // Check to see if the column number is within the bounds of the grid
            if (ColNo < 0) ColNo = 0;
            if (ColNo >= GridRow.Count()) ColNo = GridRow.Count() - 1;

            // Add the GridblockConfiguration object to the grid and set the parent reference in the GridblockConfiguration object
            GridRow[ColNo] = gridblock_in;
            gridblock_in.setParentGrid(this);

            // Set the references to the adjacent gridblocks and cornerpoints: cornerpoints always reference to the southern and western neighbour gridblocks

            // Check if there is a cell to the west
            if (connectToWesternNeighbour)
            {
                if ((ColNo > 0) && (GridRow[ColNo - 1] != null))
                {
                    // Get reference to western neighbour gridblock
                    GridblockConfiguration W_neighbour = GridRow[ColNo - 1];

                    // Set mutual references to neighbouring gridblocks
                    W_neighbour.NeighbourGridblocks[GridDirection.E] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.W] = W_neighbour;

                    // Overwrite the western cornerpoints with those of the western neighbour
                    gridblock_in.OverwriteGridblockCorners(GridDirection.W, W_neighbour.SEtop, W_neighbour.SEbottom, W_neighbour.NEtop, W_neighbour.NEbottom);
                }
            }

            // Check if there is a cell to the south
            if (ConnectToSouthernNeighbour)
            {
                if (RowNo > 0)
                {
                    List<GridblockConfiguration> RowToS = Gridblocks[RowNo - 1];
                    if ((ColNo < RowToS.Count()) && (RowToS[ColNo] != null))
                    {
                        // Get reference to southern neighbour gridblock
                        GridblockConfiguration S_neighbour = RowToS[ColNo];

                        // Set mutual references to neighbouring gridblocks
                        S_neighbour.NeighbourGridblocks[GridDirection.N] = gridblock_in;
                        gridblock_in.NeighbourGridblocks[GridDirection.S] = S_neighbour;

                        // Overwrite the southern cornerpoints with those of the southern neighbour
                        gridblock_in.OverwriteGridblockCorners(GridDirection.S, S_neighbour.NEtop, S_neighbour.NEbottom, S_neighbour.NWtop, S_neighbour.NWbottom);
                    }
                }
            }

            // Check if there is a cell to the east
            if (connectToEasternNeighbour)
            {
                if ((ColNo < GridRow.Count() - 1) && (GridRow[ColNo + 1] != null))
                {
                    // Get reference to eastern neighbour gridblock
                    GridblockConfiguration E_neighbour = GridRow[ColNo + 1];

                    // Set mutual references to neighbouring gridblocks
                    E_neighbour.NeighbourGridblocks[GridDirection.W] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.E] = E_neighbour;

                    // Overwrite the western cornerpoints of the eastern neighbour with the eastern cornerpoints of this gridblock
                    E_neighbour.OverwriteGridblockCorners(GridDirection.W, gridblock_in.SEtop, gridblock_in.SEbottom, gridblock_in.NEtop, gridblock_in.NEbottom);
                }
            }

            // Check if there is a cell to the north
            if (ConnectToNorthernNeighbour)
            {
                if (RowNo < Gridblocks.Count - 1)
                {
                    List<GridblockConfiguration> RowToN = Gridblocks[RowNo + 1];
                    if ((ColNo < RowToN.Count()) && (RowToN[ColNo] != null))
                    {
                        // Get reference to northern neighbour gridblock
                        GridblockConfiguration N_neighbour = RowToN[ColNo];

                        // Set mutual references to neighbouring gridblocks
                        N_neighbour.NeighbourGridblocks[GridDirection.S] = gridblock_in;
                        gridblock_in.NeighbourGridblocks[GridDirection.N] = N_neighbour;

                        // Overwrite the southern cornerpoints of the northern neighbour with the northern cornerpoints of this gridblock
                        N_neighbour.OverwriteGridblockCorners(GridDirection.S, gridblock_in.NEtop, gridblock_in.NEbottom, gridblock_in.NWtop, gridblock_in.NWbottom);
                    }
                }
            }
        }
        /// <summary>
        /// Random number generator for creating fracture nucleation points
        /// </summary>
        public Random RandomNumberGenerator;

        // Constructors
        /// <summary>
        /// Constructor - create an MxN FractureGrid and fill with null objects
        /// </summary>
        /// <param name="NoRows"></param>
        /// <param name="NoCols"></param>
        public FractureGrid(int NoRows, int NoCols) : this()
        {
            for (int RowNo = 1; RowNo <= NoRows; RowNo++)
            {
                List<GridblockConfiguration> GridRow = new List<GridblockConfiguration>();

                for (int ColNo = 1; ColNo <= NoCols; ColNo++)
                    GridRow.Add(null);

                Gridblocks.Add(GridRow);
            }
        }
        /// <summary>
        /// Default constructor - create an empty FractureGrid object and an empty DFN object
        /// </summary>
        public FractureGrid()
        {
            // Create an empty grid object
            Gridblocks = new List<List<GridblockConfiguration>>();

            // Create a new DFN control object
            DFNControl = new DFNGenerationControl();

            // Create an empty global DFN object
            CurrentDFN = new GlobalDFN(this);

            // Create an empty list of GlobalDFN objects representing the DFN at intermediate stages of fracture propagation
            DFNGrowthStages = new List<GlobalDFN>();

            // Initialise random number generator for placing fractures
            RandomNumberGenerator = new Random();

            // Initialise flag to indicate that the explicit DFN was not generated in some gridblocks, due to the layer tthickness cutoff
            DFNThicknessCutoffActivated = false;
    }
}
}
