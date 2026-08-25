using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Web.Models;

public sealed record SendMessageRequest(int ReceiverId, string Content, string? AttachmentUrl = null, int? RelatedProductId = null);

public sealed record MessageResponse(
    int Id, int SenderId, int ReceiverId, string Content, 
    string? AttachmentUrl, int? RelatedProductId, 
    DateTime Timestamp, bool IsRead);

public sealed record ConversationItem(
    int WithUserId, string WithUsername, string LastMessage,
    DateTime LastMessageAt, int UnreadCount);

public sealed record ConversationListResponse(IReadOnlyList<ConversationItem> Items);
public sealed record UnreadCountResponse(int Count);

// View Models for the Web layer
public sealed class InboxViewModel
{
    public IReadOnlyList<ConversationItem> Conversations { get; set; } = Array.Empty<ConversationItem>();
}

public sealed class ConversationViewModel
{
    public int OtherUserId { get; set; }
    public string OtherUsername { get; set; } = string.Empty;
    public IReadOnlyList<ConversationItem> Conversations { get; set; } = Array.Empty<ConversationItem>();
    public IReadOnlyList<MessageResponse> Messages { get; set; } = Array.Empty<MessageResponse>();
    public PaginationDto Pagination { get; set; } = new(1, 30, 0, 0);
}
