using System;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Models;
using Tweetinvi;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Streaming;

namespace DiscordBot
{
    public class TwitterBot
    {

        private TwitterClient _twitter;
        private IFilteredStream _tweetStream;
        private readonly long _policemp6Id = 1367264490484621314;

        private IUser _policeMP6User;
        
        public TwitterBot()
        {
            
        }

        public async Task StartTwitterBot()
        {
            try
            {
                Console.WriteLine("Starting Twitter Intergration");

                _twitter = new TwitterClient("token");

                var user = await _twitter.Users.GetAuthenticatedUserAsync();

                Console.WriteLine($"Connected to App as {user.Name}");

                _tweetStream = _twitter.Streams.CreateFilteredStream();
                
                _policeMP6User = await _twitter.Users.GetUserAsync(_policemp6Id);

                _tweetStream.AddFollow(_policeMP6User.Id);
            
                Console.WriteLine($"Notifications enabled for {_policeMP6User.Name}!");

                _tweetStream.StreamStarted += (sender, args) =>
                {
                    Console.WriteLine("Tweet Stream Started!");
                };

                _tweetStream.MatchingTweetReceived += TweetStreamOnMatchingTweetReceived;

                await _tweetStream.StartMatchingAnyConditionAsync();

                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private void TweetStreamOnMatchingTweetReceived(object sender, MatchedTweetReceivedEventArgs args)
        {
            try
            {
                if (args.Tweet.CreatedBy.Name == _policeMP6User.Name)
                {
                    Console.WriteLine($"New Tweet from @{args.Tweet.CreatedBy.ScreenName}");
            
                    var newTweet = new Tweet
                    {
                        Name = args.Tweet.CreatedBy.Name,
                        ScreenName = args.Tweet.CreatedBy.ScreenName,
                        UserId = args.Tweet.CreatedBy.Id,
                        Text = args.Tweet.Text,
                        ProfileImage = args.Tweet.CreatedBy.ProfileImageUrl400x400,
                        TweetUrl = args.Tweet.Url,
                        Media = args.Tweet.Media
                    };

                    
                    DiscordBot.SendTweetToChannel(newTweet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }
    }
}