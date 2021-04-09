
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace psapi
{
    class Basic4 : IExample
    {
        public void Run()
        {
            using (var powershell = PowerShell.Create())
            {
                Collection<PSObject> results = powershell.AddScript("Get-ChildItem -Path .").Invoke();

                foreach (PSObject result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}