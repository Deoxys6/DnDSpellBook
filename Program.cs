namespace ConsoleSpellBook
{
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Text.Json;
    using System;
    using System.Collections.Generic;

    public class ConsoleSpellBook
    {
        //Dictionary that stores the class name and a dictionary of the spellname and URL to get the details of the spell.
        public static Dictionary<string, Dictionary<string, string>> dictOfClasses = new Dictionary<string, Dictionary<string, string>>();
        public static string[] classnames = { "bard", "cleric", "paladin", "ranger", "sorcerer", "warlock", "wizard" };

        private static async Task CallAPIForIndividualSpell(string spellString)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.BaseAddress = new Uri("https://www.dnd5eapi.co/api/");
                HttpResponseMessage response = await client.GetAsync(spellString);
                string content = await response.Content.ReadAsStringAsync();
                //Return if we do not get a successful status code, do it this way to prevent a large nested mess
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }
                Spell? spell = JsonSerializer.Deserialize<Spell>(content);
                //Since spell is nullable, check to make sure we have received data
                if (spell != null)
                {
                    //check that the name isnt null, if it is we use the name so we can re-use this code for both ability score and skills
                    Console.WriteLine(spell.name);
                    foreach (string line in spell.desc)
                    {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine();

                    foreach (string line in spell.higher_level)
                    {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine();

                    if (spell.damage is not null)
                    {
                        foreach (var (key, value) in spell.damage.damage_at_slot_level)
                        {
                            Console.WriteLine(key + ": " + value);
                        }
                    }
                    
                    Console.WriteLine();

                    foreach (DnDClass dndClassThatCanCastSpell in spell.classes)
                    {
                        //check the outer dict to see if we have a valid initial add, if not we only need to obtain the inner dict to append the spell to that dict
                        //We use tryAdd in both of these because we are going to be reusing this function and do not want the program to crash
                        if (!dictOfClasses.Keys.ToArray().Contains(dndClassThatCanCastSpell.name.ToLower()))
                        {
                            Dictionary<string, string> dict = new Dictionary<string, string>();
                            dict.Add(spell.name, spell.url);
                            dictOfClasses.TryAdd(dndClassThatCanCastSpell.name.ToLower(), dict);
                        }
                        else
                        {
                            dictOfClasses[dndClassThatCanCastSpell.name.ToLower()].TryAdd(spell.name, spell.url);
                        }
                    }
                }


            };
        }

        /*
            * Quick function used to obtain all the spells, because the API does not natively have a spell list for each class.
            * We will need to create a spell list for each class to reference in the future
            */
        private static async Task CallAPIFullSpellList()
        {
            string spellListString = "/api/spells";
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.BaseAddress = new Uri("https://www.dnd5eapi.co/api/");
                HttpResponseMessage response = await client.GetAsync(spellListString);
                string content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }
                SpellListSpell? spellList = JsonSerializer.Deserialize<SpellListSpell>(content);

                if (spellList != null)
                {
                    Console.WriteLine(spellList.count);
                    foreach (SpellListSpell.SpellListInner spellListInner in spellList.results)
                    {
                        await CallAPIForIndividualSpell(spellListInner.url);

                    }
                }

            }



        }

        public static async Task Main(string[] args)
        {
            string userinput = "";

            while (true) {
                //Console.WriteLine(cachedSpells.classDict["ranger"]["Alarm"]);
                while (!classnames.Contains(userinput.ToLower()))
                {
                    Console.WriteLine("Please enter the classname to lookup spells for (Bard, Cleric, Paladin, Ranger, Sorcerer, Warlock, or, Wizard, or q to Quit)");
                    userinput = Console.ReadLine();
                    if (userinput.ToLower() == "q")
                    {
                        System.Environment.Exit(0);
                    }
                }
                string className = userinput.ToLower();
                //await CallAPIForIndividualSpell("/api/spells/fireball");
                //await CallAPIFullSpellList();
                cachedSpells.displayDict(className);

                string[] spellNames = cachedSpells.classDict[className].Keys.ToArray();
                while (!spellNames.Contains(userinput))
                {
                    Console.WriteLine("Enter the name of a spell to view, or q to Quit");
                    userinput = Console.ReadLine();
                    if (userinput.ToLower() == "q")
                    {
                        System.Environment.Exit(0);
                    }
                };
                //Write a line to make it look nicer
                Console.WriteLine();
                string spellName = userinput;
                await CallAPIForIndividualSpell(cachedSpells.classDict[className][spellName]);

            }
        }


        /*
            *Function to compose the individual cached dictionary 
            */
        public static List<string> composeDictionaryForWriting(string queriedClass)
        {
            Dictionary<string, string> classdict = dictOfClasses[queriedClass];
            string dictName = queriedClass + "Dict";
            List<string> lines = new List<string> { "public static Dictionary<string, string> " + dictName + " = new Dictionary<string, string>()",
        "{"};

            foreach (var (key, value) in classdict)
            {
                lines.Add("[\"" + key + "\"] = " + "\"" + value + "\",");

            }

            lines.Add("};");
            return lines;
        }

        /*
            * Function used to create the static dictionaries that will be in the program so we wont need to call the API for spells we dont need to look up
            */
        public static void writeToFile()
        {
            List<string> lines = new List<string>();

            foreach (string s in classnames)
            {
                lines.AddRange(composeDictionaryForWriting(s));
            }
            // Set a variable to the Documents path.
            string docPath =
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Write the string array to a new file named "WriteLines.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "dictionary.txt")))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }
        }
    }



    public class Spell
    {
        //public string index { get; set; }
        public string name { get; set; }
        public string[] desc { get; set; }
        public string[] higher_level { get; set; }
        public string[] components { get; set; }
        public string material { get; set; }
        public Boolean ritual { get; set; }
        public string duration { get; set; }
        public Boolean concentration { get; set; }
        public string casting_time { get; set; }
        public int level { get; set; }
        public string attack_type { get; set; }
        public Damage damage { get; set; }
        public School school { get; set; }
        public DnDClass[] classes { get; set; }
        public string url { get; set; }

    }
    public class Damage
    {
        public Damage_type damage_type { get; set; }
        public Dictionary<int, string> damage_at_slot_level { get; set; }

    }
    public class Damage_type
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class School
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class DnDClass
    {
        public string name { get; set; }
        public string url { get; set; }
    }
    public class SpellListSpell
    {
        public int count { get; set; }
        public SpellListInner[] results { get; set; }
        public class SpellListInner
        {
            public string name { get; set; }
            public int level { get; set; }
            public string url { get; set; }

        }
    }
}