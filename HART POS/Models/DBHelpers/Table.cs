using System;

namespace InstaCareEMR.Core.DBHelpers
{
    public class Table : Attribute
    {
        public string TableName { get; set; }
        public Table(string tableName)
        {
            this.TableName = tableName;
        }
    }
}
