using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.IO;
using System;
using Terraria.ModLoader.Config;
using MonoMod.Utils;
using Terraria.ID;
using System.Linq.Expressions;
using Humanizer;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Stubble.Core.Exceptions;
using Steamworks;

namespace Terrafish.Players
{
    public class Fish : ModPlayer
    {
        public int ticksAlive = 0;
        Tile[,] tileMap;
        int screenTileWidth = 0, screenTileHeight = 0;
        int focusState;
        List<Func<Workstate>> ToDoList;
        TileCoord tileTarget;
        Vector2 spawn;
        Dictionary<string, List<bool>> controlBuff;

        public override void OnEnterWorld()
        {
            // This triggers specifically when the player's character physicaly enters the world
            ticksAlive = 0;
            tileMap = new Tile[Main.maxTilesX, Main.maxTilesY];
            screenTileWidth = Main.screenWidth / 16 - 1;
            screenTileHeight = Main.screenHeight / 16 - 1;
            focusState = 0;
            ToDoList = new();
            controlBuff = new(){
                {"right", [false]},
                {"left", [false]},
                {"jump", [false]},
                {"down", [false]},
                {"hook", [false]},
                {"inv", [false]},
                {"map", [false]},
                {"mount", [false]},
                {"quickHeal", [false]},
                {"quickMana", [false]},
                {"throw", [false]},
                {"torch", [false]},
                {"up", [false]},
                {"useItem", [false]},
                {"useTile", [false]},
                {"smart", [false]}
            };
        }
        public override void SetControls()
        {
            base.SetControls();
            var system = ModContent.GetInstance<Terrafish.Systems.FocusChecker>();
            if (focusState == 2) {
                Player.controlInv = true;
                system.focused = true;
                focusState = 0;
            }
            if (focusState == 1)
            {
                Player.controlInv = false;
                focusState = 2;
            }
            if (ticksAlive <= 1)
                spawn = TileizeWorldCoord(Player.Bottom);
            if (Main.playerInventory)
                return;
            UpdateMap();
            if (ToDoList.Count > 0) {
                Workstate status = ToDoList[0]();
                if (Workstate.Done == status) {
                    ToDoList.RemoveAt(0);
                }
            }
            if (0 == ToDoList.Count) {
                //ToDoList.Add(() => MineClosestTreeBase());
                ToDoList.Add(() => BuildShoebox());
            }
            SetPlayerControls();
            if (!system.focused && 0 == focusState)
            {
                Player.controlInv = true;
                focusState = 1;
                Main.NewText("Toggled active");
            }
            // if (Main.SmartCursorShowing) {
            //     Player.controlSmart = true;
            // } else
            // {
            //     Player.controlSmart = false;
            // }
            Main.SmartCursorWanted_Mouse = false;
            Main.SmartCursorWanted_GamePad = false;
            // Player.controlSmart = false;
        }

        public override void PostUpdate()
        {
            ticksAlive++;
            Player.statLifeMax2 = 2000;
            if (Player.statLife < 1000)
            {
                Player.statLife = 1000;
            } else
            {
                Player.statLife++;
            }
            if (0 == ticksAlive % 60)
            {
                int x = GetMouseTilePos()[0], y = GetMouseTilePos()[1];
                Color tileLighting = Lighting.GetColor(x, y);
                Main.NewText("[" + x.ToString() + ", " + y.ToString() + "]; " + Main.tile[x, y] + "; " + Lighting.GetColor(x, y).ToString() + "; " + GetTileBrightness(x, y).ToString() + "; " + Distance(new((int) Player.Center.X / 16, (int) Player.Center.Y / 16), new((int) Main.MouseWorld.X / 16, (int) Main.MouseWorld.Y / 16)).ToString(), Color.Green);
            }
        }

