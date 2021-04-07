using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace psapi
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var powershell = PowerShell.Create())
            {
                powershell.AddCommand("systeminfo").Invoke();
            }
        }
    }
}
