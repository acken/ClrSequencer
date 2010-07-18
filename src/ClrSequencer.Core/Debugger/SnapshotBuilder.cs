using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.Debugging.CorDebug;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace ClrSequencer.Core.Debugger
{
    class SnapshotBuilder
    {
        private CorThread _thread;
        private List<KeyValuePair<string, ISymbolReader>> _symbolReaders;

        public SnapshotBuilder(CorThread thread, List<KeyValuePair<string, ISymbolReader>> symbolReaders)
        {
            _thread = thread;
            _symbolReaders = symbolReaders;
        }

        public Snapshot Build()
        {
            var reader = getSymbolReaderFromActiveModule();
            return get_location(reader);
        }

        private ISymbolReader getSymbolReaderFromActiveModule()
        {
            return _symbolReaders.Find(
                r => r.Key.Equals(_thread.ActiveFrame.Function.Module.Name)).Value;
        }

        private Snapshot get_location(ISymbolReader reader)
        {
            var frame = _thread.ActiveFrame;
            var function = frame.Function;

            uint offset;
            CorDebugMappingResult mapping_result;
            frame.GetIP(out offset, out mapping_result);
            SequencePoint real_sp = null;
            try
            {
                var method = reader.GetMethod(new SymbolToken(frame.Function.Token));


                foreach (var sp in new SequencePointFactory().Generate(method))
                {
                    if (sp.Offset > offset)
                        break;
                    if (sp.LineStart != 0xfeefee)
                        real_sp = sp;
                }
            }
            catch (Exception)
            {
                // If we cant fint symbol method ignore
            }

            if (real_sp == null)
                return null; // string.Format("Location (offset {0})", offset);

            return new Snapshot(new Position(real_sp.Document.URL, real_sp.Offset, real_sp.LineStart, real_sp.LineStartColumn, real_sp.LineEnd, real_sp.LineEndColumn));
        }

        private SequencePoint get_location(CorFrame frame, out uint offset)
        {
            CorDebugMappingResult mapping_result;
            frame.GetIP(out offset, out mapping_result);

            if (frame.FrameType != CorFrameType.ILFrame)
                return null;
            return get_location(frame.Function, offset);
        }

        private SequencePoint get_location(CorFunction function, uint offset)
        {
            var reader = _symbolReaders.Find(r => r.Key.Equals(function.Module.Name)).Value;
            var symmethod = reader.GetMethod(new SymbolToken(function.Token));

            if (symmethod == null)
                return null;

            SequencePoint prev_sp = null;
            foreach (var sp in new SequencePointFactory().Generate(symmethod))
            {
                if (sp.Offset > offset)
                    break;
                prev_sp = sp;
            }
            return prev_sp;
        }
    }
}
