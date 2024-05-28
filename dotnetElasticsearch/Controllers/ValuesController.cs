using dotnetElasticsearch.Context;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace dotnetElasticsearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ValuesController : ControllerBase
    {
        AppDbContext _context = new();
        [HttpGet("[action]")]
        public async Task<IActionResult> CreateData()
        {
            IList<Travel> travels = new List<Travel>();
            var random = new Random();
            for (var i = 0; i < 50000; i++)
            {
                var title = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 5)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                var words = new List<string>();

                for (var j = 0; j < 500; j++)
                {
                    words.Add(new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 5)
                                               .Select(s => s[random.Next(s.Length)]).ToArray()));
                }
                var description = string.Join(" ", words);
                Travel newTravel = new Travel
                {
                    Title = title,
                    Description = description
                };
                travels.Add(newTravel);
            }
            await _context.Set<Travel>().AddRangeAsync(travels);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet("[action]/{description}")]
        public async Task<IActionResult> GetDescriptionEF(string description)
        {
            IList<Travel> travels = await _context.Set<Travel>().Where(p => p.Description.Contains
            (description)).AsNoTracking().ToListAsync();
            return Ok(travels.Take(10));
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> syncToElastic()
        {
            var settings = new ConnectionConfiguration(new Uri("http://localhost:9200"))
                .RequestTimeout(TimeSpan.FromMinutes(2));
            var client = new ElasticLowLevelClient(settings);

            List<Travel> travels = await _context.Set<Travel>().AsNoTracking().ToListAsync();

            var tasks = new List<Task>();


            foreach (var travel in travels)
            {
                tasks.Add(client.IndexAsync<StringResponse>("travels", travel.Id.ToString(), PostData.Serializable(new
                {
                    travel.Id,
                    travel.Title,
                    travel.Description
                })));
            }

            await Task.WhenAll(tasks);

            return Ok();
        }

        [HttpGet("[action]/{description}")]
        public async Task<IActionResult> GetDescriptionES(string description)
        {
            var settings = new ConnectionConfiguration(new Uri("http://localhost:9200"))
                .RequestTimeout(TimeSpan.FromMinutes(2));
            var client = new ElasticLowLevelClient(settings);

            var res = await client.SearchAsync<StringResponse>("travels", PostData.Serializable(new
            {
                query = new
                {
                    query_string = new
                    {
                        query = $"*{description}*",
                        fields = new[] { "Description" }
                    }
                }
            }));

            var response = JObject.Parse(res.Body);
            var hits = response["hits"]["hits"].ToObject<List<JObject>>();
            List<Travel> travels = new List<Travel>();
            foreach (var hit in hits)
            {
                travels.Add(hit["_source"].ToObject<Travel>());
            }
            return Ok(travels.Take(10));
        }
    }
}
