using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IEmailDispatchService
{
    Task<EmailDispatchResultDto> QueueAsync(EmailDispatchRequestDto request, CancellationToken cancellationToken = default);
    Task<int> DispatchPendingAsync(int batchSize = 20, CancellationToken cancellationToken = default);
}
