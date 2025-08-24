using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ResumableFunctions.Attributes;
using ResumableFunctions.Converters;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public sealed record RegisteredMethod(ResumableFunctionAttribute ResumeAttribute, MethodInfo OriginalMethod, Type StateMachine)
{
    public override string ToString()
    {
        return string.IsNullOrEmpty(ResumeAttribute.MethodNameOverride)
            ? $"{nameof(RegisteredMethod)}: '{OriginalMethod.Name}'[{ResumeAttribute.ImplementationVersion}]; {StateMachine.FullName}"
            : $"{nameof(RegisteredMethod)}: '{ResumeAttribute.MethodNameOverride}'[{ResumeAttribute.ImplementationVersion}] ('{OriginalMethod.Name}'); {StateMachine.FullName}";
    }
}

public sealed class ResumableManager : IResumableManager
{
    private readonly IStorage storage;
    private readonly HashSet<Assembly> assemblies = [];
    private readonly List<RegisteredMethod> registeredMethods = [];
    private const Formatting JSON_FORMATTING = Formatting.Indented;
    private readonly JsonSerializerSettings settings = new()
    {
        Converters =
        {
            new AsyncEnumeratorConverter(),
            //new StringEnumConverter(new DefaultNamingStrategy()),
        },
    };

    public ResumableManager()
    {
        storage = InstanceProvider.Get<IStorage>();
    }

    public ResumableManager RegisterAssembly<T>() => RegisterAssembly(typeof(T));

    public ResumableManager RegisterAssembly(Type type) => RegisterAssembly(type.Assembly);

    public ResumableManager RegisterAssembly(Assembly assembly)
    {
        assemblies.Add(assembly);
        return this;
    }

