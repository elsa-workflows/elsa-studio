using System.Text.Json;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps a Flowchart activity to an X6Graph.
/// </summary>
internal interface IFlowchartMapper
{
    /// <summary>
    /// Maps a Flowchart activity to an X6Graph.
    /// </summary>
    /// <param name="flowchartElement">The Flowchart activity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An X6Graph.</returns>
    Task<X6Graph> MapAsync(JsonElement flowchartElement, CancellationToken cancellationToken = default);
}