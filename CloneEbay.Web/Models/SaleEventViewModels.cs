using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Web.Models;

public sealed class SaleEventIndexViewModel
{
    public IReadOnlyList<SaleEventSummaryDto> Events { get; set; } = [];
}

public sealed class SaleEventFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sự kiện."), StringLength(90, ErrorMessage = "Tên tối đa 90 ký tự."), Display(Name = "Tên sự kiện")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự."), Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required, Display(Name = "Hình thức")]
    public string Mode { get; set; } = "DiscountAndSaleEvent";

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu."), Display(Name = "Ngày bắt đầu")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc."), Display(Name = "Ngày kết thúc")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Miễn phí vận chuyển trong thời gian sự kiện")]
    public bool OfferFreeShipping { get; set; }

    [Display(Name = "Bao gồm cả sản phẩm đã bỏ qua trước đó")]
    public bool IncludeSkippedItems { get; set; }

    [Display(Name = "Khóa việc tăng giá trong thời gian sự kiện")]
    public bool BlockPriceIncreaseRevisions { get; set; }

    [Range(1, 90, ErrorMessage = "Phần trăm nổi bật phải từ 1 đến 90."), Display(Name = "Phần trăm hiển thị nổi bật")]
    public decimal? HighlightPercentage { get; set; }

    /// <summary>
    /// JSON array of tiers built client-side by sale-events form JS, e.g.
    /// [{"priority":1,"discountType":"Percent","discountValue":10,"label":"Flash sale","productIds":[1,2]}].
    /// Parsed server-side in the controller before calling the API.
    /// </summary>
    public string? TiersJson { get; set; }

    public IReadOnlyList<EligibleProductDto> EligibleProducts { get; set; } = [];
}
