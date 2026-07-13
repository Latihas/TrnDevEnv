using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Triggernometry.Core;
using Triggernometry.Core.Variables;
using Triggernometry.FFXIV;
using Triggernometry.PluginBridges;
using Triggernometry.PScript;
using Triggernometry.UI.CustomControls;
using Triggernometry.UI.Forms;
using static Triggernometry.Core.Scripting.ScriptHelper;
using static Triggernometry.PScript.ScriptUtils;

[SuppressMessage("Performance", "SYSLIB1045")]
[SuppressMessage("Performance", "CS8509")]
public class ScriptUwU : IScriptBase {
	public override bool IsDev => true;
	private static ScriptUwU Instance = null!;
	private static readonly GameConfigForm.ConfigInfo Info = new("绝神兵宝宝椅", "3.9", "Laihas, Nag0mi, 莫灵喵", "UWU_cfg");
	private static Jobs? MyJob;
	private bool lbfull, jzhStarted;
	private static string? MyName;
	private static readonly Jobs[] TBJobOrder = [Jobs.MT, Jobs.ST, Jobs.D1, Jobs.D2, Jobs.D3, Jobs.D4, Jobs.H1, Jobs.H2];
	private readonly Ciyu ciyu = new();
	private readonly List<string> P43PlayerName = [];
	private readonly Player[] Players = new Player[8];
	private readonly List<Player> Threebuckets = [];

	private readonly List<Zhuzi> Zhuzis = [];
	private CancellationTokenSource? ctssimple, ctsp1, ctsp2, ctsp3, ctsp41, ctsp42, ctsp43, ctsp5;
	private string? hs, ttsafe;
	private int p43dhCount;
	private List<DHP43> p43dhDist = [];
	private volatile State state;
	private bool Ubroadcast, Uthreebucket, Uauto, Umarklocal, Umark;
	private Vector2 Zhuzi2;
	public override string Desc => "输入/e cfg UWU 以设置";

	private enum Jobs {
		MT,
		ST,
		H1,
		H2,
		D1,
		D2,
		D3,
		D4
	}

	private enum State {
		None,
		P1,
		P2Start,
		P2,
		P3Start,
		P3,
		P4Start,
		P41,
		P42,
		P43,
		P5
	}

