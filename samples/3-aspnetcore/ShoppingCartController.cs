using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreTest.Controllers
{
    public class ShoppingCart
    {
        public string Id { get; set; }

        public ShoppingCartLine[] Lines { get; set; }
    }

    public class ShoppingCartLine
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public int Quatity { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, ShoppingCart> _shoppingCarts = new ConcurrentDictionary<string, ShoppingCart>();

        [HttpGet("{id}")]
        public ActionResult<ShoppingCart> Get(string id)
        {
            if (_shoppingCarts.TryGetValue(id, out var result))
            {
                return result;
            }

            return NotFound();
        }

        [HttpPost]
        public ActionResult<ShoppingCart> Post([FromBody] ShoppingCart value)
        {
            value.Id = Guid.NewGuid().ToString();
            return _shoppingCarts.GetOrAdd(value.Id, value);
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            if (_shoppingCarts.TryRemove(id, out _))
            {
                return Ok();
            }

            return NotFound();
        }
    }
}
