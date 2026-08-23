using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Web.Models;

public sealed class CouponFormViewModel
{
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm áp dụng mã."), Display(Name = "Sản phẩm áp dụng")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã giảm giá."), StringLength(50, MinimumLength = 3, ErrorMessage = "Mã phải từ 3 đến 50 ký tự."), Display(Name = "Mã giảm giá")]
    public string Code { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100", ErrorMessage = "Phần trăm giảm giá phải từ 0.01 đến 100."), Display(Name = "Phần trăm giảm giá (%)")]
    public decimal DiscountPercent { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu."), Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc."), Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

    [Range(1, int.MaxValue, ErrorMessage = "Số lượt sử dụng tối đa phải từ 1 trở lên."), Display(Name = "Số lượt sử dụng tối đa")]
    public int MaxUsage { get; set; } = 100;

    public string? RecaptchaToken { get; set; }

    public IReadOnlyList<ProductDto> ProductOptions { get; set; } = [];
}

public sealed class CouponIndexViewModel
{
    public IReadOnlyList<CouponDto> Coupons { get; set; } = [];
    public IReadOnlyList<ProductDto> Products { get; set; } = [];
    public int? ProductId { get; set; }
    public string? Status { get; set; }
    public int ActiveCount => Coupons.Count(coupon => coupon.Status == "Active");
    public int ScheduledCount => Coupons.Count(coupon => coupon.Status == "Scheduled");
    public int ExpiredCount => Coupons.Count(coupon => coupon.Status is "Expired" or "UsedUp");
}
