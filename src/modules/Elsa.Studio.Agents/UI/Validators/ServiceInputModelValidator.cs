using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Contracts;
using FluentValidation;

namespace Elsa.Studio.Agents.UI.Validators;

/// <summary>
/// A validator for <see cref="ServiceInputModel"/> instances.
/// </summary>
public class ServiceInputModelValidator : AbstractValidator<ServiceInputModel>
{
    /// <inheritdoc />
    public ServiceInputModelValidator(IServicesApi api, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the service.");
        
        RuleFor(x => x.Name)
            .MustAsync(async (context, name, cancellationToken) =>
            {
                blazorServiceAccessor.Services = serviceProvider;
                var request = new IsUniqueNameRequest
                {
                    Name = name!,
                };
                var response = await api.GetIsNameUniqueAsync(request, cancellationToken);
                return response.IsUnique;
            })
            .WithMessage("A service with this name already exists.");
    }
}