# Heroes of Might and Magic 3 (HoMM3) Savegame Editor

Well, I want to learn Windows Presentation Foundation (WPF) by doing a project, and I recently
got back into playing HoMM3 after discovering the wonderful
[HoMM3 HD Mod](https://sites.google.com/site/heroes3hd/). With all the real-life responsibilities
these days, I can no longer bear the tedious part of the game, thus this savegame editor to make it
a little easier.

## About CGM Format

HoMM3 Savegame (.CGM) is supposed to be a GZip file, but all the tools/libraries either fail to unzip it
or complain about CRC error:
* [7-Zip](https://www.7-zip.org/): can extract but complains about CRC error
* [GZipStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.gzipstream?view=netcore-3.1):
  unsupported compression method exception
* [sharpcompress](https://github.com/adamhathcock/sharpcompress): unsupported compression method exception
* [SharpZipLib](https://github.com/icsharpcode/SharpZipLib): CRC exception

In particular, SharpZipLib can unzip it but will raise a CRC exception, which in some cases will stop halfway
through uncompression. This is the reason why I included part of SharpZipLib 1.2.0 source code in this
project -- it's the part that deals with GZip but with the line raising CRC exception commented out.

## Acknowlegement

* [Editing heroes in memory](http://heroescommunity.com/viewthread.php3?TID=18817) on
  [Heroes Community](http://heroescommunity.com/)
* [HoMM3: Hex editing issue](https://www.gog.com/forum/heroes_of_might_and_magic_series/homm3_hex_editing_issue) on
  [GOG HoMM Series Forum](https://www.gog.com/forum/heroes_of_might_and_magic_series#1589412409).
* Various game information on [HoMM3 Wiki](https://heroes.thelazy.net//index.php/Main_Page).
* [Visual Studio Image Library](https://www.microsoft.com/en-us/download/details.aspx?id=35825) for the icons.
* [Exhumed](http://www.iconarchive.com/artist/3xhumed.html) for the application icon.