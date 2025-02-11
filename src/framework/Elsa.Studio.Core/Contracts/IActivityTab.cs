using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Contracts;

public interface IActivityTab
{
    string Title { get; }

    Func<IDictionary<string, object?>, RenderFragment> Render { get; }
}
