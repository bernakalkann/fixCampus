using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FixCampus.Web.Data.Migrations;

[DbContext(typeof(FixCampusDbContext))]
public class FixCampusDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
        => ProfileEducationModel.Build(modelBuilder);
}
