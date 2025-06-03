using System;
using System.Collections.Generic;
using System.Text;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// A ray segment is confined within a single gridblock
    /// NB All positional references are in XYZ coordinates
    /// </summary>
    class RaySegment
    {
        // References to external objects
        /// <summary>
        /// Reference to parent FractureSet object
        /// </summary>
        private UnconfinedFractureSet ufs;
        /// <summary>
        /// Reference to grandparent gridblock object
        /// </summary>
        private GridblockConfiguration gbc;

        // Geometric components
        /// <summary>
        /// Non-propagating node
        /// </summary>
        public PointXYZ NonPropNode { get; private set; }
        /// <summary>
        /// Vector representing the ray segment
        /// Not saved directly but calculated from the ray segment orientation and length
        /// </summary>
        private VectorXYZ RaySegmentVector { get { return Length * UnitVector; } }
        /// <summary>
        /// Propagating node
        /// Not saved directly but calculated from the non-prop node and the ray segment orientation and length
        /// </summary>
        public  PointXYZ PropNode { get { PointXYZ propNode = new PointXYZ(NonPropNode); propNode.AddVector(RaySegmentVector); return propNode; } }

        // Geometric properties
        /// <summary>
        /// Length of the ray segment
        /// </summary>
        public double Length { get; private set; }
        /// <summary>
        /// Azimuth of the ray segment
        /// </summary>
        public double Azimuth { get { return UnitVector.Azimuth; } }
        /// <summary>
        /// Dip of the ray segment
        /// </summary>
        public double Dip { get { return UnitVector.Dip; } }
        /// <summary>
        /// Unit length vector representing the orientation of the ray segment
        /// </summary>
        public VectorXYZ UnitVector { get; private set; }

        // Dynamic data
        /// <summary>
        /// Flag to specify node type for non-propagating node
        /// </summary>
        public SegmentNodeType NonPropNodeType { get; private set; }
        /// <summary>
        /// Flag to specify node type for propagating node
        /// </summary>
        public SegmentNodeType PropNodeType { get; set; }
        /// <summary>
        /// Ray segment state - true if the fracture segment is still propagating
        /// </summary>
        public bool Active { get { return (PropNodeType == SegmentNodeType.Propagating); } }
        /// <summary>
        /// Reference to the ray segment that terminates this segment, by intersection, stress shadow interaction or propagation out of the gridblock; initially set to null
        /// </summary>
        public RaySegment TerminatingSegment { get; set; }
        /// <summary>
        /// Time of ray segment nucleation
        /// </summary>
        public double NucleationTime { get; private set; }
        /// <summary>
        /// Timestep of fracture nucleation
        /// </summary>
        public int NucleationTimestep { get; private set; }

        // Reset and data input functions

        // Constructors
        /// <summary>
        /// Create a new ray segment for a new fracture nucleating within a gridblock
        /// </summary>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="NucleationPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="InitialLength">Initial length of the ray segment</param>
        /// <param name="NucleationTime_in">Real time of nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public RaySegment(UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ NucleationPoint, VectorXYZ Orientation, double InitialLength, double NucleationTime_in, int NucleationTimestep_in)
        {
            // Set the references to the parent fracture set and grandparent gridblock objects
            ufs = ufs_in;
            gbc = gbc_in;

            // Set the geometric data
            // Make copies of the non prop node and unit vector objects supplied
            NonPropNode = new PointXYZ(NucleationPoint);
            UnitVector = Orientation.GetNormalisedVector();
            Length = InitialLength;

            // Set the node types
            NonPropNodeType = SegmentNodeType.NucleationPoint;
            PropNodeType = SegmentNodeType.Propagating;

            // Set the nucleation time
            NucleationTime = NucleationTime_in;
            NucleationTimestep = NucleationTimestep_in;
        }
        /// <summary>
        /// Create a new ray segment for a ray that has crossed a gridblock boundary
        /// </summary>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="InsertionPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NucleationTime_in">Real time of nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public RaySegment(UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ InsertionPoint, VectorXYZ Orientation, double NucleationTime_in, int NucleationTimestep_in)
        {
            // Set the references to the parent fracture set and grandparent gridblock objects
            ufs = ufs_in;
            gbc = gbc_in;

            // Set the geometric data
            // Make copies of the non prop node and unit vector objects supplied
            NonPropNode = new PointXYZ(InsertionPoint);
            UnitVector = Orientation.GetNormalisedVector();
            // The ray segment initially has zero length
            Length = 0;

            // Set the node types
            NonPropNodeType = SegmentNodeType.ConnectedGridblockBound;
            PropNodeType = SegmentNodeType.Propagating;

            // Set the nucleation time
            NucleationTime = NucleationTime_in;
            NucleationTimestep = NucleationTimestep_in;
        }
    }

    /// <summary>
    /// A ray may be made of multiple segments spanning multiple gridblocks
    /// </summary>
    class Ray
    {
        // References to external objects
        /// <summary>
        /// Reference to parent UnconfinedFracture object
        /// </summary>
        private UnconfinedFracture fracture;

        // List of segments
        /// <summary>
        /// List of ray segments
        /// </summary>
        private List<RaySegment> segments;
        /// <summary>
        /// Add a new ray segment when the current active segment propagates across a gridblock boundary
        /// </summary>
        /// <param name="ufs_in"></param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="InsertionPoint"></param>
        /// <param name="Orientation"></param>
        /// <param name="NucleationTime_in"></param>
        /// <param name="NucleationTimestep_in"></param>
        public void AddSegment(UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ InsertionPoint, VectorXYZ Orientation, double NucleationTime_in, int NucleationTimestep_in)
        {
            segments.Add(new RaySegment(ufs_in, gbc_in, InsertionPoint, Orientation, NucleationTime_in, NucleationTimestep_in));
        }

        // Geometric data
        /// <summary>
        /// Total length of the ray
        /// </summary>
        public double Length { get { double length = 0; foreach (RaySegment segment in segments) length += segment.Length; return length; } }
        /// <summary>
        /// Effective length controls aperture, stress concentration and propagation rate
        /// </summary>
        public double EffectiveLength { get { return (Length + fracture.MinimumRadius) / 2; } }
        /// <summary>
        /// Total area of this section of the fracture
        /// </summary>
        public double Area { get { double length = Length; return (Math.PI * length * length) / fracture.NoRays; } }

        // Dynamic data
        /// <summary>
        /// Flag to specify type and connectivity of ray tips
        /// </summary>
        public FractureTipType TipType { get; private set; }
        /// <summary>
        /// Ray tip state - true if the ray tip is still propagating
        /// </summary>
        public bool Active { get { return (TipType == FractureTipType.Propagating); } }
        /// <summary>
        /// Reference to the ray segment that terminates this ray, by intersection or stress shadow interaction; initially set to null
        /// </summary>
        public RaySegment TerminatingRaySegment { get; private set; }
        /// <summary>
        /// ID number of the fracture that terminates the specified tip of this macrofracture, by intersection or stress shadow interaction; 0 if there is no terminating fracture
        /// </summary>
        public int TerminatingFracture { get; private set; }
        /// <summary>
        /// Time of fracture nucleation (real time) - this will not change after fracture is initiated
        /// </summary>
        public double NucleationTime { get; private set; }

        // Reset and data input functions

        // Constructors
        /// <summary>
        /// Create a new ray for a new fracture nucleating within a gridblock
        /// </summary>
        /// <param name="fracture_in">Reference to parent UnconfinedFracture object</param>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="NucleationPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="InitialLength">Initial length of the ray segment</param>
        /// <param name="NucleationTime_in">Real time of nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public Ray(UnconfinedFracture fracture_in, UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ NucleationPoint, VectorXYZ Orientation, double InitialLength, double NucleationTime_in, int NucleationTimestep_in)
        {
            // Set the reference to the parent fracture
            fracture = fracture_in;

            // Create a new segment list and add a nucleation segment
            segments = new List<RaySegment>();
            segments.Add(new RaySegment(ufs_in, gbc_in, NucleationPoint, Orientation, InitialLength, NucleationTime_in, NucleationTimestep_in));

            // Set the tip data
            TipType = FractureTipType.Propagating;
            TerminatingRaySegment = null;
            TerminatingFracture = -1;

            // Set the nucleation time
            NucleationTime = NucleationTime_in;
        }
    }

    /// <summary>
    /// Unconfined fracture object comprising a series of rays propagating outwards from a single point, spaced at equal angles from the top of the fracture
    /// </summary>
    class UnconfinedFracture
    {
        // References to external objects
        /// <summary>
        /// Reference to parent UnconfinedDFN object
        /// </summary>
        private UnconfinedDFN dfn;

        // Fracture geometry data
        /// <summary>
        /// Array of rays
        /// </summary>
        private Ray[] rays;
        /// <summary>
        /// Number of rays comprising the fracture
        /// </summary>
        public int NoRays { get { return rays.Length; } }
        /// <summary>
        /// Length of the shortest ray; this will control the fracture aperture and propagation rate
        /// </summary>
        public double MinimumRadius { get { double minLength = double.PositiveInfinity; foreach (Ray ray in rays) { double rayLength = ray.Length; if (minLength > rayLength) minLength = rayLength; } return minLength; }  }
        /// <summary>
        /// Length of the longest ray
        /// </summary>
        public double MaximumRadius { get { double maxLength = 0; foreach (Ray ray in rays) { double rayLength = ray.Length; if (maxLength < rayLength) maxLength = rayLength; } return maxLength; } }
        /// <summary>
        /// Mean ray length
        /// </summary>
        public double MeanRadius { get { double totalLength = 0; foreach (Ray ray in rays) totalLength += ray.Length; return totalLength / NoRays; } }
        /// <summary>
        /// Total area of the fracture
        /// </summary>
        public double Area { get { double area = 0; foreach (Ray ray in rays) area += ray.Area; return area; } }
        /// <summary>
        /// Azimuth of the fracture at its nucleation point
        /// </summary>
        public double Azimuth { get { return NormalVector.Azimuth; } }
        /// <summary>
        /// Strike of the fracture at its nucleation point
        /// </summary>
        public double Strike { get { double strike = Azimuth - (Math.PI / 2); if (strike < 0) strike += 2 * Math.PI; return strike; } }
        /// <summary>
        /// Dip of the fracture at its nucleation point
        /// </summary>
        public double Dip { get { return NormalVector.Dip - (Math.PI / 2); } }
        /// <summary>
        /// Unit length vector representing the orientation of the fracture at its nucleation point
        /// </summary>
        public VectorXYZ NormalVector { get; private set; }

        // Dynamic data
        /// <summary>
        /// True if all rays are still active; otherwise false
        /// </summary>
        public bool FullyActive { get { foreach (Ray ray in rays) if (!ray.Active) return false; return true; } }
        /// <summary>
        /// True if all rays are deactivated; false if any rays are still active
        /// </summary>
        public bool FullyDeactivated { get { foreach (Ray ray in rays) if (ray.Active) return false; return true; } }

        // Reset and data input functions

        // Constructors
        /// <summary>
        /// Create a new ray for a new fracture nucleating within a gridblock
        /// </summary>
        /// <param name="dfn_in">Reference to parent DFN object</param>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="NucleationPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NoRays">Number of rays making up the fracture; these will be spaced at equal angles from the top of the fracture</param>
        /// <param name="InitialRadius">Initial radius of the fracture</param>
        /// <param name="NucleationTime_in">Real time of nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public UnconfinedFracture(UnconfinedDFN dfn_in, UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ NucleationPoint, VectorXYZ Orientation, int NoRays, double InitialRadius, double NucleationTime_in, int NucleationTimestep_in)
        {
            // Set the reference to the parent DFN object
            dfn = dfn_in;

            // Set the fracture orientation data
            // The normal vectore should be oriented pointing upwards, so dip is negative
            // If it is pointing the wrong way it should be inverted
            NormalVector = Orientation.GetNormalisedVector();
            if (NormalVector.Component(VectorComponents.Z) < 0)
                NormalVector = -NormalVector;

            // Create a new array of rays and populate it, calculating the orientation of each initial ray segment
            rays = new Ray[NoRays];
            // Calculate useful local variables
            double sinAzi = VectorXYZ.Sin_trim(Azimuth);
            double cosAzi = VectorXYZ.Cos_trim(Azimuth);
            double sinDip = VectorXYZ.Sin_trim(Dip);
            double cosDip = VectorXYZ.Cos_trim(Dip);
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                // Get the sine and cosine of the cornerpoint relative to the fracture
                double rayPitch = 2 * Math.PI * ((double)rayNo / (double)NoRays);
                double pitchSin = VectorXYZ.Sin_trim(rayPitch);
                double pitchCos = VectorXYZ.Cos_trim(rayPitch);

                // Calculate ray vector components
                double rayVector_I = pitchSin * InitialRadius;
                double rayVector_J = -(cosDip * pitchCos * InitialRadius);
                double rayVector_X = -(rayVector_I * cosAzi) + (rayVector_J * sinAzi);
                double rayVector_Y = +(rayVector_I * sinAzi) + (rayVector_J * cosAzi);
                double rayVector_Z = +(sinDip * pitchCos * InitialRadius);

                // Create a new ray and add it to the array
                VectorXYZ nextRayVector = new VectorXYZ(rayVector_X, rayVector_Y, rayVector_Z);
                Ray nextRay = new Ray(this, ufs_in, gbc_in, NucleationPoint, nextRayVector, InitialRadius, NucleationTime_in, NucleationTimestep_in);
                rays[rayNo] = nextRay;
            }
        }
    }

    /// <summary>
    /// DFN object covering an entire grid, comprising multiple unconfined fractures
    /// </summary>
    class UnconfinedDFN
    {
        /// <summary>
        /// Reference to parent Grid object
        /// </summary>
        private FractureGrid gd;

        // Geometry data
        /// <summary>
        /// List of fractures
        /// </summary>
        private List<UnconfinedFracture> fractures;

        // Reset and data input functions
        /// <summary>
        /// Create a new fracture and add it to the DFN
        /// </summary>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="NucleationPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NoRays">Number of rays making up the fracture; these will be spaced at equal angles fro the top of the fracture</param>
        /// <param name="InitialRadius">Initial radius of the fracture</param>
        /// <param name="NucleationTime_in">Real time of nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public void AddFracture(UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ NucleationPoint, VectorXYZ Orientation, int NoRays, double InitialRadius, double NucleationTime_in, int NucleationTimestep_in)
        {
            UnconfinedFracture newFracture = new UnconfinedFracture(this, ufs_in, gbc_in, NucleationPoint, Orientation, NoRays, InitialRadius, NucleationTime_in, NucleationTimestep_in);
            fractures.Add(newFracture);
        }

        // Constructors
        public UnconfinedDFN(FractureGrid gd_in)
        {
            // Set the reference to the parent grid object
            gd = gd_in;

            // Create a new (empty) fracture list
            fractures = new List<UnconfinedFracture>();
        }
    }

}
