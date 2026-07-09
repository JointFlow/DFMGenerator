using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DFMGenerator_SharedCode;

namespace DFMGenerator_DataTransfer
{
    /// <summary>
    /// Enumerator to describe grid file types
    /// </summary>
    enum GridFileType { GRDECL }

    /// <summary>
    /// Enumerator for the current status of a shadow grid object
    /// </summary>
    enum ShadowGridErrorStatus { DataLoadedOK, NoDataLoaded, ErrorReadingFile, ErrorBuildingGrid, ErrorPopulatingProperties }

    /// <summary>
    /// A single pillar in a pillar grid
    /// </summary>
    class ShadowGridPillar
    {
        // Pillar geometry
        /// <summary>
        /// Point at the top of the pillar
        /// </summary>
        public PointXYZ PillarTop { get; private set; }
        /// <summary>
        /// Point at the bottom of the pillar
        /// </summary>
        public PointXYZ PillarBottom { get; private set; }
        /// <summary>
        /// Pillar height in Z; vertical distance between the specified pillar top and pillar bottom
        /// </summary>
        private double PillarHeight { get { return (PillarTop.Z - PillarBottom.Z); } }

        // Cell cornerpoints
        /// <summary>
        /// Arrays of Z coordinates of the layers
        /// There will be a separate array for each of the cell cornerpoint
        /// </summary>
        private Dictionary<GridblockCornerpoint, double[]> CellCornerpointZcoords;
        /// <summary>
        /// Set the Z coordinate of a specified cornerpoint for the appropriate cell in a specified layer
        /// </summary>
        /// <param name="LayerNo">Index number of the specified layer; zero indexed and positive downwards</param>
        /// <param name="Cornerpoint">Specified cornerpoint on the cell adjacent to this pillar; e.g. if NWTop is specified, the NW top corner of the cell to the SE of this pillar will be set</param>
        /// <param name="ZCoord">Value of the Z coordinate of the specified cornerpoint</param>
        /// <returns>Return code: 0 if the specified Z coordinate is set correctly, 1 if the specified layer number is out of range</returns>
        public int SetLayerZCoordinate(int LayerNo, GridblockCornerpoint Cornerpoint, double ZCoord)
        {
            // Get a reference to the correct cornerpoint array
            double[] zcoords = CellCornerpointZcoords[Cornerpoint];

            // If the specified layer number is out of range return error code 1
            if ((LayerNo < 0) || (LayerNo >= zcoords.Length))
                return 1;

            // Set the specified Z coordinate and return code 0
            zcoords[LayerNo] = ZCoord;
            return 0;
        }
        /// <summary>
        /// Get the full coordinates of a specified cornerpoint for the appropriate cell in a specified layer
        /// </summary>
        /// <param name="LayerNo">Index number of the specified layer; zero indexed and positive downwards</param>
        /// <param name="Cornerpoint">Specified cornerpoint on the cell adjacent to this pillar; e.g. if NWTop is specified, the NW top corner of the cell to the SE of this pillar will be returned</param>
        /// <returns>PointXYZ object representing the specified cornerpoint, or null if it is out of range or undefined</returns>
        public PointXYZ GetCellCornerpoint(int LayerNo, GridblockCornerpoint Cornerpoint)
        {
            // Get a reference to the correct cornerpoint array
            double[] zcoords = CellCornerpointZcoords[Cornerpoint];

            // If the specified layer number is out of range return null
            if ((LayerNo < 0) || (LayerNo >= zcoords.Length))
                return null;

            // Get the Z coordinate of the specified cornerpoint
            // If this is undefined return null
            double cellCornerZ = zcoords[LayerNo];
            if (double.IsNaN(cellCornerZ))
                return null;

            // Calculate the X and Y coordinates of the specified cornerpoint from the pillar endpoint coordinates
            double proportionalDistanceFromTop = (PillarTop.Z - cellCornerZ) / PillarHeight;
            double cellCornerX = (PillarTop.X * (1 - proportionalDistanceFromTop)) + (PillarBottom.X * proportionalDistanceFromTop);
            double cellCornerY = (PillarTop.Y * (1 - proportionalDistanceFromTop)) + (PillarBottom.Y * proportionalDistanceFromTop);

            // Return a PointXYZ object representing the specified cornerpoint
            return new PointXYZ(cellCornerX, cellCornerY, cellCornerZ);
        }
        /// <summary>
        /// Get the depth (-Z coordinate) of a specified cornerpoint for the appropriate cell in a specified layer
        /// </summary>
        /// <param name="LayerNo">Index number of the specified layer; zero indexed and positive downwards</param>
        /// <param name="Cornerpoint">Specified cornerpoint on the cell adjacent to this pillar; e.g. if NWTop is specified, the NW top corner of the cell to the SE of this pillar will be returned</param>
        /// <returns>Depth of the specified cornerpoint, or NaN if it is out of range or undefined</returns>
        public double GetCellCornerDepth(int LayerNo, GridblockCornerpoint Cornerpoint)
        {
            // Get a reference to the correct cornerpoint array
            double[] zcoords = CellCornerpointZcoords[Cornerpoint];

            // If the specified layer number is out of range return null
            if ((LayerNo < 0) || (LayerNo >= zcoords.Length))
                return double.NaN;

            // Return the Z coordinate of the specified cornerpoint
            return -zcoords[LayerNo];
        }

        // Constructors
        /// <summary>
        /// Default consturctor: Define the top and bottom points of the pillar and create empty arrays for the Z Coordinates
        /// </summary>
        /// <param name="PillarTop_in">PointXYZ object representing the top of the pillar</param>
        /// <param name="PillarBottom_in">PointXYZ object representing the bottom of the pillar</param>
        /// <param name="NoLayers">Number of layers in the grid</param>
        public ShadowGridPillar(PointXYZ PillarTop_in, PointXYZ PillarBottom_in, int NoLayers)
        {
            // Set the top and bottom points
            PillarTop = PillarTop_in;
            PillarBottom = PillarBottom_in;

            // Create the arrays for Z Coordinates of the cornerpoints; these will initially be set to null values
            CellCornerpointZcoords = new Dictionary<GridblockCornerpoint, double[]>();
            foreach (GridblockCornerpoint corner in Enum.GetValues(typeof(GridblockCornerpoint)).Cast<GridblockCornerpoint>())
                CellCornerpointZcoords[corner] = new double[NoLayers];
        }
    }

    /// <summary>
    /// Template for a named property attached to the shadow grid
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    class GridPropertyArray<T>
    {
        // Property name
        /// <summary>
        /// Property name; this is used to index and find the property
        /// </summary>
        public string PropertyName { get; private set; }

        // Property values
        /// <summary>
        /// Array of property values for each cell in the shadow grid
        /// </summary>
        private T[,,] values;
        /// <summary>
        /// Set all property to the same value in all cells in the array
        /// </summary>
        /// <param name="value">Value to set the property to</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified cell is out of range</returns>
        public int SetInitialPropertyValue(T value)
        {
            int NoIValues = values.GetLength(0);
            int NoJValues = values.GetLength(1);
            int NoKValues = values.GetLength(2);

            for (int i = 0; i < NoIValues; i++)
                for (int j = 0; j < NoJValues; j++)
                    for (int k = 0; k < NoKValues; k++)
                        values[i, j, k] = value;

            return 0;
        }
        /// <summary>
        /// Set all property to the same value in all cells in a specified layer
        /// </summary>
        /// <param name="LayerNo">Layer number to set the value in</param>
        /// <param name="value">Value to set the property to</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified layer is out of range</returns>
        public int SetInitialPropertyValue(int LayerNo, T value)
        {
            int NoIValues = values.GetLength(0);
            int NoJValues = values.GetLength(1);
            int NoKValues = values.GetLength(2);

            if ((LayerNo < 0) || (LayerNo >= NoKValues))
                return 1;

            for (int i = 0; i < NoIValues; i++)
                for (int j = 0; j < NoJValues; j++)
                    values[i, j, LayerNo] = value;

            return 0;
        }
        /// <summary>
        /// Set the value of the property in a specified cell
        /// </summary>
        /// <param name="cellI">I coordinate of cell to set</param>
        /// <param name="cellJ">J coordinate of cell to set</param>
        /// <param name="cellK">K coordinate of cell to set</param>
        /// <param name="value">Value to set the property to</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified cell is out of range</returns>
        public int SetPropertyValue(int cellI, int cellJ, int cellK, T value)
        {
            try
            {
                values[cellI, cellJ, cellK] = value;
                return 0;
            }
            catch (IndexOutOfRangeException)
            {
                return 1;
            }
        }
        /// <summary>
        /// Get the value of the property in a specified cell
        /// </summary>
        /// <param name="cellI">I coordinate of cell to get data from</param>
        /// <param name="cellJ">J coordinate of cell to get data from</param>
        /// <param name="cellK">K coordinate of cell to get data from</param>
        /// <returns>The value of the property in the specified cell, or the default value (NaN for floating point types, 0 for integer types) if the specified cell is out of range</returns>
        public T GetPropertyValue(int cellI, int cellJ, int cellK)
        {
            try
            {
                return values[cellI, cellJ, cellK];
            }
            catch (IndexOutOfRangeException)
            {
                return default(T);
            }
        }

        // Constructors
        /// <summary>
        /// Create a new property and fill it with default values (NaN for floating point types, 0 for integer types)
        /// </summary>
        /// <param name="PropertyName_in">Property name; this is used to index and find the property</param>
        /// <param name="NoICols">Number of columns (I coordinate) in the grid</param>
        /// <param name="NoJRows">Number of rows (J coordinate) in the grid</param>
        /// <param name="NoKLayers">Number of layers (K coordinate) in the grid</param>
        public GridPropertyArray(string PropertyName_in, int NoICols, int NoJRows, int NoKLayers)
        {
            PropertyName = PropertyName_in;
            values = new T[NoICols, NoJRows, NoKLayers];
        }
        /// <summary>
        /// Create a new property and fill it with specified values
        /// </summary>
        /// <param name="PropertyName_in">Property name; this is used to index and find the property</param>
        /// <param name="NoICols">Number of columns (I coordinate) in the grid</param>
        /// <param name="NoJRows">Number of rows (J coordinate) in the grid</param>
        /// <param name="NoKLayers">Number of layers (K coordinate) in the grid</param>
        /// <param name="InitialValue">Value to set the property to</param>
        public GridPropertyArray(string PropertyName_in, int NoICols, int NoJRows, int NoKLayers, T InitialValue) :
            this(PropertyName_in, NoICols, NoJRows, NoKLayers)
        {
            SetInitialPropertyValue(InitialValue);
        }
    }

    /// <summary>
    /// Template for an arbitrary block of data read from a GRDECL file
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    class GRDECLDataBlock<T>
    {
        // Block name
        /// <summary>
        /// Block name; this is used to index and find the data in the block
        /// </summary>
        public string BlockName { get; private set; }

        // Data values
        /// <summary>
        /// List of data values
        /// </summary>
        public List<T> BlockData { get; private set; }
        /// <summary>
        /// Number of data values in the data list
        /// </summary>
        public int NoDataValues { get { return BlockData.Count; } }

        // Constructors
        /// <summary>
        /// Default constructor: specify the block name and create a new data list
        /// </summary>
        /// <param name="BlockName_in">Block name</param>
        public GRDECLDataBlock(string BlockName_in)
        {
            BlockName = BlockName_in;
            BlockData = new List<T>();
        }
    }

