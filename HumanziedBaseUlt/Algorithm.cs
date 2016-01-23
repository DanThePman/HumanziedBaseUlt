using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace HumanziedBaseUlt
{
    class Algorithm
    {
        public static float SimulateHealthRegen(AIHeroClient enemy, int StartTime, int EndTime)
        {    
            float regen = 0;

            int start = StartTime;
            int end = EndTime;

            bool hasbuff = Listing.Regeneration.enemyBuffs.Any(x => x.Key.Equals(enemy));
            BuffInstance regenBuff = hasbuff ?
                Listing.Regeneration.enemyBuffs.First(x => x.Key.Equals(enemy)).Value : null;
            float buffEndTime = hasbuff ? regenBuff.EndTime : 0;

            for (int i = start; i <= end; ++i)
            {
                regen += 
                    i >= buffEndTime || !hasbuff
                    ? 
                    Listing.invisEnemiesList.First(x => x.sender.Equals(enemy)).StdHealthRegen / 1000
                    : 
                    Listing.Regeneration.GetPotionRegenRate(regenBuff) / 1000;
            }

            return (float) Math.Ceiling(regen);
        }

        /// <summary>
        /// Adds regnerated std hp during delay time in fountain
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="recallEnd"></param>
        /// <param name="aioDmg"></param>
        /// <param name="fountainReg"></param>
        /// <returns></returns>
        public static float SimulateRealDelayTime(AIHeroClient enemy, int recallEnd, float aioDmg, 
            float fountainReg)
        {
            Events.InvisibleEventArgs invisEntry = Listing.invisEnemiesList.First(x => x.sender.Equals(enemy));

            float regedRecallEnd = SimulateHealthRegen(enemy, invisEntry.StartTime, recallEnd);
            float hpRecallEnd = enemy.Health + regedRecallEnd;

            // totalEnemyHp + fountainReg * seconds = myDmg
            float normalDelay = ((aioDmg - hpRecallEnd) / fountainReg) * 1000;

            float arriveTime0 = recallEnd + normalDelay;

            int start = recallEnd;
            int end = (int)Math.Ceiling(arriveTime0);

            float additionalStdRegDuringDelay = SimulateHealthRegen(enemy, start, end);

            // totalEnemyHp + fountainReg * seconds + normalRegAfterRecallFinished * seconds = myDmg <=> time
            float realDelayTime = ((aioDmg - hpRecallEnd) / (fountainReg + additionalStdRegDuringDelay)) * 1000;

            return realDelayTime;
        }

        public static IEnumerable<Obj_AI_Base> GetCollision(string sourceName)
        {
            if (sourceName == "Ezreal")
                return new List<Obj_AI_Base>();

            var heroEntry = Listing.spellDataList.First(x => x.championName == sourceName);
            Vector3 enemyBaseVec = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position;

            return (from unit in EntityManager.Heroes.Enemies.Where(h => ObjectManager.Player.Distance(h) < 2000)
                    let pred =
                        Prediction.Position.PredictLinearMissile(unit, 2000, (int)heroEntry.Width, (int)heroEntry.Delay,
                            heroEntry.Speed, -1)
                    let endpos = ObjectManager.Player.ServerPosition.Extend(enemyBaseVec, 2000)
                    let projectOn = pred.UnitPosition.To2D().ProjectOn(ObjectManager.Player.ServerPosition.To2D(), endpos)
                    where projectOn.SegmentPoint.Distance(endpos) < (int)heroEntry.Width + unit.BoundingRadius
                    select unit).Cast<Obj_AI_Base>().ToList();
        }

        public static float GetUltTravelTime(AIHeroClient source, Vector3? dest = null)
        {
            try
            {
                var targetpos = dest.HasValue ? dest.Value : ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position;
                float speed = Listing.spellDataList.First(x => x.championName == source.ChampionName).Speed;
                float delay = Listing.spellDataList.First(x => x.championName == source.ChampionName).Delay;


                float distance = source.ServerPosition.Distance(targetpos);

                float missilespeed = speed;

                if (source.ChampionName.ToLower().Contains("jinx") && distance > 1350)
                {
                    const float accelerationrate = 0.3f; //= (1500f - 1350f) / (2200 - speed), 1 unit = 0.3units/second

                    var acceldifference = distance - 1350f;

                    if (acceldifference > 150f) //it only accelerates 150 units
                        acceldifference = 150f;

                    var difference = distance - 1500f;

                    missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) +
                        difference * 2200f) / distance;
                }

                return (distance / missilespeed + delay) * 1000;
            }
            catch
            {
                return int.MaxValue;
            }
        }
    }
}
