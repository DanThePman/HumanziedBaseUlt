using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using HumanziedBaseUlt.DamageCalculation;
using SharpDX;

namespace HumanziedBaseUlt
{
    static class Damage
    {
        private static float lastRegenDelay = 0;

        /// <summary>
        /// Set last known extra delay for premades -  If try would succeed
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="timeLeft"></param>
        public static void SetRegenerationDelay(float delay, float timeLeft)
        {
            regenDelayIsSet = true;
            lastRegenDelay = delay;

            Core.DelayAction(() =>
            {
                lastRegenDelay = 0;
                regenDelayIsSet = false;

            }, (int)Math.Ceiling(timeLeft + delay));
        }

        private static bool regenDelayIsSet = false;
        public static bool isRegenDelaySet
        {
            get { return regenDelayIsSet; }
        }

        /// <summary>
        /// list of premades, dmg
        /// </summary>
        /// <param name="target"></param>
        /// <param name="timeLeft"></param>
        /// <param name="dest">Where the destination point is to check collision</param>
        /// <returns></returns>
        public static float GetAioDmg(AIHeroClient target, float timeLeft, Vector3 dest)
        {
            float dmg = 0;
            bool elobuddyDamageMethod = Listing.MiscMenu.Get<Slider>("damageCalcMethod").CurrentValue == 0;
            int premadesInvolvedCount = 0;

            foreach (var ally in EntityManager.Heroes.Allies.Where(x => x.IsValid && !x.ChampionName.ToLower().Contains("karthus")))
            {
                bool isGlobalChamp = Listing.UltSpellDataList.Any(x => x.Key == ally.ChampionName);
                if (!isGlobalChamp)
                    continue;

                string menuid = ally.ChampionName + "/Premade";
                bool isPremade = Listing.allyconfig.Get<CheckBox>(menuid).CurrentValue;
                if (!isPremade)
                    continue;

                var spell = ally.Spellbook.GetSpell(SpellSlot.R);
                var cooldown = spell.CooldownExpires - Game.Time;

                float travelTime = Algorithm.GetUltTravelTime(ally, dest);
                bool intime = travelTime <= timeLeft + lastRegenDelay;
                bool collision = Algorithm.GetCollision(ally.ChampionName, dest).Any();

                float delay = timeLeft + lastRegenDelay - travelTime;
                bool canr = cooldown <= delay/1000 && ally.Mana >= 100 && ally.Level >= 6;

                if (canr && intime && !collision)
                {
                    dmg += elobuddyDamageMethod ? GetBaseUltSpellDamage(target, ally) :
                        (float)GetBaseUltSpellDamageAdvanced(target, ally);
                    premadesInvolvedCount++;
                }
            }

            var karthusAlly =
                EntityManager.Heroes.Allies.FirstOrDefault(x => x.IsValid && x.ChampionName.ToLower().Contains("karthus"));

            if (karthusAlly != null)
            {
                string karthusMenuid = karthusAlly.ChampionName + "/Premade";
                bool isKarthusPremade = Listing.allyconfig.Get<CheckBox>(karthusMenuid).CurrentValue;

                if (isKarthusPremade && premadesInvolvedCount > 0)
                {
                    var spell = karthusAlly.Spellbook.GetSpell(SpellSlot.R);
                    var cooldown = spell.CooldownExpires - Game.Time;

                    bool intimeKarthus = timeLeft + lastRegenDelay >= 4000;
                    float delay = timeLeft + lastRegenDelay - 4000;

                    bool canr = cooldown <= delay/1000 && karthusAlly.Mana >= 100 && karthusAlly.Level >= 6;

                    if (canr && intimeKarthus)
                    {
                        dmg += elobuddyDamageMethod
                            ? DamageLibrary.GetSpellDamage(karthusAlly, target, SpellSlot.R)
                            : (float)GetBaseUltSpellDamageAdvanced(target, karthusAlly);
                    }
                }
            }

            return dmg;
        }

        static Vector3 enemySpawn
        {
            get { return ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position; }
        }

        public static float GetBaseUltSpellDamage(AIHeroClient target, AIHeroClient source)
        {
            var level = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level - 1;
            float dmg = 0;

            if (source.ChampionName == "Jinx")
            { 
                {
                    var damage = new float[] {250, 350, 450}[level] +
                                 new float[] {25, 30, 35}[level]/100*(target.MaxHealth - target.Health) +
                                 ObjectManager.Player.FlatPhysicalDamageMod;
                    dmg = source.CalculateDamageOnUnit(target, DamageType.Physical, damage);
                }
            }
            if (source.ChampionName == "Ezreal")
            {
                {
                    var damage = new float[] {350, 500, 650}[level] + 0.9f*ObjectManager.Player.FlatMagicDamageMod +
                                 1*ObjectManager.Player.FlatPhysicalDamageMod;
                    dmg = source.CalculateDamageOnUnit(target, DamageType.Magical, damage)*0.7f;
                }
            }
            if (source.ChampionName == "Ashe")
            {
                {
                    var damage = new float[] {250, 425, 600}[level] + 1*ObjectManager.Player.FlatMagicDamageMod;
                    dmg = source.CalculateDamageOnUnit(target, DamageType.Magical, damage);
                }
            }
            if (source.ChampionName == "Draven")
            {
                {
                    var damage = new float[] {175, 275, 375}[level] + 1.1f*ObjectManager.Player.FlatPhysicalDamageMod;
                    dmg = source.CalculateDamageOnUnit(target, DamageType.Physical, damage)*0.7f;
                }
            }

            return dmg*0.7f;
        }

        public static double GetBaseUltSpellDamageAdvanced(AIHeroClient target, AIHeroClient source)
        {
            float damageMultiplicator = Listing.UltSpellDataList[source.ChampionName].DamageMultiplicator;
            int spellStage = Listing.UltSpellDataList[source.ChampionName].SpellStage;

            return source.GetSpellDamage(target, SpellSlot.R, spellStage)*damageMultiplicator;
        }
    }
}
