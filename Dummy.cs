using ResumableFunctions.Attributes;
using ResumableFunctions.Data;

namespace ResumableFunctions;

public class Dummy
{
    public string something = "Hello";
    public int thing = 42;
    private int counter;

    [ResumableFunction]
    public async IAsyncEnumerable<ResumableFunctionState<string>> ThatMethod()
    {
        yield return ResumableFunctionState.Yield();
        yield return ResumableFunctionState.Yield();
        await Task.Yield();
        yield return ResumableFunctionState.Yield();
        yield return ResumableFunctionState.Success("Hello");
    }

    //[ResumableFunction("My super method",1)]
    private static async IAsyncEnumerable<ResumableFunctionState> PrivateMethod()
    {
        await Task.Yield();
        yield return ResumableFunctionState.Success();
    }

    [ResumableFunction]
    public async IAsyncEnumerable<ResumableFunctionState<int>> SomeMethod(int someParam = 10, string otherParam = "_")
    {
        counter++;
        someParam += 3;
        something = "A";
        Console.WriteLine("starting some method");
        yield return ResumableFunctionState.Yield();
        counter++;
        something = "B";
        await Task.Delay(500);
        someParam++;
        int a = someParam;
        await Task.Yield();
        yield return ResumableFunctionState.Yield();
        counter++;
        a++;
        await Task.Delay(500);
        otherParam += "\\/";
        yield return ResumableFunctionState.Yield();
        counter++;
        await Task.Delay(500);
        Console.WriteLine(otherParam);
        something = "C";
        if (a > 10)
        {
            counter++;
            yield return ResumableFunctionState.Success(a);
            //yield return TransactionState.Success();
            //yield break; // Success state will handle quiting method early
        }
        if (a > 0)
        {
            counter++;
            yield return ResumableFunctionState.Fail(-1);
            //yield return TransactionState.Fail();
        }
        counter++;
        yield return ResumableFunctionState.Fail();
    }

    [ResumableFunction("Another method v1")]
    public async IAsyncEnumerable<ResumableFunctionState> AnotherMethod(bool x)
    {
        yield return ResumableFunctionState.Yield();
        await Task.Delay(100);
        yield return ResumableFunctionState.Yield();
        await Task.Delay(100);
        yield return ResumableFunctionState.Yield();
        await Task.Delay(100);
        yield return ResumableFunctionState.Yield();
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
