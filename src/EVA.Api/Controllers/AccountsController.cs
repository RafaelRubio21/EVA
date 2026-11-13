using EVA.Application.Accounts.Commands.AddEmailAccount;
using EVA.Application.Accounts.Commands.DeleteEmailAccount;
using EVA.Application.Accounts.Commands.ToggleEmailAccount;
using EVA.Application.Accounts.Queries.GetEmailAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class AccountsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lista todas as contas de e-mail cadastradas para monitoramento pela EVA.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmailAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmailAccountDto>>> GetAccounts(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmailAccountsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra uma nova conta de e-mail (ex.: corporativa e pessoal) para triagem simultânea.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmailAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmailAccountDto>> AddAccount(
        [FromBody] AddEmailAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAccounts), new { id = result.Id }, result);
    }

    /// <summary>
    /// Ativa ou pausa o monitoramento automático de uma conta de e-mail.
    /// </summary>
    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType(typeof(EmailAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailAccountDto>> ToggleAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ToggleEmailAccountCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove uma conta de e-mail cadastrada do monitoramento da EVA.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteEmailAccountCommand(id), cancellationToken);
        return NoContent();
    }
}
