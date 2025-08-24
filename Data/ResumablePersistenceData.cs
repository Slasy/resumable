using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ResumableFunctions.Data;

internal sealed class ResumablePersistenceData
{
    internal sealed class ResumableMetaData
    {
        public required string Id { get; init; }
        public required string MethodName { get; init; }
        public required DateTime StartTime { get; init; }
        public DateTime LastUpdateTime { get; } = DateTime.UtcNow;
        public required Type StateMachineType { get; init; }
        public required bool IsStatic { get; init; }
        public required BindingFlags Flags { get; init; }
    }

    [JsonProperty("Metadata")]
    public required ResumableMetaData Metadata { get; init; }
    [JsonProperty("State")]
    public string SerializedEnumeratorState { get; set; } = "{}";

    public ResumablePersistenceData SetEnumeratorState<T>(
        IAsyncEnumerator<T> enumerator,
        JsonSerializerSettings? jsonSerializerSettings = null
    )
    {
        SerializedEnumeratorState = StateMachineDump.Dump(enumerator).Serialize(jsonSerializerSettings);
        return this;
    }

    public void Restore(object enumeratorToRestore, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        var state = StateMachineDump.Deserialize(SerializedEnumeratorState, jsonSerializerSettings);
        state.Restore(enumeratorToRestore);
    }
}
