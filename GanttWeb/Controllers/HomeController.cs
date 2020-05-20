namespace GanttWeb.Controllers
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using GanttWeb.Models;
    using GanttWeb.Utils;

    using System.Data;
    using System.Data.SqlClient;
    using System;

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            Common.MyDataSet = new DataSet();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Index([Bind("StartDate")] CaricoViewModel viewModel) 
        {
            var conStr = @"
data source=192.168.96.37;initial catalog=ONLYOU; User ID=nicu; password=onlyouolimpias;";
            /*query method verbatim*/
            var query = @"
select                     
Comenzi.NrComanda as Commessa,
Articole.Articol as Articole,
Comenzi.Line as Linea,
Comenzi.Cantitate as Qty,
Comenzi.Carico as Carico,
Comenzi.Diff_t as Diff,
Convert(date,Comenzi.DataLansare,110) as Arrivo,
Convert(date,Comenzi.DataProduzione,110) as Produzione,
Convert(date,Comenzi.DataFine,110) as Fine,
Convert(date,Comenzi.DataLivrare,110) As Consegia,
Convert(date,Comenzi.RDD,110) as Rdd,
Convert(date,Comenzi.DVC,110) as Dvc,
comenzi.IdStare,  
comenzi.Department,  
comenzi.note
from Comenzi
inner join Articole on Comenzi.IdArticol = Articole.Id 
where comenzi.dataLivrare is not null and comenzi.dataLivrare<>'' 
order by len(comenzi.department),comenzi.Department, Convert(date,Comenzi.DataLivrare,110) desc";

            var finalDs = new DataSet();

            using (var con = new SqlConnection(conStr))
            {
                if (con.State == ConnectionState.Open) con.Close();
                var cmd = new SqlCommand(query, con);

                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter(cmd))
                {
                    sqlAdapter.Fill(finalDs);
                }
                cmd = null;
            }

            Common.MyDataSet = new DataSet("myDataset");
            Common.MyDataSet.Tables.Add("Comenzi");
            Common.MyDataSet.Tables[0].Columns.Add("Commessa");
            Common.MyDataSet.Tables[0].Columns.Add("Articole");
            Common.MyDataSet.Tables[0].Columns.Add("Linea");
            Common.MyDataSet.Tables[0].Columns.Add("Qty");
            Common.MyDataSet.Tables[0].Columns.Add("Carico");
            Common.MyDataSet.Tables[0].Columns.Add("Diff");
            Common.MyDataSet.Tables[0].Columns.Add("Arrivo",typeof(string));
            Common.MyDataSet.Tables[0].Columns.Add("Produzione", typeof(string));
            Common.MyDataSet.Tables[0].Columns.Add("Fine", typeof(string));
            Common.MyDataSet.Tables[0].Columns.Add("Consegia", typeof(string));
            Common.MyDataSet.Tables[0].Columns.Add("Rdd",typeof(string));
            Common.MyDataSet.Tables[0].Columns.Add("Dvc",typeof(string));
            Common.MyDataSet.Tables[0].Columns.Add("Status");
            Common.MyDataSet.Tables[0].Columns.Add("Department");
            Common.MyDataSet.Tables[0].Columns.Add("Note");
            /*master detail*/
            foreach (DataRow row in finalDs.Tables[0].Rows)
            {
                var dataRow = Common.MyDataSet.Tables[0].NewRow();
                DateTime.TryParse(row[6].ToString(), out var Arrivo);
                DateTime.TryParse(row[7].ToString(), out var Produzione);
                DateTime.TryParse(row[8].ToString(), out var Fine);
                DateTime.TryParse(row[9].ToString(), out var Consegia);
                DateTime.TryParse(row[10].ToString(), out var Rdd);
                DateTime.TryParse(row[11].ToString(), out var Dvc);

                if (viewModel.StartDate != DateTime.MinValue
                  && Consegia < viewModel.StartDate) continue;

                dataRow[0] = row[0].ToString();
                dataRow[1] = row[1].ToString();
                dataRow[2] = row[2].ToString();
                dataRow[3] = row[3].ToString();
                dataRow[4] = row[4].ToString();
                dataRow[5] = row[5].ToString();
                dataRow[6] = Arrivo.ToString("yyyy-MM-dd");
                dataRow[7] = Produzione.ToString("yyyy-MM-dd");
                dataRow[8] = Fine.ToString("yyyy-MM-dd");
                dataRow[9] = Consegia.ToString("yyyy-MM-dd");
                dataRow[10] = Rdd.ToString("yyyy-MM-dd");
                dataRow[11] = Dvc.ToString("yyyy-MM-dd");
                dataRow[12] = row[12].ToString();
                dataRow[13] = row[13].ToString();
                dataRow[14] = row[14].ToString();

                Common.MyDataSet.Tables[0].Rows.Add(dataRow);               
            }

            return View();
        }
         
        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache
            (
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel 
            {
                RequestId = Activity.Current?.Id 
                ?? HttpContext.TraceIdentifier 
            });
        }
    }
}
