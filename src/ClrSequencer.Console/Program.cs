using System;
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
            var assembly = Path.GetFullPath("DebugMe.exe");
            process.Start(assembly, "", new Breakpoint(assembly, null, 0, 0));
            foreach (var snapshot in process.Sequence)
            {
                System.Console.WriteLine("{0}:{1}", snapshot.Position.File, snapshot.Position.LineStart);
                var locals = "\t";
                foreach (var parameter in snapshot.Parameters)
                    locals += string.Format("{0}={1}, ", parameter.Name, parameter.Value);
                foreach (var variable in snapshot.Variables)
                    locals += string.Format("{0}={1}, ", variable.Name, variable.Value);
                System.Console.WriteLine(locals);
            }
            System.Console.ReadLine();
        }
    }
}
