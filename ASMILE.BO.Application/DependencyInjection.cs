using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IItemAppService, ItemAppService>();
        services.AddScoped<ICustomerAppService, CustomerAppService>();
        services.AddScoped<ISupplierAppService, SupplierAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();
        services.AddScoped<IDepartmentAppService, DepartmentAppService>();
        services.AddScoped<IPersonAppService, PersonAppService>();
        services.AddScoped<IInventoryAppService, InventoryAppService>();
        services.AddScoped<IVirtualExplorerService, VirtualExplorerService>();
        services.AddScoped<IBusinessDocumentService, BusinessDocumentService>();
        return services;
    }
}

