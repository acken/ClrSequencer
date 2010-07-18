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
            var assembly = Path.GetFullPath("DebugMe.exe");
            var arguments = "";
            var breakPoint = new Breakpoint(assembly, null, 0, 0);

            var process = new ClrProcess();
            process.Start(assembly, arguments, breakPoint);

            Assert.AreEqual(23, process.Sequence.Length);
        }
    }
}
