using System.Collections.Concurrent;

namespace ResumableFunctions;

/// <summary>
/// Simple "dependency injection", just for prototyping
/// </summary>
public static class InstanceProvider
{
    private static readonly ConcurrentDictionary<Type, Func<object>> factories = new();
    private static readonly ConcurrentDictionary<Type, object> instances = new();

    public static void Register<TType>(TType instance)
        where TType : class
    {
        instances.TryAdd(typeof(TType), instance);
    }

    public static void Register<TType, TInstance>()
        where TType : class
        where TInstance : class, TType, new()
    {
        instances.TryAdd(typeof(TType), new TInstance());
    }

    public static void Register<TType>(Func<TType> factory)
        where TType : class
    {
        factories.TryAdd(typeof(TType), factory);
    }

    public static TType Get<TType>()
        where TType : class
    {
        if (instances.TryGetValue(typeof(TType), out var value) && value is TType instance)
        {
            return instance;
        }
        if (factories.TryRemove(typeof(TType), out var factory))
        {
            instances.TryAdd(typeof(TType), instance = (TType)factory());
            return instance;
        }
        throw new Exception($"Instance of type {typeof(TType).Name} could not be found.");
    }
}
