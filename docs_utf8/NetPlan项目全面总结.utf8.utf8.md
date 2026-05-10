锘? NetPlan 妞ゅ湱娲伴崗銊╂桨閹崵绮?
## 1. 妞ゅ湱娲板鍌濆牚
- **妞ゅ湱娲伴崥宥囆?*閿涙瓊etPlan閿涘牏缍夌紒婊嗩吀閸掓帪绱? 鏉╂稑瀹崇拋鈥冲灊缁狅紕鎮婄化鑽ょ埠
- **閹垛偓閺堫垱鐖?*閿涙lazor Server (.NET 10) + SQLite + Chart.js
- **鏉╂劘顢戦弮?*閿?NET 10.0.203閿涘ode 24.14.0
- **閸氼垰濮╃粩顖氬經**閿涙ttp://localhost:5000
- **閺嶇绺鹃崝鐔诲厴**閿涙氨鏁囬悧鐟版禈閵嗕焦妞傞弽鍥у蓟娴狅絽褰跨純鎴犵捕閸ユ拝绱橲VG閿涘鈧竼PM鐠嬪啫瀹冲鏇熸惛閵嗕浇绁┃鎰吀閻炲棎鈧浇顓搁崚鎺戝瀻閺?
## 2. 妞ょ敻娼扮捄顖滄暠
| 妞ょ敻娼?| 鐠侯垳鏁? | 鐠侯垳鏁? |
|------|-------|-------|
| 妫ｆ牠銆?| `/` | |
| 閻㈡澹掗崶?| `/project/{ProjectId:int}/gantt` | |
| 缂冩垹绮堕崶?| `/project/{ProjectId:int}/network` | |
| 鐠у嫭绨?| `/project/{ProjectId:int}/resources` | |
| 閸掑棙鐎?| `/analysis` | `/project/{ProjectId:int}/analysis` |

## 3. 妞ゅ湱娲扮紒鎾寸€?```
i:\NetPlan\
閳规壕鏀㈤埞鈧?src/NetPlan.Server/
閳?  閳规壕鏀㈤埞鈧?Program.cs                    # 閸忋儱褰涢敍瀛孖濞夈劌鍞介敍灞艰厬闂傜繝娆?閳?  閳规壕鏀㈤埞鈧?GlobalUsings.cs
閳?  閳规壕鏀㈤埞鈧?Pages/
閳?  閳?  閳规壕鏀㈤埞鈧?Index.razor              # 妫ｆ牠銆夐敍鍫ャ€嶉惄顔煎灙鐞?閺傛澘缂?鐎电厧鍙?鐎电厧鍤?閸楁洟鈧浠堥崝顭掔礆
閳?  閳?  閳规柡鏀㈤埞鈧?Project/
閳?  閳?      閳规壕鏀㈤埞鈧?Gantt.razor          # 閻㈡澹掗崶楣冦€夐棃?閳?  閳?      閳规壕鏀㈤埞鈧?Network.razor        # 閺冭埖鐖ｇ純鎴犵捕閸ラ箖銆夐棃?閳?  閳?      閳规壕鏀㈤埞鈧?Resources.razor      # 鐠у嫭绨粻锛勬倞妞ょ敻娼?閳?  閳?      閳规柡鏀㈤埞鈧?Analysis.razor       # 鐠佲€冲灊閸掑棙鐎芥禒顏囥€冮弶?閳?  閳规壕鏀㈤埞鈧?Shared/
閳?  閳?  閳规壕鏀㈤埞鈧?MainLayout.razor         # 閸忋劌鐪い鍫曞劥鐎佃壈鍩呴弽蹇ョ礄navToChecked閼辨柨濮╅敍?閳?  閳?  閳规柡鏀㈤埞鈧?NavMenu.razor            # (閺堫亙濞囬悽顭掔礉娣囨繄鏆€閻ㄥ嫬甯慨瀣侀弶?
閳?  閳规壕鏀㈤埞鈧?Models/
閳?  閳?  閳规壕鏀㈤埞鈧?Project.cs              # 妞ゅ湱娲扮€圭偘缍?閳?  閳?  閳规壕鏀㈤埞鈧?TaskItem.cs             # 娴犺濮熺€圭偘缍嬮敍鍫濇儓CPM鐎涙顔岄敍?閳?  閳?  閳规壕鏀㈤埞鈧?TaskRelation.cs          # 娴犺濮熼崗宕囬兇閿涘湗S/SS/SF/FF閿?閳?  閳?  閳规壕鏀㈤埞鈧?Resource.cs             # 鐠у嫭绨€圭偘缍嬮敍鍫滄眽瀹?閺夋劖鏋?鐠佹儳顦敍?閳?  閳?  閳规壕鏀㈤埞鈧?ResourceAssignment.cs   # 鐠у嫭绨崚鍡涘帳
閳?  閳?  閳规柡鏀㈤埞鈧?AnalysisResult.cs       # 閸掑棙鐎界紒鎾寸亯DTO
閳?  閳规壕鏀㈤埞鈧?Services/
閳?  閳?  閳规壕鏀㈤埞鈧?ProjectService.cs / IProjectService.cs
閳?  閳?  閳规壕鏀㈤埞鈧?ScheduleEngine.cs / IScheduleEngine.cs  # CPM缁犳纭?閳?  閳?  閳规壕鏀㈤埞鈧?ResourceService.cs / IResourceService.cs
閳?  閳?  閳规壕鏀㈤埞鈧?AnalysisService.cs / IAnalysisService.cs
閳?  閳?  閳规柡鏀㈤埞鈧?ExcelTemplateService.cs  # Excel鐎电厧鍙嗙€电厧鍤?閳?  閳规壕鏀㈤埞鈧?Data/
閳?  閳?  閳规柡鏀㈤埞鈧?NetPlanDbContext.cs     # EF Core + SQLite
閳?  閳规柡鏀㈤埞鈧?wwwroot/
閳?      閳规壕鏀㈤埞鈧?css/site.css             # 閸忋劌鐪弽宄扮础
閳?      閳规柡鏀㈤埞鈧?js/netplan.js            # 閺嶇绺鹃崜宥囶伂JS閿?68鐞涘矉绱?```

