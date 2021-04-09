using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace psapi
{
    class Basic3 : IExample
    {
        public void Run()
        {
            using (var powershell = PowerShell.Create())
            {
                Collection<PSObject> results = powershell
                    .AddCommand("Get-Date")
                    .AddParameter("Date", "10:00:00")
                    .Invoke();

                foreach (PSObject result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}