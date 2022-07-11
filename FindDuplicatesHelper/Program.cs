using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FindDuplicatesHelper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int cnt = 0;
            Assembly[] assemblies = new Assembly[args.Count()-1];
            foreach (var package in args.SkipLast(1))
            {
                assemblies[cnt++] = Assembly.Load(package);
            }
            // Analyze for duplicate names
            var duplicates = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                .Select(type => new Usage(type))
                .Concat(assemblies.SelectMany(Usage.GetNamespaces))
                .GroupBy(usage => usage.Name)
                .Where(grouping => grouping.Count() > 1 && !grouping.All(usage => usage.IsNamespace))
                .Select(grouping => new Conflict(name: grouping.Key, usages: grouping))
                .OrderByDescending(conflict => conflict.Libraries.Count)
                .ToList();
            // Analyze the conflicts across planes (ignoring Synapse)
            var boundaryConflicts = duplicates.Where(p => p.Libraries.Any(l => l.IsManagement) && p.Libraries.Any(l => !l.IsManagement && !l.FullName.StartsWith("Azure.Analytics.Synapse.Artifacts"))).ToList();
            // Analyze the conflicts with namespace
            var nsConflicts = duplicates.Where(p => p.Libraries.Any(l => l.IsNamespace)).ToList();
            // Report the results
            bool save = true;
            using (TextWriter writer = save ? File.CreateText("Duplicates.txt") : Console.Out!)
            {
                Conflict.Report(writer, duplicates, args.Last(), "Duplicates");
            }
            using (TextWriter writer = save ? File.CreateText("Conflicts.txt") : Console.Out!)
            {
                Conflict.Report(writer, boundaryConflicts, args.Last(), "Conflicts across planes");
            }
            using (TextWriter writer = save ? File.CreateText("NamespaceConflicts.txt") : Console.Out!)
            {
                Conflict.Report(writer, nsConflicts, args.Last(), "Conflicts with namespaces");
            }
        }
    }
}
