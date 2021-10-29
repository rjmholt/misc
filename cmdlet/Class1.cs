using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;

namespace cmdlet
{
    [Cmdlet("Test", "MyCmdlet")]
    public class MyCmdlet : PSCmdlet
    {
        [Parameter]
        public bool Boolean { get; set; }
    }

    [Cmdlet(VerbsDiagnostic.Test, "Cmdlet")]
    public class TestCmdletCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public int Count { get; set; }

        protected override void EndProcessing()
        {
            long progress = 0;
            int currPercent = 0;
            Enumerable.Range(0, Count)
                .AsParallel()
                .Select(v => {
                    // Find the current percentage this operation corresponds to
                    // Note we need to convert from int to double to int in order to prevent an integer overflow
                    int thisPercent = (int)(100.0*Interlocked.Increment(ref progress)/Count);

                    // Now we want to check if the percentage has actually updated
                    // If it hasn't, don't do the expensive WriteProgress operation
                    //
                    // We replace the current progress percentage with our new one (relying on it to monotonically increase)
                    // and if it's changed, we write progress
                    if (Interlocked.Exchange(ref currPercent, thisPercent) < thisPercent)
                    {
                        Host.UI.WriteProgress(
                            sourceId: 1,
                            new ProgressRecord(activityId: 0, "Counting", $"Counting to {Count}")
                            {
                                PercentComplete = thisPercent,
                            });
                    }

                    return v;
                })
                .ToList();
        }
    }

    public class Example : Cmdlet
    {
        protected override void EndProcessing()
        {
            PSSession session = null;

            using (var pwsh = PowerShell.Create())
            {
                pwsh.Runspace = session.Runspace;

                Collection<PSObject> results = pwsh
                    .AddCommand("Get-ChildItem")
                    .AddParameter("Path", "C:\\")
                    .AddParameter("Force", true)
                    .Invoke();

                WriteObject(results, enumerateCollection: true);
            }
        }
    }

    [Cmdlet("Test", "Positional")]
    public class Positional : PSCmdlet
    {
        [Parameter(Position = 0)]
        public string Thing { get; set; }

        [Parameter(ValueFromRemainingArguments = true)]
        public object[] Remaining { get; set; }

        protected override void EndProcessing()
        {
            Console.WriteLine(MyInvocation.BoundParameters.GetType());
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "BoolParam1")]
    public class BoolParamCmdlet1 : PSCmdlet
    {
        [Parameter]
        public bool EnableThing { get; set; }

        protected override void EndProcessing()
        {
            var value = new Dictionary<string, object>
            {
                { "DefaultValue", "example" }
            };

            if (MyInvocation.BoundParameters.ContainsKey(nameof(EnableThing)))
            {
                value["EnableThing"] = EnableThing;
            }

            WriteObject(value);
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "BoolParam2")]
    public class BoolParamCmdlet2 : Cmdlet
    {
        [Parameter]
        public bool? EnableThing { get; set; }

        protected override void EndProcessing()
        {
            var value = new Dictionary<string, object>
            {
                { "DefaultValue", "example" }
            };

            if (EnableThing.HasValue)
            {
                value["EnableThing"] = EnableThing.Value;
            }

            WriteObject(value);
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "EnumParam")]
    public class EnumParamCmdlet : PSCmdlet
    {
        public enum Values
        {
            On,
            Off,
        }

        [Parameter]
        public Values ThingState { get; set; }

        protected override void EndProcessing()
        {
            var value = new Dictionary<string, object>
            {
                { "DefaultValue", "example" }
            };

            if (MyInvocation.BoundParameters.ContainsKey(nameof(ThingState)))
            {
                value["EnableThing"] = ThingState switch
                {
                    Values.On => true,
                    Values.Off => false,
                    _ => throw new Exception("Bad"),
                };
            }

            WriteObject(value);
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "Help", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/skype/dummy")]
    public class TestHelpCommand : PSCmdlet
    {

    }

    [Cmdlet(VerbsDiagnostic.Test, "Pipeline")]
    public class TestPipelineCommand : PSCmdlet
    {
        private PropertyInfo _pipelineParameter;

        [Parameter(ValueFromPipeline = true)]
        public object[] Stuff { get; set; }

        protected override void BeginProcessing()
        {
            _pipelineParameter = GetPipelineParameter();
        }

        protected override void ProcessRecord()
        {
            if (_pipelineParameter is not null)
            {
                object value = _pipelineParameter.GetValue(this);

                if (value is object[] array)
                {
                    foreach (object item in array)
                    {
                        Console.WriteLine(item);
                    }
                    return;
                }

                Console.WriteLine(value);
            }
        }

        private PropertyInfo GetPipelineParameter()
        {
            foreach (PropertyInfo property in typeof(TestPipelineCommand).GetProperties())
            {
                foreach (ParameterAttribute paramAttr in property.GetCustomAttributes<ParameterAttribute>())
                {
                    if (!string.Equals(paramAttr.ParameterSetName, ParameterSetName, StringComparison.OrdinalIgnoreCase)
                        || !paramAttr.ValueFromPipeline)
                    {
                        continue;
                    }

                    return property;
                }
            }

            return null;
        }
    }
}
