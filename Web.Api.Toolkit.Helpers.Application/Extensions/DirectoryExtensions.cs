using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extensions
{
    public static class DirectoryExtensions
    {
        public static long GetDirectorySize(this DirectoryInfo directory)
        {
            if (!directory.Exists)
                return 0;

            return directory
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
    }
}
