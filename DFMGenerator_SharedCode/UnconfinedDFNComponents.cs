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
    public enum RaySegmentPropagationRateControl { Critical, SubcriticalFullyActive, SubcriticalRestricted, GrowToInitialSize }

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
        //private UnconfinedFractureRay ufr;
        public UnconfinedFractureRay ufr;
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
        /// <summary>
        /// Parent unconfined fracture ID 
        /// </summary>
        public int UnconfinedFractureID { get { return ucf.UnconfinedFractureID; } }

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
        /// <summary>
        /// Check if the ray that this segment belongs to nucleated in a specific gridblock
        /// </summary>
        /// <param name="GridblockToCheck">Reference to the gridblock to check for nucleation</param>
        /// <returns>True if this ray nucleated in the specified gridblock, otherwise false</returns>
        public bool CheckNucleationGridblock(GridblockConfiguration GridblockToCheck)
        {
            return ucf.CheckNucleationGridblock(GridblockToCheck);
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
        /// Maximum effective length that this ray can grow to; when this is reached, the ray will continue propagating but velocity and stress shadow width will be independent of fracture size
        /// </summary>
        public double MaximumEffectiveRayLength { get { return ufs.MaximumEffectiveFractureRadius; } }
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
        /// Flag to specify tip type of the ray containing this segment
        /// </summary>
        public FractureTipType RayTipType { get { return ufr.TipType; } }
        /// <summary>
        /// Flag to specify if this ray segment is being grown to the mininum unconfined fracture radius
        /// This is done to ensure boundary intersections and other interactions are correctly modelled
        /// </summary>
        public bool GrowToInitialSize { get { return ufr.GrowToInitialSize; } set { if (Active) ufr.GrowToInitialSize = value; } }
        /// <summary>
        /// Flag for active segment propagation - true if the fracture segment is still propagating within the current gridblock
        /// </summary>
        public bool Active { get { return (PropNodeType == SegmentNodeType.Propagating); } }
        /// <summary>
        /// Fracture state - true if the fracture is fully active (all rays are active), otherwise false
        /// </summary>
        public bool FractureFullyActive { get { return ucf.FullyActive; } }
        /// <summary>
        /// True if the fracture has reached the maximum effective radius, in which case propagation rate and stress shadow width will no longer be dependent on fracture size
        /// </summary>
        public bool ConstantKi { get { return ucf.ConstantPropagationRate; } }
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

            // Add a reference to the entire fracture to the new fracture set, if it is not already there
            if (!ufs_in.LocalDFNUnconfinedFractures.Contains(ucf))
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

            // Set the node types
            NonPropNodeType = SegmentNodeType.NucleationPoint;
            PropNodeType = SegmentNodeType.Propagating;

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
            NonPropNodeBoundary = PointXYZ.GetOppositeDirection(initiatorRaySegment.PropNodeBoundary);
            PropNodeBoundary = GridDirection.None;
            unitVector = Orientation.GetNormalisedVector();
            // The ray segment initially has zero length
            Length = 0;

            // Set the node types and propagation rate control
            NonPropNodeType = SegmentNodeType.ConnectedGridblockBound;
            PropNodeType = SegmentNodeType.Propagating;

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
        /// <summary>
        /// Parent unconfined fracture ID 
        /// </summary>
        public int UnconfinedFractureID { get { return ucf.UnconfinedFractureID; } }

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
        public FractureTipType TipType { get { return GetRayTipType(); } }
        /// <summary>
        /// Flag to specify if the ray is being grown to the mininum unconfined fracture radius
        /// This is done to ensure boundary intersections and other interactions are correctly modelled
        /// </summary>
        public bool GrowToInitialSize { get; set; }
        /// <summary>
        /// Ray tip state - true if the ray tip is still propagating
        /// </summary>
        public bool Active { get { if (segments.Count > 0) return (segments[segments.Count - 1].PropNodeType == SegmentNodeType.Propagating); else return false; } }
        /*/// <summary>
        /// Reference to the ray segment that terminates this ray, by intersection or stress shadow interaction; initially set to null
        /// </summary>
        public UnconfinedFractureRaySegment TerminatingRaySegment { get; private set; }*/
        /// <summary>
        /// ID number of the fracture that terminates the specified tip of this macrofracture, by intersection or stress shadow interaction; -1 if there is no terminating fracture
        /// </summary>
        public int TerminatingFracture { get { return GetTerminatingFracture(); } }
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
        /// <summary>
        /// Get the tip type for the ray
        /// </summary>
        private FractureTipType GetRayTipType()
        {
            if (NoSegments > 0)
            {
                UnconfinedFractureRaySegment outerSegment = segments[NoSegments - 1];
                switch (outerSegment.PropNodeType)
                {
                    // NB The nucleation point cannot be the outermost node
                    case SegmentNodeType.Propagating:
                        return FractureTipType.Propagating;
                    case SegmentNodeType.ConnectedStressShadow:
                        return FractureTipType.StressShadow;
                    case SegmentNodeType.NonconnectedStressShadow:
                        return FractureTipType.StressShadow;
                    case SegmentNodeType.Intersection:
                        return FractureTipType.Intersection;
                    case SegmentNodeType.Convergence:
                        return FractureTipType.Convergence;
                    // NB A connected gridblock boundary cannot be the outermost node
                    case SegmentNodeType.NonconnectedGridblockBound:
                        return FractureTipType.OutOfBounds;
                    // NB The outermost segment can be a relay segment, if the fracture interacts with the stress shadow of an inactive fracture segment
                    case SegmentNodeType.Relay:
                        return FractureTipType.StressShadow;
                    case SegmentNodeType.Pinchout:
                        return FractureTipType.Pinchout;
                    case SegmentNodeType.Arrested:
                        return FractureTipType.Arrested;
                    default:
                        return FractureTipType.OutOfBounds;
                }
            }
            else
                return FractureTipType.OutOfBounds;
        }
        /// <summary>
        /// Get the terminating fracture for the ray
        /// </summary>
        private int GetTerminatingFracture()
        {
            if (NoSegments > 0)
            {
                UnconfinedFractureRaySegment outerSegment = segments[NoSegments - 1];
                switch (outerSegment.PropNodeType)
                {
                    // NB The nucleation point cannot be the outermost node
                    case SegmentNodeType.Propagating:
                        {
                            // Still propagating so no terminating fracture
                        }
                        break;
                    case SegmentNodeType.ConnectedStressShadow:
                        {
                            // With a connected stress shadow, the fracture tip interacts directly with the stress shadow of a similar sized fracture propagating in the opposite direction
                            if (!(outerSegment.TerminatingFracture is null))
                                return outerSegment.TerminatingFracture.UnconfinedFractureID;
                        }
                        break;
                    case SegmentNodeType.NonconnectedStressShadow:
                        {
                            // With a nonconnected stress shadow, the fracture tip becomes enveloped in the stress shadow of a larger fracture
                            // There is no direct connection to the larger fracture 
                        }
                        break;
                    case SegmentNodeType.Intersection:
                        {
                            // With an intersection, the fracture tip terminates against another fracture from a different set
                            if (!(outerSegment.TerminatingFracture is null))
                                return outerSegment.TerminatingFracture.UnconfinedFractureID;
                        }
                        break;
                    case SegmentNodeType.Convergence:
                        {
                            // Does not apply to unconfined fractures
                        }
                        break;
                    // NB A connected gridblock boundary cannot be the outermost node
                    case SegmentNodeType.NonconnectedGridblockBound:
                        {
                            // Still propagating so no terminating fracture
                        }
                        break;
                    // NB The outermost segment can be a relay segment, if the fracture interacts with the stress shadow of an inactive fracture segment
                    case SegmentNodeType.Relay:
                        {
                            if (!(outerSegment.TerminatingFracture is null))
                                return outerSegment.TerminatingFracture.UnconfinedFractureID;
                        }
                        break;
                    case SegmentNodeType.Pinchout:
                        {
                            // Fracture stops propagating because the unit it is confined to becomes too thin; no terminating fracture
                        }
                        break;
                    case SegmentNodeType.Arrested:
                        {
                            // Fracture stops propagating for an undefined reason; no terminating fracture
                        }
                        break;
                    default:
                        {
                            // No terminating fracture
                        }
                        break;
                }
            }
            // If there is no terminating fracture return -1
            return -1;
        }
        /// <summary>
        /// Check if the ray nucleated in a specific gridblock
        /// </summary>
        /// <param name="GridblockToCheck">Reference to the gridblock to check for nucleation</param>
        /// <returns>True if this ray nucleated in the specified gridblock, otherwise false</returns>
        public bool CheckNucleationGridblock(GridblockConfiguration GridblockToCheck)
        {
            return ucf.CheckNucleationGridblock(GridblockToCheck);
        }

        // Reset and data input functions
        /// <summary>
        /// Go through each ray and remove zero-length segments
        /// </summary>
        public void RemoveZeroLengthSegments()
        {
            // Loop through all segments in the ray except the innermost - this can never be removed
            for(int SegmentNo = NoSegments - 1; SegmentNo > 0; SegmentNo--)
            if (NoSegments > 1)
            {
                // Check if the outermost segment has length zero, negative or NaN
                UnconfinedFractureRaySegment outerSegment = segments[SegmentNo];
                if (!(outerSegment.Length > 0))
                {
                    UnconfinedFractureRaySegment innerSegment = segments[SegmentNo - 1];
                    // Set the node type and terminating fracture of the next outermost segment to that of the zero-length segment to be removed
                    innerSegment.PropNodeType = outerSegment.PropNodeType;
                    innerSegment.TerminatingFracture = outerSegment.TerminatingFracture;
                    // Remove the zero-length segment
                    segments.RemoveAt(SegmentNo);
                }
            }
        }

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

            // Set the ray tip and propagation rate control data

            // Initially the fracture will be subcritical and fully active, unless the initial length is 0
            // In this case we will assume that the fracture is being allowed to grow to the mininum unconfined fracture radius
            // This will ensure boundary intersections and other interactions are correctly modelled
            if (InitialLength > 0)
                GrowToInitialSize = false;
            else
                GrowToInitialSize = true;
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

            // Set the ray tip and propagation rate control data
            //TerminatingRaySegment = ray_in.TerminatingRaySegment;
            GrowToInitialSize = ray_in.GrowToInitialSize;
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
        private static int unconfinedFractureCounter = 0;
        /// <summary>
        /// Unique unconfined fracture ID 
        /// </summary>
        public int UnconfinedFractureID { get; private set; }

        // References to external objects
        /// <summary>
        /// Reference to the gridblock in which the fracture nucleated
        /// </summary>
        private GridblockConfiguration nucleationGridblock;
        /// <summary>
        /// Reference to parent UnconfinedFractureSet object of the initial nucleating segments
        /// </summary>
        private UnconfinedFractureSet ufs;
        /// <summary>
        /// Check if the fracture nucleated in a specific gridblock
        /// </summary>
        /// <param name="GridblockToCheck">Reference to the gridblock to check for nucleation</param>
        /// <returns>True if this fracture nucleated in the specified gridblock, otherwise false</returns>
        public bool CheckNucleationGridblock(GridblockConfiguration GridblockToCheck)
        {
            return GridblockConfiguration.ReferenceEquals(GridblockToCheck, nucleationGridblock);
        }

        // Fracture geometry data
        /// <summary>
        /// Array of rays
        /// </summary>
        private UnconfinedFractureRay[] rays;
        /// <summary>
        /// Get a list of all rays of this fracture
        /// </summary>
        /// <returns>List of UnconfinedFractureRay objects</returns>
        public List<UnconfinedFractureRay> GetRays()
        {
            List<UnconfinedFractureRay> output = new List<UnconfinedFractureRay>();
            foreach (UnconfinedFractureRay ray in rays)
                output.Add(new UnconfinedFractureRay(ray));
            return output;
        } 
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
        /// <summary>
        /// Flag for whether this fracture should be treated as a large fracture, considered to influence the entire grid when checking stress shadows
        /// </summary>
        public bool LargeFracture { get; set; }

        // Geometric data
        /// <summary>
        /// Number of rays comprising the fracture
        /// </summary>
        public int NoRays { get { return rays.Length; } }
        /// <summary>
        /// Length of the shortest ray; this will control the fracture aperture and propagation rate
        /// </summary>
        public double MinimumRayLength { get; private set; }
        /// <summary>
        /// Length of the longest ray
        /// </summary>
        public double MaximumRayLength { get; private set; }
        /// <summary>
        /// Mean ray length
        /// NB This is calculated the root mean square ray length, to ensure that the area of the fracture calculated from the mean ray length will equal the sum of the area of all the rays
        /// </summary>
        public double MeanRayLength { get; private set; }
        /// <summary>
        /// The effective fracture radius, used for calculating whole fracture stress shadow width, is the average of the minimum (or propagation contolling) ray length and the mean ray length, weighted by area
        /// The average is weighted by area to ensure that the whole fracture stress shadow volume calculated from fracture effective radius is equal to the sum of the stress shadow volume around all the rays
        /// </summary>
        public double EffectiveRadius { get; private set; }
        /// <summary>
        /// Total area of the fracture
        /// </summary>
        public double Area { get; private set; }
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
        public PointXYZ Centroid { get; private set; }
        /// <summary>
        /// Recalculate the minimum, maximum and mean ray lengths, total fracture area and P33 volume (for calculating effective radius), flags for fully active and fully deactivated and location of centroid
        /// This should be done after all fracture growth has been calculated for the timestep, as these values will be used to calculate effective radii and fracture growth rates in the next timestep
        /// </summary>
        public void RecalculateGeometry()
        {
            double minLength = double.PositiveInfinity; 
            double maxLength = 0; 
            //double totalLength = 0; 
            List<PointXYZ> outerTips = new List<PointXYZ>();
            bool fullyActive = true;
            bool fullyDeactivated = true;
            // Loop through the rays once to get the minimum and maximum ray lengths, flags for fully active and fully deactivated and location of centroid
            foreach (UnconfinedFractureRay ray in rays)
            {
                double rayLength = ray.Length;
                if (minLength > rayLength)
                    minLength = rayLength;
                if (maxLength < rayLength)
                    maxLength = rayLength;
                //totalLength += rayLength;
                //outerTips.AddRange(ray.GetNodesInXYZ());
                outerTips.Add(new PointXYZ(ray.OuterTip));
                if (ray.Active)
                    fullyDeactivated = false;
                else
                    fullyActive = false;
            }
            MinimumRayLength = minLength;
            MaximumRayLength = maxLength;
            Centroid = PointXYZ.getCentroid(outerTips);
            FullyActive = fullyActive;
            FullyDeactivated = fullyDeactivated;

            // Loop through the rays a second time to get the total fracture area and P33 volume (for calculating effective radius)
            // NB the P33 volume required the effective ray length, thus can only be calculated after the minimum ray length has been determined
            double areaElements = 0;
            double volumeElements = 0;
            // Loop through the rays once to get the minimum and maximum ray lengths, flags for fully active and fully deactivated and location of centroid
            foreach (UnconfinedFractureRay ray in rays)
            {
                double rayLength = ray.Length;
                double lengthSquared = (rayLength * rayLength);
                double effectiveLength = (rayLength + MinimumRayLength) / 2;
                areaElements += lengthSquared;
                volumeElements += lengthSquared * effectiveLength;
            }
            Area = Math.PI * areaElements;
            MeanRayLength = Math.Sqrt(areaElements / NoRays);
            EffectiveRadius = volumeElements / areaElements;
        }

        // Fracture properties
        /// <summary>
        /// Fracture aperture, averaged across the fracture surface
        /// </summary>
        public double MeanAperture { get; private set; }
        /// <summary>
        /// Fracture compressibility, based on the aperture control data
        /// </summary>
        public double Compressibility { get; private set; }

        // Dynamic data
        /// <summary>
        /// True if all rays are still active; otherwise false
        /// </summary>
        public bool FullyActive { get; private set; }
        /// <summary>
        /// True if all rays are deactivated; false if any rays are still active
        /// </summary>
        public bool FullyDeactivated { get; private set; }
        /// <summary>
        /// True if the fracture has reached the maximum effective radius, in which case propagation rate and stress shadow width will no longer be dependent on fracture size
        /// </summary>
        public bool ConstantPropagationRate { get { return (EffectiveRadius > ufs.MaximumEffectiveFractureRadius); } }
        /// <summary>
        /// Time of fracture nucleation (real time) - this will not change after fracture is initiated
        /// </summary>
        public double NucleationTime { get; private set; }

        // Reset, data input, control and implementation functions
        /// <summary>
        /// Populate data: nothing is required as the fracture is already in XYZ coordinates and no bevelling is needed
        /// </summary>
        public void PopulateData()
        {
            // Set the mean fracture aperture based on the current stress field
            MeanAperture = ufs.getMeanFractureAperture(EffectiveRadius);
            // Set the fracture compressibility based on the aperture control data
            Compressibility = ufs.getFractureCompressibility(EffectiveRadius);
        }
        /// <summary>
        /// Remove zero-length segments from each ray
        /// </summary>
        public void RemoveZeroLengthSegments()
        {
            foreach (UnconfinedFractureRay ray in rays)
                ray.RemoveZeroLengthSegments();
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
        /// Function to return a list of the XYZ coordinates of the nodes along each ray of the fracture
        /// </summary>
        /// <returns>A primary array, each item representing a ray, containing a nested list of nodes as PointXYZ objects</returns>
        public List<PointXYZ>[] GetRayNodes()
        {
            // Get a list of the position of the nodes along each ray
            List<PointXYZ>[] nodeArray = new List<PointXYZ>[NoRays];
            for (int rayNo = 0; rayNo < NoRays; rayNo++)
            {
                List<PointXYZ> nodeList = rays[rayNo].GetNodesInXYZ();
                nodeArray[rayNo] = nodeList;
            }
            return nodeArray;
        }
        /// <summary>
        /// Subdivide the fracture into planar patches and return total number of patches
        /// </summary>
        /// <param name="CreateTriangularPatches">Flag to create only triangular patches: if true, all patches will be triangular; if false, a single patch will be created representing the innermost segments, but the patches representing outer segments will still be triangular </param>
        /// <returns>The number of planar patches comprising the fracture</returns>
        public int GetNoFracturePatchesInXYZ(bool CreateTriangularPatches)
        {
            // Create a counter for the number of patches
            int noPatches = 0;

            // Get a list of the position of the nodes along each ray
            List<PointXYZ>[] nodeArray = GetRayNodes();

            // If the CreateTriangularSegments flag is set to true, there will be one innermost patch per ray, unless the ray length is zero
            if (CreateTriangularPatches)
            {
                foreach (List<PointXYZ> rayNodes in nodeArray)
                    if (rayNodes.Count > 0)
                        noPatches++;
            }
            // Otherwise there will be just one innermost patch
            else
            {
                noPatches++;
            }

            // There will be two patches corresponding to each of the outer segments
            foreach (List<PointXYZ> rayNodes in nodeArray)
            {
                int outerSegmentCount = rayNodes.Count - 2;
                if (outerSegmentCount < 0)
                    outerSegmentCount = 0;
                noPatches += (outerSegmentCount * 2);
            }

            return noPatches;
        }
        /// <summary>
        /// Subdivide the fracture into planar patches and return a list of the XYZ coordinates of the cornerpoints of each patch
        /// </summary>
        /// <param name="CreateTriangularPatches">Flag to create only triangular patches: if true, all patches will be triangular; if false, a single patch will be created representing the innermost segments, but the patches representing outer segments will still be triangular </param>
        /// <returns>A primary list, each item representing a fracture patch, containing a nested array of cornerpoints as PointXYZ objects</returns>
        public List<PointXYZ[]> GetFracturePatchesInXYZ(bool CreateTriangularPatches)
        {
            // Create a new list object for the cornerpoint lists for each patch
            // This will be populated with new objects, not references to the existing UnconfinedFractureXYZ member objects
            List<PointXYZ[]> NewPatchList = new List<PointXYZ[]>();

            // Get a list of the position of the nodes along each ray
            List<PointXYZ>[] nodeArray = GetRayNodes();

            // Move outwards from the centre of the fracture along the rays, one point at a time
            PointXYZ[] centralPatch = new PointXYZ[NoRays];
            int nodeIndex = 1;
            List<PointXYZ> previousRayNodes = nodeArray[NoRays - 1];

            // Create patch(es) corresponding to the innermost ray segments
            // If the CreateTriangularSegments flag is set to true, we must create one triangular patch per ray, unless the ray length is zero
            if (CreateTriangularPatches)
            {
                for (int rayNo = 0; rayNo < NoRays; rayNo++)
                {
                    List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                    // Check if there are sufficient nodes on the rays to create this patch
                    if ((previousRayNodes.Count > nodeIndex) && (currentRayNodes.Count > nodeIndex))
                    {
                        // Create an array of three points to represent the patch
                        PointXYZ[] nextPatch = new PointXYZ[3];
                        // Add a copy of the fracture centrepoint to the array
                        nextPatch[0] = new PointXYZ(NucleationPoint);
                        // Add copies of the first node on this ray and the previous ray to the array
                        nextPatch[1] = new PointXYZ(previousRayNodes[nodeIndex]);
                        nextPatch[2] = new PointXYZ(currentRayNodes[nodeIndex]);

                        // Add the new array to the list of patches
                        NewPatchList.Add(nextPatch);
                    }

                    // Update the previous ray node list
                    previousRayNodes = currentRayNodes;
                }
            }
            // Otherwise just create one innermost patch
            else
            {
                for (int rayNo = 0; rayNo < NoRays; rayNo++)
                {
                    List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                    // Check if there are sufficient nodes on the rays to create this element
                    if ((previousRayNodes.Count > nodeIndex) || (currentRayNodes.Count > nodeIndex))
                    {
                        // Add a copy of the first node on this ray to the array
                        centralPatch[rayNo] = new PointXYZ(currentRayNodes[nodeIndex]);
                    }

                    // Update the previous ray node list
                    previousRayNodes = currentRayNodes;
                }

                // Add the new array to the list of patches
                NewPatchList.Add(centralPatch);
            }

            // Now create patches corresponding to the outer ray segments
            bool moveToNextNodeIndex;
            do
            {
                // Increment the node index and set the next node index flag to false; this will only be set to true if there is at least one new patch created
                nodeIndex++;
                moveToNextNodeIndex = false;

                // Loop through all the arrays
                for (int rayNo = 0; rayNo < NoRays; rayNo++)
                {
                    List<PointXYZ> currentRayNodes = nodeArray[rayNo];

                    // Check if there are sufficient outer nodes to create one or two patches
                    if ((previousRayNodes.Count > nodeIndex) && (currentRayNodes.Count > nodeIndex))
                    {
                        PointXYZ outerNode1 = new PointXYZ(previousRayNodes[nodeIndex]);
                        PointXYZ outerNode2 = new PointXYZ(currentRayNodes[nodeIndex]);
                        PointXYZ innerNode1 = new PointXYZ(previousRayNodes[nodeIndex - 1]);
                        PointXYZ innerNode2 = new PointXYZ(currentRayNodes[nodeIndex - 1]);

                        // With four points we can create two patches
                        PointXYZ[] patch1 = new PointXYZ[3];
                        patch1[0] = innerNode1;
                        patch1[1] = innerNode2;
                        patch1[2] = outerNode1;

                        PointXYZ[] patch2 = new PointXYZ[3];
                        patch2[0] = innerNode2;
                        patch2[1] = outerNode1;
                        patch2[2] = outerNode2;

                        // Add the new arrays to the list of patches
                        NewPatchList.Add(patch1);
                        NewPatchList.Add(patch2);

                        // Set the next node index flag to true
                        moveToNextNodeIndex = true;
                    }
                    else if (previousRayNodes.Count > nodeIndex)
                    {
                        PointXYZ outerNode1 = new PointXYZ(previousRayNodes[nodeIndex]);
                        PointXYZ innerNode1 = new PointXYZ(previousRayNodes[nodeIndex - 1]);
                        PointXYZ innerNode2 = new PointXYZ(currentRayNodes[currentRayNodes.Count - 1]);

                        // With three points we can only create one new patch
                        PointXYZ[] patch1 = new PointXYZ[3];
                        patch1[0] = innerNode1;
                        patch1[1] = innerNode2;
                        patch1[2] = outerNode1;

                        // Add the new array to the list of patches
                        NewPatchList.Add(patch1);
                    }
                    else if (currentRayNodes.Count > nodeIndex)
                    {
                        PointXYZ outerNode2 = new PointXYZ(currentRayNodes[nodeIndex]);
                        PointXYZ innerNode1 = new PointXYZ(previousRayNodes[previousRayNodes.Count - 1]);
                        PointXYZ innerNode2 = new PointXYZ(currentRayNodes[nodeIndex - 1]);

                        // With three points we can only create one new patch
                        PointXYZ[] patch1 = new PointXYZ[3];
                        patch1[0] = innerNode1;
                        patch1[1] = innerNode2;
                        patch1[2] = outerNode2;

                        // Add the new array to the list of patches
                        NewPatchList.Add(patch1);

                        // Set the next node index flag to true
                        moveToNextNodeIndex = true;
                    }

                    // Update the previous ray node list
                    previousRayNodes = currentRayNodes;
                }
            }
            while (moveToNextNodeIndex);

            // Return the new patch list object
            return NewPatchList;
        }
        /*/// <summary>
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
        }*/
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
            UnconfinedFractureID = ++unconfinedFractureCounter;

            // Set the reference to the gridblock in which the fracture nucleated and parent unconfined fracture set
            nucleationGridblock = gbc_in;
            ufs = ufs_in;

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

            // Set the fracture geometry data (minimum, maximum and mean ray lengths, effective radius and total area)
            MinimumRayLength = InitialRadius;
            MaximumRayLength = InitialRadius;
            MeanRayLength = InitialRadius;
            EffectiveRadius = InitialRadius;
            Area = Math.PI * InitialRadius * InitialRadius;
            Centroid = new PointXYZ(NucleationPoint_in);

            // Set the flags for fully active and fully deactivated
            FullyActive = true;
            FullyDeactivated = false;

            // Set the flag for a large fracture
            LargeFracture = false;
        }
        /// <summary>
        /// Copy constructor: copy all data from an existing UnconfinedFractureXYZ object
        /// </summary>
        /// <param name="fracture_in">Reference to an existing UnconfinedFractureXYZ object to copy</param>
        public UnconfinedFractureXYZ(UnconfinedFractureXYZ fracture_in)
        {
            // Assign the new object an ID number and increment the unconfined fracture counter
            UnconfinedFractureID = ++unconfinedFractureCounter;

            // Set the reference to the gridblock in which the fracture nucleated and parent unconfined fracture set
            nucleationGridblock = fracture_in.nucleationGridblock;
            ufs = fracture_in.ufs;

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

            // Set the fracture geometry data (minimum, maximum and mean ray lengths, effective radius and total area)
            MinimumRayLength = fracture_in.MinimumRayLength;
            MaximumRayLength = fracture_in.MaximumRayLength;
            MeanRayLength = fracture_in.MeanRayLength;
            EffectiveRadius = fracture_in.EffectiveRadius;
            Area = fracture_in.Area;
            Centroid = new PointXYZ(fracture_in.Centroid);

            // Set the flags for fully active and fully deactivated
            FullyActive = fracture_in.FullyActive;
            FullyDeactivated = fracture_in.FullyDeactivated;

            // Set the flag for a large fracture
            LargeFracture = fracture_in.LargeFracture;
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
