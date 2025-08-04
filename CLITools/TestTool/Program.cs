// See https://aka.ms/new-console-template for more information
using ChicksLib.IO;
using ChicksLib.Audio;
using System.IO;

Console.WriteLine("TestTool");

FileStream fs = new FileStream("fuck.wem", FileMode.Open, FileAccess.Read);

Wwise.Read(fs);