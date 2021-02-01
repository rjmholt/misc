using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.IO;

namespace psapi
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var powershell = PowerShell.Create())
            {
                DateTime date = powershell
                    .AddCommand("Get-Date")
                    .Invoke<DateTime>()
                    .First();

                Console.WriteLine(date);

                DateTime secondDate = powershell
                    .AddCommand("Get-Date")
                    .Invoke<DateTime>()
                    .First();

                Console.WriteLine(secondDate);
            }
        }
    }
}
