using Domain;

namespace Services;

public sealed record ProductoConfigDto(TipoProducto Tipo, decimal LitrosPorUnidad, int VidaUtilDias, decimal Precio);
public sealed record ConfiguracionDto(int NumeroVacas, decimal LitrosPorVaca, IReadOnlyList<ProductoConfigDto> Productos);
public sealed record ActualizarConfiguracionDto(int NumeroVacas, decimal LitrosPorVaca, IReadOnlyList<ProductoConfigDto> Productos);
public sealed record ProduccionVacaDto(int VacaId, string Vaca, decimal Litros);
public sealed record ProduccionDiaDto(DateOnly Fecha, decimal LitrosTotales, IReadOnlyList<ProduccionVacaDto> Vacas);
public sealed record TransformacionDto(DateOnly? Fecha, TipoProducto TipoProducto, decimal Litros);
public sealed record TransformacionResultDto(DateOnly Fecha, TipoProducto TipoProducto, decimal LitrosUsados, decimal UnidadesProducidas, int LoteId);
public sealed record LoteDto(int Id, TipoProducto TipoProducto, DateOnly FechaProduccion, DateOnly FechaVencimiento, decimal CantidadInicial, decimal CantidadDisponible, decimal CantidadDanada);
public sealed record VentaRequestDto(TipoProducto TipoProducto, decimal Cantidad, DateOnly? Fecha);
public sealed record VentaDetalleDto(int LoteId, DateOnly FechaProduccion, decimal Cantidad);
public sealed record VentaResponseDto(int Id, DateOnly Fecha, TipoProducto TipoProducto, decimal Cantidad, decimal PrecioUnitario, decimal Total, IReadOnlyList<VentaDetalleDto> Detalle);
public sealed record ReporteVentasDto(IReadOnlyList<VentaResponseDto> Ventas, decimal TotalGeneral);
public sealed record ReporteLecheDto(DateOnly Fecha, decimal LitrosProducidos, decimal LitrosTransformados, decimal LitrosNoAprovechados);
public sealed record ReporteProductoDiarioDto(DateOnly Fecha, TipoProducto TipoProducto, decimal CantidadProducida);
public sealed record ReporteProductoTotalDto(TipoProducto TipoProducto, decimal TotalProducido, decimal TotalVendido, decimal Disponible, decimal Danado);
public sealed record ReporteDanadoDto(int LoteId, TipoProducto TipoProducto, DateOnly FechaProduccion, DateOnly FechaVencimiento, decimal CantidadDanada);
public sealed record ReporteVacaItemDto(int VacaId, string Vaca, decimal LitrosProducidos, decimal PromedioDiario);
public sealed record ReporteVacasDto(decimal PromedioPorVaca, ReporteVacaItemDto? MayorProductora, ReporteVacaItemDto? MenorProductora, IReadOnlyList<ReporteVacaItemDto> DebajoDelPromedio, IReadOnlyList<ReporteVacaItemDto> Detalle);

public interface IGranjaService
{
    Task<ConfiguracionDto> ObtenerConfiguracionAsync(CancellationToken ct);
    Task<ConfiguracionDto> ActualizarConfiguracionAsync(ActualizarConfiguracionDto request, CancellationToken ct);
    Task<ProduccionDiaDto> RegistrarProduccionAsync(DateOnly? fecha, CancellationToken ct);
    Task<TransformacionResultDto> TransformarAsync(TransformacionDto request, CancellationToken ct);
    Task<IReadOnlyList<LoteDto>> ObtenerInventarioAsync(DateOnly? fecha, CancellationToken ct);
    Task<IReadOnlyList<ReporteDanadoDto>> ProcesarVencimientosAsync(DateOnly? fecha, CancellationToken ct);
    Task<VentaResponseDto> RegistrarVentaAsync(VentaRequestDto request, CancellationToken ct);
    Task<ReporteVentasDto> ReporteVentasAsync(DateOnly? desde, DateOnly? hasta, TipoProducto? tipo, CancellationToken ct);
    Task<IReadOnlyList<ReporteLecheDto>> ReporteLecheAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);
    Task<IReadOnlyList<ReporteProductoDiarioDto>> ReporteProductoDiarioAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);
    Task<IReadOnlyList<ReporteProductoTotalDto>> ReporteProductoTotalAsync(CancellationToken ct);
    Task<IReadOnlyList<ReporteDanadoDto>> ReporteDanadoAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);
    Task<ReporteVacasDto> ReporteVacasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);
}

