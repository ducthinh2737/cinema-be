using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Combos;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CombosController : ControllerBase
    {
        private readonly IComboService _comboService;

        public CombosController(IComboService comboService)
        {
            _comboService = comboService;
        }

        #region Customer Endpoints

        [HttpGet]
        public async Task<IActionResult> GetCombos([FromQuery] ComboQueryParameters queryParams)
        {
            var result = await _comboService.GetActiveCombosAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetComboById(int id)
        {
            var result = await _comboService.GetComboByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("recommend")]
        public async Task<IActionResult> GetRecommendedCombos([FromQuery] int showtimeId, [FromQuery] int ticketCount, CancellationToken cancellationToken)
        {
            var result = await _comboService.GetRecommendedCombosAsync(showtimeId, ticketCount, cancellationToken);
            return Ok(result);
        }

        [HttpPost("booking/{bookingId:int}")]
        public async Task<IActionResult> AddCombosToBooking(int bookingId, [FromBody] List<OrderComboInputDto> combosDto, CancellationToken cancellationToken)
        {
            var result = await _comboService.AddCombosToBookingAsync(bookingId, combosDto, cancellationToken);
            return Ok(result);
        }

        [HttpPut("booking/{bookingId:int}")]
        public async Task<IActionResult> UpdateBookingCombos(int bookingId, [FromBody] List<OrderComboInputDto> combosDto, CancellationToken cancellationToken)
        {
            var result = await _comboService.UpdateBookingCombosAsync(bookingId, combosDto, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("booking/{bookingId:int}")]
        public async Task<IActionResult> DeleteCombosFromBooking(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _comboService.DeleteCombosFromBookingAsync(bookingId, cancellationToken);
            return Ok(result);
        }

        #endregion

        #region Admin Combo Endpoints

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetPagedCombosAdmin([FromQuery] ComboQueryParameters queryParams)
        {
            var result = await _comboService.GetPagedCombosAdminAsync(queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin")]
        public async Task<IActionResult> CreateCombo([FromBody] ComboCreateUpdateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _comboService.CreateComboAsync(createDto);
            return CreatedAtAction(nameof(GetComboById), new { id = result.Data.Id }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id:int}")]
        public async Task<IActionResult> UpdateCombo(int id, [FromBody] ComboCreateUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _comboService.UpdateComboAsync(id, updateDto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id:int}")]
        public async Task<IActionResult> DeleteCombo(int id)
        {
            var result = await _comboService.DeleteComboAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/{id:int}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            var result = await _comboService.ToggleComboStatusAsync(id, isActive);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/{id:int}/display-order")]
        public async Task<IActionResult> UpdateDisplayOrder(int id, [FromBody] int displayOrder)
        {
            var result = await _comboService.UpdateComboDisplayOrderAsync(id, displayOrder);
            return Ok(result);
        }

        #endregion

        #region Admin Product Endpoints (Ingredients management)

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/products")]
        public async Task<IActionResult> GetAllProductsAdmin()
        {
            var result = await _comboService.GetAllProductsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/products")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateUpdateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _comboService.CreateProductAsync(createDto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/products/{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductCreateUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _comboService.UpdateProductAsync(id, updateDto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/products/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _comboService.DeleteProductAsync(id);
            return Ok(result);
        }

        #endregion
    }
}
