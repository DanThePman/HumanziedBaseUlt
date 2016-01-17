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
    class Main : Events
    {
        private readonly AIHeroClient me = ObjectManager.Player;
        private Menu config;

        public Main()
        {
            config = MainMenu.AddMenu("HumanizedBaseUlts", "humanizedBaseUlts");
            config.Add("on", new CheckBox("Enabled"));
            config.Add("min20", new CheckBox("20 min passed"));
            config.Add("minDelay", new Slider("Minimum required delay", 750, 0, 2500));
            config.AddLabel("The time to let the enemy regenerate health in base");
            config.AddSeparator(20);
            config.Add("fountainReg", new Slider("Enemy regeneration speed", 89, 84, 91));
            config.Add("fountainRegMin20", new Slider("Enemy regeneration speed after minute 20", 369, 350, 390));

            Listing.allyconfig = config.AddSubMenu("Premades");
            foreach (var ally in EntityManager.Heroes.Allies)
            {
                if (Listing.spellDataList.Any(x => x.championName == ally.ChampionName))
                    Listing.allyconfig.Add(ally.ChampionName + "/Premade", new CheckBox(ally.ChampionName, false));
            }

            Game.OnUpdate += GameOnOnUpdate;
            Teleport.OnTeleport += TeleportOnOnTeleport;
            OnEnemyInvisible += OnOnEnemyInvisible;
            OnEnemyVisible += OnOnEnemyVisible;
        }

        private void TeleportOnOnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            var invisEnemiesEntry = Listing.invisEnemiesList.FirstOrDefault(x => x.sender == sender);

            switch (args.Status)
            {
                case TeleportStatus.Start:
                    if (invisEnemiesEntry == null)
                        return;
                    if (Listing.teleportingEnemies.All(x => x.Sender != sender))
                    {
                        float lastHpReg = Listing.Regeneration.lastEnemyRegens.First(x => x.Key.Equals(sender)).Value;
                        float invisTime = (Core.GameTickCount - invisEnemiesEntry.StartTime) / 1000;
                        float totalRegedHp = lastHpReg*invisTime;
                        Listing.teleportingEnemies.Add(new Listing.PortingEnemy
                        {
                            Sender = (AIHeroClient) sender,
                            Duration = args.Duration,
                            StartTick = args.Start,
                            TotalRegedHealthSinceInvis = totalRegedHp
                        });
                    }
                    break;
                case TeleportStatus.Abort:
                    var teleportingEnemiesEntry = Listing.teleportingEnemies.First(x => x.Sender.Equals(sender));
                    Listing.teleportingEnemies.Remove(teleportingEnemiesEntry);
                    break;

                case TeleportStatus.Finish:
                    var teleportingEnemiesEntry2 = Listing.teleportingEnemies.First(x => x.Sender.Equals(sender));
                    Core.DelayAction(() => Listing.teleportingEnemies.Remove(teleportingEnemiesEntry2), 5000);
                    break;
            }
        }
              
        /*enemy appear*/
        private void OnOnEnemyVisible(AIHeroClient sender)
        {
            Listing.Regeneration.enemyBuffs.Remove(sender);

            Listing.visibleEnemies.Add(sender);
            var invisEntry = Listing.invisEnemiesList.First(x => x.sender.Equals(sender));
            Listing.invisEnemiesList.Remove(invisEntry);
        }
        
        /*enemy disappear*/
        private void OnOnEnemyInvisible(InvisibleEventArgs args)
        {
            if (Listing.Regeneration.HasPotionActive(args.sender))
                Listing.Regeneration.enemyBuffs.Add(args.sender, Listing.Regeneration.GetPotionBuff(args.sender));

            Listing.visibleEnemies.Remove(args.sender);
            Listing.invisEnemiesList.Add(args);
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            if (!config.Get<CheckBox>("on").CurrentValue)
                return;

            config.Get<CheckBox>("min20").CurrentValue = Game.Time > 1225f;

            Listing.Regeneration.CheckEnemyBaseRegenartions();

            CheckForInvisibility();

            CheckRecallingEnemies();
        }

        private void CheckRecallingEnemies()
        {
            foreach (var enemyInst in Listing.teleportingEnemies.OrderBy(x => x.Sender.Health + x.TotalRegedHealthSinceInvis))
            {
                var enemy = enemyInst.Sender;
                var invisEntry = Listing.invisEnemiesList.First(x => x.sender.Equals(enemy));

                int recallEndTime = enemyInst.StartTick + enemyInst.Duration;
                float timeLeft = recallEndTime - Core.GameTickCount;
                float travelTime = Algorithm.GetUltTravelTime(me);

                float regedHealthRecallFinished = Algorithm.SimulateHealthRegen(enemy, invisEntry.StartTime, recallEndTime);
                float totalEnemyHp = enemy.Health + regedHealthRecallFinished;
                float fountainReg = GetFountainReg(enemy);

                var aioDmg = Damage.GetAioDmg(enemy, timeLeft);

                if (aioDmg > totalEnemyHp)
                {
                    // totalEnemyHp + fountainReg * seconds = myDmg
                    var waitRegMSeconds = ((aioDmg - totalEnemyHp)/fountainReg)*1000;
                    if (waitRegMSeconds < config.Get<Slider>("minDelay").CurrentValue)
                        continue;

                    Messaging.ProcessInfo(waitRegMSeconds, enemy.ChampionName);

                    if (travelTime <= (timeLeft + waitRegMSeconds))
                    {
                        if (!Algorithm.GetCollision(me.ChampionName).Any())
                        {
                            Vector3 enemyBaseVec =
                                ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position;
                            float delay = timeLeft + waitRegMSeconds - travelTime;
                            Chat.Print(delay);
                            Core.DelayAction(() => Player.CastSpell(SpellSlot.R, enemyBaseVec), (int)delay);
                            Listing.teleportingEnemies.Remove(enemyInst);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// per second
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        private float GetFountainReg(AIHeroClient enemy)
        {
            float regSpeedDefault = config.Get<Slider>("fountainReg").CurrentValue / 10;
            float regSpeedMin20 = config.Get<Slider>("fountainRegMin20").CurrentValue / 10;


            float fountainReg = config.Get<CheckBox>("min20").CurrentValue ? enemy.MaxHealth / 100 * regSpeedMin20 : 
                                    enemy.MaxHealth / 100 * regSpeedDefault;

            return fountainReg;
        }

        private void CheckForInvisibility()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsHPBarRendered && !Listing.visibleEnemies.Contains(enemy))
                {
                    FireOnEnemyVisible(enemy);
                }
                else if (!enemy.IsHPBarRendered && Listing.visibleEnemies.Contains(enemy))
                {
                    FireOnEnemyInvisible(new InvisibleEventArgs
                    {
                        StartTime = Core.GameTickCount,
                        sender = enemy,
                        StdHealthRegen = Listing.Regeneration.lastEnemyRegens[enemy]
                    });
                }
            }
        }
    }
}
