# YGOPro2 NGUI -> UGUI 资源梳理

## 1. 先说结论

- 这个项目现在是 **混合态**：
	- 一部分老 UI / 展示层还是 **NGUI**。
	- 一部分新 UI 已经换成了 **新 prefab + TextMeshPro / modernUI**。
	- 场上大部分“战斗演出特效”本质上是 **3D prefab / 粒子系统 / 世界空间对象**，**不属于 NGUI -> UGUI 的核心迁移对象**。
- 也就是说：
	- **要迁的是 UI 展示层、贴图显示层、文本层、交互层。**
	- **不用优先迁的是召唤光效、攻击线、送墓爆炸、无效闪电这类特效 prefab。**


## 2. 资源绑定来源

`Program` 是资源注册表，资源有两层来源：

1. **旧默认绑定**：`Assets/old/loader.prefab`
2. **场景覆盖绑定**：`Assets/main.unity`

实际运行时以 `Assets/main.unity` 对 `Program` 的覆盖为准；没有覆盖的字段继续沿用 `Assets/old/loader.prefab` 的默认值。

这意味着你做迁移时，不能只看 `Program.cs`，要同时看：

- `Assets/SibylSystem/Program.cs`
- `Assets/old/loader.prefab`
- `Assets/main.unity`


## 3. 战场系统的真实结构

### 3.1 不属于 NGUI 迁移主线的部分

以下内容主要是 **世界空间对象 / 粒子 / prefab 实例化**：

- 卡片本体：`mod_ocgcore_card`
- 攻击线：`mod_ocgcore_bs_atk_line_*`
- 发动 / 无效 / 送墓 / 除外 / 盖放特效：`mod_ocgcore_decoration_*`、`mod_ocgcore_cs_*`
- 通常召唤 / 特殊召唤 / 灵摆 / Link 等效果：`mod_ocgcore_ss_*`
- Overlay 光效：`mod_ocgcore_ol_*`

这些资源的迁移策略通常是：

- **保留 prefab + 保留粒子/模型逻辑**
- 只在必要时重做“屏幕覆盖 UI 层”的部分


### 3.2 真正需要优先迁移的部分

这些才是 NGUI -> UGUI 的主战场：

- `mod_simple_ngui_text`
- `mod_simple_ngui_background_texture`
- `Pro1_superCardShower` / `Pro1_superCardShowerA`
- `new_ocgcore_field` 内部仍然依赖的 `UITexture` / `UILabel`
- `new_ui_handShower` 这类“名字是新的，但脚本还在用 NGUI”的 prefab
- `gameInfo` / `deckManager` / `superButton` / `YGOPro1` 目录下的 NGUI 组件脚本


## 4. 关键代码入口

### 战场与卡片

- `Assets/SibylSystem/Ocgcore/OCGobjects/gameCard.cs`
	- `card_picture_handler()`：把卡图贴到牌模型上
	- `animationEffect()` / `positionEffect()` / `positionShot()`：实例化特效 prefab
	- `animation_show_off()`：发动/召唤时的大图演出

- `Assets/SibylSystem/Ocgcore/Ocgcore.cs`
	- 各个 `GameMessage` 分支里决定何时播放：召唤、发动、无效、送墓、攻击等特效

- `Assets/SibylSystem/Ocgcore/OCGobjects/gameField.cs`
	- 战场场地对象
	- 已经开始混用 `TextMeshPro` 与 `NGUI`：
		- 区域计数：`new_ui_textMesh`
		- 文字 label / phase 贴图：仍有 `UILabel` / `UITexture`


### NGUI UI 入口

- `Assets/SibylSystem/BackGroundPic/BackGroundPic.cs`
	- 使用 `mod_simple_ngui_background_texture`

- `Assets/SibylSystem/MonoHelpers/TextMaster.cs`
	- 使用 `mod_simple_ngui_text`

- `Assets/ArtSystem/YGOPro1/YGO1superShower.cs`
	- `UITexture card`
	- `UITexture closeup`

- `Assets/ArtSystem/Ocgcore/gameField/phaser.cs`
	- phase UI 仍然是 `UILabel`

- `Assets/ArtSystem/picShower/handShower.cs`
	- `new_ui_handShower` 仍然是 `UITexture`


## 5. 核心资源对照表

## 5.1 老 NGUI 资源与建议替代

