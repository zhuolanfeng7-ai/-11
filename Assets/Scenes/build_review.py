from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn


ROOT = Path(r"D:\My project (2)\Assets\Scenes")
TMP = ROOT / "tmp" / "literature_review"
OUT = ROOT / "output"
PDF_RENDER = ROOT / "tmp" / "pdf_review"
TMP.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
DOCX_PATH = OUT / "琉璃愈心_药师七佛盲盒交互装置设计文献综述.docx"


def font_path(name):
    candidates = [
        Path(r"C:\Windows\Fonts") / name,
        Path(r"C:\Windows\Fonts\simhei.ttf"),
        Path(r"C:\Windows\Fonts\msyh.ttc"),
    ]
    return next(p for p in candidates if p.exists())


FONT_REG = str(font_path("msyh.ttc"))
FONT_BOLD = str(font_path("msyhbd.ttc"))


def crop(src, box, dst):
    im = Image.open(src).convert("RGB")
    im.crop(box).save(dst, quality=94)


# Figures reproduced from the supplied thesis pages. Crops retain only the cited image area.
fig1 = TMP / "fig1_cave220_full.jpg"
fig2 = TMP / "fig2_local_details.jpg"
fig3 = TMP / "fig3_western_xia.jpg"
crop(PDF_RENDER / "page-11.jpg", (150, 35, 770, 515), fig1)
crop(PDF_RENDER / "page-21.jpg", (230, 65, 705, 430), fig3)

# Recompose the two local details horizontally so they remain on one Word page.
page12 = Image.open(PDF_RENDER / "page-12.jpg").convert("RGB")
detail_a = page12.crop((360, 75, 655, 430))
detail_b = page12.crop((350, 500, 670, 875))
detail_a = detail_a.resize((382, 460), Image.Resampling.LANCZOS)
detail_b = detail_b.resize((392, 460), Image.Resampling.LANCZOS)
canvas = Image.new("RGB", (900, 520), "white")
canvas.paste(detail_a, (45, 30))
canvas.paste(detail_b, (463, 30))
canvas.save(fig2, quality=94)


