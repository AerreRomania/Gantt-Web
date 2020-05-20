namespace GanttWeb.Controllers
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Collections.Generic;

    using Microsoft.AspNetCore.Mvc;

    using DlhSoft.Web.Mvc;
    using Microsoft.AspNetCore.Http;

    using GanttWeb.Models;

    public class GanttComponentsController : Controller
    {
        #region ScheduleChartView

        public ActionResult ScheduleChartViewConf(string CommessaName = "", string StartDate = "")
        {
            ViewBag.Commessa = PopulateCommessaConf();

            if (CommessaName == "<Reset>") CommessaName = string.Empty;
            if (CommessaName == null) CommessaName = string.Empty;

            TempData["comm"] = CommessaName;

            string date = StartDate;

            DateTime.TryParse(date, out var qDate);

            var strDate = string.Empty;
            if (qDate == DateTime.MinValue)
            {
                strDate = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{1}";
            }
            else
            {
                strDate = $"{qDate.Year}-{qDate.Month}-{qDate.Day}";
            }

            Utils.Common.DefaultStartDate = qDate;
            TempData["checkDate"] = qDate;

            DataSet ds = new DataSet();

            var commStr = CommessaName != string.Empty ? " and ordername='" + CommessaName + "' " : "";

            var conStr = @"
data source=knsql2014;initial catalog=Ganttproj; User ID=sa; password=onlyouolimpias;";
            /*query method verbatim*/
            var query = "select ordername,aim,flowstartdate,flowenddate,delflowstartdate,delflowenddate," +
                "loadedqty,prodqty,endProd,department,dvc,rdd from objects " +
                "where department<>'stiro' " + commStr +
                "order by department,len(aim),aim,flowstartdate";

            using (var con = new SqlConnection(conStr))
            {
                if (con.State == ConnectionState.Open) con.Close();
                var cmd = new SqlCommand(query, con);
                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter(cmd))
                {
                    sqlAdapter.Fill(ds);
                }
            }

            var lstSchedule = new List<ScheduleChartItem>();
            var lstBars = new List<GanttChartItem>();
            if (ds.Tables[0].Rows.Count <= 0) return View();
            //get line from the first record
            var deptFirst = ds.Tables[0].Rows[0][9].ToString().Split(' ');
            var oldLine = ds.Tables[0].Rows[0][1].ToString() + " " + deptFirst[1];
            DateTime MinimalDate = new DateTime(2015, 1, 1, 0, 0, 0, 0);

            if (CommessaName != string.Empty && CommessaName == "<Reset>") CommessaName = string.Empty;

            if (CommessaName != string.Empty && CommessaName != "<Reset>")
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var dept = row[0].ToString();

                    DateTime.TryParse(row[2].ToString(), out var start);
                    DateTime.TryParse(row[3].ToString(), out var end);

                    if (start > end || start.Equals(end)) continue;

                    DateTime.TryParse(row[4].ToString(), out var delStart);
                    DateTime.TryParse(row[5].ToString(), out var delEnd);
                    int.TryParse(row[6].ToString(), out var lQty);
                    int.TryParse(row[7].ToString(), out var pQty);
                    long.TryParse(row[8].ToString(), out var prodEndProvider);

                    var prodEnd = MinimalDate.AddTicks(prodEndProvider);
                    var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");

                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = CommessaName,
                        GanttChartItems = lstBars
                    });

                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = dept == "Stiro" ? "startedStatusBarStiro" : "startedStatusBar",
                        });
                    }

                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = dept,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                var orderitem = lstSchedule.ToArray();
                return View(orderitem);
            }

            /*master detail*/
            foreach (DataRow row in ds.Tables[0].Rows)
            {               
                //aim fields
                var dept = row[9].ToString().Split(' ');
                var line = row[1].ToString();

                line = line + " " + dept[1];

                DateTime.TryParse(row[2].ToString(), out var start);
                DateTime.TryParse(row[3].ToString(), out var end);

                //skip drawing any bar if roadblock has occured
                if (start > end || start.Equals(end)) continue;

                DateTime.TryParse(row[4].ToString(), out var delStart);
                DateTime.TryParse(row[5].ToString(), out var delEnd);
                int.TryParse(row[6].ToString(), out var lQty);
                int.TryParse(row[7].ToString(), out var pQty);
                long.TryParse(row[8].ToString(), out var prodEndProvider);

                long.TryParse(row[10].ToString(), out var dvc);
                long.TryParse(row[11].ToString(), out var rdd);

                //production endDateTime getting from ticks (db2)
                var prodEnd = MinimalDate.AddTicks(prodEndProvider);

                var ordername = row[0].ToString() + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString();

                var strCssClass = "startedStatusBar";
                if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                {
                    if (MinimalDate.AddTicks(dvc) < delEnd)
                    {
                        strCssClass = "errorStatusBar";
                    }
                }

                //var str = 

                //percentage assignment getting from targeted and effected qty (db2)
                var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%"); 

                if (oldLine != line)
                    //populate schedule list from the new line and first record
                {
                    lstSchedule.Add(new ScheduleChartItem 
                    {
                        Content = oldLine, 
                        GanttChartItems = lstBars 
                    });

                    oldLine = line;
                    lstBars = new List<GanttChartItem>();

                    // check if order is done to perform an ended bar css class
                    if (pQty == lQty)                    
                    {
                        lstBars.Add(new GanttChartItem 
                        { 
                            Content = ordername, 
                            Start = start, Finish = end,
                            BarCssClass = "endedStatusBar" 
                        });
                    }
                    else
                    // draw the active bar -> perform started bar css class
                    {
                        lstBars.Add(new GanttChartItem 
                        { 
                            Content = ordername, 
                            Start = start, Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent= ordername,
                        });
                    }

                    //skip delay drawings if roadblock has occured
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                else
                // continue populating schedule list for the current line
                {
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem 
                        { 
                            Content = ordername, 
                            Start = start, Finish = end,
                            BarCssClass = "endedStatusBar" 
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem 
                        { 
                            Content = ordername, 
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd, 
                            CompletedBarCssClass= "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent = "67%"
                        });
                    }
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem 
                    { 
                        Content = ordername, 
                        Start = delStart, 
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });                                       
                }
            }

            oldLine = "";
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                //aim fields
                var dept = row[9].ToString().Split(' ');
                var line = row[1].ToString();

                if (line != "LINEA12") continue;
                line = line + " " + dept[1];

                DateTime.TryParse(row[2].ToString(), out var start);
                DateTime.TryParse(row[3].ToString(), out var end);

                //skip drawing any bar if roadblock has occured
                if (start > end || start.Equals(end)) continue;

                DateTime.TryParse(row[4].ToString(), out var delStart);
                DateTime.TryParse(row[5].ToString(), out var delEnd);
                int.TryParse(row[6].ToString(), out var lQty);
                int.TryParse(row[7].ToString(), out var pQty);
                long.TryParse(row[8].ToString(), out var prodEndProvider);
                long.TryParse(row[10].ToString(), out var dvc);
                long.TryParse(row[11].ToString(), out var rdd);

                var ordername = row[0].ToString() + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString();

                var strCssClass = "startedStatusBar";
                if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                {
                    if (MinimalDate.AddTicks(dvc) < delEnd)
                    {
                        strCssClass = "errorStatusBar";
                    }
                }

                //production endDateTime getting from ticks (db2)
                var prodEnd = MinimalDate.AddTicks(prodEndProvider);

                //percentage assignment getting from targeted and effected qty (db2)
                var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");

                if (oldLine != line)
                //populate schedule list from the new line and first record
                {
                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = "LINEA12 B",
                        GanttChartItems = lstBars
                    });

                    oldLine = line;
                    lstBars = new List<GanttChartItem>();

                    // check if order is done to perform an ended bar css class
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername + ": ended",
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    // draw the active bar -> perform started bar css class
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername + ": started",
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent= ordername,
                        });
                    }

                    //skip delay drawings if roadblock has occured
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername + ":  issues",
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                else
                // continue populating schedule list for the current line
                {
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername + ": ended",
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername + ": started",
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent = "67%"
                        });
                    }
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername + ":  issues",
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
            }
            var items = lstSchedule.ToArray(); 

            return View(model: items);
        }

        public ActionResult ScheduleChartViewStiro(string CommessaName = "", string StartDate = "")
        {
            ViewBag.Commessa = PopulateCommessaStiro();
            //cek ugasio se fon, treba i za datume da prenesemo tu tempdata za stiro i conf
            if (CommessaName == "<Reset>") CommessaName = string.Empty;
            if (CommessaName == null) CommessaName = string.Empty;

            TempData["comm"] = CommessaName;


            string date = StartDate;

            DateTime.TryParse(date, out var qDate);

            var strDate = string.Empty;
            if (qDate == DateTime.MinValue)
            {
                //yyyy-MM-dd
                strDate = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{1}";
            }
            else
            {
                strDate = $"{qDate.Year}-{qDate.Month}-{qDate.Day}";
            }

            Utils.Common.DefaultStartDate = qDate;
            TempData["checkDate"] = qDate;

            var commStr = CommessaName != string.Empty ? " and ordername='" + CommessaName + "' " : " ";

            DataSet ds = new DataSet();

            var conStr = @"
data source=knsql2014;initial catalog=Ganttproj; User ID=sa; password=onlyouolimpias;";
            /*query method verbatim*/
            var query = "select ordername,aim,flowstartdate,flowenddate,delflowstartdate, delflowenddate,loadedqty,prodqty,endProd,department,locked,dvc,rdd from objects " +
                "where department='stiro' " + commStr  +
                "order by len(aim),aim";

            using (var con = new SqlConnection(conStr))
            {
                if (con.State == ConnectionState.Open) con.Close();
                var cmd = new SqlCommand(query, con);
                using SqlDataAdapter sqlAdapter = new SqlDataAdapter(cmd);
                sqlAdapter.Fill(ds);
            }

            var lstSchedule = new List<ScheduleChartItem>();
            var lstBars = new List<GanttChartItem>();
            if (ds.Tables[0].Rows.Count <= 0) return View();
            //get line from the first record
            var oldLine = ds.Tables[0].Rows[0][1].ToString();
            DateTime MinimalDate = new DateTime(2015, 1, 1, 0, 0, 0, 0);

            if (CommessaName != string.Empty && CommessaName == "<Reset>") CommessaName = string.Empty;

            if (CommessaName != string.Empty && CommessaName != "<Reset>")
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    
                    DateTime.TryParse(row[2].ToString(), out var start);
                    DateTime.TryParse(row[3].ToString(), out var end);

                    if (start > end || start.Equals(end)) continue;

                    DateTime.TryParse(row[4].ToString(), out var delStart);
                    DateTime.TryParse(row[5].ToString(), out var delEnd);
                    int.TryParse(row[6].ToString(), out var lQty);
                    int.TryParse(row[7].ToString(), out var pQty);
                    long.TryParse(row[8].ToString(), out var prodEndProvider);
                    long.TryParse(row[11].ToString(), out var dvc);
                    long.TryParse(row[12].ToString(), out var rdd);

                    var ordername = row[0].ToString() + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString();

                    var strCssClass = "startedStatusBar";
                    if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                    {
                        if (MinimalDate.AddTicks(dvc) < delEnd)
                        {
                            strCssClass = "errorStatusBar";
                        }
                    }

                    var dept = row[9].ToString();
                    var prodEnd = MinimalDate.AddTicks(prodEndProvider);
                    var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");

                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = CommessaName,
                        GanttChartItems = lstBars
                    });

                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = dept == "Stiro" ? strCssClass : "startedStatusBar",
                        });
                    }

                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                var orderitem = lstSchedule.ToArray();
                return View(orderitem);
            }

            var x = 0;
            /*master detail*/
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                
                //aim fields
                bool.TryParse(row[10].ToString(), out var lockd);
           
                var line = row[1].ToString();
                var realLine = row[1].ToString();
                if (x > 0) line = line + " " + x.ToString();
                
                DateTime.TryParse(row[2].ToString(), out var start);
                DateTime.TryParse(row[3].ToString(), out var end);

                //skip drawing any bar if roadblock has occured
                if (start > end || start.Equals(end)) continue;

                DateTime.TryParse(row[4].ToString(), out var delStart);
                DateTime.TryParse(row[5].ToString(), out var delEnd);
                int.TryParse(row[6].ToString(), out var lQty);
                int.TryParse(row[7].ToString(), out var pQty);
                long.TryParse(row[8].ToString(), out var prodEndProvider);
                long.TryParse(row[11].ToString(), out var dvc);
                long.TryParse(row[12].ToString(), out var rdd);

                var ordername = row[0].ToString() + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString();
                var strCssClass = "startedStatusBar";
                if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                {
                    if (MinimalDate.AddTicks(dvc) < delEnd)
                    {
                        strCssClass = "errorStatusBar";
                    }
                }

                //production endDateTime getting from ticks (db2)
                var prodEnd = MinimalDate.AddTicks(prodEndProvider);

                //percentage assignment getting from targeted and effected qty (db2)
                var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");

                if (oldLine != line)
                //populate schedule list from the new line and first record
                {
                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = oldLine,
                        GanttChartItems = lstBars
                    });

                    oldLine = line;
                    lstBars = new List<GanttChartItem>();

                    // check if order is done to perform an ended bar css class
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    // draw the active bar -> perform started bar css class
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent = ordername,
                        });
                    }

                    //skip delay drawings if roadblock has occured
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                else
                // continue populating schedule list for the current line
                {
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent = "67%"
                        });
                    }
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                if (lockd) x++;
            }
            oldLine = "";
            foreach (DataRow row in ds.Tables[0].Rows)
            {

                //aim fields
                bool.TryParse(row[10].ToString(), out var lockd);

                var line = row[1].ToString();
                if (line != "LINEA3") continue;
                var realLine = row[1].ToString();
                if (x > 0) line = line + " " + x.ToString();

                DateTime.TryParse(row[2].ToString(), out var start);
                DateTime.TryParse(row[3].ToString(), out var end);

                //skip drawing any bar if roadblock has occured
                if (start > end || start.Equals(end)) continue;

                DateTime.TryParse(row[4].ToString(), out var delStart);
                DateTime.TryParse(row[5].ToString(), out var delEnd);
                int.TryParse(row[6].ToString(), out var lQty);
                int.TryParse(row[7].ToString(), out var pQty);
                long.TryParse(row[8].ToString(), out var prodEndProvider);
                long.TryParse(row[11].ToString(), out var dvc);
                long.TryParse(row[12].ToString(), out var rdd);

                var ordername = row[0].ToString() + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString();
                var strCssClass = "startedStatusBar";
                if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                {
                    if (MinimalDate.AddTicks(dvc) < delEnd)
                    {
                        strCssClass = "errorStatusBar";
                    }
                }
                //production endDateTime getting from ticks (db2)
                var prodEnd = MinimalDate.AddTicks(prodEndProvider);

                //percentage assignment getting from targeted and effected qty (db2)
                var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");

                if (oldLine != line)
                //populate schedule list from the new line and first record
                {
                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = "LINEA3",
                        GanttChartItems = lstBars
                    });

                    oldLine = line;
                    lstBars = new List<GanttChartItem>();

                    // check if order is done to perform an ended bar css class
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    // draw the active bar -> perform started bar css class
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent = ordername,
                        });
                    }

                    //skip delay drawings if roadblock has occured
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                else
                // continue populating schedule list for the current line
                {
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = ordername,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass,
                            //AssignmentsContent = "67%"
                        });
                    }
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = ordername,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                if (lockd) x++;
            }
            var items = lstSchedule.ToArray();

            return View(model: items);
        }

        #endregion

        #region ScheduleChartViewDeptAll


        /*bool checkTess = false, bool checkConf = false, bool checkStiro = false, string StartDate = "", string CommessaName = ""*/
        public ActionResult ScheduleChartViewDeptAll(bool checkTess = false, bool checkConf = false, bool checkStiro = false, string StartDate = "", string CommessaName = "")
        {
            ViewBag.Commessa = PopulateCommessa();

            if (CommessaName == "<Reset>") CommessaName = string.Empty;
            if (CommessaName == null) CommessaName = string.Empty;

            DataSet ds = new DataSet();

            var sb = new System.Text.StringBuilder();

            bool checkStiroB = Convert.ToBoolean(checkStiro);
            bool checkTessB = Convert.ToBoolean(checkTess);
            bool checkConfB = Convert.ToBoolean(checkConf);
         
            string date = StartDate;

            DateTime.TryParse(date, out var qDate);

            var strDate = string.Empty;

            if (qDate == DateTime.MinValue)
            {
                strDate = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{1}";                
            }
            else
            {
                strDate = $"{qDate.Year}-{qDate.Month}-{qDate.Day}";
            }

            Utils.Common.DefaultStartDate = qDate;
            TempData["checkDate"] = qDate;

            TempData["checkStiroB"] = checkStiro;
            TempData["checkConfB"] = checkConf;
            TempData["checkTessB"] = checkTess;
            TempData["comm"] = CommessaName;
            //TempData["CommessaNameB"] = commessa;

            if (CommessaName == "<Reset>" || CommessaName == "")
            {
                if (checkConfB == true && checkStiroB  == false)
                {
                    sb.Append("where department in ('Confezione A','Confezione B') and flowstartdate>='" + strDate + "' ");
                }
                else if (checkStiroB == true && checkConfB == false)
                {
                    sb.Append("where department in ('Stiro') and flowstartdate>='" + strDate + "' ");
                }
                else if (checkStiroB == true && checkConfB == true)
                {
                    sb.Append("where department in ('Confezione A','Confezione B','Stiro') and flowstartdate>='" + strDate + "' ");
                }
                else
                {
                    sb.Append("where FlowStartDate>='" + strDate + "' ");
                }
            }
            else
            {
                sb.Append("where ordername='" + CommessaName + "' ");
            }
            
            var conStr = @"
data source=knsql2014;initial catalog=Ganttproj; User ID=sa; password=onlyouolimpias;";
            //where ordername='20A.55760
            var query = "select department,ordername,flowstartdate,flowenddate,delflowstartdate,delflowenddate,loadedqty,prodqty,endProd,dvc,rdd from objects " + sb.ToString() +
                " order by ordername,flowstartdate";

            using (var con = new SqlConnection(conStr))
            {
                if (con.State == ConnectionState.Open) con.Close();
                var cmd = new SqlCommand(query, con);
                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter(cmd))
                {
                    sqlAdapter.Fill(ds);
                }
            }

            var lstSchedule = new List<ScheduleChartItem>();
            var lstBars = new List<GanttChartItem>();

            if (ds.Tables[0].Rows.Count <= 0) return View();

            var ordFirst = ds.Tables[0].Rows[0][1].ToString();
            DateTime MinimalDate = new DateTime(2015, 1, 1, 0, 0, 0, 0);

            if (CommessaName != string.Empty && CommessaName == "<Reset>") CommessaName = string.Empty;

            if (CommessaName != string.Empty && CommessaName!="<Reset>")
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    DateTime.TryParse(row[2].ToString(), out var start);
                    DateTime.TryParse(row[3].ToString(), out var end);

                    if (start > end || start.Equals(end)) continue;

                    DateTime.TryParse(row[4].ToString(), out var delStart);
                    DateTime.TryParse(row[5].ToString(), out var delEnd);
                    int.TryParse(row[6].ToString(), out var lQty);
                    int.TryParse(row[7].ToString(), out var pQty);
                    long.TryParse(row[8].ToString(), out var prodEndProvider);
                    
                    long.TryParse(row[9].ToString(), out var dvc);
                    long.TryParse(row[10].ToString(), out var rdd);

                    var dp = row[0].ToString();
                    if (dp == "Stiro") dp = "Stiro " + '_';
                    var dept = row[0].ToString() + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString();

                    var prodEnd = MinimalDate.AddTicks(prodEndProvider);
                    //var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");

                    var strCssClass = dept == "Stiro _" ? "startedStatusBarStiro" : "startedStatusBar";
                    if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                    {
                        if (MinimalDate.AddTicks(dvc) < delEnd)
                        {
                            strCssClass = "errorStatusBar";
                        }
                    }

                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = CommessaName,
                        GanttChartItems = lstBars
                    });

                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept,
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept,
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass
                        });
                    }

                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = dept,
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                var orderitem = lstSchedule.ToArray();
                return View(orderitem);
            }

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                var ordername = row[1].ToString();

                //aim fields

                DateTime.TryParse(row[2].ToString(), out var start);
                DateTime.TryParse(row[3].ToString(), out var end);

                //skip drawing any bar if roadblock has occured
                if (start > end || start.Equals(end)) continue;

                DateTime.TryParse(row[4].ToString(), out var delStart);
                DateTime.TryParse(row[5].ToString(), out var delEnd);
                int.TryParse(row[6].ToString(), out var lQty);
                int.TryParse(row[7].ToString(), out var pQty);
                long.TryParse(row[8].ToString(), out var prodEndProvider);

                long.TryParse(row[9].ToString(), out var dvc);
                long.TryParse(row[10].ToString(), out var rdd);

                var dept = row[0].ToString();
                if (dept == "Stiro") dept = "Stiro " + '_';

                var prodEnd = MinimalDate.AddTicks(prodEndProvider);    //production endDateTime getting from ticks (db2)

                //percentage assignment getting from targeted and effected qty (db2)
                var assignment = string.Format("{0}{1}", Convert.ToInt32(pQty / lQty * 100), "%");


                var strCssClass = dept == "Stiro _" ? "startedStatusBarStiro" : "startedStatusBar";
                if (MinimalDate.AddTicks(dvc) > MinimalDate.AddDays(+50))
                {
                    if (MinimalDate.AddTicks(dvc) < delEnd)
                    {
                        strCssClass = "errorStatusBar";
                    }
                }
                if (ordFirst != ordername)
                //populate schedule list from the new line and first record
                {
                    lstSchedule.Add(new ScheduleChartItem
                    {
                        Content = ordFirst,
                        GanttChartItems = lstBars
                    });

                    ordFirst = ordername;
                    lstBars = new List<GanttChartItem>();

                    // check if order is done to perform an ended bar css class
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString(),
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    // draw the active bar -> perform started bar css class
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString(),
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass
                            //AssignmentsContent = ordername,
                        });
                    }

                    //skip delay drawings if roadblock has occured
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = dept + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString(),
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
                else
                // continue populating schedule list for the current line
                {
                    if (pQty == lQty)
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString(),
                            Start = start,
                            Finish = end,
                            BarCssClass = "endedStatusBar"
                        });
                    }
                    else
                    {
                        lstBars.Add(new GanttChartItem
                        {
                            Content = dept + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString(),
                            Start = start,
                            Finish = end,
                            CompletedFinish = prodEnd,
                            CompletedBarCssClass = "completedStatusBar",
                            BarCssClass = strCssClass
                            //AssignmentsContent = "67%"
                        });
                    }
                    if (delStart > delEnd || delStart.Equals(delEnd)) continue;
                    lstBars.Add(new GanttChartItem
                    {
                        Content = dept + ' ' + lQty.ToString() + ' ' + pQty.ToString() + ' ' + MinimalDate.AddTicks(dvc).ToShortDateString() + ' ' + MinimalDate.AddTicks(rdd).ToShortDateString(),
                        Start = delStart,
                        Finish = delEnd,
                        BarCssClass = "issuesStatusBar"
                    });
                }
            }

            var items = lstSchedule.ToArray();
            return View(items);

        }

       
        #endregion

        #region ScheduleChartViewTess

        public ActionResult ScheduleChartViewTess()
        {
            return View();
        }

        #endregion

        #region GetCommese
        private static List<Commessa> PopulateCommessa()
        {
            List<Commessa> commessa = new List<Commessa>();
            commessa.Add(new Commessa { CommessaName = "<Reset>", CommessaId = 0 });
            string constr = @"
data source=knsql2014;initial catalog=Ganttproj; User ID=sa; password=onlyouolimpias;";
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = "SELECT Id,ordername FROM objects";
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            commessa.Add(new Commessa
                            {
                                CommessaName = sdr["ordername"].ToString(),
                                CommessaId = Convert.ToInt32(sdr["Id"])
                            });
                        }
                    }
                    con.Close();
                }
            }

            return commessa;
        }

        //Ubaciti where u query gde je dep=conf b
        private static List<Commessa> PopulateCommessaConf()
        {
            List<Commessa> commessa = new List<Commessa>();
            commessa.Add(new Commessa { CommessaName = "<Reset>", CommessaId = 0 });
            string constr = @"
data source=knsql2014;initial catalog=Ganttproj; User ID=sa; password=onlyouolimpias;";
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = "SELECT Id,ordername FROM objects where department='Confezione B' or department='Confezione A'";
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            commessa.Add(new Commessa
                            {
                                CommessaName = sdr["ordername"].ToString(),
                                CommessaId = Convert.ToInt32(sdr["Id"])
                            });
                        }
                    }
                    con.Close();
                }
            }

            return commessa;
        }

        //Ubaciti where u query gde je dep=stiro

        private static List<Commessa> PopulateCommessaStiro()
        {
            List<Commessa> commessa = new List<Commessa>();
            commessa.Add(new Commessa { CommessaName = "<Reset>", CommessaId = 0 });
            string constr = @"
data source=knsql2014;initial catalog=Ganttproj; User ID=sa; password=onlyouolimpias;";
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = "SELECT Id,ordername FROM objects where department='Stiro'";
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            commessa.Add(new Commessa
                            {
                                CommessaName = sdr["ordername"].ToString(),
                                CommessaId = Convert.ToInt32(sdr["Id"])
                            });
                        }
                    }
                    con.Close();
                }
            }

            return commessa;
        }
        #endregion
    }
}