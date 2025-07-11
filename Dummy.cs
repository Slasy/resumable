using ResumableFunctions.Attributes;

namespace ResumableFunctions;

public class Dummy
{
    public string something = "Hello";
    public int thing = 42;

    [ResumableFunction(0)]
    public IEnumerable<ResumableFunctionState<string>> ThatMethod()
    {
        yield return ResumableFunctionState.Checkpoint();
        yield return ResumableFunctionState.Checkpoint();
        yield return ResumableFunctionState.Checkpoint();
        yield return ResumableFunctionState.Success("Hello");
    }

    [ResumableFunction(0)]
    public async IAsyncEnumerable<ResumableFunctionState<int>> SomeMethod(int someParam = 10, string otherParam = "_")
    {
        someParam += 3;
        something = "A";
        Console.WriteLine("starting some method");
        yield return ResumableFunctionState.Checkpoint();
        something = "B";
        await Task.Delay(500);
        someParam++;
        int a = someParam;
        await Task.Yield();
        yield return ResumableFunctionState.Checkpoint();
        a++;
        await Task.Delay(500);
        otherParam += "\\/";
        yield return ResumableFunctionState.Checkpoint();
        await Task.Delay(500);
        Console.WriteLine(otherParam);
        something = "C";
        if (a > 10)
        {
            yield return ResumableFunctionState.Success(a);
            //yield return TransactionState.Success();
            //yield break; // Success state will handle quiting method early
        }
        if (a > 0)
        {
            yield return ResumableFunctionState.Fail(-1);
            //yield return TransactionState.Fail();
        }
        yield return ResumableFunctionState.Fail();
    }

    public async IAsyncEnumerable<ResumableFunctionState> AnotherMethod(bool x)
    {
        yield return ResumableFunctionState.Checkpoint();
        await Task.Delay(100);
        yield return ResumableFunctionState.Checkpoint();
        await Task.Delay(100);
        yield return ResumableFunctionState.Checkpoint();
        await Task.Delay(100);
        yield return ResumableFunctionState.Checkpoint();
        if (x)
        {
            yield return ResumableFunctionState.Success();
        }
        else
        {
            yield return ResumableFunctionState.Fail();
        }
    }
}
