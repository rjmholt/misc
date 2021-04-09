using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace psapi
{
    class Basic2 : IExample
    {
        public void Run()
        {
            using (var powershell = PowerShell.Create())
            {
                Collection<PSObject> results = powershell
                    .AddCommand("Get-ChildItem")
                    .AddParameter("Path", ".")
                    .Invoke();

                foreach (PSObject result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}