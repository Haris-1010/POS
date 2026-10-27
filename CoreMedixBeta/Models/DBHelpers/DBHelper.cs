using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace InstaCareEMR.Core.DBHelpers
{
    public class DBHelper
    {
        public string Query { get; set; }
        public Dictionary<string, string> values { get; set; }
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["NewConnectionString"].ToString();

        public DBHelper(bool debug = true)
        {
            this.Query = string.Empty;
            this.values = new Dictionary<string, string>();
        }

        #region InsertFunctions

        public static bool Insert<T>(T _Object, string schemaName = null, string tableName = null)
        {
            return ExecuteQuery(QueryMaker.InsertQuery(_Object, schemaName, tableName));
        }

        public static string Insert_and_get_ID<T>(T _Object, string schemaName = null, string tableName = null)
        {
            return ExecuteQuery_and_return_id(QueryMaker.InsertQueryWithID(_Object, schemaName, tableName));
        }

        #endregion InsertFunctions

        #region UpdateFunctions

        public static bool Update<T>(T _Object, string whereClause, Dictionary<string, string> values, string schemaName = null, string tableName = null)
        {
            return ExecuteQuery(QueryMaker.UpdateQuery(_Object, whereClause, schemaName, tableName), values);
        }

        public static bool Update(string tableName, Dictionary<string, string> dataset, string whereClause)
        {
            return ExecuteQuery(QueryMaker.UpdateQuery(tableName, dataset, whereClause));
        }

        public static bool Update<T>(Dictionary<string, string> ColumnsAndValues, string WhereClause, Dictionary<string, string> Params = null)
        {
            return ExecuteQuery(QueryMaker.UpdateQuery<T>(ColumnsAndValues, WhereClause), Params);
        }

        #endregion UpdateFunctions

        #region DeleteFunctions

        public static bool Delete<T>(string whereClause, Dictionary<string, string> values)
        {
            return ExecuteQuery(QueryMaker.DeleteQuery<T>(whereClause), values);
        }

        #endregion DeleteFunctions

        #region OtherFunctions

        public void BeginTrans()
        {
            Query = string.Empty;
            Query += QueryMaker.BeginTransQuery();
        }

        public bool CommitTrans()
        {
            Query += QueryMaker.CommitTransQuery();
            bool result = ExecuteQuery(Query);
            Query = string.Empty;
            return result;
        }

        public bool CommitTrans(Dictionary<string, string> Params)
        {
            Query += QueryMaker.CommitTransQuery();
            bool result = ExecuteQuery(Query, Params);
            Query = string.Empty;
            return result;
        }

        public static bool ExecuteQuery(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            Connection.Open();
            int rowsAffected = Connection.Execute(query, GetDynamicParams(Params));
            return rowsAffected > 0;
        }

        public static bool ExecuteLongQuery(string query, Dictionary<string, string> Params = null, int seconds = 1200)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            Connection.Open(); // Open connection (Dapper manages pooling)
            var commandDefinition = new CommandDefinition(query, GetDynamicParams(Params), commandTimeout: seconds);
            int rowsAffected = Connection.Execute(commandDefinition); // Execute query with timeout
            return rowsAffected > 0;
        }

        public static string ExecuteQuery_and_return_id(string query, Dictionary<string, string> Params = null)
        {
            try
            {
                SqlConnection Connection = new SqlConnection(ConnectionString);
                Connection.Open();
                var id = Connection.ExecuteScalar<int>(query, GetDynamicParams(Params));
                return id.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to insert and get id because:" + ex.Message;
            }
        }

        #endregion OtherFunctions

        #region ReadFunctions

        public static PageListObject<T> GetPagedList<T>(string query, string sortBy, string order, int numberOfRecords, int pageNumber, Dictionary<string, string> Params)
        {
            int offset = numberOfRecords * pageNumber;
            var Querries = QueryMaker.GetPagerQueries<T>(query, sortBy, order, offset, numberOfRecords);
            string mainquery = Querries.Query;
            string countquery = Querries.CountQuery;
            return PagedResults<T>(numberOfRecords, pageNumber, Params, offset, mainquery, countquery);
        }

        public static PageListObject<T> GetPagedList<T>(string query, Dictionary<string, string> sortAndOrder, int numberOfRecords, int pageNumber, Dictionary<string, string> Params, string countQuery = "")
        {
            int offset = numberOfRecords * pageNumber;
            var Querries = QueryMaker.GetPagerQueries<T>(query, sortAndOrder, offset, numberOfRecords);
            string mainquery = Querries.Query;
            string countquery = !string.IsNullOrEmpty(countQuery) ? countQuery : Querries.CountQuery;
            return PagedResults<T>(numberOfRecords, pageNumber, Params, offset, mainquery, countquery);
        }

        private static PageListObject<T> PagedResults<T>(int numberOfRecords, int pageNumber, Dictionary<string, string> Params, int offset, string mainquery, string countquery)
        {
            var Connection = new SqlConnection(ConnectionString);
            Connection.Open();
            var multi = Connection.QueryMultiple($"{mainquery}; {countquery}", GetDynamicParams(Params));
            var results = multi.Read<T>().ToList();
            int count = multi.Read<int>().FirstOrDefault();

            Pager pager = GetPagerSettings(numberOfRecords, pageNumber, count, offset);

            return new PageListObject<T> { pager = pager, Results = results };
        }

        public static int GetScaler(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            Connection.Open();
            int count = Connection.ExecuteScalar<int>(query, GetDynamicParams(Params)); // Executes query with parameters
            return count;
        }

        public static string GetSingleColumn(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            Connection.Open(); // Open connection (Dapper handles pooling)
            return Connection.ExecuteScalar<string>(query, GetDynamicParams(Params));
        }

        public static List<string> QueryColumn(string query, Dictionary<string, string> Params)
        {
            var Connection = new SqlConnection(ConnectionString);
            Connection.Open();
            var list = Connection.Query<string>(query, GetDynamicParams(Params)).ToList();
            return list;
        }

        public static T Get<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);

            var Connection = new SqlConnection(ConnectionString);
            Connection.Open();
            var result = Connection.QueryFirstOrDefault<T>(query, GetDynamicParams(Params));
            return result != null ? result : Activator.CreateInstance<T>();
        }

        public static bool Get<T>(T _Object, string query = "", string where_clause = "", Dictionary<string, string> Params = null)
        {
            List<PropertyInfo> _properties = GetReadableProperties<T>();
            query = QueryMaker.SelectQuery<T>(query, where_clause);

            SqlConnection connection = new SqlConnection(ConnectionString);
            connection.Open();
            SqlCommand cmd = GetSqlCommandWithParams(query, connection, Params);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    foreach (PropertyInfo pi in _properties)
                    {
                        DBReader(_Object, pi, reader);
                    }
                }
            }
            connection.Close();
            return true;
        }

        public static List<T> GetList<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);
            var Connection = new SqlConnection(ConnectionString);
            Connection.Open();
            return Connection.Query<T>(query, GetDynamicParams(Params)).ToList();
        }

        #endregion ReadFunctions

        #region Async Methods With Dapper

        public static async Task<bool> InsertAsync<T>(T _Object, string schemaName = null, string tableName = null)
        {
            return await ExecuteQueryAsync(QueryMaker.InsertQuery(_Object, schemaName, tableName));
        }

        public static async Task<string> InsertAndGetIdAsync<T>(T _Object, string schemaName = null, string tableName = null)
        {
            return await ExecuteQueryAndReturnIdAsync(QueryMaker.InsertQueryWithID(_Object, schemaName, tableName));
        }

        public static async Task<bool> InsertListAsync<T>(IEnumerable<T> objects, string schemaName = null, string tableName = null)
        {
            if (objects == null || !objects.Any())
                throw new ArgumentException("The list of objects to insert cannot be null or empty.");

            return await ExecuteQueryAsync(QueryMaker.InsertListQuery(objects, schemaName, tableName));
        }

        public static async Task<bool> UpdateAsync<T>(T _Object, string whereClause, Dictionary<string, string> values, string schemaName = null, string tableName = null)
        {
            return await ExecuteQueryAsync(QueryMaker.UpdateQuery(_Object, whereClause, schemaName, tableName), values);
        }

        public static async Task<bool> DeleteAsync<T>(string whereClause, Dictionary<string, string> values)
        {
            return await ExecuteQueryAsync(QueryMaker.DeleteQuery<T>(whereClause), values);
        }

        public static async Task<bool> ExecuteQueryAsync(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            int rowsAffected = await connection.ExecuteAsync(query, GetDynamicParams(Params));

            return rowsAffected > 0;
        }

        public static async Task<string> ExecuteQueryAndReturnIdAsync(string query, Dictionary<string, string> Params = null)
        {
            try
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                int result = await connection.ExecuteScalarAsync<int>(query, GetDynamicParams(Params));
                return result.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to insert and get id because: " + ex.Message;
            }
        }

        public static async Task<bool> ExecuteQueriesWithTransactionAsync(IEnumerable<(
            string query, Dictionary<string, string> parameters
            )> queries)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                foreach (var (query, parameters) in queries)
                {
                    await connection.ExecuteAsync(query, GetDynamicParams(parameters), transaction: transaction);
                }

                // Commit the transaction if all queries succeed
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Commit();
                return false;
            }
        }

        public static async Task<bool> ExecuteQueryWithTransactionAsync(string query, Dictionary<string, string> parameters)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(query, GetDynamicParams(parameters), transaction: transaction);
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /* =========== GET METHODS ========== */

        #region ReadFunctions

        public static async Task<int> GetScalerAsync(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(query, GetDynamicParams(Params));
        }

        public static async Task<string> GetSingleColumnAsync(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<string>(query, GetDynamicParams(Params)) ?? string.Empty;
        }

        public static async Task<List<T>> GetListAsync<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            var results = new List<T>();
            query = QueryMaker.SelectQuery<T>(query, whereClause);

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                results = (await connection.QueryAsync<T>(query, GetDynamicParams(Params))).ToList();
            }

            return results;
        }

        public static async Task<PageListObject<T>> GetPagedListAsync<T>(string query, Dictionary<string, string> sortAndOrder, int numberOfRecords, int pageNumber, Dictionary<string, string> Params, string countQuery = "")
        {
            int offset = numberOfRecords * pageNumber;
            var Querries = QueryMaker.GetPagerQueries<T>(query, sortAndOrder, offset, numberOfRecords);
            string mainquery = Querries.Query;
            string countquery = !string.IsNullOrEmpty(countQuery) ? countQuery : Querries.CountQuery;

            return await PagedResultsAsync<T>(numberOfRecords, pageNumber, Params, offset, mainquery, countquery);
        }

        private static async Task<PageListObject<T>> PagedResultsAsync<T>(int numberOfRecords, int pageNumber, Dictionary<string, string> Params, int offset, string mainquery, string countquery)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            int count = 0;
            List<T> results = new List<T>();

            string combinedQuery = $"{mainquery}; {countquery};";
            using (var multi = await connection.QueryMultipleAsync(combinedQuery, GetDynamicParams(Params)))
            {
                results = (await multi.ReadAsync<T>()).ToList();
                count = await multi.ReadSingleAsync<int>();
            }
            Pager pager = GetPagerSettings(numberOfRecords, pageNumber, count, offset);
            return new PageListObject<T> { pager = pager, Results = results };
        }

        public static async Task<Dictionary<string, string>> QueryRowAsync(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<IDictionary<string, object>>(query, GetDynamicParams(Params));

            return result?.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString() ?? string.Empty)
                   ?? new Dictionary<string, string>();
        }

        public static async Task<List<string>> QueryColumnAsync(string query, Dictionary<string, string> Params = null)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return (await connection.QueryAsync<string>(query, GetDynamicParams(Params))).ToList();
        }

        public static async Task<List<int>> GetIntListAsync(string query, Dictionary<string, string> Params)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return (await connection.QueryAsync<int>(query, GetDynamicParams(Params))).ToList();
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="whereClause"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static async Task<T> GetAsync<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);

            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<T>(query, GetDynamicParams(Params));

            return result != null ? result : Activator.CreateInstance<T>();
        }

        public static async Task<bool> GetAsync<T>(T _Object, string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);

            SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<T>(query, GetDynamicParams(Params));

            if (result != null)
            {
                foreach (var prop in typeof(T).GetProperties())
                {
                    var value = prop.GetValue(result);
                    prop.SetValue(_Object, value);
                }
                return true;
            }

            foreach (var prop in typeof(T).GetProperties())
            {
                prop.SetValue(_Object, Activator.CreateInstance(prop.PropertyType));
            }
            return false;
        }

        #endregion ReadFunctions

        #endregion Async Methods With Dapper

        #region Priavte Helper Functions

        private static DynamicParameters GetDynamicParams(Dictionary<string, string> Params)
        {
            var dynamicParams = new DynamicParameters();

            if (Params != null)
            {
                foreach (var param in Params)
                {
                    int length = param.Value?.Length ?? 0;
                    dynamicParams.Add(param.Key, param.Value, size: GetOptimalSize(length));
                }
            }

            return dynamicParams;
        }

        private static int GetOptimalSize(int length = 0)
        {    // Handle special small sizes first
            if (length <= 10) return 10;
            if (length <= 20) return 20;
            if (length <= 30) return 30;
            if (length <= 40) return 40;
            if (length <= 50) return 50;
            // Then proceed with normal 100-step increments
            var len = length <= 3900 ? ((length + 99) / 100) * 100 : length;
            return len;
        }

        private static Pager GetPagerSettings(int numberOfRecords, int pageNumber, int count, int offset)
        {
            return new Pager
            {
                RecordsPerPage = numberOfRecords,
                Offset = offset,
                Fetch = numberOfRecords,
                TotalRecords = count,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling((double)count / numberOfRecords)
            };
        }

        private static SqlCommand GetSqlCommandWithParams(string query, SqlConnection Connection, Dictionary<string, string> Params)
        {
            SqlCommand cmd = new SqlCommand(query, Connection);
            foreach (var element in Params ?? new Dictionary<string, string>())
            {
                cmd.Parameters.AddWithValue(element.Key, element.Value);
            }
            return cmd;
        }

        private static void DBReader(Object _Object, PropertyInfo property, SqlDataReader reader)
        {
            try
            {
                if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                {
                    Type convertTo = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    property.SetValue(_Object, Convert.ChangeType(reader[property.Name], convertTo), null);
                }
                else
                {
                    property.SetValue(_Object, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                property.SetValue(_Object, null);
            }
        }

        private static List<PropertyInfo> GetReadableProperties<T>()
        {
            return typeof(T).GetProperties().Where(x => !(Attribute.IsDefined(x, typeof(DontLoad)))).ToList();
        }

        #endregion Priavte Helper Functions
    }
}