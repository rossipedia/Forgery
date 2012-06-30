// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbKeyAttribute.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbKeyAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// Marks a column as belonging to the primary key of the table
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbKeyAttribute : Attribute
    {
    }
}