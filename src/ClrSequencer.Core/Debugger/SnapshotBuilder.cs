using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.Debugging.CorDebug;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;

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
            if (reader == null)
                return null;
            var snapshot = get_location(reader);
            if (snapshot == null)
                return null;

            snapshot.SetParameters(getParameters());

            snapshot.SetVariables(getVariables(reader));

            return snapshot;
        }

        private Variable[] getVariables(ISymbolReader reader)
        {
            var variables = new List<Variable>();
            try
            {
                var method = reader.GetMethod(new SymbolToken(_thread.ActiveFrame.Function.Token));
                var scope = method.RootScope;
                variables.AddRange(enumerateLocals(scope));
            }
            catch
            {
            }
            return variables.ToArray();
        }

        private Parameter[] getParameters()
        {
            var parameters = new List<Parameter>();
            var metadata_import = new CorMetadataImport(_thread.ActiveFrame.Function.Module);
            var method_info = metadata_import.GetMethodInfo(_thread.ActiveFrame.FunctionToken);
            foreach (var parameter in method_info.GetParameters())
            {
                var argument = _thread.ActiveFrame.GetArgument(parameter.Position - 1);
                parameters.Add(new Parameter(parameter.Name, getValue(argument)));
            }
            return parameters.ToArray();
        }

        private Variable[] enumerateLocals(ISymbolScope scope)
        {
            var variables = new List<Variable>();
            foreach (var local in scope.GetLocals())
            {
                if (isHidden(local))
                    continue;
                var value = _thread.ActiveFrame.GetLocalVariable(local.AddressField1);
                var something = getValue(value);
                variables.Add(new Variable(local.Name, something));
            }

            foreach (var child in scope.GetChildren())
                variables.AddRange(enumerateLocals(child));
            return variables.ToArray();
        }

        private CorElementType _typename;

        private object getValue(CorValue value)
        {
            var rv = value.CastToReferenceValue();
            if (rv != null)
            {
                if (rv.IsNull)
                {
                    _typename = rv.ExactType.Type;
                    return null;
                }
                return getValue(rv.Dereference());
            }
            var bv = value.CastToBoxValue();
            if (bv != null)
                return getValue(bv.GetObject());

/*_type_map = { 'System.Boolean': ELEMENT_TYPE_BOOLEAN,    
  'System.SByte'  : ELEMENT_TYPE_I1, 'System.Byte'   : ELEMENT_TYPE_U1,    
  'System.Int16'  : ELEMENT_TYPE_I2, 'System.UInt16' : ELEMENT_TYPE_U2,    
  'System.Int32'  : ELEMENT_TYPE_I4, 'System.UInt32' : ELEMENT_TYPE_U4,    
  'System.IntPtr' : ELEMENT_TYPE_I,  'System.UIntPtr': ELEMENT_TYPE_U,   
  'System.Int64'  : ELEMENT_TYPE_I8, 'System.UInt64' : ELEMENT_TYPE_U8,    
  'System.Single' : ELEMENT_TYPE_R4, 'System.Double' : ELEMENT_TYPE_R8,    
  'System.Char'   : ELEMENT_TYPE_CHAR, }*/

            var typeMap = new List<KeyValuePair<CorElementType, string>>();
            typeMap.AddRange(new KeyValuePair<CorElementType, string>[]
                                 {
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_BOOLEAN, "System.Boolean"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_I1, "System.SByte"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_U1, "System.Byte"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_I2, "System.Int16"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_U2, "System.UInt16"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_I4, "System.Int32"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_U4, "System.UInt32"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_I, "System.IntPtr"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_U, "System.UIntPtr"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_I8, "System.Int64"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_U8, "System.UInt64"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_R4, "System.Single"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_R8, "System.Double"),
                                     new KeyValuePair<CorElementType, string>(CorElementType.ELEMENT_TYPE_CHAR, "System.Char")
                                 });

            if (typeMap.Exists(t => t.Key.Equals(value.Type)))
                return value.CastToGenericValue().GetValue();
            else if (value.Type == CorElementType.ELEMENT_TYPE_STRING)
                return value.CastToStringValue().String;
            else if (value.Type == CorElementType.ELEMENT_TYPE_VALUETYPE)
            {
                var typeValue = value.ExactType.Type;
                if (typeMap.Exists(t => t.Value.Equals(_typename)))
                {
                    var gv = value.CastToGenericValue();
                    return gv.UnsafeGetValueAsType(typeMap.Find(t => t.Value.Equals(_typename)).Key);
                }
                else
                    return value.CastToObjectValue();
            }
            else if (new CorElementType[] {CorElementType.ELEMENT_TYPE_CLASS, CorElementType.ELEMENT_TYPE_OBJECT}.Contains(value.Type))
                return new object(); //value.CastToObjectValue();
            else
                return "Unknown";
        }

        private bool isHidden(ISymbolVariable local)
        {
            return local.Attributes.Equals(1);
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
