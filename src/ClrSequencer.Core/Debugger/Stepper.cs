using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.Debugging.CorDebug;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace ClrSequencer.Core.Debugger
{
    class Stepper
    {
        private List<KeyValuePair<string, ISymbolReader>> _symbolReaders;
        private CorThread _activeThread;

        public Stepper(List<KeyValuePair<string, ISymbolReader>> symbolReaders)
        {
            _symbolReaders = symbolReaders;
        }

        public void Step(CorThread thread)
        {
            _activeThread = thread;
            var stepper = createStepper();
            var mod = _activeThread.ActiveFrame.Function.Module;
            if (hasSymbolReader(mod))
                stepIn(mod, stepper);
            else
                stepOut(stepper);
        }

        private bool hasSymbolReader(CorModule mod)
        {
            return _symbolReaders.Exists(r => r.Key.Equals(mod.Name));
        }

        private void stepIn(CorModule mod, CorStepper stepper)
        {
            var range = tryGetStepRanges(_activeThread, _symbolReaders.Find(r => r.Key.Equals(mod.Name)).Value);
            stepper.StepRange(true, range);
        }

        private void stepOut(CorStepper stepper)
        {
            stepper.Step(false);
        }

        private CorStepper createStepper()
        {
            var stepper = _activeThread.ActiveFrame.CreateStepper();
            stepper.SetUnmappedStopMask(CorDebugUnmappedStop.STOP_NONE);
            return stepper;
        }


        private COR_DEBUG_STEP_RANGE[] createStepRange(uint start, int end)
        {
            var range = new COR_DEBUG_STEP_RANGE[1];
            range[0] = new COR_DEBUG_STEP_RANGE()
                           {
                               startOffset = (UInt32) start,
                               endOffset = (UInt32) end
                           };
            return range;
        }

        private COR_DEBUG_STEP_RANGE[] tryGetStepRanges(CorThread thread, ISymbolReader reader)
        {
            var frame = thread.ActiveFrame;
            uint offset;
            CorDebugMappingResult mapResult;
            frame.GetIP(out offset, out mapResult);
            try
            {
                COR_DEBUG_STEP_RANGE[] range;
                if ((range = getStepRanges(frame, reader, offset)) != null)
                return range;
            }
            catch (Exception)
            {
                // If we can't get the symbol method do step thorugh ilcode
            }
            return createStepRange(offset, frame.Function.ILCode.Size);
        }

        private COR_DEBUG_STEP_RANGE[] getStepRanges(CorFrame frame, ISymbolReader reader, uint offset)
        {
            var method = reader.GetMethod(new SymbolToken(frame.FunctionToken));
            foreach (var sp in new SequencePointFactory().Generate(method))
            {
                if (sp.Offset > offset)
                    return createStepRange(offset, sp.Offset);
            }
            return null;
        }
    }
}
