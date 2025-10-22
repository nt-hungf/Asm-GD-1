using System.Drawing;

namespace Asm_GD_1.Models
{
    public class Combo
    {
        public int ComboId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }

        public ICollection<ComboFood> ComboFoods { get; set; } = new List<ComboFood>();

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public List<Food>? Foods { get; set; }
    }
}
