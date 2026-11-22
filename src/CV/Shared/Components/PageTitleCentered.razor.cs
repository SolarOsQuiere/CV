using Microsoft.AspNetCore.Components;

namespace CV.Shared.Components;

public partial class PageTitleCentered
{
    [Parameter] public string Title { get; set; } = "";
}
