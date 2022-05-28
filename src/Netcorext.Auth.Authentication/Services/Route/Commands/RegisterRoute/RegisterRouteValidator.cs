using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Route;

public class RegisterRouteValidator : AbstractValidator<RegisterRoute>
{
    public RegisterRouteValidator()
    {
        RuleForEach(t => t.Routes)
           .NotEmpty()
           .ChildRules(c =>
                       {
                           c.RuleFor(t => t.Group).NotEmpty();
                           c.RuleFor(t => t.Protocol).NotEmpty();
                           c.RuleFor(t => t.HttpMethod).NotEmpty();
                           c.RuleFor(t => t.BaseUrl).NotEmpty();
                           c.RuleFor(t => t.RelativePath).NotEmpty();
                           // c.RuleFor(t => t.Template).NotEmpty();
                           c.RuleFor(t => t.FunctionId).NotEmpty();
                           c.RuleForEach(t => t.RouteValues).ChildRules(c2 =>
                                                                        {
                                                                            c2.RuleFor(t2 => t2.Key).NotEmpty();
                                                                            c2.RuleFor(t2 => t2.Value).NotEmpty();
                                                                        });
                       });
    }
}