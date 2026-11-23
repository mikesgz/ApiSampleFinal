using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public sealed class GranjaRepository(AppDbContext context) : IGranjaRepository
{
    public Task<ConfiguracionGranja?> ObtenerConfiguracionAsync(CancellationToken ct) => context.ConfiguracionesGranja.SingleOrDefaultAsync(x => x.Id == 1, ct);
    public Task<List<ConfiguracionProducto>> ObtenerProductosAsync(CancellationToken ct) => context.ConfiguracionesProducto.OrderBy(x => x.Tipo).ToListAsync(ct);
    public async Task GuardarConfiguracionAsync(ConfiguracionGranja granja, IEnumerable<ConfiguracionProducto> productos, CancellationToken ct)
    {
        context.ConfiguracionesGranja.Update(granja);
        foreach (var producto in productos)
            if (context.Entry(producto).State == EntityState.Detached) context.ConfiguracionesProducto.Add(producto);
        await Task.CompletedTask;
    }
    public Task<List<Cow>> ObtenerVacasActivasAsync(CancellationToken ct) => context.Cows.Where(x => x.IsActive).OrderBy(x => x.Id).ToListAsync(ct);
    public async Task SincronizarVacasAsync(int cantidad, CancellationToken ct)
    {
        var vacas = await context.Cows.OrderBy(x => x.Id).ToListAsync(ct);
        foreach (var (vaca, index) in vacas.Select((v, i) => (v, i)))
        {
            vaca.IsActive = index < cantidad;
            if (vaca.IsActive && string.IsNullOrWhiteSpace(vaca.Name)) vaca.Name = $"Vaca {vaca.Id}";
        }
        var faltantes = cantidad - vacas.Count;
        var siguiente = vacas.Count == 0 ? 1 : vacas.Max(x => x.Id) + 1;
        for (var i = 0; i < faltantes; i++) context.Cows.Add(new Cow { Name = $"Vaca {siguiente + i}", Race = "" });
    }
    public Task<List<ProduccionVaca>> ProduccionDelDiaAsync(DateOnly fecha, CancellationToken ct) => context.ProduccionesVaca.Include(x => x.Vaca).Where(x => x.Fecha == fecha).OrderBy(x => x.VacaId).ToListAsync(ct);
    public async Task AgregarProduccionAsync(IEnumerable<ProduccionVaca> produccion, CancellationToken ct) { await context.ProduccionesVaca.AddRangeAsync(produccion, ct); }
    public IQueryable<ProduccionVaca> ProduccionQuery(DateOnly? desde, DateOnly? hasta) => context.ProduccionesVaca.Include(x => x.Vaca).Where(x => (!desde.HasValue || x.Fecha >= desde.Value) && (!hasta.HasValue || x.Fecha <= hasta.Value));
    public Task<List<ProduccionVaca>> ObtenerProduccionAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct) => ProduccionQuery(desde, hasta).ToListAsync(ct);
    public Task<List<TransformacionLeche>> TransformacionesAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct) => context.TransformacionesLeche.Where(x => (!desde.HasValue || x.Fecha >= desde.Value) && (!hasta.HasValue || x.Fecha <= hasta.Value)).ToListAsync(ct);
    public async Task AgregarTransformacionAsync(TransformacionLeche transformacion, CancellationToken ct) => await context.TransformacionesLeche.AddAsync(transformacion, ct);
    public Task<List<LoteProducto>> ObtenerLotesAsync(TipoProducto? tipo, CancellationToken ct) => context.LotesProducto.Where(x => !tipo.HasValue || x.Tipo == tipo).ToListAsync(ct);
    public Task<List<LoteProducto>> ObtenerLotesAsync(DateOnly fecha, CancellationToken ct) => context.LotesProducto.Where(x => x.FechaVencimiento < fecha && x.CantidadDisponible > 0).ToListAsync(ct);
    public async Task AgregarLoteAsync(LoteProducto lote, CancellationToken ct) => await context.LotesProducto.AddAsync(lote, ct);
    public Task<List<Venta>> ObtenerVentasAsync(DateOnly? desde, DateOnly? hasta, TipoProducto? tipo, CancellationToken ct) => context.Ventas.Include(x => x.Detalles).ThenInclude(x => x.Lote).Where(x => (!desde.HasValue || x.Fecha >= desde.Value) && (!hasta.HasValue || x.Fecha <= hasta.Value) && (!tipo.HasValue || x.Tipo == tipo.Value)).ToListAsync(ct);
    public async Task AgregarVentaAsync(Venta venta, CancellationToken ct) => await context.Ventas.AddAsync(venta, ct);
}

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task<T> EjecutarAtomicoAsync<T>(Func<Task<T>> operacion, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operacion();
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    public Task GuardarAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}

public sealed class SystemClock : IClock { public DateOnly Today => DateOnly.FromDateTime(DateTime.Today); }
