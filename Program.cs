using System.Diagnostics;
using ResumableFunctions;
using ResumableFunctions.Extensions;

long start = Stopwatch.GetTimestamp();
InstanceProvider.Register<IStorage>(new Storage("./states"));
var manager = new ResumableManager()
    .RegisterAssembly<Program>()
    .RegisterAssembly<Dummy>()
    .Init();
InstanceProvider.Register<IResumableManager>(manager);
Console.WriteLine($"Init time: {Stopwatch.GetElapsedTime(start)}");

Console.WriteLine("START:");
/**/
var test = new Dummy();
Console.WriteLine($"=== {nameof(test.SomeMethod)} ===");
int x = await test.SomeMethod(30);
Console.WriteLine(x);
/*
Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(true) ===");
await test.AnotherMethod(true);
Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(false) ===");
await test.AnotherMethod(false);
Console.WriteLine($"\n=== {nameof(test.ThatMethod)} ===");
string? y = await test.ThatMethod();
Console.WriteLine(y);
*/
//int r = await Dummy.StaticMethod(0.2f);
//Console.WriteLine(r);

Console.WriteLine("RESUME:");
//await InstanceProvider.Get<IResumableManager>().ResumeAll();
foreach (string filename in InstanceProvider.Get<IStorage>().EnumerateFiles())
{
    var resumedMethod = await InstanceProvider.Get<IResumableManager>().Resume<int>(filename);
    int result = await resumedMethod;
    Console.WriteLine(result);
}
