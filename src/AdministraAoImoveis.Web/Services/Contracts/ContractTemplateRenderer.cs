using System.Globalization;
using System.Net;
using System.Text;
using AdministraAoImoveis.Web.Domain.Entities;
using System.Linq;

namespace AdministraAoImoveis.Web.Services.Contracts;

public static class ContractTemplateRenderer
{
    public static string Render(
        Property property,
        Negotiation? negotiation = null,
        ContractTemplateData? data = null)
    {
        negotiation ??= property.Negociacoes
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefault();

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var proprietario = property.Proprietario;
        var interessado = negotiation?.Interessado;

        var valorAluguel = data?.ValorAluguel ?? negotiation?.ValorProposta;
        var valorSinal = data?.ValorSinal ?? negotiation?.ValorSinal;
        var encargos = data?.Encargos;
        var dataInicio = data?.DataInicio ?? negotiation?.CreatedAt;
        var dataFim = data?.DataFim;

        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset=\"utf-8\" /><title>Contrato de Locação</title></head><body>");
        sb.AppendLine($"<h1>Contrato de Locação - {WebUtility.HtmlEncode(property.CodigoInterno)}</h1>");
        sb.AppendLine($"<p>Gerado em {DateTime.UtcNow.ToLocalTime():dd/MM/yyyy HH:mm}</p>");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Proprietário</h2>");
        sb.AppendLine($"<p>{WebUtility.HtmlEncode(proprietario?.Nome ?? "Nome do proprietário")} - Documento: {WebUtility.HtmlEncode(proprietario?.Documento ?? "___________")}</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Interessado</h2>");
        sb.AppendLine($"<p>{WebUtility.HtmlEncode(interessado?.Nome ?? "Interessado")} - Documento: {WebUtility.HtmlEncode(interessado?.Documento ?? "___________")}</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Imóvel</h2>");
        sb.AppendLine($"<p><strong>Endereço:</strong> {WebUtility.HtmlEncode(property.Endereco)} - {WebUtility.HtmlEncode(property.Bairro)} - {WebUtility.HtmlEncode(property.Cidade)}/{WebUtility.HtmlEncode(property.Estado)}</p>");
        sb.AppendLine($"<p><strong>Características:</strong> Área {property.Area} m², {property.Quartos} quarto(s), {property.Banheiros} banheiro(s), {property.Vagas} vaga(s).</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Cláusulas financeiras</h2>");
        sb.AppendLine($"<p><strong>Valor do aluguel:</strong> {(valorAluguel.HasValue ? valorAluguel.Value.ToString("C", cultura) : "________")}</p>");
        sb.AppendLine($"<p><strong>Valor do sinal/caução:</strong> {(valorSinal.HasValue ? valorSinal.Value.ToString("C", cultura) : "________")}</p>");
        sb.AppendLine($"<p><strong>Encargos:</strong> {(encargos.HasValue ? encargos.Value.ToString("C", cultura) : "________")}</p>");
        sb.AppendLine($"<p><strong>Data prevista de início:</strong> {(dataInicio.HasValue ? dataInicio.Value.ToLocalTime().ToString("dd/MM/yyyy", cultura) : "____/____/____")}</p>");
        sb.AppendLine($"<p><strong>Data prevista de término:</strong> {(dataFim.HasValue ? dataFim.Value.ToLocalTime().ToString("dd/MM/yyyy", cultura) : "____/____/____")}</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Aceites presenciais</h2>");
        sb.AppendLine("<p>Espaço reservado para assinaturas físicas das partes envolvidas.</p>");
        sb.AppendLine("<p>__________________________________________</p>");
        sb.AppendLine("<p>__________________________________________</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}

public record ContractTemplateData(
    decimal? ValorAluguel,
    decimal? ValorSinal,
    decimal? Encargos,
    DateTime? DataInicio,
    DateTime? DataFim);
