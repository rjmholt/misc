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
            using (var scriptRunner = PowerShellScriptRunner.Create(maxRunspaces: 1))
            {
                var cancellationSource = new CancellationTokenSource(2000);
                Task<PSDataCollection<PSObject>> task = scriptRunner.RunScriptAsync("Sleep 10; 'Hello'", cancellationSource.Token);

                foreach (PSObject result in task.GetAwaiter().GetResult())
                {
                    Console.WriteLine(result);
                }
            }
        }
    }

class PowerShellScriptRunner : IDisposable
{
    public static PowerShellScriptRunner Create(int maxRunspaces)
    {
        RunspacePool runspacePool = RunspaceFactory.CreateRunspacePool(minRunspaces: 1, maxRunspaces);
        runspacePool.Open();
        return new PowerShellScriptRunner(runspacePool);
    }

    private readonly RunspacePool _runspacePool;
    private bool _disposedValue;

    protected PowerShellScriptRunner(RunspacePool runspacePool)
    {
        _runspacePool = runspacePool;
    }

    public Task<PSDataCollection<PSObject>> RunScriptAsync(string script, CancellationToken cancellationToken)
    {
        var powershell = PowerShell.Create();
        powershell.RunspacePool = _runspacePool;
        cancellationToken.Register(() => powershell.Stop());

        return powershell
            .AddScript(script)
            .InvokeAsync()
            .ContinueWith(psTask =>
            {
                // Dispose of PowerShell asynchronously
                powershell.Dispose();

                PSDataCollection<PSObject> results = null;
                try
                {
                    results = psTask.GetAwaiter().GetResult();
                }
                catch (PipelineStoppedException e)
                {
                    // Convert the PipelineStoppedException into an OperationCanceledException
                    // to conform better to the TAP API expectation
                    throw new OperationCanceledException($"Execution of PowerShell script canceled", e);
                }

                return results;
            });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _runspacePool.Dispose();
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
