using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClrSequencer.Core.Debugger
{
    public class Position
    {
        public int Offset { get; private set; }
        public string File { get; private set; }
        public int LineStart { get; private set; }
        public int LineStartColumn { get; private set; }
        public int LineEnd { get; private set; }
        public int LineEndColumn { get; private set; }

        public Position(string file, int offset, int lineStart, int lineStartColumn, int lineEnd, int lineEndColumn)
        {
            File = file;
            Offset = offset;
            LineStart = lineStart;
            LineStartColumn = lineStartColumn;
            LineEnd = lineEnd;
            LineEndColumn = lineEndColumn;
        }
    }
}