## 4. 閻㈡澹掗崶鎾呯礄Gantt.razor閿?
### 妞ら潧銇旂紒鎾寸€敍鍧攅ader-left + header-right閿?```
header-left: [妞ゅ湱娲版稉瀣] [妞ょ敻娼伴弽鍥暯] [濞ｈ濮?閸掔娀娅?娑撳﹦些/娑撳些/閸掓顔曠純顔藉瘻闁界敐 [閸撳秹鏀辩痪?娴犲﹥妫╃痪?閸忓磭閮寸痪?鐠у嫭绨幎鏇炲弳 toggle]
header-right: [妫板嫯顫峕 [鐎电厧鍤璢 [濡剝婢榏 [鐎电厧鍙哴 [- 100% +] [棣冩敵闁倸绨叉い鐢告桨] [闁插秶鐣荤拋鈥冲灊]
```

### 閸忔娊鏁崣鍌涙殶
- `zoomLevel`閿涙岸绮拋?00閿涘矁瀵栭崶?-3000閿涘本鐦″▎梅?
- `dayWidth = 40 * (zoomLevel / 100)`閿?00%閺冭埖鐦℃径?0px
- `ZoomToFit()`閿涙瓪targetDayW = (rightWidth - 20) / totalDays; newZoom = targetDayW / 40 * 100`

### 閺冨爼妫块弽鍥ф槀3濡楋綇绱欓悽妯煎閸?& 缂冩垹绮堕崶鍙ョ閼疯揪绱?| dayWidth | 娑撳﹤鐪?| 娑撳鐪?| 閸涖劍婀?|
|----------|------|------|------|
| >20px | yyyy/MM | dd閿涘牊妫╅敍?| 閸忣厽妫╅悘鏉跨俺(#f0f0f0)+閻忔澘鐡?#999) |
| 10-20px | yyyy閿涘牆鍕鹃敍?| 缁楃悑閸涱煉绱橧SO 8601閿?| 閳?|
| <10px | yyyy閿涘牆鍕鹃敍?| MM閿涘牊婀€閿?| 閳?|

## 5. 閺冭埖鐖ｇ純鎴犵捕閸ユ拝绱橬etwork.razor + netplan.js閿?
### 閺嬭埖鐎?```
Network.razor (C#)
  閳?BuildData(): 娴犲顶B閸欐潰ask+relation 閳?鎼村繐鍨崠鏈朣ON 閳?hidden input
  閳?鏉烆喛顕楀Λ鈧ù?JS 缁?token 閸欐ê瀵?  閳?JS renderNetwork(): 鐟欙絾鐎絁SON 閳?calculateTimeParams() 閳?calculateVerticalLayout() 閳?buildNetworkSvg()
  閳?SVG innerHTML 濞夈劌鍙?#cy 鐎圭懓娅?  閳?濞村繗顫嶉崳銊ュ斧閻㈢喐绮撮崝顭掔礄缁鹃潧鎮?濡亜鎮滈敍?```
**CSS 閸忔娊鏁?*閿涙瓪.page-main` 韫囧懘銆忔稉?`display:flex; flex-direction:column`閿涘苯鎯侀崚?`.network-wrapper` 閻?`flex:1` 閸?block 娑撳﹣绗呴弬鍥﹁厬閺冪姵鏅ラ敍灞筋嚤閼峰瓨鏆ｉ弶锟犳懠閹炬垹鐗稉宥嗙泊閸斻劊鈧?
### SVG buildNetworkSvg 濞撳弶鐓嬬仦鍌滈獓
1. `<defs>` 缁狀厼銇旈弽鍥唶 + 濞撴劕褰?2. 閺冨爼妫块弽鍥ф槀閿涙矮绗傜仦?8px(yyyy/MM閹存牕鍕? + 娑撳鐪?4px(閺?閸?閺? = 52px閹鐝?3. 閾忔氨顔勭痪鍖＄礄閾忔氨鍤嶭瑜般垼鐭惧鍕剁礆
4. 瀹搞儰缍旂粻顓犲殠閿涘湢瑜般垹鐤勭痪?+ 缁狀厼銇?+ 閸氬秶袨/瀹搞儲婀￠弽鍥ㄦ暈 + 閼奉亞鏁遍弮璺烘▕濞夈垹鑸伴敍?5. 娴滃娆㈤懞鍌滃仯閿涘潉<circle>` + `<text>` 缂傛牕褰块敍瀹憀ass="net-event" data-task-id閿?6. 娴犲﹥妫╃痪鍖＄礄缁俱垼澹婇搹姘卞殠閿涘瘔=0 閸掓澘娴樻惔鏇礆
7. 閸撳秹鏀辩痪鍖＄礄閸欘垱瀚嬮崝銊у閼硅尙鐝痪?+ 娑撳顫楅幍瀣労 + 閺冦儲婀￠弽鍥╊劮閿涘疅roup id="net-progress-line"閿?8. 缁旀牜鍤庣純鎴炵壐閿涘牆鍕炬惔锔剧煐缁?閺堝牆瀹崇紒鍡欏殠閿涘矁鐤粚鍨弿閸ユ拝绱?9. 閸ュ彞绶ラ敍鍫濆彠闁款喚鍤庣捄?闂堢偛鍙ч柨?閾忔艾浼愭担婊愮礆
10. 閺嶅洭顣介敍鍫濅箯娑撳﹪銆嶉惄顔兼倳 + 閸欏厖绗傞幀璇蹭紣閺?+ 鎼存洟鍎寸憴鍕柤閺嶅洤鍣敍?
### 閼哄倻鍋ｇ拋锛勭暬濞夋洖绔风仦鈧敍鍧坅lculateTimeParams + calculateVerticalLayout閿?- 濮濓絽鎮滄导鐘虫尡鐠侊紕鐣?ES/EF閿涘苯寮介崥鎴ｎ吀缁?LS/LF
- TF = LS-ES, FF = 閸氬海鐢籈S-EF
- 閸ㄥ倻娲块崚鍡楃湴閿涙碍瀚囬幍鎱婩S閿涘苯鍙ч柨顔跨熅瀵板嫬婀稉濠傜湴
- LAYER_HEIGHT = 60px閿涘牏鎻ｉ崙鎴濈鐏炩偓閿?- X閸ф劖鐖?= MARGIN_LEFT(80) + ES * dayWidth

### 閸撳秹鏀辩痪鍖＄礄JGJ/T121-2015 鏉╂稑瀹崇拠鍕強閿?- ID閿涙瓪net-progress-line` 閸欘垱瀚嬮崝?group
- 閹锋牕濮╂禍瀣╂閿涙ousedown閳帟顔囪ぐ鏇炲灥婵缍呯純?startX, startLineX, startScrollLeft)閿涘ousemove閳帞些閸?鐞涖儱浼╁姘З婢х偤鍣?闁插秶鐣婚敍瀹畂useup閳帒浠犲?- 閸忣剙绱￠敍姝氳灃 = actualCompletion% - clamp((checkDate-ES)/duration*100, 0, 100)`
- 缂佽儻澹?#27ae60)閿涙?=0閿涘牊顒滅敮?鐡掑懎澧犻敍?- 姒涘嫯澹?#f39c12)閿?20<=铻?0閿涘牐浜ゅ顔挎儰閸氬函绱?- 缁俱垼澹?#e74c3c)閿涙?-20閿涘牅寮楅柌宥堟儰閸氬函绱氶幋鏍у彠闁款喛鐭惧?
### 妞ら潧銇旈弽宄扮础閿涘牅绗岄悽妯煎閸ユ儳顕鎰剁礆
```
header-left: [妞ゅ湱娲版稉瀣] [閺嶅洭顣絔 [閸撳秹鏀辩痪?娴犲﹥妫╃痪?toggle]
header-right: [棣冩灴妫板嫯顫峕 [闁插秶鐣荤拋鈥冲灊] [- 100% +] [棣冩敵闁倸绨叉い鐢告桨] [閸忔娊鏁捄顖氱窞/閺冭泛妯?toggle]
```

### C# 閸忔娊鏁€涙顔?- `zoomLevel = 100`閿涘潐ouble閿?- `showCriticalHighlight / showFloatLabels / showProgressLine / showTodayLine`閿涙艾娼庢稉绡祇ol
- `BuildData()` 閻㈢喐鍨?JSON 閸忓啰绀岄崚妤勩€冮敍鍧盿sks閸?id/label/name/eventNum/duration/es/ef/ls/lf/tf/ff/completion/isCritical閿? 閸忓磭閮撮崚妤勩€?- `optionsJson` 鎼村繐鍨崠鏍︾炊闁帪绱皊howCritical/showFloat/showTodayLine/showProgressLine/projectStartDate/totalDays/dayWidth/projectName
- 閸欏苯鍤禍瀣╂閿涙瓪[JSInvokable] OpenTaskEditor(int taskId)` 閳?閹垫挸绱戠紓鏍帆鐎电鐦藉?
## 6. 鐠у嫭绨粻锛勬倞閿涘湩esources.razor閿?- 閸掑棛琚弽鍥╊劮閺嶅骏绱欓崗銊╁劥/娴滃搫浼?閺夋劖鏋?鐠佹儳顦敍? 瑜扳晞澹奲adge
- 婢舵岸鈧?閹靛綊鍣洪幙宥勭稊閿涘牆鍙忛柅?鐎电厧鍤柅澶夎厬/閹靛綊鍣洪崚鐘绘珟閿?- 閹靛綊鍣虹€电厧鍙嗙€电厧鍤璄xcel閿?閸掓膩閺夊尅绱?娑撶嫕PI缁旑垳鍋ｉ敍?- 閸忓彉闊?妞ゅ湱娲版稉鎾崇潣瑜版帒鐫樼拋鍓х枂

## 7. 閸掑棙鐎芥禒顏囥€冮弶鍖＄礄Analysis.razor閿?- 閸忓彉闊╃挧鍕爱閺佹壆绮虹拋鈥冲幢閻?- 閸氬嫰銆嶉惄顔跨カ濠ф劕鍟跨粣浣诡洤鐟欏牐銆?- 鐠恒劑銆嶉惄顔跨カ濠ф劒濞囬悽銊嚊閹?
## 8. 閺嬪嫬缂撻崪灞芥儙閸?
### 娴?bash閿涘牊婀?NuGet 閻滎垰顣ㄩ梽鎰煑閿?```bash
# 娑撳秷鍏?restore閿涘湤uGet path1 bug閿涘绱濋棁鈧憰浣烘暏 --no-restore
dotnet build src/NetPlan.Server/NetPlan.Server.csproj --no-restore
dotnet run --project src/NetPlan.Server/NetPlan.Server.csproj --no-build
```

### 娴?PowerShell閿涘牏骞嗘晶鍐╊劀鐢潻绱?```powershell
dotnet run --project i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj
```

### 閸嬫粍婀囬崝?```bash
powershell -Command "Get-Process 'NetPlan.Server' -EA 0 | Stop-Process -Force"
```

## 9. 闁插秷顩﹂弫娆掝唲閸滃苯娼?
1. **NuGet path1 bug**閿涙瓬ash 閻滎垰顣ㄩ惃?`Environment.GetFolderPath` 鏉╂柨娲?null閿涘苯顕遍懛?`dotnet restore` 婢惰精瑙﹂妴鍌氱箑妞よ崵鏁?`--no-restore`閵嗕揪owerShell 閻滎垰顣ㄥ锝呯埗閵?
2. **Blazor script 閺嶅洨顒烽梽鎰煑**閿涙瓪<script>` 閺嶅洨顒烽崘鍛卜娑撳秷鍏橀崠鍛儓 `@閸欐﹢鍣篳 缂佹垵鐣鹃敍鍦朞M diff 娴兼碍濮?SyntaxError閿涘鈧倽袙閸愯櫕鏌熷鍫窗閻?hidden input + JS 鏉烆喛顕?token 濡偓濞村鈧?
3. **OnAfterRenderAsync 妫板嫭瑕嗛弻?*閿涙岸顣╁〒鍙夌厠閺冭埖婀囬崝锛勵伂娑旂喐澧界悰宀嬬礉JS Interop 娴兼碍濮忓鍌氱埗閵嗗倿娓剁憰?try/catch 閸栧懓锛欓妴?
4. **閺傚洣娆㈤柨浣哥暰**閿涙瓪dotnet build` 閸撳秴绻€妞よ鍘?`Stop-Process NetPlan.Server`閿涘苯鎯侀崚?exe 鐞氼偊鏀ｉ妴?
5. **CSS overflow 闁?*閿涙瓪.page-main` 韫囧懘銆?`display:flex; flex-direction:column; min-height:0`閿涘苯鎯侀崚娆忕摍鐎?flex 鐎圭懓娅掓稉宥囧閺夌喖鐝惔锔衡偓淇?cy` 娑撳秷鍏樼拋?`height:100%`閿涘牅绱伴梽鎰煑 SVG 閹炬垵绱戦敍澶涚礉閸欘亣顔?`min-height:400px`閵?
6. **妫ｆ牠銆夐崡鏇⑩偓澶庝粓閸?*閿涙岸顩绘い鐢垫暏 radio 閸楁洟鈧銆嶉惄顕嗙礄`checkedProjectId` 鐎?localStorage閿涘绱滿ainLayout 鐎佃壈鍩呴悽?`navToChecked(page)` 鐠哄疇娴嗛崚鏉垮瑎闁銆嶉惄顕€銆夐棃顫偓鍌涘閺堝銆夐棃顫瑓閹峰顢嬮崣顏呮▔缁€鍝勫瑎闁銆嶉惄顕嗙礄閺冪姴瀣€闁妞傜仦鏇犮仛閸忋劑鍎撮敍澶堚偓渚窼 閹绘劒绶?`getCheckedProject()` / `setCheckedProject(id)` / `navToChecked(page)`閵?
## 10. netplan.js 閸忔娊鏁崙鑺ユ殶缁便垹绱?
| 閸戣姤鏆?| 鐞涘本鏆?approx) | 閻劑鈧?|
|------|-------------|------|
| `syncRightToLeft/syncLeftToRight` | 6-27 | 閻㈡澹掗崶鍓ф棻閸氭垶绮撮崝銊ユ倱濮?|
| `initPanelResize` | 49 | 閻㈡澹掗崶鎯т箯娓氀囨桨閺夋寧瀚嬮幏?|
| `calculateTimeParams` | 232 | CPM閼哄倻鍋ｇ拋锛勭暬濞夋洩绱欏锝呮倻+閸欏秴鎮滈敍?|
| `calculateVerticalLayout` | 354 | 閹锋挻澧FS閸ㄥ倻娲块崚鍡楃湴 |
| `getISOWeek` | 240 | ISO 8601 閸涖劍顐肩拋锛勭暬 |
| `svgArrowMarker` | 460 | SVG缁狀厼銇旈弽鍥唶閻㈢喐鍨?|
| `buildNetworkSvg` | 473 | 鐎瑰本鏆VG閺嬪嫬缂撻崳顭掔礄閺嶅洤鏄?缁狀厾鍤?閼哄倻鍋?閸撳秹鏀辩痪鍖＄礆 |
| `updateProgressColors` | 710 | JGJ/T121-2015 鏉╂稑瀹崇拠鍕強+閼哄倻鍋ｉ惈鈧懝?|
| `renderNetwork` | 772 | 娑撶粯瑕嗛弻鎾冲弳閸欙綇绱濈憴锝嗙€絁SON閳帟顓哥粻妞诲晪閺嬪嫬缂揝VG閳帞绮︾€规矮绨ㄦ禒?|
| `networkFit` | 878 | 闁倸绨茬憴鍡楁禈閿涘湑SS scale缂傗晜鏂侀敍?|
| `getActiveProject/setActiveProject` | 894 | localStorage妞ゅ湱娲伴柅澶夎厬閻樿埖鈧?|
| `getCheckedProject/setCheckedProject` | 936 | localStorage妫ｆ牠銆夐崡鏇⑩偓澶涚礄鐠恒劍鐖ｇ粵鎹愪粓閸旑煉绱?|
| `navToChecked` | 944 | 鐎佃壈鍩呴崚鏉垮瑎闁銆嶉惄顔炬畱閹稿洤鐣炬い鐢告桨 |
| `startNetworkPoller` | 908 | 鏉烆喛顕楀Λ鈧ù濯ken閸欐ê瀵茬憴锕€褰傚〒鍙夌厠 |
