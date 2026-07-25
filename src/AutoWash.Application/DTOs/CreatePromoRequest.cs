using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs
{
    public class CreatePromoRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã khuyến mãi không được để trống")]
        public string PromoCode { get; set; } = string.Empty;

        public int? MinTierID { get; set; }

        [Required(ErrorMessage = "Loại giảm giá không được để trống")]
        [RegularExpression("^(Percentage|Fixed_Amount)$", ErrorMessage = "Loại giảm giá phải là 'Percentage' hoặc 'Fixed_Amount'")]
        public string DiscountType { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal DiscountValue { get; set; }

        public int? MaxUsage { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Giá trị đơn hàng tối thiểu không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal MinOrderValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }
    }

    public class UpdatePromoRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã khuyến mãi không được để trống")]
        public string PromoCode { get; set; } = string.Empty;

        public int? MinTierID { get; set; }

        [Required(ErrorMessage = "Loại giảm giá không được để trống")]
        [RegularExpression("^(Percentage|Fixed_Amount)$", ErrorMessage = "Loại giảm giá phải là 'Percentage' hoặc 'Fixed_Amount'")]
        public string DiscountType { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal DiscountValue { get; set; }

        public int? MaxUsage { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Giá trị đơn hàng tối thiểu không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal MinOrderValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }
    }
}
