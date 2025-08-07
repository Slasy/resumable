using System.Runtime.CompilerServices;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public class ResumableAwaiter : ICriticalNotifyCompletion, IAsyncDisposable
{
    protected readonly IResumableManager manager;

    protected Action? continuationAction;
    protected object? resultValue;
    protected Task task = null!;

    public bool IsCompleted { get; protected set; }

    public void GetResult() { }

    protected ResumableAwaiter(IResumableManager manager)
    {
        this.manager = manager;
    }

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState> asyncEnum, IResumableManager manager) : this(manager)
    {
        task = RunEnumeration(asyncEnum);
    }

    public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation)
    {
        if (IsCompleted) continuation();
        else continuationAction = continuation;
    }

    public async ValueTask DisposeAsync()
    {
        await task;
        task.Dispose();
    }

    protected string GetDefaultName(object enumerable)
    {
        string name = enumerable.GetType().FullName ?? enumerable.GetType().Name;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private async Task RunEnumeration(IAsyncEnumerable<ResumableFunctionState> enumerable)
    {
        await Task.Yield();
        await using var asyncEnumerator = enumerable.GetAsyncEnumerator();
        try
        {
            while (await asyncEnumerator.MoveNextAsync())
            {
                switch (asyncEnumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Yield:
                        await manager.Save(asyncEnumerator);
                        break;
                    case ResumableFunctionState.State.CompleteSuccess:
                    case ResumableFunctionState.State.CompleteFail:
                        await manager.Remove(asyncEnumerator);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            IsCompleted = true;
            continuationAction?.Invoke();
        }
    }
}

public sealed class ResumableAwaiter<T> : ResumableAwaiter
{
    public new T? GetResult() => (T?)resultValue;

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState<T>> asyncEnum, IResumableManager manager) : base(manager)
    {
        task = RunEnumeration(asyncEnum);
    }

    private async Task RunEnumeration(IAsyncEnumerable<ResumableFunctionState<T>> enumerable)
    {
        await Task.Yield();
        await using var asyncEnumerator = enumerable.GetAsyncEnumerator();
        try
        {
            while (await asyncEnumerator.MoveNextAsync())
            {
                switch (asyncEnumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Yield:
                        await manager.Save(asyncEnumerator);
                        break;
                    case ResumableFunctionState.State.CompleteSuccess:
                        resultValue = asyncEnumerator.Current.resultValue;
                        await manager.Remove(asyncEnumerator);
                        break;
                    case ResumableFunctionState.State.CompleteFail:
                        await manager.Remove(asyncEnumerator);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            IsCompleted = true;
            continuationAction?.Invoke();
        }
    }
}
