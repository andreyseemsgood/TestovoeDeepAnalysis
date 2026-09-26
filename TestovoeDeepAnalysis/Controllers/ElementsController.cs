using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TestovoeDeepAnalysis.Models;
using TestovoeDeepAnalysis.Services;

namespace TestovoeDeepAnalysis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ElementsController : ControllerBase
{
    private readonly ElementsService _elementsService;

    public ElementsController(ElementsService elementsService)
    {
        _elementsService = elementsService;
    }

    [HttpPost("process")]
    public async Task<ElementsResponse> Process(
        ElementsRequest request,
        CancellationToken cancellationToken)
    {
        return await _elementsService.ProcessAsync(request, cancellationToken);
    }
    
}