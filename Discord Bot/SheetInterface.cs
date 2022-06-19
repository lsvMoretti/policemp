using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DiscordBot.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class SheetInterface
    {
        private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};
        private static string GoogleCredentialsFileName = "google-credentials.json";
        /*
           Sheet1 - tab name in a spreadsheet
           A:B     - range of values we want to receive
        */
        private const string TrainingSheetId = "trainingsheetid";
        private const string TrainingSheetReadRange = "Responses!A:U";

        private const string LASTrainingSheetId = "LASTrainingSheetId";
        private const string LASTrainingSheetReadRange = "Responses!A:AG1";

        private static string StoredFile = "storedData.json";

        private const string TrainingConfirmationSheetId = "TrainingConfirmationSheetId";
        private const string TrainingConfirmationReadRange = "Responses!A:E";

        private const string NptTeamOptSheetId = "NptTeamOptSheetId";
        private const string NptTeamRange = "NPT Teams!A:G";
        private const string NptFormResponseRange = "Responses!A:G";

        private const string NewDonatorSheetId = "NewDonatorSheetId";
        private const string NewDonatorSheetRange = "Donators!A:D";
        private const string OldDonatorSheetConvert = "OldCallsigns!A:B";

        private const string LoaSheetId = "LoaSheetId";
        private const string LoaSheetReadRange = "Form responses 1!A:G";


        public async Task StartGoogleSheetInterface()
        {
            await ProcessSheetData();

            var timer = new Timer(60000)
            {
                AutoReset = true,
                Enabled = true
            };
            
            timer.Elapsed += async (sender, args) =>
            {
                await TimerOnElapsed(sender, args);
            };
        }

        private async Task ProcessSheetData()
        {
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            await ReadTrainingQuizResponses(serviceValues);
            await ReadLASTrainingQuizResponses(serviceValues);
            await ReadConstableResponses(serviceValues);
            await ReadTrainingCompletionResponses(serviceValues);
            await ReadNptFormResponses(serviceValues);
            await ReadLASTrainingResponses(serviceValues);
            await ReadLOAResponses(serviceValues);
        }

        private async Task TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await ProcessSheetData();
        }

        public static SheetsService GetSheetsService()
        {
            using var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read);
            var serviceInitializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
            };

            return new SheetsService(serviceInitializer);
        }

        public static async Task OnDonatorRoleRemoved(SpreadsheetsResource.ValuesResource valuesResource, ulong discordId)
        {
            
            var responses = await valuesResource.Get(NewDonatorSheetId, NewDonatorSheetRange).ExecuteAsync();
            var values = responses.Values;

            if (values == null || !values.Any())
            {
                Console.WriteLine("No values!");
                return;
            }

            var rowData = values.FirstOrDefault(c => c.Count > 1 && c[1] != null && (string) c[1] == discordId.ToString());

            if(rowData == null) return;

            
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        rowData[0].ToString(),
                        string.Empty,
                        string.Empty,
                        string.Empty
                    }
                }
            };

            var rowNo = values.IndexOf(rowData) + 1;
            var rangeToUpdate = $"Donators!A{rowNo}:D{rowNo}";
            
            Console.WriteLine(rangeToUpdate);

            var updateRequest = valuesResource.Update(valueRange, NewDonatorSheetId, rangeToUpdate);
            updateRequest.ValueInputOption = (SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum?) SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var response = await updateRequest.ExecuteAsync();
            return;
        }

        public static async Task OnSendDonatorCallsignCommand(SpreadsheetsResource.ValuesResource valuesResource,
            string callSign, ulong discordId)
        {
            var responses = await valuesResource.Get(NewDonatorSheetId, OldDonatorSheetConvert).ExecuteAsync();
            var values = responses.Values;

            if (values == null || !values.Any()) return;
            
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        callSign,
                        discordId.ToString()
                    }
                }
            };
            var update = valuesResource.Append(valueRange, NewDonatorSheetId, OldDonatorSheetConvert);
            update.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            var response = await update.ExecuteAsync();
            
        }

        public static async Task<string> OnDonatorRoleAdded(SpreadsheetsResource.ValuesResource valuesResource, ulong discordId,
            bool isProDonator)
        {
            try
            {
                var responses = await valuesResource.Get(NewDonatorSheetId, NewDonatorSheetRange).ExecuteAsync();
                var values = responses.Values;

                if (values == null || !values.Any())
                {
                    Console.WriteLine("No values!");
                    return string.Empty;
                }

                var rowData = values.FirstOrDefault(c => c.Count > 1 && c[1] != null && (string) c[1] == discordId.ToString());

                if (rowData != null)
                {
                    Console.WriteLine("Row not null!");
                    return rowData[0].ToString();
                }

                int d100RowId = 100;

                var newDonatorCallSignRows = values.Skip(d100RowId);

                foreach (var newDonatorCallSignRow in newDonatorCallSignRows)
                {
                    var callSign = newDonatorCallSignRow[0].ToString();

                    Console.WriteLine($"{callSign} - RowCount: {newDonatorCallSignRow.Count}");
                    
                    if (newDonatorCallSignRow.Count > 1)
                    {
                        if (newDonatorCallSignRow[1] != null) continue;
                    }
                    

                    var valueRange = new ValueRange
                    {
                        Values = new List<IList<object>>
                        {
                            new List<object>
                            {
                                callSign,
                                discordId.ToString(),
                                isProDonator ? "Pro Donator" : "Basic Donator",
                                DateTime.Now
                            }
                        }
                    };

                    var rowNo = values.IndexOf(newDonatorCallSignRow) + 1;
                    var rangeToUpdate = $"Donators!A{rowNo}:D{rowNo}";

                    var updateRequest = valuesResource.Update(valueRange, NewDonatorSheetId, rangeToUpdate);
                    updateRequest.ValueInputOption = (SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum?) SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    var response = await updateRequest.ExecuteAsync();
                    return callSign;

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return String.Empty;
            }
            
            return String.Empty;
        }

        private static async Task ReadNptFormResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var responses = await valuesResource.Get(NptTeamOptSheetId, NptFormResponseRange).ExecuteAsync();
                var values = responses.Values;

                if (values == null || !values.Any())
                {
                    Console.WriteLine("No Values!");
                    return;
                }

                var nptTeamResponses = await valuesResource.Get(NptTeamOptSheetId, NptTeamRange).ExecuteAsync();
                var teamValues = nptTeamResponses.Values;

                if (teamValues == null || !values.Any())
                {
                    Console.WriteLine("No NPT Team Info!");
                    return;
                }

                StoredData storedData = null;
                if (!File.Exists($"{Directory.GetCurrentDirectory()}/{StoredFile}"))
                {
                    storedData = new StoredData
                    {
                        LastConstableRowId = 1,
                        LastParamedicRowId = 1,
                        LastTrainingRowId = 1,
                        NptTeamResponseOptInOut = 1,
                        NptTeamAssignment = 1,
                        LastLoaRowId = 1,
                    };

                    await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                        JsonConvert.SerializeObject(storedData));
                }
                else
                {
                    var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                    storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);
                }

                var newRows = 0;

                foreach (var row in values.Skip(storedData.NptTeamResponseOptInOut))
                {
                    var discordIdString = row[1].ToString();
                    var optText = row[2].ToString();
                    var discordIdParse = ulong.TryParse(discordIdString, out ulong discordId);

                    var previousTeams = row[4].ToString();
                    var row5 = row[5];

                    var gameName = "";

                    if (row5 != null)
                    {
                        gameName = row5.ToString();
                    }
                
                    newRows++;

                    if(!discordIdParse) continue;
                
                    bool isOptIn = optText.ToLower().Contains("in");

                    if (isOptIn)
                    {
                        if (teamValues.Skip(1).All(o => o[0]?.ToString() != discordIdString))
                        {
                            // No fields in Team Page
                            var valueRange = new ValueRange
                            {
                                Values = new List<IList<object>>
                                {
                                    new List<object>
                                    {
                                        discordIdString,
                                        gameName,
                                        false,
                                        false,
                                        false,
                                        false,
                                        false
                                    }
                                }
                            };

                            Console.WriteLine(gameName);
                        
                            var update = valuesResource.Append(valueRange, NptTeamOptSheetId, NptTeamRange);
                            update.ValueInputOption =
                                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                            var response = await update.ExecuteAsync();
                            Console.WriteLine($"Added {response.Updates.UpdatedRows}!");

                            await DiscordBot.SendNewNptOptInMessage(discordId, gameName);
                        }
                    }
                    else
                    {
                        await DiscordBot.SendNewNptOptOutMessage(discordId, gameName, previousTeams);
                    }

                    storedData.NptTeamResponseOptInOut += newRows;
                    await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                        JsonConvert.SerializeObject(storedData));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private static async Task ReadTrainingCompletionResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var responses = await valuesResource.Get(TrainingConfirmationSheetId, TrainingConfirmationReadRange).ExecuteAsync();
                var values = responses.Values;

                if (values == null || !values.Any())
                {
                    Console.WriteLine("No values!");
                    return;
                }

                StoredData storedData = null;
            
                if (!File.Exists($"{Directory.GetCurrentDirectory()}/{StoredFile}"))
                {
                    storedData = new StoredData
                    {
                        LastConstableRowId = 1,
                        LastParamedicRowId = 1,
                        LastTrainingRowId = 1,
                        NptTeamResponseOptInOut = 1,
                        NptTeamAssignment = 1,
                        LastLoaRowId = 1,
                    };

                    await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                        JsonConvert.SerializeObject(storedData));
                }
                else
                {
                    var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                    storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);
                }

                var newRows = 0;

                foreach (var row in values.Skip(storedData.LastTrainingRowId))
                {
                    var timeStamp = row[0];
                    var emailAddress = row[1];
                    var trainerName = row[2];
                    var discordIdString = row[3].ToString();
                    var divisionName = row[4];
                
                    var discordIdParse = ulong.TryParse(discordIdString, out ulong discordId);

                    newRows++;
                    if (discordIdParse)
                    {
                        await DiscordBot.SetUserRoleByTraining(discordId, trainerName.ToString(), divisionName.ToString());
                    }
                }
            
                storedData.LastTrainingRowId += newRows;
            
                await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                    JsonConvert.SerializeObject(storedData));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private static async Task ReadTrainingQuizResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var responseSheetId = "1BN4NkaYWLeCnil_q7VvvPYufartStn4O_-k18gg6wL4";
                var responseReadRange = "Responses!A:Q";


                var response = await valuesResource.Get(responseSheetId, responseReadRange).ExecuteAsync();

                var values = response.Values;

                if (values == null || !values.Any()) return;

                var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                var storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);

                var newRows = 0;

                foreach (var row in values.Skip(storedData.LastTrainingQuizRowId))
                {
                    var timeStamp = row[0];
                    var scoreString = row[1].ToString();
                    var scores = scoreString.Split(" / ");
                    var tryScoreParse = int.TryParse(scores[0], out int score);
                    var name = row[2].ToString();

                    newRows++;

                    await DiscordBot.SendTrainingQuizScoreToChannel(name, "PC", score);
                }

                storedData.LastTrainingQuizRowId += newRows;
                await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                    JsonConvert.SerializeObject(storedData));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private static async Task ReadLASTrainingQuizResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var responseSheetId = "1SDTFOkFMMD455cat8ysTkmEEoRFxxDONojXoe2HKMd4";
                var responseReadRange = "Form responses 1!A:S";


                var response = await valuesResource.Get(responseSheetId, responseReadRange).ExecuteAsync();

                var values = response.Values;

                if (values == null || !values.Any()) return;

                var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                var storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);

                var newRows = 0;

                foreach (var row in values.Skip(storedData.LastLASTrainingQuizRowId))
                {
                    var timeStamp = row[0];
                    var scoreString = row[1].ToString();
                    var scores = scoreString.Split(" / ");
                    var tryScoreParse = int.TryParse(scores[0], out int score);
                    var name = row[3].ToString();

                    newRows++;

                    await DiscordBot.SendTrainingQuizScoreToChannel(name, "LAS", score);
                }

                storedData.LastLASTrainingQuizRowId += newRows;
                await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                    JsonConvert.SerializeObject(storedData));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private static async Task ReadConstableResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var response = await valuesResource.Get(TrainingSheetId, TrainingSheetReadRange).ExecuteAsync();
                var values = response.Values;

                if (values == null || !values.Any())
                {
                    Console.WriteLine($"Unable to find any values!");
                    return;
                }

                StoredData storedData = null;
            

                if (!File.Exists($"{Directory.GetCurrentDirectory()}/{StoredFile}"))
                {
                    storedData = new StoredData
                    {
                        LastConstableRowId = 1,
                        LastParamedicRowId = 1,
                        LastTrainingRowId = 1,
                        NptTeamResponseOptInOut = 1,
                        NptTeamAssignment = 1,
                        LastLoaRowId = 1,
                    };

                    await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                        JsonConvert.SerializeObject(storedData));
                }
                else
                {
                    var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                    storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);
                }

                var newRows = 0;
                var passScore = 13;

                foreach (var row in values.Skip(storedData.LastConstableRowId))
                {
                    var timeStamp = row[0];
                    var scoreString = row[1].ToString();
                    var scores = scoreString.Split(" / ");

                    var tryScoreParse = int.TryParse(scores[0], out int score);
                
                    var igName = row[2];
                    var discordIdParse = ulong.TryParse(row[3].ToString(), out ulong discordId);
                
                    Console.WriteLine($"MET IgName {igName} has scored {scores[0]}");
                    Console.WriteLine($"MET Time: {timeStamp}, Score: {score}, ig Name: {igName}, discordId {discordId}");
                    newRows++;
                    if (discordIdParse)
                    {
                        if (score >= passScore)
                        {
                            await DiscordBot.SendEntranceExamPassToChannel(igName.ToString(),discordId, "PC");
                        }
                        else
                        {
                            await DiscordBot.SendEntranceExamFailToUser(igName.ToString(),discordId, score, passScore.ToString(), "PC");
                        }
                    }
                }

                storedData.LastConstableRowId += newRows;
                await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                    JsonConvert.SerializeObject(storedData));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
            
        }
        private static async Task ReadLASTrainingResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var response = await valuesResource.Get(LASTrainingSheetId, LASTrainingSheetReadRange).ExecuteAsync();
                var values = response.Values;

                if (values == null || !values.Any())
                {
                    Console.WriteLine($"Unable to find any values!");
                    return;
                }

                StoredData storedData = null;


                if (!File.Exists($"{Directory.GetCurrentDirectory()}/{StoredFile}"))
                {
                    storedData = new StoredData
                    {
                        LastConstableRowId = 1,
                        LastParamedicRowId = 1,
                        LastTrainingRowId = 1,
                        NptTeamResponseOptInOut = 1,
                        NptTeamAssignment = 1,
                        LastLoaRowId = 1,
                    };

                    await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                        JsonConvert.SerializeObject(storedData));
                }
                else
                {
                    var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                    storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);
                }

                var newRows = 0;
                var passScore = 17;

                foreach (var row in values.Skip(storedData.LastParamedicRowId))
                {
                    var timeStamp = row[0];
                    var scoreString = row[1].ToString();
                    var scores = scoreString.Split(" / ");

                    var tryScoreParse = int.TryParse(scores[0], out int score);

                    var igName = row[2];
                    var discordIdParse = ulong.TryParse(row[3].ToString(), out ulong discordId);

                    Console.WriteLine($"LAS IgName {igName} has scored {scores[0]}");
                    Console.WriteLine($"LAS Time: {timeStamp}, Score: {score}, ig Name: {igName}, discordId {discordId}");
                    newRows++;
                    if (discordIdParse)
                    {
                        if (score >= passScore)
                        {
                            await DiscordBot.SendEntranceExamPassToChannel(igName.ToString(), discordId, "LAS");
                        }
                        else
                        {
                            await DiscordBot.SendEntranceExamFailToUser(igName.ToString(), discordId, score, passScore.ToString(), "LAS");
                        }
                    }
                }

                storedData.LastParamedicRowId += newRows;
                await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                    JsonConvert.SerializeObject(storedData));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

        }

        private static async Task ReadLOAResponses(SpreadsheetsResource.ValuesResource valuesResource)
        {
            try
            {
                var response = await valuesResource.Get(LoaSheetId, LoaSheetReadRange).ExecuteAsync();
                var values = response.Values;

                if (values == null || !values.Any())
                {
                    Console.WriteLine($"Unable to find any values!");
                    return;
                }

                StoredData storedData = null;


                if (!File.Exists($"{Directory.GetCurrentDirectory()}/{StoredFile}"))
                {
                    storedData = new StoredData
                    {
                        LastConstableRowId = 1,
                        LastParamedicRowId = 1,
                        LastTrainingRowId = 1,
                        NptTeamResponseOptInOut = 1,
                        NptTeamAssignment = 1,
                        LastLoaRowId = 1,
                    };

                    await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                        JsonConvert.SerializeObject(storedData));
                }
                else
                {
                    var storedContents = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}");
                    storedData = JsonConvert.DeserializeObject<StoredData>(storedContents);
                }

                var newRows = 0;

                foreach (var row in values.Skip(storedData.LastLoaRowId))
                {
                    var timeStamp = row[0];
                    var discordIdParse = ulong.TryParse(row[1].ToString(), out ulong discordId);
                    var rank = row[2].ToString();
                    var returnDate = row[3].ToString();
                    var longTermAbsence = row[4].ToString();
                    var discordName = row[6].ToString();

                    Console.WriteLine($"LOA Request Discord Name {discordName}");
                    newRows++;
                    if (discordIdParse)
                    {
                        await DiscordBot.SendLoaToChannel(discordId, rank, returnDate, longTermAbsence, discordName);
                    }
                }

                storedData.LastLoaRowId += newRows;
                await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/{StoredFile}",
                    JsonConvert.SerializeObject(storedData));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

        }
    }
}