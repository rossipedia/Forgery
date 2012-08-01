// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MostSpecificConstructorResolutionStrategy.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the MostSpecificConstructorResolutionStrategy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.ColumnStrategies
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// The constructor resolution strategy.
    /// </summary>
    public class MostSpecificConstructorResolutionStrategy : IConstructorResolutionStrategy
    {
        /// <summary>
        /// The select constructor.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The System.Reflection.ConstructorInfo, or null if no contructors are defined.
        /// </returns>
        public ConstructorInfo SelectConstructor(Type type)
        {
            var constructors = from constructor in type.GetConstructors()
                               orderby constructor.GetParameters().Length descending
                               select constructor;

            try
            {
                return constructors.First();
            }
            catch (InvalidOperationException exception)
            {
                var msg = string.Format("The type {0} provides no public constructors.", type.FullName);
                throw new InvalidOperationException(msg, exception);
            }
        }
    }
}
