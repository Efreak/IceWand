﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Hooks;
using Terraria;
using TShockAPI;

namespace IceWand
{
    [APIVersion(1, 12)]
    public class IceWand : TerrariaPlugin
    {
        public delegate void IceWandD(int X, int Y, int data, int plr);

        public List<IceWandAction> Actions = new List<IceWandAction>();
        public int[] ActionData = new int[256];
        public byte[] ActionTypes = new byte[256];
        public override string Author
        {
            get { return "MarioE"; }
        }
        public override string Description
        {
            get { return "Lets the ice rod do virtually anything."; }
        }
        public override string Name
        {
            get { return "IceWand"; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public IceWand(Main game)
            : base(game)
        {
            Order = 10;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
                NetHooks.GetData -= OnGetData;
            }
        }
        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
            NetHooks.GetData += OnGetData;
        }

        void OnGetData(GetDataEventArgs e)
        {
            if (e.MsgID == PacketTypes.Tile && !e.Handled &&
                e.Msg.readBuffer[e.Index] == 1 && e.Msg.readBuffer[e.Index + 9] == 127 && ActionTypes[e.Msg.whoAmI] != 0)
            {
                int X = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 1);
                int Y = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 5);
                TShock.Players[e.Msg.whoAmI].SendTileSquare(X, Y, 1);
                e.Handled = true;
            }
            else if (e.MsgID == PacketTypes.ProjectileDestroy && !e.Handled)
            {
                if (WorldGen.genRand == null)
                {
                    WorldGen.genRand = new Random();
                }

                int ID = TShock.Utils.SearchProjectile(BitConverter.ToInt16(e.Msg.readBuffer, e.Index), e.Msg.readBuffer[e.Index + 2]);
                Projectile p = Main.projectile[ID];
                if (p.type == 80)
                {
                    int X = (int)p.position.X / 16;
                    int Y = (int)p.position.Y / 16;
                    if (p.velocity != Vector2.Zero)
                    {
                        Vector2 normalized = p.velocity;
                        normalized.Normalize();
                        X = (int)Math.Round((X * 16f + 8f - normalized.X) / 16f);
                        Y = (int)Math.Round((Y * 16f + 8f - normalized.Y) / 16f);
                    }
                    if (ActionTypes[e.Msg.whoAmI] != 0)
                    {
                        Actions[ActionTypes[e.Msg.whoAmI]].action.Invoke(X, Y, ActionData[e.Msg.whoAmI], e.Msg.whoAmI);
                        e.Handled = true;
                    }
                }
            }
        }
        void OnInitialize()
        {
            Commands.ChatCommands.Add(new Command("icewand", IceWandCmd, "icewand", "iw"));

            Actions.Add(null);
            Actions.Add(new IceWandAction(Explode, "explode"));
            Actions.Add(new IceWandAction(Lava, "lava"));
            Actions.Add(new IceWandAction(Position, "position"));
            Actions.Add(new IceWandAction(SpawnMob, "spawnmob"));
            Actions.Add(new IceWandAction(Tile, "tile"));
            Actions.Add(new IceWandAction(Teleport, "tp"));
            Actions.Add(new IceWandAction(Wall, "wall"));
            Actions.Add(new IceWandAction(Water, "water"));
        }

        void IceWandCmd(CommandArgs e)
        {
            if (e.Parameters.Count > 2)
            {
                e.Player.SendMessage("Invalid syntax! Proper syntax: /icewand <action> [data]", Color.Red);
                return;
            }
            if (e.Parameters[0].ToLower() == "help" || e.Parameters[0].ToLower() == "list")
            {
                StringBuilder actions = new StringBuilder();
                for (int i = 1; i < Actions.Count; i++)
                {
                    actions.Append(Actions[i].name);
                    if (i != Actions.Count - 1)
                    {
                        actions.Append(", ");
                    }
                }
                e.Player.SendMessage("Ice wand actions: " + actions.ToString(), Color.Yellow);
                return;
            }

            for (int i = 1; i < Actions.Count; i++)
            {
                if (Actions[i].name == e.Parameters[0].ToLower())
                {
                    if (!e.Player.Group.HasPermission("iw." + Actions[i].name) && !e.Player.Group.HasPermission("iw.*"))
                    {
                        e.Player.SendMessage("You do not have access to this action.", Color.Red);
                        return;
                    }
                    if (e.Parameters.Count != 1)
                    {
                        if (!int.TryParse(e.Parameters[1], out ActionData[e.Player.Index]))
                        {
                            e.Player.SendMessage("Invalid data.", Color.Red);
                            return;
                        }
                    }
                    ActionTypes[e.Player.Index] = (byte)i;
                    e.Player.SendMessage("Ice wand action is now: " + Actions[i].name + ".");
                    return;
                }
            }
            e.Player.SendMessage("Invalid ice wand action.", Color.Red);
        }

        void Explode(int X, int Y, int data, int plr)
        {
            int ID = Projectile.NewProjectile(X * 16 + 8, Y * 16 + 8, 0, 0, 108, 250, 10);
            TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", ID);
        }
        void Lava(int X, int Y, int data, int plr)
        {
            Main.tile[X, Y].lava = true;
            Main.tile[X, Y].liquid = 255;
            WorldGen.SquareTileFrame(X, Y);
            TSPlayer.All.SendTileSquare(X, Y, 1);
        }
        void Position(int X, int Y, int data, int plr)
        {
            TShock.Players[plr].SendMessage("Position: " + X + ", " + Y, Color.Yellow);
        }
        void SpawnMob(int X, int Y, int data, int plr)
        {
            int ID = NPC.NewNPC(X * 16, Y * 16, data);
            TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", ID);
        }
        void Teleport(int X, int Y, int dat, int plr)
        {
            TShock.Players[plr].Teleport(X, Y);
        }
        void Tile(int X, int Y, int data, int plr)
        {
            if (data >= 0 && data < Main.maxTileSets)
            {
                Main.tile[X, Y].active = true;
                Main.tile[X, Y].type = (byte)data;
                WorldGen.SquareTileFrame(X, Y);
                TSPlayer.All.SendTileSquare(X, Y, 1);
            }
        }
        void Wall(int X, int Y, int data, int plr)
        {
            if (data > 0 && data < Main.maxWallTypes)
            {
                Main.tile[X, Y].wall = (byte)data;
                WorldGen.SquareWallFrame(X, Y);
                TSPlayer.All.SendTileSquare(X, Y, 1);
            }
        }
        void Water(int X, int Y, int data, int plr)
        {
            Main.tile[X, Y].lava = false;
            Main.tile[X, Y].liquid = 255;
            WorldGen.SquareTileFrame(X, Y);
            TSPlayer.All.SendTileSquare(X, Y, 1);
        }

        public class IceWandAction
        {
            public IceWandD action;
            public string name;

            public IceWandAction(IceWandD callback, string name)
            {
                action = callback;
                this.name = name;
            }
        }
    }
}