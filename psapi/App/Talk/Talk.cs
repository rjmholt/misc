using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace psapi
{
    static class Talk
    {
        public static void Run()
        {
            using (var powershell = PowerShell.Create())
            {
                Collection<int> results = powershell
                    .AddCommand("Where-Object")
                        .AddArgument(ScriptBlock.Create("$_ -gt 2"))
                    .Invoke<int>(input: new [] { 1, 2, 3, 4, 5 });

                foreach (int result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}