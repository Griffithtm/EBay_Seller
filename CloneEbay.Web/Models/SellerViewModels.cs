using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Web.Models;

public sealed class SellerApplyViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn loại người bán."), Display(Name = "Loại người bán")]
    public string SellerType { get; set; } = "Individual";

    [Required(ErrorMessage = "Vui lòng nhập tên pháp lý."), StringLength(150, MinimumLength = 2, ErrorMessage = "Tên pháp lý phải từ 2 đến 150 ký tự."), Display(Name = "Họ tên / tên doanh nghiệp")]
    public string LegalName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), StringLength(20, MinimumLength = 8, ErrorMessage = "Số điện thoại phải từ 8 đến 20 ký tự."), Display(Name = "Số điện thoại")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng."), StringLength(100, MinimumLength = 3, ErrorMessage = "Tên cửa hàng phải từ 3 đến 100 ký tự."), Display(Name = "Tên cửa hàng dự kiến")]
    public string StoreName { get; set; } = string.Empty;
}

public sealed class StoreProfileViewModel
{
    public int SellerId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng."), StringLength(100, MinimumLength = 3, ErrorMessage = "Tên cửa hàng phải từ 3 đến 100 ký tự."), Display(Name = "Tên cửa hàng")]
    public string StoreName { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2.000 ký tự."), Display(Name = "Mô tả cửa hàng")]
    public string? Description { get; set; }

    public string? CurrentBannerImageUrl { get; set; }

    [Display(Name = "Ảnh bìa cửa hàng")]
    public IFormFile? Banner { get; set; }
}
