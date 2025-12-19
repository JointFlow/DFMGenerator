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
        // References to external objects
        /// <summary>
        /// Reference to parent UnconfinedFractureData object
        /// </summary>
        private UnconfinedFractureData ufd;

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
        /// Maximum allowed increment in ray length, that will bring the total ray length to the specified maximum ray length
        /// </summary>
        private double MaxAllowedRayLengthIncrement { get { return ufd.MaximumAllowedRayLength - RayLength; } }
        /// <summary>
        /// Calculated incremental increase in the ray length, based on driving stress and duration of the current timestep - this may extend the ray beyond the specified maximum ray length
        /// </summary>
        public double CalculatedRayLengthIncrement { get; set; }
        /// <summary>
        /// Actual incremental increase in the ray length in the current timestep, taking into account the specified maximum ray length
        /// </summary>
        public double ActualRayLengthIncrement { get { return Math.Min(CalculatedRayLengthIncrement, MaxAllowedRayLengthIncrement); } }
        /// <summary>
        /// Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)
        /// </summary>
        public double dRP30 { get; private set; }
        /// <summary>
        /// Volume of stress shadow around this datapoint, taking into account overlap with stress shadows around this and other datapoints
        /// </summary>
        public double StressShadowVolume { get; set; }
        // Ray propagation status
        /// <summary>
        /// Propagation status of rays represented by this datapoint 
        /// </summary>
        public RayPropagationStatus Status { get; private set; }
        /// <summary>
        /// Flag to indicate whether the next ray length increment will reach the maximum fracture radius
        /// </summary>
        private bool IncrementToMaxRadius { get { return (CalculatedRayLengthIncrement >= MaxAllowedRayLengthIncrement); } }
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
        public double EffectiveRayLengthIncrement { get { return (Status == RayPropagationStatus.FullyActive) ? ActualRayLengthIncrement : ActualRayLengthIncrement / 2; } }
        /// <summary>
        /// Factor to calculate incremental length of rays per unit volume represented by this datapoint; must be multiplied by a geometric factor to get the true dP32 increment
        /// </summary>
        public double dP31factor { get { return dRP30 * RayLength; } }
        /// <summary>
        /// Factor to calculate the area of segments represented by this datapoint; must be multiplied by a geometric factor to get the true dP32
        /// </summary>
        public double dP32factor { get { return dRP30 * RayLength * RayLength; } }
        /// <summary>
        /// Incremental increase in dP32 factor in the current timestep; must be multiplied by a geometric factor to get the true dP32 increment
        /// </summary>
        public double dP32factorIncrement { get { double newRayLength = RayLength + ActualRayLengthIncrement; return dRP30 * ((newRayLength * newRayLength) - (RayLength * RayLength)); } }
        /// <summary>
        /// Factor to calculate the stress shadow volume around the segments represented by this datapoint; must be multiplied by a geometric factor to get the true dP33
        /// </summary>
        public double dP33factor { get { return dRP30 * RayLength * RayLength * EffectiveRayLength; } }
        /// <summary>
        /// Incremental increase in dP33 factor in the current timestep; must be multiplied by a geometric factor to get the true dP33 increment
        /// </summary>
        public double dP33factorIncrement { get { double newRayLength = RayLength + ActualRayLengthIncrement; double newEffectiveRayLength = EffectiveRayLength + EffectiveRayLengthIncrement; return dRP30 * ((newRayLength * newRayLength * newEffectiveRayLength) - (RayLength * RayLength * EffectiveRayLength)); } }
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
        /// Factor to calculate volume of an asymmetric shell of arbitrary width, around the stress shadows surrounding the fractures represented by this datapoint; must be multiplied by a geometric factor to get the true shell volume
        /// </summary>
        /// <param name="circumferentialShellWidth">Width of the shell in the plane of the fracture; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <param name="axialShellWidth">Width of the shell perpendicular to the plane of the fracture; positive for an outer shell (outside the stress shadow), negative for an inner shell (inside the stress shadow)</param>
        /// <returns></returns>
        public double dP33ShellFactor(double circumferentialShellWidth, double axialShellWidth)
        {
            double shellRadius = RayLength + circumferentialShellWidth;
            double effectiveShellRadius = EffectiveRayLength + axialShellWidth;
            if (effectiveShellRadius < 0)
                return 0;
            else if (circumferentialShellWidth < 0)
                return dRP30 * ((RayLength * RayLength * EffectiveRayLength) - (shellRadius * shellRadius * effectiveShellRadius));
            else
                return dRP30 * ((shellRadius * shellRadius * effectiveShellRadius) - (RayLength * RayLength * EffectiveRayLength));
        }
        /// <summary>
        /// Factor to calculate volume of an asymmetric shell of arbitrary inner and outer radius, surrounding the fractures represented by this datapoint; must be multiplied by a geometric factor to get the true shell volume
        /// </summary>
        /// <param name="circumferentialInnerRadius">Inner radius of the shell in the plane of the fracture (alternatively the outer radius can be specified if the shell widths are negative)</param>
        /// <param name="axialInnerRadius">Inner radius of the shell perpendicular to the plane of the fracture (alternatively the outer radius can be specified if the shell widths are negative)</param>
        /// <param name="circumferentialShellWidth">Width of the shell in the plane of the fracture; positive if the inner radius is specified, negative if the outer radius is specified</param>
        /// <param name="axialShellWidth">Width of the shell perpendicular to the plane of the fracture; positive if the inner radius is specified, negative if the outer radius is specified</param>
        /// <returns></returns>
        public double dP33DetachedShellFactor(double circumferentialInnerRadius, double axialInnerRadius, double circumferentialShellWidth, double axialShellWidth)
        {
            double shellRadius = circumferentialInnerRadius + circumferentialShellWidth;
            double effectiveShellRadius = axialInnerRadius + axialShellWidth;
            if (effectiveShellRadius < 0)
                return 0;
            else if (circumferentialShellWidth < 0)
                return dRP30 * ((circumferentialInnerRadius * circumferentialInnerRadius * axialInnerRadius) - (shellRadius * shellRadius * effectiveShellRadius));
            else
                return dRP30 * ((shellRadius * shellRadius * effectiveShellRadius) - (circumferentialInnerRadius * circumferentialInnerRadius * axialInnerRadius));
        }

        // Functions to manipulate data
        /// <summary>
        /// Add the increment in ray length to the current ray length, then reset the increment in ray length to zero
        /// </summary>
        /// <returns>True if the ray length increment will reach the maximum fracture radius, otherwise false</returns>
        public bool IncrementRayLength()
        {
            bool incrementToMaxRadius = IncrementToMaxRadius;
            RayLength += ActualRayLengthIncrement;
            if (Status == RayPropagationStatus.FullyActive)
                PropagationControllingLength += ActualRayLengthIncrement;
            CalculatedRayLengthIncrement = 0;

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
        /// <param name="proportionalIncrementAtDeactivation">Proportion of the ray length increment for the original fully active datapoint to apply before deactivation occurs: 0 assumes deactivation at the start of the current timestep and 1 implies deactivation at the end of the current timestep</param>
        /// <returns>List of ImplicitFracturePopulationDatapoints representing the originally active rays that became restricted or deactivated rays</returns>
        public ImplicitFracturePopulationDatapoint[] DeactivateRays(double proportionalIncrementAtDeactivation)
        {
            // Create an array for the output datapoints - these represent the rays that become deactivated or restricted
            ImplicitFracturePopulationDatapoint[] outputDatapoints;

            // Calculate the probabilities of deactivation due to stress shadow interaction of intersection during the timestep
            double Phi = PhiII * PhiIJ;
            double PhiII_ratio = (PhiII > 0) ? Math.Log(PhiII) / Math.Log(Phi) : 1;
            double PhiIJ_ratio = 1 - PhiII_ratio;// (PhiIJ_Ray_M > 0) ? Math.Log(PhiIJ_Ray_M) / Math.Log(Phi_Ray_M) : 1;
            double F_II_M = (PhiII_ratio > 0) ? (1 - Phi) * PhiII_ratio : 0;
            double F_IJ_M = (PhiIJ_ratio > 0) ? (1 - Phi) * PhiIJ_ratio : 0;

            // Calculate the proportion of the original fully active ray increment to be applied before deactivation occurs
            double preDeactivationIncrement = proportionalIncrementAtDeactivation * CalculatedRayLengthIncrement;
            double lengthAtDeactivation = Math.Min(RayLength + preDeactivationIncrement, ufd.MaximumAllowedRayLength);
            switch (Status)
            {
                case RayPropagationStatus.FullyActive:
                    // There will be three datapoints in the output array, representing the rays that become restricted and deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[3];
                    // Calculate the total proportion of fully active fractures for which no rays will be deactivated during the current timestep
                    // This will be the probability that a single ray will not be deactivated (Phi_Ray_M) to the power of the number of rays per fracture
                    double Phi_Fracture_M = Math.Pow(Phi, ufd.RaysPerFracture);
                    // The first datapoint in the output array represents the new restricted rays
                    // Note that this function will only increment the restricted rays until the point of deactivation
                    // This is because this object does not have access to the mechanical property data needed to calculate the increment to the end of the timestep
                    // The full increment to the end of the timestep must therefore be calculated in the grandparent UnconfinedFractureSet object
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, preDeactivationIncrement, dRP30 * (Phi - Phi_Fracture_M), StressShadowVolume * (Phi - Phi_Fracture_M), RayPropagationStatus.Restricted, lengthAtDeactivation, ufd);
                    // The second datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, preDeactivationIncrement, dRP30 * F_II_M, StressShadowVolume * F_II_M, RayPropagationStatus.StaticStressShadow, lengthAtDeactivation, ufd);
                    // The third datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[2] = new ImplicitFracturePopulationDatapoint(RayLength, preDeactivationIncrement, dRP30 * F_IJ_M, StressShadowVolume * F_IJ_M, RayPropagationStatus.StaticIntersection, lengthAtDeactivation, ufd);
                    // Reduce the volumetric density and stress shadow volume of fully active rays represented by this datapoint
                    dRP30 *= Phi_Fracture_M;
                    StressShadowVolume *= Phi_Fracture_M;
                    break;
                case RayPropagationStatus.Restricted:
                    // There will be two datapoints in the output array, representing the rays that become deactivated due to stress shadow interaction and intersection respectively
                    outputDatapoints = new ImplicitFracturePopulationDatapoint[2];
                    // The first datapoint in the output array represents the new static rays due to stress shadow interaction
                    outputDatapoints[0] = new ImplicitFracturePopulationDatapoint(RayLength, preDeactivationIncrement, dRP30 * F_II_M, StressShadowVolume * F_II_M, RayPropagationStatus.StaticStressShadow, PropagationControllingLength, ufd);
                    // The second datapoint in the output array represents the new static rays due to intersection
                    outputDatapoints[1] = new ImplicitFracturePopulationDatapoint(RayLength, preDeactivationIncrement, dRP30 * F_IJ_M, StressShadowVolume * F_IJ_M, RayPropagationStatus.StaticIntersection, PropagationControllingLength, ufd);
                    // Reduce the volumetric density and stress shadow volume of restricted rays represented by this datapoint
                    dRP30 *= Phi;
                    StressShadowVolume *= Phi;
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
        /// Increment the dRP30 value for the current datapoint by a specified amount
        /// </summary>
        /// <param name="dRP30_increment">Amount by which to increment the dP30 value</param>
        public void Increment_dRP30(double dRP30_increment)
        {
            dRP30 += dRP30_increment;
        }

        // Constructors
        /// <summary>
        /// Default constructor: Create a datapoint representing fully active rays belonging to a nucleating fracture
        /// In this case the rays start with length zero, but we add an increment to bring them to the minimum ray length
        /// </summary>
        /// <param name="rayLength_in">Initial length of rays represented by this datapoint</param>
        /// <param name="dP30_in">Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)</param>
        /// <param name="ufd_in">Reference to parent UnconfinedFractureData object</param>
        public ImplicitFracturePopulationDatapoint(double rayLength_in, double dP30_in, UnconfinedFractureData ufd_in) : this(0, rayLength_in, dP30_in, 0, RayPropagationStatus.FullyActive, rayLength_in, ufd_in)
        {
            // Defaults:
            // The current ray length will be set to 0, and the specified initial ray length will instead be specified as the current ray increment
            // This will allow for ray deactivation if they intersect or interact with the stress shadow of another fracture before they reach the specified initial length
            // Stress shadow volume: Set to 0; this will be incremented along with the ray length
            // Ray propagation status: set to FullyActive
            // Length of the ray controlling the propagation rate of this ray: For a fully active ray this will always be the ray length
        }
        /// <summary>
        /// Constructor: specify ray length, dRP30, propagation status, and propagation controlling length
        /// </summary>
        /// <param name="rayLength_in">Current length of rays represented by this datapoint</param>
        /// <param name="rayLengthIncrement_in">Incremental increase in the ray length in the current timestep</param>
        /// <param name="dRP30_in">Volumetric density of rays represented by this datapoint (NB this is an incremental rather than a cumulative population density)</param>
        /// <param name="stressShadowVolume_in">Volume of stress shadow around this datapoint, taking into account overlap with stress shadows around this and other datapoints</param>
        /// <param name="status_in">Propagation status of rays represented by this datapoint</param>
        /// <param name="propagationControllingLength_in">Length of the ray controlling the propagation rate of this ray</param>
        /// <param name="ufd_in">Reference to parent UnconfinedFractureData object</param>
        private ImplicitFracturePopulationDatapoint(double rayLength_in, double rayLengthIncrement_in, double dRP30_in, double stressShadowVolume_in, RayPropagationStatus status_in, double propagationControllingLength_in, UnconfinedFractureData ufd_in)
        {
            ufd = ufd_in;
            RayLength = rayLength_in;
            CalculatedRayLengthIncrement = rayLengthIncrement_in;
            dRP30 = dRP30_in;
            StressShadowVolume = stressShadowVolume_in;
            Status = status_in;
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
        // References to external objects
        /// <summary>
        /// Reference to parent UnconfinedFractureSet object
        /// </summary>
        private UnconfinedFractureSet ufs;

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
        /// Total length per unit volume of fully active fracture rays (i.e. all rays in the fracture are active)
        /// </summary>
        public double a_RP31_total { get { return RP31_total[RayPropagationStatus.FullyActive]; } }
        /// <summary>
        /// Total length per unit volume of restricted fracture rays (i.e. at least one other ray in the fracture is deactivated)
        /// </summary>
        public double r_RP31_total { get { return RP31_total[RayPropagationStatus.Restricted]; } }
        /// <summary>
        /// Total length per unit volume of static fracture rays deactivated due to stress shadow interaction
        /// </summary>
        public double sII_RP31_total { get { return RP31_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Total length per unit volume of static fracture rays deactivated due to intersection
        /// </summary>
        public double sIJ_RP31_total { get { return RP31_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Total length per unit volume of static fracture rays deactivated due to reaching the maximum radius
        /// </summary>
        public double sMR_RP31_total { get { return RP31_total[RayPropagationStatus.StaticMaxRadius]; } }
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
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        public double a_RP33_total { get { return RP33_total[RayPropagationStatus.FullyActive]; } }
        /// <summary>
        /// Volumetric ratio of restricted fracture rays (i.e. at least one other ray in the fracture is deactivated)
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        public double r_RP33_total { get { return RP33_total[RayPropagationStatus.Restricted]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to stress shadow interaction
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        public double sII_RP33_total { get { return RP33_total[RayPropagationStatus.StaticStressShadow]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to intersection
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        public double sIJ_RP33_total { get { return RP33_total[RayPropagationStatus.StaticIntersection]; } }
        /// <summary>
        /// Maximum volumetric ratio of static fracture rays deactivated due to reaching the maximum radius
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        public double sMR_RP33_total { get { return RP33_total[RayPropagationStatus.StaticMaxRadius]; } }
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
                return RP30 / (double)RaysPerFracture;
            }
        }
        /// <summary>
        /// Total diameter per unit volume of all fractures
        /// </summary>
        public double FP31_total
        {
            get
            {
                double RP31 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP31 += RP31_total[status];
                return RP31 * 2 / (double)RaysPerFracture;
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
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        public double FP33_total
        {
            get
            {
                double RP33 = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    RP33 += RP33_total[status];
                return RP33;
            }
        }
        /// <summary>
        /// Total fracture stress shadow volume
        /// This value takes into account stress shadow overlap
        /// It will therefore be less than FP33 * (W/r)
        /// </summary>
        public double StressShadowVolume_total
        {
            get
            {
                double psi = 0;
                foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                    psi += Psi_total[status];
                if (psi > 1)
                    psi = 1;
                return psi;
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

            return RP30 / (double)RaysPerFracture;
        }
        /// <summary>
        /// Get the total diameter per unit volume of all fractures with effective radius greater than a specified value
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P31 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP31(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            double RP31 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < CutoffRadius)
                        break;
                    RP31 += datapoint.dP31factor;
                }

            return RP31 * 2 / (double)RaysPerFracture;
        }
        /// <summary>
        /// Get the total mean linear density of all fracture segments with effective radius greater than a specified value
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

            return RP32 * (Math.PI / (double)RaysPerFracture);
        }
        /// <summary>
        /// Get the maximum volumetric ratio of all fracture segments with effective radius greater than a specified value
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative P33 density of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_FP33(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            // It also ignores the adjustment factors - these are only calculated for the entire fracture population

            double RP33 = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < CutoffRadius)
                        break;
                    RP33 += datapoint.dP33factor;
                }

            return RP33 * (4d / 3d) * (Math.PI / (double)RaysPerFracture);
        }
        /// <summary>
        /// Get the total stress shadow volume of all fracture segments with effective radius greater than a specified value
        /// This value takes into account stress shadow overlap
        /// It will therefore be less than cumulative_FP33 * (W/r)
        /// </summary>
        /// <param name="CutoffRadius">Minimum effective radius</param>
        /// <returns>Cumulative stress shadow volume of all fractures with effective radius greater than or equal to the specified minimum</returns>
        public double cumulative_StressShadowVolume(double CutoffRadius)
        {
            // NB this calculation assumes that the population data arrays have already been sorted from largest to smallest
            // It also ignores the adjustment factors - these are only calculated for the entire fracture population

            double psi = 0;
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    if (datapoint.EffectiveRayLength < CutoffRadius)
                    {
                        if (datapoint.EffectiveRayLength < CutoffRadius)
                            break;
                        psi += datapoint.dP33factor;
                    }
                }

            return psi;
        }

        /// <summary>
        /// Dictionary object containing volumetric density of fracture rays for all ray propagation statuses
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP30_total;
        /// <summary>
        /// Dictionary object containing total length per unit volume of fracture rays for all ray propagation statuses
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP31_total;
        /// <summary>
        /// Dictionary object containing mean linear density of fracture rays for all ray propagation statuses
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP32_total;
        /// <summary>
        /// Dictionary object containing the total maximum volumetric ratio of fracture rays for all ray propagation statuses
        /// NB This does not take into account stress shadow overlap
        /// It will therefore be an overestimate of the true volumetric density and should not be used for calculating total stress shadow volume
        /// However it can be used for porosity calculations since fracture aperture is much smaller than fracture radius, so overlap is negligible
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_total;
        /// <summary>
        /// Dictionary object containing total fracture stress shadow volume for all ray propagation statuses
        /// This value takes into account stress shadow overlap
        /// It will therefore be less than RP33_total * (W/r)
        /// </summary>
        private Dictionary<RayPropagationStatus, double> Psi_total;
        /// <summary>
        /// Dictionary object containing the adjustment factors for total length per unit volume of fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in area between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP31_total_adjustment;
        /// <summary>
        /// Dictionary object containing the adjustment factors for mean linear density of fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in area between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP32_total_adjustment;
        /// <summary>
        /// Dictionary object containing the adjustment factors for total maximum volumetric ratio of fracture rays for all ray propagation statuses
        /// The adjustment factor arises when the size of the population distribution function arrays are reduced by amalgamating datapoints
        /// It represents the difference in volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
        /// </summary>
        private Dictionary<RayPropagationStatus, double> RP33_total_adjustment;
        /// <summary>
        /// Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius
        /// </summary>
        private double minStressShadowDeactivationRatio;
        /// <summary>
        /// Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius
        /// </summary>
        private double minIntersectionDeactivationRatio;
        /// <summary>
        /// Recalculate the total population data from the piecewise population distribution function arrays
        /// </summary>
        public void RecalculateTotalPopulationData()
        {
            // Reset the total population values to zero
            ResetTotalPopulationData();

            // Sort each of the fracture population distribution arrays on effective ray length from largest to smallest
            // The arrays must be in size order when calculating optimal timestep duration, fracture deactivation probabilities, cumulative densities above a specified cutoff and when culling datapoints
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                fracturePopulationDatapoints[status].Sort();

            // Reset the maximum ray length
            double MaximumEffectiveRayLength = 0;

            // Recalculate all of the total fracture population distribution values
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Loop through the arrays for population distribution functions
                foreach (ImplicitFracturePopulationDatapoint dataPoint in fracturePopulationDatapoints[status])
                {
                    RP30_total[status] += dataPoint.dRP30;
                    RP31_total[status] += dataPoint.dP31factor;
                    RP32_total[status] += dataPoint.dP32factor;
                    RP33_total[status] += dataPoint.dP33factor;
                    Psi_total[status] += dataPoint.StressShadowVolume;
                    if (MaximumEffectiveRayLength < dataPoint.EffectiveRayLength)
                        MaximumEffectiveRayLength = dataPoint.EffectiveRayLength;
                }

                // Add the adjustment factors to the P31, P32 and P33 values
                RP31_total[status] += RP31_total_adjustment[status];
                RP32_total[status] += RP32_total_adjustment[status];
                RP33_total[status] += RP33_total_adjustment[status];

                // Apply the area and volume multipliers
                // No multipliers required for RP31 or stress shadow volume
                RP32_total[status] *= (Math.PI / (double)RaysPerFracture);
                RP33_total[status] *= (4d / 3d) * (Math.PI / (double)RaysPerFracture);
            }

            // Recalculate mean ray lengths
            double activeRP30 = RP30_total[RayPropagationStatus.FullyActive] + RP30_total[RayPropagationStatus.Restricted];
            double staticRP30 = RP30_total[RayPropagationStatus.StaticStressShadow] + RP30_total[RayPropagationStatus.StaticIntersection] + RP30_total[RayPropagationStatus.StaticMaxRadius];
            double activeRP31 = RP31_total[RayPropagationStatus.FullyActive] + RP31_total[RayPropagationStatus.Restricted];
            double staticRP31 = RP31_total[RayPropagationStatus.StaticStressShadow] + RP31_total[RayPropagationStatus.StaticIntersection] + RP31_total[RayPropagationStatus.StaticMaxRadius];
            if (activeRP30 > 0)
                MeanActiveRayLength = activeRP31 / activeRP30;
            else
                MeanActiveRayLength = 0;
            if (staticRP30 > 0)
                MeanStaticRayLength = staticRP31 / staticRP30;
            else
                MeanStaticRayLength = 0;
            if ((activeRP30 + staticRP30) > 0)
                MeanRayLength = (activeRP31 + staticRP31) / (activeRP30 + staticRP30);
            else
                MeanRayLength = 0;
        }
        /// <summary>
        /// Recalculate the stress shadow volume data from the piecewise population distribution function arrays
        /// </summary>
        public void RecalculateStressShadowVolumeData()
        {
            // Recalculate all of the total fracture population distribution values
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                // Set the current value for the speciofied ray status to zero
                Psi_total[status] = 0;

                // Loop through the arrays for population distribution functions
                foreach (ImplicitFracturePopulationDatapoint dataPoint in fracturePopulationDatapoints[status])
                {
                    Psi_total[status] += dataPoint.StressShadowVolume;
                }
            }
        }

        // Fracture population distribution functions
        /// <summary>
        /// Arrays for piecewise population distribution functions
        /// </summary>
        public Dictionary<RayPropagationStatus, List<ImplicitFracturePopulationDatapoint>> fracturePopulationDatapoints;
        /// <summary>
        /// Number of rays comprising each fracture
        /// </summary>
        public ushort RaysPerFracture { get { return ufs.RaysPerFracture; } }
        /// <summary>
        /// Maximum effective ray length of all the current fractures
        /// </summary>
        public double MaximumEffectiveRayLength { get; private set; }
        /// <summary>
        /// Maximum allowed fracture ray length from the parent UnconfinedFractureSet object
        /// </summary>
        public double MaximumAllowedRayLength { get { return ufs.MaximumFractureRadius; } }
        /// <summary>
        /// Mean length of all rays
        /// </summary>
        public double MeanRayLength { get; private set; }
        /// <summary>
        /// Mean length of all currently active rays (status FullyActive or Restricted)
        /// </summary>
        public double MeanActiveRayLength { get; private set; }
        /// <summary>
        /// Mean length of all static rays (status StaticStressShadow, StaticIntersection or StaticMaxRadius)
        /// </summary>
        public double MeanStaticRayLength { get; private set; }

        // Data calculation and output functions
        /// <summary>
        /// Calculate increment in the stress shadow volume for each datapoint based on the calculated ray length increment
        /// </summary>
        public void CalculateStressShadowIncrementsFromRayLengthIncrement()
        {
            // Cache the stress shadow width ratio locally
            double stressShadowWidthRatio = ufs.Max_F_StressShadowWidthRatio;

            // Loop through each datapoint and calculate the stress shadow increment from the ray length increment
            // Because stress shadows can overlap, an increase in the stress shadow volume of one datapoint will cause a decrease in the stress shadow volume of all datapoints with which it can overlap
            foreach (RayPropagationStatus status in new RayPropagationStatus[4] { RayPropagationStatus.FullyActive, RayPropagationStatus.Restricted, RayPropagationStatus.StaticStressShadow, RayPropagationStatus.StaticIntersection })
            {
                int noDataPoints = fracturePopulationDatapoints[status].Count;
                for (int datapointNo = noDataPoints - 1; datapointNo >= 0; datapointNo--)
                {
                    ImplicitFracturePopulationDatapoint datapoint = fracturePopulationDatapoints[status][datapointNo];
                    if (datapoint.ActualRayLengthIncrement > 0)
                    {
                        // Calculate the increment in stress shadow volume due to growth of this fracture
                        double stressShadowVolumeIncrement = datapoint.dP33factorIncrement * stressShadowWidthRatio * (4d / 3d) * Math.PI / (double)RaysPerFracture;
                        datapoint.StressShadowVolume += stressShadowVolumeIncrement;

                        // The stress shadow increment for this datapoint may overlap the stress shadows around any other datapoint with effective radius less than the minimum radius needed to deactivate this ray
                        // We will therefore reduce their stress shadow width in proportion to the increase in the stress shadow of this datapoint
                        double stressShadowReductionFactor = 1 - stressShadowVolumeIncrement;
                        if (stressShadowReductionFactor < 0)
                            stressShadowReductionFactor = 0;
                        double minStressShadowDeactivationRadius = minStressShadowDeactivationRatio * datapoint.EffectiveRayLength;
                        foreach (RayPropagationStatus status2 in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                            foreach (ImplicitFracturePopulationDatapoint datapoint2 in fracturePopulationDatapoints[status2])
                            {
                                if (datapoint2.EffectiveRayLength < minStressShadowDeactivationRadius)
                                    datapoint2.StressShadowVolume *= stressShadowReductionFactor;
                            }
                    }
                }
            }
        }
        /// <summary>
        /// Calculate increment in the stress shadow volume for each datapoint based on a change in the stress shadow width
        /// </summary>
        /// <param name="dW_Wi">Change in stress shadow width:fracture radius ratio as a proportion of initial stress shadow width ratio</param>
        public void CalculateStressShadowIncrementsFromStressShadowWidthChange(double dW_Wi)
        {
            // Loop through each datapoint and calculate the stress shadow volume associated with the specified change in the stress shadow width:fracture radius ratio, taking into account possible stress shadow overlap 
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    // Calculate the change in stress shadow volume for this datapoint
                    double stressShadowVolumeIncrement = datapoint.StressShadowVolume * dW_Wi;
                    if (dW_Wi > 0)
                    {
                        // If the stress shadow width is growing, some of the additional stress shadow volume may overlap with stress shadows around other fractures (both existing and additional)
                        // In this case we will calculate a multiplier to account for the overlap with existing stress shadow volume and with additional stress shadow volume around other fractures
                        double initialStressShadowVolume = StressShadowVolume_total;
                        double incrementOverlapMultiplier = (1 - initialStressShadowVolume) * Math.Exp(-initialStressShadowVolume * dW_Wi);
                        stressShadowVolumeIncrement *= incrementOverlapMultiplier;
                    }
                    else
                    {
                        // If the stress shadow volume is shrinking, we assume there is no overlap in the lost volume
                        // This is not strictly true, but the overlap is likely to be much smaller than for growing stress shadows
                    }

                    // Update the stress shadow volume for this datapoint
                    datapoint.StressShadowVolume += stressShadowVolumeIncrement;
                }

        }
        /// <summary>
        /// Add the calculated ray length increment to the ray length and reset the increment to zero for each datapoint
        /// </summary>
        public void ApplyRayLengthIncrements()
        {
            // Loop through each datapoint and apply the calculated ray length increment
            foreach (RayPropagationStatus status in new RayPropagationStatus[4] { RayPropagationStatus.FullyActive, RayPropagationStatus.Restricted, RayPropagationStatus.StaticStressShadow, RayPropagationStatus.StaticIntersection })
            {
                int noDataPoints = fracturePopulationDatapoints[status].Count;
                for (int datapointNo = noDataPoints - 1; datapointNo >= 0; datapointNo--)
                {
                    ImplicitFracturePopulationDatapoint datapoint = fracturePopulationDatapoints[status][datapointNo];
                    if (datapoint.ActualRayLengthIncrement > 0)
                    {
                        // The ImplicitFracturePopulationDatapoint.IncrementRayLength() returns true if the ray length increment will reach the maximum fracture radius, otherwise false
                        bool maxRadiusReached = datapoint.IncrementRayLength();

                        // If any rays have reached the maximum specified length, deactivate them and move the appropriate datapoints to the StaticMaxRadius array
                        if (maxRadiusReached)
                        {
                            fracturePopulationDatapoints[RayPropagationStatus.StaticMaxRadius].Add(datapoint);
                            fracturePopulationDatapoints[status].RemoveAt(datapointNo);
                        }
                    }
                }
            }
        }
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
            // Cache the ray length, stress shadow width and minimum stress shadow deactivation radius of the specified datapoint locally
            double minStressShadowDeactivationRadius = dtc_effectiveRaylength * minStressShadowDeactivationRatio;
            double stressShadowHalfWidthRatio = ufs.Max_F_StressShadowWidthRatio / 2;
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
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
                {
                    // Check if it is large enough to deactivate the current fracture - if not we can ignore it
                    if (datapoint.EffectiveRayLength >= minStressShadowDeactivationRadius)
                    {
                        stressShadowVolume += datapoint.StressShadowVolume;
                        exclusiveOuterExclusionZoneVolume += (datapoint.dP33ShellFactor(dtc_rayLength, dtc_stressShadowHalfWidth) * (4d / 3d) * Math.PI / (double)RaysPerFracture);
                    }
                }
            }

            // Calculate the inverse stress shadow volume, and the clear zone volume taking into account overlap of the outer shells
            InverseStressShadowVolume = 1 - StressShadowVolume_total;
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
            double minStressShadowDeactivationRadius = DatapointToCheck.EffectiveRayLength * minStressShadowDeactivationRatio;
            double dtc_rayLength = DatapointToCheck.RayLength;
            double stressShadowHalfWidthRatio = ufs.Max_F_StressShadowWidthRatio / 2;
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
                foreach (ImplicitFracturePopulationDatapoint datapoint in fracturePopulationDatapoints[status])
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

        // Reset and data input functions
        /// <summary>
        /// Reset the total population values to zero 
        /// </summary>
        private void ResetTotalPopulationData()
        {
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                RP30_total[status] = 0;
                RP31_total[status] = 0;
                RP32_total[status] = 0;
                RP33_total[status] = 0;
                Psi_total[status] = 0;
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
            // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction: set to 1
            // Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection: set to 1

            ResetPopulationDistributionData(0, 0, 1, 1);
        }
        /// <summary>
        /// Reset the total population values and the arrays for the piecewise population distribution functions
        /// </summary>
        /// <param name="a_RP30_initial">Initial volumetric density of fracture rays</param>
        /// <param name="rmin">Radius of initial fractures</param>
        /// <param name="minStressShadowDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius</param>
        /// <param name="minIntersectionDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius</param>
        public void ResetPopulationDistributionData(double a_RP30_initial, double rmin, double minStressShadowDeactivationRatio_in, double minIntersectionDeactivationRatio_in)
        {
            // Reset the population total population adjustment factors
            // The adjustment factors arise when the size of the population distribution function arrays are reduced by amalgamating datapoints
            // They represent the difference in area or volume between the actual size of fractures represented by datapoints that are removed and the size represented by the datapoint they are reassigned to
            RP31_total_adjustment = new Dictionary<RayPropagationStatus, double>();
            RP32_total_adjustment = new Dictionary<RayPropagationStatus, double>();
            RP33_total_adjustment = new Dictionary<RayPropagationStatus, double>();
            foreach (RayPropagationStatus status in Enum.GetValues(typeof(RayPropagationStatus)).Cast<RayPropagationStatus>())
            {
                RP31_total_adjustment[status] = 0;
                RP32_total_adjustment[status] = 0;
                RP33_total_adjustment[status] = 0;
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
                fracturePopulationDatapoints[RayPropagationStatus.FullyActive].Add(new ImplicitFracturePopulationDatapoint(rmin, a_RP30_initial, this));

            // Reset and recalculate the total population values
            RP30_total = new Dictionary<RayPropagationStatus, double>();
            RP31_total = new Dictionary<RayPropagationStatus, double>();
            RP32_total = new Dictionary<RayPropagationStatus, double>();
            RP33_total = new Dictionary<RayPropagationStatus, double>();
            Psi_total = new Dictionary<RayPropagationStatus, double>();
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

            // Create variables to calculate the total P31, P32 and P33 adjustments that will be required
            double P31_adjustment = 0;
            double P32_adjustment = 0;
            double P33_adjustment = 0;

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

                // Create variables for the total volumetric density of rays and total stress shadow volume to be amalgamated into the current datapoint 
                double dP30_increment = 0;
                double stressShadowVolumeIncrement = 0;

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

                    // Increment the variables for the total volumetric density and stress shadow volume of rays to be amalgamated into the current datapoint and the total P32 and P33 adjustments
                    dP30_increment += currentPoint_dP30;
                    P31_adjustment += currentPoint_dP30 * (currentPoint_Radius - nextPoint_Radius);
                    P32_adjustment += currentPoint_dP30 * (currentPoint_Area - nextPoint_Area);
                    P33_adjustment += currentPoint_dP30 * (currentPoint_Volume - nextPoint_Volume);
                    stressShadowVolumeIncrement += currentPoint.StressShadowVolume;

                    // Remove the datapoint to be culled
                    listToCull.RemoveAt(currentDataPointNo);
                }

                // Increment the total volumetric density of rays and stress shadow volume associated with the current datapoint to include the datapoints that have been culled
                listToCull[currentDataPointNo].Increment_dRP30(dP30_increment);
                listToCull[currentDataPointNo].StressShadowVolume += stressShadowVolumeIncrement;

                // Reduce the current datapoint number by one, so that we skip the point we are keeping
                currentDataPointNo--;
            }

            // Update the adjustment factors
            RP31_total_adjustment[ArrayToCull] += P31_adjustment;
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
            // This function amalgamates a collection of population distribution function array datapoints into a single datapoint, if both the ray lengths and effective ray lengths lie within a specified range (expressed as a ratio)
            // NB this function will remove the smaller datapoints from each collection of points to be amalgamated, and retain the largest datapoint
            // The P32 and P33 adjustments will therefore be negative

            // Get a reference to the list to be culled
            List<ImplicitFracturePopulationDatapoint> listToCull = fracturePopulationDatapoints[ArrayToCull];

            // First sort the fracture population distribution arrays on effective ray length from largest to smallest
            listToCull.Sort();

            // Create variables to calculate the total P32 and P33 adjustments that will be required
            double P31_adjustment = 0;
            double P32_adjustment = 0;
            double P33_adjustment = 0;

            // Loop through the list of datapoints in ascending order of index number, i.e. in descending order of effective ray length
            for (int currentDataPointNo = 0; currentDataPointNo < listToCull.Count; currentDataPointNo++)
            {
                // Get a reference to the current datapoint and cache relevant data locally
                ImplicitFracturePopulationDatapoint currentPoint = listToCull[currentDataPointNo];
                double currentPoint_Radius = currentPoint.RayLength;
                double currentPoint_EffectiveRadius = currentPoint.EffectiveRayLength;
                double currentPoint_Area = currentPoint_Radius * currentPoint_Radius;
                double currentPoint_Volume = currentPoint_Area * currentPoint_EffectiveRadius;

                // Calculate the minimum ray length and effective ray lengths for the smaller datapoints that will be amalgamated with this datapoint
                double minRayLengthToAmalgamate = currentPoint_Radius / (1 + LengthRatio);
                double minEffectiveRayLengthToAmalgamate = currentPoint_EffectiveRadius / (1 + LengthRatio);

                // Create variables for the total volumetric density of rays and total stress shadow volume to be amalgamated into the current datapoint 
                double dP30_increment = 0;
                double stressShadowVolumeIncrement = 0;

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

                    // Increment the variables for the total volumetric density and stress shadow volume of rays to be amalgamated into the current datapoint and the total P32 and P33 adjustments
                    double nextPoint_EffectiveRadius = nextPoint.EffectiveRayLength;
                    double nextPoint_dP30 = nextPoint.dRP30;
                    double nextPoint_Area = nextPoint_Radius * nextPoint_Radius;
                    double nextPoint_Volume = nextPoint_Area * nextPoint_EffectiveRadius;
                    dP30_increment += nextPoint_dP30;
                    P31_adjustment += nextPoint_dP30 * (currentPoint_Radius - nextPoint_Radius);
                    P32_adjustment += nextPoint_dP30 * (nextPoint_Area - currentPoint_Area);
                    P33_adjustment += nextPoint_dP30 * (nextPoint_Volume - currentPoint_Volume);
                    stressShadowVolumeIncrement += currentPoint.StressShadowVolume;

                    // Remove the datapoint to be culled
                    listToCull.RemoveAt(nextDataPointNo);
                }

                // Increment the total volumetric density of rays and stress shadow volume associated with the current datapoint to include the datapoints that have been culled
                listToCull[currentDataPointNo].Increment_dRP30(dP30_increment);
                listToCull[currentDataPointNo].StressShadowVolume += stressShadowVolumeIncrement;
            }

            // Update the adjustment factors
            RP31_total_adjustment[ArrayToCull] += P31_adjustment;
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
        /// <param name="ufs_in">Reference to parent UnconfinedFractureSet object</param>
        public UnconfinedFractureData(UnconfinedFractureSet ufs_in)
        {
            ufs = ufs_in;
            ResetPopulationDistributionData();
        }
        /// <summary>
        /// Constructor: Set an initial fracture population of minimum radius
        /// </summary>
        /// <param name="a_RP30_initial">Initial volumetric density of fractures</param>
        /// <param name="rmin">Radius of initial fractures</param>
        /// <param name="minStressShadowDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to stress shadow interaction, as a ratio of the propagating fracture radius</param>
        /// <param name="minIntersectionDeactivationRatio_in">Minimum radius of unconfined fractures able to cause deactivation of a propagating unconfined fracture due to intersection, as a ratio of the propagating fracture radius</param>
        /// <param name="ufs_in">Reference to parent UnconfinedFractureSet object</param>
        public UnconfinedFractureData(double a_RP30_initial, double rmin, double minStressShadowDeactivationRatio_in, double minIntersectionDeactivationRatio_in, UnconfinedFractureSet ufs_in)
        {
            ufs = ufs_in;
            ResetPopulationDistributionData(a_RP30_initial, rmin, minStressShadowDeactivationRatio_in, minIntersectionDeactivationRatio_in);
        }
    }
}
