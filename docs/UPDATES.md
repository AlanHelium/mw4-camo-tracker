# 资料更新

从 v0.7.0 起，主界面或小窗的「··· → 资料更新」提供「检查更新」和「导入离线包」。正常启动、游戏小窗及空闲时不请求网络；点击检查才连接本项目的公开 GitHub 资料源。无需账号，不上传本地进度。

## 玩家使用

1. 点击「检查更新」。有新版时下载并校验，再显示新增武器、挑战与条件调整的数量。
2. 点击「安装资料」。新资料立即用于主界面、小窗、清单和统计，无需重新下载整个 APP。
3. GitHub 无法连接时，在 Releases 下载作者发布的 `catalog-pack.zip`，选择「导入离线包」，查看变更后安装。程序压缩包和个人存档 JSON 不能作为资料包导入。

已有解锁、相同计数口径下的局后总进度、当前目标、武器等级与巅峰状态、今日计划及个人设置会保留。用户自定义武器、任务及手动改写的条件也保留；作者修正不会覆盖手动改写的任务。

任务改版导致计数含义变化时，维护者提升该项 `ProgressRevision`：已解锁继续保留，未完成项目的旧计数改为未知，等待用户按游戏重新补录。

安装前完整存档保存在 `%LOCALAPPDATA%\CamoTracker\progress.json.before-data-日期时间`。安装成功后清空旧撤销历史，避免撤销把资料恢复成旧版。写入失败自动恢复；中途退出则下次启动恢复更新前存档。离线资料保存在 `%LOCALAPPDATA%\CamoTracker\catalogs`，与程序文件分离。

资料包包含武器及中英文名称、PNG/JPEG 枪图与迷彩参考图、挑战文本及中英文条件、目标次数、开放等级、武器巅峰要求和前置关系。支持自身/同类别/全武器的指定挑战或迷彩类别要求；全局数量门槛可独立调整。加入新枪不会擅自把官方固定的 33 把门槛改成武器总数。

新交互、全新规则运算、3D 模型、动态材质或程序功能仍需升级 APP；不支持的资料结构或最低程序版本会拒绝安装并提示升级。资料更新不会自动读取游戏账号，也不会凭空补齐尚未核实的图片或条件。

## 维护者发布

正式资料源是仓库中的 `data/catalog.json`，保存完整目录、稳定标识、双语内容、前置和图片映射；程序发行时嵌入同一文件作为离线基线。`src/Data` 保留最初采集记录，旧采集脚本只作辅助，不直接覆盖正式更新源。

1. 先用官方公告和游戏实际条件核实，社区资料作补充；在每条 `Source` / `SourceRule` 及图片 `Source` 留来源，未知项保留不确定标记。
2. 编辑 `data/catalog.json`。新增武器使用新的 `CatalogKey`，已有武器和任务即使改名也保持原 key；同一武器内不能复用旧任务 key 表示不同任务。维护 `ChineseName` / `EnglishName` 和挑战 `Unit`（人、组、次等）。提升 `Revision`（`YYYYMMDDNN`）、更新 `Updated` 和中文 `Notes`。任务改变计数口径时另提升 `ProgressRevision`。
3. 新枪图放在 `src/Assets/Weapons`，迷彩图放在 `src/Assets`，接受 PNG/JPEG 小写文件名；登记在 `WeaponImages` / `CamoImages`。缺图可暂不登记，界面不借用别的枪图。
4. `CamoImages.Crop` 为空表示整图，四个数表示 `[x,y,width,height]`，坐标以原图等比缩放到宽 1920 为基准。`Caption` 写清示例枪型，勿把样式参考称为其他武器的实际涂装。
5. 运行 `python scripts/package-data.py`，生成完整 ZIP、SHA256 和 `data/channel.json`。脚本只用标准库，输出可重复，不能包含玩家进度。
6. 运行 `scripts/build.ps1`：发布、迁移/故障恢复测试、完整包与图片校验。将 `data/catalog.json`、`data/catalog-pack.zip`、`data/channel.json` 及新增图片在同一提交发布。channel 的 `Blob` 为 ZIP 的 Git blob SHA，由脚本计算。
7. main 生效后，运行 `CamoTracker.exe --verify-feed` 验证公开清单、下载 SHA256、解压及图片。将同一个 `catalog-pack.zip` 附在 GitHub Release 供离线导入。纯资料更新可单独发布 data 标签，无需重新发行程序。

界面只下载核实后发布的资料包，不直接爬网站后覆盖用户数据。清单固定取自 `AlanHelium/mw4-camo-tracker`，ZIP 经不可变 Git blob API 下载，与清单 SHA256 和大小比对；这是完整性校验，不宣称离线 ZIP 有数字签名。

schema 1 限制：ZIP 24 MB、解压总量 64 MB、单文件 8 MB、单图 4096 × 4096，文件名和类型白名单；拒绝重复文件、越界路径、缺图、未知规则种类和不兼容版本。合并后重新验证存档容量和范围，超过上限时需升级程序。
