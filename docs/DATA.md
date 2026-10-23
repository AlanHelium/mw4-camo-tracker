# 数据来源与范围

整理日期：2026-10-23。应用离线加载 `src/Data/catalog.json`，单项保留资料来源。

- [Call of Duty 官方进度博客](https://www.callofduty.com/ca/en/blog/2026/10/call-of-duty-modern-warfare-4-ready-for-launch-progression-prestige)：路线、巅峰、部分挑战与外观参考。
- [Upblazed tracker](https://upblazed.com/tools/mw4-camo-tracker)：33 把武器的公开名称与条件事实。未复制网站界面、代码或其模拟迷彩图。
- [用户提供的小黑盒解读](https://www.xiaoheihe.cn/app/bbs/link/c94c72acbda3)：辅助核对路线区别，本身不是官方规则。

33 把武器的 462 条军事、特殊与完成主义记录，另附每把 4 项武器巅峰与 10 项全局路线，共 604 条。数据量不等于核验程度：少数条件未公开、部分社区条目与官方描述冲突，大多数特殊迷彩的确切开放等级未知。

- 冲突或缺失的要求不按数值自动判定完成，以游戏内实际解锁勾选为准。
- 开放等级未知的特殊迷彩等待用户确认「游戏中已开放此挑战」。
- 目前仅填入 HAN 86（72）与 Sang 38（38）的普通满级，其他武器不猜测满级。
- 武器巅峰满级、巅峰大师 150 / 250 等门槛分别判断。
- 勾选历史黄金补齐本武器必要前置；不推测其他武器已解锁，不从历史迷彩推断当前转生后的等级。
- 多人推荐与 DMZ 条件参考分开，Warzone 未核实项目保留不确定性。

官方素材地址见 [SOURCES](../src/Assets/SOURCES.md)。部分图片从官方合集裁切，其他武器上的显示只是迷彩样式参考；缺少对应图时使用类别标识。

设计参考 Call of Duty 官方进度页面的路线区分、[Raycast Windows](https://www.raycast.com/windows) 的紧凑层级、[SteelSeries Sonar](https://steelseries.com/gg/sonar) 的信息优先级、[Elgato Wave Link](https://www.elgato.com/us/en/s/wave-link-app) 的主次区域。布局、交互、界面代码独立实现，未复用参考软件的图形或代码。
