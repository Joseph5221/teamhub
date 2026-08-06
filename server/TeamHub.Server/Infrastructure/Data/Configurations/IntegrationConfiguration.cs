using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHub.Server.Domain.Entities;

namespace TeamHub.Server.Infrastructure.Data.Configurations;

public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.HasOne(i => i.Team)
            .WithMany(t => t.Integrations)
            .HasForeignKey(i => i.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
