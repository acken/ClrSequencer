using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClrSequencer.Core.Debugger
{
    public class Variable
    {
        public string Name { get; private set; }
        public object Value { get; private set; }

        public Variable(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
