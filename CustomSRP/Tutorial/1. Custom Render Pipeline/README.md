# **Custom Render Pipeline**

- Create a render pipeline asset and instance.
- Render a camera's view.
- Perform culling, filtering, and sorting.
- Separate opaque, transparent, and invalid passes.
- Work with more than one camera.

## 1. A new Render Pipeline

当进行渲染的时候，Unity需要决定把它画成什么形状，以及画在哪里、什么时候画、用什么样的设定去画等等。它的复杂程度取决于涉及到多少的效果。灯光、阴影、透明度、图像效应（后处理）、体积效应等等。所有的效果都需要按照正确的顺序叠加到最后的图像上，这就是我们说的渲染管线所做的事情。

在以前，Unity只支持一些内置的方式来渲染物体。Unity2018引入了脚本化的渲染管线scriptable render pipelines(简称RPS)，让我们可以做任何我们想做的事情，同时仍然能够依靠Unity来执行基本的步骤，比如剔除。Unity2018年还增加了两个实验性的RPs来支持这个特性：轻量级RP和高清晰度RP。在Unity2019，轻量级RP不再是实验性的，并在Unity2019.3被重新命名为Universal RP。

Universal RP注定要取代当前遗留的RP作为默认的渲染管线。之所以这么说，是因为这是一个适合大多数的RP，也相当容易定制。除了自定义RP之外，这个系列还将从零开始创建一个完整的RP。

这个教程会使用最基础的Unlit的前向渲染来画一个基础形状，用来做RP演示的基础。完成之后，会在后面的教程里拓展光照、阴影、不同的渲染方法以及更多的高级特性。

### 1.1 Project Setup

在Unity 2019.2.6或更高版本中创建新的3D项目。因为我们将创建自己的管线，因此不要选择任意的RP项目模板。打开项目后，你可以转到package manager并删除所有不需要的package 。在本教程中，将仅使用Unity UI包来绘制UI，因此可以保留该UI。

该示例会在linear 色彩空间中工作，但Unity 2019.2仍将gamma空间用作默认值。通过“Edit / Project Settings ”进入Player设置，然后选择“Player”，然后将“Other Settings”部分下的“Color Space”切换为“Linear”。

![](color-space.png)

使用标准的， standard, unlit opaque 和transparent 的材质进行混合，然后用一些对象填充默认场景。因为“Unlit/Transparent”着色器仅适用于纹理，因此这里看到的是该球体的UV贴图。

<img src="sphere-alpha-map.png" style="background:black" width=600>

<p align=center><font color=#B8B8B8 ><i>UV sphere alpha map, on black background.</i></p>

我在测试场景中放了几个立方体，所有这些都是不透明的。红色的使用Standard 着色器的材质，绿色和黄色的使用Unlit/Color着色器的材质。蓝色球体使用Standard 着色器，Rendering Mode 设置为Transparent，而白色球体使用Unlit/Transparent着色器。