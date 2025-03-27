using System;
using System.Collections.Generic;
using System.Text;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Representation of a set of circular fractures of arbitrary orientation in an unconfined space
    /// </summary>
    class UnconfinedFractureSet
    {
        // References to external objects
        /// <summary>
        /// Reference to parent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;

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
        /// Variable for fracture dip
        /// </summary>
        private double dip;
        /// <summary>
        /// Variable for sine of fracture dip
        /// </summary>
        private double sindip;
        /// <summary>
        /// Variable for cosine of fracture dip
        /// </summary>
        private double cosdip;
        /// <summary>
        /// Unit vector on the fracture plane in direction of maximum dip
        /// </summary>
        private VectorXYZ dipVector;
        /// <summary>
        /// Variable for normal vector to the fracture
        /// </summary>
        private VectorXYZ normalVector;
        /// <summary>
        /// Strike of fracture set (radians); positive in the IPlus direction
        /// </summary>
        public double Strike { get { return strike; } private set { while (value >= (2 * Math.PI)) value -= (2 * Math.PI); while (value < 0) value += (2 * Math.PI); strike = value; strikeVector = VectorXYZ.GetLineVector(value, 0); azimuthVector = VectorXYZ.GetLineVector(value + (Math.PI / 2), 0); setCornerPoints(); } }
        /// <summary>
        /// Azimuth of fracture set (radians); positive in the JPlus direction
        /// </summary>
        public double Azimuth { get { double azimuth = strike + (Math.PI / 2); while (azimuth >= (2 * Math.PI)) azimuth -= (2 * Math.PI); return azimuth; } }
        /// <summary>
        /// Fracture dip
        /// </summary>
        public double Dip
        {
            get
            {
                return dip;
            }
            // The set method will set both the fracture dip and the normal and azimuth vectors
            private set
            {
                // Set the fracture dip
                dip = value;

                // Set the sine and cosine of fracture dip
                sindip = VectorXYZ.Sin_trim(Dip);
                cosdip = VectorXYZ.Cos_trim(Dip);

                // Create a new normal vector
                normalVector = VectorXYZ.GetNormalToPlane(Strike + (Math.PI / 2), value);

                // Create a new azimuth vector
                dipVector = VectorXYZ.GetLineVector(Strike + (Math.PI / 2), value);
            }
        }
        /// <summary>
        /// Normal vector to the fracture
        /// </summary>
        public VectorXYZ NormalVector { get { return new VectorXYZ(normalVector); } }
        /// <summary>
        /// Unit vector on the fracture plane in direction of maximum dip
        /// </summary>
        public VectorXYZ DipVector { get { return new VectorXYZ(dipVector); } }
        /// <summary>
        /// Horizontal unit vector in direction of fracture strike
        /// </summary>
        public VectorXYZ StrikeVector { get { return new VectorXYZ(strikeVector); } }
        /// <summary>
        /// Horizontal unit vector in direction of fracture azimuth
        /// </summary>
        public VectorXYZ AzimuthVector { get { return new VectorXYZ(azimuthVector); } }
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

        // Fracture distribution control data
        /// <summary>
        /// Initial microfracture distribution function - at present only Power Law is implemented
        /// </summary>
        public InitialFractureDistribution InitialDistribution { get; set; }
        /// <summary>
        /// Initial microfracture density coefficient B (/m3)
        /// </summary>
        public double CapB { get; set; }
        /// <summary>
        /// Initial microfracture distribution coefficient c
        /// </summary>
        public double c_coefficient { get; set; }
        /// <summary>
        /// Get the volumetric density of the initial fractures
        /// </summary>
        /// <returns></returns>
        public double InitialP30()
        {
            return InitialP30(MinimumFractureRadius);
        }
        /// <summary>
        /// Get the value of the initial microfracture distribution function for a specified fracture radius
        /// </summary>
        /// <param name="radius">Specified fracture radius</param>
        /// <returns></returns>
        private double InitialP30(double radius)
        {
            double P30 = 0;
            switch (InitialDistribution)
            {
                // Only Power Law is currently implemented
                case InitialFractureDistribution.PowerLaw:
                    P30 = CapB * Math.Pow(radius, -c_coefficient);
                    break;
                case InitialFractureDistribution.Exponential:
                    break;
                case InitialFractureDistribution.LogNormal:
                    break;
                default:
                    break;
            }
            return P30;
        }

        // Implicit fracture population data
        /// <summary>
        /// Number of rays comprising each fracture
        /// </summary>
        private int RaysPerFracture { get; set; }
        /// <summary>
        /// Minimum radius for a fracture; this will be the length of the rays at nucleation
        /// </summary>
        private double MinimumFractureRadius { get; set; }
        /// <summary>
        /// Object containing cumulative population data for all fractures
        /// </summary>
        private UnconfinedFractureData Fractures { get; set; }

        // Fracture data for previous timesteps
        /// <summary>
        /// Object containing minimal dynamic propagation data for the current timestep; updated as each timestep is calculated
        /// </summary>
        private FractureCalculationData_Minimised CurrentFractureData;
        /// <summary>
        /// Object containing a list of FractureCalculationData_Minimised objects with dynamic propagation data for the previous timesteps
        /// </summary>
        private FCD_List_Minimised PreviousFractureData;
        /// <summary>
        /// Flag to deactivate the fracture set at the start of the next timestep
        /// </summary>
        private bool DeactivateNextTimestep;

        // Fracture aperture control data - for uniform and size-dependent aperture, which are dependent on dip set
        // NB fracture aperture control data for dynamic and Barton-Bandis aperture are independent of dip set, so are contained in the MechanicalProperties object for the gridblock
        /// <summary>
        /// Fixed aperture for fractures in the uniform aperture case (m)
        /// </summary>
        public double UniformAperture { get; set; }
        /// <summary>
        /// Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier
        /// </summary>
        public double SizeDependentApertureMultiplier { get; set; }
        /// <summary>
        /// Current maximum aperture of a fracture of a specified radius
        /// </summary>
        /// <param name="radius">Fracture radius (m)</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumFractureAperture(double radius)
        {
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = 2 * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = 0;// -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (8 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = 0;// -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Current mean aperture of a fracture of a specified radius
        /// </summary>
        /// <param name="radius">Fracture radius (m)</param>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanFractureAperture(double radius)
        {
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = (4d / 3d) * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = 0;// -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (16 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = 0;// -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Fracture aperture based on Barton-Bandis model for a specified effective normal stress
        /// </summary>
        /// <param name="SigmaNeff">Effective normal stress on the fracture (Pa)</param>
        /// <returns></returns>
        public double BartonBandisAperture(double SigmaNeff)
        {
            // Cache variables locally and convert into units required by Barton-Bandis formulae
            // Convert effective normal stress on the fracture to MPa
            SigmaNeff = SigmaNeff / 1E+6;
            // Joint Roughness Coefficient
            double JRC = gbc.MechProps.JRC;
            // Ratio of unconfined compressive strength of unfractured rock to fractured rock
            double UCS_ratio = gbc.MechProps.UCS_ratio;
            // Initial normal stress on fracture (MPa)
            double InitialNormalStress = gbc.MechProps.InitialNormalStress / 1E+6;
            // Stiffness normal to the fracture, at initial normal stress (MPa/mm)
            double FractureNormalStiffness = gbc.MechProps.FractureNormalStiffness / 1E+9;
            // Maximum fracture closure (mm)
            double MaximumClosure = gbc.MechProps.MaximumClosure * 1E+3;

            // First calculate initial fracture aperture (i.e. the aperture at InitialNormalStress) in mm
            double a0 = (JRC / 5) * ((0.2 * UCS_ratio) - 0.1);
            // Then calculate stress-dependent fracture closure in mm
            // This is dependent on the fracture mode
            double delta_a;
            if (SigmaNeff < 0)
            {
                // If effective normal stress on the fracture is tensile, set it to zero
                SigmaNeff = Math.Max(SigmaNeff, 0);
                delta_a = SigmaNeff / (FractureNormalStiffness + (SigmaNeff / MaximumClosure));
            }
            else
            {
                // If effective normal stress on the fracture is less than the initial normal stress, set it to the initial normal stress
                SigmaNeff = Math.Max(SigmaNeff, InitialNormalStress);
                delta_a = (Math.Log10(SigmaNeff) - Math.Log10(InitialNormalStress)) * Math.Log(10) * (InitialNormalStress / FractureNormalStiffness);
            }
            // Subtract the fracture closure from the initial aperture, and convert to metres
            double aperture = (a0 - delta_a) / 1E+3;
            // If the calculated aperture is negative, set it to zero
            if (aperture < 0) aperture = 0;

            return aperture;
        }
        /// <summary>
        /// Get the Fracture compressibility, based on the aperture control data
        /// </summary>
        /// <returns>Elastic compressibility for Dynamic aperture, inverse of specified fracture normal stiffness for Barton-Bandis aperture, NaN for other apertures</returns>
        public double getFractureCompressibility(double radius)
        {
            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    // Fracture compressibility not defined for Uniform aperture
                    return double.NaN;
                case FractureApertureType.SizeDependent:
                    // Fracture compressibility not defined for Size Dependent aperture
                    return double.NaN;
                case FractureApertureType.Dynamic:
                    // Calculate compressibility based on elastic closure
                    double geometricFactor = 8 / Math.PI;
                    double elasticMod = (1 - Math.Pow(gbc.MechProps.Nu_r, 2)) / gbc.MechProps.E_r;
                    double sizeFactor = radius;
                    return geometricFactor * elasticMod * sizeFactor;
                case FractureApertureType.BartonBandis:
                    // Use the inverse of the specified fracture normal stiffness
                    return 1 / gbc.MechProps.FractureNormalStiffness;
                default:
                    // Return NaN
                    return double.NaN;
            }
        }

        // Dynamic and geomechanical data
        /// <summary>
        /// Unit vector for the direction of shear stress on the fracture surface
        /// </summary>
        private VectorXYZ shearStressVector;
        /// <summary>
        /// Pitch of the shear stress vector on the surface of the fractures relative to fracture strike (radians, positive downwards; will return NaN if shear stress is zero)
        /// </summary>
        public double ShearStressPitch { get; private set; }
        /// <summary>
        /// Unit vector for the direction of shear stress on the fracture surface
        /// </summary>
        public VectorXYZ ShearStressVector { get { return new VectorXYZ(shearStressVector); } }
        /// <summary>
        /// Predominant sense of fracture displacement
        /// </summary>
        public FractureDisplacementSense DisplacementSense
        {
            get
            {
                if (CurrentFractureData.SigmaNeff_Const_M < 0)
                    return FractureDisplacementSense.Dilatant;
                else if (ShearStressPitch > (0.75 * Math.PI))
                    return FractureDisplacementSense.LeftLateral;
                else if (ShearStressPitch >= (0.25 * Math.PI))
                    return FractureDisplacementSense.Reverse;
                else if (ShearStressPitch > -(0.25 * Math.PI))
                    return FractureDisplacementSense.RightLateral;
                else //if (ShearStressPitch >= (-0.75 * Math.PI))
                    return FractureDisplacementSense.Normal;
            }
        }
        /// <summary>
        /// Driving stress vector at the start of the timestep; this is equivalent to and has a magnitude equal to the scalar quantity U
        /// </summary>
        private VectorXYZ DrivingStressVector;
        /// <summary>
        /// Mean displacement vector at the start of the timestep for a fracture of unit radius
        /// </summary>
        public VectorXYZ DisplacementVector { get { return -(16 / (3 * Math.PI * gbc.MechProps.PlainStrainEffectiveE_r)) * DrivingStressVector; } }
        /// <summary>
        /// Recalculate the shear displacement pitch and vector, the fracture mode and the compliance tensor base for the given effective stress tensor
        /// </summary>
        public void RecalculateElasticResponse(Tensor2S CurrentStress)
        {
            // Get stress vector acting on the fracture plane
            VectorXYZ stressOnFracture = CurrentStress * normalVector;

            // Calculate the magnitude of normal stress on the fracture plane, and the shear stresses acting on the fracture plane in the downdip and along-strike directions respectively
            double normalStressMagnitude = normalVector & stressOnFracture;
            double dipShearStressMagnitude = dipVector & stressOnFracture;
            double strikeShearStressMagnitude = strikeVector & stressOnFracture;

            // Recalculate the shear displacement pitch and vector, and flag if it has changed
            double shearStressMagnitude;
            bool stressVectorChanged = RecalculateStressDisplacementVectors(dipShearStressMagnitude, strikeShearStressMagnitude, out shearStressMagnitude);

            // Check if the fractures are dilatant, and flag if this has changed
            // Fractures are dilatant if the effective normal stress acting on them is tensile (i.e. negative)
            bool previous_sigmaneff_negative = (CurrentFractureData.SigmaNeff_Const_M< 0);
            bool sigmaneff_negative = (normalStressMagnitude < 0);
            bool sigmaneff_changed = (sigmaneff_negative != previous_sigmaneff_negative);

            // Check if the fractures can accommodate elastic strain, and flag if this has changed
            // Fractures can only accumulate displacement, and hence accommodate elastic strain, if the driving stress is positive
            // This will be the case if either the normal stress on the fractures is negative (Mode 1 displacement) or the shear stress exceeds the frictional traction (Mode 2 displacement)
            // NB If the driving stress is zero (within rounding error) we will count it as positive - since it is likely to increase over the current timestep
            bool previous_sigmad_positive = (CurrentFractureData.Mean_SigmaD_M > 0);
            bool sigmad_positive = false;
            if (normalStressMagnitude <= PreviousFractureData.MaxDrivingStressRoundingError)
                sigmad_positive = true;
            else if ((float)shearStressMagnitude - (float)(gbc.MechProps.MuFr * normalStressMagnitude) >= -PreviousFractureData.MaxDrivingStressRoundingError)
                sigmad_positive = true;
            bool sigmad_changed = (sigmad_positive != previous_sigmad_positive);

            // Recalculate the compliance tensor base
            // This is only necessary if either:
            // - the fracture mode has changed (from dilatant to shear or vice versa)
            // - the driving stress has changed from positive to negative, or vice versa, so fractures can now / can no longer accommodate elastic strain
            // - the shear displacement vector has changed, for shear fractures (the compliance tensor is independent of the shear displacement vector for dilatant fractures so this does not apply for these)
            if (sigmaneff_changed || sigmad_changed || (stressVectorChanged && !sigmaneff_negative))
                RecalculateComplianceTensorBase(sigmaneff_negative, sigmad_positive);
        }
        /// <summary>
        /// Recalculate the shear stress and displacement pitch and vectors if they have changed
        /// </summary>
        /// <param name="DipShearStressMagnitude">Magnitude of the shear stress acting on the fracture in the downdip direction</param>
        /// <param name="StrikeShearStressMagnitude">Magnitude of the shear stress acting on the fracture in the strike direction</param>
        /// <param name="ShearStressMagnitude">Reference parameter for the magnitude of the total shear stress acting on the fracture</param>
        /// <returns>True if the shear displacement vector has changed, false if it has not</returns>
        private bool RecalculateStressDisplacementVectors(double DipShearStressMagnitude, double StrikeShearStressMagnitude, out double ShearStressMagnitude)
        {
            // Calculate pitch of shear stress on the fracture
            // NB there will be two possible shear stress pitches in opposite directions, one with a negative shear stress magnitude and one with a positive shear stress magnitude
            // We will use the one with a positive shear stress magnitude
            // If the shear stress magnitude is zero then the shear displacement pitch will be set to NaN
            double newShearStressPitch = Math.Atan2(DipShearStressMagnitude, StrikeShearStressMagnitude);
            ShearStressMagnitude = (DipShearStressMagnitude * VectorXYZ.Sin_trim(newShearStressPitch)) + (StrikeShearStressMagnitude * (VectorXYZ.Cos_trim(newShearStressPitch)));
            if (ShearStressMagnitude == 0)
                newShearStressPitch = double.NaN;

            // If the shear displacement pitch has not changed we do not need to recalculate the shear displacement vector, and can simply return false
            // NB We use Equals to compare rather than == so if both pitches are NaN (e.g. shear stress on the fracture is 0) the overall expression will return true
            if (newShearStressPitch.Equals(ShearStressPitch))
                return false;

            // Update the shear stress pitch and vector
            // The shear stress vector is parallel to fracture, in the direction of the shear stress pitch
            // If the shear stress magnitude is zero, will be set to (0,0,0)
            ShearStressPitch = newShearStressPitch;
            if (double.IsNaN(newShearStressPitch))
                shearStressVector = new VectorXYZ(0, 0, 0);
            else
                shearStressVector = (VectorXYZ.Sin_trim(newShearStressPitch) * dipVector) + (VectorXYZ.Cos_trim(newShearStressPitch) * StrikeVector);

            // The shear displacement vector has changed so return true
            return true;
        }
        /// <summary>
        /// Recalculate the compliance tensor base, based on the current driving stress, fracture orientation, mode and displacement vector
        /// </summary>
        /// <param name="sigmaneff_negative">True if the current effective normal stress on the fracture negative (i.e. the fracture is dilatant), otherwise false</param>
        /// <param name="sigmad_positive">True if the current fracture driving stress is positive, otherwise false</param>
        private void RecalculateComplianceTensorBase(bool sigmaneff_negative, bool sigmad_positive)
        {
            // If the driving stress is positive, the components of the compliance tensor base will be dependent on the current fracture mode, orientation and displacement vector
            if (sigmad_positive)
            {
                if (sigmaneff_negative)
                {
                    // For dilatant fractures, the fracture compliance tensor base is most easily generated using the fourth order outer vector product operator on the normal vector
                    // This returns a fourth order tensor C such that Cijkl=(AiBkDjl+AiBlDjk+AjBkDil+AjBlDik)/4, where D is the Kronecker delta
                    Fracture_ComplianceTensorBase = normalVector | normalVector;
                    Fracture_ComplianceTensorBase.DoubleShearColumnComponents();

                    // Recalculate fracture mode factors
                    double oneMinusNur = 1 - gbc.MechProps.Nu_r;
                    double oneMinus2Nur = 1 - (2 * gbc.MechProps.Nu_r);
                    Mff = Math.Pow(oneMinusNur, 2) / oneMinus2Nur;
                    Mfw = 0;
                    Mww = oneMinusNur / 2;
                    Mfs = 0;
                    Mss = oneMinusNur / 2;
                }
                else
                {
                    // For shear fractures, the fracture compliance tensor base is most easily generated using a combination of outer vector product and outer tensor product operators on the normal and shear displacement vectors
                    double mufr = gbc.MechProps.MuFr;
                    VectorXYZ strikeVector = StrikeVector;
                    Tensor2S normal_OP_mu_normal = normalVector ^ (mufr * normalVector);
                    Tensor2S normal_OP_dip = normalVector ^ dipVector;
                    Tensor2S normal_OP_strike = normalVector ^ strikeVector;
                    Tensor4_2Sx2S normal_OP_dip_OP_normal_OP_dip = normal_OP_dip ^ normal_OP_dip;
                    Tensor4_2Sx2S normal_OP_strike_OP_normal_OP_strike = normal_OP_strike ^ normal_OP_strike;
                    Tensor4_2Sx2S frictional_traction = (normalVector ^ shearStressVector) ^ normal_OP_mu_normal;
                    Fracture_ComplianceTensorBase = normal_OP_dip_OP_normal_OP_dip + normal_OP_strike_OP_normal_OP_strike - frictional_traction;
                    Fracture_ComplianceTensorBase.DoubleShearColumnComponents();

                    // Recalculate fracture mode factors
                    double sinpitch, cospitch;
                    if (double.IsNaN(ShearStressPitch))
                    {
                        sinpitch = 0;
                        cospitch = 0;
                    }
                    else
                    {
                        sinpitch = VectorXYZ.Sin_trim(ShearStressPitch);
                        cospitch = VectorXYZ.Cos_trim(ShearStressPitch);
                    }
                    double oneMinusNur = 1 - gbc.MechProps.Nu_r;
                    double oneMinus2Nur = 1 - (2 * gbc.MechProps.Nu_r);
                    Mff = 0;
                    Mfw = -(Math.Pow(oneMinusNur, 2) / (2 * oneMinus2Nur)) * mufr * sinpitch;
                    Mww = oneMinusNur / 2;
                    Mfs = -(Math.Pow(oneMinusNur, 2) / (2 * oneMinus2Nur)) * mufr * cospitch;
                    Mss = oneMinusNur / 2;
                }
            }
            // If the driving stress is negative, no displacement can occur on the fractures so the compliance tensor and mode factors will be zero
            else
            {
                // In this case the compliance tensor bases will contain only zero values
                Fracture_ComplianceTensorBase = new Tensor4_2Sx2S();

                // Set all mode factors to zero
                Mff = 0;
                Mfw = 0;
                Mww = 0;
                Mfs = 0;
                Mss = 0;
            }
        }
        /// <summary>
        /// Base for the fracture compliance tensor, constructed from a combination of the fracture normal vector and the shear displacement vector
        /// </summary>
        private Tensor4_2Sx2S Fracture_ComplianceTensorBase;
        /// <summary>
        /// Compliance tensor for this fracture set
        /// </summary>
        public Tensor4_2Sx2S S_set
        {
            get
            {
                double elasticityMultiplier = (1 - Math.Pow(gbc.MechProps.Nu_r, 2)) / gbc.MechProps.E_r;
                double fractureDensityMultiplier = (4 / Math.PI) * Fractures.FP33_total;
                return elasticityMultiplier * fractureDensityMultiplier * Fracture_ComplianceTensorBase;
            }
        }

        // We may want to use the present day stress to calculate fracture aperture and reactivation risk, rather than the stress at the time of fracture development
        // In this case we will use a supplied effective stress tensor to calculate the present day stress on the fracture
        /// <summary>
        /// Present day effective normal stress acting on the fracture
        /// </summary>
        private double PresentDaySigmaNeff { get; set; }
        /// <summary>
        /// Present day shear stress acting on the fracture
        /// </summary>
        private double PresentDayTau { get; set; }
        /// <summary>
        /// Present day fracture reactivition potential
        /// This will return the driving stress if that is positive; however it will return a negative value representing the cohesionless distance to failure if the fracture is closed and not critically stressed
        /// </summary>
        public double PresentDayReactivationPotential { get { return Math.Max(PresentDayDilatancyPotential, PresentDaySlipPotential); } }
        /// <summary>
        /// Present day fracture dilatancy potential
        /// This will return the inverse of the normal effective stress on the fracture, representing the dilatant driving stress if positive, and the cohesionless distance to dilational failure if it is negative
        /// </summary>
        public double PresentDayDilatancyPotential
        {
            get
            {
                // If the fractures are dilatant (i.e. the normal stress on them is tensile), the dilatancy protential is positive and equal to the total stress (normal and shear) on the fracture
                if (PresentDaySigmaNeff <= 0)
                    return Math.Sqrt(Math.Pow(PresentDaySigmaNeff, 2) + Math.Pow(PresentDayTau, 2));
                // If the fractures are closed, the dilatancy potential will be the inverse of the (compressive) normal effective stress on the fracture
                else
                    return -PresentDaySigmaNeff;
            }
        }
        /// <summary>
        /// Present day fracture slip potential
        /// This represents the shear driving stress if that is positive, and the cohesionless distance to shear failure if it is negative
        /// </summary>
        public double PresentDaySlipPotential
        {
            get
            {
                // If the fractures are dilatant (i.e. the normal stress on them is tensile), we do not need to take into account friction
                // Therefore the slip potential is equal to the maximum shear stress
                if (PresentDaySigmaNeff <= 0)
                    return PresentDayTau;
                // If the fractures are closed, the shear driving stress and slip potential will equal the shear stress on the fractures minus the frictional traction
                return PresentDayTau - (gbc.MechProps.MuFr * PresentDaySigmaNeff);
            }
        }
        /// <summary>
        /// Present day fracture slip tendency
        /// This represents the minimum frictional coefficient required to prevent slip on a cohesionless fracture, i.e. shear stress / normal stress
        /// If the fractures are dilatant (i.e. the normal stress on them is tensile), the slip tendency is undefined and will return NaN 
        /// </summary>
        public double PresentDaySlipTendency
        {
            get
            {
                if (PresentDaySigmaNeff > 0)
                    return PresentDayTau / PresentDaySigmaNeff;
                else
                    return double.NaN;
            }
        }
        /// <summary>
        /// This represents the diplacement sense closest to failure, or with the highest driving stress if the fracture is critical
        /// </summary>
        public FractureDisplacementSense MostLikelyReactivationSense { get; private set; }
        /// <summary>
        /// Present day driving stress acting on the fracture
        /// </summary>
        public double PresentDayDrivingStress { get { return (PresentDayReactivationPotential > 0 ? PresentDayReactivationPotential : 0); } }
        /// <summary>
        /// Flag specifying whether to use present day stress or stress at the time of fracture development to calculate fracture aperture and reactivation risk
        /// </summary>
        private bool usePresentDayStress;
        /// <summary>
        /// Call this function to use the present day stress to calculate fracture aperture and reactivation risk, rather than the stress at the time of fracture development  
        /// </summary>
        /// <param name="PresentDayEffectiveStress">Tensor2S object to specify the present day effective stress; if null, the stress at the time of fracture development will be used to calculate fracture aperture and reactivation risk</param>
        public void UsePresentDayStress(Tensor2S PresentDayEffectiveStress)
        {
            if (PresentDayEffectiveStress is null) // Set Present Day normal and driving stress to NaN
            {
                usePresentDayStress = false;
                PresentDaySigmaNeff = double.NaN;
                PresentDayTau = double.NaN;
            }
            else // Set Present Day normal and driving stress based on the supplied effective stress tensor
            {
                usePresentDayStress = true;

                // Get stress vector acting on the fracture plane
                VectorXYZ stressOnFracture = PresentDayEffectiveStress * normalVector;

                // Calculate the magnitude of normal stress on the fracture plane, and the shear stresses acting on the fracture plane in the along-strike and downdip directions respectively
                // The maximum present day shear stress Tau can be calculated by taking the root of the squares of the orthogonal strike and downdip shear stress components
                PresentDaySigmaNeff = normalVector & stressOnFracture;
                double presentDayTauDip = dipVector & stressOnFracture;
                double presentDayTauStrike = strikeVector & stressOnFracture;
                PresentDayTau = Math.Sqrt(Math.Pow(presentDayTauDip, 2) + Math.Pow(presentDayTauStrike, 2));

                // Calculate the most likely reactivation displacement sense
                double presentDayShearStressPitch = Math.Atan2(presentDayTauDip, presentDayTauStrike);
                if (PresentDayDilatancyPotential > PresentDaySlipPotential)
                    MostLikelyReactivationSense = FractureDisplacementSense.Dilatant;
                else if (presentDayShearStressPitch > (0.75 * Math.PI))
                    MostLikelyReactivationSense = FractureDisplacementSense.LeftLateral;
                else if (presentDayShearStressPitch >= (0.25 * Math.PI))
                    MostLikelyReactivationSense = FractureDisplacementSense.Reverse;
                else if (presentDayShearStressPitch > -(0.25 * Math.PI))
                    MostLikelyReactivationSense = FractureDisplacementSense.RightLateral;
                else //if (presentDayShearStressPitch >= (-0.75 * Math.PI))
                    MostLikelyReactivationSense = FractureDisplacementSense.Normal;
            }
        }

        // Applied strain components
        /// <summary>
        /// Ratio of incremental normal strain to total incremental normal strain on the fracture, given by eff^2 / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double eff2d_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental normal strain x downdip shear strain to total incremental normal strain on the fracture, given by eff*efw / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efffwd_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental downdip shear strain to total incremental normal strain on the fracture, given by efw^2 / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efw2d_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental normal strain x alongstrike shear strain to total incremental normal strain on the fracture, given by eff*efs / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efffsd_e2d { get; set; }
        /// <summary>
        /// Ratio of incremental alongstrike shear strain to total incremental normal strain on the fracture, given by efs^2 / (eff^2 + efw^2 + efs^2)
        /// </summary>
        private double efs2d_e2d { get { return 1 - eff2d_e2d - efw2d_e2d; } }
        /// <summary>
        /// Recalculate the applied strain components acting on the fractures, for a specified strain or strain rate tensor
        /// </summary>
        /// <param name="AppliedStrainTensor">Current strain or strain rate tensor</param>
        public void RecalculateStrainRatios(Tensor2S AppliedStrainTensor)
        {
            VectorXYZ normalStrainOnFracture = AppliedStrainTensor * normalVector;
            VectorXYZ downDipStrainOnFracture = AppliedStrainTensor * dipVector;
            VectorXYZ alongStrikeStrainOnFracture = AppliedStrainTensor * strikeVector;
            double effd = normalVector & normalStrainOnFracture;
            double efwd = dipVector & normalStrainOnFracture;
            double ewwd = dipVector & downDipStrainOnFracture;
            double efsd = strikeVector & normalStrainOnFracture;
            double essd = strikeVector & alongStrikeStrainOnFracture;

            // Set the strain ratios to zero if they are small - this will avoid rounding errors
            double emax = effd + efwd + ewwd + efsd + essd;
            if ((float)(emax + effd) == (float)emax)
                effd = 0;
            if ((float)(emax + efwd) == (float)emax)
                efwd = 0;
            if ((float)(emax + ewwd) == (float)emax)
                ewwd = 0;
            if ((float)(emax + efsd) == (float)emax)
                efsd = 0;
            if ((float)(emax + essd) == (float)emax)
                essd = 0;
            double eff_squared = Math.Pow(effd, 2);
            double efw_squared = Math.Pow(efwd, 2);
            double efs_squared = Math.Pow(efsd, 2);
            double e_squared = eff_squared + efw_squared + efs_squared;
            eff2d_e2d = (e_squared > 0 ? eff_squared / e_squared : 1);
            efw2d_e2d = (e_squared > 0 ? efw_squared / e_squared : 0);
            efffwd_e2d = (e_squared > 0 ? (effd * efwd) / e_squared : 0);
            efffsd_e2d = (e_squared > 0 ? (effd * efsd) / e_squared : 0);
        }

        // Fracture mode factors - these form the basis for the stress shadow width
        // They represent the ratio of far-field displacement (i.e. applied strain) to displacement on a fracture, normalised to remove the effects of fracture size and geometry
        // For convenience, these are combined with the respective strain components when they are calculated, so they need only be multiplied by geometric factors to determine stress shadow widths
        /// <summary>
        /// Fracture Mode Factor: azimuthal strain => azimuthal displacement
        /// </summary>
        private double Maa_eaa2d_eh2d { get { return Math.Max(((eff2d_e2d * Mff) + (efffwd_e2d * Mfw) + (efw2d_e2d * Mww)), 0); } }
        /// <summary>
        /// Fracture Mode Factor: strike-parallel shear strain => azimuthal displacement
        /// </summary>
        private double Mas_eaaasd_eh2d { get { return Math.Max((efffsd_e2d * Mfs), 0); } }
        /// <summary>
        /// Fracture Mode Factor: strike-parallel shear strain => strike-slip displacement
        /// </summary>
        private double Mss_eas2d_eh2d { get { return Math.Max((efs2d_e2d * Mss), 0); } }
        /// <summary>
        /// Fracture Mode Factor: maximum horizontal strain => horizontal displacement
        /// </summary>
        private double Mhh_eh2d { get { return Maa_eaa2d_eh2d + Mas_eaaasd_eh2d + Mss_eas2d_eh2d; } }
        /// <summary>
        /// Geometric factor: normal strain => normal displacement
        /// </summary>
        private double Mff { get; set; }
        /// <summary>
        /// Geometric factor: normal strain => down-dip displacement
        /// </summary>
        private double Mfw { get; set; }
        /// <summary>
        /// Geometric factor: normal strain => along-strike displacement
        /// </summary>
        private double Mfs { get; set; }
        /// <summary>
        /// Geometric factor: down-dip strain => down-dip displacement
        /// </summary>
        private double Mww { get; set; }
        /// <summary>
        /// Geometric factor: along-strike strain => along-strike displacement
        /// </summary>
        private double Mss { get; set; }

        // Stress shadow width
        /// <summary>
        /// Ratio of maximum stress shadow width to fracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Max_F_StressShadowWidth_r
        {
            get { return Mhh_eh2d * (8 / Math.PI); }
        }
        /// <summary>
        /// Ratio of mean stress shadow width to fracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_F_StressShadowWidth_r
        {
            get { return Mhh_eh2d * (16 / (3 * Math.PI)); }
        }
        /*/// <summary>
        /// Azimuthal component of ratio of mean fracture stress shadow width to fracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_Azimuthal_F_StressShadowWidth_r
        {
            get { return Maa_eaa2d_eh2d * (16 / (3 * Math.PI)); }
        }
        /// <summary>
        /// Strike-slip shear component of ratio of mean fracture stress shadow width to fracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_Shear_F_StressShadowWidth_r
        {
            get { return (Mas_eaaasd_eh2d + Mss_eas2d_eh2d) * (16 / (3 * Math.PI)); }
        }*/

        // Functions to calculate fracture population data
        /// <summary>
        /// Create a new FractureCalculationData object for the current timestep, populate it with data from the end of the previous timestep, and add it to the list of previous timestep data
        /// </summary>
        public void setTimestepData()
        {
            // Create a new FractureCalculationData object for the current timestep
            CurrentFractureData = CurrentFractureData.GetNextTimestepData();

            // Now we can add the CurrentFractureData object to the list of previous FractureCalculationData objects in the PreviousFractureData object
            PreviousFractureData.AddTimestep(CurrentFractureData, true);

            // If the flag is set to deactivate the fracture set at the start of the next timestep, do this now
            if (DeactivateNextTimestep)
                CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
        }


        // Reset and data input functions
        /// <summary>
        /// Reset all fracture data to initial values (no fractures, no previous deformation)
        /// </summary>
        public void resetFractureData(ushort raysPerFracture_in, double rmin_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in)
        {
            // Set the initial fracture distribution data
            InitialDistribution = uFDistributionIn;
            CapB = B_in;
            c_coefficient = c_in;

            // Set the implicit fracture population data 
            // Number of rays comprising each fracture
            RaysPerFracture = raysPerFracture_in;
            // Minimum radius for a fracture; this will be the length of the rays at nucleation
            MinimumFractureRadius = rmin_in;
            // Create new UnconfinedFractureData object
            Fractures = new UnconfinedFractureData(InitialP30(), rmin_in, raysPerFracture_in);

            // Create new fracture calculation data object, and initialise for timestep 0 (i.e. initial data before the model runs)
            CurrentFractureData = new FractureCalculationData_Minimised();
            // Create an new list for previous fracture calculation data objects and use the CurrentFractureData object for timestep 0 
            PreviousFractureData = new FCD_List_Minimised(CurrentFractureData, true);
            // Set the flag to deactivate the fracture set at the start of the next timestep to false
            DeactivateNextTimestep = false;
        }

        // Constructors
        /// <summary>
        /// Default constructor: set default values 
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in)
                : this(gbc_in, 0, Math.PI/2, 8, 0.1, InitialFractureDistribution.PowerLaw, 0.001, 3d)
        {
            // Defaults:

            // Fracture orientation: set to vertical N-striking fractures
            // Number of rays per fracture: set to 8
            // Minimum fracture radius: set to 0.1
            // Initial microfracture distribution - set to power law, B=0.001, c=3
        }
        /// <summary>
        /// Constructor: input fracture strike and dip, number of rays per fracture, minimum fracture radius, and initial microfracture distribution parameters
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        /// <param name="raysPerFracture_in">Number of rays comprising each fracture</param>
        /// <param name="rmin_in">Minimum radius for a fracture; this will be the length of the rays at nucleation</param>
        /// <param name="uFDistributionIn">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in, double Strike_in, double Dip_in, int raysPerFracture_in, double rmin_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in)
            : this (gbc_in, Strike_in, Dip_in, raysPerFracture_in, rmin_in, uFDistributionIn, B_in, c_in, 0.0005, 1E-5)
        {
            // Defaults for fracture aperture control data for uniform and size-dependent aperture:

            // Fixed aperture for fractures in the uniform aperture case: 0.5mm
            // Multiplier for fracture aperture in the size-dependent aperture case: 1E-5 (gives 1mm aperture for 100m high fracture) 
        }
        /// <summary>
        /// Constructor: input fracture strike and dip, number of rays per fracture, minimum fracture radius, initial microfracture distribution parameters, and fracture aperture control data for uniform and size-dependent aperture
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        /// <param name="raysPerFracture_in">Number of rays comprising each fracture</param>
        /// <param name="rmin_in">Minimum radius for a fracture; this will be the length of the rays at nucleation</param>
        /// <param name="uFDistributionIn">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="UniformAperture_in">Fixed aperture for fractures in the uniform aperture case (m)</param>
        /// <param name="SizeDependentApertureMultiplier_in">Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in, double Strike_in, double Dip_in, int raysPerFracture_in, double rmin_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in, double UniformAperture_in, double SizeDependentApertureMultiplier_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set fracture orientation
            Strike = Strike_in;
            Dip = Dip_in;

            // Set the initial shear stress pitch to NaN and the initial shear stress vector to (0,0,0)
            // This represents no shear stress on the fracture
            //ShearStressPitch = double.NaN;
            //shearStressVector = new VectorXYZ(0, 0, 0);

            // Set the initial shear displacement pitch to NaN and the initial shear displacement vector to (0,0,0)
            // This represents no shear displacement
            //DisplacementPitch = double.NaN;
            //shearDisplacementVector = new VectorXYZ(0, 0, 0);

            // Set the initial driving stress vectors to (0,0,0)
            //DrivingStressVector = new VectorXYZ(0, 0, 0);
            //MFDisplacementAdjustedDrivingStressVector = new VectorXYZ(0, 0, 0);

            // Calculate the initial compliance tensor base; NB we assume initial driving stress is zero
            RecalculateComplianceTensorBase(false);

            // Reset implicit fracture population data
            resetFractureData((ushort) raysPerFracture_in, rmin_in, uFDistributionIn, B_in, c_in);

            // Set fracture aperture control data for uniform and size-dependent aperture
            // Fixed aperture for fractures in the uniform aperture case (m)
            UniformAperture = UniformAperture_in;
            // Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier
            SizeDependentApertureMultiplier = SizeDependentApertureMultiplier_in;
        }

    }
}
