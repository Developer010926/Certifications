using Certifications.Domain.Entities;
using Certifications.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Certifications.Infrastructure.Persistence.Configurations;

internal sealed class ProlongationConfiguration : IEntityTypeConfiguration<Prolongation>
{
    public void Configure(EntityTypeBuilder<Prolongation> builder)
    {
        builder.ToTable(
            "prolongations",
            table => table.HasCheckConstraint(
                "ck_prolongations_date_sequence",
                "(protocol_date IS NULL OR protocol_date >= certification_date)"
                + " AND (prolongation_send IS NULL"
                + " OR (protocol_date IS NOT NULL AND prolongation_send >= protocol_date))"
                + " AND (prolongation_returned IS NULL"
                + " OR (prolongation_send IS NOT NULL AND prolongation_returned >= prolongation_send))"));

        builder.HasKey(prolongation => prolongation.Id)
            .HasName("pk_prolongations");

        builder.Property(prolongation => prolongation.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(prolongation => prolongation.ContractId)
            .HasColumnName("contract_id");

        builder.Property(prolongation => prolongation.Assessor)
            .HasColumnName("assessor")
            .IsRequired();

        builder.Property(prolongation => prolongation.CertificationDate)
            .HasColumnName("certification_date")
            .HasColumnType("date");

        builder.Property(prolongation => prolongation.ProtocolDate)
            .HasColumnName("protocol_date")
            .HasColumnType("date");

        builder.Property(prolongation => prolongation.ProlongationSend)
            .HasColumnName("prolongation_send")
            .HasColumnType("date");

        builder.Property(prolongation => prolongation.ProlongationReturned)
            .HasColumnName("prolongation_returned")
            .HasColumnType("date");

        builder.HasIndex(
                prolongation => new
                {
                    prolongation.ContractId,
                    prolongation.CertificationDate
                })
            .IsDescending(false, true)
            .HasDatabaseName("ix_prolongations_contract_id_certification_date");

        builder.HasIndex(prolongation => prolongation.ContractId)
            .IsUnique()
            .HasFilter("prolongation_returned IS NULL")
            .HasDatabaseName("ux_prolongations_contract_id_in_progress");

        builder.Ignore(prolongation => prolongation.IsCompleted);

        builder.HasData(CriminalPoliceSeedData.Prolongations);
    }
}
