using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ClrSequencer.Core.Debugger;
using System.IO;

namespace ClrSequencer.Core.Test.Debugger
{
    [TestFixture]
    public class ClrProcessTest
    {
        [Test]
        public void ShouldCreateSequence()
        {
            var assembly = Path.GetFullPath("SomeSimpleConsoleApp.exe");
            var arguments = "";
            var file = @"C:\Users\sveina\src\DotNET\Private\SomeSimpleConsoleApp\SomeClass.cs";
            var breakPoint = new Breakpoint(assembly, file, 11, 0);

            var process = new ClrProcess();
            process.Start(assembly, arguments, breakPoint);

            Assert.AreEqual(14, process.Sequence.Length);
        }
    }
}
