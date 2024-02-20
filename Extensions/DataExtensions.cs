using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using projects.Attributes;

namespace practice.DataExtensions
{
    public static class DataExtensions
    {
        #region Public Methods 

        public static DbCommand LoadStoredProc(this DbContext context, string storedProcName, bool prependDefaultSchema = true, short commandTimeout = 30)
        {
            var connection = context.Database.GetDbConnection() as SqlConnection;
            var cmd = connection.CreateCommand();

            cmd.CommandTimeout = commandTimeout;

            if (prependDefaultSchema)
            {
                var schemaName = context.Model.GetDefaultSchema();
                if (schemaName != null)
                {
                    storedProcName = $"{schemaName}.{storedProcName}";
                }
            }

            cmd.CommandText = storedProcName;
            cmd.CommandType = CommandType.StoredProcedure;

            return cmd;
        }

        public static DbCommand WithSqlParam(this DbCommand cmd, string paramName, object paramValue, Action<DbParameter> configureParam = null)
        {
            if (string.IsNullOrEmpty(cmd.CommandText) && cmd.CommandType != CommandType.StoredProcedure)
                throw new InvalidOperationException("Call LoadStoredProc before using this method");

            var param = cmd.CreateParameter();
            param.ParameterName = paramName;
            param.Value = paramValue ?? DBNull.Value;
            configureParam?.Invoke(param);
            cmd.Parameters.Add(param);
            return cmd;
        }

        public static DbCommand WithSqlParam(this DbCommand cmd, string paramName, Action<DbParameter> configureParam = null)
        {
            if (string.IsNullOrEmpty(cmd.CommandText) && cmd.CommandType != CommandType.StoredProcedure)
                throw new InvalidOperationException("Call LoadStoredProc before using this method");

            var param = cmd.CreateParameter();
            param.ParameterName = paramName;
            configureParam?.Invoke(param);
            cmd.Parameters.Add(param);
            return cmd;
        }

        public static DbCommand WithSqlParam(this DbCommand cmd, string paramName, SqlParameter parameter)
        {
            if (string.IsNullOrEmpty(cmd.CommandText) && cmd.CommandType != CommandType.StoredProcedure)
                throw new InvalidOperationException("Call LoadStoredProc before using this method");

            cmd.Parameters.Add(parameter);

            return cmd;
        }

        public static DbCommand WithTableSqlParam<T>(this DbCommand cmd, string paramName, IEnumerable<T> parameterValues, string columnName = "Id")
        {
            if (string.IsNullOrEmpty(cmd.CommandText) && cmd.CommandType != CommandType.StoredProcedure)
                throw new InvalidOperationException("Call LoadStoredProc before using this method");

            var table = new DataTable();
            table.Columns.Add(columnName, typeof(T));
            parameterValues?.ToList().ForEach(v => table.Rows.Add(v));

            var param = cmd.CreateParameter();
            param.ParameterName = paramName;
            param.Value = table;
            cmd.Parameters.Add(param);
            return cmd;
        }

        public static DbCommand WithErrorCodeAndErrorMessageParam(this DbCommand cmd, string errorCodeParamName = "ErrorCode", string errorDescriptionparamName = "ErrorDescription")
        {

            return cmd.WithSqlParam("ErrorCode", 0, x =>
            {
                x.Direction = ParameterDirection.Output;
                x.DbType = DbType.Int32;
            })
                .WithSqlParam("ErrorDescription", String.Empty, x =>
                {
                    x.DbType = DbType.String;
                    x.Size = 2047;
                    x.Direction = ParameterDirection.Output;
                });
        }

        public static DbCommand ExecuteStoredProc(this DbCommand command, Action<SpResultsReader> handleResults, CommandBehavior commandBehaviour = CommandBehavior.Default, bool manageConnection = true, Action<DbCommand> outputParameterHandler = null)
        {
            if (handleResults == null)
            {
                throw new ArgumentNullException(nameof(handleResults));
            }

            using (command)
            {
                if (manageConnection && command.Connection.State == ConnectionState.Closed)
                    command.Connection.Open();
                try
                {
                    using (var reader = command.ExecuteReader(commandBehaviour))
                    {
                        var sprocResults = new SpResultsReader(reader);
                        outputParameterHandler?.Invoke(command);
                        handleResults(sprocResults);

                        return command;

                    }
                }
                finally
                {
                    if (manageConnection)
                    {
                        command.Connection.Close();
                    }
                }
            }
        }

        public async static Task ExecuteStoredProcAsync(this DbCommand command, Action<SpResultsReader> handleResults, CommandBehavior commandBehaviour = CommandBehavior.Default, CancellationToken ct = default(CancellationToken), bool manageConnection = true, Action<DbCommand> outputParameterHandler = null)
        {
            if (handleResults == null)
            {
                throw new ArgumentNullException(nameof(handleResults));
            }

            using (command)
            {
                if (manageConnection && command.Connection.State == ConnectionState.Closed)
                    await command.Connection.OpenAsync(ct).ConfigureAwait(false);
                try
                {
                    using (var reader = await command.ExecuteReaderAsync(commandBehaviour, ct).ConfigureAwait(false))
                    {
                        var sprocResults = new SpResultsReader(reader);
                        outputParameterHandler?.Invoke(command);
                        handleResults(sprocResults);
                    }
                }
                finally
                {
                    if (manageConnection)
                    {
                        command.Connection.Close();
                    }
                }
            }
        }

        public static int ExecuteStoredNonQuery(this DbCommand command, CommandBehavior commandBehaviour = CommandBehavior.Default, bool manageConnection = true, Action<DbCommand> outputParameterHandler = null)
        {
            int numberOfRecordsAffected = -1;

            using (command)
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    command.Connection.Open();
                }

