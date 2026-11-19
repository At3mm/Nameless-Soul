import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
const bundle=process.env.CODEX_NODE_MODULES || path.join(path.dirname(process.execPath),'../node_modules');
const require=createRequire(path.join(bundle,'diagram-entry.cjs'));
const {instance}=require('@viz-js/viz');
const sharp=require('sharp');
const out=path.dirname(new URL(import.meta.url).pathname).replace(/^\/(\w:)/,'$1');
const project=path.resolve(out,'../..');
const viz=await instance();
const esc=s=>String(s).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const q=s=>JSON.stringify(String(s));
const N=(id,name,kind,fields=[],methods=[],source='')=>({id,name,kind,fields,methods,source});
const E=(a,b,kind,label='',mult='')=>({a,b,kind,label,mult});
const scriptPaths=new Map();
function indexScripts(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,entry.name);if(entry.isDirectory())indexScripts(p);else if(entry.name.endsWith('.cs')){if(scriptPaths.has(entry.name))throw new Error(`Ambiguous script filename ${entry.name}`);scriptPaths.set(entry.name,path.relative(project,p).replaceAll('\\','/'));}}}
indexScripts(path.join(project,'Assets/Scripts'));
const S=(area,file)=>{const p=scriptPaths.get(`${file}.cs`);if(!p)throw new Error(`Missing script ${file}.cs`);return p;};
const actor=N('player','Người chơi','actor');
const diagrams=[];
diagrams.push({id:'01-UC-tong-quan',title:'Hình 1. Use Case tổng quan — NONAME',type:'uc',subtitle:'Phạm vi: chức năng người chơi trong bản game hiện tại.',nodes:[actor,
 ...[['u1','UC01 · Bắt đầu hành trình mới'],['u2','UC02 · Tiếp tục tiến trình đã lưu'],['u3','UC03 · Điều chỉnh cài đặt / gán phím'],['u4','UC04 · Di chuyển và khám phá khu vực'],['u5','UC05 · Chiến đấu với quái / boss'],['u6','UC06 · Quản lý túi đồ và dùng vật phẩm'],['u7','UC07 · Hội thoại và chọn câu trả lời'],['u8','UC08 · Nhận / thực hiện / trả nhiệm vụ'],['u9','UC09 · Mua và bán tại NPC'],['u10','UC10 · Thao tác tại tượng thần'],['u11','UC11 · Thu hồi Soul / vàng đã rơi'],['u12','UC12 · Tạm dừng / trở về menu / thoát']].map(([id,name])=>N(id,name,'uc'))],edges:Array.from({length:12},(_,i)=>E('player','u'+(i+1),'association')),note:'NPC và quái thuộc bên trong hệ thống, không được coi là tác nhân ngoài. Không có đăng nhập hoặc multiplayer trong phạm vi code đã đọc.'});
diagrams.push({id:'02-UC-tuong-than',title:'Hình 2. Use Case chi tiết — tượng thần',type:'uc',subtitle:'Các mục tùy chọn mở rộng thao tác tại tượng; không phải tất cả đều chạy mỗi lần tương tác.',nodes:[actor,N('shrine','UC10 · Thao tác tại tượng thần','uc'),N('pray','Cầu nguyện','uc'),N('upgrade','Nâng chỉ số','uc'),N('seed','Hiến tế Hạt sinh mệnh','uc'),N('purify','Thanh tẩy Ăn hồn','uc'),N('travel','Du hành giữa các tượng','uc'),N('restore','Hồi phục người chơi / bình máu','uc'),N('respawn','Đặt lại quái thường','uc'),N('payexp','Thanh toán EXP nâng cấp','uc'),N('payseed','Tiêu hao hạt, tăng sức chứa bình','uc'),N('payincense','Tiêu hao Hương thanh hồn','uc'),N('unlocked','Kiểm tra tượng đích đã mở','uc')],edges:[E('player','shrine','association'),...['pray','upgrade','seed','purify','travel'].map(x=>E(x,'shrine','extend','«extend»')),E('pray','restore','include','«include»'),E('pray','respawn','include','«include»'),E('upgrade','payexp','include','«include»'),E('seed','payseed','include','«include»'),E('purify','payincense','include','«include»'),E('travel','unlocked','include','«include»')],ordering:[['shrine','pray'],['shrine','upgrade'],['shrine','seed'],['shrine','purify'],['shrine','travel']],note:'Điều kiện: người chơi còn sống, đủ gần và không giao tranh. Bình máu khởi đầu 3, tối đa 10. Thanh tẩy yêu cầu đang nhiễm và có vật phẩm.'});
diagrams.push({id:'03-UC-nhiem-vu-hoi-thoai',title:'Hình 3. Use Case chi tiết — hội thoại và nhiệm vụ',type:'uc',subtitle:'Nhận nhiệm vụ theo cấu hình hội thoại; đủ mục tiêu rồi quay lại đúng NPC sẽ tự trả và nhận thưởng.',nodes:[actor,N('talk','UC07 · Hội thoại / chọn câu trả lời','uc'),N('accept','Nhận nhiệm vụ','uc'),N('report','Trở lại NPC để tự trả nhiệm vụ','uc'),N('shop','Mở cửa hàng NPC','uc'),N('kill','UC05 · Hạ boss yêu cầu','uc'),N('checkavailable','Kiểm tra điều kiện mở nhiệm vụ','uc'),N('checkready','Kiểm tra mục tiêu đã đạt','uc'),N('reward','Nhận phần thưởng nhiệm vụ','uc'),N('progress','Ghi nhận hạ boss / tiến độ','uc')],edges:[E('player','talk','association'),E('player','kill','association'),E('accept','talk','extend','«extend»'),E('report','talk','extend','«extend»'),E('shop','talk','extend','«extend»'),E('accept','checkavailable','include','«include»'),E('report','checkready','include','«include»'),E('report','reward','include','«include»'),E('kill','progress','include','«include»')],ordering:[['talk','accept'],['talk','report'],['talk','shop']],note:'Chuỗi nhiệm vụ sử dụng prerequisites. Tiến trình: Locked → Available → Active → ReadyToReport → Completed; phần thưởng chỉ được trả khi TurnIn thành công.'});

