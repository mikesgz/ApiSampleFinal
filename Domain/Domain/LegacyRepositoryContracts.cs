namespace Domain;

public interface ICowRepository
{
    void AddCow(Cow cow);
    IList<Cow> GetAllCows();
}

public interface IMilkRepository
{
    Milk? GetMilk(int id);
    void Save(Milk milk);
}
