using System;
using System.Linq;
using EloBuddy;

namespace HumanziedBaseUlt
{
    class Algorithm
    {
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
    }
}
