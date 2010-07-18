using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClrSequencer.Core.Debugger
{
    public class Snapshot
    {
        private List<Parameter> _parameters;
        private List<Variable> _variables;

        public Position Position { get; private set; }
        public Parameter[] Parameters { get { return _parameters.ToArray(); } }
        public Variable[] Variables { get { return _variables.ToArray(); } }

        public Snapshot(Position position)
        {
            Position = position;
        }

        public void SetParameters(Parameter[] parameters)
        {
            _parameters = new List<Parameter>();
            _parameters.AddRange(parameters);
        }

        public void SetVariables(Variable[] variables)
        {
            _variables = new List<Variable>();
            _variables.AddRange(variables);
        }
    }
}
