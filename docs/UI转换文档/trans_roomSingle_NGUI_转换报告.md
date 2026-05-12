# NGUI to UGUI 转换分析报告
**目标对象:** `trans_roomSingle`
**生成时间:** `5/9/2026 9:05:00 AM`

---

### 📦 [trans_roomSingle]
- *纯节点或无 NGUI 核心UI组件，映射为普通 `RectTransform`*

    ### 📦 [glass]
    - **检测到 NGUI 组件**: `UITexture`
      - **UGUI 替代方案**: `RawImage`
      - **参数映射**:
        - NGUI 尺寸 (572 x 346) -> UGUI `RectTransform` 的 Width/Height
        - NGUI Depth (-100) -> 转换为 **Hierarchy 面板中的节点上下顺序** (越靠下显示在越上层)
        - Texture: `Default-Checker` -> 赋给 RawImage 的 Texture 属性

    ### 📦 [GameObject]
    - **检测到 NGUI 组件**: `UIPanel`
      - **UGUI 替代方案**: 需手动分析对应逻辑。

        ### 📦 [mainWindow]
        - **检测到 NGUI 组件**: `UISprite`
          - **UGUI 替代方案**: `Image`
          - **参数映射**:
            - NGUI 尺寸 (608 x 396) -> UGUI `RectTransform` 的 Width/Height
            - Sprite Name: `bg` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
            - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)
        - **检测到 NGUI 组件**: `UIDragObject`
          - **UGUI 替代方案**: 需手动分析对应逻辑。

            ### 📦 [Rname_]
            - **检测到 NGUI 组件**: `UILabel`
              - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
              - **参数映射**:
                - 文本内容: "TAG模式"
                - NGUI 尺寸 (538 x 22) -> UGUI `RectTransform` 框大小
                - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

            ### 📦 [exit_]
            - **检测到 NGUI 组件**: `UIButton`
              - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
              - **注意事项**: 
                - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
            - **检测到 NGUI 组件**: `UIPlayAnimation`
              - **UGUI 替代方案**: 需手动分析对应逻辑。
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (20 x 20) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `close` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [line_]
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (576 x 5) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `lineWin` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [start]

                ### 📦 [Texture]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (32 x 32) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `launch` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [start_]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [!lable]
                    - **检测到 NGUI 组件**: `UILabel`
                      - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                      - **参数映射**:
                        - 文本内容: "开始游戏"
                        - NGUI 尺寸 (74 x 22) -> UGUI `RectTransform` 框大小
                        - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

            ### 📦 [observer]

                ### 📦 [Texture]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (32 x 32) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `see` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [observer_]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [!lable]
                    - **检测到 NGUI 组件**: `UILabel`
                      - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                      - **参数映射**:
                        - 文本内容: "到观战者"
                        - NGUI 尺寸 (74 x 22) -> UGUI `RectTransform` 框大小
                        - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

            ### 📦 [duelist]

                ### 📦 [Texture]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (32 x 32) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `duel` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [duelist_]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [!lable]
                    - **检测到 NGUI 组件**: `UILabel`
                      - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                      - **参数映射**:
                        - 文本内容: "到决斗者"
                        - NGUI 尺寸 (74 x 22) -> UGUI `RectTransform` 框大小
                        - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

            ### 📦 [ready]

                ### 📦 [Texture]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (32 x 32) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `code` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [ready_]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [!lable]
                    - **检测到 NGUI 组件**: `UILabel`
                      - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                      - **参数映射**:
                        - 文本内容: "决斗准备"
                        - NGUI 尺寸 (74 x 22) -> UGUI `RectTransform` 框大小
                        - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

            ### 📦 [0]
            - *纯节点或无 NGUI 核心UI组件，映射为普通 `RectTransform`*

                ### 📦 [name]
                - **检测到 NGUI 组件**: `UILabel`
                  - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                  - **参数映射**:
                    - 文本内容: "一二三四五六七八九十一二三四"
                    - NGUI 尺寸 (252 x 18) -> UGUI `RectTransform` 框大小
                    - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

                ### 📦 [prep]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIToggle`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIButtonRotation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [Checkmark]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (10 x 10) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `wwhite` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                    ### 📦 [Background]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (18 x 18) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `kuang` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [facePanel]
                - **检测到 NGUI 组件**: `UIPanel`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [face_]
                    - **检测到 NGUI 组件**: `UITexture`
                      - **UGUI 替代方案**: `RawImage`
                      - **参数映射**:
                        - NGUI 尺寸 (40 x 40) -> UGUI `RectTransform` 的 Width/Height
                        - NGUI Depth (23) -> 转换为 **Hierarchy 面板中的节点上下顺序** (越靠下显示在越上层)
                        - Texture: `18` -> 赋给 RawImage 的 Texture 属性

                ### 📦 [kick]
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (20 x 20) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `close` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [line]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (252 x 34) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `whiteLine` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [1]
            - *纯节点或无 NGUI 核心UI组件，映射为普通 `RectTransform`*

                ### 📦 [name]
                - **检测到 NGUI 组件**: `UILabel`
                  - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                  - **参数映射**:
                    - 文本内容: "一秒一喵机会"
                    - NGUI 尺寸 (108 x 18) -> UGUI `RectTransform` 框大小
                    - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

                ### 📦 [prep]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIToggle`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIButtonRotation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [Checkmark]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (10 x 10) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `wwhite` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                    ### 📦 [Background]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (18 x 18) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `kuang` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [facePanel]
                - **检测到 NGUI 组件**: `UIPanel`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [face_]
                    - **检测到 NGUI 组件**: `UITexture`
                      - **UGUI 替代方案**: `RawImage`
                      - **参数映射**:
                        - NGUI 尺寸 (40 x 40) -> UGUI `RectTransform` 的 Width/Height
                        - NGUI Depth (23) -> 转换为 **Hierarchy 面板中的节点上下顺序** (越靠下显示在越上层)
                        - Texture: `18` -> 赋给 RawImage 的 Texture 属性

                ### 📦 [kick]
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (20 x 20) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `close` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [line]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (108 x 34) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `whiteLine` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [2]
            - *纯节点或无 NGUI 核心UI组件，映射为普通 `RectTransform`*

                ### 📦 [name]
                - **检测到 NGUI 组件**: `UILabel`
                  - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                  - **参数映射**:
                    - 文本内容: "一秒一喵机会"
                    - NGUI 尺寸 (108 x 2) -> UGUI `RectTransform` 框大小
                    - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

                ### 📦 [prep]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIToggle`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIButtonRotation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [Checkmark]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (10 x 10) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `wwhite` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                    ### 📦 [Background]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (18 x 18) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `kuang` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [facePanel]
                - **检测到 NGUI 组件**: `UIPanel`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [face_]
                    - **检测到 NGUI 组件**: `UITexture`
                      - **UGUI 替代方案**: `RawImage`
                      - **参数映射**:
                        - NGUI 尺寸 (40 x 40) -> UGUI `RectTransform` 的 Width/Height
                        - NGUI Depth (23) -> 转换为 **Hierarchy 面板中的节点上下顺序** (越靠下显示在越上层)
                        - Texture: `18` -> 赋给 RawImage 的 Texture 属性

                ### 📦 [kick]
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (20 x 20) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `close` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [line]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (2 x 34) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `whiteLine` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [3]
            - *纯节点或无 NGUI 核心UI组件，映射为普通 `RectTransform`*

                ### 📦 [name]
                - **检测到 NGUI 组件**: `UILabel`
                  - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                  - **参数映射**:
                    - 文本内容: "一秒一喵机会"
                    - NGUI 尺寸 (108 x 2) -> UGUI `RectTransform` 框大小
                    - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。

                ### 📦 [prep]
                - **检测到 NGUI 组件**: `UIWidget`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIToggle`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIButtonRotation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [Checkmark]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (10 x 10) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `wwhite` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                    ### 📦 [Background]
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (18 x 18) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `kuang` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [facePanel]
                - **检测到 NGUI 组件**: `UIPanel`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [face_]
                    - **检测到 NGUI 组件**: `UITexture`
                      - **UGUI 替代方案**: `RawImage`
                      - **参数映射**:
                        - NGUI 尺寸 (40 x 40) -> UGUI `RectTransform` 的 Width/Height
                        - NGUI Depth (23) -> 转换为 **Hierarchy 面板中的节点上下顺序** (越靠下显示在越上层)
                        - Texture: `18` -> 赋给 RawImage 的 Texture 属性

                ### 📦 [kick]
                - **检测到 NGUI 组件**: `UIButton`
                  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                  - **注意事项**: 
                    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                - **检测到 NGUI 组件**: `UIPlayAnimation`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (20 x 20) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `close` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [line]
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (2 x 34) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `whiteLine` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [board (1)]
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (46 x 46) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `board` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [board (2)]
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (46 x 46) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `board` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [und (2)]
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (40 x 40) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `mask` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [und (1)]
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (40 x 40) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `mask` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

            ### 📦 [deck]
            - **检测到 NGUI 组件**: `UISprite`
              - **UGUI 替代方案**: `Image`
              - **参数映射**:
                - NGUI 尺寸 (226 x 310) -> UGUI `RectTransform` 的 Width/Height
                - Sprite Name: `kuang` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                - Image Type: `Sliced` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)
            - **检测到 NGUI 组件**: `UIselectableList`
              - **UGUI 替代方案**: 需手动分析对应逻辑。

                ### 📦 [bar_]
                - **检测到 NGUI 组件**: `UIScrollBar`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。
                - **检测到 NGUI 组件**: `UISprite`
                  - **UGUI 替代方案**: `Image`
                  - **参数映射**:
                    - NGUI 尺寸 (10 x 310) -> UGUI `RectTransform` 的 Width/Height
                    - Sprite Name: `` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                    - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                    ### 📦 [Foreground]
                    - **检测到 NGUI 组件**: `UIButton`
                      - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)
                      - **注意事项**: 
                        - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。
                        - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。
                    - **检测到 NGUI 组件**: `UISprite`
                      - **UGUI 替代方案**: `Image`
                      - **参数映射**:
                        - NGUI 尺寸 (10 x 310) -> UGUI `RectTransform` 的 Width/Height
                        - Sprite Name: `white` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image
                        - Image Type: `Simple` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)

                ### 📦 [panel_]
                - **检测到 NGUI 组件**: `UIPanel`
                  - **UGUI 替代方案**: 需手动分析对应逻辑。

                    ### 📦 [description_]
                    - **检测到 NGUI 组件**: `UILabel`
                      - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`
                      - **参数映射**:
                        - 文本内容: "比赛模式\n禁限卡表:2016.10.1..."
                        - NGUI 尺寸 (218 x 116) -> UGUI `RectTransform` 框大小
                        - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。
                    - **检测到 NGUI 组件**: `UIDragScrollView`
                      - **UGUI 替代方案**: 需手动分析对应逻辑。
