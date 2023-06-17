using ProjectM;
using PvPModes.Systems;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace PvPModes.Utils
{
    public class SquadList
    {
        public static Dictionary<int, string> bandit_units = new Dictionary<int, string>()
        {
            { 1, "CHAR_Bandit_Bomber" },
            { 2, "CHAR_Bandit_Deadeye" },
            { 3, "CHAR_Bandit_Hunter" },
            { 4, "CHAR_Bandit_Mugger" },
            { 5, "CHAR_Bandit_Stalker" },
            { 6, "CHAR_Bandit_Thief" },
            { 7, "CHAR_Bandit_Thug" },
            { 8, "CHAR_Bandit_Trapper" },
            { 9, "CHAR_Bandit_Wolf" }
        };
        public static Dictionary<int, string> militia_units = new Dictionary<int, string>()
        {
            { 1, "CHAR_Militia_Bomber" },
            { 2, "CHAR_Militia_Crossbow" },
            { 3, "CHAR_Militia_Devoted" },
            { 4, "CHAR_Militia_Guard" },
            { 5, "CHAR_Militia_Heavy" },
            { 6, "CHAR_Militia_Hound" },
            { 7, "CHAR_Militia_Light" },
            { 8, "CHAR_Militia_Longbowman" },
            { 9, "CHAR_Militia_Torchbearer" },
            { 10, "CHAR_Farmlands_Nun" }
        };
        public static Dictionary<int, string> soldier_units = new Dictionary<int, string>()
        {
            { 1, "CHAR_Town_Archer" },
            { 2, "CHAR_Town_CardinalAide" },
            { 3, "CHAR_Town_Footman" },
            { 4, "CHAR_Town_Knight_2H" },
            { 5, "CHAR_Town_Knight_Shield" },
            { 6, "CHAR_Town_Rifleman" },
            { 7, "CHAR_Militia_Bomber" },
            { 8, "CHAR_Militia_Torchbearer" }
        };
        public static Dictionary<int, string> holy_units = new Dictionary<int, string>()
        {
            { 1, "CHAR_Town_Cleric" },
            { 2, "CHAR_Town_Knight_2H" },
            { 3, "CHAR_Town_Priest" },
            { 4, "CHAR_Town_Rifleman" },
            { 5, "CHAR_Town_Lightweaver" },
            { 6, "CHAR_Town_Paladin" }
        };
        public static Dictionary<int, string> hunter_units = new Dictionary<int, string>()
        {
            { 1, "CHAR_Town_Paladin" },
            { 2, "CHAR_Town_Lightweaver" },
            { 3, "CHAR_Town_Cleric" },
            { 4, "CHAR_Town_Knight_2H" }
        };
        public static Dictionary<int, string> hunter_leader = new Dictionary<int, string>()
        {
            { 1, "CHAR_VHunter_Leader_VBlood" },
            { 2, "CHAR_VHunter_Jade_VBlood" },
            { 3, "CHAR_Paladin_DivineAngel" }
        };

        private static EntityManager entityManager = Plugin.Server.EntityManager;
        private static Entity empty_entity = new Entity();
        private static string unitName = "CHAR_Banding_Thug";
        private static System.Random generate = new System.Random();
    }
}
