// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DbConstructResolutionStrategy.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbConstructResolutionStrategy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.ColumnStrategies
{
    using System;
    using System.Linq;
    using System.Reflection;

    using ObjectMapper.Attributes;

    /// <summary>
    /// The db construct resolution strategy.
    /// </summary>
    public class DbConstructResolutionStrategy : IConstructorResolutionStrategy
    {
        /// <summary>
        /// The select constructor.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The System.Reflection.ConstructorInfo.
        /// </returns>
        public ConstructorInfo SelectConstructor(Type type)
        {
            var constructors = from constructor in type.GetConstructors()
                               where Attribute.IsDefined(constructor, typeof(DbConstructAttribute))
                               select constructor;

            try
            {
                return constructors.Single();
            }
            catch (InvalidOperationException ex)
            {
                const string Format = "The type {0} does not provide a constructor marked with DbConstructAttribute.";
                var msg = string.Format(Format, type.FullName);
                throw new InvalidOperationException(msg, ex);
            }
        }
    }
}