const combat=[
N('controller','PlayerController','MonoBehaviour',['+moveSpeed: float','+IsSprinting: bool'],['+StopSprinting()','+DelayStaminaRecovery()'],S('Player','PlayerController')),
N('dash','PlayerDash','MonoBehaviour',['+dashDuration: float','+dashStaminaCost: float'],['+TryDash(inputDirection)','+CancelDash()'],S('Player','PlayerDash')),
N('pc','PlayerCombat','MonoBehaviour',['+normalAttackSkillName: string','+skill2SkillName: string'],[],S('Player','PlayerCombat')),
N('parry','CharacterParry','MonoBehaviour',['+isParrying: bool'],['+EnableParryWindow()','+ExecuteParry(attacker)'],S('Core','CharacterParry')),
N('stats','CharacterStats','MonoBehaviour',['+maxHP / currentHP: float','+maxStamina / currentStamina: float','+ATK / DEF / STR: float'],['+AddPosture(amount)'],S('Core','CharacterStats')),
N('health','CharacterHealth','MonoBehaviour',['+isDead: bool','+{static} AnyDied: event'],['+TakeDamage(...)','+Heal(amount)'],S('Core','CharacterHealth')),
N('mgr','SkillDamageManager','MonoBehaviour',['+skills: List<SkillData>','+targetLayer: LayerMask'],['+MarkSkillUsed(skillName)','+ExecuteHit(skillName)'],S('Combat','SkillDamageManager')),
N('skill','SkillData','Serializable',['+skillName: string','+damageMultiplier: float','+cooldownTime: float','+attackRange: float'],[],S('Combat','SkillData')),
N('ai','EnemyBehavior','MonoBehaviour',['+currentState: OrcState'],['+IsInCombatWith(target)'],S('Enemies','EnemyBehavior')),
N('aware','EnemyAwareness','MonoBehaviour',['+maxChaseDistance: float'],['+HasLineOfSight(target)','+MaxAlert()'],S('Enemies','EnemyAwareness')),
N('ec','EnemyCombat','MonoBehaviour',['+skillManager: SkillDamageManager'],['+TriggerAttack(...)','+TriggerSkill(...)','#ShouldLunge(skill): bool'],S('Enemies','EnemyCombat')),
N('break','EnemyBreak','MonoBehaviour',['+recoveryDelay: float','+IsBroken / Ratio'],['+ReceiveHit(staminaDamage)','+ClearBreak()'],S('Enemies','EnemyBreak')),
N('phase','BossPhaseController','MonoBehaviour',['+phase1Skills / phase2Skills','+IsTransitioning: bool'],['+AllowsSkill(skill)','+EvaluatePhase()'],S('Enemies','BossPhaseController')),
N('director','BossMovesetDirector','MonoBehaviour',['+combos: List<Combo>','+ObservedMove: PlayerMove'],['+SelectOpening(...)','+OnSkillStarted(...)'],S('Enemies','BossMovesetDirector')),
 ...['SlimeCombat','WerebearCombat','WerewolfCombat','OrcRiderCombat','NecromancerCombat','BlackKnightCombat','BlackKnightBossCombat'].map(name=>N(name,name,'EnemyCombat subclass',[],[],S('Enemies',name)))];
