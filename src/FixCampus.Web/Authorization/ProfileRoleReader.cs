using FixCampus.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Authorization;

public interface IProfileRoleReader
{
    Task<string?> ReadAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class ProfileRoleReader(FixCampusDbContext db) : IProfileRoleReader
{
    public Task<string?> ReadAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Profiller.AsNoTracking()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.Rol)
            .SingleOrDefaultAsync(cancellationToken);
}
