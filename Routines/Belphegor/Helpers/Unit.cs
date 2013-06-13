using System;
using System.Collections.Generic;
using System.Linq;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.Internals.SNO;

namespace Belphegor.Helpers
{
    internal static class Unit
    {
        private static int _pulsesSinceLastReset;

        /// <summary>
        /// List of all known treasure goblins
        /// </summary>
        private static readonly HashSet<int> TreasureGoblin = new HashSet<int>
                                                                  {
                                                                      5984, //Goblin A
                                                                      5985, //Goblin B
                                                                      5987, //Goblin C
                                                                      5988 //Goblin D
                                                                  };

        private static readonly HashSet<int> FollowerBlackList = new HashSet<int>
                                                                       {
                                                                           4482, // Enchantress
                                                                           52693, // Templer
                                                                           52694 // Scoundrel
                                                                       };

        private static readonly Dictionary<int, bool> IsEliteCache = new Dictionary<int, bool>();

        static Unit()
        {
            Pulsator.OnPulse += Pulsator_OnPulse;
        }

        /// <summary>
        /// Checks if we are Blind, Feared, Frozen, Stunned or Rooted
        /// </summary>
        public static bool IsMeIncapacited
        {
            get
            {
                DiaActivePlayer me = ZetaDia.Me;
                return me.IsFeared || me.IsStunned || me.IsFrozen || me.IsBlind || me.IsRooted;
            }
        }

        /// <summary>
        /// Checks if we are Blind, Feared, Frozen or Stunned
        /// </summary>
        public static bool IsMeFearedStunnedFrozenOrBlind
        {
            get
            {
                DiaActivePlayer me = ZetaDia.Me;
                return me.IsFeared || me.IsStunned || me.IsFrozen || me.IsBlind;
            }
        }

        private static void Pulsator_OnPulse(object sender, EventArgs e)
        {
            if (_pulsesSinceLastReset == 100)
            {
                _pulsesSinceLastReset = 0;
                IsEliteCache.Clear();
            }
            else
            {
                _pulsesSinceLastReset++;
            }
        }

        public static bool IsSNOContainedInFollowerBlacklist(this int item)
        {
            return FollowerBlackList.Contains(item);
        }

        /// <summary>
        /// Checks if the mob is Elite or Rare
        /// </summary>
        /// <param name="unit">DiaUnit</param>
        /// <returns>True if Current unit is Elite</returns>
        public static bool IsElite(this DiaUnit unit)
        {
            ACD commonData = unit.CommonData;
            if (unit.IsValid && commonData != null)
            {
                int key = commonData.DynamicId;

                if (!IsEliteCache.ContainsKey(key))
                {
                    MonsterAffixes affixes = commonData.MonsterAffixes;
                    IsEliteCache.Add(key,
                                      affixes.HasFlag(MonsterAffixes.Elite) || affixes.HasFlag(MonsterAffixes.Rare) ||
                                      affixes.HasFlag(MonsterAffixes.Unique) || TreasureGoblin.Contains(unit.ActorSNO));
                }
                return IsEliteCache[key];
            }
            return false;
        }

        /// <summary>
        /// Checks if the mob is Elite or Rare && with range
        /// </summary>
        /// <param name="unit">DiaUnit</param>
        /// <param name="range">Range from us to unit</param>
        /// <returns>True if current uint is Elite && unit is in range</returns>
        public static bool IsElite(this DiaUnit unit, float range)
        {
            Vector3 myLoc = ZetaDia.Me.Position;
            return unit.IsElite() && unit.Position.DistanceSqr(myLoc) <= range*range;
        }

        /// <summary>
        /// Checks if any unit is elite within a set range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public static bool IsEliteInRange(float range)
        {
            return CombatTargeting.Instance.LastObjects.OfType<DiaUnit>().Any(u => (u).IsElite(range));
        }

        private static readonly Dictionary<int, MonsterSize> _monsterSizeCache = new Dictionary<int, MonsterSize>();
        private static readonly Dictionary<int, float> _monsterDistanceModifierCache = new Dictionary<int, float>();

        public static MonsterSize GetMonsterSize(this DiaUnit u)
        {
            int sno = u.ActorSNO;
            if (!_monsterSizeCache.ContainsKey(sno))
                _monsterSizeCache.Add(sno, u.MonsterInfo.MonsterSize);
            return _monsterSizeCache[sno];
        }

        public static float GetMonsterDistanceModifier(this DiaUnit u)
        {
            int sno = u.ActorSNO;
            if (!_monsterDistanceModifierCache.ContainsKey(sno))
                _monsterDistanceModifierCache.Add(sno, u.CollisionSphere.Radius);
            return _monsterDistanceModifierCache[sno];
        }
    }
}