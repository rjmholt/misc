using System.Collections.Generic;
using System.Management.Automation;

[Cmdlet(VerbsDiagnostic.Test, "RunspaceInheritance")]
public class TestRunspaceInheritanceCommand : Cmdlet
{
    [Parameter(Mandatory = true)]
    public string VariableName { get; set; }

    protected override void EndProcessing()
    {
        using (var powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
        {
            IEnumerable<PSObject> results = powershell
                .AddScript($"${VariableName}")
                .Invoke();

            foreach (PSObject result in results)
            {
                WriteObject(result);
            }
        }
    }
}