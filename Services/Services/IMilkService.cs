using Domain;

namespace Services
{
    public interface IMilkService
    {
        void AddMilkExistences(Milk milk);
        Milk GetMilkExistences(int id);
    }
}
