﻿using System;
using System.Linq;
using Discord.WebSocket;
using FluentScheduler;
using LittleSteve.Data;
using LittleSteve.Extensions;
using LittleSteve.Services;
using Microsoft.EntityFrameworkCore;

namespace LittleSteve.Jobs
{
    public class YoutubeMonitoringJob : IJob
    {
        private readonly SteveBotContext _botContext;
        private readonly string _channelId;

        private readonly DiscordSocketClient _client;

        // private readonly string _feedUrl;
        public YoutubeMonitoringJob(string channelId, SteveBotContext botContext, DiscordSocketClient client)
        {
            _channelId = channelId;
            _botContext = botContext;
            _client = client;
            // _feedUrl = $@"https://www.youtube.com/feeds/videos.xml?channel_id={_channelId}";
        }

        public void Execute()
        {
            var youtuber = _botContext.Youtubers.Include(x => x.YoutubeAlertSubscriptions)
                .FirstOrDefault(x => x.Id == _channelId);

            var video = YoutubeFeedReader.GetLastestVideoFromFeed(_channelId).AsSync(false);

            if (video.DatePublished - youtuber.LatestVideoDate <= TimeSpan.Zero)
            {
                return;
            }

            youtuber.LatestVideoDate = video.DatePublished;

            foreach (var subscription in youtuber.YoutubeAlertSubscriptions)
            {
                var channel = _client.GetChannel((ulong) subscription.DiscordChannelId) as SocketTextChannel;
                channel.SendMessageAsync(video.Url);
            }

            _botContext.SaveChanges();
        }
    }
}