public sealed class GranjaService(IGranjaRepository repo, IUnitOfWork uow, IClock clock) : IGranjaService
{
    public async Task<ConfiguracionDto> ObtenerConfiguracionAsync(CancellationToken ct)
    {
        var g = await Config(ct); var productos = await repo.ObtenerProductosAsync(ct);
        return new(g.NumeroVacas, g.LitrosPorVaca, productos.Select(MapConfig).ToList());
    }

    public async Task<ConfiguracionDto> ActualizarConfiguracionAsync(ActualizarConfiguracionDto req, CancellationToken ct)
    {
        if (req.NumeroVacas < 1 || req.LitrosPorVaca <= 0 || req.Productos is null || req.Productos.Count != 2 || req.Productos.Any(p => !Enum.IsDefined(p.Tipo) || p.LitrosPorUnidad <= 0 || p.VidaUtilDias <= 0 || p.Precio < 0) || req.Productos.Select(p => p.Tipo).Distinct().Count() != 2)
            throw new ArgumentException("Configure un número positivo de vacas y litros, y parámetros válidos para queso y yogurt.");
        var g = await Config(ct); g.Actualizar(req.NumeroVacas, req.LitrosPorVaca);
        var actuales = await repo.ObtenerProductosAsync(ct);
        foreach (var p in req.Productos)
        {
            var c = actuales.SingleOrDefault(x => x.Tipo == p.Tipo);
            if (c is null) actuales.Add(new ConfiguracionProducto(p.Tipo, p.LitrosPorUnidad, p.VidaUtilDias, p.Precio));
            else c.Actualizar(p.LitrosPorUnidad, p.VidaUtilDias, p.Precio);
        }
        await repo.GuardarConfiguracionAsync(g, actuales, ct);
        await repo.SincronizarVacasAsync(req.NumeroVacas, ct);
        await uow.GuardarAsync(ct);
        return new(g.NumeroVacas, g.LitrosPorVaca, actuales.Select(MapConfig).ToList());
    }

    public async Task<ProduccionDiaDto> RegistrarProduccionAsync(DateOnly? fecha, CancellationToken ct)
    {
        var dia = fecha ?? clock.Today; var config = await Config(ct);
        if ((await repo.ProduccionDelDiaAsync(dia, ct)).Count != 0) throw new RegistroDuplicadoException("La producción de esa fecha ya fue registrada.");
        var vacas = await repo.ObtenerVacasActivasAsync(ct);
        if (vacas.Count != config.NumeroVacas) { await repo.SincronizarVacasAsync(config.NumeroVacas, ct); vacas = await repo.ObtenerVacasActivasAsync(ct); }
        var registros = vacas.Select(v => new ProduccionVaca(v.Id, dia, config.LitrosPorVaca)).ToList();
        await repo.AgregarProduccionAsync(registros, ct); await uow.GuardarAsync(ct);
        return new(dia, registros.Sum(r => r.Litros), vacas.Select((v, i) => new ProduccionVacaDto(v.Id, v.Name, registros[i].Litros)).ToList());
    }

    public async Task<TransformacionResultDto> TransformarAsync(TransformacionDto req, CancellationToken ct)
    {
        if (req.Litros <= 0) throw new ArgumentException("Los litros a transformar deben ser mayores que cero.");
        if (!Enum.IsDefined(req.TipoProducto)) throw new ArgumentException("El tipo de producto no es válido.");
        var dia = req.Fecha ?? clock.Today;
        return await uow.EjecutarAtomicoAsync(async () =>
        {
            var produccion = await repo.ProduccionDelDiaAsync(dia, ct);
            if (produccion.Count == 0) throw new KeyNotFoundException("No hay producción registrada para esa fecha.");
            var usadas = (await repo.TransformacionesAsync(dia, dia, ct)).Sum(t => t.LitrosUsados);
            if (usadas + req.Litros > produccion.Sum(p => p.Litros)) throw new LecheInsuficienteException();
            var cfg = (await repo.ObtenerProductosAsync(ct)).SingleOrDefault(p => p.Tipo == req.TipoProducto) ?? throw new KeyNotFoundException("No existe configuración para ese producto.");
            var unidades = req.Litros / cfg.LitrosPorUnidad;
            var lote = new LoteProducto(req.TipoProducto, dia, cfg.VidaUtilDias, unidades);
            await repo.AgregarLoteAsync(lote, ct);
            await repo.AgregarTransformacionAsync(new TransformacionLeche(dia, req.TipoProducto, req.Litros, unidades), ct);
            await uow.GuardarAsync(ct);
            return new TransformacionResultDto(dia, req.TipoProducto, req.Litros, unidades, lote.Id);
        }, ct);
    }

