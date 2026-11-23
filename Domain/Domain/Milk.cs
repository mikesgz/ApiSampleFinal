using System.ComponentModel.DataAnnotations;

namespace Domain
{
    public class Milk
    {
        //indicamos que id es la llave primaria
        [Key]
        public int Id { get; set; }
        public int Litters { get; set; }
        public string Farm { get; set; }
        public DateTime ExpirationDate { get; set; }
        public Milk()
        {
            Farm = string.Empty;
        }

    }
}
