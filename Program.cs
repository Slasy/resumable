using ResumableFunctions;
using ResumableFunctions.Extensions;

var test = new Dummy();
Console.WriteLine($"=== {nameof(test.SomeMethod)} ===");
var x = await test.SomeMethod(30);
Console.WriteLine(x);
//Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(true) ===");
//await test.AnotherMethod(true);
//Console.WriteLine($"\n=== {nameof(test.AnotherMethod)}(false) ===");
//await test.AnotherMethod(false);
