using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartPulse.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SmartPulse.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }



        //Uygulama başladığında çalışan fonksiyon.
        public async Task<IActionResult> Index()
        {
            //GetTradeHistory fonksiyonu ile api'den veriler alınıp listeye çevirilerek return ediliyor.

            var tradeHistories = GetTradeHistory();
            if (tradeHistories != null)
            {
                //PB ile başlamayan ve aynı concrat değerine sahip veriler gruplanıyor.

                var group =
                               from tradeHistory in tradeHistories
                               where tradeHistory.Conract[1] != 'B'
                               group tradeHistory by tradeHistory.Conract into newGroup
                               orderby newGroup.Key
                               select newGroup;

                //Tabloda gösterilecek değerler hesaplanıyor ve ResulTable nesnesi olarak return ediliyor.

                var resultTable = CalculateResultTableValues(group.ToList());

                //Tabloda gösterilecek veriler liste halinde View'a gönderiliyor.

                return View(resultTable.TableValues);
            }

            return NotFound();

        }

        //CalculateResultTableValues fonksiyonunda tabloda gösterilecek değerler hesaplanıyor ve ResulTable nesnesi olarak return ediliyor.
        public ResultTable CalculateResultTableValues(List<IGrouping<string, TradeHistory>> group)
        {
            //Aynı concrat değerine sahip verilerin toplam değerleri bulunabilmek için gerekli değişkenler tanımlanıyor.

            ResultTable resultTable = new ResultTable();
            double toplamIslemMiktarı;
            double toplamIslemTutarı;
            double agirlikOrtalamaFiyat;
            string date;

            //Gruplanmış concrat değerlerine iç içe 2 döngü ile ulaşılıyor.

            foreach (var history in group)
            {
                //Her iç döngüden çıkışında yeni bir concrat grubuna geçeceği için değişkenler 0' a eşitleniyor.

                toplamIslemTutarı = 0;
                toplamIslemMiktarı = 0;
                agirlikOrtalamaFiyat = 0;
                date = "";


                foreach (var tradeHistory in history)
                {

                    //Aynı değere sahip concratların toplam sonuçları hesaplanıyor.

                    date = tradeHistory.Conract;
                    toplamIslemMiktarı += Convert.ToDouble(tradeHistory.Quantity) / 10;
                    toplamIslemTutarı +=
                        (Convert.ToDouble(tradeHistory.Price) * Convert.ToDouble(tradeHistory.Quantity)) / 10;

                }

                /*Her concrat grubu için hesaplanan toplam değerler View'da gösterebilmek için ResultTable
                sınıfı içerisindeki tableValues listesine ekleniyor.*/

                agirlikOrtalamaFiyat = toplamIslemTutarı / toplamIslemMiktarı;
                resultTable.TableValues.Add(new TableValue()
                {
                    Tarih = ChangeDateFormat(date),
                    AgirlikliOrtalamaFiyat = string.Format("{0:0,0}", Convert.ToInt32(agirlikOrtalamaFiyat)).Replace(",", "."),
                    ToplamIslemMIktari = string.Format("{0:0,0}", Convert.ToInt32(toplamIslemMiktarı)).Replace(",", "."),
                    ToplamIslemTutari = string.Format("{0:0,0}", Convert.ToInt32(toplamIslemTutarı)).Replace(",", "."),
                });
                

            }

            return resultTable;
        }




        //GetTradeHistory fonksiyonu ile api'den veriler alınıp TradeHistory modeli listsine çevirilerek return ediliyor.
        public List<TradeHistory> GetTradeHistory()
        {
            using (var client = new HttpClient())
            {
                //Api'ye parametre olarak gönderilecek date değeri doğru formata çevriliyor.

                string endDate = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" +
                                 DateTime.Now.Day.ToString();
                string startDate = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" +
                                   DateTime.Now.Day.ToString();

                //Api' ye istek yapılıyor.
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                HttpResponseMessage response = client
                    .GetAsync(
                        $"https://seffaflik.epias.com.tr/transparency/service/market/intra-day-trade-history?endDate={endDate}&startDate={startDate}")
                    .Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //Gelen string veri XML formata parse ediliyor.

                    XDocument xdoc = XDocument.Parse(response.Content.ReadAsStringAsync().Result);

                    //XML formatındaki veride bulunan listeyi  oluşturduğumuz TradeHistory modeli listesine aktarıyoruz.
                    var tradeHistories = xdoc.Elements("intraDayTradeHistoryResponse").Elements("body")
                        .Elements("intraDayTradeHistoryList")
                        .Select(p => new TradeHistory
                        {
                            Id = p.Element("id").Value,
                            Date = p.Element("date").Value,
                            Conract = p.Element("conract").Value,
                            Price = p.Element("price").Value,
                            Quantity = p.Element("quantity").Value
                        })
                        .ToList();
                    return tradeHistories;
                }

                return new List<TradeHistory>();

            }
        }




        //ChangeDateFormat fonksiyonunda concrete içerisindeki date doğru formata çevriliyor ve return ediliyor.
        public string ChangeDateFormat(string date)
        {
            var a = date.Length;
            var newDateFormat = date.Substring(6, 2) + "." + date.Substring(4, 2) + "." + date.Substring(3, 2) + " " +
                            date.Substring(8, 2) + ":00";
            return newDateFormat;
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
