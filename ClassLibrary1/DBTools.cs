﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace LightDataAccess
{
    /// <summary>
    /// Class DBTools
    /// </summary>
    public static class DBTools
    {
        /// <summary>
        /// Executes the non query.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <returns>System.Int32.</returns>
        public static int ExecuteNonQuery(DbConnection conn, string commandText, CmdParams cmdParams)
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes the scalar.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <returns>System.Object.</returns>
        public static object ExecuteScalar(DbConnection conn, string commandText, CmdParams cmdParams)
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes the scalar.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <returns>``0.</returns>
        public static T ExecuteScalar<T>(DbConnection conn, string commandText, CmdParams cmdParams)
        {
            object result = ExecuteScalar(conn, commandText, cmdParams);
            if (typeof(T) == typeof(Guid))
            {
                if (result == null)
                {
                    return (T)((object)Guid.Empty);
                }
                return (T)((object)new Guid(result.ToString()));
            }
            if (result is DBNull || result == null)
            {
                return default(T);
            }
            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Executes the reader.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <returns>IDataReader.</returns>
        public static IDataReader ExecuteReader(DbConnection conn, string commandText, CmdParams cmdParams)
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            {
                return cmd.ExecuteReader();
            }
        }

        /// <summary>
        /// Executes the reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <param name="func">The func.</param>
        /// <returns>``0.</returns>
        public static T ExecuteReader<T>(DbConnection conn, string commandText, CmdParams cmdParams, Func<IDataReader, T> func) where T : class
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return func(reader);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Executes the reader struct.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <param name="func">The func.</param>
        /// <returns>``0.</returns>
        public static T ExecuteReaderStruct<T>(DbConnection conn, string commandText, CmdParams cmdParams, Func<IDataReader, T> func) where T : struct
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return func(reader);
                }
                else
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Executes the reader enum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <param name="func">The func.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ExecuteReaderEnum<T>(DbConnection conn, string commandText, CmdParams cmdParams, Func<IDataReader, T> func)
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return func(reader);
                }
            }
        }

        /// <summary>
        /// Reads the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ReadCollection<T>(
            DbConnection conn,
            string commandText,
            CmdParams cmdParams,
            string[] excludeFields)
        {
            return ReadCollection<T>(conn, commandText, cmdParams, excludeFields, null);
        }

        /// <summary>
        /// Reads the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <param name="changeTracker">The change tracker.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ReadCollection<T>(
            DbConnection conn,
            string commandText,
            CmdParams cmdParams,
            string[] excludeFields,
            ObjectsChangeTracker changeTracker)
        {
            using (DbCommand cmd = CreateCommand(conn, commandText, cmdParams))
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return reader.ToObject<T>(null, excludeFields, changeTracker);
                }
            }
        }

        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <returns>DbCommand.</returns>
        public static DbCommand CreateCommand(DbConnection conn, string commandText)
        {
            DbCommand result = CreateCommand(conn, commandText, null);
            return result;
        }

        /// <summary>
        /// Adds the param.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="paramName">Name of the param.</param>
        /// <param name="paramValue">The param value.</param>
        /// <returns>DbCommand.</returns>
        public static DbCommand AddParam(this DbCommand cmd, string paramName, object paramValue)
        {
            if (paramValue is Guid)
            {
                paramValue = ((Guid)paramValue).ToGuidStr();
            }

            if (paramValue == null)
            {
                paramValue = DBNull.Value;
            }

            DbParameter par = cmd.CreateParameter();
            par.ParameterName = paramName;
            par.Value = paramValue;
            cmd.Parameters.Add(par);
            return cmd;
        }

        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cmdParams">The CMD params.</param>
        /// <returns>DbCommand.</returns>
        public static DbCommand CreateCommand(DbConnection conn, string commandText, CmdParams cmdParams)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            DbCommand result = conn.CreateCommand();
            result.CommandText = commandText;
            result.CommandType = CommandType.Text;
            if (cmdParams != null)
            {
                foreach (KeyValuePair<string, object> param in cmdParams)
                {
                    object value;
                    if (param.Value is Guid)
                    {
                        value = ((Guid)param.Value).ToGuidStr();
                    }
                    else if (param.Value is bool)
                    {
                        value = ((bool)param.Value).ToShort();
                    }
                    else
                    {
                        value = param.Value;
                    }
                    result.AddParam(param.Key, value);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates the stored procedure command.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="spName">Name of the sp.</param>
        /// <returns>DbCommand.</returns>
        public static DbCommand CreateStoredProcedureCommand(DbConnection conn, string spName)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            DbCommand result = conn.CreateCommand();
            result.CommandText = spName;
            result.CommandType = CommandType.StoredProcedure;
            return result;
        }

        /// <summary>
        /// To the CSV.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="delim">The delim.</param>
        /// <returns>System.String.</returns>
        public static string ToCSV<T>(this IEnumerable<T> collection, string delim)
        {
            if (collection == null)
            {
                return "";
            }

            StringBuilder result = new StringBuilder();
            foreach (T value in collection)
            {
                result.Append(value);
                result.Append(delim);
            }
            if (result.Length > 0)
            {
                result.Length -= delim.Length;
            }
            return result.ToString();
        }

        /// <summary>
        /// To the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>``0.</returns>
        public static T ToObject<T>(this IDataReader reader)
        {
            return reader.ToObject<T>(null, null, null);
        }

        /// <summary>
        /// To the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <returns>``0.</returns>
        public static T ToObject<T>(this IDataReader reader, string readerName)
        {
            return reader.ToObject<T>(readerName, null, null);
        }

        /// <summary>
        /// To the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <returns>``0.</returns>
        public static T ToObject<T>(this IDataReader reader, string[] excludeFields)
        {
            return reader.ToObject<T>(null, excludeFields, null);
        }

        /// <summary>
        /// To the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <param name="changeTracker">The change tracker.</param>
        /// <returns>``0.</returns>
        public static T ToObject<T>(this IDataReader reader, string readerName, string[] excludeFields, ObjectsChangeTracker changeTracker)
        {
            T result = new DataReaderToObjectMapper<T>(readerName, null, excludeFields).ReadSingle(reader, changeTracker);
            if (changeTracker != null)
            {
                changeTracker.RegisterObject(result);
            }
            return result;
        }

        /// <summary>
        /// To the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ToObjects<T>(this IDataReader reader)
        {
            return reader.ToObjects<T>(null, null, null);
        }

        /// <summary>
        /// To the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ToObjects<T>(this IDataReader reader, string readerName)
        {
            return reader.ToObjects<T>(readerName, null, null);
        }

        /// <summary>
        /// To the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ToObjects<T>(this IDataReader reader, string[] excludeFields)
        {
            return reader.ToObjects<T>(null, excludeFields, null);
        }

        /// <summary>
        /// To the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <param name="changeTracker">The change tracker.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> ToObjects<T>(this IDataReader reader, string readerName, string[] excludeFields, ObjectsChangeTracker changeTracker)
        {
            if (string.IsNullOrEmpty(readerName))
            {
                StringBuilder mappingKeyBuilder = new StringBuilder();
                for (int i = 0; i < reader.FieldCount; ++i)
                {
                    mappingKeyBuilder.Append(reader.GetName(i));
                    mappingKeyBuilder.Append(' ');
                }
                readerName = mappingKeyBuilder.ToString();
            }

            DataReaderToObjectMapper<T> mapper = new DataReaderToObjectMapper<T>(readerName, null, excludeFields);

            while (reader.Read())
            {
                yield return mapper.ReadSingle(reader, changeTracker);
            }

        }

        /// <summary>
        /// Inserts the object.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="dbSettings">The db settings.</param>
        /// <param name="includeFields">The include fields.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        public static Task<int> InsertObject(
            DbConnection conn,
            object obj,
            string tableName,
            DbSettings dbSettings,
            string[] includeFields,
            string[] excludeFields
        )
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.BuildInsertCommand(obj, tableName, dbSettings, includeFields, excludeFields);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts the object.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="dbSettings">The db settings.</param>
        public static Task<int> InsertObject(
            DbConnection conn,
            object obj,
            string tableName,
            DbSettings dbSettings
        )
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.BuildInsertCommand(obj, tableName, dbSettings);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Updates the object.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idFieldNames">The id field names.</param>
        /// <param name="changeTracker">The change tracker.</param>
        /// <param name="dbSettings">The db settings.</param>
        public static Task<int> UpdateObject(
            DbConnection conn,
            object obj,
            string tableName,
            string[] idFieldNames,
            ObjectsChangeTracker changeTracker,
            DbSettings dbSettings
        )
        {
            return UpdateObject(conn, obj, tableName, idFieldNames, null, null, changeTracker, dbSettings);
        }

        /// <summary>
        /// Updates the object.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idFieldNames">The id field names.</param>
        /// <param name="includeFields">The include fields.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <param name="changeTracker">The change tracker.</param>
        /// <param name="dbSettings">The db settings.</param>
        public static Task<int> UpdateObject(
            DbConnection conn,
            object obj,
            string tableName,
            string[] idFieldNames,
            string[] includeFields,
            string[] excludeFields,
            ObjectsChangeTracker changeTracker,
            DbSettings dbSettings
        )
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                if (
                    cmd.BuildUpdateCommand(
                        obj,
                        tableName,
                        idFieldNames,
                        includeFields,
                        excludeFields,
                        changeTracker,
                        dbSettings
                    )
                )
                {
                    return cmd.ExecuteNonQueryAsync();
                }
                return Task.FromResult(0);
            }
        }

        /// <summary>
        /// Updates the object.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idFieldNames">The id field names.</param>
        /// <param name="dbSettings">The db settings.</param>
        public static Task<int> UpdateObject(
            DbConnection conn,
            object obj,
            string tableName,
            string[] idFieldNames,
            DbSettings dbSettings
        )
        {
            return UpdateObject(conn, obj, tableName, idFieldNames, null, null, null, dbSettings);
        }
    }
}
