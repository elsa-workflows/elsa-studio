using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Contracts;
using FluentValidation;

namespace Elsa.Studio.Agents.UI.Validators;

/// <summary>
/// A validator for <see cref="AgentInputModel"/> instances.
/// </summary>
public class AgentInputModelValidator : AbstractValidator<AgentInputModel>
{
    /// <inheritdoc />
    public AgentInputModelValidator(IAgentsApi agentsApi, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the agent.");
        
        RuleFor(x => x.Name)
            .MustAsync(async (context, name, cancellationToken) =>
            {
                blazorServiceAccessor.Services = serviceProvider;
                var request = new IsUniqueNameRequest
                {
                    Name = name!,
                };
                var response = await agentsApi.GetIsNameUniqueAsync(request, cancellationToken);
                return response.IsUnique;
            })
            .WithMessage("An agent with this name already exists.");
    }
}