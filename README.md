# unity-demo-PinBall

## 脚本说明

### ObstacleGenerator (障碍生成器)
用于在组件表面生成多个障碍物预制体，支持多种类型和详细日志记录。

#### 🎮 运行时控制：
- **自动生成**：游戏开始时（Start方法）自动生成障碍物
- **键盘控制**：按 **T 键** 可重新生成障碍物

#### 使用方法：
1. 将`ObstacleGenerator`脚本附加到具有Renderer或Collider组件的GameObject上
2. 在Inspector中配置障碍物类型和参数
3. 运行游戏时会自动生成，按T键可重新生成

#### 详细功能说明请查看代码注释
