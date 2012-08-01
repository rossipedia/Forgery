// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IExpressionBuilder.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the IObjectBuilder type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper.Builders
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Linq.Expressions;

    using ObjectMapper.ColumnStrategies;
    using ObjectMapper.FieldStrategies;

    public interface IExpressionBuilder
    {
        Func<IDataRecord, T> BuildExpression<T>();
    }

    public class ConstructorExpressionBuilder : IExpressionBuilder
    {
        private readonly IConstructorResolutionStrategy constructorResolutionStrategy;

        private readonly IFieldResolutionStrategy fieldResolutionStrategy;

        public ConstructorExpressionBuilder(IConstructorResolutionStrategy constructorResolutionStrategy)
        {
            this.constructorResolutionStrategy = constructorResolutionStrategy;
            this.fieldResolutionStrategy = new ConstructorParametersStrategy(constructorResolutionStrategy);
        }

        public Func<IDataRecord, T> BuildExpression<T>()
        {
            var constructor = this.constructorResolutionStrategy.SelectConstructor(typeof(T));
            var record = Expression.Parameter(typeof(IDataRecord), "record");
            
            var args = from field in this.fieldResolutionStrategy.EnumerateFields(typeof(T))
                       select GetFieldExpr(record, field);

            return Expression.Lambda<Func<IDataRecord, T>>(Expression.New(constructor, args)).Compile();
        }

        private static Expression GetFieldExpr(ParameterExpression record, DataField field)
        {
            Func<IDataRecord, object> getField = rec => rec[field.FieldName];
            return Expression.Convert(Expression.Invoke(Expression.Constant(getField), record), field.FieldType);
        }
    }

    public class InitializerExpressionBuilder : IExpressionBuilder
    {
        private IFieldResolutionStrategy fieldResolutionStrategy;

        private IConstructorResolutionStrategy constructorResolutionStrategy;

        public InitializerExpressionBuilder()
        {
            this.constructorResolutionStrategy = new DefaultConstructorResolutionStrategy();
            this.fieldResolutionStrategy = new PropertiesStrategy();
        }

        public Func<IDataRecord, T> BuildExpression<T>()
        {
            
            var record = Expression.Parameter(typeof(IDataRecord), "record");
            var bindings = from field in this.fieldResolutionStrategy.EnumerateFields(typeof(T))
                           select GetBindingExpr(typeof(T), record, field);

            var constructor = this.constructorResolutionStrategy.SelectConstructor(typeof(T));
            var newExpr = Expression.New(constructor);
            return Expression.Lambda<Func<IDataRecord, T>>(Expression.MemberInit(newExpr, bindings)).Compile();
        }

        private static MemberBinding GetBindingExpr(Type type, ParameterExpression record, DataField field)
        {
            var member = type.GetProperty(field.FieldName, field.FieldType);
            Func<IDataRecord, object> getField = rec => rec[field.FieldName];
            var getFieldExpr = Expression.Convert(Expression.Invoke(Expression.Constant(getField), record), field.FieldType);
            return Expression.Bind(member, getFieldExpr);
        }
    }
}
