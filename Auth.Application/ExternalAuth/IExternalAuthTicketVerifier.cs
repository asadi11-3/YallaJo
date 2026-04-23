using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.ExternalAuth;

public interface IExternalAuthTicketVerifier
{
    Result<ExternalAuthTicket> Verify(string ticket);
}
