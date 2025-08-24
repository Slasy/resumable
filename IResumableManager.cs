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
    /// <summary>Restore state of stored function from <paramref name="filename"/></summary>
    /// <remarks>You must choose correct <see cref="Resume(string)"/> method based on return type of original method.</remarks>
    Task<IAsyncEnumerator<ResumableFunctionState>> Resume(string filename);
    /// <inheritdoc cref="Resume(string)"/>
    Task<IAsyncEnumerator<ResumableFunctionState<TOut>>> Resume<TOut>(string filename);
    Task<IAsyncEnumerator<ResumableFunctionState>> Resume(string filename, object existingInstance);
    Task<IAsyncEnumerator<ResumableFunctionState<TOut>>> Resume<TOut>(string filename, object existingInstance);
    Task ResumeAll();
}
