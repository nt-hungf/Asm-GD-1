namespace Asm_GD_1.Models
{
    public class Category
    {
        public int CategoryId { get;set; }
        public string Name { get; set; }
        public ICollection<Food> Foods { get; set; }
    }   
}                                                                                           
