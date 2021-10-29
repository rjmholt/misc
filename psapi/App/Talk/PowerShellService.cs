using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace psapi
{
    public class PowerShellService : IPowerShellService
    {
        public static PowerShellService Create(int maxRunspaces)
        {
            throw new NotImplementedException();
        }

        public Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}