| 旧字段 | 当前绑定资源 | 主要用途 | 当前代码入口 | UGUI/TMP 迁移建议 |
|---|---|---|---|---|
| `mod_simple_ngui_text` | `Assets/NGUI/mod_ngui_text.prefab` | 世界空间/2D 文本提示 | `gameField.cs`, `TextMaster.cs` | 新建统一的 `TMP_Text` prefab，替代 `UILabel` |
| `mod_simple_ngui_background_texture` | `Assets/NGUI/mod_background_texture.prefab` | 背景大图 | `BackGroundPic.cs` | 改为 `Canvas + RawImage + AspectRatioFitter` |
| `Pro1_superCardShower` | `Assets/ArtSystem/YGOPro1/YGO1_superShower.prefab` | 发动/展示时的大图演出 | `gameCard.animation_show_off()` | 改为 `Canvas + RawImage + Animator/Timeline` |
| `Pro1_superCardShowerA` | `Assets/ArtSystem/YGOPro1/YGO1_superShowerActor.prefab` | 带 closeup 的大图演出 | `gameCard.animation_show_off()` | 同上，拆分为 `card image + closeup image` |
| `mod_ocgcore_number` | `Assets/old/Ocgcore/numbers/mod_ocgcore_number.prefab` | 数字显示 | `gameField.cs`, `Ocgcore.cs` | 若希望统一 UI 栈，替换成 TMP 数字 prefab |
| `new_ui_textMesh` | `Assets/ArtSystem/Ocgcore/gameField/new_ui_textMesh.prefab` | 已迁移的文字显示 | `gameField.cs`, `gameCard.cs` | 继续作为 TMP 方向的标准文字 prefab |


## 5.2 “新 prefab，但内部仍依赖 NGUI”

| 资源 / 模块 | 当前绑定资源 | 现状 | 说明 |
|---|---|---|---|
| `new_ocgcore_field` | `Assets/ArtSystem/Ocgcore/gameField/new_gameField.prefab` | **混合态** | `gameField.cs` 已用 `new_ui_textMesh`，但 `phaser.cs` / `gameField.cs` 仍有 `UILabel` / `UITexture` |
| `new_ui_handShower` | `Assets/modernUI/prefab/modern_handShower.prefab` | **未真正去 NGUI** | `handShower.cs` 仍然使用 `UITexture texture_0/1` |
| `new_ui_faceShower` | `Assets/ArtSystem/picShower/new_ui_faceShower.prefab` | 候选替代 | 可考虑承接 `Pro1_superCardShower*` 的 closeup/展示逻辑，但需要二次改造 |
| `gameInfo` 模块 | `Assets/ArtSystem/gameInfo/*` | 仍重度 NGUI | `barPngLoader.cs`, `gameInfo.cs`, `spriteChanger.cs` 都有 `UITexture` / `UILabel` |
| `deckManager` 模块 | `Assets/ArtSystem/deckManager/*` | 仍重度 NGUI | `cardPicLoader.cs`, `descKeeper.cs`, `forceColor.cs` 等仍有 `UITexture` / `UILabel` |
| `superButton` 模块 | `Assets/ArtSystem/superButton/*` | 仍依赖 NGUI | `iconSetForButton.cs`, `hinter.cs` 仍在用 `UITexture` / `UILabel` |


## 5.3 战场演出特效资源表（通常不需要改成 UGUI）

这些资源建议先 **保持现状**，因为它们本质上不是 NGUI 控件，而是特效 prefab：

