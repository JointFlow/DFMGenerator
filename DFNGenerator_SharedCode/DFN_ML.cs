using System;
using System.Collections.Generic;
using System.Text;

namespace DFNGenerator_SharedCode
{
    class MacrofractureSegmentIJK_ML : MacrofractureSegmentIJK
    {
        // References to external objects

        // Macrofracture segment geometry data
        /// <summary>
        /// Height above the fracture segment above the nucleation point (Non-Prop node) 
        /// </summary>
        double UpperHeight { get; set; }
        /// <summary>
        /// Height above the fracture segment below the nucleation point (Non-Prop node) 
        /// </summary>
        double LowerHeight { get; set; }

        // Constructors
        /// <summary>
        /// Constructor: specify the nucleation point in fracture set (IJK) coordinates, and the propagation direction, azimuth and dip, and nucleation time, for a fracture nucleating within a gridblock
        /// </summary>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="nucleationPoint">Reference to a pointIJK object specifying the fracture nucleation point - the new MacrofractureSegmentIJK will use this reference for the non-propagating node and create a copy for the propagating node</param>
        /// <param name="current_propdir_in">Direction of fracture propagation in local IJK coordinate frame (IPlus = strike direction)</param>
        /// <param name="original_propdir_in">Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated (IPlus = strike direction)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture segment nucleation</param>
        public MacrofractureSegmentIJK_ML(int FractureDipSetIndex_in, PointIJK nucleationPoint, PropagationDirection current_propdir_in, PropagationDirection original_propdir_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
            : base (null, FractureDipSetIndex_in, nucleationPoint, current_propdir_in, original_propdir_in, dipdir_in, currentLTime, currentTimestep)
        {
        }
        /// <summary>
        /// Constructor: specify the nucleation point in grid (XYZ) coordinates, and the propagation direction, azimuth and dip, and nucleation time, for a fracture nucleating within a gridblock
        /// </summary>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="nucleationPoint">Reference to a pointXYZ object specifying the fracture nucleation point - the new MacrofractureSegmentIJK will create a new PointIJK object for each node</param>
        /// <param name="current_propdir_in">Direction of fracture propagation in local IJK coordinate frame (IPlus = strike direction)</param>
        /// <param name="original_propdir_in">Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated (IPlus = strike direction)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture segment nucleation</param>
        public MacrofractureSegmentIJK_ML(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointXYZ nucleationPoint, PropagationDirection current_propdir_in, PropagationDirection original_propdir_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
            : this(FractureDipSetIndex_in, fs_in.convertXYZtoIJK(nucleationPoint), current_propdir_in, original_propdir_in, dipdir_in, currentLTime, currentTimestep)
        {
            // Reference to parent FractureSet object
            // Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
            // Set reference to corresponding FractureDipSet object based on index supplied
            // Initially, both the non-propagating and propagating nodes will be at the nucleation point - create new PointIJK objects based on supplied PointXYZ object
            // Direction of fracture propagation in local IJK coordinate frame - this will not change after fracture segment is initiated
            // Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated - this will not change after fracture segment is initiated
            // Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
            // Fracture dip - this is taken from the corresponding fracture dip set so need not be set here
            // Flag to specify the non-propagating node type - since the fracture is nucleating within a gridblock, this will be set to NucleationPoint
            // Set the initial fracture state and propagating node type to active (still propagating)
            // Set the propagating node boundary and the tracking boundary to none
            // Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
            // Timestep of fracture nucleation - this will not change after fracture is initiated
        }


    }
}
