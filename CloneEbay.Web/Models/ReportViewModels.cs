using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Web.Models;

public sealed class ReportsIndexViewModel
{
    public IReadOnlyList<ReportDownloadDto> Downloads { get; set; } = [];
    public IReadOnlyList<ReportScheduleDto> Schedules { get; set; } = [];
    public CreateReportDownloadViewModel DownloadForm { get; set; } = new();
    public CreateReportScheduleViewModel ScheduleForm { get; set; } = new();
}

public sealed class CreateReportDownloadViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn nguồn báo cáo."), Display(Name = "Nguồn")]
    public string Source { get; set; } = "listings";

    [Required(ErrorMessage = "Vui lòng chọn loại báo cáo."), Display(Name = "Loại")]
    public string Type { get; set; } = "all-active";

    [Display(Name = "Từ ngày")]
    public DateTime? RangeStartUtc { get; set; }

    [Display(Name = "Đến ngày")]
    public DateTime? RangeEndUtc { get; set; }
}

public sealed class CreateReportScheduleViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn nguồn báo cáo."), Display(Name = "Nguồn")]
    public string Source { get; set; } = "listings";

    [Required(ErrorMessage = "Vui lòng chọn loại báo cáo."), Display(Name = "Loại")]
    public string Type { get; set; } = "all-active";

    [Required(ErrorMessage = "Vui lòng chọn tần suất."), Display(Name = "Tần suất")]
    public string Frequency { get; set; } = "Daily";

    [EmailAddress(ErrorMessage = "Email không hợp lệ."), Display(Name = "Email nhận báo cáo (tuỳ chọn)")]
    public string? DeliveryEmail { get; set; }

    [Display(Name = "Ngày kết thúc (tuỳ chọn)")]
    public DateTime? EndDateUtc { get; set; }
}
