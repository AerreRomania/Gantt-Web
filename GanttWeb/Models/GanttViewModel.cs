using DlhSoft.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GanttWeb.Models
{
    public class GanttViewModel
    {
        public string OrderNumber { get; set; }
        public string Aim { get; set; }
        public string Department { get; set; }
        public string Client { get; set; }

        public bool IsTess { get; set; }
        public bool IsConf { get; set; }

        public bool IsStiro { get; set; }

        public List<ScheduleChartItem> LstSchedule { get; set; }
        public List<GanttChartItem> LstBars { get; set; }

        public object Items { get; set; }

    }
}
