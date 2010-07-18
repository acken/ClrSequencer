using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClrSequencer.Core.Debugger
{
    public class Snapshot
    {
        public Position Position { get; private set; }

        public Snapshot(Position position)
        {
            Position = position;
        }
    }
}
