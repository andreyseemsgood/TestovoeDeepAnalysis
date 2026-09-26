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
    private readonly IValidator<ElementsRequest> _validator;

    public ElementsController(ElementsService elementsService, IValidator<ElementsRequest> validator)
    {
        _elementsService = elementsService;
        _validator = validator;
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process(
        ElementsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => error.ErrorMessage)
                .ToList();

            return BadRequest(new ElementsResponse
            {
                IsError = 1,
                ErrorCode = 400,
                ErrorMessage = string.Join(" ", errors)
            });
        }

        var response = await _elementsService.ProcessAsync(
            request,
            cancellationToken);

        return StatusCode(
            response.IsError == 1 ? response.ErrorCode : 200,
            response);
    }
    
}