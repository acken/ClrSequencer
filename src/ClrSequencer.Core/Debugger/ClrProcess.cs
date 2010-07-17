using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Samples.Debugging.CorDebug;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using System.IO;

namespace ClrSequencer.Core.Debugger
{
    class ClrProcess
    {
        private const int HIDDEN_LINE = 0xfeefee;

        private string _assembly;
        private Breakpoint _breakpoint;
        private AutoResetEvent _terminateEvent;
        private AutoResetEvent _breakEvent;
        private CorProcess _process;
        private CorThread _activeThread;
        private List<KeyValuePair<string, ISymbolReader>> _symbolReaders = new List<KeyValuePair<string, ISymbolReader>>();

        public void Start(string assembly, string arguments, Breakpoint breakpoint)
        {
            _assembly = assembly;
            _breakpoint = breakpoint;
            createDebugger(arguments);
            debug();
        }

        private void _process_OnCreateAppDomain(object sender, CorAppDomainEventArgs e)
        {
            e.AppDomain.Attach();
        }

        void _process_OnModuleLoad(object sender, CorModuleEventArgs e)
        {
            initializeReaderAndSetBreakpoint(e);
        }

        private void _process_OnProcessExit(object sender, CorProcessEventArgs e)
        {
            _terminateEvent.Set();
        }

        private void _process_OnBreakpoint(object sender, CorBreakpointEventArgs e)
        {
            e.Continue = false;
            _activeThread = e.Thread;
            _breakEvent.Set();
        }

        private void _process_OnStepComplete(object sender, CorStepCompleteEventArgs e)
        {
            e.Continue = false;
            _activeThread = e.Thread;
            _breakEvent.Set();
        }

