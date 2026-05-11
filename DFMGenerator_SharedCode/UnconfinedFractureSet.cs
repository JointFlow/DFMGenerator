using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

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
        public double Strike { get { return strike; } }
        /// <summary>
        /// Azimuth of fracture set (radians); positive in the JPlus direction
        /// </summary>
        public double Azimuth { get { double azimuth = strike + (Math.PI / 2); while (azimuth >= (2 * Math.PI)) azimuth -= (2 * Math.PI); return azimuth; } }
        /// <summary>
        /// Fracture dip
        /// </summary>
        public double Dip { get { return dip; } }
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
        /// Get the orientation of the unconfined fracture ray closest to the supplied vector
        /// </summary>
        /// <param name="InputVector">Vector to test against the rays of this unconfined fracture set</param>
        /// <returns>VectorXYZ object representing the orientation of the closest unconfined fracture ray to the supplied vector</returns>
        public VectorXYZ getClosestRayOrientation(VectorXYZ InputVector)
        {
            // Normalise the input vector if it is not already normalised - therefore the dot product of the input vector and the ray vectors will just be the cosine of the angle between them
            VectorXYZ vectorToTest = InputVector.GetNormalisedVector();

            // Loop through all ray vectors, checking the cosine of the angle between them and the input vector
            // The closest match in orientation will have a value of cosine nearest to 1
            // In this case we must match the direction as well as the orientation of the vectors, so we should not take the absolute value of the cosine
            // A cosine of -1 indicates an opposite vector direction thus a poor match
            VectorXYZ closestRayOrientation = null;
            double bestMatch = 0;
            foreach (VectorXYZ rayVector in RayVectors)
            {
                double orientationMatch = rayVector & vectorToTest;
                if (orientationMatch > bestMatch)
                {
                    bestMatch = orientationMatch;
                    closestRayOrientation = rayVector;
                }
            }

            // Return the orientation of the closest matching ray
            return closestRayOrientation;
        }
        /// <summary>
        /// Find the index number of the unconfined fracture ray with orientation closest to the supplied vector
        /// </summary>
        /// <param name="InputVector">Vector to test against the rays of this unconfined fracture set</param>
        /// <returns>Index number of the UnconfinedFractureSet with vector closest to the supplied vector</returns>
        public int getClosestRayIndex(VectorXYZ InputVector)
        {
            // Normalise the input vector if it is not already normalised - therefore the dot product of the input vector and the ray vectors will just be the cosine of the angle between them
            VectorXYZ vectorToTest = InputVector.GetNormalisedVector();

            // Loop through all ray vectors, checking the cosine of the angle between them and the input vector
            // The closest match in orientation will have a value of cosine nearest to 1
            // In this case we must match the direction as well as the orientation of the vectors, so we should not take the absolute value of the cosine
            // A cosine of -1 indicates an opposite vector direction thus a poor match
            int closestRayIndex = -1;
            double bestMatch = 0;
            for (int ray_index = 0; ray_index < RaysPerFracture; ray_index++)
            {
                VectorXYZ rayVector = rayVectors[ray_index];
                double orientationMatch = rayVector & vectorToTest;
                if (orientationMatch > bestMatch)
                {
                    bestMatch = orientationMatch;
                    closestRayIndex = ray_index;
                }
            }

            // Return the index number of the closest matching ray
            return closestRayIndex;
        }
        /// <summary>
        /// Convert a vector in XYZ coordinates into a vector in FDS coordinates for this fracture set (fracture normal, fracture dip, fracture strike)
        /// NB since we have not yet created and FDS vector object, the output will be supplied as individual coordinates in reference variables
        /// </summary>
        /// <param name="InputVector">Vector in XYZ coordinates to convert</param>
        /// <param name="Fcoord">Reference variable for the F coordinate of the FDS vector</param>
        /// <param name="Dcoord">Reference variable for the D coordinate of the FDS vector</param>
        /// <param name="Scoord">Reference variable for the S coordinate of the FDS vector</param>
        public void convertXYZVectortoFDSVector(VectorXYZ InputVector, out double Fcoord, out double Dcoord, out double Scoord)
        {
            double sinAzi = VectorXYZ.Sin_trim(Azimuth);
            double cosAzi = VectorXYZ.Cos_trim(Azimuth);
            double sinDip = VectorXYZ.Sin_trim(Dip);
            double cosDip = VectorXYZ.Cos_trim(Dip);

            double Xcoord = InputVector.Component(VectorComponents.X);
            double Ycoord = InputVector.Component(VectorComponents.Y);
            double Zcoord = InputVector.Component(VectorComponents.Z);

            Fcoord = (sinAzi * sinDip * Xcoord) + (cosAzi * sinDip * Ycoord) + (cosDip * Zcoord);
            Dcoord = (sinAzi * cosDip * Xcoord) + (cosAzi * cosDip * Ycoord) + (-sinDip * Zcoord);
            Scoord = (-cosAzi * Xcoord) + (sinAzi * Ycoord);
        }
        /// <summary>
        /// Convert a vector in FDS coordinates for this fracture set (fracture normal, fracture dip, fracture strike) into a vector in XYZ coordinates
        /// NB since we have not yet created and FDS vector object, the input will be supplied as individual coordinates
        /// </summary>
        /// <param name="Fcoord">F coordinate of the input FDS vector</param>
        /// <param name="Dcoord">D coordinate of the input FDS vector</param>
        /// <param name="Scoord">S coordinate of the input FDS vector</param>
        /// <returns>Vector in XYZ coordinates</returns>
        public VectorXYZ convertFDSVectortoXYZVector(double Fcoord, double Dcoord, double Scoord)
        {
            double sinAzi = VectorXYZ.Sin_trim(Azimuth);
            double cosAzi = VectorXYZ.Cos_trim(Azimuth);
            double sinDip = VectorXYZ.Sin_trim(Dip);
            double cosDip = VectorXYZ.Cos_trim(Dip);

            double Xcoord = (sinAzi * sinDip * Fcoord) + (sinAzi * cosDip * Dcoord) + (-cosAzi * Scoord);
            double Ycoord = (cosAzi * sinDip * Fcoord) + (cosAzi * cosDip * Dcoord) + (sinAzi * Scoord);
            double Zcoord = (cosDip * Fcoord) + (-sinDip * Dcoord);

            return new VectorXYZ(Xcoord, Ycoord, Zcoord);
        }

        /// <summary>
        /// StressDistribution object describing the spatial distribution of the fractures: EvenlyDistributedStress gives a random fracture distribution, StressShadow and DuctileBoundary gives a regular spacing
        /// </summary>
        public StressDistribution FractureDistribution { get; set; }
        /// <summary>
        /// Array for the orientation multipliers for the weighted mean linear density of fractures from other sets seen by rays from this set
        /// </summary>
        private double[,] orientationMultipliers;

        // Fracture distribution control data
        /// <summary>
        /// Allowable rounding error in dRP30 when calculating whether a new datapoint will be nucleated
        /// </summary>
        const double dRP30_rounding_error = 0.999;
        /// <summary>
        /// Factor a used in Winitzki's algorithms for approximating erf and inverse erf
        /// </summary>
        private const double Winitzki_a = (8 * (Math.PI - 3)) / (3 * Math.PI * (4 - Math.PI));
        /// <summary>
        /// Winitzki's algorithm for calculating an approximate value for the error function erf
        /// </summary>
        /// <param name="x_in">Input value</param>
        /// <returns>~erf(x)</returns>
        public static double erf(double x_in)
        {
            double erf = Math.Sign(x_in) * Math.Sqrt(1 - Math.Exp(-(x_in * x_in) * (((4 / Math.PI) + (Winitzki_a * x_in * x_in)) / (1 + (Winitzki_a * x_in * x_in)))));
            return erf;
        }
        /// <summary>
        /// Winitzki's algorithm for calculating an approximate value for the inverse of the error function erf
        /// </summary>
        /// <param name="erf_in">Value of the error function erf (between -1 and 1)</param>
        /// <returns>Value of x such that erf(x)~erf_in</returns>
        public static double inverse_erf(double erf_in)
        {
            double term1 = Math.Log(1 - (erf_in * erf_in));
            double term2 = (2 / (Math.PI * Winitzki_a)) + (term1 / 2);
            double x_out = Math.Sign(erf_in) * Math.Sqrt(Math.Sqrt((term2 * term2) - (term1 / Winitzki_a)) - term2);
            return x_out;
        }
        /// <summary>
        /// Winitzki's algorithm for calculating an approximate value for half of the complementary error function erfc
        /// This represents the cumulative density distribution function for a log normal distribution
        /// </summary>
        /// <param name="x_in">Input value</param>
        /// <returns>~erfc(x)/2</returns>
        public static double CDDF_LogNormal(double x_in)
        {
            return (1 - UnconfinedFractureSet.erf(x_in)) / 2;
        }
        /// <summary>
        /// Winitzki's algorithm for calculating an approximate value for the inverse of half of the complementary error function erfc
        /// This gives the inverse of the cumulative density distribution function for a log normal distribution
        /// </summary>
        /// <param name="CumulativeDensity_in">Value of the cumulative density distribution function (between 0 and 1)</param>
        /// <returns>Value of x such that erfc(x)/2~CumulativeDensity_in</returns>
        public static double inverse_CDDF_LogNormal(double CumulativeDensity_in)
        {
            double erf = 1 - (CumulativeDensity_in * 2);
            return UnconfinedFractureSet.inverse_erf(erf);
        }
        /// <summary>
        /// Initial microfracture distribution function - at present only Power Law is implemented
        /// </summary>
        public InitialFractureDistribution InitialDistribution { get; private set; }
        /// <summary>
        /// Initial microfracture density coefficient B (/m3)
        /// </summary>
        public double CapB { get; private set; }
        /// <summary>
        /// Initial microfracture distribution coefficient c
        /// </summary>
        public double c_coefficient { get; private set; }
        /// <summary>
        /// Median initial microfracture radius - this is only used for the log-normal distribution function
        /// </summary>
        public double Median_uF_radius { get; private set; }
        /// <summary>
        /// Equal to log of the initial microfracture radius - this is only used for the log-normal distribution function
        /// </summary>
        public double M_term { get; private set; }
        /// <summary>
        /// Sqrt(2) * standard deviation of initial microfracture size S - this is only used for the log-normal distribution function
        /// </summary>
        public double Sqrt2_S { get; private set; }
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
        public double InitialP30(double radius)
        {
            double P30 = 0;
            switch (InitialDistribution)
            {
                case InitialFractureDistribution.PowerLaw:
                    P30 = CapB * Math.Pow(radius, -c_coefficient);
                    break;
                case InitialFractureDistribution.Exponential:
                    P30 = CapB * Math.Exp(-radius * c_coefficient);
                    break;
                case InitialFractureDistribution.LogNormal:
                    P30 = CapB * UnconfinedFractureSet.CDDF_LogNormal((Math.Log(radius) - M_term) / Sqrt2_S);
                    break;
                default:
                    break;
            }
            return P30;
        }
        /// <summary>
        /// Get the value of the initial microfracture radius for a specified limiting volumetric fracture density
        /// </summary>
        /// <param name="LFP30">Limiting volumetric fracture density, i.e. assuming no stress shadow deactivation</param>
        /// <returns>Minimum radius of the initial microfractures representing the specified limiting density</returns>
        public double InitialRadius(double LFP30)
        {
            double radius = 0;
            switch (InitialDistribution)
            {
                case InitialFractureDistribution.PowerLaw:
                    radius = Math.Pow(LFP30 / CapB, -1 / c_coefficient);
                    break;
                case InitialFractureDistribution.Exponential:
                    radius = -Math.Log(LFP30 / CapB) / c_coefficient;
                    break;
                case InitialFractureDistribution.LogNormal:
                    radius = Median_uF_radius * Math.Exp(Sqrt2_S * UnconfinedFractureSet.inverse_CDDF_LogNormal(LFP30 / CapB));
                    break;
                default:
                    break;
            }
            return radius;
        }
        /// <summary>
        /// Get the value of the initial microfracture radius for a specified nucleating explicit fracture
        /// </summary>
        /// <param name="Ln">Index number of the nucleating explicit fracture, i.e. assuming no stress shadow deactivation</param>
        /// <returns>Initial radius of the specified nucleating fracture</returns>
        public double InitialRadius(int Ln)
        {
            double implied_LFP30 = (double)Ln / gbc.Volume;
            return InitialRadius(Ln);
        }

        // Implicit fracture population data
        /// <summary>
        /// Number of rays comprising each fracture
        /// </summary>
        public ushort RaysPerFracture { get; private set; }
        /// <summary>
        /// Minimum radius for a fracture; this will be the length of the rays at nucleation
        /// </summary>
        public double MinimumFractureRadius { get; private set; }
        /// <summary>
        /// Maximum allowed radius for a ray; rays will stop propagating when they reach this length
        /// </summary>
        public double MaximumFractureRadius { get; private set; }
        /// <summary>
        /// Mean fracture ray length
        /// </summary>
        public double MeanFractureRadius { get { return Fractures.MeanRayLength; } }
        /// <summary>
        /// Object containing cumulative population data for all fractures
        /// </summary>
        private UnconfinedFractureData Fractures { get; set; }
        /// <summary>
        /// List of discrete unconfined fracture objects in XYZ coordinates - represents all unconfined fractures from this set that lie wholly or partially within this gridblock
        /// </summary>
        public List<UnconfinedFractureXYZ> LocalDFNUnconfinedFractures;
        /// <summary>
        /// Variable to hold maximum historic active mean linear fracture density; used to check if termination criteria are met, and updated when the CheckFractureDeactivation is called
        /// </summary>
        private double max_historic_a_UCFP32;
        /// <summary>
        /// Limiting RP30 (i.e. maximum potential RP30 with no stress shadow deactivation) at the last time a new implicit fracture datapoint was created
        /// </summary>
        private double previous_LRP30;
        /// <summary>
        /// Limiting count of explicit fractures in this gridblock (i.e. maximum potential number of nucleated fractures with no stress shadow deactivation) at the last time a new explicit fracture was created
        /// </summary>
        private int previous_Ln;
        /// <summary>
        /// Minimum RP30 value for a nucleating fracture datapoint - a new datapoint will not be created until the volumetric density of the nucleating fractures reaches this value
        /// </summary>
        private double min_NucleatingDatapoint_RP30;
        /// <summary>
        /// Minimum RP30 value for a growing fracture datapoint to be included when determining the maximum timestep duration based on increase in ray length
        /// </summary>
        private double min_GrowingDatapoint_RP30;

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
        /// Maximum number of consecutive attempts to nucleate a new explicit fracture before the set is considered incapable of nucleating new explicit fractures
        /// When this is exceeded the ExplicitNucleationActive flag is set to false
        /// </summary>
        private int MaxNucleationAttempts { get { return (int)(10d / gbc.PropControl.minimum_UCFClearZone_Volume); } }
        /// <summary>
        /// Counter for the number of consecutive failed explicit fracture nucleation attempts
        /// When this exceeds MaxNucleationAttempts the set will be considered incapable of nucleating new explicit fractures and the ExplicitNucleationActive flag is set to false
        /// </summary>
        private int failedNucleationAttempts;
        /// <summary>
        /// Increment the counter for the number of consecutive failed explicit fracture nucleation attempts; if necessary this will also set the ExplicitNucleationActive flag is set to false
        /// </summary>
        public void IncrementFailedNucleationAttemptCounter()
        {
            failedNucleationAttempts++;
        }
        /// <summary>
        /// Increment the counter for the number of consecutive failed explicit fracture nucleation attempts to zero
        /// </summary>
        public void ResetFailedNucleationAttemptCounter()
        {
            failedNucleationAttempts = 0;
        }
        /// <summary>
        /// Flag to specify whether the set is still capable of nucleating new explicit fractures in the current gridblock during DFN generation
        /// </summary>
        public bool ExplicitNucleationActive { get { return (failedNucleationAttempts < MaxNucleationAttempts); } }
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
        /// Factor related to fracture propagation rate during the current timestep: (A / |beta|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b!=2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
        /// </summary>
        /// <returns></returns>
        public double getFracturePropRateCoefficient() { return CurrentFractureData.gamma_InvBeta_M; }
        /// <summary>
        /// Factor related to fracture growth during the current timestep: -inv_gamma_factor * M_duration (b less than or equal to 2) or +inv_gamma_factor * M_duration (b greater than 2)
        /// </summary>
        /// <returns></returns>
        public double getFractureGrowthFactor() { return CurrentFractureData.gamma_Duration_M; }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including the current timestep (m^(1+b/2))
        /// </summary>
        /// <returns></returns>
        public double getCumGamma() { return CurrentFractureData.Cum_Gamma_M; }
        /// <summary>
        /// Return the volumetric density of all fully active rays during the current timestep (fractures/m3)
        /// </summary>
        /// <returns></returns>
        public double geta_RP30_M() { return CurrentFractureData.a_RP30_M; }
        /// <summary>
        /// Return the volumetric density of all restricted rays during the current timestep (fractures/m3)
        /// </summary>
        /// <returns></returns>
        public double getr_RP30_M() { return CurrentFractureData.r_RP30_M; }
        /// <summary>
        /// Return the volumetric density of all static rays terminated due to stress shadow interaction during the current timestep (fractures/m3)
        /// </summary>
        /// <returns></returns>
        public double getsII_RP30_M() { return CurrentFractureData.sII_RP30_M; }
        /// <summary>
        /// Return the volumetric density of all static rays terminated due to intersection during the current timestep (fractures/m3)
        /// </summary>
        /// <returns></returns>
        public double getsIJ_RP30_M() { return CurrentFractureData.sIJ_RP30_M; }
        /// <summary>
        /// Return the volumetric density of all static rays terminated due to exceeding the maximum radius during the current timestep (fractures/m3)
        /// </summary>
        /// <returns></returns>
        public double getsRMax_RP30_M() { return CurrentFractureData.sRMax_RP30_M; }
        /// <summary>
        /// Return the total volumetric density of all unconfined fractures during the current timestep (fractures/m3)
        /// </summary>
        /// <returns></returns>
        public double getTotalUCFP30() { return CurrentFractureData.Total_RP30_M / RaysPerFracture; }
        /// <summary>
        /// Return the volumetric density of all unconfined fractures from other fracture sets that terminate against fractures from this set, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTerminatingFractureDensity() { return CurrentFractureData.TerminatingFractureDensity_M; }
        /// <summary>
        /// Return the mean number of unconfined fractures from other fracture sets that terminate against a fracture from this set, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTerminatingFracturesPerUCF() { return (CurrentFractureData.Total_RP30_M > 0) ? CurrentFractureData.TerminatingFractureDensity_M / getTotalUCFP30() : 0; }
        /// <summary>
        /// Return the total linear density of all unconfined fractures during the current timestep (fractures/m)
        /// </summary>
        /// <returns></returns>
        public double getTotalUCFP32() { return CurrentFractureData.Total_RP32_M; }
        /// <summary>
        /// Return the maximum volumetric ratio of all unconfined fractures, not accounting for overlap, during the current timestep
        /// </summary>
        /// <returns></returns>
        public double getTotalUCFP33() { return CurrentFractureData.Total_RP33_M; }
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
        /// <summary>
        /// Inverse stress shadow volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the stress shadow of any fracture set, at end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getInverseStressShadowVolumeAllFS() { return CurrentFractureData.theta_allFS_M; }
        /// <summary>
        /// Clear zone volume for all fracture sets (including this one), i.e. cumulative probability that an initial microfracture from this fracture set does not lie in the exclusion zone of any fracture set, at end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getClearZoneVolumeAllFS() { return CurrentFractureData.theta_dashed_allFS_M; }
        /// <summary>
        /// Increment of displacement on the fracture resulting from an increment in the applied strain, at the end of the current timestep
        /// </summary>
        /// <returns></returns>
        public VectorXYZ getIncrementalDisplacement() { return CurrentFractureData.IncrementalDisplacement_M; }
        /// <summary>
        /// Increment of applied strain acting on the fracture, at the end of the current timestep
        /// </summary>
        /// <returns></returns>
        public VectorXYZ getIncrementalStrainOnFracture() { return CurrentFractureData.IncrementalStrainOnFracture_M; }
        /// <summary>
        /// Ratio of the maximum fracture stress shadow width to effective fracture radius, at the end of the current timestep
        /// </summary>
        /// <returns></returns>
        public double getStressShadowWidthRatio() { return CurrentFractureData.StressShadowWidthRatio_M; }
        /// <summary>
        /// Ratio of the maximum fracture stress shadow width to effective fracture radius for a fracture in another set as seen by a fracture in this set, at the end of the current timestep
        /// </summary>
        /// <param name="UFS_I">Reference to the fracture set containing the fracture with the stress shadow</param>
        /// <returns></returns>
        public double getStressShadowWidthRatio(UnconfinedFractureSet UFS_I) { return UFS_I.getUFSW_IJ(this) * CurrentFractureData.StressShadowWidthRatio_M; }

        // Functions to return data for previous timesteps from the PreviousFractureData list
        /// <summary>
        /// Flag for the stage of evolution of the fracture dip set at a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public FractureEvolutionStage getEvolutionStage(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getEvolutionStage(Timestep_M); }
        /// <summary>
        /// Effective normal stress on the fracture at the end of a specified previous timestep (Pa)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getFinalEffectiveNormalStress(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFinalNormalStress(Timestep_M); }
        /// <summary>
        /// Driving stress at the start of a specified previous timestep U (Pa) 
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getConstantDrivingStressU(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getConstantDrivingStressU(Timestep_M); }
        /// <summary>
        /// Rate of increase of driving stress during a specified previous timestep V (Pa/s)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getVariableDrivingStressV(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getVariableDrivingStressV(Timestep_M); }
        /// <summary>
        /// Weighted mean driving stress during a specified previous timestep (Pa)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getMeanDrivingStressSigmaD(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getMeanDrivingStressSigmaD(Timestep_M); }
        /// <summary>
        /// Driving stress at the end of a specified previous timestep (Pa)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getFinalDrivingStressSigmaD(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFinalDrivingStressSigmaD(Timestep_M); }
        /// <summary>
        /// Factor related to fracture propagation rate during a specified previous timestep: (A / |beta|) * ((2 sigmaD) / (Sqrt(Pi) * Kc)) ^ b (m^(1+b/2)/s) for b!=2; A * (4 * sigmaD^2) / (Pi * Kc^2) (m^2/s) for b=2
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getFracturePropRateCoefficient(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFPropagationRateFactor(Timestep_M); }
        /// <summary>
        /// Factor related to fracture growth during a specified previous timestep: -inv_gamma_factor * M_duration (b less than or equal to 2) or +inv_gamma_factor * M_duration (b greater than 2) in this gridblock for timestep M
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getFractureGrowthFactor(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFGrowthFactor(Timestep_M); }
        /// <summary>
        /// Get the cumulative fracture growth factor between the end of timestep M and the end of timestep N
        /// </summary>
        /// <param name="Timestep_N">Index number of the end timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <param name="Timestep_M">Index number of the start timestep</param>
        /// <returns></returns>
        public double getCumulativeFractureGrowthFactor(int Timestep_N, int Timestep_M) { if (Timestep_N < 0) Timestep_N = gbc.CurrentExplicitTimestep; return PreviousFractureData.getFGrowthFactor(Timestep_N, Timestep_M); }
        /// <summary>
        /// Cumulative value of gamma_InvBeta_K * K_duration in this gridblock for all timesteps K up to and including a specified previous timestep (m^(1+b/2))
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getCumGamma(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCum_Gamma_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all fully active rays at the end of a specified previous timestep (fractures/m3)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double geta_RP30_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.geta_RP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all restricted rays at the end of a specified previous timestep (fractures/m3)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getr_RP30_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getr_RP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all static rays terminated due to stress shadow interaction at the end of a specified previous timestep (fractures/m3)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getsII_RP30_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getsII_RP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all static rays terminated due to intersection at the end of a specified previous timestep (fractures/m3)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getsIJ_RP30_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getsIJ_RP30_M(Timestep_M); }
        /// <summary>
        /// Return the volumetric density of all static rays terminated due to exceeding the maximum radius at the end of a specified previous timestep (fractures/m3)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getsRMax_RP30_M(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getsRMax_RP30_M(Timestep_M); }
        /// <summary>
        /// Return the total volumetric density of all unconfined fractures at the end of a specified previous timestep (fractures/m3)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalUCFP30(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_RP30_M(Timestep_M) / RaysPerFracture; }
        /// <summary>
        /// Return the volumetric density of all unconfined fractures from other fracture sets that terminate against fractures from this set at the end of a specified previous timestep
        /// </summary>
        /// <returns></returns>
        public double getTerminatingFractureDensity(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTerminatingFractureDensity_M(Timestep_M); }
        /// <summary>
        /// Return the mean number of unconfined fractures from other fracture sets that terminate against a fracture from this set at the end of a specified previous timestep
        /// </summary>
        /// <returns></returns>
        public double getTerminatingFracturesPerUCF(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return (PreviousFractureData.getTotal_RP30_M(Timestep_M) > 0) ? PreviousFractureData.getTerminatingFractureDensity_M(Timestep_M) / getTotalUCFP30(Timestep_M) : 0; }
        /// <summary>
        /// Return the total linear density of all unconfined fractures at the end of a specified previous timestep (fractures/m)
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalUCFP32(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_RP32_M(Timestep_M); }
        /// <summary>
        /// Return the maximum volumetric ratio of all unconfined fractures, not accounting for overlap, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalUCFP33(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getTotal_RP33_M(Timestep_M); }
        /// <summary>
        /// Return the total porosity of all unconfined fractures that were present at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalUCFPorosity(int Timestep_M)
        {
            return getTotalUCFPorosity(gbc.PropControl.FractureApertureControl, Timestep_M);
        }
        /// <summary>
        /// Return the total porosity of all unconfined fractures that were present at the end of a specified previous timestep, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getTotalUCFPorosity(FractureApertureType ApertureControl, int Timestep_M)
        {
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;
            double output;

            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = PreviousFractureData.getTotal_RP32_M(Timestep_M) * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = PreviousFractureData.getTotal_RP33_M(Timestep_M) * (SizeDependentApertureMultiplier / 2);
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = PreviousFractureData.getTotal_RP33_M(Timestep_M) * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = PreviousFractureData.getTotal_RP32_M(Timestep_M) * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }
        /// <summary>
        /// Inverse stress shadow volume (1-psi), i.e. cumulative probability that an initial microfracture in this gridblock is still active, during a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getInverseStressShadowVolume(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeTheta(Timestep_M); }
        /// <summary>
        /// Clear zone volume (1 - Chi), i.e. cumulative probability that a macrofracture nucleating in this gridblock does not lie in a stress shadow exclusion zone, at end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getClearZoneVolume(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getCumulativeThetaDashed(Timestep_M); }
        /// <summary>
        /// Increment of displacement on the fracture resulting from an increment in the applied strain, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public VectorXYZ getIncrementalDisplacement(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getIncrementalDisplacement_M(Timestep_M); }
        /// <summary>
        /// Increment of applied strain acting on the fracture, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public VectorXYZ getIncrementalStrainOnFracture(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getIncrementalStrainOnFracture_M(Timestep_M); }
        /// <summary>
        /// Ratio of the maximum fracture stress shadow width to effective fracture radius, at the end of a specified previous timestep
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getStressShadowWidthRatio(int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return PreviousFractureData.getStressShadowWidthRatio_M(Timestep_M); }
        /// <summary>
        /// Ratio of the maximum fracture stress shadow width to effective fracture radius for a fracture in another set as seen by a fracture in this set, at the end of a specified previous timestep
        /// </summary>
        /// <param name="UFS_I">Reference to the fracture set containing the fracture with the stress shadow</param>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns></returns>
        public double getStressShadowWidthRatio(UnconfinedFractureSet UFS_I, int Timestep_M) { if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep; return UFS_I.getUFSW_IJ(this, Timestep_M) * PreviousFractureData.getStressShadowWidthRatio_M(Timestep_M); }
        /// <summary>
        /// Get the time at which the fracture set becomes deactivated
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if the fracture set was never active: if true, will return Nan; if false, will return 0</param>
        /// <returns>Deactivation time of fracture set; will return zero or NaN if the fracture set was never active</returns>
        public double getFinalActiveTime(bool ReturnNanForUndefined)
        {
            // Get time units and unit conversion modifier for output time data if not in SI units
            double timeUnits_Modifier = gbc.PropControl.getTimeUnitsModifier();

            return PreviousFractureData.getFinalActiveTime(ReturnNanForUndefined) / timeUnits_Modifier;
        }

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
                output.Add(dp.dRP30);
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
        /// <returns>List of the dP33 factors; these values must be multiplied by 4/3 pi/No rays per fracture to get the true P33</returns>
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
        /// Total volumetric density of fully active fracture rays
        /// </summary>
        /// <returns></returns>
        public double a_UCRP30_total() { return Fractures.a_RP30_total; }
        /// <summary>
        /// Total volumetric density of restricted fracture rays
        /// </summary>
        /// <returns></returns>
        public double r_UCRP30_total() { return Fractures.r_RP30_total; }
        /// <summary>
        /// Total volumetric density of fracture rays deactivated due to stress shadow interaction
        /// </summary>
        /// <returns></returns>
        public double sII_UCRP30_total() { return Fractures.sII_RP30_total; }
        /// <summary>
        /// Total volumetric density of fracture rays deactivated due to intersection
        /// </summary>
        /// <returns></returns>
        public double sIJ_UCRP30_total() { return Fractures.sIJ_RP30_total; }
        /// <summary>
        /// Total volumetric density of fracture rays deactivated due to reaching the maximum radius
        /// </summary>
        /// <returns></returns>
        public double sMR_UCRP30_total() { return Fractures.sMR_RP30_total; }
        /// <summary>
        /// Total volumetric density of fractures in the set
        /// </summary>
        /// <returns></returns>
        public double UCFP30_total() { return Fractures.FP30_total; }
        /// <summary>
        /// Total mean linear density of fully active fracture rays
        /// </summary>
        /// <returns></returns>
        public double a_UCRP32_total() { return Fractures.a_RP32_total; }
        /// <summary>
        /// Total mean linear density of restricted fracture rays
        /// </summary>
        /// <returns></returns>
        public double r_UCRP32_total() { return Fractures.r_RP32_total; }
        /// <summary>
        /// Total mean linear density of fracture rays deactivated due to stress shadow interaction
        /// </summary>
        /// <returns></returns>
        public double sII_UCRP32_total() { return Fractures.sII_RP32_total; }
        /// <summary>
        /// Total mean linear density of fracture rays deactivated due to intersection
        /// </summary>
        /// <returns></returns>
        public double sIJ_UCRP32_total() { return Fractures.sIJ_RP32_total; }
        /// <summary>
        /// Total mean linear density of fracture rays deactivated due to reaching the maximum radius
        /// </summary>
        /// <returns></returns>
        public double sMR_UCRP32_total() { return Fractures.sMR_RP32_total; }
        /// <summary>
        /// Total mean linear density of fractures in the set
        /// </summary>
        /// <returns></returns>
        public double UCFP32_total() { return Fractures.FP32_total; }
        /// <summary>
        /// Get a piecewise cumulative P30 density distribution function for the specified ray type
        /// </summary>
        /// <param name="indexRadii">List of index radii for the piecewise cumulative density distribution function</param>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of cumulative P30 density values for the specified ray type; each item in the list will represent the cumulative P30 for the corresponding index radius in the input list</returns>
        public List<double> GetCumulativeUCRP30Values(List<double> indexRadii, RayPropagationStatus rayType)
        {
            // Sort the incoming list in reverse order
            indexRadii.Sort();
            indexRadii.Reverse();

            // Create an output list
            List<double> cumP30List = new List<double>();

            // Create the index pointer and cumulative total for the P30 datapoints
            int datapointNo = 0;
            double cumP30 = 0;

            // Get a reference to the required datapoint list
            List<ImplicitFracturePopulationDatapoint> datapoints = Fractures.fracturePopulationDatapoints[rayType];

            // Loop through all the index points in the incoming list, increment the cumulative P30 total and add a corresponding datapoint to the output list
            foreach (double nextRadius in indexRadii)
            {
                while ((datapoints.Count > datapointNo) && (datapoints[datapointNo].RayLength >= nextRadius))
                {
                    cumP30 += datapoints[datapointNo].dRP30;
                    datapointNo++;
                }
                cumP30List.Add(cumP30);
            }

            // Reverse the index list and the final output list so they are in ascending order, and return the final output list
            indexRadii.Reverse();
            cumP30List.Reverse();
            return cumP30List;
        }
        /// <summary>
        /// Get a piecewise cumulative P32 density distribution function for the specified ray type
        /// </summary>
        /// <param name="indexRadii">List of index radii for the piecewise cumulative density distribution function</param>
        /// <param name="rayType">Specified ray type</param>
        /// <returns>List of cumulative P32 density values for the specified ray type; each item in the list will represent the cumulative P32 for the corresponding index radius in the input list</returns>
        public List<double> GetCumulativeUCRP32Values(List<double> indexRadii, RayPropagationStatus rayType)
        {
            // Sort the incoming list in reverse order
            indexRadii.Sort();
            indexRadii.Reverse();

            // Create an output list and P32 multiplier
            List<double> cumP32List = new List<double>();
            double P32multiplier = Math.PI / (double)RaysPerFracture;

            // Create the index pointer and cumulative total for the P32 datapoints
            int datapointNo = 0;
            double cumP32factor = 0;

            // Get a reference to the required datapoint list
            List<ImplicitFracturePopulationDatapoint> datapoints = Fractures.fracturePopulationDatapoints[rayType];

            // Loop through all the index points in the incoming list, increment the cumulative P32 total and add a corresponding datapoint to the output list
            foreach (double nextRadius in indexRadii)
            {
                while ((datapoints.Count > datapointNo) && (datapoints[datapointNo].RayLength >= nextRadius))
                {
                    cumP32factor += datapoints[datapointNo].dP32factor;
                    datapointNo++;
                }
                cumP32List.Add(cumP32factor * P32multiplier);
            }

            // Reverse the index list and the final output list so they are in ascending order, and return the final output list
            indexRadii.Reverse();
            cumP32List.Reverse();
            return cumP32List;
        }
