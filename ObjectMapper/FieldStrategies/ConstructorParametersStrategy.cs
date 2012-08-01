// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConstructorParametersStrategy.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the ConstructorParametersStrategy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.ColumnStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ObjectMapper.FieldStrategies;

    /// <summary>
    /// The constructor parameters strategy.
    /// </summary>
    public class ConstructorParametersStrategy : IFieldResolutionStrategy
    {
        private readonly IConstructorResolutionStrategy resolutionStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorParametersStrategy" /> class.
        /// </summary>
        /// <param name="resolutionStrategy">The resolution strategy.</param>
        public ConstructorParametersStrategy(IConstructorResolutionStrategy resolutionStrategy)
        {
            this.resolutionStrategy = resolutionStrategy;
        }

        /// <summary>
        /// Enumerates the fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An IEnumerable of field names</returns>
        public IEnumerable<DataField> EnumerateFields(Type type)
        {
            var constructor = this.resolutionStrategy.SelectConstructor(type);
            return constructor.GetParameters().Select(p => new DataField(p.Name, p.ParameterType));
        }
    }
}
