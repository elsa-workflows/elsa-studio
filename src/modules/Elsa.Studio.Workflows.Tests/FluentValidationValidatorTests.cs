using Blazilla;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class FluentValidationValidatorTests : TestContext
{
    [Fact]
    public async Task ValidateAsync_WhenModelIsInvalid_AddsValidationMessages()
    {
        var model = new TestModel();
        var validator = new TestModelValidator();
        var cut = Render<TestHost>(parameters => parameters
            .Add(x => x.Model, model)
            .Add(x => x.Validator, validator));

        var isValid = await cut.Instance.EditContext.ValidateAsync();

        Assert.False(isValid);
        Assert.Equal(["Name is required."], cut.Instance.EditContext.GetValidationMessages(new FieldIdentifier(model, nameof(TestModel.Name))));
    }

    [Fact]
    public async Task ValidateAsync_WhenModelBecomesValid_ClearsPreviousMessages()
    {
        var model = new TestModel();
        var validator = new TestModelValidator();
        var cut = Render<TestHost>(parameters => parameters
            .Add(x => x.Model, model)
            .Add(x => x.Validator, validator));

        await cut.Instance.EditContext.ValidateAsync();
        model.Name = "Elsa";

        var isValid = await cut.Instance.EditContext.ValidateAsync();

        Assert.True(isValid);
        Assert.Empty(cut.Instance.EditContext.GetValidationMessages(new FieldIdentifier(model, nameof(TestModel.Name))));
    }

    [Fact]
    public void NotifyFieldChanged_RevalidatesOnlyTheChangedField()
    {
        var model = new TestModel();
        var validator = new TestModelValidator();
        var cut = Render<TestHost>(parameters => parameters
            .Add(x => x.Model, model)
            .Add(x => x.Validator, validator));
        var nameField = new FieldIdentifier(model, nameof(TestModel.Name));
        var descriptionField = new FieldIdentifier(model, nameof(TestModel.Description));

        cut.Instance.EditContext.NotifyFieldChanged(nameField);

        Assert.Equal(["Name is required."], cut.Instance.EditContext.GetValidationMessages(nameField));
        Assert.Empty(cut.Instance.EditContext.GetValidationMessages(descriptionField));
    }

    public class TestHost : ComponentBase
    {
        [Parameter, EditorRequired] public TestModel Model { get; set; } = default!;
        [Parameter, EditorRequired] public IValidator Validator { get; set; } = default!;

        public EditContext EditContext { get; private set; } = default!;
        protected override void OnParametersSet()
        {
            EditContext = new EditContext(Model);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddAttribute(1, "EditContext", EditContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<EditContext>)(_ => childBuilder =>
            {
                childBuilder.OpenComponent<FluentValidator>(0);
                childBuilder.AddAttribute(1, nameof(FluentValidator.Validator), Validator);
                childBuilder.AddAttribute(2, nameof(FluentValidator.AsyncMode), true);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }
}
