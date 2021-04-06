using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace psapi
{
    [Cmdlet(VerbsLifecycle.Invoke, "Example")]
    public class InvokeExampleCommand : PSCmdlet
    {
        private readonly ConcurrentQueue<(Action, TaskCompletionSource)> _callbacks;
        private readonly BlockingCollection<(Action, TaskCompletionSource)> _callbackQueue;

        public InvokeExampleCommand()
        {
            _callbacks = new ConcurrentQueue<(Action, TaskCompletionSource)>();
            _callbackQueue = new BlockingCollection<(Action, TaskCompletionSource)>(_callbacks);
        }

        protected override void EndProcessing()
        {
            // Kick off the work we need to do
            Task workTask = DoWorkAsync();
            // While we wait, service the task
            // You might like to implement this as an extension method: DoWorkAsync().AwaitAndRunCallbacks()
            AwaitTasksAndRunCallbacks(workTask);
        }

        private async Task DoWorkAsync()
        {
            await Task.Delay(1000);

            // WriteVerbose is now async-ified
            // Note that we need to await it so we don't race the callback loop
            await WriteVerboseAsync("Work done!");
        }

        // Create a new method that lets us call back to the pipeline thread and wait for the result
        private Task WriteVerboseAsync(string message)
        {
            // Simply queue up the work we want to do, and return a task completion source to wait on
            var completion = new TaskCompletionSource();
            _callbackQueue.Add((() => WriteVerbose(message), completion));
            return completion.Task;
        }

        // Here we service the callbacks and also join the task that's been passed in
        private void AwaitTasksAndRunCallbacks(params Task[] tasks)
        {
            var allDone = Task.WhenAll(tasks);

            // Set up a cancellation to allow us to break out of the loop below
            using (var cancellationSource = new CancellationTokenSource())
            {
                // When the tasks complete, we'll cancel the loop
                allDone.ContinueWith(_ => cancellationSource.Cancel());

                try
                {
                    // Service the callback queue while we wait for the task to complete
                    foreach ((Action action, TaskCompletionSource completion) callback in _callbackQueue.GetConsumingEnumerable(cancellationSource.Token))
                    {
                        // Call the desired callback on the pipeline thread
                        callback.action();
                        // Now tell the async caller the callback is done
                        callback.completion.SetResult();
                    }
                }
                catch (OperationCanceledException)
                {
                    // When the loop is cancelled, we absorb this exception and continue
                }
            }

            // Make sure there aren't any unserved callbacks on the next call
            _callbacks.Clear();

            // Join the tasks back to the pipeline thread, surfacing any exceptions from them
            allDone.GetAwaiter().GetResult();
        }
    }
}