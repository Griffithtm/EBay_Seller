using CloneEbay.Application.Common;

namespace CloneEbay.Application.Messages;

public interface IMessageService
{
    Task<ConversationListResponse> GetConversationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageResponse>> GetMessagesAsync(int userId, int conversationWith, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MessageResponse> SendMessageAsync(int senderId, SendMessageRequest request, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int messageId, int currentUserId, CancellationToken cancellationToken = default);
    Task<int> MarkConversationAsReadAsync(int currentUserId, int otherUserId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
}
