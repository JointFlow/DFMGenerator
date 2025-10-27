using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator for the fracture ray propagation status: FullyActive (i.e. all rays in the fracture are active), Restricted (i.e. at least one other ray in the fracture is deactivated), StaticStressShadow (i.e. this ray is deactivated due to stress shadow interaction), StaticIntersection (i.e. this ray is deactivated due to intersecting a fracture from another set)
    /// </summary>
    enum RayPropagationStatus { FullyActive, Restricted, StaticStressShadow, StaticIntersection, StaticMaxRadius }

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
        /// <summary>
        /// Comparator function to use as an overload for the default comparator in order to sort the datapoint arrays by ray length, in ascending order, if required
        /// </summary>
        /// <param name="dp1">First ImplicitFracturePopulationDatapoint object to compare</param>
        /// <param name="dp2">Second ImplicitFracturePopulationDatapoint object to compare</param>
        /// <returns>>Positive if the first ImplicitFracturePopulationDatapoint represents the longest rays, negative if the second FractureDipSet ImplicitFracturePopulationDatapoint represents the longest rays, zero if they represent rays of equal length</returns>
        public static int CompareByRayLengthAscending(ImplicitFracturePopulationDatapoint dp1, ImplicitFracturePopulationDatapoint dp2)
        {
            return dp1.RayLength.CompareTo(dp2.RayLength);
        }

        // Basic data
        /// <summary>
        /// Current length of rays represented by this datapoint
        /// </summary>
        public double RayLength { get; private set; }
        /// <summary>
        /// Incremental increase in the ray length in the current timestep
        /// </summary>
        public double RayLengthIncrement { get; private set; }
        /// <summary>
        /// Set the incremental increase in the ray length in the current timestep
        /// </summary>
        /// <param name="rayLengthIncrement_in">Incremental increase in the ray length in the current timestep</param>
        /// <param name="incrementToMaxRadius_in">Flag to indicate whether the next ray length increment will reach the maximum fracture radius</param>
        public void SetRayLengthIncrement(double rayLengthIncrement_in, bool incrementToMaxRadius_in)
        {
            RayLengthIncrement = rayLengthIncrement_in;
            incrementToMaxRadius = incrementToMaxRadius_in;
        }
        /// <summary>
        /// Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)
        /// </summary>
        public double dRP30 { get; private set; }
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
        /// Flag to indicate whether the next ray length increment will reach the maximum fracture radius; if so, set the ray propagation status to StaticMaxRadius when the ray length is incremented
        /// </summary>
        private bool incrementToMaxRadius;
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
        /// Probability that an active ray has not been deactivated for any reason since the last deactivation check
        /// </summary>
        public double Phi { get { return PhiII * PhiIJ; } }
        /// <summary>
        /// Effective ray length at the last deactivation check
        /// </summary>
        private double EffectiveRayLengthAtLastDeactivationCheck { get; set; }
        /// <summary>
        /// The proportional growth in the effective ray length since the last deactivation check
        /// </summary>
        public double ProportionalGrowthSinceLastDeactivationCheck { get { return (EffectiveRayLength + EffectiveRayLengthIncrement - EffectiveRayLengthAtLastDeactivationCheck) / EffectiveRayLengthAtLastDeactivationCheck; } }
        // Debugging data - only required in debug mode
#if DEBUG
        /// <summary>
        /// Probability that an active ray has not been deactivated due to stress shadow interaction since the start of the model
        /// </summary>
        public double CumulativePhiII { get; private set; }
        /// <summary>
        /// Probability that an active ray has not been deactivated due to intersecting a fracture from another set since the start of the model
        /// </summary>
        public double CumulativePhiIJ { get; private set; }
        /// <summary>
        /// Probability that an active ray has not been deactivated for any reason since the start of the model
        /// </summary>
        public double CumulativePhi { get { return CumulativePhiII * CumulativePhiIJ; } }
