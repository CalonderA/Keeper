using Keeper.Api.Contracts;
using Keeper.Api.Domain;
using Keeper.Api.Models;
using Keeper.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Keeper.Api.Controllers;

[ApiController]
[Route("api/visitors")]
[Produces("application/json")]
public class VisitorsController : ControllerBase
{
    private readonly IKeeperService _keeper;

    public VisitorsController(IKeeperService keeper)
    {
        _keeper = keeper;
    }

    [HttpPost]
    [ProducesResponseType(typeof(VisitorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VisitorResponse>> Register(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateVisitorRequest? request,
        CancellationToken cancellationToken)
    {
        var visitor = await _keeper.RegisterVisitorAsync(request?.Name, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = visitor.Id }, VisitorResponse.From(visitor));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VisitorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VisitorResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var visitors = await _keeper.GetVisitorsAsync(cancellationToken);
        return Ok(visitors.Select(VisitorResponse.From));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VisitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisitorResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var visitor = await _keeper.GetVisitorAsync(id, cancellationToken);
        if (visitor is null)
        {
            return VisitorNotFound(id);
        }

        return Ok(VisitorResponse.From(visitor));
    }

    [HttpPost("{id:int}/actions")]
    [ProducesResponseType(typeof(ActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionResponse>> PerformAction(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] VisitorActionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request?.Action is null)
        {
            return BadRequest(new ErrorResponse(
                "Поле 'action' обязательно. Допустимые значения: Introduce, OfferHelp, Threaten."));
        }

        var outcome = await _keeper.PerformActionAsync(id, request.Action.Value, cancellationToken);

        switch (outcome.Status)
        {
            case ActionOutcomeStatus.VisitorNotFound:
                return VisitorNotFound(id);

            case ActionOutcomeStatus.AlreadyPerformed:
                return BadRequest(new ErrorResponse(
                    $"Посетитель уже выполнял действие '{request.Action}'. Повторное выполнение запрещено."));

            default:
                var record = outcome.Record!;
                return Ok(new ActionResponse(
                    record.VisitorId,
                    record.Action,
                    record.TrustChange,
                    record.TrustLevelAfter,
                    record.PerformedAt,
                    BuildActionMessage(record)));
        }
    }

    [HttpGet("{id:int}/access")]
    [ProducesResponseType(typeof(AccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessResponse>> CheckAccess(int id, CancellationToken cancellationToken)
    {
        var visitor = await _keeper.CheckAccessAsync(id, cancellationToken);
        if (visitor is null)
        {
            return VisitorNotFound(id);
        }

        var allowed = visitor.TrustLevel >= TrustRules.AccessThreshold;
        var message = allowed
            ? $"Хранитель разрешает проход: уровень доверия {visitor.TrustLevel} не ниже {TrustRules.AccessThreshold}."
            : $"Хранитель не пропускает: уровень доверия {visitor.TrustLevel} ниже {TrustRules.AccessThreshold}.";

        return Ok(new AccessResponse(
            visitor.Id,
            visitor.TrustLevel,
            TrustRules.AccessThreshold,
            allowed,
            message));
    }

    private NotFoundObjectResult VisitorNotFound(int id) =>
        NotFound(new ErrorResponse($"Посетитель с идентификатором {id} не найден."));

    private static string BuildActionMessage(VisitorActionRecord record) => record.Action switch
    {
        VisitorActionType.Introduce =>
            $"Посетитель представился и объяснил цель визита. Уровень доверия: {record.TrustLevelAfter}.",
        VisitorActionType.OfferHelp =>
            $"Посетитель предложил помощь в охране замка. Уровень доверия: {record.TrustLevelAfter}.",
        VisitorActionType.Threaten =>
            $"Посетитель угрожал Хранителю. Уровень доверия: {record.TrustLevelAfter}.",
        _ => $"Действие выполнено. Уровень доверия: {record.TrustLevelAfter}."
    };
}
