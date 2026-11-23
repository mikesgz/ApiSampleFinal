# API de gestión de una granja lechera

Solución en .NET 10 con arquitectura por capas para registrar producción diaria de leche, transformarla en queso o yogurt, manejar lotes con vencimiento y FIFO, registrar ventas y consultar reportes.

## Proyectos

- `Domain/Domain`: entidades, reglas de lote, excepciones e interfaces de persistencia y reloj.
- `Infrastructure/Infrastructure`: Entity Framework Core, SQLite, repositorios, transacciones y reloj del sistema.
- `Services/Services`: casos de uso, validaciones, DTOs y reportes. No referencia Infrastructure.
- `Web`: controladores, Swagger, DI y respuestas de error `ProblemDetails`.
- `Test/DomainTest`: pruebas del dominio.

Las dependencias apuntan hacia el dominio. Web referencia Services e Infrastructure solo para configurar la aplicación; los controllers solo invocan servicios.

## Configuración y ejecución

Los valores iniciales editables están en `Web/appsettings.json` (`FarmDefaults`). Se copian a la base SQLite al primer inicio. Luego pueden consultarse o cambiarse con `GET` y `PUT /api/granja`.

Desde `Web`:

```powershell
dotnet restore .\ApiSampleFinal.csproj
dotnet run --project .\ApiSampleFinal.csproj
```

La aplicación aplica las migraciones al iniciar. Swagger está disponible en `/swagger` cuando se ejecuta en Development. La cadena de conexión local apunta a `Web/localDb.db`.

## API

Las fechas usan `yyyy-MM-dd`. `tipoProducto` acepta `Queso` o `Yogurt`.

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/granja` | Consultar parámetros de granja y productos. |
| PUT | `/api/granja` | Actualizar vacas, litros y parámetros de queso/yogurt. |
| POST | `/api/produccion?fecha=2026-12-01` | Registrar una vez la producción diaria de todas las vacas activas. |
| POST | `/api/transformaciones` | Transformar leche de una fecha en un tipo de producto. |
| GET | `/api/inventario?fecha=2026-12-03` | Consultar lotes vigentes con disponibilidad. |
| POST | `/api/inventario/procesar-vencimientos?fecha=2026-12-17` | Pasar a dañado el saldo disponible de lotes vencidos. |
| POST | `/api/ventas` | Registrar venta con FIFO y detalle de lotes. |
| GET | `/api/reportes/ventas?desde=&hasta=&tipo=` | Ventas del período y total general. |
| GET | `/api/reportes/leche-diaria?desde=&hasta=` | Litros producidos, transformados y no aprovechados por día. |
| GET | `/api/reportes/producto-diario?desde=&hasta=` | Producto elaborado por día y tipo. |
| GET | `/api/reportes/producto-total` | Producido, vendido, disponible y dañado por producto. |
| GET | `/api/reportes/producto-danado?desde=&hasta=` | Lotes dañados y fecha de vencimiento. |
| GET | `/api/reportes/vacas?desde=&hasta=` | Promedio por vaca, mayor, menor y vacas bajo el promedio. |

### Ejemplos de cuerpos

Actualizar configuración:

```json
{
  "numeroVacas": 10,
  "litrosPorVaca": 15,
  "productos": [
    { "tipo": "Queso", "litrosPorUnidad": 10, "vidaUtilDias": 30, "precio": 28000 },
    { "tipo": "Yogurt", "litrosPorUnidad": 1, "vidaUtilDias": 15, "precio": 5000 }
  ]
}
```

Transformar leche:

```json
{ "fecha": "2026-12-01", "tipoProducto": "Queso", "litros": 100 }
```

Registrar venta:

```json
{ "tipoProducto": "Queso", "cantidad": 12, "fecha": "2026-12-03" }
```

Las ventas usan precio configurado en el momento de la venta, descuentan lotes por fecha de producción ascendente y fallan completas con `409 Conflict` cuando no existe saldo vigente suficiente. Los errores de validación se devuelven como `ProblemDetails`.

## Escenario de referencia

Con 10 vacas que producen 15 L cada una, transformar 100 L en queso (10 L/kg) y 50 L en yogurt (1 L/L) el primer día genera 10 kg y 50 L. Transformar 150 L en queso el segundo día genera 15 kg. Una venta de 12 kg toma 10 kg del primer lote y 2 kg del segundo.

## Migraciones y pruebas

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef database update --project ..\Infrastructure\Infrastructure\Infrastructure.csproj --startup-project .\ApiSampleFinal.csproj --context Infrastructure.AppDbContext
dotnet test ..\Test\DomainTest\DomainTest.csproj
```

Las pruebas del servicio cubren producción por vaca y duplicados, límite de transformación, ventas FIFO entre lotes, rechazo de lotes vencidos, inventario intacto ante insuficiencia y traslado de saldo vencido a dañado.
