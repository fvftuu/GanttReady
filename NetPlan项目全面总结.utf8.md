# NetPlan 椤圭洰鍏ㄩ潰鎬荤粨

## 1. 椤圭洰姒傝堪
- **椤圭洰鍚嶇О**锛歂etPlan锛堢綉缁滆鍒掞級- 杩涘害璁″垝绠＄悊绯荤粺
- **鎶€鏈爤**锛欱lazor Server (.NET 10) + SQLite + Chart.js
- **杩愯鏃?*锛?NET 10.0.203锛宯ode 24.14.0
- **鍚姩绔彛**锛歨ttp://localhost:5000
- **鏍稿績鍔熻兘**锛氱敇鐗瑰浘銆佹椂鏍囧弻浠ｅ彿缃戠粶鍥撅紙SVG锛夈€丆PM璋冨害寮曟搸銆佽祫婧愮鐞嗐€佽鍒掑垎鏋?
## 2. 椤甸潰璺敱
| 椤甸潰 | 璺敱1 | 璺敱2 |
|------|-------|-------|
| 棣栭〉 | `/` | |
| 鐢樼壒鍥?| `/project/{ProjectId:int}/gantt` | |
| 缃戠粶鍥?| `/project/{ProjectId:int}/network` | |
| 璧勬簮 | `/project/{ProjectId:int}/resources` | |
| 鍒嗘瀽 | `/analysis` | `/project/{ProjectId:int}/analysis` |

## 3. 椤圭洰缁撴瀯
```
i:\NetPlan\
鈹溾攢鈹€ src/NetPlan.Server/
鈹?  鈹溾攢鈹€ Program.cs                    # 鍏ュ彛锛孌I娉ㄥ唽锛屼腑闂翠欢
鈹?  鈹溾攢鈹€ GlobalUsings.cs
鈹?  鈹溾攢鈹€ Pages/
鈹?  鈹?  鈹溾攢鈹€ Index.razor              # 棣栭〉锛堥」鐩垪琛?鏂板缓/瀵煎叆/瀵煎嚭+鍗曢€夎仈鍔級
鈹?  鈹?  鈹斺攢鈹€ Project/
鈹?  鈹?      鈹溾攢鈹€ Gantt.razor          # 鐢樼壒鍥鹃〉闈?鈹?  鈹?      鈹溾攢鈹€ Network.razor        # 鏃舵爣缃戠粶鍥鹃〉闈?鈹?  鈹?      鈹溾攢鈹€ Resources.razor      # 璧勬簮绠＄悊椤甸潰
鈹?  鈹?      鈹斺攢鈹€ Analysis.razor       # 璁″垝鍒嗘瀽浠〃鏉?鈹?  鈹溾攢鈹€ Shared/
鈹?  鈹?  鈹溾攢鈹€ MainLayout.razor         # 鍏ㄥ眬椤堕儴瀵艰埅鏍忥紙navToChecked鑱斿姩锛?鈹?  鈹?  鈹斺攢鈹€ NavMenu.razor            # (鏈娇鐢紝淇濈暀鐨勫師濮嬫ā鏉?
鈹?  鈹溾攢鈹€ Models/
鈹?  鈹?  鈹溾攢鈹€ Project.cs              # 椤圭洰瀹炰綋
鈹?  鈹?  鈹溾攢鈹€ TaskItem.cs             # 浠诲姟瀹炰綋锛堝惈CPM瀛楁锛?鈹?  鈹?  鈹溾攢鈹€ TaskRelation.cs          # 浠诲姟鍏崇郴锛團S/SS/SF/FF锛?鈹?  鈹?  鈹溾攢鈹€ Resource.cs             # 璧勬簮瀹炰綋锛堜汉宸?鏉愭枡/璁惧锛?鈹?  鈹?  鈹溾攢鈹€ ResourceAssignment.cs   # 璧勬簮鍒嗛厤
鈹?  鈹?  鈹斺攢鈹€ AnalysisResult.cs       # 鍒嗘瀽缁撴灉DTO
鈹?  鈹溾攢鈹€ Services/
鈹?  鈹?  鈹溾攢鈹€ ProjectService.cs / IProjectService.cs
鈹?  鈹?  鈹溾攢鈹€ ScheduleEngine.cs / IScheduleEngine.cs  # CPM绠楁硶
鈹?  鈹?  鈹溾攢鈹€ ResourceService.cs / IResourceService.cs
鈹?  鈹?  鈹溾攢鈹€ AnalysisService.cs / IAnalysisService.cs
鈹?  鈹?  鈹斺攢鈹€ ExcelTemplateService.cs  # Excel瀵煎叆瀵煎嚭
鈹?  鈹溾攢鈹€ Data/
鈹?  鈹?  鈹斺攢鈹€ NetPlanDbContext.cs     # EF Core + SQLite
鈹?  鈹斺攢鈹€ wwwroot/
鈹?      鈹溾攢鈹€ css/site.css             # 鍏ㄥ眬鏍峰紡
鈹?      鈹斺攢鈹€ js/netplan.js            # 鏍稿績鍓嶇JS锛?68琛岋級
```

