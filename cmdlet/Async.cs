using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace cmdlet
{
    public abstract class AsyncCmdlet : PSCmdlet
    {
        private readonly ConcurrentQueue<ICmdletTask> _cmdletTaskQueue;

        private readonly List<Task> _processingTasks;

        private Task _beginProcessingTask;

        protected AsyncCmdlet()
        {
            _processingTasks = new List<Task>();
            _cmdletTaskQueue = new ConcurrentQueue<ICmdletTask>();
        }

        protected abstract void HandleException(Exception e);

        protected virtual Task BeginProcessingAsync() => Task.CompletedTask;

        protected virtual Task ProcessRecordAsync() => Task.CompletedTask;

        protected virtual Task EndProcessingAsync() => Task.CompletedTask;

        protected override void BeginProcessing()
        {
            // Kick off BeginProcessingAsync()
            _beginProcessingTask = BeginProcessingAsync();

            // Drain queue to this point
            RunQueuedTasks();
        }

        protected override void ProcessRecord()
        {
            // If not yet processed, complete BeginProcessingAsync() queue, mark it as processed
            if (_beginProcessingTask is not null)
            {
                try
                {
                    _beginProcessingTask.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    HandleException(e);
                }

                RunQueuedTasks();
                _beginProcessingTask = null;
            }

            // Kick off ProcessRecordAsync()
            _processingTasks.Add(ProcessRecordAsync());

            // Drain queue to this point
            RunQueuedTasks();
        }

        protected override void EndProcessing()
        {
            // Complete ProcessRecord() queue
            foreach (Task processTask in _processingTasks)
            {
                try
                {
                    processTask.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
            }

            RunQueuedTasks();

            // Run EndProcessingAsync()
            try
            {
                EndProcessingAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private void RunQueuedTasks()
        {
            var tasks = new List<ICmdletTask>();

            while (_cmdletTaskQueue.TryDequeue(out ICmdletTask task))
            {
                tasks.Add(task);
            }

            foreach (ICmdletTask task in tasks)
            {
                task.DoInvoke(this);
            }
        }
    }

    internal interface ICmdletTask
    {
        void DoInvoke(Cmdlet cmdlet);
    }

    internal abstract class CmdletTask<T> : ICmdletTask
    {
        private readonly TaskCompletionSource<T> _taskCompletionSource;

        protected CmdletTask()
        {
            _taskCompletionSource = new TaskCompletionSource<T>();
        }

        public Task<T> ResultTask => _taskCompletionSource.Task;

        public abstract T Invoke(Cmdlet cmdlet);

        public void DoInvoke(Cmdlet cmdlet)
        {
            try
            {
                T result = Invoke(cmdlet);
                _taskCompletionSource.SetResult(result);
            }
            catch (Exception e)
            {
                _taskCompletionSource.SetException(e);
            }
        }
    }

    internal abstract class CmdletTask : ICmdletTask
    {
        private readonly TaskCompletionSource<object> _taskCompletionSource;

        protected CmdletTask()
        {
            _taskCompletionSource = new TaskCompletionSource<object>();
        }

        public Task Result => _taskCompletionSource.Task;

        public abstract void Invoke(Cmdlet cmdlet);

        public void DoInvoke(Cmdlet cmdlet)
        {
            try
            {
                Invoke(cmdlet);
                _taskCompletionSource.SetResult(null);
            }
            catch (Exception e)
            {
                _taskCompletionSource.SetException(e);
            }
        }
    }

    internal abstract class StringMessage : CmdletTask
    {
        protected StringMessage(string message)
        {
            Message = message;
        }

        protected string Message { get; }
    }

    internal class VerboseMessage : StringMessage
    {
        public VerboseMessage(string message)
            : base(message)
        {
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteVerbose(Message);
        }
    }

    internal class DebugMessage : StringMessage
    {
        public DebugMessage(string message)
            : base(message)
        {
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteDebug(Message);
        }
    }

    internal class WarningMessage : StringMessage
    {
        public WarningMessage(string message)
            : base(message)
        {
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteWarning(Message);
        }
    }

    internal class ErrorMessage : CmdletTask
    {
        private readonly ErrorRecord _errorRecord;

        public ErrorMessage(ErrorRecord errorRecord)
        {
            _errorRecord = errorRecord;
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteError(_errorRecord);
        }
    }

    internal class InformationMessage : CmdletTask
    {
        private readonly InformationRecord _informationRecord;

        public InformationMessage(InformationRecord informationRecord)
        {
            _informationRecord = informationRecord;
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteInformation(_informationRecord);
        }
    }

    internal class ObjectMessage : CmdletTask
    {
        private readonly object _object;

        private readonly bool _enumerate;

        public ObjectMessage(object obj, bool enumerate)
        {
            _object = obj;
            _enumerate = enumerate;
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteObject(_object, _enumerate);
        }
    }

    internal class ProgressMessage : CmdletTask
    {
        private readonly ProgressRecord _progressRecord;

        public ProgressMessage(ProgressRecord progressRecord)
        {
            _progressRecord = progressRecord;
        }

        public override void Invoke(Cmdlet cmdlet)
        {
            cmdlet.WriteProgress(_progressRecord);
        }
    }
}