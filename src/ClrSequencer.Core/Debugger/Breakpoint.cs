using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClrSequencer.Core.Debugger
{
    public class Breakpoint
    {
        public string Assembly { get; private set; }
        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public Breakpoint(string assembly, string file, int line, int column)
        {
            Assembly = assembly;
            File = file;
            Line = line;
            Column = column;
        }
    }
}
