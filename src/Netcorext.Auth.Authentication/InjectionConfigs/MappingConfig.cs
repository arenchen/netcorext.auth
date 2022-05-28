using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Mapster;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class MappingConfig
{
    public MappingConfig()
    {
        TypeAdapterConfig.GlobalSettings.Default
                         .UseDestinationValue(t => t.SetterModifier == AccessModifier.None &&
                                                   t.Type.IsGenericType &&
                                                   t.Type.GetGenericTypeDefinition() == typeof(RepeatedField<>));

        TypeAdapterConfig<Timestamp?, DateTimeOffset?>
           .ForType()
           .MapWith(t => t == null ? null : t.ToDateTimeOffset());

        TypeAdapterConfig<DateTimeOffset?, Timestamp?>
           .ForType()
           .MapWith(t => t.HasValue ? Timestamp.FromDateTimeOffset(t.Value) : null);

        TypeAdapterConfig<Duration?, TimeSpan?>
           .ForType()
           .MapWith(t => t == null ? null : t.ToTimeSpan());

        TypeAdapterConfig<TimeSpan?, Duration?>
           .ForType()
           .MapWith(t => t.HasValue ? Duration.FromTimeSpan(t.Value) : null);
    }
}