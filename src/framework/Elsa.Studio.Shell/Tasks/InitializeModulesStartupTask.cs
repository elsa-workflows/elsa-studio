// using Elsa.Studio.Contracts;
//
// namespace Elsa.Studio.Shell.Tasks;
//
// /// <summary>
// /// Initializes the modules.
// /// </summary>
// public class InitializeModulesStartupTask : IStartupTask
// {
//     private readonly IFeatureService _featureService;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="InitializeModulesStartupTask"/> class.
//     /// </summary>
//     public InitializeModulesStartupTask(IFeatureService featureService)
//     {
//         _featureService = featureService;
//     }
//
//     /// <inheritdoc />
//     public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
//     {
//         await _featureService.InitializeFeaturesAsync(cancellationToken);
//     }
// }