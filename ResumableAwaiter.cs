using System.Runtime.CompilerServices;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public class ResumableAwaiter : ICriticalNotifyCompletion, IAsyncDisposable
{
    private readonly IAsyncEnumerator<ResumableFunctionState> enumerator;

    protected readonly IResumableManager manager;

    protected Action? continuationAction;
    protected object? resultValue;
    protected Task task = null!;
    public readonly DateTime startTime;

    public bool IsCompleted { get; protected set; }

    public void GetResult() { }

    protected ResumableAwaiter(IResumableManager manager)
    {
        startTime = DateTime.UtcNow;
        this.manager = manager;
        enumerator = null!;
    }

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState> asyncEnum, IResumableManager manager) : this(manager)
    {
        enumerator = asyncEnum.GetAsyncEnumerator();
        task = RunEnumeration();
    }

    public ResumableAwaiter(IAsyncEnumerator<ResumableFunctionState> asyncEnum, IResumableManager manager) : this(manager)
    {
        enumerator = asyncEnum;
        task = RunEnumeration();
    }

    public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation)
    {
        if (IsCompleted) continuation();
        else continuationAction = continuation;
    }

    public virtual async ValueTask DisposeAsync()
    {
        await task;
        await enumerator.DisposeAsync();
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

    private async Task RunEnumeration()
    {
        await Task.Yield();
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                switch (enumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Yield:
                        await manager.Save(this, enumerator);
                        break;
                    case ResumableFunctionState.State.CompleteSuccess:
                    case ResumableFunctionState.State.CompleteFail:
                        await manager.Remove(this, enumerator);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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
    private readonly IAsyncEnumerator<ResumableFunctionState<T>> enumerator;

    public new T? GetResult() => (T?)resultValue;

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState<T>> asyncEnum, IResumableManager manager) : base(manager)
    {
        enumerator = asyncEnum.GetAsyncEnumerator();
        task = RunEnumeration();
    }

    public ResumableAwaiter(IAsyncEnumerator<ResumableFunctionState<T>> asyncEnum, IResumableManager manager) : base(manager)
    {
        enumerator = asyncEnum;
        task = RunEnumeration();
    }

    public override async ValueTask DisposeAsync()
    {
        await task;
        await enumerator.DisposeAsync();
        task.Dispose();
    }

    private async Task RunEnumeration()
    {
        await Task.Yield();
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                switch (enumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Yield:
                        await manager.Save(this, enumerator);
                        break;
                    case ResumableFunctionState.State.CompleteSuccess:
                        resultValue = enumerator.Current.resultValue;
                        await manager.Remove(this, enumerator);
                        break;
                    case ResumableFunctionState.State.CompleteFail:
                        await manager.Remove(this, enumerator);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            IsCompleted = true;
            continuationAction?.Invoke();
        }
    }
}
