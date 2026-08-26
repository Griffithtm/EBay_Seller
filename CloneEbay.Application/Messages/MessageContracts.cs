namespace CloneEbay.Application.Messages;

// --- Requests ---
public sealed record SendMessageRequest(int ReceiverId, string Content, string? AttachmentUrl = null, int? RelatedProductId = null);

// --- Responses ---
public sealed record MessageResponse(
    int Id, int SenderId, int ReceiverId, string Content, 
    string? AttachmentUrl, int? RelatedProductId, 
    DateTime Timestamp, bool IsRead);

public sealed record ConversationItem(
    int WithUserId, string WithUsername, string LastMessage,
    DateTime LastMessageAt, int UnreadCount);

public sealed record ConversationListResponse(IReadOnlyList<ConversationItem> Items);

// --- Internal projection ---
public sealed record ConversationSummary(
    int OtherUserId, string OtherUsername, string LastMessageContent,
    DateTime LastMessageAt, int UnreadCount);
