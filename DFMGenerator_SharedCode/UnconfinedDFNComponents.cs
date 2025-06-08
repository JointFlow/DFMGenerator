using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for the mechanism controlling the rate of propagation of an unconfined fracture ray
    /// </summary>
    public enum RaySegmentPropagationRateControl { Critical, SubcriticalFullyActive, SubcriticalRestricted }

    /// <summary>
    /// A ray segment is confined within a single gridblock
    /// NB All positional references are in XYZ coordinates
    /// </summary>
    class UnconfinedFractureRaySegment
    {
        // References to external objects
        /// <summary>
        /// Reference to parent UnconfinedFractureRay object
        /// </summary>
        private UnconfinedFractureRay ufr;
        /// <summary>
        /// Reference to grandparent UnconfinedFracture object
        /// </summary>
        private UnconfinedFractureXYZ ucf;
        /// <summary>
        /// Reference to great-grandparent FractureSet object
        /// </summary>
        private UnconfinedFractureSet ufs;
        /// <summary>
        /// Reference to great-grandparent gridblock object
        /// </summary>
        private GridblockConfiguration gbc;

        // Geometric components
        /// <summary>
        /// Non-propagating node
        /// </summary>
        private PointXYZ nonPropNode;
        /// <summary>
        /// Non-propagating node
        /// </summary>
        public PointXYZ NonPropNode { get { return new PointXYZ(nonPropNode); } }
        /// <summary>
        /// Vector representing the ray segment
        /// Not saved directly but calculated from the ray segment orientation and length
        /// </summary>
        private VectorXYZ RaySegmentVector { get { return Length * unitVector; } }
        /// <summary>
        /// Propagating node
        /// Not saved directly but calculated from the non-prop node and the ray segment orientation and length
        /// </summary>
        public PointXYZ PropNode { get { PointXYZ propNode = new PointXYZ(nonPropNode); propNode.AddVector(RaySegmentVector); return propNode; } }
        /// <summary>
        /// Gridblock boundary on which non-propagating fracture node lies - this will not change after fracture segment is initiated
        /// </summary>
        public GridDirection NonPropNodeBoundary { get; private set; }
        /// <summary>
        /// Gridblock boundary on which propagating fracture node lies
        /// </summary>
        public GridDirection PropNodeBoundary { get; set; }
        /// <summary>
        /// Check whether this fracture ray segment is part of a specified unconfined fracture
        /// </summary>
        /// <param name="ucf_in">Reference to the UnconfinedFractureXYZ object to be checked</param>
        /// <returns>True if this ray segment is part of the specified unconfined fracture, otherwise false</returns>
        public bool IsSegmentInFracture(UnconfinedFractureXYZ ucf_in)
        {
            return UnconfinedFractureXYZ.ReferenceEquals(ucf, ucf_in);
        }
        /// <summary>
        /// Check whether this fracture ray segment lies within a specified gridblock
        /// </summary>
        /// <param name="gbc_in">Reference to the Gridblock object to be checked</param>
        /// <returns>True if this ray segment lies within the specified gridblock, otherwise false</returns>
        public bool IsSegmentInGridblock(GridblockConfiguration gbc_in)
        {
            return GridblockConfiguration.ReferenceEquals(gbc, gbc_in);
        }

        // Geometric properties
        /// <summary>
        /// Length of the ray segment
        /// </summary>
        public double Length { get; set; }
        /// <summary>
        /// Total length of the ray containing this segment
        /// </summary>
        public double RayLength { get { return ufr.Length; } }
        /// <summary>
        /// Effective ray length controls aperture, stress concentration and propagation rate
        /// </summary>
        public double EffectiveRayLength { get { return ufr.EffectiveLength; } }
        /// <summary>
        /// The propagation controlling ray length is the length of the shortest ray comprising the fracture, and will limit propagation aperture, stress concentration and propagation rate of all rays
        /// </summary>
        public double PropagationControllingRayLength { get { return ucf.MinimumRayLength; } }
        /// <summary>
        /// Azimuth of the ray segment
        /// </summary>
        public double Azimuth { get { return unitVector.Azimuth; } }
        /// <summary>
        /// Dip of the ray segment
        /// </summary>
        public double Dip { get { return unitVector.Dip; } }
        /// <summary>
        /// Unit length vector representing the orientation of the ray segment
        /// </summary>
        private VectorXYZ unitVector;
        /// <summary>
        /// Unit length vector representing the orientation of the ray segment
        /// </summary>
        public VectorXYZ UnitVector { get { return new VectorXYZ(unitVector); } }

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
        /// Flag to specify for the mechanism controlling the rate of propagation of this ray segment
        /// </summary>
        public RaySegmentPropagationRateControl PropagationRateControl { get; set; }
        /// <summary>
        /// Ray segment state - true if the fracture segment is still propagating
        /// </summary>
        public bool Active { get { return (PropNodeType == SegmentNodeType.Propagating); } }
        /// <summary>
        /// Fracture state - true if the fracture is fully active (all rays are active), otherwise false
        /// </summary>
        public bool FractureFullyActive { get { return ucf.FullyActive; } }
        /// <summary>
        /// Reference to the fracture that terminates this segment, by intersection, stress shadow interaction or propagation out of the gridblock; initially set to null
        /// </summary>
        public UnconfinedFractureXYZ TerminatingFracture { get; set; }
        /// <summary>
        /// Real time of ray segment nucleation - this will not change after the segment is initiated
        /// </summary>
        public double NucleationTime { get; private set; }
        /// <summary>
        /// Fracture growth weighted time (WTime) of ray segment nucleation - this will not change after the segment is initiated
        /// </summary>
        public double NucleationWTime { get; private set; }
        /// <summary>
        /// Timestep of ray segment nucleation
        /// </summary>
        public int NucleationTimestep { get; private set; }

        // Reset and data input functions
        /// <summary>
        /// Add a new segment to the ray when this segment propagates across a gridblock boundary
        /// </summary>
        /// <param name="ufs_in">Reference to great-grandparent FractureSet object</param>
        /// <param name="gbc_in">Reference to great-grandparent Gridblock object</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NucleationTime_in">Real time of ray segment nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of ray segment nucleation</param>
        /// <returns>Reference to the new UnconfinedFractureRaySegment object</returns>
        public UnconfinedFractureRaySegment AddSegment(UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, VectorXYZ Orientation, double NucleationTime_in, int NucleationTimestep_in)
        {
            // Create a new unconfined fracture ray segment and add it to the parent ray
            UnconfinedFractureRaySegment newSegment = ufr.AddSegment(this, ufs_in, gbc_in, Orientation, NucleationTime_in, NucleationTimestep_in);

            // Add a reference to the entire fracture to the new fracture set
            ufs_in.LocalDFNUnconfinedFractures.Add(ucf);

            // Return the new UnconfinedFractureRaySegment object
            return newSegment;
        }

        // Constructors
        /// <summary>
        /// Create a new ray segment for a new fracture nucleating within a gridblock
        /// </summary>
        /// <param name="ufr_in">Reference to parent UnconfinedFractureRay object</param>
        /// <param name="ucf_in">Reference to grandparent UnconfinedFractureXYZ object</param>
        /// <param name="ufs_in">Reference to great-grandparent FractureSet object</param>
        /// <param name="gbc_in">Reference to great-grandparent Gridblock object</param>
        /// <param name="NucleationPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="InitialLength">Initial length of the ray segment</param>
        /// <param name="NucleationTime_in">Real time of ray segment nucleation</param>
        /// <param name="NucleationWTime_in">Fracture growth weighted time (WTime) of ray segment nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of ray segment nucleation</param>
        public UnconfinedFractureRaySegment(UnconfinedFractureRay ufr_in, UnconfinedFractureXYZ ucf_in, UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ NucleationPoint, VectorXYZ Orientation, double InitialLength, double NucleationTime_in, double NucleationWTime_in, int NucleationTimestep_in)
        {
            // Set the references to the parent fracture ray, grandparent fracture and great-grandparent fracture set and gridblock objects
            ufr = ufr_in;
            ucf = ucf_in;
            ufs = ufs_in;
            gbc = gbc_in;

            // Set the geometric data
            // Make copies of the non prop node and unit vector objects supplied
            nonPropNode = new PointXYZ(NucleationPoint);
            NonPropNodeBoundary = GridDirection.None;
            PropNodeBoundary = GridDirection.None;
            unitVector = Orientation.GetNormalisedVector();
            Length = InitialLength;

            // Set the node types and propagation rate control
            NonPropNodeType = SegmentNodeType.NucleationPoint;
            PropNodeType = SegmentNodeType.Propagating;
            // Initially the fracture will be subcritical and fully active
            PropagationRateControl = RaySegmentPropagationRateControl.SubcriticalFullyActive;

            // Set the nucleation time
            NucleationTime = NucleationTime_in;
            NucleationWTime = NucleationWTime_in;
            NucleationTimestep = NucleationTimestep_in;
        }
        /// <summary>
        /// Create a new ray segment for a ray that has crossed a gridblock boundary
        /// </summary>
        /// <param name="initiatorRaySegment">Reference to the UnconfinedFractureRaySegment in the neighbouring gridblock that spawned this segment</param>
        /// <param name="ucf_in">Reference to grandparent UnconfinedFractureXYZ object</param>
        /// <param name="ufs_in">Reference to great-grandparent FractureSet object</param>
        /// <param name="gbc_in">Reference to great-grandparent Gridblock object</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NucleationTime_in">Real time of ray segment nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of ray segment nucleation</param>
        public UnconfinedFractureRaySegment(UnconfinedFractureRaySegment initiatorRaySegment, UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, VectorXYZ Orientation, double NucleationTime_in, int NucleationTimestep_in)
        {
            // Set the references to the parent fracture ray, grandparent fracture and great-grandparent fracture set and gridblock objects
            ufr = initiatorRaySegment.ufr;
            ucf = initiatorRaySegment.ucf;
            ufs = ufs_in;
            gbc = gbc_in;

            // Set the geometric data
            // Make copies of the non prop node and unit vector objects supplied
            nonPropNode = new PointXYZ(initiatorRaySegment.PropNode);
            NonPropNodeBoundary = GridblockConfiguration.GetOppositeBoundary(initiatorRaySegment.PropNodeBoundary);
            PropNodeBoundary = GridDirection.None;
            unitVector = Orientation.GetNormalisedVector();
            // The ray segment initially has zero length
            Length = 0;

            // Set the node types and propagation rate control
            NonPropNodeType = SegmentNodeType.ConnectedGridblockBound;
            PropNodeType = SegmentNodeType.Propagating;
            PropagationRateControl = initiatorRaySegment.PropagationRateControl;

            // Set the nucleation time
            NucleationTime = NucleationTime_in;
            NucleationWTime = ufs_in.ConvertTimeToWeightedTime(NucleationTime_in, NucleationTimestep_in);
            NucleationTimestep = NucleationTimestep_in;
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing UnconfinedFractureRaySegment object
        /// </summary>
        /// <param name="segment_in">Reference to an existing UnconfinedFractureRaySegment object to copy</param>
        public UnconfinedFractureRaySegment(UnconfinedFractureRaySegment segment_in)
        {
            // Set the references to the parent fracture set and grandparent gridblock objects
            ufs = segment_in.ufs;
            gbc = segment_in.gbc;

            // Set the geometric data
            // Make copies of the non prop node and unit vector objects supplied
            nonPropNode = new PointXYZ(segment_in.NonPropNode);
            unitVector = segment_in.UnitVector;
            Length = segment_in.Length;

            // Set the node types, propagation rate control and terminating segment
            NonPropNodeType = segment_in.NonPropNodeType;
            PropNodeType = segment_in.PropNodeType;
            PropagationRateControl = segment_in.PropagationRateControl;
            TerminatingFracture = segment_in.TerminatingFracture;

            // Set the nucleation time
            NucleationTime = segment_in.NucleationTime;
            NucleationWTime = segment_in.NucleationWTime;
            NucleationTimestep = segment_in.NucleationTimestep;
        }
    }

    /// <summary>
    /// A ray may be made of multiple segments spanning multiple gridblocks
    /// </summary>
    class UnconfinedFractureRay
    {
        // References to external objects
        /// <summary>
        /// Reference to parent UnconfinedFractureXYZ object
        /// </summary>
        private UnconfinedFractureXYZ ucf;

        // List of segments
        /// <summary>
        /// List of ray segments
        /// </summary>
        private List<UnconfinedFractureRaySegment> segments;
        /// <summary>
        /// Get the number of segments currently comprising the ray
        /// </summary>
        public int NoSegments { get { return segments.Count; } }
        /// <summary>
        /// Add a new ray segment when the current active segment propagates across a gridblock boundary
        /// </summary>
        /// <param name="initiatorRaySegment">Reference to the UnconfinedFractureRaySegment in the neighbouring gridblock that spawned this segment</param>
        /// <param name="ufs_in">Reference to great-grandparent FractureSet object</param>
        /// <param name="gbc_in">Reference to great-grandparent Gridblock object</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NucleationTime_in">Real time of ray segment nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of ray segment nucleation</param>
        /// <returns>Reference to the new UnconfinedFractureRaySegment object</returns>
        public UnconfinedFractureRaySegment AddSegment(UnconfinedFractureRaySegment initiatorRaySegment, UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, VectorXYZ Orientation, double NucleationTime_in, int NucleationTimestep_in)
        {
            UnconfinedFractureRaySegment newSegment = new UnconfinedFractureRaySegment(initiatorRaySegment, ufs_in, gbc_in, Orientation, NucleationTime_in, NucleationTimestep_in);
            segments.Add(newSegment);
            return newSegment;
        }
        /// <summary>
        /// Get a list of all segments of this ray that lie within a specified gridblock
        /// </summary>
        /// <param name="gbc_in">Reference to the Gridblock object to be checked</param>
        /// <returns>List of UnconfinedFractureRaySegment objects lying within the specified gridblock - normally there should only be 1 or 0 segments within a specific gridblock</returns>
        public List<UnconfinedFractureRaySegment> GetRaySegmentsInGridblock(GridblockConfiguration gbc_in)
        {
            List<UnconfinedFractureRaySegment> segmentsInGridblock = new List<UnconfinedFractureRaySegment>();
            foreach (UnconfinedFractureRaySegment segment in segments)
                if (segment.IsSegmentInGridblock(gbc_in))
                    segmentsInGridblock.Add(segment);
            return segmentsInGridblock;
        }

        // Geometric data
        /// <summary>
        /// Total length of the ray
        /// </summary>
        public double Length { get { double length = 0; foreach (UnconfinedFractureRaySegment segment in segments) length += segment.Length; return length; } }
        /// <summary>
        /// Effective ray length controls aperture, stress concentration and propagation rate
        /// </summary>
        public double EffectiveLength { get { return (Length + ucf.MinimumRayLength) / 2; } }
        /// <summary>
        /// Total area of this section of the fracture
        /// </summary>
        public double Area { get { double length = Length; return (Math.PI * length * length) / ucf.NoRays; } }

        // Dynamic data
        /// <summary>
        /// Flag to specify type and connectivity of ray tips
        /// </summary>
        public FractureTipType TipType { get; private set; }
        /// <summary>
        /// Ray tip state - true if the ray tip is still propagating
        /// </summary>
        public bool Active { get { if (segments.Count > 0) return (segments[segments.Count - 1].PropNodeType == SegmentNodeType.Propagating); else return false; } }
        /// <summary>
        /// Reference to the ray segment that terminates this ray, by intersection or stress shadow interaction; initially set to null
        /// </summary>
        public UnconfinedFractureRaySegment TerminatingRaySegment { get; private set; }
        /// <summary>
        /// ID number of the fracture that terminates the specified tip of this macrofracture, by intersection or stress shadow interaction; 0 if there is no terminating fracture
        /// </summary>
        public int TerminatingFracture { get; private set; }
        /// <summary>
        /// Time of ray nucleation (real time) - this will be the time of fracture nucleation
        /// </summary>
        public double NucleationTime { get { return ucf.NucleationTime; } }

        // Output functions
        /// <summary>
        /// Function to return a list of the XYZ coordinates of all the nodes along the ray, from the fracture nucleation point outwards
        /// </summary>
        /// <returns>A list of PointXYZ objects representing the nodes along the ray, from the fracture nucleation point outwards</returns>
        public List<PointXYZ> GetNodesInXYZ()
        {
            List<PointXYZ> nodes = new List<PointXYZ>();
            nodes.Add(new PointXYZ(ucf.NucleationPoint));
            foreach (UnconfinedFractureRaySegment segment in segments)
                nodes.Add(new PointXYZ(segment.PropNode));
            return nodes;
        }
        /// <summary>
        /// Get the position of the inner tip of the ray - this will be the fracture nucleation point
        /// </summary>
        public PointXYZ InnerTip { get { return ucf.NucleationPoint; } }
        /// <summary>
        /// Get the position of the outer tip of the ray - this will be the propagating node of the outermost ray segment
        /// </summary>
        public PointXYZ OuterTip { get { if (segments.Count > 0) return (new PointXYZ(segments[segments.Count - 1].PropNode)); else return null; } }

        // Reset and data input functions

        // Constructors
        /// <summary>
        /// Create a new ray for a new fracture nucleating within a gridblock
        /// </summary>
        /// <param name="ucf_in">Reference to parent UnconfinedFracture object</param>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="NucleationPoint">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="InitialLength">Initial length of the ray segment</param>
        /// <param name="NucleationTime_in">Real time of nucleation</param>
        /// <param name="NucleationWTime_in">Fracture growth weighted time (WTime) of ray segment nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public UnconfinedFractureRay(UnconfinedFractureXYZ ucf_in, UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, PointXYZ NucleationPoint, VectorXYZ Orientation, double InitialLength, double NucleationTime_in, double NucleationWTime_in, int NucleationTimestep_in)
        {
            // Set the reference to the parent fracture
            ucf = ucf_in;

            // Create a new segment list and add a nucleation segment
            segments = new List<UnconfinedFractureRaySegment>();
            segments.Add(new UnconfinedFractureRaySegment(this, ucf_in, ufs_in, gbc_in, NucleationPoint, Orientation, InitialLength, NucleationTime_in, NucleationWTime_in, NucleationTimestep_in));

            // Set the tip data
            TipType = FractureTipType.Propagating;
            TerminatingRaySegment = null;
            TerminatingFracture = -1;
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing UnconfinedFractureRay object
        /// </summary>
        /// <param name="ray_in">Reference to an existing UnconfinedFractureRay object to copy</param>
        public UnconfinedFractureRay(UnconfinedFractureRay ray_in)
        {
            // Set the reference to the parent fracture
            ucf = ray_in.ucf;

            // Create a new segment list and add copies of the segments in the input UnconfinedFractureRay object
            segments = new List<UnconfinedFractureRaySegment>();
            foreach (UnconfinedFractureRaySegment segment in ray_in.segments)
                segments.Add(new UnconfinedFractureRaySegment(segment));

            // Set the tip data
            TipType = ray_in.TipType;
            TerminatingRaySegment = ray_in.TerminatingRaySegment;
            TerminatingFracture = ray_in.TerminatingFracture;
        }
    }

    /// <summary>
    /// Unconfined fracture object comprising a series of rays propagating outwards from a single point, spaced at equal angles from the top of the fracture
    /// </summary>
    class UnconfinedFractureXYZ : IComparable<UnconfinedFractureXYZ>
    {
        // Unique unconfined fracture ID number
        /// <summary>
        /// Global unconfined fracture counter - used to set an ID for each new unconfined fracture object
        /// </summary>
        private static int unconfinedfractureCounter = 0;
        /// <summary>
        /// Unique unconfined fracture ID 
        /// </summary>
        public int UnconfinedFractureID { get; private set; }

        // References to external objects

        // Fracture geometry data
        /// <summary>
        /// Array of rays
        /// </summary>
        private UnconfinedFractureRay[] rays;
        /// <summary>
        /// Get a list of all ray segments of this fracture that lie within a specified gridblock
        /// </summary>
        /// <param name="gbc_in">Reference to the Gridblock object to be checked</param>
        /// <returns>List of UnconfinedFractureRaySegment objects lying within the specified gridblock</returns>
        public List<UnconfinedFractureRaySegment> GetRaySegmentsInGridblock(GridblockConfiguration gbc_in)
        {
            List<UnconfinedFractureRaySegment> segmentsInGridblock = new List<UnconfinedFractureRaySegment>();
            foreach (UnconfinedFractureRay ray in rays)
                segmentsInGridblock.AddRange(ray.GetRaySegmentsInGridblock(gbc_in));
            return segmentsInGridblock;
        }

        // Geometric data
        /// <summary>
        /// Number of rays comprising the fracture
        /// </summary>
        public int NoRays { get { return rays.Length; } }
        /// <summary>
        /// Length of the shortest ray; this will control the fracture aperture and propagation rate
        /// </summary>
        public double MinimumRayLength { get { double minLength = double.PositiveInfinity; foreach (UnconfinedFractureRay ray in rays) { double rayLength = ray.Length; if (minLength > rayLength) minLength = rayLength; } return minLength; }  }
        /// <summary>
        /// Length of the longest ray
        /// </summary>
        public double MaximumRayLength { get { double maxLength = 0; foreach (UnconfinedFractureRay ray in rays) { double rayLength = ray.Length; if (maxLength < rayLength) maxLength = rayLength; } return maxLength; } }
        /// <summary>
        /// Mean ray length
        /// </summary>
        public double MeanRayLength { get { double totalLength = 0; foreach (UnconfinedFractureRay ray in rays) totalLength += ray.Length; return totalLength / NoRays; } }
        /// <summary>
        /// The mean effective fracture radius, used for calculating whole fracture stress shadow width, is the average of the minimum (or propagation contolling) ray length and the mean ray length
        /// </summary>
        public double MeanEffectiveRadius { get { return (MinimumRayLength + MeanRayLength) / 2; } }
        /// <summary>
        /// Total area of the fracture
        /// </summary>
        public double Area { get { double area = 0; foreach (UnconfinedFractureRay ray in rays) area += ray.Area; return area; } }
        /// <summary>
        /// Fracture set index number - this will not change after fracture is initiated
        /// </summary>
        public int SetIndex { get; private set; }
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
        /// <summary>
        /// Location of the fracture nucleation point; this will be the innermost node of all rays
        /// </summary>
        public PointXYZ NucleationPoint { get; private set; }
        /// <summary>
        /// Get the centroid of the fracture - this is defined as the mean of the position of all nodes on all rays
        /// </summary>
        public PointXYZ Centroid
        {
            get
            {
                List<PointXYZ> outerTips = new List<PointXYZ>();
                foreach (UnconfinedFractureRay ray in rays)
                    outerTips.AddRange(ray.GetNodesInXYZ());
                return PointXYZ.getCentroid(outerTips);
            }
        }

        // Dynamic data
        /// <summary>
        /// True if all rays are still active; otherwise false
        /// </summary>
        public bool FullyActive { get { foreach (UnconfinedFractureRay ray in rays) if (!ray.Active) return false; return true; } }
        /// <summary>
        /// True if all rays are deactivated; false if any rays are still active
        /// </summary>
        public bool FullyDeactivated { get { foreach (UnconfinedFractureRay ray in rays) if (ray.Active) return false; return true; } }
        /// <summary>
        /// Time of fracture nucleation (real time) - this will not change after fracture is initiated
        /// </summary>
        public double NucleationTime { get; private set; }

        // Reset, data input, control and implementation functions
        public void PopulateData()
        {
            // Add code to create and tidy up fractures prior to populating the DFN, if needed
        }
        /// <summary>
        /// Criterion to use when sorting unconfined fractures
        /// </summary>
        public static SortProperty SortCriterion { get; set; }
        /// <summary>
        /// Compare UnconfinedFractureXYZ objects based on the sort criterion specified by SortCriterion
        /// </summary>
        /// <param name="that">UnconfinedFractureXYZ object to compare with</param>
        /// <returns>Negative if this UnconfinedFractureXYZ object has the greatest value of the specified property, positive if that UnconfinedFractureXYZ object has the greatest value of the specified property, zero if they have equal values</returns>
        public int CompareTo(UnconfinedFractureXYZ that)
        {
            switch (SortCriterion)
            {
                case SortProperty.Size_SmallestFirst:
                    return this.Area.CompareTo(that.Area);
                case SortProperty.Size_LargestFirst:
                    return -this.Area.CompareTo(that.Area);
                case SortProperty.NucleationTime:
                    return this.NucleationTime.CompareTo(that.NucleationTime);
                default:
                    return 0;
            }
        }

        // Output functions
        /// <summary>
        /// Get the number of triangular elements required to represent this fracture
        /// </summary>
        /// <returns></returns>
        public int NoTriangularElements()
        {
            int noElements = 0;
            // Each segment in the ray will generate two triangular elements, except the innermost segment which will only generate 1
            foreach (UnconfinedFractureRay ray in rays)
                if (ray.NoSegments > 0)
                    noElements += ((2 * ray.NoSegments) - 1);
            return noElements;
        }
        /// <summary>
        /// Function to return a list of the XYZ coordinates of the cornerpoints of each segment of the fractures
        /// </summary>
        /// <returns>A primary list, each item representing a fracture segment, containing nested arrays of cornerpoints as PointXYZ objects</returns>
        public List<PointXYZ[]> GetFractureSegmentsInXYZ()
        {
            // Create a new list object for the cornerpoint lists for each segment
            // This will be populated with new objects, not references to the existing UnconfinedFractureXYZ member objects
            List<PointXYZ[]> NewElementList = new List<PointXYZ[]>();

            // Get a list of the position of the nodes along each ray
            List<PointXYZ>[] nodeArray = new List<PointXYZ>[NoRays];
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                List<PointXYZ> nodeList = rays[rayNo].GetNodesInXYZ();
                nodeArray[rayNo] = nodeList;
            }

            // Move outwards from the centre of the fracture along the rays, one point at a time
            // There is only one element representing the central segments
            PointXYZ[] centralElement = new PointXYZ[NoRays];
            int nodeIndex = 1;
            List<PointXYZ> previousRayNodes = nodeArray[NoRays - 1];
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                // Check if there are sufficient nodes on the rays to create this element
                if ((previousRayNodes.Count > nodeIndex) && (currentRayNodes.Count > nodeIndex))
                {
                    // Add a copy of the first node on this ray to the array
                    centralElement[rayNo] = new PointXYZ(currentRayNodes[nodeIndex]);

                }

                // Update the previous ray node list
                previousRayNodes = currentRayNodes;
            }
            // Add the new array to the list of elements
            NewElementList.Add(centralElement);
            // Now move outwards
            bool moveToNextNodeIndex;
            do
            {
                // Increment the node index and set the next node index flag to false; this will only be set to true if there is at least one double element created
                nodeIndex++;
                moveToNextNodeIndex = false;

                // Loop through all the arrays
                for (int rayNo = 0; rayNo < NoRays; rayNo++)
                {
                    List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                    // Check if there are sufficient inner nodes on the rays to create one or more element
                    if ((previousRayNodes.Count > nodeIndex - 1) && (currentRayNodes.Count > nodeIndex - 1))
                    {
                        PointXYZ innerNode1 = new PointXYZ(previousRayNodes[nodeIndex - 1]);
                        PointXYZ innerNode2 = new PointXYZ(currentRayNodes[nodeIndex - 1]);

                        // Check if there are sufficient outer nodes to create one or two elements
                        if ((previousRayNodes.Count > nodeIndex) && (currentRayNodes.Count > nodeIndex))
                        {
                            PointXYZ outerNode1 = new PointXYZ(previousRayNodes[nodeIndex]);
                            PointXYZ outerNode2 = new PointXYZ(currentRayNodes[nodeIndex]);

                            // With four points we can create two elements
                            PointXYZ[] element1 = new PointXYZ[3];
                            element1[0] = innerNode1;
                            element1[1] = innerNode2;
                            element1[2] = outerNode1;

                            PointXYZ[] element2 = new PointXYZ[3];
                            element2[0] = innerNode2;
                            element2[1] = outerNode1;
                            element2[2] = outerNode2;

                            // Add the new arrays to the list of elements
                            NewElementList.Add(element1);
                            NewElementList.Add(element2);

                            // Set the next node index flag to false
                            moveToNextNodeIndex = true;
                        }
                        else if (previousRayNodes.Count > nodeIndex)
                        {
                            // With three points we can only create elements bounding one ray - but there may be many of these
                            for (int nodeIndex2 = nodeIndex; nodeIndex2 < previousRayNodes.Count; nodeIndex2++)
                            {
                                PointXYZ outerNode1 = new PointXYZ(previousRayNodes[nodeIndex2]);

                                PointXYZ[] element1 = new PointXYZ[3];
                                element1[0] = innerNode1;
                                element1[1] = innerNode2;
                                element1[2] = outerNode1;

                                // Add the new array to the list of elements
                                NewElementList.Add(element1);
                            }
                        }
                        else if (currentRayNodes.Count > nodeIndex)
                        {
                            // With three points we can only create elements bounding one ray - but there may be many of these
                            for (int nodeIndex2 = nodeIndex; nodeIndex2 < currentRayNodes.Count; nodeIndex2++)
                            {
                                PointXYZ outerNode2 = new PointXYZ(currentRayNodes[nodeIndex2]);

                                // With three points we can only create one elements
                                PointXYZ[] element1 = new PointXYZ[3];
                                element1[0] = innerNode1;
                                element1[1] = innerNode2;
                                element1[2] = outerNode2;

                                // Add the new array to the list of elements
                                NewElementList.Add(element1);
                            }
                        }
                    }

                    // Update the previous ray node list
                    previousRayNodes = currentRayNodes;
                }
            }
            while (moveToNextNodeIndex);

            // Return the new segment list object
            return NewElementList;
        }
        /// <summary>
        /// Function to return a list of the XYZ coordinates of the cornerpoints of each element of a triangular mesh representing the fracture
        /// </summary>
        /// <returns>A primary list, each item representing a triangular element, containing nested lists of cornerpoints as PointXYZ objects</returns>
        public List<PointXYZ[]> GetTriangularFractureSegmentsInXYZ()
        {
            // Create a new list object for the cornerpoint lists for each segment
            // This will be populated with new objects, not references to the existing UnconfinedFractureXYZ member objects
            List<PointXYZ[]> NewElementList = new List<PointXYZ[]>();

            // Get a list of the position of the nodes along each ray
            List<PointXYZ>[] nodeArray = new List<PointXYZ>[NoRays];
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                List<PointXYZ> nodeList = rays[rayNo].GetNodesInXYZ();
                nodeArray[rayNo] = nodeList;
            }

            // Move outwards from the centre of the fracture along the rays, one point at a time
            // First add the elements representing the central segments
            int nodeIndex = 1;
            List<PointXYZ> previousRayNodes = nodeArray[NoRays - 1];
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                // Check if there are sufficient nodes on the rays to create this element
                if ((previousRayNodes.Count > nodeIndex ) && (currentRayNodes.Count > nodeIndex))
                {
                    // Create an array of three points to represent the element
                    PointXYZ[] nextElement = new PointXYZ[3];
                    // Add a copy of the fracture centrepoint to the array
                    nextElement[0] = new PointXYZ(NucleationPoint);
                    // Add copies of the first node on this ray and the previous ray to the array
                    nextElement[1] = new PointXYZ(previousRayNodes[nodeIndex]);
                    nextElement[2] = new PointXYZ(currentRayNodes[nodeIndex]);

                    // Add the new array to the list of elements
                    NewElementList.Add(nextElement);
                }

                // Update the previous ray node list
                previousRayNodes = currentRayNodes;
            }
            // Now move outwards
            bool moveToNextNodeIndex;
            do
            {
                // Increment the node index and set the next node index flag to false; this will only be set to true if there is at least one double element created
                nodeIndex++;
                moveToNextNodeIndex = false;

                // Loop through all the arrays
                for (int rayNo = 0; rayNo < NoRays; rayNo++)
                {
                    List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                    // Check if there are sufficient inner nodes on the rays to create one or more element
                    if ((previousRayNodes.Count > nodeIndex - 1) && (currentRayNodes.Count > nodeIndex - 1))
                    {
                        PointXYZ innerNode1 = new PointXYZ(previousRayNodes[nodeIndex - 1]);
                        PointXYZ innerNode2 = new PointXYZ(currentRayNodes[nodeIndex - 1]);

                        // Check if there are sufficient outer nodes to create one or two elements
                        if ((previousRayNodes.Count > nodeIndex) && (currentRayNodes.Count > nodeIndex))
                        {
                            PointXYZ outerNode1 = new PointXYZ(previousRayNodes[nodeIndex]);
                            PointXYZ outerNode2 = new PointXYZ(currentRayNodes[nodeIndex]);

                            // With four points we can create two elements
                            PointXYZ[] element1 = new PointXYZ[3];
                            element1[0] = innerNode1;
                            element1[1] = innerNode2;
                            element1[2] = outerNode1;

                            PointXYZ[] element2 = new PointXYZ[3];
                            element2[0] = innerNode2;
                            element2[1] = outerNode1;
                            element2[2] = outerNode2;

                            // Add the new arrays to the list of elements
                            NewElementList.Add(element1);
                            NewElementList.Add(element2);

                            // Set the next node index flag to false
                            moveToNextNodeIndex = true;
                        }
                        else if (previousRayNodes.Count > nodeIndex)
                        {
                            // With three points we can only create elements bounding one ray - but there may be many of these
                            for (int nodeIndex2 = nodeIndex; nodeIndex2 < previousRayNodes.Count; nodeIndex2++)
                            {
                                PointXYZ outerNode1 = new PointXYZ(previousRayNodes[nodeIndex2]);

                                PointXYZ[] element1 = new PointXYZ[3];
                                element1[0] = innerNode1;
                                element1[1] = innerNode2;
                                element1[2] = outerNode1;

                                // Add the new array to the list of elements
                                NewElementList.Add(element1);
                            }
                        }
                        else if (currentRayNodes.Count > nodeIndex)
                        {
                            // With three points we can only create elements bounding one ray - but there may be many of these
                            for (int nodeIndex2 = nodeIndex; nodeIndex2 < currentRayNodes.Count; nodeIndex2++)
                            {
                                PointXYZ outerNode2 = new PointXYZ(currentRayNodes[nodeIndex2]);

                                // With three points we can only create one elements
                                PointXYZ[] element1 = new PointXYZ[3];
                                element1[0] = innerNode1;
                                element1[1] = innerNode2;
                                element1[2] = outerNode2;

                                // Add the new array to the list of elements
                                NewElementList.Add(element1);
                            }
                        }
                    }

                    // Update the previous ray node list
                    previousRayNodes = currentRayNodes;
                }
            }
            while (moveToNextNodeIndex);

            // Return the new segment list object
            return NewElementList;
        }
        /// <summary>
        /// Create a single list of all the outer cornerpoints of the fracture
        /// </summary>
        /// <returns></returns>
        public List<PointXYZ> GetCornerpoints()
        {
            // Create a new cornerpoint list object
            List<PointXYZ> CornerPoints = new List<PointXYZ>();

            // Loop through all the rays in turn, getting the outermost cornerpoint
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                List<PointXYZ> currentRayNodes = rays[rayNo].GetNodesInXYZ();

                // Add the outermost point to the array list
                int outermostNodeIndex = currentRayNodes.Count - 1;
                CornerPoints.Add(new PointXYZ(currentRayNodes[outermostNodeIndex]));
            }

            // Add an extra copy of the first point on the list to close the loop
            if (CornerPoints.Count > 0)
                CornerPoints.Add(new PointXYZ(CornerPoints[0]));

            // Return the cornerpoint list
            return CornerPoints;
        }

        // Constructors
        /// <summary>
        /// Create a new fracture nucleating within a gridblock
        /// </summary>
        /// <param name="ufs_in">Reference to parent FractureSet object</param>
        /// <param name="gbc_in">Reference to grandparent Gridblock object</param>
        /// <param name="setIndex_in">Index number of the parent FractureSet object</param>
        /// <param name="NucleationPoint_in">PointXYZ representing the point of fracture nucleation</param>
        /// <param name="Orientation">VectorXYZ object representing the ray segment orientation, in the direction of propagation</param>
        /// <param name="NoRays">Number of rays making up the fracture; these will be spaced at equal angles from the top of the fracture</param>
        /// <param name="InitialRadius">Initial radius of the fracture</param>
        /// <param name="NucleationWTime_in">Fracture growth weighted time (WTime) of fracture nucleation</param>
        /// <param name="NucleationTimestep_in">Timestep of nucleation</param>
        public UnconfinedFractureXYZ(UnconfinedFractureSet ufs_in, GridblockConfiguration gbc_in, int setIndex_in, PointXYZ NucleationPoint_in, VectorXYZ Orientation, int NoRays, double InitialRadius, double NucleationWTime_in, int NucleationTimestep_in)
        {
            // Assign the new object an ID number and increment the unconfined fracture counter
            UnconfinedFractureID = ++unconfinedfractureCounter;

            // Fracture set index number - this will not change after fracture is initiated
            SetIndex = setIndex_in;

            // Set the fracture nucleation point
            NucleationPoint = NucleationPoint_in;

            // Set the fracture orientation data
            // The normal vector should be oriented pointing upwards, so dip is negative
            // If it is pointing the wrong way it should be inverted
            NormalVector = Orientation.GetNormalisedVector();
            if (NormalVector.Component(VectorComponents.Z) < 0)
                NormalVector = -NormalVector;

            // Set the true fracture nucleation time
            NucleationTime = ufs_in.ConvertWeightedTimeToTime(NucleationWTime_in, NucleationTimestep_in);

            // Create a new array of rays and populate it, calculating the orientation of each initial ray segment
            rays = new UnconfinedFractureRay[NoRays];
            VectorXYZ[] rayVectors = ufs_in.RayVectors;
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                // Create a new ray and add it to the array
                VectorXYZ nextRayVector = rayVectors[rayNo];
                UnconfinedFractureRay nextRay = new UnconfinedFractureRay(this, ufs_in, gbc_in, NucleationPoint_in, nextRayVector, InitialRadius, NucleationTime, NucleationWTime_in, NucleationTimestep_in);
                rays[rayNo] = nextRay;
            }
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing UnconfinedFractureXYZ object
        /// </summary>
        /// <param name="fracture_in">Reference to an existing UnconfinedFractureXYZ object to copy</param>
        public UnconfinedFractureXYZ(UnconfinedFractureXYZ fracture_in)
        {
            // Assign the new object an ID number and increment the unconfined fracture counter
            UnconfinedFractureID = ++unconfinedfractureCounter;

            // Fracture set index number - this will not change after fracture is initiated
            SetIndex = fracture_in.SetIndex;

            // Set the fracture nucleation point
            NucleationPoint = fracture_in.NucleationPoint;

            // Set the fracture orientation data
            NormalVector = fracture_in.NormalVector;

            // Create a new array of rays and populate it with copies of the UnconfinedFractureRay objects in the input UnconfinedFractureXYZ object
            rays = new UnconfinedFractureRay[fracture_in.NoRays];
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                rays[rayNo] = new UnconfinedFractureRay(fracture_in.rays[rayNo]);
            }

            // Set the nucleation time
            NucleationTime = fracture_in.NucleationTime;
        }
    }

    /*/// <summary>
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
        private List<UnconfinedFractureXYZ> fractures;

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
            UnconfinedFractureXYZ newFracture = new UnconfinedFractureXYZ(this, ufs_in, gbc_in, NucleationPoint, Orientation, NoRays, InitialRadius, NucleationTime_in, NucleationTimestep_in);
            fractures.Add(newFracture);
        }

        // Constructors
        public UnconfinedDFN(FractureGrid gd_in)
        {
            // Set the reference to the parent grid object
            gd = gd_in;

            // Create a new (empty) fracture list
            fractures = new List<UnconfinedFractureXYZ>();
        }
    }*/
}
