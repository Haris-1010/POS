using System.Collections.Generic;

namespace InstaCareEMR.Core.DBHelpers
{
    public class PageListObject<T>
    {
        public Pager pager { get; set; }
        public List<T> Results { get; set; }
    }
}