    public async Task<IReadOnlyList<LoteDto>> ObtenerInventarioAsync(DateOnly? fecha, CancellationToken ct)
    {
        var dia = fecha ?? clock.Today;
        return (await repo.ObtenerLotesAsync(null, ct)).Where(l => !l.EstaVencido(dia) && l.CantidadDisponible > 0).OrderBy(l => l.FechaProduccion).Select(MapLote).ToList();
    }

    public async Task<IReadOnlyList<ReporteDanadoDto>> ProcesarVencimientosAsync(DateOnly? fecha, CancellationToken ct)
    {
        var dia = fecha ?? clock.Today;
        return await uow.EjecutarAtomicoAsync(async () =>
        {
            var lotes = await repo.ObtenerLotesAsync(dia, ct); var report = new List<ReporteDanadoDto>();
            foreach (var l in lotes.Where(x => x.EstaVencido(dia) && x.CantidadDisponible > 0))
            { l.MarcarComoDanado(); report.Add(MapDanado(l)); }
            return (IReadOnlyList<ReporteDanadoDto>)report;
        }, ct);
    }

    public async Task<VentaResponseDto> RegistrarVentaAsync(VentaRequestDto req, CancellationToken ct)
    {
        if (req.Cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
        if (!Enum.IsDefined(req.TipoProducto)) throw new ArgumentException("El tipo de producto no es válido.");
        var dia = req.Fecha ?? clock.Today;
        return await uow.EjecutarAtomicoAsync(async () =>
        {
            var lotes = (await repo.ObtenerLotesAsync(req.TipoProducto, ct)).Where(l => !l.EstaVencido(dia) && l.CantidadDisponible > 0).OrderBy(l => l.FechaProduccion).ThenBy(l => l.Id).ToList();
            if (lotes.Sum(l => l.CantidadDisponible) < req.Cantidad) throw new InventarioInsuficienteException(req.TipoProducto, req.Cantidad);
            var config = (await repo.ObtenerProductosAsync(ct)).Single(p => p.Tipo == req.TipoProducto);
            var venta = new Venta(dia, req.TipoProducto, req.Cantidad, config.Precio); var restante = req.Cantidad;
            foreach (var lote in lotes)
            {
                if (restante <= 0) break;
                var tomar = Math.Min(lote.CantidadDisponible, restante); lote.Descontar(tomar); venta.AgregarDetalle(lote, tomar); restante -= tomar;
            }
            await repo.AgregarVentaAsync(venta, ct); await uow.GuardarAsync(ct);
            return MapVenta(venta);
        }, ct);
    }

    public async Task<ReporteVentasDto> ReporteVentasAsync(DateOnly? desde, DateOnly? hasta, TipoProducto? tipo, CancellationToken ct)
    { ValidarRango(desde, hasta); var items = (await repo.ObtenerVentasAsync(desde, hasta, tipo, ct)).OrderBy(v => v.Fecha).ThenBy(v => v.Id).Select(MapVenta).ToList(); return new(items, items.Sum(v => v.Total)); }

    public async Task<IReadOnlyList<ReporteLecheDto>> ReporteLecheAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        ValidarRango(desde, hasta); var prods = (await repo.ObtenerProduccionAsync(desde, hasta, ct)).GroupBy(p => p.Fecha).ToDictionary(g => g.Key, g => g.Sum(p => p.Litros));
        var trans = (await repo.TransformacionesAsync(desde, hasta, ct)).GroupBy(t => t.Fecha).ToDictionary(g => g.Key, g => g.Sum(t => t.LitrosUsados));
        return prods.OrderBy(x => x.Key).Select(x => new ReporteLecheDto(x.Key, x.Value, trans.GetValueOrDefault(x.Key), x.Value - trans.GetValueOrDefault(x.Key))).ToList();
    }

