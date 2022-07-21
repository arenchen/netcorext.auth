using Mapster;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class MappingConfig
{
    public MappingConfig()
    {
        TypeAdapterConfig.GlobalSettings.LoadProtobufConfig();
    }
}