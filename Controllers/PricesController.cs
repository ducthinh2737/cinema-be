using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PricesController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public PricesController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPrices()
        {
            var prices = await _context.Prices.ToListAsync();
            return Ok(prices);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPriceById(int id)
        {
            var price = await _context.Prices.FindAsync(id);
            if (price == null) return NotFound(new { Message = $"Price configuration with ID {id} not found." });
            return Ok(price);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePrice([FromBody] PriceCreateDto dto)
        {
            var price = new Price 
            { 
                Value = dto.Value, 
                TicketType = dto.TicketType 
            };
            _context.Prices.Add(price);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPriceById), new { id = price.PriceId }, price);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePrice(int id, [FromBody] PriceCreateDto dto)
        {
            var price = await _context.Prices.FindAsync(id);
            if (price == null) return NotFound(new { Message = $"Price configuration with ID {id} not found." });
            
            price.Value = dto.Value;
            price.TicketType = dto.TicketType;
            
            await _context.SaveChangesAsync();
            return Ok(price);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePrice(int id)
        {
            var price = await _context.Prices.FindAsync(id);
            if (price == null) return NotFound(new { Message = $"Price configuration with ID {id} not found." });
            
            // Check if there are showtimes associated with this price
            var hasShowtimes = await _context.Showtimes.AnyAsync(s => s.PriceId == id);
            if (hasShowtimes)
            {
                return BadRequest(new { Message = "Cannot delete price configuration because it is currently assigned to active showtimes." });
            }

            _context.Prices.Remove(price);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Price configuration deleted successfully." });
        }
    }

    public class PriceCreateDto
    {
        public decimal Value { get; set; }
        public string TicketType { get; set; } = null!;
    }
}
