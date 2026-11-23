namespace Domain;

public enum TipoProducto { Queso, Yogurt }

public sealed class ConfiguracionProducto
{
    private ConfiguracionProducto() { }
    public ConfiguracionProducto(TipoProducto tipo, decimal litrosPorUnidad, int vidaUtilDias, decimal precio)
    { Tipo = tipo; LitrosPorUnidad = litrosPorUnidad; VidaUtilDias = vidaUtilDias; Precio = precio; }
    public TipoProducto Tipo { get; private set; }
    public decimal LitrosPorUnidad { get; private set; }
    public int VidaUtilDias { get; private set; }
    public decimal Precio { get; private set; }
    public void Actualizar(decimal litrosPorUnidad, int vidaUtilDias, decimal precio)
    { LitrosPorUnidad = litrosPorUnidad; VidaUtilDias = vidaUtilDias; Precio = precio; }
}

public sealed class ConfiguracionGranja
{
    private ConfiguracionGranja() { }
    public ConfiguracionGranja(int numeroVacas, decimal litrosPorVaca)
    { NumeroVacas = numeroVacas; LitrosPorVaca = litrosPorVaca; }
    public int Id { get; private set; } = 1;
    public int NumeroVacas { get; private set; }
    public decimal LitrosPorVaca { get; private set; }
    public void Actualizar(int numeroVacas, decimal litrosPorVaca)
    { NumeroVacas = numeroVacas; LitrosPorVaca = litrosPorVaca; }
}

public sealed class ProduccionVaca
{
    private ProduccionVaca() { }
    public ProduccionVaca(int vacaId, DateOnly fecha, decimal litros) { VacaId = vacaId; Fecha = fecha; Litros = litros; }
    public int Id { get; private set; }
    public int VacaId { get; private set; }
    public Cow Vaca { get; private set; } = null!;
    public DateOnly Fecha { get; private set; }
    public decimal Litros { get; private set; }
}

public sealed class TransformacionLeche
{
    private TransformacionLeche() { }
    public TransformacionLeche(DateOnly fecha, TipoProducto tipo, decimal litros, decimal unidades)
    { Fecha = fecha; Tipo = tipo; LitrosUsados = litros; UnidadesProducidas = unidades; }
    public int Id { get; private set; }
    public DateOnly Fecha { get; private set; }
    public TipoProducto Tipo { get; private set; }
    public decimal LitrosUsados { get; private set; }
    public decimal UnidadesProducidas { get; private set; }
}

public sealed class LoteProducto
{
    private LoteProducto() { }
    public LoteProducto(TipoProducto tipo, DateOnly fecha, int vidaUtilDias, decimal cantidad)
    { Tipo = tipo; FechaProduccion = fecha; FechaVencimiento = fecha.AddDays(vidaUtilDias); CantidadInicial = cantidad; CantidadDisponible = cantidad; }
    public int Id { get; private set; }
    public TipoProducto Tipo { get; private set; }
    public DateOnly FechaProduccion { get; private set; }
    public DateOnly FechaVencimiento { get; private set; }
    public decimal CantidadInicial { get; private set; }
    public decimal CantidadDisponible { get; private set; }
    public decimal CantidadDanada { get; private set; }
    public bool EstaVencido(DateOnly fecha) => fecha > FechaVencimiento;
    public void Descontar(decimal cantidad)
    {
        if (cantidad <= 0 || cantidad > CantidadDisponible) throw new InvalidOperationException("Cantidad inválida para el lote.");
        CantidadDisponible -= cantidad;
    }
    public decimal MarcarComoDanado()
    { var perdida = CantidadDisponible; CantidadDanada += perdida; CantidadDisponible = 0; return perdida; }
}

public sealed class Venta
{
    private Venta() { }
    public Venta(DateOnly fecha, TipoProducto tipo, decimal cantidad, decimal precioUnitario)
    { Fecha = fecha; Tipo = tipo; Cantidad = cantidad; PrecioUnitario = precioUnitario; Total = cantidad * precioUnitario; }
    public int Id { get; private set; }
    public DateOnly Fecha { get; private set; }
    public TipoProducto Tipo { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Total { get; private set; }
    public List<DetalleVenta> Detalles { get; private set; } = [];
    public void AgregarDetalle(LoteProducto lote, decimal cantidad) => Detalles.Add(new DetalleVenta(lote, cantidad));
}

public sealed class DetalleVenta
{
    private DetalleVenta() { }
    public DetalleVenta(LoteProducto lote, decimal cantidad) { Lote = lote; LoteId = lote.Id; Cantidad = cantidad; }
    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int LoteId { get; private set; }
    public LoteProducto Lote { get; private set; } = null!;
    public decimal Cantidad { get; private set; }
}

public sealed class InventarioInsuficienteException : Exception
{
    public InventarioInsuficienteException(TipoProducto tipo, decimal cantidad)
        : base($"No hay suficiente inventario vigente de {tipo} para vender {cantidad} unidades.") { }
}

public interface IClock { DateOnly Today { get; } }
public interface IGranjaRepository
{
    Task<ConfiguracionGranja?> ObtenerConfiguracionAsync(CancellationToken ct);
    Task<List<ConfiguracionProducto>> ObtenerProductosAsync(CancellationToken ct);
    Task GuardarConfiguracionAsync(ConfiguracionGranja granja, IEnumerable<ConfiguracionProducto> productos, CancellationToken ct);
    Task<List<Cow>> ObtenerVacasActivasAsync(CancellationToken ct);
    Task SincronizarVacasAsync(int cantidad, CancellationToken ct);
    Task<List<ProduccionVaca>> ProduccionDelDiaAsync(DateOnly fecha, CancellationToken ct);
    Task AgregarProduccionAsync(IEnumerable<ProduccionVaca> produccion, CancellationToken ct);
    Task<List<TransformacionLeche>> TransformacionesAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);
    Task<List<ProduccionVaca>> ObtenerProduccionAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);
    Task AgregarTransformacionAsync(TransformacionLeche transformacion, CancellationToken ct);
    Task<List<LoteProducto>> ObtenerLotesAsync(TipoProducto? tipo, CancellationToken ct);
    Task AgregarLoteAsync(LoteProducto lote, CancellationToken ct);
    Task<List<Venta>> ObtenerVentasAsync(DateOnly? desde, DateOnly? hasta, TipoProducto? tipo, CancellationToken ct);
    Task<List<LoteProducto>> ObtenerLotesAsync(DateOnly fecha, CancellationToken ct);
    Task AgregarVentaAsync(Venta venta, CancellationToken ct);
}

public interface IUnitOfWork { Task<T> EjecutarAtomicoAsync<T>(Func<Task<T>> operacion, CancellationToken ct); Task GuardarAsync(CancellationToken ct); }
