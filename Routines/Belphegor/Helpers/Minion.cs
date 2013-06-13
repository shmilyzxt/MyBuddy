using System.Collections.Generic;
using System.Linq;
using Belphegor.Utilities;
using Zeta;
using Zeta.Internals.Actors;

namespace Belphegor.Helpers
{
    internal class Minion
    {
        private static readonly HashSet<int> MysticAlly = new HashSet<int>
                                                              {
                                                                  169123, //Femal Air
                                                                  123885, //Female No Rune
                                                                  169890, //Female Water
                                                                  168878, //Female Fire
                                                                  169891, //Female Eternal
                                                                  169077, //Female Earth

                                                                  //Male Version Credit to Tesslerc
                                                                  169904, // Male No Rune
                                                                  169907, // Male Water
                                                                  169906, // Male Fire
                                                                  169908, // Male Air
                                                                  169905, // Male Eternal
                                                                  169909, // Male Earth
                                                              };

        private static readonly HashSet<int> DH_Companion = new HashSet<int>
                                                                {
                                                                    133741, //DH_Companion
                                                                    159098, //DH_Companion_RuneC
                                                                    159102, //DH_Companion_RuneD
                                                                    159144, //DH_Companion_RuneE
                                                                    178664, //DH_Companion_Ferret
                                                                    173827, //DH_Companion_Spider
                                                                    181748, //DH_Companion_Boar
                                                                };

        private static readonly HashSet<int> DH_Sentry = new HashSet<int>
                                                             {
                                                                 150024, //DH_sentry_addsDuration
                                                                 150025, //DH_sentry_addsMissiles
                                                                 150026, //DH_sentry_addsHeals
                                                                 150027, //DH_sentry_addsShield
                                                                 141402, //DH_sentry
                                                                 168815, //DH_sentry_tether
                                                             };

        private static readonly HashSet<int> DH_Caltrops = new HashSet<int>
                                                               {
                                                                   196030, //DH_caltrops_inactive_proxyActor
                                                                   129784, //DH_caltrops_unruned
                                                                   155848, //DH_caltrops_runeD_reduceDiscipline
                                                                   155734, //DH_caltrops_runeB_slower
                                                                   155376, //DH_caltrops_runeE_empower
                                                                   155159, //DH_caltrops_runeC_weakenMonsters
                                                                   154811, //DH_caltrops_runeA_damage
                                                               };

        private static readonly HashSet<int> DH_SpikeTrap = new HashSet<int>
                                                                {
                                                                    111330, //DemonHunter_SpikeTrap_Proxy
                                                                    194565,
                                                                    //DemonHunter_SpikeTrapRune_chainLightning_Proxy
                                                                    158843, //DemonHunter_SpikeTrapRune_damage_Proxy
                                                                    158916, //DemonHunter_SpikeTrapRune_explode_Proxy
                                                                    158941, //DemonHunter_SpikeTrapRune_multiTrap_Proxy
                                                                };

        private static readonly HashSet<int> Hydra = new HashSet<int>
                                                         {
                                                             80758, //No Rune
                                                             81231, //Arcane
                                                             81229, //Lightning
                                                             81227, //Venom
                                                             83025, //Frost
                                                             83959, //Mammoth
                                                         };

        private static readonly HashSet<int> Gargantuan = new HashSet<int>
                                                              {
                                                                  122305, //No Rune
                                                                  179778, //Humungoid
                                                                  179780, //Relentless
                                                                  179772, //Wrathfull
                                                                  179776, //WD_Gargantuan_Absorb 
                                                                  179779, //WD_Gargantuan_Poison
                                                              };

        private static readonly HashSet<int> ZombieDogs = new HashSet<int>
                                                              {
                                                                  51353, //No Rune
                                                                  103217, //Rabid
                                                                  105763, //Final Gift
                                                                  110959, //Life Link
                                                                  103215, // Burning
                                                                  103235, //Leeching
                                                              };

        /// <summary>
        /// Determins if you have a pet summoned by you
        /// </summary>
        /// <param name="pet">Type of Pet</param>
        /// <returns></returns>
        public static bool HasPet(Pet pet)
        {
            return GetMinions(pet).Any();
        }

        /// <summary>
        /// Counts how many minions you got summoned.
        /// </summary>
        /// <param name="pet">Type of Pet</param>
        /// <returns></returns>
        public static int PetCount(Pet pet)
        {
            return GetMinions(pet).Count();
        }

        /// <summary>
        /// Get the minion summoned by you for the pet type.
        /// </summary>
        /// <param name="minion"></param>
        /// <returns></returns>
        public static IEnumerable<DiaUnit> GetMinions(Pet minion)
        {
            int dynId = ZetaDia.Me.CommonData.DynamicId;
            return
                ZetaDia.Actors.GetActorsOfType<DiaUnit>().Where(
                    u => u.IsValid && u.IsACDBased && MinionCheck(minion, u) && u.SummonedByACDId == dynId);
        }

        private static bool MinionCheck(Pet minion, DiaUnit unit)
        {
            switch (minion)
            {
                case Pet.MysticAlly:
                    return MysticAlly.Contains(unit.ActorSNO);
                case Pet.Hydra:
                    return Hydra.Contains(unit.ActorSNO);
                case Pet.DH_Caltrops:
                    return DH_Caltrops.Contains(unit.ActorSNO);
                case Pet.DH_Companion:
                    return DH_Companion.Contains(unit.ActorSNO);
                case Pet.DH_Sentry:
                    return DH_Sentry.Contains(unit.ActorSNO);
                case Pet.DH_SpikeTrap:
                    return DH_SpikeTrap.Contains(unit.ActorSNO);
                case Pet.Gargantuan:
                    return Gargantuan.Contains(unit.ActorSNO);
                case Pet.ZombieDogs:
                    return ZombieDogs.Contains(unit.ActorSNO);
                case Pet.Enchantress:
                    return unit.ActorSNO == 4482;
                case Pet.Templer:
                    return unit.ActorSNO == 52693;
                case Pet.Scoundrel:
                    return unit.ActorSNO == 52694;
                default:
                    return false;
            }
        }
    }
}