diagrams.push({id:'04-class-chien-dau',title:'Hình 4. Class Diagram — điều khiển và chiến đấu',type:'class',nodes:combat,edges:[E('controller','dash','association','điều khiển dash'),E('dash','stats','association','tiêu hao stamina'),E('pc','mgr','association','gọi kỹ năng'),E('parry','break','dependency','đỡ đòn → sát thương choáng'),E('health','stats','association','đọc / cập nhật HP'),E('health','break','association','chuyển sát thương choáng'),E('mgr','skill','composition','skills','0..*'),E('mgr','stats','association','tính sát thương'),E('mgr','health','dependency','gây sát thương mục tiêu'),E('ai','aware','association','nhận biết / LOS'),E('ai','ec','association','ra lệnh tấn công'),E('ec','mgr','association','thực thi kỹ năng'),E('ec','phase','dependency','lọc chiêu theo phase'),E('ec','director','dependency','chọn đòn / combo'),E('break','stats','association','hồi thanh choáng'),...combat.filter(n=>n.kind==='EnemyCombat subclass').map(n=>E(n.id,'ec','inheritance'))],note:'Các component cùng gắn trên GameObject được nối bằng tham chiếu, không phải kế thừa. Skeleton / Armor Skeleton / Greatsword Skeleton dùng EnemyCombat chung; không có lớp C# riêng tương ứng tên prefab.'});
diagrams.push({id:'05-class-hoi-thoai-nhiem-vu',title:'Hình 5. Class Diagram — tương tác, hội thoại và nhiệm vụ',type:'class',nodes:[
N('interactor','World.PlayerInteractor','MonoBehaviour',['+Target: WorldInteractable','+CanInteract / IsInCombat'],['+TryInteract()','+ShowNotice(text)'],S('World','PlayerInteractor')),
N('base','World.WorldInteractable','abstract MonoBehaviour',['+interactionRadius: float','+interactionLabel: string'],['+TryInteract(player)','#Interact(player): bool {abstract}'],S('World','WorldInteractable')),
N('npc','World.NpcInteractable','MonoBehaviour',['+dialogue: DialogueDefinition','+CurrentNodeId: string'],['+Open(player)','+Choose(index)'],S('World','NpcInteractable')),
N('shop','World.NpcShop','MonoBehaviour',['+Quantity: int'],['+Open(...)','+Trade(action, quantity)'],S('World','NpcShop')),
N('dialogue','DialogueDefinition','ScriptableObject',['+startNodeId: string','+nodes: List<Node>'],['+Find(id): Node'],S('World','DialogueDefinition')),
N('node','DialogueDefinition.Node','Serializable',['+id / speaker / text: string','+nextNodeId: string'],[],S('World','DialogueDefinition')),
N('choice','DialogueDefinition.Choice','Serializable',['+action: DialogueAction','+quest: QuestDefinition','+conditionQuest: QuestDefinition','+requiredStatus: QuestStatus'],[],S('World','DialogueDefinition')),
N('qs','QuestService','static service',['+{static} Changed: event'],['+{static} Accept(quest)','+{static} TurnIn(quest)','+{static} Status(quest)','+{static} OwnedItems()'],S('World','QuestService')),
N('qd','QuestDefinition','ScriptableObject',['+questId / title: string','+prerequisites: List<QuestDefinition>','+objectives / consumables / items'],[],S('World','QuestDefinition')),
N('objective','QuestDefinition.BossObjective','Serializable',['+bossId / encounterId: string','+count: int'],[],S('World','QuestDefinition')),
N('reward','QuestDefinition.ItemReward','Serializable',['+item: QuestItemDefinition','+count: int'],[],S('World','QuestDefinition')),
N('qi','QuestItemDefinition','ScriptableObject',['+itemId / displayName: string','+icon: Sprite'],['+{static} Find(id)'],S('World','QuestItemDefinition')),
N('gate','World.QuestBossEncounter','MonoBehaviour',['+requiredQuest: QuestDefinition','+conversation: NpcInteractable','+CurrentStage: Stage'],['+RefreshQuestState()'],S('World','QuestBossEncounter')),
N('eco','GameEconomy','static service',[],['+{static} TryGrantQuestReward(...)','+{static} BuyConsumable(...)','+{static} SellConsumable(...)'],S('Core','GameEconomy'))],edges:[E('interactor','base','association','chọn đối tượng','0..1'),E('npc','base','inheritance'),E('npc','dialogue','association','kịch bản','0..1'),E('dialogue','node','composition','nodes','0..*'),E('node','choice','composition','choices','0..*'),E('choice','qd','association','quest / điều kiện','0..1'),E('npc','qs','dependency','Accept / TryAutoTurnIn'),E('npc','shop','association','cửa hàng tùy chọn','0..1'),E('shop','eco','dependency','mua / bán'),E('qs','qd','dependency','đọc định nghĩa'),E('qs','eco','dependency','trả thưởng có receipt'),E('qd','objective','composition','objectives','1..*'),E('qd','reward','composition','items','0..*'),E('reward','qi','association','tham chiếu asset','1'),E('gate','qd','association','requiredQuest','1'),E('gate','npc','association','conversation','0..1'),E('gate','qs','dependency','nghe Changed')],note:'NpcInteractable là partial class: thuộc tính và lựa chọn hội thoại còn nằm trong NpcDialogueChoices.cs. Black Knight chỉ chuyển thành boss sau khi hoàn thành nhiệm vụ và chọn giao chiến ở cuối hội thoại.'});
diagrams.push({id:'06-class-checkpoint-luu-game',title:'Hình 6. Class Diagram — checkpoint, kinh tế và lưu game',type:'class',nodes:[
N('cp','World.CheckpointInteractable','WorldInteractable subclass',['+checkpointId: string','+RespawnPosition: Vector3'],['#Interact(player)'],S('World','CheckpointInteractable')),
N('menu','World.CheckpointMenu','MonoBehaviour',['+checkpointName: string','+CurrentSection: int'],['+Pray()','+TravelToSelected()'],S('World','CheckpointMenu')),
N('service','World.CheckpointService','singleton MonoBehaviour',['-{static} instance: CheckpointService','+{static} SavedChanged: event'],['+{static} Save(...)','+{static} TravelTo(...)','+{static} ReturnToLastShrine(...)'],S('World','CheckpointService')),
N('record','CheckpointService.Record','Serializable',['+scenePath / checkpointId: string','+returnScenePath / returnCheckpointId','+x / y / z: float'],[],S('World','CheckpointService')),
N('fade','World.ScreenFader','singleton MonoBehaviour',[],['+FadeOut(...) / FadeIn(...)','+TransitionToScene(...)'],S('World','ScreenFader')),
N('respawn','World.NormalEnemyRespawns','static service',[],['+{static} Register(...)','+{static} ResetNormals(scene)'],S('World','NormalEnemyRespawns')),
N('eco','GameEconomy','static service',['+{static} Gold / Experience / LifeSeeds','+{static} Changed: event'],['+{static} TryUpgrade(stat)','+{static} SacrificeLifeSeed(potion)','+{static} PurifySoulConsumption()'],S('Core','GameEconomy')),
N('settings','EconomySettings','ScriptableObject',[],['+UpgradeCost(purchaseCount)','+Rewards(type, ...)'],S('Core','EconomySettings')),
N('prog','PlayerProgression','MonoBehaviour',[],['+GrantGold(amount)','+GrantExperience(amount)'],S('Player','PlayerProgression')),
N('potion','PlayerHealthPotion','MonoBehaviour',['+StartingCapacity = 3','+MaximumCapacity = 10','+Charges: int'],['+TryUse()','+ResetForRest()'],S('Player','PlayerHealthPotion')),
N('items','PlayerConsumables','MonoBehaviour',['+Selected: ConsumableKind'],['+TryUse(kind)','+Equip(kind)'],S('Player','PlayerConsumables')),
N('catalog','ConsumableCatalog','ScriptableObject',['+items: Item[]'],['+Find(kind): Item'],S('Core','ConsumableCatalog')),
N('soul','SoulConsumption','static service',['+{static} Active / NextChancePercent'],['+{static} OutgoingMultiplier(...)','+{static} IncomingMultiplier(...)'],S('Core','SoulConsumption')),
N('drops','World.PlayerDeathDrops','static service',[],['+{static} OnDeath(player)','+{static} Refresh()'],S('World','PlayerDeathDrops')),
N('pickup','World.PlayerDeathDropPickup','WorldInteractable subclass',['+DropId: string'],['#Interact(player)'],S('World','PlayerDeathDropPickup')),
N('defeat','EnemyDefeatState','static service',[],['+{static} WasDefeated(id)'],S('Enemies','EnemyDefeatState')),
N('bossinv','BossItemInventory','static service',[],['+{static} TryGrant(...)','+{static} OwnedItems()'],S('Core','BossItemInventory')),
N('prefs','UnityEngine.PlayerPrefs','Unity API',[],['+{static} GetString(...)','+{static} SetString(...) / Save()'],'')],edges:[E('cp','menu','association','mở menu'),E('cp','service','dependency','lưu / mở tượng'),E('menu','service','dependency','du hành / ghi nhớ tượng'),E('service','record','association','pending / fallback'),E('service','fade','dependency','chuyển cảnh / fade'),E('menu','respawn','dependency','cầu nguyện'),E('menu','eco','dependency','nâng cấp / hiến tế / thanh tẩy'),E('menu','potion','dependency','hồi bình máu'),E('eco','settings','association','đọc giá / thưởng'),E('prog','eco','dependency','nghe Changed'),E('items','catalog','association','đọc hiệu ứng / icon'),E('items','eco','dependency','quản lý ô dùng nhanh'),E('items','service','dependency','Bùa hồi quy'),E('soul','eco','dependency','trạng thái nhiễm / thưởng'),E('drops','eco','dependency','ghi khoản rơi 50%'),E('drops','pickup','dependency','tạo điểm thu hồi'),E('pickup','eco','dependency','thu hồi khoản rơi'),...['eco','service','defeat','bossinv'].map(id=>E(id,'prefs','dependency','JSON / PlayerPrefs'))],note:'QuestService và GameControls cũng lưu PlayerPrefs (lược bớt khỏi hình này). Chưa có cơ sở dữ liệu SQL/server; definition ScriptableObject và dữ liệu tiến trình JSON là hai nhóm khác nhau.'});

