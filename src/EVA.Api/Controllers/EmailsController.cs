using EVA.Application.Common.Models;
using EVA.Application.Emails.Commands.CompleteEmailResponse;
using EVA.Application.Emails.Commands.ProcessNewEmail;
using EVA.Application.Emails.Commands.RescheduleEmailReminder;
using EVA.Application.Emails.Queries.GetEmailById;
using EVA.Application.Emails.Queries.GetEmails;
using EVA.Application.Emails.Queries.GetPendingEmails;
using EVA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EVA.Api.Controllers;

public sealed record RescheduleReminderRequest(DateTimeOffset NextReminderAt);

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class EmailsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lista e-mails com filtros opcionais (conta, prioridade, pendência de resposta) e paginação.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmailSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EmailSummaryDto>>> GetEmails(
        [FromQuery] string? accountEmail,
        [FromQuery] PriorityLevel? priorityLevel,
        [FromQuery] bool? requiresResponse,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetEmailsQuery(accountEmail, priorityLevel, requiresResponse, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lista todos os e-mails já triados pela IA que exigem resposta do usuário,
    /// ordenados por nível de prioridade, prazo final e data de recebimento.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingEmailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingEmailDto>>> GetPendingEmails(
        [FromQuery] string? accountEmail,
        [FromQuery] PriorityLevel? priorityLevel,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPendingEmailsQuery(accountEmail, priorityLevel),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém os detalhes completos de um e-mail e sua análise de IA pelo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmailDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailDetailsDto>> GetEmailById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmailByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Recebe um novo e-mail para triagem inteligente pela EVA, classifica a prioridade via IA
    /// e agenda o próximo lembrete caso exija resposta.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessNewEmailResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProcessNewEmailResult>> ProcessNewEmail(
        [FromBody] ProcessNewEmailCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEmailById), new { id = result.EmailRecordId }, result);
    }

    /// <summary>
    /// Marca a resposta de um e-mail pendente como concluída e cancela lembretes futuros.
    /// </summary>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompleteEmailResponse(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CompleteEmailResponseCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reagenda o próximo lembrete de um e-mail que aguarda resposta, respeitando o Deadline.
    /// </summary>
    [HttpPatch("{id:guid}/reminder")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RescheduleReminder(
        Guid id,
        [FromBody] RescheduleReminderRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledAt = await sender.Send(
            new RescheduleEmailReminderCommand(id, request.NextReminderAt),
            cancellationToken);

        return Ok(new { EmailId = id, NextReminderAt = scheduledAt });
    }
}
