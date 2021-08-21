using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace codeBH.Function
{
    public static class BankHolidays
    {
        private struct HolidayEvent
        {
            public string title;
            public string date;
            public string notes;
            public bool bunting;
        }

        [FunctionName("BankHolidays")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Got request for holidays...");

            string year = req.Query["year"];

            if (String.IsNullOrEmpty(year))
            {
                log.LogInformation("Year parameter is required");

                return new BadRequestObjectResult("Year is required.");
            }

            else
            {
                log.LogInformation("Checking if the year is valid...");
                int year_i = 0;
                if (Int32.TryParse(year, out year_i))
                {
                    if (year_i < 0)
                    {
                        log.LogInformation("Invalid year. It should be not negative.");

                        return new BadRequestObjectResult("Invalid year. It should be not negative.");
                    }
                    else
                    {
                        log.LogInformation("Year is valid, getting holidays data...");

                        try
                        {
                            HttpClient newClient = new HttpClient();
                            HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.gov.uk/bank-holidays.json");

                            HttpResponseMessage response = await newClient.SendAsync(newRequest);

                            log.LogInformation(string.Format("Got holidays data, filtering the results for year {0}...", year));

                            string jsonString = await response.Content.ReadAsStringAsync();
                            dynamic json = JsonConvert.DeserializeObject(jsonString);

                            DateTime start = new DateTime(year_i, 1, 1);
                            DateTime end = new DateTime(year_i, 12, 31);

                            List<HolidayEvent> data = json["england-and-wales"].events.ToObject<List<HolidayEvent>>();

                            string ret = data.Where(x =>
                            {
                                DateTime date = DateTime.Parse(x.date);

                                return date >= start && date <= end;
                            })
                            .OrderByDescending(x => x.date)
                            .Select(x => string.Format("{0}, {1}", x.date, x.title))
                            .Aggregate((c, n) => string.Format("{0}\n{1}", c, n));


                            return new FileContentResult(Encoding.UTF8.GetBytes(ret), "application/octet-stream")
                            {
                                FileDownloadName = string.Format("england-and-wales-{0}.csv", year)
                            };
                        }
                        catch (Exception e)
                        {
                            //TODO: add exception treatment
                            //Ex.: not reaching the UK api, asked year not present in the source, etc 
                            return new BadRequestObjectResult(e.Message);
                        }
                    }
                }
                else
                {
                    log.LogInformation("Invalid year param. Not a valid number.");

                    return new BadRequestObjectResult("Year is not a valid number.");

                }


            }
        }
    }
}
