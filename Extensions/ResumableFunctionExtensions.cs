namespace ResumableFunctions.Extensions;

public static class ResumableFunctionExtensions
{
    public static ResumableAwaiter<T> GetAwaiter<T>(this IAsyncEnumerable<ResumableFunctionState<T>> self)
    {
        return new ResumableAwaiter<T>(self);
    }

    public static ResumableAwaiter GetAwaiter(this IAsyncEnumerable<ResumableFunctionState> self)
    {
        return new ResumableAwaiter(self);
    }
}