        public void SetPlayerControls()
        {
            foreach (KeyValuePair<string, List<bool>> control in controlBuff)
            {
                if (controlBuff[control.Key].Count == 0)
                    controlBuff[control.Key] = [false];
            }
            Player.controlRight = controlBuff["right"][0];
            Player.controlLeft = controlBuff["left"][0];
            Player.controlDown = controlBuff["down"][0];
            Player.controlUp = controlBuff["up"][0];
            Player.controlJump = controlBuff["jump"][0];
            Player.controlHook = controlBuff["hook"][0];
            // Player.controlInv = controlBuff["inv"][0];
            Player.controlMap = controlBuff["map"][0];
            Player.controlMount = controlBuff["mount"][0];
            Player.controlTorch = controlBuff["torch"][0];
            Player.controlThrow = controlBuff["throw"][0];
            Player.controlUseItem = controlBuff["useItem"][0];
            Player.controlUseTile = controlBuff["useTile"][0];
            Player.controlQuickHeal = controlBuff["quickHeal"][0];
            Player.controlQuickMana = controlBuff["quickMana"][0];
            Player.controlSmart = controlBuff["smart"][0];
            foreach (string key in controlBuff.Keys)
            {
                controlBuff[key].RemoveAt(0);
            }
        }

        public void Win() {
            ToDoList.Add(() => BuildShoebox());
        }

        public Workstate BuildShoebox() {
            if (!Player.HasItem(ItemID.Wood) || Player.CountItem(ItemID.Wood) < 100) {
                Workstate status = MineClosestTreeBase();
                if (Workstate.Failed == status)
                {
                    Goto(new TileCoord(spawn));
                    return Workstate.Working;
                }
                return status;
            }
            return Goto(new TileCoord(spawn));
        }

        public Workstate Goto(TileCoord coord) {
            TileCoord ppos = Playerpos();
            if (Distance(ppos, coord) <= 2)
                return Workstate.Done;
            if (coord.x > ppos.x) {
                controlBuff["right"].Add(true);
            } else
            {
                controlBuff["left"].Add(true);
            }
            if (coord.y < ppos.y)
            {
                Jump();
            } else
            {
                controlBuff["down"].Add(true);
            }
            return Workstate.Working;
        }

        public void Jump()
        {
            if (controlBuff["jump"].Count == 0)
            {
                controlBuff["jump"].Add(false);
                for (int i = 0; i < 9; i++)
                {
                    controlBuff["jump"].Add(true);
                }
            }
        }

        public TileCoord Playerpos() {
            return new TileCoord(TileizeWorldCoord(Player.Bottom));
        }

        // public Workstate GetResource(int id, int count) {
        // }

        public void UpdateMap()
        {
            int[] screenTilePos = GetScreenTilePos();
            for (int x = 0; x < screenTileWidth; x++)
            {
                for (int y = 0; y < screenTileHeight; y++)
                {
                    int[] realPos = [screenTilePos[0] + x, screenTilePos[1] + y];
                    Tile tile = Main.tile[realPos[0], realPos[1]];
                    float brightness = GetTileBrightness(realPos[0], realPos[1]);
                    if (brightness > 0.10)
                    {
                        tileMap[realPos[0], realPos[1]] = tile;
                    }
                }
            }
        }

        public float GetTileBrightness(int x, int y)
        {
            Color tileLighting = Lighting.GetColor(x, y);
            float brightness = (tileLighting.R + tileLighting.G + tileLighting.B) / 3f / 255f;
            return brightness;
        }

        public int[] GetScreenTilePos()
        {
            return [(int) (Main.screenPosition.X / 16) + 1, (int) (Main.screenPosition.Y / 16) + 1];
        }

        public int[] GetMouseTilePos()
        {
            return [(int) Main.MouseWorld.X / 16, (int) Main.MouseWorld.Y / 16];
        }

        public bool MouseOverTile(int x, int y)
        {
            int[] screenTilePos = GetScreenTilePos();
            int targetMouseX = (int) (16 * (x - screenTilePos[0]) + (16 - Main.screenPosition.X % 16));
            int targetMouseY = (int) (16 * (y - screenTilePos[1]) + (16 - Main.screenPosition.Y % 16));
            if (!(x >= screenTilePos[0] && x <= screenTilePos[0] + screenTileWidth && y >= screenTilePos[1] && y <= screenTilePos[1] + screenTileHeight))
            {
                // tile out of bounds
                Main.NewText("MousePos Target: " + targetMouseX.ToString() + ", " + targetMouseY.ToString());
                return false;
            }
            Main.mouseX = targetMouseX;
            Main.mouseY = targetMouseY;
            return true;
        }

        public Workstate MineTile(int x, int y)
        {
            bool isTileOnScreen = MouseOverTile(x, y);
            if (!isTileOnScreen) {
                // Main.NewText(x.ToString() + ", " + y.ToString());
                controlBuff["useItem"].Add(false);
                return Workstate.Failed;
            }
            SelectTool(GetTileID(new(x, y)));
            SelectItemByType(ItemType.Pickaxe);
            if (!Main.tile[x, y].HasTile)
            {
                controlBuff["useItem"].Add(false);
                return Workstate.Done;
            }
            controlBuff["useItem"].Add(true);
            return Workstate.Working;
        }

