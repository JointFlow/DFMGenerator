using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFMGenerator_SharedCode
{
    class MicrofractureData
    {
        // References to external objects
        /// Reference to great-grandparent GridblockConfiguration object
        /// </summary>
        private GridblockConfiguration gbc;
        /// <summary>
        /// Reference to parent FractureDipSet object
        /// </summary>
        private FractureDipSet fds;

        // Population data for all fractures
        /// <summary>
        /// Total volumetric density of active microfractures
        /// </summary>
        public double a_P30_total { get; set; }
        /// <summary>
        /// Total volumetric density of static microfractures
        /// </summary>
        public double s_P30_total { get; set; }
        /// <summary>
        /// Total linear density of active microfractures
        /// </summary>
        public double a_P32_total { get; set; }
        /// <summary>
        /// Total linear density of static microfractures
        /// </summary>
        public double s_P32_total { get; set; }
        /// <summary>
        /// Total volumetric ratio of active microfractures
        /// </summary>
        public double a_P33_total { get; set; }
        /// <summary>
        /// Total volumetric ratio of static microfractures
        /// </summary>
        public double s_P33_total { get; set; }
        /// <summary>
        /// Total P35 value for active microfractures
        /// </summary>
        public double a_P35_total { get; set; }
        /// <summary>
        /// Total P35 value for static microfractures
        /// </summary>
        public double s_P35_total { get; set; }
        // We will not include values for total P31 or P34 at present
        /// <summary>
        /// Total P31 value for active microfractures
        /// </summary>
        //public double a_P31_total { get; set; }
        /// <summary>
        /// Total P31 value for static microfractures
        /// </summary>
        //public double s_P31_total { get; set; }
        /// <summary>
        /// Total P34 value for active microfractures
        /// </summary>
        //public double a_P34_total { get; set; }
        /// <summary>
        /// Total P34 value for static microfractures
        /// </summary>
        //public double s_P34_total { get; set; }

        // Arrays for population distribution functions
        /// <summary>
        /// Index radii for cumulative population distribution function arrays
        /// </summary>
        public double[] radii { get; private set; }
        // Arrays for piecewise volumetric density distribution functions
        // These are not cumulative but represent the number of fractures within each size bin per unit volume
        // This can be used to calculate the cumulative population distribution functions
        /// <summary>
        /// Volumetric density distribution (not cumulative) for active microfractures 
        /// </summary>
        public double[] a_DP30 { get; set; }
        /// <summary>
        /// Volumetric density distribution (not cumulative) for static microfractures 
        /// </summary>
        public double[] s_DP30 { get; set; }
        // Arrays for piecewise cumulative population distribution functions
        /// <summary>
        /// Cumulative volumetric density distribution for active microfractures 
        /// </summary>
        public double[] a_P30 { get; set; }
        /// <summary>
        /// Cumulative volumetric density distribution for static microfractures 
        /// </summary>
        public double[] s_P30 { get; set; }
        /// <summary>
        /// Cumulative linear density distribution for active microfractures 
        /// </summary>
        public double[] a_P32 { get; set; }
        /// <summary>
        /// Cumulative linear density distribution for static microfractures 
        /// </summary>
        public double[] s_P32 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for active microfractures 
        /// </summary>
        public double[] a_P33 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static microfractures 
        /// </summary>
        public double[] s_P33 { get; set; }
        // We will not include cumulative arrays for the P31, P34 or P35 values at present
        /// <summary>
        /// Cumulative volumetric ratio distribution for active microfractures 
        /// </summary>
        //public double[] a_P31 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static microfractures 
        /// </summary>
        //public double[] s_P31 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for active microfractures 
        /// </summary>
        //public double[] a_P34 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static microfractures 
        /// </summary>
        //public double[] s_P34 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for active microfractures 
        /// </summary>
        //public double[] a_P35 { get; set; }
        /// <summary>
        /// Cumulative volumetric ratio distribution for static microfractures 
        /// </summary>
        //public double[] s_P35 { get; set; }

        // Displacement functions: These are included in the parent FractureDipSet object
        // Porosity / heave functions: These are included in the parent FractureDipSet object
        // Stress shadow width functions: These are included in the parent FractureDipSet object
        // Stress shadow volume functions: These are included in the parent FractureDipSet object

        // Reset and data input functions
        /// <summary>
        /// Reset the arrays for the piecewise population distribution functions based on a supplied array of index radii
        /// </summary>
        /// <param name="radii_in">Array of microfracture radii to use as index values for the piecewise population distribution functions</param>
        public void ResetPopulationDistributionData(double[] radii_in)
        {
            // Set the array of radii for the piecewise population distribution functions
            radii = radii_in;
            int no_radii = radii_in.Count();

            // Recreate arrays for the piecewise population distribution functions and fill then with zero values
            a_DP30 = new double[no_radii];
            s_DP30 = new double[no_radii];
            a_P30 = new double[no_radii];
            s_P30 = new double[no_radii];
            a_P32 = new double[no_radii];
            s_P32 = new double[no_radii];
            a_P33 = new double[no_radii];
            s_P33 = new double[no_radii];
            //a_P31 = new double[no_radii];
            //s_P31 = new double[no_radii];
            //a_P34 = new double[no_radii];
            //s_P34 = new double[no_radii];
            //a_P35 = new double[no_radii];
            //s_P35 = new double[no_radii];
            for (int radius_no = 0; radius_no < no_radii; radius_no++)
            {
                a_DP30[radius_no] = 0;
                s_DP30[radius_no] = 0;
                a_P30[radius_no] = 0;
                s_P30[radius_no] = 0;
                a_P32[radius_no] = 0;
                s_P32[radius_no] = 0;
                a_P33[radius_no] = 0;
                s_P33[radius_no] = 0;
                //a_P31[radius_no] = 0;
                //s_P31[radius_no] = 0;
                //a_P34[radius_no] = 0;
                //s_P34[radius_no] = 0;
                //a_P35[radius_no] = 0;
                //s_P35[radius_no] = 0;
            }
        }

        /// <summary>
        /// Default Constructor: initial state has no fractures and empty arrays for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        public MicrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in) : this(gbc_in, fds_in, new double[0])
        {
            // Reference to an array of microfracture radii: Create a new empty array
        }

        /// <summary>
        /// Constructor: Supply an external array for the halflength values for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        /// <param name="radii_in">Array of microfracture radii to use as index values for the piecewise population distribution functions</param>
        public MicrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in, double[] radii_in)
        {
            // Reference to parent FractureDipSet object
            fds = fds_in;
            // Reference to great-grandparent GridblockConfiguration object 
            gbc = gbc_in;

            // Set total fracture population values to zero
            a_P30_total = 0;
            s_P30_total = 0;
            //a_P31_total = 0;
            //s_P31_total = 0;
            a_P32_total = 0;
            s_P32_total = 0;
            a_P33_total = 0;
            s_P33_total = 0;
            //a_P34_total = 0;
            //s_P34_total = 0;
            a_P35_total = 0;
            s_P35_total = 0;

            // Reset the arrays for the piecewise population distribution functions based on a supplied array of index radii
            ResetPopulationDistributionData(radii_in);
        }

    }
}
