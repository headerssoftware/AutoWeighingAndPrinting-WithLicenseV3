using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoWeighingAndPrinting.Helpers
{
    public static class PrnHelper
    {

        public static string PrnRootPath =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "PRN FILE"
            );

        public static string[] GetSizeFolders()
        {
            if (!Directory.Exists(PrnRootPath))
                return new string[0];

            return Directory.GetDirectories(PrnRootPath)
                    .Where(dir => !string.Equals(
                        Path.GetFileName(dir),
                        "TOTAL WEIGHT",
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
        }
    }

}
