using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Models;
using DiscordBot.Services;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PoliceMP.Shared.Models.Weather;
using Color = Discord.Color;
using Timer = System.Timers.Timer;

namespace DiscordBot
{
    public class DiscordBot
    {
        private static DiscordSocketClient _discord;

        private static SocketGuild _mainGuild;
        private static readonly ulong MainGuildId = 598107702652043264;

        private readonly string LogoUrl =
            "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg";

        #region Channels

        public static ITextChannel NewUserRulesChannel;
        private static readonly ulong NewUserRulesChannelId = 826179180071878676;

        private static ITextChannel _systemMessagesChannel;
        private static readonly ulong SystemMessagesId = 663062545308844043;

        private static ITextChannel _roleAssignmentChannel;
        private static readonly ulong _roleAssignmentChannelId = 848264958164336640;

        private static IVoiceChannel _onlinePlayerCountChannel;
        private static readonly ulong _onlinePlayerCountChannelId = 903759769502904410;

        private static IVoiceChannel _serverStatusChannel;
        private static readonly ulong _serverStatusChannelId = 903762970293723136;

        #endregion Channels

        #region Roles

        private static IRole _nonVerifiedRole;
        private static readonly ulong NonVerifiedRoleId = 861989523235405854;

        private static IRole _loaRole;
        private static readonly ulong LoaRoleId = 849742751474122802;

        private static IRole _verifiedRole;
        private static readonly ulong VerifiedRoleId = 826178299335278602;

        private static IRole _devSubscriberRole;
        private static readonly ulong _devSubscriberRoleId = 777852649637937184;

        private static IRole _twitterSubscriberRole;
        private static readonly ulong _twitterSubscriberRoleId = 848265532779659327;

        private static IRole _readyOrNotRole;
        private static readonly ulong _readyOrNotRoleId = 925118930371088416;
        
        #endregion Roles

        #region Reactions

        private static readonly string _twitterEmoteName = "bird";
        private static readonly string _devSubscriberEmoteName = "construction_worker";
        
        
        private static IEmote twitterEmote = new Emoji($"🐦");
        private static IEmote devEmote = new Emoji("👷");
        private static IEmote readyOrNotEmote = new Emoji("🔫");

        #endregion
        
        public DiscordBot()
        {
        }
        
        public async Task StartDiscordBot()
        {
            Console.WriteLine("Starting PoliceMP Bot");

            var services = ConfigureServices();

            _discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true
            });
            
            _discord.Log += Log;

            services.GetRequiredService<CommandService>().Log += Log;

            await _discord.LoginAsync(TokenType.Bot, "tokenhere");
            await _discord.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            _discord.ReactionAdded += DiscordOnReactionAdded;
            _discord.ReactionRemoved += DiscordOnReactionRemoved;
            _discord.GuildMemberUpdated += DiscordOnGuildMemberUpdated;
            _discord.UserJoined += DiscordOnUserJoined;
            _discord.MessageReceived += DiscordOnMessageReceived;
            _discord.InteractionCreated += DiscordOnInteractionCreated;
            _discord.Ready += DiscordOnReady;
            _discord.UserLeft += DiscordOnUserLeft;
            _discord.Ready += DiscordOnReady;
            
