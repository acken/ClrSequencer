using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Samples.Debugging.CorDebug;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using System.IO;
using Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorSymbolStore;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ClrSequencer.Core.Debugger
{
    public class ClrProcess
    {
        private const int HIDDEN_LINE = 0xfeefee;

        private SequencePointFactory _sequencePointFactory = new SequencePointFactory();

        private string _assembly;
        private Breakpoint _breakpoint;
        private AutoResetEvent _terminateEvent;
        private AutoResetEvent _breakEvent;
        private CorProcess _process;
        private CorThread _activeThread;
        private List<KeyValuePair<string, ISymbolReader>> _symbolReaders = new List<KeyValuePair<string, ISymbolReader>>();

        private List<Snapshot> _sequence = new List<Snapshot>();

        public Snapshot[] Sequence { get { return _sequence.ToArray(); } }

        public void Start(string assembly, string arguments, Breakpoint breakpoint)
        {
            _assembly = assembly;
            _breakpoint = breakpoint;
            prepareDebugger(arguments);
            debug();
        }

        private void debug()
        {
            var events = prepareResetEvents();

            while (true)
            {
                _process.Continue(false);
                int resetEvent = waitForDebugger(events);
                if (isTerminateEvent(resetEvent))
                    break;
                takeSnapshot();
                step();
            }
        }

        private void takeSnapshot()
        {
            var builder = new SnapshotBuilder(_activeThread, _symbolReaders);
            var snapshot = builder.Build();
            if (snapshot != null)
                _sequence.Add(snapshot);
        }

        private bool isTerminateEvent(int resetEvent)
        {
            return resetEvent == 0;
        }

        private int waitForDebugger(WaitHandle[] handles)
        {
            return WaitHandle.WaitAny(handles);
        }

        private WaitHandle[] prepareResetEvents()
        {
            _terminateEvent = new AutoResetEvent(false);
            _breakEvent = new AutoResetEvent(false);
            WaitHandle[] handles = new WaitHandle[2];
            handles[0] = _terminateEvent;
            handles[1] = _breakEvent;
            return handles;
        }

        private void prepareDebugger(string arguments)
        {
            var debugger = new CorDebugger(CorDebugger.GetDefaultDebuggerVersion());
            _process = debugger.CreateProcess(_assembly, arguments);

            _process.OnCreateAppDomain += new CorAppDomainEventHandler(process_OnCreateAppDomain);
            _process.OnProcessExit += new CorProcessEventHandler(process_OnProcessExit);
            _process.OnBreakpoint += new BreakpointEventHandler(process_OnBreakpoint);
            _process.OnStepComplete += new StepCompleteEventHandler(_process_OnStepComplete);
            _process.OnModuleLoad += new CorModuleEventHandler(_process_OnModuleLoad);
            _process.OnDebuggerError += new DebuggerErrorEventHandler(_process_OnDebuggerError);
        }

        private void step()
        {
            var stepper = new Stepper(_symbolReaders);
            stepper.Step(_activeThread);
        }

        void _process_OnDebuggerError(object sender, CorDebuggerErrorEventArgs e)
        {
            Marshal.ThrowExceptionForHR(e.HResult);
        }

        void _process_OnModuleLoad(object sender, CorModuleEventArgs e)
        {
            var module = e.Module;
            var reader = loadSymbolReader(module);
            if (module.Name.Equals(_breakpoint.Assembly))
                setBreakpoint(module, reader);
        }

        private ISymbolReader loadSymbolReader(CorModule module)
        {
            var loader = new SymbolLoader();
            var reader = loader.GetSymbolReader(module);
            if (reader != null)
                _symbolReaders.Add(new KeyValuePair<string, ISymbolReader>(module.Name, reader));
            return reader;
        }

        private void setBreakpoint(CorModule module, ISymbolReader reader)
        {
            var setter = new BreakpointSetter(_breakpoint);
            setter.Set(module, reader);
        }

        void _process_OnStepComplete(object sender, CorStepCompleteEventArgs e)
        {
            e.Continue = false;
            _activeThread = e.Thread;
            _breakEvent.Set();
        }

        void process_OnBreakpoint(object sender, CorBreakpointEventArgs e)
        {
            e.Continue = false;
            _activeThread = e.Thread;
            _breakEvent.Set();
        }

        void process_OnCreateAppDomain(object sender, CorAppDomainEventArgs e)
        {
            e.AppDomain.Attach();
        }

        void process_OnProcessExit(object sender, CorProcessEventArgs e)
        {
            _terminateEvent.Set();
        }
    }
}
