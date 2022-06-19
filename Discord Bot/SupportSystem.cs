using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Models;

namespace DiscordBot
{
    public class SupportSystem
    {
        public static async Task<MessageComponent> CreateSupportSystemMessageButtons()
        {
            var builder = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("supportMenu")
                    .WithPlaceholder($"Select a Support Option from below for some help!")
                    .AddOption("General Support", "genSupport", "Any support that isn't listed below")
                    .AddOption("Game Support", "gameSupport", "Any questions or issues with PoliceMP")
                    .AddOption("Forum Support", "forumSupport", "Any issues with the Forums, or things such as User Changes")
                    .AddOption("Subscription Support", "donationSupport", "Any issues with Tebex, Billing or Subscriptions")
                    .AddOption("Technical Support", "technicalSupport", "Any technical issues")
                    .AddOption("Request a Game Moderator", "modRequest", "Request a Game Moderator")
                    .AddOption("Request a Discord Moderator", "requestDiscordMod", "Request a Discord Moderator")
                    .AddOption("Report a Moderator", "modReport", "Report a Moderator for wrong doing")
                    .AddOption("Training Enquires", "trainingEnq", "Any questions or concerns regarding training")
                    .AddOption("CAD Support", "cadEnq", "Any questions or support request relating to the CAD")
                    .AddOption("Bug Report", "bugReport", "Report a Bug to the Development Team")
                    .AddOption("Merchandise", "merch", "Talk with our Merch team!"));
            return builder.Build();
        }

        public static async Task<List<Overwrite>> FetchChannelPermissions(ulong memberUserId, List<ulong> allowedRoles)
        {
            ulong everyoneId = 598107702652043264;
            
            var permissions = new List<Overwrite>
            {
                new Overwrite(everyoneId, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(memberUserId, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, readMessageHistory: PermValue.Allow))
            };

            foreach (var allowedRole in allowedRoles)
            {
                permissions.Add(new Overwrite(allowedRole, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, readMessageHistory: PermValue.Allow)));
            }

            return permissions;
        }
        
        public static async Task<List<Overwrite>> FetchClosedChannelPermissions()
        {
            ulong everyoneId = 598107702652043264;
            ulong seniorModeratorId = 780149973051244544;

            var permissions = new List<Overwrite>
            {
                new Overwrite(everyoneId, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(seniorModeratorId, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)),
            };

            return permissions;
        }
    }
}