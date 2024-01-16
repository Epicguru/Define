## Define
Define is a library used to load discreet data objects, known as *defs*, from XML documents.
These defs are intended to be used in games and similar projects to define anything from world objects, entities, or weapons to menu buttons or music tracks.  
It is primarily intended for game developers who value a clear separation between their game's data and behaviour,
and is ideal for developers who are interested in allowing their players to inspect and even modify the game data (although this is far from a requirement).

Define is heavily inspired by [Rimworld](https://rimworldgame.com/)'s game data system, which is functionally similar to this library.

An example *def* file might look like this:
```XML
<Defs>
  <!-- Define a faction of raiders -->
  <EvilRaiders Type="FactionDef">
    <Slogan>Arr! We're really evil!</Slogan>
  </EvilRaiders>
  
  <!-- Define an enemy with ID 'BasicEnemy' -->
  <BasicEnemy Type="EnemyDef">
    <Hitpoints>100</Hitpoints>
    <AttackPower>10</AttackPower>
    <Faction>EvilRaiders</Faction> <!-- This is a reference to the faction created above -->
  </BasicEnemy>
  
  <!-- Define another enemy. -->
  <!-- This boss enemy is a child of BasicEnemy, so all the properties are inherited -->
  <BossEnemy Parent="BasicEnemy">
    <!-- Override the number of hitpoints for this boss -->
    <Hitpoints>200</Hitpoints>
  </BossEnemy>
</Defs>
```

The corresponding C# types for the above defs are as follows:
```C#
class FactionDef : IDef
{
    public string ID { get; set; }
    public string Slogan = "Default slogan";
}

class EnemyDef : IDef
{
    public string ID { get; set; }
    public int Hitpoints = 10;
    public int AttackPower = 5;
    public FactionDef Faction;
}
```

These defs can be loaded like this:
```C#
// A def database is used to load and keep track of defs.
var config = new DefSerializeConfig();
var database = new DefDatabase(config);

string xmlText = File.ReadAllText("MyDefFile.xml");
database.AddDefDocument(xmlText, "MyDefFile.xml");
database.FinishLoading();

// Defs are now loaded and can be accessed.
IReadOnlyList<EnemyDef> allEnemies = database.GetAll<EnemyDef>();
foreach (var enemy in allEnemies)
{
    Console.WriteLine($"{enemy.ID}: {enemy.Hitpoints} hp, {enemy.AttackPower} ap");
}
/*
 * Prints:
 * BasicEnemy: 100 hp, 10 ap
 * BossEnemy: 200 hp, 10 ap
 */
```