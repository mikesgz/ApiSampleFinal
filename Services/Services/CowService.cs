using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services
{
    public class CowService : ICowService
    {
        public ICowRepository Repository { get; set; }

        public CowService(ICowRepository repository)
        {
            Repository = repository;
        }
        public void AddCow(Cow cow)
        {
            Repository. AddCow(cow);
        }

        public IList<Cow> GetAllCows()
        {
           return Repository.GetAllCows();
        }
    }
}
