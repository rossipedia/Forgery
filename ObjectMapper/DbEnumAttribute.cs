// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbEnumAttribute.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbEnumAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// Indicates how enumerations should be handle. May be applied to a property,
    /// an Enum type itself, or an entire assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Assembly)]
    public class DbEnumAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbEnumAttribute" /> class.
        /// </summary>
        /// <param name="saveType">Type of the save.</param>
        public DbEnumAttribute(EnumSaveType saveType)
        {
            this.SaveType = saveType;
        }

        /// <summary>
        /// Gets the <see cref="EnumSaveType"/>.
        /// </summary>
        /// <value>
        /// The <see cref="EnumSaveType"/>.
        /// </value>
        public EnumSaveType SaveType { get; private set; }
    }
}