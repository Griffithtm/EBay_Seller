using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Roles = "Seller, Buyer")]
public sealed class MessagesController(SellerApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = await apiClient.GetConversationsAsync();
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error?.Message ?? "Failed to load conversations.";
            return View(new InboxViewModel());
        }

        var viewModel = new InboxViewModel
        {
            Conversations = result.Data?.Items ?? Array.Empty<ConversationItem>()
        };

        return View(viewModel);
    }

    [HttpGet("Messages/Conversation/{userId:int}")]
    public async Task<IActionResult> Conversation(int userId, int page = 1)
    {
        var result = await apiClient.GetMessagesAsync(userId, page, 50);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = "Failed to load messages.";
            return RedirectToAction(nameof(Index));
        }

        // Mark as read when opened
        await apiClient.MarkConversationAsReadAsync(userId);

        // We need the username. Let's find it from conversations list for now, or the API should return it.
        // As a quick fix, we fetch conversations to find the username.
        var convs = await apiClient.GetConversationsAsync();
        var username = convs.Data?.Items.FirstOrDefault(c => c.WithUserId == userId)?.WithUsername ?? $"User {userId}";

        var viewModel = new ConversationViewModel
        {
            OtherUserId = userId,
            OtherUsername = username,
            Messages = result.Data?.Items ?? Array.Empty<MessageResponse>(),
            Pagination = result.Data?.Pagination ?? new PaginationDto(1, 50, 0, 0)
        };

        return View(viewModel);
    }

    [HttpPost("Messages/Send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var result = await apiClient.SendMessageAsync(request);
        if (!result.Success)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    [HttpGet("Messages/UnreadCount")]
    public async Task<IActionResult> UnreadCount()
    {
        var result = await apiClient.GetUnreadCountAsync();
        if (!result.Success) return Ok(new { count = 0 });
        return Ok(new { count = result.Data?.Count ?? 0 });
    }
}
