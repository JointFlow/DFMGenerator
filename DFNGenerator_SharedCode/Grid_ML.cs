using System;
using System.Collections.Generic;
using System.Text;

namespace DFNGenerator_SharedCode
{
    class FractureGrid_ML : FractureGrid
    {
        // Grid data
        /// <summary>
        /// 2D array containing GridblockStack_ML objects for each gridblock stack in the grid
        /// </summary>
        private List<List<GridblockStack_ML>> GridblockStacks;
        /// <summary>
        /// Return reference to a specific stack in the grid
        /// </summary>
        /// <param name="RowNo">Row number of stack to retrieve</param>
        /// <param name="ColNo">Column number of stack to retrieve</param>
        /// <returns>Reference to the specified GridblockStack_ML object</returns>
        public GridblockStack_ML GetGridblockStack(int RowNo, int ColNo)
        {
            return GridblockStacks[RowNo][ColNo];
        }
        /// <summary>
        /// Return reference to a specific gridblock in the grid
        /// </summary>
        /// <param name="RowNo">Row number of gridblock to retrieve</param>
        /// <param name="ColNo">Column number of gridblock to retrieve</param>
        /// <param name="LayerNo">layer number of gridblock to retrieve</param>
        /// <returns>Reference to the specified GridblockConfiguration_ML object</returns>
        public GridblockConfiguration_ML GetGridblock(int RowNo, int ColNo, int LayerNo)
        {
            return GridblockStacks[RowNo][ColNo].GetGridblock(LayerNo);
        }

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

