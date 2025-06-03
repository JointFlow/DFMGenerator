using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

        /*// Local copies centrepoints of corner pillars in fracture set (IJK) coordinates
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
        }*/

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
        /// Array of vectors representing the fracture rays
        /// </summary>
        private VectorXYZ[] rayVectors;
        /// <summary>
        /// Strike of fracture set (radians); positive in the IPlus direction
        /// </summary>
        public double Strike { get { return strike; } private set { while (value >= (2 * Math.PI)) value -= (2 * Math.PI); while (value < 0) value += (2 * Math.PI); strike = value; strikeVector = VectorXYZ.GetLineVector(value, 0); azimuthVector = VectorXYZ.GetLineVector(value + (Math.PI / 2), 0); } }
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
        /// Array of vectors representing the fracture rays
        /// </summary>
        public VectorXYZ[] RayVectors { get { VectorXYZ[] output = (VectorXYZ[])rayVectors.Clone(); for (int rayNo = 0; rayNo < output.Length; rayNo++) output[rayNo] = new VectorXYZ(rayVectors[rayNo]); return output; } }
        /// <summary>
        /// StressDistribution object describing the spatial distribution of the fractures: EvenlyDistributedStress gives a random fracture distribution, StressShadow and DuctileBoundary gives a regular spacing
        /// </summary>
        public StressDistribution FractureDistribution { get; set; }
        /// <summary>
        /// Array for the orientation multipliers for the weighted mean linear density of fractures from other sets seen by rays from this set
        /// </summary>
        private double[,] orientationMultipliers;
        /*/// <summary>
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
        }*/

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
        private ushort RaysPerFracture { get; set; }
        /// <summary>
        /// Minimum radius for a fracture; this will be the length of the rays at nucleation
        /// </summary>
        private double MinimumFractureRadius { get; set; }
        /// <summary>
        /// Maximum allowed radius for a ray; rays will stop propagating when they reach this length
        /// </summary>
        private double MaximumFractureRadius { get; set; }
        /// <summary>
        /// Effective radius of the largest current fracture; if there are no datapoints, return zero
        /// NB this assumes that the fracture population datapoint arrays have already been sorted from largest to smallest
        /// </summary>
        private double MaximumCurrentFractureRadius
        {
            get
            {
                double output = 0;
                double maxFullyActiveRadius = (Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive].Count > 0) ? Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][0].EffectiveRayLength : 0;
                if (output < maxFullyActiveRadius)
                    output = maxFullyActiveRadius;
                double maxRestrictedRadius = (Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted].Count > 0) ? Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][0].EffectiveRayLength : 0;
                if (output < maxRestrictedRadius)
                    output = maxRestrictedRadius;
                double maxStaticMaxLengthRadius = (Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticMaxRadius].Count > 0) ? Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticMaxRadius][0].EffectiveRayLength : 0;
                if (output < maxStaticMaxLengthRadius)
                    output = maxStaticMaxLengthRadius;
                return output;
            }
        }
        /// <summary>
        /// Object containing cumulative population data for all fractures
        /// </summary>
        private UnconfinedFractureData Fractures { get; set; }
        /// <summary>
        /// Variable to hold maximum historic active mean linear fracture density; used to check if termination criteria are met, and updated when the CheckFractureDeactivation is called
        /// </summary>
        private double max_historic_a_RP32;
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration at the last time new fractures nucleated
        /// </summary>
        private double previous_CumGamma;
        /// <summary>
        /// Minimum RP30 value for a nucleating fracture datapoint - a new datapoint will not be created until the volumetric density of the nucleating fractures reaches this value
        /// </summary>
        private double min_datapoint_MFP30;

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

        // Functions to return data for the current timestep from the CurrentFractureData object
        /// <summary>
        /// Flag for the current stage of evolution of the fracture dip set
        /// </summary>
        /// <returns></returns>
        public FractureEvolutionStage getEvolutionStage() { return CurrentFractureData.EvolutionStage; }
        /// <summary>
        /// Effective normal stress on the fracture at the end of the current timestep (Pa)
        /// </summary>
        /// <returns></returns>
        public double getFinalEffectiveNormalStress() { return CurrentFractureData.SigmaNeff_Final_M; }
        /// <summary>
        /// Driving stress at the start of the current timestep U (Pa) 
        /// </summary>
        /// <returns></returns>
        public double getConstantDrivingStressU() { return CurrentFractureData.U_M; }
        /// <summary>
        /// Rate of increase of driving stress during the current timestep V (Pa/s)
        /// </summary>
        /// <returns></returns>
        public double getVariableDrivingStressV() { return CurrentFractureData.V_M; }
        /// <summary>
        /// Weighted mean driving stress during the current timestep (Pa)
        /// </summary>
        /// <returns></returns>
        public double getMeanDrivingStressSigmaD() { return CurrentFractureData.Mean_SigmaD_M; }
        /// <summary>
        /// Driving stress at the end of the current timestep (Pa)
        /// </summary>
        /// <returns></returns>
        public double getFinalDrivingStressSigmaD() { return CurrentFractureData.Final_SigmaD_M; }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getInverseStressShadowVolume() { return CurrentFractureData.theta_M; }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getClearZoneVolume() { return CurrentFractureData.theta_dashed_M; }

        // Functions to get logging data - only required in debug mode
#if DEBUG
        /// <summary>
        /// Get the current number of ray datapoints of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns></returns>
        public int getNoDatapoints(RayPropagationStatus rayType)
        {
            return Fractures.fracturePopulationDatapoints[rayType].Count;
        }
        /// <summary>
        /// Get a list of the ray lengths for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of ray lengths</returns>
        public List<double> getRayLengths(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.RayLength);
            return output;
        }
        /// <summary>
        /// Get a list of the effective ray lengths for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of effective ray lengths</returns>
        public List<double> getEffectiveRayLengths(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.EffectiveRayLength);
            return output;
        }
        /// <summary>
        /// Get a list of the propagation controlling ray lengths for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of propagation controlling ray lengths</returns>
        public List<double> getPropagationControllingLengths(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.PropagationControllingLength);
            return output;
        }
        /// <summary>
        /// Get a list of the dP30 values for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of the dP30 values</returns>
        public List<double> getdP30Values(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.dP30);
            return output;
        }
        /// <summary>
        /// Get a list of the dP32 factors for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of the dP32 factors; these values must be multiplied by pi/No rays per fracture to get the true P32</returns>
        public List<double> getdP32Factors(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.dP32factor);
            return output;
        }
        /// <summary>
        /// Get a list of the dP33 factors for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of the dP33 factors; these values must be multiplied by 4/3 pi/No rays per fracture to get the true P32</returns>
        public List<double> getdP33Factors(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.dP33factor);
            return output;
        }
        /// <summary>
        /// Get a list of the Phi values for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of the Phi values</returns>
        public List<double> getPhiValues(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.CumulativePhi);
            return output;
        }
        /// <summary>
        /// Get a list of the PhiII values for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of the PhiII values</returns>
        public List<double> getPhiIIValues(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.CumulativePhiII);
            return output;
        }
        /// <summary>
        /// Get a list of the PhiIJ values for each datapoint of the ray type specified
        /// </summary>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of the PhiIJ values</returns>
        public List<double> getPhiIJValues(RayPropagationStatus rayType)
        {
            List<double> output = new List<double>();
            foreach (ImplicitFracturePopulationDatapoint dp in Fractures.fracturePopulationDatapoints[rayType])
                output.Add(dp.CumulativePhiIJ);
            return output;
        }
