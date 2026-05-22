using System;
using System.IO;

var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
var output = $"Task1Result:{timestamp}";
File.WriteAllText("step1.txt", output);
Console.WriteLine(output);
