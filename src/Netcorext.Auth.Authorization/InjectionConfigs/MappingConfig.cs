using Mapster;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class MappingConfig
{
    public MappingConfig()
    {
        TypeAdapterConfig.GlobalSettings.LoadProtobufConfig();
    }
}