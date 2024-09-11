using Elsa.Secrets;
using Elsa.Secrets.UniqueName;
using Elsa.Studio.Contracts;
using Elsa.Studio.Secrets.Client;
using FluentValidation;

namespace Elsa.Studio.Secrets.UI.Validators;

/// <summary>
/// A validator for <see cref="SecretInputModel"/> instances.
/// </summary>
public class SecretInputModelValidator : AbstractValidator<SecretInputModel>
{
    /// <inheritdoc />
    public SecretInputModelValidator(ISecretsApi api, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the secret.");
        
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
            .WithMessage("A secret with this name already exists.");
    }
}