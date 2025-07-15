using System.Diagnostics;
using ResumableFunctions;
using ResumableFunctions.Extensions;

long start = Stopwatch.GetTimestamp();
var manager = new ResumableManager(new Storage("."))
    .RegisterAssembly<Program>()
    .RegisterAssembly<Dummy>()
    .Init();
ResumableFunctionExtensions.manager = manager;
Console.WriteLine($"Init time: {Stopwatch.GetElapsedTime(start)}");

Console.WriteLine("### registered resumable functions ###");
foreach (var method in manager.RegisteredMethods)
{
    Console.WriteLine(method.ToString());
}

var test = new Dummy();
Console.WriteLine($"=== {nameof(test.SomeMethod)} ===");
var x = await test.SomeMethod(30);
Console.WriteLine(x);
Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(true) ===");
await test.AnotherMethod(true);
//Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(false) ===");
//await test.AnotherMethod(false);
