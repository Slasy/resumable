using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using ResumableFunctions.Converters;

namespace ResumableFunctions;

public sealed class ResumableAwaiter<T> : ResumableAwaiter
{
    public new T? GetResult() => (T?)resultValue;

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState<T>> asyncEnum)
    {
        task = RunEnumeration(asyncEnum);
    }

    private async Task RunEnumeration(IAsyncEnumerable<ResumableFunctionState<T>> enumerable)
    {
        string state = "{}";
        await Task.Yield();
        await using var asyncEnumerator = enumerable.GetAsyncEnumerator();
        try
        {
            while (await asyncEnumerator.MoveNextAsync())
            {
                state = JsonConvert.SerializeObject(asyncEnumerator, asyncEnumerator.GetType(), jsonFormatting, settings);
                Console.WriteLine(state);
                switch (asyncEnumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Checkpoint:
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
    protected readonly Formatting jsonFormatting = Formatting.None;
    protected readonly JsonSerializerSettings settings = new()
    {
        //ContractResolver = new AllFieldsResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        Converters = { new AsyncEnumeratorConverter() },
    };

    protected Action? continuationAction;
    protected object? resultValue;
    protected Task task = null!;

    public bool IsCompleted { get; protected set; }

    public virtual void GetResult() { }

    protected ResumableAwaiter() { }

    public ResumableAwaiter(IAsyncEnumerable<ResumableFunctionState> asyncEnum)
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

    private async Task RunEnumeration(IAsyncEnumerable<ResumableFunctionState> enumerable)
    {
        string state = "{}";
        await Task.Yield();
        await using var asyncEnumerator = enumerable.GetAsyncEnumerator();
        try
        {
            while (await asyncEnumerator.MoveNextAsync())
            {
                state = JsonConvert.SerializeObject(asyncEnumerator, asyncEnumerator.GetType(), jsonFormatting, settings);
                Console.WriteLine(state);
                switch (asyncEnumerator.Current.stateValue)
                {
                    case ResumableFunctionState.State.Checkpoint:
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
