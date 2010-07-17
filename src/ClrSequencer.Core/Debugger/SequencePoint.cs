using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.SymbolStore;

namespace ClrSequencer.Core.Debugger
{
    class SequencePoint
    {
        public int Offset { get; private set; }
        public ISymbolDocument Document { get; private set; }
        public int LineStart { get; private set; }
        public int LineStartColumn { get; private set; }
        public int LineEnd { get; private set; }
        public int LineEndColumn { get; private set; }

        public SequencePoint(int offset, ISymbolDocument document, int lineStart, int lineStartColumn, int lineEnd, int lineEndColumn)
        {
            Offset = offset;
            Document = document;
            LineStart = lineStart;
            LineStartColumn = lineStartColumn;
            LineEnd = lineEnd;
            LineEndColumn = lineEndColumn;
        }
    }
}
