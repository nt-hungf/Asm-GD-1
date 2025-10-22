namespace Asm_GD_1.Models
{
    public class Food
    {
        public int FoodId { get; set; }

        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<ComboFood> ComboFoods { get; set; } = new List<ComboFood>();
    }
}

