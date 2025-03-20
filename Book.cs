using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_bookstoreapi
{
    public class Book
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("title")]
        public string title { get; set; }
        [JsonProperty("author")]
        public string author { get; set; }
        [JsonProperty("genre")] 
        public string genre { get; set; }
        [JsonProperty("price")]
        public double price { get; set; }
        
        [JsonProperty("time")]
        public DateTime timestamp {get; set;}
    }
}
