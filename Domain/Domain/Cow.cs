using System.ComponentModel.DataAnnotations;

namespace Domain
{
    public class Cow
    {
        [Key]
        public int Id { get; set; }
        public string Race { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Milk? Milk { get; set; }

        public int GetMilkQuantity()
        {
            if(Milk != null)
            {
                return Milk.Litters;
            }
            return 0;
        }

        public void  SetMilk(Milk m )
        {
            Milk = m;
        }
    }
}
