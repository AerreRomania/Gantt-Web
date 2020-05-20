namespace GanttWeb.Utils
{
    using DlhSoft.Web.Mvc;
    using System;

    public abstract class Common
    {
        public static string UserJobType { get; set; }

        public static DateTime StartDate { get; set; }

        public static System.Data.DataSet MyDataSet { get; set; }

        public static ScheduleChartItem[] Items { get; set; }

        public static DateTime DefaultStartDate { get; set; }
    }
}
