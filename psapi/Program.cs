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
            using (var powershell = PowerShell.Create())
            {
                IEnumerable<FileSystemInfo> results = powershell
                    .AddCommand("Get-ChildItem")
                        .AddParameter("Path", "./here")
                        .AddParameter("Recurse")
                    .Invoke<FileSystemInfo>()
                    .Where(fsi => fsi.Name.EndsWith(".txt"))
                    .Take(10);

                foreach (FileSystemInfo result in results)
                {
                    Console.WriteLine(result.FullName);
                }
            }
        }
    }
}
