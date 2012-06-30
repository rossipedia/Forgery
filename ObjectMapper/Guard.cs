// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Guard.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the Guard type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// A static utility class for preconditions.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Checks an argument for null.
        /// </summary>
        /// <typeparam name="T">The type of the object to check. Must be a reference type.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="name">The name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is <c>null</c></exception>
        public static void ArgumentNotNull(object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
