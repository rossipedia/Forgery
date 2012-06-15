using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectMapper
{
    #region Exceptions
    public class MetadataValidationException : InvalidOperationException
    {
        public MetadataValidationException(string message) : base(message) {}
        public MetadataValidationException(string message, Exception innerException) : base(message, innerException) {}
    }
    #endregion

    #region Enums
    internal enum DbOperationType
    {
        Insert,
        Update,
        Delete
    }

    public enum EnumSaveType
    {
        Numeric,
        String
    }
    #endregion

    #region Attributes

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class DbTableAttribute : Attribute
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DbIdentityAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class DbKeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class DbIgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Assembly)]
    public class DbEnumAttribute : Attribute
    {
        public EnumSaveType SaveType { get; set; }
        public DbEnumAttribute(EnumSaveType saveType) { SaveType = saveType; }
    }

    // Convenience attributes
    [AttributeUsage(AttributeTargets.Property)]
    public class DbModifiedTimestampAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class DbCreatedTimestampAttribute : Attribute { }

    #endregion

    #region Public API
    // ReSharper disable MemberCanBePrivate.Global
    public static class Extensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            while (reader.Read()) yield return reader;
        }

        public static T MapObject<T>(this IDataRecord record) where T : class, new()
        {
            if (record == null) throw new ArgumentNullException("record");
            return ObjectMapper<T>.MapObject(record);
        }

        public static IEnumerable<T> MapToEnumerable<T>(this IDataReader reader) where T : class, new()
        {
            if (reader == null) throw new ArgumentNullException("reader");
            return reader.AsEnumerable().Select(r => r.MapObject<T>());
        }

        public static void SetMappedInsertParameters<T>(this IDbCommand cmd, T obj) where T : class, new()
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            if (obj == null) throw new ArgumentNullException("obj");
            ObjectMapper<T>.SetInsertParameters(cmd, obj);
        }

        public static void SetMappedUpdateParameters<T>(this IDbCommand cmd, T obj) where T : class, new()
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            if (obj == null) throw new ArgumentNullException("obj");
            ObjectMapper<T>.SetUpdateParameters(cmd, obj);
        }

        public static void SetMappedDeleteParameters<T>(this IDbCommand cmd, T obj) where T : class, new()
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            if (obj == null) throw new ArgumentNullException("obj");
            ObjectMapper<T>.SetDeleteParameters(cmd, obj);
        }

        public static IDbCommand CreateMappedInsertCommand<T>(this IDbConnection conn, T obj = null) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = ObjectMapper<T>.InsertStatement;
            cmd.SetMappedInsertParameters(obj ?? new T());
            return cmd;
        }

        public static IDbCommand CreateMappedUpdateCommand<T>(this IDbConnection conn, T obj = null) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = ObjectMapper<T>.UpdateStatement;
            cmd.SetMappedUpdateParameters(obj ?? new T());
            return cmd;
        }

        public static IDbCommand CreateMappedDeleteCommand<T>(this IDbConnection conn, T obj = null) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = ObjectMapper<T>.DeleteStatement;
            cmd.SetMappedDeleteParameters(obj ?? new T());
            return cmd;
        }

        public static IDbCommand CreateMappedSelectCommand<T>(this IDbConnection conn) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            var cmd = conn.CreateCommand();
            cmd.CommandText = ObjectMapper<T>.SelectStatement;
            cmd.CommandType = CommandType.Text;
            return cmd;
        }

        public static IDbCommand CreateMappedSelectCommand<T>(this IDbConnection conn, string criteria, params object[] parameterValues) where T : class, new()
        {
            if (criteria == null) throw new ArgumentNullException("criteria");
            var cmd = conn.CreateMappedSelectCommand<T>();

            cmd.CommandText += criteria;
            if (parameterValues != null)
            {
                for (var i = 0; i < parameterValues.Length; ++i)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@" + i;
                    p.Value = Helpers.GetDbValue(parameterValues[i]);
                    cmd.Parameters.Add(p);
                }
            }
            return cmd;
        }

        public static void InsertMappedObject<T>(this IDbConnection conn, T obj) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            if (obj == null) throw new ArgumentNullException("obj");
            var cmd = conn.CreateMappedInsertCommand(obj);
            ObjectMapper<T>.InsertMappedObject(cmd, obj);
        }
    }
    // ReSharper restore MemberCanBePrivate.Global
    #endregion

    #region Internal Implementation

    internal static class ObjectMapper<T> where T : class, new()
    {
        #region Mapping function

        static Func<IDataRecord, T> _mapFunc;

        public static T MapObject(IDataRecord reader)
        {
            if (_mapFunc == null) _mapFunc = BuildMapper();
            return _mapFunc(reader);
        }

        static Func<IDataRecord, T> BuildMapper()
        {
            var entityType = typeof(T);
            var record = Expression.Parameter(typeof(IDataRecord), "record");

            // Ensure record isn't null
            var ifNull = Expression.IfThen(
                Expression.Equal(record, Expression.Constant(null)),
                // ReSharper disable NotResolvedInText
                Expression.Throw(Expression.Constant(new ArgumentNullException("record")))
                // ReSharper restore NotResolvedInText
            );

            var result = Expression.Variable(entityType, "result");

            var newEntity = Expression.Assign(result, Expression.New(entityType));
            var propertyAssignExprs = GetPropertySetterExprs(entityType, result, record);

            var blockExprs = new List<Expression> { ifNull, newEntity };
            blockExprs.AddRange(propertyAssignExprs);
            blockExprs.Add(result);

            var body = Expression.Block(new[] { result }, blockExprs);
            return Expression.Lambda<Func<IDataRecord, T>>(body, record).Compile();
        }

        static IEnumerable<Expression> GetPropertySetterExprs(Type entityType, ParameterExpression resultExpr, ParameterExpression readerExpr)
        {
            return entityType.GetProperties().Where(CanMapProperty).Select(p => GetPropertySetterExpr(p, resultExpr, readerExpr));
        }

        private static bool CanMapProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.CanWrite && !Attribute.IsDefined(propertyInfo, typeof(DbIgnoreAttribute));
        }

        static Expression GetPropertySetterExpr(PropertyInfo property, ParameterExpression resultExpr, ParameterExpression readerExpr)
        {
            return
                WrappedReaderExpr(
                    property.Name,
                    Expression.Assign(
                        Expression.Property(resultExpr, property),
                        CreateReaderGetValueExpr(property, readerExpr)
                    )
                );
        }

        static Expression CreateReaderGetValueExpr(PropertyInfo property, ParameterExpression readerExpr)
        {
            var propertyName = Expression.Constant(property.Name);
            var ordinalCallExpr = Expression.Call(readerExpr, ReaderGetOrdinalMethod, propertyName);
            var getValueExpr = Expression.Property(readerExpr, ReaderIndexByStringProperty, propertyName);

            // Check if property is enum first!
            if (property.PropertyType.IsEnum)
            {
                // (PropertyType)Enum.Parse(PropertyType, reader[PropertyName].ToString())
                return Expression.Convert(
                    Expression.Call(
                        EnumParseMethod,
                        Expression.Constant(property.PropertyType),
                        Expression.Call(getValueExpr, ToStringMethod),
                        Expression.Constant(true)
                    ),
                    property.PropertyType
                    );
            }

            // Check for fast method
            var methodName = "Get" + property.PropertyType.Name; // GetString, GetInt32, etc...
            var fastMethod = typeof(IDataRecord).GetMethod(methodName, new[] { typeof(int) });
            if (fastMethod != null)
            {
                // No cast necessary
                return Expression.Call(readerExpr, fastMethod, ordinalCallExpr);
            }
            return Expression.Convert(getValueExpr, property.PropertyType);
        }

        private static Expression WrappedReaderExpr(string propertyName, Expression inner)
        {
            var msg = Expression.Constant(string.Format("The field \"{0}\" was not found in the reader", propertyName));
            var ex = Expression.Parameter(typeof(IndexOutOfRangeException), "ex");
            var newEx = Expression.New(IndexOutOfRangeCtor, msg, ex);
            var thrower = Expression.Block(
                Expression.Throw(newEx),
                Expression.Default(inner.Type)
            );
            var catchExpr = Expression.Catch(ex, thrower);
            return Expression.TryCatch(inner, catchExpr);
        }

        #region Method Infos
        static readonly MethodInfo ReaderGetOrdinalMethod = typeof(IDataRecord).GetMethod("GetOrdinal", new[] { typeof(string) });
        static readonly PropertyInfo ReaderIndexByStringProperty = typeof(IDataRecord).GetProperty("Item", new[] { typeof(string) });
        static readonly PropertyInfo DateTimeNowProperty = typeof(DateTime).GetProperty("Now", BindingFlags.Public | BindingFlags.Static);
        static readonly MethodInfo ExecuteScalarMethod = typeof(IDbCommand).GetMethod("ExecuteScalar");
        static readonly MethodInfo ExecuteNonQueryMethod = typeof(IDbCommand).GetMethod("ExecuteNonQuery");
        static readonly MethodInfo EnumParseMethod = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) });
        static readonly MethodInfo ToStringMethod = typeof(object).GetMethod("ToString");
        static readonly ConstructorInfo IndexOutOfRangeCtor = typeof(IndexOutOfRangeException).GetConstructor(new[] { typeof(string), typeof(Exception) });
        static readonly MethodInfo SetCommandParameterValueMethod = typeof(Helpers).GetMethod("SetCommandParameterValue", BindingFlags.Static | BindingFlags.Public);
        #endregion

        #endregion

        #region Metadata

        private static Metadata _metadata;

        internal static Metadata Metadata
        {
            get { return _metadata ?? (_metadata = new Metadata(typeof(T))); }
        }

        #endregion

        #region SQL Generation
        private static string _insertStatement;
        internal static string InsertStatement
        {
            get { return _insertStatement ?? (_insertStatement = CreateInsertStatement()); }
        }

        private static string _updateStatement;
        public static string UpdateStatement
        {
            get { return _updateStatement ?? (_updateStatement = CreateUpdateStatement()); }
        }


        private static string _deleteStatement;
        public static string DeleteStatement
        {
            get { return _deleteStatement ?? (_deleteStatement = CreateDeleteStatement()); }

        }

        private static string _selectStatement;
        public static string SelectStatement
        {
            get { return _selectStatement ?? (_selectStatement = CreateSelectStatement()); }
        }

        private static string CreateInsertStatement()
        {
            // Get names of columns for insert
            var insertColumns = Metadata.Columns.Where(c => c.IsInsertable).ToList();

            return string.Concat(
                "INSERT INTO ",
                Metadata.TableName,
                " (",
                    string.Join(", ", insertColumns.Select(c => c.Name)),
                ") VALUES (",
                    string.Join(", ", insertColumns.Select(c => c.ParamName)),
                "); ",
                Metadata.IdentityColumn != null
                    ? "SELECT SCOPE_IDENTITY() as " + Metadata.IdentityColumn.Name + ";"
                    : string.Empty
            );
        }
        private static string CreateUpdateStatement()
        {
            var updateColumns = Metadata.Columns.Where(c => c.IsUpdatable).ToList();
            if (!updateColumns.Any())
                throw new MetadataValidationException("Cannot update. No non-key columns found for type " + typeof(T).FullName);

            return string.Concat(
                "UPDATE ",
                Metadata.TableName,
                " SET ",
                string.Join(", ", updateColumns.Select(c => c.Name + " = " + c.ParamName)),
                " WHERE ",
                string.Join(" AND ", Metadata.Columns.Where(c => c.IsKey).Select(c => c.Name + " = " + c.ParamName)),
                ";"
            );
        }
        private static string CreateDeleteStatement()
        {
            return string.Concat(
                "DELETE FROM ",
                Metadata.TableName,
                " WHERE ",
                string.Join(" AND ", Metadata.Columns.Where(c => c.IsKey).Select(c => c.Name + " = " + c.ParamName)),
                ";"
            );
        }
        private static string CreateSelectStatement()
        {
            return string.Concat(
                "SELECT ",
                string.Join(", ", Metadata.Columns.Select(c => c.Name)),
                " FROM ",
                Metadata.TableName,
                " "
            );
        }
        #endregion

        #region Insert Parameters
        private static Action<IDbCommand, T> _setInsertParamsFunc;
        internal static Action<IDbCommand, T> SetInsertParameters
        {
            get { return _setInsertParamsFunc ?? (_setInsertParamsFunc = CreateSetParametersFunc(DbOperationType.Insert, c => c.IsInsertableParam)); }
        }
        #endregion

        #region Insert Mapped Object
        private static Action<IDbCommand, T> _insertMappedObject;
        internal static Action<IDbCommand, T> InsertMappedObject
        {
            get { return _insertMappedObject ?? (_insertMappedObject = CreateInsertMappedObjectFunc()); }
        }
        #endregion

        #region Update Parameters
        private static Action<IDbCommand, T> _setUpdateParamsFunc;
        internal static Action<IDbCommand, T> SetUpdateParameters
        {
            get { return _setUpdateParamsFunc ?? (_setUpdateParamsFunc = CreateSetParametersFunc(DbOperationType.Update, c => c.IsUpdatableParam)); }
        }
        #endregion

        #region Delete Parameters
        private static Action<IDbCommand, T> _deleteParamsFunc;
        public static Action<IDbCommand, T> SetDeleteParameters
        {
            get { return _deleteParamsFunc ?? (_deleteParamsFunc = CreateSetParametersFunc(DbOperationType.Delete, c => c.IsKey)); }
        }
        #endregion

        #region On-Demand Method Generation
        private static Action<IDbCommand, T> CreateSetParametersFunc(DbOperationType operation, Func<Metadata.Column, bool> colPredicate = null)
        {
            var cmdExpr = Expression.Parameter(typeof(IDbCommand), "cmd");
            var objExpr = Expression.Parameter(typeof(T), "obj");
            var whichColumns = Metadata.Columns.AsEnumerable();

            if (colPredicate != null)
                whichColumns = whichColumns.Where(colPredicate);

            var blockExprs = whichColumns.Select(
                column => ColumnToParamExpression(cmdExpr, operation, column, objExpr)
            );

            var bodyExpr = Expression.Block(blockExprs);

            return Expression.Lambda<Action<IDbCommand, T>>(bodyExpr, cmdExpr, objExpr).Compile();
        }
        private static MethodCallExpression ColumnToParamExpression(ParameterExpression cmdExpr, DbOperationType operation, Metadata.Column column, ParameterExpression objExpr)
        {
            Expression valueExpr;

            if (
                (operation == DbOperationType.Insert && column.IsCreatedTimestamp) ||
                ((operation == DbOperationType.Update || operation == DbOperationType.Insert) && column.IsModifiedTimestamp) // Insert Modified timestamp
                )
                valueExpr = Expression.Property(null, DateTimeNowProperty);
            else if (column.IsEnum && column.EnumSaveType == EnumSaveType.String)
                valueExpr = Expression.Call(Expression.Property(objExpr, column.Property), ToStringMethod);
            else
                valueExpr = Expression.Property(objExpr, column.Property);

            return Expression.Call(SetCommandParameterValueMethod,
                cmdExpr,
                Expression.Constant("@" + column.Name),
                Expression.Convert(
                    valueExpr,
                    typeof(object)
                )
            );
        }
        private static Action<IDbCommand, T> CreateInsertMappedObjectFunc()
        {
            var cmdExpr = Expression.Parameter(typeof(IDbCommand), "cmd");
            var objExpr = Expression.Parameter(typeof(T), "obj");
            var bodyExpr = Metadata.IdentityColumn != null
                ? (Expression)Expression.Assign(
                        Expression.Property(objExpr, Metadata.IdentityColumn.Property),
                        Expression.Convert(
                            Expression.Call(
                                cmdExpr,
                                ExecuteScalarMethod
                            ),
                            Metadata.IdentityColumn.Property.PropertyType
                        )
                    )
                : Expression.Call(
                        cmdExpr,
                        ExecuteNonQueryMethod
                    );

            return Expression.Lambda<Action<IDbCommand, T>>(bodyExpr, cmdExpr, objExpr).Compile();
        }
        #endregion
    }

    #region Helper Methods
    internal static class Helpers
    {
        // ReSharper disable UnusedMember.Global
        public static IDataParameter CreateParameterWithValue(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            return p;
        }

        public static void SetCommandParameterValue(IDbCommand cmd, string name, object value)
        {
            if (cmd.Parameters.Contains(name))
            {
                ((IDataParameter)cmd.Parameters[name]).Value = value;
            }
            else
            {
                cmd.Parameters.Add(CreateParameterWithValue(cmd, name, value));
            }
        }
        // ReSharper restore UnusedMember.Global
        public static object GetDbValue(object o)
        {
            if (o != null && o.GetType().IsEnum)
            {
                var enumAttr = Attribute.GetCustomAttribute(o.GetType(), typeof(DbEnumAttribute)) as DbEnumAttribute;
                enumAttr = enumAttr ?? Attribute.GetCustomAttribute(o.GetType().Assembly, typeof(DbEnumAttribute)) as DbEnumAttribute;
                if (enumAttr != null && enumAttr.SaveType == EnumSaveType.String)
                    o = o.ToString();
            }
            return o;
        }
    }
    #endregion

    #region Metadata Type
    public class Metadata
    {
        internal Metadata(Type type)
        {
            TableName = GetTableName(type);
            Columns = type.GetProperties().Select(PropertyToColumn).Where(c => c != null).ToList();
            try
            {
                IdentityColumn = Columns.SingleOrDefault(c => c.IsIdentity); // With throw if multiple identity 
            }
            catch (InvalidOperationException ex)
            {
                throw new MetadataValidationException("Multiple Identities found for type: " + type.FullName, ex);
            }
        }

        public string TableName { get; private set; }
        public Column IdentityColumn { get; private set; }
        public List<Column> Columns { get; private set; }

        public class Column
        {
            internal Column() { }
            public string Name { get; internal set; }
            public bool IsKey { get; internal set; }
            public bool IsIdentity { get; internal set; }
            public bool IsEnum { get; internal set; }
            public string ParamName { get; internal set; }
            public EnumSaveType EnumSaveType { get; internal set; }
            public PropertyInfo Property { get; internal set; }
            public bool IsCreatedTimestamp { get; internal set; }
            public bool IsModifiedTimestamp { get; internal set; }

            public bool IsUpdatable { get { return !(IsIdentity || IsKey || IsCreatedTimestamp); } }
            public bool IsUpdatableParam { get { return !IsCreatedTimestamp; } }
            public bool IsInsertable { get { return !IsIdentity; } }
            public bool IsInsertableParam { get { return !IsIdentity; } }
        }

        #region Helpers
        private static string GetTableName(Type type)
        {
            var typeTableAttr = Attribute.GetCustomAttribute(type, typeof(DbTableAttribute)) as DbTableAttribute;
            if (typeTableAttr != null)
            {
                if (!String.IsNullOrWhiteSpace(typeTableAttr.Name))
                    return typeTableAttr.Name;
                if (!String.IsNullOrWhiteSpace(typeTableAttr.Prefix))
                    return typeTableAttr.Prefix + type.Name;
            }

            var assmTableAttr = Attribute.GetCustomAttribute(type.Assembly, typeof(DbTableAttribute)) as DbTableAttribute;
            if (assmTableAttr != null && !String.IsNullOrWhiteSpace(assmTableAttr.Prefix))
                return assmTableAttr.Prefix + type.Name;

            return type.Name;
        }

        private static Column PropertyToColumn(PropertyInfo property)
        {
            if (Attribute.IsDefined(property, typeof(DbIgnoreAttribute)))
                return null;

            var col = new Column
            {
                Name = property.Name,
                IsKey = Attribute.IsDefined(property, typeof(DbKeyAttribute)),
                IsIdentity = Attribute.IsDefined(property, typeof(DbIdentityAttribute)),
                Property = property,
                ParamName = "@" + property.Name,
                IsEnum = property.PropertyType.IsEnum,
                IsCreatedTimestamp = Attribute.IsDefined(property, typeof(DbCreatedTimestampAttribute)),
                IsModifiedTimestamp = Attribute.IsDefined(property, typeof(DbModifiedTimestampAttribute))
            };

            if (col.IsEnum)
            {
                // Check for save type
                Debug.Assert(property.DeclaringType != null, "property.DeclaringType != null");

                var enumAttr = Attribute.GetCustomAttribute(property, typeof(DbEnumAttribute)) as DbEnumAttribute;
                enumAttr = enumAttr ?? Attribute.GetCustomAttribute(property.PropertyType, typeof(DbEnumAttribute)) as DbEnumAttribute;
                enumAttr = enumAttr ?? Attribute.GetCustomAttribute(property.PropertyType.Assembly, typeof(DbEnumAttribute)) as DbEnumAttribute;
                if (enumAttr != null) col.EnumSaveType = enumAttr.SaveType;
            }

            // Some sanity checking
            if (col.IsCreatedTimestamp && col.IsModifiedTimestamp)
                throw new MetadataValidationException("Property " + property.Name + " cannot be both modified and created timestamp");

            if ((col.IsCreatedTimestamp || col.IsModifiedTimestamp) && property.PropertyType != typeof(DateTime))
                throw new MetadataValidationException("Property " + property.Name + " is not of type DateTime");

            return col;
        }
        #endregion
    }
    #endregion

    #endregion
}
