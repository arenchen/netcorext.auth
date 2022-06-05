using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Route;

public class RegisterRouteValidator : AbstractValidator<RegisterRoute>
{
    public RegisterRouteValidator()
    {
        RuleFor(t => t.Groups).NotEmpty();

        RuleForEach(t => t.Groups).ChildRules(c =>
                                              {
                                                  c.RuleFor(t => t.Name).NotEmpty();
                                                  c.RuleFor(t => t.BaseUrl).NotEmpty();
                                                  c.RuleFor(t => t.Routes).NotEmpty();

                                                  c.RuleForEach(t => t.Routes).ChildRules(c2 =>
                                                                                          {
                                                                                              c2.RuleFor(t => t.Protocol).NotEmpty();
                                                                                              c2.RuleFor(t => t.HttpMethod).NotEmpty();
                                                                                              c2.RuleFor(t => t.RelativePath).NotEmpty();

                                                                                              // c2.RuleFor(t => t.Template).NotEmpty();
                                                                                              c2.RuleFor(t => t.FunctionId).NotEmpty();

                                                                                              c2.RuleForEach(t => t.RouteValues).ChildRules(c3 =>
                                                                                                                                            {
                                                                                                                                                c3.RuleFor(t => t.Key).NotEmpty();
                                                                                                                                                c3.RuleFor(t => t.Value).NotEmpty();
                                                                                                                                            });
                                                                                          });
                                              });
    }
}