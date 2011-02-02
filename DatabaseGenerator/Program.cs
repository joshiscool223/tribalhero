﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CSVToXML;
using Game.Data;
using Game.Data.Stats;
using Game.Logic;
using Game.Setup;
using NDesk.Options;

#endregion

namespace DatabaseGenerator
{
    class Program
    {
        private static readonly ushort[] StructureTypes = new ushort[]
                                                          {
                                                                  2000, 2106, 2107, 2110, 2109, 2111, 2201, 2204, 2202, 2203,
                                                                  2301, 2302, 2303, 2402, 2403, 2501, 2502, 3002, 3003, 3004,
                                                                  3005
                                                          };

        private static readonly ushort[] TechnologyTypes = new ushort[]
                                                           {
                                                                   23011, 23013, 23014, 23021, 23022, 23023, 23024, 23031,
                                                                   23032, 23033, 23034, 22022, 21101, 21102, 21071, 22011,
                                                                   22012, 22013
                                                           };

        private static readonly ushort[] UnitTypes = new ushort[] {11, 12, 101, 102, 103, 104, 105, 106, 107, 108, 401};

        private static readonly Dictionary<string, string> Lang = new Dictionary<string, string>();

        private static string output = "output";

        private static void Main(string[] args)
        {
            Factory.CompileConfigFiles();
            Factory.InitAll();

            LoadLanguages();

            try
            {
                var p = new OptionSet {{"output=", v => output = v},};

                p.Parse(Environment.GetCommandLineArgs());
            }
            catch(Exception)
            {
            }

            // Process structures
            Directory.CreateDirectory(output);
            using (var writer = new StreamWriter(File.Create(Path.Combine(output, "structure_listing.inc.php"))))
            {
                writer.Write(@"<?php
                    $structures = array(
                ");

                foreach (var type in StructureTypes)
                {
                    ProcessStructure(type);

                    StructureBaseStats stats = StructureFactory.GetBaseStats(type, 1);
                    writer.WriteLine("'{2}_STRUCTURE' => array('name' => '{1}', 'sprite' => '{0}'),",
                                     stats.SpriteClass,
                                     Lang[stats.Name + "_STRUCTURE_NAME"],
                                     stats.Name);
                }

                writer.Write(@");");
            }

            // Process units
            using (var writer = new StreamWriter(File.Create(Path.Combine(output, "unit_listing.inc.php"))))
            {
                writer.Write(@"<?php
                    $units = array(
                ");
                foreach (var type in UnitTypes)
                {
                    ProcessUnit(type);

                    BaseUnitStats stats = UnitFactory.GetUnitStats(type, 1);
                    writer.WriteLine("'{2}_UNIT' => array('name' => '{1}', 'sprite' => '{0}'),",
                                     stats.SpriteClass,
                                     Lang[stats.Name + "_UNIT"],
                                     stats.Name);
                }
                writer.Write(@");");
            }

            // Process technologies
            // Process units
            using (var writer = new StreamWriter(File.Create(Path.Combine(output, "technology_listing.inc.php"))))
            {
                writer.Write(@"<?php
                    $technologies = array(
                ");
                foreach (var type in TechnologyTypes)
                {
                    ProcessTechnology(type);

                    TechnologyBase stats = TechnologyFactory.GetTechnologyBase(type, 1);
                    writer.WriteLine("'{0}_TECHNOLOGY' => array('name' => '{1}'),",
                                     stats.Name,
                                     Lang[stats.Name + "_TECHNOLOGY_NAME"]);
                }
                writer.Write(@");");
            }
        }

        // Technologies        
        private static void ProcessTechnology(ushort type)
        {
            string generalTemplate =
                    @"<?php
                // Generated by DatabaseGenerator program on #DATE#
	            $techKey = '#TECH#';
	            $techName = '#TECH_NAME#';
	
	            $trainedBy = array('name' => '#TRAINED_BY_NAME#', 'key' => '#TRAINED_BY#', 'level' => '#TRAINED_BY_LEVEL#');
	
	            // Levels array should contain:
	            $levels = array(
		            #LEVELS#
	            );

                include '/../technology_view.ctp';
            ";

            const string levelTemplate =
                    @"array('description' => '#DESCRIPTION#', 'time' => '#TIME#', 'gold' => #GOLD#, 'crop' => #CROP#, 'iron' => #IRON#, 'labor' => #LABOR#, 'wood' => #WOOD#, 'requirements' => array(#REQUIREMENTS#)),";

            string requirementTemplate = @"'#REQUIREMENT#',";

            // Get basic information
            TechnologyBase tech = TechnologyFactory.GetTechnologyBase(type, 1);

            generalTemplate = generalTemplate.Replace("#DATE#", DateTime.Now.ToString());
            generalTemplate = generalTemplate.Replace("#TECH#", tech.Name + "_TECHNOLOGY");
            generalTemplate = generalTemplate.Replace("#TECH_NAME#", Lang[tech.Name + "_TECHNOLOGY_NAME"]);

            // Builder info
            StructureBaseStats trainer;
            FindTechnologyTrainer(type, out trainer);
            generalTemplate = generalTemplate.Replace("#TRAINED_BY_NAME#",
                                                      trainer != null ? Lang[trainer.Name + "_STRUCTURE_NAME"] : "");
            generalTemplate = generalTemplate.Replace("#TRAINED_BY#", trainer != null ? trainer.Name + "_STRUCTURE" : "");
            generalTemplate = generalTemplate.Replace("#TRAINED_BY_LEVEL#",
                                                      trainer != null ? trainer.Lvl.ToString() : "");

            // Level info
            byte level = 1;
            var levelsWriter = new StringWriter(new StringBuilder());
            TechnologyBase currentStats = tech;
            do
            {
                string currentLevel = levelTemplate.Replace("#DESCRIPTION#",
                                                            Lang[currentStats.Name + "_TECHNOLOGY_LVL_" + level].Replace
                                                                    ("'", "\\'"));
                currentLevel = currentLevel.Replace("#TIME#", currentStats.Time.ToString());
                currentLevel = currentLevel.Replace("#GOLD#", currentStats.Resources.Gold.ToString());
                currentLevel = currentLevel.Replace("#CROP#", currentStats.Resources.Crop.ToString());
                currentLevel = currentLevel.Replace("#IRON#", currentStats.Resources.Iron.ToString());
                currentLevel = currentLevel.Replace("#LABOR#", currentStats.Resources.Labor.ToString());
                currentLevel = currentLevel.Replace("#WOOD#", currentStats.Resources.Wood.ToString());

                // Get requirements
                var requirementsWriter = new StringWriter(new StringBuilder());

                if (trainer != null)
                {
                    foreach (var requirement in GetTechnologyRequirements(type, level, trainer))
                        requirementsWriter.WriteLine(requirementTemplate.Replace("#REQUIREMENT#", requirement));
                }

                currentLevel = currentLevel.Replace("#REQUIREMENTS#", requirementsWriter.ToString());

                levelsWriter.WriteLine(currentLevel);

                trainer = StructureFactory.GetBaseStats(type, level);
                level++;
            } while ((currentStats = TechnologyFactory.GetTechnologyBase(type, level)) != null);

            generalTemplate = generalTemplate.Replace("#LEVELS#", levelsWriter.ToString());

            using (
                    var writer =
                            new StreamWriter(
                                    File.Create(Path.Combine(output, string.Format("{0}_TECHNOLOGY.ctp", tech.Name)))))
            {
                writer.Write(generalTemplate);
            }
        }

        private static void FindTechnologyTrainer(ushort type, out StructureBaseStats trainer)
        {
            foreach (var builderType in StructureTypes)
            {
                byte level = 1;
                StructureBaseStats stats;

                while ((stats = StructureFactory.GetBaseStats(builderType, level)) != null)
                {
                    level++;

                    ActionRecord record = ActionFactory.GetActionRequirementRecord(stats.WorkerId);

                    if (record == null)
                        continue;

                    foreach (var action in record.List)
                    {
                        if (action.Type == ActionType.TechnologyUpgrade && ushort.Parse(action.Parms[0]) == type)
                        {
                            trainer = stats;
                            return;
                        }
                    }
                }
            }

            trainer = null;
        }

        private static IEnumerable<string> GetTechnologyRequirements(ushort type, byte level, StructureBaseStats trainer)
        {
            ActionRecord record = ActionFactory.GetActionRequirementRecord(trainer.WorkerId);

            ActionRequirement foundAction = null;
            foreach (var action in record.List)
            {
                if (action.Type == ActionType.TechnologyUpgrade && ushort.Parse(action.Parms[0]) == type &&
                    ushort.Parse(action.Parms[1]) >= level)
                {
                    foundAction = action;
                    break;
                }
            }

            if (foundAction != null)
            {
                var requirements = EffectRequirementFactory.GetEffectRequirementContainer(foundAction.EffectReqId);
                foreach (var requirement in requirements)
                {
                    if (requirement.WebsiteDescription != string.Empty)
                        yield return requirement.WebsiteDescription;
                }
            }
        }

        // Units
        private static void ProcessUnit(ushort type)
        {
            string generalTemplate =
                    @"<?php
                // Generated by DatabaseGenerator program on #DATE#
	            $unitKey = '#UNIT#';
	            $unitName = '#UNIT_NAME#';
	            $description = '#DESCRIPTION#';
	
	            $trainedBy = array('name' => '#TRAINED_BY_NAME#', 'key' => '#TRAINED_BY#', 'level' => '#TRAINED_BY_LEVEL#');
	
	            // Levels array should contain:
	            $levels = array(
		            #LEVELS#
	            );

                include '/../unit_view.ctp';
            ";

            const string levelTemplate =
                    @"array('time' => #TIME#, 'carry' => #CARRY#, 'speed' => #SPEED#, 'upkeep' => #UPKEEP#, 'gold' => #GOLD#, 'crop' => #CROP#, 'iron' => #IRON#, 'labor' => #LABOR#, 'wood' => #WOOD#, 'hp' => #HP#, 'defense' => #DEFENSE#, 'attack' => #ATTACK#, 'range' => #RANGE#, 'stealth' => #STEALTH#, 'armor' => '#ARMOR#', 'weapon' => '#WEAPON#', 'weaponClass' => '#WEAPONCLASS#', 'unitClass' => '#UNITCLASS#', 'requirements' => array(#REQUIREMENTS#)),";

            string requirementTemplate = @"'#REQUIREMENT#',";

            // Get basic information
            BaseUnitStats stats = UnitFactory.GetUnitStats(type, 1);

            generalTemplate = generalTemplate.Replace("#DATE#", DateTime.Now.ToString());
            generalTemplate = generalTemplate.Replace("#UNIT#", stats.Name + "_UNIT");
            generalTemplate = generalTemplate.Replace("#UNIT_NAME#", Lang[stats.Name + "_UNIT"]);
            generalTemplate = generalTemplate.Replace("#DESCRIPTION#",
                                                      Lang[stats.Name + "_UNIT_DESC"].Replace("'", "\\'"));

            // Builder info
            StructureBaseStats trainer;
            FindUnitTrainer(type, out trainer);
            generalTemplate = generalTemplate.Replace("#TRAINED_BY_NAME#",
                                                      trainer != null ? Lang[trainer.Name + "_STRUCTURE_NAME"] : "");
            generalTemplate = generalTemplate.Replace("#TRAINED_BY#", trainer != null ? trainer.Name + "_STRUCTURE" : "");
            generalTemplate = generalTemplate.Replace("#TRAINED_BY_LEVEL#",
                                                      trainer != null ? trainer.Lvl.ToString() : "");

            // Level info
            byte level = 1;
            var levelsWriter = new StringWriter(new StringBuilder());
            BaseUnitStats currentStats = stats;
            do
            {
                string currentLevel = levelTemplate.Replace("#TIME#", currentStats.BuildTime.ToString());
                currentLevel = currentLevel.Replace("#GOLD#", currentStats.Cost.Gold.ToString());
                currentLevel = currentLevel.Replace("#CROP#", currentStats.Cost.Crop.ToString());
                currentLevel = currentLevel.Replace("#IRON#", currentStats.Cost.Iron.ToString());
                currentLevel = currentLevel.Replace("#LABOR#", currentStats.Cost.Labor.ToString());
                currentLevel = currentLevel.Replace("#WOOD#", currentStats.Cost.Wood.ToString());
                currentLevel = currentLevel.Replace("#HP#", currentStats.Battle.MaxHp.ToString());
                currentLevel = currentLevel.Replace("#DEFENSE#", currentStats.Battle.Def.ToString());
                currentLevel = currentLevel.Replace("#RANGE#", currentStats.Battle.Rng.ToString());
                currentLevel = currentLevel.Replace("#STEALTH#", currentStats.Battle.Stl.ToString());
                currentLevel = currentLevel.Replace("#WEAPON#", currentStats.Battle.Weapon.ToString());
                currentLevel = currentLevel.Replace("#ATTACK#", currentStats.Battle.Atk.ToString());
                currentLevel = currentLevel.Replace("#CARRY#", currentStats.Battle.Carry.ToString());
                currentLevel = currentLevel.Replace("#WEAPONCLASS#", currentStats.Battle.WeaponClass.ToString());
                currentLevel = currentLevel.Replace("#ARMOR#", currentStats.Battle.Armor.ToString());
                currentLevel = currentLevel.Replace("#UNITCLASS#", currentStats.Battle.ArmorClass.ToString());
                currentLevel = currentLevel.Replace("#SPEED#", currentStats.Battle.Spd.ToString());
                currentLevel = currentLevel.Replace("#UPKEEP#", currentStats.Upkeep.ToString());

                // Get requirements
                var requirementsWriter = new StringWriter(new StringBuilder());

                if (trainer != null)
                {
                    foreach (var requirement in GetUnitRequirements(type, level, trainer))
                        requirementsWriter.WriteLine(requirementTemplate.Replace("#REQUIREMENT#", requirement));
                }

                currentLevel = currentLevel.Replace("#REQUIREMENTS#", requirementsWriter.ToString());

                levelsWriter.WriteLine(currentLevel);

                trainer = StructureFactory.GetBaseStats(type, level);
                level++;
            } while ((currentStats = UnitFactory.GetUnitStats(type, level)) != null);

            generalTemplate = generalTemplate.Replace("#LEVELS#", levelsWriter.ToString());

            using (
                    var writer =
                            new StreamWriter(File.Create(Path.Combine(output, string.Format("{0}_UNIT.ctp", stats.Name))))
                    )
            {
                writer.Write(generalTemplate);
            }
        }

        private static void FindUnitTrainer(ushort type, out StructureBaseStats trainer)
        {
            foreach (var builderType in StructureTypes)
            {
                byte level = 1;
                StructureBaseStats stats;

                while ((stats = StructureFactory.GetBaseStats(builderType, level)) != null)
                {
                    level++;

                    ActionRecord record = ActionFactory.GetActionRequirementRecord(stats.WorkerId);

                    if (record == null)
                        continue;

                    foreach (var action in record.List)
                    {
                        if (action.Type == ActionType.UnitTrain && ushort.Parse(action.Parms[0]) == type)
                        {
                            trainer = stats;
                            return;
                        }
                    }
                }
            }

            trainer = null;
        }

        private static IEnumerable<string> GetUnitRequirements(ushort type, byte level, StructureBaseStats trainer)
        {
            ActionRecord record = ActionFactory.GetActionRequirementRecord(trainer.WorkerId);

            ActionRequirement foundAction = null;
            foreach (var action in record.List)
            {
                if (action.Type == ActionType.UnitTrain && ushort.Parse(action.Parms[0]) == type)
                {
                    foundAction = action;
                    break;
                }
            }

            if (foundAction != null)
            {
                var requirements = EffectRequirementFactory.GetEffectRequirementContainer(foundAction.EffectReqId);
                foreach (var requirement in requirements)
                {
                    if (requirement.WebsiteDescription != string.Empty)
                        yield return requirement.WebsiteDescription;
                }
            }
        }

        // Structure Database
        private static void ProcessStructure(ushort type)
        {
            string generalTemplate =
                    @"<?php
                // Generated by DatabaseGenerator program on #DATE#
	            $structureKey = '#STRUCTURE#';
	            $structureName = '#STRUCTURE_NAME#';
	            $description = '#DESCRIPTION#';
	
	            $converted = '#CONVERTED#';
	            $builtBy = array('name' => '#BUILT_BY_NAME#', 'key' => '#BUILT_BY#', 'level' => '#BUILT_BY_LEVEL#');
	
	            // Levels array should contain:
	            $levels = array(
		            #LEVELS#
	            );

                include '/../structure_view.ctp';
            ";

            const string levelTemplate =
                    @"array('description' => '#DESCRIPTION#', 'time' => #TIME#, 'gold' => #GOLD#, 'crop' => #CROP#, 'iron' => #IRON#, 'labor' => #LABOR#, 'wood' => #WOOD#, 'hp' => #HP#, 'defense' => #DEFENSE#, 'range' => #RANGE#, 'stealth' => #STEALTH#, 'weapon' => '#WEAPON#', 'maxLabor' => #MAXLABOR#, 'requirements' => array(#REQUIREMENTS#)),";

            string requirementTemplate = @"'#REQUIREMENT#',";

            // Get basic information
            StructureBaseStats stats = StructureFactory.GetBaseStats(type, 1);

            generalTemplate = generalTemplate.Replace("#DATE#", DateTime.Now.ToString());
            generalTemplate = generalTemplate.Replace("#STRUCTURE#", stats.Name + "_STRUCTURE");
            generalTemplate = generalTemplate.Replace("#STRUCTURE_NAME#", Lang[stats.Name + "_STRUCTURE_NAME"]);
            generalTemplate = generalTemplate.Replace("#DESCRIPTION#",
                                                      Lang[stats.Name + "_STRUCTURE_DESCRIPTION"].Replace("'", "\\'"));

            // Builder info
            StructureBaseStats builder;
            bool converted;
            FindStructureBuilder(type, out builder, out converted);
            generalTemplate = generalTemplate.Replace("#CONVERTED#", converted ? "1" : "0");
            generalTemplate = generalTemplate.Replace("#BUILT_BY_NAME#",
                                                      builder != null ? Lang[builder.Name + "_STRUCTURE_NAME"] : "");
            generalTemplate = generalTemplate.Replace("#BUILT_BY#", builder != null ? builder.Name + "_STRUCTURE" : "");
            generalTemplate = generalTemplate.Replace("#BUILT_BY_LEVEL#", builder != null ? builder.Lvl.ToString() : "");

            // Level info
            byte level = 1;
            var levelsWriter = new StringWriter(new StringBuilder());
            StructureBaseStats currentStats = stats;
            do
            {
                string currentLevel = levelTemplate.Replace("#DESCRIPTION#",
                                                            Lang[currentStats.Name + "_STRUCTURE_LVL_" + level].Replace(
                                                                                                                        "'",
                                                                                                                        "\\'"));
                currentLevel = currentLevel.Replace("#TIME#", currentStats.BuildTime.ToString());
                currentLevel = currentLevel.Replace("#GOLD#", currentStats.Cost.Gold.ToString());
                currentLevel = currentLevel.Replace("#CROP#", currentStats.Cost.Crop.ToString());
                currentLevel = currentLevel.Replace("#IRON#", currentStats.Cost.Iron.ToString());
                currentLevel = currentLevel.Replace("#LABOR#", currentStats.Cost.Labor.ToString());
                currentLevel = currentLevel.Replace("#WOOD#", currentStats.Cost.Wood.ToString());
                currentLevel = currentLevel.Replace("#HP#", currentStats.Battle.MaxHp.ToString());
                currentLevel = currentLevel.Replace("#DEFENSE#", currentStats.Battle.Def.ToString());
                currentLevel = currentLevel.Replace("#RANGE#", currentStats.Battle.Rng.ToString());
                currentLevel = currentLevel.Replace("#STEALTH#", currentStats.Battle.Stl.ToString());
                currentLevel = currentLevel.Replace("#WEAPON#", currentStats.Battle.Weapon.ToString());
                currentLevel = currentLevel.Replace("#MAXLABOR#", currentStats.MaxLabor.ToString());

                // Get requirements
                var requirementsWriter = new StringWriter(new StringBuilder());

                if (builder != null)
                {
                    foreach (var requirement in GetStructureRequirements(type, level, builder))
                        requirementsWriter.WriteLine(requirementTemplate.Replace("#REQUIREMENT#", requirement));
                }

                currentLevel = currentLevel.Replace("#REQUIREMENTS#", requirementsWriter.ToString());

                levelsWriter.WriteLine(currentLevel);

                builder = StructureFactory.GetBaseStats(type, level);
                level++;
            } while ((currentStats = StructureFactory.GetBaseStats(type, level)) != null);

            generalTemplate = generalTemplate.Replace("#LEVELS#", levelsWriter.ToString());

            using (
                    var writer =
                            new StreamWriter(
                                    File.Create(Path.Combine(output, string.Format("{0}_STRUCTURE.ctp", stats.Name)))))
            {
                writer.Write(generalTemplate);
            }
        }

        private static IEnumerable<string> GetStructureRequirements(ushort type, byte level, StructureBaseStats builder)
        {
            ActionRecord record = ActionFactory.GetActionRequirementRecord(builder.WorkerId);

            ActionRequirement foundAction = null;
            foreach (var action in record.List)
            {
                if (level == 1 && action.Type == ActionType.StructureBuild && ushort.Parse(action.Parms[0]) == type)
                {
                    foundAction = action;
                    break;
                }

                if (level == 1 && action.Type == ActionType.StructureChange && ushort.Parse(action.Parms[0]) == type)
                {
                    foundAction = action;
                    break;
                }

                if (action.Type == ActionType.StructureUpgrade && byte.Parse(action.Parms[0]) < level)
                {
                    foundAction = action;
                    break;
                }
            }

            if (foundAction != null)
            {
                var requirements = EffectRequirementFactory.GetEffectRequirementContainer(foundAction.EffectReqId);
                foreach (var requirement in requirements)
                {
                    if (requirement.WebsiteDescription != string.Empty)
                        yield return requirement.WebsiteDescription;
                }
            }
        }

        private static void FindStructureBuilder(ushort type, out StructureBaseStats builder, out bool convert)
        {
            foreach (var builderType in StructureTypes)
            {
                if (builderType == type)
                    continue;

                byte level = 1;
                StructureBaseStats stats;

                while ((stats = StructureFactory.GetBaseStats(builderType, level)) != null)
                {
                    level++;

                    ActionRecord record = ActionFactory.GetActionRequirementRecord(stats.WorkerId);

                    if (record == null)
                        continue;

                    foreach (var action in record.List)
                    {
                        if (action.Type == ActionType.StructureBuild && ushort.Parse(action.Parms[0]) == type)
                        {
                            builder = stats;
                            convert = false;
                            return;
                        }

                        if (action.Type == ActionType.StructureChange && ushort.Parse(action.Parms[0]) == type)
                        {
                            builder = stats;
                            convert = true;
                            return;
                        }
                    }
                }
            }

            builder = null;
            convert = false;
        }

        private static void LoadLanguages()
        {
            string[] files = Directory.GetFiles(Config.csv_folder, "lang.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                using (var langReader = new CsvReader(new StreamReader(File.Open(file, FileMode.Open))))
                {
                    while (true)
                    {
                        string[] obj = langReader.ReadRow();
                        if (obj == null)
                            break;

                        if (obj[0] == string.Empty)
                            continue;

                        Lang[obj[0]] = obj[1];
                    }
                }
            }
        }
    }
}