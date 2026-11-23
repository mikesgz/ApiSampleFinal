using Domain;

namespace Infrastructure.Repositories
{
    public class MilkRepository : BaseRepository, IMilkRepository
    {
        public MilkRepository(AppDbContext context) : base(context)
        {
        }

        public Milk? GetMilk(int id)
        {
            return context.Milks.Find(id);
        }

        public void Save(Milk milk)
        {
            try
            {
                Beguin();
                context.Milks.Add(milk);
                Save();
                Comit();
            }
            catch
            {
                RollBack();
                throw;
            }
        }
    }
}
