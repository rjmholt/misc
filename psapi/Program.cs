using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace psapi
{
    class Program
    {
        static void Main(string[] args)
        {
            // Note the big problem here:
            // In synchronous contexts we'd use a `using` statement
            // to dispose of PowerShell when it's done.
            // Now we're asynchronous, we need to get smarter
            var powershell = PowerShell.Create().AddScript("Get-Module -ListAvailable");

            Task<Collection<PSModuleInfo>> task = powershell.InvokeAsync<PSModuleInfo>(new PSDataCollection<PSModuleInfo>())
                .ContinueWith(psTask =>
                {
                    powershell.Dispose();

                    var list = new List<PSModuleInfo>(psTask.Result.Count);
                    foreach (PSObject result in psTask.Result)
                    {
                        list.Add((PSModuleInfo)result.BaseObject);
                    }
                    return new Collection<PSModuleInfo>(list);
                });

            foreach (PSModuleInfo module in task.GetAwaiter().GetResult())
            {
                Console.WriteLine(module);
            }
        }
    }
}
