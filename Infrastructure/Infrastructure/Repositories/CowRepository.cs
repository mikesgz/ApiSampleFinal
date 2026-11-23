using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class CowRepository :BaseRepository, ICowRepository
    {
        public CowRepository(AppDbContext context) : base(context)
        {
        }

        public void AddCow(Cow cow)
        {
            try
            {
                Beguin();
                context.Cows.Add(cow);
                context.SaveChanges();
                Comit();
            }
            catch (Exception ex)
            {
                RollBack();
            }
        }

        public IList<Cow> GetAllCows()
        {
            return context.Cows.Include( X=>X.Milk).ToList();
        }
    }
}
