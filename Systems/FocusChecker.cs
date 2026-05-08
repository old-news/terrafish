using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using System.Security.Cryptography.Pkcs;
using Terraria.DataStructures;
using Terraria.ObjectData;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Numerics;
using Humanizer;
using Mono.Cecil.Cil;
using Terraria.GameContent.Bestiary;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.UI;

namespace Terrafish.Systems
{
    public class FocusChecker : ModSystem
    {
        public bool focused = true;
        public override void UpdateUI(GameTime gameTime)
        {
            base.UpdateUI(gameTime);
            if (!Main.instance.IsActive)
            {
                focused = false;
            }
        }
    }
}
