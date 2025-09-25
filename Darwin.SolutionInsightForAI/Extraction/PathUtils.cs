using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Tools.Extraction
{
    public static class PathUtils
    {
        public static string ToForwardSlashes(string path)
            => string.IsNullOrWhiteSpace(path) ? path : path.Replace('\\', '/');
    }
}
