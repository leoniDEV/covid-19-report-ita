using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Abstraction.Service;
using Covid19Report.Ita.Api.Infrastructure;
using Covid19Report.Ita.Api.Model;
using Covid19Report.Ita.Api.Model.Dto;
using Covid19Report.Ita.Api.Service;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Covid19Report.Ita.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class CovidDataCollectorController : ControllerBase
    {
        private readonly ICosmosRepository cosmosRepository;
        private readonly IDataCollector dataCollector;
        private readonly IOptions<SourceDataOptions> sourceDataOptions;
        private readonly IDictionary<string, ICosmosService> covid19Services;

        public CovidDataCollectorController(ICosmosRepository cosmosRepository, IDataCollector dataCollector, IOptions<SourceDataOptions> sourceDataOptions)
        {
            this.cosmosRepository = cosmosRepository;
            this.dataCollector = dataCollector;
            this.sourceDataOptions = sourceDataOptions;
            covid19Services = cosmosRepository.CosmosServices["covid19-ita"].Value;
        }

        [HttpPost]
        [Route("sync")]
        [Authorize]
        public async Task<IActionResult> SyncronizeAsync(string? resource)
        {
            switch (resource)
            {
                case "commits":
                    {
                        return await SyncronizeCommitAsync();
                    }

                default:
                    break;
            }
            return BadRequest();
        }

        private async IAsyncEnumerable<ItemDate> GetDataListAsync()
        {
            string? url = sourceDataOptions.Value.DateListUrl;

            if (url is null)
            {
                throw new ConfigurationErrorsException();
            }

            var dateList = await dataCollector.GetDataAsync<IEnumerable<ItemDateDto>>(url, SerializerKind.Json);

            foreach (var item in dateList)
            {
                yield return new ItemDate
                {
                    Id = item.Data.ToString("s", CultureInfo.InvariantCulture),
                    PartitionKey = "covid19-ita"
                };
            }
        }

        private async Task<IActionResult> SyncronizeCommitAsync()
        {
            string? url = sourceDataOptions.Value.GitHubCommits;
            var commitService = covid19Services["commits"];

            if (url is null)
            {
                return BadRequest();
            }

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonCommitConverter() }
            };

            var commitList = await dataCollector.GetDataAsync<IEnumerable<ItemCommitDto>>(url, SerializerKind.Json, jsonSerializerOptions);

            var filteredList = commitList.Take(15).ToArray();

            foreach (var item in filteredList)
            {
                var commit = new ItemCommit
                {
                    Id = Array.IndexOf(filteredList, item).ToString(),
                    Autore = item.Autore,
                    Data = item.Data.ToString("s", CultureInfo.InvariantCulture),
                    Sha = item.Sha,
                    Messaggio = item.Mesasggio,
                    Url = item.HtmlUrl,
                    PartitionKey = "covid19-ita"
                };

                var response = await commitService.UpdateDataAsync(commit, commit.PartitionKey);
                if (!new[] { HttpStatusCode.OK, HttpStatusCode.Created }.Contains(response))
                {
                    return StatusCode((int)response);
                }
            }

            return Ok();
        }
    }
}
