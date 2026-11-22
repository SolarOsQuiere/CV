using CV.Shared.Resources;
using CV.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace CV.Pages;

public partial class Inicio(IStringLocalizer<HeroRes> L, IStringLocalizer<CommonRes> Common)
{
    /// <summary>Muestra los botones de accion (PDF / contacto). Desactivado en el documento imprimible.</summary>
    [Parameter] public bool ShowActions { get; set; }

    private static readonly string[] Stack = ["C#", ".NET", "Blazor", "SQL", "Docker", "Jenkins", "SonarQube"];

    /// <summary>Suma de todos los periodos de la seccion de experiencia, p. ej. "5 años 5 meses".</summary>
    private string ExperienciaTotal
    {
        get
        {
            var total = PeriodosExperiencia.TotalMeses(DateTime.Today);
            var anios = total / 12;
            var meses = total % 12;

            var texto = $"{anios} {Common[anios == 1 ? "year" : "years"]}";
            if (meses > 0)
            {
                texto += $" {meses} {Common[meses == 1 ? "mes" : "meses"]}";
            }
            return texto;
        }
    }
}