    /// <summary>
    /// DFM Generator shadow grid that can load data from and write data to GRDECL files
    /// </summary>
    class ShadowGrid : IDataTransfer
    {
        // Error status
        /// <summary>
        /// Current status of the grid; will be set when loading data
        /// </summary>
        public ShadowGridErrorStatus ErrorStatus { get; private set; }

        // Grid geometry
        // Grid is zero-indexed from SWtop corner
        // I increases towards E
        // J increases towards N
        // K increases downwards
        // NB J coordinate direction is the opposite to that in most GRDECL files
        /// <summary>
        /// Number of columns (I coordinate) in the grid
        /// </summary>
        public int NoICols { get; private set; }
        /// <summary>
        /// Number of rows (J coordinate) in the grid
        /// </summary>
        public int NoJRows { get; private set; }
        /// <summary>
        /// Number of layers (K coordinate) in the grid
        /// </summary>
        public int NoKLayers { get; private set; }
        /// <summary>
        /// Total number of cells in the grid
        /// </summary>
        public int NoCells { get { return NoICols * NoJRows * NoKLayers; } }
        /// <summary>
        /// Total number of pillars in the grid
        /// </summary>
        public int NoPillars { get { return (NoICols + 1) * (NoJRows + 1); } }
        /// <summary>
        /// Array of grid pillars
        /// </summary>
        private ShadowGridPillar[,] gridPillars;
        /// <summary>
        /// Arrays of cell face types (0 for unfaulted faces, >0 for faulted faces, giving the index number of the fault)
        /// </summary>
        private Dictionary<GridDirection, GridPropertyArray<int>> cellFaces;
        /// <summary>
        /// List of fault names; the index number of the name in the list corresponds to the fault index numbers in the cellFaces arrays
        /// </summary>
        private List<string> faultNames;

        // Input properties
        // These properties will be set from GRDECL input files and read by DFM Generator as inputs
        /// <summary>
        /// List of floating point input property arrays
        /// </summary>
        private List<GridPropertyArray<double>> floatingPointInputProperties;
        /// <summary>
        /// List of integer input property arrays
        /// </summary>
        private List<GridPropertyArray<int>> integerInputProperties;

        // Output properties
        // These properties will be set from DFM Generator outputs and written to GRDECL output files
        // These comprise nested lists of properties, so it is possible to maintain multiple lists representing different stages in the model output
        // There will also be pointers pointing to the lists for the current stage for easy access, and a list of stage names
        /// <summary>
        /// List of floating point output property arrays for all stages
        /// </summary>
        private List<List<GridPropertyArray<double>>> floatingPointOutputProperties;
        /// <summary>
        /// List of integer output property arrays for all stages
        /// </summary>
        private List<List<GridPropertyArray<int>>> integerOutputProperties;
        /// <summary>
        /// Pointer to the list of floating point output property arrays for the current stage
        /// </summary>
        private List<GridPropertyArray<double>> currentStageFloatingPointOutputProperties;
        /// <summary>
        /// Pointer to the list of integer output property arrays for the current stage
        /// </summary>
        private List<GridPropertyArray<int>> currentStageIntegerOutputProperties;
        /// <summary>
        /// List of names for each stage; will be used to name output GRDECL files
        /// </summary>
        private List<string> stageNames;
        /// <summary>
        /// Number of output stages currently defined
        /// </summary>
        private int NoStages { get { return stageNames.Count; } }

        // Functions to read data from the shadow grid to DFM Generator
        /// <summary>
        /// Get the coordinates of one of the cornerpoints of a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="corner">Corner of the cell for which coordinates are required</param>
        /// <returns>PointXYZ object representing the required cell corner</returns>
        public PointXYZ GetCellCornerpoint(int cellI, int cellJ, int cellK, GridblockCornerpoint corner)
        {
            // Get the correct pillar coordinates
            int pillarI = cellI;
            if ((corner == GridblockCornerpoint.NETop) || (corner == GridblockCornerpoint.NEBottom) || (corner == GridblockCornerpoint.SETop) || (corner == GridblockCornerpoint.SEBottom))
                pillarI++;
            int pillarJ = cellJ;
            if ((corner == GridblockCornerpoint.NWTop) || (corner == GridblockCornerpoint.NWBottom) || (corner == GridblockCornerpoint.NETop) || (corner == GridblockCornerpoint.NEBottom))
                pillarJ++;
            return gridPillars[pillarI, pillarJ].GetCellCornerpoint(cellK, corner);
        }
        /// <summary>
        /// Get the depth (-Z coordinate) of one of the cornerpoints of a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="corner">Corner of the cell for which coordinates are required</param>
        /// <returns>Depth of the required cell corner</returns>
        private double GetCellCornerDepth(int cellI, int cellJ, int cellK, GridblockCornerpoint corner)
        {
            // Get the correct pillar coordinates
            int pillarI = cellI;
            if ((corner == GridblockCornerpoint.NETop) || (corner == GridblockCornerpoint.NEBottom) || (corner == GridblockCornerpoint.SETop) || (corner == GridblockCornerpoint.SEBottom))
                pillarI++;
            int pillarJ = cellJ;
            if ((corner == GridblockCornerpoint.NWTop) || (corner == GridblockCornerpoint.NWBottom) || (corner == GridblockCornerpoint.NETop) || (corner == GridblockCornerpoint.NEBottom))
                pillarJ++;
            return gridPillars[pillarI, pillarJ].GetCellCornerDepth(cellK, corner);
        }
        /// <summary>
        /// Check to see if the face of a cell in the shadow grid is faulted
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to check</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to check</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to check</param>
        /// <param name="corner">Face of the cell to check</param>
        /// <returns>True if the specified cell face is faulted, otherwise false</returns>
        public bool FaultedContact(int cellI, int cellJ, int cellK, GridDirection face)
        {
            int faceType = cellFaces[face].GetPropertyValue(cellI, cellJ, cellK);
            if (faceType == 0)
                return false;
            else
                return true;
        }
        /// Get the value of a specified floating point property in a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="PropertyName">Name of the property required</param>
        /// <returns>Value of the property in the specified cell; if the cell or the property do not exist, returns NaN</returns>
        public double GetFloatingPointPropertyValue(int cellI, int cellJ, int cellK, string PropertyName)
        {
            GridPropertyArray<double> prop = floatingPointInputProperties.Find(x => x.PropertyName == PropertyName);
            if (prop is null)
                return double.NaN;
            else
                return prop.GetPropertyValue(cellI, cellJ, cellK);
        }
        /// <summary>
        /// Get the value of a specified integer property in a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="PropertyName">Name of the property required</param>
        /// <returns>Value of the property in the specified cell; if the cell or the property do not exist, returns the default null value</returns>
        public int GetIntegerPropertyValue(int cellI, int cellJ, int cellK, string PropertyName)
        {
            GridPropertyArray<int> prop = integerInputProperties.Find(x => x.PropertyName == PropertyName);
            if (prop is null)
                return default(int);
            else
                return prop.GetPropertyValue(cellI, cellJ, cellK);
        }

        // Functions to write data to the shadow grid from DFM Generator
        /// <summary>
        /// Create a new output stage with new lists of floating point and integer properties
        /// </summary>
        /// <returns>Return code: 0 if the operation was successful</returns>
        public int CreateNewStage(string StageName)
        {
            currentStageFloatingPointOutputProperties = new List<GridPropertyArray<double>>();
            currentStageIntegerOutputProperties = new List<GridPropertyArray<int>>();
            floatingPointOutputProperties.Add(currentStageFloatingPointOutputProperties);
            integerOutputProperties.Add(currentStageIntegerOutputProperties);
            stageNames.Add(StageName);
            return 0;
        }
        /// <summary>
        /// Create a new floating point property in the shadow grid and populate it with NaNs
        /// </summary>
        /// <param name="PropertyName">Name of the new property</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if a property already exists with the specified name</returns>
        public int CreateFloatingPointProperty(string PropertyName)
        {
            // Check to see if a property with the specified name already exists; if so return 1
            if (currentStageFloatingPointOutputProperties.Exists(x => x.PropertyName == PropertyName))
                return 1;

            // Create a new property array and add it to the appropriate list
            GridPropertyArray<double> newProperty = new GridPropertyArray<double>(PropertyName, NoICols, NoJRows, NoKLayers);
            currentStageFloatingPointOutputProperties.Add(newProperty);
            return 0;
        }
        /// <summary>
        /// Create a new integer property in the shadow grid and populate it with default null values
        /// </summary>
        /// <param name="PropertyName">Name of the new property</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if a property already exists with the specified name</returns>
        public int CreateIntegerProperty(string PropertyName)
        {
            // Check to see if a property with the specified name already exists; if so return 1
            if (currentStageIntegerOutputProperties.Exists(x => x.PropertyName == PropertyName))
                return 1;

            // Create a new property array and add it to the appropriate list
            GridPropertyArray<int> newProperty = new GridPropertyArray<int>(PropertyName, NoICols, NoJRows, NoKLayers);
            currentStageIntegerOutputProperties.Add(newProperty);
            return 0;
        }
        /// <summary>
        /// Set the value of a specified floating point property in a specified cell in the shadow grid to a specified value
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to write data to</param>
        /// <param name="PropertyName">Name of the property to write data to</param>
        /// <param name="value">Value of the property to write</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified cell does not exist, 2 if the specified property does not exist, 3 if the specified value is invalid</returns>
        public int SetFloatingPointProperty(int cellI, int cellJ, int cellK, string PropertyName, double value)
        {
            GridPropertyArray<double> prop = currentStageFloatingPointOutputProperties.Find(x => x.PropertyName == PropertyName);
            if (prop is null)
                return 1;
            else
                return prop.SetPropertyValue(cellI, cellJ, cellK, value);
        }
        /// <summary>
        /// Set the value of a specified integer property in a specified cell in the shadow grid to a specified value
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to write data to</param>
        /// <param name="PropertyName">Name of the property to write data to</param>
        /// <param name="value">Value of the property to write</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified cell does not exist, 2 if the specified property does not exist, 3 if the specified value is invalid</returns>
        public int SetIntegerProperty(int cellI, int cellJ, int cellK, string PropertyName, int value)
        {
            GridPropertyArray<int> prop = currentStageIntegerOutputProperties.Find(x => x.PropertyName == PropertyName);
            if (prop is null)
                return 1;
            else
                return prop.SetPropertyValue(cellI, cellJ, cellK, value);
        }

