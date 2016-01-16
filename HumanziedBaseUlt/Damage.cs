using EloBuddy;
using EloBuddy.SDK;

namespace HumanziedBaseUlt
{
    class Damage
    {
        public static float GetBaseUltSpellDamage(AIHeroClient target, string sourceChampionName)
        {
            var level = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level - 1;
            if (sourceChampionName == "Jinx")
            {
                
                {
                    var damage = new float[] {250, 350, 450}[level] +
                                 new float[] {25, 30, 35}[level]/100*(target.MaxHealth - target.Health) +
                                 1*ObjectManager.Player.FlatPhysicalDamageMod;
                    return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Physical, damage);
                }
            }
            if (sourceChampionName == "Ezreal")
            {
                {
                    var damage = new float[] {350, 500, 650}[level] + 0.9f*ObjectManager.Player.FlatMagicDamageMod +
                                 1*ObjectManager.Player.FlatPhysicalDamageMod;
                    return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical, damage)*0.7f;
                }
            }
            if (sourceChampionName == "Ashe")
            {
                {
                    var damage = new float[] {250, 425, 600}[level] + 1*ObjectManager.Player.FlatMagicDamageMod;
                    return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical, damage);
                }
            }
            if (sourceChampionName == "Draven")
            {
                {
                    var damage = new float[] {175, 275, 375}[level] + 1.1f*ObjectManager.Player.FlatPhysicalDamageMod;
                    return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Physical, damage)*0.7f;
                }
            }

            return 0;
        }
    }
}
