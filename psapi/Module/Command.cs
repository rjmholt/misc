using System.Collections.ObjectModel;
using System.Management.Automation;

namespace psapi
{
    [Cmdlet(VerbsLifecycle.Invoke, "Example")]
    public class Command : PSCmdlet
    {
        [Parameter]
        public ScriptBlock Script { get; set; }

        protected override void EndProcessing()
        {
            Collection<PSObject> results = Script.Invoke();
            WriteObject(results, enumerateCollection: true);
        }
    }
}