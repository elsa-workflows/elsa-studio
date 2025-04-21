using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Elsa.Labels.Entities;

namespace Elsa.Studio.Labels.Comparers;

public class LabelComparer : IEqualityComparer<Label>
{
    public bool Equals(Label? x, Label? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        return x?.Id == y?.Id;
    }

    public int GetHashCode([DisallowNull] Label obj)
    {
        return obj.Id.GetHashCode();
    }
}
