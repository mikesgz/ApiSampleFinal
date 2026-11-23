using Domain;

namespace Services
{
    public class MilkService : IMilkService
    {
        IMilkRepository MilkRepository { get; set; }
        public MilkService(IMilkRepository MilkRepository)
        {
            this.MilkRepository = MilkRepository;
        }
        public void AddMilkExistences(Milk milk)
        {
            MilkRepository.Save(milk);
        }

        public Milk GetMilkExistences(int id)
        {
            return MilkRepository.GetMilk(id) ?? new Milk();
        }
    }
}
