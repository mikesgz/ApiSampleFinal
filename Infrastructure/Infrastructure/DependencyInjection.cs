using Infrastructure.Repositories;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
        {

            var c = configuration.GetConnectionString("DefaultConnection");
            //aca van los repositorios
            services.AddScoped<IMilkRepository, MilkRepository>();
            services.AddScoped<ICowRepository, CowRepository>();
            services.AddScoped<IGranjaRepository, GranjaRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IClock, SystemClock>();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(c);
            });



            return services;
        }
    }
}
