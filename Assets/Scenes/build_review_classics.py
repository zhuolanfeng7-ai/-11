from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path(r"D:\My project (2)\Assets\Scenes")
TMP=ROOT/"tmp"/"literature_review"
OUT=ROOT/"output"; OUT.mkdir(exist_ok=True)
DOCX=OUT/"琉璃愈心_药师七佛盲盒交互装置设计文献综述_经典文献重写版.docx"
FONT=r"C:\Windows\Fonts\msyh.ttc"; FONTB=r"C:\Windows\Fonts\msyhbd.ttc"

def diagram(path):
    im=Image.new("RGB",(1800,780),"#F7F5EF"); d=ImageDraw.Draw(im)
    ft=ImageFont.truetype(FONTB,48); fh=ImageFont.truetype(FONTB,30); fb=ImageFont.truetype(FONT,25)
    d.text((75,45),"经典语义向当代体验的转译层级",font=ft,fill="#111111")
    cards=[("经典层","七佛名号与本愿\n救苦 安乐 光明 行善","#E7EFEA"),("视觉层","琉璃色 光环 手印\n药钵 莲座 对称秩序","#E8E6F0"),("交互层","选择情绪球并打开\n触发形象 声音 粒子","#F0E8D8"),("体验层","识别 承认 释放\n获得回应 形成行动","#EEDFD9")]
    x=70
    for i,(h,b,c) in enumerate(cards):
        d.rounded_rectangle((x,180,x+360,470),26,fill=c,outline="#888888",width=3)
        d.text((x+30,215),h,font=fh,fill="#111111")
        yy=295
        for line in b.split("\n"):
            d.text((x+30,yy),line,font=fb,fill="#333333"); yy+=50
        if i<3:
            d.line((x+375,325,x+415,325),fill="#8B6A3C",width=7)
            d.polygon([(x+415,325),(x+395,310),(x+395,340)],fill="#8B6A3C")
        x+=430
    d.text((75,545),"研究边界",font=fh,fill="#111111")
    d.text((75,610),"七种情绪是面向当代观众建立的设计映射，并非经典中既有的心理分类；疗愈指文化体验与情绪支持，不替代医学诊疗。",font=fb,fill="#444444")
    im.save(path)

flow=TMP/"classics_translation.png"; diagram(flow)

doc=Document(); sec=doc.sections[0]
sec.top_margin=Cm(2.35); sec.bottom_margin=Cm(2.2); sec.left_margin=Cm(2.6); sec.right_margin=Cm(2.45)
normal=doc.styles["Normal"]; normal.font.name="宋体"; normal._element.rPr.rFonts.set(qn("w:eastAsia"),"宋体"); normal.font.size=Pt(11); normal.paragraph_format.line_spacing=1.55; normal.paragraph_format.space_after=Pt(5); normal.paragraph_format.first_line_indent=Cm(.74)
for name,size in [("Title",22),("Heading 1",16),("Heading 2",13)]:
    st=doc.styles[name]; st.font.name="微软雅黑"; st._element.rPr.rFonts.set(qn("w:eastAsia"),"微软雅黑"); st.font.size=Pt(size); st.font.bold=True; st.font.color.rgb=RGBColor(0,0,0); st.paragraph_format.space_before=Pt(12); st.paragraph_format.space_after=Pt(8); st.paragraph_format.keep_with_next=True
    ppr=st._element.get_or_add_pPr(); b=ppr.find(qn("w:pBdr"));
    if b is not None: ppr.remove(b)

def body(t): doc.add_paragraph(t)
def fig(path,cap,w=14.5):
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.first_line_indent=Cm(0); p.paragraph_format.keep_with_next=True; p.add_run().add_picture(str(path),width=Cm(w))
    c=doc.add_paragraph(); c.alignment=WD_ALIGN_PARAGRAPH.CENTER; c.paragraph_format.first_line_indent=Cm(0); c.paragraph_format.space_after=Pt(9); r=c.add_run(cap); r.font.size=Pt(9); r.font.color.rgb=RGBColor(80,80,80)
def shade(cell,fill):
    pr=cell._tc.get_or_add_tcPr(); s=OxmlElement("w:shd"); s.set(qn("w:fill"),fill); pr.append(s)