        // Functions to write data from the shadow grid to GRDECL files
        /// <summary>
        /// String describing the source of the geometry data, to be included as a comment in GRDECL output files
        /// </summary>
        private string GeometryDataSource { get; set; }
        /// <summary>
        /// String describing the source of the input property data, to be included as a comment in GRDECL output files
        /// </summary>
        private string InputDataSource { get; set; }
        /// <summary>
        /// String describing the source of the output property data, to be included as a comment in GRDECL output files
        /// </summary>
        private string OutputDataSource { get; set; }
        /// <summary>
        /// Get a string representing the map unit data in GRDECL format
        /// </summary>
        /// <returns>A string representing the map unit data in GRDECL format</returns>
        private string GetMapUnitsInGRDECLFormat()
        {
            string unitData = "";
            unitData += string.Format("MAPUNITS\t{0} Generated: {1}\n", CommentIndicator, GeometryDataSource);
            unitData += string.Format("  METRES {0}\n\n", EndBlockIndicator);
            return unitData;
        }
        /// <summary>
        /// Get a string representing the map axis data in GRDECL format
        /// </summary>
        /// <returns>A string representing the map axis data in GRDECL format</returns>
        private string GetMapAxesInGRDECLFormat()
        {
            string axisData = "";
            PointXYZ gridOrigin = gridPillars[0, 0].GetCellCornerpoint(0, GridblockCornerpoint.NWTop);
            axisData += string.Format("MAPAXES\t{0} Generated: {1}\n", CommentIndicator, GeometryDataSource);
            axisData += string.Format("  {0} {1} {2} {3} {4} {5} {6}\n\n", gridOrigin.X, gridOrigin.Y + 1000, gridOrigin.X, gridOrigin.X, gridOrigin.X + 1000, gridOrigin.Y, EndBlockIndicator);
            return axisData;
        }
        /// <summary>
        /// Get a string representing the grid unit data in GRDECL format
        /// </summary>
        /// <returns>A string representing the grid unit data in GRDECL format</returns>
        private string GetGridUnitsInGRDECLFormat()
        {
            string unitData = "";
            unitData += string.Format("GRIDUNIT\t{0} Generated: {1}\n", CommentIndicator, GeometryDataSource);
            unitData += string.Format("  METRES MAP {0}\n\n", EndBlockIndicator);
            return unitData;
        }
        /// <summary>
        /// Get a string representing the grid coordinate system data in GRDECL format
        /// </summary>
        /// <returns>A string representing the grid coordinate system data in GRDECL format</returns>
        private string GetCoordinateSystemInGRDECLFormat()
        {
            string coordinateSystemData = "";
            coordinateSystemData += string.Format("COORDSYS\t{0} Generated: {1}\n", CommentIndicator, GeometryDataSource);
            coordinateSystemData += string.Format("  {0} {1} {2}\n\n", 1, 5, EndBlockIndicator);
            return coordinateSystemData;
        }
        /// <summary>
        /// Get a string representing the dimension data for the entire grid in GRDECL format
        /// </summary>
        /// <returns>A string representing the dimension data for the entire grid in GRDECL format</returns>
        private string GetGridDimensionsInGRDECLFormat()
        {
            return GetGridDimensionsInGRDECLFormat(0, NoKLayers - 1);
        }
        /// <summary>
        /// Get a string representing the dimension data for a specified stratigraphic interval of the grid in GRDECL format
        /// </summary>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <returns>A string representing the dimension data for a specified stratigraphic interval of the grid in GRDECL format</returns>
        private string GetGridDimensionsInGRDECLFormat(int TopLayerK, int BottomLayerK)
        {
            int noLayers = BottomLayerK + 1 - TopLayerK;
            string dimensionData = "";
            //dimensionData += string.Format("DIMENS\t{0} Generated: {1}\n", CommentIndicator, GeometryDataSource);
            //dimensionData += string.Format("  {0} {1} {2}/\n\n", NoICols, NoJRows, noLayers);
            dimensionData += string.Format("SPECGRID\t{0} Generated: {1}\n", CommentIndicator, GeometryDataSource);
            dimensionData += string.Format("  {0} {1} {2} {3} {4} {5}\n\n", NoICols, NoJRows, noLayers, 1, "F", EndBlockIndicator);
            return dimensionData;
        }
        /// <summary>
        /// Get a string representing the grid pillar endpoints in GRDECL format
        /// </summary>
        /// <returns>A string representing the grid pillar endpoints in GRDECL format</returns>
        private string GetPillarsInGRDECLFormat()
        {
            string pillarData = string.Format("COORD\t{0} Generated : {1}\n", CommentIndicator, GeometryDataSource);
            for (int pillarJ = NoJRows; pillarJ >= 0; pillarJ--)
                for (int pillarI = 0; pillarI <= NoICols; pillarI++)
                {
                    PointXYZ PillarTop = gridPillars[pillarI, pillarJ].PillarTop;
                    PointXYZ PillarBottom = gridPillars[pillarI, pillarJ].PillarBottom;
                    pillarData += string.Format("  {0} {1} {2} {3} {4} {5}\n", PillarTop.X, PillarTop.Y, PillarTop.Depth, PillarBottom.X, PillarBottom.Y, PillarBottom.Depth);
                }
            pillarData += string.Format("  {0}\n\n", EndBlockIndicator);
            return pillarData;
        }
        /// <summary>
        /// Get a string representing the cell cornerpoints for the entire grid in GRDECL format
        /// NB only the Z coordinates of the cell cornerpoints are stored in the GRDECL file; the X and Y coordinates are recalculated from the Z coordinates and the pillar endpoints when the file is parsed
        /// </summary>
        /// <returns>A string representing the cell cornerpoints for the entire grid in GRDECL format</returns>
        private string GetCellCornerDepthsInGRDECLFormat()
        {
            return GetCellCornerDepthsInGRDECLFormat(0, NoKLayers - 1);
        }
        /// <summary>
        /// Get a string representing the cell cornerpoints of a specified stratigraphic interval of the grid in GRDECL format
        /// </summary>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <returns>A string representing the cell cornerpoints of a specified stratigraphic interval of the grid in GRDECL format</returns>
        private string GetCellCornerDepthsInGRDECLFormat(int TopLayerK, int BottomLayerK)
        {
            string cornerDepthData = string.Format("ZCORN\t{0} Generated : {1}\n ", CommentIndicator, GeometryDataSource);
            for (int cellK = TopLayerK; cellK <= BottomLayerK; cellK++)
            {
                // Get the depths for the top corners of all the cells in layer K
                for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                {
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        cornerDepthData += string.Format(" {0} {1}", GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.NWTop), GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.NETop));
                    }
                    cornerDepthData += "\n ";
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        cornerDepthData += string.Format(" {0} {1}", GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.SWTop), GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.SETop));
                    }
                    cornerDepthData += "\n ";
                }

                // Get the depths for the bottom corners of all the cells in layer K
                for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                {
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        cornerDepthData += string.Format(" {0} {1}", GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.NWBottom), GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.NEBottom));
                    }
                    cornerDepthData += "\n ";
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        cornerDepthData += string.Format(" {0} {1}", GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.SWBottom), GetCellCornerDepth(cellI, cellJ, cellK, GridblockCornerpoint.SEBottom));
                    }
                    cornerDepthData += "\n ";
                }
            }
            cornerDepthData += string.Format("  {0}\n\n", EndBlockIndicator);
            return cornerDepthData;
        }
        /// <summary>
        /// Get a string representing the faults within the entire grid in GRDECL format
        /// </summary>
        /// <returns>A string representing a nested datablock describing the faulted cell faces within the entire grid in GRDECL format</returns>
        private string GetFaultsInGRDECLFormat()
        {
            return GetFaultsInGRDECLFormat(0, NoKLayers - 1);
        }
        /// <summary>
        /// Get a string representing the faults within a specified stratigraphic interval of the grid in GRDECL format
        /// </summary>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <returns>A string representing a nested datablock describing the faulted cell faces within a specified stratigraphic interval of the grid in GRDECL format</returns>
        private string GetFaultsInGRDECLFormat(int TopLayerK, int BottomLayerK)
        {
            string faultData = string.Format("FAULTS\t{0} Generated : {1}\n\n", CommentIndicator, GeometryDataSource);
            faultData += CommentIndicator + " NAME\tIX1\tIX2\tIY1\tIY2\tIZ1\tIZ2\tFACE\n";

            // Loop through all the cells finding fault patches
            // We will assume patches are a maximum of one cell wide, but may be many cells tall
            // We will therefore loop through the I and J grid coordinates first to find cell stacks, and then through each face of the stack
            for (int cellI = 0; cellI < NoICols; cellI++)
                for (int cellJ = 0; cellJ < NoJRows; cellJ++)
                    foreach (GridDirection face in new GridDirection[] { GridDirection.E, GridDirection.W, GridDirection.S, GridDirection.N })
                    {
                        string directionValue = "";
                        switch (face)
                        {
                            case GridDirection.N:
                                directionValue = "Y-";
                                break;
                            case GridDirection.E:
                                directionValue = "X+";
                                break;
                            case GridDirection.S:
                                directionValue = "Y+";
                                break;
                            case GridDirection.W:
                                directionValue = "X-";
                                break;
                            case GridDirection.None:
                                break;
                            default:
                                break;
                        }

                        // Set null values for the top and bottom of the current fault patch and the current fault index number
                        int TopK = -1;
                        int BottomK = -1;
                        int currentFaultIndex = 0;

                        // Now loop through the K layers looking for faulted patches on the current face
                        for (int cellK = TopLayerK; cellK <= BottomLayerK; cellK++)
                        {
                            // Get the value in the cellFaces array for the appropriate face of the current cell
                            int cellFaceValue = cellFaces[face].GetPropertyValue(cellI, cellJ, cellK);

                            // If we have not yet found a fault patch, look for the start of a fault patch
                            if (currentFaultIndex == 0)
                            {
                                if (cellFaceValue > 0)
                                {
                                    // If we have found the top of the fault patch, set the top of the current fault patch and the current fault index number
                                    TopK = cellK;
                                    currentFaultIndex = cellFaceValue;
                                }
                            }

                            // If we have aleady found the top of a fault patch, check if we are at the bottom of the patch
                            // This may be because we are at the bottom of the cell stack, or because the cellFaces value of the underlying cell is different to the current fault index
                            if ((currentFaultIndex != 0) && ((cellK == BottomLayerK) || (cellFaces[face].GetPropertyValue(cellI, cellJ, cellK + 1) != currentFaultIndex)))
                            {
                                // If we are at the bottom of the cell stack we will need to set the BottomK value manually
                                if (cellK == BottomLayerK)
                                    BottomK = BottomLayerK;

                                // If we have found the bottom of the fault patch, write a new subblock to the nested fault data block, then reset the top abd bottom fault 
                                // The coordinates in the GRDECL files are 1-indexed so must be converted from 0-index
                                // The J coordinates must also be reversed
                                // Finally the fault index number must be converted from 1-index to 0-index to lookup the fault name
                                string faultName = (currentFaultIndex <= faultNames.Count) ? faultNames[currentFaultIndex - 1] : "FAULT";
                                string subblockText = string.Format("'{0}' {1} {2} {3} {4} {5} {6} '{7}' {8}\n", faultName, cellI + 1, cellI + 1, NoJRows - cellJ, NoJRows - cellJ, TopK + 1, BottomK + 1, directionValue, EndBlockIndicator);
                                faultData += subblockText;
                                TopK = -1;
                                BottomK = -1;
                                currentFaultIndex = 0;
                            }
                        }
                    }

            // Add a list of unique fault names and an end block indicator to the nested block
            faultData += string.Format("\n{0} List of unique fault names\n", CommentIndicator);
            foreach (string faultName in faultNames)
                faultData += string.Format("{0} {1}\n", CommentIndicator, faultName);
            faultData += string.Format("\n{0}\n\n", EndBlockIndicator);

            return faultData;
        }
        /// <summary>
        /// Get a string representing the Actnum values of all the cells in the grid in GRDECL format, assuming all cells are active (Actnum value 1)
        /// </summary>
        /// <returns>A string representing the Actnum values of all the cells in the grid in GRDECL format</returns>
        private string GetActnumDataInGRDECLFormat()
        {
            string actnumData = string.Format("ACTNUM\t{0} Generated : {1}\n  ", CommentIndicator, GeometryDataSource);
            actnumData += string.Format("  {0}*1  {1}\n\n", NoCells, EndBlockIndicator);
            return actnumData;
        }
        /// <summary>
        /// Get a string representing the Actnum values of all the cells in a specified stratigraphic interval of the grid in GRDECL format, assuming all cells are active (Actnum value 1)
        /// </summary>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <returns>A string representing the Actnum values of all the cells in a specified stratigraphic interval of the grid in GRDECL formatt</returns>
        private string GetActnumDataInGRDECLFormat(int TopLayerK, int BottomLayerK)
        {
            string actnumData = string.Format("ACTNUM\t{0} Generated : {1}\n  ", CommentIndicator, GeometryDataSource);
            actnumData += string.Format("  {0}*1  {1}\n\n", NoICols * NoJRows * (TopLayerK - BottomLayerK + 1), EndBlockIndicator);
            return actnumData;
        }
        /// <summary>
        /// Get a string representing the values of a specified floating point property for all the cells in the grid in GRDECL format
        /// </summary>
        /// <param name="Property">GridPropertyArray object containing the property to be exported</param>
        /// <param name="Source">A string defining the source of the property data; this will be included as a comment in the GRDECL format output string</param>
        /// <returns>A string representing the values of the specified property for all the cells in the grid in GRDECL format</returns>
        private string GetPropertyDataInGRDECLFormat(GridPropertyArray<double> Property, string Source)
        {
            return GetPropertyDataInGRDECLFormat(Property, 0, NoKLayers - 1, Source);
        }
        /// <summary>
        /// Get a string representing the values of a specified floating point property for all the cells in a specified stratigraphic interval of the grid in GRDECL format
        /// </summary>
        /// <param name="Property">GridPropertyArray object containing the property to be exported</param>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <param name="Source">A string defining the source of the property data; this will be included as a comment in the GRDECL format output string</param>
        /// <returns>A string representing the values of the specified property for all the cells in a specified stratigraphic interval of the grid in GRDECL format</returns>
        private string GetPropertyDataInGRDECLFormat(GridPropertyArray<double> Property, int TopLayerK, int BottomLayerK, string Source)
        {
            // Set the source by checking if the property is in the input or output arrays and set the source
            /*string Source = "";
            if (floatingPointInputProperties.Contains(Property))
                Source = InputDataSource;
            else foreach (List<GridPropertyArray<double>> nextStageProperties in floatingPointOutputProperties)
                    if (nextStageProperties.Contains(Property))
                        Source = OutputDataSource;*/

            // Create a string for the property
            string propertyData = string.Format("{0}\t{1} Generated : {2}\n ", Property.PropertyName, CommentIndicator, Source);

            // Loop through all cells in the specified layers in the appropriate order, getting the correct property values
            for (int cellK = TopLayerK; cellK <= BottomLayerK; cellK++)
            {
                for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                {
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        propertyData += string.Format(" {0}", Property.GetPropertyValue(cellI, cellJ, cellK));
                    }
                    propertyData += "\n ";
                }
            }

            // Add the terminator to the property string and return it
            propertyData += string.Format("  {0}\n\n", EndBlockIndicator);
            return propertyData;
        }
        /// <summary>
        /// Get a string representing the values of a specified integer property for all the cells in the grid in GRDECL format
        /// </summary>
        /// <param name="Property">GridPropertyArray object containing the property to be exported</param>
        /// <param name="Source">A string defining the source of the property data; this will be included as a comment in the GRDECL format output string</param>
        /// <returns>A string representing the values of the specified property for all the cells in the grid in GRDECL format</returns>
        private string GetPropertyDataInGRDECLFormat(GridPropertyArray<int> Property, string Source)
        {
            return GetPropertyDataInGRDECLFormat(Property, 0, NoKLayers - 1, Source);
        }
        /// <summary>
        /// Get a string representing the values of a specified integer property for all the cells in a specified stratigraphic interval of the grid in GRDECL format
        /// </summary>
        /// <param name="Property">GridPropertyArray object containing the property to be exported</param>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <param name="Source">A string defining the source of the property data; this will be included as a comment in the GRDECL format output string</param>
        /// <returns>A string representing the values of the specified property for all the cells in a specified stratigraphic interval of the grid in GRDECL format</returns>
        private string GetPropertyDataInGRDECLFormat(GridPropertyArray<int> Property, int TopLayerK, int BottomLayerK, string Source)
        {
            // Set the source by checking if the property is in the input or output arrays and set the source
            /*string Source = "";
            if (integerInputProperties.Contains(Property))
                Source = InputDataSource;
            else foreach (List<GridPropertyArray<int>> nextStageProperties in integerOutputProperties)
                    if (nextStageProperties.Contains(Property))
                        Source = OutputDataSource;*/

            // Create a string for the property
            string propertyData = string.Format("{0}\t{1} Generated : {2}\n ", Property.PropertyName, CommentIndicator, Source);

            // Loop through all cells in the specified layers in the appropriate order, getting the correct property values
            for (int cellK = TopLayerK; cellK <= BottomLayerK; cellK++)
            {
                for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                {
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        propertyData += string.Format(" {0}", Property.GetPropertyValue(cellI, cellJ, cellK));
                    }
                    propertyData += "\n ";
                }
            }

            // Add the terminator to the property string and return it
            propertyData += string.Format("  {0}\n\n", EndBlockIndicator);
            return propertyData;
        }
        /// <summary>
        /// Write grid geometry and implicit output data of a specified output stage for a specified stratigraphic interval to a GRDECL format file
        /// </summary>
        /// <param name="ModelName">Name of the model to output - will be included in the filename</param>
        /// <param name="FilePath">File path to write the output file to</param>
        /// <param name="progressReporter">Reference to progress reporter implementing the IProgressReporterWrapper interface</param>
        /// <param name="Stage">Stage of the model output to write to the GRDECL file, indexed from 1</param>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <param name="IncludeGridGeometry">Flag to include grid geometry data in the GRDECL file</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified stage does not exist, 2 if there is an error writing to the specified file</returns>
        public int WriteGRDECLFile(string ModelName, string FilePath, IProgressReporterWrapper progressReporter, int Stage, int TopLayerK, int BottomLayerK, bool IncludeGridGeometry, bool PopulateEmptyGridblocks)
        {
            try
            {
                // Check if the specified top and bottom layers are within range; if not adjust them to lie within the grid
                if (TopLayerK < 0) TopLayerK = 0;
                if (BottomLayerK >= NoKLayers) BottomLayerK = NoKLayers - 1;
                if (TopLayerK > BottomLayerK) TopLayerK = BottomLayerK;

                // Get the lists of output properties for the soecified stage and the stage name
                // If the specified stage is out of range create an error message and return code 1
                if ((Stage < 0) || (Stage > NoStages))
                {
                    progressReporter.OutputMessage(string.Format("There is no output data for stage {0}", Stage));
                    progressReporter.OutputMessage("No output files will be written");
                    return 1;
                }
                List<GridPropertyArray<double>> floatingPointPropertiesToOutput = floatingPointOutputProperties[Stage-1];
                List<GridPropertyArray<int>> integerPropertiesToOutput = integerOutputProperties[Stage-1];
                string stageName = stageNames[Stage - 1];

                // If the calculation has already been cancelled, do not write any output data
                if (!progressReporter.abortCalculation())
                {
                    // Write implicit fracture property data to an Eclipse GRDECL file or files

                    // Set the output file name and extension
                    string fileName = ModelName + "_" + stageName;
                    string fileExtension = ".GRDECL";
                    string fullFileName = FilePath + fileName + fileExtension;
                    progressReporter.OutputMessage(string.Format("Writing implicit data for stage {0} to GRDECL file {1}", Stage, fullFileName));

                    // Create the output file
                    StreamWriter outputFile = new StreamWriter(fullFileName);

                    // Write header
                    string headerInfo = "";
                    headerInfo += string.Format("{0} Generated [\n", CommentIndicator);
                    headerInfo += string.Format("{0} Format      : Eclipse keywords(grid geometry and properties)(ASCII)\n", CommentIndicator);
                    headerInfo += string.Format("{0} Exported by: {1}\n", CommentIndicator, FractureGrid.VersionNumber);
                    headerInfo += string.Format("{0} User name: {1}\n", CommentIndicator, "");
                    headerInfo += string.Format("{0} Date: {1}\n", CommentIndicator, System.DateTime.Now);
                    headerInfo += string.Format("{0} Project: {1}\n", CommentIndicator, ModelName);
                    headerInfo += string.Format("{0} Grid: \n", CommentIndicator);
                    headerInfo += string.Format("{0} Generated ]\n\n", CommentIndicator);
                    outputFile.Write(headerInfo);


                    // If required, write the grid and unit metadata and grid geometry data
                    if (IncludeGridGeometry)
                    {
                        // Write the map units and axes to the output file
                        outputFile.Write(GetMapUnitsInGRDECLFormat());
                        outputFile.Write(GetMapAxesInGRDECLFormat());
                        // Write the grid units to the output file
                        outputFile.Write(GetGridUnitsInGRDECLFormat());
                        // Write the grid dimensions to the output file
                        outputFile.Write(GetGridDimensionsInGRDECLFormat(TopLayerK, BottomLayerK));
                        // Write the coordinate system to the output file 
                        outputFile.Write(GetCoordinateSystemInGRDECLFormat());
                        // Write the coordinates of the grid pillars to the output file
                        outputFile.Write(GetPillarsInGRDECLFormat());
                        // Write the depths (-Z coordinates) of the cell cornerpoint to the output file
                        outputFile.Write(GetCellCornerDepthsInGRDECLFormat(TopLayerK, BottomLayerK));
                        // Write the fault patches to the output file
                        outputFile.Write(GetFaultsInGRDECLFormat(TopLayerK, BottomLayerK));
                    }

                    // Write the property data for all properties in the specified output stage to the output file
                    foreach (GridPropertyArray<double> nextProperty in floatingPointPropertiesToOutput)
                        outputFile.Write(GetPropertyDataInGRDECLFormat(nextProperty, TopLayerK, BottomLayerK, OutputDataSource));
                    foreach (GridPropertyArray<int> nextProperty in integerPropertiesToOutput)
                        outputFile.Write(GetPropertyDataInGRDECLFormat(nextProperty, TopLayerK, BottomLayerK, OutputDataSource));

                    // Close the output file
                    outputFile.Close();
                }
            }
            catch (Exception e)
            {
                // Write an error message and return code 2
                progressReporter.OutputMessage(string.Format("Error occurred while writing output file for {0} stage {1} to {2}", ModelName, Stage, FilePath));
                progressReporter.OutputMessage(string.Format("Error message: {0}", e.Message));
                return 2;
            }

            // Return code 0
            return 0;
        }
        /// <summary>
        /// Write explicit DFN data to a FAB format file or files
        /// Data will be written for each specified intermediate stage as well as the final stage
        /// </summary>
        /// <param name="ModelName">Name of the model to output - will be included in the filenames</param>
        /// <param name="FilePath">File path to write the output file to</param>
        /// <param name="ModelGrid">Reference to the FractureGrid object containing the DFNs to be exported</param>
        /// <param name="progressReporter">Reference to progress reporter implementing the IProgressReporterWrapper interface</param>
        /// <param name="WriteuFData">Write data for microfractures in the DFN</param>
        /// <param name="WriteMFData">Write data for layer-bound macrofractures in the DFN</param>
        /// <param name="WriteUCFData">Write data for unconfined fractures in the DFN</param>
        public void WriteFABFiles(string ModelName, string FilePath, FractureGrid ModelGrid, IProgressReporterWrapper progressReporter, bool WriteuFData, bool WriteMFData, bool WriteUCFData)
        {
            // Get control data from DFNControl object
            // Number of intermediate DFNs to output and flag to control their separation
            int NoIntermediateOutputs = ModelGrid.DFNControl.NumberOfIntermediateOutputs;
            if (NoIntermediateOutputs < 0) NoIntermediateOutputs = 0;
            IntermediateOutputInterval IntermediateOutputIntervalControl = ModelGrid.DFNControl.SeparateIntermediateOutputsBy;
            // Number of cornerpoints for each microfracture
            int nouFCornerPoints = ModelGrid.DFNControl.NumberOfuFPoints;
            // Default values for fracture aperture, permeability and compressibility
            double DefaultFractureAperture = ModelGrid.DFNControl.DefaultFractureAperture;
            double DefaultFracturePermeability = ModelGrid.DFNControl.DefaultFracturePermeability;
            double DefaultFractureCompressibility = ModelGrid.DFNControl.DefaultFractureCompressibility;
            // Get the time units and time units modifier for the output labels
            double currentTime = ModelGrid.CurrentDFN.CurrentTime;
            TimeUnits timeUnits = ModelGrid.DFNControl.timeUnits;
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
            double timeUnits_Modifier = ModelGrid.DFNControl.getTimeUnitsModifier();

            // If the calculation has already been cancelled, do not write any output data
            if (!progressReporter.abortCalculation())
            {
                // Write explicit fracture property data to an FAB file
                progressReporter.OutputMessage(string.Format("Write explicit data to FAB file(s) in {0}", FilePath));

                // Set the output file extension
                string fileExtension = ".FAB";

                // Get the total number of fractures to write and update the progress bar
                int totalNoFractures = 0;
                foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                {
                    totalNoFractures += (DFN.GlobalDFNMicrofractures.Count + DFN.GlobalDFNMacrofractures.Count);
                }

                // Set the number of elements in the progress bar to twice the total number of fractures
                // We must loop through all the fractures twice - the first time to generate the fracture objects and the second to assign properties to them
                // Unless we are generating fracture centrelines in which case we will need to loop through a third time
                int numberOfElements = totalNoFractures;
                progressReporter.SetNumberOfElements(numberOfElements);
                int noFracturesGenerated = 0;

                // Loop through each stage in the fracture growth
                int stageNumber = 1;
                int NoStages = ModelGrid.DFNGrowthStages.Count;

                // Loop through each stage in the fracture growth
                foreach (GlobalDFN DFN in ModelGrid.DFNGrowthStages)
                {
                    // Create a stage-specific label and description for the output
                    string outputStageLabel;
                    string stageNameOverride = null;
                    if ((stageNameOverride is null) || (stageNameOverride.Length == 0))
                        outputStageLabel = (stageNumber == NoStages) ? "_Final" : string.Format("_Stage{0}_Time{1}{2}", stageNumber, (DFN.CurrentTime / timeUnits_Modifier).ToString("G3"), ProjectTimeUnits);
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
                            string outputFileName = FilePath + fileNameBase + fileExtension;
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

                                    // Update progress bar
                                    progressReporter.UpdateProgress(++noFracturesGenerated);
                                }

                                // Write FAB footer data to logfile
                                string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                uF_outputFile.WriteLine(footer);
                            }

                            // Close microfracture output file
                            uF_outputFile.Close();
                        }

                        // Write layer-bound fracture data to file
                        if (WriteMFData)
                        {
                            // Create file for layer-bound fractures
                            string fileNameBase = ModelName + outputStageLabel + "_LayerBoundFractures";
                            string outputFileName = FilePath + fileNameBase + fileExtension;
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

                                    // Update progress bar
                                    progressReporter.UpdateProgress(++noFracturesGenerated);
                                }

                                // Write FAB footer data to logfile
                                string footer = string.Format("{0}\r\n\r\n{1}\r\n{2}\r\n\r\n{3}\r\n{4}", "END FRACTURE", "BEGIN TESSFRACTURE", "END TESSFRACTURE", "BEGIN ROCKBLOCK", "END ROCKBLOCK");
                                MF_outputFile.WriteLine(footer);
                            }

                            // Close macrofracture  output file
                            MF_outputFile.Close();
                        }

                        // Write unconfined fracture data to file
                        if (WriteUCFData)
                        {
                            // Create file for unconfined fractures
                            string fileNameBase = ModelName + outputStageLabel + "_UnconfinedFractures";
                            string outputFileName = FilePath + fileNameBase + fileExtension;
                            StreamWriter UCF_outputFile = new StreamWriter(outputFileName);
                            outputFiles.Add(UCF_outputFile);

                            {
                                int No_UCFracs = DFN.NoUnconfinedFractureElements();
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
                                    UnconfinedFractureXYZ frac = DFN.GlobalDFNUnconfinedFractures[UCFracNo];

                                    // Set the fracture aperture
                                    double aperture = 0;// frac.MeanAperture;
                                    double permeability = 0;// Math.Pow(aperture, 2) / 12;
                                    if (double.IsNaN(aperture))
                                    {
                                        aperture = DefaultFractureAperture;
                                        permeability = DefaultFracturePermeability;
                                    }
                                    double compressibility = 0;// frac.Compressibility;
                                    if (double.IsNaN(compressibility))
                                        compressibility = DefaultFractureCompressibility;

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

                            // Close unconfined fracture output file
                            UCF_outputFile.Close();
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

                        // Increment the stage counter
                        stageNumber++;
                    }
                }
            }
        }

        // Functions to populate the shadow grid with data read from a GRDECL file
        /// <summary>
        /// Null value when reading integer data from GRDECL files
        /// </summary>
        private int NullIntegerValue;
        /// <summary>
        /// Null value when reading floating point data from GRDECL files
        /// </summary>
        private double NullFloatingPointValue;
        /// <summary>
        /// Indicates a comment in a GRDECL file; the rest of the line will be ignored
        /// </summary>
        private string CommentIndicator;
        /// <summary>
        /// Indicates the end of a data block in a GRDECL file
        /// </summary>
        private string EndBlockIndicator;
        /// <summary>
        /// Read a block of string data with a specific name from a GRDECL file and return it as a GRDECLDataBlock object
        /// </summary>
        /// <param name="BlockName">Name of the block to read</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in the block</param>
        /// <returns>A GRDECLDataBlock object containing the required data block, or an empty GRDECLDataBlock if the data block is not found</returns>
        private GRDECLDataBlock<string> ExtractStringBlockFromGRDECLData(string BlockName, string[] RawData, int MaxNumberOfDataItems)
        {
            GRDECLDataBlock<string> currentDataBlock = null;

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(CommentIndicator);
                string trimmedNextLine = (commentPosition >= 0) ? nextLine.Remove(commentPosition) : nextLine;

                // Split the line into strings separated by spaces or tabs
                string[] items = trimmedNextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // Remove any initial or final quotes
                    string trimmedItem = item.Trim('\u0022', '\u0027');

                    // If we have not already found the start of the required block, look for the specified block name
                    if (currentDataBlock is null)
                    {
                        if (trimmedItem == BlockName)
                        {
                            currentDataBlock = new GRDECLDataBlock<string>(trimmedItem);
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so return the data
                    else if ((trimmedItem == EndBlockIndicator) || (currentDataBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        return currentDataBlock;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        currentDataBlock.BlockData.Add(trimmedItem);
                    }
                }
            }

            // If we have run out of data to read, return the data read so far
            // if no data has been read, create an empty GRDECLDataBlock<string> object with name NULL and return that
            if (currentDataBlock is null)
                currentDataBlock = new GRDECLDataBlock<string>("NULL");
            return currentDataBlock;
        }
        /// <summary>
        /// Read a block of nested sub-blocks of string data from a GRDECL file and return them as a list of GRDECLDataBlock object
        /// </summary>
        /// <param name="NestedBlockName">Name of the nested outer block containing the sub-blocks</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in each sub-block</param>
        /// <returns>A list of GRDECLDataBlock objects each containing a sub-block, or an empty list of GRDECLDataBlocks if the outer nested block is not found or contains no data</returns>
        private List<GRDECLDataBlock<string>> ExtractNestedBlockFromGRDECLData(string NestedBlockName, string[] RawData, int MaxNumberOfDataItems)
        {
            // Create a new list for the nested blocks and a flag to specify whether we have found them yet
            List<GRDECLDataBlock<string>> nestedBlocks = new List<GRDECLDataBlock<string>>();
            bool readingNestedBlocks = false;

            // Create an object for the current sub-block and a flag to specify if this is being read
            GRDECLDataBlock<string> currentSubBlock = null;
            bool readingSubBlock = false;

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(CommentIndicator);
                string trimmedNextLine = (commentPosition >= 0) ? nextLine.Remove(commentPosition) : nextLine;

                // Split the line into strings separated by spaces or tabs
                string[] items = trimmedNextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // Remove any initial or final quotes
                    string trimmedItem = item.Trim('\u0022', '\u0027');

                    // If we have not already found the start of the nested blocks, look for the specified nested block name
                    if (!readingNestedBlocks)
                    {
                        // If the current item is the specified nested block name, set the flag to indicate that it has been found
                        // In either case, then move on to the next item
                        if (trimmedItem == NestedBlockName)
                            readingNestedBlocks = true;
                        continue;
                    }
                    // If we are not currently reading a sub-block, check for a sub-block block name or the block termination character
                    if (!readingSubBlock)
                    {
                        // If we read the end block indicator while not reading a sub-block, assume this refers to the outer nested blocks, and return the current list of sub-blocks
                        if (trimmedItem == EndBlockIndicator)
                        {
                            return nestedBlocks;
                        }
                        // Otherwise assume that the next item is the name of the next sub-block
                        else
                        {
                            // Create a new sub-block
                            currentSubBlock = new GRDECLDataBlock<string>(trimmedItem);
                            // Set the flag to indicate we are currently reading a sub-block; therefore the next end block indicator will refer to the subblock
                            readingSubBlock = true;
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so add it to the list of nested blocks and set the flag to indicate we are no longer reading a sub-block
                    else if ((trimmedItem == EndBlockIndicator) || (currentSubBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        nestedBlocks.Add(currentSubBlock);
                        readingSubBlock = false;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        currentSubBlock.BlockData.Add(trimmedItem);
                    }
                }
            }

            // If we have run out of data to read, return the data read so far
            // if no data has been read, we will return an empty list of blocks
            return nestedBlocks;
        }
        /// <summary>
        /// Read a block of integer data with a specific name from a GRDECL file and return it as a GRDECLDataBlock object
        /// </summary>
        /// <param name="BlockName">Name of the block to read</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in the block</param>
        /// <returns>A GRDECLDataBlock object containing the required data block, or an empty GRDECLDataBlock if the data block is not found</returns>
        private GRDECLDataBlock<int> ExtractIntegerBlockFromGRDECLData(string BlockName, string[] RawData, int MaxNumberOfDataItems)
        {
            GRDECLDataBlock<int> currentDataBlock = null;

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(CommentIndicator);
                string trimmedNextLine = (commentPosition >= 0) ? nextLine.Remove(commentPosition) : nextLine;

                // Split the line into strings separated by spaces or tabs
                string[] items = trimmedNextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // Remove any initial or final quotes
                    string trimmedItem = item.Trim('\u0022', '\u0027');

                    // If we have not already found the start of the required block, look for the specified block name
                    if (currentDataBlock is null)
                    {
                        if (trimmedItem == BlockName)
                        {
                            currentDataBlock = new GRDECLDataBlock<int>(trimmedItem);
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so return the data
                    else if ((trimmedItem == EndBlockIndicator) || (currentDataBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        return currentDataBlock;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        // Check if the current data entry is using '*' to indicate repeat values
                        if (trimmedItem.Contains('*'))
                        {
                            string[] itemSplit = trimmedItem.Split('*');
                            // Catch any errors due to invalid format or index out of range exceptions
                            int repeatNumber;
                            try
                            {
                                repeatNumber = Convert.ToInt32(itemSplit[0]);
                            }
                            catch
                            {
                                // If the repeat number is invalid, set it to 1
                                repeatNumber = 1;
                            }
                            // Catch any errors due to invalid format or index out of range exceptions
                            int value;
                            try
                            {
                                value = Convert.ToInt32(itemSplit[1]);
                            }
                            catch
                            {
                                // If the value is invalid, set it to the specified null value
                                value = NullIntegerValue;
                            }
                            for (int i = 0; i < repeatNumber; i++)
                                currentDataBlock.BlockData.Add(value);
                        }
                        // Otherwise add a single instance of the value
                        else
                        {
                            // Catch any errors due to invalid format exceptions
                            int value;
                            try
                            {
                                value = Convert.ToInt32(trimmedItem);
                            }
                            catch
                            {
                                // If the value is invalid, set it to the specified null value
                                value = NullIntegerValue;
                            }
                            currentDataBlock.BlockData.Add(value);
                        }
                    }
                }
            }

            // If we have run out of data to read, return the data read so far
            // if no data has been read, create an empty GRDECLDataBlock<int> object with name NULL and return that
            if (currentDataBlock is null)
                currentDataBlock = new GRDECLDataBlock<int>("NULL");
            return currentDataBlock;
        }
        /// <summary>
        /// Read a block of floating point data with a specific name from a GRDECL file and return it as a GRDECLDataBlock object
        /// </summary>
        /// <param name="BlockName">Name of the block to read</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in the block</param>
        /// <returns>A GRDECLDataBlock object containing the required data block, or an empty GRDECLDataBlock if the data block is not found</returns>
        private GRDECLDataBlock<double> ExtractFloatingPointBlockFromGRDECLData(string BlockName, string[] RawData, int MaxNumberOfDataItems)
        {
            GRDECLDataBlock<double> currentDataBlock = null;

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(CommentIndicator);
                string trimmedNextLine = (commentPosition >= 0) ? nextLine.Remove(commentPosition) : nextLine;

                // Split the line into strings separated by spaces or tabs
                string[] items = trimmedNextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // Remove any initial or final quotes
                    string trimmedItem = item.Trim('\u0022', '\u0027');

                    // If we have not already found the start of the required block, look for the specified block name
                    if (currentDataBlock is null)
                    {
                        if (trimmedItem == BlockName)
                        {
                            currentDataBlock = new GRDECLDataBlock<double>(trimmedItem);
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so return the data
                    else if ((trimmedItem == EndBlockIndicator) || (currentDataBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        return currentDataBlock;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        // Check if the current data entry is using '*' to indicate repeat values
                        if (trimmedItem.Contains('*'))
                        {
                            string[] itemSplit = trimmedItem.Split('*');
                            // Catch any errors due to invalid format or index out of range exceptions
                            int repeatNumber;
                            try
                            {
                                repeatNumber = Convert.ToInt32(itemSplit[0]);
                            }
                            catch
                            {
                                // If the repeat number is invalid, set it to 1
                                repeatNumber = 1;
                            }
                            // Catch any errors due to invalid format or index out of range exceptions
                            double value;
                            try
                            {
                                value = Convert.ToDouble(itemSplit[1]);
                                // Replace null values with NaN
                                if (value == NullFloatingPointValue)
                                    value = double.NaN;
                            }
                            catch
                            {
                                // If the value is invalid, set it to NaN
                                value = double.NaN;
                            }
                            for (int i = 0; i < repeatNumber; i++)
                                currentDataBlock.BlockData.Add(value);
                        }
                        // Otherwise add a single instance of the value
                        else
                        {
                            // Catch any errors due to invalid format exceptions
                            double value;
                            try
                            {
                                value = Convert.ToDouble(trimmedItem);
                                // Replace null values with NaN
                                if (value == NullFloatingPointValue)
                                    value = double.NaN;
                            }
                            catch
                            {
                                // If the value is invalid, set it to NaN
                                value = double.NaN;
                            }
                            currentDataBlock.BlockData.Add(value);
                        }
                    }
                }
            }

            // If we have run out of data to read, return the data read so far
            // if no data has been read, create an empty GRDECLDataBlock<double> object with name NULL and return that
            if (currentDataBlock is null)
                currentDataBlock = new GRDECLDataBlock<double>("NULL");
            return currentDataBlock;
        }
        /// <summary>
        /// Read all blocks of floating point data (except those with certain specific names) from a GRDECL file and return them as a GRDECLDataBlock list
        /// This is useful for reading grid property data, where the names of the properties may not be known in advance
        /// </summary>
        /// <param name="BlockNamesToSkip">List of block names to ignore</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in each block</param>
        /// <returns>A list of GRDECLDataBlock objects containing all the data blocks except thoise specified</returns>
        private List<GRDECLDataBlock<double>> ExtractFloatingPointBlocksFromGRDECLData(List<string> BlockNamesToSkip, string[] RawData, int MaxNumberOfDataItems)
        {
            List<GRDECLDataBlock<double>> dataBlocks = new List<GRDECLDataBlock<double>>();
            GRDECLDataBlock<double> currentDataBlock = null;

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(CommentIndicator);
                string trimmedNextLine = (commentPosition >= 0) ? nextLine.Remove(commentPosition) : nextLine;

                // Split the line into strings separated by spaces or tabs
                string[] items = trimmedNextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // Remove any initial or final quotes
                    string trimmedItem = item.Trim('\u0022', '\u0027');

                    // If we are not currently in a data block, check if the next item is a number, the end of block indicator or is on the list of names to skip
                    // If not, assume that it is the name of the next data block
                    if (currentDataBlock is null)
                    {
                        // First check if the next item can be converted to a double
                        // If not, assume it is a new block name
                        bool startNewBlock = false;
                        try
                        {
                            double d = Convert.ToDouble(trimmedItem);
                        }
                        catch
                        {
                            startNewBlock = true;
                        }
                        // Check if it contains * or the end block indicator
                        // If so it is not a new block name
                        if (trimmedItem.Contains('*') || trimmedItem.Contains(EndBlockIndicator))
                            startNewBlock = false;
                        // Check if it is on the list of names to skip
                        if (BlockNamesToSkip.Contains(trimmedItem))
                            startNewBlock = false;

                        // If the next item is a valid new block name, create a new GRDECLDataBlock object and add it to the list of data blocks to return
                        if (startNewBlock)
                        {
                            currentDataBlock = new GRDECLDataBlock<double>(trimmedItem);
                            dataBlocks.Add(currentDataBlock);
                        }
                    }
                    // Otherwise check for an end of block indicator; if so set the current block pointer to null
                    else if (trimmedItem == EndBlockIndicator)
                    {
                        currentDataBlock = null;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        // Check if the current data entry is using '*' to indicate repeat values
                        if (trimmedItem.Contains('*'))
                        {
                            string[] itemSplit = trimmedItem.Split('*');
                            // Catch any errors due to invalid format or index out of range exceptions
                            int repeatNumber;
                            try
                            {
                                repeatNumber = Convert.ToInt32(itemSplit[0]);
                            }
                            catch
                            {
                                // If the repeat number is invalid, set it to 1
                                repeatNumber = 1;
                            }
                            // Catch any errors due to invalid format or index out of range exceptions
                            double value;
                            try
                            {
                                value = Convert.ToDouble(itemSplit[1]);
                                // Replace null values with NaN
                                if (value == NullFloatingPointValue)
                                    value = double.NaN;
                            }
                            catch
                            {
                                // If the value is invalid, set it to NaN
                                value = double.NaN;
                            }
                            for (int i = 0; i < repeatNumber; i++)
                                currentDataBlock.BlockData.Add(value);
                        }
                        // Otherwise add a single instance of the value
                        else
                        {
                            // Catch any errors due to invalid format exceptions
                            double value;
                            try
                            {
                                value = Convert.ToDouble(trimmedItem);
                                // Replace null values with NaN
                                if (value == NullFloatingPointValue)
                                    value = double.NaN;
                            }
                            catch
                            {
                                // If the value is invalid, set it to NaN
                                value = double.NaN;
                            }
                            currentDataBlock.BlockData.Add(value);
                        }

                        // Check if we have exceeded the maximum number of data items; if so set the current block pointer to null
                        if (currentDataBlock.NoDataValues >= MaxNumberOfDataItems)
                            currentDataBlock = null;
                    }
                }
            }

            // If we have run out of data to read, return the data read so far
            return dataBlocks;
        }
        /// <summary>
        /// Reset all arrays and lists and recreate the grid
        /// This should be called when building the grid geometry from an input file
        /// </summary>
        /// <param name="NoICols_in">Number of columns (I coordinate) in the grid</param>
        /// <param name="NoJRows_in">Number of rows (J coordinate) in the grid</param>
        /// <param name="NoKLayers_in">Number of layers (K coordinate) in the grid</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified dimensions are invalid</returns>
        private int ResetGridGeometry(int NoICols_in, int NoJRows_in, int NoKLayers_in)
        {
            // If the specified dimensions are invalid return 1
            if ((NoICols_in < 1) || (NoJRows_in < 1) || (NoKLayers_in < 1))
                return 1;

            // Reset the grid dimensions and recreate the arrays of pillars and cell face types
            NoICols = NoICols_in;
            NoJRows = NoJRows_in;
            NoKLayers = NoKLayers_in;
            gridPillars = new ShadowGridPillar[NoICols_in + 1, NoJRows_in + 1];
            cellFaces = new Dictionary<GridDirection, GridPropertyArray<int>>();
            // Set all the cell face types to 0 (unfaulted)
            foreach (GridDirection face in Enum.GetValues(typeof(GridDirection)).Cast<GridDirection>())
            {
                string arrayName = string.Format("CellFace_{0}", face);
                cellFaces[face] = new GridPropertyArray<int>(arrayName, NoICols_in, NoJRows_in, NoKLayers_in, 0);
            }

            // Recreate the list of fault names
            faultNames = new List<string>();

            // Recreate lists for floating point and integer input and output properties
            // NB a new output stage must be created using CreateNewStage(StageName) before output property data can be written
            floatingPointInputProperties = new List<GridPropertyArray<double>>();
            integerInputProperties = new List<GridPropertyArray<int>>();
            floatingPointOutputProperties = new List<List<GridPropertyArray<double>>>();
            integerOutputProperties = new List<List<GridPropertyArray<int>>>();
            stageNames = new List<string>();

            // Return 0
            return 0;
        }
        /// <summary>
        /// Rebuild the pillar array
        /// </summary>
        /// <param name="PillarData">List of coordinates for the ends of the pillars in the following order: Pillar[1,1] Top X,Y,Z Bottom X,Y,Z; Pillar[2,1] Top X,Y,Z Bottom X,Y,Z; ... Pillar[1,2] Top X,Y,Z Bottom X,Y,Z; ...</param>
        /// <param name="UnitConverter">Conversion factor from the units of the supplied data to metres</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if insufficient datapoints are supplied, 2 if any datapoints are invalid</returns>
        private int RebuildPillars(List<double> PillarData, double UnitConverter)
        {
            // Check if there are enough datapoints
            if (PillarData.Count < (NoPillars * 6))
                return 1;

            // Create a counter to keep track of the position in the input data
            int dataCounter = 0;

            // Loop through the pillars in turn, creating new pillar objects and setting the pillar endpoints
            for (int pillarJ = NoJRows; pillarJ >= 0; pillarJ--)
                for (int pillarI = 0; pillarI <= NoICols; pillarI++)
                {
                    // Get the coordinates for the top of the pillar
                    // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                    double topX = PillarData[dataCounter++] * UnitConverter;
                    double topY = PillarData[dataCounter++] * UnitConverter;
                    double topZ = -PillarData[dataCounter++] * UnitConverter;
                    // Check if any values are invalid; if so return error code 2
                    if (double.IsNaN(topX) || double.IsNaN(topY) || double.IsNaN(topZ))
                        return 2;
                    PointXYZ newPillarTop = new PointXYZ(topX, topY, topZ);

                    // Get the coordinates for the top of the pillar
                    // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                    double bottomX = PillarData[dataCounter++] * UnitConverter;
                    double bottomY = PillarData[dataCounter++] * UnitConverter;
                    double bottomZ = -PillarData[dataCounter++] * UnitConverter;
                    // Check if any values are invalid; if so return error code 2
                    if (double.IsNaN(bottomX) || double.IsNaN(bottomY) || double.IsNaN(bottomZ))
                        return 2;
                    PointXYZ newPillarBottom = new PointXYZ(bottomX, bottomY, bottomZ);

                    //Create the new pillar object
                    gridPillars[pillarI, pillarJ] = new ShadowGridPillar(newPillarTop, newPillarBottom, NoKLayers);
                }

            // Return 0
            return 0;
        }
        /// <summary>
        /// Populate the pillar cornerpoint Z coordinate arrays
        /// </summary>
        /// <param name="CellCornerDepths">List of cell corner Z coordinates in the following order: Cell[1,1,1] NWtop, NEtop; Cell[2,1,1]NWtop, NEtop; ... Cell[1,1,1] SWtop, SEtop; Cell[2,1,1]SWtop, SEtop; ... Cell[1,2,1] NWtop, NEtop; Cell[2,2,1]NWtop, NEtop; ... Cell[1,1,1] NWbottom, NEbottom; Cell[2,1,1]NWbottom, NEbottom; ... Cell[1,1,2] NWtop, NEtop; Cell[2,1,2]NWtop, NEtop; ... </param>
        /// <param name="UnitConverter">Conversion factor from the units of the supplied data to metres</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if insufficient datapoints are supplied, 2 if any datapoints are invalid, 3 if an error is thrown when setting the values</returns>
        private int PopulateCellCorners(List<double> CellCornerDepths, double UnitConverter)
        {
            // Check if there are enough datapoints
            if (CellCornerDepths.Count < (NoCells * 8))
                return 1;

            // Create a counter to keep track of the position in the input data
            int dataCounter = 0;

            for (int cellK = 0; cellK < NoKLayers; cellK++)
            {
                // Get the depths for the top corners of all the cells in layer K
                for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                {
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        // Get the depth value for the NW top corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double NWTopZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(NWTopZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.NWTop, NWTopZ) > 0)
                            return 3;

                        // Get the depth value for the NE top corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double NETopZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(NETopZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.NETop, NETopZ) > 0)
                            return 3;
                    }
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        // Get the depth value for the SW top corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double SWTopZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(SWTopZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.SWTop, SWTopZ) > 0)
                            return 3;

                        // Get the depth value for the SE top corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double SETopZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(SETopZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.SETop, SETopZ) > 0)
                            return 3;
                    }
                }

                // Get the depths for the bottom corners of all the cells in layer K
                for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                {
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        // Get the depth value for the NW bottom corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double NWBottomZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(NWBottomZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.NWBottom, NWBottomZ) > 0)
                            return 3;

                        // Get the depth value for the NE bottom corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double NEBottomZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(NEBottomZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.NEBottom, NEBottomZ) > 0)
                            return 3;
                    }
                    for (int cellI = 0; cellI < NoICols; cellI++)
                    {
                        // Get the depth value for the SW bottom corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double SWBottomZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(SWBottomZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.SWBottom, SWBottomZ) > 0)
                            return 3;

                        // Get the depth value for the SE bottom corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double SEBottomZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(SEBottomZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.SEBottom, SEBottomZ) > 0)
                            return 3;
                    }
                }
            }

            // Return 0
            return 0;
        }
        /// <summary>
        /// Populate the grid with faults
        /// </summary>
        /// <param name="Faults">List of string GRDECLDataBlock items from a nested data block, containing the faulted cell faces</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the fault coordinates are in an invalid format, 2 if a fault coordinate is out of range, 3 if the fault face data is in an invalid format, 4 for any other errors</returns>
        private int AddFaultsToGrid(List<GRDECLDataBlock<string>> Faults)
        {
            // Each string in the input string list represents a sub-block describing a single fault patch
            foreach (GRDECLDataBlock<string> subblock in Faults)
            {
                // Extract data from the sub-block
                try
                {
                    // Get the fault name from the sub-block
                    string faultName = subblock.BlockName.Trim('\u0022', '\u0027');
                    // Check if it is already in the list of fault names and if so get the index number; if not add it
                    int faultIndexNo = faultNames.IndexOf(faultName);
                    if (faultIndexNo < 0)
                    {
                        faultIndexNo = faultNames.Count;
                        faultNames.Add(faultName);
                    }

                    // Get the coordinates of the fault patch represented by the sub-block
                    // The coordinates in the GRDECL files are 1-indexed so must be converted to 0-index
                    // The J coordinates must also be reversed
                    int I1 = Convert.ToInt32(subblock.BlockData[0]) - 1;
                    int I2 = Convert.ToInt32(subblock.BlockData[1]) - 1;
                    int J1 = NoJRows - Convert.ToInt32(subblock.BlockData[2]);
                    int J2 = NoJRows - Convert.ToInt32(subblock.BlockData[3]);
                    int K1 = Convert.ToInt32(subblock.BlockData[4]) - 1;
                    int K2 = Convert.ToInt32(subblock.BlockData[5]) - 1;

                    // Get the start and end I, J and K coordinates for the fault patch
                    int startI = (I1 <= I2) ? I1 : I2;
                    int endI = (I1 > I2) ? I1 : I2;
                    int startJ = (J1 <= J2) ? J1 : J2;
                    int endJ = (J1 > J2) ? J1 : J2;
                    int startK = (K1 <= K2) ? K1 : K2;
                    int endK = (K1 > K2) ? K1 : K2;

                    // Check that the coordinates all lie within the grid; if not throw an error
                    if ((startI < 0) || (startI >= NoICols))
                        throw (new ArgumentOutOfRangeException("startI", startI, string.Format("{0} value {1} is outside the grid range {2}-{3}", "startI", startI, 0, NoICols)));
                    if ((endI < startI) || (endI >= NoICols))
                        throw (new ArgumentOutOfRangeException("endI", endI, string.Format("{0} value {1} is outside the grid range {2}-{3}", "endI", endI, 0, NoICols)));
                    if ((startJ < 0) || (startJ >= NoJRows))
                        throw (new ArgumentOutOfRangeException("startJ", startJ, string.Format("{0} value {1} is outside the grid range {2}-{3}", "startJ", startJ, 0, NoJRows)));
                    if ((endJ < startJ) || (endJ >= NoJRows))
                        throw (new ArgumentOutOfRangeException("endJ", endJ, string.Format("{0} value {1} is outside the grid range {2}-{3}", "endJ", endJ, 0, NoJRows)));
                    if ((startK < 0) || (startK >= NoKLayers))
                        throw (new ArgumentOutOfRangeException("startK", startK, string.Format("{0} value {1} is outside the grid range {2}-{3}", "startK", startK, 0, NoKLayers)));
                    if ((endK < startK) || (endK >= NoKLayers))
                        throw (new ArgumentOutOfRangeException("endK", endK, string.Format("{0} value {1} is outside the grid range {2}-{3}", "endK", endK, 0, NoKLayers)));

                    // Get the face of the cell
                    GridDirection face;
                    string faceData = subblock.BlockData[6].Trim('\u0022', '\u0027');
                    switch (faceData)
                    {
                        case "X":
                        case "X+":
                            face = GridDirection.E;
                            break;
                        case "X-":
                            face = GridDirection.W;
                            break;
                        case "Y":
                        case "Y+":
                            face = GridDirection.S;
                            break;
                        case "Y-":
                            face = GridDirection.N;
                            break;
                        default:
                            return 3;
                    }

                    // Set the value of the specified face of all cells in the specified range to the fault index number
                    // This must be converted from 0-indexed to 1-indexed
                    for (int cellI = startI; cellI <= endI; cellI++)
                        for (int cellJ = startJ; cellJ <= endJ; cellJ++)
                            for (int cellK = startK; cellK <= endK; cellK++)
                                cellFaces[face].SetPropertyValue(cellI, cellJ, cellK, faultIndexNo + 1);
                }
                catch (FormatException)
                {
                    // Invalid data for fault coordinate
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Fault coordinate out of range
                    return 2;
                }
                catch (Exception)
                {
                    // Undefined error
                    return 4;
                }
            }

            // Return 0
            return 0;
        }
        /// <summary>
        /// Rebuild the shadow grid using data from a GRDECL file
        /// </summary>
        /// <param name="RawData">Data from a GRDECL file, split line by line into strings</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if dimension data is invalid, 2 if the pillar data is invalid, 3 if the cell cornerpoint data is invalid</returns>
        private int BuildGrid(string[] RawData)
        {
            // Get the grid units and create a unit converter
            // Grid units can be read from either the GRIDUNIT or the MAPUNITS data block
            // If no units are specified we will assume data is in metres
            GRDECLDataBlock<string> gridUnitData = ExtractStringBlockFromGRDECLData("GRIDUNIT", RawData, 1);
            if (gridUnitData.NoDataValues < 1)
                gridUnitData = ExtractStringBlockFromGRDECLData("MAPUNITS", RawData, 1);
            double unitCoverter = 1;
            if ((gridUnitData.NoDataValues > 0) && (gridUnitData.BlockData[0].ToUpper() == "FEET"))
                unitCoverter = 0.3048;

            // Get the grid size and create a new set of pillars
            // Dimensions can be read from either the SPECGRID or DIMENS data block
            GRDECLDataBlock<int> dimensions = ExtractIntegerBlockFromGRDECLData("SPECGRID", RawData, 3);
            if (dimensions.NoDataValues < 3)
                dimensions = ExtractIntegerBlockFromGRDECLData("DIMENS", RawData, 3);
            // If dimension data cannot be found or is invalid, return error code 1
            if (dimensions.NoDataValues < 3)
                return 1;
            else if (ResetGridGeometry(dimensions.BlockData[0], dimensions.BlockData[1], dimensions.BlockData[2]) > 0)
                return 1;

            // Get the pillar geometry and rebuild the pillar grid
            GRDECLDataBlock<double> pillars = ExtractFloatingPointBlockFromGRDECLData("COORD", RawData, NoPillars * 6);
            // If pillar data cannot be found or is invalid, return error code 2
            if (RebuildPillars(pillars.BlockData, unitCoverter) > 0)
                return 2;

            // Get the cell cornerpoint depths and add them to the pillars
            GRDECLDataBlock<double> cellCorners = ExtractFloatingPointBlockFromGRDECLData("ZCORN", RawData, NoCells * 8);
            // If the cell cornerpoint data cannot be found or is invalid, return error code 3
            if (PopulateCellCorners(cellCorners.BlockData, unitCoverter) > 0)
                return 3;

            // Get the fault data and modify the cell faces accordingly
            // NB since the fault data is stored in nested blocks, we will need to extract all of these and pass them into the appropriate function
            List<GRDECLDataBlock<string>> faults = ExtractNestedBlockFromGRDECLData("FAULTS", RawData, 7);
            // If the fault data is invalid, return error code 4
            if (AddFaultsToGrid(faults) > 0)
                return 4;

            // If there are no errors, return 0
            return 0;
        }
        /// <summary>
        /// Populate the grid with property data
        /// </summary>
        /// <param name="RawData">Data from a GRDECL file, split line by line into strings</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if no property data was found, 2 if there were load errors in one or more property (some property data may have been loaded)</returns>
        private int PopulateProperties(string[] RawData)
        {
            // Create a list of keywords defining the geometry
            // Get a list of data blocks with any name other than the geometry keywords
            List<string> keywords = new List<string> { "GRIDUNIT", "MAPUNITS", "METRES", "FEET", "SPECGRID", "DIMENS", "COORD", "ZCORN", "FAULTS", "MAPAXES", "MAP", "F", "COORDSYS" };
            List<GRDECLDataBlock<double>> propertyBlocks = ExtractFloatingPointBlocksFromGRDECLData(keywords, RawData, NoCells);
            // If no data was found, return 1
            if (propertyBlocks.Count == 0)
                return 1;

            // We will assume that all extracted data blocks with any name other than the geometry keywords that have the required number of values are grid properties
            // We will assume they are floating point values unless they are on a specific list of defined integer properties
            int loadErrors = 0;
            List<string> integerProperties = new List<string> { "FACIES", "ACTNUM", "LAYERS" };
            foreach (GRDECLDataBlock<double> propertyBlock in propertyBlocks)
            {
                // Check if it has the required number of values; if not move on to the next block
                if (propertyBlock.NoDataValues < NoCells)
                {
                    loadErrors++;
                    continue;
                }

                // Get the property name and values
                string propertyName = propertyBlock.BlockName;
                List<double> propertyData = propertyBlock.BlockData;

                // Check if the property name is on the specified list of integer properties
                bool convertToInteger = integerProperties.Exists(x => x == propertyName);
                if (convertToInteger)
                {
                    // Create a new integer grid property object
                    GridPropertyArray<int> newProperty = new GridPropertyArray<int>(propertyName, NoICols, NoJRows, NoKLayers);

                    // Create counters to keep track of the position in the input data and the number of bad data values
                    int dataCounter = 0;
                    int badValues = 0;

                    // Loop through all the cells in the grid and assign values from the data block - NB we must loop through the J cells in reverse order
                    for (int cellK = 0; cellK < NoKLayers; cellK++)
                        for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                            for (int cellI = 0; cellI < NoICols; cellI++)
                            {
                                // Convert the data from a double to an int; if it fails or is NaN, set it to the integer default null value
                                double nextFPvalue = propertyData[dataCounter++];
                                int nextValue;
                                if (double.IsNaN(nextFPvalue))
                                {
                                    nextValue = NullIntegerValue;
                                }
                                else
                                {
                                    try
                                    {
                                        nextValue = Convert.ToInt32(nextFPvalue);
                                    }
                                    catch
                                    {
                                        nextValue = NullIntegerValue;
                                        badValues++;
                                    }
                                }
                                newProperty.SetPropertyValue(cellI, cellJ, cellK, nextValue);
                            }

                    // Add the new property to the integer input property list
                    integerInputProperties.Add(newProperty);

                    // If there were bad data values, increment the load error list
                    if (badValues > 0)
                        loadErrors++;
                }
                // Otherwise assume the property is a floating point property
                else
                {
                    // Create a new floating point grid property object
                    GridPropertyArray<double> newProperty = new GridPropertyArray<double>(propertyName, NoICols, NoJRows, NoKLayers);

                    // Create a counter to keep track of the position in the input data
                    int dataCounter = 0;

                    // Loop through all the cells in the grid and assign values from the data block - NB we must loop through the J cells in reverse order
                    for (int cellK = 0; cellK < NoKLayers; cellK++)
                        for (int cellJ = NoJRows - 1; cellJ >= 0; cellJ--)
                            for (int cellI = 0; cellI < NoICols; cellI++)
                            {
                                double nextFPvalue = propertyData[dataCounter++];
                                newProperty.SetPropertyValue(cellI, cellJ, cellK, nextFPvalue);
                            }

                    // Add the new proeprty to the integer input property list
                    floatingPointInputProperties.Add(newProperty);
                }
            }

            // Return 0 if there were no load errors, otherwise 2
            if (loadErrors == 0)
                return 0;
            else
                return 2;
        }
        /// <summary>
        /// Open a data file and read the data line by line into a string array
        /// Also open and read data from any include files in the specified file, recursively
        /// </summary>
        /// <param name="FileNameWithExtension">Filename for the file to read, including any extension but not including the file path</param>
        /// <param name="FilePath">Filepath for the file to read</param>
        /// <param name="data">List of strings to read the file data into</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if there were read errors (some data may have been read)</returns>
        private int ReadDataFile(string FileNameWithExtension, string FilePath, ref List<string> data)
        {
            // Open the specified GRDECL file and write each line into a string array
            // Check each line for INCLUDE files, if so open and read them also
            int returnCode = 0;
            string includeKeyword = "INCLUDE";
            string fullFileName = FilePath + FileNameWithExtension;
            try
            {
                foreach (string nextLine in File.ReadAllLines(fullFileName))
                {
                    if (nextLine.StartsWith(includeKeyword))
                    {
                        // If we find an include file, get the filename and call ReadGRDECLFile recursively to read it
                        string[] items = nextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        int NoItems = items.Length;
                        // Find the include file name
                        for (int itemNo = 0; itemNo < (NoItems - 1); itemNo++)
                            if (items[itemNo].ToUpper() == includeKeyword)
                            {
                                string includeFileName = items[itemNo + 1];
                                // Get the include file path if specified;; otherwise assume it is in the same folder as this file
                                string includeFilePath = FilePath;
                                // Get the include file return code
                                int includeFileReturnCode = 1;
                                if (includeFileName.Contains('\u005C'))
                                {
                                    includeFilePath = includeFileName.Substring(0, includeFileName.LastIndexOf('\u005C') + 1);
                                }
                                // Read the include file the exit from the loop
                                includeFileReturnCode = ReadDataFile(includeFileName, includeFilePath, ref data);
                                if (includeFileReturnCode > 0)
                                    returnCode = 1;
                                break;
                            }
                    }
                    else
                    {
                        // Otherwise just append the line of text to the array of data
                        data.Add(nextLine);
                    }
                }
            }
            catch
            {
                returnCode = 1;
            }

            return returnCode;
        }
        /// <summary>
        /// Read data from a GRDECL file and use it to rebuild the grid and populate it with properties
        /// </summary>
        /// <param name="FileName">Filename for the file to read, not including the GRDECL extension or the file path</param>
        /// <param name="FilePath">Filepath for the file to read</param>
        /// <returns>Return code: 0 if the operation was successful, 2 if there were errors reading the data, 3 if there were errors building the grid, 4 if there were errors populating the grid properties</returns>
        public ShadowGridErrorStatus LoadGRDECLFile(string FileName, string FilePath)
        {
            ShadowGridErrorStatus returnCode = ShadowGridErrorStatus.DataLoadedOK;

            // Read data from the specified GRDECL file into a string array
            // Check each line for INCLUDE files, if so open and read them also
            string fileExtension = ".GRDECL";
            string fileNamePlusExtension = FileName + fileExtension;
            // The data will be read as a List object and then converted to an Array; this is because we do not yet know the number of lines in the file
            string[] RawData;
            {
                List<string> FileData = new List<string>();
                // If there are errors reading the data, set the return code to 1
                if (ReadDataFile(FileName, FilePath, ref FileData) > 0)
                    returnCode = ShadowGridErrorStatus.ErrorReadingFile;
                RawData = FileData.ToArray();
            }

            // Recreate the grid and set the grid geometry using the data read from the specified GRDECL file
            // If there are errors recreating the grid, set the return code to 2
            if (BuildGrid(RawData) > 0)
                returnCode = ShadowGridErrorStatus.ErrorBuildingGrid;

            // Populate the grid with property data read from the specified GRDECL file
            // If there are errors reading the property data, set the return code to 3
            if (PopulateProperties(RawData) > 0)
                returnCode = ShadowGridErrorStatus.ErrorPopulatingProperties;

            // Return the return code
            return returnCode;
        }

        // Constructors
        /// <summary>
        /// Read data from a GRDECL file and use it to build the grid and populate it with properties
        /// </summary>
        /// <param name="FileName">Filename for the file to read, not including the GRDECL extension or the file path</param>
        /// <param name="FilePath">Filepath for the file to read</param>
        public ShadowGrid(string FileName, string FilePath, GridFileType FileType) : this()
        {
            switch (FileType)
            {
                case GridFileType.GRDECL:
                    // Read data from the specified GRDECL file and use it to rebuild the grid and load grid properties
                    ErrorStatus = LoadGRDECLFile(FileName, FilePath);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Basic constructor to instantiate the required arrays and lists
        /// This should not be called directly but from another constructor that builds the grid geometry from an input file
        /// </summary>
        private ShadowGrid()
        {
            // Set the null values
            NullIntegerValue = -999;
            NullFloatingPointValue = -999.99;

            // Set the comment and end block indicators
            CommentIndicator = "--";
            EndBlockIndicator = "/";

            // Create all the required list objects, as well as empty arrays of pillars and cell face types
            ResetGridGeometry(0, 0, 0);

            // Set the error status to NoDataLoaded
            ErrorStatus = ShadowGridErrorStatus.NoDataLoaded;
        }
    }
}
