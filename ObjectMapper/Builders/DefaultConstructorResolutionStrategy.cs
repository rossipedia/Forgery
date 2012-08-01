namespace ObjectMapper.Builders
{
    using System;
    using System.Linq;
    using System.Reflection;

    using ObjectMapper.ColumnStrategies;

    public class DefaultConstructorResolutionStrategy : IConstructorResolutionStrategy
    {
        public ConstructorInfo SelectConstructor(Type type)
        {
            var constructors = from constructor in type.GetConstructors()
                               where constructor.GetParameters().Length == 0
                               select constructor;
            try
            {
                return constructors.Single();
            }
            catch (InvalidOperationException ex)
            {
                var msg = string.Format("The type {0} does not provide a default constructor.", type.FullName);
                throw new InvalidOperationException(msg, ex);
            }
        }
    }
}