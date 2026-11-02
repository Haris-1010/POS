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
    public class StoredProcedureHelper
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["NewConnectionString"]?.ConnectionString;

        //public static GetSessionUserOutputDto GetSessionUser(Dictionary<string, string> Params)
        //{
        //    try
        //    {
        //        GetSessionUserOutputDto output = new GetSessionUserOutputDto();

        //        DynamicParameters parameters = GetDynamicParams(Params);

        //        using (SqlConnection connection = new SqlConnection(ConnectionString))
        //        {
        //            connection.Open();

        //            using var multi = connection.QueryMultiple("GetEMRSessionUser", parameters, commandType: CommandType.StoredProcedure);
        //            output.InstaUser = multi.Read<User>().FirstOrDefault();
        //            if (output.InstaUser.Type == "Pharmacy Manager")
        //            {
        //                output.Permissions = multi.Read<PermissionListVM>().ToList();
        //                output.PracticeLocations = multi.Read<LocationShortDto>().ToList();
        //                output.SessionEssentials = multi.Read<SessionEssentialsDto>().FirstOrDefault();
        //                output.SystemAlerts = multi.Read<SystemAlert>().ToList();
        //                output.PackageDetail = multi.Read<Doctor_Package_Detail>().FirstOrDefault() ?? new Doctor_Package_Detail();
        //            }
        //            else
        //            {
        //                output.Package = multi.Read<SubscriptionPackageDto>().FirstOrDefault() ?? new SubscriptionPackageDto();
        //                output.PackageDetail = multi.Read<Doctor_Package_Detail>().FirstOrDefault() ?? new Doctor_Package_Detail();
        //                output.PrescriptionStyle = multi.Read<PrescriptionStyleDto>().FirstOrDefault() ?? new PrescriptionStyleDto();
        //                output.Permission = multi.Read<Core.DBModels.InstaUser.Permission>().FirstOrDefault() ?? new Core.DBModels.InstaUser.Permission();
        //                output.Permissions = multi.Read<PermissionListVM>().ToList() ?? new List<PermissionListVM>();
        //                output.PracticeLocations = multi.Read<LocationShortDto>().ToList() ?? new List<LocationShortDto>();
        //                output.MenuItems = multi.Read<MenuItemsDto>().FirstOrDefault() ?? new MenuItemsDto();
        //                output.Branding = multi.Read<Branding>().FirstOrDefault() ?? new Branding();
        //                output.AppointmentSettings = multi.Read<Settings>().FirstOrDefault() ?? new Settings();
        //                output.SystemAlerts = multi.Read<SystemAlert>().ToList() ?? new List<SystemAlert>();
        //                output.SMSBalance = multi.Read<SMSBalance>().FirstOrDefault() ?? new SMSBalance();
        //                output.SessionEssentials = multi.Read<SessionEssentialsDto>().FirstOrDefault() ?? new SessionEssentialsDto();
        //                output.MedDoctor = multi.Read<Med_Doctor>().FirstOrDefault() ?? new Med_Doctor();
        //                output.AccountManager = multi.Read<Users>().FirstOrDefault() ?? new Users();
        //                output.ServiceProvider = multi.Read<ServiceProviderDto>().FirstOrDefault() ?? new ServiceProviderDto();

        //                try
        //                {
        //                    output.MasterLoactions = multi.Read<MasterLocationstDto>().ToList() ?? new List<MasterLocationstDto>();
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine(ex.Message);
        //                    output.MasterLoactions = new List<MasterLocationstDto>();
        //                }
        //            }
        //        }

        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return new GetSessionUserOutputDto();
        //    }
        //}

        #region Generic Methods

        public static bool ExecuteSP(string query, Dictionary<string, string> Params = null)
        {
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();
                SqlCommand cmd = GetSqlCommandWithParams(query, Connection, Params);
                int rowseffected = cmd.ExecuteNonQuery();
                Connection.Close();
                return true;
            }
        }

        public static async Task<bool> ExecuteSPAsync(string query, Dictionary<string, string> Params = null)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand cmd = GetSqlCommandWithParams(query, connection, Params);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
        }

        public static async Task<List<T>> GetListSPAsync<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);
            var results = new List<T>();
            var properties = GetReadableProperties<T>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                SqlCommand cmd = GetSqlCommandWithParams(query, connection, Params);
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = Activator.CreateInstance<T>();
                            foreach (var property in properties)
                            {
                                DBReader(item, property, reader);
                            }
                            results.Add(item);
                        }
                    }
                }
            }

            return results;
        }

        public static T GetSP<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);

            T t = Activator.CreateInstance<T>();
            List<PropertyInfo> properties = GetReadableProperties<T>();
            query = QueryMaker.SelectQuery<T>(query, whereClause);
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();
                SqlCommand cmd = GetSqlCommandWithParams(query, Connection, Params);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        foreach (var property in properties)
                        {
                            DBReader(t, property, reader);
                        }
                    }
                }
                Connection.Close();
            }
            return t;
        }

        public static async Task<T> GetSPAsync<T>(string query = null, string whereClause = null, Dictionary<string, string> Params = null)
        {
            query = QueryMaker.SelectQuery<T>(query, whereClause);

            T t = Activator.CreateInstance<T>();
            List<PropertyInfo> properties = GetReadableProperties<T>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                SqlCommand cmd = GetSqlCommandWithParams(query, connection, Params);
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            foreach (var property in properties)
                            {
                                DBReader(t, property, reader);
                            }
                        }
                    }
                }
            }

            return t;
        }

        public static int GetSPScaler(string query, Dictionary<string, string> Params = null)
        {
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();
                SqlCommand cmd = GetSqlCommandWithParams(query, Connection, Params);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                Connection.Close();
                return count;
            }
        }

        public static async Task<int> GetSPScalerAsync(string query, Dictionary<string, string> Params = null)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                SqlCommand cmd = GetSqlCommandWithParams(query, connection, Params);
                object result = await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
        }

        /// <summary>
        /// Multiple Result Sets with manual mapping
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="procedureName"></param>
        /// <param name="mapper"></param>
        /// <param name="Params"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        //public static async Task<TReturn> QueryMultipleAsync<TReturn>(
        //    string procedureName,
        //    Dictionary<string, string> Params,
        //    Func<SqlMapper.GridReader, Task<TReturn>> mapper,
        //    int? commandTimeout = null) where TReturn : class, new()
        //{
        //    try
        //    {
        //        using var connection = new SqlConnection(ConnectionString);
        //        await connection.OpenAsync();

        //        using var multi = await connection.QueryMultipleAsync(
        //            procedureName,
        //            GetDynamicParams(Params),
        //            commandType: CommandType.StoredProcedure,
        //            commandTimeout: commandTimeout);
        //        return await mapper(multi);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error executing {procedureName}: {ex.Message}");
        //        return new TReturn();
        //    }
        //}

        #endregion Generic Methods

        #region Private Helper Fuctions

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

        private static DynamicParameters GetDynamicParams(Dictionary<string, string> Params)
        {
            var parameters = new DynamicParameters();

            if (Params != null)
            {
                foreach (var param in Params)
                {
                    int length = param.Value?.Length ?? 0;
                    parameters.Add(param.Key, param.Value, size: GetOptimalSize(length));
                }
            }

            return parameters;
        }

        private static int GetOptimalSize(int length = 0)
        {    // Handle special small sizes first
            if (length <= 10) return 10;
            if (length <= 20) return 20;
            if (length <= 30) return 30;
            if (length <= 40) return 40;
            if (length <= 50) return 50;
            // Then proceed with normal 100-step increments
            return length <= 3900 ? ((length + 99) / 100) * 100 : length;
        }

        private static List<PropertyInfo> GetReadableProperties<T>()
        {
            return typeof(T).GetProperties().Where(x => !(Attribute.IsDefined(x, typeof(DontLoad)))).ToList();
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

        #endregion Private Helper Fuctions
    }
}