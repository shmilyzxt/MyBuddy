using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;
using System;

namespace Belphegor.Settings
{
    [XmlElement("AvoidanceSettings")]
    public class AvoidanceSettings : XmlSettings
    {
        public AvoidanceSettings()
            : base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "AvoidanceSettings.xml"))
        {
        }

        [XmlElement("IsAvoidanceActive")]
        [DisplayName("Activate Avoidance")]
        [Description("Allows avoidance of ground effects.")]
        [DefaultValue(true)]
        [Setting]
        public bool IsAvoidanceActive { get; set; }

        [XmlElement("IsLocalPathingForPointGenerationActive")]
        [DisplayName("Use pathing validation")]
        [Description("When enabled local navigation is used for point validation.")]
        [DefaultValue(true)]
        [Setting]
        public bool IsLocalPathingForPointGenerationActive { get; set; }

        [XmlElement("StuckVelocity")]
        [DisplayName("Stuck velocity")]
        [Description("The velocity at wich the char is considered stuck.")]
        [DefaultValue(10)]
        [Setting]
        [Limit(0, 20)]
        public float StuckVelocity { get; set; }

        [XmlElement("IsNearestTargetPointEvaluationActive")]
        [DisplayName("Find nearest target point")]
        [Description(
            "When enabled it trys to select the nearest point, it will make you D3 freeze for 0.5sec everytime it generates target points."
            )]
        [DefaultValue(false)]
        [Setting]
        public bool IsNearestTargetPointEvaluationActive { get; set; }

        #region Arcane

        [XmlElement("IsArcaneDodgeActive")]
        [DisplayName("Activate arcane dodgeing")]
        [Description("Should arcane ground effects be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Arcane")]
        public bool IsArcaneDodgeActive { get; set; }

        [XmlElement("ArcaneDodgeRadius")]
        [DisplayName("Arcane dodge radius")]
        [Description("Range for arcane avoidance.")]
        [DefaultValue(26)]
        [Setting]
        [Category("Arcane")]
        [Limit(6, 40)]
        public float ArcaneDodgeRadius { get; set; }

        [XmlElement("ArcaneDodgeHealthPct")]
        [DisplayName("Arcane dodge health %")]
        [Description("Maximum health % to trigger arcane avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Arcane")]
        [Limit(0, 1)]
        public float ArcaneDodgeHealthPct { get; set; }

        #endregion

        #region Ice Clusters

        [XmlElement("IsIceClustersDodgeActive")]
        [DisplayName("Activate ice clusters dodgeing")]
        [Description("Should ice clusters ground effects be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Ice Clusters")]
        public bool IsIceClustersDodgeActive { get; set; }

        [XmlElement("IceClustersDodgeRadius")]
        [DisplayName("Ice cluster dodge radius")]
        [Description("Range for ice cluster avoidance.")]
        [DefaultValue(20)]
        [Setting]
        [Category("Ice Clusters")]
        [Limit(6, 40)]
        public float IceClustersDodgeRadius { get; set; }

        [XmlElement("IceClustersDodgeHealthPct")]
        [DisplayName("Ice Clusters dodge health %")]
        [Description("Maximum health % to trigger ice cluster avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Ice Clusters")]
        [Limit(0, 1)]
        public float IceClustersDodgeHealthPct { get; set; }

        #endregion

        #region Molten

        [XmlElement("IsMoltenDodgeActive")]
        [DisplayName("Activate molten dodgeing")]
        [Description("Should molten ground effects be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Molten")]
        public bool IsMoltenDodgeActive { get; set; }

        [XmlElement("MoltenDodgeRadius")]
        [DisplayName("Molten dodge radius")]
        [Description("Range for molten avoidance.")]
        [DefaultValue(15)]
        [Setting]
        [Category("Molten")]
        [Limit(6, 40)]
        public float MoltenDodgeRadius { get; set; }

        [XmlElement("MoltenDodgeHealthPct")]
        [DisplayName("Molten dodge health %")]
        [Description("Maximum health % to trigger molten avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Molten")]
        [Limit(0, 1)]
        public float MoltenDodgeHealthPct { get; set; }

        [XmlElement("IsMoltenTrailDodgeActive")]
        [DisplayName("Activate Molten Trail dodgeing")]
        [Description("Should molten trail ground effects be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Molten")]
        public bool IsMoltenTrailDodgeActive { get; set; }

        [XmlElement("MoltenTrailDodgeRadius")]
        [DisplayName("Molten Trail dodge radius")]
        [Description("Range for molten trail avoidance.")]
        [DefaultValue(4)]
        [Setting]
        [Category("Molten")]
        [Limit(1, 40)]
        public float MoltenTrailDodgeRadius { get; set; }

        [XmlElement("MoltenTrailDodgeHealthPct")]
        [DisplayName("Molten trail dodge health %")]
        [Description("Maximum health % to trigger molten trail avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Molten")]
        [Limit(0, 1)]
        public float MoltenTrailDodgeHealthPct { get; set; }

        #endregion

        #region Desecrator

        [XmlElement("IsDesecratorDodgeActive")]
        [DisplayName("Activate Desecrator dodgeing")]
        [Description("Should desecrator ground effects be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Desecrator")]
        public bool IsDesecratorDodgeActive { get; set; }

        [XmlElement("DesecratorDodgeRadius")]
        [DisplayName("Desecrator dodge radius")]
        [Description("Range for desecrator avoidance.")]
        [DefaultValue(9)]
        [Setting]
        [Category("Desecrator")]
        [Limit(1, 40)]
        public float DesecratorDodgeRadius { get; set; }

        [XmlElement("DesecratorDodgeHealthPct")]
        [DisplayName("Desecrator dodge health %")]
        [Description("Maximum health % to trigger desecrator avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Desecrator")]
        [Limit(0, 1)]
        public float DesecratorDodgeHealthPct { get; set; }

        #endregion

        #region Plagued

        [XmlElement("IsPlaguedDodgeActive")]
        [DisplayName("Activate Plagued dodgeing")]
        [Description("Should Plagued ground effects be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Plagued")]
        public bool IsPlaguedDodgeActive { get; set; }

        [XmlElement("PlaguedDodgeRadius")]
        [DisplayName("Plagued dodge radius")]
        [Description("Range for Plagued avoidance.")]
        [DefaultValue(9)]
        [Setting]
        [Category("Plagued")]
        [Limit(1, 40)]
        public float PlaguedDodgeRadius { get; set; }

        [XmlElement("PlaguedDodgeHealthPct")]
        [DisplayName("Plagued dodge health %")]
        [Description("Maximum health % to trigger plagued avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Plagued")]
        [Limit(0, 1)]
        public float PlaguedDodgeHealthPct { get; set; }

        #endregion

        #region Spore

        [XmlElement("IsSporeDodgeActive")]
        [DisplayName("Activate Spore dodgeing")]
        [Description("Should Spore's be dodged. Spawned by Highland Walkers.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Spore")]
        public bool IsSporeDodgeActive { get; set; }

        [XmlElement("SporeDodgeRadius")]
        [DisplayName("Spore dodge radius")]
        [Description("Range for spore avoidance.")]
        [DefaultValue(4)]
        [Setting]
        [Category("Spore")]
        [Limit(1, 40)]
        public float SporeDodgeRadius { get; set; }

        [XmlElement("SporeDodgeHealthPct")]
        [DisplayName("Spore dodge health %")]
        [Description("Maximum health % to trigger spore avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Spore")]
        [Limit(0, 1)]
        public float SporeDodgeHealthPct { get; set; }

        #endregion

        #region Belial

        [XmlElement("IsBelialBombDodgeActive")]
        [DisplayName("Activate Belial dodgeing")]
        [Description("Should Belials bomb's be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Belial")]
        public bool IsBelialBombDodgeActive { get; set; }

        [XmlElement("BelialBombDodgeRadius")]
        [DisplayName("Belial bomb radius")]
        [Description("Range for Belial bomb avoidance.")]
        [DefaultValue(10)]
        [Setting]
        [Category("Belial")]
        [Limit(1, 40)]
        public float BelialBombDodgeRadius { get; set; }

        [XmlElement("BelialBombDodgeHealthPct")]
        [DisplayName("Belial bomb health %")]
        [Description("Maximum health % to trigger Belial bomb avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Belial")]
        [Limit(0, 1)]
        public float BelialBombDodgeHealthPct { get; set; }

        #endregion

        #region Creep Mob Arm

        [XmlElement("IsCreepMobArmDodgeActive")]
        [DisplayName("Activate Creep Mob Arm dodgeing")]
        [Description("Should Creep Mob Arm's be dodged. Also called Ball Ticklers.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Creep Mob Arm")]
        public bool IsCreepMobArmDodgeActive { get; set; }

        [XmlElement("CreepMobArmDodgeRadius")]
        [DisplayName("Creep Mob Arm dodge radius")]
        [Description("Range for Creep Mob Arm avoidance.")]
        [DefaultValue(15)]
        [Setting]
        [Category("Creep Mob Arm")]
        [Limit(1, 40)]
        public float CreepMobArmDodgeRadius { get; set; }

        [XmlElement("CreepMobArmDodgeHealthPct")]
        [DisplayName("Creep Mob Arm dodge health %")]
        [Description("Maximum health % to trigger Creep Mob Arm avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Creep Mob Arm")]
        [Limit(0, 1)]
        public float CreepMobArmDodgeHealthPct { get; set; }

        #endregion

        #region Azmodan AOD Demon

        [XmlElement("IsAzmodanAODDemonDodgeActive")]
        [DisplayName("Activate Azmodan Black Pool dodgeing")]
        [Description("Should Azmodan Black Pool's be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Azmodan")]
        public bool IsAzmodanAODDemonDodgeActive { get; set; }

        [XmlElement("AzmodanAODDemonDodgeRadius")]
        [DisplayName("Azmodan Black Pool dodge radius")]
        [Description("Range for Azmodan Black Pool avoidance.")]
        [DefaultValue(50)]
        [Setting]
        [Category("Azmodan")]
        [Limit(1, 100)]
        public float AzmodanAODDemonDodgeRadius { get; set; }

        [XmlElement("AzmodanAODDemonDodgeHealthPct")]
        [DisplayName("Azmodan Black Pool dodge health %")]
        [Description("Maximum health % to trigger Azmodan Black Pool avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Azmodan")]
        [Limit(0, 1)]
        public float AzmodanAODDemonDodgeHealthPct { get; set; }

        #endregion

        #region Ghom
        [XmlElement("IsGhomGasDodgeActive")]
        [DisplayName("Activate Ghom Gas Cloud dodgeing")]
        [Description("Should Ghom Gas Cloud's be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Ghom")]
        public bool IsGhomGasDodgeActive { get; set; }

        [XmlElement("GhomGasDodgeRadius")]
        [DisplayName("Ghom Gas Cloud dodge raduis")]
        [Description("Range for Ghom Gas Cloud's avoidance.")]
        [DefaultValue(25)]
        [Setting]
        [Category("Ghom")]
        [Limit(1, 100)]
        public float GhomGasDodgeRadius { get; set; }

        [XmlElement("GhomGasDodgeHealthPct")]
        [DisplayName("Ghom Gas Cloud dodge health %")]
        [Description("Maximum health % to trigger Ghom Gas Cloud's avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Ghom")]
        [Limit(0, 1)]
        public float GhomGasDodgeHealthPct { get; set; }
        #endregion

        #region Diablo
        [XmlElement("IsDiabloFireRingDodgeActive")]
        [DisplayName("Activate Diablo Fire Ring dodgeing")]
        [Description("Should Diablo Fire Ring be dodged.")]
        [DefaultValue(true)]
        [Setting]
        [Category("Diablo")]
        public bool IsDiabloFireRingDodgeActive { get; set; }

        [XmlElement("DiabloFireRingDodgeRadius")]
        [DisplayName("Diablo Fire Ring dodge raduis")]
        [Description("Range for Diablo Fire Ring avoidance.")]
        [DefaultValue(50)]
        [Setting]
        [Category("Diablo")]
        [Limit(1, 100)]
        public float DiabloFireRingDodgeRadius { get; set; }

        [XmlElement("DiabloFireRingDodgeHealthPct")]
        [DisplayName("Diablo Fire Ring dodge health %")]
        [Description("Maximum health % to trigger Diablo Fire Ring avoidance.")]
        [DefaultValue(1)]
        [Setting]
        [Category("Diablo")]
        [Limit(0, 1)]
        public float DiabloFireRingDodgeHealthPct { get; set; }
        #endregion
    }
}