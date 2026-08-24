using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Messages;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class MessageRepository(CloneEbayDbContext dbContext) : IMessageRepository
{
    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userMessages = dbContext.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId);

        var latestMessageIds = await userMessages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(group => group
                .OrderByDescending(message => message.Timestamp)
                .ThenByDescending(message => message.Id)
                .Select(message => message.Id)
                .First())
            .ToListAsync(cancellationToken);

        var conversations = await dbContext.Messages
            .AsNoTracking()
            .Where(message => latestMessageIds.Contains(message.Id))
            .Select(m => new
            {
                OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
                LastMessageContent = m.Content,
                LastMessageAt = m.Timestamp
            })
            .ToListAsync(cancellationToken);

        // Fetch unread counts separately for each conversation
        var unreadCounts = await dbContext.Messages
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .GroupBy(m => m.SenderId)
            .Select(g => new { SenderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.SenderId, v => v.Count, cancellationToken);

        // Fetch usernames
        var otherUserIds = conversations.Select(c => c.OtherUserId).Distinct().ToList();
        var users = await dbContext.Users
            .Where(u => otherUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var result = conversations.Select(c => new ConversationSummary(
            c.OtherUserId,
            users.TryGetValue(c.OtherUserId, out var username) ? username : "Unknown",
            c.LastMessageContent,
            c.LastMessageAt,
            unreadCounts.GetValueOrDefault(c.OtherUserId)
        )).OrderByDescending(c => c.LastMessageAt).ToList();

        return result;
    }

    public async Task<(IReadOnlyList<Message> Items, int TotalCount)> GetMessagesAsync(int userId, int otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) || 
                        (m.SenderId == otherUserId && m.ReceiverId == userId));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await dbContext.Messages.AddAsync(message, cancellationToken);
    }

    public async Task<Message?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<int> MarkConversationAsReadAsync(int receiverId, int senderUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .Where(m => m.ReceiverId == receiverId && m.SenderId == senderUserId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsRead, true), cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead, cancellationToken);
    }
}
