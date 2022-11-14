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

        // Arrays for piecewise cumulative population distribution functions
        /// <summary>
        /// Index radii for cumulative population distribution function arrays
        /// </summary>
        public List<double> radii;
        /// <summary>
        /// Cumulative volumetric density distribution for active microfractures 
        /// </summary>
        public List<double> a_P30;
        /// <summary>
        /// Cumulative volumetric density distribution for static microfractures 
        /// </summary>
        public List<double> s_P30;
        /// <summary>
        /// Cumulative linear density distribution for active microfractures 
        /// </summary>
        public List<double> a_P32;
        /// <summary>
        /// Cumulative linear density distribution for static microfractures 
        /// </summary>
        public List<double> s_P32;
        /// <summary>
        /// Cumulative volumetric ratio distribution for active microfractures 
        /// </summary>
        public List<double> a_P33;
        /// <summary>
        /// Cumulative volumetric ratio distribution for static microfractures 
        /// </summary>
        public List<double> s_P33;

        // Displacement functions: These are included in the parent FractureDipSet object
        // Porosity / heave functions: These are included in the parent FractureDipSet object
        // Stress shadow width functions: These are included in the parent FractureDipSet object
        // Stress shadow volume functions: These are included in the parent FractureDipSet object

        /// <summary>
        /// Default Constructor: initial state has no fractures and empty arrays for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        public MicrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in) : this(gbc_in, fds_in, new List<double>())
        {
        }

        /// <summary>
        /// Constructor: Supply an external array for the halflength values for the piecewise cumulative population distribution functions
        /// </summary>
        /// <param name="gbc_in">Reference to great-grandparent GridblockConfiguration object</param>
        /// <param name="fds_in">Reference to parent FractureDipSet object</param>
        /// <param name="radii_in">Reference to an array of microfracture radii</param>
        public MicrofractureData(GridblockConfiguration gbc_in, FractureDipSet fds_in, List<double> radii_in)
        {
            // Reference to parent FractureDipSet object
            fds = fds_in;
            // Reference to great-grandparent GridblockConfiguration object 
            gbc = gbc_in;

            // Set total fracture population value to zero
            a_P30_total = 0;
            s_P30_total = 0;
            a_P32_total = 0;
            s_P32_total = 0;
            a_P33_total = 0;
            s_P33_total = 0;

            // Set the array of radii for the piecewise cumulative population distribution functions to point externally
            radii = radii_in;
            int NoValues = radii_in.Count;

            // Create arrays for piecewise cumulative population distribution functions and fill then with zero values
            a_P30 = new List<double>();
            s_P30 = new List<double>();
            a_P32 = new List<double>();
            s_P32 = new List<double>();
            a_P33 = new List<double>();
            s_P33 = new List<double>();
            for (int index = 0; index < NoValues; index++)
            {
                a_P30.Add(0d);
                s_P30.Add(0d);
                a_P32.Add(0d);
                s_P32.Add(0d);
                a_P33.Add(0d);
                s_P33.Add(0d);
            }
        }

    }
}
