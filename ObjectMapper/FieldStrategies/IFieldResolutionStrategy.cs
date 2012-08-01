// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFieldResolutionStrategy.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   The ColumnStrategy interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.FieldStrategies
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The ColumnStrategy interface.
    /// </summary>
    public interface IFieldResolutionStrategy
    {
        /// <summary>
        /// Enumerates the fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An IEnumerable of field names</returns>
        IEnumerable<DataField> EnumerateFields(Type type);
    }
}