#if DEBUG
        /// <summary>
        /// Total volumetric ratio of fully active fracture rays
        /// </summary>
        /// <returns></returns>
        public double a_UCRP33_total() { return Fractures.a_RP33_total; }
        /// <summary>
        /// Total volumetric ratio of restricted fracture rays
        /// </summary>
        /// <returns></returns>
        public double r_UCRP33_total() { return Fractures.r_RP33_total; }
        /// <summary>
        /// Total volumetric ratio of fracture rays deactivated due to stress shadow interaction
        /// This value does not take into account that the stress shadows around static fracture rays may overlap; it will therefore be an overestimate of the true value
        /// </summary>
        /// <returns></returns>
        public double sII_UCRP33_total() { return Fractures.sII_RP33_total; }
        /// <summary>
        /// Total volumetric ratio of fracture rays deactivated due to intersection
        /// This value does not take into account that the stress shadows around static fracture rays may overlap; it will therefore be an overestimate of the true value
        /// </summary>
        /// <returns></returns>
        public double sIJ_UCRP33_total() { return Fractures.sIJ_RP33_total; }
        /// <summary>
        /// Total volumetric ratio of fracture rays deactivated due to reaching the maximum radius
        /// This value does not take into account that the stress shadows around static fracture rays may overlap; it will therefore be an overestimate of the true value
        /// </summary>
        /// <returns></returns>
        public double sMR_UCRP33_total() { return Fractures.sMR_RP33_total; }
        /// <summary>
        /// Maximum volumetric ratio of all fractures
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        /// <returns></returns>
        public double UCFP33_total() { return Fractures.FP33_total; }
        /// <summary>
        /// Total fracture stress shadow volume
        /// This value takes into account stress shadow overlap
        /// It will therefore be less than FP33 * (W/r)
        /// </summary>
        /// <returns></returns>
        public double StressShadowVolume_total() { return Fractures.StressShadowVolume_total; }
