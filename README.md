# FindDuplicates

FindDuplicates is a cli tool that finds duplicated names across packages in azure-sdk-for-net whose namespace starts with `Azure.`.

## Usage

You could go to directory `FindDuplicates` and use `dotnet run` and necessary argument to run the tool.

### Argument and options

One required argument of this tool is the local root directory path of the `azure-sdk-for-net` repo:

```
dotnet run /path/to/azure-sdk-for-net
```

One option is `targetPackage`, if you specify this value, the results will be filtered to include only duplicates related to the target package.
