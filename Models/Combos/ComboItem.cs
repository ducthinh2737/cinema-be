namespace CinemaBooking.API.Models.Combos
{
    public class ComboItem
    {
        public int ComboId { get; set; }
        public Combo Combo { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
