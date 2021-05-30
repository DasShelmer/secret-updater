using System.IO;
using IniParser;
using IniParser.Exceptions;
using IniParser.Model;

namespace SUpdater
{
    static class Config
    {
        static string configPath = Path.GetFullPath("./config.ini");


        public static string dbPath = null;

        public static void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                var parser = new FileIniDataParser();
                IniData data;
                try
                {
                    data = parser.ReadFile(configPath);
                }
                catch(ParsingException e)
                {
                    throw new FileLoadException("Файл настроек повреждён.", configPath, e);
                }
                var dbP = data["MAIN"]["dbPath"];
                
                dbPath = dbP ?? throw new System.Exception("В файле не указан параметр \"dbPath\" в категории [MAIN].");
            }
            else
            {
                throw new FileNotFoundException("Файл настроек не найден.", configPath);
            }
        }
    }
}