        private void _process_OnDebuggerError(object sender, CorDebuggerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void createDebugger(string arguments)
        {
            var debugger = new CorDebugger(CorDebugger.GetDefaultDebuggerVersion());
            _process = debugger.CreateProcess(_assembly, arguments);
            _process.OnCreateAppDomain += new CorAppDomainEventHandler(_process_OnCreateAppDomain);
            _process.OnModuleLoad += new CorModuleEventHandler(_process_OnModuleLoad);
            _process.OnProcessExit += new CorProcessEventHandler(_process_OnProcessExit);
            _process.OnBreakpoint += new BreakpointEventHandler(_process_OnBreakpoint);
            _process.OnStepComplete += new StepCompleteEventHandler(_process_OnStepComplete);
            _process.OnDebuggerError += new DebuggerErrorEventHandler(_process_OnDebuggerError);
        }

        private void debug()
        {
            var handles = wireUpResetEvents();
            while (true)
            {
                int handle = wait(handles);
                if (isTerminateEvent(handle))
                    break;
                stepIn();
            }
        }

        private WaitHandle[] wireUpResetEvents()
        {
            _terminateEvent = new AutoResetEvent(false);
            _breakEvent = new AutoResetEvent(false);
            var handles = new WaitHandle[2];
            handles[0] = _terminateEvent;
            handles[1] = _breakEvent;
            return handles;
        }

        private int wait(WaitHandle[] handles)
        {
            _process.Continue(false);
            return WaitHandle.WaitAny(handles);
        }

        private bool isTerminateEvent(int handle)
        {
            return handle == 0;
        }

        private void stepIn()
        {
            var stepper = createStepper(_activeThread);
            var mod = _activeThread.ActiveFrame.Function.Module;
            if (!_symbolReaders.Exists(r => r.Key.Equals(mod.Name)))
                stepper.Step(true);
            else
                stepInThroughIL(mod, stepper);
        }

        private void stepInThroughIL(CorModule mod, CorStepper stepper)
        {
            var range = getStepRanges(_activeThread, _symbolReaders.Find(r => r.Key.Equals(mod.Name)).Value);
            stepper.StepRange(true, range);
        }

        private static CorStepper createStepper(CorThread thread)
        {
            var stepper = thread.ActiveFrame.CreateStepper();
            stepper.SetUnmappedStopMask(CorDebugUnmappedStop.STOP_NONE);
            return stepper;
        }

        private static COR_DEBUG_STEP_RANGE[] getStepRanges(CorThread thread, ISymbolReader reader)
        {
            var frame = thread.ActiveFrame;
            uint offset;
            CorDebugMappingResult mapResult;
            frame.GetIP(out offset, out mapResult);
            var sequencePoint = getSequencePoint(frame, reader, offset);
            if (sequencePoint != null)
                return createStepRange(offset, sequencePoint.Offset);
            return createStepRange(offset, frame.Function.ILCode.Size);
        }

        private static SequencePoint getSequencePoint(CorFrame frame, ISymbolReader reader, uint offset)
        {
            SequencePoint sequencePoint = null;
            try
            {
                var method = reader.GetMethod(new SymbolToken(frame.FunctionToken));
                sequencePoint = getSequencePoint(method, offset);
            }
            catch (Exception)
            {
                // If we can't get the symbol method do step thorugh ilcode
            }
            return sequencePoint;
        }

        private static SequencePoint getSequencePoint(ISymbolMethod method, uint offset)
        {
            foreach (var sp in getSequencePoints(method))
            {
                if (sp.Offset > offset)
                    return sp;
            }
            return null;
        }

        private static COR_DEBUG_STEP_RANGE[] createStepRange(uint start, int end)
        {
            var range = new COR_DEBUG_STEP_RANGE[1];
            range[0] = new COR_DEBUG_STEP_RANGE()
            {
                startOffset = (UInt32)start,
                endOffset = (UInt32)end
            };
            return range;
        }

        private static SequencePoint[] getSequencePoints(ISymbolMethod method)
        {
            var sequencePoints = new List<SequencePoint>();
            var sp_count = method.SequencePointCount;
            var spOffsets = new int[sp_count];
            var spDocs = new ISymbolDocument[sp_count];
            var spStartLines = new int[sp_count];
            var spEndLines = new int[sp_count];
            var spStartCol = new int[sp_count];
            var spEndCol = new int[sp_count];

            method.GetSequencePoints(spOffsets, spDocs, spStartLines, spStartCol, spEndLines, spEndCol);

            for (int i = 0; i < sp_count; i++)
            {
                if (spStartLines[i] != HIDDEN_LINE)
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

        private void initializeReaderAndSetBreakpoint(CorModuleEventArgs e)
        {
            var module = e.Module;
            var reader = getSymbolReader(module);
            if (reader == null)
                return;

            if (e.Module.Name.Equals(_breakpoint.Assembly))
            {

                setBreakpoint(module, reader);
            }

            var func = e.Module.GetFunctionFromToken(1);
            var br = func.CreateBreakpoint();
            br.Activate(true);
        }

        private void setBreakpoint(CorModule module, ISymbolReader reader)
        {
            foreach (var doc in reader.GetDocuments())
            {
                if (Path.GetFileName(doc.URL).Equals(Path.GetFileName(_breakpoint.File)))
                    createBreakpoint(reader, doc, module);
            }
        }

        private void createBreakpoint(ISymbolReader reader, ISymbolDocument doc, CorModule module)
        {
            int line = _breakpoint.Line;
            line = doc.FindClosestLine(line);
            var method = reader.GetMethodFromDocumentPosition(doc, line, _breakpoint.Column);
            var function = module.GetFunctionFromToken(method.Token.GetToken());
            if (setInIL(doc, line, method, function))
                return;

            var bp = function.CreateBreakpoint();
            bp.Activate(true);
        }

        private bool setInIL(ISymbolDocument doc, int line, ISymbolMethod method, CorFunction function)
        {
            bool found = false;
            foreach (var sp in getSequencePoints(method))
            {
                if (sp.Document.URL.Equals(doc.URL) && sp.LineStart.Equals(line))
                {
                    var bp = function.ILCode.CreateBreakpoint(sp.Offset);
                    bp.Activate(true);
                    found = true;
                }
            }
            return found;
        }

        private ISymbolReader getSymbolReader(CorModule module)
        {
            ISymbolReader reader = null;
            try
            {
                if (!module.IsDynamic && !module.IsInMemory)
                {
                    reader = getSymbolReader(module);
                    _symbolReaders.Add(new KeyValuePair<string, ISymbolReader>(module.Name, reader));
                }
            }
            catch (Exception)
            {
                reader = null;
            }
            return reader;
        }
    }
}
