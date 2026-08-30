using Certifications.Application.Abstractions;
using Certifications.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Certifications.Infrastructure.Time;

internal sealed class BusinessClock : IBusinessClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public BusinessClock(
        TimeProvider timeProvider,
        IOptions<BusinessOptions> options)
    {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    }

    public DateOnly Today
    {
        get
        {
            var local = TimeZoneInfo.ConvertTime(
                _timeProvider.GetUtcNow(),
                _timeZone);
            return DateOnly.FromDateTime(local.DateTime);
        }
    }
}
