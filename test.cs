// using System;
// using System.Collections.Concurrent;
// using System.Linq;
// using System.Numerics;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using Advanced_Combat_Tracker;
// using Triggernometry;
// using Triggernometry.Core.Scripting;
// using static System.Threading.Timeout;
// using static LatihasBA.SpeechScheduler;
// using static Triggernometry.FFXIV.Entity;
// using static Triggernometry.Core.RealPlugin;
// using Entity = Triggernometry.FFXIV.Entity;
//
// ;
// Instance.UnregisterNamedCallback("LatihasBA");
// Instance.RegisterNamedCallback("LatihasBA", new Action<object, string>(LatihasBA.Callback), null);
//
// public static class LatihasBA {
// 	public static string mdbw = "";
// 	public static int count_bird, count_bbls;
//
// 	public static void InitParams() {
// 		mdbw = "";
// 		count_bird = count_bbls = 0;
// 	}
//
// 	private static void Log(string message) => Instance.InvokeNamedCallback("command", $"/e {message}");
//
// 	public static bool inRect(Entity entity, Vector4 rect) {
// 		var x = entity.PosX;
// 		var y = entity.PosY;
// 		return x >= rect.X && x <= rect.Z && y >= rect.Y && y <= rect.W;
// 	}
//
// 	private static string GetScale(string name) => ScriptHelper.GetScalarVariable(false, "ba_" + name);
// 	// private static readonly Trigger _tri = new();
//
// 	// private static void PostTTS(string text) => ActGlobals.oFormActMain.TTS(text);
//
// 	public static class SpeechScheduler {
// 		private static readonly ConcurrentQueue<string> _speechQueue = new();
// 		private static volatile bool _isProcessing;
// 		private static readonly object _syncLock = new();
// 		private static readonly Timer _cdTimer = new(_ => ResetCoolDown(), null, Infinite, Infinite);
// 		private static readonly ManualResetEvent _resetEvent = new(false);
//
// 		public static void P(string message) {
// 			if (GetScale("p") != "p") {
// 				Instance.InvokeNamedCallback("command", $"/e [测试]{message}");
// 				return;
// 			}
// 			_speechQueue.Enqueue(message);
// 			if (!_isProcessing) StartProcessing();
// 		}
//
// 		private static void StartProcessing() {
// 			lock (_syncLock) {
// 				if (_isProcessing) return;
// 				_isProcessing = true;
// 			}
//
// 			Task.Run(() => {
// 				while (_speechQueue.TryDequeue(out var message)) {
// 					Instance.InvokeNamedCallback("command", $"/y {message}");
// 					WaitCoolDown();
// 				}
// 				lock (_syncLock) _isProcessing = false;
// 			});
// 		}
//
// 		private static void WaitCoolDown() {
// 			_cdTimer.Change(1250, Infinite);
// 			_resetEvent.WaitOne();
// 		}
//
// 		private static void ResetCoolDown() {
// 			_resetEvent.Set();
// 			_resetEvent.Reset();
// 		}
// 	}
//
// 	public static void Callback(object _, string str) {
// 		try {
// 			var ss = str.Split(':');
// 			var p = ss[0].ToLower();
// 			if (p == "p") P(ss[1]);
// 			else if (p == "log") Log(ss[1]);
// 			else if (p == "init") {
// 				InitParams();
// 				Log("LatihasBA初始化完成");
// 				Task.Run(async () => {
// 					await Task.Delay(5000);
// 					if (GetEntities().All(i => i.BNpcNameID != 0x1F41)) return;
// 					P("进入区域BA");
// 					P("所有人在桥上等工兵上双盾，身上有盾后下桥。检查自身文理并吃400品以上食物。默认135魔素，246文理。兵武恶魔书墙等待MT拉稳仇恨后再打");
// 					P("《/魔素板自动》，复制书名号内指令进宏。按一下自动切五攻，按两下自动切五防，按三下取消自动。");
// 					P("斗剑T及DPS请按一下切到5攻，奶妈与豪杰T按两下切到5防");
// 					P("非MT请关闭盾姿！！！");
// 				});
// 			} else if (p == "kill") {
// 				var sx = ss[1].Split(',');
// 				Log($"【{sx[1]}】杀死了【{sx[0]}】");
// 				var name = sx[0];
// 				if (name == "兵武恶魔书墙") {
// 					Task.Run(() => {
// 						P("等MT拉稳仇恨后AOE攻击【兵武智蛙】，所有黄圈都要[躲]。");
// 						P("之后攻击【兵武比布鲁斯】，并且[不要]站在*头前尾后*。读条圆形AOE《魔法锤》可以下踢打断，但是不一定有人给你打断，尽量避开。");
// 					});
// 				} else if (name is "欧文" or "亚特") {
// 					Task.Run(() => {
// 						P("所有人身位[不要超过]【工兵】和【MT】，保持一定距离，避免被炸弹误伤");
// 						P("上平台后贴墙走，远离平台炸弹。平台边缘中间有@宝箱@，保证安全再拾取。");
// 						P("在打任何怪之前请确保MT接稳仇恨，所有黄圈都要躲。");
// 						P("[不要]站到【兵武卡尔克布莉娜】(娃娃)前面，在身后输出。在【兵武半人马】读条《狂暴》时法系催眠【兵武半人马】打断读条");
// 					});
// 				} else if (name == "莱丁") {
// 					Task.Run(async () => {
// 						P("出口处有@宝箱@记得拾取");
// 						P("所有人身位[不要超过]【工兵】和【MT】，等MT接稳仇恨再打，[上平台]打小怪以看清AOE");
// 						P("[不要]站到【兵武卡尔克布莉娜】(娃娃)前面，在身后输出");
// 						P("打完小怪后再上平台走到底有@宝箱@，注意雷位置");
// 						var found = false;
// 						while (!found) {
// 							await Task.Delay(1000);
// 							foreach (var en in GetEntities().Where(i => i.Name.Contains("宝箱") && !i.IsCharacter)) {
// 								if (inRect(en, new Vector4(-104, 254, -104 + 48, 254 + 57))) {
// 									P("1X冰箱");
// 									found = true;
// 								}
// 								if (inRect(en, new Vector4(-40, 254, -40 + 48, 254 + 57))) {
// 									P("1X雷箱");
// 									found = true;
// 								}
// 								if (inRect(en, new Vector4(24, 254, 24 + 48, 254 + 57))) {
// 									P("1X火箱");
// 									found = true;
// 								}
// 								if (inRect(en, new Vector4(-104, 317, -104 + 48, 317 + 57))) {
// 									P("1X水箱");
// 									found = true;
// 								}
// 								if (inRect(en, new Vector4(-40, 317, -40 + 48, 317 + 57))) {
// 									P("1X风箱");
// 									found = true;
// 								}
// 								if (inRect(en, new Vector4(24, 317, 24 + 48, 317 + 57))) {
// 									P("1X土箱");
// 									found = true;
// 								}
// 							}
// 						}
// 					});
// 				} else if (name == "兵武博学林鸮") {
// 					count_bird += 1;
// 					if (count_bird != 2) return;
// 					P("《原型奥兹玛》有两个核心机制：加速度炸弹＆陨石");
// 					P("《加速度炸弹》：红绿色糖果状debuff，会出现在限制复活debuff边上，被点名请《双击esc》不要动等其时间结束。");
// 					P("《陨石》：是每个平台随机点名两人的大黑圈，被点名请在12点放置陨石，1点优先留给当前平台mt。");
// 					P("如果所在平台被点陨石两人去了同侧，请不要折返跑或二人转，将两个陨石放在同一个标点上，t奶视情况减伤抬血。");
// 				} else if (name == "兵武比布鲁斯") {
// 					count_bbls += 1;
// 					if (count_bbls != 2) return;
// 					P("【黑枪】重要机制《妖枪乱击》地面会有变暗特效，所有人[不要移动](可以攻击)");
// 					P("【白枪】重要机制《连装魔》，倒三角点名玩家[集合](2人以上站一起即可)");
// 					P("【白枪】重要机制：生成小怪【白手】，先[背对]【白手】，线由白变[紫]，等【白手】靠近后[再正对]");
// 				}
// 			} else if (p == "mdbw") {
// 				mdbw = ss[1];
// 				P(ss[1] == "b" ? "《变异》~暗~附魔" : "《变异》~光~附魔");
// 			} else if (p == "mdpz")
// 				P(mdbw == "b" ? "《极性波动》*黑*盘子会扩大一圈，[靠近白]盘子边缘，不会走的[跟人群]" : "《极性波动》*白*盘子会扩大一圈，[靠近黑]盘子边缘，不会走的[跟人群]");
// 			else if (p == "add") {
// 				foreach (var en in GetEntities().Where(i => i.ID == Convert.ToInt32(ss[1], 16)))
// 					if (en.BNpcNameID == 0x1F24)
// 						P("《白手》先[背对]【白手】，线由白变[紫]，等【白手】靠近后[再正对]");
// 			} else if (p == "mdyyg") {
// 				foreach (var en in GetEntities().Where(i => i.ID == Convert.ToInt32(ss[1], 16))) {
// 					foreach (var j in en.Statuses) {
// 						if (j.StatusID == 0x6AE) P("白手，[去黑]半场");
// 						if (j.StatusID == 0x6AF) P("黑手，[去白]半场");
// 					}
// 					break;
// 				}
// 			}
// 		} catch (Exception e) {
// 			Log($"Error: {str}");
// 			Log(e.StackTrace);
// 		}
// 	}
// }

