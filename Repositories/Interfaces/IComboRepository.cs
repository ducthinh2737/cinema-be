using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Combos;
using CinemaBooking.API.Models.Combos;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IComboRepository
    {
        // Combo Operations
        Task<(IEnumerable<Combo> Combos, int TotalCount)> GetPagedCombosAsync(ComboQueryParameters queryParams);
        Task<Combo?> GetComboByIdAsync(int id);
        Task AddComboAsync(Combo combo);
        Task UpdateComboAsync(Combo combo);
        Task DeleteComboAsync(Combo combo);

        // Product Operations
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(Product product);

        // Save Changes
        Task<bool> SaveChangesAsync();
    }
}
