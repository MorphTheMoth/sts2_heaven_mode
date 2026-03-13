# 杀戮尖塔2 Mod 架构解析

> 本文档描述 `RemoveMultiplayerPlayerLimit` mod 的完整构成与构建流程。

参考项目目录：本项目下的ref\sts2-RMP-Mods。

---

## 一、项目概览

| 字段     | 值                                                     |
| -------- | ------------------------------------------------------ |
| Mod 名称 | Remove Multiplayer Player Limit                        |
| 版本     | 0.0.4A                                                 |
| 作者     | Rain_G                                                 |
| 功能     | 将联机大厅玩家上限从 4 人扩展到最多 16 人（默认 8 人） |
| 技术栈   | C# (.NET 9.0) + HarmonyLib + Godot 4.5.1 SDK           |

---

## 二、目录结构

```
sts2-RMP-Mods/
├── src/                            # C# 源代码
│   ├── ModEntry.cs                 # Mod 入口 + 配置加载 + 工具方法
│   ├── Patches.Network.cs          # 网络层 Patch（大厅容量、序列化位宽）
│   ├── Patches.RestSite.cs         # 篝火场景 Patch（多余玩家座位布局）
│   ├── Patches.Merchant.cs         # 商人场景 Patch（玩家网格布局）
│   └── Patches.Treasure.cs         # 宝藏场景 Patch（遗物选择逻辑 + 跳过按钮）
│
├── RemoveMultiplayerPlayerLimit/   # Mod 静态资源（打包进 .pck）
│   ├── mod_image.png               # Mod 封面图
│   └── localization/
│       ├── en_us.json              # 英文本地化
│       └── zh_cn.json             # 简体中文本地化
│
├── tools/
│   ├── build_pck.gd                # Godot GDScript：生成 .pck 资源包
│   ├── build_release.sh            # Linux/macOS 完整构建脚本
│   └── build_release.ps1           # Windows 完整构建脚本
│
├── libs/
│   ├── sts2.dll                    # 游戏程序集（仅引用，不发布）
│   └── 0Harmony.dll                # HarmonyLib 运行时
│
├── build/                          # 构建产物（自动生成）
│   ├── RemoveMultiplayerPlayerLimit/
│   │   ├── RemoveMultiplayerPlayerLimit.dll   # 编译后的 Mod 程序集
│   │   └── RemoveMultiplayerPlayerLimit.pck   # Godot 资源包
│   └── sts2-RMP-<version>.zip      # 最终发布压缩包
│
├── mod_manifest.json               # Mod 元数据（名称、作者、版本等）
├── RemoveMultiplayerPlayerLimit.csproj  # C# 项目文件
└── project.godot                   # Godot 项目文件（用于构建 .pck）
```

---

## 三、核心机制：HarmonyLib 运行时 Patch

