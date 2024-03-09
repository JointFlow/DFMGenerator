using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for fracture intersection angle: Orthogonal_IPlus for +90deg, Orthogonal_IMinus for -90deg, Parallel for 0deg, Oblique for anything else
    /// </summary>
    public enum IntersectionAngle { Orthogonal_Plus, Orthogonal_Minus, Oblique, Parallel }

    /// <summary>
    /// Representation of a fracture set in a single gridblock
    /// </summary>
    class Gridblock_FractureSet
    {
        // References to external objects
        /// <summary>
        /// Reference to parent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;

        // Control data for calculating fracture spacing distribution coefficients
        /// <summary>
        /// Number of iterations used to calculate BBMean and CCn+1 within each timestep
        /// </summary>
        private const int NoCCIterations = 2;
        /// <summary>
        /// Overshoot factor to use when reducing AA1 so that the total volume of spacings where x > W1 is slightly less than the available volume
        /// Set this to slightly greater than 1 to avoid infinite BB and CC values
        /// This is for output purposes only; the C Sharp code will still function correctly with infinite BB and CC values (Excess_Volume_Multiplier = 1)
        /// </summary>
        private const double Excess_Volume_Multiplier = 1; //1.000001;
        /// <summary>
        /// Flag to allow CC values to become negative
        /// </summary>
        private bool AllowNegativeCC = true;

        // Local copies centrepoints of corner pillars in fracture set (IJK) coordinates
        // NB these are stored as hard variables to reduce calculation time when propagating fractures, so must be recalculated whenever fracture orientation or gridblock geometry is reset
        /// <summary>
        /// Local IJK coordinates of the southwest corner of the vertical centre of the gridblock
        /// </summary>
        public PointIJK SWMidPoint { get; private set; }
        /// <summary>
        /// Local IJK coordinates of the northwest corner of the vertical centre of the gridblock
        /// </summary>
        public PointIJK NWMidPoint { get; private set; }
        /// <summary>
        /// Local IJK coordinates of the northeast corner of the vertical centre of the gridblock
        /// </summary>
        public PointIJK NEMidPoint { get; private set; }
        /// <summary>
        /// Local IJK coordinates of the southeast corner of the vertical centre of the gridblock
        /// </summary>
        public PointIJK SEMidPoint { get; private set; }
        /// <summary>
        /// Minimum I coordinate of all cornerpoints
        /// </summary>
        private double MinI { get; set; }
        /// <summary>
        /// Maximum I coordinate of all cornerpoints
        /// </summary>
        private double MaxI { get; set; }
        /// <summary>
        /// Minimum J coordinate of all cornerpoints
        /// </summary>
        private double MinJ { get; set; }
        /// <summary>
        /// Maximum J coordinate of all cornerpoints
        /// </summary>
        private double MaxJ { get; set; }
        /// <summary>
        /// Set the values of the local copies centrepoints of corner pillars in fracture set (IJK) coordinates
        /// </summary>
        public void setCornerPoints()
        {
            // Calculate centrepoints of corner pillars in fracture set (IJK) coordinates
            if (gbc.checkCornerpointsDefined()) // We can only do this if the cornerpoints of the parent gridblock object have been defined
            {
                SWMidPoint = convertXYZtoIJK(gbc.getSWMidPoint());
                NWMidPoint = convertXYZtoIJK(gbc.getNWMidPoint());
                NEMidPoint = convertXYZtoIJK(gbc.getNEMidPoint());
                SEMidPoint = convertXYZtoIJK(gbc.getSEMidPoint());

                // Find the minimum and maximum I values
                MinI = SWMidPoint.I;
                MaxI = SWMidPoint.I;
                MinJ = SWMidPoint.J;
                MaxJ = SWMidPoint.J;
                foreach (PointIJK cornerPoint in new PointIJK[3] { NWMidPoint, NEMidPoint, SEMidPoint })
                {
                    if (MinI > cornerPoint.I) MinI = cornerPoint.I;
                    if (MaxI < cornerPoint.I) MaxI = cornerPoint.I;
                    if (MinJ > cornerPoint.J) MinJ = cornerPoint.J;
                    if (MaxJ < cornerPoint.J) MaxJ = cornerPoint.J;
                }
            }
        }

        // Geometric data and functions
        /// <summary>
        /// Strike of fracture set (radians); positive in the IPlus direction
        /// </summary>
        private double strike;
        /// <summary>
        /// Horizontal unit vector in direction of fracture strike
        /// </summary>
        private VectorXYZ strikeVector;
        /// <summary>
        /// Horizontal unit vector in direction of fracture azimuth
        /// </summary>
        private VectorXYZ azimuthVector;
        /// <summary>
        /// Strike of fracture set (radians); positive in the IPlus direction
        /// </summary>
        public double Strike { get { return strike; } private set { while (value >= (2 * Math.PI)) value -= (2 * Math.PI); while (value < 0) value += (2 * Math.PI); strike = value; strikeVector = VectorXYZ.GetLineVector(value, 0); azimuthVector = VectorXYZ.GetLineVector(value + (Math.PI / 2), 0); setCornerPoints(); } }
        /// <summary>
        /// Azimuth of fracture set (radians); positive in the JPlus direction
        /// </summary>
        public double Azimuth { get { double azimuth = strike + (Math.PI / 2); while (azimuth >= (2 * Math.PI)) azimuth -= (2 * Math.PI); return azimuth; } }
        /// <summary>
        /// Horizontal unit vector in direction of fracture strike
        /// </summary>
        public VectorXYZ StrikeVector { get { return new VectorXYZ(strikeVector); } }
        /// <summary>
        /// Horizontal unit vector in direction of fracture azimuth
        /// </summary>
        public VectorXYZ AzimuthVector { get { return new VectorXYZ(azimuthVector); } }
        /// <summary>
        /// Get the fracture propagation azimuth for a fracture propagating in a specified direction
        /// </summary>
        /// <param name="propDir">Direction of fracture propagation</param>
        /// <returns></returns>
        public double getPropagationAzimuth(PropagationDirection propDir)
        {
            // Fracture propagation azimuth will be equal to fracture strike if propagating in the IPlus direction and the opposite if propagating in the IMinus direction
            double propAzi = Strike;
            if (propDir == PropagationDirection.IMinus)
            {
                propAzi += Math.PI;
                // Reset propagation azimuth to lie within the range 0 to 2pi if it is outside that range
                while (propAzi >= (2 * Math.PI)) propAzi -= (2 * Math.PI);
            }

            return propAzi;
        }
        /// <summary>
        /// StressDistribution object describing the spatial distribution of the fractures: EvenlyDistributedStress gives a random fracture distribution, StressShadow and DuctileBoundary gives a regular spacing
        /// </summary>
        public StressDistribution FractureDistribution { get; set; }
        /// <summary>
        /// Calculate the I coordinate (relative to fracture strike) of a point in grid (XYZ) coordinates
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>I coordinate of input point</returns>
        public double getICoordinate(PointXYZ point_in)
        {
            return (VectorXYZ.Sin_trim(strike) * point_in.X) + (VectorXYZ.Cos_trim(strike) * point_in.Y);
        }
        /// <summary>
        /// Calculate the J coordinate (relative to fracture dip direction) of a point in grid (XYZ) coordinates
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>J coordinate of input point</returns>
        public double getJCoordinate(PointXYZ point_in)
        {
            return (VectorXYZ.Cos_trim(strike) * point_in.X) - (VectorXYZ.Sin_trim(strike) * point_in.Y);
        }
        /// <summary>
        /// Convert a point in grid (XYZ) coordinates to a point in fracture set (IJK) coordinates
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>New point in IJK coordinates</returns>
        public PointIJK convertXYZtoIJK(PointXYZ point_in)
        {
            // Get sin and cosine of fracture strike (i.e. I+ axis)
            double sinstrike = VectorXYZ.Sin_trim(strike);
            double cosstrike = VectorXYZ.Cos_trim(strike);

            // Convert X and Y coordinates
            double I = (sinstrike * point_in.X) + (cosstrike * point_in.Y);
            double J = (cosstrike * point_in.X) - (sinstrike * point_in.Y);

            // Get point_z of centre of gridblock at point x,y
            double K = point_in.Z - gbc.getCentreZ(point_in.X, point_in.Y);

            // Create an IJK point and return it
            PointIJK output = new PointIJK(I, J, K);
            return output;
        }
        /// <summary>
        /// Convert a point in fracture set (IJK) coordinates to a point in grid (XYZ) coordinates
        /// </summary>
        /// <param name="point_in">Input point in IJK coordinates</param>
        /// <returns>New point in XYZ coordinates</returns>
        public PointXYZ convertIJKtoXYZ(PointIJK point_in)
        {
            // Get sin and cosine of fracture strike (i.e. I+ axis)
            double sinstrike = VectorXYZ.Sin_trim(strike);
            double cosstrike = VectorXYZ.Cos_trim(strike);

            // Convert X and Y coordinates
            double X = (sinstrike * point_in.I) + (cosstrike * point_in.J);
            double Y = (cosstrike * point_in.I) - (sinstrike * point_in.J);

            // Get point_z of centre of gridblock at point x,y
            double Z = point_in.K + gbc.getCentreZ(X, Y);

            // Create an IJK point and return it
            PointXYZ output = new PointXYZ(X, Y, Z);
            return output;
        }
        /// <summary>
        /// Get the true vertical thickness of the layer at a specified point in grid (XYZ) coordinates
        /// </summary>
        /// <param name="point_in">Input point in XYZ coordinates</param>
        /// <returns>True vertical thickness of the gridblock (m)</returns>
        public double getTVTAtPoint(PointXYZ point_in)
        {
            // Return point_t of gridblock at point x,y
            return gbc.getTVT(point_in.X, point_in.Y);
        }
        /// <summary>
        /// Get the true vertical thickness of the layer at a specified point in fracture set (IJK) coordinates
        /// </summary>
        /// <param name="point_in">Input point in IJK coordinates</param>
        /// <returns>point_t of the gridblock (m)</returns>
        public double getTVTAtPoint(PointIJK point_in)
        {
            // Get sin and cosine of fracture strike (i.e. I+ axis)
            double sinstrike = VectorXYZ.Sin_trim(strike);
            double cosstrike = VectorXYZ.Cos_trim(strike);

            // Convert X and Y coordinates
            double X = (sinstrike * point_in.I) + (cosstrike * point_in.J);
            double Y = (cosstrike * point_in.I) - (sinstrike * point_in.J);

            // Return point_t of gridblock at point x,y
            return gbc.getTVT(X, Y);
        }
        /// <summary>
        /// Get the I and J coordinates of the endpoints of a specified boundary segment
        /// </summary>
        /// <param name="boundary">Boundary segment for which endpoints are required</param>
        /// <param name="boundaryleftI">Reference parameter for the I coordinate of the left hand boundary endpoint</param>
        /// <param name="boundaryleftJ">Reference parameter for the J coordinate of the left hand boundary endpoint</param>
        /// <param name="boundaryrightI">Reference parameter for the I coordinate of the right hand boundary endpoint</param>
        /// <param name="boundaryrightJ">Reference parameter for the J coordinate of the right hand boundary endpoint</param>
        public void getBoundaryEndPoints(GridDirection boundary, out double boundaryleftI, out double boundaryleftJ, out double boundaryrightI, out double boundaryrightJ)
        {
            switch (boundary)
            {
                case GridDirection.N:
                    {
                        boundaryleftI = NWMidPoint.I;
                        boundaryleftJ = NWMidPoint.J;
                        boundaryrightI = NEMidPoint.I;
                        boundaryrightJ = NEMidPoint.J;
                        return;
                    }
                case GridDirection.E:
                    {
                        boundaryleftI = NEMidPoint.I;
                        boundaryleftJ = NEMidPoint.J;
                        boundaryrightI = SEMidPoint.I;
                        boundaryrightJ = SEMidPoint.J;
                        return;
                    }
                case GridDirection.S:
                    {
                        boundaryleftI = SEMidPoint.I;
                        boundaryleftJ = SEMidPoint.J;
                        boundaryrightI = SWMidPoint.I;
                        boundaryrightJ = SWMidPoint.J;
                        return;
                    }
                case GridDirection.W:
                    {
                        boundaryleftI = SWMidPoint.I;
                        boundaryleftJ = SWMidPoint.J;
                        boundaryrightI = NWMidPoint.I;
                        boundaryrightJ = NWMidPoint.J;
                        return;
                    }
                default:
                    {
                        boundaryleftI = 0;
                        boundaryleftJ = 0;
                        boundaryrightI = 0;
                        boundaryrightJ = 0;
                        return;
                    }
            }
        }
        /// <summary>
        /// Get the cornerpoints of a specified boundary as PointXYZ objects
        /// </summary>
        /// <param name="boundary">Boundary for which cornerpoint are required</param>
        /// <param name="UpperLeftCorner">Reference parameter for PointXYZ object representing the upper left cornerpoint of the specified boundary</param>
        /// <param name="UpperRightCorner">Reference parameter for PointXYZ object representing the upper right cornerpoint of the specified boundary</param>
        /// <param name="LowerLeftCorner">Reference parameter for PointXYZ object representing the lower left cornerpoint of the specified boundary</param>
        /// <param name="LowerRightCorner">Reference parameter for PointXYZ object representing the lower right cornerpoint of the specified boundary</param>
        public void getBoundaryCorners(GridDirection boundary, out PointXYZ UpperLeftCorner, out PointXYZ UpperRightCorner, out PointXYZ LowerLeftCorner, out PointXYZ LowerRightCorner)
        {
            gbc.getBoundaryCornerpoints(boundary, out UpperLeftCorner, out UpperRightCorner, out LowerLeftCorner, out LowerRightCorner);
        }
        /// <summary>
        /// Get the I coordinate of the intersection between a fracture with a specified J coordinate, and a specified boundary segment
        /// Note that the fracture is considered infinite in either direction but it must intersect the boundary segment between the gridblock cornerpoints, otherwise the function will return NaN
        /// </summary>
        /// <param name="intersection_j">J coordinate of the fracture</param>
        /// <param name="boundary">Boundary with which to calculate the intersection</param>
        /// <param name="propDir">Direction of propagation, used to determine whether whether the fracture crosses out from or into the gridblock</param>
        /// <param name="crossesOutward">Output flag to determine whether the fracture crosses out from the gridblock (true) or into the gridblock (false)</param>
        /// <returns>The I coordinate of the intersection point; NaN if the fracture does not intersect the specified boundary segment when extended to infinity</returns>
        private double getBoundaryIntersection(double intersection_j, GridDirection boundary, PropagationDirection propDir, out bool crossesOutward)
        {
            // By default set crossesOutward flag to false
            crossesOutward = false;

            // Get the coordinates for the fracture and boundary cornerpoints relative to the direction of the fracture (i)
            double leftCorner_i, leftCorner_j, rightCorner_i, rightCorner_j;
            getBoundaryEndPoints(boundary, out leftCorner_i, out leftCorner_j, out rightCorner_i, out rightCorner_j);

            // Determine whether the fracture intersects the boundary between the cornerpoints; if so, determine which direction it is crossing, if not return NaN
            // Also return NaN if the boundary is parallel to the fracture (i.e. leftCorner_j == rightCorner_j)
            if (leftCorner_j == rightCorner_j) // If leftCorner_j == rightCorner_j the line is parallel to the line
                return double.NaN;
            else if ((leftCorner_j <= intersection_j) && (intersection_j <= rightCorner_j)) // If leftCorner_j < rightCorner_j the fracture is crossing outwards
                crossesOutward = true;
            else if ((leftCorner_j >= intersection_j) && (intersection_j >= rightCorner_j)) // If leftCorner_j > rightCorner_j the fracture is crossing inwards
                crossesOutward = false;
            else // If intersection_j does not lie between leftCorner_j and rightCorner_j the fracture does not intersect the boundary segment between the cornerpoints 
                return double.NaN;
            // If the fracture is propagating in the IMinus direction, reverse the flag for whether the fracture crosses out from or into the gridblock
            if (propDir == PropagationDirection.IMinus)
                crossesOutward = !crossesOutward;

            // Calculate the position of the intersection and return it
            // This is valid whether rightCorner_j > leftCorner_j or leftCorner_j > rightCorner_j
            double relativeIntersectionPoint = (intersection_j - leftCorner_j) / (rightCorner_j - leftCorner_j);
            return (leftCorner_i * (1 - relativeIntersectionPoint)) + (rightCorner_i * relativeIntersectionPoint);
        }
        /// <summary>
        /// Create a random fracture nucleation position within the gridblock
        /// </summary>
        /// <returns></returns>
        public PointIJK getRandomNucleationPoint()
        {
            return getRandomNucleationPoint(-1);
        }
        /// <summary>
        /// Create a random fracture nucleation position within the gridblock - the depth can either be random or specified relative to the gridblock
        /// </summary>
        /// <param name="FractureNucleationPosition_w">Specified depth of nucleation relative to the gridblock: set to 0 for the base of the layer, 0.5 for the centre of the layer, 1 for the top of the layer, and -1 for a random depth within the layer</param>
        /// <returns>A random point within the layer in IJK coordinates</returns>
        public PointIJK getRandomNucleationPoint(double FractureNucleationPosition_w)
        {
            // Get reference to the random number generator
            Random randGen = gbc.RandGen;

            // Get range of allowable I and J values
            double IRange = MaxI - MinI;
            double JRange = MaxJ - MinJ;

            // Set the default location to the SW cornerpoint
            double nucleationPointI = SWMidPoint.I;
            double nucleationPointJ = SWMidPoint.J;

            // Find a random pair of I and J coordinates that lie within the gridblock
            // If this cannot be done within 1000 attempts, it will revert to the default - this way the function will always return a point
            for (int MaxTries = 0; MaxTries < 1000; MaxTries++)
            {
                // Select random I and J coordinates within the range of allowable values, and see if they lie within the gridblock
                double pointI = MinI + (IRange * randGen.NextDouble());
                double pointJ = MinJ + (JRange * randGen.NextDouble());

                // Check whether the point lies within the gridblock, and whether an infinite extension of the fracture will cross at least one gridblock boundary outwards in each direction
                int IPlusBoundaryIntersections = 0;
                int IMinusBoundaryIntersections = 0;
                foreach (GridDirection boundary in new GridDirection[4] { GridDirection.N, GridDirection.E, GridDirection.S, GridDirection.W })
                {
                    bool crossesOutwards;
                    double boundaryIntersection = getBoundaryIntersection(pointJ, boundary, PropagationDirection.IPlus, out crossesOutwards);
                    if (boundaryIntersection >= pointI)
                    {
                        if (crossesOutwards)
                            IPlusBoundaryIntersections++;
                        else
                            IPlusBoundaryIntersections--;
                    }
                    if (boundaryIntersection < pointI)
                    {
                        // For boundary intersections in the IMinus direction, we must reverse the outwards direction
                        if (!crossesOutwards)
                            IMinusBoundaryIntersections++;
                        else
                            IMinusBoundaryIntersections--;
                    }
                }

                if ((IPlusBoundaryIntersections == 1) && (IMinusBoundaryIntersections == 1))
                {
                    nucleationPointI = pointI;
                    nucleationPointJ = pointJ;
                    break;
                }
            }

            // Create a PointIJK object at the specificied I and J coordinates with K=0
            PointIJK nucleationPoint = new PointIJK(nucleationPointI, nucleationPointJ, 0);

            // If the relative nucleation depth is 0.5, then K=0 and there is no need for further calculation
            // Otherwise we will need to calculate the K coordinate
            if (FractureNucleationPosition_w != 0.5)
            {
                // If the specified relative nucleation depth is negative or no relative nucleation depth is specified, generate a random value
                if (!(FractureNucleationPosition_w >= 0))
                    FractureNucleationPosition_w = randGen.NextDouble();
                double TVT = getTVTAtPoint(nucleationPoint);
                nucleationPoint.K = TVT * (FractureNucleationPosition_w - 0.5);
            }

            // Return the new point
            return nucleationPoint;
        }

        // Incremental strain acting on the fractures 
        /// <summary>
        /// Incremental azimuthal strain acting on the fractures (i.e. component of strain rate tensor on the fracture plane parallel to fracture azimuth)
        /// </summary>
        private double eaad;
        /// <summary>
        /// Incremental strike-parallel shear strain acting on the fractures (i.e. component of strain rate tensor on the fracture plane parallel to fracture strike)
        /// </summary>
        private double easd;
        /// <summary>
        /// Incremental strain parallel to fracture strike (i.e. component of strain rate tensor on a plane perpendicular to fracture strike)
        /// </summary>
        private double essd;
        /// <summary>
        /// Ratio of incremental azimuthal strain to total incremental horizontal strain on the fracture, given by eaa^2 / (eaa^2 + eas^2)
        /// </summary>
        public double eaa2d_eh2d { get; private set; }
        /// <summary>
        /// Ratio of incremental azimuthal strain x strike-slip shear strain to total incremental horizontal strain on the fracture, given by eaa*eas / (eaa^2 + eas^2)
        /// </summary>
        public double eaaasd_eh2d { get; private set; }
        /// <summary>
        /// Ratio of incremental strike-parallel shear strain to total incremental horizontal strain on the fracture, given by eas^2 / (eaa^2 + eas^2)
        /// </summary>
        public double eas2d_eh2d { get { return 1 - eaa2d_eh2d; } }
        /// <summary>
        /// Calculate the azimuthal stress shadow multiplier between this fracture set (I) and another fracture set J
        /// </summary>
        /// <param name="J">Fracture set for which to calculate azimuthal stress shadow multiplier</param>
        /// <returns>Azimuthal stress shadow multiplier between this fracture set (I) and fracture set J</returns>
        public double getFaaIJ(Gridblock_FractureSet J)
        {
            double angle_IJ = Azimuth - J.Azimuth;
            double cos2IJ = Math.Pow(VectorXYZ.Cos_trim(angle_IJ), 2);
            double sincosIJ = VectorXYZ.Sin_trim(angle_IJ) * VectorXYZ.Cos_trim(angle_IJ);
            double ehh2I = Math.Pow(eaad, 2) + Math.Pow(easd, 2);
            double ehh2J = Math.Pow(J.eaad, 2) + Math.Pow(J.easd, 2);
            double ehh_factor = (ehh2J > 0 ? ehh2I / ehh2J : 0);
            double FaaIJ = (cos2IJ + (eaad == 0 ? 0 : ((easd / eaad) * sincosIJ))) * ehh_factor;
            // In some circumstances, the fracture I may actually enhance the stress on fracture, giving a negative stress shadow multiplier; in this case we will set it to 0 (no stress shadow)
            if (FaaIJ < 0)
                FaaIJ = 0;
            return FaaIJ;
        }
        /// <summary>
        /// Calculate the strike-slip shear stress shadow multiplier between this fracture set (I) and another fracture set J
        /// </summary>
        /// <param name="J">Fracture set for which to calculate strike-slip shear stress shadow multiplier</param>
        /// <returns>Strike-slip shear stress shadow multiplier between this fracture set (I) and fracture set J</returns>
        public double getFasIJ(Gridblock_FractureSet J)
        {
            double angle_IJ = Azimuth - J.Azimuth;
            double sincosIJ = VectorXYZ.Sin_trim(angle_IJ) * VectorXYZ.Cos_trim(angle_IJ);
            double ehh2I = Math.Pow(eaad, 2) + Math.Pow(easd, 2);
            double ehh2J = Math.Pow(J.eaad, 2) + Math.Pow(J.easd, 2);
            double ehh_factor = (ehh2J > 0 ? ehh2I / ehh2J : 0);
            double FasIJ = (1 + (easd == 0 ? 0 : (((eaad + essd) / easd) * sincosIJ))) * ehh_factor;
            // In some circumstances, the fracture I may actually enhance the stress on fracture, giving a negative stress shadow multiplier; in this case we will set it to 0 (no stress shadow)
            if (FasIJ < 0)
                FasIJ = 0;
            return FasIJ;
        }

        /// <summary>
        /// Recalculate the azimuthal and horizontal shear strains acting on the fractures, for a specified strain or strain rate tensor
        /// </summary>
        /// <param name="AppliedStrainTensor">Current strain or strain rate tensor</param>
        public void RecalculateHorizontalStrainRatios(Tensor2S AppliedStrainTensor)
        {
            VectorXYZ horizontalStrainOnFracture = AppliedStrainTensor * azimuthVector;
            VectorXYZ horizontalStrainAlongStrike = AppliedStrainTensor * strikeVector;
            eaad = azimuthVector & horizontalStrainOnFracture;
            easd = strikeVector & horizontalStrainOnFracture;
            essd = strikeVector & horizontalStrainAlongStrike;
            // Set the strain ratios to zero if they are small - this will avoid rounding errors when calculating the FaaIJ and FasIJ multipliers
            double emax = eaad + easd + essd;
            if ((float)(emax + eaad) == (float)emax)
                eaad = 0;
            if ((float)(emax + easd) == (float)emax)
                easd = 0;
            if ((float)(emax + essd) == (float)emax)
                essd = 0;
            double eaa_squared = Math.Pow(eaad, 2);
            double eas_squared = Math.Pow(easd, 2);
            double ea_squared = eaa_squared + eas_squared;
            eaa2d_eh2d = (ea_squared > 0 ? eaa_squared / ea_squared : 1);
            eaaasd_eh2d = (ea_squared > 0 ? (eaad * easd) / ea_squared : 0);

            // Recalculate the strain ratios for non-biazimuthal dipsets individually
            foreach (FractureDipSet fds in FractureDipSets)
                if (!fds.BiazimuthalConjugate)
                    fds.RecalculateStrainRatios(AppliedStrainTensor);
        }

        // Fracture data
        /// <summary>
        /// List of fracture dipsets
        /// </summary>
        public List<FractureDipSet> FractureDipSets;
        /// <summary>
        /// Private copy of the list of fracture dipsets sorted by stress shadow width (in reverse order) 
        /// This list contains references to the same FractureDipSet objects as in the public FractureDipSets list, but in order of stress shadow width rather than fracture mode 
        /// This is used in calculating the cumulative macrofracture spacing distribution
        /// </summary>
        private List<FractureDipSet> FractureDipSets_SortedByW;
        /// <summary>
        /// List of discrete microfracture objects in IJK coordinates - represents microfracture component of local gridblock DFN
        /// </summary>
        public List<MicrofractureIJK> LocalDFNMicrofractures;
        /// <summary>
        /// Lists of discrete macrofracture segments in IJK coordinates propagating in the IMinus and IPlus directions - represent macrofracture component of local gridblock DFN
        /// </summary>
        public Dictionary<PropagationDirection, List<MacrofractureSegmentIJK>> LocalDFNMacrofractureSegments;
        /// <summary>
        /// Variable to hold maximum historic active macrofracture volumetric ratio; used to check if termination criteria are met, and updated when the CheckFractureDeactivation is called
        /// </summary>
        private double max_historic_a_MFP33;
        /// <summary>
        /// Class used to compare FractureDipSet objects by mean stress shadow widths at some specified previous timestep (by default, in the FractureDipSet class they are compared at the present time)
        /// </summary>
        private class FDS_W : IComparable<FDS_W>
        {
            /// <summary>
            /// Reference to a FractureDipSet object
            /// </summary>
            public FractureDipSet DipSet { get; private set; }
            /// <summary>
            /// Mean stress shadow width of the fracture dip set at the specified timestep (may not be the current mean stress shadow width)
            /// </summary>
            private double MeanW;

            // Control and implementation functions
            /// <summary>
            /// Compare FDS_W objects based on MeanW
            /// NB Comparison result is inverted so that the fracture dipsets will be sorted in reverse order of stress shadow width (largest to smallest)
            /// </summary>
            /// <param name="that">FDS_W object to compare with</param>
            /// <returns>Negative if this has the greatest MeanW value, positive if that has the greatest MeanW value, zero if they have the same MeanW value</returns>
            public int CompareTo(FDS_W that)
            {
                return -this.MeanW.CompareTo(that.MeanW);
            }

            // Constructors
            /// <summary>
            /// Constructor: specify the FractureDipSet object and the timestep for which to compare mean stress shadow widths
            /// </summary>
            /// <param name="fds_in"></param>
            /// <param name="tsM_in"></param>
            public FDS_W (FractureDipSet fds_in, int tsM_in)
            {
                DipSet = fds_in;
                MeanW = fds_in.getMeanStressShadowWidth(tsM_in);
            }
        }
        /// <summary>
        /// Sort the fracture sip set objects in the FractureDipSets_SortedByW list in order of mean stress shadow width at some previous timestep, largest to smallest
        /// </summary>
        /// <param name="tsM">Index number of timestep at which to sort the list</param>
        private void FractureDipSets_SortByW(int tsM)
        {
            // Check that the specified timestep to compare has already been calculated
            if (tsM > gbc.CurrentImplicitTimestep)
                tsM = gbc.CurrentImplicitTimestep;

            // Get the total number of dipsets
            int NoDipSets = FractureDipSets_SortedByW.Count;

            // Create a list of FDS_W objects and sort it by MeanW values, largest to smallest
            List<FDS_W> FDSW_list = new List<FDS_W>();
            foreach (FractureDipSet fds in FractureDipSets_SortedByW)
                FDSW_list.Add(new FDS_W(fds, tsM));
            FDSW_list.Sort();

            // Put the new FractureDipSet references back into the FractureDipSets_SortedByW list in the new order
            for (int DipSetNo = 0; DipSetNo < NoDipSets; DipSetNo++)
                FractureDipSets_SortedByW[DipSetNo] = FDSW_list[DipSetNo].DipSet;
        }

        // Functions to get combined fracture set property data

        // Microfracture total population data
        /// <summary>
        /// Total volumetric density of active microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_a_uFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.a_uFP30_total();
            return output;
        }
        /// <summary>
        /// Total volumetric density of static microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_s_uFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.s_uFP30_total();
            return output;
        }
        /// <summary>
        /// Total volumetric density of all microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_T_uFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += (DipSet.a_uFP30_total() + DipSet.s_uFP30_total());
            return output;
        }
        /// <summary>
        /// Total linear density of active microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_a_uFP32_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.a_uFP32_total();
            return output;
        }
        /// <summary>
        /// Total linear density of static microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_s_uFP32_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.s_uFP32_total();
            return output;
        }
        /// <summary>
        /// Total linear density of all microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_T_uFP32_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += (DipSet.a_uFP32_total() + DipSet.s_uFP32_total());
            return output;
        }
        /// <summary>
        /// Total volumetric ratio of active microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_a_uFP33_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.a_uFP33_total();
            return output;
        }
        /// <summary>
        /// Total volumetric ratio of static microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_s_uFP33_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.s_uFP33_total();
            return output;
        }
        /// <summary>
        /// Total volumetric ratio of all microfractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_T_uFP33_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += (DipSet.a_uFP33_total() + DipSet.s_uFP33_total());
            return output;
        }

        // Macrofracture total population data
        /// <summary>
        /// Total volumetric density of active half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_a_MFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.a_MFP30_total();
            return output;
        }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to stress shadow interaction in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_sII_MFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.sII_MFP30_total();
            return output;
        }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to intersection with other fracture sets in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_sIJ_MFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.sIJ_MFP30_total();
            return output;
        }
        /// <summary>
        /// Total volumetric density of all half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_T_MFP30_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += (DipSet.a_MFP30_total() + DipSet.sII_MFP30_total() + DipSet.sIJ_MFP30_total());
            return output;
        }
        /// <summary>
        /// Total linear density of active half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_a_MFP32_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.a_MFP32_total();
            return output;
        }
        /// <summary>
        /// Total linear density of static half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_s_MFP32_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.s_MFP32_total();
            return output;
        }
        /// <summary>
        /// Total linear density of all half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_T_MFP32_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                output += (DipSet.a_MFP32_total() + DipSet.s_MFP32_total());
            return output;
        }
        /// <summary>
        /// Total volumetric ratio of active half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_a_MFP33_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.a_MFP33_total();
            return output;
        }
        /// <summary>
        /// Total volumetric ratio of static half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_s_MFP33_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.s_MFP33_total();
            return output;
        }
        /// <summary>
        /// Total volumetric ratio of all half-macrofractures in all dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_T_MFP33_total()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets) output += (DipSet.a_MFP33_total() + DipSet.s_MFP33_total());
            return output;
        }

        // Porosity and volumetric heave functions
        /// <summary>
        /// Combined dynamic microfracture porosity for all Mode 1 dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_uF_Porosity()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                if (DipSet.Mode == FractureMode.Mode1) output += DipSet.Total_uF_DynamicPorosityHeave();
            return output;
        }
        /// <summary>
        /// Combined dynamic microfracture volumetric heave for all Mode 2 dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_uF_VolumetricHeave()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                if (DipSet.Mode != FractureMode.Mode1) output += DipSet.Total_uF_DynamicPorosityHeave();
            return output;
        }
        /// <summary>
        /// Combined dynamic half-macrofracture porosity for all Mode 1 dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_MF_Porosity()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                if (DipSet.Mode == FractureMode.Mode1) output += (DipSet.Total_MF_DynamicPorosityHeave());
            return output;
        }
        /// <summary>
        /// Combined dynamic half-macrofracture volumetric heave for all Mode 2 dip sets
        /// </summary>
        /// <returns></returns>
        public double combined_MF_VolumetricHeave()
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                if (DipSet.Mode != FractureMode.Mode1) output += (DipSet.Total_MF_DynamicPorosityHeave());
            return output;
        }
        /// <summary>
        /// Combined microfracture porosity for all dip sets, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl"></param>
        /// <returns></returns>
        public double combined_uF_Porosity(FractureApertureType ApertureControl)
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                output += DipSet.Total_uF_Porosity(ApertureControl);
            return output;
        }
        /// <summary>
        /// Combined half-macrofracture porosity for all dip sets, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl"></param>
        /// <returns></returns>
        public double combined_MF_Porosity(FractureApertureType ApertureControl)
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                output += DipSet.Total_MF_Porosity(ApertureControl);
            return output;
        }
        /// <summary>
        /// Total fracture porosity for all fractures in all dip sets, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl"></param>
        /// <returns></returns>
        public double combined_Fracture_Porosity(FractureApertureType ApertureControl)
        {
            double output = 0;
            foreach (FractureDipSet DipSet in FractureDipSets)
                output += DipSet.Total_Fracture_Porosity(ApertureControl);
            return output;
        }

        // Macrofracture stress shadow, fracture spacing and exclusion zone volume
        // Functions to return stress shadow width and volume
        /// <summary>
        /// Combined stress shadow volume for all half-macrofractures
        /// </summary>
        /// <returns></returns>
        public double combined_MF_StressShadowVolume()
        {
            // NB this calculation assumes that stress shadow widths do not change in time, so there is no overlap of stress shadows
            switch (FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows - return 0
                    return 0;
                case StressDistribution.StressShadow:
                    // Calculate the sum of the stress shadow widths for all fracture dip sets
                    double output = 0;
                    foreach (FractureDipSet DipSet in FractureDipSets) output += DipSet.Total_MF_StressShadowVolume();
                    return (output > 1 ? 1 : output);
                case StressDistribution.DuctileBoundary:
                    return 0;
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Mean macrofracture stress shadow width, averaged by fracture length
        /// </summary>
        /// <returns></returns>
        public double average_Mean_MF_StressShadowWidth()
        {
            double totalStressShadowVolume = combined_MF_StressShadowVolume();
            double totalFractureArea = combined_T_MFP32_total();
            return (totalFractureArea > 0 ? totalStressShadowVolume / totalFractureArea : 0);
        }
        /// <summary>
        /// Mean macrofracture stress shadow width, averaged by number of fracture tips meeting specified criteria
        /// </summary>
        /// <param name="propdir">Direction of propagation of fracture tips</param>
        /// <param name="includeAllTips">True to include all tips propagating in specified direction; false to include only static fracture tips deactivated due to stress shadow interaction</param>
        /// <returns></returns>
        public double average_Mean_MF_StressShadowWidth(PropagationDirection propdir, bool includeAllTips)
        {
            switch (FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows - return 0
                    return 0;
                case StressDistribution.StressShadow:
                    double totalStressShadowWidth = 0;
                    double totalNoTips = 0;
                    foreach (FractureDipSet DipSet in FractureDipSets)
                    {
                        double noTips = DipSet.sII_MFP30_total(propdir);
                        if (includeAllTips) noTips += DipSet.a_MFP30_total(propdir) + DipSet.sIJ_MFP30_total(propdir);
                        totalStressShadowWidth += (DipSet.Mean_MF_StressShadowWidth * noTips);
                        totalNoTips += noTips;
                    }
                    return (totalNoTips > 0 ? totalStressShadowWidth / totalNoTips : 0);
                case StressDistribution.DuctileBoundary:
                    // Not yet implemented - return 0
                    return 0;
                default:
                    // Assume no stress shadow interaction as default - return 0
                    return 0;
            }
        }
        // Functions to set stress shadow width
        /// <summary>
        /// Set the new stress shadow widths; if necessary, also recalculate the cumulative macrofracture spacing distribution data
        /// </summary>
        /// <returns>True if the stress shadow width of any dipset has changed, false if the stress shadow widths of all dipsets are unchanged</returns>
        public bool setStressShadowWidthData()
        {
            // Cache the total number of fracture dip sets
            int noDipsets = FractureDipSets.Count;

            // First check to see if any of the stress shadow widths have changed
            bool any_changed_W = false;
            List<bool> changed_W = new List<bool>();
            for (int DipsetNo_Q = 0; DipsetNo_Q < noDipsets; DipsetNo_Q++)
            {
                // Get reference to dipset Q
                FractureDipSet Dipset_Q = FractureDipSets_SortedByW[DipsetNo_Q];

                if (Dipset_Q.checkStressShadowWidthChange())
                {
                    any_changed_W = true;
                    changed_W.Add(true);
                }
                else
                {
                    changed_W.Add(false);
                }
            }

            // If any of the stress shadow widths have changed we will need to recalculate the spacing distribution data for all dipsets, as well as setting the new widths
            if (any_changed_W)
            {
                // Get the new AA values for each dipset and cache them locally
                // The new AA values will be the total cumulative spacing density for the new stress shadow width
                // They will therefore only change if the stress shadow width has changed; otherwise we can use the old value
                // NB we must calculate the new values using the old cumulative spacing distribution function, so we cannot reset any of the AA values until we have calculated all the new values
                // Also we will calculate the total exponential offset at x=0, CCn+1 and cache it locally
                double CC_nplus1 = 0;
                double[] new_AA_values = new double[noDipsets + 1];
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Get reference to dipset r
                    FractureDipSet Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                    // Update value of CCn+1
                    CC_nplus1 += Dipset_r.getCCStep();

                    // Get new value for AA
                    if (changed_W[DipsetNo_r]) // If the stress shadow width of dipset r has changed, calculate the new value for AA from the cumulative spacing distribution function
                    {
                        double new_W = Dipset_r.Mean_MF_StressShadowWidth;
                        new_AA_values[DipsetNo_r] = getCumulativeSpacingDensity(new_W);
                    }
                    else // Otherwise we can still use the old value
                    {
                        new_AA_values[DipsetNo_r] = Dipset_r.getAA();
                    }
                }

                // Cache the BB value for the first dipset
                double old_BB_1 = FractureDipSets_SortedByW[0].getBB();

                // We can now set the new width values for each dipset
                // We must set the new AA values for each dipset before sorting the list of dipsets in order of stress shadow width, otherwise it will not be in sync with the local list of new AA values
                // However we cannot set the new BB values until after sorting the list of dipsets in order of stress shadow width, since these are dependent on the stress shadow width of the next larger dipset

                // Set the new AA values for each dipset
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Get reference to dipset r
                    FractureDipSet Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                    // Set the stress shadow widths, if these have changed
                    if (changed_W[DipsetNo_r])
                        Dipset_r.setStressShadowWidth();

                    // Set the new AA values only
                    Dipset_r.setMacrofractureSpacingData(new_AA_values[DipsetNo_r], -1, 0);
                }

                // Now sort the list of fracture dip sets by the new stress shadow widths
                // NB Because the comparison result is inverted, the fracture dipsets will be sorted in reverse order of stress shadow width (largest to smallest)
                FractureDipSets_SortedByW.Sort();

                // Repopulate the array of stress shadow widths and AA values in the new order of the dipsets
                // Also create an array of stress shadow widths
                double[] new_W_values = new double[noDipsets];
                for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
                {
                    // Get a handle to the dipset s object
                    FractureDipSet Dipset_s = FractureDipSets_SortedByW[DipsetNo_s];

                    // Add current values for AA, BB and CCStep to the lists
                    new_AA_values[DipsetNo_s] = Dipset_s.getAA();
                    new_W_values[DipsetNo_s] = Dipset_s.getMeanStressShadowWidth();
                }

                // Create local arrays for the new values of BB, CC and CCStep, and add BB1 to the BB list
                // The rest of the arrays will be populated by the Recalculate_BB_CC() function
                double[] new_BB_values = new double[noDipsets + 1];
                new_BB_values[0] = old_BB_1;
                double[] new_CC_values = new double[noDipsets + 1];
                double[] new_CCStep_values = new double[noDipsets];

                // Get theta_M: the inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
                // This will be used to calibrate the AA, BB and CC values for the macrofracture distribution function
                double new_OneMinusPsi = 1 - combined_MF_StressShadowVolume();

                // Get the total density of spacings (equal to the mean linear fracture density), and the density of spacings from dipset 1
                double MFP32 = combined_T_MFP32_total();

                // Calculate new BB and CC values for dipsets s > 1
                Recalculate_BB_CC(new_OneMinusPsi, MFP32, new_W_values, ref CC_nplus1, ref new_AA_values, ref new_BB_values, ref new_CC_values, ref new_CCStep_values);

                // Create local array for the new clear zone volume values
                double[] new_CZV_values = new double[noDipsets];

                // Calculate new clear zone volume values for each dipset (1 - Chi_r)
                // Loop through all the dipsets, from largest to smallest stress shadow width
                double new_CZV_r = 0;
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Cache the new values for AA_r, AA_r-1, BB_r, CC_r and W_r-1 - W_r
                    double AA_r = new_AA_values[DipsetNo_r];
                    double AA_rminus1 = (DipsetNo_r > 0 ? new_AA_values[DipsetNo_r - 1] : 0);
                    double BB_r = new_BB_values[DipsetNo_r];
                    double CC_r = new_CC_values[DipsetNo_r];
                    // If r=1, Wr-1 is infinite but CC_r=0, so the CC_r*dW term will be zero; the linear approximation is not valid for dipset r=1 so should not be used
                    double dW_r = (DipsetNo_r > 0 ? new_W_values[DipsetNo_r - 1] - new_W_values[DipsetNo_r] : double.PositiveInfinity);

                    // If CC_r is very large and BB_r is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                    bool useLinearApprox_r = (float)CC_r == (float)(CC_r + BB_r);

                    // Update CVR term
                    if (useLinearApprox_r)
                    {
                        new_CZV_r += (double.IsInfinity(dW_r) ? 1 : (AA_r + AA_rminus1) * dW_r / 2);
                    }
                    else
                    {
                        new_CZV_r += (AA_r - AA_rminus1) / BB_r;
                        new_CZV_r -= (double.IsInfinity(dW_r) ? 0 : CC_r * dW_r);
                    }

                    // Check if the calculated clear zone volume exceeds 1 and if so reset it to 1
                    // This can happen in early timesteps because we are using AA and BB values from previous timesteps
                    if (new_CZV_r > 1)
                        new_CZV_r = 1;

                    // Add the new clear zone volume value to the appropriate list
                    new_CZV_values[DipsetNo_r] = new_CZV_r;
                } // End calculate new clear zone volume value

                // We will not recalculate the rate of increase of exclusion zone volume when adding new macrofractures (dChi_dP32) since we have not added any new fractures
                // Note however that the current value of this parameter will no longer be valid, until recalculated by the calculateMacrofractureSpacingDistributionData function

                // Set the new BB, CCstep, inverse stress shadow and clear zone volume values for each dipset
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Get reference to dipset r
                    FractureDipSet Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                    // Set the values for dipset r using the values stored in the local arrays
                    // Note that we do not need to set the new AA values as these have already been set and we have not recalculated the dChi_dP32 values since we have not added any new fractures
                    Dipset_r.setMacrofractureSpacingData(-1, new_BB_values[DipsetNo_r], new_CCStep_values[DipsetNo_r]);
                    Dipset_r.setMacrofractureExclusionZoneData(new_OneMinusPsi, new_CZV_values[DipsetNo_r], -1);
                }
            }

            // Return true if the stress shadow width of any dipset has changed, false if the stress shadow widths of all dipsets are unchanged
            return any_changed_W;
        }
        // Functions to return values from the cumulative fracture spacing distribution
        /// <summary>
        /// Calculate the total density of fracture spacings of a specified width or greater, from the cumulative fracture spacing distribution function 
        /// </summary>
        /// <param name="W_in">Specified minimum spacing width; can take any positive value</param>
        /// <returns></returns>
        public double getCumulativeSpacingDensity(double W_in)
        {
            // Cache the total number of fracture dip sets
            int noDipsets = FractureDipSets_SortedByW.Count;

            // Create a reference to the current dipset r
            FractureDipSet Dipset_r = FractureDipSets_SortedByW[0];

            // Set up a local variable for CCr - this will be updated iteratively as we loop through the timesteps
            double CC_r = 0;

            // Create a flag to use a linear approximation for S(x)
            bool useLinearApprox = false;

            // Calculate the new AA values for each dipset, and the BB-1 value for the first dipset
            // Loop through all the dipsets, from largest to smallest stress shadow width
            for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
            {
                // Get reference to dipset r
                Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                // Cache value for W_r
                double W_r = Dipset_r.getMeanStressShadowWidth();

                // Check if W_in >= W_r - if so we can calculate and return the cumulative spacing density
                // Otherwise we must loop on to the next dipset
                if (W_in >= W_r)
                {
                    // Cache values for AA_r and BB_r
                    double AA_r = Dipset_r.getAA();
                    double BB_r = Dipset_r.getBB();

                    // Calculate and return the cumulative spacing density
                    // If CC_r is very large and BB_r is very small, use a linear approximation
                    useLinearApprox = (float)CC_r == (float)(CC_r + BB_r);
                    if (useLinearApprox)
                    {
                        if (DipsetNo_r > 0)
                        {
                            FractureDipSet Dipset_rminus1 = FractureDipSets_SortedByW[DipsetNo_r - 1];
                            double AA_rminus1 = Dipset_rminus1.getAA();
                            double W_rminus1 = Dipset_rminus1.getMeanStressShadowWidth();
                            double dW = W_rminus1 - W_r;
                            if (dW > 0)
                                return (AA_r * ((W_rminus1 - W_in) / dW)) + (AA_rminus1 * ((W_in - W_r) / dW));
                            else
                                return AA_r;
                        }
                        else
                        {
                            return AA_r;
                        }
                    }
                    else
                    {
                        double W_factor = W_in - W_r;
                        return ((AA_r + CC_r) * (W_factor > 0 ? Math.Exp(-BB_r * W_factor) : 1)) - CC_r;
                    }
                }

                // Update value of CCr
                CC_r += Dipset_r.getCCStep();
            }

            // Calculation is slightly different if W_in is less than the smallest stress shadow width Wn
            // The Dipset_r pointer is already set to dipset n so we can read AA_n and W_n directly from the database
            double AA_n = Dipset_r.getAA();
            double W_n = Dipset_r.getMeanStressShadowWidth();

            // AAn+1 is equal to total MFP32
            double MFP32 = combined_T_MFP32_total();

            // We must calculate a value for BBn+1
            // NB We know Wn > 0 because otherwise we would already have returned a value within the loop, when Wr = Wn = 0
            double BB_nplus1 = (Math.Log(MFP32 + CC_r) - Math.Log(AA_n + CC_r)) / W_n;
            // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
            if (double.IsNaN(BB_nplus1) || double.IsNegativeInfinity(BB_nplus1))
                BB_nplus1 = double.PositiveInfinity;

            // If CC_n+1 is very large and BB_n+1 is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
            useLinearApprox = (float)CC_r == (float)(CC_r + BB_nplus1);

            // Calculate and return the value of the exponential cumulative spacing function at W_in
            // NB We know Wn > 0 because otherwise we would already have returned a value within the loop, when Wr = Wn = 0
            if (useLinearApprox)
            {
                return (MFP32 * ((W_n - W_in) / W_n)) + (AA_n * (W_in / W_n));
            }
            else
            {
                return ((MFP32 + CC_r) * (W_in > 0 ? Math.Exp(-BB_nplus1 * W_in) : 1)) - CC_r;
            }
        }
        /// <summary>
        /// Calculate the total clear zone volume for a specified stress shadow width, from the cumulative fracture spacing distribution function 
        /// </summary>
        /// <param name="W_in">Specified stress shadow width; can take any positive value (need not be equal to the macrofracture stress shadow width of an existing fracture dipset)</param>
        /// <returns></returns>
        public double getClearZoneVolume(double W_in)
        {
            // Cache the total number of fracture dip sets
            int noDipsets = FractureDipSets_SortedByW.Count;

            // Create variables for the total clear zone volume - initially this will be zero - and for AAs, BBs, CCs, Ws, AAs-1 and Ws-1
            double total_CZV = 0;
            double AA_s = 0;
            double BB_s = 0;
            double CC_s = 0;
            double W_s = 0;
            double AA_sminus1 = 0;
            double W_sminus1 = double.PositiveInfinity;

            // Create a flag to use a linear approximation for S(x)
            bool useLinearApprox = false;

            // Loop through all the dipsets in order of decreasing stress shadow width
            // Dipset s is an iterator
            for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
            {
                // Get reference to dipset s
                FractureDipSet Dipset_s = FractureDipSets_SortedByW[DipsetNo_s];

                // Cache values for AAs, BBs and Ws
                AA_s = Dipset_s.getAA();
                BB_s = Dipset_s.getBB();
                W_s = Dipset_s.getMeanStressShadowWidth();

                // If CC_s is very large and BB_s is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                useLinearApprox = (float)CC_s == (float)(CC_s + BB_s);

                // If W_in is greater than Ws we can calculate and return the final total clear zone volume
                if (W_in >= W_s)
                {
                    if (useLinearApprox)
                    {
                        if (double.IsInfinity(W_sminus1))
                        {
                            total_CZV = 1 - combined_MF_StressShadowVolume();
                        }
                        else
                        {
                            double AA_W_term = (W_sminus1 > W_s ? (AA_s - AA_sminus1) / (W_sminus1 - W_s) : 0);
                            total_CZV += (AA_W_term / 2) * Math.Pow(W_sminus1 - W_in, 2);
                            total_CZV -= AA_sminus1 * (W_sminus1 - W_in);
                        }
                    }
                    else
                    {
                        double W_factor = W_in - W_s;
                        total_CZV += ((AA_s + CC_s) / BB_s) * (W_factor > 0 ? Math.Exp(-BB_s * W_factor) : 1);
                        total_CZV -= (AA_sminus1 + CC_s) / BB_s;
                        total_CZV -= (!double.IsInfinity(W_sminus1) ? CC_s * (W_sminus1 - W_in) : 0);
                    }

                    if (total_CZV < 0)
                        total_CZV = 0;
                    if (total_CZV > 1)
                        total_CZV = 1;
                    return total_CZV;
                }

                // Otherwise add the latest value of the Sigma[AAs-AAs-1 / BBs] series to the total clear zone volume
                if (useLinearApprox)
                {
                    total_CZV += (!double.IsInfinity(W_sminus1) ? (AA_s + AA_sminus1) * (W_sminus1 - W_s) / 2 : 1 - combined_MF_StressShadowVolume());
                }
                else
                {
                    total_CZV += (AA_s - AA_sminus1) / BB_s;
                    total_CZV -= (!double.IsInfinity(W_sminus1) ? CC_s * (W_sminus1 - W_s) : 0);
                }

                // Update AAs-1, Ws-1 and CCs
                AA_sminus1 = AA_s;
                W_sminus1 = W_s;
                CC_s += Dipset_s.getCCStep();
            }

            // If we still have not returned a value then W_in must be less than W_n, i.e. less than the width of the narrowest stress shadows
            // In this case we must use AAn+1 (=MFP32) and BBn+1 to calculate the final total clear zone volume
            // We must calculate values for AAn+1 and for BBn+1; NB we have already set CCs to CCn+1, and Wn+1 is zero
            AA_s = combined_T_MFP32_total();
            W_s = 0;
            // NB If Wn is zero, set BBn+1 = BBn
            if (W_sminus1 > 0)
                BB_s = (Math.Log(AA_s + CC_s) - Math.Log(AA_sminus1 + CC_s)) / W_sminus1;
            // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
            if (double.IsNaN(BB_s) || double.IsNegativeInfinity(BB_s))
                BB_s = double.PositiveInfinity;
            // If CC_n+1 is very large and BB_n+1 is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
            useLinearApprox = (float)CC_s == (float)(CC_s + BB_s);

            if (useLinearApprox)
            {
                total_CZV += (!double.IsInfinity(W_sminus1) ? ((AA_s - AA_sminus1) / (W_sminus1 - W_s) / 2) * Math.Pow(W_sminus1 - W_in, 2) : 1 - combined_MF_StressShadowVolume());
                total_CZV -= (!double.IsInfinity(W_sminus1) ? AA_sminus1 * (W_sminus1 - W_in) : 0);
            }
            else
            {
                double W_factor = W_in - W_s;
                total_CZV += ((AA_s + CC_s) / BB_s) * (W_factor > 0 ? Math.Exp(-BB_s * W_factor) : 1);
                total_CZV -= (AA_sminus1 + CC_s) / BB_s;
                total_CZV -= (!double.IsInfinity(W_sminus1) ? CC_s * (W_sminus1 - W_in) : 0);
            }

            if (total_CZV < 0)
                total_CZV = 0;
            if (total_CZV > 1)
                total_CZV = 1;
            return total_CZV;
        }
        /// <summary>
        /// Calculate the total exclusion zone volume for a specified stress shadow width, from the cumulative fracture spacing distribution function
        /// </summary>
        /// <param name="W_in">Specified stress shadow width; can take any positive value (need not be equal to the macrofracture stress shadow width of an existing fracture dipset)</param>
        /// <returns></returns>
        public double getExclusionZoneVolume(double W_in)
        {
            return 1 - getClearZoneVolume(W_in);
        }
        /// <summary>
        /// Calculate the total volume within a proximity zone of specified width around all macrofractures 
        /// </summary>
        /// <param name="PZ_Width">Width of the proximity zone</param>
        /// <returns></returns>
        public double getProximityZoneVolume(double PZ_Width)
        {
            // Cache total stress shadow volume and fracture area locally 
            double MFP32 = combined_T_MFP32_total();
            double psi = combined_MF_StressShadowVolume();

            // If there are no macrofractures there will be no proximity zone - return 0
            if (MFP32 <= 0)
                return 0;

            // Calculate maximum (non-overlapping) proximity zone volume
            double max_PZ_Volume = PZ_Width * MFP32;
            // This formula assumes that all stress shadows have equal width, taken as the average width
            // If the stress shadows have different widths, this will give an approximation where the proximity zone width is greater than the minimum stress shadow width - however the discrepency should not be large
            double W_av = psi / MFP32;

            // If proximity zone width is less than the average stress shadow width (or stress shadow volume is 1) use regular formula
            if ((max_PZ_Volume <= psi) || (psi >= 1))
                return Math.Min(max_PZ_Volume, 1);
            // If proximity zone width is greater than stress shadow width use semi-regular formula
            else
                return 1 - getClearZoneVolume(PZ_Width - W_av);
        }
        /// <summary>
        /// Calculate the total volume not lying within a proximity zone of specified width around all macrofractures
        /// </summary>
        /// <param name="PZ_Width">Width of the proximity zone</param>
        /// <returns></returns>
        public double getInverseProximityZoneVolume(double PZ_Width)
        {
            return 1 - getProximityZoneVolume(PZ_Width);
        }
        /// <summary>
        /// Total volumes not lying within proximity zones of specified widths around all macrofractures, during a specified previous timestep M 
        /// </summary>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating</param>
        /// <param name="PZ_Widths">List of proximity zone widths</param>
        /// <returns></returns>
        public List<double> getInverseProximityZoneVolume(int Timestep_M, List<double> PZ_Widths)
        {
            // Create a new list for the output values; also create a new list of flags to indicate whether the output items have been calculated yet
            List<double> Inverse_PZ_Volumes = new List<double>();
            List<bool> Calculated = new List<bool>();

            // Get the index number of timestep M-1, so we can retrieve Psi, MFP32, AA and BB values for the end of the previous timestep
            // If a valid timestep number is not supplied, use the current data
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the total number of supplied proximity zone widths
            int noPZWidths = PZ_Widths.Count;

            // Get the total stress shadow volume and linear macrofracture density at the end of the previous timestep M-1, by looping through all dipsets
            // Then use these to calculate average stress shadow width
            double Psi, MFP32, Wav;
            if (UseCurrentData)
            {
                Psi = combined_MF_StressShadowVolume();
                MFP32 = combined_T_MFP32_total();
                Wav = average_Mean_MF_StressShadowWidth();
            }
            else
            {
                Psi = 0;
                MFP32 = 0;
                foreach (FractureDipSet fds in FractureDipSets)
                {
                    double fds_MFP32 = fds.getTotalMFP32(Timestep_Mminus1);
                    double fds_Psi = fds_MFP32 * fds.getMeanStressShadowWidth(Timestep_Mminus1);
                    MFP32 += fds_MFP32;
                    Psi += fds_Psi;
                }
                if (Psi > 1) Psi = 1;
                Wav = (MFP32 > 0 ? Psi / MFP32 : 0);
            }

            // If there are no macrofractures there will be no proximity zones, so the inverse proximity zone volume will 1 for all specified proximity zone widths
            // We can therefore create an array of 1s and return this
            if (MFP32 <= 0)
            {
                foreach (double PZ_width in PZ_Widths)
                    Inverse_PZ_Volumes.Add(1);
                return Inverse_PZ_Volumes;
            }

            // Loop through all the specified proximity zone widths and check if they are less than the average stress shadow width
            // If so we can use the linear formula to calculate the inverse proximity zone volume and add it to the list
            // If not we will add a zero to the list as a placeholder, and calculate the true inverse proximity zone volume later
            // Also set the flags for calculation
            for (int PZ_widthNo = 0; PZ_widthNo < noPZWidths; PZ_widthNo++)
            {
                double PZ_Width = PZ_Widths[PZ_widthNo];
                if (PZ_Width <= Wav)
                {
                    Inverse_PZ_Volumes.Add(1 - (MFP32 * PZ_Width));
                    Calculated.Add(true);
                }
                else
                {
                    Inverse_PZ_Volumes.Add(0);
                    Calculated.Add(false);
                }
            }

            // Now we need to calculate the inverse proximity zone volumes for all specified proximity zone widths greater than the average stress shadow width
            {
                // Cache the total number of fracture dip sets
                // We will also sort the dip sets by stress shadow width, since the order may vary between timesteps
                FractureDipSets_SortByW(Timestep_Mminus1);
                int noDipsets = FractureDipSets_SortedByW.Count;

                // Create variables for AAs, BBs, CCs, Ws, AAs-1 and Ws-1
                double AA_s = 0;
                double BB_s = 0;
                double CC_s = 0;
                double W_s = 0;
                double AA_sminus1 = 0;
                double W_sminus1 = double.PositiveInfinity;

                // Create a flag to use a linear approximation for S(x)
                bool useLinearApprox = false;

                // Create a variable for the Sigma[(AAs-AAs-1)/BBs - CCs(Ws-Ws-1)] series - initially this will be zero
                double Sigma_AAs_AAsminus1_BBs_CCs_W = 0;

                // Loop through all the dipsets in order of decreasing stress shadow width
                // Dipset s is an iterator
                for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
                {
                    // Get reference to dipsets s
                    FractureDipSet Dipset_s = FractureDipSets_SortedByW[DipsetNo_s];

                    // Cache values for AAs, BBs and Ws
                    if (UseCurrentData)
                    {
                        AA_s = Dipset_s.getAA();
                        BB_s = Dipset_s.getBB();
                        W_s = Dipset_s.getMeanStressShadowWidth();
                    }
                    else
                    {
                        AA_s = Dipset_s.getAA(Timestep_Mminus1);
                        BB_s = Dipset_s.getBB(Timestep_Mminus1);
                        W_s = Dipset_s.getMeanStressShadowWidth(Timestep_Mminus1);
                    }

                    // If CC_s is very large and BB_s is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                    useLinearApprox = (float)CC_s == (float)(CC_s + BB_s);

                    // Loop through all the uncalculated specified proximity zone widths - if they are greater than W_s we can calculate the inverse proximity zone volume, store it in the output list, and set the flag to calculated
                    for (int PZ_widthNo = 0; PZ_widthNo < noPZWidths; PZ_widthNo++)
                        if (!Calculated[PZ_widthNo])
                        {
                            double Outer_PZ_Width = PZ_Widths[PZ_widthNo] - Wav;
                            if (Outer_PZ_Width >= W_s)
                            {
                                double Inverse_PZ_Volume = Sigma_AAs_AAsminus1_BBs_CCs_W;
                                if (useLinearApprox)
                                {
                                    if (double.IsInfinity(W_sminus1))
                                    {
                                        Inverse_PZ_Volume = 1 - Psi;
                                    }
                                    else
                                    {
                                        double AA_W_term = (W_sminus1 > W_s ? (AA_s - AA_sminus1) / (W_sminus1 - W_s) : 0);
                                        Inverse_PZ_Volume += (AA_W_term / 2) * Math.Pow(W_sminus1 - Outer_PZ_Width, 2);
                                        Inverse_PZ_Volume -= AA_sminus1 * (W_sminus1 - Outer_PZ_Width);
                                    }
                                }
                                else
                                {
                                    double W_factor = Outer_PZ_Width - W_s;
                                    Inverse_PZ_Volume += ((AA_s + CC_s) / BB_s) * (W_factor > 0 ? Math.Exp(-BB_s * W_factor) : 1);
                                    Inverse_PZ_Volume -= (AA_sminus1 + CC_s) / BB_s;
                                    Inverse_PZ_Volume -= (!double.IsInfinity(W_sminus1) ? CC_s * (W_sminus1 - Outer_PZ_Width) : 0);
                                }

                                Inverse_PZ_Volumes[PZ_widthNo] = Inverse_PZ_Volume;
                                Calculated[PZ_widthNo] = true;
                            }
                        }

                    // Update the Sigma[(AAs-AAs-1)/BBs - CCs(Ws-Ws-1)] series variable
                    if (useLinearApprox)
                    {
                        Sigma_AAs_AAsminus1_BBs_CCs_W += (!double.IsInfinity(W_sminus1) ? (AA_s + AA_sminus1) * (W_sminus1 - W_s) / 2 : 1 - Psi);
                    }
                    else
                    {
                        Sigma_AAs_AAsminus1_BBs_CCs_W += (AA_s - AA_sminus1) / BB_s;
                        Sigma_AAs_AAsminus1_BBs_CCs_W -= (!double.IsInfinity(W_sminus1) ? CC_s * (W_sminus1 - W_s) : 0);
                    }

                    // Update AAs-1, Ws-1 and CCs
                    AA_sminus1 = AA_s;
                    W_sminus1 = W_s;
                    CC_s += (UseCurrentData ? Dipset_s.getCCStep() : Dipset_s.getCCStep(Timestep_Mminus1));
                }

                // The proximity zone widths that are still uncalculated must have W_in less than W_n, i.e. less than the width of the narrowest stress shadows
                // In this case we must use AAn+1 (=MFP32) and BBn+1 to calculate the final total clear zone volume
                // We must calculate a value for BBn+1; NB we have already set CCs to CCn+1, and Wn+1 is zero
                AA_s = MFP32;
                W_s = 0;
                // NB If Wn is zero, set BBn+1 = BBn
                if (W_sminus1 > 0)
                    BB_s = (Math.Log(AA_s + CC_s) - Math.Log(AA_sminus1 + CC_s)) / W_sminus1;
                // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                if (double.IsNaN(BB_s) || double.IsNegativeInfinity(BB_s))
                    BB_s = double.PositiveInfinity;
                // If CC_n+1 is very large and BB_n+1 is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                useLinearApprox = (float)CC_s == (float)(CC_s + BB_s);

                // Loop through all the specified proximity zone widths - if we still have not calculated a value then the proximity zone width must be less than W_n, i.e. less than the width of the narrowest stress shadows
                // In this case we must use AA_n+1 (=MFP32) and BB_n+1 (=BB_s+1) to calculate the inverse proximity zone volume and store it in the output list
                for (int PZ_widthNo = 0; PZ_widthNo < noPZWidths; PZ_widthNo++)
                    if (!Calculated[PZ_widthNo])
                    {
                        double Outer_PZ_Width = PZ_Widths[PZ_widthNo] - Wav;
                        //if (Outer_PZ_Width >= 0) //We should not need to check this as any PZ_Widths < Wav should already have been calculated; however this check can be activated for bug fixing purposes
                        {
                            double Inverse_PZ_Volume = Sigma_AAs_AAsminus1_BBs_CCs_W;
                            if (useLinearApprox)
                            {
                                Inverse_PZ_Volume += (!double.IsInfinity(W_sminus1) ? ((AA_s - AA_sminus1) / (W_sminus1 - W_s) / 2) * Math.Pow(W_sminus1 - Outer_PZ_Width, 2) : 1 - Psi);
                                Inverse_PZ_Volume -= (!double.IsInfinity(W_sminus1) ? AA_sminus1 * (W_sminus1 - Outer_PZ_Width) : 0);
                            }
                            else
                            {
                                double W_factor = Outer_PZ_Width - W_s;
                                Inverse_PZ_Volume += ((AA_s + CC_s) / BB_s) * (W_factor > 0 ? Math.Exp(-BB_s * W_factor) : 1);
                                Inverse_PZ_Volume -= (AA_sminus1 + CC_s) / BB_s;
                                Inverse_PZ_Volume -= (!double.IsInfinity(W_sminus1) ? CC_s * (W_sminus1 - Outer_PZ_Width) : 0);
                            }

                            Inverse_PZ_Volumes[PZ_widthNo] = Inverse_PZ_Volume;
                            Calculated[PZ_widthNo] = true;
                        }
                    }
            }

            // Check that all calculated proximity zone widths lie between 0 and 1
            for (int PZ_widthNo = 0; PZ_widthNo < noPZWidths; PZ_widthNo++)
            {
                if (Inverse_PZ_Volumes[PZ_widthNo] < 0)
                    Inverse_PZ_Volumes[PZ_widthNo] = 0;
                if (Inverse_PZ_Volumes[PZ_widthNo] > 1)
                    Inverse_PZ_Volumes[PZ_widthNo] = 1;
            }

            // Return the calculated inverse proximity zone volumes
            return Inverse_PZ_Volumes;
        }
        /// <summary>
        /// Estimate the current rate of growth of exclusion zones around a fracture of a specified dipset as a proportion of the rate of growth of exclusion zones around all fractures
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the specified dipset</param>
        /// <returns></returns>
        public double get_dChiMP_dChiTot(FractureDipSet mp_fds)
        {
            // Calculate the rate of change of exclusion zone volume for the specified dip set
            double W_mp_fds = mp_fds.Mean_MF_StressShadowWidth;
            double dChi_mp = (2 * W_mp_fds) * mp_fds.getMeanMFPropagationRate() * mp_fds.a_MFP30_total();

            // Calculate the rate of change of exclusion zone volume for all dipsets
            double dChiTot = 0;
            foreach(FractureDipSet mx_fds in FractureDipSets)
            {
                double W_mx_fds = mx_fds.Mean_MF_StressShadowWidth;
                dChiTot += (W_mp_fds + W_mx_fds) * mx_fds.getMeanMFPropagationRate() * mx_fds.a_MFP30_total();
            }

            return (dChiTot > 0 ? dChi_mp / dChiTot : 1);
        }
        /// <summary>
        /// Estimate the rate of growth of exclusion zones around a fracture of a specified dipset as a proportion of the rate of growth of exclusion zones around all fractures, during a specified previous timestep M
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the specified dipset</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating</param>
        /// <returns></returns>
        public double get_dChiMP_dChiTot(FractureDipSet mp_fds, int Timestep_M)
        {
            // Calculate the rate of change of exclusion zone volume for the specified dip set
            double W_mp_fds = mp_fds.getMeanStressShadowWidth(Timestep_M);
            double dChi_mp = (2 * W_mp_fds) * mp_fds.getMeanMFPropagationRate(Timestep_M) * mp_fds.getActiveMFP30(Timestep_M);

            // Calculate the rate of change of exclusion zone volume for all dipsets
            double dChiTot = 0;
            foreach (FractureDipSet mx_fds in FractureDipSets)
            {
                double W_mx_fds = mx_fds.getMeanStressShadowWidth(Timestep_M);
                dChiTot += (W_mp_fds + W_mx_fds) * mx_fds.getMeanMFPropagationRate(Timestep_M) * mx_fds.getActiveMFP30(Timestep_M);
            }

            return (dChiTot > 0 ? dChi_mp / dChiTot : 1);
        }
        // Functions to calculate and set fracture spacing distribution
        /// <summary>
        /// Recalculate the macrofracture spacing distribution function parameters AA, BB, and CC and exclusion zone volume chi, for each dipset
        /// </summary>
        public void calculateMacrofractureSpacingDistributionData()
        {
            // Cache the total number of fracture dipsets
            // If there are no dipsets, return - subsequent calculations assume at least one dipset
            int noDipsets = FractureDipSets_SortedByW.Count;
            if (noDipsets < 1)
                return;

            // Set up local variables to store the calculated new values of all parameters
            // This is necessary as in many cases, we must calculate the new values using the old values, so we cannot update each value as we calculate it
            // To start, we will set these to default or existing values
            // New value of theta_M: the inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
            double new_OneMinusPsi = 1;
            // Create local arrays for the new values of AA, BB, CCStep, ClearZoneVolume and dChi_dP32
            double[] new_AA_values = new double[noDipsets];
            double[] new_BB_values = new double[noDipsets + 1];
            double[] new_CCStep_values = new double[noDipsets];
            double[] new_CZV_values = new double[noDipsets];
            double[] new_dChi_dP32_values = new double[noDipsets];
            // Populate the lists for new values of AA, BB and CCStep with the current values
            // This is necessary as these parameters are incremented
            // We can populate the lists of new values for ClearZoneVolume and dChi_dP32 with defaults, since these values are recalculated from scratch
            for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
            {
                // Get a handle to the dipset s object
                FractureDipSet Dipset_s = FractureDipSets_SortedByW[DipsetNo_s];

                // Add current values for AA, BB and CCStep to the lists
                new_AA_values[DipsetNo_s] = Dipset_s.getAA();
                new_BB_values[DipsetNo_s] = Dipset_s.getBB();
                new_CCStep_values[DipsetNo_s] = Dipset_s.getCCStep();

                // Add default values for ClearZoneVolume and dChi_dP32 to the lists
                new_CZV_values[DipsetNo_s] = 1;
                new_dChi_dP32_values[DipsetNo_s] = 0;
            }

            // Calculate the new values for theta_M, AA, BB, CCStep, ClearZoneVolume and dChi_dP32
            // These will depend on the stress distribution scenario
            switch (FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress: 
                    {
                        // There are no stress shadows in the evenly distributed stress case so theta_M remains at the default value 1

                        // With no stress shadows, the fracture spacing follows a perfect exponential distribution with AA=BB=MFP32 for all dipsets
                        // Get the total MFP32 for all dipsets
                        double MFP32 = combined_T_MFP32_total();

                        // Loop through all the dipsets, from largest to smallest stress shadow width
                        // Dipset r represents the spacing interval we are updating (i.e. the segment of the spacing curve from W_r-1 to W_r)
                        for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                        {
                            // Set the new values for AA and BB to the total MFP32 for all dipsets
                            new_AA_values[DipsetNo_r] = MFP32;
                            new_BB_values[DipsetNo_r] = MFP32;
                            // Set the new values for CCStep to 0 for all dipsets
                            new_CCStep_values[DipsetNo_r] = 0;
                        }

                        // With no stress shadows the clear zone volume remains at the default value 1
                        // The gradient dChi_dP32 remains at the default value 0
                    }
                    break;
                case StressDistribution.StressShadow:
                    {
                        // Cache total mean linear fracture density MFP32 locally 
                        double MFP32 = combined_T_MFP32_total();

                        // Get theta_M: the inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, at end of timestep M
                        // This will be used to calibrate the AA, BB and CC values for the macrofracture distribution function
                        // This can only be calculated after we have calculated macrofracture growth for this fracture set in this timestep
                        // NB this calculation assumes that stress shadow widths do not change in time, so there is no overlap of stress shadows
                        new_OneMinusPsi = 1 - combined_MF_StressShadowVolume();

                        // Create and populate local arrays for the original values of AA, BB, CCstep, CC, W and ClearZoneVolume for each dipset
                        // Also add values for AAn+1, BBn+1, CCn+1 and Wn+1
                        // NB we will use the original values of AA, BB and CC when calculating new values for AA and BB1
                        // We will then use the new values of AA, BB and CC to calculate ClearZoneVolume and dChi_dP32
                        // W values will not change
                        double[] old_AA_values = new double[noDipsets + 1];
                        double[] old_BB_values = new double[noDipsets + 1];
                        double[] old_CC_values = new double[noDipsets + 1];
                        double[] old_CCstep_values = new double[noDipsets];
                        double[] W_values = new double[noDipsets + 1];
                        double[] old_CZV_values = new double[noDipsets];
                        double CC_value = 0;
                        for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
                        {
                            FractureDipSet Dipset_s = FractureDipSets_SortedByW[DipsetNo_s];
                            old_AA_values[DipsetNo_s] = Dipset_s.getAA();
                            old_BB_values[DipsetNo_s] = Dipset_s.getBB();
                            old_CC_values[DipsetNo_s] = CC_value;
                            double CCstep_value = Dipset_s.getCCStep();
                            CC_value += CCstep_value;
                            old_CCstep_values[DipsetNo_s] = CCstep_value;
                            W_values[DipsetNo_s] = Dipset_s.getMeanStressShadowWidth();
                            old_CZV_values[DipsetNo_s] = Dipset_s.getClearZoneVolume();
                        }
                        // Add AAn+1, BBn+1, CCn+1 and Wn+1
                        {
                            // AAn+1 is MFP32
                            old_AA_values[noDipsets] = MFP32;
                            // Wn+1 is zero
                            W_values[noDipsets] = 0;
                            // CCn+1 is equal to the sum of all CCStep values; BBn+1 must be calculated
                            double W_n = W_values[noDipsets - 1];
                            double AA_n_CC_nplus1 = old_AA_values[noDipsets - 1] + CC_value;
                            double MFP32_CC_nplus1 = MFP32 + CC_value;
                            if ((W_n > 0) && (AA_n_CC_nplus1 > 0))
                                old_BB_values[noDipsets] = (Math.Log(MFP32_CC_nplus1) - Math.Log(AA_n_CC_nplus1)) / W_n;
                            else
                                old_BB_values[noDipsets] = old_BB_values[noDipsets - 1];
                            old_CC_values[noDipsets] = CC_value;
                        }

                        // Create local arrays for the new values of dAAr_dqP32 and dBBr_dqP32
                        double[][] new_dAAr_dqP32_values = new double[noDipsets][];
                        double[][] new_dBBr_dqP32_values = new double[noDipsets][];

                        // Calculate the new AA values for each dipset
                        // Loop through all the dipsets, from largest to smallest stress shadow width
                        // Dipset q represents the dipsets of the fractures we are adding
                        for (int DipsetNo_q = 0; DipsetNo_q < noDipsets; DipsetNo_q++)
                        {
                            // Create local arrays for the new values of dAAr_dqP32 and dBBr_dqP32 for this q value
                            double[] new_dAAr_dthisqP32_values = new double[noDipsets];
                            double[] new_dBBr_dthisqP32_values = new double[noDipsets + 1];

                            // Cache values for (1-Chi)_q, W_q and dqP32 at the end of the previous timestep
                            double CZV_q = old_CZV_values[DipsetNo_q];
                            double W_q = W_values[DipsetNo_q];
                            double dP32_q = FractureDipSets_SortedByW[DipsetNo_q].da_MFP32 + FractureDipSets_SortedByW[DipsetNo_q].ds_MFP32;

                            // Loop through all the dipsets, from largest to smallest stress shadow width
                            // Dipset r represents the spacing interval we are updating (i.e. the segment of the spacing curve from W_r-1 to W_r)
                            for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                            {
                                // Check whether r is greater than q or not (i.e. is the stress shadow width of dipset r less than the stress shadow width of dipset q)
                                bool r_gt_q = (DipsetNo_r > DipsetNo_q);

                                // Cache values for AA_r and W_r at the end of the previous timestep
                                double AA_r = old_AA_values[DipsetNo_r];
                                double W_r = W_values[DipsetNo_r];

                                // Find R - the spacing interval in which W_r + W_q lies - and get a reference to dipset R
                                int DipsetNo_R = 0;
                                while (W_values[DipsetNo_R] > (W_r + W_q))
                                    DipsetNo_R++;

                                // Cache values for AA_R, AA_R-1, BB_R and W_R at the end of the previous timestep
                                double AA_R = old_AA_values[DipsetNo_R];
                                double AA_Rminus1 = (DipsetNo_R > 0 ? old_AA_values[DipsetNo_R - 1] : 0);
                                double BB_R = old_BB_values[DipsetNo_R];
                                double CC_R = old_CC_values[DipsetNo_R];
                                double W_R = W_values[DipsetNo_R];
                                double W_Rminus1 = (DipsetNo_R > 0 ? W_values[DipsetNo_R - 1] : double.PositiveInfinity);

                                // If CC_R is very large and BB_R is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                                bool useLinearApprox_R = (float)CC_R == (float)(CC_R + BB_R);

                                // Calculate R- term
                                // Loop through all the dipsets from 1 to R-1 (NB this may not include any dipsets at all)
                                // Dipset s is an iterator
                                double Rminus_term = 0;
                                for (int DipsetNo_s = 0; DipsetNo_s < DipsetNo_R; DipsetNo_s++)
                                {
                                    // Cache values for AA_s, AA_s-1, BB_s, CC_s and W_s-1 - W_s at the end of the previous timestep
                                    double AA_s = old_AA_values[DipsetNo_s];
                                    double AA_sminus1 = (DipsetNo_s > 0 ? old_AA_values[DipsetNo_s - 1] : 0);
                                    double BB_s = old_BB_values[DipsetNo_s];
                                    double CC_s = old_CC_values[DipsetNo_s];
                                    // If s=1, Ws-1 is infinite but CC_s=0, so the CC_s*dW component of the Rminus term will be zero; the linear approximation is not valid for dipset s=1 so should not be used
                                    double dW_s = (DipsetNo_s > 0 ? W_values[DipsetNo_s - 1] - W_values[DipsetNo_s] : double.PositiveInfinity);

                                    // If CC_s is very large and BB_s is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                                    bool useLinearApprox_s = (float)CC_s == (float)(CC_s + BB_s);

                                    // Update R- term
                                    if (useLinearApprox_s)
                                    {
                                        Rminus_term += (double.IsInfinity(dW_s) ? 0 : (AA_s + AA_sminus1) * dW_s / 2);
                                    }
                                    else
                                    {
                                        Rminus_term += (AA_s - AA_sminus1) / BB_s;
                                        Rminus_term -= (double.IsInfinity(dW_s) ? 0 : CC_s * dW_s);
                                    }
                                }

                                // Calculate R+ term
                                // Loop through all the dipsets from R to r-1 (if r<=q) or q-1 (if r>q); NB this may not include any dipsets at all
                                // Dipset s is an iterator
                                double Rplus_term = 0;
                                int Rplus_limit = (r_gt_q ? DipsetNo_q : DipsetNo_r);
                                for (int DipsetNo_s = DipsetNo_R + 1; DipsetNo_s <= Rplus_limit; DipsetNo_s++)
                                {
                                    // Cache values for AA_s, AA_s-1, BB_s, CC_s and W_s-1 - W_s at the end of the previous timestep
                                    double AA_s = old_AA_values[DipsetNo_s];
                                    double AA_sminus1 = (DipsetNo_s > 0 ? old_AA_values[DipsetNo_s - 1] : 0);
                                    double BB_s = old_BB_values[DipsetNo_s];
                                    double CC_s = old_CC_values[DipsetNo_s];
                                    // If s=1, Ws-1 is infinite but CC_s=0, so the CC_s*dW component of the Rminus term will be zero; the linear approximation is not valid for dipset s=1 so should not be used
                                    double dW_s = (DipsetNo_s > 0 ? W_values[DipsetNo_s - 1] - W_values[DipsetNo_s] : double.PositiveInfinity);

                                    // If CC_s is very large and BB_s is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                                    bool useLinearApprox_s = (float)CC_s == (float)(CC_s + BB_s);

                                    // Update R+ term
                                    if (useLinearApprox_s)
                                    {
                                        Rplus_term += (double.IsInfinity(dW_s) ? 0 : (AA_s + AA_sminus1) * dW_s / 2);
                                    }
                                    else
                                    {
                                        Rplus_term += (AA_s - AA_sminus1) / BB_s;
                                        Rplus_term -= (double.IsInfinity(dW_s) ? 0 : CC_s * dW_s);
                                    }
                                }

                                // Calculate R and CCW terms
                                double R_term, CCWR_term;
                                if (useLinearApprox_R)
                                {
                                    // If R=1, WR-1 is infinite and we can set the R term to 1
                                    if (double.IsInfinity(W_Rminus1))
                                    {
                                        R_term = 1;
                                        CCWR_term = 0;
                                    }
                                    else
                                    {
                                        double AA_W_W_term = (W_Rminus1 > W_R ? ((AA_R * W_R) - (AA_Rminus1 * W_Rminus1)) / (W_Rminus1 - W_R) : 0);
                                        double AA_W_term = (W_Rminus1 > W_R ? (AA_R - AA_Rminus1) / (W_Rminus1 - W_R) : 0);
                                        R_term = AA_W_W_term * (W_Rminus1 + W_R - (2 * (W_r + W_q)));
                                        R_term -= AA_W_term * (Math.Pow(W_Rminus1, 2) + Math.Pow(W_R, 2) - (2 * Math.Pow(W_r + W_q, 2))) / 2;
                                    }
                                    CCWR_term = 0;
                                }
                                else
                                {
                                    double W_factor = W_q + W_r - W_R;
                                    R_term = 2 * ((AA_R + CC_R) / BB_R) * (W_factor > 0 ? Math.Exp(-BB_R * W_factor) : 1);
                                    R_term -= (AA_R + AA_Rminus1 + (2 * CC_R)) / BB_R;
                                    // If R=1, WR-1 is infinite but CC_R=0, so the CCWR term will be zero
                                    if (double.IsInfinity(W_Rminus1))
                                    {
                                        CCWR_term = 0;
                                    }
                                    else
                                    {
                                        CCWR_term = 2 * CC_R * (W_q + W_r);
                                        CCWR_term -= CC_R * (W_R + W_Rminus1);
                                    }
                                }

                                // Calculate AArW term
                                double AArW_term;
                                if (r_gt_q)
                                    AArW_term = 0;
                                else
                                    AArW_term = AA_r * (W_r - W_q);

                                // Calculate new value for AA_r after adding new fractures from dipset q
                                double dAAr_dqP32 = (CZV_q > 0 ? (Rminus_term - Rplus_term + R_term + CCWR_term - AArW_term) / CZV_q : 0);
                                new_dAAr_dthisqP32_values[DipsetNo_r] = dAAr_dqP32;
                                new_AA_values[DipsetNo_r] += (dAAr_dqP32 * dP32_q);

                            } // End loop through dipsets r

                            // Calculate new value for BB_1 after adding new fractures from dipset q
                            double dBB1_dqP32 = (CZV_q > 0 ? 1 / CZV_q : 0);
                            new_dBBr_dthisqP32_values[0] = dBB1_dqP32;
                            new_BB_values[0] += (dBB1_dqP32 * dP32_q);

                            // Add the new dAAr_dqP32 and dBB1_dqP32 values for this dipset q to the lists of all dAAr_dqP32 and dBBr_dqP32 values
                            new_dAAr_dqP32_values[DipsetNo_q] = new_dAAr_dthisqP32_values;
                            new_dBBr_dqP32_values[DipsetNo_q] = new_dBBr_dthisqP32_values;

                        } // End loop through dipsets q

                        // Calculate new BB and CC values for dipsets s > 1
                        // First create a list for new CC values
                        double[] new_CC_values = new double[noDipsets + 1];
                        double CC_nplus1 = old_CC_values[noDipsets];
                        Recalculate_BB_CC(new_OneMinusPsi, MFP32, W_values, ref CC_nplus1, ref new_AA_values, ref new_BB_values, ref new_CC_values, ref new_CCStep_values);

                        // Cache the new values for AA1, BB1, AA1/BB1 and W1 - these will be used later
                        double new_AA_1 = new_AA_values[0];
                        double new_BB_1 = new_BB_values[0];
                        double AA1_BB1 = (new_BB_1 > 0 ? new_AA_1 / new_BB_1 : 1);
                        double W_1 = W_values[0];

                        // Calculate new clear zone volume values for each dipset (1 - Chi_r)
                        // Loop through all the dipsets, from largest to smallest stress shadow width
                        double new_CZV_r = 0;
                        for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                        {
                            // Cache the new values for AA_r, AA_r-1, BB_r, CC_r and W_r-1 - W_r
                            double AA_r = new_AA_values[DipsetNo_r];
                            double AA_rminus1 = (DipsetNo_r > 0 ? new_AA_values[DipsetNo_r - 1] : 0);
                            double BB_r = new_BB_values[DipsetNo_r];
                            double CC_r = new_CC_values[DipsetNo_r];
                            // If r=1, Wr-1 is infinite but CC_r=0, so the CC_r*dW term will be zero; the linear approximation is not valid for dipset r=1 so should not be used
                            double dW_r = (DipsetNo_r > 0 ? W_values[DipsetNo_r - 1] - W_values[DipsetNo_r] : double.PositiveInfinity);

                            // If CC_r is very large and BB_r is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                            bool useLinearApprox_r = (float)CC_r == (float)(CC_r + BB_r);

                            // Update CVR term
                            if (useLinearApprox_r)
                            {
                                new_CZV_r += (double.IsInfinity(dW_r) ? 1 : (AA_r + AA_rminus1) * dW_r / 2);
                            }
                            else
                            {
                                new_CZV_r += (AA_r - AA_rminus1) / BB_r;
                                new_CZV_r -= (double.IsInfinity(dW_r) ? 0 : CC_r * dW_r);
                            }

                            // Check if the calculated clear zone volume exceeds 1 and if so reset it to 1
                            // This can happen in early timesteps because we are using AA and BB values from previous timesteps
                            if (new_CZV_r > 1)
                                new_CZV_r = 1;

                            // Add the new clear zone volume value to the appropriate list
                            new_CZV_values[DipsetNo_r] = new_CZV_r;
                        } // End calculate new clear zone volume value

                        // Calculate the new dBBr_dqP32 and dChiq_dqP32 values
                        // Loop through all the dipsets, from largest to smallest stress shadow width
                        // Dipset q represents the dipsets of the fractures we are adding
                        for (int DipsetNo_q = 0; DipsetNo_q < noDipsets; DipsetNo_q++)
                        {
                            // Cache the new value for clear zone volume (1 - Chi_q)
                            double new_CZV_q = new_CZV_values[DipsetNo_q];

                            // Cache the value for W_q at the end of the previous timestep
                            double W_q = W_values[DipsetNo_q];

                            // Calculate the new dBBr_dqP32 values (except dBB1_dqP32) 
                            // Loop through all the dipsets, from second largest to smallest stress shadow width
                            // Dipset r represents the spacing interval we are updating (i.e. the segment of the spacing curve from W_r-1 to W_r)
                            for (int DipsetNo_r = 1; DipsetNo_r < noDipsets; DipsetNo_r++)
                            {
                                // Cache the new values for AAr, dAAr_dqP32, AAr-1, dAAr-1_dqP32 and CCr-CCr-1
                                // NB there must always be a dipset r-1 since the minimum value for the r iterator is 1
                                double new_AA_r = new_AA_values[DipsetNo_r];
                                double new_dAAr_dqP32 = new_dAAr_dqP32_values[DipsetNo_q][DipsetNo_r];
                                double new_AA_rminus1 = new_AA_values[DipsetNo_r - 1];
                                double new_dAArminus_dqP32 = new_dAAr_dqP32_values[DipsetNo_q][DipsetNo_r - 1];
                                double new_CC_r = new_CC_values[DipsetNo_r];

                                // Cache the values for Wr and Wr-1 at the end of the previous timestep
                                double W_r = W_values[DipsetNo_r];
                                double W_rminus1 = W_values[DipsetNo_r - 1];

                                // Calculate the new dBBr/dqP32 value and add it to the correct list
                                // NB If Wr = Wr-1, set dBBr/dqP32 = dBBr-1/dqP32
                                double new_dBBr_dqP32_value;
                                if (W_r < W_rminus1)
                                {
                                    double new_AAr_plusCCr = new_AA_r + new_CC_r;
                                    double new_AArminus1_plusCCr = new_AA_rminus1 + new_CC_r;

                                    // If AA_r or AA_r-1 are positive then we can use the differential of the formula for BB_r to calculate dBB_r/dMFP32
                                    if (((float)new_AAr_plusCCr > 0f) && ((float)new_AArminus1_plusCCr > 0f))
                                        new_dBBr_dqP32_value = ((new_dAAr_dqP32 / new_AAr_plusCCr) - (new_dAArminus_dqP32 / new_AArminus1_plusCCr)) / (W_rminus1 - W_r);
                                    // If AA_r or AA_r-1 are zero and MFP32 is not zero, then the fractures are saturated and BB_r will be infinite; we can set dBB_r/dMFP32 to zero
                                    else if (MFP32 > 0)
                                        new_dBBr_dqP32_value = 0;
                                    // If AA_r or AA_r-1 are zero and MFP32 is zero, then there are no fractures so dBB_r/dMFP32 = 1
                                    else
                                        new_dBBr_dqP32_value = 1;
                                }
                                else
                                {
                                    new_dBBr_dqP32_value = new_dBBr_dqP32_values[DipsetNo_q][DipsetNo_r - 1];
                                }
                                new_dBBr_dqP32_values[DipsetNo_q][DipsetNo_r] = new_dBBr_dqP32_value;
                            }

                            // Calculate new dChiq_dqP32
                            // Initially (i.e. when BB_1=0), dChiq/dPsiq = 2 so dChiq/dqMFP32 = 2*W_q
                            double new_dChiq_dP32q = 2 * W_q;

                            // If BB_1>0 then we must calculate dChiq/dqMFP32 and then dChiq/dPsiq
                            if ((float)new_BB_1 > 0f)
                            {
                                // Calculate dipset 1 term
                                double BB_term_numerator = (1 - Math.Exp(-new_BB_1 * W_q));
                                double BB_term = 2 * (BB_term_numerator / new_BB_1);
                                new_dChiq_dP32q = (new_CZV_q > 0 ? (AA1_BB1 * (BB_term + (W_1 - W_q))) / new_CZV_q : 0);
                                // Loop through all the dipsets from 2 to q (NB this may not include any dipsets at all)
                                // Dipset s is an iterator
                                for (int DipsetNo_s = 1; DipsetNo_s <= DipsetNo_q; DipsetNo_s++)
                                {
                                    // Cache the new values for AA_s, AA_s-1, BB_s and CCs-CCs-1
                                    // NB there must always be a dipset s-1 since the minimum value for the s iterator is 1
                                    double new_AA_s = new_AA_values[DipsetNo_s];
                                    double new_AA_sminus1 = new_AA_values[DipsetNo_s - 1];
                                    double new_BB_s = new_BB_values[DipsetNo_s];
                                    double new_CCStep_sminus1 = new_CCStep_values[DipsetNo_s - 1];

                                    // Cache the new values for dAAs_dqP32, dAAs-1_dqP32 and dBBs_dqP32
                                    double new_dAAs_dqP32 = new_dAAr_dqP32_values[DipsetNo_q][DipsetNo_s];
                                    double new_dAAsminus1_dqP32 = new_dAAr_dqP32_values[DipsetNo_q][DipsetNo_s - 1];
                                    double new_dBBs_dqP32 = new_dBBr_dqP32_values[DipsetNo_q][DipsetNo_s];

                                    // Update dChiq_dqP32 term
                                    new_dChiq_dP32q += (new_BB_s > 0 ? ((new_AA_s - new_AA_sminus1) / Math.Pow(new_BB_s, 2)) * new_dBBs_dqP32 : 0);
                                    new_dChiq_dP32q -= (new_BB_s > 0 ? (new_dAAs_dqP32 - new_dAAsminus1_dqP32) / new_BB_s : 0);
                                }

                                // Check if the calculated dChiq_dqP32 is negative or exceeds 2*W_q, and if so reset it to 0 or 2*W_q respectively
                                // This can happen in early timesteps because we are using AA and BB values from previous timesteps
                                if (new_dChiq_dP32q < 0)
                                    new_dChiq_dP32q = 0;
                                if (new_dChiq_dP32q > 2 * W_q)
                                    new_dChiq_dP32q = 2 * W_q;
                            }

                            // Add the new dChiq_dqP32 value to the appropriate list
                            new_dChi_dP32_values[DipsetNo_q] = new_dChiq_dP32q;

                        } // End calculate new dBBr_dqP32 and dChiq_dqP32 values

                    } // End case StressDistribution.StressShadow
                    break;
                case StressDistribution.DuctileBoundary: // Not yet implemented
                    break;
                default:
                    break;
            }

            // Set the new values for each dipset
            for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
            {
                // Get reference to dipset r
                FractureDipSet Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                // Set the values for dipset r using the values stored in the local arrays
                Dipset_r.setMacrofractureSpacingData(new_AA_values[DipsetNo_r], new_BB_values[DipsetNo_r], new_CCStep_values[DipsetNo_r]);
                Dipset_r.setMacrofractureExclusionZoneData(new_OneMinusPsi, new_CZV_values[DipsetNo_r], new_dChi_dP32_values[DipsetNo_r]);
            }
        }
        /// <summary>
        /// Calculate new BB and CC values for dipsets s > 1; NB this will not repopulate the FractureDipSet.CurrentFractureData objects, only the local lists supplied as arguments
        /// </summary>
        /// <param name="OneMinusPsi">Current inverse stress shadow volume</param>
        /// <param name="MFP32">Current total mean linear macrofracture density for this fracture set</param>
        /// <param name="W_values">Array of stress shadow widths for each dipset, in ascending order; min array size = noDipsets</param>
        /// <param name="AA_values">Array of current AA values for each dipset, in ascending order of stress shadow width; this must be populated, but may be modified by this function; min array size = noDipsets</param>
        /// <param name="CC_nplus1">Current CC value for set n+1; this will be modified by this function</param>
        /// <param name="BB_values">Array of current BB values for each dipset and n+1, in ascending order of stress shadow width; the first value (BB1) must be populated, the rest of the list will be populated by this function; min array size = noDipsets+1</param>
        /// <param name="CC_values">Array of CC values for each dipset and n+1, in ascending order of stress shadow width; will be populated by this function; min array size = noDipsets+1</param>
        /// <param name="CCStep_values">Array of increments in CC values for each dipset, in ascending order of stress shadow width; will be populated by this function; min array size = noDipsets</param>
        private void Recalculate_BB_CC(double OneMinusPsi, double MFP32, double[] W_values, ref double CC_nplus1, ref double[] AA_values, ref double[] BB_values, ref double[] CC_values, ref double[] CCStep_values)
        {
            // Cache the total number of fracture dipsets
            // If there are no dipsets, return - subsequent calculations assume at least one dipset
            int noDipsets = FractureDipSets_SortedByW.Count;
            if (noDipsets < 1)
                return;

            // If there are no fractures (MFP32=0) then fill the BB_values, CC_values and CCStep_values lists with zeros and return
            if (MFP32 <= 0)
            {
                for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
                {
                    BB_values[DipsetNo_s] = 0;
                    CC_values[DipsetNo_s] = 0;
                    CCStep_values[DipsetNo_s] = 0;
                }
                BB_values[noDipsets] = 0;
                CC_values[noDipsets] = 0;
                return;
            }

            // Cache values for AA1, BB1, AA1/BB1 and W1 - these will be used later
            double AA_1 = AA_values[0];
            double BB_1 = BB_values[0];
            double AA1_BB1 = (BB_1 > 0 ? AA_1 / BB_1 : 1);
            double W_1 = W_values[0];

            // Calculate the minimum and maximum possible volume of all spacings for the current AA and BB1 values
            // The minimum possible volume = A1/B1 + Sigma[s=1,n;(As-As-1)Ws], and assumes that CCs = -AAs-1 and BBs = infinity for all s>1
            // The maximum possible volume = A1/B1 + A1W1 + Sigma[s=1,n;(As+1-As)(Ws+1+Ws)/2], and assumes that CCs = infinity and BBs = 0 for all s>1
            // If 1-psi does not lie between these two values, it is not possible to calculate BB and CC values such that the total spacing volume is equal to the total volume available
            // This may be the case either in early timesteps, where both AA1 and BB1 are small (and therefore AA1/BB1 can suffer extreme rounding errors), or when the stress shadow width of one or more dipsets changes
            // In this case we will adjust AA1, BB1 or other AA values to get a fit
            // We will also create a list of the minimum and maximum volumes occupied by spacings of width W < Ws for all dipsets s
            double minSpacingVolume = 0;
            double maxSpacingVolume = (MFP32 - AA_values[noDipsets - 1]) * (W_values[noDipsets - 1]) / 2;
            double[] minSpacingVolumes = new double[noDipsets];
            double[] maxSpacingVolumes = new double[noDipsets];
            for (int DipsetNo_s = noDipsets - 1; DipsetNo_s > 0; DipsetNo_s--)
            {
                // NB the minimum and maximum volumes occupied by spacings of width W < Ws will not include the minimum volume occupied by spacings with width Ws-1 > W > Ws
                minSpacingVolumes[DipsetNo_s] = minSpacingVolume;
                maxSpacingVolumes[DipsetNo_s] = maxSpacingVolume;
                // Minimum volume occupied by spacings with width Ws < W < Ws-1 is (As - As-1) * Ws (i.e. assume all spacings in that interval have minimum width Ws)
                minSpacingVolume += (AA_values[DipsetNo_s] - AA_values[DipsetNo_s - 1]) * W_values[DipsetNo_s];
                // Maximum volume occupied by spacings with width Ws < W < Ws-1 is (As - As-1) * (Ws + Ws-1) / 2 (i.e. assume S(x) is linear between Ws,As and Ws-1,As-1)
                maxSpacingVolume += (AA_values[DipsetNo_s] - AA_values[DipsetNo_s - 1]) * (W_values[DipsetNo_s] + W_values[DipsetNo_s - 1]) / 2;
            }
            minSpacingVolumes[0] = minSpacingVolume;
            maxSpacingVolumes[0] = maxSpacingVolume;
            // Minimum and maximum volume occupied by spacings with width W > W1 is (A1/B1) + (A1*W1)
            minSpacingVolume += AA1_BB1 + (AA_1 * W_1);
            maxSpacingVolume += AA1_BB1 + (AA_1 * W_1);

            // If there is not enough room to accommodate the minimum possible volume of spacings, we will need to adjust the AA, BB and CC values accordingly
            // NB we can only do this if negative CC values are allowed
            if (AllowNegativeCC && (minSpacingVolume > (OneMinusPsi / Excess_Volume_Multiplier)))
            {
                // Adjust the value of AA1 to match the total volume of spacings with the available volume
                // If (AA1/BB1)+Sigma((AAs-AAs-1)*Ws) > 1-psi then the minimum possible volume of spacings already exceeds the total volume outside the stress shadows available to accommodate them
                // This can be the case in the early stages of fracture development, and also if the stress shadow widths increase

                // We will first try to do this by decreasing AA1 and increasing BB1 in proportion
                double volumeAvailable = (OneMinusPsi / Excess_Volume_Multiplier) - minSpacingVolumes[0];
                double W1_AA1 = W_1 * AA_1;
                double AA_multiplier = (Math.Sqrt(Math.Pow(W1_AA1, 2) + (4 * AA1_BB1 * Math.Max(volumeAvailable, 0))) - W1_AA1) / (2 * AA1_BB1);
                AA1_BB1 *= AA_multiplier;
                BB_1 /= AA_multiplier;
                // We must also update the new_AA_values array and the AA1_BB1 value
                AA_values[0] = AA_1;
                AA1_BB1 = (BB_1 > 0 ? AA_1 / BB_1 : 1);

                // The CC value for the first dipset will be zero, but the CCstep value can be calculated to set the CC value of the next dipset to just above -AA1
                CC_values[0] = 0;
                CCStep_values[0] = -AA_1 / Excess_Volume_Multiplier;

                // We can add BB1 to the BB list
                BB_values[0] = BB_1;

                // Loop through all the other dipsets in order, adjusting the AA values if required and calculating BB and CC values for each
                for (int DipsetNo_r = 1; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Check if we need to adjust the value of AAr to match the total volume of spacings with the available volume
                    // This will only be necessary if we have already reduced the volume of spacings where x > Wr-1 to zero  
                    if ((minSpacingVolumes[DipsetNo_r - 1] * Excess_Volume_Multiplier) > OneMinusPsi)
                    {
                        if (W_values[DipsetNo_r] > 0)
                            AA_values[DipsetNo_r] = (OneMinusPsi - minSpacingVolumes[DipsetNo_r]) / (W_values[DipsetNo_r] * Excess_Volume_Multiplier);
                    }

                    // Set the CC value to just above -AAr-1, and the CCstep value so that CCr+1 will be just above -AAr
                    CC_values[DipsetNo_r] = -AA_values[DipsetNo_r - 1] / Excess_Volume_Multiplier;
                    CCStep_values[DipsetNo_r] = (AA_values[DipsetNo_r - 1] - AA_values[DipsetNo_r]) / Excess_Volume_Multiplier;

                    // Calculate the new BB value - this will be very high, so that S(x) will be approximately L-shaped
                    double dW = W_values[DipsetNo_r - 1] - W_values[DipsetNo_r];
                    // NB If dW is zero, set BBr = BBr-1
                    if (dW > 0)
                    {
                        double new_BB_r = (Math.Log(AA_values[DipsetNo_r] + CC_values[DipsetNo_r]) - Math.Log(AA_values[DipsetNo_r - 1] + CC_values[DipsetNo_r])) / dW;
                        // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                        if (double.IsNaN(new_BB_r) || double.IsNegativeInfinity(new_BB_r))
                            new_BB_r = double.PositiveInfinity;
                        BB_values[DipsetNo_r] = new_BB_r;
                    }
                    else
                    {
                        BB_values[DipsetNo_r] = BB_values[DipsetNo_r - 1];
                    }
                }

                // Get the new CCn+1 value and add it to the list of CC values
                CC_nplus1 = CC_values[noDipsets - 1] + CCStep_values[noDipsets - 1];
                CC_values[noDipsets] = CC_nplus1;

                // Get the new BBn+1 value and add it to the list of BB values
                // NB If Wn is zero, set BBn+1 = BBn
                double Wn = W_values[noDipsets - 1];
                if (Wn > 0)
                {
                    double new_BB_nplus1 = (Math.Log(MFP32 + CC_nplus1) - Math.Log(AA_values[noDipsets - 1] + CC_nplus1)) / Wn;
                    // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                    if (double.IsNaN(new_BB_nplus1) || double.IsNegativeInfinity(new_BB_nplus1))
                        new_BB_nplus1 = double.PositiveInfinity;
                    BB_values[noDipsets] = new_BB_nplus1;
                }
                else
                {
                    BB_values[noDipsets] = BB_values[noDipsets - 1];
                }
            }

            // If there is enough room to accommodate the minimum possible volume of spacings, we can now calculate the BB and CC values for dipsets s>1
            // These will be calculated to match the total volume of spacings with the available volume
            else
            {
                // If there is too much room to accommodate the maximum possible volume of spacings, we will adjust BB1 accordingly
                // We can then recalculate the BB and CC values as normal - this should set BB values towards 0 and CC values high
                if (maxSpacingVolume < (OneMinusPsi * Excess_Volume_Multiplier))
                {
                    BB_1 = AA_1 / ((Excess_Volume_Multiplier * OneMinusPsi) - (AA_1 * W_1) - maxSpacingVolumes[0]);

                    // We must also update the AA1_BB1 value
                    AA1_BB1 = (BB_1 > 0 ? AA_1 / BB_1 : 1);
                }

                // Create a list of normalised CC values (CCs/CCn+1); this determines the distribution of CC values between the dipsets
                // This is based on empirical formulae
                // We can then use this to populate the list of actual CC values
                double[] CC_s_normalised_values = new double[noDipsets + 1];
                // When CCn+1 is positive, the distribution of CC values will be proportional to the distribution of fractures (MFP32) between dipsets
                // This is consistent with the process by which the spacing distribution curve changes as fractures are added
                if (CC_nplus1 > 0)
                {
                    double cum_MFP32_s = 0;
                    for (int DipsetNo_s = 0; DipsetNo_s < noDipsets; DipsetNo_s++)
                    {
                        // NB CC_1 will always be zero
                        double CC_s_normalised_value = (MFP32 > 0 ? cum_MFP32_s / MFP32 : 0);
                        CC_s_normalised_values[DipsetNo_s] = CC_s_normalised_value;
                        CC_values[DipsetNo_s] = CC_s_normalised_value * CC_nplus1;
                        FractureDipSet Dipset_s = FractureDipSets_SortedByW[DipsetNo_s];
                        cum_MFP32_s += Dipset_s.a_MFP32_total() + Dipset_s.s_MFP32_total();
                    }
                    // The normalised CC value for dipset n+1 will always be 1; we can add this and the CCn+1 value to the appropriate lists
                    CC_s_normalised_values[noDipsets] = 1;
                    CC_values[noDipsets] = CC_nplus1;
                }
                else
                // When CCn+1 is negative, the distribution of CC values will be proportional to the AA values; specifically, CC_s will be proportional to AA_s-1
                // This will minimise the volume of the spacings
                {
                    // NB CC_1 will always be zero
                    CC_s_normalised_values[0] = 0;
                    CC_values[0] = 0;
                    for (int DipsetNo_s = 1; DipsetNo_s < noDipsets; DipsetNo_s++)
                    {
                        double CC_s_normalised_value = (AA_values[noDipsets - 1] > 0 ? AA_values[DipsetNo_s - 1] / AA_values[noDipsets - 1] : 0);
                        CC_s_normalised_values[DipsetNo_s] = CC_s_normalised_value;
                        CC_values[DipsetNo_s] = CC_s_normalised_value * CC_nplus1;
                    }
                    // The normalised CC value for dipset n+1 will always be 1; we can add this and the CCn+1 value to the appropriate lists
                    CC_s_normalised_values[noDipsets] = 1;
                    CC_values[noDipsets] = CC_nplus1;
                }

                // Calculate the initial BB values
                // First we can add BB1 to the BB list
                BB_values[0] = BB_1;
                for (int DipsetNo_r = 1; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    double dW = W_values[DipsetNo_r - 1] - W_values[DipsetNo_r];
                    // NB If dW is zero, set BBr = BBr-1
                    if (dW > 0)
                    {
                        double new_BB_r = (Math.Log(AA_values[DipsetNo_r] + CC_values[DipsetNo_r]) - Math.Log(AA_values[DipsetNo_r - 1] + CC_values[DipsetNo_r])) / dW;
                        // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                        if (double.IsNaN(new_BB_r) || double.IsNegativeInfinity(new_BB_r))
                            new_BB_r = double.PositiveInfinity;
                        BB_values[DipsetNo_r] = new_BB_r;
                    }
                    else
                    {
                        BB_values[DipsetNo_r] = BB_values[DipsetNo_r - 1];
                    }
                }
                // Get the new BBn+1 value and add it to the list of BB values
                // NB If Wn is zero, set BBn+1 = BBn
                double Wn = W_values[noDipsets - 1];
                if (Wn > 0)
                {
                    double new_BB_nplus1 = (Math.Log(MFP32 + CC_nplus1) - Math.Log(AA_values[noDipsets - 1] + CC_nplus1)) / Wn;
                    // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                    if (double.IsNaN(new_BB_nplus1) || double.IsNegativeInfinity(new_BB_nplus1))
                        new_BB_nplus1 = double.PositiveInfinity;
                    BB_values[noDipsets] = new_BB_nplus1;
                }
                else
                {
                    BB_values[noDipsets] = BB_values[noDipsets - 1];
                }

                // Calculate the new BB and CCn+1 values
                // BB and CCn+1 must be calculated iteratively to obtain values that give a monotonic cumulative spacing distribution consistent with the total stress shadow volume
                // However if we use Newton-Raphson iteration, this converges quickly so that two explicit iterations per timestep should be sufficient to give accurate results
                // Note that iteration also occurs within the framework of the timesteps
                for (int iterationNo = 0; iterationNo < NoCCIterations; iterationNo++)
                {
                    // Calculate the f(x) and f'(x) terms for Newton-Raphson iteration
                    // Add values for dipset 1
                    double f_term = AA1_BB1 - OneMinusPsi;
                    double fdashed_term = 0;
                    // Add values for dipsets 2 to n
                    for (int DipsetNo_s = 1; DipsetNo_s < noDipsets; DipsetNo_s++)
                    {
                        // Cache the new values for AA_s, BB_s, BB_s+1, CC_s+1 - CC_s and W_s
                        // NB there must always be a dipset s-1 since the minimum value for the s iterator is 1
                        double new_AA_s = AA_values[DipsetNo_s];
                        double new_AA_sminus1 = AA_values[DipsetNo_s - 1];
                        double new_BB_s = BB_values[DipsetNo_s];
                        double new_CC_s = CC_values[DipsetNo_s];
                        double CC_normalised_s = CC_s_normalised_values[DipsetNo_s];
                        double dW = W_values[DipsetNo_s - 1] - W_values[DipsetNo_s];

                        // Update f and f' terms
                        f_term += (new_BB_s > 0 ? (new_AA_s - new_AA_sminus1) / new_BB_s : 0) - (new_CC_s * dW);
                        double fdashed_denominator = (new_AA_s + new_CC_s) * (new_AA_sminus1 + new_CC_s) * dW * Math.Pow(new_BB_s, 2);
                        fdashed_term += CC_normalised_s * ((fdashed_denominator > 0 ? Math.Pow(new_AA_s - new_AA_sminus1, 2) / fdashed_denominator : 0) - dW);
                    }
                    // Add values for dipset n+1
                    {
                        // Cache the new values for AA_s, AA_s-1, BB_s, CC_s and W_s-1 - W_s
                        double new_AA_s = MFP32;
                        double new_AA_sminus1 = AA_values[noDipsets - 1];
                        double new_BB_s = BB_values[noDipsets];
                        double new_CC_s = CC_values[noDipsets];
                        double CC_normalised_s = CC_s_normalised_values[noDipsets];

                        // Update f and f' terms
                        f_term += (new_BB_s > 0 ? (new_AA_s - new_AA_sminus1) / new_BB_s : 0) - (new_CC_s * Wn);
                        double fdashed_denominator = (new_AA_s + new_CC_s) * (new_AA_sminus1 + new_CC_s) * Wn * Math.Pow(new_BB_s, 2);
                        fdashed_term += CC_normalised_s * ((fdashed_denominator > 0 ? Math.Pow(new_AA_s - new_AA_sminus1, 2) / fdashed_denominator : 0) - Wn);
                    }

                    // Calculate the new CCn+1 value
                    double dCC_nplus1 = -(fdashed_term != 0 ? f_term / fdashed_term : 0);
                    CC_nplus1 += dCC_nplus1;

                    // If negative CC values are not allowed, check if CCn+1 is negative and if so set it to zero
                    // Even if negative CC values are allowed, AAn+CCn+1 should always be positive
                    if (AllowNegativeCC)
                    {
                        if (CC_nplus1 < (-AA_values[noDipsets - 1] / Excess_Volume_Multiplier))
                            CC_nplus1 = -AA_values[noDipsets - 1] / Excess_Volume_Multiplier;
                    }
                    else
                    {
                        if (CC_nplus1 < 0)
                            CC_nplus1 = 0;
                    }

                    // Calculate the new BB and CC values
                    for (int DipsetNo_r = 1; DipsetNo_r < noDipsets; DipsetNo_r++)
                    {
                        double new_CC_value = CC_s_normalised_values[DipsetNo_r] * CC_nplus1;
                        CC_values[DipsetNo_r] = new_CC_value;
                        double dW = W_values[DipsetNo_r - 1] - W_values[DipsetNo_r];
                        // NB If dW is zero, set BBr = BBr-1
                        if (dW > 0)
                        {
                            double new_BB_r = (Math.Log(AA_values[DipsetNo_r] + new_CC_value) - Math.Log(AA_values[DipsetNo_r - 1] + new_CC_value)) / dW;
                            // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                            if (double.IsNaN(new_BB_r) || double.IsNegativeInfinity(new_BB_r))
                                new_BB_r = double.PositiveInfinity;
                            BB_values[DipsetNo_r] = new_BB_r;
                        }
                        else
                        {
                            BB_values[DipsetNo_r] = BB_values[DipsetNo_r - 1];
                        }
                    }

                    // Add the new CCn+1 value to the list of CC values
                    CC_values[noDipsets] = CC_nplus1;

                    // Get the new BBn+1 value and add it to the list of BB values
                    // NB If Wn is zero, set BBn+1 = BBn
                    if (Wn > 0)
                    {
                        double new_BB_nplus1 = (Math.Log(MFP32 + CC_nplus1) - Math.Log(AA_values[noDipsets - 1] + CC_nplus1)) / Wn;
                        // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                        if (double.IsNaN(new_BB_nplus1) || double.IsNegativeInfinity(new_BB_nplus1))
                            new_BB_nplus1 = double.PositiveInfinity;
                        BB_values[noDipsets] = new_BB_nplus1;
                    }
                    else
                    {
                        BB_values[noDipsets] = BB_values[noDipsets - 1];
                    }
                }

                // Calculate the new CCStep (CC_r+1 - CC_r) values
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    CCStep_values[DipsetNo_r] = CC_values[DipsetNo_r + 1] - CC_values[DipsetNo_r];
                }
            }
        }

        // Macrofracture length functions
        /// <summary>
        /// Current mean macrofracture half-length
        /// </summary>
        /// <returns></returns>
        public double Mean_MF_HalfLength()
        {
            return (combined_T_MFP32_total() / (combined_T_MFP30_total() * gbc.ThicknessAtDeformation));
        }

        // Macrofracture connectivity indices
        /// <summary>
        /// Proportion of unconnected macrofracture tips - i.e. active macrofracture tips
        /// </summary>
        /// <returns>Ratio of a_MFP30_total to T_MFP30_total</returns>
        public double UnconnectedTipRatio()
        {
            double T_MFP30 = combined_T_MFP30_total();
            return (T_MFP30 > 0 ? combined_a_MFP30_total() / combined_T_MFP30_total() : 1);
        }
        /// <summary>
        /// Proportion of macrofracture tips connected to relay zones - i.e. static macrofracture tips deactivated due to stress shadow interaction
        /// </summary>
        /// <returns>Ratio of sII_MFP30_total to T_MFP30_total</returns>
        public double RelayTipRatio()
        {
            double T_MFP30 = combined_T_MFP30_total();
            return (T_MFP30 > 0 ? combined_sII_MFP30_total() / combined_T_MFP30_total() : 0);
        }
        /// <summary>
        /// Proportion of connected macrofracture tips - i.e. static macrofracture tips deactivated due to intersection with orthogonal or oblique fractures
        /// </summary>
        /// <returns>Ratio of sIJ_MFP30_total to T_MFP30_total</returns>
        public double ConnectedTipRatio()
        {
            double T_MFP30 = combined_T_MFP30_total();
            return (T_MFP30 > 0 ? combined_sIJ_MFP30_total() / combined_T_MFP30_total() : 0);
        }

        // Functions used to calculate probability of macrofracture deactivation
        /// <summary>
        /// Control variable: Set True to calculate the weighting factor for sIIP30 to account for stress shadow blocking by other stress shadows based on current fracture populations; set False to use default_ZetaII
        /// </summary>
        private static bool calculate_ZetaII = true;
        /// <summary>
        /// Default value for the weighting factor for sIIP30 to account for stress shadow blocking by other stress shadows based on current fracture populations
        /// </summary>
        private static double default_ZetaII = 0.25;
        /// <summary>
        /// Control variable: Set True to calculate the weighting factor for sIJP30 to account for stress shadow blocking by intersecting fractures based on current fracture populations; set False to use default_ZetaIJ
        /// </summary>
        private static bool calculate_ZetaIJ = true;
        /// <summary>
        /// Default value for the weighting factor for sIJP30 to account for stress shadow blocking by intersecting fractures based on current fracture populations
        /// </summary>
        private static double default_ZetaIJ = 0;
        /// <summary>
        /// Weighting factor for sIIP30 to account for stress shadow blocking by other stress shadows
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="mt_fds">Reference to the FractureDipSet object for the terminating macrofracture</param>
        /// <param name="mp_PropDir">Propagation direction of the propagating fracture tip</param>
        /// <returns></returns>
        private double ZetaII(FractureDipSet mp_fds, FractureDipSet mt_fds, PropagationDirection mp_PropDir)
        {
            // Cache variables locally
            double Wmp = mp_fds.Mean_MF_StressShadowWidth;
            double Wmt = mt_fds.Mean_MF_StressShadowWidth;
            double Wbav = average_Mean_MF_StressShadowWidth(mp_PropDir, false);

            // Calculate and return ZetaII
            double Wfactor = ((Wmt + Wmp) * (Wmt + Wbav));
            return (Wfactor > 0 ? Math.Pow(Wmt, 2) / Wfactor : 0.25);
        }
        /// <summary>
        /// Weighting factor for sIJP30 to account for stress shadow blocking by intersecting fractures
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="mt_fds">Reference to the FractureDipSet object for the terminating macrofracture</param>
        /// <param name="mp_PropDir">Propagation direction of the propagating fracture tip</param>
        /// <returns></returns>
        private double ZetaIJ(FractureDipSet mp_fds, FractureDipSet mt_fds, PropagationDirection mp_PropDir)
        {
            // Cache variables locally
            double W_ExcZ = mp_fds.Mean_MF_StressShadowWidth + mt_fds.Mean_MF_StressShadowWidth;
            double h = gbc.ThicknessAtDeformation;

            // Loop through all other fracture sets to calculate total_fb_MFP30 and total_fb_apparentMFP32
            double fb_MFP30 = 0;
            double app_fb_MFP32 = 0;
            foreach (Gridblock_FractureSet fb_fs in gbc.FractureSets)
                if (fb_fs != this)
                {
                    double intersectionAngleSin = Math.Abs(VectorXYZ.Sin_trim(Strike - fb_fs.Strike));
                    fb_MFP30 += fb_fs.combined_T_MFP30_total();
                    app_fb_MFP32 += (intersectionAngleSin * fb_fs.combined_T_MFP32_total());
                }

            // Calculate helper variables
            double h_W_fb_MFP30_factor = fb_MFP30 * h * W_ExcZ;

            // Calculate and return ZetaIJ
            double output = 0;
            if (h_W_fb_MFP30_factor > (app_fb_MFP32 * 4))
                output = (2 * app_fb_MFP32) / (h_W_fb_MFP30_factor);
            else if (app_fb_MFP32 > 0)
                output = h_W_fb_MFP30_factor / (8 * app_fb_MFP32);
            return output;
        }
        /// <summary>
        /// Rate of growth of vertical lengthwise area of stress shadow interaction boxes, weighted by stress shadow width
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="mp_PropDir">Propagation direction of the propagating fracture tip</param>
        /// <returns></returns>
        private double Weighted_dIBP32_dt(FractureDipSet mp_fds, PropagationDirection mp_PropDir)
        {
            double output = 0;
            double W_mp_fds = mp_fds.Mean_MF_StressShadowWidth;
            foreach (FractureDipSet mt_fds in FractureDipSets)
            {
                double zetaII = (calculate_ZetaII ? ZetaII(mp_fds, mt_fds, mp_PropDir) : default_ZetaII);
                double zetaIJ = (calculate_ZetaIJ ? ZetaIJ(mp_fds, mt_fds, mp_PropDir) : default_ZetaIJ);
                double W_mt_fds = mt_fds.Mean_MF_StressShadowWidth;
                double W_multiplier = (W_mp_fds > 0 ? (W_mp_fds + W_mt_fds) / (2 * W_mp_fds) : 1);
                output += (W_multiplier * mt_fds.Xi(mp_fds, mp_PropDir, zetaII, zetaIJ));
            }
            output *= gbc.ThicknessAtDeformation;
            return output;
        }
        /// <summary>
        /// Calculate the probability that an active half-macrofracture from a specified dipset will not be deactivated due to stress shadow interaction with any other fractures from this set, within a specified time period
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="mp_PropDir">Propagation direction of the propagating fracture tip</param>
        /// <param name="time">Time duration in which to check for fracture deactivation</param>
        /// <returns></returns>
        public double Calculate_PhiII_ByTime(FractureDipSet mp_fds, PropagationDirection mp_PropDir, double time)
        {
            switch (FractureDistribution)
            {
                case StressDistribution.EvenlyDistributedStress:
                    // No stress shadows - return 1
                    return 1;
                case StressDistribution.StressShadow:
                    // Calculate the rate of growth of the exclusion zone - i.e. the total volume of the stress shadow interaction boxes
                    double dChi_dt = mp_fds.getdChi_dMFP32_M() * Weighted_dIBP32_dt(mp_fds, mp_PropDir);
                    // Get clear zone volume (for this set only)
                    double CZV = mp_fds.getClearZoneVolume();
                    // If clear zone volume is zero then fractures cannot propagate, so return 0
                    if (CZV <= 0) return 0;
                    // If there can be stress shadow interaction between different fracture sets, the volume of the stress shadow interaction boxes must be weighted further
                    // This is because they may occupy half of the exclusion zones of the other sets, but cannot intersect the propagating tip in these areas
                    if (gbc.PropControl.checkAlluFStressShadows)
                    {
                        // Get clear zone volume for all fracture sets, including this one
                        double CZV_AllFS = mp_fds.getClearZoneVolumeAllFS();
                        // Calculate multiplier and apply it
                        double dChi_dt_multiplier = (2 * CZV_AllFS) / (CZV + CZV_AllFS);
                        dChi_dt *= dChi_dt_multiplier;
                    }
                    // Calculate instantaneous rate of stress shadow interaction - this is the (weighted) volume of the stress shadow interaction boxes divided by the clear zone volume
                    double FII = dChi_dt / CZV;
                    // Use the exponential formula to calculate PhiII
                    return Math.Exp(-FII * time);
                case StressDistribution.DuctileBoundary:
                    // Not yet implemented - return 1
                    return 1;
                default:
                    // Assume no stress shadow interaction as default - return 1
                    return 1;
            }
        }
        /// <summary>
        /// Calculate the probability that an active half-macrofracture from a specified dipset will not be deactivated due to intersection with fractures from another set, as it propagates over a specified distance
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="propagationDistance">Propagation distance of the propagating fracture</param>
        /// <returns></returns>
        public double Calculate_PhiIJ_ByDistance(FractureDipSet mp_fds, double propagationDistance)
        {
            // Set the initial value to 1 (no deactivation)
            double PhiIJ = 1;

            // Loop through every other fracture set in the gridblock
            foreach (Gridblock_FractureSet fb_fs in gbc.FractureSets)
                if (fb_fs != this)
                {
                    // Get the positive sine of the angle of intersection between the two fracture sets
                    double intersectionAngleSin = Math.Abs(VectorXYZ.Sin_trim(Strike - fb_fs.Strike));

                    // Calculate the propagation distance perpendicular to this fracture set
                    double relativePropagationDistance = propagationDistance * intersectionAngleSin;

                    // Calculate the inverse proximity volume for this fracture set
                    double inverseProximityVolume = fb_fs.getInverseProximityZoneVolume(relativePropagationDistance);

                    // Calculate PhiIJ for this fracture set
                    double fs_PhiIJ;
                    // If stress shadows from other fracture sets can control fracture nucleation, we must take this into account when calculating the probability of deactivation as a function of length
                    if (gbc.PropControl.checkAlluFStressShadows)
                    {
                        // The probability that a set I fracture nucleating within the clear zone will intersect a set J fracture is given by the intersection of the clear zone volume and the propagation distance proximity volume, as a proportion of the clear zone volume
                        // This is complicated by the fact that the clear zone volume lies midway between the set J fractures, but the propagation distance proximity volume lies adjacent to one of the set J fractures

                        // First we must get the exclusion zone width around a set J fracture as seen by a set I fracture
                        double EZWidthJI = gbc.getCrossFSExclusionZoneWidth(fb_fs, this, mp_fds, -1);

                        // Now we can calculate the full clear zone volume around a set of fractures J as seen by a set I fracture
                        double CZV_JI = fb_fs.getInverseProximityZoneVolume(EZWidthJI);

                        // The area of intersection of the clear zone volume and the propagation distance proximity volume is given by the proximity zone volume minus the volume of proximity zones equal to half the exclusion zone width
                        // NB if the relative propagation distance is less than half the exclusion zone width, the proximity zones will lie entirely within the exclusion zones and the area of intersection with the clear zone will be zero
                        // However the intersection of the clear zone volume and the propagation distance proximity volume cannot be larger than the total clear zone volume
                        double EZHalfWidthJI = EZWidthJI / 2;
                        double CZV_PZV_intersectionVolume;
                        if (relativePropagationDistance <= EZHalfWidthJI)
                        {
                            CZV_PZV_intersectionVolume = 0;
                        }
                        else
                        {
                            CZV_PZV_intersectionVolume = fb_fs.getInverseProximityZoneVolume(EZHalfWidthJI) - inverseProximityVolume;
                            if (CZV_PZV_intersectionVolume > CZV_JI)
                                CZV_PZV_intersectionVolume = CZV_JI;
                        }

                        // A fracture will be deactivated if it lies within the intersection of the clear zone volume and the propagation distance proximity volume, so the activation probability PhiIJ is the inverse of that
                        fs_PhiIJ = 1 - (CZV_PZV_intersectionVolume / CZV_JI);
                    }
                    // Otherwise PhiIJ is simply the inverse proximity volume
                    else
                    {
                        fs_PhiIJ = inverseProximityVolume;
                    }

                    // Update total PhiIJ
                    PhiIJ *= fs_PhiIJ;
                }

            // Return PhiIJ
            return PhiIJ;
        }
        /// <summary>
        /// Calculate the probability that an active half-macrofracture from this set will not be deactivated for any reason while it propagates for a specified time
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="mp_PropDir">Propagation direction of the propagating fracture tip</param>
        /// <param name="propagationTime">Time of propagation</param>
        /// <returns></returns>
        public double Calculate_Phi_ByTime(FractureDipSet mp_fds, PropagationDirection mp_PropDir, double propagationTime)
        {
            // Calculate the time taken to propagate the specified distance - if the fracture is not propagating return zero
            double propagationRate = mp_fds.getMeanMFPropagationRate();
            double propagationDistance = propagationTime * propagationRate;
            if (propagationDistance > 0)
                return Calculate_Phi_ByDistance(mp_fds, mp_PropDir, propagationDistance);
            else
                return 0;
        }
        /// <summary>
        /// Calculate the probability that an active half-macrofracture from this set will not be deactivated for any reason while it propagates a specified distance
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="mp_PropDir">Propagation direction of the propagating fracture tip</param>
        /// <param name="propagationDistance">Propagation distance</param>
        /// <returns></returns>
        public double Calculate_Phi_ByDistance(FractureDipSet mp_fds, PropagationDirection mp_PropDir, double propagationDistance)
        {
            return Calculate_Phi_ByDistance(mp_fds, -1, new List<double> { propagationDistance })[0];
        }
        /// <summary>
        /// Calculate the probability that an active half-macrofracture will not be deactivated for any reason while it propagates various specified distances, during a specified previous timestep M
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating</param>
        /// <param name="propagationDistances">List of propagation distances</param>
        /// <returns></returns>
        public List<double> Calculate_Phi_ByDistance(FractureDipSet mp_fds, int Timestep_M, List<double> propagationDistances)
        {
            // Create a new list for the output values
            List<double> Phi_Values = new List<double>();

            // Propagation and deactivation rate data will be taken from timestep M
            // If a valid timestep number is not supplied, use the current data
            bool UseCurrentData = (Timestep_M < 1);

            // Get the total number of supplied propagation distances
            int noDistances = propagationDistances.Count;

            // Get the required macrofracture data from timestep M: mean propagation rate and instantaneous probability of fracture deactivation due to stress shadow interaction
            double propagationRate = (UseCurrentData ? mp_fds.getMeanMFPropagationRate() : mp_fds.getMeanMFPropagationRate(Timestep_M));
            double FII = (UseCurrentData ? mp_fds.getInstantaneousFII() : mp_fds.getInstantaneousFII(Timestep_M));
            // NB if the mean propagation rate is zero (i.e. the fracture is not propagating) the Phi values will all be 1 (no deactivation) so we can create an array of 1s and return this
            if (propagationRate <= 0)
            {
                foreach (double propagationDistance in propagationDistances)
                    Phi_Values.Add(1);
                return Phi_Values;
            }

            // Calculate the probability of deactivation due to stress shadow interaction (Phi_II) for each of the specified propagation distances and add this to the list of output values
            foreach (double propagationDistance in propagationDistances)
            {
                // Calculate the time taken to propagate the specified distance
                double propagationTime = propagationDistance / propagationRate;
                // Calculate Phi_II and add this to the list of output values
                Phi_Values.Add(Math.Exp(-FII * propagationTime));
            }

            // Calculate the probability of deactivation due to intersection with other fracture sets (Phi_IJ) for each of the specified propagation distances and recalculate the output values accordingly
            // Loop through every other fracture set in the gridblock
            foreach (Gridblock_FractureSet fb_fs in gbc.FractureSets)
                if (fb_fs != this)
                {
                    // Get the positive sine of the angle of intersection between the two fracture sets
                    double intersectionAngleSin = Math.Abs(VectorXYZ.Sin_trim(Strike - fb_fs.Strike));

                    // Create a new list of the propagation distance perpendicular to this fracture set
                    List<double> relativePropagationDistances = new List<double>();
                    foreach (double propagationDistance in propagationDistances)
                        relativePropagationDistances.Add(propagationDistance * intersectionAngleSin);

                    // Get a list of PhiIJ values for each of the specified propagation distances for this fracture set
                    List<double> fs_PhiIJ = fb_fs.getInverseProximityZoneVolume(Timestep_M, relativePropagationDistances);

                    // If stress shadows from other fracture sets can control fracture nucleation, we must take this into account when calculating the probability of deactivation as a function of length
                    if (gbc.PropControl.checkAlluFStressShadows)
                    {
                        // Get the minimum distance from a set J fracture that a macrofracture from this set can nucleate
                        double MinimumDistance = gbc.getCrossFSExclusionZoneWidth(fb_fs, this, mp_fds, Timestep_M) / 2;

                        // Get the probability that a fracture nucleating anywhere will reach the minimum distance a fracture can nucleate from a set J fracture
                        double PhiIJMinDist = fb_fs.getInverseProximityZoneVolume(Timestep_M, new List<double> { MinimumDistance })[0];

                        // Modify the calculated PhiIJ values: 
                        // - if the relative propagation distance is less than the minimum distance, set PhiIJ=1 (no deactivation)
                        // - otherwise normalise PhiIJ against the probability that a fracture nucleating anywhere will reach the minimum distance
                        for (int propagationDistanceNo = 0; propagationDistanceNo < noDistances; propagationDistanceNo++)
                        {
                            if (relativePropagationDistances[propagationDistanceNo] <= MinimumDistance)
                                fs_PhiIJ[propagationDistanceNo] = 1;
                            else
                                fs_PhiIJ[propagationDistanceNo] /= PhiIJMinDist;
                        }
                    }

                    // Update the total Phi values for each of the specified propagation distances
                    for (int propagationDistanceNo = 0; propagationDistanceNo < noDistances; propagationDistanceNo++)
                    {
                        Phi_Values[propagationDistanceNo] *= fs_PhiIJ[propagationDistanceNo];
                    }
                }

            // Return the output list
            return Phi_Values;
        }
        /// <summary>
        /// Get the mean distance that an initially active fracture will propagate before being deactivated - this will give the mean length of the residual fractures
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="useQuickMethod">If true, use quick calculation (this assumes macrofracture deactivation is a purely random process), otherwise use long calculation (this takes account of semi-regular macrofracture interaction probabilities, but is only valid for two orthogonal fracture sets)</param>
        /// <returns></returns>
        public double Calculate_MeanPropagationDistance(FractureDipSet mp_fds, bool useQuickMethod)
        {
            return Calculate_MeanPropagationDistance(mp_fds, -1, new List<double>() { 0 }, useQuickMethod)[0];
        }
        /// <summary>
        /// Get the mean distance that an initially active fracture will propagate during a specified previous timestep M before being deactivated, subject to specified minimum cutoffs
        /// These values give the normalised cumulative lengths of all fractures longer than the cutoffs
        /// Multiplying these values by the total volumetric density of residual fractures will give the total lengths of all residual fractures longer than the cutoffs
        /// </summary>
        /// <param name="mp_fds">Reference to the FractureDipSet object for the propagating macrofracture</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating</param>
        /// <param name="cutoffLength">List of cutoff lengths</param>
        /// <param name="useQuickMethod">If true, use quick calculation (this assumes macrofracture deactivation is a purely random process), otherwise use long calculation (this takes account of semi-regular macrofracture interaction probabilities, but is only valid for two orthogonal fracture sets)</param>
        /// <returns></returns>
        public List<double> Calculate_MeanPropagationDistance(FractureDipSet mp_fds, int Timestep_M, List<double> cutoffLengths, bool useQuickMethod)
        {
            // Set up a new list for the output values
            List<double> meanPropagationDistances = new List<double>();

            // Get the index number of timestep M-1, so we can retrieve MFP32 rate values for the end of the previous timestep
            // Propagation and deactivation rate data will be taken from timestep M
            // If a valid timestep number is not supplied, use the current data
            // NB the MFP32 values for the current timestep will not yet have been calculated, but the propagation and deactivation rate data has been
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the mean propagation rate
            // NB if this is zero the mean propagation distances will all be zero so we can create an array of zero values and return this
            double propagationRate = (UseCurrentData ? mp_fds.getMeanMFPropagationRate() : mp_fds.getMeanMFPropagationRate(Timestep_M));
            if ((float)propagationRate <= 0f)
            {
                foreach (double cutoff in cutoffLengths)
                    meanPropagationDistances.Add(0);
                return meanPropagationDistances;
            }

            if (useQuickMethod) // Quick calculation assumes macrofracture deactivation is a purely random process, so mean fracture propagation distance can be calculated by integrating an exponential deactivation function with respect to length
            {
                // F_byDistance represents the instantaneous probability of fracture deactivation for any reason as a function of distance
                double F_byTime = (UseCurrentData ? mp_fds.getInstantaneousF() : mp_fds.getInstantaneousF(Timestep_M));
                double F_byDistance = F_byTime / propagationRate;

                // Calculate the mean length for each of the specified length cutoffs
                foreach (double cutoff in cutoffLengths)
                    meanPropagationDistances.Add((cutoff + (1 / F_byDistance)) * Math.Exp(-F_byDistance * cutoff));
            }
            // Long calculation takes account of semi-regular macrofracture interaction probabilities
            // There are various methods we can use to do this, depending on the number of fracture sets and distribution of fractures
            else 
            {
                // First find the fracture set with the highest P32 density (except for this one)
                double maxP32 = 0;
                double totalP32 = 0;
                Gridblock_FractureSet fb_fs = null;
                foreach (Gridblock_FractureSet fs in gbc.FractureSets)
                    if (fs != this)
                    {
                        double fsP32 = 0;
                        foreach (FractureDipSet fds in fs.FractureDipSets)
                            fsP32 += (UseCurrentData ? fds.getTotalMFP32() : fds.getTotalMFP32(Timestep_Mminus1));
                        if (fsP32 > maxP32)
                        {
                            maxP32 = fsP32;
                            fb_fs = fs;
                        }
                        totalP32 += fsP32;
                    }

                // Calculate the instantaneous probability of fracture deactivation due to any reason other than intersecting with a fracture from set fb_fs as a function of distance
                // First we get the instantaneous probability of fracture deactivation due to stress shadow interaction as a function of distance
                double FII_byTime = (UseCurrentData ? mp_fds.getInstantaneousFII() : mp_fds.getInstantaneousFII(Timestep_M));
                double FII_byDistance = FII_byTime / propagationRate;

                // Get the mean fracture propagation distance, using the most appropriate method
                // If no other fracture sets have any fractures, fb_fs will be null and we can calculate the mean fracture propagation distance assuming deactivation by stress shadow interaction only
                if (fb_fs == null)
                {
                    foreach (double cutoff in cutoffLengths)
                        meanPropagationDistances.Add((cutoff + (1 / FII_byDistance)) * Math.Exp(-FII_byDistance * cutoff));
                }
                // There is a method for calculating the mean propagation distance exactly, taking into account macrofracture interaction with another fracture set with semi-regular distribution
                // This is only strictly valid for two fracture sets                    
                // However if there are more than two sets, but one set is dominant (i.e. if its P32 density, as a proportion of total P32 density, exceeds the user-defined anisotropy cutoff value), 
                // we can make an approximation by taking account of semi-regular macrofracture interaction probabilities for the fracture set with the highest density only
                else if ((maxP32/totalP32) > gbc.PropControl.anisotropyCutoff)
                {
                    // Get the angle of intersection between the two fracture sets
                    double fs_fb_intersectionAngle = Strike - fb_fs.Strike;

                    // We assume that fractures in all sets other than the highest density set (and this set) are distributed randomly
                    // Therefore the instantaneous probability of intersection as a function of distance is just the mean linear density corrected for intersection angle
                    double F_byDistance = FII_byDistance;
                    foreach (Gridblock_FractureSet fs in gbc.FractureSets)
                        if ((fs != this) && (fs != fb_fs))
                        {
                            double intersectionAngleSin = Math.Abs(VectorXYZ.Sin_trim(Strike - fs.Strike));
                            double fsP32 = 0;
                            foreach (FractureDipSet fds in fs.FractureDipSets)
                                fsP32 += (UseCurrentData ? fds.getTotalMFP32() : fds.getTotalMFP32(Timestep_Mminus1));
                            F_byDistance += (fsP32 * intersectionAngleSin);
                        }

                    if (gbc.PropControl.checkAlluFStressShadows)
                    {
                        // If stress shadows from other fracture sets can control fracture nucleation, we must take this into account when calculating the mean lengths
                        double minimumPropagationDistance = gbc.getCrossFSExclusionZoneWidth(fb_fs, this, mp_fds, Timestep_M) / 2;
                        meanPropagationDistances = fb_fs.Calculate_MeanPropagationDistance_IntersectingFracture(F_byDistance, fs_fb_intersectionAngle, minimumPropagationDistance, Timestep_M, cutoffLengths);
                    }
                    else
                    {
                        // If other fractures sets do not affect fracture nucleation, we can use the standard formula to calculate the mean lengths
                        meanPropagationDistances = fb_fs.Calculate_MeanPropagationDistance_IntersectingFracture(F_byDistance, fs_fb_intersectionAngle, Timestep_M, cutoffLengths);
                    }

                }
                // If there are several other fracture sets with significant populations, we must use a numerical integration method to calculate approximate mean lengths
                // The function here uses a quadratic approximation for the integral of -l x dPhi/dl
                else
                {
                    meanPropagationDistances = Calculate_MeanPropagationDistance_IntersectingFractures(FII_byDistance, mp_fds, Timestep_M, cutoffLengths);
                }
            }

            // Return the calculated mean propagation distance
            return meanPropagationDistances;
        }
        /// <summary>
        /// Get the mean propagation distance for a fracture from another set propagating towards this set
        /// </summary>
        /// <param name="FII_byDistance">Instantaneous probability of stress shadow interaction of the inward propagating fracture, divided by propagation rate</param>
        /// <param name="IntersectionAngle">Angle between the propagating fracture and the set it is propagating towards</param>
        /// <returns></returns>
        private double Calculate_MeanPropagationDistance_IntersectingFracture(double FII_byDistance, double IntersectionAngle)
        {
            return Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, IntersectionAngle, -1, new List<double>() { 0 })[0];
        }
        /// <summary>
        /// Get the mean propagation distance for a fracture from another set propagating towards this set, where the propagating fracture nucleated a minimum distance from the nearest fracture of this set
        /// </summary>
        /// <param name="FII_byDistance">Instantaneous probability of stress shadow interaction of the inward propagating fracture, divided by propagation rate</param>
        /// <param name="IntersectionAngle">Angle between the propagating fracture and the set it is propagating towards</param>
        /// <param name="MinimumPropagationDistance">The minimum distance of between the nucleation point of the propagating fracture and the nearest fracture from this set</param>
        /// <returns></returns>
        private double Calculate_MeanPropagationDistance_IntersectingFracture(double FII_byDistance, double IntersectionAngle, double MinimumPropagationDistance)
        {
            return Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, IntersectionAngle, MinimumPropagationDistance, -1, new List<double>() { 0 })[0];
        }
        /// <summary>
        /// Get the mean propagation distance for a fracture from an oblique set propagating towards this set during a specified previous timestep M, subject to specified minimum cutoffs
        /// These values give the normalised cumulative lengths of all fractures longer than the cutoffs
        /// Multiplying these values by the total volumetric density of residual fractures will give the total lengths of all residual fractures longer than the cutoffs
        /// </summary>
        /// <param name="FII_byDistance">Instantaneous probability of stress shadow interaction of the inward propagating fracture, divided by propagation rate</param>
        /// <param name="IntersectionAngle">Angle between the propagating fracture and the set it is propagating towards</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating; set to -1 to use current data</param>
        /// <param name="cutoffLength">List of cutoff lengths</param>
        /// <returns></returns>
        private List<double> Calculate_MeanPropagationDistance_IntersectingFracture(double FII_byDistance, double IntersectionAngle, int Timestep_M, List<double> cutoffLengths)
        {
            // Get the total number of supplied cutoff values
            int noCutoffs = cutoffLengths.Count;

            // Create a new list for the output values
            List<double> meanPropagationDistances = new List<double>();

            // Get the sine of the intersecting angle
            // We will perform the calculation by projecting the propagating fracture onto a line perpendicular to the set it is propagating towards
            // At the end of the calculation, we can convert it back to a distance parallel to the propagating fracture
            // NB If the intersection angle is 0, the propagating fracture will never intersect a fracture from the other set so we can return a value based on FII_dashed
            double sinIntersectionAngle = Math.Abs(VectorXYZ.Sin_trim(IntersectionAngle));
            if (sinIntersectionAngle == 0)
            {
                // Calculate mean propagation distance based on FII only for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    double meanPropagationDistance = ((1 / FII_byDistance) + cutoff) * Math.Exp(-FII_byDistance * cutoff);
                    meanPropagationDistances.Add(meanPropagationDistance);
                }
                return meanPropagationDistances;
            }

            // We will need to modify the supplied cutoff distances
            // NB we will need to copy this to a local list object to avoid changing the cutoff distances in the FractureDipSet cutoff list
            List<double> relativeCutoffLengths = new List<double>();
            for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                relativeCutoffLengths.Add(cutoffLengths[cutoffNo] * sinIntersectionAngle);

            // Since FII_dashed is the instantaneous probability of fracture deactivation as a function of distance, this will need to be modified also
            FII_byDistance /= sinIntersectionAngle;

            // Get the mean propagation distances projected onto a line perpendicular to the strike of the intersecting fractures
            meanPropagationDistances = Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, Timestep_M, relativeCutoffLengths);

            // Project the mean propagation distances back onto the propagating fracture
            for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                meanPropagationDistances[cutoffNo] /= sinIntersectionAngle;

            // Return mean propagation distances
            return meanPropagationDistances;
        }
        /// <summary>
        /// Get the mean propagation distance for fractures from another set propagating towards this set during a specified previous timestep M, subject to specified minimum cutoffs and a minimum nucleation distance from the nearest fracture of this set
        /// These values give the normalised cumulative lengths of all fractures which nucleate further than a specified distance from the nearest fracture of this set, and which are longer than the cutoffs
        /// Multiplying these values by the total volumetric density of residual fractures will give the total lengths of all residual fractures which nucleated in the specified zones and which are longer than the cutoffs
        /// </summary>
        /// <param name="FII_byDistance">Instantaneous probability of stress shadow interaction of the inward propagating fracture, divided by propagation rate</param>
        /// <param name="IntersectionAngle">Angle between the propagating fracture and the set it is propagating towards</param>
        /// <param name="MinimumPropagationDistance">The minimum distance of between the nucleation point of the propagating fracture and the nearest fracture from this set, measured perpendicular to the strike of this set</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating; set to -1 to use current data</param>
        /// <param name="cutoffLength">List of cutoff lengths, measured parallel to the propagating fracture strike (independent of the minimum distance of the nucleation point)</param>
        /// <returns></returns>
        private List<double> Calculate_MeanPropagationDistance_IntersectingFracture(double FII_byDistance, double IntersectionAngle, double MinimumPropagationDistance, int Timestep_M, List<double> cutoffLengths)
        {
            // Get the index number of timestep M-1, so we can retrieve Psi, MFP32, AA and BB values for the end of the previous timestep
            // If a valid timestep number is not supplied, use the current data
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the total number of supplied cutoff values
            int noCutoffs = cutoffLengths.Count;

            // Create a new list for the output values
            List<double> meanPropagationDistances = new List<double>();

            // Get the sine of the intersecting angle
            // We will perform the calculation by projecting the propagating fracture onto a line perpendicular to the set it is propagating towards
            // At the end of the calculation, we can convert it back to a distance parallel to the propagating fracture
            // NB If the intersection angle is 0, the propagating fracture will never intersect a fracture from the other set so we can return a value based on FII_dashed
            // This will also be independent of the minimum distance from the nearest set J fracture
            double sinIntersectionAngle = Math.Abs(VectorXYZ.Sin_trim(IntersectionAngle));
            if (sinIntersectionAngle == 0)
            {
                // Calculate mean propagation distance based on FII only for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    double meanPropagationDistance = ((1 / FII_byDistance) + cutoff) * Math.Exp(-FII_byDistance * cutoff);
                    meanPropagationDistances.Add(meanPropagationDistance);
                }
                return meanPropagationDistances;
            }

            // We will need to modify the supplied cutoff distances
            // NB we will need to copy this to a local list object to avoid changing the cutoff distances in the FractureDipSet cutoff list
            List<double> relativeCutoffLengths = new List<double>();
            for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                relativeCutoffLengths.Add(cutoffLengths[cutoffNo] * sinIntersectionAngle);

            // Since FII_dashed is the instantaneous probability of fracture deactivation as a function of distance, this will need to be modified also
            FII_byDistance /= sinIntersectionAngle;

            // The minimum distance from the intersecting fractures will not change, as this already measured perpendicular to these fractures

            // Get the linear macrofracture density at the end of the previous timestep, by looping through all dipsets
            double MFP32_J;
            if (UseCurrentData)
            {
                MFP32_J = combined_T_MFP32_total();
            }
            else
            {
                MFP32_J = 0;
                foreach (FractureDipSet fds in FractureDipSets)
                {
                    double fds_MFP32 = fds.getTotalMFP32(Timestep_Mminus1);
                    double fds_Psi = fds_MFP32 * fds.getMeanStressShadowWidth(Timestep_Mminus1);
                    MFP32_J += fds_MFP32;
                }
            }

            if (MFP32_J <= 0) // If there are no perpendicular fractures to intersect, then use simplified version of the formula based on rates of deactivation due to stress shadow interaction only
            {
                // Calculate mean propagation distance based on FII only for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = relativeCutoffLengths[cutoffNo];
                    double meanPropagationDistance = ((1 / FII_byDistance) + cutoff) * Math.Exp(-FII_byDistance * cutoff);
                    meanPropagationDistances.Add(meanPropagationDistance);
                }
            }
            else if (FII_byDistance < (MFP32_J / 1000)) // If the likelihood of interacting with a stress shadow is negligible compared to the likelihood of intersecting a fracture, then use simplified version of the formula based on rates of deactivation due to fracture intersection only
            {
                // Get the probability that a fracture nucleating anywhere will reach the minimum distance a fracture can nucleate from a set J fracture
                double PhiIJMinDist;
                if (UseCurrentData)
                    PhiIJMinDist = getInverseProximityZoneVolume(MinimumPropagationDistance);
                else
                    PhiIJMinDist = getInverseProximityZoneVolume(Timestep_M, new List<double> { MinimumPropagationDistance })[0];

                // Get the mean propagation distance for a fracture nucleating at the minimum distance from a set J fracture
                double lMean_MinDist = Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, Timestep_M, new List<double> { MinimumPropagationDistance })[0];
                double lMeanPhi_MinDist = lMean_MinDist / PhiIJMinDist;

                // Get the mean propagation distances for fractures nucleating anywhere
                meanPropagationDistances = Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, Timestep_M, relativeCutoffLengths);

                // Modify the list of mean propagation distances to account for restrictions on the fracture nucleation location
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = relativeCutoffLengths[cutoffNo];
                    if (cutoff < MinimumPropagationDistance)
                        meanPropagationDistances[cutoffNo] = lMeanPhi_MinDist;
                    else
                        meanPropagationDistances[cutoffNo] /= PhiIJMinDist;
                }
            }
            else // Otherwise we need to use the full version version based on Phi_II and Phi_IJ
            {
                // Get the probability that a fracture nucleating anywhere will reach the minimum distance a fracture can nucleate from a set J fracture
                double PhiIJMinDist;
                if (UseCurrentData)
                    PhiIJMinDist = getInverseProximityZoneVolume(MinimumPropagationDistance);
                else
                    PhiIJMinDist = getInverseProximityZoneVolume(Timestep_M, new List<double> { MinimumPropagationDistance })[0];

                // Get the mean propagation distance for a fracture nucleating at the minimum distance from a set J fracture
                double lMean_MinDist = Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, Timestep_M, new List<double> { MinimumPropagationDistance })[0];
                double lMeanPhi_MinDist = lMean_MinDist / PhiIJMinDist;

                // Get the upper bound for the propagation distance integral below the minimum distance from a set J fracture
                double integralUpperBound = ((1 / FII_byDistance) + MinimumPropagationDistance) * Math.Exp(-FII_byDistance * MinimumPropagationDistance);

                // Get the mean propagation distances for fractures nucleating anywhere
                meanPropagationDistances = Calculate_MeanPropagationDistance_IntersectingFracture(FII_byDistance, Timestep_M, relativeCutoffLengths);

                // Modify the list of mean propagation distances to account for restrictions on the fracture nucleation location
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = relativeCutoffLengths[cutoffNo];
                    if (cutoff < MinimumPropagationDistance)
                        meanPropagationDistances[cutoffNo] = (((1 / FII_byDistance) + cutoff) * Math.Exp(-FII_byDistance * cutoff)) - integralUpperBound + lMeanPhi_MinDist;
                    else
                        meanPropagationDistances[cutoffNo] /= PhiIJMinDist;
                }
            }

            // Project the mean propagation distances back onto the propagating fracture
            for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                meanPropagationDistances[cutoffNo] /= sinIntersectionAngle;

            // Return mean propagation distances
            return meanPropagationDistances;
        }
        /// <summary>
        /// Get the mean propagation distance for a fracture from a perpendicular set propagating towards this set during a specified previous timestep M, subject to specified minimum cutoffs
        /// These values give the normalised cumulative lengths of all fractures longer than the cutoffs
        /// Multiplying these values by the total volumetric density of residual fractures will give the total lengths of all residual fractures longer than the cutoffs
        /// </summary>
        /// <param name="FII_byDistance">Instantaneous probability of stress shadow interaction of the inward propagating fracture, divided by propagation rate</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating; set to -1 to use current data</param>
        /// <param name="cutoffLength">List of cutoff lengths</param>
        /// <returns></returns>
        private List<double> Calculate_MeanPropagationDistance_IntersectingFracture(double FII_byDistance, int Timestep_M, List<double> cutoffLengths)
        {
            // Get the index number of timestep M-1, so we can retrieve Psi, MFP32, AA and BB values for the end of the previous timestep
            // If a valid timestep number is not supplied, use the current data
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the total number of supplied cutoff values
            int noCutoffs = cutoffLengths.Count;

            // Create a new list for the output values and populate it with zero values
            List<double> meanPropagationDistances = new List<double>();
            foreach (double cutoff in cutoffLengths)
                meanPropagationDistances.Add(0);

            // Get the total stress shadow volume and linear macrofracture density at the end of the previous timestep, by looping through all dipsets
            // Then use these to calculate average stress shadow width
            double Psi_J, MFP32_J, Wav_J;
            if (UseCurrentData)
            {
                Psi_J = combined_MF_StressShadowVolume();
                MFP32_J = combined_T_MFP32_total();
                Wav_J = average_Mean_MF_StressShadowWidth();
            }
            else
            {
                Psi_J = 0;
                MFP32_J = 0;
                foreach (FractureDipSet fds in FractureDipSets)
                {
                    double fds_MFP32 = fds.getTotalMFP32(Timestep_Mminus1);
                    double fds_Psi = fds_MFP32 * fds.getMeanStressShadowWidth(Timestep_Mminus1);
                    MFP32_J += fds_MFP32;
                    Psi_J += fds_Psi;
                }
                if (Psi_J > 1) Psi_J = 1;
                Wav_J = (MFP32_J > 0 ? Psi_J / MFP32_J : 0);

                // We will also sort the dip sets by stress shadow width, since the order may vary between timesteps
                FractureDipSets_SortByW(Timestep_Mminus1);
            }

            // Cache the total number of fracture dip sets
            int noDipsets = FractureDipSets_SortedByW.Count;

            if (MFP32_J <= 0) // If there are no perpendicular fractures to intersect, then use simplified version of the formula based on rates of deactivation due to stress shadow interaction only
            {
                // Calculate mean propagation distance based on FII only for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    double meanPropagationDistance = ((1 / FII_byDistance) + cutoff) * Math.Exp(-FII_byDistance * cutoff);
                    meanPropagationDistances[cutoffNo] = meanPropagationDistance;
                }
            }
            else if (FII_byDistance < (MFP32_J / 1000)) // If the likelihood of interacting with a stress shadow is negligible compared to the likelihood of intersecting a fracture, then use simplified version of the formula based on rates of deactivation due to fracture intersection only
            {
                // Create variables for AAr, BBr, CCr, Wr, AAr-1 and Wr-1
                double AA_r = 0;
                double BB_r = 0;
                double CC_r = 0;
                double W_r = 0;
                double AA_rminus1 = 0;
                double W_rminus1 = double.PositiveInfinity;

                // Create a flag to use a linear approximation for S(x)
                bool useLinearApprox = false;

                // Loop through all the dipsets adding integral terms
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Get a reference to dipset r
                    FractureDipSet Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                    // Get values for AAr, BBr, CCr+1-CCr and Wr
                    if (UseCurrentData)
                    {
                        AA_r = Dipset_r.getAA();
                        BB_r = Dipset_r.getBB();
                        W_r = Dipset_r.getMeanStressShadowWidth();
                    }
                    else
                    {
                        AA_r = Dipset_r.getAA(Timestep_Mminus1);
                        BB_r = Dipset_r.getBB(Timestep_Mminus1);
                        W_r = Dipset_r.getMeanStressShadowWidth(Timestep_Mminus1);
                    }

                    // If CC_r is very large and BB_r is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                    useLinearApprox = (float)CC_r == (float)(CC_r + BB_r);

                    // Calculate the standard increment and decrement terms for dipset r
                    double mpd_increment_r, mpd_decrement_r;
                    double W_rminus1_Wav = W_rminus1 + Wav_J;
                    double W_r_Wav = W_r + Wav_J;
                    double dW = W_rminus1 - W_r;
                    double AA_W_term = 0;
                    if (useLinearApprox)
                    {
                        // For the first dipset, Wr-1 is infinite so we cannot use the linear approximation for the main integral term
                        if (DipsetNo_r > 0)
                        {
                            AA_W_term = (dW > 0 ? (AA_r - AA_rminus1) / dW : 0);
                            // Calculate the lower limit (positive component) of the integral term for dipset r
                            {
                                mpd_increment_r = (AA_W_term / 6) * ((2 * Math.Pow(W_r_Wav, 3)) - (3 * W_rminus1_Wav * Math.Pow(W_r_Wav, 2)));
                                mpd_increment_r += (AA_rminus1 / 2) * Math.Pow(W_r_Wav, 2);
                            }
                            // Calculate the upper limit (negative component) of the integral term for dipset r
                            {
                                mpd_decrement_r = (AA_W_term / 6) * -Math.Pow(W_rminus1_Wav, 3);
                                mpd_decrement_r += (AA_rminus1 / 2) * Math.Pow(W_rminus1_Wav, 2);
                            }
                        }
                        else
                        {
                            // We should never need to use the linear approximation for the first dipset, but if so we will assume AA1 is approximately zero so there will be no increments
                            mpd_increment_r = 0;
                            mpd_decrement_r = 0;
                        }
                    }
                    else
                    {
                        // Calculate the lower limit (positive component) of the integral term for dipset r
                        {
                            // Lower limit to the main integral term for dipset r
                            mpd_increment_r = (AA_r / BB_r) * (W_r_Wav + (1 / BB_r));

                            // CCr/BBr term
                            mpd_increment_r += (CC_r / BB_r) * W_r_Wav;

                            // CCrl integral term
                            mpd_increment_r += (CC_r / 2) * Math.Pow(W_r_Wav, 2);
                        }
                        // The upper limit (negative component) of the integral term is zero for the first dipset
                        if (DipsetNo_r > 0)
                        {
                            // Upper limit to the main integral term for dipset r
                            mpd_decrement_r = (AA_rminus1 / BB_r) * (W_rminus1_Wav + (1 / BB_r));

                            // CCr/BBr term
                            mpd_decrement_r += (CC_r / BB_r) * W_rminus1_Wav;

                            // CCrl integral term
                            mpd_decrement_r += (CC_r / 2) * Math.Pow(W_rminus1_Wav, 2);
                        }
                        else
                        {
                            mpd_decrement_r = 0;
                        }
                    }

                    // Add integral terms for dipset r for each of the specified length cutoffs
                    for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                    {
                        double cutoff = cutoffLengths[cutoffNo];
                        if (cutoff < W_rminus1_Wav)
                        {
                            // Add the main integral and CCrl terms for dipset r
                            if (cutoff <= W_r_Wav)
                            {
                                meanPropagationDistances[cutoffNo] += mpd_increment_r;
                            }
                            else
                            {
                                // If the cutoff is greater than Wr+Wav, then it will form the lower limit to the integral, and we will need to calculate the increment term independently 
                                double mpd_increment_q;
                                if (useLinearApprox)
                                {
                                    // For the first dipset, Wr-1 is infinite so we cannot use the linear approximation for the main integral term
                                    if (DipsetNo_r > 0)
                                    {
                                        // Calculate the lower limit (positive component) of the integral term for dipset r
                                        mpd_increment_q = (AA_W_term / 6) * ((2 * Math.Pow(cutoff, 3)) - (3 * W_rminus1_Wav * Math.Pow(cutoff, 2)));
                                        mpd_increment_q += (AA_rminus1 / 2) * Math.Pow(cutoff, 2);
                                    }
                                    else
                                    {
                                        // We should never need to use the linear approximation for the first dipset, but if so we will assume AA1 is approximately zero so there will be no increments
                                        mpd_increment_q = 0;
                                    }
                                }
                                else
                                {
                                    // Calculate the lower limit (positive component) of the integral term for the specified cutoff length
                                    {
                                        // Lower limit to the main integral term for dipset r
                                        mpd_increment_q = (AA_r / BB_r) * (cutoff + (1 / BB_r)) * Math.Exp(-BB_r * (cutoff - W_r_Wav));

                                        // CCr/BBr term
                                        mpd_increment_q += (CC_r / BB_r) * cutoff;

                                        // CCrl integral term
                                        mpd_increment_q += (CC_r / 2) * Math.Pow(cutoff, 2);
                                    }
                                }

                                // Add the lower limit (positive component) of the integral term for dipset q
                                meanPropagationDistances[cutoffNo] += mpd_increment_q;
                            }

                            // Subtract the upper limit (negative component) of the integral term for dipset r
                            meanPropagationDistances[cutoffNo] -= mpd_decrement_r;
                        }
                    }

                    // Update AAr-1, Wr-1 and CCr
                    AA_rminus1 = AA_r;
                    W_rminus1 = W_r;
                    if (UseCurrentData)
                        CC_r += Dipset_r.getCCStep();
                    else
                        CC_r += Dipset_r.getCCStep(Timestep_Mminus1);
                }

                // We must calculate a value for BBn+1; NB AAn+1=MFP32, we have already set CCr to CCn+1, and Wn+1 is zero
                // NB If Wn is zero, set BBn+1 = BBn
                if (W_rminus1 > 0)
                    BB_r = (Math.Log(MFP32_J + CC_r) - Math.Log(AA_rminus1 + CC_r)) / W_rminus1;
                // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                if (double.IsNaN(BB_r) || double.IsNegativeInfinity(BB_r))
                    BB_r = double.PositiveInfinity;

                // If CC_n+1 is very large and BB_n+1 is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                useLinearApprox = (float)CC_r == (float)(CC_r + BB_r);

                // Calculate the standard increment and decrement terms for 0 < l-Wav_J < Wn
                double mpd_increment_nplus1, mpd_decrement_nplus1;
                double Wn_Wav = W_rminus1 + Wav_J;
                double MFP32_W_term = 0;
                if (useLinearApprox)
                {
                    MFP32_W_term = (W_rminus1 > 0 ? (MFP32_J - AA_rminus1) / W_rminus1 : 0);
                    // Calculate the lower limit (positive component) of the integral term for 0 < l-Wav_J < Wn
                    {
                        mpd_increment_nplus1 = (MFP32_W_term / 6) * ((2 * Math.Pow(Wav_J, 3)) - (3 * Wn_Wav * Math.Pow(Wav_J, 2)));
                        mpd_increment_nplus1 += (AA_rminus1 / 2) * Math.Pow(Wav_J, 2);
                    }
                    // Calculate the upper limit (negative component) of the integral term for 0 < l-Wav_J < Wn
                    {
                        mpd_decrement_nplus1 = (MFP32_W_term / 6) * -Math.Pow(Wn_Wav, 3);
                        mpd_decrement_nplus1 += (AA_rminus1 / 2) * Math.Pow(Wn_Wav, 2);
                    }
                }
                else
                {
                    // Calculate the lower limit (positive component) of the integral term for 0 < l-Wav_J < Wn
                    {
                        // Lower limit to the main integral term for dipset r
                        mpd_increment_nplus1 = (MFP32_J / BB_r) * (Wav_J + (1 / BB_r));

                        // CCr/BBr term
                        mpd_increment_nplus1 += (CC_r / BB_r) * Wav_J;

                        // CCrl integral term
                        mpd_increment_nplus1 += (CC_r / 2) * Math.Pow(Wav_J, 2);
                    }
                    // Calculate the upper limit (negative component) of the integral term for 0 < l-Wav_J < Wn
                    {
                        // Upper limit to the main integral term for dipset r
                        mpd_decrement_nplus1 = (AA_rminus1 / BB_r) * (Wn_Wav + (1 / BB_r));

                        // CCr/BBr term
                        mpd_decrement_nplus1 += (CC_r / BB_r) * Wn_Wav;

                        // CCrl integral term
                        mpd_decrement_nplus1 += (CC_r / 2) * Math.Pow(Wn_Wav, 2);
                    }
                }

                // Add the main integral and CCrl terms for 0 < l-Wav_J < Wn for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    if (cutoff < Wn_Wav)
                    {
                        // Add the main integral and CCrl terms for 0 < l-Wav_J < Wn
                        if (cutoff <= Wav_J)
                        {
                            meanPropagationDistances[cutoffNo] += mpd_increment_nplus1;
                        }
                        else
                        {
                            // If the cutoff is greater than Wav, then it will form the lower limit to the integral, and we will need to calculate the increment term independently 
                            double mpd_increment_q;
                            if (useLinearApprox)
                            {
                                // Calculate the lower limit (positive component) of the integral term for 0 < l-Wav_J < Wn
                                mpd_increment_q = (MFP32_W_term / 6) * ((2 * Math.Pow(cutoff, 3)) - (3 * Wn_Wav * Math.Pow(cutoff, 2)));
                                mpd_increment_q += (AA_rminus1 / 2) * Math.Pow(cutoff, 2);
                            }
                            else
                            {
                                // Lower limit to the main integral term for 0 < l-Wav_J < Wn
                                mpd_increment_q = (MFP32_J / BB_r) * (cutoff + (1 / BB_r)) * Math.Exp(-BB_r * (cutoff - Wav_J));

                                // CCr/BBr term
                                mpd_increment_q += (CC_r / BB_r) * cutoff;

                                // CCrl integral term
                                mpd_increment_q += (CC_r / 2) * Math.Pow(cutoff, 2);
                            }

                            // Add the lower limit (positive component) of the integral term for dipset q
                            meanPropagationDistances[cutoffNo] += mpd_increment_q;
                        }

                        // Subtract the upper limit (negative component) of the integral term for dipset r
                        meanPropagationDistances[cutoffNo] -= mpd_decrement_nplus1;
                    }
                }

                // Calculate standard increment term for l < Wav_J
                double mpd_increment_Wav = 0.5 * Psi_J * Wav_J;

                // Add integral term for l < Wav_J for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    if (cutoff < Wav_J)
                    {
                        meanPropagationDistances[cutoffNo] += mpd_increment_Wav;
                        if (cutoff > 0)
                            meanPropagationDistances[cutoffNo] -= 0.5 * MFP32_J * Math.Pow(cutoff, 2);
                    }
                }
            }
            else // Otherwise we need to use the full version version based on Phi_II and Phi_IJ
            {
                // Create variables for AAr, BBr, CCr, Wr, AAr-1 and Wr-1
                double AA_r = 0;
                double BB_r = 0;
                double CC_r = 0;
                double W_r = 0;
                double AA_rminus1 = 0;
                double W_rminus1 = double.PositiveInfinity;

                // Create a flag to use a linear approximation for S(x)
                bool useLinearApprox = false;

                // Create a variable for the Sigma[(AAs-AAs-1)/BBs - CCs(Ws-Ws-1)] series - initially this will be zero
                double Sigma_AAs_AAsminus1_BBs_CCs_W = 0;

                // Loop through all the dipsets adding integral terms
                for (int DipsetNo_r = 0; DipsetNo_r < noDipsets; DipsetNo_r++)
                {
                    // Get reference to dipset r
                    FractureDipSet Dipset_r = FractureDipSets_SortedByW[DipsetNo_r];

                    // Get values for AAr, BBr, CCr+1-CCr and Wr
                    if (UseCurrentData)
                    {
                        AA_r = Dipset_r.getAA();
                        BB_r = Dipset_r.getBB();
                        W_r = Dipset_r.getMeanStressShadowWidth();
                    }
                    else
                    {
                        AA_r = Dipset_r.getAA(Timestep_Mminus1);
                        BB_r = Dipset_r.getBB(Timestep_Mminus1);
                        W_r = Dipset_r.getMeanStressShadowWidth(Timestep_Mminus1);
                    }

                    // If CC_r is very large and BB_r is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                    useLinearApprox = (float)CC_r == (float)(CC_r + BB_r);

                    // Calculate the standard increment and decrement terms for dipset r
                    double mpd_increment_r, mpd_decrement_r;
                    double W_rminus1_Wav = W_rminus1 + Wav_J;
                    double W_r_Wav = W_r + Wav_J;
                    double dW = W_rminus1 - W_r;
                    double AA_W_term = 0;
                    if (useLinearApprox)
                    {
                        // For the first dipset, Wr-1 is infinite so we cannot use the linear approximation for the main integral term
                        if (DipsetNo_r > 0)
                        {
                            AA_W_term = (dW > 0 ? (AA_r - AA_rminus1) / dW : 0);
                            // Calculate the lower limit (positive component) of the main integral term for dipset r
                            {
                                mpd_increment_r = (AA_rminus1 - (AA_W_term * W_rminus1_Wav)) * Math.Pow(FII_byDistance, -2);
                                mpd_increment_r += (Sigma_AAs_AAsminus1_BBs_CCs_W + (AA_W_term * Math.Pow(FII_byDistance, -2)) + ((AA_r + AA_rminus1) * dW / 2)) * (W_r_Wav + (1 / FII_byDistance));
                                mpd_increment_r *= Math.Exp(-FII_byDistance * W_r_Wav);
                            }
                            // Calculate the upper limit (negative component) of the integral term for dipset r
                            {
                                mpd_decrement_r = (AA_rminus1 - (AA_W_term * W_rminus1_Wav)) * Math.Pow(FII_byDistance, -2);
                                mpd_decrement_r += (Sigma_AAs_AAsminus1_BBs_CCs_W + (AA_W_term * Math.Pow(FII_byDistance, -2))) * (W_rminus1_Wav + (1 / FII_byDistance));
                                mpd_decrement_r *= Math.Exp(-FII_byDistance * W_rminus1_Wav);
                            }
                        }
                        else
                        {
                            // We should never need to use the linear approximation for the first dipset, but if so we will assume AA1 is approximately zero so there will be no increments
                            mpd_increment_r = 0;
                            mpd_decrement_r = 0;
                        }
                    }
                    else
                    {
                        // Calculate the lower limit (positive component) of the main integral term for dipset r
                        {
                            double CCr_dW = (double.IsInfinity(dW) ? 0 : CC_r * dW);
                            mpd_increment_r = ((AA_r + CC_r) / BB_r) * (W_r_Wav + (1 / (BB_r + FII_byDistance)));
                            mpd_increment_r += CC_r * Math.Pow(FII_byDistance, -2);
                            mpd_increment_r += (Sigma_AAs_AAsminus1_BBs_CCs_W - ((AA_rminus1 + CC_r) / BB_r) - CCr_dW) * (W_r_Wav + (1 / FII_byDistance));
                            mpd_increment_r *= Math.Exp(-FII_byDistance * (W_r + Wav_J));
                        }
                        // The upper limit (negative component) of the integral term is zero for the first dipset
                        if (DipsetNo_r > 0)
                        {
                            mpd_decrement_r = ((AA_rminus1 + CC_r) / BB_r) * (W_rminus1_Wav + (1 / (BB_r + FII_byDistance)));
                            mpd_decrement_r += CC_r * Math.Pow(FII_byDistance, -2);
                            mpd_decrement_r += (Sigma_AAs_AAsminus1_BBs_CCs_W - ((AA_rminus1 + CC_r) / BB_r)) * (W_rminus1_Wav + (1 / FII_byDistance));
                            mpd_decrement_r *= Math.Exp(-FII_byDistance * W_rminus1_Wav);
                        }
                        else
                        {
                            mpd_decrement_r = 0;
                        }
                    }

                    // Add integral terms for dipset r for each of the specified length cutoffs
                    for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                    {
                        double cutoff = cutoffLengths[cutoffNo];
                        if (cutoff < W_rminus1_Wav)
                        {
                            // Add the lower limit (positive component) of the integral term for dipset r
                            if (cutoff <= W_r_Wav)
                            {
                                // If the cutoff is less than Wr+Wav, then the lower limit to the integral will be Wr+Wav and we can use the standard increment term for dipset r 
                                meanPropagationDistances[cutoffNo] += mpd_increment_r;
                            }
                            else
                            {
                                // If the cutoff is greater than Wr+Wav, then it will form the lower limit to the integral, and we will need to calculate the increment term independently 
                                double mpd_increment_q;
                                double W_cutoff_term = W_rminus1_Wav - cutoff;
                                if (useLinearApprox)
                                {
                                    // For the first dipset, Wr-1 is infinite so we cannot use the linear approximation for the main integral term
                                    if (DipsetNo_r > 0)
                                    {
                                        // Calculate the lower limit (positive component) of the main integral term for the specified cutoff length
                                        mpd_increment_q = (AA_rminus1 - (AA_W_term * W_rminus1_Wav)) * Math.Pow(FII_byDistance, -2);
                                        mpd_increment_q += (Sigma_AAs_AAsminus1_BBs_CCs_W + (AA_W_term * Math.Pow(FII_byDistance, -2)) + (AA_W_term * Math.Pow(W_cutoff_term, 2) / 2) + (AA_rminus1 * W_cutoff_term)) * (cutoff + (1 / FII_byDistance));
                                        mpd_increment_q *= Math.Exp(-FII_byDistance * cutoff);
                                    }
                                    else
                                    {
                                        // We should never need to use the linear approximation for the first dipset, but if so we will assume AA1 is approximately zero so there will be no increments
                                        mpd_increment_q = 0;
                                    }
                                }
                                else
                                {
                                    // Calculate the lower limit (positive component) of the main integral term for the specified cutoff length
                                    {
                                        double CCr_W_cutoff = (double.IsInfinity(W_cutoff_term) ? 0 : CC_r * W_cutoff_term);
                                        mpd_increment_q = ((AA_r + CC_r) / BB_r) * (cutoff + (1 / (BB_r + FII_byDistance))) * Math.Exp(-BB_r * (cutoff - W_r_Wav));
                                        mpd_increment_q += CC_r * Math.Pow(FII_byDistance, -2);
                                        mpd_increment_q += (Sigma_AAs_AAsminus1_BBs_CCs_W - ((AA_rminus1 + CC_r) / BB_r) - CCr_W_cutoff) * (cutoff + (1 / FII_byDistance));
                                        mpd_increment_q *= Math.Exp(-FII_byDistance * cutoff);
                                    }
                                }

                                // Add the lower limit (positive component) of the integral term for dipset q
                                meanPropagationDistances[cutoffNo] += mpd_increment_q;
                            }

                            // Subtract the upper limit (negative component) of the integral term for dipset r
                            meanPropagationDistances[cutoffNo] -= mpd_decrement_r;
                        }
                    }

                    // Update the Sigma[(AAs-AAs-1)/BBs - CCs(Ws-Ws-1)] series variable
                    if (useLinearApprox)
                    {
                        // For the first dipset, Wr-1 and hence dW are infinite
                        // We should never need to use the linear approximation for the first dipset, but if so we can set the Sigma[(AAs-AAs-1)/BBs - CCs(Ws-Ws-1)] series increment to 1
                        Sigma_AAs_AAsminus1_BBs_CCs_W += (!double.IsInfinity(dW) ? (AA_r + AA_rminus1) * dW / 2 : 1);
                    }
                    else
                    {
                        Sigma_AAs_AAsminus1_BBs_CCs_W += (AA_r - AA_rminus1) / BB_r;
                        // For the first dipset, Wr-1 and hence dW are infinite; however CCr should be zero so we can ignore this term
                        Sigma_AAs_AAsminus1_BBs_CCs_W -= (!double.IsInfinity(dW) ? CC_r * dW : 0);
                    }

                    // Update AAr-1, Wr-1 and CCr
                    AA_rminus1 = AA_r;
                    W_rminus1 = W_r;
                    if (UseCurrentData)
                        CC_r += Dipset_r.getCCStep();
                    else
                        CC_r += Dipset_r.getCCStep(Timestep_Mminus1);
                }

                // We must calculate a value for BBn+1; NB AAn+1=MFP32, we have already set CCr to CCn+1, and Wn+1 is zero
                // NB If Wn is zero, set BBn+1 = BBn
                if (W_rminus1 > 0)
                    BB_r = (Math.Log(MFP32_J + CC_r) - Math.Log(AA_rminus1 + CC_r)) / W_rminus1;
                // Check if this produces a NaN or negative infinity - this is possible due to rounding errors; if so set to positive infinity
                if (double.IsNaN(BB_r) || double.IsNegativeInfinity(BB_r))
                    BB_r = double.PositiveInfinity;

                // If CC_n+1 is very large and BB_n+1 is very small, we will use a linear approximation for S(x) to calculate the appropriate terms
                useLinearApprox = (float)CC_r == (float)(CC_r + BB_r);

                // Calculate the standard increment and decrement terms for 0 < l-Wav_J < Wn
                double mpd_increment_nplus1, mpd_decrement_nplus1;
                double Wn_Wav = W_rminus1 + Wav_J;
                double MFP32_W_term = 0;
                if (useLinearApprox)
                {
                    MFP32_W_term = (W_rminus1 > 0 ? (MFP32_J - AA_rminus1) / W_rminus1 : 0);
                    // Calculate the lower limit (positive component) of the main integral term for dipset r
                    {
                        mpd_increment_nplus1 = (AA_rminus1 - (MFP32_W_term * Wn_Wav)) * Math.Pow(FII_byDistance, -2);
                        mpd_increment_nplus1 += (Sigma_AAs_AAsminus1_BBs_CCs_W + (MFP32_W_term * Math.Pow(FII_byDistance, -2)) + ((MFP32_J + AA_rminus1) * (W_rminus1) / 2)) * ((Wav_J) + (1 / FII_byDistance));
                        mpd_increment_nplus1 *= Math.Exp(-FII_byDistance * (Wav_J));
                    }
                    // Calculate the upper limit (negative component) of the integral term for dipset r
                    {
                        mpd_decrement_nplus1 = (AA_rminus1 - (MFP32_W_term * Wn_Wav)) * Math.Pow(FII_byDistance, -2);
                        mpd_decrement_nplus1 += (Sigma_AAs_AAsminus1_BBs_CCs_W + (MFP32_W_term * Math.Pow(FII_byDistance, -2))) * (Wn_Wav + (1 / FII_byDistance));
                        mpd_decrement_nplus1 *= Math.Exp(-FII_byDistance * Wn_Wav);
                    }
                }
                else
                {
                    // Calculate the lower limit (positive component) of the main integral term for dipset r
                    {
                        mpd_increment_nplus1 = ((MFP32_J + CC_r) / BB_r) * ((Wav_J) + (1 / (BB_r + FII_byDistance)));
                        mpd_increment_nplus1 += CC_r * Math.Pow(FII_byDistance, -2);
                        mpd_increment_nplus1 += (Sigma_AAs_AAsminus1_BBs_CCs_W - ((AA_rminus1 + CC_r) / BB_r) - (CC_r * (W_rminus1))) * ((Wav_J) + (1 / FII_byDistance));
                        mpd_increment_nplus1 *= Math.Exp(-FII_byDistance * (Wav_J));
                    }
                    // Calculate the upper limit (negative component) of the integral term for dipset r
                    {
                        mpd_decrement_nplus1 = ((AA_rminus1 + CC_r) / BB_r) * (Wn_Wav + (1 / (BB_r + FII_byDistance)));
                        mpd_decrement_nplus1 += CC_r * Math.Pow(FII_byDistance, -2);
                        mpd_decrement_nplus1 += (Sigma_AAs_AAsminus1_BBs_CCs_W - ((AA_rminus1 + CC_r) / BB_r)) * (Wn_Wav + (1 / FII_byDistance));
                        mpd_decrement_nplus1 *= Math.Exp(-FII_byDistance * Wn_Wav);
                    }
                }

                // Add integral terms for 0 < l-Wav_J < Wn for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    if (cutoff < Wn_Wav)
                    {
                        // Add the lower limit (positive component) of the integral term for 0 < l-Wav_J < Wn
                        if (cutoff <= Wav_J)
                        {
                            // If the cutoff is less than Wav, then the lower limit to the integral will be Wav and we can use the standard increment term for 0 < l-Wav_J < Wn 
                            meanPropagationDistances[cutoffNo] += mpd_increment_nplus1;
                        }
                        else
                        {
                            // If the cutoff is greater than Wav, then it will form the lower limit to the integral, and we will need to calculate the increment term independently 
                            double mpd_increment_q;
                            double W_cutoff_term = Wn_Wav - cutoff;
                            if (useLinearApprox)
                            {
                                // Calculate the lower limit (positive component) of the main integral term for the specified cutoff length
                                mpd_increment_q = (AA_rminus1 - (MFP32_W_term * Wn_Wav)) * Math.Pow(FII_byDistance, -2);
                                mpd_increment_q += (Sigma_AAs_AAsminus1_BBs_CCs_W + (MFP32_W_term * Math.Pow(FII_byDistance, -2)) + (MFP32_W_term * Math.Pow(W_cutoff_term, 2) / 2) + (AA_rminus1 * W_cutoff_term)) * (cutoff + (1 / FII_byDistance));
                                mpd_increment_q *= Math.Exp(-FII_byDistance * cutoff);
                            }
                            else
                            {
                                // Calculate the lower limit (positive component) of the main integral term for the specified cutoff length
                                mpd_increment_q = ((MFP32_J + CC_r) / BB_r) * (cutoff + (1 / (BB_r + FII_byDistance))) * Math.Exp(-BB_r * (cutoff - Wav_J));
                                mpd_increment_q += CC_r * Math.Pow(FII_byDistance, -2);
                                mpd_increment_q += (Sigma_AAs_AAsminus1_BBs_CCs_W - ((AA_rminus1 + CC_r) / BB_r) - (CC_r * W_cutoff_term)) * (cutoff + (1 / FII_byDistance));
                                mpd_increment_q *= Math.Exp(-FII_byDistance * cutoff);
                            }

                            // Add the lower limit (positive component) of the integral term for dipset q
                            meanPropagationDistances[cutoffNo] += mpd_increment_q;
                        }

                        // Subtract the upper limit (negative component) of the integral term for 0 < l-Wav_J < Wn
                        meanPropagationDistances[cutoffNo] -= mpd_decrement_nplus1;
                    }
                }

                // Calculate standard increment term for l < Wav_J
                double mpd_increment_Wav = ((Psi_J * Wav_J) + ((Psi_J / FII_byDistance) - Wav_J) + ((MFP32_J / Math.Pow(FII_byDistance, 2)) - (1 / FII_byDistance))) * Math.Exp(-FII_byDistance * Wav_J);

                // Add integral term for l < Wav_J for each of the specified length cutoffs
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    if (cutoff < Wav_J)
                    {
                        meanPropagationDistances[cutoffNo] += mpd_increment_Wav;
                        if (cutoff <= 0)
                            meanPropagationDistances[cutoffNo] -= ((MFP32_J / Math.Pow(FII_byDistance, 2)) - (1 / FII_byDistance));
                        else
                            meanPropagationDistances[cutoffNo] -= ((MFP32_J * Math.Pow(cutoff, 2)) + (((MFP32_J / FII_byDistance) - 1) * cutoff) + ((MFP32_J / Math.Pow(FII_byDistance, 2)) - (1 / FII_byDistance))) * Math.Exp(-FII_byDistance * cutoff);
                    }
                }
            }

            // Return mean propagation distances
            return meanPropagationDistances;
        }
        /// <summary>
        /// Get the mean propagation distance for a fracture from a this set propagating towards multiple fracture sets during a specified previous timestep M, subject to specified minimum cutoffs
        /// These values give the normalised cumulative lengths of all fractures longer than the cutoffs
        /// Multiplying these values by the total volumetric density of residual fractures will give the total lengths of all residual fractures longer than the cutoffs
        /// </summary>
        /// <param name="FII_byDistance">Instantaneous probability of stress shadow interaction of the propagating fracture, divided by propagation rate</param>
        /// <param name="mp_fds">Reference to the dipset of the propagating fracture</param>
        /// <param name="Timestep_M">Timestep during which the fractures are propagating; set to -1 to use current data</param>
        /// <param name="cutoffLength">List of cutoff lengths</param>
        /// <returns></returns>
        private List<double> Calculate_MeanPropagationDistance_IntersectingFractures(double FII_byDistance, FractureDipSet mp_fds, int Timestep_M, List<double> cutoffLengths)
        {
            // Get the index number of timestep M-1, so we can retrieve Psi, MFP32, AA and BB values for the end of the previous timestep
            // If a valid timestep number is not supplied, use the current data
            int Timestep_Mminus1 = Timestep_M - 1;
            bool UseCurrentData = (Timestep_Mminus1 < 0);

            // Get the total number of supplied cutoff values
            int noCutoffs = cutoffLengths.Count;

            // Create a new list for the output values and populate it with zero values
            List<double> meanPropagationDistances = new List<double>();
            foreach (double cutoff in cutoffLengths)
                meanPropagationDistances.Add(0);

            // Create a list of references to all other fracture sets
            List<Gridblock_FractureSet> otherFractureSets = new List<Gridblock_FractureSet>();
            foreach (Gridblock_FractureSet fs in gbc.FractureSets)
                if (fs != this)
                    otherFractureSets.Add(fs);
            int noOtherFS = otherFractureSets.Count;

            // Create arrays of sine of the angle of intersection, the apparent exclusion zone width seen by dipset mp_fds (i.e. the minimum propagation distance before intersecting another fracture),
            // and the probability that a fracture of this set nucleating anywhere will reach the minimum distance a fracture can nucleate from each of the other fracture sets (PhiIJ_MinPropDist)
            // Also create a list of points to use in the numerical integration and add zero and the minimum propagation distances to this list
            double[] intersectionAngleSins = new double[noOtherFS];
            double[] apparentEZWidths = new double[noOtherFS];
            double[] PhiIJ_MinPropDists = new double[noOtherFS];
            List<double> integrationPoints = new List<double>();
            integrationPoints.Add(0);
            for (int otherFSIndex = 0; otherFSIndex < noOtherFS; otherFSIndex++)
            {
                Gridblock_FractureSet otherFS = otherFractureSets[otherFSIndex];
                double intersectionAngleSin = Math.Abs(VectorXYZ.Sin_trim(Strike - otherFS.Strike));
                intersectionAngleSins[otherFSIndex] = intersectionAngleSin;
                // trueEZWidth represents the mean exclusion zone width for fractures from otherFS as seen by fractures from dipset mp_fds (in this fracture set), measured perpendicular to the strike of otherFS
                double trueEZWidth = gbc.getCrossFSExclusionZoneWidth(otherFS, this, mp_fds, Timestep_M) / 2;
                // apparentEZWidth represents the mean exclusion zone width for fractures from otherFS as seen by fractures from dipset mp_fds (in this fracture set), measured along the strike of this fracture set
                double apparentEZWidth = trueEZWidth / intersectionAngleSin;
                apparentEZWidths[otherFSIndex] = apparentEZWidth;
                integrationPoints.Add(apparentEZWidth);
                // To calculate PhiIJ_MinPropDist we must get the inverse proximity zone volume of the true exclusion zone width, measured perpendicular to strike of the other fracture set
                if (UseCurrentData)
                    PhiIJ_MinPropDists[otherFSIndex] = otherFS.getInverseProximityZoneVolume(trueEZWidth);
                else
                    PhiIJ_MinPropDists[otherFSIndex] = otherFS.getInverseProximityZoneVolume(Timestep_M, new List<double> { trueEZWidth })[0];
            }

            // Calculate the apparent stress shadow widths for every dipset in every other fracture set and add this to the list of integration points
            // NB These are the stress shadow widths seen by other fractures of the same set, projected onto the strike of this fracture set
            // NOT the stress shadow widths as seen by fractures of this set
            // The former control the spacing of the fractures in the other set; the latter the minimum propagation distance of this fracture set from the other set
            for (int otherFSIndex = 0; otherFSIndex < noOtherFS; otherFSIndex++)
            {
                Gridblock_FractureSet otherFS = otherFractureSets[otherFSIndex];
                double intersectionAngleSin = intersectionAngleSins[otherFSIndex];
                foreach (FractureDipSet otherFDS in otherFS.FractureDipSets)
                {
                    double trueStressShadowWidth;
                    if (UseCurrentData)
                        trueStressShadowWidth = otherFDS.Mean_MF_StressShadowWidth;
                    else
                        trueStressShadowWidth = otherFDS.getMeanStressShadowWidth(Timestep_Mminus1);
                    double apparentStressShadowWidth = trueStressShadowWidth / intersectionAngleSin;
                    integrationPoints.Add(apparentStressShadowWidth);
                }
            }

            // Sort the list of integration points into ascending order and remove duplicates
            integrationPoints.Sort();
            for (int IPNo = integrationPoints.Count - 1; IPNo > 0; IPNo--)
            {
                if ((float)integrationPoints[IPNo] == (float)integrationPoints[IPNo - 1])
                    integrationPoints.RemoveAt(IPNo);
            }

            // Add midpoints between each current pair of integration points
            int noPrimaryIntegrationPoints = integrationPoints.Count;
            for (int IPNo = noPrimaryIntegrationPoints - 1; IPNo > 0; IPNo--)
            {
                double midPointValue = (integrationPoints[IPNo] + integrationPoints[IPNo - 1]) / 2;
                integrationPoints.Insert(IPNo, midPointValue);
            }

            // Calculate the value of Phi for each integration point
            int noIntegrationPoints = integrationPoints.Count;
            double[] PhiValues = new double[noIntegrationPoints];
            // Calculate PhiII for each integration point
            for (int IPNo = 0; IPNo < noIntegrationPoints; IPNo++)
            {
                double integrationPoint = integrationPoints[IPNo];
                PhiValues[IPNo] = Math.Exp(-FII_byDistance * integrationPoint);
            }
            // Calculate PhiIJ for each of the other fracture sets for each integration point
            for (int otherFSIndex = 0; otherFSIndex < noOtherFS; otherFSIndex++)
            {
                Gridblock_FractureSet otherFS = otherFractureSets[otherFSIndex];
                double intersectionAngleSin = intersectionAngleSins[otherFSIndex];
                double PhiIJ_MinPropDist = PhiIJ_MinPropDists[otherFSIndex];
                for (int IPNo = 0; IPNo < noIntegrationPoints; IPNo++)
                {
                    double integrationPoint = integrationPoints[IPNo];
                    double orientationCorrectedDistance = integrationPoint * intersectionAngleSin;
                    double unweightedPhiValue;
                    if (UseCurrentData)
                        unweightedPhiValue = otherFS.getInverseProximityZoneVolume(orientationCorrectedDistance);
                    else
                        unweightedPhiValue = otherFS.getInverseProximityZoneVolume(Timestep_M, new List<double> { orientationCorrectedDistance })[0];
                    double weightedPhiValue = (unweightedPhiValue < PhiIJ_MinPropDist ? unweightedPhiValue / PhiIJ_MinPropDist : 1);
                    PhiValues[IPNo] *= weightedPhiValue;
                }
            }

            // For distances greater than the largest integration point we will use an exponential integration
            // For this we need to calculate the distribution coefficient BB_tot, which is the sum of the orientation-adjusted BB1 values for each of the other fracture sets and the FII value for this set
            // The density coefficient AA_tot can be taken as the Phi value for the largest integration point 
            double maxIPValue = integrationPoints[noIntegrationPoints - 1];
            double AA_tot = PhiValues[noIntegrationPoints - 1];
            double BB_tot = FII_byDistance;
            for (int otherFSIndex = 0; otherFSIndex < noOtherFS; otherFSIndex++)
            {
                Gridblock_FractureSet otherFS = otherFractureSets[otherFSIndex];
                FractureDipSet Dipset_1 = otherFS.FractureDipSets_SortedByW[0];
                double intersectionAngleSin = intersectionAngleSins[otherFSIndex];
                double BB_fs = (UseCurrentData ? Dipset_1.getBB() : Dipset_1.getBB(Timestep_Mminus1)) * intersectionAngleSin;
                BB_tot += BB_fs;
            }

            // Now we can loop through the list of cutoff lengths and calculate the integral of Phi from each cutoff to infinity
            // Start with the interval greater than the largest integration point
            {
                // Calculate the full integral of Phi from maxIPvalue to infinity
                double Phi_integral_segment = (BB_tot > 0 ? AA_tot / BB_tot : 0);

                // Loop through each of the cutoffs adding the appropriate integral term
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    if (true)
                    {
                        // Add the integral term for l > maxIPvalue
                        if (cutoff <= maxIPValue)
                        {
                            // If the cutoff is less than maxIPvalue, then we can add the full integral of Phi from maxIPvalue to infinity 
                            meanPropagationDistances[cutoffNo] += Phi_integral_segment;
                        }
                        else
                        {
                            // If the cutoff is greater than maxIPvalue, then we must add the integral of Phi from the cutoff to infinity 
                            double partial_Phi_integral = Phi_integral_segment * Math.Exp(-BB_tot * (cutoff - maxIPValue));
                            meanPropagationDistances[cutoffNo] += partial_Phi_integral;
                        }
                    }
                }
            }
            // Loop through each of the integration intervals, using a quadratic approximation to calculate the integral of Phi within the interval
            for (int IPNo = noIntegrationPoints - 1; IPNo > 1; IPNo -= 2)
            {
                // Get the upper and lower bounds of this integration interval
                // NB as we are using a quadratic approximation, each integration interval contains three integration points, at the upper and lower boundary and in the middle
                double upperBound = integrationPoints[IPNo];
                double midPoint = integrationPoints[IPNo - 1];
                double lowerBound = integrationPoints[IPNo - 2];
                double halfWidth = midPoint - lowerBound;

                // Get the value of Phi for the three integration points relating to this interval
                double upperValue = PhiValues[IPNo];
                double midValue = PhiValues[IPNo - 1];
                double lowerValue = PhiValues[IPNo - 2];

                // Calculate the full integral of Phi across the integration interval
                double Phi_integral_segment = (upperValue + (4 * midValue) + lowerValue) * (halfWidth / 3);

                // Loop through each of the cutoffs adding the appropriate integral term
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    if (cutoff < upperBound)
                    {
                        // Add the integral term for l > maxIPvalue
                        if (cutoff <= lowerBound)
                        {
                            // If the cutoff is less than the lower bound, then we can add the full integral of Phi across the segment 
                            meanPropagationDistances[cutoffNo] += Phi_integral_segment;
                        }
                        else
                        {
                            // If the cutoff is greater than the lower bound, then we muat add the integral of Phi from the cutoff to the upper bound
                            double cutoffRatio = (cutoff - midPoint) / halfWidth;
                            double partial_Phi_integral = (midValue * halfWidth * (1 - cutoffRatio)) + ((upperValue - lowerValue) * (halfWidth / 4) * (1 - Math.Pow(cutoffRatio, 2))) + ((upperValue - (2 * midValue) + lowerValue) * (halfWidth / 6) * (1 - Math.Pow(cutoffRatio, 3)));
                            meanPropagationDistances[cutoffNo] += partial_Phi_integral;
                        }
                    }
                }
            }

            // Finally we can add the terms for l*Phi(l)
            double[] lPhilValues = new double[noCutoffs];
            // Calculate PhiII for each cutoff point
            for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
            {
                double cutoff = cutoffLengths[cutoffNo];
                lPhilValues[cutoffNo] = cutoff * Math.Exp(-FII_byDistance * cutoff);
            }
            // Calculate PhiIJ for each of the other fracture sets for each cutoff point
            for (int otherFSIndex = 0; otherFSIndex < noOtherFS; otherFSIndex++)
            {
                Gridblock_FractureSet otherFS = otherFractureSets[otherFSIndex];
                double intersectionAngleSin = intersectionAngleSins[otherFSIndex];
                double PhiIJ_MinPropDist = PhiIJ_MinPropDists[otherFSIndex];
                for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
                {
                    double cutoff = cutoffLengths[cutoffNo];
                    double orientationCorrectedDistance = cutoff * intersectionAngleSin;
                    double unweightedPhiValue;
                    if (UseCurrentData)
                        unweightedPhiValue = otherFS.getInverseProximityZoneVolume(orientationCorrectedDistance);
                    else
                        unweightedPhiValue = otherFS.getInverseProximityZoneVolume(Timestep_M, new List<double> { orientationCorrectedDistance })[0];
                    double weightedPhiValue = (unweightedPhiValue < PhiIJ_MinPropDist ? unweightedPhiValue / PhiIJ_MinPropDist : 1);
                    lPhilValues[cutoffNo] *= weightedPhiValue;
                }
            }
            for (int cutoffNo = 0; cutoffNo < noCutoffs; cutoffNo++)
            {
                meanPropagationDistances[cutoffNo] += lPhilValues[cutoffNo];
            }

            // Return mean propagation distances
            return meanPropagationDistances;
        }

        // Elastic properties of the fracture set
        /// <summary>
        /// Compliance tensor for this fracture set
        /// </summary>
        public Tensor4_2Sx2S S_set
        {
            get
            {
                Tensor4_2Sx2S s_set = new Tensor4_2Sx2S();
                foreach (FractureDipSet DipSet in FractureDipSets)
                    s_set += DipSet.S_dipset;
                return s_set;
            }
        }

        // Deactivation functions - used to deactivate fracture sets
        /// <summary>
        /// Check if the fracture set meets the specified deactivation criteria, and if so set the evolution stage for all fracture dip sets to Deactivated
        /// </summary>
        /// <param name="historic_a_MFP33_termination_ratio">Ratio of current to maximum active macrofracture volumetric ratio at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio</param>
        /// <param name="active_total_MFP30_termination_ratio">Ratio of active to total macrofracture volumetric density at which fracture sets are considered inactive; calculation will terminate when all fracture sets fall below this ratio</param>
        /// <param name="minimum_ClearZone_Volume">Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated</param>
        /// <param name="minrb_minRad">Maximum radius of microfractures in the smallest bin; calculation will terminate if the extrapolated initial radius of these macrofractures is less than zero (will only apply if b is lees than 2)</param>
        /// <returns>True if at least one dip set within the fracture set is deactivated, and no sets are still growing or residual active</returns>
        public bool CheckFractureDeactivation(double historic_a_MFP33_termination_ratio, double active_total_MFP30_termination_ratio, double minimum_ClearZone_Volume, double minrb_minRad)
        {
            // Flag to deactivate fracture set; initially set to false
            bool deactivateFractureSet = false;

            // Calculate the ratio of current to maximum active macrofracture volumetric ratio for this fracture set, and if it is below the specified minimum set the fracture deactivation flag to true
            // We only need to do this if the specified minimum is greater than zero; otherwise the check is not performed
            if (historic_a_MFP33_termination_ratio > 0)
            {
                // If the active half-macrofracture volumetric ratio for this fracture set is increasing, update the maximum historic active macrofracture volumetric ratio
                double current_tot_a_MFP33 = combined_a_MFP33_total();
                if (max_historic_a_MFP33 < current_tot_a_MFP33)
                    max_historic_a_MFP33 = current_tot_a_MFP33;

                double historic_a_MFP33_ratio = (max_historic_a_MFP33 > 0 ? current_tot_a_MFP33 / max_historic_a_MFP33 : 1);
                if (historic_a_MFP33_ratio <= historic_a_MFP33_termination_ratio)
                    deactivateFractureSet = true;
            }

            // Calculate the active to total half-macrofracture volumetric ratio for this fracture set, and if it is below the specified minimum set the fracture deactivation flag to true
            // We only need to do this if the specified minimum is greater than zero; otherwise the check is not performed
            if (active_total_MFP30_termination_ratio > 0)
            {
                double a_MFP30_ratio = combined_a_MFP30_total() / (combined_a_MFP30_total() + combined_sII_MFP30_total() + combined_sIJ_MFP30_total());
                if (a_MFP30_ratio <= active_total_MFP30_termination_ratio)
                    deactivateFractureSet = true;
            }

            bool OneDipSetDeactivated = false;
            bool OneDipSetStillActive = false;
            // If the fracture deactivation flag is true, deactivate all the dipsets
            foreach (FractureDipSet fds in FractureDipSets)
            {
                // If the dipset is already deactivated, we do not need to check it again
                if (fds.getEvolutionStage() == FractureEvolutionStage.Deactivated)
                {
                    OneDipSetDeactivated = true;
                    continue;
                }
                // If the entire fracture set has been deactivated, deactivate this fracture dipset
                else if (deactivateFractureSet)
                {
                    fds.deactivateFractures();
                    OneDipSetDeactivated = true;
                }
                // If the clear zone volume has dropped below the minimum specified, deactivate this fracture dipset only
                else if (fds.getClearZoneVolumeAllFS() < gbc.PropControl.minimum_ClearZone_Volume)
                {
                    fds.deactivateFractures();
                    OneDipSetDeactivated = true;
                }
                // Check if the extrapolated initial radius of microfractures in the smallest bin is less than zero (will only apply if b<2); if so, deactivate this fracture dipset only
                else if (fds.Check_Initial_uF_Radius(minrb_minRad, gbc.MechProps.b_factor))
                {
                    OneDipSetDeactivated = true;
                }

                // Check if the fracture dipset is currently active
                if ((fds.getEvolutionStage() == FractureEvolutionStage.Growing) || (fds.getEvolutionStage() == FractureEvolutionStage.ResidualActivity)) 
                    OneDipSetStillActive = true;
            }
            if (OneDipSetDeactivated && !OneDipSetStillActive)
                deactivateFractureSet = true;

            return deactivateFractureSet;
        }

        // DFN fracture interaction functions: used to check if fractures interact with other fractures during DFN generation
        /// <summary>
        /// Check whether a specified point (in local IJK coordinates) lies within a proximity zone of arbitrary width around any of the macrofracture segments in the explicit DFN associated with this fracture set; the proximity zone width can vary for different fracture dip sets
        /// </summary>
        /// <param name="point">Input point in IJK coordinates</param>
        /// <param name="proximityZoneHalfWidths">List of proximity zone widths around each fracture dip set</param>
        /// <returns>True if point lies within a proximity zone, otherwise false</returns>
        public bool checkInMFProximityZone(PointIJK point, List<double> proximityZoneHalfWidths)
        {
            // Cache useful data locally
            double point_I = point.I;
            double point_J = point.J;

            // Check the list of proximity zone widths matches the number of fracture dip sets
            // If not, add extra zeros to pad out the list
            int NoDipsets = FractureDipSets.Count;
            while (proximityZoneHalfWidths.Count < NoDipsets)
                proximityZoneHalfWidths.Add(0);

            // Loop through every macrofracture segment and check if the specified point lies in the exclusion zone
            foreach (PropagationDirection MF_PropDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                foreach (MacrofractureSegmentIJK MF_segment in LocalDFNMacrofractureSegments[MF_PropDir])
                {
                    // If the segment has zero length then it will have no proximity zone; we can go on to the next segment
                    if ((float)MF_segment.StrikeLength == 0f)
                        continue;

                    // Get the I coordinates of the segment nodes
                    double segment_Imin, segment_Imax;
                    if (MF_PropDir == PropagationDirection.IPlus)
                    {
                        segment_Imin = MF_segment.NonPropNode.I;
                        segment_Imax = MF_segment.PropNode.I;
                    }
                    else
                    {
                        segment_Imin = MF_segment.PropNode.I;
                        segment_Imax = MF_segment.NonPropNode.I;
                    }

                    // Get the J coordinates of the segment nodes +- proximity zone width
                    double segment_proximityzonehalfwidth = proximityZoneHalfWidths[MF_segment.FractureDipSetIndex];
                    double segment_Jmin = MF_segment.PropNode.J - segment_proximityzonehalfwidth;
                    double segment_Jmax = MF_segment.PropNode.J + segment_proximityzonehalfwidth;

                    // Check if the point lies within the I and J ranges of the proximity zone
                    // If so, we can return true immediately
                    if ((point_I >= segment_Imin) && (point_I <= segment_Imax))
                        if ((point_J >= segment_Jmin) && (point_J <= segment_Jmax))
                            return true;
                }
            }

            // If the point does not lie in the proximity zone of any segments, return false
            return false;
        }
        /// <summary>
        /// Check whether a specified point (in local IJK coordinates) lies within the stress shadows of any of the macrofracture segments in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in IJK coordinates</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        public bool checkInMFStressShadow(PointIJK point)
        {
            return checkInMFExclusionZone(point, 0);
        }
        /// <summary>
        /// Check whether a specified point lying on a fracture in this set (in local IJK coordinates) lies within the exclusion zone of any of the macrofracture segments in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in IJK coordinates</param>
        /// <param name="inputfracStressShadowWidth">Total width (i.e. on both sides) of the stress shadow around the input fracture (m)</param>
        /// <returns>True if point lies within an exclusion zone, otherwise false</returns>
        public bool checkInMFExclusionZone(PointIJK point, double inputfracStressShadowWidth)
        {
            // Get the exclusion zone half-widths for all fracture dip sets (i.e. the stress shadow width for the dip set plus the input stress shadow width, halved)
            // NB if the interacting fracture set is in another gridblock, the current explicit timestep for these fractures may be different from the current explicit timestep in this gridblock
            // Using the value for this gridblock may result in an "index out of range" exception; therefore we will set the index to -1, which will automatically use the current explicit timestep for the host gridblock 
            List<double> exclusionzonehalfwidths = new List<double>();
            foreach (FractureDipSet fds in FractureDipSets)
                exclusionzonehalfwidths.Add((fds.getMeanStressShadowWidth(-1) + inputfracStressShadowWidth) / 2);

            // Use the checkInMFProximityZone function to check if the specified point lies in the exclusion zone of any of the segments
            return checkInMFProximityZone(point, exclusionzonehalfwidths);
        }
        /// <summary>
        /// Check whether a specified point (in global XYZ coordinates) lies within a proximity zone of arbitrary width around any of the macrofracture segments in the explicit DFN associated with this fracture set; the proximity zone width can vary for different fracture dip sets
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="proximityZoneHalfWidths">List of proximity zone widths around each fracture dip set</param>
        /// <returns>True if point lies within a proximity zone, otherwise false</returns>
        public bool checkInMFProximityZone(PointXYZ point, List<double> proximityZoneHalfWidths)
        {
            return checkInMFProximityZone(convertXYZtoIJK(point), proximityZoneHalfWidths);
        }
        /// <summary>
        /// Check whether a specified point (in global XYZ coordinates) lies within the stress shadows of any of the macrofracture segments in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        public bool checkInMFStressShadow(PointXYZ point)
        {
            return checkInMFStressShadow(convertXYZtoIJK(point));
        }
        /// <summary>
        /// Check whether a specified point lying on a fracture in this set (in global XYZ coordinates) lies within the exclusion zone of any of the macrofracture segments in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="inputfracStressShadowWidth">Total width (i.e. on both sides) of the stress shadow around the input fracture (m)</param>
        /// <returns>True if point lies within a stress shadow, otherwise false</returns>
        public bool checkInMFExclusionZone(PointXYZ point, double inputfracStressShadowWidth)
        {
            return checkInMFExclusionZone(convertXYZtoIJK(point), inputfracStressShadowWidth);
        }
        /// <summary>
        /// Check whether a propagating macrofracture segment from this fracture set will terminate due to stress shadow interaction with of any of the other macrofracture segments in the explicit DFN associated with this fracture set, as it propagates a specified distance
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment interacts with another macrofracture stress shadow (m)</param>
        /// <param name="checkRelayCrossing">If true, do not record a stress shadow interaction if the relay zone between the two fracture tips is cut by a third fracture</param>
        /// <param name="terminateIfInteracts">If true, automatically flag propagating fracture segment as inactive due to stress shadow interaction; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment interacts with another macrofracture stress shadow, otherwise false</returns>
        public bool checkStressShadowInteraction(MacrofractureSegmentIJK propagatingSegment, ref double propagationLength, bool checkRelayCrossing, bool terminateIfInteracts)
        {
            return checkStressShadowInteraction(propagatingSegment, this, ref propagationLength, checkRelayCrossing, terminateIfInteracts);
        }
        /// <summary>
        /// Check whether a propagating macrofracture segment from this fracture set will terminate due to stress shadow interaction with of any of the other macrofracture segments in the explicit DFN associated with a fracture set in another gridblock
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="interacting_fs">Reference to a Gridblock_FractureSet object representing the fracture set which the propagating fracture segment will interact with</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment interacts with another macrofracture stress shadow (m)</param>
        /// <param name="checkRelayCrossing">If true, do not record a stress shadow interaction if the relay zone between the two fracture tips is cut by a third fracture</param>
        /// <param name="terminateIfInteracts">If true, automatically flag propagating fracture segment as inactive due to stress shadow interaction; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment interacts with another macrofracture stress shadow, otherwise false</returns>
        public bool checkStressShadowInteraction(MacrofractureSegmentIJK propagatingSegment, Gridblock_FractureSet interacting_fs, ref double propagationLength, bool checkRelayCrossing, bool terminateIfInteracts)
        {
            // Set return value to false initially
            bool interacts = false;

            // Flag to indicate whether the propagating fracture segment is from the same fracture set (and hence gridblock) as the interacting set
            // If not we will need to convert the coordinates of the interacting segments before checking for stress shadow interaction
            bool sameSet = (interacting_fs == this);

            // Cache useful data locally
            double propNode_I = propagatingSegment.PropNode.I;
            double propNode_J = propagatingSegment.PropNode.J;
            PropagationDirection propNode_dir = propagatingSegment.LocalPropDir;

            // Get the propagation direction of the interacting tips
            // Normally this will be the opposite to the direction of the propagating tip, but if it is from a different gridblock, the propagation directions may be reversed
            PropagationDirection interactingNode_dir = (propNode_dir == PropagationDirection.IPlus ? PropagationDirection.IMinus : PropagationDirection.IPlus);
            if ((!sameSet) && (PointXYZ.getAngularDifference(Strike, interacting_fs.Strike) > Math.PI / 2))
                interactingNode_dir = propNode_dir;

            // Get the exclusion zone half-widths for all fracture dip sets (half the sum of the stress shadow width for this fracture and the stress shadow width for the dip set)
            // If the propagating and interacting fractures are not from the same set, we will need to adjust the apparent width of the stress shadows in the interacting set, to account for the difference in strike between them
            double interactingStressShadowWidthMultiplier = 1;
            if (!sameSet)
                interactingStressShadowWidthMultiplier = VectorXYZ.Cos_trim(PointXYZ.getStrikeDifference(Strike, interacting_fs.Strike));
            double propagatingSegment_stresshadowwidth = FractureDipSets[propagatingSegment.FractureDipSetIndex].getMeanStressShadowWidth(-1);
            List<double> stressshadowhalfwidths = new List<double>();
            foreach (FractureDipSet fds in interacting_fs.FractureDipSets)
            {
                double interactingSegment_stressshadowwidth = interactingStressShadowWidthMultiplier * fds.getMeanStressShadowWidth(-1);
                stressshadowhalfwidths.Add((propagatingSegment_stresshadowwidth + interactingSegment_stressshadowwidth) / 2);
            }

            // The intersection criteria are slightly different for fractures propagating in the IPlus and IMinus directions so we must code these separately
            if (propNode_dir == PropagationDirection.IPlus) // Propagating fracture is propagating in the IPlus direction, interacting fractures are propagating in the IMinus direction
            {
                // Get the I coordinates of the ends of the stress shadow interaction box of the propagating fracture
                double stressshadowintbox_Imin = propNode_I;
                double stressshadowintbox_Imax = propNode_I + propagationLength;

                // Loop through every macrofracture segment with the opposite propagation direction (even if currently inactive) and check if the specified point lies in its stress shadow
                foreach (MacrofractureSegmentIJK interacting_MF_segment in interacting_fs.LocalDFNMacrofractureSegments[interactingNode_dir])
                {
                    // Get the I and J coordinates of the outer (i.e. propagating) node of the interacting segment
                    double segmentnode_I, segmentnode_J;
                    if (sameSet) // If the propagating and interacting segments are from the same set, we can use the IJK coordinates of the interacting node directly
                    {
                        segmentnode_I = interacting_MF_segment.PropNode.I;
                        segmentnode_J = interacting_MF_segment.PropNode.J;
                    }
                    else // Otherwise we will need to convert the IJK coordinates of the interacting node to the frame of the propagating fracture set
                    {
                        PointXYZ interacting_segment_PropNode_XYZ = interacting_MF_segment.getPropNodeinXYZ();
                        segmentnode_I = getICoordinate(interacting_segment_PropNode_XYZ);
                        segmentnode_J = getJCoordinate(interacting_segment_PropNode_XYZ);
                    }

                    // Get the J coordinates of the stress shadow interaction box
                    double stressshadowintboxhalfwidth = stressshadowhalfwidths[interacting_MF_segment.FractureDipSetIndex];
                    double stressshadowintbox_Jmin = propNode_J - stressshadowintboxhalfwidth;
                    double stressshadowintbox_Jmax = propNode_J + stressshadowintboxhalfwidth;

                    // Check if the outer (i.e. propagating) node of the interacting segment lies within the stress shadow interaction box
                    // NB we do not record an interaction if the propagating node of the interacting segment lies on the far boundary of the stress shadow interaction box (i.e. segmentnode_I = stressshadowintbox_Imax)
                    // This is so that there will be no interaction whenever the function returns a propagation length equal to the input length
                    // We also need to check the propagating and interacting segments do not belong to the same fracture
                    if ((segmentnode_I >= stressshadowintbox_Imin) && (segmentnode_I < stressshadowintbox_Imax))
                        if ((segmentnode_J >= stressshadowintbox_Jmin) && (segmentnode_J <= stressshadowintbox_Jmax))
                            if (propagatingSegment.NonPropNode != interacting_MF_segment.NonPropNode)
                            {
                                // If required, check if the relay zone between the two fracture tips is cut by a third fracture, and if so, move on to the next segment 
                                if (checkRelayCrossing && checkCrossingFractures(new PointIJK(segmentnode_I, propNode_J, 0), interacting_MF_segment.PropNode, interacting_fs))
                                    continue;

                                // Set the return value to true
                                interacts = true;

                                // Reduce the maximum propagation distance and stress shadow interaction box dimensions accordingly
                                stressshadowintbox_Imax = segmentnode_I;
                                propagationLength = segmentnode_I - stressshadowintbox_Imin;

                                // Set the propagating macrofracture segment to inactive, due to stress shadow interaction, and set reference to terminating macrofracture segment
                                if (terminateIfInteracts)
                                {
                                    propagatingSegment.PropNodeType = SegmentNodeType.ConnectedStressShadow;
                                    propagatingSegment.TerminatingSegment = interacting_MF_segment;
                                }

                                // NB the calling function will be responsible for setting the activity and termination type of the interacting segment
                                // We cannot do this here since it may be that the propagating fracture is terminated by another mechanism first

                                // We also need to continue looping through all remaining macrofracture segments with the opposite propagation direction - one of them may interact earlier than the selected segment
                            }
                }
            }
            else // Propagating fracture is propagating in the IMinus direction, interacting fractures are propagating in the IPlus direction
            {
                // Get the I coordinates of the ends of the stress shadow interaction box of the propagating fracture
                double stressshadowintbox_Imin = propNode_I - propagationLength;
                double stressshadowintbox_Imax = propNode_I;

                // Loop through every macrofracture segment with the opposite propagation direction (even if currently inactive) and check if the specified point lies in its stress shadow
                foreach (MacrofractureSegmentIJK interacting_MF_segment in interacting_fs.LocalDFNMacrofractureSegments[interactingNode_dir])
                {
                    // Get the I and J coordinates of the outer (i.e. propagating) node of the interacting segment
                    double segmentnode_I, segmentnode_J;
                    if (sameSet) // If the propagating and interacting segments are from the same set, we can use the IJK coordinates of the interacting node directly
                    {
                        segmentnode_I = interacting_MF_segment.PropNode.I;
                        segmentnode_J = interacting_MF_segment.PropNode.J;
                    }
                    else // Otherwise we will need to convert the IJK coordinates of the interacting node to the frame of the propagating fracture set
                    {
                        PointXYZ interacting_segment_PropNode_XYZ = interacting_MF_segment.getPropNodeinXYZ();
                        segmentnode_I = getICoordinate(interacting_segment_PropNode_XYZ);
                        segmentnode_J = getJCoordinate(interacting_segment_PropNode_XYZ);
                    }

                    // Get the J coordinates of the stress shadow interaction box
                    double stressshadowintboxhalfwidth = stressshadowhalfwidths[interacting_MF_segment.FractureDipSetIndex];
                    double stressshadowintbox_Jmin = propNode_J - stressshadowintboxhalfwidth;
                    double stressshadowintbox_Jmax = propNode_J + stressshadowintboxhalfwidth;

                    // Check if the outer (i.e. propagating) node of the interacting segment lies within the stress shadow interaction box
                    // NB we do not record an interaction if the propagating node of the interacting segment lies on the far boundary of the stress shadow interaction box (i.e. segmentnode_I = stressshadowintbox_Imin)
                    // This is so that there will be no interaction whenever the function returns a propagation length equal to the input length
                    // We also need to check the propagating and interacting segments do not belong to the same fracture
                    if ((segmentnode_I > stressshadowintbox_Imin) && (segmentnode_I <= stressshadowintbox_Imax))
                        if ((segmentnode_J >= stressshadowintbox_Jmin) && (segmentnode_J <= stressshadowintbox_Jmax))
                            if (propagatingSegment.NonPropNode != interacting_MF_segment.NonPropNode)
                            {
                                // If required, check if the relay zone between the two fracture tips is cut by a third fracture, and if so, move on to the next segment 
                                if (checkRelayCrossing && checkCrossingFractures(new PointIJK(segmentnode_I, propNode_J, 0), interacting_MF_segment.PropNode, interacting_fs))
                                    continue;

                                // Set the return value to true
                                interacts = true;

                                // Reduce the maximum propagation distance and stress shadow interaction box dimensions accordingly
                                stressshadowintbox_Imin = segmentnode_I;
                                propagationLength = stressshadowintbox_Imax - segmentnode_I;

                                // Set the propagating macrofracture segment to inactive, due to stress shadow interaction, and set reference to terminating macrofracture segment
                                if (terminateIfInteracts)
                                {
                                    propagatingSegment.PropNodeType = SegmentNodeType.ConnectedStressShadow;
                                    propagatingSegment.TerminatingSegment = interacting_MF_segment;
                                }

                                // NB the calling function will be responsible for setting the activity and termination type of the interacting segment
                                // We cannot do this here since it may be that the propagating fracture is terminated by another mechanism first

                                // We also need to continue looping through all remaining macrofracture segments with the opposite propagation direction - one of them may interact earlier than the selected segment
                            }
                }
            }

            return interacts;
        }
        /// <summary>
        /// Check whether any other fracture segments intersect a line between two specified fracture tips
        /// THis is useful to check if a potential relay zone would be intersected by another fracture
        /// </summary>
        /// <param name="FractureTip1">First fracture tip; must belong to a fracture in this fracture set</param>
        /// <param name="FractureTip2">Second fracture tip; may belong to a fracture in another fracture set</param>
        /// <param name="Tip2Set">Fracture set that the second fracture tip belongs to</param>
        /// <returns>True if any other fracture segment intersects the line between the two specified fracture tips, otherwise false</returns>
        private bool checkCrossingFractures(PointIJK FractureTip1, PointIJK FractureTip2, Gridblock_FractureSet Tip2Set)
        {
            // Convert the two specified fracture tips to XYZ coordinates
            PointXYZ fractureTip1XYZ = convertIJKtoXYZ(FractureTip1);
            PointXYZ fractureTip2XYZ = Tip2Set.convertIJKtoXYZ(FractureTip2);

            // Loop through every other fracture set in this gridblock, apart from this one and the tip 2 set
            foreach (Gridblock_FractureSet intersecting_fs in this.gbc.FractureSets)
            {
                if ((intersecting_fs == this) || (intersecting_fs == Tip2Set))
                    continue;

                // Loop through each segment in the fracture set
                foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                    foreach (MacrofractureSegmentIJK intersectingSegment in intersecting_fs.LocalDFNMacrofractureSegments[dir])
                    {
                        // Convert the two tips of the third segment to XYZ coordinates
                        PointXYZ intersectingSegmentTip1XYZ = intersectingSegment.getNonPropNodeinXYZ();
                        PointXYZ intersectingSegmentTip2XYZ = intersectingSegment.getPropNodeinXYZ();

                        // If the third segment intersects the line between the two specified fracture tips, return true
                        if (PointXYZ.checkCrossover(fractureTip1XYZ, fractureTip2XYZ, intersectingSegmentTip1XYZ, intersectingSegmentTip2XYZ))
                            return true;
                    }
            }

            // If fracture tip 2 lies in another gridblock, check against other fracture sets in that gridblock as well
            if (Tip2Set.gbc != this.gbc)
                foreach (Gridblock_FractureSet intersecting_fs in Tip2Set.gbc.FractureSets)
                {
                    if ((intersecting_fs == this) || (intersecting_fs == Tip2Set))
                        continue;

                    // Loop through each segment in the fracture set
                    foreach (PropagationDirection dir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
                        foreach (MacrofractureSegmentIJK intersectingSegment in intersecting_fs.LocalDFNMacrofractureSegments[dir])
                        {
                            // Convert the two tips of the third segment to XYZ coordinates
                            PointXYZ intersectingSegmentTip1XYZ = intersectingSegment.getNonPropNodeinXYZ();
                            PointXYZ intersectingSegmentTip2XYZ = intersectingSegment.getPropNodeinXYZ();

                            // If the third segment intersects the line between the two specified fracture tips, return true
                            if (PointXYZ.checkCrossover(fractureTip1XYZ, fractureTip2XYZ, intersectingSegmentTip1XYZ, intersectingSegmentTip2XYZ))
                                return true;
                        }
                }

            // If no other fracture segments intersect the line between the two specified fracture tips, return false
            return false;
        }
        /// <summary>
        /// Check whether a boundary tracking macrofracture segment from this fracture set will converge with another boundary tracking macrofracture segment
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment interacts with another macrofracture stress shadow (m)</param>
        /// <param name="terminateIfInteracts">If true, automatically flag propagating fracture segment as inactive due to stress shadow interaction; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment interacts with another macrofracture stress shadow, otherwise false</returns>
        public bool checkFractureConvergence(MacrofractureSegmentIJK propagatingSegment, ref double propagationLength, bool terminateIfInteracts)
        {
            // Get the tracking boundary
            GridDirection trackingBoundary = propagatingSegment.TrackingBoundary;

            // If TrackingBoundary is set to none, will will abort and return false
            if (trackingBoundary == GridDirection.None)
                return false;

            // Set return value to false initially
            bool converges = false;

            // Cache useful data locally
            double propNode_I = propagatingSegment.PropNode.I;
            PropagationDirection propNode_dir = propagatingSegment.LocalPropDir;

            // The intersection criteria are slightly different for fractures propagating in the IPlus and IMinus directions so we must code these separately
            // Since all boundary tracking fractures must be propagating in the same direction, we only need to check against fractures propagating in the same direction as this one
            if (propNode_dir == PropagationDirection.IPlus) // Propagating fracture is propagating in the IPlus direction, convergent fractures are propagating in the IPlus direction
            {
                // Get the I coordinates of the ends of the projection line of the propagating segment
                double projectionLine_Imin = propNode_I;
                double projectionLine_Imax = propNode_I + propagationLength;

                // Loop through every other boundary tracking macrofracture segment tracking the correct boundary (even if currently inactive)
                // Then check if the rear (i.e. non-propagating) node of the convergent segment lies within the projection line of the propagating segment
                foreach (MacrofractureSegmentIJK convergent_MF_segment in LocalDFNMacrofractureSegments[PropagationDirection.IPlus])
                    if (convergent_MF_segment.TrackingBoundary == trackingBoundary)
                    {
                        if (convergent_MF_segment != propagatingSegment)
                        {
                            // Get the I coordinate of the rear (i.e. non-propagating) node of the convergent segment
                            double segmentnode_I = convergent_MF_segment.NonPropNode.I;

                            // Check if the rear (i.e. non-propagating) node of the convergent segment lies within the projection line of the propagating segment
                            // NB we do not record an interaction if the non-propagating node of the convergent segment lies at the far tip of the projection line of the propagating segment (i.e. segmentnode_I = projectionLine_Imax)
                            // This is so that there will be no interaction whenever the function returns a propagation length equal to the input length
                            if ((segmentnode_I >= projectionLine_Imin) && (segmentnode_I < projectionLine_Imax))
                            {
                                // Set the return value to true
                                converges = true;

                                // Reduce the maximum propagation distance and stress shadow interaction box dimensions accordingly
                                projectionLine_Imax = segmentnode_I;
                                propagationLength = segmentnode_I - projectionLine_Imin;

                                // Set the propagating macrofracture segment to inactive, due to convergence, and set reference to terminating macrofracture segment
                                if (terminateIfInteracts)
                                {
                                    propagatingSegment.PropNodeType = SegmentNodeType.Convergence;
                                    propagatingSegment.TerminatingSegment = convergent_MF_segment;
                                }

                                // We also need to continue looping through all remaining boundary tracking macrofracture segments - one of them may converge earlier than the selected segment
                            }
                        }
                    }
            }
            else // Propagating fracture is propagating in the IMinus direction, convergent fractures are propagating in the IMinus direction
            {
                // Get the I coordinates of the ends of the projection line of the propagating segment
                double projectionLine_Imin = propNode_I - propagationLength;
                double projectionLine_Imax = propNode_I;

                // Loop through every other boundary tracking macrofracture segment tracking the correct boundary (even if currently inactive)
                // Then check if the rear (i.e. non-propagating) node of the convergent segment lies within the projection line of the propagating segment
                foreach (MacrofractureSegmentIJK convergent_MF_segment in LocalDFNMacrofractureSegments[PropagationDirection.IMinus])
                    if (convergent_MF_segment.TrackingBoundary == trackingBoundary)
                    {
                        // Get the I coordinate of the rear (i.e. non-propagating) node of the convergent segment
                        double segmentnode_I = convergent_MF_segment.NonPropNode.I;

                        // Check if the rear (i.e. non-propagating) node of the convergent segment lies within the projection line of the propagating segment
                        // NB we do not record an interaction if the non-propagating node of the convergent segment lies at the far tip of the projection line of the propagating segment (i.e. segmentnode_I = projectionLine_Imax)
                        // This is so that there will be no interaction whenever the function returns a propagation length equal to the input length
                        if ((segmentnode_I > projectionLine_Imin) && (segmentnode_I <= projectionLine_Imax))
                        {
                            // Set the return value to true
                            converges = true;

                            // Reduce the maximum propagation distance and stress shadow interaction box dimensions accordingly
                            projectionLine_Imin = segmentnode_I;
                            propagationLength = projectionLine_Imax - segmentnode_I;

                            // Set the propagating macrofracture segment to inactive, due to stress shadow interaction, and set reference to terminating macrofracture segment
                            if (terminateIfInteracts)
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.Convergence;
                                propagatingSegment.TerminatingSegment = convergent_MF_segment;
                            }

                            // We also need to continue looping through all remaining boundary tracking macrofracture segments - one of them may converge earlier than the selected segment
                        }
                    }
            }

            return converges;
        }
        /// <summary>
        /// Check whether a propagating macrofracture segment from this fracture set will intersect a non-boundary tracking macrofracture segment from another fracture set in this gridblock
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="intersecting_fs">Reference to a Gridblock_FractureSet object representing the fracture set which the propagating fracture segment will intersect</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment intersects another macrofracture segment (m)</param>
        /// <param name="terminateIfIntersects">If true, automatically flag propagating fracture segment as inactive due to intersection; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment intersects another macrofracture segment, otherwise false</returns>
        public bool checkFractureIntersection(MacrofractureSegmentIJK propagatingSegment, Gridblock_FractureSet intersecting_fs, ref double propagationLength, bool terminateIfIntersects)
        {
            // Check if the propagating segment is tracking a boundary - if so call the checkBoundaryTrackingFractureIntersection function
            if (propagatingSegment.TrackingBoundary != GridDirection.None)
                return checkBoundaryTrackingFractureIntersection(propagatingSegment, intersecting_fs, ref propagationLength, terminateIfIntersects);

            // Calculate the angle of intersection, given by the strike of the intersecting fracture set - the strike of the propagating fracture set (NB may not be propagation direction)
            double intersectionAngle = intersecting_fs.Strike - Strike;

            // Set return value to false initially
            bool intersects = false;

            // Calculate sin and cosine of intersection angle
            double sin_intersectionAngle = VectorXYZ.Sin_trim(intersectionAngle);
            double cos_intersectionAngle = VectorXYZ.Cos_trim(intersectionAngle);

            // Cache coordinates and direction of propagating segment locally
            double propNode_I = propagatingSegment.PropNode.I;
            double propNode_J = propagatingSegment.PropNode.J;
            PropagationDirection propNode_dir = propagatingSegment.LocalPropDir;

            // If the intersection angle is +/-(pi/2) we can simplify the coordinate conversion and intersection calculations
            double orthogonal_sin_cutoff = 0.999; // If the sine of the intersection angle is higher than this value we will assume the fracture sets are orthogonal
            double parallel_cos_cutoff = 0.999; // If the cosine of the intersection angle is higher than this value we will assume the fracture sets are parallel
            IntersectionAngle orthogonalFractures;
            if (sin_intersectionAngle > orthogonal_sin_cutoff) // Intersection angle = +90deg (propagating fracture set = hmin, intersecting fracture set = hmax) 
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Orthogonal_Plus;
            }
            else if (sin_intersectionAngle < -orthogonal_sin_cutoff) // Intersection angle = -90deg (propagating fracture set = hmax, intersecting fracture set = hmin) 
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Orthogonal_Minus;
            }
            else if (Math.Abs(cos_intersectionAngle) > parallel_cos_cutoff) // Intersection angle = 0deg (propagating fracture set = intersecting fracture set)
            {
                // Set flag for parallel fractures
                orthogonalFractures = IntersectionAngle.Parallel;
            }
            else // Propagating fractures are oblique to intersecting fractures
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Oblique;
            }

            // If fractures are parallel we can abort and return false
            if (orthogonalFractures == IntersectionAngle.Parallel) return false;

            // Loop through every macrofracture segment and check if it intersects the projection line of the propagating fracture
            foreach (PropagationDirection intersecting_PropDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                foreach (MacrofractureSegmentIJK intersecting_MF_segment in intersecting_fs.LocalDFNMacrofractureSegments[intersecting_PropDir])
                {
                    // Check if the intersecting segment is a boundary tracking fracture
                    // If so we will not check it here - it will be checked after checking if the fracture crosses the boundary
                    if (intersecting_MF_segment.TrackingBoundary == GridDirection.None)
                    {
                        // Get the J coordinates of the two ends of the intersecting segment
                        // NB intersecting_segment_NPN_J, intersecting_segment_PN_J, intersecting_segment_MinJ and intersecting_segment_MaxJ represent the coordinates of the intersecting segment in the frame of this fracture set
                        // These will be different from its coordinates in its own frame
                        double intersecting_segment_NPN_J, intersecting_segment_PN_J;
                        double intersecting_segment_MinJ, intersecting_segment_MaxJ;
                        switch (orthogonalFractures)
                        {
                            case IntersectionAngle.Orthogonal_Plus:
                                {
                                    intersecting_segment_NPN_J = intersecting_MF_segment.NonPropNode.I;
                                    intersecting_segment_PN_J = intersecting_MF_segment.PropNode.I;
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            case IntersectionAngle.Orthogonal_Minus:
                                {
                                    intersecting_segment_NPN_J = -intersecting_MF_segment.NonPropNode.I;
                                    intersecting_segment_PN_J = -intersecting_MF_segment.PropNode.I;
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            case IntersectionAngle.Oblique:
                                {
                                    intersecting_segment_NPN_J = (intersecting_MF_segment.NonPropNode.I * sin_intersectionAngle) + (intersecting_MF_segment.NonPropNode.J * cos_intersectionAngle);
                                    intersecting_segment_PN_J = (intersecting_MF_segment.PropNode.I * sin_intersectionAngle) + (intersecting_MF_segment.PropNode.J * cos_intersectionAngle);
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            case IntersectionAngle.Parallel:
                            // Not possible so will fall through to default
                            default:
                                {
                                    intersecting_segment_NPN_J = 0;
                                    intersecting_segment_PN_J = 0;
                                    // Default sets MinJ > MaxJ so propagating segment will never lie within the width range of the intersecting segment
                                    intersecting_segment_MinJ = 1;
                                    intersecting_segment_MaxJ = -1;
                                }
                                break;
                        }

                        // Check if the propagating segment lies within the width range of the intersecting segment
                        if ((propNode_J >= intersecting_segment_MinJ) && (propNode_J <= intersecting_segment_MaxJ))
                        {
                            // If so, get the I coordinate of the intersecting segment at the point of intersection
                            // NB intersecting_segment_NPN_I, intersecting_segment_PN_I and intersecting_segment_I represent the coordinates of the intersecting segment in the frame of this fracture set
                            // These will be different from its coordinates in its own frame
                            double intersecting_segment_I;
                            switch (orthogonalFractures)
                            {
                                case IntersectionAngle.Orthogonal_Plus:
                                    {
                                        intersecting_segment_I = -intersecting_MF_segment.NonPropNode.J;
                                    }
                                    break;
                                case IntersectionAngle.Orthogonal_Minus:
                                    {
                                        intersecting_segment_I = intersecting_MF_segment.NonPropNode.J;
                                    }
                                    break;
                                case IntersectionAngle.Oblique:
                                    {
                                        double intersecting_segment_NPN_I = (intersecting_MF_segment.NonPropNode.I * cos_intersectionAngle) - (intersecting_MF_segment.NonPropNode.J * sin_intersectionAngle);
                                        double intersecting_segment_PN_I = (intersecting_MF_segment.PropNode.I * cos_intersectionAngle) - (intersecting_MF_segment.PropNode.J * sin_intersectionAngle);
                                        double intersecting_segment_dJ = intersecting_segment_PN_J - intersecting_segment_NPN_J;
                                        if (intersecting_segment_dJ == 0)
                                            intersecting_segment_I = intersecting_segment_PN_I;
                                        else
                                            intersecting_segment_I = (((propNode_J - intersecting_segment_NPN_J) / intersecting_segment_dJ) * intersecting_segment_PN_I) + (((intersecting_segment_PN_J - propNode_J) / intersecting_segment_dJ) * intersecting_segment_NPN_I);
                                    }
                                    break;
                                case IntersectionAngle.Parallel:
                                // Not possible so will fall through to default
                                default:
                                    // Default sets intersecting segment I to 0 (should not reach here)
                                    intersecting_segment_I = 0;
                                    break;
                            }

                            // Check if the intersecting segment lies within the propagation range of the propagating segment
                            // The intersection criteria are slightly different for fractures propagating in the IPlus and IMinus directions so we must code these separately
                            switch (propNode_dir)
                            {
                                case PropagationDirection.IPlus:
                                    // Check if the projection line of the propagating fracture intersects the segment
                                    // NB we do not record an intersection if the tip of the projection line lies on the intersecting segment (i.e. propNode_I + propagationLength = intersecting_segment_I)
                                    // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                                    if ((propNode_I <= intersecting_segment_I) && ((propNode_I + propagationLength) > intersecting_segment_I))
                                    {
                                        // Set the return value to true
                                        intersects = true;

                                        // Reduce the maximum propagation distance of the propagating fracture accordingly
                                        propagationLength = intersecting_segment_I - propNode_I;

                                        // Set the propagating macrofracture segment to inactive, due to intersection, and set reference to terminating macrofracture segment
                                        if (terminateIfIntersects)
                                        {
                                            propagatingSegment.PropNodeType = SegmentNodeType.Intersection;
                                            propagatingSegment.TerminatingSegment = intersecting_MF_segment;
                                        }

                                        // We need to continue looping through all remaining macrofracture segments with the opposite propagation direction - one of them may interact earlier than the selected segment
                                    }
                                    break;
                                case PropagationDirection.IMinus:
                                    // Check if the projection line of the propagating fracture intersects the segment
                                    // NB we do not record an intersection if the tip of the projection line lies on the intersecting segment (i.e. propNode_I - propagationLength = intersecting_segment_I)
                                    // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                                    if ((propNode_I >= intersecting_segment_I) && ((propNode_I - propagationLength) < intersecting_segment_I))
                                    {
                                        // Set the return value to true
                                        intersects = true;

                                        // Reduce the maximum propagation distance of the propagating fracture accordingly
                                        propagationLength = propNode_I - intersecting_segment_I;

                                        // Set the propagating macrofracture segment to inactive, due to intersection, and set reference to terminating macrofracture segment
                                        if (terminateIfIntersects)
                                        {
                                            propagatingSegment.PropNodeType = SegmentNodeType.Intersection;
                                            propagatingSegment.TerminatingSegment = intersecting_MF_segment;
                                        }

                                        // We need to continue looping through all remaining macrofracture segments with the opposite propagation direction - one of them may interact earlier than the selected segment
                                    }
                                    break;
                                default:
                                    break;
                            } // End switch on propagation direction
                        } // End check if the propagating segment lies within the width range of the intersecting segment
                    } // End check if the intersecting segment is a boundary tracking fracture
                } // End loop through every intersecting macrofracture segment
            } // End loop through every propagation direction for intersecting macrofracture segments

            return intersects;
        }
        /// <summary>
        /// Check whether a boundary tracking macrofracture segment from this fracture set will intersect a non-boundary tracking macrofracture segment from another fracture set in this gridblock
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="intersecting_fs">Reference to the intersecting fracture set</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment intersects another macrofracture segment (m)</param>
        /// <param name="terminateIfIntersects">If true, automatically flag propagating fracture segment as inactive due to intersection; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment intersects another macrofracture segment, otherwise false</returns>
        public bool checkBoundaryTrackingFractureIntersection(MacrofractureSegmentIJK propagatingSegment, Gridblock_FractureSet intersecting_fs, ref double propagationLength, bool terminateIfIntersects)
        {
            // Get the tracking boundary
            GridDirection trackingBoundary = propagatingSegment.TrackingBoundary;

            // If TrackingBoundary is set to none, will call checkFractureIntersection
            if (trackingBoundary == GridDirection.None)
                return checkFractureIntersection(propagatingSegment, intersecting_fs, ref propagationLength, terminateIfIntersects);

            // Calculate the angle of intersection, given by the strike of the intersecting fracture set - the strike of the propagating fracture set (NB may not be propagation direction)
            double intersectionAngle = intersecting_fs.Strike - Strike;

            // Set return value to false initially
            bool intersects = false;

            // Calculate sin and cosine of intersection angle
            double sin_intersectionAngle = VectorXYZ.Sin_trim(intersectionAngle);
            double cos_intersectionAngle = VectorXYZ.Cos_trim(intersectionAngle);

            // Cache coordinates and direction of propagating segment locally
            // Note that we do not need to check if the propagating segment lies within the width range of the intersecting segment, since they will both lie on the boundary
            double propNode_I = propagatingSegment.PropNode.I;
            //double propNode_J = propagatingSegment.PropNode.J;
            PropagationDirection propNode_dir = propagatingSegment.LocalPropDir;

            // If the intersection angle is +/-(pi/2) we can simplify the coordinate conversion and intersection calculations
            double orthogonal_sin_cutoff = 0.999; // If the sine of the intersection angle is higher than this value we will assume the fracture sets are orthogonal
            double parallel_cos_cutoff = 0.999; // If the cosine of the intersection angle is higher than this value we will assume the fracture sets are parallel
            IntersectionAngle orthogonalFractures;
            if (sin_intersectionAngle > orthogonal_sin_cutoff) // Intersection angle = +90deg (propagating fracture set = hmin, intersecting fracture set = hmax) 
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Orthogonal_Plus;
            }
            else if (sin_intersectionAngle < -orthogonal_sin_cutoff) // Intersection angle = -90deg (propagating fracture set = hmax, intersecting fracture set = hmin) 
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Orthogonal_Minus;
            }
            else if (Math.Abs(cos_intersectionAngle) > parallel_cos_cutoff) // Intersection angle = 0deg (propagating fracture set = intersecting fracture set)
            {
                // Set flag for parallel fractures
                orthogonalFractures = IntersectionAngle.Parallel;
            }
            else // Propagating fractures are oblique to intersecting fractures
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Oblique;
            }

            // Loop through every macrofracture segment and check if either of the segment nodes intersect the projection line of the propagating fracture
            foreach (PropagationDirection MF_PropDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                foreach (MacrofractureSegmentIJK intersecting_MF_segment in intersecting_fs.LocalDFNMacrofractureSegments[MF_PropDir])
                {
                    // Check if either of the segment nodes lie on the tracked boundary, and if so add them to a list
                    List<PointIJK> segmentNodes = new List<PointIJK>();
                    if (intersecting_MF_segment.NonPropNodeBoundary == trackingBoundary) segmentNodes.Add(intersecting_MF_segment.NonPropNode);
                    if (intersecting_MF_segment.PropNodeBoundary == trackingBoundary) segmentNodes.Add(intersecting_MF_segment.PropNode);

                    // Loop through every segment node lying on the tracked boundary and check if it lies within the projection line of the propagating fracture
                    foreach (PointIJK intersectionPoint in segmentNodes)
                    {
                        // Get the I coordinate of the intersecting segment node
                        // NB intersecting_segment_I represents the coordinates of the intersecting segment in the frame of this fracture set
                        // These will be different from its coordinates in its own frame
                        double intersecting_segment_I;
                        switch (orthogonalFractures)
                        {
                            case IntersectionAngle.Orthogonal_Plus:
                                intersecting_segment_I = -intersectionPoint.J;
                                break;
                            case IntersectionAngle.Orthogonal_Minus:
                                intersecting_segment_I = intersectionPoint.J;
                                break;
                            case IntersectionAngle.Oblique:
                                intersecting_segment_I = (intersectionPoint.I * cos_intersectionAngle) - (intersectionPoint.J * sin_intersectionAngle);
                                break;
                            case IntersectionAngle.Parallel:
                                // Fracture sets are parallel so IJK coordinates will not change
                                intersecting_segment_I = intersectionPoint.I;
                                break;
                            default:
                                // Default sets intersecting segment I to 0
                                intersecting_segment_I = 0;
                                break;
                        }

                        // Check if the intersecting fracture node lies within the propagation range of the propagating segment
                        // The intersection criteria are slightly different for fractures propagating in the IPlus and IMinus directions so we must code these separately
                        switch (propNode_dir)
                        {
                            case PropagationDirection.IPlus:
                                // Check if the projection line of the propagating fracture intersects the segment
                                // NB we do not record an intersection if the tip of the projection line lies on the intersecting segment (i.e. propNode_I + propagationLength = intersecting_segment_I)
                                // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                                if ((propNode_I <= intersecting_segment_I) && ((propNode_I + propagationLength) > intersecting_segment_I))
                                {
                                    // Set the return value to true
                                    intersects = true;

                                    // Reduce the maximum propagation distance of the propagating fracture accordingly
                                    propagationLength = intersecting_segment_I - propNode_I;

                                    // Set the propagating macrofracture segment to inactive, due to intersection, and set reference to terminating macrofracture segment
                                    if (terminateIfIntersects)
                                    {
                                        propagatingSegment.PropNodeType = SegmentNodeType.Intersection;
                                        propagatingSegment.TerminatingSegment = intersecting_MF_segment;
                                    }

                                    // We also need to continue looping through all remaining macrofracture segments - one of them may interact earlier than the selected segment
                                }
                                break;
                            case PropagationDirection.IMinus:
                                // Check if the projection line of the propagating fracture intersects the segment
                                // NB we do not record an intersection if the tip of the projection line lies on the intersecting segment (i.e. propNode_I - propagationLength = intersecting_segment_I)
                                // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                                if ((propNode_I >= intersecting_segment_I) && ((propNode_I - propagationLength) < intersecting_segment_I))
                                {
                                    // Set the return value to true
                                    intersects = true;

                                    // Reduce the maximum propagation distance of the propagating fracture accordingly
                                    propagationLength = propNode_I - intersecting_segment_I;

                                    // Set the propagating macrofracture segment to inactive, due to intersection, and set reference to terminating macrofracture segment
                                    if (terminateIfIntersects)
                                    {
                                        propagatingSegment.PropNodeType = SegmentNodeType.Intersection;
                                        propagatingSegment.TerminatingSegment = intersecting_MF_segment;
                                    }

                                    // We need to continue looping through all remaining macrofracture segments with the opposite propagation direction - one of them may interact earlier than the selected segment
                                }
                                break;
                            default:
                                break;
                        } // End switch on propagation direction
                    } // End loop through every segment node lying on the tracked boundary
                } // End loop through every intersecting macrofracture segment
            } // End loop through every propagation direction for intersecting macrofracture segments

            return intersects;
        }
        /// <summary>
        /// Check whether a macrofracture segment from this fracture set crossing the gridblock boundary will intersect a boundary tracking macrofracture segment from another fracture set
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="intersecting_fs">Reference to the intersecting fracture set</param>
        /// <param name="checkPropagatingNode">If true, check the propagating node of the propagating fracture; if false, check the non-propagating node of the propagating fracture</param>
        /// <param name="terminateIfIntersects">If true, automatically flag propagating fracture segment as inactive due to intersection; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment intersects another macrofracture segment, otherwise false</returns>
        public bool checkFractureIntersectionOnBoundary(MacrofractureSegmentIJK propagatingSegment, Gridblock_FractureSet intersecting_fs, bool checkPropagatingNode, bool terminateIfIntersects)
        {
            // Get the correct node from the propagating segment, and check it lies on a boundary
            PointIJK nodeToCheck;
            GridDirection boundary;
            if (checkPropagatingNode)
            {
                nodeToCheck = propagatingSegment.PropNode;
                boundary = propagatingSegment.PropNodeBoundary;
            }
            else
            {
                nodeToCheck = propagatingSegment.NonPropNode;
                boundary = propagatingSegment.NonPropNodeBoundary;
            }

            // If the specified node does not lie on a boundary, return false
            if (boundary == GridDirection.None)
                return false;

            // Calculate the angle of intersection, given by the strike of the intersecting fracture set - the strike of the propagating fracture set (NB may not be propagation direction)
            double intersectionAngle = intersecting_fs.Strike - Strike;

            // Set return value to false initially
            bool intersects = false;

            // Calculate sin and cosine of intersection angle
            double sin_intersectionAngle = VectorXYZ.Sin_trim(intersectionAngle);
            double cos_intersectionAngle = VectorXYZ.Cos_trim(intersectionAngle);

            // Cache coordinates and direction of propagating segment locally
            // Note that we do not need to check if the propagating segment lies within the projection line of the intersecting segment, since any intersection will occur on the boundary
            //double nodeToCheck_I = nodeToCheck.I;
            double nodeToCheck_J = nodeToCheck.J;
            //PropagationDirection propNode_dir = propagatingSegment.CurrentPropDir;

            // If the intersection angle is +/-(pi/2) we can simplify the coordinate conversion and intersection calculations
            double orthogonal_sin_cutoff = 0.999; // If the sine of the intersection angle is higher than this value we will assume the fracture sets are orthogonal
            double parallel_cos_cutoff = 0.999; // If the cosine of the intersection angle is higher than this value we will assume the fracture sets are parallel
            IntersectionAngle orthogonalFractures;
            if (sin_intersectionAngle > orthogonal_sin_cutoff) // Intersection angle = +90deg (propagating fracture set = hmin, intersecting fracture set = hmax) 
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Orthogonal_Plus;
            }
            else if (sin_intersectionAngle < -orthogonal_sin_cutoff) // Intersection angle = -90deg (propagating fracture set = hmax, intersecting fracture set = hmin) 
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Orthogonal_Minus;
            }
            else if (Math.Abs(cos_intersectionAngle) > parallel_cos_cutoff) // Intersection angle = 0deg (propagating fracture set = intersecting fracture set)
            {
                // Set flag for parallel fractures
                orthogonalFractures = IntersectionAngle.Parallel;
            }
            else // Propagating fractures are oblique to intersecting fractures
            {
                // Set flag for orthogonal fractures
                orthogonalFractures = IntersectionAngle.Oblique;
            }

            // Loop through every macrofracture segment and check if it is tracking the correct boundary
            foreach (PropagationDirection MF_PropDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                foreach (MacrofractureSegmentIJK intersecting_MF_segment in intersecting_fs.LocalDFNMacrofractureSegments[MF_PropDir])
                {
                    if (intersecting_MF_segment.TrackingBoundary == boundary)
                    {
                        // Get the J coordinates of the two ends of the intersecting segment
                        // NB intersecting_segment_NPN_J, intersecting_segment_PN_J, intersecting_segment_MinJ and intersecting_segment_MaxJ represent the coordinates of the intersecting segment in the frame of this fracture set
                        // These will be different from its coordinates in its own frame
                        double intersecting_segment_NPN_J, intersecting_segment_PN_J;
                        double intersecting_segment_MinJ, intersecting_segment_MaxJ;
                        switch (orthogonalFractures)
                        {
                            case IntersectionAngle.Orthogonal_Plus:
                                {
                                    intersecting_segment_NPN_J = intersecting_MF_segment.NonPropNode.I;
                                    intersecting_segment_PN_J = intersecting_MF_segment.PropNode.I;
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            case IntersectionAngle.Orthogonal_Minus:
                                {
                                    intersecting_segment_NPN_J = -intersecting_MF_segment.NonPropNode.I;
                                    intersecting_segment_PN_J = -intersecting_MF_segment.PropNode.I;
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            case IntersectionAngle.Oblique:
                                {
                                    intersecting_segment_NPN_J = (intersecting_MF_segment.NonPropNode.I * sin_intersectionAngle) + (intersecting_MF_segment.NonPropNode.J * cos_intersectionAngle);
                                    intersecting_segment_PN_J = (intersecting_MF_segment.PropNode.I * sin_intersectionAngle) + (intersecting_MF_segment.PropNode.J * cos_intersectionAngle);
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            case IntersectionAngle.Parallel:
                                {
                                    // Fracture sets are parallel so IJK coordinates will not change
                                    intersecting_segment_NPN_J = intersecting_MF_segment.NonPropNode.J;
                                    intersecting_segment_PN_J = intersecting_MF_segment.PropNode.J;
                                    intersecting_segment_MinJ = Math.Min(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                    intersecting_segment_MaxJ = Math.Max(intersecting_segment_NPN_J, intersecting_segment_PN_J);
                                }
                                break;
                            default:
                                {
                                    intersecting_segment_NPN_J = 0;
                                    intersecting_segment_PN_J = 0;
                                    // Default sets MinJ > MaxJ so propagating segment will never lie within the width range of the intersecting segment
                                    intersecting_segment_MinJ = 1;
                                    intersecting_segment_MaxJ = -1;
                                }
                                break;
                        }

                        // Check if the propagating segment lies within the width range of the intersecting segment
                        if ((nodeToCheck_J >= intersecting_segment_MinJ) && (nodeToCheck_J <= intersecting_segment_MaxJ))
                        {
                            // Set the return value to true
                            intersects = true;

                            // Set the propagating macrofracture segment to inactive, due to intersection, and set reference to terminating macrofracture segment
                            if (terminateIfIntersects)
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.Intersection;
                                propagatingSegment.TerminatingSegment = intersecting_MF_segment;
                            }
                        }

                    } // End check if intersecting macrofracture segment lies on the tracked boundary
                } // End loop through every intersecting macrofracture segment
            } // End loop through every propagation direction for intersecting macrofracture segments

            return intersects;
        }
        /// <summary>
        /// Check whether a propagating macrofracture segment from this fracture set will intersect a gridblock boundary
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment intersects a gridblock boundary (m)</param>
        /// <param name="boundaryCrossed">Reference to a GridDirection enum; this will be set to indicate which boundary is intersected</param>
        /// <param name="terminateIfIntersects">If true, update propagating fracture segment status and flags; if false only update maximum propagation length</param>
        /// <param name="terminateIfNoNeighbour">If true, deactivate propagating fracture and reduce maximum propagation length even if neighbouring gridblock is null; if false only deactivate propagating fracture and reduce maximum propagation length if neighbouring gridblock is defined</param>
        /// <returns>True if the propagating fracture segment intersects a gridblock boundary, otherwise false</returns>
        public bool checkBoundaryIntersection(MacrofractureSegmentIJK propagatingSegment, ref double propagationLength, out GridDirection boundaryCrossed, bool terminateIfIntersects, bool terminateIfNoNeighbour)
        {
            // Check if a tracking boundary has been specified - if so call the checkCornerIntersection function
            if (propagatingSegment.TrackingBoundary != GridDirection.None)
                return checkCornerIntersection(propagatingSegment, ref propagationLength, out boundaryCrossed, terminateIfIntersects, terminateIfNoNeighbour);

            // Set the return value and the boundary crossed parameter to null to false initially
            bool crossesBoundary = false;
            boundaryCrossed = GridDirection.None;

            // Cache useful data locally
            double propNode_I = propagatingSegment.PropNode.I;
            double propNode_J = propagatingSegment.PropNode.J;
            PropagationDirection propNode_dir = propagatingSegment.LocalPropDir;

            // Create a list of outward boundary intersections along the line of the fracture, extended infinitely in each direction
            List<double> outwardBoundaryIntersections = new List<double>();
            List<GridDirection> outwardBoundaries = new List<GridDirection>();
            foreach (GridDirection boundary in new GridDirection[4] { GridDirection.N, GridDirection.E, GridDirection.S, GridDirection.W })
            {
                // Get the boundary segment endpoints
                double boundaryleftI, boundaryleftJ, boundaryrightI, boundaryrightJ;
                getBoundaryEndPoints(boundary, out boundaryleftI, out boundaryleftJ, out boundaryrightI, out boundaryrightJ);

                // Get the intersection point between the fracture and the boundary segment
                bool crossesOutwards;
                double intersectionI = getBoundaryIntersection(propNode_J, boundary, propNode_dir, out crossesOutwards);

                // If there is an intersection point crossing outwards, add it to the list, then move on to the next boundary
                if (!double.IsNaN(intersectionI) && crossesOutwards)
                {
                    outwardBoundaryIntersections.Add(intersectionI);
                    outwardBoundaries.Add(boundary);
                }
            } // End loop through boundaries
            int noBoundariesCrossed = outwardBoundaryIntersections.Count;

            // If there are no boundary intersections in the extended line of the fracture, something has gone wrong
            // In this case we set the propagation length to zero but do not deactivate the fracture
            if (noBoundariesCrossed < 1)
            {
                propagationLength = 0;
                //throw (new Exception(string.Format("Fracture in cell at {0},{1} azimuth {2}, propagating node at I={3},J={4} has no boundary intersections", gbc.SWtop.X, gbc.SWtop.Y, Azimuth, propNode_I, propNode_J)));
            }
            // If there is only one outward boundary intersection, we only need to check if the boundary lies behind the projected tip of the fracture
            // This will record a boundary crossing even if the current fracture tip is already outside the gridblock - this is necessary for inverted gridblocks
            else if (noBoundariesCrossed == 1)
            {
                // Get the intersection point and boundary intersected
                double intersectionI = outwardBoundaryIntersections[0];
                GridDirection boundary = outwardBoundaries[0];

                // Check if the boundary intersection point lies within the projection line of the fracture (i.e. will the fracture cross the boundary within the propagation specified distance)
                // This calculation is slightly different for fractures propagating in the IPlus and IMinus directions
                if (propNode_dir == PropagationDirection.IPlus) // Propagating fracture is propagating in the IPlus direction
                {
                    // NB we do not record an interaction if the tip of the projection line lies on the intersecting segment (i.e. intersectionI = propNode_I + propagationLength)
                    // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                    if (intersectionI < propNode_I + propagationLength)
                    {
                        // Set the return value to true
                        crossesBoundary = true;

                        // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                        boundaryCrossed = boundary;
                        bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                        // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                        if (!NoNeighbour || terminateIfNoNeighbour)
                            propagationLength = intersectionI - propNode_I;

                        // Set the propagating macrofracture segment status and flags, if we have specified to do so
                        // The exact status and flags will depend on the boundary type and input settings
                        // NB there is no terminating fracture segment so we set this reference to null
                        if (terminateIfIntersects)
                        {
                            if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                propagatingSegment.TerminatingSegment = null;
                            }
                            else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the fracture anyway, set NodeType to NonconnectedGridblockBound
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                propagatingSegment.TerminatingSegment = null;
                            }
                            // If we have specified not to terminate the fracture when the neighbour is null, leave fracture as active and NodeType as Propagating
                        } // End set the propagating macrofracture segment status and flags
                    } // End check if the boundary intersection point lies behind the projected fracture tip
                } // End Propagating fracture is propagating in the IPlus direction
                else // Propagating fracture is propagating in the IMinus direction
                {
                    // NB we do not record an interaction if the tip of the projection line lies on the intersecting segment (i.e. intersectionI = propNode_I - propagationLength)
                    // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                    if (intersectionI > propNode_I - propagationLength)
                    {
                        // Set the return value to true
                        crossesBoundary = true;

                        // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                        boundaryCrossed = boundary;
                        bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                        // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                        if (!NoNeighbour || terminateIfNoNeighbour)
                            propagationLength = propNode_I - intersectionI;

                        // Set the propagating macrofracture segment status and flags, if we have specified to do so
                        // The exact status and flags will depend on the boundary type and input settings
                        // NB there is no terminating fracture segment so we set this reference to null
                        if (terminateIfIntersects)
                        {
                            if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                propagatingSegment.TerminatingSegment = null;
                            }
                            else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the fracture anyway, set NodeType to NonconnectedGridblockBound
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                propagatingSegment.TerminatingSegment = null;
                            }
                            // If we have specified not to terminate the fracture when the neighbour is null, leave fracture as active and NodeType as Propagating
                        } // End set the propagating macrofracture segment status and flags
                    } // End check if the boundary intersection point lies behind the projected fracture tip
                } // End Propagating fracture is propagating in the IMinus direction
            } // End if there is only one outward boundary intersection
            // If there is more than one outward boundary intersection, the cell must be concave
            // In this case we need to check if the boundary lies in front of the current tip of the fracture as well as behind the projected tip of the fracture
            // This will only record a boundary crossing if the current fracture tip is inside the gridblock and behind the boundary - this is necessary to prevent intersections with concave boundary segments being recorded
            // However we must also check that there is at least one boundary ahead of the propagating fracture
            // If there is not, we set the propagation length to zero but do not deactivate the fracture - this can occasionally happen where an inverted cell bounds a concave cell so a fracture nucleates on the wrong side of the boundary segment 
            else
            {
                bool boundaryAhead = false;

                // We will need to check each boundary intersection
                for (int boundaryNo = 0; boundaryNo < noBoundariesCrossed; boundaryNo++)
                {
                    // Get the intersection point and boundary intersected
                    double intersectionI = outwardBoundaryIntersections[boundaryNo];
                    GridDirection boundary = outwardBoundaries[boundaryNo];

                    // Check if the boundary intersection point lies within the projection line of the fracture (i.e. will the fracture cross the boundary within the propagation specified distance)
                    // This calculation is slightly different for fractures propagating in the IPlus and IMinus directions
                    if (propNode_dir == PropagationDirection.IPlus) // Propagating fracture is propagating in the IPlus direction
                    {
                        // NB we do not record an interaction if the tip of the projection line lies on the intersecting segment (i.e. intersectionI = propNode_I + propagationLength)
                        // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                        // An intersection will be recorded if the fracture tip lies on the intersecting segment, within rounding error
                        if ((float)intersectionI >= (float)propNode_I)
                        {
                            boundaryAhead = true;

                            if (intersectionI < propNode_I + propagationLength)
                            {
                                // Set the return value to true
                                crossesBoundary = true;

                                // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                                boundaryCrossed = boundary;
                                bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                                // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                                if (!NoNeighbour || terminateIfNoNeighbour)
                                    propagationLength = intersectionI - propNode_I;

                                // Set the propagating macrofracture segment status and flags, if we have specified to do so
                                // The exact status and flags will depend on the boundary type and input settings
                                // NB there is no terminating fracture segment so we set this reference to null
                                if (terminateIfIntersects)
                                {
                                    if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                                    {
                                        propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                        propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                        propagatingSegment.TerminatingSegment = null;
                                    }
                                    else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the fracture anyway, set NodeType to NonconnectedGridblockBound
                                    {
                                        propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                        propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                        propagatingSegment.TerminatingSegment = null;
                                    }
                                    // If we have specified not to terminate the fracture when the neighbour is null, leave fracture as active and NodeType as Propagating
                                } // End set the propagating macrofracture segment status and flags
                            } // End check if the boundary intersection point lies behind the projected fracture tip
                        } // End check if the boundary intersection point lies ahead of the current fracture tip
                    } // End Propagating fracture is propagating in the IPlus direction
                    else // Propagating fracture is propagating in the IMinus direction
                    {
                        // NB we do not record an interaction if the tip of the projection line lies on the intersecting segment (i.e. intersectionI = propNode_I - propagationLength)
                        // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                        // An intersection will be recorded if the fracture tip lies on the intersecting segment
                        if ((float)intersectionI <= (float)propNode_I)
                        {
                            boundaryAhead = true;

                            if (intersectionI > propNode_I - propagationLength)
                            {
                                // Set the return value to true
                                crossesBoundary = true;

                                // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                                boundaryCrossed = boundary;
                                bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                                // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                                if (!NoNeighbour || terminateIfNoNeighbour)
                                    propagationLength = propNode_I - intersectionI;

                                // Set the propagating macrofracture segment status and flags, if we have specified to do so
                                // The exact status and flags will depend on the boundary type and input settings
                                // NB there is no terminating fracture segment so we set this reference to null
                                if (terminateIfIntersects)
                                {
                                    if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                                    {
                                        propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                        propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                        propagatingSegment.TerminatingSegment = null;
                                    }
                                    else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the fracture anyway, set NodeType to NonconnectedGridblockBound
                                    {
                                        propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                        propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                        propagatingSegment.TerminatingSegment = null;
                                    }
                                    // If we have specified not to terminate the fracture when the neighbour is null, leave fracture as active and NodeType as Propagating
                                }
                            } // End set the propagating macrofracture segment status and flags
                        } // End check if the boundary intersection point lies behind the projected fracture tip
                    } // End check if the boundary intersection point lies ahead of the current fracture tip
                } // End loop through each boundary intersection

                // If there are no boundary intersections ahead of the fracture tip, we set the propagation length to zero but do not deactivate the fracture
                if (!boundaryAhead)
                {
                    propagationLength = 0;
                    //throw (new Exception(string.Format("Fracture in cell at {0},{1} azimuth {2}, propagating node at I={3},J={4} has no boundary intersections", gbc.SWtop.X, gbc.SWtop.Y, Azimuth, propNode_I, propNode_J)));
                }
            } // End if there is more than one outward boundary intersection

            // Return true if the fracture will intersect a boundary, otherwise false
            return crossesBoundary;
        }
        /// <summary>
        /// Check whether a boundary tracking macrofracture segment from this fracture set will cross another gridblock boundary
        /// </summary>
        /// <param name="propagatingSegment">Reference to a MacrofractureSegmentIJK object representing the propagating fracture segment</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this segment will propagate; this will be altered if the propagating fracture segment intersects a gridblock boundary (m)</param>
        /// <param name="boundaryCrossed">Reference to a GridDirection enum; this will be set to indicate which boundary is intersected</param>
        /// <param name="terminateIfIntersects">If true, update propagating fracture segment status and flags; if false only update maximum propagation length</param>
        /// <param name="terminateIfNoNeighbour">If true, deactivate propagating fracture and reduce maximum propagation length even if neighbouring gridblock is null; if false only deactivate propagating fracture and reduce maximum propagation length if neighbouring gridblock is defined</param>
        /// <returns>True if the propagating fracture segment intersects a gridblock boundary, otherwise false</returns>
        public bool checkCornerIntersection(MacrofractureSegmentIJK propagatingSegment, ref double propagationLength, out GridDirection boundaryCrossed, bool terminateIfIntersects, bool terminateIfNoNeighbour)
        {
            // Get the tracking boundary
            GridDirection trackingBoundary = propagatingSegment.TrackingBoundary;

            // If TrackingBoundary is set to none, will call checkBoundaryIntersection
            if (trackingBoundary == GridDirection.None)
                return checkBoundaryIntersection(propagatingSegment, ref propagationLength, out boundaryCrossed, terminateIfIntersects, terminateIfNoNeighbour);

            // Set the boundary crossed parameter to null
            boundaryCrossed = GridDirection.None;

            // Cache useful data locally
            double propNode_I = propagatingSegment.PropNode.I;
            PropagationDirection propNode_dir = propagatingSegment.LocalPropDir;

            // Create local variables for the direction and I coordinate of the boundary the fracture is propagating towards
            GridDirection targetBoundary = GridDirection.None;
            double targetBoundaryI = -1;

            // The intersection criteria are slightly different for fractures propagating in the IPlus and IMinus directions so we must code these separately
            if (propNode_dir == PropagationDirection.IPlus) // Propagating fracture is propagating in the IPlus direction
            {
                // Find out which boundary the fracture is propagating towards, and get the I coordinate of the cornerpoint
                switch (trackingBoundary)
                {
                    case GridDirection.N:
                        {
                            if (NWMidPoint.I > NEMidPoint.I) // If I coordinate for the NW corner is greater than the I coordinate for the NE corner, the fracture is propagating towards the W boundary
                            {
                                targetBoundary = GridDirection.W;
                                targetBoundaryI = NWMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the E boundary
                            {
                                targetBoundary = GridDirection.E;
                                targetBoundaryI = NEMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.E:
                        {
                            if (NEMidPoint.I > SEMidPoint.I) // If I coordinate for the NE corner is greater than the I coordinate for the SE corner, the fracture is propagating towards the N boundary
                            {
                                targetBoundary = GridDirection.N;
                                targetBoundaryI = NEMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the S boundary
                            {
                                targetBoundary = GridDirection.S;
                                targetBoundaryI = SEMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.S:
                        {
                            if (SWMidPoint.I > SEMidPoint.I) // If I coordinate for the SW corner is greater than the I coordinate for the SE corner, the fracture is propagating towards the W boundary
                            {
                                targetBoundary = GridDirection.W;
                                targetBoundaryI = SWMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the E boundary
                            {
                                targetBoundary = GridDirection.E;
                                targetBoundaryI = SEMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.W:
                        {
                            if (NWMidPoint.I > SWMidPoint.I) // If I coordinate for the NW corner is greater than the I coordinate for the SW corner, the fracture is propagating towards the N boundary
                            {
                                targetBoundary = GridDirection.N;
                                targetBoundaryI = NWMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the S boundary
                            {
                                targetBoundary = GridDirection.S;
                                targetBoundaryI = SWMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.None:
                        break;
                    default:
                        break;
                }

                // Check if the fracture is propagating towards any boundary
                if (targetBoundary != GridDirection.None)
                {
                    // Check if the projected fracture tip lies beyond the target corner
                    // NB we do not record an interaction if the tip of the projection line lies on the corner (i.e. targetBoundaryI = propNode_I + propagationLength)
                    // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                    // Also we do not check if the current fracture tip lies within the boundary - this should always be the case, but not checking allows it always indentify instances where the tip lies on the boundary, allowing for rounding errors
                    if (targetBoundaryI < propNode_I + propagationLength)
                    {
                        // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                        boundaryCrossed = targetBoundary;
                        bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                        // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                        if (!NoNeighbour || terminateIfNoNeighbour)
                            propagationLength = targetBoundaryI - propNode_I;

                        // Set the propagating macrofracture segment status and flags, if we have specified to do so
                        // The exact status and flags will depend on the boundary type and input settings
                        if (terminateIfIntersects)
                        {
                            if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                            }
                            else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the fracture anyway, set NodeType to NonconnectedGridblockBound
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                            }
                            // If we have specified not to terminate the fracture when the neighbour is null, leave fracture as active and NodeType as Propagating
                        }

                        // Since a fracture can cross only one boundary, we do not need to check the others
                        return true;
                    } // End check if projected fracture tip lies beyond the target corner 
                } // End check if the fracture is propagating towards any boundary
            } // End Propagating fracture is propagating in the IPlus direction
            else // Propagating fracture is propagating in the IMinus direction
            {
                // Find out which boundary the fracture is propagating towards, and get the I coordinate of the cornerpoint
                switch (trackingBoundary)
                {
                    case GridDirection.N:
                        {
                            if (NWMidPoint.I < NEMidPoint.I) // If I coordinate for the NW corner is less than the I coordinate for the NE corner, the fracture is propagating towards the W boundary
                            {
                                targetBoundary = GridDirection.W;
                                targetBoundaryI = NWMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the E boundary
                            {
                                targetBoundary = GridDirection.E;
                                targetBoundaryI = NEMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.E:
                        {
                            if (NEMidPoint.I < SEMidPoint.I) // If I coordinate for the NE corner is less than the I coordinate for the SE corner, the fracture is propagating towards the N boundary
                            {
                                targetBoundary = GridDirection.N;
                                targetBoundaryI = NEMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the S boundary
                            {
                                targetBoundary = GridDirection.S;
                                targetBoundaryI = SEMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.S:
                        {
                            if (SWMidPoint.I < SEMidPoint.I) // If I coordinate for the SW corner is less than the I coordinate for the SE corner, the fracture is propagating towards the W boundary
                            {
                                targetBoundary = GridDirection.W;
                                targetBoundaryI = SWMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the E boundary
                            {
                                targetBoundary = GridDirection.E;
                                targetBoundaryI = SEMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.W:
                        {
                            if (NWMidPoint.I < SWMidPoint.I) // If I coordinate for the NW corner is less than the I coordinate for the SW corner, the fracture is propagating towards the N boundary
                            {
                                targetBoundary = GridDirection.N;
                                targetBoundaryI = NWMidPoint.I;
                            }
                            else // Otherwise the fracture is propagating towards the S boundary
                            {
                                targetBoundary = GridDirection.S;
                                targetBoundaryI = SWMidPoint.I;
                            }
                        }
                        break;
                    case GridDirection.None:
                        break;
                    default:
                        break;
                }

                // Check if the fracture is propagating towards any boundary
                if (targetBoundary != GridDirection.None)
                {
                    // Check if the projected fracture tip lies beyond the target corner
                    // NB we do not record an interaction if the tip of the projection line lies on the corner (i.e. targetBoundaryI = propNode_I + propagationLength)
                    // This is so that there will be no intersection whenever the function returns a propagation length equal to the input length
                    // Also we do not check if the current fracture tip lies within the boundary - this should always be the case, but not checking allows it always indentify instances where the tip lies on the boundary, allowing for rounding errors
                    if (targetBoundaryI > propNode_I - propagationLength)
                    {
                        // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                        boundaryCrossed = targetBoundary;
                        bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                        // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                        if (!NoNeighbour || terminateIfNoNeighbour)
                            propagationLength = propNode_I - targetBoundaryI;

                        // Set the propagating macrofracture segment status and flags, if we have specified to do so
                        // The exact status and flags will depend on the boundary type and input settings
                        if (terminateIfIntersects)
                        {
                            if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                            }
                            else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the fracture anyway, set NodeType to NonconnectedGridblockBound
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                            }
                            // If we have specified not to terminate the fracture when the neighbour is null, leave fracture as active and NodeType as Propagating
                        }

                        // Since a fracture can cross only one boundary, we do not need to check the others
                        return true;
                    } // End check if projected fracture tip lies beyond the target corner 
                } // End check if the fracture is propagating towards any boundary
            } // End Propagating fracture is propagating in the IPlus direction

            // We have not intersected any boundaries
            return false;
        }

        // Reset and data input functions
        /// <summary>
        /// Set the fracture aperture control data for uniform and size-dependent aperture independently for Mode 1 and Mode 2 fractures
        /// </summary>
        /// <param name="Mode1_UniformAperture_in">Fixed aperture for Mode 1 fractures in the uniform aperture case (m)</param>
        /// <param name="Mode2_UniformAperture_in">Fixed aperture for Mode 2 fractures in the uniform aperture case (m)</param>
        /// <param name="Mode1_SizeDependentApertureMultiplier_in">Multiplier for Mode 1 fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        /// <param name="Mode2_SizeDependentApertureMultiplier_in">Multiplier for Mode 2 fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public void SetFractureApertureControlData(double Mode1_UniformAperture_in, double Mode2_UniformAperture_in, double Mode1_SizeDependentApertureMultiplier_in, double Mode2_SizeDependentApertureMultiplier_in)
        {
            foreach (FractureDipSet fds in FractureDipSets)
            {
                fds.UniformAperture = (fds.Mode == FractureMode.Mode1 ? Mode1_UniformAperture_in : Mode2_UniformAperture_in);
                fds.SizeDependentApertureMultiplier = (fds.Mode == FractureMode.Mode1 ? Mode1_SizeDependentApertureMultiplier_in : Mode2_SizeDependentApertureMultiplier_in);
            }
        }
        /// <summary>
        /// Set the fracture aperture control data for uniform and size-dependent aperture independently for Mode 1 and Mode 2 fractures
        /// </summary>
        /// <param name="UniformAperture_in">Fixed aperture for fractures in the uniform aperture case (m)</param>
        /// <param name="SizeDependentApertureMultiplier_in">Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public void SetFractureApertureControlData(double UniformAperture_in, double SizeDependentApertureMultiplier_in)
        {
            foreach (FractureDipSet fds in FractureDipSets)
            {
                fds.UniformAperture = UniformAperture_in;
                fds.SizeDependentApertureMultiplier = SizeDependentApertureMultiplier_in;
            }
        }

        // Functions to return default numbers of and labels for dipsets
        /// <summary>
        /// Get the default number of fracture dipsets that will be created for the given parameters
        /// </summary>
        /// <param name="Mode1Only">Flag to generate Mode 1 vertical fractures only</param>
        /// <param name="Mode2Only">Flag to generate Mode 2 inclined fractures only</param>
        /// <param name="BiazimuthalConjugate">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <returns>Default number of fracture dipsets</returns>
        public static int DefaultDipSets(bool Mode1Only, bool Mode2Only, bool BiazimuthalConjugate, bool IncludeReverseFractures)
        {
            if (Mode1Only)
                return 1;
            else if (Mode2Only)
                return 1;
            else
            {
                if (BiazimuthalConjugate)
                {
                    if (IncludeReverseFractures)
                        return 3;
                    else
                        return 2;
                }
                else
                {
                    if (IncludeReverseFractures)
                        return 5;
                    else
                        return 3;
                }
            }
        }
        /// <summary>
        /// Get a list of default labels for the fracture dipsets that will be created for the given parameters
        /// </summary>
        /// <param name="Mode1Only">Flag to generate Mode 1 vertical fractures only</param>
        /// <param name="Mode2Only">Flag to generate Mode 2 inclined fractures only</param>
        /// <param name="BiazimuthalConjugate">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <returns>List of strings representing default labels for each of the fracture dipsets, in order of index number</returns>
        public static List<string> DefaultDipSetLabels(bool Mode1Only, bool Mode2Only, bool BiazimuthalConjugate, bool IncludeReverseFractures)
        {
            List<string> output = new List<string>();
            if (Mode1Only)
            {
                output.Add("Vertical");
            }
            else if (Mode2Only)
            {
                output.Add("InclinedNormal");
            }
            else
            {
                output.Add("Vertical");
                if (BiazimuthalConjugate)
                {
                    output.Add("InclinedSteep");
                }
                else
                {
                    output.Add("InclinedSteep_R");
                    output.Add("InclinedSteep_L");
                }
                if (IncludeReverseFractures)
                {
                    if (BiazimuthalConjugate)
                    {
                        output.Add("InclinedShallow");
                    }
                    else
                    {
                        output.Add("InclinedShallow_R");
                        output.Add("InclinedShallow_L");
                    }
                }

            }
            return output;
        }
        /// <summary>
        /// Get a list of labels for the fracture dipsets in this fracture set
        /// </summary>
        /// <returns>List of strings representing labels for each of the fracture dipsets, in order of index number</returns>
        public List<string> DipSetLabels()
        {
            List<string> output = new List<string>();
            foreach (FractureDipSet fds in FractureDipSets)
            {
                if ((float)fds.Dip <= 0f)
                {
                    output.Add("Horizontal");
                }
                else if ((float)fds.Dip < (float)(Math.PI / 4))
                {
                    if (fds.BiazimuthalConjugate)
                        output.Add("InclinedShallow");
                    else
                        output.Add("InclinedShallow_R");
                }
                else if ((float)fds.Dip < (float)(Math.PI / 2))
                {
                    if (fds.BiazimuthalConjugate)
                        output.Add("InclinedSteep");
                    else
                        output.Add("InclinedSteep_R");
                }
                else if ((float)fds.Dip == (float)(Math.PI / 2))
                {
                    output.Add("Vertical");
                }
                else if ((float)fds.Dip <= (float)(Math.PI * 3 / 4))
                {
                    output.Add("InclinedSteep_L");
                }
                else if ((float)fds.Dip < (float)(Math.PI))
                {
                    output.Add("InclinedShallow_L");
                }
                else
                {
                    output.Add("Horizontal");
                }
            }
            return output;
        }

        // Constructors
        /// <summary>
        /// Default constructor: Create an empty fracture set
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        public Gridblock_FractureSet(GridblockConfiguration gbc_in)
        {
            // Reference to grandparent GridblockConfiguration object
            gbc = gbc_in;

            // Fracture strike: set to 0
            Strike = 0;

            // Set the azimuthal and horizontal shear strains to default values
            eaad = 0;
            easd = 0;
            eaa2d_eh2d = 1;
            eaaasd_eh2d = 0;

            // As a default set the fracture distribution to EvenlyDistributedStress (random) - this may change when the fractures are generated
            FractureDistribution = StressDistribution.EvenlyDistributedStress;

            // Create an empty list for fracture dip set objects
            FractureDipSets = new List<FractureDipSet>();

            // Create the FractureDipSets_SortedByW list as an empty list
            FractureDipSets_SortedByW = new List<FractureDipSet>();

            // Create empty lists for the local gridblock DFN
            LocalDFNMicrofractures = new List<MicrofractureIJK>();
            LocalDFNMacrofractureSegments = new Dictionary<PropagationDirection, List<MacrofractureSegmentIJK>>();
            foreach (PropagationDirection propDir in Enum.GetValues(typeof(PropagationDirection)).Cast<PropagationDirection>())
            {
                List<MacrofractureSegmentIJK> directional_LocalDFNMacrofractureSegments = new List<MacrofractureSegmentIJK>();
                LocalDFNMacrofractureSegments.Add(propDir, directional_LocalDFNMacrofractureSegments);
            }

            // Set the maximum historic active macrofracture volumetric ratio to 0
            max_historic_a_MFP33 = 0;
        }
        /// <summary>
        /// Constructor: Create fracture set with multiple dip sets in optimal orientations for dilatant, normal and (if required) reverse shear displacement  
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public Gridblock_FractureSet(GridblockConfiguration gbc_in, double Strike_in, double B_in, double c_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in) : this(gbc_in)
        {
            // Fracture strike
            Strike = Strike_in;

            // Create vertical Mode 1 dip set and add it to the list
            double opt_dip = Math.PI / 2;
            FractureDipSet DipSet1 = new FractureDipSet(gbc, this, FractureMode.Mode1, BiazimuthalConjugate_in, IncludeReverseFractures_in, opt_dip, B_in, c_in);
            FractureDipSets.Add(DipSet1);

            // If the set is a biazimuthal conjugate set, only one inclined dip set will be created, containing fractures dipping in both directions
            // // Otherwise two inclined dip sets will be created dipping in opposite directions, each containing half the initial microfracture population
            opt_dip = ((Math.PI / 2) + Math.Atan(gbc.MechProps.MuFr)) / 2;
            if (BiazimuthalConjugate_in)
            {
                // Create one optimally oriented biazimuthal conjugate Mode 2 dip set and add it to the list
                FractureDipSet DipSet2 = new FractureDipSet(gbc, this, FractureMode.Mode2, true, IncludeReverseFractures_in, opt_dip, B_in, c_in);
                FractureDipSets.Add(DipSet2);
            }
            else
            {
                // Create two optimally oriented Mode 2 dip sets and add them to the list
                FractureDipSet DipSet2 = new FractureDipSet(gbc, this, FractureMode.Mode2, false, IncludeReverseFractures_in, opt_dip, B_in / 2, c_in);
                FractureDipSets.Add(DipSet2);

                FractureDipSet DipSet3 = new FractureDipSet(gbc, this, FractureMode.Mode2, false, IncludeReverseFractures_in, Math.PI - opt_dip, B_in / 2, c_in);
                FractureDipSets.Add(DipSet3);
            }

            // If reverse fractures are allowed, create one or two additional low angle shear dip sets optimally oriented for reverse displacement
            // NB If the friction coefficient is 0, optimally oriented reverse fractures will have the same orientation as optimally oriented normal fractures, so there is no need to create an extra set
            if (IncludeReverseFractures_in && (gbc.MechProps.MuFr > 0))
            {
                opt_dip = ((Math.PI / 2) - Math.Atan(gbc.MechProps.MuFr)) / 2;
                if (BiazimuthalConjugate_in)
                {
                    // Create one optimally oriented biazimuthal conjugate Mode 2 dip set and add it to the list
                    FractureDipSet DipSet4 = new FractureDipSet(gbc, this, FractureMode.Mode2, true, IncludeReverseFractures_in, opt_dip, B_in, c_in);
                    FractureDipSets.Add(DipSet4);
                }
                else
                {
                    // Create two optimally oriented Mode 2 dip sets and add them to the list
                    FractureDipSet DipSet4 = new FractureDipSet(gbc, this, FractureMode.Mode2, false, IncludeReverseFractures_in, opt_dip, B_in / 2, c_in);
                    FractureDipSets.Add(DipSet4);

                    FractureDipSet DipSet5 = new FractureDipSet(gbc, this, FractureMode.Mode2, false, IncludeReverseFractures_in, Math.PI - opt_dip, B_in / 2, c_in);
                    FractureDipSets.Add(DipSet5);
                }
            }

            // Create the FractureDipSets_SortedByW list as a copy of the FractureDipSets list, and sort it by stress shadow width
            FractureDipSets_SortedByW = new List<FractureDipSet>(FractureDipSets);
            FractureDipSets_SortedByW.Sort();
        }
        /// <summary>
        /// Constructor: Create fracture set with a single bimodal conjugate dip set of specified mode and dip
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="Mode_in">Fracture mode</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        public Gridblock_FractureSet(GridblockConfiguration gbc_in, double Strike_in, FractureMode Mode_in, double Dip_in, double B_in, double c_in, bool IncludeReverseFractures_in) : this(gbc_in)
        {
            // Fracture strike
            Strike = Strike_in;

            // Create fracture dip set and add it to the list
            FractureDipSet DipSet = new FractureDipSet(gbc, this, Mode_in, true, IncludeReverseFractures_in, Dip_in, B_in, c_in);
            FractureDipSets.Add(DipSet);

            // Create the FractureDipSets_SortedByW list as a copy of the FractureDipSets list, and sort it by stress shadow width
            FractureDipSets_SortedByW = new List<FractureDipSet>(FractureDipSets);
            FractureDipSets_SortedByW.Sort();
        }
        /// <summary>
        /// Constructor: Create fracture set with two bimodal conjugate dip sets: vertical Mode 1 and optimally oriented Mode 2
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="BiazimuthalConjugate_in">Flag for a biazimuthal conjugate dipset: if true, one dip set will be created containing equal numbers of fractures dipping in both directions; if false, the two dip sets will be created containing fractures dipping in opposite directions</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if true, additional dip sets will be created in the optimal orientation for reverse displacement; if false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <param name="Mode1_UniformAperture_in">Fixed aperture for Mode 1 fractures in the uniform aperture case (m)</param>
        /// <param name="Mode2_UniformAperture_in">Fixed aperture for Mode 2 fractures in the uniform aperture case (m)</param>
        /// <param name="Mode1_SizeDependentApertureMultiplier_in">Multiplier for Mode 1 fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        /// <param name="Mode2_SizeDependentApertureMultiplier_in">Multiplier for Mode 2 fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public Gridblock_FractureSet(GridblockConfiguration gbc_in, double Strike_in, double B_in, double c_in, bool BiazimuthalConjugate_in, bool IncludeReverseFractures_in, double Mode1_UniformAperture_in, double Mode2_UniformAperture_in, double Mode1_SizeDependentApertureMultiplier_in, double Mode2_SizeDependentApertureMultiplier_in)
            : this(gbc_in, Strike_in, B_in, c_in, BiazimuthalConjugate_in, IncludeReverseFractures_in)
        {
            // Set the fracture aperture control data for uniform and size-dependent aperture
            SetFractureApertureControlData(Mode1_UniformAperture_in, Mode2_UniformAperture_in, Mode1_SizeDependentApertureMultiplier_in, Mode2_SizeDependentApertureMultiplier_in);
        }
        /// <summary>
        /// Constructor: Create fracture set with a single bimodal conjugate dip set of specified mode and dip
        /// </summary>
        /// <param name="gbc_in">Reference to grandparent GridblockConfiguration object</param>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="Mode_in">Fracture mode</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="IncludeReverseFractures_in">Flag to allow reverse fractures: if set to false, fracture dipsets with a reverse displacement vector will not be allowed to accumulate displacement or grow</param>
        /// <param name="UniformAperture_in">Fixed aperture for fractures in the uniform aperture case (m)</param>
        /// <param name="SizeDependentApertureMultiplier_in">Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public Gridblock_FractureSet(GridblockConfiguration gbc_in, double Strike_in, FractureMode Mode_in, double Dip_in, double B_in, double c_in, bool IncludeReverseFractures_in, double UniformAperture_in, double SizeDependentApertureMultiplier_in)
            : this(gbc_in, Strike_in, Mode_in, Dip_in, B_in, c_in, IncludeReverseFractures_in)
        {
            // Set the fracture aperture control data for uniform and size-dependent aperture
            SetFractureApertureControlData(UniformAperture_in, SizeDependentApertureMultiplier_in);
        }
    }
}
