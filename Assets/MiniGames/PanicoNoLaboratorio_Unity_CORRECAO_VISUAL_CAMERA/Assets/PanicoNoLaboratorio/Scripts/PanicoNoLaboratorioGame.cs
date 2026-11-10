using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pânico no Laboratório - versão Unity embutível.
/// Adicione este componente a um GameObject vazio. Não troca de cena nem chama
/// Application.Quit: ao encerrar, dispara ExitRequested para o jogo hospedeiro.
/// </summary>
public class PanicoNoLaboratorioGame : MonoBehaviour
{
    public event Action ExitRequested;

    [Header("Integração")]
    public bool iniciarComIntroducao = true;
    public bool pausarJogoHospedeiro = false;
    public KeyCode teclaPulo = KeyCode.Space;
    public KeyCode teclaDeslizar = KeyCode.DownArrow;

    const float BaseSpeed = 200f, JumpVelocity = 300f, Gravity = 900f;
    const float GameDuration = 60f;
    const float GroundY = 300f;
    const float WorldEnd = 12000f;

    class Thing
    {
        public string id;
        public float x, y, w, h;
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float frameRate = 5, animTime;
        public bool loop = true, consumed, cracked;
        public string damage;
    }

    Camera gameCamera;
    Camera hostCamera;
    int hostCameraOriginalCullingMask;
    bool hostCameraMaskChanged;
    const int MinigameLayer = 31;
    Transform worldRoot, introRoot;
    GameObject playerObject;
    float Y(float godotY) => 360f - godotY;
    SpriteRenderer playerRenderer, bgRenderer, floorRenderer;
    Sprite[] playerRun, playerSlip, playerTrip;
    Sprite playerJump, playerSlide, playerHurt, playerIdle;
    Sprite playerStand, playerTalk, playerWalk1, playerWalk2;
    Sprite screenWorking, screenGlitch, screenAlert, mugUp, mugSpill, smokeSprite, sirenSprite;
    SpriteRenderer introPlayer, introMonitor, introMug, introSmoke, introSiren;
    AudioSource audioSource;
    Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    List<Thing> things = new List<Thing>();
    Vector2 playerPos = new Vector2(100, 252);
    float playerVy, speed = BaseSpeed, slowdown = 1f;
    bool sliding, hurt, frozen, intro, introSkipped, paused, saveDialog, ended, victory;
    bool alarmActive, showAlert, dialogueVisible = true;
    float timer = GameDuration, shake, introElapsed, smokeElapsed, panelShake;
    int points, lives = 3;
    string dialogue = "", endReason = "";
    string warning = "⚠ O tempo no laboratório continua correndo!";
    float originalTimeScale = 1f;
    bool originalCursorVisible;
    CursorLockMode originalCursorLock;
    Rect savePanel;

    void Awake()
    {
        originalTimeScale = Time.timeScale;
        originalCursorVisible = Cursor.visible;
        originalCursorLock = Cursor.lockState;
        // Nunca altere a câmera do jogo hospedeiro. O minigame usa uma câmera
        // própria, em uma camada exclusiva, desenhada por cima da câmera principal.
        hostCamera = Camera.main;
        if (hostCamera != null)
        {
            hostCameraOriginalCullingMask = hostCamera.cullingMask;
            hostCamera.cullingMask &= ~(1 << MinigameLayer);
            hostCameraMaskChanged = true;
        }
        var cameraObject = new GameObject("PanicoNoLaboratorio_Camera");
        gameCamera = cameraObject.AddComponent<Camera>();
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 180f;
        gameCamera.transform.position = new Vector3(320f, 180f, -100f);
        gameCamera.transform.rotation = Quaternion.identity;
        gameCamera.clearFlags = CameraClearFlags.Depth;
        gameCamera.cullingMask = 1 << MinigameLayer;
        gameCamera.depth = hostCamera != null ? hostCamera.depth + 1f : 0f;
        gameCamera.rect = new Rect(0f, 0f, 1f, 1f);
        audioSource = gameObject.AddComponent<AudioSource>();
        LoadAssets();
        if (iniciarComIntroducao) StartIntro();
        else StartRunner();
    }

