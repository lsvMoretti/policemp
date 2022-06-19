using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Data;
using PoliceMP.Data.Entities;

namespace PoliceMP.Server.Services
{
    public class ActivityService //: IActivityService
    {
        private readonly GtaDbContext _context;

        public ActivityService(GtaDbContext context)
        {
            _context = context;
        }

        public async Task<UserActivity> CreateAsync(string steamId, string steamName, bool isMod)
        {
            if (await _context.UserActivities.AnyAsync(u => u.SteamId == steamId)) return null;

            var userActivity = new UserActivity
            {
                SteamId = steamId,
                SteamName = steamName,
                TotalMinutes = 0,
                LastLogin = DateTime.Now,
                IsMod = isMod
            };

            await _context.UserActivities.AddAsync(userActivity);
            await _context.SaveChangesAsync();
            return userActivity;
        }

        public async Task<UserActivity> GetBySteamIdAsync(string steamId)
        {
            return await _context.UserActivities.FirstOrDefaultAsync(u => u.SteamId == steamId);
        }

        public async Task UpdateSteamUserName(string steamId, string newSteamName)
        {
            var userActivity = await _context.UserActivities.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (userActivity == null) return;

            userActivity.SteamName = newSteamName;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateLastLogin(string steamId)
        {
            var userActivity = await _context.UserActivities.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (userActivity == null) return;

            userActivity.LastLogin = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task IncrementPlayTime(string steamId)
        {
            var userActivity = await _context.UserActivities.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (userActivity == null) return;

            userActivity.TotalMinutes++;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateModStatus(string steamId, bool isMod)
        {
            var userActivity = await _context.UserActivities.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (userActivity == null) return;

            userActivity.IsMod = isMod;

            await _context.SaveChangesAsync();

        }
    }
}