                try
                {
                    numberOfRecordsAffected = command.ExecuteNonQuery();
                    outputParameterHandler?.Invoke(command);
                }
                finally
                {
                    if (manageConnection)
                    {
                        command.Connection.Close();
                    }
                }
            }

            return numberOfRecordsAffected;
        }

        public async static Task<int> ExecuteStoredNonQueryAsync(this DbCommand command, CommandBehavior commandBehaviour = CommandBehavior.Default, CancellationToken ct = default(CancellationToken), bool manageConnection = true, Action<DbCommand> outputParameterHandler = null)
        {
            int numberOfRecordsAffected = -1;

            using (command)
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    await command.Connection.OpenAsync(ct).ConfigureAwait(false);
                }

                try
                {
                    numberOfRecordsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    outputParameterHandler?.Invoke(command);
                }
                finally
                {
                    if (manageConnection)
                    {
                        command.Connection.Close();
                    }
                }
            }

            return numberOfRecordsAffected;
        }

        public static DbCommand WithMultiColumnTableSqlParam<T>(this DbCommand cmd, string paramName, IEnumerable<T> parameterValues)
        {
            if (string.IsNullOrEmpty(cmd.CommandText) && cmd.CommandType != CommandType.StoredProcedure)
                throw new InvalidOperationException("Call LoadStoredProc before using this method");

            var properties = typeof(T).GetProperties()
                .Where(x => x.IsDefined(typeof(ColumnSqlAttribute)))
                .OrderBy(x => x.GetCustomAttribute<ColumnSqlAttribute>().Order).ToList();

            var table = new DataTable();

            properties.ForEach(p => table.Columns.Add(p.GetCustomAttribute<ColumnSqlAttribute>().Name, Nullable.GetUnderlyingType(
                                                                                                           p.PropertyType) ?? p.PropertyType));

            parameterValues?.ToList().ForEach(
                v =>
                {
                    var values = properties.Select(x => x.GetValue(v)).ToArray();
                    table.Rows.Add(values);
                });

            var param = cmd.CreateParameter();
            param.ParameterName = paramName;
            param.Value = table;
            cmd.Parameters.Add(param);
            return cmd;
        }

        #endregion

        #region Private Methods


        #endregion
    }

    public class SpResultsReader
    {
        #region Fields

        private DbDataReader reader;

        #endregion

        #region Constractor

        public SpResultsReader(DbDataReader reader)
        {
            this.reader = reader;
        }

        #endregion

        #region Public Methods

        public List<T> ReadToList<T>()
        {
            return MapToList<T>(reader);
        }

        public T ReadToEntity<T>() where T : class
        {
            return MapToList<T>(reader).FirstOrDefault();
        }

        public T? ReadToValue<T>() where T : struct
        {
            return MapToValue<T>(reader);
        }

        public Task<bool> NextResultAsync()
        {
            return reader.NextResultAsync();
        }

        public Task<bool> NextResultAsync(CancellationToken ct)
        {
            return reader.NextResultAsync(ct);
        }

        public bool NextResult()
        {
            return reader.NextResult();
        }

        #endregion

        #region Private Methods

        private List<T> MapToList<T>(DbDataReader dr)
        {
            var objList = new List<T>();
            var props = typeof(T).GetRuntimeProperties().ToList();

            var colMapping = dr.GetColumnSchema()
                .Where(x => props.Any(y => y.Name.ToLower() == x.ColumnName.ToLower()))
                .ToDictionary(key => key.ColumnName.ToLower());

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    T obj = Activator.CreateInstance<T>();
                    foreach (var prop in props)
                    {
                        if (colMapping.ContainsKey(prop.Name.ToLower()))
                        {
                            var column = colMapping[prop.Name.ToLower()];

                            if (column?.ColumnOrdinal != null)
                            {
                                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    var genericType = prop.PropertyType.GetGenericArguments()[0];
                                    object val = dr.GetValue(column.ColumnOrdinal.Value);
                                    prop.SetValue(obj, val == DBNull.Value ? null : Convert.ChangeType(val, genericType), null);

                                }
                                else
                                {
                                    object val = dr.GetValue(column.ColumnOrdinal.Value);
                                    prop.SetValue(obj, val == DBNull.Value ? null : val);
                                }
                                //object val = dr.GetValue(column.ColumnOrdinal.Value);
                                //prop.SetValue(obj, val == DBNull.Value ? null : val);
                            }

                        }
                    }
                    objList.Add(obj);
                }
            }
            return objList;
        }
        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        private T MapToEntity<T>(DbDataReader dr) where T : class
        {
            var props = typeof(T).GetRuntimeProperties().ToList();

            var colMapping = dr.GetColumnSchema()
                .Where(x => props.Any(y => y.Name.ToLower() == x.ColumnName.ToLower()))
                .ToDictionary(key => key.ColumnName.ToLower());

            if (!dr.HasRows) return null;

            T obj = Activator.CreateInstance<T>();
            foreach (var prop in props)
            {
                if (colMapping.ContainsKey(prop.Name.ToLower()))
                {
                    var column = colMapping[prop.Name.ToLower()];

                    if (column?.ColumnOrdinal != null)
                    {
                        var val = dr.GetValue(column.ColumnOrdinal.Value);
                        prop.SetValue(obj, val == DBNull.Value ? null : val);
                    }

                }
            }
            return obj;
        }
        private T? MapToValue<T>(DbDataReader dr) where T : struct
        {
            if (dr.HasRows)
            {
                if (dr.Read())
                {
                    return dr.IsDBNull(0) ? new T?() : new T?(dr.GetFieldValue<T>(0));
                }
            }
            return new T?();
        }

        #endregion
    }

}


