using AutoWash.Application.DTOs;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<PromoValidateResponse> ValidatePromoAsync(string code, int? customerId);
        Task<IEnumerable<Promotion>> GetPromotionsAsync();
        Task<Promotion> CreatePromotionAsync(CreatePromoRequest request);
        Task<Promotion> UpdatePromotionAsync(int id, UpdatePromoRequest request);
        Task<Promotion> TogglePromoActiveAsync(int id);
        Task<PromoDetailResponse> GetPromoUsageAsync(int id);
        Task<CustomerNotification> DispatchRfmActionAsync(RfmActionRequest request);
        Task<List<CustomerNotificationDto>> GetMyNotificationsAsync(int? customerId);
    }
}
