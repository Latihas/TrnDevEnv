using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Triggernometry.FFXIV;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptTEA : IScriptBase {
	private enum State {
		None,
		P1,
		P2,
		P3,
		P4_1,
		P4_2
	}

	public override string Desc => "绝亚，配合可达鸭使用";
	private State state = State.None;
	public override bool IsDev => true;
	private string? id_shuijilao, id_huoshuizhishou;
	private bool p2lei, p2shui;
	private int p43dhCount;
	private DateTime p43dhLastTrigger = DateTime.MinValue;
	private readonly Lock p43dhLock = new();
	private readonly List<(string, int)> TetherList = [];

	public override uint[] TerritoryIds() => [887];
	public override void DeInitPlugin() => ResetCts();

	private void InitParams() {
		id_shuijilao = null;
		id_huoshuizhishou = null;
		state = State.None;
		p2shui = false;
		p2lei = false;
		lock (TetherList) TetherList.Clear();
		p43dhCount = 0;
		p43dhLastTrigger = DateTime.MinValue;
	}

	private void CheckP1HpDelta() {
		if (id_shuijilao == null || id_huoshuizhishou == null) return;
		var shuijilao = Entity.GetEntityByID(id_shuijilao);
		var huoshuizhishou = Entity.GetEntityByID(id_huoshuizhishou);
		if (1f * shuijilao.CurrentHP / shuijilao.MaxHP - 1f * huoshuizhishou.CurrentHP / huoshuizhishou.MaxHP > 0.04)
			TTS("快打水基佬");
		if (1f * shuijilao.CurrentHP / shuijilao.MaxHP - 1f * huoshuizhishou.CurrentHP / huoshuizhishou.MaxHP < -0.04)
			TTS("快打手");
	}

	public override List<StartsCasting> StartsCastingList => [
		new(() => {
			if (state != State.None) return;
			InitParams();
			var token = CtsPool.CreateCts("P1");
			CtsPool.CreateCts("Global");
			Task.Run(async () => {
				state = State.P1;
				TTS("全屏AOE");
				await Task.Delay(6000, token);
				TTS("处理猜拳");
				await Task.Delay(12000, token);
				//播放声音?
				await Task.Delay(13000, token);
				TTS("水球开始移动");
				await Task.Delay(15500, token);
				TTS("万变水波");
				CheckP1HpDelta();
				await Task.Delay(7000, token);
				TTS("第一组引导");
				await Task.Delay(4500, token);
				TTS("第二组引导");
				await Task.Delay(5000, token);
				TTS("六连拍地板");
				await Task.Delay(5000, token);
				CheckP1HpDelta();
				await Task.Delay(4000, token);
				TTS("全屏AOE");
				await Task.Delay(7000, token);
				TTS("快驱散");
				await Task.Delay(6000, token);
				TTS("万变水波");
				CheckP1HpDelta();
				await Task.Delay(5000, token);
				TTS("第一组引导");
				await Task.Delay(5000, token);
				TTS("第二组引导");
				await Task.Delay(6000, token);
				TTS("处理猜拳");
				await Task.Delay(6000, token);
				CheckP1HpDelta();
			}, token);
		}, Id: 0x4826)
	];
	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex(@"^.{14} Director 21:.{8}:400000(?:03|1[026]|05|11)"), _ => {
			ActGlobals.oFormActMain.EndCombat(false);
			ClearAllIGShape();
			InitParams();
			ResetCts();
		}),
		new(new Regex(@"^.{14} 260 104:.:1:.:1"), _ => {
			InitParams();
			ResetCts();
		}),

		#region P1

		new(new Regex(@"^.{14} (?:\w+ )25:(?<id>[0-9A-F]{8}):(有生命活水|living liquid|リビングリキッド):"), g => {
			id_shuijilao = g["id"].Value;
		}),
		new(new Regex(@"^.{14} (?:\w+ )25:(?<id>[0-9A-F]{8}):(活水之手|liquid limb|リキッドハンド):"), g => {
			id_huoshuizhishou = g["id"].Value;
		}),
		new(new Regex(@"^.{14} AddCombatant 03:(?<id>.{8}):.+:9215:"), g => {
			TTS("水球出现");
			var en = Entity.GetEntityByID(g["id"].Value);
			var pos = en.Pos;
			(pos.Y, pos.Z) = (pos.Z, pos.Y);
			DrawShape(new IGRay(pos, 20, en.Heading, 30));
		}),
		new(new Regex(@"^.{14} 261 105:Add:(?<id>.{8}):BNpcID:2C4A:BNpcNameID:23FE:"), g => {
			if (!CtsPool.GetToken("P1", out var token)) return;
			Task.Run(async () => {
				var en = () => Entity.GetEntityByID(g["id"].Value);
				var a = () => {
					var pos = en().Pos;
					(pos.Y, pos.Z) = (pos.Z, pos.Y);
					return pos;
				};
				// if(en.CurrentHP>0) DrawShape(new IGCircle(a, 8.8, 4000, color: 0x20FFFFFFu));
				await Task.Delay(2000);
				if (en().CurrentHP > 0) DrawShape(new IGCircle(a, 8.8, 4000, 0x20FFFFFFu));
				await Task.Delay(12600);
				if (en().CurrentHP > 0) DrawShape(new IGCircle(a, 8.8, 4000, 0x20FFFFFFu));
				await Task.Delay(23300);
				if (en().CurrentHP > 0) DrawShape(new IGCircle(a, 8.8, 4000, 0x20FFFFFFu));
				// var party = ProxyPlugin.currentPartyInfo;
			}, token);
		}),

		#endregion

		#region P15

		new(new Regex(@"^.{14} TargetIcon 1B:(?<targetId>.{8}):(?<player>.*?):(?:[^:]*:){2}(?<shadow_id>00(4F|5[0123456])):.{8}:"), g => {
			CtsPool.DestroyCts("P1");
			var mjid = Convert.ToInt32(g["shadow_id"].Value, 16) - 0x4F + 1;
			if (g["targetId"].Value == Me_HexID().ToString("X8")) {
				var sb = new StringBuilder(mjid.ToString());
				sb.Append("号，");
				if (state == State.P1)
					sb.Append(mjid switch {
						1 => "内圈领跑",
						2 => "内圈跟随",
						3 => "内圈领跑",
						4 => "内圈跟随",
						5 => "外圈领跑",
						6 => "外圈跟随",
						7 => "外圈领跑",
						8 => "外圈跟随"
					});
				else if (state == State.P3)
					sb.Append(mjid switch {
						1 => "面朝外，踩第三次灵泉",
						2 => "面朝内，踩第三次灵泉",
						3 => "面朝外，引导超级跳",
						4 => "面朝内，引导超级跳",
						5 => "面朝外，踩第一次灵泉",
						6 => "面朝内，踩第一次灵泉",
						7 => "面朝外，踩第二次灵泉",
						8 => "面朝内，踩第二次灵泉"
					});
				TTS(sb.ToString());
			}
		}),

		#endregion

		#region P2

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:483E:"), _ => {
			var token = CtsPool.CreateCts("P2");
			Task.Run(async () => {
				state = State.P2;
				TTS("上毒");
				await Task.Delay(6500, token);
				TTS("场外飞盘");
				await Task.Delay(10000, token);
				TTS("回场中后血条清空");
				await Task.Delay(18000, token);
				TTS("5秒后水雷分摊");
				await Task.Delay(6000, token);
				TTS("两人引导火圈");
				await Task.Delay(6000, token);
				TTS("三三分组");
				await Task.Delay(18000, token);
				TTS("3秒后水雷分摊");
				await Task.Delay(11000, token);
				TTS("集火护盾");
				await Task.Delay(18000, token);
				TTS("5秒后水雷分摊");
				await Task.Delay(33000, token);
				TTS("双T血量清空后分摊");
				await Task.Delay(20000, token);
				TTS("加油啊！残暴正义号！");
				await Task.Delay(3000, token);
				TTS("干翻他们");
			}, token);
		}),
		new(new Regex(@"^.{14} StatusList 26:(?<id>[0-9A-F]{8}):(?<name>[^:]+):.*:085E:"), g => {
			if (state != State.P2) return;
			// if (!CtsPool.GetToken("P2", out var token)) return;
			// Task.Run(async () => {
			if (p2shui) return;
			p2shui = true;
			if (g["id"].Value == Me_HexID().ToString("X8")) TTS("水分摊点你");
			// await Task.Delay(24000, token);
			// TTS("5秒后分摊");
			// await Task.Delay(2000, token);
			// TTS("3");
			// await Task.Delay(1000, token);
			// TTS("2");
			// await Task.Delay(1200, token);
			// TTS("1");
			// }, token);
		}),
		new(new Regex(@"^.{14} StatusList 26:(?<id>[0-9A-F]{8}):(?<name>[^:]+):.*:085F:"), g => {
			if (state != State.P2) return;
			// Task.Run(async () => {
			if (p2lei) return;
			p2lei = true;
			if (g["id"].Value == Me_HexID().ToString("X8")) TTS("雷分摊点你");
			// await Task.Delay(24000, token);
			// TTS("5秒后分摊");
			// await Task.Delay(2000, token);
			// TTS("3");
			// await Task.Delay(1000, token);
			// TTS("2");
			// await Task.Delay(1200, token);
			// TTS("1");
			// }, token);
		}),

		#endregion

		#region P3

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:485A:"), _ => {
			CtsPool.DestroyCts("P2");
			var token = CtsPool.CreateCts("P3");
			Task.Run(async () => {
				state = State.P3;
				Place("A:100,85;B:115,100;C:100,115;D:85,100;1:95,100;2:105,100");
				TTS("时间停止");
				await Task.Delay(22000, token);
				TTS("死刑");
				await Task.Delay(18000, token);
				TTS("一运开始");
				//禁用时停buff，启用禁止接近
				await Task.Delay(28000, token);
				TTS("去残暴和亚历山大之间");
				await Task.Delay(6000, token);
				TTS("引导喷火");
				await Task.Delay(15000, token);
				TTS("看真心，去两侧");
				await Task.Delay(9000, token);
				TTS("飞机斩杀");
				await Task.Delay(8000, token);
				TTS("一运结束");
				//禁用禁止接近
				await Task.Delay(2000, token);
				TTS("死刑");
				await Task.Delay(20000, token);
				TTS("二运开始，去正义胖子脚下");
				await Task.Delay(41000, token);
				TTS("集合分摊，然后三三分组");
				await Task.Delay(10000, token);
				TTS("两次AOE");
				await Task.Delay(22000, token);
				TTS("软狂暴");
				await Task.Delay(38000, token);
				TTS("骑士翅膀");
			}, token);
		}),
		new(new Regex(@"^.{14} AddCombatant 03:.{8}:(夏诺雅|Shanoa|シャノア):"), _ => {
			if (!CtsPool.GetToken("P3", out var token)) return;
			Task.Run(async () => {
				await Task.Delay(33000, token);
				Place("A:96,100;B:100,96;C:106,100;D:100,104;1:95,100;2:105,100");
				await Task.Delay(22000, token);
				Place("A:100,85;B:115,100;C:100,115;D:85,100;1:100,100;2:clear");
			}, token);
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:.+?:485B:[^:]*:.{8}:[^:]*:[^:]*:[^:]*:(?<y>[^:]*):"), g => {
			var y = float.Parse(g["y"].Value);
			if (y > 120) Place("A:95,82;B:105,82;C:95,92;D:105,92");
			else if (y < 80) Place("A:95,118;B:105,118;C:95,108;D:105,108");
		}),

		#endregion

		#region P4_0.5

		new(new Regex(@"^.{14} StatusList 26:.{8}:(?<name>[^:]+):.*:0869:"), g => {
			Log($"大光: {g["name"].Value}");
		}),
		new(new Regex(@"^.{14} StatusList 26:.{8}:(?<name>[^:]+):.*:086B:"), g => {
			Log($"小光: {g["name"].Value}");
		}),

		#endregion

		#region P4_1

		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:487B:"), _ => {
			state = State.P4_1;
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:4B0D:"), _ => {
			if (!CtsPool.GetToken("Global", out var token)) return;
			TTS("动1");
			Log("动1");
			Task.Run(async () => {
				await Task.Delay(20000, token);
				TTS("动动动");
			}, token);
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:4899:"), _ => {
			if (!CtsPool.GetToken("Global", out var token)) return;
			TTS("动2");
			Log("动2");
			Task.Run(async () => {
				await Task.Delay(15000, token);
				TTS("动动动");
			}, token);
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:4B0E:"), _ => {
			if (!CtsPool.GetToken("Global", out var token)) return;
			TTS("静1");
			Log("静1");
			Task.Run(async () => {
				await Task.Delay(20000, token);
				TTS("停停停");
			}, token);
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:489A:"), _ => {
			if (!CtsPool.GetToken("Global", out var token)) return;
			TTS("静2");
			Log("静2");
			Task.Run(async () => {
				await Task.Delay(15000, token);
				TTS("停停停");
			}, token);
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:489F:.+:91.01:78.29:"), _ => TTS("左左左")),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:489F:.+:108.99:78.29:"), _ => TTS("右右右")),
		new(new Regex(@"^.{14} Tether 23:.{8}:(?<name>[^:]+):(?<shadow_id>.{8}):.+?:0062:.{8}:000F:"), g => {
			if (state == State.P4_1)
				lock (TetherList) {
					TetherList.Add((g["name"].Value, Convert.ToInt32(g["shadow_id"].Value, 16)));
					if (TetherList.Count != 8) return;
					TetherList.Sort((a, b) => a.Item2.CompareTo(b.Item2));
					var circle = TetherList[6].Item1;
					string[] lightning = [TetherList[5].Item1, TetherList[4].Item1, TetherList[3].Item1];
					string[] fentan = [TetherList[7].Item1, TetherList[2].Item1, TetherList[1].Item1, TetherList[0].Item1];
					Log($"大圈: {circle}");
					Log($"闪电: {string.Join(", ", lightning)}");
					Log($"分摊: {string.Join(", ", fentan)}");
					if (circle == Me_Name) TTS("大圈，远离人群");
					else if (lightning.Contains(Me_Name)) TTS("闪电，左侧");
					else if (fentan.Contains(Me_Name)) TTS("分摊，右侧");
					TetherList.Clear();
				}
			else if (state == State.P4_2) {
				lock (TetherList) {
					TetherList.Add((g["name"].Value, Convert.ToInt32(g["shadow_id"].Value, 16)));
					if (TetherList.Count != 8) return;
					TetherList.Sort((a, b) => a.Item2.CompareTo(b.Item2));
					string[] smallLight = [TetherList[0].Item1, TetherList[2].Item1, TetherList[4].Item1];
					var bigLight = TetherList[6].Item1;
					var farSmallDark = TetherList[1].Item1;
					var nearSmallDark = TetherList[3].Item1;
					var wirelessSmallDark = TetherList[5].Item1;
					var bigDark = TetherList[7].Item1;
					Log($"小光: {string.Join(", ", smallLight)}");
					Log($"大光: {bigLight}");
					Log($"远线小暗: {farSmallDark}");
					Log($"近线小暗: {nearSmallDark}");
					Log($"无线小暗: {wirelessSmallDark}");
					Log($"大暗: {bigDark}");
					if (smallLight.Contains(Me_Name)) TTS("小光小光，B点左上");
					else if (bigLight == Me_Name) TTS("大光，A点最上");
					else if (farSmallDark == Me_Name) TTS("远线小暗，B点左下");
					else if (nearSmallDark == Me_Name) TTS("近线小暗，B点左上");
					else if (wirelessSmallDark == Me_Name) TTS("无线小暗，B点左中");
					else if (bigDark == Me_Name) TTS("大暗，B点右侧");
					TetherList.Clear();
				}
			}
		}),

		#endregion

		#region P4_2

		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]+:4B13:"), _ => {
			state = State.P4_2;
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:48A0:"), _ => {
			if (!CtsPool.GetToken("Global", out var token)) return;
			TTS("分散");
			Log("分散");
			Task.Run(async () => {
				await Task.Delay(27600, token);
				TTS("分散");
			}, token);
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:48A1:"), _ => {
			if (!CtsPool.GetToken("Global", out var token)) return;
			TTS("分摊");
			Log("分摊");
			Task.Run(async () => {
				await Task.Delay(27600, token);
				TTS("分摊");
			}, token);
		}),
		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:489E:.+?:::(?<x>[^:]*?):(?<y>[^:]*?):0"), g => {
			var x = float.Parse(g["x"].Value);
			var y = float.Parse(g["y"].Value);
			var b = x > 115 && y < 110 ? "月环2B" :
				x < 85 && y < 110 ? "月环4D" :
				y > 110 ? "月环3C" : null;
			if (b == null) return;
			TTS(b);
			if (!CtsPool.GetToken("Global", out var token)) return;
			Task.Run(async () => {
				await Task.Delay(26000, token);
				TTS(b);
			}, token);
		}),

		#endregion

		new(new Regex(@"^.{14} ActionEffect 15:.{8}:[^:]+:488F:[^:]+:.{8}:.+:(?:92|100|108).00:108.00:"), _ => {
			lock (p43dhLock) {
				if ((DateTime.Now - p43dhLastTrigger).TotalSeconds > 1) {
					p43dhLastTrigger = DateTime.Now;
					p43dhCount++;
					if (p43dhCount == 3) {
						p43dhCount = 0;
						if (!CtsPool.GetToken("Global", out var token)) return;
						Task.Run(async () => {
							TTS("3");
							await Task.Delay(900, token);
							TTS("2");
							await Task.Delay(900, token);
							TTS("1");
							await Task.Delay(1000, token);
							TTS("冲鸭");
						}, token);
					}
				}
			}
		})
	];
}