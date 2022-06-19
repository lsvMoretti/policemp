using System.Collections.Generic;
using Tweetinvi.Models.Entities;

namespace DiscordBot.Models
{
    public class Tweet
    {
        public string Name { get; set; }
        public string ScreenName { get; set; }
        public long UserId { get; set; }
        public string Text { get; set; }
        public string ProfileImage { get; set; }
        public string TweetUrl { get; set; }

        public List<IMediaEntity> Media { get; set; }
        
        public Tweet()
        {
            
        }
    }
}