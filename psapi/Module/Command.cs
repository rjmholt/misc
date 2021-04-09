using System.Management.Automation;

namespace psapi
{
    [Cmdlet(VerbsLifecycle.Invoke, "Example")]
    public class Command : PSCmdlet
    {
        protected override void EndProcessing()
        {
            WriteObject("Hello");
        }
    }
}