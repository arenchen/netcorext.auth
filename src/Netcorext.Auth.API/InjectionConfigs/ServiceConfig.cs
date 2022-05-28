using Netcorext.Auth.API.Services.Client;
using Netcorext.Auth.API.Services.Client.Pipelines;
using Netcorext.Auth.API.Services.Role;
using Netcorext.Auth.API.Services.Role.Pipelines;
using Netcorext.Auth.API.Services.User;
using Netcorext.Auth.API.Services.User.Pipelines;
using Netcorext.Contracts;
using Netcorext.Extensions.DependencyInjection;

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
                .AddRequestPipeline<RoleChangeNotifyPipeline, CreateRole, Result<long?>>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, UpdateRole, Result>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, DeleteRole, Result>();
    }
}