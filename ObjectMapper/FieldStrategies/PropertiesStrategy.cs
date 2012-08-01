// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertiesStrategy.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the PropertiesStrategy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.FieldStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PropertiesStrategy : IFieldResolutionStrategy
    {
        public IEnumerable<DataField> EnumerateFields(Type type)
        {
            return from p in type.GetProperties() where p.CanWrite select new DataField(p.Name, p.PropertyType);
        }
    }
}
