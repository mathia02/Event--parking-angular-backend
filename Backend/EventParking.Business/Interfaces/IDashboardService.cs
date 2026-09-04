using EventParking.Models.DTOs.Dashboard;

namespace EventParking.Business.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardDto>
        GetAdminDashboardAsync();

    Task<CustomerDashboardDto>
        GetCustomerDashboardAsync(
            int customerId);
}