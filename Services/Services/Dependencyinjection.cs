using Microsoft.Extensions.DependencyInjection;

namespace Services
{
    public static class Dependencyinjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddTransient<IMilkService, MilkService>();
            services.AddTransient<ICowService, CowService>();
            services.AddScoped<IGranjaService, GranjaService>();

            return services;
        }
    }
}
