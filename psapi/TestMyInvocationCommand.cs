
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

[Cmdlet(VerbsDiagnostic.Test, "MyInvocation")]
public class TestScriptRunCommand : PSCmdlet
{
protected override void EndProcessing()
{
    // Lock for incrementing percentage and writing progress
    var progressLock = new object();
    // This is our global monotonically increasing count of task progress across threads
    int count = 0;
    // How much progress we've made
    int percentage = 0;

    Enumerable.Range(0, 1000)
        .AsParallel()
        .ForAll(i => {
            // Simulate work
            Task.Delay(1).Wait();

            // Each thread can atomically increment the count and get a unique result
            // If this thread is one of the 100 that ticks over a 0, it writes progress
            if (Interlocked.Increment(ref count) % 10 == 0)
            {
                // Now we lock around both writing progress and incrementing percentage
                lock (progressLock)
                {
                    Host.UI.WriteProgress(
                        sourceId: 1,
                        new ProgressRecord(activityId: 1, "Counting", "Almost done!")
                        {
                            PercentComplete = percentage++
                        });
                }

                // If we decided that Host.UI.WriteProgress is threadsafe, we could do this equivalently and save needing to use a lock:
                // Host.UI.WriteProgress(
                //     sourceId: 1,
                //     new ProgressRecord(activityId: 1, "Counting", "Almost done!")
                //     {
                //         PercentComplete = Interlocked.Increment(ref percentage),
                //     });
            }
        });
}
}