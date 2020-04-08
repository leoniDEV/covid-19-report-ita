using System;
using System.Data;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Covid19Report.Ita.Api.Infrastructure
{
    public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly SqlConnection sqlConnection;
        public BasicAuthHandler(DbConnection connection, IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock systemClock)
            : base(options, logger, encoder, systemClock)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            sqlConnection = (SqlConnection)connection;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            (string service, string password) auth;

            if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out var authHeader))
            {
                return AuthenticateResult.Fail("You need to authenticate!");
            }

            try
            {
                byte[] credByets = Convert.FromBase64String(authHeader.Parameter);
                string[] credential = Encoding.UTF8.GetString(credByets).Split(new[] { ':' }, 2);
                auth.service = credential[0];
                auth.password = credential[1];
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Header");
            }

            using var cmd = sqlConnection.CreateCommand();
            var param = cmd.CreateParameter();
            param.ParameterName = @"@name";
            param.SqlDbType = SqlDbType.NVarChar;
            param.Direction = ParameterDirection.Input;
            param.Size = 30;
            param.Value = auth.service;

            cmd.CommandText = "select * from [user] where [name] = @name";
            cmd.Parameters.Add(param);

            sqlConnection.Open();

            using var sqlReader = await cmd.ExecuteReaderAsync();

            if (sqlReader.HasRows)
            {
                sqlReader.Read();
                string pwd = sqlReader.GetString(1);
            }

            if (auth.password != sqlReader.GetString(1) || auth.service != sqlReader.GetString(0))
            {
                return AuthenticateResult.Fail("Invalid Service or Password");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, auth.service)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