def table(headers,rows,widths=None):
    t=doc.add_table(rows=1,cols=len(headers)); t.alignment=WD_TABLE_ALIGNMENT.CENTER; t.style="Table Grid"
    for i,h in enumerate(headers):
        c=t.rows[0].cells[i]; shade(c,"333333"); c.text=h
        for r in c.paragraphs[0].runs: r.font.color.rgb=RGBColor(255,255,255); r.bold=True; r.font.size=Pt(9)
    for ri,row in enumerate(rows):
        cells=t.add_row().cells
        for i,v in enumerate(row):
            cells[i].text=v; cells[i].vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            if ri%2: shade(cells[i],"F3F3F3")
            for p in cells[i].paragraphs:
                p.paragraph_format.first_line_indent=Cm(0); p.paragraph_format.space_after=Pt(2)
                for r in p.runs: r.font.size=Pt(9)
    doc.add_paragraph().paragraph_format.space_after=Pt(1)
    return t
def page_field(p):
    p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run(); a=OxmlElement("w:fldChar"); a.set(qn("w:fldCharType"),"begin"); b=OxmlElement("w:instrText"); b.set(qn("xml:space"),"preserve"); b.text=" PAGE "; c=OxmlElement("w:fldChar"); c.set(qn("w:fldCharType"),"end"); r._r.extend([a,b,c])
page_field(sec.footer.paragraphs[0])

p=doc.add_paragraph(style="Title"); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run("基于药师佛疗愈精神的数字交互装置设计研究综述")
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.first_line_indent=Cm(0); r=p.add_run("琉璃愈心·药师七佛盲盒交互装置设计"); r.font.name="微软雅黑"; r._element.rPr.rFonts.set(qn("w:eastAsia"),"微软雅黑"); r.font.size=Pt(13)

doc.add_heading("摘要",1)
body("本文以唐代义净译《药师琉璃光七佛本愿功德经》为主要经典起点，并参照玄奘译《药师琉璃光如来本愿功德经》，梳理药师七佛的本愿结构、药师信仰的现实关怀、西夏药师经变的视觉语言，以及数字文化遗产、情绪调节和沉浸体验领域的相关研究。现有成果已分别回答了经典讲什么、图像如何形成、文化遗产如何数字化以及交互如何影响情绪等问题，但这些知识常处于彼此分离的学科框架之中。本文据此提出：设计研究的关键不是把佛像进行表层卡通化，也不是以宗教符号作情绪消费，而是建立“经典语义—视觉编码—身体操作—多模态反馈—积极行动”的转译链。作品以七种颜色球体承载当代情绪选择，以开球动作完成情绪外化，并以七佛形象、声音、光效、粒子和鼓励性语言构成回应，从而探索药师佛慈悲救苦、无忧吉祥、身心安乐与积极行动精神的现代体验方式。")
body("关键词：药师七佛；药师经变；西夏佛教艺术；数字交互装置；情感疗愈；沉浸体验")

doc.add_heading("研究对象与综述路径",1)
body("本综述所说的“药师经”并非单指一种译本。义净译《药师琉璃光七佛本愿功德经》以七佛名号、国土和本愿构成完整系统，是“药师七佛”角色体系的直接文本依据；玄奘译《药师琉璃光如来本愿功德经》则在中国佛教传播与敦煌药师信仰中影响尤深。两种文本的并置，有助于区分“七佛体系的经典来源”与“药师图像的历史传播来源”。本文同时引入敦煌、西夏艺术研究和现代体验研究，目的在于评价已有研究能否支持从宗教思想到互动体验的完整设计转译。")
fig(flow,"图1  经典语义向当代体验的转译层级（作者根据文献研究绘制）",15.2)

doc.add_heading("1 药师佛文化及疗愈思想研究现状",1)
doc.add_heading("1.1 经典文本与七佛体系",2)
body("义净译本开篇借文殊师利之问，明确提出为后世遭受疾病、忧悲和苦恼者宣说佛名、本愿、国土庄严及善巧差别，使其获得安乐。这说明药师信仰的关怀对象既包括身体病苦，也包括忧惧、匮乏、冲突和行为迷失等现实处境。经中的“疗愈”因而不是现代临床医学意义上的治疗，而是一套由愿力、伦理劝善、仪式实践和心理安顿共同构成的文化性救苦机制。")
table(["序号","七佛名号","本愿语义的设计提炼（非经典固定情绪分类）"],[
 ("1","善名称吉祥王如来","离苦得安、匮乏救助与吉祥支持"),("2","宝月智严光音自在王如来","光明照见、安稳与智慧回应"),("3","金色宝光妙行成就如来","善行、尊严与行动成就"),("4","无忧最胜吉祥如来","解除忧恼、恐惧与不安"),("5","法海雷音如来","正见、清晰表达与唤醒"),("6","法海胜慧游戏神通如来","智慧转化、灵活应对与自主性"),("7","药师琉璃光如来","身心安乐、内外澄明与积极作为")])
