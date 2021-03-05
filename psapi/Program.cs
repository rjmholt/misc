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

            var parameters = new Dictionary<string, object>
            {
                { "x", "banana" },
                { "y", "duck" },
                { "z", "horsey" },
            };

            string scriptLocation = Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "..",
                    "..",
                    "..",
                    "ex.ps1"));

            using (var powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                IEnumerable<PSObject> results = powershell
                    .AddCommand(scriptLocation)
                    .AddParameters(parameters)
                    .Invoke();

                foreach (PSObject result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}
