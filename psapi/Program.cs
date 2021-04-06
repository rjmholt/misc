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
                var dataCollection = new PSDataCollection<PSObject>();
                Task<PSDataCollection<PSObject>> psTask = powershell.AddScript("1..10 | % { $_; sleep 1 }").InvokeAsync(dataCollection);

                Thread.Sleep(2000);

                powershell.Stop();

                try
                {
                    psTask.GetAwaiter().GetResult();
                }
                catch (PipelineStoppedException)
                {

                }

                foreach (PSObject result in dataCollection)
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
            var runspacePool = RunspaceFactory.CreateRunspacePool(1, maxRunspaces);
            runspacePool.Open();
            return new PowerShellScriptRunner(runspacePool);
        }

        private readonly RunspacePool _runspacePool;
        private bool _disposedValue;

        public PowerShellScriptRunner(RunspacePool runspacePool)
        {
            _runspacePool = runspacePool;
        }

        public Task<IReadOnlyList<T>> RunScriptAsync<T>(string script, CancellationToken cancellationToken)
        {
            var powershell = PowerShell.Create();
            powershell.RunspacePool = _runspacePool;

            // If this async call is cancelled, then this PowerShell run will be cancelled
            cancellationToken.Register(() => powershell.Stop());

            powershell.AddScript(script);

            return powershell.InvokeAsync()
                .ContinueWith(psTask =>
                {
                    powershell.Dispose();

                    var list = new List<T>();
                    foreach (PSObject result in psTask.Result)
                    {
                        list.Add((T)result.BaseObject);
                    }
                    return (IReadOnlyList<T>)list;
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
