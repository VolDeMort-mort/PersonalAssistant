
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Features.Journal.Commands;
using PersonalAssistant.Application.Features.Journal.Queries;

namespace PersonalAssistant.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalController : ControllerBase
{
    private readonly IMediator _mediator;

    public JournalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> SaveEntry([FromBody] SaveJournalSessionCommand command)
    {
        await _mediator.Send(command);
        return Ok();
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var query = new GetJournalEntriesByDateQuery(date);
        var entries = await _mediator.Send(query);

        return Ok(entries);
    }

}