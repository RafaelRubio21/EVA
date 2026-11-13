using EVA.Application.Settings.Commands.UpdateEvaSettings;
using EVA.Application.Settings.Queries.GetEvaSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class SettingsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retorna os intervalos atuais configurados para leitura de e-mails e verificação de lembretes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(EvaSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EvaSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEvaSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza os intervalos (em segundos) de leitura de e-mails e verificação de lembretes.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(EvaSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EvaSettingsDto>> UpdateSettings(
        [FromBody] UpdateEvaSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
