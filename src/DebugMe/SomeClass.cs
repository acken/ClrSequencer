using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DebugMe
{
    class SomeClass
    {
        public string ReturnString()
        {
            var a = 15;
            var b = 20;
            var multiplier = new SomeMultiplier();
            var sum = multiplier.Multiply(a, b);
            return string.Format("15 *20 is {0}", sum);
        }
    }
}
