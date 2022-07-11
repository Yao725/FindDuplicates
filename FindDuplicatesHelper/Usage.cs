using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FindDuplicatesHelper
{
    internal class Usage
    {
        public string Library { get; }
        public string Name { get; }
        public string FullName { get; }
        public bool IsManagement { get; } = false;
        public bool IsNamespace { get; } = false;
        private Usage(Assembly library, string name, string fullName, bool isNs)
        {
            Library = library.GetName().Name!;
            Name = name;
            FullName = fullName;
            IsManagement = Library.StartsWith("Azure.ResourceManager");
            IsNamespace = isNs;
        }
        public Usage(Type type) :
            this(
                type.Assembly,
                type.IsNested ? type.DeclaringType!.Name + "." + type.Name :
                    type.Name,
                type.FullName!,
                isNs: false)
        {
        }
        public static IEnumerable<Usage> GetNamespaces(Assembly assembly)
        {
            HashSet<string> used = new HashSet<string>();
            foreach (string ns in assembly.GetExportedTypes()
                .Select(type => type.Namespace)
                .Distinct()
                .OrderBy(ns => ns?.Length ?? 0))
                {
                    if (ns == null) { continue; }
                    foreach (string component in ns.Split('.'))
                    {
                        if (used.Contains(component)) { continue; }
                        used.Add(component);
                        yield return new Usage(assembly, component, ns, isNs: true);
                    }
                }
        }
    }
}
