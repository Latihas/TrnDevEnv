using System.Windows.Forms;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptPixelPerfect : IScriptBase {
	private IGDot dot = null!;

	public override void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) =>
		DrawShape(dot = new IGDot(Me_Position, 6, persist: true));

	public override void DeInitPlugin() => dot.toRemove = true;
}