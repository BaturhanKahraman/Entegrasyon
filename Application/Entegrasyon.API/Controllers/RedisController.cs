using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedisController : ControllerBase
    {
        private readonly IDistributedCache _distributedCache;
        public RedisController(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }
        // GET: api/<RedisController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var cachedValue = await _distributedCache.GetStringAsync("deneme");
            if (string.IsNullOrEmpty(cachedValue))
            {
                cachedValue = "keşlenmiş değer";
                await _distributedCache.SetStringAsync("deneme", "cached Value");
                return Created("Cached : " +cachedValue,null);
            }
            return Ok(cachedValue);
        }

        // GET api/<RedisController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<RedisController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<RedisController>/5
        [HttpPut("{id}")]
        public void Put(int id,[FromBody] string value)
        {
        }

        // DELETE api/<RedisController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
