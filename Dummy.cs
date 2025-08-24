using ResumableFunctions.Attributes;
using ResumableFunctions.Data;
using ResumableFunctions.Extensions;

namespace ResumableFunctions;

public class Dummy
{
    public string something = "Hello";
    public int thing = 42;
    private int counter;
    private static int staticCounter = 100;

    [ResumableFunction]
    public static async IAsyncEnumerable<ResumableFunctionState<int>> StaticMethod(float explodeChance)
    {
        //var rng = new Random();
        await Task.Delay(100);
        int result = 30;
        yield return ResumableFunctionState.Yield();
        await Task.Delay(100);
        result++;
        yield return ResumableFunctionState.Yield();
        result++;
        float chance = 0;//rng.NextSingle();
        Console.WriteLine(chance);
        if (chance < explodeChance)
        {
            throw new ArgumentException("It exploded");
        }
        yield return ResumableFunctionState.Yield();
        await Task.Delay(100);
        result++;
        yield return ResumableFunctionState.Success(result);
    }

    [ResumableFunction]
    public async IAsyncEnumerable<ResumableFunctionState<string>> ThatMethod()
    {
        yield return ResumableFunctionState.Yield();
        yield return ResumableFunctionState.Yield();
        await Task.Yield();
        await PrivateMethod();
        yield return ResumableFunctionState.Yield();
        await PrivateMethod();
        yield return ResumableFunctionState.Success("Hello");
    }

    [ResumableFunction("My super method", 1)]
    private static async IAsyncEnumerable<ResumableFunctionState> PrivateMethod()
    {
        await Task.Yield();
        await Task.Delay(300);
        staticCounter += 7;
        Console.WriteLine("Private method...");
        await Task.Delay(300);
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
