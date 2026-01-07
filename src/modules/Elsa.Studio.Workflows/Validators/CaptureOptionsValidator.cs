using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Options;
using FluentValidation;

namespace Elsa.Studio.Workflows.Validators;

/// <summary>
/// A validator for <see cref="CaptureOptions"/> instances.
/// </summary>
public class CaptureOptionsValidator : AbstractValidator<CaptureOptions>
{
    public CaptureOptionsValidator(ILocalizer localizer)
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage(localizer["Please enter a filename for the export."]);
        RuleFor(x => x.Padding).GreaterThanOrEqualTo(0);
    }
}