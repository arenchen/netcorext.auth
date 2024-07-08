using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Maintenance.Queries;

public class GetMaintain : IRequest<Result<IDictionary<string, Models.MaintainItem>>>
{ }
