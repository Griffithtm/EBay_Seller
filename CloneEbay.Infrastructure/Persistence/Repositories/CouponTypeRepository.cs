using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class CouponTypeRepository(CloneEbayDbContext dbContext) : ICouponTypeRepository
{
    public Task<CouponType?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.CouponTypes.AsNoTracking().SingleOrDefaultAsync(couponType => couponType.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CouponType>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CouponTypes.AsNoTracking().OrderBy(couponType => couponType.Id).ToListAsync(cancellationToken);
}
