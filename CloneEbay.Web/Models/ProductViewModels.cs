using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CloneEbay.Web.Models;

public sealed class ProductFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề sản phẩm."), StringLength(255, MinimumLength = 3, ErrorMessage = "Tiêu đề phải từ 3 đến 255 ký tự."), Display(Name = "Tiêu đề sản phẩm")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả sản phẩm."), StringLength(4000, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 4.000 ký tự."), Display(Name = "Mô tả chi tiết")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "Giá phải lớn hơn 0."), Display(Name = "Giá bán / giá khởi điểm")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục."), Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "Hình thức đăng bán")]
    public bool IsAuction { get; set; }

    [Display(Name = "Thời điểm kết thúc đấu giá")]
    public DateTime? AuctionEndTime { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm."), Display(Name = "Số lượng tồn kho")]
    public int Quantity { get; set; }

    [ValidateNever]
    [Display(Name = "Hình ảnh sản phẩm")]
    public List<IFormFile>? Images { get; set; } = [];

    public IReadOnlyList<string> ExistingImages { get; set; } = [];
    public IReadOnlyList<CategoryDto> Categories { get; set; } = [];
}

public sealed class ProductIndexViewModel
{
    public IReadOnlyList<ProductDto> Products { get; set; } = [];
    public IReadOnlyList<CategoryDto> Categories { get; set; } = [];
    public PaginationDto Pagination { get; set; } = new(1, 20, 0, 0);
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int? CategoryId { get; set; }
    public int ActiveCount => Products.Count(product => product.Status == "Active");
    public int AttentionCount => Products.Count(product => product.Status is "OutOfStock" or "Hidden");
    public int AuctionCount => Products.Count(product => product.IsAuction);
}

public sealed class ProductDetailViewModel
{
    public ProductDto Product { get; set; } = null!;
    public BidListDto? Bids { get; set; }
    public decimal BidAmount { get; set; }
    public bool IsOwner { get; set; }
}

public sealed record InventoryUpdateViewModel(int ProductId, int Quantity);
