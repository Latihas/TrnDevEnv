using System.Collections.Generic;
using Triggernometry.PScript;
using static Triggernometry.PScript.ScriptUtils;

public class ScriptYd : IScriptBase {
    private enum YdStatus {
        满月流,
        新月流
    }

    public override uint[] TerritoryIds() => [1318];
    private YdStatus? ydStatus;
    public override List<TargetIcon> TargetIconList => [
        new(MTTS("点你分散", 1000), Me_HexID, 0x0017),
        new(MTTS("点你陨石"), Me_HexID, 0x0083)
    ];
    public override List<StartsCasting> StartsCastingList => [
        new(MTTS("五加三"), Id: 0xB165),
        new(MTTS("子弹，分摊"), Id: 0xB130),
        new(MTTS("AOE"), Id: 0xB13B),
        new(MTTS("九连环，四次分摊"), Id: 0xB150),
        new(MTTS("死刑"), Id: 0xB12E),
        new((src, _, _) => {
            TTS("范围死刑");
            DrawShape(new IGCone(GetGameObjectById_Position(src), 12, BossFacingToPlayer(src), Deg2Rad(90), 5000));
        }, Id: 0xB12F),
        new((src, _, _) => {
            DrawShape(new IGCone(GetGameObjectById_Position(src)(), 20, GetGameObjectById_Rotation(src)() + Deg2Rad(120), Deg2Rad(240), 5000));
            if (ydStatus == YdStatus.满月流) {
                TTS("去左远离");
                DrawShape(new IGCircle(GetGameObjectById_Position(src)(), 5, 5000));
            }
            else if (ydStatus == YdStatus.新月流) {
                TTS("去左靠近");
                DrawShape(new IGRing(GetGameObjectById_Position(src)(), 20, 5, 5000));
            }
        }, Id: 0xB14E),
        new((src, _, _) => {
            DrawShape(new IGCone(GetGameObjectById_Position(src)(), 20, GetGameObjectById_Rotation(src)() - Deg2Rad(120), Deg2Rad(240), 5000));
            if (ydStatus == YdStatus.满月流) {
                TTS("去右远离");
                DrawShape(new IGCircle(GetGameObjectById_Position(src)(), 5, 5000));
            }
            else if (ydStatus == YdStatus.新月流) {
                TTS("去右靠近");
                DrawShape(new IGRing(GetGameObjectById_Position(src)(), 20, 5, 5000));
            }
        }, Id: 0xB14F)
    ];
    public override List<StatusAdd> StatusAddList => [
        new(() => ydStatus = YdStatus.满月流, 0x5FF),
        new(() => ydStatus = YdStatus.新月流, 0x600),
        new(MTTS("去黑"), 0x602, TargetId: Me_HexID, Count: 0x04),
        new(MTTS("去白"), 0x603, TargetId: Me_HexID, Count: 0x04)
    ];
}