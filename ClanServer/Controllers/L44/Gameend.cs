using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using eAmuseCore.KBinXML;

using ClanServer.Helpers;
using ClanServer.Routing;
using ClanServer.Models;

namespace ClanServer.Controllers.L44
{
    [ApiController, Route("L44")]
    public class GameendController : ControllerBase
    {
        private readonly ClanServerContext ctx;

        public GameendController(ClanServerContext ctx)
        {
            this.ctx = ctx;
        }

        [HttpPost, Route("8"), XrpcCall("gameend.regist")]
        public async Task<ActionResult<EamuseXrpcData>> Register([FromBody] EamuseXrpcData xrpcData)
        {
            try
            {
                XElement dataE = xrpcData.Document.Element("call").Element("gameend").Element("data");
                XElement playerE = dataE.Element("player");

                xrpcData.Document = new XDocument(new XElement("response", new XElement("gameend")));

                if (playerE == null)
                    return xrpcData;

                int jid = int.Parse(playerE.Element("jid").Value);

                JubeatProfile profile = await ctx.JubeatProfiles
                    .Include(p => p.ClanData)
                    .Include(p => p.ClanSettings)
                    .Include(p => p.Jubilitys)
                    .SingleOrDefaultAsync(p => p.ID == jid);

                var data = profile.ClanData;
                var settings = profile.ClanSettings;

                if (profile == null || data == null || settings == null)
                    return NotFound();

                XElement teamE = playerE.Element("team");
                data.Team = byte.Parse(teamE.Attribute("id").Value);
                data.Street = int.Parse(teamE.Element("street").Value);
                data.Section = int.Parse(teamE.Element("section").Value);
                data.HouseNo1 = short.Parse(teamE.Element("house_number_1").Value);
                data.HouseNo1 = short.Parse(teamE.Element("house_number_2").Value);

                XElement infoE = dataE.Element("info");
                data.PlayTime = int.Parse(infoE.Element("play_time").Value);

                XElement pInfoE = playerE.Element("info");
                data.TuneCount = int.Parse(pInfoE.Element("tune_cnt").Value);
                data.ClearCount = int.Parse(pInfoE.Element("clear_cnt").Value);
                data.FcCount = int.Parse(pInfoE.Element("fc_cnt").Value);
                data.ExCount = int.Parse(pInfoE.Element("ex_cnt").Value);
                data.MatchCount = int.Parse(pInfoE.Element("match_cnt").Value);
                data.BeatCount = int.Parse(pInfoE.Element("beat_cnt").Value);
                data.SaveCount = int.Parse(pInfoE.Element("save_cnt").Value);
                data.SavedCount = int.Parse(pInfoE.Element("saved_cnt").Value);
                data.BonusTunePoints = int.Parse(pInfoE.Element("bonus_tune_points").Value);
                data.BonusTunePlayed = pInfoE.Element("is_bonus_tune_played").Value == "1";

                XElement jboxE = playerE.Element("jbox");
                data.JboxPoints = int.Parse(jboxE.Element("point").Value);

                XElement lastE = playerE.Element("last");
                settings.ExpertOption = sbyte.Parse(lastE.Element("expert_option").Value);
                settings.Sort = sbyte.Parse(lastE.Element("sort").Value);
                settings.Category = sbyte.Parse(lastE.Element("category").Value);

                XElement settingsE = lastE.Element("settings");
                settings.Marker = sbyte.Parse(settingsE.Element("marker").Value);
                settings.Theme = sbyte.Parse(settingsE.Element("theme").Value);
                settings.RankSort = sbyte.Parse(settingsE.Element("rank_sort").Value);
                settings.ComboDisplay = sbyte.Parse(settingsE.Element("combo_disp").Value);
                settings.Matching = sbyte.Parse(settingsE.Element("matching").Value);
                settings.Hard = sbyte.Parse(settingsE.Element("hard").Value);
                settings.Hazard = sbyte.Parse(settingsE.Element("hazard").Value);

                IEnumerable<XElement> tunes = dataE.Element("result").Elements("tune");

                foreach (XElement tune in tunes)
                {
                    XElement tunePlayer = tune.Element("player");
                    XElement tuneScore = tunePlayer.Element("score");

                    int musicId = int.Parse(tune.Element("music").Value);
                    sbyte seq = sbyte.Parse(tuneScore.Attribute("seq").Value);

                    JubeatScore score = new JubeatScore()
                    {
                        ProfileID = profile.ID,
                        MusicID = musicId,
                        Seq = seq
                    };

                    score.Timestamp = long.Parse(tune.Element("timestamp").Value);
                    score.Score = int.Parse(tunePlayer.Element("score").Value);
                    score.Clear = sbyte.Parse(tuneScore.Attribute("clear").Value);
                    score.IsHardMode = tunePlayer.Element("is_hard_mode").Value == "1";
                    score.IsHazardMode = tunePlayer.Element("is_hazard_end").Value == "1";
                    score.NumPerfect = short.Parse(tunePlayer.Element("nr_perfect").Value);
                    score.NumGreat = short.Parse(tunePlayer.Element("nr_great").Value);
                    score.NumGood = short.Parse(tunePlayer.Element("nr_good").Value);
                    score.NumPoor = short.Parse(tunePlayer.Element("nr_poor").Value);
                    score.NumMiss = short.Parse(tunePlayer.Element("nr_miss").Value);

                    string[] mbarStrs = tunePlayer.Element("play_mbar").Value.Split(' ');
                    score.Bar = Array.ConvertAll(mbarStrs, s => byte.Parse(s));

                    ctx.JubeatScores.Add(score);

                    JubeatHighscore highscore = ctx.JubeatHighscores
                        .Where(s => s.MusicID == musicId && s.Seq == seq && s.ProfileID == profile.ID)
                        .SingleOrDefault();

                    if (highscore == null)
                    {
                        highscore = new JubeatHighscore()
                        {
                            ProfileID = profile.ID,
                            MusicID = musicId,
                            Seq = seq
                        };

                        ctx.JubeatHighscores.Add(highscore);
                    }

                    if (score.Score > highscore.Score)
                        highscore.Timestamp = score.Timestamp;

                    highscore.Score = int.Parse(tunePlayer.Element("best_score").Value);
                    highscore.Clear = sbyte.Parse(tunePlayer.Element("best_clear").Value);
                    highscore.PlayCount = int.Parse(tunePlayer.Element("play_cnt").Value);
                    highscore.ClearCount = int.Parse(tunePlayer.Element("clear_cnt").Value);
                    highscore.FcCount = int.Parse(tunePlayer.Element("fc_cnt").Value);
                    highscore.ExcCount = int.Parse(tunePlayer.Element("ex_cnt").Value);

                    mbarStrs = tunePlayer.Element("mbar").Value.Split(' ');
                    highscore.Bar = Array.ConvertAll(mbarStrs, s => byte.Parse(s));
                }

                XElement jubility = playerE.Element("jubility");
                data.JubilityParam = int.Parse(jubility.Attribute("param").Value);

                profile.Jubilitys.Clear();

                foreach (XElement targetMusic in jubility.Element("target_music_list").Elements("target_music"))
                {
                    profile.Jubilitys.Add(new JubeatClanJubility()
                    {
                        MusicID = int.Parse(targetMusic.Element("music_id").Value),
                        Seq = sbyte.Parse(targetMusic.Element("seq").Value),
                        Score = int.Parse(targetMusic.Element("score").Value),
                        Value = int.Parse(targetMusic.Element("value").Value),
                        IsHardMode = targetMusic.Element("is_hard_mode").Value == "1"
                    });
                }

                await ctx.SaveChangesAsync();

                return xrpcData;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return StatusCode(500);
            }
        }

        [HttpPost, Route("8"), XrpcCall("gameend.final")]
        public async Task<ActionResult<EamuseXrpcData>> Final([FromBody] EamuseXrpcData xrpcData)
        {
            try
            {
                XElement dataE = xrpcData.Document.Element("call").Element("gameend").Element("data");
                XElement playerE = dataE.Element("player");

                xrpcData.Document = new XDocument(new XElement("response", new XElement("gameend")));

                if (playerE == null)
                    return xrpcData;

                int jid = int.Parse(playerE.Element("jid").Value);

                JubeatProfile profile = await ctx.JubeatProfiles
                    .Include(p => p.ClanData)
                    .Include(p => p.ClanSettings)
                    .Include(p => p.Jubilitys)
                    .SingleOrDefaultAsync(p => p.ID == jid);

                var data = profile.ClanData;

                XElement jboxE = playerE.Element("jbox");
                data.JboxPoints = int.Parse(jboxE.Element("point").Value);

                await ctx.SaveChangesAsync();

                return xrpcData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return StatusCode(500);
            }
        }
    }
}
