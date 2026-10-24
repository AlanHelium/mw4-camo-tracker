# 数据来源与范围

整理日期：2026-10-23。v0.7.0 起应用内置 `data/catalog.json`，支持独立资料包覆盖；单项保留资料来源。旧的 `src/Data/catalog.json` 是初始采集记录。发布和更新方法见 [资料更新](UPDATES.md)。

- [Call of Duty 官方进度博客](https://www.callofduty.com/ca/en/blog/2026/10/call-of-duty-modern-warfare-4-ready-for-launch-progression-prestige)：路线、巅峰、部分挑战与外观参考。
- [Upblazed tracker](https://upblazed.com/tools/mw4-camo-tracker)：33 把武器的公开名称与条件事实。未复制网站界面、代码或其模拟迷彩图。
- [用户提供的小黑盒解读](https://www.xiaoheihe.cn/app/bbs/link/c94c72acbda3)：辅助核对路线区别，本身不是官方规则。

33 把武器的 462 条军事、特殊与完成主义记录，另附每把 4 项武器巅峰与 10 项全局路线，共 604 条。数据量不等于核验程度：少数条件未公开、部分社区条目与官方描述冲突，大多数特殊迷彩的确切开放等级未知。

- 冲突或缺失的要求不按数值自动判定完成，以游戏内实际解锁勾选为准。
- 开放等级未知的特殊迷彩等待用户确认「游戏中已开放此挑战」。
- 33 把武器的普通满级从官方首发武器资料页填写；未知旧存档自动补齐，保留用户已填的满级值。
- 武器巅峰满级、巅峰大师 150 / 250 等门槛分别判断。
- 勾选历史黄金补齐本武器必要前置；不推测其他武器已解锁，不从历史迷彩推断当前转生后的等级。
- 多人推荐与 DMZ 条件参考分开，Warzone 未核实项目保留不确定性。

官方素材地址见 [SOURCES](../src/Assets/SOURCES.md)。部分图片从官方合集裁切，其他武器上的显示只是迷彩样式参考；缺少对应图时使用类别标识。

v0.5.0 加入 [官方英文武器资料](https://www.callofduty.com/uk/en/blog/2026/10/call-of-duty-modern-warfare-4-launch-multiplayer-operators-weapons-equipment-create-a-class) 与 [官方简中资料](https://www.callofduty.com/cn/zh/blog/2026/10/call-of-duty-modern-warfare-4-launch-multiplayer-operators-weapons-equipment-create-a-class) 的 33 张首发枪型卡。`src/Data/weapons.json` 逐项记录原图 URL、裁切位置、官方中英文名及满级。裁切仅保留原厂枪型，不拼接或重绘涂装。已逐张查看卡片中的武器名称；官方 HTML 少数 alt 文案与实际卡片不符，以图内名称和对应标题为准。

99 个特殊迷彩的中文名为本工具参考翻译，英文原名保留用于游戏对照，不声称是官方简中翻译。已有中文的路线名沿用既有资料，英文名与条件来自原始来源字段或已核对的官方门槛。武器中文名直接采用官方简中首发资料的武器标题，官方保留 M4 等型号时原样保留，不用类别名冒充中文名。用户改写任务后不再显示旧任务的英文条件，避免错误对照。

设计参考 Call of Duty 官方进度页面的路线区分、[Raycast Windows](https://www.raycast.com/windows) 的紧凑层级、[SteelSeries Sonar](https://steelseries.com/gg/sonar) 的信息优先级、[Elgato Wave Link](https://www.elgato.com/us/en/s/wave-link-app) 的主次区域。布局、交互、界面代码独立实现，未复用参考软件的图形或代码。

2026-10-24 再核对：公开资料将军用迷彩列为武器进度奖励，具体游戏内前置文字未逐项核实；武器巅峰采用官方简中「黑曜 / 暮曜 / 苔曜 / 荧曜」。官方简中与英文对 mp-2、mp-3 和三项 DMZ 大师挑战次数不一致，这些项目标记待核对，保留英文参考数值，不用于自动判断完成。


v0.7.1 校正：官方武器资料说明配件可在兼容武器之间共享，已拥有配件对应的升级可能跳过。不能据此把社区等级数字当作所需配件数量，也不能把旧等级计数等同于当前实际解锁。因此 264 项军用奖励暂按游戏实际拥有状态确认，社区门槛仅保留在详情参考字段，不参与前置或百分比判定；黄金、白金、巅峰等已有门槛继续检查。
