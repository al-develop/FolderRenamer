This Tool renames folders which contain music files.
Here's an exmaple:

intial folder name: 
 - The Beatles - White Album (1968)
-  Elvis - Good Times
-  Led Zeppelin
  
will be renamed to
-  1968 - The Beatles - White Album
-  1974 - Elvis - Good Times
-  1975 - Led Zeppelin - Physical Graffiti

For the renaming, it will either use the year in the braces (normal braces, [] and {} are supported) 
or, if no braces with year is available, it will use the first *.mp3 file to determine the album name or/and the year by mp3 tags 
(using #TagLib from NuGet)

Folders which coulndn't be renamed, get logged in a separate *.txt File in BaseDirectory.
