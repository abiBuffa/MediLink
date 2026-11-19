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
    /// Pide insumos a PharmaLink:
    /// 1. Consulta GET /api/insumos para obtener el id y validar stockDisponible.
    /// 2. Si hay stock, registra el pedido con POST /api/reposiciones { items: [{ insumoId, cantidad }] }.
    /// - Si PharmaLink está caída, da timeout o error 5xx -> lanza FarmaciaNoDisponibleException,
    ///   para que ReposicionService marque el pedido como Fallido y responda 503.
    /// </summary>
    public async Task<RespuestaReposicion> SolicitarInsumosAsync(string nombreInsumo, int cantidad)
    {
        // PASO 1: Consultar catálogo de insumos hospitalarios en PharmaLink (GET /api/insumos)
        HttpResponseMessage catalogoResponse;
        try
        {
            _logger.LogInformation("Consultando catálogo de insumos en PharmaLink para {Cantidad}x {Insumo}...", cantidad, nombreInsumo);
            catalogoResponse = await _httpClient.GetAsync("/api/insumos");
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

        if ((int)catalogoResponse.StatusCode >= 500)
        {
            _logger.LogError("PharmaLink devolvió error {StatusCode} al consultar insumos.", catalogoResponse.StatusCode);
            throw new FarmaciaNoDisponibleException($"PharmaLink devolvió error {(int)catalogoResponse.StatusCode}.");
        }

        if (!catalogoResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("PharmaLink rechazó la consulta de insumos con estado {StatusCode}.", catalogoResponse.StatusCode);
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"PharmaLink rechazó la consulta de insumos ({(int)catalogoResponse.StatusCode})."
            };
        }

        int insumoIdPharmaLink = 0;
        string nombreEncontrado = nombreInsumo;
        int stockDisponible = 0;
        bool encontrado = false;

        try
        {
            var jsonString = await catalogoResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var nombre = item.TryGetProperty("nombre", out var n) ? n.GetString() : "";

                    if (!string.IsNullOrEmpty(nombre) &&
                        nombre.Contains(nombreInsumo, StringComparison.OrdinalIgnoreCase))
                    {
                        encontrado = true;
                        nombreEncontrado = nombre;
                        insumoIdPharmaLink = item.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var idVal) ? idVal : 0;

                        // Leemos stockDisponible (y si no viene, usamos stock como respaldo)
                        if (item.TryGetProperty("stockDisponible", out var sdProp) && sdProp.TryGetInt32(out var sdVal))
                            stockDisponible = sdVal;
                        else if (item.TryGetProperty("stock", out var sProp) && sProp.TryGetInt32(out var sVal))
                            stockDisponible = sVal;

                        break;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "PharmaLink devolvió un catálogo de insumos con formato inválido.");
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = "Respuesta de PharmaLink con formato inválido al leer insumos."
            };
        }

        if (!encontrado)
        {
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"El insumo '{nombreInsumo}' no existe en el catálogo de PharmaLink."
            };
        }

        if (stockDisponible < cantidad)
        {
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"Stock insuficiente en PharmaLink para {nombreEncontrado} (disponible: {stockDisponible}, solicitado: {cantidad})."
            };
        }

        // PASO 2: Registrar el pedido de reposición en PharmaLink (POST /api/reposiciones)
        var payload = new
        {
            items = new[]
            {
                new
                {
                    insumoId = insumoIdPharmaLink,
                    cantidad = cantidad
                }
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage postResponse;
        try
        {
            _logger.LogInformation("Enviando pedido de reposición ({Cantidad}x {Insumo}, Id={Id}) a POST /api/reposiciones de PharmaLink...",
                cantidad, nombreEncontrado, insumoIdPharmaLink);
            postResponse = await _httpClient.PostAsync("/api/reposiciones", jsonContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con PharmaLink al registrar la reposición.");
            throw new FarmaciaNoDisponibleException("No se pudo conectar con PharmaLink.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "PharmaLink no respondió a tiempo (timeout) al registrar la reposición.");
            throw new FarmaciaNoDisponibleException("PharmaLink no respondió a tiempo (timeout).", ex);
        }

        if ((int)postResponse.StatusCode >= 500)
        {
            _logger.LogError("PharmaLink devolvió error {StatusCode} al registrar la reposición.", postResponse.StatusCode);
            throw new FarmaciaNoDisponibleException($"PharmaLink devolvió error {(int)postResponse.StatusCode}.");
        }

        if (!postResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("PharmaLink rechazó el POST /api/reposiciones con estado {StatusCode}.", postResponse.StatusCode);
            return new RespuestaReposicion
            {
                Confirmado = false,
                Mensaje = $"PharmaLink rechazó el pedido de reposición ({(int)postResponse.StatusCode})."
            };
        }

        // Intentamos leer si PharmaLink devolvió un codigoEnvio en su respuesta; si no, generamos uno de seguimiento
        string codigoEnvio = $"ENV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        try
        {
            var respBody = await postResponse.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(respBody))
            {
                using var respDoc = JsonDocument.Parse(respBody);
                if (respDoc.RootElement.TryGetProperty("codigoEnvio", out var codProp) && !string.IsNullOrEmpty(codProp.GetString()))
                {
                    codigoEnvio = codProp.GetString()!;
                }
                else if (respDoc.RootElement.TryGetProperty("id", out var repIdProp))
                {
                    codigoEnvio = $"PHL-{repIdProp}";
                }
            }
        }
        catch
        {
            // Si la respuesta no trae JSON con codigoEnvio, conservamos el generado
        }

        _logger.LogInformation("Reposición de {Cantidad}x {Insumo} confirmada por PharmaLink. Código: {CodigoEnvio}",
            cantidad, nombreEncontrado, codigoEnvio);

        return new RespuestaReposicion
        {
            Confirmado = true,
            CodigoEnvio = codigoEnvio,
            Mensaje = $"Reposición confirmada por PharmaLink ({cantidad}x {nombreEncontrado})."
        };
    }
}