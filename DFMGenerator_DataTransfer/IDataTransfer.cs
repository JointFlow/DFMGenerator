using System;
using System.Collections.Generic;
using System.Text;
using DFMGenerator_SharedCode;

namespace DFMGenerator_DataTransfer
{
    /// <summary>
    /// Interface for a shadow grid to transfer required data to and from a DFM Generator grid
    /// Must be able to read geometry and proeprty data from the shadow grid, and to create new properties and write property data to the shadow grid
    /// </summary>
    interface IDataTransfer
    {
        // Functions to read data from the shadow grid
        /// <summary>
        /// Get the coordinates of one of the cornerpoints of a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="corner">Corner of the cell for which coordinates are required</param>
        /// <returns>PointXYZ object representing the required cell corner</returns>
        PointXYZ GetCellCornerpoint(int cellI, int cellJ, int cellK, GridblockCornerpoint corner);
        /// <summary>
        /// Check to see if the face of a cell in the shadow grid is faulted
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to check</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to check</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to check</param>
        /// <param name="corner">Face of the cell to check</param>
        /// <returns>True if the specified cell face is faulted, otherwise false</returns>
        bool FaultedContact(int cellI, int cellJ, int cellK, GridDirection face);

        /// <summary>
        /// Get the value of a specified floating point property in a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="PropertyName">Name of the property required</param>
        /// <returns>Value of the property in the specified cell; if the cell or the property do not exist, returns NaN</returns>
        double GetFloatingPointPropertyValue(int cellI, int cellJ, int cellK, string PropertyName);
        /// <summary>
        /// Get the value of a specified integer property in a specified cell in the shadow grid
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to get data from</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to get data from</param>
        /// <param name="PropertyName">Name of the property required</param>
        /// <returns>Value of the property in the specified cell; if the cell or the property do not exist, returns the default null value</returns>
        int GetIntegerPropertyValue(int cellI, int cellJ, int cellK, string PropertyName);

        // Functions to write data to the shadow grid
        /// <summary>
        /// Create a new floating point property in the shadow grid and populate it with NaNs
        /// </summary>
        /// <param name="PropertyName">Name of the new property</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if a property already exists with the specified name</returns>
        int CreateFloatingPointProperty(string PropertyName);
        /// <summary>
        /// Create a new integer property in the shadow grid and populate it with default null values
        /// </summary>
        /// <param name="PropertyName">Name of the new property</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if a property already exists with the specified name</returns>
        int CreateIntegerProperty(string PropertyName);
        /// <summary>
        /// Set the value of a specified floating point property in a specified cell in the shadow grid to a specified value
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to write data to</param>
        /// <param name="PropertyName">Name of the property to write data to</param>
        /// <param name="value">Value of the property to write</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified cell does not exist, 2 if the specified property does not exist, 3 if the specified value is invalid</returns>
        int SetFloatingPointProperty(int cellI, int cellJ, int cellK, string PropertyName, double value);
        /// <summary>
        /// Set the value of a specified integer property in a specified cell in the shadow grid to a specified value
        /// </summary>
        /// <param name="cellI">I coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellJ">J coordinate of cell in the shadow grid to write data to</param>
        /// <param name="cellK">K coordinate of cell in the shadow grid to write data to</param>
        /// <param name="PropertyName">Name of the property to write data to</param>
        /// <param name="value">Value of the property to write</param>
        /// <returns>Return code: 0 if the operation was successful, 1 if the specified cell does not exist, 2 if the specified property does not exist, 3 if the specified value is invalid</returns>
        int SetIntegerProperty(int cellI, int cellJ, int cellK, string PropertyName, int value);
    }
}
