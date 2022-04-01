using System.Collections.Generic;
namespace SmartPulse.Models
{
    public class ResultTable
    {
        public List<TableValue> TableValues { get; set; }

        public ResultTable()
        {
            TableValues = new List<TableValue>();
        }
    }
}
