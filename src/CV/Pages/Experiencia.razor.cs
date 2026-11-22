using CV.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace CV.Pages;

public partial class Experiencia(IStringLocalizer<ExperienceRes> L, IStringLocalizer<CommonRes> Common)
{
    private string TotalYearsMonthsFormatted(DateTime fechaInicio, DateTime fechaFin)
    {
        var totalMeses = ((fechaFin.Year - fechaInicio.Year) * 12) + fechaFin.Month - fechaInicio.Month;
        var anios = totalMeses / 12;
        var meses = totalMeses % 12;

        var formattedString = $"{anios} {Common[anios == 1 ? "year" : "years"]}";
        formattedString += $" {meses} {Common[meses == 1 ? "mes" : "meses"]}";

        return formattedString;
    }
}
