using Mapster;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class MappingConfig
{
    public MappingConfig()
    {
        TypeAdapterConfig.GlobalSettings.LoadProtobufConfig();
    }
}