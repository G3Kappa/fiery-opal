﻿using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using static FieryOpal.Src.Procedural.GenUtil;
using Microsoft.Xna.Framework.Input;
using FieryOpal.src;

namespace FieryOpal.Src
{
    public static partial class Util
    {
        public static KeybindConfigInfo LoadDefaultKeyConfig()
        {
            return new KeybindConfigLoader().LoadFile("cfg/keys.cfg");
        }

        public static FontConfigInfo LoadDefaultFontConfig()
        {
            return new FontConfigLoader().LoadFile("cfg/fonts.cfg");
        }

        public static InitConfigInfo LoadDefaultInitConfig()
        {
            return new InitConfigLoader().LoadFile("cfg/init.cfg");
        }

        public static LocalizationInfo LoadDefaultLocalizationConfig(InitConfigInfo init)
        {
            return new LocalizationLoader().LoadFile(init.Locale);
        }
    }

    public static partial class Extensions
    {
    }
}
