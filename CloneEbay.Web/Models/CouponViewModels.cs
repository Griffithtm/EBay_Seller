using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Web.Models;

public sealed class CouponIndexViewModel
{
    public IReadOnlyList<CouponSummaryDto> Coupons { get; set; } = [];
    public int ActiveCount => Coupons.Count(coupon => coupon.IsActive);
    public int InactiveCount => Coupons.Count(coupon => !coupon.IsActive);
}

public sealed class CouponFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại mã giảm giá."), Display(Name = "Loại mã giảm giá")]
    public int CouponTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nội bộ."), StringLength(100), Display(Name = "Tên nội bộ")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã giảm giá."), StringLength(50), Display(Name = "Mã giảm giá")]
    public string Code { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0."), Display(Name = "Giá trị giảm")]
    public decimal DiscountValue { get; set; }

    [Required, Display(Name = "Đơn vị giảm")]
    public string DiscountUnit { get; set; } = "Percent";

    [Range(0.01, double.MaxValue), Display(Name = "Giảm tối đa")]
    public decimal? MaxDiscount { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu."), Display(Name = "Ngày bắt đầu")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc."), Display(Name = "Ngày kết thúc")]
    public DateTime? EndDate { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Tổng lượt dùng tối đa")]
    public int? UsageLimit { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Lượt dùng tối đa mỗi người")]
    public int? UsagePerUser { get; set; }

    [Range(0, double.MaxValue), Display(Name = "Giá trị đơn hàng tối thiểu")]
    public decimal? MinimumOrderValue { get; set; }

    [Range(0, double.MaxValue), Display(Name = "Giá sản phẩm tối thiểu áp dụng")]
    public decimal? ApplicablePriceMin { get; set; }

    [Range(0, double.MaxValue), Display(Name = "Giá sản phẩm tối đa áp dụng")]
    public decimal? ApplicablePriceMax { get; set; }

    [Display(Name = "Giới hạn theo danh mục")]
    public int? CategoryId { get; set; }

    [Display(Name = "Giới hạn theo sản phẩm")]
    public int? ProductId { get; set; }

    [Display(Name = "Kích hoạt ngay sau khi lưu")]
    public bool IsActive { get; set; } = true;

    // Type-specific condition fields (shown/hidden client-side based on the selected coupon type).
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }
    public decimal? GetDiscountPercent { get; set; }
    public decimal? SaveEveryAmount { get; set; }
    public int? SaveEveryItems { get; set; }

    [StringLength(255), Display(Name = "Ghi chú cho người mua")]
    public string? ConditionDescription { get; set; }

    public string? RecaptchaToken { get; set; }

    public IReadOnlyList<CouponTypeDto> Types { get; set; } = [];
    public IReadOnlyList<CategoryDto> Categories { get; set; } = [];
    public IReadOnlyList<ProductDto> ProductOptions { get; set; } = [];
}