| 字段 | 资源路径 | 作用 |
|---|---|---|
| `mod_ocgcore_decoration_thunder` | `Assets/old/UiverseAssests/art_plugin/thunder/ocgcore/mod_ocgcore_lighting.prefab` | 雷电连线 / 附着类特效 |
| `mod_ocgcore_decoration_magic_activated` | `Assets/old/UiverseAssests/art_plugin/RFX_Resources/ocgcore/mod_ocgcore_decoration_magic_activated.prefab` | 魔法发动 |
| `mod_ocgcore_decoration_trap_activated` | `Assets/old/UiverseAssests/art_plugin/ExtremeFXvol1/ocgcore/mod_ocgcore_decoration_trap_activated.prefab` | 陷阱发动 |
| `mod_ocgcore_decoration_removed` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_decoration_removed.prefab` | 除外 |
| `mod_ocgcore_decoration_tograve` | `Assets/old/UiverseAssests/art_plugin/ExtremeFXvol1/ocgcore/mod_ocgcore_decoration_to_grave.prefab` | 送墓 |
| `mod_ocgcore_decoration_card_setted` | `Assets/old/UiverseAssests/art_plugin/FT_MagicEffect_vol3/ocgcore/mod_ocgcore_decoration_card_setted.prefab` | 盖放 |
| `mod_ocgcore_cs_end` | `Assets/old/UiverseAssests/art_plugin/ice_chaining/ocgcore/mod_ocgcore_cs_end.prefab` | 连锁结算结束 |
| `mod_ocgcore_cs_bomb` | `Assets/old/UiverseAssests/art_plugin/ExtremeFXvol1/ocgcore/mod_ocgcore_cs_chain_bomb.prefab` | 连锁爆点 |
| `mod_ocgcore_cs_negated` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_cs_negated.prefab` | 无效 |
| `mod_ocgcore_bs_atk_decoration` | `Assets/old/UiverseAssests/art_plugin/attack_sign/new_attack_decoration.prefab` | 攻击指示 |
| `mod_ocgcore_bs_atk_line_earth` | `Assets/old/UiverseAssests/art_plugin/attack_light_line/ocgcore/mod_ocgcore_atk_earth.prefab` | 地属性攻击线 |
| `mod_ocgcore_bs_atk_line_water` | `Assets/old/UiverseAssests/art_plugin/attack_light_line/ocgcore/mod_ocgcore_atk_water.prefab` | 水属性攻击线 |
| `mod_ocgcore_bs_atk_line_fire` | `Assets/old/UiverseAssests/art_plugin/attack_light_line/ocgcore/mod_ocgcore_atk_fire.prefab` | 火属性攻击线 |
| `mod_ocgcore_bs_atk_line_wind` | `Assets/old/UiverseAssests/art_plugin/attack_light_line/ocgcore/mod_ocgcore_atk_wind.prefab` | 风属性攻击线 |
| `mod_ocgcore_bs_atk_line_dark` | `Assets/old/UiverseAssests/art_plugin/attack_light_line/ocgcore/mod_ocgcore_atk_dark.prefab` | 暗属性攻击线 |
| `mod_ocgcore_bs_atk_line_light` | `Assets/old/UiverseAssests/art_plugin/attack_light_line/ocgcore/mod_ocgcore_atk_light.prefab` | 光属性攻击线 |
| `mod_ocgcore_ss_p_idle_effect` | `Assets/old/UiverseAssests/art_plugin/FT_MagicEffect_vol3/ocgcore/mod_ocgcore_p_eff.prefab` | 灵摆区 idle 特效 |
| `mod_ocgcore_ss_p_sum_effect` | `Assets/old/UiverseAssests/art_plugin/FT_MagicEffect_vol3/ocgcore/mod_ocgcore_psum_eff.prefab` | 灵摆召唤特效 |
| `mod_ocgcore_ss_dark_hole` | `Assets/ArtSystem/darkholl/new_darkholl.prefab` | 黑洞类演出 |
| `mod_ocgcore_ss_link_mark` | `Assets/ArtSystem/darkholl/UnionAssetes/Particle/Space/SG_SpacePackage/mod_ss_link_mark.prefab` | Link 指向标记 |
| `mod_ocgcore_ss_summon_earth` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_earth.prefab` | 地属性召唤 |
| `mod_ocgcore_ss_summon_water` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_water.prefab` | 水属性召唤 |
| `mod_ocgcore_ss_summon_fire` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_fire.prefab` | 火属性召唤 |
| `mod_ocgcore_ss_summon_wind` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_wind.prefab` | 风属性召唤 |
| `mod_ocgcore_ss_summon_dark` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_dark.prefab` | 暗属性召唤 |
| `mod_ocgcore_ss_summon_light` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_light.prefab` | 光属性召唤 |
| `mod_ocgcore_ss_spsummon_normal` | `Assets/old/UiverseAssests/art_plugin/RFX_Resources/ocgcore/mod_ocgcore_ss_summon.prefab` | 通用特殊召唤 |
| `mod_ocgcore_ss_spsummon_ronghe` | `Assets/old/UiverseAssests/art_plugin/RFX_Resources/ocgcore/mod_ocgcore_ss_fusion.prefab` | 融合召唤 |
| `mod_ocgcore_ss_spsummon_tongtiao` | `Assets/old/UiverseAssests/art_plugin/FT_Pulse_volume01/ocgcore/mod_ocgcore_ss_tongtiao.prefab` | 同调召唤 |
| `mod_ocgcore_ss_spsummon_yishi` | `Assets/old/UiverseAssests/art_plugin/ice_chaining/ocgcore/mod_ocgcore_ss_yishi.prefab` | 仪式召唤 |
| `mod_ocgcore_ss_spsummon_link` | `Assets/old/UiverseAssests/art_plugin/MagicEffectsLightningVol01/ocgcore/mod_ocgcore_ss_link.prefab` | Link 召唤 |
| `mod_ocgcore_ol_earth` | `Assets/old/Ocgcore/overlay_light/new_overlay_light_earth.prefab` | 地属性 overlay |
| `mod_ocgcore_ol_water` | `Assets/old/Ocgcore/overlay_light/new_overlay_light_water.prefab` | 水属性 overlay |
| `mod_ocgcore_ol_fire` | `Assets/old/Ocgcore/overlay_light/new_overlay_light_fire.prefab` | 火属性 overlay |
| `mod_ocgcore_ol_wind` | `Assets/old/Ocgcore/overlay_light/new_overlay_light_wind.prefab` | 风属性 overlay |
| `mod_ocgcore_ol_dark` | `Assets/old/Ocgcore/overlay_light/new_overlay_light_dark.prefab` | 暗属性 overlay |
| `mod_ocgcore_ol_light` | `Assets/old/Ocgcore/overlay_light/new_overlay_light_light.prefab` | 光属性 overlay |


