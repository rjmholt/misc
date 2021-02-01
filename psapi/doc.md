# Using the PowerShell .NET API

There are a number cases where it's desirable to call PowerShell from another .NET language,
particularly from C#.
The two main classes of scenario here are:

- Running PowerShell code or reusing PowerShell commands from a cmdlet or module.
- Using PowerShell from a standalone .NET application, usually by hosting a fresh instance of PowerShell

Both use cases have a largely similar usage pattern,
but there are some subtle differences that will be covered further in.

In this article we'll discuss how to call PowerShell from .NET
using the `PowerShell` API &mdash; the type exported by the PowerShell SDK
that provides access to the PowerShell engine.

## PowerShell API Basics: Running commands using the `PowerShell` object

The basic pattern for calling PowerShell from .NET looks like this:

```csharp
// Create a "PowerShell" object for use with the API
using (var powershell = PowerShell.Create())
{
    // Add the script to the PowerShell object and invoke it for the result
    powershell
        .AddScript("Restart-Service -Name winrm -Force")
        .Invoke();
}
```

A `PowerShell` object is created which manages our interaction with the PowerShell engine.
We can use it to build state, such as commands, and then invoke them.
Then, we simply dispose of this object once we're done to clean things up.

### Using the structured command API

The simple invocation above will work for many use-cases,
but there's a better way to create command invocations through the PowerShell API.
Methods on the `PowerShell` object allow us to build a command invocation
in a way that's more readable, efficient and also allows us to pass .NET objects directly in as parameters.

The command we invoked with `AddScript()` before, we can now build with more structure:

```csharp
using (var powershell = PowerShell.Create())
{
    powershell
        .AddCommand("Restart-Service")
            .AddParameter("Name", "winrm")
            .AddParameter("Force")
        .Invoke();
}
```

