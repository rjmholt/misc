using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace psapi
{
    class Program
    {
        static void Main(string[] args)
        {
            Runspace.DefaultRunspace = RunspaceFactory.CreateRunspace();
            Runspace.DefaultRunspace.Open();

            using (var powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                // Set $x = 2 and import PSScriptAnalyzer
                IEnumerable<PSModuleInfo> modules = powershell
                    .AddScript("$x = 2")
                    .AddStatement()
                    .AddCommand("Import-Module")
                        .AddParameter("Name", "PSScriptAnalyzer")
                        .AddParameter("PassThru")
                    .Invoke<PSModuleInfo>();

                Console.WriteLine("IMPORTED MODULES:");
                foreach (PSModuleInfo module in modules)
                {
                    Console.WriteLine($"{module.Name} [{module.Version}]");
                }
                Console.WriteLine();
            }

            using (var powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                // Now list all loaded modules
                IEnumerable<PSModuleInfo> loadedModules = powershell
                    .AddCommand("Get-Module")
                    .Invoke<PSModuleInfo>();
                
                Console.WriteLine("FOUND LOADED MODULES:");
                foreach (PSModuleInfo module in loadedModules)
                {
                    Console.WriteLine($"{module.Name} [{module.Version}]");
                }
                Console.WriteLine();

                powershell.Commands.Clear();

                // Now get the value of $x
                IEnumerable<PSObject> results = powershell
                    .AddScript("$x")
                    .Invoke();

                foreach (PSObject result in results)
                {
                    Console.WriteLine($"$x = {result}");
                }
            }
        }
    }
}