## 6. 目录级迁移优先级

按“改动收益 / 风险比”建议这样排：

### P0：先把最基础的 NGUI 文本/贴图替换掉

1. `mod_simple_ngui_text`
2. `mod_simple_ngui_background_texture`
3. `TextMaster.cs`
4. `BackGroundPic.cs`
5. `gameField.cs` 里剩下的 `UILabel` / `UITexture`

目标：先建立可复用的 **UGUI/TMP 基础件**。


### P1：解决战场展示层的混合状态

1. `new_ocgcore_field`
2. `Assets/ArtSystem/Ocgcore/gameField/phaser.cs`
3. `Assets/SibylSystem/Ocgcore/OCGobjects/gameField.cs`
4. `mod_ocgcore_number` 是否统一替换为 TMP 版本

目标：让战场 HUD / phase / 区域计数从 NGUI 脱离。


### P2：重做大图演出 UI

1. `Pro1_superCardShower`
2. `Pro1_superCardShowerA`
3. `Assets/ArtSystem/YGOPro1/YGO1superShower.cs`
4. `gameCard.animation_show_off()`

目标：把 `UITexture` 演出层切到 `Canvas + RawImage + Animator/Timeline`。


### P3：再清理次级 UI 模块

1. `Assets/ArtSystem/gameInfo/*`
2. `Assets/ArtSystem/deckManager/*`
3. `Assets/ArtSystem/superButton/*`
4. `Assets/ArtSystem/picShower/handShower.cs`

目标：统一整个工程的 UI 技术栈。


## 7. 迁移时不要混淆的点

### 7.1 不是所有 `mod_ocgcore_*` 都要换成 UGUI

很多 `mod_ocgcore_*` 是世界空间特效 prefab，不是 `UITexture/UILabel`。这类资源：

- 可以继续保持 `Instantiate(prefab)` 的用法
- 不需要为了“去掉 NGUI”而强行改成 `Canvas` 控件


### 7.2 “new_” 前缀不代表已经完全去掉 NGUI

这个项目里，`new_*` 更多表示“新版资源 / remaster 资源”，**不等于内部一定是 UGUI**。

最典型的例子：

- `new_ocgcore_field`：仍然混着 `UILabel` / `UITexture`
- `new_ui_handShower`：脚本还在用 `UITexture`


### 7.3 你要优先抽象掉的是 `Program` 上的 UI prefab 依赖

建议把 `Program` 中偏 UI 的引用先分成两类：

- **UI 资源**：准备迁到 `Canvas/TMP/UGUI`
- **特效资源**：继续保留 `world-space prefab`

这样后面改代码时，不会把 UI 和特效搅在一起。


## 8. 建议的下一步实现顺序

1. 先做一个新的 **TMP 文本 prefab**，替掉 `mod_simple_ngui_text`
2. 再做一个新的 **背景 RawImage prefab**，替掉 `mod_simple_ngui_background_texture`
3. 修改 `TextMaster.cs`、`BackGroundPic.cs`、`gameField.cs`
4. 给 `new_ocgcore_field` 做第二轮“去 NGUI”
5. 最后再处理 `YGO1superShower`、`handShower`、`gameInfo`、`deckManager`


## 9. 这份文档的使用方式

你后面改一个模块时，建议按下面顺序核对：

1. 看 `Program.cs` 有没有这个资源字段
2. 去 `Assets/main.unity` 看是否有场景覆盖
3. 没有覆盖再去 `Assets/old/loader.prefab` 看默认绑定
4. 查这个资源在代码里是作为：
	 - NGUI UI prefab 使用
	 - TMP / 新 UI prefab 使用
	 - 世界空间特效 prefab 使用
5. 只有第一类，才是 NGUI -> UGUI 的直接迁移目标