        public Workstate MineTile(TileCoord coord)
        {
            if (!coord.valid)
            {
                controlBuff["useItem"].Add(false);
                Main.NewText("Invalid coord: " + coord.ToString());
                return Workstate.Failed;
            }
            int x = coord.x, y = coord.y;
            return MineTile(x, y);
        }

        public bool SelectTool(TileID id)
        {
            if (new List<int>{5}.Contains(id)) {
                Player.SelectI
            }
        }

        public bool SelectItemByType(ItemType type)
        {
            if (Player.selectedItem != 1)
            {
                Player.ScrollHotbar(1 - Player.selectedItem);
            }
            return true;
        }

        public TileCoord SearchForClosestTile(TileRef tileRef, int maxDistance=-1)
        {
            if (-1 == maxDistance) maxDistance = Main.maxTilesX;
            int playerX = (int) Player.Bottom.X / 16;
            int playerY = (int) Player.Bottom.Y / 16;
            TileCoord playerPos = new(playerX, playerY);
            TileCoord tileCoords = new(9999, 9999, false);
            int distance = 0;
            int id = tileRef.ID;
            int fX = tileRef.fX;
            int fY = tileRef.fY;
            while (!tileCoords.valid && distance < maxDistance)
            {
                int yTop = Math.Max(playerY - distance, 0);
                int yBottom = Math.Min(playerY + distance, Main.maxTilesY - 1);
                for (int x = -distance; x < distance; x++)
                {
                    if (playerX + x < 0) continue;
                    if (playerX + x >= Main.maxTilesX) break;
                    TileCoord topCoord = new(playerX + x, yTop);
                    TileCoord bottomCoord = new(playerX + x, yBottom);
                    Tile topTile = tileMap[topCoord.x, topCoord.y];
                    Tile bottomTile = tileMap[bottomCoord.x, bottomCoord.y];
                    if (id == bottomTile.TileType && (-9999 == fX || fX == bottomTile.TileFrameX) && (-9999 == fY || fY == bottomTile.TileFrameY))
                    {
                        tileCoords = Closest(playerPos, tileCoords, bottomCoord);
                    }
                    if (id == topTile.TileType && (-9999 == fX || fX == topTile.TileFrameX) && (-9999 == fY || fY == topTile.TileFrameY))
                    {
                        tileCoords = Closest(playerPos, tileCoords, topCoord);
                    }
                }
                int xLeft = Math.Max(playerX - distance, 0);
                int xRight = Math.Min(playerX + distance, Main.maxTilesX - 1);
                for (int y = -distance; y < distance; y++)
                {
                    if (playerY + y < 0) continue;
                    if (playerY + y >= Main.maxTilesY) break;
                    TileCoord leftCoord = new(xLeft, playerY + y);
                    TileCoord rightCoord = new(xRight, playerY + y);
                    Tile leftTile = tileMap[leftCoord.x, leftCoord.y];
                    Tile rightTile = tileMap[rightCoord.x, rightCoord.y];
                    if (id == leftTile.TileType && (-9999 == fX || fX == leftTile.TileFrameX) && (-9999 == fY || fY == leftTile.TileFrameY))
                    {
                        tileCoords = Closest(playerPos, tileCoords, leftCoord);
                    }
                    if (id == rightTile.TileType && (-9999 == fX || fX == rightTile.TileFrameX) && (-9999 == fY || fY == rightTile.TileFrameY))
                    {
                        tileCoords = Closest(playerPos, tileCoords, rightCoord);
                    }
                }
                distance++;
                if (playerX + distance > Main.maxTilesX && playerX - distance < 0) break;
            }
            return tileCoords;
        }

