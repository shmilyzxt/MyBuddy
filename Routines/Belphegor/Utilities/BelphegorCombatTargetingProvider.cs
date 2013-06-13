using System;
using System.Collections.Generic;
using System.Linq;
using Belphegor.Helpers;
using Belphegor.Settings;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Settings;
using Zeta.Internals.Actors;
using Zeta.Internals.SNO;

namespace Belphegor.Utilities
{
    public class BelphegorCombatTargetingProvider : ITargetingProvider
    {
        private static readonly BelphegorCombatTargetingProvider _Instance = new BelphegorCombatTargetingProvider();

        private static readonly HashSet<int> ActorBlackList = new HashSet<int>
                                                                   {
                                                                       5840,
                                                                       111456,
                                                                       5013,
                                                                       5014,
                                                                       205756,
                                                                       205746,
                                                                       4182,
                                                                       4183,
                                                                       4644,
                                                                       4062,
                                                                       4538,
                                                                       52693,
                                                                       162575,
                                                                       2928,
                                                                       51291,
                                                                       51292,
                                                                       96132,
                                                                       90958,
                                                                       90959,
                                                                       80980,
                                                                       51292,
                                                                       51291,
                                                                       2928,
                                                                       3546,
                                                                       129345,
                                                                       81857,
                                                                       138428,
                                                                       81857,
                                                                       60583,
                                                                       170038,
                                                                       174854,
                                                                       190390,
                                                                       194263,
                                                                       5482,
                                                                       174900,
                                                                       219702,
                                                                       221225,
                                                                       87189,
                                                                       90072,
                                                                       107031,
                                                                       106584,
                                                                       186130,
                                                                       187265,
                                                                       201426,
                                                                       201242,
                                                                       200969,
                                                                       201423,
                                                                       201438,
                                                                       201464,
                                                                       201454,
                                                                       108012,
                                                                       103279,
                                                                       89578,
                                                                       74004,
                                                                       84531,
                                                                       84538,
                                                                       89579,
                                                                       190492,
                                                                       209133,
                                                                       6318,
                                                                       107705,
                                                                       105681,
                                                                       89934,
                                                                       89933,
                                                                       182276,
                                                                       117574,
                                                                       182271,
                                                                       182283,
                                                                       182278,
                                                                       128895,
                                                                       81980,
                                                                       82111,
                                                                       81226,
                                                                       81227,
                                                                       249320
                                                                   };

        private readonly Dictionary<int, float> _distanceCache = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _isConsideredCache = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> _isTownVendorCache = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> _isValidCache = new Dictionary<int, bool>();

        private readonly Dictionary<int, MonsterType> _monsterTypeCache = new Dictionary<int, MonsterType>();
        private readonly Dictionary<int, float> _predeterminedWeightCache = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _staticDataValidCache = new Dictionary<int, bool>();
        private readonly Dictionary<int, float> _weightCache = new Dictionary<int, float>();
        private List<DiaObject> _cache;
        private bool _isUpdateNeeded;

        private HashSet<int> _blacklistedActorsFromProfile;
        private float _killRadiusSqr;
        private Vector3 _playerCurrentPosition;
        private int _pulsesSinceLastReset;

        /// <summary>
        /// Initializes a new instance of the BelphegorCombatTargetingProvider class.
        /// </summary>
        public BelphegorCombatTargetingProvider()
        {
            Pulsator.OnPulse += ResetCache;
        }

        public static BelphegorCombatTargetingProvider Instance
        {
            get { return _Instance; }
        }

        #region ITargetingProvider Members

        List<DiaObject> ITargetingProvider.GetObjectsByWeight()
        {
            using (
                new PerformanceLogger(BelphegorSettings.Instance.Debug.IsDebugTargetProviderLoggingActive,
                                      "GetObjectsByWeight"))
            {
                if (!CombatTargeting.Instance.AllowedToKillMonsters)
                    return new List<DiaObject>();
                if (_isUpdateNeeded || _cache == null)
                {
                    _playerCurrentPosition = ZetaDia.Me.Position;
                    _killRadiusSqr = CharacterSettings.Instance.KillRadius*CharacterSettings.Instance.KillRadius;
                    _cache = ZetaDia.Actors.GetActorsOfType<DiaUnit>()
                        .Where(IsConsidered).Take(20)
                        .Select(u => new
                                         {
                                             Weight = GetWeight(u),
                                             Unit = u
                                         })
                        .OrderByDescending(wu => wu.Weight)
                        .Select(wu => wu.Unit as DiaObject)
                        .ToList();
                    _isUpdateNeeded = false;
                }
                return _cache;
            }
        }

        #endregion

        private void ResetCache(object sender, EventArgs e)
        {
            _isUpdateNeeded = true;
            if (_pulsesSinceLastReset%5 == 0)
            {
                _isConsideredCache.Clear();
                _predeterminedWeightCache.Clear();
                _distanceCache.Clear();
                _weightCache.Clear();
            }
            if (_pulsesSinceLastReset%10 == 0)
            {
                _isValidCache.Clear();
            }
            if (_pulsesSinceLastReset == 100)
            {
                _blacklistedActorsFromProfile =
                    new HashSet<int>(ProfileManager.CurrentProfile.TargetBlacklists.Select(i => i.ActorId));
                _predeterminedWeightCache.Clear();
                _pulsesSinceLastReset = 0;
            }
            else
            {
                _pulsesSinceLastReset++;
            }
        }

