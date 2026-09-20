
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Features.Journal.Commands;

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
    public async Task<IActionResult> SaveEntry([FromBody] SaveJournalEntryCommand command)
    {
        var resultId = await _mediator.Send(command);
        return Ok(new { Id = resultId });
    }
}