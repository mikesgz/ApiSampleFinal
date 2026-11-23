using Domain;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ApiSampleFinal;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando la solicitud");
            var status = ex switch
            {
                InventarioInsuficienteException or RegistroDuplicadoException => StatusCodes.Status409Conflict,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                ArgumentException or LecheInsuficienteException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = status switch { 400 => "Solicitud inválida", 404 => "Recurso no encontrado", 409 => "Conflicto", _ => "Error interno" },
                Detail = status == 500 ? "Ocurrió un error interno al procesar la solicitud." : ex.Message,
                Instance = context.Request.Path
            });
        }
    }
}
