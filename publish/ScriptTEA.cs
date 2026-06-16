using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Triggernometry.FFXIV;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptTEA : IScriptBase {
	private IGDot dot = null!;
	private CancellationTokenSource? ctssimple, ctsp1, ctsp2, ctsp3, ctsp4, ctsp5;

	public override void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) =>
		DrawShape(dot = new IGDot(Me_Position, 6));

	private string id_shuijilao, id_huoshuizhishou;
	public override void DeInitPlugin() => dot.toRemove = true;

	private void CheckP1HpDelta() {
		var shuijilao = Entity.GetEntityByID(id_shuijilao);
		var huoshuizhishou = Entity.GetEntityByID(id_huoshuizhishou);
		if (1f * shuijilao.CurrentHP / shuijilao.MaxHP - 1f * huoshuizhishou.CurrentHP / huoshuizhishou.MaxHP > 0.04)
			TTS("快打水基佬");
		if (1f * shuijilao.CurrentHP / shuijilao.MaxHP - 1f * huoshuizhishou.CurrentHP / huoshuizhishou.MaxHP < -0.04)
			TTS("快打手");
	}

	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		#region P1

		new(new Regex(@"^.{14} (?:\w+ )25:(?<id>[0-9A-F]{8}):(有生命活水|living liquid|リビングリキッド):"), g => {
			id_shuijilao = g["id"].Value;
		}),
		new(new Regex(@"^.{14} (?:\w+ )25:(?<id>[0-9A-F]{8}):(活水之手|liquid limb|リキッドハンド):"), g => {
			id_huoshuizhishou = g["id"].Value;
		}),
		new(new Regex(@"^.{14} (?:\w+ )03:.{8}:(栓塞|embolus|エンボラス):"), g => {
			TTS("水球出现");
		}),
		new(new Regex(@"^.{14} (?:\w+ )14:.{8}:[^:]*:4826:"), g => {
			ResetCts();
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
			});
		}),

		#endregion
	];

	private void ResetCts() {
	}

	private void InitParams() {
		id_shuijilao = "";
		id_huoshuizhishou = "";
	}
}