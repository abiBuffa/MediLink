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

        var baseUrl = _configuration["PharmaLink:BaseUrl"] ?? "http://localhost:5053";
        var apiKey = _configuration["PharmaLink:ApiKey"] ?? "PharmaLink_HospitalApiKey_Secret_2026_SecureKey";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<bool> NotificarRecetaAsync(Receta receta)
    {
        try
        {
            var payload = new
            {
                codigoReceta = receta.Codigo,
                medicamentoId = 1,
                cantidad = 1
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Enviando dispensación de receta {Codigo} a PharmaLink...", receta.Codigo);
            var response = await _httpClient.PostAsync("/api/dispensaciones", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Receta {Codigo} registrada en PharmaLink con éxito.", receta.Codigo);
                return true;
            }

            _logger.LogWarning("PharmaLink devolvió código {StatusCode} al registrar receta {Codigo}.", response.StatusCode, receta.Codigo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de comunicación con PharmaLink al registrar la receta {Codigo}.", receta.Codigo);
            return false;
        }
    }

    /// <summary>
    /// Pide insumos a PharmaLink (consultando disponibilidad en GET /api/medicamentos).
    /// - Si PharmaLink responde (acepta o rechaza) -> devuelve RespuestaReposicion.
    /// - Si PharmaLink está caída, da timeout o error 5xx -> lanza FarmaciaNoDisponibleException,
    ///   para que ReposicionService marque el pedido como Fallido y responda 503.
    /// </summary>
    public async Task<RespuestaReposicion> SolicitarInsumosAsync(string nombreInsumo, int cantidad)
    {
        HttpResponseMessage response;
        try
        {
            _logger.LogInformation("Consultando disponibilidad de {Cantidad}x {Insumo} en PharmaLink...", cantidad, nombreInsumo);
            response = await _httpClient.GetAsync("/api/medicamentos");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con PharmaLink al consultar insumos.");
            throw new FarmaciaNoDisponibleException("No se pudo conectar con PharmaLink.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "PharmaLink no respondió a tiempo (timeout) al consultar insumos.");
            throw new FarmaciaNoDisponibleException("PharmaLink no respondió a tiempo (timeout).", ex);
        }

        // Error del lado de PharmaLink (500, 502, 503...) = servicio no disponible
        if ((int)response.StatusCode >= 500)
        {
            _logger.LogError("PharmaLink devolvió error {StatusCode} al consultar insumos.", response.StatusCode);
            throw new FarmaciaNoDisponibleException($"PharmaLink devolvió error {(int)response.StatusCode}.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PharmaLink rechazó la consulta con estado {StatusCode}.", response.StatusCode);
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"PharmaLink rechazó la consulta de insumos ({(int)response.StatusCode})."
            };
        }

        try
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);

            // Buscamos si existe el medicamento en el listado devuelto por PharmaLink y si alcanza el stock
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var nombre = item.TryGetProperty("nombre", out var n) ? n.GetString() : "";
                    var stock = item.TryGetProperty("stock", out var s) && s.TryGetInt32(out var st) ? st : 0;

                    if (!string.IsNullOrEmpty(nombre) &&
                        nombre.Contains(nombreInsumo, StringComparison.OrdinalIgnoreCase))
                    {
                        if (stock >= cantidad)
                        {
                            return new RespuestaReposicion
                            {
                                Confirmado = true,
                                Mensaje = $"PharmaLink cuenta con stock suficiente de {nombre} ({stock} disponibles)."
                            };
                        }

                        return new RespuestaReposicion
                        {
                            Confirmado = false,
                            Mensaje = $"Stock insuficiente en PharmaLink para {nombre} (disponible: {stock}, solicitado: {cantidad})."
                        };
                    }
                }
            }

            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"El insumo '{nombreInsumo}' no figura en el catálogo de PharmaLink."
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
