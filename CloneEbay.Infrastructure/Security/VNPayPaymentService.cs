using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CloneEbay.Infrastructure.Security;

public interface IVNPayPaymentService
{
    string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string returnUrl, string ipAddress = "127.0.0.1");
    VNPayPaymentResult VerifyPayment(IDictionary<string, string> queryParams);
}

public sealed record VNPayPaymentResult(bool Success, int OrderId, string TransactionNo, decimal Amount, string ResponseCode, string Message);

public sealed class VNPayPaymentService(IConfiguration configuration) : IVNPayPaymentService
{
    private readonly string _tmnCode = configuration["VNPay:TmnCode"] ?? "CGXZLS0Z";
    private readonly string _hashSecret = configuration["VNPay:HashSecret"] ?? "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";
    private readonly string _baseUrl = configuration["VNPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string returnUrl, string ipAddress = "127.0.0.1")
    {
        var vnpParams = new SortedList<string, string>(new VnPayCompare())
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", _tmnCode },
            { "vnp_Amount", ((long)(amount * 100)).ToString(CultureInfo.InvariantCulture) },
            { "vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", string.IsNullOrWhiteSpace(orderInfo) ? $"Thanh toan don hang {orderId}" : orderInfo.Replace("#", "") },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", $"{orderId}_{DateTime.UtcNow.Ticks}" }
        };

        var query = new StringBuilder();
        foreach (var (key, value) in vnpParams)
        {
            if (!string.IsNullOrEmpty(value))
            {
                query.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value)).Append('&');
            }
        }

        var queryString = query.ToString();
        var rawData = queryString.Remove(queryString.Length - 1, 1);
        var secureHash = HmacSha512(_hashSecret, rawData);

        return $"{_baseUrl}?{queryString}vnp_SecureHash={secureHash}";
    }

    public VNPayPaymentResult VerifyPayment(IDictionary<string, string> queryParams)
    {
        var responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var code) ? code : "99";
        var txnRef = queryParams.TryGetValue("vnp_TxnRef", out var txn) ? txn : "0";
        var transNo = queryParams.TryGetValue("vnp_TransactionNo", out var tn) ? tn : "0";
        var amountStr = queryParams.TryGetValue("vnp_Amount", out var am) ? am : "0";

        var orderId = 0;
        if (txnRef.Contains('_'))
        {
            int.TryParse(txnRef.Split('_')[0], out orderId);
        }
        else
        {
            int.TryParse(txnRef, out orderId);
        }

        decimal.TryParse(amountStr, out var rawAmount);
        var amount = rawAmount / 100m;

        var success = responseCode == "00";
        var msg = success ? "Giao dịch thanh toán VNPay thành công." : $"Giao dịch thất bại (Mã lỗi: {responseCode}).";

        return new VNPayPaymentResult(success, orderId, transNo, amount, responseCode, msg);
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        foreach (var b in hashValue)
        {
            hash.Append(b.ToString("x2"));
        }
        return hash.ToString();
    }

    public sealed class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
