// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbIgnoreAttribute.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbIgnoreAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.Attributes
{
    using System;

    /// <summary>
    /// Indicates that the ObjectMapper should
    /// ignore this property when resolving to database columns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbIgnoreAttribute : Attribute
    {
    }
}