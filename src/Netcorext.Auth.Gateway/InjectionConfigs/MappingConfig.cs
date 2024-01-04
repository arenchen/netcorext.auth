using Mapster;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class MappingConfig
{
    public MappingConfig()
    {
        TypeAdapterConfig.GlobalSettings.LoadProtobufConfig();
    }
}
