using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace HumanziedBaseUlt
{
    class Main
    {
        class UltSpellDataS
        {
            public string championName;
            public int SpellStage;
            public float DamageMultiplicator;
            public float Width;
            public float Delay;
            public float Speed;
            public bool Collision;
        }

        readonly List<UltSpellDataS> spellDataList = new List<UltSpellDataS>
        {
            new UltSpellDataS { championName = "Jinx", SpellStage = 1, DamageMultiplicator = 1.0f, Width = 140f, Delay = 0600f/1000f, Speed = 1700f, Collision = true},
            new UltSpellDataS { championName = "Ashe", SpellStage = 0, DamageMultiplicator = 1.0f, Width = 130f, Delay = 0250f/1000f, Speed = 1600f, Collision = true},
            //new UltSpellDataS { championName = "Draven", SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 0400f/1000f, Speed = 2000f, Collision = true},
            new UltSpellDataS { championName = "Ezreal", SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 1000f/1000f, Speed = 2000f, Collision = false},
            //new UltSpellDataS { championName = "Karthus", SpellStage = 0, DamageMultiplicator = 1.0f, Width = 000f, Delay = 3125f/1000f, Speed = 0000f, Collision = false}
        };
        private AIHeroClient me = ObjectManager.Player;


        public class InvisibleEventArgs : EventArgs
        {
            public int StartTime { get; set; }
            public AIHeroClient sender { get; set; }
            public float LastHealth { get; set; }

            /// <summary>
            /// per second
            /// </summary>
            public float HealthRegen { get; set; }
        }

        public delegate void OnEnemyInvisibleH(InvisibleEventArgs args);
        public event OnEnemyInvisibleH OnEnemyInvisible;
        protected virtual void FireOnEnemyInvisible(InvisibleEventArgs args)
        {
            if (OnEnemyInvisible != null) OnEnemyInvisible(args);
        }

        public delegate void OnEnemyVisibleH(AIHeroClient sender);
        public event OnEnemyVisibleH OnEnemyVisible;
        protected virtual void FireOnEnemyVisible(AIHeroClient sender)
        {
            if (OnEnemyVisible != null) OnEnemyVisible(sender);
        }

        private Menu config;
        public Main()
        {
            config = MainMenu.AddMenu("HumanizedBaseUlts", "huminizedBaseUlts");
            config.Add("on", new CheckBox("Enabled"));
            config.Add("min20", new CheckBox("20 min passed", false));
            config.Add("minDelay", new Slider("Minimum required delay", 750, 0, 2500));

            Game.OnUpdate += GameOnOnUpdate;
            Teleport.OnTeleport += TeleportOnOnTeleport;
            OnEnemyInvisible += OnOnEnemyInvisible;
            OnEnemyVisible += OnOnEnemyVisible;
        }

        class PortingEnemy
        {
            public AIHeroClient Sender { get; set; }
            public float TotalRegedHealthSinceInvis { get; set; }
            public int StartTick { get; set; }

            public int Duration { get; set; }
        }

        readonly List<PortingEnemy> teleportingEnemies = new List<PortingEnemy>(5); 
        private void TeleportOnOnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            var invisEnemiesEntry = invisEnemiesList.FirstOrDefault(x => x.sender == sender);

            switch (args.Status)
            {
                case TeleportStatus.Start:
                    if (invisEnemiesEntry == null)
                        return;
                    if (teleportingEnemies.All(x => x.Sender != sender))
                    {
                        float lastHpReg = lastEnemyRegens.First(x => x.Key.Equals(sender)).Value;
                        float invisTime = (Environment.TickCount - invisEnemiesEntry.StartTime) / 1000;
                        float totalRegedHp = lastHpReg*invisTime;
                        teleportingEnemies.Add(new PortingEnemy
                        {
                            Sender = (AIHeroClient) sender,
                            Duration = args.Duration,
                            StartTick = args.Start,
                            TotalRegedHealthSinceInvis = totalRegedHp
                        });
                    }
                    break;
                case TeleportStatus.Abort:
                    var teleportingEnemiesEntry = teleportingEnemies.First(x => x.Sender.Equals(sender));
                    teleportingEnemies.Remove(teleportingEnemiesEntry);
                    break;

                case TeleportStatus.Finish:
                    var teleportingEnemiesEntry2 = teleportingEnemies.First(x => x.Sender.Equals(sender));
                    Core.DelayAction(() => teleportingEnemies.Remove(teleportingEnemiesEntry2), 5000);
                    break;
            }
        }

        readonly List<AIHeroClient> visibleEnemies = new List<AIHeroClient>(5);
        private void OnOnEnemyVisible(AIHeroClient sender)
        {
            visibleEnemies.Add(sender);
            var invisEntry = invisEnemiesList.First(x => x.sender.Equals(sender));
            invisEnemiesList.Remove(invisEntry);
        }

        readonly List<InvisibleEventArgs> invisEnemiesList = new List<InvisibleEventArgs>(5); 
        private void OnOnEnemyInvisible(InvisibleEventArgs args)
        {
            visibleEnemies.Remove(args.sender);
            invisEnemiesList.Add(args);
        }

        private int lastHealthRegenCheck = 0;
        readonly Dictionary<AIHeroClient, float> lastEnemyHealths = new Dictionary<AIHeroClient, float>(5);
        readonly Dictionary<AIHeroClient, float> lastEnemyRegens = new Dictionary<AIHeroClient, float>(5);
        private void GameOnOnUpdate(EventArgs args)
        {
            if (!config.Get<CheckBox>("on").CurrentValue)
                return;

            if (Environment.TickCount - lastHealthRegenCheck >= 1000)
            {
                lastHealthRegenCheck = Environment.TickCount;
                CheckEnemyRegenartions();
            }

            CheckForInvisibility();          

            CheckRecallingEnemies();
        }

        private void CheckRecallingEnemies()
        {
            if (!teleportingEnemies.Any())
                return;

            foreach (var enemyInst in teleportingEnemies)
            {
                var enemy = enemyInst.Sender;
                int endTime = enemyInst.StartTick + enemyInst.Duration;
                float timeLeft = endTime - Core.GameTickCount;
                float travelTime = GetUltTravelTime(me);

                float lastEnemyReg = lastEnemyRegens.First(x => x.Key.Equals(enemy)).Value;
                float recallHpReg = lastEnemyReg * (enemyInst.Duration / 1000);
                float regedHealthTillRecallFinished = enemyInst.TotalRegedHealthSinceInvis + recallHpReg;
                float totalEnemyHp = enemy.Health + regedHealthTillRecallFinished;

                float myDmg = Damage.GetBaseUltSpellDamage(enemy, me.ChampionName);

                
                if (myDmg > totalEnemyHp)
                {
                    // totalEnemyHp + fountainReg * seconds = myDmg
                    
                    float fountainReg = config.Get<CheckBox>("min20").CurrentValue ?
                        enemy.MaxHealth / 100 * 37f
                        : enemy.MaxHealth / 100 * 8.7f;
                    var waitRegMSeconds = ((myDmg - totalEnemyHp)/fountainReg) *1000;
                    if (waitRegMSeconds < config.Get<Slider>("minDelay").CurrentValue)
                        continue;
                    Chat.Print("OK /" + enemyInst.Sender.ChampionName + "|| " + waitRegMSeconds);

                    if (travelTime > timeLeft + waitRegMSeconds && travelTime - (timeLeft + waitRegMSeconds) < 250)
                    {
                        Vector3 enemyBaseVec = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position;
                        Player.CastSpell(SpellSlot.R, enemyBaseVec);
                        teleportingEnemies.Remove(enemyInst);
                    }
                }
                else
                {
                    Chat.Print(myDmg + "< " + totalEnemyHp + " //" + enemyInst.Sender.ChampionName);
                    teleportingEnemies.Remove(enemyInst);
                }
            }
        }

        private void CheckForInvisibility()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsHPBarRendered && !visibleEnemies.Contains(enemy))
                {
                    FireOnEnemyVisible(enemy);
                }
                else if (!enemy.IsHPBarRendered && visibleEnemies.Contains(enemy))
                {
                    FireOnEnemyInvisible(new InvisibleEventArgs
                    {
                        StartTime = Environment.TickCount,
                        LastHealth = enemy.Health,
                        sender = enemy,
                        HealthRegen = lastEnemyRegens.ContainsKey(enemy) ? lastEnemyRegens[enemy] : 3f
                    });
                }
            }
        }

        private void CheckEnemyRegenartions()
        {
            /*Updating last regen list*/
            if (lastEnemyHealths.Any())
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    KeyValuePair<AIHeroClient, float> lastEntry = lastEnemyHealths.First(x => x.Key.Equals(enemy));
                    float dhp = enemy.Health - lastEntry.Value;
                    if (dhp > float.Epsilon)
                    {
                        if (lastEnemyRegens.ContainsKey(enemy))
                            lastEnemyRegens.Remove(enemy);
                        lastEnemyRegens.Add(enemy, dhp);
                    }
                }
            }

            /*Updating last health list*/
            lastEnemyHealths.Clear();
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                lastEnemyHealths.Add(enemy, enemy.Health);
            }
        }

        public float GetUltTravelTime(AIHeroClient source)
        {
            try
            {
                var targetpos = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy);
                float speed = spellDataList.First(x => x.championName == source.ChampionName).Speed;
                float delay = spellDataList.First(x => x.championName == source.ChampionName).Delay;


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
