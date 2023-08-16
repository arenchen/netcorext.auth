using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Client.Commands;
using Netcorext.Auth.API.Services.Client.Pipelines;
using Netcorext.Auth.API.Services.Permission.Commands;
using Netcorext.Auth.API.Services.Permission.Pipelines;
using Netcorext.Auth.API.Services.Role.Commands;
using Netcorext.Auth.API.Services.Role.Pipelines;
using Netcorext.Auth.API.Services.User.Commands;
using Netcorext.Auth.API.Services.User.Pipelines;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;

namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class ServiceConfig
{
    public ServiceConfig(IServiceCollection services)
    {
        services.AddMediator()
                .AddRedisQueuing((provider, options) =>
                                 {
                                     var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                     options.ConnectionString = cfg.Connections.Redis.GetDefault().Connection;
                                 })
                .AddPerformancePipeline()
                .AddValidatorPipeline()
                .AddRequestPipeline<ClientChangeNotifyPipeline, CreateClient, Result<long?>>()
                .AddRequestPipeline<ClientChangeNotifyPipeline, UpdateClient, Result>()
                .AddRequestPipeline<ClientChangeNotifyPipeline, DeleteClient, Result>()
                .AddRequestPipeline<UserChangeNotifyPipeline, CreateUser, Result<long?>>()
                .AddRequestPipeline<UserChangeNotifyPipeline, UpdateUser, Result>()
                .AddRequestPipeline<UserChangeNotifyPipeline, DeleteUser, Result>()
                .AddRequestPipeline<PermissionChangeNotifyPipeline, CreatePermission, Result<IEnumerable<long>>>()
                .AddRequestPipeline<PermissionChangeNotifyPipeline, UpdatePermission, Result>()
                .AddRequestPipeline<PermissionChangeNotifyPipeline, DeletePermission, Result>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, CreateRole, Result<IEnumerable<long>>>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, UpdateRole, Result>()
                .AddRequestPipeline<RoleChangeNotifyPipeline, DeleteRole, Result>();
    }
}