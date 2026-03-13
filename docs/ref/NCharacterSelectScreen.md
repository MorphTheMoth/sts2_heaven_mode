# NCharacterSelectScreen

**命名空间：** `MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect`

## 功能

角色选择界面（主菜单 → 创建游戏），供 Mod 注入自定义 UI 组件。

## 关键节点路径

| 节点路径 | 类型 | 说明 |
|---------|------|------|
| `%ActDropdown` | `NActDropdown` | 关卡选择下拉框（random / overgrowth / underdocks） |
| `%AscensionPanel` | `NAscensionPanel` | 难度选择面板 |
| `ActLabel` | `MegaRichTextLabel` | Act 下拉框的静态标题标签 |
| `BackButton` | `NBackButton` | 返回按钮 |
| `ConfirmButton` | `NConfirmButton` | 出发按钮 |

## 注入 UI 的方式

通过 Harmony Postfix 在 `_Ready()` 后注入：

```csharp
[HarmonyPatch(typeof(NCharacterSelectScreen))]
internal static class Patches_CharacterSelect
{
    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    private static void AfterReady(NCharacterSelectScreen __instance) { ... }
}
```

## NActDropdown 复制方法

`%ActDropdown`（`NActDropdown`）可通过 `Duplicate()` 复制，得到完整保留子节点结构的新实例。
复制后需 `CallDeferred` 等待所有子节点 `_Ready()` 执行完毕，再修改 `NDropdownItem.Text`：

```csharp
var template = ((Node)screen).GetNode<NActDropdown>("%ActDropdown");
if (template.Duplicate() is Control heavenDropdown)
{
    ((Node)screen).AddChild(heavenDropdown);
    Callable.From(() => FixupItems(heavenDropdown)).CallDeferred();
}

private static void FixupItems(Control dropdown)
{
    var vbox = ((Node)dropdown).GetNodeOrNull<Control>("DropdownContainer/VBoxContainer");
    var items = ((IEnumerable)vbox.GetChildren(false)).OfType<NDropdownItem>().ToList();
    items[0].Text = "Off";   // 覆盖默认 "Random"
    items[1].Text = "1";
    items[2].Text = "2";
    // 重置按钮面上显示的当前选项
    ((Node)dropdown).GetNodeOrNull("%Label")?.Set("text", "Off");
}
```

## NActDropdown 结构

| 子节点 | 类型 | 说明 |
|--------|------|------|
| `%Highlight` | `Control` | 高亮背景 |
| `%Label` | `MegaLabel` | 当前选中项的显示标签 |
| `%DropdownContainer` | `NDropdownContainer` | 弹出列表容器 |
| `DropdownContainer/VBoxContainer` | `VBoxContainer` | 包含各 `NDropdownItem` |
| `%Dismisser` | `NButton` | 点击空白区域关闭下拉框的覆盖层 |

`NDropdownItem` 属性：
- `Text`（get/set）— 读写选项文本（内部调用 `MegaLabel.SetTextAutoSize`）
- 发出信号 `NDropdownItem.SignalName.Selected`，参数为自身

## 项目实现

参见 `src/Patches.CharacterSelect.cs`

## 参考来源

- `sts2_decompile/sts2/MegaCrit/sts2/Core/Nodes/Screens/CharacterSelect/NCharacterSelectScreen.cs`
- `sts2_decompile/sts2/MegaCrit/sts2/Core/Nodes/Screens/CharacterSelect/NActDropdown.cs`
- `sts2_decompile/sts2/MegaCrit/sts2/Core/Nodes/GodotExtensions/NDropdown.cs`
- `sts2_decompile/sts2/MegaCrit/sts2/Core/Nodes/CommonUi/NDropdownItem.cs`

## HeavenMode note

Current usage in this repo:

- Inject a duplicated `NActDropdown` as the Heaven selector in `_Ready()`.
- Add a custom description panel positioned to the right of `%AscensionPanel`.
- Hide the description panel when the player selects `Off`.
- Show the description panel and update its text when the player selects Heaven option `1` or `2`.
- Heaven option `2` description should include Heaven option `1` effects as part of the displayed text.

Useful implementation details:

- Reuse the official `%AscensionPanel/HBoxContainer/AscensionDescription` panel by duplicating it.
- Reuse the official `Description` child (`MegaRichTextLabel`) so title formatting matches vanilla.
- The panel is attached directly to `NCharacterSelectScreen` and positioned relative to `%AscensionPanel`.
- Description text is updated from the `NDropdownItem.SignalName.Selected` callback.
