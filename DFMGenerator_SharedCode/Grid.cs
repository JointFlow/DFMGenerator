// Set this flag to output a list of gridblocks when calculating the implicit and explicit fracture populations
// Use for debugging only; will significantly increase runtime
//#define LOGGRIDBLOCKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Class representing the entire fractured layer, containing a 2D array of gridblocks
    /// </summary>
    class FractureGrid
    {
        /// <summary>
        /// Program name and version number (hard coded)
        /// </summary>
        public static string VersionNumber { get { return "DFMGenerator v4.0.0"; } }

        // Grid data
        /// <summary>
        /// 3D array containing GridblockConfiguration objects for each cell in the grid
        /// </summary>
        private GridblockConfiguration[,,] Gridblocks;
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
        /// <param name="LayerNo">Layer number of gridblock to retrieve</param>
        /// <returns>Reference to the specified GridblockConfiguration object</returns>
        public GridblockConfiguration GetGridblock(int RowNo, int ColNo, int LayerNo)
        {
            return Gridblocks[RowNo, ColNo, LayerNo];
        }

        // Objects containing geomechanical, fracture property and calculation data relating to the grid
        /// <summary>
        /// Control data for generating the DFN 
        /// </summary>
        public DFNGenerationControl DFNControl;
        /// <summary>
        /// Counter to indicate the number of gridblock in which the implicit calculation hits the timestep limit before running to completion
        /// </summary>
        public int HitTimestepLimit { get; private set; }
        /// <summary>
        /// Counter for exceptions thrown when calculating implicit fracture data
        /// </summary>
        public int ImplicitCalculationException { get; private set; }
        /// <summary>
        /// Counter to indicate the number of gridblocks in which the explicit DFN is not generated due to the layer thickness cutoff
        /// </summary>
        public int DFNThicknessCutoffActivated { get; private set; }
        /// <summary>
        /// Counter to indicate the number of errors encountered because the gridblock geometry is not correctly defined
        /// This will be greater than the number of gridblocks in which this error is encountered, as the counter will be incremented for every gridblock timestep
        /// </summary>
        public int DFNGeometricErrors { get; private set; }
        /// <summary>
        /// Counter to indicate the number of errors encountered because the driving stress or propagation distance is not correctly defined
        /// This will be greater than the number of gridblocks in which this error is encountered, as the counter will be incremented for every gridblock timestep
        /// </summary>
        public int DFNStressErrors { get; private set; }
        /// <summary>
        /// Counter to indicate the number of errors encountered because the fracture nucleation limit is hit
        /// This will be greater than the number of gridblocks in which this error is encountered, as the counter will be incremented for every gridblock timestep
        /// </summary>
        public int DFNFractureLimitErrors { get; private set; }
        /// <summary>
        /// Counter for exceptions thrown when calculating explicit DFN data
        /// </summary>
        public int ExplicitCalculationException { get; private set; }

        // Functions to calculate fracture population data and generate the DFN
        /// <summary>
        /// Calculate fracture data for each gridblock in the grid based on existing GridblockConfiguration.PropagationControl objects, without updating progress
        /// </summary>
        public void CalculateAllFractureData()
        {
            CalculateFractureDataInStratigraphicInterval(-1, -1, null);
        }
        /// <summary>
        /// Calculate fracture data for each gridblock in the grid based on existing GridblockConfiguration.PropagationControl objects 
        /// </summary>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        public void CalculateAllFractureData(IProgressReporterWrapper progressReporter)
        {
            CalculateFractureDataInStratigraphicInterval(-1, -1, progressReporter);
        }
        /// <summary>
        /// Calculate fracture data for each gridblock in a single specified layer based on existing GridblockConfiguration.PropagationControl objects, without updating progress
        /// </summary>
        /// <param name="LayerNo">Index of the layer to calculate</param>
        public void CalculateFractureDataInLayer(int LayerNo)
        {
            CalculateFractureDataInStratigraphicInterval(LayerNo, LayerNo, null);
        }
        /// <summary>
        /// Calculate fracture data for each gridblock in a single specified layer based on existing GridblockConfiguration.PropagationControl objects
        /// </summary>
        /// <param name="LayerNo">Index of the layer to calculate</param>
        public void CalculateFractureDataInLayer(int LayerNo, IProgressReporterWrapper progressReporter)
        {
            CalculateFractureDataInStratigraphicInterval(LayerNo, LayerNo, progressReporter);
        }
        /// <summary>
        /// Calculate fracture data for each gridblock in a specified stratigraphic interval based on existing GridblockConfiguration.PropagationControl objects 
        /// </summary>
        /// <param name="TopLayerNo">Index of the uppermost layer to calculate</param>
        /// <param name="BottomLayerNo">Index of the lowermost layer to calculate</param>
        /// <param name="progressReporter">Reference to a progress reporter - can be any object implementing the IProgressReporterWrapper interface</param>
        private void CalculateFractureDataInStratigraphicInterval(int TopLayerNo, int BottomLayerNo, IProgressReporterWrapper progressReporter)
        {
#if LOGGRIDBLOCKS
            // If the output folder does not exist, create it
            string logFolderPath = DFNControl.FolderPath;
            if (!Directory.Exists(logFolderPath))
                Directory.CreateDirectory(logFolderPath);
            // Open the log file
            string logFileName = string.Format("ImplicitCalculation_TopLayer{0}_LogFile.txt", TopLayerNo);
            String logFileNameComb = logFolderPath + logFileName;
            StreamWriter logFile = new StreamWriter(logFileNameComb);
#endif

            // Reset the counter for the number of gridblocks in which the timestep limit is hit and the number of exceptions thrown
            HitTimestepLimit = 0;
            ImplicitCalculationException = 0;

            // Get the total number of grid rows and columns
            int NoRows = Gridblocks.GetLength(0);
            int NoCols = Gridblocks.GetLength(1);

            // If the top or bottom layer are out of the grid range, set them to the top and bottom layer of the grid respectively
            int NoLayers = Gridblocks.GetLength(2);
            if (TopLayerNo < 0)
                TopLayerNo = 0;
            if ((BottomLayerNo < 0) || (BottomLayerNo >= NoLayers))
                BottomLayerNo = NoLayers - 1;
            NoLayers = (BottomLayerNo - TopLayerNo) + 1;

            // If the supplied progress reporter is null, create a new DefaultProgressReporter object (this will not actually report any progress)
            // Otherwise calculate the number of gridblocks and set the total number of calculation elements (= number of gridblocks)
            if (progressReporter == null)
            {
                progressReporter = new DefaultProgressReporter();
            }
            else
            {
                // Calculate total number of gridblocks
                int TotalNoGridblocksInLayer = NoRows * NoCols * NoLayers;

                // Set the total number of calculation elements in the progress reporter
                progressReporter.SetNumberOfElements(TotalNoGridblocksInLayer);
            }

            // Loop through every gridblock in the grid
            int NoGridblocksCalculated = 0;
            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                    for (int LayerNo = TopLayerNo; LayerNo < NoLayers; LayerNo++)
                    {
                        // Check if calculation has been aborted
                        if (progressReporter.abortCalculation())
                        {
                            // Clean up any resources or data
                            break;
                        }

                        // Get a reference to the GridblockConfiguration object
                        GridblockConfiguration Gridblock = Gridblocks[RowNo, ColNo, LayerNo];

                        // Check if it is null
                        if (Gridblock != null)
                        {
#if !DEBUG
                            try
#endif
                            {
#if LOGGRIDBLOCKS
                                // Write next gridblock details to logfile
                                string nextGridBlockLabel = string.Format("Launching Gridblock.CalculateFractureData() for gridblock {0},{1},{2} at real time {3}", ColNo, RowNo, LayerNo, DateTime.Now);
                                logFile.WriteLine(nextGridBlockLabel);
#endif
                                // Run the calculation function for the specified gridblock and get the return code
                                CalculateFractureDataReturnCode calculateDataReturnCode = Gridblock.CalculateFractureData();
                                if (calculateDataReturnCode == CalculateFractureDataReturnCode.TimestepLimitExceeded)
                                    HitTimestepLimit++;
                            }
#if !DEBUG
                            catch (Exception e)
                            {
                                progressReporter.OutputMessage(string.Format("Error in creating implicit fracture model in the gridblock {0},{1},{2}", ColNo, RowNo, LayerNo));
                                progressReporter.OutputMessage(string.Format("Error: {0}", e.Message));
                                //progressReporter.OutputMessage(e.StackTrace);
                                ImplicitCalculationException++;
                            }
#endif
                        }

                        // Update progress
                        progressReporter.UpdateProgress(++NoGridblocksCalculated);
                    }

            // Set the flag to indicate if the implicit calculation hit the timestep limit in any gridblock
            if (HitTimestepLimit > 0)
            {
                string timestepLimitMessage = string.Format("Timestep limit was reached in {0} out of {1} gridblocks.", HitTimestepLimit, NoGridblocksCalculated);
                progressReporter.OutputMessage(timestepLimitMessage);
            }

#if LOGGRIDBLOCKS
            // Close the log file
            logFile.Close();
#endif
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

            // Generate a list of timestep end times for all timesteps in every gridblock in the grid
            int totalNoCalculationElements, noGridblocksBelowThicknessCutoff;
            List<GridblockTimestepControl> timestepList = GetTimestepList(out totalNoCalculationElements, out noGridblocksBelowThicknessCutoff);

            // Reset the error counters
            DFNThicknessCutoffActivated = noGridblocksBelowThicknessCutoff;
            DFNGeometricErrors = 0;
            DFNStressErrors = 0;
            DFNFractureLimitErrors = 0;
            ExplicitCalculationException = 0;

            // Create a counter for the number of fractures in the latest DFN
            int noDFNFractures = 0;

            // If the supplied progress reporter is null, create a new DefaultProgressReporter object (this will not actually report any progress)
            // Otherwise set the total number of calculation elements in the progress reporter
            if (progressReporter == null)
                progressReporter = new DefaultProgressReporter();
            else
                progressReporter.SetNumberOfElements(totalNoCalculationElements);

            // Output an error message if the explicit DFN is not generated in some gridblocks, due to the layer thickness cutoff
            if (DFNThicknessCutoffActivated > 0)
            {
                // Give a message saying how many gridblocks the explicit DFN will not be generated in due to the layer thickness being less than the minimum cutoff
                string thicknessLimitMessage = string.Format("The explicit DFN will not be generated in {0} gridblocks due to the layer thickness cutoff. To prevent this, reduce the cutoff value in the Control Parameters.", DFNThicknessCutoffActivated);
                progressReporter.OutputMessage(thicknessLimitMessage);
            }

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
                    double nextListValue = DFNControl.GetIntermediateOutputTime(nextStage - 1); // List of intermediate outputs is zero-based
                    double nextIntermediateEndTime = (!double.IsNaN(nextListValue) ? nextListValue : endTime); // If the next list value is NaN (i.e. we have reached the end of the list), used the end time instead
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
                double minArea = 0;
                latestDFN.removeShortestFractures(minRadius, minLength, minArea, DFNControl.MaxNoFractures);

                // Update the counter for the number of fractures in the DFN
                noDFNFractures = latestDFN.NoDFNFractures;

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
                    string fractureFileExtension;
                    switch (DFNControl.OutputFileType)
                    {
                        case DFNFileType.ASCII:
                            fractureFileExtension = ".txt";
                            break;
                        case DFNFileType.FAB:
                            fractureFileExtension = ".FAB";
                            break;
                        default:
                            fractureFileExtension = "";
                            break;
                    }

                    // Write microfracture data to file
                    if (latestDFN.GlobalDFNMicrofractures.Count > 0)
                    {
                        // Create file for microfractures
                        string fileName = "Microfractures_" + outputLabel + fractureFileExtension;
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
                                        MicrofractureXYZ frac = latestDFN.GlobalDFNMicrofractures[uFracNo];
                                        double aperture = frac.MeanAperture;
                                        double permeability = Math.Pow(aperture, 2) / 12;
                                        double compressibility = frac.Compressibility;
                                        if (double.IsNaN(compressibility))
                                            compressibility = DFNControl.DefaultFractureCompressibility;

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
                                break;
                            default:
                                break;
                        }

                        // Close microfracture output file
                        uF_outputFile.Close();
                    }

                    // Write macrofracture data to file
                    if (latestDFN.GlobalDFNMacrofractures.Count > 0)
                    {
                        // Create output file for macrofractures
                        string fileName = "Macrofractures_" + outputLabel + fractureFileExtension;
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

                                    // Loop through each macrofracture and count the total number of segments and cornerpoints, excluding zero length segments
                                    foreach (MacrofractureXYZ frac in latestDFN.GlobalDFNMacrofractures)
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
                                    foreach (MacrofractureXYZ MF in latestDFN.GlobalDFNMacrofractures)
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
                                                double compressibility = MF.SegmentCompressibility[dir][MF_segmentNo];
                                                if (double.IsNaN(compressibility))
                                                    compressibility = DFNControl.DefaultFractureCompressibility;

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
                                break;
                            default:
                                break;
                        }

                        // Close macrofracture  output file
                        MF_outputFile.Close();
                    }

                    // Write unconfined fracture data to file
                    if (latestDFN.GlobalDFNUnconfinedFractures.Count > 0)
                    {
                        // Create file for unconfined fractures
                        string fileName = "UnconfinedFractures_" + outputLabel + fractureFileExtension;
                        String namecomb = DFNControl.FolderPath + fileName;
                        StreamWriter UCF_outputFile = new StreamWriter(namecomb);

                        switch (DFNControl.OutputFileType)
                        {
                            case DFNFileType.ASCII:
                                {
                                    // Write header data
                                    string FSheader1 = "FracNo\tSet\tCentre X\tCentre Y\tCentre Depth\tRadius\tDip\tAzimuth\tActive\t";
                                    UCF_outputFile.WriteLine(FSheader1);

                                    // Loop through each unconfined fracture and write data to logfile
                                    foreach (UnconfinedFractureXYZ frac in latestDFN.GlobalDFNUnconfinedFractures)
                                    {
                                        PointXYZ centroid = frac.Centroid;
                                        string data = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t", frac.UnconfinedFractureID, frac.SetIndex, centroid.X, centroid.Y, centroid.Depth, frac.EffectiveRadius, frac.Dip, frac.Azimuth, !frac.FullyDeactivated);
                                        UCF_outputFile.WriteLine(data);

                                        // Write output data for each of the fracture rays
                                        UCF_outputFile.WriteLine("Start Rays");
                                        string rayHeader = string.Format("Tip X\tTip Y\tTip Depth\tRay Length\tTip Type\tTerminating Fracture\t");
                                        UCF_outputFile.WriteLine(rayHeader);

                                        // Loop through the rays and write the output data (including tip coordinates) to file
                                        List<UnconfinedFractureRay> rays = frac.GetRays();
                                        foreach (UnconfinedFractureRay ray in rays)
                                        {
                                            string rayOutputData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t", ray.OuterTip.X, ray.OuterTip.Y, ray.OuterTip.Depth, ray.Length, ray.TipType, ray.TerminatingFracture);
                                            UCF_outputFile.WriteLine(rayOutputData);
                                        }

                                        // Add only the tip coordinates for the first ray to the end of the list to close the loop
                                        if (rays.Count > 0)
                                        {
                                            UnconfinedFractureRay ray = rays[0];
                                            string rayOutputData = string.Format("{0}\t{1}\t{2}\t", ray.OuterTip.X, ray.OuterTip.Y, ray.OuterTip.Depth);
                                            UCF_outputFile.WriteLine(rayOutputData);
                                        }

                                        UCF_outputFile.WriteLine("End Rays");
                                    }
                                }
                                break;
                            case DFNFileType.FAB:
                                {
                                    int No_UCFracs = latestDFN.NoUnconfinedFractureElements();
                                    int noElementCornerpoints = 3; // We are using triangular elements
                                    int No_Nodes = No_UCFracs * noElementCornerpoints;

                                    // Write general fracture FAB header data to logfile
                                    string FAB_header_1 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}\r\n{4}", "BEGIN FORMAT", "Format = Ascii", "Length_Unit = M", "XAxis = East", "Scale = 8124.44");
                                    UCF_outputFile.WriteLine(FAB_header_1);
                                    string FAB_header5 = string.Format("{0} {1}", "No_Fractures =", No_UCFracs);
                                    string FAB_header6 = string.Format("No_TessFractures = 0");
                                    string FAB_header7 = string.Format("{0} {1}", "No_Nodes = ", No_Nodes);
                                    UCF_outputFile.WriteLine(FAB_header5);
                                    UCF_outputFile.WriteLine(FAB_header6);
                                    UCF_outputFile.WriteLine(FAB_header7);

                                    string FAB_header_3 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}\r\n", "No_RockBlocks = 0", "No_NodesRockBlock = 0", "No_Properties = 3", "END FORMAT");
                                    UCF_outputFile.WriteLine(FAB_header_3);
                                    string FAB_header_4 = string.Format("{0}\r\n{1}\r\n{2}\r\n{3}", "BEGIN PROPERTIES", "Prop1    =    (Real*4) \"Permeability\"", "Prop2    =    (Real*4) \"Compressibility\"", "Prop3    =    (Real*4) \"Aperture\"");
                                    UCF_outputFile.WriteLine(FAB_header_4);
                                    string FAB_header_5 = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n{3}\r\n\r\n{4}", "END PROPERTIES", "BEGIN SETS", "Set1    =    \"Discrete fractures\"", "END SETS", "BEGIN FRACTURE");
                                    UCF_outputFile.WriteLine(FAB_header_5);

                                    // Loop through each unconfined fracture and write data to logfile
                                    int UCFelementNo = 1;
                                    for (int UCFracNo = 0; UCFracNo < No_UCFracs; UCFracNo++)
                                    {
                                        UnconfinedFractureXYZ frac = latestDFN.GlobalDFNUnconfinedFractures[UCFracNo];
                                        double aperture = 0;// frac.MeanAperture;
                                        double permeability = 0;// Math.Pow(aperture, 2) / 12;
                                        double compressibility = 0;// frac.Compressibility;
                                        if (double.IsNaN(compressibility))
                                            compressibility = DFNControl.DefaultFractureCompressibility;

                                        // Get a list of triangular elements comprising this fracture
                                        List<PointXYZ[]> elements = frac.GetTriangularFractureSegmentsInXYZ();

                                        // Loop through each element in the list
                                        // Each element will be output as a separate fracture
                                        foreach (PointXYZ[] element in elements)
                                        {
                                            string data = string.Format("{0} {1} {2} {3} {4} {5}", UCFelementNo++, noElementCornerpoints, 1, permeability, compressibility, aperture);
                                            UCF_outputFile.WriteLine(data);

                                            // Loop through each cornerpoint and write the coordinates to file
                                            int pointNo = 1;
                                            foreach (PointXYZ cornerPoint in element)
                                            {
                                                string pointCoords = string.Format("{0} {1} {2} {3}", pointNo++, cornerPoint.X, cornerPoint.Y, cornerPoint.Z);
                                                UCF_outputFile.WriteLine(pointCoords);
                                            }

                                            VectorXYZ fractureNormal = frac.NormalVector;
                                            string lastLine = string.Format("{0} {1} {2} {3}", 0, fractureNormal.Component(VectorComponents.X), fractureNormal.Component(VectorComponents.Y), fractureNormal.Component(VectorComponents.Z));
                                            UCF_outputFile.WriteLine(lastLine);
                                        }
                                    }

                                    // Write FAB footer data to logfile
                                    string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                    UCF_outputFile.WriteLine(footer);
                                }
                                break;
                            default:
                                break;
                        }

                        // Close unconfined fracture output file
                        UCF_outputFile.Close();
                    }

                    // If required, write macrofracture centrepoints to file
                    if (outputCentrepoints)
                    {
                        string centrepointFileExtension;
                        switch (DFNControl.OutputFileType)
                        {
                            case DFNFileType.ASCII:
                                centrepointFileExtension = ".txt";
                                break;
                            case DFNFileType.FAB:
                                centrepointFileExtension = "";
                                break;
                            default:
                                centrepointFileExtension = "";
                                break;
                        }

                        // Write macrofracture data to file
                        if (latestDFN.GlobalDFNMacrofractures.Count > 0)
                        {
                            // Create output file for macrofracture centrepoints
                            string fileName = "MFCentrepoints_" + outputLabel + centrepointFileExtension;
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
                                            List<PointXYZ> CentrePoints = frac.SegmentCentrePoints;
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
                    }
                } // End write fracture data to file

                // Update the next output stage counter
                nextStage++;

            } // End loop through the intermediate DFNs

            // Output error messages if there were geometric or driving stress errors or the fracture nucleation limit was reached
            // To avoid excessive messages, we will not output a message saying that the DFN was not generated due to the layer thickness being less than the minimum cutoff, since this is a not an error
            if (DFNGeometricErrors > 0)
                progressReporter.OutputMessage(string.Format("There were errors in determining the geometry of one or more gridblock; no fractures were generated in those gridblocks"));
            if (DFNStressErrors > 0)
                progressReporter.OutputMessage(string.Format("There were errors in determining the fracture driving stress in one or more gridblock"));
            if (DFNFractureLimitErrors > 0)
                progressReporter.OutputMessage(string.Format("The fracture nucleation limit was exceeded in one or more gridblock"));
            // Output a message if the model ran to completion but no fractures were generated
            if (noDFNFractures == 0)
                progressReporter.OutputMessage(string.Format("The model ran to completion but no explicit DFN fractures were generated"));
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
        /// <param name="noGridblocksBelowThicknessCutoff">Reference parameter for the total number of gridblocks below the minimum thickness cutoff</param>
        /// <returns>New list of GridblockTimestepControl objects representing all timesteps in every gridblock in the grid</returns>
        private List<GridblockTimestepControl> GetTimestepList(out int totalNoCalculationElements, out int noGridblocksBelowThicknessCutoff)
        {
#if LOGGRIDBLOCKS
            // If the output folder does not exist, create it
            string logFolderPath = DFNControl.FolderPath;
            if (!Directory.Exists(logFolderPath))
                Directory.CreateDirectory(logFolderPath);
            // Open the log file
            string logFileName = string.Format("GridblockTimestepList_LogFile.txt");
            String logFileNameComb = logFolderPath + logFileName;
            StreamWriter logFile = new StreamWriter(logFileNameComb);
#endif
            // Create a new list of GridblockTimestepControl objects and set the total number of calculation elements (i.e. the number of GridblockTimestepControl objects) to 0
            totalNoCalculationElements = 0;
            List<GridblockTimestepControl> timestepList = new List<GridblockTimestepControl>();

            // Set the counter for the number of gridblocks below the minimum thickness cutoff, and get the specified cutoff
            noGridblocksBelowThicknessCutoff = 0;
            double gridblockThicknessCutoff = DFNControl.MinimumLayerThickness;

            // Get the total number of grid rows, columns and layers
            int NoRows = Gridblocks.GetLength(0);
            int NoCols = Gridblocks.GetLength(1);
            int NoLayers = Gridblocks.GetLength(2);

#if LOGGRIDBLOCKS
            // Write next gridblock details to logfile
            string gridblockCount = string.Format("NoRows: {0}, NoCols: {1}, NoLayers: {2}", NoRows, NoCols, NoLayers);
            logFile.WriteLine(gridblockCount);
#endif

            // Loop through every gridblock in the grid
            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                    for (int LayerNo = 0; LayerNo < NoLayers; LayerNo++)
                    {
                        // Get a reference to the GridblockConfiguration object
                        GridblockConfiguration Gridblock = Gridblocks[RowNo, ColNo, LayerNo];

                        // Check if it is null
                        if (Gridblock != null)
                        {
                            // Check if the gridblock is below the minimum thickness cutoff, and if so update the counter
                            // NB gridblocks below the thickness cutoff will still be added to the list, but will be filtered out later
                            if (Gridblock.ThicknessAtDeformation < gridblockThicknessCutoff)
                                noGridblocksBelowThicknessCutoff++;

                            // Loop through every timestep and add it to the list
                            int NoTimesteps = Gridblock.TimestepEndTimes.Count() - 1;
                            totalNoCalculationElements += NoTimesteps;
                            for (int TimestepNo = 1; TimestepNo <= NoTimesteps; TimestepNo++)
                            {
                                GridblockTimestepControl nextTimestep = new GridblockTimestepControl(Gridblock, TimestepNo, Gridblock.TimestepEndTimes[TimestepNo]);
                                timestepList.Add(nextTimestep);
                            }
#if LOGGRIDBLOCKS
                            // Write next gridblock details to logfile
                            string timestepCount = string.Format("Gridblock {0},{1},{2}: added {3} timesteps", RowNo, ColNo, LayerNo, NoTimesteps);
                            logFile.WriteLine(timestepCount);
#endif
                        }
                    }

            // Sort the list
            timestepList.Sort();

#if LOGGRIDBLOCKS
            // Close the log file
            logFile.Close();
#endif

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
#if LOGGRIDBLOCKS
            // If the output folder does not exist, create it
            string logFolderPath = DFNControl.FolderPath;
            if (!Directory.Exists(logFolderPath))
                Directory.CreateDirectory(logFolderPath);
            // Open the log file
            string logFileName = string.Format("ExplicitCalculation_LogFile_Element_{0}_to_{1}.txt", currentCalculationElement, endCalculationElement);
            String logFileNameComb = logFolderPath + logFileName;
            StreamWriter logFile = new StreamWriter(logFileNameComb);
#endif

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
#if !DEBUG
                try
#endif
                {
                    if (nextTimestep.Gridblock.ThicknessAtDeformation > minLayerThickness)
                    {
#if LOGGRIDBLOCKS
                        // Write next gridblock details to logfile
                        string nextGridBlockLabel = string.Format("Launching Gridblock.PropagateDFN() for gridblock at {0},{1},{2} TS {3} at real time {4}", nextTimestep.Gridblock.SWtop.X, nextTimestep.Gridblock.SWtop.Y, nextTimestep.Gridblock.SWtop.Z, nextTimestep.TimestepNo, DateTime.Now);
                        logFile.WriteLine(nextGridBlockLabel);
#endif
                        PropagateDFNReturnCode DFNReturnCode = nextTimestep.Gridblock.PropagateDFN(CurrentDFN, DFNControl);
                        switch (DFNReturnCode)
                        {
                            case PropagateDFNReturnCode.Completed:
                                break;
                            case PropagateDFNReturnCode.GridblockGeometryError:
                                DFNGeometricErrors++;
                                break;
                            case PropagateDFNReturnCode.DrivingStressError:
                                DFNStressErrors++;
                                break;
                            case PropagateDFNReturnCode.NewFractureLimitExceeded:
                                DFNFractureLimitErrors++;
                                break;
                            default:
                                break;
                        }
                    }
                }
#if !DEBUG
                catch (Exception e)
                {
                    progressReporter.OutputMessage(string.Format("Error in creating explicit DFN in gridblock at {0},{1},{2} TS {3}", nextTimestep.Gridblock.SWtop.X, nextTimestep.Gridblock.SWtop.Y, nextTimestep.Gridblock.SWtop.Z, nextTimestep.TimestepNo));
                    progressReporter.OutputMessage(string.Format("Error: {0}", e.Message));
                    //progressReporter.OutputMessage(e.StackTrace);
                    ExplicitCalculationException++;
                }
#endif

                // Update progress
                progressReporter.UpdateProgress(currentCalculationElement);
            }

            // If we have run any calculations, regenerate the fractures in the global DFN
            if (currentTime >= 0)
                CurrentDFN.updateDFN(currentTime);