    void LoadAssets()
    {
        Sprite LoadSprite(string path)
        {
            // As artes são importadas como Sprite pelo importador incluído no pacote.
            var sprite = Resources.Load<Sprite>(path);
            if (!sprite) Debug.LogWarning("Sprite ausente ou não importado como Sprite: " + path);
            return sprite;
        }
        Sprite[] LoadFrames(params string[] paths)
        {
            var a = new Sprite[paths.Length];
            for(int i=0;i<paths.Length;i++) a[i]=LoadSprite(paths[i]);
            return a;
        }
        playerRun = LoadFrames("art/player/PNG/Player/Poses/player_walk1","art/player/PNG/Player/Poses/player_walk2");
        playerSlip = LoadFrames("art/player/PNG/Player/Poses/player_slip_1","art/player/PNG/Player/Poses/player_slip_2");
        playerTrip = LoadFrames("art/player/PNG/Player/Poses/player_trip_1","art/player/PNG/Player/Poses/player_trip_2");
        playerJump=LoadSprite("art/player/PNG/Player/Poses/player_jump");
        playerSlide=LoadSprite("art/player/PNG/Player/Poses/player_slide");
        playerHurt=LoadSprite("art/player/PNG/Player/Poses/player_hurt");
        playerIdle=LoadSprite("art/player/PNG/Player/Poses/player_idle");
        playerStand=LoadSprite("art/player/PNG/Player/Poses/player_stand");
        playerTalk=LoadSprite("art/player/PNG/Player/Poses/player_talk");
        playerWalk1=playerRun[0]; playerWalk2=playerRun[1];
        screenWorking=LoadSprite("art/cutscene/screen_working");
        screenGlitch=LoadSprite("art/cutscene/screen_glitch");
        screenAlert=LoadSprite("art/cutscene/screen_alert");
        mugUp=LoadSprite("art/cutscene/coffee_mug_up");
        mugSpill=LoadSprite("art/cutscene/coffee_spill");
        smokeSprite=LoadSprite("art/cutscene/explosion_puff");
        sirenSprite=LoadSprite("art/cutscene/alarm_siren");
        string[] names={"footstep_wood_000","impactGlass_light_000","zapTwoTone","impactMetal_heavy_000","twoTone1","phaseJump1","powerUp2"};
        foreach(string n in names) clips[n]=Resources.Load<AudioClip>("audio/sfx/Audio/"+n);
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite, Vector2 pos, int order=0, Transform parent=null)
    {
        var go=new GameObject(name);
        go.layer=MinigameLayer;
        if(parent) go.transform.SetParent(parent,false);
        go.transform.position=new Vector3(pos.x,pos.y,0);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sortingOrder=order;
        return sr;
    }
    void Play(string name, float volume=1f)
    {
        if(clips.TryGetValue(name,out var clip) && clip) audioSource.PlayOneShot(clip,volume);
    }

