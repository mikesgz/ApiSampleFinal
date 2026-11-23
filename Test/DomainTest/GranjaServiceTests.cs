using Domain;
using Services;

namespace DomainTest;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class GranjaServiceTests
{
    private readonly FakeRepository repo = new();
    private readonly GranjaService service;
    private static readonly DateOnly Dia1 = new(2026, 12, 1);

    public GranjaServiceTests() => service = new GranjaService(repo, new FakeUnitOfWork(), new FakeClock(Dia1));

    [Test]
    public async Task ProduccionEsNumeroDeVacasPorLitrosYNoSeDuplica()
    {
        var result = await service.RegistrarProduccionAsync(Dia1, default);
        Assert.That(result.LitrosTotales, Is.EqualTo(30));
        Assert.That(result.Vacas, Has.Count.EqualTo(2));
        Assert.That(Assert.ThrowsAsync<RegistroDuplicadoException>(() => service.RegistrarProduccionAsync(Dia1, default)), Is.Not.Null);
    }

    [Test]
    public async Task TransformacionNoPuedeSuperarLaLecheDelDia()
    {
        await service.RegistrarProduccionAsync(Dia1, default);
        Assert.ThrowsAsync<LecheInsuficienteException>(() => service.TransformarAsync(new TransformacionDto(Dia1, TipoProducto.Queso, 31), default));
        Assert.That(repo.Lotes, Is.Empty);
    }

    [Test]
    public async Task VentaConsumeLotesEnFIFO_YContinuaAlSiguiente()
    {
        var primero = new LoteProducto(TipoProducto.Queso, Dia1, 30, 10);
        var segundo = new LoteProducto(TipoProducto.Queso, Dia1.AddDays(1), 30, 5);
        repo.Lotes.AddRange([primero, segundo]);

        var venta = await service.RegistrarVentaAsync(new VentaRequestDto(TipoProducto.Queso, 12, Dia1.AddDays(2)), default);

        Assert.That(venta.Detalle.Select(x => x.Cantidad), Is.EqualTo(new decimal[] { 10, 2 }));
        Assert.That(venta.Detalle.Select(x => x.FechaProduccion), Is.EqualTo(new[] { Dia1, Dia1.AddDays(1) }));
        Assert.That(primero.CantidadDisponible, Is.Zero);
        Assert.That(segundo.CantidadDisponible, Is.EqualTo(3));
    }

    [Test]
    public async Task LoteVencidoNoSeVende()
    {
        var lote = new LoteProducto(TipoProducto.Yogurt, Dia1, 15, 4);
        repo.Lotes.Add(lote);
        Assert.ThrowsAsync<InventarioInsuficienteException>(() => service.RegistrarVentaAsync(new VentaRequestDto(TipoProducto.Yogurt, 1, Dia1.AddDays(16)), default));
        Assert.That(lote.CantidadDisponible, Is.EqualTo(4));
    }

    [Test]
    public async Task VentaInsuficienteNoModificaNingunLote()
    {
        var primero = new LoteProducto(TipoProducto.Queso, Dia1, 30, 5);
        var segundo = new LoteProducto(TipoProducto.Queso, Dia1.AddDays(1), 30, 3);
        repo.Lotes.AddRange([primero, segundo]);
        Assert.ThrowsAsync<InventarioInsuficienteException>(() => service.RegistrarVentaAsync(new VentaRequestDto(TipoProducto.Queso, 9, Dia1.AddDays(2)), default));
        Assert.That(primero.CantidadDisponible, Is.EqualTo(5));
        Assert.That(segundo.CantidadDisponible, Is.EqualTo(3));
        Assert.That(repo.Ventas, Is.Empty);
    }

    [Test]
    public async Task ProcesarVencimientosPasaSaldoDisponibleADanado()
    {
        var lote = new LoteProducto(TipoProducto.Yogurt, Dia1, 15, 12);
        lote.Descontar(2);
        repo.Lotes.Add(lote);
        var result = await service.ProcesarVencimientosAsync(Dia1.AddDays(16), default);
        Assert.That(lote.CantidadDisponible, Is.Zero);
        Assert.That(lote.CantidadDanada, Is.EqualTo(10));
        Assert.That(result.Single().CantidadDanada, Is.EqualTo(10));
        Assert.That((await service.ReporteProductoTotalAsync(default)).Single(x => x.TipoProducto == TipoProducto.Yogurt).Danado, Is.EqualTo(10));
    }

