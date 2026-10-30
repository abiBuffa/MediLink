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

        var baseUrl = _configuration["PharmaLink:BaseUrl"] ?? "http://localhost:5050";
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

    /// <summary>
    /// Pide insumos a PharmaLink.
    /// - Si PharmaLink responde (acepta o rechaza) -> devuelve RespuestaReposicion.
    /// - Si PharmaLink está caída, da timeout o error 5xx -> lanza FarmaciaNoDisponibleException,
    ///   para que ReposicionService marque el pedido como Fallido y responda 503.
    /// </summary>
    public async Task<RespuestaReposicion> SolicitarInsumosAsync(string nombreInsumo, int cantidad)
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

        HttpResponseMessage response;
        try
        {
            _logger.LogInformation("Solicitando reposición de {Cantidad}x {Insumo} a PharmaLink...", cantidad, nombreInsumo);
            response = await _httpClient.PostAsync("/api/reposiciones", jsonContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con PharmaLink al solicitar insumos.");
            throw new FarmaciaNoDisponibleException("No se pudo conectar con PharmaLink.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "PharmaLink no respondió a tiempo (timeout) al solicitar insumos.");
            throw new FarmaciaNoDisponibleException("PharmaLink no respondió a tiempo (timeout).", ex);
        }

        // Error del lado de PharmaLink (500, 502, 503...) = servicio no disponible
        if ((int)response.StatusCode >= 500)
        {
            _logger.LogError("PharmaLink devolvió error {StatusCode} al solicitar insumos.", response.StatusCode);
            throw new FarmaciaNoDisponibleException($"PharmaLink devolvió error {(int)response.StatusCode}.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PharmaLink rechazó la solicitud de insumos con estado {StatusCode}.", response.StatusCode);
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"PharmaLink rechazó la solicitud de insumos ({(int)response.StatusCode})."
            };
        }

        try
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
        catch (JsonException ex)
        {
            _logger.LogError(ex, "PharmaLink devolvió una respuesta con formato inválido.");
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = "Respuesta de PharmaLink con formato inválido."
            };
        }
    }
}
