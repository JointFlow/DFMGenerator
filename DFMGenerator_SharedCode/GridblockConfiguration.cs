// Set this flag to output detailed information on the behaviour of explicit fractures in the DFN
// Use for debugging only; will significantly increase runtime
//#define LOGDFNPOP

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
    /// Enumerator for global grid directions (N = Y+, E = X+, S = Y-, W = X-)
    /// </summary>
    public enum GridDirection { N, E, S, W, None }

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

                // Calculate internal angles of the SW and NE corners, projected onto horizontal plane
                // Use cosine law and dot product of the two adjacent sides
                double SWcorner_dotProduct = (X2 * X3) + (Y2 * Y3);
                double SWcorner_cosine = SWcorner_dotProduct / (Length_SSide * Length_WSide);
                Angle_SWcorner = Math.Acos(SWcorner_cosine);
                double NEcorner_dotProduct = (X6 * X5) + (Y6 * Y5);
                double NEcorner_cosine = NEcorner_dotProduct / (Length_ESide * Length_NSide);
                Angle_NEcorner = Math.Acos(NEcorner_cosine);

                // Calculate area
                double AreaSWTriangle = (Length_SSide * Length_WSide * Math.Sin(Angle_SWcorner)) / 2;
                double AreaNETriangle = (Length_ESide * Length_NSide * Math.Sin(Angle_NEcorner)) / 2;
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
            }
        }

        // Public geometric properties and functions - for internal or external use
        // NB these functions all assume vertical pillars (i.e. cell cornerpoints are vertically aligned)
        /// <summary>
        /// Area of the middle surface of the gridblock, projected onto the horizontal; recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Area { get; private set; }
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
        /// Internal angle of the SW corner of the middle surface of the gridblock, projected onto the horizontal (radians); recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Angle_SWcorner { get; private set; }
        /// <summary>
        /// Internal angle of the NE corner of the middle surface of the gridblock, projected onto the horizontal (radians); recalculated whenever gridblock cornerpoints are changed
        /// </summary>
        public double Angle_NEcorner { get; private set; }
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
        /// <param name="useQuickMethod">If true, use quick calculation (valid for any shape cell but there will be a bias in the point location if the gridblock is not a parallelipiped), otherwise use long calculation (slower but gives a perfectly random position regardless of gridblock shape)</param>
        /// <returns>PointXYZ object representing a randomly located point in grid (XYZ) coordinates</returns>
        public PointXYZ getRandomPoint(bool useQuickMethod)
        {
            PointXYZ output = null;

            // Check to see if all cornerpoints are defined
            if (checkCornerpointsDefined())
            {
                // Get reference to the random number generator
                Random randGen = gd.RandomNumberGenerator;

                if (useQuickMethod)
                {
                    // Quick calculation: valid for any shape cell but there will be a bias in the point location if the gridblock is not a parallelipiped

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
        /// Check if a specified point lies within the gridblock
        /// </summary>
        /// <param name="point_in">Point in XYZ coordinates</param>
        /// <returns>true if point_in lies within the gridblock, false if it does not</returns>
        public bool checkPointInGridblock(PointXYZ point_in)
        {
            // Get the location of the point relative to the horizontal projection of the gridblock boundaries (uvw coordinates)
            double u, v, w;
            // If the u, v and w coordinates can be calculated then we must check if they are within the range 0 to 1
            if (getPositionRelativeToGridblock(out u, out v, out w, point_in))
            {
                // Check if point is out of bounds, if so return false
                if (u < 0) return false;
                if (u > 1) return false;
                if (v < 0) return false;
                if (v > 1) return false;
                if (w < 0) return false;
                if (w > 1) return false;

                // Point is in bounds, return true
                return true;
            }
            else
            {
                // If the u, v and w coordinates cannot be calculated then the point must lie outside the gridblock (or the cornerpoints are undefined)
                return false;
            }
        }
        /// <summary>
        /// Get a reference to the fracture set in this gridblock that best matches the orientation and strike of another fracture set (typically in another gridblock)
        /// </summary>
        /// <param name="inputFS_index">Orientation of the input fracture set</param>
        /// <param name="inputFS_strike">Strike of the input fracture set</param>
        /// <returns></returns>
        public Gridblock_FractureSet getClosestFractureSet(int inputFS_index, double inputFS_strike)
        {
            // First we will try the equivalent set to that of the input fracture set
            Gridblock_FractureSet thisGB_fs = FractureSets[inputFS_index];

            // Check if the strike of this equivalent set lies within the allowed range
            double maxStrikeDifference = gd.DFNControl.MaxConsistencyAngle;
            double actualStrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, thisGB_fs.Strike);
            if (actualStrikeDifference > maxStrikeDifference)
            {
                // If the strike of the equivalent set lies outside the allowed range, loop through all fracture sets to find the best fit
                // NB this may still be the equivalent set
                foreach (Gridblock_FractureSet test_fs in FractureSets)
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
            Gridblock_FractureSet thisGB_fs = FractureSets[inputFS_index];

            // Check if the strike of this equivalent set lies within the allowed range
            double maxStrikeDifference = gd.DFNControl.MaxConsistencyAngle;
            double actualStrikeDifference = PointXYZ.getStrikeDifference(inputFS_strike, thisGB_fs.Strike);
            if (actualStrikeDifference > maxStrikeDifference)
            {
                // If the strike of the equivalent set lies outside the allowed range, loop through all fracture sets to find the best fit
                // NB this may still be the equivalent set
                for (int FS_index = 0; FS_index < NoFractureSets; FS_index++)
                {
                    Gridblock_FractureSet test_fs = FractureSets[FS_index];

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

        // List containing fracture sets
        /// <summary>
        /// Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax
        /// </summary>
        public int NoFractureSets { get; private set; }
        /// <summary>
        /// Index number of the fracture set perpendicular to HMin
        /// </summary>
        public int HMin_FractureSet_Index { get { return 0; } }
        /// <summary>
        /// Index number of the fracture set perpendicular to HMax (or the closest set, if the total number of fracture sets is odd)
        /// </summary>
        public int HMax_FractureSet_Index { get { return NoFractureSets / 2; } }
        /// <summary>
        /// Get a name for a specified fracture set in this gridblock, indicating its orientation
        /// </summary>
        /// <param name="indexNo">Index number of the fracture set</param>
        /// <returns>String representing the fracture set name</returns>
        public string getFractureSetName(int indexNo)
        {
            return getFractureSetName(indexNo, NoFractureSets);
        }
        /// <summary>
        /// Get a name for a specified fracture set in a generic gridblock, indicating its orientation
        /// </summary>
        /// <param name="indexNo">Index number of the fracture set</param>
        /// <param name="noFractureSets">Total number of fracture sets in the gridblock</param>
        /// <returns>String representing the fracture set name</returns>
        public static string getFractureSetName(int indexNo, int noFractureSets)
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
        /// Array of azimuthal stress shadow multipliers relating stress shadows for different fracture sets
        /// </summary>
        private double[,] FaaIJ;
        /// <summary>
        /// Array of strike-slip shear stress shadow multipliers relating stress shadows for different fracture sets
        /// </summary>
        private double[,] FasIJ;
        /// <summary>
        /// Array containing the number of static half-macrofractures (MFP30) from each dipset terminating against macrofractures from every other fracture set
        /// Indices are: [set of propagating fracture, set of terminating fracture][dipset of terminating fracture]
        /// </summary>
        private double[,][] MFTerminations;
        /// <summary>
        /// Update the MFTerminations array with the most recent dsIJ_MFP30 for each fracture set
        /// </summary>
        private void upDateMFTerminations()
        {
            // Loop through every set of propagating fractures I
            for (int fsI_Index = 0; fsI_Index < NoFractureSets; fsI_Index++)
            {
                Gridblock_FractureSet fsI = FractureSets[fsI_Index];

                // Get the increment in sIJMFP30 for set I
                double dsIJ_MFP30 = 0;
                foreach (FractureDipSet dipSetIm in fsI.FractureDipSets)
                    dsIJ_MFP30 += dipSetIm.dsIJ_MFP30;

                // Get the total apparent MFP32 for all terminating fracture sets J
                // This includes all fracture sets except set I
                double totalApparentMFP32J = 0;
                double[][] apparentMFP32J = new double[NoFractureSets][];
                for (int fsJ_Index = 0; fsJ_Index < NoFractureSets; fsJ_Index++)
                {
                    if (fsI_Index == fsJ_Index)
                        continue;
                    Gridblock_FractureSet fsJ = FractureSets[fsJ_Index];

                    // Orientation multiplier to project the length of the terminating set J fracture perpendicular to the propagating set I fracture
                    double sinIJ = Math.Abs(Math.Sin(fsI.Strike - fsJ.Strike));

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
                for (int fsJ_Index = 0; fsJ_Index < NoFractureSets; fsJ_Index++)
                {
                    if (fsI_Index == fsJ_Index)
                        continue;

                    // Loop through each dipset Jm in J
                    int noDipSetsJ = FractureSets[fsJ_Index].FractureDipSets.Count;
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
        }
        /// <summary>
        /// Calculate the inverse stress shadow and clear zone volume for each fracture set J due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// </summary>
        /// <param name="isotropicFractureNetwork">Flag to use algorithm for isotropic or anisotropic fracture networks; set to true if the fracture network is (near) isotropic, otherwise set to false</param>
        private void setCrossFSStressShadows()
        {
            bool isotropicFractureNetwork = (P32AnisotropyIndex(true) <= PropControl.anisotropyCutoff);

            if (isotropicFractureNetwork)
                setCrossFSStressShadows_isotropic();
            else
                setCrossFSStressShadows_anisotropic();
        }
        /// <summary>
        /// Calculate the inverse stress shadow and clear zone volume for each fracture set J due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// Valid for isotropic fracture networks as it takes account of multiple fractures overlapping but does not account for the influence of a primary fracture set on the distribution of secondary sets
        /// </summary>
        private void setCrossFSStressShadows_isotropic()
        {
            // Fracture set K represents the fractures to which the stress shadows will apply
            // The stress shadow and exclusion zone widths will therefore be as seen by fracture set K
            for (int fsK_Index = 0; fsK_Index < NoFractureSets; fsK_Index++)
            {
                // Get a handle to fracture set K, and get the number of dipsets in set K
                Gridblock_FractureSet fsK = FractureSets[fsK_Index];
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
                for (int fsI_Index = 0; fsI_Index < NoFractureSets; fsI_Index++)
                {
                    // Get a handle to fracture set I
                    Gridblock_FractureSet fsI = FractureSets[fsI_Index];
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
                    double cosIK = Math.Abs(Math.Cos(fsI.Strike - fsK.Strike));

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
        /// Calculate the inverse stress shadow and clear zone volume for each fracture set J due to the stress shadows from other fracture sets I, and apply these to the FractureDipSet objects
        /// Valid for anisotropic fracture networks as it uses the number of fracture tips to calculate overlaps between fracture sets
        /// This takes account of the influence of a primary fracture set on the distribution of secondary sets, but ignores overlaps of multiple fractures
        /// </summary>
        private void setCrossFSStressShadows_anisotropic()
        {
            // Create a matrix of proportional stress shadow and exclusion zone overlaps between all fracture sets
            double[,][] tipOverlaps = new double[NoFractureSets, NoFractureSets][];
            // Fracture set I represents the propagating fracture
            for (int fsI_Index = 0; fsI_Index < NoFractureSets; fsI_Index++)
            {
                Gridblock_FractureSet fsI = FractureSets[fsI_Index];

                // Get the total area of static set I fractures
                // NB we will ignore active macrofractures as these may overlap the stress shadow or exclusion zones of other fracture sets without terminating against them
                double IMFP32 = 0;
                foreach (FractureDipSet dipSetIm in fsI.FractureDipSets)
                    IMFP32 += dipSetIm.s_MFP32_total();

                // Fracture dipset Jm represents the terminating fracture
                for (int fsJ_Index = 0; fsJ_Index < NoFractureSets; fsJ_Index++)
                {
                    Gridblock_FractureSet fsJ = FractureSets[fsJ_Index];
                    int noDipSetsJ = fsJ.FractureDipSets.Count;
                    tipOverlaps[fsI_Index, fsJ_Index] = new double[noDipSetsJ];

                    // Orientation multiplier to project the width of a stress shadow around around a set J fracture onto the strike of a set I fracture
                    double sinIJ = Math.Abs(Math.Sin(fsI.Strike - fsJ.Strike));
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
            for (int fsK_Index = 0; fsK_Index < NoFractureSets; fsK_Index++)
            {
                // Get a handle to fracture set K, and get the number of dipsets in set K
                Gridblock_FractureSet fsK = FractureSets[fsK_Index];
                int noDipSetsK = fsK.FractureDipSets.Count;

                // First we will calculate:
                // - the stress shadow width of every dipset Im as seen by set K,
                // - the total stress shadow volume of every set I as seen by set K (not including overlaps),
                // - the maximum exclusion zone width of every dipset Im as seen by dipset Kn, and
                // - the total exclusion zone volume of every set I as seen by dipset Kn (not including overlaps)
                double[][] stressShadowWidthImK = new double[NoFractureSets][];
                double[] stressShadowVolumeIK = new double[NoFractureSets];
                double[][][] exclusionZoneWidthImKn = new double[NoFractureSets][][];
                double[][] exclusionZoneVolumeIKn = new double[NoFractureSets][];

                // Fracture set I represents the fractures which the stress shadows and exclusion zones surround
                for (int fsI_Index = 0; fsI_Index < NoFractureSets; fsI_Index++)
                {
                    // Get a handle to fracture set I
                    Gridblock_FractureSet fsI = FractureSets[fsI_Index];
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
                    double cosIK = Math.Abs(Math.Cos(fsI.Strike - fsK.Strike));

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
                for (int fsI_Index = 0; fsI_Index < NoFractureSets; fsI_Index++)
                {
                    // Calculate the proportional overlap of stress shadows around fracture set I with every other dipset Jm
                    double stressShadowIOverlap = 0;
                    // Fracture dipset Jm represents the terminating fracture
                    for (int fsJ_Index = 0; fsJ_Index < NoFractureSets; fsJ_Index++)
                    {
                        if (fsJ_Index == fsI_Index)
                            continue;
                        int noDipSetsJ = FractureSets[fsJ_Index].FractureDipSets.Count;
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
                    for (int fsI_Index = 0; fsI_Index < NoFractureSets; fsI_Index++)
                    {
                        // Calculate the proportional overlap of exclusion zones around fracture set I with every other dipset Jm
                        double exclusionZoneIOverlap = 0;
                        // Fracture dipset Jm represents the terminating fracture
                        for (int fsJ_Index = 0; fsJ_Index < NoFractureSets; fsJ_Index++)
                        {
                            if (fsJ_Index == fsI_Index)
                                continue;
                            int noDipSetsJ = FractureSets[fsJ_Index].FractureDipSets.Count;
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
        /// <summary>
        /// Get the mean width of the exclusion zone around fractures from set I, as seen by a propagating fracture from dipset J, during a specified timestep
        /// </summary>
        /// <param name="fsI">Fracture set of the intersecting fractures</param>
        /// <param name="fsJ">Fracture set containing the propagating fracture</param>
        /// <param name="dipSetJ">Fracture dipset containing the propagating fracture</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating; set to -1 to use current data</param>
        /// <returns></returns>
        public double getCrossFSExclusionZoneWidth(Gridblock_FractureSet fsI, Gridblock_FractureSet fsJ, FractureDipSet dipSetJ, int Timestep_M)
        {
            // Get the index number of timestep M-1, so we can retrieve MFP32 values for the end of the previous timestep
            // If a valid timestep number is not supplied, use the current data
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the index numbers of the two sets; if we cannot find them, return NaN
            int fsI_Index = -1;
            int fsJ_Index = -1;
            for (int fs_Index = 0; fs_Index < NoFractureSets; fs_Index++)
            {
                if (fsI == FractureSets[fs_Index])
                    fsI_Index = fs_Index;
                if (fsJ == FractureSets[fs_Index])
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
            double cosIJ = Math.Abs(Math.Cos(fsI.Strike - fsJ.Strike));

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
        /// Collection of fracture sets referenced by Orientation
        /// </summary>
        public List<Gridblock_FractureSet> FractureSets;
        /// <summary>
        /// List of references to all macrofracture segments in all fracture sets in the gridblock in order of nucleation - used to ensure fracture propagation is carried out strictly in order of nucleation, with no 
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
            /// Orientation of fracture set holding the MacrofractureSegmentIJK object
            /// </summary>
            public int FractureSetIndex;

            // Control and implementation functions
            /// <summary>
            /// Compare MacrofractureSegmentHolder objects based on time
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
        /// <returns>Deactivation time of final fracture set; will return zero if none of the fracture sets were ever active</returns>
        public double getFinalActiveTime()
        {
            // Get time units and unit conversion modifier for output time data if not in SI units
            double timeUnits_Modifier = PropControl.getTimeUnitsModifier();

            // Loop through the timesteps in reverse order
            for (int TimestepNo = FinalTimestep; TimestepNo > 0; TimestepNo--)
            {
                // Loop through each fracture dipset
                foreach (Gridblock_FractureSet fs in FractureSets)
                {
                    foreach (FractureDipSet fds in fs.FractureDipSets)
                    {
                        // Check if the dipset is active (or residual active); if so return the end time of the current timestep
                        FractureEvolutionStage CurrentStage = fds.getEvolutionStage(TimestepNo);
                        if ((CurrentStage == FractureEvolutionStage.Growing) || (CurrentStage == FractureEvolutionStage.ResidualActivity))
                            return TimestepEndTimes[TimestepNo] / timeUnits_Modifier;
                    }
                }
            }

            // If none of the fracture sets were ever active, return 0
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

        // Functions to return fracture anisotropy and connectivity indices
        /// <summary>
        /// Fracture anisotropy index based on P32: (MaxP32 - MinP32) / (MaxP32 + MinP32)
        /// </summary>
        /// <param name="FindMinMaxSets">If true, will find the two sets with the largest and smallest P32 values; if false, will use the sets perpendicular to HMin and HMax respectively</param>
        /// <returns>(MaxP32 - MinP32) / (MaxP32 + MinP32)</returns>
        public double P32AnisotropyIndex(bool FindMinMaxSets)
        {
            // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
            if (NoFractureSets < 2)
                return 1;

            // Otherwise we will compare the sets with the highest and lowest P32 values
            int hmin_index = 0;
            double Max_P32 = FractureSets[hmin_index].combined_T_MFP32_total() + FractureSets[hmin_index].combined_T_uFP32_total();
            double Min_P32 = Max_P32;
            if (FindMinMaxSets)
                for (int fs_Index = 1; fs_Index < NoFractureSets; fs_Index++)
                {
                    double fs_P32 = FractureSets[fs_Index].combined_T_MFP32_total() + FractureSets[fs_Index].combined_T_uFP32_total();
                    if (fs_P32 > Max_P32)
                        Max_P32 = fs_P32;
                    if (fs_P32 < Min_P32)
                        Min_P32 = fs_P32;
                }
            else
            {
                int hmax_index = NoFractureSets / 2;
                Min_P32 = FractureSets[hmax_index].combined_T_MFP32_total() + FractureSets[hmax_index].combined_T_uFP32_total();
            }

            double Combined_P32 = Max_P32 + Min_P32;
            return (Combined_P32 > 0 ? (Max_P32 - Min_P32) / Combined_P32 : 0);
        }
        /// <summary>
        /// Fracture anisotropy index based on P33: (MaxP33 - MinP33) / (MaxP33 + MinP33)
        /// </summary>
        /// <param name="FindMinMaxSets">If true, will find the two sets with the largest and smallest P32 values; if false, will use the sets perpendicular to HMin and HMax respectively</param>
        /// <returns>(MaxP33 - MinP33) / (MaxP33 + MinP33)</returns>
        public double P33AnisotropyIndex(bool FindMinMaxSets)
        {
            // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
            if (NoFractureSets < 2)
                return 1;

            // Otherwise we will compare the sets with the highest and lowest P32 values
            int hmin_index = 0;
            double Max_P33 = FractureSets[hmin_index].combined_T_MFP33_total() + FractureSets[hmin_index].combined_T_uFP33_total();
            double Min_P33 = Max_P33;
            if (FindMinMaxSets)
                for (int fs_Index = 1; fs_Index < NoFractureSets; fs_Index++)
                {
                    double fs_P32 = FractureSets[fs_Index].combined_T_MFP33_total() + FractureSets[fs_Index].combined_T_uFP33_total();
                    if (fs_P32 > Max_P33)
                        Max_P33 = fs_P32;
                    if (fs_P32 < Min_P33)
                        Min_P33 = fs_P32;
                }
            else
            {
                int hmax_index = NoFractureSets / 2;
                Min_P33 = FractureSets[hmax_index].combined_T_MFP33_total() + FractureSets[hmax_index].combined_T_uFP33_total();
            }

            double Combined_P32 = Max_P33 + Min_P33;
            return (Combined_P32 > 0 ? (Max_P33 - Min_P33) / Combined_P32 : 0);
        }
        /// <summary>
        /// Fracture anisotropy index based on fracture porosity: (HMinPorosity - HMaxPorosity) / (HMinPorosity + HMaxPorosity)
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns>(HMinPorosity - HMaxPorosity) / (HMinPorosity + HMaxPorosity)</returns>
        public double FracturePorosityAnisotropyIndex(FractureApertureType ApertureControl)
        {
            // If there is only one fracture set, the anisotropy index will be 1 (completely anisotropic)
            if (NoFractureSets < 2)
                return 1;

            int hmin_index = 0;
            int hmax_index = NoFractureSets / 2;
            double HMin_Porosity = FractureSets[hmin_index].combined_MF_Porosity(ApertureControl) + FractureSets[hmin_index].combined_uF_Porosity(ApertureControl);
            double HMax_Porosity = FractureSets[hmax_index].combined_MF_Porosity(ApertureControl) + FractureSets[hmax_index].combined_uF_Porosity(ApertureControl);
            double Combined_Porosity = HMin_Porosity + HMax_Porosity;
            return (Combined_Porosity > 0 ? (HMin_Porosity - HMax_Porosity) / Combined_Porosity : 0);
        }
        /// <summary>
        /// Proportion of unconnected macrofracture tips - i.e. active macrofracture tips
        /// </summary>
        /// <returns>Ratio of a_MFP30_total to T_MFP30_total</returns>
        public double UnconnectedTipRatio()
        {
            double TotalUnconnectedTips = 0;
            double TotalAllTips = 0;
            foreach (Gridblock_FractureSet fs in FractureSets)
            {
                TotalUnconnectedTips += fs.combined_a_MFP30_total();
                TotalAllTips += fs.combined_T_MFP30_total();
            }

            return (TotalAllTips > 0 ? TotalUnconnectedTips / TotalAllTips : 1);
        }
        /// <summary>
        /// Proportion of macrofracture tips connected to relay zones - i.e. static macrofracture tips deactivated due to stress shadow interaction
        /// </summary>
        /// <returns>Ratio of sII_MFP30_total to T_MFP30_total</returns>
        public double RelayTipRatio()
        {
            double TotalRelayTips = 0;
            double TotalAllTips = 0;
            foreach (Gridblock_FractureSet fs in FractureSets)
            {
                TotalRelayTips += fs.combined_sII_MFP30_total();
                TotalAllTips += fs.combined_T_MFP30_total();
            }

            return (TotalAllTips > 0 ? TotalRelayTips / TotalAllTips : 0);
        }
        /// <summary>
        /// Proportion of connected macrofracture tips - i.e. static macrofracture tips deactivated due to intersection with orthogonal or oblique fractures
        /// </summary>
        /// <returns>Ratio of sIJ_MFP30_total to T_MFP30_total</returns>
        public double ConnectedTipRatio()
        {
            double TotalConnectedTips = 0;
            double TotalAllTips = 0;
            foreach (Gridblock_FractureSet fs in FractureSets)
            {
                TotalConnectedTips += fs.combined_sIJ_MFP30_total();
                TotalAllTips += fs.combined_T_MFP30_total();
            }

            return (TotalAllTips > 0 ? TotalConnectedTips / TotalAllTips : 0);
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
                foreach (Gridblock_FractureSet fs in FractureSets)
                    s_F += fs.S_set;
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
        /// <returns>True if the calculation runs to completion before hitting the timestep limit; false if the timestep limit is hit</returns>
        public bool CalculateFractureData()
        {
            // Declare local variables
            bool CalculationCompleted = false;
            bool HitTimestepLimit = false;
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
            // Set the target maximum increase in MFP33 allowed per timestep
            double d_MFP33 = PropControl.max_TS_MFP33_increase;
            // Set the ratio of current to maximum active macrofracture volumetric ratio at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio
            double historic_a_MFP33_termination_ratio = PropControl.historic_a_MFP33_termination_ratio;
            // Set the ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio 
            double active_total_MFP30_termination_ratio = PropControl.active_total_MFP30_termination_ratio;
            // Set the minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated
            double minimum_ClearZone_Volume = PropControl.minimum_ClearZone_Volume;
            // Set maximum number of timesteps allowed
            int maxTimesteps = PropControl.maxTimesteps;
            // Set maximum duration for individual timesteps
            double maxTimestepDuration = PropControl.maxTimestepDuration;
            bool useMaxTSDurationCutoff = (maxTimestepDuration > 0);
            // Flag for whether all fracture sets have been deactivated
            bool AllSetsDeactivated = false;

            // Set the number of bins to split the microfracture radii into when calculating uFP33 numerically
            int no_r_bins = PropControl.no_r_bins;
            // Maximum radius of microfractures in the smallest bin - used in determining fracture set deactivation
            double minrb_maxRad = (1 / (double)no_r_bins) * MaximumMicrofractureRadius;
            // Flag to check microfractures against stress shadows of all macrofractures, regardless of set; if false will only check microfractures against stress shadows of macrofractures in the same set
            // NB we do not need to do this in the evenly distributed stress scenario
            bool checkAlluFStressShadows = PropControl.checkAlluFStressShadows && (SD != StressDistribution.EvenlyDistributedStress);

            // Output control criteria
            // Flag to calculate separate tensors for cumulative inelastic (relaxed) strain in host rock and fractures; if false, will only calculate overall total cumulative strain tensor
            bool CalculateRelaxedStrainPartitioning = PropControl.CalculateRelaxedStrainPartitioning;
            // Flag to output the bulk rock compliance and stiffness tensors
            bool OutputBulkRockElasticTensors = PropControl.OutputBulkRockElasticTensors;
            // Flag to calculate full fracture cumulative population distribution function; if false, will only calculate total cumulative properties (i.e. r = 0 and l = 0)
            bool CalculatePopulationDistributionData = PropControl.CalculatePopulationDistributionData;
            // Flag to calculate and output fracture porosity
            bool CalculateFracturePorosity = PropControl.CalculateFracturePorosity;
            // Flag to determine method used to determine fracture aperture - used in porosity and permeability calculation
            FractureApertureType FractureApertureControl = PropControl.FractureApertureControl;

            // Get time units and unit conversion modifier for output time data if not in SI units
            TimeUnits timeUnits = PropControl.timeUnits;
            double timeUnits_Modifier = PropControl.getTimeUnitsModifier();

            // Determine whether implicit fracture data should be written to file, and if so create a file
            bool writeImplicitDataToFile = PropControl.WriteImplicitDataFiles;
            StreamWriter outputFile = null;
            if (writeImplicitDataToFile)
            {
                string fileName = string.Format("ImplicitData_X{0}_Y{1}.txt", SWtop.X, SWtop.Y);
                String namecomb = PropControl.FolderPath + fileName;
                outputFile = new StreamWriter(namecomb);
            }
            bool useSetNames = false;
            bool useDipSetNames = true;

            // Write header to logfile
            if (writeImplicitDataToFile)
            {
                string headerLine1 = string.Format("Timestep\tDuration ({0})\tEnd Time ({0})\tElastic ehmin\tElastic ehmax\tTotal ehmin\tTotal ehmax\t", timeUnits);
                string headerLine2 = "\t\t\t\t\t\t\t";
                string FSheader1 = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
                string FSheader2 = "Fracture stage\tDriving stress\tDisplacement sense\tSlip pitch\ta_uFP30\ts_uFP30\ta_uFP32\ts_uFP32\ta_MFP30\tsII_MFP30\tsIJ_MFP30\ta_MFP32\ts_MFP32\tClear zone volume\t";
#if LOGDFNPOP
                headerLine1 = string.Format("Timestep\tDuration ({0})\tEnd Time ({0})\t{1}\t{2}\t{3}\t{4}\t", timeUnits, "Sigma_eff.XX", "Sigma_eff.YY", "Sigma_eff.XY", "Sigma_eff.ZZ");
                FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "Mean_Azimuthal_MF_StressShadowWidth", "Mean_Shear_MF_StressShadowWidth", "Mean_MF_StressShadowWidth", "getInverseStressShadowVolume", "getInverseStressShadowVolumeAllFS",
                    "getClearZoneVolume", "sII_MFP30_total", "sIJ_MFP30_total", "a_MFP32_total", "s_MFP32_total", "getClearZoneVolumeAllFS");
                //FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "getPhi", "getInstantaneousF", "getMeanMFPropagationRate", "getConstantDrivingStressU", "getVariableDrivingStressV", 
                //    "a_MFP30_total", "sII_MFP30_total", "sIJ_MFP30_total", "a_MFP32_total", "s_MFP32_total", "getClearZoneVolume");
                //FSheader2 = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", "getEvolutionStage", "getFinalDrivingStressSigmaD", "Mode", "getAA", "getBB", "getCCStep", "getMeanStressShadowWidth", "getMeanShearStressShadowWidth",
                //    "getInverseStressShadowVolume", "getInverseStressShadowVolumeAllFS", "getClearZoneVolume", "a_MFP32_total", "s_MFP32_total", "getClearZoneVolumeAllFS");
#endif
                string TS0data = "0\t0\t0\t0\t0\t0\t0\t";
                for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
                {
                    Gridblock_FractureSet fs = FractureSets[fs_index];
                    int NoDipSets = fs.FractureDipSets.Count();
                    List<string> dipSetLabels = fs.DipSetLabels();
                    for (int dipsetIndex = 0; dipsetIndex < NoDipSets; dipsetIndex++)
                    {
                        headerLine1 += string.Format("FS {0} {1}", (useSetNames ? getFractureSetName(fs_index) : fs_index.ToString()), (useDipSetNames ? dipSetLabels[dipsetIndex] : string.Format("Dipset {0}", dipsetIndex))) + FSheader1;
                        headerLine2 += FSheader2;
                        TS0data += "NotActivated\t0\t\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t1\t";
                    }
                }
                if (CalculateFracturePorosity)
                {
                    headerLine1 += "uF Porosity: Uniform aperture\tMF Porosity: Uniform aperture\tuF Porosity: Size-dependent aperture\tMF Porosity: Size-dependent aperture\tuF Porosity: Dynamic aperture\tMF Porosity: Dynamic aperture\tuF Porosity: Barton-Bandis aperture\tMF Porosity: Barton-Bandis aperture\t";
                    headerLine2 += "\t\t\t\t\t\t\t\t";
                    TS0data += "0\t0\t0\t0\t0\t0\t0\t0\t";
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

            // Set the fracture distribution flags for each fracture set, based on the specified stress distribution case
            foreach (Gridblock_FractureSet fs in FractureSets)
                fs.FractureDistribution = SD;

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
                    Tensor2S appliedEffectiveStressRate = currentDeformationEpisode.Absolute_Stress_dashed - new Tensor2S(-fluidPressureRate, -fluidPressureRate, -fluidPressureRate, 0, 0, 0);
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

                // Usually the calculation will start from the stress and strain state at the end of the previous deformation episode, or the initial lithostatic load state if this is the first episode
                // However if coupling with output from geomechanical modelling, it may be necessary to adjust the stress and strain state to match the state in the geomechanical model output
                // This is done by passing the  object from the current DeformationEpisodeLoadControl to the StressStrainState.SetStressStrainState function
                if (currentDeformationEpisode.InitialStressStateDefined)
                    StressStrain.SetStressState(currentDeformationEpisode.InitialStressState);

                // We will also recalculate the incremental azimuthal and horizontal shear strain acting on the fractures, for the specified applied strain rate tensor
                foreach (Gridblock_FractureSet fs in FractureSets)
                {
                    fs.RecalculateHorizontalStrainRatios(appliedStrainRate);
                }

                // If required, populate the azimuthal and strike-slip shear stress shadow multiplier arrays
                if (checkAlluFStressShadows)
                {
                    for (int I = 0; I < NoFractureSets; I++)
                    {
                        Gridblock_FractureSet FSI = FractureSets[I];
                        for (int J = 0; J < NoFractureSets; J++)
                        {
                            if (I != J)
                            {
                                Gridblock_FractureSet FSJ = FractureSets[J];
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
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.RecalculateElasticResponse(StressStrain.Sigma_eff);
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
                                    if (!StaticStrain && (TimestepDuration > tr))
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
                                            if ((timeToEquilibrium > 0) && (TimestepDuration > timeToEquilibrium))
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

                    // Create a new FractureCalculationData object for the current timestep, and populate it with data from the end of the previous timestep
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setTimestepData();
                        }
                    }

                    // Update the macrofracture stress shadow widths (which may have changed due to changes in the in situ stress)
                    // If any stress shadow widths have changed, this will also update the macrofracture spacing distribution data and clear zone volume
                    bool stressShadowWidthChanged = false;
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        if (fs.setStressShadowWidthData())
                            stressShadowWidthChanged = true;
                    }

                    // If required, calculate the inverse stress shadow and clear zone volume multipliers to account for stress shadows from other fracture sets
                    if (checkAlluFStressShadows && stressShadowWidthChanged)
                        setCrossFSStressShadows();

                    // Check if any of the fracture sets meet the deactivation criteria, after in situ stress and stress shadow widths have been recalculated 
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        fs.CheckFractureDeactivation(historic_a_MFP33_termination_ratio, active_total_MFP30_termination_ratio, minimum_ClearZone_Volume, minrb_maxRad);
                    }

                    // Reset the current Fracture Calculation Data, calculate the U and V values and optimal timestep duration for each fracture dip set
                    foreach (Gridblock_FractureSet fs in FractureSets)
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

                    // If the timestep duration is still infinity, no further fractures can form; therefore set the current timestep duration to zero and set the flag to stop the calculation at the end of it
                    if (double.IsInfinity(TimestepDuration))
                    {
                        TimestepDuration = 0;
                        CalculationCompleted = true;
                    }

                    // Calculate calculate the driving stress and propagation rate data for each fracture dip set
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setTimestepPropagationData(endLastTimestep, TimestepDuration);
                        }
                    }

                    // Calculate the macrofracture deactivation probabilities Phi_II_M and Phi_IJ_M for each fracture dip set
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setMacrofractureDeactivationRate();
                        }
                    }

                    // Calculate the total half-macrofracture population data for this timestep for each fracture dip set
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.calculateTotalMacrofracturePopulation();
                        }
                    }

                    // If required, update the macrofracture termination array
                    if (checkAlluFStressShadows)
                        upDateMFTerminations();

                    // Calculate and update the macrofracture density, macrofracture spacing distribution and clear zone volume data in the CurrentFractureData object
                    // NB we cannot do this as we calculate the new macrofracture density data for the timestep, because we need to keep the previous values until all macrofracture sets have been calculated
                    // Otherwise we will introduce a bias in the calculation of residual fracture populations based on the order of calculation
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            fds.setMacrofractureDensityData();
                        }
                        fs.calculateMacrofractureSpacingDistributionData();
                    }

                    // If required, calculate the inverse stress shadow and clear zone volume multipliers to account for stress shadows from other fracture sets
                    if (checkAlluFStressShadows)
                        setCrossFSStressShadows();

                    // Calculate the new total linear microfracture population data for each fracture dip set, and update the CurrentFractureData object
                    // NB the microfracture densities from one set do not affect the microfracture density calculations for the other sets
                    // so we do not need to calculate the population data for all sets before we can update the CurrentFractureData objects
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                        {
                            // Calculate the total linear microfracture population data for this timestep for each fracture dip set
                            // NB we cannot calculate uF_P_30(0,t) for power law initial microfracture distribution as this will be infinite
                            fds.calculateTotalMicrofracturePopulation(no_r_bins);
                            fds.setMicrofractureDensityData();
                        }
                    }

                    // Check if any or all of the fracture sets meet the deactivation criteria, after the fracture densities have been recalculated
                    AllSetsDeactivated = true;
                    foreach (Gridblock_FractureSet fs in FractureSets)
                    {
                        AllSetsDeactivated = AllSetsDeactivated && fs.CheckFractureDeactivation(historic_a_MFP33_termination_ratio, active_total_MFP30_termination_ratio, minimum_ClearZone_Volume, minrb_maxRad);
                    }

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
#if LOGDFNPOP
                        timestepData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t", CurrentImplicitTimestep, TimestepDuration / timeUnits_Modifier, CurrentImplicitTime / timeUnits_Modifier, StressStrain.Sigma_eff.Component(Tensor2SComponents.XX), StressStrain.Sigma_eff.Component(Tensor2SComponents.YY), StressStrain.Sigma_eff.Component(Tensor2SComponents.XY), StressStrain.Sigma_eff.Component(Tensor2SComponents.ZZ));
                        //timestepData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t", CurrentImplicitTimestep, TimestepDuration / timeUnits_Modifier, CurrentImplicitTime / timeUnits_Modifier, StressStrain.Sigma_dashed.Component(Tensor2SComponents.XX), StressStrain.Sigma_dashed.Component(Tensor2SComponents.YY), StressStrain.Sigma_dashed.Component(Tensor2SComponents.XY), StressStrain.Sigma_dashed.Component(Tensor2SComponents.ZZ));
#endif
                        // Write data for each fracture set
                        foreach (Gridblock_FractureSet fs in FractureSets)
                        {
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                            {
                                // Get fracture data and add to timestep log string
                                fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.DisplacementSense, fds.DisplacementPitch, fds.a_uFP30_total(), fds.s_uFP30_total(), fds.a_uFP32_total(), fds.s_uFP32_total(),
                                    fds.a_MFP30_total(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
#if LOGDFNPOP
                                fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.Mean_Azimuthal_MF_StressShadowWidth, fds.Mean_Shear_MF_StressShadowWidth, fds.Mean_MF_StressShadowWidth, fds.getInverseStressShadowVolume(), fds.getInverseStressShadowVolumeAllFS(),
                                    fds.getClearZoneVolume(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
                                //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.getPhi(), fds.getInstantaneousF(), fds.getMeanMFPropagationRate(), fds.getConstantDrivingStressU(), fds.getVariableDrivingStressV(), 
                                //    fds.a_MFP30_total(), fds.sII_MFP30_total(), fds.sIJ_MFP30_total(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolume());
                                //fractureSetData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t", fds.getEvolutionStage(), fds.getFinalDrivingStressSigmaD(), fds.Mode, fds.getAA(), fds.getBB(), fds.getCCStep(), fds.getMeanStressShadowWidth(), fds.getMeanShearStressShadowWidth(),
                                //    fds.getInverseStressShadowVolume(), fds.getInverseStressShadowVolumeAllFS(), fds.getClearZoneVolume(), fds.a_MFP32_total(), fds.s_MFP32_total(), fds.getClearZoneVolumeAllFS());
#endif
                                timestepData = timestepData + fractureSetData;
                            }
                        }

                        // If required, write the fracture porosity data
                        // Currently configured to output porosity data for all aperture types 
                        if (CalculateFracturePorosity)
                        {
                            Dictionary<FractureApertureType, double> uFPorosity = new Dictionary<FractureApertureType, double>();
                            Dictionary<FractureApertureType, double> MFPorosity = new Dictionary<FractureApertureType, double>();

                            uFPorosity.Add(FractureApertureType.Uniform, 0);
                            MFPorosity.Add(FractureApertureType.Uniform, 0);
                            uFPorosity.Add(FractureApertureType.SizeDependent, 0);
                            MFPorosity.Add(FractureApertureType.SizeDependent, 0);
                            uFPorosity.Add(FractureApertureType.Dynamic, 0);
                            MFPorosity.Add(FractureApertureType.Dynamic, 0);
                            uFPorosity.Add(FractureApertureType.BartonBandis, 0);
                            MFPorosity.Add(FractureApertureType.BartonBandis, 0);

                            foreach (FractureApertureType apertureType in Enum.GetValues(typeof(FractureApertureType)).Cast<FractureApertureType>())
                            {
                                foreach (Gridblock_FractureSet fs in FractureSets)
                                {
                                    uFPorosity[apertureType] += fs.combined_uF_Porosity(apertureType);
                                    MFPorosity[apertureType] += fs.combined_MF_Porosity(apertureType);
                                }
                            }

                            string porosityData = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t", uFPorosity[FractureApertureType.Uniform], MFPorosity[FractureApertureType.Uniform], uFPorosity[FractureApertureType.SizeDependent], MFPorosity[FractureApertureType.SizeDependent], uFPorosity[FractureApertureType.Dynamic], MFPorosity[FractureApertureType.Dynamic], uFPorosity[FractureApertureType.BartonBandis], MFPorosity[FractureApertureType.BartonBandis]);
                            timestepData += porosityData;
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

                    // Check if calculation is finished
                    // Check if we have run to completion
                    if (CurrentImplicitTime >= CurrentDeformationEpisodeEndTime)
                        CalculationCompleted = true;
                    // Check if we have exceeded maximum number of timesteps
                    if (CurrentImplicitTimestep >= maxTimesteps)
                    {
                        CalculationCompleted = true;
                        HitTimestepLimit = true;
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
                double maxLengthMultiplier = 4;

                double maxHMinLength = PropControl.max_HMin_l_indexPoint_Length;
                double maxHMaxLength = PropControl.max_HMax_l_indexPoint_Length;

                for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
                {
                    Gridblock_FractureSet fs = FractureSets[fs_index];

                    // Calculate the maximum length for the macrofracture cumulative population distribution function index values based on orientation
                    // If this has not been specified, calculate this by applying a multiplier to the mean macrofracture length
                    double maxIndexLength;
                    if ((maxHMinLength <= 0) || (maxHMaxLength <= 0))
                    {
                        double denominator = fs.combined_T_MFP30_total() * ThicknessAtDeformation;
                        maxIndexLength = (denominator > 0 ? fs.combined_T_MFP32_total() / denominator : 0);
                        maxIndexLength *= maxLengthMultiplier;
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
                    else if ((fs_index == (NoFractureSets / 2)) && ((NoFractureSets % 2) == 0))
                    {
                        maxIndexLength = maxHMaxLength;
                    }
                    else
                    {
                        double relativeAngle = Math.PI * ((double)fs_index / (double)NoFractureSets);
                        double HMinComponent = Math.Pow(Math.Cos(relativeAngle), 2);
                        double HMaxComponent = Math.Pow(Math.Sin(relativeAngle), 2);
                        maxIndexLength = (maxHMinLength * HMinComponent) + (maxHMaxLength * HMaxComponent);
                    }

                    int NoDipSets = fs.FractureDipSets.Count();
                    List<string> dipSetLabels = fs.DipSetLabels();
                    for (int dipsetIndex = 0; dipsetIndex < NoDipSets; dipsetIndex++)
                    {
                        // Get a reference to the fracture dip set object
                        FractureDipSet fds = fs.FractureDipSets[dipsetIndex];

                        // Check to see if a maximum length has been set for the macrofracture cumulative population distribution function index values
                        if (maxIndexLength > 0) // If a maximum length has been set, generate the halflength index array manually
                        {
                            // Create a new list of halflength values
                            List<double> indexPoints = new List<double>();

                            // Add the required number of intermediate index points on a logarithmic scale
                            double logMaxLength = Math.Log(maxIndexLength + 1);
#if LOGDFNPOP
                            // Use to create an index point at length zero; this is for debugging only, as normally we will use the total P30 and P32 values calculated while looping through the timesteps for the zero length cumulative distribution function values
                            for (int indexPointNo = 0; indexPointNo < no_l_IndexPoints; indexPointNo++)
#else
                            for (int indexPointNo = 1; indexPointNo < no_l_IndexPoints; indexPointNo++)
#endif
                            {
                                if (fs.FractureDistribution == StressDistribution.EvenlyDistributedStress)
                                {
                                    double logNewValue = ((double)(no_l_IndexPoints - indexPointNo) / (double)no_l_IndexPoints) * logMaxLength;
                                    indexPoints.Add(maxIndexLength + 1 - Math.Exp(logNewValue));
                                }
                                else
                                {
                                    double logNewValue = ((double)indexPointNo / (double)no_l_IndexPoints) * logMaxLength;
                                    indexPoints.Add(Math.Exp(logNewValue) - 1);
                                }
                            }

                            // Add the final index point
                            indexPoints.Add(maxIndexLength);

                            // Set the macrofracture index array
                            fds.MF_halflengths = indexPoints;
                        }
                        else // otherwise generate the index array automatically using the reset_MF_halflength_array function
                        {
                            // Reset the macrofracture index array
                            fds.reset_MF_halflength_array(true, cullValue);
                        }

                        // Call the calculation function for the macrofracture cumulative population distribution function arrays
                        fds.calculateCumulativeMacrofracturePopulationArrays();

                        // Reset the microfracture index array
                        fds.reset_uF_radii_array(true, no_r_bins);
                        // Call the calculation function for the macrofracture cumulative population distribution function arrays
                        fds.calculateCumulativeMicrofracturePopulationArrays();

                        // Write data to logfile
                        if (writeImplicitDataToFile)
                        {
                            // Create strings for header data and write to file
                            string headerData = string.Format("FS {0} {1}", (useSetNames ? getFractureSetName(fs_index) : fs_index.ToString()), (useDipSetNames ? dipSetLabels[dipsetIndex] : string.Format("Dipset {0}", dipsetIndex)));
                            outputFile.WriteLine(headerData);

                            // Write macrofracture data
                            {
                                // Create stings for macrofracture data
                                string indexData = string.Format("Half-length\t{0}\t", 0);
                                string a_MFP30_data = string.Format("a_MFP30\t{0}\t", fds.a_MFP30_total());
                                string sII_MFP30_data = string.Format("sII_MFP30\t{0}\t", fds.sII_MFP30_total());
                                string sIJ_MFP30_data = string.Format("sIJ_MFP30\t{0}\t", fds.sIJ_MFP30_total());
                                string a_MFP32_data = string.Format("a_MFP32\t{0}\t", fds.a_MFP32_total());
                                string s_MFP32_data = string.Format("s_MFP32\t{0}\t", fds.s_MFP32_total());

                                // Loop through each point in the index value array and write data for that point
                                int noIndexPoints = fds.MF_halflengths.Count();
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
                                string indexData = string.Format("Radius\t{0}\t", 0);
                                string a_uFP30_data = string.Format("a_uFP30\t{0}\t", fds.a_uFP30_total());
                                string s_uFP30_data = string.Format("s_uFP30\t{0}\t", fds.s_uFP30_total());
                                string a_uFP32_data = string.Format("a_uFP32\t{0}\t", fds.a_uFP32_total());
                                string s_uFP32_data = string.Format("s_uFP32\t{0}\t", fds.s_uFP32_total());
                                string a_uFP33_data = string.Format("a_uFP33\t{0}\t", fds.a_uFP33_total());
                                string s_uFP33_data = string.Format("s_uFP33\t{0}\t", fds.s_uFP33_total());

                                // Loop through each point in the index value array and write data for that point
                                int noIndexPoints = fds.uF_radii.Count();
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
            } // End calculate cumulative population distribution function arrays

            // If we have more than 2 fracture sets, output a table of connectivity between fracture sets for the final fracture network
            if (writeImplicitDataToFile && NoFractureSets > 2)
            {
                // Table header
                outputFile.WriteLine();
                outputFile.WriteLine("Fracture interconnectivity: volumetric density (P30) of macrofracture tips from fracture set I terminating against macrofractures from dipset Jm");
                outputFile.WriteLine("Terminating fracture dipset (Jm):\tPropagating fracture set (I):");
                string headerLine = "\t";
                for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
                    headerLine += string.Format("FS {0}\t", (useSetNames ? getFractureSetName(fs_index) : fs_index.ToString()));
                outputFile.WriteLine(headerLine);

                // Write table data
                for (int fsJ_index = 0; fsJ_index < NoFractureSets; fsJ_index++)
                {
                    Gridblock_FractureSet fsJ = FractureSets[fsJ_index];
                    int noDipSetsJ = fsJ.FractureDipSets.Count;
                    for (int dipSetIndexJ = 0; dipSetIndexJ < noDipSetsJ; dipSetIndexJ++)
                    {
                        FractureDipSet dipSetJ = fsJ.FractureDipSets[dipSetIndexJ];
                        string tableRow = string.Format("FS {0} {1}\t", fsJ_index, dipSetJ.Mode);
                        for (int fsI_index = 0; fsI_index < NoFractureSets; fsI_index++)
                            tableRow += string.Format("{0}\t", MFTerminations[fsI_index, fsJ_index][dipSetIndexJ]);
                        outputFile.WriteLine(tableRow);
                    }
                }
            }

            // Close the log file
            if (writeImplicitDataToFile)
                outputFile.Close();

            return HitTimestepLimit;
        }
        /// <summary>
        /// Calculate fracture data based on user-specified PropagationControl object, building on existing fracture populations 
        /// </summary>
        /// <param name="pc_in">PropagationControl object containing propagation control data</param>
        /// <returns>True if the calculation runs to completion before hitting the timestep limit; false if the timestep limit is hit</returns>
        public bool CalculateFractureData(PropagationControl pc_in)
        {
            PropControl = pc_in;
            return CalculateFractureData();
        }
        /// <summary>
        /// Grow the explicit DFN for the next timestep, based on data from the implicit model calculation, cached in the appropriate FCD_list objects
        /// </summary>
        /// <param name="global_DFN">Reference to GlobalDFN object containing the global DFN</param>
        /// <param name="DFNControl">Reference to DFNControl object containing control data for DFN generation</param>
        public void PropagateDFN(GlobalDFN global_DFN, DFNGenerationControl DFNControl)
        {
            // Check gridblock geometry is defined, if not abort
            if (!checkCornerpointsDefined()) return;

            // Update the current timestep counter
            CurrentExplicitTimestep++;

            // Cache constants locally
            double max_uF_radius = MaximumMicrofractureRadius;
            double SqrtPi = Math.Sqrt(Math.PI);
            double CapA = MechProps.CapA;
            double b = MechProps.b_factor;
            bool bis2 = (MechProps.GetbType() == bType.Equals2);
            double beta = MechProps.beta;
            double Kc = MechProps.Kc;
            // initial_uF_factor is a component related to the maximum microfracture radius rmax, included in Cum_hGamma to represent the initial population of seed macrofractures: ln(rmax) for b=2; rmax^(1/beta) for b!=2
            double initial_uF_factor = Initial_uF_factor;
            // hb1_factor is (h/2)^(b/2), = h/2 if b=2
            // NB this relates to macrofracture propagation rate so is always calculated from h/2, regardless of the fracture nucleation position
            double hb1_factor = (bis2 ? ThicknessAtDeformation / 2 : Math.Pow(ThicknessAtDeformation / 2, b / 2));
            double GridblockVolume = Area * ThicknessAtDeformation;
            double TimestepDuration = CurrentExplicitTime - TimestepEndTimes[CurrentExplicitTimestep - 1];
            double uF_minRadius = DFNControl.MicrofractureDFNMinimumRadius;
            double MF_minLength = DFNControl.MacrofractureDFNMinimumLength;
            bool SpecifyFractureNucleationPosition = (PropControl.FractureNucleationPosition >= 0);
            double FractureNucleationPosition_w = PropControl.FractureNucleationPosition;

            // Flags for calculating microfractures and macrofractures
            bool calc_uF = ((uF_minRadius > 0) && (uF_minRadius < max_uF_radius));
            bool calc_MF = (MF_minLength >= 0);
            bool add_MF_directly = calc_MF && !calc_uF; // We do not need to add macrofractures directly to the DFN if it includes microfractures - macrofractures will be nucleated automatically when microfracture radius reaches h/2
            bool use_MF_min_length_cutoff = (MF_minLength > 0) && !calc_uF; // There cannot be a minimum macrofracture cutoff length if the DFN includes microfractures
            bool checkStressShadow = (PropControl.StressDistributionCase == StressDistribution.StressShadow);
            bool checkAlluFStressShadows = PropControl.checkAlluFStressShadows && checkStressShadow;
            bool TerminateAtGridBoundary = DFNControl.CropToGrid;
            double probabilisticFractureNucleationLimit = DFNControl.probabilisticFractureNucleationLimit;
            bool allowProbabilisticFractureNucleation = (probabilisticFractureNucleationLimit > 0);
            bool searchNeighbouringGridblocks = SearchNeighbouringGridblocks();
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

            // Get reference to the random number generator
            Random randGen = gd.RandomNumberGenerator;

            // Create a temporary list for the maximum macrofracture propagation lengths
            List<List<double>> maxPropLengths = new List<List<double>>();

#if LOGDFNPOP
            // Create a dictionary for the output files for each fracture set
            List<StreamWriter> DFNPopLogFiles = new List<StreamWriter>();

            // Create dictionaries for all set-specific logging variables
            List<int> Dict_NoTotalFracs = new List<int>();
            List<int> Dict_NoActiveFracs = new List<int>();
            List<int> Dict_NoTotalExistingFracs = new List<int>();
            List<int> Dict_NoActiveExistingFracs = new List<int>();
            List<int> Dict_NoStressShadowInteractions = new List<int>();
            List<int> Dict_NoIntersections = new List<int>();
            List<int> Dict_NoPropagatingOut = new List<int>();
            List<int> Dict_NoTotalNucleating = new List<int>();
            List<int> Dict_NoActiveNucleating = new List<int>();
            List<int> Dict_MostPopulousDipSetIndex = new List<int>();
            List<double> Dict_measuredStressShadowVol = new List<double>();
            List<double> Dict_measuredExclusionZoneVol = new List<double>();
#endif

            // Propagate microfractures and nucleate macrofractures, for each fracture set
            // Loop through each fracture set
            for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
            {
                Gridblock_FractureSet fs = FractureSets[fs_index];

                // Create a null reference to a list of stress shadow half-widths of other fracture sets as seen by this fracture set, and to the stress shadow half-widths of this fracture set as seen by other fracture sets
                // These will be filled out as required
                List<List<double>> StressShadowHalfWidthsIJ = null;
                List<List<double>> StressShadowHalfWidthsJI = null;

#if LOGDFNPOP
                string fileName = string.Format("DFNPopulationLog_X{0}_Y{1}_Set{2}.txt", SWtop.X, SWtop.Y, fs_index);
                String namecomb = PropControl.FolderPath + fileName;

                StreamWriter DFNPopLogFile;
                if (CurrentExplicitTimestep == 1)
                {
                    DFNPopLogFile = new StreamWriter(namecomb, false);
                    DFNPopLogFile.WriteLine(string.Format("DFN statistics: Set {0}", fs_index));
                    DFNPopLogFile.WriteLine("");
                    DFNPopLogFile.WriteLine("Timestep\tTotal no of fractures at end of TS\tNo of propagating fractures at end of TS\tTotal no of fractures at start of TS\tNo of propagating fractures at start of TS\tNumber of fractures terminating due to stress shadow interaction\tNumber of fractures terminating due to intersection\tNumber of fractures propagating out\tMaximum potential new fractures nucleating\tNumber of active new fractures nucleating\tMost populous dipset\tStress shadow volume\tExclusion zone volume");
                }
                else
                {
                    DFNPopLogFile = new StreamWriter(namecomb, true);
                }
                DFNPopLogFiles.Add(DFNPopLogFile);

                // Initialise counter variables for this fracture set
                Dict_NoTotalFracs.Add(0);
                Dict_NoActiveFracs.Add(0);
                Dict_NoTotalExistingFracs.Add(0);
                Dict_NoActiveExistingFracs.Add(0);
                Dict_NoStressShadowInteractions.Add(0);
                Dict_NoIntersections.Add(0);
                Dict_NoPropagatingOut.Add(0);
                Dict_NoTotalNucleating.Add(0);
                Dict_NoActiveNucleating.Add(0);

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
                        if (checkInMFStressShadow(testPointXYZ, fs_index, ref StressShadowHalfWidthsIJ))
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
                        if (checkInMFExclusionZone(testPointXYZ, fs_index, MostPopulousDipSetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI))
                            NoInExclusionZone++;
                    }
                    else
                    {
                        if (fs.checkInMFExclusionZone(testPoint, StressShadowWidth))
                            NoInExclusionZone++;
                    }
                }

                // Calculate the stress shadow and exclusion zone volumes from the proportion of points lying in them
                Dict_MostPopulousDipSetIndex.Add(MostPopulousDipSetIndex);
                Dict_measuredStressShadowVol.Add((double)NoInStressShadow / (double)NoTestPoints);
                Dict_measuredExclusionZoneVol.Add((double)NoInExclusionZone / (double)NoTestPoints);
#endif

                // Determine the number of fracture dip sets and the maximum macrofracture propagation length for each dip set
                int NoDipSets = fs.FractureDipSets.Count();
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
                    if (halfLength_M > 1E+50)
                        return;

                    // Calculate local helper variables
                    double CapB = fds.CapB;
                    double CapBV = CapB * GridblockVolume;
                    double c_coefficient = fds.c_coefficient;
                    // betac_factor is -beta*c if b<>2, -c if b=2
                    double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
                    // Maximum propagation length: already stored in FractureCalculationData object
                    double ts_PropLength = halfLength_M;
                    // Macrofracture stress shadow width
                    double MF_StressShadowWidth = fds.getMeanStressShadowWidth(CurrentExplicitTimestep);

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
                                // Get random location for new microfracture
                                PointXYZ new_uf_centrepointXYZ = getRandomPoint(false);
                                if (SpecifyFractureNucleationPosition)
                                {
                                    double u, v;
                                    if (getPositionRelativeToGridblock(out u, out v, new_uf_centrepointXYZ.X, new_uf_centrepointXYZ.Y))
                                        new_uf_centrepointXYZ = getAbsolutePosition(u, v, FractureNucleationPosition_w);
                                }
                                PointIJK new_uF_centrepointIJK = fs.convertXYZtoIJK(new_uf_centrepointXYZ);

                                // There are no macrofractures yet so there will be no stress shadows even if we are including stress shadow effects
                                bool addThisFracture = true;

                                // Generate a new microfracture and add it to the DFN
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
                                    MicrofractureIJK new_local_uF = new MicrofractureIJK(fs, dipsetIndex, new_uF_centrepointIJK, next_uF_radius, dipdir, initialLTime, 0);
                                    fs.LocalDFNMicrofractures.Add(new_local_uF);

                                    // Create a corresponding MicrofractureXYZ object and add it to the global DFN
                                    MicrofractureXYZ new_global_uF = new_local_uF.createLinkedGlobalMicrofracture(fs_index);
                                    global_DFN.GlobalDFNMicrofractures.Add(new_global_uF);
                                }

                                // Update the number of microfractures that we need to add - if there are no more to add we can break out of the loop
                                no_uF_toAdd--;
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
                            PointXYZ new_uf_centrepointXYZ = getRandomPoint(false);
                            if (SpecifyFractureNucleationPosition)
                            {
                                double u, v;
                                if (getPositionRelativeToGridblock(out u, out v, new_uf_centrepointXYZ.X, new_uf_centrepointXYZ.Y))
                                    new_uf_centrepointXYZ = getAbsolutePosition(u, v, FractureNucleationPosition_w);
                            }
                            PointIJK new_uF_centrepointIJK = fs.convertXYZtoIJK(new_uf_centrepointXYZ);

                            // If we are including stress shadow effects, check whether this point lies in the stress shadow of an existing macrofracture and if so set flag to ignore it
                            bool addThisFracture = true;
                            if (checkStressShadow)
                            {
                                // First check other macrofractures from this gridblock
                                if (checkAlluFStressShadows)
                                    addThisFracture = !checkInMFStressShadow(new_uf_centrepointXYZ, fs_index, ref StressShadowHalfWidthsIJ);
                                else
                                    addThisFracture = !fs.checkInMFStressShadow(new_uF_centrepointIJK);

                                // Then, if required, check macrofractures from adjacent gridblocks
                                // NB we do not need to do this if we have already found a stress shadow interaction
                                if (addThisFracture && SearchNeighbouringGridblocks())
                                {
                                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                    // Loop through each gridblock in the list
                                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                    {
                                        if (checkAlluFStressShadows)
                                        {
                                            // Find the index number of the equivalent fracture set in the neighbouring gridblock
                                            int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, fs.Strike);

                                            // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                            // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                            // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                                            // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                                            // We will therefore use the list generated for this gridblock to speed up the calculation
                                            if (neighbour_gb.checkInMFStressShadow(new_uf_centrepointXYZ, neighbourGB_fs_index, ref StressShadowHalfWidthsIJ))
                                            {
                                                addThisFracture = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            // Find the correct fracture set in the neighbouring gridblock to search
                                            Gridblock_FractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fs_index, fs.Strike);

                                            // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                            // Since the neighbouring gridblock will have different local coordinates, we must supply the location of the new macrofracture nucleation point in global XYZ coordinates
                                            // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                            if (neighbourGB_fs.checkInMFStressShadow(new_uf_centrepointXYZ))
                                            {
                                                addThisFracture = false;
                                            }
                                        }
                                    }
                                } // End check macrofractures from adjacent gridblocks
                            } // End check whether this point lies in the stress shadow of an existing macrofracture

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
                                Dict_NoTotalNucleating[fs_index] += 2;
                                Dict_NoActiveNucleating[fs_index] += 2;
#endif
                                // Get random location for new macrofracture nucleation point
                                PointIJK new_MF_nucleationpoint = fs.convertXYZtoIJK(getRandomPoint(false));

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
                            Dict_NoTotalNucleating[fs_index] += 2;
#endif
                            // Get random location for new macrofracture nucleation point
                            PointXYZ new_MF_nucleationpointXYZ = getRandomPoint(false);
                            PointIJK new_MF_nucleationpointIJK = fs.convertXYZtoIJK(new_MF_nucleationpointXYZ);

                            // If we are including stress shadow effects, check whether this point lies in the exclusion zone of an existing macrofracture and if so set flag to ignore it
                            bool addThisFracture = true;
                            if (checkStressShadow)
                            {
                                // First check other macrofractures from this gridblock
                                if (checkAlluFStressShadows)
                                    addThisFracture = !checkInMFExclusionZone(new_MF_nucleationpointXYZ, fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI);
                                else
                                    addThisFracture = !fs.checkInMFExclusionZone(new_MF_nucleationpointIJK, MF_StressShadowWidth);

                                // Then, if required, check macrofractures from adjacent gridblocks
                                // NB we do not need to do this if we have already found a stress shadow interaction
                                if (addThisFracture && SearchNeighbouringGridblocks())
                                {
                                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                    // Loop through each gridblock in the list
                                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                    {
                                        if (checkAlluFStressShadows)
                                        {
                                            // Find the index number of the equivalent fracture set in the neighbouring gridblock
                                            int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, fs.Strike);

                                            // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                            // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                                            // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                                            // We will therefore use the list generated for this gridblock to speed up the calculation
                                            // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                            if (neighbour_gb.checkInMFExclusionZone(new_MF_nucleationpointXYZ, neighbourGB_fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI))
                                            {
                                                addThisFracture = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            // Find the correct fracture set in the neighbouring gridblock to search
                                            Gridblock_FractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fs_index, fs.Strike);

                                            // Now check the macrofractures in the identified neighbouring gridblock fracture set for stress shadow interaction
                                            // Since the neighbouring gridblock will have different local coordinates, we must supply the location of the new macrofracture nucleation point in global XYZ coordinates
                                            // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                            if (neighbourGB_fs.checkInMFExclusionZone(new_MF_nucleationpointXYZ, MF_StressShadowWidth))
                                            {
                                                addThisFracture = false;
                                                break;
                                            }
                                        }
                                    }
                                } // End check macrofractures from adjacent gridblocks
                            } // End check whether this point lies in the stress shadow of an existing macrofracture

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
                                Dict_NoActiveNucleating[fs_index] += 2;
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
                        }
                    } // End add macrofractures
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
                                // First check other macrofractures from this gridblock
                                // If we find a stress shadow interaction, deactivate this microfracture and move on to the next one
                                if (checkAlluFStressShadows)
                                {
                                    if (checkInMFStressShadow(fs.convertIJKtoXYZ(uF.CentrePoint), fs_index, ref StressShadowHalfWidthsIJ))
                                    {
                                        uF.Active = false;
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (fs.checkInMFStressShadow(uF.CentrePoint))
                                    {
                                        uF.Active = false;
                                        continue;
                                    }
                                }

                                // Then, if required, check macrofractures from adjacent gridblocks
                                if (SearchNeighbouringGridblocks())
                                {
                                    // Convert the microfracture centrepoint to global (XYZ) coordinates
                                    PointXYZ uFcentrepointXYZ = fs.convertIJKtoXYZ(uF.CentrePoint);

                                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                    // Loop through each gridblock in the list
                                    bool deactivateFracture = false;
                                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                    {
                                        if (checkAlluFStressShadows)
                                        {
                                            // Find the index number of the equivalent fracture set in the neighbouring gridblock
                                            int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, fs.Strike);

                                            // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                            // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                                            // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                                            // We will therefore use the list generated for this gridblock to speed up the calculation
                                            // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                            if (neighbour_gb.checkInMFStressShadow(uFcentrepointXYZ, neighbourGB_fs_index, ref StressShadowHalfWidthsIJ))
                                            {
                                                deactivateFracture = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            // Find the correct fracture set in the neighbouring gridblock to search
                                            Gridblock_FractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fs_index, fs.Strike);

                                            // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                            // Since the neighbouring gridblock will have different local coordinates, we must supply the location of the new macrofracture nucleation point in global XYZ coordinates
                                            // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                            if (neighbourGB_fs.checkInMFStressShadow(uFcentrepointXYZ))
                                            {
                                                deactivateFracture = false;
                                                break;
                                            }
                                        }
                                    }

                                    // If we find a stress shadow interaction with a macrofracture from another gridblock, deactivate this microfracture and move on to the next one
                                    if (deactivateFracture)
                                    {
                                        uF.Active = false;
                                        continue;
                                    }
                                } // End check macrofractures from adjacent gridblocks
                            } // End check if it is in a macrofracture stress shadow

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
                                // Set radius to h/2
                                uF.Radius = (max_uF_radius);

                                // Set centrepoint of microfracture to centre of layer - this is to prevent it extending out of layer
                                uF.CentrePoint.K = 0;

                                // Set microfracture flag to inactive
                                uF.Active = false;

                                // If we are including stress shadow effects, check whether the microfracture lies in the exclusion zone of an existing macrofracture and if so set flag to ignore it
                                // NB microfractures may remain active while they are in an exclusion zone, as long as they are not within a stress shadow, so we must recheck this
                                bool addThisFracture = true;
                                double MF_StressShadowWidth = fs.FractureDipSets[dipsetIndex].Mean_MF_StressShadowWidth;
                                if (checkStressShadow)
                                {
                                    // First check other macrofractures from this gridblock
                                    if (checkAlluFStressShadows)
                                        addThisFracture = !checkInMFExclusionZone(fs.convertIJKtoXYZ(uF.CentrePoint), fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI);
                                    else
                                        addThisFracture = !fs.checkInMFExclusionZone(uF.CentrePoint, MF_StressShadowWidth);

                                    // Then, if required, check macrofractures from adjacent gridblocks
                                    // NB we do not need to do this if we have already found a stress shadow interaction
                                    if (addThisFracture && SearchNeighbouringGridblocks())
                                    {
                                        // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                        List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                        // Loop through each gridblock in the list
                                        foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                        {
                                            if (checkAlluFStressShadows)
                                            {
                                                // Find the index number of the equivalent fracture set in the neighbouring gridblock
                                                int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, fs.Strike);

                                                // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                                // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                                                // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                                                // We will therefore use the list generated for this gridblock to speed up the calculation
                                                // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                                if (neighbour_gb.checkInMFExclusionZone(fs.convertIJKtoXYZ(uF.CentrePoint), neighbourGB_fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI))
                                                {
                                                    addThisFracture = false;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                // Find the correct fracture set in the neighbouring gridblock to search
                                                Gridblock_FractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fs_index, fs.Strike);

                                                // Now check the macrofractures in the identified neighbouring gridblock fracture set for stress shadow interaction
                                                // Since the neighbouring gridblock will have different local coordinates, we must supply the location of the new macrofracture nucleation point in global XYZ coordinates
                                                // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                                if (neighbourGB_fs.checkInMFExclusionZone(fs.convertIJKtoXYZ(uF.CentrePoint), MF_StressShadowWidth))
                                                {
                                                    addThisFracture = false;
                                                    break;
                                                }
                                            }
                                        }
                                    } // End check macrofractures from adjacent gridblocks
                                } // End check whether this point lies in the stress shadow of an existing macrofracture

                                if (addThisFracture)
                                {
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
                                }
                            }
                            else // Otherwise just increment the microfracture radius
                            {
                                uF.Radius = newRadius;

                                // If the fracture nucleation position is undefined and the microfracture tip has reached one of the layer boundaries, move its centrepoint towards centre of layer
                                // This will prevent the microfracture extending out of layer; however this is only geologically valid if the microfractures grow anisotropically and it may skew the microfracture volumetric distribution
                                // If the fracture nucleation position is defined, microfractures may extend out of the layer; this can be rectified when generating the microfracture cornerpoints
                                if (PropControl.FractureNucleationPosition < 0)
                                {
                                    if (uF.CentrePoint.K < (uF.Radius - max_uF_radius)) uF.CentrePoint.K = (uF.Radius - max_uF_radius);
                                    if (uF.CentrePoint.K > (max_uF_radius - uF.Radius)) uF.CentrePoint.K = (max_uF_radius - uF.Radius);
                                }
                            }
                        } // End if microfracture is active
                    } // Loop to next microfracture
                } // End propagate microfractures
            } // Loop to next fracture set

            // If required, sort the list of all macrofracture segments in the gridblock in order of nucleation time
            // This will propagate macrofractures in strict order of nucleation, regardless of fracture set
            if (DFNControl.propagateFracturesInNucleationOrder)
                MacrofractureSegments.Sort();

            // Propagate macrofractures, testing for intersection, stress shadow interaction and leaving bounds
            // Loop through each macrofracture segment in the gridblock list
            if (calc_MF)
            {
                // First we will check again that macrofracture segments nucleated at time zero with has zero length do not lie in the exclusion zone of macrofracture from another set
                // This is necessary to deactivate dormant initial macrofractures that now lie within the exclusion zones of macrofractures from other sets
                if (checkAlluFStressShadows)
                {
                    foreach (MacrofractureSegmentHolder segmentHolder in MacrofractureSegments)
                    {
                        MacrofractureSegmentIJK MFSegment = segmentHolder.Segment;

                        // Check if macrofracture segment is still active, nucleated at timestep zero and currently has length zero
                        if (MFSegment.Active && (MFSegment.NucleationTimestep == 0) && ((float)MFSegment.StrikeLength == 0f))
                        {
                            // Get the macrofracture set and dipset indices
                            int fs_index = segmentHolder.FractureSetIndex;
                            Gridblock_FractureSet fs = FractureSets[fs_index];
                            int dipsetIndex = MFSegment.FractureDipSetIndex;

                            // Calculate maximum propagation distance - given by integral of alpha_MF * sigmad_b * t for duration of growth
                            // This will be equal to PropLength if the uF nucleated in a previous timestep, or (PropLength - NucleationLTime) if it nucleated in this timestep
                            double fds_maxPropLength = maxPropLengths[fs_index][dipsetIndex];
                            double maxPropLength = ((MFSegment.NucleationTimestep == CurrentExplicitTimestep) ? fds_maxPropLength - MFSegment.NucleationLTime : fds_maxPropLength);

                            // Check if the maximum propagation length is zero - if so we can skip the calculation
                            if (maxPropLength > 0)
                            {
                                // Create a null reference to a list of stress shadow half-widths of other fracture sets as seen by this fracture set, and to the stress shadow half-widths of this fracture set as seen by other fracture sets
                                List<List<double>> StressShadowHalfWidthsIJ = null;
                                List<List<double>> StressShadowHalfWidthsJI = null;

                                // First check other macrofractures from this gridblock
                                PointXYZ segmentPropNodeXYZ = MFSegment.getPropNodeinXYZ();
                                bool deactivateThisFracture = checkInMFExclusionZone(segmentPropNodeXYZ, fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI);

                                // Then, if required, check macrofractures from adjacent gridblocks
                                // NB we do not need to do this if we have already found a stress shadow interaction
                                if (!deactivateThisFracture && SearchNeighbouringGridblocks())
                                {
                                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                                    // Loop through each gridblock in the list
                                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                                    {
                                        // Find the index number of the equivalent fracture set in the neighbouring gridblock
                                        int neighbourGB_fs_index = neighbour_gb.getClosestFractureSetIndex(fs_index, fs.Strike);

                                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                                        // NB Strictly speaking, we should generate a new list of stress shadow half-widths, as the current list is not applicable to the neighbouring gridblocks
                                        // However we will assume that the differences between stress shadow widths in neighbouring gridblocks is small (and will in any case be gradual)
                                        // We will therefore use the list generated for this gridblock to speed up the calculation
                                        // If a stress shadow interaction is found, we do not need to check the remaining gridblocks
                                        if (neighbour_gb.checkInMFExclusionZone(segmentPropNodeXYZ, neighbourGB_fs_index, dipsetIndex, ref StressShadowHalfWidthsIJ, ref StressShadowHalfWidthsJI))
                                        {
                                            deactivateThisFracture = true;
                                            break;
                                        }
                                    }
                                } // End check macrofractures from adjacent gridblocks

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
                } // End check again that macrofracture segments nucleated at time zero with has zero length do not lie in the exclusion zone of macrofracture from another set

                // Propagate macrofracture segments
                foreach (MacrofractureSegmentHolder segmentHolder in MacrofractureSegments)
                {
                    MacrofractureSegmentIJK MFSegment = segmentHolder.Segment;
                    int fs_index = segmentHolder.FractureSetIndex;
                    Gridblock_FractureSet fs = FractureSets[fs_index];
#if LOGDFNPOP
                    // Update counter for total number of fractures and total excluding fractures nucleating during this timestep, relay segments and zero length fractures
                    Dict_NoTotalFracs[fs_index]++;
                    bool fromPreviousTS = ((MFSegment.NucleationTimestep < CurrentExplicitTimestep) && (MFSegment.StrikeLength > 0));
                    if (fromPreviousTS) Dict_NoTotalExistingFracs[fs_index]++;
#endif
                    // If macrofracture segment is still active, grow it
                    if (MFSegment.Active)
                    {
#if LOGDFNPOP
                        // Update counters for number of active fractures existing already and nucleating in this timestep
                        Dict_NoActiveFracs[fs_index]++;
                        if (fromPreviousTS) Dict_NoActiveExistingFracs[fs_index]++;
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
                            // The process of extending the fracture, after checking for stress shadow interaction, intersection or propagating across a gridblock boundary, is handled by a separate function
#if LOGDFNPOP
                            int NoStressShadowInteractions = Dict_NoStressShadowInteractions[fs_index];
                            int NoIntersections = Dict_NoIntersections[fs_index];
                            int NoPropagatingOut = Dict_NoPropagatingOut[fs_index];
                            ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fs_index, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
                            Dict_NoStressShadowInteractions[fs_index] = NoStressShadowInteractions;
                            Dict_NoIntersections[fs_index] = NoIntersections;
                            Dict_NoPropagatingOut[fs_index] = NoPropagatingOut;
#else
                            ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fs_index, fs, MFSegment, dipsetIndex, ref maxPropLength);
#endif
                        } // End if the maximum propagation length is zero
                    } // End if macrofracture segment is active
                } /// Loop to next macrofracture segment
            } // End propagate macrofractures

#if LOGDFNPOP
            // Write fracture counts to logfiles and close them
            for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
            {
                StreamWriter DFNPopLogFile = DFNPopLogFiles[fs_index];
                DFNPopLogFile.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}", CurrentExplicitTimestep, Dict_NoTotalFracs[fs_index], Dict_NoActiveFracs[fs_index], Dict_NoTotalExistingFracs[fs_index], Dict_NoActiveExistingFracs[fs_index], Dict_NoStressShadowInteractions[fs_index], Dict_NoIntersections[fs_index], Dict_NoPropagatingOut[fs_index], Dict_NoTotalNucleating[fs_index], Dict_NoActiveNucleating[fs_index], Dict_MostPopulousDipSetIndex[fs_index], Dict_measuredStressShadowVol[fs_index], Dict_measuredExclusionZoneVol[fs_index]));
                DFNPopLogFile.Close();
            }
#endif
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
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
#if LOGDFNPOP
        /// <param name="fromPreviousTS">Flag specifying whether fracture nucleated in this timestep or a previous timestep - used for debugging only</param>
        /// <param name="NoStressShadowInteractions">Counter for fracture stress shadow interactions - used for debugging only</param>
        /// <param name="NoIntersections">Counter for fracture intersections - used for debugging only</param>
        /// <param name="NoPropagatingOut">Counter for fractures propagating across gridblock boundaries - used for debugging only</param>
        public void PropagateMFIntoGridblock(MacrofractureSegmentIJK initiatorSegment, int segmentFSIndex, GridDirection FromBoundary, PointXYZ insertionPoint, double newSegmentNucleationTime, bool use_MF_min_length_cutoff, bool checkStressShadow, bool TerminateAtGridBoundary, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut)
#else
        private void PropagateMFIntoGridblock(MacrofractureSegmentIJK initiatorSegment, int segmentFSIndex, GridDirection FromBoundary, PointXYZ insertionPoint, double newSegmentNucleationTime, bool use_MF_min_length_cutoff, bool checkStressShadow, bool TerminateAtGridBoundary)
#endif
        {
            // Check that the fracture set index for the incoming fracture is not higher than the total number of sets in this gridblock
            if (segmentFSIndex >= NoFractureSets)
                segmentFSIndex = NoFractureSets - 1;
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
            Gridblock_FractureSet newSegment_fs = FractureSets[segmentFSIndex];
            double currentPropagationDirection = newSegment_fs.getPropagationAzimuth(newSegment_PropDir);

            // Check if the propagation direction of the equivalent set in this gridblock lies within the allowed range
            double maxAngularDifference = gd.DFNControl.MaxConsistencyAngle;
            double angularVariability = PointXYZ.getAngularDifference(currentPropagationDirection, previousPropagationDirection);
            if (angularVariability > maxAngularDifference)
            {
                // If the propagation direction lies within the allowed range, loop through all fracture sets and propagation directions to find the best fit (which may still be the equivalent)
                for (int test_fs_index = 0; test_fs_index < NoFractureSets; test_fs_index++)
                {
                    Gridblock_FractureSet test_fs = FractureSets[test_fs_index];

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
            foreach (Gridblock_FractureSet intersecting_fs in FractureSets)
            {
                // Call the function to check intersection
                if (newSegment_fs.checkFractureIntersectionOnBoundary(newSegment, intersecting_fs, false, true))
                {
                    // Set the fracture deactivation mechanism of the initiator segment to Intersection, and set reference to terminating macrofracture segment
                    initiatorSegment.PropNodeType = SegmentNodeType.Intersection;
                    initiatorSegment.TerminatingSegment = newSegment.TerminatingSegment;

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
                    ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, newSegment_FSIndex, newSegment_fs, newSegment, newSegment_DipSetIndex, ref propagationLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                    ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, newSegment_FSIndex, newSegment_fs, newSegment, newSegment_DipSetIndex, ref propagationLength);
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
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
        /// <param name="fsIndex">Fracture orientation</param>
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
        private SegmentNodeType ExtendFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool TerminateAtGridBoundary, int fsIndex, Gridblock_FractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut)
#else
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool TerminateAtGridBoundary, int fsIndex, Gridblock_FractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength)
#endif
        {
            // Check if a tracking boundary has been specified - if so call the ExtendBoundaryTrackingFracture function
            if (MFSegment.TrackingBoundary != GridDirection.None)
#if LOGDFNPOP
                return ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                return ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength);
#endif

            // Cache the initial maximum propagation length
            double initial_maxPropLength = maxPropLength;

            // Create a flag for fracture deactivation mechanism
            SegmentNodeType tipDeactivationMechanism = SegmentNodeType.Propagating;

            // Check if the segment will intersect a macrofracture from another set
            // Loop through every other fracture set, except this one
            for (int intersecting_fs_index = 0; intersecting_fs_index < NoFractureSets; intersecting_fs_index++)
            {
                if (intersecting_fs_index != fsIndex)
                {
                    Gridblock_FractureSet intersecting_fs = FractureSets[intersecting_fs_index];
                    if (fs.checkFractureIntersection(MFSegment, intersecting_fs, ref maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.Intersection;
                }
            }

            // Check if the segment will interact with another macrofracture stress shadow
            if (checkStressShadow)
            {
                // First check other macrofractures from this gridblock
                if (fs.checkStressShadowInteraction(MFSegment, ref maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;

                // Then, if required, check macrofractures from adjacent gridblocks
                if (SearchNeighbouringGridblocks())
                {
                    // Create a list of neighbouring gridblocks to search - include diagonal neighbours
                    List<GridblockConfiguration> gridblocksToSearch = getNeighbourGridblocks(true);

                    // Loop through each gridblock in the list
                    foreach (GridblockConfiguration neighbour_gb in gridblocksToSearch)
                    {
                        // Find the correct fracture set in the neighbouring gridblock to search
                        Gridblock_FractureSet neighbourGB_fs = neighbour_gb.getClosestFractureSet(fsIndex, fs.Strike);

                        // Now check the macrofractures in the identified adjacent gridblock fracture set for stress shadow interaction
                        if (fs.checkStressShadowInteraction(MFSegment, neighbourGB_fs, ref maxPropLength, true)) tipDeactivationMechanism = SegmentNodeType.ConnectedStressShadow;

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
                for (int intersecting_fs_index = 0; intersecting_fs_index < NoFractureSets; intersecting_fs_index++)
                {
                    if (intersecting_fs_index != fsIndex)
                    {
                        Gridblock_FractureSet intersecting_fs = FractureSets[intersecting_fs_index];
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

                    // We will not add the relay segment to the local fracture segment list for this gridblock as it is inactive and perpendicular to the other segments in this set
                    // Also, doing so would throw an exception as we would be adding to the collection of local segments while looping through them
                    // However we will add the linking segment to the same global DFN as the initiator segment
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
                        ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                        ExtendBoundaryTrackingFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength);
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
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary);
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
        /// <param name="TerminateAtGridBoundary">Flag specifying whether to terminate fracture propagation if the fracture crosses the external grid boundary</param>
        /// <param name="fsIndex">Fracture orientation</param>
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
        private SegmentNodeType ExtendBoundaryTrackingFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool TerminateAtGridBoundary, int fsIndex, Gridblock_FractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength, bool fromPreviousTS, ref int NoStressShadowInteractions, ref int NoIntersections, ref int NoPropagatingOut)
#else
        /// <returns>Flag specifying whether and how fracture terminates early</returns>
        private SegmentNodeType ExtendBoundaryTrackingFracture(bool use_MF_min_length_cutoff, bool checkStressShadow, bool TerminateAtGridBoundary, int fsIndex, Gridblock_FractureSet fs, MacrofractureSegmentIJK MFSegment, int dipsetIndex, ref double maxPropLength)
#endif
        {
            // Get the tracking boundary
            GridDirection TrackingBoundary = MFSegment.TrackingBoundary;

            // If TrackingBoundary is set to none, will call ExtendFracture without specifying a boundary
            if (TrackingBoundary == GridDirection.None)
#if LOGDFNPOP
                return ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                return ExtendFracture(use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fsIndex, fs, MFSegment, dipsetIndex, ref maxPropLength);
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
            for (int intersecting_fs_index = 0; intersecting_fs_index < NoFractureSets; intersecting_fs_index++)
            {
                if (intersecting_fs_index != fsIndex)
                {
                    Gridblock_FractureSet intersecting_fs = FractureSets[intersecting_fs_index];
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
                for (int intersecting_fs_index = 0; intersecting_fs_index < NoFractureSets; intersecting_fs_index++)
                {
                    if (intersecting_fs_index != fsIndex)
                    {
                        Gridblock_FractureSet intersecting_fs = FractureSets[intersecting_fs_index];
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
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary, fromPreviousTS, ref NoStressShadowInteractions, ref NoIntersections, ref NoPropagatingOut);
#else
                        NeighbourGridblocks[intersectedBoundary].PropagateMFIntoGridblock(MFSegment, fsIndex, oppositeBoundary, intersectionPoint, intersectionRealTime, use_MF_min_length_cutoff, checkStressShadow, TerminateAtGridBoundary);
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
        /// Check whether a specified point (in global XYZ coordinates) lies within the stress shadow of a macrofracture segment from any fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="FSJ_Index">Index number of the fracture set to which the point belongs</param>
        /// <param name="StressShadowHalfWidthsIJ">Reference to a nested list of stress shadow half widths for each dip set in each fracture set, as seen by this fracture - if this is null, a new list will be created</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        private bool checkInMFStressShadow(PointXYZ point, int FSJ_Index, ref List<List<double>> StressShadowHalfWidthsIJ)
        {
            // Check the specified fracture set number lies within the range of fracture sets
            // Otherwise return false
            if ((FSJ_Index < 0) || (FSJ_Index >= NoFractureSets))
                return false;

            // Get a handle to the fracture set to which the specified point belongs (set J)
            Gridblock_FractureSet FSJ = FractureSets[FSJ_Index];

            // Create a new list for stress shadow half-widths if one does not already exist
            if (StressShadowHalfWidthsIJ == null)
                StressShadowHalfWidthsIJ = new List<List<double>>();

            // Loop through all the fracture sets
            for (int FSI_Index = 0; FSI_Index < NoFractureSets; FSI_Index++)
            {
                // Get a handle to fracture set I
                Gridblock_FractureSet FSI = FractureSets[FSI_Index];

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
        /// Check whether a specified point (in global XYZ coordinates) lies within the stress shadow of a macrofracture segment from any fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="FSJ_Index">Index number of the fracture set to which the point belongs</param>
        /// <param name="FSJ_DipSet_Index">Index number of the fracture dip set to which the point belongs</param>
        /// <param name="StressShadowHalfWidthsIJ">Reference to a nested list of stress shadow half widths for each dip set in each fracture set, as seen by this fracture - if this is null, a new list will be created</param>
        /// <param name="StressShadowHalfWidthsJI">Reference to a nested list of stress shadow half widths this fracture, as seen by every fracture set - if this is null, a new list will be created</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        private bool checkInMFExclusionZone(PointXYZ point, int FSJ_Index, int FSJ_DipSet_Index, ref List<List<double>> StressShadowHalfWidthsIJ, ref List<List<double>> StressShadowHalfWidthsJI)
        {
            // Check the specified fracture set number lies within the range of fracture sets
            // Otherwise return false
            if ((FSJ_Index < 0) || (FSJ_Index >= NoFractureSets))
                return false;

            // Get a handle to the fracture set to which the specified point belongs (set J)
            Gridblock_FractureSet FSJ = FractureSets[FSJ_Index];

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
            for (int FSI_Index = 0; FSI_Index < NoFractureSets; FSI_Index++)
            {
                // Get a handle to fracture set I
                Gridblock_FractureSet FSI = FractureSets[FSI_Index];

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
                double cosIJ = Math.Abs(Math.Cos(FSI.Strike - FSJ.Strike));

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
            foreach (Gridblock_FractureSet fs in FractureSets)
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
            foreach (Gridblock_FractureSet fs in FractureSets)
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
                case GridDirection.None:
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
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, reset the number of fracture sets and create new sets each containing two dip sets (Mode 1 and Mode 2)
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public void resetFractures(int NoFractureSets_in, double B_in, double c_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in)
        {
            NoFractureSets = NoFractureSets_in;
            resetFractures(B_in, c_in, BiazimuthalConjugate_in, IncludeReverseFractures_in);
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, and create new fracture sets each containing two dip sets (Mode 1 and Mode 2)
        /// </summary>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public void resetFractures(double B_in, double c_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in)
        {
            // Clear all existing data
            ClearFractureData();

            // Create new fracture sets
            for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
            {
                double strike = Hmin_azimuth + (Math.PI / 2) + (Math.PI * ((double)fs_index / (double)NoFractureSets));
                Gridblock_FractureSet new_FractureSet = new Gridblock_FractureSet(this, strike, B_in, c_in, BiazimuthalConjugate_in, IncludeReverseFractures_in);
                FractureSets.Add(new_FractureSet);
            }

            // Repopulate the macrofracture termination array
            for (int I = 0; I < NoFractureSets; I++)
                for (int J = 0; J < NoFractureSets; J++)
                {
                    int NoDipsetsJ = FractureSets[J].FractureDipSets.Count;
                    MFTerminations[I, J] = new double[NoDipsetsJ];
                    for (int fdsJ = 0; fdsJ < NoDipsetsJ; fdsJ++)
                        MFTerminations[I, J][fdsJ] = 0;
                }
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, reset the number of fracture sets and create new sets each containing only one dip set of specified mode
        /// </summary>
        /// <param name="NoFractureSets_in">Number of fracture sets: set to 2 for two orthogonal sets perpendicular to ehmin and ehmax</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="FractureMode_in">Fracture mode; Fracture sets will contain only 1 dip set of specified mode</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public void resetFractures(int NoFractureSets_in, double B_in, double c_in, FractureMode FractureMode_in, bool IncludeReverseFractures_in)
        {
            NoFractureSets = NoFractureSets_in;
            resetFractures(B_in, c_in, FractureMode_in, IncludeReverseFractures_in);
        }
        /// <summary>
        /// Remove all fractures, explicit and implicit, reset the stress and strain tensors, and create new fracture sets each containing only one dip set of specified mode
        /// </summary>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="FractureMode_in">Fracture mode; Fracture sets will contain only 1 dip set of specified mode</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures; if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public void resetFractures(double B_in, double c_in, FractureMode FractureMode_in, bool IncludeReverseFractures_in)
        {
            // Clear all existing data
            ClearFractureData();

            // Fracture dip is vertical for Mode 1 or Mode 3 fractures, inclined (dependent on friction coefficient) for Mode 2 fractures
            double opt_dip = Math.PI / 2;
            if (FractureMode_in == FractureMode.Mode2)
                opt_dip = ((Math.PI / 2) + Math.Atan(MechProps.MuFr)) / 2;

            // Create new fracture sets
            for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
            {
                double strike = Hmin_azimuth + (Math.PI / 2) + (Math.PI * ((double)fs_index / (double)NoFractureSets));
                Gridblock_FractureSet new_FractureSet = new Gridblock_FractureSet(this, strike, FractureMode_in, opt_dip, B_in, c_in, IncludeReverseFractures_in);
                FractureSets.Add(new_FractureSet);
            }

            // Repopulate the macrofracture termination array
            for (int I = 0; I < NoFractureSets; I++)
                for (int J = 0; J < NoFractureSets; J++)
                {
                    int NoDipsetsJ = FractureSets[J].FractureDipSets.Count;
                    MFTerminations[I, J] = new double[NoDipsetsJ];
                    for (int fdsJ = 0; fdsJ < NoDipsetsJ; fdsJ++)
                        MFTerminations[I, J][fdsJ] = 0;
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
            FractureSets.Clear();

            // Reset the azimuthal and strike-slip shear stress shadow multiplier arrays, and populate with default values
            FaaIJ = new double[NoFractureSets, NoFractureSets];
            FasIJ = new double[NoFractureSets, NoFractureSets];
            for (int I = 0; I < NoFractureSets; I++)
                for (int J = 0; J < NoFractureSets; J++)
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

            // Reset the macrofracture termination array
            // NB we cannot populate this until we have regenerated the fracture sets
            MFTerminations = new double[NoFractureSets, NoFractureSets][];
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
        /// <param name="thickness_in">Layer thickness at time of deformation (m)</param>
        /// <param name="depth_in">Depth at time of deformation (m)</param>
        public GridblockConfiguration(double thickness_in, double depth_in) : this(thickness_in, depth_in, 2)
        {
            // Defaults:

            // Set the number of fracture sets to 2, for two orthogonal sets perpendicular to ehmin and ehmax
        }
        /// <summary>
        /// Constructor: specify layer thickness and depth at the start of deformation, and the number of fracture sets, but create empty fracture sets
        /// </summary>
        /// <param name="InitialThickness_in">Layer thickness at the start of deformation (m)</param>
        /// <param name="InitialDepth_in">Depth at the start of deformation (m)</param>
        /// <param name="NoFractureSets">Number of fracture sets</param>
        public GridblockConfiguration(double InitialThickness_in, double InitialDepth_in, int NoFractureSets_in)
        {
            // Set layer thickness and depth at the start of deformation
            SetInitialThicknessAndDepth(InitialThickness_in, InitialDepth_in);

            // Create a dictionary of adjacent gridblocks and fill with null references (except for reference to this gridblock)
            NeighbourGridblocks = new Dictionary<GridDirection, GridblockConfiguration>();
            NeighbourGridblocks.Add(GridDirection.N, null);
            NeighbourGridblocks.Add(GridDirection.E, null);
            NeighbourGridblocks.Add(GridDirection.S, null);
            NeighbourGridblocks.Add(GridDirection.W, null);
            NeighbourGridblocks.Add(GridDirection.None, this);

            // Set the flag to search neighbouring gridblocks for stress shadow interaction to false
            searchNeighbouringGridblocks_Automatic = false;

            // Create default objects for mechanical properties, stress and strain state and propagation control
            MechProps = new MechanicalProperties(this);
            StressStrain = new StressStrainState(this);
            PropControl = new PropagationControl();

            // Set the number of fracture sets (minimum 0)
            if (NoFractureSets_in < 0)
                NoFractureSets_in = 0;
            NoFractureSets = NoFractureSets_in;

            // Create empty fracture sets
            FractureSets = new List<Gridblock_FractureSet>();
            for (int fs_index = 0; fs_index < NoFractureSets; fs_index++)
            {
                Gridblock_FractureSet new_FractureSet = new Gridblock_FractureSet(this);
                FractureSets.Add(new_FractureSet);
            }

            // Create the azimuthal and strike-slip shear stress shadow multiplier arrays
            FaaIJ = new double[NoFractureSets, NoFractureSets];
            FasIJ = new double[NoFractureSets, NoFractureSets];
            for (int I = 0; I < NoFractureSets; I++)
                for (int J = 0; J < NoFractureSets; J++)
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

            // Create the macrofracture termination array
            MFTerminations = new double[NoFractureSets, NoFractureSets][];
            for (int I = 0; I < NoFractureSets; I++)
                for (int J = 0; J < NoFractureSets; J++)
                {
                    int NoDipsetsJ = FractureSets[J].FractureDipSets.Count;
                    MFTerminations[I, J] = new double[NoDipsetsJ];
                    for (int fdsJ = 0; fdsJ < NoDipsetsJ; fdsJ++)
                        MFTerminations[I, J][fdsJ] = 0;
                }

            // Create an empty list for the references to all macrofracture segments
            MacrofractureSegments = new List<MacrofractureSegmentHolder>();

            // Create a list of timestep end times and add a zero value to it
            TimestepEndTimes = new List<double>();
            TimestepEndTimes.Add(0d);

            // Set the pointers to the last calculated timesteps for implicit and explicit data to timestep 0
            CurrentImplicitTimestep = 0;
            CurrentExplicitTimestep = 0;
        }
    }
}
