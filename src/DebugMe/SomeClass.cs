using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DebugMe
{
    class SomeClass
    {
        private int _numberToAdd;

        public SomeClass(int number)
        {
            _numberToAdd = number;
        }

        public string ReturnString()
        {
            var a = 15;
            var b = 20;
            var multiplier = new SomeMultiplier();
            var sum = multiplier.Multiply(a, b) + _numberToAdd;
            return string.Format("(15 * 20) + {1} is {0}", sum, _numberToAdd);
        }
    }
}
