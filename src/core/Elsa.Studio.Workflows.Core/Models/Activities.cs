using System.Collections.ObjectModel;
using Elsa.Api.Client.Activities;
using Elsa.Studio.Models;

namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// A union of an activity and a collection of activities.
/// </summary>
public class Activities : Union<Activity, Collection<Activity>>
{
    private Activities(Activity value) : base(value) { }
    private Activities(Collection<Activity> value) : base(value) { }
    public static implicit operator Activities(Activity value) => new(value);
    public static implicit operator Activities(Collection<Activity> value) => new(value);
}