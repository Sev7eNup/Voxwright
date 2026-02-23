using System.Text.Json.Nodes;

namespace WhisperShow.Core.Services.Configuration;

public interface ISettingsPersistenceService
{
    void ScheduleUpdate(Action<JsonNode> mutator);
}
