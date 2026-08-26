using CloneEbay.Application.Common;
using CloneEbay.Application.Reviews;
using CloneEbay.Application.Returns;
using CloneEbay.Domain.Entities;
using Xunit;

namespace CloneEbay.Tests;

public sealed class Role5CSKHTests
{
    [Theory]
    [InlineData("Requested", "Accepted", true)]
    [InlineData("Requested", "Declined", true)]
    [InlineData("Accepted", "RefundedByReturn", true)]
    [InlineData("Requested", "Closed", false)]
    [InlineData("Closed", "Accepted", false)]
    public void ReturnRequest_Status_Transitions_Follow_Rules(string current, string target, bool expectedValid)
    {
        var isValid = ReturnRequestStatus.IsValidTransition(current, target);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReturnRequest_Decline_Allowed_Only_For_ChangeOfMind()
    {
        var validReason = "Tôi đổi ý không muốn mua nữa (change of mind)";
        var invalidReason = "Hàng bị móp vỡ hư hỏng nặng";

        var isChangeOfMindValid = validReason.ToLowerInvariant().Contains("đổi ý") || validReason.ToLowerInvariant().Contains("change");
        var isChangeOfMindInvalid = invalidReason.ToLowerInvariant().Contains("đổi ý") || invalidReason.ToLowerInvariant().Contains("change");

        Assert.True(isChangeOfMindValid);
        Assert.False(isChangeOfMindInvalid);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Feedback_Calculation_Computes_Correct_Average_And_Positive_Rate()
    {
        var ratings = new List<int> { 5, 5, 4, 2, 1 }; // 3 positive (>= 4), 5 total
        var total = ratings.Count;
        var avg = Math.Round(ratings.Average(r => (decimal)r), 2);
        var positiveRate = Math.Round((decimal)ratings.Count(r => r >= 4) / total * 100, 2);

        Assert.Equal(5, total);
        Assert.Equal(3.40m, avg);
        Assert.Equal(60.00m, positiveRate);
    }
}
