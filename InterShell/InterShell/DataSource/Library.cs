﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace InterShell.DataSource {

    public class Library {

        public string Home;
        public string DataFile = "InterShell.dat";
        public string PrefFile = "InterShell.ini";

        public List<Group> Groups { get; set; } = new List<Group>();

        public Dictionary<string, string> Preferences = new Dictionary<string, string>();

        #region Access

        public string GetGroupName() {
            return Preferences.ContainsKey("group") ? Preferences["group"] : ""; 
        }

        public void SetGroupName(string value) {
            Preferences["group"] = value;
        }

        public Group GetGroup() {
            return Groups.Where(x => x.Name == GetGroupName()).FirstOrDefault() ?? new Group();
        }

        public List<Group> GetGroups() {
            return Groups;
        }

        public List<Command> GetCommands() {
            return GetGroup().Commands;
        }

        public List<Setting> GetSettings() {
            return GetGroup().Settings;
        }

        public void SetSetting(string name, string value) {
           var setting = GetSettings().Where(x => x.Name == name).FirstOrDefault();
           if (setting != null) setting.Value = value;
        }

        public string GetCode() {
            return string.Join(Environment.NewLine, Encode());
        }

        public void SetCode(string value) {
            Decode(value);
        }

        #endregion

        #region Coder 

        public string[] Encode() {
            var code = new List<string>();
            foreach (var group in Groups) {
                code.AddRange(group.Encode());
            }
            return code.ToArray();
        }

        public void Decode(string text) {
            Decode(text.Replace('\r', '\n').Split('\n'));
        }

        public void Decode(string[] lines) {
            Groups.Clear();
            var group = new Group();
            var command = new Command();
            var setting = new Setting();
            var context = "";
            foreach (var rawLine in lines) {
                var line = rawLine.Trim();
                if (line.Length == 0) {
                    // skip
                }
                else if (line.StartsWith("#")) {
                    context = "group";
                    group = new Group();
                    group.Name = line.TrimStart('#').Trim();
                    Groups.Add(group);
                }
                else if (line.StartsWith("*")) {
                    context = "command";
                    command = new Command();
                    command.Name = line.TrimStart('*').Trim();
                    group.Commands.Add(command);
                }
                else if (line.StartsWith("-")) {
                    switch (context) {
                        case "group": group.Note = line.TrimStart('-').Trim(); break;
                        case "command": command.Note = line.TrimStart('-').Trim(); break;
                        default: break;
                    }
                }
                else if (line.StartsWith(":")) {
                    setting.AddOptions(line.TrimStart(':').Split(','));
                }
                else {
                    switch (context) {
                        case "group":
                            setting = new Setting(line.Split('='));
                            group.Settings.Add(setting);
                            break;
                        case "command":
                            command.Instructions.Add(line);
                            break;
                        default: break;
                    }
                }
            }
        }

        #endregion

        #region Storage

        public void LoadPrefs() {
            var path = Path.Combine(Home, PrefFile);
            if (!File.Exists(path)) return;
            foreach (var line in File.ReadAllLines(path)) {
                if (line.Contains("=")) {
                    var fields = line.Split('=');
                    if (fields.Length == 2) {
                        Preferences[fields[0].Trim()] = fields[1].Trim();
                    }
                }
            }
        }

        public void SavePrefs() {
            var code = new List<string>();
            foreach (var name in Preferences.Keys) {
                code.Add($"{name} = {Preferences[name]}");
            }
            var path = Path.Combine(Home, PrefFile);
            File.WriteAllLines(Path.Combine(Home, PrefFile), code);
        }

        public void LoadData() {
            var path = Path.Combine(Home, DataFile);
            if (!File.Exists(path)) return;
            Decode(File.ReadAllLines(path));
        }

        public void SaveData() {
            File.WriteAllLines(Path.Combine(Home, DataFile), Encode());
        }

        #endregion

    }
}