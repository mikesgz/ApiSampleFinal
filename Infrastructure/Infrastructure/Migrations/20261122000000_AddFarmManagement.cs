using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261122000000_AddFarmManagement")]
public partial class AddFarmManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Name", table: "Cows", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<bool>(name: "IsActive", table: "Cows", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.CreateTable(name: "ConfiguracionesGranja", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false),
            NumeroVacas = table.Column<int>(type: "INTEGER", nullable: false),
            LitrosPorVaca = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ConfiguracionesGranja", x => x.Id));
        migrationBuilder.CreateTable(name: "ConfiguracionesProducto", columns: table => new
        {
            Tipo = table.Column<int>(type: "INTEGER", nullable: false),
            LitrosPorUnidad = table.Column<decimal>(type: "TEXT", nullable: false),
            VidaUtilDias = table.Column<int>(type: "INTEGER", nullable: false),
            Precio = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ConfiguracionesProducto", x => x.Tipo));
        migrationBuilder.CreateTable(name: "LotesProducto", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            Tipo = table.Column<int>(type: "INTEGER", nullable: false),
            FechaProduccion = table.Column<DateOnly>(type: "TEXT", nullable: false),
            FechaVencimiento = table.Column<DateOnly>(type: "TEXT", nullable: false),
            CantidadInicial = table.Column<decimal>(type: "TEXT", nullable: false),
            CantidadDisponible = table.Column<decimal>(type: "TEXT", nullable: false),
            CantidadDanada = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_LotesProducto", x => x.Id));
        migrationBuilder.CreateTable(name: "TransformacionesLeche", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
            Tipo = table.Column<int>(type: "INTEGER", nullable: false),
            LitrosUsados = table.Column<decimal>(type: "TEXT", nullable: false),
            UnidadesProducidas = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_TransformacionesLeche", x => x.Id));
        migrationBuilder.CreateTable(name: "ProduccionesVaca", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            VacaId = table.Column<int>(type: "INTEGER", nullable: false),
            Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
            Litros = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ProduccionesVaca", x => x.Id);
            table.ForeignKey("FK_ProduccionesVaca_Cows_VacaId", x => x.VacaId, "Cows", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable(name: "Ventas", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
            Tipo = table.Column<int>(type: "INTEGER", nullable: false),
            Cantidad = table.Column<decimal>(type: "TEXT", nullable: false),
            PrecioUnitario = table.Column<decimal>(type: "TEXT", nullable: false),
            Total = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Ventas", x => x.Id));
        migrationBuilder.CreateTable(name: "DetallesVenta", columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            VentaId = table.Column<int>(type: "INTEGER", nullable: false),
            LoteId = table.Column<int>(type: "INTEGER", nullable: false),
            Cantidad = table.Column<decimal>(type: "TEXT", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_DetallesVenta", x => x.Id);
            table.ForeignKey("FK_DetallesVenta_LotesProducto_LoteId", x => x.LoteId, "LotesProducto", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_DetallesVenta_Ventas_VentaId", x => x.VentaId, "Ventas", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_ProduccionesVaca_VacaId_Fecha", table: "ProduccionesVaca", columns: new[] { "VacaId", "Fecha" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_TransformacionesLeche_Fecha", table: "TransformacionesLeche", column: "Fecha");
        migrationBuilder.CreateIndex(name: "IX_LotesProducto_Tipo_FechaProduccion", table: "LotesProducto", columns: new[] { "Tipo", "FechaProduccion" });
        migrationBuilder.CreateIndex(name: "IX_DetallesVenta_LoteId", table: "DetallesVenta", column: "LoteId");
        migrationBuilder.CreateIndex(name: "IX_DetallesVenta_VentaId", table: "DetallesVenta", column: "VentaId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConfiguracionesGranja");
        migrationBuilder.DropTable(name: "ConfiguracionesProducto");
        migrationBuilder.DropTable(name: "DetallesVenta");
        migrationBuilder.DropTable(name: "ProduccionesVaca");
        migrationBuilder.DropTable(name: "TransformacionesLeche");
        migrationBuilder.DropTable(name: "Ventas");
        migrationBuilder.DropTable(name: "LotesProducto");
        migrationBuilder.DropColumn(name: "Name", table: "Cows");
        migrationBuilder.DropColumn(name: "IsActive", table: "Cows");
    }
}
