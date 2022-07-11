using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FindDuplicatesHelper
{
    internal class Conflict
    {
        public string Name { get; }
        public IList<Usage> Libraries { get; }
        public Conflict(string name, IEnumerable<Usage> usages)
        {
            Name = name;
            Libraries = usages.Distinct().OrderBy(x => (x.IsManagement ? "1" : "0") + (x.IsNamespace ? "0" : "1") + x.Library).ToList();
        }
        public static void Report(TextWriter writer, IEnumerable<Conflict> conflicts, string targetPackage, string category = "Conflicts")
        {
            if (targetPackage != "NoTargetPackage")
            {
                conflicts = conflicts.Where(c => c.Libraries.Any(lib => lib.Library.Equals(targetPackage)));
            }
            writer.WriteLine($"{conflicts.Count()} {category}!\n");
            foreach (var conflict in conflicts)
            {
                writer.WriteLine($"{conflict.Name} ({conflict.Libraries.Count}):");
                foreach (Usage usage in conflict.Libraries)
                {
                    string type = usage.IsNamespace ? "namespace" : "type";
                    writer.WriteLine($"    {usage.Library} ({type} {usage.FullName})");
                }
                writer.WriteLine();
            }
        }
    }
}
