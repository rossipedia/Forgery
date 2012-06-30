// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbIdentityAttribute.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbIdentityAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// Marks the property as the Identity property for instances of the type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbIdentityAttribute : Attribute
    {
    }
}