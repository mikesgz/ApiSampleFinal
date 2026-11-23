using Domain;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ApiSampleFinal.Controllers;

[ApiController]
[Route("api/granja")]
public sealed class GranjaController(IGranjaService service) : ControllerBase
{
    [HttpGet]
    public Task<ConfiguracionDto> Get(CancellationToken ct) => service.ObtenerConfiguracionAsync(ct);

    [HttpPut]
    public Task<ConfiguracionDto> Put([FromBody] ActualizarConfiguracionDto request, CancellationToken ct) => service.ActualizarConfiguracionAsync(request, ct);
}

[ApiController]
[Route("api/produccion")]
public sealed class ProduccionController(IGranjaService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromQuery] DateOnly? fecha, CancellationToken ct)
    {
        var result = await service.RegistrarProduccionAsync(fecha, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

[ApiController]
[Route("api/transformaciones")]
public sealed class TransformacionesController(IGranjaService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TransformacionDto request, CancellationToken ct)
    {
        var result = await service.TransformarAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

[ApiController]
[Route("api/inventario")]
public sealed class InventarioController(IGranjaService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LoteDto>> Get([FromQuery] DateOnly? fecha, CancellationToken ct) => service.ObtenerInventarioAsync(fecha, ct);

    [HttpPost("procesar-vencimientos")]
    public Task<IReadOnlyList<ReporteDanadoDto>> ProcesarVencimientos([FromQuery] DateOnly? fecha, CancellationToken ct) => service.ProcesarVencimientosAsync(fecha, ct);
}

[ApiController]
[Route("api/ventas")]
public sealed class VentasController(IGranjaService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] VentaRequestDto request, CancellationToken ct)
    {
        var venta = await service.RegistrarVentaAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, venta);
    }
}

[ApiController]
[Route("api/reportes")]
public sealed class ReportesController(IGranjaService service) : ControllerBase
{
    [HttpGet("ventas")]
    public Task<ReporteVentasDto> Ventas([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] TipoProducto? tipo, CancellationToken ct) => service.ReporteVentasAsync(desde, hasta, tipo, ct);

    [HttpGet("leche-diaria")]
    public Task<IReadOnlyList<ReporteLecheDto>> LecheDiaria([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) => service.ReporteLecheAsync(desde, hasta, ct);

    [HttpGet("producto-diario")]
    public Task<IReadOnlyList<ReporteProductoDiarioDto>> ProductoDiario([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) => service.ReporteProductoDiarioAsync(desde, hasta, ct);

    [HttpGet("producto-total")]
    public Task<IReadOnlyList<ReporteProductoTotalDto>> ProductoTotal(CancellationToken ct) => service.ReporteProductoTotalAsync(ct);

    [HttpGet("producto-danado")]
    public Task<IReadOnlyList<ReporteDanadoDto>> ProductoDanado([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) => service.ReporteDanadoAsync(desde, hasta, ct);

    [HttpGet("vacas")]
    public Task<ReporteVacasDto> Vacas([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) => service.ReporteVacasAsync(desde, hasta, ct);
}