def rounded(draw, xy, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def make_flow(path):
    W, H = 1800, 620
    im = Image.new("RGB", (W, H), "#F7F4EC")
    d = ImageDraw.Draw(im)
    f_title = ImageFont.truetype(FONT_BOLD, 48)
    f_h = ImageFont.truetype(FONT_BOLD, 34)
    f_b = ImageFont.truetype(FONT_REG, 26)
    d.text((90, 55), "琉璃愈心的体验转译链", font=f_title, fill="#223B39")
    cards = [
        ("识别", "选择情绪球\n承认当下状态", "#DDE9E4"),
        ("外化", "亲手打开球体\n把情绪转为行动", "#E8DFC8"),
        ("回应", "佛像 声音 粒子\n与疗愈语句同步出现", "#D9DCEA"),
        ("转化", "获得可执行提示\n形成积极行动意向", "#E6D6CE"),
    ]
    x0, y0, cw, ch, gap = 90, 190, 350, 250, 75
    for i, (h, b, color) in enumerate(cards):
        x = x0 + i * (cw + gap)
        rounded(d, (x, y0, x + cw, y0 + ch), 28, color, "#8D948E", 3)
        d.text((x + 28, y0 + 30), f"0{i+1}  {h}", font=f_h, fill="#7A3F34")
        yy = y0 + 105
        for line in b.split("\n"):
            d.text((x + 28, yy), line, font=f_b, fill="#303735")
            yy += 48
        if i < 3:
            ax = x + cw + 14
            ay = y0 + ch // 2
            d.line((ax, ay, ax + 42, ay), fill="#8A6B3F", width=7)
            d.polygon([(ax + 42, ay), (ax + 25, ay - 13), (ax + 25, ay + 13)], fill="#8A6B3F")
    d.text((90, 505), "设计原则  每一步均由观众触发  多模态反馈保持一致  疗愈语句避免医学功效承诺", font=f_b, fill="#5B625E")
    im.save(path)


def make_gap(path):
    W, H = 1800, 850
    im = Image.new("RGB", (W, H), "#F7F4EC")
    d = ImageDraw.Draw(im)
    ft = ImageFont.truetype(FONT_BOLD, 48)
    fh = ImageFont.truetype(FONT_BOLD, 31)
    fb = ImageFont.truetype(FONT_REG, 25)
    d.text((85, 45), "研究基础与设计缺口", font=ft, fill="#223B39")
    cols = [
        ("宗教与图像研究", ["经典与信仰流变", "七佛与眷属配置", "构图 色彩 乐舞"], "#DDE9E4"),
        ("数字遗产研究", ["三维重建与展示", "沉浸和交互界面", "教育与传播应用"], "#D9DCEA"),
        ("情绪体验研究", ["情绪命名", "自主选择与调节", "临场感和多感官反馈"], "#E8DFC8"),
    ]
    x0, y0, cw, ch, gap = 85, 160, 500, 310, 65
    for i, (h, items, color) in enumerate(cols):
        x = x0 + i * (cw + gap)
        rounded(d, (x, y0, x + cw, y0 + ch), 26, color, "#8D948E", 3)
        d.text((x + 30, y0 + 30), h, font=fh, fill="#7A3F34")
        for j, item in enumerate(items):
            d.ellipse((x + 34, y0 + 105 + 58*j, x + 46, y0 + 117 + 58*j), fill="#8A6B3F")
            d.text((x + 62, y0 + 92 + 58*j), item, font=fb, fill="#303735")
    d.line((900, 490, 900, 565), fill="#8A6B3F", width=8)
    d.polygon([(900, 580), (884, 552), (916, 552)], fill="#8A6B3F")
    rounded(d, (260, 590, 1540, 785), 32, "#223B39")
    d.text((315, 625), "尚未充分解决的问题", font=fh, fill="#F4E6C8")
    d.text((315, 690), "如何把药师七佛的文化语义转化为可被选择 可被操作 可被感知的现代情绪体验", font=fb, fill="white")
    d.text((315, 735), "并以用户研究检验文化理解 情绪感受与交互可用性", font=fb, fill="white")
    im.save(path)


flow_img = TMP / "flow.png"
gap_img = TMP / "gap.png"
make_flow(flow_img)
make_gap(gap_img)


doc = Document()
sec = doc.sections[0]
sec.top_margin = Cm(2.45)
sec.bottom_margin = Cm(2.25)
sec.left_margin = Cm(2.65)
sec.right_margin = Cm(2.45)


styles = doc.styles
styles["Normal"].font.name = "宋体"
styles["Normal"]._element.rPr.rFonts.set(qn("w:eastAsia"), "宋体")
styles["Normal"].font.size = Pt(11)
styles["Normal"].paragraph_format.line_spacing = 1.55
styles["Normal"].paragraph_format.space_after = Pt(6)
styles["Normal"].paragraph_format.first_line_indent = Cm(0.74)

title_style = styles["Title"]
title_style.font.name = "微软雅黑"
title_style._element.rPr.rFonts.set(qn("w:eastAsia"), "微软雅黑")
title_style.font.size = Pt(23)
title_style.font.bold = True
title_style.font.color.rgb = RGBColor(34, 59, 57)
title_style.paragraph_format.space_after = Pt(15)
# Remove Word's built-in Title border so whitespace, not a rule, separates the title.
title_ppr = title_style._element.get_or_add_pPr()
title_border = title_ppr.find(qn("w:pBdr"))
if title_border is not None:
    title_ppr.remove(title_border)

for n, size, color in [("Heading 1", 16, RGBColor(122, 63, 52)), ("Heading 2", 13, RGBColor(34, 59, 57))]:
    st = styles[n]
    st.font.name = "微软雅黑"
    st._element.rPr.rFonts.set(qn("w:eastAsia"), "微软雅黑")
    st.font.size = Pt(size)
    st.font.bold = True
    st.font.color.rgb = color
    st.paragraph_format.space_before = Pt(14)
    st.paragraph_format.space_after = Pt(8)
    st.paragraph_format.keep_with_next = True


def set_cell_shading(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = tcPr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tcPr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_text(cell, text, bold=False, color=None, size=10):
    cell.text = ""
    p = cell.paragraphs[0]
    p.paragraph_format.first_line_indent = Cm(0)
    p.paragraph_format.space_after = Pt(2)
    r = p.add_run(text)
    r.bold = bold
    r.font.size = Pt(size)
    r.font.name = "宋体"
    r._element.rPr.rFonts.set(qn("w:eastAsia"), "宋体")
    if color:
        r.font.color.rgb = RGBColor(*color)
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER


def add_body(text, bold_prefix=None):
    p = doc.add_paragraph()
    if bold_prefix and text.startswith(bold_prefix):
        r1 = p.add_run(bold_prefix)
        r1.bold = True
        p.add_run(text[len(bold_prefix):])
    else:
        p.add_run(text)
    return p


def add_figure(path, caption, width_cm=14.8):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.first_line_indent = Cm(0)
    p.paragraph_format.keep_with_next = True
    p.add_run().add_picture(str(path), width=Cm(width_cm))
    c = doc.add_paragraph()
    c.alignment = WD_ALIGN_PARAGRAPH.CENTER
    c.paragraph_format.first_line_indent = Cm(0)
    c.paragraph_format.space_before = Pt(3)
    c.paragraph_format.space_after = Pt(10)
    r = c.add_run(caption)
    r.font.size = Pt(9)
    r.font.color.rgb = RGBColor(90, 90, 90)


def add_page_number(paragraph):
    paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = paragraph.add_run()
    fldChar1 = OxmlElement("w:fldChar")
    fldChar1.set(qn("w:fldCharType"), "begin")
    instrText = OxmlElement("w:instrText")
    instrText.set(qn("xml:space"), "preserve")
    instrText.text = " PAGE "
    fldChar2 = OxmlElement("w:fldChar")
    fldChar2.set(qn("w:fldCharType"), "end")
    run._r.append(fldChar1)
    run._r.append(instrText)
    run._r.append(fldChar2)


add_page_number(sec.footer.paragraphs[0])


p = doc.add_paragraph(style="Title")
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.add_run("基于药师佛疗愈精神的数字交互装置设计研究综述")
p2 = doc.add_paragraph()
p2.alignment = WD_ALIGN_PARAGRAPH.CENTER
p2.paragraph_format.first_line_indent = Cm(0)
r = p2.add_run("以琉璃愈心药师七佛盲盒交互装置设计为对象")
r.font.name = "微软雅黑"
r._element.rPr.rFonts.set(qn("w:eastAsia"), "微软雅黑")
r.font.size = Pt(13)
r.font.color.rgb = RGBColor(105, 83, 55)

box = doc.add_table(rows=1, cols=1)
box.alignment = WD_TABLE_ALIGNMENT.CENTER
box.autofit = False
box.columns[0].width = Cm(15.5)
cell = box.cell(0, 0)
set_cell_shading(cell, "EEF2EF")
set_cell_text(cell, "核心判断  现有研究已经分别解释药师信仰的历史内涵 药师经变画的视觉谱系 数字遗产的展示方法以及沉浸体验的情绪机制 但仍缺少一套把药师七佛的文化语义转化为现代观众可识别 可操作 可回应的完整体验模型", bold=True, color=(34,59,57), size=11)

doc.add_heading("摘要", level=1)
add_body("本综述围绕药师佛文化、敦煌及西夏药师佛艺术、传统文化数字化展示和情感疗愈体验四条研究线索展开。赵竟汐对敦煌莫高窟第220窟北壁东方药师经变画的研究，为七佛图式、构图层级、色彩关系及时代演变提供了直接依据。相关宗教史与图像学研究说明，药师信仰的核心关切包括救苦、除病、消灾、安乐与现实利益，但这些宗教语义不能被直接等同于现代医学或心理治疗。数字遗产研究证明，三维、虚拟现实和混合现实可以扩大文化接触并改善展示方式，同时也提醒设计者处理技术吸引力、内容理解与使用成本之间的关系。情绪调节研究则为情绪命名、自主选择、具身操作和多模态反馈提供了可检验的心理学线索。基于上述研究，本设计提出情绪识别、具身开启、多模态回应与积极行动的体验链，将药师七佛从被观看的宗教图像转化为观众主动参与的文化疗愈体验。")
add_body("关键词  药师七佛  东方药师经变  西夏佛教艺术  数字交互装置  情绪疗愈  沉浸体验")

doc.add_heading("研究范围与文献基础", level=1)
add_body("本文所称疗愈，指借助文化叙事、审美氛围和交互行为形成的情绪识别、安抚与积极行动体验，不指临床诊断、心理治疗或宗教功效验证。这样的边界既保留药师信仰关怀身心疾苦的文化内涵，也避免把设计作品表述为具有未经验证的医疗效果。")
add_body("用户提供的赵竟汐硕士论文是本综述的主要图像学来源。该文以敦煌莫高窟第220窟北壁初唐东方药师经变画为核心，采用文献学、图像学和历史分析方法，并把隋、唐、五代、宋及西夏材料置于同一演变序列。需要说明的是，该文并非西夏药师佛艺术的专题研究，对西夏部分主要通过敦煌莫高窟及五个庙第三窟的个案进行比较。因此，本综述在讨论西夏视觉元素时，将其作为药师经变图式后期程式化与简化的参照，而不将单篇论文扩张为对全部西夏佛教艺术的概括。")

doc.add_heading("1 药师佛文化及疗愈思想研究现状", level=1)
doc.add_heading("1.1 信仰内涵与经典谱系", level=2)
add_body("现有研究首先从经典、信仰来源和历史流变解释药师佛。赵竟汐据经典释义指出，药师佛又称药师琉璃光如来，相关叙述以随病施药、拔除身心之病和光明照暗为核心意象[1]。论文同时梳理了十二大愿、九横死以及东方净琉璃世界等内容，并指出药师信仰约在东晋以后进入中土，在南北朝至隋唐的社会传播中逐渐与治病、延寿、消灾和现实安乐相联系。张悦、韩宏彬从南北朝和隋唐的流变讨论了药师信仰在中国社会中的传播与调适[2]；党燕妮则从中古敦煌地区考察信仰活动与图像材料的关系[3]。")
add_body("关于药师信仰的起源，赵竟汐归纳了印度、西域以及佛教思想与中国本土观念融合等不同意见，并明确指出学界尚无统一结论[1]。因此，设计研究不宜把某一种起源说写成定论。更可靠的做法是把本设计的文化依据限定在汉译药师经典、敦煌图像及其在中国社会中的历史接受上。")

doc.add_heading("1.2 七佛系统与现实关怀", level=2)
add_body("七佛是本设计人数结构的直接文化依据。赵竟汐指出，现存汉译药师经本中，义净译本对东方七佛有较完整的叙述；敦煌第220窟北壁又保存了罕见的早期七佛立像，为七佛的视觉并置提供了实物依据[1]。七佛并非七种现代情绪的历史对应物。将七种情绪分别配置给七佛，是当代交互设计建立的解释系统，应通过佛名、本愿、手势、持物、色彩和语言之间的语义一致性加以论证，并在展签中说明哪些属于传统依据，哪些属于设计转译。")
add_body("从疗愈思想看，现有宗教史研究强调的是救苦、安乐、消灾和现实关怀。现代设计可以从中提取慈悲回应、身心安定和主动行善等价值，但不能宣称装置能够治疗疾病。对观众而言，作品更合理的目标是帮助其注意情绪、表达状态、接受象征性回应，并形成一个可执行的积极行动。这样的目标与药师文化的关怀方向相连，同时保留学术和伦理边界。")

doc.add_heading("1.3 对本设计的启示", level=2)
add_body("药师文化研究为作品提供三层内容框架。第一层是人物身份，即七佛名号、本愿与传统造像特征；第二层是价值语义，即慈悲救苦、无忧吉祥、身心安乐与积极行动；第三层是体验行为，即观众通过选择、开启和回应完成个人化接触。三层必须相互验证。若只保留佛像外观，作品容易成为传统元素装饰；若只使用鼓励性语言，又会失去七佛之间的文化差异。")

doc.add_heading("2 西夏药师佛艺术视觉元素研究现状", level=1)
doc.add_heading("2.1 第220窟七佛图式的视觉依据", level=2)
add_body("赵竟汐将第220窟北壁东方药师经变画概括为大型全景式经变。画面约高3.42米、宽5.4米，中心为七身药师佛，周围配置八大菩萨、神将、圣众、力士、飞天、乐师、舞者、莲池、宝台、灯轮与楼阁等元素[1]。人物尺寸承担清晰的等级功能：七佛最大，八大菩萨次之，神将与圣众再次，乐师舞者较小。研究者还指出画面虽总体对称，却通过人物面貌、动作、服饰、飘带和卷云的差异避免呆板。")
add_figure(fig1, "图1  敦煌莫高窟第220窟北壁东方药师经变画全景  来源 赵竟汐论文图1.4 PDF第11页 原图引自中国敦煌壁画全集5", width_cm=9.5)
add_body("这套层级关系适合转化为数字场景：佛像作为视觉焦点，光背和粒子承担第二层信息，莲池、灯轮、乐舞和建筑纹样构成环境层。设计不必复刻满构图，但可以保留中心聚焦、左右平衡和局部变化三项原则，使七个角色既属于同一系统，又能通过动作和光效形成个体差异。")

doc.add_heading("2.2 色彩 材料与动态线索", level=2)
add_body("论文把第220窟的色彩特征概括为土红、石绿、石青、黄色等有限矿物色之间的组织关系，并强调冷暖对比、颜料厚薄和局部品质差异共同建立主次[1]。暖色集中于主要人物，石绿与石青在环境和次要部分形成相对安静的背景。由此可见，传统色彩的价值不在于提取若干色号，而在于用色彩关系引导注意力。")
add_figure(fig2, "图2  第220窟局部人物 动作与服饰差异  来源 赵竟汐论文图1.5和图1.6 PDF第12页 原图引自中国敦煌壁画全集5", width_cm=14.2)
add_body("动作、飘带、卷云、乐舞和灯光为三维动画提供了传统动态线索。七个角色的差异可以体现在手势速度、身体朝向、光背明暗、粒子轨迹和声音节奏，而不必依赖夸张表情。七种情绪球的颜色可以从土红、石绿、石青、赭黄、白、墨和琉璃蓝等体系展开，但颜色与情绪的对应属于现代设计假设，需要通过小样测试确认观众是否能稳定理解。")

doc.add_heading("2.3 西夏阶段的比较价值", level=2)
add_body("赵竟汐通过历代比较指出，药师经变画从隋代的相对简洁、初唐的生动、盛唐的宏伟，逐步走向中晚唐后的程式化；至五代、宋和西夏，楼阁、人物及空中元素减少，构图更简略和模式化[1]。论文列举西夏五个庙第三窟药师经变局部，认为其相较第220窟减少了十二大愿、九横死、复杂楼阁及空中神佛等内容，整体更集中于药师说法和净土主题。这个判断来自敦煌区域的具体样本，不应扩大为对全部西夏美术的单一评价。")
add_figure(fig3, "图3  西夏五个庙第三窟药师经变局部  来源 赵竟汐论文图2.8 PDF第21页 原图引自中国敦煌壁画全集10")
add_body("西夏材料对本设计的意义，在于提示减法转译的可能。数字装置面对短时体验，不可能容纳经变画全部人物和故事。设计可借鉴后期图式的集中化倾向，保留七佛、莲座、光背、手势、持物与少量净土环境符号，再通过交互逐层揭示信息。这样既避免把壁画压缩成拥挤的数字布景，也能把观众注意力放在所选情绪与对应角色上。")

h3 = doc.add_heading("3 传统文化数字化展示与交互设计研究现状", level=1)
h3.paragraph_format.page_break_before = True
doc.add_heading("3.1 从保存再现到体验传播", level=2)
add_body("传统文化数字化研究早期多集中于数字采集、档案保存、三维重建和虚拟复原。随着交互技术进入博物馆和展览空间，研究重点逐步扩展到教育、展陈增强、探索式学习和虚拟博物馆。Bekele等对文化遗产领域增强现实、虚拟现实和混合现实的综述表明，这些技术已被用于教育、展览增强、文化遗产探索、重建和虚拟博物馆等场景[13]。UNESCO也将数字制图、三维建模、虚拟博物馆和扩大文化可及性列为文化数字化的重要实践方向[16]。")
add_body("研究同时表明，数字化不等于文化理解。Carrozzino和Bergamasco在博物馆沉浸式虚拟现实研究中，依据交互和沉浸程度对装置进行分类，并讨论成本、可用性和感官体验质量之间的约束[14]。这意味着设计评价不能只记录模型精度、粒子数量或投影规模，还应观察观众是否理解七佛差异、是否知道下一步操作、是否能把反馈与自己的选择联系起来。")

doc.add_heading("3.2 交互作为意义生成机制", level=2)
add_body("在传统展陈中，观众通常沿既定说明阅读图像。交互装置改变的是意义获得方式：观众的选择成为系统输入，系统反馈又反过来解释这一选择。对琉璃愈心而言，情绪球不是普通按钮。球体封闭、被选择、被亲手打开，再释放佛像、声音和粒子，这一物理过程承担了从内在状态到外在表达的象征转换。只有当动作、反馈与文化语义一致时，交互才构成内容，而非附加效果。")
add_figure(flow_img, "图4  琉璃愈心的体验转译链  本文绘制", width_cm=16.0)
add_body("现有数字遗产项目常以再现原貌或增加信息为目标，而本设计更接近解释性转译。它不追求把洞窟复制进屏幕，而是从传统图像中选择人物关系、色彩秩序、光明意象和仪式性动作，重新组织为当代观众可以完成的一段体验。由此，真实性也不只表现为造型复原，还包括来源可追溯、传统与创新边界清楚以及核心语义不被误读。")

h33 = doc.add_heading("3.3 适用于本项目的设计原则", level=2)
table = doc.add_table(rows=1, cols=4)
table.alignment = WD_TABLE_ALIGNMENT.CENTER
table.style = "Table Grid"
headers = ["研究问题", "传统依据", "交互转译", "验证方式"]
for i, h in enumerate(headers):
    set_cell_shading(table.rows[0].cells[i], "223B39")
    set_cell_text(table.rows[0].cells[i], h, bold=True, color=(255,255,255), size=9)
rows = [
    ("如何识别状态", "药师信仰关怀身心疾苦", "选择七色情绪球", "选择用时与情绪匹配度"),
    ("如何形成行动", "礼敬与仪式具有行为过程", "双手打开实体球体", "操作成功率与动作理解"),
    ("如何区分七佛", "佛名 本愿 手势 持物", "差异化角色和反馈", "角色辨识与文化理解"),
    ("如何建立沉浸", "琉璃光 乐舞 莲池 净土", "声 光 粒子同步反馈", "临场感与感官一致性"),
    ("如何避免误导", "宗教语义与现代设计有别", "标注传统依据和设计转译", "访谈中的误读类型"),
]
for row in rows:
    cells = table.add_row().cells
    for i, value in enumerate(row):
        set_cell_text(cells[i], value, size=9)

doc.add_heading("4 情感疗愈与沉浸式体验设计研究现状", level=1)
doc.add_heading("4.1 情绪识别与命名", level=2)
add_body("情绪调节研究为选择情绪球提供了可检验的理论线索。Lieberman等的实验把用词语标记情绪称为情绪命名，并发现相较于其他编码任务，情绪命名与负性情绪图片引发的部分边缘系统反应下降相关[11]。这一结果支持把识别和命名情绪作为体验入口，但它不能直接证明选择彩色球体具有临床疗效。后续关于情绪调节选择的系统综述纳入219项研究，归纳出情感、认知、动机、个体和社会文化等多类影响因素，说明人们选择调节策略的过程具有明显差异[12]。因此，装置应允许观众自主选择，也应提供不确定、跳过或重新选择的空间。")

doc.add_heading("4.2 具身操作与象征性释放", level=2)
add_body("打开球体把抽象情绪转化为可见动作，具有外化和仪式化特征。它的设计价值来自动作与意义的对应：封闭状态象征尚未表达的压力，选择表示识别，开启表示愿意面对并接受回应。此处应使用象征性、体验性表述，避免把动作描述为必然完成情绪释放。观众可能感到安慰、好奇、平静，也可能没有明显变化，评价工具应允许这些结果同时出现。")

doc.add_heading("4.3 临场感与多模态反馈", level=2)
add_body("沉浸体验研究通常区分技术提供的沉浸条件和观众形成的临场感。Slater提出场所错觉与可信性错觉，用来解释参与者为什么会对虚拟环境作出较真实的反应[15]。Diemer等的综述则指出，临场感与情绪反应之间存在联系，但不同研究的测量方式和因果解释并不一致[17]。因此，投影更大、粒子更多或音量更强，并不自动产生更好的疗愈体验。对于本项目，更重要的是所选球体、出现的佛像、声音语气、光色变化和疗愈语句保持一致，并直接回应观众刚刚完成的动作。")
add_body("鼓励性语言宜从空泛安慰转向温和而具体的行动提示。例如，与焦虑相关的反馈可提示放慢呼吸并确认当下可完成的一件事；与疲惫相关的反馈可允许暂停和休息；与自我怀疑相关的反馈可引导回忆已经完成的行动。语句应避免保证结果、否定负面情绪或要求观众保持积极。若作品面向公共展览，还应设置退出机制、音量提示、闪烁控制和隐私保护。")

doc.add_heading("4.4 可评价的疗愈体验", level=2)
add_body("高质量的毕业设计需要把疗愈从宣传语转化为可观察指标。测试可分为三个层次：第一，交互可用性，包括是否理解选择与开启动作、是否顺利触发反馈；第二，文化理解，包括是否能区分七佛、是否理解光明和本愿等符号；第三，情绪体验，包括体验前后自评变化、是否感到被回应以及是否形成具体行动意向。样本量较小时，应把结果写为设计测试和趋势观察，不作普遍心理效应推断。")

doc.add_heading("5 现有研究不足与本设计切入点", level=1)
doc.add_heading("5.1 现有研究之间的断层", level=2)
add_body("第一，药师佛研究以经典考证、信仰流变和图像史为主，能够回答七佛从何而来、如何被表现，却较少回答现代观众如何通过动作理解这些内容。第二，数字遗产研究已经形成三维重建、虚拟展示和沉浸传播的方法，但许多项目仍以观看和信息获取为中心，文化语义与交互行为之间缺少逐项对应。第三，情绪体验研究提供了情绪命名、自主选择和临场感等概念，但这些概念很少与药师七佛的本愿、造像和光明意象共同构成体验。第四，疗愈主题设计容易用主观感受代替评价，缺乏对可用性、文化理解和情绪体验的分层验证。")
add_figure(gap_img, "图5  研究基础与设计缺口  本文绘制", width_cm=16.0)

doc.add_heading("5.2 本设计的核心切入点", level=2)
add_body("琉璃愈心的价值在于建立一套可被执行和检验的文化转译模型。观众首先选择与自身状态相符的情绪球，完成情绪识别；随后亲手打开球体，把内在感受转化为具身行动；系统依据选择触发对应佛像、声音、粒子和语言，形成多模态回应；最后以温和、具体的提示把体验指向休息、表达、求助或完成一件小事等积极行动。四个阶段分别对应文化内容、交互动作、感官反馈和行为意向，避免传统符号与技术效果彼此分离。")
add_body("七佛角色的设计应建立可追溯的角色档案。每个角色至少记录佛名与本愿来源、传统造像依据、现代情绪主题、主色与辅助色、动作、光效、声音、疗愈语句和风险说明。若传统文献没有规定某种颜色或情绪对应关系，应明确标记为设计者转译。通过这一方法，创新不再依赖任意拼贴，而成为可以说明、比较和修改的设计过程。")

h53 = doc.add_heading("5.3 设计验证建议", level=2)
h53.paragraph_format.page_break_before = True
table2 = doc.add_table(rows=1, cols=3)
table2.alignment = WD_TABLE_ALIGNMENT.CENTER
table2.style = "Table Grid"
for i, h in enumerate(["验证维度", "观察指标", "建议方法"]):
    set_cell_shading(table2.rows[0].cells[i], "7A3F34")
    set_cell_text(table2.rows[0].cells[i], h, bold=True, color=(255,255,255), size=9)
tests = [
    ("文化准确性", "七佛身份和本愿是否清楚 传统与创新是否可区分", "专家访谈 文献对照"),
    ("角色辨识", "观众能否从色彩 动作 光效和语言区分角色", "卡片分类 半结构访谈"),
    ("交互可用性", "选择 开启 触发和退出是否顺畅", "任务完成率 观察记录"),
    ("情绪体验", "是否感到被回应 体验前后主观状态是否变化", "简短量表 开放式描述"),
    ("行动转化", "是否记住并愿意执行反馈中的一个行动", "即时提问 延迟回访"),
    ("安全与包容", "是否出现不适 误解 强迫感或隐私顾虑", "退出记录 风险访谈"),
]
for row in tests:
    cells = table2.add_row().cells
    for i, value in enumerate(row):
        set_cell_text(cells[i], value, size=9)

# Keep table rows intact and repeat the header after an unavoidable page break.
for t in (table, table2):
    trPr = t.rows[0]._tr.get_or_add_trPr()
    tbl_header = OxmlElement("w:tblHeader")
    tbl_header.set(qn("w:val"), "true")
    trPr.append(tbl_header)
    for row in t.rows:
        trPr = row._tr.get_or_add_trPr()
        cant_split = OxmlElement("w:cantSplit")
        trPr.append(cant_split)

doc.add_heading("结论", level=1)
add_body("现有文献已经为本项目提供了相对扎实的分项基础。药师信仰研究解释了救苦、安乐和现实关怀的文化来源；敦煌及西夏比较研究提供了七佛图式、中心层级、冷暖色彩、光明意象和图式简化的视觉依据；数字遗产研究说明交互与沉浸技术能够扩展文化展示，但必须同时处理内容理解与可用性；情绪研究则为情绪识别、自主选择、具身行动和多模态回应提供了方法线索。现有研究的主要不足不在某一领域完全空白，而在这些知识尚未被组织成一条面向现代观众的完整体验路径。")
add_body("因此，本设计需要证明的不是药师佛本身具有意义，而是以下研究判断：别人研究过药师佛，也研究过数字交互，但是还没有很好地解决如何把药师佛的疗愈精神转化为现代人的体验这个问题，所以琉璃愈心的设计探索具有价值。")

doc.add_heading("参考文献", level=1)
refs = [
    "[1] 赵竟汐. 浅析东方药师经变画的样式与时代特点 以敦煌莫高窟第220窟北壁为例[D]. 2024.",
    "[2] 张悦, 韩宏彬. 药师佛信仰在南北朝 隋唐时期的流变[J]. 法音, 2023(06): 26-37.",
    "[3] 党燕妮. 中古时期敦煌地区的药师佛信仰[J]. 南京晓庄学院学报, 2013, 29(06).",
    "[4] 罗华庆. 敦煌壁画中的东方药师净土变[J]. 敦煌研究, 1989(02): 5-18+126.",
    "[5] 陈卉. 敦煌220窟东方药师经变画乐舞图像研究[J]. 西北大学学报 哲学社会科学版, 2016.",
    "[6] 李徽, 罗明. 敦煌莫高窟第220窟北壁壁画乐舞图之艺术探析[J]. 装饰, 2013(11): 76-77.",
    "[7] 余志雯. 剥落与变色之美 敦煌莫高窟220窟北壁初唐壁画东方药师经变画的审美价值探析[D]. 中国艺术研究院, 2021.",
    "[8] 段文杰, 樊锦诗卷主编. 中国敦煌壁画全集5 敦煌初唐[M]. 沈阳: 辽宁美术出版社; 天津: 天津人民美术出版社, 2006.",
    "[9] 中国敦煌壁画全集编辑委员会编, 段文杰主编. 中国敦煌壁画全集10 敦煌西夏元[M]. 天津: 天津人民美术出版社, 1996.",
    "[10] 大正新修大藏经刊行会. 大正藏 第4册[M]. 1934.",
    "[11] Lieberman M D, Eisenberger N I, Crockett M J, et al. Putting Feelings Into Words Affect Labeling Disrupts Amygdala Activity in Response to Affective Stimuli[J]. Psychological Science, 2007, 18(5): 421-428. DOI: 10.1111/j.1467-9280.2007.01916.x.",
    "[12] Matthews M, Webb T L, Shafir R, et al. Identifying the determinants of emotion regulation choice A systematic review with meta-analysis[J]. Cognition and Emotion, 2021, 35(6): 1056-1084. DOI: 10.1080/02699931.2021.1945538.",
    "[13] Bekele M K, Pierdicca R, Frontoni E, et al. A Survey of Augmented Virtual and Mixed Reality for Cultural Heritage[J]. Journal on Computing and Cultural Heritage, 2018, 11(2): Article 7, 1-36. DOI: 10.1145/3145534.",
    "[14] Carrozzino M, Bergamasco M. Beyond virtual museums Experiencing immersive virtual reality in real museums[J]. Journal of Cultural Heritage, 2010, 11(4): 452-458. DOI: 10.1016/j.culher.2010.04.001.",
    "[15] Slater M. Place illusion and plausibility can lead to realistic behaviour in immersive virtual environments[J]. Philosophical Transactions of the Royal Society B, 2009, 364(1535): 3549-3557. DOI: 10.1098/rstb.2009.0138.",
    "[16] UNESCO. Culture and Digital Technologies[EB/OL]. https://www.unesco.org/en/culture-and-digital-technologies.",
    "[17] Diemer J, Alpers G W, Peperkorn H M, et al. The impact of perception and presence on emotional reactions A review of research in virtual reality[J]. Frontiers in Psychology, 2015, 6: 26. DOI: 10.3389/fpsyg.2015.00026.",
]
for ref in refs:
    p = doc.add_paragraph()
    p.paragraph_format.first_line_indent = Cm(0)
    p.paragraph_format.left_indent = Cm(0.74)
    p.paragraph_format.first_line_indent = Cm(-0.74)
    p.paragraph_format.line_spacing = 1.15
    p.paragraph_format.space_after = Pt(2)
    rr = p.add_run(ref)
    rr.font.size = Pt(9)

doc.add_heading("图像来源说明", level=1)
add_body("图1至图3均从用户提供的赵竟汐论文相应页面中截取，用于学术分析。该论文中的原图分别引自中国敦煌壁画全集5和中国敦煌壁画全集10。图4和图5为本文依据综述结论绘制的分析图。引用图像仅用于毕业设计研究与文献综述，正式公开发表时仍需依据学校规范核查图像授权和原始出版物信息。")

# Keep captions and tables readable; set table widths approximately.
for table in doc.tables:
    for row in table.rows:
        for cell in row.cells:
            cell.margin_top = Cm(0.1)
            cell.margin_bottom = Cm(0.1)

doc.core_properties.title = "基于药师佛疗愈精神的数字交互装置设计研究综述"
doc.core_properties.subject = "琉璃愈心 药师七佛盲盒交互装置设计"
doc.core_properties.author = ""
doc.save(DOCX_PATH)
print(DOCX_PATH)
