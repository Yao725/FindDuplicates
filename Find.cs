using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FindDuplicates
{
    internal class Find
    {
        private static readonly string ProjectPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.ToString();

        private static readonly HashSet<string> ExtraPackages = new HashSet<string> { 
            "Microsoft.Azure.Cosmos",
            "Microsoft.Extensions.Azure",
            "Microsoft.Azure.Data.SchemaRegistry.ApacheAvro"
        };

        private static readonly HashSet<string> ExcludedPackages = new HashSet<string> {
            "Azure.Core.Experimental",
            "Azure.Core.TestFramework"
        };

        private static readonly HashSet<string> OnlyProjectReference = new HashSet<string> {
            "Azure.ResourceManager.Communication"
        };

        private string Target;

        private string Root;

        public Find(string root, string target)
        {
            Root = root;
            Target = target;
        }

        public void Exexute()
        {
            List<string> packageReferenceList = new List<string>(), projectReferenceList = new List<string>(), projectReferencePaths = new List<string>();
            AddReference(packageReferenceList, projectReferenceList, projectReferencePaths, Root);
            FindDuplicates(packageReferenceList.Concat(projectReferenceList).Append($"{Target ?? "NoTargetPackage"}"));
            RemoveReference(packageReferenceList, projectReferencePaths);
        }

        private static void AddReference(List<string> packages, List<string> projects, List<string> projectPaths, string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
                throw new ArgumentNullException(nameof(Root));
            foreach (var package in ExcludedPackages)
            {
                if (ExecuteDotnet(ProjectPath, "dotnet", $"add  FindDuplicatesHelper\\FindDuplicatesHelper.csproj package {package}"))
                {
                    packages.Add(package);
                }
            }
            foreach (var serviceDir in Directory.GetDirectories(Path.Combine(rootPath, "sdk")))
            {
                foreach (var solutionDir in Directory.GetDirectories(serviceDir))
                {
                    var dirName = Path.GetFileName(solutionDir);
                    if (dirName.StartsWith("Azure."))
                    {
                        if (ExcludedPackages.Contains(dirName))
                            continue;
                        if(!OnlyProjectReference.Contains(dirName) && ExecuteDotnet(ProjectPath, "dotnet", $"add  FindDuplicatesHelper\\FindDuplicatesHelper.csproj package {dirName}"))
                        {
                            packages.Add(dirName);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to add package reference for {dirName}. Will try to add the project reference.");
                            string projReferencePath = $"{solutionDir}\\src\\{dirName}.csproj";
                            if (!File.Exists(projReferencePath))
                            {
                                Console.WriteLine($"Can't find the project file for {dirName}, will skip this package");
                                continue;
                            }
                            if (ExecuteDotnet(ProjectPath, "dotnet", $"add FindDuplicatesHelper\\FindDuplicatesHelper.csproj reference {projReferencePath}"))
                            {
                                projects.Add(dirName); ;
                                projectPaths.Add(projReferencePath);
                            }
                            else
                            {
                                Console.WriteLine($"Failed to add project reference for {dirName}. Please double check.");
                            }
                        }
                    }
                }
            }
        }
        private static void FindDuplicates(IEnumerable<string> packages)
        {
            string directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName;
            string binDirectory = directory + "\\FindDuplicatesHelper\\bin";
            string objDirectory = directory + "\\FindDuplicatesHelper\\obj";
            if (Directory.Exists(binDirectory))
            {
                Console.WriteLine("Start deleting the bin directory...");
                Directory.Delete(binDirectory, true);
                Console.WriteLine("Finish deleting the bin directory");
            }
            if (Directory.Exists(objDirectory))
            {
                Console.WriteLine("Start deleting the obj directory...");
                Directory.Delete(objDirectory, true);
                Console.WriteLine("Finish deleting the obj directory");
            }
            Console.WriteLine("Start building the project...");
            ExecuteDotnet(ProjectPath, "dotnet", "build .\\FindDuplicatesHelper\\FindDuplicatesHelper.csproj");
            Console.WriteLine("Finish building the project");
            Console.WriteLine("Start finding duplicates...");
            ExecuteDotnet(ProjectPath, "dotnet", $"run --project .\\FindDuplicatesHelper\\FindDuplicatesHelper.csproj {string.Join(' ', packages)}");
            Console.WriteLine("Finish finding");
        }

        private static void RemoveReference(IEnumerable<string> packagesToRemove, IEnumerable<string> projectsToRemove)
        {
            foreach (var package in packagesToRemove)
            {

                if (!ExecuteDotnet(ProjectPath, "dotnet", $"remove FindDuplicatesHelper\\FindDuplicatesHelper.csproj package {package}"))
                {
                    throw new Exception($"The command \"dotnet remove package {package}\" failed.");
                }
            }
            foreach (var project in projectsToRemove)
            {

                if (!ExecuteDotnet(ProjectPath, "dotnet", $"remove FindDuplicatesHelper\\FindDuplicatesHelper.csproj reference {project}"))
                {
                    throw new Exception($"The command \"dotnet remove reference {project}\" failed.");
                }
            }
        }

        private static bool ExecuteDotnet(string solutionDir, string program, string command)
        {
            bool success = true;
            using (Process process = new Process())
            {
                process.StartInfo.WorkingDirectory = solutionDir;
                process.StartInfo.FileName = program;
                process.StartInfo.Arguments = command;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit(60000);
                if (!process.HasExited)
                {
                    process.Kill();
                    success = false;
                }
                else
                {
                    if (process.ExitCode != 0)
                    {
                        success = false;
                    }
                }
            }
            return success;
        }
    }
}
