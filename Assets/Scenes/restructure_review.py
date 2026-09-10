from pathlib import Path
from docx import Document
from docx.shared import Pt, RGBColor
from docx.oxml.ns import qn

src = Path(r"C:\Users\13560\Desktop\琉璃愈心_药师七佛盲盒交互装置设计文献综述_经典文献重写版.docx")
out = Path(r"D:\My project (2)\Assets\Scenes\output\琉璃愈心_药师七佛盲盒交互装置设计文献综述_四段式结构修订版.docx")
doc = Document(src)

def set_text(p, text, style=None):
    p.text = text
    if style:
        p.style = style

def find(text):
    for p in doc.paragraphs:
        if p.text.strip() == text:
            return p
    raise ValueError(text)

def insert_before(anchor, text, style):
    p = anchor.insert_paragraph_before(text)
    p.style = style
    return p

# Four-part review framework.
set_text(find("研究对象与综述路径"), "1 背景引入", "Heading 1")
set_text(find("1 药师佛文化及疗愈思想研究现状"), "2 前人成果梳理", "Heading 1")
first_sub = find("1.1 经典文本与七佛体系")
insert_before(first_sub, "2.1 国内研究成果", "Heading 2")
set_text(first_sub, "2.1.1 药师佛经典与疗愈思想研究", "Heading 3")
set_text(find("1.2 疗愈思想的结构"), "2.1.2 药师佛疗愈思想的结构阐释", "Heading 3")
set_text(find("1.3 信仰史研究的启示"), "2.1.3 药师信仰传播研究", "Heading 3")
set_text(find("2 西夏药师佛艺术视觉元素研究现状"), "2.1.4 西夏药师佛艺术研究", "Heading 3")
set_text(find("2.1 从数量判断转向图像制度研究"), "西夏药师图像的类型与空间配置", "Heading 4")
set_text(find("2.2 可转译的视觉语法"), "西夏药师图像的视觉语言", "Heading 4")
set_text(find("3 传统文化数字化展示与交互设计研究现状"), "2.1.5 国内传统文化数字化展示研究", "Heading 3")
set_text(find("3.1 从保存复原到参与式阐释"), "数字保存与参与式阐释", "Heading 4")
set_text(find("3.2 本设计的交互语义"), "国内研究对交互转译的启示", "Heading 4")

# Make the domestic digital-heritage paragraph genuinely domestic; foreign work is discussed below.
p = find("数字文化遗产研究已形成从高清采集、三维重建、数据库保存，到虚拟漫游、增强现实和参与式叙事的连续谱系。Carrozzino和Bergamasco指出，虚拟博物馆只有超越静态模型展示，才能形成真正的文化体验；Bekele等人的综述则表明，增强、虚拟和混合现实被广泛用于教育、可视化、重建和遗址体验。中国的“数字敦煌”“数字藏经洞”进一步说明，游戏引擎、动态光照和交互叙事能够突破洞窟开放、时空和承载量限制。")
set_text(p, "国内传统文化数字化研究已从高清采集、数据库保存和三维复原，逐步扩展到虚拟漫游、游戏化叙事和参与式体验。“数字敦煌”以洞窟图像采集和全景展示建立数字保存基础，“数字藏经洞”进一步运用游戏引擎、动态光照和情节交互复现历史场景。这类实践证明，数字技术能够缓解洞窟开放、参观距离和文物承载量的限制，也使观众从观看图像转向探索场景。现有国内实践仍较重视复原精度与传播规模，对宗教图像的精神内涵如何进入具体操作规则讨论不足。")

