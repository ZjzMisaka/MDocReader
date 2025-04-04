using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDocReader
{
    internal class History
    {
        public History(string path, string fragment)
        {
            Path = path;
            Fragment = fragment;
        }

        internal string Path { get; set; }
        internal string Fragment { get; set; }
    }
}
