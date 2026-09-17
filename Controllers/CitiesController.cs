using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Cinemas;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CitiesController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public CitiesController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _context.Cities
                .Select(c => new CityDto
                {
                    CityId = c.CityId,
                    CityName = c.CityName
                })
                .OrderBy(c => c.CityName)
                .ToListAsync();

            return Ok(cities);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCityById(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound(new { Message = $"Không tìm thấy tỉnh/thành phố với ID {id}." });

            var dto = new CityDto
            {
                CityId = city.CityId,
                CityName = city.CityName
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCity([FromBody] CityCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CityName))
            {
                return BadRequest(new { Message = "Tên tỉnh/thành phố không được để trống." });
            }

            var exists = await _context.Cities.AnyAsync(c => c.CityName.ToLower() == dto.CityName.Trim().ToLower());
            if (exists)
            {
                return BadRequest(new { Message = "Tỉnh/thành phố này đã tồn tại trên hệ thống." });
            }

            var city = new City
            {
                CityName = dto.CityName.Trim()
            };

            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            var resultDto = new CityDto
            {
                CityId = city.CityId,
                CityName = city.CityName
            };

            return CreatedAtAction(nameof(GetCityById), new { id = city.CityId }, resultDto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CityName))
            {
                return BadRequest(new { Message = "Tên tỉnh/thành phố không được để trống." });
            }

            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound(new { Message = $"Không tìm thấy tỉnh/thành phố với ID {id}." });

            var exists = await _context.Cities.AnyAsync(c => c.CityId != id && c.CityName.ToLower() == dto.CityName.Trim().ToLower());
            if (exists)
            {
                return BadRequest(new { Message = "Tên tỉnh/thành phố này đã trùng với một bản ghi khác." });
            }

            city.CityName = dto.CityName.Trim();
            await _context.SaveChangesAsync();

            var resultDto = new CityDto
            {
                CityId = city.CityId,
                CityName = city.CityName
            };

            return Ok(resultDto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound(new { Message = $"Không tìm thấy tỉnh/thành phố với ID {id}." });

            var hasCinemas = await _context.Cinemas.AnyAsync(c => c.CityId == id);
            if (hasCinemas)
            {
                return BadRequest(new { Message = "Không thể xóa tỉnh/thành phố này vì vẫn còn rạp chiếu đang thuộc về nó." });
            }

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Xóa tỉnh/thành phố thành công." });
        }
    }

    public class CityDto
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = null!;
    }

    public class CityCreateDto
    {
        public string CityName { get; set; } = null!;
    }

    public class CityUpdateDto
    {
        public string CityName { get; set; } = null!;
    }
}
