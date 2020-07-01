using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using RazorLight;

namespace motogp_no_spoiler
{
    public static class Config {
        public static string BaseUrl = "/";
    }
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

            await Task.WhenAll(years.Select(y => ProcessYear(y, years, razorEngine)));
            await GenerateIndexHtml();

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        static async Task ProcessYear(string yearString, IEnumerable<string> years, RazorLightEngine razorEngine) {
            var year = await FetchYearData(yearString);
            Console.WriteLine($"Starting generation for {year.Title}");
            await GenerateYearHtml(years, year, razorEngine);
            Console.WriteLine($"Starting GP generation for {year.Title}");
            await GenerateGPsForYearHtml(years, year, razorEngine);
            Console.WriteLine($"Finished generation for {year.Title}");
        }

        static async Task<IEnumerable<string>> GetAllYears() {
            var data = await FetchData();
            return data.AvailableSids;
        } 

        static async Task<MotoGpData> FetchData() {
            Console.WriteLine("Fetching all data");
            var data = await client.GetStringAsync("https://www.motogp.com/en/motogpapp/video/nospoiler/2020");
            var options = new JsonSerializerOptions {
            };
            return JsonSerializer.Deserialize<MotoGpData>(data, options);
        }

        static async Task<YearData> FetchYearData(string year) {
            Console.WriteLine($"Fetching {year} data");
            var data = await client.GetStringAsync($"https://www.motogp.com/en/motogpapp/video/nospoiler/{year}");
            var options = new JsonSerializerOptions {
            };
            return JsonSerializer.Deserialize<YearData>(data, options);
        }

        static async Task GenerateIndexHtml() {
            var year = DateTime.Now.Year.ToString();
            var yearFile = $"output/{year}.html";
            var contents = await File.ReadAllTextAsync(yearFile);
            await File.WriteAllTextAsync("output/index.html", contents);
        }

        static async Task GenerateYearHtml(IEnumerable<string> years, YearData year, RazorLightEngine razor) {
            Console.WriteLine($"Generating page for {year.Title}");
            string result = await razor.CompileRenderAsync("Templates.Year", new {Years = years.ToArray(), Year = year});

            Directory.CreateDirectory("output/");
            var filePath = $"{year.Title}.html";
            await File.WriteAllTextAsync($"output/{filePath}", result);
        }
        static async Task GenerateGPsForYearHtml(IEnumerable<string> allYears, YearData year, RazorLightEngine razor) {
            Console.WriteLine($"Generating GP pages for {year.Title}");
            foreach (var gp in year.GPs) {
                Console.WriteLine($"Generating GP page for {gp.Title}");
                string result = await razor.CompileRenderAsync("Templates.Gp", new { Years = allYears.ToArray(), GP = gp});

                Directory.CreateDirectory($"output/{year.Title}");
                var filePath = $"{year.Title}/{gp.ShortName}.html";
                await File.WriteAllTextAsync($"output/{filePath}", result);
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
        [JsonPropertyName("urlname")]
        public string UrlName { get; set; }

        public IEnumerable<Video> AllVideos {
            get {
                return Days.SelectMany(day => day.Videos);
            }
        }

        public IEnumerable<IGrouping<string, Video>> VideosByChampionship {
            get {
                return Days.SelectMany(day => day.Videos).GroupBy(vid => vid.Championship);
            }
        }
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
        [JsonPropertyName("champ_name")]
        public string Championship { get; set; }
    }
}