The command invocation is now separated out into the constituent command and parameters.
This can be preferable simply for readability and validation reasons
(it's impossible to suffer a syntax error using this structured builder approach),
but there are two other, greater advantages.

Firstly, because PowerShell is an object-oriented shell,
using strings for everything can be limiting,
and indeed there are times when it's desirable to pass an object from .NET directly to PowerShell.
And this is possible to do using the `PowerShell` API.
This can be useful if:

- You already have the object type you want the cmdlet to use
- You want to avoid needing to embed a PowerShell syntax within a .NET call
  (e.g. rather than writing a string in C# that will be parsed as a PowerShell hashtable, you want to just pass a hashtable object in)
- You can process the input object faster in C#, or are looking to avoid possible PowerShell type conversions

For example, if we have a .NET timezone object,
we can pass it straight through to `Set-TimeZone`,
since it has a parameter that accepts a `TimeZoneInfo` object:

```csharp
using (var powershell = PowerShell.Create())
{
    var vladivostokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Vladivostok Standard Time");

    powershell
        .AddCommand("Set-TimeZone")
            .AddParameter("InputObject", vladivostokTimeZone)
        .Invoke();
}
```

The second advantage of the structured `PowerShell` API is that it's possible to bypass command lookup,
meaning that:

- It's possible to get a marginal improvement in first-time performance, and
- For advanced scenarios, it's possible to use anonymous or unexported commands

To reuse our last example, this time we create a `CmdletInfo` object to pass in as the command.
The difference is that because we've supplied the type,
PowerShell does not need to do as much work to find the cmdlet:

```csharp
using (var powershell = PowerShell.Create())
{
    var vladivostokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Vladivostok Standard Time");

    var setTimeZoneCmdlet = new CmdletInfo("Set-TimeZone", typeof(Microsoft.PowerShell.Commands.SetTimeZoneCommand));

    powershell
        .AddCommand(setTimeZoneCmdlet)
            .AddParameter("InputObject", vladivostokTimeZone)
        .Invoke();
}
```

Note also that PowerShell will still apply its parameter type conversion logic to a parameter input
(the logic from `LanguagePrimitives.ConvertTo()`),
so for example the following will work passing `Set-Date` a string when the parameter type is `DateTime`:

```csharp
using (var powershell = PowerShell.Create())
{
    string date = "2017-03-04 00:00:00Z";

    powershell
        .AddCommand("Set-Date")
            .AddParameter("Date", date)
        .Invoke();
}
```

### Getting output from PowerShell invocations

So far we've discussed executing PowerShell from .NET and passing arguments in,
but a common need is to get the result of a PowerShell execution in .NET.
The `PowerShell` API makes this very straightforward,
with the `Invoke()` method returning all results from the executed command:

```csharp
using (var powershell = PowerShell.Create())
{
    Collection<PSObject> modules = powershell
        .AddCommand("Get-Module")
            .AddParameter("Name", "Az.*")
            .AddParameter("ListAvailable")
        .Invoke();

    foreach (PSObject result in modules)
    {
        PSModuleInfo module = result.BaseObject;
        Console.WriteLine($"Found Az module '{module.Name}' at path '{module.Path}'");
    }
}
```

You'll notice here though that the result objects we get back are:

- All of the same type, and
- Of a type we have a static concept of in our .NET application (i.e. we can refer to the type without reflection using something like `typeof(T)`)

In this case, we can improve our call so that the `PowerShell` API gives us a more strongly-typed collection back:

```csharp
using (var powershell = PowerShell.Create())
{
    Collection<PSModuleInfo> modules = powershell
        .AddCommand("Get-Module")
            .AddParameter("Name", "Az.*")
            .AddParameter("ListAvailable")
        .Invoke<PSModuleInfo>();

    foreach (PSModuleInfo module in modules)
    {
        Console.WriteLine($"Found Az module '{module.Name}' at path '{module.Path}'");
    }
}
```

You might wonder what happens when you get a return type
that doesn't match the generic type you specified,
and the simple answer is that it throws an error.
So executing this:

```csharp
using (var powershell = PowerShell.Create())
{
    Collection<Hashtable> result = powershell
        .AddCommand("Import-Module")
            .AddParameter("Name", "Pester")
            .AddParameter("PassThru")
        .Invoke<Hashtable>();
}
```

Throws an error like this:

```output
Unhandled exception. System.Management.Automation.CmdletInvocationException: Cannot convert the "Pester" value of type "System.Management.Automation.PSModuleInfo" to type "System.Collections.Hashtable".
 ---> System.Management.Automation.PSInvalidCastException: Cannot convert the "Pester" value of type "System.Management.Automation.PSModuleInfo" to type "System.Collections.Hashtable".
   at System.Management.Automation.LanguagePrimitives.ThrowInvalidCastException(Object valueToConvert, Type resultType)
   at System.Management.Automation.LanguagePrimitives.ConvertNoConversion(Object valueToConvert, Type resultType, Boolean recurse, PSObject originalValueToConvert, IFormatProvider formatProvider, TypeTable backupTable)
   at System.Management.Automation.LanguagePrimitives.ConversionData`1.Invoke(Object valueToConvert, Type resultType, Boolean recurse, PSObject originalValueToConvert, IFormatProvider formatProvider, TypeTable backupTable)
   at System.Management.Automation.LanguagePrimitives.ConvertTo(Object valueToConvert, Type resultType, Boolean recursion, IFormatProvider formatProvider, TypeTable backupTypeTable)
   at System.Management.Automation.Internal.PSDataCollectionStream`1.Write(Object obj, Boolean enumerateCollection)
   at System.Management.Automation.Internal.ObjectStreamBase.Write(Object value)
   at System.Management.Automation.Internal.ObjectWriter.Write(Object obj)
   at System.Management.Automation.Internal.Pipe.AddToPipe(Object obj)
   at System.Management.Automation.Internal.Pipe.Add(Object obj)
   at System.Management.Automation.MshCommandRuntime._WriteObjectSkipAllowCheck(Object sendToPipeline)
   at System.Management.Automation.MshCommandRuntime.DoWriteObject(Object sendToPipeline)
   at System.Management.Automation.MshCommandRuntime.WriteObject(Object sendToPipeline)
   at System.Management.Automation.Cmdlet.WriteObject(Object sendToPipeline)
   at Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadModule(PSModuleInfo parentModule, String fileName, String moduleBase, String prefix, SessionState ss, Object privateData, ImportModuleOptions& options, ManifestProcessingFlags manifestProcessingFlags, Boolean& found, Boolean& moduleFileFound)
   at Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadUsingExtensions(PSModuleInfo parentModule, String moduleName, String fileBaseName, String extension, String moduleBase, String prefix, SessionState ss, ImportModuleOptions options, ManifestProcessingFlags manifestProcessingFlags, Boolean& found, Boolean& moduleFileFound)
   at Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadUsingMultiVersionModuleBase(String moduleBase, ManifestProcessingFlags manifestProcessingFlags, ImportModuleOptions importModuleOptions, Boolean& found)
   at Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadUsingModulePath(PSModuleInfo parentModule, Boolean found, IEnumerable`1 modulePath, String name, SessionState ss, ImportModuleOptions options, ManifestProcessingFlags manifestProcessingFlags, PSModuleInfo& module)
   at Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadUsingModulePath(Boolean found, IEnumerable`1 modulePath, String name, SessionState ss, ImportModuleOptions options, ManifestProcessingFlags manifestProcessingFlags, PSModuleInfo& module)
   at Microsoft.PowerShell.Commands.ImportModuleCommand.ImportModule_LocallyViaName(ImportModuleOptions importModuleOptions, String name)
   at Microsoft.PowerShell.Commands.ImportModuleCommand.ImportModule_LocallyViaName_WithTelemetry(ImportModuleOptions importModuleOptions, String name)
   at Microsoft.PowerShell.Commands.ImportModuleCommand.ProcessRecord()
   at System.Management.Automation.Cmdlet.DoProcessRecord()
   at System.Management.Automation.CommandProcessor.ProcessRecord()
   --- End of inner exception stack trace ---
   at System.Management.Automation.Runspaces.PipelineBase.Invoke(IEnumerable input)
   at System.Management.Automation.Runspaces.Pipeline.Invoke()
   at System.Management.Automation.PowerShell.Worker.ConstructPipelineAndDoWork(Runspace rs, Boolean performSyncInvoke)
   at System.Management.Automation.PowerShell.Worker.CreateRunspaceIfNeededAndDoWork(Runspace rsToUse, Boolean isSync)
   at System.Management.Automation.PowerShell.CoreInvokeHelper[TInput,TOutput](PSDataCollection`1 input, PSDataCollection`1 output, PSInvocationSettings settings)
   at System.Management.Automation.PowerShell.CoreInvoke[TInput,TOutput](PSDataCollection`1 input, PSDataCollection`1 output, PSInvocationSettings settings)
   at System.Management.Automation.PowerShell.Invoke[T](IEnumerable input, IList`1 output, PSInvocationSettings settings)
   at System.Management.Automation.PowerShell.Invoke[T]()
   at psapi.Program.Main(String[] args) in C:\Users\me\psapi\Program.cs:line 16
```

Another common scenario though is calling commands
that have a return type that can't be statically resolved
or that return pure PSObjects.
One tip for those scenarios is using `dynamic` to more easily manipulate output.
For example, the `Invoke-ScriptAnalyzer` command from the PSScriptAnalyzer module emits a type called `DiagnosticRecord`,
but because the only way to compile an application against PSScriptAnalyzer (currently) is to clone the source code,
it's hard to get a compile-time reference to the `DiagnosticRecord` type.
Instead, an easy way to manipulate the emitted objects is to simply reference them as `dynamic`,
because PowerShell's PSObjects implement `DynamicMetaObject` to make this seamless:

```csharp
using (var powershell = PowerShell.Create())
{
    var pssaSettings = new Hashtable
    {
        { "IncludeRules", new [] { "PSAvoidUsingPlainTextForPassword", "UseBOMForUnicodeEncodedFile" } },
    };

    Collection<PSObject> diagnostics = powershell
        .AddCommand("Invoke-ScriptAnalyzer")
            .AddParameter("Path", scriptPath)
            .AddParameter("Settings", pssaSettings)
        .Invoke();

    foreach (dynamic diagnostic in diagnostics)
    {
        Console.WriteLine($"Issue found: '{diagnostic.Message}' in '{diagnostic.Extent.File}'");
    }
}
```

One final variation on getting results from a PowerShell invocation is to provide an `IList<T>` for PowerShell to populate.
Modifying our earlier example:

```csharp
using (var powershell = PowerShell.Create())
{
    var modules = new List<PSModuleInfo>();

    powershell
        .AddCommand("Get-Module")
            .AddParameter("Name", "Az.*")
            .AddParameter("ListAvailable")
        .Invoke<PSModuleInfo>(input: null, output: modules);

    foreach (PSModuleInfo module in modules)
    {
        Console.WriteLine($"Found Az module '{module.Name}' at path '{module.Path}'");
    }
}
```

This can be useful when:

- You want a different collection to `Collection<T>` without copying from one to the other
- You want specific actions to occur when each item is added to the collection.
  If this second case is required, `PSDataCollection<T>` may be a good option,
  since it provides for concurrent scenarios and has `DataAdded` and `DataAdding` events.

### Pipelines and statements

In all the examples above, we've only looked at how to use the `PowerShell` API to invoke a single command.
However, the structured API is actually more capable than that;
we can execute PowerShell pipelines (where the output of one command is used as the input to another)
and even statements.

For example, let's take a command that gets the 10 most memory intensive processes on the system,
`Get-Process | Sort-Object -Property WS -Descending | Select-Object -First 10`,
and use the `PowerShell` API to run it:

```csharp
using (var powershell = PowerShell.Create())
{
    // Note that subsequent .AddCommand() calls
    // implicitly pipe the previous command to the next
    Collection<Process> processes = powershell
        .AddCommand("Get-Process")
        .AddCommand("Sort-Object")
            .AddParameter("Property", "WS")
            .AddParameter("Descending")
        .AddCommand("Select-Object")
            .AddParameter("First", 10)
        .Invoke<Process>();

    foreach (Process process in processes)
    {
        Console.WriteLine($"Process '{process.Name}' ({process.Id})");
    }
}
```

Extending this example somewhat,
we can add a preceding statement to the invocation:

```csharp
using (var powershell = PowerShell.Create())
{
    Collection<Process> processes = powershell
        .AddCommand("Start-Process")
            .AddParameter("FilePath", "calc.exe")
        .AddStatement()
        .AddCommand("Get-Process")
        .AddCommand("Sort-Object")
            .AddParameter("Property", "WS")
            .AddParameter("Descending")
        .AddCommand("Select-Object")
            .AddParameter("First", 10)
        .Invoke<Process>();

    foreach (Process process in processes)
    {
        Console.WriteLine($"Process '{process.Name}' ({process.Id})");
    }
}
```

This is now equivalent to:

```powershell
Start-Process -FilePath "calc.exe"

Get-Process | Sort-Object -Property WS -Descending | Select-Object -First 10
```

So you can think of the `AddStatement()` method as like adding a semicolon to the invocation you're assembling.

An important thing to note here is the result;
just like in a PowerShell script, all statements emit objects to the final result returned,
so the `Process` object emitted by `calc.exe` will be the first object in the `processes` value
(which will have up to 11 entries).

### Adding inputs and arguments

In some circumstances, it's also desirable to introduce values into the start of the invocation.
With the `PowerShell` API this is simple to do by providing an `input` argument to the `Invoke()` method,
which just adds the given objects to the invocation as if they were at the start of the pipeline.

Let's say we want to run something simple like `$hashtable | ConvertTo-Json`,
then we can accomplish that fairly easily like this:

```csharp
using (var powershell = PowerShell.Create())
{
    var hashtable = new Hashtable
    {
        { "A", 1 },
        { "B", 2 },
    };

    // Note that we need to wrap the hashtable in an array,
    // since input is expected to be an enumerable
    //
    // Also note that .First() is from LINQ,
    // which combines well with PowerShell's fluent API
    string json = powershell
        .AddCommand("ConvertTo-Json")
        .Invoke<string>(new object[] { hashtable })
        .First();

    Console.WriteLine($"JSON:\n{json}");
}
```

Often when the command being invoked is done with the structured approach though,
it's easier to pass the input in as a value:

```csharp
using (var powershell = PowerShell.Create())
{
    var hashtable = new Hashtable
    {
        { "A", 1 },
        { "B", 2 },
    };

    string json = powershell
        .AddCommand("ConvertTo-Json")
            .AddParameter("InputObject", hashtable)
        .Invoke<string>()
        .First();

    Console.WriteLine($"JSON:\n{json}");
}
```

However, there are scenarios where it's easier to run PowerShell as a script,
but where the full .NET object is needed from within that script.
When executed with `AddScript()`, the input value comes from the `$input` automatic variable:

```csharp
using (var powershell = PowerShell.Create())
{
    var nums = new [] { 1, 2, 3 };

    string json = powershell
        .AddScript("$input | ConvertTo-Json")
        .Invoke<string>(nums)
        .First();

    Console.WriteLine($"JSON:\n{json}");
}
```

Note that for `$input` to work properly, you need to pipe it to your commands.
Giving it directly as an argument can lead to strange behavior with the `IEnumerable` value.

### Managing errors and streams

PowerShell invocation is not always a simple matter of inputs and outputs.
PowerShell has multiple streams where information can be sent,
and commands and execution can fail.

Importantly, PowerShell has several categories of error,
and they are exposed to a .NET application in different ways.

First of all, parse errors (which reflect an incorrect syntax in the script) and terminating errors
will all throw the error up into the hosting context.
These errors are all instances of `System.Management.Automation.RuntimeException`,
but in particular may be things like `ParseException` or `ActionPreferenceStopException`.
For example, running a program like this:

```csharp
using (var powershell = PowerShell.Create())
{
    try
    {
        powershell.AddScript("throw 'Bad'").Invoke();
    }
    catch (RuntimeException e)
    {
        Console.WriteLine(e);
    }
}
```

Will print an output like this:

```output
System.Management.Automation.RuntimeException: e
 ---> System.Management.Automation.RuntimeException: e
   --- End of inner exception stack trace ---
   at System.Management.Automation.Runspaces.PipelineBase.Invoke(IEnumerable input)
   at System.Management.Automation.Runspaces.Pipeline.Invoke()
   at System.Management.Automation.PowerShell.Worker.ConstructPipelineAndDoWork(Runspace rs, Boolean performSyncInvoke)
   at System.Management.Automation.PowerShell.Worker.CreateRunspaceIfNeededAndDoWork(Runspace rsToUse, Boolean isSync)
   at System.Management.Automation.PowerShell.CoreInvokeHelper[TInput,TOutput](PSDataCollection`1 input, PSDataCollection`1 output, PSInvocationSettings settings)
   at System.Management.Automation.PowerShell.CoreInvoke[TInput,TOutput](PSDataCollection`1 input, PSDataCollection`1 output, PSInvocationSettings settings)
   at System.Management.Automation.PowerShell.Invoke(IEnumerable input, PSInvocationSettings settings)
   at System.Management.Automation.PowerShell.Invoke()
   at psapi.Program.Main(String[] args) in C:\Users\me\psapi\Program.cs:line 16
```

It's more likely you'll hit a terminating error because of a cmdlet, which might look like this:

```csharp
using (var powershell = PowerShell.Create())
{
    try
    {
        powershell
            .AddCommand("New-Item")
                .AddParameter("LiteralPath", "C:\\Does\\Not\\Exist.txt")
                .AddParameter("ErrorAction", ActionPreference.Stop)
            .Invoke();
    }
    catch (RuntimeException e)
    {
        Console.WriteLine(e);
    }
}
```

Often though, errors in PowerShell are *non-terminating*,
in which case they will not throw.
In the following example, no output will be shown at all:

```csharp
using (var powershell = PowerShell.Create())
{
    Collection<FileSystemInfo> files = powershell
        .AddCommand("Get-ChildItem")
            .AddParameter("LiteralPath", "C:\\Does\\not\\exist")
        .Invoke<FileSystemInfo>();

    // files will be empty, so nothing will be printed
    foreach (FileSystemInfo file in files)
    {
        Console.WriteLine(file.FullName);
    }
}
```

Instead, it's necessary to check for errors and then inspect the error stream:

```csharp
using (var powershell = PowerShell.Create())
{
    Collection<FileSystemInfo> files = powershell
        .AddCommand("Get-ChildItem")
            .AddParameter("LiteralPath", "C:\\Does\\not\\exist")
        .Invoke<FileSystemInfo>();

    foreach (FileSystemInfo file in files)
    {
        Console.WriteLine(file.FullName);
    }

    // HadErrors (corresponding to $? in PowerShell script) will be true
    if (powershell.HadErrors)
    {
        foreach (ErrorRecord error in powershell.Streams.Error)
        {
            Console.WriteLine(error);
        }
    }
}
```

Indeed the `Streams` property is where any information sent to any stream can be found:

```csharp
using (var powershell = PowerShell.Create())
{
    powershell
        .AddCommand("Import-Module")
            .AddParameter("Name", "Pester")
        .Invoke();

    foreach (VerboseRecord verboseRecord in powershell.Streams.Verbose)
    {
        Console.WriteLine($"[VRB]: {verboseRecord}");
    }
}
```

This will print something like:

```output
[VRB]: Loading module from path 'C:\Users\Robert Holt\Documents\PowerShell\Modules\Pester\5.1.0\Pester.psd1'.
[VRB]: Populating RepositorySourceLocation property for module Pester.
[VRB]: Loading module from path 'C:\Users\Robert Holt\Documents\PowerShell\Modules\Pester\5.1.0\Pester.psm1'.
[VRB]: Importing function 'Add-ShouldOperator'.
[VRB]: Importing function 'AfterAll'.
[VRB]: Importing function 'AfterEach'.
...
```

### Dealing with formatting

We've been printing a lot in the examples we've looked at (because it's an easy way to present output),
but unlike in the PowerShell console, we're not getting any of the nice formatting we sometimes get.
For example, if we run `Get-ChildItem` and print the resulting objects like this:

```csharp
using (var powershell = PowerShell.Create())
{
    IEnumerable<FileSystemInfo> files = powershell
        .AddCommand("Get-ChildItem")
        .Invoke<FileSystemInfo>();

    foreach (FileSystemInfo file in files)
    {
        Console.WriteLine(file);
    }
}
```

Then the printed output is exactly what .NET provides by default (because it's the same type):

```output
/Users/me/psapi/bin
/Users/me/psapi/obj
/Users/me/psapi/doc.md
/Users/me/psapi/Program.cs
/Users/me/psapi/psapi.csproj
```

But if we want to get the actual formatted output that PowerShell would present in the console,
we need to run the formatters somehow.
The way to do that is to pipe to `Out-String`
(remembering that the result will now be a collection of strings; the lines of the formatted output):

```csharp
using (var powershell = PowerShell.Create())
{
    IEnumerable<string> lines = powershell
        .AddCommand("Get-ChildItem")
        .AddCommand("Out-String")
        .Invoke<string>();

    foreach (string line in lines)
    {
        Console.WriteLine(line);
    }
}
```

This prints something like the following:

```output

    Directory: /Users/me/psapi

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d----           31/1/2021  7:49 pm                bin
d----           31/1/2021  7:53 pm                obj
-----           31/1/2021  8:02 pm          24561 doc.md
-----           31/1/2021  8:05 pm            664 Program.cs
-----           31/1/2021  7:49 pm            279 psapi.csproj

```

This may seem like an obscure thing to do,
but there is a key scenario where it can be important,
and that is when the end of a pipeline that you wish to execute
could be a `Format-*` cmdlet.
Let's see what happens in a case like this:

```csharp
using (var powershell = PowerShell.Create())
{
    IEnumerable<PSObject> results = powershell
        .AddCommand("Get-ChildItem")
        .AddCommand("Format-Table")
            .AddParameter("Property", new [] { "Name", "FullName" })
        .Invoke<PSObject>();

    foreach (PSObject result in results)
    {
        Console.WriteLine(result);
    }
}
```

Running this prints:

```output
Microsoft.PowerShell.Commands.Internal.Format.FormatStartData
Microsoft.PowerShell.Commands.Internal.Format.GroupStartData
Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData
Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData
Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData
Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData
Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData
Microsoft.PowerShell.Commands.Internal.Format.GroupEndData
Microsoft.PowerShell.Commands.Internal.Format.FormatEndData
```

If we want to get the proper output (i.e. render these `FormatEntryData` objects),
we must use `Out-String`:

```csharp
using (var powershell = PowerShell.Create())
{
    IEnumerable<string> lines = powershell
        .AddCommand("Get-ChildItem")
        .AddCommand("Format-Table")
            .AddParameter("Property", new [] { "Name", "FullName" })
        .AddCommand("Out-String")
        .Invoke<string>();

    foreach (string line in lines)
    {
        Console.WriteLine(result);
    }
}
```

With `Out-String` added, we now get the right output:

```output

Name         FullName
----         --------
bin          /Users/me/psapi/bin
obj          /Users/me/psapi/obj
doc.md       /Users/me/psapi/doc.md
Program.cs   /Users/me/psapi/Program.cs
psapi.csproj /Users/me/psapi/psapi.csproj

```

Again, this may seem like an obscure scenario,
but it's a common one when implementing an appliciation that takes arbitrary PowerShell scripts,
executes them, and then presents the output.
If users use a `Format-*` cmdlet, they expect to see formatted output.
The most common form of this is when implementing a PowerShell Host,
which we won't go into here,
but in that case `Out-Default` should be used instead of `Out-String`.
(However, `Out-String` is still needed when implementing debugging REPL functionality.)

### Reusing the PowerShell instance

One thing you might find yourself wanting to do is running multiple invocations
using the same `PowerShell` instance.

You'll notice that if you try to simply run multiple commands in sequence from the same instance
you might get a strange result:

```

```

### Using `PSInvocationSettings` to configure your invocation

### `PowerShell` vs `PSCommand`

### API style tips

The first thing to note about the PowerShell API is that it offers a *fluent* interface,
where method chaining is used to build the object state.

## Runspaces, threads, async and the PowerShell API

### Runspace configuration and disposal

### Runspaces and state in PowerShell

### Using the async API

## Better ways to run PowerShell from cmdlets; the Intrinsics APIs

## Things to know when using threads with the PowerShell API

### Runspace creation and reuse

### Cmdlet callbacks and the pipeline thread
