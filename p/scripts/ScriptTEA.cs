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
	private CancellationTokenSource? ctsp1, ctsp2, ctsp3;
	public override bool IsDev => true;
	private string? id_shuijilao, id_huoshuizhishou;
	public override uint[] TerritoryIds() => [887];
	public override void DeInitPlugin() => ResetCts();
	private void CheckP1HpDelta() {
		if (id_shuijilao == null || id_huoshuizhishou == null) return;
		var shuijilao = Entity.GetEntityByID(id_shuijilao);
		var huoshuizhishou = Entity.GetEntityByID(id_huoshuizhishou);
		if (1f * shuijilao.CurrentHP / shuijilao.MaxHP - 1f * huoshuizhishou.CurrentHP / huoshuizhishou.MaxHP > 0.04)
			TTS("快打水基佬");
		if (1f * shuijilao.CurrentHP / shuijilao.MaxHP - 1f * huoshuizhishou.CurrentHP / huoshuizhishou.MaxHP < -0.04)
			TTS("快打手");
	}

	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex(@"^.{14} Director 21:.{8}:400000(?:03|1[026]|05|11)"), _ => {
			ActGlobals.oFormActMain.EndCombat(false);
			ClearAllIGShape();
			ResetCts();
			InitParams();
		}),
		#region P1

		new(new Regex(@"^.{14} (?:\w+ )25:(?<id>[0-9A-F]{8}):(有生命活水|living liquid|リビングリキッド):"), g => {
			id_shuijilao = g["id"].Value;
		}),
		new(new Regex(@"^.{14} (?:\w+ )25:(?<id>[0-9A-F]{8}):(活水之手|liquid limb|リキッドハンド):"), g => {
			id_huoshuizhishou = g["id"].Value;
		}),
		new(new Regex(@"^.{14} AddCombatant 03:.{8}:.+:9215:"), g => {
			TTS("水球出现");
		}),
		new(new Regex(@"^.{14} StartsCasting 14:.{8}:[^:]*:4826:"), g => {
			InitParams();
			ctsp1 = new();
			var token = ctsp1.Token;
			Task.Run(async () => {
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
		}),

		#endregion

		#region P15

		new(new Regex(@"^.{14} TargetIcon 1B:.{8}:(?<player>.*?):(?:[^:]*:){2}(?<shadow_id>00(4F|5[0123456])):.{8}:"), g => {
			var mjid = Convert.ToInt32(g["shadow_id"].Value, 16) - 0x4F + 1;
			var sb = new StringBuilder(mjid.ToString());
			sb.Append("号，");
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
			TTS(sb.ToString());
		}),

		#endregion

		#region P2

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:483E:"), g => {
			ctsp1?.Cancel();
			ctsp1?.Dispose();
			ctsp2 = new();
			var token = ctsp2.Token;
			Task.Run(async () => {
				TTS("上毒");
				await Task.Delay(6500, token);
				TTS("场外飞盘");
				await Task.Delay(10000, token);
				TTS("回场中后雪条清空");
				await Task.Delay(18000, token);
				TTS("3秒后水雷分摊");
				await Task.Delay(6000, token);
				TTS("两人引导火圈");
				await Task.Delay(6000, token);
				TTS("三三分组");
				await Task.Delay(18000, token);
				TTS("3秒后水雷分摊");
				await Task.Delay(11000, token);
				TTS("集火护盾");
				await Task.Delay(18000, token);
				TTS("3秒后水雷分摊");
				await Task.Delay(33000, token);
				TTS("双T血量清空后分摊");
				await Task.Delay(20000, token);
				TTS("加油啊！残暴正义号！");
				await Task.Delay(3000, token);
				TTS("干翻他们");
			}, token);
		}),
		new(new Regex(@"^.{14} StatusList 26:(?<id>[0-9A-F]{8}):(?<name>[^:]+):.*:085E:"), g => {
			if (ctsp2 == null) return;
			var token = ctsp2.Token;
			Task.Run(async () => {
				TTS(g["id"].Value == Me_HexID().ToString("X8") ? "水分摊点你" : "下次水分摊");
				await Task.Delay(24000, token);
				TTS("5秒后水分摊");
				await Task.Delay(2000, token);
				TTS("3");
				await Task.Delay(1000, token);
				TTS("2");
				await Task.Delay(1200, token);
				TTS("1");
			}, token);
		}),
		new(new Regex(@"^.{14} StatusList 26:(?<id>[0-9A-F]{8}):(?<name>[^:]+):.*:085F:"), g => {
			if (ctsp2 == null) return;
			var token = ctsp2.Token;
			Task.Run(async () => {
				TTS(g["id"].Value == Me_HexID().ToString("X8") ? "雷分摊点你" : "下次雷分摊");
				await Task.Delay(24000, token);
				TTS("5秒后雷分摊");
				await Task.Delay(2000, token);
				TTS("3");
				await Task.Delay(1000, token);
				TTS("2");
				await Task.Delay(1200, token);
				TTS("1");
			}, token);
		}),

		#endregion

		#region P3

		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:485A:"), g => {
			ctsp2?.Cancel();
			ctsp2?.Dispose();
			ctsp3 = new();
			var token = ctsp3.Token;
			Task.Run(async () => {
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
		new(new Regex(@"^.{14} (?:\w+ )03:.{8}:(夏诺雅|Shanoa|シャノア):"), g => {
			if (ctsp3 == null) return;
			var token = ctsp3.Token;
			Task.Run(async () => {
				await Task.Delay(33000, token);
				Place("A:96,100;B:100,96;C:106,100,115;D:100,104;1:95,100;2:105,100");
				await Task.Delay(22000, token);
				Place("A:100,85;B:115,100;C:100,115;D:85,100;1:100,100;2:clear");
			}, token);
		}),
		new(new Regex(@"^.{14} (?:\w+ )1[56]:[0-9A-F]{8}:(?<name>[^:]+?):485B:.+:121.50:0.00:-3.14:[0-9A-F]{8}:0"), g => {
			Place("A:95,82;B:105,82;C:95,92;D:105,92;");
		}),
		new(new Regex(@"^.{14} (?:\w+ )1[56]:[0-9A-F]{8}:(?<name>[^:]+):485B:.+:78.5:4.265151E-15:-4.792213E-05:[0-9A-F]{8}:0|^.{14} ActionEffect 1[56]:[0-9A-F]{8}:(?<name>[^:]+):485B:.+:100.00:78.50:0.00:"), g => {
			Place("A:95,118;B:105,118;C:95,108;D:105,108;");
		}),

		#endregion
	];

	private void ResetCts() {
		ctsp1?.Cancel();
		ctsp1?.Dispose();
		ctsp1 = null;
		ctsp2?.Cancel();
		ctsp2?.Dispose();
		ctsp2 = null;
		ctsp3?.Cancel();
		ctsp3?.Dispose();
		ctsp3 = null;
	}

	private void InitParams() {
		id_shuijilao = null;
		id_huoshuizhishou = null;
	}
}