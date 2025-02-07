﻿namespace Elsa.Studio.Models;

public record DataPanelItem(string? Text = default, string? Link = default, Func<Task>? OnClick = default);