foreign = find("4 情感疗愈与沉浸式体验设计研究现状")
set_text(foreign, "2.2 国外研究成果", "Heading 2")
insert_before(find("4.1 情绪命名与自主选择"), "2.2.1 数字文化遗产与沉浸展示研究", "Heading 3")
insert_before(find("4.1 情绪命名与自主选择"), "国外数字文化遗产研究较早关注虚拟博物馆如何从三维模型陈列转向体验设计。Carrozzino和Bergamasco认为，沉浸式系统只有把空间、叙事与观众行为联系起来，才能超越静态展示。Bekele等人对增强现实、虚拟现实和混合现实的综述表明，相关技术已广泛用于文化遗产教育、可视化、复原与遗址体验。Mu等人对敦煌第61窟五台山图的虚拟重构进一步显示，符合原作风格的三维转化与情节引导有助于观众理解复杂壁画。这些成果为本研究提供了体验设计方法，但研究重点仍主要是空间进入和知识理解，对宗教疗愈精神的情绪化转译涉及较少。", "Normal")
set_text(find("4.1 情绪命名与自主选择"), "2.2.2 情绪命名与自主选择研究", "Heading 3")
set_text(find("4.2 身体动作、多模态反馈与临场感"), "2.2.3 身体动作与沉浸体验研究", "Heading 3")
set_text(find("4.3 疗愈边界与验证方法"), "2.2.4 情感疗愈的边界与评价研究", "Heading 3")

section3 = find("5 现有研究不足与本设计切入点")
set_text(section3, "3 研究评述与研究缺口", "Heading 1")
contrib = find("5.1 现有研究的四重断裂")
set_text(contrib, "3.1 现有研究的主要成果", "Heading 2")
old_gap = contrib._p.getnext()
old_gap_p = next(p for p in doc.paragraphs if p._p is old_gap)
set_text(old_gap_p, "国内外研究已经形成四方面基础。佛教经典和信仰史研究厘清了药师七佛的名号、本愿及现实救苦取向；敦煌与西夏美术研究梳理了药师经变的构图、造像和仪式元素；数字文化遗产研究积累了三维复原、虚拟展示和交互叙事方法；情绪调节与沉浸体验研究则解释了情绪命名、自主选择、多感官反馈和临场感的作用。这些成果为本设计的文本依据、视觉设计、交互方式和评价方法提供了相对完整的知识来源。")

gap_anchor = find("5.2 琉璃愈心的设计切入")
insert_before(gap_anchor, "3.2 现有研究的不足", "Heading 2")
insert_before(gap_anchor, "现有成果的不足主要表现为研究领域之间缺少衔接。经典与艺术史研究擅长解释本愿、信仰和图像，却很少讨论这些内容如何转化为当代观众可以操作的体验；数字文化遗产研究强调保存、复原和沉浸展示，对宗教精神如何进入交互规则讨论不足；情绪体验研究常采用通用的色彩、声音与心理调节方法，容易脱离具体文化语境；现有数字佛教艺术项目也较少同时检验文本准确性、文化理解、情绪感受和行动变化。", "Normal")
set_text(gap_anchor, "3.3 本研究需要回应的核心缺口", "Heading 2")

conclusion = find("结论")
set_text(conclusion, "4 小结与本研究的提出", "Heading 1")
first_conclusion = conclusion._p.getnext()
first_conclusion_p = next(p for p in doc.paragraphs if p._p is first_conclusion)
set_text(first_conclusion_p, "综合国内外成果可以看出，药师佛经典、敦煌和西夏药师图像、数字文化遗产以及情绪体验均已拥有较丰富的研究积累，但它们尚未形成一套面向当代观众的连续转译方法。本研究由此提出“琉璃愈心”药师七佛盲盒交互装置：以义净译《药师琉璃光七佛本愿功德经》建立七佛语义基础，以西夏及敦煌药师图像提炼造型、色彩和光效，通过“识别情绪、选择球体、亲手打开、获得多模态回应、形成积极行动”的体验流程，将经典中的慈悲救苦、无忧吉祥和身心安乐转化为可参与、可理解并可评价的现代文化体验。")

# Heading styles created by Word may carry theme blue; normalize all heading levels to black.
for name, size in [("Heading 1", 16), ("Heading 2", 13), ("Heading 3", 11.5), ("Heading 4", 11)]:
    st = doc.styles[name]
    st.font.name = "微软雅黑"
    st._element.rPr.rFonts.set(qn("w:eastAsia"), "微软雅黑")
    st.font.size = Pt(size)
    st.font.bold = True
    st.font.color.rgb = RGBColor(0, 0, 0)
    st.paragraph_format.keep_with_next = True

doc.core_properties.title = "基于药师佛疗愈精神的数字交互装置设计研究综述"
doc.core_properties.subject = "按背景引入 前人成果 研究评述 小结引出本研究的结构修订"
doc.save(out)
print(out)
