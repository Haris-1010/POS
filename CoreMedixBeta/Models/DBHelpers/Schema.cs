using System;

namespace InstaCareEMR.Core.DBHelpers
{
    public class Schema : Attribute
    {
        public string SchemaName { get; set; }
        public Schema(string schemaName)
        {
            this.SchemaName = schemaName;
        }
    }
}