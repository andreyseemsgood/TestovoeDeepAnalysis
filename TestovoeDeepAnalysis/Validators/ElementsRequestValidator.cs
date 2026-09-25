using FluentValidation;
using TestovoeDeepAnalysis.Models;

namespace TestovoeDeepAnalysis.Validators;

public class ElementsRequestValidator : AbstractValidator<ElementsRequest>
{
    public ElementsRequestValidator()
    {
        RuleFor(x => x.Selector)
            .NotEmpty()
            .WithMessage("Selector is required.");

        RuleFor(x => x.Attribute)
            .NotEmpty()
            .WithMessage("Attribute is required.");

        RuleFor(x => x.UrlB64)
            .NotEmpty()
            .WithMessage("URL is required.");

        RuleFor(x => x.EncryptedTextBytesB64)
            .NotEmpty()
            .WithMessage("Encrypted text is required.");

        RuleFor(x => x.KeyBytesB64)
            .NotEmpty()
            .WithMessage("Encryption key is required.");

        RuleFor(x => x.PageB64)
            .NotEmpty()
            .WithMessage("Page is required.");
    }
}