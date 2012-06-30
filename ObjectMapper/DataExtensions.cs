// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataExtensions.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the DbOperationType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
// ReSharper disable MemberCanBePrivate.Global

namespace ObjectMapper
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Exposes the ObjectMapper API as extension methods
    /// on various System.Data.Common interfaces.
    /// </summary>
    public static class DataExtensions
    {
        /// <summary>
        /// Converts the <see cref="IDataReader"/> to an <see cref="IEnumerable{IDataRecord}"/>
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>An <see cref="IEnumerable{IDataRecord}"/>.</returns>
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            Guard.ArgumentNotNull(reader, "reader");
            while (reader.Read())
            {
                yield return reader;
            }
        }

        /// <summary>
        /// Maps an object from an <see cref="IDataRecord"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to map</typeparam>
        /// <param name="record">The record.</param>
        /// <returns>An instance of <typeparamref name="T" /> loaded from <paramref name="record"/>.</returns>
        public static T MapObject<T>(this IDataRecord record) where T : class, new()
        {
            Guard.ArgumentNotNull(record, "record");
            return ObjectMapper<T>.MapObjectFromDataRecord(record);
        }

        /// <summary>
        /// Maps an <see cref="IDataReader"/> to a sequence of <typeparamref name="T"/> objects.
        /// </summary>
        /// <typeparam name="T">The type of instance to create</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>An <see cref="IEnumerable{T}"/></returns>
        public static IEnumerable<T> MapToEnumerable<T>(this IDataReader reader) where T : class, new()
        {
            Guard.ArgumentNotNull(reader, "reader");
            return reader.AsEnumerable().Select(r => r.MapObject<T>());
        }

        /// <summary>
        /// Sets parameters for an insert command from the given object
        /// </summary>
        /// <typeparam name="T">The type of object to read parameter values from</typeparam>
        /// <param name="cmd">The command.</param>
        /// <param name="obj">The object to load parameter values from.</param>
        public static void SetMappedInsertParameters<T>(this IDbCommand cmd, T obj) where T : class, new()
        {
            Guard.ArgumentNotNull(cmd, "cmd");
            Guard.ArgumentNotNull(obj, "obj");
            ObjectMapper<T>.SetInsertParameters(cmd, obj);
        }

        /// <summary>
        /// Sets parameters for an update command from the given object.
        /// </summary>
        /// <typeparam name="T">The type of object to read parameter values from</typeparam>
        /// <param name="cmd">The command.</param>
        /// <param name="obj">The object to load parameter values from.</param>
        public static void SetMappedUpdateParameters<T>(this IDbCommand cmd, T obj) where T : class, new()
        {
            Guard.ArgumentNotNull(cmd, "cmd");
            Guard.ArgumentNotNull(obj, "obj");
            ObjectMapper<T>.SetUpdateParameters(cmd, obj);
        }

        /// <summary>
        /// Sets parameters for a delete command from the given object
        /// </summary>
        /// <typeparam name="T">The type of object to read parameter values from</typeparam>
        /// <param name="cmd">The command.</param>
        /// <param name="obj">The object to load parameter values from.</param>
        public static void SetMappedDeleteParameters<T>(this IDbCommand cmd, T obj) where T : class, new()
        {
            Guard.ArgumentNotNull(cmd, "cmd");
            Guard.ArgumentNotNull(obj, "obj");
            ObjectMapper<T>.SetDeleteParameters(cmd, obj);
        }

        /// <summary>
        /// Creates an insert command and adds parameters from the given instance.
        /// </summary>
        /// <typeparam name="T">The type of object to read parameter values from</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="obj">The object to load parameter values from.</param>m>
        /// <returns>An <see cref="IDbCommand"/> that can be used to insert instances of <typeparamref name="T"/>.</returns>
        public static IDbCommand CreateMappedInsertCommand<T>(this IDbConnection connection, T obj = null) where T : class, new()
        {
            Guard.ArgumentNotNull(connection, "conn");
            var cmd = connection.CreateCommand(CommandType.Text, ObjectMapper<T>.InsertStatement);
            cmd.SetMappedInsertParameters(obj ?? new T());
            return cmd;
        }

        /// <summary>
        /// Creates an update command and adds parameters from the given instance.
        /// </summary>
        /// <typeparam name="T">The type of object to read parameter values from</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="obj">The object to load parameter values from.</param>m>
        /// <returns>An <see cref="IDbCommand"/> that can be used to update instances of <typeparamref name="T"/>.</returns>
        public static IDbCommand CreateMappedUpdateCommand<T>(this IDbConnection connection, T obj = null) where T : class, new()
        {
            Guard.ArgumentNotNull(connection, "conn");
            var cmd = connection.CreateCommand(CommandType.Text, ObjectMapper<T>.UpdateStatement);
            cmd.SetMappedUpdateParameters(obj ?? new T());
            return cmd;
        }

        /// <summary>
        /// Creates a delete command and adds parameters from the given instance
        /// </summary>
        /// <typeparam name="T">The type of object to read parameter values from</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="obj">The object private private to load parameter values from.</param>m>
        /// <returns>An <see cref="IDbCommand"/> that can be used to delete instances of <typeparamref name="T"/>.</returns>
        public static IDbCommand CreateMappedDeleteCommand<T>(this IDbConnection connection, T obj = null) where T : class, new()
        {
            Guard.ArgumentNotNull(connection, "conn");
            var cmd = connection.CreateCommand(CommandType.Text, ObjectMapper<T>.DeleteStatement);
            cmd.SetMappedDeleteParameters(obj ?? new T());
            return cmd;
        }

        /// <summary>
        /// Creates a select command that can be used to retreive instances of <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type of object to load</typeparam>
        /// <param name="connection">The connection.</param>
        /// <returns>An IDbCommand that can be used to read objects of <typeparamref name="T"/></returns>
        public static IDbCommand CreateMappedSelectCommand<T>(this IDbConnection connection) where T : class, new()
        {
            return connection.CreateCommand(CommandType.Text, ObjectMapper<T>.SelectStatement);
        }

        /// <summary>
        /// Creates a select command that can be used to retreive instances of <typeparamref name="T"/>
        /// with a specific criteria
        /// </summary>
        /// <typeparam name="T">The type of object to load</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="criteria">The criteria. This must be a WHERE clause suitable for restricting results from the database.</param>
        /// <param name="parameterValues">The parameter values. The values to supply to the command as parameters. These
        /// are zero indexed, and parameter names in the <paramref name="criteria"/> value must take the form of
        /// '@0', '@1', '@2', etc...</param>
        /// <returns>An IDbCommand that can be used to read objects of <typeparamref name="T"/></returns>
        public static IDbCommand CreateMappedSelectCommand<T>(this IDbConnection connection, string criteria, params object[] parameterValues) where T : class, new()
        {
            var cmd = CreateMappedSelectCommand<T>(connection);
            Guard.ArgumentNotNull(criteria, "criteria");
            cmd.CommandText += criteria;
            cmd.AddIndexedParameters(parameterValues);
            return cmd;
        }

        /// <summary>
        /// Simple helper method for creating a command and setting its CommandType and 
        /// CommandType all at once..
        /// </summary>
        /// <param name="connection">The conn.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="commandText">The command text.</param>
        /// <returns>An <see cref="IDbCommand"/></returns>
        /// <exception cref="System.ArgumentException"><paramref name="commandText"/> is null or empty.</exception>
        public static IDbCommand CreateCommand(this IDbConnection connection, CommandType commandType, string commandText)
        {
            Guard.ArgumentNotNull(connection, "conn");
            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException("commandText");
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            return cmd;
        }

        /// <summary>
        /// Executes a simple SQL query with optional parameters
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="parameterValues">The parameter values. The values to supply to the command as parameters. These
        /// are zero indexed, and parameter names in the <paramref name="commandText"/> value must take the form of
        /// '@0', '@1', '@2', etc...</param>
        /// <returns>The number of records affected</returns>
        public static int ExecuteNonQueryText(this IDbConnection connection, string commandText, params object[] parameterValues)
        {
            var cmd = connection.CreateCommand(CommandType.Text, commandText);
            cmd.AddIndexedParameters(parameterValues);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a simple SQL query with optional parameters and returns an IDataReader
        /// for reading the records returned
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="parameterValues">The parameter values. The values to supply to the command as parameters. These
        /// are zero indexed, and parameter names in the <paramref name="commandText"/> value must take the form of
        /// '@0', '@1', '@2', etc...</param>
        /// <returns>The number of records affected</returns>
        public static IDataReader ExecuteReaderText(this IDbConnection connection, string commandText, params object[] parameterValues)
        {
            var cmd = connection.CreateCommand(CommandType.Text, commandText);
            cmd.AddIndexedParameters(parameterValues);
            return cmd.ExecuteReader();
        }

        /// <summary>
        /// Inserts an object to the database.
        /// </summary>
        /// <typeparam name="T">The type of object to insert.</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="instance">The object to insert.</param>
        /// <returns>The number of records affected. Almost certainly a 1 on success, otherwise 0</returns>
        /// <remarks>
        /// After this method completes successfully, if a property on the type <typeparamref name="T"/>
        /// is marked with a <see cref="DbIdentityAttribute"/>, it will have been set to the new
        /// identity value for the type's table in the database.
        /// </remarks>
        public static int InsertMappedObject<T>(this IDbConnection connection, T instance) where T : class, new()
        {
            Guard.ArgumentNotNull(connection, "conn");
            Guard.ArgumentNotNull(instance, "obj");
            var cmd = connection.CreateMappedInsertCommand(instance);
            return ObjectMapper<T>.InsertObject(cmd, instance);
        }

        internal static void AddIndexedParameters(this IDbCommand cmd, params object[] parameterValues)
        {
            Guard.ArgumentNotNull(cmd, "cmd");
            if (parameterValues == null)
            {
                return;
            }

            for (var i = 0; i < parameterValues.Length; ++i)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@" + i;
                p.Value = Helpers.GetDbValue(parameterValues[i]);
                cmd.Parameters.Add(p);
            }
        }

        internal static class ObjectMapper<T> where T : class, new()
        {
            private static readonly MethodInfo ReaderGetOrdinalMethod = typeof(IDataRecord).GetMethod("GetOrdinal", new[] { typeof(string) });

            private static readonly PropertyInfo ReaderIndexByStringProperty = typeof(IDataRecord).GetProperty("Item", new[] { typeof(string) });

            private static readonly MethodInfo ExecuteScalarMethod = typeof(IDbCommand).GetMethod("ExecuteScalar");

            private static readonly MethodInfo ExecuteNonQueryMethod = typeof(IDbCommand).GetMethod("ExecuteNonQuery");

            private static readonly MethodInfo EnumParseMethod = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) });

            private static readonly MethodInfo ToStringMethod = typeof(object).GetMethod("ToString");

            private static readonly MethodInfo SetCommandParameterValueMethod = typeof(Helpers).GetMethod("SetCommandParameterValue", BindingFlags.Static | BindingFlags.Public);

            private static readonly MethodInfo GuardArgumentNotNullMethod = typeof(Guard).GetMethod("ArgumentNotNull", BindingFlags.Static | BindingFlags.Public);

            private static Lazy<Metadata> metadata = new Lazy<Metadata>(() => new Metadata(typeof(T)));

            private static Lazy<Func<IDataRecord, T>> mapFunc = new Lazy<Func<IDataRecord, T>>(BuildMapper);

            private static Lazy<string> insertStatement = new Lazy<string>(CreateInsertStatement);

            private static Lazy<string> updateStatement = new Lazy<string>(CreateUpdateStatement);

            private static Lazy<string> deleteStatement = new Lazy<string>(CreateDeleteStatement);

            private static Lazy<string> selectStatement = new Lazy<string>(CreateSelectStatement);

            private static Lazy<string> whereConditionByKey = new Lazy<string>(() => string.Join(" AND ", ColumnsToEqualSql(KeyColumns)));

            private static Lazy<string> selectColumnsList = new Lazy<string>(() => string.Join(", ", Metadata.Columns.Select(c => c.Name)));

            private static Lazy<Action<IDbCommand, T>> setInsertParamsFunc = new Lazy<Action<IDbCommand, T>>(() => CreateSetParametersFunc(c => c.IsInsertableParam));

            private static Lazy<Func<IDbCommand, T, int>> insertMappedObject = new Lazy<Func<IDbCommand, T, int>>(CreateInsertMappedObjectFunc);

            private static Lazy<Action<IDbCommand, T>> setUpdateParamsFunc = new Lazy<Action<IDbCommand, T>>(() => CreateSetParametersFunc(c => c.IsUpdatableParam));

            private static Lazy<Action<IDbCommand, T>> deleteParamsFunc = new Lazy<Action<IDbCommand, T>>(() => CreateSetParametersFunc(c => c.IsKey));

            public static string InsertStatement
            {
                get
                {
                    return insertStatement.Value;
                }
            }

            public static string UpdateStatement
            {
                get
                {
                    return updateStatement.Value;
                }
            }

            public static string DeleteStatement
            {
                get
                {
                    return deleteStatement.Value;
                }
            }

            public static string SelectStatement
            {
                get
                {
                    return selectStatement.Value;
                }
            }

            public static Func<IDataRecord, T> MapObjectFromDataRecord
            {
                get
                {
                    return mapFunc.Value;
                }
            }

            public static Action<IDbCommand, T> SetDeleteParameters
            {
                get
                {
                    return deleteParamsFunc.Value;
                }
            }

            internal static Action<IDbCommand, T> SetUpdateParameters
            {
                get
                {
                    return setUpdateParamsFunc.Value;
                }
            }

            internal static Metadata Metadata
            {
                get
                {
                    return metadata.Value;
                }
            }

            internal static Action<IDbCommand, T> SetInsertParameters
            {
                get
                {
                    return setInsertParamsFunc.Value;
                }
            }

            internal static Func<IDbCommand, T, int> InsertObject
            {
                get
                {
                    return insertMappedObject.Value;
                }
            }

            private static IEnumerable<Metadata.Column> KeyColumns
            {
                get
                {
                    return Metadata.Columns.Where(c => c.IsKey);
                }
            }

            private static string SelectColumnsList
            {
                get
                {
                    return selectColumnsList.Value;
                }
            }

            private static string WhereConditionByKey
            {
                get
                {
                    return whereConditionByKey.Value;
                }
            }

            private static Func<IDataRecord, T> BuildMapper()
            {
                var entityType = typeof(T);
                var record = Expression.Parameter(typeof(IDataRecord), "record");

                var statements = new List<Expression>
                    {
                        Expression.Call(GuardArgumentNotNullMethod, record, Expression.Constant("record")) 
                    };

                var result = Expression.Variable(entityType, "result");
                statements.Add(Expression.Assign(result, Expression.New(entityType)));

                var propertyAssignExprs = GetPropertySetterExprs(result, record);
                statements.AddRange(propertyAssignExprs);
                statements.Add(result);

                var body = Expression.Block(new[] { result }, statements);
                return Expression.Lambda<Func<IDataRecord, T>>(body, record).Compile();
            }

            private static IEnumerable<Expression> GetPropertySetterExprs(Expression resultExpr, Expression readerExpr)
            {
                return from column in Metadata.Columns
                       select GetPropertySetterExpr(column, resultExpr, readerExpr);
            }

            private static Expression GetPropertySetterExpr(Metadata.Column column, Expression resultExpr, Expression readerExpr)
            {
                var propertyExpression = Expression.Property(resultExpr, column.Property);
                var readerGetValueByNameExpression = CreateReaderGetValueExpr(column, readerExpr);

                return Expression.Assign(propertyExpression, readerGetValueByNameExpression);
            }

            private static Expression CreateReaderGetValueExpr(Metadata.Column column, Expression readerExpr)
            {
                var propertyName = Expression.Constant(column.Name);
                var ordinalCallExpr = Expression.Call(readerExpr, ReaderGetOrdinalMethod, propertyName);
                var getValueExpr = Expression.Property(readerExpr, ReaderIndexByStringProperty, propertyName);

                // Return the first valid expression:
                // - Property is Enum, 
                var propertyType = column.Property.PropertyType;
                return EnumParseReaderValueExpression(column, getValueExpr)
                    ?? ReaderGetSpecificTypeExpression(propertyType, readerExpr, ordinalCallExpr)
                    ?? Expression.Convert(getValueExpr, propertyType);
            }

            private static Expression EnumParseReaderValueExpression(Metadata.Column column, Expression getValueExpr)
            {
                if (column.IsEnum)
                {
                    // If DbEnumSaveType is Numeric, just cast
                    var propertyType = column.Property.PropertyType;
                    if (column.EnumSaveType == EnumSaveType.Numeric)
                    {
                        return Expression.Convert(getValueExpr, propertyType);
                    }

                    // Otherwise, Call (EnumType)Enum.Parse(typeof(EnumType), reader[Name].ToString(), true)
                    return
                        Expression.Convert(
                            Expression.Call(
                                EnumParseMethod,
                                Expression.Constant(propertyType),
                                Expression.Call(getValueExpr, ToStringMethod),
                                Expression.Constant(true)),
                            propertyType);
                }

                return null;
            }

            private static Expression ReaderGetSpecificTypeExpression(Type propertyType, Expression readerExpr, Expression ordinalCallExpr)
            {
                var methodName = "Get" + propertyType.Name; // GetString, GetInt32, etc...
                var fastMethod = typeof(IDataRecord).GetMethod(methodName, new[] { typeof(int) });
                return fastMethod != null ? Expression.Call(readerExpr, fastMethod, ordinalCallExpr) : null;
            }

            private static string CreateInsertStatement()
            {
                // Get names of columns for insert
                var insertColumns = Metadata.Columns.Where(c => c.IsInsertable).ToList();

                var format = "INSERT INTO {0} ({1}) VALUES ({2}) ";
                if (Metadata.IdentityColumn != null)
                {
                    format += "SELECT SCOPE_IDENTITY() AS " + Metadata.IdentityColumn.Name;
                }

                var columnList = string.Join(", ", insertColumns.Select(c => c.Name));
                var valuesList = string.Join(", ", insertColumns.Select(c => c.ParamName));

                return string.Format(format, Metadata.TableName, columnList, valuesList);
            }

            private static string CreateUpdateStatement()
            {
                var updateColumns = Metadata.Columns.Where(c => c.IsUpdatable).ToList();
                if (!updateColumns.Any())
                {
                    throw new MetadataValidationException("Cannot update. No non-key columns found for type " + typeof(T).FullName);
                }

                const string UpdateFormat = "UPDATE {0} SET {1} WHERE {2}";
                var columnAssigns = string.Join(", ", ColumnsToEqualSql(updateColumns));
                return string.Format(UpdateFormat, Metadata.TableName, columnAssigns, WhereConditionByKey);
            }

            private static IEnumerable<string> ColumnsToEqualSql(IEnumerable<Metadata.Column> cols)
            {
                return cols.Select(c => c.Name + " = " + c.ParamName);
            }

            private static string CreateDeleteStatement()
            {
                return string.Format("DELETE FROM {0} WHERE {1}", Metadata.TableName, WhereConditionByKey);
            }

            private static string CreateSelectStatement()
            {
                return string.Format("SELECT {0} FROM {1} ", SelectColumnsList, Metadata.TableName);
            }

            private static Action<IDbCommand, T> CreateSetParametersFunc(Func<Metadata.Column, bool> colPredicate = null)
            {
                var cmdExpr = Expression.Parameter(typeof(IDbCommand), "cmd");
                var objExpr = Expression.Parameter(typeof(T), "obj");
                var whichColumns = Metadata.Columns.AsEnumerable();

                if (colPredicate != null)
                {
                    whichColumns = whichColumns.Where(colPredicate);
                }

                var blockExprs = whichColumns.Select(column => ColumnToParamExpression(cmdExpr, column, objExpr));
                var bodyExpr = Expression.Block(blockExprs);
                return Expression.Lambda<Action<IDbCommand, T>>(bodyExpr, cmdExpr, objExpr).Compile();
            }

            private static MethodCallExpression ColumnToParamExpression(Expression cmdExpr, Metadata.Column column, Expression objExpr)
            {
                Expression valueExpr;

                if (column.IsEnum && column.EnumSaveType == EnumSaveType.String)
                {
                    valueExpr = Expression.Call(Expression.Property(objExpr, column.Property), ToStringMethod);
                }
                else
                {
                    valueExpr = Expression.Property(objExpr, column.Property);
                }

                var parameterName = Expression.Constant("@" + column.Name);
                var valueAsObject = Expression.Convert(valueExpr, typeof(object));
                return Expression.Call(SetCommandParameterValueMethod, cmdExpr, parameterName, valueAsObject);
            }

            private static Func<IDbCommand, T, int> CreateInsertMappedObjectFunc()
            {
                var cmdExpr = Expression.Parameter(typeof(IDbCommand), "cmd");
                var objExpr = Expression.Parameter(typeof(T), "obj");

                var bodyExpr = Metadata.IdentityColumn != null
                             ? ExecuteScalarExpression(objExpr, cmdExpr)
                             : ExecuteNonQueryExpression(cmdExpr);

                return Expression.Lambda<Func<IDbCommand, T, int>>(bodyExpr, cmdExpr, objExpr).Compile();
            }

            private static MethodCallExpression ExecuteNonQueryExpression(ParameterExpression cmdExpr)
            {
                return Expression.Call(cmdExpr, ExecuteNonQueryMethod);
            }

            private static Expression ExecuteScalarExpression(Expression objExpr, Expression cmdExpr)
            {
                var propertyExpression = Expression.Property(objExpr, Metadata.IdentityColumn.Property);
                var callExecuteScalar = Expression.Call(cmdExpr, ExecuteScalarMethod);
                var scalarConversionExpression = Expression.Convert(callExecuteScalar, Metadata.IdentityColumn.Property.PropertyType);
                return Expression.Assign(propertyExpression, scalarConversionExpression);
            }
        }

        internal static class Helpers
        {
            public static IDataParameter CreateParameterWithValue(IDbCommand cmd, string name, object value)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                return p;
            }

            // ReSharper disable UnusedMember.Global
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
                    {
                        o = o.ToString();
                    }
                }

                return o;
            }
        }

        internal class Metadata
        {
            internal Metadata(Type type)
            {
                this.TableName = GetTableName(type);
                this.Columns = type.GetProperties().Select(PropertyToColumn).Where(c => c != null).ToList();
                try
                {
                    this.IdentityColumn = this.Columns.SingleOrDefault(c => c.IsIdentity); // With throw if multiple identity 
                }
                catch (InvalidOperationException ex)
                {
                    throw new MetadataValidationException("Multiple Identities found for type: " + type.FullName, ex);
                }
            }

            public string TableName
            {
                get;
                private set;
            }

            public Column IdentityColumn
            {
                get;
                private set;
            }

            public List<Column> Columns
            {
                get;
                private set;
            }

            #region Helpers

            private static string GetTableName(Type type)
            {
                var typeTableAttr = Attribute.GetCustomAttribute(type, typeof(DbTableAttribute)) as DbTableAttribute;
                if (typeTableAttr != null)
                {
                    if (!string.IsNullOrWhiteSpace(typeTableAttr.Name))
                    {
                        return typeTableAttr.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(typeTableAttr.Prefix))
                    {
                        return typeTableAttr.Prefix + type.Name;
                    }
                }

                var assmTableAttr = Attribute.GetCustomAttribute(type.Assembly, typeof(DbTableAttribute)) as DbTableAttribute;
                if (assmTableAttr != null && !string.IsNullOrWhiteSpace(assmTableAttr.Prefix))
                {
                    return assmTableAttr.Prefix + type.Name;
                }

                return type.Name;
            }

            private static Column PropertyToColumn(PropertyInfo property)
            {
                if (Attribute.IsDefined(property, typeof(DbIgnoreAttribute)))
                {
                    return null;
                }

                var col = new Column
                {
                    Name = property.Name,
                    IsKey = Attribute.IsDefined(property, typeof(DbKeyAttribute)),
                    IsIdentity = Attribute.IsDefined(property, typeof(DbIdentityAttribute)),
                    Property = property,
                    ParamName = "@" + property.Name,
                    IsEnum = property.PropertyType.IsEnum
                };

                if (col.IsEnum)
                {
                    // Check for save type
                    Debug.Assert(property.DeclaringType != null, "property.DeclaringType != null");

                    var enumAttr = Attribute.GetCustomAttribute(property, typeof(DbEnumAttribute)) as DbEnumAttribute;
                    enumAttr = enumAttr ?? Attribute.GetCustomAttribute(property.PropertyType, typeof(DbEnumAttribute)) as DbEnumAttribute;
                    enumAttr = enumAttr ?? Attribute.GetCustomAttribute(property.PropertyType.Assembly, typeof(DbEnumAttribute)) as DbEnumAttribute;
                    if (enumAttr != null)
                    {
                        col.EnumSaveType = enumAttr.SaveType;
                    }
                }

                return col;
            }

            /// <summary>
            /// Represents a column in the database
            /// </summary>
            public class Column
            {
                internal Column()
                {
                }

                public string Name { get; internal set; }

                public bool IsKey { get; internal set; }

                public bool IsIdentity { get; internal set; }

                public bool IsEnum { get; internal set; }

                public string ParamName { get; internal set; }

                public EnumSaveType EnumSaveType { get; internal set; }

                public PropertyInfo Property { get; internal set; }

                public bool IsUpdatable
                {
                    get
                    {
                        return !(this.IsIdentity || this.IsKey);
                    }
                }

                public bool IsUpdatableParam
                {
                    get
                    {
                        return true;
                    }
                }

                public bool IsInsertable
                {
                    get
                    {
                        return !this.IsIdentity;
                    }
                }

                public bool IsInsertableParam
                {
                    get
                    {
                        return !this.IsIdentity;
                    }
                }
            }

            #endregion
        }
    }
}

// ReSharper restore MemberCanBePrivate.Global
