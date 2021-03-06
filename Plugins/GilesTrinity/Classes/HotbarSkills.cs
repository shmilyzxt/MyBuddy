﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta;
using Zeta.Internals;
using Zeta.Internals.Actors;

namespace GilesTrinity
{
    public class HotbarSkills
    {
        private static HashSet<HotbarSkills> _assignedSkills = new HashSet<HotbarSkills>();
        /// <summary>
        /// The currently assigned hotbar skills with runes and slots
        /// </summary>
        internal static HashSet<HotbarSkills> AssignedSkills
        {
            get
            {
                if (_assignedSkills == null)
                {
                    _assignedSkills = new HashSet<HotbarSkills>();
                    Update();
                }
                return _assignedSkills;
            }
            set
            {
                _assignedSkills = value;
            }
        }

        public HotbarSlot Slot { get; set; }
        public SNOPower Power { get; set; }
        public int RuneIndex { get; set; }

        public HotbarSkills()
        {

        }

        /// <summary>
        /// Updates AssignedSkills
        /// </summary>
        public static void Update()
        {
            if (GilesTrinity.PlayerStatus.ActorClass != ActorClass.Wizard && !GilesTrinity.GetHasBuff(SNOPower.Wizard_Archon) &&
                GilesTrinity.PlayerStatus.ActorClass != ActorClass.WitchDoctor && !GilesTrinity.GetHasBuff(SNOPower.Witchdoctor_Hex))
            {
                _assignedSkills.Clear();
                foreach (SNOPower p in GilesTrinity.Hotbar)
                {
                    _assignedSkills.Add(new HotbarSkills()
                    {
                        Power = p,
                        Slot = HotbarSkills.GetHotbarSlotFromPower(p),
                        RuneIndex = HotbarSkills.GetRuneIndexFromPower(p)
                    });
                }
            }
        }


        /// <summary>
        /// Returns the slot index (0-5) for the given SNOPower, returns HotbarSlot.Invalid if not in hotbar
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        private static HotbarSlot GetHotbarSlotFromPower(SNOPower power)
        {
            if (GilesTrinity.Hotbar.Contains(power))
            {
                for (int i = 0; i < 6; i++)
                {
                    if (cPlayer.GetPowerForSlot((HotbarSlot)i) == power)
                    {
                        return (HotbarSlot)i;
                    }
                }
            }
            return HotbarSlot.Invalid;
        }

        /// <summary>
        /// Returns the rune index for the given SNOPower. If rune can't be found returns -999
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        private static int GetRuneIndexFromPower(SNOPower power)
        {
            int runeIndex = -999;
            HotbarSlot slot = GetHotbarSlotFromPower(power);

            if (slot != HotbarSlot.Invalid)
            {
                return cPlayer.GetRuneIndexForSlot(slot);
            }

            return runeIndex;
        }

        private static CPlayer cPlayer { get { return ZetaDia.CPlayer; } }

    }

}
