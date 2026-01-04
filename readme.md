本脚本系统旨在简单创建触发器，对标Cactbot。简单是宗旨。

本系统仍在开发中，部分API可能会频繁变动。

# IActPluginV1

对于开发经验更丰富的挂U，可以选择直接继承IActPluginV1写。跟ACT插件一样，脚本在加载的时候会执行InitPlugin方法(
不会执行界面绘制，但是可以用ImGui来替代实现)
，卸载的时候会执行DeInitPlugin方法。由于与IINACT深度绑定，插件可以自由访问全部的Dalamud、IINACT、Triggernometry等资源。

直接继承IActPluginV1的脚本上限极高，取决于开发者的水平，这里不详细描述，开发时对着源码和引用的dll研究一会儿就行。

# IScriptBase

IScriptBase继承了IActPluginV1，提供的是副本触发器的快速创建，支持TargetIcon，StartsCasting，StatusAdd日志行。IScriptBase已经重写了这两个方法为空，在载入的时候会自动定向到读取日志行。其需要指定的字段有TerritoryIds,TargetIconList,StartsCastingList,StatusAddList。

TerritoryIds是副本的TerritoryType，可以填写多个；如果留空则为全部匹配。

各种List就是日志行匹配了。

整个脚本不需要定义入口，override上述几个字段后会自动对地图检测并匹配日志行。

详细使用可以看这个仓库下面的各个.cs文件，一般还是能保证可以运行的，直接拿过来照着改就行了。

# IGShape

绘图脚本。

这个说明还不是很完善，可以照着仓库内已有的脚本参考着写。