using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Triggernometry.Core;
using Triggernometry.Core.Scripting;
using Triggernometry.FFXIV;
using Triggernometry.PScript;
using TriggernometryProxy;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptBA : IScriptBase {
	public string mdbw = "";
	public int count_bird, count_bbls;
	public override uint? TerritoryIds() => 827;

	public bool inRect(Entity entity, Vector4 rect) {
		var x = entity.PosX;
		var y = entity.PosY;
		return x >= rect.X && x <= rect.Z && y >= rect.Y && y <= rect.W;
	}

	private static string GetScale(string name) => ScriptHelper.GetScalarVariable(false, "ba_" + name);
	public override List<TargetIcon> TargetIconList => [
		new((tid, _) => RealPlugin.Instance.InvokeNamedCallback("command", $"/e 分摊：{ProxyPlugin.ObjectTable.First(i => i.Address.ToInt32() == (int)tid).Name}"), Id: 0x003E),
		new((tid, _) => RealPlugin.Instance.InvokeNamedCallback("command", $"/e 即将加速度判定：{ProxyPlugin.ObjectTable.First(i => i.Address.ToInt32() == (int)tid).Name}"), Id: 0x004B),
		new((tid, _) => RealPlugin.Instance.InvokeNamedCallback("command", $"/e 陨石：{ProxyPlugin.ObjectTable.First(i => i.Address.ToInt32() == (int)tid).Name}"), Id: 0x0039)
	];
	private readonly Action<ulong, int, ulong> a = (src, _, dst) => {
		TTS("范围死刑");
		DrawShape(new IGCone(GetGameObjectById_Position(src), 12, BossFacingToTarget(src, dst), Deg2Rad(90), 8000));
	};
	public override List<StartsCasting> StartsCastingList => [
		//白枪
		new(() => p("《光之枪》圆圈点名玩家[分散]"), Id: 0x3944),
		new(() => p("《强袭》[远离]落点"), Id: 0x394D),
		new(() => p("《三连枪》对MT死刑，其他人不用管"), Id: 0x3945),
		new(() => p("《真妖枪旋》全屏MOE，奶妈[做盾抬血]"), Id: 0x3946),
		//黑枪
		new(() => p("《处刑场》刚才动了的人开疾跑"), Id: 0x3933),
		new(() => p("《连装魔》倒三角点名玩家[集合](2人以上站一起即可)"), Id: 0x393E),
		new(() => p("《强袭》远离黑枪落点，[靠边站]"), Id: 0x392F),
		new(() => p("《三连枪》对MT死刑，其他人不用管"), Id: 0x3934),
		new(() => p("《妖枪乱击》所有人[不要移动](可以攻击)"), Id: 0x3932),
		new(() => p("《妖枪旋》月环，[站Boss脚底]"), Id: 0x3929),
		new(() => p("《妖枪振》钢铁，[远离Boss]"), Id: 0x3928),
		new(() => p("《真妖枪旋》全屏MOE，奶妈[做盾抬血]"), Id: 0x3935),
		new(() => p("《重力地雷/夜？》步进式AOE,点名玩家[绕场边跑]，其余玩家[远离点名玩家]"), Id: 0x392C),
		//莱丁
		new(() => p("《哀痛雷鸣》[躲开紫圈]"), Id: 0x3870),
		new(() => p("《雷枪》[分散场边放置]，场地中心不要有雷枪。优先集火【雷枪】，若【莱丁】血量见底（<5%）可直接打莱丁"), Id: 0x3876),
		new(() => p("《天逆矛》超大范围钢铁，半径约为半个场地，读条到一半时近战去场中[与远程贴贴]。不慎吃到会中【麻痹】，需要奶妈[康复]"), Id: 0x3868),
		new(() => p("《涡雷》全屏AOE，奶妈[做盾抬血]"), Id: 0x386E),
		new(() => p("《袭雷》场边多轮步进式地火，不会跑的[跟人群]"), Id: 0x3870),
		new(() => p("《旋·斩铁剑》即死月环，集合站到boss[脚底内圈]"), Id: 0x386A),
		new(() => p("《英灵魂》全屏AOE，奶妈[做盾抬血]"), Id: 0x387A),
		new(() => p("《战死击》小范围钢铁，[远离]boss[后靠近]"), Id: 0x387C),
		new(() => p("《招雷》场地边缘生成一圈【雷池】，[请勿进入]"), Id: 0x387F),
		new(() => p("《真眼击》对MT死刑，之后MT将【莱丁】[拉到场边]，近战[跟随]Boss，远程[站场中]"), Id: 0x387B),
		new(() => p("《罪恶荆棘》读条完后连线玩家[拉断连线],[注意地火]，然后迅速boss[脚下集合]"), Id: 0x3874),
		//小怪
		new(() => p("《狂暴》法系[催眠]【兵武半人马】"), Id: 0x3BFE),
		new(() => p("《魔法锤》近战[下踢]【兵武比布鲁斯】"), Id: 0x3BFD),
		new(() => p("《昏暗之章》近战[下踢]【兵武博学林鸮】"), Id: 0x3C0D),
		new(() => p("《玩具锤》法系[催眠]【兵武智蛙】"), Id: 0x3C03)
	];

	private void p(string s) {
	}

	public override List<(Regex, Action<GroupCollection>)> CustomList => [
		new(new Regex("TransformationId:28(?<x>.)$"), Groups => {
			var x = Groups["x"].Value;
			if (x == "7") p("《Boss变火》[去冰枪](蓝)脚下");
			if (x == "8") p("《Boss变冰》[去火枪](红)脚下");
		}),
		new(new Regex(".{14} ChatLog 00:0044:[^:]+:尔等理念…… 将吾从万古之诅咒中解放……"), _ => {
			p("【绝对的美德】《美杜莎投枪》（超大扇形）[靠近]Boss躲开");
			p("【绝对的美德】《以太乱流》场边生成8座塔，先[靠近Boss]躲《美杜莎投枪》，然后连线玩家[白踩黑，黑踩白，踩异色]");
		}),
		new(new Regex("_PlayActionTimeline AAA:203:C8(?<x>.):.{8}:"), Groups => {
			var x = Groups["x"].Value;
			if (x == "2") p("面向莱丁[去右]半场");
			if (x == "3") p("面向莱丁[去左]半场");
		}),
		new(new Regex("^.{14} AOEActionEffect 16:.{8}:[^:]+:37AA:[^:]+:.{8}:(?<name>[^:]+):"), Groups => {
			var name = Groups["name"].Value;
			RealPlugin.Instance.InvokeNamedCallback("command", $"/e 加速度炸弹：${name}");
		})
	];
}