﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkyBot.Models
{
    public class DiscordMessageModel
    {
        [JsonProperty("_id")]
        public ulong Id { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("author_id")]
        public ulong AuthorId { get; set; }
    }
}