body("需要特别说明：七佛名号和本愿是经典事实，而把七佛分别对应七种当代情绪，是本设计为便于识别和参与而建立的诠释框架。该映射应在创作中保留文本出处、转译理由与可修订性，避免声称佛经已经提出了现代心理学式的“七情模型”。")
doc.add_heading("1.2 疗愈思想的结构",2)
body("从本愿内容看，药师七佛的现实关怀大致可归纳为四个层次。第一是解除疾病、饥渴、贫困和危险等生存痛苦；第二是缓和忧悲、恐惧、怨害与孤立等心理和关系困境；第三是以光明、智慧、正见象征认知澄明；第四是把获得安乐与布施、慈心、善行等积极行动联系起来。因此，“疗愈”并非单向安慰，而包含由被救助者重新获得行动能力的过程。作品中的鼓励性语言若只停留在“你会好起来”，便削弱了经典的伦理行动维度；更合适的方式是把回应落到呼吸、表达、求助、休息、修复关系或完成一件小事等可执行建议。")
doc.add_heading("1.3 信仰史研究的启示",2)
body("敦煌文献与经变研究表明，药师信仰在中国传播中具有鲜明的现世利益取向，消灾、延寿、治病和获得安稳是持续出现的主题。相关统计也显示玄奘译本在敦煌写本中占据显著位置，而义净七佛译本的直接影响相对有限。这个差异对本设计十分重要：作品可以以义净译本建立七佛叙事骨架，同时从玄奘译本及敦煌图像传统中吸收琉璃光、药钵、灯幡、眷属和净土秩序等视觉资源，但不能把所有历史壁画都简单解释为义净七佛经典的图解。")

doc.add_heading("2 西夏药师佛艺术视觉元素研究现状",1)
doc.add_heading("2.1 从数量判断转向图像制度研究",2)
body("西夏药师图像研究的数量结论会因“经变画”“单尊药师像”“后期重绘”及洞窟断代标准不同而出现差异。因此，仅用作品数量论证信仰兴衰并不稳妥。较有解释力的研究转向图像配置和空间位置：西夏时期的药师经变多见于洞窟侧壁，构图趋向集中、对称和程式化，部分洞窟又出现具有曼荼罗意味的创新布局。图像减少也不必然等于信仰衰退，因为单尊药师像与其他组合形式仍可能持续流行。")
fig(TMP/"fig3_western_xia.jpg","图2  榆林窟第3窟药师佛相关图像（西夏）。图像资料据《中国敦煌壁画全集10 敦煌西夏元》整理",12.7)
doc.add_heading("2.2 可转译的视觉语法",2)
body("药师经变的识别性并不只来自一尊佛像，还来自主尊、胁侍、眷属、供养仪式和净土空间共同形成的视觉制度。药钵或持物说明救苦属性，日光与月光菩萨及十二药叉形成护持关系，七层灯、五色幡、莲座、华盖和光轮构成仪式与光明意象；中轴对称、层级尺度和环绕式布局建立庄严秩序。色彩方面，青绿、石青、赭红、金色和白光不宜被机械复制，而应转化为材质、光效、粒子密度和界面层级。")
fig(TMP/"fig1_cave220_full.jpg","图3  莫高窟第220窟北壁东方药师经变（初唐），用于与西夏图像的构图和仪式元素比较。图像资料据《中国敦煌壁画全集5 敦煌初唐》整理",13.8)
body("对毕业设计而言，视觉研究的价值在于提炼“语法”而非拼贴“纹样”。例如，琉璃可以通过半透明材质和内部发光表现“内外清彻”；光轮可转化为回应出现时的扩散动画；供养灯可转化为情绪被识别后的逐层点亮；手印和坐姿可区分安抚、唤醒、接纳与行动提示。这样既保留传统造像的可识别性，又使动作、光效和声音成为意义载体。")

