using System.Text;
using System.Text.Json;
using MediLink.Interfaces;
using MediLink.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediLink.Clients;

public class FarmaciaClient : IFarmaciaClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FarmaciaClient> _logger;

    public FarmaciaClient(HttpClient httpClient, IConfiguration configuration, ILogger<FarmaciaClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["PharmaLink:BaseUrl"] ?? "https://localhost:7001";
        var apiKey = _configuration["PharmaLink:ApiKey"] ?? "MediLink-Secret-Key-2026";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<bool> NotificarRecetaAsync(Receta receta)
    {
        try
        {
            var payload = new
            {
                codigo = receta.Codigo,
                medicamentos = receta.Medicamentos,
                fechaEmision = receta.FechaEmision
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Enviando notificación de receta {Codigo} a PharmaLink...", receta.Codigo);
            var response = await _httpClient.PostAsync("/api/recetas/notificar", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Receta {Codigo} notificada con éxito a PharmaLink.", receta.Codigo);
                return true;
            }

            _logger.LogWarning("PharmaLink devolvió código {StatusCode} al notificar receta {Codigo}.", response.StatusCode, receta.Codigo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de comunicación con PharmaLink al notificar la receta {Codigo}.", receta.Codigo);
            return false;
        }
    }

    public async Task<RespuestaReposicion> SolicitarInsumosAsync(string nombreInsumo, int cantidad)
    {
        try
        {
            var payload = new
            {
                insumo = nombreInsumo,
                cantidad = cantidad
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Solicitando reposición de {Cantidad}x {Insumo} a PharmaLink...", cantidad, nombreInsumo);
            var response = await _httpClient.PostAsync("/api/reposiciones", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseStream = await response.Content.ReadAsStreamAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultado = await JsonSerializer.DeserializeAsync<RespuestaReposicion>(responseStream, options);

                _logger.LogInformation("Solicitud de reposición de {Insumo} procesada por PharmaLink.", nombreInsumo);
                return resultado ?? new RespuestaReposicion
                {
                    Confirmado = false,
                    Mensaje = "Respuesta vacía recibida desde PharmaLink."
                };
            }

            _logger.LogWarning("PharmaLink rechazó la solicitud de insumos con estado {StatusCode}.", response.StatusCode);
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = "PharmaLink rechazó la solicitud de insumos."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de conexión con PharmaLink al solicitar insumos.");
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"Error de comunicación con PharmaLink: {ex.Message}"
            };
        }
    }
}