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
        public MetadataValidationException(string message) : base(message) { }
        public MetadataValidationException(string message, Exception innerException) : base(message, innerException) { }
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
    // ReSharper disable MemberCanBePrivate.Global

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

    // ReSharper restore MemberCanBePrivate.Global
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
            var cmd = conn.CreateCommand(CommandType.Text, ObjectMapper<T>.InsertStatement);
            cmd.SetMappedInsertParameters(obj ?? new T());
            return cmd;
        }

        public static IDbCommand CreateMappedUpdateCommand<T>(this IDbConnection conn, T obj = null) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            var cmd = conn.CreateCommand(CommandType.Text, ObjectMapper<T>.UpdateStatement);
            cmd.SetMappedUpdateParameters(obj ?? new T());
            return cmd;
        }

        public static IDbCommand CreateMappedDeleteCommand<T>(this IDbConnection conn, T obj = null) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            var cmd = conn.CreateCommand(CommandType.Text, ObjectMapper<T>.DeleteStatement);
            cmd.SetMappedDeleteParameters(obj ?? new T());
            return cmd;
        }

        public static IDbCommand CreateMappedSelectCommand<T>(this IDbConnection conn) where T : class, new()
        {
            return conn.CreateCommand(CommandType.Text, ObjectMapper<T>.SelectStatement);
        }

        public static IDbCommand CreateMappedSelectCommand<T>(this IDbConnection conn, string criteria, params object[] parameterValues) where T : class, new()
        {
            var cmd = CreateMappedSelectCommand<T>(conn);
            if (criteria == null) throw new ArgumentNullException("criteria");
            cmd.CommandText += criteria;
            cmd.AddIndexedParameters(parameterValues);
            return cmd;
        }

        public static IDbCommand CreateCommand(this IDbConnection conn, CommandType commandType, string commandText)
        {
            if (conn == null) throw new ArgumentNullException("conn");
            if (string.IsNullOrWhiteSpace(commandText)) throw new ArgumentException("commandText");
            var cmd = conn.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            return cmd;
        }

        public static int ExecuteNonQueryText(this IDbConnection conn, string commandText, params object[] parameterValues)
        {
            var cmd = conn.CreateCommand(CommandType.Text, commandText);
            cmd.AddIndexedParameters(parameterValues);
            return cmd.ExecuteNonQuery();
        }

        public static IDataReader ExecuteReaderText(this IDbConnection conn, string commandText, params object[] parameterValues)
        {
            var cmd = conn.CreateCommand(CommandType.Text, commandText);
            cmd.AddIndexedParameters(parameterValues);
            return cmd.ExecuteReader();
        }

        //public static object ExecuteScalar(this IDbCommand cmd, CommandType type, string commandText, params object[] parameterValues)
        //{
        //    //cmd.SetCommandTypeTextAndIndexedParameters(type, commandText, parameterValues);
        //    var cmd = 
        //    return cmd.ExecuteScalar();
        //}

        public static int InsertMappedObject<T>(this IDbConnection conn, T obj) where T : class, new()
        {
            if (conn == null) throw new ArgumentNullException("conn");
            if (obj == null) throw new ArgumentNullException("obj");
            var cmd = conn.CreateMappedInsertCommand(obj);
            return ObjectMapper<T>.InsertMappedObject(cmd, obj);
        }

        #region Internal - Might go public later
        internal static void AddIndexedParameters(this IDbCommand cmd, params object[] parameterValues)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            if (parameterValues == null)
                return;

            for (var i = 0; i < parameterValues.Length; ++i)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@" + i;
                p.Value = Helpers.GetDbValue(parameterValues[i]);
                cmd.Parameters.Add(p);
            }
        }
        #endregion
    }
    // ReSharper restore MemberCanBePrivate.Global
    #endregion

    #region Internal Implementation

    internal static class ObjectMapper<T> where T : class, new()
    {
        #region Mapping function

        static Func<IDataRecord, T> _mapFunc;
        public static Func<IDataRecord, T> MapObject { get { return _mapFunc ?? (_mapFunc = BuildMapper()); } }
        
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

        #region Dynamic Method Generation using Expressions
        
        static IEnumerable<Expression> GetPropertySetterExprs(Type entityType, Expression resultExpr, Expression readerExpr)
        {
            return entityType.GetProperties().Where(CanMapProperty).Select(p => GetPropertySetterExpr(p, resultExpr, readerExpr));
        }

        static bool CanMapProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.CanWrite && !Attribute.IsDefined(propertyInfo, typeof(DbIgnoreAttribute));
        }

        static Expression GetPropertySetterExpr(PropertyInfo property, Expression resultExpr, Expression readerExpr)
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

        static Expression CreateReaderGetValueExpr(PropertyInfo property, Expression readerExpr)
        {
            var propertyName = Expression.Constant(property.Name);
            var ordinalCallExpr = Expression.Call(readerExpr, ReaderGetOrdinalMethod, propertyName);
            var getValueExpr = Expression.Property(readerExpr, ReaderIndexByStringProperty, propertyName);

            return EnumParseReaderValueExpression(property, getValueExpr)
                ?? ReaderGetSpecificTypeExpression(property, readerExpr, ordinalCallExpr)
                ?? Expression.Convert(getValueExpr, property.PropertyType);
        }

        static Expression ReaderGetSpecificTypeExpression(PropertyInfo property, Expression readerExpr, Expression ordinalCallExpr)
        {
            var methodName = "Get" + property.PropertyType.Name; // GetString, GetInt32, etc...
            var fastMethod = typeof(IDataRecord).GetMethod(methodName, new[] { typeof(int) });
            return fastMethod != null ? Expression.Call(readerExpr, fastMethod, ordinalCallExpr) : null;
        }

        static Expression EnumParseReaderValueExpression(PropertyInfo property, Expression getValueExpr)
        {
            return property.PropertyType.IsEnum
                 ? Expression.Convert(
                    Expression.Call(
                        EnumParseMethod,
                            Expression.Constant(property.PropertyType),
                            Expression.Call(getValueExpr, ToStringMethod),
                            Expression.Constant(true)
                        ),
                        property.PropertyType
                    )
                 : null;
        }

        static Expression WrappedReaderExpr(string propertyName, Expression inner)
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

        #endregion

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
                string.Join(", ", ColumnsToEqualSql(updateColumns)),
                " WHERE ",
                WhereConditionByKey,
                ";"
            );
        }

        static string _whereConditionByKey;
        static string WhereConditionByKey
        {
            get
            {
                return _whereConditionByKey ?? (_whereConditionByKey = string.Join(" AND ", ColumnsToEqualSql(KeyColumns)));
            }
        }

        static IEnumerable<Metadata.Column> KeyColumns
        {
            get { return Metadata.Columns.Where(c => c.IsKey); }
        }

        static string _selectColumnsList;
        static string SelectColumnsList
        {
            get
            {
                return _selectColumnsList ?? (_selectColumnsList = string.Join(", ", Metadata.Columns.Select(c => c.Name)));
            }
        }

        static IEnumerable<string> ColumnsToEqualSql(IEnumerable<Metadata.Column> cols)
        {
            return cols.Select(c => c.Name + " = " + c.ParamName);
        }

        private static string CreateDeleteStatement()
        {
            return string.Format("DELETE FROM {0} WHERE {1};", Metadata.TableName, WhereConditionByKey);
        }

        private static string CreateSelectStatement()
        {
            return string.Format("SELECT {0} FROM {1} ", SelectColumnsList, Metadata.TableName);
        }
        #endregion

        #region Insert Parameters
        private static Action<IDbCommand, T> _setInsertParamsFunc;
        internal static Action<IDbCommand, T> SetInsertParameters
        {
            get 
            { 
                return _setInsertParamsFunc 
                ?? (
                    _setInsertParamsFunc = 
                    CreateSetParametersFunc(DbOperationType.Insert, c => c.IsInsertableParam)
                ); 
            }
        }
        #endregion

        #region Insert Mapped Object
        private static Func<IDbCommand, T, int> _insertMappedObject;
        internal static Func<IDbCommand, T, int> InsertMappedObject
        {
            get
            {
                return _insertMappedObject 
                    ?? (_insertMappedObject = CreateInsertMappedObjectFunc());
            }
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

        static Action<IDbCommand, T> CreateSetParametersFunc(DbOperationType operation, Func<Metadata.Column, bool> colPredicate = null)
        {
            var cmdExpr = Expression.Parameter(typeof(IDbCommand), "cmd");
            var objExpr = Expression.Parameter(typeof(T), "obj");
            var whichColumns = Metadata.Columns.AsEnumerable();

            if (colPredicate != null)
                whichColumns = whichColumns.Where(colPredicate);

            var blockExprs = whichColumns.Select(column => ColumnToParamExpression(cmdExpr, operation, column, objExpr));
            var bodyExpr = Expression.Block(blockExprs);
            return Expression.Lambda<Action<IDbCommand, T>>(bodyExpr, cmdExpr, objExpr).Compile();
        }

        static MethodCallExpression ColumnToParamExpression(Expression cmdExpr, DbOperationType operation, Metadata.Column column, Expression objExpr)
        {
            Expression valueExpr;
            var isInsert = operation == DbOperationType.Insert;
            var isUpdate = operation == DbOperationType.Update;
            var useCurrentTimestamp = isInsert && column.IsCreatedTimestamp || (isUpdate || isInsert) && column.IsModifiedTimestamp;

            if (useCurrentTimestamp)
                // [value] = DateTime.Now
                valueExpr = Expression.Property(null, DateTimeNowProperty);
            else if (column.IsEnum && column.EnumSaveType == EnumSaveType.String)
                // [value] = obj.Property.ToString()
                valueExpr = Expression.Call(Expression.Property(objExpr, column.Property), ToStringMethod);
            else
                // [value] = obj.Property
                valueExpr = Expression.Property(objExpr, column.Property);

            return Expression.Call(
                // Helpers.SetCommandParameterValue(cmd, @columnName, (object), [value]);
                SetCommandParameterValueMethod, cmdExpr, Expression.Constant("@" + column.Name), Expression.Convert(valueExpr, typeof(object))
            );
        }

        static Func<IDbCommand, T, int> CreateInsertMappedObjectFunc()
        {
            var cmdExpr = Expression.Parameter(typeof(IDbCommand), "cmd");
            var objExpr = Expression.Parameter(typeof(T), "obj");

            var bodyExpr = Metadata.IdentityColumn != null
                         ? ExecuteScalarExpression(objExpr, cmdExpr)
                         : ExecuteNonQueryExpression(cmdExpr);

            return Expression.Lambda<Func<IDbCommand, T, int>>(bodyExpr, cmdExpr, objExpr).Compile();
        }
        static MethodCallExpression ExecuteNonQueryExpression(ParameterExpression cmdExpr)
        {
            return Expression.Call(cmdExpr, ExecuteNonQueryMethod);
        }
        static Expression ExecuteScalarExpression(Expression objExpr, Expression cmdExpr)
        {
            return Expression.Assign(
                Expression.Property(objExpr, Metadata.IdentityColumn.Property),
                Expression.Convert(
                    Expression.Call(cmdExpr, ExecuteScalarMethod),
                    Metadata.IdentityColumn.Property.PropertyType
                    )
                );
        }

        #endregion
    }

    #region Helper Methods
    internal static class Helpers
    {
        // ReSharper disable UnusedMember.Global
        // ReSharper disable MemberCanBePrivate.Global
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
                ((IDataParameter) cmd.Parameters[name]).Value = value;
            else
                cmd.Parameters.Add(CreateParameterWithValue(cmd, name, value));
        }

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
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore UnusedMember.Global
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

