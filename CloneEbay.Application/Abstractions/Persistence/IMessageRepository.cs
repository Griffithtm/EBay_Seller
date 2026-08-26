using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IMessageRepository
{
    Task<IReadOnlyList<CloneEbay.Application.Messages.ConversationSummary>> GetConversationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Message> Items, int TotalCount)> GetMessagesAsync(int userId, int otherUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task<Message?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> MarkConversationAsReadAsync(int receiverId, int senderUserId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
}
