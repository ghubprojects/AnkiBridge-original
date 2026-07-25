using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AnkiBridge.Infrastructure.ExternalServices.AnkiConnect.Contracts;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.AnkiConnect;

public sealed class AnkiConnectClient(HttpClient httpClient, IOptions<AnkiConnectOptions> options) : IAnkiConnectClient
{
    private readonly AnkiConnectOptions _options = options.Value;
    private readonly int _version = 6;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<T>> SendAsync<T>(
        string action,
        object? parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AnkiConnectRequest(action, parameters ?? new { }, _version);

            var json = JsonSerializer.Serialize(request, _jsonSerializerOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync(_options.BaseUrl, stringContent, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<AnkiConnectResponse<T>>(cancellationToken);

            if (response is null)
                return Result<T>.Failure("Invalid response from Anki");

            if (response.Error is not null)
                return Result<T>.Failure($"Anki error: {response.Error}");

            return response.Result!;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            return Result<T>.Failure("Cannot connect to Anki. Please make sure Anki is running and accessible.");
        }
        catch (Exception)
        {
            return Result<T>.Failure($"An error occurred while connecting to Anki.");
        }
    }
}
