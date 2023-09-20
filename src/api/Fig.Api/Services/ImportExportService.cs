using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.SettingVerification;
using Fig.Api.Utils;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Services;

public class ImportExportService : AuthenticatedService, IImportExportService
{
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IClientExportConverter _clientExportConverter;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly ISettingVerifier _settingVerifier;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IDeferredClientConverter _deferredClientConverter;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly ISettingApplier _settingApplier;
    private readonly ISettingChangeRecorder _settingChangeRecorder;
    private readonly ILogger<ImportExportService> _logger;

    public ImportExportService(ISettingClientRepository settingClientRepository,
        IClientExportConverter clientExportConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ISettingVerifier settingVerifier,
        ISettingHistoryRepository settingHistoryRepository,
        IDeferredClientConverter deferredClientConverter,
        IDeferredClientImportRepository deferredClientImportRepository,
        ISettingApplier settingApplier,
        ISettingChangeRecorder settingChangeRecorder,
        ILogger<ImportExportService> logger)
    {
        _settingClientRepository = settingClientRepository;
        _clientExportConverter = clientExportConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _settingVerifier = settingVerifier;
        _settingHistoryRepository = settingHistoryRepository;
        _deferredClientConverter = deferredClientConverter;
        _deferredClientImportRepository = deferredClientImportRepository;
        _settingApplier = settingApplier;
        _settingChangeRecorder = settingChangeRecorder;
        _logger = logger;
    }
    
    public async Task<ImportResultDataContract> Import(FigDataExportDataContract? data, ImportMode importMode)
    {
        foreach (var client in data?.Clients.Select(a => a.Name) ?? new List<string>())
            ThrowIfNoAccess(client);
        
        try
        {
            return await PerformImport(data, importMode);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Import failed");
            _eventLogRepository.Add(_eventLogFactory.DataImportFailed(data?.ImportType ?? ImportType.AddNew, importMode, AuthenticatedUser, e.Message));
            return new ImportResultDataContract
            {
                ErrorMessage = e.Message
            };
        }
    }

