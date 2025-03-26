using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Statistical data describing implicit layer-bound macrofracture population for a single dipset
    /// </summary>
    class MacrofractureData
    {
        // References to external objects
        /// <summary>
        /// Reference to great-grandparent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;
        /// <summary>
        /// Reference to parent FractureDipSet object
        /// </summary>
        private FractureDipSet fds;

        // Population data for all fractures
        /// <summary>
        /// Total volumetric density of active half-macrofractures
        /// </summary>
        public double a_P30_total { get; set; }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to stress shadow interaction
        /// </summary>
        public double sII_P30_total { get; set; }
        /// <summary>
        /// Total volumetric density of static half-macrofractures terminated due to intersection with other fracture sets
        /// </summary>
        public double sIJ_P30_total { get; set; }
        /// <summary>
        /// Total linear density of active half-macrofractures
        /// </summary>
        public double a_P32_total { get; set; }
        /// <summary>
        /// Total linear density of static half-macrofractures
        /// </summary>
        public double s_P32_total { get; set; }
        /// <summary>
        /// Total volumetric ratio of active half-macrofractures
        /// </summary>
        public double a_P33_total() { return (Math.PI/4) * gbc.ThicknessAtDeformation * a_P32_total; }
        /// <summary>
        /// Total volumetric ratio of static half-macrofractures
        /// </summary>
        public double s_P33_total() { return (Math.PI/4) * gbc.ThicknessAtDeformation * s_P32_total; }

        // Arrays for population distribution functions
        /// <summary>
        /// Index half-lengths for cumulative population distribution function arrays
        /// </summary>
        public double[] halflengths { get; private set; }
        // Arrays for piecewise cumulative population distribution functions
        /// <summary>
        /// Cumulative volumetric density distribution for active half-macrofractures 
        /// </summary>
        public double[] a_P30 { get; set; }
        /// <summary>
        /// Cumulative volumetric density distribution for static half-macrofractures terminated due to stress shadow interaction
        /// </summary>
        public double[] sII_P30 { get; set; }
        /// <summary>
        /// Cumulative volumetric density distribution for static half-macrofractures terminated due to intersection with other fracture sets
        /// </summary>
        public double[] sIJ_P30 { get; set; }
        /// <summary>
        /// Cumulative linear density distribution for active half-macrofractures 
        /// </summary>
        public double[] a_P32 { get; set; }
        /// <summary>
        /// Cumulative linear density distribution for static half-macrofractures 
        /// </summary>
        public double[] s_P32 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for active half-macrofractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double a_P33(int index) { return (Math.PI / 4) * gbc.ThicknessAtDeformation * a_P32[index]; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static half-macrofractures
        /// </summary>
        /// <param name="index">Index for piecewise cumulative distribution function arrays</param>
        /// <returns></returns>
        public double s_P33(int index) { return (Math.PI / 4) * gbc.ThicknessAtDeformation * s_P32[index]; }

        // Displacement functions: These are included in the parent FractureDipSet object
        // Porosity / heave functions: These are included in the parent FractureDipSet object
        // Stress shadow width functions: These are included in the parent FractureDipSet object
        // Stress shadow volume functions: These are included in the parent FractureDipSet object

        // Reset and data input functions
        /// <summary>
        /// Reset the arrays for the piecewise population distribution functions based on a supplied array of index halflengths
        /// </summary>
        /// <param name="halflengths_in">Array of macrofracture halflengths to use as index values for the piecewise population distribution functions</param>
        public void ResetPopulationDistributionData(double[] halflengths_in)
        {
            // Set the array of radii for the piecewise population distribution functions
            halflengths = halflengths_in;
            int no_halflengths = halflengths_in.Count();

            // Recreate arrays for the piecewise population distribution functions and fill then with zero values
            a_P30 = new double[no_halflengths];
            sII_P30 = new double[no_halflengths];
            sIJ_P30 = new double[no_halflengths];
            a_P32 = new double[no_halflengths];
            s_P32 = new double[no_halflengths];
            for (int halflength_no = 0; halflength_no < no_halflengths; halflength_no++)
            {
                a_P30[halflength_no] = 0;
                sII_P30[halflength_no] = 0;
                sIJ_P30[halflength_no] = 0;
                a_P32[halflength_no] = 0;
                s_P32[halflength_no] = 0;
            }
        }


        /// <summary>
        /// Default Constructor: initial state has no fractures and empty arrays for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        public MacrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in) : this (gbc_in, fds_in, new double[0])
        {
        }

        /// <summary>
        /// Constructor: Supply an external array for the halflength values for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        /// <param name="halflengths_in">Array of macrofracture halflengths to use as index values for the piecewise population distribution functions</param>
        public MacrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in, double[] halflengths_in)
        {
            // Reference to parent FractureDipSet object
            fds = fds_in;
            // Reference to great-grandparent GridblockConfiguration object 
            gbc = gbc_in;

            // Set total fracture population value to zero
            a_P30_total = 0;
            sII_P30_total = 0;
            sIJ_P30_total = 0;
            a_P32_total = 0;
            s_P32_total = 0;

            // Reset the arrays for the piecewise population distribution functions based on a supplied array of index halflengths
            ResetPopulationDistributionData(halflengths_in);
        }
    }
}
