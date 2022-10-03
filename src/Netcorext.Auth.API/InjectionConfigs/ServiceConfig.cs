using Netcorext.Auth.API.Services.Client.Commands;
using Netcorext.Auth.API.Services.Client.Pipelines;
using Netcorext.Auth.API.Services.Role.Commands;
using Netcorext.Auth.API.Services.Role.Pipelines;
using Netcorext.Auth.API.Services.User.Commands;
using Netcorext.Auth.API.Services.User.Pipelines;
using Netcorext.Contracts;

namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class ServiceConfig
{
    public ServiceConfig(IServiceCollection services)
    {
        services.AddMediator()
                .AddPerformancePipeline()
                .AddValidatorPipeline()
                .AddRequestPipeline<ClientChangeNotifyPipeline, CreateClient, Result<long?>>()
                .AddRequestPipeline<ClientChangeNotifyPipeline, UpdateClient, Result>()
                .AddRequestPipeline<ClientChangeNotifyPipeline, DeleteClient, Result>()
                .AddRequestPipeline<UserChangeNotifyPipeline, CreateUser, Result<long?>>()
                .AddRequestPipeline<UserChangeNotifyPipeline, UpdateUser, Result>()
                .AddRequestPipeline<UserChangeNotifyPipeline, DeleteUser, Result>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, CreateRole, Result<IEnumerable<long>>>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, UpdateRole, Result>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, DeleteRole, Result>();
    }
}