    public FigDataExportDataContract Export(bool decryptSecrets)
    {
        var clients = _settingClientRepository.GetAllClients(AuthenticatedUser);

        _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser, decryptSecrets));
        
        // TODO How to manage versions.
        return new FigDataExportDataContract(DateTime.UtcNow,
            ImportType.AddNew,
            1,
            clients.Select(a => _clientExportConverter.Convert(a,
                    decryptSecrets))
                .ToList());
    }

    public FigValueOnlyDataExportDataContract ValueOnlyExport()
    {
        var clients = _settingClientRepository.GetAllClients(AuthenticatedUser);

        _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser, false));
        
        return new FigValueOnlyDataExportDataContract(DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            clients.Select(a => _clientExportConverter.ConvertValueOnly(a))
                .ToList());
    }

    public ImportResultDataContract ValueOnlyImport(FigValueOnlyDataExportDataContract? data, ImportMode importMode)
    {
        foreach (var client in data?.Clients.Select(a => a.Name) ?? new List<string>())
            ThrowIfNoAccess(client);
        
        if (data?.ImportType != ImportType.UpdateValues)
            throw new NotSupportedException(
                $"Value only imports only support {nameof(ImportType.UpdateValues)} import type");
        
        if (!data.Clients.Any())
            return new ImportResultDataContract { ImportType = data.ImportType, ErrorMessage = "No clients to import"};

        _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));
        
        var importedClients = new List<string>();
        var deferredClients = new List<string>();

        foreach (var clientToUpdate in data.Clients)
        {
            var client = _settingClientRepository.GetClient(clientToUpdate.Name, clientToUpdate.Instance);

            if (client != null)
            {
                UpdateClient(client, clientToUpdate);
                importedClients.Add(client.Name);
            }
            else
            {
                AddDeferredImport(clientToUpdate);
                deferredClients.Add(clientToUpdate.Name);
            }
        }
        
        if (importedClients.Any())
            _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, importedClients.Count, AuthenticatedUser));
        
        if (deferredClients.Any())
            _eventLogRepository.Add(_eventLogFactory.DeferredImportRegistered(data.ImportType, importMode, deferredClients.Count, AuthenticatedUser));
        
        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = importedClients,
            DeferredImportClients = deferredClients
        };
    }

    public List<DeferredImportClientDataContract> GetDeferredImportClients()
    {
        var clients = _deferredClientImportRepository.GetAllClients(AuthenticatedUser);
        return clients.Select(a => new DeferredImportClientDataContract(a.Name, a.Instance, a.SettingCount, a.AuthenticatedUser)).ToList();
    }

    private async Task<ImportResultDataContract> PerformImport(FigDataExportDataContract? data, ImportMode importMode)
    {
        if (data?.Clients.Any() != true)
            return new ImportResultDataContract() { ImportType = data?.ImportType ?? ImportType.AddNew, ErrorMessage = "No Clients to Import" };

        _eventLogRepository.Add(_eventLogFactory.DataImportStarted(data.ImportType, importMode, AuthenticatedUser));

        ImportResultDataContract result;
        switch (data.ImportType)
        {
            case ImportType.ClearAndImport:
                result = await ClearAndImport(data);
                break;
            case ImportType.ReplaceExisting:
            {
                result = await ReplaceExisting(data);
                break;
            }
            case ImportType.AddNew:
            {
                result = await AddNew(data);
                break;
            }
            default:
                throw new NotSupportedException($"Import type {data.ImportType} not supported for full imports");
        }

        if (result.ImportedClients.Count > 0)
            _eventLogRepository.Add(_eventLogFactory.DataImported(data.ImportType, importMode, result.ImportedClients.Count, AuthenticatedUser));

        return result;
    }

    private async Task<ImportResultDataContract> ClearAndImport(FigDataExportDataContract data)
    {
        var clients = await ConvertAndValidate(data.Clients);
        var deletedClients = DeleteClients(_ => true);
        AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = data.Clients.Select(a => a.Name).ToList(),
            DeletedClients = deletedClients,
        };
    }

    private async Task<ImportResultDataContract> ReplaceExisting(FigDataExportDataContract data)
    {
        var clients = await ConvertAndValidate(data.Clients);
        var importedClients = data.Clients.Select(a => a.GetIdentifier());
        var deletedClients = DeleteClients(a => importedClients.Contains(a.GetIdentifier()));
        AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = data.Clients.Select(a => a.Name).ToList(),
            DeletedClients = deletedClients,
        };
    }

    private async Task<ImportResultDataContract> AddNew(FigDataExportDataContract data)
    {
        var existingClients = _settingClientRepository.GetAllClients(AuthenticatedUser).Select(a => a.GetIdentifier());
        var clientsToAdd = data.Clients.Where(a => !existingClients.Contains(a.GetIdentifier())).ToList();
        var clients = await ConvertAndValidate(clientsToAdd);
        AddClients(clients);

        return new ImportResultDataContract
        {
            ImportType = data.ImportType,
            ImportedClients = clientsToAdd.Select(a => a.Name).ToList()
        };
    }

    private void AddDeferredImport(SettingClientValueExportDataContract clientToUpdate)
    {
        var businessEntity = _deferredClientConverter.Convert(clientToUpdate, AuthenticatedUser);
        _deferredClientImportRepository.SaveClient(businessEntity);
    }

    private void UpdateClient(SettingClientBusinessEntity client, SettingClientValueExportDataContract clientToUpdate)
    {
        var timeOfUpdate = DateTime.UtcNow;
        var changes = _settingApplier.ApplySettings(client, clientToUpdate.Settings);
        client.LastSettingValueUpdate = timeOfUpdate;
        _settingClientRepository.UpdateClient(client);
        _settingChangeRecorder.RecordSettingChanges(changes, null, timeOfUpdate, client, AuthenticatedUser?.Username);
    }

    private void RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.Value is DataGridSettingBusinessEntity dataGridVal
                ? ChangedSetting.GetDataGridValue(dataGridVal)
                : setting.Value;
            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = value,
                ChangedBy = "REGISTRATION"
            });
        }
    }

    private async Task<List<SettingClientBusinessEntity>> ConvertAndValidate(
        List<SettingClientExportDataContract> importClients)
    {
        List<SettingClientBusinessEntity> clients = new();
        foreach (var clientToAdd in importClients)
        {
            var client = _clientExportConverter.Convert(clientToAdd);

            foreach (var verification in client.DynamicVerifications)
                await _settingVerifier.Compile(verification);

            client.Settings.ToList().ForEach(a => a.Validate());
            clients.Add(client);
        }

        return clients;
    }
    
    private void AddClients(List<SettingClientBusinessEntity> clients)
    {
        foreach (var client in clients)
        {
            client.LastRegistration = DateTime.UtcNow;

            _settingClientRepository.RegisterClient(client);
            RecordInitialSettingValues(client);
            _eventLogRepository.Add(_eventLogFactory.Imported(client, AuthenticatedUser));
        }
    }

    private List<string> DeleteClients(Func<SettingClientBusinessEntity, bool> selector)
    {
        var clients = _settingClientRepository.GetAllClients(AuthenticatedUser);

        var names = new List<string>();
        foreach (var client in clients.Where(selector))
        {
            _settingClientRepository.DeleteClient(client);
            _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, client.Name, client.Instance, AuthenticatedUser));
            names.Add(client.Name);
        }

        return names;
    }
}