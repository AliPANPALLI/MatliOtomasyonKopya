using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Linq;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace MatliOtomasyon.Shared.Web.Security
{
    public static class CustomTokenAuth
    {
        public static void AddCustomTokenAuth(this IServiceCollection services, CustomTokenOption tokenOptions)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    ValidIssuer = tokenOptions.Issuer,
                    ValidAudiences = tokenOptions.Audience,
                    ValidAudience = tokenOptions.Audience[0],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecurityKey)),

                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                // 🔑 SignalR WebSocket: JWT querystring’ten gelir -> burada yakala
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var path = ctx.HttpContext.Request.Path;

                        // SignalR hub
                        var accessToken = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/kumesRaporHub"))
                        {
                            ctx.Token = accessToken;
                            return Task.CompletedTask;
                        }

                        // Ocelot WebSocket proxy: tarayıcı WebSocket API'si el sıkışmada
                        // custom header göndermeyi desteklemediği için token,
                        // Sec-WebSocket-Protocol alt-protokolü ile taşınır.
                        // İstemci: new WebSocket(url, ["Bearer", jwt]) -> header: "Bearer, <jwt>"
                        if (path.StartsWithSegments("/ws"))
                        {
                            var protocolHeader = ctx.HttpContext.Request.Headers["Sec-WebSocket-Protocol"].ToString();
                            if (!string.IsNullOrEmpty(protocolHeader))
                            {
                                var parts = protocolHeader.Split(',').Select(p => p.Trim()).ToArray();
                                if (parts.Length >= 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                                {
                                    ctx.Token = parts[1];
                                }
                                else if (parts.Length == 1 && !string.IsNullOrEmpty(parts[0]))
                                {
                                    // Sadece token gönderilmişse (şema öneki olmadan) da kabul et
                                    ctx.Token = parts[0];
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine("JWT auth failed: " + ctx.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
