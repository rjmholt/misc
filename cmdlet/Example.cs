using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;

/*
public abstract class ExampleCmdlet : PSCmdlet
{
    private IAsyncPipelineProcessor _asyncProcessor;

    // Ideally each cmdlet can override this rather than needing to use reflection.
    // If you do use reflection, cache the result so you don't have to do it for every pipeline input
    protected abstract IReadOnlyList<object> GetProcessingInput();

    protected override void ProcessRecord()
    {
        // Initialize the processor if we haven't already
        if (_asyncProcessor is null)
        {
            _asyncProcessor = TryGetFlightedApi(out FlightedCommandInfo flightedCommand)
                ? CreateFlightedPipelineProcessor(flightedCommand)
                : CreateTrpsPipelineProcessor();

            _asyncProcessor.Begin();
        }

        // Send current input through
        _asyncProcessor.SendInput(GetProcessingInput());

        // Receive any output and write it out
        IReadOnlyList<object> pendingResults = _asyncProcessor.ReceiveOutput();
        WriteObject(pendingResults, enumerateCollection: true);
    }

    protected override void EndProcessing()
    {
        IReadOnlyList<object> results = _asyncProcessor.WaitForAndCollectRemainingOutput();
        WriteObject(results, enumerateCollection: true);
    }

    private bool TryGetFlightedApi(out FlightingCommandInfo flightedCommand)
    {
        flightedCommand = FlightingUtils.GetFlightingCommandInfo(MyInvocation.BoundParameters, MyInvocation.MyCommand.Name);
        return IsConfigApiExecution(flightedCommand);
    }

    private SteppablePipelineProcessor CreateFlightedPipelineProcessor(FlightingCommandInfo flightedCommand)
    {
        CommandInfo modernCommandInfo = flightedCommand.MockedCommandInfo ?? flightedCommand.AutoRestCommandInfo;
        return SteppablePipelineProcessor.Create(MyInvocation, modernCommandInfo);
    }

    private RemoteCommandPipelineProcessor CreateTrpsPipelineProcessor()
    {
        var pwsh = PowerShell.Create()
            .AddCommand(MyInvocation.MyCommand.Name)
            .AddParameters(MyInvocation.BoundParameters);
        pwsh.Runspace = TrpsSessionPsCmds.GetSession().Runspace;
        return new RemoteCommandPipelineProcessor(pwsh);
    }
}

internal interface IAsyncPipelineProcessor
{
    void Begin();

    void SendInput(IReadOnlyList<object> input);

    IReadOnlyList<object> ReceiveOutput();

    IReadOnlyList<object> WaitForAndCollectRemainingOutput();
}

internal class SteppablePipelineProcessor : IAsyncPipelineProcessor
{
    public static SteppablePipelineProcessor Create(InvocationInfo myInvocation, CommandInfo commandInfo)
    {
        SteppablePipeline steppablePipeline = ScriptBlock
            .Create(@"param($cmdletName, $boundParams) & $cmdletName @boundParams")
            .GetSteppablePipeline(myInvocation.CommandOrigin, new object[] { commandInfo, myInvocation.BoundParameters });

        return new SteppablePipelineProcessor(steppablePipeline, myInvocation.ExpectingInput);
    }

    private readonly SteppablePipeline _steppablePipeline;

    private readonly bool _expectingInput;

    private readonly Queue<object> _resultBuffer;

    public SteppablePipelineProcessor(SteppablePipeline steppablePipeline, bool expectingInput)
    {
        _steppablePipeline = steppablePipeline;
        _expectingInput = expectingInput;
        _resultBuffer = new Queue<object>();
    }

    public void Begin()
    {
        _steppablePipeline.Begin(_expectingInput);
    }

    public void SendInput(IReadOnlyList<object> input)
    {
        foreach (object obj in input)
        {
            foreach (object result in _steppablePipeline.Process(obj))
            {
                _resultBuffer.Enqueue(result);
            }
        }
    }

    public IReadOnlyList<object> ReceiveOutput()
    {
        var results = new List<object>();
        while (_resultBuffer.Count > 0)
        {
            results.Add(_resultBuffer.Dequeue());
        }
        return results;
    }

    public IReadOnlyList<object> WaitForAndCollectRemainingOutput()
    {
        return (object[])_steppablePipeline.End();
    }
}

internal class RemoteCommandPipelineProcessor : IAsyncPipelineProcessor
{
    private readonly PowerShell _pwsh;

    private readonly PSDataCollection<object> _input;

    private readonly PSDataCollection<object> _output;

    private readonly ConcurrentQueue<object> _resultBuffer;

    private IAsyncResult _pwshAsyncResult;

    public RemoteCommandPipelineProcessor(PowerShell pwsh)
    {
        _pwsh = pwsh;
        _input = new PSDataCollection<object>() { BlockingEnumerator = true };
        _output = new PSDataCollection<object>();
        _output.DataAdding += OnDataAdding;
        _resultBuffer = new ConcurrentQueue<object>();
    }

    public void Begin()
    {
        _pwsh.BeginInvoke(_input, _output);
    }

    public void SendInput(IReadOnlyList<object> input)
    {
        foreach (object obj in input)
        {
            _input.Add(obj);
        }
    }

    public IReadOnlyList<object> ReceiveOutput() => DrainResultBuffer();

    public IReadOnlyList<object> WaitForAndCollectRemainingOutput()
    {
        _pwsh.EndInvoke(_pwshAsyncResult);
        return DrainResultBuffer();
    }

    private IReadOnlyList<object> DrainResultBuffer()
    {
        var list = new List<object>();
        while (_resultBuffer.TryDequeue(out object obj))
        {
            list.Add(obj);
        }
        return list;
    }

    private void OnDataAdding(object sender, DataAddingEventArgs args)
    {
        _resultBuffer.Enqueue(args.ItemAdded);
    }
}
*/