    void StartIntro()
    {
        intro=true; introElapsed=0; introRoot=new GameObject("Panico Intro").transform; introRoot.SetParent(transform,false); dialogue="Ufa... 99% do relatório pronto!\nSó mais uma linha de código e um gole de café...";
        var bg=LoadSpriteRuntime("art/environment/display_lab_background_clean");
        MakeSprite("Intro Background",bg,new Vector2(320,Y(180)),-10,introRoot);
        var floor=LoadSpriteRuntime("art/environment/lab_floor_tile_128");
        var floorSr=MakeSprite("Intro Floor",floor,new Vector2(320,Y(320)),-5,introRoot);
        if(floorSr) floorSr.drawMode=SpriteDrawMode.Tiled;
        MakeSprite("Desk",LoadSpriteRuntime("art/cutscene/lab_desk"),new Vector2(370,Y(240)),1,introRoot);
        introMonitor=MakeSprite("Monitor",screenWorking,new Vector2(385,Y(186)),2,introRoot);
        MakeSprite("Keyboard",LoadSpriteRuntime("art/cutscene/keyboard"),new Vector2(335,Y(222)),3,introRoot);
        introMug=MakeSprite("Coffee Mug",mugUp,new Vector2(308,Y(218)),4,introRoot);
        introSmoke=MakeSprite("Smoke",smokeSprite,new Vector2(385,Y(180)),5,introRoot); if(introSmoke) introSmoke.enabled=false;
        introPlayer=MakeSprite("Scientist",playerStand,new Vector2(240,Y(248)),6,introRoot);
        introSiren=MakeSprite("Siren",sirenSprite,new Vector2(320,Y(12)),7,introRoot);
        if(introSiren) introSiren.transform.localScale=Vector3.one*1.5f;
        Play("footstep_wood_000",.5f);
    }
    Sprite LoadSpriteRuntime(string p)
    {
        var sprite = Resources.Load<Sprite>(p);
        if (!sprite) Debug.LogWarning("Sprite não encontrado em Resources: " + p);
        return sprite;
    }

