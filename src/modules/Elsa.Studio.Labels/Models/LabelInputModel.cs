using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Labels.Models;

/// <summary>
/// The inputmodel for a label.
/// </summary>
public class LabelInputModel
{
    public string Name { get; internal set; }
    public string Description { get; internal set; }
    public string Color { get; internal set; }
}
