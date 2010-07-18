﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClrSequencer.Core.Debugger;
using System.IO;

namespace ClrSequencer.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = new ClrProcess();
            process.Start(Path.GetFullPath("SomeSimpleConsoleApp.exe"), "", new Breakpoint(Path.GetFullPath("SomeSimpleConsoleApp.exe"), @"C:\Users\sveina\src\DotNET\Private\SomeSimpleConsoleApp\SomeClass.cs", 11, 0));
            foreach (var snapshot in process.Sequence)
            {
                System.Console.WriteLine("{0}:{1}", snapshot.Position.File, snapshot.Position.LineStart);
            }
            System.Console.ReadLine();
        }
    }
}