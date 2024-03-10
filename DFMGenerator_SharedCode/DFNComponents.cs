//#define MODIFY_FRAC_WIDTH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for different macrofracture tip types
    /// </summary>
    public enum FractureTipType { Propagating, StressShadow, Intersection, Convergence, OutOfBounds, Pinchout }
    /// <summary>
    /// Enumerator for different macrofracture segment node types
    /// </summary>
    public enum SegmentNodeType { NucleationPoint, Propagating, ConnectedStressShadow, NonconnectedStressShadow, Intersection, Convergence, ConnectedGridblockBound, NonconnectedGridblockBound, Relay, Pinchout }
    /// <summary>
    /// Enumerator for direction of propagation of macrofracture segment
    /// </summary>
    public enum PropagationDirection { IPlus, IMinus }
    /// <summary>
    /// Enumerator for dip direction of propagation of fracture in IJK coordinates
    /// </summary>
    public enum DipDirection { JPlus, JMinus }
    /// <summary>
    /// Enumerator to control fracture sort criterion
    /// </summary>
    public enum SortProperty { Size_SmallestFirst, Size_LargestFirst, NucleationTime }

    /// <summary>
    /// Skeleton of a discrete circular microfracture lying within the layer, referenced in local (IJK) coordinates - use in local (gridblock) DFN
    /// </summary>
    class MicrofractureIJK
    {
        // References to external objects
        /// <summary>
        /// Reference to parent FractureSet object
        /// </summary>
        private Gridblock_FractureSet fs;
        /// <summary>
        /// Reference to corresponding FractureDipSet object
        /// </summary>
        private FractureDipSet fds;
        /// <summary>
        /// Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
        /// </summary>
        public int FractureDipSetIndex { get; private set; }
        /// <summary>
        /// Reference to corresponding microfracture object in the global (grid) DFN
        /// </summary>
        private MicrofractureXYZ uf_global;
        /// <summary>
        /// Create a new MicrofractureXYZ object linked to this object, and populate it using data from this object
        /// </summary>
        /// <param name="setIndex">Index number of the set that the fracture object belongs to</param>
        /// <returns>Reference to linked MicrofractureXYZ object</returns>
        public MicrofractureXYZ createLinkedGlobalMicrofracture(int setIndex)
        {
            // Create a new MicrofractureXYZ object linking to and using data from this object
            MicrofractureXYZ globalMicrofracture = new MicrofractureXYZ(this, setIndex);

            // Set the reference in this object to the corresponding microfracture object in the global (grid) DFN to point to the new MicrofractureXYZ object
            uf_global = globalMicrofracture;

            // Return reference to the new MicrofractureXYZ object
            return globalMicrofracture;
        }

        // Microfracture geometry data
        /// <summary>
        /// Centrepoint of the fracture in IJK coordinates
        /// </summary>
        public PointIJK CentrePoint { get; set; }
        /// <summary>
        /// Fracture radius
        /// </summary>
        public double Radius { get; set; }
        /// <summary>
        /// Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
        /// </summary>
        public DipDirection DipDir { get; private set; }
        /// <summary>
        /// Fracture dip - this is taken from the corresponding fracture dip set and will not change after fracture is initiated
        /// </summary>
        public double Dip { get { return fds.Dip; } }
        /// <summary>
        /// Get the fracture dip azimuth referenced to N
        /// </summary>
        /// <returns>Azimuth (rad)</returns>
        public double getAzimuth()
        {
            double azimuth = double.NaN;

            // Segment azimuth will be equal to fracture strike +/- pi/2 depending on dip direction
            switch (DipDir)
            {
                case DipDirection.JPlus:
                    azimuth = fs.Strike + (Math.PI / 2);
                    break;
                case DipDirection.JMinus:
                    azimuth = fs.Strike - (Math.PI / 2);
                    break;
                default:
                    break;
            }

            // Reset azimuth to lie within the range 0 to 2pi if it is outside that range
            while (azimuth >= (2 * Math.PI)) azimuth -= (2 * Math.PI);
            while (azimuth < 0) azimuth += (2 * Math.PI);

            // Return azimuth
            return azimuth;
        }
        /// <summary>
        /// Get the centrepoint position in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the microfracture centrepoint</returns>
        public PointXYZ getCentrePointinXYZ()
        {
            return fs.convertIJKtoXYZ(CentrePoint);
        }
        /// <summary>
        /// Mean fracture aperture in the current stress field (averaged across fracture surface)
        /// </summary>
        /// <returns></returns>
        public double MeanAperture() { return fds.getMeanMicrofractureAperture(Radius); }
        /// <summary>
        /// Fracture compressibility, based on the aperture control data
        /// </summary>
        /// <returns></returns>
        public double Compressibility() { return fds.getMicrofractureCompressibility(Radius); }

        // Microfracture dynamic data
        /// <summary>
        /// Fracture state - true if the fracture is still propagating
        /// </summary>
        public bool Active { get; set; }
        /// <summary>
        /// Flag for macrofracture nucleation - true if this fracture has nucleated a new macrofracture
        /// </summary>
        public bool NucleatedMacrofracture { get; set; }
        /// <summary>
        /// Weighted time of fracture nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
        /// </summary>
        public double NucleationLTime { get; private set; }
        /// <summary>
        /// Get the real time of fracture segment nucleation
        /// </summary>
        /// <returns>Nucleation time (s)</returns>
        public double getNucleationTime()
        {
            return fds.ConvertLengthToTime(NucleationLTime, NucleationTimestep);
        }
        /// <summary>
        /// Timestep of fracture nucleation - this will not change after fracture is initiated
        /// </summary>
        public int NucleationTimestep { get; private set; }

        // Reset and data input functions
        /// <summary>
        /// Remove this MacrofractureSegmentIJK object from the relevant Gridblock.FractureSet list
        /// </summary>
        public void RemoveLocalFractureFromGridblock()
        {
            fs.LocalDFNMicrofractures.Remove(this);
        }

        // Constructors
        /// <summary>
        /// Constructor: specify the nucleation point in fracture set (IJK) coordinates, and the radius, azimuth and dip, and nucleation time
        /// </summary>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="nucleationPoint">Reference to a pointIJK object specifying the fracture nucleation point - the new MicrofractureIJK will use this reference as the centrepoint</param>
        /// <param name="radius_in">Initial fracture radius (m)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture nucleation</param>
        public MicrofractureIJK(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointIJK nucleationPoint, double radius_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
        {
            // Reference to parent FractureSet object
            fs = fs_in;
            // Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
            FractureDipSetIndex = FractureDipSetIndex_in;
            // Set reference to corresponding FractureDipSet object based on index supplied
            fds = fs.FractureDipSets[FractureDipSetIndex];
            // Centrepoint of the fracture in IJK coordinates - use supplied PointIJK object
            CentrePoint = nucleationPoint;
            // Fracture radius
            Radius = radius_in;
            // Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
            DipDir = dipdir_in;
            // Fracture dip - this is taken from the corresponding fracture dip set so need not be set here
            // Set initial fracture state to active (still propagating)
            Active = true;
            // Set flag for macrofracture nucleation to false - this fracture has not nucleated a new macrofracture
            NucleatedMacrofracture = false;
            // Weighted time of fracture nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
            NucleationLTime = currentLTime;
            // Timestep of fracture nucleation - this will not change after fracture is initiated
            NucleationTimestep = currentTimestep;
        }
        /// <summary>
        /// Constructor: specify the nucleation point in grid (XYZ) coordinates, and the radius, azimuth and dip, and nucleation time
        /// </summary>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="nucleationPoint">Reference to a pointXYZ object specifying the fracture nucleation point - the new MicrofractureIJK will create a new PointIJK object to use as the centrepoint</param>
        /// <param name="radius_in">Initial fracture radius (m)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture nucleation</param>
        public MicrofractureIJK(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointXYZ nucleationPoint, double radius_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
            : this(fs_in, FractureDipSetIndex_in, fs_in.convertXYZtoIJK(nucleationPoint), radius_in, dipdir_in, currentLTime, currentTimestep)
        {
            // Reference to parent FractureSet object
            // Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
            // Set reference to corresponding FractureDipSet object based on index supplied
            // Centrepoint of the fracture in IJK coordinates - create a new PointIJK object based on supplied PointXYZ object
            // Fracture radius
            // Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
            // Fracture dip - this is taken from the corresponding fracture dip set so need not be set here
            // Set initial fracture state to active (still propagating)
            // Weighted time of fracture nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
            // Timestep of fracture nucleation - this will not change after fracture is initiated
        }
    }

    /// <summary>
    /// Discrete circular microfracture lying within the layer, referenced in global (XYZ) coordinates - use in global (grid) DFN
    /// </summary>
    class MicrofractureXYZ : IComparable<MicrofractureXYZ>
    {
        // Unique microfracture ID number
        /// <summary>
        /// Global microfracture counter - used to set an ID for each new microfracture object
        /// </summary>
        private static int microfractureCounter = 0;
        /// <summary>
        /// Unique microfracture ID 
        /// </summary>
        public int MicrofractureID { get; private set; }
        
        // References to external objects
        /// <summary>
        /// Reference to corresponding microfracture object in the local (gridblock) DFN
        /// </summary>
        private MicrofractureIJK uF_local;

        // Microfracture geometry data - this will be fixed since all propagation calculations are carried out on the local DFNs
        /// <summary>
        /// Centrepoint of the fracture in XYZ coordinates
        /// </summary>
        public PointXYZ CentrePoint { get; private set; }
        /// <summary>
        /// Fracture radius
        /// </summary>
        public double Radius { get; private set; }
        /// <summary>
        /// Fracture set index number - this will not change after fracture is initiated
        /// </summary>
        public int SetIndex { get; private set; }
        /// <summary>
        /// Fracture dip azimuth - this will not change after fracture is initiated
        /// </summary>
        public double Azimuth { get; private set; }
        /// <summary>
        /// Fracture dip - this will not change after fracture is initiated
        /// </summary>
        public double Dip { get; private set; }
        /// <summary>
        /// Fracture aperture, averaged across the fracture surface
        /// </summary>
        public double MeanAperture { get; private set; }
        /// <summary>
        /// Fracture compressibility, based on the aperture control data
        /// </summary>
        public double Compressibility { get; private set; }

        // Microfracture dynamic data - this will be fixed since all propagation calculations are carried out on the local DFNs
        /// <summary>
        /// Flag to determine if the fracture is still propagating
        /// </summary>
        public bool Active { get; private set; }
        /// <summary>
        /// Flag for macrofracture nucleation - true if this fracture has nucleated a new macrofracture
        /// </summary>
        public bool NucleatedMacrofracture { get; private set; }
        /// <summary>
        /// Time of fracture nucleation (real time) - this will not change after fracture is initiated
        /// </summary>
        public double NucleationTime { get; private set; }

        // Reset, data input, control and implementation functions
        /// <summary>
        /// Set the microfracture geometry and dynamic data based on the corresponding microfracture object in the local (gridblock) DFN
        /// </summary>
        public void PopulateData()
        {
            // Microfracture geometry data - this will be fixed from object initialisation since all propagation calculations are carried out on the local DFNs
            // Centrepoint of the fracture in XYZ coordinates
            CentrePoint = uF_local.getCentrePointinXYZ();
            // Fracture radius
            Radius = uF_local.Radius;
            // Fracture dip azimuth - this was set at object initialisation and will not have changed
            // Fracture dip - this was set at object initialisation and will not have changed
            // Set the mean fracture aperture based on the current stress field
            MeanAperture = uF_local.MeanAperture();
            // Set tracture compressibility based on the aperture control data
            Compressibility = uF_local.Compressibility();

            // Microfracture dynamic data - this will be fixed since all propagation calculations are carried out on the local DFNs
            // Flag to determine if the fracture is still propagating
            Active = uF_local.Active;
            // Flag for macrofracture nucleation
            NucleatedMacrofracture = uF_local.NucleatedMacrofracture;
        }
        /// <summary>
        /// Remove the MicrofractureSegmentIJK object from the relevant Gridblock.FractureSet list
        /// </summary>
        public void DeleteLocalFracture()
        {
            uF_local.RemoveLocalFractureFromGridblock();
        }
        /// <summary>
        /// Criterion to use when sorting microfractures
        /// </summary>
        public static SortProperty SortCriterion { get; set; }
        /// <summary>
        /// Compare MicrofractureXYZ objects based on the sort criterion specified by SortCriterion
        /// </summary>
        /// <param name="that">MicrofractureXYZ object to compare with</param>
        /// <returns>Negative if this MicrofractureXYZ object has the greatest value of the specified property, positive if that MicrofractureXYZ object has the greatest value of the specified property, zero if they have equal values</returns>
        public int CompareTo(MicrofractureXYZ that)
        {
            switch (SortCriterion)
            {
                case SortProperty.Size_SmallestFirst:
                    return this.Radius.CompareTo(that.Radius);
                case SortProperty.Size_LargestFirst:
                    return -this.Radius.CompareTo(that.Radius);
                case SortProperty.NucleationTime:
                    return this.NucleationTime.CompareTo(that.NucleationTime);
                default:
                    return 0;
            }
        }

        // Output functions
        /// <summary>
        /// Function to return a list of the XYZ coordinates of a series of points around the circumference of the fracture
        /// </summary>
        /// <param name="nouFCornerPoints">Number of circumferential points to return</param>
        /// <returns>A list of cornerpoints as PointXYZ objects</returns>
        public List<PointXYZ> GetFractureCornerpointsInXYZ(int nouFCornerPoints)
        {
            // Create a new list of PointXYZ objects for the cornerpoints
            List<PointXYZ> FractureCornerPoints = new List<PointXYZ>();

            // Loop through each point and add the coordinates to the list
            for (int pointNo = 0; pointNo < nouFCornerPoints; pointNo++)
            {
                // Get the sine and cosine of the cornerpoint relative to the fracture
                double pointAngle = 2 * Math.PI * ((double)pointNo / (double)nouFCornerPoints);
                double pointSin = VectorXYZ.Sin_trim(pointAngle);
                double pointCos = VectorXYZ.Cos_trim(pointAngle);

                // Calculate useful local variables
                double sinAzi = VectorXYZ.Sin_trim(Azimuth);
                double cosAzi = VectorXYZ.Cos_trim(Azimuth);
                double sinDip = VectorXYZ.Sin_trim(Dip);
                double cosDip = VectorXYZ.Cos_trim(Dip);

                // Calculate cornerpoint coordinates
                double point_I = pointSin * Radius;
                double point_J = -(cosDip * pointCos * Radius);
                double point_X = CentrePoint.X - (point_I * cosAzi) + (point_J * sinAzi);
                double point_Y = CentrePoint.Y + (point_I * sinAzi) + (point_J * cosAzi);
                double point_Z = CentrePoint.Z + (sinDip * pointCos * Radius);

                // Create a cornerpoint and add it to the list
                PointXYZ nextPoint = new PointXYZ(point_X, point_Y, point_Z);
                FractureCornerPoints.Add(nextPoint);
            }

            return FractureCornerPoints;
        }

        // Constructors
        /// <summary>
        /// Constructor: create a global DFN microfracture object based on a supplied local DFN microfracture object
        /// </summary>
        /// <param name="local_uF_in">MicrofractureIJK object representing the microfracture in the local (gridblock) DFN</param>
        /// <param name="setIndex_in">Index number of the set that the fracture object belongs to</param>
        public MicrofractureXYZ(MicrofractureIJK local_uF_in, int setIndex_in)
        {
            // Assign the new object an ID number and increment the microfracture counter
            MicrofractureID = ++microfractureCounter;

            // Reference to corresponding microfracture object in the local (gridblock) DFN
            uF_local = local_uF_in;
            // Fracture set index number - this will not change after fracture is initiated
            SetIndex = setIndex_in;
            // Fracture dip azimuth - this is set at object initialisation and will not change afterwards
            Azimuth = local_uF_in.getAzimuth();
            // Fracture dip - this is set at object initialisation and will not change afterwards
            Dip = local_uF_in.Dip;
            // Time of fracture nucleation (real time) - this will not change after fracture is initiated, but must be converted from weighted time to real time
            NucleationTime = local_uF_in.getNucleationTime();

            // Set centrepoint, radius and active flag using PopulateData function
            PopulateData();
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing MicrofractureXYZ object
        /// </summary>
        /// <param name="global_uF_in">Reference to an existing MicrofractureXYZ object to copy</param>
        public MicrofractureXYZ(MicrofractureXYZ global_uF_in)
        {
            // Assign the new object an ID number and increment the microfracture counter
            MicrofractureID = ++microfractureCounter;

            // Reference to corresponding microfracture object in the local (gridblock) DFN
            // NB the local MicrofractureIJK object will remain linked to the input global MicrofractureXYZ object, not this one
            uF_local = global_uF_in.uF_local;
            // Fracture set index number - this will not change after fracture is initiated
            SetIndex = global_uF_in.SetIndex;
            // Fracture dip azimuth - this is set at object initialisation and will not change afterwards
            Azimuth = global_uF_in.Azimuth;
            // Fracture dip - this is set at object initialisation and will not change afterwards
            Dip = global_uF_in.Dip;
            // Time of fracture nucleation (real time) - this will not change after fracture is initiated, but must be converted from weighted time to real time
            NucleationTime = global_uF_in.NucleationTime;

            // Set centrepoint, radius and active flag using PopulateData function
            PopulateData();
        }
    }

    /// <summary>
    /// Skeleton of a quadrilateral planar segment of a layer-bound macrofracture, referenced in local (IJK) coordinates - use in local (gridblock) DFN
    /// </summary>
    class MacrofractureSegmentIJK
    {
        // References to external objects
        /// <summary>
        /// Reference to parent FractureSet object
        /// </summary>
        private Gridblock_FractureSet fs;
        /// <summary>
        /// Reference to corresponding FractureDipSet object
        /// </summary>
        private FractureDipSet fds;
        /// <summary>
        /// Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
        /// </summary>
        public int FractureDipSetIndex { get; private set; }
        /// <summary>
        /// Reference to corresponding macrofracture object in the global (grid) DFN
        /// </summary>
        public MacrofractureXYZ GlobalMacrofracture { get; private set; }
        /// <summary>
        /// Create a mirror image macrofracture segment, then create a new MacrofractureXYZ object linked to both segment objects
        /// </summary>
        /// <param name="setIndex">Index number of the set that the fracture object belongs to</param>
        /// <param name="mirrorSegment">Reference to a fracture segment object; this will be set to point to the mirror segment</param>
        /// <returns>Reference to linked MicrofractureXYZ object</returns>
        public MacrofractureXYZ createLinkedGlobalMacrofracture(int setIndex, out MacrofractureSegmentIJK mirrorSegment)
        {
            // Create a new MacrofractureXYZ object linking to and using data from this macrofracture segment, and a mirror segment that it will create and add to the local DFN automatically
            MacrofractureXYZ globalMacrofracture = new MacrofractureXYZ(this, out mirrorSegment, setIndex);

            // The reference to the corresponding global macrofracture object in this and the new mirror macrofracture segment objects will be set automatically by the global macrofracture constructor
            // (which calls the linktoGlobalMacrofracture function in this object to do so)

            // Return reference to the new MacrofractureXYZ object
            return globalMacrofracture;
        }
        /// <summary>
        /// Link this local macrofracture segment to a specified global macrofracture object
        /// </summary>
        /// <param name="globalMacrofracture">Reference to global macrofracture object to link to</param>
        public void linktoGlobalMacrofracture(MacrofractureXYZ globalMacrofracture)
        {
            // Set the reference in this object to the corresponding macrofracture object in the global (grid) DFN to point to the specified MacrofractureXYZ object
            GlobalMacrofracture = globalMacrofracture;

            // Add reference to this object to the list of linked macrofracture segments in the specified MacrofractureXYZ object
            globalMacrofracture.AddSegment(this);
        }
        /// <summary>
        /// Link this segment to the same global macrofracture object as another local macrofracture segment
        /// </summary>
        /// <param name="globalMacrofracture">Reference to another local macrofracture segment, to containing a link to a global macrofracture object</param>
        public void linktoGlobalMacrofracture(MacrofractureSegmentIJK newSegment)
        {
            // Set the reference in this object to the corresponding macrofracture object in the global (grid) DFN to point to the same MacrofractureXYZ object as the input MacrofractureSegmentIJK object does
            GlobalMacrofracture = newSegment.GlobalMacrofracture;

            // Add reference to this object to the list of linked macrofracture segments in the specified MacrofractureXYZ object
            GlobalMacrofracture.AddSegment(this);
        }
        /// <summary>
        /// Set the reference to the terminating fracture for a MacrofractureXYZ object that is terminated by this segment
        /// </summary>
        /// <param name="MF_in">Reference to the MacrofractureXYZ that is terminated by this segment</param>
        /// <param name="PropDir_in">Propagation direction of the tip in the input MacrofractureXYZ that is terminated by this segment</param>
        public void SetTerminatingFractureReference(MacrofractureXYZ MF_in, PropagationDirection PropDir_in)
        {
            // Set the reference to the terminating fracture in the input MacrofractureXYZ object to the MacrofractureXYZ linked to this segment
            MF_in.SetTerminatingFracture(PropDir_in, GlobalMacrofracture);
        }
        /// <summary>
        /// Calculate the I coordinate (relative to segment strike) of a point in grid (XYZ) coordinates
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>I coordinate of input point</returns>
        public double getICoordinate(PointXYZ point_in)
        {
            return fs.getICoordinate(point_in);
        }
        /// <summary>
        /// Calculate the J coordinate (relative to segment dip direction) of a point in grid (XYZ) coordinates
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>J coordinate of input point</returns>
        public double getJCoordinate(PointXYZ point_in)
        {
            return fs.getJCoordinate(point_in);
        }
        /// <summary>
        /// Convert a point in grid (XYZ) coordinates to a point in IJK coordinates relative to this segment
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>New point in IJK coordinates</returns>
        public PointIJK convertXYZtoIJK(PointXYZ point_in)
        {
            return fs.convertXYZtoIJK(point_in);
        }

        // Macrofracture segment geometry data
        /// <summary>
        /// Non-propagating fracture node in IJK coordinates - this will not change after fracture segment is initiated
        /// </summary>
        public PointIJK NonPropNode { get; private set; }
        /// <summary>
        /// Propagating fracture node in IJK coordinates
        /// </summary>
        public PointIJK PropNode { get; set; }
        /// <summary>
        /// Gridblock boundary on which non-propagating fracture node lies - this will not change after fracture segment is initiated
        /// </summary>
        public GridDirection NonPropNodeBoundary { get; private set; }
        /// <summary>
        /// Gridblock boundary on which propagating fracture node lies
        /// </summary>
        private GridDirection propnodeboundary;
        /// <summary>
        /// Gridblock boundary on which propagating fracture node lies
        /// </summary>
        public GridDirection PropNodeBoundary { get { return propnodeboundary; } set { if (trackingboundary == GridDirection.None) propnodeboundary = value; } }
        /// <summary>
        /// Flag to specify if fracture is tracking a gridblock boundary; initially set to None
        /// </summary>
        private GridDirection trackingboundary;
        /// <summary>
        /// Flag to specify if fracture is tracking a gridblock boundary; initially set to None
        /// </summary>
        public GridDirection TrackingBoundary { get { return trackingboundary; } set { if (NonPropNodeBoundary == value) { trackingboundary = value; propnodeboundary = value; } } }
        /// <summary>
        /// Direction of the outer node relative to the inner node in the local IJK coordinate frame
        /// </summary>
        public PropagationDirection LocalOrientation { get { if (!reverseNodes) return LocalPropDir; else if (LocalPropDir == PropagationDirection.IPlus) return PropagationDirection.IMinus; else return PropagationDirection.IPlus; } }
        /// <summary>
        /// Direction of fracture propagation in the local IJK coordinate frame - this will not change after fracture segment is initiated
        /// </summary>
        public PropagationDirection LocalPropDir { get; private set; }
        /// <summary>
        /// Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated - this will not change after fracture segment is initiated
        /// </summary>
        private PropagationDirection OriginalPropDir { get; set; }
        /// <summary>
        /// Which side of the MacrofractureIJK object does this segment lie on; will be the OriginalPropDir unless the nodes have been reversed, as happens when independent fractures link up, in which case it will be the opposite to the OriginalPropDir
        /// </summary>
        /// <returns></returns>
        public PropagationDirection SideOfFracture()
        {
            if (reverseNodes)
                return (OriginalPropDir == PropagationDirection.IPlus ? PropagationDirection.IMinus : PropagationDirection.IPlus);
            else
                return OriginalPropDir;
        }
        /// <summary>
        /// Segment length in strike direction; relay segments will have zero length (m)
        /// </summary>
        public double StrikeLength { get { return Math.Abs(PropNode.I - NonPropNode.I); } }
        /// <summary>
        /// Total segment length (m)
        /// </summary>
        public double TotalLength { get { return Math.Sqrt(Math.Pow(PropNode.I - NonPropNode.I, 2) + Math.Pow(PropNode.J - NonPropNode.J, 2)); } }
        /// <summary>
        /// Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
        /// </summary>
        public DipDirection DipDir { get; private set; }
        /// <summary>
        /// Get the dip of this fracture segment
        /// </summary>
        /// <returns>Segment dip (rad)</returns>
        public double getDip()
        {
            // The segment dip will be equal to the dip of the fracture dip set unless it is a relay segment, in which case it will be vertical
            if ((PropNodeType == SegmentNodeType.Relay) && (NonPropNodeType == SegmentNodeType.Relay))
                return Math.PI / 2;
            else
                return fds.Dip;
        }
        /// <summary>
        /// Get the dip azimuth of this fracture segment referenced to N
        /// </summary>
        /// <returns>Segment dip azimuth (rad)</returns>
        public double getAzimuth()
        {
            double azimuth = double.NaN;

            if (PropNode.J == NonPropNode.J) // This is a normal segment, aligned parallel to the fracture set strike for this gridblock
            {
                // Segment azimuth will be equal to fracture strike +/- pi/2 depending on dip direction
                switch (DipDir)
                {
                    case DipDirection.JPlus:
                        azimuth = fs.Strike + (Math.PI / 2);
                        break;
                    case DipDirection.JMinus:
                        azimuth = fs.Strike - (Math.PI / 2);
                        break;
                    default:
                        break;
                }
            }
            else if (PropNode.I == NonPropNode.I) // This is a relay segment
            {
                // Relay segments are vertical and perpendicular to the main fracture strike, so segment azimuth will be equal to fracture strike
                azimuth = fs.Strike;
            }
            else
            {
                // The strike of this segment will be rotated slightly from the fracture strike (i.e. the I axis), due to the offset in the J direction
                // Segment azimuth will be equal to segment strike +/- pi/2 depending on dip direction
                double dI = PropNode.I - NonPropNode.I;
                double dJ = PropNode.J - NonPropNode.J;
                switch (DipDir)
                {
                    case DipDirection.JPlus:
                        azimuth = fs.Strike + Math.Atan(dJ / dI) + (Math.PI / 2);
                        break;
                    case DipDirection.JMinus:
                        azimuth = fs.Strike + Math.Atan(dJ / dI) - (Math.PI / 2);
                        break;
                    default:
                        break;
                }
            }

            // Reset azimuth to lie within the range 0 to 2pi if it is outside that range
            while (azimuth >= (2 * Math.PI)) azimuth -= (2 * Math.PI);
            while (azimuth < 0) azimuth += (2 * Math.PI);

            // Return azimuth
            return azimuth;
        }
        /// <summary>
        /// Get the fracture propagation azimuth referenced to N
        /// </summary>
        /// <returns>Propagation azimuth (rad)</returns>
        public double getPropagationAzimuth()
        {
            // Fracture propagation azimuth will be equal to fracture strike if propagating in the IPlus direction and the opposite if propagating in the IMinus direction
            double propAzi = fs.Strike;

            if (LocalPropDir == PropagationDirection.IMinus) propAzi += Math.PI;

            // Reset propagation azimuth to lie within the range 0 to 2pi if it is outside that range
            while (propAzi >= (2 * Math.PI)) propAzi -= (2 * Math.PI);
            while (propAzi < 0) propAzi += (2 * Math.PI);

            // Return propagation azimuth
            return propAzi;
        }
        /// <summary>
        /// Get the non-propagating node of the fracture segment in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the non-propagating node of the fracture segment</returns>
        public PointXYZ getNonPropNodeinXYZ()
        {
            return fs.convertIJKtoXYZ(NonPropNode);
        }
        /// <summary>
        /// Get the propagating node of the fracture segment in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the propagating node of the fracture segment</returns>
        public PointXYZ getPropNodeinXYZ()
        {
            return fs.convertIJKtoXYZ(PropNode);
        }
        /// <summary>
        /// Get the lower non-propagating cornerpoint of the fracture segment in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the lowermost non-propagating corner of the fracture segment</returns>
        public PointXYZ getLowerNonPropCornerinXYZ()
        {
            // Get the non-propagating node (i.e. the non-propagating tip of the fracture in the vertical centre of the gridblock) in x, y, and z coordinates
            PointXYZ NodeXYZ = getNonPropNodeinXYZ();

            // Get the true vertical thickness of the gridblock at the non-propagating node
            double layerThickness = fs.getTVTAtPoint(NodeXYZ);

            // Get the sin and cosine of the fracture dip and azimuth
            double Dip = getDip();
            double Azimuth = getAzimuth();
            double tandip = Math.Tan(Dip);
            double sinazi = VectorXYZ.Sin_trim(Azimuth);
            double cosazi = VectorXYZ.Cos_trim(Azimuth);

            // Calculate x, y and z coordinates of lowermost non-propagating corner of the fracture segment
            double X_out = NodeXYZ.X + (0.5 * layerThickness * (sinazi / tandip));
            double Y_out = NodeXYZ.Y + (0.5 * layerThickness * (cosazi / tandip));
            double Z_out = NodeXYZ.Z - (0.5 * layerThickness);
#if MODIFY_FRAC_WIDTH
            X_out = NodeXYZ.X + (1 * sinazi * (fds.getMeanStressShadowWidth(-1) / 2));
            Y_out = NodeXYZ.Y + (1 * cosazi * (fds.getMeanStressShadowWidth(-1) / 2));
#endif

            // Create a new PointXYZ object with the calculated coordinates and return it
            return new PointXYZ(X_out, Y_out, Z_out);
        }
        /// <summary>
        /// Get the upper non-propagating cornerpoint of the fracture segment in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the uppermost non-propagating corner of the fracture segment</returns>
        public PointXYZ getUpperNonPropCornerinXYZ()
        {
            // Get the non-propagating node (i.e. the non-propagating tip of the fracture in the vertical centre of the gridblock) in x, y, and z coordinates
            PointXYZ NodeXYZ = getNonPropNodeinXYZ();

            // Get the true vertical thickness of the gridblock at the non-propagating node
            double layerThickness = fs.getTVTAtPoint(NodeXYZ);

            // Get the sin and cosine of the fracture dip and azimuth
            double Dip = getDip();
            double Azimuth = getAzimuth();
            double tandip = Math.Tan(Dip);
            double sinazi = VectorXYZ.Sin_trim(Azimuth);
            double cosazi = VectorXYZ.Cos_trim(Azimuth);

            // Calculate x, y and z coordinates of uppermost non-propagating corner of the fracture segment
            double X_out = NodeXYZ.X - (0.5 * layerThickness * (sinazi / tandip));
            double Y_out = NodeXYZ.Y - (0.5 * layerThickness * (cosazi / tandip));
            double Z_out = NodeXYZ.Z + (0.5 * layerThickness);
#if MODIFY_FRAC_WIDTH
            X_out = NodeXYZ.X - (1 * sinazi * (fds.getMeanStressShadowWidth(-1) / 2));
            Y_out = NodeXYZ.Y - (1 * cosazi * (fds.getMeanStressShadowWidth(-1) / 2));
#endif

            // Create a new PointXYZ object with the calculated coordinates and return it
            return new PointXYZ(X_out, Y_out, Z_out);
        }
        /// <summary>
        /// Get the lower propagating cornerpoint of the fracture segment in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the lowermost propagating corner of the fracture segment</returns>
        public PointXYZ getLowerPropCornerinXYZ()
        {
            // Get the propagating node (i.e. the propagating tip of the fracture in the vertical centre of the gridblock) in x, y, and z coordinates
            PointXYZ NodeXYZ = getPropNodeinXYZ();

            // Get the true vertical thickness of the gridblock at the propagating node
            double layerThickness = fs.getTVTAtPoint(NodeXYZ);

            // Get the sin and cosine of the fracture dip and azimuth
            double Dip = getDip();
            double Azimuth = getAzimuth();
            double tandip = Math.Tan(Dip);
            double sinazi = VectorXYZ.Sin_trim(Azimuth);
            double cosazi = VectorXYZ.Cos_trim(Azimuth);

            // Calculate x, y and z coordinates of lowermost propagating corner of the fracture segment
            double X_out = NodeXYZ.X + (0.5 * layerThickness * (sinazi / tandip));
            double Y_out = NodeXYZ.Y + (0.5 * layerThickness * (cosazi / tandip));
            double Z_out = NodeXYZ.Z - (0.5 * layerThickness);
#if MODIFY_FRAC_WIDTH
            X_out = NodeXYZ.X + (1 * sinazi * (fds.getMeanStressShadowWidth(-1) / 2));
            Y_out = NodeXYZ.Y + (1 * cosazi * (fds.getMeanStressShadowWidth(-1) / 2));
#endif

            // Create a new PointXYZ object with the calculated coordinates and return it
            return new PointXYZ(X_out, Y_out, Z_out);
        }
        /// <summary>
        /// Get the upper propagating cornerpoint of the fracture segment in XYZ coordinates
        /// </summary>
        /// <returns>New PointXYZ object representing the uppermost propagating corner of the fracture segment</returns>
        public PointXYZ getUpperPropCornerinXYZ()
        {
            // Get the propagating node (i.e. the propagating tip of the fracture in the vertical centre of the gridblock) in x, y, and z coordinates
            PointXYZ NodeXYZ = getPropNodeinXYZ();

            // Get the true vertical thickness of the gridblock at the propagating node
            double layerThickness = fs.getTVTAtPoint(NodeXYZ);

            // Get the sin and cosine of the fracture dip and azimuth
            double Dip = getDip();
            double Azimuth = getAzimuth();
            double tandip = Math.Tan(Dip);
            double sinazi = VectorXYZ.Sin_trim(Azimuth);
            double cosazi = VectorXYZ.Cos_trim(Azimuth);

            // Calculate x, y and z coordinates of uppermost propagating corner of the fracture segment
            double X_out = NodeXYZ.X - (0.5 * layerThickness * (sinazi / tandip));
            double Y_out = NodeXYZ.Y - (0.5 * layerThickness * (cosazi / tandip));
            double Z_out = NodeXYZ.Z + (0.5 * layerThickness);
#if MODIFY_FRAC_WIDTH
            X_out = NodeXYZ.X - (1 * sinazi * (fds.getMeanStressShadowWidth(-1) / 2));
            Y_out = NodeXYZ.Y - (1 * cosazi * (fds.getMeanStressShadowWidth(-1) / 2));
#endif
            // Create a new PointXYZ object with the calculated coordinates and return it
            return new PointXYZ(X_out, Y_out, Z_out);
        }
        /// <summary>
        /// Flag to specify if the nodes have been reversed, as happens when independent fractures link up; if true, the non-propagating node becomes the outer node and the propagating node becomes the inner node
        /// </summary>
        private bool reverseNodes;
        /// <summary>
        /// Swap the fracture nodes - set the Reverse Nodes flag to the opposite of its current value
        /// </summary>
        public void swapNodes()
        {
            reverseNodes = !reverseNodes;
        }
        /// <summary>
        /// Get the inner centrepoint (i.e. centrepoint furthest from the fracture tip) of the fracture segment in XYZ coordinates; this is normally the non-propagating node unless the nodes have been reversed
        /// </summary>
        /// <returns></returns>
        public PointXYZ getInnerCentrepointinXYZ()
        {
            if (reverseNodes)
                return getPropNodeinXYZ();
            else
                return getNonPropNodeinXYZ();
        }
        /// <summary>
        /// Get the outer centrepoint (i.e. centrepoint nearest to the fracture tip) of the fracture segment in XYZ coordinates; this is normally the propagating node unless the nodes have been reversed
        /// </summary>
        /// <returns>New PointXYZ object representing the lowermost outer corner of the fracture segment</returns>
        public PointXYZ getOuterCentrepointinXYZ()
        {
            if (reverseNodes)
                return getNonPropNodeinXYZ();
            else
                return getPropNodeinXYZ();
        }
        /// <summary>
        /// Get the lower inner cornerpoint (i.e. cornerpoint furthest from the fracture tip) of the fracture segment in XYZ coordinates; this is normally the lower non-propagating corner unless the nodes have been reversed
        /// </summary>
        /// <returns>New PointXYZ object representing the lowermost inner corner of the fracture segment</returns>
        public PointXYZ getLowerInnerCornerinXYZ()
        {
            if (reverseNodes)
                return getLowerPropCornerinXYZ();
            else
                return getLowerNonPropCornerinXYZ();
        }
        /// <summary>
        /// Get the upper inner cornerpoint (i.e. cornerpoint furthest from the fracture tip) of the fracture segment in XYZ coordinates; this is normally the upper non-propagating corner unless the nodes have been reversed
        /// </summary>
        /// <returns>New PointXYZ object representing the uppermost inner corner of the fracture segment</returns>
        public PointXYZ getUpperInnerCornerinXYZ()
        {
            if (reverseNodes)
                return getUpperPropCornerinXYZ();
            else
                return getUpperNonPropCornerinXYZ();
        }
        /// <summary>
        /// Get the lower outer cornerpoint (i.e. cornerpoint nearest to the fracture tip) of the fracture segment in XYZ coordinates; this is normally the lower propagating corner unless the nodes have been reversed
        /// </summary>
        /// <returns>New PointXYZ object representing the lowermost outer corner of the fracture segment</returns>
        public PointXYZ getLowerOuterCornerinXYZ()
        {
            if (reverseNodes)
                return getLowerNonPropCornerinXYZ();
            else
                return getLowerPropCornerinXYZ();
        }
        /// <summary>
        /// Get the upper outer cornerpoint (i.e. cornerpoint nearest to the fracture tip) of the fracture segment in XYZ coordinates; this is normally the upper propagating corner unless the nodes have been reversed
        /// </summary>
        /// <returns>New PointXYZ object representing the uppermost outer corner of the fracture segment</returns>
        public PointXYZ getUpperOuterCornerinXYZ()
        {
            if (reverseNodes)
                return getUpperNonPropCornerinXYZ();
            else
                return getUpperPropCornerinXYZ();
        }
        /// <summary>
        /// Get the cornerpoints of the boundary on which the propagating node lies as PointXYZ objects; if the propagating node does not lie on a boundary, will return null values
        /// </summary>
        /// <param name="UpperLeftCorner">Reference parameter for PointXYZ object representing the upper left cornerpoint of the specified boundary</param>
        /// <param name="UpperRightCorner">Reference parameter for PointXYZ object representing the upper right cornerpoint of the specified boundary</param>
        /// <param name="LowerLeftCorner">Reference parameter for PointXYZ object representing the lower left cornerpoint of the specified boundary</param>
        /// <param name="LowerRightCorner">Reference parameter for PointXYZ object representing the lower right cornerpoint of the specified boundary</param>
        public void PropNodeBoundaryCorners(out PointXYZ UpperLeftCorner, out PointXYZ UpperRightCorner, out PointXYZ LowerLeftCorner, out PointXYZ LowerRightCorner)
        {
            fs.getBoundaryCorners(PropNodeBoundary, out UpperLeftCorner, out UpperRightCorner, out LowerLeftCorner, out LowerRightCorner);
        }
        /// <summary>
        /// Mean fracture aperture in the current stress field (averaged across fracture surface)
        /// </summary>
        /// <returns></returns>
        public double MeanAperture() { return fds.getMeanMacrofractureAperture(); }
        /// <summary>
        /// Fracture compressibility, based on the aperture control data
        /// </summary>
        /// <returns></returns>
        public double Compressibility() { return fds.getMacrofractureCompressibility(); }

        // Macrofracture segment dynamic data
        /// <summary>
        /// Flag to specify node type for non-propagating node
        /// </summary>
        public SegmentNodeType NonPropNodeType { get; set; }
        /// <summary>
        /// Flag to specify node type for propagating node
        /// </summary>
        public SegmentNodeType PropNodeType { get; set; }
        /// <summary>
        /// Flag to specify node type for inner node; this is normally the non-propagating node unless the nodes have been reversed
        /// </summary>
        public SegmentNodeType InnerNodeType { get { return (reverseNodes ? PropNodeType : NonPropNodeType); } }
        /// <summary>
        /// Flag to specify node type for outer node; this is normally the propagating node unless the nodes have been reversed
        /// </summary>
        public SegmentNodeType OuterNodeType { get { return (reverseNodes ? NonPropNodeType : PropNodeType); } }
        /// <summary>
        /// Fracture segment state - true if the fracture segment is still propagating
        /// </summary>
        public bool Active { get { return (PropNodeType == SegmentNodeType.Propagating); } }
        /// <summary>
        /// Reference to the macrofracture segment that terminates this segment, by intersection, stress shadow interaction or propagation out of the gridblock; initially set to null
        /// </summary>
        public MacrofractureSegmentIJK TerminatingSegment { get; set; }
        /// <summary>
        /// Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
        /// </summary>
        public double NucleationLTime { get; private set; }
        /// <summary>
        /// Get the real time of fracture segment nucleation
        /// </summary>
        /// <returns>Nucleation time (s)</returns>
        public double getNucleationTime()
        {
            return fds.ConvertLengthToTime(NucleationLTime, NucleationTimestep);
        }
        /// <summary>
        /// Timestep of fracture nucleation - this will not change after fracture is initiated
        /// </summary>
        public int NucleationTimestep { get; private set; }

        // Reset and data input functions
        /// <summary>
        /// Create a new MacrofractureSegmentIJK object as a mirror image of this macrofracture segment and add it to the local DFN 
        /// </summary>
        /// <returns>Reference to the new mirror MacrofractureSegmentIJK object</returns>
        public MacrofractureSegmentIJK CreateMirrorSegment()
        {
            // Set the propagation direction for the mirror segment
            PropagationDirection mirrorPropDir = PropagationDirection.IMinus;
            if (LocalPropDir == PropagationDirection.IMinus) mirrorPropDir = PropagationDirection.IPlus;

            // Create a new MacrofractureSegmentIJK object as a mirror image of this macrofracture segment
            // NB the mirror segment will have zero length even if the current segment does not
            MacrofractureSegmentIJK mirrorSegment = new MacrofractureSegmentIJK(fs, FractureDipSetIndex, NonPropNode, mirrorPropDir, mirrorPropDir, DipDir, NucleationLTime, NucleationTimestep);

            // Make sure the non-propagating node of this segment has type NucleationPoint
            NonPropNodeType = SegmentNodeType.NucleationPoint;

            // Add the mirror segment to the local DFN
            fs.LocalDFNMacrofractureSegments[mirrorPropDir].Add(mirrorSegment);

            // Return the mirror segment
            return mirrorSegment;
        }
        /// <summary>
        /// Remove this MacrofractureSegmentIJK object from the relevant Gridblock.FractureSet list
        /// </summary>
        public void RemoveSegmentFromGridblock()
        {
            fs.LocalDFNMacrofractureSegments[LocalPropDir].Remove(this);
        }

        // Constructors
        /// <summary>
        /// Constructor: specify the nucleation point in fracture set (IJK) coordinates, and the propagation direction, azimuth and dip, and nucleation time, for a fracture nucleating within a gridblock
        /// </summary>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="nucleationPoint">Reference to a pointIJK object specifying the fracture nucleation point - the new MacrofractureSegmentIJK will use this reference for the non-propagating node and create a copy for the propagating node</param>
        /// <param name="current_propdir_in">Direction of fracture propagation in local IJK coordinate frame (IPlus = strike direction)</param>
        /// <param name="original_propdir_in">Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated (IPlus = strike direction)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture segment nucleation</param>
        public MacrofractureSegmentIJK(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointIJK nucleationPoint, PropagationDirection current_propdir_in, PropagationDirection original_propdir_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
        {
            // Reference to parent FractureSet object
            fs = fs_in;
            // Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
            FractureDipSetIndex = FractureDipSetIndex_in;
            // Set reference to corresponding FractureDipSet object based on index supplied
            fds = fs.FractureDipSets[FractureDipSetIndex];
            // Initially, both the non-propagating and propagating nodes will be at the nucleation point - use supplied PointIJK object for the non-propagating node and create an identical copy for the propagating node
            // First we need to set the Z coordinate of the nucleation point to zero - we assume the fracture will nucleate in the centre of the layer
            nucleationPoint.K = 0;
            NonPropNode = nucleationPoint;
            PropNode = new PointIJK(nucleationPoint);
            // Direction of fracture propagation in local IJK coordinate frame - this will not change after fracture segment is initiated
            LocalPropDir = current_propdir_in;
            // Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated - this will not change after fracture segment is initiated
            OriginalPropDir = original_propdir_in;
            // Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
            DipDir = dipdir_in;
            // Fracture dip - this is taken from the corresponding fracture dip set so need not be set here
            // Flag to specify if the nodes have been reversed, as happens when independent fractures link up; initially set to false (the non-propagating node is the inner node and the propagating node is the outer node
            reverseNodes = false;
            // Flag to specify the non-propagating node type - since the fracture is nucleating within a gridblock, this will be set to NucleationPoint, and the non-propagating node boundary to None
            NonPropNodeType = SegmentNodeType.NucleationPoint;
            NonPropNodeBoundary = GridDirection.None;
            // Set the propagating node type to propagating (i.e. active)
            PropNodeType = SegmentNodeType.Propagating;
            // Set the propagating node boundary and the tracking boundary to none
            PropNodeBoundary = GridDirection.None;
            TrackingBoundary = GridDirection.None;
            // Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
            NucleationLTime = currentLTime;
            // Timestep of fracture nucleation - this will not change after fracture is initiated
            NucleationTimestep = currentTimestep;
        }
        /// <summary>
        /// Constructor: specify the nucleation point in fracture set (IJK) coordinates, and the propagation direction, azimuth and dip, and nucleation time, for a fracture segment propagating out from a gridblock boundary
        /// </summary>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="insertionPoint">Reference to a pointIJK object specifying the point at which the fracture enters the gridblock - the new MacrofractureSegmentIJK will use this reference for the non-propagating node and create a copy for the propagating node</param>
        /// <param name="nonpropnodeboundary_in">Boundary from which the fracture enters the gridblock</param>
        /// <param name="current_propdir_in">Direction of fracture propagation in local IJK coordinate frame (IPlus = strike direction)</param>
        /// <param name="original_propdir_in">Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated (IPlus = strike direction)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture segment nucleation</param>
        public MacrofractureSegmentIJK(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointIJK insertionPoint, GridDirection nonpropnodeboundary_in, PropagationDirection current_propdir_in, PropagationDirection original_propdir_in, DipDirection dipdir_in, double currentLTime, int currentTimestep) 
            : this(fs_in, FractureDipSetIndex_in, insertionPoint, current_propdir_in, original_propdir_in, dipdir_in, currentLTime, currentTimestep)
        {
            // Check whether a boundary has been specified
            if (nonpropnodeboundary_in != GridDirection.None)
            {
                // Since the fracture is propagating out from a gridblock boundary, set the non-propagating node type to ConnectedGridblockBound and the non propagating node boundary to the specified boundary
                NonPropNodeType = SegmentNodeType.ConnectedGridblockBound;
                NonPropNodeBoundary = nonpropnodeboundary_in;
            }
        }
        /// <summary>
        /// Constructor: specify the nucleation point in grid (XYZ) coordinates, and the propagation direction, azimuth and dip, and nucleation time, for a fracture nucleating within a gridblock
        /// </summary>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="nucleationPoint">Reference to a pointXYZ object specifying the fracture nucleation point - the new MacrofractureSegmentIJK will create a new PointIJK object for each node</param>
        /// <param name="current_propdir_in">Direction of fracture propagation in local IJK coordinate frame (IPlus = strike direction)</param>
        /// <param name="original_propdir_in">Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated (IPlus = strike direction)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture segment nucleation</param>
        public MacrofractureSegmentIJK(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointXYZ nucleationPoint, PropagationDirection current_propdir_in, PropagationDirection original_propdir_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
            : this(fs_in, FractureDipSetIndex_in, fs_in.convertXYZtoIJK(nucleationPoint), current_propdir_in, original_propdir_in, dipdir_in, currentLTime, currentTimestep)
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
        /// <summary>
        /// Constructor: specify the nucleation point in grid (XYZ) coordinates, and the propagation direction, azimuth and dip, and nucleation time, for a fracture segment propagating out from a gridblock boundary
        /// </summary>
        /// <param name="fs_in">Reference to parent FractureSet object</param>
        /// <param name="FractureDipSetIndex_in">Index of fracture dip set corresponding to this fracture</param>
        /// <param name="insertionPoint">Reference to a pointXYZ object specifying the point at which the fracture enters the gridblock - the new MacrofractureSegmentIJK will create a new PointIJK object for each node</param>
        /// <param name="nonpropnodeboundary_in">Boundary from which the fracture enters the gridblock</param>
        /// <param name="current_propdir_in">Direction of fracture propagation in local IJK coordinate frame (IPlus = strike direction)</param>
        /// <param name="original_propdir_in">Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated (IPlus = strike direction)</param>
        /// <param name="dipdir_in">Fracture dip direction (JPlus is anticlockwise from strike direction IPlus)</param>
        /// <param name="currentLTime">Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t</param>
        /// <param name="currentTimestep">Timestep of fracture segment nucleation</param>
        public MacrofractureSegmentIJK(Gridblock_FractureSet fs_in, int FractureDipSetIndex_in, PointXYZ insertionPoint, GridDirection nonpropnodeboundary_in, PropagationDirection current_propdir_in, PropagationDirection original_propdir_in, DipDirection dipdir_in, double currentLTime, int currentTimestep)
            : this(fs_in, FractureDipSetIndex_in, fs_in.convertXYZtoIJK(insertionPoint), nonpropnodeboundary_in, current_propdir_in, original_propdir_in, dipdir_in, currentLTime, currentTimestep)
        {
            // Reference to parent FractureSet object
            // Index of fracture dip set corresponding to this fracture - this contains driving stress and dip data and will not change after fracture is initiated
            // Set reference to corresponding FractureDipSet object based on index supplied
            // Initially, both the non-propagating and propagating nodes will be at the insertion point - create new PointIJK objects based on supplied PointXYZ object
            // Direction of fracture propagation in local IJK coordinate frame - this will not change after fracture segment is initiated
            // Direction of fracture propagation in the IJK coordinate frame of the gridblock in which the fracture nucleated - this will not change after fracture segment is initiated
            // Fracture dip direction in IJK coordinate frame - this will not change after fracture is initiated
            // Fracture dip - this is taken from the corresponding fracture dip set so need not be set here
            // Since the fracture is propagating out from a gridblock boundary, set the non-propagating node type to ConnectedGridblockBound and the non propagating node boundary to the specified boundary
            // Set the initial fracture state and propagating node type to active (still propagating)
            // Set the propagating node boundary and the tracking boundary to none
            // Weighted time of fracture segment nucleation, calculated as the ts_PropLength of macrofracture propagation since the start of the timestep = integral alpha_MF * sigmad_b * t. This will not change after fracture is initiated
            // Timestep of fracture nucleation - this will not change after fracture is initiated
        }
    }

    /// <summary>
    /// Discrete layer-bound macrofracture, referenced in global (XYZ) coordinates, comprising one or more planar quadrilateral segments which need not be coplanar - use in global (grid) DFN
    /// </summary>
    class MacrofractureXYZ : IComparable<MacrofractureXYZ>
    {
        // References to external objects
        /// <summary>
        /// Links to contiguous macrofracture segments in the local (gridblock) DFN propagating in the IPlus and IMinus directions
        /// </summary>
        public Dictionary<PropagationDirection, List<MacrofractureSegmentIJK>> MF_segments;
        /// <summary>
        /// Add a local macrofracture segment to this fracture
        /// </summary>
        /// <param name="segment_in">Reference to local macrofracture segment object</param>
        public void AddSegment(MacrofractureSegmentIJK segment_in)
        {
            MF_segments[segment_in.SideOfFracture()].Add(segment_in);
        }
        /// <summary>
        /// Combine this fracture with another; add the segments of the other fracture onto the specified tip of this fracture, leaving the other fracture empty
        /// </summary>
        /// <param name="tipToAddOnto">The tip of this fracture on which to add the other fracture</param>
        /// <param name="fractureToCombine">Reference to a MacrofractureXYZ object specifying the fracture to add to this one</param>
        public void CombineMacrofractures(PropagationDirection tipToAddOnto, MacrofractureXYZ fractureToCombine, PropagationDirection fractureToCombine_Tip)
        {
            // Check that we are not trying to combine a fracture with itself!
            if (fractureToCombine == this)
                return;

            // First loop through the connecting tip side of the fracture we are combining with, from the outside inwards, adding the segments to this fracture
            int fractureToCombine_NoSegments = fractureToCombine.MF_segments[fractureToCombine_Tip].Count;
            for (int fractureToCombine_SegmentNo = fractureToCombine_NoSegments - 1; fractureToCombine_SegmentNo >= 0; fractureToCombine_SegmentNo--)
            {
                // Get the segment from the fracture we are combining with
                MacrofractureSegmentIJK segmentToAdd = fractureToCombine.MF_segments[fractureToCombine_Tip][fractureToCombine_SegmentNo];

                // If it is on the opposite side of the fracture, we will need to swap the nodes on the new segment, since it is being added to the opposite side of this fracture
                if (fractureToCombine_Tip != tipToAddOnto)
                    segmentToAdd.swapNodes();

                // Add the segment to the correct side of this fracture and set its parent MacrofractureXYZ reference to this object
                segmentToAdd.linktoGlobalMacrofracture(this);
            }

            // Now loop through the other side of the fracture we are combining with, from the inside outwards, adding the segments to this fracture
            fractureToCombine_Tip = (fractureToCombine_Tip == PropagationDirection.IPlus ? PropagationDirection.IMinus : PropagationDirection.IPlus);
            fractureToCombine_NoSegments = fractureToCombine.MF_segments[fractureToCombine_Tip].Count;
            for (int fractureToCombine_SegmentNo = 0; fractureToCombine_SegmentNo < fractureToCombine_NoSegments; fractureToCombine_SegmentNo++)
            {
                // Get the segment from the fracture we are combining with
                MacrofractureSegmentIJK segmentToAdd = fractureToCombine.MF_segments[fractureToCombine_Tip][fractureToCombine_SegmentNo];

                // If it is on the opposite side of the fracture, we will need to swap the nodes on the new segment, since it is being added to the opposite side of this fracture
                if (fractureToCombine_Tip != tipToAddOnto)
                    segmentToAdd.swapNodes();

                // Add the segment to the correct side of this fracture and set its parent MacrofractureXYZ reference to this object
                segmentToAdd.linktoGlobalMacrofracture(this);
            }

            // Finally remove all the segments from the fracture we have combined with
            // This will now be an empty fracture and can be removed by cleanup processes later
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                fractureToCombine.MF_segments[propDir].Clear();
        }

        // Unique macrofracture ID number
        /// <summary>
        /// Global macrofracture counter - used to set an ID for each new macrofracture object
        /// </summary>
        private static int macrofractureCounter = 0;
        /// <summary>
        /// Unique macrofracture ID 
        /// </summary>
        public int MacrofractureID { get; private set; }

        // Macrofracture geometry data - this will be fixed since all propagation calculations are carried out on the local DFNs
        /// <summary>
        /// List of segment centrepoints - i.e. the nodes at the end of each segment along the horizontal centreline of the fracture - starting at the IMinus tip and moving towards the IPlus tip
        /// This can be used to recreate the fracture centreline as a polyline
        /// It is populated by PopulateFractureData() and stored independently of the MacrofractureSegmentIJK objects so it will not be affected by subsequent growth of the segments
        /// </summary>
        public List<PointXYZ> SegmentCentrePoints { get; private set; }
        /// <summary>
        /// List of cornerpoints of each segment
        /// </summary>
        public Dictionary<PropagationDirection, List<List<PointXYZ>>> SegmentCornerPoints { get; private set; }
        /// <summary>
        /// Macrofracture halflength along strike; does not include relay segments (m)
        /// </summary>
        private Dictionary<PropagationDirection, double> strikehalflength;
        /// <summary>
        /// Macrofracture length along strike; does not include relay segments (m)
        /// </summary>
        public double StrikeLength { get { return strikehalflength[PropagationDirection.IMinus] + strikehalflength[PropagationDirection.IPlus]; } }
        /// <summary>
        /// Macrofracture halflength along strike; does not include relay segments (m)
        /// </summary>
        /// <param name="PropDir">Propagation direction of required half-macrofracture</param>
        /// <returns></returns>
        public double StrikeHalfLength(PropagationDirection PropDir) { return strikehalflength[PropDir]; }
        /// <summary>
        /// Total macrofracture halflength; includes relay segments (m)
        /// </summary>
        private Dictionary<PropagationDirection, double> totalhalflength;
        /// <summary>
        /// Total macrofracture length; includes relay segments (m)
        /// </summary>
        public double TotalLength { get { return totalhalflength[PropagationDirection.IMinus] + totalhalflength[PropagationDirection.IPlus]; } }
        /// <summary>
        /// Total macrofracture halflength; includes relay segments (m)
        /// </summary>
        /// <param name="PropDir">Propagation direction of required half-macrofracture</param>
        /// <returns></returns>
        public double TotalHalfLength(PropagationDirection PropDir) { return totalhalflength[PropDir]; }
        /// <summary>
        /// Fracture set index number - this will not change after fracture is initiated
        /// </summary>
        public int SetIndex { get; private set; }
        /// <summary>
        /// Fracture dip - this will not change after fracture is initiated
        /// </summary>
        public double Dip { get; private set; }
        /*/// <summary>
        /// Returns the azimuth of the first macrofracture segment in the IPlus direction; use for debugging
        /// </summary>
        /// <returns></returns>
        public double InitialSegmentAzimuth() { return MF_segments[PropagationDirection.IPlus][0].getAzimuth(); } */
        /// <summary>
        /// List of flags for zero length segments
        /// </summary>
        public Dictionary<PropagationDirection, List<bool>> ZeroLengthSegments { get; private set; }
        /// <summary>
        /// List of mean apertures (in current stress field) of each segment
        /// </summary>
        public Dictionary<PropagationDirection, List<double>> SegmentMeanAperture { get; private set; }
        /// <summary>
        /// Compressibility of the specified fracture segment
        /// </summary>
        /// <param name="PropDir">Propagation direction of the required segment</param>
        /// <param name="SegmentNo">Index number of the required segment</param>
        /// <returns>Fracture compressibility, based on aperture control data</returns>
        public Dictionary<PropagationDirection, List<double>> SegmentCompressibility { get; private set; }

        // Macrofracture dynamic data - this will be fixed since all propagation calculations are carried out on the local DFNs
        /// <summary>
        /// Flags to specify type and connectivity of fracture tips
        /// </summary>
        private Dictionary<PropagationDirection, FractureTipType> tiptypes;
        /// <summary>
        /// Flags to specify type and connectivity of fracture tips
        /// </summary>
        /// <param name="PropDir">Propagation direction of required tip</param>
        /// <returns></returns>
        public FractureTipType TipTypes(PropagationDirection PropDir) { return tiptypes[PropDir]; }
        /// <summary>
        /// Fracture tip states - true if the fracture tips are still propagating
        /// </summary>
        private Dictionary<PropagationDirection, bool> active;
        /// <summary>
        /// Fracture tip states - true if the fracture tips are still propagating
        /// </summary>
        /// <param name="PropDir">Propagation direction of required tip</param>
        /// <returns></returns>
        public bool Active(PropagationDirection PropDir) { return active[PropDir]; }
        /// <summary>
        /// Reference to the macrofractures that terminates this fracture, by intersection or stress shadow interaction; initially set to null
        /// </summary>
        private Dictionary<PropagationDirection, MacrofractureXYZ> terminatingfracture;
        /// <summary>
        /// ID number of the macrofracture that terminates the specified tip of this macrofracture, by intersection or stress shadow interaction; 0 if there is no terminating fracture
        /// </summary>
        /// <param name="PropDir">Propagation direction of the specified tip</param>
        /// <returns></returns>
        public int TerminatingFracture(PropagationDirection PropDir)
        {
            if (terminatingfracture[PropDir] == null)
                return 0;
            else
                return terminatingfracture[PropDir].MacrofractureID;
        }
        /// <summary>
        /// Set the reference to the macrofractures that terminates this fracture, by intersection or stress shadow interaction
        /// </summary>
        /// <param name="PropDir">Propagation direction of required tip</param>
        /// <param name="TerminatingFracture_in">Reference to the terminating fracture MacrofractureXYZ object</param>
        public void SetTerminatingFracture(PropagationDirection PropDir, MacrofractureXYZ TerminatingFracture_in) { terminatingfracture[PropDir] = TerminatingFracture_in; }
        /// <summary>
        /// Time of fracture nucleation (real time) - this will not change after fracture is initiated
        /// </summary>
        public double NucleationTime { get; private set; }

        // Reset, data input, control and implementation functions
        /// <summary>
        /// The maximum proportional distance of the crossover point outside the specified points on the first line, when bevelling the top and bottom edges of fracture segments using PointXYZ.getCrossoverPoint()
        /// </summary>
        private const double extensionRatio = 1;
        /// <summary>
        /// The tangent of the minimum permitted angle between the two lines, when bevelling the top and bottom edges of fracture segments using PointXYZ.getCrossoverPoint(); if the angle is smaller than this, they will be considered parallel
        /// 0.02 corresponds to ang angle of slightly more than 1 degree
        /// </summary>
        private const double angularTolerance = 0.02;
        /// <summary>
        /// Calculate the position of the segment cornerpoints and set the dynamic fracture data, based on the location of the segment nodes in local (IJK) coordinates, their dip and magnitude, and the top and bottom surfaces of the layer
        /// </summary>
        public void PopulateData()
        {
            // Clear the current lists of segment cornerpoints, and reset the total and along-strike fracture lengths
            SegmentCornerPoints[PropagationDirection.IPlus].Clear();
            SegmentCornerPoints[PropagationDirection.IMinus].Clear();
            strikehalflength[PropagationDirection.IPlus] = 0;
            strikehalflength[PropagationDirection.IMinus] = 0;
            totalhalflength[PropagationDirection.IPlus] = 0;
            totalhalflength[PropagationDirection.IMinus] = 0;

            // Clear the current list of zero length segment flags
            ZeroLengthSegments[PropagationDirection.IPlus].Clear();
            ZeroLengthSegments[PropagationDirection.IMinus].Clear();

            // Clear the current list of segment mean aperture values
            SegmentMeanAperture[PropagationDirection.IPlus].Clear();
            SegmentMeanAperture[PropagationDirection.IMinus].Clear();

            // Clear the current list of segment compressibility values
            SegmentCompressibility[PropagationDirection.IPlus].Clear();
            SegmentCompressibility[PropagationDirection.IMinus].Clear();

            // Create two arrays of flags for whether the upper and lower segment cornerpoints will need later adjustment
            // Segment cornerpoints will need adjusting they lie outside the cornerpoints of the adjacent segments, due to bevelling
            Dictionary<PropagationDirection, List<bool>> adjustUpperInnerCornerPoints = new Dictionary<PropagationDirection, List<bool>>();
            Dictionary<PropagationDirection, List<bool>> adjustLowerInnerCornerPoints = new Dictionary<PropagationDirection, List<bool>>();

            // Create a new list of cornerpoints for each segment, used to recreate the fracture as a series of contiguous planar segments
            // Move along the IPlus and IMinus sides of the fracture in turn
            foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                // Get the number of segments on this side of the fracture
                List<MacrofractureSegmentIJK> segments = MF_segments[dir];
                int noSegments = segments.Count;

                // Create handles for the cornerpoints and bevelled cornerpoints of the current segment and the next segment along
                // Segment cornerpoints refer to points projected directly up or down from the segment nodes, along the segment plane (i.e. updip or downdip) onto the top or bottom of the gridblock
                // Bevelled cornerpoints refer to the intersection points of the top or bottom of the segment plane and its neighbouring segment planes (or intersecting fractures)
                // Using cornerpoints rather than boundarypoints to define the overall fracture geometry gives a smoother fracture with proper bevelling between segments
                PointXYZ ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint;
                PointXYZ NextSegment_UpperInnerSegmentCornerPoint = null;
                PointXYZ NextSegment_UpperOuterSegmentCornerPoint = null;
                PointXYZ NextSegment_LowerInnerSegmentCornerPoint = null;
                PointXYZ NextSegment_LowerOuterSegmentCornerPoint = null;
                PointXYZ ThisSegment_UpperInnerBevelledCornerPoint, ThisSegment_LowerInnerBevelledCornerPoint;
                PointXYZ ThisSegment_UpperOuterBevelledCornerPoint = null;
                PointXYZ ThisSegment_LowerOuterBevelledCornerPoint = null;

                // Create arrays of flags for whether the upper and lower segment cornerpoints will need later adjustment, and add them to the appropriate dictionaries
                List<bool> adjustUpperInnerCornerPointsCurrentSide = new List<bool>();
                List<bool> adjustLowerInnerCornerPointsCurrentSide = new List<bool>();
                adjustUpperInnerCornerPoints.Add(dir, adjustUpperInnerCornerPointsCurrentSide);
                adjustLowerInnerCornerPoints.Add(dir, adjustLowerInnerCornerPointsCurrentSide);

                // Create an array of flags for inverted segments
                // Inverted segments are segments where the upper segment boundary vector is in opposite direction to the lower segment boundary vector, so the segment sides cross
                // If so we will later need to swap the lower cornerpoints
                List<bool> invertedSegment = new List<bool>();

                // Move along the fracture, calculating the cornerpoints of each segment and adding them to the segment cornerpoint list
                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    // Create a handle for the current segment
                    MacrofractureSegmentIJK CurrentSegment = segments[segmentNo];

                    // If this is the first segment, get the segment cornerpoints
                    if (segmentNo == 0)
                    {
                        ThisSegment_UpperInnerSegmentCornerPoint = CurrentSegment.getUpperInnerCornerinXYZ();
                        ThisSegment_UpperOuterSegmentCornerPoint = CurrentSegment.getUpperOuterCornerinXYZ();
                        ThisSegment_LowerInnerSegmentCornerPoint = CurrentSegment.getLowerInnerCornerinXYZ();
                        ThisSegment_LowerOuterSegmentCornerPoint = CurrentSegment.getLowerOuterCornerinXYZ();

                        // We can also use the inner segment cornerpoints as bevelled cornerpoints
                        ThisSegment_UpperInnerBevelledCornerPoint = new PointXYZ(ThisSegment_UpperInnerSegmentCornerPoint);
                        ThisSegment_LowerInnerBevelledCornerPoint = new PointXYZ(ThisSegment_LowerInnerSegmentCornerPoint);
                    }
                    else // Otherwise take them from the cornerpoints of the next segment that were calculated in the previous iteration
                    {
                        ThisSegment_UpperInnerSegmentCornerPoint = NextSegment_UpperInnerSegmentCornerPoint;
                        ThisSegment_UpperOuterSegmentCornerPoint = NextSegment_UpperOuterSegmentCornerPoint;
                        ThisSegment_LowerInnerSegmentCornerPoint = NextSegment_LowerInnerSegmentCornerPoint;
                        ThisSegment_LowerOuterSegmentCornerPoint = NextSegment_LowerOuterSegmentCornerPoint;

                        // Also use the outer bevelled cornerpoints calculated for the previous segment as the inner bevelled cornerpoints for this segment
                        ThisSegment_UpperInnerBevelledCornerPoint = ThisSegment_UpperOuterBevelledCornerPoint;
                        ThisSegment_LowerInnerBevelledCornerPoint = ThisSegment_LowerOuterBevelledCornerPoint;
                    }

                    // If this is not the last segment, get the cornerpoints of the next segment and calculate the outer bevelled cornerpoints
                    if (segmentNo < (noSegments - 1))
                    {
                        NextSegment_UpperInnerSegmentCornerPoint = segments[segmentNo + 1].getUpperInnerCornerinXYZ();
                        NextSegment_UpperOuterSegmentCornerPoint = segments[segmentNo + 1].getUpperOuterCornerinXYZ();
                        NextSegment_LowerInnerSegmentCornerPoint = segments[segmentNo + 1].getLowerInnerCornerinXYZ();
                        NextSegment_LowerOuterSegmentCornerPoint = segments[segmentNo + 1].getLowerOuterCornerinXYZ();

                        // If there is a significant bend at the cornerpoint, the original cornerpoints of the two adjacent segments may not be coincident
                        // In this case we will set the bevelled cornerpoint to the crossover point of the segment boundaries

                        // Calculate bevelled cornerpoint of the upper segment boundaries
                        // We can calculate the crossover point of the segment boundaries using the getCrossoverPoint function
                        PointXYZ UpperCrossoverPoint;
                        if (CurrentSegment.OuterNodeType == SegmentNodeType.Relay)
                            UpperCrossoverPoint = PointXYZ.getCrossoverPoint(NextSegment_UpperInnerSegmentCornerPoint, NextSegment_UpperOuterSegmentCornerPoint, ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, CrossoverType.Trim, extensionRatio, angularTolerance);
                        else
                            UpperCrossoverPoint = PointXYZ.getCrossoverPoint(ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, NextSegment_UpperInnerSegmentCornerPoint, NextSegment_UpperOuterSegmentCornerPoint, CrossoverType.Trim, extensionRatio, angularTolerance);

                        // If one (or both) of the two segments has zero length, or they are both parallel, the getCrossoverPoint function will return a null value
                        // In this case we cannot calculate the crossover point, so we will use the original segment cornerpoint as the bevelled cornerpoint
                        if (UpperCrossoverPoint is null)
                            ThisSegment_UpperOuterBevelledCornerPoint = new PointXYZ(ThisSegment_UpperOuterSegmentCornerPoint);
                        // Otherwise we will set the bevelled cornerpoint to the crossover point of the segment boundaries
                        else
                            ThisSegment_UpperOuterBevelledCornerPoint = UpperCrossoverPoint;

                        // Calculate bevelled cornerpoint of the lower segment boundaries
                        // We can calculate the crossover point of the segment boundaries using the getCrossoverPoint function
                        PointXYZ LowerCrossoverPoint;
                        if (CurrentSegment.OuterNodeType == SegmentNodeType.Relay)
                            LowerCrossoverPoint = PointXYZ.getCrossoverPoint(NextSegment_LowerInnerSegmentCornerPoint, NextSegment_LowerOuterSegmentCornerPoint, ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint, CrossoverType.Trim, extensionRatio, angularTolerance);
                        else
                            LowerCrossoverPoint = PointXYZ.getCrossoverPoint(ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint, NextSegment_LowerInnerSegmentCornerPoint, NextSegment_LowerOuterSegmentCornerPoint, CrossoverType.Trim, extensionRatio, angularTolerance);

                        // If one (or both) of the two segments has zero length, or they are both parallel, the getCrossoverPoint function will return a null value
                        // In this case we cannot calculate the crossover point, so we will use the original segment cornerpoint as the bevelled cornerpoint
                        if (LowerCrossoverPoint is null)
                            ThisSegment_LowerOuterBevelledCornerPoint = new PointXYZ(ThisSegment_LowerOuterSegmentCornerPoint);
                        // Otherwise we will set the bevelled cornerpoint to the crossover point of the segment boundaries
                        else
                            ThisSegment_LowerOuterBevelledCornerPoint = LowerCrossoverPoint;
                    }
                    else
                    {
                        // For the last segment we can use the outer segment cornerpoints as bevelled cornerpoints, unless:
                        // - The tip intersects another fracture, when we will need to calculate the crossover with the intersecting segment
                        // - The tip lies on a grid boundary, when we will need to calculate the crossover with the appropriate gridblock edge
                        ThisSegment_UpperOuterBevelledCornerPoint = new PointXYZ(ThisSegment_UpperOuterSegmentCornerPoint);
                        ThisSegment_LowerOuterBevelledCornerPoint = new PointXYZ(ThisSegment_LowerOuterSegmentCornerPoint);

                        // Get the node type of the outer node of the last segment
                        SegmentNodeType LastSegment_OuterNodeType = CurrentSegment.OuterNodeType;

                        // Check if the fracture terminates against another fracture (node type Intersection, Convergence or Relay) - if so we must bevel the cornerpoints against the intersecting fracture
                        if ((LastSegment_OuterNodeType == SegmentNodeType.Intersection) || (LastSegment_OuterNodeType == SegmentNodeType.Convergence) || (LastSegment_OuterNodeType == SegmentNodeType.Relay))
                        {
                            // Get the cornerpoints of the terminating segment
                            PointXYZ TerminatingSegment_UpperInnerSegmentCornerPoint = CurrentSegment.TerminatingSegment.getUpperInnerCornerinXYZ();
                            PointXYZ TerminatingSegment_UpperOuterSegmentCornerPoint = CurrentSegment.TerminatingSegment.getUpperOuterCornerinXYZ();
                            PointXYZ TerminatingSegment_LowerInnerSegmentCornerPoint = CurrentSegment.TerminatingSegment.getLowerInnerCornerinXYZ();
                            PointXYZ TerminatingSegment_LowerOuterSegmentCornerPoint = CurrentSegment.TerminatingSegment.getLowerOuterCornerinXYZ();

                            // Calculate crossover point of the upper segment boundaries using the getCrossoverPoint function
                            // If we cannot calculate a crossover point from the available data, use the segment cornerpoint as the bevelled cornerpoint
                            PointXYZ UpperCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingSegment_UpperInnerSegmentCornerPoint, TerminatingSegment_UpperOuterSegmentCornerPoint, ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, CrossoverType.Trim);
                            if (UpperCrossoverPoint != null) ThisSegment_UpperOuterBevelledCornerPoint = UpperCrossoverPoint;

                            // Calculate crossover point of the lower segment boundaries using the getCrossoverPoint function
                            // If we cannot calculate a crossover point from the available data, use the segment cornerpoint as the bevelled cornerpoint
                            PointXYZ LowerCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingSegment_LowerInnerSegmentCornerPoint, TerminatingSegment_LowerOuterSegmentCornerPoint, ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint, CrossoverType.Trim);
                            if (LowerCrossoverPoint != null) ThisSegment_LowerOuterBevelledCornerPoint = LowerCrossoverPoint;
                        }
                        else if (LastSegment_OuterNodeType == SegmentNodeType.NonconnectedGridblockBound)
                        {
                            // Get the cornerpoints of the terminating gridcell boundary
                            PointXYZ TerminatingBoundary_UpperLeftCornerPoint;
                            PointXYZ TerminatingBoundary_UpperRightCornerPoint;
                            PointXYZ TerminatingBoundary_LowerLeftCornerPoint;
                            PointXYZ TerminatingBoundary_LowerRightCornerPoint;
                            CurrentSegment.PropNodeBoundaryCorners(out TerminatingBoundary_UpperLeftCornerPoint, out TerminatingBoundary_UpperRightCornerPoint, out TerminatingBoundary_LowerLeftCornerPoint, out TerminatingBoundary_LowerRightCornerPoint);

                            // Get the midpoints of the sides of the terminating gridcell boundary
                            PointXYZ TerminatingBoundary_LeftMidPoint = PointXYZ.getMidPoint(TerminatingBoundary_UpperLeftCornerPoint, TerminatingBoundary_LowerLeftCornerPoint);
                            PointXYZ TerminatingBoundary_RightMidPoint = PointXYZ.getMidPoint(TerminatingBoundary_UpperRightCornerPoint, TerminatingBoundary_LowerRightCornerPoint);

                            // Calculate bevelled cornerpoint of the upper segment boundary
                            if ((TerminatingBoundary_UpperLeftCornerPoint != null) && (TerminatingBoundary_UpperRightCornerPoint != null))
                            {
                                // First we will see if the upper boundary of the segment intersects the upper terminating gridcell boundary, and if so calculate the intersection point 
                                PointXYZ UpperCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingBoundary_UpperLeftCornerPoint, TerminatingBoundary_UpperRightCornerPoint, ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, CrossoverType.Restrict);

                                // If it does not (i.e. UpperCrossoverPoint is null), check whether it intersects the left side of the terminating gridcell boundary, and if so calculate the intersection point
                                // This may occur if the cell is highly skewed
                                if (UpperCrossoverPoint == null)
                                    UpperCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingBoundary_UpperLeftCornerPoint, TerminatingBoundary_LeftMidPoint, ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, CrossoverType.Restrict);

                                // If it does not (i.e. UpperCrossoverPoint is still null), check whether it intersects the right side of the terminating gridcell boundary, and if so calculate the intersection point
                                // This may occur if the cell is highly skewed
                                if (UpperCrossoverPoint == null)
                                    UpperCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingBoundary_UpperRightCornerPoint, TerminatingBoundary_RightMidPoint, ThisSegment_UpperInnerSegmentCornerPoint, ThisSegment_UpperOuterSegmentCornerPoint, CrossoverType.Restrict);

                                // If the upper boundary of the segment intersects any of these sides of the terminating gridcell boundary, set the upper bevelled cornerpoint to that intersection; otherwise use the segment cornerpoint as the bevelled cornerpoint
                                if (UpperCrossoverPoint != null) ThisSegment_UpperOuterBevelledCornerPoint = UpperCrossoverPoint;
                            }

                            // Calculate bevelled cornerpoint of the lower segment boundary
                            if ((TerminatingBoundary_LowerLeftCornerPoint != null) && (TerminatingBoundary_LowerRightCornerPoint != null))
                            {
                                // First we will see if the lower boundary of the segment intersects the Lower terminating gridcell boundary, and if so calculate the intersection point 
                                PointXYZ LowerCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingBoundary_LowerLeftCornerPoint, TerminatingBoundary_LowerRightCornerPoint, ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint, CrossoverType.Restrict);

                                // If it does not (i.e. LowerCrossoverPoint is null), check whether it intersects the left side of the terminating gridcell boundary, and if so calculate the intersection point
                                // This may occur if the cell is highly skewed
                                if (LowerCrossoverPoint == null)
                                    LowerCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingBoundary_LowerLeftCornerPoint, TerminatingBoundary_LeftMidPoint, ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint, CrossoverType.Restrict);

                                // If it does not (i.e. LowerCrossoverPoint is still null), check whether it intersects the right side of the terminating gridcell boundary, and if so calculate the intersection point
                                // This may occur if the cell is highly skewed
                                if (LowerCrossoverPoint == null)
                                    LowerCrossoverPoint = PointXYZ.getCrossoverPoint(TerminatingBoundary_LowerRightCornerPoint, TerminatingBoundary_RightMidPoint, ThisSegment_LowerInnerSegmentCornerPoint, ThisSegment_LowerOuterSegmentCornerPoint, CrossoverType.Restrict);

                                // If the lower boundary of the segment intersects any of these sides of the terminating gridcell boundary, set the lower bevelled cornerpoint to that intersection; otherwise use the segment cornerpoint as the bevelled cornerpoint
                                if (LowerCrossoverPoint != null) ThisSegment_LowerOuterBevelledCornerPoint = LowerCrossoverPoint;
                            }
                        }

                        switch (LastSegment_OuterNodeType)
                        {
                            // NB The nucleation point cannot be the outermost node
                            case SegmentNodeType.Propagating:
                                {
                                    tiptypes[dir] = FractureTipType.Propagating;
                                    // Still propagating so no terminating fracture
                                }
                                break;
                            case SegmentNodeType.ConnectedStressShadow:
                                {
                                    tiptypes[dir] = FractureTipType.StressShadow;
                                    // With a connected stress shadow, the fracture tip interacts directly with the stress shadow of a similar sized fracture propagating in the opposite direction
                                    CurrentSegment.TerminatingSegment.SetTerminatingFractureReference(this, dir);
                                }
                                break;
                            case SegmentNodeType.NonconnectedStressShadow:
                                {
                                    tiptypes[dir] = FractureTipType.StressShadow;
                                    // With a nonconnected stress shadow, the fracture tip becomes enveloped in the stress shadow of a larger fracture
                                    // There is no direct connection to the larger fracture 
                                }
                                break;
                            case SegmentNodeType.Intersection:
                                {
                                    tiptypes[dir] = FractureTipType.Intersection;
                                    CurrentSegment.TerminatingSegment.SetTerminatingFractureReference(this, dir);
                                }
                                break;
                            case SegmentNodeType.Convergence:
                                {
                                    tiptypes[dir] = FractureTipType.Convergence;
                                    CurrentSegment.TerminatingSegment.SetTerminatingFractureReference(this, dir);
                                }
                                break;
                            // NB A connected gridblock boundary cannot be the outermost node
                            case SegmentNodeType.NonconnectedGridblockBound:
                                {
                                    tiptypes[dir] = FractureTipType.OutOfBounds;
                                    // Still propagating so no terminating fracture
                                }
                                break;
                            // NB The outermost segment can be a relay segment, if the fracture interacts with the stress shadow of an inactive fracture segment
                            case SegmentNodeType.Relay:
                                {
                                    tiptypes[dir] = FractureTipType.StressShadow;
                                    CurrentSegment.TerminatingSegment.SetTerminatingFractureReference(this, dir);
                                }
                                break;
                            case SegmentNodeType.Pinchout:
                                {
                                    tiptypes[dir] = FractureTipType.Pinchout;
                                    // No terminating fracture
                                }
                                break;
                            default:
                                {
                                    tiptypes[dir] = FractureTipType.OutOfBounds;
                                    // No terminating fracture
                                }
                                break;
                        }
                        active[dir] = CurrentSegment.Active;
                    }

                    // If the inner bevelled cornerpoints lie outside the outer bevelled cornerpoints for this segment, we will need to adjust them later
                    bool UpperBoundaryInverted = false;
                    bool LowerBoundaryInverted = false;
                    // Unless this is a relay segment, in which case they should not be adjusted
                    if (CurrentSegment.OuterNodeType != SegmentNodeType.Relay)
                    {
                        double UpperInnerBevelledCornerPoint_I = CurrentSegment.getICoordinate(ThisSegment_UpperInnerBevelledCornerPoint);
                        double UpperOuterBevelledCornerPoint_I = CurrentSegment.getICoordinate(ThisSegment_UpperOuterBevelledCornerPoint);
                        double LowerInnerBevelledCornerPoint_I = CurrentSegment.getICoordinate(ThisSegment_LowerInnerBevelledCornerPoint);
                        double LowerOuterBevelledCornerPoint_I = CurrentSegment.getICoordinate(ThisSegment_LowerOuterBevelledCornerPoint);

                        if (CurrentSegment.LocalOrientation == PropagationDirection.IPlus)
                            UpperBoundaryInverted = (UpperOuterBevelledCornerPoint_I <= UpperInnerBevelledCornerPoint_I);
                        else
                            UpperBoundaryInverted = (UpperOuterBevelledCornerPoint_I >= UpperInnerBevelledCornerPoint_I);

                        if (CurrentSegment.LocalOrientation == PropagationDirection.IPlus)
                            LowerBoundaryInverted = (LowerOuterBevelledCornerPoint_I <= LowerInnerBevelledCornerPoint_I);
                        else
                            LowerBoundaryInverted = (LowerOuterBevelledCornerPoint_I >= LowerInnerBevelledCornerPoint_I);
                    }
                    adjustUpperInnerCornerPointsCurrentSide.Add(UpperBoundaryInverted);
                    adjustLowerInnerCornerPointsCurrentSide.Add(LowerBoundaryInverted);

                    // Set the flag for inverted segments (i.e. segments whose upper segment boundary vector is in opposite direction to their lower segment boundary vector, so the segment sides cross)
                    // Most segments will not be inverted (set the inverted segment flag to false)
                    bool SegmentInverted = false;
                    // Mode 2 relay segments can be inverted if the two adjacent fracture segments dip in opposite directions
                    // If so set the inverted segment flag to true
                    if (CurrentSegment.OuterNodeType == SegmentNodeType.Relay)
                    {
                        double UpperInnerBevelledCornerPoint_J = CurrentSegment.getJCoordinate(ThisSegment_UpperInnerBevelledCornerPoint);
                        double UpperOuterBevelledCornerPoint_J = CurrentSegment.getJCoordinate(ThisSegment_UpperOuterBevelledCornerPoint);
                        double LowerInnerBevelledCornerPoint_J = CurrentSegment.getJCoordinate(ThisSegment_LowerInnerBevelledCornerPoint);
                        double LowerOuterBevelledCornerPoint_J = CurrentSegment.getJCoordinate(ThisSegment_LowerOuterBevelledCornerPoint);

                        if (Math.Sign(UpperOuterBevelledCornerPoint_J - UpperInnerBevelledCornerPoint_J) == -Math.Sign(LowerOuterBevelledCornerPoint_J - LowerInnerBevelledCornerPoint_J))
                            SegmentInverted = true;
                    }
                    invertedSegment.Add(SegmentInverted);

                    // Create a new segment cornerpoint list and add the bevelled cornerpoints to it
                    // NB we cannot swap the cornerpoints for inverted segments yet, as this would cause problems when calculating the cornerpoints for the next segment
                    List<PointXYZ> ThisSegment_CornerPoints = new List<PointXYZ>();
                    ThisSegment_CornerPoints.Add(ThisSegment_UpperInnerBevelledCornerPoint);
                    ThisSegment_CornerPoints.Add(ThisSegment_UpperOuterBevelledCornerPoint);
                    ThisSegment_CornerPoints.Add(ThisSegment_LowerOuterBevelledCornerPoint);
                    ThisSegment_CornerPoints.Add(ThisSegment_LowerInnerBevelledCornerPoint);

                    // Add the cornerpoint list to the segment cornerpoint list
                    SegmentCornerPoints[dir].Add(ThisSegment_CornerPoints);

                    // Also add the total and along-strike segment lengths
                    strikehalflength[dir] += CurrentSegment.StrikeLength;
                    totalhalflength[dir] += CurrentSegment.TotalLength;

                    // Add the flag for zero length segments
                    if (((float)CurrentSegment.PropNode.I == (float)CurrentSegment.NonPropNode.I) && ((float)CurrentSegment.PropNode.J == (float)CurrentSegment.NonPropNode.J))
                        ZeroLengthSegments[dir].Add(true);
                    else
                        ZeroLengthSegments[dir].Add(false);

                    // Finally update and add the segment aperture and compressibility, in the current stress field
                    SegmentMeanAperture[dir].Add(CurrentSegment.MeanAperture());
                    SegmentCompressibility[dir].Add(CurrentSegment.Compressibility());

                } // End move along the fracture adding cornerpoints

                // Move along the fracture, adjusting the outer cornerpoints of each segment if required
                for (int segmentNo = 1; segmentNo < noSegments; segmentNo++)
                {
                    // Check if we need to adjust the upper inner cornerpoint for the segment
                    if (adjustUpperInnerCornerPointsCurrentSide[segmentNo])
                    {
                        // Calculate the length of each of the two segments as a proportion of the total length of both of the two segments
                        double firstSegmentLength = segments[segmentNo - 1].TotalLength;
                        double secondSegmentLength = segments[segmentNo].TotalLength;
                        double combinedSegmentLength = firstSegmentLength + secondSegmentLength;
                        double firstSegmentLengthRatio = (combinedSegmentLength > 0 ? firstSegmentLength / combinedSegmentLength : 0);
                        double secondSegmentLengthRatio = 1 - firstSegmentLengthRatio;

                        // Get the inner cornerpoint of the first segment and the outer cornerpoint of the second segment - the cornerpoint between the two segments will be calculated by interpolating between these two points
                        PointXYZ firstSegmentUpperInnerCornerpoint = SegmentCornerPoints[dir][segmentNo - 1][0];
                        PointXYZ secondSegmentUpperOuterCornerpoint = SegmentCornerPoints[dir][segmentNo][1];

                        // Calculate the new X, Y and Z coordinates of the cornerpoint between the two segments, by interpolation, based on the relative length of the two segments
                        double newCornerpointX = (firstSegmentUpperInnerCornerpoint.X * secondSegmentLengthRatio) + (secondSegmentUpperOuterCornerpoint.X * firstSegmentLengthRatio);
                        double newCornerpointY = (firstSegmentUpperInnerCornerpoint.Y * secondSegmentLengthRatio) + (secondSegmentUpperOuterCornerpoint.Y * firstSegmentLengthRatio);
                        double newCornerpointZ = (firstSegmentUpperInnerCornerpoint.Z * secondSegmentLengthRatio) + (secondSegmentUpperOuterCornerpoint.Z * firstSegmentLengthRatio);

                        // Adjust the position of the outer cornerpoint of the first segment accordingly
                        // NB the position of the inner cornerpoint of the second segment will be adjusted automatically, as this is a reference to the same object as the outer cornerpoint of the first segment
                        PointXYZ firstSegmentUpperOuterCornerpoint = SegmentCornerPoints[dir][segmentNo - 1][1];
                        firstSegmentUpperOuterCornerpoint.SetCoordinates(newCornerpointX, newCornerpointY, newCornerpointZ);
                    }

                    // Check if we need to adjust the lower outer cornerpoint for the segment
                    if (adjustLowerInnerCornerPointsCurrentSide[segmentNo])
                    {
                        // Calculate the length of each of the two segments as a proportion of the total length of both of the two segments
                        double firstSegmentLength = segments[segmentNo - 1].TotalLength;
                        double secondSegmentLength = segments[segmentNo].TotalLength;
                        double combinedSegmentLength = firstSegmentLength + secondSegmentLength;
                        double firstSegmentLengthRatio = (combinedSegmentLength > 0 ? firstSegmentLength / combinedSegmentLength : 0);
                        double secondSegmentLengthRatio = 1 - firstSegmentLengthRatio;

                        // Get the inner cornerpoint of the first segment and the outer cornerpoint of the second segment - the cornerpoint between the two segments will be calculated by interpolating between these two points
                        PointXYZ firstSegmentLowerInnerCornerpoint = SegmentCornerPoints[dir][segmentNo - 1][3];
                        PointXYZ secondSegmentLowerOuterCornerpoint = SegmentCornerPoints[dir][segmentNo][2];

                        // Calculate the new X, Y and Z coordinates of the cornerpoint between the two segments, by interpolation, based on the relative length of the two segments
                        double newCornerpointX = (firstSegmentLowerInnerCornerpoint.X * secondSegmentLengthRatio) + (secondSegmentLowerOuterCornerpoint.X * firstSegmentLengthRatio);
                        double newCornerpointY = (firstSegmentLowerInnerCornerpoint.Y * secondSegmentLengthRatio) + (secondSegmentLowerOuterCornerpoint.Y * firstSegmentLengthRatio);
                        double newCornerpointZ = (firstSegmentLowerInnerCornerpoint.Z * secondSegmentLengthRatio) + (secondSegmentLowerOuterCornerpoint.Z * firstSegmentLengthRatio);

                        // Adjust the position of the outer cornerpoint of the first segment accordingly
                        // NB the position of the inner cornerpoint of the second segment will be adjusted automatically, as this is a reference to the same object as the outer cornerpoint of the first segment
                        PointXYZ firstSegmentLowerOuterCornerpoint = SegmentCornerPoints[dir][segmentNo - 1][2];
                        firstSegmentLowerOuterCornerpoint.SetCoordinates(newCornerpointX, newCornerpointY, newCornerpointZ);
                    }

                } // End move along the fracture adjusting the outer cornerpoints

                // Move along the fracture, checking for inverted segments and swapping lower cornerpoints where necessary
                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    if (invertedSegment[segmentNo])
                    {
                        PointXYZ newPoint2 = SegmentCornerPoints[dir][segmentNo][3];
                        PointXYZ newPoint3 = SegmentCornerPoints[dir][segmentNo][2];
                        SegmentCornerPoints[dir][segmentNo][2] = newPoint2;
                        SegmentCornerPoints[dir][segmentNo][3] = newPoint3;
                    }
                }

            } // End move along the IPlus and IMinus sides of the fracture in turn

            // If necessary, adjust the position of the nucleation points (i.e. the inner cornerpoints of the first segments in each direction)
            // This is necessary if they lie outside the outer cornerpoints of the first two segments
            // However we can only do this if there is at least one segment in each direction
            if ((MF_segments[PropagationDirection.IPlus].Count > 0) && (MF_segments[PropagationDirection.IMinus].Count > 0))
            {
                // Create handles for the innermost segments and the innermost segment cornerpoint lists
                MacrofractureSegmentIJK firstIPlusSegment = MF_segments[PropagationDirection.IPlus][0];
                MacrofractureSegmentIJK firstIMinusSegment = MF_segments[PropagationDirection.IMinus][0];
                List<PointXYZ> firstIPlusSegmentCornerPoints = SegmentCornerPoints[PropagationDirection.IPlus][0];
                List<PointXYZ> firstIMinusSegmentCornerPoints = SegmentCornerPoints[PropagationDirection.IMinus][0];

                // Calculate the length of each of the two segments as a proportion of the total length of both of the two segments
                double IPlusSegmentLength = firstIPlusSegment.TotalLength;
                double IMinusSegmentLength = firstIMinusSegment.TotalLength;
                double combinedSegmentLength = IPlusSegmentLength + IMinusSegmentLength;
                double IPlusSegmentLengthRatio = (combinedSegmentLength > 0 ? IPlusSegmentLength / combinedSegmentLength : 0);
                double IMinusSegmentLengthRatio = 1 - IPlusSegmentLengthRatio;

                // Get the outer cornerpoint of the two segments - the inner cornerpoints will be calculated by interpolating between these points
                PointXYZ IPlusSegmentUpperOuterCornerpoint = firstIPlusSegmentCornerPoints[1];
                PointXYZ IMinusSegmentUpperOuterCornerpoint = firstIMinusSegmentCornerPoints[1];
                PointXYZ IPlusSegmentLowerOuterCornerpoint = firstIPlusSegmentCornerPoints[2];
                PointXYZ IMinusSegmentLowerOuterCornerpoint = firstIMinusSegmentCornerPoints[2];

                // Check whether the two segments combined are doubly inverted (i.e. their combined upper segment boundary vector is in opposite direction to their combined lower segment boundary vector, so the outer sides of the two segment cross)
                // This can happen in segments bevelled against an intersecting or convergent fracture or a grid boundary, if these have very shallow dip and both the inner node and the inner node of the adjacent segment lie within the slope
                // When this is the case, the I coordinate of the outer cornerpoint of the IPlus segment will be less than the I coordinate of the outer cornerpoint of the IMinus segment
                // In this case we will set both cornerpoints to the crossover point
                double IPlusSegmentUpperOuterCornerpoint_I = firstIPlusSegment.getICoordinate(IPlusSegmentUpperOuterCornerpoint);
                double IMinusSegmentUpperOuterCornerpoint_I = firstIMinusSegment.getICoordinate(IMinusSegmentUpperOuterCornerpoint);
                double IPlusSegmentLowerOuterCornerpoint_I = firstIPlusSegment.getICoordinate(IPlusSegmentLowerOuterCornerpoint);
                double IMinusSegmentLowerOuterCornerpoint_I = firstIMinusSegment.getICoordinate(IMinusSegmentLowerOuterCornerpoint);
                if (IPlusSegmentUpperOuterCornerpoint_I < IMinusSegmentUpperOuterCornerpoint_I)
                {
                    PointXYZ UpperCrossoverPoint = PointXYZ.get3DCrossoverPoint(IPlusSegmentUpperOuterCornerpoint, IPlusSegmentLowerOuterCornerpoint, IMinusSegmentUpperOuterCornerpoint, IMinusSegmentLowerOuterCornerpoint, CrossoverType.Trim);
                    if (UpperCrossoverPoint != null)
                    {
                        IPlusSegmentUpperOuterCornerpoint = UpperCrossoverPoint;
                        IMinusSegmentUpperOuterCornerpoint = UpperCrossoverPoint;
                    }
                    else
                    {
                        IPlusSegmentUpperOuterCornerpoint = firstIPlusSegmentCornerPoints[0];
                        IMinusSegmentUpperOuterCornerpoint = firstIMinusSegmentCornerPoints[0];
                    }

                    // We also need to insert the new outer cornerpoints in the respective cornerpoint lists
                    firstIPlusSegmentCornerPoints[1] = IPlusSegmentUpperOuterCornerpoint;
                    firstIMinusSegmentCornerPoints[1] = IMinusSegmentUpperOuterCornerpoint;
                }
                else if (IPlusSegmentLowerOuterCornerpoint_I < IMinusSegmentLowerOuterCornerpoint_I)
                {
                    PointXYZ LowerCrossoverPoint = PointXYZ.get3DCrossoverPoint(IPlusSegmentUpperOuterCornerpoint, IPlusSegmentLowerOuterCornerpoint, IMinusSegmentUpperOuterCornerpoint, IMinusSegmentLowerOuterCornerpoint, CrossoverType.Trim);
                    if (LowerCrossoverPoint != null)
                    {
                        IPlusSegmentLowerOuterCornerpoint = LowerCrossoverPoint;
                        IMinusSegmentLowerOuterCornerpoint = LowerCrossoverPoint;
                    }
                    else
                    {
                        IPlusSegmentLowerOuterCornerpoint = firstIPlusSegmentCornerPoints[3];
                        IMinusSegmentLowerOuterCornerpoint = firstIMinusSegmentCornerPoints[3];
                    }
                    // We also need to insert the new outer cornerpoints in the respective cornerpoint lists
                    firstIPlusSegmentCornerPoints[2] = IPlusSegmentLowerOuterCornerpoint;
                    firstIMinusSegmentCornerPoints[2] = IMinusSegmentLowerOuterCornerpoint;
                }

                // Check if the upper nucleation point needs to be adjusted
                if (adjustUpperInnerCornerPoints[PropagationDirection.IPlus][0] || adjustUpperInnerCornerPoints[PropagationDirection.IMinus][0])
                {
                    // Calculate the new X, Y and Z coordinates of the cornerpoint between the two segments, by interpolation, based on the relative length of the two segments
                    double newUpperCornerpointX = (IPlusSegmentUpperOuterCornerpoint.X * IMinusSegmentLengthRatio) + (IMinusSegmentUpperOuterCornerpoint.X * IPlusSegmentLengthRatio);
                    double newUpperCornerpointY = (IPlusSegmentUpperOuterCornerpoint.Y * IMinusSegmentLengthRatio) + (IMinusSegmentUpperOuterCornerpoint.Y * IPlusSegmentLengthRatio);
                    double newUpperCornerpointZ = (IPlusSegmentUpperOuterCornerpoint.Z * IMinusSegmentLengthRatio) + (IMinusSegmentUpperOuterCornerpoint.Z * IPlusSegmentLengthRatio);

                    // Create the new upper cornerpoint and insert it into the respective cornerpoint lists
                    // NB we will only create one new point for the upper cornerpoints, but add references to this object to both the IPlus and IMinus cornerpoint lists
                    PointXYZ newUpperInnerCornerpoint = new PointXYZ(newUpperCornerpointX, newUpperCornerpointY, newUpperCornerpointZ);
                    firstIPlusSegmentCornerPoints[0] = newUpperInnerCornerpoint;
                    firstIMinusSegmentCornerPoints[0] = newUpperInnerCornerpoint;
                }

                // Check if the lower nucleation point needs to be adjusted
                if (adjustLowerInnerCornerPoints[PropagationDirection.IPlus][0] || adjustLowerInnerCornerPoints[PropagationDirection.IMinus][0])
                {
                    // Calculate the new X, Y and Z coordinates of the cornerpoint between the two segments, by interpolation, based on the relative length of the two segments
                    double newLowerCornerpointX = (IPlusSegmentLowerOuterCornerpoint.X * IMinusSegmentLengthRatio) + (IMinusSegmentLowerOuterCornerpoint.X * IPlusSegmentLengthRatio);
                    double newLowerCornerpointY = (IPlusSegmentLowerOuterCornerpoint.Y * IMinusSegmentLengthRatio) + (IMinusSegmentLowerOuterCornerpoint.Y * IPlusSegmentLengthRatio);
                    double newLowerCornerpointZ = (IPlusSegmentLowerOuterCornerpoint.Z * IMinusSegmentLengthRatio) + (IMinusSegmentLowerOuterCornerpoint.Z * IPlusSegmentLengthRatio);

                    // Create the new lower cornerpoints and insert it into the respective cornerpoint lists
                    // NB we will only create one new point for the lower cornerpoints, but add references to this object to both the IPlus and IMinus cornerpoint lists
                    PointXYZ newLowerInnerCornerpoint = new PointXYZ(newLowerCornerpointX, newLowerCornerpointY, newLowerCornerpointZ);
                    firstIPlusSegmentCornerPoints[3] = newLowerInnerCornerpoint;
                    firstIMinusSegmentCornerPoints[3] = newLowerInnerCornerpoint;
                }
            } // End adjust the position of the nucleation points (i.e. the inner cornerpoints of the first segments in each direction)

            // Repopulate the segment centrepoint list
            PopulateCentrepoints();
        }
        /// <summary>
        /// Create a single list of all fracture centrepoints, starting at the IMinus tip and moving towards the IPlus tip
        /// This can be used to recreate the fracture centreline as a polyline
        /// </summary>
        /// <returns></returns>
        private void PopulateCentrepoints()
        {
            // Clear the current list of segment centrepoints
            SegmentCentrePoints.Clear();

            // Keep a record of the previous point in the list; this is necessary to remove duplicates
            PointXYZ lastPoint = null;

            // First move along the IMinus side of the fracture
            {
                PropagationDirection dir = PropagationDirection.IMinus;

                // Get the number of segments on this side of the fracture
                int noSegments = MF_segments[dir].Count;

                // Now move along the fracture in the IPlus direction adding points to the centrepoint list
                for (int segmentNo = noSegments - 1; segmentNo >= 0; segmentNo--)
                {
                    // Get a reference to the appropriate segment
                    MacrofractureSegmentIJK ThisSegment = MF_segments[dir][segmentNo];

                    // Add the outer centrepoint to the list
                    // First check if it is a duplicate of the previous point
                    PointXYZ nextPoint = ThisSegment.getOuterCentrepointinXYZ();
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        SegmentCentrePoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

            } // End move along the IMinus side of the fracture

            // Next move along the IPlus side of the fracture
            {
                PropagationDirection dir = PropagationDirection.IPlus;

                // Get the number of segments on this side of the fracture
                int noSegments = MF_segments[dir].Count;

                // Add the inner centrepoint of the first segment to the list - this will be the nucleation point
                // First check if it is a duplicate of the previous point
                if (noSegments > 0)
                {
                    PointXYZ nextPoint = MF_segments[dir][0].getInnerCentrepointinXYZ();
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        SegmentCentrePoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

                // Move along the fracture in the IPlus direction adding points to the centrepoint list
                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    // Get a reference to the appropriate segment
                    MacrofractureSegmentIJK ThisSegment = MF_segments[dir][segmentNo];

                    // For all segments, add the outer centrepoint to the list
                    PointXYZ nextPoint = ThisSegment.getOuterCentrepointinXYZ();
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        SegmentCentrePoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

            } // End move along the IPlus side of the fracture
        }
        /// <summary>
        /// Remove all MacrofractureSegmentIJK objects from the relevant Gridblock.FractureSet lists
        /// </summary>
        public void DeleteSegments()
        {
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                foreach (MacrofractureSegmentIJK segment in MF_segments[propDir])
                    segment.RemoveSegmentFromGridblock();
        }
        /// <summary>
        /// Criterion to use when sorting macrofractures
        /// </summary>
        public static SortProperty SortCriterion { get; set; }
        /// <summary>
        /// Compare MacrofractureXYZ objects based on the sort criterion specified by SortCriterion
        /// </summary>
        /// <param name="that">MacrofractureXYZ object to compare with</param>
        /// <returns>Negative if this MacrofractureXYZ object has the greatest value of the specified property, positive if that MacrofractureXYZ object has the greatest value of the specified property, zero if they have equal values</returns>
        public int CompareTo(MacrofractureXYZ that)
        {
            switch (SortCriterion)
            {
                case SortProperty.Size_SmallestFirst:
                    return this.StrikeLength.CompareTo(that.StrikeLength);
                case SortProperty.Size_LargestFirst:
                    return -this.StrikeLength.CompareTo(that.StrikeLength);
                case SortProperty.NucleationTime:
                    return this.NucleationTime.CompareTo(that.NucleationTime);
                default:
                    return 0;
            }
        }

        // Output functions
        /// <summary>
        /// Function to return a list of the XYZ coordinates of the cornerpoints of each segment of the fracture, excluding zero length segments
        /// Note that using this function has a high overhead in computational cost; if possible access the cornerpoints directly from the SegmentCornerPoints nested list
        /// </summary>
        /// <returns>A primary list, each item representing a fracture segment, containing nested lists of cornerpoints as PointXYZ objects; NB this is a copy of the list in the MacrofractureXYZ object, not a reference to that list or any of the objects within it</returns>
        public List<List<PointXYZ>> GetFractureSegmentsInXYZ()
        {
            // Create a new list object for the cornerpoint lists for each segment
            // This will be populated with new objects, not references to the existing MicrofractureXYZ member objects
            List<List<PointXYZ>> NewSegmentList = new List<List<PointXYZ>>();

            // Loop through all fracture segments
            // First move along the IMinus side of the fracture in the IPlus direction
            PropagationDirection dir = PropagationDirection.IMinus;
            {
                // Get a reference to the list of segments in the MacrofractureXYZ object
                // NB we will copy data out of this list, not simply copy a reference to the list
                List<List<PointXYZ>> segments = SegmentCornerPoints[dir];

                // Move along the top of the fracture in the IPlus direction adding points
                int noSegments = MF_segments[dir].Count();
                int segmentNo;
                for (segmentNo = noSegments - 1; segmentNo >= 0; segmentNo--)
                {
                    // Check if it is a zero length segment; if so, move on to the next segment
                    if (ZeroLengthSegments[dir][segmentNo])
                        continue;

                    // Create a new list of PointXYZ objects for the cornerpoints
                    List<PointXYZ> NewSegmentCornerPoints = new List<PointXYZ>();

                    // Get a reference to the list of cornerpoints in this segment
                    // NB we will copy data out of this list, not simply copy a reference to the list
                    List<PointXYZ> segment = segments[segmentNo];

                    // Add each corner to our new list of cornerpoints for this segment
                    // NB we will create copies of the PointXYZ objects in the original list, rather than copying across references to those objects
                    foreach (PointXYZ corner in segment)
                        NewSegmentCornerPoints.Add(new PointXYZ(corner));

                    // Add the new segment to the new list of segments
                    NewSegmentList.Add(NewSegmentCornerPoints);
                }
            }
            // Then move along the IPlus side of the fracture in the IPlus direction
            dir = PropagationDirection.IPlus;
            {
                // Get a reference to the list of segments in the MacrofractureXYZ object
                // NB we will copy data out of this list, not simply copy a reference to the list
                List<List<PointXYZ>> segments = SegmentCornerPoints[dir];

                // Move along the top of the fracture in the IPlus direction adding points
                int noSegments = MF_segments[dir].Count();
                int segmentNo;
                for (segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    // Check if it is a zero length segment; if so, move on to the next segment
                    if (ZeroLengthSegments[dir][segmentNo])
                        continue;

                    // Create a new list of PointXYZ objects for the cornerpoints
                    List<PointXYZ> NewSegmentCornerPoints = new List<PointXYZ>();

                    // Get a reference to the list of cornerpoints in this segment
                    // NB we will copy data out of this list, not simply copy a reference to the list
                    List<PointXYZ> segment = segments[segmentNo];

                    // Add each corner to our new list of cornerpoints for this segment
                    // NB we will create copies of the PointXYZ objects in the original list, rather than copying across references to those objects
                    foreach (PointXYZ corner in segment)
                        NewSegmentCornerPoints.Add(new PointXYZ(corner));

                    // Add the new segment to the new list of segments
                    NewSegmentList.Add(NewSegmentCornerPoints);
                }
            }

            // Return the new segment list object
            return NewSegmentList;
        }
        /// <summary>
        /// Function to return a list of the XYZ coordinates of the cornerpoints of each element of a triangular mesh representing the fracture
        /// Note that using this function has a high overhead in computational cost; if possible access the cornerpoints directly from the SegmentCornerPoints nested list
        /// </summary>
        /// <returns>A primary list, each item representing a triangular element, containing nested lists of cornerpoints as PointXYZ objects; NB this is a copy of the list in the MacrofractureXYZ object, not a reference to that list or any of the objects within it</returns>
        public List<List<PointXYZ>> GetTriangularFractureSegmentsInXYZ()
        {
            // Create a new list object for the cornerpoint lists for each segment
            // This will be populated with new objects, not references to the existing MicrofractureXYZ member objects
            List<List<PointXYZ>> NewElementList = new List<List<PointXYZ>>();

            // Loop through all fracture segments
            // First move along the IMinus side of the fracture in the IPlus direction
            PropagationDirection dir = PropagationDirection.IMinus;
            {
                // Get a reference to the list of segments in the MacrofractureXYZ object
                // NB we will copy data out of this list, not simply copy a reference to the list
                List<List<PointXYZ>> segments = SegmentCornerPoints[dir];

                // Move along the top of the fracture in the IPlus direction adding points
                int noSegments = MF_segments[dir].Count();
                int segmentNo;
                for (segmentNo = noSegments - 1; segmentNo >= 0; segmentNo--)
                {
                    // Check if it is a zero length segment; if so, move on to the next segment
                    if (ZeroLengthSegments[dir][segmentNo])
                        continue;

                    // Each segment will be broken down into two triangular elements
                    // Therefore we must create two new lists of PointXYZ objects for the cornerpoints of each element
                    List<PointXYZ> Element1CornerPoints = new List<PointXYZ>();
                    List<PointXYZ> Element2CornerPoints = new List<PointXYZ>();

                    // Get a reference to the list of cornerpoints in this segment
                    // NB we will copy data out of this list, not simply copy a reference to the list
                    List<PointXYZ> segment = segments[segmentNo];

                    // Since the four cornerpoints in the original list are arranged in order moving around the edge of the fracture segment, we will use the first, second and third points for the first triangular element
                    // and the third, fourth and first points for the second triangular element
                    // NB we will create copies of the PointXYZ objects in the original list, rather than copying across references to those objects
                    Element1CornerPoints.Add(new PointXYZ(segment[0]));
                    Element1CornerPoints.Add(new PointXYZ(segment[1]));
                    Element1CornerPoints.Add(new PointXYZ(segment[2]));
                    Element2CornerPoints.Add(new PointXYZ(segment[2]));
                    Element2CornerPoints.Add(new PointXYZ(segment[3]));
                    Element2CornerPoints.Add(new PointXYZ(segment[0]));

                    // Add the new elements to the new list of elements
                    NewElementList.Add(Element1CornerPoints);
                    NewElementList.Add(Element2CornerPoints);
                }
            }
            // Then move along the IPlus side of the fracture in the IPlus direction
            dir = PropagationDirection.IPlus;
            {
                // Get a reference to the list of segments in the MacrofractureXYZ object
                // NB we will copy data out of this list, not simply copy a reference to the list
                List<List<PointXYZ>> segments = SegmentCornerPoints[dir];

                // Move along the top of the fracture in the IPlus direction adding points
                int noSegments = MF_segments[dir].Count();
                int segmentNo;
                for (segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    // Check if it is a zero length segment; if so, move on to the next segment
                    if (ZeroLengthSegments[dir][segmentNo])
                        continue;

                    // Each segment will be broken down into two triangular elements
                    // Therefore we must create two new lists of PointXYZ objects for the cornerpoints of each element
                    List<PointXYZ> Element1CornerPoints = new List<PointXYZ>();
                    List<PointXYZ> Element2CornerPoints = new List<PointXYZ>();

                    // Get a reference to the list of cornerpoints in this segment
                    // NB we will copy data out of this list, not simply copy a reference to the list
                    List<PointXYZ> segment = segments[segmentNo];

                    // Since the four cornerpoints in the original list are arranged in order moving around the edge of the fracture segment, we will use the first, second and third points for the first triangular element
                    // and the third, fourth and first points for the second triangular element
                    // NB we will create copies of the PointXYZ objects in the original list, rather than copying across references to those objects
                    Element1CornerPoints.Add(new PointXYZ(segment[0]));
                    Element1CornerPoints.Add(new PointXYZ(segment[1]));
                    Element1CornerPoints.Add(new PointXYZ(segment[2]));
                    Element2CornerPoints.Add(new PointXYZ(segment[2]));
                    Element2CornerPoints.Add(new PointXYZ(segment[3]));
                    Element2CornerPoints.Add(new PointXYZ(segment[0]));

                    // Add the new elements to the new list of elements
                    NewElementList.Add(Element1CornerPoints);
                    NewElementList.Add(Element2CornerPoints);
                }
            }

            // Return the new segment list object
            return NewElementList;
        }
        /// <summary>
        /// Create a single list of all fracture cornerpoints, starting at the nucleation point top and moving on a circular path around the edge of the fracture
        /// This can be used to recreate the fracture as a single, nonplanar geometric object
        /// </summary>
        /// <returns></returns>
        public List<PointXYZ> GetCornerpoints()
        {
            // Create a new cornerpoint list object
            List<PointXYZ> CornerPoints = new List<PointXYZ>();

            // Keep a record of the previous point in the list; this is neccessary to remove duplicates
            PointXYZ lastPoint = null;

            // First move around the IPlus side of the fracture
            {
                PropagationDirection dir = PropagationDirection.IPlus;

                // Get the list of segment cornerpoints we have just created and the number of segments on this side of the fracture
                List<List<PointXYZ>> segmentCorners = SegmentCornerPoints[dir];
                int noSegments = segmentCorners.Count();

                // Move along the top of the fracture in the IPlus direction adding points to the cornerpoint list
                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    // Get a reference to the appropriate segment cornerpoint list
                    List<PointXYZ> ThisSegment_Corners = segmentCorners[segmentNo];

                    // Check the segment cornerpoint list has been populated
                    if (ThisSegment_Corners.Count < 4)
                        continue;

                    // If this is the first segment, add the upper inner cornerpoint to the list - this will be the nucleation point top
                    if (segmentNo == 0)
                    {
                        PointXYZ firstPoint = new PointXYZ(ThisSegment_Corners[0]);
                        CornerPoints.Add(firstPoint);
                        lastPoint = firstPoint;
                    }

                    // For all segments, add the (bevelled) upper outer cornerpoint to the list
                    // First check if it is a duplicate of the previous point
                    PointXYZ nextPoint = new PointXYZ(ThisSegment_Corners[1]);
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        CornerPoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

                // Now move back along the bottom of the fracture in the IMinus direction adding points to the cornerpoint list
                for (int segmentNo = noSegments - 1; segmentNo >= 0; segmentNo--)
                {
                    // Get a reference to the appropriate segment cornerpoint list
                    List<PointXYZ> ThisSegment_Corners = segmentCorners[segmentNo];

                    // Check the segment cornerpoint list has been populated
                    if (ThisSegment_Corners.Count < 4)
                        continue;

                    // For all segments, add the (bevelled) lower outer cornerpoint to the list
                    // First check if it is a duplicate of the previous point
                    PointXYZ nextPoint = new PointXYZ(ThisSegment_Corners[2]);
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        CornerPoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

            } // End move around the IPlus side of the fracture

            // Next move around the IMinus side of the fracture
            {
                PropagationDirection dir = PropagationDirection.IMinus;

                // Get the list of segment cornerpoints we have just created and the number of segments on this side of the fracture
                List<List<PointXYZ>> segmentCorners = SegmentCornerPoints[dir];
                int noSegments = segmentCorners.Count();

                // Move along the bottom of the fracture in the IMinus direction adding points to the cornerpoint list
                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    // Get a reference to the appropriate segment cornerpoint list
                    List<PointXYZ> ThisSegment_Corners = segmentCorners[segmentNo];

                    // Check the segment cornerpoint list has been populated
                    if (ThisSegment_Corners.Count < 4)
                        continue;

                    // If this is the first segment, add the lower inner cornerpoint to the list - this will be the nucleation point bottom
                    // First check if it is a duplicate of the previous point
                    if (segmentNo == 0)
                    {
                        PointXYZ firstPoint = new PointXYZ(ThisSegment_Corners[3]);
                        if (!PointXYZ.comparePoints(lastPoint, firstPoint))
                            CornerPoints.Add(firstPoint);
                        lastPoint = firstPoint;
                    }

                    // For all segments, add the (bevelled) lower outer cornerpoint to the list
                    // First check if it is a duplicate of the previous point
                    PointXYZ nextPoint = new PointXYZ(ThisSegment_Corners[2]);
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        CornerPoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

                // Now move back along the top of the fracture in the IPlus direction adding points to the cornerpoint list
                for (int segmentNo = noSegments - 1; segmentNo >= 0; segmentNo--)
                {
                    // Get a reference to the appropriate segment cornerpoint list
                    List<PointXYZ> ThisSegment_Corners = segmentCorners[segmentNo];

                    // Check the segment cornerpoint list has been populated
                    if (ThisSegment_Corners.Count < 4)
                        continue;

                    // For all segments, add the (bevelled) upper outer cornerpoint to the list
                    // First check if it is a duplicate of the previous point
                    PointXYZ nextPoint = new PointXYZ(ThisSegment_Corners[1]);
                    if (!PointXYZ.comparePoints(lastPoint, nextPoint))
                        CornerPoints.Add(nextPoint);
                    lastPoint = nextPoint;
                }

            } // End move around the IMinus side of the fracture

            // Return the cornerpoint list
            return CornerPoints;
        }
        /// <summary>
        /// Create a list of the normal vectors for each fracture segment
        /// </summary>
        /// <returns>Dictionary containing lists of the normal vectors of each macrofracture segment, as VectorXYZ objects</returns>
        public Dictionary<PropagationDirection, List<VectorXYZ>> GetSegmentNormalVectors()
        {
            Dictionary<PropagationDirection, List<VectorXYZ>> SegmentNormalVectors = new Dictionary<PropagationDirection, List<VectorXYZ>>();

            // Move along the IPlus and IMinus sides of the fracture in turn
            foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                // Create a new list of vectors and add it to the dictionary
                List<VectorXYZ> normalVectorList = new List<VectorXYZ>();
                SegmentNormalVectors.Add(dir, normalVectorList);

                // Get the number of segments on this side of the fracture
                List<MacrofractureSegmentIJK> segments = MF_segments[dir];
                int noSegments = segments.Count;

                // Move along the fracture, creating a normal vector for each segment and adding it to the list
                for (int segmentNo = 0; segmentNo < noSegments; segmentNo++)
                {
                    MacrofractureSegmentIJK segment = segments[segmentNo];
                    VectorXYZ segmentNormalVector = VectorXYZ.GetNormalToPlane(segment.getAzimuth(), segment.getDip());
                    normalVectorList.Add(segmentNormalVector);
                }
            }

            return SegmentNormalVectors;
        }

        // Constructors
        /// <summary>
        /// Constructor: create a new MacrofractureXYZ object based on a supplied macrofracture segment object; this will be mirrored to create a bidirectional macrofracture
        /// </summary>
        /// <param name="initialSegment">Reference to a fracture segment object; this will be mirrored to create a bidirectional macrofracture</param>
        /// <param name="mirrorSegment">Reference to a fracture segment object; this will be set to point to the mirror segment</param>
        /// <param name="setIndex_in">Index number of the set that the fracture object belongs to</param>
        public MacrofractureXYZ(MacrofractureSegmentIJK initialSegment, out MacrofractureSegmentIJK mirrorSegment, int setIndex_in)
        {
            // Assign the new object an ID number and increment the macrofracture counter
            MacrofractureID = ++macrofractureCounter;

            // Create a mirror segment for the input segment
            // NB this will automatically be added to the local (gridblock) DFN
            mirrorSegment = initialSegment.CreateMirrorSegment();

            // Create a dictionary object for the linked local macrofracture segments
            MF_segments = new Dictionary<PropagationDirection, List<MacrofractureSegmentIJK>>();

            // Create list objects for IPlus and IMinus segments
            List<MacrofractureSegmentIJK> IPlusSegments = new List<MacrofractureSegmentIJK>();
            MF_segments.Add(PropagationDirection.IPlus, IPlusSegments);
            List<MacrofractureSegmentIJK> IMinusSegments = new List<MacrofractureSegmentIJK>();
            MF_segments.Add(PropagationDirection.IMinus, IMinusSegments);

            // Create a dictionary of nested lists of cornerpoints for each segment, and add an empty list for each propagation direction
            SegmentCornerPoints = new Dictionary<PropagationDirection, List<List<PointXYZ>>>();
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                List<List<PointXYZ>> segmentCornerPointList = new List<List<PointXYZ>>();
                SegmentCornerPoints.Add(propDir, segmentCornerPointList);
            }

            // Create a list object for segment centrepoints
            SegmentCentrePoints = new List<PointXYZ>();

            // Create dictionaries for the zero length segment flags, the segment mean apertures and the segment compressibilities, and add empty lists for each propagation direction
            ZeroLengthSegments = new Dictionary<PropagationDirection, List<bool>>();
            SegmentMeanAperture = new Dictionary<PropagationDirection, List<double>>();
            SegmentCompressibility = new Dictionary<PropagationDirection, List<double>>();
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                List<bool> zeroLengthSegmentList = new List<bool>();
                ZeroLengthSegments.Add(propDir, zeroLengthSegmentList);
                List<double> segmentMeanApertureList = new List<double>();
                SegmentMeanAperture.Add(propDir, segmentMeanApertureList);
                List<double> segmentCompressibilityList = new List<double>();
                SegmentCompressibility.Add(propDir, segmentCompressibilityList);
            }

            // Link both macrofracture segment objects to this new macrofracture object
            // To do this we will call the linktoGlobalMacrofracture function in the macrofracture segment objects
            // This will both add a reference to this macrofracture object in the macrofracture segment objects, and add references to the macrofracture segment objects to the new IPlus and IMinus list objects
            initialSegment.linktoGlobalMacrofracture(this);
            mirrorSegment.linktoGlobalMacrofracture(this);

            // Fracture set index number - this is set at object initialisation and will not change afterwards
            SetIndex = setIndex_in;
            // Fracture dip - this is set at object initialisation and will not change afterwards
            Dip = initialSegment.getDip();

            // Create dictionary objects for the half-lengths, tip types, tip states and terminating fractures, and set them to default values (zero length, propagating, active and no terminating fracture)
            strikehalflength = new Dictionary<PropagationDirection, double>();
            strikehalflength.Add(PropagationDirection.IPlus, 0);
            strikehalflength.Add(PropagationDirection.IMinus, 0);
            totalhalflength = new Dictionary<PropagationDirection, double>();
            totalhalflength.Add(PropagationDirection.IPlus, 0);
            totalhalflength.Add(PropagationDirection.IMinus, 0);
            tiptypes = new Dictionary<PropagationDirection, FractureTipType>();
            tiptypes.Add(PropagationDirection.IPlus, FractureTipType.Propagating);
            tiptypes.Add(PropagationDirection.IMinus, FractureTipType.Propagating);
            active = new Dictionary<PropagationDirection, bool>();
            active.Add(PropagationDirection.IPlus, true);
            active.Add(PropagationDirection.IMinus, true);
            terminatingfracture = new Dictionary<PropagationDirection, MacrofractureXYZ>();
            terminatingfracture.Add(PropagationDirection.IPlus, null);
            terminatingfracture.Add(PropagationDirection.IMinus, null);

            // Time of fracture nucleation (real time) - this will not change after fracture is initiated, but must be converted from weighted time to real time
            NucleationTime = initialSegment.getNucleationTime();

            // Set other geometric data using PopulateData function
            PopulateData();
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing MacrofractureXYZ object
        /// </summary>
        /// <param name="global_MF_in">Reference to an existing MacrofractureXYZ object to copy</param>
        public MacrofractureXYZ(MacrofractureXYZ global_MF_in)
        {
            // Assign the new object an ID number and increment the macrofracture counter
            MacrofractureID = ++macrofractureCounter;

            // Create a dictionary object for the linked local macrofracture segments
            MF_segments = new Dictionary<PropagationDirection, List<MacrofractureSegmentIJK>>();

            // Create list objects for IPlus and IMinus segments and copy the references across from the input global MacrofractureXYZ object
            // NB the local MacrofractureSegmentIJK objects will remain linked to the input global MacrofractureXYZ object, not this one
            List<MacrofractureSegmentIJK> IPlusSegments = new List<MacrofractureSegmentIJK>(global_MF_in.MF_segments[PropagationDirection.IPlus]);
            MF_segments.Add(PropagationDirection.IPlus, IPlusSegments);
            List<MacrofractureSegmentIJK> IMinusSegments = new List<MacrofractureSegmentIJK>(global_MF_in.MF_segments[PropagationDirection.IMinus]);
            MF_segments.Add(PropagationDirection.IMinus, IMinusSegments);

            // Create a dictionary of nested lists of cornerpoints for each segment, and add an empty list for each propagation direction
            SegmentCornerPoints = new Dictionary<PropagationDirection, List<List<PointXYZ>>>();
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                List<List<PointXYZ>> segmentCornerPointList = new List<List<PointXYZ>>();
                SegmentCornerPoints.Add(propDir, segmentCornerPointList);
            }

            // Create a list object for segment centrepoints
            SegmentCentrePoints = new List<PointXYZ>();

            // Create dictionaries for the zero length segment flags, the segment mean apertures and the segment compressibilities, and add empty lists for each propagation direction
            ZeroLengthSegments = new Dictionary<PropagationDirection, List<bool>>();
            SegmentMeanAperture = new Dictionary<PropagationDirection, List<double>>();
            SegmentCompressibility = new Dictionary<PropagationDirection, List<double>>();
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                List<bool> zeroLengthSegmentList = new List<bool>();
                ZeroLengthSegments.Add(propDir, zeroLengthSegmentList);
                List<double> segmentMeanApertureList = new List<double>();
                SegmentMeanAperture.Add(propDir, segmentMeanApertureList);
                List<double> segmentCompressibilityList = new List<double>();
                SegmentCompressibility.Add(propDir, segmentCompressibilityList);
            }

            // Fracture set index number - this is set at object initialisation and will not change afterwards
            SetIndex = global_MF_in.SetIndex;
            // Fracture dip - this is set at object initialisation and will not change afterwards
            Dip = global_MF_in.Dip;

            // Create dictionary objects for the half-lengths, tip types, tip states and terminating fractures, and set them to default values (zero length, propagating, active and no terminating fracture)
            strikehalflength = new Dictionary<PropagationDirection, double>();
            strikehalflength.Add(PropagationDirection.IPlus, 0);
            strikehalflength.Add(PropagationDirection.IMinus, 0);
            totalhalflength = new Dictionary<PropagationDirection, double>();
            totalhalflength.Add(PropagationDirection.IPlus, 0);
            totalhalflength.Add(PropagationDirection.IMinus, 0);
            tiptypes = new Dictionary<PropagationDirection, FractureTipType>();
            tiptypes.Add(PropagationDirection.IPlus, FractureTipType.Propagating);
            tiptypes.Add(PropagationDirection.IMinus, FractureTipType.Propagating);
            active = new Dictionary<PropagationDirection, bool>();
            active.Add(PropagationDirection.IPlus, true);
            active.Add(PropagationDirection.IMinus, true);
            terminatingfracture = new Dictionary<PropagationDirection, MacrofractureXYZ>();
            terminatingfracture.Add(PropagationDirection.IPlus, null);
            terminatingfracture.Add(PropagationDirection.IMinus, null);

            // Time of fracture nucleation (real time) - this will not change after fracture is initiated, but must be converted from weighted time to real time
            NucleationTime = global_MF_in.NucleationTime;

            // Set other geometric data using PopulateData function
            PopulateData();
        }
    }

    /// <summary>
    /// Holder for discrete MicrofractureXYZ and MacrofractureXYZ objects representing the entire fracture network; spand entire grid and referenced in global (XYZ) coordinates
    /// </summary>
    class GlobalDFN
    {
        // References to external objects
        /// <summary>
        /// Reference to parent FractureGrid object
        /// </summary>
        private FractureGrid gd;

        // Discrete fracture data
        /// <summary>
        /// List of discrete microfracture objects in XYZ coordinates - represents microfracture component of global DFN
        /// </summary>
        public List<MicrofractureXYZ> GlobalDFNMicrofractures;
        /// <summary>
        /// List of discrete macrofracture objects in XYZ coordinates - represents macrofracture component of global DFN
        /// </summary>
        public List<MacrofractureXYZ> GlobalDFNMacrofractures;
        /// <summary>
        /// End time of last timestep used to generate this DFN
        /// </summary>
        public double CurrentTime { get; private set; }

        // Functions to populate and sort DFN
        /// <summary>
        /// Repopulate the microfracture and macrofracture collections by calling the respective PopulateData functions
        /// </summary>
        /// <param name="currentTime_in">End time of last timestep used to generate this DFN</param>
        public void updateDFN(double currentTime_in)
        {
            // Loop through each microfracture in the discrete microfracture list and update the geometry and dynamic data
            foreach(MicrofractureXYZ uF in GlobalDFNMicrofractures)
                uF.PopulateData();

            // Remove any microfractures that have nucleated macrofractures
            // Loop through all microfractures in reverse order, deleting them if they have nucleated a macrofracture
            int CurrentMicrofractureIndex = GlobalDFNMicrofractures.Count - 1;
            while (CurrentMicrofractureIndex >= 0)
            {
                // Get reference to the microfracture
                MicrofractureXYZ CurrentMicrofracture = GlobalDFNMicrofractures[CurrentMicrofractureIndex];

                // Check if the flag for nucleating a macrofracture is set to true - if so we will delete the microfracture 
                if (CurrentMicrofracture.NucleatedMacrofracture)
                {
                    // Remove the associated local MicrofractureIJK object
                    CurrentMicrofracture.DeleteLocalFracture();

                    // Remove the MicrofractureXYZ object from the list
                    GlobalDFNMicrofractures.Remove(CurrentMicrofracture);
                }

                // Move on to the next microfracture
                CurrentMicrofractureIndex--;
            }

            // Loop through each macrofracture in the discrete macrofracture list and update the geometry and dynamic data
            foreach (MacrofractureXYZ MF in GlobalDFNMacrofractures)
                MF.PopulateData();

            // Set the current time
            CurrentTime = currentTime_in;
        }
        /// <summary>
        /// Sort all microfractures and macrofractures by specified sort criterion
        /// </summary>
        /// <param name="SortCriterion_in"></param>
        public void sortFractures(SortProperty SortCriterion_in)
        {
            MacrofractureXYZ.SortCriterion = SortCriterion_in;
            GlobalDFNMacrofractures.Sort();
            MicrofractureXYZ.SortCriterion = SortCriterion_in;
            GlobalDFNMicrofractures.Sort();
        }
        /// <summary>
        /// Cull the smallest fractures (both microfractures and macrofractures) based on specified minimum sizes and/or maximum number of fractures
        /// </summary>
        /// <param name="MinMicrofractureRadius">Minimum microfracture radius: all microfractures smaller than or equal to this radius will be removed; if negative, no microfractures will be removed at this stag</param>
        /// <param name="MinMacrofractureLength">Minimum macrofracture length: all macrofractures shorter than or equal to this length will be removed; if negative, no macrofractures will be removed at this stag</param>
        /// <param name="MaxFractureNumber">Maximum number of fractures: the smallest fractures (microfractures then macrofractures) will be removed to bring the total number of fractures under this limit; if negative, no fractures will be removed at this stage</param>
        public void removeShortestFractures(double MinMicrofractureRadius, double MinMacrofractureLength, int MaxFractureNumber)
        {
            // Sort all the fractures, largest first
            sortFractures(SortProperty.Size_LargestFirst);

            // Get the index number of the smallest microfracture
            int LastMicrofractureIndex = GlobalDFNMicrofractures.Count - 1;

            // Loop through all microfractures in reverse order, deleting them if they are smaller than or equal to the specified minimum radius
            while (LastMicrofractureIndex >= 0)
            {
                // Get reference to the smallest microfracture
                MicrofractureXYZ LastMicrofracture = GlobalDFNMicrofractures[LastMicrofractureIndex];

                // If the radius is greater than the specified minimum, we can break out of the loop 
                if (LastMicrofracture.Radius > MinMicrofractureRadius)
                    break;

                // Remove the associated local MicrofractureIJK object
                LastMicrofracture.DeleteLocalFracture();

                // Remove the MicrofractureXYZ object from the list
                GlobalDFNMicrofractures.Remove(LastMicrofracture);

                // Update the for the smallest microfracture
                LastMicrofractureIndex--;
            }

            // Get the index number of the smallest macrofracture
            int LastMacrofractureIndex = GlobalDFNMacrofractures.Count - 1;

            // Loop through all macrofractures in reverse order, deleting them if they are smaller than or equal to the specified minimum length
            while (LastMacrofractureIndex >= 0)
            {
                // Get reference to the smallest macrofracture
                MacrofractureXYZ LastMacrofracture = GlobalDFNMacrofractures[LastMacrofractureIndex];

                // If the length is greater than the specified minimum, we can break out of the loop 
                if ((float)LastMacrofracture.StrikeLength > (float)MinMacrofractureLength)
                    break;

                // Remove the associated local MacrofractureIJK segments
                LastMacrofracture.DeleteSegments();

                // Remove the MacrofractureXYZ object from the list
                GlobalDFNMacrofractures.Remove(LastMacrofracture);

                // Update the counter for the smallest macrofracture
                LastMacrofractureIndex--;
            }

            // Remove all fractures in excess of the specified maximum number
            if (MaxFractureNumber >= 0)
            {
                // Get the total number of fractures remaining
                int NoFracsRemaining = LastMacrofractureIndex + LastMicrofractureIndex + 2;

                // Loop through microfractures in reverse order, deleting them until we have the specified maximum number
                while ((LastMicrofractureIndex >= 0) && (NoFracsRemaining > MaxFractureNumber))
                {
                    // Get reference to the smallest microfracture
                    MicrofractureXYZ LastMicrofracture = GlobalDFNMicrofractures[LastMicrofractureIndex];

                    // Remove the associated local MacrofractureIJK object
                    LastMicrofracture.DeleteLocalFracture();

                    // Remove the MicrofractureXYZ object from the list
                    GlobalDFNMicrofractures.Remove(LastMicrofracture);

                    // Update the counters for the smallest microfracture and total number of fractures remaining
                    LastMicrofractureIndex--;
                    NoFracsRemaining--;
                }

                // Loop through macrofractures in reverse order, deleting them until we have the specified maximum number
                while ((LastMacrofractureIndex >= 0) && (NoFracsRemaining > MaxFractureNumber))
                {
                    // Get reference to the smallest macrofracture
                    MacrofractureXYZ LastMacrofracture = GlobalDFNMacrofractures[LastMacrofractureIndex];

                    // Remove the associated local MacrofractureIJK segments
                    LastMacrofracture.DeleteSegments();

                    // Remove the MacrofractureXYZ object from the list
                    GlobalDFNMacrofractures.Remove(LastMacrofracture);

                    // Update the counters for the smallest macrofracture and total number of fractures remaining
                    LastMacrofractureIndex--;
                    NoFracsRemaining--;
                }
            }
        }

        // Constructors
        /// <summary>
        /// Default constructor: create empty lists for discrete microfratures and macrofractures
        /// </summary>
        /// <param name="grid_in">Reference to parent FractureGrid object</param>
        public GlobalDFN(FractureGrid grid_in)
        {
            // Reference to parent Grid object
            gd = grid_in;

            // Set current time to 0
            CurrentTime = 0;

            // Create an empty list for the discrete microfractures
            GlobalDFNMicrofractures = new List<MicrofractureXYZ>();

            // Create an empty list for the discrete macrofractures
            GlobalDFNMacrofractures = new List<MacrofractureXYZ>();
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing GlobalDFN object
        /// </summary>
        /// <param name="DFNToCopy">Reference to an existing GlobalDFN object to copy data from</param>
        public GlobalDFN(GlobalDFN DFNToCopy)
        {
            // Reference to parent Grid object
            gd = DFNToCopy.gd;

            // Set current time
            CurrentTime = DFNToCopy.CurrentTime;

            // Create an empty list for the discrete microfractures
            GlobalDFNMicrofractures = new List<MicrofractureXYZ>();
            foreach (MicrofractureXYZ uF in DFNToCopy.GlobalDFNMicrofractures)
                GlobalDFNMicrofractures.Add(new MicrofractureXYZ(uF));

            // Create an empty list for the discrete macrofractures
            GlobalDFNMacrofractures = new List<MacrofractureXYZ>();
            foreach (MacrofractureXYZ MF in DFNToCopy.GlobalDFNMacrofractures)
                GlobalDFNMacrofractures.Add(new MacrofractureXYZ(MF));
        }
    }
}