#if LOGGRIDBLOCKS
            // Close the log file
            logFile.Close();
#endif

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
#if LOGGRIDBLOCKS
            // If the output folder does not exist, create it
            string logFolderPath = DFNControl.FolderPath;
            if (!Directory.Exists(logFolderPath))
                Directory.CreateDirectory(logFolderPath);
            // Open the log file
            string logFileName = string.Format("ExplicitCalculation_LogFile_Element_{0}_to_{1}s.txt", currentCalculationElement, endTime);
            String logFileNameComb = logFolderPath + logFileName;
            StreamWriter logFile = new StreamWriter(logFileNameComb);
#endif

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
#if !DEBUG
                try
#endif
                {
                    if (nextTimestep.Gridblock.ThicknessAtDeformation > minLayerThickness)
                    {
#if LOGGRIDBLOCKS
                        // Write next gridblock details to logfile
                        string nextGridBlockLabel = string.Format("Launching Gridblock.PropagateDFN() for gridblock at {0},{1} TS {2} at real time {3}", nextTimestep.Gridblock.SWtop.X, nextTimestep.Gridblock.SWtop.Y, nextTimestep.TimestepNo, DateTime.Now);
                        logFile.WriteLine(nextGridBlockLabel);
#endif
                        PropagateDFNReturnCode DFNReturnCode = nextTimestep.Gridblock.PropagateDFN(CurrentDFN, DFNControl);
                        switch (DFNReturnCode)
                        {
                            case PropagateDFNReturnCode.Completed:
                                break;
                            case PropagateDFNReturnCode.GridblockGeometryError:
                                DFNGeometricErrors++;
                                break;
                            case PropagateDFNReturnCode.DrivingStressError:
                                DFNStressErrors++;
                                break;
                            case PropagateDFNReturnCode.NewFractureLimitExceeded:
                                DFNFractureLimitErrors++;
                                break;
                            default:
                                break;
                        }
                    }
                }
