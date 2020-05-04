using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Abstraction.Service;
using Covid19Report.Ita.Api.Extensions;
using Covid19Report.Ita.Api.Infrastructure;
using Covid19Report.Ita.Api.Model;
using Covid19Report.Ita.Api.Model.Dto;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using Octokit;

namespace Covid19Report.Ita.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CovidDataCollectorController : ControllerBase
    {
        private readonly ICosmosRepository cosmosRepository;
        private readonly IRepositoriesClient gitHubRepo;
        private readonly IDataCollector dataCollector;
        private readonly SqlConnection dbConnection;
        private readonly IOptions<GitHubConfig> gitHubConfig;
        private readonly IDictionary<string, ICosmosService> covid19Services;

        public CovidDataCollectorController(ICosmosRepository cosmosRepository, IGitHubClient gitHubClient, IDataCollector dataCollector, DbConnection dbConnection, IOptions<GitHubConfig> gitHubConfig)
        {
            this.cosmosRepository = cosmosRepository;
            this.gitHubRepo = gitHubClient.Repository;
            this.dataCollector = dataCollector;
            this.dbConnection = (SqlConnection)dbConnection;
            this.gitHubConfig = gitHubConfig;
            covid19Services = cosmosRepository.CosmosServices["covid19-ita"].Value;
        }

        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> SyncronizeAsync(string? resource, [FromBody] JsonElement? json)
        {
            switch (resource)
            {
                case "commits":
                    var response = await SyncronizeCommitAsync(json);
                    return new StatusCodeResult((int)response);

                case "covid-data":
                    (bool check, var commit) = await CheckLastCommitAsync();

                    if (check)
                    {
                        return NoContent();
                    }

                    if (await SyncDataListAsync() != HttpStatusCode.NoContent)
                    {
                        return BadRequest();
                    }

                    if (await SyncCovidDataAsync() == HttpStatusCode.NoContent)
                    {
                        response = await UpdateLastCommitDateAsync(commit);
                        return new StatusCodeResult((int)response);
                    }

                    return BadRequest();

                default:
                    break;
            }

            return BadRequest();
        }

        private async Task<HttpStatusCode> SyncCovidDataAsync()
        {
            return HttpStatusCode.NotImplemented;
        }

        private async Task<(bool IsCurrent, GitHubCommit LastCommit)> CheckLastCommitAsync()
        {
            var lastCommit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", "master");
            string lastCommitDate = lastCommit.Commit.Author.Date.ToStringWithSeparatorAndZone(CultureInfo.InvariantCulture);

            using var command = dbConnection.CreateCommand();

            command.CommandText = "select * from [lastcommit] where [id] = @id";
            command.Parameters.AddWithValue("@id", 0);

            dbConnection.Open();
            using var sqlReader = await command.ExecuteReaderAsync();

            if (sqlReader.HasRows)
            {
                sqlReader.Read();

                if (lastCommitDate == sqlReader.GetString(1))
                {
                    dbConnection.Close();
                    return (true, lastCommit);
                }
            }

            dbConnection.Close();
            return (false, lastCommit);
        }

        private async Task<HttpStatusCode> UpdateLastCommitDateAsync(GitHubCommit lastCommit)
        {
            using var command = dbConnection.CreateCommand();

            command.Parameters.AddWithValue("@id", 0);
            command.Parameters.AddWithValue("@data", lastCommit.Commit.Author.Date.ToStringWithSeparatorAndZone(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@sha", lastCommit.Sha);

            command.CommandText = "update [lastcommit] set [data] = @data, [sha] = @sha where[id] = @id";

            dbConnection.Open();
            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 1)
            {
                dbConnection.Close();
                return HttpStatusCode.NoContent;
            }

            dbConnection.Close();
            return HttpStatusCode.BadRequest;
        }

        private async Task<HttpStatusCode> SyncDataListAsync()
        {
            string? url = gitHubConfig.Value.DateListUrl;
            var dateListService = covid19Services["dateTable"];

            if (url is null)
            {
                throw new ConfigurationErrorsException();
            }

            var dateList = await dataCollector.GetDataAsync<IEnumerable<ItemDateDto>>(url, SerializerKind.Json);

            foreach (var item in dateList)
            {
#if Windows
                string date = TimeZoneInfo.ConvertTimeToUtc(item.Data, TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time")).ToStringWithSeparatorAndZone(CultureInfo.InvariantCulture);
#endif
#if Linux
                string date = TimeZoneInfo.ConvertTimeToUtc(item.Data, TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")).ToStringWithSeparatorAndZone(CultureInfo.InvariantCulture);
#endif
                var dateItem = new ItemDate
                {
                    Id = date,
                    PartitionKey = date
                };

                var response = await dateListService.UpdateDataAsync(dateItem, dateItem.PartitionKey);
                if (!new[] { HttpStatusCode.OK, HttpStatusCode.Created }.Contains(response))
                {
                    return response;
                }
            }

            return HttpStatusCode.NoContent;
        }

        private async Task<HttpStatusCode> SyncronizeCommitAsync(JsonElement? jsonBody = null)
        {
            JsonElement? resource = null;
            if (Request.Headers["User-Agent"] is StringValues userAgent && userAgent.Any(x => x.Contains("VSServices", StringComparison.InvariantCultureIgnoreCase)))
            {
                resource = jsonBody?.GetProperty("resource");
                if (resource?.GetProperty("stage").GetProperty("name").GetString() != "deployProd")
                {
                    return HttpStatusCode.BadRequest;
                }
            }

            var commitService = covid19Services["commits"];
            var option = new ApiOptions
            {
                PageSize = 15,
                PageCount = 1
            };

            var request = new CommitRequest()
            {
                Until = resource?.GetProperty("run").GetProperty("finishedDate").GetString() is string finishTime ? DateTimeOffset.Parse(finishTime, CultureInfo.InvariantCulture) : DateTimeOffset.UtcNow
            };

            var commits = await gitHubRepo.Commit.GetAll("leonidev", "covid-19-report-ita", request, option);

            if (commits.Count == 0)
            {
                return HttpStatusCode.NoContent;
            }

            for (int i = 0; i < commits.Count; i++)
            {
                var currentCommit = commits.ElementAt(i);
                var itemCommit = new ItemCommit
                {
                    Id = i.ToString(CultureInfo.InvariantCulture),
                    Autore = currentCommit.Author.Login,
                    Data = currentCommit.Commit.Author.Date.ToStringWithSeparatorAndZone(CultureInfo.InvariantCulture),
                    Sha = currentCommit.Sha,
                    Messaggio = currentCommit.Commit.Message,
                    Url = currentCommit.HtmlUrl,
                    PartitionKey = "covid19-ita"
                };

                var response = await commitService.UpdateDataAsync(itemCommit, itemCommit.PartitionKey!);
                if (!new[] { HttpStatusCode.OK, HttpStatusCode.Created }.Contains(response))
                {
                    return response;
                }
            }

            return HttpStatusCode.NoContent;
        }
    }
}
