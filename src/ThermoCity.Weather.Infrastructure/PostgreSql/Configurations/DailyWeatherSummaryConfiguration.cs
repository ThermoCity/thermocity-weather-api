
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.Bson;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Infrastructure.PostgreSql.Configurations;

public class DailyWeatherSummaryConfiguration
    : IEntityTypeConfiguration<DailyWeatherSummary>
{
    public void Configure(EntityTypeBuilder<DailyWeatherSummary> builder)
    {
        builder.ToTable("daily_weather_summaries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.ToString(),
                value => ObjectId.Parse(value))
            .HasMaxLength(24)
            .ValueGeneratedNever();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.ReferenceDate)
            .HasColumnType("date");

        builder.HasIndex(x => new { x.Source, x.ReferenceDate })
            .IsUnique();
    }
}