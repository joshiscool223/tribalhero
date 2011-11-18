namespace Game.Data.Stats
{
    public class BattleStats : BaseStats
    {
        #region Base Stats

        public ushort MaxHp { get; set; }

        public ushort Atk { get; set; }

        public byte Splash { get; set; }

        public byte Rng { get; set; }

        public byte Stl { get; set; }

        public byte Spd { get; set; }

        public ushort Carry { get; set; }

        public BaseBattleStats Base { get; private set; }

        #endregion

        #region Constructors

        public BattleStats(BaseBattleStats baseStats)
        {
            Base = baseStats;
            MaxHp = baseStats.MaxHp;
            Atk = baseStats.Atk;
            Splash = baseStats.Splash;
            Rng = baseStats.Rng;
            Stl = baseStats.Stl;
            Spd = baseStats.Spd;
            Carry = baseStats.Carry;
        }

        #endregion
    }
}