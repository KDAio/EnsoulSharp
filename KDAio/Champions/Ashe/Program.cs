using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace KDAAshe
{
    internal class Program{

        public static Font TextBold;
        private static Spell Q,W,E,R;
        public static int Kills = 0;
        private static Menu mainMenu;


        public static void OnGameLoad()
        {
            TextBold = new Font(
    Drawing.Direct3DDevice, new FontDescription
    {
        FaceName = "Tahoma",
        Height = 30,
        Weight = FontWeight.ExtraBold,
        OutputPrecision = FontPrecision.Default,
        Quality = FontQuality.ClearType,
    }
);
            if (GameObjects.Player.CharacterName != "Ashe") return;
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W,1200f);
            W.SetSkillshot(0.25f, 40f, 1500f, true, SpellType.Line);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R,float.MaxValue);
            R.SetSkillshot(0.25f, 260f, 1600f, false, SpellType.Line);


            mainMenu = new Menu("Ashe","[KDA] Ashe",true); //odpowiada za stworzenie menu pod shiftem
            
            var Combo = new Menu("Combo", "[KDA] Combo Settings");//Podkatalog pod shiftem [1 nazwa to odwolanie, druga to nazwa wyswietlana]
            Combo.Add(new MenuBool("Quse", "Use Q", true)); // dodanie nowego boola
            Combo.Add(new MenuBool("Wuse", "Use W", true));
            Combo.Add(new MenuBool("Ruse", "Use R", true));
            Combo.Add(new MenuSlider("Rcount", "^If enemy count is:", 2, 1, 5));
            mainMenu.Add(Combo);

            var Harass = new Menu("Harass", "[KDA] Harass Settings");
            Harass.Add(new MenuBool("Wuse", "Use W", true));
            Harass.Add(new MenuBool("Euse", "Use E", true));
            Harass.Add(new MenuBool("onlyr", "^ Only if target is out of auto attack range", true));
            Harass.Add(new MenuSlider("mana%", "Mana percentage", 50, 0, 100));
            mainMenu.Add(Harass);
            
            var KS = new Menu("KS", "[KDA] Kill Steal Settings");
            KS.Add(new MenuBool("Wuse", "Auto W to kill", true));
            KS.Add(new MenuBool("Ruse", "Auto R to kill", true));
            KS.Add(new MenuSlider("Renemy", "^ Use if less than enemies in range", 1, 1, 5));
            mainMenu.Add(KS);

            var Farm = new Menu("Farm", "[KDA] LaneClear Settings");
            Farm.Add(new MenuBool("Wuse", "Use W", false));
            Farm.Add(new MenuSlider("Eminions", "^ Minions to cast W", 3, 1, 7));
            Farm.Add(new MenuSlider("mana%", "Mana percentage", 50, 0, 100));
            mainMenu.Add(Farm);

            var Jungle = new Menu("Jungle", "[KDA] JungleClear Settings");
            Jungle.Add(new MenuBool("Quse", "Use Q", true));
            Jungle.Add(new MenuBool("Wuse", "Use W", true));
            mainMenu.Add(Jungle);
            

            var Misc = new Menu("Misc", "[KDA] Miscellaneous Settings");
            Misc.Add(new MenuKeyBind("back", "Silent back key", Keys.B, KeyBindType.Press));
            Misc.Add(new MenuSlider("skins", "Set skin", 1, 0, 17));
            Misc.Add(new MenuKeyBind("setskin", "^ Press to set skin", Keys.U, KeyBindType.Press));
            mainMenu.Add(Misc);

            var Draw = new Menu("Draw","[KDA] Draw Settinga");
            Draw.Add(new MenuBool("rangeW","W range",true));
            Draw.Add(new MenuBool("anav","Draw only if spell is ready",true));
            Draw.Add(new MenuBool("Qtime", "Show Q timer", true));
            mainMenu.Add(Draw);

            mainMenu.Attach();
            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Console.Write("[KDAio Loaded] By CnG ");
            Game.Print("<font color = '#00a6d6'>[KDAio]:: <font color = '#FFFF00'>has been loaded! Enjoy.\n");
        }

        public static void SetSkin(){
            if (mainMenu["Misc"].GetValue<MenuKeyBind>("setskin").Active){
                GameObjects.Player.SetSkin(mainMenu["Misc"].GetValue<MenuSlider>("skins").Value);
            }
        }
        public static void SilentBack()
        {
            if (mainMenu["Misc"].GetValue<MenuKeyBind>("back").Active && Q.IsReady())
            {
                Q.Cast();
                DelayAction.Add((int)(Q.Delay+300),()=>ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Recall));
            }
        }
        public static void ComboLogic()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.IsVisible))
            {
                if (target == null) return;
                var input = W.GetPrediction(target);
                if (mainMenu["Combo"].GetValue<MenuBool>("Quse").Enabled && Q.IsReady() && GameObjects.Player.Distance(target.Position) < 1300f)
                {
                    Q.Cast();
                }

                if (mainMenu["Combo"].GetValue<MenuBool>("Wuse").Enabled && W.IsReady() && target.IsValidTarget(W.Range) && input.Hitchance >= HitChance.High && target.IsValidTarget(W.Range))
                {
                    W.Cast(input.UnitPosition);
                }

                if (mainMenu["Combo"].GetValue<MenuBool>("Ruse").Enabled && R.IsReady() && target.IsValidTarget(R.Range))
                {
                    if (ObjectManager.Player.CountEnemyHeroesInRange(R.Range) >= mainMenu["Combo"].GetValue<MenuSlider>("Rcount").Value)
                    {
                        R.Cast();
                    }
                }
            }
        }

        public static void HarassLogic()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var input = W.GetPrediction(target);
            if(target == null) return;
            if (mainMenu["Harass"].GetValue<MenuBool>("Wuse").Enabled)
            {
                if (target.IsValidTarget(W.Range) && W.IsReady() && input.Hitchance >= HitChance.High)
                {
                    W.Cast(input.UnitPosition);
                }
            }

            if (mainMenu["Harass"].GetValue<MenuSlider>("mana%").Value <= GameObjects.Player.ManaPercent)
            {
                if (mainMenu["Harass"].GetValue<MenuBool>("onlyr").Enabled)
                {
                    if (mainMenu["Harass"].GetValue<MenuBool>("Euse").Enabled && (GameObjects.Player.Distance(target.Position) > ObjectManager.Player.AttackRange) && target.IsValidTarget(E.Range) && target.HasBuff("TwitchDeadlyVenom"))
                    {
                        E.Cast();
                    }
                }
                else
                {
                    if (mainMenu["Harass"].GetValue<MenuBool>("Euse").Enabled && target.IsValidTarget(E.Range) && target.HasBuff("TwitchDeadlyVenom"))
                    {
                        E.Cast();
                    }
                }
            }
            
        }

        public static void KSLogic()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x=> x.IsValidTarget(E.Range)))
            {
                if(target==null) return;
                if (mainMenu["KS"].GetValue<MenuBool>("Euse").Enabled && E.IsReady())
                {
                    if (GameObjects.EnemyHeroes.Any(x => E.GetDamage(target) > target.Health - ObjectManager.Player.CalculateDamage(target, DamageType.Mixed, 1)))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void LaneClearLogic()
        {
            int cont = 0;
            foreach (var minion in GameObjects.EnemyMinions.Where(x=> x.IsValidTarget(E.Range)))
            {
                if(minion == null) return;
                
                var Edmg = E.GetDamage(minion);
                if (Edmg > minion.Health - ObjectManager.Player.CalculateDamage(minion, DamageType.Mixed, 1) &&
                    minion.HasBuff("TwitchDeadlyVenom"))
                {
                    cont++;
                }

                if (cont > 0)
                {
                    if (mainMenu["Farm"].GetValue<MenuBool>("Euse").Enabled)
                    {
                        if (cont >= mainMenu["Farm"].GetValue<MenuSlider>("Eminions").Value && E.IsReady())
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void JungleClearLogic()
        {
            var mobs = GameObjects.Jungle.FirstOrDefault(x => x.IsValidTarget(E.Range));
            var inpput = W.GetPrediction(mobs);
            if(mobs == null) return;
            if(mainMenu["Jungle"].GetValue<MenuBool>("Euse").Enabled){
                var Edmg = E.GetDamage(mobs);
                if(Edmg>mobs.Health-ObjectManager.Player.CalculateDamage(mobs,DamageType.Mixed,1)){
                    E.Cast();
                }
            }
            if(mainMenu["Jungle"].GetValue<MenuBool>("Wuse").Enabled){
                if(mobs.IsValidTarget(W.Range) && inpput.Hitchance >= HitChance.High){
                    W.Cast(inpput.UnitPosition);
                }
            }
        }
        private static void OnGameUpdate(EventArgs args){
            if(GameObjects.Player.IsDead) return;
            KSLogic();
            SetSkin();
            SilentBack();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    ComboLogic();
                    break;
                case OrbwalkerMode.Harass:
                    HarassLogic();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClearLogic();
                    JungleClearLogic();
                    break;
                
            }
            
                
            
            
        }
        public static void DrawText(Font fuente, String text, float posx, float posy, SharpDX.ColorBGRA color){
            fuente.DrawText(null,text,(int)posx,(int)posy,color);
        }


        public static double QTime(AIBaseClient target)
        {
            if (target.HasBuff("AsheQBuff"))
            {
                return Math.Max(0, target.GetBuff("AsheQBuff").EndTime) - Game.Time;
            }

            return 0;
        }
        
        
        public static void OnDraw(EventArgs args){
            if(mainMenu["Draw"].GetValue<MenuBool>("lista").Enabled){
                if(mainMenu["Draw"].GetValue<MenuBool>("rangeW").Enabled){
                    if(W.IsReady()){
                        Render.Circle.DrawCircle(GameObjects.Player.Position,W.Range,System.Drawing.Color.Cyan);
                    }
                }
                if(mainMenu["Draw"].GetValue<MenuBool>("rangeE").Enabled){
                    if(E.IsReady()){
                        Render.Circle.DrawCircle(GameObjects.Player.Position,E.Range,System.Drawing.Color.Cyan);
                    }
                }
                if(mainMenu["Draw"].GetValue<MenuBool>("rangeR").Enabled){
                    if(R.IsReady()){
                        Render.Circle.DrawCircle(GameObjects.Player.Position,R.Range,System.Drawing.Color.Cyan);
                    }
                }
            }else{
                if(mainMenu["Draw"].GetValue<MenuBool>("rangeW").Enabled){
                    Render.Circle.DrawCircle(GameObjects.Player.Position,W.Range,System.Drawing.Color.Cyan);
                }
                if(mainMenu["Draw"].GetValue<MenuBool>("rangeE").Enabled){
                    Render.Circle.DrawCircle(GameObjects.Player.Position,E.Range,System.Drawing.Color.Cyan);
                }
                if(mainMenu["Draw"].GetValue<MenuBool>("rangeR").Enabled){
                    Render.Circle.DrawCircle(GameObjects.Player.Position,R.Range,System.Drawing.Color.Cyan);
                }
            }

            if (mainMenu["Draw"].GetValue<MenuBool>("Edmg").Enabled)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.HasBuff("TwitchDeadlyVenom") && (!x.IsDead || x.IsZombie()) && x.IsVisible))
                {
                    var targetpos = target.HPBarPosition;
                    var DamagePorcnt = MathUtil.Clamp((E.GetDamage(target)/target.Health +target.PhysicalShield)*100,0,100);
                    var VectorPos = new Vector2(targetpos.X + 50, targetpos.Y - 45);
                    if (DamagePorcnt != 0)
                    {
                        DrawText(TextBold,Math.Round(DamagePorcnt).ToString()+"%",targetpos.X+50,targetpos.Y-70,(DamagePorcnt>=100)? SharpDX.Color.Green: SharpDX.Color.White);
                    }
                }
            }

            if (mainMenu["Draw"].GetValue<MenuBool>("Edmgmob").Enabled)
            {
                foreach(var mobs in GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range) && x.HasBuff("TwitchDeadlyVenom"))){
                    if(mobs == null ) return;
                    if(mobs.Name.Contains("Dragon") || mobs.Name.Contains("Baron") || mobs.Name.Contains("Herald")){
                        var mobs2 = mobs.Position;
                        var mobpos = mobs.HPBarPosition;
                        var Edmg = E.GetDamage(mobs);
                        var DamagePorcnt = MathUtil.Clamp((Edmg/mobs.Health+mobs.PhysicalShield)*100,0,100);
                        if(DamagePorcnt != 0){
                            DrawText(TextBold,Math.Round(DamagePorcnt).ToString()+"%",mobpos.X+50,mobpos.Y-70,
                                (DamagePorcnt>=100) ? SharpDX.Color.Green : SharpDX.Color.White);
                        }
                    }
                }
            }

            if (mainMenu["Draw"].GetValue<MenuBool>("Qtime").Enabled && GameObjects.Player.HasBuff("AsheQBuff"))
            {
                var PlayerPos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                
                DrawText(TextBold,"Q Time: "+Math.Round(QTime(ObjectManager.Player),MidpointRounding.ToEven),PlayerPos.X,PlayerPos.Y+100,SharpDX.Color.White);
            }
        }
    }
}