            _discord.MessageReceived += DiscordMessageReceived;
        }

        #region User Left

        private async Task DiscordOnUserLeft(SocketGuild guild, SocketUser user)
        {
            var leaveEmbed = new EmbedBuilder
            {
                Title = "User Left",
                Description = $"{user.Mention} has left the server!",
                Timestamp = DateTimeOffset.Now,
                Color = Color.Red,
                ThumbnailUrl = user.GetAvatarUrl(),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"User ID: {user.Id}"
                }
            };
            await _systemMessagesChannel.SendMessageAsync(embed: leaveEmbed.Build());
        }

        #endregion

        #region Interaction
        
        private async Task DiscordOnInteractionCreated(SocketInteraction interaction)
        {

            #region Drop Down If

            if (interaction is SocketMessageComponent componentInteraction)
            {
                if (componentInteraction.Data.Type == ComponentType.Button)
                {
                    if (componentInteraction.Data.CustomId == "closeSupportTicket")
                    {
                        await componentInteraction.DeferAsync(true);
                        // Close Support Ticket
                        var channel = (ITextChannel)componentInteraction.Channel;

                        if (channel.CategoryId == 815240585995878420) return;
                        
                        await channel.ModifyAsync(async properties =>
                        {
                            properties.Name = $"closed-{channel.Name}";
                            properties.CategoryId = 815240585995878420;
                            properties.PermissionOverwrites = await SupportSystem.FetchClosedChannelPermissions();
                        });
                        
                        var buttons = new ComponentBuilder().WithButton("Complete Ticket", "completeSupportTicket", ButtonStyle.Success).Build();
                        await channel.SendMessageAsync(
                            $"{componentInteraction.User.Mention} has closed this ticket. You may now complete it.",
                            components: buttons);
                    }

                    if (componentInteraction.Data.CustomId == "completeSupportTicket")
                    {
                        await componentInteraction.DeferAsync();
                        var channel = (ITextChannel)componentInteraction.Channel;
                        await channel.DeleteAsync();
                    }
                }
                
                if (componentInteraction.Data.Type == ComponentType.SelectMenu)
                {
                    if (componentInteraction.Data.CustomId == "supportMenu")
                    {
                        await componentInteraction.DeferAsync(true);
                        if(componentInteraction.Data.Values.First() == "bugReport")
                        {
                            await componentInteraction.FollowupAsync(
                                "Hey. We've moved the Bug Reports over the forum!\nhttps://forum.policemp.com/index.php?forums/bug-reports.232/");
                            return;
                        }
                        var channel = await CreateSupportTicket(componentInteraction);
                        await componentInteraction.FollowupAsync(
                            $"A Channel has been created! Thanks for requesting support. Head over to it! {channel.Mention}", ephemeral: true);
                        
                        return;
                    }
                }
            }

            #endregion
            

            #region Command If
            if (interaction is SocketSlashCommand command)
            {
                switch (command.Data.Name)
                {
                    case "ping":
                        await command.RespondAsync($"{command.User.Mention} Pong!", ephemeral: true);
                        break;
                    case "bug":
                        var mentionedUser = command.User;
                        var hasUser = command.Data.Options.Any();
                        if (hasUser)
                        {
                            mentionedUser = (SocketGuildUser)command.Data.Options.First().Value ?? command.User;
                        }
                        await command.RespondAsync($"**Bug Reports**\n" +
                                                   $"*Thank you for sharing your findings with us {mentionedUser.Mention}. The development team are very busy resolving any issues and adding in new features!*\n" +
                                                   $"Please head over to the forums and post a bug report! Don't forget to use as much information as possible!\n" +
                                                   $"https://forum.policemp.com/index.php?form/bug-report.5/select");
                        break;
                    case "myid":
                        await command.RespondAsync($"Your Discord User ID is: {command.User.Id}", ephemeral: true);
                        break;
                    case "connect":
                        await command.RespondAsync(
                            $"You can direct connect to the server! fivem://connect/play.policemp.com");
                        break;
                    case "donate":
                        await command.RespondAsync(
                            "You can subscribe through our Tebex! https://policemp.com/store");
                        break;
                    case "subscribe":
                        await command.RespondAsync(
                            "You can subscribe through our Tebex! https://policemp.com/store");
                        break;
                    case "pclink":
                        await command.RespondAsync($"You can head over to the forums and apply for PC!\n" +
                                                   $"Once your application is submitted, it will be automatically scored and you'll receive access to the training lounge!\n" +
                                                   $"https://forum.policemp.com/index.php?categories/police-constable-application-form.203/");
                        break;
                    case "laslink":
                        await command.RespondAsync($"You can head over to the forums and apply for LAS Paramedic!\n" +
                                                   $"Once your application is submitted, it will be automatically scored and you'll receive access to the training lounge!\n" +
                                                   $"https://forum.policemp.com/index.php?categories/student-paramedic-application-form.205/");
                        break;
                    case "loalink":
                        await command.RespondAsync($"You can head over to the Google Forms to apply for a Leave of Absence (LOA).\n" +
                                                   $"Once you submit the form, the LOA tag will be added within few hours. You only need to apply for LOA if you are a PC+ or LAS Paramedic+.\n" +
                                                   $"https://forms.gle/k2MnRM9dqTe5Sspd9");
                        break;
                    case "verify":
                        await command.RespondAsync($"Head over to {NewUserRulesChannel.Mention} to verify yourself!");
                        break;
                    case "guides":
                        await command.RespondAsync($"We have a lot of helpful guides you can check out.\n" +
                                                   $"You can check out some useful guides on our forums!\n" +
                                                   $"https://policemp.com/guides\n" +
                                                   $"We also have a cadet video!\n" +
                                                   $"https://policemp.com/guides/getting-started");
                        break;
                    case "pchelp":
                        await command.RespondAsync(
                            "**1.** Go to https://forum.policemp.com/index.php?categories/police-constable-application-form.203/\n" +
                            "**2**. If it says \"Oops! We ran into some problems.\", go to the top right where it either says your name or login/sign up.\n" +
                            "**3.** Create an account if you haven't got one already but if you have, just signin.\n" +
                            "**4.** Click your name at the top right, account settings and go down to \"Connected Accounts\" or just click this link https://forum.policemp.com/index.php?account/connected-accounts/\n" +
                            "**5.** Click \"associate with discord\" and follow the steps.\n" +
                            "**6.** Now you have linked your discord, go back to the forums, go to Police Constable Application and fill that in OR click this link again https://forum.policemp.com/index.php?categories/police-constable-application-form.203/");
                        break;
                    case "lashelp":
                        await command.RespondAsync(
                            "**1.** Go to https://forum.policemp.com/index.php?categories/student-paramedic-application-form.205/\n" +
                            "**2**. If it says \"Oops! We ran into some problems.\", go to the top right where it either says your name or login/sign up.\n" +
                            "**3.** Create an account if you haven't got one already but if you have, just signin.\n" +
                            "**4.** Click your name at the top right, account settings and go down to \"Connected Accounts\" or just click this link https://forum.policemp.com/index.php?account/connected-accounts/\n" +
                            "**5.** Click \"associate with discord\" and follow the steps.\n" +
                            "**6.** Now you have linked your discord, go back to the forums, go to Student Paramedic Application and fill that in OR click this link again https://forum.policemp.com/index.php?categories/student-paramedic-application-form.205/");
                        break;
                    case "cadhelp":
                        await command.RespondAsync(
                            "To set-up CAD, follow the steps in the CAD Guide: https://docs.google.com/presentation/d/11QesgNGFh6RfUTGT8s0SHej-SqR9F_Ozzvk3qZTMm3I/edit\n" +
                            "Pleas note, to use the CAD you need to be at least a **Police Constable** or **Student Paramedic**."
                            );
                            break;
                    case "mentee":
                        await command.RespondAsync(
                            "Use this link to become a mentee - https://forms.gle/VPYvxBqUWvSZ4VQs6");
                        break;
                    case "support":
                        await command.RespondAsync("Please select an option from the menu.", components: await SupportSystem.CreateSupportSystemMessageButtons(), ephemeral: true);
                        break;
                    case "getid":
                        var getIdUser = (SocketGuildUser)command.Data.Options.First().Value ?? command.User;
                        await command.RespondAsync($"The ID of the user is ```{getIdUser.Id}```", ephemeral: true);
                        break;
                    case "loaremove":
                        var loaRemoveUser = command.User as SocketGuildUser; 
                        var loaRole = loaRemoveUser.Roles.FirstOrDefault(x => x.Id == LoaRoleId);

                        if (loaRole != null)
                        {
                            await loaRemoveUser.RemoveRoleAsync(loaRole);

                            var policeBand2Channel = _mainGuild.GetTextChannel(892178538181038080);

                            var loaRemoveEmbedBuilder = new EmbedBuilder();

                            loaRemoveEmbedBuilder.Color = Color.DarkerGrey;
                            loaRemoveEmbedBuilder.Title = "LOA Role Removed";
                            loaRemoveEmbedBuilder.Description = $"{command.User.Username} has requested removal of their LOA role.";

                            await policeBand2Channel.SendMessageAsync(embed: loaRemoveEmbedBuilder.Build());
                            await command.RespondAsync($"Your LOA has role has been removed.", ephemeral: true);
                        }
                        else
                        {
                            Console.WriteLine($"User ID {loaRemoveUser.Id} has requested LOA removal, but does not currently have it.");
                            await command.RespondAsync($"You currently do not have an LOA role. Please only use this command if you have the role present.", ephemeral: true);
                        }
                        break;
                    case "weather":
                        var weather = await FetchWeather();
                        
                        var cultureInfo = Thread.CurrentThread.CurrentCulture;
                        var textInfo = cultureInfo.TextInfo;

                        var embedBuilder = new EmbedBuilder
                            {
                                Color = Color.Blue,
                                Description =
                                    $"Current Weather: {textInfo.ToTitleCase(weather.weather.First().description)}",
                                ThumbnailUrl =
                                    $"https://openweathermap.org/img/wn/{weather.weather.FirstOrDefault()?.icon}.png",
                                Timestamp = DateTimeOffset.Now,
                                Title = $"Current Weather for {weather.name}."
                            }.AddField("Temperature", $"{Math.Round(weather.main.temp)}°C")
                            .AddField("High Temperature", $"{Math.Round(weather.main.temp_max)}°C")
                            .AddField("Low Temperature", $"{Math.Round(weather.main.temp_min)}°C")
                            .AddField("Humidity", $"{weather.main.humidity} %RH")
                            .AddField("Visibility", $"{weather.visibility} meters")
                            .WithFooter("PoliceMP Weather Integration", "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg");
                        await command.RespondAsync(embed: embedBuilder.Build());
                        break;
                    case "post":
                        var channel = (SocketChannel)command.Data.Options.First(d => d.Name == "channel").Value;
                        var title = command.Data.Options.First(d => d.Name == "title").Value.ToString();
                        var message = command.Data.Options.First(d => d.Name == "message").Value.ToString();
                        var role = (SocketRole)command.Data.Options.FirstOrDefault(d => d.Name == "role")?.Value;
                        var hasEmbedData = command.Data.Options.Any(d => d.Name == "embed");
                        var embedMessage = true;
                        if (hasEmbedData)
                        {
                            embedMessage = (bool) command.Data.Options.FirstOrDefault(d => d.Name == "embed")?.Value;
                        }

                        var textChannel = (ITextChannel) channel;

                        if (textChannel == null)
                        {
                            await command.RespondAsync($"Channel must be a text channel!", ephemeral: true);
                            return;
                        }

                        if (embedMessage)
                        {
                            var postEmbedBuilder = new EmbedBuilder
                            {
                                Title = title,
                                Description = message,
                                ThumbnailUrl =
                                    "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg",
                                Timestamp = DateTimeOffset.Now,
                                Color = Color.Blue
                            };
                            if (role != null)
                            {
                                await textChannel.SendMessageAsync($"{role.Mention}", embed: postEmbedBuilder.Build());
                            }
                            else
                            {
                                await textChannel.SendMessageAsync(embed: postEmbedBuilder.Build());
                            }
                        }
                        else
                        {
                            if (role != null)
                            {
                                await textChannel.SendMessageAsync($"{role.Mention}\n{message}");
                            }
                            else
                            {
                                await textChannel.SendMessageAsync(message);
                            }
                        }
                        await command.RespondAsync("Message Sent!");
                        break;

                    case "removerole":
                        var roleToRemove = (SocketRole)command.Data.Options.FirstOrDefault(d => d.Name == "role")?.Value;
                        var userToRemove = (IGuildUser)command.Data.Options.FirstOrDefault(d => d.Name == "user")?.Value;
                        var removeRoleCommandTrigger = command.User as SocketGuildUser;

                        List<ulong> allowedRoleIds = new List<ulong>();
                        allowedRoleIds.Add(849742751474122802); //LOA Role
                        allowedRoleIds.Add(755152624369795102); //LFB Role
                        allowedRoleIds.Add(797934748185264158); //PC Role
                        allowedRoleIds.Add(864592335730900992); //ERT Role
                        allowedRoleIds.Add(755152618229072014); //LAS Para Role
                        allowedRoleIds.Add(886449681104769085); //Highways Role

                        if (roleToRemove != null && userToRemove != null)
						{
                            if (allowedRoleIds.Contains(roleToRemove.Id) && userToRemove.RoleIds.Contains(roleToRemove.Id))
                            {
                                await userToRemove.RemoveRoleAsync(roleToRemove);
                                await _systemMessagesChannel.SendMessageAsync(
                                    $"{userToRemove.Username} has been removed from the {roleToRemove.Name} role by {removeRoleCommandTrigger.Username}.");
                                await command.RespondAsync($"{userToRemove.Username} has been removed from the {roleToRemove.Name} role.", ephemeral: true);
                            }
                            else if (allowedRoleIds.Contains(roleToRemove.Id) && !userToRemove.RoleIds.Contains(roleToRemove.Id))
							{
                                await command.RespondAsync($"{userToRemove.Username} does not have the {roleToRemove.Name} role.", ephemeral: true);
                            }
                            else
                            {
                                await command.RespondAsync($"The role which you are trying to remove cannot be removed using this command.", ephemeral: true);
                            }
						}
                        else
						{
                            await command.RespondAsync($"Error - Please raise a bug report.", ephemeral: true);
                        }

                        break;
                    case "donatorcallsign":
                        var discordId = command.User.Id;
                        var callsign = (double)command.Data.Options.First(d => d.Name == "callsign").Value;
                        var serviceValues = SheetInterface.GetSheetsService().Spreadsheets.Values;
                        await SheetInterface.OnSendDonatorCallsignCommand(serviceValues, $"D{callsign}", discordId);
                        await command.RespondAsync(
                            "Thank you for giving us this. We will cross-reference and any issues we'll be in contact!", ephemeral: true);
                        break;
                }
            }

            #endregion
        }
        
        #endregion

        #region Weather

        private async Task<OpenWeather> FetchWeather()
        {
            var locationId = "2643743";
            var appId = "37c1a999011411a01b4d200ea16e9b9a";
            var url = new Uri(
                $"http://api.openweathermap.org/data/2.5/weather?id={locationId}&mode=json&units=metric&APPID={appId}");
            using var webClient = new WebClient();
            
            var currentWeatherJson = await webClient.DownloadStringTaskAsync(url);

            if (string.IsNullOrEmpty(currentWeatherJson)) return null;
            var currentWeather = JsonConvert.DeserializeObject<OpenWeather>(currentWeatherJson);
            return currentWeather;
        }

        #endregion
        
        #region User Joined

        private async Task DiscordOnUserJoined(SocketGuildUser user)
        {
            Console.WriteLine("User Join Event");
        
            if (user == null)
            {
                Console.WriteLine("User is Null");
                return;
            }

            try
            {
                var joinEmbed = new EmbedBuilder
                {
                    Title = "User Joined",
                    Description = $"{user.Mention} has joined the server!",
                    Timestamp = DateTimeOffset.Now,
                    Color = Color.Green,
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Footer = new EmbedFooterBuilder
                    {
                    Text = $"User ID: {user.Id}"
                    }
                };
                
                await user.AddRoleAsync(_nonVerifiedRole);
                await _systemMessagesChannel.SendMessageAsync(embed: joinEmbed.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            return;
        }

        #endregion

        #region Member Updated

        private async Task DiscordOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> userBefore, SocketGuildUser userAfter)
        {
            var rolesAdded = userAfter.Roles.Count > userBefore.Value.Roles.Count;
            if (rolesAdded)
            {
                var newRoles = userAfter.Roles.Except(userBefore.Value.Roles);
                var rolesAddedEmbed = new EmbedBuilder
                {
                    Title = "Roles Added",
                    Description = $"{userAfter.Mention} has been granted some new roles!",
                    Color = Color.Green,
                    Timestamp = DateTimeOffset.Now,
                    ThumbnailUrl = LogoUrl,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"User ID: {userAfter.Id}"
                    }
                };

                foreach (var newRole in newRoles)
                {
                    if (newRole.Id is 793170213759746049 or 673911514004062208)
                    {
                        // Donator Pro
                        var serviceValues = SheetInterface.GetSheetsService().Spreadsheets.Values;
                        var newDonatorCallsign = await SheetInterface.OnDonatorRoleAdded(serviceValues, userAfter.Id, newRole.Id == 793170213759746049);
                        await userAfter.SendMessageAsync(
                            $"Thank you for Donating to PoliceMP! :partying_face: :partying_face: - Your new Donator callsign is {newDonatorCallsign}!");

                        var donatorChannel = _mainGuild.GetTextChannel(717172337660657716);
                        await donatorChannel.SendMessageAsync(
                            $"Thank you for Donating to PoliceMP {userAfter.Mention}! :partying_face: :partying_face: - Your new Donator callsign is {newDonatorCallsign}!");
                    }
                    
                    rolesAddedEmbed.AddField("New Role", newRole.Name);
                }

                await _systemMessagesChannel.SendMessageAsync(embed: rolesAddedEmbed.Build());
            }

            var rolesRemoved = userBefore.Value.Roles.Count > userAfter.Roles.Count;
            if (rolesRemoved)
            {
                var removedRoles = userBefore.Value.Roles.Except(userAfter.Roles);
                
                var rolesRemovedEmbed = new EmbedBuilder
                {
                    Title = "Roles Added",
                    Description = $"{userAfter.Mention} has had some roles removed!",
                    Color = Color.Red,
                    Timestamp = DateTimeOffset.Now,
                    ThumbnailUrl = LogoUrl,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"User ID: {userBefore.Id}"
                    }
                };
                
                foreach (var removedRole in removedRoles)
                {
                    if (removedRole.Id is 793170213759746049 or 673911514004062208)
                    {
                        // Donator Pro
                        var serviceValues = SheetInterface.GetSheetsService().Spreadsheets.Values;
                        await SheetInterface.OnDonatorRoleRemoved(serviceValues, userAfter.Id);
                        await userAfter.SendMessageAsync(
                            $"We are sorry to hear that you have stopped donating to PoliceMP. Your previous call sign has been marked to be reallocated");
                    }
                    rolesRemovedEmbed.AddField("Removed Role", removedRole.Name);
                }

                await _systemMessagesChannel.SendMessageAsync(embed: rolesRemovedEmbed.Build());
            }
        }

        #endregion

        #region Reaction Removed

        private async Task DiscordOnReactionRemoved(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var users = await _mainGuild.GetUsersAsync().FlattenAsync();
            var user = users.FirstOrDefault(i => i.Id == reaction.User.Value.Id);

            if (user == null) return;

            if (user.IsBot) return;

            if (channel.Value == _roleAssignmentChannel)
            {
                if (reaction.Emote.Name == twitterEmote.Name)
                {
                    if (!reaction.User.IsSpecified) return;
                    if (user == null) return;
                    
                    await user.RemoveRoleAsync(_twitterSubscriberRole); 
                    await _systemMessagesChannel.SendMessageAsync(
                        $"{user.Username} has been removed from the {_twitterSubscriberRole.Name} role.");
                    
                }

                if (reaction.Emote.Name == readyOrNotEmote.Name)
                {
                    if (!reaction.User.IsSpecified) return;
                    if (user == null) return;
                    
                    await user.RemoveRoleAsync(_readyOrNotRole); 
                    await _systemMessagesChannel.SendMessageAsync(
                        $"{user.Username} has been removed from the {_readyOrNotRole.Name} role.");
                }
                
                if (reaction.Emote.Name == devEmote.Name)
                {
                    if (!reaction.User.IsSpecified) return;
                    if (user == null) return;

                    await user.RemoveRoleAsync(_devSubscriberRole);
                    await _systemMessagesChannel.SendMessageAsync(
                        $"{user.Username} has been removed from the {_devSubscriberRole.Name} role.");
                }
                
            }
        }

        #endregion

        #region Reaction Added

        private async Task DiscordOnReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var users = await _mainGuild.GetUsersAsync().FlattenAsync();
            var user = users.FirstOrDefault(i => i.Id == reaction.User.Value.Id);

            if (user == null) return;
            
            if (user.IsBot) return;
            
            if (channel.Value == NewUserRulesChannel)
            {
                if (reaction.Emote.Name != "jobdone") return;

                if (!reaction.User.IsSpecified) return;

                if (user == null) return;

                await user.AddRoleAsync(_verifiedRole);
                await user.RemoveRoleAsync(_nonVerifiedRole);

                if (user.CreatedAt > DateTimeOffset.Now.AddHours(-24))
                {
                    try
                    {
                        var modNotesChannel = (ITextChannel)_mainGuild.GetChannel(863173222651265074);
                        var embedNewUserWarning = new EmbedBuilder
                        {
                            Description =
                                $"A user who has registered in the last 24h has joined Discord.",
                            Title = $"WARNING! New User Registered!",
                            Color = Color.Orange,
                            ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                        }
                        .AddField("Discord ID", user.Id)
                        .AddField("Discord Name", user.Username)
                        .AddField("Registration date", user.CreatedAt.ToString()).Build();
                        await modNotesChannel.SendMessageAsync(embed: embedNewUserWarning);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return;
                    }
                }
                
                try
                {
                    var embed = new EmbedBuilder
                    {
                        Description =
                            $"{user.Mention}, Welcome to PoliceMP!" +
                            $"\n\nIf you're new to the community, make sure you checkout the Getting Started Guide to help you get up and running: https://policemp.com/guides/getting-started " +
                            $"\n\nYou can apply for a Police Constable role, which will unlock a taser, new uniforms and new cars by following the steps: https://policemp.com/apply " +
                            $"\n\n Lastly, if you need any support or looking for a member of staff, please use /support in any of our channels to raise a request. We have a team on stand-by to help with any queries, our Senior Staff are very busy, therefore please don't message them directly." +
                            $"\n\nEnjoy your stay at PoliceMP, we're looking forward to seeing your on our server!",
                        Title = $"Welcome to {_mainGuild.Name}!",
                        Color = Color.Blue,
                        ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                    }.Build();


                    await user.SendMessageAsync(embed: embed);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }

                await _systemMessagesChannel.SendMessageAsync(
                    $"{user.Username} has been assigned to the {_verifiedRole.Name} role.");

            }

            if (channel.Value == _roleAssignmentChannel)
            {
                if (reaction.Emote.Name == twitterEmote.Name)
                {
                    if (!reaction.User.IsSpecified) return;
                    if (user == null) return;
                    
                    await user.AddRoleAsync(_twitterSubscriberRole); 
                    await _systemMessagesChannel.SendMessageAsync(
                        $"{user.Username} has been added to the {_twitterSubscriberRole.Name} role.");
                    
                }
                
                if (reaction.Emote.Name == readyOrNotEmote.Name)
                {
                    if (!reaction.User.IsSpecified) return;
                    if (user == null) return;
                    
                    await user.AddRoleAsync(_readyOrNotRole); 
                    await _systemMessagesChannel.SendMessageAsync(
                        $"{user.Username} has been added to the {_readyOrNotRole.Name} role.");
                    
                }
                
                if (reaction.Emote.Name == devEmote.Name)
                {
                    if (!reaction.User.IsSpecified) return;
                    if (user == null) return;

                    await user.AddRoleAsync(_devSubscriberRole);
                    await _systemMessagesChannel.SendMessageAsync(
                        $"{user.Username} has been added to the {_devSubscriberRole.Name} role.");
                }
            }
            return;
        }

        #endregion

        #region Discord Ready

        private async Task DiscordOnReady()
        {
            
            _mainGuild = _discord.GetGuild(MainGuildId);

            int count = 0;

            while (_mainGuild == null && count < 10)
            {
                Console.WriteLine("Waiting to get main guild");
                count++;
                _mainGuild = _discord.GetGuild(MainGuildId);
                await Task.Delay(100);
            }
            
            if (_mainGuild is null)
            {
                Console.WriteLine("Unable to connect to the Guild!");
                return;
            }

            await _discord.SetStatusAsync(UserStatus.Invisible);
            Console.WriteLine($"Connected to {_mainGuild.Name}");

            #region Fetch Channels

            NewUserRulesChannel = _mainGuild.GetTextChannel(NewUserRulesChannelId);

            if (NewUserRulesChannel is null)
            {
                Console.WriteLine("Unable to fetch the main support channel!");
                return;
            }

            Console.WriteLine($"Found {NewUserRulesChannel.Name}");

            _systemMessagesChannel = _mainGuild.GetTextChannel(SystemMessagesId);

            if (_systemMessagesChannel is null)
            {
                Console.WriteLine($"Unable to get the system messages channel!");
                return;
            }

            _roleAssignmentChannel = _mainGuild.GetTextChannel(_roleAssignmentChannelId);

            if (_roleAssignmentChannel is null)
            {
                Console.WriteLine("Unable to get the Role Assignment channel!");
                return;
            }

            _onlinePlayerCountChannel = _mainGuild.GetVoiceChannel(_onlinePlayerCountChannelId);

            if (_onlinePlayerCountChannel is null)
            {
                Console.WriteLine("Unable to get the player count channel!");
                return;
            }

            _serverStatusChannel = _mainGuild.GetVoiceChannel(_serverStatusChannelId);

            if (_serverStatusChannel is null)
            {
                Console.WriteLine("Unable to get the service status channel");
                return;
            }
            
            #endregion

            #region Fetch Roles

            _verifiedRole = _mainGuild.GetRole(VerifiedRoleId);
            _nonVerifiedRole = _mainGuild.GetRole(NonVerifiedRoleId);
            _devSubscriberRole = _mainGuild.GetRole(_devSubscriberRoleId);
            _twitterSubscriberRole = _mainGuild.GetRole(_twitterSubscriberRoleId);
            _readyOrNotRole = _mainGuild.GetRole(_readyOrNotRoleId);

            if (_verifiedRole == null)
            {
                Console.WriteLine("Unable to find verified role");
            }

            if (_nonVerifiedRole == null)
            {
                Console.WriteLine("Unable to find non-verified role");
            }

            if (_devSubscriberRole == null)
            {
                Console.WriteLine("Unable to find dev subscriber role");
            }

            if (_twitterSubscriberRole == null)
            {
                Console.WriteLine("Unable to find twitter subscriber role");
            }

            Console.WriteLine("Found Discord Roles?");
            
            #endregion
            
            
            var users = await  _mainGuild.GetUsersAsync().FlattenAsync();
            
        
            foreach (var guildUser in await _mainGuild.GetUsersAsync().FlattenAsync())
            {
                var user = _mainGuild.GetUser(guildUser.Id);
                if (user.Roles.Contains(_nonVerifiedRole)) continue;
                if (user.Roles.Contains(_verifiedRole)) continue;
                await guildUser.AddRoleAsync(_nonVerifiedRole);
                Console.WriteLine($"Added {guildUser.Nickname} to Non-Verified");
            }

            #region Commands

            var pingCommand = new SlashCommandBuilder
            {
                Name = "ping",
                Description = "Ping Command",
            };

            var bugCommand = new SlashCommandBuilder
            {
                Name = "bug",
                Description = "Fetches the Bug Report forum link!"
                
            }.AddOption("user", ApplicationCommandOptionType.User, "If you wish to mention a user", false);

            var idCommand = new SlashCommandBuilder
            {
                Name = "myid",
                Description = "Fetches your Discord ID!"
            };

            var connectCommand = new SlashCommandBuilder
            {
                Name = "connect",
                Description = "Used to fetch the connection URL for the server"
            };
            var donateCommand = new SlashCommandBuilder
            {
                Name = "donate",
                Description = "Used to fetch the store link for the server."
            };
            var subscribeCommand = new SlashCommandBuilder
            {
                Name = "subscribe",
                Description = "Used to fetch the store link for the server."
            };
            var pcCommand = new SlashCommandBuilder
            {
                Name = "pclink",
                Description = "Used to post the PC application link."
            };
            var lasLinkCommand = new SlashCommandBuilder
            {
                Name = "laslink",
                Description = "Used to post the LAS application link."
            };
            var loaLinkCommand = new SlashCommandBuilder
            {
                Name = "loalink",
                Description = "Used to post the LOA form link."
            };
            var verifyCommand = new SlashCommandBuilder
            {
                Name = "verify",
                Description = "Used to share how to get verified on Discord!"
            };
            var guideCommand = new SlashCommandBuilder
            {
                Name = "guides",
                Description = "Shares a link to the PoliceMP guides!"
            };
            var pcHelpCommand = new SlashCommandBuilder
            {
                Name = "pchelp",
                Description = "Used to send out how to apply for PC!"
            };
            var lasHelpCommand = new SlashCommandBuilder
            {
                Name = "lashelp",
                Description = "Used to send out how to apply for Student Paramedic!"
            };
            var cadHelpCommand = new SlashCommandBuilder
            {
                Name = "cadhelp",
                Description = "Used to send out how to set-up the CAD."
            };
            var menteeCommand = new SlashCommandBuilder
            {
                Name = "mentee",
                Description = "Used to send out the Mentee Signup Link!"
            };
            var loaRemoveCommand = new SlashCommandBuilder
            {
                Name = "loaremove",
                Description = "Used to remove the LOA role. Can only be used by the user with the LOA role."
            };
            var supportCommand = new SlashCommandBuilder
            {
                Name = "support",
                Description = "Request a Ticket for some support!"
            };

            var findIdCommand = new SlashCommandBuilder
            {
                Name = "getid",
                Description = "Used to get a users discord ID"
            }.AddOption("user", ApplicationCommandOptionType.User, "The user you want the ID for", true);

            var weatherCommand = new SlashCommandBuilder
            {
                Name = "weather",
                Description = "Fetches the latest weather in the server!"
            };

            var convertDonatorCallsignCommand = new SlashCommandBuilder
            {
                Name = "donatorcallsign",
                Description = "Convert your Donator Callsign to our new system!"
            }
                .AddOption("callsign", ApplicationCommandOptionType.Number, "Your Donator Callsign", true);

            var postCommandBuilder = new SlashCommandBuilder
            {
                Name = "post",
                Description = "Post as the PoliceMP Bot",
                IsDefaultPermission = false,
            }.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel you wish to post in", true)
                .AddOption("title", ApplicationCommandOptionType.String, "The Title of the Embed Message", true)
                .AddOption("message", ApplicationCommandOptionType.String, "The message you wish to type.", true)
                .AddOption("role", ApplicationCommandOptionType.Role, "The Role you wish to Tag", false)
                .AddOption("embed", ApplicationCommandOptionType.Boolean, "Should the message be sent as an embed?", false);


            var removeRoleCommandBuilder = new SlashCommandBuilder
            {
                Name = "removerole",
                Description = "Remove PC, LAS, LFB, Highways or LOA Role.",
                IsDefaultPermission = false
            }.AddOption("user", ApplicationCommandOptionType.User, "The user to remove the role from.", true)
                .AddOption("role", ApplicationCommandOptionType.Role, "The Role you wish to remove", true);

            await _discord.Rest.CreateGuildCommand(pingCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(bugCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(idCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(connectCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(donateCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(subscribeCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(pcCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(lasLinkCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(loaLinkCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(verifyCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(guideCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(pcHelpCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(lasHelpCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(cadHelpCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(menteeCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(loaRemoveCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(supportCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(findIdCommand.Build(), MainGuildId);
            await _discord.Rest.CreateGuildCommand(weatherCommand.Build(), MainGuildId);
            var postCommand = await _discord.Rest.CreateGuildCommand(postCommandBuilder.Build(), MainGuildId);
            var removeRoleCommand = await _discord.Rest.CreateGuildCommand(removeRoleCommandBuilder.Build(), MainGuildId);

            var seniorStaffRole = _mainGuild.GetRole(616641490336219137);
            var seniorOnly = new ApplicationCommandPermission(seniorStaffRole, true);
            var everyoneDeny = new ApplicationCommandPermission(_mainGuild.EveryoneRole, false);

            var donatorCommand =
                await _discord.Rest.CreateGuildCommand(convertDonatorCallsignCommand.Build(), MainGuildId);
            var proDonatorRole = _mainGuild.GetRole(793170213759746049);
            var proDonatorAllow = new ApplicationCommandPermission(proDonatorRole, true);
            var donatorRole = _mainGuild.GetRole(673911514004062208);
            var donatorAllow = new ApplicationCommandPermission(donatorRole, true);

            var donatorCommandPermission = new ApplicationCommandPermission[]
            {
                seniorOnly,
                donatorAllow,
                proDonatorAllow,
                everyoneDeny,
            };
            
            //await donatorCommand.ModifyCommandPermissions(donatorCommandPermission);

            var band2Role = _mainGuild.GetRole(892158706878414918);
            var band2RoleAllow = new ApplicationCommandPermission(band2Role, true);
            var band3Role = _mainGuild.GetRole(892158572278980730);
            var band3RoleAllow = new ApplicationCommandPermission(band3Role, true);
            var band4Role = _mainGuild.GetRole(892154062940811344);
            var band4RoleAllow = new ApplicationCommandPermission(band4Role, true);

            var removeRoleCommandPermission = new ApplicationCommandPermission[]
            {
                seniorOnly,
                band2RoleAllow,
                band3RoleAllow,
                band4RoleAllow,
                proDonatorAllow,
                everyoneDeny,
            };
            //await removeRoleCommand.ModifyCommandPermissions(removeRoleCommandPermission);

            #endregion

            var playerCountUpdate = new Timer(300000)
            {
                AutoReset = true,
                Enabled = true
            };

            playerCountUpdate.Elapsed += async (sender, args) =>
            {
                await UpdateOnlinePlayerCount();
            };
            
            Console.WriteLine($"Updating Online Player Count");

            await UpdateOnlinePlayerCount();
            
            await CheckRoleAssignmentReactionMessage();

            Console.WriteLine("Download Users");
            
            await _mainGuild.DownloadUsersAsync();
            
            Console.WriteLine("Downloaded Users");
            
            Console.WriteLine("---------[Discord Ready]---------");
        }

        #endregion

        private async Task UpdateOnlinePlayerCount()
        {
            try
            {
                using var webClient = new HttpClient();
                var url = new Uri("http://play.policemp.com:30120/players.json");
                var output = await webClient.GetStringAsync(url);
                var masterList = JsonConvert.DeserializeObject<List<MasterListPlayer>>(output);
                await Log(new LogMessage(LogSeverity.Debug, "MasterList", $"Found {masterList.Count} players on PoliceMP!"));
                await _onlinePlayerCountChannel.ModifyAsync(properties =>
                {
                    properties.Name = $"Online Player Count: {masterList.Count}";
                });

                var pingSender = new Ping();
                var pingOptions = new PingOptions();
                pingOptions.DontFragment = true;
                var data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                var buffer = Encoding.ASCII.GetBytes(data);
                var timeout = 120;
                var reply = pingSender.Send("play.policemp.com", timeout);
                if (reply.Status == IPStatus.Success)
                {
                    await _serverStatusChannel.ModifyAsync(properties =>
                    {
                        properties.Name = $"Server Status: Online";
                    });
                }
                else
                {
                    await _serverStatusChannel.ModifyAsync(properties =>
                    {
                        properties.Name = $"Server Status: Offline";
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;  
            }
        }

        private async Task CheckRoleAssignmentReactionMessage()
        {
            var pinnedMessages = await _roleAssignmentChannel.GetPinnedMessagesAsync();
            if (!pinnedMessages.Any())
            {
                Console.WriteLine("No Pinned Messages!");
                await GenerateRoleAssignmentMessage();
                return;
            }
        }

        private async Task GenerateRoleAssignmentMessage()
        {
            var messages = await _roleAssignmentChannel.GetMessagesAsync().FlattenAsync();

            foreach (IMessage oldMessage in messages)
            {
                await oldMessage.DeleteAsync();
            }
            
            var embedBuilder = new EmbedBuilder
            {
                Color = Color.Blue,
                Description = "Feel free to react to the message below with the reactions to gain access to some roles!",
                ThumbnailUrl = "https://policemp.com/forum/data/assets/logo/pmplogo21.png",
                Title = "Role Assignment",
            };

            var developerSubscribeField = new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Developer Subscriber",
                Value = $"React to the :{_devSubscriberEmoteName}: emote to gain notifications from the Developers!"
            };
            
            var twitterSubscribeField = new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Twitter Subscriber",
                Value = $"React to the :{_twitterEmoteName}: emote to gain notifications from the Twitter Feed!"
            };

            var readyOrNotField = new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Ready Or Not",
                Value = $"Ready to the :{readyOrNotEmote}: emote to gain access to the Ready Or Not Game Area!"
            };

            embedBuilder.AddField(developerSubscribeField);
            embedBuilder.AddField(twitterSubscribeField);
            embedBuilder.AddField(readyOrNotField);

            var message = await _roleAssignmentChannel.SendMessageAsync(embed: embedBuilder.Build());

            await message.AddReactionAsync(devEmote);
            await message.AddReactionAsync(twitterEmote);
            await message.AddReactionAsync(readyOrNotEmote);

            await message.PinAsync();
        }

        public static async void SendTweetToChannel(Tweet tweet)
        {
            var embedBuilder = new EmbedBuilder
            {
                Description = tweet.Text,
                ThumbnailUrl = tweet.ProfileImage,
                Title = $"{tweet.Name} has posted a Tweet!",
                Url = tweet.TweetUrl,
            };
            
            var footerBuilder = new EmbedFooterBuilder
            {
                IconUrl = "https://cdn.cms-twdigitalassets.com/content/dam/about-twitter/en/brand-toolkit/brand-download-img-1.jpg.twimg.2560.jpg",
                Text = $"Twitter & PoliceMP Integration"
            };

            embedBuilder.WithFooter(footerBuilder);
            
            if (tweet.Media.Any())
            {
                embedBuilder.ImageUrl = tweet.Media.FirstOrDefault()?.MediaURL;
            }
            
            var mediaChannel = _mainGuild.GetTextChannel(626185457667145729);

            await mediaChannel.SendMessageAsync($"{_twitterSubscriberRole.Mention}", false, embedBuilder.Build());
        }

        public static async Task SendTrainingQuizScoreToChannel(string name, string examType, int score)
        {
            try
            {

                while (_mainGuild == null)
                {
                    await Task.Delay(100);
                }

                var collegePcTraining = _mainGuild.GetTextChannel(945005657235542036);

                var embedBuilder = new EmbedBuilder();

                if (examType == "PC" && score >= 10)
                {
                    // Pass
                    embedBuilder.Color = Color.Green;
                    embedBuilder.Title = $"{examType} Training Quiz - Pass";
                    embedBuilder.Description = $"{name} has passed their {examType} training quiz.";
                    embedBuilder.AddField("Score", score);
                }
                else if (examType == "LAS" && score >= 12)
                {
                    // Pass
                    embedBuilder.Color = Color.Green;
                    embedBuilder.Title = $"{examType} Training Quiz - Pass";
                    embedBuilder.Description = $"{name} has passed their {examType} training quiz.";
                    embedBuilder.AddField("Score", score);
                }
                else
                {
                    embedBuilder.Color = Color.Red;
                    embedBuilder.Title = $"{examType} Training Quiz - Fail";
                    embedBuilder.Description = $"{name} has failed their {examType} training quiz.";
                    embedBuilder.AddField("Score", score);
                }

                await collegePcTraining.SendMessageAsync(embed: embedBuilder.Build());

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }
        
        public static async Task SendEntranceExamFailToUser(string gameName, ulong discordId, int score, string passScore, string examType)
        {
            try
            {
                while (_mainGuild == null)
                {
                    Console.WriteLine("Main Guild null!");
                    await Task.Delay(100);
                }
            
                var pcTrainingChannel = (ITextChannel)_mainGuild.GetChannel(945005657235542036);
            
                var discordUsers = await _mainGuild.GetUsersAsync().FlattenAsync();
                var discordUser = discordUsers.FirstOrDefault(x => x.Id == discordId);

                var tryCount = 0;
            
                while (discordUser == null && tryCount < 10)
                {
                    discordUsers = await _mainGuild.GetUsersAsync().FlattenAsync();
                    discordUser = discordUsers.FirstOrDefault(x => x.Id == discordId);
                    Console.WriteLine("Null Discord User");
                    tryCount++;
                    await Task.Delay(100);
                }
            
                if (discordUser == null)
                {
                    Console.WriteLine($"Pass Null User for {examType} Apps: UserID: {discordId}, Name: {gameName}");
                    var nullUser = new EmbedBuilder
                    {
                        Title = "Incorrect Discord ID",
                        Description = $"{gameName} has applied for {examType} and failed, however their Discord ID is invalid. Their score was {score}",
                        Color = Color.Red,
                        ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                    };
                    await pcTrainingChannel.SendMessageAsync(embed: nullUser.Build());
                    return;
                }
            
                var embedBuilder = new EmbedBuilder
                {
                    Description =
                        $"{discordUser.Mention} - We are sorry to inform you've failed your Entrance Exam! Your score was {score} and the pass rate is {passScore} or higher!\nYou can re-take the test when you are ready!",
                    Title = $"Entrance Exam",
                    Color = Color.Red,
                    ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                };
                await discordUser.SendMessageAsync(embed: embedBuilder.Build(), allowedMentions: AllowedMentions.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        public static async Task SendEntranceExamPassToChannel(string gameName, ulong discordId, string examType)
        {
            try
            {
                while (_mainGuild == null)
                {
                    Console.WriteLine("Main Guild null!");
                    await Task.Delay(100);
                }

                var entryExamTrainingChannel = (ITextChannel)_mainGuild.GetChannel(945005657235542036);

                var discordUsers = await _mainGuild.GetUsersAsync().FlattenAsync();
            
                var discordUser = discordUsers.FirstOrDefault(x => x.Id == discordId);
            
                var tryCount = 0;
            
                while (discordUser == null && tryCount < 10)
                { 
                    discordUsers = await _mainGuild.GetUsersAsync().FlattenAsync();
                    discordUser = discordUsers.FirstOrDefault(x => x.Id == discordId);
                    Console.WriteLine("Null Discord User");
                    tryCount++;
                    await Task.Delay(100);
                }
            
                if (discordUser == null || discordUser.Id == 825337473197408288)
                {
                    Console.WriteLine($"Pass Null User for {examType} Apps: UserID: {discordId}, Name: {gameName}");
                    var nullUser = new EmbedBuilder
                    {
                        Title = "Incorrect Discord ID",
                        Description = $"{gameName} has applied for {examType} and passed, however their Discord ID was invalid!",
                        Color = Color.Red,
                        ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                    };
                    await entryExamTrainingChannel.SendMessageAsync(embed: nullUser.Build());
                    return;
                }

                var awaitingPCTraining = _mainGuild.GetRole(879886426496450600);
                var awaitingLASTraining = _mainGuild.GetRole(945011920577118238);

                if (examType == "PC")
                {
                    await discordUser.AddRoleAsync(awaitingPCTraining);
                }
                else if (examType == "LAS")
                {
                    await discordUser.AddRoleAsync(awaitingLASTraining);
                }

                var awaitingTrainingLoungeChannel = (ITextChannel)_mainGuild.GetChannel(879911972613783553);
                var awaitingTrainingChannel = (ITextChannel)_mainGuild.GetChannel(879896132581457981);

                var embedBuilder = new EmbedBuilder
                {
                    Description =
                        $"{discordUser.Mention} - Congratulations on passing your {examType} Entrance Exam! Head over to {awaitingTrainingChannel.Mention} for the next steps!",
                    Title = $"{examType} Entrance Exam",
                    Color = Color.Green,
                    ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                };

                await awaitingTrainingLoungeChannel.SendMessageAsync($"{discordUser.Mention}",embed: embedBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }
        public static async Task SendLoaToChannel(ulong discordId, string rank, string returnDate, string longTermLeave, string discordName)
        {
            try
            {
                while (_mainGuild == null)
                {
                    Console.WriteLine("Main Guild null!");
                    await Task.Delay(100);
                }

                var bandTwoChannel = (ITextChannel)_mainGuild.GetChannel(892178538181038080);
                var bandThreeChannel = (ITextChannel)_mainGuild.GetChannel(892178444765503528);

                var discordUsers = await _mainGuild.GetUsersAsync().FlattenAsync();

                var discordUser = discordUsers.FirstOrDefault(x => x.Id == discordId);

                var tryCount = 0;

                while (discordUser == null && tryCount < 10)
                {
                    discordUsers = await _mainGuild.GetUsersAsync().FlattenAsync();
                    discordUser = discordUsers.FirstOrDefault(x => x.Id == discordId);
                    Console.WriteLine("Null Discord User");
                    tryCount++;
                    await Task.Delay(100);
                }

                if (discordUser == null)
                {
                    Console.WriteLine($"Loa Null User for UserID: {discordId}, Name: {discordName}");
                    var nullUser = new EmbedBuilder
                    {
                        Title = "LOA Incorrect User ID",
                        Description = $"{discordName} has applied for LOA, however their Discord ID was invalid!\nRank: {rank}\nReturn date: {returnDate}",
                        Color = Color.Red,
                        ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                    };
                    await bandTwoChannel.SendMessageAsync(embed: nullUser.Build());
                    return;
                }

                var loaRole = _mainGuild.GetRole(849742751474122802);
                await discordUser.AddRoleAsync(loaRole);

                if (rank == "PC")
                {
                    if (longTermLeave == "No")
                    {
                        var embedBuilderLoa = new EmbedBuilder
                        {
                            Title = "LOA Request",
                            Description = $"{discordName} has applied for LOA.",
                            Color = Color.Green,
                        }.AddField("Rank", rank)
                        .AddField("Return Date", returnDate);
                        await bandTwoChannel.SendMessageAsync(embed: embedBuilderLoa.Build());

                    }
                    else
                    {
                        var embedBuilderLoa = new EmbedBuilder
                        {
                            Title = "LOA Request",
                            Description = $"{discordName} has applied for LOA.",
                            Color = Color.Orange,
                        }.AddField("Rank", rank)
                        .AddField("Return Date", returnDate);
                        await bandTwoChannel.SendMessageAsync(embed: embedBuilderLoa.Build());

                    }
                }
                else if (rank == "SGT+")
                {
                    var embedBuilderLoa = new EmbedBuilder
                    {
                        Title = "SGT+ has applied for LOA",
                        Description = $"{discordName} has applied for LOA.",
                        Color = Color.Orange,
                    }.AddField("Rank", rank)
                    .AddField("Return Date", returnDate);
                    await bandThreeChannel.SendMessageAsync(embed: embedBuilderLoa.Build());
                }

                var embedBuilder = new EmbedBuilder
                {
                    Description =
                        $"\n\nOn your return, please type /loaremove in any PoliceMP Discord Channel."+
                        $"\n\nIf you need to extend your Leave of Absence, please fill out the form again closer to the time. Ensure you keep your divisional command up to date.",
                    Title = $"Your Leave of Absence Request has been processed.",
                    Color = Color.Green,
                    ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                }.AddField("Discord Name", discordName)
                .AddField("Rank", rank)
                .AddField("Return date", returnDate);
                await discordUser.SendMessageAsync(embed: embedBuilder.Build(), allowedMentions: AllowedMentions.None);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        public static async Task SetUserRoleByTraining(ulong discordId, string trainerName, string division)
        {
            try
            {
                while (_mainGuild == null)
                {
                    Console.WriteLine("Main Guild null!");
                    await Task.Delay(100);
                }

                var users = await  _mainGuild.GetUsersAsync().FlattenAsync();
                var discordUser = users.FirstOrDefault(i => i.Id == discordId);
            
                if (discordUser == null) return;

                var pcRole = _mainGuild.GetRole(797934748185264158);
                var nptRole = _mainGuild.GetRole(864592335730900992);
                var paramedicRole = _mainGuild.GetRole(755152618229072014);
                var hihgwaysRole = _mainGuild.GetRole(886449681104769085);
                var fireFighterRole = _mainGuild.GetRole(755152624369795102);
                var awaitingTraining = _mainGuild.GetRole(879886426496450600);
                var awaitingLasTraining = _mainGuild.GetRole(945011920577118238);
                var messageRoleName = "";

                if (pcRole == null || nptRole == null || paramedicRole == null || hihgwaysRole == null || fireFighterRole == null || awaitingTraining == null || awaitingLasTraining == null) return;

                if (division == "MET")
                {
                    messageRoleName = pcRole.Name;
                    await discordUser.AddRoleAsync(pcRole);
                    await discordUser.AddRoleAsync(nptRole);
                    await discordUser.RemoveRoleAsync(awaitingTraining);

                }
                else if (division == "LAS")
                {
                    messageRoleName = paramedicRole.Name;
                    await discordUser.AddRoleAsync(paramedicRole);
                    await discordUser.RemoveRoleAsync(awaitingLasTraining);
                }
                else if (division == "Highways")
                {
                    messageRoleName = hihgwaysRole.Name;
                    await discordUser.AddRoleAsync(hihgwaysRole);
                }
                else if (division == "LFB")
                {
                    messageRoleName = fireFighterRole.Name;
                    await discordUser.AddRoleAsync(fireFighterRole);
                }

                var embedBuilder = new EmbedBuilder
                {
                    Title = $"{messageRoleName} Training",
                    Description =
                        $"Congratulations on passing your training! You are now a {messageRoleName} over at PoliceMP!\nYour trainer was {trainerName}.",
                    Color = Color.Green,
                    ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                };

                await discordUser.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        public static async Task SendNewNptOptInMessage(ulong discordId, string gameName)
        {
            try
            {
                while (_mainGuild == null)
                {
                    Console.WriteLine("Main Guild null!");
                    await Task.Delay(100);
                }
                var users = await  _mainGuild.GetUsersAsync().FlattenAsync();
                var discordUser = users.FirstOrDefault(i => i.Id == discordId);
                if (discordUser == null) return;

                var nptManagement = (ITextChannel)_mainGuild.GetChannel(859765685479407616);

                var discordEmbed = new EmbedBuilder
                {
                    Title = "New Team Opt-In Request",
                    Description = $"{discordUser.Mention} ({gameName}) wishes to be added to an NPT team!",
                    Color = Color.Blue,
                    ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                }.Build();

                await nptManagement.SendMessageAsync(embed: discordEmbed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

        }
        
        public static async Task SendNewNptOptOutMessage(ulong discordId, string gameName, string currentTeam)
        {
            try
            {
                while (_mainGuild == null)
                {
                    Console.WriteLine("Main Guild null!");
                    await Task.Delay(100);
                }
                var users = await  _mainGuild.GetUsersAsync().FlattenAsync();
                var discordUser = users.FirstOrDefault(i => i.Id == discordId);
                if (discordUser == null) return;

                var nptManagement = (ITextChannel)_mainGuild.GetChannel(859765685479407616);

                var discordEmbed = new EmbedBuilder
                {
                    Title = "New Team Opt-Out Request",
                    Description = $"{discordUser.Mention} ({gameName}) wishes to be removed from their NPT teams!",
                    Color = Color.Red,
                    ThumbnailUrl = "https://pbs.twimg.com/profile_images/1487001710782562306/bKuWVjEd_400x400.jpg"
                }.AddField("Current Team", currentTeam).Build();

                await nptManagement.SendMessageAsync(embed: discordEmbed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

        }

        private async Task<ITextChannel> CreateSupportTicket(SocketMessageComponent component)
        {
            try
            {
                var requestId = component.Data.Values.First();
            
                Console.WriteLine($"Request ID: {requestId}");

                var userName = component.User.Username;

                var requestType = requestId switch
                {
                    "genSupport" => SupportType.General,
                    "gameSupport" => SupportType.Game,
                    "forumSupport" => SupportType.Forum,
                    "donationSupport" => SupportType.Donation,
                    "technicalSupport" => SupportType.Technical,
                    "modRequest" => SupportType.RequestMod,
                    "requestDiscordMod" => SupportType.RequestDiscordMod,
                    "modReport" => SupportType.ReportMod,
                    "trainingEnq" => SupportType.Training,
                    "cadEnq" => SupportType.Cad,
                    "bugReport" => SupportType.Bug,
                    "merch" => SupportType.Merchandise,
                    _ => SupportType.General
                };

                var channelName = requestType switch
                {
                    SupportType.General => $"General Support - {userName}",
                    SupportType.Game => $"Game Support - {userName}",
                    SupportType.Forum => $"Forum Support - {userName}",
                    SupportType.Donation => $"Subscription Support - {userName}",
                    SupportType.Technical => $"Technical Support - {userName}",
                    SupportType.RequestMod => $"Moderator Request - {userName}",
                    SupportType.RequestDiscordMod => $"Discord Moderator Request - {userName}",
                    SupportType.ReportMod => $"Report A Moderator - {userName}",
                    SupportType.Training => $"Training Enquiry - {userName}",
                    SupportType.Cad => $"CAD Support - {userName}",
                    SupportType.Bug => $"Bug Report - {userName}",
                    SupportType.Merchandise => $"Merchandise - {userName}",
                    _ => $"General Support - {userName}"
                };

                ulong gameModeratorId = 625801206274588673;
                ulong discordModeratorId = 876869067712106596;
                ulong seniorModeratorId = 780149973051244544;
                ulong communityTeamId = 861372226279702559;
                ulong developmentTeamId = 864677599417204736;
                ulong seniorStaffId = 616641490336219137;
                ulong controlRoomId = 776476637596221502;
                ulong founderId = 598110517478817794;
                ulong seniorDevelopmentTeamId = 801191272349827093;
                ulong seniorTrainerTeamId = 835605558416375818;
                ulong merchandiseTeamId = 886818930012880936;
            
                var allowedRoles = requestType switch
                {
                    SupportType.General => new List<ulong>{discordModeratorId, seniorModeratorId, communityTeamId, gameModeratorId},
                    SupportType.Game => new List<ulong>{gameModeratorId, seniorModeratorId, communityTeamId},
                    SupportType.Forum => new List<ulong>{seniorModeratorId,communityTeamId},
                    SupportType.Donation => new List<ulong>{seniorStaffId, founderId},
                    SupportType.Technical => new List<ulong>{discordModeratorId, seniorModeratorId, communityTeamId, developmentTeamId, gameModeratorId},
                    SupportType.RequestMod => new List<ulong> { seniorModeratorId, communityTeamId, gameModeratorId },
                    SupportType.RequestDiscordMod => new List<ulong> { discordModeratorId, seniorModeratorId, communityTeamId },
                    SupportType.ReportMod => new List<ulong>{communityTeamId},
                    SupportType.Training => new List<ulong> { discordModeratorId, seniorModeratorId, communityTeamId, seniorTrainerTeamId, gameModeratorId },
                    SupportType.Cad => new List<ulong> { discordModeratorId, seniorModeratorId, communityTeamId, controlRoomId, gameModeratorId },
                    SupportType.Bug => new List<ulong>{developmentTeamId, seniorDevelopmentTeamId},
                    SupportType.Merchandise => new List<ulong>{merchandiseTeamId},
                    _ => new List<ulong>() {discordModeratorId, seniorModeratorId, communityTeamId}
                };

                var supportChannel = await _mainGuild.CreateTextChannelAsync(channelName, async properties =>
                {
                    properties.CategoryId = 815240359280902214;
                    properties.PermissionOverwrites =
                        await SupportSystem.FetchChannelPermissions(component.User.Id, allowedRoles);
                });

                var supportTypeString = channelName.Split(" - ");

                var embedBuilder = new EmbedBuilder
                {
                    Title = $"Thank you for creating a {supportTypeString[0]} Ticket!",
                    Color = Color.Gold,
                    Description =
                        "Please describe below on the assistance you require. A member of staff will be with you shortly."
                };

                var buttons = new ComponentBuilder().WithButton("Close Ticket", "closeSupportTicket", ButtonStyle.Danger).Build();

                await supportChannel.SendMessageAsync(component.User.Mention, embed: embedBuilder.Build(), components: buttons);

                if (requestType == SupportType.Bug || requestType == SupportType.Technical)
                {
                    var bugEmbedBuilder = embedBuilder = new EmbedBuilder
                    {
                        Title = "Auto Bug Report Response",
                        Color = Color.Red,
                        Description =
                            "Please note, the development team are very busy and it can take a few days to get any replies to the ticket.\n" +
                            "**Please make sure you provide the following information.**\n" +
                            "**1** - Your FiveM log file for the time of the incident\n" +
                            "**2** - Any screenshots or video evidence of the bug if available\n" +
                            "**3** - Steps to reproduce the bug."
                    };

                    await supportChannel.SendMessageAsync($"{component.User.Mention}", embed: bugEmbedBuilder.Build());
                }

                if (requestType == SupportType.RequestMod || requestType == SupportType.RequestDiscordMod)
                {
                    var modRequestType = requestType == SupportType.RequestMod ? "Game" : "Discord";

                    var modRequestEmbedBuilder = embedBuilder = new EmbedBuilder
                    {
                        Title = "Auto Moderator Request",
                        Color = Color.Orange,
                        Description =
                            $"Thank you for raising a request for a {modRequestType} Moderator.\n" +
                             "**Please make sure you provide the following information.**\n" +
                             "**1** - Name of the player(s) you're reporting\n" +
                             "**2** - Description of the report\n" +
                             "**3** - Any proof such as screenshots or videos"
                    };

                    if (requestType == SupportType.RequestMod)
                    {
                        await supportChannel.SendMessageAsync("<@&625801206274588673>", embed: modRequestEmbedBuilder.Build());
                    }
                    else
                    {
                        await supportChannel.SendMessageAsync("<@&876869067712106596>", embed: modRequestEmbedBuilder.Build());
                    }
                }

                return supportChannel;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
        
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();
        }
        
    }
}