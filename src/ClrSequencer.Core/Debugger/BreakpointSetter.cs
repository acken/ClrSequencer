using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.Debugging.CorDebug;
using System.Diagnostics.SymbolStore;

namespace ClrSequencer.Core.Debugger
{
    class BreakpointSetter
    {
        private Breakpoint _breakpoint;

        public BreakpointSetter(Breakpoint breakpoint)
        {
            _breakpoint = breakpoint;
        }

        public void Set(CorModule module, ISymbolReader reader)
        {
            foreach (var doc in reader.GetDocuments())
            {
                if (attemptSetBreakpoint(reader, doc, module))
                    return;
            }
            setBreakpointThroughModule(module);
        }

        private void setBreakpointThroughModule(CorModule module)
        {
            var func = module.GetFunctionFromToken(1);
            var br = func.CreateBreakpoint();
            br.Activate(true);
        }

        private bool attemptSetBreakpoint(ISymbolReader reader, ISymbolDocument doc, CorModule module)
        {
            if (!doc.URL.Equals(_breakpoint.File))
                return false;

            var line = doc.FindClosestLine(_breakpoint.Line);
            var method = reader.GetMethodFromDocumentPosition(doc, line, _breakpoint.Column);
            var function = module.GetFunctionFromToken(method.Token.GetToken());

            var wasSet = attemptToSetBreakpointThroughSequencePoints(doc, line, method, function);
            if (!wasSet)
                setBreakpointThroughFunction(function);
            return true;
        }

        private void setBreakpointThroughFunction(CorFunction function)
        {
            var bp = function.CreateBreakpoint();
            bp.Activate(true);
        }

        private bool attemptToSetBreakpointThroughSequencePoints(ISymbolDocument doc, int line, ISymbolMethod method, CorFunction function)
        {
            bool found = false;
            foreach (var sp in getSequencePoints(method))
            {
                if (sp.Document.URL.Equals(doc.URL) && sp.LineStart.Equals(line))
                {
                    var bp = function.ILCode.CreateBreakpoint(sp.Offset);
                    bp.Activate(true);
                    found = true;
                    break;
                }
            }
            return found;
        }

        private SequencePoint[] getSequencePoints(ISymbolMethod method)
        {
            var factory = new SequencePointFactory();
            return factory.Generate(method);
        }
    }
}
