﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abyss
{
    public class PersistenceGuild
    {
        public ulong Id { get; set; }
        public ulong ActionLogChannelId { get; set; } = 0;
    }
}
