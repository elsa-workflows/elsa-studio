using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;

namespace Elsa.Studio.UIHintHandlers.Models;

public class Case
{
    public string Label { get; set; } = default!;
    public IExpression Condition { get; set; } = new JavaScriptExpression("");
}