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
        public static float GetUltTravelTime(AIHeroClient source)
        {
            try
            {
                var targetpos = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy);
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

        public static float SimulateHealthRegen(AIHeroClient enemy, int StartTime, int EndTime)
        {    
            float regen = 0;

            int start = StartTime/1000;
            int end = EndTime / 1000;

            bool hasbuff = Listing.Regeneration.enemyBuffs.Any(x => x.Key.Equals(enemy));
            BuffInstance regenBuff = hasbuff ?
                Listing.Regeneration.enemyBuffs.First(x => x.Key.Equals(enemy)).Value : null;
            float buffEndTime = hasbuff ? regenBuff.EndTime/1000 : 0;

            for (int i = start; i <= end; ++i)
            {
                regen += 
                    i >= buffEndTime || !hasbuff
                    ? 
                    Listing.invisEnemiesList.First(x => x.sender.Equals(enemy)).StdHealthRegen
                    : 
                    Listing.Regeneration.GetPotionRegenRate(regenBuff);
            }

            return regen;
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
    }
}
