using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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

                    if (await SyncDateListAsync() != HttpStatusCode.OK)

                    {
                        return BadRequest();
                    }


                    if (await SyncCovidDataAsync(check.lastCheck) == HttpStatusCode.OK)

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

        private async Task<HttpStatusCode> SyncCovidDataAsync(DateTimeOffset? lastCheck)
        {

            var regionRequest = new CommitRequest()
            {
                Since = lastCheck,
                Path = "dati-regioni"
            };

            var provinceRequest = new CommitRequest()
            {
                Since = lastCheck,
                Path = "dati-province"
            };

            var fieldTableRequest = new CommitRequest()
            {
                Since = lastCheck,
                Path = "dati-andamento-covid19-italia.md"
            };

            var commitsRegion = await gitHubRepo.Commit.GetAll("pcm-dpc", "COVID-19", regionRequest);
            var commitsProvince = await gitHubRepo.Commit.GetAll("pcm-dpc", "COVID-19", provinceRequest);
            var commitsFieldTable = await gitHubRepo.Commit.GetAll("pcm-dpc", "COVID-19", fieldTableRequest);

            var csvRegion = new List<string>();
            var csvProvince = new List<string>();
            string? fieldTable = null;
            var regexReg = new Regex("dati-regioni/.*-\\d*\\.csv");
            var regexProv = new Regex("dati-province/.*-\\d*\\.csv");

            foreach (var item in commitsRegion)
            {
                var commit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", item.Sha);
                foreach (var file in commit.Files)
                {
                    if (regexReg.IsMatch(file.Filename))
                    {
                        csvRegion.Add(file.RawUrl);
                    }
                }
            }

            foreach (var item in commitsProvince)
            {
                var commit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", item.Sha);
                foreach (var file in commit.Files)
                {
                    if (regexProv.IsMatch(file.Filename))
                    {
                        csvProvince.Add(file.RawUrl);
                    }
                }
            }

            foreach (var item in commitsFieldTable)
            {
                var commit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", item.Sha);
                foreach (var file in commit.Files)
                {
                    if (file.Filename == "dati-andamento-covid19-italia.md")
                    {
                        fieldTable = file.BlobUrl;
                    }
                }
            }

            var regionService = covid19Services["regionData"];
            var provinceService = covid19Services["provinceData"];

            var response = HttpStatusCode.OK;

            if (csvRegion.Count > 0)
            {
                foreach (string csv in csvRegion)
                {
                    var trentinoList = new List<ItemRegioneDto>(2);
                    await foreach (var record in dataCollector.GetDataAsync<ItemRegioneDto>(csv, SerializerKind.CSV))
                    {
                        var value = new ItemRegione();
                        if (new string[] { "04", "21", "22" }.Contains(record.CodiceRegione))
                        {
                            trentinoList.Add(record);
                            continue;
                        }
                        else
                        {
                            value.Data = NormalizeDate(DateTime.Parse(record.Data, CultureInfo.InvariantCulture));
                            (_,
                                 _,
                                 value.CodiceRegione,
                                 _,
                                 _,
                                 _,
                                 value.RicoveratiConSintomi,
                                 value.TerapiaIntensiva,
                                 value.TotaleOspedalizzati,
                                 value.IsolamentoDomiciliare,
                                 value.TotalePositivi,
                                 value.VariazioneTotalePositivi,
                                 value.NuoviPositivi,
                                 value.DimessiGuariti,
                                 value.Deceduti,
                                 value.TotaleCasi,
                                 value.Tamponi,
                                 value.CasiTestati,
                                 value.NoteIt,
                                 value.NoteEn) = record;

                            value.Id = $"{value.Data}_{value.CodiceRegione}";
                            value.PartitionKey = value.Data;
                        }

                        response = await regionService.UpdateDataAsync(value, value.PartitionKey);
                        if ((int)response > 299)
                        {
                            return response;
                        }
                    }

                    var trentino = MergeTrentino(trentinoList.ToArray());
                    if (trentino != null)
                    {
                        response = await regionService.UpdateDataAsync(trentino, trentino.PartitionKey);
                        if ((int)response > 299)
                        {
                            return response;
                        }
                    }
                }
            }

            if (csvProvince.Count > 0)
            {
                foreach (string item in csvProvince)
                {
                    await foreach (var record in dataCollector.GetDataAsync<ItemProvinciaDto>(item, SerializerKind.CSV))
                    {
                        var value = new ItemProvincia
                        {
                            Data = NormalizeDate(DateTime.Parse(record.Data, CultureInfo.InvariantCulture))
                        };
                        (_,
                             _,
                             value.CodiceRegione,
                             _,
                             value.CodiceProvincia,
                             _,
                             _,
                             _,
                             _,
                             value.TotaleCasi,
                             value.NoteIt,
                             value.NoteEn) = record;

                        value.Id = $"{value.Data}_{value.CodiceRegione}_{value.CodiceProvincia}";
                        value.PartitionKey = value.Data;

                        if (new string[] { "04", "21", "22" }.Contains(record.CodiceRegione))
                        {
                            value.CodiceRegione = "04";
                        }

                        response = await provinceService.UpdateDataAsync(value, value.PartitionKey);
                        if ((int)response > 299)
                        {
                            return response;
                        }
                    }
                }
            }

            if (fieldTable != null)
            {
                response = HttpStatusCode.OK;
            }

            return response;

        }

        private async Task<(bool IsCurrent, DateTimeOffset? lastCheck, GitHubCommit LastCommit)> CheckLastCommitAsync()
        {
            var lastCommit = await gitHubRepo.Commit.Get("pcm-dpc", "COVID-19", "master");
            DateTimeOffset lastCommitDate = lastCommit.Commit.Author.Date;

            using var command = dbConnection.CreateCommand();

            command.CommandText = "select * from [lastcommit] where [id] = @id";
            command.Parameters.AddWithValue("@id", 0);

            dbConnection.Open();
            using var sqlReader = await command.ExecuteReaderAsync();

            if (sqlReader.HasRows)
            {
                sqlReader.Read();
                var lastCheck = DateTimeOffset.Parse(sqlReader.GetString(1), CultureInfo.InvariantCulture);
                if (lastCommitDate <= lastCheck)
                {
                    dbConnection.Close();
                    return (true, lastCheck, lastCommit);
                }

                dbConnection.Close();
                return (false, lastCheck, lastCommit);
            }

            dbConnection.Close();
            return (false, DateTimeOffset.UtcNow, lastCommit);
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

        private async Task<HttpStatusCode> SyncDateListAsync()
        {
            string? url = gitHubConfig.Value.DateListUrl;
            var dateListService = covid19Services["dateTable"];

            if (url is null)
            {
                throw new ConfigurationErrorsException();
            }

            await foreach (var dateList in dataCollector.GetDataAsync<IEnumerable<ItemDateDto>>(url, SerializerKind.Json))
            {
                foreach (var item in dateList)
                {
                    var dateItem = new ItemDate
                    {
                        Id = NormalizeDate(item.Data),
                        PartitionKey = "covid19"
                    };

                    var response = await dateListService.UpdateDataAsync(dateItem, dateItem.PartitionKey);
                    if ((int)response > 299)
                    {
                        return response;
                    }
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

        private ItemRegione? MergeTrentino(ItemRegioneDto[]? records)
        {
            if (records is null || records.Length == 0)
            {
                return null;
            }

            var trentino = new ItemRegione
            {
                Data = NormalizeDate(DateTime.Parse(records[0].Data, CultureInfo.InvariantCulture)),
                CodiceRegione = "04",
            };
            trentino.Id = $"{trentino.Data}_{trentino.CodiceRegione}";
            trentino.PartitionKey = trentino.Data;

            Func<string?, string?, string> sumValue = (val1, val2) => (Int32.Parse(val1 ?? "0", CultureInfo.InvariantCulture) + Int32.Parse(val2 ?? "0", CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);

            trentino.RicoveratiConSintomi = sumValue(records[0].RicoveratiConSintomi, records[1].RicoveratiConSintomi);
            trentino.TerapiaIntensiva = sumValue(records[0].TerapiaIntensiva, records[1].TerapiaIntensiva);
            trentino.TotaleOspedalizzati = sumValue(records[0].TotaleOspedalizzati, records[1].TotaleOspedalizzati);
            trentino.IsolamentoDomiciliare = sumValue(records[0].IsolamentoDomiciliare, records[1].IsolamentoDomiciliare);
            trentino.TotalePositivi = sumValue(records[0].TotalePositivi, records[1].TotalePositivi);
            trentino.VariazioneTotalePositivi = sumValue(records[0].VariazioneTotalePositivi, records[1].VariazioneTotalePositivi);
            trentino.NuoviPositivi = sumValue(records[0].NuoviPositivi, records[1].NuoviPositivi);
            trentino.DimessiGuariti = sumValue(records[0].DimessiGuariti, records[1].DimessiGuariti);
            trentino.Deceduti = sumValue(records[0].Deceduti, records[1].Deceduti);
            trentino.TotaleCasi = sumValue(records[0].TotaleCasi, records[1].TotaleCasi);
            trentino.Tamponi = sumValue(records[0].Tamponi, records[1].Tamponi);
            trentino.CasiTestati = sumValue(records[0].CasiTestati, records[1].CasiTestati);

            return trentino;
        }

        private string NormalizeDate(DateTime data)
        {
#if Windows
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
#endif
#if Linux
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
#endif
            return TimeZoneInfo.ConvertTimeToUtc(data, timeZoneInfo).ToStringWithSeparatorAndZone(CultureInfo.InvariantCulture);
        }
    }
}
