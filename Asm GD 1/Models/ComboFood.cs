namespace Asm_GD_1.Models
{
    public class ComboFood
    {
        public int ComboFoodId { get; set; }
        public int ComboId { get; set; }
        public Combo Combo { get; set; }

        public int FoodId { get; set; }
        public Food Food { get; set; }

        public int Quantity { get; set; }
        
    }
}
