using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DebugMe
{
    class Program
    {
        static void Main(string[] args)
        {
            var someClass = new SomeClass(33);
            someClass.ReturnString();
        }
    }
}