#endif

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
        /// Maximum aperture of a fracture of a specified radius at the end of a previous timestep
        /// </summary>
        /// <param name="radius">Fracture radius (m)</param>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Maximum fracture aperture (m)</returns>
        public double getMaximumFractureAperture(double radius, int timestep)
        {
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    // Mean aperture 
                    output = (4 / Math.PI) * radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -PreviousFractureData.getFinalNormalStress(timestep);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (8 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = PreviousFractureData.getFinalNormalStress(timestep);
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
        /// Mean aperture of a fracture of a specified radius at the end of a previous timestep
        /// </summary>
        /// <param name="radius">Fracture radius (m)</param>
        /// <param name="timestep">Index for a previous timestep</param>
        /// <returns>Mean fracture aperture (m)</returns>
        public double getMeanFractureAperture(double radius, int timestep)
        {
            double output;

            switch (gbc.PropControl.FractureApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    // Mean aperture 
                    output = radius * SizeDependentApertureMultiplier;
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -PreviousFractureData.getFinalNormalStress(timestep);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = radius * gbc.MechProps.DynamicApertureMultiplier * (16 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (3 * Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = PreviousFractureData.getFinalNormalStress(timestep);
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
        /// <param name="CurrentStress">Current effective stress tensor</param>
        /// <returns>True if the shear displacement vector has changed, false if it has not</returns>
        public bool RecalculateElasticResponse(Tensor2S CurrentStress)
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
            bool previous_sigmaneff_negative = (CurrentFractureData.SigmaNeff_Const_M <= PreviousFractureData.MaxDrivingStressRoundingError);
            bool sigmaneff_negative = (normalStressMagnitude <= PreviousFractureData.MaxDrivingStressRoundingError);
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

            // Recalculate the compliance tensor base, the fracture mode factors and the incremental displacement on the fracture
            // This is only necessary if either:
            // - the fracture mode has changed (from dilatant to shear or vice versa)
            // - the driving stress has changed from positive to negative, or vice versa, so fractures can now / can no longer accommodate elastic strain
            // - the shear displacement vector has changed, for shear fractures (the compliance tensor is independent of the shear displacement vector for dilatant fractures so this does not apply for these)
            bool fractureStateChanged = sigmaneff_changed || sigmad_changed || (stressVectorChanged && !sigmaneff_negative);
            if (fractureStateChanged)
            {
                RecalculateComplianceTensorBase(sigmaneff_negative, sigmad_positive);
                RecalculateIncrementalDisplacement();
            }

            return fractureStateChanged;
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

        // Fracture porosity values
        /// <summary>
        /// Total porosity of all unconfined fractures
        /// </summary>
        /// <returns></returns>
        public double Total_UCF_Porosity()
        {
            return Total_UCF_Porosity(gbc.PropControl.FractureApertureControl);
        }
        /// <summary>
        /// Total porosity of all unconfined fractures, based on specified method for determining fracture aperture
        /// </summary>
        /// <param name="ApertureControl">Method for determining fracture aperture</param>
        /// <returns></returns>
        public double Total_UCF_Porosity(FractureApertureType ApertureControl)
        {
            double output;

            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            switch (ApertureControl)
            {
                case FractureApertureType.Uniform:
                    output = CurrentFractureData.Total_RP32_M * UniformAperture;
                    break;
                case FractureApertureType.SizeDependent:
                    output = CurrentFractureData.Total_RP33_M * (SizeDependentApertureMultiplier / 2);
                    break;
                case FractureApertureType.Dynamic:
                    double tensile_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (tensile_sigmaNeff < 0) tensile_sigmaNeff = 0;
                    output = CurrentFractureData.Total_RP33_M * gbc.MechProps.DynamicApertureMultiplier * (2 * tensile_sigmaNeff * (1 - Math.Pow(gbc.MechProps.Nu_r, 2))) / (Math.PI * gbc.MechProps.E_r);
                    break;
                case FractureApertureType.BartonBandis:
                    double compressive_sigmaNeff = -(usePresentDayStress ? PresentDaySigmaNeff : CurrentFractureData.SigmaNeff_Final_M);
                    if (compressive_sigmaNeff < 0) compressive_sigmaNeff = 0;
                    output = CurrentFractureData.Total_RP32_M * BartonBandisAperture(compressive_sigmaNeff);
                    break;
                default:
                    output = 0;
                    break;
            }

            return output;
        }

        // Fracture permeability tensors
        /// <summary>
        /// Get the uncorrected permeability tensor for all current unconfined fractures in this set
        /// This is based on the Oda (1985) model and assumes fractures of infinite size and connectivity
        /// </summary>
        /// <returns>Tensor2S object representing the uncorrected unconfined fracture permeability</returns>
        public Tensor2S Total_UCF_Permeability()
        {
            return Total_UCF_Permeability(-1);
        }
        /// <summary>
        /// Get the uncorrected permeability tensor for all unconfined fractures in this dipset, at the end of a specified previous timestep
        /// This is based on the Oda (1985) model and assumes fractures of infinite size and connectivity
        /// </summary>
        /// <param name="Timestep_M">Index number of the specified timestep</param>
        /// <returns>Tensor2S object representing the uncorrected unconfined fracture permeability</returns>
        public Tensor2S Total_UCF_Permeability(int Timestep_M)
        {
            // Since the UCF implicit fracture population arrays are cleared at the end of the Gridblock.CalculateFractureData() function to save space, 
            // we must always take data from the FractureCalculationData list
            bool useCurrentDensityData = (Timestep_M < 0);
            bool useCurrentApertureData = useCurrentDensityData || usePresentDayStress;

            double geometryMultiplier = 1d / 12d;
            double apertureMultiplier;
            double densityMultiplier;
            switch (gbc.PropControl.FractureApertureControl)
            {
                // In the Uniform and Barton Bandis fracture aperture scenarios, aperture is uniform across the fracture
                // The permeability will therefore be proportional to the cube of the mean aperture
                case FractureApertureType.Uniform:
                case FractureApertureType.BartonBandis:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMeanFractureAperture(1) : getMeanFractureAperture(1, Timestep_M), 3);
                    densityMultiplier = useCurrentDensityData ? getTotalUCFP32() : getTotalUCFP32(Timestep_M);
                    break;
                // In the Size Dependent and Dynamic fracture aperture scenarios, aperture follows an elliptical profile
                // The aperture multiplier is calculated by integrating the cube of the local aperture across every fracture
                // However the aperture calculated by these methods is likely to be unrealistically large for unconfined fractures so is not recommended for permeability calculations
                // We therefore do not calculate the P35 value, which is required to calculate permeability exactly for a fracture with an elliptical profile
                // Instead we approximate aperture from mean fracture radius
                case FractureApertureType.SizeDependent:
                case FractureApertureType.Dynamic:
                    apertureMultiplier = Math.Pow(useCurrentApertureData ? getMaximumFractureAperture(1) : getMaximumFractureAperture(1, Timestep_M), 3);
                    densityMultiplier = useCurrentDensityData ? getTotalUCFP32() : getTotalUCFP32(Timestep_M); //(useCurrentDensityData ? UCFP35_total() : getTotalUCFP35(Timestep_M)) / 8;
                    break;
                // Aperture is not defined
                default:
                    apertureMultiplier = 0;
                    densityMultiplier = 0;
                    break;
            }

            Tensor2S permTensor = Tensor2S.BiaxialTensor(normalVector, geometryMultiplier * apertureMultiplier * densityMultiplier);
            return permTensor;
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
        /// Recalculate the applied strain components acting on the fractures, for a specified strain or strain rate tensor
        /// </summary>
        /// <param name="AppliedStrainTensor">Current strain or strain rate tensor</param>
        public void RecalculateStrainRatios(Tensor2S AppliedStrainTensor)
        {
            CurrentFractureData.IncrementalStrainOnFracture_M = AppliedStrainTensor * normalVector;
            RecalculateIncrementalDisplacement();
        }
        /// <summary>
        /// Recalculate the incremental displacement and the incremental displacement/applied strain ratio controlling stress shadow width, based the current fracture mode factors and incremental applied strain vector
        /// </summary>
        private void RecalculateIncrementalDisplacement()
        {
            // Get the components of the tensor for the applied stress increment
            VectorXYZ strainOnFracture = CurrentFractureData.IncrementalStrainOnFracture_M;
            double effd = normalVector & strainOnFracture;
            double efwd = dipVector & strainOnFracture;
            double efsd = strikeVector & strainOnFracture;

            // Set the strain ratios to zero if they are small - this will avoid rounding errors
            double emax = effd + efwd + efsd;
            if ((float)(emax + effd) == (float)emax)
                effd = 0;
            if ((float)(emax + efwd) == (float)emax)
                efwd = 0;
            if ((float)(emax + efsd) == (float)emax)
                efsd = 0;

            // Get the components of the fracture displacement vector
            // The shear components Dw and Ds are divided by 2 so they are equivalent to the fd and fs components of the fracture shear tensor
            double Df = Mff * effd;
            double Dw_half = (Mfw * effd) + (Mww * efwd);
            double Ds_half = (Mfs * effd) + (Mss * efsd);

            // Recalculate the incremental displacement vector in XYZ coordinates
            // This is used to calculate the ratio of incremental displacement to applied strain on the fracture
            CurrentFractureData.IncrementalDisplacement_M = (Df * normalVector) + (Dw_half * dipVector) + (Ds_half * strikeVector);

            // Update the ratio of incremental displacement to applied strain on the fracture
            // This is used to calculate the stress shadow width
            De_ee_Ratio = CurrentFractureData.Displacement_Strain_Ratio;
        }
        /// <summary>
        /// Return the current ratio of the incremental displacement on this fracture set I to the incremental displacement on fracture set J, projected onto the applied strain vector for J; i.e. DI.eJ / DJ.eJ
        /// </summary>
        /// <param name="J">Reference to unconfined fracture set J</param>
        /// <returns>The ratio DI.eJ / DJ.eJ, or 0 if DJ.eJ is zero (i.e. the applied strain on J is 0)</returns>
        public double getUFSW_IJ(UnconfinedFractureSet J)
        {
            // Get the displacement increment and applied stress increment on fracture set J
            VectorXYZ displacementIncrement_J = J.CurrentFractureData.IncrementalDisplacement_M;
            VectorXYZ strainOnFractureJ = J.CurrentFractureData.IncrementalStrainOnFracture_M;

            // Calculate DIeJ and DJeJ
            double DIeJ = CurrentFractureData.IncrementalDisplacement_M & strainOnFractureJ;
            double DJeJ = displacementIncrement_J & strainOnFractureJ;

            if (DJeJ > 0)
                return Math.Abs(DIeJ / DJeJ);
            else
                return 0;
        }
        /// <summary>
        /// Return the ratio of the incremental displacement on this fracture set I to the incremental displacement on fracture set J, projected onto the applied strain vector for J, in a specified timestep M; i.e. DI.eJ / DJ.eJ
        /// </summary>
        /// <param name="J">Reference to unconfined fracture set J</param>
        /// <param name="Timestep_M">Index number of the specified timestep; set to -1 to use the current timestep in the explicit fracture calculation</param>
        /// <returns>The ratio DI.eJ / DJ.eJ, or 0 if DJ.eJ is zero (i.e. the applied strain on J is 0)</returns>
        public double getUFSW_IJ(UnconfinedFractureSet J, int Timestep_M)
        {
            if (Timestep_M < 0) Timestep_M = gbc.CurrentExplicitTimestep;

            // Get the displacement increment and applied stress increment on fracture set J in timestep M
            VectorXYZ displacementIncrement_J = J.PreviousFractureData.getIncrementalDisplacement_M(Timestep_M);
            VectorXYZ strainOnFractureJ = J.PreviousFractureData.getIncrementalStrainOnFracture_M(Timestep_M);

            // Get the displacement increment on this fracture set I in timestep M
            VectorXYZ displacementIncrement_I = PreviousFractureData.getIncrementalDisplacement_M(Timestep_M);

            // Calculate DIeJ and DJeJ
            double DIeJ = displacementIncrement_I & strainOnFractureJ;
            double DJeJ = displacementIncrement_J & strainOnFractureJ;

            if (DJeJ > 0)
                return Math.Abs(DIeJ / DJeJ);
            else
                return 0;
        }
        /// <summary>
        /// Ratio of incremental displacement on the fracture to applied strain; equal to D.e/e.e
        /// </summary>
        private double De_ee_Ratio { get; set; }
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
        /// Ratio of maximum stress shadow width to effective fracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Max_F_StressShadowWidthRatio
        {
            get { return De_ee_Ratio * (8 / Math.PI); }
        }
        /// <summary>
        /// Ratio of mean stress shadow width to effective fracture radius - returns a value regardless of the FractureDistribution case
        /// </summary>
        /// <returns></returns>
        public double Mean_F_StressShadowWidthRatio
        {
            get { return De_ee_Ratio * (16 / (3 * Math.PI)); }
        }
        // Functions to set stress shadow width ratio
        /// <summary>
        /// Set the new stress shadow width ratios
        /// </summary>
        /// <returns>True if the stress shadow width ratio of this fracture set has changed, false if the stress shadow width ratio is unchanged</returns>
        public bool setStressShadowWidthData()
        {
            // Get the current stress shadow width ratio
            // This will depend on the stress distribution scenario
            double current_StressShadowWidthRatio;
            switch (FractureDistribution)
            {
                // There are no stress shadows in the evenly distributed stress scenario
                case StressDistribution.EvenlyDistributedStress:
                    current_StressShadowWidthRatio = 0;
                    break;
                // Stress shadow widths are proportional to the effective fracture radius in the stress shadow scenario
                case StressDistribution.StressShadow:
                // The ductile boundary scenario is not valid for unconfined fractures, so we default to the stress shadow scenario
                case StressDistribution.DuctileBoundary:
                    current_StressShadowWidthRatio = Max_F_StressShadowWidthRatio;
                    break;
                // By default assume no stress shadows
                default:
                    current_StressShadowWidthRatio = 0;
                    break;
            }

            // If the stress shadow width has changed, update the CurrentFractureData object and recalculate the stress shadow volume for each datapoint
            double previous_StressShadowWidthRatio = CurrentFractureData.StressShadowWidthRatio_M;
            bool stressShadowWidthChanged = ((float)current_StressShadowWidthRatio != (float)previous_StressShadowWidthRatio);
            if (stressShadowWidthChanged)
            {
                // Calculate a multiplier for the stress shadow volume around each datapoint to represent the change in stress shadow width
                // NB This will be positive for growing stress shadows and negative for shrinking stress shadows
                double dW_Wi = (previous_StressShadowWidthRatio > 0) ? (current_StressShadowWidthRatio - previous_StressShadowWidthRatio) / previous_StressShadowWidthRatio : 0;

                // Recalculate the stress shadow volume associated with each datapoint in the UnconfinedFractureData object
                if (dW_Wi != 0)
                    Fractures.CalculateStressShadowIncrementsFromStressShadowWidthChange(dW_Wi);

                // Recalculate the total stress shadow volume for the implicit fracture data
                Fractures.RecalculateStressShadowVolumeData();

                // Update the stress shadow widths in the CurrentFractureData object; the stress shadow volume will be updated later
                CurrentFractureData.SetStressShadowWidth(current_StressShadowWidthRatio);

                // Also revert any residual active sets to growing, since the deactivation probabilities may have significantly reduced
                // If the deactivation probabilities have not significantly reduced, the fracture dipsets will revert to Residual Active when the calculateTotalMacrofracturePopulation() function is called
                if (getEvolutionStage() == FractureEvolutionStage.ResidualActivity)
                    CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Growing);
            }

            // Return the flag for stress shadow widths changed
            return stressShadowWidthChanged;
        }

        // Stress shadow and exclusion zone data
        /// <summary>
        /// Get the total clear zone volume seen by as seen by rays represented by a specified datapoint, taking account of overlap and the effective radius of all fractures
        /// </summary>
        /// <param name="DatapointToCheck">Implicit fracture population datapoint representing the dimensions of the specified fracture rays</param>
        /// <param name="InverseStressShadowVolume">Reference variable to return the inverse stress shadow volume as well, if this is required</param>
        /// <returns>Clear zone volume seen by the specified fracture; this is the inverse of the exclusion zone volume seen by the fracture</returns>
        public double getStressShadowClearZoneVolume(ImplicitFracturePopulationDatapoint DatapointToCheck, out double InverseStressShadowVolume)
        {
            return getStressShadowClearZoneVolume(DatapointToCheck.RayLength, DatapointToCheck.EffectiveRayLength, out InverseStressShadowVolume);
        }
        /// <summary>
        /// Get the total clear zone volume and inverse stress shadow volume seen by rays of specified dimensions, taking account of stress shadow exclusion zone overlap
        /// </summary>
        /// <param name="dtc_rayLength">Length of the specified fracture rays</param>
        /// <param name="dtc_effectiveRaylength">Effective length of the specified fracture rays</param>
        /// <param name="InverseStressShadowVolume">Reference variable to return the inverse stress shadow volume as well, if this is required</param>
        /// <returns>Clear zone volume seen by rays represented by a specified datapoint; this is the volume in which the centre of the specified fracture could be placed without its stress shadow overlapping the stress shadow of any other fractures</returns>
        public double getStressShadowClearZoneVolume(double dtc_rayLength, double dtc_effectiveRaylength, out double InverseStressShadowVolume)
        {
            return getStressShadowClearZoneVolume(dtc_rayLength, dtc_effectiveRaylength, 1, out InverseStressShadowVolume);
        }
        /// <summary>
        /// Get the total clear zone volume and inverse stress shadow volume seen by rays of specified dimensions from any fracture set, taking account of stress shadow exclusion zone overlap
        /// </summary>
        /// <param name="dtc_rayLength">Length of the specified fracture rays</param>
        /// <param name="dtc_effectiveRaylength">Effective length of the specified fracture rays</param>
        /// <param name="stressShadowWidthMultiplier">Multiplier to take account of cross fault set stress shadows</param>
        /// <param name="InverseStressShadowVolume">Reference variable to return the inverse stress shadow volume as well, if this is required</param>
        /// <returns>Clear zone volume seen by rays represented by a specified datapoint; this is the volume in which the centre of the specified fracture could be placed without its stress shadow overlapping the stress shadow of any other fractures</returns>
        public double getStressShadowClearZoneVolume(double dtc_rayLength, double dtc_effectiveRaylength, double stressShadowWidthMultiplier, out double InverseStressShadowVolume)
        {
            // Cache the ray length, stress shadow width and minimum stress shadow deactivation radius of the specified datapoint locally
            double minStressShadowDeactivationRadius = dtc_effectiveRaylength * gbc.PropControl.MinStressShadowDeactivationRatio;
            double stressShadowHalfWidthRatio = Max_F_StressShadowWidthRatio / 2;
            double dtc_stressShadowHalfWidth = dtc_effectiveRaylength * stressShadowHalfWidthRatio;

            // Calculate the total stress shadow volume and the exclusive outer exclusion zone volume of fractures that can deactivate this fracture
            // The outer exclusion zone volume is the volume within the exclusion zone volume but outside the stress shadow volume of every other fracture
            // The outer exclusion zones of individual fractures can overlap with each other and with the stress shadows of the fractures - however first we must calculate the sum of the outer exclusion zone volumes of all fractures ignoring overlap
            double stressShadowVolume = 0;
            double exclusiveOuterExclusionZoneVolume = 0;
            // Loop through each ray propagation status
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Loop through each datapoint in the population data array
                foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[status])
                {
                    // Check if it is large enough to deactivate the current fracture - if not we can ignore it
                    if (datapoint.EffectiveRayLength >= minStressShadowDeactivationRadius)
                    {
                        stressShadowVolume += datapoint.StressShadowVolume;
                        exclusiveOuterExclusionZoneVolume += (datapoint.dP33ShellFactor(dtc_rayLength, dtc_stressShadowHalfWidth) * (4d / 3d) * Math.PI / (double)RaysPerFracture);
                    }
                }
            }

            // Calculate the inverse stress shadow volume, and the clear zone volume taking into account overlap of the outer shells, and also taking into account the multiplier for cross fault set stress shadows
            // The multiplier for cross fault set stress shadows is applied only to the stress shadow volume, not to the outer exclusion zone volume
            // This is because the outer exclusion zone volume represents the stress shadow around the fracture being tested, not around a fracture from a different set
            // NB This will not be exact as the multiplier for cross fault set stress shadows (assuming it is < 1) will mean that some of the outer exclusion zone volume lies within the stress shadow volume so cannot overlap
            InverseStressShadowVolume = 1 - (stressShadowVolume * stressShadowWidthMultiplier);
            double clearZoneVolume = InverseStressShadowVolume * Math.Exp(-exclusiveOuterExclusionZoneVolume);

            // Return the clear zone volume
            return clearZoneVolume;
        }
        /// <summary>
        /// Get the ratio of the total volume not in an interaction or exclusion zone around any fracture segment in this set to the total clear zone volume as seen by rays represented by a specified datapoint, taking account of stress shadow interaction zone overlap
        /// This represents the volume in which the centre of the specified fracture could be placed without experiencing stress shadow interaction in the current timestep
        /// This is equivalent to the expected probability that an active ray that will not be deactivated due to stress shadow interaction in this timestep (PhiII_M)
        /// </summary>
        /// <param name="DatapointToCheck">Implicit fracture population datapoint representing the dimensions of the specified fracture rays</param>
        /// <returns>Ratio of the total volume not in an interaction or exclusion zone around any fracture segment in this set to the total clear zone volume as seen by rays represented by the specified datapoint (Phi_II)</returns>
        public double getStressShadowNonInteractionVolumeRatio(ImplicitFracturePopulationDatapoint DatapointToCheck)
        {
            // Cache the ray length and increment, stress shadow width and increment and minimum stress shadow deactivation radius of the specified datapoint locally
            double minStressShadowDeactivationRadius = DatapointToCheck.EffectiveRayLength * gbc.PropControl.MinStressShadowDeactivationRatio;
            double dtc_rayLength = DatapointToCheck.RayLength;
            double stressShadowHalfWidthRatio = Max_F_StressShadowWidthRatio / 2;
            double dtc_stressShadowHalfWidth = DatapointToCheck.EffectiveRayLength * stressShadowHalfWidthRatio;
            double dtc_rayLengthIncrement = DatapointToCheck.ActualRayLengthIncrement;
            double dtc_stressShadowHalfWidthIncrement = DatapointToCheck.EffectiveRayLengthIncrement * stressShadowHalfWidthRatio;

            // The increment shell volume is a shell of width equal to the combined radius and stress shadow increment of both fractures in the coming timestep
            // This is the volume in which the centre of the specified fracture could be placed without experiencing stress shadow interaction in the current timestep
            // Since the origin of an active ray cannot lie within a stress shadow exclusion zone, the ratio of the increment shell volume to the clear zone volume gives the expected proportion of active rays that will be deactivated due to stress shadow interaction in this timestep (PhiII_M)
            // However the increment shells of individual fractures can overlap with each other and with the outer exclusion zones and stress shadows of the fractures
            //
            // This can be expressed as                (TIEZV - TEZV)       (1 - SSV) exp(-ExOEZV) (1 - exp(-ExISV))
            //                           PhiII_M = 1 - -------------- = 1 - ---------------------------------------- = exp(-ExISV)
            //                                           (1 - TEZV)                  (1 - SSV) exp(-ExOEZV)
            //
            // TIEZV = total interaction and exclusion zone volume, accounting for overlap
            // TEZV = total exclusion zone volume, accounting for overlap
            // SSV = total stress shadow volume
            // ExOEZV = sum of outer exclusion zone volumes around each fracture, ignoring overlap
            // ExISV = sum of increment shell zone volumes around each fracture, ignoring overlap
            //
            // Because the terms representing the stress shadow and outer deactivation zone volume cancel each other out in the ratio, it is not necessary to calculate these
            // We therefore only need to calculate the exclusive increment shell volume of fractures that can deactivate this fracture (ExISV)
            double exclusiveIncrementShellVolume = 0;
            // Loop through each ray propagation status
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Loop through each datapoint in the population data array
                foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[status])
                {
                    // Check if it is large enough to deactivate the current fracture - if not we can ignore it
                    if (datapoint.EffectiveRayLength >= minStressShadowDeactivationRadius)
                    {
                        // Cache the combined ray length and stress shadow width of the specified and the current datapoints
                        double combined_rayLength = dtc_rayLength + datapoint.RayLength;
                        double combined_stressShadowHalfWidth = dtc_stressShadowHalfWidth + (datapoint.EffectiveRayLength * stressShadowHalfWidthRatio);

                        // Cache the combined ray length increment and stress shadow width increment of the specified and the current datapoints
                        double combined_rayLengthIncrement = dtc_rayLengthIncrement + datapoint.ActualRayLengthIncrement;
                        double combined_stressShadowHalfWidthIncrement = dtc_stressShadowHalfWidthIncrement + (datapoint.EffectiveRayLengthIncrement * stressShadowHalfWidthRatio);

                        // Update the total exclusize outer exclusion zone volume and increment shell volume
                        exclusiveIncrementShellVolume += (datapoint.dP33DetachedShellFactor(combined_rayLength, combined_stressShadowHalfWidth, combined_rayLengthIncrement, combined_stressShadowHalfWidthIncrement) * (4d / 3d) * Math.PI / (double)RaysPerFracture);
                    }
                }
            }

            // Calculate ratio of the total volume not in an interaction or exclusion zone around any fracture segment in this set to the total clear zone volume (Phi_II)
            double phi_II = Math.Exp(-exclusiveIncrementShellVolume);

            // Return the clear zone volume
            return phi_II;
        }

        // Connectivity indices
        /// <summary>
        /// Proportion of unconnected fracture tip - i.e. the proportion of the total circumference of all fractures that is not connected to another fracture
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 1</param>
        /// <returns>Ratio of (a_UCRP30_total + r_UCRP30_total + sMR_RP30_total) to UCFP30_total; i.e. normalised to the number of rays per fracture</returns>
        public double UnconnectedTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 1;
            double T_FP30 = Fractures.FP30_total;
            return (T_FP30 > 0 ? (Fractures.a_RP30_total + Fractures.r_RP30_total + Fractures.sMR_RP30_total) / T_FP30 : undefinedReturn);
        }
        /// <summary>
        /// Proportion of fracture tip connected to a relay zone - i.e. the proportion of the total circumference of all fractures that is deactivated due to stress shadow interaction
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sII_RP30_total to UCFP30_total</returns>
        public double RelayTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double T_FP30 = Fractures.FP30_total;
            return (T_FP30 > 0 ? (Fractures.sII_RP30_total) / T_FP30 : undefinedReturn);
        }
        /// <summary>
        /// Proportion of intersecting fracture tip - i.e. the proportion of the total circumference of all fractures that intersects with another orthogonal or oblique fracture
        /// </summary>
        /// <param name="ReturnNanForUndefined">Determine return value if there are no fractures: if true, will return Nan; if false, will return 0</param>
        /// <returns>Ratio of sIJ_RP30_total to UCFP30_total</returns>
        public double IntersectingTipRatio(bool ReturnNanForUndefined)
        {
            double undefinedReturn = ReturnNanForUndefined ? double.NaN : 0;
            double T_FP30 = Fractures.FP30_total;
            return (T_FP30 > 0 ? (Fractures.sIJ_RP30_total) / T_FP30 : undefinedReturn);
        }

        // Functions to convert between fracture growth weighted time (WTime, proportional to CumGamma) and real time
        /// <summary>
        /// Convert from fracture growth weighted time (WTime) since start of timestep to real time; for constant driving stress these are proportional but for variable driving stress they are not
        /// </summary>
        /// <param name="wtime">Weighted time since the start of the timestep</param>
        /// <param name="timestep">Timestep index</param>
        /// <returns>Real time (s)</returns>
        public double ConvertWeightedTimeToTime(double wtime, int timestep)
        {
            double time;

            // Check specified timestep is within range
            if ((timestep >= 0) && (timestep <= PreviousFractureData.NoTimesteps))
            {
                // Cache constants locally
                double b = gbc.MechProps.b_factor;
                double CapA = gbc.MechProps.CapA;
                double Kc = gbc.MechProps.Kc;
                double SqrtPi = Math.Sqrt(Math.PI);
                double sqrtpi_Kc_factor = 2 / (SqrtPi * Kc);

                // Set start time to timestep
                time = PreviousFractureData.getStartTime(timestep);

                // Get U, V
                double tsU = PreviousFractureData.getConstantDrivingStressU(timestep);
                double tsV = PreviousFractureData.getVariableDrivingStressV(timestep);

                if (tsV == 0) // If V is zero (i.e. UniformStrainRelaxation and FractureOnlyStrainRelaxation strain relaxation cases) the driving stress is constant, given by U
                {
                    if (tsU > 0)
                        time += wtime / (CapA * Math.Pow(sqrtpi_Kc_factor * tsU, b));
                }
                else // If V is not zero (i.e. NoStrainRelaxation strain relaxation case) the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    double U_factor = Math.Pow(sqrtpi_Kc_factor * tsU, b) * tsU;
                    double V_factor = ((b + 1) * tsV * wtime) / CapA;
                    double UV_factor = Math.Pow(sqrtpi_Kc_factor, -b / (b + 1)) * Math.Pow(V_factor + U_factor, 1 / (b + 1));
                    double UV_factor_minus_U = UV_factor - tsU;
                    // UV_factor should always be greater than U, but due to rounding errors, sometimes (UV_factor - tsU) returns a small negative number when it should return 0
                    // This is incorrect and can cause problems later, so to prevent this we will ensure that UV_factor_minus_U is never less than 0
                    if (UV_factor_minus_U < 0) UV_factor_minus_U = 0;
                    time += (UV_factor_minus_U / tsV);
                }
            }
            // If the specified timestep is out of range, set return value to NaN
            else
            {
                time = double.NaN;
            }

            return time;
        }
        /// <summary>
        /// Convert from real time to fracture growth weighted time (WTime) since start of timestep; for constant driving stress these are proportional but for variable driving stress they are not
        /// <param name="time">Real time (s)</param>
        /// <param name="timestep">Timestep index</param>
        /// <returns>Weighted time since the start of the timestep</returns>
        public double ConvertTimeToWeightedTime(double time, int timestep)
        {
            double wtime;

            // Check specified timestep is within range
            if ((timestep >= 0) && (timestep <= PreviousFractureData.NoTimesteps))
            {
                // Cache constants locally
                double b = gbc.MechProps.b_factor;
                double CapA = gbc.MechProps.CapA;
                double Kc = gbc.MechProps.Kc;
                double SqrtPi = Math.Sqrt(Math.PI);
                double sqrtpi_Kc_factor = 2 / (SqrtPi * Kc);

                // Subtract start time of timestep
                time -= PreviousFractureData.getStartTime(timestep);

                // Get U, V
                double tsU = PreviousFractureData.getConstantDrivingStressU(timestep);
                double tsV = PreviousFractureData.getVariableDrivingStressV(timestep);

                if (tsV == 0) // If V is zero (i.e. UniformStrainRelaxation and FractureOnlyStrainRelaxation strain relaxation cases) the driving stress is constant, given by U
                {
                    if (tsU >= 0)
                        wtime = time * (CapA * Math.Pow(sqrtpi_Kc_factor * tsU, b));
                    // If U < 0 then the driving stress is negative; therefore the fractures cannot grow and weighted time is 0
                    else
                        wtime = 0;
                }
                else // If V is not zero (i.e. NoStrainRelaxation strain relaxation case) the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    double UV_factor1 = tsU + (tsV * time);
                    if (UV_factor1 >= 0)
                    {
                        double UV_factor2 = Math.Pow(sqrtpi_Kc_factor * UV_factor1, b) * UV_factor1;
                        double U_factor2 = Math.Pow(sqrtpi_Kc_factor * tsU, b) * tsU;
                        wtime = (CapA / ((b + 1) * tsV)) * (UV_factor2 - U_factor2);
                    }
                    // If UV_factor1 < 0 then the driving stress is negative; therefore the fractures cannot grow and weighted time is 0
                    else
                    {
                        wtime = 0;
                    }
                }
            }
            // If the specified timestep is out of range, set return value to NaN
            else
            {
                wtime = double.NaN;
            }

            return wtime;
        }

        // Functions to calculate implicit fracture population data
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
                deactivateFractures();
        }
        /// <summary>
        /// Check if the fracture set meets the specified deactivation criteria, and if so set the fracture evolution stage to Deactivated
        /// </summary>
        /// <param name="historic_a_UCFP32_termination_ratio">Ratio of current to maximum active mean linear fracture density at which fracture sets are considered inactive; calculation will terminate when fracture set falls below this ratio</param>
        /// <param name="active_total_RP30_termination_ratio">Ratio of active to total fracture volumetric density at which fracture sets are considered inactive; calculation will terminate when fracture set falls below this ratio</param>
        /// <param name="minimum_ClearZone_Volume">Minimum required clear zone volume in which fractures can nucleate without stress shadow interactions (as a proportion of total volume); if the clear zone volume falls below this value, the fracture set will be deactivated</param>
        /// <returns>True if the fracture set meets any of the deactivation criteria</returns>
        public bool CheckFractureDeactivation(double historic_a_UCFP32_termination_ratio, double active_total_RP30_termination_ratio, double minimum_ClearZone_Volume)
        {
            // If the fracture set is already deactivated, we do not need to check it again
            if (CurrentFractureData.EvolutionStage == FractureEvolutionStage.Deactivated)
                return true;

            // Flag to deactivate fracture set; initially set to false
            bool deactivateFractureSet = false;

            // Calculate the ratio of current to maximum active fracture volumetric ratio for this fracture set, and if it is below the specified minimum set the fracture deactivation flag to true
            // We only need to do this if the specified minimum is greater than zero; otherwise the check is not performed
            if (historic_a_UCFP32_termination_ratio > 0)
            {
                // If the active fracture volumetric ratio for this fracture set is increasing, update the maximum historic active fracture volumetric ratio
                double current_tot_a_RP32 = Fractures.a_RP32_total + Fractures.r_RP32_total;
                if (max_historic_a_UCFP32 < current_tot_a_RP32)
                    max_historic_a_UCFP32 = current_tot_a_RP32;

                double historic_a_RP32_ratio = (max_historic_a_UCFP32 > 0 ? current_tot_a_RP32 / max_historic_a_UCFP32 : 1);
                if (historic_a_RP32_ratio <= historic_a_UCFP32_termination_ratio)
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
            if (CurrentFractureData.theta_dashed_allFS_M < minimum_ClearZone_Volume)
            {
                deactivateFractureSet = true;
            }

            // Check if the extrapolated initial microfracture radius of current nucleating fractures is less than zero (will only apply if b<2); if so, set the fracture deactivation flag to true
            if (Check_Initial_UCF_Radius(MinimumFractureRadius))
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
            // If the initial driving stress is positive or zero, there will be fracture growth in this timestep so the optimal timestep duration will be the estimated minimum time taken for the fracture set to grow by the specified limiting amount d_rmax
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

                // If a fracture ray growth limit is specified, calculate the time taken for the largest active fracture rays with P30 above the cutoff to grow by the specified amount
                if (d_rmax > 0)
                {
                    // First calculate the time taken for the largest fully active fracture rays with P30 above the cutoff to grow by the specified amount
                    int FADatapointNo = 0;
                    while (Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive].Count > FADatapointNo)
                    {
                        // If the dRP30 value for this datapoint is below the minimum, move on to the next datapoint
                        double dRP30 = Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][FADatapointNo].dRP30;
                        if (dRP30 < min_GrowingDatapoint_RP30)
                        {
                            FADatapointNo++;
                            continue;
                        }

                        double timeTodRmax;

                        // Get the current radius of the largest fully active fracture with P30 above the cutoff
                        double maxR = Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive][FADatapointNo].RayLength;

                        // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                        double criticalRadius = Math.Pow(sqrtpi_Kc_factor * U, -2);
                        if (maxR > criticalRadius)
                        {
                            // Critical fracture propagation occurs at a constant rate
                            // The time taken to grow by a specified amount can thus be calculated by division
                            timeTodRmax = (d_rmax * maxR) / CapA;
                        }
                        else
                        {
                            // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                            double drmax_term = bis2 ? Math.Log(1 + d_rmax) : (1 - Math.Pow(1 + d_rmax, 1 / beta));
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

                    // Then calculate the time taken for the largest restricted fracture rays with P30 above the cutoff to grow by the specified amount
                    int RDatapointNo = 0;
                    while (Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted].Count > RDatapointNo)
                    {
                        // If the dRP30 value for this datapoint is below the minimum, move on to the next datapoint
                        double dRP30 = Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][RDatapointNo].dRP30;
                        if (dRP30 < min_GrowingDatapoint_RP30)
                        {
                            RDatapointNo++;
                            continue;
                        }

                        double timeTodRmax;

                        // Get the current radius and controlling radius of the largest restricted fracture ray
                        double maxR = Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][RDatapointNo].RayLength;
                        double R0 = Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted][RDatapointNo].PropagationControllingLength;

                        // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                        double criticalRadius = (2 * Math.Pow(sqrtpi_Kc_factor * U, -2)) - R0;
                        if (maxR > criticalRadius)
                        {
                            // Critical fracture propagation occurs at a constant rate
                            // The time taken to grow by a specified amount can thus be calculated by division
                            timeTodRmax = (d_rmax * maxR) / CapA;
                        }
                        else
                        {
                            // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                            double drmax_r0_term = ((1 + d_rmax) + (R0 / maxR)) / 2;
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
                }

                // If a P33 growth limit is specified, calculate the time taken for this to be exceeded
                // NB it may require multiple datapoints to exceed the value
                if (d_P33max > 0)
                {
                    // Create a list to compare the expected growth in P33 volumetric density represented by each datapoint
                    List<FractureGrowthControl> datapointP33Increments = new List<FractureGrowthControl>();

                    // Loop through all the fully active and restricted datapoints and add them to the list
                    foreach (RayPropagationStatus status in new RayPropagationStatus[2] { RayPropagationStatus.FullyActive, RayPropagationStatus.Restricted })
                        foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[status])
                        {
                            // Create variables for the dP33 increment that can be achieved by this datapoint, the time required to grow by this amount, and a flag to indicate whether the dP33 increment of this datapoint alone is sufficient to reach the specified dP33 growth target
                            double growthDurationRequired, dP33_increment;
                            bool requiredGrowthAchieved;

                            // Get the current radius
                            double datapoint_currentR = datapoint.RayLength;

                            // Find the radius the current fracture would need to grow to to increment dP33 by the required amount
                            double datapoint_current_dP33 = (4d / 3d) * (Math.PI / (double)RaysPerFracture) * datapoint.dP33factor;
                            double datapoint_drmax = Math.Pow((d_P33max / datapoint_current_dP33) + 1, 1d / 3d) - 1;
                            // If this is greater than the maximum allowable fracture radius, calculate the actual dP33 growth achieved and set the flag to indicate that the required dP33 growth cannot be achieved by this datapoint alone
                            // NB we will not reduce the radius increment for the datapoint accordingly; if this is the time-limiting datapoint, it will grow to the maximum radius and then arrest, but will not fall short due to rounding errors
                            double max_drmax = (MaximumFractureRadius / datapoint_currentR) - 1;
                            if (datapoint_drmax > max_drmax)
                            {
                                dP33_increment = datapoint_current_dP33 * (Math.Pow(datapoint_drmax + 1, 3) - 1);
                                requiredGrowthAchieved = false;
                            }
                            else // Otherwise set the actual dP33 growth achieved to the required dP33 growth and set the flag to indicate that the required dP33 growth can be achieved by this datapoint alone
                            {
                                dP33_increment = d_P33max;
                                requiredGrowthAchieved = true;
                            }

                            // Now calculate the time taken to achieve this growth
                            // This calculation will be different for fully active and restricted datapoints
                            if (status == RayPropagationStatus.FullyActive)
                            {
                                // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                                double criticalRadius = Math.Pow(sqrtpi_Kc_factor * U, -2);
                                if (datapoint_currentR > criticalRadius)
                                {
                                    // Critical fracture propagation occurs at a constant rate
                                    // The time taken to grow by a specified amount can thus be calculated by division
                                    growthDurationRequired = (datapoint_drmax * datapoint_currentR) / CapA;
                                }
                                else
                                {
                                    // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                                    double drmax_term = bis2 ? Math.Log(1 + datapoint_drmax) : (1 - Math.Pow(1 + datapoint_drmax, 1 / beta));
                                    double U_term = U * Math.Pow(U * sqrtpi_Kc_factor, b);
                                    double V_term1 = bis2 ? drmax_term : -beta * (drmax_term * Math.Pow(datapoint_currentR, 1 / beta));
                                    double V_term2 = ((b + 1) * (V / CapA) * V_term1);
                                    double UV_term = U_term + V_term2;

                                    // If U>>V then the exact equation for optimal duration may give zero because (U_term + V_term2) is indistinguishable from U_term due to rounding
                                    if ((float)(UV_term / U_term) > 1f)
                                    {
                                        // Use the formula for increasing stress to calculate optimal duration
                                        growthDurationRequired = (Math.Pow(UV_term, 1 / (b + 1)) / (V * Math.Pow(sqrtpi_Kc_factor, b / (b + 1)))) - (U / V);
                                    }
                                    else // In this case we can approximate V=0 and use the constant driving stress formula
                                    {
                                        // Use the formula for constant stress to calculate optimal duration, and set V to zero
                                        growthDurationRequired = V_term1 / (CapA * Math.Pow(U * sqrtpi_Kc_factor, b));
                                        setVto0 = true;
                                    }
                                }
                            }
                            else
                            {
                                // Get the controlling radius of the restricted fracture ray
                                double datapoint_R0 = datapoint.PropagationControllingLength;

                                // Check if the fracture radius exceeds that at which critical propagation will occur at the initial driving stress
                                double criticalRadius = (2 * Math.Pow(sqrtpi_Kc_factor * U, -2)) - datapoint_R0;
                                if (datapoint_currentR > criticalRadius)
                                {
                                    // Critical fracture propagation occurs at a constant rate
                                    // The time taken to grow by a specified amount can thus be calculated by division
                                    growthDurationRequired = (datapoint_drmax * datapoint_currentR) / CapA;
                                }
                                else
                                {
                                    // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                                    double drmax_r0_term = ((1 + datapoint_drmax) + (datapoint_R0 / datapoint_currentR)) / 2;
                                    double rmax_r0_term = (datapoint_currentR + datapoint_R0) / (2 * datapoint_currentR);
                                    double drmax_term = 2 * (bis2 ? Math.Log(drmax_r0_term) - Math.Log(rmax_r0_term) : Math.Pow(rmax_r0_term, 1 / beta) - Math.Pow(drmax_r0_term, 1 / beta));
                                    double U_term = U * Math.Pow(U * sqrtpi_Kc_factor, b);
                                    double V_term1 = bis2 ? drmax_term : -beta * (drmax_term * Math.Pow(datapoint_currentR, 1 / beta));
                                    double V_term2 = ((b + 1) * (V / CapA) * V_term1);
                                    double UV_term = U_term + V_term2;

                                    // If U>>V then the exact equation for optimal duration may give zero because (U_term + V_term2) is indistinguishable from U_term due to rounding
                                    if ((float)(UV_term / U_term) > 1f)
                                    {
                                        // Use the formula for increasing stress to calculate optimal duration
                                        growthDurationRequired = (Math.Pow(UV_term, 1 / (b + 1)) / (V * Math.Pow(sqrtpi_Kc_factor, b / (b + 1)))) - (U / V);
                                    }
                                    else // In this case we can approximate V=0 and use the constant driving stress formula
                                    {
                                        // Use the formula for constant stress to calculate optimal duration, and set V to zero
                                        growthDurationRequired = V_term1 / (CapA * Math.Pow(U * sqrtpi_Kc_factor, b));
                                        setVto0 = true;
                                    }
                                }
                            }

                            // Create a new FractureGrowthControl object for this datapoint and add it to the list
                            // Screen for NaN values first
                            if (!double.IsNaN(growthDurationRequired))
                                datapointP33Increments.Add(new FractureGrowthControl(datapoint, dP33_increment, growthDurationRequired, requiredGrowthAchieved));
                        }

                    // Sort the list by duration, from shortest to longest
                    datapointP33Increments.Sort();
                    // Then go through the list in order, adding up the dP33 increments acheived by each datapoint, until the total required increment is achieved
                    // If any of the datapoints can achieve the required dP33 increment alone, the shortest of these will define the timestep duration
                    double timeTodP33max = double.PositiveInfinity;
                    double dP33_to_accommodate = d_P33max;
                    foreach (FractureGrowthControl datapointControl in datapointP33Increments)
                    {
                        timeTodP33max = datapointControl.TimeToReachIncrement;
                        dP33_to_accommodate -= datapointControl.dP33Increment;
                        if (dP33_to_accommodate <= 0)
                            break;
                    }

                    // Calculate the time until the next datapoint representing nucleating fractures will form
                    {
                        // Cache required data locally
                        double rmin_beta = bis2 ? Math.Log(MinimumFractureRadius) : Math.Pow(MinimumFractureRadius, 1 / beta);
                        double cumGammaRmin_Nminus1 = rmin_beta + CurrentFractureData.Cum_Gamma_Mminus1;
                        double next_URP30 = previous_LRP30 + (min_NucleatingDatapoint_RP30 / CurrentFractureData.theta_Mminus1);
                        double next_UP30 = next_URP30 / (double)RaysPerFracture;

                        // Get the weighted time until the next datapoint will nucleate
                        double r0 = InitialRadius(next_UP30);
                        double nucleationWtime = bis2 ? -(Math.Log(r0) - cumGammaRmin_Nminus1) : -beta * (Math.Pow(r0, 1 / beta) - cumGammaRmin_Nminus1);

                        // Convert the weighted time into a real time
                        if (nucleationWtime > 0)
                        {
                            double timeToNextDatapoint;
                            double U_term = Math.Pow(U * sqrtpi_Kc_factor, b + 1);
                            double V_term = (nucleationWtime / CapA) * V * (b + 1) * sqrtpi_Kc_factor;
                            double UV_term = U_term + V_term;
                            if ((float)(UV_term / U_term) > 1f)
                            {
                                timeToNextDatapoint = (Math.Pow(UV_term, 1 / (b + 1)) / (V * sqrtpi_Kc_factor)) - (U / V);
                            }
                            else
                            {
                                timeToNextDatapoint = (nucleationWtime / CapA) / Math.Pow(U * sqrtpi_Kc_factor, b);
                            }

                            if (timeTodP33max > timeToNextDatapoint)
                                timeTodP33max = timeToNextDatapoint;
                        }
                    }

                    // If the real time until the next datapoint will nucleate is less than the current timestep duration, set the current timestep duration to the real time until the next detapoint will nucleate
                    if (timeTodP33max < optdur)
                        optdur = timeTodP33max;
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
        /// Class used to collate growth in all implicit fracture population datapoints and compare them by duration
        /// </summary>
        private class FractureGrowthControl : IComparable<FractureGrowthControl>
        {
            // Data
            /// <summary>
            /// Reference to the relevent ImplicitFracturePopulationDatapoint object
            /// </summary>
            public ImplicitFracturePopulationDatapoint Datapoint;
            /// <summary>
            /// Increment in the P33 volumetric density represented by the datapoint during the growth period
            /// </summary>
            public double dP33Increment;
            /// <summary>
            /// Duration of the growth period
            /// </summary>
            public double TimeToReachIncrement;
            /// <summary>
            /// Flag to indicate whether the dP33 increment of this datapoint alone is sufficient to reach the specified dP33 growth target
            /// </summary>
            public bool RequiredGrowthAchieved;

            // Control and implementation functions
            /// <summary>
            /// Compare FractureGrowthControl objects based on duration
            /// </summary>
            /// <param name="that">FractureGrowthControl object to compare with</param>
            /// <returns>Positive if this FractureGrowthControl object has the longest duration, negative if that FractureGrowthControl object has the longest duration, zero if they have the same duration</returns>
            public int CompareTo(FractureGrowthControl that)
            {
                return this.TimeToReachIncrement.CompareTo(that.TimeToReachIncrement);
            }

            // Constructors
            /// <summary>
            /// Constructor: specify GridblockCOnfiguration object, timestep number and end time
            /// </summary>
            /// <param name="Datapoint_in">Reference to ImplicitFracturePopulationDatapoint object</param>
            /// <param name="dP33Increment_in">Increment in the P33 volumetric density represented by the datapoint during the growth period</param>
            /// <param name="TimeToReachIncrement_in">Duration of the growth period (s)</param>
            /// <param name="RequiredGrowthAchieved_in">Flag to indicate whether the dP33 increment of this datapoint alone is sufficient to reach the specified dP33 growth target</param>
            public FractureGrowthControl(ImplicitFracturePopulationDatapoint Datapoint_in, double dP33Increment_in, double TimeToReachIncrement_in, bool RequiredGrowthAchieved_in)
            {
                // Reference to relevant ImplicitFracturePopulationDatapoint object
                Datapoint = Datapoint_in;
                // Increment in the P33 volumetric density represented by the datapoint during the growth period
                dP33Increment = dP33Increment_in;
                // Duration of the growth period
                TimeToReachIncrement = TimeToReachIncrement_in;
                // Flag to indicate whether the dP33 increment of this datapoint alone is sufficient to reach the specified dP33 growth target
                RequiredGrowthAchieved = RequiredGrowthAchieved_in;
            }
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
            double F_Growth_Factor = 0;
            if ((float)U_M >= 0f) // If the initial driving stress is less than zero, the mean driving stress for the timestep will be zero
            {
                // Calculate the final driving stress for the timestep
                double final_SigmaD_M = U_M + (TimestepDuration_in * V_M);

                if ((float)final_SigmaD_M == (float)U_M) // If the final driving stress is equal to the initial driving stress then assume the driving stress is constant; NB we check this rather than checking for V_M equals zero, as this will also pick up very short timesteps, where the change in driving stress during the timestep is negligible even though V_M > 0
                {
                    // To calculate alpha * SigmaDb_M, we divide mean_SigmaD_M by Kc before raising it to b, to avoid excessively large numbers
                    mean_SigmaD_M = U_M;
                    // We will not calculate the fracture propagation rate coefficient if the fractures are no longer active
                    // This will prevent continued propagation of the explicit fractures in the DFN after the implicit fracture model is saturated
                    if (FracturesActive)
                    {
                        F_PropRate_Coefficient = CapA * Math.Pow(sqrtpi_Kc_factor * mean_SigmaD_M, b);
                        F_Growth_Factor = F_PropRate_Coefficient * TimestepDuration_in;
                        if (!bis2)
                        {
                            F_PropRate_Coefficient /= Math.Abs(beta);
                            F_Growth_Factor /= -beta;
                        }
                        else
                        {
                            F_Growth_Factor = -F_Growth_Factor;
                        }
                    }
                }
                else // If final driving stress is not equal to the initial driving stress then the driving stress will vary through the timestep, so we must calculate a weighted mean
                {
                    // We will combine the U and V power terms with Kc to avoid getting extreme values when b is high
                    double UV_U_term = (final_SigmaD_M < 0 ? 0 : (final_SigmaD_M * Math.Pow(sqrtpi_Kc_factor * final_SigmaD_M, b)) - (U_M * Math.Pow(sqrtpi_Kc_factor * U_M, b)));

                    mean_SigmaD_M = Math.Pow(UV_U_term / ((b + 1) * V_M * TimestepDuration_in), 1 / b) / sqrtpi_Kc_factor;
                    // We will not calculate the fracture propagation rate coefficient if the fractures are no longer active
                    // This will prevent continued propagation of the explicit fractures in the DFN after the implicit fracture model is saturated
                    if (FracturesActive)
                    {
                        F_Growth_Factor = CapA * (UV_U_term / ((b + 1) * V_M));
                        F_PropRate_Coefficient = F_Growth_Factor / TimestepDuration_in;
                        if (!bis2)
                        {
                            F_PropRate_Coefficient /= Math.Abs(beta);
                            F_Growth_Factor /= -beta;
                        }
                        else
                        {
                            F_Growth_Factor = -F_Growth_Factor;
                        }
                    }
                }
            }

            // Set the timestep duration mean driving stress, mean macrofracture propagation rate and microfracture propagation rate coefficient (= gamma ^ 1/beta)
            CurrentFractureData.SetDynamicData(TimestepDuration_in, mean_SigmaD_M, F_PropRate_Coefficient);

            // If the fracture set has been deactivated, there will be no fracture growth or nucleation
            if (!FracturesActive)
                return;

            // Calculate the growth increments for all fully active datapoints
            // Note that these are the maximum stress-driven growth increments that do no take into account the maximum allowed ray length
            foreach (ImplicitFracturePopulationDatapoint activeFracturePopulationDatapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive])
            {
                double initialR = activeFracturePopulationDatapoint.RayLength;

                // If this datapoint represents newly nucleating fractures and an initial increment has already been set to bring the datapoint to the nucleation radius, skip it an move on to the next datapoint
                if (((float)initialR == 0f) && (activeFracturePopulationDatapoint.ActualRayLengthIncrement > 0))
                    continue;

                // Calculation of the increment in ray length will depend on whether propagation of the fracture is critical or subcritical
                // First calculate the increment in ray length assuming subcritical fracture propagation
                // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                double finalR = bis2 ? (initialR * Math.Exp(-F_Growth_Factor)) : Math.Pow(Math.Pow(initialR, 1 / beta) - F_Growth_Factor, beta);
                double incrementR = finalR - initialR;

                // It is not possible to calculate analytically the onset of critical fracture propagation if the driving stress is varying during the timestep, as the fracture radius is also not constant
                // Therefore we will check whether the increment in ray length is greater than that which would be achieved by critical fracture propagation, and if so reduce it to this amount
                // This will overestimate the total fracture propagation if the fracture becomes critical during the timestep, but not by as much as if we only check for critical propagation at the start of the timestep
                // NB If the fracture reaches the blow-up radius within this timestep then the finalR calculation will return NaN (this can only happen for R>2)
                // It is therefore also important to check for NaNs
                double criticalPropagationIncrement = CapA * TimestepDuration_in;
                if (((float)incrementR >= (float)criticalPropagationIncrement) || double.IsNaN(incrementR))
                    incrementR = criticalPropagationIncrement;

                // Set the ray length increment
                activeFracturePopulationDatapoint.CalculatedRayLengthIncrement = incrementR;
            }

            // Set the growth rates for all restricted datapoints
            foreach (ImplicitFracturePopulationDatapoint restrictedFracturePopulationDatapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted])
            {
                double initialR = restrictedFracturePopulationDatapoint.RayLength;
                double initialReff = restrictedFracturePopulationDatapoint.EffectiveRayLength;
                double initialRc = restrictedFracturePopulationDatapoint.PropagationControllingLength;

                // Calculation of the increment in ray length will depend on whether propagation of the fracture is critical or subcritical
                // First calculate the increment in ray length assuming subcritical fracture propagation
                // Subcritical propagation rate is dependent on driving stress and fracture size, both of which may vary during the timestep
                double finalR = bis2 ? (2 * initialReff * Math.Exp(-F_Growth_Factor / 2)) - initialRc : (2 * Math.Pow(Math.Pow(initialReff, 1 / beta) - (F_Growth_Factor / 2), beta)) - initialRc;
                double incrementR = finalR - initialR;

                // It is not possible to calculate analytically the onset of critical fracture propagation if the driving stress is varying during the timestep, as the fracture radius is also not constant
                // Therefore we will check whether the increment in ray length is greater than that which would be achieved by critical fracture propagation, and if so reduce it to this amount
                // This will overestimate the total fracture propagation if the fracture becomes critical during the timestep, but not by as much as if we only check for critical propagation at the start of the timestep
                // NB If the fracture reaches the blow-up radius within this timestep then the finalR calculation will return NaN (this can only happen for R>2)
                // It is therefore also important to check for NaNs
                double criticalPropagationIncrement = CapA * TimestepDuration_in;
                if (((float)incrementR >= (float)criticalPropagationIncrement) || double.IsNaN(incrementR))
                    incrementR = criticalPropagationIncrement;

                // Set the ray length increment
                restrictedFracturePopulationDatapoint.CalculatedRayLengthIncrement = incrementR;
            }

            // Add a new fully active datapoint representing fractures nucleating in this timestep - but only if any new fractures nucleate
            if (FracturesActive && ((float)mean_SigmaD_M > 0f))
            {
                ImplicitFracturePopulationDatapoint nucleatingFractures = getNucleatingFractures();
                if (!(nucleatingFractures is null) && ((float)nucleatingFractures.dRP30 > 0f))
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

            // Get the limiting volumetric ray density LRP30 (assuming no stress shadow deactivation) for fractures with radius > rmin at the end of the current timestep
            double r0 = bis2 ? Math.Exp(cumGammaRmin_N) : Math.Pow(cumGammaRmin_N, beta);
            double LFP30_N = InitialP30(r0);
            double LRP30_N = LFP30_N * (double)RaysPerFracture;

            // Get the actual incremental increase in the volumetric ray density dRP30 from the time the previous datapoint nucleated
            // To do this we must subtract the limiting ray density when the previous datapoint nucleated, and then multiply the volumetric density increment by the inverse stress shadow volume seen by fully active fractures with minimum radius
            // This will correct for the fact that fracture seed points located in a stress shadow cannot nucleate fractures
            double dRP30 = (LRP30_N - previous_LRP30) * CurrentFractureData.theta_Mminus1;

            // If the calculated dMFP30 value is less than the specified minimum, return null (no new datapoint will be created)
            // Allow some leeway to account for rounding error, if the timestep duration has been calculated to exactly reach the datapoint nucleation threshold
            // This is especially important in the early timesteps with a log-normal initial microfracture distribution, where the CDDF is very close to 1
            if (dRP30 < (dRP30_rounding_error * min_NucleatingDatapoint_RP30))
                return null;

            // Create a new datapoint and return it
            // Also update the cumulative value of the LRP30 at the last time new fractures nucleated
            previous_LRP30 = LRP30_N;
            return new ImplicitFracturePopulationDatapoint(MinimumFractureRadius, dRP30, Fractures);
        }
        /// <summary>
        /// Calculate the probability of deactivation for fractures represented by each of the datapoints
        /// Note that this function does not implement the deactivation - this will only be done after deactivation probabilites have been calculated for all fracture sets, by the updateTotalFracturePopulation() function
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
                        orientationMultipliers[setNo, rayNo] = Math.Abs(ufs.normalVector & this.rayVectors[rayNo]);
                }
            }

            // Cache the minimum intersection deactivation ratio locally
            double minIntersectionDeactivationRatio = gbc.PropControl.MinIntersectionDeactivationRatio;

            // Loop through all fully active datapoints, calculating deactivation rates due to stress shadow interaction and intersection
            foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive])
            {
                // Get the probability that a fracture represented by this datapoint will not be deactivated due to stress shadow interaction in the current timestep
                // This is given by the inverse of the interaction zone volume around all other fractures in the current set
                // This will depend on the stress distribution scenario
                double phiII_M;
                switch (FractureDistribution)
                {
                    // There are no stress shadows in the evenly distributed stress scenario
                    case StressDistribution.EvenlyDistributedStress:
                        phiII_M = 1;
                        break;
                    // Stress shadow widths are proportional to the effective fracture radius in the stress shadow scenario
                    case StressDistribution.StressShadow:
                    // The ductile boundary scenario is not valid for unconfined fractures, so we default to the stress shadow scenario
                    case StressDistribution.DuctileBoundary:
                        phiII_M = getStressShadowNonInteractionVolumeRatio(datapoint);
                        break;
                    // By default assume no stress shadows
                    default:
                        phiII_M = 1;
                        break;
                }

                // Get the probability that a fracture represented by this datapoint will not be deactivated due to intersecting a fracture from another set in the current timestep
                // This is given by the apparent P32 of the intersected fracture set, corrected for orientation of the intersected fracture and the propagating ray
                double mean_apparent_P32 = 0;
                double minIntersectionRadius = minIntersectionDeactivationRatio * datapoint.EffectiveRayLength;
                for (int setNo = 0; setNo < noSets; setNo++)
                {
                    UnconfinedFractureSet ufs = gbc.UnconfinedFractureSets[setNo];
                    double ufs_P32 = ufs.Fractures.cumulative_FP32(minIntersectionRadius);
                    // For fully active fractures, we will take the maximum probability that any ray from the propagating fracture will hit a fracture from the other set
                    // This is because a fully active fracture will become restricted if any of the rays hits another fracture
                    double maxOrientationMultiplier = 0;
                    for (int rayNo = 0; rayNo < RaysPerFracture; rayNo++)
                    {
                        if (maxOrientationMultiplier < orientationMultipliers[setNo, rayNo])
                            maxOrientationMultiplier = orientationMultipliers[setNo, rayNo];
                    }
                    mean_apparent_P32 += (ufs_P32 * maxOrientationMultiplier);
                }
                double phiIJ_M = Math.Exp(-mean_apparent_P32 * datapoint.ActualRayLengthIncrement);

                // Update the fracture activation probabilites for the datapoint
                datapoint.UpdateFractureActivationProbabilities(phiII_M, phiIJ_M);
            }

            // Loop through all restricted datapoints, calculating deactivation rates due to stress shadow interaction and intersection
            foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted])
            {
                // Get the probability that a fracture represented by this datapoint will not be deactivated due to stress shadow interaction in the current timestep
                // This is given by the inverse of the interaction zone volume around all other fractures in the current set
                // This will depend on the stress distribution scenario
                double phiII_M;
                switch (FractureDistribution)
                {
                    // There are no stress shadows in the evenly distributed stress scenario
                    case StressDistribution.EvenlyDistributedStress:
                        phiII_M = 1;
                        break;
                    // Stress shadow widths are proportional to the effective fracture radius in the stress shadow scenario
                    case StressDistribution.StressShadow:
                    // The ductile boundary scenario is not valid for unconfined fractures, so we default to the stress shadow scenario
                    case StressDistribution.DuctileBoundary:
                        phiII_M = getStressShadowNonInteractionVolumeRatio(datapoint);
                        break;
                    // By default assume no stress shadows
                    default:
                        phiII_M = 1;
                        break;
                }

                // Get the probability that a fracture ray represented by this datapoint will not be deactivated due to intersecting a fracture from another set in the current timestep
                double mean_apparent_P32 = 0;
                double minIntersectionRadius = minIntersectionDeactivationRatio * datapoint.EffectiveRayLength;
                for (int setNo = 0; setNo < noSets; setNo++)
                {
                    UnconfinedFractureSet ufs = gbc.UnconfinedFractureSets[setNo];
                    double ufs_P32 = ufs.Fractures.cumulative_FP32(minIntersectionRadius);
                    // For restricted fractures, we will take the mean probability that any ray from the propagating fracture will hit a fracture from the other set
                    // This is because any of the restricted fracture rays will become deactivated if they hit another fracture
                    double meanOrientationMultiplier = 0;
                    for (int rayNo = 0; rayNo < RaysPerFracture; rayNo++)
                        meanOrientationMultiplier += orientationMultipliers[setNo, rayNo];
                    meanOrientationMultiplier /= (double)RaysPerFracture;
                    mean_apparent_P32 += (ufs_P32 * meanOrientationMultiplier);
                }
                double phiIJ_M = Math.Exp(-mean_apparent_P32 * datapoint.ActualRayLengthIncrement);

                // Update the fracture activation probabilites for the datapoint
                datapoint.UpdateFractureActivationProbabilities(phiII_M, phiIJ_M);
            }
        }
        /// <summary>
        /// Lock in the previously calculated increments in ray length and resort the fracture population distribution arrays
        /// </summary>
        public void updateTotalFracturePopulation()
        {
            // Cache the proportion of the ray length increment to apply to deactivating fractures before they deactivate, fracture growth deactivation cutoff and minimum fracture activation probability locally
            double proportionalIncrementToApply = gbc.PropControl.proportionalIncrementToApply;
            if (!(proportionalIncrementToApply >= 0))
                proportionalIncrementToApply = 1 - Fractures.StressShadowVolume_total;
            double max_R_deactivation = gbc.PropControl.max_R_DeactivationCheck_interval;
            double min_R_activation = gbc.PropControl.min_R_ActivationProbability;

            // First cache the mechanical properties and timestep dynamic data required to calculate the increment of restricted fractures
            double beta = gbc.MechProps.beta;
            bool bis2 = (gbc.MechProps.GetbType() == bType.Equals2);
            double criticalIncrement = gbc.MechProps.CapA * CurrentFractureData.M_Duration;

            // Loop through all restricted datapoints, calculating deactivation rates due to stress shadow interaction and intersection
            // We will calculate the restricted datapoints before the fully active ones, as the deactivation of fully active fractures will create new restricted fractures
            foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted])
            {
                // Only implement deactivation if the ray has grown by the specified amount or the activation probability has dropped below a minimum value
                if ((datapoint.ProportionalGrowthSinceLastDeactivationCheck >= max_R_deactivation) || (datapoint.Phi <= min_R_activation))
                {
                    // Calculate the probability that this ray will be deactivated due to stress shadow interaction and due to intersection during this timestep
                    // This function will also reduce the volumetric density for the datapoint proportionally
                    ImplicitFracturePopulationDatapoint[] newdatapoints = datapoint.DeactivateRays(proportionalIncrementToApply);

                    // Add the new datapoints to the appropriate arrays - but only if the volumetric density is greater than zero
                    if (newdatapoints.Length == 2)
                    {
                        if ((float)newdatapoints[0].dRP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticStressShadow].Add(newdatapoints[0]);
                        if ((float)newdatapoints[1].dRP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticIntersection].Add(newdatapoints[1]);
                    }
                }
            }

            // Loop through all fully active datapoints, calculating deactivation rates due to stress shadow interaction and intersection
            foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[RayPropagationStatus.FullyActive])
            {
                // Only implement deactivation if the ray has grown by the specified amount or the activation probability has dropped below a minimum value
                if ((datapoint.ProportionalGrowthSinceLastDeactivationCheck >= max_R_deactivation) || (datapoint.Phi <= min_R_activation))
                {
                    // Calculate the probability that this ray will be deactivated due to stress shadow interaction and due to intersection during this timestep
                    // This function will also reduce the volumetric density for the datapoint proportionally
                    ImplicitFracturePopulationDatapoint[] newdatapoints = datapoint.DeactivateRays(proportionalIncrementToApply);

                    // Add the new datapoints to the appropriate arrays - but only if the volumetric density is greater than zero
                    // Before doing so, calculate the appropriate ray length increment for the restricted ray in this timestep
                    if (newdatapoints.Length == 3)
                    {
                        // If the fully active rays are deactivated before the full increment is applied, the DeactivateRays(proportionalIncrementToApply) function will only calculate the increment on the restricted rays spawned from fully active rays until the point of deactivation
                        // We therefore need to calculate the increment for the new restricted rays until the end of the timestep
                        if ((proportionalIncrementToApply < 1) && ((float)newdatapoints[0].dRP30 > 0f))
                        {
                            // Calculate the correct increment for the new restricted ray until the end of the timestep
                            double fullyActiveRayIncrement = datapoint.CalculatedRayLengthIncrement;
                            double restrictedRayIncrementToDeactivation = newdatapoints[0].CalculatedRayLengthIncrement;
                            double lengthAtDeactivation = newdatapoints[0].RayLength + restrictedRayIncrementToDeactivation;
                            double restrictedRayIncrementFromDeactivationToEndTimestep;
                            // If the ray length at deactivation is less than the minimum ray length, extend the ray to the minimum length
                            if (lengthAtDeactivation < MinimumFractureRadius)
                            {
                                restrictedRayIncrementFromDeactivationToEndTimestep = MinimumFractureRadius - lengthAtDeactivation;
                            }
                            // If the ray is already propagating at the critical rate, this will not change
                            else if ((float)fullyActiveRayIncrement >= (float)criticalIncrement)
                            {
                                restrictedRayIncrementFromDeactivationToEndTimestep = criticalIncrement - restrictedRayIncrementToDeactivation;
                            }
                            // Otherwise calculate the length increment of the restricted ray from the length increment of the fully active rays
                            else
                            {
                                double fullyActiveFinalR = datapoint.RayLength + datapoint.CalculatedRayLengthIncrement;
                                if (bis2)
                                {
                                    restrictedRayIncrementFromDeactivationToEndTimestep = (2 * Math.Sqrt(lengthAtDeactivation * fullyActiveFinalR)) - (2 * lengthAtDeactivation);
                                }
                                else
                                {
                                    double R0term = Math.Pow(lengthAtDeactivation, 1 / beta) / 2;
                                    double RFAterm = Math.Pow(fullyActiveFinalR, 1 / beta) / 2;
                                    restrictedRayIncrementFromDeactivationToEndTimestep = (2 * Math.Pow(R0term + RFAterm, beta)) - (2 * lengthAtDeactivation);
                                }
                            }
                            // Set the ray length increment
                            newdatapoints[0].CalculatedRayLengthIncrement += restrictedRayIncrementFromDeactivationToEndTimestep;
                        }

                        if ((float)newdatapoints[0].dRP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.Restricted].Add(newdatapoints[0]);
                        if ((float)newdatapoints[1].dRP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticStressShadow].Add(newdatapoints[1]);
                        if ((float)newdatapoints[2].dRP30 > 0f)
                            Fractures.fracturePopulationDatapoints[RayPropagationStatus.StaticIntersection].Add(newdatapoints[2]);
                    }
                }
            }

            // Calculate increments in the stress shadow volume for each datapoint
            Fractures.CalculateStressShadowIncrementsFromRayLengthIncrement();

            // Apply the increments in ray length calculated by setTimestepPropagationData()
            Fractures.ApplyRayLengthIncrements();

            // Recalculate the total RP30 and RP32 values from the fracture population distribution arrays
            // This will also resort the fracture population distribution arrays based on new effective radius, from largest to smallest
            Fractures.RecalculateTotalPopulationData();

            // Recalculate the minimum RP30 value for nucleating fracture datapoints based on the mean static ray length
            // This is taken as a proxy for the maximum length that the nucleating rays will grow to
            // The minimum RP30 value represents the value that will generate the specified maximum dP33 increment when the rays grow to their maximum length
            double dP33 = gbc.PropControl.max_TS_UCFP33_increase;
            double maxRadius = (Fractures.MeanStaticRayLength > 0) ? Fractures.MeanStaticRayLength : MaximumFractureRadius;
            double maxFracVol = (4d / 3d) * Math.PI * Math.Pow(maxRadius, 3);
            min_NucleatingDatapoint_RP30 = (dP33 / maxFracVol) * (double)RaysPerFracture;
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
            Fractures.CullArray(RayPropagationStatus.StaticMaxRadius, minDatapointSizeRatio);
        }
        /// <summary>
        /// Set the unconfined fracture density indices a_RP30, r_RP30, sII_RP30, sIJ_RP30, sRMax_RP30, FP32 and FP33 in the CurrentFractureData object
        /// </summary>
        public void setFractureDensityData()
        {
            CurrentFractureData.SetFractureDensityData(Fractures.a_RP30_total, Fractures.r_RP30_total, Fractures.sII_RP30_total, Fractures.sIJ_RP30_total, Fractures.sMR_RP30_total, Fractures.FP32_total, Fractures.FP33_total);
        }
        /// <summary>
        /// Set the volumetric density of all unconfined fractures from other fracture sets that terminate against fractures from this set, at the end of timestep M
        /// </summary>
        /// <param name="terminatingFractureDensity_in">Volumetric density of all unconfined fractures from other fracture sets that terminate against fractures from this set</param>
        public void setTerminatingFractureDensity(double terminatingFractureDensity_in)
        {
            CurrentFractureData.TerminatingFractureDensity_M = terminatingFractureDensity_in;
        }
        /// <summary>
        /// Update the values describing the inverse stress shadow and clear zone volumes for this fracture set
        /// </summary>
        public void setFractureExclusionZoneData()
        {
            double theta;
            double theta_dashed = getStressShadowClearZoneVolume(MinimumFractureRadius, MinimumFractureRadius, out theta);
            CurrentFractureData.SetFractureExclusionZoneData(theta, theta_dashed);
        }
        /// <summary>
        /// Update the values describing the inverse stress shadow and clear zone volumes for all fracture sets
        /// </summary>
        /// <param name="theta_allFS_in">Inverse stress shadow volume (1 - Psi) of all fracture sets as seen by this set, i.e. cumulative probability that an initial microfracture from this set will not lie in the stress shadow of another fracture from any set, at end of timestep M</param>
        /// <param name="theta_dashed_allFS_in">Clear zone volume (1 - Chi) of all fracture sets as seen by this set, i.e. cumulative probability that a fracture nucleating from this set will not lie in the stress shadow exclusion zone of another fracture from any set, at end of timestep M</param>
        public void setOtherFSExclusionZoneData(double theta_AllFS, double theta_dashed_allFS)
        {
            CurrentFractureData.SetOtherFSExclusionZoneData(theta_AllFS, theta_dashed_allFS);
        }

        /// <summary>
        /// Set the current fracture evolution stage for the entire fracture set to Deactivated
        /// </summary>
        private void deactivateFractures()
        {
            // Set the ray length increments for all active datapoints to zero
            foreach (RayPropagationStatus status in new RayPropagationStatus[2] { RayPropagationStatus.FullyActive, RayPropagationStatus.Restricted })
                foreach (ImplicitFracturePopulationDatapoint datapoint in Fractures.fracturePopulationDatapoints[status])
                    datapoint.CalculatedRayLengthIncrement = 0;

            // Set the fracture evolution stage to deactivated
            CurrentFractureData.SetEvolutionStage(FractureEvolutionStage.Deactivated);
        }
        /// <summary>
        /// Check if the if the extrapolated initial radius of a fully active unconfined fracture with current radius r is zero or less (will only apply if b is less than 2)
        /// </summary>
        /// <param name="r">Current fracture radius</param>
        /// <returns>True if initial fracture radius is zero or less; false if it is greater than zero or b is greater than or equal to 2</returns>
        public bool Check_Initial_UCF_Radius(double r)
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

        // Functions to calculate explicit DFN fracture behaviour: used to check when fractures nucleate and if they interact with other fractures during DFN growth
        /// <summary>
        /// Get the initial radius (at time t=0) of the last explicit fracture to nucleate
        /// </summary>
        /// <returns></returns>
        public double getPreviousNucleatingFractureInitialRadius()
        {
            double implied_P30 = (double)(previous_Ln) / gbc.Volume;
            return InitialRadius(implied_P30);
        }
        /// <summary>
        /// Get the initial radius (at time t=0) of the next explicit fracture to nucleate, and increment the limit of explicit fractures nucleated
        /// </summary>
        /// <param name="incrementCounter">If true, will increment the counter Ln for the limit of explicit fractures nucleated before calculating the initial radius of the next fracture to nucleate; if false, Ln will not be incremented</param>
        /// <returns></returns>
        public double getNextNucleatingFractureInitialRadius(bool incrementCounter)
        {
            if (incrementCounter)
                previous_Ln++;
            double implied_P30 = (double)(previous_Ln + 1) / gbc.Volume;
            return InitialRadius(implied_P30);
        }
        /// <summary>
        /// Check whether a specified point (in XYZ coordinates) lies within the stress shadow of any of the unconfined fractures in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <returns></returns>
        public bool checkInUCFStressShadow(PointXYZ point)
        {
            return checkInUCFExclusionZone(point, 0, 0, getStressShadowWidthRatio(-1));
        }
        /// <summary>
        /// Check whether a specified point (in XYZ coordinates) lies within the stress shadow of any of the unconfined fractures in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="StressShadowWidthRatio">Ratio of the apparent stress shadow width to effective fracture radius</param>
        /// <returns></returns>
        public bool checkInUCFStressShadow(PointXYZ point, double StressShadowWidthRatio)
        {
            return checkInUCFExclusionZone(point, 0, 0, StressShadowWidthRatio);
        }
        /// <summary>
        /// Check whether a specified point (in XYZ coordinates) lies within an exclusion zone of arbitrary width around any of the stress shadows of the unconfined fractures in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="MaxOuterExclusionZoneZoneWidth">Width of the outer exclusion zone in the plane of the fracture (this will be the mean radius of the fracture we are checking against)</param>
        /// <param name="MinOuterExclusionZoneWidth">Width of the outer exclusion zone perpendicular to the plane of the fracture (this will be the effective radius of the fracture we are checking against)</param>
        /// <returns>True if point lies within an unconfined fracture stress shadow or within the surrounding proximity zone, otherwise false</returns>
        public bool checkInUCFExclusionZone(PointXYZ point, double MaxOuterExclusionZoneZoneWidth, double MinOuterExclusionZoneWidth)
        {
            return checkInUCFExclusionZone(point, MaxOuterExclusionZoneZoneWidth, MinOuterExclusionZoneWidth, getStressShadowWidthRatio(-1));
        }
        /// <summary>
        /// Check whether a specified point (in XYZ coordinates) lies within an exclusion zone of arbitrary width around any of the stress shadows of the unconfined fractures in the explicit DFN associated with this fracture set
        /// </summary>
        /// <param name="point">Input point in XYZ coordinates</param>
        /// <param name="MaxOuterExclusionZoneWidth">Width of the outer exclusion zone in the plane of the fracture (this will be the mean radius of the fracture we are checking against)</param>
        /// <param name="MinOuterExclusionZoneWidth">Width of the outer exclusion zone perpendicular to the plane of the fracture (this will be the effective radius of the fracture we are checking against)</param>
        /// <param name="StressShadowWidthRatio">Ratio of the apparent stress shadow width to effective fracture radius</param>
        /// <returns>True if point lies within an unconfined fracture stress shadow or within the surrounding proximity zone, otherwise false</returns>
        public bool checkInUCFExclusionZone(PointXYZ point, double MaxOuterExclusionZoneWidth, double MinOuterExclusionZoneWidth, double StressShadowWidthRatio)
        {
            // Loop through every unconfined fracture and check if the specified point lies in the exclusion zone
            foreach (UnconfinedFractureXYZ UCF in LocalDFNUnconfinedFractures)
            {
                // If the stress shadow width ratio is zero, there will be no stress shadow interaction so we can return false
                if (StressShadowWidthRatio <= 0)
                    return false;

                // Get the minimum and maximum radius of the exclusion zone around this fracture
                // These may be different because the effective fracture radius, which controls the stress around the fracture, may be different to the mean fracture radius 
                // The maximum radius, i.e. the radius in the plane of the fracture, will be the sum of the mean radius of this fracture plus the outer exclusion zone width
                double EZMaxWidth = UCF.MeanRayLength + MaxOuterExclusionZoneWidth;
                // The minimum radius, i.e. the radius perpendicular to the plane of the fracture, will be the sum of the effective radius of this fracture plus the outer exclusion zone width
                double EZMinWidth = UCF.EffectiveRadius + MinOuterExclusionZoneWidth;
                double EZWidthRatio = (EZMaxWidth > 0) ? EZMinWidth / EZMaxWidth : 1;

                // Calculate the ratio of the width of the exclusion zone perpendicular to the fracture centroid to the mean fracture radius
                double effectiveStressShadowMultiplier = (StressShadowWidthRatio / 2) * EZWidthRatio;

                // Get the vector between the centroid of this fracture and the specified point
                VectorXYZ centroidToPoint = new VectorXYZ(UCF.Centroid, point);

                // Convert the vector coordinates to the FDS frame for this fracture set (fracture normal, fracture dip, fracture strike)
                double Fcoord, Dcoord, Scoord;
                convertXYZVectortoFDSVector(centroidToPoint, out Fcoord, out Dcoord, out Scoord);

                // Adjust the Fcoordinate of the vector to take account of the stress shadow width to fracture radius ratio
                // This will have the effect of stretching the FDS coordinate space parallel to F to make the exclusion zone into a sphere
                Fcoord /= effectiveStressShadowMultiplier;

                // Now find whether the length of the stretched vector is less than the radius of the exclusion zone sphere
                double adjustedCentroidToPointDistance = Math.Sqrt((Fcoord * Fcoord) + (Dcoord * Dcoord) + (Scoord * Scoord));
                if (adjustedCentroidToPointDistance < EZMaxWidth)
                    return true;
            }

            // If the point does not lie in the exclusion zone around any of the fractures, return false
            return false;
        }
        /// <summary>
        /// Check whether a propagating unconfined fracture ray segment from this fracture set will terminate due to stress shadow interaction with of any of the other unconfined fractures in this set
        /// </summary>
        /// <param name="propagatingSegment">Reference to a UnconfinedFractureRaySegment object representing the propagating fracture ray segment</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this ray segment will propagate; this will be altered if the propagating ray segment interacts with another unconfined fracture stress shadow (m)</param>
        /// <param name="terminateIfInteracts">If true, automatically flag propagating ray segment as inactive due to stress shadow interaction; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment interacts with another macrofracture stress shadow, otherwise false</returns>
        public bool checkStressShadowInteraction(UnconfinedFractureRaySegment propagatingSegment, ref double propagationLength, bool terminateIfInteracts)
        {
            return checkStressShadowInteraction(propagatingSegment, this, ref propagationLength, 1, terminateIfInteracts);
        }
        /// <summary>
        /// Check whether a propagating unconfined fracture ray segment from this fracture set will terminate due to stress shadow interaction with of any of the other unconfined fractures in the explicit DFN associated with an unconfined fracture set in another gridblock
        /// </summary>
        /// <param name="propagatingSegment">Reference to a UnconfinedFractureRaySegment object representing the propagating fracture ray segment</param>
        /// <param name="interacting_ufs">Reference to a UnconfinedFractureSet object representing the fracture set which the propagating ray segment will interact with</param>
        /// <param name="propagationLength">Reference to variable containing the maximum length that this ray segment will propagate; this will be altered if the propagating ray segment interacts with another unconfined fracture stress shadow (m)</param>
        /// <param name="StressShadowWidthMultiplier">Multiplier for the stress shadow width to take account of misalignment between the fracture sets; if not known, set to 1</param>
        /// <param name="terminateIfInteracts">If true, automatically flag propagating ray segment as inactive due to stress shadow interaction; if false only update maximum propagation length</param>
        /// <returns>True if the propagating fracture segment interacts with another macrofracture stress shadow, otherwise false</returns>
        public bool checkStressShadowInteraction(UnconfinedFractureRaySegment propagatingSegment, UnconfinedFractureSet interacting_ufs, ref double propagationLength, double StressShadowWidthMultiplier, bool terminateIfInteracts)
        {
            // Set return value to false initially
            bool interacts = false;

            // Flag to indicate whether the propagating fracture segment is from the same fracture set (and hence gridblock) as the interacting set
            // NB This calculation assumes that both fractures are parallel
            // If the two fractures are in different gridblocks this may not be true
            // However we will assume the mismatch in alignments is small
            bool sameSet = (interacting_ufs == this);

            // Get the current stress shadow width ratio
            // If this is zero, there will be no stress shadow interaction so we can return false
            double stressShadowWidthRatio = getStressShadowWidthRatio(-1);
            if (stressShadowWidthRatio <= 0)
                return false;

            // Get the minimum effective radius of fractures that can deactivate the current ray by stress shadow interaction
            double minStressShadowInteractionRadius = gbc.PropControl.MinStressShadowDeactivationRatio * propagatingSegment.EffectiveRayLength;

            // Cache useful data locally
            VectorXYZ fractureNormalVector = NormalVector;
            VectorXYZ propagationDirection = propagatingSegment.UnitVector;
            VectorXYZ segmentAxis = (fractureNormalVector * propagationDirection).GetNormalisedVector();
            double initialRayLength = propagatingSegment.RayLength;
            double finalRayLength = initialRayLength + propagationLength;

            // Calculate the projected origin of the propagating ray by extrapolating back along the propagation direction
            PointXYZ rayTip = propagatingSegment.PropNode;
            PointXYZ projectedRayOrigin = propagatingSegment.PropNode;
            projectedRayOrigin.SubtractVector(initialRayLength * propagationDirection);
            PointXYZ projectedFinalRayTip = propagatingSegment.PropNode;
            projectedFinalRayTip.AddVector(propagationLength * propagationDirection);

            // Calculate the edge of the stress shadow perpendicular to the projected origin of the propagating ray
            // If the initial ray length is zero, the fracture has just nucleated so we can assume the ratio of effective ray length to actual ray length is 1
            double propagatingSegmentRayLengthRatio = (initialRayLength > 0) ? propagatingSegment.EffectiveRayLength / initialRayLength : 1;
            double propagatingSegmentStressShadowMultiplier = (stressShadowWidthRatio / 2) * StressShadowWidthMultiplier * propagatingSegmentRayLengthRatio;
            PointXYZ edgeOfRayStressShadow = new PointXYZ(projectedRayOrigin);
            edgeOfRayStressShadow.AddVector((finalRayLength * propagatingSegmentStressShadowMultiplier) * fractureNormalVector);

            // Loop through all the fractures in the interacting fracture set
            foreach (UnconfinedFractureXYZ UCF in interacting_ufs.LocalDFNUnconfinedFractures)
            {
                // Check if it is the parent fracture of the propagating segment; if so move on to the next
                if (propagatingSegment.IsSegmentInFracture(UCF))
                    continue;

                // Check if it has effective radius less than the minimum required for stress shadow interaction; if so move on to the next
                if (UCF.EffectiveRadius < minStressShadowInteractionRadius)
                    continue;

                // Cache the centrepoint and mean radius of this fracture locally
                PointXYZ fractureCentrepoint = UCF.Centroid;
                double fractureEffectiveRadius = UCF.MeanRayLength;

                // Determine whether the point of intersection of the fracture axis vector and the plane of the ray stress shadow lies within the fracture stress shadow
                // If it does not, the stress shadows do not interact and we can move on to the next fracture
                double distanceToAxisIntersection = PointXYZ.getIntersectionDistance(fractureCentrepoint, segmentAxis, projectedRayOrigin, projectedFinalRayTip, edgeOfRayStressShadow, CrossoverType.Extend);
                if (Math.Abs(distanceToAxisIntersection) > fractureEffectiveRadius)
                    continue;
                PointXYZ axis_rayStressShadow_intersection = new PointXYZ(fractureCentrepoint);
                axis_rayStressShadow_intersection.AddVector(distanceToAxisIntersection * segmentAxis);

                // Check to see if the vector from the propagating ray origin to the intersection point is in the same direction (within +/-90degrees) of the propagation direction
                // This will be the case if the scalar product of the two vectors is positive
                // If not, the ray is propagating in the wrong direction to interact with the fracture so we can move on to the next fracture
                VectorXYZ rayOriginToIntersection = new VectorXYZ(projectedRayOrigin, axis_rayStressShadow_intersection);
                if ((rayOriginToIntersection & propagationDirection) < 0)
                    continue;

                // Calculate the mean stress shadow width to ray length ratio for both fractures combined
                // NB This will be an approximation
                double combinedEffectiveRadius = propagatingSegment.EffectiveRayLength + UCF.EffectiveRadius;
                double combinedActualRadius = initialRayLength + UCF.MeanRayLength;
                double combinedRadiusRatio = (combinedActualRadius > 0) ? combinedEffectiveRadius / combinedActualRadius : 1;
                double combinedStressShadowMultiplier = (stressShadowWidthRatio / 2) * StressShadowWidthMultiplier * combinedRadiusRatio;
                // If the combined stress shadow multiplier is zero there will be no stress shadow so no stress shadow interaction; move on to the next fracture
                if (!(combinedStressShadowMultiplier > 0))
                    continue;

                // Check if the intersection point lies within the ray stress shadow
                double fx = fractureNormalVector.Component(VectorComponents.X);
                double fy = fractureNormalVector.Component(VectorComponents.Y);
                double fz = fractureNormalVector.Component(VectorComponents.Z);
                double rx = propagationDirection.Component(VectorComponents.X);
                double ry = propagationDirection.Component(VectorComponents.Y);
                double rz = propagationDirection.Component(VectorComponents.Z);
                double intersectionToRayOriginX = axis_rayStressShadow_intersection.X - projectedRayOrigin.X;
                double intersectionToRayOriginY = axis_rayStressShadow_intersection.Y - projectedRayOrigin.Y;
                double intersectionToRayOriginZ = axis_rayStressShadow_intersection.Z - projectedRayOrigin.Z;
                double frxy_factor = Math.Abs((fx * ry) - (fy * rx));
                double fryz_factor = Math.Abs((fy * rz) - (fz * ry));
                double frzx_factor = Math.Abs((fy * rx) - (fx * rz));
                // Find the best set of axes to calculate the distance from the ray origin to the intersection point, taking into account the squashing of the ray circle
                double adjustedIntersectionPointDistanceFromRayOrigin;
                if ((frxy_factor > fryz_factor) && (frxy_factor > frzx_factor))
                {
                    double wfpc_factor = combinedStressShadowMultiplier * ((fy * intersectionToRayOriginX) - (fx * intersectionToRayOriginY));
                    double rpc_factor = (ry * intersectionToRayOriginX) - (rx * intersectionToRayOriginY);
                    adjustedIntersectionPointDistanceFromRayOrigin = Math.Sqrt((wfpc_factor * wfpc_factor) + (rpc_factor * rpc_factor)) / (combinedStressShadowMultiplier * frxy_factor);
                }
                else if (fryz_factor > frzx_factor)
                {
                    double wfpc_factor = combinedStressShadowMultiplier * ((fz * intersectionToRayOriginY) - (fy * intersectionToRayOriginZ));
                    double rpc_factor = (rz * intersectionToRayOriginY) - (ry * intersectionToRayOriginZ);
                    adjustedIntersectionPointDistanceFromRayOrigin = Math.Sqrt((wfpc_factor * wfpc_factor) + (rpc_factor * rpc_factor)) / (combinedStressShadowMultiplier * fryz_factor);
                }
                else
                {
                    double wfpc_factor = combinedStressShadowMultiplier * ((fx * intersectionToRayOriginZ) - (fz * intersectionToRayOriginX));
                    double rpc_factor = (rx * intersectionToRayOriginZ) - (rz * intersectionToRayOriginX);
                    adjustedIntersectionPointDistanceFromRayOrigin = Math.Sqrt((wfpc_factor * wfpc_factor) + (rpc_factor * rpc_factor)) / (combinedStressShadowMultiplier * frzx_factor);
                }

                // If the adjusted distance from the intersection point to the ray origin is greater than the radius of the stress shadow around the ray, the two stress shadows may still intersect
                // We can check this by a geometric calculation
                // If the two stress shadows do not overlap, move onto the next fracture
                double fractureStressShadowRadius_projectedOntoRaySegmentPlane = Math.Sqrt((fractureEffectiveRadius * fractureEffectiveRadius) - (distanceToAxisIntersection * distanceToAxisIntersection));
                if ((adjustedIntersectionPointDistanceFromRayOrigin) > (finalRayLength + fractureStressShadowRadius_projectedOntoRaySegmentPlane))
                    continue;

                // The two stress shadows do overlap
                // We can easily find the ray length at which they first touch
                double rayLengthForStressShadowInteraction = adjustedIntersectionPointDistanceFromRayOrigin - fractureStressShadowRadius_projectedOntoRaySegmentPlane;
                // If the ray length at which they first touch is greater than the initial effective ray length, then two stress shadows already overlap before any propagation
                // In this case we will set the propagation distance to zero
                double propagationLengthToStressShadowInteraction = rayLengthForStressShadowInteraction - initialRayLength;
                if (propagationLengthToStressShadowInteraction < 0)
                    propagationLengthToStressShadowInteraction = 0;

                // Set the return value to true
                interacts = true;

                // Reduce the maximum propagation distance accordingly
                if (propagationLength > propagationLengthToStressShadowInteraction)
                    propagationLength = propagationLengthToStressShadowInteraction;
                //else
                //    propagationLength = propagationLength;

                // Set the propagating macrofracture segment to inactive, due to stress shadow interaction, and set reference to terminating macrofracture segment
                if (terminateIfInteracts)
                {
                    propagatingSegment.PropNodeType = SegmentNodeType.ConnectedStressShadow;
                    propagatingSegment.TerminatingFracture = UCF;
                }
            }

            return interacts;
        }
        /// <summary>
        /// Check if a propagating unconfined fracture ray segment will intersect any of the fractures in a specified unconfined fracture set
        /// </summary>
        /// <param name="propagatingSegment">Reference to the propagating UnconfinedFractureRaySegment</param>
        /// <param name="intersecting_ufs">Reference to the UnconfinedFractureSet to check for intersection</param>
        /// <param name="MaxPropagationLength">Reference to the current maximum propagation length of the propagating ray segment; this will be updated if intersection is detected</param>
        /// <param name="TerminateIfIntersects">If true, automatically flag propagating fracture ray segment as inactive due to intersection; if false only update maximum propagation length</param>
        /// <returns>True if the propagating unconfined fracture ray segment will intersect any of the fractures in the specified unconfined fracture set; otherwise false</returns>
        public bool checkUnconfinedFractureIntersection(UnconfinedFractureRaySegment propagatingSegment, UnconfinedFractureSet intersecting_ufs, ref double MaxPropagationLength, bool TerminateIfIntersects)
        {
            // Set return value to false initially
            bool intersects = false;

            // Get the minimum effective radius of fractures that the current ray will terminate against if it intersects
            double minIntersectionRadius = gbc.PropControl.MinIntersectionDeactivationRatio * propagatingSegment.EffectiveRayLength;

            // Get the propagation direction and start point
            PointXYZ startPoint = propagatingSegment.PropNode;
            VectorXYZ propagationVector = propagatingSegment.UnitVector;

            // Loop through all the fractures in the supplied fracture set
            foreach (UnconfinedFractureXYZ fracture in intersecting_ufs.LocalDFNUnconfinedFractures)
            {
                // If the effective radius of the intersecting fracture is below the cutoff, ignore it and move on to the next
                if (fracture.EffectiveRadius < minIntersectionRadius)
                    continue;

                // Get a list of triangular segments comprising the fracture surface
                List<PointXYZ[]> fractureSegments = fracture.GetTriangularFractureSegmentsInXYZ();

                // Go through each triangular fracture segment and find the distance the propagating ray segment needs to propagate in order to intersect it
                foreach (PointXYZ[] fractureSegment in fractureSegments)
                {
                    if (fractureSegment.Length < 3)
                        continue;

                    double distanceToIntersection = PointXYZ.getIntersectionDistance(startPoint, propagationVector, fractureSegment[0], fractureSegment[1], fractureSegment[2], CrossoverType.Restrict);

                    // If the propagating ray segment will never intersect the fracture segment, the function will return NaN or a negative value
                    if (distanceToIntersection > 0)
                    {
                        // If it does intersect the fracture segment, check if the distance to the intersection point is less than the current maximum propagation distance
                        if (distanceToIntersection < MaxPropagationLength)
                        {
                            // If so, set the return value to true
                            intersects = true;

                            // Reduce the maximum propagation distance of the propagating fracture ray segment accordingly
                            MaxPropagationLength = distanceToIntersection;

                            // Set the propagating fracture ray segment to inactive, due to intersection, and set reference to terminating fracture
                            if (TerminateIfIntersects)
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.Intersection;
                                propagatingSegment.TerminatingFracture = fracture;
                            }
                        }
                    }
                }
            }

            return intersects;
        }
        /// <summary>
        /// Container for the four cornerpoints of a gridblock boundary
        /// </summary>
        private class BoundaryCornerpoints
        {
            // Data
            /// <summary>
            /// Orientation of the boundary
            /// </summary>
            public GridDirection Boundary;
            /// <summary>
            /// List of the cornerpoints, in order: top left, top right, bottom right, bottom left looking out from the centre of the gridblock
            /// </summary>
            public PointXYZ[] CornerPoints;

            // Constructors
            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="Boundary_in"Orientation of the boundary></param>
            /// <param name="TopLeftCorner">Position in XYZ coordinates of the top left corner</param>
            /// <param name="TopRightCorner">Position in XYZ coordinates of the top right corner</param>
            /// <param name="BottomRightCorner">Position in XYZ coordinates of the bottom right corner</param>
            /// <param name="BottomLeftCorner">Position in XYZ coordinates of the bottom left corner</param>
            public BoundaryCornerpoints(GridDirection Boundary_in, PointXYZ TopLeftCorner, PointXYZ TopRightCorner, PointXYZ BottomRightCorner, PointXYZ BottomLeftCorner)
            {
                Boundary = Boundary_in;

                CornerPoints = new PointXYZ[4];
                CornerPoints[0] = TopLeftCorner;
                CornerPoints[1] = TopRightCorner;
                CornerPoints[2] = BottomRightCorner;
                CornerPoints[3] = BottomLeftCorner;
            }
        }
        /// <summary>
        /// Check whether a propagating unconfined fracture ray segment will intersect a gridblock boundary
        /// </summary>
        /// <param name="propagatingSegment">Reference to the propagating UnconfinedFractureRaySegment</param>
        /// <param name="MaxPropagationLength">Reference to the current maximum propagation length of the propagating ray segment; this will be updated if intersection is detected</param>
        /// <param name="boundaryCrossed">Reference to a GridDirection enum; this will be set to indicate which boundary is intersected</param>
        /// <param name="terminateIfIntersects">If true, update propagating ray segment status and flags; if false only update maximum propagation length</param>
        /// <param name="terminateIfNoNeighbour">If true, deactivate propagating ray segment and reduce maximum propagation length even if neighbouring gridblock is null; if false only deactivate propagating ray segment and reduce maximum propagation length if neighbouring gridblock is defined</param>
        /// <returns>True if the propagating unconfined fracture ray segment intersects a gridblock boundary, otherwise false</returns>
        public bool checkBoundaryIntersection(UnconfinedFractureRaySegment propagatingSegment, ref double MaxPropagationLength, out GridDirection boundaryCrossed, bool terminateIfIntersects, bool terminateIfNoNeighbour)
        {
            // Set the return value and the boundary crossed parameter to null to false initially
            bool crossesBoundary = false;
            boundaryCrossed = GridDirection.None;

            // Get the propagation direction and start point
            PointXYZ startPoint = propagatingSegment.PropNode;
            VectorXYZ propagationVector = propagatingSegment.UnitVector;

            // Start by creating a list of cornerpoints for each boundary
            List<BoundaryCornerpoints> boundaries = new List<BoundaryCornerpoints>();
            boundaries.Add(new BoundaryCornerpoints(GridDirection.N, gbc.NWtop, gbc.NEtop, gbc.NEbottom, gbc.NWbottom));
            boundaries.Add(new BoundaryCornerpoints(GridDirection.E, gbc.NEtop, gbc.SEtop, gbc.SEbottom, gbc.NEbottom));
            boundaries.Add(new BoundaryCornerpoints(GridDirection.S, gbc.SEtop, gbc.SWtop, gbc.SWbottom, gbc.SEbottom));
            boundaries.Add(new BoundaryCornerpoints(GridDirection.W, gbc.SWtop, gbc.NWtop, gbc.NWbottom, gbc.SWbottom));
            boundaries.Add(new BoundaryCornerpoints(GridDirection.D, gbc.NWbottom, gbc.NEbottom, gbc.SEbottom, gbc.SWbottom));
            boundaries.Add(new BoundaryCornerpoints(GridDirection.U, gbc.NEtop, gbc.NWtop, gbc.SWtop, gbc.SEtop));

            // Loop through the six gridblock boundaries checking for intersection
            foreach (BoundaryCornerpoints boundary in boundaries)
            {
                // Check if the ray segment nucleated on this boundary - if so move onto the next boundary
                if (boundary.Boundary == propagatingSegment.NonPropNodeBoundary)
                    continue;

                // Each boundary can be split into two triangular segments to check for intersection
                // However since these are coplanar we only need to check one of them if we set crossover type to extend
                double distanceToIntersection = PointXYZ.getIntersectionDistance(startPoint, propagationVector, boundary.CornerPoints[0], boundary.CornerPoints[1], boundary.CornerPoints[2], CrossoverType.Extend);

                // If the propagating ray segment will never intersect the boundary, the function will return a negative value
                if (distanceToIntersection > 0)
                {
                    // If it does intersect the fracture segment, check if the distance to the intersection point is less than the current maximum propagation distance
                    if (distanceToIntersection < MaxPropagationLength)
                    {
                        // Set the return value to true
                        crossesBoundary = true;

                        // Set the reference to the intersecting boundary, and check if neighbouring gridblock is null
                        boundaryCrossed = boundary.Boundary;
                        bool NoNeighbour = (gbc.NeighbourGridblocks[boundaryCrossed] == null);

                        // Reduce the maximum propagation distance - if the neighbour is non-null or if we have specified to terminate even if neighbour is null
                        if (!NoNeighbour || terminateIfNoNeighbour)
                            MaxPropagationLength = distanceToIntersection;

                        // Set the propagating fracture ray segment status and flags, if we have specified to do so
                        // The exact status and flags will depend on the boundary type and input settings
                        // NB there is no terminating fracture so we set this reference to null
                        if (terminateIfIntersects)
                        {
                            if (!NoNeighbour) // If the neighbour is non-null, set NodeType to ConnectedGridblockBound 
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.ConnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                propagatingSegment.TerminatingFracture = null;
                            }
                            else if (terminateIfNoNeighbour) // If the neighbour is null but we have specified to terminate the ray segment anyway, set NodeType to NonconnectedGridblockBound
                            {
                                propagatingSegment.PropNodeType = SegmentNodeType.NonconnectedGridblockBound;
                                propagatingSegment.PropNodeBoundary = boundaryCrossed;
                                propagatingSegment.TerminatingFracture = null;
                            }
                            // If we have specified not to terminate the fracture when the neighbour is null, leave the ray segment as active and NodeType as Propagating
                        }
                    }
                }
            }

            // Return true if the propagating segment will intersect a boundary, otherwise false
            return crossesBoundary;
        }
        /// <summary>
        /// Check if a propagating unconfined fracture ray segment will reach the specified maximum length
        /// </summary>
        /// <param name="propagatingSegment">Reference to the propagating UnconfinedFractureRaySegment</param>
        /// <param name="MaxPropagationLength">Reference to the current maximum propagation length of the propagating ray segment; this will be updated if intersection is detected</param>
        /// <param name="TerminateIfArrests">If true, automatically flag propagating fracture ray segment as arrested; if false only update maximum propagation length</param>
        /// <returns>True if the propagating unconfined fracture ray segment will intersect any of the fractures in the specified unconfined fracture set; otherwise false</returns>
        public bool checkMaximumLength(UnconfinedFractureRaySegment propagatingSegment, ref double MaxPropagationLength, bool TerminateIfArrests)
        {
            bool reachesMaximumLength = false;

            // Check if the propagation increment will cause the ray to reach or exceed the maximum fracture radius
            if ((propagatingSegment.RayLength + MaxPropagationLength) >= MaximumFractureRadius)
            {
                // Set the return value to true
                reachesMaximumLength = true;

                // Reduce the maximum propagation distance of the propagating fracture ray segment accordingly
                MaxPropagationLength = MaximumFractureRadius - propagatingSegment.RayLength;

                // Set the propagating fracture ray segment to inactive, due to arrest
                if (TerminateIfArrests)
                {
                    propagatingSegment.PropNodeType = SegmentNodeType.Arrested;
                }
            }

            return reachesMaximumLength;
        }

        // Reset and data input functions
        /// <summary>
        /// Clear the implicit fracture population arrays and reset them to the initial state (comprising a single fully active fracture datapoint representing initial microfractures)
        /// </summary>
        public void clearImplicitFracturePopulationArrays()
        {
            double initialRP30 = InitialP30() * (double)RaysPerFracture;
            Fractures.ResetPopulationDistributionData(initialRP30, MinimumFractureRadius, gbc.PropControl.MinStressShadowDeactivationRatio, gbc.PropControl.MinIntersectionDeactivationRatio);
        }
        /// <summary>
        /// Reset all fracture data to initial values (implicit population comprising only initial microfractures, no explicit fractures, no previous deformation)
        /// </summary>
        /// <param name="raysPerFracture_in">Number of rays comprising each fracture</param>
        /// <param name="rmin_in">Minimum radius for a fracture; this will be the length of the rays at nucleation</param>
        /// <param name="uFDistributionIn">Initial microfracture distribution function</param>
        /// <param name="B_in">Initial microfracture density coefficient B (/m3)</param>
        /// <param name="c_in">Initial microfracture distribution coefficient c</param>
        /// <param name="uFrmedian_in">Median initial microfracture radius - this is only used for the log-normal distribution function</param>
        public void resetFractureData(ushort raysPerFracture_in, double rmin_in, double rmax_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in, double uFrmedian_in)
        {
            // Set the initial fracture distribution data
            InitialDistribution = uFDistributionIn;
            CapB = B_in;
            c_coefficient = c_in;
            Median_uF_radius = uFrmedian_in;
            M_term = Math.Log(uFrmedian_in);
            Sqrt2_S = Math.Sqrt(2) * c_in;

            // Set the implicit fracture population data 
            // Number of rays comprising each fracture
            RaysPerFracture = raysPerFracture_in;
            // Minimum radius for a fracture; this will be the length of the rays at nucleation
            MinimumFractureRadius = rmin_in;
            // Maximum allowed radius for a fracture; rays will stop propagating when they reach this length
            MaximumFractureRadius = rmax_in;
            // Create new UnconfinedFractureData object
            double initialRP30 = InitialP30() * (double)raysPerFracture_in;
            Fractures = new UnconfinedFractureData(initialRP30, rmin_in, gbc.PropControl.MinStressShadowDeactivationRatio, gbc.PropControl.MinIntersectionDeactivationRatio, this);

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
            // Set the counter for consecutive failed nucleation attempts to 0
            failedNucleationAttempts = 0;

            // Set the unrestricted RP30 (i.e. maximum potential RP30 with no stress shadow deactivation) to zero
            previous_LRP30 = 0;
            // Set the unrestricted count of explicit fractures in this gridblock (i.e. maximum potential number of nucleated fractures with no stress shadow deactivation) to zero
            previous_Ln = 0;

            // Set the minimum RP30 value for nucleating and growing fracture datapoints
            // Both these are initially set to the value that will generate the specified maximum dP33 increment with fractures of maximum size
            // The minimum RP30 value for growing fracture datapoints will not change, but the minimum RP30 value for nucleating fracture datapoints will be recalculated at each timestep based on the mean static ray length
            double dP33 = gbc.PropControl.max_TS_UCFP33_increase;
            double maxFracVol = (4d / 3d) * Math.PI * Math.Pow(MaximumFractureRadius, 3);
            min_NucleatingDatapoint_RP30 = (dP33 / maxFracVol) * (double)raysPerFracture_in;
            min_GrowingDatapoint_RP30 = (dP33 / maxFracVol) * (double)raysPerFracture_in;
        }
        /// <summary>
        /// Set the fracture orientation data: dip, strike, normal vector and azimuth
        /// </summary>
        /// <param name="Strike_in">Fracture strike (radians)</param>
        /// <param name="Dip_in">Fracture dip (radians)</param>
        private void setOrientation(double Strike_in, double Dip_in)
        {
            // Trim values, and ensure azimuth is clockwise of strike
            if (Dip_in < 0)
            {
                Dip_in = -Dip_in;
                Strike_in += Math.PI;
            }
            if (Dip_in > (Math.PI / 2))
            {
                Dip_in = Math.PI - Dip_in;
                Strike_in += Math.PI;
            }
            while (Strike_in > (2 * Math.PI))
                Strike_in -= (2 * Math.PI);
            while (Strike_in < 0)
                Strike_in += (2 * Math.PI);

            // Set the sine and cosine of fracture dip
            sindip = VectorXYZ.Sin_trim(Dip_in);
            cosdip = VectorXYZ.Cos_trim(Dip_in);

            // Create new vectors for strike, dip, azimuth and fracture normal
            strikeVector = VectorXYZ.GetLineVector(Strike_in, 0);
            dipVector = VectorXYZ.GetLineVector(Strike_in + (Math.PI / 2), Dip_in);
            azimuthVector = VectorXYZ.GetLineVector(Strike_in + (Math.PI / 2), 0);
            normalVector = VectorXYZ.GetNormalToPlane(Strike_in + (Math.PI / 2), Dip_in);

            // Set the dip and strike values
            dip = Dip_in;
            strike = Strike_in;
        }
        /// <summary>
        /// Set the   fracture aperture control data for uniform and size-dependent aperture
        /// </summary>
        /// <param name="UniformAperture_in">Fixed aperture for fractures in the uniform aperture case (m)</param>
        /// <param name="SizeDependentApertureMultiplier_in">Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier</param>
        public void SetFractureApertureControlData(double UniformAperture_in, double SizeDependentApertureMultiplier_in)
        {
            // Set fracture aperture control data for uniform and size-dependent aperture
            // Fixed aperture for fractures in the uniform aperture case (m)
            UniformAperture = UniformAperture_in;
            // Multiplier for fracture aperture in the size-dependent aperture case - layer-bound fracture aperture is given by layer thickness times this multiplier
            SizeDependentApertureMultiplier = SizeDependentApertureMultiplier_in;
        }

        // Constructors
        /// <summary>
        /// Default constructor: set default values 
        /// </summary>
        /// <param name="gbc_in">Reference to parent GridblockConfiguration object</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in)
                    : this(gbc_in, 0, Math.PI / 2, 8, 0.1, 100, InitialFractureDistribution.PowerLaw, 0.001, 3d, 0)
        {
            // Defaults:

            // Fracture orientation: set to vertical N-striking fractures
            // Number of rays per fracture: set to 8
            // Minimum fracture radius: set to 0.1
            // Minimum fracture radius: set to 100
            // Initial microfracture distribution - set to power law, B=0.001, c=3, M=0 (undefined)
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
        /// <param name="uFrmedian_in">Median initial microfracture radius - this is only used for the log-normal distribution function</param>
        public UnconfinedFractureSet(GridblockConfiguration gbc_in, double Strike_in, double Dip_in, int raysPerFracture_in, double rmin_in, double rmax_in, InitialFractureDistribution uFDistributionIn, double B_in, double c_in, double uFrmedian_in)
        {
            // Reference to parent GridblockConfiguration object
            gbc = gbc_in;

            // Set fracture orientation
            setOrientation(Strike_in, Dip_in);

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
            resetFractureData((ushort)raysPerFracture_in, rmin_in, rmax_in, uFDistributionIn, B_in, c_in, uFrmedian_in);

            // Create an empty list for the local gridblock DFN
            LocalDFNUnconfinedFractures = new List<UnconfinedFractureXYZ>();

            // Set the maximum historic active fracture volumetric ratio to 0
            max_historic_a_UCFP32 = 0;
        }
    }
}
