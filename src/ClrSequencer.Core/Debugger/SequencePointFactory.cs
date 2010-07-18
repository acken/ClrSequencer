using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.SymbolStore;

namespace ClrSequencer.Core.Debugger
{
    class SequencePointFactory
    {
        public SequencePoint[] Generate(ISymbolMethod method)
        {
            var sequencePoints = new List<SequencePoint>();
            var sp_count = method.SequencePointCount;
            var spOffsets = new int[sp_count]; // Array.CreateInstance(int, sp_count)
            var spDocs = new ISymbolDocument[sp_count]; // Array.CreateInstance(ISymbolDocument, sp_count)
            var spStartLines = new int[sp_count]; // Array.CreateInstance(int, sp_count)
            var spEndLines = new int[sp_count]; // Array.CreateInstance(int, sp_count)
            var spStartCol = new int[sp_count]; // Array.CreateInstance(int, sp_count)
            var spEndCol = new int[sp_count]; // Array.CreateInstance(int, sp_count)

            method.GetSequencePoints(spOffsets, spDocs, spStartLines, spStartCol, spEndLines, spEndCol);

            for (int i = 0; i < sp_count; i++) // (var i in range(sp_count))
            {
                if (spStartLines[i] != 0xfeefee) // if spStartLines[i] != 0xfeefee:
                {
                    sequencePoints.Add(new SequencePoint(spOffsets[i],
                                                         spDocs[i],
                                                         spStartLines[i],
                                                         spStartCol[i],
                                                         spEndLines[i],
                                                         spEndCol[i]));
                }
            }
            return sequencePoints.ToArray();
        }
    }
}
