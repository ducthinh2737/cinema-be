using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Promotions;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionsController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPromotions([FromQuery] PromotionQueryParameters queryParams)
        {
            var result = await _promotionService.GetPagedPromotionsAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPromotionById(int id)
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id);
            if (promotion == null)
            {
                return NotFound(new { Message = $"Promotion with ID {id} not found." });
            }
            return Ok(promotion);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var promotion = await _promotionService.CreatePromotionAsync(createDto);
            return CreatedAtAction(nameof(GetPromotionById), new { id = promotion.PromotionId }, promotion);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] PromotionUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedPromo = await _promotionService.UpdatePromotionAsync(id, updateDto);
            if (updatedPromo == null)
            {
                return NotFound(new { Message = $"Promotion with ID {id} not found." });
            }
            return Ok(updatedPromo);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var success = await _promotionService.DeletePromotionAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Promotion with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Promotion has been successfully soft deleted." });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidatePromotion([FromBody] PromotionValidateDto validateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _promotionService.ValidatePromotionAsync(validateDto);
            return Ok(result);
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyPromotion([FromBody] PromotionApplyDto applyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _promotionService.ApplyPromotionAsync(applyDto);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
