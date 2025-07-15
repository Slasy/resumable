using ResumableFunctions.Data;

namespace ResumableFunctions.Extensions;

public static class ResumableFunctionExtensions
{
    private static readonly IStorage storage = new Storage(".");
    /// <summary>ONLY FOR PROTOTYPE, should be replaced by dependency injection.</summary>
    [Obsolete("Replace with dependency injection")]
    public static IResumableManager manager;

    public static ResumableAwaiter<T> GetAwaiter<T>(this IAsyncEnumerable<ResumableFunctionState<T>> self)
    {
        return new ResumableAwaiter<T>(self, manager);
    }

    public static ResumableAwaiter GetAwaiter(this IAsyncEnumerable<ResumableFunctionState> self)
    {
        return new ResumableAwaiter(self, manager);
    }
}
