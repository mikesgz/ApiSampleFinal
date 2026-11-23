using System.Text.Json.Serialization;
using ApiSampleFinal.Automapper;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services;

namespace ApiSampleFinal;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddServices();
        builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddRepositories(builder.Configuration);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        var mappingConfiguration = new AutoMapper.MapperConfiguration(m => m.AddProfile(new MappingProfile()), NullLoggerFactory.Instance);
        builder.Services.AddSingleton(mappingConfiguration.CreateMapper());
        builder.Services.AddCors(options => options.AddPolicy("CORS_Policy", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            if (!db.ConfiguracionesGranja.Any())
            {
                var defaults = app.Configuration.GetSection("FarmDefaults");
                var n = defaults.GetValue<int>("NumeroVacas");
                var litros = defaults.GetValue<decimal>("LitrosPorVaca");
                db.ConfiguracionesGranja.Add(new ConfiguracionGranja(n, litros));
                foreach (var tipo in Enum.GetValues<TipoProducto>())
                {
                    var product = defaults.GetSection($"Productos:{tipo}");
                    db.ConfiguracionesProducto.Add(new ConfiguracionProducto(tipo, product.GetValue<decimal>("LitrosPorUnidad"), product.GetValue<int>("VidaUtilDias"), product.GetValue<decimal>("Precio")));
                }
                var cows = db.Cows.OrderBy(x => x.Id).ToList();
                for (var i = 0; i < cows.Count; i++)
                {
                    cows[i].IsActive = i < n;
                    if (cows[i].IsActive && string.IsNullOrWhiteSpace(cows[i].Name)) cows[i].Name = $"Vaca {cows[i].Id}";
                }
                for (var i = cows.Count + 1; i <= n; i++) db.Cows.Add(new Cow { Name = $"Vaca {i}", IsActive = true });
                db.SaveChanges();
            }
        }

        if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseCors("CORS_Policy");
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
