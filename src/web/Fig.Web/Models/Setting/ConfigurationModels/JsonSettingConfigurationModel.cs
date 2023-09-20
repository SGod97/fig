using Fig.Common.NetStandard.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;
using Newtonsoft.Json;
using NJsonSchema;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class JsonSettingConfigurationModel : SettingConfigurationModel<string>
{
    private JsonSchema? _jsonSchema;
    
    public JsonSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        bool isReadOnly)
        : base(dataContract, parent, isReadOnly)
    {
    }

    public void GenerateJson()
    {
        _jsonSchema ??= JsonSchema.FromJsonAsync(JsonSchemaString).Result;
        Value = _jsonSchema.ToSampleJson().ToString();
    }

    public async Task FormatJson()
    {
        if (Value == null)
            return;

        try
        {
            var jsonObject = JsonConvert.DeserializeObject(Value, JsonSettings.FigDefault);
            Value = JsonConvert.SerializeObject(jsonObject, Formatting.Indented, JsonSettings.FigDefault);
        }
        catch (Exception ex)
        {
            var message = $"Failed to format JSON. {ex.Message}";
            await Parent.SettingEvent(new SettingEventModel(Name, message, SettingEventType.ShowErrorNotification));
        }
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new JsonSettingConfigurationModel(DefinitionDataContract, parent, isReadOnly)
        {
            IsDirty = setDirty
        };

        return clone;
    }
    
    protected override void Validate(string? value)
    {
        _jsonSchema ??= JsonSchema.FromJsonAsync(JsonSchemaString).Result;

        try
        {
            var errors = _jsonSchema.Validate(value);
            
            IsValid = !errors.Any();
            if (errors.Any())
                ValidationExplanation = $"{errors.Count} errors including {errors.First()}";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            IsValid = false;
            ValidationExplanation = $"Invalid JSON. {e.Message}";
        }
    }
}