function header(n){return `<TABLE BORDER="0" CELLBORDER="1" CELLSPACING="0" CELLPADDING="8"><TR><TD BGCOLOR="#eef2f5"><FONT POINT-SIZE="11">«${esc(n.kind)}»</FONT><BR/><B>${esc(n.name)}</B></TD></TR>${n.fields.length?`<TR><TD ALIGN="LEFT">${n.fields.map(x=>esc(x).replace('{static}','[static]')).join('<BR ALIGN="LEFT"/>')}<BR ALIGN="LEFT"/></TD></TR>`:''}${n.methods.length?`<TR><TD ALIGN="LEFT">${n.methods.map(x=>esc(x).replace('{static}','[static]')).join('<BR ALIGN="LEFT"/>')}<BR ALIGN="LEFT"/></TD></TR>`:''}</TABLE>`;}
function dot(d){
 const manual=d.id.startsWith('05');
 const lines=[`digraph UML { graph [rankdir=${d.type==='uc'?'LR':'TB'}, bgcolor="white", fontname="Arial", fontsize=19, label=${q(d.title)}, labelloc=t, pad="0.28", nodesep="0.45", ranksep="0.85", splines=polyline, outputorder=edgesfirst, ordering=out]; node [fontname="Arial", fontsize=13, color="#263442"]; edge [fontname="Arial", fontsize=10, color="#526170", arrowsize=.7];`];
 if(d.type==='uc'){
  lines.push('player [shape=plaintext, width=1.5, height=1.7, fixedsize=true, labelloc=b, label="Người chơi"];');
  lines.push('subgraph cluster_system { label="Hệ thống game NONAME"; fontsize=15; color="#758493"; margin=24;');
  const cases=d.nodes.filter(n=>n.kind==='uc');if(d.id.startsWith('01'))cases.reverse();
  for(const n of cases)lines.push(`${q(n.id)} [shape=ellipse, style=filled, fillcolor=white, margin="0.22,0.16", label=${q(n.name)}];`);
  if(d.id.startsWith('01'))lines.push('{rank=same; '+cases.map(n=>q(n.id)).join(';')+';}');
  lines.push('}');
 }else for(const n of d.nodes)lines.push(`${q(n.id)} [shape=plain, label=<${header(n)}>];`);
 for(const e of (d.id.startsWith('01')?[...d.edges].reverse():d.edges)){
  let a=e.a,b=e.b,attrs='';
  if(e.kind==='inheritance'){a=e.b;b=e.a;attrs='dir=back, arrowtail=empty, arrowhead=none';}
  else if(e.kind==='composition')attrs='dir=both, arrowtail=diamond, arrowhead=none';
  else if(e.kind==='dependency'||e.kind==='include'||e.kind==='extend')attrs='style=dashed, arrowhead=vee';
  else attrs=d.type==='uc'?'dir=none':'arrowhead=vee';
  if(e.kind==='extend'||manual)attrs+=', constraint=false';
  const label=[e.label,e.mult].filter(Boolean).join(' · ');
  lines.push(`${q(a)} -> ${q(b)} [${attrs}${label?', '+(e.kind==='extend'?'xlabel':'label')+'='+q(label):''}];`);
 }
 for(const [a,b] of d.ordering??[])lines.push(`${q(a)} -> ${q(b)} [style=invis, weight=8];`);
 if(manual){const rows=[['interactor','gate'],['base','qs'],['npc','qd'],['dialogue','shop','objective','reward'],['node','choice','eco','qi']];for(const row of rows)lines.push('{rank=same; '+row.map(q).join(';')+';}');for(let i=0;i<rows.length-1;i++)lines.push(`${q(rows[i][0])} -> ${q(rows[i+1][0])} [style=invis,weight=100];`);}
 lines.push('}');return lines.join('\n');
}
function puml(d){
 let lines=['@startuml','skinparam backgroundColor white','skinparam shadowing false','skinparam defaultFontName Arial','skinparam classAttributeIconSize 0',`title ${d.title}`];
 if(d.type==='uc'){
  lines.push('left to right direction','actor "Người chơi" as player','rectangle "Hệ thống game NONAME" {');
  for(const n of d.nodes.filter(n=>n.kind==='uc'))lines.push(`usecase ${q(n.name)} as ${n.id}`);
  lines.push('}');
 }else for(const n of d.nodes){lines.push(`${n.kind.startsWith('abstract')?'abstract ':''}class ${q(n.name)} as ${n.id} <<${n.kind}>> {`,...n.fields,...(n.methods.length?['--',...n.methods]:[]),'}');}
 for(const e of d.edges){const op={inheritance:'--|>',composition:'*--',dependency:'..>',include:'..>',extend:'..>',association:d.type==='uc'?'--':'-->'}[e.kind];lines.push(`${e.a} ${op} ${e.mult?q(e.mult)+' ':''}${e.b}${e.label?' : '+e.label:''}`);}
 lines.push(`legend bottom\n${d.note}\nendlegend`,'@enduml');return lines.join('\n');
}
function drawioPage(d,layout){
 const bb=layout.bb.split(',').map(Number),H=bb[3],scale=1.25;
 const cells=['<mxCell id="0"/><mxCell id="1" parent="0"/>'];
 let id=2;const map=new Map();
 for(const g of layout.objects??[]){if(!g.pos&&g.bb){const b=g.bb.split(',').map(Number);cells.push(`<mxCell id="${id++}" value="Hệ thống game NONAME" style="rounded=0;whiteSpace=wrap;html=0;verticalAlign=top;align=left;spacing=12;fillColor=none;strokeColor=#758493;fontSize=16;" vertex="1" parent="1"><mxGeometry x="${b[0]*scale+20}" y="${(H-b[3])*scale+70}" width="${(b[2]-b[0])*scale}" height="${(b[3]-b[1])*scale}" as="geometry"/></mxCell>`);}}
 for(const n of d.nodes){
  const g=layout.objects.find(o=>o.name===n.id);if(!g?.pos)throw Error('Missing layout node '+n.id);
  const [x,y]=g.pos.split(',').map(Number),w=g.width*72,h=g.height*72;const cid=String(id++);map.set(n.id,cid);
  const label=n.kind==='uc'||n.kind==='actor'?n.name:`«${n.kind}»\n${n.name}${n.fields.length?'\n────────────\n'+n.fields.join('\n'):''}${n.methods.length?'\n────────────\n'+n.methods.join('\n'):''}`;
  const style=n.kind==='actor'?'shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;':n.kind==='uc'?'ellipse;':'rounded=0;align=left;spacing=10;';
  cells.push(`<mxCell id="${cid}" value="${esc(label).replaceAll('\n','&#xa;')}" style="${style}whiteSpace=wrap;html=0;fillColor=#ffffff;strokeColor=#263442;fontFamily=Arial;fontSize=14;" vertex="1" parent="1"><mxGeometry x="${(x-w/2)*scale+20}" y="${(H-y-h/2)*scale+70}" width="${w*scale}" height="${h*scale}" as="geometry"/></mxCell>`);
 }
 for(const e of d.edges){
  const style=e.kind==='inheritance'?'endArrow=block;endFill=0;':e.kind==='composition'?'startArrow=diamond;startFill=1;endArrow=none;':e.kind==='association'&&d.type==='uc'?'endArrow=none;':`endArrow=open;${['dependency','include','extend'].includes(e.kind)?'dashed=1;':''}`;
  const sourceName=e.kind==='inheritance'?e.b:e.a,targetName=e.kind==='inheritance'?e.a:e.b;
  const sourceNode=layout.objects.find(o=>o.name===sourceName),targetNode=layout.objects.find(o=>o.name===targetName);
  const ge=layout.edges?.find(edge=>edge.tail===sourceNode._gvid&&edge.head===targetNode._gvid);
  let points=ge?._draw_?.find(op=>op.op==='b')?.points??[];if(e.kind==='inheritance')points=[...points].reverse();
  const route=points.length>2?'<Array as="points">'+points.slice(1,-1).map(([x,y])=>`<mxPoint x="${x*scale+20}" y="${(H-y)*scale+70}"/>`).join('')+'</Array>':'';
  cells.push(`<mxCell id="${id++}" value="${esc([e.label,e.mult].filter(Boolean).join(' · '))}" style="${style}rounded=0;html=0;fontFamily=Arial;fontSize=12;strokeColor=#526170;labelBackgroundColor=#ffffff;" edge="1" parent="1" source="${map.get(e.a)}" target="${map.get(e.b)}"><mxGeometry relative="1" as="geometry">${route}</mxGeometry></mxCell>`);
 }
 cells.push(`<mxCell id="${id++}" value="${esc(d.title)}" style="text;html=0;align=left;fontStyle=1;fontSize=22;" vertex="1" parent="1"><mxGeometry x="20" y="10" width="${bb[2]*scale}" height="40" as="geometry"/></mxCell>`);
 cells.push(`<mxCell id="${id++}" value="${esc(d.note)}" style="text;html=0;whiteSpace=wrap;align=left;fontSize=13;" vertex="1" parent="1"><mxGeometry x="20" y="${H*scale+90}" width="${bb[2]*scale}" height="90" as="geometry"/></mxCell>`);
 return `<diagram id="${d.id}" name="${esc(d.title)}"><mxGraphModel dx="1200" dy="900" grid="1" gridSize="10" page="1" pageWidth="${Math.ceil(bb[2]*scale+60)}" pageHeight="${Math.ceil(H*scale+200)}"><root>${cells.join('')}</root></mxGraphModel></diagram>`;
}
fs.mkdirSync(path.join(out,'images'),{recursive:true});fs.mkdirSync(path.join(out,'sources'),{recursive:true});
const drawio=[],index=[];
const sourceRefs=new Map();
for(const d of diagrams){
 for(const n of d.nodes)if(n.source){const text=fs.readFileSync(path.join(project,n.source),'utf8');const last=n.name.split('.').at(-1);if(!new RegExp('\\bclass\\s+'+last+'\\b').test(text))throw Error('Class not found '+n.name+' '+n.source);sourceRefs.set(n.name,n.source);}
 const code=dot(d);fs.writeFileSync(path.join(out,'sources',d.id+'.dot'),code);fs.writeFileSync(path.join(out,'sources',d.id+'.puml'),puml(d));
 const layout=viz.renderJSON(code,{engine:'dot'});
 fs.writeFileSync(path.join(out,'sources',d.id+'.layout.json'),JSON.stringify(layout,null,2));
 let svg=viz.renderString(code,{format:'svg',engine:'dot'});
 if(d.type==='uc'){
  const g=layout.objects.find(o=>o.name==='player');const [x,y]=g.pos.split(',').map(Number),cy=-y-20;
  svg=svg.replace(/(<g id="node\d+" class="node">\s*<title>player<\/title>)/,`$1<g fill="none" stroke="#263442" stroke-width="1.8"><circle cx="${x}" cy="${cy-18}" r="10"/><path d="M${x},${cy-8} L${x},${cy+23} M${x-21},${cy+3} L${x+21},${cy+3} M${x},${cy+23} L${x-19},${cy+47} M${x},${cy+23} L${x+19},${cy+47}"/></g>`);
  svg=svg.replace(/(<g id="node\d+" class="node">\s*<title>player<\/title>[\s\S]*?<text[^>]* y=")[^"]+/,`$1${cy-40}`);
  svg=svg.replace(/<g id="edge\d+" class="edge">[\s\S]*?<\/g>/g,chunk=>chunk.includes('<title>player')?chunk.replace(/d="M[-\d., ]+?([CL])/,`d="M${x+21},${cy+3}$1`):chunk);
 }
 fs.writeFileSync(path.join(out,'images',d.id+'.svg'),svg);
 await sharp(Buffer.from(svg),{density:190}).resize({width:4400,height:5200,fit:'inside',withoutEnlargement:true}).png().withMetadata({density:300}).toFile(path.join(out,'images',d.id+'.png'));
 const meta=await sharp(path.join(out,'images',d.id+'.png')).metadata();
 drawio.push(drawioPage(d,layout));index.push({id:d.id,title:d.title,nodes:d.nodes.length,edges:d.edges.length,png:[meta.width,meta.height],note:d.note});
 console.log(d.id,meta.width+'x'+meta.height);
}
fs.writeFileSync(path.join(out,'NONAME-UML.drawio'),`<mxfile host="app.diagrams.net" version="24.7.17">${drawio.join('')}</mxfile>`);
fs.writeFileSync(path.join(out,'sources','diagram-model.json'),JSON.stringify(diagrams,null,2));
fs.writeFileSync(path.join(out,'sources','source-references.json'),JSON.stringify(Object.fromEntries(sourceRefs),null,2));
fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify({date:'2026-11-17',engine:'Unity 6000.3.9f1',diagrams:index},null,2));
const cards=index.map((d,i)=>`<button onclick="show(${i})">${esc(d.title)}</button>`).join('');
fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>NONAME — Use Case và Class</title><style>body{margin:0;background:#f5f6f8;color:#182433;font:15px Arial,sans-serif}header{padding:24px 30px;background:white;border-bottom:1px solid #d5dce3}h1{font-size:24px;margin:0 0 8px}main{display:grid;grid-template-columns:290px minmax(0,1fr);gap:20px;padding:24px}nav button{display:block;width:100%;padding:14px 10px;margin:0 0 8px;text-align:left;background:white;border:1px solid #cbd4dc;color:inherit;cursor:pointer}nav a{display:block;margin:16px 0}article{min-width:0}#viewport{background:white;border:1px solid #cbd4dc;overflow:auto;height:75vh}#image{display:block;margin:auto;width:100%;height:auto;min-width:700px}#note{line-height:1.6;padding:12px 0}.tools{display:flex;gap:16px;align-items:center;margin-bottom:12px}a{color:#245476}small{color:#536474}input{width:170px}@media print{header,nav,.tools{display:none}main{display:block;padding:0}#viewport{height:auto;border:0;overflow:visible}#image{min-width:0}}</style><header><h1>NONAME · Use Case và Class Diagram</h1><small>Đối chiếu code dự án ngày 17/11/2026 · Sơ đồ lớp đã lược bớt thành phần phụ trợ để dùng trong báo cáo.</small></header><main><nav>${cards}<a href="NONAME-UML.drawio">Mở / tải bộ draw.io</a><a href="Bao-cao-UML.md">Đọc thuyết minh và đặc tả UC</a></nav><article><div class="tools"><label>Phóng to <input type="range" min="50" max="220" value="100" oninput="document.getElementById('image').style.width=this.value+'%'"></label><a id="svg" target="_blank">SVG gốc</a><a id="png" target="_blank">PNG báo cáo</a><a id="puml">PlantUML</a></div><div id="viewport"><img id="image" alt="Sơ đồ UML"></div><p id="note"></p></article></main><script>const figures=${JSON.stringify(index)};function show(i){const f=figures[i];document.getElementById('image').src='images/'+f.id+'.svg';document.getElementById('svg').href='images/'+f.id+'.svg';document.getElementById('png').href='images/'+f.id+'.png';document.getElementById('puml').href='sources/'+f.id+'.puml';document.getElementById('note').textContent=f.note}show(0)</script></html>`);
const sourceMd=[...sourceRefs].map(([name,file])=>`- **${name}**: [${path.basename(file)}](../../${file})`).join('\n');
fs.writeFileSync(path.join(out,'Doi-chieu-ma-nguon.md'),'# Đối chiếu lớp và mã nguồn\n\nCác lớp trong sơ đồ đều đã kiểm tra tồn tại trong code. Một số tên dùng dấu chấm để biểu diễn namespace hoặc lớp lồng. Các thuộc tính/phương thức được rút gọn tham số cho dễ đọc.\n\n'+sourceMd+'\n\nNpcInteractable còn có partial class trong [NpcDialogueChoices.cs](../../Assets/Scripts/NPCs/NpcDialogueChoices.cs).\n');
console.log('Validated',sourceRefs.size,'source-backed class labels; exported six diagrams.');
