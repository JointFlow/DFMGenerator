using System;
using System.Collections.Generic;
using System.Text;

namespace DFNGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for global grid directions (N = Y+, E = X+, S = Y-, W = X-)
    /// </summary>
    public enum VerticalGridDirection { Up, Down, None }


    /// <summary>
    /// Class representing a gridblock in a multilayer grid
    /// </summary>
    class GridblockConfiguration_ML : GridblockConfiguration
    {
        // References to external objects
        /// <summary>
        /// Reference to parent GridblockStack_ML object - this does not need to be filled as the GridblockConfiguration_ML can also function as a standalone object
        /// </summary>
        private GridblockStack_ML gs;

        // References to adjacent gridblocks
        /// <summary>
        /// Dictionary containing references to GridblockConfiguration objects above and below
        /// </summary>
        public Dictionary<VerticalGridDirection, GridblockConfiguration> VerticalNeighbourGridblocks;

        // Reset and data input functions
        /// <summary>
        /// Set the parent FractureGrid object
        /// </summary>
        /// <param name="grid_in">Reference to parent FractureGrid object</param>
        public void setParentStack(GridblockStack_ML stack_in, FractureGrid grid_in)
        {
            gs = stack_in;
            setParentGrid(grid_in);
        }
        /// <summary>
        /// Overwrite the cornerpoints on one of the gridblock boundaries with new cornerpoints, and recalculate gridblock geometry
        /// </summary>
        /// <param name="sideToOverwrite">Side of the gridblock to overwrite</param>
        /// <param name="leftTopCornerPoint">Reference to PointXYZ object for top left hand corner (looking out from the gridblock)</param>
        /// <param name="leftBottomCornerPoint">Reference to PointXYZ object for bottom left hand corner (looking out from the gridblock)</param>
        /// <param name="rightTopCornerPoint">Reference to PointXYZ object for top right hand corner (looking out from the gridblock)</param>
        /// <param name="rightBottomCornerPoint">Reference to PointXYZ object for bottom right hand corner (looking out from the gridblock)</param>
        public void OverwriteGridblockCorners(VerticalGridDirection surfaceToOverwrite, PointXYZ SW_corner_in, PointXYZ NW_corner_in, PointXYZ NE_corner_in, PointXYZ SE_corner_in)
        {
            // Overwite the existing cornerpoints with the supplied references
            switch (surfaceToOverwrite)
            {
                case VerticalGridDirection.Up:
                    {
                        SWtop = SW_corner_in;
                        NWtop = NW_corner_in;
                        NEtop = NE_corner_in;
                        SEtop = SE_corner_in;
                    }
                    break;
                case VerticalGridDirection.Down:
                    {
                        SWbottom = SW_corner_in;
                        NWbottom = NW_corner_in;
                        NEbottom = NE_corner_in;
                        SEbottom = SE_corner_in;
                    }
                    break;
                case VerticalGridDirection.None:
                    break;
                default:
                    break;
            }

            // Recalculate gridblock geometry and volume
            recalculateGeometry();

            // Set the centrepoints of corner pillars in local (IJK) coordinates, for each fracture set
            foreach (Gridblock_FractureSet fs in FractureSets)
            {
                fs.setCornerPoints();
            }
        }

        // Constructors
        /// <summary>
        /// Constructor: specify layer thickness and depth, and the number of fracture sets, but create empty fracture sets
        /// </summary>
        /// <param name="thickness_in">Layer thickness at time of deformation (m)</param>
        /// <param name="depth_in">Depth at time of deformation (m)</param>
        /// <param name="NoFractureSets">Number of fracture sets</param>
        public GridblockConfiguration_ML(double thickness_in, double depth_in, int NoFractureSets_in)
            : base(thickness_in, depth_in, NoFractureSets_in) // Call the GridblockConfiguration constructor
        {
            // Create a dictionary of adjacent gridblocks above and below and fill with null references (except for reference to this gridblock)
            VerticalNeighbourGridblocks = new Dictionary<VerticalGridDirection, GridblockConfiguration>();
            VerticalNeighbourGridblocks.Add(VerticalGridDirection.Up, null);
            VerticalNeighbourGridblocks.Add(VerticalGridDirection.Down, null);
            VerticalNeighbourGridblocks.Add(VerticalGridDirection.None, this);
        }
    }
}
