using System.Diagnostics;
using ResumableFunctions;
using ResumableFunctions.Extensions;

long start = Stopwatch.GetTimestamp();
var manager = new ResumableManager(new Storage("./states"))
    .RegisterAssembly<Program>()
    .RegisterAssembly<Dummy>()
    .Init();
InstanceProvider.Register<IResumableManager>(manager);
Console.WriteLine($"Init time: {Stopwatch.GetElapsedTime(start)}");

var test = new Dummy();
Console.WriteLine($"=== {nameof(test.SomeMethod)} ===");
var x = await test.SomeMethod(30);
Console.WriteLine(x);
Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(true) ===");
await test.AnotherMethod(true);
Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(false) ===");
await test.AnotherMethod(false);
Console.WriteLine($"\n=== {nameof(test.ThatMethod)} ===");
string? y = await test.ThatMethod();
Console.WriteLine(y);

//InstanceProvider.Get<IResumableManager>()
