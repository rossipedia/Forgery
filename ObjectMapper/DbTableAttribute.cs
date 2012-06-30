// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbTableAttribute.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbTableAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// Specifies information about the database table
    /// used to store objects of this type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class DbTableAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets Name of the table.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Prefix for the table.
        /// </summary>
        public string Prefix { get; set; }
    }
}