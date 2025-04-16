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
        /// Compare ImplicitFracturePopulationDatapoint objects based on effective ray length
        /// NB Comparison result is inverted so that the datapoints will be sorted in reverse order of effective ray length (largest to smallest)
        /// </summary>
        /// <param name="that">ImplicitFracturePopulationDatapoint object to compare with</param>
        /// <returns>Negative if this ImplicitFracturePopulationDatapoint represents the longest rays, positive if that FractureDipSet ImplicitFracturePopulationDatapoint represents the longest rays, zero if they represent rays of equal length</returns>
        public int CompareTo(ImplicitFracturePopulationDatapoint that)
        {
            return -this.EffectiveRayLength.CompareTo(that.EffectiveRayLength);
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
        // Data relating to ray deactivation
        /// <summary>
        /// Probability that an active ray has not been deactivated due to stress shadow interaction since the last deactivation check
        /// </summary>
        public double PhiII { get; private set; }
        /// <summary>
        /// Probability that an active ray has not been deactivated due to intersecting a fracture from another set since the last deactivation check
        /// </summary>
        public double PhiIJ { get; private set; }
        /// <summary>
        /// Effective ray length at the last deactivation check
        /// </summary>
        private double EffectiveRayLengthAtLastDeactivationCheck { get; set; }
        /// <summary>
        /// The proportional growth in the effective ray length since the last deactivation check
        /// </summary>
        public double ProportionalGrowthSinceLastDeactivationCheck { get { return (EffectiveRayLength + EffectiveRayLengthIncrement - EffectiveRayLengthAtLastDeactivationCheck) / EffectiveRayLengthAtLastDeactivationCheck; } }

        // Properties and functions to extract data
        /// <summary>
        /// Effective ray length, used when calculating aperture, stress shadow volume or propagation rate
        /// </summary>
        public double EffectiveRayLength { get { return (Status == RayPropagationStatus.FullyActive) ? RayLength : (RayLength + PropagationControllingLength) / 2; } }
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
        public double dP33factor { get { return  dP30 * RayLength * RayLength * EffectiveRayLength; } }
        /// <summary>
        /// Get the inverse of the exclusion zone volume seen by a fully active fracture with a specified radius, around all fractures represented by this datapoint
        /// </summary>
        /// <param name="Fracture2Radius">Radius of the fully active fracture</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius for this set</param>
        /// <returns>Inverse exclusion zone volume seen by the specified fracture</returns>
        public double GetInverseOuterExclusionZoneVolume(double Fracture2Radius, double StressShadowWidthRatio)
        {
            double EZLength = RayLength + Fracture2Radius;
            double EZWidth = EffectiveRayLength + Fracture2Radius;

            // Since the outer exclusion zone volumes can overlap, the total inverse outer exclusion zone volume musat be calculated by multiplying the inverse of the outer exclusion zone volume around each fracture
            // This can be done by raising the inverse exclusion zone volume around a single fracture to the power of the number of fractures
            double OEZShellVolume = (4d / 3d) * Math.PI * StressShadowWidthRatio * ((EZLength * EZLength * EZWidth) - (RayLength * RayLength * EffectiveRayLength));
            return Math.Exp(-OEZShellVolume * (dP30 / (double)NoRaysPerFracture));
        }
        /*/// <summary>
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
        }*/
        /// <summary>
        /// Get the inverse of the total volume of the interaction zones around fracture segments represented by this datapoint as seen by rays represented by another datapoint
        /// The interaction zone is the zone surrounding the exclusion zone; if the centrepoint of the other fracture lies within the interaction zone of this fracture, the two stress shadows will intersect in the next timestep
        /// </summary>
        /// <param name="Fracture2">Reference to another implicit fracture population distribution function datapoint, representing the propagating rays which are being checked</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius for this set</param>
        /// <returns>Inverse of the total volume of the interaction zones around fracture segments represented by this datapoint as seen by rays represented by the specified datapoint</returns>
        public double GetNonInteractionZoneVolume(ImplicitFracturePopulationDatapoint Fracture2, double StressShadowWidthRatio)
        {
            double EZLength = this.RayLength + Fracture2.RayLength;
            double EZWidth = (this.EffectiveRayLength + Fracture2.EffectiveRayLength) * StressShadowWidthRatio;
            double IZLength = EZLength + this.RayLengthIncrement + Fracture2.RayLengthIncrement;
            double IZWidth = EZWidth + ((this.EffectiveRayLengthIncrement + Fracture2.EffectiveRayLengthIncrement) * StressShadowWidthRatio);

            double IZShellVolume = (4d / 3d) * Math.PI * ((IZLength * IZLength * IZWidth) - (EZLength * EZLength * EZWidth));
            double output = Math.Exp(-IZShellVolume * (Fracture2.dP30 / (double)NoRaysPerFracture));
            return Math.Exp(-IZShellVolume * (Fracture2.dP30 / (double)NoRaysPerFracture));
        }

        // Functions to manipulate data
        /// <summary>
        /// Add the increment in ray length to the current ray length, then reset the increment in ray length to zero
        /// </summary>
        public void IncrementRayLength()
        {
            RayLength += RayLengthIncrement;
            if (Status == RayPropagationStatus.FullyActive)
                PropagationControllingLength += RayLengthIncrement;
            RayLengthIncrement = 0;
        }
        /// <summary>
        /// Update the fracture activation probabilities for the current timestep
        /// </summary>
        /// <param name="PhiII_M">Probability that an active ray will not be deactivated due to stress shadow interaction during the current timestep</param>
        /// <param name="PhiIJ_M">Probability that an active ray will not be deactivated due to intersecting a fracture from another set during the current timestep</param>
        public void UpdateFractureActivationProbabilities(double PhiII_M, double PhiIJ_M)
        {
            PhiII *= PhiII_M;
            PhiIJ *= PhiIJ_M;
            if (PhiII < 0)
                PhiII = 0;
            if (PhiIJ < 0)
                PhiIJ = 0;
        }
        /// <summary>
        /// Reset the fracture activation probabilites and the effective ray length at the last deactivation check
        /// Call this after deactivating the rays 
        /// </summary>
        private void ResetFractureActivationProbabilities()
        {
            PhiII = 1;
            PhiIJ = 1;
            EffectiveRayLengthAtLastDeactivationCheck = EffectiveRayLength;
        }
        /// <summary>
        /// Deactivate a proportion of currently active rays based on the current fracture activation probabilities
        /// </summary>
        /// <returns></returns>
        public ImplicitFracturePopulationDatapoint[] DeactivateRays()
        {
            // Create an array for the output datapoints - these represent the rays that become deactivated or restricted
            ImplicitFracturePopulationDatapoint[] outputDatapoints;

            // Calculate the probabilities of deactivation due to stress shadow interaction of intersection during the timestep
            double Phi = PhiII * PhiIJ;
            double PhiII_ratio = (PhiII > 0) ? Math.Log(PhiII) / Math.Log(Phi) : 1;
            double PhiIJ_ratio = 1 - PhiII_ratio;// (PhiIJ_Ray_M > 0) ? Math.Log(PhiIJ_Ray_M) / Math.Log(Phi_Ray_M) : 1;
            double F_II_M = (PhiII_ratio > 0) ? (1 - Phi) * PhiII_ratio : 0;
            double F_IJ_M = (PhiIJ_ratio > 0) ? (1 - Phi) * PhiIJ_ratio : 0;

            switch (Status)
            {
                case RayPropagationStatus.FullyActive:
                    // There will be three datapoints in the output array, representing the rays that become restricted and deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[3];
                    // Calculate the total proportion of fractures for which no rays will be deactivated during the current timestep
                    // This will be the probability that a single ray will not be deactivated (Phi_Ray_M) to the power of the number of rays per fracture
                    double Phi_Fracture_M = Math.Pow(Phi, NoRaysPerFracture);
                    // The first datapoint in the output array represents the new restricted rays
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * (Phi - Phi_Fracture_M), RayPropagationStatus.Restricted, RayLength);
                    // The second datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_II_M, RayPropagationStatus.StaticStressShadow, RayLength);
                    // The third datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[2] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_IJ_M, RayPropagationStatus.StaticIntersection, RayLength);
                    // Reduce the volumetric density of fully active rays represented by this datapoint
                    dP30 *= Phi_Fracture_M;
                    break;
                case RayPropagationStatus.Restricted:
                    // There will be two datapoints in the output array, representing the rays that become deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[2];
                    // The first datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_II_M, RayPropagationStatus.StaticStressShadow, RayLength);
                    // The second datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, 0, dP30 * F_IJ_M, RayPropagationStatus.StaticIntersection, RayLength);
                    // Reduce the volumetric density of restricted rays represented by this datapoint
                    dP30 *= Phi;
                    break;
                case RayPropagationStatus.StaticStressShadow:
                case RayPropagationStatus.StaticIntersection:
                    // There will be no datapoints in the output array
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[0];
                    break;
                default:
                    // Return a null output array
                    outputDatapoints = null;
                    break;
            }

            // Reset the fracture activation probabilities
            ResetFractureActivationProbabilities();

            // Return the new datapoints
            return outputDatapoints;
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
            ResetFractureActivationProbabilities();
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
        /// Maximum volumetric ratio of static fracture rays deactivated due to stress shadow interaction
        /// This value does not take into account that the stress shadows around static fractures may overlap
        /// It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
        /// </summary>
        public double sII_RP33_total { get { return RP33_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to intersection
        /// This value does not take into account that the stress shadows around static fractures may overlap
        /// It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
        /// </summary>
        public double sIJ_RP33_total { get { return RP33_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Volumetric density of all fractures
        /// </summary>
        public double FP30_total
        {
            get
            {
                double RP30 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP30 += RP30_total[status];
                return RP30 / (double)noSegments;
            }
        }
        /// <summary>
        /// Mean linear density of all fractures
        /// </summary>
        public double FP32_total
        {
            get
            {
                double RP32 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP32 += RP32_total[status];
                return RP32;
            }
        }
        /// <summary>
        /// Maximum volumetric ratio of all fractures
        /// This value does not take into account that the stress shadows around static fractures may overlap
        /// It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
        /// </summary>
        public double FP33_total
        {
            get
            {
                double RP33 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP33 += RP33_total[status];
                return RP33;

                /*double OneMinus_arRP33 = 1 - RP33_total[RayPropagationStatus.FullyActive] - RP33_total[RayPropagationStatus.Restricted];
                if (OneMinus_arRP33 < 0) OneMinus_arRP33 = 0;
                double OneMinus_sIIRP33 = 1 - RP33_total[RayPropagationStatus.StaticStressShadow];
                if (OneMinus_sIIRP33 < 0) OneMinus_sIIRP33 = 0;
                double OneMinus_sIJRP33 = 1 - RP33_total[RayPropagationStatus.StaticIntersection];
                if (OneMinus_sIJRP33 < 0) OneMinus_sIJRP33 = 0;
                return 1 - (OneMinus_arRP33 * OneMinus_sIIRP33 * OneMinus_sIJRP33);*/
            }
        }
        /// <summary>
        /// Get the total volumetric density of all segments with effective radius greater than a specified value
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P30 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP30(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            double RP30 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < CutoffRadius)
                        break;
                    RP30 += datapoint.dP30;
                }

            return RP30;
        }
        /// <summary>
        /// Get the total mean linear density of all segments with effective radius greater than a specified value
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P32 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP32(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            // It also ignores the adjustment factors - these are only calculated for the entire fracture population
            double RP32 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < CutoffRadius)
                        break;
                    RP32 += datapoint.dP32factor;
                }

            return RP32 * (Math.PI / (double)noSegments);
        }
        /// <summary>
        /// Get the total volumetric ratio of all segments with effective radius greater than a specified value
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P33 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP33(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            // It also ignores the adjustment factors - these are only calculated for the entire fracture population
            // Finally it does not take into account that the stress shadows around static fractures may overlap
            // It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
            double RP33 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < CutoffRadius)
                        break;
                    RP33 += datapoint.dP33factor;
                }

            return RP33 * (4 / 3) * (Math.PI / (double)noSegments);
        }
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
                // Sort the arrays on effective ray length from largest to smallest
                fracturePopulationDatapoints[status].Sort();

                // NB The RP33 values for static fractures calculated here do not take into account that the stress shadows around static fractures may overlap
                // These values will therefore be overestimates of the true values and should not be used for calculating total stress shadow volume
                // However it is not possible to calculate the true RP33 values without knowing the stress shadow widths

                // Loop through the arrays for population distribution functions
                foreach (ImplicitFracturePopulationDatapoint dataPoint in fracturePopulationDatapoints[status])
                {
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

        // Data calculation and output functions
        /// <summary>
        /// Get the total clear zone volume seen by a fully active fracture with a specified radius 
        /// </summary>
        /// <param name="radius_in">Radius of the fully active fracture</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius</param>
        /// <returns>Clear zone volume seen by the specified fracture; this is the inverse of the exclusion zone volume seen by the fracture</returns>
        public void getStressShadowClearZoneVolume(double Radius, double StressShadowWidthRatio, out double InverseStressShadowVolume, out double ClearZoneVolume)
        {
            // First calculate the inverse stress shadow volume
            // This will also be the basis of the clear zone volume
            InverseStressShadowVolume = 1;
            double stressShadowVolumeMultiplier = (4 / 3) * Math.PI * StressShadowWidthRatio / (double)noSegments;

            // Stress shadows around fully active and restricted rays cannot overlap, so this component of the exclusion zone volume is equal to the sum of the stress shadow volume
            double ar_RStressShadow = StressShadowWidthRatio * (RP33_total[RayPropagationStatus.FullyActive] + RP33_total[RayPropagationStatus.Restricted]);
            InverseStressShadowVolume -= ar_RStressShadow;
            if (InverseStressShadowVolume < 0) InverseStressShadowVolume = 0;

            // Stress shadows around static fractures from different datapoints can overlap so must be calculated by multiplying their inverses
            // NB Stress shadows around static fractures represented by the same datapoint cannot overlap, since these were all deactivated at the same time so must all have been active simultaneously
            foreach (RayPropagationStatus status in new RayPropagationStatus[2] { RayPropagationStatus.StaticStressShadow, RayPropagationStatus.StaticIntersection })
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    double datapoint_issv = 1 - (datapoint.dP33factor * stressShadowVolumeMultiplier);
                    if (datapoint_issv < 0) datapoint_issv = 0;
                    InverseStressShadowVolume *= datapoint_issv;
                }

            // The clear zone volume can be calculated from the inverse stress shadow volume by removing the outer exclusion zone volume
            ClearZoneVolume = InverseStressShadowVolume;

            // The outer exclusion zones can overlap with exclusion zones and stress shadows around all other fractures, including other fractures represented by the same datapoint
            // We therefore use the ImplicitFracturePopulationDatapoint.GetInverseOuterExclusionZoneVolume to get the inverse exclusion zone volume for each datapoint
            // This function allows for overlap between exclusion zones around each fracture
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    ClearZoneVolume *= datapoint.GetInverseOuterExclusionZoneVolume(Radius, StressShadowWidthRatio);
                }
        }
        /// <summary>
        /// Get the inverse of the total volume of the interaction zones around all fracture segments in this set as seen by rays represented by specific datapoint
        /// </summary>
        /// <param name="Fracture2"></param>
        /// <param name="MinRadiusToCheck"></param>
        /// <param name="StressShadowWidthRatio"></param>
        /// <returns></returns>
        public double getStressShadowNonInteractionVolume(ImplicitFracturePopulationDatapoint Fracture2, double MinRadiusToCheck, double StressShadowWidthRatio)
        {
            double niv = 1;

            // Loop through all the datapoints that exceed the minimum specified radius and incorporate the non-interaction volume for that datapoint into the total non-interaction volume
            // Since interaction zones around individual fractures can overlap, this must be done by multiplying
            // NB this will include the interaction zones around the specified datapopint, as one fracture represented by this datapoint may interact with other fractures represented by the same datapoint
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint dataPoint in fracturePopulationDatapoints[status])
                    if (dataPoint.EffectiveRayLength >= MinRadiusToCheck)
                        niv *= dataPoint.GetNonInteractionZoneVolume(Fracture2, StressShadowWidthRatio);
            return niv;
        }

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
