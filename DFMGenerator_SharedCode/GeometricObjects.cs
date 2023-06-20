using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Enumerator use to control functions to calculate crossover points
    /// </summary>
    public enum CrossoverType { Extend, Trim, Restrict }
    /// <summary>
    /// Enumerator for the components of a 3-dimensional Cartesian vector
    /// </summary>
    public enum VectorComponents { X, Y, Z }
    /// <summary>
    /// Enumerator for the components of a second order symmetrical 3-dimensional Cartesian tensor
    /// </summary>
    public enum Tensor2SComponents { XX, YY, ZZ, XY, YZ, ZX }

    /// <summary>
    /// Point with coordinates specified in the local reference frame of the fracture set (i.e. IJK coordinates)
    /// </summary>
    class PointIJK
    {
        // Geometric data
        /// <summary>
        /// I coordinate - parallel to fracture strike
        /// </summary>
        public double I { get; set; }
        /// <summary>
        /// J coordinate - perpendicular to fracture strike
        /// </summary>
        public double J { get; set; }
        /// <summary>
        /// K coordinate - relative to centre of gridblock, positive up
        /// </summary>
        public double K { get; set; }
        /// <summary>
        /// Set coordinates to the specified I, J and K values
        /// </summary>
        /// <param name="I_in">New I coordinate - parallel to fracture strike</param>
        /// <param name="J_in">New J coordinate - perpendicular to fracture strike</param>
        /// <param name="K_in">New K coordinate - relative to centre of gridblock, positive down</param>
        public void SetCoordinates(double I_in, double J_in, double K_in)
        {
            I = I_in;
            J = J_in;
            K = K_in;
        }
        /// <summary>
        /// Set coordinates to match those of specified point; use this to copy the data from a specified point, rather than the reference
        /// </summary>
        /// <param name="point_in">Point to copy coordinates from</param>
        public void SetCoordinates(PointIJK point_in)
        {
            I = point_in.I;
            J = point_in.J;
            K = point_in.K;
        }

        // Constructors
        /// <summary>
        /// Constructor to create a new point with specified coordinates
        /// </summary>
        /// <param name="I_in">I coordinate - parallel to fracture strike</param>
        /// <param name="J_in">J coordinate - perpendicular to fracture strike</param>
        /// <param name="K_in">K coordinate - relative to centre of gridblock, positive down</param>
        public PointIJK(double I_in, double J_in, double K_in)
        {
            I = I_in;
            J = J_in;
            K = K_in;
        }
        /// <summary>
        /// Constructor to create a copy of an existing IJK point
        /// </summary>
        /// <param name="point_in">PointIJK object to copy</param>
        public PointIJK(PointIJK point_in)
        {
            I = point_in.I;
            J = point_in.J;
            K = point_in.K;
        }
    }

    /// <summary>
    /// Point with coordinates specified in the global reference frame of the grid (i.e. XYZ coordinates)
    /// </summary>
    class PointXYZ
    {
        // Geometric data
        /// <summary>
        /// X coordinate - to east
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Y coordinate - to north
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// Z coordinate - upwards positive
        /// </summary>
        public double Z { get; set; }
        /// <summary>
        /// Depth - downwards positive
        /// </summary>
        public double Depth { get { return -Z; } set { Z = -value; } }
        /// <summary>
        /// Set coordinates to the specified X, Y and Z values
        /// </summary>
        /// <param name="X_in">New X coordinate</param>
        /// <param name="Y_in">New Y coordinate</param>
        /// <param name="Z_in">New Z coordinate - positive downwards</param>
        public void SetCoordinates(double X_in, double Y_in, double Z_in)
        {
            X = X_in;
            Y = Y_in;
            Z = Z_in;
        }
        /// <summary>
        /// Set coordinates to match those of specified point; use this to copy the data from a specified point, rather than the reference
        /// </summary>
        /// <param name="point_in">Point to copy coordinates from</param>
        public void SetCoordinates(PointXYZ point_in)
        {
            X = point_in.X;
            Y = point_in.Y;
            Z = point_in.Z;
        }

        // Geometric functions
        /// <summary>
        /// Return the opposite to a specified direction 
        /// </summary>
        /// <param name="gridDirection_in">GridDirection object to find the opposite for</param>
        /// <returns></returns>
        public static GridDirection GetOppositeDirection(GridDirection gridDirection_in)
        {
            switch (gridDirection_in)
            {
                case GridDirection.N:
                    return GridDirection.S;
                case GridDirection.E:
                    return GridDirection.W;
                case GridDirection.S:
                    return GridDirection.N;
                case GridDirection.W:
                    return GridDirection.E;
                case GridDirection.None:
                    return GridDirection.None;
                default:
                    return GridDirection.None;
            }
        }
        /// <summary>
        /// Return the smallest angle between two azimuths; azimuths can be given in any order but must be between 0 and 2*pi
        /// </summary>
        /// <param name="azimuth1">First azimuth - must be between 0 and 2*pi</param>
        /// <param name="azimuth2">Second azimuth - must be between 0 and 2*pi</param>
        /// <returns></returns>
        public static double getAngularDifference(double azimuth1, double azimuth2)
        {
            double directDifference = Math.Abs(azimuth1 - azimuth2);
            if (directDifference < Math.PI)
                return directDifference;
            else
                return (2 * Math.PI) - directDifference;
        }
        /// <summary>
        /// Return the smallest angle between two strikes; strikes can be given in any order but are assumed to be non-direction (i.e. can be trimmed to the range 0 to pi)
        /// </summary>
        /// <param name="strike1">First strike - will be converted to a value between 0 and pi</param>
        /// <param name="strike2">Second strike - will be converted to a value between 0 and pi</param>
        /// <returns></returns>
        public static double getStrikeDifference(double strike1, double strike2)
        {
            // Trim the input values so they lie between 0 and pi
            while (strike1 < 0)
                strike1 += Math.PI;
            while (strike1 >= Math.PI)
                strike1 -= Math.PI;
            while (strike2 < 0)
                strike2 += Math.PI;
            while (strike2 >= Math.PI)
                strike2 -= Math.PI;

            double directDifference = Math.Abs(strike1 - strike2);
            if (directDifference < (Math.PI / 2))
                return directDifference;
            else
                return Math.PI - directDifference;
        }
        /// <summary>
        /// Find the midpoint between two specified points
        /// </summary>
        /// <param name="Point1">First point</param>
        /// <param name="Point2">Second point</param>
        /// <returns>Midpoint between the two specified points, as PointXYZ object</returns>
        public static PointXYZ getMidPoint(PointXYZ Point1, PointXYZ Point2)
        {
            double midpointX = (Point1.X + Point2.X) / 2;
            double midpointY = (Point1.Y + Point2.Y) / 2;
            double midpointZ = (Point1.Z + Point2.Z) / 2;

            return new PointXYZ(midpointX, midpointY, midpointZ);
        }
        /// <summary>
        /// Check to see if two line segments cross, when projected onto a horizontal plane
        /// </summary>
        /// <param name="Line1Point1"></param>
        /// <param name="Line1Point2"></param>
        /// <param name="Line2Point1"></param>
        /// <param name="Line2Point2"></param>
        /// <returns></returns>
        public static bool checkCrossover(PointXYZ Line1Point1, PointXYZ Line1Point2, PointXYZ Line2Point1, PointXYZ Line2Point2)
        {
            // Get x, y and z coordinates of base points and vectors of both lines
            double x1 = Line1Point1.X;
            double y1 = Line1Point1.Y;
            double z1 = Line1Point1.Z;
            double dx1 = Line1Point2.X - x1;
            double dy1 = Line1Point2.Y - y1;
            double dz1 = Line1Point2.Z - z1;
            double x2 = Line2Point1.X;
            double y2 = Line2Point1.Y;
            double dx2 = Line2Point2.X - x2;
            double dy2 = Line2Point2.Y - y2;

            // Get position of crossover point relative to basepoint and vector of first line
            double p = 0;
            double denominator = ((dx2 * dy1) - (dx1 * dy2));
            if (denominator == 0) // If the denominator is zero, the points defining one or both lines are coincident, or the two lines are parallel, so do not cross
                return false;
            else
                p = ((dx2 * (y2 - y1)) - ((x2 - x1) * dy2)) / denominator;

            // The crossover point lies between the two specified points on the first line if p is between 0 and 1; if it is not we can return false
            if (p < 0)
                return false;
            else if (p > 1)
                return false;

            // Get position of crossover point relative to basepoint and vector of second line
            denominator = ((dx1 * dy2) - (dx2 * dy1));
            if (denominator == 0) // If the denominator is zero, the points defining one or both lines are coincident, or the two lines are parallel, so do not cross
                return false;
            else
                p = ((dx1 * (y1 - y2)) - ((x1 - x2) * dy1)) / denominator;

            // The crossover point now lies between the two specified points on the second line if p is between 0 and 1; if it is not we can return false
            if (p < 0)
                return false;
            else if (p > 1)
                return false;

            // Otherwise we can return true
            return true;
        }
        /// <summary>
        /// Find the lateral crossover point of two lines, projected vertically onto the first line
        /// </summary>
        /// <param name="Line1Point1">Point lying on the first line</param>
        /// <param name="Line1Point2">Point lying on the first line</param>
        /// <param name="Line2Point1">Point lying on the second line</param>
        /// <param name="Line2Point2">Point lying on the second line</param>
        /// <param name="XOType">Controls calculation: Extend will return the location of crossover wherever it occurs; Trim will only return crossover point if it lies between the two specified points on the first line, otherwise it will return the nearest of these two points; Restrict will only return crossover point if it lies between the two specified points on the first line, otherwise it will return null</param>
        /// <returns>Lateral crossover point, projected vertically onto the first line, as PointXYZ object</returns>
        public static PointXYZ getCrossoverPoint(PointXYZ Line1Point1, PointXYZ Line1Point2, PointXYZ Line2Point1, PointXYZ Line2Point2, CrossoverType XOType)
        {
            // Get x, y and z coordinates of base points and vectors of both lines
            double x1 = Line1Point1.X;
            double y1 = Line1Point1.Y;
            double z1 = Line1Point1.Z;
            double dx1 = Line1Point2.X - x1;
            double dy1 = Line1Point2.Y - y1;
            double dz1 = Line1Point2.Z - z1;
            double x2 = Line2Point1.X;
            double y2 = Line2Point1.Y;
            double dx2 = Line2Point2.X - x2;
            double dy2 = Line2Point2.Y - y2;

            // Get position of crossover point relative to basepoint and vector of first line
            double p = 0;
            double denominator = ((dx2 * dy1) - (dx1 * dy2));
            if ((float)denominator == 0f) // If the denominator is zero, the points defining one or both lines are coincident, or the two lines are parallel
                return null;
            else
                p = ((dx2 * (y2 - y1)) - ((x2 - x1) * dy2)) / denominator;

            // Get x, y and z coordinates of vertical projection of crossover point
            double x_crossover = x1 + (p * dx1);
            double y_crossover = y1 + (p * dy1);
            double z_crossover = z1 + (p * dz1);

            // Create a PointXYZ object representing the vertical projection of crossover point
            PointXYZ returnPoint = new PointXYZ(x_crossover, y_crossover, z_crossover);

            // Check whether the crossover point lies between the two specified points on the first line, and if not adjust it according to the specified crossover type
            switch (XOType)
            {
                case CrossoverType.Extend:
                    // Return the calculated point wherever it lies, so no adjustment needed
                    break;
                case CrossoverType.Trim:
                    // If the crossover point does not lie between the two specified points on the first line, adjust it to the nearest of these two points
                    if (p < 0)
                        returnPoint = new PointXYZ(Line1Point1);
                    else if (p > 1)
                        returnPoint = new PointXYZ(Line1Point2);
                    break;
                case CrossoverType.Restrict:
                    // If the crossover point does not lie between the two specified points on the first line, return null
                    if (p < 0)
                        returnPoint = null;
                    else if (p > 1)
                        returnPoint = null;
                    break;
                default:
                    break;
            }

            // Return the calculated point
            return returnPoint;
        }
        /// <summary>
        /// Find the crossover point of two lines in 3D
        /// </summary>
        /// <param name="Line1Point1">Point lying on the first line</param>
        /// <param name="Line1Point2">Point lying on the first line</param>
        /// <param name="Line2Point1">Point lying on the second line</param>
        /// <param name="Line2Point2">Point lying on the second line</param>
        /// <param name="XOType">Controls calculation: Extend will return the location of crossover wherever it occurs; Trim will only return crossover point if it lies between the two specified points on the first line, otherwise it will return the nearest of these two points; Restrict will only return crossover point if it lies between the two specified points on the first line, otherwise it will return null</param>
        /// <returns>Crossover point, or nearest point to the crossover projected along one of the axes if the lines do not cross, as PointXYZ object</returns>
        public static PointXYZ get3DCrossoverPoint(PointXYZ Line1Point1, PointXYZ Line1Point2, PointXYZ Line2Point1, PointXYZ Line2Point2, CrossoverType XOType)
        {
            // Get x, y and z coordinates of base points and vectors of both lines
            double x1 = Line1Point1.X;
            double y1 = Line1Point1.Y;
            double z1 = Line1Point1.Z;
            double dx1 = Line1Point2.X - x1;
            double dy1 = Line1Point2.Y - y1;
            double dz1 = Line1Point2.Z - z1;
            double x2 = Line2Point1.X;
            double y2 = Line2Point1.Y;
            double z2 = Line2Point1.Z;
            double dx2 = Line2Point2.X - x2;
            double dy2 = Line2Point2.Y - y2;
            double dz2 = Line2Point2.Z - z2;

            // Get position of crossover point relative to basepoint and vector of first line
            double p = 0;
            double xydenominator = ((dx2 * dy1) - (dx1 * dy2));
            double yzdenominator = ((dy2 * dz1) - (dy1 * dz2));
            double zxdenominator = ((dz2 * dx1) - (dz1 * dx2));

            if (((float)xydenominator == 0f) && ((float)yzdenominator == 0f) && ((float)zxdenominator == 0f)) // If all the denominators are zero, the points defining one or both lines are coincident, or the two lines are parallel
                return null;
            else
            {
                // Use the denominator with the highest absolute value
                if ((Math.Abs(xydenominator) > Math.Abs(yzdenominator)) && (Math.Abs(xydenominator) > Math.Abs(zxdenominator)))
                    p = ((dx2 * (y2 - y1)) - ((x2 - x1) * dy2)) / xydenominator;
                else if (Math.Abs(yzdenominator) > Math.Abs(zxdenominator))
                    p = ((dy2 * (z2 - z1)) - ((y2 - y1) * dz2)) / yzdenominator;
                else
                    p = ((dz2 * (x2 - x1)) - ((z2 - z1) * dx2)) / zxdenominator;
            }

            // Get x, y and z coordinates of vertical projection of crossover point
            double x_crossover = x1 + (p * dx1);
            double y_crossover = y1 + (p * dy1);
            double z_crossover = z1 + (p * dz1);

            // Create a PointXYZ object representing the vertical projection of crossover point
            PointXYZ returnPoint = new PointXYZ(x_crossover, y_crossover, z_crossover);

            // Check whether the crossover point lies between the two specified points on the first line, and if not adjust it according to the specified crossover type
            switch (XOType)
            {
                case CrossoverType.Extend:
                    // Return the calculated point wherever it lies, so no adjustment needed
                    break;
                case CrossoverType.Trim:
                    // If the crossover point does not lie between the two specified points on the first line, adjust it to the nearest of these two points
                    if (p < 0)
                        returnPoint = new PointXYZ(Line1Point1);
                    else if (p > 1)
                        returnPoint = new PointXYZ(Line1Point2);
                    break;
                case CrossoverType.Restrict:
                    // If the crossover point does not lie between the two specified points on the first line, return null
                    if (p < 0)
                        returnPoint = null;
                    else if (p > 1)
                        returnPoint = null;
                    break;
                default:
                    break;
            }

            // Return the calculated point
            return returnPoint;
        }
        /// <summary>
        /// Check if two points have the same coordinates
        /// </summary>
        /// <param name="Point1">First point</param>
        /// <param name="Point2">Second point</param>
        /// <returns>True if both points have identical X, Y and Z coordinates within rounding error, otherwise false</returns>
        public static bool comparePoints(PointXYZ Point1, PointXYZ Point2)
        {
            if ((Point1 is null) || (Point2 is null))
                return false;
            else if ((float)Point1.X != (float)Point2.X)
                return false;
            else if ((float)Point1.Y != (float)Point2.Y)
                return false;
            else if ((float)Point1.Z != (float)Point2.Z)
                return false;
            else
                return true;
        }

        // Vector functions
        /// <summary>
        /// Add a specified vector to the current point
        /// </summary>
        /// <param name="vector">Vector to add</param>
        public void AddVector(VectorXYZ vector)
        {
            X += vector.Component(VectorComponents.X);
            Y += vector.Component(VectorComponents.Y);
            Z += vector.Component(VectorComponents.Z);
        }
        /// <summary>
        /// Subtract a specified vector to the current point
        /// </summary>
        /// <param name="vector">Vector to subtract</param>
        public void SubtractVector(VectorXYZ vector)
        {
            X -= vector.Component(VectorComponents.X);
            Y -= vector.Component(VectorComponents.Y);
            Z -= vector.Component(VectorComponents.Z);
        }

        // Constructors
        /// <summary>
        /// Constructor to create a new point with specified coordinates
        /// </summary>
        /// <param name="X_in">X coordinate</param>
        /// <param name="Y_in">Y coordinate</param>
        /// <param name="Z_in">Z coordinate - positive upwards</param>
        public PointXYZ(double X_in, double Y_in, double Z_in)
        {
            X = X_in;
            Y = Y_in;
            Z = Z_in;
        }
        /// <summary>
        /// Constructor to create a copy of an existing XYZ point
        /// </summary>
        /// <param name="point_in">PointXYZ object to copy</param>
        public PointXYZ(PointXYZ point_in)
        {
            X = point_in.X;
            Y = point_in.Y;
            Z = point_in.Z;
        }
    }

    /// <summary>
    /// Vector specified a global 3D Cartesian reference frame (i.e. X, Y, Z)
    /// </summary>
    class VectorXYZ
    {
        // Vector components
        /// <summary>
        /// Get a component of the vector
        /// </summary>
        /// <param name="Index">Index of the vector component to return (X, Y or Z)</param>
        /// <returns>Value of the specified vector component</returns>
        public double Component(VectorComponents Index) { return components[Index]; }
        /// <summary>
        /// Set a component of the vector
        /// </summary>
        /// <param name="Index">Index of the vector component to set (X, Y or Z)</param>
        /// <param name="Value">Value to set the specified vector component to</param>
        public void Component(VectorComponents Index, double Value) { components[Index] = Value; }
        /// <summary>
        /// Add a specified amount to a component of the vector
        /// </summary>
        /// <param name="Index">Index of the vector component to set (X, Y or Z)</param>
        /// <param name="Value">Value to add to the specified vector component to</param>
        public void ComponentAdd(VectorComponents Index, double Value) { components[Index] += Value; }
        /// <summary>
        /// Dictionary containing the vector components
        /// </summary>
        private Dictionary<VectorComponents, double> components;

        // Operator overloads
        /// <summary>
        /// Overide for the GetHashCode function - works in the same way as for the base class
        /// </summary>
        /// <returns>A hash code for the current object</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Unitary plus - returns the positive of the input vector (i.e. an identical copy)
        /// </summary>
        /// <param name="input">Vector to be copied</param>
        /// <returns>Positive copy of the input vector</returns>
        public static VectorXYZ operator +(VectorXYZ input)
        {
            VectorXYZ output = new VectorXYZ(0, 0, 0);
            return output + input;
        }
        /// <summary>
        /// Unitary minus - returns the negative of the input vector (all components made negative)
        /// </summary>
        /// <param name="input">Vector to be negated</param>
        /// <returns>Negative of the input vector</returns>
        public static VectorXYZ operator -(VectorXYZ input)
        {
            VectorXYZ output = new VectorXYZ(0, 0, 0);
            return output - input;
        }
        /// <summary>
        /// Vector addition - add the components of the vector
        /// </summary>
        /// <param name="lhs">Vector to be added to</param>
        /// <param name="rhs">Vector to add</param>
        /// <returns>Sum of the two vectors</returns>
        public static VectorXYZ operator +(VectorXYZ lhs, VectorXYZ rhs)
        {
            VectorXYZ output = new VectorXYZ(lhs);
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                output.components[i] += rhs.components[i];
            return output;
        }
        /// <summary>
        /// Vector subtraction - subtract the components of the vector
        /// </summary>
        /// <param name="lhs">Vector to be subtracted from</param>
        /// <param name="rhs">Vector to subtract</param>
        /// <returns>Difference of the two vectors</returns>
        public static VectorXYZ operator -(VectorXYZ lhs, VectorXYZ rhs)
        {
            VectorXYZ output = new VectorXYZ(lhs);
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                output.components[i] -= rhs.components[i];
            return output;
        }
        /// <summary>
        /// Vector equality comparison - check if two vectors are identical
        /// </summary>
        /// <param name="lhs">Vector to be compared to</param>
        /// <param name="rhs">Vector to compare</param>
        /// <returns>True if all components of the vectors are equal, otherwise false</returns>
        public static bool operator ==(VectorXYZ lhs, VectorXYZ rhs)
        {
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                if (lhs.components[i] != rhs.components[i])
                    return false;
            return true;
        }
        /// <summary>
        /// Vector inequality comparison - check if two vectors are not identical
        /// </summary>
        /// <param name="lhs">Vector to be compared to</param>
        /// <param name="rhs">Vector to compare</param>
        /// <returns>False if all components of the vectors are equal, otherwise true</returns>
        public static bool operator !=(VectorXYZ lhs, VectorXYZ rhs)
        {
            return !(lhs == rhs);
        }
        /// <summary>
        /// Vector equality comparison - check if this vector is equal to the supplied vector
        /// </summary>
        /// <param name="obj">Object to compare to this vector</param>
        /// <returns>True if the supplied object is a vector and all its components are equal to this one, otherwise false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is VectorXYZ))
                return false;
            return this == (VectorXYZ)obj;
        }
        /// <summary>
        /// Multiply a vector by a scalar - multiply each component of the vector by the scalar
        /// </summary>
        /// <param name="lhs">Scalar to multiply by</param>
        /// <param name="rhs">Vector to be multiplied</param>
        /// <returns>Product of the scalar and the vector</returns>
        public static VectorXYZ operator *(double lhs, VectorXYZ rhs)
        {
            VectorXYZ output = new VectorXYZ(rhs);
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                output.components[i] *= lhs;
            return output;
        }
        /// <summary>
        /// Divide a vector by a scalar - divide each component of the vector by the scalar
        /// </summary>
        /// <param name="lhs">Vector to be divided</param>
        /// <param name="rhs">Scalar to divided by</param>
        /// <returns>Quotient of the scalar and the vector</returns>
        public static VectorXYZ operator /(VectorXYZ lhs, double rhs)
        {
            VectorXYZ output = new VectorXYZ(lhs);
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                output.components[i] /= rhs;
            return output;
        }
        /// <summary>
        /// Dot product - returns scalar product of two vectors
        /// </summary>
        /// <param name="lhs">Vector to multiply by</param>
        /// <param name="rhs">Vector to be multiplied</param>
        /// <returns>Scalar product of the two vectors</returns>
        public static double operator &(VectorXYZ lhs, VectorXYZ rhs)
        {
            double output = 0;
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                output+= lhs.components[i] * rhs.components[i];
            return output;
        }
        /// <summary>
        /// Vector product - returns vector product of two vectors
        /// </summary>
        /// <param name="lhs">Vector to multiply by</param>
        /// <param name="rhs">Vector to be multiplied</param>
        /// <returns>Vector product of the two vectors</returns>
        public static VectorXYZ operator *(VectorXYZ lhs, VectorXYZ rhs)
        {
            double outputx = (lhs.components[VectorComponents.Y] * rhs.components[VectorComponents.Z]) - (lhs.components[VectorComponents.Z] * rhs.components[VectorComponents.Y]);
            double outputy = (lhs.components[VectorComponents.Z] * rhs.components[VectorComponents.X]) - (lhs.components[VectorComponents.X] * rhs.components[VectorComponents.Z]);
            double outputz = (lhs.components[VectorComponents.X] * rhs.components[VectorComponents.Y]) - (lhs.components[VectorComponents.Y] * rhs.components[VectorComponents.X]);
            return new VectorXYZ(outputx, outputy, outputz);
        }
        /// <summary>
        /// Vector outer product - returns second order symmetric tensor product of two vectors
        /// </summary>
        /// <param name="lhs">Vector to multiply by</param>
        /// <param name="rhs">Vector to be multiplied</param>
        /// <returns>Second order symmetric tensor representing the outer product of the two input vectors</returns>
        public static Tensor2S operator ^(VectorXYZ lhs, VectorXYZ rhs)
        {
            // NB in general the outer product of 2 vectors will be a non-symmetric tensor (since in general lhs.i*rhs.j != lhs.j*rhs.i)
            // In this case we will only return the symmetric part of this tensor
            Tensor2S output = new Tensor2S();
            // Loop through each component of the output tensor matrix
            // NB Each of the shear components will be calculated twice, as they each appear twice in the matrix; however we will obtain the same result both times, so the only impact is on calculation speed
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                foreach (VectorComponents j in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                {
                    double outputComponent_ij = ((lhs.components[i] * rhs.components[j]) + (lhs.components[j] * rhs.components[i])) / 2;
                    output.Component(i, j, outputComponent_ij);
                }
            return output;
        }
        /// <summary>
        /// Multiply a vector by a second order tensor
        /// </summary>
        /// <param name="lhs">Tensor to multiply the vector by</param>
        /// <param name="rhs">Vector to be multiplied</param>
        /// <returns>Vector product of a tensor and a vector</returns>
        public static VectorXYZ operator *(Tensor2S lhs, VectorXYZ rhs)
        {
            VectorXYZ output = new VectorXYZ(0, 0, 0);
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                foreach (VectorComponents j in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                    output.components[i] += lhs.Component(i, j) * rhs.components[j];
            return output;
        }
        /// <summary>
        /// Divide a vector by a second order tensor (i.e. multiply the vector by the inverse of the tensor)
        /// </summary>
        /// <param name="lhs">Vector to be divided</param>
        /// <param name="rhs">Second order tensor to divide by</param>
        /// <returns>Vector representing the quotient of the input vector and tensor</returns>
        public static VectorXYZ operator /(VectorXYZ lhs, Tensor2S rhs)
        {
            return rhs.Inverse() * lhs;
        }
        /// <summary>
        /// Fourth order outer product - returns a fourth order tensor C such that Cijkl=(AiBkDjl+AiBlDjk+AjBkDil+AjBlDik)/4, where D is the Kronecker delta
        /// </summary>
        /// <param name="lhs">Vector to multiply by</param>
        /// <param name="rhs">Vector to be multiplied</param>
        /// <returns>Fourth order tensor representing the fourth order outer product of the two input vectors</returns>
        public static Tensor4_2Sx2S operator |(VectorXYZ lhs, VectorXYZ rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();

            // Loop through each component of the output tensor, calculating and populating the values
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                List<VectorComponents> ij_components = Tensor2S.GetMatrixComponentIndices(ij);
                VectorComponents i = ij_components[0];
                VectorComponents j = ij_components[1];
                double f_i = lhs.Component(i);
                double f_j = lhs.Component(j);
                foreach (Tensor2SComponents kl in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                {
                    List<VectorComponents> kl_components = Tensor2S.GetMatrixComponentIndices(kl);
                    VectorComponents k = kl_components[0];
                    VectorComponents l = kl_components[1];
                    double f_k = rhs.Component(k);
                    double f_l = rhs.Component(l);
                    double C_ijkl = 0;
                    if (i == k)
                        C_ijkl += (f_j * f_l);
                    if (i == l)
                        C_ijkl += (f_j * f_k);
                    if (j == k)
                        C_ijkl += (f_i * f_l);
                    if (j == l)
                        C_ijkl += (f_i * f_k);
                    output.Component(ij, kl, C_ijkl);
                }
            }

            // Divide all values by 4 and return the output tensor
            output /= 4;
            return output;
        }

        // Geometric properties
        /// <summary>
        /// Length of the vector
        /// </summary>
        public double Length
        {
            get
            {
                double lengthsquared = 0;
                foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                    lengthsquared += Math.Pow(components[i], 2);
                return Math.Sqrt(lengthsquared);
            }

            set
            {
                double lengthsquared = 0;
                foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                    lengthsquared += Math.Pow(components[i], 2);
                double lengthMultiplier = (lengthsquared > 0 ? value / Math.Sqrt(lengthsquared) : value);
                foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                    components[i] *= lengthMultiplier;
            }
        }
        /// <summary>
        /// Length of the vector projected vertically onto the horizontal plane
        /// </summary>
        public double HorizontalLength
        {
            get
            {
                double lenthsquared = 0;
                foreach (VectorComponents i in new VectorComponents[2] { VectorComponents.X, VectorComponents.Y })
                    lenthsquared += Math.Pow(components[i], 2);
                return Math.Sqrt(lenthsquared);
            }
        }
        /// <summary>
        /// Azimuth of the vector (radians, clockwise from N)
        /// </summary>
        public double Azimuth
        {
            get
            {
                return Math.Atan2(components[VectorComponents.X], components[VectorComponents.Y]);
            }
        }
        /// <summary>
        /// Dip of the vector from horizontal (radians, positive downwards)
        /// </summary>
        public double Dip
        {
            get
            {
                return Math.Atan(-components[VectorComponents.Z] / HorizontalLength);
            }
        }

        // Geometric functions
        /// <summary>
        /// Minimum magnitude for nonzero sine and cosine values; smaller values will be rounded to zero. 
        /// </summary>
        private static double roundToZero = 1E-10;
        /// <summary>
        /// Modified sin function that will return exactly zero for sin(pi) and multiples; this will give more accurate vector representations of lines with dip or azimuth orthogonal to the X, Y or Z axes
        /// </summary>
        /// <param name="angle">Angle to calculate sine of</param>
        /// <returns>Math.Sin(angle), except 0d for angle=Math.Pi or a multiple</returns>
        public static double Sin_trim(double angle)
        {
            double output = Math.Sin(angle);
            if (Math.Abs(output) < roundToZero)
                return 0;
            else
                return output;
        }
        /// <summary>
        /// Modified cos function that will return exactly zero for cos(pi/2) and multiples; this will give more accurate vector representations of lines with dip or azimuth orthogonal to the X, Y or Z axes
        /// </summary>
        /// <param name="angle">Angle to calculate cosine of</param>
        /// <returns>Math.Cos(angle), except 0d for angle=Math.Pi/2 or a multiple</returns>
        public static double Cos_trim(double angle)
        {
            double output = Math.Cos(angle);
            if (Math.Abs(output) < roundToZero)
                return 0;
            else
                return output;
        }
        /// <summary>
        /// Return a unit length vector with the specified orientation
        /// </summary>
        /// <param name="Azimuth">Azimuth of the vector (radians, clockwise from N)</param>
        /// <param name="Dip">Dip of the vector from horizontal (radians, positive downwards)</param>
        /// <returns>Unit length vector in the specified orientation</returns>
        public static VectorXYZ GetLineVector(double Azimuth, double Dip)
        {
            double sinazi = Sin_trim(Azimuth);
            double cosazi = Cos_trim(Azimuth);
            double sindip = Sin_trim(Dip);
            double cosdip = Cos_trim(Dip);
            return new VectorXYZ(sinazi * cosdip, cosazi * cosdip, -sindip);
        }
        /// <summary>
        /// Return a vector of the specified length with the specified orientation
        /// </summary>
        /// <param name="Azimuth">Azimuth of the vector (radians, clockwise from N)</param>
        /// <param name="Dip">Dip of the vector from horizontal (radians, positive downwards)</param>
        /// <param name="Length">Length</param>
        /// <returns>Vector of the specified length in the specified orientation</returns>
        public static VectorXYZ GetLineVector(double Azimuth, double Dip, double Length)
        {
            return Length * GetLineVector(Azimuth, Dip);
        }
        /// <summary>
        /// Return a unit length vector normal to the specified plane orientation
        /// </summary>
        /// <param name="Azimuth">Azimuth of the plane (radians, clockwise from N)</param>
        /// <param name="Dip">Dip of the plane from horizontal (radians, positive downwards)</param>
        /// <returns>Unit length vector normal to the specified plane</returns>
        public static VectorXYZ GetNormalToPlane(double Azimuth, double Dip)
        {
            double sinazi = Sin_trim(Azimuth);
            double cosazi = Cos_trim(Azimuth);
            double sindip = Sin_trim(Dip);
            double cosdip = Cos_trim(Dip);
            return new VectorXYZ(sinazi * sindip, cosazi * sindip, cosdip);
        }
        /// <summary>
        /// Return a unit length vector normal to a plane defined by three points
        /// </summary>
        /// <param name="point1">PointXYZ object representing the first point defining the plane</param>
        /// <param name="point2">PointXYZ object representing the second point defining the plane</param>
        /// <param name="point3">PointXYZ object representing the third point defining the plane</param>
        /// <returns>Unit length vector normal to the specified plane</returns>
        public static VectorXYZ GetNormalToPlane(PointXYZ point1, PointXYZ point2, PointXYZ point3)
        {
            VectorXYZ V1 = new VectorXYZ(point1, point2);
            VectorXYZ V2 = new VectorXYZ(point1, point3);
            VectorXYZ normal = V1 * V2;
            normal.Length = 1;
            return normal;
        }
        /// <summary>
        /// Return a unit length vector representing a line with the specified pitch on a plane with specified orientation
        /// </summary>
        /// <param name="Azimuth">Azimuth of the plane (radians, clockwise from N)</param>
        /// <param name="Dip">Dip of the plane from horizontal (radians, positive downwards)</param>
        /// <param name="Pitch">Pitch of the line on the plane (radians, downwards from strike direction Azimuth-Pi/2</param>
        /// <returns>Unit length vector of a line at the specified pitch on the specified plane</returns>
        public static VectorXYZ GetLineOnPlane(double Azimuth, double Dip, double Pitch)
        {
            double sinazi = Sin_trim(Azimuth);
            double cosazi = Cos_trim(Azimuth);
            double sindip = Sin_trim(Dip);
            double cosdip = Cos_trim(Dip);
            double sinpitch = Sin_trim(Pitch);
            double cospitch = Cos_trim(Pitch);
            return new VectorXYZ((sinazi * cosdip * sinpitch) - (cosazi * cospitch), (cosazi * cosdip * sinpitch) + (sinazi * cospitch), -(sindip * sinpitch));
        }
        /// <summary>
        /// Return a unit length vector parallel to this vector
        /// </summary>
        /// <returns>Unit length vector parallel to this vector</returns>
        public VectorXYZ GetNormalisedVector()
        {
            VectorXYZ output = new VectorXYZ(this);
            output.Length = 1;
            return output;
        }

        // Constructors
        /// <summary>
        /// Constructor to create a new vector with specified components
        /// </summary>
        /// <param name="X_in">X component</param>
        /// <param name="Y_in">Y component</param>
        /// <param name="Z_in">Z component - positive downwards</param>
        public VectorXYZ(double X_in, double Y_in, double Z_in)
        {
            components = new Dictionary<VectorComponents, double>();
            components.Add(VectorComponents.X, X_in);
            components.Add(VectorComponents.Y, Y_in);
            components.Add(VectorComponents.Z, Z_in);
        }
        /// <summary>
        /// Constructor to create a copy of an existing vector
        /// </summary>
        /// <param name="vector_in">VectorXYZ object to copy</param>
        public VectorXYZ(VectorXYZ vector_in)
        {
            components = new Dictionary<VectorComponents, double>();
            foreach (VectorComponents Index in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                components.Add(Index, vector_in.components[Index]);
        }
        /// <summary>
        /// Create a VectorXYZ object representing the vector between two specified points
        /// </summary>
        /// <param name="point1">PointXYZ object representing the point at the start of the vector</param>
        /// <param name="point2">PointXYZ object representing the point at the end of the vector</param>
        public VectorXYZ(PointXYZ point1, PointXYZ point2)
        {
            components = new Dictionary<VectorComponents, double>();
            components.Add(VectorComponents.X, point2.X - point1.X);
            components.Add(VectorComponents.Y, point2.Y - point1.Y);
            components.Add(VectorComponents.Z, point2.Z - point1.Z);
        }
    }

    /// <summary>
    /// Second order symmetric tensor with 6 components, e.g. a stress or strain tensor
    /// </summary>
    class Tensor2S
    {
        // Tensor components
        /// <summary>
        /// Get a component of the tensor using vector-type indexing
        /// </summary>
        /// <param name="Index">Index of the tensor component to return in vector-type indexing (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <returns>Value of the specified tensor component</returns>
        public double Component(Tensor2SComponents Index) { return components[Index]; }
        /// <summary>
        /// Get a component of the tensor using matrix-type indexing
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (X, Y or Z)</param>
        /// <returns>Value of the specified tensor component</returns>
        public double Component(VectorComponents RowIndex, VectorComponents ColumnIndex) { return components[Tensor2S.GetTensorComponentIndex(RowIndex, ColumnIndex)]; }
        /// <summary>
        /// Set a component of the vector using vector-type indexing
        /// </summary>
        /// <param name="Index">Index of the tensor component to set in vector-type indexing (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="Value">Value to set the specified tensor component to</param>
        public void Component(Tensor2SComponents Index, double Value) { components[Index] = Value; }
        /// <summary>
        /// Set a component of the tensor using matrix-type indexing
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="Value">Value to set the specified tensor component to</param>
        public void Component(VectorComponents RowIndex, VectorComponents ColumnIndex, double Value) { components[Tensor2S.GetTensorComponentIndex(RowIndex, ColumnIndex)] = Value; }
        /// <summary>
        /// Add a specified amount to a component of the vector using vector-type indexing
        /// </summary>
        /// <param name="Index">Index of the tensor component to set in vector-type indexing (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="Value">Value to add to the specified tensor component to</param>
        public void ComponentAdd(Tensor2SComponents Index, double Value) { components[Index] += Value; }
        /// <summary>
        /// Add a specified amount to a component of the tensor using matrix-type indexing
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="Value">Value to add to the specified tensor component to</param>
        public void ComponentAdd(VectorComponents RowIndex, VectorComponents ColumnIndex, double Value) { components[Tensor2S.GetTensorComponentIndex(RowIndex, ColumnIndex)] += Value; }
        /// <summary>
        /// Dictionary containing the tensor components
        /// </summary>
        private Dictionary<Tensor2SComponents, double> components;

        // General matrix index functions
        /// <summary>
        /// Return the vector-type index of a tensor component (XX, YY, ZZ, XY, YZ or ZX) for specified matrix-type indices
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (X, Y or Z)</param>
        /// <returns>Vector-type index of a tensor component (XX, YY, ZZ, XY, YZ or ZX)</returns>
        public static Tensor2SComponents GetTensorComponentIndex(VectorComponents RowIndex, VectorComponents ColumnIndex)
        {
            switch (RowIndex)
            {
                case VectorComponents.X:
                    switch (ColumnIndex)
                    {
                        case VectorComponents.X:
                            return Tensor2SComponents.XX;
                        case VectorComponents.Y:
                            return Tensor2SComponents.XY;
                        case VectorComponents.Z:
                            return Tensor2SComponents.ZX;
                        default:
                            return Tensor2SComponents.XX;
                    }
                case VectorComponents.Y:
                    switch (ColumnIndex)
                    {
                        case VectorComponents.X:
                            return Tensor2SComponents.XY;
                        case VectorComponents.Y:
                            return Tensor2SComponents.YY;
                        case VectorComponents.Z:
                            return Tensor2SComponents.YZ;
                        default:
                            return Tensor2SComponents.YY;
                    }
                case VectorComponents.Z:
                    switch (ColumnIndex)
                    {
                        case VectorComponents.X:
                            return Tensor2SComponents.ZX;
                        case VectorComponents.Y:
                            return Tensor2SComponents.YZ;
                        case VectorComponents.Z:
                            return Tensor2SComponents.ZZ;
                        default:
                            return Tensor2SComponents.ZZ;
                    }
                default:
                    return Tensor2SComponents.ZZ;
            }
        }
        /// <summary>
        /// Get the two matrix-type indices corresponding to a specified vector-type tensor component index (XX, YY, ZZ, XY, YZ or ZX)
        /// </summary>
        /// <param name="TensorIndex">Vector-type index of a tensor component (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <returns>List containing the two tensor matrix row and column indices (X, Y or Z)</returns>
        public static List<VectorComponents> GetMatrixComponentIndices(Tensor2SComponents TensorIndex)
        {
            List<VectorComponents> indices = new List<VectorComponents>();
            switch (TensorIndex)
            {
                case Tensor2SComponents.XX:
                    indices.Add(VectorComponents.X);
                    indices.Add(VectorComponents.X);
                    break;
                case Tensor2SComponents.YY:
                    indices.Add(VectorComponents.Y);
                    indices.Add(VectorComponents.Y);
                    break;
                case Tensor2SComponents.ZZ:
                    indices.Add(VectorComponents.Z);
                    indices.Add(VectorComponents.Z);
                    break;
                case Tensor2SComponents.XY:
                    indices.Add(VectorComponents.X);
                    indices.Add(VectorComponents.Y);
                    break;
                case Tensor2SComponents.YZ:
                    indices.Add(VectorComponents.Y);
                    indices.Add(VectorComponents.Z);
                    break;
                case Tensor2SComponents.ZX:
                    indices.Add(VectorComponents.Z);
                    indices.Add(VectorComponents.X);
                    break;
                default:
                    break;
            }
            return indices;
        }

        /// <summary>
        /// Kronecker delta function: return 1 if i=j; otherwise return 0
        /// </summary>
        /// <param name="i">Vector index (X, Y or Z)</param>
        /// <param name="j">Vector index (X, Y or Z)</param>
        /// <returns>1 if i=j, otherwise 0</returns>
        public static double delta(VectorComponents i, VectorComponents j)
        {
            if (i == j)
                return 1;
            else
                return 0;
        }
        /// <summary>
        /// Epsilon function: return 1 if i,j, and k are an even permutation of X, Y and Z; return -1 if i,j, and k are an odd permutation of X, Y and Z; otherwise return 0
        /// </summary>
        /// <param name="i">Vector index (X, Y or Z)</param>
        /// <param name="j">Vector index (X, Y or Z)</param>
        /// <param name="k">Vector index (X, Y or Z)</param>
        /// <returns>1 if i,j, and k are an even combination of X, Y and Z, -1 if i,j, and k are an odd combination of X, Y and Z, otherwise 0</returns>
        public static double epsilon(VectorComponents i, VectorComponents j, VectorComponents k)
        {
            // Convert the supplied indices i, j and k into a list of integers
            List<int> integerList = new List<int>();
            integerList.Add((int)i);
            integerList.Add((int)j);
            integerList.Add((int)k);

            // Call the Tensor4_2Sx2S.epsilon(List<int>) function to determine the polarity of the list
            return Tensor4_2Sx2S.epsilon(integerList);

            // Direct calculation method; not used
            /*if ((i == j) || (j == k) || (k == i))
                return 0;
            else if (((i == VectorComponents.X) && (j == VectorComponents.Y)) || ((j == VectorComponents.X) && (k == VectorComponents.Y)) || ((k == VectorComponents.X) && (i == VectorComponents.Y)))
                return 1;
            else
                return -1;*/
        }

        // Operator overloads
        /// <summary>
        /// Overide for the GetHashCode function - works in the same way as for the base class
        /// </summary>
        /// <returns>A hash code for the current object</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Unitary plus - returns the positive of the input second order tensor (i.e. an identical copy)
        /// </summary>
        /// <param name="input">Tensor to be copied</param>
        /// <returns>Positive copy of the input tensor</returns>
        public static Tensor2S operator +(Tensor2S input)
        {
            Tensor2S output = new Tensor2S();
            return output + input;
        }
        /// <summary>
        /// Unitary minus - returns the negative of the input second order tensor (all components made negative)
        /// </summary>
        /// <param name="input">Tensor to be negated</param>
        /// <returns>Negative of the input tensor</returns>
        public static Tensor2S operator -(Tensor2S input)
        {
            Tensor2S output = new Tensor2S();
            return output - input;
        }
        /// <summary>
        /// Second order tensor addition - add the components of the tensors
        /// </summary>
        /// <param name="lhs">Tensor to be added to</param>
        /// <param name="rhs">Tensor to add</param>
        /// <returns>Sum of the two tensors</returns>
        public static Tensor2S operator +(Tensor2S lhs, Tensor2S rhs)
        {
            Tensor2S output = new Tensor2S(lhs);
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                output.components[ij] += rhs.components[ij];
            return output;
        }
        /// <summary>
        /// Second order tensor subtraction - subtract the components of the tensors
        /// </summary>
        /// <param name="lhs">Tensor to be subtracted from</param>
        /// <param name="rhs">Tensor to subtract</param>
        /// <returns>Difference of the two tensors</returns>
        public static Tensor2S operator -(Tensor2S lhs, Tensor2S rhs)
        {
            Tensor2S output = new Tensor2S(lhs);
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                output.components[ij] -= rhs.components[ij];
            return output;
        }
        /// <summary>
        /// Second order tensor equality comparison - check if two tensors are identical
        /// </summary>
        /// <param name="lhs">Tensor to be compared to</param>
        /// <param name="rhs">Tensor to compare</param>
        /// <returns>True if all components of the tensors are equal, otherwise false</returns>
        public static bool operator ==(Tensor2S lhs, Tensor2S rhs)
        {
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                if (lhs.components[ij] != rhs.components[ij])
                    return false;
            return true;
        }
        /// <summary>
        /// Second order tensor inequality comparison - check if two tensors are not identical
        /// </summary>
        /// <param name="lhs">Tensor to be compared to</param>
        /// <param name="rhs">Tensor to compare</param>
        /// <returns>False if all components of the tensors are equal, otherwise true</returns>
        public static bool operator !=(Tensor2S lhs, Tensor2S rhs)
        {
            return !(lhs == rhs);
        }
        /// <summary>
        /// Second order tensor equality comparison - check if this tensor is equal to the supplied tensor
        /// </summary>
        /// <param name="obj">Object to compare to this tensor</param>
        /// <returns>True if the supplied object is a tensor and all its components are equal to this one, otherwise false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Tensor2S))
                return false;
            return this == (Tensor2S)obj;
        }
        /// <summary>
        /// Multiply a second order tensor by a scalar - multiply each component of the tensor by the scalar
        /// </summary>
        /// <param name="lhs">Scalar to multiply by</param>
        /// <param name="rhs">Tensor to be multiplied</param>
        /// <returns>Product of the scalar and the tensor</returns>
        public static Tensor2S operator *(double lhs, Tensor2S rhs)
        {
            Tensor2S output = new Tensor2S(rhs);
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                output.components[ij] *= lhs;
            return output;
        }
        /// <summary>
        /// Divide a second order tensor by a scalar - divide each component of the tensor by the scalar
        /// </summary>
        /// <param name="lhs">Tensor to be divided</param>
        /// <param name="rhs">Scalar to divided by</param>
        /// <returns>Quotient of the scalar and the tensor</returns>
        public static Tensor2S operator /(Tensor2S lhs, double rhs)
        {
            Tensor2S output = new Tensor2S(lhs);
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                output.components[ij] /= rhs;
            return output;
        }
        // NB multiplying 2 second order symmetric will not neccessarily produce another second order symmetric tensor, and we have not defined a general second order tensor class
        // The Tensor2S multiplication operator therefore only returns the symmetric part of the resulting tensor
        /// <summary>
        /// Multiply two second order tensors
        /// </summary>
        /// <param name="lhs">Second order tensor to multiply by</param>
        /// <param name="rhs">Second order tensor to be multiplied</param>
        /// <returns>Second order symmetric tensor representing the product of the two input tensors</returns>
        public static Tensor2S operator *(Tensor2S lhs, Tensor2S rhs)
        {
            // NB in general the outer product of 2 vectors will be a non-symmetrical tensor (since in general lhs.i*rhs.j != lhs.j*rhs.i)
            // In this case we will only return the symmetric part of this tensor
            Tensor2S output = new Tensor2S();

            // NB Each of the shear components will be calculated twice, as they each appear twice in the matrix; however we will obtain the same result both times, so the only impact is on calculation speed
            foreach (VectorComponents i in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                foreach (VectorComponents j in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                {
                    double outputComponent_ij = 0;
                    foreach (VectorComponents k in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
                        outputComponent_ij += (lhs.Component(i, k) * rhs.Component(k, j)) + (lhs.Component(j, k) * rhs.Component(k, i)) / 2;
                    output.Component(i, j, outputComponent_ij);
                }
            return output;
        }
        /// <summary>
        /// Divide a second order tensor by another second order tensor (i.e. multiply the dividend tensor by the inverse of the divisor tensor)
        /// </summary>
        /// <param name="lhs">Second order tensor to be divided</param>
        /// <param name="rhs">Second order tensor to divide by</param>
        /// <returns>Second order tensor representing the quotient of the two input tensors</returns>
        public static Tensor2S operator /(Tensor2S lhs, Tensor2S rhs)
        {
            return rhs.Inverse() * lhs;
        }
        /// <summary>
        /// Outer product of two second order symmetric tensors - returns a fourth order tensor
        /// </summary>
        /// <param name="lhs">Second order tensor to multiply by</param>
        /// <param name="rhs">Second order tensor to be multiplied</param>
        /// <returns>Fourth order tensor representing the outer product of the two input tensors</returns>
        public static Tensor4_2Sx2S operator ^(Tensor2S lhs, Tensor2S rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();
            // Loop through each component of the output tensor matrix
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents kl in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                {
                    double outputComponent_ijkl = (lhs.components[ij] * rhs.components[kl]);
                    output.Component(ij, kl, outputComponent_ijkl);
                }
            return output;
        }
        /// <summary>
        /// Multiply a second order tensor by a fourth order tensor
        /// </summary>
        /// <param name="lhs">Fourth order tensor to multiply by</param>
        /// <param name="rhs">Second order tensor to be multiplied</param>
        /// <returns>Second order tensor representing the product of the two input tensors</returns>
        public static Tensor2S operator *(Tensor4_2Sx2S lhs, Tensor2S rhs)
        {
            Tensor2S output = new Tensor2S();
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                double outputComponent_i = 0;
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    outputComponent_i += lhs.Component(i, j) * rhs.components[j];
                output.components[i] = outputComponent_i;
            }
            return output;
        }
        /// <summary>
        /// Divide a second order tensor by a fourth order tensor (i.e. multiply the second order tensor by the inverse of the fourth order tensor)
        /// </summary>
        /// <param name="lhs">Second order tensor to be divided</param>
        /// <param name="rhs">Fourth order tensor to divide by</param>
        /// <returns>Second order tensor representing the quotient of the two input tensors</returns>
        public static Tensor2S operator /(Tensor2S lhs, Tensor4_2Sx2S rhs)
        {
            return rhs.Inverse() * lhs;
        }

        // Geometric properties
        /// <summary>
        /// Trace (first invariant) of the tensor matrix
        /// </summary>
        public double Trace
        {
            get
            {
                double xx = components[Tensor2SComponents.XX];
                double yy = components[Tensor2SComponents.YY];
                double zz = components[Tensor2SComponents.ZZ];
                return xx + yy + zz;
            }
        }
        /// <summary>
        /// Second invariant of the tensor matrix
        /// </summary>
        public double SecondInvariant
        {
            get
            {
                double xx = components[Tensor2SComponents.XX];
                double yy = components[Tensor2SComponents.YY];
                double zz = components[Tensor2SComponents.ZZ];
                double xy = components[Tensor2SComponents.XY];
                double yz = components[Tensor2SComponents.YZ];
                double zx = components[Tensor2SComponents.ZX];
                return (xx * yy) + (yy * zz) + (zz * xx) - Math.Pow(xy, 2) - Math.Pow(yz, 2) - Math.Pow(zx, 2);
            }
        }
        /// <summary>
        /// Determinant (third invariant) of the tensor matrix
        /// </summary>
        public double Determinant
        {
            get
            {
                double xx = components[Tensor2SComponents.XX];
                double yy = components[Tensor2SComponents.YY];
                double zz = components[Tensor2SComponents.ZZ];
                double xy = components[Tensor2SComponents.XY];
                double yz = components[Tensor2SComponents.YZ];
                double zx = components[Tensor2SComponents.ZX];
                return (xx * yy * zz) + (2 * xy * yz * zx) - (xx * Math.Pow(yz, 2)) - (yy * Math.Pow(zx, 2)) - (zz * Math.Pow(xy, 2));
            }
        }
        /// <summary>
        /// Cofactor to a specified component of the tensor
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (X, Y or Z)</param>
        /// <returns>Cofactor to the specified tensor component</returns>
        public double Cofactor(VectorComponents RowIndex, VectorComponents ColumnIndex)
        {
            // The cofactor is the determinant of a 2x2 matrix including all the components of the tensor matrix except those in the specified row and column, multiplied by the polarity of the specified row and column indices
            // We can calculate this using the formula |A|=epsilon(i,j)*ai1*aj2, summing across i,j, i.e. through all valid permutations of indices

            // First generate lists of all the matrix components except the specified row and column
            List<VectorComponents> rowComponents = new List<VectorComponents>();
            List<VectorComponents> colComponents = new List<VectorComponents>();
            foreach (VectorComponents component in Enum.GetValues(typeof(VectorComponents)).Cast<VectorComponents>())
            {
                if (component != RowIndex)
                    rowComponents.Add(component);
                if (component != ColumnIndex)
                    colComponents.Add(component);
            }

            // Determine the polarity of the specified row and column indices 
            double polarity = ((((int)RowIndex - (int)ColumnIndex)) % 2 == 0 ? 1 : -1);

            // Calculate the determinant of the 2x2 matrix left when the specified row and column are removed
            double submatrixDet = (Component(rowComponents[0], colComponents[0]) * Component(rowComponents[1], colComponents[1]))
                - (Component(rowComponents[0], colComponents[1]) * Component(rowComponents[1], colComponents[0]));

            // Return the final value of the determinant
            return polarity * submatrixDet;
        }
        /// <summary>
        /// Get the inverse to this tensor 
        /// </summary>
        /// <returns>Inverse tensor to this tensor; null if there is no inverse</returns>
        public Tensor2S Inverse()
        {
            double det = Determinant;
            if (det == 0)
                return null;
            double xx = Cofactor(VectorComponents.X, VectorComponents.X) / det;
            double yy = Cofactor(VectorComponents.Y, VectorComponents.Y) / det;
            double zz = Cofactor(VectorComponents.Z, VectorComponents.Z) / det;
            double xy = Cofactor(VectorComponents.Y, VectorComponents.X) / det;
            double yz = Cofactor(VectorComponents.Z, VectorComponents.Y) / det;
            double zx = Cofactor(VectorComponents.X, VectorComponents.Z) / det;
            return new Tensor2S(xx, yy, zz, xy, yz, zx);
        }
        /// <summary>
        /// Get a list of eigenvalues for the tensor, in ascending order
        /// </summary>
        /// <returns>List of eigenvalues for the tensor, in ascending order</returns>
        public List<double> GetEigenvalues()
        {
            // Create a list to store the eigenvalues in
            List<double> eigenvalues = new List<double>();

            // Get invariants
            double I1 = Trace;
            double I2 = SecondInvariant;
            double I3 = Determinant;

            // Solve the cubic equation giving the eigenvalues in terms of the invariants (E^3-I1xE^2+I2xE-I3=0)
            // Using Cardano method
            double alpha = I1 / 3;
            double c = I2 - (2 * I1 * alpha) + (3 * Math.Pow(alpha, 2));
            double d = -I3 + (I2 * alpha) - (I1 * Math.Pow(alpha, 2)) + Math.Pow(alpha, 3);
            double gamma = Math.Sqrt(-(4 / 3) * c);
            double theta1 = (Math.Acos(-(4 * d) / Math.Pow(gamma, 3))) / 3;
            double theta2 = (Math.Acos(-(4 * d) / Math.Pow(gamma, 3)) + (2 * Math.PI)) / 3;
            double theta3 = (Math.Acos(-(4 * d) / Math.Pow(gamma, 3)) - (2 * Math.PI)) / 3;
            double z1 = gamma * Math.Cos(theta1);
            double z2 = gamma * Math.Cos(theta2);
            double z3 = gamma * Math.Cos(theta3);
            eigenvalues.Add(z1 + alpha);
            eigenvalues.Add(z2 + alpha);
            eigenvalues.Add(z3 + alpha);

            // Sort the eigenvalue list in ascending order (i.e. minimum value first) and return
            eigenvalues.Sort();
            return eigenvalues;
        }
        /// <summary>
        /// Get a list of eigenvectors for the tensor, in ascending order.
        /// NB if eigenvalues are also required, use the GetEigenvectors(out eigenvalues) overload to save calculating the eigenvalues twice
        /// </summary>
        /// <returns>List of normalised (unit length) eigenvectors for the tensor, in ascending order of eigenvalues</returns>
        public List<VectorXYZ> GetEigenvectors()
        {
            List<double> eigenvalues;
            return GetEigenvectors(out eigenvalues);
        }
        /// <summary>
        /// Get a list of eigenvalues and eigenvectors for the tensor, in ascending order
        /// </summary>
        /// <param name="eigenvalues">Reference parameter for a list of eigenvalues, in ascending order</param>
        /// <returns>List of normalised (unit length) eigenvectors for the tensor, in ascending order of eigenvalues</returns>
        public List<VectorXYZ> GetEigenvectors(out List<double> eigenvalues)
        {
            // Create a list to store the eigenvectors in
            List<VectorXYZ> eigenvectors = new List<VectorXYZ>();

            // Get a list of the eigenvalues
            eigenvalues = GetEigenvalues();

            // Loop through each eigenvalue and calculate the associated eigenvector
            foreach(double eigenvalue in eigenvalues)
            {
                // First we need to determine the optimal permutation of indices to use to calculate the eigenvector for this eigenvalue
                // This is especially important if the eigenvector is close to one of the X, Y and Z axes
                // The optimal permutation is to calculate the indices in ascending order of the absolute values of the three diagonal components in the characteristic equation

                // Calculate the values of the three diagonal components in the characteristic equation
                Dictionary<VectorComponents, double> diagonals = new Dictionary<VectorComponents, double>();
                diagonals[VectorComponents.X] = components[Tensor2SComponents.XX] - eigenvalue;
                diagonals[VectorComponents.Y] = components[Tensor2SComponents.YY] - eigenvalue;
                diagonals[VectorComponents.Z] = components[Tensor2SComponents.ZZ] - eigenvalue;

                // Create three indices i, j and k and assign them in ascending order of the absolute values of the three diagonal components in the characteristic equation
                VectorComponents i, j, k;
                if ((Math.Abs(diagonals[VectorComponents.Z]) < Math.Abs(diagonals[VectorComponents.X])) && (Math.Abs(diagonals[VectorComponents.Z]) < Math.Abs(diagonals[VectorComponents.Y])))
                {
                    i = VectorComponents.X;
                    j = VectorComponents.Y;
                    k = VectorComponents.Z;
                }
                else if ((Math.Abs(diagonals[VectorComponents.Y]) < Math.Abs(diagonals[VectorComponents.X])) && (Math.Abs(diagonals[VectorComponents.Y]) < Math.Abs(diagonals[VectorComponents.Z])))
                {
                    i = VectorComponents.Z;
                    j = VectorComponents.X;
                    k = VectorComponents.Y;
                }
                else
                {
                    i = VectorComponents.Y;
                    j = VectorComponents.Z;
                    k = VectorComponents.X;
                }

                // Calculate the three components of the eigenvector
                double ei = (Component(i, j) * Component(j, k)) - (Component(i, k) * diagonals[j]);
                double ej = (Component(i, k) * Component(i, j)) - (Component(j, k) * diagonals[i]);
                double ek = (diagonals[i] * diagonals[j]) - Math.Pow(Component(i, j), 2);

                // Combine them to form a vector
                VectorXYZ eigenvector = new VectorXYZ(0, 0, 0);
                eigenvector.Component(i, ei);
                eigenvector.Component(j, ej);
                eigenvector.Component(k, ek);

                // Set the length of the vector to 1 and add it to the list
                eigenvector.Length = 1;
                eigenvectors.Add(eigenvector);
            }

            // Return the list of eigenvectors
            return eigenvectors;
        }
        /// <summary>
        /// Get the azimuth of the minimum horizontal value of the tensor
        /// </summary>
        /// <returns>Azimuth of the minimum horizontal value of the tensor (radians, clockwise from N); NaN if the tensor is isotropic</returns>
        public double GetMinimumHorizontalAzimuth()
        {
            double numerator = 2 * components[Tensor2SComponents.XY];
            double denominator = components[Tensor2SComponents.YY] - components[Tensor2SComponents.XX];

            if ((numerator == 0) && (denominator == 0))
                return double.NaN;
            else
                return (Math.PI + Math.Atan2(numerator, denominator)) / 2;
        }
        /// <summary>
        /// Get the minimum and maximum horizontal values of the tensor (if one of the eigenvectors is vertical, these will represent the other two eigenvalues)
        /// NB if the azimuth of the minimum horizontal value is also required, use the GetMinMaxHorizontalValues(out MinimumHorizontalAzimuth) overload to save calculating this twice
        /// </summary>
        /// <returns>List containing the minimum and maximum horizontal values of the tensor, in that order</returns>
        public List<double> GetMinMaxHorizontalValues()
        {
            double minHorAzi;
            return GetMinMaxHorizontalValues(out minHorAzi);
        }
        /// <summary>
        /// Get the minimum and maximum horizontal values of the tensor (if one of the eigenvectors is vertical, these will represent the other two eigenvalues)
        /// </summary>
        /// <param name="MinimumHorizontalAzimuth">Reference parameter for the azimuth of the minimum horizontal value of the tensor</param>
        /// <returns>List containing the minimum and maximum horizontal values of the tensor, in that order</returns>
        public List<double> GetMinMaxHorizontalValues(out double MinimumHorizontalAzimuth)
        {
            // Create a list to store the output values in
            List<double> horizontalValues = new List<double>();

            // Get the minimum horizontal azimuth
            MinimumHorizontalAzimuth = GetMinimumHorizontalAzimuth();
            // If the tensor is isotropic this will return NaN; in this case take the minimum azimuth as zero
            if (double.IsNaN(MinimumHorizontalAzimuth))
                MinimumHorizontalAzimuth = 0;

            // Calculate helper variables
            double sin_double_azi = VectorXYZ.Sin_trim(2 * MinimumHorizontalAzimuth);
            double cos_double_azi = VectorXYZ.Cos_trim(2 * MinimumHorizontalAzimuth);
            double xx_plus_yy_component = (components[Tensor2SComponents.XX] + components[Tensor2SComponents.YY]) / 2;
            double xx_minus_yy_component;
            if (Math.Abs(cos_double_azi) > Math.Abs(sin_double_azi))
                xx_minus_yy_component = (components[Tensor2SComponents.XX] - components[Tensor2SComponents.YY]) / (2 * cos_double_azi);
            else
                xx_minus_yy_component = -components[Tensor2SComponents.XY] / sin_double_azi;

            // Calculate the minimum and maximum horizontal values and add them to the list
            horizontalValues.Add(xx_plus_yy_component - xx_minus_yy_component);
            horizontalValues.Add(xx_plus_yy_component + xx_minus_yy_component);

            // Return the list of values
            return horizontalValues;
        }

        // Functions to generate specific elastic tensors
        /// <summary>
        /// Create a tensor for the horizontal applied strain from minimum and maximum horizontal strain magnitudes, and azimuth of minimum horizontal strain; Z components will be zero
        /// </summary>
        /// <param name="Epsilon_hmin">Minimum horizontal strain (Pa, negative for extensional)</param>
        /// <param name="Epsilon_hmax">Maximum horizontal strain (Pa, negative for extensional)</param>
        /// <param name="Epsilon_hmin_azimuth">Azimuth of minimum horizontal strain (rad)<</param>
        /// <returns>Tensor2D object with the required components of horizontal strain (XZ, ZY and ZZ components zero)</returns>
        public static Tensor2S HorizontalStrainTensor(double Epsilon_hmin, double Epsilon_hmax, double Epsilon_hmin_azimuth)
        {
            if (double.IsNaN(Epsilon_hmin_azimuth) || double.IsNaN(Epsilon_hmax) || double.IsNaN(Epsilon_hmin_azimuth))
                return null;

            // Calculate the horizontal components of the horizontal strain tensor; vertical components will be zero as there is no applied vertical strain
            double sinazi = VectorXYZ.Sin_trim(Epsilon_hmin_azimuth);
            double cosazi = VectorXYZ.Cos_trim(Epsilon_hmin_azimuth);
            double epsilon_dashed_xx = (Epsilon_hmin * Math.Pow(sinazi, 2)) + (Epsilon_hmax * Math.Pow(cosazi, 2));
            double epsilon_dashed_yy = (Epsilon_hmin * Math.Pow(cosazi, 2)) + (Epsilon_hmax * Math.Pow(sinazi, 2));
            double epsilon_dashed_xy = (Epsilon_hmin - Epsilon_hmax) * sinazi * cosazi;

            // Trim to remove nonzero components caused by rounding error
            double sum = Math.Abs(epsilon_dashed_xx) + Math.Abs(epsilon_dashed_yy) + Math.Abs(epsilon_dashed_xy);
            if ((float)(epsilon_dashed_xx + sum) == (float)sum) epsilon_dashed_xx = 0;
            if ((float)(epsilon_dashed_yy + sum) == (float)sum) epsilon_dashed_yy = 0;
            if ((float)(epsilon_dashed_xy + sum) == (float)sum) epsilon_dashed_xy = 0;

            // Create a new tensor horizontal strain tensor and return it
            return new Tensor2S(epsilon_dashed_xx, epsilon_dashed_yy, 0, epsilon_dashed_xy, 0, 0);
        }

        // Constructors
        /// <summary>
        /// Constructor to create a new tensor with zero values for all components
        /// </summary>
        public Tensor2S() : this(0, 0, 0, 0, 0, 0)
        {
            // Set all components to zero
        }
        /// <summary>
        /// Constructor to create a new tensor with specified components
        /// </summary>
        /// <param name="XX_in">XX component</param>
        /// <param name="YY_in">YY component</param>
        /// <param name="ZZ_in">ZZ component</param>
        /// <param name="XY_in">XY component</param>
        /// <param name="YZ_in">YZ component</param>
        /// <param name="ZX_in">ZX component</param>
        public Tensor2S(double XX_in, double YY_in, double ZZ_in, double XY_in, double YZ_in, double ZX_in)
        {
            components = new Dictionary<Tensor2SComponents, double>();
            components.Add(Tensor2SComponents.XX, XX_in);
            components.Add(Tensor2SComponents.YY, YY_in);
            components.Add(Tensor2SComponents.ZZ, ZZ_in);
            components.Add(Tensor2SComponents.XY, XY_in);
            components.Add(Tensor2SComponents.YZ, YZ_in);
            components.Add(Tensor2SComponents.ZX, ZX_in);
        }
        /// <summary>
        /// Constructor to create a copy of an existing tensor
        /// </summary>
        /// <param name="tensor_in">Tensor2S object to copy</param>
        public Tensor2S(Tensor2S tensor_in)
        {
            components = new Dictionary<Tensor2SComponents, double>();
            foreach (Tensor2SComponents Index in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                components.Add(Index, tensor_in.components[Index]);
        }
    }

    /// <summary>
    /// Fourth order tensor with 36 components relating two symmetric second order tensors, e.g. a stiffness or compliance tensor
    /// </summary>
    class Tensor4_2Sx2S
    {
        // Tensor components
        /// <summary>
        /// Get a component of the tensor using matrix-type indexing
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <returns>Value of the specified tensor component</returns>
        public double Component(Tensor2SComponents RowIndex, Tensor2SComponents ColumnIndex) { return components[RowIndex][ColumnIndex]; }
        /// <summary>
        /// Get a component of the tensor using four indices
        /// </summary>
        /// <param name="RowIndex1">First index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="RowIndex2">Second index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex1">First index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="ColumnIndex2">Second index of the tensor matrix column (X, Y or Z)</param>
        /// <returns>Value of the specified tensor component</returns>
        public double Component(VectorComponents RowIndex1, VectorComponents RowIndex2, VectorComponents ColumnIndex1, VectorComponents ColumnIndex2) { return components[Tensor2S.GetTensorComponentIndex(RowIndex1, RowIndex2)][Tensor2S.GetTensorComponentIndex(ColumnIndex1, ColumnIndex2)]; }
        /// <summary>
        /// Set a component of the tensor using matrix-type indexing
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="Value">Value to set the specified tensor component to</param>
        public void Component(Tensor2SComponents RowIndex, Tensor2SComponents ColumnIndex, double Value) { components[RowIndex][ColumnIndex] = Value; }
        /// <summary>
        /// Set a component of the tensor using four indices
        /// </summary>
        /// <param name="RowIndex1">First index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="RowIndex2">Second index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex1">First index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="ColumnIndex2">Second index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="Value">Value to set the specified tensor component to</param>
        public void Component(VectorComponents RowIndex1, VectorComponents RowIndex2, VectorComponents ColumnIndex1, VectorComponents ColumnIndex2, double Value) { components[Tensor2S.GetTensorComponentIndex(RowIndex1, RowIndex2)][Tensor2S.GetTensorComponentIndex(ColumnIndex1, ColumnIndex2)] = Value; }
        /// <summary>
        /// Add a specified amount to a component of the tensor using matrix-type indexing
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="Value">Value to add to the specified tensor component to</param>
        public void ComponentAdd(Tensor2SComponents RowIndex, Tensor2SComponents ColumnIndex, double Value) { components[RowIndex][ColumnIndex] += Value; }
        /// <summary>
        /// Add a specified amount to a component of the tensor using four indices
        /// </summary>
        /// <param name="RowIndex1">First index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="RowIndex2">Second index of the tensor matrix row (X, Y or Z)</param>
        /// <param name="ColumnIndex1">First index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="ColumnIndex2">Second index of the tensor matrix column (X, Y or Z)</param>
        /// <param name="Value">Value to add to the specified tensor component to</param>
        public void ComponentAdd(VectorComponents RowIndex1, VectorComponents RowIndex2, VectorComponents ColumnIndex1, VectorComponents ColumnIndex2, double Value) { components[Tensor2S.GetTensorComponentIndex(RowIndex1, RowIndex2)][Tensor2S.GetTensorComponentIndex(ColumnIndex1, ColumnIndex2)] += Value; }
        /// <summary>
        /// Dictionary array containing the tensor components
        /// </summary>
        private Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, double>> components;

        // General matrix index functions
        /// <summary>
        /// Kronecker delta function: return 1 if i=j; otherwise return 0
        /// </summary>
        /// <param name="i">Second order symmetrical tensor index (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="j">Second order symmetrical tensor index (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <returns>1 if i=j, otherwise 0</returns>
        public static double delta(Tensor2SComponents i, Tensor2SComponents j)
        {
            if (i == j)
                return 1;
            else
                return 0;
        }
        /// <summary>
        /// Epsilon function: return 1 if the supplied indices are an even permutation of XX, YY, ZZ, XY, YZ or ZX; return -1 if the supplied indices are an odd permutation of XX, YY, ZZ, XY, YZ or ZX; otherwise return 0
        /// </summary>
        /// <param name="indexList">List of integers representing the permutation to test</param>
        /// <returns>1 if the integers are an even permutation; -1 if the integers are an odd permutation; otherwise 0</returns>
        public static double epsilon(List<Tensor2SComponents> indexList)
        {
            // Convert the list of tensor indices into a list of integers
            List<int> integerList = new List<int>();
            foreach (Tensor2SComponents item in indexList)
                integerList.Add((int)item);

            // Call the epsilon(List<int>) function to determine the polarity of the list
            return epsilon(integerList);
        }
        /// <summary>
        /// Epsilon function for a list of integers: return 1 if the integers are an even permutation; return -1 if the integers are an odd permutation; otherwise return 0
        /// </summary>
        /// <param name="integerList">List of integers representing the permutation to test</param>
        /// <returns>1 if the integers are an even permutation; -1 if the integers are an odd permutation; otherwise 0</returns>
        public static double epsilon(List<int> integerList)
        {
            // Determine the parity of a list of integers by counting the number of inversions (parity of the list is the same as the parity of the number of inversions)
            // At the same time we can  determine whether the list is a valid permuation at all - if any two components are equal it will not be a valid permutation
            // Thus we can loop through each pair of items on the list, determining whether:
            //  A) they have equal values - in which case it is not a valid permutation and we can return 1
            //  B) they are inverted (the integer that appears last in the list (i.e. the integer with the highest index) has a lower value than the integer that appears first on the list (i.e. the integer with the lower index)
            // This method has several advantages:
            //  - We can check for validity and parity at the same time
            //  - It will work whatever the actual values of the integers in the original (natural) sequence, as long as they are in ascending order
            //  - It will work for any number of items in the list - i.e. it can be used to determine the parity of sub-lists or partial lists derived from a larger list of components
            int NoItems = integerList.Count;
            int NoInversions = 0;
            for (int i = 0; i < NoItems; i++)
                for (int j = i + 1; j < NoItems; j++)
                {
                    // Check if the two items are equal; if so, the list is not a valid permutation and we can return zero immediately
                    if (integerList[i] == integerList[j])
                        return 0;
                    // Otherwise, if the first item (i) is greater than the second item (j), they are inverted, so increment the inversion counter
                    else if (integerList[i] > integerList[j])
                        NoInversions++;
                }

            // The list is a valid permutation, so return +1 if the number of inversions is even and -1 if it is odd
            if (NoInversions % 2 == 0)
                return 1;
            else
                return -1;
        }

        // Operator overloads
        /// <summary>
        /// Overide for the GetHashCode function - works in the same way as for the base class
        /// </summary>
        /// <returns>A hash code for the current object</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Unitary plus - returns the positive of the input fourth order tensor (i.e. an identical copy)
        /// </summary>
        /// <param name="input">Tensor to be copied</param>
        /// <returns>Positive copy of the input tensor</returns>
        public static Tensor4_2Sx2S operator +(Tensor4_2Sx2S input)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();
            return output + input;
        }
        /// <summary>
        /// Unitary minus - returns the negative of the input fourth order tensor (all components made negative)
        /// </summary>
        /// <param name="input">Tensor to be negated</param>
        /// <returns>Negative of the input tensor</returns>
        public static Tensor4_2Sx2S operator -(Tensor4_2Sx2S input)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();
            return output - input;
        }
        /// <summary>
        /// Fourth order tensor addition - add the components of the tensors
        /// </summary>
        /// <param name="lhs">Tensor to be added to</param>
        /// <param name="rhs">Tensor to add</param>
        /// <returns>Sum of the two tensors</returns>
        public static Tensor4_2Sx2S operator +(Tensor4_2Sx2S lhs, Tensor4_2Sx2S rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S(lhs);
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    output.components[i][j] += rhs.components[i][j];
            return output;
        }
        /// <summary>
        /// Fourth order tensor subtraction - subtract the components of the tensors
        /// </summary>
        /// <param name="lhs">Tensor to be subtracted from</param>
        /// <param name="rhs">Tensor to subtract</param>
        /// <returns>Difference of the two tensors</returns>
        public static Tensor4_2Sx2S operator -(Tensor4_2Sx2S lhs, Tensor4_2Sx2S rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S(lhs);
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    output.components[i][j] -= rhs.components[i][j];
            return output;
        }
        /// <summary>
        /// Fourth order tensor equality comparison - check if two tensors are identical
        /// </summary>
        /// <param name="lhs">Tensor to be compared to</param>
        /// <param name="rhs">Tensor to compare</param>
        /// <returns>True if all components of the tensors are equal, otherwise false</returns>
        public static bool operator ==(Tensor4_2Sx2S lhs, Tensor4_2Sx2S rhs)
        {
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    if (lhs.components[i][j] != rhs.components[i][j])
                        return false;
            return true;
        }
        /// <summary>
        /// Fourth order tensor inequality comparison - check if two tensors are not identical
        /// </summary>
        /// <param name="lhs">Tensor to be compared to</param>
        /// <param name="rhs">Tensor compare</param>
        /// <returns>False if all components of the tensors are equal, otherwise true</returns>
        public static bool operator !=(Tensor4_2Sx2S lhs, Tensor4_2Sx2S rhs)
        {
            return !(lhs == rhs);
        }
        /// <summary>
        /// Fourth order tensor equality comparison - check if this tensor is equal to the supplied tensor
        /// </summary>
        /// <param name="obj">Object to compare to this tensor</param>
        /// <returns>True if the supplied object is a tensor and all its components are equal to this one, otherwise false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Tensor4_2Sx2S))
                return false;
            return this == (Tensor4_2Sx2S)obj;
        }
        /// <summary>
        /// Multiply a fourth order tensor by a scalar - multiply each component of the tensor by the scalar
        /// </summary>
        /// <param name="lhs">Scalar to multiply by</param>
        /// <param name="rhs">Tensor to be multiplied</param>
        /// <returns>Product of the scalar and the tensor</returns>
        public static Tensor4_2Sx2S operator *(double lhs, Tensor4_2Sx2S rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S(rhs);
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    output.components[i][j] *= lhs;
            return output;
        }
        /// <summary>
        /// Divide a fourth order tensor by a scalar - divide each component of the tensor by the scalar
        /// </summary>
        /// <param name="lhs">Tensor to be divided</param>
        /// <param name="rhs">Scalar to divided by</param>
        /// <returns>Quotient of the scalar and the tensor</returns>
        public static Tensor4_2Sx2S operator /(Tensor4_2Sx2S lhs, double rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S(lhs);
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    output.components[i][j] /= rhs;
            return output;
        }
        /// <summary>
        /// Multiply two fourth order tensors
        /// </summary>
        /// <param name="lhs">Tensor to multiply by</param>
        /// <param name="rhs">Tensor to be multiplied</param>
        /// <returns>Fourth order tensor representing the product of the two input tensors</returns>
        public static Tensor4_2Sx2S operator *(Tensor4_2Sx2S lhs, Tensor4_2Sx2S rhs)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                {
                    double component_ij = 0;
                    foreach (Tensor2SComponents k in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                        component_ij += lhs.components[i][k] * rhs.components[k][j];
                    output.components[i][j] = component_ij;
                }
            return output;
        }
        /// <summary>
        /// Divide a fourth order tensor by another fourth order tensor (i.e. multiply the dividend tensor by the inverse of the divisor tensor)
        /// </summary>
        /// <param name="lhs">Fourth order tensor to be divided</param>
        /// <param name="rhs">Fourth order tensor to divide by</param>
        /// <returns>Fourth order tensor representing the quotient of the two input tensors</returns>
        public static Tensor4_2Sx2S operator /(Tensor4_2Sx2S lhs, Tensor4_2Sx2S rhs)
        {
            return rhs.Inverse() * lhs;
        }

        // Geometric properties
        /// <summary>
        /// Trace (first invariant) of the tensor matrix
        /// </summary>
        public double Trace
        {
            get
            {
                double output = 0;
                foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    output += components[i][i];
                return output;
            }
        }
        /// <summary>
        /// Determinant (third invariant) of the tensor matrix
        /// </summary>
        public double Determinant
        {
            get
            {
                // Use the recursive method implemented in the SubmatrixDeterminant function, but defining a submatrix that contains all the components of the tensor matrix
                List<Tensor2SComponents> rowComponents = new List<Tensor2SComponents>(Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>());
                List<Tensor2SComponents> colComponents = new List<Tensor2SComponents>(Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>());
                return SubmatrixDeterminant(rowComponents, colComponents);
            }
        }
        /// <summary>
        /// Cofactor to a specified component of the tensor
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <returns>Cofactor to the specified tensor component</returns>
        public double Cofactor(Tensor2SComponents RowIndex, Tensor2SComponents ColumnIndex)
        {
            // The cofactor is the determinant of a 5x5 matrix including all the components of the tensor matrix except those in the specified row and column, multiplied by the polarity of the specified row and column indices
            // We can calculate the determinant of the matrix using the SubmatrixDeterminant function

            // First we will calculate the polarity of the specified row and column indices 
            double polarity = ((((int)RowIndex - (int)ColumnIndex)) % 2 == 0 ? 1 : -1);

            // Then we can generate lists of all the Tensor2S components except for the specified row and column
            List<Tensor2SComponents> submatrixRows = new List<Tensor2SComponents>();
            List<Tensor2SComponents> submatrixCols = new List<Tensor2SComponents>();
            foreach (Tensor2SComponents component in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                if (component != RowIndex)
                    submatrixRows.Add(component);
                if (component != ColumnIndex)
                    submatrixCols.Add(component);
            }

            // Now we can call the SubmatrixDeterminant function to calculate the determinant of the submatrix
            double submatrixDet = SubmatrixDeterminant(submatrixRows, submatrixCols);

            // Finally we can return the polarity multiplied by the determinant of the submatrix
            return polarity * submatrixDet;

        }
        /// <summary>
        /// Get the inverse to this tensor 
        /// </summary>
        /// <returns>Inverse tensor to this tensor; null if there is no inverse</returns>
        public Tensor4_2Sx2S Inverse()
        {
            // It may also be possible to speed up the calculation by reducing the Tensor4_2Sx2S matrix further 
            //  - If the fracture sets are all symmetrical about the horizontal plane, then the vertical shear components Cij,yz and Cij,zx components should all be zero (except Cyz,yz and Czx,zx)
            //    In this case we can reduce the tensors further by removing the YZ and ZX components
            //    This will simplify the calculation significantly, as we will only need to invert a 3x3 matrix containing XX, YY and XY components
            //  - If the compliance tensor is orthotropic, we can remove all shear components except Cxy,xy, Cyz,yz and Czx,zx which all have values (1+v)/E
            //    In this case we will only need to invert a 2x2 matrix containing XX and YY
            //    This procedure is equivalent to the horizontal stress relationships shmax = E/(1-v2)*(ehmax-v*ehmin) + v/(1-v)sv, shmin = E/(1-v2)*(ehmin-v*ehmax) + v/(1-v)sv
            bool horizontalSymmetric = true;
            bool noXYshear = true;
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                if ((ij != Tensor2SComponents.XY) && ((float)components[ij][Tensor2SComponents.XY] != 0f))
                    noXYshear = false;
                if ((ij != Tensor2SComponents.YZ) && ((float)components[ij][Tensor2SComponents.YZ] != 0f))
                    horizontalSymmetric = false;
                if ((ij != Tensor2SComponents.ZX) && ((float)components[ij][Tensor2SComponents.ZX] != 0f))
                    horizontalSymmetric = false;
            }
            bool orthotropic = horizontalSymmetric && noXYshear;

            // Create a list of components in the reduced tensor matrix
            List<Tensor2SComponents> TensorComponents;
            if (orthotropic)
                TensorComponents = new List<Tensor2SComponents>(new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ });
            else if (horizontalSymmetric)
                TensorComponents = new List<Tensor2SComponents>(new Tensor2SComponents[4] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY });
            else
                TensorComponents = new List<Tensor2SComponents>(new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX });

            // Invert the tensor
            // To do this we will first create a new Tensor4_2Sx2S object to store the inverted tensor in, and calculate the determinant of the current tensor matrix.
            // We will then loop through the relevant rows and columns of the reduced tensor matrix, calculating the cofactors for each component in turn.
            // We will then populate the inverted tensor object with the cofactors and the determinant,
            // bearing in mind each component of the inverted matrix is equal to the cofactor of a transpose of the current tensor matrix divided by the determinant of the currect tensor matrix.
            // Finally we will populate the ij,ij components of the rows that were removed, with 1/Cij,ij.
            // All other components of the inverted tenor will be left as zero.

            // Create a new Tensor4_2Sx2S object to store the inverted tensor in
            Tensor4_2Sx2S inverseMatrix = new Tensor4_2Sx2S();

            // Create variables for the overall determinant of the current tensor matrix, and for the polarity of the first column in the current row
            double determinant = SubmatrixDeterminant(TensorComponents, TensorComponents);
            double rowpolarity = 1;

            // If the determinant is zero, there is no inverse; return null
            if (determinant == 0)
                return null;

            // Loop through each row in the tensor matrix
            foreach (Tensor2SComponents row in TensorComponents)
            {
                // Create a list of all the tensor components except for the current row
                List<Tensor2SComponents> subrow = new List<Tensor2SComponents>(TensorComponents);
                subrow.Remove(row);
                // Set the polarity of the first component to the polarity of the first column in the current row
                double componentpolarity = rowpolarity;
                // Loop through each column in the tensor matrix
                foreach (Tensor2SComponents col in TensorComponents)
                {
                    // Create a list of all the tensor components except for the current column
                    List<Tensor2SComponents> subcol = new List<Tensor2SComponents>(TensorComponents);
                    subcol.Remove(col);
                    // Calculate the cofactor for the current row and column
                    double cofactor = componentpolarity * SubmatrixDeterminant(subrow, subcol);
                    // Populate the transverse of the current row and column in the inverse matrix
                    inverseMatrix.components[col][row] = cofactor / determinant;
                    // The next column will have the opposite polarity to this one
                    componentpolarity = -componentpolarity;
                }
                // The first column in the next row will have the opposite polarity to this one
                rowpolarity = -rowpolarity;
            }
            // Populate the shear components if the matrix was reduced
            if (orthotropic)
            {
                inverseMatrix.components[Tensor2SComponents.XY][Tensor2SComponents.XY] = 1 / components[Tensor2SComponents.XY][Tensor2SComponents.XY];
            }
            if (horizontalSymmetric)
            {
                inverseMatrix.components[Tensor2SComponents.YZ][Tensor2SComponents.YZ] = 1 / components[Tensor2SComponents.YZ][Tensor2SComponents.YZ];
                inverseMatrix.components[Tensor2SComponents.ZX][Tensor2SComponents.ZX] = 1 / components[Tensor2SComponents.ZX][Tensor2SComponents.ZX];
            }

            // Quick method; may be slightly faster if the matrix cannot be reduced, but will be slower if it can be reduced
            /*double determinant = Determinant;
            if (determinant == 0)
                return null;

            Tensor4_2Sx2S inverseMatrix = new Tensor4_2Sx2S();
            foreach (Tensor2SComponents i in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents j in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                {
                    double component_ij = Cofactor(j,i);
                    component_ij /= det;
                    inverseMatrix.components[i][j] = component_ij;
                }*/

            return inverseMatrix;
        }
        /// <summary>
        /// Calculate the determinant of a submatrix comprising some or all of the tensor matrix
        /// </summary>
        /// <param name="RowComponents">List of Tensor2SComponents indicating which rows to include in the submatrix</param>
        /// <param name="ColumnComponents">List of Tensor2SComponents indicating which columns to include in the submatrix</param>
        /// <returns>Determinant of a submatrix including the specified rows and columns from the tensor matrix</returns>
        private double SubmatrixDeterminant(List<Tensor2SComponents> RowComponents, List<Tensor2SComponents> ColumnComponents)
        {
            // Get the total number of rows in the specified submatrix
            int SubmatrixSize = RowComponents.Count;

            // Check that the total number of rows and columns in the specified submatrix is equal, and is not greater than 6 (the complete tensor matrix)
            // Otherwise the determinant cannot be calculated; return NaN
            if ((SubmatrixSize > 6) || (ColumnComponents.Count != SubmatrixSize))
                return double.NaN;

            // If the submatrix has size 3x3 components or smaller, we will calculate its determinant directly using standard formulae
            // If the submatrix is larger than 3x3 components then we will calculate its determinant recursively, breaking it up into smaller submatrices until we get down to 3x3
            if (SubmatrixSize == 1)
            {
                // For a 1x1 submatrix, the determinant is the value of the single component
                double c00 = components[RowComponents[0]][ColumnComponents[0]];
                return c00;
            }
            else if (SubmatrixSize == 2)
            {
                // For a 2x2 submatrix, the determinant is given by (c00 * c11) - (c01 * c10)
                double c00 = components[RowComponents[0]][ColumnComponents[0]];
                double c01 = components[RowComponents[0]][ColumnComponents[1]];
                double c10 = components[RowComponents[1]][ColumnComponents[0]];
                double c11 = components[RowComponents[1]][ColumnComponents[1]];
                return (c00 * c11) - (c01 * c10);
            }
            if (SubmatrixSize == 3)
            {
                // For a 3x3 submatrix, the determinant is given by (c00 * ((c11 * c22) - (c12 * c21))) + (c01 * ((c12 * c20) - (c10 * c22))) + (c02 * ((c10 * c21) - (c11 * c20)))
                double c00 = components[RowComponents[0]][ColumnComponents[0]];
                double c01 = components[RowComponents[0]][ColumnComponents[1]];
                double c02 = components[RowComponents[0]][ColumnComponents[2]];
                double c10 = components[RowComponents[1]][ColumnComponents[0]];
                double c11 = components[RowComponents[1]][ColumnComponents[1]];
                double c12 = components[RowComponents[1]][ColumnComponents[2]];
                double c20 = components[RowComponents[2]][ColumnComponents[0]];
                double c21 = components[RowComponents[2]][ColumnComponents[1]];
                double c22 = components[RowComponents[2]][ColumnComponents[2]];
                return (c00 * ((c11 * c22) - (c12 * c21))) + (c01 * ((c12 * c20) - (c10 * c22))) + (c02 * ((c10 * c21) - (c11 * c20)));
            }
            else if (SubmatrixSize > 3)
            {
                // For a submatrix larger than 3x3, we will reduce it to a series of smaller submatrices by removing one column at a time, and combining the determinants of these smaller submatrices
                // We calculate the determinants to these smaller submatrices by calling this function recursively
                // Note that if these smaller submatrices are still larger than 3x3, they will be broken down again when this function is called

                // Create variables for the overall determinant of the current submatrix, and for the polarity of the smaller submatrices
                double determinant = 0;
                double polarity = 1;

                // Create a list of row components for the smaller submatrices, comprising all the rows in the current submatrix except the first
                List<Tensor2SComponents> submatrixRows = new List<Tensor2SComponents>(RowComponents);
                Tensor2SComponents toprow = submatrixRows[0];
                submatrixRows.RemoveAt(0);

                // We can now generate a series of smaller submatrices by removing each column in turn from the current submatrix
                // The contribution of each smaller submatrix to the overall determinant will be the product of the component of the first row and the column we have removed, 
                // the polarity of the smaller submatrix formed by removing this column, and the determinant of the smaller submatrix formed by removing this column 
                // We will calculate this for each of the submatrices and add it to the overall determinant
                // Loop through each column in this submatrix
                foreach (Tensor2SComponents column in ColumnComponents)
                {
                    // Create a list of column components for the smaller submatrix, comprising all the columns in this submatrix except the column we are removing
                    List<Tensor2SComponents> submatrixCols = new List<Tensor2SComponents>(ColumnComponents);
                    submatrixCols.Remove(column);

                    // Calculate the increment to the overall determinant and add it to the current value
                    double increment = polarity * components[toprow][column] * SubmatrixDeterminant(submatrixRows, submatrixCols);
                    determinant += increment;

                    // The next smaller submatrix will have the opposite polarity to this one
                    polarity = -polarity;
                }

                // Now we can return the value of the overall determinant
                return determinant;
            }
            else
                // If the submatrix does not match any of these specifications, return NaN
                return double.NaN;
        }
        // Alternative methods for calculating determinant and cofactors for the full tensor matrix, using the formula |A|=epsilon(i,j,k,l,m,n)*ai1*aj2*ak3*al4*am5*an6, summing across i,j,k,l,m,n
        // and cycling through all valid permutations by swapping indices one by one, in sequence.
        // This may be slightly faster than the current method for the full tensor matrix, but it is not as robust and flexible since it does not easily allow for reduced matrices.
        // Using reduced matrices when e.g. vertical stress is known or shear components can be ignored will lead to much greater increase in calculation speed, so the more flexible method is used instead.
        /*/// <summary>
        /// Determinant (third invariant) of the tensor matrix
        /// </summary>
        public double Determinant
        {
            get
            {
                // Use the formula |A|=epsilon(i,j,k,l,m,n)*ai1*aj2*ak3*al4*am5*an6, summing across i,j,k,l,m,n, i.e. through all valid permutations of indices
                // However to speed up the calculation, we can cycle through all valid permutations by swapping indices one by one, in sequence
                // If we start with a valid permutation then each swap will also generate a valid permutation, so we do not need to check the validity of each permutation
                // Since each swap will reverse the polarity of the permutation, we do not need to calculating the polarity of each permutation
                Tensor2SComponents[] index1Permutation = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                Tensor2SComponents[] index2Permutation = new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX };
                double polarity = 1;
                double output = 0;

                // Cycle through every possible valid permutation using a series of nested loops, each swapping one of the indices in turn

                // Loop swapping the 1st index in the current permutation - loop six times through this
                for (int loop_i = 0; loop_i < 6; loop_i++)
                {

                    // Loop swapping the 2nd index in the current permutation - loop five times through this
                    for (int loop_j = 0; loop_j < 5; loop_j++)
                    {

                        // Loop swapping the 3rd index in the current permutation - loop four times through this
                        for (int loop_k = 0; loop_k < 4; loop_k++)
                        {

                            // Loop swapping the 4th index in the current permutation - loop three times through this
                            for (int loop_l = 0; loop_l < 3; loop_l++)
                            {

                                // Loop swapping the 5th and 6th indices in the current permutation - loop twice through this
                                for (int loop_m = 0; loop_m < 2; loop_m++)
                                {
                                    // Calculate the term for the current permutation and add it to the determinant
                                    double currentTerm = polarity;
                                    for (int factor = 0; factor < 6; factor++)
                                        currentTerm *= components[index1Permutation[factor]][index2Permutation[factor]];
                                    output += currentTerm;


                                    // If this is not the final iteration of the loop then swap the 5th and 6th indices of the current permutation
                                    if (loop_m < 1)
                                    {
                                        // Swap the 5th and 6th indices of the current permutation
                                        Tensor2SComponents swap_m = index2Permutation[5];
                                        index2Permutation[5] = index2Permutation[4];
                                        index2Permutation[4] = swap_m;

                                        // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                                        polarity = -polarity;
                                    }
                                    // If this is the final iteration of the loop then do not swap the indices - instead move on to the next loop out and swap those indices

                                } // End loop swapping the 5th and 6th indices

                                // If this is not the final iteration of the loop then swap the 4th and 6th indices of the current permutation
                                if (loop_l < 2)
                                {
                                    // Swap the 4th and 6th indices of the current permutation
                                    Tensor2SComponents swap_l = index2Permutation[5];
                                    index2Permutation[5] = index2Permutation[3];
                                    index2Permutation[3] = swap_l;

                                    // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                                    polarity = -polarity;
                                }
                                // If this is the final iteration of the loop then swap the 4th and 5th indices of the current permutation
                                else
                                {
                                    // Swap the 4th and 5th indices of the current permutation
                                    Tensor2SComponents swap_l = index2Permutation[4];
                                    index2Permutation[4] = index2Permutation[3];
                                    index2Permutation[3] = swap_l;

                                    // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                                    polarity = -polarity;
                                }

                            } // End loop swapping the 4th index

                            // If this is not the final iteration of the loop then swap the 3rd and 6th indices of the current permutation
                            if (loop_k < 3)
                            {
                                // Swap the 3rd and 6th indices of the current permutation
                                Tensor2SComponents swap_k = index2Permutation[5];
                                index2Permutation[5] = index2Permutation[2];
                                index2Permutation[2] = swap_k;

                                // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                                polarity = -polarity;
                            }
                            // If this is the final iteration of the loop then cycle the 3rd, 4th and 5th indices to the left
                            else
                            {
                                // Cycle the 3rd, 4th and 5th indices to the left
                                Tensor2SComponents swap_k = index2Permutation[4];
                                index2Permutation[4] = index2Permutation[2];
                                index2Permutation[2] = index2Permutation[3];
                                index2Permutation[3] = swap_k;

                                // Do not swap the polarity - we have made two index swaps so the polarity will not change
                            }

                        } // End loop swapping the 3rd index

                        // If this is not the final iteration of the loop then swap the 2nd and 6th indices of the current permutation
                        if (loop_j < 4)
                        {
                            // Swap the 2nd and 6th indices of the current permutation
                            Tensor2SComponents swap_j = index2Permutation[5];
                            index2Permutation[5] = index2Permutation[1];
                            index2Permutation[1] = swap_j;

                            // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                            polarity = -polarity;
                        }
                        // If this is the final iteration of the loop then cycle the 2nd, 3rd, 4th and 5th indices to the left
                        else
                        {
                            // Cycle the 2nd, 3rd, 4th and 5th indices to the left
                            Tensor2SComponents swap_j = index2Permutation[4];
                            index2Permutation[4] = index2Permutation[1];
                            index2Permutation[1] = index2Permutation[2];
                            index2Permutation[2] = index2Permutation[3];
                            index2Permutation[3] = swap_j;

                            // Swap the polarity - we have made three index swaps so the next term will have the opposite polarity
                            polarity = -polarity;
                        }

                    } // End loop swapping the 2nd index

                    // If this is not the final iteration of the loop then swap the 1st and 6th indices of the current permutation
                    if (loop_i < 5)
                    {
                        // Swap the 1st and 6th indices of the current permutation
                        Tensor2SComponents swap_i = index2Permutation[5];
                        index2Permutation[5] = index2Permutation[0];
                        index2Permutation[0] = swap_i;

                        // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                        polarity = -polarity;
                    }
                    // If this is the final iteration of the loop then the calculation is finished - we do not need to swap the indices

                } // End loop swapping the 1st index

                // Return the final value of the determinant
                return output;
            }
        }
        /// <summary>
        /// Cofactor to a specified component of the tensor
        /// </summary>
        /// <param name="RowIndex">Index of the tensor matrix row (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <param name="ColumnIndex">Index of the tensor matrix column (XX, YY, ZZ, XY, YZ or ZX)</param>
        /// <returns>Cofactor to the specified tensor component</returns>
        public double Cofactor(Tensor2SComponents RowIndex, Tensor2SComponents ColumnIndex)
        {
            // The cofactor is the determinant of a 5x5 matrix including all the components of the tensor matrix except those in the specified row and column, multiplied by the polarity of the specified row and column indices
            // We can calculate this using the formula |A|=epsilon(i,j,k,l,m)*ai1*aj2*ak3*al4*am5, summing across i,j,k,l,m, i.e. through all valid permutations of indices
            // However to speed up the calculation, we can cycle through all valid permutations by swapping indices one by one, in sequence
            // If we start with a valid permutation then each swap will also generate a valid permutation, so we do not need to check the validity of each permutation
            // Since each swap will reverse the polarity of the permutation, we do not need to calculating the polarity of each permutation
            List<Tensor2SComponents> index1Permutation = new List<Tensor2SComponents>();
            List<Tensor2SComponents> index2Permutation = new List<Tensor2SComponents>();
            foreach (Tensor2SComponents component in new Tensor2SComponents[6] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX })
            {
                if (component != RowIndex)
                    index1Permutation.Add(component);
                if (component != ColumnIndex)
                    index2Permutation.Add(component);
            }
            // We can start by setting the initial polarity to that of the specified row and column indices 
            double polarity = ((((int)RowIndex - (int)ColumnIndex)) % 2 == 0 ? 1 : -1);
            double output = 0;

            // Cycle through every possible valid permutation using a series of nested loops, each swapping one of the indices in turn

            // Loop swapping the 1st index in the current permutation - loop five times through this
            for (int loop_i = 0; loop_i < 5; loop_i++)
            {

                // Loop swapping the 2nd index in the current permutation - loop four times through this
                for (int loop_j = 0; loop_j < 4; loop_j++)
                {

                    // Loop swapping the 3rd index in the current permutation - loop three times through this
                    for (int loop_k = 0; loop_k < 3; loop_k++)
                    {

                        // Loop swapping the 4th and 5th indices in the current permutation - loop twice through this
                        for (int loop_l = 0; loop_l < 2; loop_l++)
                        {
                            // Calculate the term for the current permutation and add it to the determinant
                            double currentTerm = polarity;
                            for (int factor = 0; factor < 5; factor++)
                                currentTerm *= components[index1Permutation[factor]][index2Permutation[factor]];
                            output += currentTerm;

                            // If this is not the final iteration of the loop then swap the 4th and 5th indices of the current permutation
                            if (loop_l < 1)
                            {
                                // Swap the 4th and 5th indices of the current permutation
                                Tensor2SComponents swap_l = index2Permutation[4];
                                index2Permutation[4] = index2Permutation[3];
                                index2Permutation[3] = swap_l;

                                // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                                polarity = -polarity;
                            }
                            // If this is the final iteration of the loop then do not swap the indices - instead move on to the next loop out and swap those indices

                        } // End loop swapping the 4th and 5th indices

                        // If this is not the final iteration of the loop then swap the 3rd and 5th indices of the current permutation
                        if (loop_k < 2)
                        {
                            // Swap the 3rd and 5th indices of the current permutation
                            Tensor2SComponents swap_k = index2Permutation[4];
                            index2Permutation[4] = index2Permutation[2];
                            index2Permutation[4] = swap_k;

                            // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                            polarity = -polarity;
                        }
                        // If this is the final iteration of the loop then swap the 3rd and 4th indices of the current permutation
                        else
                        {
                            // Swap the 3rd and 4th indices of the current permutation
                            Tensor2SComponents swap_k = index2Permutation[3];
                            index2Permutation[3] = index2Permutation[2];
                            index2Permutation[2] = swap_k;

                            // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                            polarity = -polarity;
                        }

                    } // End loop swapping the 3rd index

                    // If this is not the final iteration of the loop then swap the 2nd and 5th indices of the current permutation
                    if (loop_j < 3)
                    {
                        // Swap the 2nd and 5th indices of the current permutation
                        Tensor2SComponents swap_j = index2Permutation[4];
                        index2Permutation[4] = index2Permutation[1];
                        index2Permutation[1] = swap_j;

                        // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                        polarity = -polarity;
                    }
                    // If this is the final iteration of the loop then cycle the 2nd, 3rd, and 4th indices to the left
                    else
                    {
                        // Cycle the 2nd, 3rd, and 4th indices to the left
                        Tensor2SComponents swap_j = index2Permutation[3];
                        index2Permutation[3] = index2Permutation[1];
                        index2Permutation[1] = index2Permutation[2];
                        index2Permutation[2] = swap_j;

                        // Do not swap the polarity - we have made two index swaps so the polarity will not change
                    }

                } // End loop swapping the 2nd index

                // If this is not the final iteration of the loop then swap the 1st and 6th indices of the current permutation
                if (loop_i < 4)
                {
                    // Swap the 1st and 5th indices of the current permutation
                    Tensor2SComponents swap_i = index2Permutation[4];
                    index2Permutation[4] = index2Permutation[0];
                    index2Permutation[0] = swap_i;

                    // Swap the polarity - we have made one index swap so the next term will have the opposite polarity
                    polarity = -polarity;
                }
                // If this is the final iteration of the loop then the calculation is finished - we do not need to swap the indices

            } // End loop swapping the 1st index

            // Return the final value of the determinant
            return output;
        }*/

        // Geometric operations
        /// <summary>
        /// Double the values of all tensor components in the shear columns (i.e. ij,XY, ij,YZ and ij,ZX)
        /// This can be useful in some calculations to compensate for the fact that the shear components appear only once in the vector form of the symmetric second order tensors
        /// </summary>
        public void DoubleShearColumnComponents()
        {
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                foreach (Tensor2SComponents kl in new Tensor2SComponents[3] { Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX })
                    components[ij][kl] *= 2;
        }
        /// <summary>
        /// Populate the second order symmetric tensors A and B, where A = this * B, and components Axx, Ayy, Axy, Ayz, Azx and Bzz are known
        /// </summary>
        /// <param name="A">Reference to a Tensor2S object where XX, YY, XY, YZ and ZX components are set; ZZ component will be populated</param>
        /// <param name="B">Reference to a Tensor2S object where ZZ component is set; other components will be populated</param>
        public void PartialInversion(ref Tensor2S A, ref Tensor2S B)
        {
            // This is useful for example to populate the stress tensor, if the vertical stress and horizontal strain components are known
            // In that case, this Tensor4_2Sx2S object should represent the compliance tensor
            // A represents the strain tensor and B represents the stress tensor

            // We will do this by generating a reduced Tensor4_2Sx2S matrix with the ZZ components removed, and then inverting it
            // We must also subtract components representing CijzzBzz from tensor A
            // We can then multiply the inverse of the reduced Tensor4_2Sx2S matrix by the modified tensor A 

            // It may also be possible to speed up the calculation by reducing the Tensor4_2Sx2S matrix further 
            //  - If the fracture sets are all symmetrical about the horizontal plane, then the vertical shear components Cij,yz and Cij,zx components should all be zero (except Cyz,yz and Czx,zx)
            //    In this case we can reduce the tensors further by removing the YZ and ZX components
            //    This will simplify the calculation significantly, as we will only need to invert a 3x3 matrix containing XX, YY and XY components
            //  - If the compliance tensor is orthotropic, we can remove all shear components except Cxy,xy, Cyz,yz and Czx,zx which all have values (1+v)/E
            //    In this case we will only need to invert a 2x2 matrix containing XX and YY
            //    This procedure is equivalent to the horizontal stress relationships shmax = E/(1-v2)*(ehmax-v*ehmin) + v/(1-v)sv, shmin = E/(1-v2)*(ehmin-v*ehmax) + v/(1-v)sv
            bool horizontalSymmetric = true;
            bool noXYshear = true;
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                if ((ij != Tensor2SComponents.XY) && ((float)components[ij][Tensor2SComponents.XY] != 0f))
                    noXYshear = false;
                if ((ij != Tensor2SComponents.YZ) && ((float)components[ij][Tensor2SComponents.YZ] != 0f))
                    horizontalSymmetric = false;
                if ((ij != Tensor2SComponents.ZX) && ((float)components[ij][Tensor2SComponents.ZX] != 0f))
                    horizontalSymmetric = false;
            }
            bool orthotropic = horizontalSymmetric && noXYshear;
            
            // Create a list of components in the reduced tensor matrix
            List<Tensor2SComponents> TensorComponents;
            if (orthotropic)
                TensorComponents = new List<Tensor2SComponents>(new Tensor2SComponents[2] { Tensor2SComponents.XX, Tensor2SComponents.YY });
            else if (horizontalSymmetric)
                TensorComponents = new List<Tensor2SComponents>(new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.XY });
            else
                TensorComponents = new List<Tensor2SComponents>(new Tensor2SComponents[5] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX });

            // Invert the reduced tensor
            // To do this we will first create a new Tensor4_2Sx2S object to store the inverted tensor in, and calculate the determinant of the current tensor matrix.
            // We will then loop through the relevant rows and columns of the reduced tensor matrix, calculating the cofactors for each component in turn.
            // We will then populate the inverted tensor object with the cofactors and the determinant,
            // bearing in mind each component of the inverted matrix is equal to the cofactor of a transpose of the current tensor matrix divided by the determinant of the currect tensor matrix.
            // Finally we will populate the ij,ij components of the rows that were removed, with 1/Cij,ij.
            // All other components of the inverted tenor will be left as zero.
            // Create a new Tensor4_2Sx2S object to store the inverted tensor in
            Tensor4_2Sx2S inverseMatrix = new Tensor4_2Sx2S();
            // Create variables for the overall determinant of the current tensor matrix, and for the polarity of the first column in the current row
            double determinant = SubmatrixDeterminant(TensorComponents, TensorComponents);
            double rowpolarity = 1;
            // Loop through each row in the tensor matrix
            foreach (Tensor2SComponents row in TensorComponents)
            {
                // Create a list of all the tensor components except for the current row
                List<Tensor2SComponents> subrow = new List<Tensor2SComponents>(TensorComponents);
                subrow.Remove(row);
                // Set the polarity of the first component to the polarity of the first column in the current row
                double componentpolarity = rowpolarity;
                // Loop through each column in the tensor matrix
                foreach (Tensor2SComponents col in TensorComponents)
                {
                    // Create a list of all the tensor components except for the current column
                    List<Tensor2SComponents> subcol = new List<Tensor2SComponents>(TensorComponents);
                    subcol.Remove(col);
                    // Calculate the cofactor for the current row and column
                    double cofactor = componentpolarity * SubmatrixDeterminant(subrow, subcol);
                    // Populate the transverse of the current row and column in the inverse matrix
                    inverseMatrix.components[col][row] = cofactor / determinant;
                    // The next column will have the opposite polarity to this one
                    componentpolarity = -componentpolarity;
                }
                // The first column in the next row will have the opposite polarity to this one
                rowpolarity = -rowpolarity;
            }
            // Populate the shear components if the matrix was reduced
            if (orthotropic)
            {
                inverseMatrix.components[Tensor2SComponents.XY][Tensor2SComponents.XY] = 1 / components[Tensor2SComponents.XY][Tensor2SComponents.XY];
            }
            if (horizontalSymmetric)
            {
                inverseMatrix.components[Tensor2SComponents.YZ][Tensor2SComponents.YZ] = 1 / components[Tensor2SComponents.YZ][Tensor2SComponents.YZ];
                inverseMatrix.components[Tensor2SComponents.ZX][Tensor2SComponents.ZX] = 1 / components[Tensor2SComponents.ZX][Tensor2SComponents.ZX];
            }

            // Next we need to subtract CijzzBzz from the components of the A tensor
            double Bzz = B.Component(Tensor2SComponents.ZZ);
            Tensor2S AMinusBzz = new Tensor2S();
            foreach (Tensor2SComponents ij in new Tensor2SComponents[5] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX })
            {
                double AMinusBzzComponent = A.Component(ij) - (components[ij][Tensor2SComponents.ZZ] * Bzz);
                AMinusBzz.Component(ij, AMinusBzzComponent);
            }

            // Now we can multiply the modified A tensor by the inverse of the reduced Tensor4_2Sx2S matrix to populate B
            B = inverseMatrix * AMinusBzz;
            // We must add back the Bzz component as this will have been lost in the previous calculation
            B.Component(Tensor2SComponents.ZZ, Bzz);

            // Finally we can calculate the (so far) unknown value of Azz, using the B tensor we have just calculated and the original Tensor4_2Sx2S matrix
            double Azz = 0;
            foreach (Tensor2SComponents ij in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                Azz += components[Tensor2SComponents.ZZ][ij] * A.Component(ij);
            // We can now add the Azz component into the A tensor
            A.Component(Tensor2SComponents.ZZ, Azz);
        }

        // Functions to generate specific elastic tensors
        /// <summary>
        /// Create an isotropic stiffness tensor for the specified Young's Modulus and Poisson's ratio values
        /// </summary>
        /// <param name="YoungsModulus">Young's Modulus</param>
        /// <param name="PoissonsRatio">Poisson's ratio</param>
        /// <returns>Tensor4_2Sx2S object representing the isotropic stiffness tensor</returns>
        public static Tensor4_2Sx2S IsotropicStiffnessTensor(double YoungsModulus, double PoissonsRatio)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();

            double iiii_component = (YoungsModulus * (1 - PoissonsRatio)) / ((1 + PoissonsRatio) * (1 - (2 * PoissonsRatio)));
            double iijj_component = (YoungsModulus * PoissonsRatio) / ((1 + PoissonsRatio) * (1 - (2 * PoissonsRatio)));
            double ijij_component = YoungsModulus / (1 + PoissonsRatio);

            foreach (Tensor2SComponents RowIndex in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ })
                foreach (Tensor2SComponents ColumnIndex in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ })
                {
                    if (RowIndex == ColumnIndex)
                        output.components[RowIndex][ColumnIndex] = iiii_component;
                    else
                        output.components[RowIndex][ColumnIndex] = iijj_component;
                }
            foreach (Tensor2SComponents RowIndex in new Tensor2SComponents[3] { Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX })
            {
                Tensor2SComponents ColumnIndex = RowIndex;
                output.components[RowIndex][ColumnIndex] = ijij_component;
            }

            return output;
        }
        /// <summary>
        /// Create an isotropic compliance tensor for the specified Young's Modulus and Poisson's ratio values
        /// </summary>
        /// <param name="YoungsModulus">Young's Modulus</param>
        /// <param name="PoissonsRatio">Poisson's ratio</param>
        /// <returns>Tensor4_2Sx2S object representing the isotropic compliance tensor</returns>
        public static Tensor4_2Sx2S IsotropicComplianceTensor(double YoungsModulus, double PoissonsRatio)
        {
            Tensor4_2Sx2S output = new Tensor4_2Sx2S();

            double iiii_component = 1 / YoungsModulus;
            double iijj_component = -PoissonsRatio / YoungsModulus;
            double ijij_component = (1 + PoissonsRatio) / YoungsModulus;

            foreach (Tensor2SComponents RowIndex in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ })
                foreach (Tensor2SComponents ColumnIndex in new Tensor2SComponents[3] { Tensor2SComponents.XX, Tensor2SComponents.YY, Tensor2SComponents.ZZ })
                {
                    if (RowIndex == ColumnIndex)
                        output.components[RowIndex][ColumnIndex] = iiii_component;
                    else
                        output.components[RowIndex][ColumnIndex] = iijj_component;
                }
            foreach (Tensor2SComponents RowIndex in new Tensor2SComponents[3] { Tensor2SComponents.XY, Tensor2SComponents.YZ, Tensor2SComponents.ZX })
            {
                Tensor2SComponents ColumnIndex = RowIndex;
                output.components[RowIndex][ColumnIndex] = ijij_component;
            }

            return output;
        }

        // Constructors
        /// <summary>
        /// Constructor to create a new tensor with zero values for all components
        /// </summary>
        public Tensor4_2Sx2S()
        {
            // Create dictionary array containing the tensor components, and add zero for each component
            components = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, double>>();
            foreach (Tensor2SComponents RowIndex in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                Dictionary<Tensor2SComponents, double> thisRow = new Dictionary<Tensor2SComponents, double>();
                foreach (Tensor2SComponents ColumnIndex in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    thisRow.Add(ColumnIndex, 0);
                components.Add(RowIndex, thisRow);
            }
        }
        /// <summary>
        /// Constructor to create a copy of an existing tensor
        /// </summary>
        /// <param name="tensor_in">Tensor4_2Sx2S object to copy</param>
        public Tensor4_2Sx2S(Tensor4_2Sx2S tensor_in)
        {
            // Create dictionary array containing the tensor components, and add the components from the input tensor
            components = new Dictionary<Tensor2SComponents, Dictionary<Tensor2SComponents, double>>();
            foreach (Tensor2SComponents RowIndex in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
            {
                Dictionary<Tensor2SComponents, double> thisRow = new Dictionary<Tensor2SComponents, double>();
                foreach (Tensor2SComponents ColumnIndex in Enum.GetValues(typeof(Tensor2SComponents)).Cast<Tensor2SComponents>())
                    thisRow.Add(ColumnIndex, tensor_in.components[RowIndex][ColumnIndex]);
                components.Add(RowIndex, thisRow);
            }
        }
    }
}
