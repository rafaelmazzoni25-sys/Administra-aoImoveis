namespace AdministraAoImoveis.Web.Domain.Enumerations;

public enum AvailabilityStatus
{
    Disponivel = 0,
    Reservado = 1,
    EmNegociacao = 2,
    Ocupado = 3,
    EmVistoriaEntrada = 4,
    EmVistoriaSaida = 5,
    EmManutencao = 6,
    Indisponivel = 7,
    AgendadoParaDisponibilizacao = 8
}
