using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for the fracture ray propagation status: FullyActive (i.e. all rays in the fracture are active), Restricted (i.e. at least one other ray in the fracture is deactivated), StaticStressShadow (i.e. this ray is deactivated due to stress shadow interaction), StaticIntersection (i.e. this ray is deactivated due to intersecting a fracture from another set)
    /// </summary>
    enum RayPropagationStatus { FullyActive, Restricted, StaticStressShadow, StaticIntersection }

    /// <summary>
    /// Object representing a specific point in an implicit fracture population distribution function array
    /// </summary>
    class ImplicitFracturePopulationDatapoint : IComparable<ImplicitFracturePopulationDatapoint>
    {
        // Control and implementation functions
        /// <summary>
        /// Compare ImplicitFracturePopulationDatapoint objects based on ray length
        /// </summary>
        /// <param name="that">ImplicitFracturePopulationDatapoint object to compare with</param>
        /// <returns>Positive if this ImplicitFracturePopulationDatapoint represents the longest rays, negative if that FractureDipSet ImplicitFracturePopulationDatapoint represents the longest rays, zero if they represent rays of equal length</returns>
        public int CompareTo(ImplicitFracturePopulationDatapoint that)
        {
            return this.RayLength.CompareTo(that.RayLength);
        }

        // Basic data
        /// <summary>
        /// Current length of rays represented by this datapoint
        /// </summary>
        public double RayLength { get; private set; }
        /// <summary>
        /// Incremental increase in the ray length in the current timestep
        /// </summary>
        public double RayLengthIncrement { get; set; }
        /// <summary>
        /// Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)
        /// </summary>
        public double dP30 { get; private set; }
        /// <summary>
        /// Number of rays comprising each fracture; this is used to calculate the volumetric density of fractures represented by this datapoint
        /// </summary>
        public static ushort NoRaysPerFracture {private get; set; }
        // Ray propagation status
        /// <summary>
        /// Propagation status of rays represented by this datapoint 
        /// </summary>
        public RayPropagationStatus Status { get; private set; }
        /// <summary>
        /// Length of the ray controlling the propagation rate of this ray; once set, this will not change
        /// </summary>
        public double PropagationControllingLength { get; private set; }

        // Properties and functions to extract data
        /// <summary>
        /// Effective ray length, used when calculating aperture, stress shadow volume or propagation rate
        /// </summary>
        public double EffectiveRayLength { get { return (Status == RayPropagationStatus.FullyActive) ? RayLength : (RayLength + EffectiveRayLength) / 2; } }
        /// <summary>
        /// Effective incremental increase in the ray length, used when calculating aperture, stress shadow volume or propagation rate
        /// </summary>
        public double EffectiveRayLengthIncrement
        {
            get
            {
                switch (Status)
                {
                    case RayPropagationStatus.FullyActive:
                        return RayLengthIncrement;
                    case RayPropagationStatus.Restricted:
                        return RayLengthIncrement / 2;
                    case RayPropagationStatus.StaticStressShadow:
                    case RayPropagationStatus.StaticIntersection:
                        return 0;
                    default:
                        return 0;
                }
            }
        }
        /// <summary>
        /// Factor to calculate incremental area of segments represented by this datapoint; must be multiplied by a geometric factor to get the true dP32 increment
        /// </summary>
        public double dP32factor { get { return dP30 * RayLength * RayLength; } }
        /// <summary>
        /// Factor to calculate incremental stress shadow volume around the segments represented by this datapoint; must be multiplied by a geometric factor to get the true dP33 increment
        /// </summary>
        public double dP33factor { get { return dP30 * RayLength * RayLength * EffectiveRayLength; } }
        /// <summary>
        /// Get the total volume of the exclusion zones around fracture segments represented by another datapoint as seen by rays represented by this datapoint
        /// </summary>
        /// <param name="Fracture2">Reference to another implicit fracture population distribution function datapoint, representing the segments around which the exclusion zones lie</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius for this set</param>
        /// <returns>Total volume of the exclusion zones around fracture segments represented by the specified datapoint as seen by rays represented by this datapoint</returns>
        public double GetExclusionZoneVolume(ImplicitFracturePopulationDatapoint Fracture2, double StressShadowWidthRatio)
        {
            double EZLength = this.RayLength + Fracture2.RayLength;
            double EZWidth = (this.EffectiveRayLength + Fracture2.EffectiveRayLength) * StressShadowWidthRatio;
            return (4d / 3d) * Math.PI * (Fracture2.dP30 / (double)NoRaysPerFracture) * EZLength * EZLength * EZWidth;
        }
        /// <summary>
        /// Get the inverse of the total volume of the interaction zones around fracture segments represented by another datapoint as seen by rays represented by this datapoint
        /// The interaction zone is the zone surrounding the exclusion zone; if the centrepoint of this fracture lies within the interaction zone of another fracture, the two stress shadows will intersect in the next timestep
        /// </summary>
        /// <param name="Fracture2">Reference to another implicit fracture population distribution function datapoint, representing the segments around which the exclusion zones lie</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius for this set</param>
        /// <returns>Inverse of the total volume of the interaction zones around fracture segments represented by the specified datapoint as seen by rays represented by this datapoint</returns>
        public double GetNonInteractionZoneVolume(ImplicitFracturePopulationDatapoint Fracture2, double StressShadowWidthRatio)
        {
            double EZLength = this.RayLength + Fracture2.RayLength;
            double EZWidth = (this.EffectiveRayLength + Fracture2.EffectiveRayLength) * StressShadowWidthRatio;
            double IZLength = EZLength + this.RayLengthIncrement + Fracture2.RayLengthIncrement;
            double IZWidth = EZWidth + ((this.EffectiveRayLengthIncrement + Fracture2.EffectiveRayLengthIncrement) * StressShadowWidthRatio);

            double IZShellVolume = (4d / 3d) * Math.PI * ((IZLength * IZLength * IZWidth) - (EZLength * EZLength * EZWidth));
            return Math.Pow(1 - IZShellVolume, Fracture2.dP30 / (double)NoRaysPerFracture);
        }

        // Functions to manipulate data
        /// <summary>
        /// Add the increment in ray length to the current ray length, then reset the increment in ray length to zero
        /// </summary>
        public void IncrementRayLength()
        {
            RayLength += RayLengthIncrement;
            RayLengthIncrement = 0;
        }
        /// <summary>
        /// Deactivate a specified proportion of rays
        /// </summary>
        /// <param name="PhiII_Ray_M">Probability that an active ray will not be deactivated due to stress shadow interaction during the current timestep</param>
        /// <param name="PhiIJ_Ray_M">Probability that an active ray will not be deactivated due to intersecting a fracture from another set during the current timestep</param>
        /// <param name="NoRaysPerFracture">Number of rays in each fracture</param>
        /// <returns></returns>
        public ImplicitFracturePopulationDatapoint[] DeactivateRays(double PhiII_Ray_M, double PhiIJ_Ray_M)
        {
            // Create an array for the output datapoints - these represent the rays that become deactivated or restricted
            ImplicitFracturePopulationDatapoint[] outputDatapoints;

            // Calculate the probabilities of deactivation due to stress shadow interaction of intersection during the timestep
            double Phi_Ray_M = PhiII_Ray_M * PhiIJ_Ray_M;
            double PhiII_ratio = (PhiII_Ray_M > 0) ? Math.Log(PhiII_Ray_M) / Math.Log(Phi_Ray_M) : 1;
            double PhiIJ_ratio = (PhiIJ_Ray_M > 0) ? Math.Log(PhiIJ_Ray_M) / Math.Log(Phi_Ray_M) : 1;
            double F_II_M = (PhiII_ratio > 0) ? (1 - Phi_Ray_M) * PhiII_ratio : 0;
            double F_IJ_M = (PhiIJ_ratio > 0) ? (1 - Phi_Ray_M) * PhiIJ_ratio : 0;

            switch (Status)
            {
                case RayPropagationStatus.FullyActive:
                    // There will be three datapoints in the output array, representing the rays that become restricted and deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[3];
                    // Calculate the total proportion of fractures for which no rays will be deactivated during the current timestep
                    // This will be the probability that a single ray will not be deactivated (Phi_Ray_M) to the power of the number of rays per fracture
                    double Phi_Fracture_M = Math.Pow(Phi_Ray_M, NoRaysPerFracture);
                    // The first datapoint in the output array represents the new restricted rays
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * (Phi_Ray_M - Phi_Fracture_M), RayPropagationStatus.Restricted, RayLength);
                    // The second datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_II_M, RayPropagationStatus.StaticStressShadow, RayLength);
                    // The third datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[2] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_IJ_M, RayPropagationStatus.StaticIntersection, RayLength);
                    // Reduce the volumetric density of fully active rays represented by this datapoint
                    dP30 *= Phi_Fracture_M;
                    // Return the new datapoints
                    return outputDatapoints;
                case RayPropagationStatus.Restricted:
                    // There will be two datapoints in the output array, representing the rays that become deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[1];
                    // The first datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_II_M, RayPropagationStatus.StaticStressShadow, RayLength);
                    // The second datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_IJ_M, RayPropagationStatus.StaticIntersection, RayLength);
                    // Reduce the volumetric density of restricted rays represented by this datapoint
                    dP30 *= Phi_Ray_M;
                    // Return the new datapoints
                    return outputDatapoints;
                case RayPropagationStatus.StaticStressShadow:
                case RayPropagationStatus.StaticIntersection:
                    // There will be no datapoints in the output array
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[0];
                    // Static rays are already deactivated so there will be no reduction in the volumetric density of static rays represented by this datapoint
                    return outputDatapoints;
                default:
                    return null;
            }
        }
        /// <summary>
        /// Increment the dP30 value for the current datapoint by a specified amount
        /// </summary>
        /// <param name="dP30_increment">Amount by which to increment the dP30 value</param>
        public void Increment_dP30(double dP30_increment)
        {
            dP30 += dP30_increment;
        }

        // Constructors
        /// <summary>
        /// Default constructor: Create a datapoint representing fully active rays of a specified length
        /// </summary>
        /// <param name="rayLength_in">Current length of rays represented by this datapoint</param>
        /// <param name="dP30_in">Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)</param>
        public ImplicitFracturePopulationDatapoint(double rayLength_in, double dP30_in) : this(rayLength_in, 0, dP30_in, RayPropagationStatus.FullyActive, rayLength_in)
        {
            // Defaults:
            // Incremental increase in the ray length in the current timestep: set to 0
            // Length of the ray controlling the propagation rate of this ray: For a fully active ray this will always be the ray length
        }
        /// <summary>
        /// Constructor: specify ray length, dP30, propagation status, and propagation controlling length
        /// </summary>
        /// <param name="rayLength_in">Current length of rays represented by this datapoint</param>
        /// <param name="rayLengthIncrement_in">Incremental increase in the ray length in the current timestep</param>
        /// <param name="dP30_in">Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)</param>
        /// <param name="status_in">Propagation status of rays represented by this datapoint</param>
        /// <param name="propagationControllingLength_in">Length of the ray controlling the propagation rate of this ray</param>
        private ImplicitFracturePopulationDatapoint(double rayLength_in, double rayLengthIncrement_in, double dP30_in, RayPropagationStatus status_in, double propagationControllingLength_in)
        {
            RayLength = rayLength_in;
            RayLengthIncrement = rayLengthIncrement_in;
            dP30 = dP30_in;
            Status = status_in;
            if (status_in == RayPropagationStatus.FullyActive)
                PropagationControllingLength = rayLength_in;
            else
                PropagationControllingLength = propagationControllingLength_in;
        }
    }

    /// <summary>
    ///Statistical data describing implicit population of unconfined circular fractures for a single fracture set
    /// </summary>
    class UnconfinedFractureData
    {
        // Population data for all fractures
        /// <summary>
        /// Volumetric density of fully active fracture rays (i.e. all rays in the fracture are active)
        /// </summary>
        public double a_RP30_total { get { return RP30_total[RayPropagationStatus.FullyActive]; } }
        /// <summary>
        /// Volumetric density of restricted fracture rays (i.e. at least one other ray in the fracture is deactivated)
        /// </summary>
        public double r_RP30_total { get { return RP30_total[RayPropagationStatus.Restricted]; } }
        /// <summary>
        /// Volumetric density of static fracture rays deactivated due to stress shadow interaction
        /// </summary>
        public double sII_RP30_total { get { return RP30_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Volumetric density of static fracture rays deactivated due to intersection
        /// </summary>
        public double sIJ_RP30_total { get { return RP30_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Mean linear density of fully active fracture rays (i.e. all rays in the fracture are active)
        /// </summary>
        public double a_RP32_total { get { return RP32_total[RayPropagationStatus.FullyActive]; } }
        /// <summary>
        /// Mean linear density of restricted fracture rays (i.e. at least one other ray in the fracture is deactivated)
        /// </summary>
        public double r_RP32_total { get { return RP32_total[RayPropagationStatus.Restricted]; } }
        /// <summary>
        /// Mean linear density of static fracture rays deactivated due to stress shadow interaction
        /// </summary>
        public double sII_RP32_total { get { return RP32_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Mean linear density of static fracture rays deactivated due to intersection
        /// </summary>
        public double sIJ_RP32_total { get { return RP32_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Volumetric ratio of fully active fracture rays (i.e. all rays in the fracture are active)
        /// </summary>
        public double a_RP33_total { get { return RP33_total[RayPropagationStatus.FullyActive]; } }
        /// <summary>
        /// Volumetric ratio of restricted fracture rays (i.e. at least one other ray in the fracture is deactivated)
        /// </summary>
        public double r_RP33_total { get { return RP33_total[RayPropagationStatus.Restricted]; } }
        /// <summary>
        /// Volumetric ratio of static fracture rays deactivated due to stress shadow interaction
        /// </summary>
        public double sII_RP33_total { get { return RP33_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Volumetric ratio of static fracture rays deactivated due to intersection
        /// </summary>
        public double sIJ_RP33_total { get { return RP33_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Dictionary object containing volumetric density of fracture rays for all ray propagation statuses
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP30_total;
        /// <summary>
        /// Dictionary object containing mean linear density of fracture rays for all ray propagation statuses
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP32_total;
        /// <summary>
        /// Dictionary object containing volumetric ratio of fracture rays for all ray propagation statuses
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_total;
        /// <summary>
        /// Dictionary object containing the adjustment factors for mean linear density of fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in area between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP32_total_adjustment;
        /// <summary>
        /// Dictionary object containing the adjustment factors for volumetric ratio of fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_total_adjustment;
        /// <summary>
        /// Recalculate the total population data from the piecewise population distribution function arrays
        /// </summary>
        public void RecalculateTotalPopulationData()
        {
            // Reset the total population values to zero
            ResetTotalPopulationData();

            // Recalculate each of the fracture population distribution functions
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Loop through the arrays for population distribution functions
                foreach (ImplicitFracturePopulationDatapoint dataPoint in fracturePopulationDatapoints[status])
                {
                    double effectiveLength = dataPoint.RayLength;
                    RP30_total[status] += dataPoint.dP30;
                    RP32_total[status] += dataPoint.dP32factor;
                    RP33_total[status] += dataPoint.dP33factor;
                }

                // Add the adjustment factors to the P32 and P33 values
                RP32_total[status] += RP32_total_adjustment[status];
                RP33_total[status] += RP33_total_adjustment[status];

                // Apply the area and volume multipliers
                RP32_total[status] *= (Math.PI / (double)noSegments);
                RP33_total[status] *= (4 / 3) * (Math.PI / (double)noSegments);
            }
        }

        // Fracture population distribution functions
        /// <summary>
        /// Arrays for piecewise population distribution functions
        /// </summary>
        public Dictionary<RayPropagationStatus, List<ImplicitFracturePopulationDatapoint>> fracturePopulationDatapoints;
        /// <summary>
        /// Number of segments in each fracture (a segment is the part of the fracture surrounding each ray)
        /// </summary>
        private ushort noSegments { get; set; }

        // Reset and data input functions
        /// <summary>
        /// Reset the total population values to zero 
        /// </summary>
        private void ResetTotalPopulationData()
        {
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                RP30_total[status] = 0;
                RP32_total[status] = 0;
                RP33_total[status] = 0;
            }
        }
        /// <summary>
        /// Reset the arrays for the piecewise population distribution functions
        /// </summary>
        public void ResetPopulationDistributionData()
        {
            // Defaults
            // Initial volumetric fracture density set to zero
            // Initial fracture radius set to zero
            // Number of rays comprising each fracture set to 8
            ResetPopulationDistributionData(0, 0, 8);
        }
        /// <summary>
        /// Reset the total population values and the arrays for the piecewise population distribution functions
        /// </summary>
        /// <param name="a_RP30_initial">Initial volumetric density of fractures</param>
        /// <param name="rmin">Radius of initial fractures</param>
        /// <param name="raysPerFracture">Number of rays comprising each fracture (equal to the number of segments per fracture)</param>
        public void ResetPopulationDistributionData(double a_RP30_initial, double rmin, ushort raysPerFracture)
        {
            // Set the number of segments per fracture
            noSegments = raysPerFracture;
            ImplicitFracturePopulationDatapoint.NoRaysPerFracture = raysPerFracture;

            // Reset the population total population adjustment factors
            // The adjustment factors arise when the size of the population distribution function arrays are reduced by amalgamating datapoints
            // They represent the difference in area or volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
            RP32_total_adjustment = new Dictionary<RayPropagationStatus, double>();
            RP33_total_adjustment = new Dictionary<RayPropagationStatus, double>();
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                RP32_total_adjustment[status] = 0;
                RP33_total_adjustment[status] = 0;
            }

            // Reset the population arrays
            fracturePopulationDatapoints = new Dictionary<RayPropagationStatus, List<ImplicitFracturePopulationDatapoint>>();
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                fracturePopulationDatapoints[status] = new List<ImplicitFracturePopulationDatapoint>();

            // Add a datapoint to the active population array for the initial active fracture population
            if (a_RP30_initial > 0)
                fracturePopulationDatapoints[RayPropagationStatus.FullyActive].Add(new ImplicitFracturePopulationDatapoint(rmin, a_RP30_initial));

            // Reset and recalculate the total population values
            RP30_total = new Dictionary<RayPropagationStatus, double>();
            RP32_total = new Dictionary<RayPropagationStatus, double>();
            RP33_total = new Dictionary<RayPropagationStatus, double>();
            RecalculateTotalPopulationData();
        }
        /// <summary>
        /// Reduce the size of a specified population distribution function array by amalgamating datapoints at a specified regular interval
        /// </summary>
        /// <param name="ArrayToCull">Specified datapoint array</param>
        /// <param name="PointSpacing">Number of points to amalgamate into a single datapoint</param>
        public void CullArray(RayPropagationStatus ArrayToCull, ushort PointSpacing)
        {
            double P32_adjustment = 0;
            double P33_adjustment = 0;
            List<ImplicitFracturePopulationDatapoint> listToCull = fracturePopulationDatapoints[ArrayToCull];
            int dataPointNo = listToCull.Count - 1;
            for (int nextPointToKeep = (dataPointNo / PointSpacing) * PointSpacing; nextPointToKeep >= 0; nextPointToKeep -= PointSpacing)
            {
                double dP30_increment = 0;
                double nextPoint_Radius = listToCull[nextPointToKeep].RayLength;
                double nextPoint_EffectiveRadius = listToCull[nextPointToKeep].EffectiveRayLength;
                double nextPoint_Area = nextPoint_Radius * nextPoint_Radius;
                double nextPoint_Volume = nextPoint_Area * nextPoint_EffectiveRadius;
                for (; dataPointNo > nextPointToKeep; dataPointNo--)
                {
                    double thisPoint_dP30 = listToCull[dataPointNo].dP30;
                    double thisPoint_Radius = listToCull[dataPointNo].RayLength;
                    double thisPoint_EffectiveRadius = listToCull[dataPointNo].EffectiveRayLength;
                    double thisPoint_Area = thisPoint_Radius * thisPoint_Radius;
                    double thisPoint_Volume = thisPoint_Area * thisPoint_EffectiveRadius;
                    dP30_increment += thisPoint_dP30;
                    P32_adjustment += thisPoint_dP30 * (thisPoint_Area - nextPoint_Area);
                    P33_adjustment += thisPoint_dP30 * (thisPoint_Volume - nextPoint_Volume);
                    listToCull.RemoveAt(dataPointNo);
                }
                listToCull[dataPointNo].Increment_dP30(dP30_increment);
                dataPointNo--;
            }

            // Update the adjustment factors
            RP32_total_adjustment[ArrayToCull] += P32_adjustment;
            RP33_total_adjustment[ArrayToCull] += P33_adjustment;
        }
        /// <summary>
        /// Reduce the size of a specified population distribution function array by amalgamating datapoints representing ray lengths within a specified ratio
        /// </summary>
        /// <param name="ArrayToCull">Specified datapoint array</param>
        /// <param name="LengthRatio">Ratio of ray lengths to amalgamate into a single datapoint</param>
        public void CullArray(RayPropagationStatus ArrayToCull, double LengthRatio)
        {
            double P32_adjustment = 0;
            double P33_adjustment = 0;
            List<ImplicitFracturePopulationDatapoint> listToCull = fracturePopulationDatapoints[ArrayToCull];

            for (int dataPointNo = 0; dataPointNo < listToCull.Count; dataPointNo++)
            {
                double maxRayLength = listToCull[dataPointNo].RayLength * LengthRatio;
                double dP30_increment = 0;
                double thisPoint_Radius = listToCull[dataPointNo].RayLength;
                double thisPoint_EffectiveRadius = listToCull[dataPointNo].EffectiveRayLength;
                double thisPoint_Area = thisPoint_Radius * thisPoint_Radius;
                double thisPoint_Volume = thisPoint_Area * thisPoint_EffectiveRadius;
                for (int nextDataPointNo = dataPointNo + 1; (nextDataPointNo < listToCull.Count) && (listToCull[nextDataPointNo].RayLength < maxRayLength);)
                {
                    double nextPoint_dP30 = listToCull[nextDataPointNo].dP30;
                    double nextPoint_Radius = listToCull[nextDataPointNo].RayLength;
                    double nextPoint_EffectiveRadius = listToCull[nextDataPointNo].EffectiveRayLength;
                    double nextPoint_Area = nextPoint_Radius * nextPoint_Radius;
                    double nextPoint_Volume = nextPoint_Area * nextPoint_EffectiveRadius;
                    dP30_increment += nextPoint_dP30;
                    P32_adjustment += nextPoint_dP30 * (nextPoint_Area - thisPoint_Area);
                    P33_adjustment += nextPoint_dP30 * (nextPoint_Volume - thisPoint_Volume);
                    listToCull.RemoveAt(nextDataPointNo);
                }
                listToCull[dataPointNo].Increment_dP30(dP30_increment);
            }

            // Update the adjustment factors
            RP32_total_adjustment[ArrayToCull] += P32_adjustment;
            RP33_total_adjustment[ArrayToCull] += P33_adjustment;
        }
        /// <summary>
        /// Save memory space by clearing all data from the population distribution function arrays
        /// </summary>
        public void ClearFracturePopulationDistributionArrays()
        {
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                fracturePopulationDatapoints[status].Clear();
        }

        // Constructors
        /// <summary>
        /// Default Constructor: initial state has no fractures and empty arrays for the piecewise cumulative population distribution functions
        /// </summary>
        public UnconfinedFractureData()
        {
            ResetPopulationDistributionData();
        }
        /// <summary>
        /// Constructor: Set an initial fracture population of minimum radius
        /// </summary>
        /// <param name="a_RP30_initial">Initial volumetric density of fractures</param>
        /// <param name="rmin">Radius of initial fractures</param>
        /// <param name="raysPerFracture">Number of rays comprising each fracture (equal to the number of segments per fracture)</param>
        public UnconfinedFractureData(double a_RP30_initial, double rmin, ushort raysPerFracture)
        {
            ResetPopulationDistributionData(a_RP30_initial, rmin, raysPerFracture);
        }
    }
}
