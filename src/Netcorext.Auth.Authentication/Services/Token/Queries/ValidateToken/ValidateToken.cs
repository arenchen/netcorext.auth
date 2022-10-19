using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token.Queries;

public class ValidateToken : IRequest<Result>
{
    public string Token { get; set; } = null!;
    public ValidationParameter? ValidationParameters { get; set; }

    public class ValidationParameter
    {
        public bool ValidateIssuer { get; set; }
        public bool ValidateAudience { get; set; }
        public bool ValidateLifetime { get; set; }
    }
}