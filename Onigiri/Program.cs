using Discord;
using Discord.WebSocket;
using DiscordUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TwitterSharp.Client;
using TwitterSharp.Request;
using TwitterSharp.Request.AdvancedSearch;
using TwitterSharp.Rule;

DiscordSocketClient client = new(new DiscordSocketConfig
{
    LogLevel = LogSeverity.Verbose,
});

client.Log += Utils.Log;

if (!File.Exists("Credentials.json"))
{
    Console.WriteLine("Enter your Discord bot token");
    var botToken = Console.ReadLine();
    Console.WriteLine("Enter the ID of your guild");
    var guildId = Console.ReadLine();
    Console.WriteLine("Enter the ID of the channel that will receives the notifications");
    var channelId = Console.ReadLine();
    Console.WriteLine("Enter your Twitter bearer token");
    var twitterBearerToken = Console.ReadLine();

    File.WriteAllText("Credentials.json", JsonConvert.SerializeObject(new Credentials
    {
        BotToken = botToken,
        GuildId = guildId,
        ChannelId = channelId,
        TwitterBearerToken = twitterBearerToken
    }));
}

var credentials = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("Credentials.json"));

var twitterClient = new TwitterClient(credentials.TwitterBearerToken);

var subs = (await twitterClient.GetInfoTweetStreamAsync()).Where(x => x.Tag == "Onigiri").ToArray();
if (subs.Length == 0)
{
    string[] channels = new[]
    {
        "tokino_sora", "robocosan", "sakuramiko35", "suisei_hosimati", "AZKi_VDiVA",
        "yozoramel", "natsuiromatsuri", "akirosenthal", "akaihaato", "minatoaqua",
        "murasakishionch", "nakiriayame", "yuzukichococh", "oozorasubaru",
        "shirakamifubuki", "ookamimio", "nekomataokayu", "inugamikorone",
        "usadapekora", "uruharushia", "shiranuiflare", "shiroganenoel", "houshoumarine",
        "amanekanatach", "kiryucoco", "tsunomakiwatame", "tokoyamitowa", "himemoriluna",
        "yukihanalamy", "momosuzunene", "shishirobotan", "omarupolka", "manoaloe",
        "ayunda_risu", "moonahoshinova", "airaniiofifteen",
        "kureijiollie", "anyamelfissa", "pavoliareine",
        "moricalliope", "takanashikiara", "ninomaeinanis", "gawrgura", "watsonameliaEN",
        "_YOGiRi_owo", "_Civia", "SpadeEcho",
        "Doris_Hololive", "Rosalyn_holoCN", "Artia_OW",
        "miyabihanasaki", "kanadeizuru", "arurandeisu", "rikkaroid", "kagamikirach", "YakushijiSuzaku",
        "astelleda", "kishidotemma", "yukokuroberu",
        "kageyamashien", "aragamioga", "tsukishitakaoru",
        "achan_UGA", "tanigox", "daidoushinove"
    };
    List<StreamRequest> requests = new();
    for (int i = 0; i < channels.Length; i += 20)
    {
        int max = i + 20;
        if (max > channels.Length) max = channels.Length;
        var exp = Expression.Author(channels[i]);
        if (i + 1 < max)
        {
            List<Expression> exps = new();
            for (int y = i + 1; y < max; y++)
            {
                exps.Add(Expression.Author(channels[y]));
            }
            exp = exp.Or(exps.ToArray());
        }
        requests.Add(new(exp, "Onigiri"));
    }
    subs = await twitterClient.AddTweetStreamAsync(requests.ToArray());
}

Console.WriteLine("Current subscriptions:\n" + string.Join("\n\n", subs.Select(x => x.Value.ToString())));

client.Ready += () =>
{
    var guild = client.GetGuild(ulong.Parse(credentials.GuildId));
    if (guild == null)
        throw new NullReferenceException("Guild is null");
    var textChan = guild.GetTextChannel(ulong.Parse(credentials.ChannelId));
    if (textChan == null)
        throw new NullReferenceException("Text channel is null");

    _ = Task.Run(async () => {
        await twitterClient.NextTweetStreamAsync((tweet) =>
        {
            textChan.SendMessageAsync(embed: new EmbedBuilder
            {
                Title = tweet.Author.Name,
                ImageUrl = tweet.Author.ProfileImageUrl,
                Color = Color.Blue,
                Description = tweet.Text
            }.Build());
        }, new[] { UserOption.Profile_Image_Url });
    });

    return Task.CompletedTask;
};

await client.LoginAsync(TokenType.Bot, credentials.BotToken);
await client.StartAsync();

await Task.Delay(-1);

public class Credentials
{
    public string BotToken;
    public string GuildId;
    public string ChannelId;
    public string TwitterBearerToken;
}