	public override void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
		Instance = this;
	}

	public override List<(Regex, Action<GroupCollection>)> IgnoreTerritory => [
		new(new Regex(@"^.{15}\S+ 00:0038:: *[Cc][Ff][Gg] +(?:绝神兵|UWU) *$"), ShowConfigForm)
	];

	public override void DeInitPlugin() => ResetCts();

	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex(@"^.{15}\S+ 01:"), ShowConfigForm),
		new(new Regex(@"^.{14} Director 21:.{8}:400000(?:03|1[026]|05|11)"), _ => {
			ActGlobals.oFormActMain.EndCombat(false);
			ClearAllIGShape();
			ResetCts();
			InitParams();
			PostTip($"已使用{MyJob}进行初始化");
		}),
		//通用技能
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B88:"), _ => {
			TTS("站位，坦克LB1");
		}),
		new(new Regex(@"^.{14} StatusAdd 1A:5FC:[^:]+:(?<time>[^:]+):"), g => {
			if (ctssimple == null) return;
			var token = ctssimple.Token;
			Task.Run(async () => {
				await Task.Delay(((int)float.Parse(g["time"].Value) - 3) * 1000, token);
				TTS("准备爆炸");
			}, token);
		}),
		new(new Regex(@"^.{14} StatusRemove 1E:30F:"), _ => TTS("奶妈LB")),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B72:"), _ => TTS("法系LB")),
		new(new Regex(@"^.{14} Death 19:(?<id>40.{6}):"), g => {
			var id = g["id"].Value;
			foreach (var z in Zhuzis.Where(z => z.zid == id && !z.cs2)) {
				Broadcast($"柱炸: {z.dmg}");
				break;
			}
			if (id == ciyu.zid && !ciyu.cs2) Broadcast($"羽炸: {ciyu.dmg}");
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B5F:"), _ => {
			if (ctssimple == null) return;
			var token = ctssimple.Token;
			Task.Run(async () => {
				await Task.Delay(2000, token);
				Beep(2000, 300);
			}, token);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B5B:[^:]+:.{8}:(?<RoleName>[^:]+):"), g => {
			var name = g["RoleName"].Value;
			var HDead = false;
			foreach (var p in Players) {
				if (BridgeFFXIV.GetNamedPartyMember(p.name).GetValue("currenthp").ToString() == "0" && p.job is Jobs.H1 or Jobs.H2) {
					Log("Dead: " + p.name);
					HDead = true;
				}
				if (p.name != name || MyName != name) continue;
				PostTip(state == State.P42 ? "热风点你，出去接线" : "热风点你，快出去");
			}
			if (HDead) Broadcast("奶死亡，热风注意出人群", true);
		}),
		new(new Regex(@"^.{14} StatusAdd 1A:31:[^:]+:[^:]+:.{8}:[^:]+:.{8}:(?<id>[^:]+):"), g => {
			RealPlugin.Instance.InvokeNamedCallback("command", $"/e 爆发药: {g["id"].Value}");
		}),
		new(new Regex(@"^.{14} AddCombatant 03:.{8}:.+:00::2138:8735"), _ => {
			TTS("准备撞球");
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B8B:"), _ => {
			TTS("坦克LB3");
		}),
		new(new Regex(@"^.{14} StatusAdd 1A:5F9:[^:]+:9999.00:E0000000::(?<id>.{8}):"), _ => {
			Broadcast("已觉醒");
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B74:"), _ => {
			if (ctssimple == null) return;
			var token = ctssimple.Token;
			Task.Run(async () => {
				await Task.Delay(2000, token);
				TTS("近战LB");
			}, token);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B7B:"), _ => {
			var TDead = false;
			foreach (var p in Players) {
				if (BridgeFFXIV.GetNamedPartyMember(p.name).GetValue("currenthp").ToString() == "0" && p.job is Jobs.ST or Jobs.MT) {
					Log("Dead: " + p.name);
					TDead = true;
				}
			}
			var op = "二仇炮。";
			if (TDead) {
				op += "T死亡，二仇注意出人群";
				Broadcast(op, true);
			}
			TTS(op);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B87:"), _ => TTS("核爆减伤")),
		new(new Regex(@"^.{14} LimitBreak 24:7530:3"), _ => lbfull = true),

		#region P1

		new(new Regex(@"^.{14} (?:\w+ )00:0044:(迦楼罗|Garuda|ガルーダ):(哈哈哈哈哈！ 你们这些蝼蚁只有被我的狂风吹散的下场|Heehee HAHA hahaha HEEHEE haha HEEEEEE!!!|無残に散れッ！)"), _ => {
			InitParams();
			ctsp1 = new();
			ctssimple = new();
			var token = ctsp1.Token;
			Task.Run(async () => {
				Place(StaticPlace.initPlace);
				Place(StaticPlace.clear2);
				Place(StaticPlace.clear3);
				Place(StaticPlace.clear4);
				if (MyJob == null) TTS("未设置职业，请检查设置。");
				if (Ubroadcast) Broadcast("已开启团队播报，请注意不要冲突");
				if (Uthreebucket) Broadcast("已开启三桶播报，请注意不要冲突");
				if (Uauto) Broadcast("已开启全自动打印移动坐标，如不需要使用请关闭。");
				if (Umark && Umarklocal) Broadcast("已启用本地标记。");
				if (Umark && !Umarklocal) Broadcast("已启用小队可见标记。");
				//启用触发器 启用触发
				state = State.P1;
				await Task.Delay(4000, token);
				TTS("顺劈");
				await Task.Delay(15000, token);
				TTS("顺劈加死刑");
				await Task.Delay(14000, token);
				TTS("躲羽毛");
				await Task.Delay(4000, token);
				TTS("AOE");
				await Task.Delay(7000, token);
				TTS("ST消除");
				await Task.Delay(5000, token);
				TTS("远奶消除");
				await Task.Delay(4500, token);
				TTS("远奶剩余三秒");
				await Task.Delay(1000, token);
				Beep(1046.5f, 300);
				await Task.Delay(1000, token);
				Beep(1546.5f, 300);
				await Task.Delay(1000, token);
				Beep(2046.5f, 300);
				await Task.Delay(500, token);
				TTS("近战消除");
				await Task.Delay(11000, token);
				TTS("躲羽毛");
				await Task.Delay(4000, token);
				TTS("AOE");
				await Task.Delay(17000, token);
				TTS("躲羽毛");
				await Task.Delay(4000, token);
				var es = GetXYFromBnpcid("8723");
				if (es.Count == 2) {
					var fs1 = GetDir(es[0]);
					var fs2 = GetDir(es[1]);
					TTS($"{fs1},{fs2}");
					FsPlaceByRule(fs1, fs2);
				}
				await Task.Delay(2000, token);
				if (MyJob is Jobs.MT or Jobs.ST) TTS("双T去三四点挡风枪");
				BelowHP("8722", 0.15, "请注意风神血量<se.6>", "stop1");
				await Task.Delay(8000, token);
				TTS("躲羽毛");
				await Task.Delay(6000, token);
				Place(StaticPlace.initPlace);
				await Task.Delay(2000, token);
				BelowHP("8722", 0.08, "请注意风神血量<se.6>", "stop2");
				await Task.Delay(3000, token);
				const string fs3 = "左西";
				const string fs4 = "右东";
				TTS($"{fs3},{fs4}");
				FsPlace(fs3, fs4);
				await Task.Delay(3000, token);
				TTS("接线");
				await Task.Delay(1000, token);
				TTS("顺劈加死刑");
				await Task.Delay(7500, token);
				TTS("躲羽毛");
				await Task.Delay(13000, token);
				TTS("顺劈");
				await Task.Delay(8000, token);
				TTS("钢铁月环");
				await Task.Delay(8000, token);
				TTS("分摊");
				await Task.Delay(4000, token);
				TTS("顺劈");
			}, token);
		}),
		new(new Regex(@"^.{14} AddCombatant 03:(?<zid>.{8}):.+:00::2091:8726:"), g => {
			ciyu.zid = g["zid"].Value;
		}),
		new(new Regex(@"^.{14} (?:ActionEffect 15|AOEActionEffect 16):.{8}:(?<sname>[^:]+):(?<jid>[^:]+):(?<jname>[^:]+):.+:71878:0:10000"), g => {
			if (g["jid"].ToString() is "2B45" or "2B46") return; //? TODO
			ciyu.dmg.Append(g["sname"].Value).Append('[').Append(g["jname"].Value).Append(']');
		}),
		new(new Regex(@"^.{14} StatusAdd 1A:5F5:[^:]+:9999.00:40.{6}:[^:]+:10.{6}:[^:]+:02:[^:]+:71878"), _ => {
			ciyu.cs2 = true;
		}),

		#endregion

		#region P2

		new(new Regex(@"^.{14} (?:\w+ )00:0044:(迦楼罗|Garuda|ガルーダ):(怎……怎么可能……区区蝼蚁……|My power... No...|お、おのれ……クソ虫がぁぁぁぁぁッ！！！)"), _ => {
			ctsp1?.Cancel();
			ctsp1?.Dispose();
			ctsp1 = null;
			ctsp2 = new();
			var token = ctsp2.Token;
			Task.Run(async () => {
				state = State.P2Start;
				TTS("开疾跑");
				await Task.Delay(11000, token);
				TTS("两次AOE");
				state = State.P2;
				Place(StaticPlace.initPlace);
				await Task.Delay(8000, token);
				TTS("群盾防击退");
				await Task.Delay(4000, token);
				TTS("三连死刑后，MT挑衅");
				await Task.Delay(18000, token);
				if (MyJob is Jobs.D1 or Jobs.D2 or Jobs.D3 or Jobs.D4) TTS("连线靠近，沿绿色点放置地火");
				if (MyJob == Jobs.MT) TTS("拉近到三四柱中间");
				await Task.Delay(33000, token);
				TTS("AOE");
				p2zhuzi23();
				await Task.Delay(7000, token);
				if (MyJob is Jobs.H1 or Jobs.H2) TTS("热风奶前往3点");
				if (MyJob is Jobs.D3 or Jobs.D4) TTS("引导地火");
				await Task.Delay(14000, token);
				TTS("十字冲");
				await Task.Delay(12000, token);
				TTS("六人分摊");
				await Task.Delay(11000, token);
				TTS("火神冲看本体");
				await Task.Delay(18000, token);
				TTS("三连死刑");
				await Task.Delay(11000, token);
				TTS("引导地火");
				await Task.Delay(8000, token);
				TTS("分摊");
			}, token);
		}),
		new(new Regex(@"^.{14} 271 10F:(?<id>.{8}):[^:]+:00:00:(?<x>80.5|100.0|119.5)000:(?<y>80.5|100.0|119.5)000"), g => {
			if (state != State.P2Start) return;
			if (BridgeFFXIV.GetIdEntity(g["id"].Value).GetValue("bnpcid").ToString() != "8730") return;
			hs = GetDir(g["x"].Value, g["y"].Value);
			if (hs is "上北" or "下南") Place(StaticPlace.p2hsWEsafe);
			if (hs is "左西" or "右东") Place(StaticPlace.p2hsNSsafe);
			Place(StaticPlace.clear4);
			PostTip("火神" + hs, 50, 50, "main2");
		}),
		new(new Regex(@"^.{14} 263 107:.{8}:2B61:(?<x>[^:]+):(?<y>[^:]+):"), g => {
			if (state != State.P2Start) return;
			if (hs == "") return;
			var x = float.Parse(g["x"].Value);
			var y = float.Parse(g["y"].Value);
			string? safe = null;
			if (Math.Abs(x - 100) < 1 && y < 85 && hs != "上北" && hs != "下南") safe = "下南C点";
			if (Math.Abs(x - 100) < 1 && y > 115 && hs != "上北" && hs != "下南") safe = "上北A点";
			if (Math.Abs(y - 100) < 1 && x < 85 && hs != "左西" && hs != "右东") safe = "右东B点";
			if (Math.Abs(y - 100) < 1 && x > 115 && hs != "左西" && hs != "右东") safe = "左西D点";
			if (safe is null) {
				Log($"未找到安全点({x},{y})");
				return;
			}
			Place(safe switch {
				"上北A点" => StaticPlace.safeA,
				"下南C点" => StaticPlace.safeC,
				"左西D点" => StaticPlace.safeD,
				"右东B点" => StaticPlace.safeB
			});
			safe += "安全";
			PostTip(safe);
			Broadcast($"火神转场{safe}");
		}),
		new(new Regex(@"^.{14} AddCombatant 03:(?<zid>.{8}):.+:00::1186:8731:.+:10000:::(?<x>[^:]+):(?<y>[^:]+):"), g => {
			p2zhuzi(g["zid"].Value, g["x"].Value, g["y"].Value);
		}),
		new(new Regex(@"^.{14} StatusAdd 1A:609:[^:]+:9999.00:.{8}::(?<zid>.{8}):[^:]+:02:"), g => {
			foreach (var z in Zhuzis.Where(z => z.zid == g["zid"].Value)) {
				z.cs2 = true;
				break;
			}
		}),
		new(new Regex(@"^.{14} (?:ActionEffect 15|AOEActionEffect 16):.{8}:(?<sname>[^:]+):(?<jid>[^:]+):(?<jname>[^:]+):(?<tid>.{8}):.+:26870:0:10000"), g => {
			if (g["jid"].Value == "2B58") return;
			foreach (var z in Zhuzis.Where(z => z.zid == g["tid"].Value)) {
				z.dmg.Append(g["sname"].Value).Append('[').Append(g["jname"].Value).Append(']');
				break;
			}
		}),

		#endregion

		#region P3

		new(new Regex(@"^.{14} (?:ActionEffect 15|AOEActionEffect 16):.{8}:(?<sname>[^:]+):(?<jid>[^:]+):(?<jname>[^:]+):(?<tid>.{8}):.+:26870:0:10000"), _ => {
			ctsp2?.Cancel();
			ctsp2?.Dispose();
			ctsp2 = null;
			ctsp3 = new();
			var token = ctsp3.Token;
			Task.Run(async () => {
				state = State.P3Start;
				Place(StaticPlace.initPlace);
				TTS("贴边");
				await Task.Delay(3500, token);
				TTS("群奶，法系捡碎片");
				await Task.Delay(3500, token);
				TTS("AOE");
				await Task.Delay(10000, token);
				TTS("死刑后，引导流沙");
				await Task.Delay(13000, token);
				TTS("去反方向");
				await Task.Delay(15000, token);
				TTS("站石牢");
				await Task.Delay(10000, token);
				TTS("连续AOE");
				state = State.P3;
				await Task.Delay(4000, token);
				Place(StaticPlace.initPlace);
				await Task.Delay(9000, token);
				TTS("引导流沙");
				await Task.Delay(7000, token);
				TTS("穿冲拳");
				await Task.Delay(8000, token);
				TTS("去反方向");
				await Task.Delay(15000, token);
				TTS("打石牢，穿冲拳");
				Place(StaticPlace.initPlace);
				await Task.Delay(13000, token);
				TTS("连续AOE");
				await Task.Delay(8000, token);
				TTS("顺劈加死刑");
				await Task.Delay(3000, token);
				if (!lbfull) BelowHP("8727", 0.15, "请注意土神血量和LB槽<se.6>", "stop2");
				await Task.Delay(3000, token);
				TTS("掀桌后穿，靠近T");
				await Task.Delay(4000, token);
				TTS("引导流沙穿冲拳，贴边");
				await Task.Delay(17000, token);
				TTS("顺劈加死刑");
				await Task.Delay(7000, token);
				TTS("引导流沙三次");
				await Task.Delay(8000, token);
				TTS("连续AOE");
			}, token);
		}),
		new(new Regex(@"^.{14} AddCombatant 03:.{8}:.+:00::1803:8728:.+:10000:::(?<x>[^:]+):(?<y>[^:]+):"),
			g => NEWp3ygjd(g["x"].Value, g["y"].Value)),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:2B6(?:B|C):[^:]+:.{8}:(?<name>[^:]+):"), g => {
			if (state != State.P3Start) return;
			ThreeBucket(g["name"].Value);
		}),
		new(new Regex(@".{14} AOEActionEffect 16:.{8}:[^:]+:2B66:"), _ => {
			Place(StaticPlace.initPlace);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:(?<id>.{8}):[^:]+:2B66:"), g => {
			var pos = GetXY(g["id"].Value);
			ttsafe = "";
			if (Math.Abs(pos.X - 100) <= 2 && Math.Abs(pos.Y - 114) <= 2) {
				ttsafe = "A点上北安全";
				Place(StaticPlace.safeA);
			}
			if (Math.Abs(pos.X - 86) <= 2 && Math.Abs(pos.Y - 100) <= 2) {
				ttsafe = "B点右东安全";
				Place(StaticPlace.safeB);
			}
			if (Math.Abs(pos.X - 100) <= 2 && Math.Abs(pos.Y - 86) <= 2) {
				ttsafe = "C点下南安全";
				Place(StaticPlace.safeC);
			}
			if (Math.Abs(pos.X - 114) <= 2 && Math.Abs(pos.Y - 100) <= 2) {
				ttsafe = "D点左西安全";
				Place(StaticPlace.safeD);
			}
			if (ttsafe == "") Log($"Taitan Loc Fail At:({pos.X},{pos.Y})");
			Broadcast(ttsafe, true);
			TTS(ttsafe);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:(?<id>.{8}):[^:]+:2CB8:"), g => {
			Broadcast("优先攻击石牢");
			Mark(g["id"].Value, "attack1");
		}),

		#endregion

		#region P41

		new(new Regex(@"^.{14} (?:\w+ )00:0044:(泰坦|Titan|タイタン):(我的……孩子们……终有一日……|Hie, my children, into the dark!|ぬぬぬぬ……無念……)"), _ => {
			ctsp3?.Cancel();
			ctsp3?.Dispose();
			ctsp3 = null;
			state = State.P4Start;
		}),
		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:2B76:"), _ => {
			ctsp41 = new();
			var token = ctsp41.Token;
			Task.Run(async () => {
				state = State.P41;
				TTS("读条一运");
				await Task.Delay(9000, token);
				TTS("开疾跑");
				await Task.Delay(1000, token);
				p41();
				await Task.Delay(4000, token);
				TTS("躲地裂");
				await Task.Delay(3000, token);
				TTS("贴边穿第四个字");
				await Task.Delay(5000, token);
				TTS("躲羽毛");
				Place(StaticPlace.initPlace);
				Place(StaticPlace.clear2);
				p41dihuo();
				await Task.Delay(9500, token);
				TTS("引导地火");
				await Task.Delay(14000, token);
				TTS("边缘贴贴");
				await Task.Delay(8000, token);
				TTS("穿");
				await Task.Delay(4000, token);
				TTS("连续AOE");
				await Task.Delay(5000, token);
				TTS("吸附炸弹，换T");
				await Task.Delay(6500, token);
				TTS("躲羽毛");
				await Task.Delay(4000, token);
				TTS("躲羽毛");
			}, token);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:2B7D:"), _ => {
			if (state != State.P41) return;
			if (ctsp41 == null) return;
			var token = ctsp41.Token;
			Task.Run(async () => {
				TTS("地火准备");
				Place(StaticPlace.p41place2);
				await Task.Delay(1000, token);
				TTS("3");
				await Task.Delay(1000, token);
				TTS("2");
				await Task.Delay(1000, token);
				TTS("1");
				await Task.Delay(1000, token);
				TTS("冲鸭");
				Place(StaticPlace.clear2);
			}, token);
		}),
		new(new Regex(@"^.{14} AddCombatant 03:.{8}:.+:00::1801:9020:.+:10000:::(?<x>[^:]+):86.30:"), g => {
			var dir = g["x"].Value switch {
				"113.70" => "右右右",
				"86.30" => "左左左"
			};
			PostTip(dir);
		}),

		#endregion

		#region P42

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:2D4C:"), _ => {
			ctsp41?.Cancel();
			ctsp41?.Dispose();
			ctsp41 = null;
			ctsp42 = new();
			var token = ctsp42.Token;
			Task.Run(async () => {
				state = State.P42;
				TTS("读条二运，神兵左侧集合");
				await Task.Delay(11000, token);
				TTS("三连流沙，右左上");
				Place(StaticPlace.p42place2);
				Place(StaticPlace.p42place4d4);
				if (MyJob == Jobs.D4) TTS("D4接线后去4点");
				await Task.Delay(1000, token);
				Place(StaticPlace.p42place2rf);
				await Task.Delay(10000, token);
				TTS("躲羽毛");
				await Task.Delay(7500, token);
				TTS("穿冲拳");
				var yflt = GetXYFromBnpcid("8730", 1)[0];
				var dir = GetDir(yflt);
				if (dir is "左上西北" or "右下东南") Place("4:112,112");
				if (dir is "左下西南" or "右上东北") Place("4:88,112");
				await Task.Delay(2000, token);
				if (MyJob is Jobs.H1 or Jobs.H2) TTS("热风奶去4点躲避");
				await Task.Delay(8000, token);
				TTS("躲羽毛");
				await Task.Delay(21000, token);
				TTS("穿台风眼");
				await Task.Delay(8000, token);
				TTS("顺劈");
				await Task.Delay(8000, token);
				TTS("二仇炮，击退做盾");
				await Task.Delay(11000, token);
				TTS("顺劈");
			}, token);
		}),

		#endregion

		#region P43

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:2D4D:"), _ => {
			ctsp42?.Cancel();
			ctsp42?.Dispose();
			ctsp42 = null;
			ctsp43 = new();
			var token = ctsp43.Token;
			Task.Run(async () => {
				state = State.P43;
				TTS("读条三运，准备站位");
				Place(StaticPlace.startp43);
				await Task.Delay(10000, token);
				TTS("看好点名");
				await Task.Delay(1000, token);
				Place(StaticPlace.initPlace);
				await Task.Delay(5000, token);
				TTS("鸟叫完走");
				await Task.Delay(3000, token);
				TTS("打石牢，躲羽毛，注意场边刚羽");
				await Task.Delay(9000, token);
				TTS("MT接线，穿冲拳分摊");
				await Task.Delay(8000, token);
				TTS("群奶，躲羽毛");
				await Task.Delay(10000, token);
				TTS("坦克LB");
			}, token);
		}),
		new(new Regex(@"^.{14} 264 108:.{8}:2B83:.{8}:1:(?<x>[^:]+):(?<y>[^:]+):"),
			g => {
				var ox = g["x"].Value;
				var oy = g["y"].Value;
				Place($"2:{ox},{oy}");
				if (float.Parse(ox) > 100 && float.Parse(oy) > 100) Broadcast("警告，潜地炮位置出现在右下");
			}),
		new(new Regex(@"^.{14} 263 107:.{8}:2B5A:(?<x>[^:]+):(?<y>[^:]+):"), g => {
			if (state != State.P43) return;
			p43dh(float.Parse(g["x"].Value), float.Parse(g["y"].Value));
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:2B6(?:B|C):[^:]+:.{8}:(?<name>[^:]+):"), g => {
			if (state != State.P43) return;
			Broadcast($"石牢{g["name"].Value}");
			TTS("打石牢");
			PostTip("石牢");
		}),
		new(new Regex(@"^.{14} TargetIcon 1B:.{8}:(?<RoleName>[^:]+):0000:0000:0010"), g => {
			p43dh_fq(g["RoleName"].Value, "风枪");
		}),

		#endregion

		#region P5

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]+:2B88:"), _ => {
			ctsp43?.Cancel();
			ctsp43?.Dispose();
			ctsp43 = null;
			ctsp5 = new();
			var token = ctsp5.Token;
			Task.Run(async () => {
				state = State.P5;
				Place(StaticPlace.p42place2);
				TTS("站位撞球");
				await Task.Delay(11000, token);
				TTS("A点集合");
				jzhA(false);
			}, token);
		}),
		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]+:2CD5:"), _ => {
			if (jzhStarted || ctsp5 == null) return;
			jzhStarted = true;
			var token = ctsp5.Token;
			Task.Run(async () => {
				TTS("土火风，三连流沙");
				await Task.Delay(2000, token);
				jzhA();
				await Task.Delay(3000, token);
				jzh2();
				await Task.Delay(2000, token);
				jzhA();
				await Task.Delay(3000, token);
				TTS("群盾团减");
				await Task.Delay(12000, token);
				TTS("A点放地火");
				jzhA(false);
				await Task.Delay(7000, token);
				TTS("开疾跑");
				await Task.Delay(2500, token);
				jzh2();
				await Task.Delay(16500, token);
				TTS("从侧边穿钢铁月环,去3点");
				jzh3(false);
				await Task.Delay(6500, token);
				TTS("穿穿穿");
				await Task.Delay(11500, token);
				TTS("躲羽毛");
			}, token);
		}),
		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]+:2CD4:"), _ => {
			if (jzhStarted || ctsp5 == null) return;
			jzhStarted = true;
			var token = ctsp5.Token;
			Task.Run(async () => {
				TTS("火风土，开疾跑");
				await Task.Delay(6000, token);
				jzh2();
				await Task.Delay(11000, token);
				TTS("从侧边穿钢铁月环,去3点");
				jzh3(false);
				await Task.Delay(5500, token);
				TTS("穿穿穿");
				await Task.Delay(9000, token);
				TTS("原地不动，躲羽毛，回A点");
				await Task.Delay(4000, token);
				jzhA(false);
				await Task.Delay(15000, token);
				TTS("三连流沙");
				await Task.Delay(2000, token);
				jzh2();
				await Task.Delay(4000, token);
				jzhA();
				await Task.Delay(3000, token);
				TTS("群盾团减");
			}, token);
		}),
		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]+:2CD3:"), _ => {
			if (jzhStarted || ctsp5 == null) return;
			jzhStarted = true;
			var token = ctsp5.Token;
			Task.Run(async () => {
				TTS("风火土，从侧边穿钢铁月环，去3点");
				jzh3(false);
				await Task.Delay(9000, token);
				TTS("穿穿穿");
				await Task.Delay(11500, token);
				TTS("原地不动，躲羽毛，回A点，放地火");
				jzhA(false);
				await Task.Delay(3500, token);
				TTS("火神开疾跑");
				await Task.Delay(2500, token);
				jzh2();
				await Task.Delay(6500, token);
				TTS("原地不动");
				await Task.Delay(9000, token);
				TTS("三连流沙");
				await Task.Delay(1000, token);
				jzhA();
				await Task.Delay(2000, token);
				jzh2();
				await Task.Delay(3000, token);
				TTS("群盾团减");
				await Task.Delay(1000, token);
				jzhA();
			}, token);
		}),

		#endregion
	];

	private void jzhA(bool tip = true) {
		Place(StaticPlace.p5jzhA);
		Broadcast("去A点", tip);
	}

	private void jzh2(bool tip = true) {
		Place(StaticPlace.p5jzh2);
		Broadcast("去2点", tip);
	}

	private void jzh3(bool tip = true) {
		Place(StaticPlace.p5jzh3);
		Broadcast("去3点", tip);
	}

	public override uint[] TerritoryIds() => [0x309];

	private void InitParams() {
		state = State.None;
		jzhStarted = false;
		ResetCts();
		Threebuckets.Clear();
		P43PlayerName.Clear();
		Zhuzis.Clear();
		Zhuzi2 = new Vector2();
		hs = "";
		ttsafe = "";
		ciyu.Reset();
		p43dhCount = 0;
		p43dhDist = [];
		Ubroadcast = GetPScale("Ubroadcast") == "1";
		Uthreebucket = GetPScale("Uthreebucket") == "1";
		Uauto = GetPScale("Uauto") == "1";
		Umarklocal = GetPScale("Umarklocal") == "1";
		Umark = GetPScale("Umark") == "1";
		lbfull = false;
	}

	private void ResetCts() {
		ctssimple?.Cancel();
		ctssimple?.Dispose();
		ctssimple = null;
		ctsp1?.Cancel();
		ctsp1?.Dispose();
		ctsp1 = null;
		ctsp2?.Cancel();
		ctsp2?.Dispose();
		ctsp2 = null;
		ctsp3?.Cancel();
		ctsp3?.Dispose();
		ctsp3 = null;
		ctsp41?.Cancel();
		ctsp41?.Dispose();
		ctsp41 = null;
		ctsp42?.Cancel();
		ctsp42?.Dispose();
		ctsp42 = null;
		ctsp43?.Cancel();
		ctsp43?.Dispose();
		ctsp43 = null;
		ctsp5?.Cancel();
		ctsp5?.Dispose();
		ctsp5 = null;
	}

	private static void ShowConfigForm(GroupCollection _) {
		TTS("正在打开配置，请检查后台窗口");
		Entry.Start();
	}

	private void BelowHP(string bnpcid, double percent, string desc, string marktype) {
		foreach (var en in Entity.GetEntities().Where(x => x.BNpcID.ToString() == bnpcid)) {
			if (!(1f * en.CurrentHP / en.MaxHP < percent)) return;
			Broadcast(desc, true);
			if (!Umarklocal && Umark) Mark($"{en.ID:X8}", marktype);
			return;
		}
	}

	private void Mark(string hexId, string marktype) {
		if (!Umark) return;
		RealPlugin.Instance.InvokeNamedCallback("mark", Umarklocal
			? $"{{\"ActorID\":0x{hexId},\"MarkType\":\"{marktype}\",\"LocalOnly\":\"True\"}}"
			: $"{{\"ActorID\":0x{hexId},\"MarkType\":\"{marktype}\"}}");
	}

	private void p41() {
		var jll = GetXYFromBnpcid("8722", 1)[0];
		var tt = GetXYFromBnpcid("8727", 1)[0];
		var yflt = GetXYFromBnpcid("8730", 1)[0];
		var yflt_dir = GetDir(yflt);
		var jjsb = GetXYFromBnpcid("8734")[0];
		var jjsb_dir = GetDir(jjsb);
		var tt_dir = GetDir(tt);
		var result = new List<P41Info>();
		foreach (var s in new[] { "上北", "下南", "左西", "右东" }) {
			if (jll.X > 100 && s == "右东" || jll.X < 100 && s == "左西" ||
			    jll.Y > 100 && s == "下南" || jll.Y < 100 && s == "上北" ||
			    s == tt_dir) continue;
			switch (s) {
				case "上北":
					if (jjsb_dir != "左上西北")
						result.Add(new P41Info(s, "上北然后逆时针", new Vector2(91, 83),
							yflt_dir != "左上西北" && yflt_dir != "右下东南"));
					if (jjsb_dir != "右上东北")
						result.Add(new P41Info(s, "上北然后顺时针", new Vector2(109, 83),
							yflt_dir != "右上东北" && yflt_dir != "左下西南"));
					break;
				case "下南":
					if (jjsb_dir != "左下西南")
						result.Add(new P41Info(s, "下南然后顺时针", new Vector2(91, 117),
							yflt_dir != "右上东北" && yflt_dir != "左下西南"));
					if (jjsb_dir != "右下东南")
						result.Add(new P41Info(s, "下南然后逆时针", new Vector2(109, 117),
							yflt_dir != "左上西北" && yflt_dir != "右下东南"));
					break;
				case "左西":
					if (jjsb_dir != "左上西北")
						result.Add(new P41Info(s, "左西然后顺时针", new Vector2(83, 91),
							yflt_dir != "左上西北" && yflt_dir != "右下东南"));
					if (jjsb_dir != "左下西南")
						result.Add(new P41Info(s, "左西然后逆时针", new Vector2(83, 109),
							yflt_dir != "右上东北" && yflt_dir != "左下西南"));
					break;
				case "右东":
					if (jjsb_dir != "右上东北")
						result.Add(new P41Info(s, "右东然后逆时针", new Vector2(117, 91),
							yflt_dir != "右上东北" && yflt_dir != "左下西南"));
					if (jjsb_dir != "右下东南")
						result.Add(new P41Info(s, "右东然后顺时针", new Vector2(117, 109),
							yflt_dir != "左上西北" && yflt_dir != "右下东南"));
					break;
			}
		}
		var esresult = result.Where(t => t.canES).ToList();
		esresult.AddRange(result.Where(t => !t.canES));
		var recommand = esresult[0];
		PostTip(recommand.desc);
		Place(recommand.first switch {
			"上北" => StaticPlace.safeA,
			"下南" => StaticPlace.safeC,
			"左西" => StaticPlace.safeD,
			"右东" => StaticPlace.safeB
		});
		if (Uauto) Log($"可能安全点:{string.Join("|", esresult)}。神兵:{jjsb_dir},土神:{tt_dir},火神:{yflt_dir}。");
		if (ctssimple != null) {
			var token = ctssimple.Token;
			Task.Run(async () => {
				await Task.Delay(recommand.canES ? 1000 : 6000, token);
				Place($"4:{recommand.after.X},{recommand.after.Y}");
			}, token);
		}
	}

	private void p43dh_fq(string s, string desc) {
		if (s == MyName) PostTip(desc);
		else if (desc == "地火") desc += "(仅供参考)";
		P43PlayerName.Add(s);
		Broadcast($"{desc} {s}");
		if (P43PlayerName.Count == 5) p43qdp();
	}

	private void p43qdp() {
		var nm = "";
		foreach (var player in Players) {
			var pn = player.name;
			if (player.job is Jobs.MT or Jobs.ST || P43PlayerName.Contains(pn)) continue;
			if (nm == "") nm = pn;
			else return;
		}
		if (nm == MyName) PostTip("潜地炮");
		Broadcast($"潜地炮 {nm}");
	}

	private void p43dh(float x, float y) {
		var pos = new Vector2(x, y);

		var iter = 0;
		foreach (var player in Players) {
			if (player.job is Jobs.MT or Jobs.ST) continue;
			var pn = player.name;
			var pl = BridgeFFXIV.GetNamedPartyMember(pn);
			var dist = Vector2.DistanceSquared(pos,
				new Vector2(float.Parse(pl.GetValue("x").ToString()),
					float.Parse(pl.GetValue("y").ToString())));
			if (p43dhCount == 0) p43dhDist.Add(new DHP43(pn, dist));
			else {
				p43dhDist[iter].dist = Math.Min(p43dhDist[iter].dist, dist);
				iter++;
			}
		}
		if (++p43dhCount != 3) return;
		p43dhDist.Sort((a, b) => a.dist.CompareTo(b.dist));
		p43dh_fq(p43dhDist[0].name, "地火");
		p43dh_fq(p43dhDist[1].name, "地火");
		p43dh_fq(p43dhDist[2].name, "地火");
	}

	private void p41dihuo() {
		// 初始点和圆心
		var startPoint = new Vector2(100, 118);
		var center = new Vector2(100, 100);

		// 自定义角度转弧度函数
		float ToRadians(float degrees) {
			return (float)(degrees * Math.PI / 180.0);
		}

		// 绘制原始点
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 25\nPos: {startPoint.X}, {startPoint.Y}\nScale: 1,1\nColor: 1, 1, 1, 0.9");
		if (Uauto) {
			RealPlugin.Instance.InvokeNamedCallback("command", $"/e 原始点:({startPoint.X:F1}, {startPoint.Y:F1})");
		}

		// 顺时针旋转三次：30度, 30度, 30度
		float currentAngle = 0;
		for (var i = 0; i < 3; i++) {
			currentAngle += 30;
			var rotatedPoint = RotatePoint(startPoint, center, ToRadians(currentAngle));

			// 使用PictoACT绘制omen
			RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 25\nPos: {rotatedPoint.X}, {rotatedPoint.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");

			if (Uauto) {
				RealPlugin.Instance.InvokeNamedCallback("command", $"/e 顺时针第{i + 1}次旋转(累计{currentAngle}度) - 点:({rotatedPoint.X:F1}, {rotatedPoint.Y:F1})");
			}
		}

		// 逆时针旋转三次：30度, 30度, 30度
		currentAngle = 0;
		for (var i = 0; i < 3; i++) {
			currentAngle -= 30;
			var rotatedPoint = RotatePoint(startPoint, center, ToRadians(currentAngle));

			// 使用PictoACT绘制omen
			RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 25\nPos: {rotatedPoint.X}, {rotatedPoint.Y}\nScale: 1,1\nColor: 1, 0.1, 0.1, 0.9");

			if (Uauto) {
				RealPlugin.Instance.InvokeNamedCallback("command", $"/e 逆时针第{i + 1}次旋转(累计{Math.Abs(currentAngle)}度) - 点:({rotatedPoint.X:F1}, {rotatedPoint.Y:F1})");
			}
		}
		return;

		// 旋转函数
		Vector2 RotatePoint(Vector2 point, Vector2 center, float angleRadians) {
			var translatedPoint = point - center;
			var rotatedPoint = new Vector2(
				translatedPoint.X * (float)Math.Cos(angleRadians) - translatedPoint.Y * (float)Math.Sin(angleRadians),
				translatedPoint.X * (float)Math.Sin(angleRadians) + translatedPoint.Y * (float)Math.Cos(angleRadians)
			);
			return rotatedPoint + center;
		}
	}


	private void p2zhuzi23() {
		if (Zhuzi2.X is 83.88f or 93.3f) Zhuzi2.X = 88;
		if (Zhuzi2.X is 106.7f or 116.12f) Zhuzi2.X = 112;
		if (Zhuzi2.Y is 83.88f or 93.3f) Zhuzi2.Y = 88;
		if (Zhuzi2.Y is 106.7f or 116.12f) Zhuzi2.Y = 112;
		Place($"2:{Zhuzi2.X},{Zhuzi2.Y};3:{200 - Zhuzi2.X},{200 - Zhuzi2.Y}");

		// 三点坐标
		var point3 = new Vector2(200 - Zhuzi2.X, 200 - Zhuzi2.Y);
		// 圆心
		var center = new Vector2(100, 100);

		// 自定义角度转弧度函数
		float ToRadians(float degrees) {
			return (float)(degrees * Math.PI / 180.0);
		}

		// 顺时针旋转四次：45度, 30度, 30度, 30度
		var clockwisePoints = new Vector2[4];
		float[] clockwiseAngles = [45, 30, 30, 30];
		float currentAngle = 0;

		for (var i = 0; i < 4; i++) {
			currentAngle += clockwiseAngles[i];
			var rotatedPoint1 = RotatePoint(point3, center, ToRadians(currentAngle));

			clockwisePoints[i] = rotatedPoint1;

			// 使用PictoACT绘制omen - 每次旋转只绘制一个点
			// 修改颜色从蓝色到绿色渐变
			var blue = 1.0f - i * 0.25f; // 蓝色分量从1递减到0.25
			var green = 0.1f + i * 0.25f; // 绿色分量从0.1递增到0.85
			RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 25\nPos: {rotatedPoint1.X}, {rotatedPoint1.Y}\nScale: 1,1\nColor: 0.1,{green:F1},{blue:F1}, 0.9");

			if (Uauto) {
				RealPlugin.Instance.InvokeNamedCallback("command", $"/e 顺时针第{i + 1}次旋转(累计{currentAngle}度) - 点:({rotatedPoint1.X:F1}, {rotatedPoint1.Y:F1})");
			}
		}

		// 逆时针旋转四次：45度, 30度, 30度, 30度
		var counterclockwisePoints = new Vector2[4];
		float[] counterclockwiseAngles = [-45, -30, -30, -30];
		currentAngle = 0;

		for (var i = 0; i < 4; i++) {
			currentAngle += counterclockwiseAngles[i];
			var rotatedPoint1 = RotatePoint(point3, center, ToRadians(currentAngle));

			counterclockwisePoints[i] = rotatedPoint1;

			// 使用PictoACT绘制omen - 每次旋转只绘制一个点
			// 修改颜色从蓝色到绿色渐变
			var blue = 1.0f - i * 0.25f; // 蓝色分量从1递减到0.25
			var green = 0.1f + i * 0.25f; // 绿色分量从0.1递增到0.85
			RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 25\nPos: {rotatedPoint1.X}, {rotatedPoint1.Y}\nScale: 1,1\nColor: 0.1,{green:F1},{blue:F1}, 0.9");

			if (Uauto) {
				RealPlugin.Instance.InvokeNamedCallback("command", $"/e 逆时针第{i + 1}次旋转(累计{currentAngle}度) - 点:({rotatedPoint1.X:F1}, {rotatedPoint1.Y:F1})");
			}
		}
		return;

		// 旋转函数
		Vector2 RotatePoint(Vector2 point, Vector2 center, float angleRadians) {
			var translatedPoint = point - center;
			var rotatedPoint = new Vector2(
				translatedPoint.X * (float)Math.Cos(angleRadians) - translatedPoint.Y * (float)Math.Sin(angleRadians),
				translatedPoint.X * (float)Math.Sin(angleRadians) + translatedPoint.Y * (float)Math.Cos(angleRadians)
			);
			return rotatedPoint + center;
		}
	}

	private void p2zhuzimark(int a1, int a2, int a3, int a4) {
		Mark(Zhuzis[a1].zid, "attack1");
		Mark(Zhuzis[a2].zid, "attack2");
		Mark(Zhuzis[a3].zid, "attack3");
		Mark(Zhuzis[a4].zid, "attack4");
		RealPlugin.Instance.InvokeNamedCallback("command", "/e 柱子标记完成");
	}

	private void FindIntersectionWithCircle(int a1, int a2, int a3, int a4) {
		// 步骤 1: 检查索引是否合法
		if (Zhuzis.Count <= a3 || Zhuzis.Count <= a4) {
			Console.WriteLine("索引超出范围");
			return;
		}

		// 步骤 2: 获取 pos 值
		var posA1 = Zhuzis[a1].pos;
		var posA2 = Zhuzis[a2].pos;
		var posA3 = Zhuzis[a3].pos;
		var posA4 = Zhuzis[a4].pos;

		// 假设 pos 是一个二维点，比如通过 DecodePos 方法解析
		var pointA1 = DecodePos(posA1);
		var pointA2 = DecodePos(posA2);
		var pointA3 = DecodePos(posA3); // 需要定义 DecodePos 方法
		var pointA4 = DecodePos(posA4);

		// 步骤 3: 计算中点 zdpos
		var zdpos = new Vector2(
			(pointA3.X + pointA4.X) / 2,
			(pointA3.Y + pointA4.Y) / 2
		);

		// 圆心和半径
		var center = new Vector2(100, 100);
		const float radius = 15;

		// 步骤 4: 计算从圆心到 zdpos 的方向向量
		var direction = Vector2.Normalize(zdpos - center);

		// 使用曼哈顿距离计算D1和D2的位置
		// 计算从圆心到pointA1和pointA2的曼哈顿距离方向
		var D1diff = pointA1 - center;
		var D2diff = pointA2 - center;

		// 计算曼哈顿距离
		var D1manhattan = Math.Abs(D1diff.X) + Math.Abs(D1diff.Y);
		var D2manhattan = Math.Abs(D2diff.X) + Math.Abs(D2diff.Y);

		// 归一化方向向量（保持曼哈顿距离的比例）
		var D1direction = new Vector2(D1diff.X / D1manhattan, D1diff.Y / D1manhattan);
		var D2direction = new Vector2(D2diff.X / D2manhattan, D2diff.Y / D2manhattan);

		// 沿方向移动 7 单位长度（使用曼哈顿距离）
		var D1resultPoint = center + D1direction * 7;
		var D2resultPoint = center + D2direction * 7;

		// 步骤 5: 射线与圆的交点（沿方向移动半径长度）
		var intersection = center + direction * radius;

		// 步骤 6: 输出交点
		RotateIntersection(intersection);
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {intersection.X}, {intersection.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 10\nPos: {D1resultPoint.X}, {D1resultPoint.Y}\nScale: 1,1\nColor: 0.1, 0.1, 1, 0.9");
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 10\nPos: {D2resultPoint.X}, {D2resultPoint.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");

		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D1坐标：({D1resultPoint.X}, {D1resultPoint.Y})");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D2坐标：({D2resultPoint.X}, {D2resultPoint.Y})");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火第四次移动坐标：({intersection.X}, {intersection.Y})");
	}

// 示例 DecodePos 方法（根据实际数据调整）
	private static Vector2 DecodePos(int pos) {
		// 这里假设每个 pos 对应一个预定义的坐标
		return pos switch {
			1 << 7 => new Vector2(100, 90),
			1 << 6 => new Vector2(107, 93),
			1 << 5 => new Vector2(110, 100),
			1 << 4 => new Vector2(107, 107),
			1 << 3 => new Vector2(100, 110),
			1 << 2 => new Vector2(93, 107),
			1 << 1 => new Vector2(90, 100),
			1 => new Vector2(93, 93),
			_ => new Vector2(100, 100)
		};
	}

	private void RotateIntersection(Vector2 intersection) {
		// 圆心
		var center = new Vector2(100, 100);

		// 逆时针旋转


		// 第一次旋转：40度
		var translatedPoint = intersection - center;
		var rotatedPoint1 = new Vector2(
			translatedPoint.X * (float)Math.Cos(ToRadians(40)) - translatedPoint.Y * (float)Math.Sin(ToRadians(40)),
			translatedPoint.X * (float)Math.Sin(ToRadians(40)) + translatedPoint.Y * (float)Math.Cos(ToRadians(40))
		);
		var finalPoint1 = rotatedPoint1 + center;
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint1.X}, {finalPoint1.Y}\nScale: 1,1\nColor: 0, 0.3, 0.5");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D4第三次移动坐标：({finalPoint1.X}, {finalPoint1.Y})");

		// 第二次旋转：45度
		translatedPoint = finalPoint1 - center;
		var rotatedPoint2 = new Vector2(
			translatedPoint.X * (float)Math.Cos(ToRadians(45)) - translatedPoint.Y * (float)Math.Sin(ToRadians(45)),
			translatedPoint.X * (float)Math.Sin(ToRadians(45)) + translatedPoint.Y * (float)Math.Cos(ToRadians(45))
		);
		var finalPoint2 = rotatedPoint2 + center;
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint2.X}, {finalPoint2.Y}\nScale: 1,1\nColor: 0, 0.2, 0.7");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D4第二次移动坐标：({finalPoint2.X}, {finalPoint2.Y})");

		// 第三次旋转：33度
		translatedPoint = finalPoint2 - center;
		var rotatedPoint3 = new Vector2(
			translatedPoint.X * (float)Math.Cos(ToRadians(33)) - translatedPoint.Y * (float)Math.Sin(ToRadians(33)),
			translatedPoint.X * (float)Math.Sin(ToRadians(33)) + translatedPoint.Y * (float)Math.Cos(ToRadians(33))
		);
		var finalPoint3 = rotatedPoint3 + center;
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint3.X}, {finalPoint3.Y}\nScale: 1,1\nColor: 0, 0.2, 0.9");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D4第一次坐标：({finalPoint3.X}, {finalPoint3.Y})");


		// 顺时针旋转
		Console.WriteLine("\n顺时针旋转：");

		// 第一次旋转：-40度
		translatedPoint = intersection - center;
		rotatedPoint1 = new Vector2(
			translatedPoint.X * (float)Math.Cos(ToRadians(-40)) - translatedPoint.Y * (float)Math.Sin(ToRadians(-40)),
			translatedPoint.X * (float)Math.Sin(ToRadians(-40)) + translatedPoint.Y * (float)Math.Cos(ToRadians(-40))
		);
		finalPoint1 = rotatedPoint1 + center;
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint1.X}, {finalPoint1.Y}\nScale: 1,1\nColor: 0, 0.3, 0.5");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D3第三次移动坐标：({finalPoint1.X}, {finalPoint1.Y})");

		// 第二次旋转：-45度
		translatedPoint = finalPoint1 - center;
		rotatedPoint2 = new Vector2(
			translatedPoint.X * (float)Math.Cos(ToRadians(-45)) - translatedPoint.Y * (float)Math.Sin(ToRadians(-45)),
			translatedPoint.X * (float)Math.Sin(ToRadians(-45)) + translatedPoint.Y * (float)Math.Cos(ToRadians(-45))
		);
		finalPoint2 = rotatedPoint2 + center;
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D3第二次移动坐标：({finalPoint2.X}, {finalPoint2.Y})");
		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint2.X}, {finalPoint2.Y}\nScale: 1,1\nColor: 0, 0.2, 0.7");

		// 第三次旋转：-33度
		translatedPoint = finalPoint2 - center;
		rotatedPoint3 = new Vector2(
			translatedPoint.X * (float)Math.Cos(ToRadians(-33)) - translatedPoint.Y * (float)Math.Sin(ToRadians(-33)),
			translatedPoint.X * (float)Math.Sin(ToRadians(-33)) + translatedPoint.Y * (float)Math.Cos(ToRadians(-33))
		);
		finalPoint3 = rotatedPoint3 + center;

		RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint3.X}, {finalPoint3.Y}\nScale: 1,1\nColor: 0, 0.2, 0.9");
		if (Uauto) RealPlugin.Instance.InvokeNamedCallback("Command", $"/e 地火D3第一次移动坐标：({finalPoint3.X}, {finalPoint3.Y})");
		return;

		// 自定义角度转弧度函数
		float ToRadians(float degrees) {
			return (float)(degrees * Math.PI / 180.0);
		}
	}


	private void p2zhuzi(string d, string x, string y) {
		if (x == "100.00" && y == "90.00") Zhuzis.Add(new Zhuzi(d, 1 << 7));
		else if (x == "107.00" && y == "93.00") Zhuzis.Add(new Zhuzi(d, 1 << 6));
		else if (x == "110.00" && y == "100.00") Zhuzis.Add(new Zhuzi(d, 1 << 5));
		else if (x == "107.00" && y == "107.00") Zhuzis.Add(new Zhuzi(d, 1 << 4));
		else if (x == "100.00" && y == "110.00") Zhuzis.Add(new Zhuzi(d, 1 << 3));
		else if (x == "93.00" && y is "107.00" or "106.95" or "107") Zhuzis.Add(new Zhuzi(d, 1 << 2));
		else if (x == "90.00" && y == "100.00") Zhuzis.Add(new Zhuzi(d, 1 << 1));
		else if (x == "93.00" && y == "93.00") Zhuzis.Add(new Zhuzi(d, 1));
		else Log($"未知柱子 {d},{x},{y}");

		if (Zhuzis.Count != 4) return;
		Zhuzis.Sort((a, b) => a.pos.CompareTo(b.pos));
		switch (Zhuzis.Aggregate(0, (current, z) => current | z.pos)) {
			case 0b11010010:
				Zhuzi2 = new Vector2(93.3f, 116.12f);
				p2zhuzimark(1, 0, 2, 3);
				FindIntersectionWithCircle(1, 0, 2, 3);
				break;
			case 0b01101001:
				Zhuzi2 = new Vector2(83.88f, 106.7f);
				p2zhuzimark(1, 0, 2, 3);
				FindIntersectionWithCircle(1, 0, 2, 3);
				break;
			case 0b10110100:
				Zhuzi2 = new Vector2(83.88f, 93.3f);
				p2zhuzimark(0, 3, 1, 2);
				FindIntersectionWithCircle(0, 3, 1, 2);
				break;
			case 0b01011010:
				Zhuzi2 = new Vector2(93.3f, 83.88f);
				p2zhuzimark(0, 3, 1, 2);
				FindIntersectionWithCircle(0, 3, 1, 2);
				break;
			case 0b00101101:
				Zhuzi2 = new Vector2(106.7f, 83.88f);
				p2zhuzimark(0, 3, 1, 2);
				FindIntersectionWithCircle(0, 3, 1, 2);
				break;
			case 0b10010110:
				Zhuzi2 = new Vector2(116.12f, 93.3f);
				p2zhuzimark(3, 2, 0, 1);
				FindIntersectionWithCircle(3, 2, 0, 1);
				break;
			case 0b01001011:
				Zhuzi2 = new Vector2(116.12f, 106.7f);
				p2zhuzimark(3, 2, 0, 1);
				FindIntersectionWithCircle(3, 2, 0, 1);
				break;
			case 0b10100101:
				Zhuzi2 = new Vector2(106.7f, 116.12f);
				p2zhuzimark(2, 1, 3, 0);
				FindIntersectionWithCircle(2, 1, 3, 0);
				break;
		}
		Place($"2:{Zhuzi2.X},{Zhuzi2.Y}");
	}


	private void NEWp3ygjd(string x, string y) {
		string ygjddir;
		var isRight = x == "95.00" && y is "111.00" or "112.00" ||
		              x == "88.00" && y == "95.00" ||
		              x == "105.00" && y == "88.00" ||
		              x == "112.00" && y == "105.00";

		var isLeft = x == "105.00" && y == "112.00" ||
		             x == "88.00" && y == "105.00" ||
		             x == "95.00" && y == "88.00" ||
		             x is "111.00" or "112.00" && y == "95.00";

		if (isRight) ygjddir = "右右右";
		else if (isLeft) ygjddir = "左左左";
		else return;

		// 定义坐标映射表
		var placeMap = new Dictionary<(string, string), string> {
			{ ("95.00", "111.00"), "3:105,105" }, //A右3
			{ ("95.00", "112.00"), "3:105,105" },
			{ ("111.00", "95.00"), "3:105,105" }, //D左3
			{ ("112.00", "95.00"), "3:105,105" },

			{ ("105.00", "112.00"), "3:95,105" }, //A左3
			{ ("88.00", "95.00"), "3:95,105" },

			{ ("105.00", "88.00"), "3:95,95" },
			{ ("88.00", "105.00"), "3:95,95" },

			{ ("95.00", "88.00"), "3:105,95" },
			{ ("112.00", "105.00"), "3:105,95" } //D右3
		};

		if (placeMap.TryGetValue((x, y), out var place)) {
			Place(place);
			var posValue = place.Split([':'], 2).LastOrDefault();
			RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 2\nPos: {posValue}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
			if (Uauto) RealPlugin.Instance.InvokeNamedCallback("command", $"/e 3桶移动坐标:{posValue}");
		}

		// ttsafe 分支逻辑
		int? index = null;
		switch (ttsafe) {
			case "A点上北安全":
				Place("1:100,106;2:100,100");
				RealPlugin.Instance.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2\n Pos: 100,106\n Scale: 1,1\n Color: 0.1, 1, 0.1, 0.9");
				index = 0;
				if (Uauto) RealPlugin.Instance.InvokeNamedCallback("command", "/e 1桶移动坐标:100,106");
				break;
			case "B点右东安全":
				Place("1:94,100;2:100,100"); //D1
				RealPlugin.Instance.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2 \nPos: 94,100 \nScale: 1,1 \nColor: 0.1, 1, 0.1, 0.9");
				index = 1;
				if (Uauto) RealPlugin.Instance.InvokeNamedCallback("command", "/e 1桶移动坐标:94,100");
				break;
			case "C点下南安全":
				Place("1:100,94;2:100,100"); //A1
				RealPlugin.Instance.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2 \nPos: 100,94 \nScale: 1,1 \nColor: 0.1, 1, 0.1, 0.9");
				index = 2;
				if (Uauto) RealPlugin.Instance.InvokeNamedCallback("command", "/e 1桶移动坐标:100,94");
				break;
			case "D点左西安全":
				Place("1:106,100;2:100,100");
				RealPlugin.Instance.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2 \nPos:106,100 \nScale: 1,1 \nColor: 0.1, 1, 0.1, 0.9");
				index = 3;
				if (Uauto) RealPlugin.Instance.InvokeNamedCallback("command", "/e 1桶移动坐标:106,100");

				break;
		}

		if (index.HasValue) {
			var place4 = StaticPlace.ygjdPlace4[index.Value, ygjddir == "左左左" ? 0 : 1];
			//Place(place4);
			// 提取 "4:xxx,yyy" 中冒号后的内容作为 Pos 值
			var posValue = place4.Split([':'], 2).LastOrDefault();
			RealPlugin.Instance.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f \nt: 5 \nPos: {posValue} \nScale: 1,1\nColor: 0.1, 1, 0.1");
			if (Uauto) RealPlugin.Instance.InvokeNamedCallback("command", $"/e 大怒震移动坐标:{posValue} ");
		}

		PostTip(ygjddir);
	}

	private void Broadcast(string s, bool tip = false) {
		if (tip) PostTip(s, tts: false);
		RealPlugin.Instance.InvokeNamedCallback("command", Ubroadcast ? $"/p {s} " : $"/e {s}");
	}

	private static string GetPScale(string name) => GetScalarVariable(true, "ptuw_" + name);

	private static void PostTip(string text, float x = 0, float y = 0, string id = "main", bool tts = true) {
		RealPlugin.Instance.QueueAction(fakectx, faketri, null,
			new ActionOld {
				ActionType = ActionOld.ActionTypeEnum.TextAura,
				TextAuraOp = nameof(ActionOld.AuraOpEnum.ActivateAura),
				TextAuraName = $"tuw_subtitle_${id}",
				TextAuraExpression = text,
				TextAuraTTLTickExpression = @"5-${_since}",
				TextAuraEffect = "Bold",
				TextAuraFontSize = "26.25",
				TextAuraXIniExpression = (1413 + x).ToString(),
				TextAuraYIniExpression = (333 + y).ToString(),
				TextAuraWIniExpression = "400",
				TextAuraHIniExpression = "200"
			}, DateTime.Now, true);
		if (tts) TTS(text);
	}

	private static void FsPlace(string pl1, string pl2) {
		Place(pl1 switch {
			"上北" => StaticPlace.p1fsN3,
			"下南" => StaticPlace.p1fsS3,
			"左西" => StaticPlace.p1fsW3,
			"右东" => StaticPlace.p1fsE3
		});
		Place(pl2 switch {
			"上北" => StaticPlace.p1fsN4,
			"下南" => StaticPlace.p1fsS4,
			"左西" => StaticPlace.p1fsW4,
			"右东" => StaticPlace.p1fsE4
		});
	}

	public static void FsPlaceByRule(string pos1, string pos2) {
		// 3点位优先顺序
		string[] p3Order = ["上北", "右东", "下南", "左西"];
		string[] input = [pos1, pos2];
		// 按优先级排序
		Array.Sort(input, (a, b) => Array.IndexOf(p3Order, a).CompareTo(Array.IndexOf(p3Order, b)));
		var p3 = input[0];
		var p4 = input[1];
		// 3点位
		switch (p3) {
			case "上北": Place(StaticPlace.p1fsN3); break;
			case "右东": Place(StaticPlace.p1fsE3); break;
			case "下南": Place(StaticPlace.p1fsS3); break;
			case "左西": Place(StaticPlace.p1fsW3); break;
		}
		// 4点位
		switch (p4) {
			case "上北": Place(StaticPlace.p1fsN4); break;
			case "右东": Place(StaticPlace.p1fsE4); break;
			case "下南": Place(StaticPlace.p1fsS4); break;
			case "左西": Place(StaticPlace.p1fsW4); break;
		}
	}


	private static string GetDir(Vector2 v) => GetDir(v.X, v.Y);

	private static string GetDir(string x, string y) => GetDir(float.Parse(x), float.Parse(y));

	private static string GetDir(float x, float y) {
		return x switch {
			> 110 when y > 110 => "右下东南",
			> 110 when y < 90 => "右上东北",
			< 90 when y < 90 => "左上西北",
			< 90 when y > 110 => "左下西南",
			> 115 => "右东",
			< 85 => "左西",
			_ => y switch {
				< 85 => "上北",
				> 115 => "下南",
				_ => ""
			}
		};
	}

	private static List<Vector2> GetXYFromBnpcid(string arg, int reqHP = -1) {
		return (arg.Any(c => c is (< '0' or > '9') and (< 'A' or > 'F') and (< 'a' or > 'f'))
			? null
			: (from en in BridgeFFXIV.GetAllEntities().Where(en => en.GetValue("bnpcid").ToString() == arg)
			where reqHP == -1 || int.Parse(en.GetValue("currenthp").ToString()) == reqHP
			select new Vector2(float.Parse(en.GetValue("x").ToString()), float.Parse(en.GetValue("y").ToString()))).ToList()) ?? throw new InvalidOperationException();
	}

	private static Vector2 GetXY(string id_xy) {
		if (id_xy.Contains(',')) {
			var ss = id_xy.Split(',');
			return new Vector2(float.Parse(ss[0]), float.Parse(ss[1]));
		}
		if (id_xy.Any(c => c is (< '0' or > '9') and (< 'A' or > 'F') and (< 'a' or > 'f'))) return new Vector2();
		var e = BridgeFFXIV.GetIdEntity(id_xy);
		return new Vector2(float.Parse(e.GetValue("x").ToString()), float.Parse(e.GetValue("y").ToString()));
	}

	private void ThreeBucket(string args) {
		foreach (var v in Players) {
			if (args != v.name) continue;
			switch (Threebuckets.Count) {
				case 0:
					Threebuckets.Add(v);
					break;
				case 1:
					if (Threebuckets[0].storder > v.storder) Threebuckets.Insert(0, v);
					else Threebuckets.Add(v);
					break;
				case 2:
					if (Threebuckets[0].storder > v.storder) Threebuckets.Insert(0, v);
					else if (Threebuckets[1].storder > v.storder) Threebuckets.Insert(1, v);
					else Threebuckets.Add(v);
					if (Uthreebucket) {
						RealPlugin.Instance.InvokeNamedCallback("command", $"/mk attack1 <{Threebuckets[0].partyorder}>");
						RealPlugin.Instance.InvokeNamedCallback("command", $"/mk attack2 <{Threebuckets[1].partyorder}>");
						RealPlugin.Instance.InvokeNamedCallback("command", $"/mk attack3 <{Threebuckets[2].partyorder}>");

						// 添加个人提示，让玩家知道自己点的是哪个桶
						if (Threebuckets[0].name == MyName) PostTip("一桶点你");
						else if (Threebuckets[1].name == MyName) PostTip("二桶点你");
						else if (Threebuckets[2].name == MyName) PostTip("三桶点你");
						if (ctssimple != null) {
							var token = ctssimple.Token;
							Task.Run(async () => {
								await Task.Delay(9000, token);
								RealPlugin.Instance.InvokeNamedCallback("command", "/mk attack1 <attack1>");
								RealPlugin.Instance.InvokeNamedCallback("command", "/mk attack2 <attack2>");
								RealPlugin.Instance.InvokeNamedCallback("command", "/mk attack3 <attack3>");
							}, token);
						}
					} else {
						RealPlugin.Instance.InvokeNamedCallback("command", $"/e attack1 <{Threebuckets[0].partyorder}>");
						RealPlugin.Instance.InvokeNamedCallback("command", $"/e attack2 <{Threebuckets[1].partyorder}>");
						RealPlugin.Instance.InvokeNamedCallback("command", $"/e attack3 <{Threebuckets[2].partyorder}>");

						// 添加个人提示，让玩家知道自己点的是哪个桶
						if (Threebuckets[0].name == MyName) PostTip("一桶点你");
						else if (Threebuckets[1].name == MyName) PostTip("二桶点你");
						else if (Threebuckets[2].name == MyName) PostTip("三桶点你");
					}
					Threebuckets.Clear();
					break;
			}

			break;
		}
	}


	private static class Entry {
		private static void RunConfigForm() {
			var configForm = new GameConfigForm(Info);

			var P2VFXSetting = new BijectDictionary<string, string>(
				("1", "开启"),
				("0", "关闭")
			);
			var VFXSetting = configForm.AddOptionGroup("设置");
			var P2VFXMode = new GameConfigForm.OptionCbx("团队播报", "ptuw_Ubroadcast", P2VFXSetting, "0",
				"小队可见提示，关闭则仅自己可见。");
			var P3VFXMode = new GameConfigForm.OptionCbx("头标", "ptuw_Umark", P2VFXSetting, "1",
				"柱子，停手等标记。");
			var Umarklocal = new GameConfigForm.OptionCbx("头标仅本地可见", "ptuw_Umarklocal", P2VFXSetting, "1",
				"开启后头标仅本地可见。");
			var three = new GameConfigForm.OptionCbx("三连桶", "ptuw_Uthreebucket", P2VFXSetting, "0",
				"与其他设置独立，开启仅本地可见也可以正常标三连桶。");
			var P5VFXMode = new GameConfigForm.OptionCbx("自动切目标", "ptuw_Utarget", P2VFXSetting, "0",
				"开启后自动选柱子奶桶");
			configForm.AddOption(three, VFXSetting);
			configForm.AddOption(P2VFXMode, VFXSetting);
			configForm.AddOption(P3VFXMode, VFXSetting);
			configForm.AddOption(Umarklocal, VFXSetting);
			configForm.AddOption(P5VFXMode, VFXSetting);
			var partylist = new PartyListPanel(Enum.GetValues<Jobs>().Select(i => i.ToString()).ToArray());
			configForm.AddPartyGroup(" 队员顺序保证和游戏内一致 ", partylist);
			configForm.Run();
			configForm.FormClosing += (_, _) => {
				var vs = RealPlugin.Instance.GetVariableStore(false);
				var valid = false;
				string[]? plist = null;
				lock (vs.List) {
					if (vs.List.TryGetValue(partylist.PlayerIdsLvarName, out var party)) {
						plist = party.Values.Select(i => (i as VariableScalar)?.Value).Where(i => !string.IsNullOrEmpty(i)).Cast<string>().ToArray();
						valid = party.Size == partylist.PlayerCount && plist.Length == partylist.PlayerCount;
					}
				}
				if (!valid || plist == null) return;
				lock (vs.Scalar) {
					if (!vs.Scalar.TryGetValue(partylist.PlayerIdxVarName, out var myIdx)) return;
					if (!int.TryParse(myIdx.Value, out var v)) return;
					MyJob = (Jobs)(v - 1);
					var me = Entity.GetMyself();
					MyName = me.Name;
					for (var i = 0; i < partylist.PlayerCount; i++)
						Instance.Players[i] = new Player((Jobs)i, i, Entity.GetEntityByID(plist[i]).Name);
					Instance.InitParams();
					var sb = new StringBuilder("绝神兵小队初始化完成。职业").Append(MyJob).Append('。');
					if (Instance.Ubroadcast) sb.Append("启用团队播报。");
					if (Instance.Uthreebucket) sb.Append("启用三连桶点名。");
					if (Instance.Uauto) sb.Append("启用FA坐标");
					if (Instance is { Umark: true, Umarklocal: true }) sb.Append("启用本地标记。");
					if (Instance is { Umark: true, Umarklocal: false }) sb.Append("启用小队可见标记。");
					var sbs = sb.ToString();
					Log(sbs);
					TTS(sbs);
				}
			};
		}

		[STAThread]
		public static void Start() {
			try {
				SetScalarVariable(false, $"{Info.ConfigName}_isRunning", "1");
				var staThread = new Thread(RunConfigForm);
				staThread.SetApartmentState(ApartmentState.STA);
				staThread.Start();
				staThread.Join();
				SetScalarVariable(false, $"{Info.ConfigName}_isRunning", null);
			} catch {
				SetScalarVariable(false, $"{Info.ConfigName}_isRunning", null);
				throw;
			}
		}
	}

	private record DHP43(string name, float dist) {
		internal readonly string name = name;
		internal float dist = dist;
	}

	private record P41Info {
		internal readonly Vector2 after;
		internal readonly bool canES;
		internal readonly string desc, first;

		internal P41Info(string first, string desc, Vector2 after, bool canES) {
			this.first = first;
			this.canES = canES;
			this.desc = desc;
			if (canES) this.desc += "(提前安全)";
			this.after = after;
		}

		public override string ToString() => desc;
	}

	private record Ciyu {
		internal readonly StringBuilder dmg = new();
		internal bool cs2;
		internal string? zid;

		internal void Reset() {
			zid = null;
			cs2 = false;
			dmg.Clear();
		}
	}

	private record Zhuzi {
		internal readonly StringBuilder dmg = new();
		internal readonly int pos;
		internal readonly string zid;
		internal bool cs2;

		internal Zhuzi(string zid, int pos) {
			this.zid = zid;
			this.pos = pos;
		}
	}

	private static class StaticPlace {
		internal const string initPlace = "A:100,82;B:118,100;C:100,118;D:82,100;3:93,93;4:107,107;1:100,100;2:87,87",
			clear2 = "2:clear",
			clear3 = "3:clear",
			clear4 = "4:clear",
			safeA = "1:100,84;2:98,82;3:102,82;4:100,92.5",
			safeB = "1:116,100;2:118,98;3:118,102;4:107.5,100",
			safeC = "1:100,116;2:102,118;3:98,118;4:100,107.5",
			safeD = "1:84,100;2:82,102;3:82,98;4:92.5,100",
			p1fsN3 = "3:100,90",
			p1fsN4 = "4:100,90",
			p1fsS3 = "3:100,110",
			p1fsS4 = "4:100,110",
			p1fsW3 = "3:90,100",
			p1fsW4 = "4:90,100",
			p1fsE3 = "3:110,100",
			p1fsE4 = "4:110,100",
			p2hsWEsafe = "2:90,100;3:110,100",
			p2hsNSsafe = "2:100,90;3:100,110",
			p41place2 = "2:100,111",
			p42place2 = "2:88,88",
			p42place4d4 = "4:100,110",
			p42place2rf = "2:100,112",
			startp43 = "A:100,82;B:94.5,83;1:89.5,85.5;2:85.5,89.5;3:83,94.5;4:82,100",
			p5jzhA = "B:101,81;C:101,83;D:99,81;4:99,83",
			p5jzh2 = "B:89,87;C:89,89;D:87,87;4:87,89",
			p5jzh3 = "B:94,92;C:94,94;D:92,92;4:92,94";
		internal static readonly string[,] ygjdPlace4 = {
			{ "4:101.1,109.6", "4:98.9,109.6" },
			{ "4:90.4,101.1", "4:90.4,98.9" },
			{ "4:98.9,90.4", "4:101.1,90.4" },
			{ "4:109.6,98.9", "4:109.6,101.1" }
		};
	}


	private readonly record struct Player {
		internal Jobs job => (Jobs)partyorder;
		internal readonly string name;
		internal readonly int partyorder, storder;

		internal Player(Jobs job, int partyorder, string name) {
			this.partyorder = partyorder;
			var o = 0;
			for (var i = 0; i < TBJobOrder.Length; i++) {
				if (job != TBJobOrder[i]) continue;
				o = i;
				break;
			}
			storder = o;
			this.name = name;
		}

		public override string ToString() => $"[{job}({partyorder})]:{name}";
	}
}