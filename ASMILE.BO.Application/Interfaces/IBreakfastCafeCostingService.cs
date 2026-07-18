namespace ASMILE.BO.Application.Interfaces;

public interface IBreakfastCafeCostingService
{
    Task ConsumeRecipeAsync(Guid orderId, string? postedBy = null, CancellationToken cancellationToken = default);
}
