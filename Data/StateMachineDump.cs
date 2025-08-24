using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ResumableFunctions.Data;

internal sealed class StateMachineDump
{
    private sealed class StateValues
    {
        private const int DEFAULT_MACHINE_STATE = -2;
        private int? machineStateCache;
        private string? machineStateKeyCache;

        public readonly Dictionary<string, object?> machineState = new();
        public readonly Dictionary<string, object?> localState = new();
        public readonly Dictionary<string, object?> thisState = new();

        private string MachineStateKey => (machineStateKeyCache ??= machineState.Keys.SingleOrDefault(x => Regex.IsMatch(x, @"^<>\d+__state$"))) ?? string.Empty;
        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public int MachineState => (machineStateCache ??= machineState.TryGetValue(MachineStateKey, out object? value) ? (int?)value : null) ?? DEFAULT_MACHINE_STATE;
    }

    private readonly StateValues stateValues;
    private static readonly Regex stateMachineField = new(@"^<>\d+__(?!this$)");
    private static readonly Regex localField = new(@"^<\w+>\d+__");
    private static readonly Regex thisField = new(@"^<>\d+__this$");

    private static bool IsStateMachineFiled(FieldInfo field) => stateMachineField.IsMatch(field.Name);
    private static bool IsLocalField(FieldInfo field) => localField.IsMatch(field.Name);
    private static bool IsThisInstanceField(FieldInfo field) => thisField.IsMatch(field.Name);

    private StateMachineDump()
    {
        stateValues = new();
    }

    private StateMachineDump(StateValues stateValues)
    {
        this.stateValues = stateValues;
    }

    public static StateMachineDump Dump(object stateMachine)
    {
        StateMachineDump smd = new();
        Type type = stateMachine.GetType();
        foreach (var field in GetAllFields(type))
        {
            if (IsLocalField(field))
            {
                smd.stateValues.localState.Add(field.Name, field.GetValue(stateMachine));
            }
            else if (IsStateMachineFiled(field))
            {
                smd.stateValues.machineState.Add(field.Name, field.GetValue(stateMachine));
            }
            else if (IsThisInstanceField(field))
            {
                DumpInternal(field.GetValue(stateMachine), smd.stateValues.thisState);
            }
        }
        return smd;
    }

    public void Restore(object stateMachineToRestore)
    {
        Type type = stateMachineToRestore.GetType();
        foreach (var field in GetAllFields(type))
        {
            if (localField.IsMatch(field.Name))
            {
                if (stateValues.localState.TryGetValue(field.Name, out object? value))
                {
                    field.SetValue(stateMachineToRestore, value);
                }
            }
            else if (stateMachineField.IsMatch(field.Name))
            {
                if (stateValues.machineState.TryGetValue(field.Name, out object? value))
                {
                    field.SetValue(stateMachineToRestore, value);
                }
            }
            else if (thisField.IsMatch(field.Name))
            {
                if (field.GetValue(stateMachineToRestore) is not { } thisInstance)
                {
                    thisInstance = Activator.CreateInstance(field.FieldType)!;
                    field.SetValue(stateMachineToRestore, thisInstance);
                }
                RestoreInternal(thisInstance, stateValues.thisState);
            }
        }
    }

    public string Serialize(JsonSerializerSettings? settings = null)
    {
        return JsonConvert.SerializeObject(stateValues, Formatting.None, settings) ?? "{}";
    }

    public static StateMachineDump Deserialize(string stateMachineJson, JsonSerializerSettings? settings = null)
    {
        var state = JsonConvert.DeserializeObject<StateValues>(stateMachineJson, settings) ?? new StateValues();
        return new StateMachineDump(state);
    }

    private static void DumpInternal(object? reference, Dictionary<string, object?> values)
    {
        if (reference is null) return;
        Type type = reference.GetType();
        foreach (var field in GetAllFields(type))
        {
            values[field.Name] = field.GetValue(reference);
        }
    }

    private static void RestoreInternal(object reference, Dictionary<string, object?> fields)
    {
        Type type = reference.GetType();
        foreach (var field in GetAllFields(type))
        {
            if (!fields.TryGetValue(field.Name, out object? value)) continue;
            field.SetValue(reference, value);
        }
    }

    private static IEnumerable<FieldInfo> GetAllFields(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
