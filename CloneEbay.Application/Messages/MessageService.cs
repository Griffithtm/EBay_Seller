using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Messages;

public sealed class MessageService(
    IMessageRepository messageRepository,
    IUserRepository userRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IMessageService
{
    public async Task<ConversationListResponse> GetConversationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var summaries = await messageRepository.GetConversationsAsync(userId, cancellationToken);
        var items = summaries.Select(s => new ConversationItem(
            s.OtherUserId, s.OtherUsername, s.LastMessageContent, s.LastMessageAt, s.UnreadCount)).ToList();
            
        return new ConversationListResponse(items);
    }

    public async Task<PagedResult<MessageResponse>> GetMessagesAsync(int userId, int conversationWith, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 30;

        var (items, totalCount) = await messageRepository.GetMessagesAsync(userId, conversationWith, page, pageSize, cancellationToken);
        
        var responses = items.Select(m => new MessageResponse(
            m.Id, m.SenderId, m.ReceiverId, m.Content, m.AttachmentUrl, m.RelatedProductId, m.Timestamp, m.IsRead)).ToList();

        return new PagedResult<MessageResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<MessageResponse> SendMessageAsync(int senderId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl) && request.RelatedProductId == null)
            throw new AppProblemException(422, "EMPTY_CONTENT", "Message content cannot be empty.");
            
        if (request.Content.Length > 2000)
            throw new AppProblemException(422, "CONTENT_TOO_LONG", "Message content exceeds 2000 characters.");

        var receiver = await userRepository.GetByIdAsync(request.ReceiverId, cancellationToken);
        if (receiver == null || !receiver.IsActive)
            throw new AppProblemException(404, "USER_NOT_FOUND", "Receiver not found or inactive.");

        if (request.RelatedProductId.HasValue)
        {
            var product = await productRepository.GetByIdAsync(request.RelatedProductId.Value, false, cancellationToken);
            if (product == null)
                throw new AppProblemException(404, "PRODUCT_NOT_FOUND", "Related product not found.");
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Content = request.Content ?? "",
            AttachmentUrl = request.AttachmentUrl,
            RelatedProductId = request.RelatedProductId,
            Timestamp = clock.UtcNow,
            IsRead = false
        };

        await messageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse(message.Id, message.SenderId, message.ReceiverId, message.Content, message.AttachmentUrl, message.RelatedProductId, message.Timestamp, message.IsRead);
    }

    public async Task MarkAsReadAsync(int messageId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var message = await messageRepository.GetByIdAsync(messageId, cancellationToken)
            ?? throw new AppProblemException(404, "MESSAGE_NOT_FOUND", "Message not found.");

        if (message.ReceiverId != currentUserId)
            throw new AppProblemException(403, "NOT_RECEIVER", "Only the receiver can mark a message as read.");

        if (!message.IsRead)
        {
            message.IsRead = true;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> MarkConversationAsReadAsync(int currentUserId, int otherUserId, CancellationToken cancellationToken = default)
    {
        var updatedCount = await messageRepository.MarkConversationAsReadAsync(currentUserId, otherUserId, cancellationToken);
        return updatedCount;
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await messageRepository.GetUnreadCountAsync(userId, cancellationToken);
    }
}
