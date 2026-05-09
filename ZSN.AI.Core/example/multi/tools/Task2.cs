using System;
using System.IO;

var step1 = File.Exists("step1.txt") ? File.ReadAllText("step1.txt") : string.Empty;
var output = $"Task2Result:{step1}";
File.WriteAllText("step2.txt", output);