#endif


        // Functions to get total population data for all fractures in the dipset
        /// <summary>
        /// Total volumetric density of fully active fracture segments
        /// </summary>
        /// <returns></returns>
        public double a_RP30_total() { return Fractures.a_RP30_total; }
        /// <summary>
        /// Total volumetric density of restricted fracture segments
        /// </summary>
        /// <returns></returns>
        public double r_RP30_total() { return Fractures.r_RP30_total; }
        /// <summary>
        /// Total volumetric density of fracture segments deactivated due to stress shadow interaction
        /// </summary>
        /// <returns></returns>
        public double sII_RP30_total() { return Fractures.sII_RP30_total; }
        /// <summary>
        /// Total volumetric density of fracture segments deactivated due to intersection
        /// </summary>
        /// <returns></returns>
        public double sIJ_RP30_total() { return Fractures.sIJ_RP30_total; }
        /// <summary>
        /// Total mean linear density of fully active fracture segments
        /// </summary>
        /// <returns></returns>
        public double a_RP32_total() { return Fractures.a_RP32_total; }
        /// <summary>
        /// Total mean linear density of restricted fracture segments
        /// </summary>
        /// <returns></returns>
        public double r_RP32_total() { return Fractures.r_RP32_total; }
        /// <summary>
        /// Total mean linear density of fracture segments deactivated due to stress shadow interaction
        /// </summary>
        /// <returns></returns>
        public double sII_RP32_total() { return Fractures.sII_RP32_total; }
        /// <summary>
        /// Total mean linear density of fracture segments deactivated due to intersection
        /// </summary>
        /// <returns></returns>
        public double sIJ_RP32_total() { return Fractures.sIJ_RP32_total; }

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
            bool previous_sigmaneff_negative = (CurrentFractureData.SigmaNeff_Const_M < 0);
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
                double fractureDensityMultiplier = (4 / Math.PI) * Fractures.FP33_overlapping_total;
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
        /// <summary>
        /// Check if the fracture set meets the specified deactivation criteria, and if so set the fracture evolution stage to Deactivated
        /// </summary>
        /// <param name="historic_a_RP32_termination_ratio">Ratio of current to maximum active man linear fracture density at which fracture sets are considered inactive; calculation will terminate when fracture set falls below this ratio</param>
        /// <param name="active_total_RP30_termination_ratio">Ratio of active to total fracture volumetric density at which fracture sets are considered inactive; calculation will terminate when fracture set falls below this ratio</param>
        /// <param name="minimum_ClearZone_Volume">Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated</param>
        /// <returns>True if the fracture set meets any of the deactivation criteria</returns>
        public bool CheckFractureDeactivation(double historic_a_RP32_termination_ratio, double active_total_RP30_termination_ratio, double minimum_ClearZone_Volume)
        {
            // If the fracture set is already deactivated, we do not need to check it again
            if (CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated)
                return true;

            // Flag to deactivate fracture set; initially set to false
            bool deactivateFractureSet = false;

            // Calculate the ratio of current to maximum active fracture volumetric ratio for this fracture set, and if it is below the specified minimum set the fracture deactivation flag to true
            // We only need to do this if the specified minimum is greater than zero; otherwise the check is not performed
            if (historic_a_RP32_termination_ratio > 0)
            {
                // If the active fracture volumetric ratio for this fracture set is increasing, update the maximum historic active fracture volumetric ratio
                double current_tot_a_RP32 = Fractures.a_RP32_total + Fractures.r_RP32_total;
                if (max_historic_a_RP32 < current_tot_a_RP32)
                    max_historic_a_RP32 = current_tot_a_RP32;

                double historic_a_RP32_ratio = (max_historic_a_RP32 > 0 ? current_tot_a_RP32 / max_historic_a_RP32 : 1);
                if (historic_a_RP32_ratio <= historic_a_RP32_termination_ratio)
                    deactivateFractureSet = true;
            }

            // Calculate the active to total fracture volumetric density for this fracture set, and if it is below the specified minimum set the fracture deactivation flag to true
            // We only need to do this if the specified minimum is greater than zero; otherwise the check is not performed
            if (active_total_RP30_termination_ratio > 0)
            {
                double activeRays = Fractures.a_RP30_total + Fractures.r_RP30_total;
                double totalRays = activeRays + Fractures.sII_RP30_total + Fractures.sIJ_RP30_total;
                double a_RP30_ratio = activeRays / totalRays;
                if (a_RP30_ratio <= active_total_RP30_termination_ratio)
                    deactivateFractureSet = true;
            }

            // If the clear zone volume for nucleating fractures has dropped below the minimum specified, set the fracture deactivation flag to true
            if (CurrentFractureData.theta_dashed_M < minimum_ClearZone_Volume)
            {
                deactivateFractureSet = true;
            }

            // Check if the extrapolated initial microfracture radius of current nucleating fractures is less than zero (will only apply if b<2); if so, set the fracture deactivation flag to true
            if (Check_Initial_uF_Radius(MinimumFractureRadius))
            {
                deactivateFractureSet = true;
            }

            // If the fracture deactivation flag is set to true, deactivate the fracture set
            if (deactivateFractureSet)
                deactivateFractures();

            // Return the fracture deactivation flag
            return deactivateFractureSet;
        }
        /// <summary>
        /// Calculate the constant and variable components of the driving stress (U and V) for the upcoming timestep, and estimate the optimal timestep duration based on a specified maximum increase in fracture radius
        /// NB this function should be run before the CurrentFractureData object is updated to the new timestep, so it still contains dynamic data from the previous timestep 
        /// </summary>
        /// <param name="Sigma_Const">Tensor for initial in situ effective stress (Pa)</param>
        /// <param name="Sigma_Var">Tensor for rate of change of effective stress (Pa/s)</param>
        /// <param name="d_rmax">Maximum allowed increase in fracture radius for the largest fractures</param>
        /// <param name="d_P33max">Maximum allowed increase in dP33 for the largest fractures</param>
        /// <returns>Maximum allowable timestep duration (s)</returns>
        public double getOptimalDuration(Tensor2S Sigma_Const, Tensor2S Sigma_Var, double d_rmax, double d_P33max)
        {
            // If it is not possible to calculate a value for the optimal timestep duration, return infinity
            // This will always be greater than any actual calculated optimal duration
            double optdur = double.PositiveInfinity;

            // Set the ratio for comparing initial and rate of change of stress values; if the initial value is less than the rate of change times the comparison ratio, we can round the initial value down to zero
            const double stress_comparator = 0.01;

            // Get the magnitudes of the initial values and the rate of change of the normal and shear stresses acting on the fractures
            // NB sneff, taustrike and taudip represent three orthogonal components of the stress acting on the fault: normal, shear in the direction of strike, and shear in the downdip direction
            // These can be calculated by taking the dot product of the (initial or rate of change of) stress vector on the fracture and the normal, strike or downdip vector of the fracture
            VectorXYZ SigmaF_Const = Sigma_Const * normalVector;
            VectorXYZ SigmaF_Var = Sigma_Var * normalVector;
            double sneff_cst = normalVector & SigmaF_Const;
            double sneff_var = normalVector & SigmaF_Var;
            // If sneff_cst << sneff_var or sneff_cst is less than the maximum driving stress error then we can assume this is rounding error and set snd_cst to 0; otherwise we will get stuck in a loop
            if ((Math.Abs(sneff_cst) < Math.Abs(sneff_var * stress_comparator)) || (Math.Abs(sneff_cst) <= PreviousFractureData.MaxDrivingStressRoundingError))
                sneff_cst = 0;
            double taudip_cst = dipVector & SigmaF_Const;
            double taudip_var = dipVector & SigmaF_Var;
            // If taudip_cst << taudip_var or taudip_cst is less than the maximum driving stress error then we can assume this is rounding error and set taudip_cst to 0
            if ((Math.Abs(taudip_cst) < (taudip_var * stress_comparator)) || (Math.Abs(taudip_cst) <= PreviousFractureData.MaxDrivingStressRoundingError))
                taudip_cst = 0;
            double taustrike_cst = strikeVector & SigmaF_Const;
            double taustrike_var = strikeVector & SigmaF_Var;
            // If taustrike_cst << taustrike_var or taustrike_cst is less than the maximum driving stress error then we can assume this is rounding error and set taustrike_cst to 0
            if ((Math.Abs(taustrike_cst) < (taustrike_var * stress_comparator)) || (Math.Abs(taustrike_cst) <= PreviousFractureData.MaxDrivingStressRoundingError))
                taustrike_cst = 0;

            // Check whether the normal stress on the fractures is tensile (i.e. the fractures are dilatant) or compressive (the fractures are closed)
            // NB If the sneff_cst is zero but sneff_var is negative, the normal stress on the fractures will be tensile during most of the timestep so we flag the fractures as dilatant
            bool dilatant = (sneff_cst < 0) || ((sneff_cst == 0) && (sneff_var < 0));

            // Calculate initial estimates for U and V
            // U is the initial driving stress at the start of the timestep
            // V is the rate of change of driving stress (in SI units, Pa/s) at the start of the timestep
            // NB V may not be constant, as the shear displacement vector may change through time as the in situ stress changes
            double U = 0;
            double V = 0;
            // If the fractures are dilatant (i.e. the initial normal stress on them is tensile), we do not need to take into account friction
            if (dilatant)
            {
                // For vertical dilatant (Mode 1) fractures, the driving stress will equal the tensile normal stress on the fractures
                // For inclined dilatant fractures, the driving stress will equal the root of the square of the normal and shear stress components acting on the fractures
                U = Math.Sqrt(Math.Pow(sneff_cst, 2) + Math.Pow(taudip_cst, 2) + Math.Pow(taustrike_cst, 2));
                // If U is zero, V can also be calculated by taking the square of the normal and shear stress components acting on the fractures
                // If U is not zero, we will have to calculate V by differentiating the expression for driving stress in terms of its three components sneff, taudip and taustrike
                // NB the rate of change of driving stress may not be constant, since the shear displacement vector may change through time as the in situ stress changes
                // Here we set V to the rate of change of driving stress at the start of the timestep
                if ((float)U == 0f)
                    V = Math.Sqrt(Math.Pow(sneff_var, 2) + Math.Pow(taudip_var, 2) + Math.Pow(taustrike_var, 2));
                else
                    V = ((sneff_cst * sneff_var) + (taudip_cst * taudip_var) + (taustrike_cst * taustrike_var)) / U;
            }
            // If initial normal stress on fracture is compressive, we must take into account friction
            else
            {
                // For inclined closed (Mode 2) fractures, the driving stress will equal the shear stress on the fractures minus the frictional traction
                // We must therefore start by calculating the magnitude of the shear stress in the direction of shear displacement (i.e. the maximum shear stress, tau)
                // The initial shear stress (tau_cst) can be calculated by taking the root of the squares of the orthogonal strike and downdip shear stress components
                double tau_cst = Math.Sqrt(Math.Pow(taudip_cst, 2) + Math.Pow(taustrike_cst, 2));
                // If tau_cst is zero, the variable component of the maximum shear stress (tau_var) can also be calculated by taking the root of the squares of the orthogonal strike and downdip shear stress components
                // If tau_cst is not zero, we will have to calculate tau_var by differentiating the expression for maximum shear stress in terms of its two components taudip and taustrike
                // NB the rate of change of shear stress may not be constant, since the shear displacement vector may change through time as the in situ stress changes
                // Here we set tau_var to the rate of change of shear stress at the start of the timestep
                double tau_var = Math.Sqrt(Math.Pow(taudip_var, 2) + Math.Pow(taustrike_var, 2));
                if ((float)tau_cst > (float)tau_var)
                    tau_var = ((taudip_cst * taudip_var) + (taustrike_cst * taustrike_var)) / tau_cst;

                // We also need to know the coefficient of friction on the fractures
                double MuFr = gbc.MechProps.MuFr;

                // We can now calculate U and V as the shear stress on the fractures minus the frictional traction
                // NB the rate of change of driving stress may not be constant, since tau_var may change through time
                // Here we set V to the rate of change of driving stress at the start of the timestep
                U = tau_cst - (MuFr * sneff_cst);
                V = tau_var - (MuFr * sneff_var);
            }

            // Set the constant and variable components of normal stress on the fracture in the current Fracture Calculation Data object
            CurrentFractureData.SigmaNeff_Const_M = sneff_cst;
            CurrentFractureData.SigmaNeff_Var_M = sneff_var;

            // Now we can calculate the optimal timestep duration for this fracture set

            // First we will calculate the time taken for the driving stress to reach zero (from either a positive or negative value) based on the rate of change of driving stress
            // This will act as an upper bound to the timestep length
            // If the driving stress is less than zero it will represent the time until the fracture set becomes active
            // If the driving stress is greater than zero it will represent the time until the fracture set stops propagating
            // This can only be calculated if the initial driving stress U is not zero and the rate of change of driving stress V is in the opposite direction to U
            // In fact we compare the initial driving stress to the maximum rounding error, not zero, to determine if this is the case.
            // This is because calculating the time required for driving stress to reach zero, multiplying it by horizontal strain rate to increment horizontal strain, and using new horizontal strain to calculate driving stress does not always give a driving stress = 0 (as it should).
            // Sometimes due to rounding errors in the calculation, it generates a slightly negative driving stress. This can cause the calculation to get stuck in a loop, with no strain increments and no fracture growth, until the maximum number of timesteps is reached.
            if ((Math.Abs(U) > PreviousFractureData.MaxDrivingStressRoundingError) && (Math.Sign(U) != Math.Sign(V)) && ((float)Math.Abs(V) > 0f))
            {
                // Calculating the time taken for the driving stress to reach zero for a fracture with shear displacement is more complicated than simply dividing the negative initial driving stress -U by the rate of change of driving stress V
                // As we have noted previously, the rate of change of driving stress may itself change through time, as the shear displacement vector and in situ stress change
                // We must therefore use a quadratic expression comprising the three orthogonal components of the stress acting on the fault: normal, shear in the direction of strike, and shear in the downdip direction
                // The rates of change of these components do not change through time

                // Calculate the multiplier for the normal stress component
                double mufr_squared = Math.Pow(gbc.MechProps.MuFr, 2);

                // Calculate the three quadratic terms
                double a_term = Math.Pow(taustrike_var, 2) + Math.Pow(taudip_var, 2) - (mufr_squared * Math.Pow(sneff_var, 2));
                double b_term = 2 * ((taustrike_cst * taustrike_var) + (taudip_cst * taudip_var) - (mufr_squared * sneff_cst * sneff_var));
                double c_term = Math.Pow(taustrike_cst, 2) + Math.Pow(taudip_cst, 2) - (mufr_squared * Math.Pow(sneff_cst, 2));
                double rootterm = Math.Sqrt(Math.Pow(b_term, 2) - (4 * a_term * c_term));

                // Take the lowest positive root as the optimal timestep duration
                // If U is negative and V is positive (i.e. the initial driving stress is negative but increasing) then at least one root should be positive
                // However in case neither are, or the quadratic does not have real roots (i.e. rootterm is NaN) then we can approximate the optimal timestep duration by dividing the negative initial driving stress -U by the rate of change of driving stress V
                double negativeroot = (-b_term - rootterm) / (2 * a_term);
                double positiveroot = (-b_term + rootterm) / (2 * a_term);
                double timeToSdZero;
                if ((negativeroot > PreviousFractureData.MaxDrivingStressRoundingError) && (negativeroot < positiveroot))
                    timeToSdZero = negativeroot;
                else if (positiveroot > PreviousFractureData.MaxDrivingStressRoundingError)
                    timeToSdZero = positiveroot;
                else
                    timeToSdZero = -U / V;
                if (timeToSdZero < optdur)
                    optdur = timeToSdZero;
            }

            // Next, we will calculate the time taken for the normal stress on the fracture to reach zero (from either a positive or negative value)
            // This will act as an upper bound to the timestep length if it is less than the time taken for the driving stress to reach zero
            // This represents the point at which the fracture will switch mode, from Mode 1 to Mode 2 or 3, or vice versa
            // This can only happen if it is moving in the right direction, and is not already zero
            if ((Math.Sign(sneff_cst) == -Math.Sign(sneff_var)) && ((float)sneff_cst != 0f) && ((float)sneff_var != 0f))
            {
                double timeToSneffZero = -(sneff_cst / sneff_var);
                if (timeToSneffZero < optdur)
                    optdur = timeToSneffZero;
            }

            // Finally, we will calculate a maximum timestep duration based on the rate of fracture growth, and the time taken for the largest fully active fractures to grow by the specified limit d_rmax
            // This is only required if the fracture set is growing
            // Otherwise we will return the upper bound to the duration (the time until fracture driving stress or normal stress reaches zero), or if this is not calculated, the default value infinity (no optimal duration calculated)
            // If the initial driving stress is negative, there will be no fracture growth in this timestep so we cannot calculate an optimal duration based on growth
            if (U < -PreviousFractureData.MaxDrivingStressRoundingError)
            {
                // Update the maximum driving stress rounding error
                PreviousFractureData.UpdateMaxDrivingStressRoundingError(U);

                // Since the initial driving stress is negative or zero throughout this timestep, we can set U and V to zero
                U = 0;
                V = 0;
            }
            // If the initial driving stress is zero and not increasing (i.e. U=0, V<=0), there will be no fracture growth in this timestep so we cannot calculate an optimal duration based on growth
            else if ((U < PreviousFractureData.MaxDrivingStressRoundingError) && ((float)V <= 0f))
            {
                // If U is not quite zero due to rounding error, we must round it up to zero
                // We can also set V to zero
                U = 0;
                V = 0;
            }
            // If the fracture set has been deactivated, there will be no fracture growth in this timestep so we cannot calculate an optimal duration based on growth
            // Driving stress may still be positive however
            else if (CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated)
            {
                // No calculation required
            }
            // If the initial driving stress is positive or zero, there will be no fracture growth in this timestep so the optimal timestep duration will be the estimated minimum time taken for the fracture set to grow by the specified limiting amount d_rmax
            // This can be calculated from the appropriate equations
            else
            {
                // If U is less than zero due to rounding error, we must round it up to zero
                if (U < 0) U = 0;

                // Cache constants locally
                double b = gbc.MechProps.b_factor;
                double beta = gbc.MechProps.beta;
                bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);
                double CapA = gbc.MechProps.CapA;
                double Kc = gbc.MechProps.Kc;
                double SqrtPi = Math.Sqrt(Math.PI);
                double sqrtpi_Kc_factor = 2 / (SqrtPi * Kc);
                //double alpha = CapA * Math.Pow(sqrtpi_Kc_factor, b);

                // If the fracture set has not yet been activated, activate the fracture set
                if (CurrentFractureData.EvolutionStage == FractureEvolutionStage.NotActivated)
                    CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Growing);

                // If the driving stress is constant we will set V=0, but only after completing the calculation for both fully active and restricted rays
                bool setVto0 = false;

                // Calculate the time taken for the largest fully active fractures with P30 above the cutoff to grow by the specified amount
                int FADatapointNo = 0;
                while (Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive].Count > FADatapointNo)
                {
                    // If the dFP30 value for this datapoint is below the minimum, move on to the next datapoint
                    double dP30 = Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][FADatapointNo].dP30;
                    if (dP30 < min_datapoint_MFP30)
                    {
                        FADatapointNo++;
                        continue;
                    }

                    double timeTodRmax;

                    // Get the current radius of the largest fully active fracture with P30 above the cutoff
                    double maxR = Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][FADatapointNo].RayLength;

                    // If a maximum dP33 increase is specified, find the radius the current fracture would need to grow to to increment dP33 by the required amount
                    double current_dP33 = (4 / 3) * Math.PI * Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][FADatapointNo].dP33factor;
                    double d_rmax_dP33 = Math.Pow((d_P33max / current_dP33) + 1, 1d / 3d) - 1;
                    // Use the lowest of the two radius increments to calculate the timestep duration
                    double dR = (double.IsNaN(d_rmax) || (d_rmax_dP33 < d_rmax)) ? d_rmax_dP33 : d_rmax;

                    // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                    double criticalRadius = Math.Pow(sqrtpi_Kc_factor * U, -2);
                    if (maxR > criticalRadius)
                    {
                        // Critical fracture propagation occurs at a constant rate
                        // The time taken to grow by a specified amount can thus be calculated by division
                        timeTodRmax = (dR * maxR) / CapA;
                    }
                    else
                    {
                        // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                        double drmax_term = bis2 ? Math.Log(1 + dR) : (1 - Math.Pow(1 + dR, 1 / beta));
                        double U_term = U * Math.Pow(U * sqrtpi_Kc_factor, b);
                        double V_term1 = bis2 ? drmax_term : -beta * (drmax_term * Math.Pow(maxR, 1 / beta));
                        double V_term2 = ((b + 1) * (V / CapA) * V_term1);
                        double UV_term = U_term + V_term2;

                        // If U>>V then the exact equation for optimal duration may give zero because (U_component + V_component2) ^ (1 / (b + 1)) is indistinguishable from U due to rounding
                        if ((float)(UV_term / U_term) > 1f)
                        {
                            // Use the formula for increasing stress to calculate optimal duration
                            timeTodRmax = (Math.Pow(UV_term, 1 / (b + 1)) / (V * Math.Pow(sqrtpi_Kc_factor, b / (b + 1)))) - (U / V);
                        }
                        else // In this case we can approximate V=0 and use the constant driving stress formula
                        {
                            // Use the formula for constant stress to calculate optimal duration, and set V to zero
                            timeTodRmax = V_term1 / (CapA * Math.Pow(U * sqrtpi_Kc_factor, b));
                            setVto0 = true;
                        }
                    }

                    if (timeTodRmax < optdur)
                        optdur = timeTodRmax;

                    break;
                }

                // Calculate the time taken for the largest restricted fractures with P30 above the cutoff to grow by the specified amount
                int RDatapointNo = 0;
                while (Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted].Count > RDatapointNo)
                {
                    // If the dFP30 value for this datapoint is below the minimum, move on to the next datapoint
                    double dP30 = Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][RDatapointNo].dP30;
                    if (dP30 < min_datapoint_MFP30)
                    {
                        RDatapointNo++;
                        continue;
                    }

                    double timeTodRmax;

                    // Get the current radius and controlling radius of the largest restricted fracture ray
                    double maxR = Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][RDatapointNo].RayLength;
                    double R0 = Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][RDatapointNo].PropagationControllingLength;

                    // If a maximum dP33 increase is specified, find the radius the current fracture would need to grow to to increment dP33 by the required amount
                    double current_dP33 = (4 / 3) * Math.PI * Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][FADatapointNo].dP33factor;
                    double d_rmax_dP33 = Math.Pow((d_P33max / current_dP33) + 1, 1d / 3d) - 1;
                    // Use the lowest of the two radius increments to calculate the timestep duration
                    double dR = (double.IsNaN(d_rmax) || (d_rmax_dP33 < d_rmax)) ? d_rmax_dP33 : d_rmax;

                    // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                    double criticalRadius = (2 * Math.Pow(sqrtpi_Kc_factor * U, -2)) - R0;
                    if (maxR > criticalRadius)
                    {
                        // Critical fracture propagation occurs at a constant rate
                        // The time taken to grow by a specified amount can thus be calculated by division
                        timeTodRmax = (dR * maxR) / CapA;
                    }
                    else
                    {
                        // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                        double drmax_r0_term = ((1 + dR) + (R0 / maxR)) / 2;
                        double rmax_r0_term = (maxR + R0) / (2 * maxR);
                        double drmax_term = 2 * (bis2 ? Math.Log(drmax_r0_term) - Math.Log(rmax_r0_term) : Math.Pow(rmax_r0_term, 1 / beta) - Math.Pow(drmax_r0_term, 1 / beta));
                        double U_term = U * Math.Pow(U * sqrtpi_Kc_factor, b);
                        double V_term1 = bis2 ? drmax_term : -beta * (drmax_term * Math.Pow(maxR, 1 / beta));
                        double V_term2 = ((b + 1) * (V / CapA) * V_term1);
                        double UV_term = U_term + V_term2;

                        // If U>>V then the exact equation for optimal duration may give zero because (V_factor + U_factor2) ^ (1 / (b + 1)) is indistinguishable from U due to rounding
                        if ((float)(UV_term / U_term) > 1f)
                        {
                            // Use the formula for increasing stress to calculate optimal duration
                            timeTodRmax = (Math.Pow(UV_term, 1 / (b + 1)) / (V * Math.Pow(sqrtpi_Kc_factor, b / (b + 1)))) - (U / V);
                        }
                        else // In this case we can approximate V=0 and use the constant driving stress formula
                        {
                            // Use the formula for constant stress to calculate optimal duration, and set V to zero
                            timeTodRmax = V_term1 / (CapA * Math.Pow(U * sqrtpi_Kc_factor, b));
                            setVto0 = true;
                        }
                    }

                    if (timeTodRmax < optdur)
                        optdur = timeTodRmax;

                    break;
                }
                if (setVto0)
                    V = 0;
            }

            // Set the constant and variable components of driving stress in the current Fracture Calculation Data object
            CurrentFractureData.U_M = U;
            CurrentFractureData.V_M = V;

            // Recalculate the fracture driving stress vectors
            if (U <= 0)
            {
                DrivingStressVector = new VectorXYZ(0, 0, 0);
            }
            else if (sneff_cst <= 0)
            {
                DrivingStressVector = SigmaF_Const;
            }
            else
            {
                double MuFr = gbc.MechProps.MuFr;
                double sinpitch = VectorXYZ.Sin_trim(ShearStressPitch);
                double cospitch = VectorXYZ.Cos_trim(ShearStressPitch);

                DrivingStressVector = ((taudip_cst - (sinpitch * MuFr * sneff_cst)) * dipVector) + ((taustrike_cst - (cospitch * MuFr * sneff_cst)) * strikeVector);
            }

            // Return the calculated maximum duration
            return optdur;
        }
        /// <summary>
        /// Set the duration, driving stress and propagation rate data for the current timestep; this can only be done after U and V are set using the getOptimalDuration function
        /// </summary>
        /// <param name="CurrentTime_in">Time at start of timestep (s)</param>
        /// <param name="TimestepDuration_in">Timestep duration (s)</param>
        public void setTimestepPropagationData(double CurrentTime_in, double TimestepDuration_in)
        {
            // Get a reference to the mechanical property data object for the gridblock
            MechanicalProperties MechProps = gbc.MechProps;

            // Set the timestep start time
            CurrentFractureData.M_StartTime = CurrentTime_in;

            // Cache required mechanical properties locally
            double CapA = MechProps.CapA;
            double b = MechProps.b_factor;
            double beta = MechProps.beta;
            bType b_type = MechProps.GetbType();
            bool bis2 = (b_type == bType.Equals2);
            double Kc = MechProps.Kc;
            double SqrtPi = Math.Sqrt(Math.PI);
            double sqrtpi_Kc_factor = 2 / (SqrtPi * Kc);
            //double alpha = CapA * Math.Pow(sqrtpi_Kc_factor, b);
            // Flag to show that the fracture set has not been deactivated
            bool FracturesActive = !(CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated);

            // Set the flag for whether subcritical fracture propagation index b is less than, equal to or greater than 2
            CurrentFractureData.M_bType = b_type;

            // Cache constant and variable components of driving stress for this timestep locally
            double U_M = CurrentFractureData.U_M;
            double V_M = CurrentFractureData.V_M;

            // Calculate weighted mean driving stress (Pa) and fracture propagation rate coefficient (gamma ^ 1/beta) during timestep M
            // These will have default values of zero if there is no fracture propagation in this timestep
            // Weighted mean driving stress during timestep M (Pa)
            double mean_SigmaD_M = 0;
            // Fracture propagation rate coefficient (gamma ^ 1/beta) - a helper function related to fracture propagation rate for timestep M 
            // (alpha / |B|) * SigmaD^b (m^(1+b/2)/s) for b!=2; alpha * SigmaD^2 (m^2/s) for b=2
            // This is the same for both constant and variable driving stress, but is calculated differently to optimise accuracy
            double F_PropRate_Coefficient = 0;
            double F_PropRate_Coefficient_time = 0;
            if ((float)U_M >= 0f) // If the initial driving stress is less than zero, the mean driving stress for the timestep will be zero
            {
                // Calculate the final driving stress for the timestep
                double final_SigmaD_M = U_M + (TimestepDuration_in * V_M);

                if ((float)final_SigmaD_M == (float)U_M) // If the final driving stress is equal to the initial driving stress then assume the driving stress is constant; NB we check this rather than checking for V_M equals zero, as this will also pick up very short timesteps, where the change in driving stress during the timestep is negligible even though V_M > 0
                {
                    // To calculate alpha * SigmaDb_M, we divide mean_SigmaD_M by Kc before raising it to b, to avoid excessively large numbers
                    mean_SigmaD_M = U_M;
                    // We will only calculate the fracture propagation rate coefficient if the fractures are active
                    if (FracturesActive)
                    {
                        F_PropRate_Coefficient = CapA * Math.Pow(sqrtpi_Kc_factor * mean_SigmaD_M, b);
                        F_PropRate_Coefficient_time = F_PropRate_Coefficient * TimestepDuration_in;
                        if (!bis2)
                        {
                            F_PropRate_Coefficient /= Math.Abs(beta);
                            F_PropRate_Coefficient_time /= beta;
                        }
                    }
                }
                else // If final driving stress is not equal to the initial driving stress then the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    // We will combine the U and V power terms with Kc to avoid getting extreme values when b is high
                    double UV_U_term = (final_SigmaD_M < 0 ? 0 : (final_SigmaD_M * Math.Pow(sqrtpi_Kc_factor * final_SigmaD_M, b)) - (U_M * Math.Pow(sqrtpi_Kc_factor * U_M, b)));

                    mean_SigmaD_M = Math.Pow(UV_U_term / ((b + 1) * V_M * TimestepDuration_in), 1 / b) / sqrtpi_Kc_factor;
                    // We will only calculate the mean half-macrofracture propagation rate and microfracture propagation rate coefficient if the fractures are active
                    if (FracturesActive)
                    {
                        F_PropRate_Coefficient_time = CapA * (UV_U_term / ((b + 1) * V_M));
                        F_PropRate_Coefficient = F_PropRate_Coefficient_time / TimestepDuration_in;
                        if (!bis2)
                        {
                            F_PropRate_Coefficient /= Math.Abs(beta);
                            F_PropRate_Coefficient_time /= beta;
                        }
                    }
                }
            }

            // Set the growth rates for all fully active datapoints
            foreach (ImplicitFracturePopulationDatapoint activeFracturePopulationDatapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive])
            {
                double initialR = activeFracturePopulationDatapoint.RayLength;
                // Calculation of the increment in ray length will depend on whether propagation of the fracture is critical or subcritical
                double incrementR;
                // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                double criticalRadius = Math.Pow(sqrtpi_Kc_factor * U_M, -2);
                if (initialR > criticalRadius)
                {
                    // Critical fracture propagation occurs at a constant rate
                    incrementR = CapA * TimestepDuration_in;
                }
                else
                {
                    // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                    double finalR = bis2 ? (initialR * Math.Exp(F_PropRate_Coefficient_time)) : Math.Pow(Math.Pow(initialR, 1 / beta) + F_PropRate_Coefficient_time, beta);
                    // If the fracture reaches the blow-up radius within this timestep then the finalR calculation will return NaN (this can only happen for R>2)
                    // In this case we will use the critical propagation rate to calculate the growth increment
                    if (double.IsNaN(finalR))
                        incrementR = CapA * TimestepDuration_in;
                    else
                        incrementR = finalR - initialR;
                }

                // Check if this will cause the fracture to exceed the maximum allowed radius, and if so reduce the propagation increment
                if (incrementR > (MaximumFractureRadius - initialR))
                    incrementR = MaximumFractureRadius - initialR;

                activeFracturePopulationDatapoint.RayLengthIncrement = incrementR;
            }

            // Set the growth rates for all restricted datapoints
            foreach (ImplicitFracturePopulationDatapoint restrictedFracturePopulationDatapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted])
            {
                double initialR = restrictedFracturePopulationDatapoint.RayLength;
                double initialReff = restrictedFracturePopulationDatapoint.EffectiveRayLength;
                double initialRc = restrictedFracturePopulationDatapoint.PropagationControllingLength;
                // Calculation of the increment in ray length will depend on whether propagation of the fracture is critical or subcritical
                double incrementR;
                // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                double criticalRadius = (2 * Math.Pow(sqrtpi_Kc_factor * U_M, -2)) - initialRc;
                if (initialR > criticalRadius)
                {
                    // Critical fracture propagation occurs at a constant rate
                    incrementR = CapA * TimestepDuration_in;
                }
                else
                {
                    // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                    double finalR = bis2 ? (2 * initialReff * Math.Exp(F_PropRate_Coefficient_time / 2)) - initialRc : (2 * Math.Pow(Math.Pow(initialReff, 1 / beta) + (F_PropRate_Coefficient_time / 2), beta)) - initialRc;
                    // If the fracture reaches the blow-up radius within this timestep then the finalR calculation will return NaN (this can only happen for R>2)
                    // In this case we will use the critical propagation rate to calculate the growth increment
                    if (double.IsNaN(finalR))
                        incrementR = CapA * TimestepDuration_in;
                    else
                        incrementR = finalR - initialR;
                }

                // Check if this will cause the fracture to exceed the maximum allowed radius, and if so reduce the propagation increment
                if (incrementR > (MaximumFractureRadius - initialR))
                    incrementR = MaximumFractureRadius - initialR;

                restrictedFracturePopulationDatapoint.RayLengthIncrement = incrementR;
            }

            // Set the timestep duration mean driving stress, mean macrofracture propagation rate and microfracture propagation rate coefficient (= gamma ^ 1/beta)
            CurrentFractureData.SetDynamicData(TimestepDuration_in, mean_SigmaD_M, F_PropRate_Coefficient);

            // Add a new fully active datapoint representing fractures nucleating in this timestep - but only if any new fractures nucleate
            if (FracturesActive && ((float)mean_SigmaD_M > 0f))
            {
                ImplicitFracturePopulationDatapoint nucleatingFractures = getNucleatingFractures();
                if (!(nucleatingFractures is null) && ((float)nucleatingFractures.dP30 > 0f))
                    Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive].Add(nucleatingFractures);
            }
        }
        /// <summary>
        /// Get a fracture population datapoint representing the new fractures nucleating during the current timestep
        /// </summary>
        /// <returns></returns>
        private ImplicitFracturePopulationDatapoint getNucleatingFractures()
        {
            // Cache required data locally
            double beta = gbc.MechProps.beta;
            bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);
            double rmin_beta = bis2 ? Math.Log(MinimumFractureRadius) : Math.Pow(MinimumFractureRadius, 1 / beta);
            double cumGammaRmin_N = rmin_beta + CurrentFractureData.Cum_Gamma_M;
            double cumGammaRmin_Nminus1 = rmin_beta + previous_CumGamma;// CurrentFractureData.Cum_Gamma_Mminus1;

            // Get the initial and final volumetric density of fractures with radius > rmin for the current timestep, ignoring stress shadows
            // Only Power Law is currently implemented
            double dMFP30 = 0;
            switch (InitialDistribution)
            {
                case InitialFractureDistribution.PowerLaw:
                    {
                        // betac_factor is -beta*c if b<>2, -c if b=2
                        double betac_factor = (bis2 ? -c_coefficient : -(beta * c_coefficient));
                        dMFP30 = CapB * (bis2 ? Math.Exp(cumGammaRmin_N * betac_factor) - Math.Exp(cumGammaRmin_Nminus1 * betac_factor) : Math.Pow(cumGammaRmin_N, betac_factor) - Math.Pow(cumGammaRmin_Nminus1, betac_factor));
                    }
                    break;
                case InitialFractureDistribution.Exponential:
                    break;
                case InitialFractureDistribution.LogNormal:
                    break;
                default:
                    break;
            }

            // Multiply the volumetric density increment by the total clear zone volume seen by fully active fractures with minimum radius
            // This will correct for fractures nucleating in an exclusion zone
            dMFP30 *= CurrentFractureData.theta_dashed_Mminus1;

            // If the calculated dMFP30 value is less than the specified minimum, return null (no new datapoint will be created)
            if (dMFP30 < min_datapoint_MFP30)
                return null;

            // Multiply by the number of rays per fracture to get the volumetric density of rays
            double dRP30 = dMFP30 * (double)RaysPerFracture;

            // Create a new datapoint and return it
            // Also update the cumulative value of gamma_InvBeta_K * K_duration at the last time new fractures nucleated
            previous_CumGamma = CurrentFractureData.Cum_Gamma_M;
            return new ImplicitFracturePopulationDatapoint(MinimumFractureRadius, dRP30);
        }
        /// <summary>
        /// Calculate the probability of deactivation for fractures represented by each of the datapoints, and implement the deactivation
        /// </summary>
        public void setFractureDeactivationRate()
        {
            // Check the array for the orientation multipliers for the weighted mean linear density of fractures from other sets seen by rays from this set
            // If this is null or the wrong size, recalculate it
            int noSets = gbc.UnconfinedFractureSets.Count;
            if ((orientationMultipliers is null) || (orientationMultipliers.GetLength(0) != noSets) || (orientationMultipliers.GetLength(1) != RaysPerFracture))
            {
                orientationMultipliers = new double[noSets, RaysPerFracture];
                for (int setNo = 0; setNo < noSets; setNo++)
                {
                    UnconfinedFractureSet ufs = gbc.UnconfinedFractureSets[setNo];
                    for (int rayNo = 0; rayNo < RaysPerFracture; rayNo++)
                        orientationMultipliers[setNo, rayNo] = Math.Abs(ufs.normalVector & this.rayVectors[rayNo]) / (double)RaysPerFracture;
                }
            }

            // Cache the stress shadow width ratio, fracture growth deactivation cutoff and minimum fracture activation probability locally
            double stressShadowWidthRatio = Max_F_StressShadowWidth_r;
            double max_R_deactivation = gbc.PropControl.max_R_DeactivationCheck_interval;
            double min_R_activation = gbc.PropControl.min_R_ActivationProbability;

            // Loop through all restricted datapoints, calculating deactivation rates due to stress shadow interaction and intersection
            // We will calculate the restricted datapoints before the fully active ones, as the deactivation of fully active fractures will create new restricted fractures
            foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted])
            {
                // If the ray has already reached the maximum length it will not be deactivated
                if ((float)datapoint.RayLength == (float)MaximumFractureRadius)
                    continue;

                // Get the probability that a fracture represented by this datapoint will not be deactivated due to stress shadow interaction in the current timestep
                // This is given by the inverse of the interaction zone volume around all other fractures in the current set
                double phiII_M = Fractures.getStressShadowNonInteractionVolume(datapoint, stressShadowWidthRatio);

                // Get the probability that a fracture represented by this datapoint will not be deactivated due to intersecting a fracture from another set in the current timestep
                // This is given by the inverse of the interaction zone volume around all other fractures in the current set
                double mean_apparent_P32 = 0;
                double minIntersectionRadius = gbc.PropControl.MinIntersectionDeactivationRatio * datapoint.EffectiveRayLength;
                for (int setNo = 0; setNo < noSets; setNo++)
                {
                    UnconfinedFractureSet ufs = gbc.UnconfinedFractureSets[setNo];
                    for (int rayNo = 0; rayNo < RaysPerFracture; rayNo++)
                        mean_apparent_P32 += (ufs.Fractures.cumulative_FP32(minIntersectionRadius) * orientationMultipliers[setNo, rayNo]);
                }
                double phiIJ_M = Math.Exp(-mean_apparent_P32 * datapoint.RayLengthIncrement);

                // Update the fracture activation probabilites for the datapoint
                datapoint.UpdateFractureActivationProbabilities(phiII_M, phiIJ_M);

                // Only implement deactivation if the ray has grown by the specified amount or the activation probability has dropped below a minimum value
                if ((datapoint.ProportionalGrowthSinceLastDeactivationCheck >= max_R_deactivation) || (datapoint.Phi <= min_R_activation))
                {
                    // If the ray has already reached the maximum length it will not be deactivated
                    if ((float)datapoint.RayLength == (float)MaximumFractureRadius)
                        continue;

                    // Calculate the probability that this ray will be deactivated due to stress shadow interaction and due to intersection during this timestep
                    // This function will also reduce the volumetric density for the datapoint proportionally
                    ImplicitFracturePopulationDatapoint[] newdatapoints = datapoint.DeactivateRays();

                    // Add the new datapoints to the appropriate arrays - but only if the volumetric density is greater than zero
                    if (newdatapoints.Length == 2)
                    {
                        if ((float)newdatapoints[0].dP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticStressShadow].Add(newdatapoints[0]);
                        if ((float)newdatapoints[1].dP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticIntersection].Add(newdatapoints[1]);
                    }
                }
            }

            // Loop through all fully active datapoints, calculating deactivation rates due to stress shadow interaction and intersection
            foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive])
            {
                // Get the probability that a fracture represented by this datapoint will not be deactivated due to stress shadow interaction in the current timestep
                // This is given by the inverse of the interaction zone volume around all other fractures in the current set
                double phiII_M = Fractures.getStressShadowNonInteractionVolume(datapoint, stressShadowWidthRatio);

                // Get the probability that a fracture represented by this datapoint will not be deactivated due to intersecting a fracture from another set in the current timestep
                // This is given by the inverse of the interaction zone volume around all other fractures in the current set
                double mean_apparent_P32 = 0;
                double minIntersectionRadius = gbc.PropControl.MinIntersectionDeactivationRatio * datapoint.EffectiveRayLength;
                for (int setNo = 0; setNo < noSets; setNo++)
                {
                    UnconfinedFractureSet ufs = gbc.UnconfinedFractureSets[setNo];
                    for (int rayNo = 0; rayNo < RaysPerFracture; rayNo++)
                        mean_apparent_P32 += (ufs.Fractures.cumulative_FP32(minIntersectionRadius) * orientationMultipliers[setNo, rayNo]);
                }
                double phiIJ_M = Math.Exp(-mean_apparent_P32 * datapoint.RayLengthIncrement);

                // Update the fracture activation probabilites for the datapoint
                datapoint.UpdateFractureActivationProbabilities(phiII_M, phiIJ_M);

                // Only implement deactivation if the ray has grown by the specified amount or the activation probability has dropped below a minimum value
                if ((datapoint.ProportionalGrowthSinceLastDeactivationCheck >= max_R_deactivation) || (datapoint.Phi <= min_R_activation))
                {
                    // Calculate the probability that this ray will be deactivated due to stress shadow interaction and due to intersection during this timestep
                    // This function will also reduce the volumetric density for the datapoint proportionally
                    ImplicitFracturePopulationDatapoint[] newdatapoints = datapoint.DeactivateRays();

                    // Add the new datapoints to the appropriate arrays
                    if (newdatapoints.Length == 3)
                    {
                        if ((float)newdatapoints[0].dP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted].Add(newdatapoints[0]);
                        if ((float)newdatapoints[1].dP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticStressShadow].Add(newdatapoints[1]);
                        if ((float)newdatapoints[2].dP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticIntersection].Add(newdatapoints[2]);
                    }
                }
            }
        }
        /// <summary>
        /// Lock in the previously calculated increments in ray length and resort the fracture population distribution arrays
        /// </summary>
        public void updateTotalFracturePopulation()
        {
            // Lock in the increments in ray length calculated by setTimestepPropagationData()
            // If any rays have reached the maximum specified length, deactivate them and move the appropriate datapoints to the StaticMaxRadius array
            foreach (RayPropagationStatus status in new RayPropagationStatus[2] { RayPropagationStatus.FullyActive, RayPropagationStatus.Restricted })
            {
                int noDataPoints = Fractures.fracturePopulationDatapoints[status].Count;
                for (int datapointNo = noDataPoints - 1; datapointNo >= 0; datapointNo--)
                {
                    ImplicitFracturePopulationDatapoint datapoint = Fractures.fracturePopulationDatapoints[status][datapointNo];
                    datapoint.IncrementRayLength();
                    if ((float)datapoint.RayLength >= (float)MaximumFractureRadius)
                    {
                        Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticMaxRadius].Add(datapoint);
                        Fractures.fracturePopulationDatapoints[status].RemoveAt(datapointNo);
                    }
                }
            }

            // Recalculate the total RP30 and RP32 values from the fracture population distribution arrays
            // This will also resort the fracture population distribution arrays based on new effective radius, from largest to smallest
            Fractures.RecalculateTotalPopulationData();
        }
        /// <summary>
        /// Cull datapoints from the static fracture population distribution arrays
        /// </summary>
        public void cullStaticFracturePopulationDatapoints()
        {
            // Cache the minimum proportional size difference for static unconfined fracture datapoints locally
            double minDatapointSizeRatio = gbc.PropControl.min_R_staticDatapointSizeRatio;
            Fractures.CullArray(RayPropagationStatus.StaticStressShadow, minDatapointSizeRatio);
            Fractures.CullArray(RayPropagationStatus.StaticIntersection, minDatapointSizeRatio);
        }

        /// <summary>
        /// Update the values describing the inverse stress shadow and clear zone volumes for this fracture set
        /// </summary>
        public void setFractureExclusionZoneData()
        {
            double theta;
            double theta_dashed = Fractures.getStressShadowClearZoneVolume(MinimumFractureRadius, Max_F_StressShadowWidth_r, out theta);
            CurrentFractureData.SetFractureExclusionZoneData(theta, theta_dashed);
        }

        /// <summary>
        /// Set the current fracture evolution stage for the entire fracture set to Deactivated
        /// </summary>
        private void deactivateFractures()
        {
            CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
        }
        /// <summary>
        /// Check if the if the extrapolated initial radius of a fully active fracture with current radius r is zero or less (will only apply if b is less than 2)
        /// </summary>
        /// <param name="r">Current fracture radius</param>
        /// <returns>True if initial fracture radius is zero or less; false if it is greater than zero or b is greater than or equal to 2</returns>
        public bool Check_Initial_uF_Radius(double r)
        {
            double b = gbc.MechProps.b_factor;
            if (b < 2)
            {
                double initial_minrb_minRad = Math.Pow(r, (2 - b) / 2) + CurrentFractureData.Cum_Gamma_M;
                if (initial_minrb_minRad <= 0)
                    return true;
            }
            return false;
        }

        // Reset and data input functions
        /// <summary>
        /// Clear the implicit fracture population arrays and reset them to the initial state (comprising a single fully active fracture datapoint representing initial microfractures)
        /// </summary>
        public void clearImplicitFracturePopulationArrays()
        {
            Fractures.ResetPopulationDistributionData(InitialP30(), MinimumFractureRadius, RaysPerFracture, gbc.PropControl.MinStressShadowDeactivationRatio, gbc.PropControl.MinIntersectionDeactivationRatio);
        }
        /// <summary>
        /// Reset all fracture data to initial values (implicit population comprising only initial microfractures, no explicit fractures, no previous deformation)
        /// </summary>
        /// <param name="raysPerFracture_in">Number of rays comprising each fracture</param>
        /// <param name="rmin_in">Minimum radius for a fracture; this will be the length of the rays at nucleation</param>
        /// <param name="uFDistributionIn">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        public void resetFractureData(ushort raysPerFracture_in, double rmin_in, double rmax_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in)
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
            // Maximum allowed radius for a fracture; rays will stop propagating when they reach this length
            MaximumFractureRadius = rmax_in;
            // Create new UnconfinedFractureData object
            Fractures = new UnconfinedFractureData(InitialP30(), rmin_in, raysPerFracture_in, gbc.PropControl.MinStressShadowDeactivationRatio, gbc.PropControl.MinIntersectionDeactivationRatio);

            // Recreate the array of fracture ray vectors
            // We start with the dip vector and then rotate this using the Rodrigues method
            rayVectors = new VectorXYZ[raysPerFracture_in];
            double rotationAngle = (2d * Math.PI) / (double)raysPerFracture_in;
            double sinRotationAngle = VectorXYZ.Sin_trim(rotationAngle);
            double cosRotationAngle = VectorXYZ.Cos_trim(rotationAngle);
            VectorXYZ nextVector = new VectorXYZ(dipVector);
            for (int rayNo = 0; rayNo < raysPerFracture_in; rayNo++)
            {
                rayVectors[rayNo] = nextVector;
                nextVector = (cosRotationAngle * nextVector) + (sinRotationAngle * (normalVector * nextVector)) + ((1 - cosRotationAngle) * (normalVector & nextVector) * normalVector);
            }

            // Create new fracture calculation data object, and initialise for timestep 0 (i.e. initial data before the model runs)
            CurrentFractureData = new FractureCalculationData_Minimised();
            // Create an new list for previous fracture calculation data objects and use the CurrentFractureData object for timestep 0 
            PreviousFractureData = new FCD_List_Minimised(CurrentFractureData, true);
            // Set the flag to deactivate the fracture set at the start of the next timestep to false
            DeactivateNextTimestep = false;

            // Set the cumulative value of gamma_InvBeta_K * K_duration at the last time new fractures nucleated to 0
            previous_CumGamma = 0;
            // Set the minimum RP30 value for a nucleating fracture datapoint
            // This is the value that will generate the specified dP33 value if all fractures grow to the maximum radius
            double dP33 = gbc.PropControl.max_TS_MFP33_increase;
            double maxFracVol = (4 / 3) * Math.PI * Math.Pow(MaximumFractureRadius, 3);
            min_datapoint_MFP30 = dP33 / maxFracVol;
        }

        // Constructors
        /// <summary>
        /// Default constructor: set default values 
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in)
                    : this(gbc_in, 0, Math.PI / 2, 8, 0.1, 100, InitialFractureDistribution.PowerLaw, 0.001, 3d)
        {
            // Defaults:

            // Fracture orientation: set to vertical N-striking fractures
            // Number of rays per fracture: set to 8
            // Minimum fracture radius: set to 0.1
            // Minimum fracture radius: set to 100
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
        /// <param name="rmax_in">Maximum allowed radius for a fracture; rays will stop propagating when they reach this length</param>
        /// <param name="uFDistributionIn">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in, double Strike_in, double Dip_in, int raysPerFracture_in, double rmin_in, double rmax_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in)
            : this(gbc_in, Strike_in, Dip_in, raysPerFracture_in, rmin_in, rmax_in, uFDistributionIn, B_in, c_in, 0.0005, 1E-5)
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
        /// <param name="rmax_in">Maximum allowed radius for a fracture; rays will stop propagating when they reach this length</param>
        /// <param name="uFDistributionIn">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="UniformAperture_in">Fixed aperture for fractures in the uniform aperture case (m)</param>
        /// <param name="SizeDependentApertureMultiplier_in">Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in, double Strike_in, double Dip_in, int raysPerFracture_in, double rmin_in, double rmax_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in, double UniformAperture_in, double SizeDependentApertureMultiplier_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set fracture orientation
            Strike = Strike_in;
            Dip = Dip_in;

            // Set the initial shear stress pitch to NaN and the initial shear stress vector to (0,0,0)
            // This represents no shear stress on the fracture
            ShearStressPitch = double.NaN;
            shearStressVector = new VectorXYZ(0, 0, 0);

            // Set the initial shear displacement pitch to NaN and the initial shear displacement vector to (0,0,0)
            // This represents no shear displacement
            //DisplacementPitch = double.NaN;
            //shearDisplacementVector = new VectorXYZ(0, 0, 0);

            // Set the initial driving stress vectors to (0,0,0)
            DrivingStressVector = new VectorXYZ(0, 0, 0);
            //MFDisplacementAdjustedDrivingStressVector = new VectorXYZ(0, 0, 0);

            // Calculate the initial compliance tensor base; NB we assume initial driving stress is zero
            RecalculateComplianceTensorBase(false, false);

            // Reset implicit fracture population data
            resetFractureData((ushort)raysPerFracture_in, rmin_in, rmax_in, uFDistributionIn, B_in, c_in);

            // Set fracture aperture control data for uniform and size-dependent aperture
            // Fixed aperture for fractures in the uniform aperture case (m)
            UniformAperture = UniformAperture_in;
            // Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier
            SizeDependentApertureMultiplier = SizeDependentApertureMultiplier_in;

            // Set the maximum historic active fracture volumetric ratio to 0
            max_historic_a_RP32 = 0;
        }

    }
}
