using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace psapi
{
    [Route("ps")]
    [ApiController]
    public class PsaasController : ControllerBase
    {
        private readonly IPowerShellService _psService;

        public PsaasController(IPowerShellService psService)
        {
            _psService = psService;
        }

        [HttpGet]
        public async Task<ActionResult<string>> RunScript(
            [FromQuery] string script,
            [FromQuery] int? timeout,
            CancellationToken requestCancellationToken)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
            cts.CancelAfter(timeout ?? 2000);
            try
            {
                return Ok(await _psService.RunPowerShellAsync(script, cts.Token).ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, $"Request to run script '{script}' timed out");
            }
            finally
            {
                cts?.Dispose();
            }
        }
    }
}