doc.add_heading("3 传统文化数字化展示与交互设计研究现状",1)
doc.add_heading("3.1 从保存复原到参与式阐释",2)
body("数字文化遗产研究已形成从高清采集、三维重建、数据库保存，到虚拟漫游、增强现实和参与式叙事的连续谱系。Carrozzino和Bergamasco指出，虚拟博物馆只有超越静态模型展示，才能形成真正的文化体验；Bekele等人的综述则表明，增强、虚拟和混合现实被广泛用于教育、可视化、重建和遗址体验。中国的“数字敦煌”“数字藏经洞”进一步说明，游戏引擎、动态光照和交互叙事能够突破洞窟开放、时空和承载量限制。")
body("但“可以走进洞窟”不等于“理解图像”。很多项目仍以更高清、更逼真和更沉浸为主要目标，观众的操作与文化语义之间联系较弱。真正的交互阐释要求每个动作都有意义：用户为什么选择、为什么触碰、反馈为何以这种声音和光出现，以及体验之后获得了什么认知或行为变化。")
doc.add_heading("3.2 本设计的交互语义",2)
table(["文化语义","观众行为","系统反馈","预期体验"],[
 ("觉察当下","选择最接近自身状态的颜色球","球体色彩、文字与呼吸节奏确认","把模糊感受命名并外化"),("破除封闭","亲手旋转或打开球体","阻力变化、裂隙光和声音渐入","形成主动释放的身体隐喻"),("获得回应","观看对应七佛形象","动作、名号、愿文摘要、粒子同步","理解回应来自何种文化语义"),("走向行动","选择一条现实可行的小建议","生成可带走的光签或记录","把安慰转化为行动意向")])
body("“盲盒”在此不是诱导消费和随机稀缺，而是借用“封闭—开启—显现”的熟悉操作结构。为避免娱乐机制压倒宗教内涵，情绪选择应先于角色揭晓，不设置付费抽取、稀有度和攀比排行；观众还应能查看七佛名号、本愿出处和设计转译说明。")

doc.add_heading("4 情感疗愈与沉浸式体验设计研究现状",1)
doc.add_heading("4.1 情绪命名与自主选择",2)
body("Lieberman等人的神经影像研究显示，把情绪用语言标记与情绪反应加工的变化相关；Matthews等人的系统综述进一步指出，情绪调节策略的选择受到情绪强度、认知资源、动机、个体差异和社会文化环境共同影响。这些研究不能直接证明某种艺术装置具有治疗功效，却能支持两个设计判断：第一，先让观众选择并命名状态，比系统替观众判断更尊重主体性；第二，七个选项必须有清晰、非评判性的描述，并允许“暂时说不清”或退出。")
doc.add_heading("4.2 身体动作、多模态反馈与临场感",2)
body("沉浸体验研究区分“技术上的沉浸”与“主观临场感”。Slater提出地方幻觉与可信性可解释用户为何在虚拟情境中产生真实反应；Diemer等人的综述也表明，临场感与情绪反应之间存在密切联系。由此看，投影面积或粒子数量并非越多越好。开球触觉、佛像动作、空间声音、光色和语句只有在时间上同步、语义上一致时，才会形成可信的回应。过度刺激、突然高音量或强闪烁反而可能引发不适，因此应提供音量、亮度、字幕和跳过选项。")
doc.add_heading("4.3 疗愈边界与验证方法",2)
body("本设计宜把“疗愈”界定为促进自我觉察、情绪表达、短时舒缓和积极行动意向的文化体验，而非诊断或治疗精神疾病。疗愈语句应避免绝对承诺和神秘化因果，并在现场提供隐私说明、安静退出和必要的专业求助信息。效果评价可以采用前后简短量表与访谈相结合的方法，观察情绪识别清晰度、文化理解、主观舒缓、行动意向、可用性和不适反应，而不是只用“好看”“震撼”作为成功标准。")

doc.add_heading("5 现有研究不足与本设计切入点",1)
doc.add_heading("5.1 现有研究的四重断裂",2)
body("第一，经典与艺术史研究擅长说明本愿、图像和信仰流变，却较少讨论它们如何成为当代观众可操作的体验。第二，数字遗产研究重保存、复原与展示，对宗教精神如何进入交互规则讨论不足。第三，情绪交互研究常使用通用色彩、呼吸和正念语言，容易脱离具体文化语境。第四，相关创作的评价多集中于传播效果和视觉满意度，缺少对文本忠实度、文化理解、情绪感受和行动变化的综合检验。")
fig(TMP/"gap.png","图4  既有研究基础与“文化语义转化为现代体验”的核心缺口（作者绘制）",15.0)
doc.add_heading("5.2 琉璃愈心的设计切入",2)
body("“琉璃愈心”的切入点不是再做一个可观看的数字佛像，而是把药师七佛的疗愈精神组织成一段由观众完成的体验过程。七种颜色球体承担低门槛的情绪入口；选择行为意味着识别和承认；打开球体把内在情绪转为身体行动；佛像、名号、声音、光效和粒子组成有出处的文化回应；最后的鼓励语与小行动建议把安乐愿景落到现实生活。其创新性应体现在转译关系的可解释性，而不是技术数量。")
body("具体实施时，可为每位佛建立一张“转译档案”：记录名号与愿文出处、核心语义、传统造像依据、颜色与材质理由、动作设计、声音设计、疗愈语句及风险边界；再通过专家审读和用户测试修订。评价上可比较体验前后观众是否更能说出自己的状态、是否理解至少一项药师文化信息、是否感到被尊重和适度舒缓、是否愿意采取一项积极行动，并记录眩晕、宗教误读或选择压力等负面反馈。")
doc.add_heading("结论",1)
body("本综述从经典、图像、数字遗产和情感体验四个领域说明，“琉璃愈心”拥有充分的知识基础，却也面对明确的跨学科空白。高质量设计不应把经典当作装饰性故事库，也不应把现代心理学概念倒置为佛经原义，而应通过可追溯、可体验、可验证的转译，让传统文化的精神结构进入观众的选择、动作和反馈之中。")
body("因此，本综述要证明的不是“药师佛很有意义”，而是：别人研究过药师佛，也研究过数字交互，但是还没有很好地解决“如何把药师佛的疗愈精神转化为现代人的体验”这个问题，所以“琉璃愈心”的设计有价值。")

