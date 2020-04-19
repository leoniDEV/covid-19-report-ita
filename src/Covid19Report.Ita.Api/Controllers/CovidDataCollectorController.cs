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
using Covid19Report.Ita.Api.Infrastructure;
using Covid19Report.Ita.Api.Model;
using Covid19Report.Ita.Api.Model.Dto;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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
        private GitHubCommit? lastCommit;

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
                    return await SyncronizeCommitAsync(json);

                case "covid-data":
                    return await CheckLastCommitAsync() ? await SyncCovidDataAsync() : Ok();

                default:
                    break;
            }

            return BadRequest();
        }

        private async Task<bool> CheckLastCommitAsync()
        {
            lastCommit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", "master");
            using var command = dbConnection.CreateCommand();

            command.CommandText = "select * from [lastcommit] where [id] = @id";
            command.Parameters.AddWithValue("@id", 0);

            dbConnection.Open();
            using var sqlReader = await command.ExecuteReaderAsync();

            if (sqlReader.HasRows)
            {
                sqlReader.Read();

                if (lastCommit.Commit.Author.Date.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) == sqlReader.GetString(1))
                {
                    dbConnection.Close();
                    return false;
                }
            }

            dbConnection.Close();
            return true;
        }

        private async Task<IActionResult> SyncCovidDataAsync()
        {
            var dateListResponse = await GetDataListAsync();

            if (dateListResponse.StatusCode != (int)HttpStatusCode.Created)
            {
                return BadRequest();
            }

            if (lastCommit is null)
            {
                lastCommit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", "master");
            }

            using var command = dbConnection.CreateCommand();

            command.Parameters.AddWithValue("@id", 0);
            command.Parameters.AddWithValue("@data", lastCommit.Commit.Author.Date.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@sha", lastCommit.Sha);

            command.CommandText = "update [lastcommit] set [data] = @data, [sha] = @sha where[id] = @id";

            dbConnection.Open();
            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 1)
            {
                dbConnection.Close();
                return Ok();
            }

            dbConnection.Close();
            return BadRequest();
        }

        private async Task<StatusCodeResult> GetDataListAsync()
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
                var response = await dateListService.CreateItemAsync(item, "covid19-ita");
                if (!new[] { HttpStatusCode.OK, HttpStatusCode.Created }.Contains(response))
                {
                    return StatusCode((int)response);
                }
            }

            return new StatusCodeResult(201);
        }

        private async Task<IActionResult> SyncronizeCommitAsync(JsonElement? jsonBody = null)
        {
            JsonElement? resource = null;
            if (Request.Headers["User-Agent"] is StringValues userAgent && userAgent.Any(x => x.Contains("VSServices", StringComparison.InvariantCultureIgnoreCase)))
            {
                resource = jsonBody?.GetProperty("resource");
                if (resource?.GetProperty("stage").GetProperty("name").GetString() != "__default")
                {
                    return BadRequest();
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
                return Ok();
            }

            for (int i = 0; i < commits.Count; i++)
            {
                var currentCommit = commits.ElementAt(i);
                var itemCommit = new ItemCommit
                {
                    Id = i.ToString(CultureInfo.InvariantCulture),
                    Autore = currentCommit.Author.Login,
                    Data = currentCommit.Commit.Author.Date.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    Sha = currentCommit.Sha,
                    Messaggio = currentCommit.Commit.Message,
                    Url = currentCommit.HtmlUrl,
                    PartitionKey = "covid19-ita"
                };

                var response = await commitService.UpdateDataAsync(itemCommit, itemCommit.PartitionKey!);
                if (!new[] { HttpStatusCode.OK, HttpStatusCode.Created }.Contains(response))
                {
                    return StatusCode((int)response);
                }
            }

            return Ok();
        }
    }
}