        // Reset and data input functions
        /// <summary>
        /// Add a GridblockStack_ML object to a specified position in the grid, and set up references between adjecent gridblocks. The specified stack in the grid must already exist.
        /// </summary>
        /// <param name="gridblock_in">GridblockConfiguration object to add to the grid</param>
        /// <param name="RowNo">Row number of position to place it in (zero referenced)</param>
        /// <param name="ColNo">Column number of position to place it in (zero referenced)</param>
        public void AddGridblockStack(GridblockStack_ML gridblockStack_in, int RowNo, int ColNo)
        {
            AddGridblockStack(gridblockStack_in, RowNo, ColNo, true, true, true, true);
        }
        /// <summary>
        /// Add a GridblockStack_ML object to a specified position in the grid, and set up references between adjecent gridblocks if required. The specified stack in the grid must already exist.
        /// </summary>
        /// <param name="gridblockStack_in">GridblockConfiguration object to add to the grid</param>
        /// <param name="RowNo">Row number of position to place it in (zero referenced)</param>
        /// <param name="ColNo">Column number of position to place it in (zero referenced)</param>
        /// <param name="connectToWesternNeighbour">Flag to connect to western neighbouring gridblock stack; if false, the western corners of this gridblock will not match the eastern corners of the neighbouring gridblock</param>
        /// <param name="ConnectToSouthernNeighbour">Flag to connect to southern neighbouring gridblock stack; if false, the southern corners of this gridblock will not match the northern corners of the neighbouring gridblock</param>
        /// <param name="connectToEasternNeighbour">Flag to connect to eastern neighbouring gridblock stack; if false, the eastern corners of this gridblock will not match the western corners of the neighbouring gridblock</param>
        /// <param name="ConnectToNorthernNeighbour">Flag to connect to northern neighbouring gridblock stack; if false, the northern corners of this gridblock will not match the southern corners of the neighbouring gridblock</param>
        public void AddGridblockStack(GridblockStack_ML gridblockStack_in, int RowNo, int ColNo, bool connectToWesternNeighbour, bool ConnectToSouthernNeighbour, bool connectToEasternNeighbour, bool ConnectToNorthernNeighbour)
        {
            // Check to see if the row number is within the bounds of the grid
            if (RowNo < 0) RowNo = 0;
            if (RowNo >= GridblockStacks.Count) RowNo = GridblockStacks.Count - 1;

            // Get the row object
            List<GridblockStack_ML> GridRow = GridblockStacks[RowNo];

            // Check to see if the column number is within the bounds of the grid
            if (ColNo < 0) ColNo = 0;
            if (ColNo >= GridRow.Count) ColNo = GridRow.Count - 1;

            // Add the GridblockConfiguration object to the grid and set the parent reference in the GridblockConfiguration object
            GridRow[ColNo] = gridblockStack_in;
            gridblockStack_in.setParentGrid(this);

            // Set the references between the adjacent gridblocks and cornerpoints: cornerpoints always reference to the southern and western neighbour gridblocks
            // Get the number of layers
            int NoLayers = gridblockStack_in.NoLayers;

            // Check if there is a stack to the west
            if (connectToWesternNeighbour)
            {
                if ((ColNo > 0) && (GridRow[ColNo - 1] != null))
                {
                    // Get reference to western neighbour gridblock stack
                    GridblockStack_ML W_neighbourStack = GridRow[ColNo - 1];
                    int NoNeighbourLayers = W_neighbourStack.NoLayers;

                    // Loop through each layer in the stack
                    for (int LayerNo = 0; (LayerNo < NoLayers) && (LayerNo < NoNeighbourLayers); LayerNo++)
                    {
                        // Get the respective gridblock objects and check they exist
                        GridblockConfiguration_ML gridblock_in = gridblockStack_in.GetGridblock(LayerNo);
                        GridblockConfiguration_ML W_neighbour = W_neighbourStack.GetGridblock(LayerNo);
                        if ((gridblock_in == null) || (W_neighbour == null))
                            continue;

                        // Set mutual references to neighbouring gridblocks
                        W_neighbour.NeighbourGridblocks[GridDirection.E] = gridblock_in;
                        gridblock_in.NeighbourGridblocks[GridDirection.W] = W_neighbour;

                        // Overwrite the western cornerpoints with those of the western neighbour
                        gridblock_in.OverwriteGridblockCorners(GridDirection.W, W_neighbour.SEtop, W_neighbour.SEbottom, W_neighbour.NEtop, W_neighbour.NEbottom);
                    }
                }
            }

            // Check if there is a stack to the south
            if (ConnectToSouthernNeighbour)
            {
                if (RowNo > 0)
                {
                    List<GridblockStack_ML> RowToS = GridblockStacks[RowNo - 1];
                    if ((ColNo < RowToS.Count) && (RowToS[ColNo] != null))
                    {
                        // Get reference to southern neighbour gridblock stack
                        GridblockStack_ML S_neighbourStack = RowToS[ColNo];
                        int NoNeighbourLayers = S_neighbourStack.NoLayers;

                        // Loop through each layer in the stack
                        for (int LayerNo = 0; (LayerNo < NoLayers) && (LayerNo < NoNeighbourLayers); LayerNo++)
                        {
                            // Get the respective gridblock objects and check they exist
                            GridblockConfiguration_ML gridblock_in = gridblockStack_in.GetGridblock(LayerNo);
                            GridblockConfiguration_ML S_neighbour = S_neighbourStack.GetGridblock(LayerNo);
                            if ((gridblock_in == null) || (S_neighbour == null))
                                continue;

                            // Set mutual references to neighbouring gridblocks
                            S_neighbour.NeighbourGridblocks[GridDirection.N] = gridblock_in;
                            gridblock_in.NeighbourGridblocks[GridDirection.S] = S_neighbour;

                            // Overwrite the southern cornerpoints with those of the southern neighbour
                            gridblock_in.OverwriteGridblockCorners(GridDirection.S, S_neighbour.NEtop, S_neighbour.NEbottom, S_neighbour.NWtop, S_neighbour.NWbottom);
                        }
                    }
                }
            }

            // Check if there is a stack to the east
            if (connectToEasternNeighbour)
            {
                if ((ColNo < GridRow.Count - 1) && (GridRow[ColNo + 1] != null))
                {
                    // Get reference to eastern neighbour gridblock stack
                    GridblockStack_ML E_neighbourStack = GridRow[ColNo + 1];
                    int NoNeighbourLayers = E_neighbourStack.NoLayers;

                    // Loop through each layer in the stack
                    for (int LayerNo = 0; (LayerNo < NoLayers) && (LayerNo < NoNeighbourLayers); LayerNo++)
                    {
                        // Get the respective gridblock objects and check they exist
                        GridblockConfiguration_ML gridblock_in = gridblockStack_in.GetGridblock(LayerNo);
                        GridblockConfiguration_ML E_neighbour = E_neighbourStack.GetGridblock(LayerNo);
                        if ((gridblock_in == null) || (E_neighbour == null))
                            continue;

                        // Set mutual references to neighbouring gridblocks
                        E_neighbour.NeighbourGridblocks[GridDirection.W] = gridblock_in;
                        gridblock_in.NeighbourGridblocks[GridDirection.E] = E_neighbour;

                        // Overwrite the western cornerpoints of the eastern neighbour with the eastern cornerpoints of this gridblock
                        E_neighbour.OverwriteGridblockCorners(GridDirection.W, gridblock_in.SEtop, gridblock_in.SEbottom, gridblock_in.NEtop, gridblock_in.NEbottom);
                    }
                }
            }

            // Check if there is a stack to the north
            if (ConnectToNorthernNeighbour)
            {
                if (RowNo < GridblockStacks.Count - 1)
                {
                    List<GridblockStack_ML> RowToN = GridblockStacks[RowNo + 1];
                    if ((ColNo < RowToN.Count) && (RowToN[ColNo] != null))
                    {
                        // Get reference to northern neighbour gridblock stack
                        GridblockStack_ML N_neighbourStack = RowToN[ColNo];
                        int NoNeighbourLayers = N_neighbourStack.NoLayers;

                        // Loop through each layer in the stack
                        for (int LayerNo = 0; (LayerNo < NoLayers) && (LayerNo < NoNeighbourLayers); LayerNo++)
                        {
                            // Get the respective gridblock objects and check they exist
                            GridblockConfiguration_ML gridblock_in = gridblockStack_in.GetGridblock(LayerNo);
                            GridblockConfiguration_ML N_neighbour = N_neighbourStack.GetGridblock(LayerNo);
                            if ((gridblock_in == null) || (N_neighbour == null))
                                continue;

                            // Set mutual references to neighbouring gridblocks
                            N_neighbour.NeighbourGridblocks[GridDirection.S] = gridblock_in;
                            gridblock_in.NeighbourGridblocks[GridDirection.N] = N_neighbour;

                            // Overwrite the southern cornerpoints of the northern neighbour with the northern cornerpoints of this gridblock
                            N_neighbour.OverwriteGridblockCorners(GridDirection.S, gridblock_in.NEtop, gridblock_in.NEbottom, gridblock_in.NWtop, gridblock_in.NWbottom);
                        }
                    }
                }
            }
        }

        // Constructors
        /// <summary>
        /// Constructor - create an MxN FractureGrid and fill with null objects
        /// </summary>
        /// <param name="NoRows"></param>
        /// <param name="NoCols"></param>
        public FractureGrid_ML(int NoRows, int NoCols) : this()
        {
            for (int RowNo = 1; RowNo <= NoRows; RowNo++)
            {
                List<GridblockStack_ML> GridRow = new List<GridblockStack_ML>();

                for (int ColNo = 1; ColNo <= NoCols; ColNo++)
                    GridRow.Add(null);

                GridblockStacks.Add(GridRow);
            }
        }
        /// <summary>
        /// Default constructor - create an empty FractureGrid object and an empty DFN object
        /// </summary>
        public FractureGrid_ML() : base ()
        {
            // Create an empty grid object
            GridblockStacks = new List<List<GridblockStack_ML>>();
        }
    }
}
