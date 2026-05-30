namespace YallaJo.SharedKernel.Infrastructure.Data
{
    public interface IModuleDbInitializer
    {
        int Order { get; }

        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
