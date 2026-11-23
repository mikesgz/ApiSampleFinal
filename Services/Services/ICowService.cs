using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services
{
    public interface ICowService
    {
        void AddCow(Cow cow);
        IList<Cow> GetAllCows();
    }
}
