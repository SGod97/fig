using Fig.Contracts.Status;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Clients;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ClientStatusFacade : IClientStatusFacade
{
    private readonly IClientRunSessionConverter _clientRunSessionConverter;
    private readonly IHttpService _httpService;

    public ClientStatusFacade(IHttpService httpService, IClientRunSessionConverter clientRunSessionConverter, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _clientRunSessionConverter = clientRunSessionConverter;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            ClientRunSessions.Clear();
            PossibleMemoryLeaks.Clear();
        });
    }

    public List<ClientRunSessionModel> ClientRunSessions { get; } = new();

    public List<MemoryUsageAnalysisModel> PossibleMemoryLeaks { get; } = new();

    public async Task Refresh()
    {
        var result = await _httpService.Get<List<ClientStatusDataContract>>("statuses");

        if (result == null)
            return;

        ClientRunSessions.Clear();
        PossibleMemoryLeaks.Clear();
        var newSessions = _clientRunSessionConverter.Convert(result);
        var memoryLeaks = _clientRunSessionConverter.ConvertToMemoryAnalysis(result);

        foreach (var session in newSessions.OrderBy(a => a.Name).ThenBy(a => a.Hostname))
            ClientRunSessions.Add(session);
        
        foreach (var leak in memoryLeaks)
            PossibleMemoryLeaks.Add(leak);

        Console.WriteLine($"Loaded {ClientRunSessions.Count} client run sessions");
    }

    public async Task RequestRestart(ClientRunSessionModel client)
    {
        await _httpService.Put(
            $"statuses/{client.RunSessionId}/restart", null);
    }
    
    public async Task SetLiveReload(ClientRunSessionModel client)
    {
        await _httpService.Put(
            $"statuses/{client.RunSessionId}/liveReload?liveReload={client.LiveReload}", null);
    }
}