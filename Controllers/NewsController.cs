using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.News;
using CinemaBooking.API.DTOs.News;

namespace CinemaBooking.API.Controllers
{
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public NewsController(CinemaDbContext context)
        {
            _context = context;
        }

        // GET: api/news
        [HttpGet("api/news")]
        public async Task<IActionResult> GetNews([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var query = _context.NewsItems
                .Where(n => !n.IsDeleted && n.IsActive && n.Category == "news");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(lowerSearch) || (n.ShortDesc != null && n.ShortDesc.ToLower().Contains(lowerSearch)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortDesc = n.ShortDesc,
                    Content = n.Content,
                    Image = n.Image,
                    Category = n.Category,
                    CategoryLabel = n.CategoryLabel,
                    Slug = n.Slug,
                    Trending = n.Trending,
                    Tag = n.Tag,
                    EventDate = n.EventDate,
                    IsActive = n.IsActive,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            });
        }

        // GET: api/events
        [HttpGet("api/events")]
        public async Task<IActionResult> GetEvents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var query = _context.NewsItems
                .Where(n => !n.IsDeleted && n.IsActive && n.Category == "events");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(lowerSearch) || (n.ShortDesc != null && n.ShortDesc.ToLower().Contains(lowerSearch)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortDesc = n.ShortDesc,
                    Content = n.Content,
                    Image = n.Image,
                    Category = n.Category,
                    CategoryLabel = n.CategoryLabel,
                    Slug = n.Slug,
                    Trending = n.Trending,
                    Tag = n.Tag,
                    EventDate = n.EventDate,
                    IsActive = n.IsActive,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            });
        }

        // GET: api/news/admin
        [Authorize(Roles = "Admin")]
        [HttpGet("api/news/admin")]
        public async Task<IActionResult> GetAdminNews([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? category = null)
        {
            var query = _context.NewsItems.Where(n => !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(lowerSearch) || (n.ShortDesc != null && n.ShortDesc.ToLower().Contains(lowerSearch)) || n.Slug.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(n => n.Category == category);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortDesc = n.ShortDesc,
                    Content = n.Content,
                    Image = n.Image,
                    Category = n.Category,
                    CategoryLabel = n.CategoryLabel,
                    Slug = n.Slug,
                    Trending = n.Trending,
                    Tag = n.Tag,
                    EventDate = n.EventDate,
                    IsActive = n.IsActive,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            });
        }

        // GET: api/news/{id}
        [HttpGet("api/news/{id:int}")]
        public async Task<IActionResult> GetNewsById(int id)
        {
            var n = await _context.NewsItems
                .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

            if (n == null)
            {
                return NotFound(new { Message = $"News item with ID {id} not found." });
            }

            return Ok(new NewsDto
            {
                Id = n.Id,
                Title = n.Title,
                ShortDesc = n.ShortDesc,
                Content = n.Content,
                Image = n.Image,
                Category = n.Category,
                CategoryLabel = n.CategoryLabel,
                Slug = n.Slug,
                Trending = n.Trending,
                Tag = n.Tag,
                EventDate = n.EventDate,
                IsActive = n.IsActive,
                CreatedAt = n.CreatedAt
            });
        }

        // POST: api/news
        [Authorize(Roles = "Admin")]
        [HttpPost("api/news")]
        public async Task<IActionResult> CreateNews([FromBody] NewsCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Generate slug if not provided
            var slug = string.IsNullOrWhiteSpace(createDto.Slug) 
                ? GenerateSlug(createDto.Title) 
                : createDto.Slug.Trim().ToLower();

            var newsItem = new NewsItem
                {
                    Title = createDto.Title.Trim(),
                    ShortDesc = createDto.ShortDesc?.Trim(),
                    Content = createDto.Content?.Trim(),
                    Image = createDto.Image?.Trim(),
                    Category = createDto.Category,
                    CategoryLabel = createDto.Category == "events" ? "Sự kiện" : "Tin điện ảnh",
                    Slug = slug,
                    Trending = createDto.Trending,
                    Tag = createDto.Tag?.Trim(),
                    EventDate = createDto.EventDate?.Trim(),
                    IsActive = createDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

            _context.NewsItems.Add(newsItem);
            await _context.SaveChangesAsync();

            var resultDto = new NewsDto
            {
                Id = newsItem.Id,
                Title = newsItem.Title,
                ShortDesc = newsItem.ShortDesc,
                Content = newsItem.Content,
                Image = newsItem.Image,
                Category = newsItem.Category,
                CategoryLabel = newsItem.CategoryLabel,
                Slug = newsItem.Slug,
                Trending = newsItem.Trending,
                Tag = newsItem.Tag,
                EventDate = newsItem.EventDate,
                IsActive = newsItem.IsActive,
                CreatedAt = newsItem.CreatedAt
            };

            return CreatedAtAction(nameof(GetNewsById), new { id = newsItem.Id }, resultDto);
        }

        // PUT: api/news/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("api/news/{id:int}")]
        public async Task<IActionResult> UpdateNews(int id, [FromBody] NewsUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newsItem = await _context.NewsItems
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

            if (newsItem == null)
            {
                return NotFound(new { Message = $"News item with ID {id} not found." });
            }

            var slug = string.IsNullOrWhiteSpace(updateDto.Slug) 
                ? GenerateSlug(updateDto.Title) 
                : updateDto.Slug.Trim().ToLower();

            newsItem.Title = updateDto.Title.Trim();
            newsItem.ShortDesc = updateDto.ShortDesc?.Trim();
            newsItem.Content = updateDto.Content?.Trim();
            newsItem.Image = updateDto.Image?.Trim();
            newsItem.Category = updateDto.Category;
            newsItem.CategoryLabel = updateDto.Category == "events" ? "Sự kiện" : "Tin điện ảnh";
            newsItem.Slug = slug;
            newsItem.Trending = updateDto.Trending;
            newsItem.Tag = updateDto.Tag?.Trim();
            newsItem.EventDate = updateDto.EventDate?.Trim();
            newsItem.IsActive = updateDto.IsActive;
            newsItem.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var resultDto = new NewsDto
            {
                Id = newsItem.Id,
                Title = newsItem.Title,
                ShortDesc = newsItem.ShortDesc,
                Content = newsItem.Content,
                Image = newsItem.Image,
                Category = newsItem.Category,
                CategoryLabel = newsItem.CategoryLabel,
                Slug = newsItem.Slug,
                Trending = newsItem.Trending,
                Tag = newsItem.Tag,
                EventDate = newsItem.EventDate,
                IsActive = newsItem.IsActive,
                CreatedAt = newsItem.CreatedAt
            };

            return Ok(resultDto);
        }

        // DELETE: api/news/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("api/news/{id:int}")]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var newsItem = await _context.NewsItems
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

            if (newsItem == null)
            {
                return NotFound(new { Message = $"News item with ID {id} not found or already deleted." });
            }

            newsItem.IsDeleted = true;
            newsItem.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "News item has been successfully deleted." });
        }

        private string GenerateSlug(string phrase)
        {
            string str = phrase.ToLower();
            // Convert to ascii
            byte[] tempBytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(str);
            str = System.Text.Encoding.ASCII.GetString(tempBytes);
            // Replace invalid chars
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // Convert multiple spaces into one space
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", " ").Trim();
            // Cut string to max 60 chars
            str = str.Substring(0, str.Length <= 60 ? str.Length : 60).Trim();
            // Replace spaces with hyphens
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}
