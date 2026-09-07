using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Validators;

/// <summary>
/// A validator for <see cref="ExportGraphOptions"/> instances.
/// </summary>
public class ExportGraphOptionsValidator : AbstractValidator<ExportGraphOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportGraphOptionsValidator"/> class.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    public ExportGraphOptionsValidator(ILocalizer localizer)
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage(localizer["Please enter a file name for the export."]);

        // Scoped to the formats whose export actually uses the padding, which are exactly the formats for which the
        // dialog shows the field. Validating it for SVG too would block the dialog on a field the user cannot see.
        RuleFor(x => x.Padding)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Format != ExportGraphFormat.Svg)
            .WithMessage(localizer["The padding cannot be negative."]);
    }
}