#endif

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
                    case RayPropagationStatus.StaticMaxRadius:
                        return 0;
                    default:
                        return 0;
                }
            }
        }
        /// <summary>
        /// Factor to calculate incremental area of segments represented by this datapoint; must be multiplied by a geometric factor to get the true dP32 increment
        /// </summary>
        public double dP32factor { get { return dRP30 * RayLength * RayLength; } }
        /// <summary>
        /// Factor to calculate incremental stress shadow volume around the segments represented by this datapoint; must be multiplied by a geometric factor to get the true dP33 increment
        /// </summary>
        public double dP33factor { get { return dRP30 * RayLength * RayLength * EffectiveRayLength; } }
        /// <summary>
        /// Factor to calculate volume of the stress shadow represented by this datapoint plus or minus an additional shell; must be multiplied by a geometric factor to get the true shell volume
        /// </summary>
        /// <param name="shellWidth">Width of the shell; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <returns></returns>
        public double dP33AdjustedFactor(double shellWidth)
        {
            return dP33AdjustedFactor(shellWidth, shellWidth);
        }
        /// <summary>
        /// Factor to calculate volume of the stress shadow represented by this datapoint plus or minus an additional shell; must be multiplied by a geometric factor to get the true shell volume
        /// </summary>
        /// <param name="shellWidth">Width of the shell in the plane of the fracture; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <param name="effectiveShellWidth">Width of the shell perpendicular to the plane of the fracture; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <returns></returns>
        public double dP33AdjustedFactor(double shellWidth, double effectiveShellWidth)
        {
            double shellRadius = RayLength + shellWidth;
            double effectiveShellRadius = EffectiveRayLength + effectiveShellWidth;
            if (effectiveShellRadius < 0)
                return 0;
            else
                return dRP30 * (shellRadius * shellRadius * effectiveShellRadius);
        }
        /// <summary>
        /// Factor to calculate volume of a symmetric shell around the stress shadow represented by this datapoint; must be multiplied by a geometric factor to get the true shell volume
        /// </summary>
        /// <param name="shellWidth">Width of the shell; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <returns></returns>
        public double dP33ShellFactor(double shellWidth)
        {
            return dP33ShellFactor(shellWidth, shellWidth);
        }
        /// <summary>
        /// Factor to calculate volume of an asymmetric shell around the stress shadow represented by this datapoint; must be multiplied by a geometric factor to get the true shell volume
        /// </summary>
        /// <param name="shellWidth">Width of the shell in the plane of the fracture; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <param name="effectiveShellWidth">Width of the shell perpendicular to the plane of the fracture; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <returns></returns>
        public double dP33ShellFactor(double shellWidth, double effectiveShellWidth)
        {
            double shellRadius = RayLength + shellWidth;
            double effectiveShellRadius = EffectiveRayLength + effectiveShellWidth;
            if (effectiveShellRadius < 0)
                return 0;
            else if (shellWidth < 0)
                return dRP30 * ((RayLength * RayLength * EffectiveRayLength) - (shellRadius * shellRadius * effectiveShellRadius));
            else
                return dRP30 * ((shellRadius * shellRadius * effectiveShellRadius) - (RayLength * RayLength * EffectiveRayLength));
        }
        /*/// <summary>
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
            double OEZShellVolume = (4d / 3d) * Math.PI * (StressShadowWidthRatio / 2) * ((EZLength * EZLength * EZWidth) - (RayLength * RayLength * EffectiveRayLength));
            return Math.Exp(-OEZShellVolume * (dP30 / (double)NoRaysPerFracture));
        }
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
        /// Get the inverse of the total volume of the interaction zones around fracture segments represented by this datapoint as seen by rays represented by another datapoint
        /// The interaction zone is the zone surrounding the exclusion zone; if the centrepoint of the other fracture lies within the interaction zone of this fracture, the two stress shadows will intersect in the next timestep
        /// </summary>
        /// <param name="Fracture2">Reference to another implicit fracture population distribution function datapoint, representing the propagating rays which are being checked</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius for this set</param>
        /// <returns>Inverse of the total volume of the interaction zones around fracture segments represented by this datapoint as seen by rays represented by the specified datapoint</returns>
        public double GetNonInteractionZoneVolume(ImplicitFracturePopulationDatapoint Fracture2, double StressShadowWidthRatio)
        {
            double EZLength = this.RayLength + Fracture2.RayLength;
            double EZWidth = (this.EffectiveRayLength + Fracture2.EffectiveRayLength) * (StressShadowWidthRatio / 2);
            double IZLength = EZLength + this.RayLengthIncrement + Fracture2.RayLengthIncrement;
            double IZWidth = EZWidth + ((this.EffectiveRayLengthIncrement + Fracture2.EffectiveRayLengthIncrement) * (StressShadowWidthRatio / 2));

            double IZShellVolume = (4d / 3d) * Math.PI * ((IZLength * IZLength * IZWidth) - (EZLength * EZLength * EZWidth));
            //double output = Math.Exp(-IZShellVolume * (Fracture2.dP30 / (double)NoRaysPerFracture));
            return Math.Exp(-IZShellVolume * (this.dP30 / (double)NoRaysPerFracture));
        }*/

        // Functions to manipulate data
        /// <summary>
        /// Add the increment in ray length to the current ray length, then reset the increment in ray length to zero
        /// </summary>
        /// <returns>True if the ray length increment will reach the maximum fracture radius, otherwise false</returns>
        public bool IncrementRayLength()
        {
            RayLength += RayLengthIncrement;
            if (Status == RayPropagationStatus.FullyActive)
                PropagationControllingLength += RayLengthIncrement;
            RayLengthIncrement = 0;

            if (incrementToMaxRadius)
                Status = RayPropagationStatus.StaticMaxRadius;
            return incrementToMaxRadius;
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
#if DEBUG
            CumulativePhiII *= PhiII_M;
            CumulativePhiIJ *= PhiIJ_M;
#endif
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
        /// Deactivate a proportion of currently active rays based on the current fracture activation probabilities at the end of the current timestep
        /// </summary>
        /// <returns>List of ImplicitFracturePopulationDatapoints representing the originally active rays that became restricted or deactivated rays</returns>
        public ImplicitFracturePopulationDatapoint[] DeactivateRays()
        {
            // Set the proportion of the ray length increment for the original fully active datapoint to apply to the deactivated and restricted datapoints to 1
            // The full increment will therefore be applied to the derived deactivated and restricted datapoints
            // This implies deactivation at the end of the current timestep
            return DeactivateRays(1);
        }
        /// <summary>
        /// Deactivate a proportion of currently active rays based on the current fracture activation probabilities, applying a specified proportion of the original increment to the deactivated rays
        /// </summary>
        /// <param name="proportionalIncrementToApply">Proportion of the ray length increment for the original fully active datapoint to apply to the deactivated and restricted datapoints: 0 assumes deactivation at the start of the current timestep and 1 implies deactivation at the end of the current timestep</param>
        /// <returns>List of ImplicitFracturePopulationDatapoints representing the originally active rays that became restricted or deactivated rays</returns>
        private ImplicitFracturePopulationDatapoint[] DeactivateRays(double proportionalIncrementToApply)
        {
            // Create an array for the output datapoints - these represent the rays that become deactivated or restricted
            ImplicitFracturePopulationDatapoint[] outputDatapoints;

            // Calculate the probabilities of deactivation due to stress shadow interaction of intersection during the timestep
            double Phi = PhiII * PhiIJ;
            double PhiII_ratio = (PhiII > 0) ? Math.Log(PhiII) / Math.Log(Phi) : 1;
            double PhiIJ_ratio = 1 - PhiII_ratio;// (PhiIJ_Ray_M > 0) ? Math.Log(PhiIJ_Ray_M) / Math.Log(Phi_Ray_M) : 1;
            double F_II_M = (PhiII_ratio > 0) ? (1 - Phi) * PhiII_ratio : 0;
            double F_IJ_M = (PhiIJ_ratio > 0) ? (1 - Phi) * PhiIJ_ratio : 0;

            // Calculate the proportion of the original fully active ray increment to be applied to the derived deactivated and restricted datapoints
            double incrementToApply = proportionalIncrementToApply * RayLengthIncrement;
            bool incrementDerivedDatapointsToMaxRadius = incrementToMaxRadius && (proportionalIncrementToApply >= 1);
            double finalRayLength = RayLength + incrementToApply;

            switch (Status)
            {
                case RayPropagationStatus.FullyActive:
                    // There will be three datapoints in the output array, representing the rays that become restricted and deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[3];
                    // Calculate the total proportion of fully active fractures for which no rays will be deactivated during the current timestep
                    // This will be the probability that a single ray will not be deactivated (Phi_Ray_M) to the power of the number of rays per fracture
                    double Phi_Fracture_M = Math.Pow(Phi, NoRaysPerFracture);
                    // The first datapoint in the output array represents the new restricted rays
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, incrementToApply, incrementDerivedDatapointsToMaxRadius, dRP30 * (Phi - Phi_Fracture_M), RayPropagationStatus.Restricted, finalRayLength);
                    // The second datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, incrementToApply, incrementDerivedDatapointsToMaxRadius, dRP30 * F_II_M, RayPropagationStatus.StaticStressShadow, finalRayLength);
                    // The third datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[2] = new ImplicitFracturePopulationDatapoint(RayLength, incrementToApply, incrementDerivedDatapointsToMaxRadius, dRP30 * F_IJ_M, RayPropagationStatus.StaticIntersection, finalRayLength);
                    // Reduce the volumetric density of fully active rays represented by this datapoint
                    dRP30 *= Phi_Fracture_M;
                    break;
                case RayPropagationStatus.Restricted:
                    // There will be two datapoints in the output array, representing the rays that become deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[2];
                    // The first datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, incrementToApply, incrementDerivedDatapointsToMaxRadius, dRP30 * F_II_M, RayPropagationStatus.StaticStressShadow, PropagationControllingLength);
                    // The second datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, incrementToApply, incrementDerivedDatapointsToMaxRadius, dRP30 * F_IJ_M, RayPropagationStatus.StaticIntersection, PropagationControllingLength);
                    // Reduce the volumetric density of restricted rays represented by this datapoint
                    dRP30 *= Phi;
                    break;
                case RayPropagationStatus.StaticStressShadow:
                case RayPropagationStatus.StaticIntersection:
                case RayPropagationStatus.StaticMaxRadius:
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
            dRP30 += dP30_increment;
        }

        // Constructors
        /// <summary>
        /// Default constructor: Create a datapoint representing fully active rays belonging to a nucleating fracture
        /// In this case the rays start with length zero, but we add an increment to bring them to the minimum ray length
        /// </summary>
        /// <param name="rayLength_in">Initial length of rays represented by this datapoint</param>
        /// <param name="dP30_in">Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)</param>
        public ImplicitFracturePopulationDatapoint(double rayLength_in, double dP30_in) : this(0, rayLength_in, false, dP30_in, RayPropagationStatus.FullyActive, rayLength_in)
        {
            // Defaults:
            // The current ray length will be set to 0, and the specified initial ray length will instead be specified as the current ray increment
            // This will allow for ray deactivation if they intersect or interact with the stress shadow of another fracture before they reach the specified initial length
            // The flag to indicate whether the next ray length increment will reach the maximum fracture radius will be set to false - it is assumed the specified initial length is less than the maximum ray length
            // Length of the ray controlling the propagation rate of this ray: For a fully active ray this will always be the ray length
        }
        /// <summary>
        /// Constructor: specify ray length, dP30, propagation status, and propagation controlling length
        /// </summary>
        /// <param name="rayLength_in">Current length of rays represented by this datapoint</param>
        /// <param name="rayLengthIncrement_in">Incremental increase in the ray length in the current timestep</param>
        /// <param name="incrementToMaxRadius_in">Flag to indicate whether the next ray length increment will reach the maximum fracture radius</param>
        /// <param name="dP30_in">Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)</param>
        /// <param name="status_in">Propagation status of rays represented by this datapoint</param>
        /// <param name="propagationControllingLength_in">Length of the ray controlling the propagation rate of this ray</param>
        private ImplicitFracturePopulationDatapoint(double rayLength_in, double rayLengthIncrement_in, bool incrementToMaxRadius_in, double dP30_in, RayPropagationStatus status_in, double propagationControllingLength_in)
        {
            RayLength = rayLength_in;
            RayLengthIncrement = rayLengthIncrement_in;
            dRP30 = dP30_in;
            Status = status_in;
            incrementToMaxRadius = incrementToMaxRadius_in;
            if (status_in == RayPropagationStatus.FullyActive)
                PropagationControllingLength = rayLength_in;
            else
                PropagationControllingLength = propagationControllingLength_in;
            ResetFractureActivationProbabilities();
#if DEBUG
            CumulativePhiII = 1;
            CumulativePhiIJ = 1;
#endif
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
        /// Volumetric density of static fracture rays deactivated due to reaching the maximum radius
        /// </summary>
        public double sMR_RP30_total { get { return RP30_total[RayPropagationStatus.StaticMaxRadius]; } }
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
        /// Mean linear density of static fracture rays deactivated due to reaching the maximum radius
        /// </summary>
        public double sMR_RP32_total { get { return RP32_total[RayPropagationStatus.StaticMaxRadius]; } }
#if DEBUG
        /// <summary>
        /// Volumetric ratio of fully active fracture rays (i.e. all rays in the fracture are active)
        /// NB Stress shadows around fully active fracture rays never overlap
        /// </summary>
        public double a_RP33_total { get { return RP33_exclusive_total[RayPropagationStatus.FullyActive]; } }
        /// <summary>
        /// Volumetric ratio of restricted fracture rays (i.e. at least one other ray in the fracture is deactivated)
        /// NB Stress shadows around restricted fracture rays never overlap
        /// </summary>
        public double r_RP33_total { get { return RP33_exclusive_total[RayPropagationStatus.Restricted]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to stress shadow interaction
        /// This value does not take into account that the stress shadows around static fracture rays may overlap
        /// It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
        /// </summary>
        public double sII_RP33_total { get { return RP33_exclusive_total[RayPropagationStatus.StaticStressShadow] + RP33_overlapping_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to intersection
        /// This value does not take into account that the stress shadows around static fracture rays may overlap
        /// It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
        /// </summary>
        public double sIJ_RP33_total { get { return RP33_exclusive_total[RayPropagationStatus.StaticIntersection] + RP33_overlapping_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to reaching the maximum radius
        /// This value does not take into account that the stress shadows around static fracture rays may overlap
        /// It will therefore be an overestimate of the true value and should not be used for calculating total stress shadow volume
        /// </summary>
        public double sMR_RP33_total { get { return RP33_exclusive_total[RayPropagationStatus.StaticMaxRadius] + RP33_overlapping_total[RayPropagationStatus.StaticMaxRadius]; } }
#endif
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
        /// Maximum volumetric ratio of all non-overlapping fractures
        /// This includes all fully active and restricted rays, all rays that have reached maximum length, and the largest static rays
        /// However it does not include static rays with effective ray length shorter than the minimum size that can be deactivated by the largest current fractures
        /// It should therefore not be used directly for calculating total stress shadow volume
        /// </summary>
        public double FP33_exclusive_total
        {
            get
            {
                double RP33 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP33 += RP33_exclusive_total[status];
                return RP33;
            }
        }
        /// Maximum volumetric ratio of all overlapping fractures
        /// This value does not take into account that the stress shadows around these fractures may overlap
        /// It will therefore be an overestimate of the true value and should not be used directly for calculating total stress shadow volume
        /// </summary>
        public double FP33_overlapping_total
        {
            get
            {
                double RP33 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP33 += RP33_overlapping_total[status];
                return RP33;
            }
        }
        /// <summary>
        /// Get the total volumetric density of all fractures with effective radius greater than a specified value
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
                    RP30 += datapoint.dRP30;
                }

            return RP30 / (double)noSegments;
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
        /// Get the total volumetric ratio of all non-overlapping fracture segments with effective radius greater than a specified value
        /// This includes all fully active and restricted rays, all rays that have reached maximum length, and the largest static rays
        /// However it does not include static rays with effective ray length shprter than the minimum size that can be deactivated by the largest current fractures
        /// It should therefore not be used directly for calculating total stress shadow volume
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P33 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP33_exclusive(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            // It also ignores the adjustment factors - these are only calculated for the entire fracture population

            double RP33 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // If the minimum size for non-overlapping fracture rays for the current ray propagation status is greater than the specified cutoff, we will only look at fractures larger than that
                double minEffectiveRayLength = Math.Max(CutoffRadius, getMinimumExclusiveStressShadowEffectiveRayLength(status));

                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < minEffectiveRayLength)
                        break;
                    RP33 += datapoint.dP33factor;
                }
            }

            return RP33 * (4d / 3d) * (Math.PI / (double)noSegments);
        }
        /// <summary>
        /// Get the total volumetric ratio of all overlapping fracture segments with effective radius greater than a specified value
        /// This value does not take into account that the stress shadows around these fractures may overlap
        /// It will therefore be an overestimate of the true value and should not be used directly for calculating total stress shadow volume
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P33 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP33_overlapping(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            // It also ignores the adjustment factors - these are only calculated for the entire fracture population

            double RP33 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
                double minExclusiveEffectiveRayLength = Math.Max(CutoffRadius, getMinimumExclusiveStressShadowEffectiveRayLength(status));

                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < minExclusiveEffectiveRayLength)
                    {
                        if (datapoint.EffectiveRayLength < CutoffRadius)
                            break;
                        RP33 += datapoint.dP33factor;
                    }
                }
            }

            return RP33 * (4d / 3d) * (Math.PI / (double)noSegments);
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
        /// Dictionary object containing volumetric ratio of non-overlapping fracture rays for all ray propagation statuses
        /// This includes all fully active and restricted rays, all rays that have reached maximum length, and the largest static rays
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_exclusive_total;
        /// <summary>
        /// Dictionary object containing volumetric ratio of overlapping fracture rays for all ray propagation statuses
        /// This includes only the smaller static rays, and no fully active rays, restricted rays, or rays that have reached maximum length
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_overlapping_total;
        /// <summary>
        /// Dictionary object containing the adjustment factors for mean linear density of fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in area between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP32_total_adjustment;
        /// <summary>
        /// Dictionary object containing the adjustment factors for volumetric ratio of non-overlapping fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_exclusive_adjustment;
        /// <summary>
        /// Dictionary object containing the adjustment factors for volumetric ratio of overlapping fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_overlapping_adjustment;
        /// <summary>
        /// Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius
        /// </summary>
        private double minStressShadowDeactivationRatio;
        /// <summary>
        /// Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius
        /// </summary>
        private double minIntersectionDeactivationRatio;
        /// <summary>
        /// Get the minimum effective radius for overlapping fracture rays for the specified ray propagation status
        /// The stress shadows of rays with effective radius below this cutoff may overlap, while the stress shadows of rays with effective radius above this cutoff may not overlap
        /// </summary>
        /// <param name="status">Specified ray propagation status</param>
        /// <returns></returns>
        private double getMinimumExclusiveStressShadowEffectiveRayLength (RayPropagationStatus status)
        {
            // All fully active rays, restricted rays and rays that have reached maximum length are non-overlapping, so for these ray propagation statuses the minimum value is -1
            // Static rays with effective ray length longer than the minimum size that can be deactivated by the largest current fractures are also non-overlapping
            // However static rays with effective ray length shprter than the minimum size that can be deactivated by the largest current fractures may overlap

            switch (status)
            {
                case RayPropagationStatus.FullyActive:
                    return -1;
                case RayPropagationStatus.Restricted:
                    return -1;
                case RayPropagationStatus.StaticStressShadow:
                    return minStressShadowDeactivationRatio * MaximumEffectiveRayLength;
                case RayPropagationStatus.StaticIntersection:
                    return minIntersectionDeactivationRatio * MaximumEffectiveRayLength;
                case RayPropagationStatus.StaticMaxRadius:
                    return -1;
                default:
                    return -1;
            }
        }
        /// <summary>
        /// Recalculate the total population data from the piecewise population distribution function arrays
        /// </summary>
        public void RecalculateTotalPopulationData()
        {
            // Reset the total population values to zero
            ResetTotalPopulationData();

            // Sort each of the fracture population distribution arrays on effective ray length from largest to smallest
            // This must be done for all raays before recalculating any of the total fracture population distribution values, so we get the correct minimum size for non-overlapping fracture rays
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                fracturePopulationDatapoints[status].Sort();

            // Recalculate all of the total fracture population distribution values
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
                double minExclusiveEffectiveRayLength = getMinimumExclusiveStressShadowEffectiveRayLength(status);

                // Loop through the arrays for population distribution functions
                foreach (ImplicitFracturePopulationDatapoint dataPoint in fracturePopulationDatapoints[status])
                {
                    RP30_total[status] += dataPoint.dRP30;
                    RP32_total[status] += dataPoint.dP32factor;
                    if (dataPoint.EffectiveRayLength < minExclusiveEffectiveRayLength)
                        RP33_overlapping_total[status] += dataPoint.dP33factor;
                    else
                        RP33_exclusive_total[status] += dataPoint.dP33factor;
                }

                // Add the adjustment factors to the P32 and P33 values
                RP32_total[status] += RP32_total_adjustment[status];
                RP33_exclusive_total[status] += RP33_exclusive_adjustment[status];
                RP33_overlapping_total[status] += RP33_overlapping_adjustment[status];

                // Apply the area and volume multipliers
                RP32_total[status] *= (Math.PI / (double)noSegments);
                RP33_exclusive_total[status] *= (4d / 3d) * (Math.PI / (double)noSegments);
                RP33_overlapping_total[status] *= (4d / 3d) * (Math.PI / (double)noSegments);
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
        /// <summary>
        /// Get the maximum effective ray length of all the current fractures
        /// </summary>
        public double MaximumEffectiveRayLength
        {
            get
            {
                // If any fractures have reached the maximum radius, return this
                if (fracturePopulationDatapoints[RayPropagationStatus.StaticMaxRadius].Count > 0)
                    return fracturePopulationDatapoints[RayPropagationStatus.StaticMaxRadius][0].EffectiveRayLength;
                // Otherwise go through all the other categories and find the largest effective radius
                // We assume that the arrays are sorted so the first datapoint will have the largest effective radius
                double maxEffRadius = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    if (fracturePopulationDatapoints[status].Count > 0)
                    {
                        double nextMaxEffRadius = fracturePopulationDatapoints[status][0].EffectiveRayLength;
                        if (maxEffRadius < nextMaxEffRadius)
                            maxEffRadius = nextMaxEffRadius;
                    }
                return maxEffRadius;
            }
        }

        // Data calculation and output functions
        /// <summary>
        /// Get the total volume not in stress shadow, taking account of overlapping stress shadows
        /// </summary>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to effective fracture radius</param>
        /// <returns></returns>
        public double getInverseStressShadowVolume(double StressShadowWidthRatio)
        {
            double inverseStressShadowVolume = 1;

            // First remove the exclusive stress shadow volume
            // This is the volume of stress shadows around fully active and restricted rays, rays that have reached maximum length, and the largest static rays, which cannot overlap each other
            double exclusiveStressShadowVolume = FP33_exclusive_total * (StressShadowWidthRatio / 2);
            inverseStressShadowVolume -= exclusiveStressShadowVolume;
            if (inverseStressShadowVolume < 0)
                inverseStressShadowVolume = 0;

            // Now remove the overlapping stress shadow volume
            // Since this comprises the stress shadows that can overlap other stress shadows, we must calculate this as an exponential multiplier
            double overlappingStressShadowVolume = FP33_overlapping_total * (StressShadowWidthRatio / 2);
            inverseStressShadowVolume *= Math.Exp(-overlappingStressShadowVolume);

            // Return the final calculated inverse stress shadow volume
            return inverseStressShadowVolume;
        }
        /// <summary>
        /// Get the total stress shadow volume, taking account of overlap
        /// </summary>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to effective fracture radius</param>
        /// <returns></returns>
        public double getStressShadowVolume(double StressShadowWidthRatio)
        {
            return 1 - getInverseStressShadowVolume(StressShadowWidthRatio);
        }
        /// <summary>
        /// Get the total clear zone volume seen by a fully active fracture with a specified radius, taking account of overlap
        /// </summary>
        /// <param name="Radius">Radius of the fully active fracture</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to effective fracture radius</param>
        /// <param name="InverseStressShadowVolume">Reference variable to return the inverse stress shadow volume as well, if this is required</param>
        /// <returns>Clear zone volume seen by the specified fracture; this is the inverse of the exclusion zone volume seen by the fracture</returns>
        public double getStressShadowClearZoneVolume(double Radius, double StressShadowWidthRatio, out double InverseStressShadowVolume)
        {
            return getStressShadowClearZoneVolume(Radius, Radius, StressShadowWidthRatio, out InverseStressShadowVolume);
        }
        /// <summary>
        /// Get the total clear zone volume seen by as seen by rays represented by a specified datapoint, taking account of overlap and the effective radius of all fractures
        /// </summary>
        /// <param name="Fracture2">Implicit fracture population datapoint representing the dimensions of the specified fracture rays</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to effective fracture radius</param>
        /// <param name="InverseStressShadowVolume">Reference variable to return the inverse stress shadow volume as well, if this is required</param>
        /// <returns>Clear zone volume seen by the specified fracture; this is the inverse of the exclusion zone volume seen by the fracture</returns>
        public double getStressShadowClearZoneVolume(ImplicitFracturePopulationDatapoint Fracture2, double StressShadowWidthRatio, out double InverseStressShadowVolume)
        {
            return getStressShadowClearZoneVolume(Fracture2.RayLength, Fracture2.EffectiveRayLength, StressShadowWidthRatio, out InverseStressShadowVolume);
        }
        /// <summary>
        /// Get the total clear zone volume seen by as seen any other fracture, taking account of overlap and the effective radius of all fractures
        /// </summary>
        /// <param name="radius">Radius of the fully active fracture</param>
        /// <param name="effectiveRadius">Effective radius of the fully active fracture</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to effective fracture radius</param>
        /// <param name="InverseStressShadowVolume">Reference variable to return the inverse stress shadow volume as well, if this is required</param>
        /// <returns>Clear zone volume seen by the specified fracture; this is the inverse of the exclusion zone volume seen by the fracture</returns>
        private double getStressShadowClearZoneVolume(double radius, double effectiveRadius, double StressShadowWidthRatio, out double InverseStressShadowVolume)
        {
            // First get the inverse stress shadow volume
            InverseStressShadowVolume = getInverseStressShadowVolume(StressShadowWidthRatio);
            double clearZoneVolume = InverseStressShadowVolume;

            // Calculate the inner core and outer shell volumes of the non-overlapping fractures, and the outer shell volumes of the overlapping fractures
            // The outer shell volume of non-overlapping stress shadows is the outer exclusion zone of the non-overlapping fractures - i.e. not part of the stress shadow, but where new fractures cannot nucleate without overlapping the stress shadow
            // Although the stress shadows of the non-overlapping fractures cannot overlap, the outer exclusion zones can overlap each other and, to a limited extent, the stress shadows
            // The inner core volume of non-overlapping stress shadows is the volume which the outer exclusion zones cannot overlap
            // The outer shell volume of overlapping stress shadows is the outer exclusion zone of the overlapping fractures - like the stress shadows, these can overlap anything
            double exclusiveOuterShellVolume = 0;
            double exclusiveInnerCoreVolume = 0;
            double overlappingOuterShellVolume = 0;
            // Loop through each ray propagation status
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
                double minExclusiveEffectiveRayLength = getMinimumExclusiveStressShadowEffectiveRayLength(status);

                // Loop through each datapoint in the population data array
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    // Check if it is an overlapping or a non-overlapping datapoint, and increment the appropriate total shell volumes
                    if (datapoint.EffectiveRayLength < minExclusiveEffectiveRayLength)
                    {
                        overlappingOuterShellVolume += datapoint.dP33ShellFactor(radius, effectiveRadius);
                    }
                    else
                    {
                        exclusiveOuterShellVolume += datapoint.dP33ShellFactor(radius, effectiveRadius);
                        exclusiveInnerCoreVolume += datapoint.dP33AdjustedFactor(-radius, -effectiveRadius);
                    }
                }
            }
            // Apply the required multiplier to calculate the true shell volumes 
            double stressShadowVolumeMultiplier = (4d / 3d) * Math.PI * (StressShadowWidthRatio / 2) / (double)noSegments;
            if (exclusiveInnerCoreVolume > 1)
                exclusiveInnerCoreVolume = 1;
            exclusiveOuterShellVolume *= stressShadowVolumeMultiplier;
            exclusiveInnerCoreVolume *= stressShadowVolumeMultiplier;
            overlappingOuterShellVolume *= stressShadowVolumeMultiplier;

            // Remove the outer exclusion zone of non-overlapping stress shadows, taking into account that these may overlap the stress shadows to a limited degree and the other exclusion zones without limit
            clearZoneVolume *= Math.Exp(-exclusiveOuterShellVolume / (1 - exclusiveInnerCoreVolume));

            // Remove the outer exclusion zone of overlapping stress shadows, assuming that these may overlap all stress shadows and other exclusion zones without limit
            clearZoneVolume *= Math.Exp(-overlappingOuterShellVolume);

            // Return the clear zone volume
            return clearZoneVolume;
        }
        /// <summary>
        /// Get the inverse of the total volume of the interaction zones around all fracture segments in this set as seen by rays represented by a specified datapoint
        /// </summary>
        /// <param name="Fracture2">Implicit fracture population datapoint representing the dimensions of the specified fracture rays</param>
        /// <param name="StressShadowWidthRatio">Ratio of stress shadow width to fracture radius</param>
        /// <returns></returns>
        public double getStressShadowNonInteractionVolume(ImplicitFracturePopulationDatapoint Fracture2, double StressShadowWidthRatio)
        {
            // This calculates the probability that a random point lies within the clear zone volume after an incremental increase in the size of all fracture rays
            // (including the specified datapoint), assuming that it lay within the clear zone volume before the incremental increase
            // This is more complex than calculating the clear zone volume, since the increment in fracture ray length will be different for every datapoint
            // Also we must take account of the minimum stress shadow deactivation ratio
            // Fracture rays will not terminate if they encounter another fracture with effective radius less than a specified proportion of their own effective radius
            double minDeactivationRadius = Fracture2.EffectiveRayLength * minStressShadowDeactivationRatio;

            // Calculate the required multiplier to convert dP33 factors into true shell volumes 
            double stressShadowVolumeMultiplier = (4d / 3d) * Math.PI * (StressShadowWidthRatio / 2) / (double)noSegments;

            // Get the initial and final radius and effective radius of fracture 2
            double frac2InitialRadius = Fracture2.RayLength;
            double frac2InitialEffectiveRadius = Fracture2.EffectiveRayLength;
            double frac2FinalRadius = frac2InitialRadius + Fracture2.RayLengthIncrement;
            double frac2FinalEffectiveRadius = frac2InitialEffectiveRadius + Fracture2.EffectiveRayLengthIncrement;

            // Calculate the fixed components of the inner shell volumes of the non-overlapping fractures
            double Gamma0 = 0;
            double Gamma1 = 0;
            double Gamma2 = 0;
            double Gamma3 = 0;
            double Gamma4 = 0;
            double Gamma5 = 0;
            // Loop through each ray propagation status
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
                double minExclusiveEffectiveRayLength = getMinimumExclusiveStressShadowEffectiveRayLength(status);

                // Loop through each datapoint in the population data array
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    // If the effective ray length is less than the specified minimum, we can move on to the next array (assuming all arrays are sorted in descending order of effective ray length)
                    double frac1EffRayLength = datapoint.EffectiveRayLength;
                    if (frac1EffRayLength < minExclusiveEffectiveRayLength)
                        break;

                    double dp_dp30 = datapoint.dRP30;
                    double dp_shellRadius = datapoint.RayLength - frac2FinalRadius;
                    double dp_shellEffectiveRadius = datapoint.EffectiveRayLength - frac2FinalEffectiveRadius;
                    if (dp_shellRadius < 0)
                        dp_shellRadius = 0;
                    if (dp_shellEffectiveRadius < 0)
                        dp_shellEffectiveRadius = 0;

                    Gamma0 += (dp_dp30 * dp_shellRadius * dp_shellRadius * dp_shellEffectiveRadius);
                    Gamma1 += (dp_dp30 * dp_shellRadius * dp_shellRadius);
                    Gamma2 += (dp_dp30 * dp_shellRadius * dp_shellEffectiveRadius);
                    Gamma3 += (dp_dp30 * dp_shellRadius);
                    Gamma4 += (dp_dp30 * dp_shellEffectiveRadius);
                    Gamma5 += dp_dp30;
                }
            }
            // Calculate the outer shell volumes of the non-overlapping fractures, and the outer shell volumes of the overlapping fractures, before and after ray length increment
            // The outer shell volume of non-overlapping stress shadows is the outer exclusion zone of the non-overlapping fractures - i.e. not part of the stress shadow, but where new fractures cannot nucleate without overlapping the stress shadow
            // Although the stress shadows of the non-overlapping fractures cannot overlap, the outer exclusion zones can overlap each other and, to a limited extent, the stress shadows
            // The inner core volume of non-overlapping stress shadows is the volume which the outer exclusion zones cannot overlap
            // The outer shell volume of overlapping stress shadows is the outer exclusion zone of the overlapping fractures - like the stress shadows, these can overlap anything
            double initialExclusiveInnerCoreVolume = Gamma0;           
            double initialExclusiveOuterShellVolume = 0;
            double initialOverlappingOuterShellVolume = 0;
            double finalExclusiveOuterShellVolume = 0;
            double finalOverlappingOuterShellVolume = 0;
            // Loop through each ray propagation status
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
                double minExclusiveEffectiveRayLength = getMinimumExclusiveStressShadowEffectiveRayLength(status);

                // Loop through each datapoint in the population data array
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    // Get the increment of the radius and effective radius of fracture 1
                    double frac1RadiusIncrement = datapoint.RayLengthIncrement;
                    double frac1EffectiveRadiusIncrement = datapoint.EffectiveRayLengthIncrement;

                    // If the effective ray length is less than the specified minimum, we can move on to the next array (assuming all arrays are sorted in descending order of effective ray length)
                    double frac1EffRayLength = datapoint.EffectiveRayLength;
                    if (frac1EffRayLength < minDeactivationRadius)
                        break;

                    // Check if it is an overlapping or a non-overlapping datapoint, and increment the appropriate total shell volumes
                    if (frac1EffRayLength < minExclusiveEffectiveRayLength)
                    {
                        initialOverlappingOuterShellVolume += datapoint.dP33ShellFactor(frac2InitialRadius, frac2InitialEffectiveRadius);
                        finalOverlappingOuterShellVolume += datapoint.dP33ShellFactor(frac2FinalRadius + frac1RadiusIncrement, frac2FinalEffectiveRadius + frac1EffectiveRadiusIncrement);
                    }
                    else
                    {
                        // The initial exclusive inner core volume is the same for all fractures, but the final exclusive inner core volume will vary as it includes the increment of fracture size
                        // We must therefore calculate it separately for all fractures
                        // We can use a full version or an approximation, valid when frac1RadiusIncrement << frac1Radius
                        // Full version
                        double finalExclusiveInnerCoreVolume = Gamma0 - (Gamma1 * frac1EffectiveRadiusIncrement) - (2 * Gamma2 * frac1RadiusIncrement) + (2 * Gamma3 * frac1RadiusIncrement * frac1EffectiveRadiusIncrement)
                            + (Gamma4 * frac1RadiusIncrement * frac1RadiusIncrement) - (Gamma5 * frac1RadiusIncrement * frac1RadiusIncrement * frac1EffectiveRadiusIncrement);
                        // Approximation
                        //double finalExclusiveInnerCoreVolume = Gamma0 - (Gamma1 * frac1EffectiveRadiusIncrement) - (2 * Gamma2 * frac1RadiusIncrement);
                        // Apply the required multiplier to calculate the true final exclusive inner core volume 
                        finalExclusiveInnerCoreVolume *= stressShadowVolumeMultiplier;
                        if (finalExclusiveInnerCoreVolume > 1)
                            finalExclusiveInnerCoreVolume = 1;
                        initialExclusiveOuterShellVolume += datapoint.dP33ShellFactor(frac2InitialRadius, frac2InitialEffectiveRadius);
                        finalExclusiveOuterShellVolume += (datapoint.dP33ShellFactor(frac2FinalRadius + frac1RadiusIncrement, frac2FinalEffectiveRadius + frac1EffectiveRadiusIncrement) / (1 - finalExclusiveInnerCoreVolume));
                    }
                }
            }

            // Apply the required multiplier to calculate the true shell volumes 
            initialExclusiveInnerCoreVolume *= stressShadowVolumeMultiplier;
            if (initialExclusiveInnerCoreVolume > 1)
                initialExclusiveInnerCoreVolume = 1;
            initialExclusiveOuterShellVolume /= (1 - initialExclusiveInnerCoreVolume);
            initialOverlappingOuterShellVolume *= stressShadowVolumeMultiplier;
            finalOverlappingOuterShellVolume *= stressShadowVolumeMultiplier;
            initialExclusiveOuterShellVolume *= stressShadowVolumeMultiplier;
            finalExclusiveOuterShellVolume *= stressShadowVolumeMultiplier;

            // Calculate the initial and final clear zone volumes and return the ratio of the two
            double initialClearZoneVolume = Math.Exp(-(initialExclusiveOuterShellVolume + initialOverlappingOuterShellVolume));
            double finalClearZoneVolume = Math.Exp(-(finalExclusiveOuterShellVolume + finalOverlappingOuterShellVolume));
            return finalClearZoneVolume / initialClearZoneVolume;
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
                RP33_exclusive_total[status] = 0;
                RP33_overlapping_total[status] = 0;
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
            // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction: set to 1
            // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection: set to 1

            ResetPopulationDistributionData(0, 0, 8, 1, 1);
        }
        /// <summary>
        /// Reset the total population values and the arrays for the piecewise population distribution functions
        /// </summary>
        /// <param name="a_RP30_initial">Initial volumetric density of fractures</param>
        /// <param name="rmin">Radius of initial fractures</param>
        /// <param name="raysPerFracture">Number of rays comprising each fracture (equal to the number of segments per fracture)</param>
        /// <param name="minStressShadowDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius</param>
        /// <param name="minIntersectionDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius</param>
        public void ResetPopulationDistributionData(double a_RP30_initial, double rmin, ushort raysPerFracture, double minStressShadowDeactivationRatio_in, double minIntersectionDeactivationRatio_in)
        {
            // Set the number of segments per fracture
            noSegments = raysPerFracture;
            ImplicitFracturePopulationDatapoint.NoRaysPerFracture = raysPerFracture;

            // Reset the population total population adjustment factors
            // The adjustment factors arise when the size of the population distribution function arrays are reduced by amalgamating datapoints
            // They represent the difference in area or volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
            RP32_total_adjustment = new Dictionary<RayPropagationStatus, double>();
            RP33_exclusive_adjustment = new Dictionary<RayPropagationStatus, double>();
            RP33_overlapping_adjustment = new Dictionary<RayPropagationStatus, double>();
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                RP32_total_adjustment[status] = 0;
                RP33_exclusive_adjustment[status] = 0;
                RP33_overlapping_adjustment[status] = 0;
            }

            // Set the variables for the minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction and intersection, as a ratio of the propagating fracture radius
            // These are used to calculate the minimum effective radius for overlapping static fracture rays
            minStressShadowDeactivationRatio = minStressShadowDeactivationRatio_in;
            minIntersectionDeactivationRatio = minIntersectionDeactivationRatio_in;

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
            RP33_exclusive_total = new Dictionary<RayPropagationStatus, double>();
            RP33_overlapping_total = new Dictionary<RayPropagationStatus, double>();
            RecalculateTotalPopulationData();
        }
        /// <summary>
        /// Reduce the size of a specified population distribution function array by amalgamating datapoints at a specified regular interval
        /// </summary>
        /// <param name="ArrayToCull">Specified datapoint array</param>
        /// <param name="PointSpacing">Number of points to amalgamate into a single datapoint</param>
        public void CullArray(RayPropagationStatus ArrayToCull, ushort PointSpacing)
        {
            // This function amalgamates a collection of population distribution function array datapoints into a single datapoint, if both the ray lengths and effective ray lengths lie within a specified range (expressed as a ratio)
            // NB this function will remove the smaller datapoints from each collection of points to be amalgamated, and retain the largest datapoint
            // The P32 and P33 adjustments will therefore be negative

            // Get a reference to the list to be culled
            List<ImplicitFracturePopulationDatapoint> listToCull = fracturePopulationDatapoints[ArrayToCull];

            // First sort the fracture population distribution arrays on effective ray length from largest to smallest
            listToCull.Sort();

            // Create variables to calculate the total P32 and P33 adjustments that will be required
            double P32_adjustment = 0;
            double P33_exclusive_adjustment = 0;
            double P33_overlapping_adjustment = 0;

            // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
            double minExclusiveEffectiveRayLength = getMinimumExclusiveStressShadowEffectiveRayLength(ArrayToCull);

            // Loop through the list of datapoints in descending order of index number, i.e. in ascending order of effective ray length
            int currentDataPointNo = listToCull.Count - 1;
            for (int nextDataPointNoToKeep = (currentDataPointNo / PointSpacing) * PointSpacing; nextDataPointNoToKeep >= 0; nextDataPointNoToKeep -= PointSpacing)
            {
                // Get a reference to the next datapoint to keep and cache relevant data locally
                ImplicitFracturePopulationDatapoint nextPointToKeep = listToCull[nextDataPointNoToKeep];
                double nextPoint_Radius = nextPointToKeep.RayLength;
                double nextPoint_EffectiveRadius = nextPointToKeep.EffectiveRayLength;
                double nextPoint_Area = nextPoint_Radius * nextPoint_Radius;
                double nextPoint_Volume = nextPoint_Area * nextPoint_EffectiveRadius;

                // Check whether the next datapoint to keep is an exclusive or an overlapping datapoint
                bool nextPointIsOverlapping = (nextPoint_EffectiveRadius < minExclusiveEffectiveRayLength);

                // Create a variable for the total volumetric density of rays to be amalgamated into the current datapoint 
                double dP30_increment = 0;

                // Loop through the datapoints to cull
                for (; currentDataPointNo > nextDataPointNoToKeep; currentDataPointNo--)
                {
                    // Get a reference to the current datapoint to keep and cache relevant data locally
                    ImplicitFracturePopulationDatapoint currentPoint = listToCull[currentDataPointNo];
                    double currentPoint_dP30 = currentPoint.dRP30;
                    double currentPoint_Radius = currentPoint.RayLength;
                    double currentPoint_EffectiveRadius = listToCull[currentDataPointNo].EffectiveRayLength;
                    double currentPoint_Area = currentPoint_Radius * currentPoint_Radius;
                    double currentPoint_Volume = currentPoint_Area * currentPoint_EffectiveRadius;

                    // Increment the variables for the total volumetric density of rays to be amalgamated into the current datapoint and the total P32 and P33 adjustments
                    dP30_increment += currentPoint_dP30;
                    P32_adjustment += currentPoint_dP30 * (currentPoint_Area - nextPoint_Area);
                    if (nextPointIsOverlapping)
                        P33_overlapping_adjustment += currentPoint_dP30 * (currentPoint_Volume - nextPoint_Volume);
                    else
                        P33_exclusive_adjustment += currentPoint_dP30 * (currentPoint_Volume - nextPoint_Volume);

                    // Remove the datapoint to be culled
                    listToCull.RemoveAt(currentDataPointNo);
                }

                // Increment the total volumetric density of rays associated with the current datapoint to include the datapoints that have been culled
                listToCull[currentDataPointNo].Increment_dP30(dP30_increment);

                // Reduce the current datapoint number by one, so that we skip the point we are keeping
                currentDataPointNo--;
            }

            // Update the adjustment factors
            RP32_total_adjustment[ArrayToCull] += P32_adjustment;
            RP33_exclusive_adjustment[ArrayToCull] += P33_exclusive_adjustment;
            RP33_overlapping_adjustment[ArrayToCull] += P33_overlapping_adjustment;
        }
        /// <summary>
        /// Reduce the size of a specified population distribution function array by amalgamating datapoints representing ray lengths within a specified ratio
        /// </summary>
        /// <param name="ArrayToCull">Specified datapoint array</param>
        /// <param name="LengthRatio">Ratio of ray lengths to amalgamate into a single datapoint</param>
        public void CullArray(RayPropagationStatus ArrayToCull, double LengthRatio)
        {
            // This function amalgamates a collection of population distribution function array datapoints into a single datapoint, if both the ray lengths and effective ray lengths lie within a specified range (expressed as a ratio)
            // NB this function will remove the smaller datapoints from each collection of points to be amalgamated, and retain the largest datapoint
            // The P32 and P33 adjustments will therefore be negative

            // Get a reference to the list to be culled
            List<ImplicitFracturePopulationDatapoint> listToCull = fracturePopulationDatapoints[ArrayToCull];

            // First sort the fracture population distribution arrays on effective ray length from largest to smallest
            listToCull.Sort();

            // Create variables to calculate the total P32 and P33 adjustments that will be required
            double P32_adjustment = 0;
            double P33_exclusive_adjustment = 0;
            double P33_overlapping_adjustment = 0;

            // Get the minimum size for non-overlapping fracture rays for the current ray propagation status
            double minExclusiveEffectiveRayLength = getMinimumExclusiveStressShadowEffectiveRayLength(ArrayToCull);

            // Loop through the list of datapoints in ascending order of index number, i.e. in descending order of effective ray length
            for (int currentDataPointNo = 0; currentDataPointNo < listToCull.Count; currentDataPointNo++)
            {
                // Get a reference to the current datapoint and cache relevant data locally
                ImplicitFracturePopulationDatapoint currentPoint = listToCull[currentDataPointNo];
                double currentPoint_Radius = currentPoint.RayLength;
                double currentPoint_EffectiveRadius = currentPoint.EffectiveRayLength;
                double currentPoint_Area = currentPoint_Radius * currentPoint_Radius;
                double currentPoint_Volume = currentPoint_Area * currentPoint_EffectiveRadius;

                // Check whether the current datapoint is an exclusive or an overlapping datapoint
                bool currentPointIsOverlapping = (currentPoint_EffectiveRadius < minExclusiveEffectiveRayLength);

                // Calculate the minimum ray length and effective ray lengths for the smaller datapoints that will be amalgamated with this datapoint
                double minRayLengthToAmalgamate = currentPoint_Radius / (1 + LengthRatio);
                double minEffectiveRayLengthToAmalgamate = currentPoint_EffectiveRadius / (1 + LengthRatio);

                // Create a variable for the total volumetric density of rays to be amalgamated into the current datapoint 
                double dP30_increment = 0;

                // Loop ahead of the current datapoint to find any other datapoints within the specified effective ray length
                for (int nextDataPointNo = currentDataPointNo + 1; (nextDataPointNo < listToCull.Count) && (listToCull[nextDataPointNo].EffectiveRayLength >= minEffectiveRayLengthToAmalgamate);)
                {
                    // Get a reference to the next datapoint and check if it is also within the specified ray length range
                    ImplicitFracturePopulationDatapoint nextPoint = listToCull[nextDataPointNo];
                    double nextPoint_Radius = nextPoint.RayLength;
                    if (nextPoint_Radius < minRayLengthToAmalgamate)
                    {
                        currentDataPointNo++;
                        nextDataPointNo++;
                        continue;
                    }

                    // Increment the variables for the total volumetric density of rays to be amalgamated into the current datapoint and the total P32 and P33 adjustments
                    double nextPoint_EffectiveRadius = nextPoint.EffectiveRayLength;
                    double nextPoint_dP30 = nextPoint.dRP30;
                    double nextPoint_Area = nextPoint_Radius * nextPoint_Radius;
                    double nextPoint_Volume = nextPoint_Area * nextPoint_EffectiveRadius;
                    dP30_increment += nextPoint_dP30;
                    P32_adjustment += nextPoint_dP30 * (nextPoint_Area - currentPoint_Area);
                    if (currentPointIsOverlapping)
                        P33_overlapping_adjustment += nextPoint_dP30 * (nextPoint_Volume - currentPoint_Volume);
                    else
                        P33_exclusive_adjustment += nextPoint_dP30 * (nextPoint_Volume - currentPoint_Volume);

                    // Remove the datapoint to be culled
                    listToCull.RemoveAt(nextDataPointNo);
                }

                // Increment the total volumetric density of rays associated with the current datapoint to include the datapoints that have been culled
                listToCull[currentDataPointNo].Increment_dP30(dP30_increment);
            }

            // Update the adjustment factors
            RP32_total_adjustment[ArrayToCull] += P32_adjustment;
            RP33_exclusive_adjustment[ArrayToCull] += P33_exclusive_adjustment;
            RP33_overlapping_adjustment[ArrayToCull] += P33_overlapping_adjustment;
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
        /// <param name="minStressShadowDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius</param>
        /// <param name="minIntersectionDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius</param>
        public UnconfinedFractureData(double a_RP30_initial, double rmin, ushort raysPerFracture, double minStressShadowDeactivationRatio_in, double minIntersectionDeactivationRatio_in)
        {
            ResetPopulationDistributionData(a_RP30_initial, rmin, raysPerFracture, minStressShadowDeactivationRatio_in, minIntersectionDeactivationRatio_in);
        }
    }
}