本 Mod **不替换游戏文件**，而是在运行时通过 [HarmonyLib](https://github.com/pardeike/Harmony) 动态修改游戏方法的 IL 字节码，属于非侵入式注入。

### 3.1 Mod 入口点

游戏通过 `[ModInitializer("Initialize")]` 特性发现并调用 Mod 的入口方法：

```csharp
// src/ModEntry.cs
[ModInitializer("Initialize")]
public static partial class ModEntry
{
    public static void Initialize()
    {
        TargetPlayerLimit = LoadOrCreatePlayerLimit(); // 读取配置
        new Harmony("cn.remove.multiplayer.playerlimit").PatchAll(); // 应用所有 Patch
    }
}
```

`Harmony.PatchAll()` 会扫描程序集内所有带 `[HarmonyPatch]` 特性的内部类并自动应用。

---

## 四、各源码文件功能详解

### 4.1 `ModEntry.cs` — 入口 & 配置 & 工具

**职责：**

- 读取/生成 `config.json`（`max_player_limit` 字段，范围 4~16，默认 8）
- 计算序列化所需的位宽（`SlotIdBits`、`LobbyListLengthBits`）
- 提供跨文件共享的辅助方法（`TryGetCharacter`、`TryGetHoveredOption`、`IsRemote` 等）

**配置文件位置解析优先级：**

1. Mod 程序集所在目录
2. `<游戏根目录>/mods/RemoveMultiplayerPlayerLimit/`
3. `%AppData%/StS2Mods/RemoveMultiplayerPlayerLimit/`

---

### 4.2 `Patches.Network.cs` — 网络层

解决的核心问题：游戏网络层用**固定位数**编码玩家槽位 ID 和大厅列表长度，4 人上限只需 2 位（`VanillaSlotIdBits = 2`），扩展后需要更多位。

| Patch 类                                              | 目标方法                            | 补丁类型   | 作用                       |
| ----------------------------------------------------- | ----------------------------------- | ---------- | -------------------------- |
| `StartENetHostPatch`                                  | `NetHostGameService.StartENetHost`  | Prefix     | 提升 ENet 最大客户端数     |
| `StartSteamHostPatch`                                 | `NetHostGameService.StartSteamHost` | Prefix     | 提升 Steam 大厅最大人数    |
| `StartRunLobbyConstructorPatch`                       | `StartRunLobby..ctor`               | Postfix    | 反射修改 `MaxPlayers` 字段 |
| `LobbyPlayerSerializePatch`                           | `LobbyPlayer.Serialize`             | Transpiler | 替换序列化位宽常量 2→N     |
| `LobbyPlayerDeserializePatch`                         | `LobbyPlayer.Deserialize`           | Transpiler | 替换反序列化位宽常量 2→N   |
| `ClientLobbyJoinResponse{Serialize/Deserialize}Patch` | 对应方法                            | Transpiler | 替换大厅列表位宽 3→N       |
| `LobbyBeginRun{Serialize/Deserialize}Patch`           | 对应方法                            | Transpiler | 同上                       |

**Transpiler 原理：** `ReplaceBitWidthBeforeCall()` 扫描 IL 指令列表，找到紧邻目标 `call` 指令前的整数常量加载指令（`ldc.i4.*`），将其操作数从原始位宽替换为目标位宽。

---

### 4.3 `Patches.RestSite.cs` — 篝火场景

原版篝火场景只有 4 个角色容器（座位）。超过 4 人时自动生成额外容器并计算位置。

| Patch 类                         | 目标方法                               | 补丁类型   | 作用                                     |
| -------------------------------- | -------------------------------------- | ---------- | ---------------------------------------- |
| `NRestSiteRoomReadyPatch`        | `NRestSiteRoom._Ready`                 | Transpiler | 拦截容器索引访问，按需动态创建新座位节点 |
| `NRestSiteRoomHoverPatch`        | `OnPlayerChangedHoveredRestSiteOption` | Prefix     | 修复悬停选项显示（支持超过 4 名玩家）    |
| `NRestSiteRoomBeforeSelectPatch` | `OnBeforePlayerSelectedRestSiteOption` | Prefix     | 修复选择前动画                           |
| `NRestSiteRoomAfterSelectPatch`  | `OnAfterPlayerSelectedRestSiteOption`  | Prefix     | 修复选择后动画                           |

**座位位置算法：**

- 第 5、6 个玩家放在原 4 座的左右前方延伸位置
- 第 7、8 个玩家放在左右后方延伸位置
- 更多玩家沿对角线方向递进偏移（`ExtraSeatStep = (70, -45)`）
- 同时复制背景篝火木头节点（`RestSiteLLog`/`RestSiteRLog`）并横向偏移，避免视觉穿模

---

### 4.4 `Patches.Merchant.cs` — 商人场景

超过 4 名玩家时，重新计算所有角色的二维网格坐标，防止重叠堆叠。

**布局算法：**

- 人数 ≤ 8：分 2 行
- 人数 > 8：行数 = `ceil(人数 / 4)`
- 每行从右向左排列，同时向前（Y+）错开，产生纵深感

---

### 4.5 `Patches.Treasure.cs` — 宝藏场景

功能最复杂的模块，解决两个问题：

**问题 1：遗物槽位不足**

- `NTreasureRoomRelicCollectionInitializePatch` (Prefix)：清理并重建遗物 Holder 节点，超出原版 4 个时动态复制模板节点
- `NTreasureRoomRelicCollectionInitializePatch` (Postfix)：重新计算所有 Holder 的二维网格布局（居中对齐、自适应行列数）
- `NTreasureRoomRelicCollectionDefaultFocusPatch`：将默认焦点指向本地玩家对应的槽位

**问题 2：缺少"跳过"按钮（原版 4 人可跳过，多人模式逻辑需重写）**

- `NTreasureRoomRelicCollectionReadyPatch`：在场景 `_Ready` 时注入"跳过"按钮节点
- 完整重写了 `TreasureRoomRelicSynchronizer.OnPicked` 的投票聚合逻辑：
  - 支持 `index == -1` 表示跳过投票
  - 所有玩家投票后计算结果：无人竞争→直接给予，多人竞争→随机决斗，弃权者获安慰奖
  - 跳过票用字节 `255` 在网络上传输（规避原版协议限制）
- 本地化支持（`en_us`/`zh_cn` 的 `TREASURE_RELIC_SKIP_BUTTON` 键）

---

## 五、构建流程

### 5.1 两阶段构建

```
阶段 1：dotnet build
  RemoveMultiplayerPlayerLimit.csproj
  → 引用 libs/sts2.dll + libs/0Harmony.dll
  → 输出 .godot/mono/temp/bin/Debug/RemoveMultiplayerPlayerLimit.dll

阶段 2：Godot 无头模式执行 tools/build_pck.gd
  → 打包 mod_manifest.json
  → 打包 RemoveMultiplayerPlayerLimit/localization/*.json
  → 打包 RemoveMultiplayerPlayerLimit/mod_image.png（含 .import 元数据）
  → 输出 build/RemoveMultiplayerPlayerLimit.pck
```

### 5.2 打包发布

构建脚本（`build_release.sh` / `build_release.ps1`）将两个产物合并：

```
build/RemoveMultiplayerPlayerLimit/
├── RemoveMultiplayerPlayerLimit.dll   ← dotnet build 产物
└── RemoveMultiplayerPlayerLimit.pck   ← Godot PCKPacker 产物

→ 压缩为 build/sts2-RMP-<version>.zip
```

### 5.3 用户安装

解压后将 `RemoveMultiplayerPlayerLimit/` 文件夹放入游戏的 `mods/` 目录，游戏启动时自动加载。

---

## 六、关键依赖

| 依赖                                    | 用途                                                                 |
| --------------------------------------- | -------------------------------------------------------------------- |
| `libs/sts2.dll`                         | 游戏程序集，提供所有要 Patch 的类型定义（仅编译引用，不随 Mod 发布） |
| `libs/0Harmony.dll`                     | HarmonyLib，提供运行时 IL Patch 能力（随 Mod 发布）                  |
| `Godot.NET.Sdk/4.5.1`                   | 提供 `Vector2`、`Node`、`Control` 等 Godot 引擎类型                  |
| `Godot_v4.5.1-stable_win64_console.exe` | 无头模式执行 GDScript 构建 .pck                                      |

---

## 七、Mod 加载机制总结

```
游戏启动
  └─ 扫描 mods/ 目录
       └─ 读取 RemoveMultiplayerPlayerLimit.pck 中的 mod_manifest.json
            └─ 加载 RemoveMultiplayerPlayerLimit.dll
                 └─ 发现 [ModInitializer("Initialize")] → 调用 ModEntry.Initialize()
                      ├─ 读取/生成 config.json
                      ├─ 计算目标位宽参数
                      └─ Harmony.PatchAll() → 注入所有 [HarmonyPatch] 类
```
