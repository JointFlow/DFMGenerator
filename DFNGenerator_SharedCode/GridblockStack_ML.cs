using System;
using System.Collections.Generic;
using System.Text;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Vertical stack of GridblockConfiguration_ML objects
    /// </summary>
    class GridblockStack_ML
    {
        // References to external objects
        /// <summary>
        /// Reference to parent FractureGrid object - this does not need to be filled as the GridblockConfiguration can also function as a standalone object
        /// </summary>
        private FractureGrid gd;

        // Stack data
        /// <summary>
        /// 1D array containing GridblockConfiguration_ML objects for each cell in the stack
        /// </summary>
        private List<GridblockConfiguration_ML> Gridblocks;
        /// <summary>
        /// Lists of discrete macrofracture segments in IJK coordinates, one per fracture set - represent macrofracture component of local stack DFN
        /// </summary>
        private List<List<MacrofractureSegmentIJK_ML>> LocalDFNMacrofractureSegments;
        /// <summary>
        /// Return reference to a specific gridblock in the stack
        /// </summary>
        /// <param name="LayerNo">Layer number of gridblock to retrieve</param>
        /// <returns>Reference to the specified GridblockConfiguration_ML object</returns>
        public GridblockConfiguration_ML GetGridblock(int LayerNo)
        {
            return Gridblocks[LayerNo];
        }
        /// <summary>
        /// Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax
        /// </summary>
        public int NoFractureSets { get; private set; }
        /// <summary>
        /// Number of layers (i.e. gridblocks) in the stack
        /// </summary>
        public int NoLayers { get { return Gridblocks.Count; } }

        // Objects containing geomechanical, fracture property and calculation data relating to the gridblock stack
        /// <summary>
        /// Control data for calculating fracture propagation
        /// </summary>
        public PropagationControl PropControl { get; private set; }

        // Functions to calculate fracture population data and generate the DFN
        /// <summary>
        /// Grow the explicit DFN for the next timestep, based on data from the implicit model calculation, cached in the appropriate FCD_list objects
        /// </summary>
        public void PropagateDFN()
        {

        }

        // Reset and data input functions
        /// <summary>
        /// Set the parent FractureGrid object
        /// </summary>
        /// <param name="grid_in">Reference to parent FractureGrid_ML object</param>
        public void setParentGrid(FractureGrid_ML grid_in)
        {
            gd = grid_in;
        }
        /// <summary>
        /// Add a GridblockConfiguration_ML object to a specified cell in the stack and set up references to adjacent cells. The specified cell in the stack must already exist.
        /// </summary>
        /// <param name="gridblock_in">GridblockConfiguration_ML object to add to the grid</param>
        /// <param name="LayerNo">Layer number of cell to place it in (zero referenced)</param>
        public void AddGridblock(GridblockConfiguration_ML gridblock_in, int LayerNo)
        {
            AddGridblock(gridblock_in, LayerNo, true, true);
        }
        /// <summary>
        /// Add a GridblockConfiguration object to a specified cell in the grid, and set up references to adjecent cells if required. The specified cell in the grid must already exist.
        /// </summary>
        /// <param name="gridblock_in">GridblockConfiguration object to add to the grid</param>
        /// <param name="LayerNo">Layer number of cell to place it in (zero referenced)</param>
        /// <param name="ConnectToNeighbourAbove">Flag to connect to overlying gridblock; if false, the top corners of this gridblock will not match the bottom corners of the overlying gridblock</param>
        /// <param name="ConnectToNeighbourBelow">Flag to connect to underlying gridblock; if false, the bottom corners of this gridblock will not match the top corners of the underlying gridblock</param>
        public void AddGridblock(GridblockConfiguration_ML gridblock_in, int LayerNo, bool ConnectToNeighbourAbove, bool ConnectToNeighbourBelow)
        {
            // Check to see if the layer number is within the bounds of the stack
            if (LayerNo < 0) LayerNo = 0;
            if (LayerNo >= Gridblocks.Count) LayerNo = Gridblocks.Count - 1;

            // Add the GridblockConfiguration_ML object to the grid and set the parent references in the GridblockConfiguration_ML object
            Gridblocks[LayerNo] = gridblock_in;
            gridblock_in.setParentStack(this, gd);

            // Set the references to the adjacent gridblocks and cornerpoints: cornerpoints always reference to the overlying neighbour gridblocks

            // Check if there is a cell above
            if (ConnectToNeighbourAbove)
            {
                if ((LayerNo > 0) && (Gridblocks[LayerNo - 1] != null))
                {
                    // Get reference to overlying gridblock
                    GridblockConfiguration_ML overlying_neighbour = Gridblocks[LayerNo - 1];

                    // Set mutual references to neighbouring gridblocks
                    overlying_neighbour.VerticalNeighbourGridblocks[VerticalGridDirection.Down] = gridblock_in;
                    gridblock_in.VerticalNeighbourGridblocks[VerticalGridDirection.Up] = overlying_neighbour;

                    // Overwrite the upper cornerpoints with those of the overlying neighbour
                    gridblock_in.OverwriteGridblockCorners(VerticalGridDirection.Up, overlying_neighbour.SWbottom, overlying_neighbour.NWbottom, overlying_neighbour.NEbottom, overlying_neighbour.SEbottom);
                }
            }

            // Check if there is a cell below
            if (ConnectToNeighbourBelow)
            {
                if ((LayerNo < Gridblocks.Count-1) && (Gridblocks[LayerNo + 1] != null))
                {
                    // Get reference to underlying gridblock
                    GridblockConfiguration_ML underlying_neighbour = Gridblocks[LayerNo + 1];

                    // Set mutual references to neighbouring gridblocks
                    underlying_neighbour.VerticalNeighbourGridblocks[VerticalGridDirection.Up] = gridblock_in;
                    gridblock_in.VerticalNeighbourGridblocks[VerticalGridDirection.Down] = underlying_neighbour;

                    // Overwrite the upper cornerpoints of the underlying neighbour with the lower cornerpoints of this gridblock
                    underlying_neighbour.OverwriteGridblockCorners(VerticalGridDirection.Up, gridblock_in.SWbottom, gridblock_in.NWbottom, gridblock_in.NEbottom, gridblock_in.SEbottom);
                }
            }
        }

        // Constructors
        /// <summary>
        /// Constructor - create an M layer tall GridblockStack_ML filled with null objects, and an empty DFN object with the specified number of fracture sets
        /// </summary>
        /// <param name="NoLayers"></param>
        /// <param name="NoFractureSets_in">Number of fracture sets</param>
        public GridblockStack_ML(int NoLayers, int NoFractureSets_in) : this(NoFractureSets_in)
        {
            for (int LayerNo = 1; LayerNo <= NoLayers; LayerNo++)
                Gridblocks.Add(null);
        }
        /// <summary>
        /// Constructor - create an empty GridblockStack_ML object and an empty DFN object with the specified number of fracture sets
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets</param>
        public GridblockStack_ML(int NoFractureSets_in)
        {
            // Create an empty grid object
            Gridblocks = new List<GridblockConfiguration_ML>();

            // Create a new fracture propagation control object
            PropControl = new PropagationControl();

            // Set the number of fracture sets (minimum 0)
            if (NoFractureSets_in < 0)
                NoFractureSets_in = 0;
            NoFractureSets = NoFractureSets_in;

            /// Create empty lists for discrete macrofracture segments in IJK coordinates, one per fracture set
            LocalDFNMacrofractureSegments = new List<List<MacrofractureSegmentIJK_ML>>();
            for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
            {
                List<MacrofractureSegmentIJK_ML> setDFNMacrofractureSegments = new List<MacrofractureSegmentIJK_ML>();
                LocalDFNMacrofractureSegments.Add(setDFNMacrofractureSegments);
            }
        }
        /// <summary>
        /// Default constructor - create an empty GridblockStack_ML object and an empty DFN object with 2 fracture sets
        /// </summary>
        public GridblockStack_ML() : this (2)
        {
            // Defaults:

            // Set the number of fracture sets to 2, for two orthogonal sets perpendicular to ehmin and ehmax
        }
    }
}
