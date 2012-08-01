// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConstructorResolutionStrategy.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   The ConstructorResolutionStrategy interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.ColumnStrategies
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The ConstructorResolutionStrategy interface.
    /// </summary>
    public interface IConstructorResolutionStrategy
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
        ConstructorInfo SelectConstructor(Type type);
    }
}