using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganizzeReports.Console.Utils
{
    public class QueryBuilder
    {
        private readonly List<string> _queryParameters = new List<string>();

        public void Add(string key, string value)
        {
            _queryParameters.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        public override string ToString()
        {
            return "?" + string.Join("&", _queryParameters);
        }
    }
}
