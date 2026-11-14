
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.Bson;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Infrastructure.PostgreSql.Configurations;

public class DailyForecastConfiguration
    : IEntityTypeConfiguration<DailyForecast>
{
    public void Configure(EntityTypeBuilder<DailyForecast> builder)
    {
        builder.ToTable("daily_forecasts");

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

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ForecastDate)
            .HasColumnType("date");

        builder.HasIndex(x => new
        {
            x.Source,
            x.ForecastEmissionDate,
            x.ForecastDate
        }).IsUnique();
    }
}