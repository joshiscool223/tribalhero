#region

using System;
using System.IO;
using System.Reflection;
using Game.Data;

#endregion

namespace Game.Setup {
    public class Config {
        public static readonly int server_port = 48888;
        public static readonly string server_listen_address = "0.0.0.0";

        public static readonly string flash_domain = "*";

        public static readonly string csv_folder = "conf/csv/";
        public static readonly string csv_compiled_folder = "conf/csv/compiled";
        public static readonly string settings_folder = "conf/";
        public static readonly string maps_folder = "conf/maps/";
        public static readonly string data_folder = "conf/data/";

        public static readonly uint map_width = 3808;
        public static readonly uint map_height = 6944;
        public static readonly uint region_width = 34;
        public static readonly uint region_height = 62;

        public static readonly int column = (int) (map_width/region_width);
        public static readonly int row = (int) (map_height/region_height);
        public static readonly int regions_count = column*row;

        public static readonly uint city_region_width = 56;
        public static readonly uint city_region_height = 56;
        public static readonly int city_region_column = (int) (map_width/city_region_width);
        public static readonly int city_region_row = (int) (map_height/city_region_height);

        public static readonly double seconds_per_unit = 1.0; //dont make it zero
        public static readonly int battle_turn_interval = 60;
        public static readonly int battle_min_rounds = 5;
        public static readonly int battle_loot_per_round = 10;  // percentage of total carry
        public static readonly int stamina_initial;
        public static readonly bool resource_upkeep = true;
        public static readonly bool resource_cap = true;
        public static readonly bool resource_fast_income = true;

        public static readonly byte minimum_distance_apart = 8;
        public static readonly int height_margin = 10;
        public static readonly int width_margin = 10;

        public static readonly bool database_verbose;
        public static readonly bool database_empty;
        public static readonly bool database_load_players = true;
        public static readonly string database_host = "127.0.0.1";
        public static readonly string database_username = "root";
        public static readonly string database_password = "";
        public static readonly string database_database = "game";
        public static readonly string database_test = "game_test";
        public static readonly string database_salt = "DFjkxcVsDfwgf4kuj2sDmM334";
        public static bool database_dump;

        public static readonly bool ai_enabled;
        public static readonly int ai_count = 100;

        public static readonly bool actions_instant_time;
        
        public static Random Random { get; private set; }

        static Config() {
            Random = new Random();

            string key = string.Empty;
            
            try {
                string settingsFile = "settings.ini";

                // Set the settings INI file location if specified
                string[] args = Environment.GetCommandLineArgs();
                foreach (string arg in args) {
                    string[] parts = arg.Split('=');
                    if (parts.Length != 2) continue;

                    switch (parts[0]) {
                        case "-settings":
                            settingsFile = parts[1];
                            break;
                    }
                }

                if (!File.Exists(settingsFile)) {
                    return;
                }

                using (StreamReader file = new StreamReader(File.Open(settingsFile, FileMode.Open))) {
                    string line;
                    while ((line = file.ReadLine()) != null) {
                        line = line.Trim();

                        if (line == string.Empty || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("\\\\"))
                            continue;

                        key = line.Substring(0, line.IndexOf('=')).ToLower();
                        key = key.Substring(0, key.IndexOf('.')) + "_" + key.Substring(key.IndexOf('.') + 1);

                        string value = line.Substring(line.IndexOf('=') + 1);

                        Type type = Type.GetType("Game.Setup.Config");
                        FieldInfo field = type.GetField(key, BindingFlags.Public | BindingFlags.Static);
                        if (field.FieldType.FullName == "System.Boolean")
                            field.SetValue(null, Boolean.Parse(value));
                        else if (field.FieldType.FullName == "System.Double")
                            field.SetValue(null, Double.Parse(value));
                        else if (field.FieldType.FullName == "System.Int32")
                            field.SetValue(null, Int32.Parse(value));
                        else if (field.FieldType.FullName == "System.UInt32")
                            field.SetValue(null, UInt32.Parse(value));
                        else
                            field.SetValue(null, value);
                    }
                }
            }
            catch (Exception e) {
                Global.Logger.Error("Error loading settings file at " + key, e);
            }
        }
    }
}