using System.CommandLine;
using FindDuplicates;

var rootDirOption = new Argument<string>(
    name: "--sdkPath",
    description: "The root directory of azure-sdk-for-net");

var targetPackage = new Option<string>(
    name: "--targetPackage",
    description: "The target package that you are dealing with");

var rootCommand = new RootCommand("Cli tool to find duplicates between the target package and other .NET track 2 packages");
rootCommand.AddArgument(rootDirOption);
rootCommand.AddOption(targetPackage);

rootCommand.SetHandler(
    (string rootDir, string target) => new Find(rootDir, target).Exexute(), rootDirOption, targetPackage);

return await rootCommand.InvokeAsync(args);