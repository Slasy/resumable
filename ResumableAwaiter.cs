using System.Runtime.CompilerServices;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public sealed class ResumableAwaiter<T> : ResumableAwaiter
{
    public new T? GetResult() => (T?)resultValue;

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState<T>> asyncEnum, IResumableManager manager) : base(manager)
    {
        task = RunEnumeration(asyncEnum);
    }

    private async Task RunEnumeration(IAsyncEnumerable<ResumableFunctionState<T>> enumerable)
    {
        manager.TryGetOriginalMethod(enumerable, out var method);
        await Task.Yield();
        await using var asyncEnumerator = enumerable.GetAsyncEnumerator();
        try
        {
            while (await asyncEnumerator.MoveNextAsync())
            {
                await manager.Save(asyncEnumerator);
                switch (asyncEnumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Yield:
                        Console.WriteLine("checkpoint");
                        break;
                    case ResumableFunctionState.State.CompleteSuccess:
                        Console.WriteLine("done");
                        resultValue = asyncEnumerator.Current.resultValue;
                        return;
                    case ResumableFunctionState.State.CompleteFail:
                        Console.WriteLine("fail");
                        resultValue = asyncEnumerator.Current.resultValue;
                        return;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            Console.WriteLine("finishing awaiter");
            IsCompleted = true;
            Console.WriteLine("invoking continuation method");
            continuationAction?.Invoke();
        }
    }
}

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
        Console.WriteLine("on completed called");
        if (IsCompleted) continuation();
        else continuationAction = continuation;
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("disposing awaiter");
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
                await manager.Save(asyncEnumerator);
                switch (asyncEnumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Yield:
                        Console.WriteLine("checkpoint");
                        break;
                    case ResumableFunctionState.State.CompleteSuccess:
                        Console.WriteLine("done");
                        return;
                    case ResumableFunctionState.State.CompleteFail:
                        Console.WriteLine("fail");
                        return;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            Console.WriteLine("finishing awaiter");
            IsCompleted = true;
            Console.WriteLine("invoking continuation method");
            continuationAction?.Invoke();
        }
    }
}
