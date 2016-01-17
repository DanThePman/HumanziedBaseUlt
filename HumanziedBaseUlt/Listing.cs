using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;

namespace HumanziedBaseUlt
{
    class Listing
    {
        public static Menu allyconfig;

        public class UltSpellDataS
        {
            public string championName;
            public int SpellStage;
            public float DamageMultiplicator;
            public float Width;
            public float Delay;
            public float Speed;
            public bool Collision;
        }
        public static readonly List<UltSpellDataS> spellDataList = new List<UltSpellDataS>
        {
            new UltSpellDataS { championName = "Jinx", SpellStage = 1, DamageMultiplicator = 1.0f, Width = 140f, Delay = 0600f/1000f, Speed = 1700f, Collision = true},
            new UltSpellDataS { championName = "Ashe", SpellStage = 0, DamageMultiplicator = 1.0f, Width = 130f, Delay = 0250f/1000f, Speed = 1600f, Collision = true},
            new UltSpellDataS { championName = "Draven", SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 0400f/1000f, Speed = 2000f, Collision = true},
            new UltSpellDataS { championName = "Ezreal", SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 1000f/1000f, Speed = 2000f, Collision = false},
            //new UltSpellDataS { championName = "Karthus", SpellStage = 0, DamageMultiplicator = 1.0f, Width = 000f, Delay = 3125f/1000f, Speed = 0000f, Collision = false}
        };

        public class PortingEnemy
        {
            public AIHeroClient Sender { get; set; }
            public float TotalRegedHealthSinceInvis { get; set; }
            public int StartTick { get; set; }

            public int Duration { get; set; }
        }
        public static readonly List<PortingEnemy> teleportingEnemies = new List<PortingEnemy>(5);

        public static readonly List<AIHeroClient> visibleEnemies = new List<AIHeroClient>(5);
        public static readonly List<Events.InvisibleEventArgs> invisEnemiesList = new List<Events.InvisibleEventArgs>(5);

        public class Regeneration
        {
            public static readonly Dictionary<AIHeroClient, float> lastEnemyRegens = new Dictionary<AIHeroClient, float>(5);
            public static readonly Dictionary<AIHeroClient, BuffInstance> enemyBuffs = new Dictionary<AIHeroClient, BuffInstance>(5);           

            public static void CheckEnemyBaseRegenartions()
            {
                
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    bool hasbuff = HasPotionActive(enemy);
                    BuffInstance buff = hasbuff ? GetPotionBuff(enemy) : null;

                    float val = hasbuff
                        ? enemy.HPRegenRate - GetPotionRegenRate(buff)
                        : enemy.HPRegenRate;

                    if (hasbuff && val < 0) //HPRegenRate not updated on potion
                        val = enemy.HPRegenRate;

                    if (lastEnemyRegens.ContainsKey(enemy))
                        lastEnemyRegens[enemy] = val;
                    else
                        lastEnemyRegens.Add(enemy, val);
                }
            }

            /// <summary>
            /// Returns Duration
            /// </summary>
            /// <param name="hero"></param>
            /// <returns></returns>
            public static bool HasPotionActive(AIHeroClient hero)
            {
                string[] potionStrings = {
                    RegenerationSpellBook.HealthPotion.BuffName,
                    RegenerationSpellBook.HealthPotion.BuffNameCookie,
                    RegenerationSpellBook.RefillablePotion.BuffName,
                    RegenerationSpellBook.CorruptingPotion.BuffName,
                    RegenerationSpellBook.HuntersPotion.BuffName
                };

                return hero.Buffs.Any(x => potionStrings.Contains(x.Name));
            }

            public static BuffInstance GetPotionBuff(AIHeroClient hero)
            {
                string[] potionStrings = {
                    RegenerationSpellBook.HealthPotion.BuffName,
                    RegenerationSpellBook.HealthPotion.BuffNameCookie,
                    RegenerationSpellBook.RefillablePotion.BuffName,
                    RegenerationSpellBook.CorruptingPotion.BuffName,
                    RegenerationSpellBook.HuntersPotion.BuffName
                };

                return hero.Buffs.First(x => potionStrings.Contains(x.Name));
            }

            /// <summary>
            /// per second
            /// </summary>
            /// <param name="buff"></param>
            /// <returns></returns>
            public static float GetPotionRegenRate(BuffInstance buff)
            {
                if (buff.Name == RegenerationSpellBook.HealthPotion.BuffName ||
                    buff.Name == RegenerationSpellBook.HealthPotion.BuffNameCookie)
                {
                    return RegenerationSpellBook.HealthPotion.RegenRate;
                }
                if (buff.Name == RegenerationSpellBook.RefillablePotion.BuffName)
                {
                    return RegenerationSpellBook.RefillablePotion.RegenRate;
                }
                if (buff.Name == RegenerationSpellBook.CorruptingPotion.BuffName)
                {
                    return RegenerationSpellBook.CorruptingPotion.RegenRate;
                }
                if (buff.Name == RegenerationSpellBook.HuntersPotion.BuffName)
                {
                    return RegenerationSpellBook.HuntersPotion.RegenRate;
                }

                return float.NaN;
            }
        }

        /// <summary>
        /// Regens per second
        /// </summary>
        public class RegenerationSpellBook
        {
            public static class HealthPotion
            {
                public static string BuffName = "RegenerationPotion";
                public static string BuffNameCookie = "ItemMiniRegenPotion";
                public static float RegenRate = 10f;
                public static float Duration = 15000;
            }
            public static class RefillablePotion
            {
                public static string BuffName = "ItemCrystalFlask";
                public static float RegenRate = 10.42f;
                public static float Duration = 12000;
            }
            public static class HuntersPotion
            {
                public static string BuffName = "ItemCrystalFlaskJungle";
                public static float RegenRate = 7.5f;
                public static float Duration = 8000;
            }
            public static class CorruptingPotion
            {
                public static string BuffName = "ItemDarkCrystalFlask";
                public static float RegenRate = 12.5f;
                public static float Duration = 12000;
            }
        }
    }
}
