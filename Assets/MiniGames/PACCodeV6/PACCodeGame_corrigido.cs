using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// PAC-CODE V6: port independente da cena/camera, desenhado sobre o jogo com IMGUI.
// Usa tempo não escalado: o laboratório pode continuar com Time.timeScale == 0.
public class PACCodeGame : MonoBehaviour
{
    public event Action ExitRequested;
    public event Action<bool, int> Finished;

    const int W = 19, H = 15, Tile = 36;
    static readonly string[][] Maps = {
        new [] {"###################","#.......#.........#","#######.#.###.#####","#...#.............#","#.###.#######.###.#","#.#...#.....#...#.#","#.#.###.###.###...#","#...#...#.#.#...#.#","#.###.###...#.###.#","#.....#...#.#...#.#","#.#.#.###.#.#####.#","#.#.#...#.#.#.....#","#.#.###.#.#.#.##..#","#.............#...#","###################"},
        new [] {"###################","#.#.........#.....#","#.#######...#.#.#.#","#.......#.#...#...#","#####.#.#.###.###.#","#.....#.#.......#.#","#.###.#.#######.#.#","#...#.#...#.....#.#","###.#.###.#.#######","#...#.....#.......#","#.###############.#","#...#.............#","#.#...###########.#","#.....#...........#","###################"},
        new [] {"###################","#.#.........#...#.#","#.#.###.###.#.#.#.#","#.#...#...#...#...#","#.###.###.#.###..##","#.#.#...#...#.....#","#...###.#.###.###.#","#.#.....#.....#...#","#.###.#########.#.#","#...#.....#.....#.#","###.#####...#.#...#","#.#.........#.#.#.#","#.###.#########.#.#","#.................#","###################"}
    };
    static Vector2Int P(int x, int y) => new Vector2Int(x, y);
    static readonly Vector2Int[][] Items = {
        new [] {P(7,1),P(17,3),P(1,13)},
        new [] {P(17,1),P(1,9),P(17,9),P(5,13),P(13,11)},
        new [] {P(17,1),P(1,5),P(17,5),P(3,9),P(15,9),P(1,13),P(17,13)}
    };
    static readonly Vector2Int[][] BombPickups = {
        new [] {P(3,3),P(13,7)}, new [] {P(5,1),P(15,5),P(9,13)}, new [] {P(3,1),P(9,5),P(11,11),P(7,13)}
    };
    static readonly Vector2Int[][] ShieldPickups = {
        new [] {P(13,11)}, new [] {P(3,5),P(15,13)}, new [] {P(9,3),P(5,11),P(13,13)}
    };
    static readonly Vector2Int[][] BugStarts = {
        new [] {P(17,13),P(17,1),P(9,13)},
        new [] {P(17,13),P(15,1),P(9,13),P(1,11)},
        new [] {P(17,11),P(15,1),P(9,13),P(1,11),P(17,5)}
    };
    static readonly Vector2Int[][] Powers = {
        new [] {P(11,1),P(5,9)}, new [] {P(3,1),P(15,11)}, new [] {P(11,1),P(7,11)}
    };
    static readonly string[] ItemNames = {"CPU","RAM","SSD","MOUSE","TECLADO","MONITOR","GPU"};
    static readonly Vector2Int[] Dirs = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
    class TimedCell { public Vector2Int cell; public float timer; public TimedCell(Vector2Int p,float t){cell=p;timer=t;} }
    readonly List<Vector2Int> bugs = new List<Vector2Int>(), previous = new List<Vector2Int>();
    readonly HashSet<Vector2Int> picked = new HashSet<Vector2Int>(), bombItems = new HashSet<Vector2Int>(), shieldItems = new HashSet<Vector2Int>();
    readonly Dictionary<Vector2Int,int> powers = new Dictionary<Vector2Int,int>();
    readonly List<TimedCell> bombs = new List<TimedCell>(), explosions = new List<TimedCell>();
    readonly Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();
    AudioSource music;
    Texture2D pixel, disc;
    GUIStyle titleStyle, labelStyle, smallStyle, centerStyle;
    int level, lives, points, ammo, shields, overclocks, antiviruses, bossHP;
    Vector2Int robot, facing, boss;
    float invuln, shieldTime, overclockTime, antivirusTime, attackCooldown, attackFlash, elapsed, moveClock, mineClock;
    string state;
    bool bossActive, resultSent;
    System.Random random = new System.Random();
    static readonly Color Bg = Hex("070b19"), Cyan = Hex("64f5e1"), White = Hex("e8f3ff"), Purple = Hex("b987ff"), Green = Hex("70f0ae");
    static Color Hex(string s) { ColorUtility.TryParseHtmlString("#"+s, out var c); return c; }

