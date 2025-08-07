using System.Diagnostics.CodeAnalysis;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public interface IResumableManager
{
    Task Save(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState> asyncEnumerator);
    Task Save<T>(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState<T>> asyncEnumerator);
    Task Remove(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState> asyncEnumerator);
    Task Remove<T>(ResumableAwaiter awaiter, IAsyncEnumerator<ResumableFunctionState<T>> asyncEnumerator);
    bool TryGetOriginalMethod(IAsyncEnumerable<ResumableFunctionState> enumerable, [NotNullWhen(true)] out RegisteredMethod? method);
    bool TryGetOriginalMethod<T>(IAsyncEnumerable<ResumableFunctionState<T>> enumerable, [NotNullWhen(true)] out RegisteredMethod? method);
}
