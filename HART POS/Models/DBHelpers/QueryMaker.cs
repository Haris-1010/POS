using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace InstaCareEMR.Core.DBHelpers
{
    public class QueryMaker
    {
        #region Public methods

        public static string InsertQueryWithID(string tableName, Dictionary<string, string> dataset)
        {
            return $"{InsertQuery(tableName, dataset)};Select Scope_Identity()";
        }

        public static string SelectQuery<T>(string query, string where_clause)
        {
            StringBuilder queryBuilder = new StringBuilder();
            if (string.IsNullOrWhiteSpace(query))
            {
                List<PropertyInfo> _properties = GetReadableProperties<T>();
                queryBuilder.Append("SELECT ");
                foreach (var element in _properties)
                {
                    queryBuilder.Append($"[{element.Name}], ");
                }
                queryBuilder.Length -= 2;
                queryBuilder.Append($"FROM {GetSchemaName<T>()}.{GetTableName<T>()} {where_clause}");
            }
            else
            {
                queryBuilder.Append(query);
                queryBuilder.Append($" {where_clause} ");
            }
            return queryBuilder.ToString();
        }

        public static string InsertQuery<T>(T _Object, string schemaName = null, string tableName = null)
        {
            List<PropertyInfo> properties = GetInsertProperties<T>();
            StringBuilder ColumnQueryBuilder = new StringBuilder();
            StringBuilder ValuesQueryBuilder = new StringBuilder();
            ColumnQueryBuilder.Append($"INSERT INTO {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()}");
            ColumnQueryBuilder.Append("(");
            ValuesQueryBuilder.Append(" VALUES (");
            foreach (var pi in properties)
            {
                ColumnQueryBuilder.Append($"{GetColumnName(pi)} , ");
                ValuesQueryBuilder.Append($"{ValueReader(pi.GetValue(_Object))}, ");
            }
            ColumnQueryBuilder.Length -= 2;
            ColumnQueryBuilder.Append(")");
            ValuesQueryBuilder.Length -= 2;
            ValuesQueryBuilder.Append(")");
            ColumnQueryBuilder.Append(ValuesQueryBuilder.ToString());
            return ColumnQueryBuilder.ToString();
        }

        public static string InsertQueryWithID<T>(T _Object, string schemaName = null, string tableName = null)
        {
            List<PropertyInfo> properties = GetInsertProperties<T>();
            StringBuilder ColumnQueryBuilder = new StringBuilder();
            StringBuilder ValuesQueryBuilder = new StringBuilder();
            ColumnQueryBuilder.Append($"INSERT INTO {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()}");
            ColumnQueryBuilder.Append("(");
            ValuesQueryBuilder.Append(" VALUES (");
            foreach (var pi in properties)
            {
                ColumnQueryBuilder.Append($"{GetColumnName(pi)} , ");
                ValuesQueryBuilder.Append($"{ValueReader(pi.GetValue(_Object))}, ");
            }
            ColumnQueryBuilder.Length -= 2;
            ColumnQueryBuilder.Append(")");
            ValuesQueryBuilder.Length -= 2;
            ValuesQueryBuilder.Append(")");
            ColumnQueryBuilder.Append(ValuesQueryBuilder.ToString());
            string q = ColumnQueryBuilder.ToString();
            q += "    ; Select Scope_Identity();";
            return q;
        }

        public static string InsertQuery(string table, Dictionary<string, string> dataset)
        {
            StringBuilder InsertQueryBuilder = new StringBuilder();
            StringBuilder ColumnQueryBuilder = new StringBuilder();
            StringBuilder ValuesQueryBuilder = new StringBuilder();
            InsertQueryBuilder.Append($"Insert into {table}");
            ColumnQueryBuilder.Append("(");
            ValuesQueryBuilder.Append("VALUES (");
            foreach (var col in dataset)
            {
                ColumnQueryBuilder.Append($"{col.Key.ToString()}, ");
                ValuesQueryBuilder.Append($"{ValueReader(col.Value)}, ");
            }
            ColumnQueryBuilder.Length -= 2;
            ColumnQueryBuilder.Append(")");
            ValuesQueryBuilder.Length -= 2;
            ValuesQueryBuilder.Append(")");
            InsertQueryBuilder.Append(ColumnQueryBuilder.ToString() + ValuesQueryBuilder.ToString() + ";").ToString();
            return InsertQueryBuilder.ToString();
        }

        public static string InsertListQuery<T>(IEnumerable<T> objects, string schemaName = null, string tableName = null)
        {
            List<PropertyInfo> properties = GetInsertProperties<T>();
            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append($"INSERT INTO {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()} (");

            foreach (var pi in properties)
            {
                queryBuilder.Append($"{GetColumnName(pi)}, ");
            }
            queryBuilder.Length -= 2;
            queryBuilder.Append(") VALUES ");

            foreach (var obj in objects)
            {
                queryBuilder.Append("(");
                foreach (var pi in properties)
                {
                    queryBuilder.Append($"{ValueReader(pi.GetValue(obj))}, ");
                }
                queryBuilder.Length -= 2;
                queryBuilder.Append("), ");
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(";");

            return queryBuilder.ToString();
        }

        public static string UpdateQuery<T>(T _Object, string whereClause, string schemaName = null, string tableName = null)
        {
            List<PropertyInfo> properties = GetUpdateProperties<T>();
            StringBuilder UpdateQueryBuilder = new StringBuilder();
            UpdateQueryBuilder.Append($"UPDATE {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()} SET ");
            foreach (var pi in properties)
            {
                UpdateQueryBuilder.Append($"{GetColumnName(pi)} = {ValueReader(pi.GetValue(_Object))}, ");
            }
            UpdateQueryBuilder.Length -= 2;
            UpdateQueryBuilder.Append($" {whereClause}");
            return UpdateQueryBuilder.ToString();
        }

        public static string UpdateQuery<T>(T _Object, List<PropertyInfo> properties, string whereClause, string schemaName = null, string tableName = null)
        {
            StringBuilder UpdateQueryBuilder = new StringBuilder();
            UpdateQueryBuilder.Append($"UPDATE {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()} SET ");
            foreach (var pi in properties)
            {
                UpdateQueryBuilder.Append($"{GetColumnName(pi)} = {ValueReader(pi.GetValue(_Object))}, ");
            }
            UpdateQueryBuilder.Length -= 2;
            UpdateQueryBuilder.Append($" {whereClause}");
            return UpdateQueryBuilder.ToString();
        }

        public static string UpdateQuery<T>(Dictionary<string, string> ColumnAndValues, string WhereClause, string schemaName = null, string tableName = null)
        {
            StringBuilder UpdateQueryBuilder = new StringBuilder();
            UpdateQueryBuilder.Append($"UPDATE {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()} SET ");
            foreach (var pi in ColumnAndValues)
            {
                UpdateQueryBuilder.Append($"{pi.Key} = {pi.Value}, ");
            }
            UpdateQueryBuilder.Length -= 2;
            UpdateQueryBuilder.Append($" {WhereClause}");
            return UpdateQueryBuilder.ToString();
        }

        public static string UpdateQuery(string table, Dictionary<string, string> dataset, string whereClause)
        {
            StringBuilder UpdateQueryBuilder = new StringBuilder();
            UpdateQueryBuilder.Append($"Update {table} SET ");
            foreach (var col in dataset)
            {
                UpdateQueryBuilder.Append($"{col.Key} = {ValueReader(col.Value)}, ");
            }
            UpdateQueryBuilder.Length -= 2;
            UpdateQueryBuilder.Append($" {whereClause};");
            return UpdateQueryBuilder.ToString();
        }

        public static string DeleteQuery<T>(string whereClause, string schemaName = null, string tableName = null)
        {
            return $"Delete from {schemaName ?? GetSchemaName<T>()}.{tableName ?? GetTableName<T>()} {whereClause};";
        }

        public static string BeginTransQuery()
        {
            string query = string.Empty;
            query +=
                "BEGIN TRY " +
                "BEGIN TRANSACTION ";
            return query;
        }

        public static string CommitTransQuery()
        {
            string query = string.Empty;
            query +=
                "COMMIT " +
                "END TRY " +
                "BEGIN CATCH " +
                "declare @ErrorMessage nvarchar(max), @ErrorSeverity int, @ErrorState int;" +
                "select @ErrorMessage = ERROR_MESSAGE() + ' Line ' + cast(ERROR_LINE() as nvarchar(5)), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();" +
                "rollback transaction;" +
                "raiserror(@ErrorMessage, @ErrorSeverity, @ErrorState);" +
                "END CATCH ";
            return query;
        }

        public static PagedQuery GetPagerQueries<T>(string query, string sortBy, string order, int offset, int numberOfRecords)
        {
            string mainquery = $"SELECT * FROM ({query ?? SelectQuery<T>(query, "")}) MyList order by {sortBy} {order} offset {offset} rows fetch next {numberOfRecords} rows only";
            string countquery = $"SELECT COUNT(*) FROM ({query ?? SelectQuery<T>(query, "")}) MyList";
            return new PagedQuery { Query = mainquery, CountQuery = countquery };
        }

        public static PagedQuery GetPagerQueries<T>(string query, Dictionary<string, string> sortAndOrder, int offset, int numberOfRecords)
        {
            string mainquery = $"SELECT * FROM ({query ?? SelectQuery<T>(query, "")}) MyList {MultipleOrderByQuery(sortAndOrder, numberOfRecords, offset)}";
            string countquery = $"SELECT COUNT(*) FROM ({query ?? SelectQuery<T>(query, "")}) MyList";
            return new PagedQuery { Query = mainquery, CountQuery = countquery };
        }

        #endregion Public methods

        #region Private methods

        private static List<PropertyInfo> GetReadableProperties<T>()
        {
            return typeof(T).GetProperties().Where(x => !(Attribute.IsDefined(x, typeof(DontLoad)))).ToList();
        }

        private static List<PropertyInfo> GetInsertProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList().Where(x => !Attribute.IsDefined(x, typeof(DontInsert))).ToList();
        }

        private static List<PropertyInfo> GetUpdateProperties<T>()
        {
            return typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).ToList().Where(x => !(Attribute.IsDefined(x, typeof(DontUpdate)))).ToList();
        }

        private static string GetColumnName(PropertyInfo pi)
        {
            if (Attribute.IsDefined(pi, typeof(Column)))
            {
                Column k = (Column)Attribute.GetCustomAttribute(pi, typeof(Column));
                return $"[{k.Name}]";
            }
            else
            {
                return $"[{pi.Name}]";
            }
        }

        private static string GetTableName<T>()
        {
            if (Attribute.IsDefined(typeof(T), typeof(Table)))
            {
                Table t = (Table)Attribute.GetCustomAttribute(typeof(T), typeof(Table));
                return $"[{t.TableName}]";
            }
            else
            {
                return $"[{typeof(T).Name}]";
            }
        }

        private static string GetSchemaName<T>()
        {
            if (Attribute.IsDefined(typeof(T), typeof(Schema)))
            {
                Schema t = (Schema)Attribute.GetCustomAttribute(typeof(T), typeof(Schema));
                return $"[{t.SchemaName}]";
            }
            else
            {
                return "[dbo]";
            }
        }

        private static string ValueReader(Object value)
        {
            if (value == null)
            {
                return "NULL";
            }
            else if (value.GetType() == typeof(DateTime))
            {
                DateTime date = (DateTime)value;
                return $"'{date.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else
            {
                return $"'{value.ToString().Replace("\'", "'\'\'")}'";
            }
        }

        private static string MultipleOrderByQuery(Dictionary<string, string> sortByAndOrder, int numberOfRecords, int offset)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" order by ");
            foreach (var element in sortByAndOrder)
            {
                sb.Append(element.Key);
                sb.Append($" {element.Value}, ");
            }
            sb.Length -= 2;
            sb.Append($" offset {offset} rows fetch next {numberOfRecords} rows only");
            return sb.ToString();
        }

        #endregion Private methods
    }
}