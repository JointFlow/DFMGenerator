using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DFMGenerator_SharedCode;

namespace DFMGenerator_Compatible
{
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
            catch (IndexOutOfRangeException e)
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
            catch (IndexOutOfRangeException e)
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
    class ShadowGrid_GRDECL : IDataTransferToDFMGenerator
    {
        // Grid geometry
        // Grid is zero-indexed from SWtop corner
        // I increases towards E
        // J increases towards N
        // K increases downwards
        // NB J coordinate direction is the opposite to that in most GRDECL files
        /// <summary>
        /// Number of columns (I coordinate) in the grid
        /// </summary>
        private int NoICols { get; set; }
        /// <summary>
        /// Number of rows (J coordinate) in the grid
        /// </summary>
        private int NoJRows { get; set; }
        /// <summary>
        /// Number of layers (K coordinate) in the grid
        /// </summary>
        private int NoKLayers { get; set; }
        /// <summary>
        /// Total number of cells in the grid
        /// </summary>
        private int NoCells { get { return NoICols * NoJRows * NoKLayers; } }
        /// <summary>
        /// Total number of pillars in the grid
        /// </summary>
        private int NoPillars { get { return (NoICols + 1) * (NoJRows + 1); } }
        /// <summary>
        /// Array of grid pillars
        /// </summary>
        private ShadowGridPillar[,] gridPillars;
        /// <summary>
        /// Arrays of cell face types (0=unfaulted, 1=faulted)
        /// </summary>
        private Dictionary<GridDirection, GridPropertyArray<int>> cellFaces;

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
            GridPropertyArray<double> prop = floatingPointInputProperties.Find(x => x.PropertyName == PropertyName);
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
            GridPropertyArray<int> prop = integerInputProperties.Find(x => x.PropertyName == PropertyName);
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
            unitData += string.Format("MAPUNITS\t-- Generated: {0}\n", GeometryDataSource);
            unitData += string.Format("  METRES /\n\n");
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
            axisData += string.Format("MAPAXES\t-- Generated: {0}\n", GeometryDataSource);
            axisData += string.Format("  {0} {1} {2} {3} {4} {5}/\n\n", gridOrigin.X, gridOrigin.Y + 1000, gridOrigin.X, gridOrigin.X, gridOrigin.X + 1000, gridOrigin.Y);
            return axisData;
        }
        /// <summary>
        /// Get a string representing the grid unit data in GRDECL format
        /// </summary>
        /// <returns>A string representing the grid unit data in GRDECL format</returns>
        private string GetGridUnitsInGRDECLFormat()
        {
            string unitData = "";
            unitData += string.Format("GRIDUNIT\t-- Generated: {0}\n", GeometryDataSource);
            unitData += string.Format("  METRES MAP/\n\n");
            return unitData;
        }
        /// <summary>
        /// Get a string representing the dimension data for the entire grid in GRDECL format
        /// </summary>
        /// <returns>A string representing the dimension data for the entire grid in GRDECL format</returns>
        private string GetGridDimensionsInGRDECLFormat()
        {
            string dimensionData = "";
            //dimensionData += string.Format("DIMENS\t-- Generated: {0}\n", GeometryDataSource);
            //dimensionData += string.Format("  {0} {1} {2}/\n\n", NoICols, NoJRows, NoKLayers);
            dimensionData += string.Format("SPECGRID\t-- Generated: {0}\n", GeometryDataSource);
            dimensionData += string.Format("  {0} {1} {2} {3} {4}/\n\n", NoICols, NoJRows, NoKLayers, 1, "F");
            return dimensionData;
        }
        /// <summary>
        /// Get a string representing the grid coordinate system data in GRDECL format
        /// </summary>
        /// <returns>A string representing the grid coordinate system data in GRDECL format</returns>
        private string GetCoordinateSystemInGRDECLFormat()
        {
            string coordinateSystemData = "";
            coordinateSystemData += string.Format("COORDSYS\t-- Generated: {0}\n", GeometryDataSource);
            coordinateSystemData += string.Format("  {0} {1}/\n\n", 1, 5);
            return coordinateSystemData;
        }
        /// <summary>
        /// Get a string representing the dimension data for a specified stratigraphic interval of the grid in GRDECL format
        /// </summary>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <returns>A string representing the dimension data for a specified stratigraphic interval of the grid in GRDECL format</returns>
        private string GetGridDimensionsInGRDECLFormat(int TopLayerK, int BottomLayerK)
        {
            int noLayers = TopLayerK - BottomLayerK + 1;
            string dimensionData = "";
            //dimensionData += string.Format("DIMENS\t-- Generated: {0}\n", GeometryDataSource);
            //dimensionData += string.Format("  {0} {1} {2}/\n\n", NoICols, NoJRows, noLayers);
            dimensionData += string.Format("SPECGRID\t-- Generated: {0}\n", GeometryDataSource);
            dimensionData += string.Format("  {0} {1} {2} {3} {4}/\n\n", NoICols, NoJRows, noLayers, 1, "F");
            return dimensionData;
        }
        /// <summary>
        /// Get a string representing the grid pillar endpoints in GRDECL format
        /// </summary>
        /// <returns>A string representing the grid pillar endpoints in GRDECL format</returns>
        private string GetPillarsInGRDECLFormat()
        {
            string pillarData = string.Format("COORD\t-- Generated : {0}\n", GeometryDataSource);
            for (int pillarJ = NoJRows; pillarJ >= 0; pillarJ--)
                for (int pillarI = 0; pillarI <= NoICols; pillarI++)
                {
                    PointXYZ PillarTop = gridPillars[pillarI, pillarJ].PillarTop;
                    PointXYZ PillarBottom = gridPillars[pillarI, pillarJ].PillarBottom;
                    pillarData += string.Format("  {0} {1} {2} {3} {4} {5}\n", PillarTop.X, PillarTop.Y, PillarTop.Depth, PillarBottom.X, PillarBottom.Y, PillarBottom.Depth);
                }
            pillarData += "  /\n\n";
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
            string cornerDepthData = string.Format("ZCORN\t-- Generated : {0}\n ", GeometryDataSource);
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
            cornerDepthData += " /\n\n";
            return cornerDepthData;
        }
        /// <summary>
        /// Get a string representing the Actnum values of all the cells in the grid in GRDECL format, assuming all cells are active (Actnum value 1)
        /// </summary>
        /// <returns>A string representing the Actnum values of all the cells in the grid in GRDECL format</returns>
        private string GetActnumDataInGRDECLFormat()
        {
            string actnumData = string.Format("ACTNUM\t-- Generated : {0}\n  ", GeometryDataSource);
            actnumData += string.Format("  {0}*1  /\n\n", NoCells);
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
            string actnumData = string.Format("ACTNUM\t-- Generated : {0}\n  ", GeometryDataSource);
            actnumData += string.Format("  {0}*1  /\n\n", NoICols * NoJRows * (TopLayerK - BottomLayerK + 1));
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
            string propertyData = string.Format("{0}\t-- Generated : {1}\n ", Property.PropertyName, Source);

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
            propertyData += " /\n\n";
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
            string propertyData = string.Format("{0}\t-- Generated : {1}\n ", Property.PropertyName, Source);

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
            propertyData += " /\n\n";
            return propertyData;
        }
        /// <summary>
        /// Write grid geometry and implicit output data of a specified output stage for a specified stratigraphic interval to a GRDECL format file
        /// </summary>
        /// <param name="ModelName">Name of the model to output - will be included in the filename</param>
        /// <param name="FilePath">File path to write the output file to</param>
        /// <param name="progressReporter">Reference to progress reporter implementing the IProgressReporterWrapper interface</param>
        /// <param name="Stage">Stage of the model output to write to the GRDECL file</param>
        /// <param name="TopLayerK">K index of the uppermost layer of the stratigraphic interval to be exported</param>
        /// <param name="BottomLayerK">K index of the lowermost layer of the stratigraphic interval to be exported</param>
        /// <param name="IncludeGridGeometry">Flag to include grid geometry data in the GRDECL file</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified stage does not exist, 2 if there is an error writing to the specified file</returns>
        public int WriteGRDECL(string ModelName, string FilePath, IProgressReporterWrapper progressReporter, int Stage, int TopLayerK, int BottomLayerK, bool IncludeGridGeometry, bool PopulateEmptyGridblocks)
        {
            try
            {
                // Check if the specified top and bottom layers are within range; if not adjust them to lie within the grid
                if (TopLayerK < 0) TopLayerK = 0;
                if (BottomLayerK >= NoKLayers) BottomLayerK = NoKLayers - 1;
                if (TopLayerK > BottomLayerK) TopLayerK = BottomLayerK;

                // Get the lists of output properties for the soecified stage and the stage name
                // If the specified stage is out of range create an error message and return code 1
                if ((Stage < 0) || (Stage >= NoStages))
                {
                    progressReporter.OutputMessage(string.Format("There is no output data for stage {0}", Stage));
                    progressReporter.OutputMessage("No output files will be written");
                    return 1;
                }
                List<GridPropertyArray<double>> floatingPointPropertiesToOutput = floatingPointOutputProperties[Stage];
                List<GridPropertyArray<int>> integerPropertiesToOutput = integerOutputProperties[Stage];
                string stageName = stageNames[Stage];

                // If the calculation has already been cancelled, do not write any output data
                if (!progressReporter.abortCalculation())
                {
                    // Write implicit fracture property data to an Eclipse GRDECL file or files
                    progressReporter.OutputMessage(string.Format("Write implicit data for stage {0} to GRDECL file(s)", Stage));

                    // Set the output file name and extension
                    string fileName = ModelName + "_" + stageName;
                    string fileExtension = ".GRDECL";
                    string fullFileName = FilePath + fileName + fileExtension;

                    // Create the output file
                    StreamWriter outputFile = new StreamWriter(fullFileName);

                    // Write header
                    string headerInfo = "";
                    headerInfo += string.Format("-- Generated [\n");
                    headerInfo += string.Format("--Format      : Eclipse keywords(grid geometry and properties)(ASCII)\n");
                    headerInfo += string.Format("-- Exported by: {0}\n", FractureGrid.VersionNumber);
                    headerInfo += string.Format("-- User name: {0}\n", "");
                    headerInfo += string.Format("-- Date: {0}\n", System.DateTime.Now);
                    headerInfo += string.Format("-- Project: {0}\n", ModelName);
                    headerInfo += string.Format("-- Grid: \n");
                    headerInfo += string.Format("-- Generated ]\n\n");
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
        /// Read a block of string data with a specific name from a GRDECL file and return it as a GRDECLDataBlock object
        /// </summary>
        /// <param name="BlockName">Name of the block to read</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in the block</param>
        /// <returns>A GRDECLDataBlock object containing the required data block, or an empty GRDECLDataBlock if the data block is not found</returns>
        private GRDECLDataBlock<string> ExtractStringBlockFromGRDECLData(string BlockName, string[] RawData, int MaxNumberOfDataItems)
        {
            GRDECLDataBlock<string> currentDataBlock = null;
            string commentIndicator = "--";
            string endBlockIndicator = "/";

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(commentIndicator);
                if (commentPosition >= 0) nextLine.Remove(commentPosition);

                // Split the line into strings separated by spaces or tabs
                string[] items = nextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // If we have not already found the start of the required block, look for the specified block name
                    if (currentDataBlock is null)
                    {
                        if (item == BlockName)
                        {
                            currentDataBlock = new GRDECLDataBlock<string>(item);
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so return the data
                    else if ((item == endBlockIndicator) || (currentDataBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        return currentDataBlock;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        currentDataBlock.BlockData.Add(item);
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
        /// Read a block of integer data with a specific name from a GRDECL file and return it as a GRDECLDataBlock object
        /// </summary>
        /// <param name="BlockName">Name of the block to read</param>
        /// <param name="RawData">Entire contents of the GRDECL file as a string array</param>
        /// <param name="MaxNumberOfDataItems">Maximum number of data items allowed in the block</param>
        /// <returns>A GRDECLDataBlock object containing the required data block, or an empty GRDECLDataBlock if the data block is not found</returns>
        private GRDECLDataBlock<int> ExtractIntegerBlockFromGRDECLData(string BlockName, string[] RawData, int MaxNumberOfDataItems)
        {
            GRDECLDataBlock<int> currentDataBlock = null;
            string commentIndicator = "--";
            string endBlockIndicator = "/";

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(commentIndicator);
                if (commentPosition >= 0) nextLine.Remove(commentPosition);

                // Split the line into strings separated by spaces or tabs
                string[] items = nextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // If we have not already found the start of the required block, look for the specified block name
                    if (currentDataBlock is null)
                    {
                        if (item == BlockName)
                        {
                            currentDataBlock = new GRDECLDataBlock<int>(item);
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so return the data
                    else if ((item == endBlockIndicator) || (currentDataBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        return currentDataBlock;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        // Check if the current data entry is using '*' to indicate repeat values
                        if (item.Contains('*'))
                        {
                            string[] itemSplit = item.Split('*');
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
                                value = Convert.ToInt32(item);
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
            string commentIndicator = "--";
            string endBlockIndicator = "/";

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(commentIndicator);
                if (commentPosition >= 0) nextLine.Remove(commentPosition);

                // Split the line into strings separated by spaces or tabs
                string[] items = nextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // If we have not already found the start of the required block, look for the specified block name
                    if (currentDataBlock is null)
                    {
                        if (item == BlockName)
                        {
                            currentDataBlock = new GRDECLDataBlock<double>(item);
                        }
                    }
                    // Otherwise check if we have reached the end of the block; if so return the data
                    else if ((item == endBlockIndicator) || (currentDataBlock.NoDataValues >= MaxNumberOfDataItems))
                    {
                        return currentDataBlock;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        // Check if the current data entry is using '*' to indicate repeat values
                        if (item.Contains('*'))
                        {
                            string[] itemSplit = item.Split('*');
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
                                value = Convert.ToDouble(item);
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
        /// <param name="NullValue">Null value; will be replaced with NaN</param>
        /// <returns>A list of GRDECLDataBlock objects containing all the data blocks except thoise specified</returns>
        private List<GRDECLDataBlock<double>> ExtractFloatingPointBlocksFromGRDECLData(List<string> BlockNamesToSkip, string[] RawData, int MaxNumberOfDataItems, double NullValue)
        {
            List<GRDECLDataBlock<double>> dataBlocks = new List<GRDECLDataBlock<double>>();
            GRDECLDataBlock<double> currentDataBlock = null;
            string commentIndicator = "--";
            string endBlockIndicator = "/";

            // Loop through each line in the input datafile
            foreach (string nextLine in RawData)
            {
                // First remove any data after a comment indicator
                int commentPosition = nextLine.IndexOf(commentIndicator);
                if (commentPosition >= 0) nextLine.Remove(commentPosition);

                // Split the line into strings separated by spaces or tabs
                string[] items = nextLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Loop through each item in turn
                foreach (string item in items)
                {
                    // If we are not currently in a data block, check if the next item is a number, the end of block indicator or is on the list of names to skip
                    // If not, assume that it is the name of the next data block
                    if (currentDataBlock is null)
                    {
                        // First check if the next item can be converted to a double
                        // If not, assume it is a new block name
                        bool startNewBlock = false;
                        try
                        {
                            double d = Convert.ToDouble(item);
                        }
                        catch
                        {
                            startNewBlock = true;
                        }
                        // Check if it contains * or the end block indicator
                        // If so it is not a new block name
                        if (item.Contains('*') || item.Contains(endBlockIndicator))
                            startNewBlock = false;
                        // Check if it is on the list of names to skip
                        if (BlockNamesToSkip.Contains(item))
                            startNewBlock = false;

                        // If the next item is a valid new block name, create a new GRDECLDataBlock object and add it to the list of data blocks to return
                        if (startNewBlock)
                        {
                            currentDataBlock = new GRDECLDataBlock<double>(item);
                            dataBlocks.Add(currentDataBlock);
                        }
                    }
                    // Otherwise check for an end of block indicator; if so set the current block pointer to null
                    else if (item == endBlockIndicator) 
                    {
                        currentDataBlock = null;
                    }
                    // Otherwise add the data to the block
                    else
                    {
                        // Check if the current data entry is using '*' to indicate repeat values
                        if (item.Contains('*'))
                        {
                            string[] itemSplit = item.Split('*');
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
                                if (value == NullValue)
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
                                value = Convert.ToDouble(item);
                                // Replace null values with NaN
                                if (value == NullValue)
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
        private int ResetGridGeometry (int NoICols_in, int NoJRows_in, int NoKLayers_in)
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

            for (int cellK = 0; cellK <= NoKLayers; cellK++)
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
                        if (gridPillars[cellI, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.NWTop, NWTopZ) > 0)
                            return 3;

                        // Get the depth value for the NE top corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double NETopZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(NETopZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.NETop, NETopZ) > 0)
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
                        if (gridPillars[cellI, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.SWTop, SWTopZ) > 0)
                            return 3;

                        // Get the depth value for the SE top corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double SETopZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(SETopZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.SETop, SETopZ) > 0)
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
                        if (gridPillars[cellI, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.NWBottom, NWBottomZ) > 0)
                            return 3;

                        // Get the depth value for the NE bottom corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double NEBottomZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(NEBottomZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ].SetLayerZCoordinate(cellK, GridblockCornerpoint.NEBottom, NEBottomZ) > 0)
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
                        if (gridPillars[cellI, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.SWBottom, SWBottomZ) > 0)
                            return 3;

                        // Get the depth value for the SE bottom corner of the current cell
                        // NB since Z coordinates in GRDECL files are specified increasing downwards, they will need to be inverted to match the PointXYZ specification
                        double SEBottomZ = -CellCornerDepths[dataCounter++] * UnitConverter;
                        // Check if the value is invalid; if so return error code 2
                        if (double.IsNaN(SEBottomZ))
                            return 2;
                        // Otherwise add it to the appropriate pillar
                        if (gridPillars[cellI + 1, cellJ + 1].SetLayerZCoordinate(cellK, GridblockCornerpoint.SEBottom, SEBottomZ) > 0)
                            return 3;
                    }
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

            // If there are no errors, return 0
            return 0;
        }

        private int PopulateProperties(string[] RawData)
        {

        }


        // Constructors

        /// <summary>
        /// Basic constructor to instantiate the required arrays and lists
        /// This should not be called directly but from another constructor that builds the grid geometry from an input file
        /// </summary>
        /// <param name="NoICols_in">Number of columns (I coordinate) in the grid</param>
        /// <param name="NoJRows_in">Number of rows (J coordinate) in the grid</param>
        /// <param name="NoKLayers_in">Number of layers (K coordinate) in the grid</param>
        private ShadowGrid_GRDECL(int NoICols_in, int NoJRows_in, int NoKLayers_in)
        {
            // Set the null values
            NullIntegerValue = -999;
            NullFloatingPointValue = -999.99;

            // Set the grid dimensions and create the arrays of pillars and cell face types
            ResetGridGeometry( NoICols_in,  NoJRows_in,  NoKLayers_in);
        }
    }
}
