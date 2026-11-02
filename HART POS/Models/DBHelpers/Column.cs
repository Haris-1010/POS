using System;

namespace InstaCareEMR.Core.DBHelpers
{
    internal class Column : Attribute
    {
        public string Name { get; set; }
        public Column(string name)
        {
            this.Name = name;
        }
    }
}
