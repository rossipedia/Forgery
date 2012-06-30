// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumSaveType.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the EnumSaveType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    /// <summary>
    /// How to save enums in the database
    /// </summary>
    public enum EnumSaveType
    {
        /// <summary>
        /// Indicates that the numeric value of the enum
        /// should be saved in the database.
        /// </summary>
        Numeric,
        
        /// <summary>
        /// Indicates that the string representation of the enum
        /// should be saved in the database
        /// </summary>
        String
    }
}