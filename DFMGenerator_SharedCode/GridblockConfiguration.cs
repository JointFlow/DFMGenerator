// Set this flag to output detailed information on the behaviour of the implicit fracture distribution
// Must be run in DEBUG mode
// Use for debugging only; will significantly increase runtime
//#define LOGIMPPOP
// Set this flag to output detailed information on the behaviour of explicit fractures in the DFN
// Use for debugging only; will significantly increase runtime
//#define LOGDFNPOP
// Set this flag to include clear zone data in the list of stored data for unconfined fracture sets and use this to determine UFS termination criterion
#define CHECKCZV
// Set this flag to log detailed information on the growth of explicit unconfined fracture rays
//#define LOGUFRGROWTH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for principal strain orientations
    /// </summary>
    public enum PrincipalStrainOrientation { HMin, HMax }
    /// <summary>
    /// Enumerator for time units
    /// </summary>
    public enum TimeUnits { second, year, ma }
    /// <summary>
    /// Enumerator for global grid directions (N = Y+, E = X+, S = Y-, W = X-, U = Z+, D = Z-)
    /// </summary>
    public enum GridDirection { N, E, S, W, U, D, None }
    /// <summary>
    /// Enumerator for the gridblock cornerpoints
    /// NB This is not properly implemented in the GridblockConfiguration object but is provided for the DataTransferToDFMGenerator interface
    /// </summary>
    public enum GridblockCornerpoint { NWTop, NETop, SETop, SWTop, NWBottom, NEBottom, SEBottom, SWBottom }
    /// <summary>
    /// Enumerator for return codes for the CalculateFractureData function: 0 if the calculation runs to completion without errors; 1 if the timestep limit is hit
    /// </summary>
    public enum CalculateFractureDataReturnCode { Completed, TimestepLimitExceeded }
    /// <summary>
    /// Enumerator for return codes for the PropagateDFN function: 0 if the calculation runs to completion without errors; 1 if the gridblock geometry is not correctly defined; 2 if there is an error in the driving stress or propagation distance; 3 if fracture limit is hit
    /// </summary>
    public enum PropagateDFNReturnCode { Completed, GridblockGeometryError, DrivingStressError, FractureLimitExceeded }
    /// <summary>
    /// Enumerator for algorithms to calculate fracture permeability
    /// </summary>
    public enum PermeabilityCalculationAlgorithm { Oda1986, OdaCorrected1987, SizeConnectivityCorrected }
    /// <summary>
    /// Enumerator for the data used to define a stress state
    /// </summary>
    public enum StressStateDefinition { Strain, AbsoluteStress, TerzaghiEffectiveStress, BiotEffectiveStress }

    /// <summary>
    /// Class representing an entire gridblock
    /// </summary>
    class GridblockConfiguration
    {
        // References to external objects
        /// <summary>
        /// Reference to parent FractureGrid object - this does not need to be filled as the GridblockConfiguration can also function as a standalone object
        /// </summary>
        private FractureGrid gd;
        /// <summary>
        /// Reference to the parent FractureGrid object's random number generator
        /// </summary>
        public Random RandGen { get { return gd.RandomNumberGenerator; } }

        // Gridblock geometry data
        /// <summary>
        /// Mean layer thickness at the start of deformation (m); can be set independently of cornerpoints
        /// </summary>
        private double InitialThickness;
        /// <summary>
        /// Mean layer thickness during deformation (m), used to calculate fracture population data
        /// </summary>
        public double ThicknessAtDeformation { get; private set; }
        /// <summary>
        /// Mean depth of top surface at the start of deformation (in metres, positive downwards), used to calculate in situ stress state; can be set independently of cornerpoints
        /// </summary>
        private double InitialDepth;
        /// <summary>
        /// Mean depth of top surface during deformation (m), used to calculate fracture population data
        /// </summary>
        public double DepthAtDeformation { get; private set; }
        /// <summary>
        /// Reset the mean layer thickness and mean depth to the initial thickness and depth at the start of deformation
        /// </summary>
        public void ResetThicknessAndDepth()
        {
            ThicknessAtDeformation = InitialThickness;
            DepthAtDeformation = InitialDepth;
        }
        /// <summary>
        /// Set the mean layer thickness and mean depth to the current thickness and depth
        /// </summary>
        public void SetToCurrentThicknessAndDepth()
        {
            ThicknessAtDeformation = CurrentThickness;
            DepthAtDeformation = CurrentDepth;
        }
        /// <summary>
        /// Set the mean layer thickness and mean depth at the start of deformation to the specified values
        /// </summary>
        /// <param name="InitialThickness_in">Mean layer thickness at the start of deformation (m) - this does not need to be the current thickness</param>
        /// <param name="InitialDepth_in">Mean depth of top surface at the start of deformation (m) - this does not need to be the current depth</param>
        public void SetInitialThicknessAndDepth(double InitialThickness_in, double InitialDepth_in)
        {
            InitialThickness = InitialThickness_in;
            InitialDepth = InitialDepth_in;
            ResetThicknessAndDepth();
        }

        // Coordinates of cornerpoints
        /// <summary>
        /// Coordinates of the point at the top southwest corner of the gridblock
        /// </summary>
        public PointXYZ SWtop { get; private set; }
        /// <summary>
        /// Coordinates of the point at the bottom southwest corner of the gridblock
        /// </summary>
        public PointXYZ SWbottom { get; private set; }
        /// <summary>
        /// Coordinates of the point at the top northwest corner of the gridblock
        /// </summary>
        public PointXYZ NWtop { get; private set; }
        /// <summary>
        /// Coordinates of the point at the bottom northwest corner of the gridblock
        /// </summary>
        public PointXYZ NWbottom { get; private set; }
        /// <summary>
        /// Coordinates of the point at the top northeast corner of the gridblock
        /// </summary>
        public PointXYZ NEtop { get; private set; }
        /// <summary>
        /// Coordinates of the point at the bottom northeast corner of the gridblock
        /// </summary>
        public PointXYZ NEbottom { get; private set; }
        /// <summary>
        /// Coordinates of the point at the top southeast corner of the gridblock
        /// </summary>
        public PointXYZ SEtop { get; private set; }
        /// <summary>
        /// Coordinates of the point at the bottom southeast corner of the gridblock
        /// </summary>
        public PointXYZ SEbottom { get; private set; }
        /// <summary>
        /// Return coordinates of the point in the centre of the southwest corner pillar of the gridblock
        /// </summary>
        /// <returns>New PointXYZ object representing centrepoint of the southwest corner pillar of the gridblock</returns>
        public PointXYZ getSWMidPoint()
        {
            return new PointXYZ((SWtop.X + SWbottom.X) / 2, (SWtop.Y + SWbottom.Y) / 2, (SWtop.Z + SWbottom.Z) / 2);
        }
        /// <summary>
        /// Return coordinates of the point in the centre of the northwest corner pillar of the gridblock
        /// </summary>
        /// <returns>New PointXYZ object representing centrepoint of the northwest corner pillar of the gridblock</returns>
        public PointXYZ getNWMidPoint()
        {
            return new PointXYZ((NWtop.X + NWbottom.X) / 2, (NWtop.Y + NWbottom.Y) / 2, (NWtop.Z + NWbottom.Z) / 2);
        }
        /// <summary>
        /// Return coordinates of the point in the centre of the northeast corner pillar of the gridblock
        /// </summary>
        /// <returns>New PointXYZ object representing centrepoint of the northeast corner pillar of the gridblock</returns>
        public PointXYZ getNEMidPoint()
        {
            return new PointXYZ((NEtop.X + NEbottom.X) / 2, (NEtop.Y + NEbottom.Y) / 2, (NEtop.Z + NEbottom.Z) / 2);
        }
        /// <summary>
        /// Return coordinates of the point in the centre of the southeast corner pillar of the gridblock
        /// </summary>
        /// <returns>New PointXYZ object representing centrepoint of the southeast corner pillar of the gridblock</returns>
        public PointXYZ getSEMidPoint()
        {
            return new PointXYZ((SEtop.X + SEbottom.X) / 2, (SEtop.Y + SEbottom.Y) / 2, (SEtop.Z + SEbottom.Z) / 2);
        }
        /// <summary>
        /// Current mean depth of the top of the gridblock - may not be the same as depth at the time of deformation
        /// </summary>
        public double CurrentDepth { get { return (SWtop.Depth + NWtop.Depth + NEtop.Depth + SEtop.Depth) / 4; } }
        /// <summary>
        /// Curren mean thickness of the gridblock - may not be the same as thickness at the time of deformation
        /// </summary>
        public double CurrentThickness { get { return ((SWtop.Z - SWbottom.Z) + (NWtop.Z - NWbottom.Z) + (NEtop.Z - NEbottom.Z) + (SEtop.Z - SEbottom.Z)) / 4; } }
        /// <summary>
        /// Get a list of all cornerpoints as PointXYZ objects
        /// </summary>
        /// <returns>Return a list of cornerpoints in order: SWtop, SWbottom, NWtop, NWbottom, NEtop, NEbottom, SEtop, SEbottom</returns>
        public List<PointXYZ> GetCornerPointList()
        {
            List<PointXYZ> output = new List<PointXYZ>();
            output.Add(SWtop);
            output.Add(SWbottom);
            output.Add(NWtop);
            output.Add(NWbottom);
            output.Add(NEtop);
            output.Add(NEbottom);
            output.Add(SEtop);
            output.Add(SEbottom);

            return output;
        }
        /// <summary>
        /// Check if the cornerpoints are defined
        /// </summary>
        /// <returns>true if all cornerpoints are defined; false if any cornerpoint references are null</returns>
        public bool checkCornerpointsDefined()
        {
            if ((SWtop != null) && (SWbottom != null) && (NWtop != null) && (NWbottom != null) && (NEtop != null) && (NEbottom != null) && (SEtop != null) && (SEbottom != null))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Get the cornerpoints of a specified boundary as PointXYZ objects
        /// </summary>
        /// <param name="boundary">Boundary for which cornerpoint are required</param>
        /// <param name="UpperLeftCorner">Reference parameter for PointXYZ object representing the upper left cornerpoint of the specified boundary</param>
        /// <param name="UpperRightCorner">Reference parameter for PointXYZ object representing the upper right cornerpoint of the specified boundary</param>
        /// <param name="LowerLeftCorner">Reference parameter for PointXYZ object representing the lower left cornerpoint of the specified boundary</param>
        /// <param name="LowerRightCorner">Reference parameter for PointXYZ object representing the lower right cornerpoint of the specified boundary</param>
        public void getBoundaryCornerpoints(GridDirection boundary, out PointXYZ UpperLeftCorner, out PointXYZ UpperRightCorner, out PointXYZ LowerLeftCorner, out PointXYZ LowerRightCorner)
        {
            switch (boundary)
            {
                case GridDirection.N:
                    {
                        UpperLeftCorner = new PointXYZ(NWtop);
                        UpperRightCorner = new PointXYZ(NEtop);
                        LowerLeftCorner = new PointXYZ(NWbottom);
                        LowerRightCorner = new PointXYZ(NEbottom);
                        return;
                    }
                case GridDirection.E:
                    {
                        UpperLeftCorner = new PointXYZ(NEtop);
                        UpperRightCorner = new PointXYZ(SEtop);
                        LowerLeftCorner = new PointXYZ(NEbottom);
                        LowerRightCorner = new PointXYZ(SEbottom);
                        return;
                    }
                case GridDirection.S:
                    {
                        UpperLeftCorner = new PointXYZ(SEtop);
                        UpperRightCorner = new PointXYZ(SWtop);
                        LowerLeftCorner = new PointXYZ(SEbottom);
                        LowerRightCorner = new PointXYZ(SWbottom);
                        return;
                    }
                case GridDirection.W:
                    {
                        UpperLeftCorner = new PointXYZ(SWtop);
                        UpperRightCorner = new PointXYZ(NWtop);
                        LowerLeftCorner = new PointXYZ(SWbottom);
                        LowerRightCorner = new PointXYZ(NWbottom);
                        return;
                    }
                default:
                    {
                        UpperLeftCorner = null;
                        UpperRightCorner = null;
                        LowerLeftCorner = null;
                        LowerRightCorner = null;
                        return;
                    }
            }
        }
        /// <summary>
        /// Dictionary containing the gridblock faces as PlaneXYZ objects
        /// </summary>
        public Dictionary<GridDirection, PlaneXYZ> GridblockFaces { get; private set; }
        /// <summary>
        /// Return a list object containing all the gridblock faces as PlaneXYZ objects
        /// </summary>
        /// <returns></returns>
        public List<PlaneXYZ> getGridblockFaces()
        {
            return new List<PlaneXYZ>() { GridblockFaces[GridDirection.N], GridblockFaces[GridDirection.E], GridblockFaces[GridDirection.S], GridblockFaces[GridDirection.W], GridblockFaces[GridDirection.U], GridblockFaces[GridDirection.D]};
        }

        // Private geometric properties - for internal use only
        // These are invariants used by the public geometric functions; to save time they can be calculated when the gridblock corners are defined and stored for later use
        // NB these functions all assume vertical pillars (i.e. cell cornerpoints are vertically aligned)
        /// <summary>
        /// Minimum X coordinate of all cornerpoints
        /// </summary>
        private double MinX { get; set; }
        /// <summary>
        /// Maximum X coordinate of all cornerpoints
        /// </summary>
        private double MaxX { get; set; }
        /// <summary>
        /// Minimum Y coordinate of all cornerpoints
        /// </summary>
        private double MinY { get; set; }
        /// <summary>
        /// Maximum Y coordinate of all cornerpoints
        /// </summary>
        private double MaxY { get; set; }
        /// <summary>
        /// Minimum Z coordinate of all cornerpoints
        /// </summary>
        private double MinZ { get; set; }
        /// <summary>
        /// Maximum Z coordinate of all cornerpoints
        /// </summary>
        private double MaxZ { get; set; }
        /// <summary>
        /// Defined as SE corner X - SW corner X
        /// </summary>
        private double X2 { get; set; }
        /// <summary>
        /// Defined as NW corner X - SW corner X
        /// </summary>
        private double X3 { get; set; }
        /// <summary>
        /// Defined as SW corner X - SE corner X - NW corner X + NE corner X
        /// </summary>
        private double X4 { get; set; }
        /// <summary>
        /// Defined as SE corner Y - SW corner Y
        /// </summary>
        private double Y2 { get; set; }
        /// <summary>
        /// Defined as NW corner Y - SW corner Y
        /// </summary>
        private double Y3 { get; set; }
        /// <summary>
        /// Defined as SW corner Y - SE corner Y - NW corner Y + NE corner Y
        /// </summary>
        private double Y4 { get; set; }
        /// <summary>
        /// Defined as (X2*Y4) - (X4*Y2)
        /// </summary>
        private double Au { get; set; }
        /// <summary>
        /// Defined as (X3*Y4) - (X4*Y3)
        /// </summary>
        private double Av { get; set; }
        /// <summary>
        /// Recalculate the area, side lengths and corner angles of the middle surface of the gridblock, projected onto the horizontal; this should be called whenever the cornerpoints are changed
        /// </summary>
        private void recalculateGeometry()
        {
            // Check to see if all cornerpoints are defined
            if (checkCornerpointsDefined())
            {
                // Get centrepoints of corner pillars
                PointXYZ SWCorner = getSWMidPoint();
                PointXYZ NWCorner = getNWMidPoint();
                PointXYZ NECorner = getNEMidPoint();
                PointXYZ SECorner = getSEMidPoint();

                // Calculate X2, X3, X4, Y2, Y3, Y4, Am and An; these are used when calculating the position of a point within the gridblock (i.e. m and n)
                // X2 Defined as SE corner X - SW corner X
                X2 = SECorner.X - SWCorner.X;
                // X3 defined as NW corner X - SW corner X
                X3 = NWCorner.X - SWCorner.X;
                // X4 defined as SW corner X - SE corner X - NW corner X + NE corner X
                X4 = SWCorner.X - SECorner.X - NWCorner.X + NECorner.X;
                // Y2 defined as SE corner Y - SW corner Y
                Y2 = SECorner.Y - SWCorner.Y;
                // Y3 defined as NW corner Y - SW corner Y
                Y3 = NWCorner.Y - SWCorner.Y;
                // Y4 defined as SW corner Y - SE corner Y - NW corner Y + NE corner Y
                Y4 = SWCorner.Y - SECorner.Y - NWCorner.Y + NECorner.Y;
                // Am defined as (X2*Y4) - (X4*Y2)
                Au = (X2 * Y4) - (X4 * Y2);
                // An defined as (X3*Y4) - (X4*Y3)
                Av = (X3 * Y4) - (X4 * Y3);

                // Calculate X5, X6, Y5, Y6; these are just used locally to calculate lengths of N and E gridblock sides
                // X5 Defined as NE corner X - NW corner X
                double X5 = NECorner.X - NWCorner.X;
                // X6 defined as NE corner X - SE corner X
                double X6 = NECorner.X - SECorner.X;
                // Y5 Defined as NE corner Y - NW corner Y
                double Y5 = NECorner.Y - NWCorner.Y;
                // Y6 defined as NE corner Y - SE corner Y
                double Y6 = NECorner.Y - SECorner.Y;

                // Calculate the squares of the lengths of the gridblock sides, projected onto horizontal plane
                double Length_WSide_squared = Math.Pow(X3, 2) + Math.Pow(Y3, 2);
                double Length_NSide_squared = Math.Pow(X5, 2) + Math.Pow(Y5, 2);
                double Length_ESide_squared = Math.Pow(X6, 2) + Math.Pow(Y6, 2);
                double Length_SSide_squared = Math.Pow(X2, 2) + Math.Pow(Y2, 2);

                // Calculate lengths of the gridblock sides
                Length_WSide = Math.Sqrt(Length_WSide_squared);
                Length_NSide = Math.Sqrt(Length_NSide_squared);
                Length_ESide = Math.Sqrt(Length_ESide_squared);
                Length_SSide = Math.Sqrt(Length_SSide_squared);
                Length_SEtoNW_diagonal = Math.Sqrt(Math.Pow(NWCorner.X - SECorner.X, 2) + Math.Pow(NWCorner.Y - SECorner.Y, 2));
                Length_NEtoSW_diagonal = Math.Sqrt(Math.Pow(NECorner.X - SWCorner.X, 2) + Math.Pow(NECorner.Y - SWCorner.Y, 2));

                // Calculate internal angles of the SW and NE corners, projected onto horizontal plane
                // Use cosine law and dot product of the two adjacent sides
                double SWcorner_dotProduct = (X2 * X3) + (Y2 * Y3);
                double SWcorner_cosine = SWcorner_dotProduct / (Length_SSide * Length_WSide);
                Angle_SWcorner = Math.Acos(SWcorner_cosine);
                double NEcorner_dotProduct = (X6 * X5) + (Y6 * Y5);
                double NEcorner_cosine = NEcorner_dotProduct / (Length_ESide * Length_NSide);
                Angle_NEcorner = Math.Acos(NEcorner_cosine);

                // Calculate area
                double AreaSWTriangle = (Length_SSide * Length_WSide * VectorXYZ.Sin_trim(Angle_SWcorner)) / 2;
                double AreaNETriangle = (Length_ESide * Length_NSide * VectorXYZ.Sin_trim(Angle_NEcorner)) / 2;
                Area = AreaSWTriangle + AreaNETriangle;

                // Reset maximum and minimum X, Y and Z coordinates
                MinX = SWtop.X;
                MaxX = SWtop.X;
                MinY = SWtop.Y;
                MaxY = SWtop.Y;
                MinZ = SWtop.Z;
                MaxZ = SWtop.Z;
                List<PointXYZ> CornerPointList = GetCornerPointList();
                foreach (PointXYZ cornerpoint in CornerPointList)
                {
                    if (MinX > cornerpoint.X) MinX = cornerpoint.X;
                    if (MaxX < cornerpoint.X) MaxX = cornerpoint.X;
                    if (MinY > cornerpoint.Y) MinY = cornerpoint.Y;
                    if (MaxY < cornerpoint.Y) MaxY = cornerpoint.Y;
                    if (MinZ > cornerpoint.Z) MinZ = cornerpoint.Z;
                    if (MaxZ < cornerpoint.Z) MaxZ = cornerpoint.Z;
                }

                // Reset the flag to determine whether to search neighbouring gridblocks for stress shadow interaction
                searchNeighbouringGridblocks_Automatic = ThicknessAtDeformation > (0.5 * Math.Sqrt(Area));

                // Populate the dictionary of gridblock faces
                GridblockFaces[GridDirection.N] = new PlaneXYZ(NEtop, NEbottom, NWbottom, NWtop);
                GridblockFaces[GridDirection.E] = new PlaneXYZ(SEtop, SEbottom, NEbottom, NEtop);
                GridblockFaces[GridDirection.S] = new PlaneXYZ(SWtop, SWbottom, SEbottom, SEtop);
                GridblockFaces[GridDirection.W] = new PlaneXYZ(NWtop, NWbottom, SWbottom, SWtop);
                GridblockFaces[GridDirection.U] = new PlaneXYZ(NWtop, SWtop, SEtop, NEtop);
                GridblockFaces[GridDirection.D] = new PlaneXYZ(NEbottom, SEbottom, SWbottom, NWbottom);
                GridblockFaces[GridDirection.None] = null;
            }
        }

        // Public geometric properties and functions - for internal or external use
        // NB these functions all assume vertical pillars (i.e. cell cornerpoints are vertically aligned)
        /// <summary>
        /// Area of the middle surface of the gridblock, projected onto the horizontal; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Area { get; private set; }
        /// <summary>
        /// Volume of the gridblock at the time of deformation
        /// </summary>
        public double Volume { get { return Area * ThicknessAtDeformation; } }
        /// <summary>
        /// Point representing the local gridblock origin, with the minimum X, Y and Z values of all corners of the gridblock
        /// </summary>
        public PointXYZ Gridblock_Origin { get { return new PointXYZ(MinX, MinY, MinZ); } }
        /// <summary>
        /// Point representing the local gridblock maximum, with the maximum X, Y and Z values of all corners of the gridblock
        /// </summary>
        public PointXYZ Gridblock_Maximum { get { return new PointXYZ(MaxX, MaxY, MaxZ); } }
        /// <summary>
        /// Length of the west side of the middle surface of the gridblock, projected onto the horizontal; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Length_WSide { get; private set; }
        /// <summary>
        /// Length of the north side of the middle surface of the gridblock, projected onto horizontal plane; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Length_NSide { get; private set; }
        /// <summary>
        /// Length of the east side of the middle surface of the gridblock, projected onto the horizontal, projected onto horizontal plane; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Length_ESide { get; private set; }
        /// <summary>
        /// Length of the south side of the middle surface of the gridblock, projected onto the horizontal, projected onto horizontal plane; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Length_SSide { get; private set; }
        /// <summary>
        /// Length of the SE to NW diagonal of the middle surface of the gridblock, projected onto the horizontal, projected onto horizontal plane; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Length_SEtoNW_diagonal { get; private set; }
        /// <summary>
        /// Length of the NE to EW diagonal of the middle surface of the gridblock, projected onto the horizontal, projected onto horizontal plane; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Length_NEtoSW_diagonal { get; private set; }
        /// <summary>
        /// Maximum internal length within the middle surface of the gridblock - given by the maximum length of the sides and diagonals
        /// </summary>
        public double MaxGridblockLength
        {
            get
            {
                double output = Length_WSide;
                if (output < Length_NSide)
                    output = Length_NSide;
                if (output < Length_ESide)
                    output = Length_ESide;
                if (output < Length_SSide)
                    output = Length_SSide;
                if (output < Length_SEtoNW_diagonal)
                    output = Length_SEtoNW_diagonal;
                if (output < Length_NEtoSW_diagonal)
                    output = Length_NEtoSW_diagonal;
                return output;
            }
        }
        /// <summary>
        /// Internal angle of the SW corner of the middle surface of the gridblock, projected onto the horizontal (radians); recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Angle_SWcorner { get; private set; }
        /// <summary>
        /// Internal angle of the NE corner of the middle surface of the gridblock, projected onto the horizontal (radians); recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Angle_NEcorner { get; private set; }
        /// <summary>
        /// Get the position of the intersection between a line parallel to the X or Y axis, passing through a specified point, and a specified boundary segment projected onto the middle surface of the gridblock
        /// Note that the intersecting line is considered infinite in either direction but it must intersect the boundary segment between the gridblock cornerpoints, otherwise the function will return NaN
        /// </summary>
        /// <param name="point">Position of a point through which the intersecting line passes</param>
        /// <param name="intersectionLine">Direction of the intersecting line: specify N or S for a line parallel to the Y axis and E or W for a line parallel to the X axis </param>
        /// <param name="boundary">Boundary with which to calculate the intersection</param>
        /// <param name="crossesOutward">Output flag to determine whether the line crosses out from the gridblock (true) or into the gridblock (false)</param>
        /// <returns>The Y coordinate of the intersection point if the intersecting line is N-S; the X coordinate of the intersetion point if the intersecting line is E-W, NaN if the line does not intersect the specified boundary segment</returns>
        private double getBoundaryIntersection(PointXYZ point, GridDirection intersectionLine, GridDirection boundary, out bool crossesOutward)
        {
            // By default set crossesOutward flag to false
            crossesOutward = false;

            // Get left and right cornerpoints for the selected boundary
            PointXYZ leftCorner = null;
            PointXYZ rightCorner = null;
            switch (boundary)
            {

                case GridDirection.N:
                    leftCorner = getNWMidPoint();
                    rightCorner = getNEMidPoint();
                    break;
                case GridDirection.E:
                    leftCorner = getNEMidPoint();
                    rightCorner = getSEMidPoint();
                    break;
                case GridDirection.S:
                    leftCorner = getSEMidPoint();
                    rightCorner = getSWMidPoint();
                    break;
                case GridDirection.W:
                    leftCorner = getSWMidPoint();
                    rightCorner = getNWMidPoint();
                    break;
                // If no boundary is specified return NaN
                case GridDirection.None:
                default:
                    return double.NaN;
            }

            // Get the coordinates for the line and boundary cornerpoints relative to the direction of the line (i)
            double intersection_j, leftCorner_i, leftCorner_j, rightCorner_i, rightCorner_j;
            switch (intersectionLine)
            {
                // Line is parallel to the positive y axis: i=+Y, j=+X
                case GridDirection.N:
                    intersection_j = point.X;
                    leftCorner_i = leftCorner.Y;
                    leftCorner_j = leftCorner.X;
                    rightCorner_i = rightCorner.Y;
                    rightCorner_j = rightCorner.X;
                    break;
                // Line is parallel to the positive x axis: i=+X, j=-Y
                case GridDirection.E:
                    intersection_j = -point.Y;
                    leftCorner_i = leftCorner.X;
                    leftCorner_j = -leftCorner.Y;
                    rightCorner_i = rightCorner.X;
                    rightCorner_j = -rightCorner.Y;
                    break;
                // Line is parallel to the negative y axis: i=-Y, j=-X
                case GridDirection.S:
                    intersection_j = -point.X;
                    leftCorner_i = -leftCorner.Y;
                    leftCorner_j = -leftCorner.X;
                    rightCorner_i = -rightCorner.Y;
                    rightCorner_j = -rightCorner.X;
                    break;
                // Line is parallel to the negative x axis: i=-X, j=+Y
                case GridDirection.W:
                    intersection_j = point.Y;
                    leftCorner_i = -leftCorner.X;
                    leftCorner_j = leftCorner.Y;
                    rightCorner_i = -rightCorner.X;
                    rightCorner_j = rightCorner.Y;
                    break;
                // If no line direction is specified return NaN
                case GridDirection.None:
                default:
                    return double.NaN;
            }

            // Determine whether the line intersects the boundary between the cornerpoints; if so, determine which direction it is crossing, if not return NaN
            // Also return NaN if the boundary is parallel to the line (i.e. leftCorner_j == rightCorner_j)
            if (leftCorner_j == rightCorner_j) // If leftCorner_j == rightCorner_j the line is parallel to the line
                return double.NaN;
            else if ((leftCorner_j <= intersection_j) && (intersection_j <= rightCorner_j)) // If leftCorner_j < rightCorner_j the line is crossing outwards
                crossesOutward = true;
            else if ((leftCorner_j >= intersection_j) && (intersection_j >= rightCorner_j)) // If leftCorner_j > rightCorner_j the line is crossing inwards
                crossesOutward = false;
            else // If intersection_j does not lie between leftCorner_j and rightCorner_j the line does not intersect the boundary segment between the cornerpoints 
                return double.NaN;

            // Calculate the position of the intersection and return it
            // This is valid whether leftCorner_j < rightCorner_j or leftCorner_j > rightCorner_j
            double relativeIntersectionPoint = (intersection_j - leftCorner_j) / (rightCorner_j - leftCorner_j);
            double intersection_i = (leftCorner_i * (1 - relativeIntersectionPoint)) + (rightCorner_i * relativeIntersectionPoint);
            // If the line direction is S or W, the i coordinates must be inverted to convert to X or Y coordinates respectively
            if ((intersectionLine == GridDirection.S) || (intersectionLine == GridDirection.W))
                return -intersection_i;
            else
                return intersection_i;
        }
        /// <summary>
        /// Function to return the position of a location specified relative to the horizontal projection of the gridblock boundaries (uv coordinates) in grid (XY) coordinates
        /// </summary>
        /// <param name="u_in">u coordinate (relative position along the W-E direction; u=0 for a point on the W gridblock boundary and u=1 for a point on the E gridblock boundary)</param>
        /// <param name="v_in">v coordinate (relative position along the S-N direction; v=0 for a point on the S gridblock boundary and v=1 for a point on the N gridblock boundary)</param>
        /// <param name="X_out">Reference parameter for the X (grid) coordinate of the location</param>
        /// <param name="Y_out">Reference parameter for the Y (grid) coordinate of the location</param>
        public void getAbsolutePosition(double u_in, double v_in, out double X_out, out double Y_out)
        {
            // Get centrepoints of corner pillars
            PointXYZ SWCorner = getSWMidPoint();
            PointXYZ NWCorner = getNWMidPoint();
            PointXYZ NECorner = getNEMidPoint();
            PointXYZ SECorner = getSEMidPoint();

            // Get inverse of u and v
            double u_inv = 1 - u_in;
            double v_inv = 1 - v_in;

            // Calculate X and Y
            X_out = (u_inv * v_inv * SWCorner.X) + (u_in * v_inv * SECorner.X) + (u_inv * v_in * NWCorner.X) + (u_in * v_in * NECorner.X);
            Y_out = (u_inv * v_inv * SWCorner.Y) + (u_in * v_inv * SECorner.Y) + (u_inv * v_in * NWCorner.Y) + (u_in * v_in * NECorner.Y);
        }
        /// <summary>
        /// Function to return the position of a point specified relative to the horizontal projection of the gridblock boundaries (uvw coordinates) in grid (XYZ) coordinates
        /// </summary>
        /// <param name="u_in">u coordinate (relative position along the W-E direction; u=0 for a point on the W gridblock boundary and u=1 for a point on the E gridblock boundary)</param>
        /// <param name="v_in">v coordinate (relative position along the S-N direction; v=0 for a point on the S gridblock boundary and v=1 for a point on the N gridblock boundary)</param>
        /// <param name="w_in">w coordinate (relative position in the vertical direction; w=0 for a point on the bottom gridblock boundary and w=1 for a point on the top gridblock boundary)</param>
        /// <returns>Point XYZ object with the calculated coordinates</returns>
        public PointXYZ getAbsolutePosition(double u_in, double v_in, double w_in)
        {
            // Get centrepoints of corner pillars
            PointXYZ SWCorner = getSWMidPoint();
            PointXYZ NWCorner = getNWMidPoint();
            PointXYZ NECorner = getNEMidPoint();
            PointXYZ SECorner = getSEMidPoint();

            // Get vertical thickness at the corner pillars
            double SW_TVT = (SWtop.Z - SWbottom.Z);
            double SE_TVT = (SEtop.Z - SEbottom.Z);
            double NW_TVT = (NWtop.Z - NWbottom.Z);
            double NE_TVT = (NEtop.Z - NEbottom.Z);

            // Get inverse of u and v
            double u_inv = 1 - u_in;
            double v_inv = 1 - v_in;

            // Reset p (relative depth) to range from 0.5 (top of cell) to -0.5 (bottom of cell)
            w_in -= 0.5;

            // Calculate X and Y
            double X_out = (u_inv * v_inv * SWCorner.X) + (u_in * v_inv * SECorner.X) + (u_inv * v_in * NWCorner.X) + (u_in * v_in * NECorner.X);
            double Y_out = (u_inv * v_inv * SWCorner.Y) + (u_in * v_inv * SECorner.Y) + (u_inv * v_in * NWCorner.Y) + (u_in * v_in * NECorner.Y);

            // Calculate Z
            double Z_out = (u_inv * v_inv * SWCorner.Z) + (u_in * v_inv * SECorner.Z) + (u_inv * v_in * NWCorner.Z) + (u_in * v_in * NECorner.Z);
            double TVT_out = (u_inv * v_inv * SW_TVT) + (u_in * v_inv * SE_TVT) + (u_inv * v_in * NW_TVT) + (u_in * v_in * NE_TVT);
            Z_out += (TVT_out * w_in);

            // Return a PointXYZ object with the calculated coordinates
            return new PointXYZ(X_out, Y_out, Z_out);
        }
        /// <summary>
        /// Function to return the position of a location specified in grid (XY) coordinates, relative to the horizontal projection of the gridblock boundaries (uv coordinates) 
        /// </summary>
        /// <param name="u_out">Reference parameter for the u coordinate (relative position along the W-E direction; u=0 for a point on the W gridblock boundary and u=1 for a point on the E gridblock boundary)</param>
        /// <param name="v_out">Reference parameter for the v coordinate (relative position along the S-N direction; v=0 for a point on the S gridblock boundary and v=1 for a point on the N gridblock boundary)</param>
        /// <param name="X_in">X (grid) coordinate of the location</param>
        /// <param name="Y_in">Y (grid) coordinate of the location</param>
        /// <returns>Returns true if the calculation is successful; returns false if the cornerpoints are undefined, or if either m or n are undefined for the specified X and Y; note that this does not specify if the point lies within the gridblock or not</returns>
        public bool getPositionRelativeToGridblock(out double u_out, out double v_out, double X_in, double Y_in)
        {
            // Calculation will be unsuccessful if the cornerpoints are undefined, or if either m or n are undefined for the specified x and y (this can only occur if the point lies outside the gridblock)
            // If the calculation is unsuccessful, we will return false and set m and n to NaN
            u_out = double.NaN;
            v_out = double.NaN;

            // If all cornerpoints are not defined we cannot continue the calculation, so return false
            if (!checkCornerpointsDefined())
                return false;

            // Get centrepoints of corner pillars
            PointXYZ SWCorner = getSWMidPoint();
            PointXYZ NWCorner = getNWMidPoint();
            PointXYZ NECorner = getNEMidPoint();
            PointXYZ SECorner = getSEMidPoint();

            // Calculate X1, Y1, Bu, Cu, Bv anbd Cv
            // X1 Defined as input point X - SW corner X
            double X1 = X_in - SWCorner.X;
            // Y1 Defined as input point Y - SW corner Y
            double Y1 = Y_in - SWCorner.Y;
            // Bu defined as (X4*Y1) - (X1*Y4) - (X3*Y2) + (X2*Y3)
            double Bu = (X4 * Y1) - (X1 * Y4) - (X3 * Y2) + (X2 * Y3);
            // Cu defined as (X3*Y1) - (X1*Y3)
            double Cu = (X3 * Y1) - (X1 * Y3);
            // Bv defined as (X4*Y1) - (X1*Y4) - (X2*Y3) + (X3*Y2)
            double Bv = (X4 * Y1) - (X1 * Y4) - (X2 * Y3) + (X3 * Y2);
            // Cv defined as (X2*Y1) - (X1*Y2)
            double Cv = (X2 * Y1) - (X1 * Y2);

            // Calculate the root term for the quadratic formula for u
            // If the quadratic formula returns no real roots, then u is undefined for the specified X and Y, so return false
            double u_rootterm = Math.Pow(Bu, 2) - (4 * Au * Cu);
            if (u_rootterm < 0)
                return false;

            double u_root1, u_root2;
            double v_root1, v_root2;

            // We will start by calculating the most likely value of u and its associated v value
            // If these lie within bounds (between 0 and 1) then we can save time by returning these values directly, without calculating the second root
            // Calculate v using the quadratic formula
            if (Au == 0) // If Au = 0 then we can calculate u from a linear formula
                u_root1 = -Cu / Bu;
            else // We must calculate v from quadratic formula
                u_root1 = (Bu < 0 ? (-Bu - Math.Sqrt(u_rootterm)) / (2 * Au) : (-Bu + Math.Sqrt(u_rootterm)) / (2 * Au));
            // Calculate the associated value of v
            double v_root1_xdenom = X3 + (X4 * u_root1);
            double v_root1_ydenom = Y3 + (Y4 * u_root1);
            if (Math.Abs(v_root1_xdenom) > Math.Abs(v_root1_ydenom))
                v_root1 = (X1 - (X2 * u_root1)) / v_root1_xdenom;
            else
                v_root1 = (Y1 - (Y2 * u_root1)) / v_root1_ydenom;
            // Calculate the total variance of the u and v values for the first root from the range 0-1 (representing internal points)
            double u_root1_variance = 0;
            if (u_root1 < 0)
                u_root1_variance += Math.Pow(0 - u_root1, 2);
            if (u_root1 > 1)
                u_root1_variance += Math.Pow(u_root1 - 1, 2);
            if (v_root1 < 0)
                u_root1_variance += Math.Pow(0 - v_root1, 2);
            if (v_root1 > 1)
                u_root1_variance += Math.Pow(v_root1 - 1, 2);
            // Set the output values to the first root values
            u_out = u_root1;
            v_out = v_root1;

            // If variance for the first root is zero (i.e. the point lies within the gridblock) then we can use these values directly
            if (u_root1_variance == 0)
                return true;

            // Otherwise we will calculate the second root and use the one with the lowest variance to calculate the output
            // Calculate u using the quadratic formula
            if (Au == 0) // If Au = 0 then we can calculate u from a linear formula
                u_root2 = -Cu / Bu;
            else // We must calculate u from quadratic formula
                u_root2 = (Bu < 0 ? (-Bu + Math.Sqrt(u_rootterm)) / (2 * Au) : (-Bu - Math.Sqrt(u_rootterm)) / (2 * Au));
            // Calculate the associated value of v
            double v_root2_xdenom = X3 + (X4 * u_root2);
            double v_root2_ydenom = Y3 + (Y4 * u_root2);
            if (Math.Abs(v_root2_xdenom) > Math.Abs(v_root2_ydenom))
                v_root2 = (X1 - (X2 * u_root2)) / v_root2_xdenom;
            else
                v_root2 = (Y1 - (Y2 * u_root2)) / v_root2_ydenom;
            // Calculate the total variance of the u and v values for the second root from the range 0-1 (representing internal points)
            double u_root2_variance = 0;
            if (u_root2 < 0)
                u_root2_variance += Math.Pow(0 - u_root2, 2);
            if (u_root2 > 1)
                u_root2_variance += Math.Pow(u_root2 - 1, 2);
            if (v_root2 < 0)
                u_root2_variance += Math.Pow(0 - v_root2, 2);
            if (v_root2 > 1)
                u_root2_variance += Math.Pow(v_root2 - 1, 2);
            // If the variance of the second root is less than the variance of the first root, set the output values to the second root values instead
            if (u_root2_variance < u_root1_variance)
            {
                u_out = u_root2;
                v_out = v_root2;
            }

            // Return the flag to specify if the calculation is successful. The u and v values will be returned via reference parameters
            // Note that this flag does not specify if the point lies within the gridblock or not - the calculation may still be successful for a point outside the gridblock
            return true;
        }
        /// <summary>
        /// Function to return the position of a point specified in grid (XYZ) coordinates, relative to the horizontal projection of the gridblock boundaries (uvw coordinates)
        /// </summary>
        /// <param name="u_out">Reference parameter for the u coordinate (relative position along the W-E direction; u=0 for a point on the W gridblock boundary and u=1 for a point on the E gridblock boundary)</param>
        /// <param name="v_out">Reference parameter for the v coordinate (relative position along the S-N direction; v=0 for a point on the S gridblock boundary and v=1 for a point on the N gridblock boundary)</param>
        /// <param name="w_out">Reference parameter for the w coordinate (relative position in the vertical direction; w=0 for a point on the top gridblock boundary and w=1 for a point on the bottom gridblock boundary)</param>
        /// <param name="point_in">Point XYZ object with the specified coordinates</param>
        /// <returns>Returns true if the calculation is successful; returns false if the cornerpoints are undefined, or if either u or v are undefined for the specified X and Y; note that this does not specify if the point lies within the gridblock or not</returns>
        public bool getPositionRelativeToGridblock(out double u_out, out double v_out, out double w_out, PointXYZ point_in)
        {
            // Get the coordinates of the input point
            double X_in = point_in.X;
            double Y_in = point_in.Y;
            double Z_in = point_in.Z;

            // Calculate the location of the point in mn coordinates using the other getPositionRelativeToGridblock function
            // This will return false if the cornerpoints are undefined, or if either u or v are undefined for the specified x and y (this can only occur if the point lies outside the gridblock)
            bool calculationSuccessful = getPositionRelativeToGridblock(out u_out, out v_out, point_in.X, point_in.Y);

            // If this is successful then we can calculate the relative position in the vertical direction w
            if (calculationSuccessful)
            {
                // Get depths of centrepoints of corner pillars
                double SW_Z = getSWMidPoint().Z;
                double SE_Z = getSEMidPoint().Z;
                double NW_Z = getNWMidPoint().Z;
                double NE_Z = getNEMidPoint().Z;

                // Get vertical thickness at the corner pillars
                double SW_TVT = (SWtop.Z - SWbottom.Z);
                double SE_TVT = (SEtop.Z - SEbottom.Z);
                double NW_TVT = (NWtop.Z - NWbottom.Z);
                double NE_TVT = (NEtop.Z - NEbottom.Z);

                // Get inverse of u and v
                double u_inv = 1 - u_out;
                double v_inv = 1 - v_out;

                // Calculate depth of gridblock centre and gridblock thickness at the specified point
                double Z_centre = (u_inv * v_inv * SW_Z) + (u_out * v_inv * SE_Z) + (u_inv * v_out * NW_Z) + (u_out * v_out * NE_Z);
                double TVT = (u_inv * v_inv * SW_TVT) + (u_out * v_inv * SE_TVT) + (u_inv * v_out * NW_TVT) + (u_out * v_out * NE_TVT);

                // Calculate w (relative depth)
                w_out = (Z_in - Z_centre) / TVT;

                // Reset w to range from 0 (bottom of cell) to 1 (top of cell)
                w_out += 0.5;
            }
            else // Otherwise set the relative position in the vertical direction w to NaN
            {
                w_out = double.NaN;
            }

            // Return the flag specifying if the u and v coordinates are defined
            return calculationSuccessful;
        }
        /// <summary>
        /// Get the Z coordinate (positive upwards) of the centre of the gridblock at a specified location in grid (XY) coordinates; returns NaN if gridblock cornerpoints are not defined 
        /// </summary>
        /// <param name="X_in">X (grid) coordinate of the location</param>
        /// <param name="Y_in">Y (grid) coordinate of the location</param>
        /// <returns>Z coordinate (positive upwards) of the centre of the gridblock</returns>
        public double getCentreZ(double X_in, double Y_in)
        {
            // Define variables for output and for position relative to the relative to the gridblock boundaries (uv coordinates)
            double Z_out;
            double u, v;

            // Get the position relative to the relative to the gridblock boundaries (uv coordinates)
            // If this is undefined, the getPositionRelativeToGridblock function will return false. In this case we cannot calculate the depth and must return the default value.
            if (getPositionRelativeToGridblock(out u, out v, X_in, Y_in))
            {
                // Get depths of centrepoints of corner pillars
                double SW_Z = getSWMidPoint().Z;
                double SE_Z = getSEMidPoint().Z;
                double NW_Z = getNWMidPoint().Z;
                double NE_Z = getNEMidPoint().Z;

                // Get inverse of u and v
                double u_inv = 1 - u;
                double v_inv = 1 - v;

                // Calculate Z at X,Y
                Z_out = (u_inv * v_inv * SW_Z) + (u * v_inv * SE_Z) + (u_inv * v * NW_Z) + (u * v * NE_Z);
            }
            // Default return value is -(CurrentDepth + (CurrentThickness/2)), representing the mean centre depth for the gridblock
            // This will be returned if the the depth at the specified point cannot be calculated (which may occur with inverted geometries)
            else
            {
                Z_out = -(CurrentDepth + (CurrentThickness / 2));
            }

            // Return Z
            return Z_out;
        }
        /// <summary>
        /// Get the depth (positive downwards) of the centre of the gridblock at a specified location in grid (XY) coordinates; returns NaN if gridblock cornerpoints are not defined 
        /// </summary>
        /// <param name="X_in">X (grid) coordinate of the location</param>
        /// <param name="Y_in">Y (grid) coordinate of the location</param>
        /// <returns>Depth (positive downwards) of the centre of the gridblock</returns>
        public double getDepth(double X_in, double Y_in)
        {
            return -getCentreZ(X_in, Y_in);
        }
        /// <summary>
        /// Get the true vertical thickness of the gridblock at a specified location in grid (XY) coordinates; returns NaN if gridblock cornerpoints are not defined 
        /// </summary>
        /// <param name="X_in">X (grid) coordinate of the location</param>
        /// <param name="Y_in">Y (grid) coordinate of the location</param>
        /// <returns>Thickness of the gridblock (m)</returns>
        public double getTVT(double X_in, double Y_in)
        {
            // Define variables for output and for position relative to the relative to the gridblock boundaries (uv coordinates)
            double TVT_out;
            double u, v;

            // Get the position relative to the relative to the gridblock boundaries (uv coordinates)
            // If this is undefined, the getPositionRelativeToGridblock function will return false. In this case we cannot calculate the depth and must return the default value.
            if (getPositionRelativeToGridblock(out u, out v, X_in, Y_in))
            {
                // Get TVT at the corner pillars
                double SW_TVT = (SWtop.Z - SWbottom.Z);
                double SE_TVT = (SEtop.Z - SEbottom.Z);
                double NW_TVT = (NWtop.Z - NWbottom.Z);
                double NE_TVT = (NEtop.Z - NEbottom.Z);

                // Get inverse of u and v
                double u_inv = 1 - u;
                double v_inv = 1 - v;

                // Calculate TVT at X,Y
                TVT_out = (u_inv * v_inv * SW_TVT) + (u * v_inv * SE_TVT) + (u_inv * v * NW_TVT) + (u * v * NE_TVT);
            }
            // Default return value is CurrentThickness, representing the mean thickness of the gridblock
            // This will be returned if the the depth at the specified point cannot be calculated (which may occur with inverted geometries)
            else
            {
                TVT_out = CurrentThickness;
            }

            // Return TVT
            return TVT_out;
        }
        /// <summary>
        /// Projects a list of points vertically to the centre of the gridblock; sets Z values to -1 if gridblock cornerpoints are not defined
        /// </summary>
        /// <param name="points_in">List of XYZ points to project vertically</param>
        public void getDepths(List<PointXYZ> points_in)
        {
            // Go through the list of input points, calculating the depth for each
            // NB the getDepth function will automatically set the depth to -1 if the cornerpoints are undefined or the depth cannot be calculated for the specified X and Y coordinates
            foreach (PointXYZ point in points_in)
                point.Z = getCentreZ(point.X, point.Y);
        }
        /// <summary>
        /// Create a point at a random location within the gridblock
        /// </summary>
        /// <param name="useQuickMethod">If true, use quick calculation (not valid for concave or inverted gridblocks, and there will be a bias in the point location if the gridblock is not a parallelipiped), otherwise use long calculation (slower but gives a perfectly random position regardless of gridblock shape)</param>
        /// <returns>PointXYZ object representing a randomly located point in grid (XYZ) coordinates</returns>
        public PointXYZ getRandomPoint(bool useQuickMethod)
        {
            PointXYZ output = null;

            // Check to see if all cornerpoints are defined
            if (checkCornerpointsDefined())
            {
                // Get reference to the random number generator
                Random randGen = RandGen;

                if (useQuickMethod)
                {
                    // Quick calculation: not valid for concave or inverted gridblocks, and there will be a bias in the point location if the gridblock is not a parallelipiped

                    // Get a random position relative to the relative to the gridblock boundaries (uvw coordinates)
                    double u = randGen.NextDouble();
                    double v = randGen.NextDouble();
                    double w = randGen.NextDouble();

                    // Convert relative coordinates to XYZ coordinates
                    output = getAbsolutePosition(u, v, w);
                }
                else
                {
                    // Long calculation: select a random point in xyz coordinates then check to see if it lies within the gridblock boundaries
                    // Slower but gives a perfectly random position regardless of gridblock shape

                    // Get edge lengths projected onto X, Y and Z axes
                    double Xwidth = (MaxX - MinX);
                    double Ylength = (MaxY - MinY);
                    double Zheight = (MaxZ - MinZ);

                    // Generate the point and then check if it lies within the gridblock
                    int MaxTries = 1000;
                    do
                    {
                        double newX = MinX + (Xwidth * randGen.NextDouble());
                        double newY = MinY + (Ylength * randGen.NextDouble());
                        double newZ = MinZ + (Zheight * randGen.NextDouble());
                        output = new PointXYZ(newX, newY, newZ);

                        // If we cannot get a point after 1000 tries, use the quick method
                        MaxTries--;
                        if (MaxTries < 0) break;

                    } while (!checkPointInGridblock(output));

                    if (MaxTries < 0)
                        output = getRandomPoint(true);
                }
            }
            return output;
        }
        /// <summary>
        /// Check if a specified point lies within the gridblock (strictly speaking, a vertical projection of the gridblock from the middle surface, where the edges connecting the upper and lower surfaces are rotated to vertical)
        /// </summary>
        /// <param name="point_in">Point in XYZ coordinates</param>
        /// <returns>true if point_in lies within the gridblock, false if it does not</returns>
        public bool checkPointInGridblock(PointXYZ point_in)
        {
            // Cache coordinates locally
            double pointX = point_in.X;
            double pointY = point_in.Y;
            double pointZ = point_in.Z;

            // To check whether the projection of the point lies within the gridblock on the XY plane, count the number of gridblock boundaries crossed by an infinite line projected north from the point, and the direction of crossing
            // If the number of boundaries crossed outward is one more then the number of boundaries crossed inwards, the point must lie inside the gridblock
            // Otherwise the point must lie outside the gridblock
            // This is valid regardless of gridblock geometry - even for concave or inverted gridblocks (NB for inverted gridblocks, the inverted section is considered to lie outside the gridblock)
            int noBoundariesCrossed = 0;
            foreach (GridDirection boundary in new GridDirection[4] { GridDirection.N, GridDirection.E, GridDirection.S, GridDirection.W })
            {
                bool crossesOutwards;
                double intersectionY = getBoundaryIntersection(point_in, GridDirection.N, boundary, out crossesOutwards);
                if (intersectionY >= pointY)
                {
                    if (crossesOutwards)
                        noBoundariesCrossed++;
                    else
                        noBoundariesCrossed--;
                }
            }
            if (noBoundariesCrossed != 1)
                return false;

            // If the projection of the point lies within the gridblock on the XY plane, the point will lie within the gridblock if its Z coordinate lies between the upper and lower surfaces of the gridblock
            double centreZ = getCentreZ(pointX, pointY);
            double TVT = getTVT(pointX, pointY);
            double topSurfaceIntersection = centreZ + (TVT / 2);
            double bottomSurfaceIntersection = centreZ - (TVT / 2);
            if ((pointZ >= bottomSurfaceIntersection) && (pointZ <= topSurfaceIntersection))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Get a reference to the fracture set in this gridblock that best matches the orientation and strike of another fracture set (typically in another gridblock)
        /// </summary>
        /// <param name="thisGB_fs">Reference to the input fracture set</param>
        /// <param name="inputFS_strike">Strike of the input fracture set</param>
        /// <returns></returns>
        public LayerBoundFractureSet getClosestFractureSet(LayerBoundFractureSet thisGB_fs, double inputFS_strike)
        {
            // Check if the strike of this equivalent set lies within the allowed range
            double maxStrikeDifference = gd.DFNControl.MaxConsistencyAngle;
            double actualStrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, thisGB_fs.Strike);
            if (actualStrikeDifference > maxStrikeDifference)
            {
                // If the strike of the equivalent set lies outside the allowed range, loop through all fracture sets to find the best fit
                // NB this may still be the equivalent set
                foreach (LayerBoundFractureSet test_fs in LayerBoundFractureSets)
                {
                    // Check if the difference between the previous propagation direction and this configuration is less than the minimum found so far
                    double test_StrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, test_fs.Strike);
                    if (test_StrikeDifference < actualStrikeDifference)
                    {
                        // If so set the best match set to this set; also update the minimum angular difference found so far
                        actualStrikeDifference = test_StrikeDifference;
                        thisGB_fs = test_fs;
                    }
                }
            }

            // Return a reference to the best fit fracture set
            return thisGB_fs;
        }
        /// <summary>
        /// Get a reference to the fracture set in this gridblock that best matches the orientation and strike of another fracture set (typically in another gridblock)
        /// </summary>
        /// <param name="inputFS_index">Index number of the input fracture set</param>
        /// <param name="inputFS_strike">Strike of the input fracture set</param>
        /// <returns></returns>
        public LayerBoundFractureSet getClosestFractureSet(int inputFS_index, double inputFS_strike)
        {
            // First we will try the equivalent set to that of the input fracture set
            LayerBoundFractureSet thisGB_fs = LayerBoundFractureSets[inputFS_index];

            // Check if the strike of this equivalent set lies within the allowed range
            double maxStrikeDifference = gd.DFNControl.MaxConsistencyAngle;
            double actualStrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, thisGB_fs.Strike);
            if (actualStrikeDifference > maxStrikeDifference)
            {
                // If the strike of the equivalent set lies outside the allowed range, loop through all fracture sets to find the best fit
                // NB this may still be the equivalent set
                foreach (LayerBoundFractureSet test_fs in LayerBoundFractureSets)
                {
                    // Check if the difference between the previous propagation direction and this configuration is less than the minimum found so far
                    double test_StrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, test_fs.Strike);
                    if (test_StrikeDifference < actualStrikeDifference)
                    {
                        // If so set the best match set to this set; also update the minimum angular difference found so far
                        actualStrikeDifference = test_StrikeDifference;
                        thisGB_fs = test_fs;
                    }
                }
            }

            // Return a reference to the best fit fracture set
            return thisGB_fs;
        }
        /// <summary>
        /// Get the index number of the fracture set in this gridblock that best matches the orientation and strike of another fracture set (typically in another gridblock)
        /// </summary>
        /// <param name="inputFS_index">Index number of the input fracture set</param>
        /// <param name="inputFS_strike">Strike of the input fracture set</param>
        /// <returns></returns>
        public int getClosestFractureSetIndex(int inputFS_index, double inputFS_strike)
        {
            // First we will try the equivalent set to that of the input fracture set
            LayerBoundFractureSet thisGB_fs = LayerBoundFractureSets[inputFS_index];

            // Check if the strike of this equivalent set lies within the allowed range
            double maxStrikeDifference = gd.DFNControl.MaxConsistencyAngle;
            double actualStrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, thisGB_fs.Strike);
            if (actualStrikeDifference > maxStrikeDifference)
            {
                // If the strike of the equivalent set lies outside the allowed range, loop through all fracture sets to find the best fit
                // NB this may still be the equivalent set
                for (int FS_index = 0; FS_index < NoLayerBoundFractureSets; FS_index++)
                {
                    LayerBoundFractureSet test_fs = LayerBoundFractureSets[FS_index];

                    // Check if the difference between the previous propagation direction and this configuration is less than the minimum found so far
                    double test_StrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, test_fs.Strike);
                    if (test_StrikeDifference < actualStrikeDifference)
                    {
                        // If so set the best match set to this set; also update the minimum angular difference found so far
                        actualStrikeDifference = test_StrikeDifference;
                        inputFS_index = FS_index;
                    }
                }
            }

            // Return a reference to the best fit fracture set
            return inputFS_index;
        }
        /// <summary>
        /// Find the unconfined fracture set with orientation closest to the supplied normal vector
        /// </summary>
        /// <param name="NormalVector">Normal vector to test against the normal vectors of the unconfined fracture sets</param>
        /// <returns>Reference to the UnconfinedFractureSet with normal vector closest to the supplied normal vector; null if there are no unconfined fracture sets</returns>
        public UnconfinedFractureSet getClosestUnconfinedFractureSet(VectorXYZ NormalVector)
        {
            // Normalise the input vector if it is not already normalised - therefore the dot product of the input vector and the fracture set normal vectors will just be the cosine of the angle between them
            VectorXYZ vectorToTest = NormalVector.GetNormalisedVector();

            // Loop through all unconfined fracture sets, checking the cosine of the angle between their normals and the supplied normal
            // The closest match in orientation will have an absolute value of cosine nearest to 1
            UnconfinedFractureSet closestSet = null;
            double bestMatch = 0;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
            {
                double orientationMatch = Math.Abs(ufs.NormalVector & vectorToTest);
                if (orientationMatch > bestMatch)
                {
                    bestMatch = orientationMatch;
                    closestSet = ufs;
                }
            }

            // Return the closest matching fracture set
            return closestSet;
        }
        /// <summary>
        /// Find the index number of the unconfined fracture set with orientation closest to the supplied normal vector
        /// </summary>
        /// <param name="NormalVector">Normal vector to test against the normal vectors of the unconfined fracture sets</param>
        /// <returns>Index number of the UnconfinedFractureSet with normal vector closest to the supplied normal vector; -1 if there are no unconfined fracture sets</returns>
        public int getClosestUnconfinedFractureSetIndex(VectorXYZ NormalVector)
        {
            // Normalise the input vector if it is not already normalised - therefore the dot product of the input vector and the fracture set normal vectors will just be the cosine of the angle between them
            VectorXYZ vectorToTest = NormalVector.GetNormalisedVector();

            // Loop through all unconfined fracture sets, checking the cosine of the angle between their normals and the supplied normal
            // The closest match in orientation will have an absolute value of cosine nearest to 1
            int closestSetIndex = -1;
            double bestMatch = 0;
            for (int ufs_index = 0; ufs_index < NoUnconfinedFractureSets; ufs_index++)
            {
                UnconfinedFractureSet ufs = UnconfinedFractureSets[ufs_index];
                double orientationMatch = Math.Abs(ufs.NormalVector & vectorToTest);
                if (orientationMatch > bestMatch)
                {
                    bestMatch = orientationMatch;
                    closestSetIndex = ufs_index;
                }
            }

            // Return the index number of the closest matching fracture set
            return closestSetIndex;
        }

        /// <summary>
        /// Flag to connect parallel fractures that are deactivated because their stress shadows interact; this will allow long composite fractures to form
        /// </summary>
        public bool LinkFracturesInStressShadow { get { return gd.DFNControl.LinkFracturesInStressShadow; } }

        // References to adjacent gridblocks
        /// <summary>
        /// Dictionary containing references to neighbouring GridblockConfiguration objects
        /// </summary>
        public Dictionary<GridDirection, GridblockConfiguration> NeighbourGridblocks;
        /// <summary>
        /// Get a reference to a diagonal neighbouring gridblock (i.e. to SW, NW, NE or SE), if it exists
        /// </summary>
        /// <param name="Direction1">First part of diagonal direction</param>
        /// <param name="Direction2">Second part of diagonal direction</param>
        /// <returns>Reference to the specified diagonal neighbouring gridblock (or null if it does not exist)</returns>
        public GridblockConfiguration getDiagonalNeighbour(GridDirection Direction1, GridDirection Direction2)
        {
            if (NeighbourGridblocks[Direction1] != null)
                if (NeighbourGridblocks[Direction1].NeighbourGridblocks[Direction2] != null)
                    return NeighbourGridblocks[Direction1].NeighbourGridblocks[Direction2];
            if (NeighbourGridblocks[Direction2] != null)
                return NeighbourGridblocks[Direction2].NeighbourGridblocks[Direction1];
            return null;
        }
        /// <summary>
        /// Get a reference to a diagonal neighbouring gridblock in 3D (i.e. to upper or lower SW, NW, NE or SE), if it exists
        /// </summary>
        /// <param name="Direction1">First part of diagonal direction</param>
        /// <param name="Direction2">Second part of diagonal direction</param>
        /// <param name="Direction3">Third part of diagonal direction</param>
        /// <returns>Reference to the specified diagonal neighbouring gridblock (or null if it does not exist)</returns>
        public GridblockConfiguration getDiagonalNeighbour(GridDirection Direction1, GridDirection Direction2, GridDirection Direction3)
        {
            if (NeighbourGridblocks[Direction1] != null)
                return NeighbourGridblocks[Direction1].getDiagonalNeighbour(Direction2, Direction3);
            else if (NeighbourGridblocks[Direction2] != null)
                return NeighbourGridblocks[Direction2].getDiagonalNeighbour(Direction1, Direction3);
            else if (NeighbourGridblocks[Direction3] != null)
                return NeighbourGridblocks[Direction3].getDiagonalNeighbour(Direction1, Direction2);
            return null;
        }
        /// <summary>
        /// Flag to determine whether to search neighbouring gridblocks for stress shadow interaction; dependent on gridblock geometry
        /// Only applies if the flag in the DFNControl object is set to Automatic
        /// </summary>
        private bool searchNeighbouringGridblocks_Automatic;
        /// <summary>
        /// Flag to control whether to search neighbouring gridblocks for stress shadow interaction
        /// </summary>
        /// <returns></returns>
        private bool SearchNeighbouringGridblocks()
        {
            // If the parent grid object has not been defined, then assume this is automatic
            if (gd == null)
                return searchNeighbouringGridblocks_Automatic;

            // Check whether to search neighbouring gridblocks for stress shadow interaction
            switch (gd.DFNControl.SearchNeighbouringGridblocks)
            {
                case AutomaticFlag.None:
                    return false;
                case AutomaticFlag.All:
                    return true;
                case AutomaticFlag.Automatic:
                    return searchNeighbouringGridblocks_Automatic;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Create a list of all the neighbouring gridblocks that exist
        /// </summary>
        /// <param name="includeDiagonalNeighbours">Include the diagonal neighbours (gridblocks to NE, NW, SE and SW in the list</param>
        /// <returns></returns>
        public List<GridblockConfiguration> getNeighbourGridblocks(bool includeDiagonalNeighbours)
        {
            // Create a list item to store references to the neighbouring gridblocks
            List<GridblockConfiguration> neighbourGridblocks = new List<GridblockConfiguration>();

            // Add the neighbouring gridblocks in clockwise order from SW, including diagonal neighbours if required
            {
                if (includeDiagonalNeighbours)
                {
                    GridblockConfiguration gb_southwest = getDiagonalNeighbour(GridDirection.S, GridDirection.W);
                    if (gb_southwest != null)
                        neighbourGridblocks.Add(gb_southwest);
                }
                GridblockConfiguration gb_west = NeighbourGridblocks[GridDirection.W];
                if (gb_west != null)
                    neighbourGridblocks.Add(gb_west);
                if (includeDiagonalNeighbours)
                {
                    GridblockConfiguration gb_northwest = getDiagonalNeighbour(GridDirection.N, GridDirection.W);
                    if (gb_northwest != null)
                        neighbourGridblocks.Add(gb_northwest);
                }
                GridblockConfiguration gb_north = NeighbourGridblocks[GridDirection.N];
                if (gb_north != null)
                    neighbourGridblocks.Add(gb_north);
                if (includeDiagonalNeighbours)
                {
                    GridblockConfiguration gb_northeast = getDiagonalNeighbour(GridDirection.N, GridDirection.E);
                    if (gb_northeast != null)
                        neighbourGridblocks.Add(gb_northeast);
                }
                GridblockConfiguration gb_east = NeighbourGridblocks[GridDirection.E];
                if (gb_east != null)
                    neighbourGridblocks.Add(gb_east);
                if (includeDiagonalNeighbours)
                {
                    GridblockConfiguration gb_southeast = getDiagonalNeighbour(GridDirection.S, GridDirection.E);
                    if (gb_southeast != null)
                        neighbourGridblocks.Add(gb_southeast);
                }
                GridblockConfiguration gb_south = NeighbourGridblocks[GridDirection.S];
                if (gb_south != null)
                    neighbourGridblocks.Add(gb_south);
            }

            // Add gridblocks from the overlying and underlying layers
            // NB to save time in single layer grids, we will only add gridblocks from the overlying and underlying layers if the gridblocks immediately overlying and underlying the current gridblock are defined
            foreach (GridDirection verticalDirection in new GridDirection[2] { GridDirection.D, GridDirection.U })
            {
                GridblockConfiguration gb_vertical = NeighbourGridblocks[verticalDirection];
                if (gb_vertical != null)
                {
                    neighbourGridblocks.Add(gb_vertical);
                    if (includeDiagonalNeighbours)
                    {
                        GridblockConfiguration gb_southwest = getDiagonalNeighbour(verticalDirection, GridDirection.S, GridDirection.W);
                        if (gb_southwest != null)
                            neighbourGridblocks.Add(gb_southwest);
                        GridblockConfiguration gb_west = getDiagonalNeighbour(verticalDirection, GridDirection.W);
                        if (gb_west != null)
                            neighbourGridblocks.Add(gb_west);
                        GridblockConfiguration gb_northwest = getDiagonalNeighbour(verticalDirection, GridDirection.N, GridDirection.W);
                        if (gb_northwest != null)
                            neighbourGridblocks.Add(gb_northwest);
                        GridblockConfiguration gb_north = getDiagonalNeighbour(verticalDirection, GridDirection.N);
                        if (gb_north != null)
                            neighbourGridblocks.Add(gb_north);
                        GridblockConfiguration gb_northeast = getDiagonalNeighbour(verticalDirection, GridDirection.N, GridDirection.E);
                        if (gb_northeast != null)
                            neighbourGridblocks.Add(gb_northeast);
                        GridblockConfiguration gb_east = getDiagonalNeighbour(verticalDirection, GridDirection.E);
                        if (gb_east != null)
                            neighbourGridblocks.Add(gb_east);
                        GridblockConfiguration gb_southeast = getDiagonalNeighbour(verticalDirection, GridDirection.S, GridDirection.E);
                        if (gb_southeast != null)
                            neighbourGridblocks.Add(gb_southeast);
                        GridblockConfiguration gb_south = getDiagonalNeighbour(verticalDirection, GridDirection.S);
                        if (gb_south != null)
                            neighbourGridblocks.Add(gb_south);
                    }
                }
            }

            // Return the list
            return neighbourGridblocks;
        }

        // Objects containing geomechanical, fracture property and calculation data relating to the gridblock
        /// <summary>
        /// Control data for calculating fracture propagation
        /// </summary>
        public PropagationControl PropControl { get; private set; }
        /// <summary>
        /// Mechanical properties data
        /// </summary>
        public MechanicalProperties MechProps { get; private set; }
        /// <summary>
        /// Current stress and strain state data
        /// </summary>
        public StressStrainState StressStrain { get; private set; }

        // Orientation of fracture sets (assumed to be coaxial with applied minimum strain orientation in the first deformation episode)
        /// <summary>
        /// Azimuth of minimum horizontal strain (radians)
        /// </summary>
        public double Hmin_azimuth { get { return PropControl.Initial_Applied_Epsilon_hmin_azimuth; } }

        // Present day effective stress - to use for calculating fracture aperture and permeability
        /// <summary>
        /// Present day Terzaghi effective stress tensor - can be used to override the stress at the time of deformation, to calculate present day fracture aperture and permeability
        /// </summary>
        public Tensor2S PresentDayStress { get; private set; }
        /// <summary>
        /// Flag to use present day effective stress tensor, instead of stress at the time of deformation, to calculate fracture aperture and permeability
        /// </summary>
        public bool UsePresentDayStress { get { return !(PresentDayStress is null); } }
        /// <summary>
        /// Specify the present day Terzaghi effective stress, to be used when calculating fracture aperture and permeability
        /// </summary>
        /// <param name="EffectiveStress">Tensor2S object defining the present day Terzaghi effective stress</param>
        public void SetPresentDayStress(Tensor2S EffectiveStress)
        {
            PresentDayStress = EffectiveStress;
            UpdatePresentDayStressOnFractures();
        }
        /// <summary>
        /// Specify the present day Terzaghi effective stress, to be used when calculating fracture aperture and permeability
        /// </summary>
        /// <param name="SigmaEffXX">XX component of Terzaghi effective stress tensor (Pa)</param>
        /// <param name="SigmaEffYY">YY component of Terzaghi effective stress tensor (Pa)</param>
        /// <param name="SigmaEffZZ">ZZ component of Terzaghi effective stress tensor (Pa)</param>
        /// <param name="SigmaEffXY">XY component of Terzaghi effective stress tensor (Pa)</param>
        /// <param name="SigmaEffYZ">YZ component of Terzaghi effective stress tensor (Pa)</param>
        /// <param name="SigmaEffZX">ZX component of Terzaghi effective stress tensor (Pa)</param>
        public void SetPresentDayTerzaghiStress(double SigmaEffXX, double SigmaEffYY, double SigmaEffZZ, double SigmaEffXY, double SigmaEffYZ, double SigmaEffZX)
        {
            SetPresentDayStress(new Tensor2S(SigmaEffXX, SigmaEffYY, SigmaEffZZ, SigmaEffXY, SigmaEffYZ, SigmaEffZX));
        }
        /// <summary>
        /// Specify the present day absolute stress and fluid pressure, to calculate an effective stress tensor to be used when calculating fracture aperture and permeability
        /// </summary>
        /// <param name="SigmaXX">XX component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaYY">YY component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaZZ">ZZ component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaXY">XY component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaYZ">YZ component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaZX">ZX component of absolute stress tensor (Pa)</param>
        /// <param name="FP">Fluid pressure</param>
        public void SetPresentDayAbsoluteStress(double SigmaXX, double SigmaYY, double SigmaZZ, double SigmaXY, double SigmaYZ, double SigmaZX, double FP)
        {
            SetPresentDayStress(new Tensor2S(SigmaXX - FP, SigmaYY - FP, SigmaZZ - FP, SigmaXY, SigmaYZ, SigmaZX));
        }
        /// <summary>
        /// Specify the present day Biot effective stress and fluid pressure, to calculate a Terzaghi effective stress tensor to be used when calculating fracture aperture and permeability
        /// </summary>
        /// <param name="SigmaXX">XX component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaYY">YY component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaZZ">ZZ component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaXY">XY component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaYZ">YZ component of absolute stress tensor (Pa)</param>
        /// <param name="SigmaZX">ZX component of absolute stress tensor (Pa)</param>
        /// <param name="FP">Fluid pressure</param>
        /// <param name="BiotCoefficient">Biot coefficient</param>
        public void SetPresentDayBiotStress(double SigmaXX, double SigmaYY, double SigmaZZ, double SigmaXY, double SigmaYZ, double SigmaZX, double FP, double BiotCoefficient)
        {
            if (double.IsNaN(BiotCoefficient))
                BiotCoefficient = MechProps.Biot;
            double stressAdjustment = FP * (1 - BiotCoefficient);
            SetPresentDayStress(new Tensor2S(SigmaXX - stressAdjustment, SigmaYY - stressAdjustment, SigmaZZ - stressAdjustment, SigmaXY, SigmaYZ, SigmaZX));
        }
        /// <summary>
        /// Specify the present day strain and fluid overpressure, to calculate an effective stress tensor to be used when calculating fracture aperture and permeability
        /// </summary>
        /// <param name="Ehmin">Minimum (most tensile) principal applied horizontal strain</param>
        /// <param name="Ehmax">Maximum (most compressive) principal applied horizontal strain</param>
        /// <param name="EhminAzi">Azimuth of minimum principal applied horizontal strain, clockwise from N (radians)</param>
        /// <param name="fluidOverpressure">Fluid overpressure (Pa)</param>
        /// <param name="E_r">Present day Young's Modulus (Pa); if NaN, will use Young's Modulus defined in the MechProps object</param>
        /// <param name="Nu_r">Present day Poisson's ratio; if NaN, will use Poisson's ratio defined in the MechProps object</param>
        /// <param name="BiotCoefficient">Present day Biot coefficient; if NaN, will use Biot coefficient defined in the MechProps object</param>
        /// <param name="InitialStressRelaxation">Present day initial stress relaxation; if NaN, will use initial stress relaxation defined in the StressStrain object</param>
        public void SetPresentDayStressFromStrain(double Ehmin, double Ehmax, double EhminAzi, double fluidOverpressure, double E_r, double Nu_r, double BiotCoefficient, double InitialStressRelaxation)
        {
            // NB We use the Terzaghi effective stress rather than the Biot effective stress, because the differential compaction of the grains and the bulk rock is accounted for in the compactional strain
            // As a result the el_Epsilon strain tensor is related to the Terzaghi effective stress tensor by Hooke's Law
            // Similarly the el_Epsilon_noncompactional strain tensor is related to the Biot effective stress tensor by Hooke's Law

            // If valid present day mechanical property, stress or strain data have not been supplied, use the values in the MechProps or StressStrain objects, or other already defined values
            // These represent the values at the time of deformation
            // Also set a flag for whether a valid Young's Modulus has been supplied - if so we will use this rather than the bulk rock stiffness tensor to calculate stress
            bool E_supplied = (E_r > 0);
            if (!E_supplied)
                E_r = MechProps.E_r;
            bool Nu_supplied = (Nu_r >= 0) && (Nu_r <= 0.5);
            if (!Nu_supplied)
                Nu_r = MechProps.Nu_r;
            double E_eff = E_r / (1 - Math.Pow(Nu_r, 2));
            if (double.IsNaN(BiotCoefficient))
                BiotCoefficient = MechProps.Biot;
            double OneMinusBiot = 1 - BiotCoefficient;
            if (double.IsNaN(fluidOverpressure))
                fluidOverpressure = StressStrain.FluidOverpressure;
            if (double.IsNaN(Ehmin))
                Ehmin = 0;
            if (double.IsNaN(Ehmax))
                Ehmax = 0;
            if (double.IsNaN(EhminAzi))
                EhminAzi = 0;

            // Get the current lithostatic effective stress
            double fluidPressure = (CurrentDepth * StressStrain.FluidDensity * StressStrainState.Gravity) + fluidOverpressure;
            double lithostaticStress_eff_Terzaghi = (CurrentDepth * StressStrain.MeanOverlyingBulkRockDensity * StressStrainState.Gravity) - fluidPressure;

            // Get the initial stress relaxation, and if necessary calculate the critical initial stress relaxation
            if (double.IsNaN(InitialStressRelaxation))
                InitialStressRelaxation = StressStrain.InitialStressRelaxation;
            if (double.IsNaN(InitialStressRelaxation) || (InitialStressRelaxation < 0))
            {
                // Cache mechanical properties for intact rock
                double MuFr = MechProps.MuFr;

                // Calculate the initial stress relaxation required for critical stress state
                double friction_angle = Math.Atan(MuFr);
                double sin_friction_angle = Math.Sin(friction_angle);
                double sh0d_svd = (1 - sin_friction_angle) / (1 + sin_friction_angle);
                InitialStressRelaxation = (((1 - Nu_r) * sh0d_svd) - Nu_r) / (1 - (2 * Nu_r));
                // Add component to take account of differential grain compaction (Biot coefficient)
                if (OneMinusBiot != 0)
                {
                    InitialStressRelaxation += (OneMinusBiot * (fluidPressure / lithostaticStress_eff_Terzaghi));
                }
            }

            // Calculate the present day Terzaghi effective stress tensor
            Tensor2S effStress;
            if ((PropControl.StressDistributionCase == StressDistribution.EvenlyDistributedStress) && !E_supplied)
            {
                // Effective stress tensor will be calculated from the lithostatic stress and horizontal strain by partial inversion of the bulk rock compliance tensor
                Tensor4_2Sx2S bulkRockCompliance = S_beff;

                // Define tensors for the horizontal strain (including both compactional and applied strain) and the vertical stress
                // Vertical strain and horizontal stress components will be filled in by partial inversion of the compliance tensor
                double horizontalInitialStrain = InitialStressRelaxation * (1 - (2 * Nu_r)) * (lithostaticStress_eff_Terzaghi / E_r);
                Tensor2S strain = new Tensor2S(horizontalInitialStrain, horizontalInitialStrain, 0, 0, 0, 0);
                strain += Tensor2S.HorizontalStrainTensor(Ehmin, Ehmax, EhminAzi);
                effStress = new Tensor2S(0, 0, lithostaticStress_eff_Terzaghi, 0, 0, 0);
                bulkRockCompliance.PartialInversion(ref strain, ref effStress);
            }
            else
            {
                // Reset the Terzaghi effective stress tensor to the initial stress with no applied horizontal strain (but including initial stress relaxation)
                // NB The differential compaction of the grains and the bulk rock by fluid pressure is accounted for in the compactional strain
                // As a result the el_Epsilon strain tensor (which includes compactional strain) is related to the Terzaghi effective stress tensor by Hooke's Law
                // Similarly the el_Epsilon_noncompactional strain tensor is related to the Biot effective stress tensor by Hooke's Law
                // Calculate the component of initial horizontal effective stress due to the weight of the overburden (lithostatic stress)
                double sigma_h0_eff_lithstress = (((InitialStressRelaxation * (1 - Nu_r)) + ((1 - InitialStressRelaxation) * Nu_r)) / (1 - Nu_r)) * lithostaticStress_eff_Terzaghi;
                // Calculate the component of initial horizontal effective stress due to differential compaction of grains by fluid pressure
                double sigma_h0_eff_fluidpress = -((1 - (2 * Nu_r)) / (1 - Nu_r)) * OneMinusBiot * fluidPressure;
                double sigma_h0_eff = sigma_h0_eff_lithstress + sigma_h0_eff_fluidpress;
                // Create the initial compactional effective stress tensor,with zero applied strain
                effStress = new Tensor2S(sigma_h0_eff, sigma_h0_eff, lithostaticStress_eff_Terzaghi, 0, 0, 0);

                // Calculate the horizontal effective stress required to balance the applied strain
                double sigma_hmin_eff = E_eff * (Ehmin + (Nu_r * Ehmax));
                double sigma_hmax_eff = E_eff * ((Nu_r * Ehmin) + Ehmax);
                // Add the stress due to applied strain to the compactional effective stress tensor
                // NB We can use the Tensor2S.HorizontalStrainTensor function to generate this, as it can be used for any tensor quantity
                effStress += Tensor2S.HorizontalStrainTensor(sigma_hmin_eff, sigma_hmax_eff, EhminAzi);
            }

            // Set the present day Terzaghi effective stress tensor
            SetPresentDayStress(effStress);
        }
        /// <summary>
        /// Reset to use stress at the time of deformation, instead of present day effective stress tensor, to calculate fracture aperture and permeability
        /// </summary>
        public void UnsetPresentDayStress()
        {
            PresentDayStress = null;
            UpdatePresentDayStressOnFractures();
        }
        /// <summary>
        /// Recalculate present day effective stress acting on each fracture dip set
        /// </summary>
        private void UpdatePresentDayStressOnFractures()
        {
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    fds.UsePresentDayStress(PresentDayStress);
        }

        // Lists containing fracture sets
        /// <summary>
        /// Total number of fracture sets of all types
        /// </summary>
        public int NoFractureSets { get { return NoLayerBoundFractureSets + NoUnconfinedFractureSets; } }
        /// <summary>
        /// Number of layer-bound fracture sets
        /// These are defined by fracture strike, and subdivided into dipset by dip
        /// </summary>
        public int NoLayerBoundFractureSets { get { return LayerBoundFractureSets.Count; } }
        /// <summary>
        /// Number of unconfined fracture sets
        /// NB Unconfined fracture sets are not subdivided into dipsets; unconfined fractures with the same strike but different dips are counted as different sets
        /// </summary>
        public int NoUnconfinedFractureSets { get { return UnconfinedFractureSets.Count; } }
        /// <summary>
        /// Index number of the fracture set perpendicular to HMin
        /// </summary>
        public int HMin_FractureSet_Index { get { return 0; } }
        /// <summary>
        /// Index number of the fracture set perpendicular to HMax (or the closest set, if the total number of fracture sets is odd)
        /// </summary>
        public int HMax_FractureSet_Index { get { return NoLayerBoundFractureSets / 2; } }
        /// <summary>
        /// Get a name for a specified layer-bound fracture set in this gridblock, indicating its orientation
        /// </summary>
        /// <param name="indexNo">Index number of the layer-bound fracture set</param>
        /// <returns>String representing the fracture set name</returns>
        public string getLayerBoundFractureSetName(int indexNo)
        {
            return getLayerBoundFractureSetName(indexNo, NoLayerBoundFractureSets);
        }
        /// <summary>
        /// Get a name for a specified layer-bound fracture set in a generic gridblock, indicating its orientation
        /// </summary>
        /// <param name="indexNo">Index number of the layer-bound fracture set</param>
        /// <param name="noFractureSets">Total number of layer-bound fracture sets in the gridblock</param>
        /// <returns>String representing the fracture set name</returns>
        public static string getLayerBoundFractureSetName(int indexNo, int noFractureSets)
        {
            int hMin_FractureSet_Index = 0;
            int hMax_FractureSet_Index = noFractureSets / 2;

            string name;
            if (noFractureSets == 1)
                name = "HMin";
            else if (indexNo < hMax_FractureSet_Index)
            {
                name = "HMin";
                if (indexNo > hMin_FractureSet_Index)
                    name += string.Format("+{0}deg", (indexNo - hMin_FractureSet_Index) * (180 / noFractureSets));
            }
            else
            {
                name = "HMax";
                if (indexNo > hMax_FractureSet_Index)
                    name += string.Format("+{0}deg", (indexNo - hMax_FractureSet_Index) * (180 / noFractureSets));
            }
            return name;
        }
        /// <summary>
        /// Get a name for a specified unconfined fracture set in this gridblock, indicating its orientation
        /// </summary>
        /// <param name="ufs_index">Index number of the unconfined fracture set</param>
        /// <returns>String representing the unconfined fracture set name</returns>
        public string getUnconfinedFractureSetName(int ufs_index)
        {
            string setName;
            if (ufs_index < NoUnconfinedFractureSets)
            {
                UnconfinedFractureSet ufs = UnconfinedFractureSets[ufs_index];
                setName = string.Format("Unconfined set {0}: Azimuth {1} Dip {2}", ufs_index, Math.Round(ufs.Azimuth * 180 / Math.PI), Math.Round(ufs.Dip * 180 / Math.PI));
            }
            else
            {
                setName = string.Format("Invalid set");
            }
            return setName;
        }
        /// <summary>
        /// Set the mechanical property overrides for any fracture sets parallel to the defined cleavages
        /// </summary>
        /// <param name="Cleavages_in">List of Cleavage objects, one per cleavage orientation</param>
        /// <param name="MaxConsistencyAngle_in">Maximum allowed variation in orientation between fracture sets and cleavages; if no fracture sets are found within this limit, no mechanical property overrides will be set</param>
        public void SetCleavages(List<Cleavage> Cleavages_in, double MaxConsistencyAngle_in)
        {
            // NB Cleavage overrides are currently applied only to unconfined fracture sets, and not to layer-bound fracture sets
            if (NoUnconfinedFractureSets > 0)
                foreach (Cleavage cleavage in Cleavages_in)
                {
                    // Find the unconfined fracture set closest to the cleavage orientation
                    double bestOrientationMatch = 0;
                    UnconfinedFractureSet closestSet = null;
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        double orientationMatch = Math.Abs(ufs.NormalVector & cleavage.NormalVector);
                        if (bestOrientationMatch < orientationMatch)
                        {
                            bestOrientationMatch = orientationMatch;
                            closestSet = ufs;
                        }
                    }

                    // Check if the orientation mismatch for the closest set is less than the maximum consistency angle
                    // If so set the mechanical property overrides
                    // Otherwise no overrides will be set
                    if ((Math.Acos(bestOrientationMatch) <= MaxConsistencyAngle_in) && !(closestSet is null))
                        closestSet.SetMechanicalPropertyOverrides(cleavage.GcOverride, cleavage.MuFrOverride);
                }
        }
        /// <summary>
        /// Array of azimuthal stress shadow multipliers relating stress shadows for different fracture sets
        /// </summary>
        private double[,] FaaIJ;
        /// <summary>
        /// Array of strike-slip shear stress shadow multipliers relating stress shadows for different fracture sets
        /// </summary>
        private double[,] FasIJ;
        /// <summary>
        /// Array of stress shadow multipliers relating stress shadows for different unconfined fracture sets
        /// UCFW_IJ * W_J gives the width of stress shadows around a set I fracture as seen by a set J fracture
        /// </summary>
        private double[,] UCFW_IJ;
        /// <summary>
        /// Array containing the number of static half-macrofractures (MFP30) from each dipset terminating against macrofractures from every other fracture set
        /// Indices are: [set of propagating fracture, set of terminating fracture][dipset of terminating fracture]
        /// </summary>
        private double[,][] MFTerminations;
        // Holders for the list of stress shadow half-widths of all fracture sets as seen by all other fracture sets, and vice versa
        /// <summary>
        /// Holder for the list of stress shadow half-widths of other fracture sets as seen by a specified fracture set
        /// </summary>
        private List<List<double>>[] StressShadowHalfWidthsIJ;
        /// <summary>
        /// Holder for the list of stress shadow half-widths of a specified fracture set as seen by other fracture sets
        /// </summary>
        private List<List<double>>[] StressShadowHalfWidthsJI;
        /// <summary>
        /// Update the MFTerminations array with the most recent dsIJ_MFP30 for each fracture set
        /// </summary>
        private void updateMFTerminations()
        {
            // Loop through every set of propagating fractures I
            for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
            {
                LayerBoundFractureSet fsI = LayerBoundFractureSets[fsI_Index];

                // Get the increment in sIJMFP30 for set I
                double dsIJ_MFP30 = 0;
                foreach (FractureDipSet dipSetIm in fsI.FractureDipSets)
                    dsIJ_MFP30 += dipSetIm.dsIJ_MFP30;

                // Get the total apparent MFP32 for all terminating fracture sets J
                // This includes all fracture sets except set I
                double totalApparentMFP32J = 0;
                double[][] apparentMFP32J = new double[NoLayerBoundFractureSets][];
                for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                {
                    if (fsI_Index == fsJ_Index)
                        continue;
                    LayerBoundFractureSet fsJ = LayerBoundFractureSets[fsJ_Index];

                    // Orientation multiplier to project the length of the terminating set J fracture perpendicular to the propagating set I fracture
                    double sinIJ = Math.Abs(VectorXYZ.Sin_trim(fsI.Strike - fsJ.Strike));

                    // Loop through each dip set in J
                    int noDipSetsJ = fsJ.FractureDipSets.Count;
                    apparentMFP32J[fsJ_Index] = new double[noDipSetsJ];
                    for (int dipSetIndexJm = 0; dipSetIndexJm < noDipSetsJ; dipSetIndexJm++)
                    {
                        FractureDipSet dipSetJm = fsJ.FractureDipSets[dipSetIndexJm];

                        double apparentMFP32_dipsetJm = sinIJ * (dipSetJm.a_MFP32_total() + dipSetJm.s_MFP32_total());
                        apparentMFP32J[fsJ_Index][dipSetIndexJm] = apparentMFP32_dipsetJm;
                        totalApparentMFP32J += apparentMFP32_dipsetJm;
                    }
                }

                // Loop through every other set of terminating fractures J and apportion sIJMFP30 values
                for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                {
                    if (fsI_Index == fsJ_Index)
                        continue;

                    // Loop through each dipset Jm in J
                    int noDipSetsJ = LayerBoundFractureSets[fsJ_Index].FractureDipSets.Count;
                    for (int dipSetIndexJm = 0; dipSetIndexJm < noDipSetsJ; dipSetIndexJm++)
                    {
                        // Calculate the apparent MFP32 of dipset Jm as a proportion of the total apparent MFP32 for all terminating fracture sets
                        // This ratio will be used to apportion the sIJMFP30 values
                        double apparentMFP32Jm_ratio = (totalApparentMFP32J > 0 ? apparentMFP32J[fsJ_Index][dipSetIndexJm] / totalApparentMFP32J : 0);

                        // Update the macrofracture termination array with the correctly proportioned sIJMFP30 value
                        MFTerminations[fsI_Index, fsJ_Index][dipSetIndexJm] += apparentMFP32Jm_ratio * dsIJ_MFP30;
                    } // End loop through each dipset Jm
                } // End loop through every other fracture set J
            } // End loop through every fracture set I

            // Loop through every fracture dipset Jm and set the total number of fractures I terminating against them
            for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
            {
                LayerBoundFractureSet fsJ = LayerBoundFractureSets[fsJ_Index];

                // Loop through each dipset Jm in J
                int noDipSetsJ = fsJ.FractureDipSets.Count;
                for (int dipSetIndexJm = 0; dipSetIndexJm < noDipSetsJ; dipSetIndexJm++)
                {
                    FractureDipSet dipSetJm = fsJ.FractureDipSets[dipSetIndexJm];

                    // Calculate the total number of fractures from all fracture sets I terminating against dipset Jm
                    double sIJm_MFP30 = 0;
                    for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                        sIJm_MFP30 += MFTerminations[fsI_Index, fsJ_Index][dipSetIndexJm];

                    // Set the mean number of fractures from all fracture sets I terminating against dipset Jm
                    dipSetJm.setTerminatingFractureDensity(sIJm_MFP30);

                } // End loop through each dipset Jm
            } // End loop through every other fracture set J
        }
        /// <summary>
        /// Update the UCFTerminations array with the most recent dsIJ_MFP30 for each fracture set
        /// </summary>
        private void updateUCFTerminations()
        {
            // Loop through every set of propagating fractures I
            for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
            {
                UnconfinedFractureSet ufsI = UnconfinedFractureSets[ufsI_Index];

                // Get the increment in sIJUCRP30 for set I
                double dsIJ_UCRP30 = ufsI.get_RP30_M(RayPropagationStatus.StaticIntersection) - ((CurrentImplicitTimestep > 0) ? ufsI.get_RP30_M(RayPropagationStatus.StaticIntersection, CurrentImplicitTimestep - 1) : 0);

                // Get the total apparent UCFP32 for all terminating fracture sets J
                // This includes all fracture sets except set I
                double totalApparentUCFP32J = 0;
                double[] apparentUCFP32J = new double[NoUnconfinedFractureSets];
                for (int ufsJ_Index = 0; ufsJ_Index < NoUnconfinedFractureSets; ufsJ_Index++)
                {
                    if (ufsI_Index == ufsJ_Index)
                        continue;
                    UnconfinedFractureSet ufsJ = UnconfinedFractureSets[ufsJ_Index];

                    // Orientation multiplier to project the length of the terminating set J fracture perpendicular to the propagating set I fracture
                    // This is difficult to calculate since we do not know the fracture rays can propagate in any direction in the plane of the fracture
                    // We will therefore take the sin of the angle between the two fracture normals
                    double sinIJ = Math.Sqrt(1 - Math.Pow(ufsI.NormalVector & ufsJ.NormalVector, 2));

                    // Get the apparent P32 of set J seen by set I
                    double apparentUCFP30_IJ = sinIJ * ufsJ.UCFP32_total();
                    apparentUCFP32J[ufsJ_Index] = apparentUCFP30_IJ;
                    totalApparentUCFP32J += apparentUCFP30_IJ;
                }

                // Loop through every other set of terminating fractures J and apportion sIJMFP30 values
                for (int ufsJ_Index = 0; ufsJ_Index < NoUnconfinedFractureSets; ufsJ_Index++)
                {
                    if (ufsI_Index == ufsJ_Index)
                        continue;
                    UnconfinedFractureSet ufsJ = UnconfinedFractureSets[ufsJ_Index];

                    // Calculate the apparent UCFP32 of set J as a proportion of the total apparent UCFP32 for all terminating fracture sets
                    // This ratio will be used to apportion the sIJUCRP30 values
                    double apparentUCFP32J_ratio = (totalApparentUCFP32J > 0 ? apparentUCFP32J[ufsJ_Index] / totalApparentUCFP32J : 0);

                    // Update the macrofracture termination array with the correctly proportioned sIJMFP30 value
                    UCFTerminations[ufsI_Index, ufsJ_Index] += apparentUCFP32J_ratio * dsIJ_UCRP30;
                } // End loop through every other fracture set J
            } // End loop through every fracture set I

            // Loop through every unconfined fracture set J and set the total number of fractures I terminating against them
            for (int ufsJ_Index = 0; ufsJ_Index < NoUnconfinedFractureSets; ufsJ_Index++)
            {
                UnconfinedFractureSet ufsJ = UnconfinedFractureSets[ufsJ_Index];

                    // Calculate the total number of fractures from all fracture sets I terminating against dipset Jm
                    double sIJm_UCFP30 = 0;
                    for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
                        sIJm_UCFP30 += UCFTerminations[ufsI_Index, ufsJ_Index];

                // Set the mean number of fractures from all fracture sets I terminating against set J
                ufsJ.setTerminatingFractureDensity(sIJm_UCFP30);
            } // End loop through every other fracture set J
        }
        /// <summary>
        /// Array containing the number of static unonfined fracture rays (UCRP30) from each set terminating against unconfined fractures from every other fracture set
        /// Indices are: [set of propagating fracture, set of terminating fracture]
        /// </summary>
        private double[,] UCFTerminations;
        /// <summary>
        /// Calculate the inverse stress shadow and clear zone volume for each fracture set J due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// </summary>
        /// <param name="isotropicFractureNetwork">Flag to use algorithm for isotropic or anisotropic fracture networks; set to true if the fracture network is (near) isotropic, otherwise set to false</param>
        private void setCrossFSStressShadows()
        {
            bool isotropicFractureNetwork = (P32AnisotropyIndex(true, false) <= PropControl.anisotropyCutoff);

            if (isotropicFractureNetwork)
                setCrossFSStressShadows_isotropic();
            else
                setCrossFSStressShadows_anisotropic();
        }
        /// <summary>
        /// Calculate the inverse stress shadow and clear zone volume for each fracture set K due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// Valid for isotropic fracture networks as it takes account of multiple fractures overlapping but does not account for the influence of a primary fracture set on the distribution of secondary sets
        /// </summary>
        private void setCrossFSStressShadows_isotropic()
        {
            // Fracture set K represents the fractures to which the stress shadows will apply
            // The stress shadow and exclusion zone widths will therefore be as seen by fracture set K
            for (int fsK_Index = 0; fsK_Index < NoLayerBoundFractureSets; fsK_Index++)
            {
                // Get a handle to fracture set K, and get the number of dipsets in set K
                LayerBoundFractureSet fsK = LayerBoundFractureSets[fsK_Index];
                int noDipSetsK = fsK.FractureDipSets.Count;

                // We must calculate:
                // - the total stress shadow volume of every set I as seen by set K,
                // - the total exclusion zone volume of every set I as seen by dipset Kn
                // To do this we will multiply the inverse stress shadow volumes and clear zone volumes of each set I
                double inverseStressShadowVolumeK = 1;
                double[] clearZoneVolumeKn = new double[noDipSetsK];
                for (int dipSetIndexKn = 0; dipSetIndexKn < noDipSetsK; dipSetIndexKn++)
                    clearZoneVolumeKn[dipSetIndexKn] = 1;

                // Fracture set I represents the fractures which the stress shadows and exclusion zones surround
                for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                {
                    // Get a handle to fracture set I
                    LayerBoundFractureSet fsI = LayerBoundFractureSets[fsI_Index];
                    int noDipSetsI = fsI.FractureDipSets.Count;

                    // Cache the appropriate azimuthal and strike-slip shear stress shadow multipliers for sets I and K locally
                    double Faa_IK = FaaIJ[fsI_Index, fsK_Index];
                    double Fas_IK = FasIJ[fsI_Index, fsK_Index];
                    double Faa_KI = FaaIJ[fsK_Index, fsI_Index];
                    double Fas_KI = FasIJ[fsK_Index, fsI_Index];

                    // Calculate the total stress shadow volume of set I and the mean stress shadow width of a set I fracture, as seen by fracture set K
                    // To do this we will need to loop through each dip set in fracture set I, calculating the stress shadow volume of each
                    double psiI = 0;
                    double P32totalI = 0;
                    for (int dipSetIndexIm = 0; dipSetIndexIm < noDipSetsI; dipSetIndexIm++)
                    {
                        FractureDipSet dipSetIm = fsI.FractureDipSets[dipSetIndexIm];

                        // Get the MFP32 and the azimuthal and strike-slip shear components of the mean stress shadow width for this dipset Im
                        double P32_dipsetIm = dipSetIm.a_MFP32_total() + dipSetIm.s_MFP32_total();
                        double WaaIm = dipSetIm.Mean_Azimuthal_MF_StressShadowWidth;
                        double WasIm = dipSetIm.Mean_Shear_MF_StressShadowWidth;

                        // Calculate the stress shadow width of a fracture from this dipset Im as seen by fracture set K
                        double W_dipsetIm = (WaaIm * Faa_IK) + (WasIm * Fas_IK);

                        // Calculate the stress shadow volume of this dipset of Im as seen by fracture set K, and add it to the total for set I
                        double psi_dipsetI = W_dipsetIm * P32_dipsetIm;
                        psiI += psi_dipsetI;
                        P32totalI += P32_dipsetIm;
                    }
                    if (psiI < 0)
                        psiI = 0;
                    if (psiI > 1)
                        psiI = 1;
                    double W_IK = (P32totalI > 0 ? psiI / P32totalI : 0);

                    // Update the total inverse stress shadow volume seen by fracture set K
                    inverseStressShadowVolumeK *= (1 - psiI);

                    // Orientation multiplier to project the width of a stress shadow around around a set I fracture onto the azimuth of a set K fracture
                    double cosIK = Math.Abs(VectorXYZ.Cos_trim(fsI.Strike - fsK.Strike));

                    // Loop through each dipset in fracture set K
                    for (int dipSetIndexKn = 0; dipSetIndexKn < noDipSetsK; dipSetIndexKn++)
                    {
                        FractureDipSet dipSetKn = fsK.FractureDipSets[dipSetIndexKn];

                        // Get the azimuthal and strike-slip shear components of the mean stress shadow width for dipset Kn
                        double WaaKn = dipSetKn.Mean_Azimuthal_MF_StressShadowWidth;
                        double WasKn = dipSetKn.Mean_Shear_MF_StressShadowWidth;

                        // Calculate the stress shadow width of a fracture from dipset Kn as seen by fracture set I
                        double W_KnI = (WaaKn * Faa_KI) + (WasKn * Fas_KI);

                        // Calculate the mean exclusion zone width around a set I fracture, as seen by fracture dipset Kn
                        double exclusionZoneWidthIKn = W_IK + (W_KnI * cosIK);

                        // Calculate the clear zone volume of set I, as seen by fracture dipset Kn
                        double clearZoneVolumeIKn;
                        if (fsI_Index == fsK_Index)
                            clearZoneVolumeIKn = fsI.getClearZoneVolume(W_KnI);
                        else
                            clearZoneVolumeIKn = fsI.getInverseProximityZoneVolume(exclusionZoneWidthIKn);

                        // Update the inverse stress shadow volume seen by fracture dipset Kn
                        clearZoneVolumeKn[dipSetIndexKn] *= clearZoneVolumeIKn;
                    }
                }

                // Finally we can set the total stress shadow and exclusion zone volumes for each dipset Kn
                for (int dipSetIndexKn = 0; dipSetIndexKn < noDipSetsK; dipSetIndexKn++)
                {
                    fsK.FractureDipSets[dipSetIndexKn].setOtherFSExclusionZoneData(1 - inverseStressShadowVolumeK, 1 - clearZoneVolumeKn[dipSetIndexKn]);
                }

            } // End loop through each fracture set K
        }
        /// <summary>
        /// Calculate the inverse stress shadow and clear zone volume for each fracture set K due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// Valid for anisotropic fracture networks as it uses the number of fracture tips to calculate overlaps between fracture sets
        /// This takes account of the influence of a primary fracture set on the distribution of secondary sets, but ignores overlaps of multiple fractures
        /// </summary>
        private void setCrossFSStressShadows_anisotropic()
        {
            // Create a matrix of proportional stress shadow and exclusion zone overlaps between all fracture sets
            double[,][] tipOverlaps = new double[NoLayerBoundFractureSets, NoLayerBoundFractureSets][];
            // Fracture set I represents the propagating fracture
            for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
            {
                LayerBoundFractureSet fsI = LayerBoundFractureSets[fsI_Index];

                // Get the total area of static set I fractures
                // NB we will ignore active macrofractures as these may overlap the stress shadow or exclusion zones of other fracture sets without terminating against them
                double IMFP32 = 0;
                foreach (FractureDipSet dipSetIm in fsI.FractureDipSets)
                    IMFP32 += dipSetIm.s_MFP32_total();

                // Fracture dipset Jm represents the terminating fracture
                for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                {
                    LayerBoundFractureSet fsJ = LayerBoundFractureSets[fsJ_Index];
                    int noDipSetsJ = fsJ.FractureDipSets.Count;
                    tipOverlaps[fsI_Index, fsJ_Index] = new double[noDipSetsJ];

                    // Orientation multiplier to project the width of a stress shadow around around a set J fracture onto the strike of a set I fracture
                    double sinIJ = Math.Abs(VectorXYZ.Sin_trim(fsI.Strike - fsJ.Strike));
                    double sinIJ_IMFP32 = sinIJ * IMFP32;

                    // Loop through each dip set in J
                    for (int dipSetIndexJm = 0; dipSetIndexJm < noDipSetsJ; dipSetIndexJm++)
                    {
                        FractureDipSet dipSetJm = fsJ.FractureDipSets[dipSetIndexJm];

                        // Get the total density of set I macrofracture tips terminating against fractures from dipset Jm
                        double sIJmMFP30 = MFTerminations[fsI_Index, fsJ_Index][dipSetIndexJm];

                        // Calculate the intersection volumes of set I and dipset Jm stress shadows and exclusion zones, and add them to the appropriate arrays
                        if (sinIJ_IMFP32 > 0)
                            tipOverlaps[fsI_Index, fsJ_Index][dipSetIndexJm] = (sIJmMFP30 * ThicknessAtDeformation) / sinIJ_IMFP32;
                        else
                            tipOverlaps[fsI_Index, fsJ_Index][dipSetIndexJm] = 0;
                    }
                }
            }

            // Fracture set K represents the fractures to which the stress shadows will apply
            // The stress shadow and exclusion zone widths will therefore be as seen by fracture set K
            for (int fsK_Index = 0; fsK_Index < NoLayerBoundFractureSets; fsK_Index++)
            {
                // Get a handle to fracture set K, and get the number of dipsets in set K
                LayerBoundFractureSet fsK = LayerBoundFractureSets[fsK_Index];
                int noDipSetsK = fsK.FractureDipSets.Count;

                // First we will calculate:
                // - the stress shadow width of every dipset Im as seen by set K,
                // - the total stress shadow volume of every set I as seen by set K (not including overlaps),
                // - the maximum exclusion zone width of every dipset Im as seen by dipset Kn, and
                // - the total exclusion zone volume of every set I as seen by dipset Kn (not including overlaps)
                double[][] stressShadowWidthImK = new double[NoLayerBoundFractureSets][];
                double[] stressShadowVolumeIK = new double[NoLayerBoundFractureSets];
                double[][][] exclusionZoneWidthImKn = new double[NoLayerBoundFractureSets][][];
                double[][] exclusionZoneVolumeIKn = new double[NoLayerBoundFractureSets][];

                // Fracture set I represents the fractures which the stress shadows and exclusion zones surround
                for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                {
                    // Get a handle to fracture set I
                    LayerBoundFractureSet fsI = LayerBoundFractureSets[fsI_Index];
                    int noDipSetsI = fsI.FractureDipSets.Count;
                    stressShadowWidthImK[fsI_Index] = new double[noDipSetsI];
                    exclusionZoneWidthImKn[fsI_Index] = new double[noDipSetsI][];
                    exclusionZoneVolumeIKn[fsI_Index] = new double[noDipSetsK];

                    // Cache the appropriate azimuthal and strike-slip shear stress shadow multipliers for sets I and K locally
                    double Faa_IK = FaaIJ[fsI_Index, fsK_Index];
                    double Fas_IK = FasIJ[fsI_Index, fsK_Index];
                    double Faa_KI = FaaIJ[fsK_Index, fsI_Index];
                    double Fas_KI = FasIJ[fsK_Index, fsI_Index];

                    // Calculate the mean stress shadow width of a set I fracture seen by fracture set K
                    // To do this we will need to loop through each dip set in fracture set I, calculating the stress shadow volume of each
                    double psiI = 0;
                    double P32totalI = 0;
                    for (int dipSetIndexIm = 0; dipSetIndexIm < noDipSetsI; dipSetIndexIm++)
                    {
                        FractureDipSet dipSetIm = fsI.FractureDipSets[dipSetIndexIm];
                        exclusionZoneWidthImKn[fsI_Index][dipSetIndexIm] = new double[noDipSetsK];

                        // Get the MFP32 and the azimuthal and strike-slip shear components of the mean stress shadow width for this dipset Im
                        double P32_dipsetIm = dipSetIm.a_MFP32_total() + dipSetIm.s_MFP32_total();
                        double WaaIm = dipSetIm.Mean_Azimuthal_MF_StressShadowWidth;
                        double WasIm = dipSetIm.Mean_Shear_MF_StressShadowWidth;

                        // Calculate the stress shadow width of a fracture from this dipset Im as seen by fracture set K
                        double W_dipsetIm = (WaaIm * Faa_IK) + (WasIm * Fas_IK);

                        // Calculate the stress shadow volume of this dipset of Im as seen by fracture set K, and add it to the total for set I
                        double psi_dipsetI = W_dipsetIm * P32_dipsetIm;
                        psiI += psi_dipsetI;
                        P32totalI += P32_dipsetIm;

                        // Write the stress shadow width for dipset Im to the appropriate array
                        stressShadowWidthImK[fsI_Index][dipSetIndexIm] = W_dipsetIm;
                    }
                    if (psiI < 0)
                        psiI = 0;
                    if (psiI > 1)
                        psiI = 1;
                    double W_IK = (P32totalI > 0 ? psiI / P32totalI : 0);

                    // Write the stress shadow volume for set I to the appropriate array
                    stressShadowVolumeIK[fsI_Index] = psiI;

                    // Orientation multiplier to project the width of a stress shadow around around a set I fracture onto the azimuth of a set K fracture
                    double cosIK = Math.Abs(VectorXYZ.Cos_trim(fsI.Strike - fsK.Strike));

                    // Loop through each dipset in fracture set K
                    for (int dipSetIndexKn = 0; dipSetIndexKn < noDipSetsK; dipSetIndexKn++)
                    {
                        FractureDipSet dipSetKn = fsK.FractureDipSets[dipSetIndexKn];

                        // Get the azimuthal and strike-slip shear components of the mean stress shadow width for dipset Kn
                        double WaaKn = dipSetKn.Mean_Azimuthal_MF_StressShadowWidth;
                        double WasKn = dipSetKn.Mean_Shear_MF_StressShadowWidth;

                        // Calculate the stress shadow width of a fracture from dipset Kn as seen by fracture set I
                        double W_KnI = (WaaKn * Faa_KI) + (WasKn * Fas_KI);

                        // Calculate the maximum exclusion zone width around each fracture dipset Im, as seen by fracture sipset Kn
                        for (int dipSetIndexIm = 0; dipSetIndexIm < noDipSetsI; dipSetIndexIm++)
                        {
                            exclusionZoneWidthImKn[fsI_Index][dipSetIndexIm][dipSetIndexKn] = stressShadowWidthImK[fsI_Index][dipSetIndexIm] + (W_KnI * cosIK);
                        }

                        // Calculate the mean exclusion zone width around a set I fracture, as seen by fracture dipset Kn
                        double exclusionZoneWidthIKn = W_IK + (W_KnI * cosIK);

                        // Calculate the clear zone volume of set I, as seen by fracture dipset Kn
                        double clearZoneVolumeIKn;
                        if (fsI_Index == fsK_Index)
                            clearZoneVolumeIKn = fsI.getClearZoneVolume(W_KnI);
                        else
                            clearZoneVolumeIKn = fsI.getInverseProximityZoneVolume(exclusionZoneWidthIKn);

                        // Write the exclusion zone volume to the appropriate array
                        exclusionZoneVolumeIKn[fsI_Index][dipSetIndexKn] = 1 - clearZoneVolumeIKn;
                    }
                }

                // Now we can adjust the stress shadow volumes around each set I for overlaps with other sets J before summing them to get the total stress shadow volume seen by set K
                double totalStressShadowVolumeK = 0;
                for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                {
                    // Calculate the proportional overlap of stress shadows around fracture set I with every other dipset Jm
                    double stressShadowIOverlap = 0;
                    // Fracture dipset Jm represents the terminating fracture
                    for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                    {
                        if (fsJ_Index == fsI_Index)
                            continue;
                        int noDipSetsJ = LayerBoundFractureSets[fsJ_Index].FractureDipSets.Count;
                        for (int dipSetIndexJm = 0; dipSetIndexJm < noDipSetsJ; dipSetIndexJm++)
                        {
                            // The volume of stress shadow overlap with dipset Jm is given by the orientation-adjusted proportion of set I fracture tips terminating against dipset Jm,
                            // times half the width of stress shadows around dipset Jm as seen by fracture set K (the factor 0.5 is included since fracture I will only overlap the stress shadow on one side of fracture J)
                            stressShadowIOverlap += tipOverlaps[fsI_Index, fsJ_Index][dipSetIndexJm] * 0.5 * stressShadowWidthImK[fsJ_Index][dipSetIndexJm];
                        }
                    }
                    if (stressShadowIOverlap > 1)
                        stressShadowIOverlap = 1;

                    // Add the stress shadow volume of set I, minus tip overlaps, to the total stress shadow volume seen by fracture set K
                    totalStressShadowVolumeK += stressShadowVolumeIK[fsI_Index] * (1 - stressShadowIOverlap);
                }

                // Unlike the stress shadow volumes, the exclusion zone volumes must be calculated separately for each dipset Kn
                for (int dipSetIndexKn = 0; dipSetIndexKn < noDipSetsK; dipSetIndexKn++)
                {
                    // Now we can adjust the exclusion zone volumes around each set I for overlaps with other sets J before summing them to get the total exclusion zone volume seen by dipset Kn
                    double totalExclusionZoneVolumeKn = 0;
                    for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                    {
                        // Calculate the proportional overlap of exclusion zones around fracture set I with every other dipset Jm
                        double exclusionZoneIOverlap = 0;
                        // Fracture dipset Jm represents the terminating fracture
                        for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                        {
                            if (fsJ_Index == fsI_Index)
                                continue;
                            int noDipSetsJ = LayerBoundFractureSets[fsJ_Index].FractureDipSets.Count;
                            for (int dipSetIndexJm = 0; dipSetIndexJm < noDipSetsJ; dipSetIndexJm++)
                            {
                                // The volume of exclusion zone overlap with dipset Jm is given by the orientation-adjusted proportion of set I fracture tips terminating against dipset Jm,
                                // times half the width of exclusion zone around dipset Jm as seen by dipset Kn (the factor 0.5 is included since fracture I will only overlap the stress shadow on one side of fracture J)
                                exclusionZoneIOverlap += tipOverlaps[fsI_Index, fsJ_Index][dipSetIndexJm] * 0.5 * exclusionZoneWidthImKn[fsJ_Index][dipSetIndexJm][dipSetIndexKn];
                            }
                        }
                        if (exclusionZoneIOverlap > 1)
                            exclusionZoneIOverlap = 1;

                        // Add the stress shadow volume of set I, minus tip overlaps, to the total stress shadow volume seen by fracture set K
                        totalExclusionZoneVolumeKn += exclusionZoneVolumeIKn[fsI_Index][dipSetIndexKn] * (1 - exclusionZoneIOverlap);
                    }

                    // Finally we can set the total stress shadow and exclusion zone volumes for dipset Kn
                    fsK.FractureDipSets[dipSetIndexKn].setOtherFSExclusionZoneData(totalStressShadowVolumeK, totalExclusionZoneVolumeKn);
                } // End loop through each dipset Kn

            } // End loop through each fracture set K
        }
#if CHECKCZV
        /// <summary>
        /// Calculate the inverse stress shadow and clear zone volume for each unconfined fracture set J due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// Valid for isotropic fracture networks as it takes account of multiple fractures overlapping but does not account for the influence of a primary fracture set on the distribution of secondary sets
        /// </summary>
        private void setCrossUFSStressShadows()
        {
            // Fracture set K represents the fractures to which the stress shadows will apply
            // The stress shadow and exclusion zone widths will therefore be as seen by fracture set K
            for (int ufsK_Index = 0; ufsK_Index < NoUnconfinedFractureSets; ufsK_Index++)
            {
                // Get a handle to fracture set K
                UnconfinedFractureSet ufsK = UnconfinedFractureSets[ufsK_Index];

                // We must calculate:
                // - the total stress shadow volume of every set I as seen by set K,
                // - the total exclusion zone volume of every set I as seen by a nucleating fracture in set K
                // To do this we will multiply the inverse stress shadow volumes and clear zone volumes of each set I
                double inverseStressShadowVolumeK = 1;
                double clearZoneVolumeK = 1;

                // Fracture set I represents the fractures which the stress shadows and exclusion zones surround
                for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
                {
                    // Get a handle to fracture set I
                    UnconfinedFractureSet ufsI = UnconfinedFractureSets[ufsI_Index];

                    // Cache the appropriate stress shadow multipliers for sets I and K locally
                    double UCFW_IK = UCFW_IJ[ufsI_Index, ufsK_Index];

                    // Calculate the total stress shadow volume of set I and the mean stress shadow width of a set I fracture, as seen by fracture set K
                    // To do this we will need to loop through each dip set in fracture set I, calculating the stress shadow volume of each
                    double inverseStressShadowVolumeIK = 1 - (UCFW_IK * ufsI.getStressShadowVolume());
                    double ClearZoneVolume_IK = ufsI.getStressShadowClearZoneVolume(ufsK.MinimumFractureRadius, ufsK.MinimumFractureRadius, UCFW_IK);
                    inverseStressShadowVolumeK *= inverseStressShadowVolumeIK;
                    clearZoneVolumeK *= ClearZoneVolume_IK;
                }

                // Set the total stress shadow and exclusion zone volumes for each dipset Kn
                ufsK.setOtherFSExclusionZoneData(inverseStressShadowVolumeK, clearZoneVolumeK);

            } // End loop through each fracture set K
        }
#else
        /// <summary>
        /// Calculate the inverse stress shadow seen by zero-radius fractures from each unconfined fracture set K due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// Valid for isotropic fracture networks as it takes account of multiple fractures overlapping but does not account for the influence of a primary fracture set on the distribution of secondary sets
        /// </summary>
        private void setCrossUFSStressShadows()
        {
            // Fracture set K represents the fractures to which the stress shadows will apply
            // The stress shadow widths will therefore be as seen by fracture set K
            for (int ufsK_Index = 0; ufsK_Index < NoUnconfinedFractureSets; ufsK_Index++)
            {
                // Get a handle to fracture set K
                UnconfinedFractureSet ufsK = UnconfinedFractureSets[ufsK_Index];

                // We must calculate the total stress shadow volume of every set I as seen by set K,
                // To do this we will multiply the inverse stress shadow volume of each set I
                double inverseStressShadowVolumeK = 1;

                // Fracture set I represents the fractures which the stress shadows surround
                for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
                {
                    // Get a handle to fracture set I
                    UnconfinedFractureSet ufsI = UnconfinedFractureSets[ufsI_Index];

                    // Cache the appropriate stress shadow multipliers for sets I and K locally
                    double UCFW_IK = UCFW_IJ[ufsI_Index, ufsK_Index];

                    // Calculate the total stress shadow volume of set I, as seen by a zero-radius fracture from set K
                    // To do this we will need to loop through each dip set in fracture set I, calculating the stress shadow volume of each
                    double inverseStressShadowVolumeIK = 1 - (ufsI.getStressShadowVolume() * UCFW_IK);
                    inverseStressShadowVolumeK *= inverseStressShadowVolumeIK;
                }

                // Set the inverse stress shadow volumes for unconfined fracture set K
                ufsK.setOtherFSExclusionZoneData(inverseStressShadowVolumeK);

            } // End loop through each fracture set K
        }
#endif
        /// <summary>
        /// Get the mean width of the exclusion zone around fractures from set I, as seen by a propagating fracture from dipset J, during a specified timestep
        /// </summary>
        /// <param name="fsI">Fracture set of the intersecting fractures</param>
        /// <param name="fsJ">Fracture set containing the propagating fracture</param>
        /// <param name="dipSetJ">Fracture dipset containing the propagating fracture</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating; set to -1 to use current data</param>
        /// <returns></returns>
        public double getCrossFSExclusionZoneWidth(LayerBoundFractureSet fsI, LayerBoundFractureSet fsJ, FractureDipSet dipSetJ, int Timestep_M)
        {
            // Get the index number of timestep M-1, so we can retrieve MFP32 values for the end of the previous timestep
            // If a valid timestep number is not supplied, use the current data
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the index numbers of the two sets; if we cannot find them, return NaN
            int fsI_Index = -1;
            int fsJ_Index = -1;
            for (int fs_Index = 0; fs_Index < NoLayerBoundFractureSets; fs_Index++)
            {
                if (fsI == LayerBoundFractureSets[fs_Index])
                    fsI_Index = fs_Index;
                if (fsJ == LayerBoundFractureSets[fs_Index])
                    fsJ_Index = fs_Index;
            }
            if ((fsI_Index < 0) || (fsJ_Index < 0))
                return double.NaN;

            // Cache the appropriate azimuthal and strike-slip shear stress shadow multipliers for sets I and J locally
            double Faa_IJ = FaaIJ[fsI_Index, fsJ_Index];
            double Fas_IJ = FasIJ[fsI_Index, fsJ_Index];
            double Faa_JI = FaaIJ[fsJ_Index, fsI_Index];
            double Fas_JI = FasIJ[fsJ_Index, fsI_Index];

            // Calculate the mean stress shadow width of a set I fracture seen by a set J fracture
            // To do this we will need to loop through each dip set in fracture set I, calculating the stress shadow volume of each
            int NoDipsetsI = fsI.FractureDipSets.Count;
            double stressShadowVolumeIJ = 0;
            double P32totalI = 0;
            for (int dipSetIndexI = 0; dipSetIndexI < NoDipsetsI; dipSetIndexI++)
            {
                FractureDipSet dipSetI = fsI.FractureDipSets[dipSetIndexI];
                // Get the MFP32 and the azimuthal and strike-slip shear components of the mean stress shadow width for this set I dip set
                double P32_total, WaaI, WasI;
                if (UseCurrentData)
                {
                    P32_total = dipSetI.a_MFP32_total() + dipSetI.s_MFP32_total();
                    WaaI = dipSetI.Mean_Azimuthal_MF_StressShadowWidth;
                    WasI = dipSetI.Mean_Shear_MF_StressShadowWidth;
                }
                else
                {
                    // NB When calculating using current data, stress shadow widths have already been recalculated for this timestep but P32 values have not
                    // For consistency, we will therefore take P32 values from the timestep M-1 but stress shadow widths from timestep M
                    P32_total = dipSetI.getTotalMFP32(Timestep_Mminus1);
                    WaaI = dipSetI.getMeanAzimuthalStressShadowWidth(Timestep_M);
                    WasI = dipSetI.getMeanShearStressShadowWidth(Timestep_M);
                }
                // Calculate the stress shadow volume of this dip set, as seen by fracture set J, and add it to the total for set I
                double WIJ = (WaaI * Faa_IJ) + (WasI * Fas_IJ);
                stressShadowVolumeIJ += (WIJ * P32_total);
                P32totalI += P32_total;
            }
            if (stressShadowVolumeIJ < 0)
                stressShadowVolumeIJ = 0;
            if (stressShadowVolumeIJ > 1)
                stressShadowVolumeIJ = 1;
            double MeanWIJ = (P32totalI > 0 ? stressShadowVolumeIJ / P32totalI : 0);

            // Orientation multiplier to project the width of a stress shadow around around a set J fracture onto the azimuth of a set I fracture
            double cosIJ = Math.Abs(VectorXYZ.Cos_trim(fsI.Strike - fsJ.Strike));

            // Calculate the stress shadow width of a fracture from this dipset of J seen by a set I fracture
            // Get the azimuthal and strike-slip shear components of the mean stress shadow width for this set J dip set
            double WaaJ, WasJ;
            if (UseCurrentData)
            {
                WaaJ = dipSetJ.Mean_Azimuthal_MF_StressShadowWidth;
                WasJ = dipSetJ.Mean_Shear_MF_StressShadowWidth;
            }
            else
            {
                WaaJ = dipSetJ.getMeanAzimuthalStressShadowWidth(Timestep_M);
                WasJ = dipSetJ.getMeanShearStressShadowWidth(Timestep_M);
            }
            // Calculate the stress shadow width of this set J dip set, as seen by fracture set I
            double MeanWJI = (WaaJ * Faa_JI) + (WasJ * Fas_JI);

            // Calculate the clear zone volume of set I, as seen by this dipset of set J
            double maxEZWidth = MeanWIJ + (MeanWJI * cosIJ);

            return maxEZWidth;
        }
        /// <summary>
        /// Collection of layer-bound fracture sets defined by orientation
        /// </summary>
        public List<LayerBoundFractureSet> LayerBoundFractureSets;
        /// <summary>
        /// Collection of unconfined fracture sets defined by orientation
        /// </summary>
        public List<UnconfinedFractureSet> UnconfinedFractureSets;
        /// <summary>
        /// List of references to all macrofracture segments in all fracture sets in the gridblock in order of nucleation - used to ensure fracture propagation is carried out strictly in order of nucleation
        /// </summary>
        private List<MacrofractureSegmentHolder> MacrofractureSegments;
        /// <summary>
        /// Holder for MacrofractureSegmentIJK objects, which contains their fracture set index number and which can be used to compare them by nucleation time
        /// </summary>
        private class MacrofractureSegmentHolder : IComparable<MacrofractureSegmentHolder>
        {
            /// <summary>
            /// Reference to a MacrofractureSegmentIJK object within a FractureSet
            /// </summary>
            public MacrofractureSegmentIJK Segment;
            /// <summary>
            /// Index of the fracture set holding the MacrofractureSegmentIJK object
            /// </summary>
            public int FractureSetIndex;

            // Control and implementation functions
            /// <summary>
            /// Compare MacrofractureSegmentHolder objects based on nucleation time
            /// </summary>
            /// <param name="that">MacrofractureSegmentHolder object to compare with</param>
            /// <returns>Positive if this is the most recent MacrofractureSegmentIJK to nucleate, negative if that is the most recent MacrofractureSegmentIJK to nucleate, zero if they have the same nucleation time</returns>
            public int CompareTo(MacrofractureSegmentHolder that)
            {
                return this.Segment.getNucleationTime().CompareTo(that.Segment.getNucleationTime());
            }

            // Constructors
            /// <summary>
            /// Constructor: specify MacrofractureSegmentIJK object and the orientation of the FractureSet object holding it
            /// </summary>
            /// <param name="Segment_in">Reference to a MacrofractureSegmentIJK object within a FractureSet</param>
            /// <param name="FractureSetIndex_in">Orientation of fracture set holding the MacrofractureSegmentIJK object </param>
            public MacrofractureSegmentHolder(MacrofractureSegmentIJK Segment_in, int FractureSetIndex_in)
            {
                Segment = Segment_in;
                FractureSetIndex = FractureSetIndex_in;
            }
        }
        /// <summary>
        /// List of references to all unconfined fracture ray segments in all unconfined fracture sets in the gridblock in order of nucleation - used to ensure fracture propagation is carried out strictly in order of nucleation
        /// </summary>
        private List<UnconfinedFractureRaySegmentHolder> UnconfinedFractureRaySegments;
        /// <summary>
        /// Holder for UnconfinedFractureRaySegment objects, which contains their fracture set index number and which can be used to compare them by nucleation time
        /// </summary>
        private class UnconfinedFractureRaySegmentHolder : IComparable<UnconfinedFractureRaySegmentHolder>
        {
            /// <summary>
            /// Reference to an UnconfinedFractureRaySegment object within an UnconfinedFractureSet
            /// </summary>
            public UnconfinedFractureRaySegment Segment;
            /// <summary>
            /// Index of the unconfined fracture set holding the UnconfinedFractureRaySegment object
            /// </summary>
            public int FractureSetIndex;

            // Control and implementation functions
            /// <summary>
            /// Compare UnconfinedFractureRaySegmentHolder objects based on effective ray length
            /// NB Comparison result is inverted so that the datapoints will be sorted in reverse order of effective ray length (largest to smallest)
            /// </summary>
            /// <param name="that">UnconfinedFractureRaySegmentHolder object to compare with</param>
            /// <returns>Negative if the ray which this segment belongs to has the greatest effective length, positive if the ray which that segment belongs to has the greatest effective length, zero if the rays have the same effective length</returns>
            public int CompareTo(UnconfinedFractureRaySegmentHolder that)
            {
                return -this.Segment.EffectiveRayLength.CompareTo(that.Segment.EffectiveRayLength);
            }

            // Constructors
            /// <summary>
            /// Constructor: specify UnconfinedFractureRaySegment object and the index number of the UnconfinedFractureSet object holding it
            /// </summary>
            /// <param name="Segment_in">Reference to a UnconfinedFractureRaySegment object within an UnconfinedFractureSet</param>
            /// <param name="FractureSetIndex_in">Orientation of unconfined fracture set holding the UnconfinedFractureRaySegment object </param>
            public UnconfinedFractureRaySegmentHolder(UnconfinedFractureRaySegment Segment_in, int FractureSetIndex_in)
            {
                Segment = Segment_in;
                FractureSetIndex = FractureSetIndex_in;
            }
        }

        // Indexes for the calculation timesteps
        /// <summary>
        /// List of end times for each timestep - populated when running the CalculateFractureData function
        /// </summary>
        public List<double> TimestepEndTimes { get; private set; }
        /// <summary>
        /// Index number of the last timestep in the model
        /// </summary>
        public int FinalTimestep { get { return TimestepEndTimes.Count - 1; } }
        /// <summary>
        /// End time of the last timestep in the model
        /// </summary>
        public double FinalTime { get { return TimestepEndTimes[FinalTimestep]; } }
        /// <summary>
        /// Index number of the current timestep in the implicit fracture calculation
        /// </summary>
        public int CurrentImplicitTimestep { get; private set; }
        /// <summary>
        /// End time of the current timestep in the implicit fracture calculation
        /// </summary>
        public double CurrentImplicitTime { get { return TimestepEndTimes[CurrentImplicitTimestep]; } }
        /// <summary>
        /// Index number of the current timestep in the explicit DFN generation
        /// </summary>
        private int currentExplicitTimestep;
        /// <summary>
        /// Index number of the current timestep in the explicit DFN generation
        /// </summary>
        public int CurrentExplicitTimestep { get { return currentExplicitTimestep; } private set { if (value < 0) value = 0; if (value > FinalTimestep) value = FinalTimestep; currentExplicitTimestep = value; } }
        /// <summary>
        /// End time of the current timestep in the explicit DFN generation
        /// </summary>
        public double CurrentExplicitTime { get { return TimestepEndTimes[CurrentExplicitTimestep]; } }
        /// <summary>
        /// Get the time at which the final fracture set becomes deactivated
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if the fracture set was never active: if true, will return Nan; if false, will return 0</param>
        /// <returns>Deactivation time of final fracture set; will return zero or NaN if none of the fracture sets were ever active</returns>
        public double getFinalActiveTime(bool ReturnNanForUndefined)
        {
            // Get time units and unit conversion modifier for output time data if not in SI units
            double timeUnits_Modifier = PropControl.getTimeUnitsModifier();

            // Loop through the timesteps in reverse order
            for (int TimestepNo = FinalTimestep; TimestepNo > 0; TimestepNo--)
            {
                // Loop through each layer-bound fracture dipset
                foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                {
                    foreach (FractureDipSet fds in fs.FractureDipSets)
                    {
                        // Check if the dipset is active (or residual active); if so return the end time of the current timestep
                        FractureEvolutionStage CurrentStage = fds.getEvolutionStage(TimestepNo);
                        if ((CurrentStage == FractureEvolutionStage.Growing) || (CurrentStage == FractureEvolutionStage.ResidualActivity))
                            return TimestepEndTimes[TimestepNo] / timeUnits_Modifier;
                    }
                }
                // Loop through each uncoinfined fracture set
                foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                {
                    // Check if the set is active (or residual active); if so return the end time of the current timestep
                    FractureEvolutionStage CurrentStage = ufs.getEvolutionStage(TimestepNo);
                    if ((CurrentStage == FractureEvolutionStage.Growing) || (CurrentStage == FractureEvolutionStage.ResidualActivity))
                        return TimestepEndTimes[TimestepNo] / timeUnits_Modifier;
                }
            }

            // If none of the fracture sets were ever active, return 0 or NaN as appropriate
            if (ReturnNanForUndefined)
                return double.NaN;
            else
                return 0;
        }
        /// <summary>
        /// Get the index number of the timestep corresponding to a specified time
        /// </summary>
        /// <param name="time">Specified time</param>
        /// <returns></returns>
        public int getTimestepIndex(double time)
        {
            int TimestepNo = CurrentExplicitTimestep;

            if (time > FinalTime)
            {
                TimestepNo = FinalTimestep;
            }
            else if (time > CurrentExplicitTime)
            {
                while (time > TimestepEndTimes[TimestepNo])
                    TimestepNo++;
            }
            else if (time <= TimestepEndTimes[0])
            {
                TimestepNo = 0;
            }
            else
            {
                while (time <= TimestepEndTimes[TimestepNo - 1])
                    TimestepNo--;
            }

            return TimestepNo;
        }
        /// <summary>
        /// Maximum radius that a microfracture can reach before nucleating a macrofracture; normally assumed to be half of the layer thickness, unless a fracture nucleation position within the layer has been specified
        /// </summary>
        public double MaximumMicrofractureRadius { get { double fractureNucleationPosition = PropControl.FractureNucleationPosition; return ThicknessAtDeformation * (0.5 + (fractureNucleationPosition >= 0 ? Math.Abs(fractureNucleationPosition - 0.5) : 0)); } }
        /// <summary>
        /// Component related to the maximum microfracture radius rmax, included in Cum_hGamma to represent the initial population of seed macrofractures: ln(rmax) for b=2; rmax^(1/beta) for b!=2
        /// </summary>
        public double Initial_uF_factor { get { return (MechProps.GetbType() == bType.Equals2 ? Math.Log(MaximumMicrofractureRadius) : Math.Pow(MaximumMicrofractureRadius, 1 / MechProps.beta)); } }

        // Functions to return fracture density and porosity
        /// <summary>
        /// Get the current P32 density of all microfractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double MicrofractureDensity_P32()
        {
            double uF_P32_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                uF_P32_value += fs.combined_T_uFP32_total();
            return uF_P32_value;
        }
        /// <summary>
        /// Get the current P32 density of all layer-bound fractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double LayerBoundFractureDensity_P32()
        {
            double MF_P32_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                MF_P32_value += fs.combined_T_MFP32_total();
            return MF_P32_value;
        }
        /// <summary>
        /// Get the current P32 density of all unconfined fractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double UnconfinedFractureDensity_P32()
        {
            double UCF_P32_value = 0;
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                UCF_P32_value += ufs.getTotalUCFP32();
            return UCF_P32_value;
        }
        /// <summary>
        /// Get the current P32 density of all fractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double TotalFractureDensity_P32()
        {
            double P32_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                P32_value += (fs.combined_T_uFP32_total() + fs.combined_T_MFP32_total());
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                P32_value += ufs.getTotalUCFP32();
            return P32_value;
        }
        /// <summary>
        /// Get the current porosity of all microfractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double MicrofracturePorosity()
        {
            double uF_Porosity_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                uF_Porosity_value += fs.combined_uF_Porosity();
            return uF_Porosity_value;
        }
        /// <summary>
        /// Get the current porosity of all layer-bound fractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double LayerBoundFracturePorosity()
        {
            double MF_Porosity_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                MF_Porosity_value += fs.combined_MF_Porosity();
            return MF_Porosity_value;
        }
        /// <summary>
        /// Get the current porosity of all unconfined fractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double UnconfinedFracturePorosity()
        {
            double UCF_Porosity_value = 0;
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                UCF_Porosity_value += ufs.Total_UCF_Porosity();
            return UCF_Porosity_value;
        }
        /// <summary>
        /// Get the current porosity of all fractures in the gridblock
        /// </summary>
        /// <returns></returns>
        public double TotalFracturePorosity()
        {
            double Porosity_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                Porosity_value += (fs.combined_uF_Porosity() + fs.combined_MF_Porosity());
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                Porosity_value += ufs.Total_UCF_Porosity();
            return Porosity_value;
        }
        /// <summary>
        /// Get the P32 density of all microfractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double MicrofractureDensity_P32(int Timestep_M)
        {
            double uF_P32_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    uF_P32_value += fds.getTotaluFP32(Timestep_M);
            return uF_P32_value;
        }
        /// <summary>
        /// Get the P32 density of all layer-bound fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double LayerBoundFractureDensity_P32(int Timestep_M)
        {
            double MF_P32_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    MF_P32_value += fds.getTotalMFP32(Timestep_M);
            return MF_P32_value;
        }
        /// <summary>
        /// Get the P32 density of all unconfined fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double UnconfinedFractureDensity_P32(int Timestep_M)
        {
            double UCF_P32_value = 0;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                UCF_P32_value += ufs.getTotalUCFP32(Timestep_M);
            return UCF_P32_value;
        }
        /// <summary>
        /// Get the P32 density of all fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double TotalFractureDensity_P32(int Timestep_M)
        {
            double P32_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    P32_value += (fds.getTotaluFP32(Timestep_M) + fds.getTotalMFP32(Timestep_M));
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                P32_value += ufs.getTotalUCFP32(Timestep_M);
            return P32_value;
        }
        /// <summary>
        /// Get the porosity of all microfractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double MicrofracturePorosity(int Timestep_M)
        {
            double uF_Porosity_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    uF_Porosity_value += fds.getTotaluFPorosity(Timestep_M);
            return uF_Porosity_value;
        }
        /// <summary>
        /// Get the porosity of all layer-bound fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double LayerBoundFracturePorosity(int Timestep_M)
        {
            double MF_Porosity_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    MF_Porosity_value += fds.getTotalMFPorosity(Timestep_M);
            return MF_Porosity_value;
        }
        /// <summary>
        /// Get the porosity of all unconfined fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double UnconfinedFracturePorosity(int Timestep_M)
        {
            double UCF_Porosity_value = 0;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                UCF_Porosity_value += ufs.getTotalUCFPorosity(Timestep_M);
            return UCF_Porosity_value;
        }
        /// <summary>
        /// Get the porosity of all fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public double TotalFracturePorosity(int Timestep_M)
        {
            double Porosity_value = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                    Porosity_value += (fds.getTotaluFPorosity(Timestep_M) + fds.getTotalMFPorosity(Timestep_M));
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                Porosity_value += ufs.getTotalUCFPorosity(Timestep_M);
            return Porosity_value;
        }

        // Functions to return fracture anisotropy and connectivity indices
        /// <summary>
        /// Layer-bound fracture anisotropy index based on P32: (MaxP32 - MinP32) / (MaxP32 + MinP32)
        /// </summary>
        /// <param name="FindMinMaxSets">If true, will find the two layer-bound fracture sets with the largest and smallest P32 values; if false, will use the sets perpendicular to HMin and HMax respectively</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no layer-bound fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>(MaxP32 - MinP32) / (MaxP32 + MinP32)</returns>
        public double P32AnisotropyIndex(bool FindMinMaxSets, bool ReturnNanForUndefined)
        {
            // If there is only one layer-bound fracture set, the anisotropy index will be 1 (completely anisotropic)
            if (NoLayerBoundFractureSets < 2)
                return 1;

            // Otherwise we will need to calculate the ratio of P32 values of the two specified layer-bound fracture sets
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            int hmin_index = 0;
            double Max_P32 = LayerBoundFractureSets[hmin_index].combined_T_MFP32_total() + LayerBoundFractureSets[hmin_index].combined_T_uFP32_total();
            double Min_P32 = Max_P32;
            // If required we will find and compare the layer-bound fracture sets with the highest and lowest P32 values
            if (FindMinMaxSets)
                for (int fs_Index = 1; fs_Index < NoLayerBoundFractureSets; fs_Index++)
                {
                    double fs_P32 = LayerBoundFractureSets[fs_Index].combined_T_MFP32_total() + LayerBoundFractureSets[fs_Index].combined_T_uFP32_total();
                    if (fs_P32 > Max_P32)
                        Max_P32 = fs_P32;
                    if (fs_P32 < Min_P32)
                        Min_P32 = fs_P32;
                }
            // Otherwise we will just compare the layer-bound fracture sets orthogonal to ehmin and ehmax
            else
            {
                int hmax_index = NoLayerBoundFractureSets / 2;
                Min_P32 = LayerBoundFractureSets[hmax_index].combined_T_MFP32_total() + LayerBoundFractureSets[hmax_index].combined_T_uFP32_total();
            }

            double Combined_P32 = Max_P32 + Min_P32;
            return (Combined_P32 > 0 ? (Max_P32 - Min_P32) / Combined_P32 : undefinedReturn);
        }
        /// <summary>
        /// Layer-bound fracture anisotropy index based on P33: (MaxP33 - MinP33) / (MaxP33 + MinP33)
        /// </summary>
        /// <param name="FindMinMaxSets">If true, will find the two layer-bound fracture sets with the largest and smallest P32 values; if false, will use the sets perpendicular to HMin and HMax respectively</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no layer-bound fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>(MaxP33 - MinP33) / (MaxP33 + MinP33)</returns>
        public double P33AnisotropyIndex(bool FindMinMaxSets, bool ReturnNanForUndefined)
        {
            // If there is only one layer-bound fracture set, the anisotropy index will be 1 (completely anisotropic)
            if (NoLayerBoundFractureSets < 2)
                return 1;

            // Otherwise we will need to calculate the ratio of P33 values of the two specified layer-bound fracture sets
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            int hmin_index = 0;
            double Max_P33 = LayerBoundFractureSets[hmin_index].combined_T_MFP33_total() + LayerBoundFractureSets[hmin_index].combined_T_uFP33_total();
            double Min_P33 = Max_P33;
            // If required we will find and compare the layer-bound fracture sets with the highest and lowest P33 values
            if (FindMinMaxSets)
                for (int fs_Index = 1; fs_Index < NoLayerBoundFractureSets; fs_Index++)
                {
                    double fs_P33 = LayerBoundFractureSets[fs_Index].combined_T_MFP33_total() + LayerBoundFractureSets[fs_Index].combined_T_uFP33_total();
                    if (fs_P33 > Max_P33)
                        Max_P33 = fs_P33;
                    if (fs_P33 < Min_P33)
                        Min_P33 = fs_P33;
                }
            // Otherwise we will just compare the layer-bound fracture sets orthogonal to ehmin and ehmax
            else
            {
                int hmax_index = NoLayerBoundFractureSets / 2;
                Min_P33 = LayerBoundFractureSets[hmax_index].combined_T_MFP33_total() + LayerBoundFractureSets[hmax_index].combined_T_uFP33_total();
            }

            double Combined_P33 = Max_P33 + Min_P33;
            return (Combined_P33 > 0 ? (Max_P33 - Min_P33) / Combined_P33 : undefinedReturn);
        }
        /// <summary>
        /// Layer-bound fracture anisotropy index based on fracture porosity: (HMinPorosity - HMaxPorosity) / (HMinPorosity + HMaxPorosity)
        /// </summary>
        /// <param name="FindMinMaxSets">If true, will find the two layer-bound fracture sets with the largest and smallest P32 values; if false, will use the sets perpendicular to HMin and HMax respectively</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no layer-bound fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>(HMinPorosity - HMaxPorosity) / (HMinPorosity + HMaxPorosity)</returns>
        public double FracturePorosityAnisotropyIndex(bool FindMinMaxSets, bool ReturnNanForUndefined)
        {
            // If there is only one layer-bound fracture set, the anisotropy index will be 1 (completely anisotropic)
            if (NoLayerBoundFractureSets < 2)
                return 1;

            // Otherwise we will need to calculate the ratio of porosity values of the two specified layer-bound fracture sets
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            int hmin_index = 0;
            double Max_Porosity = LayerBoundFractureSets[hmin_index].combined_MF_Porosity() + LayerBoundFractureSets[hmin_index].combined_uF_Porosity();
            double Min_Porosity = Max_Porosity;
            // If required we will find and compare the layer-bound fracture sets with the highest and lowest porosity values
            if (FindMinMaxSets)
                for (int fs_Index = 1; fs_Index < NoLayerBoundFractureSets; fs_Index++)
                {
                    double fs_Porosity = LayerBoundFractureSets[fs_Index].combined_MF_Porosity() + LayerBoundFractureSets[fs_Index].combined_uF_Porosity();
                    if (fs_Porosity > Max_Porosity)
                        Max_Porosity = fs_Porosity;
                    if (fs_Porosity < Min_Porosity)
                        Min_Porosity = fs_Porosity;
                }
            // Otherwise we will just compare the layer-bound fracture sets orthogonal to ehmin and ehmax
            else
            {
                int hmax_index = NoLayerBoundFractureSets / 2;
                Min_Porosity = LayerBoundFractureSets[hmax_index].combined_MF_Porosity() + LayerBoundFractureSets[hmax_index].combined_uF_Porosity();
            }

            double Combined_Porosity = Max_Porosity + Min_Porosity;
            return (Combined_Porosity > 0 ? (Max_Porosity - Min_Porosity) / Combined_Porosity : undefinedReturn);
        }
        /// <summary>
        /// Proportion of unconnected macrofracture tips - i.e. active macrofracture tips
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no macrofractures: if true, will return Nan; if false, will return 1</param>
        /// <returns>Ratio of a_MFP30_total to T_MFP30_total</returns>
        public double UnconnectedMFTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 1;
            double TotalUnconnectedTips = 0;
            double TotalAllTips = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                TotalUnconnectedTips += fs.combined_a_MFP30_total();
                TotalAllTips += fs.combined_T_MFP30_total();
            }

            return (TotalAllTips > 0 ? TotalUnconnectedTips / TotalAllTips : undefinedReturn);
        }
        /// <summary>
        /// Proportion of macrofracture tips connected to relay zones - i.e. static macrofracture tips deactivated due to stress shadow interaction
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no macrofractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sII_MFP30_total to T_MFP30_total</returns>
        public double RelayMFTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalRelayTips = 0;
            double TotalAllTips = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                TotalRelayTips += fs.combined_sII_MFP30_total();
                TotalAllTips += fs.combined_T_MFP30_total();
            }

            return (TotalAllTips > 0 ? TotalRelayTips / TotalAllTips : undefinedReturn);
        }
        /// <summary>
        /// Proportion of intersecting macrofracture tips - i.e. static macrofracture tips deactivated due to intersection with orthogonal or oblique fractures
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no macrofractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sIJ_MFP30_total to T_MFP30_total</returns>
        public double IntersectingMFTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalIntersectingTips = 0;
            double TotalAllTips = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                TotalIntersectingTips += fs.combined_sIJ_MFP30_total();
                TotalAllTips += fs.combined_T_MFP30_total();
            }

            return (TotalAllTips > 0 ? TotalIntersectingTips / TotalAllTips : undefinedReturn);
        }
        /// <summary>
        /// Get the mean number of half-macrofractures from other fracture sets that terminate against a half-macrofracture from a specified fracture dipset
        /// </summary>
        /// <param name="FractureSetNo">Index number of the specified fracture set</param>
        /// <param name="DipSetNo">Index number of the specified dipset</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns></returns>
        public double getTerminatingFracturesPerMF(int FractureSetNo, int DipSetNo, bool ReturnNanForUndefined)
        {
            // Check if the sepcified fracture dipset exists - if not return NaN
            if ((FractureSetNo < 0) || (FractureSetNo >= NoLayerBoundFractureSets) || (DipSetNo < 0) || (DipSetNo >= LayerBoundFractureSets[FractureSetNo].FractureDipSets.Count))
                return double.NaN;

            // Set the return value if there are no fractures in the specified dipset
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;

            // Calculate the total number of fractures from all fracture sets I terminating against dipset Jm
            double sIJm_MFP30 = 0;
            for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                sIJm_MFP30 += MFTerminations[fsI_Index, FractureSetNo][DipSetNo];

            // Calculate the total number of fractures in dipset Jm
            FractureDipSet Jm = LayerBoundFractureSets[FractureSetNo].FractureDipSets[DipSetNo];
            double I_MFP30 = Jm.a_MFP30_total() + Jm.sII_MFP30_total() + Jm.sIJ_MFP30_total();

            return (I_MFP30 > 0) ? (sIJm_MFP30 / I_MFP30) : undefinedReturn;
        }
        /// <summary>
        /// Mean number of other macrofractures that each macrofracture is connected to - i.e. total number of connections (intersections or hard-linked relays) divided by total number of macrofractures
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of ((2 * sII_MFP30_total) + (4 * sII_MFP30_total)) / T_MFP30_total</returns>
        public double ConnectionsPerMacrofracture(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalConnections = 0;
            double TotalFractures = 0;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                // Only hard-linked relays will be counted
                if (gd.DFNControl.LinkFracturesInStressShadow)
                    TotalConnections += fs.combined_sII_MFP30_total();
                // Intersections create two fracture connections, one on the terminating fracture and one on the terminated fracture
                TotalConnections += (2 * fs.combined_sIJ_MFP30_total());

                // The total number of macrofractures is half of the total number of half-macrofractures
                TotalFractures += (fs.combined_T_MFP30_total() / 2);
            }

            return (TotalFractures > 0 ? TotalConnections / TotalFractures : undefinedReturn);
        }
        /// <summary>
        /// Mean number of other macrofractures that each macrofracture in the specified fracture dip set is connected to - i.e. total number of connections (intersections or hard-linked relays) divided by total number of macrofractures
        /// </summary>
        /// <param name="FractureSetNo">Index number of the specified fracture set</param>
        /// <param name="DipSetNo">Index number of the specified dipset</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns></returns>
        public double ConnectionsPerMacrofracture(int FractureSetNo, int DipSetNo, bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalConnections = 0;
            LayerBoundFractureSet fs = LayerBoundFractureSets[FractureSetNo];
            double TotalFractures = fs.combined_T_MFP30_total() / 2;

            // Calculate the number of connections at the fracture tips
            // Only hard-linked relays will be counted
            if (gd.DFNControl.LinkFracturesInStressShadow)
                TotalConnections += fs.combined_sII_MFP30_total();
            TotalConnections += fs.combined_sIJ_MFP30_total();
            double connectionsPerFracture = TotalFractures > 0 ? TotalConnections / TotalFractures : undefinedReturn;

            // We must double the mean number of terminating fractures as this is calculated per half-macrofracture
            connectionsPerFracture += (2 * getTerminatingFracturesPerMF(FractureSetNo, DipSetNo, ReturnNanForUndefined));

            return connectionsPerFracture;
        }
        /// <summary>
        /// Proportion of unconnected unconfined fracture ray tips - i.e. active unconfined fracture ray tips and fracture rays that have reached the maximum length
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no unconfined fractures: if true, will return Nan; if false, will return 1</param>
        /// <returns>Ratio of a_RP30 + r_RP30 + sRmax_RP30 to total RP30</returns>
        public double UnconnectedUCFTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 1;
            double TotalUnconnectedTips = 0;
            double TotalAllTips = 0;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
            {
                // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
                // we must always take data from the FractureCalculationData list
                TotalUnconnectedTips += ufs.getActive_RP30_M() + ufs.get_RP30_M(RayPropagationStatus.StaticMaxRadius);
                TotalAllTips += ufs.getTotalUCRP30();
            }

            return (TotalAllTips > 0 ? TotalUnconnectedTips / TotalAllTips : undefinedReturn);
        }
        /// <summary>
        /// Proportion of unconfined fracture ray tips connected to relay zones - i.e. static unconfined fracture ray tips deactivated due to stress shadow interaction
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no unconfined fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sII_RP30 to total RP30</returns>
        public double RelayUCFTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalRelayTips = 0;
            double TotalAllTips = 0;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
            {
                // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
                // we must always take data from the FractureCalculationData list
                TotalRelayTips += ufs.get_RP30_M(RayPropagationStatus.StaticStressShadow);
                TotalAllTips += ufs.getTotalUCRP30();
            }

            return (TotalAllTips > 0 ? TotalRelayTips / TotalAllTips : undefinedReturn);
        }
        /// <summary>
        /// Proportion of intersecting unconfined fracture ray tips - i.e. static unconfined fracture rays deactivated due to intersection with orthogonal or oblique fractures
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no unconfined fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sIJ_RP30 to total RP30</returns>
        public double IntersectingUCFTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalIntersectingTips = 0;
            double TotalAllTips = 0;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
            {
                // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
                // we must always take data from the FractureCalculationData list
                TotalIntersectingTips += ufs.get_RP30_M(RayPropagationStatus.StaticIntersection);
                TotalAllTips += ufs.getTotalUCRP30();
            }

            return (TotalAllTips > 0 ? TotalIntersectingTips / TotalAllTips : undefinedReturn);
        }
        /// <summary>
        /// Get the mean number of unconfined fractures from other fracture sets that terminate against an unconfined fracture from a specified fracture set
        /// </summary>
        /// <param name="UnconfinedFractureSetNo">Index number of the specified fracture set</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns></returns>
        public double getTerminatingFracturesPerUCF(int UnconfinedFractureSetNo, bool ReturnNanForUndefined)
        {
            // Check if the sepcified fracture dipset exists - if not return NaN
            if ((UnconfinedFractureSetNo < 0) || (UnconfinedFractureSetNo >= NoUnconfinedFractureSets))
                return double.NaN;

            // Set the return value if there are no fractures in the specified dipset
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;

            // Calculate the total number of fractures from all fracture sets I terminating against set J
            double sIJm_UCFP30 = 0;
            for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
                sIJm_UCFP30 += UCFTerminations[ufsI_Index, UnconfinedFractureSetNo];

            // Calculate the total number of fractures in dipset J
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            UnconfinedFractureSet ufsJ = UnconfinedFractureSets[UnconfinedFractureSetNo];
            double I_UCFP30 = ufsJ.getTotalUCFP30();

            return (I_UCFP30 > 0) ? (sIJm_UCFP30 / I_UCFP30) : undefinedReturn;
        }
        /// <summary>
        /// Mean number of other unconfined fractures that each unconfined fracture is connected to - i.e. total number of connections (intersections or hard-linked relays) divided by total number of unconfined fractures
        /// NB Total number of connections is defined as total number of connecting rays; multiple rays may connect to the same fracture
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no unconfined fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of (sII_UCRP30_total + (2 * ufs.sIJ_UCRP30_total)) / TotalUCFP30</returns>
        public double ConnectionsPerUnconfinedFracture(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalConnections = 0;
            double TotalFractures = 0;
            // Calculate the number of connections at the ray tips
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
            {
                // Only hard-linked relays will be counted
                if (gd.DFNControl.LinkFracturesInStressShadow)
                    TotalConnections += ufs.get_RP30_M(RayPropagationStatus.StaticStressShadow);
                // Intersections create two fracture connections, one on the terminating fracture and one on the terminated fracture
                TotalConnections += (2 * ufs.get_RP30_M(RayPropagationStatus.StaticIntersection));
                TotalFractures += ufs.getTotalUCFP30();
            }

            return (TotalFractures > 0 ? TotalConnections / TotalFractures : undefinedReturn);
        }
        /// <summary>
        /// Mean number of other fractures that each unconfined fracture in the specified fracture set is connected to - i.e. total number of connections (intersections or hard-linked relays) divided by total number of unconfined fractures
        /// NB Total number of connections is defined as total number of connecting rays; multiple rays may connect to the same fracture
        /// </summary>
        /// <param name="UnconfinedFractureSetNo">Index number of the specified unconfined fracture set</param>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of (sII_UCRP30 + ufs.sIJ_UCRP30) / TotalUCFP30</returns>
        public double ConnectionsPerUnconfinedFracture(int UnconfinedFractureSetNo, bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double TotalConnections = 0;
            UnconfinedFractureSet ufs = UnconfinedFractureSets[UnconfinedFractureSetNo];
            double TotalFractures = ufs.getTotalUCFP30();

            // Calculate the number of connections at the ray tips
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            // Only hard-linked relays will be counted
            if (gd.DFNControl.LinkFracturesInStressShadow)
                TotalConnections += ufs.get_RP30_M(RayPropagationStatus.StaticStressShadow);
            TotalConnections += ufs.get_RP30_M(RayPropagationStatus.StaticIntersection);
            double connectionsPerFracture = TotalFractures > 0 ? TotalConnections / TotalFractures : undefinedReturn;

            // Add the mean number of terminating fractures
            connectionsPerFracture += getTerminatingFracturesPerUCF(UnconfinedFractureSetNo, ReturnNanForUndefined);

            return connectionsPerFracture;
        }
        /// <summary>
                 /// Get the current fracture fabric tensor F, as defined by Oda 1983
                 /// </summary>
                 /// <returns></returns>
        public Tensor2S FractureFabricTensor(FractureType fracType)
        {
            return FractureFabricTensor(fracType, -1);
        }
        /// <summary>
        /// Get the fracture fabric tensor F, as defined by Oda 1983, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns></returns>
        public Tensor2S FractureFabricTensor(FractureType fracType, int Timestep_M)
        {
            bool useCurrentDensityData = (Timestep_M < 0);
            Tensor2S FTensor = new Tensor2S();
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                foreach (FractureDipSet fds in fs.FractureDipSets)
                {
                    Tensor2S orientationTensor = fds.NormalVector ^ fds.NormalVector;
                    double densityFactor = 0;
                    if ((fracType == FractureType.Microfractures) || (fracType == FractureType.AllFractures))
                        densityFactor += (3d / 16d) * (useCurrentDensityData ? fds.a_uFP33_total() + fds.s_uFP33_total() : fds.getTotaluFP33(Timestep_M));
                    if ((fracType == FractureType.Microfractures) || (fracType == FractureType.AllFractures))
                        densityFactor += (3d / 16d) * (useCurrentDensityData ? fds.a_MFP33_total() + fds.s_MFP33_total() : fds.getTotalMFP33(Timestep_M));

                    FTensor += (densityFactor * orientationTensor);
                }

            return FTensor;
        }
        /// <summary>
        /// Get the trace and the anistropy of the fracture fabric tensor for all fractures, as defined by Oda (1986), at the end of a specified previous timestep. 
        /// This is used to calculate the fracture network permeability mutliplier, as defined in Oda et al. (1987). 
        /// NB The anisotropy here is calculated from the second invariant of the modified fracture fabric tensor, and is not the same as the anisotropy calculated from fracture density above
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set neagtive to get current values</param>
        /// <param name="F0">Reference parameter for the trace of the fracture fabric tensor</param>
        /// <param name="Anisotropy">Reference parameter for the anisotropy of the fracture fabric tensor</param>
        private void GetF0andAnisotropy(int Timestep_M, out double F0, out double Anisotropy)
        {
            Tensor2S FTensor = FractureFabricTensor(FractureType.AllFractures, Timestep_M);
            F0 = FTensor.Trace;
            double F0over3 = F0 / 3;
            Tensor2S Fdashed = FTensor - new Tensor2S(F0over3, F0over3, F0over3, 0, 0, 0);
            Anisotropy = Math.Sqrt(6 * Math.Abs(Fdashed.SecondInvariant)) / F0;
            return;
        }
        /// <summary>
        /// Function to get the fracture network permeability based on the mean number of conenctions per fracture, defined using data from Oda et al. 1987
        /// </summary>
        /// <param name="connectionsPerFracture">Mean number of connections per fracture</param>
        /// <returns>Fracture network permeability multiplier; does not include the fracture geometry multiplier (typically 1/12)</returns>
        public static double GetNetworkPermeabilityMultiplierFromConnections(double connectionsPerFracture)
        {
            double alpha = 0.17;
            return 1 - Math.Exp(-alpha * connectionsPerFracture);
        }
        /// <summary>
        /// Function to get the fracture network permeability based on the F0 factor and fracture network anisotropy, defined using data from Oda et al. 1987
        /// </summary>
        /// <param name="F0">F0 (trace of fracture matrix F)</param>
        /// <param name="Isotropic">Flag to indicate if network is isotropic (0.04 < AF < 0.16) or anistropic (0.48 < AF < 0.64)</param>
        /// <returns>Fracture network permeability multiplier; does not include the fracture geometry multiplier (typically 1/12)</returns>
        public static double GetNetworkPermeabilityMultiplierFromF0(double F0, bool Isotropic)
        {
            double alpha = Isotropic ? 0.07 : 0.055;
            return 1 - Math.Exp(-alpha * F0);
        }

        // Functions to return fracture permeability tensor
        /// <summary>
        /// Permeability tensor for all current microfractures in the gridblock
        /// </summary>
        /// <returns>Tensor2S object representing microfracture permeability</returns>
        public Tensor2S MicrofracturePermeability()
        {
            return MicrofracturePermeability(-1);
        }
        /// <summary>
        /// Permeability tensor for all current layer-bound macrofractures in the gridblock
        /// </summary>
        /// <returns>Tensor2S object representing macrofracture permeability</returns>
        public Tensor2S MacrofracturePermeability()
        {
            return MacrofracturePermeability(-1);
        }
        /// <summary>
        /// Permeability tensor for all current unconfined fractures in the gridblock - not yet implemented
        /// </summary>
        /// <returns>Tensor2S object representing unconfined fracture permeability</returns>
        public Tensor2S UnconfinedFracturePermeability()
        {
            return UnconfinedFracturePermeability(-1);
        }
        /// <summary>
        /// Permeability tensor for all current fractures in the gridblock
        /// </summary>
        /// <returns>Tensor2S object representing total fracture permeability</returns>
        public Tensor2S TotalFracturePermeability()
        {
            return TotalFracturePermeability(-1);
        }
        /// <summary>
        /// Permeability tensor for all microfractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the microfracture permeability</returns>
        public Tensor2S MicrofracturePermeability(int Timestep_M)
        {
            // Create an empty microfracture permeability tensor
            Tensor2S microfracturePermeability = new Tensor2S();

            // The network connectivity correction reflects the connectivity and size distribution of the entire fracture network
            switch (PropControl.PermeabilityAlgorithm)
            {
                // The Oda 1985 model assumes fractures of infinite size and connectivity, so does not take into account network connectivity
                // There is therefore no network connectivity correction required, and we can just use the sum of the uncorrected microfracture permeability tensors
                case PermeabilityCalculationAlgorithm.Oda1986:
                    {
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                microfracturePermeability += fds.Total_uF_Permeability(Timestep_M);
                    }
                    break;
                // The Oda corrected (1987) algorithm includes a simple multiplier to take account of the connectivity of individual fractures
                case PermeabilityCalculationAlgorithm.OdaCorrected1987:
                    {
                        // First we must get the sum of the uncorrected microfracture permeability tensors
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                microfracturePermeability += fds.Total_uF_Permeability(Timestep_M);

                        // Then we can apply a correction factor based on the trace and anisotropy of the fracture connectivity tensor
                        double f0, anistropy;
                        GetF0andAnisotropy(Timestep_M, out f0, out anistropy);
                        microfracturePermeability = GetNetworkPermeabilityMultiplierFromF0(f0, anistropy < 0.32) * microfracturePermeability;
                    }
                    break;
                // The size and connectivity correction algorithm takes into account flow between fractures along relay segments, fractures from other sets, or through the host rock
                // The host rock permeability is required to calculate the latter
                case PermeabilityCalculationAlgorithm.SizeConnectivityCorrected:
                    {
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                microfracturePermeability += fds.Total_uF_Permeability_Corrected(Timestep_M);
                    }
                    break;
                // If no algorithm is specified, return a zero tensor
                default:
                    microfracturePermeability = new Tensor2S();
                    break;
            }

            // Return the modified fracture permeability tensor
            return microfracturePermeability;
        }
        /// <summary>
        /// Permeability tensor for all layer-bound macrofractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the macrofracture permeability</returns>
        public Tensor2S MacrofracturePermeability(int Timestep_M)
        {
            // The network connectivity multiplier reflects the connectivity of the entire fracture network
            Tensor2S macrofracturePermeability = new Tensor2S();
            switch (PropControl.PermeabilityAlgorithm)
            {
                // The Oda 1985 model assumes fractures of infinite size and connectivity, so does not take into account network connectivity
                case PermeabilityCalculationAlgorithm.Oda1986:
                    {
                        // Get the basic macrofracture permeability tensor
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                macrofracturePermeability += fds.Total_MF_Permeability(Timestep_M);
                    }
                    break;
                // The Oda corrected (1987) algorithm includes a directional multiplier to take account of the connectivity of individual fractures
                case PermeabilityCalculationAlgorithm.OdaCorrected1987:
                    {
                        // First we must get the sum of the uncorrected macrofracture permeability tensors
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                macrofracturePermeability += fds.Total_MF_Permeability(Timestep_M);

                        // Then we can apply a correction factor based on the mean number of connections per macrofracture
                        double networkConnectivityMultiplier = GetNetworkPermeabilityMultiplierFromConnections(ConnectionsPerMacrofracture(false));
                        macrofracturePermeability = networkConnectivityMultiplier * macrofracturePermeability;
                    }
                    break;
                // The size and connectivity correction algorithm takes into account flow between fractures along relay segments, fractures from other sets, or through the host rock
                // The host rock permeability is required to calculate the latter
                case PermeabilityCalculationAlgorithm.SizeConnectivityCorrected:
                    {
                        // In this case the correction is applied to individual components of the permeability tensors for each fracture set
                        // This is based on the size and connectivity data for the fracture sets
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                macrofracturePermeability += fds.Total_MF_Permeability_Corrected(Timestep_M);
                    }
                    break;
                default:
                    break;
            }

            return macrofracturePermeability;
        }
        /// <summary>
        /// Permeability tensor for all unconfined fractures in the gridblock, at the end of a specified previous timestep - not yet implemented
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing unconfined fracture permeability</returns>
        public Tensor2S UnconfinedFracturePermeability(int Timestep_M)
        {
            // The network connectivity multiplier reflects the connectivity of the entire fracture network
            Tensor2S unconfinedFracturePermeability = new Tensor2S();
            switch (PropControl.PermeabilityAlgorithm)
            {
                // The Oda 1985 model assumes fractures of infinite size and connectivity, so does not take into account network connectivity
                case PermeabilityCalculationAlgorithm.Oda1986:
                    {
                        // Get the basic unconfined fracture permeability tensor
                        foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                            unconfinedFracturePermeability += ufs.Total_UCF_Permeability(Timestep_M);
                    }
                    break;
                // The Oda corrected (1987) algorithm includes a directional multiplier to take account of the connectivity of individual fractures
                case PermeabilityCalculationAlgorithm.OdaCorrected1987:
                    {
                        // First we must get the sum of the uncorrected unconfined fracture permeability tensors
                        // Get the basic unconfined fracture permeability tensor
                        foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                            unconfinedFracturePermeability += ufs.Total_UCF_Permeability(Timestep_M);

                        // Then we can apply a correction factor based on the mean number of connections per fracture
                        double networkConnectivityMultiplier = 1;// GetNetworkPermeabilityMultiplierFromConnections(ConnectionsPerMacrofracture(false));
                        unconfinedFracturePermeability = networkConnectivityMultiplier * unconfinedFracturePermeability;
                    }
                    break;
                // The size and connectivity correction algorithm takes into account flow between fractures along relay segments, fractures from other sets, or through the host rock
                // The host rock permeability is required to calculate the latter
                case PermeabilityCalculationAlgorithm.SizeConnectivityCorrected:
                    {
                        // Not yet implemented
                    }
                    break;
                default:
                    break;
            }

            return unconfinedFracturePermeability;
        }
        /// <summary>
        /// Permeability tensor for all fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the total fracture permeability</returns>
        public Tensor2S TotalFracturePermeability(int Timestep_M)
        {
            // Return the sum of the microfracture, macrofracture and unconfined fracture permeability tensors
            // For the Oda (1985) algorithm, this gives the same result as if calculated for the combined fracture population
            // For the Oda corrected (1987) algorithm, the microfractures and macrofractures use different correction factors
            //  - the microfractures use a correction factor based on the trace and anisotropy of the fracture connectivity tensor
            //  - the macrofractures use a correction factor based on the mean number of connections per macrofracture
            // For the size and connectivity correction algorithm, the microfractures and macrofractures will use different algorithms
            //  - the microfractures use an algorithm assuming each microfracture is isolated and calculating the distribution of distances between neighbouring microfractures where fluid must flow through the matrix
            //  - the macrofractures use an algorithm that constructs chains of connected macrofractures, and uses the tip type ratios to calculate the mean distances between neighbouring macrofractures and the flow resistance (whether flow is through the host rock or connecting fracture segments)
            return MicrofracturePermeability(Timestep_M) + MacrofracturePermeability(Timestep_M) + UnconfinedFracturePermeability(Timestep_M);
        }

        // Functions to return fracture sigma factor (related to the mean block size, as defined by Warren & Root 1963)
        /// <summary>
        /// Calculate the current minimum and maximum horizontal dimensions of fracture-bounded blocks
        /// </summary>
        /// <param name="FracType">Flag to specify whether the block is bounded by microfractures only, layer-bound macrofractures only or all fractures</param>
        /// <param name="MinL">Reference parameter for the minimum block dimension</param>
        /// <param name="MaxL">Reference parameter for the maximum block dimension</param>
        private void GetBlockDimensions(FractureType FracType, out double MinL, out double MaxL)
        {
            GetBlockDimensions(FracType, -1, out MinL, out MaxL);
        }
        /// <summary>
        /// Calculate the minimum and maximum horizontal dimensions of fracture-bounded blocks, at at the end of a specified previous timestep
        /// </summary>
        /// <param name="FracType">Flag to specify whether the block is bounded by microfractures only, layer-bound macrofractures only or all fractures</param>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <param name="MinL">Reference parameter for the minimum block dimension</param>
        /// <param name="MaxL">Reference parameter for the maximum block dimension</param>
        private void GetBlockDimensions(FractureType FracType, int Timestep_M, out double MinL, out double MaxL)
        {
            // The block dimensions for unconfined fractures are calculated in a separate function
            if ((FracType == FractureType.UnconfinedFractures) || ((FracType == FractureType.AllFractures) && (NoLayerBoundFractureSets == 0)))
            {
                GetUCFBlockDimensions(Timestep_M, out MinL, out MaxL);
                return;
            }

            bool useCurrentDensityData = (Timestep_M < 0);

            MinL = double.PositiveInfinity;
            MaxL = double.PositiveInfinity;

            // Get the respective P32 values for each fracture set
            double[] P32_values = new double[NoLayerBoundFractureSets];
            // Loop through every set of propagating fractures I
            for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
            {
                LayerBoundFractureSet fsI = LayerBoundFractureSets[fsI_Index];

                switch (FracType)
                {
                    case FractureType.Microfractures:
                        P32_values[fsI_Index] = useCurrentDensityData ? fsI.combined_T_uFP32_total() : fsI.combined_T_uFP32_total(Timestep_M);
                        break;
                    case FractureType.LayerBoundFractures:
                        P32_values[fsI_Index] = useCurrentDensityData ? fsI.combined_T_MFP32_total() : fsI.combined_T_MFP32_total(Timestep_M);
                        break;
                    case FractureType.AllFractures:
                        P32_values[fsI_Index] = useCurrentDensityData ? fsI.combined_T_uFP32_total() + fsI.combined_T_MFP32_total() : fsI.combined_T_uFP32_total(Timestep_M) + fsI.combined_T_MFP32_total(Timestep_M);
                        break;
                    default:
                        P32_values[fsI_Index] = 0;
                        break;
                }
            }

            // If there are no fracture sets, both block dimensions will be infinite
            if (NoLayerBoundFractureSets == 0)
            {
                return;
            }
            // If there is only one fracture set, we can only define the minimum block dimension
            else if (NoLayerBoundFractureSets == 1)
            {
                MinL = 1 / P32_values[0];
            }
            // If there are only two fracture sets, one will determine the minimum block dimension and the other will determine the maximum block dimension
            else if (NoLayerBoundFractureSets == 2)
            {
                if (P32_values[0] > P32_values[1])
                {
                    MinL = 1 / P32_values[0];
                    MaxL = 1 / P32_values[1];
                }
                else
                {
                    MinL = 1 / P32_values[1];
                    MaxL = 1 / P32_values[0];
                }
            }
            // If there are more than two fracture sets, the minimum and maximum block dimensions will be determined by a combination of all fracture sets
            else
            {
                // Find the orientation minimum block dimension
                // This will be the orientation where the combined apparent P32 densities of all sets is maximum
                // This need not coincide with the azimuth of any specific set; however for convenience we will only calculate density along set azimuths
                double maxP32_azimuth = 0;
                double maxP32 = 0;
                for (int fsI_Index = 0; fsI_Index < NoLayerBoundFractureSets; fsI_Index++)
                {
                    double fsI_azimuth = LayerBoundFractureSets[fsI_Index].Azimuth;
                    double P32_I = 0;
                    for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                    {
                        double fsJ_azimuth = LayerBoundFractureSets[fsJ_Index].Azimuth;
                        double cosIJ = Math.Abs(VectorXYZ.Cos_trim(fsI_azimuth - fsJ_azimuth));
                        P32_I += cosIJ * P32_values[fsJ_Index];
                    }

                    if (maxP32 < P32_I)
                    {
                        maxP32 = P32_values[fsI_Index];
                        maxP32_azimuth = fsI_azimuth;
                    }
                }

                // The maximum block dimension will be perpendicular to this
                // Get the combined apparent P32 densities of all sets in this orientation 
                double minP32_azimuth = maxP32_azimuth + (Math.PI / 2);
                double minP32 = 0;
                for (int fsJ_Index = 0; fsJ_Index < NoLayerBoundFractureSets; fsJ_Index++)
                {
                    double fsJ_azimuth = LayerBoundFractureSets[fsJ_Index].Azimuth;
                    double cosIJ = Math.Abs(VectorXYZ.Cos_trim(minP32_azimuth - fsJ_azimuth));
                    minP32 += cosIJ * P32_values[fsJ_Index];
                }

                // Calculate the minimum and maximum block dimensions
                MinL = 1 / maxP32;
                MaxL = 1 / minP32;
            }
        }
        /// <summary>
        /// Calculate the minimum and maximum horizontal dimensions of unconfined fracture-bounded blocks, at at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <param name="MinL">Reference parameter for the minimum block dimension</param>
        /// <param name="MaxL">Reference parameter for the maximum block dimension</param>
        private void GetUCFBlockDimensions(int Timestep_M, out double MinL, out double MaxL)
        {
            bool useCurrentDensityData = (Timestep_M < 0);

            MinL = double.PositiveInfinity;
            MaxL = double.PositiveInfinity;

            // Get the respective P32 values for each fracture set
            double[] P32_values = new double[NoUnconfinedFractureSets];
            // Loop through every set of propagating fractures I
            for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
            {
                UnconfinedFractureSet ufsI = UnconfinedFractureSets[ufsI_Index];
                P32_values[ufsI_Index] = useCurrentDensityData ? ufsI.UCFP32_total() : ufsI.getTotalUCFP32(Timestep_M);
            }

            // If there are no fracture sets, both block dimensions will be infinite
            if (NoUnconfinedFractureSets == 0)
            {
                return;
            }
            // If there is only one fracture set, we can only define the minimum block dimension
            else if (NoUnconfinedFractureSets == 1)
            {
                MinL = 1 / P32_values[0];
            }
            // If there are only two fracture sets, one will determine the minimum block dimension and the other will determine the maximum block dimension
            else if (NoUnconfinedFractureSets == 2)
            {
                if (P32_values[0] > P32_values[1])
                {
                    MinL = 1 / P32_values[0];
                    MaxL = 1 / P32_values[1];
                }
                else
                {
                    MinL = 1 / P32_values[1];
                    MaxL = 1 / P32_values[0];
                }
            }
            // If there are more than two fracture sets, the minimum and maximum block dimensions will be determined by a combination of all fracture sets
            else
            {
                // Find the orientation minimum block dimension
                // This will be the orientation where the combined apparent P32 densities of all sets is maximum
                // This need not coincide with the azimuth of any specific set; however for convenience we will only calculate density along set azimuths
                double maxP32_azimuth = 0;
                double maxP32 = 0;
                for (int ufsI_Index = 0; ufsI_Index < NoUnconfinedFractureSets; ufsI_Index++)
                {
                    double ufsI_azimuth = UnconfinedFractureSets[ufsI_Index].Azimuth;
                    double P32_I = 0;
                    for (int ufsJ_Index = 0; ufsJ_Index < NoUnconfinedFractureSets; ufsJ_Index++)
                    {
                        double ufsJ_azimuth = UnconfinedFractureSets[ufsJ_Index].Azimuth;
                        double cosIJ = Math.Abs(VectorXYZ.Cos_trim(ufsI_azimuth - ufsJ_azimuth));
                        P32_I += cosIJ * P32_values[ufsJ_Index];
                    }

                    if (maxP32 < P32_I)
                    {
                        maxP32 = P32_values[ufsI_Index];
                        maxP32_azimuth = ufsI_azimuth;
                    }
                }

                // The maximum block dimension will be perpendicular to this
                // Get the combined apparent P32 densities of all sets in this orientation 
                double minP32_azimuth = maxP32_azimuth + (Math.PI / 2);
                double minP32 = 0;
                for (int ufsJ_Index = 0; ufsJ_Index < NoUnconfinedFractureSets; ufsJ_Index++)
                {
                    double ufsJ_azimuth = UnconfinedFractureSets[ufsJ_Index].Azimuth;
                    double cosIJ = Math.Abs(VectorXYZ.Cos_trim(minP32_azimuth - ufsJ_azimuth));
                    minP32 += cosIJ * P32_values[ufsJ_Index];
                }

                // Calculate the minimum and maximum block dimensions
                MinL = 1 / maxP32;
                MaxL = 1 / minP32;
            }
        }
        /// <summary>
        /// Calculate the sigma factor given the minimum and maximum block dimensions, as defined in Kazemi et al (1976)
        /// </summary>
        /// <param name="MinL">Minimum block dimension; if this is infinite, sigma will be undefined</param>
        /// <param name="MaxL">Maximum block dimension; if this is infinite, sigma will be calculated for a single fracture set</param>
        /// <returns></returns>
        private double CalculateSigma(double MinL, double MaxL)
        {
            double n_factor, l_factor;
            // If there are no fractures, the minimum block dimension will be infinite
            // In this case sigma will be undefined
            if (double.IsInfinity(MinL))
            {
                n_factor = 0;
                l_factor = double.PositiveInfinity;
            }
            // If there is only one set of fractures, the maximum block dimension will be infinite
            // In this case return sigma for a single fracture set
            else if (double.IsInfinity(MaxL))
            {
                int n = 1;
                n_factor = 4d * (double)n * (double)(n + 2);
                l_factor = MinL;
            }
            // If there are more than one set of fractures, both the minimum and maximum block dimensions will be defined
            // In this case return sigma for two fracture sets
            else
            {
                int n = 2;
                n_factor = 4d * (double)n * (double)(n + 2);
                l_factor = (double)n * (MinL * MaxL) / (MinL + MaxL);
            }

            return n_factor / (l_factor * l_factor);
        }
        /// <summary>
        /// Sigma factor for all current microfractures in the gridblock
        /// </summary>
        /// <returns>Sigma factor for the microfractures in the gridblock</returns>
        public double MicrofractureSigmaFactor()
        {
            return MicrofractureSigmaFactor(-1);
        }
        /// <summary>
        /// Sigma factor for all current layer-bound macrofractures in the gridblock
        /// </summary>
        /// <returns>Sigma factor for the macrofractures in the gridblock</returns>
        public double MacrofractureSigmaFactor()
        {
            return MacrofractureSigmaFactor(-1);
        }
        /// <summary>
        /// Sigma factor for all current unconfined fractures in the gridblock - not yet implemented
        /// </summary>
        /// <returns>Sigma factor for the unconfined in the gridblock</returns>
        public double UnconfinedFractureSigmaFactor()
        {
            return UnconfinedFractureSigmaFactor(-1);
        }
        /// <summary>
        /// Sigma factor for all current fractures in the gridblock
        /// </summary>
        /// <returns>Sigma factor for all fractures in the gridblock</returns>
        public double TotalFractureSigmaFactor()
        {
            return TotalFractureSigmaFactor(-1);
        }
        /// <summary>
        /// <summary>
        /// Sigma factor for all microfractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Sigma factor for the microfractures in the gridblock</returns>
        public double MicrofractureSigmaFactor(int Timestep_M)
        {
            // Get the minimum and maximum block dimensions
            GetBlockDimensions(FractureType.Microfractures, Timestep_M, out double MinL, out double MaxL);

            // Calculate and return the sigma factor
            return CalculateSigma(MinL, MaxL);
        }
        /// Sigma factor for all layer-bound macrofractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Sigma factor for the macrofractures in the gridblock</returns>
        public double MacrofractureSigmaFactor(int Timestep_M)
        {
            // Get the minimum and maximum block dimensions
            GetBlockDimensions(FractureType.LayerBoundFractures, Timestep_M, out double MinL, out double MaxL);

            // Calculate and return the sigma factor
            return CalculateSigma(MinL, MaxL);
        }
        /// Sigma factor for all unconfined fractures in the gridblock, at the end of a specified previous timestep - not yet implemented
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Sigma factor for the unconfined fractures in the gridblock</returns>
        public double UnconfinedFractureSigmaFactor(int Timestep_M)
        {
            // Get the minimum and maximum block dimensions
            GetBlockDimensions(FractureType.UnconfinedFractures, Timestep_M, out double MinL, out double MaxL);

            // Calculate and return the sigma factor
            return CalculateSigma(MinL, MaxL);
        }
        /// <summary>
        /// Sigma factor for all fractures in the gridblock, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Sigma factor for all fractures in the gridblock</returns>
        public double TotalFractureSigmaFactor(int Timestep_M)
        {
            // Get the minimum and maximim block dimensions
            GetBlockDimensions(FractureType.AllFractures, Timestep_M, out double MinL, out double MaxL);

            // Calculate and return the sigma factor
            return CalculateSigma(MinL, MaxL);
        }

        // Bulk rock elastic properties
        /// <summary>
        /// Compliance tensor for all fracture sets
        /// </summary>
        public Tensor4_2Sx2S S_F
        {
            // This is not stored as a separate object but is calculated dynamically from the compliance tensors for the fractures when required
            get
            {
                Tensor4_2Sx2S s_F = new Tensor4_2Sx2S();
                foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    s_F += fs.S_set;
                foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    s_F += ufs.S_set;
                return s_F;
            }
        }
        /// <summary>
        /// Bulk rock compliance tensor - includes fractures and intact rock
        /// </summary>
        public Tensor4_2Sx2S S_b
        {
            // This is not stored as a separate object but is calculated dynamically from the compliance tensors for the fractures and for the intact rock when required
            get
            {
                return S_F + MechProps.S_r;
            }
        }
        /// <summary>
        /// Effective bulk rock compliance tensor - compliance tensor for the rockmass excluding stress shadows
        /// </summary>
        public Tensor4_2Sx2S S_beff
        {
            // This is equal to the bulk rock compliance tensor in the evenly distributed stress scenario, and the intact rock compliance tensor for the stress shadow scenario
            get
            {
                switch (PropControl.StressDistributionCase)
                {
                    case StressDistribution.EvenlyDistributedStress:
                        // For evenly distributed stress, there are no stress shadows so the effective bulk rock compliance tensor is the sum of the compliance tensors for the fractures and for the intact rock
                        return S_b;
                    case StressDistribution.StressShadow:
                        // With stress shadows, the strain on the fractures is accommodated within the stress shadows, which are not included in the effective bulk rock compliance tensor; therefore this is equal to the compliance tensor for the intact rock
                        return new Tensor4_2Sx2S(MechProps.S_r);
                    case StressDistribution.DuctileBoundary:
                        // Not yet implemented - return compliance tensor for the intact rock
                        return new Tensor4_2Sx2S(MechProps.S_r);
                    default:
                        // Return compliance tensor for the intact rock
                        return new Tensor4_2Sx2S(MechProps.S_r);
                }
            }
        }

        // Functions to calculate fracture population data and generate the DFN
        /// <summary>
        /// Calculate the implicit fracture model based on the parameters specified in the existing GridblockConfiguration.PropagationControl object
        /// </summary>
        /// <returns>CalculateFractureDataReturnCode object indicating if the calculation ran to completion or errors were encountered</returns>
        public CalculateFractureDataReturnCode CalculateFractureData()
        {
            // Declare local variables
            bool CalculationCompleted = false;
            bool WithinTimestepLimit = true;
            StrainRelaxationCase SRC = MechProps.GetStrainRelaxationCase();
            StressDistribution SD = PropControl.StressDistributionCase;

            // Cache constants locally
            // Cache thermo-poro-elastic properties locally
            double E_r = MechProps.E_r;
            double Nu_r = MechProps.Nu_r;
            double OneMinusBiot = 1 - MechProps.Biot;
            double Kb_r = MechProps.Kb_r;
            double ThermalExpansionCoefficient = MechProps.ThermalExpansionCoefficient;
            // Cache mechanical coefficients locally
            double tr = MechProps.tr;
            double tf = MechProps.tf;

            // Calculation control criteria
            // Set the target maximum increase in MFP33 and UCFP33 allowed per timestep
            double d_MFP33 = PropControl.max_TS_MFP33_increase;
            double d_UCFP33 = PropControl.max_TS_UCFP33_increase;
            // Set the target maximum increase in unconfined fracture ray length allowed per timestep
            double d_RayLength = PropControl.max_R_timestep_increase;
            // Set the ratio of current to maximum active fracture volumetric ratio or mean linear density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio
            double historic_a_MFP33_termination_ratio = PropControl.historic_a_MFP33_termination_ratio;
            double historic_a_UCFP32_termination_ratio = PropControl.historic_a_UCFP32_termination_ratio;
            // Set the ratio of active to total fracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio 
            double active_total_MFP30_termination_ratio = PropControl.active_total_MFP30_termination_ratio;
            double active_total_UCRP30_termination_ratio = PropControl.active_total_UCRP30_termination_ratio;
            // Set the minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
            double minimum_MFClearZone_Volume = PropControl.minimum_MFClearZone_Volume;
            double minimum_UCFClearZone_Volume = PropControl.minimum_UCFClearZone_Volume;
            // Set the minimum allowed mean static unconfined fracture ray length; if the mean static ray length drops below this value, the fracture set will be deactivated
            // If this is less than 0, we will use the minimum UCF radius
            double minStaticRayLength = PropControl.minStaticRayLength;
            // Set maximum number of timesteps allowed
            int maxTimesteps = PropControl.maxTimesteps;
            // Set maximum duration for individual timesteps
            double maxTimestepDuration = PropControl.maxTimestepDuration;
            bool useMaxTSDurationCutoff = (maxTimestepDuration > 0);
            // Flag for whether all fracture sets have been deactivated
            bool AllSetsDeactivated = false;
            // Frequency (in timesteps) with which static unconfined fracture datapoints are culled
            int ufsDatapointCullFrequency = PropControl.cullTSFrequency;

            // Set the number of bins to split the microfracture radii into when calculating uFP32 and uFP33 numerically
            int no_r_bins = PropControl.no_r_bins;
            // Maximum radius of microfractures in the smallest bin - used in determining fracture set deactivation
            double minrb_maxRad = (1 / (double)no_r_bins) * MaximumMicrofractureRadius;
            // Reset the microfracture index array for each fracture dipset
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                foreach (FractureDipSet fds in fs.FractureDipSets)
                {
                    fds.reset_uF_radii_array(no_r_bins);
                }
            }
            // Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set
            // NB we do not need to do this in the evenly distributed stress scenario
            bool checkAlluFStressShadows = PropControl.checkAlluFStressShadows && (SD != StressDistribution.EvenlyDistributedStress);
            // Flag to check unconfined fractures against stress shadows of all other unconfined fractures, regardless of set; if false will only check unconfined fractures against stress shadows of other unconfined vfractures in the same set
            // NB we do not need to do this in the evenly distributed stress scenario
            bool checkAllUCFStressShadows = PropControl.checkAllUCFStressShadows && (SD != StressDistribution.EvenlyDistributedStress);

            // Output control criteria
            // Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor
            bool CalculateRelaxedStrainPartitioning = PropControl.CalculateRelaxedStrainPartitioning;
            // Flag to output the bulk rock compliance and stiffness tensors
            bool OutputBulkRockElasticTensors = PropControl.OutputBulkRockElasticTensors;
            // Flag to calculate full fracture cumulative population distribution function; if false, will only calculate total cumulative properties (i.e. r = 0 and l = 0)
            bool CalculatePopulationDistributionData = PropControl.CalculatePopulationDistributionData;
            // Flag to calculate and output fracture porosity
            bool CalculateFracturePorosity = PropControl.CalculateFracturePorosity;
            // Flag to calculate and output fracture permeability tensor
            bool CalculateFracturePermeabilityTensor = PropControl.CalculateFracturePermeabilityTensor;
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
            FractureApertureType FractureApertureControl = PropControl.FractureApertureControl;
            // Flag to save microfracture density distribution data for each timestep
            // This is only required if calculating cumulative population distribution function or calculating permeability using the size and connectivity correction algorithm
            bool saveMicrofractureDensityDistributionData = CalculatePopulationDistributionData || (PropControl.PermeabilityAlgorithm == PermeabilityCalculationAlgorithm.SizeConnectivityCorrected);
            // Flag to calculate implicit data for unconfined fractures; if set to false no grid properties will be generated, only an explicit DFN; does not affect layer-bound fractures
            bool CalculateImplicitUCFData = PropControl.calculateImplicitUCFData;

            // Get time units and unit conversion modifier for output time data if not in SI units
            TimeUnits timeUnits = PropControl.timeUnits;
            double timeUnits_Modifier = PropControl.getTimeUnitsModifier();

            // Determine whether implicit fracture data should be written to file, and if so create a file
            bool writeImplicitDataToFile = PropControl.WriteImplicitDataFiles;
            StreamWriter outputFile = null;
            if (writeImplicitDataToFile)
            {
                string fileName = string.Format("ImplicitData_X{0}_Y{1}_Depth{2}.txt", SWtop.X, SWtop.Y, SWtop.Depth);
                String namecomb = PropControl.FolderPath + fileName;
                outputFile = new StreamWriter(namecomb);
            }
            bool useSetNames = false;
            bool useDipSetNames = true;
            bool useUnconfinedSetNames = true;
#if LOGIMPPOP
            RayPropagationStatus[] rayTypesToLog = new RayPropagationStatus[5] { RayPropagationStatus.FullyActive, RayPropagationStatus.Restricted, RayPropagationStatus.StaticStressShadow, RayPropagationStatus.StaticIntersection, RayPropagationStatus.StaticMaxRadius };
            Dictionary<RayPropagationStatus, StreamWriter> rayLogFiles = new Dictionary<RayPropagationStatus, StreamWriter>();
            int setToLog = 2;
            foreach (RayPropagationStatus rayType in rayTypesToLog)
                rayLogFiles[rayType] = new StreamWriter(PropControl.FolderPath + string.Format("Set{0}_{1}_X{2}_Y{3}_Depth{4}.txt",setToLog, rayType, SWtop.X, SWtop.Y, SWtop.Depth));
            UnconfinedFractureSet UFSToLog = UnconfinedFractureSets[setToLog];
#endif

            // Write header to logfile
            if (writeImplicitDataToFile)
            {
                string headerLine1 = string.Format("Timestep\tDuration ({0})\tEnd Time ({0})\tElastic ehmin\tElastic ehmax\tTotal ehmin\tTotal ehmax\t", timeUnits);
                string headerLine2 = "\t\t\t\t\t\t\t";
                string FSheader1 = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
                string FSheader2 = "Fracture stage\tDriving stress\tDisplacement sense\tSlip pitch\ta_uFP30\ts_uFP30\ta_uFP32\ts_uFP32\ta_MFP30\tsII_MFP30\tsIJ_MFP30\ta_MFP32\ts_MFP32\tMF Propagation Rate\tMF Deactivation Rate\tMF Stress shadow width\tMF Stress shadow volume\tClear zone volume\t\t\t";
                string FSheader3 = "Fracture stage\tDriving stress\tDisplacement sense\tSlip pitch\ta_RP30\tr_RP30\tc_RP30\tsII_RP30\tsIJ_RP30\tsMR_RP30\ta_RP32\tr_RP32\tc_RP32\tsII_RP32\tsIJ_RP32\tsMR_RP32\tStress shadow width:radius\tStress shadow volume\tClear zone volume\t\t";
#if LOGIMPPOP
                headerLine1 = string.Format("Timestep\tDuration ({0})\tEnd Time ({0})\t{1}\t{2}\t{3}\t{4}\t", timeUnits, "Sigma_eff.XX", "Sigma_eff.YY", "Sigma_eff.XY", "Sigma_eff.ZZ");
                //FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "Mean_Azimuthal_MF_StressShadowWidth", "Mean_Shear_MF_StressShadowWidth", "Mean_MF_StressShadowWidth", "getInverseStressShadowVolume", "getInverseStressShadowVolumeAllFS",
                //    "getClearZoneVolume", "sII_MFP30_total", "sIJ_MFP30_total", "a_MFP32_total", "s_MFP32_total", "getClearZoneVolumeAllFS");
                //FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "U", "V", "MF Propagation Rate", "MF Propagation Distance", "MF Maximum Propagation Distance", 
                //    "getClearZoneVolume", "sII_MFP30_total", "sIJ_MFP30_total", "a_MFP32_total", "s_MFP32_total", "getClearZoneVolumeAllFS");
                FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "getPhi", "getInstantaneousFII", "getInstantaneousFIJ", "getInverseStressShadowVolume", "getInverseStressShadowVolumeAllFS", 
                    "a_MFP30_total", "sII_MFP30_total", "sIJ_MFP30_total", "a_MFP32_total", "s_MFP32_total", "");
                //FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "getAA", "getBB", "getCCStep", "getMeanStressShadowWidth", "getMeanShearStressShadowWidth",
                //    "getInverseStressShadowVolume", "getInverseStressShadowVolumeAllFS", "getClearZoneVolume", "a_MFP32_total", "s_MFP32_total", "getClearZoneVolumeAllFS");
                FSheader3 = "Fracture stage\tDriving stress\tStress shadow width ratio\ta_RP30\tr_RP30\tc_RP30\tsII_RP30\tsIJ_RP30\tsMR_RP30\tRP31_active\tRP31_static\tRP32_active\tRP32_static\tRP33_active\tRP33_static\tThis set Stress shadow volume\tAll sets Stress shadow volume\tClear zone volume\t\t\t";
#endif
                string TS0data = "0\t0\t0\t0\t0\t0\t0\t";
                for (int fs_index = 0; fs_index < NoLayerBoundFractureSets; fs_index++)
                {
                    LayerBoundFractureSet fs = LayerBoundFractureSets[fs_index];
                    int NoDipSets = fs.FractureDipSets.Count;
                    List<string> dipSetLabels = fs.DipSetLabels();
                    for (int dipsetIndex = 0; dipsetIndex < NoDipSets; dipsetIndex++)
                    {
                        headerLine1 += string.Format("FS {0} {1}", (useSetNames ? getLayerBoundFractureSetName(fs_index) : fs_index.ToString()), (useDipSetNames ? dipSetLabels[dipsetIndex] : string.Format("Dipset {0}", dipsetIndex))) + FSheader1;
                        headerLine2 += FSheader2;
                        TS0data += "NotActivated\t0\t\t\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t1\t\t\t";
                    }
                }
                for (int ufs_index = 0; ufs_index < NoUnconfinedFractureSets; ufs_index++)
                {
                    UnconfinedFractureSet ufs = UnconfinedFractureSets[ufs_index];
                    string setLabel = useUnconfinedSetNames ? getUnconfinedFractureSetName(ufs_index) : string.Format("UFS {0}", ufs_index);
                    headerLine1 += setLabel + FSheader1;
                    headerLine2 += FSheader3;
                    TS0data += "NotActivated\t0\t\t\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t1\t\t";
                }
                if (CalculateFracturePorosity)
                {
                    headerLine1 += "uF Porosity: Uniform aperture\tMF Porosity: Uniform aperture\tUCF Porosity: Uniform aperture\tuF Porosity: Size-dependent aperture\tMF Porosity: Size-dependent aperture\tUCF Porosity: Size-dependent aperture\tuF Porosity: Dynamic aperture\tMF Porosity: Dynamic aperture\tUCF Porosity: Dynamic aperture\tuF Porosity: Barton-Bandis aperture\tMF Porosity: Barton-Bandis aperture\tUCF Porosity: Barton-Bandis aperture\t";
                    headerLine2 += "\t\t\t\t\t\t\t\t\t\t\t\t";
                    TS0data += "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t";
                }
                if (CalculateFracturePermeabilityTensor)
                {
                    Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                    // Header for microfracture permeability tensor
                    headerLine1 += "uF Permeability tensor\t\t\t\t\t\t\t";
                    foreach (Tensor2SComponents ij in tensorComponents)
                    {
                        headerLine2 += ij + "\t";
                        TS0data += string.Format("0\t");
                    }
                    headerLine2 += "Sigma\t";
                    TS0data += string.Format("0\t");
                    // Header for macrofracture permeability tensor
                    headerLine1 += "MF Permeability tensor\t\t\t\t\t\t\t";
                    foreach (Tensor2SComponents ij in tensorComponents)
                    {
                        headerLine2 += ij + "\t";
                        TS0data += string.Format("0\t");
                    }
                    headerLine2 += "Sigma\t";
                    TS0data += string.Format("0\t");
                    // Header for unconfined fracture permeability tensor
                    headerLine1 += "UCF Permeability tensor\t\t\t\t\t\t\t";
                    foreach (Tensor2SComponents ij in tensorComponents)
                    {
                        headerLine2 += ij + "\t";
                        TS0data += string.Format("0\t");
                    }
                    headerLine2 += "Sigma\t";
                    TS0data += string.Format("0\t");
                    // Header for total fracture permeability tensor
                    headerLine1 += "Total fracture Permeability tensor\t\t\t\t\t\t\t";
                    foreach (Tensor2SComponents ij in tensorComponents)
                    {
                        headerLine2 += ij + "\t";
                        TS0data += string.Format("0\t");
                    }
                    headerLine2 += "Sigma\t";
                    TS0data += string.Format("0\t");
                }
                if (OutputBulkRockElasticTensors)
                {
                    Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                    // Header for compliance tensor
                    headerLine1 += "Bulk rock compliance tensor\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
                    foreach (Tensor2SComponents ij in tensorComponents)
                        foreach (Tensor2SComponents kl in tensorComponents)
                        {
                            headerLine2 += ij + "," + kl + "\t";
                            TS0data += string.Format("{0}\t", MechProps.S_r.Component(ij, kl));
                        }
                    // Header for stiffness tensor
                    Tensor4_2Sx2S C_r = MechProps.S_r.Inverse();
                    headerLine1 += "Bulk rock stiffness tensor\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
                    foreach (Tensor2SComponents ij in tensorComponents)
                        foreach (Tensor2SComponents kl in tensorComponents)
                        {
                            headerLine2 += ij + "," + kl + "\t";
                            TS0data += string.Format("{0}\t", C_r.Component(ij, kl));
                        }
                }

                outputFile.WriteLine(headerLine1);
                outputFile.WriteLine(headerLine2);
                outputFile.WriteLine(TS0data);
            }

#if LOGIMPPOP
            string logFileHeaderLine = string.Format("Timestep\tDuration\tEnd time\tDriving stress\tTotal P33 including overlaps\tInverse stress shadow volume\tNo datapoints\t\tDatapoint No\tdP30\tRay Length\tEffective Ray Length\tPhiII\tPhiIJ");
            foreach (RayPropagationStatus rayType in rayTypesToLog)
                rayLogFiles[rayType].WriteLine(logFileHeaderLine);
#endif

            // Set the fracture distribution flags for each fracture set, based on the specified stress distribution case
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                fs.FractureDistribution = SD;
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                ufs.FractureDistribution = SD;

            // Loop through the deformation episodes
            int currentDeformationEpisodeIndex = 0;
            double endLastTimestep = 0;
            foreach (DeformationEpisodeLoadControl currentDeformationEpisode in PropControl.DeformationEpisodes)
            {
                // Check if we have already reached the maximum timestep limit; if so, end the calculation
                if (CurrentImplicitTimestep >= maxTimesteps)
                    break;
                // Otherwise reset the CalculationCompleted flag to false
                CalculationCompleted = false;

                // Update the index of the current deformation episode
                currentDeformationEpisodeIndex++;

                // Flag for whether to stop the calculation when all sets have been deactivated
                // By default we will continue until the end of the specified deformation duration
                bool StopWhenAllSetsDeactivated = false;

                // Get the deformation episode duration
                double CurrentDeformationEpisodeDuration = currentDeformationEpisode.DeformationEpisodeDuration;
                // If the deformation stage duration is negative or NaN, then we will stop automatically when all fracture sets have been deactivated
                // We will set the deformation stage duration to infinity and set the calculation to stop automatically when all sets have been deactivated
                if (double.IsNaN(CurrentDeformationEpisodeDuration) || (CurrentDeformationEpisodeDuration < 0))
                {
                    CurrentDeformationEpisodeDuration = double.PositiveInfinity;
                    StopWhenAllSetsDeactivated = true;
                }
                // Calculate the end time of the current deformation episode
                // If the current deformation episode duration is uncertain (infinite) then the current deformation episode end time will also be set to infinity
                double CurrentDeformationEpisodeEndTime = endLastTimestep + CurrentDeformationEpisodeDuration;

                // Check if the load for this deformation episode is defined by strain or stress
                // NB some dynamic loads may still be defined in terms of strain, if only the fluid pressure and/or vertical stress are defined dynamically
                // Loads will only be defined in terms of stress is at a minimum the ZZ, XX, YY and XY components of the absolute stress rate tensor are defined
                bool stressLoad = currentDeformationEpisode.StressLoadDefined;

                // Usually the calculation will start from the stress and strain state at the end of the previous deformation episode, or the initial lithostatic load state if this is the first episode
                // However if coupling with output from geomechanical modelling, it may be necessary to adjust the fluid pressure, stress and strain state to match the state in the geomechanical model output
                // This is done by passing the  object from the current DeformationEpisodeLoadControl to the StressStrainState.SetStressStrainState function
                if (currentDeformationEpisode.InitialStressStateDefined)
                    StressStrain.SetStressState(currentDeformationEpisode.InitialStressState);

                // Create local copies of the applied strain rate and compactional strain rate tensors
                Tensor2S appliedStrainRate = currentDeformationEpisode.Applied_Epsilon_dashed;
                Tensor2S compactionalStrainRate = new Tensor2S();

                // Create local copies of the overpressure rate, uplift rate, stress arching factor and rate of temperature change
                // NB If a stress load is defined, the stress arching factor will be set to NaN
                double overpressureRate = currentDeformationEpisode.AppliedOverpressureRate;
                double upliftRate = currentDeformationEpisode.AppliedUpliftRate;
                double stressArchingFactor = currentDeformationEpisode.StressArchingFactor;
                double tempChangeRate = currentDeformationEpisode.AppliedTemperatureChange - (upliftRate * StressStrain.GeothermalGradient);

                // Set the fluid overpressure and uplift rates in the StressStrain object
                // These will not vary during the deformation episode
                StressStrain.FluidOverpressureRate = overpressureRate;
                StressStrain.UpliftRate = upliftRate;
                // Now we can get the rate of change of fluid pressure, which includes changes in both hydrostatic pressure and fluid overpressure
                double fluidPressureRate = StressStrain.P_f_dashed;

                if (stressLoad)
                {
                    // If the load is defined in terms of stress, we simply need to set the Terzaghi effective stress rate tensor in the StressStrain object
                    // Since the stress load is defined in terms the absolute stress, we will need to subtract the fluid pressure rate from the XX, YY and ZZ components
                    Tensor2S appliedEffectiveStressRate = currentDeformationEpisode.Absolute_Stress_dashed - new Tensor2S(fluidPressureRate, fluidPressureRate, fluidPressureRate, 0, 0, 0);
                    StressStrain.Sigma_eff_dashed = appliedEffectiveStressRate;

                    // Recalculate the tensors for current elastic strain and rate of change of elastic strain
                    // The method for doing this will depend on the stress distribution scenario
                    switch (SD)
                    {
                        case StressDistribution.EvenlyDistributedStress:
                            // In the evenly distributed stress scenario, the bulk rock compliance tensor will change as the fractures grow
                            // We must therefore use the current bulk rock compliance tensor to recalculate the stress tensors
                            StressStrain.RecalculateStrain(S_beff);
                            break;
                        case StressDistribution.StressShadow:
                            // In the stress shadow scenario, the bulk compliance tensor is isotropic and does not change
                            // We can therefore recalculate the strain tensors from just the Young's Modulus and Poisson's ratio of the host rock
                            StressStrain.RecalculateStrain(E_r, Nu_r);
                            break;
                        case StressDistribution.DuctileBoundary:
                            // Not yet implemented
                            break;
                        default:
                            break;
                    }
                    appliedStrainRate = StressStrain.el_Epsilon_dashed;

                    // Set the compactional strain rate tensor
                    // NB this will only include thermal effects and stress arching if these are specified in the strain load parameters
                    double internalFPStressRate = (OneMinusBiot * fluidPressureRate);
                    double internalTempStressRate = -(ThermalExpansionCoefficient * Kb_r * tempChangeRate);
                    if (double.IsNaN(internalTempStressRate))
                        internalTempStressRate = 0;
                    double internalStressRate = internalFPStressRate + internalTempStressRate;
                    double internalStressRate_StressArchSupported = stressArchingFactor * ((OneMinusBiot * overpressureRate) - (ThermalExpansionCoefficient * Kb_r * tempChangeRate));
                    if (double.IsNaN(internalStressRate_StressArchSupported))
                        internalStressRate_StressArchSupported = 0;
                    double horizontalCompactionalStrainRate = ((1 - (2 * Nu_r)) / E_r) * internalStressRate;
                    double verticalCompactionalStrainRate = ((1 - (2 * Nu_r)) / E_r) * internalStressRate_StressArchSupported;
                    compactionalStrainRate.ComponentAdd(Tensor2SComponents.XX, -horizontalCompactionalStrainRate);
                    compactionalStrainRate.ComponentAdd(Tensor2SComponents.YY, -horizontalCompactionalStrainRate);
                    compactionalStrainRate.ComponentAdd(Tensor2SComponents.ZZ, -verticalCompactionalStrainRate);

                    // Set the compactional strain rate tensors in the StressStrain object
                    StressStrain.el_Epsilon_compactional_dashed = compactionalStrainRate;
                }
                else
                {
                    // The load is defined in terms of strain
                    // Calculate the compactional horizontal strain due to fluid pressure and temperature changes, and subtract this from the local applied strain rate tensor
                    // This is taken from Miller (1995), but modified to allow the degree of stress arching to be varied
                    // NB We do not need to add the vertical strain component to the applied strain rate tensor
                    // This will be calculated automatically during partial inversion as long as the compactional stress has been added to the vertical component of the stress rate tensor
                    // However we do need to add it to the compactional strain rate tensor
                    double internalStressRate = (OneMinusBiot * fluidPressureRate) - (ThermalExpansionCoefficient * Kb_r * tempChangeRate);
                    double internalStressRate_StressArchSupported = stressArchingFactor * ((OneMinusBiot * overpressureRate) - (ThermalExpansionCoefficient * Kb_r * tempChangeRate));
                    double horizontalCompactionalStrainRate = ((1 - (2 * Nu_r)) / E_r) * internalStressRate;
                    double verticalCompactionalStrainRate = ((1 - (2 * Nu_r)) / E_r) * internalStressRate_StressArchSupported;
                    appliedStrainRate.ComponentAdd(Tensor2SComponents.XX, -horizontalCompactionalStrainRate);
                    appliedStrainRate.ComponentAdd(Tensor2SComponents.YY, -horizontalCompactionalStrainRate);
                    compactionalStrainRate.ComponentAdd(Tensor2SComponents.XX, -horizontalCompactionalStrainRate);
                    compactionalStrainRate.ComponentAdd(Tensor2SComponents.YY, -horizontalCompactionalStrainRate);
                    compactionalStrainRate.ComponentAdd(Tensor2SComponents.ZZ, -verticalCompactionalStrainRate);

                    // Set the applied strain rate and compactional strain rate tensors in the StressStrain object
                    // These may vary during the deformation episode due to viscoleastic strain relaxation; in this case they will be recalculated and updated within each timestep
                    // If there is no strain relaxation, the rate of change of elastic strain is the applied strain rate and will not change during the deformation episode
                    // NB we need to keep the local copy of the applied strain rate tensor, as the ZZ component of the StressStrain.el_Epsilon_dashed tensor may be changed when calculating the stress tensors
                    StressStrain.el_Epsilon_dashed = appliedStrainRate;
                    StressStrain.el_Epsilon_compactional_dashed = compactionalStrainRate;

                    // Calculate the equivalent vertical stress due to fluid pressure and temperature changes, and add this to the stress rate tensor
                    // NB This is dependent on the degree of stress arching; if there is no stress arching, vertical stress will be equal to lithostatic stress and there will be no vertical stress change
                    double verticalAbsoluteStressRate = StressStrain.LithostaticStress_dashed;
                    double hydrostaticPressureRate = fluidPressureRate - overpressureRate;
                    double verticalEffectiveStressRate_SubsidenceSupported = (verticalAbsoluteStressRate - hydrostaticPressureRate) - ((1 - stressArchingFactor) * overpressureRate);
                    double verticalEffectiveStressRate_StressArchSupported = -internalStressRate_StressArchSupported;
                    StressStrain.Sigma_eff_dashed.Component(Tensor2SComponents.ZZ, verticalEffectiveStressRate_SubsidenceSupported + verticalEffectiveStressRate_StressArchSupported);
                }

                // Recalculate the incremental strain acting on the fractures, for the specified applied strain rate tensor
                // If the applied strain rate is zero, use the total applied strain
                if (appliedStrainRate.IsZeroValued())
                {
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                        fs.RecalculateHorizontalStrainRatios(StressStrain.el_Epsilon_noncompactional);
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                        ufs.RecalculateStrainRatios(StressStrain.el_Epsilon_noncompactional);
                }
                else
                {
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                        fs.RecalculateHorizontalStrainRatios(appliedStrainRate);
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                        ufs.RecalculateStrainRatios(appliedStrainRate);
                }

                // If required, populate the azimuthal and strike-slip shear stress shadow multiplier arrays and the unconfined fracture set stress shadow multiplier array
                if (checkAlluFStressShadows)
                {
                    for (int I = 0; I < NoLayerBoundFractureSets; I++)
                    {
                        LayerBoundFractureSet FSI = LayerBoundFractureSets[I];
                        for (int J = 0; J < NoLayerBoundFractureSets; J++)
                        {
                            if (I != J)
                            {
                                LayerBoundFractureSet FSJ = LayerBoundFractureSets[J];
                                FaaIJ[I, J] = FSI.getFaaIJ(FSJ);
                                FasIJ[I, J] = FSI.getFasIJ(FSJ);
                            }
                            else
                            {
                                FaaIJ[I, J] = 1;
                                FasIJ[I, J] = 1;
                            }
                        }
                    }
                }
                if (checkAllUCFStressShadows)
                {
                    for (int I = 0; I < NoUnconfinedFractureSets; I++)
                    {
                        UnconfinedFractureSet UFSI = UnconfinedFractureSets[I];
                        for (int J = 0; J < NoUnconfinedFractureSets; J++)
                        {
                            if (I != J)
                            {
                                UnconfinedFractureSet UFSJ = UnconfinedFractureSets[J];
                                UCFW_IJ[I, J] = UFSI.getUFSW_IJ(UFSJ);
                            }
                            else
                            {
                                UCFW_IJ[I, J] = 1;
                            }
                        }
                    }
                }

                // Loop through the timesteps
                do
                {
                    // Increment the implicit timestep counter by 1
                    CurrentImplicitTimestep++;

                    // Set the maximum timestep duration to the total time remaining
                    double TimestepDuration = CurrentDeformationEpisodeEndTime - endLastTimestep;

                    // Apply maximum timestep duration cutoff if required
                    // Do not do this if all fracture sets have been deactivated
                    if (useMaxTSDurationCutoff && !AllSetsDeactivated)
                        if (TimestepDuration > maxTimestepDuration)
                            TimestepDuration = maxTimestepDuration;

                    // Recalculate the bulk rock elastic properties
                    // This is mostly done within the FractureDipSet objects, when the FractureDipSet.S_Dipset compliance tensor is retrieved
                    // First we must recalculate the displacement vector and base for the compliance tensor for each fracture dipset
                    // This may have changed as the in situ stress tensor has changed since the previous timestep
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.RecalculateElasticResponse(StressStrain.Sigma_eff);
                        }
                    }
                    for (int I = 0; I < NoUnconfinedFractureSets; I++)
                    {
                        // For the unconfined sets, if the displacement vector has changed, we will also need to recalculate the stress shadow multipliers related to this set
                        UnconfinedFractureSet UFSI = UnconfinedFractureSets[I];
                        bool displacementChanged = UFSI.RecalculateElasticResponse(StressStrain.Sigma_eff);
                        if (checkAllUCFStressShadows && displacementChanged)
                        {
                            for (int J = 0; J < NoUnconfinedFractureSets; J++)
                            {
                                if (I != J)
                                {
                                    UnconfinedFractureSet UFSJ = UnconfinedFractureSets[J];
                                    UCFW_IJ[I, J] = UFSI.getUFSW_IJ(UFSJ);
                                    UCFW_IJ[J, I] = UFSJ.getUFSW_IJ(UFSI);
                                }
                                else
                                {
                                    UCFW_IJ[I, J] = 1;
                                }
                            }
                        }
                    }

                    if (stressLoad)
                    {
                        // If the load is defined in terms of stress, we will use the compliance tensor or bulk rock elastic properties to calculate the elastic strain
                        // In this case we will assume no strain relaxation or additional compactional strain within this timestep

                        // Recalculate the tensors for current elastic strain and rate of change of elastic strain
                        // The method for doing this will depend on the stress distribution scenario
                        switch (SD)
                        {
                            case StressDistribution.EvenlyDistributedStress:
                                // In the evenly distributed stress scenario, the bulk rock compliance tensor will change as the fractures grow
                                // We must therefore use the current bulk rock compliance tensor to recalculate the stress tensors
                                StressStrain.RecalculateStrain(S_beff);
                                break;
                            case StressDistribution.StressShadow:
                                // In the stress shadow scenario, the bulk compliance tensor is isotropic and does not change
                                // We can therefore recalculate the strain tensors from just the Young's Modulus and Poisson's ratio of the host rock
                                StressStrain.RecalculateStrain(E_r, Nu_r);
                                break;
                            case StressDistribution.DuctileBoundary:
                                // Not yet implemented
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        // If the load is defined in terms of strain, we must first calculate the actual elastic horizontal strain rate for this timestep, taking into account strain relaxation
                        // Then we will use partial inversion of the compliance tensor or bulk rock elastic properties to calculate the effective stress

                        // Calculate the tensors for the rate of change of internal elastic strain and compactional strain in this timestep
                        // This will include applied external strain, uplift, fluid overpressure and temperature changes and strain relaxation,
                        // NB the initial elastic strain tensor will be as it was at the end of the previous timestep, or in its default state
                        switch (SRC)
                        {
                            case StrainRelaxationCase.NoStrainRelaxation:
                                {
                                    // If there is no strain relaxation, the rate of change of elastic strain is the applied strain rate.
                                    // NB we need to keep the local copy of the applied strain rate tensor, as the ZZ component of the StressStrain.el_Epsilon_dashed tensor may be changed when calculating the stress tensors
                                    StressStrain.el_Epsilon_dashed = appliedStrainRate;
                                    StressStrain.el_Epsilon_compactional_dashed = compactionalStrainRate;
                                }
                                break;
                            case StrainRelaxationCase.UniformStrainRelaxation:
                                {
                                    // In this scenario the total elastic strain and rate of change of elastic strain both follow exponential curves that are valid across all timesteps, representing the solution to the differential equation combining applied strain and strain relaxation.
                                    // We could therefore calculate exact values for both initial elastic strain and strain rate at the start of each timestep.
                                    // However this would lead to slight discrepencies between the calculated initial elastic strain and the elastic strain accumulated during the previous timestep,
                                    // since the model assumes a constant rate of change of strain during each timestep, rather than an exponential decay. This would be especially noticeable during early timesteps.
                                    // Therefore instead we will use the residual elastic strain at the end of the previous timestep as the initial elastic strain, and to calculate the rate of change of elastic strain.

                                    // To calculate the rate of elastic strain relaxation at the start of the timestep, we must first subtract the initial compactional strain, as this does not undergo relaxation.
                                    Tensor2S el_epsilon_noncomp = StressStrain.el_Epsilon_noncompactional;

                                    // The rate of change of elastic strain is then given by the applied strain rate minus the rate of elastic strain relaxation
                                    // Note that when the initial elastic strain equals the applied strain rate times tr, the rate of change of elastic strain will be zero; this represents equilibrium.
                                    StressStrain.el_Epsilon_dashed = appliedStrainRate - (el_epsilon_noncomp / tr);
                                    StressStrain.el_Epsilon_compactional_dashed = compactionalStrainRate;

                                    // If any of the initial horizontal elastic strain components already at equilibrium value, then the rate of change of these components of the elastic strain will be zero
                                    // We should therefore set them explicitly to zero in the elastic strain rate tensor, to remove nonzero values resulting from to rounding errors
                                    // Also set up a flag indicating whether the elastic strain is static during this timestep (i.e. all horizontal components of the strain rate tensor are zero)
                                    bool StaticStrain = true;
                                    foreach (Tensor2SComponents ij in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.XY })
                                    {
                                        if ((float)el_epsilon_noncomp.Component(ij) == (float)(appliedStrainRate.Component(ij) * tr))
                                            StressStrain.el_Epsilon_dashed.Component(ij, 0);
                                        else
                                            StaticStrain = false;
                                    }

                                    // If necessary we will reduce the maximum timestep duration to avoid overshooting the equilibrium elastic strain
                                    if (!StaticStrain && (TimestepDuration > tr) && !AllSetsDeactivated)
                                        TimestepDuration = tr;
                                }
                                break;
                            case StrainRelaxationCase.FractureOnlyStrainRelaxation:
                                {
                                    // In this scenario the rate of strain relaxation varies with time as the fracture system grows. Therefore the differential equation combining applied strain and strain relaxation also changes with time,
                                    // so there are no exponential curves for the total elastic strain and rate of change of elastic strain that are valid across all timesteps.
                                    // We must therefore use the residual elastic strain at the end of the previous timestep as the initial elastic strain, and to calculate the rate of change of elastic strain.

                                    // The rate of change of elastic strain will be a function of the fracture population, and also the stress distribution scenario. 
                                    // If there are no fractures, it will revert to the No Strain Relaxation scenario where the rate of change of elastic strain is the applied strain rate.
                                    // To calculate the rate of strain relaxation at the start of the timestep, we must first subtract the initial compactional strain, as this does not undergo relaxation.
                                    Tensor2S el_epsilon_noncomp = StressStrain.el_Epsilon_noncompactional;

                                    // The rate of change of elastic strain is then given by the applied strain rate minus the rate of elastic strain relaxation on the fractures
                                    // Note that when the elastic strain accommodated on the fractures [given by depf_depel * bulk rock elastic strain] equals the applied strain rate times tf,
                                    // the rate of change of elastic strain will be zero; this represents equilibrium.
                                    Tensor4_2Sx2S depf_depel = S_F / S_beff;
                                    Tensor2S f_epsilon_noncomp = (depf_depel * el_epsilon_noncomp);
                                    StressStrain.el_Epsilon_dashed = appliedStrainRate - (f_epsilon_noncomp / tf);
                                    StressStrain.el_Epsilon_compactional_dashed = compactionalStrainRate;

                                    // If any of the initial horizontal elastic strain components already at equilibrium value, then the rate of change of these components of the elastic strain will be zero
                                    // We should therefore set them explicitly to zero in the elastic strain rate tensor, to remove nonzero values resulting from rounding errors
                                    // Also set up a flag indicating whether the elastic strain is static during this timestep (i.e. all horizontal components of the strain rate tensor are zero)
                                    bool StaticStrain = true;
                                    foreach (Tensor2SComponents ij in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.XY })
                                    {
                                        if ((float)f_epsilon_noncomp.Component(ij) == (float)(appliedStrainRate.Component(ij) * tf))
                                            StressStrain.el_Epsilon_dashed.Component(ij, 0);
                                        else
                                            StaticStrain = false;
                                    }

                                    // If neccessary we will reduce the maximum timestep duration to avoid overshooting the equilibrium elastic strain
                                    if (!StaticStrain)
                                    {
                                        // In the equilibrium equation for fracture only strain relaxation, the strain rate tensor, a 2nd order tensor is multiplied by depf_depel, a 4th order tensor
                                        // Therefore, unlike in the rock strain relaxation scenario, equilibrium may be reached at different times for different components of the strain tensor
                                        // We will therefore examine each horizontal component in turn to determine the time until equilibrium is reached, and reduce the maximum timestep duration if necessary
                                        // First we will calculate the sum of the squares of the horizontal stain components - we need this to compare the individual components with to see if they can be rounded down to zero
                                        double strain_magnitude_comparator = Math.Pow(f_epsilon_noncomp.Component(Tensor2SComponents.XX), 2) + Math.Pow(f_epsilon_noncomp.Component(Tensor2SComponents.YY), 2) + Math.Pow(f_epsilon_noncomp.Component(Tensor2SComponents.XY), 2);
                                        foreach (Tensor2SComponents ij in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.XY })
                                        {
                                            // Get appropriate components of the noncompactional elastic strain and fracture strain tensors
                                            double epel_ij = el_epsilon_noncomp.Component(ij);
                                            double depf_ij = f_epsilon_noncomp.Component(ij);

                                            // If the fracture strain tensor component is zero, or within rounding error of zero, there is no relaxation of this component so we can move on to the next
                                            if (Math.Pow(depf_ij, 2) <= strain_magnitude_comparator / 1000000)
                                                continue;

                                            // Now insert the two tensor components into the equilibrium equation to determine the time until equilibrium is reached
                                            double timeToEquilibrium = tf * (epel_ij / depf_ij);

                                            // If necessary, reduce the maximum timestep duration to avoid overshooting the equilibrium elastic strain
                                            // NB if the calculated time to equilibrium is zero or negative, we can ignore it
                                            if ((timeToEquilibrium > 0) && (TimestepDuration > timeToEquilibrium) && !AllSetsDeactivated)
                                                TimestepDuration = timeToEquilibrium;
                                        }
                                    }
                                }
                                break;
                            default:
                                break;
                        }

                        // Recalculate the tensors for current in situ stress and rate of change of in situ stress
                        // The method for doing this will depend on the stress distribution scenario
                        switch (SD)
                        {
                            case StressDistribution.EvenlyDistributedStress:
                                // In the evenly distributed stress scenario, the bulk rock compliance tensor will change as the fractures grow
                                // We must therefore use partial inversion of the current bulk rock compliance tensor to recalculate the stress tensors
                                StressStrain.RecalculateEffectiveStressState(S_beff);
                                break;
                            case StressDistribution.StressShadow:
                                // In the stress shadow scenario, the bulk compliance tensor is isotropic and does not change
                                // We can therefore recalculate the stress tensors from just the Young's Modulus and Poisson's ratio of the host rock
                                StressStrain.RecalculateEffectiveStressState(E_r, Nu_r);
                                break;
                            case StressDistribution.DuctileBoundary:
                                // Not yet implemented
                                break;
                            default:
                                break;
                        }
                    }

                    /*// If necessary, recalculate the horizontal strain ratios
                    if (recalculateHorizontalStrainRatios)
                    {
                        foreach (Gridblock_FractureSet fs in FractureSets)
                            fs.RecalculateHorizontalStrainRatios(StressStrain.el_Epsilon_noncompactional);
                        foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                            ufs.RecalculateStrainRatios(StressStrain.el_Epsilon_noncompactional);
                    }*/

                    // Create a new FractureCalculationData object for the current timestep, and populate it with data from the end of the previous timestep
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setTimestepData();
                        }
                    }
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        ufs.setTimestepData();
                    }

                    // Update the macrofracture and unconfined fracture stress shadow widths (which may have changed due to changes in the in situ stress)
                    // If any macrofracture stress shadow widths have changed, this will also update the macrofracture spacing distribution data and clear zone volume
                    bool MFStressShadowWidthChanged = false;
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        if (fs.setStressShadowWidthData())
                            MFStressShadowWidthChanged = true;
                    }
                    // If required, recalculate the inverse stress shadow and clear zone volume multipliers to account for stress shadows from other fracture sets
                    if (checkAlluFStressShadows && MFStressShadowWidthChanged)
                        setCrossFSStressShadows();
                    // If any unconfined fracture stress shadow widths have changed, the unconfined fracture clear zone volume will be updated later
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    bool UCFStressShadowWidthChanged = false;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        if (ufs.setStressShadowWidthData())
                            UCFStressShadowWidthChanged = true;
                    }

                    // Check if any of the fracture sets meet the deactivation criteria, after in situ stress and stress shadow widths have been recalculated 
                    AllSetsDeactivated = true;
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        AllSetsDeactivated &= fs.CheckFractureDeactivation(historic_a_MFP33_termination_ratio, active_total_MFP30_termination_ratio, minimum_MFClearZone_Volume, minrb_maxRad);
                    }
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        AllSetsDeactivated &= ufs.CheckFractureDeactivation(historic_a_UCFP32_termination_ratio, active_total_UCRP30_termination_ratio, minimum_UCFClearZone_Volume, minStaticRayLength);
                    }

                    // Reset the current Fracture Calculation Data, calculate the U and V values and optimal timestep duration for each fracture dip set
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            // Use the getOptimalDuration function in the fracture dip set object to get the optimal timestep duration for that dipset
                            double maxdur = fds.getOptimalDuration(StressStrain.Sigma_eff, StressStrain.Sigma_eff_dashed, d_MFP33);

                            // Check to see if the maximum timstep duration calculated for this timestep is less than the maximum timestep duration so far
                            // NB if it is not possible to calculate a value for the optimal timestep duration, the getOptimalDuration function will return infinity
                            // This will always be greater than any actual calculated optimal duration
                            if (maxdur < TimestepDuration)
                                TimestepDuration = maxdur;
                        }
                    }
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        // Use the getOptimalDuration function in the fracture set object to get the optimal timestep duration for that set
                        double maxdur = ufs.getOptimalDuration(StressStrain.Sigma_eff, StressStrain.Sigma_eff_dashed, d_RayLength, d_UCFP33);

                        // Check to see if the maximum timstep duration calculated for this timestep is less than the maximum timestep duration so far
                        // NB if it is not possible to calculate a value for the optimal timestep duration, the getOptimalDuration function will return infinity
                        // This will always be greater than any actual calculated optimal duration
                        if (maxdur < TimestepDuration)
                            TimestepDuration = maxdur;
                    }


                    // If the timestep duration is still infinity, no further fractures can form; therefore set the current timestep duration to zero and set the flag to stop the calculation at the end of it
                    if (double.IsInfinity(TimestepDuration))
                    {
                        TimestepDuration = 0;
                        CalculationCompleted = true;
                    }

                    // Calculate calculate the driving stress and propagation rate data for each fracture dip set
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setTimestepPropagationData(endLastTimestep, TimestepDuration);
                        }
                    }
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        ufs.setTimestepPropagationData(endLastTimestep, TimestepDuration);
                    }

                    // Calculate the macrofracture deactivation probabilities Phi_II_M and Phi_IJ_M for each fracture dip set
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setMacrofractureDeactivationRate();
                        }
                    }
                    if (CalculateImplicitUCFData)
                        foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                        {
                            ufs.setFractureDeactivationRate();
                        }

                    // Calculate the total half-macrofracture population data for this timestep for each fracture dip set
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.calculateTotalMacrofracturePopulation();
                        }
                    }
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        ufs.updateTotalFracturePopulation();
                    }

                    // Update the macrofracture and unconfined fracture termination arrays
                    updateMFTerminations();
                    updateUCFTerminations();

                    // Calculate and update the macrofracture density, macrofracture spacing distribution and clear zone volume data in the CurrentFractureData object
                    // NB we cannot do this as we calculate the new macrofracture density data for the timestep, because we need to keep the previous values until all macrofracture sets have been calculated
                    // Otherwise we will introduce a bias in the calculation of residual fracture populations based on the order of calculation
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setMacrofractureDensityData();
                        }
                        fs.calculateMacrofractureSpacingDistributionData();
                    }
                    foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    {
                        ufs.setFractureDensityData();
                        ufs.setFractureExclusionZoneData();
                    }

                    // If required, cull the static unconfined fracture population arrays to reduce the number of datapoints
                    if (CurrentImplicitTimestep % ufsDatapointCullFrequency == 0)
                        foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                        {
                            ufs.cullStaticFracturePopulationDatapoints();
                        }

                    // If required, calculate the inverse stress shadow and clear zone volume multipliers to account for stress shadows from other fracture sets
                    if (checkAlluFStressShadows)
                        setCrossFSStressShadows();
                    if (checkAllUCFStressShadows)
                        setCrossUFSStressShadows();

                    // Calculate the new total linear microfracture population data for each fracture dip set, and update the CurrentFractureData object
                    // NB the microfracture densities from one set do not affect the microfracture density calculations for the other sets
                    // so we do not need to calculate the population data for all sets before we can update the CurrentFractureData objects
                    foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            // Calculate the total linear microfracture population data for this timestep for each fracture dip set
                            // NB we cannot calculate uF_P_30(0,t) for power law initial microfracture distribution as this will be infinite
                            fds.calculateTotalMicrofracturePopulation();
                            fds.setMicrofractureDensityData();
                            if (saveMicrofractureDensityDistributionData)
                                fds.setMicrofractureDistributionData();
                        }
                    }

                    // Check if any or all of the fracture sets meet the deactivation criteria, after the fracture densities have been recalculated
                    /*AllSetsDeactivated = true;
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        AllSetsDeactivated = AllSetsDeactivated && fs.CheckFractureDeactivation(historic_a_MFP33_termination_ratio, active_total_MFP30_termination_ratio, minimum_ClearZone_Volume, minrb_maxRad);
                    }*/

                    // Update stress and strain tensors for the next timestep
                    // Update the depth of burial
                    DepthAtDeformation += (TimestepDuration * upliftRate);
                    // Update the effective stress and the bulk rock elastic strain tensors at the end of the timestep, based on the respective rate of change tensors and the timestep duration
                    StressStrain.UpdateStressStrainState(TimestepDuration);
                    // Update total cumulative strain tensor - this increases by the increment in applied external strain
                    StressStrain.tot_Epsilon += (TimestepDuration * appliedStrainRate);
                    // If required, update the cumulative inelastic strain on the fractures
                    // Note that we do not need to update the cumulative inelastic strain in the host rock; this is calculated automatically from the total cumulative strain, the total elastic noncompactional strain and the cumulative inelastic strain on the fractures
                    if (CalculateRelaxedStrainPartitioning)
                    {
                        switch (SRC)
                        {
                            case StrainRelaxationCase.NoStrainRelaxation:
                                // No increment to total or cumulative inelastic strain tensors
                                break;
                            case StrainRelaxationCase.UniformStrainRelaxation:
                                {
                                    // Total increment in relaxed strain = applied strain - increment in elastic strain
                                    Tensor2S relaxedStrain = TimestepDuration * (appliedStrainRate - StressStrain.el_Epsilon_dashed);

                                    // The proportion of this relaxed strain accommodated on the fractures is given by the ratio of the fracture compliance tensor to the bulk rock compliance tensor
                                    // NB by calculating this now, we will include the effect of any growth in the fractures during this timestep; this may therefore give a slightly different result than if calculated at the start of the timestep 
                                    Tensor4_2Sx2S depf_depel = S_F / S_beff;
                                    StressStrain.rel_Epsilon_f += (depf_depel * relaxedStrain);
                                }
                                break;
                            case StrainRelaxationCase.FractureOnlyStrainRelaxation:
                                {
                                    // Total increment in relaxed strain = applied strain - increment in elastic strain
                                    Tensor2S relaxedStrain = TimestepDuration * (appliedStrainRate - StressStrain.el_Epsilon_dashed);

                                    // In this scenario, all the relaxed strain is accommodated on the fractures
                                    StressStrain.rel_Epsilon_f += relaxedStrain;
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    // Update the current end time and the list of timestep end times
                    endLastTimestep += TimestepDuration;
                    TimestepEndTimes.Add(endLastTimestep);

                    // Write data to logfile
                    if (writeImplicitDataToFile)
                    {
                        // Create strings for logging data
                        string timestepData = "";
                        string fractureSetData = "";

                        // Get the minimum and maximum horizontal elastic and total cumulative strain values
                        List<double> minMaxElStrain = StressStrain.el_Epsilon.GetMinMaxHorizontalValues();
                        List<double> minMaxTotStrain = StressStrain.tot_Epsilon.GetMinMaxHorizontalValues();

                        // Write timestep data
                        timestepData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t", CurrentImplicitTimestep, TimestepDuration / timeUnits_Modifier, CurrentImplicitTime / timeUnits_Modifier, minMaxElStrain[0], minMaxElStrain[1], minMaxTotStrain[0], minMaxTotStrain[1]);
#if LOGIMPPOP
                        timestepData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t", CurrentImplicitTimestep, TimestepDuration / timeUnits_Modifier, CurrentImplicitTime / timeUnits_Modifier, StressStrain.Sigma_eff.Component(Tensor2SComponents.XX), StressStrain.Sigma_eff.Component(Tensor2SComponents.YY), StressStrain.Sigma_eff.Component(Tensor2SComponents.XY), StressStrain.Sigma_eff.Component(Tensor2SComponents.ZZ));
                        //timestepData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t", CurrentImplicitTimestep, TimestepDuration / timeUnits_Modifier, CurrentImplicitTime / timeUnits_Modifier, StressStrain.Sigma_dashed.Component(Tensor2SComponents.XX), StressStrain.Sigma_dashed.Component(Tensor2SComponents.YY), StressStrain.Sigma_dashed.Component(Tensor2SComponents.XY), StressStrain.Sigma_dashed.Component(Tensor2SComponents.ZZ));
#endif
                        // Write data for each fracture set
                        foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                        {
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                            {
                                // Get fracture data and add to timestep log string
                                fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t\t\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.DisplacementSense, fds.DisplacementPitch, fds.a_uFP30_total(), fds.s_uFP30_total(), fds.a_uFP32_total(), fds.s_uFP32_total(),
                                    fds.a_MFP30_total(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getMeanMFPropagationRate(), fds.getMeanF(), fds.getMeanStressShadowWidth(), 1 - fds.getInverseStressShadowVolumeAllFS(), fds.getClearZoneVolumeAllFS());
#if LOGIMPPOP
                                //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.Mean_Azimuthal_MF_StressShadowWidth, fds.Mean_Shear_MF_StressShadowWidth, fds.Mean_MF_StressShadowWidth, fds.getInverseStressShadowVolume(), fds.getInverseStressShadowVolumeAllFS(),
                                //    fds.getClearZoneVolume(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
                                //double ts_MeanMFLength = fs.Calculate_MeanPropagationDistance(fds, CurrentImplicitTimestep, new List<double>() { 0 }, PropControl.StressDistributionCase == StressDistribution.EvenlyDistributedStress)[0];
                                //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.getConstantDrivingStressU(), fds.getVariableDrivingStressV(), fds.getMeanMFPropagationRate(), fds.getMFPropagationDistance(), ts_MeanMFLength,
                                //    fds.getClearZoneVolume(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
                                fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.getPhi(), fds.getInstantaneousFII(), fds.getInstantaneousFIJ(), fds.getInverseStressShadowVolume(), fds.getInverseStressShadowVolumeAllFS(), 
                                    fds.a_MFP30_total(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolume());
                                //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.getAA(), fds.getBB(), fds.getCCStep(), fds.getMeanStressShadowWidth(), fds.getMeanShearStressShadowWidth(),
                                //    fds.getInverseStressShadowVolume(), fds.getInverseStressShadowVolumeAllFS(), fds.getClearZoneVolume(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
                                //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t\t\t\t\t\t\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.DisplacementSense, fds.DisplacementPitch, fds.getMeanAzimuthalStressShadowWidth(), fds.getMeanShearStressShadowWidth(), fds.getMeanStressShadowWidth(), fds.getInverseStressShadowVolume(), fds.getInverseStressShadowVolumeAllFS(),
                                //    fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
#endif
                                timestepData = timestepData + fractureSetData;
                            }
                        }
                        // Write data for each unconfined fracture set
                        foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                        {
                            // Get fracture data and add to timestep log string
                            fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.DisplacementSense, ufs.ShearStressPitch, ufs.get_UCRP30(RayPropagationStatus.FullyActive), ufs.get_UCRP30(RayPropagationStatus.Restricted), ufs.get_UCRP30(RayPropagationStatus.ConstantKi), ufs.get_UCRP30(RayPropagationStatus.StaticStressShadow), ufs.get_UCRP30(RayPropagationStatus.StaticIntersection), ufs.get_UCRP30(RayPropagationStatus.StaticMaxRadius),
                                ufs.get_UCRP32(RayPropagationStatus.FullyActive), ufs.get_UCRP32(RayPropagationStatus.Restricted), ufs.get_UCRP32(RayPropagationStatus.ConstantKi), ufs.get_UCRP32(RayPropagationStatus.StaticStressShadow), ufs.get_UCRP32(RayPropagationStatus.StaticIntersection), ufs.get_UCRP32(RayPropagationStatus.StaticMaxRadius), ufs.getStressShadowWidthRatio(), 1 - ufs.getInverseStressShadowVolumeAllFS(), ufs.getClearZoneVolumeAllFS());
#if LOGIMPPOP
                            //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t\t\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.DisplacementSense, ufs.ShearStressPitch, ufs.a_RP33_total(), ufs.r_RP33_total(), ufs.sII_RP33_total(), ufs.sIJ_RP33_total(), ufs.sMR_RP33_total(),
                            //    ufs.a_RP32_total(), ufs.r_RP32_total(), ufs.sII_RP32_total(), ufs.sIJ_RP32_total(), ufs.sMR_RP32_total(), ufs.RP33_exclusive_total(), ufs.RP33_overlapping_total(), ufs.getClearZoneVolume());
                            //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t\t\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.DisplacementSense, ufs.ShearStressPitch, ufs.a_UCRP33_total(), ufs.r_UCRP33_total(), ufs.sII_UCRP33_total(), ufs.sIJ_UCRP33_total(), ufs.sMR_UCRP33_total(),
                            //    ufs.a_UCRP32_total() + ufs.r_UCRP32_total() + ufs.sII_UCRP32_total() + ufs.sIJ_UCRP32_total() + ufs.sMR_UCRP32_total(), ufs.Max_F_StressShadowWidthRatio, ufs.Max_F_StressShadowWidthRatio, ufs.Max_F_StressShadowWidthRatio, ufs.UCFP33_total(), ufs.StressShadowVolume_total(), 1 - ufs.getInverseStressShadowVolume(), ufs.getClearZoneVolume());
                            //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t\t\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.getFracturePropRateCoefficient(), ufs.getCumGamma(), ufs.a_UCRP30_total(), ufs.r_UCRP30_total(), ufs.sII_UCRP30_total(), ufs.sIJ_UCRP30_total(), ufs.sMR_UCRP30_total(),
                            //    ufs.a_UCRP32_total(), ufs.r_UCRP32_total(), ufs.sII_UCRP32_total(), ufs.sIJ_UCRP32_total(), ufs.sMR_UCRP32_total(), 1 - ufs.getInverseStressShadowVolume(), 1 - ufs.getInverseStressShadowVolumeAllFS(), ufs.getClearZoneVolumeAllFS());
                            //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t\t\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.getCumGamma(), ufs.Max_F_StressShadowWidthRatio, ufs.a_UCRP30_total(), ufs.r_UCRP30_total(), ufs.sII_UCRP30_total(), ufs.sIJ_UCRP30_total(), ufs.sMR_UCRP30_total(),
                            //    ufs.a_UCRP31_total(), ufs.r_UCRP31_total(), ufs.sII_UCRP31_total(), ufs.sIJ_UCRP31_total(), ufs.sMR_UCRP31_total(), 1 - ufs.getInverseStressShadowVolume(), 1 - ufs.getInverseStressShadowVolumeAllFS(), ufs.getClearZoneVolumeAllFS());
                            //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t\t\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.previous_LRP30, ufs.min_NucleatingDatapoint_RP30, ufs.a_UCRP30_total(), ufs.r_UCRP30_total(), ufs.sII_UCRP30_total(), ufs.sIJ_UCRP30_total(), ufs.sMR_UCRP30_total(),
                            //    ufs.a_UCRP32_total(), ufs.r_UCRP32_total(), ufs.sII_UCRP32_total(), ufs.sIJ_UCRP32_total(), ufs.sMR_UCRP32_total(), 1 - ufs.getInverseStressShadowVolume(), 1 - ufs.getInverseStressShadowVolumeAllFS(), ufs.getClearZoneVolumeAllFS());
                            fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t\t\t\t", ufs.getEvolutionStage(), ufs.getFinalDrivingStressSigmaD(), ufs.Max_F_StressShadowWidthRatio, ufs.a_UCRP30_total(), ufs.r_UCRP30_total(), ufs.sII_UCRP30_total(), ufs.sIJ_UCRP30_total(), ufs.sMR_UCRP30_total(),
                                ufs.a_UCRP31_total() + ufs.r_UCRP31_total(), ufs.sII_UCRP31_total() + ufs.sIJ_UCRP31_total() + ufs.sMR_UCRP31_total(), ufs.a_UCRP32_total() + ufs.r_UCRP32_total(), ufs.sII_UCRP32_total() + ufs.sIJ_UCRP32_total() + ufs.sMR_UCRP32_total(), ufs.a_UCRP33_total() + ufs.r_UCRP33_total(), ufs.sII_UCRP33_total() + ufs.sIJ_UCRP33_total() + ufs.sMR_UCRP33_total(), 1 - ufs.getInverseStressShadowVolume(), 1 - ufs.getInverseStressShadowVolumeAllFS(), ufs.getClearZoneVolumeAllFS());
#endif

                            timestepData = timestepData + fractureSetData;
                        }

                        // If required, write the fracture porosity data
                        // Currently configured to output porosity data for all aperture types 
                        if (CalculateFracturePorosity)
                        {
                            Dictionary<FractureApertureType, double> uFPorosity = new Dictionary<FractureApertureType, double>();
                            Dictionary<FractureApertureType, double> MFPorosity = new Dictionary<FractureApertureType, double>();
                            Dictionary<FractureApertureType, double> UCFPorosity = new Dictionary<FractureApertureType, double>();

                            uFPorosity.Add(FractureApertureType.Uniform, 0);
                            MFPorosity.Add(FractureApertureType.Uniform, 0);
                            UCFPorosity.Add(FractureApertureType.Uniform, 0);
                            uFPorosity.Add(FractureApertureType.SizeDependent, 0);
                            MFPorosity.Add(FractureApertureType.SizeDependent, 0);
                            UCFPorosity.Add(FractureApertureType.SizeDependent, 0);
                            uFPorosity.Add(FractureApertureType.Dynamic, 0);
                            MFPorosity.Add(FractureApertureType.Dynamic, 0);
                            UCFPorosity.Add(FractureApertureType.Dynamic, 0);
                            uFPorosity.Add(FractureApertureType.BartonBandis, 0);
                            MFPorosity.Add(FractureApertureType.BartonBandis, 0);
                            UCFPorosity.Add(FractureApertureType.BartonBandis, 0);

                            foreach (FractureApertureType apertureType in Enum.GetValues(typeof(FractureApertureType)).Cast<FractureApertureType>())
                            {
                                foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                                {
                                    uFPorosity[apertureType] += fs.combined_uF_Porosity(apertureType);
                                    MFPorosity[apertureType] += fs.combined_MF_Porosity(apertureType);
                                }
                                foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                                {
                                    UCFPorosity[apertureType] += ufs.Total_UCF_Porosity(apertureType);
                                }
                            }

                            string porosityData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t", uFPorosity[FractureApertureType.Uniform], MFPorosity[FractureApertureType.Uniform], UCFPorosity[FractureApertureType.Uniform], uFPorosity[FractureApertureType.SizeDependent], MFPorosity[FractureApertureType.SizeDependent], UCFPorosity[FractureApertureType.SizeDependent], uFPorosity[FractureApertureType.Dynamic], MFPorosity[FractureApertureType.Dynamic], UCFPorosity[FractureApertureType.Dynamic], uFPorosity[FractureApertureType.BartonBandis], MFPorosity[FractureApertureType.BartonBandis], UCFPorosity[FractureApertureType.BartonBandis]);
                            timestepData += porosityData;
                        }
                        if (CalculateFracturePermeabilityTensor)
                        {
                            string uFPermeabilityTensorComponents = "";
                            string MFPermeabilityTensorComponents = "";
                            string UCFPermeabilityTensorComponents = "";
                            string TFPermeabilityTensorComponents = "";
                            Tensor2S uFPermeabilityTensor = MicrofracturePermeability();
                            Tensor2S MFPermeabilityTensor = MacrofracturePermeability();
                            Tensor2S UCFPermeabilityTensor = UnconfinedFracturePermeability();
                            Tensor2S TFPermeabilityTensor = TotalFracturePermeability();

                            Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                            foreach (Tensor2SComponents ij in tensorComponents)
                            {
                                uFPermeabilityTensorComponents += string.Format("{0}\t", uFPermeabilityTensor.Component(ij));
                                MFPermeabilityTensorComponents += string.Format("{0}\t", MFPermeabilityTensor.Component(ij));
                                UCFPermeabilityTensorComponents += string.Format("{0}\t", UCFPermeabilityTensor.Component(ij));
                                TFPermeabilityTensorComponents += string.Format("{0}\t", TFPermeabilityTensor.Component(ij));
                            }
                            uFPermeabilityTensorComponents += string.Format("{0}\t", MicrofractureSigmaFactor());
                            MFPermeabilityTensorComponents += string.Format("{0}\t", MacrofractureSigmaFactor());
                            UCFPermeabilityTensorComponents += string.Format("{0}\t", UnconfinedFractureSigmaFactor());
                            TFPermeabilityTensorComponents += string.Format("{0}\t", TotalFractureSigmaFactor());

                            timestepData += uFPermeabilityTensorComponents;
                            timestepData += MFPermeabilityTensorComponents;
                            timestepData += UCFPermeabilityTensorComponents;
                            timestepData += TFPermeabilityTensorComponents;
                        }
                        if (OutputBulkRockElasticTensors)
                        {
                            // NB here we output the bulk rock compliance tensor rather than the effective bulk rock compliance tensor. This will include the effect of the fractures, even in the stress shadow scenario
                            // We will also output the bulk rock stiffness tensor, obtained by inverting the compliance tensor
                            string complianceTensorComponents = "";
                            string stiffnessTensorComponents = "";
                            Tensor4_2Sx2S complianceTensor = S_b;
                            Tensor4_2Sx2S stiffnessTensor = complianceTensor.Inverse();

                            Tensor2SComponents[] tensorComponents = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                            foreach (Tensor2SComponents ij in tensorComponents)
                                foreach (Tensor2SComponents kl in tensorComponents)
                                {
                                    complianceTensorComponents += string.Format("{0}\t", complianceTensor.Component(ij, kl));
                                    stiffnessTensorComponents += string.Format("{0}\t", stiffnessTensor.Component(ij, kl));
                                }
                            timestepData += complianceTensorComponents;
                            timestepData += stiffnessTensorComponents;
                        }

                        // Write timestep data to log file
                        outputFile.WriteLine(timestepData);
                    }

#if LOGIMPPOP
                    Console.WriteLine(string.Format("TS {0}, duration {1}, Theta {2}, NoDP FA {3}, R {4}, SII {5}, SIJ {6}, SMax {7}", CurrentImplicitTimestep, TimestepDuration,
                        UFSToLog.getInverseStressShadowVolumeAllFS(), UFSToLog.getNoDatapoints(RayPropagationStatus.FullyActive),
                        UFSToLog.getNoDatapoints(RayPropagationStatus.Restricted), UFSToLog.getNoDatapoints(RayPropagationStatus.StaticStressShadow),
                        UFSToLog.getNoDatapoints(RayPropagationStatus.StaticIntersection), UFSToLog.getNoDatapoints(RayPropagationStatus.StaticMaxRadius)));
                    string TAdataoutput = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t", CurrentImplicitTimestep, TimestepDuration, endLastTimestep, UFSToLog.getFinalDrivingStressSigmaD(), UFSToLog.getTotalUCFP33(), UFSToLog.getInverseStressShadowVolumeAllFS());
                    foreach (RayPropagationStatus rayType in rayTypesToLog)
                    {
                        int noDatapoints = UFSToLog.getNoDatapoints(rayType);
                        string rayTypeDatapointOutput = TAdataoutput + string.Format("{0}\t\t", noDatapoints);
                        List<int> datapointIndexNos = UFSToLog.getDatapointIndexNos(rayType);
                        List<double> dP30List = UFSToLog.getdP30Values(rayType);
                        List<double> rayLengthList = UFSToLog.getRayLengths(rayType);
                        List<double> effRayLengthList = UFSToLog.getEffectiveRayLengths(rayType);
                        List<double> phiIIList = UFSToLog.getPhiIIValues(rayType);
                        List<double> phiIJList = UFSToLog.getPhiIJValues(rayType);
                        for (int datapointNo = 0; datapointNo < noDatapoints; datapointNo++)
                            rayTypeDatapointOutput += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t\t", datapointIndexNos[datapointNo], dP30List[datapointNo], rayLengthList[datapointNo], effRayLengthList[datapointNo], phiIIList[datapointNo], phiIJList[datapointNo]);
                        rayLogFiles[rayType].WriteLine(rayTypeDatapointOutput);
                    }
#endif

                    // Check if calculation is finished
                    // Check if we have run to completion
                    if (CurrentImplicitTime >= CurrentDeformationEpisodeEndTime)
                        CalculationCompleted = true;
                    // Check if we have exceeded maximum number of timesteps
                    if (CurrentImplicitTimestep >= maxTimesteps)
                    {
                        CalculationCompleted = true;
                        WithinTimestepLimit = false;
                    }
                    // Check if all fracture sets are deactivated
                    if (AllSetsDeactivated && StopWhenAllSetsDeactivated)
                        CalculationCompleted = true;

                } while (!CalculationCompleted); // Move on to the next timestep

            } // Move on to the next deformation episode

            // Calculate cumulative population distribution function arrays
            if (CalculatePopulationDistributionData)
            {
                int cullValue = 0;
                int no_l_IndexPoints = PropControl.no_l_indexPoints;
                if (no_l_IndexPoints > 0)
                    cullValue = CurrentImplicitTimestep / no_l_IndexPoints;
                double maxMFLengthMultiplier = 4;

                double maxHMinLength = PropControl.max_HMin_l_indexPoint_Length;
                double maxHMaxLength = PropControl.max_HMax_l_indexPoint_Length;

                // Loop through all layer-bound fracture sets
                for (int fs_index = 0; fs_index < NoLayerBoundFractureSets; fs_index++)
                {
                    LayerBoundFractureSet fs = LayerBoundFractureSets[fs_index];

                    // Calculate the maximum length for the macrofracture cumulative population distribution function index values based on orientation
                    // If this has not been specified, calculate this by applying a multiplier to the mean macrofracture length
                    double maxIndexLength;
                    if (!(maxHMinLength > 0) || !(maxHMaxLength > 0))
                    {
                        double denominator = fs.combined_T_MFP30_total() * ThicknessAtDeformation;
                        maxIndexLength = (denominator > 0 ? fs.combined_T_MFP32_total() / denominator : 0);
                        maxIndexLength *= maxMFLengthMultiplier;
                        // If the maximum possible macrofracture length is less than this, reduce the maximum index length to the maximum possible macrofracture length
                        // This will be the case if there is no fracture deactivation (uniaxial or anisotropic evenly distributed stress models)
                        double maxMFlength = 0;
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            double dipsetMaxMFlength = fds.getCumulativeMFPropagationDistance(CurrentImplicitTimestep, 0);
                            if (maxMFlength < dipsetMaxMFlength)
                                maxMFlength = dipsetMaxMFlength;
                        }
                        if (maxIndexLength > maxMFlength)
                            maxIndexLength = maxMFlength;
                    }
                    else if (fs_index == 0)
                    {
                        maxIndexLength = maxHMinLength;
                    }
                    else if ((fs_index == (NoLayerBoundFractureSets / 2)) && ((NoLayerBoundFractureSets % 2) == 0))
                    {
                        maxIndexLength = maxHMaxLength;
                    }
                    else
                    {
                        double relativeAngle = Math.PI * ((double)fs_index / (double)NoLayerBoundFractureSets);
                        double HMinComponent = Math.Pow(VectorXYZ.Cos_trim(relativeAngle), 2);
                        double HMaxComponent = Math.Pow(VectorXYZ.Sin_trim(relativeAngle), 2);
                        maxIndexLength = (maxHMinLength * HMinComponent) + (maxHMaxLength * HMaxComponent);
                    }

                    int NoDipSets = fs.FractureDipSets.Count;
                    List<string> dipSetLabels = fs.DipSetLabels();
                    for (int dipsetIndex = 0; dipsetIndex < NoDipSets; dipsetIndex++)
                    {
                        // Get a reference to the fracture dip set object
                        FractureDipSet fds = fs.FractureDipSets[dipsetIndex];

                        // Reset the macrofracture index array
                        // Check to see if a maximum length has been set for the macrofracture cumulative population distribution function index values
                        if (maxIndexLength > 0) // If a maximum length has been set, generate the halflength index array manually
                            fds.reset_MF_halflength_array(no_l_IndexPoints, maxIndexLength);
                        else // otherwise generate the index array automatically using the reset_MF_halflength_array function
                            fds.reset_MF_halflength_array(cullValue);

                        // Call the calculation function for the macrofracture cumulative population distribution function arrays
                        fds.calculateCumulativeMacrofracturePopulationArrays();

                        // Call the calculation function for the macrofracture cumulative population distribution function arrays
                        fds.calculateCumulativeMicrofracturePopulationArrays();

                        // Write data to logfile
                        if (writeImplicitDataToFile)
                        {
                            // Create strings for header data and write to file
                            string headerData = string.Format("FS {0} {1}", (useSetNames ? getLayerBoundFractureSetName(fs_index) : fs_index.ToString()), (useDipSetNames ? dipSetLabels[dipsetIndex] : string.Format("Dipset {0}", dipsetIndex)));
                            outputFile.WriteLine(headerData);

                            // Write macrofracture data
                            {
                                // Create stings for macrofracture data
                                /*string indexData = string.Format("Half-length\t{0}\t", 0);
                                string a_MFP30_data = string.Format("a_MFP30\t{0}\t", fds.a_MFP30_total());
                                string sII_MFP30_data = string.Format("sII_MFP30\t{0}\t", fds.sII_MFP30_total());
                                string sIJ_MFP30_data = string.Format("sIJ_MFP30\t{0}\t", fds.sIJ_MFP30_total());
                                string a_MFP32_data = string.Format("a_MFP32\t{0}\t", fds.a_MFP32_total());
                                string s_MFP32_data = string.Format("s_MFP32\t{0}\t", fds.s_MFP32_total());*/
                                string indexData = "Half-length\t";
                                string a_MFP30_data = "a_MFP30\t";
                                string sII_MFP30_data = "sII_MFP30\t";
                                string sIJ_MFP30_data = "sIJ_MFP30\t";
                                string a_MFP32_data = "a_MFP32\t";
                                string s_MFP32_data = "s_MFP32\t";

                                // Loop through each point in the index value array and write data for that point
                                int noIndexPoints = fds.MF_halflengths.Length;
                                for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                                {
                                    indexData += string.Format("{0}\t", fds.MF_halflengths[indexPoint]);
                                    a_MFP30_data += string.Format("{0}\t", fds.a_MFP30(indexPoint));
                                    sII_MFP30_data += string.Format("{0}\t", fds.sII_MFP30(indexPoint));
                                    sIJ_MFP30_data += string.Format("{0}\t", fds.sIJ_MFP30(indexPoint));
                                    a_MFP32_data += string.Format("{0}\t", fds.a_MFP32(indexPoint));
                                    s_MFP32_data += string.Format("{0}\t", fds.s_MFP32(indexPoint));
                                }

                                // Write all array data to log file
                                outputFile.WriteLine(indexData);
                                outputFile.WriteLine(a_MFP30_data);
                                outputFile.WriteLine(sII_MFP30_data);
                                outputFile.WriteLine(sIJ_MFP30_data);
                                outputFile.WriteLine(a_MFP32_data);
                                outputFile.WriteLine(s_MFP32_data);
                            }

                            // Write microfracture data
                            {
                                // Create stings for macrofracture data
                                /*string indexData = string.Format("Radius\t{0}\t", 0);
                                string a_uFP30_data = string.Format("a_uFP30\t{0}\t", fds.a_uFP30_total());
                                string s_uFP30_data = string.Format("s_uFP30\t{0}\t", fds.s_uFP30_total());
                                string a_uFP32_data = string.Format("a_uFP32\t{0}\t", fds.a_uFP32_total());
                                string s_uFP32_data = string.Format("s_uFP32\t{0}\t", fds.s_uFP32_total());
                                string a_uFP33_data = string.Format("a_uFP33\t{0}\t", fds.a_uFP33_total());
                                string s_uFP33_data = string.Format("s_uFP33\t{0}\t", fds.s_uFP33_total());*/
                                string indexData = "Radius\t";
                                string a_uFP30_data = "a_uFP30\t";
                                string s_uFP30_data = "s_uFP30\t";
                                string a_uFP32_data = "a_uFP32\t";
                                string s_uFP32_data = "s_uFP32\t";
                                string a_uFP33_data = "a_uFP33\t";
                                string s_uFP33_data = "s_uFP33\t";

                                // Loop through each point in the index value array and write data for that point
                                int noIndexPoints = fds.uF_radii.Length;
                                for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                                {
                                    indexData += string.Format("{0}\t", fds.uF_radii[indexPoint]);
                                    a_uFP30_data += string.Format("{0}\t", fds.a_uFP30(indexPoint));
                                    s_uFP30_data += string.Format("{0}\t", fds.s_uFP30(indexPoint));
                                    a_uFP32_data += string.Format("{0}\t", fds.a_uFP32(indexPoint));
                                    s_uFP32_data += string.Format("{0}\t", fds.s_uFP32(indexPoint));
                                    a_uFP33_data += string.Format("{0}\t", fds.a_uFP33(indexPoint));
                                    s_uFP33_data += string.Format("{0}\t", fds.s_uFP33(indexPoint));
                                }

                                // Write all array data to log file
                                outputFile.WriteLine(indexData);
                                outputFile.WriteLine(a_uFP30_data);
                                outputFile.WriteLine(s_uFP30_data);
                                outputFile.WriteLine(a_uFP32_data);
                                outputFile.WriteLine(s_uFP32_data);
                                outputFile.WriteLine(a_uFP33_data);
                                outputFile.WriteLine(s_uFP33_data);
                            }
                        } // End write data to logfile
                    } // End loop through the fracture dip sets
                } // End loop through the fracture sets

                // Loop through all unconfined fracture sets
                for (int ufs_index = 0; ufs_index < NoUnconfinedFractureSets; ufs_index++)
                {
                    UnconfinedFractureSet ufs = UnconfinedFractureSets[ufs_index];

                    // Write data to logfile
                    if (writeImplicitDataToFile)
                    {
                        // Calculate the maximum radius for the unconfined fracture ray cumulative population distribution function index values based on orientation
                        // If this has not been specified, use the maximum unconfined fracture radius
                        double maxIndexRadius;
                        if (!(maxHMinLength > 0) || !(maxHMaxLength > 0))
                        {
                            maxIndexRadius = ufs.MaximumFractureRadius;
                        }
                        else
                        {
                            double relativeAngle = PointXYZ.getAngularDifference(ufs.Azimuth, Hmin_azimuth);
                            double HMinComponent = Math.Pow(VectorXYZ.Cos_trim(relativeAngle), 2);
                            double HMaxComponent = Math.Pow(VectorXYZ.Sin_trim(relativeAngle), 2);
                            maxIndexRadius = (maxHMinLength * HMinComponent) + (maxHMaxLength * HMaxComponent);
                        }
                        // Get the minimum fracture radius
                        double minIndexRadius = ufs.MinimumFractureRadius;

                        // Create a local unconfined fracture ray index array
                        List<double> indexRadii = new List<double>();

                        // Add the first index point at zero
                        indexRadii.Add(0);

                        // Add the required number of intermediate index points on a logarithmic scale
                        for (int indexPoint_no = 1; indexPoint_no < no_l_IndexPoints; indexPoint_no++)
                        {
                            double indexRatio = (double)(indexPoint_no - 1) / (double)(no_l_IndexPoints - 1);
                            if (ufs.FractureDistribution == StressDistribution.EvenlyDistributedStress)
                            {
                                double logNewValue = (1 - indexRatio) * Math.Log(1 + maxIndexRadius - minIndexRadius);
                                indexRadii.Add(maxIndexRadius + 1 - Math.Exp(logNewValue));
                            }
                            else
                            {
                                double logNewValue = (indexRatio * Math.Log(maxIndexRadius)) + ((1 - indexRatio) * Math.Log(minIndexRadius));
                                indexRadii.Add(Math.Exp(logNewValue));
                            }
                        }

                        // Add the final index point
                        indexRadii.Add(maxIndexRadius);

                        // Call the calculation functions for the P30 and P32 cumulative population distribution function arrays
                        Dictionary<RayPropagationStatus, List<double>> P30values = new Dictionary<RayPropagationStatus, List<double>>();
                        Dictionary<RayPropagationStatus, List<double>> P32values = new Dictionary<RayPropagationStatus, List<double>>();
                        foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                        {
                            P30values[status] = ufs.GetCumulativeUCRP30Values(indexRadii, status);
                            P32values[status] = ufs.GetCumulativeUCRP32Values(indexRadii, status);
                        }

                        // Create strings for header data and write to file
                        string headerData = string.Format("Unconfined set {0}: Strike {1} Dip {2}", ufs_index, (int)(ufs.Strike * 180 / Math.PI), (int)(ufs.Dip * 180 / Math.PI));
                        outputFile.WriteLine(headerData);

                        // Write unconfined fracture data
                        {
                            // Create stings for unconfined fracture data
                            string indexData = "Radius\t";
                            string a_RP30_data = "a_RP30\t";
                            string r_RP30_data = "r_RP30\t";
                            string c_RP30_data = "r_RP30\t";
                            string sII_RP30_data = "sII_RP30\t";
                            string sIJ_RP30_data = "sIJ_RP30\t";
                            string sMR_RP30_data = "sMR_RP30\t";
                            string Total_RP30_data = "Total_RP30\t";
                            string a_RP32_data = "a_RP32\t";
                            string r_RP32_data = "r_RP32\t";
                            string c_RP32_data = "r_RP32\t";
                            string sII_RP32_data = "sII_RP32\t";
                            string sIJ_RP32_data = "sIJ_RP32\t";
                            string sMR_RP32_data = "sMR_RP32\t";
                            string Total_RP32_data = "Total_RP32\t";

                            // Loop through each point in the index value array and write data for that point
                            int noIndexPoints = indexRadii.Count;
                            for (int indexPoint = 0; indexPoint < noIndexPoints; indexPoint++)
                            {
                                double a_RP30 = P30values[RayPropagationStatus.FullyActive][indexPoint];
                                double r_RP30 = P30values[RayPropagationStatus.Restricted][indexPoint];
                                double c_RP30 = P30values[RayPropagationStatus.ConstantKi][indexPoint];
                                double sII_RP30 = P30values[RayPropagationStatus.StaticStressShadow][indexPoint];
                                double sIJ_RP30 = P30values[RayPropagationStatus.StaticIntersection][indexPoint];
                                double sMR_RP30 = P30values[RayPropagationStatus.StaticMaxRadius][indexPoint];
                                double Total_RP30 = a_RP30 + r_RP30 + c_RP30 + sII_RP30 + sIJ_RP30 + sMR_RP30;
                                double a_RP32 = P32values[RayPropagationStatus.FullyActive][indexPoint];
                                double r_RP32 = P32values[RayPropagationStatus.Restricted][indexPoint];
                                double c_RP32 = P32values[RayPropagationStatus.ConstantKi][indexPoint];
                                double sII_RP32 = P32values[RayPropagationStatus.StaticStressShadow][indexPoint];
                                double sIJ_RP32 = P32values[RayPropagationStatus.StaticIntersection][indexPoint];
                                double sMR_RP32 = P32values[RayPropagationStatus.StaticMaxRadius][indexPoint];
                                double Total_RP32 = a_RP32 + r_RP32 + c_RP32 + sII_RP32 + sIJ_RP32 + sMR_RP32;

                                indexData += string.Format("{0}\t", indexRadii[indexPoint]);
                                a_RP30_data += string.Format("{0}\t", a_RP30);
                                r_RP30_data += string.Format("{0}\t", r_RP30);
                                c_RP30_data += string.Format("{0}\t", c_RP30);
                                sII_RP30_data += string.Format("{0}\t", sII_RP30);
                                sIJ_RP30_data += string.Format("{0}\t", sIJ_RP30);
                                sMR_RP30_data += string.Format("{0}\t", sMR_RP30);
                                Total_RP30_data += string.Format("{0}\t", Total_RP30);
                                a_RP32_data += string.Format("{0}\t", a_RP32);
                                r_RP32_data += string.Format("{0}\t", r_RP32);
                                c_RP32_data += string.Format("{0}\t", c_RP32);
                                sII_RP32_data += string.Format("{0}\t", sII_RP32);
                                sIJ_RP32_data += string.Format("{0}\t", sIJ_RP32);
                                sMR_RP32_data += string.Format("{0}\t", sMR_RP32);
                                Total_RP32_data += string.Format("{0}\t", Total_RP32);
                            }

                            // Write all array data to log file
                            outputFile.WriteLine(indexData);
                            outputFile.WriteLine(a_RP30_data);
                            outputFile.WriteLine(r_RP30_data);
                            outputFile.WriteLine(c_RP30_data);
                            outputFile.WriteLine(sII_RP30_data);
                            outputFile.WriteLine(sIJ_RP30_data);
                            outputFile.WriteLine(sMR_RP30_data);
                            outputFile.WriteLine(Total_RP30_data);
                            outputFile.WriteLine(a_RP32_data);
                            outputFile.WriteLine(r_RP32_data);
                            outputFile.WriteLine(c_RP32_data);
                            outputFile.WriteLine(sII_RP32_data);
                            outputFile.WriteLine(sIJ_RP32_data);
                            outputFile.WriteLine(sMR_RP32_data);
                            outputFile.WriteLine(Total_RP32_data);
                        }
                    } // End write data to logfile
                } // End loop through the unconfined fracture sets
            } // End calculate cumulative population distribution function arrays

            if (writeImplicitDataToFile)
            {
                // If using the present day stress to calculate fracture aperture, output the present day aperture and reactivation potential of each fracture dip set
                bool outputApertureReactivationPotentialTable = UsePresentDayStress;
                // If we have more than 2 fracture sets, output a table of connectivity between fracture sets for the final fracture network
                bool outputConnectivityTable = (NoLayerBoundFractureSets > 2);

                string tableTitle = "";
                string headerLine1 = "";
                string headerLine2 = "";

                // Write table header
                if (outputApertureReactivationPotentialTable)
                {
                    tableTitle += "Present day fracture aperture and reactivation potential\t\t\t\t\t\t\t";
                    headerLine1 += "\t\t\t\t\t\t\t";
                    headerLine2 += "Fracture set\tAperture (m)\tDilatancy potential (Pa)\tSlip potential (Pa)\tReactivation sense\t\t\t";
                }
                if (outputConnectivityTable)
                {
                    tableTitle += "Fracture interconnectivity: volumetric density (P30) of macrofracture tips from fracture set I terminating against macrofractures from dipset Jm\t";
                    headerLine1 += "Terminating fracture dipset(Jm):\tPropagating fracture set(I):";
                    headerLine2 += "\t";
                    for (int fsI_index = 0; fsI_index < NoLayerBoundFractureSets; fsI_index++)
                    {
                        tableTitle += "\t";
                        headerLine1 += "\t";
                        headerLine2 += string.Format("FS {0}\t", (useSetNames ? getLayerBoundFractureSetName(fsI_index) : fsI_index.ToString()));
                    }
                }
                outputFile.WriteLine();
                outputFile.WriteLine(tableTitle);
                outputFile.WriteLine(headerLine1);
                outputFile.WriteLine(headerLine2);

                // Write table data
                for (int fs_index = 0; fs_index < NoLayerBoundFractureSets; fs_index++)
                {
                    LayerBoundFractureSet fs = LayerBoundFractureSets[fs_index];
                    int noDipSets = fs.FractureDipSets.Count;
                    for (int dipSetIndex = 0; dipSetIndex < noDipSets; dipSetIndex++)
                    {
                        FractureDipSet dipSet = fs.FractureDipSets[dipSetIndex];
                        string tableRow = "";
                        string dipsetName = string.Format("FS {0} {1}", fs_index, dipSet.Mode);

                        if (outputApertureReactivationPotentialTable)
                        {
                            tableRow += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", dipsetName, dipSet.getMeanMacrofractureAperture(), dipSet.PresentDayDilatancyPotential, dipSet.PresentDaySlipPotential, dipSet.MostLikelyReactivationSense);
                        }

                        if (outputConnectivityTable)
                        {
                            tableRow += dipsetName + "\t";
                            for (int fsI_index = 0; fsI_index < NoLayerBoundFractureSets; fsI_index++)
                                tableRow += string.Format("{0}\t", MFTerminations[fsI_index, fs_index][dipSetIndex]);
                        }
                        outputFile.WriteLine(tableRow);
                    }
                }

                // Close the log file
                outputFile.Close();
            }

#if LOGIMPPOP
            foreach (RayPropagationStatus rayType in rayTypesToLog)
                rayLogFiles[rayType].Close();
#endif

            // To free space at the end of the run we will clear the implicit fracture population function datapoint arrays for the unconfined fractures
            // We must therefore ensure that any data that may be required later is saved in the FractureCalculationData list
            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                ufs.clearImplicitFracturePopulationArrays();

            // Determine the return code and return it
            CalculateFractureDataReturnCode returnCode = (WithinTimestepLimit ? CalculateFractureDataReturnCode.Completed : CalculateFractureDataReturnCode.TimestepLimitExceeded);
            return returnCode;
        }
        /// <summary>
        /// Calculate fracture data based on user-specified PropagationControl object, building on existing fracture populations 
        /// </summary>
        /// <param name="pc_in">PropagationControl object containing propagation control data</param>
        /// <returns>CalculateFractureDataReturnCode object indicating if the calculation ran to completion or errors were encountered</returns>
        public CalculateFractureDataReturnCode CalculateFractureData(PropagationControl pc_in)
        {
            PropControl = pc_in;
            return CalculateFractureData();
        }
        /// <summary>
        /// Grow the explicit DFN for the next timestep, based on data from the implicit model calculation, cached in the appropriate FCD_list objects
        /// </summary>
        /// <param name="global_DFN">Reference to GlobalDFN object containing the global DFN</param>
        /// <param name="DFNControl">Reference to DFNControl object containing control data for DFN generation</param>
        /// <returns>PropagateDFNReturnCode object indicating if the calculation ran to completion or errors were encountered</returns>
        public PropagateDFNReturnCode PropagateDFN(GlobalDFN global_DFN, DFNGenerationControl DFNControl)
        {
            // Check gridblock geometry is defined, if not abort
            if (!checkCornerpointsDefined()) return PropagateDFNReturnCode.GridblockGeometryError;

            // Update the current timestep counter
            CurrentExplicitTimestep++;

            // Cache constants locally
            double max_uF_radius = MaximumMicrofractureRadius;
            double SqrtPi = Math.Sqrt(Math.PI);
            double CapA = MechProps.CapA;
            double b = MechProps.b_factor;
            bool bis2 = (MechProps.GetbType() == bType.Equals2);
            double beta = MechProps.beta;
            // initial_uF_factor is a component related to the maximum microfracture radius rmax, included in Cum_hGamma to represent the initial population of seed macrofractures: ln(rmax) for b=2; rmax^(1/beta) for b!=2
            double initial_uF_factor = Initial_uF_factor;
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb1_factor = (bis2 ? ThicknessAtDeformation / 2 : Math.Pow(ThicknessAtDeformation / 2, b / 2));
            double PreviousExplicitTime = (CurrentExplicitTimestep > 0) ? TimestepEndTimes[CurrentExplicitTimestep - 1] : 0;
            double TimestepDuration = CurrentExplicitTime - PreviousExplicitTime;
            double uF_minRadius = DFNControl.MicrofractureDFNMinimumRadius;
            double MF_minLength = DFNControl.MacrofractureDFNMinimumLength;
            // NB If the global minimum unconfined fracture radius is undefined (negative), we will use the local minimum unconfined fracture radius defined for the unconfined fracture set
            // If the global minimum unconfined fracture radius is 0 we will not generate any explicit unconfined fractures
            double Global_UCF_minRadius = DFNControl.UnconfinedFractureDFNMinimumRadius;
            bool SpecifyFractureNucleationPosition = (PropControl.FractureNucleationPosition >= 0);
            double FractureNucleationPosition_w = PropControl.FractureNucleationPosition;
            // rminb_factor is rmin^(b/2), = rmin if b=2
            //double rminb_factor = (bis2 ? UCF_minRadius : Math.Pow(UCF_minRadius, b / 2));

            // Flags for populating different fracture types and geometries
            bool calc_uF = ((uF_minRadius > 0) && (uF_minRadius < max_uF_radius));
            bool calc_MF = (MF_minLength >= 0);
            bool calc_UCF = ((NoUnconfinedFractureSets > 0) && (Global_UCF_minRadius != 0));
            bool add_MF_directly = calc_MF && !calc_uF; // We do not need to add macrofractures directly to the DFN if it includes microfractures - macrofractures will be nucleated automatically when microfracture radius reaches h/2
            bool use_MF_min_length_cutoff = (MF_minLength > 0) && !calc_uF; // There cannot be a minimum macrofracture cutoff length if the DFN includes microfractures
            bool checkStressShadow = (PropControl.StressDistributionCase == StressDistribution.StressShadow);
            bool checkAlluFStressShadows = PropControl.checkAlluFStressShadows && checkStressShadow;
            bool checkAllUCFStressShadows = PropControl.checkAllUCFStressShadows && checkStressShadow;
            bool TerminateAtGridBoundary = DFNControl.CropToGrid;
            double probabilisticFractureNucleationLimit = DFNControl.probabilisticFractureNucleationLimit;
            bool allowProbabilisticFractureNucleation = (probabilisticFractureNucleationLimit > 0);
            bool searchNeighbouringGridblocks = SearchNeighbouringGridblocks();
            // Minimum radius for large fractures; fractures larger than this will be considered to influence the entire grid when checking stress shadows
            double Minimum_Large_UCF_Radius = DFNControl.MinRadiusForLargeFractures;
            bool checkLargeFractures = searchNeighbouringGridblocks && (Minimum_Large_UCF_Radius >= 0);
            // Set the maximum number of new fracture segments that can nucleate as the maximum number of fracture segments per the gridblock minus the number of fracture segments currently in the gridblock
            int currentNoFractureSegments = MacrofractureSegments.Count + UnconfinedFractureRaySegments.Count;
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
                currentNoFractureSegments += fs.LocalDFNMicrofractures.Count;
            int maxNewFractureSegments = DFNControl.MaxNoFractureSegments - currentNoFractureSegments;
            bool limitNewFractures = (maxNewFractureSegments > 0);
            // If probabilisticFractureNucleationLimit is set to -1 then it should be set to automatic
            // In automatic mode, probabilistic fracture nucleation will be activated whenever searching neighbouring gridblocks is also active 
            if (probabilisticFractureNucleationLimit < 0)
            {
                if (searchNeighbouringGridblocks)
                {
                    allowProbabilisticFractureNucleation = true;
                    probabilisticFractureNucleationLimit = 1;
                }
                else
                {
                    probabilisticFractureNucleationLimit = 0;
                }
            }
            // Flag to ignore zero length macrofractures when calculating macrofracture stress shadow interaction
            // This will prevent the development of "shatter zones" with a very high density of very short segments in late timesteps 
            bool ignoreZeroLengthMFStressShadows = checkAlluFStressShadows;

            // Get reference to the random number generator
            Random randGen = RandGen;

            // Create a temporary list for the maximum macrofracture propagation lengths and the unconfined fracture growth coefficients
            List<List<double>> maxPropLengths = new List<List<double>>();
            List<double> ufsGrowthFactors = new List<double>();

#if LOGDFNPOP
            // Select a gridblock to log
            //bool writeLoggingData = (((float)SWtop.X == 1000f) && ((float)SWtop.Y == 1000f) && ((float)SWtop.Depth == 2000f));
            bool writeLoggingData = (((float)SWtop.X == 0f) && ((float)SWtop.Y == 0f) && ((float)SWtop.Depth == 2000f));

            // Create a list of unconfined fracture sets to log
            List<int> setsToLog = new List<int> { 2 };

            // Create lists for the output files for each fracture set
            List<StreamWriter> DFN_MFPopLogFiles = new List<StreamWriter>();
            List<StreamWriter> DFN_UCFPopLogFiles = new List<StreamWriter>();

            // Create lists for all set-specific logging variables
            // Counters for layer-bound macrofracture segments
            // The following counters record data for all macrofracture segments in this gridblock, regardless of the gridblock in which the fracture nucleated
            List<int> Dict_MF_NoTotalFracSegments = new List<int>();
            List<int> Dict_MF_NoActiveFracSegments = new List<int>();
            List<int> Dict_MF_NoTotalExistingFracSegments = new List<int>();
            List<int> Dict_MF_NoActiveExistingFracSegments = new List<int>();
            List<int> Dict_MF_NoStressShadowInteractions = new List<int>();
            List<int> Dict_MF_NoIntersections = new List<int>();
            List<int> Dict_MF_NoPropagatingOut = new List<int>();
            List<int> Dict_MF_NoTotalNucleating = new List<int>();
            List<int> Dict_MF_NoActiveNucleating = new List<int>();
            List<int> Dict_MF_MostPopulousDipSetIndex = new List<int>();
            List<double> Dict_MF_TotalFractureArea = new List<double>();
            List<double> Dict_MF_TotalFractureVolume = new List<double>();
            // The following counters check stress shadows and exclusion zones for all macrofracture segments in this gridblock only, even if searchNeighbouringGridblocks is specified
            List<double> Dict_MF_MeasuredStressShadowVol = new List<double>();
            List<double> Dict_MF_MeasuredExclusionZoneVol = new List<double>();
            // Counters for unconfined fracture rays
            // The following counters record data for all unconfined fracture rays from fractures that nucleated in this gridblock, regardless of the gridblock in which the ray is currently active
            // Thus they include data for fractures propagating out of this block but not for fractures propagating into it
            List<int> Dict_UCF_NoTotalFracRays = new List<int>();
            List<double> Dict_UCF_TotalFractureArea = new List<double>();
            List<double> Dict_UCF_TotalFractureVolume = new List<double>();
            List<int> Dict_UCF_NoTotalExistingFracRays = new List<int>();
            List<string> Dict_UCF_RayLengths = new List<string>();
            // The following counters record data for all unconfined fracture ray segments in this gridblock, regardless of the gridblock in which the fracture nucleated
            // Thus they include data for fractures propagating into this block but not for fractures propagating out of it
            List<int> Dict_UCF_NoActiveFracRays = new List<int>();
            List<int> Dict_UCF_NoActiveExistingFracRays = new List<int>();
            List<int> Dict_UCF_NoStressShadowInteractions = new List<int>();
            List<int> Dict_UCF_NoIntersections = new List<int>();
            List<int> Dict_UCF_NoPropagatingOut = new List<int>();
            List<int> Dict_UCF_NoReachingMaxRadius = new List<int>();
            List<int> Dict_UCF_NoTotalNucleating = new List<int>();
            List<int> Dict_UCF_NoActiveNucleating = new List<int>();
            List<int> Dict_UCF_CumulativeIncomingRays = new List<int>();
            List<int> Dict_UCF_CumulativeOutgoingRays = new List<int>();
            // The following counters check stress shadows and exclusion zones for all unconfined fractures in this gridblock and neighbouring gridblocks, if searchNeighbouringGridblocks is specified
            List<double> Dict_UCF_MeasuredStressShadowVol = new List<double>();
            List<double> Dict_UCF_MeasuredExclusionZoneVol = new List<double>();
#endif

            // Propagate microfractures and nucleate macrofractures, for each fracture set
            // Loop through each fracture set
            for (int fs_index = 0; fs_index < NoLayerBoundFractureSets; fs_index++)
            {
                LayerBoundFractureSet fs = LayerBoundFractureSets[fs_index];

                // The lists of stress shadow half-widths of all fracture sets as seen by all other fracture sets and vice versa need to be recalculated every timestep
                // Create a null reference to a list of stress shadow half-widths of other fracture sets as seen by this fracture set, and to the stress shadow half-widths of this fracture set as seen by other fracture sets
                // These will be filled out as required
                List<List<double>> SetI_StressShadowHalfWidthsIJ = null;
                List<List<double>> SetI_StressShadowHalfWidthsJI = null;
                // Also add the lists for this set to the holders for all sets
                StressShadowHalfWidthsIJ[fs_index] = SetI_StressShadowHalfWidthsIJ;
                StressShadowHalfWidthsJI[fs_index] = SetI_StressShadowHalfWidthsJI;

#if LOGDFNPOP
                string fileName = string.Format("DFN_MFPopulationLog_X{0}_Y{1}_Set{2}.txt", SWtop.X, SWtop.Y, fs_index);
                String namecomb = PropControl.FolderPath + fileName;
                //Console.WriteLine(string.Format("TS {0}, {1}", CurrentExplicitTimestep, fileName));

                StreamWriter DFN_MFPopLogFile;
                if (CurrentExplicitTimestep == 1)
                {
                    DFN_MFPopLogFile = new StreamWriter(namecomb, false);
                    DFN_MFPopLogFile.WriteLine(string.Format("DFN statistics: Set {0}", fs_index));
                    DFN_MFPopLogFile.WriteLine("");
                    DFN_MFPopLogFile.WriteLine("Timestep\tTotal no of fractures at end of TS\tNo of propagating fractures at end of TS\tTotal no of fractures at start of TS\tNo of propagating fractures at start of TS\tNumber of fractures terminating due to stress shadow interaction\tNumber of fractures terminating due to intersection\tNumber of fractures propagating out\tMaximum potential new fractures nucleating\tNumber of active new fractures nucleating\tMost populous dipset\tTotalFractureArea\tTotalFractureVolume\tStress shadow volume\tExclusion zone volume");
                }
                else
                {
                    DFN_MFPopLogFile = new StreamWriter(namecomb, true);
                }
                DFN_MFPopLogFiles.Add(DFN_MFPopLogFile);

                // Initialise counter variables for this fracture set
                Dict_MF_NoTotalFracSegments.Add(0);
                Dict_MF_NoActiveFracSegments.Add(0);
                Dict_MF_NoTotalExistingFracSegments.Add(0);
                Dict_MF_NoActiveExistingFracSegments.Add(0);
                Dict_MF_NoStressShadowInteractions.Add(0);
                Dict_MF_NoIntersections.Add(0);
                Dict_MF_NoPropagatingOut.Add(0);
                Dict_MF_NoTotalNucleating.Add(0);
                Dict_MF_NoActiveNucleating.Add(0);
                Dict_MF_TotalFractureArea.Add(0);
                Dict_MF_TotalFractureVolume.Add(0);

                // Calculate stress shadow and exclusion zone volumes by placing random points in the grid and testing if they lie in a stress shadow or exclusion zone

                // Use 1000 test points as default 
                int NoTestPoints = 1000;
                int NoInStressShadow = 0;
                int NoInExclusionZone = 0;

                // Use the stress shadow width of most populous fracture dip set
                double StressShadowWidth = 0;
                double MaxPsi = 0;
                int MostPopulousDipSetIndex = 0;
                for (int fds_index = 0; fds_index < fs.FractureDipSets.Count; fds_index++)
                {
                    FractureDipSet fds = fs.FractureDipSets[fds_index];
                    double fds_Psi = fds.Total_MF_StressShadowVolume();
                    if (fds_Psi > MaxPsi)
                    {
                        MostPopulousDipSetIndex = fds_index;
                        MaxPsi = fds_Psi;
                        StressShadowWidth = fds.Mean_MF_StressShadowWidth;
                    }
                }

                // Create points in random locations and check if they are in stress shadow and exclusion zone, if so update counters
                for (int PointNo = 0; PointNo < NoTestPoints; PointNo++)
                {
                    // Get random location for test point
                    PointXYZ testPointXYZ = getRandomPoint(false);
                    PointIJK testPoint = fs.convertXYZtoIJK(testPointXYZ);

                    // Check whether this point lies in the stress shadow of an existing macrofracture and if so update counter
                    if (checkAlluFStressShadows)
                    {
                        if (checkInMFStressShadow(testPointXYZ, fs_index, ref SetI_StressShadowHalfWidthsIJ))
                            NoInStressShadow++;
                    }
                    else
                    {
                        if (fs.checkInMFStressShadow(testPoint))
                            NoInStressShadow++;
                    }

                    // Check whether this point lies in the exclusion zone of an existing macrofracture and if so update counter
                    if (checkAlluFStressShadows)
                    {
                        if (checkInMFExclusionZone(testPointXYZ, fs_index, MostPopulousDipSetIndex, ref SetI_StressShadowHalfWidthsIJ, ref SetI_StressShadowHalfWidthsJI))
                            NoInExclusionZone++;
                    }
                    else
                    {
                        if (fs.checkInMFExclusionZone(testPoint, StressShadowWidth))
                            NoInExclusionZone++;
                    }
                }

                // Calculate the stress shadow and exclusion zone volumes from the proportion of points lying in them
                Dict_MF_MostPopulousDipSetIndex.Add(MostPopulousDipSetIndex);
                Dict_MF_MeasuredStressShadowVol.Add((double)NoInStressShadow / (double)NoTestPoints);
                Dict_MF_MeasuredExclusionZoneVol.Add((double)NoInExclusionZone / (double)NoTestPoints);
#endif

                // Determine the number of fracture dip sets and the maximum macrofracture propagation length for each dip set
                int NoDipSets = fs.FractureDipSets.Count;
                List<double> fs_maxPropLengths = new List<double>();
                for (int dipsetIndex = 0; dipsetIndex < NoDipSets; dipsetIndex++)
                {
                    // Get a reference to the fracture dip set object - this contains all the required data for fracture nucleation and propagation rate
                    FractureDipSet fds = fs.FractureDipSets[dipsetIndex];

                    // Add a maximum propagation length value of zero to the local list for this fracture dip set
                    fs_maxPropLengths.Add(0);

                    // If the dip set is deactivated, skip the rest of this stage and move on to the next dip set
                    if (fds.getEvolutionStage(CurrentExplicitTimestep) == FractureEvolutionStage.Deactivated)
                        continue;

                    // Get required data for current timestep from FractureDipSet.DFN_data object
                    double halfLength_M = fds.getMFPropagationDistance(CurrentExplicitTimestep);
                    double Cum_Gamma_Mminus1 = fds.getCumGamma(CurrentExplicitTimestep - 1);
                    double Cum_hGamma_Mminus1 = fds.getCumhGamma(CurrentExplicitTimestep - 1);

                    // If the propagation distance is greater than 1E+50 then there is probably an error in the calculation (it may be reading a NaN from the from FractureDipSet.DFN_data object)
                    // This will cause the module to hang, as it will get stuck in an infinite (or very long) loop
                    // This can be prevented by checking the propagation distance, and aborting the function if it is greater than 1E+50
                    // This should be implemented in the production code as it will prevent the whole model hanging due to the implicit calculation failing in just one gridblock
                    // However it can also mask other bugs in the implicit calculation; it is therefore useful to switch off this check in development code
#if !DEBUG
                    if (!(halfLength_M < 1E+50))
                        return PropagateDFNReturnCode.DrivingStressError;
#endif

                    // Calculate local helper variables
                    double CapB = fds.CapB;
                    double CapBV = CapB * Volume;
                    double c_coefficient = fds.c_coefficient;
                    // betac_factor is -beta*c if b<>2, -c if b=2
                    double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
                    // Maximum propagation length: already stored in FractureCalculationData object
                    double ts_PropLength = halfLength_M;

                    // Set the maximum propagation length value of zero to the local list for this fracture dip set
                    fs_maxPropLengths[dipsetIndex] = ts_PropLength;

                    // Add microfractures if required
                    if (calc_uF)
                    {
                        // Calculate local helper variables
                        double ts_CumrminGammaMminus1 = (bis2 ? Math.Log(uF_minRadius) : Math.Pow(uF_minRadius, 1 / beta)) + Cum_Gamma_Mminus1;
                        double ts_CumrminGammaMminus1betac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumrminGammaMminus1) : Math.Pow(ts_CumrminGammaMminus1, betac_factor));

                        // Calculate initial microfracture sequence number
                        // NB this may not match the actual number of microfractures since some may have been located in stress shadows so not generated
                        int uF_No = (int)(CapBV * ts_CumrminGammaMminus1betac_factor) + 1;

                        // If this is the first timestep, add initial microfractures
                        if (CurrentExplicitTimestep == 1)
                        {
                            // LTime at t=0 is zero
                            double initialLTime = 0;

                            // Since initial microfractures will have radii > mininum microfracture radius, we must loop through microfractures of radius up to the maximum (h/2)
                            // We must therefore set a local variable for the radius of the next microfracture to add
                            double next_uF_radius = uF_minRadius;

                            // We can now calculate the total number of microfractures with radius greater than the minimum - this is the total number of microfractures that we need to add
                            int no_uF_toAdd = (int)(CapBV * Math.Pow(next_uF_radius, -c_coefficient));

                            // If we are adding fractures probabilistically, determine randomly whether we should add an extra fracture
                            if (allowProbabilisticFractureNucleation)
                            {
                                if (((CapBV * Math.Pow(next_uF_radius, -c_coefficient)) - (double)no_uF_toAdd) > randGen.NextDouble())
                                    no_uF_toAdd++;
                            }

                            while (no_uF_toAdd > 0)
                            {
                                // Calculate the radius of the next microfracture - with a maximum value h/2
                                next_uF_radius = Math.Pow((double)no_uF_toAdd / CapBV, -1 / c_coefficient);
                                if (next_uF_radius > max_uF_radius) next_uF_radius = max_uF_radius;

                                // Get random location for new microfracture
                                PointIJK new_uF_centrepointIJK;
                                if (SpecifyFractureNucleationPosition)
                                    new_uF_centrepointIJK = fs.getRandomNucleationPoint(FractureNucleationPosition_w);
                                else
                                    new_uF_centrepointIJK = fs.getRandomNucleationPoint();
                                PointXYZ new_uf_centrepointXYZ = fs.convertIJKtoXYZ(new_uF_centrepointIJK);

                                // There are no macrofractures yet so there will be no stress shadows even if we are including stress shadow effects
                                bool addThisFracture = true;

                                // Generate a new microfracture and add it to the DFN
                                if (addThisFracture)
                                {
                                    // Set the fracture dip direction
                                    // If this is a biazimuthal conjugate fracture dipset, set a random dip direction
                                    // Otherwise set the fracture to dip direction to JPlus - there will be a mirror dipset with a negative dip, dipping towards JMinus)
                                    DipDirection dipdir;
                                    if (fds.BiazimuthalConjugate)
                                        dipdir = ((randGen.Next(2) == 0) ? DipDirection.JPlus : DipDirection.JMinus);
                                    else
                                        dipdir = DipDirection.JPlus;

                                    // Create a new MicrofractureIJK object and add it to the local DFN
                                    MicrofractureIJK new_local_uF = new MicrofractureIJK(fs, dipsetIndex, new_uF_centrepointIJK, next_uF_radius, dipdir, initialLTime, 0);
                                    fs.LocalDFNMicrofractures.Add(new_local_uF);

                                    // Create a corresponding MicrofractureXYZ object and add it to the global DFN
                                    MicrofractureXYZ new_global_uF = new_local_uF.createLinkedGlobalMicrofracture(fs_index);
                                    global_DFN.GlobalDFNMicrofractures.Add(new_global_uF);
                                }

                                // Update the number of microfractures that we need to add - if there are no more to add we can break out of the loop
                                no_uF_toAdd--;

                                // Update the number of new fracture segments that can be added this timestep; if this drops below zero, break out of the loop
                                if (limitNewFractures && (--maxNewFractureSegments < 0))
                                    break;
                            }
                        }

                        // Add new fractures until we reach the end of the timestep
                        // Calculate the weighted time (LTime) when the next microfracture will nucleate
                        double nVB_factor = (double)uF_No / CapBV;
                        double nVB_invbetac1_factor = (bis2 ? Math.Log(nVB_factor) / betac_factor : Math.Pow(nVB_factor, 1 / betac_factor));
                        double NucleationLTime = hb1_factor * beta * (ts_CumrminGammaMminus1 - nVB_invbetac1_factor);

                        // If we are adding fractures probabilistically, determine randomly whether we need to add an extra fracture
                        if (allowProbabilisticFractureNucleation)
                        {
                            // Calculate the total weighted time (LTime) interval between microfracture nucleation
                            double previous_nVB_factor = (double)(uF_No - 1) / CapBV;
                            double previous_nVB_invbetac1_factor = (bis2 ? Math.Log(previous_nVB_factor) / betac_factor : Math.Pow(previous_nVB_factor, 1 / betac_factor));
                            double NucleationLTime_interval = hb1_factor * beta * (previous_nVB_invbetac1_factor - nVB_invbetac1_factor);

                            // If the total weighted time interval between microfracture nucleation is greater than the limiting value (expressed in terms of the weighted timestep duration), determine randomly whether to nucleate a fracture
                            if (NucleationLTime_interval > (ts_PropLength / probabilisticFractureNucleationLimit))
                            {
                                // We set the weighted time for nucleation of the next fracture to be a random time up to the weighted time (LTime) interval between microfracture nucleation
                                // The fracture will only actually nucleate in this timestep if this value is less than the weighted timestep duration
                                NucleationLTime = randGen.NextDouble() * NucleationLTime_interval;
                            }
                        }

                        while (NucleationLTime < ts_PropLength)
                        {
                            // Get random location for new microfracture
                            PointIJK new_uF_centrepointIJK;
                            if (SpecifyFractureNucleationPosition)
                                new_uF_centrepointIJK = fs.getRandomNucleationPoint(FractureNucleationPosition_w);
                            else
                                new_uF_centrepointIJK = fs.getRandomNucleationPoint();
                            PointXYZ new_uf_centrepointXYZ = fs.convertIJKtoXYZ(new_uF_centrepointIJK);

                            // If we are including stress shadow effects, check whether this point lies in the stress shadow of an existing macrofracture and if so set flag to ignore it
                            bool addThisFracture = true;
                            if (checkStressShadow)
                            {
                                // Check if the propagating node of the macrofracture segment lies in a stress shadow
                                addThisFracture = !checkInMFStressShadow(new_uf_centrepointXYZ, fs_index, checkAlluFStressShadows, SearchNeighbouringGridblocks(), ref SetI_StressShadowHalfWidthsIJ);
                            }

                            // If the point is not in a stress shadow or we are not including stress shadow effects, generate a new microfracture and add it to the DFN
                            if (addThisFracture)
                            {
                                // Set the fracture dip direction
                                // If this is a biazimuthal conjugate fracture dipset, set a random dip direction
                                // Otherwise set the fracture to dip direction to JPlus - there will be a mirror dipsets with a negative dip, dipping towards JMinus)
                                DipDirection dipdir;
                                if (fds.BiazimuthalConjugate)
                                    dipdir = ((randGen.Next(2) == 0) ? DipDirection.JPlus : DipDirection.JMinus);
                                else
                                    dipdir = DipDirection.JPlus;

                                // Create a new MicrofractureIJK object and add it to the local DFN
                                MicrofractureIJK new_local_uF = new MicrofractureIJK(fs, dipsetIndex, new_uF_centrepointIJK, uF_minRadius, dipdir, NucleationLTime, CurrentExplicitTimestep);
                                fs.LocalDFNMicrofractures.Add(new_local_uF);

                                // Create a corresponding MicrofractureXYZ object and add it to the global DFN
                                MicrofractureXYZ new_global_uF = new_local_uF.createLinkedGlobalMicrofracture(fs_index);
                                global_DFN.GlobalDFNMicrofractures.Add(new_global_uF);
                            }

                            // Update the weighted time (LTime) when the next microfracture will nucleate
                            uF_No++;
                            nVB_factor = (double)uF_No / CapBV;
                            nVB_invbetac1_factor = (bis2 ? Math.Log(nVB_factor) / betac_factor : Math.Pow(nVB_factor, 1 / betac_factor));
                            NucleationLTime = hb1_factor * beta * (ts_CumrminGammaMminus1 - nVB_invbetac1_factor);

                            // Update the number of new fracture segments that can be added this timestep; if this drops below zero, break out of the loop
                            if (limitNewFractures && (--maxNewFractureSegments < 0))
                                break;
                        }
                    } // End add microfractures

                    // Add macrofractures directly if required
                    // NB we do not need to do this if we are including microfractures in the DFN - macrofractures will be nucleated automatically when microfracture radius reaches h/2
                    if (add_MF_directly)
                    {
                        // Declare local helper variables
                        double ts_CumhGammaMminus1, L_factor;
                        int tsM = CurrentExplicitTimestep;
                        int MF_No;

                        // Calculate local helper variables: this will vary depending on whether we have defined a minimum length cutoff
                        // Also calculate initial macrofracture sequence number
                        // NB this may not match the actual number of macrofractures since some may have been located in stress shadows or deactivated before reaching minimum length so not generated
                        if (MF_minLength > 0)
                        {
                            // Calculate the M timestep: this is the timestep in which a currently active fracture of the specified minimum length must have nucleated
                            while ((tsM > 0) && (MF_minLength >= fds.getCumulativeMFPropagationDistance(CurrentExplicitTimestep, tsM - 1))) tsM--;

                            // If timestep M = 0 we must set timestep M-1 to zero also
                            int tsMminus1 = tsM - 1;
                            if (tsMminus1 < 0) tsMminus1 = 0;

                            // Set local helper variables for timestep M
                            ts_CumhGammaMminus1 = fds.getCumhGamma(tsMminus1);
                            double IPlus_halfLength_Nminus1_Mminus1 = fds.getCumulativeMFPropagationDistance(CurrentExplicitTimestep - 1, tsMminus1);
                            L_factor = (MF_minLength / 2) - IPlus_halfLength_Nminus1_Mminus1;
                            double ts_CumhGammaMminus1Lfactor = ts_CumhGammaMminus1 + (L_factor / (beta * hb1_factor));
                            double ts_CumhGammaMminus1Lfactorbetac_factor = 0;
                            if (ts_CumhGammaMminus1Lfactor > 0)
                                ts_CumhGammaMminus1Lfactorbetac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1Lfactor) : Math.Pow(ts_CumhGammaMminus1Lfactor, betac_factor));

                            // Calculate initial macrofracture sequence number
                            MF_No = (int)(CapBV * ts_CumhGammaMminus1Lfactorbetac_factor) + 1;
                        }
                        else
                        {
                            // Set local helper variables for timestep N
                            ts_CumhGammaMminus1 = Cum_hGamma_Mminus1;
                            L_factor = 0;
                            double ts_CumhGammabetac_factor = (bis2 ? Math.Exp(betac_factor * ts_CumhGammaMminus1) : Math.Pow(ts_CumhGammaMminus1, betac_factor));

                            // Calculate initial macrofracture sequence number
                            MF_No = (int)(CapBV * ts_CumhGammabetac_factor) + 1;

                            // If we are adding fractures probabilistically, determine randomly whether we should add an extra fracture
                            if (allowProbabilisticFractureNucleation)
                            {
                                if ((((CapBV * ts_CumhGammabetac_factor) + 1) - (double)MF_No) > randGen.NextDouble())
                                    MF_No++;
                            }
                        }

                        // If this is the first timestep, add initial (zero length) macrofractures
                        if (CurrentExplicitTimestep == 1)
                        {
                            // LTime at t=0 is zero
                            double initialLTime = 0;

                            for (int initialFracNo = 1; initialFracNo < MF_No; initialFracNo++)
                            {
#if LOGDFNPOP
                                // Update counters for total and active number of fractures nucleating in this timestep
                                Dict_MF_NoTotalNucleating[fs_index] += 2;
                                Dict_MF_NoActiveNucleating[fs_index] += 2;
#endif
                                // Get random location for new macrofracture nucleation point
                                PointIJK new_MF_nucleationpoint = fs.getRandomNucleationPoint(0.5);

                                // Any existing fractures have zero length,so this point cannot lie in the exclusion zone of an existing macrofracture
                                bool addThisFracture = true;

                                // The initial fractures have zero length so we do not need to worry if it intersects another macrofracture, interacts with another stress shadow, or propagates out of the gridblock before it reaches the minimum length

                                // If the new macrofracture is valid, generate a new macrofracture object and add it to the DFN
                                if (addThisFracture)
                                {
                                    // Set the fracture dip direction
                                    // If this is a biazimuthal conjugate fracture dipset, set a random dip direction
                                    // Otherwise set the fracture to dip direction to JPlus - there will be a mirror dipsets with a negative dip, dipping towards JMinus)
                                    DipDirection dipdir;
                                    if (fds.BiazimuthalConjugate)
                                        dipdir = ((randGen.Next(2) == 0) ? DipDirection.JPlus : DipDirection.JMinus);
                                    else
                                        dipdir = DipDirection.JPlus;

                                    // Create a new MacrofractureSegmentIJK object and add it to the local DFN
                                    MacrofractureSegmentIJK new_local_MF = new MacrofractureSegmentIJK(fs, dipsetIndex, new_MF_nucleationpoint, PropagationDirection.IPlus, PropagationDirection.IPlus, dipdir, initialLTime, 0);
                                    fs.LocalDFNMacrofractureSegments[PropagationDirection.IPlus].Add(new_local_MF);

                                    // Create a corresponding MacrofractureXYZ object and add it to the global DFN
                                    // NB this will automatically create a mirror MacrofractureSegmentIJK object and add it to the local DFN
                                    MacrofractureSegmentIJK mirrorSegment;
                                    MacrofractureXYZ new_global_MF = new_local_MF.createLinkedGlobalMacrofracture(fs_index, out mirrorSegment);
                                    global_DFN.GlobalDFNMacrofractures.Add(new_global_MF);

                                    // Also add both the new segments to the list of all fracture segments in the gridblock
                                    MacrofractureSegments.Add(new MacrofractureSegmentHolder(new_local_MF, fs_index));
                                    MacrofractureSegments.Add(new MacrofractureSegmentHolder(mirrorSegment, fs_index));
                                }

                                // Update the number of new fracture segments that can be added this timestep; if this drops below zero, break out of the loop
                                if (limitNewFractures) maxNewFractureSegments -= 2;
                                if (limitNewFractures && (maxNewFractureSegments < 0))
                                    break;
                            }
                        }

                        // Add new fractures until we reach the end of the timestep
                        // Calculate the weighted time (LTime) when the next microfracture will nucleate
                        double nVB_factor = (double)MF_No / CapBV;
                        double nVB_invbetac1_factor = (bis2 ? Math.Log(nVB_factor) / betac_factor : Math.Pow(nVB_factor, 1 / betac_factor));
                        double NucleationLTime = (hb1_factor * beta * (ts_CumhGammaMminus1 - nVB_invbetac1_factor)) + L_factor;

                        // If we are adding fractures probabilistically, determine randomly whether we need to add an extra fracture
                        if (allowProbabilisticFractureNucleation)
                        {
                            // Calculate the total weighted time (LTime) interval between macrofracture nucleation
                            double previous_nVB_factor = (double)(MF_No - 1) / CapBV;
                            double previous_nVB_invbetac1_factor = (bis2 ? Math.Log(previous_nVB_factor) / betac_factor : Math.Pow(previous_nVB_factor, 1 / betac_factor));
                            double NucleationLTime_interval = hb1_factor * beta * (previous_nVB_invbetac1_factor - nVB_invbetac1_factor);

                            // If the total weighted time interval between macrofracture nucleation is greater than the limiting value (expressed in terms of the weighted timestep duration), determine randomly whether to nucleate a fracture
                            if (NucleationLTime_interval > (ts_PropLength / probabilisticFractureNucleationLimit))
                            {
                                // We set the weighted time for nucleation of the next fracture to be a random time up to the weighted time (LTime) interval between macrofracture nucleation
                                // The fracture will only actually nucleate in this timestep if this value is less than the weighted timestep duration
                                NucleationLTime = randGen.NextDouble() * NucleationLTime_interval;
                            }
                        }

                        while (NucleationLTime < ts_PropLength)
                        {
#if LOGDFNPOP
                            // Update counter for total number of fractures nucleating in this timestep
                            Dict_MF_NoTotalNucleating[fs_index] += 2;
#endif
                            // Get random location for new macrofracture nucleation point
                            PointIJK new_MF_nucleationpointIJK = fs.getRandomNucleationPoint(0.5);
                            PointXYZ new_MF_nucleationpointXYZ = fs.convertIJKtoXYZ(new_MF_nucleationpointIJK);

                            // If we are including stress shadow effects, check whether this point lies in the exclusion zone of an existing macrofracture and if so set flag to ignore it
                            bool addThisFracture = true;
                            if (checkStressShadow)
                            {
                                // Check if the propagating node of the macrofracture segment lies in an exclusion zone
                                addThisFracture = !checkInMFExclusionZone(new_MF_nucleationpointXYZ, fs_index, dipsetIndex, checkAlluFStressShadows, SearchNeighbouringGridblocks(), ref SetI_StressShadowHalfWidthsIJ, ref SetI_StressShadowHalfWidthsJI);
                            }

                            // If we are applying a minimum macrofracture length cutoff we also need to check if it intersects another macrofracture, interacts with another stress shadow, or propagates out of the gridblock before it reaches the minimum length
                            if (use_MF_min_length_cutoff)
                            {
                                // Not yet implemented
                            }

                            // If the new macrofracture is valid, generate a new macrofracture object and add it to the DFN
                            if (addThisFracture)
                            {
#if LOGDFNPOP
                                // Update counter for number of active fractures nucleating in this timestep
                                Dict_MF_NoActiveNucleating[fs_index] += 2;
#endif
                                // Set the fracture dip direction
                                // If this is a biazimuthal conjugate fracture dipset, set a random dip direction
                                // Otherwise set the fracture to dip direction to JPlus - there will be a mirror dipsets with a negative dip, dipping towards JMinus)
                                DipDirection dipdir;
                                if (fds.BiazimuthalConjugate)
                                    dipdir = ((randGen.Next(2) == 0) ? DipDirection.JPlus : DipDirection.JMinus);
                                else
                                    dipdir = DipDirection.JPlus;

                                // Create a new MacrofractureSegmentIJK object and add it to the local DFN
                                MacrofractureSegmentIJK new_local_MF = new MacrofractureSegmentIJK(fs, dipsetIndex, new_MF_nucleationpointIJK, PropagationDirection.IPlus, PropagationDirection.IPlus, dipdir, NucleationLTime, CurrentExplicitTimestep);
                                fs.LocalDFNMacrofractureSegments[PropagationDirection.IPlus].Add(new_local_MF);

                                // Create a corresponding MacrofractureXYZ object and add it to the global DFN
                                // NB this will automatically create a mirror MacrofractureSegmentIJK object and add it to the local DFN
                                MacrofractureSegmentIJK mirrorSegment;
                                MacrofractureXYZ new_global_MF = new_local_MF.createLinkedGlobalMacrofracture(fs_index, out mirrorSegment);
                                global_DFN.GlobalDFNMacrofractures.Add(new_global_MF);

                                // Also add both the new segments to the list of all fracture segments in the gridblock
                                MacrofractureSegments.Add(new MacrofractureSegmentHolder(new_local_MF, fs_index));
                                MacrofractureSegments.Add(new MacrofractureSegmentHolder(mirrorSegment, fs_index));
                            }

                            // Update the weighted time (LTime) when the next microfracture will nucleate
                            MF_No++;
                            nVB_factor = (double)MF_No / CapBV;
                            nVB_invbetac1_factor = (bis2 ? Math.Log(nVB_factor) / betac_factor : Math.Pow(nVB_factor, 1 / betac_factor));
                            NucleationLTime = (hb1_factor * beta * (ts_CumhGammaMminus1 - nVB_invbetac1_factor)) + L_factor;

                            // Update the number of new fracture segments that can be added this timestep; if this drops below zero, break out of the loop
                            if (limitNewFractures) maxNewFractureSegments -= 2;
                            if (limitNewFractures && (maxNewFractureSegments < 0))
                                break;
                        }
                    } // End add macrofractures

                    // If the number of new fracture segments that can be added this timestep has dropped below zero, break out of the loop
                    if (limitNewFractures && (maxNewFractureSegments < 0))
                        break;

                } // End loop through fracture dip sets

                // Add maximum propagation length for this fracture set to the local list
                maxPropLengths.Add(fs_maxPropLengths);

                // Propagate all microfractures, testing for stress shadow interaction and nucleating new macrofractures if required
                if (calc_uF)
                {
                    foreach (MicrofractureIJK uF in fs.LocalDFNMicrofractures)
                    {
                        // If microfracture is still active, grow it
                        if (uF.Active)
                        {
                            // Check if it is in a macrofracture stress shadow, if so deactivate it and move straight onto the next microfracture
                            if (checkStressShadow)
                            {
                                // Check if the propagating node of the macrofracture segment lies in a stress shadow
                                PointXYZ uFcentrepointXYZ = fs.convertIJKtoXYZ(uF.CentrePoint);
                                bool deactivateFracture = checkInMFStressShadow(uFcentrepointXYZ, fs_index, checkAlluFStressShadows, SearchNeighbouringGridblocks(), ref SetI_StressShadowHalfWidthsIJ);

                                // If we find a stress shadow interaction with a macrofracture from another gridblock, deactivate this microfracture and move on to the next one
                                if (deactivateFracture)
                                {
                                    uF.Active = false;
                                    continue;
                                }
                            }

                            // Get the microfracture dip set index
                            int dipsetIndex = uF.FractureDipSetIndex;

                            // Calculate helper variables
                            // Current radius factor: r^(1/beta) for b!=2; ln(r) for b=2
                            double curr_r_factor = (bis2 ? Math.Log(uF.Radius) : Math.Pow(uF.Radius, 1 / beta));
                            // Integral of alpha_MF * sigmad_b * t for duration of growth
                            // This will be equal to PropLength if the uF nucleated in a previous timestep, or (PropLength - NucleationLTime) if it nucleated in this timestep
                            double alphaMF_sigmadb_t_factor = ((uF.NucleationTimestep == CurrentExplicitTimestep) ? fs_maxPropLengths[dipsetIndex] - uF.NucleationLTime : fs_maxPropLengths[dipsetIndex]);

                            // Calculate the new microfracture radius
                            double new_r_factor = curr_r_factor + (alphaMF_sigmadb_t_factor / (beta * hb1_factor));
                            double newRadius;
                            if ((new_r_factor > 0) || bis2) // When b>2, microfractures can expand to infinite size; when this happens we will limit the size to half the layer thickness
                                newRadius = (bis2 ? Math.Exp(new_r_factor) : Math.Pow(new_r_factor, beta));
                            else
                                newRadius = max_uF_radius;

                            // Check if new radius is greater than h/2 - if so create a new macrofracture
                            if (newRadius >= max_uF_radius)
                            {
#if LOGDFNPOP
                                // Update counter for total number of fractures nucleating in this timestep
                                Dict_MF_NoTotalNucleating[fs_index] += 2;
#endif
                                // Set radius to h/2
                                uF.Radius = (max_uF_radius);

                                // Set centrepoint of microfracture to centre of layer - this is to prevent it extending out of layer
                                uF.CentrePoint.K = 0;

                                // Set microfracture flag to inactive
                                uF.Active = false;

                                // If we are including stress shadow effects, check whether the microfracture lies in the exclusion zone of an existing macrofracture and if so set flag to ignore it
                                // NB microfractures may remain active while they are in an exclusion zone, as long as they are not within a stress shadow, so we must recheck this
                                bool addThisFracture = true;
                                if (checkStressShadow)
                                {
                                    // Check if the propagating node of the macrofracture segment lies in an exclusion zone
                                    addThisFracture = !checkInMFExclusionZone(fs.convertIJKtoXYZ(uF.CentrePoint), fs_index, dipsetIndex, checkAlluFStressShadows, SearchNeighbouringGridblocks(), ref SetI_StressShadowHalfWidthsIJ, ref SetI_StressShadowHalfWidthsJI);
                                }

                                // If the number of new fracture segments that can be added this timestep has dropped to zero, do not add this fracture
                                if (limitNewFractures && (maxNewFractureSegments <= 0))
                                    addThisFracture = false;

                                if (addThisFracture)
                                {
#if LOGDFNPOP
                                    // Update counter for number of active fractures nucleating in this timestep
                                    Dict_MF_NoActiveNucleating[fs_index] += 2;
#endif
                                    // Set microfracture flag to nucleated macrofracture
                                    uF.NucleatedMacrofracture = true;

                                    // Determine LTime of macrofracture nucleation
                                    double nucleation_LTime = beta * hb1_factor * (initial_uF_factor - curr_r_factor);
                                    if (uF.NucleationTimestep == CurrentExplicitTimestep) nucleation_LTime += uF.NucleationLTime;

                                    // Create a new MacrofractureSegmentIJK object and add it to the local DFN
                                    MacrofractureSegmentIJK new_local_MF = new MacrofractureSegmentIJK(fs, dipsetIndex, uF.CentrePoint, PropagationDirection.IPlus, PropagationDirection.IPlus, uF.DipDir, nucleation_LTime, CurrentExplicitTimestep);
                                    fs.LocalDFNMacrofractureSegments[PropagationDirection.IPlus].Add(new_local_MF);

                                    // Create a corresponding MacrofractureXYZ object and add it to the global DFN
                                    // NB this will automatically create a mirror MacrofractureSegmentIJK object and add it to the local DFN
                                    MacrofractureSegmentIJK mirrorSegment;
                                    MacrofractureXYZ new_global_MF = new_local_MF.createLinkedGlobalMacrofracture(fs_index, out mirrorSegment);
                                    global_DFN.GlobalDFNMacrofractures.Add(new_global_MF);

                                    // Also add both the new segments to the list of all fracture segments in the gridblock
                                    MacrofractureSegments.Add(new MacrofractureSegmentHolder(new_local_MF, fs_index));
                                    MacrofractureSegments.Add(new MacrofractureSegmentHolder(mirrorSegment, fs_index));

                                    // Update the number of new fracture segments that can be added this timestep
                                    if (limitNewFractures) maxNewFractureSegments -= 2;
                                }
                            }
                            else // Otherwise increment the microfracture radius
                            {
                                uF.Radius = newRadius;

                                // If the fracture nucleation position is undefined and the microfracture tip has reached one of the layer boundaries, move its centrepoint towards centre of layer
                                // This will prevent the microfracture extending out of layer; however this is only geologically valid if the microfractures grow anisotropically and it may skew the microfracture volumetric distribution
                                // If the fracture nucleation position is defined, microfractures may extend out of the layer; this can be rectified when generating the microfracture cornerpoints
                                if (!SpecifyFractureNucleationPosition)
                                {
                                    if (uF.CentrePoint.K < (uF.Radius - max_uF_radius)) uF.CentrePoint.K = (uF.Radius - max_uF_radius);
                                    if (uF.CentrePoint.K > (max_uF_radius - uF.Radius)) uF.CentrePoint.K = (max_uF_radius - uF.Radius);
                                }
                            } 
                        } // End if microfracture is active
                    } // Loop to next microfracture
                } // End propagate microfractures
            } // Loop to next fracture set

            // Nucleate unconfined fractures, for each fracture set
            // Loop through each unconfined fracture set
            for (int ufs_index = 0; ufs_index < NoUnconfinedFractureSets; ufs_index++)
            {
                UnconfinedFractureSet ufs = UnconfinedFractureSets[ufs_index];

                // Create a null reference to a list of stress shadow width ratios of other fracture sets as seen by this fracture set
                // This will be filled out as required
                List<double> UCFStressShadowWidthRatios = null;

                // If the global minimum unconfined fracture radius is undefined (negative), use the local minimum unconfined fracture radius defined for the unconfined fracture set
                double UCF_minRadius = (Global_UCF_minRadius > 0) ? Global_UCF_minRadius : ufs.MinimumFractureRadius;

                // Get ufs growth factor for current timestep from FractureDipSet.DFN_data object and add it to the list, and calculate the weighted timestep duration
                double ufsGrowthFactor = ufs.getFractureGrowthFactor(CurrentExplicitTimestep);
                double WTime_M = -beta * ufsGrowthFactor;
                ufsGrowthFactors.Add(ufsGrowthFactor);

                // If the magnitude of the fracture growth weighted time (RTime) is greater than 1E+100 then there is probably an error in the calculation (it may be reading a NaN from the from FractureDipSet.DFN_data object)
                // This will cause the module to hang, as it will get stuck in an infinite (or very long) loop
                // This can be prevented by checking the RTime at the end of the timestep, and aborting the function if it is greater than 1E+50
                // This should be implemented in the production code as it will prevent the whole model hanging due to the implicit calculation failing in just one gridblock
                // However it can also mask other bugs in the implicit calculation; it is therefore useful to switch off this check in development code
#if !DEBUG
                if (!(ufsGrowthFactor < 1E+100))
                    return PropagateDFNReturnCode.DrivingStressError;
#endif

#if LOGDFNPOP
                string fileName = string.Format("DFN_UCFPopulationLog_X{0}_Y{1}_Depth{2}_Set{3}.txt", SWtop.X, SWtop.Y, SWtop.Depth, ufs_index);
                String namecomb = PropControl.FolderPath + fileName;

                StreamWriter DFN_UCFPopLogFile;
                if (writeLoggingData && setsToLog.Contains(ufs_index))
                {
                    Console.WriteLine(string.Format("TS {0}, {1}", CurrentExplicitTimestep, fileName));
                    if (CurrentExplicitTimestep == 1)
                    {
                        DFN_UCFPopLogFile = new StreamWriter(namecomb, false);
                        DFN_UCFPopLogFile.WriteLine(string.Format("DFN statistics: Set {0}", ufs_index));
                        DFN_UCFPopLogFile.WriteLine("");
                        DFN_UCFPopLogFile.WriteLine("Timestep\tTotal no of fracture rays at end of TS\tNo of propagating fracture rays at end of TS\tTotal no of fracture rays at start of TS\tNo of propagating fracture rays at start of TS\tNumber of fracture rays terminating due to stress shadow interaction\tNumber of fracture rays terminating due to intersection\tNumber of fracture rays propagating out\tNumber of fracture rays reaching maximum length\tMaximum potential new fracture rays nucleating\tNumber of active new fracture rays nucleating\tCumulative number of rays propagating into the gridblock\tCumulative number of rays propagating out of the gridblock\tTotalFractureArea\tTotalFractureVolume\tStress shadow volume\tExclusion zone volume");
                    }
                    else
                    {
                        DFN_UCFPopLogFile = new StreamWriter(namecomb, true);
                    }
                }
                else
                {
                    fileName = string.Format("Dummy_Set{0}.txt", ufs_index);
                    namecomb = PropControl.FolderPath + fileName;
                    DFN_UCFPopLogFile = new StreamWriter(namecomb, false);
                }

                DFN_UCFPopLogFiles.Add(DFN_UCFPopLogFile);

                // Initialise counter variables for this fracture set
                Dict_UCF_NoTotalFracRays.Add(0);
                Dict_UCF_NoActiveFracRays.Add(0);
                Dict_UCF_NoTotalExistingFracRays.Add(0);
                Dict_UCF_NoActiveExistingFracRays.Add(0);
                Dict_UCF_NoStressShadowInteractions.Add(0);
                Dict_UCF_NoIntersections.Add(0);
                Dict_UCF_NoPropagatingOut.Add(0);
                Dict_UCF_NoReachingMaxRadius.Add(0);
                Dict_UCF_NoTotalNucleating.Add(0);
                Dict_UCF_NoActiveNucleating.Add(0);
                Dict_UCF_CumulativeIncomingRays.Add(0);
                Dict_UCF_CumulativeOutgoingRays.Add(0);
                Dict_UCF_TotalFractureArea.Add(0);
                Dict_UCF_TotalFractureVolume.Add(0);
                Dict_UCF_RayLengths.Add("");

                if (writeLoggingData)
                {
                    // Calculate stress shadow and exclusion zone volumes by placing random points in the grid and testing if they lie in a stress shadow or exclusion zone
                    // Use 1000 test points as default 
                    int NoTestPoints = 1000;
                    int NoInStressShadow = 0;
                    int NoInExclusionZone = 0;
                    // The actual calculation is time consuming so we will only do this for active fracture sets
                    if ((setsToLog.Contains(ufs_index)) && ((ufs.getEvolutionStage(CurrentExplicitTimestep) == FractureEvolutionStage.Growing) || (ufs.getEvolutionStage(CurrentExplicitTimestep) == FractureEvolutionStage.ResidualActivity)))
                    {
                        // Create points in random locations and check if they are in stress shadow and exclusion zone, if so update counters
                        for (int PointNo = 0; PointNo < NoTestPoints; PointNo++)
                        {
                            // Get random location for test point
                            PointXYZ testPointXYZ = getRandomPoint(false);
                            bool inStressShadow = false;
                            bool inExclusionZone = false;

                            // First check whether this point lies in the stress shadow or exclusion zone of a fracture in this gridblock
                            if (checkAllUCFStressShadows)
                            {
                                inStressShadow = checkInUCFStressShadow(testPointXYZ, ufs_index, ref UCFStressShadowWidthRatios);
                                inExclusionZone = ufs.checkInUCFExclusionZone(testPointXYZ, UCF_minRadius, UCF_minRadius);
                            }
                            else
                            {
                                inStressShadow = ufs.checkInUCFStressShadow(testPointXYZ);
                                inExclusionZone = ufs.checkInUCFExclusionZone(testPointXYZ, UCF_minRadius, UCF_minRadius);
                            }

                            // Then, if required, check stress shadows and exclusion zones of unconfined fractures from adjacent gridblocks
                            if (searchNeighbouringGridblocks)
                            {
                                // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                // Loop through each gridblock in the list
                                foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                {
                                    if (checkAllUCFStressShadows)
                                    {
                                        // Find the index number of the equivalent unconfined fracture set in the neighbouring gridblock
                                        int neighbourGB_ufs_index = neighbour_gb.getClosestUnconfinedFractureSetIndex(ufs.NormalVector);

                                        // We do not need to run the checks if we have already ascertained that the point lies in a stress shadow or exclusion zone
                                        if (!inStressShadow)
                                            inStressShadow = neighbour_gb.checkInUCFStressShadow(testPointXYZ, neighbourGB_ufs_index, ref UCFStressShadowWidthRatios);
                                        if (!inExclusionZone)
                                        {
                                            UnconfinedFractureSet neighbourGB_ufs = neighbour_gb.getClosestUnconfinedFractureSet(ufs.NormalVector);
                                            inExclusionZone = neighbourGB_ufs.checkInUCFExclusionZone(testPointXYZ, UCF_minRadius, UCF_minRadius);
                                        }
                                    }
                                    else
                                    {
                                        // Find the correct unconfined fracture set in the neighbouring gridblock to search
                                        UnconfinedFractureSet neighbourGB_ufs = neighbour_gb.getClosestUnconfinedFractureSet(ufs.NormalVector);

                                        // We do not need to run the checks if we have already ascertained that the point lies in a stress shadow or exclusion zone
                                        if (!inStressShadow)
                                            inStressShadow = neighbourGB_ufs.checkInUCFStressShadow(testPointXYZ);
                                        if (!inExclusionZone)
                                            inExclusionZone = neighbourGB_ufs.checkInUCFExclusionZone(testPointXYZ, UCF_minRadius, UCF_minRadius);
                                    }
                                }
                            } // End check unconfined fractures from adjacent gridblocks

                            // Update the counters
                            if (inStressShadow) NoInStressShadow++;
                            if (inExclusionZone) NoInExclusionZone++;
                        }
                    }

                    // Calculate the stress shadow and exclusion zone volumes from the proportion of points lying in them
                    Dict_UCF_MeasuredStressShadowVol.Add((double)NoInStressShadow / (double)NoTestPoints);
                    Dict_UCF_MeasuredExclusionZoneVol.Add((double)NoInExclusionZone / (double)NoTestPoints);
                }
#endif

                // If the fracture set is deactivated, skip the rest of this stage and move on to the next dip set
                if (ufs.getEvolutionStage(CurrentExplicitTimestep) == FractureEvolutionStage.Deactivated)
                    continue;

                // If the number of new fracture segments that can be added this timestep has dropped below zero, break out of the loop
                if (limitNewFractures && (maxNewFractureSegments < 0))
                    continue;

                // Add new unconfined fractures if required
                // This will include initial unconfined fractures in timestep 1
                if (calc_UCF && ufs.ExplicitNucleationActive)
                {
                    // Helper variables - used to calculate the fracture nucleation time 
                    double Cum_Gamma_Mminus1 = ufs.getCumGamma(CurrentExplicitTimestep - 1);
                    double ts_CumrminGammaMminus1 = (bis2 ? Math.Log(UCF_minRadius) : Math.Pow(UCF_minRadius, 1 / beta)) + Cum_Gamma_Mminus1;

                    // Calculate the expected maximum potential number of fractures in the gridblock at the end of the current timestep (i.e. if there is no stress shadow deactivation)
                    // This will probably not be an integer - the fractional value will represent the probability that an additional fracture will nucleate in this gridblock at the end of the timestep
                    double Cum_Gamma_M = ufs.getCumGamma(CurrentExplicitTimestep);
                    double ts_CumrminGammaM = (bis2 ? Math.Log(UCF_minRadius) : Math.Pow(UCF_minRadius, 1 / beta)) + Cum_Gamma_M;
                    double r0_M = bis2 ? Math.Exp(ts_CumrminGammaM) : Math.Pow(ts_CumrminGammaM, beta);
                    double LP30_M = ufs.InitialP30(r0_M);
                    double expectedLn_M = LP30_M * Volume;

                    // Get the maximum potential number of fractures when the next fracture nucleates (also assuming there is no stress shadow deactivation)
                    // At this point we will not increment the counter - we only do this once the fracture has actually nucleated
                    // If we are adding fractures probabilistically, this will return a fractional value that will determine whether a new fracture will be nucleated
                    // NB The fractional value is calculated only once per nucleating fracture, to avoid "multiple dice rolls"
                    double nextFrac_Ln = ufs.getNextNucleatingFractureIndex(false, allowProbabilisticFractureNucleation);

                    // If the maximum potential P30 when the next unconfined fracture nucleates is less than or equal to the maximum potential P30 at the end of the timestep, then a new fracture can nucleate (subject to stress shadow deactivation)
                    // We will continue to add new fractures until the maximum potential P30 when the next unconfined fracture nucleates is greater than the maximum potential P30 at the end of the timestep
                    while ((float)nextFrac_Ln <= (float)expectedLn_M)
                    {
#if LOGDFNPOP
                        Dict_UCF_NoTotalNucleating[ufs_index] += ufs.RaysPerFracture;
#endif
                        // Get random location for new unconfined fracture
                        PointXYZ new_UCF_centrepointXYZ = getRandomPoint(false);

                        // If we are including stress shadow effects, check whether this point lies in the stress shadow of an existing unconfined fracture and if so set flag to ignore it
                        bool addThisFracture = true;
                        if (checkStressShadow)
                        {
                            // First check other unconfined fractures from this gridblock
                            if (checkAllUCFStressShadows)
                                addThisFracture = !checkInUCFStressShadow(new_UCF_centrepointXYZ, ufs_index, ref UCFStressShadowWidthRatios);
                            else
                                addThisFracture = !ufs.checkInUCFStressShadow(new_UCF_centrepointXYZ);

                            // Then, if required, check unconfined fractures from adjacent gridblocks
                            // NB we do not need to do this if we have already found a stress shadow interaction
                            if (addThisFracture && searchNeighbouringGridblocks)
                            {
                                // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                // Loop through each gridblock in the list
                                foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                {
                                    if (checkAllUCFStressShadows)
                                    {
                                        // Find the index number of the equivalent unconfined fracture set in the neighbouring gridblock
                                        int neighbourGB_ufs_index = neighbour_gb.getClosestUnconfinedFractureSetIndex(ufs.NormalVector);

                                        // Now check the unconfined fractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                        // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                                        // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                                        // We will therefore use the list generated for this gridblock to speed up the calculation
                                        if (neighbour_gb.checkInUCFStressShadow(new_UCF_centrepointXYZ, neighbourGB_ufs_index, ref UCFStressShadowWidthRatios))
                                        {
                                            addThisFracture = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        // Find the correct unconfined fracture set in the neighbouring gridblock to search
                                        UnconfinedFractureSet neighbourGB_ufs = neighbour_gb.getClosestUnconfinedFractureSet(ufs.NormalVector);

                                        // Now check the unconfined fractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                        if (neighbourGB_ufs.checkInUCFStressShadow(new_UCF_centrepointXYZ))
                                        {
                                            addThisFracture = false;
                                        }
                                    }
                                }
                            } // End check unconfined fractures from adjacent gridblocks

                            // Check against large fractures
                            // NB we do not need to do this if we have already found a stress shadow interaction
                            if (addThisFracture && checkLargeFractures)
                            {
                                if (gd.CheckInLargeUCFStressShadow(new_UCF_centrepointXYZ, this, ufs_index, checkAllUCFStressShadows, ref UCFStressShadowWidthRatios))
                                    addThisFracture = false;
                            }

                        } // End check whether this point lies in the stress shadow of an existing unconfined fracture

                        // If the point is not in a stress shadow or we are not including stress shadow effects, generate a new unconfined fracture and add it to the DFN
                        if (addThisFracture)
                        {
                            // Calculate the fracture nucleation time
                            double next_r0 = ufs.InitialRadius(nextFrac_Ln / Volume);
                            double nextUCF_r0_invbeta = Math.Pow(next_r0, 1 / beta);
                            double NucleationWTime = -beta * (nextUCF_r0_invbeta - ts_CumrminGammaMminus1);

                            // Create a new UnconfinedFractureXYZ object and add it to the list of unconfined fractures in the local fracture set - this is used to check for fracture intersection
                            // Fractures will all be created with zero initial radius and then allowed to grow to the mininum unconfined fracture radius
                            // This will ensure boundary intersections and other interactions are correctly modelled
                            UnconfinedFractureXYZ new_UCF = new UnconfinedFractureXYZ(ufs, this, ufs_index, new_UCF_centrepointXYZ, ufs.NormalVector, ufs.RaysPerFracture, 0, NucleationWTime, CurrentExplicitTimestep);
                            ufs.LocalDFNUnconfinedFractures.Add(new_UCF);
                            // Also add it to the list of unconfined fractures in the global DFN - this is used to generate the DFN
                            global_DFN.GlobalDFNUnconfinedFractures.Add(new_UCF);

                            // Add the new fracture ray segments to the list of all unconfined fracture ray segments in the gridblock
                            foreach (UnconfinedFractureRaySegment UCRSegment in new_UCF.GetRaySegmentsInGridblock(this))
                            {
                                UnconfinedFractureRaySegments.Add(new UnconfinedFractureRaySegmentHolder(UCRSegment, ufs_index));
                                double maxPropLength = UCF_minRadius;
#if LOGDFNPOP
                                Dict_UCF_NoActiveNucleating[ufs_index]++;
                                int NoStressShadowInteractions = Dict_UCF_NoStressShadowInteractions[ufs_index];
                                int NoIntersections = Dict_UCF_NoIntersections[ufs_index];
                                int NoPropagatingOut = Dict_UCF_NoPropagatingOut[ufs_index];
                                int NoReachingMaxRadius = Dict_UCF_NoReachingMaxRadius[ufs_index];
                                ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, ufs_index, ufs, UCRSegment, ref maxPropLength, false, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut, ref NoReachingMaxRadius);
                                Dict_UCF_NoStressShadowInteractions[ufs_index] = NoStressShadowInteractions;
                                Dict_UCF_NoIntersections[ufs_index] = NoIntersections;
                                Dict_UCF_NoPropagatingOut[ufs_index] = NoPropagatingOut;
                                Dict_UCF_NoReachingMaxRadius[ufs_index] = NoReachingMaxRadius;
#else
                                ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, ufs_index, ufs, UCRSegment, ref maxPropLength);
#endif
                            }

                            // Recalculate the fracture geometry
                            new_UCF.RecalculateGeometry();

                            // Reset the counter for the number of consecutive failed explicit fracture nucleation attempts to zero
                            ufs.ResetFailedNucleationAttemptCounter();
                        }
                        else
                        {
                            // If the attempt to nucleate a new fracture failed, increment the counter for the number of consecutive failed explicit fracture nucleation attempts
                            // If this causes the fracture set to be considered incapable of nucleating new explicit fractures, break out of the current loop of added new fractures
                            ufs.IncrementFailedNucleationAttemptCounter();
                            if (!ufs.ExplicitNucleationActive)
                                break;
                        }

                        // Get the maximum potential number of fractures when the next fracture nucleates (also assuming there is no stress shadow deactivation)
                        // This time we do need to increment the counter - even if the fracture did not actually nucleate (the counter represents maximum potential nucleated fractures)
                        nextFrac_Ln = ufs.getNextNucleatingFractureIndex(true, allowProbabilisticFractureNucleation);

                        // Update the number of new fracture segments that can be added this timestep; if this drops below zero, break out of the loop
                        if (limitNewFractures) maxNewFractureSegments -= ufs.RaysPerFracture;
                        if (limitNewFractures && (maxNewFractureSegments < 0))
                            break;

                    } // Loop back to check whether to add another fracture
                } // End add unconfined fractures
            } // End loop through unconfined fracture sets

            // If required:
            // - Sort the list of all macrofracture segments in the gridblock in order of nucleation time
            //   This will propagate macrofractures in strict order of nucleation, regardless of fracture set
            // - Sort the list of all unconfined fracture ray segments in the gridblock in order of effective ray length, largest to smallest
            //   This will propagate unconfined fracture ray segments in strict order of effective length, regardless of fracture set
            if (DFNControl.propagateFracturesInNucleationOrder)
            {
                MacrofractureSegments.Sort();
                UnconfinedFractureRaySegments.Sort();
            }

            // Propagate macrofractures, testing for intersection, stress shadow interaction and leaving bounds
            // Loop through each macrofracture segment in the gridblock list
            if (calc_MF)
            {
                // First we will check again that macrofracture segments nucleated at time zero with zero length do not lie in the exclusion zone of macrofracture from another set
                // This is necessary to deactivate dormant initial macrofractures that now lie within the exclusion zones of macrofractures from other sets
                // NB If we have set the ignoreZeroLengthMFStressShadows flag we do not need to check here, as we will check this for all zero length segments (regardless of nucleation time) before propagating them
                if (!ignoreZeroLengthMFStressShadows && checkAlluFStressShadows)
                {
                    foreach (MacrofractureSegmentHolder segmentHolder in MacrofractureSegments)
                    {
                        MacrofractureSegmentIJK MFSegment = segmentHolder.Segment;

                        // Check if macrofracture segment is still active, nucleated at timestep zero and currently has length zero
                        if (MFSegment.Active && (MFSegment.NucleationTimestep == 0) && ((float)MFSegment.StrikeLength == 0f))
                        {
                            // Get the macrofracture set and dipset indices
                            int fs_index = segmentHolder.FractureSetIndex;
                            LayerBoundFractureSet fs = LayerBoundFractureSets[fs_index];
                            int dipsetIndex = MFSegment.FractureDipSetIndex;

                            // Calculate maximum propagation distance - given by integral of alpha_MF * sigmad_b * t for duration of growth
                            // This will be equal to PropLength if the uF nucleated in a previous timestep, or (PropLength - NucleationLTime) if it nucleated in this timestep
                            double fds_maxPropLength = maxPropLengths[fs_index][dipsetIndex];
                            double maxPropLength = ((MFSegment.NucleationTimestep == CurrentExplicitTimestep) ? fds_maxPropLength - MFSegment.NucleationLTime : fds_maxPropLength);

                            // Check if the maximum propagation length is zero - if so we can skip the calculation
                            if (maxPropLength > 0)
                            {
                                // Get the list of stress shadow half-widths of other fracture sets as seen by this fracture set, and to the stress shadow half-widths of this fracture set as seen by other fracture sets
                                List<List<double>> SetI_StressShadowHalfWidthsIJ = StressShadowHalfWidthsIJ[fs_index];
                                List<List<double>> SetI_StressShadowHalfWidthsJI = StressShadowHalfWidthsJI[fs_index];

                                // Check if the propagating node of the macrofracture segment lies in an exclusion zone
                                bool deactivateThisFracture = checkInMFExclusionZone(MFSegment.getPropNodeinXYZ(), fs_index, dipsetIndex, checkAlluFStressShadows, SearchNeighbouringGridblocks(), ref SetI_StressShadowHalfWidthsIJ, ref SetI_StressShadowHalfWidthsJI);

                                // If the segment does lie in the exclusion zone of another macrofracture, deactivate it and move on to the next
                                // NB Although this will deactivate the macrofracture segment, it will not record a reference to the deactivating segment or link it up
                                // We therefore classify it as a nonconnected stress shadow
                                // These fractures will later be removed by the FractureGrid.GenerateDFN() function as they have zero length
                                if (deactivateThisFracture)
                                {
                                    MFSegment.PropNodeType = SegmentNodeType.NonconnectedStressShadow;
                                }

                            } // End check if the maximum propagation length is zero
                        } // End check if macrofracture segment is active, nucleated at timestep zero and currently has length zero
                    } // Loop to next macrofracture segment
                } // End check again that macrofracture segments nucleated at time zero with zero length do not lie in the exclusion zone of macrofracture from another set

                // Propagate macrofracture segments
                foreach (MacrofractureSegmentHolder segmentHolder in MacrofractureSegments)
                {
                    MacrofractureSegmentIJK MFSegment = segmentHolder.Segment;
                    int fs_index = segmentHolder.FractureSetIndex;
                    LayerBoundFractureSet fs = LayerBoundFractureSets[fs_index];
#if LOGDFNPOP
                    // Update counter for total number of fractures and total excluding fractures nucleating during this timestep, relay segments and zero length fractures
                    Dict_MF_NoTotalFracSegments[fs_index]++;
                    Dict_MF_TotalFractureArea[fs_index] += (MFSegment.TotalLength * ThicknessAtDeformation);
                    Dict_MF_TotalFractureVolume[fs_index] += (MFSegment.TotalLength * ThicknessAtDeformation * ThicknessAtDeformation * (Math.PI / 4));
                    bool fromPreviousTS = ((MFSegment.NucleationTimestep < CurrentExplicitTimestep) && (MFSegment.StrikeLength > 0));
                    if (fromPreviousTS) Dict_MF_NoTotalExistingFracSegments[fs_index]++;
#endif
                    // If macrofracture segment is still active, grow it
                    if (MFSegment.Active)
                    {
#if LOGDFNPOP
                        // Update counters for number of active fractures existing already and nucleating in this timestep
                        Dict_MF_NoActiveFracSegments[fs_index]++;
                        if (fromPreviousTS) Dict_MF_NoActiveExistingFracSegments[fs_index]++;
#endif
                        // Get the microfracture dip set index
                        int dipsetIndex = MFSegment.FractureDipSetIndex;

                        // Calculate helper variables
                        // Maximum propagation distance - given by integral of alpha_MF * sigmad_b * t for duration of growth
                        // This will be equal to PropLength if the uF nucleated in a previous timestep, or (PropLength - NucleationLTime) if it nucleated in this timestep
                        double fds_maxPropLength = maxPropLengths[fs_index][dipsetIndex];
                        double maxPropLength = ((MFSegment.NucleationTimestep == CurrentExplicitTimestep) ? fds_maxPropLength - MFSegment.NucleationLTime : fds_maxPropLength);

                        // Check if the maximum propagation length is zero - if so we can skip the calculation
                        if (maxPropLength > 0)
                        {
                            // First we will check again that macrofracture segments with zero length do not lie in the exclusion zone of another macrofracture
                            // This is necessary to deactivate dormant initial macrofractures that now lie within the exclusion zones of other macrofractures
                            if (ignoreZeroLengthMFStressShadows && (float)MFSegment.StrikeLength == 0f)
                            {
                                // Get the list of stress shadow half-widths of other fracture sets as seen by this fracture set, and to the stress shadow half-widths of this fracture set as seen by other fracture sets
                                List<List<double>> SetI_StressShadowHalfWidthsIJ = StressShadowHalfWidthsIJ[fs_index];
                                List<List<double>> SetI_StressShadowHalfWidthsJI = StressShadowHalfWidthsJI[fs_index];

                                // Check if the propagating node of the macrofracture segment lies in an exclusion zone
                                bool deactivateThisFracture = checkInMFExclusionZone(MFSegment.getPropNodeinXYZ(), fs_index, dipsetIndex, checkAlluFStressShadows, SearchNeighbouringGridblocks(), ref SetI_StressShadowHalfWidthsIJ, ref SetI_StressShadowHalfWidthsJI);

                                // If the segment does lie in the exclusion zone of another macrofracture, deactivate it and move on to the next
                                // NB Although this will deactivate the macrofracture segment, it will not record a reference to the deactivating segment or link it up
                                // We therefore classify it as a nonconnected stress shadow
                                // These fractures will later be removed by the FractureGrid.GenerateDFN() function as they have zero length
                                if (deactivateThisFracture)
                                {
                                    MFSegment.PropNodeType = SegmentNodeType.NonconnectedStressShadow;
                                    continue;
                                }
                            }

                            // The process of extending the fracture, after checking for stress shadow interaction, intersection or propagating across a gridblock boundary, is handled by a separate function
#if LOGDFNPOP
                            int NoStressShadowInteractions = Dict_MF_NoStressShadowInteractions[fs_index];
                            int NoIntersections = Dict_MF_NoIntersections[fs_index];
                            int NoPropagatingOut = Dict_MF_NoPropagatingOut[fs_index];
                            ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fs_index, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
                            Dict_MF_NoStressShadowInteractions[fs_index] = NoStressShadowInteractions;
                            Dict_MF_NoIntersections[fs_index] = NoIntersections;
                            Dict_MF_NoPropagatingOut[fs_index] = NoPropagatingOut;
#else
                            ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fs_index, fs, MFSegment, dipsetIndex, ref maxPropLength);
#endif
                        } // End if the maximum propagation length is zero
                    } // End if macrofracture segment is active
                } /// Loop to next macrofracture segment
            } // End propagate macrofractures

            // Propagate unconfined fracture ray segments, testing for intersection, stress shadow interaction and leaving bounds
            // Loop through each unconfined fracture ray segment in the gridblock list
            if (calc_UCF)
            {
                // Propagate unconfined fracture ray segments
                foreach (UnconfinedFractureRaySegmentHolder segmentHolder in UnconfinedFractureRaySegments)
                {
                    UnconfinedFractureRaySegment UCRSegment = segmentHolder.Segment;
                    int ufs_index = segmentHolder.FractureSetIndex;
                    UnconfinedFractureSet ufs = UnconfinedFractureSets[ufs_index];
#if LOGDFNPOP
                    // Update counter for total number of ray segments and total excluding segments nucleating during this timestep
                    bool fromPreviousTS = (UCRSegment.NucleationTimestep < CurrentExplicitTimestep);
                    if (UCRSegment.CheckNucleationGridblock(this))
                    {
                        Dict_UCF_NoTotalFracRays[ufs_index]++;
                        if (fromPreviousTS) Dict_UCF_NoTotalExistingFracRays[ufs_index]++;
                    }
                    double segmentPropagationDistance = -1;
#endif
                    // If macrofracture segment is still active, grow it
                    if (UCRSegment.Active)
                    {
#if LOGDFNPOP
                        // Update counters for number of active ray segments existing already and nucleating in this timestep
                        Dict_UCF_NoActiveFracRays[ufs_index]++;
                        if (fromPreviousTS) Dict_UCF_NoActiveExistingFracRays[ufs_index]++;
#endif
                        // Calculate the maximum propagation distance
                        // This is controlled by the integral of the PropRateCoefficient * t for duration of growth, which is equal to the GrowthFactor if the ray is propagating for the entire timestep
                        double propagatingTime, growthFactor;
                        if (UCRSegment.NucleationTimestep == CurrentExplicitTimestep)
                        {
                            propagatingTime = CurrentExplicitTime - UCRSegment.NucleationTime;
                            growthFactor = ufsGrowthFactors[ufs_index] + (UCRSegment.NucleationWTime / beta);
                        }
                        else
                        {
                            propagatingTime = TimestepDuration;
                            growthFactor = ufsGrowthFactors[ufs_index];
                        }
                        double maxPropLength = calculateUnconfinedFractureRayGrowth(UCRSegment, propagatingTime, growthFactor);

                        // Check if the maximum propagation length is zero - if so we can skip the calculation
                        if (maxPropLength > 0)
                        {
                            // The process of extending the fracture, after checking for stress shadow interaction, intersection or propagating across a gridblock boundary, is handled by a separate function
#if LOGDFNPOP
                            int NoStressShadowInteractions = Dict_UCF_NoStressShadowInteractions[ufs_index];
                            int NoIntersections = Dict_UCF_NoIntersections[ufs_index];
                            int NoPropagatingOut = Dict_UCF_NoPropagatingOut[ufs_index];
                            int NoReachingMaxRadius = Dict_UCF_NoReachingMaxRadius[ufs_index];
                            ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, ufs_index, ufs, UCRSegment, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut, ref NoReachingMaxRadius);
                            Dict_UCF_NoStressShadowInteractions[ufs_index] = NoStressShadowInteractions;
                            Dict_UCF_NoIntersections[ufs_index] = NoIntersections;
                            Dict_UCF_NoPropagatingOut[ufs_index] = NoPropagatingOut;
                            Dict_UCF_NoReachingMaxRadius[ufs_index] = NoReachingMaxRadius;
                            segmentPropagationDistance = maxPropLength;
#else
                            ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, ufs_index, ufs, UCRSegment, ref maxPropLength);
#endif
                        } // End if the maximum propagation length is greater than zero
                    } // End if unconfined fracture ray segment is active
#if LOGDFNPOP
                    // Update string of ray lengths with the final length of this ray
                    Dict_UCF_RayLengths[ufs_index] += string.Format("\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", UCRSegment.RayLength, UCRSegment.Length, segmentPropagationDistance, UCRSegment.RayTipType, UCRSegment.GrowToInitialSize, UCRSegment.NonPropNodeType, UCRSegment.PropNodeType);
                    // Update the fracture area and volume
                    if (UCRSegment.CheckNucleationGridblock(this))
                    {
                        Dict_UCF_TotalFractureArea[ufs_index] += (UCRSegment.RayLength * UCRSegment.RayLength * (Math.PI / ufs.RaysPerFracture));
                        Dict_UCF_TotalFractureVolume[ufs_index] += (UCRSegment.RayLength * UCRSegment.RayLength * UCRSegment.EffectiveRayLength * (4d / 3d) * (Math.PI / ufs.RaysPerFracture));
                    }
                    // Update the cumultive counters of incoming and outgoing rays
                    if (UCRSegment.NonPropNodeType == SegmentNodeType.ConnectedGridblockBound)
                        Dict_UCF_CumulativeIncomingRays[ufs_index]++;
                    if ((UCRSegment.PropNodeType == SegmentNodeType.ConnectedGridblockBound) || (UCRSegment.PropNodeType == SegmentNodeType.NonconnectedGridblockBound))
                        Dict_UCF_CumulativeOutgoingRays[ufs_index]++;
#endif
                } /// Loop to next unconfined fracture ray segment

                // Update the geometry for all fractures
                // This recalculates the minimum, maximum and mean ray lengths, total area and location of centroid for each fracture
                // This is only done after all fracture growth has been calculated for the timestep, as these values will be used to calculate effective radii and fracture growth rates in the next timestep
                // Also check if they have exceeded the minimum size for large fractures during this increment and if so add them to the list of large fractures
                foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
                    foreach (UnconfinedFractureXYZ ucf in ufs.LocalDFNUnconfinedFractures)
                    {
                        ucf.RecalculateGeometry();
                        if (checkLargeFractures && !ucf.LargeFracture && (ucf.EffectiveRadius >= Minimum_Large_UCF_Radius))
                        {
                            gd.LargeFractures.Add(ucf);
                            ucf.LargeFracture = true;
                        }
                    }

            } // End propagate unconfined fracture rays

#if LOGDFNPOP
            // Write fracture counts to logfiles and close them
            for (int fs_index = 0; fs_index < NoLayerBoundFractureSets; fs_index++)
            {
                StreamWriter DFNPopLogFile = DFN_MFPopLogFiles[fs_index];
                if (writeLoggingData)
                    DFNPopLogFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}", CurrentExplicitTimestep, Dict_MF_NoTotalFracSegments[fs_index], Dict_MF_NoActiveFracSegments[fs_index], Dict_MF_NoTotalExistingFracSegments[fs_index], Dict_MF_NoActiveExistingFracSegments[fs_index], Dict_MF_NoStressShadowInteractions[fs_index], Dict_MF_NoIntersections[fs_index], Dict_MF_NoPropagatingOut[fs_index], Dict_MF_NoTotalNucleating[fs_index], Dict_MF_NoActiveNucleating[fs_index], Dict_MF_MostPopulousDipSetIndex[fs_index], Dict_MF_TotalFractureArea[fs_index], Dict_MF_TotalFractureVolume[fs_index], Dict_MF_MeasuredStressShadowVol[fs_index], Dict_MF_MeasuredExclusionZoneVol[fs_index]));
                DFNPopLogFile.Close();
            }
            for (int ufs_index = 0; ufs_index < NoUnconfinedFractureSets; ufs_index++)
            {
                StreamWriter DFNPopLogFile = DFN_UCFPopLogFiles[ufs_index];
                if (writeLoggingData && setsToLog.Contains(ufs_index))
                    DFNPopLogFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}{17}", CurrentExplicitTimestep, Dict_UCF_NoTotalFracRays[ufs_index], Dict_UCF_NoActiveFracRays[ufs_index], Dict_UCF_NoTotalExistingFracRays[ufs_index], Dict_UCF_NoActiveExistingFracRays[ufs_index], Dict_UCF_NoStressShadowInteractions[ufs_index], Dict_UCF_NoIntersections[ufs_index], Dict_UCF_NoPropagatingOut[ufs_index], Dict_UCF_NoReachingMaxRadius[ufs_index], Dict_UCF_NoTotalNucleating[ufs_index], Dict_UCF_NoActiveNucleating[ufs_index], Dict_UCF_CumulativeIncomingRays[ufs_index], Dict_UCF_CumulativeOutgoingRays[ufs_index], Dict_UCF_TotalFractureArea[ufs_index], Dict_UCF_TotalFractureVolume[ufs_index], Dict_UCF_MeasuredStressShadowVol[ufs_index], Dict_UCF_MeasuredExclusionZoneVol[ufs_index], Dict_UCF_RayLengths[ufs_index]));
                DFNPopLogFile.Close();
            }
#endif

            // Determine the return code and return it
            PropagateDFNReturnCode returnCode;
            if (limitNewFractures && (maxNewFractureSegments < 0))
                returnCode = PropagateDFNReturnCode.FractureLimitExceeded;
            else
                returnCode = PropagateDFNReturnCode.Completed;
            return returnCode;
        }
        /// <summary>
        /// Calculate the maximum length of propagation of an unconfined fracture ray
        /// </summary>
        /// <param name="UCRSegment">UnconfinedFractureRaySegment object representing the propagating ray</param>
        /// <param name="propagatingTime">Duration of propagation in real time</param>
        /// <param name="growthFactor">Growth factor (CumGamma) for the duration of propagation</param>
        /// <returns>Maximum length of ray propagation (m)</returns>
        private double calculateUnconfinedFractureRayGrowth(UnconfinedFractureRaySegment UCRSegment, double propagatingTime, double growthFactor)
        {
            // If the propagation time or the growth factor are zero, there will be no growth so we can return 0
            if (!(propagatingTime > 0) || !(growthFactor > 0))
                return 0;

            // The ray is clearly no longer growing to its initial size so we can reset the flag for this
            UCRSegment.GrowToInitialSize = false;

            // Get helper variables
            double CapA = MechProps.CapA;
            double b = MechProps.b_factor;
            double beta = MechProps.beta;
            bool bis2 = (MechProps.GetbType() == bType.Equals2);
            double initialR = UCRSegment.RayLength;
            double maximumEffectiveFractureRadius = UCRSegment.MaximumEffectiveRayLength;
            double finalR, incrementR;

            // Set the growth rate if the fracture has reached the fracture has reached maximum effective radius
            // In this case the stress intensity factor is constant so propagation rate is independent of fracture size
            if (UCRSegment.ConstantKi)
            {
                // The maximum effective ray length has alreeady been reached, so the propagation rate is independent of fracture size
                // However because it is subcritical propagation, the rate is still dependent on driving stress, which may vary during the timestep
                incrementR = bis2 ? maximumEffectiveFractureRadius * (-growthFactor) : beta * Math.Pow(maximumEffectiveFractureRadius, b / 2) * (-growthFactor);
                //finalR = incrementR + initialR;
            }
            // Set the growth rate if the fracture is fully active
            else if (UCRSegment.FractureFullyActive)
            {
                // Calculation of the increment in ray length will depend on whether propagation of the fracture is critical or subcritical
                // First calculate the increment in ray length assuming subcritical fracture propagation
                // Fully active subcritical propagation rate is dependent on driving stress and ray length, both of which may vary during the timestep
                finalR = bis2 ? (initialR * Math.Exp(-growthFactor)) : Math.Pow(Math.Pow(initialR, 1 / beta) - growthFactor, beta);

                // If the maximum effective ray length is reached, subcritical propagation occurs at constant rate beyond this point
                if (finalR > maximumEffectiveFractureRadius)
                {
                    double constantRateIncrement = bis2 ? maximumEffectiveFractureRadius * ((Math.Log(initialR) - Math.Log(maximumEffectiveFractureRadius)) - growthFactor) :
                        beta * Math.Pow(maximumEffectiveFractureRadius, b / 2) * ((Math.Pow(initialR, 1 / beta) - Math.Pow(maximumEffectiveFractureRadius, 1 / beta)) - growthFactor);
                    finalR = maximumEffectiveFractureRadius + constantRateIncrement;
                }

                // Finally calculate the propagation increment
                incrementR = finalR - initialR;
            }
            // Set the growth rate if the fracture is restricted
            else
            {
                // Calculation of the increment in ray length will depend on whether propagation of the fracture is critical or subcritical
                // First calculate the increment in ray length assuming subcritical fracture propagation
                // Restricted subcritical propagation rate is dependent on driving stress and ray length, both of which may vary during the timestep, and on the length of the controlling ray, which is static and will not vary during the timestep
                double initialReff = UCRSegment.EffectiveRayLength;
                double initialRc = UCRSegment.PropagationControllingRayLength;
                double growthComponent = bis2 ? initialReff * Math.Exp(-growthFactor / 2) : Math.Pow(Math.Pow(initialReff, 1 / beta) - (growthFactor / 2), beta);
                finalR = (2 * growthComponent) - initialRc;

                // If the maximum effective ray length is reached, subcritical propagation occurs at constant rate beyond this point
                if (growthComponent > maximumEffectiveFractureRadius)
                {
                    double constantRateIncrement = bis2 ? maximumEffectiveFractureRadius * ((2 * (Math.Log(initialR) - Math.Log(maximumEffectiveFractureRadius))) - growthFactor) :
                        beta * Math.Pow(maximumEffectiveFractureRadius, b / 2) * ((2 * (Math.Pow(initialR, 1 / beta) - Math.Pow(maximumEffectiveFractureRadius, 1 / beta))) - growthFactor);
                    finalR = (2 * maximumEffectiveFractureRadius) + constantRateIncrement - initialRc;
                }

                // Finally calculate the propagation increment
                incrementR = finalR - initialR;
            }

            // It is not possible to calculate analytically the onset of critical fracture propagation if the driving stress is varying during the timestep, as the fracture radius is also not constant
            // Therefore we will check whether the increment in ray length is greater than that which would be achieved by critical fracture propagation, and if so reduce it to this amount
            // This will overestimate the total fracture propagation if the fracture becomes critical during the timestep, but not by as much as if we only check for critical propagation at the start of the timestep
            // NB If the fracture reaches the blow-up radius within this timestep then the finalR calculation will return NaN (this can only happen for R>2)
            // It is therefore also important to check for NaNs
            double criticalPropagationIncrement = CapA * propagatingTime;
            if (((float)incrementR >= (float)criticalPropagationIncrement) || double.IsNaN(incrementR))
            {
                incrementR = criticalPropagationIncrement;
                //finalR = initialR + incrementR;
            }

            return incrementR;
        }
        /// <summary>
        /// Propagate an explicit macrofracture into this gridblock from a neighbouring gridblock
        /// </summary>
        /// <param name="initiatorSegment">Initiator MacrofractureSegmentIJK object in an adjacent gridblock that has propagated into this gridblock</param>
        /// <param name="segmentFSIndex">Index number of initiator MacrofractureSegmentIJK fracture set</param>
        /// <param name="FromBoundary">Boundary from which the fracture has crossed into this gridblock</param>
        /// <param name="insertionPoint">Point at which it crosses the gridblock boundary, in global (XYZ) coordinates</param>
        /// <param name="newSegmentNucleationTime">Real time at which it crosses the gridblock boundary (s)</param>
        /// <param name="use_MF_min_length_cutoff">Flag specifying whether a minimum cutoff length is defined</param>
        /// <param name="checkStressShadow">Flag specifying whether the stress distribution case is set to stress shadow</param>
        /// <param name="ignoreZeroLengthMFStressShadows">If true, do not record a stress shadow interaction if the second fracture segment has zero length</param>
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
#if LOGDFNPOP
        /// <param name="fromPreviousTS">Flag specifying whether fracture nucleated in this timestep or a previous timestep - used for debugging only</param>
        /// <param name="NoStressShadowInteractions">Counter for fracture stress shadow interactions - used for debugging only</param>
        /// <param name="NoIntersections">Counter for fracture intersections - used for debugging only</param>
        /// <param name="NoPropagatingOut">Counter for fractures propagating across gridblock boundaries - used for debugging only</param>
        public void PropagateMFIntoGridblock(MacrofractureSegmentIJK initiatorSegment, int segmentFSIndex, GridDirection FromBoundary, PointXYZ insertionPoint, double newSegmentNucleationTime, bool use_MF_min_length_cutoff, bool checkStressShadow, bool ignoreZeroLengthMFStressShadows, bool TerminateAtGridBoundary, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut)
#else
        private void PropagateMFIntoGridblock(MacrofractureSegmentIJK initiatorSegment, int segmentFSIndex, GridDirection FromBoundary, PointXYZ insertionPoint, double newSegmentNucleationTime, bool use_MF_min_length_cutoff, bool checkStressShadow, bool ignoreZeroLengthMFStressShadows, bool TerminateAtGridBoundary)
#endif
        {
            // Check that the fracture set index for the incoming fracture is not higher than the total number of sets in this gridblock
            if (segmentFSIndex >= NoLayerBoundFractureSets)
                segmentFSIndex = NoLayerBoundFractureSets - 1;
            // If the fracture set index for the incoming fracture is less than 0 (or there are no fracture sets in this gridblock) then abort
            if (segmentFSIndex < 0)
                return;

            // Get the original fracture propagation direction - this will not change
            PropagationDirection original_PropDir = initiatorSegment.SideOfFracture();

            // Get the previous fracture propagation azimuth, and calculate the propagation direction of the equivalent set in this gridblock 
            double previousPropagationDirection = initiatorSegment.getPropagationAzimuth();
            int newSegment_FSIndex = segmentFSIndex;
            PropagationDirection newSegment_PropDir = initiatorSegment.LocalPropDir;
            DipDirection newSegment_DipDir = initiatorSegment.DipDir;
            LayerBoundFractureSet newSegment_fs = LayerBoundFractureSets[segmentFSIndex];
            double currentPropagationDirection = newSegment_fs.getPropagationAzimuth(newSegment_PropDir);

            // Check if the propagation direction of the equivalent set in this gridblock lies within the allowed range
            double maxAngularDifference = gd.DFNControl.MaxConsistencyAngle;
            double angularVariability = PointXYZ.getAngularDifference(currentPropagationDirection, previousPropagationDirection);
            if (angularVariability > maxAngularDifference)
            {
                // If the propagation direction lies within the allowed range, loop through all fracture sets and propagation directions to find the best fit (which may still be the equivalent)
                for (int test_fs_index = 0; test_fs_index < NoLayerBoundFractureSets; test_fs_index++)
                {
                    LayerBoundFractureSet test_fs = LayerBoundFractureSets[test_fs_index];

                    foreach (PropagationDirection test_propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                    {
                        // Check if the difference between the previous propagation direction and this configuration is less than the minimum found so far
                        double test_AngularVariability = PointXYZ.getAngularDifference(test_fs.getPropagationAzimuth(test_propDir), previousPropagationDirection);
                        if (test_AngularVariability < angularVariability)
                        {
                            // If so set the new set and propagation direction to the current configuration; also update the minimum angular difference found so far
                            angularVariability = test_AngularVariability;
                            newSegment_FSIndex = test_fs_index;
                            newSegment_PropDir = test_propDir;
                            newSegment_fs = test_fs;
                        }
                    }
                }

                // If the propagation direction has switched, we will also need to switch the dip direction
                if (newSegment_PropDir != initiatorSegment.LocalPropDir)
                    newSegment_DipDir = (newSegment_DipDir == DipDirection.JPlus ? DipDirection.JMinus : DipDirection.JPlus);
            }

            // Get a reference to the fracture dip set for the new segment
            int newSegment_DipSetIndex;
            int noNewFSDipSets = newSegment_fs.FractureDipSets.Count;
            int initiatorSegmentDipSet = initiatorSegment.FractureDipSetIndex;
            // If there is an equivalent dipset number in the new segment, and the dip of the equivalent dipset lies within the allowed range, then we will assign the new segment to the equivalent dipset
            if ((initiatorSegmentDipSet < noNewFSDipSets) && (Math.Abs(initiatorSegment.getDip() - newSegment_fs.FractureDipSets[initiatorSegmentDipSet].Dip) <= maxAngularDifference))
            {
                newSegment_DipSetIndex = initiatorSegmentDipSet;
            }
            else // Otherwise we will find the dipset that best matches the initiator segment dip
            {
                newSegment_DipSetIndex = 0;
                double dipVariability = double.PositiveInfinity;
                for (int dipSetNo = 0; dipSetNo < noNewFSDipSets; dipSetNo++)
                {
                    FractureDipSet test_fds = newSegment_fs.FractureDipSets[dipSetNo];
                    double test_DipVariability = Math.Abs(initiatorSegment.getDip() - test_fds.Dip);
                    if (test_DipVariability < dipVariability)
                    {
                        dipVariability = test_DipVariability;
                        newSegment_DipSetIndex = dipSetNo;
                    }
                    // For biazimuthal conjugate sets, also check if a match can be made by inverting the dip direction
                    if (test_fds.BiazimuthalConjugate)
                    {
                        test_DipVariability = Math.Abs(Math.PI - initiatorSegment.getDip() - test_fds.Dip);
                        if (test_DipVariability < dipVariability)
                        {
                            dipVariability = test_DipVariability;
                            newSegment_DipSetIndex = dipSetNo;
                            newSegment_DipDir = (newSegment_DipDir == DipDirection.JPlus ? DipDirection.JMinus : DipDirection.JPlus);
                        }
                    }
                }
            }

            // Find the nucleation timestep of the new fracture segment by checking the real nucleation time of the new segment
            // Since the gridblocks have independent timesteps, this may be earlier than the current timestep reached in the calculation of this gridblock
            int newSegment_NucleationTimestep = getTimestepIndex(newSegmentNucleationTime);
            // We will also need to convert the nucleation time to local weighted time (LTime)
            double newSegment_NucleationLTime = newSegment_fs.FractureDipSets[newSegment_DipSetIndex].ConvertTimeToLength(newSegmentNucleationTime, newSegment_NucleationTimestep);

            // Create a new macrofracture segment and add it to the local DFN
            MacrofractureSegmentIJK newSegment = new MacrofractureSegmentIJK(newSegment_fs, newSegment_DipSetIndex, newSegment_fs.convertXYZtoIJK(insertionPoint), FromBoundary, newSegment_PropDir, original_PropDir, newSegment_DipDir, newSegment_NucleationLTime, newSegment_NucleationTimestep);

            // Check if there is a boundary-tracking fracture at the insertion point
            // If so, do not add this fracture segment, and set the initiator node fracture deactivation mechanism to Intersection
            // Loop through every fracture set, including this one
            foreach (LayerBoundFractureSet intersecting_fs in LayerBoundFractureSets)
            {
                // Call the function to check intersection
                if (newSegment_fs.checkFractureIntersectionOnBoundary(newSegment, intersecting_fs, false, true))
                {
                    // Set the fracture deactivation mechanism of the initiator segment to Intersection, and set the reference to the terminating macrofracture segment
                    initiatorSegment.PropNodeType = SegmentNodeType.Intersection;
                    initiatorSegment.TerminatingSegment = newSegment.TerminatingSegment;

                    // Abort the function and return
                    return;
                }
            }

            // Check if the nucleating segment lies in the exclusion zone of another macrofracture segment
            // We need to do this even if we have already searched adjacent gridblocks for stress shadow interaction,
            // since this will only pick up interactions with the stress shadows of fractures propagating in the opposite direction,
            // but we need to check against fractures propagating in the same direction, whose stress shadows may have widened across the gridblock boundary
            // However we only need to check against macrofractures of the same set in the same gridblock
            if (checkStressShadow)
            {
                // We will need to create new stress shadow half-width tables for the new gridblock
                List<List<double>> SetI_StressShadowHalfWidthsIJ = StressShadowHalfWidthsIJ[newSegment_FSIndex];
                List<List<double>> SetI_StressShadowHalfWidthsJI = StressShadowHalfWidthsJI[newSegment_FSIndex];

                // Check if the propagating node of the macrofracture segment lies in an exclusion zone
                bool deactivateThisFracture = checkInMFExclusionZone(insertionPoint, newSegment_FSIndex, newSegment_DipSetIndex, false, false, ref SetI_StressShadowHalfWidthsIJ, ref SetI_StressShadowHalfWidthsJI);

                if (deactivateThisFracture)
                {
                    // Set the fracture deactivation mechanism of the initiator segment to NonconnectedStressShadow, but do not set a reference to an interacting macrofracture segment
                    initiatorSegment.PropNodeType = SegmentNodeType.NonconnectedStressShadow;

                    // Abort the function and return
                    return;
                }
            }

            // Add the new fracture segment to the appropriate fracture segment list for this gridblock
            newSegment_fs.LocalDFNMacrofractureSegments[newSegment_PropDir].Add(newSegment);

            // Also add both the new segments to the list of all fracture segments in the gridblock
            MacrofractureSegments.Add(new MacrofractureSegmentHolder(newSegment, newSegment_FSIndex));

            // Link the new segment to the same global DFN as the initiator segment
            newSegment.linktoGlobalMacrofracture(initiatorSegment);

            // If the new segment nucleated before the end of the last timestep calculated for this fracture set, we will need to propagate the fracture up until that time 
            if (newSegment_NucleationTimestep <= CurrentExplicitTimestep)
            {
                // Roll back the current timestep to the timestep of nucleation
                // We must make a note of the current timestep first and remember to revert to that timestep 
                int LastTimestepCalculated = CurrentExplicitTimestep;
                CurrentExplicitTimestep = newSegment_NucleationTimestep;

                // Calculate the required propagation length
                double propagationLength = newSegment_fs.FractureDipSets[newSegment_DipSetIndex].getCumulativeMFPropagationDistance(LastTimestepCalculated, Math.Max(newSegment_NucleationTimestep - 1, 0)) - newSegment_NucleationLTime;

                if (propagationLength > 0)
                {
#if LOGDFNPOP
                    ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, newSegment_FSIndex, newSegment_fs, newSegment, newSegment_DipSetIndex, ref propagationLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                    ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, newSegment_FSIndex, newSegment_fs, newSegment, newSegment_DipSetIndex, ref propagationLength);
#endif
                }

                // Reset the current timestep to the last timestep calculated
                CurrentExplicitTimestep = LastTimestepCalculated;
            }
        }
        /// <summary>
        /// Propagate an unconfined fracture ray into this gridblock from a neighbouring gridblock
        /// </summary>
        /// <param name="initiatorSegment">Initiator UnconfinedFractureRaySegment object in an adjacent gridblock that has propagated into this gridblock</param>
        /// <param name="initiatorSegment_ufs">Reference to the UnconfinedFractureSet object containing the initiator UnconfinedFractureRaySegment</param>
        /// <param name="FromBoundary">Boundary from which the ray has crossed into this gridblock</param>
        /// <param name="insertionPoint">Point at which it crosses the gridblock boundary, in global (XYZ) coordinates</param>
        /// <param name="initialPropagationDistance">Distance that the new ray should be extended into the new gridblock; this should only be used to apply the minimum radius to newly nucleating rays, and should be set to zero for propagating rays</param>
        /// <param name="newSegmentNucleationTime">Real time at which it crosses the gridblock boundary (s)</param>
        /// <param name="checkStressShadow">Flag specifying whether the stress distribution case is set to stress shadow</param>
        /// <param name="checkLargeFractures">Flag to check stress shadows for large fractures across the the entire grid</param>
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
#if LOGDFNPOP
        /// <param name="fromPreviousTS">Flag specifying whether fracture nucleated in this timestep or a previous timestep - used for debugging only</param>
        /// <param name="NoStressShadowInteractions">Counter for fracture stress shadow interactions - used for debugging only</param>
        /// <param name="NoIntersections">Counter for fracture intersections - used for debugging only</param>
        /// <param name="NoPropagatingOut">Counter for fractures propagating across gridblock boundaries - used for debugging only</param>
        /// <param name="NoReachingMaxRadius">Counter for rays terminating because they reach the maximum length - used for debugging only</param>
        public void PropagateUCRIntoGridblock(UnconfinedFractureRaySegment initiatorSegment, UnconfinedFractureSet initiatorSegment_ufs, GridDirection FromBoundary, PointXYZ insertionPoint, double initialPropagationDistance, double newSegmentNucleationTime, bool checkStressShadow, bool checkLargeFractures, bool TerminateAtGridBoundary, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut, ref int NoReachingMaxRadius)
#else
        private void PropagateUCRIntoGridblock(UnconfinedFractureRaySegment initiatorSegment, UnconfinedFractureSet initiatorSegment_ufs, GridDirection FromBoundary, PointXYZ insertionPoint, double initialPropagationDistance, double newSegmentNucleationTime, bool checkStressShadow, bool checkLargeFractures, bool TerminateAtGridBoundary)
#endif
        {
            // Find the fracture set in this gridblock with the closest orientation to the incoming fracture
            int newSegment_UFSIndex = getClosestUnconfinedFractureSetIndex(initiatorSegment_ufs.NormalVector);
            UnconfinedFractureSet newSegment_ufs = UnconfinedFractureSets[newSegment_UFSIndex];

            // Get the orientation of the ray within this fracture set with the closest orientation to the incoming fracture segment
            VectorXYZ newSegmentOrientation = newSegment_ufs.getClosestRayOrientation(initiatorSegment.UnitVector);

            // Find the nucleation timestep of the new fracture segment by checking the real nucleation time of the new segment
            // Since the gridblocks have independent timesteps, this may be earlier than the current timestep reached in the calculation of this gridblock
            int newSegment_NucleationTimestep = getTimestepIndex(newSegmentNucleationTime);

            // Add a new unconfined fracture ray segment to the fracture
            UnconfinedFractureRaySegment newSegment = initiatorSegment.AddSegment(newSegment_ufs, this, newSegmentOrientation, newSegmentNucleationTime, newSegment_NucleationTimestep);

            // Add the new ray segment to the list of all unconfined fracture ray segments in this gridblock
            UnconfinedFractureRaySegments.Add(new UnconfinedFractureRaySegmentHolder(newSegment, newSegment_UFSIndex));

            // If this is a newly nucleating ray, it will be necessary to extend the ray instantaneously so that it reaches the minimum radius for a nucleating fracture
            // NB this is independent of any additional extension that may be required if the new ray segment nucleated before the end of the last timestep calculated for this gridblock
            if (initialPropagationDistance > 0)
            {
#if LOGDFNPOP
                ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, newSegment_UFSIndex, newSegment_ufs, newSegment, ref initialPropagationDistance, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut, ref NoReachingMaxRadius);
#else
                ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, newSegment_UFSIndex, newSegment_ufs, newSegment, ref initialPropagationDistance);
#endif
                // If the fracture segment has become deactivated while extending the ray to the minimum fracture radius, there is no need to extend it further so we can return
                if (!newSegment.Active)
                    return;
            }

            // If the new ray segment nucleated before the end of the last timestep calculated for this gridblock, we will need to propagate the ray up until that time
            // Unless it is an initial fracture - these will not propagate until activated
            if ((newSegment_NucleationTimestep <= CurrentExplicitTimestep) && (newSegment_NucleationTimestep > 0))
            {
                // Roll back the current timestep to the timestep of nucleation
                // We must make a note of the current timestep first and remember to revert to that timestep 
                int LastTimestepCalculated = CurrentExplicitTimestep;
                double LastTimeCalculated = CurrentExplicitTime;
                CurrentExplicitTimestep = newSegment_NucleationTimestep;

                // Calculate the required propagation length
                // This is controlled by the integral of the PropRateCoefficient * t for duration of growth, which is equal to the GrowthFactor if the ray is propagating for the entire timestep
                double propagatingTime = LastTimeCalculated - newSegmentNucleationTime;
                //double gf1 = newSegment_ufs.getCumulativeFractureGrowthFactor(LastTimestepCalculated, Math.Max(newSegment_NucleationTimestep - 1, 0));
                //double gf2 = (newSegment.NucleationWTime / MechProps.beta);
                double growthFactor = newSegment_ufs.getCumulativeFractureGrowthFactor(LastTimestepCalculated, Math.Max(newSegment_NucleationTimestep - 1, 0)) + (newSegment.NucleationWTime / MechProps.beta);
                //double initialDrivingStress = newSegment_ufs.getConstantDrivingStressU(newSegment_NucleationTimestep) + ((newSegmentNucleationTime - (newSegment_NucleationTimestep > 0 ? TimestepEndTimes[newSegment_NucleationTimestep - 1] : 0)) * newSegment_ufs.getVariableDrivingStressV(newSegment_NucleationTimestep));
                double propagationLength = calculateUnconfinedFractureRayGrowth(newSegment, propagatingTime, growthFactor);

                if (propagationLength > 0)
                {
#if LOGDFNPOP
                    ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, newSegment_UFSIndex, newSegment_ufs, newSegment, ref propagationLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut, ref NoReachingMaxRadius);
#else
                    ExtendUnconfinedFracture(checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, newSegment_UFSIndex, newSegment_ufs, newSegment, ref propagationLength);
#endif
                }

                // Reset the current timestep to the last timestep calculated
                CurrentExplicitTimestep = LastTimestepCalculated;
            }
        }
        /// <summary>
        /// Extend an explicit macrofracture segment by a specified maximum amount, checking for intersection or stress shadow interactions with other fracture segments and whether it crosses the gridblock boundary
        /// </summary>
        /// <param name="use_MF_min_length_cutoff">Flag specifying whether a minimum cutoff length is defined</param>
        /// <param name="checkStressShadow">Flag specifying whether the stress distribution case is set to stress shadow</param>
        /// <param name="ignoreZeroLengthMFStressShadows">If true, do not record a stress shadow interaction if the second fracture segment has zero length</param>
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
        /// <param name="fsIndex">Index number of parent fracture set</param>
        /// <param name="fs">Reference to parent fracture set</param>
        /// <param name="MFSegment">Reference to MacrofractureSegmentIJK object</param>
        /// <param name="dipsetIndex">Dip set index</param>
        /// <param name="maxPropLength">Maximum propagation length - will be truncated if fracture terminates early</param>
#if LOGDFNPOP
        /// <param name="fromPreviousTS">Flag specifying whether fracture nucleated in this timestep or a previous timestep - used for debugging only</param>
        /// <param name="NoStressShadowInteractions">Counter for fracture stress shadow interactions - used for debugging only</param>
        /// <param name="NoIntersections">Counter for fracture intersections - used for debugging only</param>
        /// <param name="NoPropagatingOut">Counter for fractures propagating across gridblock boundaries - used for debugging only</param>
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool ignoreZeroLengthMFStressShadows, bool TerminateAtGridBoundary, int fsIndex, LayerBoundFractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut)
#else
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool ignoreZeroLengthMFStressShadows, bool TerminateAtGridBoundary, int fsIndex, LayerBoundFractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength)
#endif
        {
            // Check if a tracking boundary has been specified - if so call the ExtendBoundaryTrackingFracture function
            if (MFSegment.TrackingBoundary != GridDirection.None)
#if LOGDFNPOP
                return ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                return ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength);
#endif

            // Cache the initial maximum propagation length
            double initial_maxPropLength = maxPropLength;

            // Create a flag for fracture deactivation mechanism
            SegmentNodeType tipDeactivationMechanism = SegmentNodeType.Propagating;

            // Check if the segment will intersect a macrofracture from another set
            // Loop through every other fracture set, except this one
            for (int intersecting_fs_index = 0; intersecting_fs_index < NoLayerBoundFractureSets; intersecting_fs_index++)
            {
                if (intersecting_fs_index != fsIndex)
                {
                    LayerBoundFractureSet intersecting_fs = LayerBoundFractureSets[intersecting_fs_index];
                    if (fs.checkFractureIntersection(MFSegment, intersecting_fs, ref maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.Intersection;
                }
            }

            // Check if the segment will interact with another macrofracture stress shadow
            if (checkStressShadow)
            {
                // Set the flag to ignore stress shadow interactions if the two fracture tips are separated by a third fracture
                // We will do this if the flag for checking microfractures against stress shadows of all fracture sets is set
                bool checkRelayCrossing = PropControl.checkAlluFStressShadows;

                // First check other macrofractures from this gridblock
                if (fs.checkStressShadowInteraction(MFSegment, ref maxPropLength, ignoreZeroLengthMFStressShadows, checkRelayCrossing, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;

                // Then, if required, check macrofractures from adjacent gridblocks
                if (SearchNeighbouringGridblocks())
                {
                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                    // Loop through each gridblock in the list
                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                    {
                        // Find the correct fracture set in the neighbouring gridblock to search
                        LayerBoundFractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fsIndex, fs.Strike);

                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        if (fs.checkStressShadowInteraction(MFSegment, neighbourGB_fs, ref maxPropLength, ignoreZeroLengthMFStressShadows, checkRelayCrossing, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;

                    } // End loop through each gridblock in the list of neighbouring gridblocks

                } // End check macrofractures from adjacent gridblocks

            } // End check if the segment will interact with another macrofracture stress shadow

            // Check if the segment will intersect a gridblock boundary
            GridDirection intersectedBoundary;
            if (fs.checkBoundaryIntersection(MFSegment, ref maxPropLength, out intersectedBoundary, true, TerminateAtGridBoundary))
            {
                // Set fracture deactivation mechanism to ConnectedGridblockBound
                tipDeactivationMechanism = SegmentNodeType.ConnectedGridblockBound;

                // Check if there is a boundary-tracking fracture at the point of intersection
                // If so, set the fracture deactivation mechanism to Intersection
                // Loop through every other fracture set, except this one
                for (int intersecting_fs_index = 0; intersecting_fs_index < NoLayerBoundFractureSets; intersecting_fs_index++)
                {
                    if (intersecting_fs_index != fsIndex)
                    {
                        LayerBoundFractureSet intersecting_fs = LayerBoundFractureSets[intersecting_fs_index];
                        if (fs.checkFractureIntersectionOnBoundary(MFSegment, intersecting_fs, true, true)) tipDeactivationMechanism = SegmentNodeType.Intersection;
                    }
                }
            }

            // Check the maximum propagation length is not negative (this can happen if the propagating node is already outside the gridblock)
            if (maxPropLength < 0)
                maxPropLength = 0;

#if LOGDFNPOP
            // Update counters for different fracture deactivation mechanisms
            if (fromPreviousTS)
            //if (true)
            {
                if (tipDeactivationMechanism == SegmentNodeType.ConnectedStressShadow) NoStressShadowInteractions++;
                if (tipDeactivationMechanism == SegmentNodeType.Intersection) NoIntersections++;
                if (tipDeactivationMechanism == SegmentNodeType.ConnectedGridblockBound) NoPropagatingOut++;
            }
#endif
            // If we are applying a minimum macrofracture length cutoff we also need to check if it intersects or interacts with the stress shadow of a macrofracture below the minimum length
            if (use_MF_min_length_cutoff)
            {
                // Not yet implemented
            }

            // Move the propagating node by the calculated propagation length
            if (MFSegment.LocalPropDir == PropagationDirection.IPlus)
                MFSegment.PropNode.I += maxPropLength;
            else
                MFSegment.PropNode.I -= maxPropLength;

            // If the segment terminated due to interaction with another macrofracture stress shadow, we also need to deactivate that segment
            if (tipDeactivationMechanism == SegmentNodeType.ConnectedStressShadow)
            {
                // Get reference to the terminating segment from this segment - this will have been set by the checkStressShadowInteraction function
                MacrofractureSegmentIJK interacting_MFSegment = MFSegment.TerminatingSegment;

#if LOGDFNPOP
                // Update counter for fracture deactivation due to stress shadow interaction
                if (interacting_MFSegment.Active) NoStressShadowInteractions++;
#endif
                // If required, link the interacting fracture to the current fracture
                if (gd.DFNControl.LinkFracturesInStressShadow)
                {
                    // Create a new relay segment linking the two fractures
                    double interactionLTime = maxPropLength;
                    if (MFSegment.NucleationTimestep == CurrentExplicitTimestep) interactionLTime += MFSegment.NucleationLTime;
                    MacrofractureSegmentIJK relaySegment = new MacrofractureSegmentIJK(fs, dipsetIndex, MFSegment.PropNode, MFSegment.LocalPropDir, MFSegment.SideOfFracture(), MFSegment.DipDir, interactionLTime, CurrentExplicitTimestep);
                    // The propagating node of the relay segment will be at the same location as the propagating node of the interacting segment
                    // However if the interacting segment is in a different gridblock, we will need to convert its coordinates to the local IJK coordinates
                    // For simplicity, we will do this conversion whenever there is a possibility that the two segments may be in different gridblocks, i.e. if set to search neighbouring gridblocks for stress shadow interaction
                    if (SearchNeighbouringGridblocks())
                        relaySegment.PropNode = fs.convertXYZtoIJK(interacting_MFSegment.getPropNodeinXYZ());
                    else
                        relaySegment.PropNode = interacting_MFSegment.PropNode;

                    // Set the node type for both nodes of the relay segment to Relay, set the terminating fracture reference, and deactivate the segment
                    relaySegment.NonPropNodeType = SegmentNodeType.Relay;
                    relaySegment.PropNodeType = SegmentNodeType.Relay;
                    relaySegment.TerminatingSegment = interacting_MFSegment;

                    // We will not add the relay segment to the MacrofractureSegmentHolder list for this gridblock as it is inactive and perpendicular to the other segments in this set
                    // Also, doing so would throw an exception as we would be adding to the collection of local segments while looping through them
                    // However we will add the linking segment to the list of local macrofracture segments for the fracture set, and to the same global DFN as the initiator segment
                    fs.LocalDFNMacrofractureSegments[MFSegment.LocalPropDir].Add(relaySegment);
                    relaySegment.linktoGlobalMacrofracture(MFSegment);

                    // If the interacting fracture is still active combine the other fracture with this one, so they will form a single MacrofractureXYZ object
                    if (interacting_MFSegment.Active)
                        MFSegment.GlobalMacrofracture.CombineMacrofractures(MFSegment.SideOfFracture(), interacting_MFSegment.GlobalMacrofracture, interacting_MFSegment.SideOfFracture());
                }

                // Finally set the propagating node type to stress shadow and set the terminating fracture reference of the interacting fracture to this fracture
                interacting_MFSegment.PropNodeType = SegmentNodeType.ConnectedStressShadow;
                interacting_MFSegment.TerminatingSegment = MFSegment;
            }

            // If the segment propagated into a neighbouring gridblock we need to create a new macrofracture segment in the neighbouring gridblock
            if (tipDeactivationMechanism == SegmentNodeType.ConnectedGridblockBound)
            {
                // Check if there is neighbouring gridblock with thickness greater than the minimum cutoff
                if (NeighbourGridblocks[intersectedBoundary] != null) // There is a neighbouring gridblock
                {
                    if (NeighbourGridblocks[intersectedBoundary].ThicknessAtDeformation <= gd.DFNControl.MinimumLayerThickness) // The neighbouring gridblock is below the minimum thickness cutoff
                    {
                        // Update the flag for fracture deactivation mechanism
                        tipDeactivationMechanism = SegmentNodeType.Pinchout;
                    }
                    else if (intersectedBoundary == MFSegment.NonPropNodeBoundary) // The fracture is crossing back into the same gridblock it has just come from
                    {
                        // If so, set the fracture to track along the boundary
                        MFSegment.TrackingBoundary = intersectedBoundary;

                        // Use the ExtendFractureAlongBoundary function to propagate the fracture along the boundary between the two gridblocks
                        // We will first need to make the segment active again and reset the maximum propagation length
                        MFSegment.PropNodeType = SegmentNodeType.Propagating;
                        maxPropLength = initial_maxPropLength - maxPropLength;

#if LOGDFNPOP
                        ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                        ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength);
#endif
                    }
                    else // The fracture can propagate into the neighbouring gridblock
                    {
                        // Calculate the boundary intersection point (in global XYZ coordinates) and the real intersection time
                        PointXYZ intersectionPoint = fs.convertIJKtoXYZ(MFSegment.PropNode);
                        double intersectionLTime = maxPropLength;
                        if (MFSegment.NucleationTimestep == CurrentExplicitTimestep) intersectionLTime += MFSegment.NucleationLTime;
                        double intersectionRealTime = fs.FractureDipSets[dipsetIndex].ConvertLengthToTime(intersectionLTime, CurrentExplicitTimestep);

                        // Get the boundary it will propagate out from in the neighbouring gridblock - this will be the opposite of the one it progates into in this gridblock
                        GridDirection oppositeBoundary = PointXYZ.GetOppositeDirection(intersectedBoundary);

                        // Call function to create a macrofracture segment in the neighbouring gridblock
#if LOGDFNPOP
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary);
#endif
                    }
                }
                else // No neighbouring gridblock has been defined
                {
                    // Update the flag for fracture deactivation mechanism
                    tipDeactivationMechanism = SegmentNodeType.NonconnectedGridblockBound;
                } // End check if there is neighbouring gridblock
            } // End if the segment propagated into a neighbouring gridblock

            return tipDeactivationMechanism;
        }
        /// <summary>
        /// Extend a boundary tracking fracture along a gridblock boundary by a specified maximum amount, checking for intersection or convergence with other fractures and whether it crosses a gridblock corner
        /// </summary>
        /// <param name="use_MF_min_length_cutoff">Flag specifying whether a minimum cutoff length is defined</param>
        /// <param name="checkStressShadow">Flag specifying whether the stress distribution case is set to stress shadow</param>
        /// <param name="ignoreZeroLengthMFStressShadows">If true, do not record a stress shadow interaction if the second fracture segment has zero length</param>
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
        /// <param name="fsIndex">Index number of parent fracture set</param>
        /// <param name="fs">Reference to parent fracture set</param>
        /// <param name="MFSegment">Reference to MacrofractureSegmentIJK object</param>
        /// <param name="dipsetIndex">Dip set index</param>
        /// <param name="maxPropLength">Maximum propagation length - will be truncated if fracture terminates early</param>
#if LOGDFNPOP
        /// <param name="fromPreviousTS">Flag specifying whether fracture nucleated in this timestep or a previous timestep - used for debugging only</param>
        /// <param name="NoStressShadowInteractions">Counter for fracture stress shadow interactions - used for debugging only</param>
        /// <param name="NoIntersections">Counter for fracture intersections - used for debugging only</param>
        /// <param name="NoPropagatingOut">Counter for fractures propagating across gridblock boundaries - used for debugging only</param>
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendBoundaryTrackingFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool ignoreZeroLengthMFStressShadows, bool TerminateAtGridBoundary, int fsIndex, LayerBoundFractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut)
#else
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendBoundaryTrackingFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool ignoreZeroLengthMFStressShadows, bool TerminateAtGridBoundary, int fsIndex, LayerBoundFractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength)
#endif
        {
            // Get the tracking boundary
            GridDirection TrackingBoundary = MFSegment.TrackingBoundary;

            // If TrackingBoundary is set to none, will call ExtendFracture without specifying a boundary
            if (TrackingBoundary == GridDirection.None)
#if LOGDFNPOP
                return ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                return ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength);
#endif

            // Create a flag for fracture deactivation mechanism
            SegmentNodeType tipDeactivationMechanism = SegmentNodeType.Propagating;

            // Get the boundary segment endpoints
            double boundaryleftI, boundaryleftJ, boundaryrightI, boundaryrightJ;
            fs.getBoundaryEndPoints(TrackingBoundary, out boundaryleftI, out boundaryleftJ, out boundaryrightI, out boundaryrightJ);

            // Calculate the I component of the maximum propagation length projected onto the tracked boundary
            double deltaI = boundaryleftI - boundaryrightI;
            double deltaJ = boundaryleftJ - boundaryrightJ;
            double maxPropLength_Boundary_ratio = maxPropLength / Math.Sqrt(Math.Pow(deltaI, 2) + Math.Pow(deltaJ, 2));
            double projected_maxPropLength = maxPropLength_Boundary_ratio * Math.Abs(deltaI);

            // Check if the segment will intersect a macrofracture from another set
            // Loop through every other fracture set, except this one
            for (int intersecting_fs_index = 0; intersecting_fs_index < NoLayerBoundFractureSets; intersecting_fs_index++)
            {
                if (intersecting_fs_index != fsIndex)
                {
                    LayerBoundFractureSet intersecting_fs = LayerBoundFractureSets[intersecting_fs_index];
                    if (fs.checkBoundaryTrackingFractureIntersection(MFSegment, intersecting_fs, ref projected_maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.Intersection;
                }
            }

            // Check if the segment will converge with another boundary tracking fracture
            if (fs.checkFractureConvergence(MFSegment, ref projected_maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.Convergence;

            // Check if the segment will intersect a gridblock boundary
            GridDirection intersectedBoundary;
            if (fs.checkCornerIntersection(MFSegment, ref projected_maxPropLength, out intersectedBoundary, true, TerminateAtGridBoundary))
            {
                // Set fracture deactivation mechanism to ConnectedGridblockBound
                tipDeactivationMechanism = SegmentNodeType.ConnectedGridblockBound;

                // Check if there is a boundary-tracking fracture at the point of intersection
                // If so, set the fracture deactivation mechanism to Intersection
                // Loop through every other fracture set, except this one
                for (int intersecting_fs_index = 0; intersecting_fs_index < NoLayerBoundFractureSets; intersecting_fs_index++)
                {
                    if (intersecting_fs_index != fsIndex)
                    {
                        LayerBoundFractureSet intersecting_fs = LayerBoundFractureSets[intersecting_fs_index];
                        if (fs.checkFractureIntersectionOnBoundary(MFSegment, intersecting_fs, true, true)) tipDeactivationMechanism = SegmentNodeType.Intersection;
                    }
                }
            }

            // Check the projected maximum propagation length is not negative (this can happen if the propagating node is already outside the gridblock)
            if (projected_maxPropLength < 0)
                projected_maxPropLength = 0;

#if LOGDFNPOP
            // Update counters for different fracture deactivation mechanisms
            if (fromPreviousTS)
            //if (true)
            {
                if (tipDeactivationMechanism == SegmentNodeType.ConnectedStressShadow) NoStressShadowInteractions++;
                if (tipDeactivationMechanism == SegmentNodeType.Intersection) NoIntersections++;
                if (tipDeactivationMechanism == SegmentNodeType.ConnectedGridblockBound) NoPropagatingOut++;
            }
#endif
            // If we are applying a minimum macrofracture length cutoff we also need to check if it intersects or interacts with the stress shadow of a macrofracture below the minimum length
            if (use_MF_min_length_cutoff)
            {
                // Not yet implemented
            }

            // Move the propagating node by the calculated propagation length
            if (MFSegment.LocalPropDir == PropagationDirection.IPlus)
            {
                MFSegment.PropNode.I += projected_maxPropLength;
                MFSegment.PropNode.J += (projected_maxPropLength * (deltaJ / deltaI));
            }
            else
            {
                MFSegment.PropNode.I -= projected_maxPropLength;
                MFSegment.PropNode.J -= (projected_maxPropLength * (deltaJ / deltaI));
            }

            // Convert the I component of the maximum propagation length back into an absolute maximum propagation length
            maxPropLength = projected_maxPropLength * Math.Sqrt(1 + Math.Pow(deltaJ / deltaI, 2));

            // If the segment propagated into a neighbouring gridblock we need to create a new macrofracture segment in the neighbouring gridblock
            if (tipDeactivationMechanism == SegmentNodeType.ConnectedGridblockBound)
            {
                // Check if there is neighbouring gridblock with thickness greater than the minimum cutoff
                if (NeighbourGridblocks[intersectedBoundary] != null) // There is a neighbouring gridblock
                {
                    if (NeighbourGridblocks[intersectedBoundary].ThicknessAtDeformation <= gd.DFNControl.MinimumLayerThickness) // The neighbouring gridblock is below the minimum thickness cutoff
                    {
                        // Update the flag for fracture deactivation mechanism
                        tipDeactivationMechanism = SegmentNodeType.Pinchout;
                    }
                    else
                    {
                        // Calculate the boundary intersection point (in global XYZ coordinates) and the real intersection time
                        PointXYZ intersectionPoint = fs.convertIJKtoXYZ(MFSegment.PropNode);
                        double intersectionLTime = maxPropLength;
                        if (MFSegment.NucleationTimestep == CurrentExplicitTimestep) intersectionLTime += MFSegment.NucleationLTime;
                        double intersectionRealTime = fs.FractureDipSets[dipsetIndex].ConvertLengthToTime(intersectionLTime, CurrentExplicitTimestep);

                        // Get the boundary it will propagate out from in the neighbouring gridblock - this will be the opposite of the one it progates into in this gridblock
                        GridDirection oppositeBoundary = PointXYZ.GetOppositeDirection(intersectedBoundary);

                        // Call function to create a macrofracture segment in the neighbouring gridblock
#if LOGDFNPOP
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, ignoreZeroLengthMFStressShadows, TerminateAtGridBoundary);
#endif
                    }
                }
                else // No neighbouring gridblock has been defined
                {
                    // Update the flag for fracture deactivation mechanism
                    tipDeactivationMechanism = SegmentNodeType.NonconnectedGridblockBound;
                } // End check if there is neighbouring gridblock
            } // End if the segment propagated into a neighbouring gridblock

            return tipDeactivationMechanism;
        }
        /// <summary>
        /// Extend an explicit unconfined fracture ray by a specified maximum amount, checking for intersection or stress shadow interactions with other fracture segments and whether it crosses the gridblock boundary
        /// </summary>
        /// <param name="checkStressShadow">Flag specifying whether the stress distribution case is set to stress shadow</param>
        /// <param name="checkLargeFractures">Flag to check stress shadows for large fractures across the the entire grid</param>
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
        /// <param name="ufsIndex">Index number of parent fracture set</param>
        /// <param name="ufs">Reference to parent fracture set</param>
        /// <param name="UCRSegment">Reference to UnconfinedFractureRaySegment object</param>
        /// <param name="maxPropLength">Maximum propagation length - will be truncated if ray terminates early</param>
#if LOGDFNPOP
        /// <param name="fromPreviousTS">Flag specifying whether the ray nucleated in this timestep or a previous timestep - used for debugging only</param>
        /// <param name="NoStressShadowInteractions">Counter for ray stress shadow interactions - used for debugging only</param>
        /// <param name="NoIntersections">Counter for ray intersections - used for debugging only</param>
        /// <param name="NoPropagatingOut">Counter for rays propagating across gridblock boundaries - used for debugging only</param>
        /// <param name="NoReachingMaxRadius">Counter for rays terminating because they reach the maximum length - used for debugging only</param>
        /// <returns>Flag specifying whether and how the ray terminates early</returns>
        private SegmentNodeType ExtendUnconfinedFracture(bool checkStressShadow, bool checkLargeFractures, bool TerminateAtGridBoundary, int ufsIndex, UnconfinedFractureSet ufs, UnconfinedFractureRaySegment UCRSegment, ref double maxPropLength, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut, ref int NoReachingMaxRadius)
#else
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendUnconfinedFracture(bool checkStressShadow, bool checkLargeFractures, bool TerminateAtGridBoundary, int ufsIndex, UnconfinedFractureSet ufs, UnconfinedFractureRaySegment UCRSegment, ref double maxPropLength)
#endif
        {
            // Cache the initial maximum propagation length
            double initial_maxPropLength = maxPropLength;

            // Create a flag for fracture deactivation mechanism
            SegmentNodeType tipDeactivationMechanism = SegmentNodeType.Propagating;

            // Check if the ray will reach or exceed the maximum fracture radius
            if (ufs.checkMaximumLength(UCRSegment, ref maxPropLength, true))
            {
                // Set fracture deactivation mechanism to Arrested
                tipDeactivationMechanism = SegmentNodeType.Arrested;
            }

            // Check if the segment will intersect a gridblock boundary
            GridDirection intersectedBoundary;
            if (ufs.checkBoundaryIntersection(UCRSegment, ref maxPropLength, out intersectedBoundary, true, TerminateAtGridBoundary))
            {
                // Set fracture deactivation mechanism to ConnectedGridblockBound
                tipDeactivationMechanism = SegmentNodeType.ConnectedGridblockBound;
            }

#if LOGUFRGROWTH
            //if (((UCRSegment.NonPropNode.Z <= 2000) && (UCRSegment.PropNode.Z >= 2000)) || ((UCRSegment.PropNode.Z <= 2000) && (UCRSegment.NonPropNode.Z >= 2000)) ||
            //    ((UCRSegment.NonPropNode.Z <= 3000) && (UCRSegment.PropNode.Z >= 3000)) || ((UCRSegment.PropNode.Z <= 3000) && (UCRSegment.NonPropNode.Z >= 3000)))
            if ((UCRSegment.PropNodeType == SegmentNodeType.ConnectedGridblockBound) && ((UCRSegment.PropNodeBoundary == GridDirection.U) || (UCRSegment.PropNodeBoundary == GridDirection.D)))
            {
                string outputline = "1\tAfter ufs.checkBoundaryIntersection\t";
                outputline += string.Format("{0}\t{1}\t{2}\t", SWtop.X, SWtop.Y, SWtop.Z);
                outputline += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t",
                    UCRSegment.UnconfinedFractureID, ufsIndex, UCRSegment.NonPropNodeType, UCRSegment.NonPropNodeBoundary, UCRSegment.NonPropNode.X, UCRSegment.NonPropNode.Y, UCRSegment.NonPropNode.Z,
                    UCRSegment.PropNodeType, UCRSegment.PropNodeBoundary, UCRSegment.PropNode.X, UCRSegment.PropNode.Y, UCRSegment.PropNode.Z);
                outputline += string.Format("{0}\t", UCRSegment.ufr.NoSegments);
                foreach (PointXYZ node in UCRSegment.ufr.GetNodesInXYZ())
                    outputline += string.Format("{0}\t{1}\t{2}\t", node.X, node.Y, node.Z);
                Slb.Ocean.Petrel.PetrelLogger.InfoOutputWindow(outputline);
            }
#endif

            // Check if the segment will intersect an unconfined fracture from another set
            // Loop through every other fracture set, except this one
            for (int intersecting_ufs_index = 0; intersecting_ufs_index < NoUnconfinedFractureSets; intersecting_ufs_index++)
            {
                if (intersecting_ufs_index != ufsIndex)
                {
                    UnconfinedFractureSet intersecting_ufs = UnconfinedFractureSets[intersecting_ufs_index];
                    if (ufs.checkUnconfinedFractureIntersection(UCRSegment, intersecting_ufs, ref maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.Intersection;
                }
            }

            // Check if the segment will interact with another unconfined fracture stress shadow
            if (checkStressShadow)
            {
                // First check other unconfined fractures from this gridblock
                if (ufs.checkStressShadowInteraction(UCRSegment, ref maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;

                // Then, if required, check unconfined fractures from adjacent gridblocks
                if (SearchNeighbouringGridblocks())
                {
                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                    // Loop through each gridblock in the list
                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                    {
                        // Find the correct fracture set in the neighbouring gridblock to search
                        UnconfinedFractureSet neighbourGB_ufs = neighbour_gb.getClosestUnconfinedFractureSet(ufs.NormalVector);

                        // Now check the unconfined fractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        // For now we will not apply a stress shadow width multiplier to take account of the difference in orientation between the two fractures
                        if (ufs.checkStressShadowInteraction(UCRSegment, neighbourGB_ufs, ref maxPropLength, 1, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;

                    } // End loop through each gridblock in the list of neighbouring gridblocks

                } // End check unconfined fractures from adjacent gridblocks

                // Finally, if required, check large unconfined fractures
                if (checkLargeFractures)
                {
                    if (gd.checkLargeUCFStressShadowInteraction(UCRSegment, ufs, this, ref maxPropLength, 1, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;
                }

            } // End check if the segment will interact with another unconfined fracture stress shadow

            // Check the maximum propagation length is not negative (this can happen if the propagating node is already outside the gridblock)
            if (maxPropLength < 0)
                maxPropLength = 0;

#if LOGDFNPOP
            // Update counters for different fracture deactivation mechanisms
            //if (fromPreviousTS)
            {
                if (tipDeactivationMechanism == SegmentNodeType.ConnectedStressShadow) NoStressShadowInteractions++;
                if (tipDeactivationMechanism == SegmentNodeType.Intersection) NoIntersections++;
                if (tipDeactivationMechanism == SegmentNodeType.ConnectedGridblockBound) NoPropagatingOut++;
                if (tipDeactivationMechanism == SegmentNodeType.Arrested) NoReachingMaxRadius++;
            }
#endif

#if LOGUFRGROWTH
            //if (((UCRSegment.NonPropNode.Z <= 2000) && (UCRSegment.PropNode.Z >= 2000)) || ((UCRSegment.PropNode.Z <= 2000) && (UCRSegment.NonPropNode.Z >= 2000)) ||
            //    ((UCRSegment.NonPropNode.Z <= 3000) && (UCRSegment.PropNode.Z >= 3000)) || ((UCRSegment.PropNode.Z <= 3000) && (UCRSegment.NonPropNode.Z >= 3000)))
            if ((UCRSegment.PropNodeType == SegmentNodeType.ConnectedGridblockBound) && ((UCRSegment.PropNodeBoundary == GridDirection.U) || (UCRSegment.PropNodeBoundary == GridDirection.D)))
            {
                string outputline = "2\tBefore UCRSegment.Length += maxPropLength\t";
                outputline += string.Format("{0}\t{1}\t{2}\t", SWtop.X, SWtop.Y, SWtop.Z);
                outputline += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t",
                    UCRSegment.UnconfinedFractureID, ufsIndex, UCRSegment.NonPropNodeType, UCRSegment.NonPropNodeBoundary, UCRSegment.NonPropNode.X, UCRSegment.NonPropNode.Y, UCRSegment.NonPropNode.Z,
                    UCRSegment.PropNodeType, UCRSegment.PropNodeBoundary, UCRSegment.PropNode.X, UCRSegment.PropNode.Y, UCRSegment.PropNode.Z);
                outputline += string.Format("{0}\t", UCRSegment.ufr.NoSegments);
                foreach (PointXYZ node in UCRSegment.ufr.GetNodesInXYZ())
                    outputline += string.Format("{0}\t{1}\t{2}\t", node.X, node.Y, node.Z);
                Slb.Ocean.Petrel.PetrelLogger.InfoOutputWindow(outputline);
            }
#endif

            // Increment the propagating ray segment by the calculated propagation length
            // First cache the ray length and effective length prior to the increment
            double r_init = UCRSegment.RayLength;
            double reff_init = UCRSegment.EffectiveRayLength;
            UCRSegment.Length += maxPropLength;

#if LOGUFRGROWTH
            //if (((UCRSegment.NonPropNode.Z <= 2000) && (UCRSegment.PropNode.Z >= 2000)) || ((UCRSegment.PropNode.Z <= 2000) && (UCRSegment.NonPropNode.Z >= 2000)) ||
            //    ((UCRSegment.NonPropNode.Z <= 3000) && (UCRSegment.PropNode.Z >= 3000)) || ((UCRSegment.PropNode.Z <= 3000) && (UCRSegment.NonPropNode.Z >= 3000)))
            if ((UCRSegment.PropNodeType == SegmentNodeType.ConnectedGridblockBound) && ((UCRSegment.PropNodeBoundary == GridDirection.U) || (UCRSegment.PropNodeBoundary == GridDirection.D)))
            {
                string outputline = "3\tAfter UCRSegment.Length += maxPropLength\t";
                outputline += string.Format("{0}\t{1}\t{2}\t", SWtop.X, SWtop.Y, SWtop.Z);
                outputline += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t",
                    UCRSegment.UnconfinedFractureID, ufsIndex, UCRSegment.NonPropNodeType, UCRSegment.NonPropNodeBoundary, UCRSegment.NonPropNode.X, UCRSegment.NonPropNode.Y, UCRSegment.NonPropNode.Z,
                    UCRSegment.PropNodeType, UCRSegment.PropNodeBoundary, UCRSegment.PropNode.X, UCRSegment.PropNode.Y, UCRSegment.PropNode.Z);
                outputline += string.Format("{0}\t", UCRSegment.ufr.NoSegments);
                foreach (PointXYZ node in UCRSegment.ufr.GetNodesInXYZ())
                    outputline += string.Format("{0}\t{1}\t{2}\t", node.X, node.Y, node.Z);
                Slb.Ocean.Petrel.PetrelLogger.InfoOutputWindow(outputline);
            }
#endif
            /*// If the segment terminated due to interaction with another fracture stress shadow, we also need to deactivate that segment
            if (tipDeactivationMechanism == SegmentNodeType.ConnectedStressShadow)
            {
                // Get reference to the terminating segment from this segment - this will have been set by the checkStressShadowInteraction function
                UnconfinedFractureRaySegment interacting_UCRSegment = UCRSegment.TerminatingSegment;

                // Check if the terminating segment is sufficiently small to be deactivated by this segment
                if (interacting_UCRSegment.EffectiveRayLength < (UCRSegment.EffectiveRayLength * PropControl.MinStressShadowDeactivationRatio))
                {

#if LOGDFNPOP
                    // Update counter for fracture deactivation due to stress shadow interaction
                    if (interacting_UCRSegment.Active) NoStressShadowInteractions++;
#endif
                    // If required, link the interacting fracture to the current fracture
                    if (gd.DFNControl.LinkFracturesInStressShadow)
                    {
                        // Relay segments not currently implemented for unconfined fractures
                    }

                    // Finally set the propagating node type to stress shadow and set the terminating fracture reference of the interacting fracture to this fracture
                    interacting_UCRSegment.PropNodeType = SegmentNodeType.ConnectedStressShadow;
                    interacting_UCRSegment.TerminatingSegment = UCRSegment;
                }
            }*/

            // If the segment propagated into a neighbouring gridblock we need to create a new unconfined fracture segment in the neighbouring gridblock
            if (tipDeactivationMechanism == SegmentNodeType.ConnectedGridblockBound)
            {
                // Check if there is neighbouring gridblock with thickness greater than the minimum cutoff to propagate into
                if (NeighbourGridblocks[intersectedBoundary] != null) // There is a neighbouring gridblock
                {
                    // Pinchout does not apply to unconfined fractures
                    /*if (NeighbourGridblocks[intersectedBoundary].ThicknessAtDeformation <= gd.DFNControl.MinimumLayerThickness) // The neighbouring gridblock is below the minimum thickness cutoff
                    {
                        // Update the flag for fracture deactivation mechanism
                        tipDeactivationMechanism = SegmentNodeType.Pinchout;
                    }
                    else*/
                    if (intersectedBoundary == UCRSegment.NonPropNodeBoundary) // The ray segment is crossing back into the same gridblock it has just come from
                    {
                        // If so, deactivate the segment
                        UCRSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                    }
                    else // The ray can propagate into the neighbouring gridblock
                    {
                        // Get the boundary intersection point (in global XYZ coordinates) and the real intersection time
                        PointXYZ intersectionPoint = UCRSegment.PropNode;

                        // Calculate the time that the ray segment crosses the boundary and the initial propagation distance
                        double intersectionRealTime, initialPropagationDistance;
                        if (UCRSegment.GrowToInitialSize)
                        {
                            // Growing the ray to its initial size is assumed to occur instantaneously, at the nucleation time
                            intersectionRealTime = UCRSegment.NucleationTime;
                            // In this case, the distance that the new ray should be extended into the new gridblock is determined by the initial propagation distance parameter
                            initialPropagationDistance = initial_maxPropLength - maxPropLength;
                        }
                        else
                        {
                            // First calculate the intersection time assuming subcritical fracture propagation
                            if (UCRSegment.FractureFullyActive)
                            {
                                double Wt0 = (UCRSegment.NucleationTimestep == CurrentExplicitTimestep) ? UCRSegment.NucleationWTime : 0;
                                double r_final = UCRSegment.RayLength;
                                bool bis2 = (MechProps.GetbType() == bType.Equals2);
                                double beta = MechProps.beta;
                                double dWt;
                                if (bis2)
                                    dWt = Math.Log(r_final) - Math.Log(r_init);
                                else
                                    dWt = (Math.Pow(r_final, 1 / beta) - Math.Pow(r_init, 1 / beta)) * beta;
                                intersectionRealTime = ufs.ConvertWeightedTimeToTime(Wt0 + dWt, currentExplicitTimestep);
                            }
                            else
                            {
                                double Wt0 = (UCRSegment.NucleationTimestep == CurrentExplicitTimestep) ? UCRSegment.NucleationWTime : 0;
                                double reff_final = UCRSegment.EffectiveRayLength;
                                bool bis2 = (MechProps.GetbType() == bType.Equals2);
                                double beta = MechProps.beta;
                                double dWt;
                                if (bis2)
                                    dWt = 2 * (Math.Log(reff_final) - Math.Log(reff_init));
                                else
                                    dWt = 2 * (Math.Pow(reff_final, 1 / beta) - Math.Pow(reff_init, 1 / beta)) * beta;
                                intersectionRealTime = ufs.ConvertWeightedTimeToTime(Wt0 + dWt, currentExplicitTimestep);
                            }
                            // Calculate the intersection time for critical fracture propagation
                            // If the intersection time for critical fracture propagation is greater than the intersection time for subcritical propagation then the fracture must be propagating critically
                            {
                                double PreviousExplicitTime = (CurrentExplicitTimestep > 0) ? TimestepEndTimes[CurrentExplicitTimestep - 1] : 0;
                                double t0 = (UCRSegment.NucleationTimestep == CurrentExplicitTimestep) ? UCRSegment.NucleationTime : PreviousExplicitTime;
                                double criticalIntersectionRealTime = t0 + (maxPropLength / MechProps.CapA);
                                if (intersectionRealTime < criticalIntersectionRealTime)
                                    intersectionRealTime = criticalIntersectionRealTime;
                            }
                            // The initial propagation distance is only used to apply the minimum radius to newly nucleating rays, and should be set to zero for propagating rays
                            initialPropagationDistance = 0;
                        }

                        // Get the boundary it will propagate out from in the neighbouring gridblock - this will be the opposite of the one it propagates into in this gridblock
                        GridDirection oppositeBoundary = PointXYZ.GetOppositeDirection(intersectedBoundary);

                        // Call function to create an unconfined fracture segment in the neighbouring gridblock
#if LOGDFNPOP
                        NeighbourGridblocks[intersectedBoundary].PropagateUCRIntoGridblock(UCRSegment, ufs, oppositeBoundary, intersectionPoint, initialPropagationDistance, intersectionRealTime, checkStressShadow, checkLargeFractures, TerminateAtGridBoundary, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut, ref NoReachingMaxRadius);
#else
                        NeighbourGridblocks[intersectedBoundary].PropagateUCRIntoGridblock(UCRSegment, ufs, oppositeBoundary, intersectionPoint, initialPropagationDistance, intersectionRealTime, checkStressShadow, checkLargeFractures, TerminateAtGridBoundary);
#endif
                    }
                }
                else // No neighbouring gridblock has been defined
                {
                    // Update the flag for fracture deactivation mechanism
                    tipDeactivationMechanism = SegmentNodeType.NonconnectedGridblockBound;

                } // End check if there is neighbouring gridblock

#if LOGUFRGROWTH
                //if (((UCRSegment.NonPropNode.Z <= 2000) && (UCRSegment.PropNode.Z >= 2000)) || ((UCRSegment.PropNode.Z <= 2000) && (UCRSegment.NonPropNode.Z >= 2000)) ||
                //    ((UCRSegment.NonPropNode.Z <= 3000) && (UCRSegment.PropNode.Z >= 3000)) || ((UCRSegment.PropNode.Z <= 3000) && (UCRSegment.NonPropNode.Z >= 3000)))
                if ((UCRSegment.PropNodeType == SegmentNodeType.ConnectedGridblockBound) && ((UCRSegment.PropNodeBoundary == GridDirection.U) || (UCRSegment.PropNodeBoundary == GridDirection.D)))
                {
                    string outputline = "4\tAfter PropagateUCRIntoGridblock\t";
                    outputline += string.Format("{0}\t{1}\t{2}\t", SWtop.X, SWtop.Y, SWtop.Z);
                    outputline += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t",
                        UCRSegment.UnconfinedFractureID, ufsIndex, UCRSegment.NonPropNodeType, UCRSegment.NonPropNodeBoundary, UCRSegment.NonPropNode.X, UCRSegment.NonPropNode.Y, UCRSegment.NonPropNode.Z,
                        UCRSegment.PropNodeType, UCRSegment.PropNodeBoundary, UCRSegment.PropNode.X, UCRSegment.PropNode.Y, UCRSegment.PropNode.Z);
                    outputline += string.Format("{0}\t", UCRSegment.ufr.NoSegments);
                    foreach (PointXYZ node in UCRSegment.ufr.GetNodesInXYZ())
                        outputline += string.Format("{0}\t{1}\t{2}\t", node.X, node.Y, node.Z);
                    Slb.Ocean.Petrel.PetrelLogger.InfoOutputWindow(outputline);
                }
#endif

            } // End if the segment propagated into a neighbouring gridblock

            return tipDeactivationMechanism;
        }
        /// <summary>
        /// Check whether a specified point (in global XYZ coordinates) lies within the stress shadow of another macrofracture segment
        /// </summary>
        /// <param name="point">Point to check in XYZ coordinates</param>
        /// <param name="fs_index">Index of the main fracture set to check</param>
        /// <param name="checkAllFractureSets">Flag to check against stress shadows of all macrofractures, regardless of set; if false will only check against stress shadows of macrofractures in the same set</param>
        /// <param name="searchNeighbouringGridblocks">Flag to check against stress shadows of macrofractures in neighbouring gridblocks; if false will only check against stress shadows of macrofractures in this gridblock</param>
        /// <param name="StressShadowHalfWidthsIJ">Reference to a list of stress shadow half-widths of other fracture sets as seen by this fracture set</param>
        /// <returns>true if the specified point lies within a macrofracture stress shadow, otherwise false</returns>
        private bool checkInMFStressShadow(PointXYZ point, int fs_index, bool checkAllFractureSets, bool searchNeighbouringGridblocks, ref List<List<double>> StressShadowHalfWidthsIJ)
        {
            // First check other macrofractures from this gridblock
            bool fractureLiesInStressShadow;
            if (checkAllFractureSets)
                fractureLiesInStressShadow = checkInMFStressShadow(point, fs_index, ref StressShadowHalfWidthsIJ);
            else
                fractureLiesInStressShadow = LayerBoundFractureSets[fs_index].checkInMFStressShadow(point);

            // Then, if required, check macrofractures from adjacent gridblocks
            // NB we do not need to do this if we have already found a stress shadow interaction
            if (!fractureLiesInStressShadow && searchNeighbouringGridblocks)
            {
                // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                // Loop through each gridblock in the list
                foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                {
                    if (checkAllFractureSets)
                    {
                        // Find the index number of the equivalent fracture set in the neighbouring gridblock
                        int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, LayerBoundFractureSets[fs_index].Strike);

                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                        // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                        // We will therefore use the list generated for this gridblock to speed up the calculation
                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                        if (neighbour_gb.checkInMFStressShadow(point, neighbourGB_fs_index, ref StressShadowHalfWidthsIJ))
                        {
                            fractureLiesInStressShadow = true;
                            break;
                        }
                    }
                    else
                    {
                        // Find the correct fracture set in the neighbouring gridblock to search
                        LayerBoundFractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fs_index, LayerBoundFractureSets[fs_index].Strike);

                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        // Since the neighbouring gridblock will have different local coordinates, we must supply the location of the new macrofracture nucleation point in global XYZ coordinates
                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                        if (neighbourGB_fs.checkInMFStressShadow(point))
                        {
                            fractureLiesInStressShadow = true;
                            break;
                        }
                    }
                }
            } // End check macrofractures from adjacent gridblocks

            return fractureLiesInStressShadow;
        }
        /// <summary>
        /// Check whether a specified point (in global XYZ coordinates) lies within the exclusion zone of another macrofracture segment
        /// </summary>
        /// <param name="point">Point to check in XYZ coordinates</param>
        /// <param name="fs_index">Index of the main fracture set to check</param>
        /// <param name="dipsetIndex">Index of the main fracture dipset to check</param>
        /// <param name="checkAllFractureSets">Flag to check against stress shadows of all macrofractures, regardless of set; if false will only check against stress shadows of macrofractures in the same set</param>
        /// <param name="searchNeighbouringGridblocks">Flag to check against stress shadows of macrofractures in neighbouring gridblocks; if false will only check against stress shadows of macrofractures in this gridblock</param>
        /// <param name="StressShadowHalfWidthsIJ">Reference to a list of stress shadow half-widths of other fracture sets as seen by this fracture set</param>
        /// <param name="StressShadowHalfWidthsJI">Reference to a list of stress shadow half-widths of this fracture set as seen by other fracture sets</param>
        /// <returns>true if the specified point lies within a macrofracture exclusion zone, otherwise false</returns>
        private bool checkInMFExclusionZone(PointXYZ point, int fs_index, int dipsetIndex, bool checkAllFractureSets, bool searchNeighbouringGridblocks, ref List<List<double>> StressShadowHalfWidthsIJ, ref List<List<double>> StressShadowHalfWidthsJI)
        {
            // First check other macrofractures from this gridblock
            bool fractureLiesInExclusionZone;
            if (checkAllFractureSets)
                fractureLiesInExclusionZone = checkInMFExclusionZone(point, fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsIJ);
            else
                fractureLiesInExclusionZone = LayerBoundFractureSets[fs_index].checkInMFExclusionZone(point, LayerBoundFractureSets[fs_index].FractureDipSets[dipsetIndex].Mean_MF_StressShadowWidth);

            // Then, if required, check macrofractures from adjacent gridblocks
            // NB we do not need to do this if we have already found a stress shadow interaction
            if (!fractureLiesInExclusionZone && searchNeighbouringGridblocks)
            {
                // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                // Loop through each gridblock in the list
                foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                {
                    if (checkAllFractureSets)
                    {
                        // Find the index number of the equivalent fracture set in the neighbouring gridblock
                        int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, LayerBoundFractureSets[fs_index].Strike);

                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                        // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                        // We will therefore use the list generated for this gridblock to speed up the calculation
                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                        if (neighbour_gb.checkInMFExclusionZone(point, neighbourGB_fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI))
                        {
                            fractureLiesInExclusionZone = true;
                            break;
                        }
                    }
                    else
                    {
                        // Get the width of the stress shadow of this segment
                        double MF_StressShadowWidth = LayerBoundFractureSets[fs_index].FractureDipSets[dipsetIndex].Mean_MF_StressShadowWidth;

                        // Find the correct fracture set in the neighbouring gridblock to search
                        LayerBoundFractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fs_index, LayerBoundFractureSets[fs_index].Strike);

                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        // Since the neighbouring gridblock will have different local coordinates, we must supply the location of the new macrofracture nucleation point in global XYZ coordinates
                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                        if (neighbourGB_fs.checkInMFExclusionZone(point, MF_StressShadowWidth))
                        {
                            fractureLiesInExclusionZone = true;
                            break;
                        }
                    }
                }
            } // End check macrofractures from adjacent gridblocks

            return fractureLiesInExclusionZone;
        }
        /// <summary>
        /// Check whether a specified point (in global XYZ coordinates) lies within the stress shadow of a macrofracture segment from any fracture set
        /// </summary>
        /// <param name="point">Point to check in XYZ coordinates</param>
        /// <param name="FSJ_Index">Index number of the fracture set to which the point belongs</param>
        /// <param name="StressShadowHalfWidthsIJ">Reference to a nested list of stress shadow half widths for each dip set in each fracture set, as seen by this fracture - if this is null, a new list will be created</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        private bool checkInMFStressShadow(PointXYZ point, int FSJ_Index, ref List<List<double>> StressShadowHalfWidthsIJ)
        {
            // Check the specified fracture set number lies within the range of fracture sets
            // Otherwise return false
            if ((FSJ_Index < 0) || (FSJ_Index >= NoLayerBoundFractureSets))
                return false;

            // Get a handle to the fracture set to which the specified point belongs (set J)
            LayerBoundFractureSet FSJ = LayerBoundFractureSets[FSJ_Index];

            // Create a new list for stress shadow half-widths if one does not already exist
            if (StressShadowHalfWidthsIJ == null)
                StressShadowHalfWidthsIJ = new List<List<double>>();

            // Loop through all the fracture sets
            for (int FSI_Index = 0; FSI_Index < NoLayerBoundFractureSets; FSI_Index++)
            {
                // Get a handle to fracture set I
                LayerBoundFractureSet FSI = LayerBoundFractureSets[FSI_Index];

                // Check if we already have a list of IJ stress shadow half-widths for this fracture set, and if not, create one
                while (StressShadowHalfWidthsIJ.Count <= FSI_Index)
                    StressShadowHalfWidthsIJ.Add(new List<double>());

                // Check if there is a value for each dipset in the list, otherwise create a new list
                int NoDipsetsI = FSI.FractureDipSets.Count;
                if (StressShadowHalfWidthsIJ[FSI_Index].Count < NoDipsetsI)
                {
                    // Clear any data currently in the list
                    StressShadowHalfWidthsIJ[FSI_Index].Clear();

                    // Cache the appropriate azimuthal and strike-slip shear stress shadow multipliers for sets I and J locally
                    double Faa_IJ = FaaIJ[FSI_Index, FSJ_Index];
                    double Fas_IJ = FasIJ[FSI_Index, FSJ_Index];

                    // Create a list of the stress shadow half-widths for each dip set in fracture set I, as seen by a fracture from set J
                    // NB we calculate the stress shadow half-widths (i.e. the width of the stress shadow on one side of the fracture only) so we can pass this directly to the FSI.checkInMFProximityZone function
                    for (int dipsetIndexI = 0; dipsetIndexI < NoDipsetsI; dipsetIndexI++)
                    {
                        // Get the azimuthal and strike-slip shear components of the mean stress shadow width for this set I dip set
                        double WaaI = FSI.FractureDipSets[dipsetIndexI].getMeanAzimuthalStressShadowWidth(-1);
                        double WasI = FSI.FractureDipSets[dipsetIndexI].getMeanShearStressShadowWidth(-1);
                        // Calculate the stress shadow width of this set I dip set, as seen by fracture set J, and add it to the half-width list
                        double WIJ = (WaaI * Faa_IJ) + (WasI * Fas_IJ);
                        StressShadowHalfWidthsIJ[FSI_Index].Add(WIJ / 2);
                    }
                }

                // Check if the specified point lies within the stress shadow of any of the macrofracture segments in fracture set I
                if (FSI.checkInMFProximityZone(point, StressShadowHalfWidthsIJ[FSI_Index]))
                    return true;
            }

            // If the specified point does not lie in the stress shadow of any segments, return false
            return false;
        }
        /// <summary>
        /// Check whether a specified point (in global XYZ coordinates) lies within the stress shadow of a macrofracture segment from any fracture set in this gridblock
        /// </summary>
        /// <param name="point">Point to check in XYZ coordinates</param>
        /// <param name="FSJ_Index">Index number of the fracture set to which the point belongs</param>
        /// <param name="FSJ_DipSet_Index">Index number of the fracture dip set to which the point belongs</param>
        /// <param name="StressShadowHalfWidthsIJ">Reference to a nested list of stress shadow half widths for each dip set in each fracture set, as seen by this fracture - if this is null, a new list will be created</param>
        /// <param name="StressShadowHalfWidthsJI">Reference to a nested list of stress shadow half widths this fracture, as seen by every fracture set - if this is null, a new list will be created</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        private bool checkInMFExclusionZone(PointXYZ point, int FSJ_Index, int FSJ_DipSet_Index, ref List<List<double>> StressShadowHalfWidthsIJ, ref List<List<double>> StressShadowHalfWidthsJI)
        {
            // Check the specified fracture set number lies within the range of fracture sets
            // Otherwise return false
            if ((FSJ_Index < 0) || (FSJ_Index >= NoLayerBoundFractureSets))
                return false;

            // Get a handle to the fracture set to which the specified point belongs (set J)
            LayerBoundFractureSet FSJ = LayerBoundFractureSets[FSJ_Index];

            // Check the specified fracture dip set number lies within the range of fracture dip sets
            // Otherwise return false
            if ((FSJ_DipSet_Index < 0) || (FSJ_DipSet_Index >= FSJ.FractureDipSets.Count))
                return false;

            // Create new lists for stress shadow half-widths if they do not already exist
            if (StressShadowHalfWidthsIJ == null)
                StressShadowHalfWidthsIJ = new List<List<double>>();
            if (StressShadowHalfWidthsJI == null)
                StressShadowHalfWidthsJI = new List<List<double>>();

            // Loop through all the fracture sets
            for (int FSI_Index = 0; FSI_Index < NoLayerBoundFractureSets; FSI_Index++)
            {
                // Get a handle to fracture set I
                LayerBoundFractureSet FSI = LayerBoundFractureSets[FSI_Index];

                // Check if we already have a list of IJ stress shadow half-widths for this fracture set, and if not, create one
                while (StressShadowHalfWidthsIJ.Count <= FSI_Index)
                    StressShadowHalfWidthsIJ.Add(new List<double>());

                // Check if there is a value for each dipset in the list, otherwise create a new list
                int NoDipsetsI = FSI.FractureDipSets.Count;
                if (StressShadowHalfWidthsIJ[FSI_Index].Count < NoDipsetsI)
                {
                    // Clear any data currently in the list
                    StressShadowHalfWidthsIJ[FSI_Index].Clear();

                    // Cache the appropriate azimuthal and strike-slip shear stress shadow multipliers for sets I and J locally
                    double Faa_IJ = FaaIJ[FSI_Index, FSJ_Index];
                    double Fas_IJ = FasIJ[FSI_Index, FSJ_Index];

                    // Create a list of the stress shadow half-widths for each dip set in fracture set I, as seen by a fracture from set J
                    // NB we calculate the stress shadow half-widths (i.e. the width of the stress shadow on one side of the fracture only) so we can pass this directly to the FSI.checkInMFProximityZone function
                    for (int dipsetIndexI = 0; dipsetIndexI < NoDipsetsI; dipsetIndexI++)
                    {
                        // Get the azimuthal and strike-slip shear components of the mean stress shadow width for this set I dip set
                        double WaaI = FSI.FractureDipSets[dipsetIndexI].getMeanAzimuthalStressShadowWidth(-1);
                        double WasI = FSI.FractureDipSets[dipsetIndexI].getMeanShearStressShadowWidth(-1);
                        // Calculate the stress shadow width of this set I dip set, as seen by fracture set J, and add it to the half-width list
                        double WIJ = (WaaI * Faa_IJ) + (WasI * Fas_IJ);
                        StressShadowHalfWidthsIJ[FSI_Index].Add(WIJ / 2);
                    }
                }

                // Check if we already have a list of JI stress shadow half-widths for this fracture set, and if not, create one
                while (StressShadowHalfWidthsJI.Count <= FSI_Index)
                    StressShadowHalfWidthsJI.Add(new List<double>());

                // Check if there is a value for each dipset in the list, otherwise create a new list
                int NoDipsetsJ = FSJ.FractureDipSets.Count;
                if (StressShadowHalfWidthsJI[FSI_Index].Count < NoDipsetsJ)
                {
                    // Clear any data currently in the list
                    StressShadowHalfWidthsJI[FSI_Index].Clear();

                    // Cache the appropriate azimuthal and strike-slip shear stress shadow multipliers for sets I and J locally
                    double Faa_JI = FaaIJ[FSJ_Index, FSI_Index];
                    double Fas_JI = FasIJ[FSJ_Index, FSI_Index];

                    // Create a list of the stress shadow half-widths for each dip set in fracture set J, as seen by a fracture from set I
                    // NB we calculate the stress shadow half-widths (i.e. the width of the stress shadow on one side of the fracture only) so we can pass this directly to the FSI.checkInMFProximityZone function
                    for (int dipsetIndexJ = 0; dipsetIndexJ < NoDipsetsJ; dipsetIndexJ++)
                    {
                        // Get the azimuthal and strike-slip shear components of the mean stress shadow width for this set I dip set
                        double WaaJ = FSJ.FractureDipSets[dipsetIndexJ].getMeanAzimuthalStressShadowWidth(-1);
                        double WasJ = FSJ.FractureDipSets[dipsetIndexJ].getMeanShearStressShadowWidth(-1);
                        // Calculate the stress shadow width of this set I dip set, as seen by fracture set J, and add it to the half-width list
                        double WJI = (WaaJ * Faa_JI) + (WasJ * Fas_JI);
                        StressShadowHalfWidthsJI[FSI_Index].Add(WJI / 2);
                    }
                }

                // Orientation multiplier to project the width of a stress shadow around around a set J fracture onto the azimuth of a set I fracture
                double cosIJ = Math.Abs(VectorXYZ.Cos_trim(FSI.Strike - FSJ.Strike));

                // Create a new list of exclusion zone widths for each fracture set I, as seen by this fracture
                List<double> ExclusionZoneHalfWidthsIJ = new List<double>();
                for (int dipsetIndex = 0; dipsetIndex < NoDipsetsI; dipsetIndex++)
                    ExclusionZoneHalfWidthsIJ.Add(StressShadowHalfWidthsIJ[FSI_Index][dipsetIndex] + (cosIJ * StressShadowHalfWidthsJI[FSI_Index][FSJ_DipSet_Index]));

                // Check if the specified point lies within the stress shadow of any of the macrofracture segments in fracture set I
                if (FSI.checkInMFProximityZone(point, ExclusionZoneHalfWidthsIJ))
                    return true;
            }

            // If the specified point does not lie in the stress shadow of any segments, return false
            return false;
        }
        /// <summary>
        /// Check whether a specified point (in XYZ coordinates) lies within the stress shadows of an unconfined fracture from any fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="UFSJ_Index">Index number of the unconfined fracture set to which the point belongs</param>
        /// <param name="StressShadowWidthRatiosIJ">Reference to a list of stress shadow width to effective fracture radius ratios for each unconfined fracture set, as seen by this fracture - if this is null, a new list will be created</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        private bool checkInUCFStressShadow(PointXYZ point, int UFSJ_Index, ref List<double> StressShadowWidthRatiosIJ)
        {
            // Check the specified fracture set number lies within the range of fracture sets
            // Otherwise return false
            if ((UFSJ_Index < 0) || (UFSJ_Index >= NoUnconfinedFractureSets))
                return false;

            // Get a handle to the unconfined fracture set to which the specified point belongs (set J)
            UnconfinedFractureSet UFSJ = UnconfinedFractureSets[UFSJ_Index];

            // Create a new list for stress shadow half-widths if one does not already exist
            if (StressShadowWidthRatiosIJ == null)
                StressShadowWidthRatiosIJ = new List<double>();

            // Loop through all the unconfined fracture sets
            for (int UFSI_Index = 0; UFSI_Index < NoUnconfinedFractureSets; UFSI_Index++)
            {
                // Get a handle to unconfined fracture set I
                UnconfinedFractureSet UFSI = UnconfinedFractureSets[UFSI_Index];

                // Check if we already have a stress shadow width ratio for this fracture set, and if not, calculate it
                while (StressShadowWidthRatiosIJ.Count <= UFSI_Index)
                {
                    // For now we will assume no stress shadow interaction between different sets - WIJ = 0 for UFSI != UFSJ
                    double WIJ = (UFSI_Index == UFSJ_Index) ? UFSJ.getStressShadowWidthRatio(CurrentExplicitTimestep) : UFSJ.getStressShadowWidthRatio(UFSI, CurrentExplicitTimestep);

                    // Calculate the stress shadow width ratio of this fracture set, as seen by fracture set J, and add it to the list
                    StressShadowWidthRatiosIJ.Add(WIJ);
                }

                // Check if the specified point lies within the stress shadow of any of the macrofracture segments in fracture set I
                if (UFSI.checkInUCFStressShadow(point, StressShadowWidthRatiosIJ[UFSI_Index]))
                    return true;
            }

            // If the specified point does not lie in the exclusion zone around any fractures, return false
            return false;
        }
        /*// This is not valid as the axes of the spheroids around fractures from two different fracture sets will be different, and therefore the geometric calculation must take this into account
        // This is not done in the current UFS.checkInUCFExclusionZone() function which assumes the spheroids are coaxial (i.e. fractures are from the same set)
        /// <summary>
        /// Check whether a specified point (in XYZ coordinates) lies within an exclusion zone of arbitrary width around any of the stress shadows of the unconfined fractures from any fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="UFSJ_Index">Index number of the unconfined fracture set to which the point belongs</param>
        /// <param name="MaxOuterExclusionZoneZoneWidth">Width of the outer exclusion zone in the plane of the fracture (this will be the mean radius of the fracture we are checking against)</param>
        /// <param name="MinOuterExclusionZoneZoneWidth">Width of the outer exclusion zone perpendicular to the plane of the fracture (this will be the effective radius of the fracture we are checking against)</param>
        /// <param name="StressShadowWidthRatiosIJ">Reference to a list of stress shadow width to effective fracture radius ratios for each unconfined fracture set, as seen by this fracture - if this is null, a new list will be created</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        private bool checkInUCFExclusionZone(PointXYZ point, int UFSJ_Index, double MaxOuterExclusionZoneZoneWidth, double MinOuterExclusionZoneZoneWidth, ref List<double> StressShadowWidthRatiosIJ)
        {
            // Check the specified fracture set number lies within the range of fracture sets
            // Otherwise return false
            if ((UFSJ_Index < 0) || (UFSJ_Index >= NoUnconfinedFractureSets))
                return false;

            // Get a handle to the unconfined fracture set to which the specified point belongs (set J)
            UnconfinedFractureSet UFSJ = UnconfinedFractureSets[UFSJ_Index];

            // Create a new list for stress shadow half-widths if one does not already exist
            if (StressShadowWidthRatiosIJ == null)
                StressShadowWidthRatiosIJ = new List<double>();

            // Loop through all the unconfined fracture sets
            for (int UFSI_Index = 0; UFSI_Index < NoUnconfinedFractureSets; UFSI_Index++)
            {
                // Get a handle to unconfined fracture set I
                UnconfinedFractureSet UFSI = UnconfinedFractureSets[UFSI_Index];

                // Check if we already have a stress shadow width ratio for this fracture set, and if not, calculate it
                while (StressShadowWidthRatiosIJ.Count <= UFSI_Index)
                {
                    // For now we will assume no stress shadow interaction between different sets - WIJ = 0 for UFSI != UFSJ
                    double WIJ = (UFSI_Index == UFSJ_Index) ? UFSJ.getStressShadowWidthRatio(CurrentExplicitTimestep) : UFSJ.getStressShadowWidthRatio(UFSI, CurrentExplicitTimestep);

                    // Calculate the stress shadow width ratio of this fracture set, as seen by fracture set J, and add it to the list
                    StressShadowWidthRatiosIJ.Add(WIJ);
                }

                // Check if the specified point lies within the stress shadow of any of the macrofracture segments in fracture set I
                if (UFSI.checkInUCFExclusionZone(point, MaxOuterExclusionZoneZoneWidth, MinOuterExclusionZoneZoneWidth, StressShadowWidthRatiosIJ[UFSI_Index]))
                    return true;
            }

            // If the specified point does not lie in the exclusion zone around any fractures, return false
            return false;
        }*/

        // Reset and data input functions
        /// <summary>
        /// Set the parent FractureGrid object
        /// </summary>
        /// <param name="grid_in">Reference to parent FractureGrid object</param>
        public void setParentGrid(FractureGrid grid_in)
        {
            gd = grid_in;
        }
        /// <summary>
        /// Set the gridblock corners based on specified top points, and recalculate gridblock geometry; bottom points will be calculated automatically based on the layer thickness
        /// </summary>
        /// <param name="SWtop_corner_in">SW top corner in XYZ coordinates</param>
        /// <param name="NWtop_corner_in">NW top corner in XYZ coordinates</param>
        /// <param name="NEtop_corner_in">NE top corner in XYZ coordinates</param>
        /// <param name="SEtop_corner_in">SE top corner in XYZ coordinates</param>
        public void setGridblockCorners(PointXYZ SWtop_corner_in, PointXYZ NWtop_corner_in, PointXYZ NEtop_corner_in, PointXYZ SEtop_corner_in)
        {
            // Set the top corners to the input data; create new points for the bottom corners based on layer thickness
            SWtop = SWtop_corner_in;
            SWbottom = new PointXYZ(SWtop_corner_in.X, SWtop_corner_in.Y, SWtop_corner_in.Z - ThicknessAtDeformation);
            NWtop = NWtop_corner_in;
            NWbottom = new PointXYZ(NWtop_corner_in.X, NWtop_corner_in.Y, NWtop_corner_in.Z - ThicknessAtDeformation);
            NEtop = NEtop_corner_in;
            NEbottom = new PointXYZ(NEtop_corner_in.X, NEtop_corner_in.Y, NEtop_corner_in.Z - ThicknessAtDeformation);
            SEtop = SEtop_corner_in;
            SEbottom = new PointXYZ(SEtop_corner_in.X, SEtop_corner_in.Y, SEtop_corner_in.Z - ThicknessAtDeformation);

            // Recalculate gridblock geometric data
            recalculateGeometry();

            // Set the centrepoints of corner pillars in local (IJK) coordinates, for each fracture set
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                fs.setCornerPoints();
            }
        }
        /// <summary>
        /// Set the gridblock corners based on specified top and bottom points, and recalculate gridblock geometry
        /// </summary>
        /// <param name="SWtop_corner_in">SW top corner in XYZ coordinates</param>
        /// <param name="SWbottom_corner_in">SW bottom corner in XYZ coordinates</param>
        /// <param name="NWtop_corner_in">NW top corner in XYZ coordinates</param>
        /// <param name="NWbottom_corner_in">NW bottom corner in XYZ coordinates</param>
        /// <param name="NEtop_corner_in">NE top corner in XYZ coordinates</param>
        /// <param name="NEbottom_corner_in">NE bottom corner in XYZ coordinates</param>
        /// <param name="SEtop_corner_in">SE top corner in XYZ coordinates</param>
        /// <param name="SEbottom_corner_in">SE bottom corner in XYZ coordinates</param>
        public void setGridblockCorners(PointXYZ SWtop_corner_in, PointXYZ SWbottom_corner_in, PointXYZ NWtop_corner_in, PointXYZ NWbottom_corner_in, PointXYZ NEtop_corner_in, PointXYZ NEbottom_corner_in, PointXYZ SEtop_corner_in, PointXYZ SEbottom_corner_in)
        {
            // Set the corners to the input data
            SWtop = SWtop_corner_in;
            SWbottom = SWbottom_corner_in;
            NWtop = NWtop_corner_in;
            NWbottom = NWbottom_corner_in;
            NEtop = NEtop_corner_in;
            NEbottom = NEbottom_corner_in;
            SEtop = SEtop_corner_in;
            SEbottom = SEbottom_corner_in;

            // Recalculate gridblock geometric data
            recalculateGeometry();

            // Set the centrepoints of corner pillars in local (IJK) coordinates, for each fracture set
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                fs.setCornerPoints();
            }
        }
        /// <summary>
        /// Overwrite the cornerpoints on one of the gridblock boundaries with new cornerpoints, and recalculate gridblock geometry
        /// </summary>
        /// <param name="sideToOverwrite">Side of the gridblock to overwrite</param>
        /// <param name="leftTopCornerPoint">Reference to PointXYZ object for top left hand corner (looking out from the gridblock)</param>
        /// <param name="leftBottomCornerPoint">Reference to PointXYZ object for bottom left hand corner (looking out from the gridblock)</param>
        /// <param name="rightTopCornerPoint">Reference to PointXYZ object for top right hand corner (looking out from the gridblock)</param>
        /// <param name="rightBottomCornerPoint">Reference to PointXYZ object for bottom right hand corner (looking out from the gridblock)</param>
        public void OverwriteGridblockCorners(GridDirection sideToOverwrite, PointXYZ leftTopCornerPoint, PointXYZ leftBottomCornerPoint, PointXYZ rightTopCornerPoint, PointXYZ rightBottomCornerPoint)
        {
            // Overwite the existing cornerpoints with the supplied references
            switch (sideToOverwrite)
            {
                case GridDirection.N:
                    {
                        NWtop = leftTopCornerPoint;
                        NWbottom = leftBottomCornerPoint;
                        NEtop = rightTopCornerPoint;
                        NEbottom = rightBottomCornerPoint;
                    }
                    break;
                case GridDirection.E:
                    {
                        NEtop = leftTopCornerPoint;
                        NEbottom = leftBottomCornerPoint;
                        SEtop = rightTopCornerPoint;
                        SEbottom = rightBottomCornerPoint;
                    }
                    break;
                case GridDirection.S:
                    {
                        SEtop = leftTopCornerPoint;
                        SEbottom = leftBottomCornerPoint;
                        SWtop = rightTopCornerPoint;
                        SWbottom = rightBottomCornerPoint;
                    }
                    break;
                case GridDirection.W:
                    {
                        SWtop = leftTopCornerPoint;
                        SWbottom = leftBottomCornerPoint;
                        NWtop = rightTopCornerPoint;
                        NWbottom = rightBottomCornerPoint;
                    }
                    break;
                case GridDirection.U:
                    {
                        NEtop = leftTopCornerPoint;
                        SEtop = leftBottomCornerPoint;
                        NWtop = rightTopCornerPoint;
                        SWtop = rightBottomCornerPoint;
                    }
                    break;
                case GridDirection.D:
                    {
                        NWbottom = leftTopCornerPoint;
                        SWbottom = leftBottomCornerPoint;
                        NEbottom = rightTopCornerPoint;
                        SEbottom = rightBottomCornerPoint;
                    }
                    break;
                case GridDirection.None:
                    break;
                default:
                    break;
            }

            // Recalculate gridblock geometry and volume
            recalculateGeometry();

            // Set the centrepoints of corner pillars in local (IJK) coordinates, for each fracture set
            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                fs.setCornerPoints();
            }
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, and create new layer-bound and unconfined fracture sets
        /// </summary>
        /// <param name="NoLayerBoundFractureSets_in">Number of layer-bound fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="MF_B_in">Initial microfracture density coefficient B for layer-bound fracture sets (/m3)</param>
        /// <param name="MF_c_in">Initial microfracture distribution coefficient c for layer-bound fracture sets</param>
        /// <param name="SpecifyMode">Flag to specify fracture mode for layer-bound sets; if true, layer-bound fracture sets will only contain fracture dipsets of the specified mode, otherwise they will contain fracture dipsets for all modes</param>
        /// <param name="FractureMode_in">Fracture mode; Fracture sets will contain only 1 dip set of specified mode</param>
        /// <param name="BiazimuthalConjugate_in">Flag to specify biazimuthal conjugate layer-bound fracture dipsets: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions; only valid if SpecifyFractureMode flag is false</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <param name="NoUnconfinedFractureStrikeSets_in">Number of different strike orientations used to create new unconfined fracture sets</param>
        /// <param name="NoUnconfinedFractureDipSets_in">Number of different dip orientations used to create new unconfined fracture sets</param>
        /// <param name="NoRaysPerUnconfinedFracture_in">Number of rays comprising each unconfined fracture</param>
        /// <param name="MinUnconfinedFractureRadius_in">Minimum radius for unconfined fractures; this will be the length of the rays at nucleation</param>
        /// <param name="MaxUnconfinedFractureRadius_in">Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length</param>
        /// <param name="MaxUnconfinedEffectiveFractureRadius_in">Maximum allowed effective radius for unconfined fractures; when this is reached, rays will continue propagating but velocity and stress shadow width will be independent of fracture size</param>
        /// <param name="UCF_InitialMicrofractureDistribution_in">Initial microfracture distribution function for unconfined fracture sets</param>
        /// <param name="UCF_B_in">Initial microfracture density coefficient B for unconfined fracture sets (/m3)</param>
        /// <param name="UCF_c_in">Initial microfracture distribution coefficient c for unconfined fracture sets</param>
        /// <param name="UCF_M_in">Median initial microfracture radius for the log-normal distribution function for unconfined fracture sets</param>
        private void resetFractures(int NoLayerBoundFractureSets_in, double MF_B_in, double MF_c_in, bool SpecifyMode, FractureMode FractureMode_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in,
            int NoUnconfinedFractureStrikeSets_in, int NoUnconfinedFractureDipSets_in, int NoRaysPerUnconfinedFracture_in, double MinUnconfinedFractureRadius_in, double MaxUnconfinedFractureRadius_in, double MaxUnconfinedEffectiveFractureRadius_in, InitialFractureDistribution UCF_InitialMicrofractureDistribution_in, double UCF_B_in, double UCF_c_in, double UCF_M_in)
        {
            // Clear all existing data
            ClearFractureData();

            // Create layer-bound fracture sets
            if (NoLayerBoundFractureSets_in > 0)
            {
                if (SpecifyMode)
                    createSingleModeLayerBoundFractures(NoLayerBoundFractureSets_in, MF_B_in, MF_c_in, FractureMode_in, IncludeReverseFractures_in);
                else
                    createMultimodeLayerBoundFractures(NoLayerBoundFractureSets_in, MF_B_in, MF_c_in, BiazimuthalConjugate_in, IncludeReverseFractures_in);
            }

            // Create the unconfined fracture sets
            if (NoUnconfinedFractureStrikeSets_in > 0)
            {
                createUnconfinedFractures(NoUnconfinedFractureStrikeSets_in, NoUnconfinedFractureDipSets_in, NoRaysPerUnconfinedFracture_in, MinUnconfinedFractureRadius_in, MaxUnconfinedFractureRadius_in, MaxUnconfinedEffectiveFractureRadius_in, UCF_InitialMicrofractureDistribution_in, UCF_B_in, UCF_c_in, UCF_M_in);
            }

            // Repopulate the fracture set arrays
            ResetFractureSetArrays();
        }
        /// <summary>
        /// Create new layer-bound fracture sets each containing two dip sets (Mode 1 and Mode 2)
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        private void createMultimodeLayerBoundFractures(int NoFractureSets_in, double B_in, double c_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in)
        {
            // Create new fracture sets
            for (int fs_index = 0; fs_index < NoFractureSets_in; fs_index++)
            {
                double strike = Hmin_azimuth + (Math.PI / 2) + (Math.PI * ((double)fs_index / (double)NoFractureSets_in));
                LayerBoundFractureSet new_FractureSet = new LayerBoundFractureSet(this, strike, B_in, c_in, BiazimuthalConjugate_in, IncludeReverseFractures_in);
                LayerBoundFractureSets.Add(new_FractureSet);
            }
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, and create new layer-bound fracture sets each containing two dip sets (Mode 1 and Mode 2)
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public void resetLayerBoundFractures(int NoFractureSets_in, double B_in, double c_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in)
        {
            resetFractures(NoFractureSets_in, B_in, c_in, false, FractureMode.Mode1, BiazimuthalConjugate_in, IncludeReverseFractures_in, 0, 0, 0, 0, 0, 0, InitialFractureDistribution.PowerLaw, 0, 0, 0);
        }
        /// <summary>
        /// Create new layer-bound fracture sets each containing only one dip set of specified mode
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="FractureMode_in">Fracture mode; Fracture sets will contain only 1 dip set of specified mode</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        private void createSingleModeLayerBoundFractures(int NoFractureSets_in, double B_in, double c_in, FractureMode FractureMode_in, bool IncludeReverseFractures_in)
        {
            // Fracture dip is vertical for Mode 1 or Mode 3 fractures, inclined (dependent on friction coefficient) for Mode 2 fractures
            double opt_dip = (FractureMode_in == FractureMode.Mode2) ? ((Math.PI / 2) + Math.Atan(MechProps.MuFr)) / 2 : Math.PI / 2;

            // Create new fracture sets
            for (int fs_index = 0; fs_index < NoFractureSets_in; fs_index++)
            {
                double strike = Hmin_azimuth + (Math.PI / 2) + (Math.PI * ((double)fs_index / (double)NoFractureSets_in));
                LayerBoundFractureSet new_FractureSet = new LayerBoundFractureSet(this, strike, FractureMode_in, opt_dip, B_in, c_in, IncludeReverseFractures_in);
                LayerBoundFractureSets.Add(new_FractureSet);
            }
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, and create new layer-bound fracture sets each containing only one dip set of specified mode
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="FractureMode_in">Fracture mode; Fracture sets will contain only 1 dip set of specified mode</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public void resetLayerBoundFractures(int NoFractureSets_in, double B_in, double c_in, FractureMode FractureMode_in, bool IncludeReverseFractures_in)
        {
            resetFractures(NoFractureSets_in, B_in, c_in, true, FractureMode_in, true, IncludeReverseFractures_in, 0, 0, 0, 0, 0, 0, InitialFractureDistribution.PowerLaw, 0, 0, 0);
        }
        /// <summary>
        /// Create new unconfined fracture sets
        /// </summary>
        /// <param name="NoStrikeSets_in">Number of different strike orientations used to create new fracture sets</param>
        /// <param name="NoDipSets_in">Number of different dip orientations used to create new fracture sets</param>
        /// <param name="NoRaysPerFracture_in">Number of rays comprising each unconfined fracture</param>
        /// <param name="MinUnconfinedFractureRadius_in">Minimum radius for unconfined fractures; this will be the length of the rays at nucleation</param>
        /// <param name="MaxUnconfinedFractureRadius_in">Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length</param>
        /// <param name="MaxUnconfinedEffectiveFractureRadius_in">Maximum allowed effective radius for unconfined fractures; when this is reached, rays will continue propagating but velocity and stress shadow width will be independent of fracture size</param>
        /// <param name="InitialMicrofractureDistribution_in">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="uFrmedian_in">Median initial microfracture radius for the log-normal distribution function</param>
        private void createUnconfinedFractures(int NoStrikeSets_in, int NoDipSets_in, int NoRaysPerFracture_in, double MinUnconfinedFractureRadius_in, double MaxUnconfinedFractureRadius_in, double MaxUnconfinedEffectiveFractureRadius_in, InitialFractureDistribution InitialMicrofractureDistribution_in, double B_in, double c_in, double uFrmedian_in)
        {
            List<double> dips = new List<double>();

            // Add dips
            // If the specified number of dip sets is 0, add three sets, a vertical set and two conjugate inclined sets in the optimal dip slip shear orientation
            if (NoDipSets_in == 0)
            {
                double verticalDip = Math.PI / 2;
                double inclinedDip = ((Math.PI / 2) + Math.Atan(MechProps.MuFr)) / 2;
                dips.Add(verticalDip);
                dips.Add(inclinedDip);
                dips.Add(-inclinedDip);
            }
            // If the specified number of dip sets is -1, add a single vertical set
            else if (NoDipSets_in == -1)
            {
                double verticalDip = Math.PI / 2;
                dips.Add(verticalDip);
            }
            // If the specified number of dip sets is -2, add two conjugate inclined sets in the optimal dip slip shear orientation
            else if (NoDipSets_in < -1)
            {
                double inclinedDip = ((Math.PI / 2) + Math.Atan(MechProps.MuFr)) / 2;
                dips.Add(inclinedDip);
                dips.Add(-inclinedDip);
            }
            // Otherwise add the specified number of sets at equal increments of dip
            else
            {
                // Add a vertical dip
                dips.Add(Math.PI / 2);
                // Add inclined dips
                for (int dipSetNo = NoDipSets_in - 1; dipSetNo > 0; dipSetNo--)
                {
                    double dip = ((double)dipSetNo / (double)NoDipSets_in) * (Math.PI / 2);
                    dips.Add(dip);
                    dips.Add(-dip);
                }
            }

            // Create new fracture sets
            for (int ufs_index = 0; ufs_index < NoStrikeSets_in; ufs_index++)
            {
                double strike = Hmin_azimuth + (Math.PI / 2) + (Math.PI * ((double)ufs_index / (double)NoStrikeSets_in));

                foreach (double dip in dips)
                {
                    UnconfinedFractureSet new_FractureSet = new UnconfinedFractureSet(this, strike, dip, NoRaysPerFracture_in, MinUnconfinedFractureRadius_in, MaxUnconfinedFractureRadius_in, MaxUnconfinedEffectiveFractureRadius_in, InitialMicrofractureDistribution_in, B_in, c_in, uFrmedian_in);
                    UnconfinedFractureSets.Add(new_FractureSet);
                }
            }
            // Create a horizontal fracture set
            if (NoDipSets_in > 0)
            {
                UnconfinedFractureSet new_FractureSet = new UnconfinedFractureSet(this, 0, 0, NoRaysPerFracture_in, MinUnconfinedFractureRadius_in, MaxUnconfinedFractureRadius_in, MaxUnconfinedEffectiveFractureRadius_in, InitialMicrofractureDistribution_in, B_in, c_in, uFrmedian_in);
                UnconfinedFractureSets.Add(new_FractureSet);
            }
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, and create new unconfined fracture sets
        /// </summary>
        /// <param name="NoStrikeSets_in">Number of different strike orientations used to create new fracture sets</param>
        /// <param name="NoDipSets_in">Number of different dip orientations used to create new fracture sets</param>
        /// <param name="NoRaysPerFracture_in">Number of rays comprising each unconfined fracture</param>
        /// <param name="MinUnconfinedFractureRadius_in">Minimum radius for unconfined fractures; this will be the length of the rays at nucleation</param>
        /// <param name="MaxUnconfinedFractureRadius_in">Maximum allowed radius for unconfined fractures; rays will stop propagating when they reach this length</param>
        /// <param name="MaxUnconfinedEffectiveFractureRadius_in">Maximum allowed effective radius for unconfined fractures; when this is reached, rays will continue propagating but velocity and stress shadow width will be independent of fracture size</param>
        /// <param name="InitialMicrofractureDistribution_in">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="uFrmedian_in">Median initial microfracture radius - this is only used for the log-normal distribution function</param>
        public void resetUnconfinedFractures(int NoStrikeSets_in, int NoDipSets_in, int NoRaysPerFracture_in, double MinUnconfinedFractureRadius_in, double MaxUnconfinedFractureRadius_in, double MaxUnconfinedEffectiveFractureRadius_in, InitialFractureDistribution InitialMicrofractureDistribution_in, double B_in, double c_in, double uFrmedian_in)
        {
            resetFractures(0, 0, 0, false, FractureMode.Mode1, false, true, NoStrikeSets_in, NoDipSets_in, NoRaysPerFracture_in, MinUnconfinedFractureRadius_in, MaxUnconfinedFractureRadius_in, MaxUnconfinedEffectiveFractureRadius_in, InitialMicrofractureDistribution_in, B_in, c_in, uFrmedian_in);
        }
        /// <summary>
        /// Reset the arrays containing fracture set information, e.g. termination arrays, stress shadow multiplier arrays, etc
        /// </summary>
        private void ResetFractureSetArrays()
        {
            // Create the azimuthal and strike-slip shear stress shadow multiplier arrays
            FaaIJ = new double[NoLayerBoundFractureSets, NoLayerBoundFractureSets];
            FasIJ = new double[NoLayerBoundFractureSets, NoLayerBoundFractureSets];
            for (int I = 0; I < NoLayerBoundFractureSets; I++)
                for (int J = 0; J < NoLayerBoundFractureSets; J++)
                {
                    if (I == J)
                    {
                        FaaIJ[I, J] = 1;
                        FasIJ[I, J] = 1;
                    }
                    else
                    {
                        FaaIJ[I, J] = 0;
                        FasIJ[I, J] = 0;
                    }
                }

            // Create the unconfined fracture set stress shadow multiplier array
            UCFW_IJ = new double[NoUnconfinedFractureSets, NoUnconfinedFractureSets];
            for (int I = 0; I < NoUnconfinedFractureSets; I++)
                for (int J = 0; J < NoUnconfinedFractureSets; J++)
                {
                    if (I == J)
                    {
                        UCFW_IJ[I, J] = 1;
                    }
                    else
                    {
                        UCFW_IJ[I, J] = 0;
                    }
                }

            // Recreate the holders for the list of stress shadow half-widths of all fracture sets as seen by all other fracture sets, and vice versa
            StressShadowHalfWidthsIJ = new List<List<double>>[NoFractureSets];
            StressShadowHalfWidthsJI = new List<List<double>>[NoFractureSets];

            // Create the macrofracture termination array
            MFTerminations = new double[NoLayerBoundFractureSets, NoLayerBoundFractureSets][];
            for (int I = 0; I < NoLayerBoundFractureSets; I++)
                for (int J = 0; J < NoLayerBoundFractureSets; J++)
                {
                    int NoDipsetsJ = LayerBoundFractureSets[J].FractureDipSets.Count;
                    MFTerminations[I, J] = new double[NoDipsetsJ];
                    for (int fdsJ = 0; fdsJ < NoDipsetsJ; fdsJ++)
                        MFTerminations[I, J][fdsJ] = 0;
                }

            // Create the unconfined fracture termination array
            UCFTerminations = new double[NoUnconfinedFractureSets, NoUnconfinedFractureSets];
            for (int I = 0; I < NoUnconfinedFractureSets; I++)
                for (int J = 0; J < NoUnconfinedFractureSets; J++)
                {
                    UCFTerminations[I, J] = 0;
                }

        }
        /// <summary>
        /// Clear any existing fracture data in the gridblock
        /// </summary>
        private void ClearFractureData()
        {
            /// Reset the total cumulative strain tensors to zero, reset the elastic strain and stress tensors to initial compactional state, and reset the strain and stress rate tensors to zero
            StressStrain.ResetStressStrainState();

            // Clear the list of timestep end times and add a zero value for timestep zero
            TimestepEndTimes.Clear();
            TimestepEndTimes.Add(0d);

            // Set the pointers to the last calculated timesteps for implicit and explicit data to timestep 0
            CurrentImplicitTimestep = 0;
            CurrentExplicitTimestep = 0;

            // Clear the list for the references to all macrofracture segments
            MacrofractureSegments.Clear();

            // Clear all current fracture sets: no fractures, no deformation history
            LayerBoundFractureSets.Clear();
            UnconfinedFractureSets.Clear();

            // Repopulate the fracture set arrays - these will be empty as we have not yet created any fracture sets
            ResetFractureSetArrays();
        }
        /// <summary>
        /// Set the uniform aperture and size-dependent aperture multipliers for each fracture set
        /// </summary>
        /// <param name="Mode1HMin_UniformAperture">Specified aperture for Mode 1 fractures striking perpendicular to shmin</param>
        /// <param name="Mode2HMin_UniformAperture">Specified aperture for Mode 2 fractures striking perpendicular to shmin</param>
        /// <param name="Mode1HMax_UniformAperture">Specified aperture for Mode 1 fractures striking perpendicular to shmax</param>
        /// <param name="Mode2HMax_UniformAperture">Specified aperture for Mode 2 fractures striking perpendicular to shmax</param>
        /// <param name="Mode1HMin_SizeDependentApertureMultiplier">Specified size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to shmin</param>
        /// <param name="Mode2HMin_SizeDependentApertureMultiplier">Specified size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to shmin</param>
        /// <param name="Mode1HMax_SizeDependentApertureMultiplier">Specified size-dependent aperture multiplier for Mode 1 fractures striking perpendicular to shmax</param>
        /// <param name="Mode2HMax_SizeDependentApertureMultiplier">Specified size-dependent aperture multiplier for Mode 2 fractures striking perpendicular to shmax</param>
        public void SetFractureApertureControlData(double Mode1HMin_UniformAperture, double Mode2HMin_UniformAperture, double Mode1HMax_UniformAperture, double Mode2HMax_UniformAperture, double Mode1HMin_SizeDependentApertureMultiplier, double Mode2HMin_SizeDependentApertureMultiplier, double Mode1HMax_SizeDependentApertureMultiplier, double Mode2HMax_SizeDependentApertureMultiplier)
        {
            // The uniform aperture and size-dependent aperture multipliers will be determined by the orientation of the fracture set relative to the minimum horizontal strain azimuth at the present day or at the time of deformation
            double hMinAzi = UsePresentDayStress ? PresentDayStress.GetMinimumHorizontalAzimuth() : Hmin_azimuth;
            if (double.IsNaN(hMinAzi))
                hMinAzi = Hmin_azimuth;

            foreach (LayerBoundFractureSet fs in LayerBoundFractureSets)
            {
                double relativeAngle = hMinAzi - fs.Azimuth;
                double HMinComponent = Math.Pow(VectorXYZ.Cos_trim(relativeAngle), 2);
                double HMaxComponent = Math.Pow(VectorXYZ.Sin_trim(relativeAngle), 2);
                double Mode1_UniformAperture_in = (Mode1HMin_UniformAperture * HMinComponent) + (Mode1HMax_UniformAperture * HMaxComponent);
                double Mode2_UniformAperture_in = (Mode2HMin_UniformAperture * HMinComponent) + (Mode2HMax_UniformAperture * HMaxComponent);
                double Mode1_SizeDependentApertureMultiplier_in = (Mode1HMin_SizeDependentApertureMultiplier * HMinComponent) + (Mode1HMax_SizeDependentApertureMultiplier * HMaxComponent);
                double Mode2_SizeDependentApertureMultiplier_in = (Mode2HMin_SizeDependentApertureMultiplier * HMinComponent) + (Mode2HMax_SizeDependentApertureMultiplier * HMaxComponent);
                fs.SetFractureApertureControlData(Mode1_UniformAperture_in, Mode2_UniformAperture_in, Mode1_SizeDependentApertureMultiplier_in, Mode2_SizeDependentApertureMultiplier_in);
            }

            foreach (UnconfinedFractureSet ufs in UnconfinedFractureSets)
            {
                double relativeAngle = hMinAzi - ufs.Azimuth;
                double HMinComponent = Math.Pow(VectorXYZ.Cos_trim(relativeAngle), 2);
                double HMaxComponent = Math.Pow(VectorXYZ.Sin_trim(relativeAngle), 2);
                // For now we will assume fractures with dip >80deg are Mode 1
                bool Mode1 = ((ufs.Dip * (180 / Math.PI)) > 80);
                double UFS_UniformAperture_in = Mode1 ? (Mode1HMin_UniformAperture * HMinComponent) + (Mode1HMax_UniformAperture * HMaxComponent) : (Mode2HMin_UniformAperture * HMinComponent) + (Mode2HMax_UniformAperture * HMaxComponent);
                double UFS_SizeDependentApertureMultiplier_in = Mode1 ? (Mode1HMin_SizeDependentApertureMultiplier * HMinComponent) + (Mode1HMax_SizeDependentApertureMultiplier * HMaxComponent) : (Mode2HMin_SizeDependentApertureMultiplier * HMinComponent) + (Mode2HMax_SizeDependentApertureMultiplier * HMaxComponent);
                ufs.SetFractureApertureControlData(UFS_UniformAperture_in, UFS_SizeDependentApertureMultiplier_in);
            }
        }

        // Constructors
        /// <summary>
        /// Default Constructor: set layer thickness to 1m and depth to 1km
        /// </summary>
        public GridblockConfiguration() : this(1, 1000)
        {
            // Defaults:

            // Set layer thickness and depth at the start of deformation
            // Layer thickness: default 1m
            // Depth: default 1000m
        }
        /// <summary>
        /// Constructor: specify layer thickness and depth at the start of deformation, but create two empty fracture sets
        /// </summary>
        /// <param name="InitialThickness_in">Layer thickness at time of deformation (m)</param>
        /// <param name="InitialDepth_in">Depth at time of deformation (m)</param>
        public GridblockConfiguration(double InitialThickness_in, double InitialDepth_in)
        {
            // Set layer thickness and depth at the start of deformation
            SetInitialThicknessAndDepth(InitialThickness_in, InitialDepth_in);

            // Create a dictionary of adjacent gridblocks and fill with null references (except for reference to this gridblock)
            NeighbourGridblocks = new Dictionary<GridDirection, GridblockConfiguration>();
            NeighbourGridblocks.Add(GridDirection.N, null);
            NeighbourGridblocks.Add(GridDirection.E, null);
            NeighbourGridblocks.Add(GridDirection.S, null);
            NeighbourGridblocks.Add(GridDirection.W, null);
            NeighbourGridblocks.Add(GridDirection.D, null);
            NeighbourGridblocks.Add(GridDirection.U, null);
            NeighbourGridblocks.Add(GridDirection.None, this);

            // Create a dictionary of gridblock faces; this will be populated by the recalculateGeometry() function when the gridblock cornerpoints are defined
            GridblockFaces = new Dictionary<GridDirection, PlaneXYZ>();

            // Set the flag to search neighbouring gridblocks for stress shadow interaction to false
            searchNeighbouringGridblocks_Automatic = false;

            // Create default objects for mechanical properties, stress and strain state and propagation control
            MechProps = new MechanicalProperties(this);
            StressStrain = new StressStrainState(this);
            PropControl = new PropagationControl();

            // Initially the present day stress tensor will be null (i.e. the stress state defined by the StressStrain object will be used to calculate fracture aperture and permeability)
            // The present day stress tensor can be defined later by calling the SetPresentDayStress() function
            // If the present day stress tensor is defined, it will override the StressStrain object when calculating fracture aperture and permeability
            PresentDayStress = null;

            // Create empty fracture set lists
            // The fracture sets themselves are created by calling the resetFractures or resetUnconfinedFractures functions
            LayerBoundFractureSets = new List<LayerBoundFractureSet>();
            UnconfinedFractureSets = new List<UnconfinedFractureSet>();

            // Repopulate the fracture set arrays - these will be empty as we have not yet created any fracture sets
            ResetFractureSetArrays();

            // Create empty lists for the references to all macrofracture segments and unconfined fracture ray segments
            MacrofractureSegments = new List<MacrofractureSegmentHolder>();
            UnconfinedFractureRaySegments = new List<UnconfinedFractureRaySegmentHolder>();

            // Create a list of timestep end times and add a zero value to it
            TimestepEndTimes = new List<double>();
            TimestepEndTimes.Add(0d);

            // Set the pointers to the last calculated timesteps for implicit and explicit data to timestep 0
            CurrentImplicitTimestep = 0;
            CurrentExplicitTimestep = 0;
        }
    }
}
