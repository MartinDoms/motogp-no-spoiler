using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using RazorLight;

namespace motogp_no_spoiler
{
    public class Program
    {
        static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            var razorEngine = new RazorLightEngineBuilder()
                // required to have a default RazorLightProject type,
                // but not required to create a template from string.
                .UseEmbeddedResourcesProject(typeof(Program))
                .UseMemoryCachingProvider()
                .Build();

            var years = await GetAllYears();
            foreach (var y in years) {
                Console.WriteLine($"Generating data for {y}");

                var year = await FetchYearData(y);

                var yearTask = GenerateYearHtml(years, year, razorEngine);
                var eventsTask = GenerateGPsForYearHtml(years, year, razorEngine);

                await Task.WhenAll(yearTask, eventsTask);

                Console.WriteLine($"Finished generating data for {y}");
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        static async Task<IEnumerable<string>> GetAllYears() {
            var data = await FetchData();
            return data.AvailableSids;
        } 

        static async Task<MotoGpData> FetchData() {
            var data = await client.GetStringAsync("https://www.motogp.com/en/motogpapp/video/nospoiler/2020");
            var options = new JsonSerializerOptions {
            };
            return JsonSerializer.Deserialize<MotoGpData>(data, options);
        }

        static async Task<YearData> FetchYearData(string year) {
            var data = await client.GetStringAsync($"https://www.motogp.com/en/motogpapp/video/nospoiler/{year}");
            var options = new JsonSerializerOptions {
            };
            return JsonSerializer.Deserialize<YearData>(data, options);
        }

        static async Task GenerateYearHtml(IEnumerable<string> years, YearData year, RazorLightEngine razor) {
            string result = await razor.CompileRenderAsync("Templates.Year", new {Years = years.ToArray(), Year = year});

            Directory.CreateDirectory("output/");
            await File.WriteAllTextAsync($"output/{year.Title}.html", result);
        }
        static async Task GenerateGPsForYearHtml(IEnumerable<string> allYears, YearData year, RazorLightEngine razor) {
            foreach (var gp in year.GPs) {
                string result = await razor.CompileRenderAsync("Templates.Gp", new { Years = allYears.ToArray(), GP = gp});

                Directory.CreateDirectory($"output/{year.Title}");
                await File.WriteAllTextAsync($"output/{year.Title}/{gp.ShortName}.html", result);
            }
        }
        
    }

    public class MotoGpData {

        [JsonPropertyName("available_sids")]
        public List<string> AvailableSids { get; set; }
    }

    public class YearData {
        [JsonPropertyName("events")]
        public List<Gp> GPs { get; set; }
        [JsonPropertyName("current_sid")]
        public string Title { get; set; }
    }

    public class Gp {
        [JsonPropertyName("gp_days")]
        public List<GpDay> Days { get; set; }

        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class GpDay {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("date")]
        // TODO make this a DateTime
        public string Date { get; set; }
        [JsonPropertyName("videos")]
        public List<Video> Videos { get; set; }
    }

    public class Video {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }
}