    public async Task<IReadOnlyList<ReporteProductoDiarioDto>> ReporteProductoDiarioAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    { ValidarRango(desde, hasta); return (await repo.TransformacionesAsync(desde, hasta, ct)).GroupBy(t => new { t.Fecha, t.Tipo }).OrderBy(g => g.Key.Fecha).ThenBy(g => g.Key.Tipo).Select(g => new ReporteProductoDiarioDto(g.Key.Fecha, g.Key.Tipo, g.Sum(t => t.UnidadesProducidas))).ToList(); }

    public async Task<IReadOnlyList<ReporteProductoTotalDto>> ReporteProductoTotalAsync(CancellationToken ct)
    {
        var lots = await repo.ObtenerLotesAsync(null, ct); var sales = await repo.ObtenerVentasAsync(null, null, null, ct);
        return Enum.GetValues<TipoProducto>().Select(tipo => new ReporteProductoTotalDto(tipo, lots.Where(l => l.Tipo == tipo).Sum(l => l.CantidadInicial), sales.Where(s => s.Tipo == tipo).Sum(s => s.Cantidad), lots.Where(l => l.Tipo == tipo).Sum(l => l.CantidadDisponible), lots.Where(l => l.Tipo == tipo).Sum(l => l.CantidadDanada))).ToList();
    }

    public async Task<IReadOnlyList<ReporteDanadoDto>> ReporteDanadoAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    { ValidarRango(desde, hasta); return (await repo.ObtenerLotesAsync(null, ct)).Where(l => l.CantidadDanada > 0 && (!desde.HasValue || l.FechaVencimiento >= desde.Value) && (!hasta.HasValue || l.FechaVencimiento <= hasta.Value)).OrderBy(l => l.FechaVencimiento).Select(MapDanado).ToList(); }

    public async Task<ReporteVacasDto> ReporteVacasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        ValidarRango(desde, hasta); var rows = await repo.ObtenerProduccionAsync(desde, hasta, ct);
        var grouped = rows.GroupBy(x => x.VacaId).ToDictionary(g => g.Key, g => g.ToList());
        var data = (await repo.ObtenerVacasActivasAsync(ct)).Select(v => grouped.TryGetValue(v.Id, out var items)
            ? new ReporteVacaItemDto(v.Id, v.Name, items.Sum(x => x.Litros), items.Average(x => x.Litros))
            : new ReporteVacaItemDto(v.Id, v.Name, 0, 0)).OrderBy(x => x.VacaId).ToList();
        var avg = data.Count == 0 ? 0 : data.Average(x => x.LitrosProducidos);
        return new(avg, data.OrderByDescending(x => x.LitrosProducidos).FirstOrDefault(), data.OrderBy(x => x.LitrosProducidos).FirstOrDefault(), data.Where(x => x.LitrosProducidos < avg).ToList(), data);
    }

    private async Task<ConfiguracionGranja> Config(CancellationToken ct) => await repo.ObtenerConfiguracionAsync(ct) ?? throw new InvalidOperationException("Falta inicializar la configuración de la granja.");
    private static void ValidarRango(DateOnly? desde, DateOnly? hasta) { if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value) throw new ArgumentException("La fecha desde debe ser anterior o igual a hasta."); }
    private static ProductoConfigDto MapConfig(ConfiguracionProducto p) => new(p.Tipo, p.LitrosPorUnidad, p.VidaUtilDias, p.Precio);
    private static LoteDto MapLote(LoteProducto l) => new(l.Id, l.Tipo, l.FechaProduccion, l.FechaVencimiento, l.CantidadInicial, l.CantidadDisponible, l.CantidadDanada);
    private static ReporteDanadoDto MapDanado(LoteProducto l) => new(l.Id, l.Tipo, l.FechaProduccion, l.FechaVencimiento, l.CantidadDanada);
    private static VentaResponseDto MapVenta(Venta v) => new(v.Id, v.Fecha, v.Tipo, v.Cantidad, v.PrecioUnitario, v.Total, v.Detalles.Select(d => new VentaDetalleDto(d.LoteId, d.Lote.FechaProduccion, d.Cantidad)).ToList());
}

public sealed class RegistroDuplicadoException(string message) : Exception(message) { }
public sealed class LecheInsuficienteException() : Exception("La transformación supera la leche producida disponible para esa fecha.") { }