    private sealed class FakeClock(DateOnly today) : IClock { public DateOnly Today { get; } = today; }
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<T> EjecutarAtomicoAsync<T>(Func<Task<T>> operacion, CancellationToken ct) => operacion();
        public Task GuardarAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeRepository : IGranjaRepository
    {
        public ConfiguracionGranja Config = new(2, 15);
        public List<ConfiguracionProducto> Productos = [new(TipoProducto.Queso, 10, 30, 28000), new(TipoProducto.Yogurt, 1, 15, 5000)];
        public List<Cow> Vacas = [new() { Id = 1, Name = "Vaca 1", IsActive = true }, new() { Id = 2, Name = "Vaca 2", IsActive = true }];
        public List<ProduccionVaca> Producciones = [];
        public List<TransformacionLeche> Transformaciones = [];
        public List<LoteProducto> Lotes = [];
        public List<Venta> Ventas = [];
        public Task<ConfiguracionGranja?> ObtenerConfiguracionAsync(CancellationToken ct) => Task.FromResult<ConfiguracionGranja?>(Config);
        public Task<List<ConfiguracionProducto>> ObtenerProductosAsync(CancellationToken ct) => Task.FromResult(Productos);
        public Task GuardarConfiguracionAsync(ConfiguracionGranja granja, IEnumerable<ConfiguracionProducto> productos, CancellationToken ct) { Config = granja; Productos = productos.ToList(); return Task.CompletedTask; }
        public Task<List<Cow>> ObtenerVacasActivasAsync(CancellationToken ct) => Task.FromResult(Vacas.Where(x => x.IsActive).ToList());
        public Task SincronizarVacasAsync(int cantidad, CancellationToken ct)
        {
            foreach (var cow in Vacas) cow.IsActive = false;
            foreach (var cow in Vacas.Take(cantidad)) cow.IsActive = true;
            while (Vacas.Count(x => x.IsActive) < cantidad) Vacas.Add(new Cow { Id = Vacas.Count + 1, Name = $"Vaca {Vacas.Count + 1}", IsActive = true });
            return Task.CompletedTask;
        }
        public Task<List<ProduccionVaca>> ProduccionDelDiaAsync(DateOnly fecha, CancellationToken ct) => Task.FromResult(Producciones.Where(x => x.Fecha == fecha).ToList());
        public Task AgregarProduccionAsync(IEnumerable<ProduccionVaca> produccion, CancellationToken ct) { Producciones.AddRange(produccion); return Task.CompletedTask; }
        public Task<List<ProduccionVaca>> ObtenerProduccionAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct) => Task.FromResult(Producciones.Where(x => (!desde.HasValue || x.Fecha >= desde.Value) && (!hasta.HasValue || x.Fecha <= hasta.Value)).ToList());
        public Task<List<TransformacionLeche>> TransformacionesAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct) => Task.FromResult(Transformaciones.Where(x => (!desde.HasValue || x.Fecha >= desde.Value) && (!hasta.HasValue || x.Fecha <= hasta.Value)).ToList());
        public Task AgregarTransformacionAsync(TransformacionLeche transformacion, CancellationToken ct) { Transformaciones.Add(transformacion); return Task.CompletedTask; }
        public Task<List<LoteProducto>> ObtenerLotesAsync(TipoProducto? tipo, CancellationToken ct) => Task.FromResult(Lotes.Where(x => !tipo.HasValue || x.Tipo == tipo).ToList());
        public Task<List<LoteProducto>> ObtenerLotesAsync(DateOnly fecha, CancellationToken ct) => Task.FromResult(Lotes.Where(x => x.FechaVencimiento < fecha && x.CantidadDisponible > 0).ToList());
        public Task AgregarLoteAsync(LoteProducto lote, CancellationToken ct) { Lotes.Add(lote); return Task.CompletedTask; }
        public Task<List<Venta>> ObtenerVentasAsync(DateOnly? desde, DateOnly? hasta, TipoProducto? tipo, CancellationToken ct) => Task.FromResult(Ventas.Where(x => (!desde.HasValue || x.Fecha >= desde.Value) && (!hasta.HasValue || x.Fecha <= hasta.Value) && (!tipo.HasValue || x.Tipo == tipo.Value)).ToList());
        public Task AgregarVentaAsync(Venta venta, CancellationToken ct) { Ventas.Add(venta); return Task.CompletedTask; }
    }
}
