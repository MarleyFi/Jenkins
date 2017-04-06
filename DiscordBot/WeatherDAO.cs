using System;
using System.Linq;

namespace DiscordBot
{
    internal class WeatherDAO
    {
        public int time { get; set; }

        public string summary { get; set; }

        public double temperature { get; set; }

        public double apparentTemperature { get; set; }

        public double windSpeed { get; set; }

        public double cloudCover { get; set; }
    }
}