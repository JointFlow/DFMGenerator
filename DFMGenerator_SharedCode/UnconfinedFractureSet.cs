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
        public double getMeanMicrofractureAperture(double radius)
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
            double delta_a = 0;
            if (true)//(Mode == FractureMode.Mode1)
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
        public double getMicrofractureCompressibility(double radius)
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
            //RecalculateComplianceTensorBase(false);

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
