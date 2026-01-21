# unity-demo-PinBall

## 脚本说明

### ObstacleGenerator (障碍生成器)
用于在组件表面生成多个障碍物预制体。

#### 使用方法：
1. 将`ObstacleGenerator.cs`脚本附加到具有Renderer或Collider组件的GameObject上（如地板、平台等）
2. 在Inspector中设置以下参数：
   - **Obstacle Prefab**: 要生成的障碍物预制体
   - **Min Distance**: 障碍物之间的最小距离 (0.1-5)
   - **Density**: 生成密度 (0-1)，值越大生成的障碍物越多
   - **Margin**: 边缘边距，距离边缘多远范围内不生成障碍物
   - **Random Seed**: 随机种子，用于重现相同的生成结果

#### 功能特性：
- 使用泊松盘采样算法确保障碍物分布均匀且不重叠
- 可视化调试：在Scene视图中选择对象时显示生成区域边界
- 支持编辑器中实时预览生成的障碍物位置
- 提供清除功能，可重新生成障碍物

#### 公共方法：
- `GenerateObstacles()`: 生成障碍物
- `ClearObstacles()`: 清除所有生成的障碍物
