using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;

namespace psapi
{
    [Cmdlet(VerbsLifecycle.Invoke, "Script")]
    public class InvokeScriptCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock Script { get; set; }

        protected override void EndProcessing()
        {
            var sb = ScriptBlock.Create("'Hello'");
            Script.Invoke();
            sb.Invoke();
            Task.Run(() => Script.Invoke());
            Task.Run(() => sb.Invoke());
        }
    }
}