    public ResumableManager Init()
    {
        Console.WriteLine("### registered resumable functions ###");
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.GetCustomAttribute<AsyncIteratorStateMachineAttribute>() is { } machineAttribute && HasCorrectReturnType(method))
                    {
                        if (method.GetCustomAttribute<ResumableFunctionAttribute>() is { } resumeAttribute)
                        {
                            RegisteredMethod regMethod;
                            registeredMethods.Add(regMethod = new RegisteredMethod(resumeAttribute, method, machineAttribute.StateMachineType));
                            Console.WriteLine(regMethod.ToString());
                        }
                        else
                        {
                            // log warning
                            string methodName = $"{method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "<no type>"}::{method.Name}(...)";
                            Console.WriteLine($"Looks like resumable method but it's missing {nameof(ResumableFunctionAttribute)}: {methodName}");
                        }
                    }
                }
            }
        }
        return this;
    }

    public async Task Save(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState> asyncEnumerator)
    {
        ResumablePersistenceData data = PrepareData(awaiter, asyncEnumerator);
        await storage.Save(FileName(data), Serialize(data));
    }

    public async Task Save<T>(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState<T>> asyncEnumerator)
    {
        ResumablePersistenceData data = PrepareData(awaiter, asyncEnumerator);
        await storage.Save(FileName(data), Serialize(data));
    }

    public async Task Remove(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState> asyncEnumerator)
    {
        ResumablePersistenceData data = PrepareData(awaiter, asyncEnumerator);
        await storage.Delete(FileName(data));
    }

    public async Task Remove<T>(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState<T>> asyncEnumerator)
    {
        ResumablePersistenceData data = PrepareData(awaiter, asyncEnumerator);
        await storage.Delete(FileName(data));
    }

    public bool TryGetOriginalMethod<T>(IAsyncEnumerable<ResumableFunctionState<T>> enumerable, [NotNullWhen(true)] out RegisteredMethod? method)
    {
        return TryGetOriginalMethodInternal(enumerable.GetType(), out method);
    }

    public async Task<IAsyncEnumerator<ResumableFunctionState>> Resume(string filename)
    {
        string jsonContent = await storage.Load(filename);
        var resumeData = JsonConvert.DeserializeObject<ResumablePersistenceData>(jsonContent, settings);
        var resumedEnumerator = (IAsyncEnumerator<ResumableFunctionState>)JsonConvert.DeserializeObject(resumeData.SerializedEnumeratorState, resumeData.Metadata.StateMachineType, settings);
        return resumedEnumerator;
    }

    public async Task<IAsyncEnumerator<ResumableFunctionState<TOut>>> Resume<TOut>(string filename)
    {
        string jsonContent = await storage.Load(filename);
        var resumeData = JsonConvert.DeserializeObject<ResumablePersistenceData>(jsonContent, settings);
        object stateMachineInstance = Activator.CreateInstance(resumeData.Metadata.StateMachineType, 0)!;
        resumeData.Restore(stateMachineInstance, settings);
        //var resumedEnumerator = (IAsyncEnumerator<ResumableFunctionState<TOut>>)JsonConvert.DeserializeObject(resumeData.SerializedEnumeratorState, resumeData.Metadata.StateMachineType, settings);
        return (IAsyncEnumerator<ResumableFunctionState<TOut>>)stateMachineInstance;
    }

    public Task<IAsyncEnumerator<ResumableFunctionState>> Resume(string filename, object existingInstance)
    {
        throw new NotImplementedException();
    }

    public Task<IAsyncEnumerator<ResumableFunctionState<TOut>>> Resume<TOut>(string filename, object existingInstance)
    {
        throw new NotImplementedException();
    }

    public bool TryGetOriginalMethod(IAsyncEnumerable<ResumableFunctionState> enumerable, [NotNullWhen(true)] out RegisteredMethod? method)
    {
        return TryGetOriginalMethodInternal(enumerable.GetType(), out method);
    }

    public async Task ResumeAll()
    {
        foreach (string filename in storage.EnumerateFiles().Order())
        {
            string jsonContent = await storage.Load(filename);
            //ResumableData<ResumableFunctionState<T>>
            JObject json = JObject.Parse(jsonContent);
            Console.WriteLine(json);
            string methodName = json["Metadata"]!["MethodName"]!.Value<string>()!;
            bool isStatic = json["Metadata"]!["IsStatic"]!.Value<bool>();
            BindingFlags flags = (BindingFlags)json["Metadata"]!["Flags"]!.Value<int>();
            string fullTypeName = json["Metadata"]!["Type"]!.Value<string>()!;
            Type targetType = Type.GetType(fullTypeName) ?? throw new ArgumentException($"Type not found: '{fullTypeName}'");
            MethodInfo method = targetType.GetMethod(methodName, flags) ?? throw new ArgumentException($"Method not found: '{fullTypeName}::{methodName}'");
            Type enumeratorType = typeof(IAsyncEnumerator<>).MakeGenericType( /*TODO*/);
            if (!isStatic)
            {
                object instance = Activator.CreateInstance(targetType) ?? throw new ArgumentException($"Instance failed: '{fullTypeName}'");
                //json["EnumeratorState"].
            }
            else { }
        }
    }

    private static string FileName(ResumablePersistenceData data)
    {
        return $"{data.Metadata.Id}_{data.Metadata.MethodName}.json";
    }

    private RegisteredMethod GetOriginalMethodOrFail(Type stateMachineType)
    {
        if (!TryGetOriginalMethodInternal(stateMachineType, out var method))
        {
            throw new InvalidOperationException($"State machine doesn't look like resumable function: {stateMachineType.FullName}");
        }
        return method;
    }

    private bool TryGetOriginalMethodInternal(Type stateMachineType, [NotNullWhen(true)] out RegisteredMethod? method)
    {
        method = registeredMethods.SingleOrDefault(x => x.StateMachine == stateMachineType);
        return method is not null;
    }

    /// <summary>
    /// Check if method returns <see cref="IAsyncEnumerable{T}"/> with
    /// <see cref="ResumableFunctionState"/> or <see cref="ResumableFunctionState{T}"/>
    /// </summary>
    private bool HasCorrectReturnType(MethodInfo method)
    {
        if (!method.ReturnType.IsGenericType) return false;
        if (method.ReturnType.GetGenericTypeDefinition() != typeof(IAsyncEnumerable<>)) return false;
        if (method.ReturnType == typeof(IAsyncEnumerable<ResumableFunctionState>)) return true;
        if (!method.ReturnType.GenericTypeArguments[0].IsGenericType) return false;
        return method.ReturnType.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(ResumableFunctionState<>);
    }

    private ResumablePersistenceData PrepareData<T>(ResumableAwaiter awaiter, IAsyncEnumerator<T> enumerator)
    {
        RegisteredMethod method = GetOriginalMethodOrFail(enumerator.GetType());
        return new ResumablePersistenceData
        {
            Metadata = new ResumablePersistenceData.ResumableMetaData
            {
                Id = $"{awaiter.startTime:yyMMddHHmmssfff}T",
                StartTime = awaiter.startTime,
                MethodName = method.OriginalMethod.Name,
                StateMachineType = method.StateMachine,
                IsStatic = method.OriginalMethod.IsStatic,
                Flags = AttrToFlags(method.OriginalMethod),
            },
        }.SetEnumeratorState(enumerator, settings);

        static BindingFlags AttrToFlags(MethodInfo method)
        {
            BindingFlags flags = 0;
            flags |= method.Attributes.HasFlag(MethodAttributes.Public) ? BindingFlags.Public : 0;
            flags |= method.Attributes.HasFlag(MethodAttributes.Private) ? BindingFlags.NonPublic : 0;
            flags |= method.Attributes.HasFlag(MethodAttributes.Static) ? BindingFlags.Static : BindingFlags.Instance;
            return flags;
        }
    }

    private string Serialize(object obj) => JsonConvert.SerializeObject(obj, JSON_FORMATTING, settings);
    private T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, settings) ?? throw new NullReferenceException();
    private object Deserialize(string json, Type type) => JsonConvert.DeserializeObject(json, type, settings) ?? throw new NullReferenceException();
}