        public List<TileCoord> GetTilesInRing(TileCoord center, int distance)
        {
            List<TileCoord> output = [];

            int yTop = Math.Max(center.y - distance, 0);
            int yBottom = Math.Min(center.y + distance, Main.maxTilesY - 1);
            for (int x = -distance; x < distance; x++)
            {
                if (center.x + x < 0) continue;
                if (center.x + x >= Main.maxTilesX) break;
                TileCoord topCoord = new(center.x + x, yTop);
                TileCoord bottomCoord = new(center.x + x, yBottom);
                Tile topTile = tileMap[topCoord.x, topCoord.y];
                Tile bottomTile = tileMap[bottomCoord.x, bottomCoord.y];
                output.Add(topCoord);
                output.Add(bottomCoord);
            }
            int xLeft = Math.Max(center.x - distance, 0);
            int xRight = Math.Min(center.x + distance, Main.maxTilesX - 1);
            for (int y = -distance + 1; y < distance - 1; y++)
            {
                if (center.y + y < 0) continue;
                if (center.y + y >= Main.maxTilesY) break;
                TileCoord leftCoord = new(xLeft, center.y + y);
                TileCoord rightCoord = new(xRight, center.y + y);
                Tile leftTile = tileMap[leftCoord.x, leftCoord.y];
                Tile rightTile = tileMap[rightCoord.x, rightCoord.y];
                output.Add(leftCoord);
                output.Add(rightCoord);
            }

            return output;
        }

        public Workstate MineClosest(TileRef tileRef)
        {
            TileCoord coord = SearchForClosestTile(tileRef, screenTileWidth / 2);
            if (!coord.valid) return Workstate.Failed;
            Workstate status = MineTile(coord);
            if (Workstate.Failed == status)
            {
                return status;
            } else
            {
                return status;
            }
            return status;
        }

        public Workstate MineClosestTreeBase()
        {
            TileRef[] treeBaseRefs = [
                new(5),
            ];
            TileCoord closest = new();
            TileCoord playerPos = new(TileizeWorldCoord(Player.Bottom));
            foreach (TileRef tref in treeBaseRefs)
            {
                TileCoord coord = SearchForClosestTile(tref, screenTileWidth / 2);
                if (coord.valid)
                {
                    closest = Closest(playerPos, coord, closest);
                }
            }
            /*if (ticksAlive % 60 == 30) {
                Main.NewText(closest.ToString());
            }*/
            HashSet<TileCoord> tree = GetVein(closest, []);
            BBox treeBBox = new(tree);
            if (ticksAlive % 60 == 15) {
                Main.NewText(new TileCoord((int) treeBBox.Median().x, treeBBox.Bot()), Color.Red);
                Main.NewText(tree.Count.ToString() + "; " + new TileCoord(treeBBox.x, treeBBox.y).ToString() + "; " + treeBBox.width.ToString() + ", " + treeBBox.height.ToString());
            }
            TileCoord treeStump = new TileCoord((int) treeBBox.Median().x, treeBBox.Bot());
            Goto(treeStump);
            return MineTile(treeStump);
            //return MineTile(closest);
        }

        public double Distance(TileCoord v, TileCoord u)
        {
            return Math.Sqrt(Math.Pow(v.x - u.x, 2) + Math.Pow(v.y - u.y, 2));
        }

        public TileCoord Closest(TileCoord start, TileCoord v, TileCoord u)
        {
            return (Distance(start, v) < Distance(start, u)) ? v : u;
        }

        public Vector2 TileizeWorldCoord(Vector2 coord)
        {
            return new Vector2(coord.X / 16, coord.Y / 16);
        }

        public int GetTileID(TileCoord coord) {
            int x = coord.x, y = coord.y;
            return tileMap[x, y].TileType;
        }


        public class TileInfo
        {
            public int type, fX, fY, blockType, liquidAmount, liquidType, wallType, wallfX, wallfY;
            public bool hasTile, isActuated, isSolid;
            public int[] lighting = new int[3];
        }
        
        public enum ItemType
        {
            Pickaxe
        }
        
        public class TileCoord
        {
            public int x, y;
            public bool valid;
            public TileCoord()
            {
                x = -999999;
                y = -999999;
                valid = false;
            }
        
            public TileCoord(int X, int Y, bool isValid=true)
            {
                x = X;
                y = Y;
                valid = isValid;
            }
        
            public TileCoord(Vector2 pos, bool isValid=true)
            {
                x = (int) pos.X;
                y = (int) pos.Y;
                valid = isValid;
            }
        
            public override string ToString()
            {
                return string.Format("({0}, {1}, {2})", x, y, valid);
            }

            public override bool Equals(object obj) {
                TileCoord other = obj as TileCoord;
                return other != null && this.x == other.x && this.y == other.y;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.x, this.y);
            }
        
