using System;
using System.IO;

var step2 = File.Exists("step2.txt") ? File.ReadAllText("step2.txt") : string.Empty;
var output = $"Task3Result:{step2}";
File.WriteAllText("step3.txt", output);
Console.WriteLine(output);
