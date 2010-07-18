using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug;
using System.Runtime.InteropServices;
using Microsoft.Samples.Debugging.CorSymbolStore;
using System.IO;
using Microsoft.Samples.Debugging.CorMetadata;
using System.Reflection;

namespace ClrSequencer.Core.Debugger
{
    class SymbolLoader
    {
        public ISymbolReader GetSymbolReader(CorModule module)
        {
            ISymbolReader reader = null;
            try
            {
                if (!module.IsDynamic && !module.IsInMemory)
                    reader = getSymbolReader(module);
            }
            catch
            {
            }
            return reader;
        }

        private ISymbolReader getSymbolReader(CorModule module)
        {
            ISymbolReader reader = null;
            string sympath = Path.GetDirectoryName(module.Name);
            string moduleName = module.Name;
            try
            {
                SymbolBinder binder = new SymbolBinder();
                var importer = new CorMetadataImport(module);
                reader = binder.GetReaderForFile(importer.RawCOMObject, moduleName, sympath);
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode == unchecked((int)0x806D0014))  // E_PDB_CORRUPT
                {
                    // Ignore it.
                    // This may happen for mismatched pdbs
                }
                else
                    throw;
            }
            return reader;
        }
    }
}
