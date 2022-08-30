
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using Fig.Contracts.Authentication;
using Newtonsoft.Json;

namespace Fig.Integration.SqlLookupTableService;

public class HttpService : IHttpService
{
    private readonly ISettings _settings;
    private readonly ILogger<HttpService> _logger;
    private readonly HttpClient _httpClient;
    private string? _bearerToken;

    public HttpService(Fig.Common.Factories.IHttpClientFactory httpClientFactory, ISettings settings, ILogger<HttpService> logger)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = httpClientFactory.Create(settings.FigUri!);
    }
    
    public async Task<T?> Get<T>(string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequest<T>(request);
    }

    public async Task Post(string uri, object value)
    {
        var request = CreateRequest(HttpMethod.Post, uri, value);
        await SendRequest(request);
    }

    public async Task Put(string uri, object value)
    {
        var request = CreateRequest(HttpMethod.Put, uri, value);
        await SendRequest(request);
    }
    
    public async Task LogIn()
    {
        var dataContract = new AuthenticateRequestDataContract(_settings.FigUsername!, _settings.FigPassword!);
        var request = CreateRequest(HttpMethod.Post, "/users/authenticate", dataContract);
        var result = await SendRequest<AuthenticateResponseDataContract>(request);
        if (result != null)
        {
            _bearerToken = result.Token;
            if (result.Role != Role.LookupService)
                _logger.LogWarning($"Service should be configured with a user with role {nameof(Role.LookupService)}");
        }
        else
        {
            throw new AuthenticationException("Unable to authenticate to Fig");
        }
    }
    
    private HttpRequestMessage CreateRequest(HttpMethod method, string uri, object? value = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (value != null)
            request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");

        return request;
    }
    
    private async Task SendRequest(HttpRequestMessage request)
    {
        AddJwtHeader(request);

        try
        {
            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Not logged in");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error when making request, {ex}");
        }
        
    }
    
    private async Task<T?> SendRequest<T>(HttpRequestMessage request, bool requiresAuthentication = true)
    {
        AddJwtHeader(request);

        try
        {
            using var response = await _httpClient.SendAsync(request);

            Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} got response {response.StatusCode}");

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Not logged in");
            }

            var stringContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(stringContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error when making request, {ex}");
            return default;
        }
    }

    private void AddJwtHeader(HttpRequestMessage request)
    {
        var isApiUrl = !request?.RequestUri?.IsAbsoluteUri == true;
        if (_bearerToken != null && isApiUrl)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
    }
}