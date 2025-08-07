using ResumableFunctions.Data;

namespace ResumableFunctions.Extensions;

public static class ResumableFunctionExtensions
{
    public static ResumableAwaiter<T> GetAwaiter<T>(this IAsyncEnumerable<ResumableFunctionState<T>> self)
    {
        return new ResumableAwaiter<T>(self, InstanceProvider.Get<IResumableManager>());
    }

    public static ResumableAwaiter GetAwaiter(this IAsyncEnumerable<ResumableFunctionState> self)
    {
        return new ResumableAwaiter(self, InstanceProvider.Get<IResumableManager>());
    }
}
