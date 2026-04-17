using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Application.Features.ServiceItems.Events; // 👈 مسار الأحداث
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Delete;

public sealed class DeleteServiceItemCommandHandler : IRequestHandler<DeleteServiceItemCommand>
{
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IContentPlacesUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher; // 👈 لنشر الحدث
    private readonly ILogger<DeleteServiceItemCommandHandler> _logger;

    public DeleteServiceItemCommandHandler(
        IServiceItemRepository serviceItemRepository,
        IContentPlacesUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<DeleteServiceItemCommandHandler> logger)
    {
        _serviceItemRepository = serviceItemRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(DeleteServiceItemCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Deleting Service Item {ServiceItemId}", request.Id);

        // 1. جلب الخدمة
        var serviceItem = await _serviceItemRepository.GetByIdAsync(request.Id, ct);

        if (serviceItem == null)
        {
            _logger.LogWarning("Service Item {ServiceItemId} not found for deletion", request.Id);
            // تطبيق قاعدة الـ Error Code حسب الـ PDF
            throw new Exception("ServiceItem.NotFound");
        }

        // 2. تطبيق الحذف الناعم (Soft Delete)
        serviceItem.SoftDelete();

        // 3. تحديث الكيان في الداتابيز بدلاً من حذفه نهائياً
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. تجهيز وإرسال حدث الحذف
        var deleteEvent = new ServiceItemDeletedIntegrationEvent(
            serviceItem.Id,
            serviceItem.BusinessId
        );

        await _publisher.Publish(deleteEvent, ct);

        _logger.LogInformation("Service Item {ServiceItemId} softly deleted & event published", request.Id);
    }
}
