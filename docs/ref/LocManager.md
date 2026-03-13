# LocManager

**命名空间：** `MegaCrit.Sts2.Core.Localization`

## 功能

获取游戏当前语言，用于 Mod 多语言支持。

## 关键属性

| 成员 | 类型 | 说明 |
|------|------|------|
| `Instance` | `LocManager?` | 单例，可为 null（初始化前） |
| `Language` | `string` | 当前语言代码，如 `"eng"`、`"zhs"` |

## 语言代码映射

| `Language` 值 | 对应 JSON 文件 |
|---------------|---------------|
| `"eng"` | `en_us.json` |
| `"zhs"` | `zh_cn.json` |

## 使用方式

在 Mod 中读取游戏语言并加载对应本地化文件：

```csharp
string language = LocManager.Instance?.Language ?? "eng";
string langCode = language.ToLowerInvariant() switch
{
    "zhs" or "zh_cn" => "zh_cn",
    _                => "en_us",
};
```

## 本地化文件加载

本地化 JSON 文件打包在 PCK 内，用 Godot 的 `FileAccess` 读取：

```csharp
// 文件放在 res://HeavenMode/localization/{lang}.json
using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
Dictionary<string, string>? table =
    JsonSerializer.Deserialize<Dictionary<string, string>>(file.GetAsText());
```

**PCK 内路径格式：** `res://{ModPckName}/localization/{lang}.json`

## 项目实现

参见 `src/Loc.cs` — 封装了缓存、fallback（当前语言 → en_us → 硬编码默认值）的完整工具类。

## 参考来源

- `sts2_decompile/sts2-RMP-Mods/src/Patches.Treasure.cs`：`GetTreasureLocalizedText` / `GetTreasureLanguageCode` / `GetTreasureLocalizationTable`
