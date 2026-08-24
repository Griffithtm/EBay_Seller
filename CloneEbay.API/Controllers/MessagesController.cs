using CloneEbay.API.Extensions;
using CloneEbay.API.Hubs;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public sealed class MessagesController(
    IMessageService messageService,
    IHubContext<MessageHub> hubContext) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<ActionResult<ApiEnvelope<ConversationListResponse>>> GetConversations(CancellationToken cancellationToken)
    {
        var result = await messageService.GetConversationsAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<ConversationListResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{conversationWith:int}")]
    public async Task<ActionResult<ApiEnvelope<PagedResult<MessageResponse>>>> GetHistory(
        int conversationWith, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 30, 
        CancellationToken cancellationToken = default)
    {
        var result = await messageService.GetMessagesAsync(User.GetRequiredUserId(), conversationWith, page, pageSize, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<MessageResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<MessageResponse>>> SendMessage(
        [FromBody] SendMessageRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await messageService.SendMessageAsync(User.GetRequiredUserId(), request, cancellationToken);
        
        // Push notification via SignalR
        await hubContext.Clients.Group($"user-{request.ReceiverId}").SendAsync("messageReceived", result, cancellationToken);
        
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<MessageResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiEnvelope<object>>> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        await messageService.MarkAsReadAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<object>.Ok(new { Read = true }, HttpContext.TraceIdentifier));
    }

    [HttpPut("conversations/{otherUserId:int}/read")]
    public async Task<ActionResult<ApiEnvelope<object>>> MarkConversationAsRead(int otherUserId, CancellationToken cancellationToken)
    {
        var updatedCount = await messageService.MarkConversationAsReadAsync(User.GetRequiredUserId(), otherUserId, cancellationToken);
        return Ok(ApiEnvelope<object>.Ok(new { Updated = updatedCount }, HttpContext.TraceIdentifier));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiEnvelope<object>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await messageService.GetUnreadCountAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<object>.Ok(new { Count = count }, HttpContext.TraceIdentifier));
    }
}
