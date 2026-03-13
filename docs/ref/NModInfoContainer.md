# NModInfoContainer

## Mod 封面图加载

- 类：`MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModInfoContainer`
- 方法：`Fill(Mod mod)`
- 作用：在 Mod 菜单详情面板中填充标题、封面图和描述。

关键实现：

```csharp
_image.Texture = PreloadManager.Cache.GetAsset<Texture2D>($"res://{mod.pckName}/mod_image.png");
```

结论：

- 游戏会按固定路径读取封面图：`res://<mod_manifest.pck_name>/mod_image.png`
- 因此 mod 打包时需要把图片放进与 `pck_name` 同名目录下
- 为了让 Godot 在运行时稳定加载，需要把 `mod_image.png` 的 `.import` / `.ctex` 导入产物一起打进 `.pck`
- 参考 `ref\sts2-RMP-Mods\tools\build_pck.gd` 的做法：打包前先确保图片已被 Godot 导入，再把 `res://.godot/imported/mod_image.png-*.ctex` 和 `res://<pck_name>/mod_image.png.import` 一并写入 `.pck`
- 当前 `HeavenMode` 的构建脚本需要先执行 Godot `--import`，否则 `.godot/imported` 中不会出现 `mod_image.png-*.ctex`，游戏内会报 `No loader found for resource`

相关类：

- `MegaCrit.Sts2.Core.Modding.ModManager.TryLoadModFromPck`
  - 读取 `res://mod_manifest.json`
  - 校验 `mod_manifest.pck_name` 必须与 `.pck` 文件名一致