#if !DEBUG
                catch (Exception e)
                {
                    progressReporter.OutputMessage(string.Format("Error in creating explicit DFN in the gridblock at {0},{1} TS {2}", nextTimestep.Gridblock.SWtop.X, nextTimestep.Gridblock.SWtop.Y, nextTimestep.TimestepNo));
                    progressReporter.OutputMessage(string.Format("Error: {0}", e.Message));
                    //progressReporter.OutputMessage(e.StackTrace);
                    ExplicitCalculationException++;
                }
#endif

                // Update progress
                progressReporter.UpdateProgress(currentCalculationElement);
            }

            // If we have run any calculations, regenerate the fractures in the global DFN
            if (currentTime >= 0)
                CurrentDFN.updateDFN(endTime);

#if LOGGRIDBLOCKS
            // Close the log file
            logFile.Close();
#endif

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

            // Get the total number of grid rows, columns and layers
            int NoRows = Gridblocks.GetLength(0);
            int NoCols = Gridblocks.GetLength(1);
            int NoLayers = Gridblocks.GetLength(2);

            // Loop through every gridblock in the grid
            for (int RowNo = 0; RowNo < NoRows; RowNo++)
                for (int ColNo = 0; ColNo < NoCols; ColNo++)
                    for (int LayerNo = 0; LayerNo < NoLayers; LayerNo++)
                    {
                        // Get a reference to the GridblockConfiguration object
                        GridblockConfiguration Gridblock = Gridblocks[RowNo, ColNo, LayerNo];

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
            // Data
            /// <summary>
            /// Reference to the relevant GridblockConfiguration object
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
        /// <param name="LayerNo">Layer number of cell to place it in (zero referenced)</param>
        public void AddGridblock(GridblockConfiguration gridblock_in, int RowNo, int ColNo, int LayerNo)
        {
            AddGridblock(gridblock_in, RowNo, ColNo, LayerNo, true, true, true, true, true, true);
        }
        /// <summary>
        /// Add a GridblockConfiguration object to a specified cell in the grid, and set up references to adjecent cells if required.
        /// </summary>
        /// <param name="gridblock_in">GridblockConfiguration object to add to the grid</param>
        /// <param name="RowNo">Row number of cell to place it in (zero referenced)</param>
        /// <param name="ColNo">Column number of cell to place it in (zero referenced)</param>
        /// <param name="LayerNo">Layer number of cell to place it in (zero referenced)</param>
        /// <param name="connectToWesternNeighbour">Flag to connect to western neighbouring gridblock; if false, the western corners of this gridblock will not match the eastern corners of the neighbouring gridblock</param>
        /// <param name="ConnectToSouthernNeighbour">Flag to connect to southern neighbouring gridblock; if false, the southern corners of this gridblock will not match the northern corners of the neighbouring gridblock</param>
        /// <param name="connectToEasternNeighbour">Flag to connect to eastern neighbouring gridblock; if false, the eastern corners of this gridblock will not match the western corners of the neighbouring gridblock</param>
        /// <param name="ConnectToNorthernNeighbour">Flag to connect to northern neighbouring gridblock; if false, the northern corners of this gridblock will not match the southern corners of the neighbouring gridblock</param>
        /// <param name="ConnectToUnderlyingNeighbour">Flag to connect to underlying neighbouring gridblock; if false, the bottom corners of this gridblock will not match the top corners of the underlying gridblock</param>
        /// <param name="connectToOverlyingNeighbour">Flag to connect to overlying neighbouring gridblock; if false, the top corners of this gridblock will not match the bottom corners of the overlying gridblock</param>
        public void AddGridblock(GridblockConfiguration gridblock_in, int RowNo, int ColNo, int LayerNo, bool connectToWesternNeighbour, bool ConnectToSouthernNeighbour, bool connectToEasternNeighbour, bool ConnectToNorthernNeighbour, bool ConnectToUnderlyingNeighbour, bool connectToOverlyingNeighbour)
        {
            // Get the total number of grid rows, columns and layers
            int NoRows = Gridblocks.GetLength(0);
            int NoCols = Gridblocks.GetLength(1);
            int NoLayers = Gridblocks.GetLength(2);

            // Check to see if the row number is within the bounds of the grid
            if (RowNo < 0) RowNo = 0;
            if (RowNo >= NoRows) RowNo = NoRows - 1;

            // Check to see if the column number is within the bounds of the grid
            if (ColNo < 0) ColNo = 0;
            if (ColNo >= NoCols) ColNo = NoCols - 1;

            // Check to see if the layer number is within the bounds of the grid
            if (LayerNo < 0) LayerNo = 0;
            if (LayerNo >= NoLayers) LayerNo = NoLayers - 1;

            // Add the GridblockConfiguration object to the grid and set the parent reference in the GridblockConfiguration object
            Gridblocks[RowNo, ColNo, LayerNo] = gridblock_in;
            gridblock_in.setParentGrid(this);

            // Set the references to the adjacent gridblocks and cornerpoints: cornerpoints always reference to the southern, western and underlying neighbour gridblocks

            // Check if there is a gridblock to the west
            if (connectToWesternNeighbour)
            {
                if ((ColNo > 0) && (Gridblocks[RowNo, ColNo - 1, LayerNo] != null))
                {
                    // Get reference to western neighbour gridblock
                    GridblockConfiguration W_neighbour = Gridblocks[RowNo, ColNo - 1, LayerNo];

                    // Set mutual references to neighbouring gridblocks
                    W_neighbour.NeighbourGridblocks[GridDirection.E] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.W] = W_neighbour;

                    // Overwrite the western cornerpoints with those of the western neighbour
                    gridblock_in.OverwriteGridblockCorners(GridDirection.W, W_neighbour.SEtop, W_neighbour.SEbottom, W_neighbour.NEtop, W_neighbour.NEbottom);
                }
            }

            // Check if there is a gridblock to the south
            if (ConnectToSouthernNeighbour)
            {
                if ((RowNo > 0) && (Gridblocks[RowNo - 1, ColNo, LayerNo] != null))
                {
                    // Get reference to southern neighbour gridblock
                    GridblockConfiguration S_neighbour = Gridblocks[RowNo - 1, ColNo, LayerNo];

                    // Set mutual references to neighbouring gridblocks
                    S_neighbour.NeighbourGridblocks[GridDirection.N] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.S] = S_neighbour;

                    // Overwrite the southern cornerpoints with those of the southern neighbour
                    gridblock_in.OverwriteGridblockCorners(GridDirection.S, S_neighbour.NEtop, S_neighbour.NEbottom, S_neighbour.NWtop, S_neighbour.NWbottom);

                }
            }

            // Check if there is a gridblock to the east
            if (connectToEasternNeighbour)
            {
                if ((ColNo < (NoCols - 1)) && (Gridblocks[RowNo, ColNo + 1, LayerNo] != null))
                {
                    // Get reference to eastern neighbour gridblock
                    GridblockConfiguration E_neighbour = Gridblocks[RowNo, ColNo + 1, LayerNo];

                    // Set mutual references to neighbouring gridblocks
                    E_neighbour.NeighbourGridblocks[GridDirection.W] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.E] = E_neighbour;

                    // Overwrite the western cornerpoints of the eastern neighbour with the eastern cornerpoints of this gridblock
                    E_neighbour.OverwriteGridblockCorners(GridDirection.W, gridblock_in.SEtop, gridblock_in.SEbottom, gridblock_in.NEtop, gridblock_in.NEbottom);
                }
            }

            // Check if there is a gridblock to the north
            if (ConnectToNorthernNeighbour)
            {
                if ((RowNo < (NoRows - 1)) && (Gridblocks[RowNo + 1, ColNo, LayerNo] != null))
                {
                    // Get reference to northern neighbour gridblock
                    GridblockConfiguration N_neighbour = Gridblocks[RowNo + 1, ColNo, LayerNo];

                    // Set mutual references to neighbouring gridblocks
                    N_neighbour.NeighbourGridblocks[GridDirection.S] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.N] = N_neighbour;

                    // Overwrite the southern cornerpoints of the northern neighbour with the northern cornerpoints of this gridblock
                    N_neighbour.OverwriteGridblockCorners(GridDirection.S, gridblock_in.NEtop, gridblock_in.NEbottom, gridblock_in.NWtop, gridblock_in.NWbottom);
                }
            }

            // Check if there is an underlying gridblock
            if (ConnectToUnderlyingNeighbour)
            {
                if ((LayerNo > 0) && (Gridblocks[RowNo, ColNo, LayerNo - 1] != null))
                {
                    // Get reference to underlying neighbour gridblock
                    GridblockConfiguration Underlying_neighbour = Gridblocks[RowNo, ColNo, LayerNo - 1];

                    // Set mutual references to neighbouring gridblocks
                    Underlying_neighbour.NeighbourGridblocks[GridDirection.U] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.D] = Underlying_neighbour;

                    // Overwrite the bottom cornerpoints with those of the underlying neighbour
                    gridblock_in.OverwriteGridblockCorners(GridDirection.D, Underlying_neighbour.NWtop, Underlying_neighbour.SWtop, Underlying_neighbour.NEtop, Underlying_neighbour.SEtop);
                }
            }

            // Check if there is an overlying gridblock
            if (connectToOverlyingNeighbour)
            {
                if ((LayerNo < (NoLayers - 1)) && (Gridblocks[RowNo, ColNo, LayerNo + 1] != null))
                {
                    // Get reference to overlying neighbour gridblock
                    GridblockConfiguration Overlying_neighbour = Gridblocks[RowNo, ColNo, LayerNo + 1];

                    // Set mutual references to neighbouring gridblocks
                    Overlying_neighbour.NeighbourGridblocks[GridDirection.D] = gridblock_in;
                    gridblock_in.NeighbourGridblocks[GridDirection.U] = Overlying_neighbour;

                    // Overwrite the bottom cornerpoints of the overlying neighbour with the top cornerpoints of this gridblock
                    Overlying_neighbour.OverwriteGridblockCorners(GridDirection.D, gridblock_in.NWtop, gridblock_in.SWtop, gridblock_in.NEtop, gridblock_in.SEtop);
                }
            }
        }
        /// <summary>
        /// Random number generator for creating fracture nucleation points
        /// </summary>
        public Random RandomNumberGenerator;

        // Constructors
        /// <summary>
        /// Constructor - create an LxMxN FractureGrid and fill with null objects
        /// </summary>
        /// <param name="NoRows">Number of rows required in the grid</param>
        /// <param name="NoCols">Number of columns required in the grid</param>
        /// <param name="NoLayers">Number of layers required in the grid</param>
        public FractureGrid(int NoRows, int NoCols, int NoLayers)
        {
            // Create an empty gridblock array
            Gridblocks = new GridblockConfiguration[NoRows, NoCols, NoLayers];

            // Create a new DFN control object
            DFNControl = new DFNGenerationControl();

            // Create an empty global DFN object
            CurrentDFN = new GlobalDFN(this);

            // Create an empty list of GlobalDFN objects representing the DFN at intermediate stages of fracture propagation
            DFNGrowthStages = new List<GlobalDFN>();

            // Initialise random number generator for placing fractures
            RandomNumberGenerator = new Random();

            // Initialise the error counters
            HitTimestepLimit = 0;
            ImplicitCalculationException = 0;
            DFNThicknessCutoffActivated = 0;
            DFNGeometricErrors = 0;
            DFNStressErrors = 0;
            DFNFractureLimitErrors = 0;
            ExplicitCalculationException = 0;
        }
    }
}