## 4. 鐢樼壒鍥撅紙Gantt.razor锛?
### 椤靛ご缁撴瀯锛坔eader-left + header-right锛?```
header-left: [椤圭洰涓嬫媺] [椤甸潰鏍囬] [娣诲姞/鍒犻櫎/涓婄Щ/涓嬬Щ/鍒楄缃寜閽甝 [鍓嶉攱绾?浠婃棩绾?鍏崇郴绾?璧勬簮鎶曞叆 toggle]
header-right: [棰勮] [瀵煎嚭] [妯℃澘] [瀵煎叆] [- 100% +] [馃攳閫傚簲椤甸潰] [閲嶇畻璁″垝]
```

### 鍏抽敭鍙傛暟
- `zoomLevel`锛氶粯璁?00锛岃寖鍥?-3000锛屾瘡娆÷?
- `dayWidth = 40 * (zoomLevel / 100)`锛?00%鏃舵瘡澶?0px
- `ZoomToFit()`锛歚targetDayW = (rightWidth - 20) / totalDays; newZoom = targetDayW / 40 * 100`

### 鏃堕棿鏍囧昂3妗ｏ紙鐢樼壒鍥?& 缃戠粶鍥句竴鑷达級
| dayWidth | 涓婂眰 | 涓嬪眰 | 鍛ㄦ湯 |
|----------|------|------|------|
| >20px | yyyy/MM | dd锛堟棩锛?| 鍏棩鐏板簳(#f0f0f0)+鐏板瓧(#999) |
| 10-20px | yyyy锛堝勾锛?| 绗琋鍛紙ISO 8601锛?| 鈥?|
| <10px | yyyy锛堝勾锛?| MM锛堟湀锛?| 鈥?|

## 5. 鏃舵爣缃戠粶鍥撅紙Network.razor + netplan.js锛?
### 鏋舵瀯
```
Network.razor (C#)
  鈫?BuildData(): 浠嶥B鍙杢ask+relation 鈫?搴忓垪鍖朖SON 鈫?hidden input
  鈫?杞妫€娴?JS 绔?token 鍙樺寲
  鈫?JS renderNetwork(): 瑙ｆ瀽JSON 鈫?calculateTimeParams() 鈫?calculateVerticalLayout() 鈫?buildNetworkSvg()
  鈫?SVG innerHTML 娉ㄥ叆 #cy 瀹瑰櫒
  鈫?娴忚鍣ㄥ師鐢熸粴鍔紙绾靛悜+妯悜锛?```
**CSS 鍏抽敭**锛歚.page-main` 蹇呴』涓?`display:flex; flex-direction:column`锛屽惁鍒?`.network-wrapper` 鐨?`flex:1` 鍦?block 涓婁笅鏂囦腑鏃犳晥锛屽鑷存暣鏉￠摼鎾戠牬涓嶆粴鍔ㄣ€?
### SVG buildNetworkSvg 娓叉煋灞傜骇
1. `<defs>` 绠ご鏍囪 + 娓愬彉
2. 鏃堕棿鏍囧昂锛氫笂灞?8px(yyyy/MM鎴栧勾) + 涓嬪眰24px(鏃?鍛?鏈? = 52px鎬婚珮
3. 铏氱绾匡紙铏氱嚎L褰㈣矾寰勶級
4. 宸ヤ綔绠嚎锛圠褰㈠疄绾?+ 绠ご + 鍚嶇О/宸ユ湡鏍囨敞 + 鑷敱鏃跺樊娉㈠舰锛?5. 浜嬩欢鑺傜偣锛坄<circle>` + `<text>` 缂栧彿锛宑lass="net-event" data-task-id锛?6. 浠婃棩绾匡紙绾㈣壊铏氱嚎锛寉=0 鍒板浘搴曪級
7. 鍓嶉攱绾匡紙鍙嫋鍔ㄧ孩鑹茬珫绾?+ 涓夎鎵嬫焺 + 鏃ユ湡鏍囩锛実roup id="net-progress-line"锛?8. 绔栫嚎缃戞牸锛堝勾搴︾矖绾?鏈堝害缁嗙嚎锛岃疮绌垮叏鍥撅級
9. 鍥句緥锛堝叧閿嚎璺?闈炲叧閿?铏氬伐浣滐級
10. 鏍囬锛堝乏涓婇」鐩悕 + 鍙充笂鎬诲伐鏈?+ 搴曢儴瑙勭▼鏍囧噯锛?
### 鑺傜偣璁＄畻娉曞竷灞€锛坈alculateTimeParams + calculateVerticalLayout锛?- 姝ｅ悜浼犳挱璁＄畻 ES/EF锛屽弽鍚戣绠?LS/LF
- TF = LS-ES, FF = 鍚庣画ES-EF
- 鍨傜洿鍒嗗眰锛氭嫇鎵態FS锛屽叧閿矾寰勫湪涓婂眰
- LAYER_HEIGHT = 60px锛堢揣鍑戝竷灞€锛?- X鍧愭爣 = MARGIN_LEFT(80) + ES * dayWidth

### 鍓嶉攱绾匡紙JGJ/T121-2015 杩涘害璇勪及锛?- ID锛歚net-progress-line` 鍙嫋鍔?group
- 鎷栧姩浜嬩欢锛歮ousedown鈫掕褰曞垵濮嬩綅缃?startX, startLineX, startScrollLeft)锛宮ousemove鈫掔Щ鍔?琛ュ伩婊氬姩澧為噺+閲嶇畻锛宮ouseup鈫掑仠姝?- 鍏紡锛歚螖 = actualCompletion% - clamp((checkDate-ES)/duration*100, 0, 100)`
- 缁胯壊(#27ae60)锛毼?=0锛堟甯?瓒呭墠锛?- 榛勮壊(#f39c12)锛?20<=螖<0锛堣交寰惤鍚庯級
- 绾㈣壊(#e74c3c)锛毼?-20锛堜弗閲嶈惤鍚庯級鎴栧叧閿矾寰?
### 椤靛ご鏍峰紡锛堜笌鐢樼壒鍥惧榻愶級
```
header-left: [椤圭洰涓嬫媺] [鏍囬] [鍓嶉攱绾?浠婃棩绾?toggle]
header-right: [馃枿棰勮] [閲嶇畻璁″垝] [- 100% +] [馃攳閫傚簲椤甸潰] [鍏抽敭璺緞/鏃跺樊 toggle]
```

### C# 鍏抽敭瀛楁
- `zoomLevel = 100`锛坉ouble锛?- `showCriticalHighlight / showFloatLabels / showProgressLine / showTodayLine`锛氬潎涓篵ool
- `BuildData()` 鐢熸垚 JSON 鍏冪礌鍒楄〃锛坱asks鍚?id/label/name/eventNum/duration/es/ef/ls/lf/tf/ff/completion/isCritical锛? 鍏崇郴鍒楄〃
- `optionsJson` 搴忓垪鍖栦紶閫掞細showCritical/showFloat/showTodayLine/showProgressLine/projectStartDate/totalDays/dayWidth/projectName
- 鍙屽嚮浜嬩欢锛歚[JSInvokable] OpenTaskEditor(int taskId)` 鈫?鎵撳紑缂栬緫瀵硅瘽妗?
## 6. 璧勬簮绠＄悊锛圧esources.razor锛?- 鍒嗙被鏍囩鏍忥紙鍏ㄩ儴/浜哄伐/鏉愭枡/璁惧锛? 褰╄壊badge
- 澶氶€?鎵归噺鎿嶄綔锛堝叏閫?瀵煎嚭閫変腑/鎵归噺鍒犻櫎锛?- 鎵归噺瀵煎叆瀵煎嚭Excel锛?鍒楁ā鏉匡紝3涓狝PI绔偣锛?- 鍏变韩/椤圭洰涓撳睘褰掑睘璁剧疆

## 7. 鍒嗘瀽浠〃鏉匡紙Analysis.razor锛?- 鍏变韩璧勬簮鏁扮粺璁″崱鐗?- 鍚勯」鐩祫婧愬啿绐佹瑙堣〃
- 璺ㄩ」鐩祫婧愪娇鐢ㄨ鎯?
## 8. 鏋勫缓鍜屽惎鍔?
### 浠?bash锛堟湁 NuGet 鐜闄愬埗锛?```bash
# 涓嶈兘 restore锛圢uGet path1 bug锛夛紝闇€瑕佺敤 --no-restore
dotnet build src/NetPlan.Server/NetPlan.Server.csproj --no-restore
dotnet run --project src/NetPlan.Server/NetPlan.Server.csproj --no-build
```

### 浠?PowerShell锛堢幆澧冩甯革級
```powershell
dotnet run --project i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj
```

### 鍋滄湇鍔?```bash
powershell -Command "Get-Process 'NetPlan.Server' -EA 0 | Stop-Process -Force"
```

## 9. 閲嶈鏁欒鍜屽潙

1. **NuGet path1 bug**锛歜ash 鐜鐨?`Environment.GetFolderPath` 杩斿洖 null锛屽鑷?`dotnet restore` 澶辫触銆傚繀椤荤敤 `--no-restore`銆侾owerShell 鐜姝ｅ父銆?
2. **Blazor script 鏍囩闄愬埗**锛歚<script>` 鏍囩鍐呯粷涓嶈兘鍖呭惈 `@鍙橀噺` 缁戝畾锛圖OM diff 浼氭姤 SyntaxError锛夈€傝В鍐虫柟妗堬細鐢?hidden input + JS 杞 token 妫€娴嬨€?
3. **OnAfterRenderAsync 棰勬覆鏌?*锛氶娓叉煋鏃舵湇鍔＄涔熸墽琛岋紝JS Interop 浼氭姏寮傚父銆傞渶瑕?try/catch 鍖呰９銆?
4. **鏂囦欢閿佸畾**锛歚dotnet build` 鍓嶅繀椤诲厛 `Stop-Process NetPlan.Server`锛屽惁鍒?exe 琚攣銆?
5. **CSS overflow 閾?*锛歚.page-main` 蹇呴』 `display:flex; flex-direction:column; min-height:0`锛屽惁鍒欏瓙瀛?flex 瀹瑰櫒涓嶇害鏉熼珮搴︺€俙#cy` 涓嶈兘璁?`height:100%`锛堜細闄愬埗 SVG 鎾戝紑锛夛紝鍙 `min-height:400px`銆?
6. **棣栭〉鍗曢€夎仈鍔?*锛氶椤电敤 radio 鍗曢€夐」鐩紙`checkedProjectId` 瀛?localStorage锛夛紝MainLayout 瀵艰埅鐢?`navToChecked(page)` 璺宠浆鍒板嬀閫夐」鐩〉闈€傛墍鏈夐〉闈笅鎷夋鍙樉绀哄嬀閫夐」鐩紙鏃犲嬀閫夋椂灞曠ず鍏ㄩ儴锛夈€侸S 鎻愪緵 `getCheckedProject()` / `setCheckedProject(id)` / `navToChecked(page)`銆?
## 10. netplan.js 鍏抽敭鍑芥暟绱㈠紩

| 鍑芥暟 | 琛屾暟(approx) | 鐢ㄩ€?|
|------|-------------|------|
| `syncRightToLeft/syncLeftToRight` | 6-27 | 鐢樼壒鍥剧旱鍚戞粴鍔ㄥ悓姝?|
| `initPanelResize` | 49 | 鐢樼壒鍥惧乏渚ч潰鏉挎嫋鎷?|
| `calculateTimeParams` | 232 | CPM鑺傜偣璁＄畻娉曪紙姝ｅ悜+鍙嶅悜锛?|
| `calculateVerticalLayout` | 354 | 鎷撴墤BFS鍨傜洿鍒嗗眰 |
| `getISOWeek` | 240 | ISO 8601 鍛ㄦ璁＄畻 |
| `svgArrowMarker` | 460 | SVG绠ご鏍囪鐢熸垚 |
| `buildNetworkSvg` | 473 | 瀹屾暣SVG鏋勫缓鍣紙鏍囧昂+绠嚎+鑺傜偣+鍓嶉攱绾匡級 |
| `updateProgressColors` | 710 | JGJ/T121-2015 杩涘害璇勪及+鑺傜偣鐫€鑹?|
| `renderNetwork` | 772 | 涓绘覆鏌撳叆鍙ｏ紝瑙ｆ瀽JSON鈫掕绠椻啋鏋勫缓SVG鈫掔粦瀹氫簨浠?|
| `networkFit` | 878 | 閫傚簲瑙嗗浘锛圕SS scale缂╂斁锛?|
| `getActiveProject/setActiveProject` | 894 | localStorage椤圭洰閫変腑鐘舵€?|
| `getCheckedProject/setCheckedProject` | 936 | localStorage棣栭〉鍗曢€夛紙璺ㄦ爣绛捐仈鍔級 |
| `navToChecked` | 944 | 瀵艰埅鍒板嬀閫夐」鐩殑鎸囧畾椤甸潰 |
| `startNetworkPoller` | 908 | 杞妫€娴媡oken鍙樺寲瑙﹀彂娓叉煋 |
