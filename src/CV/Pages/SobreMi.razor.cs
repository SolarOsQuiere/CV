using CV.Shared.Constants;
using CV.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace CV.Pages;

public partial class SobreMi(IStringLocalizer<AboutRes> L, IStringLocalizer<CommonRes> Common)
{
    private IReadOnlyList<string> Images => AssetPaths.Gallery;

    private string? selectedImage;

    private void OpenImage(string img) => selectedImage = img;

    private void CloseImage() => selectedImage = null;
}
