﻿using System.Diagnostics.CodeAnalysis;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public interface IResumableManager
{
    Task Save(IAsyncEnumerator<ResumableFunctionState> asyncEnumerator);
    Task Save<T>(IAsyncEnumerator<ResumableFunctionState<T>> asyncEnumerator);
    bool TryGetOriginalMethod(IAsyncEnumerable<ResumableFunctionState> enumerable, [NotNullWhen(true)] out RegisteredMethod? method);
    bool TryGetOriginalMethod<T>(IAsyncEnumerable<ResumableFunctionState<T>> enumerable, [NotNullWhen(true)] out RegisteredMethod? method);
}