doc.add_heading("参考文献",1)
refs=[
"[1] （唐）义净译. 药师琉璃光七佛本愿功德经[M]//大正新修大藏经：第14册. No.451.",
"[2] （唐）玄奘译. 药师琉璃光如来本愿功德经[M]//大正新修大藏经：第14册. No.450.",
"[3] 李玉珉. 敦煌药师经变研究[J]. 故宫学术季刊, 1990, 7(3).",
"[4] 罗华庆. 敦煌壁画中的东方药师净土变[J]. 敦煌研究, 1989(2).",
"[5] 王惠民. 隋至唐前期敦煌药师图像考察[J]. 艺术史研究, 2000(2).",
"[6] 宁强. 佛经与图像：敦煌第220窟北壁壁画新解[J]. 故宫学术季刊, 1998, 15(3).",
"[7] 段文杰, 樊锦诗, 主编. 中国敦煌壁画全集5：敦煌初唐[M]. 天津: 天津人民美术出版社, 2006.",
"[8] 段文杰, 樊锦诗, 主编. 中国敦煌壁画全集10：敦煌西夏元[M]. 天津: 天津人民美术出版社, 1996.",
"[9] Carrozzino M, Bergamasco M. Beyond virtual museums: Experiencing immersive virtual reality in real museums[J]. Journal of Cultural Heritage, 2010, 11(4): 452-458.",
"[10] Bekele M K, Pierdicca R, Frontoni E, et al. A survey of augmented, virtual, and mixed reality for cultural heritage[J]. Journal on Computing and Cultural Heritage, 2018, 11(2): 1-36.",
"[11] Mu R, Nie Y, Cao K, et al. Pilgrimage to Pureland: Art, perception and the Wutai mural VR reconstruction[EB/OL]. arXiv:2304.07511, 2023.",
"[12] Lieberman M D, Eisenberger N I, Crockett M J, et al. Putting feelings into words: Affect labeling disrupts amygdala activity in response to affective stimuli[J]. Psychological Science, 2007, 18(5): 421-428.",
"[13] Matthews M, Webb T L, Sheeran P, et al. Identifying the determinants of emotion regulation choice: A systematic review with meta-analysis[J]. Cognition and Emotion, 2021, 35(6): 1056-1084.",
"[14] Slater M. Place illusion and plausibility can lead to realistic behaviour in immersive virtual environments[J]. Philosophical Transactions of the Royal Society B, 2009, 364(1535): 3549-3557.",
"[15] Diemer J, Alpers G W, Peperkorn H M, et al. The impact of perception and presence on emotional reactions: A review of research in virtual reality[J]. Frontiers in Psychology, 2015, 6: 26.",
"[16] 敦煌研究院. 数字敦煌·数字藏经洞[EB/OL]. https://dlc.e-dunhuang.com/.",
]
for x in refs:
    p=doc.add_paragraph(x); p.paragraph_format.first_line_indent=Cm(0); p.paragraph_format.hanging_indent=Cm(.7); p.paragraph_format.line_spacing=1.25; p.paragraph_format.space_after=Pt(3)

doc.core_properties.title="基于药师佛疗愈精神的数字交互装置设计研究综述"
doc.core_properties.subject="琉璃愈心·药师七佛盲盒交互装置设计"
doc.save(DOCX)
print(DOCX)
