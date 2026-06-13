// using System;
// using System.Collections.Generic;
// using System.Collections.ObjectModel;
// using System.Drawing;
// using System.Globalization;
// using System.Linq;
// using System.Threading;
// using System.Windows.Forms;
// using Triggernometry;
// using Triggernometry.Core;
// using Triggernometry.Core.Variables;
// using Triggernometry.FFXIV;
// using Triggernometry.PluginBridges;
// using static Triggernometry.Core.Scripting.ScriptHelper;
//
//
// public struct Info
// {
//     public static readonly string Name = "绝神兵宝宝椅";
//     public static readonly string Version = "3.9";
//     public static readonly string Author = "Nag0mi";
//     public static readonly string ConfigName = "UWU_cfg";   // 保存配置的触发器永久变量名
//     public static string Description => $"{Name}  v{Version}  by {Author}";
// }
//
// RealPlugin.Instance.RegisterNamedCallback(Info.ConfigName, new Action<object, string>((_, __) => Entry.Start()), null);
//
// public static class Entry
// {
//     public static void RunConfigForm()
//     {
//         ConfigForm configForm = new ConfigForm(Info.ConfigName);
//         BijectDictionary<string, string> cbxItems;
//         OptionCbx cbx;
//         // 在下面倒序添加各个选项组
//
//         #region 配置：设置
//         var P2VFXSetting = new BijectDictionary<string, string>(
//             ("1", "开启"),
//             ("0", "关闭")
//         );
//         OptionsTableLayoutPanel VFXSetting = configForm.AddOptionGroup("设置");
//         OptionCbx P2VFXMode = new OptionCbx("团队播报", "ptuw_Ubroadcast", P2VFXSetting, defaultSelection: "0",
//             hint: "小队可见提示，关闭则仅自己可见。");
//         OptionCbx P3VFXMode = new OptionCbx("头标", "ptuw_Umark", P2VFXSetting, defaultSelection: "1",
//             hint: "柱子，停手等标记。");
//         OptionCbx Umarklocal = new OptionCbx("头标仅本地可见", "ptuw_Umarklocal", P2VFXSetting, defaultSelection: "1",
//             hint: "开启后头标仅本地可见。");
//         OptionCbx three = new OptionCbx("三连桶", "ptuw_Uthreebucket", P2VFXSetting, defaultSelection: "0",
//             hint: "与其他设置独立，开启仅本地可见也可以正常标三连桶。");
//         OptionCbx P5VFXMode = new OptionCbx("自动切目标", "ptuw_Utarget", P2VFXSetting, defaultSelection: "0",
//             hint: "开启后自动选柱子奶桶");
//         configForm.AddOption(three, VFXSetting);
//         configForm.AddOption(P2VFXMode, VFXSetting);
//         configForm.AddOption(P3VFXMode, VFXSetting);
//         configForm.AddOption(Umarklocal, VFXSetting);
//         configForm.AddOption(P5VFXMode, VFXSetting);
//         #endregion 配置：设置
//
//
//
//         #region 配置：队员顺序
//         string[] playerDescriptions = { "MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4" };
//         configForm.AddPartyGroup(" 队员顺序保证和游戏内一致 ", playerDescriptions);
//         #endregion 配置：队员顺序
//         configForm.Run();
//     }
//
//     [STAThread]
//     public static void Start()
//     {
//         try
//         {
//             SetScalarVariable(false, $"{Info.ConfigName}_isRunning", "1");
//             Thread staThread = new Thread(new ThreadStart(RunConfigForm));
//             staThread.SetApartmentState(ApartmentState.STA);
//             staThread.Start();
//             staThread.Join();
//             SetScalarVariable(false, $"{Info.ConfigName}_isRunning", null);
//         }
//         catch
//         {
//             SetScalarVariable(false, $"{Info.ConfigName}_isRunning", null);
//             throw;
//         }
//     }
// }
//
// #region ConfigForm 配置表单类
// public class ConfigForm : Form
// {
//     public static readonly Font UserFont = new Font("微软雅黑", 10);
//
//     /// <summary> 储存表单中所有 Option 控件的列表。 </summary>
//     private List<Option> _options = new List<Option>();
//     /// <summary> （可选）表单绑定的小队列表控件。 </summary>
//     private PartyListPanel _partyListPanel;
//     /// <summary> 用于储存用户配置的触发器字典变量。 </summary>
//     public VariableDictionary Config = new VariableDictionary();
//
//     private bool _verified;  // 改成定义一个 Verfier
//     private int _clickCount = 0;
//
//     /// <summary> 表单上方用于放置所有选项组的 Panel，可滚动。 </summary>
//     Panel mainPanel = new BackgroundPanel();
//     /// <summary> 表单下方用于放置按钮等控件的 TableLayoutPanel。 </summary>
//     TableLayoutPanel bottomPanel = new BottomTableLayoutPanel { RowCount = 1, ColumnCount = 1 };
//
//     public Button btnSave = new Button { Text = "保存配置" };
//
//     public ConfigForm(string configName)
//     {
//         // 暂停调整布局，直至 Run()
//         SuspendLayout();
//         // 不再需要读取触发器配置字典变量，因为现在使用标量变量
//         // 基本属性
//         Text = Info.Description;
//         Font = UserFont;
//         TopMost = true;
//         StartPosition = FormStartPosition.CenterScreen;
//         int width = (TextRenderer.MeasureText("啊啊啊啊啊", UserFont).Width) * 9;
//         MinimumSize = new Size(width, width); // To-do：添加一个根据所有控件总高度调节最小高度的逻辑
//         // 控件布局
//         Controls.Add(mainPanel);
//         Controls.Add(bottomPanel);
//         bottomPanel.Controls.Add(btnSave);
//         bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//         // 绑定事件
//         Shown += (sender, e) => {
//             TopMost = true;
//             BringToFront();
//             Focus();
//             RealPlugin.Instance.InvokeNamedCallback("command", "/e <se.9>");
//             mainPanel.AutoScrollPosition = new Point(0, 0);
//         };
//         bottomPanel.MouseDown += VerifyClick;
//         btnSave.Click += btnSave_Click;
//     }
//
//     /// <summary>
//     /// 在表单上方的 mainPanel 区域添加一个 Panel - GroupBox - OptionsTableLayoutPanel 的结构，并返回这个 OptionsTableLayoutPanel。
//     /// </summary>
//     /// <param name="groupName">GroupBox 上方显示的名称，建议首尾添加空格。</param>
//     /// <returns>生成的 OptionsTableLayoutPanel，用于填充该分组的选项。</returns>
//     public OptionsTableLayoutPanel AddOptionGroup(string groupName)
//     {
//         var table = new OptionsTableLayoutPanel();
//         var group = new GroupBox { Text = groupName };
//         var panel = new GroupPanel();
//
//         mainPanel.Controls.Add(panel);
//         panel.Controls.Add(group);
//         group.Controls.Add(table);
//
//         table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
//         table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
//
//         return table;
//     }
//
//     /// <summary>
//     /// 在表单上方的 mainPanel 区域添加一个 Panel - GroupBox - PartyListPanel 的结构，并返回这个 PartyListPanel。
//     /// </summary>
//     /// <param name="groupName">GroupBox 上方显示的名称，建议首尾添加空格。</param>
//     /// <param name="playerDescriptions">包含每个队员职能描述的 string[]，如 { "MT", "ST", ... }。小队人数由 Array 长度决定。</param>
//     /// <returns>生成的 PartyListPanel，用于显示当前队员并调整顺序。</returns>
//     public void AddPartyGroup(string groupName, string[] playerDescriptions)
//     {
//         _partyListPanel = new PartyListPanel(playerDescriptions);
//         var group = new GroupBox { Text = groupName };
//         var panel = new GroupPanel();
//
//         mainPanel.Controls.Add(panel);
//         panel.Controls.Add(group);
//         group.Controls.Add(_partyListPanel);
//     }
//
//     /// <summary> 将选项添加至表单，并放置在 GroupBox 中的 Table 末尾。 </summary>
//     public void AddOption(Option option, OptionsTableLayoutPanel table)
//     {
//         _options.Add(option);
//         option.AppendToTable(table);
//     }
//
//     /// <summary> 在 GroupBox 中的 Table 末尾添加一条分割线。 </summary>
//     public void AddSeparatorLine(OptionsTableLayoutPanel table)
//     {
//         table.RowCount++;
//         Panel separator = new SeperatorPanel();
//         table.Controls.Add(separator, 0, table.RowCount - 1);
//         table.SetColumnSpan(separator, 2);
//     }
//
//     /// <summary> 在 GroupBox 中的 Table 末尾添加一个文本 Label。 </summary>
//     public Label AddLabel(string desc, OptionsTableLayoutPanel table)
//     {
//         table.RowCount++;
//         Label lbl = new Label { Text = desc };
//         table.Controls.Add(lbl, 0, table.RowCount - 1);
//         table.SetColumnSpan(lbl, 2);
//         return lbl;
//     }
//
//     /// <summary> 从触发器变量中读取全部已保存配置，若校验合法则设置到表单。 </summary>
//     public void LoadFromConfig()
//     {
//         string env = "${_env[COMPUTERNAME]} ${_env[USERNAME]}";
//         var savedEnv = GetScalarVariable(true, "env");
//         if (savedEnv == null || env != savedEnv.ToString())
//         {
//             return;
//         }
//         else
//         {
//             _verified = true;
//         }
//         _partyListPanel?.LoadFromConfig(); // 设置了小队列表控件
//         foreach (Option option in _options)
//         {
//             option.LoadFromConfig();
//         }
//     }
//
//     void btnSave_Click(object sender, EventArgs e)
//     {
//         //if (!_verified) // 不会吧不会吧 不会有人有空来这改代码解锁 没空看使用说明吧 ヽ(`Д´)ノ
//         //{
//         //MessageBox.Show("你需要完整阅读触发器使用说明，并按提示操作。\n\n" +
//         //"“完整阅读”的意思是逐行阅读，\n不是扫一眼没看见之后在群里问怎么设置。",
//         //Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
//         //return;
//         //}
//
//         string myRole = _partyListPanel.SaveToConfig();
//
//         foreach (Option option in _options)
//         {
//             option.SaveToConfig();
//         }
//         // 保存环境变量和作者信息到标量变量
//         SetScalarVariable(true, "env", "${_env[COMPUTERNAME]} ${_env[USERNAME]}");  // 储存系统环境变量以保证用户不是 copy 了别人的配置
//         SetScalarVariable(true, "author", Info.Author);
//         SetScalarVariable(true, "version", Info.Version);
//         RealPlugin.Instance.InvokeNamedCallback("command", "/e <se.10>");
//         RealPlugin.Instance.InvokeNamedCallback("command", "/e 已保存配置。");
//         if (!string.IsNullOrEmpty(myRole))
//         {
//             RealPlugin.Instance.InvokeNamedCallback("command", $"/e {myRole}");
//         }
//         this.Close();
//     }
//
//     void VerifyClick(object sender, EventArgs e)
//     {
//         _clickCount++;
//         if (_clickCount % 10 == 0)
//         {
//             if (!_verified)
//             {
//                 _verified = true;
//                 MessageBox.Show("已解除锁定。", Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
//             }
//             else
//             {
//                 MessageBox.Show("已解除过锁定，无需再次操作。", Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
//             }
//         }
//     }
//
//     /// <summary> 读取配置，恢复表单布局，显示表单。</summary>
//     public void Run()
//     {
//         LoadFromConfig();
//         ResumeLayout();
//         ShowDialog();
//         Dispose();
//     }
//
//     private class Validator
//     {
//         public bool Validated = false;
//         private ConfigForm _form;
//         public Validator(ConfigForm form) { _form = form; }
//
//     }
// }
//
// #endregion ConfigForm 配置表单类
//
// #region 小队控件类定义
// public class PartyListPanel : System.Windows.Forms.TableLayoutPanel
// {
//     List<PlayerLabel> _players = new List<PlayerLabel>();
//     public int PlayerCount;
//     public string[] PlayerDescriptions;
//     private ToolTip _tip = new ToolTip();
//
//     public const string CONFIGNAME_PLAYERNAMES_LIST = "pn";
//     public const string CONFIGNAME_PLAYERIDS_LIST = "pid";
//     public const string CONFIGNAME_PLAYERNAMES_DICT = "pn";
//     public const string CONFIGNAME_PLAYERIDS_DICT = "pid";
//     public const string CONFIGNAME_PLAYER_IDX = "myIdx";
//
//     public PartyListPanel(string[] playerDescriptions)
//     {
//         PlayerCount = playerDescriptions.Length;
//         PlayerDescriptions = playerDescriptions;
//         Dock = DockStyle.Fill;
//         AutoSize = true;
//         AutoSizeMode = AutoSizeMode.GrowAndShrink;
//
//         // 设置可拖拽队员标签
//         DragEnter += new DragEventHandler(PartyListPanel_DragEnter);
//         DragDrop += new DragEventHandler(PartyListPanel_DragDrop);
//         AllowDrop = true;
//
//         // 根据玩家数确定行列数
//         int rowCount = PlayerCount <= 4 ? 1 : 2;
//         int colCount = Math.Min(PlayerCount, 4);
//         for (int i = 0; i < rowCount; i++)
//         {
//             RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rowCount));
//         }
//         for (int i = 0; i < colCount; i++)
//         {
//             ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / colCount));
//         }
//
//         // 读取当前队员并生成标签
//         List<VariableDictionary> entities = GetSortedPartyMembers();
//         for (int i = 0; i < PlayerCount; i++)
//         {
//             PlayerLabel player = new PlayerLabel(this, entities[i], i);
//             _players.Add(player);
//         }
//
//         // 设置标签宽度
//         double lblWidth = 50;
//         double lblHeight = 30;
//         /*
//         using (Graphics g = _players[0].CreateGraphics())
//         {
//             foreach (var label in _players)
//             {
//                 lblWidth = Math.Max(lblWidth, g.MeasureString(label.Text, ConfigForm.UserFont).Width);
//             }
//             lblWidth *= 1.1;
//             lblHeight = g.MeasureString(_players[0].Text, ConfigForm.UserFont).Height * 1.1;
//         }
//         */
//         foreach (var label in _players)
//         {
//             Size size = TextRenderer.MeasureText(label.Text, ConfigForm.UserFont);
//             lblWidth = Math.Max(lblWidth, size.Width);
//             lblHeight = Math.Max(lblHeight, size.Height);
//         }
//         lblWidth *= 1.1;
//         lblHeight *= 1.1;
//
//         foreach (var label in _players)
//         {
//             label.Width = (int)lblWidth;
//             label.Height = (int)lblHeight;
//             _tip.SetToolTip(label, "拖拽调整顺序");
//         }
//     }
//
//     static string[] jobOrder = {
//         "WAR", "MRD", "DRK", "GNB", "PLD", "GLA",
//         "WHM", "CNJ", "AST", "SGE", "SCH",
//         "SAM", "MNK", "PGL", "DRG", "LNC", "NIN", "ROG", "RPR",
//         "BRD", "ARC", "MCH", "DNC", "BLM", "THM", "RDM", "SMN", "ACN", "BLU"
//     };
//
//     List<VariableDictionary> GetSortedPartyMembers()
//     {
//         List<VariableDictionary> entities = BridgeFFXIV.GetAllEntities();
//         entities = entities.Where(e => e.GetValue("id").ToString().StartsWith("10"))                // is player
//                            .OrderByDescending(e => e.GetValue("inparty").ToString() == "1")         // is party member
//                            .ThenBy(e => e.GetValue("roleid").ToString().PadLeft(3, '0'))            // sort by subrole id (Entity.RoleType)
//                            .ThenBy(e => Array.IndexOf(jobOrder, e.GetValue("jobEN3").ToString()))   // customized job order
//                            .ThenBy(e => e.GetValue("jobid").ToString().PadLeft(3, '0'))             // sort by job id
//                            .ThenBy(e => e.GetValue("name").ToString())
//                            .ToList();
//
//         if (entities.Count >= 8     // Double Caster => D2 / D4
//             && entities[5].GetValue("roleid").ToString() == ((int)Job.RoleType.PhysicalRanged).ToString()
//             && entities[6].GetValue("roleid").ToString() == ((int)Job.RoleType.MagicalRanged).ToString()
//             && entities[7].GetValue("roleid").ToString() == ((int)Job.RoleType.MagicalRanged).ToString())
//         {
//             (entities[5], entities[6]) = (entities[6], entities[5]);
//
//             if (entities[7].GetValue("jobEN3").ToString() == "BLM") // with BLM: BLM D2
//             {
//                 (entities[5], entities[7]) = (entities[7], entities[5]);
//             }
//         }
//
//         if (entities.Count < PlayerCount)
//         {
//             entities.AddRange(Enumerable.Repeat(BridgeFFXIV.NullCombatant, PlayerCount - entities.Count));
//         }
//         else if (entities.Count > PlayerCount)
//         {
//             entities.RemoveRange(PlayerCount, entities.Count - PlayerCount);
//         }
//         return entities;
//     }
//
//
//     private void PartyListPanel_DragEnter(object sender, DragEventArgs e)
//     {
//         if (e.Data.GetDataPresent(typeof(PlayerLabel)))
//         {
//             e.Effect = DragDropEffects.Move;
//         }
//     }
//
//     private void PartyListPanel_DragDrop(object sender, DragEventArgs e)
//     {
//         // 获取拖动的标签和目标位置的标签，更新 Order
//         PlayerLabel draggedLabel = (PlayerLabel)e.Data.GetData(typeof(PlayerLabel));
//         Point clientPoint = PointToClient(new Point(e.X, e.Y));
//         Control control = GetChildAtPoint(clientPoint);
//         if (control != null && control is PlayerLabel targetLabel && draggedLabel != targetLabel)
//         {
//             Parent.Parent.Parent.Parent.SuspendLayout();
//             SwapLabels(draggedLabel, targetLabel);
//             Parent.Parent.Parent.Parent.ResumeLayout(false);
//         }
//     }
//
//     private void SwapLabels(PlayerLabel draggedLabel, PlayerLabel targetLabel)
//     {
//         (draggedLabel.Order, targetLabel.Order) = (targetLabel.Order, draggedLabel.Order);
//     }
//
//     public void LoadFromConfig()
//     {
//         var savedList = GetListVariable(false, CONFIGNAME_PLAYERIDS_LIST);
//         if (savedList == null || savedList.Size != PlayerCount)
//             return;
//
//         List<string> storedPlayerIDs = savedList.Values.Select(var => var.ToString()).ToList();
//
//         List<int> indices = new List<int>();
//         foreach (var playerLabel in _players)
//         {
//             int index = storedPlayerIDs.IndexOf(playerLabel.HexID);
//             if (index >= 0 && index < PlayerCount)
//             {
//                 indices.Add(index);
//             }
//             else return;
//         }
//         HashSet<int> expectedIndices = new HashSet<int>(Enumerable.Range(0, PlayerCount));
//         if (new HashSet<int>(indices).SetEquals(expectedIndices))
//         {
//             for (int i = 0; i < PlayerCount; i++)
//             {
//                 _players[i].Order = indices[i];
//             }
//         }
//     }
//
//     public string SaveToConfig()
//     {
//         if (_players.Count <= 1) return string.Empty;
//
//         _players = _players.OrderBy(p => p.Order).ToList();
//
//         VariableList hexIDList = new VariableList();
//         VariableList nameList = new VariableList();
//         VariableDictionary hexIDDict = new VariableDictionary();
//         VariableDictionary nameDict = new VariableDictionary();
//         string changer = "ConfigForm";
//         string myRole = string.Empty;
//
//         foreach (var label in _players)
//         {
//             Variable hexID = new VariableScalar { Value = label.HexID };
//             Variable name = new VariableScalar { Value = label.PlayerName };
//             string description = PlayerDescriptions[label.Order];
//
//             hexIDList.Push(hexID, changer);
//             nameList.Push(name, changer);
//             hexIDDict.SetValue(description, hexID, changer);
//             nameDict.SetValue(description, name, changer);
//
//             if (BridgeFFXIV.PlayerHexId == label.HexID)
//             {
//                 SetScalarVariable(isPersistent: false, CONFIGNAME_PLAYER_IDX, new VariableScalar(label.Order + 1));
//                 myRole = description.ToLower();
//             }
//         }
//
//         SetListVariable(isPersistent: false, CONFIGNAME_PLAYERIDS_LIST, hexIDList);
//         SetListVariable(isPersistent: false, CONFIGNAME_PLAYERNAMES_LIST, nameList);
//         SetDictVariable(isPersistent: false, CONFIGNAME_PLAYERIDS_DICT, hexIDDict);
//         SetDictVariable(isPersistent: false, CONFIGNAME_PLAYERNAMES_DICT, nameDict);
//
//         return myRole;
//     }
//
//     private static string RoleToCN(Job.RoleType role)
//     {
//         switch (role & Job.RoleType.MainRole)
//         {
//             case Job.RoleType.Tank:
//                 return "坦克";
//             case Job.RoleType.Healer:
//                 return "治疗";
//         }
//         switch (role)
//         {
//             case Job.RoleType.StrengthMelee:
//                 return "近战力量";
//             case Job.RoleType.DexterityMelee:
//                 return "近战敏捷";
//             case Job.RoleType.PhysicalRanged:
//                 return "物理远程";
//             case Job.RoleType.MagicalRanged:
//                 return "魔法远程";
//             default:
//                 return "DPS";
//         }
//     }
// }
//
// public class PlayerLabel : System.Windows.Forms.Label
// {
//     public PartyListPanel ParentTable;
//     public string PlayerName;
//     public Job.RoleType SubRole;
//     public string JobName;
//     public string HexID;
//     private Label _draggingClone;
//
//     private int _order;
//     /// <summary> Start from 0 </summary>
//     public int Order
//     {
//         get => _order;
//         set
//         {
//             _order = value;
//             Text = $"[{ParentTable.PlayerDescriptions[_order]}] {JobName}\n" + PlayerName.Replace(" ", "\n");
//             RefreshLocation();
//         }
//     }
//
//     private Color GetForeColor()
//     {
//         switch (SubRole & Job.RoleType.MainRole)
//         {
//             case Job.RoleType.Tank: return Color.FromArgb(16, 72, 144);
//             case Job.RoleType.Healer: return Color.FromArgb(16, 144, 72);
//             case Job.RoleType.DPS:
//                 switch (SubRole)
//                 {
//                     case Job.RoleType.StrengthMelee:
//                     case Job.RoleType.DexterityMelee: return Color.FromArgb(160, 64, 0);
//                     case Job.RoleType.PhysicalRanged: return Color.FromArgb(160, 0, 0);
//                     case Job.RoleType.MagicalRanged: return Color.FromArgb(160, 0, 96);
//                     default: return Color.FromArgb(128, 128, 128);
//                 }
//             default: return Color.FromArgb(128, 128, 128);
//         }
//     }
//
//     public PlayerLabel(PartyListPanel parent, VariableDictionary entity, int order)
//     {
//         ParentTable = parent;
//         PlayerName = entity.GetValue("name").ToString();
//         SubRole = (Job.RoleType)int.Parse(entity.GetValue("roleid").ToString().PadLeft(1, '0'), NumberStyles.Integer, CultureInfo.InvariantCulture);
//         JobName = CultureInfo.CurrentCulture.Name.StartsWith("zh-")
//             ? entity.GetValue("jobCN2").ToString()
//             : entity.GetValue("jobEN3").ToString();
//         HexID = entity.GetValue("id").ToString();
//         Order = order;
//         ForeColor = GetForeColor();
//         Margin = new Padding(10);
//         AutoSize = false;
//         Anchor = AnchorStyles.None;
//         TextAlign = ContentAlignment.MiddleCenter;
//         Cursor = Cursors.SizeAll;
//         MouseDown += new MouseEventHandler(PlayerLabel_MouseDown);
//         MouseMove += new MouseEventHandler(PlayerLabel_MouseMove);
//         MouseUp += new MouseEventHandler(PlayerLabel_MouseUp);
//     }
//
//     /// <summary> 根据 Order 将标签置于父表格的正确位置  </summary>
//     public void RefreshLocation()
//     {
//         int colCount = Math.Min(ParentTable.PlayerCount, 4);
//         int row = Order / colCount;
//         int col = Order % colCount;
//         ParentTable.Controls.Add(this, col, row);
//     }
//
//     private void PlayerLabel_MouseDown(object sender, MouseEventArgs e)
//     {
//         if (e.Button == MouseButtons.Left)
//         {
//             DoDragDrop(this, DragDropEffects.Move);
//         }
//     }
//
//     private void PlayerLabel_MouseMove(object sender, MouseEventArgs e)
//     {
//         if (e.Button == MouseButtons.Left && _draggingClone != null)
//         {
//             Point newLocation = ParentTable.PointToClient(Cursor.Position);
//             newLocation.Offset(-_draggingClone.Width / 2, -_draggingClone.Height / 2);
//             _draggingClone.Location = newLocation;
//         }
//     }
//
//     private void PlayerLabel_MouseUp(object sender, MouseEventArgs e)
//     {
//         if (_draggingClone != null)
//         {
//             ParentTable.Controls.Remove(_draggingClone);
//             _draggingClone.Dispose();
//             _draggingClone = null;
//         }
//     }
// }
// #endregion
//
// #region Option 选项类定义
// public abstract class Option
// {
//     public Label Lbl;               // 左侧的描述标签（如果控件不自带文本描述）
//     public Control Ctrl;            // 控件，如 ComboBox
//     private readonly ToolTip _tip = new ToolTip();   // 鼠标悬停时显示提示文本
//
//     /// <summary> 选项对应的触发器配置字典键名。 </summary>
//     public string ConfigKey { get; set; }
//
//     /// <summary>
//     /// 在 TableLayoutPanel 末尾添加空行，并将该选项置于这一行。
//     /// </summary>
//     /// <param name="table">选项所处的父对象 TableLayoutPanel。</param>
//     public virtual void AppendToTable(OptionsTableLayoutPanel table)
//     {
//         table.RowCount++;
//         table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//
//         table.Controls.Add(Lbl, 0, table.RowCount - 1);
//         if (Ctrl != null)
//             table.Controls.Add(Ctrl, 1, table.RowCount - 1);
//         else
//             table.SetColumnSpan(Lbl, 2);
//     }
//
//     public virtual void SetHint(string hint)
//     {
//         if (!string.IsNullOrWhiteSpace(hint))
//         {
//             if (Lbl != null)
//             {
//                 _tip.SetToolTip(Lbl, hint);
//                 Lbl.Cursor = Cursors.Help;
//             }
//             if (Ctrl != null)
//             {
//                 _tip.SetToolTip(Ctrl, hint);
//                 Ctrl.Cursor = Cursors.Help;
//             }
//         }
//     }
//
//     public abstract void LoadFromConfig();
//     public abstract void SaveToConfig();
// }
//
// public class OptionTxt : Option
// {
//     public TextBox Txt => (TextBox)Ctrl;
//
//     public OptionTxt(string desc, string configKey, string defaultText = "", string hint = null)
//     {
//         Lbl = new Label { Text = desc };
//         Ctrl = new TextBox { Text = defaultText };
//         ConfigKey = configKey;
//         SetHint(hint);
//     }
//
//     /// <summary> 若配置标量变量中已有该选项，则读取已保存的文本。</summary>
//     public override void LoadFromConfig()
//     {
//         var value = GetScalarVariable(true, ConfigKey);
//         if (value != null)
//         {
//             Txt.Text = value.ToString().Trim();
//         }
//     }
//
//     /// <summary> 将输入的文本保存至配置标量变量中。 </summary>
//     public override void SaveToConfig()
//     {
//         SetScalarVariable(true, ConfigKey, Txt.Text.Trim());
//     }
// }
//
// public class OptionChk : Option
// {
//     public CheckBox Chk => (CheckBox)Ctrl;
//
//     public OptionChk(string desc, string configKey, bool defaultChecked = false, string hint = null)
//     {
//         Lbl = new Label { Text = desc };
//         Ctrl = new CheckBox { Checked = defaultChecked };
//         ConfigKey = configKey;
//         SetHint(hint);
//     }
//
//     /// <summary> 
//     /// 若配置标量变量中已有该选项，则读取已保存的选项。<br /> 
//     /// "1" 视为选中，其他文本视为未选中。
//     /// </summary>
//     public override void LoadFromConfig()
//     {
//         var value = GetScalarVariable(true, ConfigKey);
//         if (value != null)
//         {
//             Chk.Checked = value.ToString().Trim() == "1";
//         }
//     }
//
//     /// <summary> 将该选项保存至配置标量变量中，以 "1" 和 "0" 表示选中状态。 </summary>
//     public override void SaveToConfig()
//     {
//         SetScalarVariable(true, ConfigKey, Chk.Checked ? "1" : "0");
//     }
// }
//
// public class OptionCbx : Option
// {
//     public ComboBox Cbx => (ComboBox)Ctrl;
//     private readonly BijectDictionary<string, string> _data;
//
//     /// <summary>
//     /// 根据双向字典 <paramref name="data"/> 生成一个 Label 和 ComboBox 的组合。
//     /// </summary>
//     /// <param name="desc">左侧 label 描述</param>
//     /// <param name="configKey">存储到永久字典变量中的键</param>
//     /// <param name="data">字典键与选项文本描述的双向字典</param>
//     /// <param name="defaultSelection">默认选中的描述</param>
//     /// <param name="hint">鼠标悬停的提示文本</param>
//     public OptionCbx(string desc, string configKey, BijectDictionary<string, string> data, string defaultSelection = "", string hint = null)
//         : this(desc, configKey, data, data.Keys.IndexOf(defaultSelection), hint) { }
//
//     /// <summary>
//     /// 根据双向字典 <paramref name="data"/> 生成一个 Label 和 ComboBox 的组合。
//     /// </summary>
//     /// <param name="desc">左侧 label 描述</param>
//     /// <param name="configKey">存储到永久字典变量中的键</param>
//     /// <param name="data">字典键与选项文本描述的双向字典</param>
//     /// <param name="defaultIndex">默认选中的序号</param>
//     /// <param name="hint">鼠标悬停的提示文本</param>
//     public OptionCbx(string desc, string configKey, BijectDictionary<string, string> data, int defaultIndex = 0, string hint = null)
//     {
//         Lbl = new Label { Text = desc };
//         _data = data;
//         Ctrl = new ComboBox();
//         Cbx.Items.AddRange(data.Values.ToArray());
//         Cbx.SelectedIndex = (defaultIndex >= 0 && defaultIndex < Cbx.Items.Count) ? defaultIndex : 0;
//         Cbx.DropDownStyle = ComboBoxStyle.DropDownList;
//         ConfigKey = configKey;
//         SetHint(hint);
//     }
//
//     /// <summary> 若配置标量变量中已有该选项，则读取已保存的选项。 </summary>
//     public override void LoadFromConfig()
//     {   // 暂时未支持 DropDown 模式
//         var value = GetScalarVariable(true, ConfigKey);
//         if (value != null)
//         {
//             string option = value.ToString().Trim();
//             Cbx.SelectedItem = _data[option] ?? option;
//         }
//     }
//
//     /// <summary> 将选中项保存至配置标量变量中。 </summary>
//     public override void SaveToConfig()
//     {
//         string selection = Cbx.SelectedItem?.ToString() ?? Cbx.SelectedText.ToString();
//         string option = _data.GetKey(selection) ?? selection;
//         SetScalarVariable(true, ConfigKey, option);
//     }
// }
//
// /// <summary> 可以从值检索键的双射字典结构，可以用于将 ComboBox 选项和触发器内存储的键相互映射。</summary>
// public class BijectDictionary<TKey, TValue>
// {
//     private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();
//     private Dictionary<TValue, TKey> _revDict = new Dictionary<TValue, TKey>();
//     private List<TKey> _keys = new List<TKey>();
//     private List<TValue> _values = new List<TValue>();
//     public ReadOnlyCollection<TKey> Keys => _keys.AsReadOnly();
//     public ReadOnlyCollection<TValue> Values => _values.AsReadOnly();
//     public int Count { get => _dict.Count; }
//     public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
//     public bool ContainsValue(TValue value) => _revDict.ContainsKey(value);
//
//     public BijectDictionary() : this(new (TKey, TValue)[0]) { }
//     public BijectDictionary(params (TKey, TValue)[] items)
//     {
//         foreach (var (key, value) in items)
//         {
//             if (_dict.ContainsKey(key))
//                 throw new Exception($"Key \"{key}\" is duplicated in the bijective dictionary.");
//             if (_revDict.ContainsKey(value))
//                 throw new Exception($"Value \"{value}\" is duplicated in the bijective dictionary.");
//
//             _dict[key] = value;
//             _revDict[value] = key;
//             _keys.Add(key);
//             _values.Add(value);
//         }
//     }
//
//     public TValue this[TKey key]
//     {
//         get => _dict.TryGetValue(key, out TValue value) ? value : default;
//     }
//
//     public TKey GetKey(TValue value)
//     {
//         return _revDict.TryGetValue(value, out TKey key) ? key : default;
//     }
//
//     public bool RemoveKey(TKey key)
//     {
//         lock (this)
//         {
//             int index = _keys.IndexOf(key);
//             if (index < 0)
//                 return false;
//             Remove(key, _values[index], index);
//             return true;
//         }
//     }
//
//     public bool RemoveValue(TValue value)
//     {
//         lock (this)
//         {
//             int index = _values.IndexOf(value);
//             if (index < 0)
//                 return false;
//             Remove(_keys[index], value, index);
//             return true;
//         }
//     }
//
//     private void Remove(TKey key, TValue value, int index)
//     {
//         _keys.RemoveAt(index);
//         _values.RemoveAt(index);
//         _dict.Remove(key);
//         _revDict.Remove(value);
//     }
//
//     public BijectDictionary<TKey, TValue> ShallowCopy()
//     {
//         var duplicate = new BijectDictionary<TKey, TValue>();
//         foreach (var kvp in _dict)
//         {
//             duplicate._dict.Add(kvp.Key, kvp.Value);
//             duplicate._revDict.Add(kvp.Value, kvp.Key);
//             duplicate._keys.Add(kvp.Key);
//             duplicate._values.Add(kvp.Value);
//         }
//         return duplicate;
//     }
// }
//
// #endregion
//
// #region 其它控件类定义（格式调整）
// public class GroupBox : System.Windows.Forms.GroupBox
// {
//     public GroupBox() : base()
//     {
//         Dock = DockStyle.Top;
//         AutoSize = true;
//         AutoSizeMode = AutoSizeMode.GrowAndShrink;
//         Margin = new Padding(20);
//     }
// }
//
// public class CheckBox : System.Windows.Forms.CheckBox
// {
//     public CheckBox() : base()
//     {
//         AutoSize = true;
//         Dock = DockStyle.Fill;
//         Margin = new Padding(10);
//     }
// }
//
// public class TextBox : System.Windows.Forms.TextBox
// {
//     public TextBox() : base()
//     {
//         AutoSize = true;
//         Dock = DockStyle.Fill;
//         Margin = new Padding(10);
//     }
// }
//
// public class ComboBox : System.Windows.Forms.ComboBox
// {
//     public ComboBox() : base()
//     {
//         AutoSize = true;
//         Dock = DockStyle.Fill;
//         Margin = new Padding(10);
//     }
//
//     protected override void WndProc(ref Message m)
//     {
//         if (m.Msg == 0x020A)  // WM_MOUSEWHEEL
//         {
//             return;  // No-scroll
//         }
//         base.WndProc(ref m);
//     }
// }
//
// public class Label : System.Windows.Forms.Label
// {
//     public Label() : base()
//     {
//         AutoSize = true;
//         Dock = DockStyle.Fill;
//         Margin = new Padding(10);
//     }
// }
//
// public class Button : System.Windows.Forms.Button
// {
//     public Button() : base()
//     {
//         Anchor = AnchorStyles.None;
//         AutoSize = true;
//         Margin = new Padding(10);
//         Padding = new Padding(5);
//     }
// }
//
// public class SeperatorPanel : System.Windows.Forms.Panel
// {
//     public SeperatorPanel() : base()
//     {
//         Height = 2;
//         BackColor = Color.DarkGray;
//         Dock = DockStyle.Fill;
//         AutoSize = true;
//         Margin = new Padding(10);
//     }
// }
//
// public class BackgroundPanel : System.Windows.Forms.Panel
// {
//     public BackgroundPanel() : base()
//     {
//         AutoSize = true;
//         AutoSizeMode = AutoSizeMode.GrowAndShrink;
//         Dock = DockStyle.Fill;
//         AutoScroll = true;
//     }
// }
//
// public class GroupPanel : System.Windows.Forms.Panel
// {
//     public GroupPanel() : base()
//     {
//         AutoSize = true;
//         AutoSizeMode = AutoSizeMode.GrowAndShrink;
//         Dock = DockStyle.Top;
//         Padding = new Padding(20, 20, 20, 0);
//     }
// }
//
// public class OptionsTableLayoutPanel : System.Windows.Forms.TableLayoutPanel
// {
//     public OptionsTableLayoutPanel() : base()
//     {
//         AutoSize = true;
//         AutoSizeMode = AutoSizeMode.GrowAndShrink;
//         Dock = DockStyle.Fill;
//         RowCount = 0;
//         ColumnCount = 2;
//         ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
//         ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
//     }
// }
//
// public class BottomTableLayoutPanel : System.Windows.Forms.TableLayoutPanel
// {
//     public BottomTableLayoutPanel() : base()
//     {
//         Dock = DockStyle.Bottom;
//         ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//     }
// }
//
// public class ToolTip : System.Windows.Forms.ToolTip
// {
//     public ToolTip() : base()
//     {
//         InitialDelay = 0;
//         AutoPopDelay = 30000;
//         ReshowDelay = 0;
//         ShowAlways = true;
//     }
// }
//
// #endregion

