using Microsoft.AspNetCore.Mvc;
using WebAPIP01.DTO;
using System.Text.Json;
using System.Reflection;
using System.Linq;

namespace WebAPIP01.Controllers;

[ApiController]
[Route("[controller]")]

public class PlayerController : ControllerBase
{
    const string PATH = "db/db.txt";

    /*
     * filtrowanie 
     *  tam gdzie występuje 
     *  zakres
     *      data od i do - dateFrom - dateTo
     *      liczby - heightFrom - heightTo itd
     *      string - zawiera
     *          filtr=firstName:Kuba,lastName:Wyl,height:80|120,dateFrom:2020-01-01|2020-10-01
     *          w dacie rok-miesiąc-dzień
     * sortowanie multi
     *  po czymś 
     *  ?sotrBy=FirstName,LastName:desc,Country - muszą być nazwy z wielkiej litery oraz notacją wielbłądzią
     *  ?order=
     * stronicowanie ok!
     *  ?onPage=3&page=3
     */

    [HttpGet]
    public string get(
        [FromQuery] int onPage = 0,
        [FromQuery] int page = 1,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? filtr = null
        )
    {
        string data = System.IO.File.ReadAllText(PATH);
        data = "[" + data.Remove(data.Length - 2) + "]";
        List<Player> players = JsonSerializer.Deserialize<List<Player>>(data).ToList();
        
        if (filtr != null)
        {
            List<string> filtrByElements= filtr.Split(',').ToList();
            foreach (string filtrByElement in filtrByElements)
            {
                Tuple<string, string> nameAndRange = splitToNameAndOrder(filtrByElement);

                if (nameAndRange.Item1 == nameof(Player.FirstName))
                { 
                    players = players.Where(p => p.FirstName.Contains(nameAndRange.Item2)).ToList();   
                } 
                else if (nameAndRange.Item1 == nameof(Player.LastName))
                {
                    players = players.Where(p => p.LastName.Contains(nameAndRange.Item2)).ToList();
                }
                else if (nameAndRange.Item1 == nameof(Player.Country))
                {
                    players = players.Where(p => p.Country.Contains(nameAndRange.Item2)).ToList();
                }
                else if(nameAndRange.Item1 == nameof(Player.Birth))
                { 
                    DateTime fromDate = DateTime.Parse(nameAndRange.Item2.Split("|")[0]);
                    DateTime toDate = DateTime.Parse(nameAndRange.Item2.Split("|")[1]);
                    players = players.Where(p => p.Birth >= fromDate && p.Birth <= toDate).ToList();
                }
                else if(nameAndRange.Item1 == nameof(Player.Heigth))
                {
                    float from = float.Parse(nameAndRange.Item2.Split("|")[0]);
                    float to = float.Parse(nameAndRange.Item2.Split("|")[1]);
                    players = players.Where(p => p.Heigth >= from && p.Heigth <= to).ToList();
                }
                else if(nameAndRange.Item1 == nameof(Player.Weight))
                {
                    float from = float.Parse(nameAndRange.Item2.Split("|")[0]);
                    float to = float.Parse(nameAndRange.Item2.Split("|")[1]);
                    players = players.Where(p => p.Weight >= from && p.Weight <= to).ToList();
                }
            }
        }

        if (sortBy != null)
        { 
            List<string> sortByElements = sortBy.Split(',').ToList();
            players.Sort((x, y) =>
            {
                int result = 0;
                foreach (string s in sortByElements)
                {
                    Tuple<string, string> sortByNameAsc = splitToNameAndOrder(s);
                    PropertyInfo prop = typeof(Player).GetProperty(sortByNameAsc.Item1);
                    if (prop.PropertyType == typeof(string))
                    {
                        string a = (string)prop.GetValue(x, null);
                        string b = (string)prop.GetValue(y, null);
                        result = a.CompareTo(b);
                    } else if (prop.PropertyType == typeof(int))
                    {
                        int a = (int)prop.GetValue(x, null);
                        int b = (int)prop.GetValue(y, null);
                        result = a.CompareTo(b);
                    } else if (prop.PropertyType == typeof(float))
                    {
                        float a = (float)prop.GetValue(x, null);
                        float b = (float)prop.GetValue(y, null);
                        result = a.CompareTo(b);
                    } else if (prop.PropertyType == typeof(DateTime))
                    {
                        DateTime a = (DateTime)prop.GetValue(x, null);
                        DateTime b = (DateTime)prop.GetValue(y, null);
                        result = a.CompareTo(b);
                    }
                    if ( result != 0)
                    { 
                        return sortByNameAsc.Item2 == null ? result : -result;
                    }
                }
                return result;
            });
        }

        if (onPage != 0)
        {
            players = players.Skip(onPage * (page - 1)).Take(onPage).ToList();
        }

        return JsonSerializer.Serialize<List<Player>>(players);
    }

    [HttpPost]
    public void post([FromBody] Player player)
    {
        if (new System.IO.FileInfo(PATH).Length < 6)
        {
            player.Id = 0;
        } else
        {
            var lastId = System.IO.File.ReadLines(PATH).Last();
            Player? last = JsonSerializer.Deserialize<Player>(lastId.Remove(lastId.Length - 1));
            player.Id = last.Id + 1;
        }

        string j = JsonSerializer.Serialize<Player>(player);
        System.IO.File.AppendAllText(PATH, j + ",\n");
    }

    [HttpPut("{Id}")]
    public void put([FromRoute]int Id, [FromBody] Player player)
    {
        string data = System.IO.File.ReadAllText(PATH);
        System.IO.File.WriteAllText(PATH, String.Empty);
        string dataJson = "[" + data.Remove(data.Length - 2) + "]";
        List<Player>? players = JsonSerializer.Deserialize<List<Player>>(dataJson);
        players.ForEach(p =>
        {
            if (p.Id == Id)
            {
                p = player;
                player.Id = Id;
            }
            string j = JsonSerializer.Serialize<Player>(p);
            System.IO.File.AppendAllText(PATH, j + ",\n");
        });
    }

    [HttpDelete("{Id}")]
    public void delete([FromRoute] int Id)
    {
        string data = System.IO.File.ReadAllText(PATH);
        System.IO.File.WriteAllText(PATH, String.Empty);
        string dataJson = "[" + data.Remove(data.Length - 2) + "]";
        List<Player>? players = JsonSerializer.Deserialize<List<Player>>(dataJson);
        players.ForEach(p =>
        {
            if (p.Id != Id)
            {
                string j = JsonSerializer.Serialize<Player>(p);
                System.IO.File.AppendAllText(PATH, j + ",\n");
            }
        });
    }

    private Tuple<string, string> splitToNameAndOrder(string value)
    {
        string sortByName;
        string sortByAsc;
        if (value.Contains(':'))
        {
            sortByName = value.Split(':')[0];
            sortByAsc = value.Split(':')[1];
        }
        else
        {
            sortByName = value;
            sortByAsc = null;
        }
        return Tuple.Create(sortByName, sortByAsc);
    }
}
