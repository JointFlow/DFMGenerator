using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFNGenerator_SharedCode
{
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

        // Arrays for piecewise cumulative population distribution functions
        /// <summary>
        /// Index half-lengths for cumulative population distribution function arrays
        /// </summary>
        public List<double> halflengths;
        /// <summary>
        /// Cumulative volumetric density distribution for active half-macrofractures 
        /// </summary>
        public List<double> a_P30;
        /// <summary>
        /// Cumulative volumetric density distribution for static half-macrofractures terminated due to stress shadow interaction
        /// </summary>
        public List<double> sII_P30;
        /// <summary>
        /// Cumulative volumetric density distribution for static half-macrofractures terminated due to intersection with other fracture sets
        /// </summary>
        public List<double> sIJ_P30;
        /// <summary>
        /// Cumulative linear density distribution for active half-macrofractures 
        /// </summary>
        public List<double> a_P32;
        /// <summary>
        /// Cumulative linear density distribution for static half-macrofractures 
        /// </summary>
        public List<double> s_P32;
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

        /// <summary>
        /// Default Constructor: initial state has no fractures and empty arrays for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        public MacrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in) : this (gbc_in, fds_in, new List<double>())
        {
        }

        /// <summary>
        /// Constructor: Supply an external array for the halflength values for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        /// <param name="halflengths_in">Reference to an array of macrofracture half-lengths</param>
        public MacrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in, List<double> halflengths_in)
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

            // Set the array of halflengths for the piecewise cumulative population distribution functions to point externally
            halflengths = halflengths_in;
            int NoValues = halflengths_in.Count;

            // Create arrays for piecewise cumulative population distribution functions and fill then with zero values
            a_P30 = new List<double>();
            sII_P30 = new List<double>();
            sIJ_P30 = new List<double>();
            a_P32 = new List<double>();
            s_P32 = new List<double>();
            for(int index = 0; index < NoValues; index++)
            {
                a_P30.Add(0d);
                sII_P30.Add(0d);
                sIJ_P30.Add(0d);
                a_P32.Add(0d);
                s_P32.Add(0d);
            }
        }
    }
}