        private MonsterType GetMonsterType(DiaUnit u)
        {
            int sno = u.ActorSNO;
            if (!_monsterTypeCache.ContainsKey(sno))
                _monsterTypeCache.Add(sno, u.MonsterInfo.MonsterType);
            return _monsterTypeCache[sno];
        }

        private bool IsTownVendor(DiaUnit u)
        {
            int sno = u.ActorSNO;
            if (!_isTownVendorCache.ContainsKey(sno))
                _isTownVendorCache.Add(sno, u.IsTownVendor);
            return _isTownVendorCache[sno];
        }

        private float GetSizeWeight(DiaUnit u)
        {
            float sizeWeight = 1f;
            MonsterSize ms = u.GetMonsterSize();
            switch (ms)
            {
                case MonsterSize.Boss:
                    sizeWeight = 1.5f;
                    break;
                case MonsterSize.Swarm:
                    sizeWeight = 1.4f;
                    break;
                case MonsterSize.Ranged:
                    sizeWeight = 1.3f;
                    break;
            }
            return sizeWeight;
        }

        private float GetPredeterminedWeight(DiaUnit u)
        {
            ACD commonData = u.CommonData;
            int dynId = commonData.DynamicId;
            if (!_predeterminedWeightCache.ContainsKey(dynId))
            {
                float weight = 1f;
                weight *= u.IsElite() ? 1.5f : 0.5f;
                weight *= GetSizeWeight(u);
                float prioMulti =
                    ProfileManager.CurrentProfile.TargetPriorities.Where(prio => prio.ActorId == u.ActorSNO).Select(
                        prio => prio.Multiplier).FirstOrDefault();
                if (prioMulti != 0f)
                {
                    weight *= prioMulti;
                }
                _predeterminedWeightCache.Add(dynId, weight);
            }
            return _predeterminedWeightCache[dynId];
        }

        private bool IsValidMonsterType(DiaUnit u)
        {
            MonsterType currentMt = GetMonsterType(u);
            return currentMt != MonsterType.Ally &&
                   currentMt != MonsterType.Scenery &&
                   currentMt != MonsterType.Helper &&
                   currentMt != MonsterType.Team;
        }

        private bool IsStaticDataValid(DiaUnit u)
        {
            int key = u.ActorSNO;
            if (!_staticDataValidCache.ContainsKey(key))
            {
                _staticDataValidCache.Add(key,
                                          !ActorBlackList.Contains(key) && !IsTownVendor(u) && IsValidMonsterType(u));
            }
            return _staticDataValidCache[key] && !_blacklistedActorsFromProfile.Contains(key);
        }

        private bool IsConsidered(DiaUnit u)
        {
            if (u != null && u.IsValid && IsStaticDataValid(u) && u.IsACDBased)
            {
                ACD commonData = u.CommonData;
                int key = commonData.DynamicId;
                if (!_isConsideredCache.ContainsKey(key))
                {
                    _isConsideredCache.Add(key,
                                           GetDistance(u, key) <= _killRadiusSqr && IsValid(commonData) &&
                                           u.InLineOfSight);
                }
                return _isConsideredCache[key];
            }
            return false;
        }

        private float GetDistance(DiaUnit u, int dynamicId)
        {
            if (!_distanceCache.ContainsKey(dynamicId))
                _distanceCache.Add(dynamicId, u.Position.DistanceSqr(_playerCurrentPosition));
            return _distanceCache[dynamicId];
        }

        private float GetWeight(DiaUnit u)
        {
            ACD commonData = u.CommonData;
            int key = commonData.DynamicId;
            if (!_weightCache.ContainsKey(key))
            {
                float weight = GetPredeterminedWeight(u);
                float distPct = 1 - ((1/(_killRadiusSqr))*GetDistance(u, key));
                weight *= distPct;
                weight *= commonData.GetAttribute<float>(ActorAttributeType.HitpointsMaxTotal)/
                          commonData.GetAttribute<float>(ActorAttributeType.HitpointsCur);
                _weightCache.Add(key, weight);
            }
            return _weightCache[key];
        }

        private bool IsValid(ACD commonData)
        {
            if (!_isValidCache.ContainsKey(commonData.DynamicId))
            {
                _isValidCache.Add(commonData.DynamicId,
                                  commonData.GetAttribute<int>(ActorAttributeType.Untargetable) == 0 &&
                                  commonData.GetAttribute<int>(ActorAttributeType.Burrowed) == 0 &&
                                  commonData.GetAttribute<int>(ActorAttributeType.SlowdownImmune) == 0 &&
                                  commonData.GetAttribute<int>(ActorAttributeType.Uninterruptible) == 0 &&
                                  commonData.GetAttribute<int>(ActorAttributeType.Invulnerable) == 0 &&
                                  commonData.GetAttribute<int>(ActorAttributeType.StunImmune) == 0 &&
                                  commonData.GetAttribute<int>(ActorAttributeType.RootImmune) == 0 &&
                                  commonData.GetAttribute<float>(ActorAttributeType.HitpointsCur) > 0);
            }
            return _isValidCache[commonData.DynamicId];
        }
    }
}