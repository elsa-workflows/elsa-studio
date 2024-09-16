using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Contracts;
using FluentValidation;

namespace Elsa.Studio.Agents.UI.Validators;

/// <summary>
/// A validator for <see cref="ApiKeyInputModel"/> instances.
/// </summary>
public class ApiKeyInputModelValidator : AbstractValidator<ApiKeyInputModel>
{
    /// <inheritdoc />
    public ApiKeyInputModelValidator(IApiKeysApi apiKeysApi, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the API key.");
        
        RuleFor(x => x.Name)
            .MustAsync(async (context, name, cancellationToken) =>
            {
                blazorServiceAccessor.Services = serviceProvider;
                var request = new IsUniqueNameRequest
                {
                    Name = name!,
                };
                var response = await apiKeysApi.GetIsNameUniqueAsync(request, cancellationToken);
                return response.IsUnique;
            })
            .WithMessage("An API key with this name already exists.");
    }
}