    void Awake()
    {
        pixel = new Texture2D(1,1,TextureFormat.RGBA32,false); pixel.SetPixel(0,0,Color.white); pixel.Apply();
        disc = new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++) { float dx=x-31.5f,dy=y-31.5f; disc.SetPixel(x,y,dx*dx+dy*dy<=31*31?Color.white:Color.clear); }
        disc.Apply();
        titleStyle = new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold};
        labelStyle = new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold};
        smallStyle = new GUIStyle(GUI.skin.label){fontSize=14};
        centerStyle = new GUIStyle(labelStyle){alignment=TextAnchor.MiddleCenter};
        var audioRoot = new GameObject("PACCodeAudio"); audioRoot.transform.SetParent(transform,false);
        foreach(var key in new[]{"music","collect","bomb","hit","level"}) {
            var clip = Resources.Load<AudioClip>("PACCodeAudio/"+key);
            var src=audioRoot.AddComponent<AudioSource>(); src.clip=clip; src.playOnAwake=false; src.ignoreListenerPause=true;
            if(key=="music") { music=src; src.loop=true; src.volume=.10f; if(clip)src.Play(); }
            else { src.volume=.5f; sounds[key]=src; }
        }
        LoadLevel(0,true);
    }
    void Sound(string name) { if(sounds.TryGetValue(name,out var s) && s.clip){s.Stop();s.Play();} }
    void LoadLevel(int n,bool reset=false)
    {
        level=n; if(reset){lives=3;points=0;} robot=P(1,1);facing=Vector2Int.right;
        bugs.Clear();previous.Clear();for(int i=0;i<3+level;i++) { var pos=BugStarts[level][i];if(!Walkable(pos)||bugs.Contains(pos))pos=P(17,13);bugs.Add(pos);previous.Add(pos); }
        picked.Clear();bombItems.Clear();shieldItems.Clear();powers.Clear();bombs.Clear();explosions.Clear();
        foreach(var p in BombPickups[level])bombItems.Add(p);foreach(var p in ShieldPickups[level])shieldItems.Add(p);
        for(int i=0;i<2;i++)if(Walkable(Powers[level][i]))powers[Powers[level][i]]=i;
        ammo=1+level;shields=overclocks=antiviruses=0;shieldTime=overclockTime=antivirusTime=0;
        bossActive=false;boss=P(17,13);bossHP=5;invuln=2;elapsed=moveClock=attackCooldown=attackFlash=0;mineClock=3;
        state="playing";
    }
    bool Walkable(Vector2Int p) => p.y>=0 && p.y<H && p.x>=0 && p.x<Maps[level][p.y].Length && Maps[level][p.y][p.x]!='#';
    static bool Press(Key k) => Keyboard.current!=null && Keyboard.current[k].wasPressedThisFrame;
    void Update()
    {
        if(Keyboard.current==null)return;
        if(Press(Key.Escape)){ExitRequested?.Invoke();return;}
        if(Press(Key.R) || (Press(Key.Enter)&&state!="playing"&&state!="boss_fight")){
            if(state=="between_levels")LoadLevel(level+1);else LoadLevel(0,true);resultSent=false;return;
        }
        if(state!="playing"&&state!="boss_fight")return;
        if(Press(Key.Q)&&overclocks>0&&overclockTime<=0){overclocks--;overclockTime=8;Sound("level");}
        if(Press(Key.E)&&antiviruses>0&&antivirusTime<=0){antiviruses--;antivirusTime=5;Sound("level");}
        if((Press(Key.LeftShift)||Press(Key.RightShift))&&shields>0&&shieldTime<=0){shields--;shieldTime=5;}
        if(Press(Key.Space))DropBomb();if(Press(Key.F))Attack();
        var step=Vector2Int.zero;
        if(Press(Key.W)||Press(Key.UpArrow))step=Vector2Int.down;
        else if(Press(Key.S)||Press(Key.DownArrow))step=Vector2Int.up;
        else if(Press(Key.A)||Press(Key.LeftArrow))step=Vector2Int.left;
        else if(Press(Key.D)||Press(Key.RightArrow))step=Vector2Int.right;
        if(step!=Vector2Int.zero && Walkable(robot+step)){facing=step;robot+=step;Collect();HitCheck();}
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);elapsed+=dt;
        invuln=Mathf.Max(0,invuln-dt);shieldTime=Mathf.Max(0,shieldTime-dt);
        overclockTime=Mathf.Max(0,overclockTime-dt);antivirusTime=Mathf.Max(0,antivirusTime-dt);
        attackCooldown=Mathf.Max(0,attackCooldown-dt);attackFlash=Mathf.Max(0,attackFlash-dt);
        mineClock+=dt;if(mineClock>=Mathf.Max(3.8f,7-level*1.1f)){mineClock=0;SpawnMine();}
        for(int i=explosions.Count-1;i>=0;i--){explosions[i].timer-=dt;if(explosions[i].timer<=0)explosions.RemoveAt(i);}
        for(int i=bombs.Count-1;i>=0;i--){bombs[i].timer-=dt;if(bombs[i].timer<=0){var cell=bombs[i].cell;bombs.RemoveAt(i);Explode(cell);}}
        moveClock+=dt;if(moveClock>=.38f-level*.075f){moveClock=0;
            if(antivirusTime<=0)for(int i=0;i<bugs.Count;i++){var old=bugs[i];bugs[i]=BugStep(old,previous[i],i);previous[i]=old;}
            if(bossActive){var next=Shortest(boss,robot);if(next!=robot)boss=next;}HitCheck();
        }
    }
    void Collect()
    {
        for(int i=0;i<Items[level].Length;i++)if(robot==Items[level][i]&&picked.Add(robot)){
            points+=100;Sound("collect");if(picked.Count==Items[level].Length){points+=250;Sound("level");
                if(level<2)state="between_levels";else{bossActive=true;boss=P(17,13);bossHP=5;state="boss_fight";}}
        }
        if(bombItems.Remove(robot)){ammo++;points+=50;Sound("collect");}
        if(shieldItems.Remove(robot)){shields++;points+=50;Sound("level");}
        if(powers.TryGetValue(robot,out int type)){powers.Remove(robot);if(type==0)overclocks++;else antiviruses++;points+=75;Sound("level");}
    }
    void Attack()
    {
        if(attackCooldown>0)return;attackCooldown=overclockTime>0?.38f:.55f;attackFlash=.18f;
        var target=robot+facing;
        if(bossActive&&target==boss){bossHP--;points+=200;Sound("hit");if(bossHP<=0){bossActive=false;state="won";points+=1000;Finish(true);}return;}
        int i=bugs.IndexOf(target);if(i>=0){bugs.RemoveAt(i);previous.RemoveAt(i);points+=125;Sound("hit");}
    }
    void DropBomb(){if(ammo<=0)return;foreach(var b in bombs)if(b.cell==robot)return;ammo--;bombs.Add(new TimedCell(robot,1));Sound("bomb");}
    void SpawnMine()
    {
        var choices=new List<Vector2Int>();for(int y=0;y<H;y++)for(int x=0;x<W;x++){
            var p=P(x,y);if(!Walkable(p)||p==robot||bugs.Contains(p)||bombItems.Contains(p)||shieldItems.Contains(p)||Items[level].Length>0&&Array.IndexOf(Items[level],p)>=0)continue;
            bool occupied=false;foreach(var b in bombs)if(b.cell==p)occupied=true;
            if(!occupied&&Dist(p,robot)>=4)choices.Add(p);
        }
        if(choices.Count>0){bombs.Add(new TimedCell(choices[random.Next(choices.Count)],1.6f));Sound("bomb");}
    }
    void Explode(Vector2Int cell)
    {
        explosions.Add(new TimedCell(cell,.42f));for(int i=bugs.Count-1;i>=0;i--)if(Dist(bugs[i],cell)<=2){bugs.RemoveAt(i);previous.RemoveAt(i);points+=150;}
        if(Dist(robot,cell)<=1&&shieldTime<=0&&invuln<=0)Damage();points+=10;Sound("bomb");
        if(bugs.Count==0&&state=="playing"&&Walkable(P(17,13))){bugs.Add(P(17,13));previous.Add(P(17,13));}
    }
    void Damage(){lives--;Sound("hit");invuln=2;robot=P(1,1);if(lives<=0){state="lost";Finish(false);}}
    void HitCheck(){if((state=="playing"||state=="boss_fight")&&invuln<=0&&shieldTime<=0&&(bugs.Contains(robot)||bossActive&&boss==robot))Damage();}
    void Finish(bool won){if(resultSent)return;resultSent=true;Finished?.Invoke(won,points);}
    static int Dist(Vector2Int a,Vector2Int b)=>Mathf.Abs(a.x-b.x)+Mathf.Abs(a.y-b.y);
    Vector2Int BugStep(Vector2Int cur,Vector2Int prev,int i)
    {
        var choices=new List<Vector2Int>();foreach(var d in Dirs)if(Walkable(cur+d))choices.Add(cur+d);
        if(choices.Count==0)return cur;
        if(i==0)return Shortest(cur,robot);
        if(i==1){var target=robot+facing*(2+level);return Shortest(cur,Walkable(target)&&random.NextDouble()<.75?target:robot);}
        if(i==2){choices.Remove(prev);if(choices.Count==0)return prev;return choices[random.Next(choices.Count)];}
        if(i==3){var flank=robot+P(-facing.y,facing.x)*2;return Shortest(cur,Walkable(flank)?flank:robot);}
        return random.NextDouble()<.88?Shortest(cur,robot):choices[random.Next(choices.Count)];
    }
    Vector2Int Shortest(Vector2Int start,Vector2Int goal)
    {
        var q=new Queue<Vector2Int>();var parent=new Dictionary<Vector2Int,Vector2Int>();q.Enqueue(start);parent[start]=start;
        while(q.Count>0){var p=q.Dequeue();if(p==goal)break;foreach(var d in Dirs){var n=p+d;if(Walkable(n)&&!parent.ContainsKey(n)){parent[n]=p;q.Enqueue(n);}}}
        if(!parent.ContainsKey(goal))return start;var node=goal;while(node!=start&&parent[node]!=start)node=parent[node];return node;
    }
    void RectFill(float x,float y,float w,float h,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),pixel);GUI.color=old;}
    void Circle(float x,float y,float radius,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(x-radius,y-radius,radius*2,radius*2),disc);GUI.color=old;}
    void Text(float x,float y,float w,float h,string value,int size,Color c,bool centered=false){var style=centered?centerStyle:size>=26?titleStyle:size>=16?labelStyle:smallStyle;style.fontSize=size;style.normal.textColor=c;GUI.Label(new Rect(x,y,w,h),value,style);}
    Vector2 Center(Vector2Int p)=>new Vector2(46+p.x*Tile,160+p.y*Tile);
    void OnGUI()
    {
        // Uma tela IMGUI em overlay: não cria Camera, Canvas ou EventSystem adicionais.
        var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1120f,Screen.height/760f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1120*scale)*.5f,(Screen.height-760*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
        RectFill(0,0,1120,760,Bg);
        for(int x=0;x<1120;x+=28)RectFill(x,0,1,760,new Color(.18f,.34f,.52f,.11f));
        for(int y=0;y<760;y+=28)RectFill(0,y,1120,1,new Color(.18f,.34f,.52f,.11f));
        RectFill(18,120,710,555,Hex("0b1429"));RectFill(18,120,710,2,Hex("1b4964"));
        Text(28,14,600,44,"PAC-CODE",34,Cyan);Text(30,61,680,25,"dp  •  DISPLAY LABORATÓRIO DE TECNOLOGIA E JOGOS",14,Hex("b8d5e8"));
        Text(30,90,690,30,"FASE "+(level+1)+"/3  •  "+new[]{"INICIANTE","INTERMEDIARIA","AVANCADA"}[level],18,Hex("ffd05c"));
        for(int y=0;y<H;y++)for(int x=0;x<W;x++){
            float px=28+x*Tile,py=142+y*Tile;
            if(Maps[level][y][x]=='#'){RectFill(px+1,py+1,34,34,Hex("193b68"));RectFill(px+3,py+3,30,2,Hex("234f77"));RectFill(px+4,py+4,28,4,Hex("54b9d4"));}
            else{RectFill(px+1,py+1,34,34,Hex("101b35"));Circle(px+18,py+18,2,Hex("28516a"));}
        }
        for(int i=0;i<Items[level].Length;i++)if(!picked.Contains(Items[level][i])){
            var c=Center(Items[level][i]);Circle(c.x,c.y,14,new Color(.2f,.9f,.85f,.22f));RectFill(c.x-11,c.y-10,22,20,Hex("3b9aa7"));
            Text(c.x-13,c.y-8,26,20,ItemNames[i].Substring(0,Mathf.Min(2,ItemNames[i].Length)),10,White,true);
        }
        foreach(var p in bombItems){var c=Center(p);Circle(c.x,c.y,10,Hex("252b3b"));Circle(c.x-3,c.y-3,3,Hex("8795aa"));}
        foreach(var p in shieldItems){var c=Center(p);Circle(c.x,c.y,13,Hex("5c9dff"));Text(c.x-10,c.y-11,20,22,"+",22,White,true);}
        foreach(var entry in powers){var c=Center(entry.Key);var color=entry.Value==0?Hex("ffd05c"):Green;Circle(c.x,c.y,13+Mathf.Sin(elapsed*5)*2,color);Text(c.x-10,c.y-11,20,22,entry.Value==0?"O":"A",15,Bg,true);}
        foreach(var b in bombs){var c=Center(b.cell);Circle(c.x,c.y,17,new Color(1,.25f,.22f,.2f));Circle(c.x,c.y,10,Hex("ff665c"));Circle(c.x-3,c.y-3,3,Hex("ffe4b0"));}
        foreach(var b in explosions){var c=Center(b.cell);Circle(c.x,c.y,43*(1-b.timer/.42f*.35f),new Color(1,.48f,.12f,b.timer/.42f*.45f));}
        var bugColors=new[]{Hex("ff587d"),Hex("b987ff"),Hex("ff7855"),Hex("70f0ae"),Hex("ffd05c")};
        for(int i=0;i<bugs.Count;i++){var c=Center(bugs[i]);Circle(c.x,c.y,15,bugColors[Mathf.Min(i,4)]);RectFill(c.x-9,c.y-5,6,5,White);RectFill(c.x+3,c.y-5,6,5,White);}
        if(bossActive){var c=Center(boss);Circle(c.x,c.y,30,new Color(.75f,.25f,1,.2f));Circle(c.x,c.y,22,Hex("7d35b5"));Circle(c.x-7,c.y-3,5,Hex("ff547d"));Circle(c.x+7,c.y-3,5,Hex("ff547d"));}
        if(shieldTime>0||invuln<=0||Mathf.FloorToInt(elapsed*8)%2==0){var c=Center(robot);Circle(c.x,c.y,25,new Color(.25f,1,.9f,.12f));Circle(c.x,c.y,15,Hex("46d4cc"));RectFill(c.x-10,c.y-7,20,12,Hex("163450"));RectFill(c.x-7,c.y-4,14,5,Hex("a7ffff"));if(shieldTime>0)Circle(c.x,c.y,19,new Color(.4f,.6f,1,.25f));}
        if(attackFlash>0){var c=Center(robot+facing);Circle(c.x,c.y,12,Hex("ffd36b"));}
        RectFill(755,125,340,620,Hex("101a32"));RectFill(755,125,340,2,Hex("28516d"));
        Text(780,143,300,30,"CENTRAL DP DISPLAY",20,Cyan);Text(780,187,300,25,"MISSÃO: montar o computador",16,White);
        Text(780,229,300,30,"COMPONENTES",18,Hex("ffd05c"));
        for(int i=0;i<Items[level].Length;i++)Text(785,263+i*29,280,25,(picked.Contains(Items[level][i])?"✓  ":"○  ")+ItemNames[i],16,picked.Contains(Items[level][i])?Green:White);
        if(bossActive){Text(780,468,300,23,"ENERGIA DO SUPER VÍRUS",13,Purple);RectFill(780,498,270,10,Hex("302442"));RectFill(780,498,270*bossHP/5f,10,Purple);}
        Text(780,515,300,30,"VIDAS: "+new string('♥',Mathf.Max(0,lives)),18,Hex("ff8797"));
        Text(780,547,300,30,"PONTOS: "+points,18,Hex("ffd05c"));
        Text(780,580,300,25,"BOMBAS: "+ammo+"  [ESPAÇO]",15,White);
        Text(780,608,300,25,"ESCUDO: "+shields+"  [SHIFT]",15,Hex("8abaff"));
        Text(780,636,300,25,"OVERCLOCK: "+overclocks+"  [Q]",14,Hex("ffd05c"));
        Text(780,663,300,25,"ANTIVÍRUS: "+antiviruses+"  [E]",14,Green);
        Text(780,697,305,20,"WASD/SETAS: MOVER | F: ATACAR",12,White);
        Text(780,723,305,20,"R: REINICIAR | ESC: SAIR",12,White);
        if(shieldTime>0)Text(945,184,145,20,"ESCUDO "+shieldTime.ToString("F1")+"s",12,Hex("8abaff"));
        if(overclockTime>0)Text(945,206,145,20,"OVERCLOCK "+overclockTime.ToString("F1")+"s",12,Hex("ffd05c"));
        if(antivirusTime>0)Text(945,228,145,20,"ANTIVÍRUS "+antivirusTime.ToString("F1")+"s",12,Green);
        if(state=="boss_fight"){RectFill(165,12,650,76,Hex("180d2b"));Text(205,20,600,34,"SUPER VÍRUS DETECTADO!",23,Purple);Text(205,53,600,24,"Aproxime-se e pressione F para atacar",14,White);}
        if(state=="between_levels")Overlay("FASE CONCLUÍDA!","Pressione ENTER para avançar",Green);
        if(state=="won")Overlay("PAC-CODE CONCLUÍDO!","Você derrotou o SUPER VÍRUS! Pontos: "+points,Green);
        if(state=="lost")Overlay("FIM DE JOGO!","Pressione R para tentar novamente. Pontos: "+points,Hex("ff8c9d"));
        GUI.matrix=matrix;
    }
    void Overlay(string heading,string sub,Color tint){RectFill(165,290,650,145,Hex("071125"));RectFill(175,300,630,125,Hex("173c50"));Text(210,327,570,40,heading,29,tint,true);Text(190,377,600,30,sub,16,White,true);}
    void OnDestroy(){if(pixel)Destroy(pixel);if(disc)Destroy(disc);}
}
