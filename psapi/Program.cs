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
                powershell
                    .AddCommand("Import-Module")
                        .AddParameter("Name", "PSScriptAnalyzer")
                        .AddParameter("PassThru")
                    .AddCommand("Out-Host")
                    .Invoke();
            }
        }
    }
}