    void Update()
    {
        if(intro) { UpdateIntro(); return; }
        if(ended)
        {
            if(Input.GetKeyDown(KeyCode.Space)) Restart();
            return;
        }
        if(paused)
        {
            if(Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            return;
        }
        if(Input.GetKeyDown(KeyCode.P)) { TogglePause(); return; }
        if(Input.GetKeyDown(KeyCode.Escape) && saveDialog) { CancelSave(); return; }

        timer-=Time.deltaTime;
        if(saveDialog) warning="CORRA! Restam apenas "+Mathf.Max(0,(int)timer)+" segundos para salvar!";
        if(timer<=0)
        {
            timer=0;
            if(saveDialog) saveDialog=false;
            GameOver("Tempo Esgotado!\nVocê não salvou o relatório a tempo!");
            return;
        }
        UpdatePlayer();
        UpdateThings();
        UpdateCamera();
    }

    void UpdateIntro()
    {
        if(!introSkipped && (Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(0)))
        { introSkipped=true; EndIntro(); return; }
        introElapsed+=Time.deltaTime;
        if(introElapsed<2f) { }
        else if(introElapsed<4f) { if(introPlayer) introPlayer.sprite=playerTalk; }
        else if(introElapsed<6f)
        {
            dialogue="Eita, bati o braço! A CANECA NÃO...!!";
            if(introPlayer) {introPlayer.sprite=playerJump; introPlayer.transform.position=new Vector3(240,Y(236),0);}
            if(introMug) {introMug.sprite=mugSpill; introMug.transform.position=new Vector3(330,Y(246),0);}
            if(introElapsed<4.1f) Play("impactGlass_light_000");
        }
        else if(introElapsed<8.8f)
        {
            dialogue="CURTO-CIRCUITO NO TECLADO!!\nNÃO DESLIGA, POR FAVOR!!";
            if(introPlayer) introPlayer.sprite=playerHurt;
            if(introMonitor) introMonitor.sprite=screenGlitch;
            if(introElapsed<6.1f) Play("zapTwoTone");
            if(introMonitor) introMonitor.color=(Mathf.Sin(introElapsed*25)>0)?new Color(2,2,.5f):Color.white;
        }
        else if(introElapsed<12.2f)
        {
            dialogue="KABOOOOM! O COMPUTADOR EXPLODIU!!";
            if(introMonitor) introMonitor.sprite=screenAlert;
            if(introSmoke) {introSmoke.enabled=true; introSmoke.transform.localScale=Vector3.one*Mathf.Lerp(.5f,1.5f,Mathf.Clamp01((introElapsed-8.8f)/.8f));}
            if(introElapsed<8.9f) Play("impactMetal_heavy_000");
        }
        else if(introElapsed<16.4f)
        {
            alarmActive=true; showAlert=true; dialogueVisible=false;
            if(introElapsed<12.3f) Play("twoTone1");
        }
        else if(introElapsed<18.4f)
        {
            showAlert=false; dialogueVisible=true;
            dialogue="O ÚNICO BACKUP TÁ NO FIM DO CORREDOR!\nPRECISO CORRER AGORA MESMO!!";
        }
        else if(introElapsed<19.8f)
        {
            if(introPlayer) {introPlayer.sprite=(Mathf.FloorToInt(introElapsed*10)%2==0)?playerWalk1:playerWalk2; introPlayer.transform.position=Vector3.Lerp(new Vector3(240,Y(248),0),new Vector3(690,Y(248),0),(introElapsed-18.4f)/1.4f);}
        }
        else EndIntro();
    }
    void EndIntro()
    {
        intro=false;
        if(introRoot) Destroy(introRoot.gameObject);
        StartRunner();
    }

    void StartRunner()
    {
        intro=false; ended=false; paused=false; saveDialog=false; timer=GameDuration; points=0; lives=3;
        speed=BaseSpeed; slowdown=1; hurt=false; sliding=false; frozen=false; playerVy=0; playerPos=new Vector2(100,160);
        worldRoot=new GameObject("Panico World").transform; worldRoot.SetParent(transform,false);
        var bg=LoadSpriteRuntime("art/environment/display_lab_background_clean");
        bgRenderer=MakeSprite("Laboratory Background",bg,new Vector2(344,Y(-110)),-10,worldRoot);
        if(bgRenderer) bgRenderer.transform.localScale=new Vector3(.5f,.58f,1);
        var floor=LoadSpriteRuntime("art/environment/lab_floor_tile_128");
        floorRenderer=MakeSprite("Laboratory Floor",floor,new Vector2(6000,Y(300)+64),-1,worldRoot);
        if(floorRenderer) { floorRenderer.drawMode=SpriteDrawMode.Tiled; floorRenderer.size=new Vector2(30000,128); }
        playerObject=new GameObject("Player"); playerObject.layer=MinigameLayer; playerObject.transform.SetParent(worldRoot,false);
        playerRenderer=playerObject.AddComponent<SpriteRenderer>(); playerRenderer.sprite=playerRun[0]; playerRenderer.sortingOrder=5;
        playerObject.transform.position=new Vector3(playerPos.x,playerPos.y,0);
        AddThing("papers",500,224,58,24,"trip","art/obstacles/a4_papers");
        AddThing("clock",700,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("pencils",900,225,52,14,"slip","art/obstacles/pencils");
        AddThing("wires",1300,218,54,24,"trip","art/obstacles/tangled_wires");
        AddThing("water",1750,194,64,14,"slip","art/obstacles/water_cooler_1");
        AddThing("clock",1950,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("air",2200,20,110,120,"air","art/obstacles/air_conditioner");
        AddThing("notebook_obstacle",2300,216,50,26,"trip","art/obstacles/notebook_intact");
        AddCleaner(2800);
        AddThing("clock",3100,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("papers",3350,224,58,24,"trip","art/obstacles/a4_papers");
        AddThing("wires",3800,218,54,24,"trip","art/obstacles/tangled_wires");
        AddThing("pencils",4250,225,52,14,"slip","art/obstacles/pencils");
        AddThing("clock",4500,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("water",4700,194,64,14,"slip","art/obstacles/water_cooler_1");
        AddThing("air",5100,20,110,120,"air","art/obstacles/air_conditioner");
        AddThing("notebook_obstacle",5200,216,50,26,"trip","art/obstacles/notebook_intact");
        AddCleaner(5750);
        AddThing("clock",6050,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("wires",6300,218,54,24,"trip","art/obstacles/tangled_wires");
        AddThing("papers",6750,224,58,24,"trip","art/obstacles/a4_papers");
        AddThing("pencils",7200,225,52,14,"slip","art/obstacles/pencils");
        AddThing("clock",7450,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("water",7650,194,64,14,"slip","art/obstacles/water_cooler_1");
        AddThing("air",8000,20,110,120,"air","art/obstacles/air_conditioner");
        AddThing("notebook_obstacle",8150,216,50,26,"trip","art/obstacles/notebook_intact");
        AddCleaner(8700);
        AddThing("clock",9000,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("wires",9300,218,54,24,"trip","art/obstacles/tangled_wires");
        AddThing("papers",9750,224,58,24,"trip","art/obstacles/a4_papers");
        AddThing("water",10200,194,64,14,"slip","art/obstacles/water_cooler_1");
        AddThing("air",10550,20,110,120,"air","art/obstacles/air_conditioner");
        AddThing("pencils",10650,225,52,14,"slip","art/obstacles/pencils");
        AddThing("clock",10900,145,28,28,"","art/obstacles/collectible_clock");
        AddThing("notebook_obstacle",11100,216,50,26,"trip","art/obstacles/notebook_intact");
        AddCleaner(11550);
        AddThing("goal",12000,202,64,64,"goal","art/obstacles/goal_pc");
        gameCamera.transform.position=new Vector3(320,180,-100);
    }
    void AddThing(string id,float x,float y,float w,float h,string damage,string asset)
    {
        var s=LoadSpriteRuntime(asset);
        var sr=MakeSprite(id,s,new Vector2(x,Y(y)),2,worldRoot);
        if(sr && id!="air") sr.transform.localScale=new Vector3(w/(s?s.rect.width:w),h/(s?s.rect.height:h),1);
        float hitY = id=="air" ? Y(y+120f) : Y(y);
        float hitH = id=="air" ? 240f : h;
        float hitW = id=="air" ? 128f : w;
        things.Add(new Thing{id=id,x=x,y=hitY,w=hitW,h=hitH,damage=damage,sr=sr});
    }
    void AddCleaner(float x)
    {
        var t=new Thing{id="cleaner",x=x,y=Y(202),w=60,h=45,damage="hurt",frames=LoadFramesRuntime("art/obstacles/cleaner_sweep_1","art/obstacles/cleaner_sweep_2")};
        t.sr=MakeSprite("Cleaner",t.frames[0],new Vector2(x,Y(202)),3,worldRoot); things.Add(t);
    }
    Sprite[] LoadFramesRuntime(params string[] paths){var a=new Sprite[paths.Length];for(int i=0;i<a.Length;i++)a[i]=LoadSpriteRuntime(paths[i]);return a;}

    void UpdatePlayer()
    {
        float dt=Time.deltaTime;
        if(frozen) {playerVy-=Gravity*dt; playerPos.y+=playerVy*dt; return;}
        playerVy-=Gravity*dt;
        bool onFloor=playerPos.y<=148;
        if(onFloor){playerPos.y=148;playerVy=0;}
        if(!hurt)
        {
            if((Input.GetKeyDown(teclaPulo)||Input.GetKeyDown(KeyCode.UpArrow)) && onFloor && !sliding) {playerVy=JumpVelocity;Play("phaseJump1");}
            if(Input.GetKey(teclaDeslizar)&&onFloor&&!sliding) sliding=true;
            if(!Input.GetKey(teclaDeslizar)&&sliding) sliding=false;
            speed=BaseSpeed*slowdown;
            playerPos.x+=speed*dt;
        }
        else playerPos.x+=speed*dt;
        playerPos.y+=playerVy*dt;
        if(playerPos.y<148 && playerVy<=0) {playerPos.y=148;playerVy=0;}
        if(playerRenderer)
        {
            playerObject.transform.position=new Vector3(playerPos.x,playerPos.y-(sliding?12:0),0);
            if(hurt) playerRenderer.sprite=playerHurt;
            else if(sliding) playerRenderer.sprite=playerSlide;
            else if(playerPos.y>148) playerRenderer.sprite=playerJump;
            else playerRenderer.sprite=playerRun[Mathf.FloorToInt(Time.time*10)%2];
            playerRenderer.color=slowdown<1?new Color(.5f,.8f,1):Color.white;
        }
    }

    void UpdateThings()
    {
        foreach(var t in things)
        {
            if(t.consumed) continue;
            if(t.id=="cleaner")
            {
                float d=Mathf.Abs(playerPos.x-t.x);
                if(d>95) {t.x+=Mathf.Sin(Time.time*1.5f)*Time.deltaTime*55; if(t.sr)t.sr.flipX=Mathf.Sin(Time.time*1.5f)>0;}
                else if(t.sr) {t.sr.sprite=t.frames[Mathf.FloorToInt(Time.time*10)%2]; t.sr.flipX=playerPos.x>t.x;}
            }
            float dx=Mathf.Abs(playerPos.x-t.x), dy=Mathf.Abs(playerPos.y-t.y);
            bool overlap=dx<(t.w+32)*.5f && dy<(t.h+(sliding?24:48))*.5f;
            if(!overlap) continue;
            if(t.id=="clock") {t.consumed=true;if(t.sr)t.sr.enabled=false;timer+=5;points+=10;Play("powerUp2");}
            else if(t.id=="air") slowdown=.4f;
            else if(t.id=="goal") {frozen=true;saveDialog=true;}
            else if(t.id=="notebook_obstacle"&&!t.cracked) {t.cracked=true; if(t.sr)t.sr.sprite=LoadSpriteRuntime("art/obstacles/notebook_cracked"); TakeDamage("trip");}
            else if(t.damage!=""&&!hurt) TakeDamage(t.damage);
        }
        if(!things.Exists(t=>t.id=="air"&&!t.consumed&&Mathf.Abs(playerPos.x-t.x)<70)) slowdown=1f;
    }
    void UpdateCamera()
    {
        float target=Mathf.Max(320,playerPos.x+150);
        var p=gameCamera.transform.position;
        gameCamera.transform.position=new Vector3(Mathf.Lerp(p.x,target,Time.deltaTime*4),180,-100);
    }

    void TakeDamage(string type)
    {
        if(hurt||ended)return;
        if(sliding)sliding=false;
        lives--;
        if(lives<=0){GameOver("Ficou sem vidas!");return;}
        hurt=true; speed=0;
        if(type=="slip"){speed=90;playerVy=140;}
        else if(type=="trip"){speed=60;playerVy=90;}
        StartCoroutine(RecoverFromHit());
    }
    IEnumerator RecoverFromHit()
    {
        yield return new WaitForSeconds(.25f); speed=0;
        for(int i=0;i<4;i++){if(playerRenderer)playerRenderer.color=new Color(1,.2f,.2f);yield return new WaitForSeconds(.14f);if(playerRenderer)playerRenderer.color=Color.white;yield return new WaitForSeconds(.14f);}
        hurt=false;speed=BaseSpeed*slowdown;
    }

    void TogglePause(){paused=!paused;if(pausingAllowed)Time.timeScale=paused?0:1;Cursor.visible=paused;Cursor.lockState=paused?CursorLockMode.None:CursorLockMode.Locked;}
    bool pausingAllowed => pausarJogoHospedeiro;
    void Restart()
    {
        StopAllCoroutines();
        if(worldRoot)Destroy(worldRoot.gameObject);
        StartRunner();
    }
    void GameOver(string reason){ended=true;victory=false;paused=false;saveDialog=false;endReason=reason;RestoreHostControls();}
    void Win(){ended=true;victory=true;paused=false;saveDialog=false;RestoreHostControls();}
    void RestoreHostControls(){if(pausarJogoHospedeiro)Time.timeScale=originalTimeScale;Cursor.visible=originalCursorVisible;Cursor.lockState=originalCursorLock;}
    void CancelSave(){warning="ERRO: Cancelar não é uma opção! O tempo está acabando!";panelShake=.3f;}

    void OnGUI()
    {
        GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/640f,Screen.height/360f,1));
        if(intro)
        {
            if(alarmActive)GUI.color=new Color(1,0,0,(Mathf.Sin(Time.time*6)+1)*.2f);
            GUI.Box(new Rect(0,0,640,360),"");
            GUI.color=Color.white;
            if(dialogueVisible)GUI.Box(new Rect(60,285,520,60),dialogue);
            if(showAlert){GUI.Box(new Rect(50,115,540,130),"⚠ ALERTA CRÍTICO: SISTEMA EM COLAPSO ⚠\n\nDADOS DA PESQUISA PRESERVADOS APENAS NO SERVIDOR DO FUNDO!\nO LABORATÓRIO SERÁ SELADO EM 60 SEGUNDOS!");}
            GUI.Label(new Rect(410,5,220,25),"[ESPAÇO / ENTER] Pular Introdução");
            return;
        }
        GUI.color=Color.white;
        GUI.Label(new Rect(20,12,200,30),"TEMPO: "+Mathf.CeilToInt(timer));
        GUI.Label(new Rect(230,12,200,30),"PONTOS: "+points);
        GUI.Label(new Rect(440,12,180,30),"VIDAS: "+lives+" ❤");
        if(paused)
        {
            GUI.Box(new Rect(190,70,260,220),"PAUSADO");
            if(GUI.Button(new Rect(230,125,180,35),"CONTINUAR"))TogglePause();
            if(GUI.Button(new Rect(230,170,180,35),"REINICIAR")){paused=false;Restart();}
            if(GUI.Button(new Rect(230,215,180,35),"SAIR"))RequestExit();
        }
        if(saveDialog)
        {
            GUI.Box(new Rect(110,90,420,180),"Microsoft Word - Salvar Arquivo");
            GUI.Label(new Rect(135,130,370,40),"Deseja salvar as alterações feitas em:\n\"Relatório_Final_v2_DEFINITIVO.docx\"?");
            GUI.Label(new Rect(125,170,390,25),warning);
            if(GUI.Button(new Rect(145,215,95,30),"Salvar")){saveDialog=false;points+=200;Win();}
            if(GUI.Button(new Rect(255,215,110,30),"Não Salvar")){saveDialog=false;GameOver("Você clicou em 'Não Salvar'!\nO relatório foi perdido e a pesquisa falhou!");}
            if(GUI.Button(new Rect(380,215,95,30),"Cancelar"))CancelSave();
            if(GUI.Button(new Rect(510,90,20,20),"X"))CancelSave();
        }
        if(ended)
        {
            GUI.Box(new Rect(110,85,420,190),victory?"VITÓRIA!":"FIM DE JOGO!");
            GUI.Label(new Rect(135,130,370,70),victory?"Relatório Salvo com Sucesso!\nPontos: "+points:endReason);
            GUI.Label(new Rect(175,205,290,25),"ESPAÇO - Reiniciar");
            if(GUI.Button(new Rect(245,240,150,30),"SAIR DO MINIGAME"))RequestExit();
        }
    }
    void RequestExit(){RestoreHostControls();ExitRequested?.Invoke();}
    void CleanupMinigameCamera()
    {
        // Restaura exatamente a máscara original da câmera hospedeira.
        if (hostCameraMaskChanged && hostCamera != null)
        {
            hostCamera.cullingMask = hostCameraOriginalCullingMask;
            hostCameraMaskChanged = false;
        }
        if (gameCamera != null)
        {
            var cameraObject = gameCamera.gameObject;
            gameCamera = null;
            if (cameraObject != null) Destroy(cameraObject);
        }
    }
    void OnDisable(){RestoreHostControls();CleanupMinigameCamera();}
    void OnDestroy(){RestoreHostControls();CleanupMinigameCamera();}
}
