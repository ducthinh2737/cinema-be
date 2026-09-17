using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Combos;
using CinemaBooking.API.Models.Combos;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class ComboRepository : IComboRepository
    {
        private readonly CinemaDbContext _context;

        public ComboRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Combo> Combos, int TotalCount)> GetPagedCombosAsync(ComboQueryParameters queryParams)
        {
            var query = _context.Combos
                .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var search = queryParams.SearchTerm.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search) || 
                                         (c.Description != null && c.Description.ToLower().Contains(search)));
            }

            // Filter status
            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == queryParams.IsActive.Value);
            }

            // Total Count
            int totalCount = await query.CountAsync();

            // Sort
            if (!string.IsNullOrEmpty(queryParams.SortBy))
            {
                var sort = queryParams.SortBy.ToLower();
                if (sort == "name")
                {
                    query = queryParams.IsDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
                }
                else if (sort == "price")
                {
                    query = queryParams.IsDescending ? query.OrderByDescending(c => c.Price) : query.OrderBy(c => c.Price);
                }
                else if (sort == "displayorder")
                {
                    query = queryParams.IsDescending ? query.OrderByDescending(c => c.DisplayOrder) : query.OrderBy(c => c.DisplayOrder);
                }
                else
                {
                    // Default fallback
                    query = query.OrderBy(c => c.DisplayOrder);
                }
            }
            else
            {
                // Default sorting by display order
                query = query.OrderBy(c => c.DisplayOrder);
            }

            // Pagination
            var combos = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (combos, totalCount);
        }

        public async Task<Combo?> GetComboByIdAsync(int id)
        {
            return await _context.Combos
                .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public Task AddComboAsync(Combo combo)
        {
            return _context.Combos.AddAsync(combo).AsTask();
        }

        public Task UpdateComboAsync(Combo combo)
        {
            _context.Combos.Update(combo);
            return Task.CompletedTask;
        }

        public Task DeleteComboAsync(Combo combo)
        {
            combo.IsDeleted = true;
            combo.DeletedAt = DateTime.UtcNow;
            _context.Combos.Update(combo);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public Task AddProductAsync(Product product)
        {
            return _context.Products.AddAsync(product).AsTask();
        }

        public Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            return Task.CompletedTask;
        }

        public Task DeleteProductAsync(Product product)
        {
            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
