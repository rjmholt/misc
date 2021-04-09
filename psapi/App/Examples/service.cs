using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace psapi
{
    public class ExamplePowerShellService : IPowerShellService, IDisposable
    {
        public static ExamplePowerShellService Create(int maxRunspaces)
        {
            RunspacePool runspacePool = RunspaceFactory.CreateRunspacePool(minRunspaces: 1, maxRunspaces);
            runspacePool.Open();
            return new ExamplePowerShellService(runspacePool);
        }

        private readonly RunspacePool _runspacePool;

        private bool _disposedValue;

        public ExamplePowerShellService(RunspacePool runspacePool)
        {
            _runspacePool = runspacePool;
        }

        public Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken)
        {
            var powershell = PowerShell.Create();
            powershell.RunspacePool = _runspacePool;
            cancellationToken.Register(() => powershell.Stop());

            return powershell
                .AddScript(script)
                .AddCommand("Out-String")
                .InvokeAsync()
                .ContinueWith(psTask =>
                {
                    powershell.Dispose();

                    PSDataCollection<PSObject> results;
                    try
                    {
                        results = psTask.GetAwaiter().GetResult();
                    }
                    catch (PipelineStoppedException e)
                    {
                        throw new OperationCanceledException("PowerShell task cancelled", e);
                    }

                    var sb = new StringBuilder();
                    foreach (PSObject result in results)
                    {
                        sb.Append((string)result.BaseObject);
                    }
                    return sb.ToString();
                });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}