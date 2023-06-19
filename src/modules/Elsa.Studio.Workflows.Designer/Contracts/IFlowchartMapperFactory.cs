namespace Elsa.Studio.Workflows.Designer.Contracts;

internal interface IFlowchartMapperFactory
{
    Task<IFlowchartMapper> CreateAsync(CancellationToken cancellationToken = default);
}