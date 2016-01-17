using System;
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
        private Menu allyconfig;

        public Main()
        {
            config = MainMenu.AddMenu("HumanizedBaseUlts", "humanizedBaseUlts");
            config.Add("on", new CheckBox("Enabled"));
            config.Add("min20", new CheckBox("20 min passed"));
            config.Add("minDelay", new Slider("Minimum required delay", 750, 0, 2500));
            config.AddLabel("The time to let the enemy regenerate health in base");
            config.AddSeparator(20);
            config.Add("fountainReg", new Slider("Enemy regeneration speed", 87, 84, 90));
            config.Add("fountainRegMin20", new Slider("Enemy regeneration speed after minute 20", 368, 350, 390));

            allyconfig = config.AddSubMenu("Premades");
            foreach (var source in EntityManager.Heroes.Allies.Where(x => !x.IsMe && Listing.spellDataList.Any(y =>
                x.ChampionName == y.championName)))
            {
                allyconfig.Add(source.ChampionName, new CheckBox(source.ChampionName));
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
            foreach (var enemyInst in Listing.teleportingEnemies)
            {
                var enemy = enemyInst.Sender;
                var invisEntry = Listing.invisEnemiesList.First(x => x.sender.Equals(enemy));

                int recallEndTime = enemyInst.StartTick + enemyInst.Duration;
                float timeLeft = recallEndTime - Core.GameTickCount;
                float travelTime = GetUltTravelTime(me);
                
                float regedHealthRecallFinished = Algorithm.SimulateHealthRegen(enemy, invisEntry.StartTime, recallEndTime);
                float totalEnemyHp = enemy.Health + regedHealthRecallFinished;

                float myDmg = Damage.GetBaseUltSpellDamage(enemy, me.ChampionName);

                if (myDmg > totalEnemyHp)
                {
                    float fountainReg = GetFountainReg(enemy);

                    // totalEnemyHp + fountainReg * seconds = myDmg
                    var waitRegMSeconds = ((myDmg - totalEnemyHp) / fountainReg) * 1000;
                    if (waitRegMSeconds < config.Get<Slider>("minDelay").CurrentValue)
                        continue;

                    Messaging.ProcessInfo(waitRegMSeconds, enemy.ChampionName);

                    if (travelTime > timeLeft + waitRegMSeconds && travelTime - (timeLeft + waitRegMSeconds) < 250)
                    {
                        if (!Algorithm.GetCollision(me.ChampionName).Any())
                        {
                            Vector3 enemyBaseVec = ObjectManager.Get<Obj_SpawnPoint>().First(x => x.IsEnemy).Position;
                            Player.CastSpell(SpellSlot.R, enemyBaseVec);
                        }
                    }
                }
                else
                {
                    //EloBuddy.Messaging.Print(enemy.ChampionName + " not enough dmg: " + myDmg + " < " + totalEnemyHp);
                    Listing.teleportingEnemies.Remove(enemyInst);
                }
            }
        }

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

        public float GetUltTravelTime(AIHeroClient source)
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
    }
}
