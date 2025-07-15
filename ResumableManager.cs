using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using ResumableFunctions.Attributes;
using ResumableFunctions.Converters;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public sealed record RegisteredMethod(ResumableFunctionAttribute ResumeAttribute, MethodInfo OriginalMethod, Type StateMachine)
{
    public override string ToString()
    {
        return $"{nameof(RegisteredMethod)}: '{ResumeAttribute.MethodNameOverride}'[{ResumeAttribute.ImplementationVersion}] ({OriginalMethod.Name}), {StateMachine.FullName}";
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
        },
    };

    public IReadOnlyCollection<RegisteredMethod> RegisteredMethods => registeredMethods;

    public ResumableManager(IStorage storage) => this.storage = storage;

    public ResumableManager RegisterAssembly<T>() => RegisterAssembly(typeof(T));

    public ResumableManager RegisterAssembly(Type type) => RegisterAssembly(type.Assembly);

    public ResumableManager RegisterAssembly(Assembly assembly)
    {
        assemblies.Add(assembly);
        return this;
    }

    public ResumableManager Init()
    {
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
                            registeredMethods.Add(new RegisteredMethod(resumeAttribute, method, machineAttribute.StateMachineType));
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

    public async Task Save(IAsyncEnumerator<ResumableFunctionState> asyncEnumerator)
    {
        ResumableData<ResumableFunctionState> data = PrepareData(asyncEnumerator);
        await storage.Save($"{data.Metadata.Name}.json", Serialize(data));
    }

    public async Task Save<T>(IAsyncEnumerator<ResumableFunctionState<T>> asyncEnumerator)
    {
        ResumableData<ResumableFunctionState<T>> data = PrepareData(asyncEnumerator);
        await storage.Save($"{data.Metadata.Name}.json", Serialize(data));
    }

    public bool TryGetOriginalMethod<T>(IAsyncEnumerable<ResumableFunctionState<T>> enumerable, [NotNullWhen(true)] out RegisteredMethod? method)
    {
        return TryGetOriginalMethodInternal(enumerable.GetType(), out method);
    }

    public bool TryGetOriginalMethod(IAsyncEnumerable<ResumableFunctionState> enumerable, [NotNullWhen(true)] out RegisteredMethod? method)
    {
        return TryGetOriginalMethodInternal(enumerable.GetType(), out method);
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

    private ResumableData<T> PrepareData<T>(IAsyncEnumerator<T> enumerator)
    {
        var method = GetOriginalMethodOrFail(enumerator.GetType());
        return new ResumableData<T>
        {
            EnumeratorState = enumerator,
            Metadata = new ResumableData<T>.MData
            {
                Name = method.OriginalMethod.Name,
            },
        };
    }

    private string Serialize(object obj) => JsonConvert.SerializeObject(obj, JSON_FORMATTING, settings);
}
