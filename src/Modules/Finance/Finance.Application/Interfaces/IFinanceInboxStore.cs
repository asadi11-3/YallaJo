using YallaJo.SharedKernel.Application.Abstractions.Data;

namespace Finance.Application.Interfaces;

/// <summary>
/// Finance module's marker interface for the SharedKernel inbox store.
/// Registered as <c>services.AddScoped&lt;IFinanceInboxStore, FinanceInboxStore&gt;()</c>.
/// Provides idempotency for cross-module integration event handlers.
/// </summary>
public interface IFinanceInboxStore : IInboxStore
{
}
