namespace ApiSampleFinal.Models.MilkModels
{
    public class MilkDTO
    {
        public int Litters {  get; set; }
        public String Farm {  get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
    }
}
