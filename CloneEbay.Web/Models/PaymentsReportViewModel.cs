namespace CloneEbay.Web.Models;

public sealed class PaymentsReportViewModel
{
    public string Period { get; set; } = "thisMonth";
    public string Compared { get; set; } = "none";
    public PaymentReportResponseDto? Report { get; set; }
}