            public void Add(TileCoord other) {
                this.x += other.x;
                this.y += other.y;
            }
        
            public void Sub(TileCoord other) {
                this.x -= other.x;
                this.y -= other.y;
            }
        
            public void Div(double num) {
                this.x = (int) (this.x / num);
                this.y = (int) (this.y / num);
            }
        
            public void Mul(double num) {
                this.x = (int) (this.x * num);
                this.y = (int) (this.y * num);
            }
        
            public TileCoord Plus(TileCoord other) {
                return new(this.x + other.x, this.y + other.y);
            }
        
            public TileCoord Minus(TileCoord other) {
                return new(this.x - other.x, this.y - other.y);
            }
        }
        
        public class TileRef
        {
            public int ID, fX, fY;
            public TileRef()
            {
                ID = 0;
                fX = -9999;
                fY = -9999;
            }
        
            public TileRef(int id)
            {
                ID = id;
                fX = -9999;
                fY = -9999;
            }
            public TileRef(int id, int fx, int fy)
            {
                ID = id;
                fX = fx;
                fY = fy;
            }
        }
        
        public enum Workstate
        {
            Failed,
            Working,
            Done,
            NA
        }
        
        public class BBox
        {
            public int x, y, width, height;
            public HashSet<TileCoord> tiles;
        
            public BBox() {
                this.x = this.y = this.width = this.height = 0;
                this.tiles = [];
            }
        
            public BBox(TileCoord corner1, TileCoord corner2) {
                this.x = Math.Min(corner1.x, corner2.x);
                this.y = Math.Min(corner1.y, corner2.y);
                this.width = Math.Abs(corner1.x - corner2.x);
                this.height = Math.Abs(corner1.y - corner2.y);
                this.tiles = [corner1, corner2];
            }
        
            public BBox(HashSet<TileCoord> tileSet): this() {
                this.width = this.height = 0;
                this.x = 999999;
                this.y = 999999;
                int maxX = 0;
                int maxY = 0;
                foreach (TileCoord coord in tileSet) {
                    this.x = Math.Min(this.x, coord.x);
                    this.y = Math.Min(this.y, coord.y);
                    maxX = Math.Max(maxX, coord.x);
                    maxY = Math.Max(maxY, coord.y);
                    this.tiles.Add(coord);
                }
                this.width = maxX - this.x;
                this.height = maxY - this.y;
            }
        
            public void Add(TileCoord coord) {
                this.x = Math.Min(this.x, coord.x);
                this.y = Math.Min(this.y, coord.y);
                this.width = Math.Max(this.width, coord.x - this.x);
                this.height = Math.Max(this.height, coord.y - this.y);
                this.tiles.Add(coord);
            }
        
            public TileCoord Center() {
                return new TileCoord(x + (width / 2), y + (height / 2));
            }

            public TileCoord Median() {
                List<int> xVals = new();
                List<int> yVals = new();
                foreach (TileCoord coord in this.tiles) {
                    xVals.Add(coord.x);
                    yVals.Add(coord.y);
                }
                xVals.Sort();
                yVals.Sort();
                return new TileCoord(xVals[(int) xVals.Count / 2], yVals[(int) yVals.Count / 2]);
            }

            public Vector2 Mean() {
                Vector2 mean = new();
                foreach (TileCoord coord in this.tiles) {
                    mean.X += coord.x;
                    mean.Y += coord.y;
                }
                mean.X /= this.tiles.Count;
                mean.Y /= this.tiles.Count;
                return mean;
            }
        
            public int Right() {
                return this.x + this.width;
            }
        
            public int Bot() {
                return this.y + this.height;
            }
        }
        
        public HashSet<TileCoord> GetVein(TileCoord start, HashSet<TileCoord> found) {
            if (found.Count > 0 && found.Contains(start)) return found;
            int id = GetTileID(start);
            if (0 == id || 1 == id || 2 == id) return found;
            found.Add(start);
            TileCoord[] directions = [
                new(1, 0),
                new(0, 1),
                new(-1, 0),
                new(0, -1),
                new(1, 1),
                new(1, -1),
                new(-1, 1),
                new(-1, -1)
            ];
            foreach (TileCoord dir in directions) {
                TileCoord nextCoord = start.Plus(dir);
                if (GetTileID(nextCoord) == id) {
                    found = GetVein(nextCoord, found);
                }
            }
            return found;
        }
    }
}
